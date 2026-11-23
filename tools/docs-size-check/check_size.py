#!/usr/bin/env python3
"""Check that no architecture documentation page has grown past its word limit.

Python 3 standard library only, no network. See README.md in this folder for the
rules and for what to do when the check fails: split the page, do not raise the
limit.
"""

import argparse
import re
import sys
from pathlib import Path

# ---------------------------------------------------------------------------
# Configuration. Each of these values lives here and nowhere else.

# Words of prose one page of the checked set may hold. Three documents state the
# rule and name this number - AGENTS.md, which names this symbol and file as well,
# tickets/rules/10-dataphor.md and WS8 of docs/modernization-plan.md - so a change
# here has to be made in all three. Each names this folder, so `grep -rn
# docs-size-check` finds them.
LIMIT = 1500

# The pages that are checked, as repository-relative glob patterns with forward
# slashes. A pattern with no glob character must exist, so that a page renamed
# without updating this list cannot drop out of the gate unnoticed. Nothing else
# under docs/ is in the set; README.md lists the documents that are deliberately
# outside it and why.
CHECKED = (
    "docs/architecture.md",
    "docs/architecture/**/*.md",
)

# Pages allowed over the limit, each mapped to the reason it is allowed. Meant to
# stay empty: the answer to a page that outgrows the limit is to split it.
EXCEPTIONS = {}

# ---------------------------------------------------------------------------
# What counts as prose

# The fence rule is the link checker's (tools/docs-link-check/check_links.py),
# copied rather than imported so that each tool stays one standalone file: a line
# indented at most three spaces whose content starts with three or more backticks
# or tildes opens a code block, and a line of the same character repeated at
# least as many times and nothing else closes it. The guard on the info string
# matters here as much as there - a run of inline code mistaken for a fence would
# swallow the rest of the page and *under*count it, which is the one failure mode
# that lets an oversized page pass. Unlike the link checker this ignores the
# list-item exception, so a fence indented four spaces or more inside a list
# counts as prose: over-counting can only fail a page early, never let one pass.
# HTML comment text is blanked before a line is tested, for the same reason and in
# the same order as there: a line of backticks inside an HTML comment is not a
# fence, and taking it for one would swallow the rest of the page from the count.
# NOTE: _FENCE_OPEN and _strip_comments are copies, not imports; if the link checker's rules for
# fences or comments change, change these with them, or the two tools stop agreeing on what is prose.
_FENCE_OPEN = re.compile(r"^(?P<indent>[ \t]*)(?P<fence>`{3,}|~{3,})(?P<info>.*)$")


def _normalise(text):
    if text.startswith("\ufeff"):
        text = text[1:]
    return text.replace("\r\n", "\n").replace("\r", "\n")


def _indent_width(line):
    width = 0
    for ch in line:
        if ch == " ":
            width += 1
        elif ch == "\t":
            width += 4 - width % 4
        else:
            break
    return width


def _strip_comments(line, in_comment):
    """Blank out HTML comment text; returns (line, still_in_comment)."""
    out = []
    pos = 0
    while True:
        if in_comment:
            end = line.find("-->", pos)
            if end < 0:
                return "".join(out), True
            pos = end + 3
            in_comment = False
        start = line.find("<!--", pos)
        if start < 0:
            out.append(line[pos:])
            return "".join(out), False
        out.append(line[pos:start])
        pos = start + 4
        in_comment = True


def prose_words(text):
    """The whitespace-separated tokens of a Markdown text outside fenced code blocks.

    Counted as `wc -w` counts, so Markdown punctuation counts too: this is a
    budget for how long a page may get, not a measurement of its prose. Fenced
    code and HTML comment text are not prose and are not counted.
    """
    words = 0
    fence = None
    in_comment = False
    for raw in _normalise(text).split("\n"):
        if fence is not None:
            stripped = raw.strip()
            if len(stripped) >= fence[1] and stripped == fence[0] * len(stripped):
                fence = None
            continue
        line, in_comment = _strip_comments(raw, in_comment)
        opener = _FENCE_OPEN.match(line)
        if opener and _indent_width(opener.group("indent")) <= 3 and not (
            opener.group("fence")[0] == "`" and "`" in opener.group("info")
        ):
            fence = (opener.group("fence")[0], len(opener.group("fence")))
            continue
        words += len(line.split())
    return words


# ---------------------------------------------------------------------------
# Checking

# The finding kinds that fail the check. An excepted page is reported and does not.
ERROR_KINDS = ("over", "missing", "unreadable", "empty")


class Finding:
    def __init__(self, path, kind, message):
        self.path, self.kind, self.message = path, kind, message

    @property
    def is_error(self):
        return self.kind in ERROR_KINDS

    def __str__(self):
        return "%s: %s" % (self.path, self.message) if self.path else self.message


class Report:
    def __init__(self):
        self.findings = []
        self.files = 0

    def count(self, kind):
        return sum(1 for finding in self.findings if finding.kind == kind)

    @property
    def errors(self):
        return sum(1 for finding in self.findings if finding.is_error)


def repo_root():
    """The repository root, derived from this script's own path.

    The script is tools/docs-size-check/check_size.py, so the root is two folders
    above its own: the tool runs from any working directory and needs no .git to
    find itself.
    """
    return Path(__file__).resolve().parents[2]


def _read_or_none(path):
    try:
        with open(path, encoding="utf-8-sig", newline="") as stream:
            return stream.read()
    except (OSError, UnicodeDecodeError):
        return None


def check(root=None, limit=LIMIT, checked=CHECKED, exceptions=None):
    """Check every page of `checked` below `root`; returns a Report of the findings."""
    root = Path(root) if root is not None else repo_root()
    exceptions = EXCEPTIONS if exceptions is None else exceptions
    report = Report()
    pages = {}
    for pattern in checked:
        if any(ch in pattern for ch in "*?["):
            for path in root.glob(pattern):
                if path.is_file():
                    pages.setdefault(path.relative_to(root).as_posix(), path)
        else:
            path = root.joinpath(*pattern.split("/"))
            if path.is_file():
                pages.setdefault(pattern, path)
            else:
                report.findings.append(Finding(pattern, "missing", "missing"))
    for shown in sorted(pages):
        report.files += 1
        text = _read_or_none(pages[shown])
        if text is None:
            report.findings.append(Finding(shown, "unreadable", "cannot read as UTF-8"))
            continue
        words = prose_words(text)
        if words <= limit:
            continue
        reason = exceptions.get(shown)
        if reason:
            report.findings.append(Finding(shown, "exception", "%d words (exception: %s)" % (words, reason)))
        else:
            report.findings.append(Finding(shown, "over", "%d words (limit %d)" % (words, limit)))
    report.findings.sort(key=lambda finding: finding.path)
    if not report.files:
        report.findings.append(Finding("", "empty", "checked set matched no files"))
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description="Check the word count of the architecture documentation pages.")
    parser.add_argument("--root", metavar="DIR", help="resolve the checked set against DIR instead of the repository root")
    parser.add_argument("--limit", type=int, metavar="N", help="word limit to apply instead of %d (a what-if; the gate uses the default)" % LIMIT)
    args = parser.parse_args(argv)
    if args.root is not None and not Path(args.root).is_dir():
        parser.error("not a directory: %s" % args.root)
    if args.limit is not None and args.limit < 0:
        parser.error("--limit must not be negative: %d" % args.limit)
    report = check(root=args.root, limit=LIMIT if args.limit is None else args.limit)
    for finding in report.findings:
        print(finding)
    print("%d files checked, %d over the limit, %d excepted" % (report.files, report.count("over"), report.count("exception")))
    return 1 if report.errors else 0


if __name__ == "__main__":
    sys.exit(main())
