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


class Harness:
    """A source, a page map and a declaration table in a temporary folder."""

    def __init__(self, stack, pages=PAGES, declarations=(("O-Lib.Abs", DECLARATION),), extra=None):
        self.folder = stack.enter_context(tempfile.TemporaryDirectory())
        self.source = os.path.join(self.folder, "LibOperators.xml")
        with open(self.source, "wb") as stream:
            stream.write(SOURCE.encode("utf-8"))
        self.sources = [self.source]
        if extra is not None:
            name, body = extra
            path = os.path.join(self.folder, name)
            with open(path, "wb") as stream:
                stream.write(body.encode("utf-8"))
            self.sources.append(path)
        self.pages = os.path.join(self.folder, "pages.tsv")
        convert.write_tsv(self.pages, split.PAGES_COLUMNS, pages)
        self.declarations = os.path.join(self.folder, "declarations.tsv")
        convert.write_tsv(self.declarations, split.DECLARATIONS_COLUMNS,
                          [{"id": i, "declaration": split.escape_declaration(d)} for i, d in declarations])
        self.out = os.path.join(self.folder, "work")

    def run(self):
        self.report = split.Report()
        self.status = split.split(self.sources, self.pages, self.declarations, self.out, self.report)
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
        extra = ("Lib.Xor.xml",
                 '<?xml version="1.0"?>'
                 '<!DOCTYPE sect2 SYSTEM "/gone/docbookx.dtd">'
                 '<sect2><title>Xor</title>'
                 '<para>See <ulink url="O-Lib.Abs.html">Lib.Abs</ulink>.</para></sect2>')
        row = {"id": "Lib.Xor.xml", "output": "ref/operators/Lib.Xor.md", "title": "Lib.Xor", "reason": ""}
        harness = self.harness(pages=PAGES + [row], extra=extra)
        self.assertEqual(harness.run(), 0)
        document = ET.fromstring(harness.page("ref/operators/Lib.Xor.md"))
        self.assertEqual(split.title_text(document), "Lib.Xor")
        self.assertIn("Lib.Xor.xml", harness.report.lines["root-sections-addressed-by-their-file-name"])
        # Such a page has no help page and needs none: no enclosing section can cut it out, so
        # no generated navigation list ever links it by name.
        self.assertNotIn("Lib.Xor", "".join(harness.report.lines["pages-needing-a-legacy-map-row"]))

        plain = self.harness(extra=extra)
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
