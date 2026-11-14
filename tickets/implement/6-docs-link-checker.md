description: Write a small, dependency-free script that checks every link and image reference in the Markdown documentation under docs/ and reports the broken ones, so the converted documentation can be verified now and checked automatically later.
architecture: docs/modernization-plan.md#ws8-documentation
files: tools/docs-link-check/check_links.py, tools/docs-link-check/test_check_links.py, tools/docs-link-check/README.md, docs/architecture.md, docs/modernization-plan.md, docs/migration-notes.md, AGENTS.md
difficulty: easy
----
## Why

`docs-convert-asciidoc` turns the old AsciiDoc books into Markdown under `docs/`; its acceptance test is "the link checker reports zero broken internal links and anchors within `docs/`". `docs-link-check-ci` (in `tickets/backlog/feat-ws8-documentation-remainder.md`) later runs the same checker in CI. So the checker is its own tool with **no dependency on the conversion toolchain** (no Node, pandoc, Asciidoctor or Pillow): Python 3 standard library only, one file, runnable on a GitHub runner or a developer machine. The converter (`docs-convert-tooling`) imports its heading-slug function, so that the converter and the checker agree on what GitHub calls an anchor.

## Interface

```
python tools/docs-link-check/check_links.py [ROOT ...] [--pending FILE] [--list-external]
```

- `ROOT` defaults to `docs`. Every `*.md` file under each root is scanned, recursively.
- Exit code 0 when there are no errors, 1 otherwise, 2 for bad arguments.
- One line per finding, `path:line: error|pending|external: message`, paths repo-relative with forward slashes. A summary line at the end: files scanned, links checked, errors, pending, external.
- `--pending FILE`: a tab-separated file with a header row and a column named `target` (other columns are ignored). Each `target` is a path relative to the root being checked (for example `reference/system-library/operators/System.iAddition.md` when the root is `docs`), so the same list works for `docs/` and for a scratch conversion folder; with several roots, a target is tried against each. A link whose file does not exist but whose resolved path, taken relative to its root, is listed there is reported as `pending`, not as an error. This is how the 206 links to not-yet-converted legacy reference pages pass the check until `docs-convert-legacy-reference` creates those pages (the list is `tools/docs-convert/legacy-links.tsv`, produced by `docs-convert-tooling`). A pending target that now exists is simply valid.
- `--list-external`: print each external link (`http:`, `https:`, `mailto:`, `ftp:`). By default they are only counted. External links are never fetched and never fail the check (no network in CI is assumed).

## What it checks

Link sources, in each Markdown file, **outside** fenced code blocks (```` ``` ```` and `~~~`, any length of three or more, closed by the same character with at least the same length), indented code blocks, and inline code spans:

- inline links and images `[text](dest "title")`, `![alt](dest)`, including `<dest with spaces>` destinations;
- reference definitions `[label]: dest`;
- autolinks `<https://…>` (external only);
- raw HTML `href="…"` and `src="…"` attributes (pandoc writes images with width and height, figures and some tables as raw HTML).

For each internal destination:

- Split off `?query` and `#fragment`; percent-decode the path (`%20` → space).
- An empty path means the current file. Otherwise resolve relative to the file's folder; a leading `/` is relative to the repository root (GitHub's rule for repository-relative links).
- **The target must exist with exactly that case.** Windows file systems ignore case and GitHub does not, so compare each path component against the real directory listing (`os.listdir`), not `os.path.exists`. A wrong-case link is an error that says which component differs.
- A path that resolves to a directory is valid (GitHub shows its `README.md`); a fragment on a directory link is checked against that directory's `README.md`.
- If there is a fragment and the target is a `.md` file, the fragment must be one of that file's anchors: the GitHub slugs of its headings, plus any raw HTML `id="…"` or `name="…"` attribute values. Fragments on non-Markdown targets are not checked.
- Anchors are compared exactly. GitHub's slugs are lower case, so `#Foo` against a heading "Foo" is an error (stricter than GitHub's browser behaviour, which is the safe direction).

## GitHub heading slugs

Headings are ATX (`#` to `######`, with optional closing `#`s) and setext (`===` / `---` underline) headings outside code blocks. Lines inside HTML comments are not headings.

Heading text is the rendered text: strip inline-code backticks (keep the code text), emphasis markers, link syntax (keep the link text), images (drop them), raw HTML tags (keep the text between them) and backslash escapes; decode HTML entities. Then apply GitHub's slugger (the `github-slugger` algorithm, which GitHub uses for `user-content-` anchors):

- lower-case the text;
- remove every character whose Unicode category is punctuation or symbol, **except** `-` and `_`; also remove control characters; keep letters, marks, numbers and spaces;
- replace each space with `-` (consecutive spaces give consecutive hyphens; leading and trailing whitespace is trimmed first);
- within one file, the first heading with a given slug keeps it; later ones get `-1`, `-2`, … (if `foo-1` is itself already taken by a real heading, keep counting until the result is unused).

Expose this as `slugify(text)` plus `heading_slugs(markdown_text) -> list[str]` (in document order, duplicates resolved), so `tools/docs-convert/` can import it by adding `tools/docs-link-check` to `sys.path`.

## Existing hand-written docs

Run the checker over today's `docs/` (`architecture.md`, `modernization-plan.md`, `migration-notes.md`, `reference/`). Fix any broken internal link whose intended target is unambiguous (fixing a broken link is not a content edit); list any you cannot settle in the handoff. Links from `docs/` out to repository files (`../src/…`) are checked like any other relative link. Do not fix links in `AGENTS.md` or the root `README.md` in this ticket (the root README's links into `Documentation/` are `docs-remove-old-sources`' job and are outside the default root anyway).

## Test

One stdlib `unittest` module, `tools/docs-link-check/test_check_links.py`, run with `python -m unittest discover -s tools/docs-link-check`. This is the gate the conversion and later CI rely on, and the slug and code-block rules have real branching, so it pins them:

- a table of heading → slug cases: `Event Handlers` → `event-handlers`; `D4 Lexer BNF` → `d4-lexer-bnf`; `User’s Guide` (curly apostrophe) → `users-guide`; `Event_Handlers` → `event_handlers`; `` `select` Statement `` → `select-statement`; `Client-side Buffering Controls` → `client-side-buffering-controls`; `Using Dataphor Controls (UI Components)` → `using-dataphor-controls-ui-components`; `[Link](x.md) text` → `link-text`; `Two  spaces` → `two--spaces`; a heading repeated three times → `x`, `x-1`, `x-2`;
- one fixture tree built in a temporary directory, checked once, with an assertion per finding: a valid cross-file link with anchor; a missing file; a wrong-case file name; a missing anchor; a link to a duplicated heading's `-1` anchor (valid); a heading inside a fenced block that must not count as an anchor; a broken link inside an inline code span and inside a fenced block (both ignored); a raw `<img src>` to a missing image (error); a missing target listed in a `--pending` file (pending, exit code 0 when it is the only finding); an external link (counted, not an error).

No other tests.

## Edge cases & interactions

- Case-only mismatch on Windows (`Users-Guide/` vs `users-guide/`): must be an error even though `open()` succeeds. Covered by the fixture.
- Code fences opened with four backticks containing a three-backtick line: the inner line does not close the fence. By inspection.
- An unclosed fence runs to end of file (CommonMark rule): everything after it is code. By inspection.
- Pandoc output escapes `<` as `\<` in prose and writes `[^1]` footnote references and `[^1]: …` definitions: footnote labels are not links and must not be reported. By inspection against a converted page once `docs-convert-tooling` produces some; note it in the README.
- GitHub alerts (`> [!NOTE]`) are not links. By inspection.
- `--pending` file with a missing `target` column: exit 2 with a message, not a crash.
- Files with CRLF or a byte-order mark: read as UTF-8 with `utf-8-sig`, normalise newlines; line numbers still correct.
- The checker must not follow links outside the repository root (`../../..` beyond the repo): report as an error.
- Performance: about 80 converted files and a few thousand links; a directory-listing cache keeps it well under a second. No measurement needed beyond noting the run time in the handoff.

## TODO

- Write `tools/docs-link-check/check_links.py` per the interface and rules above; `slugify` and `heading_slugs` importable.
- Write `tools/docs-link-check/test_check_links.py` with the slug table and the one fixture tree; run it.
- Write `tools/docs-link-check/README.md`: purpose, command line, the `--pending` file format, what is and is not checked, the slug rule, and that it has no dependencies beyond Python 3.
- Run it over today's `docs/`; fix unambiguous broken links in the hand-written docs, list the rest in the handoff.
- Update the `tools/` row of the layout table in `AGENTS.md` only if it lists the folders of `tools/` by name (it does today: add `docs-link-check/`).
