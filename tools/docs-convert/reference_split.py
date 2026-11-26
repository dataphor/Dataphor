#!/usr/bin/env python3
"""Reshape the legacy DocBook reference material into one document per documentation page.

`convert.py`'s DocBook stage writes one page per input file and fails when two sources resolve to
the same output, so the 300-odd operator pages of `SystemOperators.xml` cannot come out of it
directly and two sections cannot be merged onto one page. This script reshapes the input first,
leaving that invariant alone: it reads the reference DocBook, fills in or removes the
generated-content placeholders the old publishing tool left behind, groups sections onto pages,
and writes one DocBook document per page for `convert.py docbook` to convert.

    reference_split.py declarations <decompiled-chm-dir> --out declarations.tsv
    reference_split.py split FILE.xml ... --seed-pages reference-pages.tsv
    reference_split.py split FILE.xml ... --pages TSV --declarations TSV --out WORKDIR
    reference_split.py split FILE.xml ... --pages TSV --facts TSV --include FILE.xml --out WORKDIR

See README.md in this folder for the tables, the commands, why the signatures come from the
published help file rather than from a live catalog, and why the library facts do not.
"""

import argparse
import collections
import html
import os
import posixpath
import re
import sys
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import convert  # noqa: E402  -- the tab-separated table dialect and the legacy page rule

PAGES_COLUMNS = ("id", "output", "title", "reason")
DECLARATIONS_COLUMNS = ("id", "declaration")
FACTS_COLUMNS = ("id", "field", "value", "detail", "reason")
WORK_PAGES_COLUMNS = ("source", "output")

# A page-map `output` that removes the section and everything under it instead of placing it on a
# page. The library chapters carry sections whose whole content was generated from a live catalog
# (the *Schema* lists) or was never written (*Connectivity*, *Translation*); a heading with
# nothing under it reads as a documentation gap rather than as a block that was never filled.
DROP = "(drop)"

# The DocBook elements that carry an id and can become a page or a section of one.
SECTIONS = ("section", "sect1", "sect2", "sect3", "sect4", "sect5", "simplesect",
            "chapter", "part", "appendix", "preface", "article", "refentry")

# Per-section editing metadata (a last-modified date). The old publishing tool dropped these
# (`droptags="sectioninfo"` in its ExtDoc directives) and so does this one.
INFO = re.compile(r"^(?:section|sect[1-5]|chapter|part|appendix|preface|article|refentry|book)info$")

# Generated-content placeholders, all of them processing instructions. `<?DocLib D4Field
# Declaration?>` inside a formalpara titled "Declarations" is an operator or type signature
# block, filled from declarations.tsv. Every other placeholder stood either for a fact this
# conversion does not take from the help file (library versions, requisites, file lists) or for
# prose the authors never wrote, and is removed.
DOCLIB = "DocLib"
XM_REPLACE = "xm-replace_text"
DECLARATION_FIELD = "D4Field Declaration"
DECLARATIONS_TITLE = "Declarations"

# The generated blocks of a library chapter's *Definition* section: the catalog table the old
# publishing tool read each from, the kind of library-facts row that replaces it, and the
# `<?DocLib D4Field <column>?>` placeholders of its template, in the order the facts table's
# `value` and `detail` columns fill them.
FACT_BLOCKS = {
    "LibraryRequisites": ("requisite", ("Required_Library_Name", "Required_Library_Version")),
    "LibraryFiles": ("file", ("FileName", "IsAssembly")),
    "RegisteredAssemblies": ("assembly", ("SimpleName",)),
    "RegisteredClasses": ("class", ("SimpleName",)),
}
FACT_FIELDS = ("library", "version") + tuple(kind for kind, _ in FACT_BLOCKS.values())

# `<?DocLib D4ExpressionBlock <wrapper> <table> …?>` introduces a repeating block: the element
# after it holds one `<wrapper>` carrying the placeholders of a single catalog row.
EXPRESSION_BLOCK = re.compile(r"^D4ExpressionBlock\s+(\S+)\s+(\S+)")
D4_FIELD = re.compile(r"^D4Field\s+(\S+)$")
# The library version is written inline, as a field in `SecurityLibraryReference.xml` and as an
# expression field in the other nine chapters.
VERSION_FIELD = re.compile(r"^D4(?:Expression)?Field\s+Version\b")
LIBRARY_NAME_FIELD = "D4Field LibraryName"
# `<?DocLib ExtDoc … <path>?>` is a splice point for a separate source file; the paths in the
# sources are absolute ones from the 2007 build machine, so only the file name is usable.
EXTDOC = re.compile(r"^(?:D4)?ExtDoc\b.*?([^\s\\/]+\.xml)\s*$", re.IGNORECASE)

# Removing a placeholder can leave its enclosing blocks with nothing to render. Only the
# ancestors of a removed placeholder are pruned, so hand-written content is left alone.
PRUNABLE = ("para", "simpara", "formalpara", "programlisting", "literallayout", "listitem",
            "itemizedlist", "orderedlist", "variablelist", "varlistentry", "example",
            "informaltable", "table", "tgroup", "tbody", "row")
# Elements that are content even with no text of their own.
SUBSTANTIVE = ("imagedata", "graphic", "inlinemediaobject", "mediaobject", "xref", "anchor",
               "ulink", "link", "olink")

LEGACY_SUFFIX = re.compile(r"\.html?$", re.IGNORECASE)
# A page-map key that names a source file instead of a section id, which is how a file whose
# root section carries no id is addressed. No DocBook id in the sources ends in `.xml`.
FILE_KEY = re.compile(r"\.xml$", re.IGNORECASE)


def xml_parser():
    """ElementTree discards processing instructions unless the parser is built this way, and
    every placeholder this script has to handle is one. Losing them would look like success and
    would drop every signature."""
    return ET.XMLParser(target=ET.TreeBuilder(insert_comments=True, insert_pis=True))


def read_xml(path):
    """The parsed root of a reference source, with its DOCTYPE dropped.

    Seven of the reference sources declare a DOCTYPE naming a DTD path that no longer exists.
    None of them needs it - each parses as it stands and uses only the predefined entities - and
    a DOCTYPE copied into the split output would be followed by pandoc.

    That is not true of every file in `Docs2.0/`: four of the book parts use `&nbsp;`, and
    `DataphorSet.xml` and `DataphorSet2.xml` declare the set's file includes in their own
    internal subset, so all six fail to parse here. None of them is a reference source, so
    nothing this script converts needs an entity; a later ticket that wants one of those files
    would have to keep the internal subset or predefine the entity."""
    with open(path, "rb") as stream:
        raw = stream.read().decode("utf-8")
    raw = re.sub(r"<!DOCTYPE[^>\[]*(?:\[.*?\])?\s*>", "", raw, count=1, flags=re.S)
    try:
        return ET.fromstring(raw, parser=xml_parser())
    except ET.ParseError as error:
        convert.fail("%s: %s" % (path, error))


def is_tag(element, name=None):
    return isinstance(element.tag, str) and (name is None or element.tag == name)


def title_element(element):
    """A copy of the element's <title> with the index terms stripped, or None.

    Index terms are an artefact of the printed index; pandoc already reads them as empty, and a
    page title is the one place where leaving them in would be visible."""
    title = element.find("title")
    if title is None:
        return None
    clone = ET.Element("title")
    clone.text = title.text
    for child in title:
        if is_tag(child, "indexterm"):
            if child.tail:
                append_text(clone, child.tail)
            continue
        clone.append(child)
    return clone


def append_text(element, text):
    children = list(element)
    if children:
        children[-1].tail = (children[-1].tail or "") + text
    else:
        element.text = (element.text or "") + text


def collapse_title(title):
    """Whitespace inside a title collapsed to single spaces and trimmed at both ends.

    The sources lay a title out over several indented lines around its index terms, so a title
    taken straight from one reaches the page as a heading with leading spaces."""
    nodes = [(title, "text")] + [(child, "tail") for child in title]
    for node, field in nodes:
        value = getattr(node, field)
        if value:
            setattr(node, field, re.sub(r"\s+", " ", value))
    node, field = nodes[0]
    if getattr(node, field):
        setattr(node, field, getattr(node, field).lstrip())
    node, field = nodes[-1]
    if getattr(node, field):
        setattr(node, field, getattr(node, field).rstrip())


def title_text(element):
    """The element's title as plain text, whitespace collapsed: the page map's review column and
    the text of a generated navigation link."""
    title = title_element(element)
    if title is None:
        return ""
    return re.sub(r"\s+", " ", "".join(title.itertext())).strip()


def sections_of(root):
    """[(element, nearest enclosing section or None)] for every section-like element in document
    order, the root included."""
    found = []

    def walk(element, enclosing):
        if is_tag(element) and element.tag in SECTIONS:
            found.append((element, enclosing))
            enclosing = element
        for child in element:
            if is_tag(child):
                walk(child, enclosing)

    walk(root, None)
    return found


# ---------------------------------------------------------------------------
# declarations: the signature blocks of the published help file

HELP_PAGE = re.compile(r"^[OT]-.+\.html$", re.IGNORECASE)
PAGE_ID = re.compile(r'<h1 class="title"><a name="([^"]+)"></a>', re.IGNORECASE)
# The DocBook XSL of the day wrote a formalpara as `<p><b>Title.&nbsp;</b>` then its content.
FORMALPARA = re.compile(r"<p><b>([^<]*?)\.&nbsp;</b>", re.IGNORECASE)
PROGRAMLISTING = re.compile(r'<pre class="programlisting">(.*?)</pre>', re.S | re.IGNORECASE)
BLOCK_END = re.compile(r"<h[1-6][ >]|<div\b|<p><b>[^<]*?\.&nbsp;</b>", re.IGNORECASE)


def declaration_of(markup):
    """The D4 script of a help page's Declarations block, or None when it has none.

    The block runs from the "Declarations." formalpara marker to the next heading, division or
    formalpara, and holds one <pre> per signature. They are joined into one script because a
    Markdown page wants one fenced block, and the printed page showed them adjacent."""
    for match in FORMALPARA.finditer(markup):
        if match.group(1).strip() != DECLARATIONS_TITLE:
            continue
        rest = markup[match.end():]
        end = BLOCK_END.search(rest)
        block = rest[: end.start()] if end else rest
        listings = [html.unescape(text).strip("\r\n") for text in PROGRAMLISTING.findall(block)]
        listings = [text for text in listings if text.strip()]
        return "\n".join(listings) or None
    return None


def escape_declaration(text):
    r"""A declaration as one table cell: a D4 script is several tab-indented lines, and the
    tables are tab-separated with no quoting."""
    return (text.replace("\\", "\\\\").replace("\t", "\\t")
                .replace("\r", "\\r").replace("\n", "\\n"))


def unescape_declaration(cell):
    out = []
    index = 0
    while index < len(cell):
        char = cell[index]
        if char != "\\":
            out.append(char)
            index += 1
            continue
        following = cell[index + 1] if index + 1 < len(cell) else ""
        out.append({"n": "\n", "t": "\t", "r": "\r", "\\": "\\"}.get(following, "\\" + following))
        index += 2
    return "".join(out)


def extract_declarations(folder, out_path, report):
    if not os.path.isdir(folder):
        convert.fail("not a folder: %s" % folder)
    rows = []
    seen = {}
    pages = 0
    for name in sorted(os.listdir(folder)):
        if not HELP_PAGE.match(name):
            continue
        pages += 1
        with open(os.path.join(folder, name), "rb") as stream:
            # The help pages declare ISO-8859-1, a single-byte encoding, so the decode cannot
            # fail and cannot mis-read a D4 script.
            markup = stream.read().decode("iso-8859-1")
        found = PAGE_ID.search(markup)
        if not found:
            report.add("help-pages-without-a-section-id", name)
            continue
        declaration = declaration_of(markup)
        if declaration is None:
            report.add("help-pages-without-a-declarations-block", found.group(1))
            continue
        identifier = found.group(1)
        if identifier in seen:
            convert.fail("section id %s appears on two help pages (%s, %s)"
                         % (identifier, seen[identifier], name))
        seen[identifier] = name
        cell = escape_declaration(declaration)
        # The escaping is this table's only real risk, so every row is decoded and compared
        # against what was extracted before the table is written.
        if unescape_declaration(cell) != declaration:
            convert.fail("the declaration of %s does not round-trip through the table" % identifier)
        rows.append({"id": identifier, "declaration": cell})
    rows.sort(key=lambda row: row["id"])
    convert.write_tsv(out_path, DECLARATIONS_COLUMNS, rows)
    report.summary.append("read %d O-*/T-* help page(s); wrote %d declaration(s) to %s"
                          % (pages, len(rows), out_path))
    return 0


def read_declarations(path):
    if not path:
        return {}
    table = {}
    for row in convert.read_tsv(path, DECLARATIONS_COLUMNS):
        if row["id"] in table:
            convert.fail("%s line %d repeats %s" % (os.path.basename(path), row["_line"], row["id"]))
        table[row["id"]] = unescape_declaration(row["declaration"])
    return table


def read_facts(path):
    """chapter id -> {field: [(value, detail)]}, in the table's own row order.

    The row order is the order the requisites, files, assemblies and classes are listed in on the
    page, so moving a row moves a list entry. `detail` is the second value of a two-column block
    (a requisite's version, whether a file is an assembly) and is empty for the one-column ones;
    the `reason` column records where the fact was read from and is not used here."""
    if not path:
        return {}
    table = collections.OrderedDict()
    for row in convert.read_tsv(path, FACTS_COLUMNS):
        field = row["field"].strip()
        if field not in FACT_FIELDS:
            convert.fail("%s line %d: field %r is not one of %s"
                         % (os.path.basename(path), row["_line"], field, ", ".join(FACT_FIELDS)))
        fields = table.setdefault(row["id"].strip(), collections.OrderedDict())
        fields.setdefault(field, []).append((row["value"].strip(), row["detail"].strip()))
    return table


def read_includes(paths):
    """lower-case file name -> the parsed root of each file `--include` names."""
    includes = collections.OrderedDict()
    for path in paths or ():
        name = os.path.basename(path).lower()
        if name in includes:
            convert.fail("--include names %s twice" % os.path.basename(path))
        includes[name] = read_xml(path)
    return includes


# ---------------------------------------------------------------------------
# split: the page map

def legacy_name(identifier):
    """The help-file page name of a section id, or None for a key that names a source file.

    A few ids in the sources carry the `.html` suffix already (`O-System.iInListOperator.html`),
    a source defect the links repeat. A key that names a file names no help page: the files
    addressed that way are the 2004 `OperatorDocs/` ones, which the published help file, built
    from the 2007 sources, never carried."""
    if FILE_KEY.search(identifier):
        return None
    return identifier if LEGACY_SUFFIX.search(identifier) else identifier + ".html"


def rule_output(identifier):
    """The page `convert.py`'s built-in legacy rule already promises for this section's help
    page, so a seeded map agrees with the links under docs/ that point at it. Empty when the
    section has no help page at all, or none the rule describes."""
    name = legacy_name(identifier)
    return (convert.legacy_target(name) or "") if name else ""


def name_root_section(root, source, pages, report):
    """Give a file's root section the file's own name as its id, when it has none and the page
    map carries a row for that name.

    The 2004 `OperatorDocs/` files are one operator per file and carry no id anywhere, so the
    file name is the only thing a page map can key on. It is opt-in by row because an id-less
    root is not always a page: `SystemOperators.xml`'s root section is the wrapper holding the
    339 operator sections, has no id either, and stays unconverted."""
    if is_tag(root) and root.tag in SECTIONS and not root.get("id") and source in pages:
        root.set("id", source)
        report.add("root-sections-addressed-by-their-file-name", source)


def link_names(pages):
    """output -> (the help-page name a generated link to that page uses, its link text).

    The name is None for a page addressed by a file name, which has no help page; such a page
    is never in a generated list, because the row reaches a file's root section and no enclosing
    section can cut it out.

    A merged page holds several sections, and only one of their help-page names is the one the
    built-in rule sends to that page - the one the links under docs/ already use. Linking by
    that name keeps the generated navigation out of `--legacy-map`; a page with no such section
    is reported, because it is the one case that does need a row there."""
    # NOTE: the preference below is between ids whose rule matches the output; a file-keyed id has
    # no help page at all, so if one were ever merged onto a page that also holds id-keyed sections
    # it could win and carry a None name into a generated list. It cannot today - the only
    # file-keyed page is one file root, which no enclosing section can cut out - so prefer a named
    # id here if a merge of that shape ever appears.
    chosen = {}
    for identifier, row in pages.items():
        output = row["output"]
        if not output or output == DROP:
            continue
        held = chosen.get(output)
        if held is None or (rule_output(identifier) == output and rule_output(held) != output):
            chosen[output] = identifier
    return {output: (legacy_name(identifier), pages[identifier]["title"] or identifier)
            for output, identifier in chosen.items()}


def seed_pages(files, out_path, report):
    rows = []
    for path in files:
        for element, _ in sections_of(read_xml(path)):
            identifier = element.get("id")
            if identifier:
                rows.append({"id": identifier, "output": rule_output(identifier),
                             "title": title_text(element), "reason": ""})
    convert.write_tsv(out_path, PAGES_COLUMNS, rows)
    unplaced = sum(1 for row in rows if not row["output"])
    report.summary.append("seeded %d row(s) to %s; %d have no output and need one by hand"
                          % (len(rows), out_path, unplaced))
    return 0


def read_pages(path):
    """id -> {output, title, order}, in the table's own row order.

    The row order matters: the sections of one page are laid out in it, so the first row for a
    page decides the page's title. A seeded table is in document order, which is the order the
    printed reference had; moving a row is how a merged page is given its primary section.

    The `reason` column is for the reader - why a row was moved, merged or left unconverted -
    and is not used here."""
    table = collections.OrderedDict()
    for order, row in enumerate(convert.read_tsv(path, PAGES_COLUMNS)):
        if row["id"] in table:
            convert.fail("%s line %d repeats %s" % (os.path.basename(path), row["_line"], row["id"]))
        table[row["id"]] = {"output": row["output"].strip(), "title": row["title"].strip(),
                            "order": order}
    return table


# ---------------------------------------------------------------------------
# split: placeholders

def placeholder(node):
    """(target, rest) for a generated-content placeholder, or None for anything else."""
    if node.tag is not ET.ProcessingInstruction:
        return None
    text = (node.text or "").strip()
    for target in (DOCLIB, XM_REPLACE):
        if text == target or text.startswith(target + " "):
            return target, text[len(target):].strip()
    return None


def parents_of(root):
    return {id(child): element for element in root.iter() for child in element}


def enclosing_formalpara(node, parents, title):
    """The formalpara with this title that encloses the node, or None."""
    current = parents.get(id(node))
    while current is not None:
        if is_tag(current, "formalpara"):
            return current if title_text(current) == title else None
        current = parents.get(id(current))
    return None


def enclosing_section_id(node, parents, fallback):
    """The id of the nearest section around the node, or `fallback` when it has none.

    A page can hold sections that are not pages of their own - a library chapter's operator
    sections are nested under their category - and a declaration block belongs to the section it
    sits in, not to the page's first section. For a page whose sections are all pages, which is
    every page of the core-library reference, this is that page's own id."""
    current = parents.get(id(node))
    while current is not None:
        if is_tag(current) and current.tag in SECTIONS and current.get("id"):
            return current.get("id")
        current = parents.get(id(current))
    return fallback


def has_content(element):
    if (element.text or "").strip():
        return True
    for child in element.iter():
        if child is element:
            continue
        if is_tag(child) and child.tag in SUBSTANTIVE:
            return True
        if (child.text or "").strip() or (child.tail or "").strip():
            return True
    return False


def is_spent(element):
    """True when the element has nothing left to render. The table cases are spelled out because
    a generated table keeps its hand-written column headings after its only row is removed, and
    a table of headings alone is not content."""
    if not is_tag(element):
        return False
    if element.tag == "row":
        # A cell is not removed on its own, or the row's remaining cells would shift column.
        return not any(has_content(cell) for cell in element)
    if element.tag == "tbody":
        return not len(element)
    if element.tag == "tgroup":
        return element.find("tbody") is None
    if element.tag in ("informaltable", "table"):
        return element.find("tgroup") is None
    if element.tag == "formalpara":
        return all(is_tag(child, "title") or not has_content(child) for child in element)
    return element.tag in PRUNABLE and not has_content(element)


def prune(chunk, spent):
    """Drop each spent element and then any enclosing block it leaves with nothing to render.

    Walks up from each spent element one level at a time, so a block is judged after its
    children have gone. Only the ancestors of a removed placeholder are visited, which is why
    hand-written content elsewhere is never touched."""
    level = list(spent)
    while level:
        parents = parents_of(chunk)
        above = {}
        for element in level:
            parent = parents.get(id(element))
            if parent is None or element is chunk:
                continue
            if is_spent(element):
                parent.remove(element)
            above[id(parent)] = parent
        level = list(above.values())


def doclib(node):
    """The text of a `<?DocLib …?>` instruction after its target, or None for anything else."""
    kind = placeholder(node)
    return kind[1] if kind is not None and kind[0] == DOCLIB else None


def replace_with_text(parent, node, text):
    """Put `text` where a child stood, keeping the text that followed it.

    ElementTree holds the text after a child on that child's `tail` and the text before the first
    child on the parent's `text`, so a value substituted into the middle of a sentence has to land
    in one of those two places and not merely at the end of the parent."""
    children = list(parent)
    index = children.index(node)
    carry = text + (node.tail or "")
    parent.remove(node)
    if index == 0:
        parent.text = (parent.text or "") + carry
    elif carry:
        before = children[index - 1]
        before.tail = (before.tail or "") + carry


def next_element(parent, node):
    """The element following the node among its parent's children, or None.

    A generated block is an instruction followed by the formalpara or section holding its
    template, which is how the old publishing tool paired the two."""
    children = list(parent)
    for child in children[children.index(node) + 1:]:
        if is_tag(child):
            return child
    return None


def template_in(container, tag):
    """(parent, the one `tag` child holding placeholders) inside the container, or (None, None)."""
    for element in container.iter():
        if not is_tag(element):
            continue
        for child in element:
            if is_tag(child, tag) and any(placeholder(node) for node in child.iter()):
                return element, child
    return None, None


def fill_block(container, wrapper, columns, values, report, where):
    """Repeat the container's template element once per fact, filling its field placeholders.

    The sources carry a single row or member holding `<?DocLib D4Field <column>?>` placeholders -
    the template the old tool repeated per catalog row. Repeating it rather than building a block
    keeps the hand-written column headings and the list's column count."""
    parent, template = template_in(container, wrapper)
    if template is None:
        report.add("generated-blocks-with-no-template", "%s: %s" % (where, wrapper))
        return 0
    index = list(parent).index(template)
    tail = template.tail
    parent.remove(template)
    for offset, value in enumerate(values):
        copy = copy_without(template, {})
        copy.tail = tail
        fields = dict(zip(columns, value))
        parents = parents_of(copy)
        for node in list(copy.iter()):
            rest = doclib(node)
            field = D4_FIELD.match(rest) if rest else None
            if field is not None and field.group(1) in fields:
                replace_with_text(parents[id(node)], node, fields[field.group(1)])
        parent.insert(index + offset, copy)
    return len(values)


def fill_library_facts(chunk, identifier, facts, report):
    """Fill a library chapter's version, requisites, files, registered assemblies and registered
    classes from the reviewed library-facts table, and write the library's name where the sources
    left a placeholder for it.

    The old publishing tool read all five from a live catalog, and the values in the published help
    file are the 2.1 ones - its `DRSimpleDeviceLibrary-Definition.html` gives version 2.1.0.0 where
    the shipped descriptor says 3.0.0.0 - so unlike the operator signatures these do not come from
    that file. They come from the library's own `.d4l` descriptor and its `Register.cs`, by way of
    `library-facts.tsv`; see README.md. A block the table has no row for is removed with its
    heading, as an unfilled generated block is."""
    rows = facts.get(identifier)
    if rows is None:
        return 0
    filled = 0
    name = rows.get("library", [("", "")])[0][0]
    version = rows.get("version", [("", "")])[0][0]
    parents = parents_of(chunk)
    for node in list(chunk.iter()):
        rest = doclib(node)
        parent = parents.get(id(node))
        if rest is None or parent is None:
            continue
        if rest == LIBRARY_NAME_FIELD:
            replace_with_text(parent, node, name)
            filled += 1
            continue
        if VERSION_FIELD.match(rest):
            replace_with_text(parent, node, version)
            filled += 1
            continue
        block = EXPRESSION_BLOCK.match(rest)
        if block is None or block.group(2) not in FACT_BLOCKS:
            continue
        kind, columns = FACT_BLOCKS[block.group(2)]
        container = next_element(parent, node)
        replace_with_text(parent, node, "")
        if container is None:
            report.add("generated-blocks-with-nothing-to-fill", "%s: %s" % (identifier, kind))
            continue
        values = rows.get(kind, [])
        if not values:
            parent.remove(container)
            report.add("generated-blocks-removed-for-want-of-a-fact", "%s: %s" % (identifier, kind))
            continue
        filled += fill_block(container, block.group(1), columns, values, report, identifier)
    return filled


def splice_includes(chunk, includes, spliced):
    """Replace an `<?DocLib ExtDoc … FILE.xml?>` instruction with the content of that file, for
    each file `--include` names.

    These instructions are the splice points where the old publishing tool pulled in a separate
    source. The per-library operator and type files are reached through the page map instead, which
    is what gives their sections their own declaration blocks, so they are not included here; the
    one file that needs this is `SQLDeviceSettings.xml`, the hand-written table of the settings
    every SQL device accepts, which is a bare fragment with no section of its own and so no page
    map can address it. An instruction naming a file that was not included is removed like any
    other placeholder."""
    parents = parents_of(chunk)
    for node in list(chunk.iter()):
        rest = doclib(node)
        parent = parents.get(id(node))
        if rest is None or parent is None:
            continue
        match = EXTDOC.match(rest)
        if match is None or match.group(1).lower() not in includes:
            continue
        name = match.group(1).lower()
        content = copy_without(includes[name], {})
        content.tail = node.tail
        index = list(parent).index(node)
        parent.remove(node)
        parent.insert(index, content)
        spliced.add(name)


def substitute_placeholders(chunk, identifier, declarations, report):
    """Fill in or remove every placeholder in the chunk."""
    filled = 0
    spent = []
    for node in list(chunk.iter()):
        kind = placeholder(node)
        if kind is None:
            continue
        # The tree is mutated as we go, so the parent map is re-derived each time, and a node
        # whose block has already been dropped is simply skipped.
        # NOTE: that is one walk of the chunk per placeholder; the largest chunk here is a
        # library chapter with about 36 of them. If a future source brings hundreds into one
        # section, collect the placeholders and their parents in a single walk first.
        parents = parents_of(chunk)
        parent = parents.get(id(node))
        if parent is None:
            continue
        if kind == (DOCLIB, DECLARATION_FIELD):
            section = enclosing_section_id(node, parents, identifier)
            formalpara = enclosing_formalpara(node, parents, DECLARATIONS_TITLE)
            if formalpara is None:
                report.add("declarations-placeholder-outside-a-declarations-block", section)
            elif not is_tag(parent, "programlisting"):
                report.add("declarations-placeholder-not-in-a-programlisting", section)
            elif section in declarations:
                # The placeholder is the whole content of the formalpara's <programlisting>; the
                # signature block replaces it there, under the "Declarations" heading.
                parent.remove(node)
                parent.text = declarations[section]
                filled += 1
                continue
            else:
                # Nothing was published for this section: drop the heading too, rather than
                # leave a "Declarations" block with nothing under it.
                report.add("sections-with-no-published-declaration", section)
                grandparent = parents.get(id(formalpara))
                if grandparent is not None:
                    grandparent.remove(formalpara)
                    spent.append(grandparent)
                continue
        external = EXTDOC.match(kind[1]) if kind[0] == DOCLIB else None
        if external is not None:
            # Listed because the splice point is where a whole separate source used to arrive:
            # a reviewer has to agree that the page does not want it. See splice_includes.
            report.add("external-document-instructions-removed",
                       "%s: %s" % (enclosing_section_id(node, parents, identifier),
                                   external.group(1)))
        tail = node.tail
        parent.remove(node)
        if tail and tail.strip():
            append_text(parent, tail)
        spent.append(parent)
    prune(chunk, spent)
    return filled


def tidy_section_titles(chunk):
    """Strip the index terms and the source's line breaks from every section title in the chunk.

    A page's own title goes through `title_for`, which already does this; the title of a section
    that is not a page of its own - a library chapter's Definition, or an operator under its
    category - comes straight from the source and needs the same treatment."""
    for element in chunk.iter():
        if not (is_tag(element) and element.tag in SECTIONS):
            continue
        for index, child in enumerate(list(element)):
            if not is_tag(child, "title"):
                continue
            tidy = title_element(element)
            collapse_title(tidy)
            tidy.tail = child.tail
            element.remove(child)
            element.insert(index, tidy)
            break


def prune_informal_headings(chunk, report, where):
    """Drop a `<bridgehead>` that removing a placeholder left with nothing under it.

    A bridgehead heads the blocks that follow it, up to the next bridgehead or the next section,
    so one with nothing in between is an empty heading. Every library chapter's *See Also* in its
    Definition section is one: the list of links there was never written and stood as a
    placeholder, whose removal leaves the heading behind."""
    for parent in list(chunk.iter()):
        if not is_tag(parent):
            continue
        children = list(parent)
        for index, child in enumerate(children):
            if not is_tag(child, "bridgehead"):
                continue
            under = []
            for following in children[index + 1:]:
                if not is_tag(following):
                    continue            # a comment does not end the run of blocks
                if following.tag == "bridgehead" or following.tag in SECTIONS:
                    break
                under.append(following)
            if not any(has_content(element) for element in under):
                parent.remove(child)
                report.add("informal-headings-with-nothing-under-them",
                           "%s: %s" % (where, "".join(child.itertext()).strip()))


def normalise_urls(chunk, report):
    """Strip the whitespace some url attributes carry, and restore the suffix a legacy page name
    lost, so the link resolves like every other one."""
    for element in chunk.iter():
        if not is_tag(element):
            continue
        url = element.get("url")
        if url is None:
            continue
        fixed = url.strip()
        path, hash_, fragment = fixed.partition("#")
        if path and not re.match(r"^[A-Za-z][A-Za-z0-9+.-]*:", path) \
                and "." not in posixpath.basename(path):
            fixed = path + ".html" + hash_ + fragment
        if fixed != url:
            element.set("url", fixed)
            report.add("urls-normalised", "%r -> %s" % (url, fixed))


# ---------------------------------------------------------------------------
# split: pages

class Report:
    def __init__(self):
        self.lines = collections.defaultdict(collections.Counter)
        self.summary = []
        self.errors = []

    def add(self, section, line):
        self.lines[section][line] += 1

    def error(self, line):
        self.errors.append(line)

    def print(self, stream):
        for line in self.summary:
            print(line, file=stream)
        for section in sorted(self.lines):
            counter = self.lines[section]
            print("\n%s (%d):" % (section, sum(counter.values())), file=stream)
            for line, count in sorted(counter.items()):
                print("  %s%s" % (line, " (x%d)" % count if count > 1 else ""), file=stream)
        for line in self.errors:
            print("\nERROR: %s" % line, file=stream)


def work_name(output):
    """The split's file name for a page: the output path flattened, so the work folder reads
    like the page map. Flattening can in principle bring two outputs to one name, which would
    lose a page silently - `convert.py docbook --pages` keeps the last row for a repeated
    source - so `split` checks the names it has built before writing any of them."""
    return re.sub(r"\.md$", "", re.sub(r"[\\/]+", "-", output)) + ".xml"


def copy_without(element, cut):
    """A deep copy of the element, leaving out the descendants in `cut` and the `*info` blocks."""
    clone = ET.Element(element.tag, dict(element.attrib)) if is_tag(element) else ET.Element(element.tag)
    clone.text = element.text
    clone.tail = element.tail
    for child in element:
        if id(child) in cut or (is_tag(child) and INFO.match(child.tag)):
            continue
        clone.append(copy_without(child, cut))
    return clone


def link_list(children):
    """The navigation list of a category page: one link per section that went to a page of its
    own, written as a link to a help page so it resolves by the same rules as every other
    reference link. A category's own prose is an unwritten placeholder in the sources, so this
    list is all such a page has, and no prose is invented for it."""
    listing = ET.Element("itemizedlist")
    listing.text = "\n"
    for name, text in children:
        item = ET.SubElement(listing, "listitem")
        item.tail = "\n"
        para = ET.SubElement(item, "para")
        link = ET.SubElement(para, "ulink", {"url": name})
        link.text = text or name
    return listing


def build_page(output, items, report):
    """One DocBook document for the page: the first section's id and title on an <article>, its
    body under it, and every further section of the page as a body section.

    The root is an <article> because pandoc's DocBook reader is certain to take its <title> as
    the document title, which is what makes the page's one `#` heading. NOTE: pandoc drops the
    root element's id, so a `#<id>` reference to a page's own title cannot resolve through
    `--global-ids`; the reference sources use no such reference (they link by help-page name
    through the legacy rule), but a later source that does would need the id kept."""
    first = items[0]
    article = ET.Element("article", {"id": first.identifier} if first.identifier else {})
    article.text = "\n"
    if first.title is not None:
        first.title.tail = "\n"
        article.append(first.title)
    else:
        report.add("pages-without-a-title", output)
    for child in first.chunk:
        if not is_tag(child, "title"):
            article.append(child)
    for item in items[1:]:
        item.chunk.tag = "section"
        item.chunk.tail = "\n"
        if item.title is not None:
            # The page map's title, so merged sections can be told apart under one page title.
            for existing in item.chunk.findall("title"):
                item.chunk.remove(existing)
            item.chunk.insert(0, item.title)
        article.append(item.chunk)
    # Checked on the tree, not on the markup: 50 of the sources' comments quote a DocLib
    # instruction in prose, and a comment is not a placeholder.
    left = [node.text for node in article.iter() if placeholder(node)]
    if left:
        report.error("%s: %d placeholder(s) survived, first <?%s?>" % (output, len(left), left[0][:90]))
    return '<?xml version="1.0" encoding="UTF-8"?>\n' + ET.tostring(article, encoding="unicode") + "\n"


class Item:
    """One section on its way to a page."""

    def __init__(self, identifier, title, chunk, order):
        self.identifier = identifier
        self.title = title
        self.chunk = chunk
        self.order = order


def title_for(element, row):
    """The <title> element a section contributes.

    The map's title wins, because a dozen of the reference titles are wrong in the sources (one
    duplicates its operator name, others disagree about the case of a qualifier) and because
    merging several sections onto one page needs the headings to tell them apart. When the map
    repeats the source title - which is what seeding writes - the source element is kept, so the
    few titles with inline markup keep it."""
    source = title_element(element)
    wanted = row["title"]
    if not wanted or (source is not None and title_text(element) == wanted):
        return source
    clone = ET.Element("title")
    clone.text = wanted
    return clone


def check_ids(found, pages, source, pages_path, seen, report):
    """Every section that has an id must have a row, and no id may be in two files; a section
    with no id cannot be mapped and stays inside its parent's page."""
    for element, _ in found:
        identifier = element.get("id")
        if not identifier:
            report.add("sections-without-an-id", "%s: %s" % (source, title_text(element) or "(untitled)"))
            continue
        if identifier in seen:
            report.error("section id %s is in both %s and %s" % (identifier, seen[identifier], source))
            continue
        seen[identifier] = source
        if identifier not in pages:
            report.error("%s: section id %s has no row in %s"
                         % (source, identifier, os.path.basename(pages_path)))


def holders_of(found, enclosing, has_page, source, report):
    """element id -> the nearest enclosing section that is a page of its own. A section that is
    not a page and has no such ancestor is not converted at all, and is listed."""
    holder = {}
    for element, parent in found:
        current = parent
        while current is not None and not has_page(current):
            current = enclosing[id(current)]
        holder[id(element)] = current
        if current is None and not has_page(element):
            report.add("sections-not-converted", "%s: %s"
                       % (source, element.get("id") or title_text(element) or "(untitled)"))
    return holder


def check_work_names(written, report):
    """No two pages may flatten to the same work file name; the second would overwrite the
    first and `pages.tsv` would name only one of them."""
    taken = {}
    for name, output, _ in written:
        if name in taken:
            report.error("%s and %s both flatten to %s" % (taken[name], output, name))
        taken[name] = output


def check_stale_documents(out_dir, written, report):
    """Documents a previous run left in the work folder that this one does not rewrite.

    `convert.py docbook` is given the folder's `*.xml`, and a source missing from `pages.tsv`
    gets an output from its file name instead - so a stale document from an earlier page map
    quietly becomes a page. Reported rather than deleted: the folder is the caller's."""
    if not os.path.isdir(out_dir):
        return
    fresh = {name for name, _, _ in written}
    for name in sorted(os.listdir(out_dir)):
        if name.lower().endswith(".xml") and name not in fresh:
            report.add("stale-documents-in-the-out-folder", name)


def check_dropped(found, is_dropped, has_page, source, report):
    """A dropped section takes its whole subtree with it, so a page inside one would be lost
    without a word; the row that asks for that is an error rather than a surprise."""
    for element, _ in found:
        if not is_dropped(element):
            continue
        for below, _ in sections_of(element):
            if below is not element and has_page(below):
                report.error("%s: %s is a page inside the dropped section %s"
                             % (source, below.get("id"), element.get("id")))


def split(files, pages_path, declarations_path, out_dir, report, facts_path=None, include=None):
    pages = read_pages(pages_path)
    declarations = read_declarations(declarations_path)
    facts = read_facts(facts_path)
    includes = read_includes(include)
    spliced = set()
    names = link_names(pages)
    for output, (name, _) in sorted(names.items()):
        if name is not None and convert.legacy_target(name) != output:
            report.add("pages-needing-a-legacy-map-row", "%s is linked as %s, which the rule sends to %s"
                       % (output, name, convert.legacy_target(name) or "no page"))

    def has_page(element):
        row = pages.get(element.get("id") or "")
        return bool(row and row["output"] and row["output"] != DROP)

    def is_dropped(element):
        row = pages.get(element.get("id") or "")
        return bool(row and row["output"] == DROP)

    items = collections.OrderedDict()   # output -> [Item]
    seen = {}
    filled = 0
    facts_filled = 0
    for path in files:
        source = os.path.basename(path)
        root = read_xml(path)
        name_root_section(root, source, pages, report)
        found = sections_of(root)
        check_ids(found, pages, source, pages_path, seen, report)
        check_dropped(found, is_dropped, has_page, source, report)
        if report.errors:
            continue
        enclosing = {id(element): parent for element, parent in found}
        holder = holders_of(found, enclosing, has_page, source, report)
        for element, _ in found:
            if not has_page(element):
                continue
            identifier = element.get("id")
            row = pages[identifier]
            output = row["output"]
            if rule_output(identifier) and output != rule_output(identifier):
                report.add("sections-moved-off-the-page-their-rule-names", "%s -> %s (the rule gives %s)"
                           % (identifier, output, rule_output(identifier)))
            cut, children, listed = {}, [], set()
            for other, _ in found:
                if other is element or holder[id(other)] is not element:
                    continue
                if is_dropped(other):
                    cut[id(other)] = other
                    report.add("sections-dropped", "%s: %s" % (source, other.get("id")))
                    continue
                if not has_page(other):
                    continue
                cut[id(other)] = other
                below = pages[other.get("id")]["output"]
                # One link per page, not per section: a category can hold two sections that
                # merge onto the same page.
                if below != output and below not in listed:
                    listed.add(below)
                    children.append(names[below])
            chunk = copy_without(element, cut)
            splice_includes(chunk, includes, spliced)
            facts_filled += fill_library_facts(chunk, identifier, facts, report)
            filled += substitute_placeholders(chunk, identifier, declarations, report)
            tidy_section_titles(chunk)
            prune_informal_headings(chunk, report, identifier)
            normalise_urls(chunk, report)
            if children:
                chunk.append(link_list(children))
            items.setdefault(output, []).append(
                Item(identifier, title_for(element, row), chunk, row["order"]))
    for identifier in sorted(set(pages) - set(seen)):
        report.add("page-map-rows-that-matched-nothing", identifier)
    for identifier in sorted(set(facts) - set(seen)):
        report.add("library-facts-rows-that-matched-nothing", identifier)
    for name in sorted(set(includes) - spliced):
        report.add("included-files-no-instruction-asked-for", name)
    if report.errors:
        return 1
    for group in items.values():
        group.sort(key=lambda item: item.order)
    written = [(work_name(output), output, build_page(output, items[output], report))
               for output in sorted(items, key=lambda output: items[output][0].order)]
    check_work_names(written, report)
    check_stale_documents(out_dir, written, report)
    if report.errors:
        return 1
    for name, _, xml in written:
        convert.write_file(os.path.join(out_dir, name), xml.encode("utf-8"))
    convert.write_tsv(os.path.join(out_dir, "pages.tsv"), WORK_PAGES_COLUMNS,
                      sorted(({"source": name, "output": output} for name, output, _ in written),
                             key=lambda row: row["source"]))
    report.summary.append("%d section(s) on %d page(s); %d declaration block(s) filled; "
                          "%d library fact(s) filled; written to %s"
                          % (sum(len(group) for group in items.values()), len(written), filled,
                             facts_filled, out_dir))
    return 0


# ---------------------------------------------------------------------------

def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    argv = sys.argv[1:] if argv is None else argv
    top = argparse.ArgumentParser(
        prog="reference_split.py",
        description="Reshape the legacy DocBook reference material into one document per page.")
    modes = top.add_subparsers(dest="mode", required=True)

    declarations = modes.add_parser("declarations", help="extract the signature blocks of the decompiled help file")
    declarations.add_argument("folder", metavar="DIR", help="folder holding the decompiled help file's pages")
    declarations.add_argument("--out", required=True, metavar="TSV", help="where to write the table")

    split_mode = modes.add_parser("split", help="write one DocBook document per documentation page")
    split_mode.add_argument("files", nargs="+", metavar="FILE.xml")
    split_mode.add_argument("--pages", metavar="TSV", help="page map with columns id, output")
    split_mode.add_argument("--declarations", metavar="TSV", help="signature table from the declarations mode")
    split_mode.add_argument("--facts", metavar="TSV", help="library facts read from the tree; columns id, field, value, detail")
    split_mode.add_argument("--include", metavar="FILE.xml", action="append",
                            help="splice this file in at the ExtDoc instruction that names it (repeatable)")
    split_mode.add_argument("--out", metavar="DIR", help="folder for the split documents and their page map")
    split_mode.add_argument("--seed-pages", metavar="TSV", help="write a seeded page map here instead of splitting")

    args = top.parse_args(argv)
    report = Report()
    try:
        if args.mode == "declarations":
            status = extract_declarations(args.folder, args.out, report)
        elif args.seed_pages:
            status = seed_pages(args.files, args.seed_pages, report)
        elif not args.pages or not args.out:
            convert.fail("split needs --pages and --out, or --seed-pages")
        else:
            status = split(args.files, args.pages, args.declarations, args.out, report,
                           facts_path=args.facts, include=args.include)
    except convert.ConvertError as error:
        report.error(str(error))
        status = 2
    report.print(sys.stdout)
    return status


if __name__ == "__main__":
    sys.exit(main())
