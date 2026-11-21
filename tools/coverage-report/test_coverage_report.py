"""Tests for coverage_report: merging, the area map, and the two boundary checks.

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
    "excluded": [{"paths": ["src/engine/DAE/REST/"], "reason": "nothing calls it"}],
}

OPTIONS = coverage_report.Options(25.0, 50.0, 200)


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
            ("Marshal", "src/engine/DAE/REST/RESTMarshal.cs"): lines(0, 40),
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


if __name__ == "__main__":
    unittest.main()
