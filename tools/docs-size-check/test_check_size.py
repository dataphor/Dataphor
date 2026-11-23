"""Tests for check_size: one case per behaviour, each against a temporary tree.

Every case points the tool at a temporary folder with --root, so the real
configuration (CHECKED, and the limit) is what gets exercised and nothing is
written into docs/.

Run with: python -m unittest discover -s tools/docs-size-check
"""

import contextlib
import io
import os
import shutil
import tempfile
import unittest
from unittest import mock

import check_size


def words(count):
    """A line of `count` whitespace-separated words."""
    return " ".join(["word"] * count)


class SizeCheckTest(unittest.TestCase):
    def tree(self, pages):
        """A temporary tree holding `pages` (repository-relative path -> text)."""
        root = tempfile.mkdtemp()
        self.addCleanup(shutil.rmtree, root, True)
        for relative, text in pages.items():
            path = os.path.join(root, *relative.split("/"))
            os.makedirs(os.path.dirname(path), exist_ok=True)
            with open(path, "w", encoding="utf-8", newline="") as stream:
                stream.write(text)
        return root

    def run_main(self, root, *extra):
        """main() against `root`; returns (exit code, printed output)."""
        with contextlib.redirect_stdout(io.StringIO()) as output:
            code = check_size.main(["--root", root] + list(extra))
        return code, output.getvalue()

    def test_page_under_the_limit_passes(self):
        root = self.tree({"docs/architecture.md": "# Root\n", "docs/architecture/a.md": words(1499) + "\n"})
        code, output = self.run_main(root)
        self.assertEqual(code, 0, output)
        self.assertEqual(output.splitlines(), ["2 files checked, 0 over the limit, 0 excepted"])

    def test_page_over_the_limit_fails(self):
        root = self.tree({"docs/architecture.md": "# Root\n", "docs/architecture/a.md": words(1501) + "\n"})
        code, output = self.run_main(root)
        self.assertEqual(code, 1, output)
        self.assertIn("docs/architecture/a.md: 1501 words (limit 1500)", output)
        self.assertIn("1 over the limit", output)

    def test_fenced_code_is_not_counted(self):
        payload = words(40)
        fenced = (
            "# Title\n"
            "\n"
            "```text\n" + payload + "\n```\n"
            "\n"
            "~~~\n" + payload + "\n"
            "```\n"  # backticks do not close a tilde fence
            + payload + "\n"
            "~~~\n"
            "\n"
            "prose one two\n"
            "\n"
            "```\n"  # unclosed: the rest of the file is code
            + payload + "\n"
        )
        root = self.tree({"docs/architecture.md": "# Root\n", "docs/architecture/a.md": fenced})
        code, output = self.run_main(root, "--limit", "10")
        self.assertEqual(code, 0, output)

        # The same words outside a fence are prose and break the same limit.
        plain = fenced.replace("```text\n", "").replace("```\n", "").replace("~~~\n", "")
        root = self.tree({"docs/architecture.md": "# Root\n", "docs/architecture/a.md": plain})
        code, output = self.run_main(root, "--limit", "10")
        self.assertEqual(code, 1, output)
        self.assertIn("docs/architecture/a.md: 165 words (limit 10)", output)

        # A line of backticks whose info string holds a backtick is inline code,
        # not a fence; were it taken for one, the rest of the page would vanish
        # from the count and an oversized page would pass.
        root = self.tree({"docs/architecture.md": "# Root\n", "docs/architecture/a.md": "``` `x` ```\n" + payload + "\n"})
        code, output = self.run_main(root, "--limit", "10")
        self.assertEqual(code, 1, output)

    def test_excepted_page_is_reported_but_not_an_error(self):
        root = self.tree({"docs/architecture.md": "# Root\n", "docs/architecture/a.md": words(1501) + "\n"})
        with mock.patch.dict(check_size.EXCEPTIONS, {"docs/architecture/a.md": "a generated table"}):
            code, output = self.run_main(root)
        self.assertEqual(code, 0, output)
        self.assertIn("docs/architecture/a.md: 1501 words (exception: a generated table)", output)
        self.assertIn("0 over the limit, 1 excepted", output)

    def test_missing_literal_path_is_an_error(self):
        root = self.tree({"docs/architecture/a.md": "# A\n"})
        code, output = self.run_main(root)
        self.assertEqual(code, 1, output)
        self.assertIn("docs/architecture.md: missing", output)

    def test_set_matching_no_files_is_an_error(self):
        root = self.tree({"docs/README.md": "# Index\n"})
        code, output = self.run_main(root)
        self.assertEqual(code, 1, output)
        self.assertIn("checked set matched no files", output)


if __name__ == "__main__":
    unittest.main()
