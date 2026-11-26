#!/usr/bin/env python3
"""Check the links and image references in Markdown documentation.

Python 3 standard library only. See README.md in this folder for the rules.
`slugify` and `heading_slugs` were written to be imported, so that a documentation
generator and this checker agree on GitHub's heading anchors; the converter that
did so has been deleted along with its sources.
"""

import argparse
import csv
import html
import os
import posixpath
import re
import sys
import unicodedata
from urllib.parse import unquote

EXTERNAL_SCHEMES = ("http", "https", "mailto", "ftp")

# ---------------------------------------------------------------------------
# GitHub heading slugs (the github-slugger algorithm)

def slugify(text):
    """GitHub's slug for one heading's rendered text, before de-duplication."""
    kept = []
    for ch in text.strip().lower():
        if ch in " -_" or unicodedata.category(ch)[0] in "LMN":
            kept.append(ch)
    return "".join(kept).replace(" ", "-")


class Slugger:
    """Assigns unique slugs within one document, as github-slugger does."""

    def __init__(self):
        self._occurrences = {}

    def slug(self, text):
        original = slugify(text)
        result = original
        while result in self._occurrences:
            self._occurrences[original] += 1
            result = "%s-%d" % (original, self._occurrences[original])
        self._occurrences[result] = 0
        return result


def heading_slugs(markdown_text):
    """The anchors of a document's headings, in document order, duplicates resolved."""
    slugger = Slugger()
    return [slugger.slug(heading_text(raw)) for raw in _parse(_normalise(markdown_text))[1]]


_CODE_RUN = re.compile(r"`+")
_ESCAPABLE = re.compile(r"\\([!-/:-@\[-`{-~])")
_AUTOLINK = re.compile(r"(?<!\\)<([A-Za-z][A-Za-z0-9+.-]{1,31}:[^\s<>]*)>")
_HTML_TAG = re.compile(r"(?<!\\)</?[A-Za-z][A-Za-z0-9-]*(?:\s[^<>]*)?/?>")
_UNDERSCORE_EMPHASIS = re.compile(r"(?<!\w)(_+)(?=\S)(.+?)(?<=\S)\1(?!\w)")


def heading_text(raw):
    """The rendered text of a heading's Markdown source, as GitHub slugs it."""
    stash = []

    def keep(text):
        stash.append(text)
        return "\x00%d\x00" % (len(stash) - 1)

    text = _replace_code_spans(raw, lambda content: keep(_code_span_text(content)))
    text = _ESCAPABLE.sub(lambda m: keep(m.group(1)), text)
    text = re.sub(r"!\[[^\]]*\](?:\([^)]*\)|\[[^\]]*\])", "", text)
    text = re.sub(r"\[([^\]]*)\](?:\([^)]*\)|\[[^\]]*\])", r"\1", text)
    text = _AUTOLINK.sub(r"\1", text)
    text = _HTML_TAG.sub("", text)
    previous = None
    while previous != text:
        previous = text
        text = _UNDERSCORE_EMPHASIS.sub(r"\2", text)
    text = html.unescape(text)
    return re.sub("\x00(\\d+)\x00", lambda m: stash[int(m.group(1))], text)


def _code_span_text(content):
    content = content.replace("\n", " ")
    if len(content) >= 2 and content[0] == " " and content[-1] == " " and content.strip():
        content = content[1:-1]
    return content


def _replace_code_spans(text, replace):
    """Replace each inline code span (backticks included) with replace(content)."""
    out = []
    pos = 0
    runs = [m for m in _CODE_RUN.finditer(text) if not _is_escaped(text, m.start())]
    i = 0
    while i < len(runs):
        opener = runs[i]
        closer = next((j for j in range(i + 1, len(runs)) if len(runs[j].group()) == len(opener.group())), None)
        if closer is None:
            i += 1
            continue
        out.append(text[pos:opener.start()])
        out.append(replace(text[opener.end():runs[closer].start()]))
        pos = runs[closer].end()
        i = closer + 1
    out.append(text[pos:])
    return "".join(out)


def _is_escaped(text, index):
    backslashes = 0
    while index - backslashes - 1 >= 0 and text[index - backslashes - 1] == "\\":
        backslashes += 1
    return backslashes % 2 == 1


# ---------------------------------------------------------------------------
# Block structure: which lines are prose, which are headings

_FENCE_OPEN = re.compile(r"^(?P<indent>[ \t]*)(?P<fence>`{3,}|~{3,})(?P<info>.*)$")
_ATX = re.compile(r"^ {0,3}(#{1,6})(?:[ \t]+(.*?))?[ \t]*$")
_ATX_CLOSE = re.compile(r"(?:^|[ \t]+)#+$")
_SETEXT = re.compile(r"^ {0,3}(?:=+|-+)[ \t]*$")
_LIST_ITEM = re.compile(r"^[ \t]*(?:[-+*]|\d{1,9}[.)])(?:[ \t]|$)")
_NOT_PLAIN_PARAGRAPH = re.compile(r"^[ \t]*(?:>|\||<|[-+*][ \t]|\d{1,9}[.)][ \t])")


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


def _parse(text):
    """Split normalised Markdown into prose blocks and heading sources.

    Returns (blocks, headings): blocks is a list of (first_line_number, text)
    for runs of consecutive lines outside code blocks and HTML comments;
    headings is the raw source text of each heading, in order.
    """
    blocks = []
    headings = []
    block_lines = []
    block_start = 0
    paragraph = []
    paragraph_plain = False
    fence = None
    in_comment = False
    in_list = False
    previous_blank = True

    def flush():
        nonlocal block_lines
        if block_lines:
            blocks.append((block_start, "\n".join(block_lines)))
            block_lines = []

    for number, raw in enumerate(text.split("\n"), 1):
        if fence is not None:
            stripped = raw.strip()
            if len(stripped) >= fence[1] and stripped == fence[0] * len(stripped):
                fence = None
            previous_blank = False
            continue
        line, in_comment = _strip_comments(raw, in_comment)
        if not line.strip():
            flush()
            paragraph = []
            previous_blank = True
            continue
        indent = _indent_width(line)
        is_item = bool(_LIST_ITEM.match(line))
        if indent == 0 and previous_blank and not is_item:
            in_list = False
        if indent >= 4 and not in_list and not paragraph:
            flush()
            previous_blank = False
            continue
        opener = _FENCE_OPEN.match(line)
        if opener and (in_list or _indent_width(opener.group("indent")) <= 3) and not (opener.group("fence")[0] == "`" and "`" in opener.group("info")):
            flush()
            paragraph = []
            fence = (opener.group("fence")[0], len(opener.group("fence")))
            previous_blank = False
            continue
        if not block_lines:
            block_start = number
        block_lines.append(line)
        previous_blank = False
        atx = _ATX.match(line)
        if atx:
            content = atx.group(2) or ""
            headings.append(_ATX_CLOSE.sub("", content).strip())
            paragraph = []
            continue
        if paragraph and paragraph_plain and _SETEXT.match(line):
            headings.append(" ".join(p.strip() for p in paragraph))
            paragraph = []
            continue
        if is_item:
            in_list = True
        if not paragraph:
            paragraph_plain = not _NOT_PLAIN_PARAGRAPH.match(line) and not in_list
        paragraph.append(line)
    flush()
    return blocks, headings


# ---------------------------------------------------------------------------
# Link extraction

_REFERENCE_DEFINITION = re.compile(r"^ {0,3}\[(?!\^)(?:[^\]\\]|\\.)+\]:[ \t]*(<[^>\n]*>|\S+)", re.MULTILINE)
_EMAIL_AUTOLINK = re.compile(r"(?<!\\)<([A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)*)>")
_HTML_ATTRIBUTE = re.compile(r"""(?<![\w-])(href|src|id|name)\s*=\s*(?:"([^"]*)"|'([^']*)')""", re.IGNORECASE)
_TITLE = re.compile(r"""[ \t\n]+(?:"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'|\((?:[^()\\]|\\.)*\))""")


def _unescape_destination(dest):
    return html.unescape(_ESCAPABLE.sub(r"\1", dest))


def _inline_destinations(text):
    """(offset, destination) for each inline link or image: `](dest "title")`."""
    for m in re.finditer(r"\]\(", text):
        if _is_escaped(text, m.start()):
            continue
        pos = m.end()
        while pos < len(text) and text[pos] in " \t\n":
            pos += 1
        if pos < len(text) and text[pos] == "<":
            end = pos + 1
            while end < len(text) and text[end] not in ">\n" and not (text[end] == "<" and not _is_escaped(text, end)):
                end += 1 if text[end] != "\\" else 2
            if end >= len(text) or text[end] != ">":
                continue
            dest = text[pos + 1:end]
            pos = end + 1
        else:
            end = pos
            depth = 0
            while end < len(text):
                ch = text[end]
                if ch == "\\":
                    end += 2
                    continue
                if ch in " \t\n" or ord(ch) < 32:
                    break
                if ch == "(":
                    depth += 1
                elif ch == ")":
                    if depth == 0:
                        break
                    depth -= 1
                end += 1
            dest = text[pos:end]
            pos = end
        title = _TITLE.match(text, pos)
        if title:
            pos = title.end()
        while pos < len(text) and text[pos] in " \t\n":
            pos += 1
        if pos < len(text) and text[pos] == ")":
            yield m.start(), _unescape_destination(dest)


def extract_links(text):
    """Links of a normalised Markdown text: (line, destination, kind) plus anchor ids.

    kind is "link" for Markdown and HTML references, "autolink" for <scheme:...>.
    Returns (links, html_ids).
    """
    links = []
    ids = []
    for start, block in _parse(text)[0]:
        masked = _replace_code_spans(block, lambda content: " " * (len(content) + 2))

        def line_of(offset):
            return start + masked.count("\n", 0, offset)

        for offset, dest in _inline_destinations(masked):
            links.append((line_of(offset), dest, "link"))
        for m in _REFERENCE_DEFINITION.finditer(masked):
            dest = m.group(1)
            if dest.startswith("<"):
                dest = dest[1:-1]
            links.append((line_of(m.start()), _unescape_destination(dest), "link"))
        for m in _AUTOLINK.finditer(masked):
            links.append((line_of(m.start()), m.group(1), "autolink"))
        for m in _EMAIL_AUTOLINK.finditer(masked):
            links.append((line_of(m.start()), "mailto:" + m.group(1), "autolink"))
        for tag in _HTML_TAG.finditer(masked):
            for attr in _HTML_ATTRIBUTE.finditer(tag.group()):
                value = html.unescape(attr.group(2) if attr.group(2) is not None else attr.group(3))
                if attr.group(1).lower() in ("href", "src"):
                    links.append((line_of(tag.start() + attr.start()), value, "link"))
                else:
                    ids.append(value)
    links.sort(key=lambda link: link[0])
    return links, ids


# ---------------------------------------------------------------------------
# Checking

class Finding:
    def __init__(self, path, line, kind, message):
        self.path, self.line, self.kind, self.message = path, line, kind, message

    def __str__(self):
        return "%s:%d: %s: %s" % (self.path, self.line, self.kind, self.message)


class Report:
    def __init__(self):
        self.findings = []
        self.files = 0
        self.links = 0

    def count(self, kind):
        return sum(1 for f in self.findings if f.kind == kind)


class PendingFileError(Exception):
    pass


def read_pending(path):
    """The `target` column of a tab-separated pending-links file."""
    try:
        with open(path, encoding="utf-8-sig", newline="") as stream:
            rows = list(csv.reader(stream, delimiter="\t", quoting=csv.QUOTE_NONE))
    except OSError as error:
        raise PendingFileError("cannot read pending file %s: %s" % (path, error.strerror))
    if not rows or "target" not in rows[0]:
        raise PendingFileError("pending file %s has no 'target' column in its header row" % path)
    column = rows[0].index("target")
    return [row[column] for row in rows[1:] if len(row) > column and row[column]]


def find_repo_root(path):
    current = os.path.abspath(path)
    while True:
        if os.path.exists(os.path.join(current, ".git")):
            return current
        parent = os.path.dirname(current)
        if parent == current:
            return None
        current = parent


def _is_within(path, folder):
    try:
        return os.path.commonpath([path, folder]) == folder
    except ValueError:
        return False


class _Checker:
    def __init__(self, roots, pending, repo_root):
        # realpath gives each root its on-disk letter case on Windows; a root typed as `Docs`
        # would otherwise make every relative link below it a wrong-case error.
        self.roots = [os.path.realpath(root) for root in roots]
        self.repo_root = os.path.realpath(repo_root) if repo_root else find_repo_root(self.roots[0])
        self.pending = {os.path.normpath(os.path.join(root, target)) for root in self.roots for target in pending}
        self.listings = {}
        self.anchors = {}
        self.report = Report()

    def display(self, path):
        base = self.repo_root if self.repo_root and _is_within(path, self.repo_root) else os.getcwd()
        try:
            return os.path.relpath(path, base).replace(os.sep, "/")
        except ValueError:
            return path.replace(os.sep, "/")

    def listdir(self, folder):
        if folder not in self.listings:
            try:
                self.listings[folder] = set(os.listdir(folder))
            except OSError:
                self.listings[folder] = None
        return self.listings[folder]

    def locate(self, boundary, parts):
        """Walk parts below boundary with exact case; returns (path, problem)."""
        current = boundary
        for index, part in enumerate(parts):
            listing = self.listdir(current)
            if listing is None:
                return None, "missing"
            if part not in listing:
                actual = [name for name in listing if name.lower() == part.lower()]
                if actual:
                    return None, "wrong case: '%s' is '%s' on disk (in %s)" % (part, actual[0], "/".join(parts))
                return None, "missing"
            current = os.path.join(current, part)
        return current, None

    def file_anchors(self, path):
        """The anchors of a Markdown file, or None when it cannot be read as UTF-8."""
        if path not in self.anchors:
            text = _read_or_none(path)
            self.anchors[path] = None if text is None else set(heading_slugs(text)) | set(extract_links(text)[1])
        return self.anchors[path]

    def run(self):
        for root in self.roots:
            boundary = self.repo_root if self.repo_root and _is_within(root, self.repo_root) else root
            for folder, subfolders, files in os.walk(root):
                subfolders.sort()
                for name in sorted(files):
                    if name.lower().endswith(".md"):
                        self.check_file(os.path.join(folder, name), boundary)
        self.report.findings.sort(key=lambda f: (f.path, f.line))
        return self.report

    def check_file(self, path, boundary):
        self.report.files += 1
        shown = self.display(path)
        text = _read_or_none(path)
        if text is None:
            self.anchors[path] = None
            self.report.findings.append(Finding(shown, 1, "error", "cannot read as UTF-8"))
            return
        links, ids = extract_links(text)
        self.anchors.setdefault(path, set(heading_slugs(text)) | set(ids))
        folder = os.path.relpath(os.path.dirname(path), boundary).replace(os.sep, "/")
        for line, dest, _kind in links:
            self.report.links += 1
            problem = self.check_destination(path, folder, boundary, dest)
            if problem:
                self.report.findings.append(Finding(shown, line, problem[0], problem[1]))

    def check_destination(self, path, folder, boundary, dest):
        scheme = re.match(r"^([A-Za-z][A-Za-z0-9+.-]*):", dest)
        if scheme:
            if scheme.group(1).lower() in EXTERNAL_SCHEMES:
                return ("external", dest)
            return ("error", "unsupported link scheme: %s" % dest)
        target, _, fragment = dest.partition("#")
        target = unquote(target.partition("?")[0])
        fragment = unquote(fragment)
        if not target:
            resolved = path
        else:
            relative = target.lstrip("/") if target.startswith("/") else posixpath.join(folder, target)
            relative = posixpath.normpath(relative) if relative else "."
            if relative == ".." or relative.startswith("../"):
                return ("error", "outside the repository: %s" % dest)
            parts = [] if relative == "." else relative.split("/")
            resolved, problem = self.locate(boundary, parts)
            if problem == "missing":
                wanted = os.path.normpath(os.path.join(boundary, *parts))
                if wanted in self.pending:
                    return ("pending", dest)
                return ("error", "missing file: %s" % dest)
            if problem:
                return ("error", "%s: %s" % (problem, dest))
        if not fragment:
            return None
        if os.path.isdir(resolved):
            if "README.md" not in (self.listdir(resolved) or ()):
                return ("error", "anchor on a directory without README.md: %s" % dest)
            resolved = os.path.join(resolved, "README.md")
        if not resolved.endswith(".md"):
            return None
        anchors = self.file_anchors(resolved)
        if anchors is None:
            return ("error", "cannot read %s as UTF-8 to check '#%s': %s" % (self.display(resolved), fragment, dest))
        if fragment not in anchors:
            return ("error", "missing anchor '#%s' in %s: %s" % (fragment, self.display(resolved), dest))
        return None


def _read_or_none(path):
    try:
        with open(path, encoding="utf-8-sig", newline="") as stream:
            return _normalise(stream.read())
    except (OSError, UnicodeDecodeError):
        return None


def check(roots, pending=(), repo_root=None):
    """Check every *.md under roots; pending is a list of root-relative targets."""
    return _Checker(roots, pending, repo_root).run()


def main(argv=None):
    parser = argparse.ArgumentParser(description="Check links and image references in Markdown files.")
    parser.add_argument("roots", nargs="*", default=["docs"], metavar="ROOT", help="folders to scan (default: docs)")
    parser.add_argument("--pending", metavar="FILE", help="tab-separated file whose 'target' column lists links allowed to be missing")
    parser.add_argument("--list-external", action="store_true", help="print each external link")
    args = parser.parse_args(argv)
    for root in args.roots:
        if not os.path.isdir(root):
            parser.error("not a directory: %s" % root)
    try:
        pending = read_pending(args.pending) if args.pending else []
    except PendingFileError as error:
        parser.error(str(error))
    report = check(args.roots, pending)
    for finding in report.findings:
        if finding.kind != "external" or args.list_external:
            print(finding)
    errors = report.count("error")
    print("%d files scanned, %d links checked, %d errors, %d pending, %d external" % (report.files, report.links, errors, report.count("pending"), report.count("external")))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
