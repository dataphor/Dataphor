#!/usr/bin/env python3
"""Convert the AsciiDoc documentation under Documentation/ to GitHub-flavoured Markdown.

Route: Asciidoctor (DocBook 5 backend) -> pandoc's DocBook reader -> the pass over pandoc's
JSON syntax tree in this file -> pandoc's gfm writer. The DocBook -> Markdown stage also runs
on its own (`convert.py docbook ...`) for DocBook that did not come from AsciiDoc.
See README.md in this folder for the commands, the inputs and the three tables.
"""

import argparse
import collections
import concurrent.futures
import hashlib
import io
import json
import os
import posixpath
import re
import shutil
import subprocess
import sys
import tempfile
import time
from urllib.parse import unquote

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, os.path.join(REPO, "tools", "docs-link-check"))
import check_links  # noqa: E402  -- the heading-slug rule and link extraction the checker uses

PANDOC_PIN = (3, 14)
NPM_PACKAGES = ("@asciidoctor/cli", "@asciidoctor/core", "@asciidoctor/docbook-converter")
README_HINT = "see tools/docs-convert/README.md, section Prerequisites"

SOURCE_ROOT = os.path.join(REPO, "Documentation")
PAGES_TSV = os.path.join(HERE, "pages.tsv")
CODE_TSV = os.path.join(HERE, "code-blocks.tsv")
XREF_TSV = os.path.join(HERE, "xref-fixes.tsv")
LEGACY_TSV = os.path.join(HERE, "legacy-links.tsv")
INDEX_EXTRA = os.path.join(HERE, "index-extra.md")

# docs/README.md is the introduction (README.adoc) followed by the GitBook table of contents
# (SUMMARY.adoc) under a "Contents" heading, then index-extra.md.
INDEX_INTRO = "README.adoc"
INDEX_CONTENTS = "SUMMARY.adoc"
INDEX_OUTPUT = "README.md"

EXTERNAL_SCHEMES = ("http", "https", "mailto", "ftp")
ALERT_CLASSES = ("note", "tip", "important", "warning", "caution")
LANGUAGES = ("d4", "sql", "xml", "bnf", "csharp", "text")
RASTER_TO_PNG = (".bmp", ".gif", ".jpg", ".jpeg")
IMAGE_EXTENSIONS = RASTER_TO_PNG + (".png", ".svg")

# Legacy DocBook reference pages, and the page under docs/ that docs-convert-legacy-reference
# creates for each. Names keep the legacy spelling and case.
LEGACY_RULES = (
    (re.compile(r"^O-(.+)\.html$"), "reference/system-library/operators/{0}.md"),
    (re.compile(r"^T-(.+)\.html$"), "reference/system-library/types/{0}.md"),
    (re.compile(r"^DR(.+)Library\.html$"), "reference/libraries/{0}.md"),
    # One link in the whole corpus, and no per-node page will ever exist: the element reference was
    # generated from the client's node interfaces and no copy of that output survives. Point at the index.
    (re.compile(r"^dilrefAlphora\.Dataphor\.Frontend\.Client\.I.+\.html$"), "reference/frontend-nodes/README.md"),
    (re.compile(r"^Alphora\.Dataphor\.DAE\.Client\..+\.html$"), "reference/data-access-components.md"),
)


class ConvertError(Exception):
    pass


def fail(message):
    raise ConvertError(message)


# ---------------------------------------------------------------------------
# Names

def page_name(base):
    """The kebab-case file name rule for pages and images: `SQLDevice` -> `sql-device`."""
    name = re.sub(r"[^A-Za-z0-9]+", "-", base)
    name = re.sub(r"([a-z0-9])([A-Z])", r"\1-\2", name)
    name = re.sub(r"([A-Z])([A-Z][a-z])", r"\1-\2", name)
    return re.sub(r"-+", "-", name.lower()).strip("-")


def image_name(file_name):
    stem, ext = os.path.splitext(file_name)
    ext = ext.lower()
    return page_name(stem) + (".png" if ext in RASTER_TO_PNG else ext)


def legacy_target(file_name):
    for pattern, target in LEGACY_RULES:
        match = pattern.match(file_name)
        if match:
            return target.format(*match.groups())
    return None


# A legacy page name, in any letter case: one source link is spelled `DFRTagReference.HTML` and
# another `DDGP2BasicConnectivity.htm`.
LEGACY_PAGE = re.compile(r"\.html?$", re.IGNORECASE)


# ---------------------------------------------------------------------------
# Tab-separated tables (header row, no quoting, UTF-8, LF)

def read_tsv(path, columns):
    with open(path, encoding="utf-8", newline="") as stream:
        lines = stream.read().split("\n")
    header = lines[0].rstrip("\r").split("\t")
    missing = [c for c in columns if c not in header]
    if missing:
        fail("%s: header row lacks column(s) %s" % (os.path.relpath(path, REPO), ", ".join(missing)))
    rows = []
    for number, line in enumerate(lines[1:], 2):
        line = line.rstrip("\r")
        if not line.strip():
            continue
        cells = line.split("\t")
        cells += [""] * (len(header) - len(cells))
        row = dict(zip(header, cells))
        row["_line"] = number
        rows.append(row)
    return rows


def tsv_cell(text):
    return re.sub(r"[\t\r\n]+", " ", text)


def write_tsv(path, columns, rows):
    lines = ["\t".join(columns)] + ["\t".join(tsv_cell(str(row[c])) for c in columns) for row in rows]
    data = ("\n".join(lines) + "\n").encode("utf-8")
    with open(path, "wb") as stream:
        stream.write(data)


# ---------------------------------------------------------------------------
# Tools

def tool_version_ok(name, found, pinned, allow, report):
    report.tools.append("%s %s" % (name, ".".join(map(str, found))))
    if tuple(found[:2]) != tuple(pinned[:2]):
        message = "%s %s does not match the pinned %s (%s); --allow-tool-versions overrides" % (
            name, ".".join(map(str, found)), ".".join(map(str, pinned)), README_HINT)
        if not allow:
            fail(message)
        report.add("tools", message)


def parse_version(text):
    return tuple(int(part) for part in re.findall(r"\d+", text))


def find_pandoc(explicit, allow, report):
    pandoc = explicit or shutil.which("pandoc")
    if not pandoc or not os.path.exists(pandoc) and not shutil.which(pandoc):
        fail("pandoc not found (pass --pandoc PATH or put pandoc %d.%d on PATH; %s)" % (PANDOC_PIN + (README_HINT,)))
    try:
        out = subprocess.run([pandoc, "--version"], capture_output=True, check=True).stdout.decode("utf-8", "replace")
    except (OSError, subprocess.CalledProcessError) as error:
        fail("cannot run pandoc at %s: %s (%s)" % (pandoc, error, README_HINT))
    match = re.match(r"pandoc(?:\.exe)? ([\d.]+)", out)
    if not match:
        fail("cannot read the pandoc version from: %s" % out.splitlines()[0])
    tool_version_ok("pandoc", parse_version(match.group(1)), PANDOC_PIN, allow, report)
    return pandoc


def find_asciidoctor(allow, report):
    node = shutil.which("node")
    if not node:
        fail("Node.js not found on PATH (%s)" % README_HINT)
    with open(os.path.join(HERE, "package.json"), encoding="utf-8") as stream:
        pins = json.load(stream)["dependencies"]
    for package in NPM_PACKAGES:
        manifest = os.path.join(HERE, "node_modules", *package.split("/"), "package.json")
        if not os.path.exists(manifest):
            fail("%s is not installed: run `npm ci --prefix tools/docs-convert` (%s)" % (package, README_HINT))
        with open(manifest, encoding="utf-8") as stream:
            installed = json.load(stream)["version"]
        tool_version_ok(package, parse_version(installed), parse_version(pins[package]), allow, report)
    cli = os.path.join(HERE, "node_modules", "@asciidoctor", "cli", "bin", "asciidoctor")
    out = subprocess.run([node, cli, "--version"], capture_output=True).stdout.decode("utf-8", "replace")
    report.tools.append(re.sub(r"\s*\[.*?\]", "", out.splitlines()[0]).strip() if out.strip() else "asciidoctor: no version output")
    node_version = subprocess.run([node, "--version"], capture_output=True).stdout.decode().strip()
    report.tools.append("node %s" % node_version)
    return node, cli


def run_pandoc(pandoc, args, data):
    process = subprocess.run([pandoc] + args, input=data, capture_output=True)
    if process.returncode:
        fail("pandoc %s failed: %s" % (" ".join(args), process.stderr.decode("utf-8", "replace").strip()))
    return process.stdout, process.stderr.decode("utf-8", "replace")


def parallel(function, items):
    with concurrent.futures.ThreadPoolExecutor(max_workers=min(16, (os.cpu_count() or 2) * 2)) as pool:
        return list(pool.map(function, items))


# ---------------------------------------------------------------------------
# Pandoc syntax tree helpers. Nodes are {"t": type, "c": content}; lists hold blocks or inlines.

def node(kind, content=None):
    return {"t": kind} if content is None else {"t": kind, "c": content}


def iter_nodes(value):
    """Every node below value, in document order (a node before its children)."""
    if isinstance(value, dict):
        if "t" in value:
            yield value
        if "c" in value:
            yield from iter_nodes(value["c"])
    elif isinstance(value, list):
        for item in value:
            yield from iter_nodes(item)


def rewrite(value, function):
    """Bottom-up rewrite: function(node) returns a node, None (delete) or a list (splice)."""
    if isinstance(value, list):
        out = []
        for item in value:
            item = rewrite(item, function)
            if isinstance(item, dict) and "t" in item:
                result = function(item)
                if isinstance(result, list):
                    out.extend(result)
                elif result is not None:
                    out.append(result)
            else:
                out.append(item)
        return out
    if isinstance(value, dict) and "c" in value:
        value["c"] = rewrite(value["c"], function)
    return value


def stringify(inlines):
    parts = []
    for item in iter_nodes(inlines):
        kind = item["t"]
        if kind == "Str":
            parts.append(item["c"])
        elif kind in ("Space", "SoftBreak", "LineBreak"):
            parts.append(" ")
        elif kind in ("Code", "Math"):
            parts.append(item["c"][1])
    return "".join(parts)


def text_inlines(text):
    out = []
    for index, word in enumerate(text.split(" ")):
        if index:
            out.append(node("Space"))
        if word:
            out.append(node("Str", word))
    return out


def caption_inlines(blocks):
    out = []
    for block in blocks:
        if block["t"] in ("Plain", "Para"):
            if out:
                out.append(node("Space"))
            out.extend(block["c"])
    return out


def bold_paragraph(inlines):
    return node("Para", [node("Strong", inlines)])


def empty_attr():
    return ["", [], []]


# ---------------------------------------------------------------------------
# Report

class Report:
    def __init__(self):
        self.tools = []
        self.sections = {}
        self.errors = []
        self.counts = collections.Counter()

    def add(self, section, line):
        self.sections.setdefault(section, []).append(line)

    def error(self, line):
        self.errors.append(line)

    def print(self, stream, elapsed):
        out = []
        out.append("== docs-convert report")
        out.append("tools: " + ("; ".join(self.tools) or "none found"))
        for section in sorted(self.sections, key=SECTION_ORDER.index):
            lines = self.sections[section]
            repeats = collections.Counter(lines)
            out.append("")
            out.append("== %s (%d)" % (SECTION_TITLES[section], len(lines)))
            out.extend("  " + line + (" (x%d)" % count if count > 1 else "") for line, count in repeats.items())
        out.append("")
        if self.errors:
            out.append("== ERRORS (%d) -- nothing was written" % len(self.errors))
            out.extend("  " + line for line in self.errors)
        out.append("finished in %.1f s" % elapsed)
        stream.write("\n".join(out) + "\n")


SECTION_TITLES = {
    "tools": "Tool version overrides",
    "summary": "Summary",
    "asciidoctor": "Asciidoctor warnings",
    "pandoc": "pandoc warnings",
    "unresolved": "Unresolved references (left as written)",
    "ambiguous": "Ambiguous heading matches (first heading taken)",
    "casefold": "References matched only case-insensitively",
    "fixes": "References settled by xref-fixes.tsv",
    "fixes-unused": "xref-fixes.tsv rows that matched no reference",
    "legacy-unresolved": "Legacy .html links with no rule, or with a folder",
    "legacy-map": "Legacy links settled by --legacy-map",
    "legacy-map-overrides": "--legacy-map rows that override a built-in rule",
    "legacy-map-unused": "--legacy-map rows that matched no link",
    "external": "External links",
    "images-missing": "Images not found",
    "images-unreferenced": "Images no converted page references",
    "headings": "Heading adjustments",
    "code-phrase-with-a-link": "Code phrases holding a link, unwrapped instead of made inline code",
    "html-tables": "HTML tables per page",
    "raw-html": "Raw HTML other than tables and sized images, per page",
    "nested-alerts": "Admonitions inside lists or quotes (GitHub shows these as plain quotes)",
    "asciidoc-text": "AsciiDoc markup that Asciidoctor left as text",
    "arrows": "Arrows Asciidoctor made of <=, =>, <-, -> in prose, per page",
    "code": "Code-block table",
}
SECTION_ORDER = list(SECTION_TITLES)


# ---------------------------------------------------------------------------
# Pages

class Heading:
    def __init__(self, identifier, text, is_title):
        self.id = identifier
        self.text = text
        self.is_title = is_title
        self.slug = None


class Page:
    def __init__(self, source, output):
        self.source = source      # relative to the source base, "/"-separated
        self.output = output      # relative to the output folder, "/"-separated
        self.sources = [source]   # every source merged into this page
        self.segments = []        # [(source or None, blocks)]; None: not link-rewritten
        self.headings = []
        self.legacy = []          # [(text, legacy page, target, source, href as written)] in document order
        self.dropped_headings = 0
        self.markdown = None

    @property
    def blocks(self):
        return [block for _, blocks in self.segments for block in blocks]


class Settings:
    """Everything the DocBook -> Markdown stage needs that differs between inputs."""

    def __init__(self):
        self.source_base = None          # folder that page sources and image paths are relative to
        self.out = None
        self.pandoc = None
        self.link_suffixes = (".xml",)   # suffixes a link may carry for a source page
        self.source_suffix = ".xml"      # the suffix page sources carry in the page map
        self.match_heading_text = False  # fragment rules (b) and (c): heading text, "_" as space
        self.global_ids = False          # a fragment-only reference may name an id in any page
        self.legacy_links = False        # rewrite legacy *.html reference links by LEGACY_RULES
        self.xref_fixes = {}             # (source, ref) -> row
        self.legacy_map = {}             # folded legacy page name -> row; consulted before LEGACY_RULES
        self.code_table = None           # path of the code-block table, or None for heuristic tags
        self.seed_code_table = False
        self.require_reviewed = False
        self.allow_unresolved = False
        self.legacy_table = None         # path to write legacy-links.tsv, or None
        self.unconverted = ()            # sources in the page map that have no output page


# ---------------------------------------------------------------------------
# Page-local transforms of one converted document

def clean_characters(blocks, counts):
    def clean(text):
        if "\u00c2\u00a0" in text:
            counts["mojibake"] += text.count("\u00c2\u00a0")
            text = text.replace("\u00c2\u00a0", " ")
        if "\u00a0" in text:
            counts["no-break spaces"] += text.count("\u00a0")
            text = text.replace("\u00a0", " ")
        if "\u200b" in text:
            # Asciidoctor follows each "..." ellipsis with a zero-width space.
            counts["zero-width spaces"] += text.count("\u200b")
            text = text.replace("\u200b", "")
        return text

    for item in iter_nodes(blocks):
        if item["t"] == "Str":
            item["c"] = clean(item["c"])
        elif item["t"] in ("Code", "CodeBlock"):
            item["c"][1] = clean(item["c"][1])


def split_title(document):
    """(title inlines, body blocks): the title is in the metadata, or else the first heading."""
    blocks = document["blocks"]
    title = document.get("meta", {}).get("title")
    if title is not None:
        inlines = title["c"] if title["t"] == "MetaInlines" else caption_inlines(title["c"])
        return inlines, blocks, False
    if blocks and blocks[0]["t"] == "Header":
        return blocks[0]["c"][2], blocks[1:], True
    return [], blocks, False


def normalise_headings(page, title, body, title_in_body, has_part, report):
    """One level-1 heading (the title); body headings start at level 2."""
    kept = []
    dropped = 0
    for block in body:
        if block["t"] == "Header" and not stringify(block["c"][2]).strip():
            dropped += 1
            continue
        kept.append(block)
    # pandoc puts a book's chapters at level 1, or at level 2 when the book has parts; a
    # document whose root kept its title in the body has its sections at level 2 already.
    base = 2 if has_part or title_in_body else 1
    raised = []
    for block in kept:
        if block["t"] == "Header":
            level = block["c"][0] + 2 - base
            if level < 2:
                raised.append(stringify(block["c"][2]))
            block["c"][0] = min(6, max(2, level))
    if raised:
        report.add("headings", "%s: level-0 section(s) shown as ## : %s" % (page.output, "; ".join(raised)))
    if not title:
        report.add("headings", "%s: no document title" % page.output)
    page.dropped_headings = dropped
    return [node("Header", [1, empty_attr(), title])] + kept


def transform_blocks(blocks, page, report):
    """Block titles, sidebars, captions, figures, index terms, list tightness."""

    def visit(item):
        kind = item["t"]
        if kind == "Span" and "indexterm" in item["c"][0][1]:
            return item["c"][1]
        if kind == "Span" and "code" in item["c"][0][1]:
            return code_phrase(item, page, report)
        if kind == "Div":
            classes = item["c"][0][1]
            content = item["c"][1]
            # A block title is a "title" Div inside the titled block's Div; the parent
            # handles it, and pandoc's alert writer expects it inside an admonition.
            if "title" in classes or any(c in ALERT_CLASSES for c in classes):
                return item
            out = []
            if content and content[0]["t"] == "Div" and "title" in content[0]["c"][0][1]:
                report.counts["block titles as bold paragraphs"] += 1
                out.append(bold_paragraph(caption_inlines(content[0]["c"][1])))
                content = content[1:]
            if "sidebar" in classes:
                report.counts["sidebars as block quotes"] += 1
                return [node("BlockQuote", out + content)]
            if classes:
                report.counts["other blocks unwrapped (%s)" % " ".join(classes)] += 1
            return out + content
        if kind == "Figure":
            caption = caption_inlines(item["c"][1][1])
            out = []
            for block in item["c"][2]:
                out.append(node("Para", block["c"]) if block["t"] == "Plain" else block)
            if caption:
                report.counts["figure captions"] += 1
                out.append(node("Para", [node("Emph", caption)]))
            return out
        if kind == "Table" and ["custom-style", "horizontal"] in item["c"][0][2]:
            terms = horizontal_list(item)
            if terms is not None:
                report.counts["horizontal labelled lists as definition lists"] += 1
                return [node("DefinitionList", [[bold_term(term), [definition]] for term, definition in terms])]
        if kind == "Table":
            caption = caption_inlines(item["c"][1][1])
            if caption:
                report.counts["table titles as bold paragraphs"] += 1
                item["c"][1] = [None, []]
                return [bold_paragraph(caption), item]
            return item
        if kind in ("BulletList", "OrderedList"):
            items = item["c"] if kind == "BulletList" else item["c"][1]
            if all(is_simple_item(entry) for entry in items):
                for entry in items:
                    for index, block in enumerate(entry):
                        if block["t"] == "Para":
                            entry[index] = node("Plain", block["c"])
            return item
        if kind == "DefinitionList":
            for entry in item["c"]:
                entry[0] = bold_term(entry[0])
            return item
        return item

    return rewrite(blocks, visit)


def code_phrase(item, page, report):
    """DocBook's `<phrase role="code">` as inline code.

    pandoc keeps the role as a class, and its gfm writer then emits a raw `<span class="code">`,
    which GitHub renders as ordinary prose - the one thing the phrase was there to say is lost.
    A code span cannot hold a link, so a phrase that has one is unwrapped and listed instead;
    the reference sources have none, and nesting anything else (they nest only bold, marking a
    keyword) says nothing a code span does not already say."""
    inlines = item["c"][1]
    if any(inline["t"] == "Link" for inline in iter_nodes(inlines)):
        report.add("code-phrase-with-a-link", "%s: %s" % (page.output, stringify(inlines)[:60]))
        return inlines
    report.counts["code phrases as inline code"] += 1
    return node("Code", [empty_attr(), stringify(inlines)])


def bold_term(inlines):
    """gfm has no definition lists; pandoc writes the term as a line of its own, so make it stand out."""
    if len(inlines) == 1 and inlines[0]["t"] == "Strong":
        return inlines
    return [node("Strong", list(inlines))]


def horizontal_list(table):
    """[(term inlines, definition blocks)] of a [horizontal] labelled list, which the DocBook
    backend writes as a two-column table; None if the table does not have that shape."""
    _, _, colspecs, head, bodies, foot = table["c"]
    if len(colspecs) != 2 or head[1] or foot[1]:
        return None
    entries = []
    for body in bodies:
        for row in body[2] + body[3]:
            cells = row[1]
            if len(cells) != 2:
                return None
            term = caption_inlines(cells[0][4])
            if len(cells[0][4]) > 1 or not term:
                return None
            entries.append((term, cells[1][4]))
    return entries


def is_simple_item(blocks):
    """A list item that AsciiDoc shows tight: one paragraph, optionally followed by sub-lists."""
    return bool(blocks) and blocks[0]["t"] in ("Para", "Plain") and all(
        block["t"] in ("BulletList", "OrderedList") for block in blocks[1:])


def nested_alerts(blocks, page, report):
    def walk(items, nested):
        for block in items:
            kind = block["t"]
            if kind == "Div" and any(c in ALERT_CLASSES for c in block["c"][0][1]):
                report.counts["admonitions"] += 1
                if nested:
                    report.add("nested-alerts", "%s: %s" % (page.output, stringify(block["c"][1][-1:])[:70]))
            if kind == "BulletList":
                for entry in block["c"]:
                    walk(entry, True)
            elif kind == "OrderedList":
                for entry in block["c"][1]:
                    walk(entry, True)
            elif kind == "DefinitionList":
                for _, definitions in block["c"]:
                    for definition in definitions:
                        walk(definition, True)
            elif kind == "BlockQuote":
                walk(block["c"], True)
            elif kind == "Div":
                walk(block["c"][1], nested)
    walk(blocks, False)


LEFTOVER_MACRO = re.compile(r"\b(?:image|link|xref|footnote|anchor|include)::?[^\s\[]*\[")
ARROWS = "\u21d0\u21d2\u2190\u2192"


def prose_findings(blocks, page, report):
    """Review items in the prose: macros that stayed text, and replacement arrows."""
    arrows = 0
    for item in iter_nodes(blocks):
        if item["t"] == "Str":
            arrows += sum(item["c"].count(arrow) for arrow in ARROWS)
            if LEFTOVER_MACRO.search(item["c"]):
                report.add("asciidoc-text", "%s: %s" % (page.output, item["c"][:80]))
    if arrows:
        report.add("arrows", "%s: %d" % (page.output, arrows))


def load_page(page, document, has_part, settings, report):
    title, body, title_in_body = split_title(document)
    blocks = normalise_headings(page, title, body, title_in_body, has_part, report)
    clean_characters(blocks, report.counts)
    blocks = transform_blocks(blocks, page, report)
    nested_alerts(blocks, page, report)
    prose_findings(blocks, page, report)
    page.segments = [(page.source, blocks)]


# ---------------------------------------------------------------------------
# Pass 1: the heading index

MATCH_QUOTES = str.maketrans({"\u2019": "'", "\u2018": "'", "\u201c": '"', "\u201d": '"'})


def match_text(text):
    return re.sub(r"\s+", " ", text.translate(MATCH_QUOTES)).strip()


def index_headings(pages, settings, api, report):
    """Each page's headings in order, with the GitHub anchor of each: the page's headings are
    rendered on their own and slugged by the link checker's rule."""
    def render(page):
        headers = [block for block in page.blocks if block["t"] == "Header"]
        return headers, check_links.heading_slugs(render_markdown(settings.pandoc, api, headers))

    for page, (headers, slugs) in zip(pages, parallel(render, pages)):
        nested = sum(1 for item in iter_nodes([b for b in page.blocks if b["t"] != "Header"]) if item["t"] == "Header")
        if nested:
            report.add("headings", "%s: %d heading(s) inside other blocks get no anchor" % (page.output, nested))
        if len(slugs) != len(headers):
            fail("%s: %d headings in the tree but %d in the rendered Markdown" % (page.output, len(headers), len(slugs)))
        page.headings = []
        for index, (header, slug) in enumerate(zip(headers, slugs)):
            heading = Heading(header["c"][1][0], match_text(stringify(header["c"][2])), index == 0)
            heading.slug = slug
            page.headings.append(heading)


# ---------------------------------------------------------------------------
# Pass 2: links and images

class Resolver:
    def __init__(self, pages, settings, report):
        self.settings = settings
        self.report = report
        self.pages = pages
        self.by_source = {source: Page(source, None) for source in settings.unconverted}
        for page in pages:
            for source in page.sources:
                self.by_source[source] = page
        self.ids = {}
        if settings.global_ids:
            for page in pages:
                for heading in page.headings:
                    if heading.id:
                        self.ids.setdefault(heading.id, []).append((page, heading))
        self.used_fixes = set()
        self.used_legacy_rows = set()
        self.images = {}          # output name -> absolute source path
        self.image_sources = {}   # absolute source path -> output name
        self.external = []
        self.unresolved = []
        self.resolved = 0

    def normalise_ref(self, ref):
        path, hash_, fragment = ref.partition("#")
        while path.startswith("./"):
            path = path[2:]
        for suffix in self.settings.link_suffixes:
            if path.endswith(suffix):
                path = path[: -len(suffix)] + self.settings.source_suffix
                break
        else:
            if path and not posixpath.splitext(path)[1]:
                path += self.settings.source_suffix
        return path + (hash_ + fragment if fragment else "")

    def locate(self, source, ref):
        """(page, heading or None, problem, note): the page and heading a reference names; note is
        find_heading's (report section, line) when the heading was matched loosely."""
        path, _, fragment = ref.partition("#")
        path = unquote(path)
        key = posixpath.normpath(posixpath.join(posixpath.dirname(source), path)) if path else source
        page = self.by_source.get(key)
        if page is None:
            return None, None, "no such page" if key != ".." and not key.startswith("../") else "outside the sources", None
        if page.output is None:
            return None, None, "page is not converted", None
        if not fragment:
            return page, None, None, None
        heading, note = self.find_heading(page, fragment)
        if heading is None and self.settings.global_ids and not path:
            matches = self.ids.get(fragment, [])
            if len(matches) == 1:
                page, heading = matches[0]
            elif matches:
                return None, None, "id '#%s' is in %d pages" % (fragment, len(matches)), None
        if heading is None:
            return None, None, "no heading matches '#%s' in %s" % (fragment, page.output), None
        if note:
            note = (note[0], "%s -> %s#%s: %s" % (source, page.output, heading.slug, note[1]))
        return page, heading, None, note

    def find_heading(self, page, fragment):
        for heading in page.headings:
            if heading.id and heading.id == fragment:
                return heading, None
        if not self.settings.match_heading_text:
            return None, None
        wanted = match_text(fragment.replace("_", " "))
        for folded in (False, True):
            def same(text):
                return text.casefold() == wanted.casefold() if folded else text == wanted
            matches = [h for h in page.headings if same(h.text)]
            if matches:
                if len(matches) > 1:
                    return matches[0], ("ambiguous", "'#%s' matches %d headings named '%s'" % (fragment, len(matches), matches[0].text))
                if folded:
                    return matches[0], ("casefold", "'#%s' matches heading '%s'" % (fragment, matches[0].text))
                return matches[0], None
        return None, None

    def href(self, current, page, heading):
        if heading is not None and heading.is_title:
            heading = None
        if page is current:
            path = "" if heading else posixpath.basename(current.output)
        else:
            path = posixpath.relpath(page.output, posixpath.dirname(current.output) or ".")
        return path + ("#" + heading.slug if heading else "")

    def link(self, current, source, item):
        """The rewritten Link node, a list of inlines (unlinked), or the node unchanged."""
        url = item["c"][2][0]
        text = stringify(item["c"][1])
        scheme = re.match(r"^([A-Za-z][A-Za-z0-9+.-]*):", url)
        if scheme:
            if scheme.group(1).lower() in EXTERNAL_SCHEMES:
                self.external.append("%s: %s" % (current.output, url))
                return item
            return self.fail(current, source, url, text, "unsupported scheme")
        path = url.partition("#")[0]
        if LEGACY_PAGE.search(path) and self.settings.legacy_links:
            return self.legacy(current, source, item, path, text)
        ref = self.normalise_ref(url)
        page, heading, problem, note = self.locate(source, ref)
        fix = self.settings.xref_fixes.get((source, ref))
        # A row settles a reference that does not resolve, or one that resolved only by taking the
        # first of several same-named headings; a reference that resolves cleanly ignores its row.
        if fix is not None and (problem or note and note[0] == "ambiguous"):
            self.used_fixes.add((source, ref))
            replacement = fix["replacement"]
            if not replacement:
                self.report.add("fixes", "%s: %s -> unlinked (%s)" % (source, ref, fix["reason"]))
                return item["c"][1]
            page, heading, problem, note = self.locate(source, self.normalise_ref(replacement))
            if problem:
                item["c"][2][0] = ref
                return self.fail(current, source, ref, text, "xref-fixes replacement %s: %s" % (replacement, problem))
            self.report.add("fixes", "%s: %s -> %s (%s)" % (source, ref, replacement, fix["reason"]))
        elif problem:
            # Left in the form the report prints, so the link checker names the same reference.
            item["c"][2][0] = ref
            return self.fail(current, source, ref, text, problem)
        if note:
            self.report.add(*note)
        item["c"][2][0] = self.href(current, page, heading)
        self.resolved += 1
        return item

    def legacy(self, current, source, item, path, text):
        url = item["c"][2][0]
        name = posixpath.basename(path)
        if name != path:
            self.report.add("legacy-unresolved", "%s: %s has a folder; rule applied to %s" % (source, path, name))
        if url != path:
            self.report.add("legacy-unresolved", "%s: %s; the fragment is dropped" % (source, url))
        # A --legacy-map row wins over the built-in rules: the map is where a page that no rule
        # describes, or one whose rule names the wrong page, is settled.
        row = self.settings.legacy_map.get(name.casefold())
        fragment = ""
        if row is not None:
            self.used_legacy_rows.add(name.casefold())
            target, _, fragment = row["target"].partition("#")
            if not target:
                self.report.add("legacy-map", "%s: %s -> unlinked (%s)" % (source, name, row["reason"]))
                return item["c"][1]
            self.report.add("legacy-map", "%s -> %s (%s)" % (name, row["target"], row["reason"]))
        else:
            target = legacy_target(name)
        if target is not None:
            relative = posixpath.relpath(target, posixpath.dirname(current.output) or ".")
            item["c"][2][0] = relative + ("#" + fragment if fragment else "")
        # The destination as written, which legacy_rows looks for in the rendered page; its
        # target column is a path, which is what the link checker's --pending compares.
        current.legacy.append((text, name, target or "", source, item["c"][2][0]))
        if target is None:
            self.report.add("legacy-unresolved", "%s: %s" % (source, path))
            return self.fail(current, source, path, text, "legacy page with no rule")
        return item

    def fail(self, current, source, ref, text, problem):
        candidates = self.candidates(source, ref, problem)
        if candidates:
            problem += "; candidates: " + ", ".join(candidates[:4]) + (" ..." if len(candidates) > 4 else "")
        self.unresolved.append((current.output, source, ref, text, problem))
        return None

    def candidates(self, source, ref, problem):
        """References, in xref-fixes.tsv's replacement form, that the unresolved one may have meant."""
        path, _, fragment = ref.partition("#")
        here = posixpath.dirname(source) or "."
        found = []
        if problem.startswith("no such page"):
            name = posixpath.basename(unquote(path)).casefold()
            for key in sorted(self.by_source):
                page = self.by_source[key]
                if posixpath.basename(key).casefold() == name and (not fragment or page.output and self.find_heading(page, fragment)[0]):
                    found.append(posixpath.relpath(key, here) + ("#" + fragment if fragment else ""))
        if fragment and not found and (problem.startswith("no heading matches") or problem.startswith("no such page")):
            # With the file name wrong, only an explicit id is specific enough to search for.
            by_text = problem.startswith("no heading matches")
            wanted = match_text(fragment.replace("_", " ")).casefold()
            for page in self.pages:
                for heading in page.headings:
                    if heading.id == fragment:
                        found.append("%s#%s" % (posixpath.relpath(page.source, here), fragment))
                    elif by_text and heading.text.casefold() == wanted:
                        found.append("%s#%s" % (posixpath.relpath(page.source, here), heading.text.replace(" ", "_")))
        return found

    def image(self, current, source, item):
        url = item["c"][2][0]
        if re.match(r"^[A-Za-z][A-Za-z0-9+.-]*:", url):
            self.report.add("images-missing", "%s: %s is not a local file" % (source, url[:60]))
            return item
        relative = posixpath.normpath(posixpath.join(posixpath.dirname(source), unquote(url)))
        found = locate_exact(self.settings.source_base, relative)
        if found is None:
            self.report.add("images-missing", "%s: %s" % (source, url))
            return item
        name = self.image_sources.get(found)
        if name is None:
            name = image_name(os.path.basename(found))
            if name in self.images:
                fail("images %s and %s both become images/%s" % (self.images[name], found, name))
            self.images[name] = found
            self.image_sources[found] = name
        item["c"][2][0] = posixpath.relpath("images/" + name, posixpath.dirname(current.output) or ".")
        return item


def locate_exact(base, relative):
    """The absolute path of base/relative, matching each component's case exactly, or None."""
    if relative == ".." or relative.startswith("../") or relative.startswith("/"):
        return None
    current = base
    for part in relative.split("/"):
        try:
            if part not in os.listdir(current):
                return None
        except OSError:
            return None
        current = os.path.join(current, part)
    return current if os.path.isfile(current) else None


def rewrite_links(pages, settings, report):
    resolver = Resolver(pages, settings, report)
    for page in pages:
        segments = []
        for source, blocks in page.segments:
            if source is not None:
                def visit(item, source=source):
                    if item["t"] == "Link":
                        result = resolver.link(page, source, item)
                        return item if result is None else result
                    if item["t"] == "Image":
                        return resolver.image(page, source, item)
                    return item
                blocks = rewrite(blocks, visit)
            segments.append((source, blocks))
        page.segments = segments
    return resolver


# ---------------------------------------------------------------------------
# Code blocks

SQL_START = re.compile(r"^\s*(?:SELECT|INSERT\s+INTO|UPDATE|DELETE\s+FROM|CREATE\s+TABLE|ALTER\s+TABLE)\b", re.MULTILINE)
D4_WORDS = re.compile(r"\b(?:select|insert|update|delete|create|alter|drop|var|begin|where|table)\b", re.IGNORECASE)
XML_TAG = re.compile(r"^<[A-Za-z_][\w.:-]*(?:[\s/>]|$)")


def guess_language(text):
    """The seed tag for a code block; the reviewed table decides the real one."""
    if "::=" in text:
        return "bnf"
    lines = [line for line in text.split("\n") if line.strip()]
    first = lines[0].strip() if lines else ""
    if first.startswith("<?xml") or (XML_TAG.match(first) and ("</" in text or "/>" in text)):
        return "xml"
    if SQL_START.search(text):
        return "sql"
    if ";" in text or ":=" in text or "{" in text or D4_WORDS.search(text):
        return "d4"
    return "text"


def code_sha(text):
    return hashlib.sha1(text.encode("utf-8")).hexdigest()[:12]


def first_line(text):
    for line in text.split("\n"):
        if line.strip():
            return line.strip()[:60]
    return ""


CODE_COLUMNS = ("page", "n", "sha", "lang", "reviewed", "first_line")


def tag_code_blocks(pages, settings, report):
    table = {}
    if settings.code_table and os.path.exists(settings.code_table):
        for row in read_tsv(settings.code_table, CODE_COLUMNS):
            table[(row["page"], int(row["n"]))] = row
    elif settings.code_table and not settings.seed_code_table:
        fail("%s does not exist; run with --seed-code-table to create it" % settings.code_table)
    rows = []
    problems = []
    seen = set()
    added = changed = 0
    languages = collections.Counter()
    for page in pages:
        blocks = [item for item in iter_nodes(page.blocks) if item["t"] == "CodeBlock"]
        for n, block in enumerate(blocks, 1):
            text = block["c"][1]
            sha = code_sha(text)
            # NOTE: rows are keyed by position, so a block added or removed earlier in a page
            # re-seeds every later row of that page and clears its review; fine while the sources
            # are frozen, but if they are edited after the review, carry rows over by sha first.
            key = (page.output, n)
            seen.add(key)
            row = table.get(key)
            if settings.code_table is None:
                lang = guess_language(text)
            elif row is None or row["sha"] != sha:
                if not settings.seed_code_table:
                    problems.append("%s block %d (%s): %s" % (page.output, n, first_line(text), "not in the table" if row is None else "text changed since the table row"))
                    lang = guess_language(text)
                else:
                    if row is None:
                        added += 1
                    else:
                        changed += 1
                    row = {"page": page.output, "n": n, "sha": sha, "lang": guess_language(text), "reviewed": "", "first_line": first_line(text)}
                    lang = row["lang"]
            else:
                lang = row["lang"]
                if lang not in LANGUAGES:
                    problems.append("%s block %d: language '%s' is not one of %s" % (page.output, n, lang, ", ".join(LANGUAGES)))
            if row is not None:
                rows.append(row)
                if settings.require_reviewed and row.get("reviewed") != "yes":
                    problems.append("%s block %d (%s): not reviewed" % (page.output, n, row["first_line"]))
            languages[lang] += 1
            block["c"][0] = ["", [lang], []]
    stale = sorted(key for key in table if key not in seen)
    if stale:
        message = "%d table row(s) name no block (first: %s block %d)" % (len(stale), stale[0][0], stale[0][1])
        if settings.seed_code_table:
            report.add("code", message + "; removed")
        else:
            problems.append(message)
    total = sum(languages.values())
    report.add("code", "%d code blocks: %s" % (total, ", ".join("%s %d" % item for item in sorted(languages.items()))))
    if settings.code_table:
        unreviewed = sum(1 for row in rows if row.get("reviewed") != "yes")
        report.add("code", "%d of %d table rows unreviewed" % (unreviewed, len(rows)))
        if settings.seed_code_table:
            report.add("code", "seeding: %d row(s) added, %d row(s) re-seeded (text changed)" % (added, changed))
    for problem in problems:
        report.error("code-blocks.tsv: " + problem)
    return rows


# ---------------------------------------------------------------------------
# Rendering and output

def render_markdown(pandoc, api, blocks):
    document = {"pandoc-api-version": api, "meta": {}, "blocks": blocks}
    data = json.dumps(document, ensure_ascii=False).encode("utf-8")
    out, _ = run_pandoc(pandoc, ["-f", "json", "-t", "gfm", "--wrap=none", "--eol=lf"], data)
    return out.decode("utf-8")


# Not an escaped "\<name>" (prose about grammar terms) and not an autolink "<http:...>".
RAW_TAG = re.compile(r"(?<!\\)<(/?)([A-Za-z][A-Za-z0-9]*)\b(?!:)")
TABLE_TAGS = {"table", "colgroup", "col", "thead", "tbody", "tfoot", "tr", "th", "td", "caption"}
BLOCK_TAGS_IN_TABLES = {"p", "ul", "ol", "li", "pre", "code", "a", "img", "strong", "em", "sup", "sub", "br", "dl", "dt", "dd", "blockquote"}


CODE_SPAN = re.compile(r"(`+)(?:(?!\1).)*?\1")
FENCE = re.compile(r"^\s*(`{3,}|~{3,})")


def html_census(markdown):
    """(HTML tables, other raw tags) outside code; tags inside a table's cells are counted with it."""
    tables = 0
    other = {}
    depth = 0
    fence = None
    for line in markdown.split("\n"):
        opener = FENCE.match(line)
        if fence:
            if opener and opener.group(1)[0] == fence[0] and len(opener.group(1)) >= len(fence) and not line.strip()[len(opener.group(1)):].strip():
                fence = None
            continue
        if opener:
            fence = opener.group(1)
            continue
        line = CODE_SPAN.sub("", line)
        for match in RAW_TAG.finditer(line):
            closing, tag = match.group(1), match.group(2).lower()
            if tag == "table":
                if closing:
                    depth -= 1
                else:
                    depth += 1
                    tables += 1
                continue
            if depth and (tag in TABLE_TAGS or tag in BLOCK_TAGS_IN_TABLES):
                continue
            if tag == "img" or closing:
                continue
            other[tag] = other.get(tag, 0) + 1
    return tables, other


def render_pages(pages, settings, api, report):
    def render(page):
        return render_markdown(settings.pandoc, api, page.blocks)

    for page, markdown in zip(pages, parallel(render, pages)):
        page.markdown = markdown
        slugs = check_links.heading_slugs(markdown)
        expected = [heading.slug for heading in page.headings]
        if slugs != expected:
            fail("%s: the rendered page's heading anchors differ from the index built in pass 1" % page.output)
        if "data:image" in markdown:
            report.error("%s: contains base64 image data" % page.output)
        tables, other = html_census(markdown)
        if tables:
            report.add("html-tables", "%s: %d" % (page.output, tables))
        if other:
            report.add("raw-html", "%s: %s" % (page.output, ", ".join("<%s> %d" % item for item in sorted(other.items()))))
        report.counts["footnotes"] += len(re.findall(r"^\[\^\d+\]:", markdown, re.MULTILINE))
        report.counts["GitHub alerts"] += len(re.findall(r"^[ >]*> \[!(?:NOTE|TIP|IMPORTANT|WARNING|CAUTION)\]", markdown, re.MULTILINE))


LEGACY_COLUMNS = ("page", "line", "text", "legacy_page", "target", "source")


def legacy_rows(pages):
    """legacy-links.tsv rows: each legacy link with the line it landed on in its page."""
    rows = []
    for page in pages:
        if not page.legacy:
            continue
        links, _ = check_links.extract_links(page.markdown)
        lines_by_dest = {}
        for line, dest, _kind in links:
            lines_by_dest.setdefault(dest, []).append(line)
        for text, name, target, source, dest in page.legacy:
            lines = lines_by_dest.get(dest)
            if not lines:
                fail("%s: cannot find the rendered link to %s" % (page.output, dest))
            # Several links to one target: take their lines in order, the last one for any extra.
            # NOTE: tree order is rendered order except for footnotes, which pandoc writes at the
            # end of the page; if one target is linked from a footnote and from the body, those
            # rows' text can swap. Harmless while every link to a target carries the same text.
            line = lines.pop(0) if len(lines) > 1 else lines[0]
            rows.append({"page": page.output, "line": line, "text": text, "legacy_page": name, "target": target, "source": source})
    rows.sort(key=lambda row: (row["page"], row["line"]))
    return rows


def convert_image(path):
    if not path.lower().endswith(RASTER_TO_PNG):
        with open(path, "rb") as stream:
            return stream.read()
    try:
        from PIL import Image
    except ImportError:
        fail("Pillow is not installed: run `python -m pip install -r tools/docs-convert/requirements.txt` (%s)" % README_HINT)
    with Image.open(path) as image:
        image.load()
        if image.mode not in ("1", "L", "LA", "P", "RGB", "RGBA", "I", "I;16"):
            image = image.convert("RGBA" if "A" in image.mode else "RGB")
        buffer = io.BytesIO()
        image.save(buffer, "PNG", optimize=True)
        return buffer.getvalue()


def convert_images(resolver, report):
    """{output name: bytes} for every referenced image, converted in memory."""
    converted = dict(zip(sorted(resolver.images), parallel(lambda name: convert_image(resolver.images[name]), sorted(resolver.images))))
    source_bytes = sum(os.path.getsize(path) for path in resolver.images.values())
    total = sum(len(data) for data in converted.values())
    kinds = collections.Counter(os.path.splitext(path)[1].lower() for path in resolver.images.values())
    if converted:
        report.add("summary", "images: %d (%s), %.1f MB of sources -> %.2f MB in images/" % (
            len(converted), ", ".join("%s %d" % item for item in sorted(kinds.items())), source_bytes / 1e6, total / 1e6))
    else:
        report.add("summary", "images: none")
    return converted


def write_outputs(pages, images, settings):
    for page in pages:
        if page.output is None:
            continue
        write_file(os.path.join(settings.out, *page.output.split("/")), page.markdown.encode("utf-8"))
    for name, data in sorted(images.items()):
        write_file(os.path.join(settings.out, "images", name), data)


def write_file(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as stream:
        stream.write(data)


# ---------------------------------------------------------------------------
# The shared DocBook -> Markdown stage

IMAGEDATA = re.compile(r"<imagedata\b[^>]*>")


def read_docbook(path):
    with open(path, "rb") as stream:
        xml = stream.read().decode("utf-8")
    # pandoc's DocBook reader takes an image's size from width/depth, which Asciidoctor
    # writes as contentwidth/contentdepth.
    xml = IMAGEDATA.sub(lambda m: m.group().replace(" contentwidth=", " width=").replace(" contentdepth=", " depth="), xml)
    return xml


def docbook_to_trees(pages_and_xml, settings, report):
    """Read each page's DocBook through pandoc; returns (api version, [(page, tree, has_part)])."""
    def read(item):
        page, xml_path = item
        xml = read_docbook(xml_path)
        out, warnings = run_pandoc(settings.pandoc, ["-f", "docbook", "-t", "json"], xml.encode("utf-8"))
        return json.loads(out.decode("utf-8")), warnings, bool(re.search(r"<part[\s>]", xml))

    results = parallel(read, pages_and_xml)
    trees = []
    api = None
    for (page, _), (tree, warnings, has_part) in zip(pages_and_xml, results):
        api = api or tree["pandoc-api-version"]
        for line in warnings.splitlines():
            if line.strip():
                report.add("pandoc", "%s: %s" % (page.source, line.strip()))
        trees.append((page, tree, has_part))
    return api, trees


def finish(pages, api, settings, report):
    """Pass 1, pass 2, code tags, rendering, the report; writes the output when there are no errors."""
    index_headings(pages, settings, api, report)
    resolver = rewrite_links(pages, settings, report)
    code_rows = tag_code_blocks(pages, settings, report)
    render_pages(pages, settings, api, report)
    images = convert_images(resolver, report)

    for output, source, ref, text, problem in sorted(resolver.unresolved):
        report.add("unresolved", "%s (from %s): %s [%s] -- %s" % (output, source, ref, text[:50], problem))
    for key in sorted(set(settings.xref_fixes) - resolver.used_fixes):
        row = settings.xref_fixes[key]
        report.add("fixes-unused", "line %d: %s %s" % (row["_line"], key[0], key[1]))
    for key in sorted(settings.legacy_map):
        row = settings.legacy_map[key]
        rule = legacy_target(row["legacy_page"].strip())
        if rule is not None:
            report.add("legacy-map-overrides", "line %d: %s -> %s, not the rule's %s"
                       % (row["_line"], row["legacy_page"], row["target"] or "(unlinked)", rule))
        if key not in resolver.used_legacy_rows:
            report.add("legacy-map-unused", "line %d: %s" % (row["_line"], row["legacy_page"]))
    for line in sorted(resolver.external):
        report.add("external", line)
    referenced = set(resolver.images.values())
    image_dir = os.path.join(settings.source_base, "Images")
    if os.path.isdir(image_dir):
        for name in sorted(os.listdir(image_dir)):
            path = os.path.join(image_dir, name)
            if os.path.isfile(path) and name.lower().endswith(IMAGE_EXTENSIONS) and path not in referenced:
                report.add("images-unreferenced", "Images/" + name)
    legacy = legacy_rows(pages) if settings.legacy_links else []
    converted = [page for page in pages if page.output]
    report.add("summary", "pages: %d written from %d sources" % (len(converted), sum(len(p.sources) for p in converted)))
    report.add("summary", "links: %d resolved to pages, %d external, %d legacy (%d with a target), %d unresolved" % (
        resolver.resolved, len(resolver.external), len(legacy), sum(1 for r in legacy if r["target"]), len(resolver.unresolved)))
    for key in sorted(report.counts):
        report.add("summary", "%s: %d" % (key, report.counts[key]))
    report.add("summary", "empty headings dropped: %d" % sum(page.dropped_headings for page in pages))
    if resolver.unresolved and not settings.allow_unresolved:
        report.error("%d unresolved reference(s); fix them or add xref-fixes.tsv rows (--allow-unresolved to write anyway)" % len(resolver.unresolved))
    if report.errors:
        return 1
    write_outputs(pages, images, settings)
    if settings.code_table and settings.seed_code_table:
        write_tsv(settings.code_table, CODE_COLUMNS, sorted(code_rows, key=lambda r: (r["page"], int(r["n"]))))
    if settings.legacy_table:
        write_tsv(settings.legacy_table, LEGACY_COLUMNS, legacy)
    return 0


# ---------------------------------------------------------------------------
# The AsciiDoc route

def read_pages_table():
    rows = read_tsv(PAGES_TSV, ("source", "output"))
    sources = {}
    outputs = {}
    for row in rows:
        source, output = row["source"], row["output"] or None
        if source in sources:
            fail("pages.tsv line %d: %s is listed twice" % (row["_line"], source))
        sources[source] = output
        if output:
            outputs.setdefault(output, []).append(source)
    for output, listed in outputs.items():
        if len(listed) > 1 and not (output == INDEX_OUTPUT and sorted(listed) == sorted([INDEX_INTRO, INDEX_CONTENTS])):
            fail("pages.tsv: %s are all written to %s" % (", ".join(listed), output))
    on_disk = set()
    for folder, subfolders, files in os.walk(SOURCE_ROOT):
        subfolders.sort()
        for name in files:
            if name.endswith(".adoc"):
                on_disk.add(os.path.relpath(os.path.join(folder, name), SOURCE_ROOT).replace(os.sep, "/"))
    missing = sorted(on_disk - set(sources))
    extra = sorted(set(sources) - on_disk)
    if missing:
        fail("pages.tsv does not list: %s" % ", ".join(missing))
    if extra:
        fail("pages.tsv lists sources that do not exist: %s" % ", ".join(extra))
    return sources


def run_asciidoctor(node, cli, sources, work, report):
    """DocBook for each converted source, one Asciidoctor run per folder; returns {source: xml path}."""
    by_folder = {}
    for source in sources:
        by_folder.setdefault(posixpath.dirname(source), []).append(source)
    xml = {}
    for folder in sorted(by_folder):
        names = sorted(posixpath.basename(s) for s in by_folder[folder])
        target = os.path.join(work, "docbook", *folder.split("/")) if folder else os.path.join(work, "docbook")
        os.makedirs(target, exist_ok=True)
        # data-uri! stops the sources' :data-uri: inlining every image as base64; reproducible
        # leaves out the conversion date. Inter-document references come out as File.xml#id.
        command = [node, cli, "-r", "@asciidoctor/docbook-converter", "-b", "docbook5",
                   "-a", "data-uri!", "-a", "reproducible", "-D", target] + names
        process = subprocess.run(command, cwd=os.path.join(SOURCE_ROOT, *folder.split("/")), capture_output=True)
        prefix = folder + "/" if folder else ""
        for line in process.stderr.decode("utf-8", "replace").splitlines():
            line = re.sub(r"^asciidoctor: ([A-Z]+): ", lambda m: m.group(1) + ": " + prefix, line.strip())
            if line:
                report.add("asciidoctor", line)
        if process.returncode:
            fail("Asciidoctor failed in Documentation/%s (exit %d)" % (folder, process.returncode))
        for source in by_folder[folder]:
            path = os.path.join(target, posixpath.splitext(posixpath.basename(source))[0] + ".xml")
            if not os.path.exists(path):
                fail("Asciidoctor wrote no DocBook for %s" % source)
            xml[source] = path
    return xml


def assemble_index(pages, settings, api, report):
    """docs/README.md: README.adoc, then SUMMARY.adoc under "Contents", then index-extra.md."""
    by_source = {page.source: page for page in pages}
    intro, contents = by_source[INDEX_INTRO], by_source[INDEX_CONTENTS]
    body = contents.blocks[1:]
    for block in body:
        if block["t"] == "Header":
            block["c"][0] = min(6, block["c"][0] + 1)
    with open(INDEX_EXTRA, "rb") as stream:
        extra, _ = run_pandoc(settings.pandoc, ["-f", "gfm", "-t", "json"], stream.read())
    heading = node("Header", [2, empty_attr(), text_inlines("Contents")])
    intro.segments = intro.segments + [(None, [heading]), (INDEX_CONTENTS, body), (None, json.loads(extra.decode("utf-8"))["blocks"])]
    intro.sources = [INDEX_INTRO, INDEX_CONTENTS]
    intro.dropped_headings += contents.dropped_headings
    pages.remove(contents)


def convert_asciidoc(args):
    report = Report()
    started = time.time()
    try:
        settings = Settings()
        settings.source_base = SOURCE_ROOT
        settings.out = os.path.abspath(args.out)
        settings.link_suffixes = (".xml", ".adoc")
        settings.source_suffix = ".adoc"
        settings.match_heading_text = True
        settings.legacy_links = True
        settings.code_table = CODE_TSV
        settings.seed_code_table = args.seed_code_table
        settings.require_reviewed = args.require_reviewed
        settings.allow_unresolved = args.allow_unresolved
        settings.legacy_table = LEGACY_TSV
        settings.xref_fixes = read_xref_fixes(XREF_TSV)
        settings.pandoc = find_pandoc(args.pandoc, args.allow_tool_versions, report)
        node_path, cli = find_asciidoctor(args.allow_tool_versions, report)
        sources = read_pages_table()
        converted = sorted(source for source, output in sources.items() if output)
        settings.unconverted = sorted(source for source, output in sources.items() if not output)
        work = args.work or tempfile.mkdtemp(prefix="docs-convert-")
        try:
            xml = run_asciidoctor(node_path, cli, converted, work, report)
            pages = [Page(source, sources[source]) for source in converted]
            api, trees = docbook_to_trees([(page, xml[page.source]) for page in pages], settings, report)
            for page, tree, has_part in trees:
                load_page(page, tree, has_part, settings, report)
            assemble_index(pages, settings, api, report)
            status = finish(pages, api, settings, report)
        finally:
            if not args.work and not args.keep_work:
                shutil.rmtree(work, ignore_errors=True)
            elif not args.work:
                print("intermediate files kept in %s" % work)
    except ConvertError as error:
        report.error(str(error))
        status = 2
    report.print(sys.stdout, time.time() - started)
    return status


LEGACY_MAP_COLUMNS = ("legacy_page", "target", "reason")


def read_legacy_map(path):
    """Legacy page name -> row, keyed case-insensitively because the sources spell the suffix
    three ways. A row whose key a built-in rule already handles is reported, not refused: that
    is how a merged or renamed reference page is retargeted."""
    rows = {}
    if not path:
        return rows
    for row in read_tsv(path, LEGACY_MAP_COLUMNS):
        key = row["legacy_page"].strip().casefold()
        if not key:
            fail("%s line %d has no legacy page name" % (os.path.basename(path), row["_line"]))
        if key in rows:
            fail("%s line %d repeats %s" % (os.path.basename(path), row["_line"], row["legacy_page"]))
        rows[key] = row
    return rows


def read_xref_fixes(path):
    fixes = {}
    if not path or not os.path.exists(path):
        return fixes
    for row in read_tsv(path, ("source", "ref", "replacement", "reason")):
        key = (row["source"], row["ref"])
        if key in fixes:
            fail("%s line %d repeats %s %s" % (os.path.basename(path), row["_line"], key[0], key[1]))
        fixes[key] = row
    return fixes


# ---------------------------------------------------------------------------
# The standalone DocBook route

def convert_docbook(args):
    report = Report()
    started = time.time()
    try:
        settings = Settings()
        files = [os.path.abspath(path) for path in args.files]
        for path in files:
            if not os.path.isfile(path):
                fail("not a file: %s" % path)
        settings.source_base = os.path.abspath(args.base) if args.base else os.path.commonpath([os.path.dirname(p) for p in files])
        settings.out = os.path.abspath(args.out)
        settings.match_heading_text = args.match_heading_text
        settings.global_ids = args.global_ids
        settings.legacy_links = args.legacy_html
        settings.code_table = os.path.abspath(args.code_table) if args.code_table else None
        settings.seed_code_table = args.seed_code_table
        settings.require_reviewed = args.require_reviewed
        settings.allow_unresolved = args.allow_unresolved
        settings.legacy_table = os.path.abspath(args.legacy_table) if args.legacy_table else None
        settings.xref_fixes = read_xref_fixes(args.xref_fixes)
        settings.legacy_map = read_legacy_map(args.legacy_map)
        settings.pandoc = find_pandoc(args.pandoc, args.allow_tool_versions, report)
        sources = {}
        for path in files:
            source = os.path.relpath(path, settings.source_base).replace(os.sep, "/")
            if source.startswith("../"):
                fail("%s is outside --base %s" % (path, settings.source_base))
            sources[source] = path
        outputs = {}
        if args.pages:
            for row in read_tsv(args.pages, ("source", "output")):
                outputs[row["source"]] = row["output"] or None
        pages = []
        for source in sorted(sources):
            if source in outputs:
                output = outputs[source]
            else:
                folder, name = posixpath.split(source)
                output = posixpath.join(folder, page_name(posixpath.splitext(name)[0]) + ".md")
            pages.append(Page(source, output))
        seen = {}
        for page in pages:
            if page.output and page.output in seen:
                fail("%s and %s are both written to %s" % (seen[page.output], page.source, page.output))
            seen[page.output] = page.source
        settings.unconverted = [page.source for page in pages if not page.output]
        pages = [page for page in pages if page.output]
        api, trees = docbook_to_trees([(page, sources[page.source]) for page in pages], settings, report)
        for page, tree, has_part in trees:
            load_page(page, tree, has_part, settings, report)
        status = finish(pages, api, settings, report)
    except ConvertError as error:
        report.error(str(error))
        status = 2
    report.print(sys.stdout, time.time() - started)
    return status


# ---------------------------------------------------------------------------

def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    argv = sys.argv[1:] if argv is None else argv
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument("--out", required=True, metavar="DIR", help="output folder, laid out as docs/")
    common.add_argument("--pandoc", metavar="PATH", help="pandoc executable (default: pandoc on PATH)")
    common.add_argument("--seed-code-table", action="store_true", help="add or re-seed code-block table rows instead of failing on them")
    common.add_argument("--require-reviewed", action="store_true", help="fail when a code-block table row is not reviewed")
    common.add_argument("--allow-unresolved", action="store_true", help="write the output even with unresolved references")
    common.add_argument("--allow-tool-versions", action="store_true", help="run with pandoc or Asciidoctor versions other than the pins")

    if argv and argv[0] == "docbook":
        parser = argparse.ArgumentParser(prog="convert.py docbook", parents=[common],
                                         description="Convert DocBook files to GitHub-flavoured Markdown (the shared DocBook stage).")
        parser.add_argument("files", nargs="+", metavar="FILE.xml")
        parser.add_argument("--base", metavar="DIR", help="folder that page sources, links and images are relative to (default: the files' common folder)")
        parser.add_argument("--pages", metavar="TSV", help="page map with columns source, output (default: the kebab-case name of each file)")
        parser.add_argument("--code-table", metavar="TSV", help="code-block language table (default: heuristic tags, no table)")
        parser.add_argument("--xref-fixes", metavar="TSV", help="corrections for references that do not resolve")
        parser.add_argument("--match-heading-text", action="store_true", help="also resolve a fragment by heading text, '_' read as space")
        parser.add_argument("--global-ids", action="store_true", help="resolve a fragment-only reference by a heading id in any page")
        parser.add_argument("--legacy-html", action="store_true", help="rewrite links to legacy *.html reference pages by the built-in rule")
        parser.add_argument("--legacy-map", metavar="TSV", help="targets for legacy pages the built-in rule does not place, consulted first")
        parser.add_argument("--legacy-table", metavar="TSV", help="write the legacy link list here (with --legacy-html)")
        return convert_docbook(parser.parse_args(argv[1:]))

    parser = argparse.ArgumentParser(prog="convert.py", parents=[common],
                                     description="Convert Documentation/ (AsciiDoc) to GitHub-flavoured Markdown. "
                                                 "`convert.py docbook --help` describes the standalone DocBook stage.")
    parser.add_argument("--work", metavar="DIR", help="folder for the intermediate DocBook (default: a temporary folder, deleted afterwards)")
    parser.add_argument("--keep-work", action="store_true", help="keep the temporary folder and print its path")
    return convert_asciidoc(parser.parse_args(argv))


if __name__ == "__main__":
    sys.exit(main())
