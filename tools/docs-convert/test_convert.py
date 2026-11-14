"""Tests for convert.py's link resolution, the parts that need no pandoc or Asciidoctor.

Run with: python -m unittest discover -s tools/docs-convert
"""

import unittest

import convert


def make_page(source, output, headings):
    page = convert.Page(source, output)
    for index, (identifier, text, slug) in enumerate(headings):
        heading = convert.Heading(identifier, text, index == 0)
        heading.slug = slug
        page.headings.append(heading)
    return page


def make_resolver(pages):
    settings = convert.Settings()
    settings.link_suffixes = (".xml", ".adoc")
    settings.source_suffix = ".adoc"
    settings.match_heading_text = True
    settings.legacy_links = True
    return convert.Resolver(pages, settings, convert.Report())


def link(url, text="label"):
    return convert.node("Link", [convert.empty_attr(), convert.text_inlines(text), [url, ""]])


class FragmentTest(unittest.TestCase):
    """Fragment rules, first match wins: (a) heading id, (b) heading text with '_' as space,
    (c) the same ignoring case; a fragment naming the title links to the page."""

    def setUp(self):
        self.page = make_page("A.adoc", "a.md", [
            ("", "Security", "security"),
            ("Events-Summary", "Summary", "summary"),
            ("", "Event Handlers", "event-handlers"),
            ("", "Security", "security-1"),
            ("", "Session-specific Objects", "session-specific-objects"),
        ])
        self.other = make_page("B.adoc", "b.md", [("", "B", "b")])
        self.resolver = make_resolver([self.page, self.other])

    def href(self, ref, current=None):
        current = current or self.other
        item = link(ref)
        self.resolver.link(current, current.source, item)
        return item["c"][2][0]

    def test_rules(self):
        cases = [
            ("A.adoc#Events-Summary", "a.md#summary"),             # (a) id
            ("A.adoc#Event_Handlers", "a.md#event-handlers"),      # (b) text
            ("A.adoc#Session-Specific_Objects", "a.md#session-specific-objects"),  # (c) case
            ("A.adoc#Security", "a.md"),                           # the title: link to the page
            ("A.adoc", "a.md"),
        ]
        for ref, expected in cases:
            with self.subTest(ref=ref):
                self.assertEqual(self.href(ref), expected)
        self.assertEqual(self.resolver.unresolved, [])

    def test_same_page_fragment(self):
        self.assertEqual(self.href("#Event_Handlers", current=self.page), "#event-handlers")

    def test_file_name_case_is_exact(self):
        self.assertEqual(self.href("a.adoc#Summary"), "a.adoc#Summary")
        self.assertEqual([problem for *_, problem in self.resolver.unresolved],
                         ["no such page; candidates: A.adoc#Summary"])


class LegacyLinkTest(unittest.TestCase):
    def test_no_rule_link_with_folder_is_listed(self):
        # Regression: legacy_rows looked for the bare file name, not the link as written, and
        # stopped the run when a legacy link with no rule carried a folder.
        page = make_page("A.adoc", "a.md", [("", "A", "a")])
        resolver = make_resolver([page])
        resolver.link(page, page.source, link("../Old/Unknown.html", "old"))
        resolver.link(page, page.source, link("O-System.iAddition.html", "add"))
        page.markdown = "# A\n\n[old](../Old/Unknown.html)\n\n[add](reference/system-library/operators/System.iAddition.md)\n"
        rows = convert.legacy_rows([page])
        self.assertEqual([(row["line"], row["legacy_page"], row["target"]) for row in rows], [
            (3, "Unknown.html", ""),
            (5, "O-System.iAddition.html", "reference/system-library/operators/System.iAddition.md"),
        ])


if __name__ == "__main__":
    unittest.main()
