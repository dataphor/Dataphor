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
- **Blocks.** A block title (`.Sections` above a list, `.Title` above a table) becomes a bold paragraph before the block; a titled image becomes the image followed by its caption in italics; a sidebar becomes a block quote; admonitions become GitHub alerts (`> [!NOTE]`); footnotes become `[^n]`; index terms are dropped. DocBook's `<phrase role="code">`, which pandoc keeps as a `code` class and its gfm writer would emit as a raw `<span>` that GitHub shows as prose, becomes inline code; a phrase holding a link is unwrapped instead, keeping the link, and listed. (The AsciiDoc sources use no such phrase; the legacy reference uses 1,035 of them.) A `[horizontal]` labelled list, which the DocBook backend writes as a two-column table, becomes a definition list again. Definition-list terms are made bold, because GitHub has no definition lists and pandoc writes each term as a line of its own. Lists whose items are single paragraphs are written tight. Tables are otherwise left as pandoc writes them: pipe tables, or HTML tables when a cell holds several paragraphs or blocks.
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

Names keep the legacy spelling and case, and the suffix is matched as `.html` or `.htm` in any letter case, because the sources spell it three ways (`DFRTagReference.HTML`, `DDGP2BasicConnectivity.htm`). To retarget, edit `LEGACY_RULES` in `convert.py` and rerun. Until those pages exist, the link checker's `--pending legacy-links.tsv` reports these links as pending instead of broken.

The DocBook stage's `--legacy-map TSV` is consulted **before** these rules, and is how the legacy reference conversion resolves the 600-odd links its sources carry into the converted guides. A row supplies the target of a page no rule describes, overrides a rule that names the wrong page (a merged operator page, say), or unlinks the reference and keeps its text when the page has no counterpart under `docs/`. A target may carry a `#fragment`; only the path part is made relative to the current page. Rows that match nothing, and rows that override a built-in rule, are both listed in the report - the second because it silently retargets a link that would otherwise have resolved. See `reference-links.tsv` below.

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
| `reference-pages.tsv` | `id` (a DocBook section id of the reference sources, or a source file name for a file whose root section carries none), `output` (relative to `docs/`; empty: not a page of its own), `title` (the section's heading; on a page's first row, the page title), `reason` (for the reader - why a row was moved, merged or left unconverted - not used by the script) | by hand, after `reference_split.py split --seed-pages` writes a row per section. Seeded `output` is what the built-in legacy rule gives that section's help page, so the seeded map already agrees with the links under `docs/` that point there; seeded `title` is the section's own title. Edited for the merges (several sections on one operator's page), for the few titles the sources get wrong, and for index sections that are not pages. See *The legacy reference* below. |
| `reference-links.tsv` | `legacy_page` (a help-file page name, matched case-insensitively), `target` (relative to `docs/`, optionally `#anchor`; empty: unlink and keep the text), `reason` | by hand; passed to the DocBook stage as `--legacy-map`. It resolves the links the reference sources carry into the converted guides, which the built-in rule knows nothing about, and retargets the handful of reference pages the rule would place wrongly. A target was found by reading the help page of that name - its `<title>` and its `rel="up"` chain name the section - and then finding that heading under `docs/`; the `bk0*` names have no help page left and were resolved from their link text. Every anchor in it was checked against `heading_slugs` for that page. |
| `declarations.tsv` | `id` (a DocBook section id), `declaration` (a D4 script written as one cell: a backslash, a tab, a carriage return and a newline are escaped as `\\`, `\t`, `\r` and `\n`, because the tables are tab-separated with no quoting) | generated by `reference_split.py declarations` from the decompiled help file and then committed, so a rerun of the conversion needs neither the help file nor Windows' `hh.exe`. Review it as a table rather than trusting it: it is the published signature of every operator and type, and it is the only part of a reference page that does not come from the DocBook sources. |
| `index-extra.md` | Markdown | by hand: the `## Project documents` section appended to `docs/README.md` (its links are relative to `docs/`, so in a scratch folder the link checker reports them missing). It is behind the section it generates - `docs/README.md` has gained bullets and the nested list of architecture pages since - so a run overwrites that section with a shorter one; read the diff before committing it. `docs-remove-old-sources` deletes this file. |

## The legacy reference: reference_split.py

`legacy/Documentation/Docs2.0/` is the DocBook 4.3 source of the published help collection, and the only source for the operator, type and library reference. It cannot go straight through the DocBook stage: that stage writes **one page per input file** and fails when two files resolve to the same output, while `SystemOperators.xml` alone holds 339 sections that become 321 pages, 13 of which merge several sections. `reference_split.py` reshapes the input first, so the one-file-one-page invariant stays as it is.

It is the tooling behind the `docs-convert-legacy-reference` chain ([plan WS8](../../docs/modernization-plan.md#ws8-documentation)), whose design notes record why the prose comes from `Docs2.0`, why the signatures come from the published help file, why library facts do not, and what the page layout is. It needs only Python; its output is then converted by `convert.py docbook`.

### declarations: the signature blocks

```
hh.exe -decompile C:\temp\chm legacy\Documentation\Dataphor.chm
python tools/docs-convert/reference_split.py declarations C:\temp\chm --out tools/docs-convert/declarations.tsv
```

Every operator and type section in the DocBook has its *Declarations* block replaced by a placeholder that the old publishing tool filled from a live catalog, so the signatures are not in the sources at all. `legacy/Documentation/Dataphor.chm` is those same sources already published and carries the filled blocks; this mode reads its `O-*.html` and `T-*.html` pages and writes one row per section. The decompile writes 3,330 files, so **send it to a scratch folder outside the repository and do not commit the dump** - `declarations.tsv` is committed instead, which is what keeps a rerun free of the help file and of Windows' `hh.exe`. The mode decodes every row it has escaped and compares it against what it extracted, and stops if any row does not round-trip.

A help page with no *Declarations* block (a category page, or one of the short cross-reference sections) contributes no row and is listed. Six operators and one type were added to the DocBook after the help file was published and so have no page in it at all; their documentation pages carry no *Declarations* heading rather than an empty one.

### split: one document per page

```
python tools/docs-convert/reference_split.py split legacy/Documentation/Docs2.0/SystemOperators.xml --seed-pages tools/docs-convert/reference-pages.tsv

python tools/docs-convert/reference_split.py split legacy/Documentation/Docs2.0/SystemOperators.xml legacy/Documentation/Docs2.0/SystemTypes.xml ^
    --pages tools/docs-convert/reference-pages.tsv --declarations tools/docs-convert/declarations.tsv --out C:\temp\refwork

python tools/docs-convert/convert.py docbook C:\temp\refwork\*.xml --out C:\temp\refout --pages C:\temp\refwork\pages.tsv ^
    --legacy-html --legacy-map tools/docs-convert/reference-links.tsv --global-ids --match-heading-text
```

`--seed-pages` writes a `reference-pages.tsv` row per section instead of splitting, so the 360 rows get reviewed rather than typed. The second command writes one DocBook document per page into `--out`, plus the `pages.tsv` that `convert.py docbook --pages` then reads; a page's file is named after its output path, flattened. Running either command twice over the same inputs gives byte-identical output.

What the split does, each point a rule rather than a heuristic:

- **The page map decides the layout.** A row's `output` names the page a section goes to, and **every** section id in the input must have a row, so nothing is dropped by omission. A row whose `id` names a *source file* (`System.iXor.xml`) addresses that file's root section when the root carries no id of its own, which is the only way to reach the 2004 `OperatorDocs/` files - they have no id anywhere. It is opt-in by row, because an id-less root is not always a page: `SystemOperators.xml`'s own root section has none either and stays unconverted. Such a page has no help page, so no generated navigation list can link it by name, and none ever needs to: a file's root section has no enclosing section to cut it out of. Rows sharing an `output` land on one page **in the table's row order** - which is why moving a row is how a merged page is given its primary section. An empty `output` means the section is not a page of its own: it stays inside the page of its nearest enclosing section that is one, or, having none, is not converted and is listed.
- **One document per page, rooted at `<article>`.** It carries the id and title of the page's first section and that section's children as its body, so that title is the page's single `#` heading; the page's further sections follow as body sections and become `##`. The `<article>` wrapper is what makes pandoc's DocBook reader take the title as the document title. Titles come from the map's `title` column, so merged sections can be told apart; where the map repeats the source title the source element is used, so the few titles with inline markup keep it. Index terms are stripped from titles, and the `*info` blocks (a per-section editing date) are dropped, as the old publishing tool did.
- **Placeholders are filled in or removed.** A `<?DocLib D4Field Declaration?>` inside a formalpara titled *Declarations* is replaced by that section's row from `declarations.tsv`; with no row, the whole formalpara goes, heading and all. Every other `<?DocLib ?>` and every `<?xm-replace_text ?>` is removed, and then so is any block it leaves with nothing to render - including a generated table whose only row was a placeholder, since its hand-written column headings are not content on their own. Only the ancestors of a removed placeholder are pruned, so hand-written content is never touched. A placeholder that survived into the output is an error: rendered as literal text it would read as published documentation.
- **Category pages get their index.** A category section's own paragraph is an unwritten placeholder in the sources, so such a page is its title plus a list of links to the pages cut out of it - one link per page, written as a link to the help page the built-in legacy rule sends there, so it resolves like every other reference link. No prose is invented for it. A page the rule cannot reach that way is listed under *pages-needing-a-legacy-map-row*, which names exactly the `reference-links.tsv` rows to add.
- **The sources are repaired where they are plainly wrong.** The DOCTYPE is dropped: seven of the reference sources declare one naming a DTD path that no longer exists, none of them needs it, and a DOCTYPE copied into the output would be followed by pandoc. Six other files of `Docs2.0/` do need theirs and so cannot be read by this script as it stands - four book parts use `&nbsp;`, and `DataphorSet.xml` and `DataphorSet2.xml` declare the set's file includes in their internal subset - but none of them is a reference source. A `url` attribute is stripped of the whitespace some of them carry, and a legacy page name that lost its suffix gets `.html` back; both are listed.
- **Processing instructions survive parsing.** `ElementTree` discards them unless its parser is built with `insert_pis=True`; losing them would look like success and would drop every signature. The surviving-placeholder check runs on the tree, not on the markup, because 50 of the sources' comments quote a `DocLib` instruction in prose.

The report lists, each only when it has entries: sections with no published declaration, sections with no id (they cannot be mapped, and stay inside their parent's page), sections not converted, sections moved off the page their help-page rule names (the merges, so a reviewer sees them), pages needing a `--legacy-map` row, page-map rows that matched nothing, urls normalised, and documents an earlier run left in the `--out` folder. That last one matters because the next command converts the folder's `*.xml`: a document whose page is no longer in the map is not in `pages.tsv` either, so `convert.py` names its output from its file name and publishes it. Use a folder of this run's own, or read that list. Errors - a section id with no row, one id in two files, two pages whose work file names collide, a surviving placeholder - are printed last and **nothing is written**.

### The run that produced `docs/reference/system-library`

`docs-reference-operators` landed the 342 pages of the core-library reference with these two commands, run from the repository root. Keep the work folder's path **short**: a 342-file command line is about 29 KB, Windows allows 32 KB and `cmd.exe` only 8 KB, so run it from bash or PowerShell and expect failure from a deep scratch path.

```
python tools/docs-convert/reference_split.py split legacy/Documentation/Docs2.0/SystemOperators.xml ^
    legacy/Documentation/Docs2.0/SystemTypes.xml legacy/Documentation/DocBookManuals/OperatorDocs/System.iXor.xml ^
    --pages tools/docs-convert/reference-pages.tsv --declarations tools/docs-convert/declarations.tsv --out C:/temp/dref/work

python tools/docs-convert/convert.py docbook C:/temp/dref/work/*.xml --out docs --pages C:/temp/dref/work/pages.tsv ^
    --legacy-html --legacy-map tools/docs-convert/reference-links.tsv --global-ids --match-heading-text ^
    --legacy-table C:/temp/dref/reference-legacy.tsv
```

Add `--pandoc PATH` where pandoc is not on `PATH`. **Send `--legacy-table` to a scratch path, never to `legacy-links.tsv`:** that file is the AsciiDoc run's output and the pending list CI reads, and writing the reference's own internal links over it would make CI's link gate meaningless. The conversion takes about 75 seconds and both steps are reproducible: a rerun over the same inputs writes byte-identical pages.

`docs/reference/system-library/README.md` is hand-written and is no source's output, so a rerun leaves it alone - as it leaves alone everything but the paths its page map names.

NOTE: the reference's code blocks are tagged by the heuristic and no code table is committed for them. The run tags 858 blocks, 639 `d4` and 219 `text`, and GitHub highlights neither language, so the handful the heuristic reads wrong - a result grid taken for D4, a line of prose in a listing - makes no visible difference to a page. If a `d4` highlighter is ever added to the documentation build, seed a table with `--code-table` and review it before trusting the tags.

**NOTE: pandoc drops the root element's id**, so a `#<id>` reference to a page's own title cannot resolve through `--global-ids`. The reference sources contain no such reference - every link in them names a help page, which the legacy rules and `--legacy-map` resolve - and references to ids *within* a page do work, because those ids land on body headings. A later source that cross-references a page by its root id would need the id carried onto the title heading in `convert.py`.

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
| `--legacy-map TSV` | targets for legacy pages, consulted before the built-in rule; columns `legacy_page`, `target`, `reason` (see `reference-links.tsv`) |

Only heading ids are anchors: an id on a table, figure or paragraph is not one GitHub keeps, so a reference to it stays unresolved unless `xref-fixes.tsv` redirects it. From Python, `import convert` and drive `Settings`, `Page`, `docbook_to_trees`, `load_page` and `finish` directly to supply a different resolver.
