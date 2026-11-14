"""Tests for check_links: the heading-slug rule and one fixture tree.

Run with: python -m unittest discover -s tools/docs-link-check
"""

import contextlib
import io
import os
import shutil
import tempfile
import unittest

import check_links


class HeadingSlugTest(unittest.TestCase):
    CASES = [
        ("Event Handlers", "event-handlers"),
        ("D4 Lexer BNF", "d4-lexer-bnf"),
        ("User’s Guide", "users-guide"),
        ("Event_Handlers", "event_handlers"),
        ("`select` Statement", "select-statement"),
        ("Client-side Buffering Controls", "client-side-buffering-controls"),
        ("Using Dataphor Controls (UI Components)", "using-dataphor-controls-ui-components"),
        ("[Link](x.md) text", "link-text"),
        ("Two  spaces", "two--spaces"),
    ]

    def test_slugs(self):
        for heading, slug in self.CASES:
            with self.subTest(heading=heading):
                self.assertEqual(check_links.heading_slugs("# %s\n" % heading), [slug])

    def test_duplicates(self):
        self.assertEqual(check_links.heading_slugs("# x\n\n## x\n\nx\n-\n"), ["x", "x-1", "x-2"])


FIXTURE = {
    "docs/guide/README.md": "# Guide\n",
    "docs/guide/a.md": (
        "# Page A\n"
        "\n"
        "## Details\n"
        "\n"
        "## Details\n"
        "\n"
        "```\n"
        "# Fenced Heading\n"
        "[broken](in-fence.md)\n"
        "```\n"
    ),
    "docs/pending/p.md": "[later](later.md)\n",
    "pending.tsv": "page\ttarget\nindex.md\tpending/later.md\npending/p.md\tlater.md\n",
    "docs/index.md": (
        "# Index\n"
        "\n"
        "[valid](guide/a.md#details)\n"
        "[missing file](guide/nope.md)\n"
        "[wrong case](Guide/a.md)\n"
        "[missing anchor](guide/a.md#nothing)\n"
        "[duplicate](guide/a.md#details-1)\n"
        "[fenced heading](guide/a.md#fenced-heading)\n"
        "In code `[broken](in-span.md)` is ignored.\n"
        '<img src="images/missing.png" width="10">\n'
        "[later](pending/later.md)\n"
        "[external](https://example.com/)\n"
        '<a data-href="not-a-link.md">x</a>\n'
    ),
}


class FixtureTreeTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tree = tempfile.mkdtemp()
        os.mkdir(os.path.join(cls.tree, ".git"))
        for path, text in FIXTURE.items():
            full = os.path.join(cls.tree, *path.split("/"))
            os.makedirs(os.path.dirname(full), exist_ok=True)
            with open(full, "w", encoding="utf-8", newline="") as stream:
                stream.write(text)
        with open(os.path.join(cls.tree, "docs", "latin1.md"), "wb") as stream:
            stream.write(b"# Caf\xe9\n")
        pending = check_links.read_pending(os.path.join(cls.tree, "pending.tsv"))
        report = check_links.check([os.path.join(cls.tree, "docs")], pending=pending)
        cls.findings = {(f.path, f.line): f for f in report.findings}
        cls.report = report

    @classmethod
    def tearDownClass(cls):
        shutil.rmtree(cls.tree)

    def finding(self, line):
        return self.findings.get(("docs/index.md", line))

    def test_valid_link_with_anchor(self):
        self.assertIsNone(self.finding(3))

    def test_missing_file(self):
        self.assertEqual(self.finding(4).kind, "error")
        self.assertIn("missing file", self.finding(4).message)

    def test_wrong_case(self):
        self.assertEqual(self.finding(5).kind, "error")
        self.assertIn("'Guide' is 'guide'", self.finding(5).message)

    def test_missing_anchor(self):
        self.assertIn("missing anchor '#nothing'", self.finding(6).message)

    def test_duplicate_heading_anchor(self):
        self.assertIsNone(self.finding(7))

    def test_heading_in_fence_is_not_an_anchor(self):
        self.assertIn("missing anchor '#fenced-heading'", self.finding(8).message)

    def test_links_in_code_are_ignored(self):
        self.assertIsNone(self.finding(9))
        self.assertFalse([f for f in self.report.findings if f.path == "docs/guide/a.md"])

    def test_data_attribute_is_not_a_link(self):
        self.assertIsNone(self.finding(13))

    def test_undecodable_file_is_an_error_not_a_crash(self):
        self.assertIn("cannot read as UTF-8", self.findings[("docs/latin1.md", 1)].message)

    def test_raw_html_image(self):
        self.assertEqual(self.finding(10).kind, "error")
        self.assertIn("images/missing.png", self.finding(10).message)

    def test_pending(self):
        self.assertEqual(self.finding(11).kind, "pending")
        # The pending target is relative to whichever root is checked, so the same file serves both runs.
        with contextlib.redirect_stdout(io.StringIO()) as output:
            code = check_links.main([os.path.join(self.tree, "docs", "pending"), "--pending", os.path.join(self.tree, "pending.tsv")])
        self.assertEqual(code, 0, output.getvalue())
        self.assertIn("pending: later.md", output.getvalue())

    def test_external(self):
        self.assertEqual(self.finding(12).kind, "external")
        self.assertEqual(self.report.count("error"), 6)
        self.assertEqual(self.report.count("pending"), 2)
        self.assertEqual(self.report.count("external"), 1)


if __name__ == "__main__":
    unittest.main()
