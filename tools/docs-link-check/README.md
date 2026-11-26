# docs-link-check — broken-link checker for the Markdown docs

Checks every link and image reference in the Markdown files under `docs/` (or any folder you name) and reports the ones that point at nothing: a missing file, a file name with the wrong letter case, a missing heading anchor, a path that leaves the repository. It was the acceptance check for the documentation conversion (`docs-convert-asciidoc`), and CI runs it: the *Documentation links* step of `.github/workflows/docs.yml`, the docs workflow that also gates the architecture page size, because `build.yml` ignores `docs/**` on purpose. That step passes `--pending tools/docs-convert/legacy-links.tsv` so that the not-yet-converted reference links do not fail the build; `docs-remove-old-sources` deletes that list and drops the argument in the same commit. It needs nothing beyond Python 3: one file, standard library only, no network.

## Run it

```
python tools/docs-link-check/check_links.py                       # checks docs/
python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv
python tools/docs-link-check/check_links.py C:\temp\docs-out --list-external
python -m unittest discover -s tools/docs-link-check               # its tests
```

`ROOT` arguments default to `docs`; every `*.md` file under each root is scanned. Output is one line per finding, `path:line: error|pending|external: message`, with paths relative to the repository root and forward slashes, then a summary line (files scanned, links checked, errors, pending, external). Exit code 0 when there are no errors, 1 when there are, 2 for bad arguments (a root that is not a folder, an unreadable pending file, a pending file without a `target` column).

- `--pending FILE` — a tab-separated file with a header row and a column named `target` (other columns are ignored). Each target is a path relative to the root being checked, so one list serves both `docs/` and a scratch conversion folder; with several roots, each target is tried against each root. A link to a file that does not exist, whose resolved path is listed, is reported as `pending` instead of `error`. This is how links to reference pages that have not been converted yet pass until those pages exist; `tools/docs-convert/legacy-links.tsv` is such a list. A listed target that exists is simply a valid link.
- `--list-external` — print each external link (`http:`, `https:`, `mailto:`, `ftp:`). By default they are only counted. External links are never fetched and never fail the check. A link with any other scheme is an error.

## What is checked

Link sources, outside fenced code blocks (three or more backticks or tildes, closed by the same character at least as many times; an unclosed fence runs to the end of the file), indented code blocks, inline code spans and HTML comments:

- inline links and images, `[text](dest "title")`, `![alt](dest)`, including `<dest with spaces>`;
- reference definitions, `[label]: dest`;
- autolinks, `<https://…>` and `<name@host>` (always external);
- `href="…"` and `src="…"` attributes of raw HTML tags (pandoc writes sized images, figures and some tables as HTML).

Not links, and not reported: footnote labels (`[^1]` and the `[^1]:` that starts a footnote; a link inside a footnote's text is checked), GitHub alerts (`> [!NOTE]`), reference-style uses (`[text][label]`; the definition is what is checked), text escaped as `\<a href=…>`, and bare URLs that GitHub turns into links (not counted).

For each internal link:

- The `?query` and `#fragment` are split off and the path is percent-decoded (`%20` is a space). An empty path is the current file. A relative path resolves from the file's folder; a leading `/` resolves from the repository root (the folder holding `.git`), as GitHub does. When a root is not inside a repository, the root itself is the boundary.
- The target must exist **with exactly that case**: each path component is compared with the real directory listing, because Windows ignores case and GitHub does not. A wrong-case link is an error naming the component.
- A path that leaves the repository (`../` past its root) is an error.
- A folder is a valid target (GitHub shows its `README.md`); a fragment on a folder link is checked against that `README.md`.
- A fragment on a `.md` target must be one of that file's anchors: the GitHub slugs of its headings, plus the values of any raw HTML `id="…"` or `name="…"` attribute. Fragments are compared exactly, so `#Foo` against the heading "Foo" (slug `foo`) is an error, which is stricter than a browser. Fragments on other targets are not checked.

Files are read as UTF-8 with or without a byte-order mark, and CRLF line endings are handled; line numbers are those of the file. A file that is not valid UTF-8 is an error (`cannot read as UTF-8`), as is a fragment link into one.

## Heading anchors

Headings are ATX (`#` to `######`, optional closing `#`s) and setext (`===` or `---` under a paragraph) headings outside code blocks and HTML comments. The heading's rendered text is taken (inline code keeps its text without backticks; emphasis markers, link destinations, images, HTML tags and backslash escapes go; HTML entities are decoded) and turned into a slug with the `github-slugger` rule that GitHub uses:

- lower-case the trimmed text;
- remove every character that is not a letter, a mark, a number, a space, `-` or `_` (so all other punctuation, symbols and control characters go: `User’s Guide` → `users-guide`);
- replace each space with `-` (`Two  spaces` → `two--spaces`);
- within one file the first heading with a slug keeps it and later ones get `-1`, `-2`, … (skipping any value a real heading already took).

`slugify(text)` and `heading_slugs(markdown_text)` (the slugs of a whole document, in order, duplicates resolved) are importable, so the conversion tooling computes the same anchors: add `tools/docs-link-check` to `sys.path` and `import check_links`.

Known simplifications: headings inside block quotes and list items are not anchors; a list item's indented continuation is treated as prose rather than as an indented code block; link destinations must be on one line; HTML attribute values must be quoted (`href=x.md` without quotes is not seen).
