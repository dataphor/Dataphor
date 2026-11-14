description: A small, dependency-free script that checks every link and image reference in the Markdown documentation under docs/ and reports the broken ones, so the converted documentation can be verified now and checked automatically later.
architecture: docs/modernization-plan.md#ws8-documentation
files: tools/docs-link-check/check_links.py, tools/docs-link-check/test_check_links.py, tools/docs-link-check/README.md, AGENTS.md
difficulty: easy
----
## What was built

`tools/docs-link-check/check_links.py` — Python 3 standard library only, one file. `python tools/docs-link-check/check_links.py [ROOT ...] [--pending FILE] [--list-external]`; `ROOT` defaults to `docs`. Output `path:line: error|pending|external: message`, repository-relative forward-slash paths, sorted by path and line, then a summary line. Exit 0 / 1 / 2 (argparse errors, a root that is not a folder, unreadable pending file, pending file without a `target` column).

Importable API for `docs-convert-tooling`: `slugify(text)`, `heading_slugs(markdown_text)`, plus `heading_text(raw)` (Markdown heading source → rendered text), `extract_links`, `check(roots, pending, repo_root=None)` returning a `Report` of `Finding`s, and `read_pending(path)`.

`README.md` documents the command line, the pending file format, what is and is not checked, the slug rule and the known simplifications. `AGENTS.md`'s `tools/` row now names `docs-link-check/`.

Python files use 4-space indentation and LF, matching `tools/net-spike/*.py` (not the C# tab convention).

## Design choices a reviewer should check

- **Repository boundary.** The repository root is found by walking up from the first root to a folder containing `.git`. Roots inside it use it as the boundary for `../` and leading-`/` links; a root outside any repository (a scratch conversion folder) uses itself as the boundary, so a converted page's `../src/…` link would be an error there. `docs-convert-tooling` should confirm its output has no links out of the docs tree, or say so.
- **Pending matching** compares the resolved absolute path, exact case, against `root/target` for every root. A wrong-case link to a listed pending target is reported as a wrong-case error only if a differently-cased file exists; otherwise it is `missing file`, not `pending`.
- **Non-external schemes** (anything other than http, https, mailto, ftp — e.g. `javascript:`, `file:`) are errors. The ticket did not say; strict seemed the safe direction.
- **HTML comments**: links inside them are skipped as well as headings (GitHub does not render them).
- **Indented code vs. list content**: a 4-space indented line is an indented code block only when not inside a list; list context ends at a column-0 non-list line after a blank line. Fences inside list context are recognised at any indentation. This is a heuristic, not full CommonMark; it errs toward checking list-item prose.
- **Fragments** are percent-decoded before comparison with slugs (so `#caf%C3%A9` matches heading "Café").
- `_` emphasis is stripped only as a matched pair at word boundaries, so `Event_Handlers` keeps its underscore; `*` and `~` are left for the slugger to delete.

## Tests (`python -m unittest discover -s tools/docs-link-check`, 12 tests, all pass)

- `HeadingSlugTest.test_slugs` — the ticket's nine heading → slug cases (curly apostrophe, inline code, link text, double space, underscore, parentheses).
- `HeadingSlugTest.test_duplicates` — `x`, `x-1`, `x-2` across ATX and setext headings.
- `FixtureTreeTest` — one temporary tree checked once with a `--pending` file read through `read_pending` (with an extra column): valid cross-file link with anchor; missing file; wrong-case folder (`Guide/` vs `guide/`, an error on Windows although `open()` would succeed); missing anchor; duplicate heading's `-1` anchor (valid); a heading inside a fence is not an anchor; links in an inline code span and in a fence are ignored; raw `<img src>` to a missing image is an error; a pending target is `pending`, and `main()` over a folder whose only finding is pending exits 0; an external link is counted, not an error; totals 5 errors, 2 pending, 1 external.

## Verified by inspection (scratch tree, deleted afterwards)

Four-backtick fence containing a three-backtick line; unclosed fence runs to end of file; footnote `[^1]` references and definitions not reported (a link inside a footnote's text is checked); `> [!NOTE]` alert not a link; `\<a href=…>` escaped HTML ignored; inline code span spanning two lines masked; BOM + CRLF file with correct line numbers; `../../x.md` beyond the repository → error; folder link with fragment checked against its `README.md`, and a bad fragment there is an error; `#Top` against heading "Top" → error; `<dest>` and `?query` forms; `%2F` decoding; email autolink counted as external `mailto:`; indented code block ignored; list-item continuation paragraph checked; pending file without `target` → exit 2 with a message; heading `` Has `code` & _emph_ <b>bold</b> \<x\> `` → `has-code--emph-bold-x`.

## Existing docs

`python tools/docs-link-check/check_links.py` over today's `docs/`: 3 files, 21 links, 0 errors, 0 pending, 0 external; nothing to fix. Run time about 0.25 s wall clock (Windows, `time`).

## Known gaps

- Headings inside block quotes or list items are not collected as anchors (GitHub does make anchors for them). Pandoc output rarely has them; if the conversion's link check reports a missing anchor that is such a heading, this is why.
- Link destinations must be on one line; `[text](` followed by a newline before the destination is not seen.
- A `](` not preceded by any `[` on the line would be read as a link (false positive possible, none seen).
- Bare URLs (GitHub's extended autolinks) are not counted as external links.
- The pandoc-specific cases (`\<` escapes, footnotes, raw HTML figures) were checked against hand-made samples only; the ticket asks for a look at real converted pages once `docs-convert-tooling` produces some.
- `slugify` uses Python's `unicodedata` tables, whose Unicode version may differ from `github-slugger`'s; only rarely assigned characters would slug differently.
