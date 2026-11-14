# docs-convert — AsciiDoc documentation to GitHub Markdown

Converts the old AsciiDoc documentation under `Documentation/` (the GitBook books: User's Guide, Developer's Guide, Dataphor Reference) into Markdown pages that GitHub renders, laid out as `docs/`, with every cross-reference turned into a working link and every image converted to PNG or kept as SVG. It is the tooling behind the conversion ticket `docs-convert-asciidoc` ([plan WS8](../../docs/modernization-plan.md#ws8-documentation)), and its DocBook stage also serves `docs-convert-legacy-reference`.

It runs **by hand**, not in CI. Once `docs-convert-asciidoc` has landed the output under `docs/`, that Markdown is the source of truth and is edited by hand like any other document; a rerun is only for reproducing the landing commit, and only until `docs-remove-old-sources` deletes `Documentation/`.

## Prerequisites

Only this tool needs them; nothing else in the repository uses Node or pandoc.

| Tool | Version | Install |
|---|---|---|
| Node.js | 20 or later (24 tested) | from nodejs.org, or `winget install OpenJS.NodeJS.LTS` |
| `@asciidoctor/cli` 4.0.0, `@asciidoctor/core` 3.0.4 (Asciidoctor 2.0.20), `@asciidoctor/docbook-converter` 3.0.0 | exact, from `package-lock.json` | `npm ci --prefix tools/docs-convert` (installs into `tools/docs-convert/node_modules/`, which git ignores) |
| pandoc | 3.14 | `winget install JohnMacFarlane.Pandoc --version 3.14`, or unzip `pandoc-3.14-windows-x86_64.zip` from the pandoc GitHub releases and pass `--pandoc PATH\pandoc.exe` |
| Python | 3.12 or later | |
| Pillow | 13.0.0 | `python -m pip install -r tools/docs-convert/requirements.txt` (used only to turn BMP, GIF and JPG into PNG) |

The DocBook converter package is required: Asciidoctor.js 3 has no built-in DocBook backend. Ruby Asciidoctor 2.0.20 would do the same job if Node is unavailable, but the pins above are the tested route and `convert.py` only knows how to run the Node one.

The converter prints each tool's version and **stops if pandoc's or a pinned npm package's major.minor version differs from the pin**, because another version writes different Markdown and a rerun would no longer reproduce the landed pages. `--allow-tool-versions` runs anyway (for experiments; the override is listed in the report). A missing tool stops the run with a one-line message pointing here.

## Run it

```
python tools/docs-convert/convert.py --out C:\temp\docs-out --allow-unresolved      # scratch run
python tools/docs-convert/convert.py --out docs                                     # the real run (docs-convert-asciidoc)
python tools/docs-convert/convert.py --out docs --require-reviewed                  # final run: every code block reviewed
python tools/docs-convert/convert.py --out C:\temp\docs-out --seed-code-table --allow-unresolved
python tools/docs-link-check/check_links.py C:\temp\docs-out --pending tools/docs-convert/legacy-links.tsv
```

| Option | Effect |
|---|---|
| `--out DIR` | where the pages and images go, laid out as `docs/` (required) |
| `--pandoc PATH` | the pandoc executable; default `pandoc` on `PATH` |
| `--allow-unresolved` | write the output even when references stay unresolved (they are still listed) |
| `--seed-code-table` | add a row to `code-blocks.tsv` for each code block it lacks and re-seed rows whose block text changed (clearing `reviewed`); without it, a missing or changed block is an error |
| `--require-reviewed` | fail if any `code-blocks.tsv` row is not marked reviewed |
| `--work DIR` | keep the intermediate DocBook in `DIR/docbook/` (default: a temporary folder, deleted afterwards) |
| `--keep-work` | keep the temporary folder and print its path |
| `--allow-tool-versions` | see Prerequisites |

The whole run takes about 7 seconds. Running it twice over the same inputs gives byte-identical pages, images and tables. To check that a rerun reproduces the committed pages, run it with `--out docs --require-reviewed` and look at `git status docs`: nothing changed. Compare through git rather than byte for byte, because the converter always writes LF and a checkout with `core.autocrlf=true` gives the working copies CRLF.

`python -m unittest discover -s tools/docs-convert` runs the converter's few tests (link resolution only; they need neither Node nor pandoc). The rest is checked by the link checker over the output and by the reproduction run.

**Nothing is written unless the run succeeds.** Errors (an unresolved reference without `--allow-unresolved`, a code-block table mismatch, an unreviewed block with `--require-reviewed`) are printed at the end of the report and the run exits 1 having written nothing; a setup problem (a missing tool, a bad table) exits 2. Exit 0 means the pages, images and tables were written.

**What it writes.** Pages at the paths `pages.tsv` names, images at `images/<name>` (flat), both under `--out`; `legacy-links.tsv` (every run) and `code-blocks.tsv` (with `--seed-code-table`) in this folder. It never deletes anything and never writes any other path, so `--out docs` leaves `architecture.md`, `modernization-plan.md`, `migration-notes.md` and `docs/reference/*.txt` alone. The report goes to standard output, not into `--out`.

## How it converts

Asciidoctor (`-b docbook5 -a data-uri! -a reproducible`, one run per source folder) writes DocBook 5; `data-uri!` stops the sources' `:data-uri:` from inlining every image as base64, `reproducible` leaves out the conversion date. pandoc's DocBook reader turns each file into its JSON syntax tree; `convert.py` rewrites the tree; pandoc's `gfm` writer (`--wrap=none --eol=lf`) writes Markdown: UTF-8 without a byte-order mark, LF line endings, paragraphs not hard-wrapped. The sources declare `:encoding: iso-8859-1` but are UTF-8, and are read as UTF-8.

The tree pass, page by page:

- **Headings.** One `#` heading per page, the document title; body headings move down one level, so `==` becomes `##` (a stray level-0 section is shown as `##` and listed). The empty heading pandoc makes of a book's untitled preface is dropped, its content kept.
- **Characters.** U+00C2 before a no-break space (mojibake) and every no-break space become an ordinary space, in prose and code; the zero-width space Asciidoctor puts after each `…` is removed. The other typographic characters Asciidoctor produces (’ — …) are kept, because the published books showed them too.
- **Blocks.** A block title (`.Sections` above a list, `.Title` above a table) becomes a bold paragraph before the block; a titled image becomes the image followed by its caption in italics; a sidebar becomes a block quote; admonitions become GitHub alerts (`> [!NOTE]`); footnotes become `[^n]`; index terms are dropped. A `[horizontal]` labelled list, which the DocBook backend writes as a two-column table, becomes a definition list again. Definition-list terms are made bold, because GitHub has no definition lists and pandoc writes each term as a line of its own. Lists whose items are single paragraphs are written tight. Tables are otherwise left as pandoc writes them: pipe tables, or HTML tables when a cell holds several paragraphs or blocks.
- **Code blocks.** Every literal or listing block becomes a fenced block with the language `code-blocks.tsv` gives it.
- **Links**, in two passes. Pass 1 records each page's headings in order, with the DocBook id of each and the anchor GitHub will give it (computed by `heading_slugs` from `tools/docs-link-check/check_links.py`, over the page's headings rendered by pandoc, so duplicate headings get `-1`, `-2` as on GitHub). Pass 2 rewrites every link:
  - `http:`, `https:`, `mailto:`, `ftp:` links are kept and listed;
  - a link to a legacy `*.html` reference page is rewritten by the rule in the table below and listed in `legacy-links.tsv`;
  - anything else is `path#fragment`. The path is resolved from the source file's folder (an empty path is the file itself; `./` is dropped; `.xml`, which Asciidoctor gives cross-references, is read as `.adoc`) and looked up in `pages.tsv` with exact letter case, never on the file system. The fragment is matched in that page only, first rule that matches: (a) a heading's DocBook id (an explicit `[[anchor]]`, or Asciidoctor's generated `_id`); (b) a heading's text, with `_` read as a space and ’ as `'` (the GitBook `<<File.adoc#Heading_Text>>` convention); (c) the same, ignoring case; (d) a row of `xref-fixes.tsv`. When two headings of a page share the text, (b) and (c) take the first, as GitBook's id did, and the report lists it as ambiguous, unless an `xref-fixes.tsv` row for that reference redirects it (usually to the other heading's explicit id). A fragment that names the page's own title is dropped, so the link goes to the page.
  - What still does not resolve is listed as unresolved, with candidates when another page has the heading, and written in the form the report prints (`File.adoc#Fragment`) so the link checker reports it too. No `<a id>` anchors are written; explicit anchors that nothing links to simply disappear (they only ever named headings, which GitHub anchors anyway).
- **Images.** Each referenced image (path percent-decoded, matched with exact case) is written to `images/` under the page-name rule applied to its file name: BMP, GIF and JPG become PNG (Pillow, `optimize=True`, palette and transparency kept), PNG and SVG are copied byte for byte. Image links are rewritten relative to the page; `width`/`height` on inline images are kept. Images no converted page uses are listed, not converted.
- **`docs/README.md`** is `README.adoc`, then a `## Contents` heading with `SUMMARY.adoc`'s nested list of links (its headings as `###`), then `index-extra.md`.

### Legacy reference links

Links to the reference pages of the old DocBook build go to the page `docs-convert-legacy-reference` will create, by rule, relative to the docs root:

| Legacy page | Target |
|---|---|
| `O-<Name>.html` | `reference/system-library/operators/<Name>.md` |
| `T-<Name>.html` | `reference/system-library/types/<Name>.md` |
| `DR<Name>Library.html` | `reference/libraries/<Name>.md` |
| `dilrefAlphora.Dataphor.Frontend.Client.I<Name>.html` | `reference/frontend-nodes/<Name>.md` |
| `Alphora.Dataphor.DAE.Client.<Name>.html` | `reference/data-access-components.md` (this conversion's placeholder page) |
| anything else | no target; listed and reported as unresolved |

Names keep the legacy spelling and case. To retarget, edit `LEGACY_RULES` in `convert.py` and rerun. Until those pages exist, the link checker's `--pending legacy-links.tsv` reports these links as pending instead of broken.

## The report

Printed to standard output; it is the review list for `docs-convert-asciidoc`. Sections appear only when they have entries: summary counts (pages, links, images and their size, admonitions, footnotes, characters replaced, blocks rewritten), Asciidoctor and pandoc warnings, unresolved references with candidates, ambiguous and case-insensitive heading matches, references settled by `xref-fixes.tsv` and rows that matched nothing, legacy links with no rule, external links, missing and unreferenced images, heading adjustments, HTML tables per page, other raw HTML per page, admonitions nested in lists (GitHub shows those as plain quotes), AsciiDoc markup that Asciidoctor left as text (a source defect), arrows Asciidoctor made of `<=`, `=>`, `<-`, `->` in prose (wrong where the text means an operator), and the code-block table's state. A line repeated for several links is printed once with `(xN)`.

## Inputs and tables

All files are tab-separated text with a header row, UTF-8, LF line endings, no quoting (a cell never contains a tab).

| File | Columns | Who edits it |
|---|---|---|
| `pages.tsv` | `source` (relative to `Documentation/`), `output` (relative to `docs/`; empty: not converted) | by hand. Seeded from the page-name rule (`page_name` in `convert.py`: split camel case and non-alphanumerics into lower-case words joined by `-`, `SQLDevice` → `sql-device`), with hand-set rows where the rule is wrong: each book's index is its folder's `README.md`, the reference stubs, `AuthoringDocumentation.adoc` not converted. Every `.adoc` under `Documentation/` must be listed exactly once; two sources may share an output only for `README.adoc` + `SUMMARY.adoc` → `README.md`. |
| `xref-fixes.tsv` | `source` (the `.adoc` the reference is in), `ref` (the reference as the report prints it, `path#fragment`), `replacement` (a corrected reference in the same form, resolved normally; empty: unlink, keeping the text), `reason` | by hand, from the report's unresolved and ambiguous lists. Consulted only for references that do not resolve otherwise or that match several same-named headings; a row for a reference that resolves cleanly is ignored (and listed as matching nothing). |
| `code-blocks.tsv` | `page` (output path), `n` (1-based position of the block in the page), `sha` (first 12 hex digits of the SHA-1 of the block text after character clean-up), `lang` (`d4`, `sql`, `xml`, `bnf`, `csharp` or `text`), `reviewed` (`yes` or empty), `first_line` (first non-blank line, 60 characters, for the reviewer) | seeded by `--seed-code-table` from a heuristic (`::=` → `bnf`; an XML declaration or a tag with a closing tag → `xml`; a line starting with an upper-case SQL statement → `sql`; `;`, `:=`, `{` or a D4 keyword → `d4`; else `text`), then reviewed by hand (`docs-convert-asciidoc`), setting `lang` and `reviewed`. A row is matched by page and position and trusted only while its `sha` matches. |
| `legacy-links.tsv` | `page`, `line` (1-based, in the output page), `text`, `legacy_page`, `target` (relative to the docs root), `source` | generated on every run; do not edit. Sorted by page, then line. Its `target` column is what the link checker's `--pending` reads, relative to whichever root it checks, so it serves `docs/` and a scratch folder alike. |
| `index-extra.md` | Markdown | by hand: the `## Project documents` section appended to `docs/README.md` (its links are relative to `docs/`, so in a scratch folder the link checker reports them missing). |

## The DocBook stage on its own

```
python tools/docs-convert/convert.py docbook FILE.xml [FILE.xml ...] --out DIR [options]
```

Runs the same pipeline from pandoc's DocBook reader onwards (characters, headings, block titles, figures, code tags, links, images, rendering, report) on DocBook that did not come from AsciiDoc, such as the legacy DocBook 4.3 reference. The resolution rules are options rather than the AsciiDoc conventions:

| Option | Effect |
|---|---|
| `--base DIR` | the folder page sources, links and image paths are relative to (default: the files' common folder) |
| `--pages TSV` | page map with columns `source` (relative to `--base`, with its `.xml` suffix) and `output`; default: the page-name rule applied to each file name, in the same relative folder |
| `--code-table TSV` | a code-block table as above (with `--seed-code-table`, `--require-reviewed`); default: heuristic tags, no table |
| `--xref-fixes TSV` | as above |
| `--match-heading-text` | enable fragment rules (b) and (c); without it a fragment must be a heading's id |
| `--global-ids` | a fragment-only reference (`#id`, what `<xref linkend>` and `<link linkend>` become) whose id is not in the current page is looked up in every page; DocBook ids span the files of a set |
| `--legacy-html`, `--legacy-table TSV` | apply the legacy `*.html` rule, and write the list |

Only heading ids are anchors: an id on a table, figure or paragraph is not one GitHub keeps, so a reference to it stays unresolved unless `xref-fixes.tsv` redirects it. From Python, `import convert` and drive `Settings`, `Page`, `docbook_to_trees`, `load_page` and `finish` directly to supply a different resolver.
