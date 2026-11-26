"""Tests for reference_split.py: the branches with judgement in them. None needs pandoc, Node
or the published help file.

Run with: python -m unittest discover -s tools/docs-convert
"""

import contextlib
import os
import tempfile
import unittest
import xml.etree.ElementTree as ET

import convert
import reference_split as split


SOURCE = """<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE section PUBLIC "-//OASIS//DTD DocBook XML V4.3//EN" "DocBookx.dtd" [
<!ENTITY nothing SYSTEM "Nothing.xml">
]>
<section id="O-Lib.MathOperators">
  <sectioninfo><date>Wednesday, June 13, 2007</date></sectioninfo>
  <title><indexterm><primary>Math</primary></indexterm>Math Operators</title>
  <para><?xm-replace_text {Paragraph}?></para>
  <section id="O-Lib.Abs">
    <title><indexterm><primary>Abs</primary></indexterm>Lib.Abs</title>
    <para></para> <?DocLib D4ExpressionBlock programlisting Operators?>
    <formalpara>
      <title>Declarations</title>
      <para><programlisting><?DocLib D4Field Declaration?></programlisting></para>
    </formalpara>
    <bridgehead renderas="sect3">Remarks</bridgehead>
    <para>About <ulink url="O-Lib.Ceiling.html">Lib.Ceiling</ulink>.</para>
  </section>
  <section id="O-Lib.AbsString">
    <title>Lib.Abs</title>
    <formalpara>
      <title>Declarations</title>
      <para><programlisting><?DocLib D4Field Declaration?></programlisting></para>
    </formalpara>
    <para>See <ulink url="O-Lib.Abs.html ">Lib.Abs</ulink> and <ulink url="DDGSomewhere">there</ulink>.</para>
  </section>
  <section id="O-Lib.Version">
    <title>Lib.Version</title>
    <formalpara>
      <title>Version</title>
      <para><informaltable><tgroup cols="1"><thead><row><entry>Name</entry></row></thead>
        <tbody><row><entry><?DocLib D4Field Name?></entry></row></tbody></tgroup></informaltable></para>
    </formalpara>
  </section>
</section>
"""

PAGES = [
    {"id": "O-Lib.MathOperators", "output": "ref/operators/Lib.MathOperators.md", "title": "Math Operators", "reason": ""},
    {"id": "O-Lib.Abs", "output": "ref/operators/Lib.Abs.md", "title": "Lib.Abs", "reason": ""},
    {"id": "O-Lib.AbsString", "output": "ref/operators/Lib.Abs.md", "title": "Lib.Abs (String)", "reason": "merged"},
    {"id": "O-Lib.Version", "output": "ref/operators/Lib.Version.md", "title": "Lib.Version", "reason": ""},
]

DECLARATION = 'create operator .Lib.Abs(const AValue : Lib.Decimal) : Lib.Decimal\n\tclass "Lib.AbsNode";'

# A library chapter in miniature: the five generated blocks of its Definition section in the shape
# every chapter of the 2007 reference has them, a never-written See Also, a generated Schema
# subtree, and a Settings section with two external-document splice points.
LIBRARY_SOURCE = r"""<?xml version="1.0"?>
<chapter id="DRLibLibrary">
  <title><indexterm><primary>Lib</primary></indexterm> <?DocLib D4Field LibraryName?> </title>
  <para>Reference material for the Dataphor <?DocLib D4Field LibraryName?> Library.</para>
  <section id="DRLibLibrary-Definition">
    <title> Definition </title>
    <formalpara><title>Version</title>
      <para><?DocLib D4ExpressionField Version Libraries where Name = "Lib"?></para></formalpara>
    <?DocLib D4ExpressionBlock row LibraryRequisites where Library_Name = "Lib"?>
    <formalpara><title>Requisites</title><para><informaltable><tgroup cols="2">
      <thead><row><entry>Name</entry><entry>Version</entry></row></thead>
      <tbody><row><entry><?DocLib D4Field Required_Library_Name?></entry>
        <entry><?DocLib D4Field Required_Library_Version?></entry></row></tbody>
    </tgroup></informaltable></para></formalpara>
    <?DocLib D4ExpressionBlock row LibraryFiles where Library_Name = "Lib"?>
    <formalpara><title><?DocLib D4Field LibraryName?> Files</title><para><informaltable><tgroup cols="2">
      <thead><row><entry>Name</entry><entry>Is Assembly</entry></row></thead>
      <tbody><row><entry><?DocLib D4Field FileName?></entry>
        <entry><?DocLib D4Field IsAssembly?></entry></row></tbody>
    </tgroup></informaltable></para></formalpara>
    <bridgehead renderas="sect4">See Also</bridgehead>
    <para><?xm-replace_text {para}?></para>
    <?DocLib D4ExpressionBlock member RegisteredClasses where Library_Name = 'Lib'?>
    <section id="DRLibLibrary-RegisteredClasses">
      <title>Registered Classes</title>
      <para><simplelist columns="2"><member><?DocLib D4Field SimpleName?></member></simplelist></para>
    </section>
  </section>
  <section id="DRLibLibrary-Settings">
    <title>Settings</title>
    <bridgehead>Attributes</bridgehead>
    <para>The attributes of the device.</para>
    <?DocLib ExtDoc outer droptags="abc" c:\src\Alphora\Docs\LibSettings.xml?>
    <?DocLib ExtDoc outer droptags="sectioninfo" c:\src\Alphora\Docs\LibElsewhere.xml?>
  </section>
  <section id="DRLibLibrary-Schema">
    <title>Schema</title>
    <para>The following sections list the schema elements defined in the Lib library.</para>
    <section id="DRLibLibrary-Schema-Forms">
      <title>Forms</title>
      <para>The following lists the forms defined in the Lib library.</para>
    </section>
  </section>
</chapter>
"""

# The common settings fragment: a bare <para> with no section of its own, which is why it can only
# reach a page by being spliced in at the instruction that names it.
LIBRARY_SETTINGS = ('<?xml version="1.0"?>'
                    "<para>Settings common to every device of this kind.</para>")

# An operator file whose root carries no id and whose operator sections are not pages of their own.
LIBRARY_OPERATORS = """<?xml version="1.0"?>
<section>
  <title>Operators</title>
  <para>The following sections describe the operators of the Lib library.</para>
  <section id="O-Lib.Sum">
    <title>Lib.Sum</title>
    <formalpara><title>Declarations</title>
      <para><programlisting><?DocLib D4Field Declaration?></programlisting></para></formalpara>
  </section>
</section>
"""

LIBRARY_PAGE = "reference/libraries/Lib.md"
LIBRARY_PAGES = [
    {"id": "DRLibLibrary", "output": LIBRARY_PAGE, "title": "Lib", "reason": ""},
    {"id": "DRLibLibrary-Definition", "output": "", "title": "Definition", "reason": ""},
    {"id": "DRLibLibrary-RegisteredClasses", "output": "", "title": "Registered Classes", "reason": ""},
    {"id": "DRLibLibrary-Settings", "output": "", "title": "Settings", "reason": ""},
    {"id": "DRLibLibrary-Schema", "output": split.DROP, "title": "Schema", "reason": "generated"},
    {"id": "DRLibLibrary-Schema-Forms", "output": split.DROP, "title": "Forms", "reason": "generated"},
    {"id": "LibOperators2.xml", "output": LIBRARY_PAGE, "title": "Operators", "reason": ""},
    {"id": "O-Lib.Sum", "output": "", "title": "Lib.Sum", "reason": ""},
]
LIBRARY_FACTS = [
    {"id": "DRLibLibrary", "field": "library", "value": "Lib", "detail": "", "reason": ""},
    {"id": "DRLibLibrary", "field": "version", "value": "1.2.3.4", "detail": "", "reason": ""},
    {"id": "DRLibLibrary", "field": "requisite", "value": "System", "detail": "*", "reason": ""},
    {"id": "DRLibLibrary", "field": "requisite", "value": "Other", "detail": "2.0.0.0", "reason": ""},
    {"id": "DRLibLibrary", "field": "file", "value": "Lib.dll", "detail": "True", "reason": ""},
]
SUM_DECLARATION = 'create operator .Lib.Sum(const AValue : Lib.Decimal) : Lib.Decimal;'


class Harness:
    """Sources, a page map and the tables that fill them, in a temporary folder."""

    def __init__(self, stack, pages=PAGES, declarations=(("O-Lib.Abs", DECLARATION),),
                 sources=(("LibOperators.xml", SOURCE),), facts=None, include=()):
        self.folder = stack.enter_context(tempfile.TemporaryDirectory())
        self.sources = [self.write(name, body) for name, body in sources]
        self.source = self.sources[0]
        self.include = [self.write(name, body) for name, body in include]
        self.pages = os.path.join(self.folder, "pages.tsv")
        convert.write_tsv(self.pages, split.PAGES_COLUMNS, pages)
        self.declarations = os.path.join(self.folder, "declarations.tsv")
        convert.write_tsv(self.declarations, split.DECLARATIONS_COLUMNS,
                          [{"id": i, "declaration": split.escape_declaration(d)} for i, d in declarations])
        self.facts = None
        if facts is not None:
            self.facts = os.path.join(self.folder, "facts.tsv")
            convert.write_tsv(self.facts, split.FACTS_COLUMNS, facts)
        self.out = os.path.join(self.folder, "work")

    def write(self, name, body):
        path = os.path.join(self.folder, name)
        with open(path, "wb") as stream:
            stream.write(body.encode("utf-8"))
        return path

    def run(self):
        self.report = split.Report()
        self.status = split.split(self.sources, self.pages, self.declarations, self.out,
                                  self.report, facts_path=self.facts, include=self.include)
        return self.status

    def page(self, output):
        with open(os.path.join(self.out, split.work_name(output)), encoding="utf-8") as stream:
            return stream.read()


class SplitTest(unittest.TestCase):
    def harness(self, **kwargs):
        stack = contextlib.ExitStack()
        self.addCleanup(stack.close)
        return Harness(stack, **kwargs)

    def test_several_sections_become_one_document_in_row_order(self):
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        document = ET.fromstring(harness.page("ref/operators/Lib.Abs.md"))
        self.assertEqual(document.tag, "article")
        self.assertEqual(document.get("id"), "O-Lib.Abs")
        # The first row's title is the document's; the second section keeps its own, which the
        # page map distinguishes from it.
        self.assertEqual([e.text for e in document.findall("title")], ["Lib.Abs"])
        self.assertEqual([(e.tag, e.get("id")) for e in document.findall("section")],
                         [("section", "O-Lib.AbsString")])
        self.assertEqual(split.title_text(document.find("section")), "Lib.Abs (String)")

    def test_declaration_is_substituted_and_a_missing_one_drops_its_block(self):
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        document = ET.fromstring(harness.page("ref/operators/Lib.Abs.md"))
        listings = [e.text for e in document.iter("programlisting")]
        self.assertEqual(listings, [DECLARATION])
        # O-Lib.AbsString has no row in the declaration table, so its whole formalpara goes:
        # a "Declarations" heading with nothing under it is worse than none.
        titles = [split.title_text(e) for e in document.iter("formalpara")]
        self.assertEqual(titles, ["Declarations"])
        self.assertIn("O-Lib.AbsString", harness.report.lines["sections-with-no-published-declaration"])

    def test_a_generated_table_is_removed_with_its_heading(self):
        # The only row of the Version table is a placeholder; the column headings that remain
        # are not content, so the table and the formalpara around it go too.
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        document = ET.fromstring(harness.page("ref/operators/Lib.Version.md"))
        self.assertEqual(list(document.iter("informaltable")), [])
        self.assertEqual(list(document.iter("formalpara")), [])

    def test_a_category_page_lists_the_pages_cut_out_of_it(self):
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        document = ET.fromstring(harness.page("ref/operators/Lib.MathOperators.md"))
        links = [(e.get("url"), e.text) for e in document.iter("ulink")]
        # Lib.AbsString shares Lib.Abs's page, so the list names that page once, by the help
        # page the built-in rule sends there.
        self.assertEqual(links, [("O-Lib.Abs.html", "Lib.Abs"), ("O-Lib.Version.html", "Lib.Version")])

    def test_urls_are_normalised(self):
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        document = ET.fromstring(harness.page("ref/operators/Lib.Abs.md"))
        urls = sorted(e.get("url") for e in document.iter("ulink"))
        self.assertEqual(urls, ["DDGSomewhere.html", "O-Lib.Abs.html", "O-Lib.Ceiling.html"])

    def test_a_root_section_with_no_id_is_addressed_by_its_file_name(self):
        # The 2004 OperatorDocs sources carry no id anywhere, so the file name is the only thing
        # a page map can key on. It has to be opt-in by row, because an id-less root is not
        # always a page: SystemOperators.xml's root section has none either and stays out.
        sources = (("LibOperators.xml", SOURCE),
                   ("Lib.Xor.xml",
                    '<?xml version="1.0"?>'
                    '<!DOCTYPE sect2 SYSTEM "/gone/docbookx.dtd">'
                    '<sect2><title>Xor</title>'
                    '<para>See <ulink url="O-Lib.Abs.html">Lib.Abs</ulink>.</para></sect2>'))
        row = {"id": "Lib.Xor.xml", "output": "ref/operators/Lib.Xor.md", "title": "Lib.Xor", "reason": ""}
        harness = self.harness(pages=PAGES + [row], sources=sources)
        self.assertEqual(harness.run(), 0)
        document = ET.fromstring(harness.page("ref/operators/Lib.Xor.md"))
        self.assertEqual(split.title_text(document), "Lib.Xor")
        self.assertIn("Lib.Xor.xml", harness.report.lines["root-sections-addressed-by-their-file-name"])
        # Such a page has no help page and needs none: no enclosing section can cut it out, so
        # no generated navigation list ever links it by name.
        self.assertNotIn("Lib.Xor", "".join(harness.report.lines["pages-needing-a-legacy-map-row"]))

        plain = self.harness(sources=sources)
        self.assertEqual(plain.run(), 0)
        self.assertFalse(os.path.exists(os.path.join(plain.out, split.work_name("ref/operators/Lib.Xor.md"))))
        self.assertIn("Lib.Xor.xml: Xor", plain.report.lines["sections-without-an-id"])

    def test_a_section_id_with_no_row_is_an_error(self):
        harness = self.harness(pages=[row for row in PAGES if row["id"] != "O-Lib.Version"])
        self.assertEqual(harness.run(), 1)
        self.assertTrue(any("O-Lib.Version" in line for line in harness.report.errors), harness.report.errors)
        self.assertFalse(os.path.isdir(harness.out))

    def test_two_pages_that_flatten_to_one_work_file_is_an_error(self):
        # The work file name is the output path with its separators folded to "-", so two outputs
        # can collide; the second would overwrite the first and the page would be lost without a
        # word, because convert.py keeps one output per source in pages.tsv.
        pages = [dict(row) for row in PAGES]
        pages[1]["output"] = "ref/operators/Lib.Abs.md"
        pages[2]["output"] = "ref-operators/Lib.Abs.md"
        harness = self.harness(pages=pages)
        self.assertEqual(harness.run(), 1)
        self.assertTrue(any("both flatten to" in line for line in harness.report.errors),
                        harness.report.errors)
        self.assertFalse(os.path.isdir(harness.out))

    def test_a_surviving_placeholder_is_an_error(self):
        # Nothing in the split leaves one behind today; this is the guard that says so, because
        # a placeholder rendered as literal text would read as published documentation.
        chunk = ET.fromstring('<section id="x"><title>X</title><para /></section>',
                              parser=split.xml_parser())
        chunk.find("para").append(ET.fromstring("<p><?DocLib D4Field Declaration?></p>",
                                                parser=split.xml_parser())[0])
        report = split.Report()
        split.build_page("ref/x.md", [split.Item("x", ET.Element("title"), chunk, 0)], report)
        self.assertEqual(len(report.errors), 1)
        self.assertIn("placeholder(s) survived", report.errors[0])


class LibraryChapterTest(unittest.TestCase):
    """The branches a library chapter needs that an operator page does not."""

    def harness(self, **kwargs):
        stack = contextlib.ExitStack()
        self.addCleanup(stack.close)
        kwargs.setdefault("pages", LIBRARY_PAGES)
        kwargs.setdefault("facts", LIBRARY_FACTS)
        kwargs.setdefault("declarations", (("O-Lib.Sum", SUM_DECLARATION),))
        kwargs.setdefault("sources", (("LibLibrary.xml", LIBRARY_SOURCE),
                                      ("LibOperators2.xml", LIBRARY_OPERATORS)))
        kwargs.setdefault("include", (("LibSettings.xml", LIBRARY_SETTINGS),))
        return Harness(stack, **kwargs)

    def page(self, harness):
        return ET.fromstring(harness.page(LIBRARY_PAGE))

    def test_a_dropped_row_takes_the_section_and_everything_under_it(self):
        # The generated Schema subtree is the reason the sentinel exists: its lists came from a
        # live catalog, and an empty heading reads as a documentation gap rather than as a block
        # that was never filled.
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        titles = [split.title_text(e) for e in self.page(harness).iter("section")]
        self.assertNotIn("Schema", titles)
        self.assertNotIn("Forms", titles)
        self.assertIn("LibLibrary.xml: DRLibLibrary-Schema", harness.report.lines["sections-dropped"])

        # A page inside a dropped section would be lost without a word, so the row asking for
        # that is an error rather than a surprise.
        pages = [dict(row) for row in LIBRARY_PAGES]
        forms = next(row for row in pages if row["id"] == "DRLibLibrary-Schema-Forms")
        forms["output"] = "reference/libraries/Lib.Forms.md"
        inside = self.harness(pages=pages)
        self.assertEqual(inside.run(), 1)
        self.assertTrue(any("inside the dropped section" in line for line in inside.report.errors),
                        inside.report.errors)

    def test_an_included_file_is_spliced_in_where_its_instruction_stood(self):
        # SQLDeviceSettings.xml is a bare fragment with no section of its own, so the page map
        # cannot address it and the splice point is the only way in. An instruction naming a file
        # that was not included is removed like any other placeholder, and listed.
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        settings = [e for e in self.page(harness).iter("section")
                    if split.title_text(e) == "Settings"][0]
        self.assertIn("Settings common to every device of this kind.",
                      [(e.text or "").strip() for e in settings.iter("para")])
        self.assertIn("DRLibLibrary-Settings: LibElsewhere.xml",
                      harness.report.lines["external-document-instructions-removed"])

        absent = self.harness(include=())
        self.assertEqual(absent.run(), 0)
        self.assertNotIn("Settings common to every device of this kind.",
                         absent.page(LIBRARY_PAGE))

    def test_the_facts_table_fills_the_definition_blocks_and_removes_the_ones_it_has_no_row_for(self):
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        document = self.page(harness)
        definition = [e for e in document.iter("section")
                      if split.title_text(e) == "Definition"][0]
        blocks = {split.title_text(e): e for e in definition.iter("formalpara")}
        self.assertEqual(sorted(blocks), ["Lib Files", "Requisites", "Version"])
        self.assertEqual("".join(blocks["Version"].find("para").itertext()).strip(), "1.2.3.4")
        # The one template row is repeated per fact, keeping the hand-written column headings.
        cells = [[" ".join("".join(c.itertext()).split()) for c in row]
                 for row in blocks["Requisites"].iter("row")]
        self.assertEqual(cells, [["Name", "Version"], ["System", "*"], ["Other", "2.0.0.0"]])
        self.assertEqual([[" ".join("".join(c.itertext()).split()) for c in row]
                          for row in blocks["Lib Files"].iter("row")],
                         [["Name", "Is Assembly"], ["Lib.dll", "True"]])
        # The library's name is written where the sources left a placeholder for it, in the
        # chapter's prose as well as in that Files heading.
        self.assertIn("Reference material for the Dataphor Lib Library.",
                      [" ".join("".join(e.itertext()).split()) for e in document.iter("para")])
        # No `class` row, so the Registered Classes section goes rather than publishing an empty
        # list: for most libraries that list is built by reflecting over the assembly.
        self.assertNotIn("Registered Classes",
                         [split.title_text(e) for e in document.iter("section")])
        self.assertIn("DRLibLibrary: class",
                      harness.report.lines["generated-blocks-removed-for-want-of-a-fact"])

    def test_a_declaration_is_taken_from_the_section_that_holds_it(self):
        # A library's operator sections are not pages of their own - they are nested under their
        # category on the library's page - so the signature has to be looked up by the id of the
        # section the block is in, not by the id of the page's first section.
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        listings = [e.text for e in self.page(harness).iter("programlisting")]
        self.assertEqual(listings, [SUM_DECLARATION])
        self.assertEqual(harness.report.lines["sections-with-no-published-declaration"], {})

    def test_an_informal_heading_left_with_nothing_under_it_is_dropped(self):
        # Every chapter's Definition section ends in a See Also whose list of links was never
        # written and stood as a placeholder; removing the placeholder leaves the heading behind.
        harness = self.harness()
        self.assertEqual(harness.run(), 0)
        headings = ["".join(e.itertext()).strip() for e in self.page(harness).iter("bridgehead")]
        self.assertNotIn("See Also", headings)
        self.assertIn("Attributes", headings)   # this one still has its paragraph
        self.assertIn("DRLibLibrary: See Also",
                      harness.report.lines["informal-headings-with-nothing-under-them"])


class DeclarationTableTest(unittest.TestCase):
    def test_escaping_round_trips_a_multi_line_value(self):
        # A D4 script is several tab-indented lines and the tables are tab-separated with no
        # quoting, so the escaping is the one thing that can silently corrupt a signature.
        for text in (DECLARATION,
                     "a\\b", "\\n", "tab\there", "crlf\r\nend", "trailing\\",
                     "create type .Lib.X\n{\n\trepresentation X\n\t{\n\t\tValue : Lib.X\n\t}\n};"):
            with self.subTest(text=text):
                cell = split.escape_declaration(text)
                self.assertNotIn("\t", cell)
                self.assertNotIn("\n", cell)
                self.assertEqual(split.unescape_declaration(cell), text)

    def test_the_declarations_block_of_a_help_page_is_every_listing_in_it(self):
        markup = ('<h1 class="title"><a name="O-Lib.Abs"></a>Lib.Abs</h1>'
                  '<p><b>Type Declaration.&nbsp;</b></p><pre class="programlisting">create type .Lib.X;</pre>'
                  '<p><b>Declarations.&nbsp;</b></p>'
                  '<pre class="programlisting">\ncreate operator .Lib.Abs(const A : Lib.Decimal) : Lib.Decimal\n'
                  ' class &quot;Lib.DecimalAbsNode&quot;;</pre><p></p>'
                  '<pre class="programlisting">create operator .Lib.Abs(const A : Lib.Integer) : Lib.Integer;</pre>'
                  '<h3><a name="d0e1"></a>Remarks</h3><pre class="programlisting">not a declaration</pre>')
        self.assertEqual(split.declaration_of(markup),
                         'create operator .Lib.Abs(const A : Lib.Decimal) : Lib.Decimal\n'
                         ' class "Lib.DecimalAbsNode";\n'
                         'create operator .Lib.Abs(const A : Lib.Integer) : Lib.Integer;')

    def test_a_help_page_with_no_declarations_block_has_none(self):
        self.assertIsNone(split.declaration_of(
            '<h1 class="title"><a name="O-Lib.MathOperators"></a>Math</h1>'
            '<div class="toc"><dl><dt><a href="O-Lib.Abs.html">Lib.Abs</a></dt></dl></div>'))


class SeedTest(unittest.TestCase):
    def test_a_seeded_row_keeps_the_source_spelling_of_its_page(self):
        # convert.py's kebab-case page-name rule would make System.Abs into system-abs; the
        # reference pages are named from the map, so they keep the legacy spelling and case.
        with contextlib.ExitStack() as stack:
            folder = stack.enter_context(tempfile.TemporaryDirectory())
            source = os.path.join(folder, "LibOperators.xml")
            with open(source, "wb") as stream:
                stream.write(SOURCE.encode("utf-8"))
            out = os.path.join(folder, "seed.tsv")
            self.assertEqual(split.seed_pages([source], out, split.Report()), 0)
            rows = convert.read_tsv(out, split.PAGES_COLUMNS)
        self.assertEqual([(row["id"], row["output"], row["title"], row["reason"]) for row in rows], [
            ("O-Lib.MathOperators", "reference/system-library/operators/Lib.MathOperators.md", "Math Operators", ""),
            ("O-Lib.Abs", "reference/system-library/operators/Lib.Abs.md", "Lib.Abs", ""),
            ("O-Lib.AbsString", "reference/system-library/operators/Lib.AbsString.md", "Lib.Abs", ""),
            ("O-Lib.Version", "reference/system-library/operators/Lib.Version.md", "Lib.Version", ""),
        ])


if __name__ == "__main__":
    unittest.main()
