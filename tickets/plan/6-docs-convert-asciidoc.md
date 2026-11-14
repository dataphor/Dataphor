description: Convert the product's user guide, developer guide and reference introduction from their old publishing format into plain Markdown pages in the repository, with working cross-links and images that display on GitHub.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws8-documentation
files: Documentation/SUMMARY.adoc, Documentation/README.adoc, Documentation/GLOSSARY.adoc, Documentation/BIBLIOGRAPHY.adoc, Documentation/AuthoringDocumentation.adoc, Documentation/UsersGuide, Documentation/DevelopersGuide, Documentation/DataphorReference, Documentation/Images, book.json, legacy/Documentation/DocBookManuals, docs/, tools/
difficulty: hard
----
## Goal

Turn the AsciiDoc documentation in `Documentation/` into GitHub-flavoured Markdown under `docs/`, readable on GitHub with no site generator (D9), and keep the scripts that did it under `tools/`. Today the books are only readable through a GitBook setup that no longer exists; the plan's target is one Markdown tree that agents and people read in place. The second half of WS8 (`docs-convert-legacy-reference`, in `feat-ws8-documentation-remainder`) reuses this ticket's tooling to convert the legacy DocBook reference, so the tooling is part of the deliverable, not a throwaway.

`Documentation/` and `legacy/Documentation/` are not moved by `repo-move-to-new-layout` and stay in place until `docs-remove-old-sources`. **This ticket writes only under `docs/` and `tools/`.** It does not delete or edit anything in `Documentation/`, `legacy/Documentation/`, `book.json` or the root `README.md` (whose links into `Documentation/` are repointed by `docs-remove-old-sources`).

## Scope

In scope, per the plan's WS8 "Target tree":

- `Documentation/UsersGuide/` (26 files) → `docs/users-guide/`.
- `Documentation/DevelopersGuide/` (36 files) → `docs/developers-guide/`.
- `Documentation/DataphorReference/` (8 files) → `docs/reference/`: `D4LanguageGrammar.adoc` → `d4-grammar.md`, `Tags.adoc` → `tags.md`, `ErrorMessagePrefixCodes.adoc` → `error-codes.md`, `DataphorReference.adoc` → `docs/reference/README.md`. The four stubs (`Libraries.adoc`, `FrontendComponents.adoc`, `CLI.adoc`, `DataAccessComponents.adoc`, about 58 words each, "Under construction") become short placeholder pages where the target tree puts that material: `docs/reference/libraries/README.md`, `docs/reference/frontend-nodes/README.md`, and two pages for the call-level interface and the data access components (the target tree has no slot for them; the planner names one). `docs-convert-legacy-reference` fills them.
- `SUMMARY.adoc` (the GitBook table of contents) and `README.adoc` (the introduction) → the `docs/README.md` index, which must also link the existing `architecture.md`, `modernization-plan.md` and `migration-notes.md`. `GLOSSARY.adoc` and `BIBLIOGRAPHY.adoc` → pages under `docs/` (planner names them; not in the target tree).
- Images referenced by the converted set → `docs/images/`, PNG and SVG only (BMP, GIF and JPG converted to PNG).
- Anchors and cross-references resolved to file-relative Markdown links; a link check that is clean within the converted set.
- A list, produced by the tooling, of every link into the legacy `.html` reference pages (206), for `docs-convert-legacy-reference`.

Out of scope:

- Converting `legacy/Documentation/` (DocBook) and repairing the 206 legacy links — `docs-convert-legacy-reference` (`feat-ws8-documentation-remainder`). The design should let that ticket reuse the DocBook → Markdown step unchanged.
- Deleting the old sources (`docs-remove-old-sources`), the link checker in CI (`docs-link-check-ci`), the contributing guide (`docs-contributing`, which also folds `Error Message Prefix Codes.txt` into `docs/reference/`; coordinate by leaving `error-codes.md` as the converted table so that ticket merges into it).
- `AuthoringDocumentation.adoc`: it describes the GitBook editing workflow, which this conversion retires. Do not convert it; `docs-contributing` writes the replacement "writing docs" page. Mention it in the handoff.
- Editing the content. This is a format conversion; outdated statements stay (they are corrected by later tickets or by `docs-admin-guide`). Fixing a broken link or a mis-encoded character is not a content edit.
- Source art in `Documentation/Images/Working/` (9 CorelDraw `.cdr` files): not converted.

## Settled constraints

- D9: Markdown under `docs/`, readable on GitHub, no site generator.
- D12: no history rewrite; the old files stay until `docs-remove-old-sources`.
- Plan WS8 design note: AsciiDoc → DocBook (Asciidoctor) → GitHub-flavoured Markdown (pandoc) is the recommended route, because the same second step converts the legacy DocBook; a rewrite pass then maps anchors and cross-references to file-relative links, tags literal blocks, converts BMP to PNG, and handles the dead links. **The source files declare `iso-8859-1`; check the actual bytes before converting** (done below: they are UTF-8).
- Fixes K17 only to the extent of listing the 206 links; their repair is `docs-convert-legacy-reference`.
- Privacy (`tickets/rules/10-dataphor.md`): the docs use sample schemas only; keep it that way.

## Facts verified in the tree

- **Files and size.** 75 `.adoc` files, about 189,000 words: User's Guide 26 files / 35,600 words, Developer's Guide 36 / 141,000, Reference 8 / 7,100 (two real chapters, `D4LanguageGrammar` 2,800 words and `Tags` 3,700; the rest stubs or index), and 5 top-level files / 5,700 (`README`, `SUMMARY`, `GLOSSARY` 3,900, `BIBLIOGRAPHY`, `AuthoringDocumentation`).
- **How the books are assembled.** There is no master file and no `include::` directive anywhere. Each file is a standalone document (`:doctype: book` in 70, `chapter` in 4). The books exist only through `Documentation/SUMMARY.adoc`, a GitBook 3 table of contents (nested `link:` lists), and the root `book.json` (GitBook config: `root: ./Documentation`, plugins `navigator`, `expandable-chapters-small`, `addcssjs`, `theme-api`). `Documentation/.bookignore` excludes `./Legacy/*`. Each book also has its own index file (`UsersGuide.adoc`, `DevelopersGuide.adoc`, `DataphorReference.adoc`) that links its chapters.
- **Encoding.** Every file carries `:encoding: iso-8859-1`, but all 75 decode as UTF-8: 50 are pure ASCII and 25 contain UTF-8 non-ASCII characters (no BOM, CRLF line endings). The characters are U+00A0 no-break space (307), U+2013 en dash (7), ®, ©, ™, and one U+00C2 in `DevelopersGuide/OperatorMapping.adoc` (`iMultiplication(ShippingRate,Â Distance`), which is double-encoded mojibake of a no-break space. Read as UTF-8 and ignore the declared attribute; reading as Latin-1 would garble all 25. Decide whether no-break spaces become ordinary spaces (they sit mostly inside code samples).
- **Structure counts.** 649 explicit `[[…]]` anchors, all at line start, all unique across the set (DocBook-era IDs such as `D4LGCatalogElements-Objects-Metadata`). 525 `<<…>>` cross-references, of which 5 are BNF text in `D4LanguageGrammar` (`<< | >>` and similar), not references. 757 literal blocks (`....`), 4 listing blocks (`----`), no `[source]` blocks, so no language tags. 62 tables, 33 footnotes, 11 admonitions (8 `NOTE`, 2 `WARNING`, 1 `IMPORTANT`), 151 `link:` targets to other `.adoc` files, 22 external `http(s)` links, 103 image macros.
- **Cross-references target heading text, not the anchors.** About 400 references have the form `<<File.adoc#Heading_Text, label>>` and about 100 `<<Heading_Text, label>>`, where `Heading_Text` is the section title with spaces replaced by underscores, case kept (for example `<<../DevelopersGuide/D4Catalog.adoc#Metadata, …>>` points at `=== Metadata`, whose explicit anchor is `D4LGCatalogElements-Objects-Metadata`). Asciidoctor's default generated IDs (`_metadata`) do not match this, so the rewrite pass must resolve references by heading text and emit GitHub's heading slugs (lowercase, hyphenated, `-1`, `-2` suffixes for repeated headings in one file). Checked against the sources: 498 resolve to a heading in the named or current file; **about 22 are broken in the source** and need a by-hand decision, which the handoff lists: 7 name a file that does not exist (wrong case or path, for example `D4ScriptsandExecution.adoc`, `../DataphorReference.adoc#…`, `D4LanguageGrammar.adoc` from the wrong folder), 7 name a heading that is not in the target file (some match an explicit anchor instead, such as `ScalarTypeMapping.adoc#DDGScalarTypeMapping`), and 8 bare references name a heading that is not in the current file (6 of them, such as `Select_Statement` and `Libraries`, exist in several other files). Two `link:` targets name `Tutorial_FrontendComponents.adoc`, which does not exist (the chapter is `Tutorial_SampleComponents.adoc`). Explicit anchors that nothing references can be dropped or kept as HTML anchors; decide.
- **Literal blocks.** Of the 757, 150 are BNF grammar productions (`<ddl statement> ::= …`; mostly `D4LanguageGrammar` and the language guide) — they start with `<` and must not be tagged as XML. 5 look like XML, and the remaining about 600 are D4 or plain text (any SQL is mostly lower-case, so it does not stand out by keywords). The tagging rule therefore needs manual review, not keyword guessing alone.
- **Legacy reference links.** 206 `link:…html[…]` references to 107 distinct legacy page names (`O-System.All.html` for operators, `T-System.Boolean.html` for types, `DRSimpleDeviceLibrary.html` for library chapters, `Alphora.Dataphor.DAE.Client.DataFieldProperties.html` for API pages). These are the pages the DocBook build generated from `legacy/Documentation/DocBookManuals/` (408 XML files in `legacy/Documentation`).
- **Images.** `Documentation/Images/` holds 107 files (59 BMP, 33 PNG, 10 SVG, 4 GIF, 1 JPG; about 30 MB, the BMPs 28 MB, largest about 1.1 MB) plus 9 `.cdr` files in `Images/Working/`. The 103 image macros reference 98 distinct files; 9 files are unreferenced (7 BMP, `InventoryDiagram.svg`, `dataphor.jpg`); `Dataphor Overall - Simplified.svg` is referenced only in URL-encoded form (`Dataphor%20Overall%20-%20Simplified.svg`) and counts as referenced. Unreferenced images need not be converted; list them in the handoff. Paths are relative (`image::../Images/Name.ext[…]`). Rename converted files to names without spaces.
- **Tools on the survey machine.** Neither Asciidoctor (Ruby or the Node package), pandoc, ImageMagick nor Python's Pillow is installed; Python 3.14 and Node 24 are.

## Open choices for the planner

1. **Toolchain.** Recommended by the plan: Asciidoctor (DocBook 5 backend) then pandoc (`docbook` reader, `gfm` writer), followed by a Python rewrite pass. Alternatives: a direct AsciiDoc → Markdown converter (for example `downdoc`, or a pandoc AsciiDoc reader if the installed version has one), or a custom converter for the small AsciiDoc subset this corpus uses (no includes, no attributes beyond the header, one block type). Weigh fidelity and the reuse of the DocBook step for `docs-convert-legacy-reference`. State what must be installed (Ruby + `asciidoctor` gem or the Node `asciidoctor` package; pandoc; Pillow or ImageMagick for images) and pin the versions used in the tool's README. **The conversion scripts run once, by a person or agent, not in CI**; only the link check later moves to CI (`docs-link-check-ci`), so the link checker should be a separate script with no dependency on the conversion toolchain.
2. **File granularity.** One Markdown file per source file (one per chapter; keeps cross-reference mapping one-to-one, about 70 files) versus splitting long chapters per section (the Developer's Guide averages 3,900 words per file; some are far longer). Recommended: one file per source file; splitting can come later without changing the link scheme much.
3. **Language tags for literal blocks** (`d4`, `sql`, `xml`, plus whatever BNF should be: `ebnf`, `bnf` or untagged `text`). A heuristic first pass with a reviewed override list per block, kept with the tooling so a rerun is reproducible. GitHub highlights `sql` and `xml`; `d4` has no highlighter but makes blocks findable for the plan's later "run doc samples as tests" idea.
4. **Tables and footnotes.** GitHub tables cannot hold multi-paragraph cells or nested blocks; pandoc falls back to HTML tables for those. Decide whether HTML tables are acceptable (GitHub renders them) or such tables get restructured. GitHub renders `[^n]` footnotes.
5. **The 206 legacy links in the converted pages.** They must not break the link check, and the follow-up must be able to find them. Options: keep each as a link to a computed future path under `docs/reference/` (for example `reference/system-library/<operator>.md`) and have the link checker accept exactly the paths in the tooling's pending-links list; or replace each with plain text plus a marker comment. Recommended: the computed future path plus the list, so `docs-convert-legacy-reference` only has to create the pages. The list itself (source file, line, link text, legacy page name, proposed target) lives under `tools/` beside the converter; name its format.
6. **Admonitions:** GitHub alert syntax (`> [!NOTE]`) or plain bold labels.
7. **Location and shape of the tooling:** one folder (for example `tools/docs-convert/`) with a README that states the commands, inputs, outputs and versions, and keeps the DocBook → Markdown step callable on its own for the follow-up.

## Done when

- `docs/users-guide/`, `docs/developers-guide/`, the `docs/reference/` pages listed above, the glossary and bibliography pages and a `docs/README.md` index exist, one Markdown file per source file (or per the planner's granularity choice), with headings, lists, tables, footnotes, admonitions and literal blocks preserved, and every literal block tagged per the chosen rule.
- Every image the converted pages reference is in `docs/images/` as PNG or SVG and displays on GitHub; no BMP, GIF or JPG under `docs/`.
- The link checker reports zero broken internal links and anchors within `docs/` (external `http(s)` links are reported, not required to resolve), with the legacy-link handling of open choice 5 as the only accepted exception; the 22 source-side broken references are resolved or explicitly listed with the decision taken.
- The tooling under `tools/` reproduces the converted tree from `Documentation/`, and its README says how; the legacy-link list is produced by it.
- A spot check of several chapters against their AsciiDoc source (headings, a large table, a BNF block, an image, a footnote, a cross-file reference) is described in the handoff.
- Testing rule: no code behaviour changes, so no characterization test; the link check is the verification.
- Migration notes: none owed (no change an application, deployment or extension notices).

## Risks

- The heading-text cross-reference convention is GitBook-specific; a toolchain that relies on Asciidoctor's generated IDs will silently produce dangling links, which only the link check will show.
- Treating the declared `iso-8859-1` as true garbles 25 files.
- Auto-tagging BNF blocks as XML.
- Very large converted files (the Developer's Guide chapters) are harder to review; check a sample of each book by hand.

`legacy/Documentation` (DocBook 4.3) can be converted by the same second step (DocBook → Markdown with pandoc plus the rewrite pass); `docs-convert-legacy-reference` reuses it, so keep that step independent of the AsciiDoc front end.

If this splits, the ticket that finishes the work item keeps the slug `docs-convert-asciidoc`; earlier pieces get new slugs and are chained to it with `prereq:`.
