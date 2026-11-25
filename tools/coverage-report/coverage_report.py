#!/usr/bin/env python3
"""Roll up Cobertura coverage reports into a readable report of what the tests exercise.

Python 3 standard library only. See README.md in this folder for the rules, the
collect command and what the numbers do and do not mean.

NOTE: CI does not collect coverage yet, but the reason it did not has now been measured
away: `test-coverage-gap-analysis` ran the whole suite under instrumentation on 2026-11-21
with no failure of any kind, at +16 % on the longest part, and the session-reclamation case
moved 0.3 s inside its own existing spread (docs/coverage-report.md). What is left is a
decision, not a risk: adding `--collect:"Code Coverage;Format=cobertura"` to the existing CI
test step is a two-line change and gives a Cobertura artifact per push. A number that is
reported rather than gated must still never be able to fail the build.
"""

import argparse
import collections
import json
import os
import sys
import xml.etree.ElementTree as ElementTree

DEFAULT_AREAS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "areas.json")
GROUPS = ("engine", "reported", "ignored")

# The classification thresholds, as percentages and line counts.
Options = collections.namedtuple("Options", "gap_below thin_below min_lines untested_min_lines")


class MapError(Exception):
    """The areas file is missing, unreadable or malformed."""


class MergeError(Exception):
    """Two input reports disagree about a class, so their numbers cannot be added up."""


# ---------------------------------------------------------------------------
# Repository-relative source paths

def find_repo_root(start):
    current = os.path.abspath(start)
    while True:
        if os.path.exists(os.path.join(current, ".git")):
            return current
        parent = os.path.dirname(current)
        if parent == current:
            return None
        current = parent


def relativize(filename, repo_root):
    """A source path repository-relative with forward slashes, or unchanged.

    The Cobertura filenames are the build machine's absolute paths. One that is not
    under repo_root - a report produced somewhere else - is returned exactly as it
    came, so the file still shows up in the report instead of crashing the run.
    """
    if not filename:
        return filename
    if not os.path.isabs(filename):
        return os.path.normpath(filename).replace(os.sep, "/")
    if repo_root is None:
        return filename
    absolute = os.path.normpath(filename)
    root = os.path.normpath(os.path.abspath(repo_root))
    # normcase only changes case and separators, never length, so the index into the
    # original (correctly cased) path is the length of the root plus its separator.
    if os.path.normcase(absolute).startswith(os.path.normcase(root) + os.sep):
        return absolute[len(root) + 1:].replace(os.sep, "/")
    return filename


def source_path(filename, sources, repo_root):
    """The reported path of one class's source file.

    A relative filename is resolved against each `<sources><source>` entry, as
    Cobertura defines; the first candidate that lands inside the repository wins.
    """
    if os.path.isabs(filename):
        candidates = [filename]
    else:
        candidates = [os.path.join(source, filename) for source in sources] + [filename]
    for candidate in candidates:
        path = relativize(candidate, repo_root)
        if not os.path.isabs(path):
            return path
    return relativize(candidates[0], repo_root)


# ---------------------------------------------------------------------------
# Reading and merging Cobertura reports

def find_inputs(paths):
    """The Cobertura files named by paths; a directory is searched recursively."""
    found = []
    seen = set()
    for path in paths:
        if os.path.isdir(path):
            names = []
            for folder, subfolders, files in os.walk(path):
                subfolders.sort()
                names.extend(os.path.join(folder, name) for name in sorted(files)
                             if name.lower().endswith(".cobertura.xml"))
        else:
            names = [path]
        for name in names:
            key = os.path.normcase(os.path.abspath(name))
            if key not in seen:
                seen.add(key)
                found.append(name)
    return found


def parse_report(path, repo_root):
    """One Cobertura file as {assembly: {(class, source path): {line number: hits}}}.

    A Cobertura `<package>` is one assembly and a `<class>` is one type. Lines are
    taken from both the class and its methods, because the collector lists a line in
    both places; within one file the higher hit count wins.
    """
    root = ElementTree.parse(path).getroot()
    sources = [element.text.strip() for element in root.iterfind("./sources/source") if element.text]
    packages = {}
    for package in root.iter("package"):
        classes = packages.setdefault(package.get("name") or "", {})
        for element in package.iter("class"):
            key = (element.get("name") or "", source_path(element.get("filename") or "", sources, repo_root))
            lines = classes.setdefault(key, {})
            for line in element.iter("line"):
                try:
                    number = int(line.get("number"))
                    hits = int(line.get("hits") or 0)
                except (TypeError, ValueError):
                    continue
                lines[number] = max(lines.get(number, 0), hits)
    return packages


def merge(reports):
    """Union of several parsed reports: a line is covered if any report hit it.

    Raises MergeError when a class common to two reports has different line numbers.
    That is what a stale report mixed with a fresh one looks like - the two were
    measured against different builds - and adding them up gives a number nobody can
    trust. Reports of one build agree exactly: verified over two independent runs,
    where all 76 classes in both had identical line sets.

    NOTE: the guard only compares classes whose key matches, and the key carries the
    resolved source path. Two reports collected on *different machines*, whose checkouts
    sit at different absolute paths, would resolve the same class to two keys, and both
    would be counted - inflating the coverable total silently instead of raising here.
    Every report today comes from the machine that runs the suite; if reports ever have
    to be merged across machines, key the merge on the class name alone.
    """
    merged = {}
    for packages in reports:
        for package, classes in packages.items():
            target = merged.setdefault(package, {})
            for key, lines in classes.items():
                if key not in target:
                    target[key] = dict(lines)
                    continue
                if set(target[key]) != set(lines):
                    raise MergeError("line sets differ: %s/%s" % (package, key[0]))
                for number, hits in lines.items():
                    target[key][number] = max(target[key][number], hits)
    return merged


# ---------------------------------------------------------------------------
# The map: assembly lists, areas and exclusions

def _matches(path, pattern):
    """A pattern ending in `/` is a folder prefix; anything else is one file."""
    return path.startswith(pattern) if pattern.endswith("/") else path == pattern


class Map:
    """The assembly lists, the area map and the exclusions of one areas file."""

    def __init__(self, groups, lists, areas, excluded):
        self.groups = groups        # assembly name -> "engine" | "reported" | "ignored"
        self.lists = lists          # group -> assembly names, in file order
        self.areas = areas          # [(name, [paths])], in file order
        self.excluded = excluded    # [{"paths": [...], "reason": str, "ticket": str}]
        self._area_paths = [(path, name) for name, paths in areas for path in paths]
        self._excluded_paths = [(path, index) for index, entry in enumerate(excluded)
                                for path in entry["paths"]]

    @classmethod
    def from_data(cls, data, source="the areas map"):
        if not isinstance(data, dict):
            raise MapError("%s is not a JSON object" % source)
        assemblies = data.get("assemblies")
        if not isinstance(assemblies, dict):
            raise MapError("%s has no 'assemblies' object" % source)
        groups = {}
        lists = {}
        for group in GROUPS:
            names = assemblies.get(group, [])
            if not isinstance(names, list) or any(not isinstance(name, str) for name in names):
                raise MapError("%s: assemblies.%s is not a list of names" % (source, group))
            lists[group] = names
            for name in names:
                if name in groups:
                    raise MapError("%s: assembly %s is in both the %s and %s lists"
                                   % (source, name, groups[name], group))
                groups[name] = group
        areas = []
        claimed = {}
        for entry in data.get("areas", []):
            if not isinstance(entry, dict) or not isinstance(entry.get("name"), str):
                raise MapError("%s: every area needs a 'name'" % source)
            paths = entry.get("paths")
            if not isinstance(paths, list) or not paths or any(not isinstance(p, str) for p in paths):
                raise MapError("%s: area %s needs a non-empty 'paths' list" % (source, entry["name"]))
            if any(entry["name"] == name for name, _ in areas):
                raise MapError("%s: two areas are named %s" % (source, entry["name"]))
            for path in paths:
                if path in claimed:
                    raise MapError("%s: path %s is claimed by both %s and %s"
                                   % (source, path, claimed[path], entry["name"]))
                claimed[path] = entry["name"]
            areas.append((entry["name"], paths))
        excluded = []
        for entry in data.get("excluded", []):
            if not isinstance(entry, dict):
                raise MapError("%s: every exclusion is an object with 'paths' and 'reason'" % source)
            paths = entry.get("paths")
            if not isinstance(paths, list) or not paths or any(not isinstance(p, str) for p in paths):
                raise MapError("%s: an exclusion needs a non-empty 'paths' list" % source)
            if not isinstance(entry.get("reason"), str):
                raise MapError("%s: exclusion %s needs a 'reason'" % (source, paths[0]))
            excluded.append({"paths": paths, "reason": entry["reason"], "ticket": entry.get("ticket")})
        return cls(groups, lists, areas, excluded)

    def area_of(self, path):
        """The area whose longest path claims this file, so a file entry beats its folder."""
        best = None
        for pattern, name in self._area_paths:
            if _matches(path, pattern) and (best is None or len(pattern) > len(best[0])):
                best = (pattern, name)
        return best[1] if best else None

    def exclusion_of(self, path):
        """The index of the exclusion whose longest path claims this file, or None."""
        best = None
        for pattern, index in self._excluded_paths:
            if _matches(path, pattern) and (best is None or len(pattern) > len(best[0])):
                best = (pattern, index)
        return best[1] if best else None


def load_map(path):
    try:
        with open(path, encoding="utf-8-sig") as stream:
            data = json.load(stream)
    except OSError as error:
        raise MapError("cannot read areas file %s: %s" % (path, error.strerror))
    except ValueError as error:
        raise MapError("areas file %s is not valid JSON: %s" % (path, error))
    return Map.from_data(data, path)


# ---------------------------------------------------------------------------
# Rolling up

class Totals:
    def __init__(self):
        self.covered = 0
        self.coverable = 0

    def add(self, lines):
        self.coverable += len(lines)
        self.covered += sum(1 for hits in lines.values() if hits)

    @property
    def uncovered(self):
        return self.coverable - self.covered

    @property
    def rate(self):
        """Line coverage as a percentage, or None for an assembly with no lines."""
        return 100.0 * self.covered / self.coverable if self.coverable else None


def classify(totals, options):
    """An area's verdict: gap, thin or covered.

    The boundaries are strict: an area exactly at --gap-below is thin, not a gap, and
    one exactly at --thin-below is covered. An area smaller than --min-lines is never
    a gap however low its rate, because a handful of lines is not worth a ticket.
    """
    rate = totals.rate
    if rate is None:
        return "covered"
    if rate < options.gap_below and totals.coverable >= options.min_lines:
        return "gap"
    if rate < options.thin_below:
        return "thin"
    return "covered"


def untested_files(analysis):
    """Every file in an engine area with coverable lines and no covered line.

    [(path, Totals, Area)], worst first by coverable count, ties by path. A file with
    no coverable lines at all is not untested, it is unmeasurable, and is left out: its
    rate is None and a row for it would read as a zero.

    Every area is searched, gaps included, so a file at 0 % inside a gap area is printed
    both here and in that area's own per-file table. That is deliberate: it makes this
    section's claim unconditional, with no "except the ones already below" caveat for a
    reader to carry.
    """
    found = [(path, totals, area)
             for area in analysis.areas
             for path, totals in area.files
             if totals.coverable and not totals.covered]
    found.sort(key=lambda item: (-item[1].coverable, item[0]))
    return found


class Area:
    def __init__(self, name):
        self.name = name
        self.totals = Totals()
        self.files = []         # [(path, Totals)]
        self.verdict = None


class Exclusion:
    def __init__(self, entry):
        self.entry = entry
        self.totals = Totals()
        self.files = 0


class Analysis:
    def __init__(self):
        self.inputs = 0
        self.assemblies = {}        # name -> Totals, for engine and reported only
        self.areas = []             # sorted worst first
        self.exclusions = []
        self.unassigned = []        # [(path, coverable lines)]
        self.unlisted = []          # [(assembly, coverable lines)]
        self.outside_root = 0

    @property
    def gaps(self):
        return [area for area in self.areas if area.verdict == "gap"]


def analyse(merged, area_map, options):
    """Roll a merged report up per assembly, per area and per file, and check the map.

    Classes whose source is under `tests/` are dropped wherever they appear: test code
    is not the subject of the report.

    NOTE: that drop happens before the assembly grouping, so an assembly whose source is
    *entirely* under `tests/` leaves nothing to group and never reaches either assembly
    check - it is neither rolled up nor reported as unlisted. That is why the test
    assemblies, the harness and the two test-support libraries belong on the `ignored`
    list for documentation only, and why a `reported` entry for one of them could only
    ever read `not in this run`. If a test-support assembly ever has to be measured,
    group first and drop `tests/` source inside the engine rollup only.
    """
    analysis = Analysis()
    engine_lines = {}       # repository-relative path -> {line number: hits}
    for package in sorted(merged):
        by_path = {}
        for (_name, path), lines in merged[package].items():
            if path.startswith("tests/"):
                continue
            target = by_path.setdefault(path, {})
            for number, hits in lines.items():
                target[number] = max(target.get(number, 0), hits)
        if not by_path:
            continue
        group = area_map.groups.get(package)
        if group is None:
            analysis.unlisted.append((package, sum(len(lines) for lines in by_path.values())))
            continue
        if group == "ignored":
            continue
        totals = analysis.assemblies.setdefault(package, Totals())
        for path, lines in by_path.items():
            totals.add(lines)
            if os.path.isabs(path):
                analysis.outside_root += 1
            if group == "engine":
                target = engine_lines.setdefault(path, {})
                for number, hits in lines.items():
                    target[number] = max(target.get(number, 0), hits)

    areas = {}
    exclusions = [Exclusion(entry) for entry in area_map.excluded]
    for path in sorted(engine_lines):
        lines = engine_lines[path]
        # NOTE: an exclusion is consulted before the areas, so a folder exclusion beats a
        # longer single-file area entry inside it - unlike the longest-prefix rule between
        # areas. No entry relies on that today; if one ever needs to, compare the two
        # matched pattern lengths here instead of ordering the checks.
        index = area_map.exclusion_of(path)
        if index is not None:
            exclusions[index].totals.add(lines)
            exclusions[index].files += 1
            continue
        name = area_map.area_of(path)
        if name is None:
            # A new source folder has to be put on the map before it can be reported on.
            if path.startswith("src/"):
                analysis.unassigned.append((path, len(lines)))
            continue
        area = areas.get(name)
        if area is None:
            area = areas[name] = Area(name)
        area.totals.add(lines)
        file_totals = Totals()
        file_totals.add(lines)
        area.files.append((path, file_totals))

    for area in areas.values():
        area.verdict = classify(area.totals, options)
        area.files.sort(key=lambda item: (-item[1].uncovered, item[0]))
    analysis.areas = sorted(areas.values(), key=lambda area: (-area.totals.uncovered, area.name))
    analysis.exclusions = [exclusion for exclusion in exclusions if exclusion.files]
    return analysis


# ---------------------------------------------------------------------------
# The Markdown report

def _rate(totals):
    return "-" if totals.rate is None else "%.1f %%" % totals.rate


def _count(value):
    return "{:,}".format(value)


def _plural(count, noun):
    return "%d %s%s" % (count, noun, "" if count == 1 else "s")


def _assembly_table(out, names, assemblies):
    out.append("| Assembly | Line coverage | Covered | Coverable |")
    out.append("|---|---:|---:|---:|")
    total = Totals()
    for name in names:
        totals = assemblies.get(name)
        if totals is None:
            out.append("| %s | - | - | not in this run |" % name)
            continue
        total.covered += totals.covered
        total.coverable += totals.coverable
        out.append("| %s | %s | %s | %s |" % (name, _rate(totals), _count(totals.covered), _count(totals.coverable)))
    out.append("| **Total** | **%s** | **%s** | **%s** |" % (_rate(total), _count(total.covered), _count(total.coverable)))
    out.append("")


def _incomplete_map_section(out, analysis):
    """Name the map's holes in the report, not only on standard output.

    The findings are printed to the terminal, but the Markdown is what gets published,
    so a report rendered under --allow-unassigned has to carry them too: without this a
    published docs/coverage-report.md reads as complete while a subsystem is missing
    from every total below.
    """
    if not (analysis.unassigned or analysis.unlisted):
        return
    out.append("## The map is incomplete - read the numbers below with that in mind")
    out.append("")
    out.append("Everything named here is **absent from the areas and the totals below**, so those numbers describe less than the engine. Put each one on the map (`tools/coverage-report/areas.json`) and run the report again.")
    out.append("")
    if analysis.unassigned:
        out.append("| Source file no area claims | Coverable |")
        out.append("|---|---:|")
        for path, lines in analysis.unassigned:
            out.append("| %s | %s |" % (path, _count(lines)))
        out.append("")
    if analysis.unlisted:
        out.append("| Assembly on no list | Coverable |")
        out.append("|---|---:|")
        for name, lines in analysis.unlisted:
            out.append("| %s | %s |" % (name, _count(lines)))
        out.append("")


def _untested_files_section(out, analysis, options):
    """The cross-area index of files nothing executed - the one table an area rate hides.

    An area's rate is a weighted average over its files, so before this section a file at
    exactly 0 % inside a well-covered area appeared in no table of the report at all. The
    empty case still prints its heading and a sentence: silence is how that blind spot went
    unnoticed, because a reader cannot tell "nothing to report" from "not reported here".
    """
    out.append("## Wholly untested files")
    out.append("")
    listed = untested_files(analysis)
    if not listed:
        out.append("No source file in an engine area is wholly untested.")
        out.append("")
        return
    named = [item for item in listed if item[1].coverable >= options.untested_min_lines]
    counted = [item for item in listed if item[1].coverable < options.untested_min_lines]
    out.append("Every source file in an engine area that the run never entered, wherever it sits. An area's rate above is a weighted average over its files, so a file at 0 %% inside a well-covered area appears in no other table here. Each row names the file's area and that area's rate: a zero inside an area already marked `gap` is expected, and a zero inside a `covered` one is the finding this section exists for.%s"
               % ("" if not options.untested_min_lines else
                  " Files of fewer than %s coverable lines are counted at the end rather than listed."
                  % _count(options.untested_min_lines)))
    out.append("")
    if named:
        out.append("| File | Coverable | Area | Area rate |")
        out.append("|---|---:|---|---:|")
        for path, totals, area in named:
            out.append("| %s | %s | %s | %s |" % (path, _count(totals.coverable), area.name, _rate(area.totals)))
        out.append("")
    if counted:
        lines_below = sum(totals.coverable for _, totals, _ in counted)
        if len(counted) == 1:
            out.append("One further wholly untested file, of %s coverable lines, is below the %s-line floor and is not listed."
                       % (_count(lines_below), _count(options.untested_min_lines)))
        else:
            out.append("Another %d wholly untested files of fewer than %s coverable lines each, %s lines between them, are not listed."
                       % (len(counted), _count(options.untested_min_lines), _count(lines_below)))
        out.append("")


def render(analysis, area_map, options):
    out = ["# Test coverage report", ""]
    out.append("Merged from %s. Rates of different runs are comparable only when the runs are of the same build: the collector's coverable-line counts move with the code." % _plural(analysis.inputs, "coverage report"))
    out.append("")
    out.append("**Line coverage says a line ran, not that anything checked its result.** Several D4 coverage scripts assert nothing at all, and a script that executes a path without checking what it produced still colours that path green. Read a high rate as *exercised*, never as *verified*.")
    out.append("")
    out.append("An area below %g %% line coverage is a **gap** unless it has fewer than %s coverable lines; below %g %% it is **thin**. Areas are listed worst first by uncovered line count, which is what makes the biggest holes the first ones a reader meets."
               % (options.gap_below, _count(options.min_lines), options.thin_below))
    out.append("")
    out.append("A file is named under **Wholly untested files** when the run never entered it at all, whatever its area's rate, down to %s coverable lines. That is a different question from an area's verdict and a different threshold: `--min-lines` is about whether a whole area is too small to call a gap, `--untested-min-lines` about whether one file is too small to name."
               % _count(options.untested_min_lines))
    out.append("")
    _incomplete_map_section(out, analysis)

    out.append("## Line coverage per assembly")
    out.append("")
    out.append("### Engine")
    out.append("")
    _assembly_table(out, area_map.lists["engine"], analysis.assemblies)
    out.append("### Also reported")
    out.append("")
    out.append("Libraries, the client stack and the hosts. These are rolled up per assembly only; the area map covers the engine.")
    out.append("")
    _assembly_table(out, area_map.lists["reported"], analysis.assemblies)

    out.append("## Engine areas")
    out.append("")
    if analysis.areas:
        out.append("| Area | Line coverage | Covered | Coverable | Uncovered | Verdict |")
        out.append("|---|---:|---:|---:|---:|---|")
        for area in analysis.areas:
            out.append("| %s | %s | %s | %s | %s | %s |" % (
                area.name, _rate(area.totals), _count(area.totals.covered),
                _count(area.totals.coverable), _count(area.totals.uncovered), area.verdict))
    else:
        out.append("No engine assembly in this run.")
    out.append("")

    _untested_files_section(out, analysis, options)

    if analysis.gaps:
        out.append("## Gaps, file by file")
        out.append("")
        for area in analysis.gaps:
            out.append("### %s - %s of %s lines" % (area.name, _rate(area.totals), _count(area.totals.coverable)))
            out.append("")
            out.append("| File | Line coverage | Covered | Coverable |")
            out.append("|---|---:|---:|---:|")
            for path, totals in area.files:
                out.append("| %s | %s | %s | %s |" % (path, _rate(totals), _count(totals.covered), _count(totals.coverable)))
            out.append("")

    if analysis.exclusions:
        out.append("## Excluded from the area rollup")
        out.append("")
        out.append("Counted by the collector, deliberately left out of the areas: not a gap, never ticketed.")
        out.append("")
        out.append("| Paths | Files | Coverable | Reason |")
        out.append("|---|---:|---:|---|")
        for exclusion in analysis.exclusions:
            reason = exclusion.entry["reason"]
            if exclusion.entry.get("ticket"):
                reason = "%s (%s)" % (reason, exclusion.entry["ticket"])
            out.append("| %s | %d | %s | %s |" % (", ".join("`%s`" % p for p in exclusion.entry["paths"]),
                                                  exclusion.files, _count(exclusion.totals.coverable), reason))
        out.append("")

    if analysis.outside_root:
        out.append("%s had a source path outside the repository root and were counted per assembly only." % _plural(analysis.outside_root, "source file"))
        out.append("")
    return "\n".join(out).rstrip("\n") + "\n"


def to_json(analysis, area_map, options):
    """The rolled-up numbers, for a later pass to diff against. No timestamps or paths."""
    return {
        "inputs": analysis.inputs,
        "thresholds": {"gap_below": options.gap_below, "thin_below": options.thin_below,
                       "min_lines": options.min_lines,
                       "untested_min_lines": options.untested_min_lines},
        "assemblies": [
            dict(name=name, group=group, covered=totals.covered, coverable=totals.coverable,
                 rate=totals.rate)
            for group in ("engine", "reported") for name in area_map.lists[group]
            for totals in [analysis.assemblies.get(name)] if totals is not None],
        "areas": [
            dict(name=area.name, verdict=area.verdict, covered=area.totals.covered,
                 coverable=area.totals.coverable, uncovered=area.totals.uncovered,
                 rate=area.totals.rate,
                 files=[dict(path=path, covered=totals.covered, coverable=totals.coverable,
                             rate=totals.rate) for path, totals in area.files])
            for area in analysis.areas],
        "excluded": [dict(paths=exclusion.entry["paths"], files=exclusion.files,
                          coverable=exclusion.totals.coverable)
                     for exclusion in analysis.exclusions],
        "unassigned": [dict(path=path, lines=lines) for path, lines in analysis.unassigned],
        "unlisted_assemblies": [dict(name=name, lines=lines) for name, lines in analysis.unlisted],
    }


# ---------------------------------------------------------------------------
# Command line

def main(argv=None):
    parser = argparse.ArgumentParser(description="Roll up Cobertura coverage reports into a Markdown coverage report.")
    parser.add_argument("inputs", nargs="+", metavar="PATH",
                       help="Cobertura files, or folders searched recursively for *.cobertura.xml")
    parser.add_argument("--areas", metavar="FILE", default=DEFAULT_AREAS,
                       help="the assembly lists, area map and exclusions (default: areas.json beside this script)")
    parser.add_argument("--repo-root", metavar="DIR",
                       help="what source paths are made relative to (default: the folder holding .git)")
    parser.add_argument("-o", "--output", metavar="FILE", help="write the report here instead of standard output")
    parser.add_argument("--gap-below", metavar="PCT", type=float, default=25.0,
                       help="below this line coverage an area is a gap (default: 25)")
    parser.add_argument("--thin-below", metavar="PCT", type=float, default=50.0,
                       help="below this, and not a gap, an area is thin (default: 50)")
    parser.add_argument("--min-lines", metavar="N", type=int, default=200,
                       help="an area with fewer coverable lines is never a gap (default: 200)")
    parser.add_argument("--untested-min-lines", metavar="N", type=int, default=20,
                       help="a wholly untested file with fewer coverable lines is counted rather than"
                            " listed under Wholly untested files; this is about one file, where"
                            " --min-lines is about a whole area (default: 20)")
    parser.add_argument("--json", metavar="FILE", help="also write the rolled-up numbers as JSON")
    parser.add_argument("--allow-unassigned", action="store_true",
                       help="print the report even when the map is incomplete (the exit code is still 1)")
    args = parser.parse_args(argv)

    for path in args.inputs:
        if not os.path.exists(path):
            parser.error("no such file or folder: %s" % path)
    if not 0 <= args.gap_below <= args.thin_below <= 100:
        parser.error("thresholds must satisfy 0 <= --gap-below <= --thin-below <= 100")
    if args.min_lines < 0:
        parser.error("--min-lines cannot be negative")
    if args.untested_min_lines < 0:
        parser.error("--untested-min-lines cannot be negative")
    if args.repo_root is not None and not os.path.isdir(args.repo_root):
        parser.error("not a directory: %s" % args.repo_root)
    try:
        area_map = load_map(args.areas)
    except MapError as error:
        parser.error(str(error))

    options = Options(args.gap_below, args.thin_below, args.min_lines, args.untested_min_lines)
    repo_root = args.repo_root or find_repo_root(os.path.dirname(os.path.abspath(__file__)))
    files = find_inputs(args.inputs)
    if not files:
        print("no input: no *.cobertura.xml under %s" % ", ".join(args.inputs))
        return 1
    try:
        merged = merge([parse_report(path, repo_root) for path in files])
    except ElementTree.ParseError as error:
        parser.error("not a Cobertura report: %s" % error)
    except OSError as error:
        parser.error("cannot read a coverage report: %s" % error)
    except MergeError as error:
        # Mixed builds: say which class disagreed and produce no numbers at all.
        print(str(error))
        print("%s read, not merged" % _plural(len(files), "coverage report"))
        return 1

    analysis = analyse(merged, area_map, options)
    analysis.inputs = len(files)
    for path, lines in analysis.unassigned:
        print("unassigned: %s (%d lines)" % (path, lines))
    for name, lines in analysis.unlisted:
        print("unlisted assembly: %s (%d lines)" % (name, lines))
    if not analysis.assemblies:
        print("no listed assembly: none of the %d assemblies in the input is in %s"
              % (len(merged), args.areas))

    # Both holes suppress the report, not just the unassigned one: an assembly on no list
    # is missing from every table and total below exactly as an unclaimed file is, so the
    # rollup would read as if its subsystem were fine. --allow-unassigned lifts both, and
    # the report it then prints names the holes in its own first section.
    incomplete = bool(analysis.unassigned or analysis.unlisted)
    if analysis.assemblies and (not incomplete or args.allow_unassigned):
        report = render(analysis, area_map, options)
        if args.output:
            # CRLF, as the repository's tracked text files have: a published report is one.
            with open(args.output, "w", encoding="utf-8", newline="\r\n") as stream:
                stream.write(report)
        else:
            sys.stdout.write(report)
        if args.json:
            with open(args.json, "w", encoding="utf-8", newline="\r\n") as stream:
                json.dump(to_json(analysis, area_map, options), stream, indent=2, sort_keys=False)
                stream.write("\n")

    print("%s merged, %d of %d engine assemblies measured, %s, %d gaps, %d unassigned files, %d unlisted assemblies"
          % (_plural(analysis.inputs, "report"),
             sum(1 for name in area_map.lists["engine"] if name in analysis.assemblies),
             len(area_map.lists["engine"]), _plural(len(analysis.areas), "area"),
             len(analysis.gaps), len(analysis.unassigned), len(analysis.unlisted)))
    return 1 if incomplete or not analysis.assemblies else 0


if __name__ == "__main__":
    sys.exit(main())
