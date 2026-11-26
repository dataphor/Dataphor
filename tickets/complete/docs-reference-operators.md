description: Published a documentation page for every built-in operator and scalar type of the core library, so that the hundreds of guide links pointing at them now work.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/reference/system-library/, tools/docs-convert/reference-pages.tsv, tools/docs-convert/reference-links.tsv, tools/docs-convert/reference_split.py, tools/docs-convert/test_reference_split.py, tools/docs-convert/README.md
----
## What landed

343 files under `docs/reference/system-library/`: 297 operator pages, 25 operator-category index pages, 20 scalar-type pages, and one hand-written `README.md` for the section. `docs/` goes from 94 files to 437. Nothing else under `docs/` changed - `docs/README.md`, `docs/reference/README.md`, `docs/images/` and `tools/docs-convert/legacy-links.tsv` are byte-identical to before.

The pages are a conversion of the DocBook sources the published 3.x help collection was built from, driven by two committed tables: `reference-pages.tsv` (which section goes to which page, and under what title) and `reference-links.tsv` (where a legacy help-page link resolves to under `docs/`). An operator page carries the operator's published D4 signature, its remarks, its examples and its cross-references. The signatures come from the shipped help file by way of the committed `declarations.tsv`, so a rerun needs neither the help file nor Windows' `hh.exe`.

Five tool files changed in the implement stage, all small:

- **`reference-pages.tsv`** - one row added for `System.iXor`, eight titles changed.
- **`reference-links.tsv`** - 18 rows added and 3 rows emptied.
- **`reference_split.py`** - a page-map row may now key on a *source file name* to reach a file whose root section carries no id. One test.
- **`test_reference_split.py`** - that test, and an optional second source in the harness.
- **`README.md`** (in `tools/docs-convert/`) - the commands that produced the pages, the new page-map key, and a `NOTE:` about the code-block tags.

The review stage changed three title cells, three `reason` cells, four generated pages and four prose blocks; see *Review findings*.

## How it was validated

Every gate was run in the review stage against the final tree, not taken from the implement handoff.

| Command | Result |
|---|---|
| `dotnet build Dataphor.slnx` | 0 errors, 127 warnings (unchanged) |
| `python -m unittest discover -s tools/docs-convert` | 24 tests OK |
| `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` | `437 files scanned, 3197 links checked, 0 errors, 3 pending, 21 external` |
| `python tools/docs-size-check/check_size.py` | 13 files, 0 over the limit |
| full split + conversion into a scratch root, then `diff -r` against `docs/reference/system-library` | differs only by `README.md`, which no source produces |

The last row is the property the rest of the chain leans on: **the committed pages are exactly what the committed tables produce, so no page has been hand-edited and a rerun cannot silently lose an edit.** It was verified twice in review - once against the implement commit's tables and once after the review's own title change - and in both cases the only difference was the hand-written `README.md`.

The 3 remaining pending links belong to later tickets: `reference/libraries/SimpleDevice.md` (`docs-reference-libraries`), and `reference/frontend-nodes/Frame.md` plus the `System.SecurityOperartors` misspelling (`docs-convert-legacy-reference`). Both of those tickets name them explicitly.

## The correction most worth carrying forward

`convert.py` reported **0 unresolved references** and "2037 legacy links, 2037 with a target" on the first full run. The link checker then reported **27 errors**. Both were right: the converter only knows whether a rule or a map row *produced* a target, not whether that target names a page that exists. Seventeen legacy page names resolved by the built-in `O-<Name>.html` rule to pages nothing in this chain will create.

**"All links have a target" is not a completeness claim. The acceptance gate is the link checker.** `docs-reference-libraries` and `docs-reference-chapters` will meet this again.

The 17 were three kinds, each resolved by a `reference-links.tsv` row whose `reason` column records why:

- **Misspellings in the 2007 source links** where the section, and so the page, is spelled right - `O-System.ConCat`, `O-System.ReguiredLibraries`, two `…ErrorDecsription` variants, and `O-System.DefaultSetMaxCallDepth`/`DefaultSetMaxStackDepth`, which scramble the word order of `SetDefaultMaxCallDepth`. Retargeted to the real page.
- **Names the 2007 reference documents as a scalar type but links as an operator** - `O-System.Guid`, `O-System.TimeSpan`, `O-System.VersionNumber`. Retargeted to `types/`.
- **Things the product dropped** - the group-based security operators `System.CreateGroup`, `System.DropGroup`, `System.GroupExists`, and the pre-3.0 string types `System.String.ASCII`, `System.String.UTF8`, `System.String.Unicode`. Unlinked: the text stays, the link goes.

Two further rows fall outside those three kinds: `D4LGOperatorPrecedence.html` (the 2004 sources' name for the section the 2007 ones call `D4LGLanguageElements-Expressions-OperatorPrecedence`) and `O-System.UseImplicitTransactions.html` (no operator of that name; retargeted to its sibling setter `SetUseImplicitTransactions`, which links back). Both were spot-checked in review and are correct. One more row unlinks `O-Frontend.ExecuteScript.html`, the single link out of the core library, with a reason naming `docs-reference-libraries`.

**Three rows that pointed at `reference/libraries/` pages were emptied** (`DRMSSQLDeviceLibrary-Example`, `DRMSSQLDeviceLibrary-Settings`, `DRSimpleDeviceLibrary-Schema`): they named pages that do not exist yet, which is the same error class. Each carries a `reason` saying `docs-reference-libraries` fills it in - the record lives in the table, not only in this ticket.

**Do not delete the 11 rows the report lists as matching no link.** They serve the library and chapter sources the next two tickets convert.

## The one tooling change, and why it was needed

`legacy/Documentation/DocBookManuals/OperatorDocs/System.iXor.xml` - the only source for `System.iXor` anywhere - has no `id` attribute on its root `<sect2>` or anywhere in the file, and neither does any of the other 274 files in that folder. The splitter keys its page map on section ids, so there was nothing to address the file with.

A blanket repair would have been wrong: `SystemOperators.xml`'s own root `<section>` has no id either, and it must stay unconverted - it is the wrapper holding the 339 operator sections. So the mechanism is **opt-in by row**: a page-map row whose `id` names a source file (`System.iXor.xml`) gives that file's root section the file's name as its id, and the split reports it under `root-sections-addressed-by-their-file-name`. No DocBook id in the sources ends in `.xml`, so the two shapes cannot be confused.

Two consequences, both handled: `legacy_name` returns `None` for such a key (the 2004 files were never published, so there is no help page of that name), and the `pages-needing-a-legacy-map-row` check skips a page with no help-page name. That skip is safe for a structural reason: a file's root section has no enclosing section, so no category can cut it out, so no generated navigation list can link it.

## Tests

One added, `test_a_root_section_with_no_id_is_addressed_by_its_file_name` in `tools/docs-convert/test_reference_split.py`. It pins both halves of the new branch: with the row, the file's root becomes a page with the map's title and the report names it; without the row, the same root is **not** a page, that is not an error, and it is listed as having no id. The second half is the guard that keeps `SystemOperators.xml`'s wrapper section out of the output. Reviewed and kept as written.

Nothing else is tested, deliberately: the rest of the work is a document conversion whose gate is the link checker, which already fails when a referenced page is missing - and did, 27 times, which is how the legacy-map rows above were found. A test asserting a page count, or that a named operator page exists, would pin a number `docs-reference-libraries` changes next.

## Review findings

The implement-stage diff (`ticket(implement): docs-reference-operators`) was read before the handoff summary. The 343 pages were checked as output of a reproducible pipeline rather than line by line: the pipeline was rerun and `diff -r`'d (above), and then the pages were swept for the defect classes a conversion produces.

**Checked and clean, with how:**

- **Reproducibility.** Fresh split + conversion into a scratch root, `diff -r` against `docs/reference/system-library`: only the hand-written `README.md` differs. No page is hand-edited.
- **Placeholders.** No `<?DocLib ?>`, no `<?xm-replace_text ?>`, no "Under construction", no `TODO`/`FIXME` anywhere in the 343 files.
- **Empty headings and duplicate anchors.** Zero empty headings. 364 headings across the set (343 page titles + 21 subsections); no heading text repeats within any page, so GitHub mints no `…-1` anchor.
- **The seven pages with no *Declarations* block** (`System.ActualDate`, `ActualDateTime`, `ActualTime`, `IsNotNil`, `MonthsBetween`, `YearsBetween`, `types/System.GroupName`) were read end to end. None has an orphan heading; `System.GroupName` does carry a hand-written *Type Declaration*, so only the generated block is missing. The handoff's claim holds.
- **Raw HTML.** Exactly 20 `<sup>` elements on 14 pages, from `<superscript>` in exponent prose. The converter's own raw-HTML report lists the same 14 pages and nothing else.
- **Angle brackets that Markdown would eat.** Eleven lines contain bare `<No Value>`, `<no value>`, `<binary data>` or `<values>`. Every one is inside a fenced code block (checked by counting fences before each) or inside backticks, so none is swallowed as an HTML tag.
- **Line endings and byte-order marks.** All 343 files are LF with no BOM in both the working tree and the index; `git ls-files --eol` shows every pre-existing file under `docs/` is also LF *in the index* (the working-tree CRLF on the older files is `core.autocrlf` on checkout, not a stored difference). `.gitattributes` deliberately pins no `text` attribute on `.md`. Consistent; nothing to do.
- **Trailing whitespace.** Present on 89 of the new pages, and on 25 pre-existing converted pages: it is inside code blocks, faithful to the source, and pre-existing converter behaviour. Left alone.
- **The signature claim about `System.Execute`** was re-verified independently against `src/engine/DAE/Schema/SystemCatalog.d4` and against `git show 361ebd83:Dataphor/DAE/Schema/SystemCatalog.d4`. Both declare the same five `Execute` overloads and neither declares the published one-argument form, so the handoff is right that the gap is a 3.x fact and not a port regression.
- **The downstream handoffs named in the handoff are real and specific.** `docs-reference-libraries`, `docs-reference-chapters`, `docs-convert-legacy-reference`, `docs-remove-old-sources` and `docs-reference-generator` are all on the board, and the first and third name the pending links and the `reference-links.tsv` rows they inherit.
- **The added test** pins both halves of the new branch and is at the right layer. Kept unchanged; nothing cut.

**Found and fixed in this pass:**

- **Two category indexes mislabelled `System.IndexOf` and `System.LastIndexOf` as "(List)".** The generated category list takes its link text from the page's `title` cell, so a page title that carries an overload qualifier also labels every index entry for that page. `System.StringOperators.md` - the index of *string* operators - listed "System.IndexOf (List)" and "System.LastIndexOf (List)", and `System.ListOperators.md` listed "System.IndexOf (List)". The handoff weighed this title as a page-heading question and left it, inviting disagreement; it had not seen the index effect. Checking the sources settles it: the 2007 DocBook puts **both** the list and the string overload sections under the String Operators category, and puts only a stub under List Operators, so "(List)" in that index is wrong rather than merely over-specific - and the page holds both overloads, so the bare operator name is the honest page title. Three `title` cells changed (`O-System.ListIndexOf` and `O-System.ListLastIndexOf` to the bare name; the `O-System.IndexOfListOperator` stub to `System.IndexOf (List Operators)`, which is what it is - the List Operators category's cross-reference entry - and which keeps it from colliding with the new page title). Each cell's `reason` column now records the argument. Rerun: exactly 4 pages changed, 6 lines, nothing else, and reproducibility still holds.
- **The reproduction commands in `tools/docs-convert/README.md` could not be run as written.** All four used `^` line continuations - `cmd.exe` syntax, a literal argument in both bash and PowerShell - while the surrounding prose (added by this ticket) says to run them from bash or PowerShell precisely because `cmd.exe` cannot carry a 29 KB command line. Each command is now one line, with a sentence saying why, so a future editor does not re-wrap them. Met while reproducing the run.
- **Two `reason` cells stated a fact that is not true.** `O-System.DropGroup.html` and `O-System.GroupExists.html` said the operator is "deprecated in the catalog"; neither is declared in `src/engine/DAE/Schema/SystemCatalog.d4` at all. Only `CreateGroup` is there, tagged `DAE.IsDeprecated = "True"`. The decision (unlink) is unaffected and if anything better founded, but the table is the surviving record, so all three reasons now say what is actually the case.
- **The handoff's count of `reference-links.tsv` rows was wrong** - 18 rows were added, not 16, and the narrative's enumeration omitted `D4LGOperatorPrecedence.html` and `O-System.UseImplicitTransactions.html` entirely. Both were verified correct in review (the `#operator-precedence` heading exists; `System.UseImplicitTransactions` is not an operator, only `SetUseImplicitTransactions` and `SetDefaultUseImplicitTransactions` are) and are now described above. The handoff's related claim that the `System.IndexOf` page has "no general section above" its two overloads is also inaccurate - there is a content-free stub section - though it does not change the conclusion.

**Recorded as a tripwire rather than a ticket:**

- **`link_names` could pick a help-page-less id for a merged page.** If a file-keyed row were ever merged onto a page that also holds id-keyed sections, the preference loop could choose the file-keyed id, whose help-page name is `None`; that would suppress the `pages-needing-a-legacy-map-row` report for that page and could carry a `None` url into a generated list. It cannot happen today - the one file-keyed page is a single file root, which no enclosing section can cut out - so a `NOTE:` at the loop in `tools/docs-convert/reference_split.py` says what to do if that shape appears, rather than hardening against a case no input can reach.
- **A DocBook table with no `<thead>` renders with an empty header row.** `operators/System.iXor.md` is the one page of 342 shaped that way: its column headings become the first body row. GitHub renders it legibly and the fix would be a `convert.py` change touching all 437 files for one page, so a `NOTE:` in `tools/docs-convert/README.md` records the condition under which to promote the first body row instead.
- **The 11 operators documented only in the 2004 `OperatorDocs/` folder stay unconverted, on purpose.** `System.ConstraintType`, `System.DayOfMonth`, `System.Diagnostics.LoadCatalog`, `System.Diagnostics.SaveCatalog`, `System.RealSQLToD4`, `System.SetLanguage`, `System.String.ASCII`, `System.String.UTF8`, `System.String.Unicode`, `System.iBetween` and `System.iRedefine`. Verified in review that no link under `docs/` targets a page for any of them, and that three name types the engine schema no longer has. `docs-remove-old-sources` deletes that folder, after which they live only in git history, so the decision and the one-row-plus-rerun recipe to undo it are now a `NOTE:` in `tools/docs-convert/README.md` rather than only in this ticket.

**Filed as evidence on an existing ticket:**

- **The measured signature sweep** was appended to `tickets/plan/23.7-docs-reference-generator.md`, which already owns the class ("a generated signature that differs from the 3.x reference is evidence about the port"). Of the 282 operator names that appear both in the published pages and in the catalog script, 262 have identical parameter lists once the help file's `generic list`/`row`/`cursor` spellings are read as the bare types the script writes; 12 are overloads the script declares and the help file never listed; 7 are selectors and comparison operators a scalar type's `representation` generates rather than a script declaring them. The twentieth is the real one - `System.Execute(const AScript : String)` is published and does not exist - and it is now on that ticket as the first case its generated-versus-published diff should explain. No new ticket: the Nth instance of a class that already has a ticket is evidence, not a ticket, and the sweep covers only the names the catalog script declares, so it is a spot check and is described as one.

**Considered and declined, with the reason:**

- **The 16 cross-reference stub sections still read as self-references** ("See [System.iIndexer](System.iIndexer.md)" from inside `System.iIndexer.md`). That is the source's own text and the merge is what the links require; dropping a section would mean inventing an editorial policy that deletes published content. Left as the implement stage left it. The honest fix is a hand-edit pass once `docs-remove-old-sources` deletes the sources and these become ordinary documents.
- **No code-block table is committed.** The run tags 858 blocks, 639 `d4` and 219 `text`, and GitHub highlights neither language, so the handful the heuristic reads wrong makes no visible difference; an 858-row table nobody has genuinely read would be false assurance. The decision and its revisit condition (a `d4` highlighter in the documentation build) are a `NOTE:` in `tools/docs-convert/README.md`. Reviewed and agreed.
- **Source warts in the converted prose are left alone,** because a hand-edit to a converted page is lost on the next rerun: `System.iXor.md`'s first sentence is garbled in the 2004 source, and `System.ActualDate.md`'s example output shows a time under a column named `ActualDate`. Same hand-edit pass as the stubs.
- **`docs/reference/README.md` is still not an index of the new section,** so today the system library is reachable only by direct link. `docs-convert-legacy-reference` rewrites that index and says so in its own body.
- **The system tables reference does not exist, and no empty page was created for it.** Both `SystemTables.xml` sources are four-line stubs; `docs/reference/system-library/README.md` says so in its own section and names `docs-reference-generator`.

**Nothing was filed as a new ticket.** Every finding either resolved inline, attached as evidence to the ticket that already owns its class, or is genuinely conditional and sits as a `NOTE:` at its site. Nothing met the filing bar on its own.

## Which tests were run, and which were not

Build (`dotnet build Dataphor.slnx`, 0 errors), the Python tool tests, the link checker and the documentation size checker - the full set of gates that can fail on this diff, each run after the review's own edits.

**No .NET test fixture or D4 coverage script was run, and that is deliberate:** the diff contains no `.cs`, no `.d4`, no project or build file, and no change to the coverage runner or manifest. It is documentation under `docs/`, three tab-separated tables, one Python module and its test, and two ticket files. There is no engine behaviour for a fixture to characterize. CI runs the whole suite in Debug and Release on the push that carries this.

## Pre-existing failures

None met. Nothing failed, so `tickets/.pre-existing-known.md` was not consulted and `tickets/.pre-existing-error.md` was not written.

The tool tests are still not run by CI - `debt-tool-tests-not-run-by-ci` (backlog) owns that gap, and both the implement and review stages ran them by hand.

## Compatibility and migration

No compatibility surface is touched: no public type or member, no D4 semantics or operator signature, no registered class name, no file format, no error code, no WCF contract, no port or on-disk layout. Documentation and a Python tool only, so **no `docs/migration-notes.md` entry**.
