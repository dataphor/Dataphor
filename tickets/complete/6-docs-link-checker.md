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

## Tests (`python -m unittest discover -s tools/docs-link-check`, 14 tests after review, all pass)

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

## Review findings

Reviewed the diff of `ticket(implement): docs-link-checker` against the implement ticket's specification, ran the tests (14 pass) and the checker over `docs/` (0 errors), and probed edge cases with a scratch tree (deleted afterwards).

**Fixed in this pass (minor, each with a fixture case in `test_check_links.py` except the Windows-only one):**

- A `.md` file that is not valid UTF-8 crashed the run with a `UnicodeDecodeError` traceback. It is now an `error: cannot read as UTF-8` finding at line 1; a fragment link into such a file is an error saying so, rather than a crash or a misleading "missing anchor". Test: `test_undecodable_file_is_an_error_not_a_crash`.
- On Windows, a root typed in the wrong case (`check_links.py Docs`) made every relative link below it a wrong-case error (8 false errors over today's `docs/`). Roots and an explicit `repo_root` now go through `os.path.realpath`, which returns the on-disk case; verified by running `Docs` (0 errors). Not tested: only reproducible on a case-insensitive file system.
- The HTML attribute pattern matched `href` inside `data-href` (and `src` in `data-src`), so such attributes were checked as links. The pattern now requires no word character or hyphen before the name. Test: `test_data_attribute_is_not_a_link`.
- README: noted that non-UTF-8 files are errors and that unquoted HTML attribute values are not seen.

**Checked, no change:**

- Slug algorithm and de-duplication loop match `github-slugger` (same counter-on-original behaviour). The nine slug cases and the duplicate case in the tests pin it.
- Pending matching, repository boundary, `/`-rooted links, `..` past the boundary, directory links with fragments: read and agree with the specification.
- Links inside a block-quoted fence (`> ```…`) are not reported: the code-span masking treats the quoted fence as a multi-line code span. Correct result by a different route; not relied on by a test.
- Tests: the implementer's 12 all exercise specified branching (slug table, the fixture tree's one assertion per specified case); none restate implementation or mock anything, so none cut.
- Style: 4-space indentation and LF, matching `tools/net-spike/*.py`; `__pycache__/` is ignored by the root `.gitignore`. File size 544 lines (`wc -l`) for one self-contained tool, acceptable.
- Docs: `AGENTS.md`'s `tools/` row names the folder; the README covers the command line, pending format, rules and gaps. `docs/architecture.md`, `docs/modernization-plan.md` and `docs/migration-notes.md` need no change (a developer tool, nothing an application notices).

**Tickets filed:** none. **Tripwires:** none new; the known gaps listed above (headings inside block quotes or lists, multi-line destinations, bare URLs, a stray `](` false positive, unquoted attributes) are documented in the README and only matter if the converted pages turn out to contain them, which `docs-convert-tooling`'s first real output will show.
