description: The last of the old reference material is now in the new documentation: the three remaining placeholder pages were replaced with real ones, the final two broken links were repaired, and every link in the documentation now points at a page that exists.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/README.md, docs/reference/README.md, docs/reference/frontend-nodes/README.md, docs/reference/call-level-interface.md, docs/reference/data-access-components.md, docs/users-guide/security.md, docs/users-guide/tutorial-movie-rental.md, Documentation/UsersGuide/Security.adoc, tools/docs-convert/convert.py, tools/docs-convert/test_convert.py, tools/docs-convert/README.md, tools/docs-convert/index-extra.md, tools/docs-convert/legacy-links.tsv, docs/modernization-plan.md, tickets/backlog/feat-frontend-node-reference.md
----
## What landed

The fifth and last ticket of the `docs-convert-legacy-reference` chain. `python tools/docs-link-check/check_links.py docs` - with **no** `--pending` argument - reports 0 errors over 3,433 links in 452 files, which is what the whole chain existed for and which closes known defect K17.

Three pages that said "Under construction" were replaced:

| Page | What it is now |
|---|---|
| `docs/reference/frontend-nodes/README.md` | The statement that there is no element reference and no source for one, a table of the seven places that do cover forms and derivation, and the one convertible thing in that chapter: prose from a 2004 DocBook file about what a user-interface document is made of, every claim re-checked against the shipping client. |
| `docs/reference/call-level-interface.md` | A hand-written overview of the interfaces a .NET program connects, prepares, executes and reads results through, and the simpler native entry point beside them. |
| `docs/reference/data-access-components.md` | The same for the components and data-aware controls a Windows application binds to. |

`docs/reference/README.md` became the section index: a nine-row table of what is in each section, the two sections that are deliberately absent with a link to the page explaining each, and the editor shortcut keys' real location. The last two broken links were closed at the source *and* the page - one misspelling in `Documentation/UsersGuide/Security.adoc`, and one `LEGACY_RULES` rule in `tools/docs-convert/convert.py` retargeted to the Frontend Components index, since the per-element page it named will never exist.

`docs/modernization-plan.md` records the work item as done, marks K17 resolved, and carries a new design note, *What the legacy reference conversion gave up*, listing the five parts of the 3.x reference with no convertible source and who owns each.

**The AsciiDoc rerun the ticket asked for was deliberately not performed**, because a rerun now reverts hand-written pages rather than reproducing them - the converter's own design makes the Markdown the source of truth once it has landed. The implementation took its documented fallback instead: fix the AsciiDoc source *and* the page, and regenerate `legacy-links.tsv` by pointing `--out` at a scratch folder, which refreshes that file without writing anything under `docs/`. The review verified both halves of that call (see below).

## Review findings

Read the implement diff (`ticket(implement): docs-convert-legacy-reference`) before the handoff summary. 15 files, +298/-145.

**Claims checked against the code, and found correct.** Every type, namespace, file and assembly name on the two new overview pages: the six CLI interfaces and their namespace and file (`src/engine/DAE/CLI.cs`, assembly `Alphora.Dataphor.DAE`); `ServerFactory.Connect` and its assembly `AlphoraDataphorClient`; the three native-CLI types and their assembly; the six DAC components, `DataView`'s declaration in `View.cs` and its `TableDataSet` / `DAEDataSet` ancestry, the five ADO.NET provider types and the two further assemblies. `View-263.cs` really is excluded - the csproj has an explicit `<Compile Include>` list and that file is not in it, so the page's warning about meeting two classes of the same name is right and useful. The accelerator behaviour on the Frontend page matches `AcceleratorManager` in all four of its branches, the doubled-ampersand and leading-tilde escapes included. The BOP namespace URI, the `MainSourceNotSpecified` error, `Frontend.GetNodeTypes`, `Windows.NodeTypes.dnt` (85 lines), `DesignerImageAttribute`, the 297/25/20 page counts behind the index's System library row, the 365 pages under `docs/reference/`, and every ticket slug the new text names (five of them, all on the board) all hold.

**The central judgment call - refusing the rerun - was right, and is now verified rather than asserted.** The review re-ran the converter with `--out` pointed at a scratch folder and compared that folder against `docs/` file by file. Two results. First, the committed `legacy-links.tsv` came back **byte-identical** (same MD5, no diff), which is the proof the handoff asked for: the two changed rows were produced by the tool from the fixed source and the retargeted rule, not typed in by hand, and nothing under `docs/` was touched by the run. Second, the destructive-rerun measurement reproduced and had moved: a rerun would now change **23 of the 170 pages the AsciiDoc stage writes**, +181/-494 lines, and write back the removed Web client screenshot. The handoff said 20, measured correctly on the clean tree *before* its own three hand-written pages existed. Both records were updated to the current figure, with the non-destructive method to re-measure it written down beside the number.

**Minor findings, all fixed in this pass.**

- **A fourth interface file was missing from the page's one practical instruction.** `docs/reference/frontend-nodes/README.md` sent a reader to three files, but `src/client/Frontend.Client/Interfaces.cs` carries 43 more `<doc/>`-marked declarations, and they are not marginal: the menu, toolbar, timer and every grid-column node live there - ten node types a form document can name, including all six column types. The page, its *Where to read instead* table and `tickets/backlog/feat-frontend-node-reference.md` (header and prose) now name four files and say what the fourth holds.
- **The `<doc/>` count was attributed to the wrong thing.** "292 of them across the three interface files" counted marks on interfaces *and* their members, over three of the four files, and called them all properties. Re-measured across all four: 335 marks, 56 on an interface and 279 on their members. The page now says that, which also tells a reader roughly how many elements there are.
- **"An `interface` may contain exactly one element node"** contradicted the empty-interface example three lines above it and the code: `Interface.IsValidChild` admits an element child only while `_rootElement` is null, so zero is legal too. Now "at most one element node - the empty one above has none".
- **The front page of the documentation set still promised what the chain had just declined to provide.** `docs/README.md`'s *Dataphor Reference* paragraph - the sentence the implement pass rewrote on `reference/README.md`, one level down - still offered "class references for using Dataphor's Data Access Layers (DAL) from a Microsoft .NET language such as C#", which the two new pages explicitly say does not exist here. Rewritten to describe what the section is and to say the generated class reference is not in it. Its contents list also omitted the System library, the largest section of the reference at 342 pages; added.
- **`tools/docs-convert/README.md` miscounted its own new table and misattributed two pages.** "Four pages ... `docs-convert-legacy-reference` wrote all four and the section index" describes a table of six pages in five rows, two of which were written by `docs-reference-operators` and `docs-reference-libraries` (confirmed in their commits). Corrected. In the same table, "lists eight sections, not the ten this release has" was wrong twice: the AsciiDoc source lists **seven** - the page's eighth entry, *Compiler Defines*, was added by hand by `docs-contributing` - and this release has **nine**.
- **The plan's pre-work survey still sent a reader after a source that does not exist**, calling `legacy/Documentation/` the only source for "the Frontend and form-node (DIL) reference". The handoff flagged this and left the choice to the review. The dated measurement is kept, with a one-sentence inline correction after it in the style the plan already uses elsewhere, because this is not a figure that went stale - it was wrong when written, and the design note that contradicts it sits a few paragraphs below. The same sentence's "keyboard shortcuts" clause was checked and **left alone**: `legacy/Documentation/Docs2.0/Keys.xml` does exist, so that clause is not plainly wrong even though the table under `docs/` came from the AsciiDoc source.
- **The next ticket's own record was about to be deleted with the folder it lives in.** `docs-remove-old-sources` deletes `tools/docs-convert/` entirely, and that README is where all five chain tickets recorded which pages are hand-written, where the published operator signatures came from, and why two tables were re-laid. Most of it survives on the pages themselves and the rerun warnings stop mattering once the inputs are gone, so the expectation is that nothing moves - but an arm was appended to `tickets/implement/23.9-docs-remove-old-sources.md` telling it to read that record once and move anything still wanted into the plan, rather than discover the loss afterwards.

**Major findings: none.** Nothing needed a ticket. The work is documentation and a four-line change to a by-hand tool; there is no architectural seam to make an invariant out of, and the one invariant that matters here - every link resolves to a page that exists - is already enforced by a CI gate over all 3,433 links.

**Tripwires: none recorded, and one candidate deliberately not recorded.** The converter README's growth (281 lines, 8,066 words) and the stale `page`/`line` columns in `legacy-links.tsv` would normally be worth a `NOTE:` at the site - but `docs-remove-old-sources` is the next ticket in the chain and deletes both files, so a tripwire there would be read by nobody. The handoff's own conditional note about `index-extra.md` falling behind again is already a sentence in that README, and is left where it is for the same reason.

**Accepted tradeoffs met: none.** No `NOTE: accepted tradeoff` exists at any site this diff touches.

## Tests

One added, in `tools/docs-convert/test_convert.py`: `LegacyRuleTest.test_an_element_reference_link_goes_to_the_section_index` pins the retargeted rule. It earns its place because the rule the ticket changed is the only entry in `LEGACY_RULES` with a fixed target rather than a captured name - a shape no existing case covered, and the one the `.format(*match.groups())` call now has to handle with no groups - and because restoring a per-element target is exactly the kind of change `feat-frontend-node-reference` would make, which should be deliberate rather than quiet.

Nothing else was added. No behaviour is ported here, and the gate that matters is stronger than a test: the link checker resolves every path component of 3,433 links against the real directory listing, letter case included.

## Gates run

```
python tools/docs-link-check/check_links.py docs
  452 files scanned, 3433 links checked, 0 errors, 0 pending, 17 external
python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv
  452 files scanned, 3433 links checked, 0 errors, 0 pending, 17 external
python tools/docs-size-check/check_size.py             13 files checked, 0 over the limit, 0 excepted
python -m unittest discover -s tools/docs-convert      Ran 30 tests, OK
python -m unittest discover -s tools/docs-link-check   Ran 14 tests, OK
python -m unittest discover -s tools/docs-size-check   Ran 7 tests, OK
dotnet build Dataphor.slnx                             0 Errors, 127 pre-existing warnings
```

The two neighbouring tool suites are the one-ring-wider set for this diff: no C# changed, so no `--filter` test run applies, and the build was run because a build error is never left for CI. The converter's own tests are not in CI; `debt-tool-tests-not-run-by-ci` owns that.

## No compatibility surface changed

Documentation, one tool rule and one tool test. No public type or member, no D4 semantics or operator signature, no registered class name, no assembly name or namespace, no file format, no error code, no WCF contract, no port, no on-disk layout. `docs/migration-notes.md` is correctly untouched.
