"""Tests for coverage_report: merging, the area map, the untested-file index and the two boundary checks.

Run with: python -m unittest discover -s tools/coverage-report

The fixtures are tiny Cobertura documents written by the test; a real report of this
repository is tens of megabytes, far too large to check in.
"""

import contextlib
import io
import json
import os
import shutil
import tempfile
import unittest
import xml.etree.ElementTree as ElementTree

import coverage_report


# ---------------------------------------------------------------------------
# Fixtures

def cobertura(packages):
    """A Cobertura document for {assembly: {(class, filename): {line: hits}}}."""
    coverage = ElementTree.Element("coverage", {"line-rate": "0", "version": "1.9"})
    ElementTree.SubElement(ElementTree.SubElement(coverage, "sources"), "source").text = "."
    container = ElementTree.SubElement(coverage, "packages")
    for name, classes in packages.items():
        package = ElementTree.SubElement(container, "package", {"name": name})
        elements = ElementTree.SubElement(package, "classes")
        for (class_name, filename), lines in classes.items():
            element = ElementTree.SubElement(elements, "class", {"name": class_name, "filename": filename})
            ElementTree.SubElement(element, "methods")
            line_container = ElementTree.SubElement(element, "lines")
            for number, hits in sorted(lines.items()):
                ElementTree.SubElement(line_container, "line", {"number": str(number), "hits": str(hits)})
    return ElementTree.tostring(coverage, encoding="unicode")


def lines(covered, uncovered, first=1):
    """A line map with `covered` hit lines followed by `uncovered` unhit ones."""
    return dict([(first + i, 1) for i in range(covered)]
                + [(first + covered + i, 0) for i in range(uncovered)])


def totals(covered, coverable):
    result = coverage_report.Totals()
    result.add(lines(covered, coverable - covered))
    return result


MAP = {
    "assemblies": {"engine": ["Engine"], "reported": ["Library"], "ignored": ["Tests"]},
    "areas": [
        {"name": "Schema", "paths": ["src/engine/DAE/Schema/"]},
        {"name": "Security", "paths": ["src/engine/DAE/Schema/Security.cs"]},
        {"name": "Debugger", "paths": ["src/engine/DAE/Debug/"]},
    ],
    "excluded": [{"paths": ["artifacts/"], "reason": "generated source"}],
}

OPTIONS = coverage_report.Options(25.0, 50.0, 200, 20)


class Tree:
    """A temporary folder holding an areas file and Cobertura reports."""

    def __init__(self, area_map=None):
        self.folder = tempfile.mkdtemp()
        self.areas = os.path.join(self.folder, "areas.json")
        with open(self.areas, "w", encoding="utf-8") as stream:
            json.dump(MAP if area_map is None else area_map, stream)

    def report(self, name, packages):
        path = os.path.join(self.folder, name + ".cobertura.xml")
        with open(path, "w", encoding="utf-8") as stream:
            stream.write(cobertura(packages))
        return path

    def run(self, *arguments):
        """main() over the whole folder; returns (exit code, standard output)."""
        with contextlib.redirect_stdout(io.StringIO()) as output:
            code = coverage_report.main([self.folder, "--areas", self.areas] + list(arguments))
        return code, output.getvalue()

    def close(self):
        shutil.rmtree(self.folder)


def parsed(packages):
    """One in-memory Cobertura document, parsed as the tool parses a file."""
    with tempfile.NamedTemporaryFile("w", suffix=".cobertura.xml", encoding="utf-8", delete=False) as stream:
        stream.write(cobertura(packages))
        path = stream.name
    try:
        return coverage_report.parse_report(path, None)
    finally:
        os.remove(path)


# ---------------------------------------------------------------------------

SECURITY = ("Security", "src/engine/DAE/Schema/Security.cs")
CATALOG = ("Catalog", "src/engine/DAE/Schema/Catalog.cs")
DEBUGGER = ("Debugger", "src/engine/DAE/Debug/Debugger.cs")


class MergeTest(unittest.TestCase):
    def test_a_line_covered_in_either_report_is_covered(self):
        first = parsed({"Engine": {SECURITY: {1: 0, 2: 1, 3: 0}}})
        second = parsed({"Engine": {SECURITY: {1: 4, 2: 0, 3: 0},
                                    DEBUGGER: {7: 1}}})
        merged = coverage_report.merge([first, second])
        self.assertEqual(merged["Engine"][SECURITY], {1: 4, 2: 1, 3: 0})
        # A class only one report saw survives the merge.
        self.assertEqual(merged["Engine"][DEBUGGER], {7: 1})

    def test_reports_of_different_builds_are_refused(self):
        tree = Tree()
        self.addCleanup(tree.close)
        tree.report("fresh", {"Engine": {SECURITY: lines(5, 5)}})
        tree.report("stale", {"Engine": {SECURITY: lines(4, 4)}})
        code, output = tree.run()
        self.assertEqual(code, 1)
        self.assertIn("line sets differ", output)
        self.assertIn("Engine/Security", output)
        self.assertNotIn("# Test coverage report", output)


class AreaMapTest(unittest.TestCase):
    def analysis(self, packages, area_map=None):
        return coverage_report.analyse(coverage_report.merge([parsed(packages)]),
                                       coverage_report.Map.from_data(area_map or MAP), OPTIONS)

    def area(self, analysis, name):
        return next((area for area in analysis.areas if area.name == name), None)

    def test_a_file_entry_beats_the_folder_that_contains_it(self):
        analysis = self.analysis({"Engine": {SECURITY: lines(1, 9), CATALOG: lines(2, 3)}})
        self.assertEqual(self.area(analysis, "Security").totals.coverable, 10)
        self.assertEqual([path for path, _ in self.area(analysis, "Security").files], [SECURITY[1]])
        # The folder keeps only what the longer path did not claim.
        self.assertEqual(self.area(analysis, "Schema").totals.coverable, 5)
        self.assertEqual([path for path, _ in self.area(analysis, "Schema").files], [CATALOG[1]])

    def test_excluded_and_test_code_belong_to_no_area(self):
        analysis = self.analysis({"Engine": {
            ("Generated", "artifacts/obj/Generated.cs"): lines(0, 40),
            ("Fixture", "tests/DAE.Server.Tests/CursorTest.cs"): lines(30, 0),
            CATALOG: lines(2, 3)}})
        self.assertEqual([area.name for area in analysis.areas], ["Schema"])
        self.assertEqual(analysis.unassigned, [])
        self.assertEqual([exclusion.totals.coverable for exclusion in analysis.exclusions], [40])
        # Test code is dropped outright, so it does not even reach the assembly rollup.
        self.assertEqual(analysis.assemblies["Engine"].coverable, 45)


class ClassificationTest(unittest.TestCase):
    def test_the_thresholds_are_exclusive(self):
        self.assertEqual(coverage_report.classify(totals(250, 1000), OPTIONS), "thin")
        self.assertEqual(coverage_report.classify(totals(249, 1000), OPTIONS), "gap")
        self.assertEqual(coverage_report.classify(totals(500, 1000), OPTIONS), "covered")
        self.assertEqual(coverage_report.classify(totals(499, 1000), OPTIONS), "thin")

    def test_a_small_area_is_never_a_gap(self):
        self.assertEqual(coverage_report.classify(totals(0, 199), OPTIONS), "thin")
        self.assertEqual(coverage_report.classify(totals(0, 200), OPTIONS), "gap")


class UnassignedTest(unittest.TestCase):
    NEW_FOLDER = ("Thing", "src/engine/DAE/NewFolder/Thing.cs")

    def setUp(self):
        self.tree = Tree()
        self.addCleanup(self.tree.close)
        self.tree.report("run", {"Engine": {CATALOG: lines(2, 3), self.NEW_FOLDER: lines(0, 7)}})

    def test_a_source_folder_no_area_claims_suppresses_the_report(self):
        code, output = self.tree.run()
        self.assertEqual(code, 1)
        self.assertIn("unassigned: src/engine/DAE/NewFolder/Thing.cs (7 lines)", output)
        self.assertNotIn("# Test coverage report", output)

    def test_allow_unassigned_prints_the_report_and_names_the_hole_in_it(self):
        code, output = self.tree.run("--allow-unassigned")
        self.assertEqual(code, 1)
        self.assertIn("unassigned: src/engine/DAE/NewFolder/Thing.cs (7 lines)", output)
        self.assertIn("# Test coverage report", output)
        # The report itself has to say it is incomplete: the Markdown is what gets
        # published, and the findings above went only to the terminal.
        report = output[output.index("# Test coverage report"):]
        self.assertIn("The map is incomplete", report)
        self.assertIn("| src/engine/DAE/NewFolder/Thing.cs | 7 |", report)


class UnlistedAssemblyTest(unittest.TestCase):
    """An assembly on none of the three lists: the other half of an incomplete map."""

    def setUp(self):
        self.tree = Tree()
        self.addCleanup(self.tree.close)
        self.tree.report("run", {"Engine": {CATALOG: lines(2, 3)},
                                 "Alphora.Dataphor.Newly.Converted": {
                                     ("Thing", "src/newly/converted/Thing.cs"): lines(0, 9)}})

    def test_an_assembly_on_no_list_suppresses_the_report(self):
        code, output = self.tree.run()
        self.assertEqual(code, 1)
        self.assertIn("unlisted assembly: Alphora.Dataphor.Newly.Converted (9 lines)", output)
        self.assertNotIn("# Test coverage report", output)

    def test_allow_unassigned_prints_the_report_and_names_the_assembly_in_it(self):
        code, output = self.tree.run("--allow-unassigned")
        self.assertEqual(code, 1)
        report = output[output.index("# Test coverage report"):]
        self.assertIn("| Alphora.Dataphor.Newly.Converted | 9 |", report)


class UntestedFilesTest(unittest.TestCase):
    """`## Wholly untested files`: the cross-area index of files nothing executed.

    These go through main() rather than untested_files() alone, because the defect was in
    the rendering - the per-file numbers were already computed and already in --json.
    """

    CATALOG_COVERED = ("Catalog", "src/engine/DAE/Schema/Catalog.cs")
    PLAN = ("Plan", "src/engine/DAE/Schema/Plan.cs")
    ORDER = ("Order", "src/engine/DAE/Schema/Order.cs")
    EMPTY = ("Empty", "src/engine/DAE/Schema/Empty.cs")
    DEBUGGER_ALL = ("Debugger", "src/engine/DAE/Debug/Debugger.cs")

    def section(self, report):
        """Just the `## Wholly untested files` section, up to the next heading."""
        start = report.index("## Wholly untested files")
        end = report.find("\n## ", start + 1)
        return report[start:] if end < 0 else report[start:end]

    def test_a_zero_inside_a_covered_area_is_listed_with_its_area(self):
        tree = Tree()
        self.addCleanup(tree.close)
        # Schema reaches 62.5 % and so is `covered`; Debug is 250 lines at 0 % and so is a
        # `gap`. Before this section the two Schema zeros appeared in no table at all.
        tree.report("run", {"Engine": {self.CATALOG_COVERED: lines(100, 0),
                                       self.PLAN: lines(0, 30),
                                       self.ORDER: lines(0, 30),
                                       self.EMPTY: {},
                                       self.DEBUGGER_ALL: lines(0, 250)}})
        code, output = tree.run()
        # The tool reports and never gates: a wholly untested file is not an error.
        self.assertEqual(code, 0)
        report = output[output.index("# Test coverage report"):]
        section = self.section(report)
        self.assertIn("| src/engine/DAE/Schema/Plan.cs | 30 | Schema | 62.5 % |", section)
        # Worst first by coverable count; the two 30-line files tie and come out in path
        # order, so two runs of one build render identically.
        self.assertEqual([line.split(" | ")[0] for line in section.splitlines() if line.startswith("| src/")],
                         ["| src/engine/DAE/Debug/Debugger.cs",
                          "| src/engine/DAE/Schema/Order.cs",
                          "| src/engine/DAE/Schema/Plan.cs"])
        # A file with lines, all of them hit, is not untested.
        self.assertNotIn("Catalog.cs", section)
        # Nor is one with no coverable lines at all: its rate is None, and a row for it
        # would read as a zero rather than as "nothing to measure".
        self.assertNotIn("Empty.cs", section)
        # A zero inside a gap area is printed twice on purpose - here and in the gap's own
        # per-file table - so that this section's claim needs no caveat.
        self.assertEqual(report.count("src/engine/DAE/Debug/Debugger.cs"), 2)

    def test_the_floor_lists_a_file_at_it_and_counts_the_ones_below(self):
        tree = Tree()
        self.addCleanup(tree.close)
        tree.report("run", {"Engine": {
            self.CATALOG_COVERED: lines(50, 0),
            ("Exact", "src/engine/DAE/Schema/Exact.cs"): lines(0, 20),
            ("Small", "src/engine/DAE/Schema/Small.cs"): lines(0, 19),
            ("Tiny", "src/engine/DAE/Schema/Tiny.cs"): lines(0, 5)}})
        code, output = tree.run()
        self.assertEqual(code, 0)
        section = self.section(output[output.index("# Test coverage report"):])
        # Exactly --untested-min-lines is listed, one fewer is only counted, following the
        # convention classify() already uses for --min-lines.
        self.assertIn("| src/engine/DAE/Schema/Exact.cs | 20 |", section)
        self.assertNotIn("Small.cs", section)
        self.assertNotIn("Tiny.cs", section)
        self.assertIn("Another 2 wholly untested files of fewer than 20 coverable lines each,"
                      " 24 lines between them, are not listed.", section)

    def test_a_floor_that_holds_nothing_back_is_not_described_as_a_rule(self):
        """--untested-min-lines 0 lists everything, so no sentence may promise a floor.

        The report used to say "down to 0 coverable lines" in its preamble while leaving a
        file of no coverable lines out, and at 1 it read "fewer than 1 coverable lines".
        """
        tree = Tree()
        self.addCleanup(tree.close)
        tree.report("run", {"Engine": {self.CATALOG_COVERED: lines(100, 0),
                                       self.PLAN: lines(0, 30),
                                       ("Tiny", "src/engine/DAE/Schema/Tiny.cs"): lines(0, 7),
                                       self.EMPTY: {}}})
        code, output = tree.run("--untested-min-lines", "0")
        self.assertEqual(code, 0)
        report = output[output.index("# Test coverage report"):]
        # Nothing is held back, so both the smallest file and the whole report say so.
        self.assertIn("| src/engine/DAE/Schema/Tiny.cs | 7 |", self.section(report))
        self.assertNotIn("coverable lines are counted at the end", report)
        self.assertNotIn("down to 0 coverable lines", report)
        # A file with no coverable line is still not a finding, at any floor.
        self.assertNotIn("Empty.cs", report)

    def test_the_section_prints_a_positive_line_when_nothing_qualifies(self):
        tree = Tree()
        self.addCleanup(tree.close)
        tree.report("run", {"Engine": {CATALOG: lines(5, 5)}})
        code, output = tree.run()
        self.assertEqual(code, 0)
        # Silence is how this blind spot went unnoticed: a reader cannot tell "nothing to
        # report" from "this tool does not report that".
        self.assertIn("No source file in an engine area is wholly untested.",
                      self.section(output[output.index("# Test coverage report"):]))


class ShippedMapTest(unittest.TestCase):
    """The checked-in areas.json, read against the tree it describes.

    Every other test here runs on fixtures. This one is about the shipped map going stale:
    a ticket that deletes a source file has to remember to take its path out of the map by
    hand, and a path left behind silently claims nothing, which moves an area's rate without
    anyone noticing. Generated roots are exempt because a clean clone has not built them yet.
    """

    SOURCE_ROOTS = ("src/", "libraries/", "samples/", "tests/", "tools/")

    def test_every_source_path_in_the_shipped_map_is_in_the_tree(self):
        area_map = json.load(open(coverage_report.DEFAULT_AREAS, encoding="utf-8-sig"))
        root = coverage_report.find_repo_root(os.path.dirname(os.path.abspath(coverage_report.__file__)))
        self.assertIsNotNone(root, "the test needs the repository it describes")
        named = [("area " + area["name"], path) for area in area_map["areas"] for path in area["paths"]]
        named += [("exclusion " + exclusion["reason"][:40], path)
                  for exclusion in area_map.get("excluded", []) for path in exclusion["paths"]]
        for who, path in named:
            if path.startswith(self.SOURCE_ROOTS):
                self.assertTrue(os.path.exists(os.path.join(root, *path.split("/"))),
                                "%s names %s, which is not in the tree" % (who, path))


if __name__ == "__main__":
    unittest.main()
