description: A view can carry presentation settings such as the title a form shows, and so can the table it is built from. The view's own setting was being quietly replaced by the one inherited from the table underneath; it now wins, and two of the shipped Security library's form surfaces change as a result.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/Schema.cs, tests/DAE.Server.Tests/ViewTagPrecedenceTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/goldens/frontend/Security, docs/architecture/catalog-and-store.md, docs/migration-notes.md
repro: verified
difficulty: medium
----
## What landed

The rule the owner settled on 2026-11-23 — **a view's own tags always win over every tag it inherits from its source** — is now what the engine does, is written down, and is pinned by a test.

**The fix, one line.** `src/engine/DAE/Compiling/Compiler.cs:4425` (was 4415 before the prereq ticket added three comment lines above it), in `CompileCreateViewStatement`:

```csharp
node.View.MetaData = localStatement.MetaData == null ? null : localStatement.MetaData.Copy();
```

with a three-line comment saying why. Before, the view and the create statement shared one tag collection, so the `MergeMetaData(localStatement.MetaData)` at `:4476` that is meant to put the view's own tags back on top merged that collection into itself and restored nothing — whatever the inherit at `:4473` or the merge at `:4475` had overwritten stayed overwritten. The copy is what gives the merge-back something to merge back from. The prereq ticket had already made this change at the other eleven create-statement sites, so the idiom and its reasoning were in the file.

**The doc comment on `Schema.Object.InheritMetaData`** (`src/engine/DAE/Schema/Schema.cs:511`) said "if the metadata for this object does not already contain it", which is false of the implementation: `Tags.Inherit(tag)` (`src/engine/DAE/Language.D4/MetaData.cs:277`) assigns `_tags[index] = tag.Inherit()` when a tag of that name is already there, so the inherited value replaces it. The summary and a new `<remarks>` now say that, and say that a caller whose own values must survive merges them back afterwards. `Tags.Inherit` itself is untouched, as the ticket required.

**The goldens.** `tests/goldens/frontend/Security/`, 16 changed lines in 14 files, regenerated and read in full. Nothing else under `tests/goldens/` moved. The diff is exactly the 14 the ticket predicted, with no surprises.

**Documentation.** One sentence appended to the "Metadata and class definitions" paragraph of `docs/architecture/catalog-and-store.md` (the prereq's subsection was there, reworded by its review; the sentence fits it as written). `python tools/docs-size-check/check_size.py` → 13 files checked, 0 over the limit. One `done` row at the end of the `## D4, catalog and libraries` section of `docs/migration-notes.md`.

**No Known-defects row in the plan.** Pre-existing, non-port engine defect fixed in passing — the category the review of `bug-remembered-statement-applies-a-changed-setting` settled as migration-notes-only; nothing in the plan's WS2 text refers to this slug. Stated here so it is not re-litigated.

## The per-surface justification (the owner's commit-message requirement)

The runner writes the commit subject itself, so the per-surface text the owner asked for lives here and in the migration-notes row:

- **`Security/SetUserDeviceUserPassword.*.xml`, eight page types, nine lines** (the `Browse` and `List` files twice each: the interface title and a grid hint; `Report` changes its report-name text area instead of an interface title). `Security.SetUserDeviceUserPassword` declares `Frontend.Title = "Set Device User Password"` (`libraries/Security/Documents/Security.d4:264`) and was being titled with the `"Device User"` it inherits from `Security.UserDeviceUser` (`:182`). "Set Device User Password" is what the library asks for. This is the ticket's own symptom. The source is a plain table-variable reference, so this surface exercises the `InheritMetaData` branch.
- **`Security/Role.{Browse,List,Add,Edit,View,Delete}.xml`, one line each.** The "Role..." detail link now derives the `Browse` page instead of `List`. Not a second defect: `DocumentBuilder.BuildShowFormAction` (`libraries/Frontend/Source/Production/DocumentBuilder.cs:161`–`178`) takes that link's page type from the `Frontend.UseList` tag of the reference's **source table**, which is `Security.UserRole` — a second view losing its own tag, whose symptom shows up on `Security.Role`'s forms. Confirmed in the sources: `Security.UserRole` declares `Frontend.UseList = "false"` (`Security.d4:340`) over `System.UserRoles`, which declares `Frontend.UseList = "True"` (`src/engine/DAE/Schema/SystemCatalog.d4:1966`). With the fix the view's `"false"` wins and `Browse` is what the library asks for. `Security.Role` is `Roles { Name } adorn { … }`, not a table-variable reference, so this surface exercises the `MergeMetaData` branch. The `Report` and `OrderBrowse` pages build no such link, which is why it is six forms and not eight.

Both inheritance branches are therefore covered by the regenerated goldens, one surface each — which was the ticket's plan for verifying them.

## The test

One new fixture, one case:

| Test | What it verifies |
|---|---|
| `tests/DAE.Server.Tests/ViewTagPrecedenceTest.TestViewTagsWinOverTheTagsInheritedFromItsSource` | The rule itself: a table with `Frontend.Title = "Source Title"` and `Frontend.UseList = "true"`, a view over it declaring both names with different values, and the view reads back as its own `"View Title"`/`"false"` — both on the live `Schema.DerivedTableVar` and in the create script the catalog emits for it. Two tags so the case also pins a tag whose value is read as a boolean, which is the one the six `Role` forms turn on. |

Confirmed red before and green after, as the ticket asked. With the one-line fix reverted the case fails on the first assertion with `Expected: "View Title" / But was: "Source Title"` — the ticket's symptom exactly; with the fix it passes in 19–41 ms. Nothing else was added and no test was cut. The fixture's summary states the rule and points at `FrontendDerivationGoldenTest` as the end-to-end cover, so a later reader does not take it for a duplicate of the goldens.

It derives from `DeviceClassAttributeFixture` for `CatalogObject<T>` and `Script` alone, which makes it the **third** fixture to do that. That fixture's own remark says moving those two helpers down to `InProcessTestFixture` is worth doing "when a fourth fixture needs them" — not tripped, and deliberately left alone.

## Validation run (targeted, per the project's per-stage rule)

`dotnet build Dataphor.slnx` — **0 errors**, no new warning code (the warnings are the pre-existing `CA1416`/`CS0618`/`CS0108` set). Then, each as its own `dotnet test tests/DAE.Server.Tests --no-build` run:

| Filter | Result |
|---|---|
| `Name=TestViewTagsWinOverTheTagsInheritedFromItsSource` | 1 passed (and 1 failed with the fix reverted) |
| `Name=EveryDerivedFormMatchesItsGolden` | 14 differences before regeneration, then `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` wrote 416 golden files and deleted 0 (an update run always fails by design) |
| `Name=TestViewTagsWinOverTheTagsInheritedFromItsSource` or `FullyQualifiedName~` each of `FrontendDerivationGoldenTest`, `FormCustomizationTest`, `FormDocumentCorpusTest`, `D4EmitterRoundTripTest`, `ParserRoundTripCorpusTest` | 534 tests: 515 passed, 19 skipped (the parked corpus documents), 0 failed |
| `FullyQualifiedName~ApplicationTransactionTest` plus `Name=` each of the coverage scripts `Views`, `References`, `Tables`, `SessionTables`, `SessionAndProcessScopedTables`, `ApplicationTransactions`, `SecurityLibrary` | 12 passed |
| `FullyQualifiedName~` each of `StoreRebuildWorkflowTest`, `CreateStatementTagRerunTest`, `SchemaCacheInvalidationTest`, `DeviceClassAttributeRerunTest` | 16 passed |

(The filters were single `--filter` expressions with the clauses joined by `|`; they are spelled out in prose here because a table cell cannot carry that character.)

`python tools/docs-size-check/check_size.py` — 13 files checked, 0 over the limit.

No pre-existing failure surfaced, so `tickets/.pre-existing-error.md` was not written.

**The three suite parts were not run.** This is a compiler change, which is one of the project rule's triggers for them — but the trigger is a lexer/parser/compiler change "that every script goes through", and this is one line on the create-view path only. The coverage scripts that do go through it (`Views`, `References`, `Tables`, `SessionTables`, `SessionAndProcessScopedTables`, `ApplicationTransactions`, `SecurityLibrary`) plus the whole frontend, parser-round-trip and emitter-round-trip corpora are in the run above. If you disagree with that reading, the three parts are the thing to add.

## What to re-check, and what is reasoned rather than measured

Honest gaps, for a reviewer treating this as a starting point:

- **`Frontend.Elaborate` is argued, not exercised.** Where a view and its source both declare it with different values, the view's value now wins and a form's whole reference structure can change — the largest shape of change an application will see. No such pair exists in the shipped libraries, which is why the diff is 16 lines and not hundreds, so **nothing in the suite covers it**. It is named in the migration-notes row. Constructing one would mean inventing a library, which the ticket did not ask for; if you want it covered, a `Frontend.Elaborate` pair added to the test libraries' corpus is the cheap way.
- **The session-view and generated-view path is confirmed by reading plus coverage scripts, not by a test of its own.** Both reads of `localStatement.MetaData` that matter (`DAE.GlobalObjectName` at `:4405`, `DAE.SourceTableName` at `:4413`) are above the copy, so name resolution cannot see it; `SessionTables`, `SessionAndProcessScopedTables`, `ApplicationTransactions` and `ApplicationTransactionTest` are green. No assertion pins a session view's tags.
- **`IsInherited` is not asserted.** A tag the view declares itself is now a plain tag and one only the source has is inherited. Nothing in the engine branches on that for a view's own tags, and `D4TextEmitter` prints both alike — which is *why* an affected view's stored create script changes, and the new test's script assertions are what make that observable. Worth a second opinion on whether the distinction deserves pinning.
- **The custom key-violation message** (`CompileCreateTableVarStatement` at `:4459`, building `DAE.Message` from `Frontend.Title` at `:3887`) runs *before* the inherit and merge, where the view's metadata is its own tags only — unchanged by this ticket, and already "the view's own tags win". Verified by reading and by no message changes appearing in the golden diff.
- **One edit beyond the TODO.** The prereq's migration-notes row ended "`create view` is not among them: what a view does with its own tags is being settled separately" — a forward reference this ticket settles. Changed to "is settled in the row below". Revert it if you would rather not touch a landed row.
- **The device arm is untouched**, as the ticket required: `Compiler.cs:9381` and the clone at `DDLNodes.cs:1138` are not in the diff.

Nothing was parked as a tripwire — no concern here is of the "fine now, work later if X" shape.
