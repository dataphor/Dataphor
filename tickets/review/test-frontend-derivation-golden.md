description: Every form the Dataphor server can generate from the sample and shipped database schemas is now saved as a reviewable file, so a change to the form generator shows up as a difference in the next test run instead of reaching a user.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/FrontendDerivationGoldenTest.cs, tests/goldens/frontend/, tests/DAE.Server.Tests/TestEnvironment.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/FormCustomizationTest.cs, AGENTS.md, .gitattributes, tickets/fix/bug-orderbrowse-derivation-fails.md
----
## What landed

The golden half of layer L5 of the WS3 test suite. `FrontendDerivationGoldenTest` (new, on the existing `FrontendFixture` server) derives every page type of every table variable the carried libraries define and compares each resulting form, character for character, with a file checked in under `tests/goldens/frontend/<Library>/<Surface>.<PageType>.xml`.

Vocabulary, for a reader without the ticket in front of them: **derivation** is the Frontend server library turning a D4 table expression into a form; a **surface** is a table variable (a table or a view) a library defines; a **page type** is the kind of form to build from it (`Browse`, `Edit`, …). The entry point is the D4 operator `Frontend.Derive(<expression>, <page type>)`, which returns the form as a single-line XML string.

- `tests/goldens/` is new, deliberately above the test project so that `test-device-sql-golden` (WS3 L3, not yet filed) can put its golden SQL beside it. The directory is found through a new `DataphorGoldensDir` `AssemblyMetadata` entry read by `TestEnvironment.GoldensDirectory`; no path is spelled in code.
- `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` makes the pass **write** the files instead of comparing them, delete the ones no case produces, and then fail the run anyway, so an update can never be mistaken for a pass. Documented in AGENTS.md beside `DATAPHOR_TEST_LOGGING`, with the rule that the diff is the review.
- `.gitattributes` gained `tests/goldens/** text eol=lf` (see *Beyond the ticket* below).
- `FormCustomizationTest.DeriveAcceptsTheBrowsePageType` is deleted, which its own `NOTE:` asked this ticket to do once the goldens covered Browse for every surface. `DeriveRejectsThePageType` keeps `DerivableSurface` and a remark pointing at the new pass.
- No `docs/migration-notes.md` entry: nothing an application, a deployment script or a C# extension can observe changes.

## The measurement in the ticket was wrong, and this is the correction

The implement ticket said 45 surfaces, 360 cases, 320 goldens, and that `System.Internet` defines no table variables. It does: seven of them (`XMLAttribute`, `XMLContent`, `XMLDefaultNamespace`, `XMLDocument`, `XMLElement`, `XMLElementParent`, `XMLNamespaceAlias`). The probe that produced the ticket's numbers never enumerated that library — in one probe run it failed to register, and in the other it was registered but left out of the enumeration. `System.Platform` and `SimpleDevice` do contribute nothing, as the ticket said.

Measured 2026-11-22, Debug, on this machine, by the test itself:

| | ticket said | actually |
|---|---|---|
| surfaces | 45, 7 libraries | **52, 8 libraries** |
| cases (× 8 page types) | 360 | **416** |
| goldens | 320 | **369** |
| failures, all `OrderBrowse` | 40 | **47** |

Total golden text 1,185,018 characters over 369 files, 2.0 MB on disk; largest 8,677 bytes (`Sample.IssueTracker/IssueDetail.Browse.xml`).

`tickets/fix/bug-orderbrowse-derivation-fails.md` has been corrected in place to 47 of 52 and told that this ticket has landed, because it is the next ticket in that chain and its old numbers would have sent its implementer looking for 40 goldens.

## What the reviewer should check hardest

**The granularity decision is the one real judgement call.** The whole cross product is *one* NUnit case, `EveryDerivedFormMatchesItsGolden`, not one case per surface. The reason is in a remark on the method: NUnit builds its case list at discovery, before any server starts, so a per-surface case list would have to come from a checked-in list or from the golden file names — and the set-agreement and expected-failure-drain checks need every case's answer in one place, which a test depending on its siblings having run would not have under the name filters AGENTS.md runs the suite with. The cost is that a failure is one red test naming up to 25 problems rather than N red tests. If the reviewer thinks per-file cases plus a separate agreement case is worth the second pass over the corpus, that is the thing to argue about.

**The goldens were spot-checked, not audited.** I read 6 of the 369 against the builder code rather than trusting the generator:

- `Security/Role.Browse.xml` — against `BrowseDocumentBuilder` with `SearchStructureBuilder` and `PluralStructureBuilder`: the `Main` source, the detail and lookup `showformaction`s with their embedded `Derive` documents, the Details/View/Navigation menus, `RootBrowseColumn` → search + grid + `GridBar`.
- `Sample.Forms/Customer.Report.xml` — against `ReportDocumentBuilder` constant by constant: `PageHeader`, `HeaderDate` at x 520, `RootDataBandHeaderBox` height 11 width 600 at (5,2), `PageFooterPageText` at x 290 and `PageFooterPageNumber` at x 310, `maxlength` taken from the column width.
- `Security/ChangePassword.OrderBrowse.xml` — against `OrderBrowseDocumentBuilder`: the synthetic `table of { OrderName : String, Description : String } { } order by { OrderName }` source and `MainGridColumnDescription` at width 45.
- `Sample.DetailAdd/Category.Edit.xml` — `SingularDocumentBuilder`: `Validate` source action, `RootEditColumn`, the laid-out `Element1` column.
- `Sample.Forms/Customer.Delete.xml` — `DeleteDocumentBuilder`: `RootDeleteRow`, `DeleteImage`, `DeleteGroup`, every field `readonly="True"`.
- `System.Internet/XMLElement.List.xml` — the library the probe missed, to confirm its surfaces derive like any other.

The other 363 were checked only by scanning: no file contains a machine name, user name, absolute path, GUID-shaped string or date-shaped string.

**The four set-agreement failures and both expected-failure branches were each made to fire.** I perturbed the tree and the table and read the messages:

| what I did | what the run said |
|---|---|
| changed one attribute in a golden | named the file and the first differing line, golden vs derived |
| deleted a golden | named the file to create and the update switch |
| added a stray file to the tree | named it as a golden no case produces |
| added a row matching a case that succeeds | named the row as matching nothing that fails that way any more |
| narrowed the `OrderBrowse` row to one surface | 46 cases reported as failing with no row covering them, report capped at 25 |
| ran update mode over the perturbed tree | wrote 369, deleted 1, restored both files byte-identical, failed the run |

**Cross-server determinism is checked by every run, not only by the old probe.** The goldens were written by one test process's server (its own temporary directory, library copies and catalog) and compared by a different process's fresh server, and all 369 matched. `DerivingTheSameFormTwiceGivesTheSameText` covers the within-process half, clearing the Frontend session's derivation cache between the two calls — without that clear the second call returns the first call's cached document and the case asserts nothing.

## Decisions worth a second opinion

- **The stored form is the derived document printed**, not `Derive`'s literal output: indented with tabs, one element per line, LF, UTF-8 with no byte-order mark, no XML declaration (the derivation engine never puts one in), ending in a newline. Both sides of the comparison go through the same printer. The declaration and the trailing newline are my calls, not the ticket's.
- **No normalizer**, as the ticket directed; the scan above is the evidence that none is needed.
- **The expected-failure table is a permission to fail, not a requirement.** One row — any surface, `OrderBrowse`, `bug-orderbrowse-derivation-fails`, `Elaborated column` — covers 47 cases; the 5 `OrderBrowse` cases that derive have goldens like any other. A row that no longer matches any failing case fails the run, which is how the row drains when the defect is fixed. The alternative reading, one row per failing surface, would have been 47 rows saying one fact.
- **`EnumerateSurfaces(IServerProcess, string)` takes the library name**, as the ticket asked, so `compat-corpus-harness` adds a caller. It interpolates that name into the D4 query text; the names come from the fixture's static list.
- **The surfaces come from the catalog, but the library list does not.** A table added to a carried library turns up as new cases; a whole new sample library does not. `test-samples-corpus` (plan, L7) owns breadth.

## Tripwires recorded, not filed

- `NOTE: accepted tradeoff` at the corpus list in `FrontendDerivationGoldenTest`: the `System` library's surfaces are left out because their forms change with every engine change and their goldens would churn on work unrelated to the Frontend. Revisit condition stated: a derivation defect found on a catalog table and not on a library table.
- `NOTE:` in the printer's documentation comment: no element of a derived form has text content today (verified across all 369 files), so re-indenting cannot change what a form says. If the engine ever emits an element with text in it, the printer needs `PreserveWhitespace` and the goldens need regenerating.

## Cost, measured

- The three cases of this fixture, run alone: 13.4 s wall clock for the whole `dotnet test` invocation, of which the golden pass itself reports 0.5–1 s for 416 derivations; the rest is server start and registering ten libraries, which `FormCustomizationTest` already paid for.
- Suite part 1 (everything but the coverage scripts): **5 m 8 s and 6 m 34 s** on two runs of the final code, 561 passed in `DAE.Server.Tests` plus 13 in `Common.Tests`. AGENTS.md records about 5.5 minutes before this change and says to split further past about 8 minutes; the spread between those two runs is this machine's noise, not this change, but part 1 is the one to keep watching.
- Parts 2 and 3: 3 m 15 s and 3 m 12 s, 0 failed. Full run green; nothing skipped but the coverage manifest's own known/unstable/fragment entries.

## Beyond the ticket

One change the ticket did not ask for: `.gitattributes` gained `tests/goldens/** text eol=lf`, with a comment. The pass writes and compares these files with LF, so a checkout that converted them would fail every comparison. It fits that file's stated policy exactly — line endings are pinned only for paths with no CRLF file in the index, and this tree is new — but it is a repository-wide file, so it should be looked at rather than waved through.

## Known gaps

- 363 of 369 goldens have been read only by the scan above. That is the nature of a golden set: the first commit is the one nobody can fully review, and every commit after it is a diff.
- The failure report is capped at 25 problems; a change that alters every form says "and 344 more".
- `library-frontend-web-leftovers` (backlog, WS7) deletes nine uncompiled `System.Web` files from `libraries/Frontend/Source`. None is in `Frontend.Server.csproj`'s compile list, so this pass must still build and pass after that deletion; nothing here depends on them.
