description: A new test file pins the behaviour every catalog store must have — cursors, ranges, nested-transaction undo and restart persistence — and runs it against the SQLite store, so a later change to a store is measured against one written description of it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/StoreContractTests.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tickets/plan/store-mssql-port.md
----
## What landed

`tests/DAE.Server.Tests/StoreContractTests.cs` (531 lines) — an abstract fixture holding 11 tests, with one concrete subclass per catalog store we build. `SQLiteStoreContractTests` runs all 11, in 1.1 s. The two direct-cursor tests and their two helpers were removed from `CatalogStoreTest.cs`, along with the four `using` lines that became dead with them.

The abstract base takes a store from its subclass through two hooks — `CreateStore(directory)` and `DisposeStore(store, directory)` — plus `ReopenStore()` for the one test that needs a second open on the same database. Each test gets its own directory and creates the small schema it needs (`T { K primary key, G, V }`, four rows in two groups), so nothing carries between tests. The catalog's own seed script is used by two tests only.

The implement stage is commit `ticket(implement): test-store-contract` (and the partial salvage commit before it, which carried the file's first 663 lines). Review changed three things in that work, listed below.

## Review findings

Read the implement diff first, then the handoff. The contract's claims were checked by reading `SQLStore`, `SQLStoreConnection`, `SQLStoreCursor`, `SQLiteStore` and `CatalogStore`, and by seven deliberate mutations of engine or test code, each built, run, and then reverted. The engine sources were confirmed byte-identical to HEAD after every mutation.

### Checked and sound

- **The prior run's live mutation really is gone.** The handoff says the salvage commit carried a cursor row key of `G` (a non-unique index column) instead of the primary key `K`, and that the final implement commit reverted it. Confirmed: the implement commit's diff is exactly that one-line revert, and the tree at HEAD had no engine-source mutation left.
- **The handoff's central mutation claim is accurate.** Re-applying the `K` → `G` row key fails exactly the four tests the handoff names — `RowReadThroughSameCursorAfterUpdate`, `CursorMovesFromUpdatedRow`, `CursorMovesFromDeletedRow`, `NestedRollbackReinsertsCursorUpdates` — and no others. Worth re-running rather than trusting, because four of the handoff's seven "verified" rows rest on it.
- **No test in the file is vacuous.** The handoff flagged five tests as green but never shown to fail, and asked a reviewer to push on them. All five now have mutation evidence. Dropping the cursor range's start bound in `SQLStoreCursor.SetRange` fails `CursorsInSequenceWithinOneTransaction`, `RowInsertedThroughOneCursorReadByAnother` and `TwoCursorsOpenOnOneTableInOneTransaction`; pointing `ReopenStore()` at a different database file fails `SeedSchemaReopens`; the eleventh is covered under the first fix below. The handoff's two named suspects (`CursorsInSequenceWithinOneTransaction`, `TwoCursorsOpenOnOneTableInOneTransaction`) were not passing for the wrong reason.
- **`CatalogStoreTest.cs` after the removals.** The four `using` lines dropped were exactly the dead ones; the remaining five are all still reached (`Path`, `System.Data.SQLite`, `Task`, `Directory`/`Stream`, `List<>`), and the `RunWithin` helper still has callers.
- **Source hygiene.** CRLF, the UTF-8 byte-order mark and tab indentation preserved in the `.cs` file; LF and no mark in the ticket files. No file reformatted or re-encoded.

### Fixed in this pass

- **A test claimed a behaviour it cannot detect.** `OuterRollbackDoesNotReplayUndoLog` was named and documented for "the undo log is cleared at the outermost end and the database's own rollback does the work". Mutating `SQLStoreConnection.RollbackTransaction` to replay the log at the outermost level instead of clearing it left all 11 tests green — the database rollback that follows erases whatever the replay did, so the final state is identical either way, and no test through the store's public surface can separate them. The test does have teeth: it is the only one of the 11 that fails when the outermost begin is not a real database transaction, and the only one that fails when a nested commit commits to the database (both measured by mutation). Renamed to `OuterRollbackUndoesCommittedNestedLevel`, doc corrected to those two measured properties, and a `NOTE:` added saying why the log-clearing half is untestable from there and pointing at the method that explains the real reason for clearing — so the next reviewer does not re-derive it.
- **A comment stated something untrue of the code.** The `NOTE:` on `ConnectCatalogStore()` said building a `CatalogStore` without a started engine "leaves the inner store's SupportsMARS and SupportsUpdatableCursor at false, so ... that is the path a server takes too". The updatable-cursor half is right; the MARS half is not — `SQLiteStore.InternalInitialize` sets `_supportsMARS = true`, so a server's store has it true where this fixture's inner store has it false. It is harmless, because MARS gates only the cursor cache in `CatalogStoreConnection` and the statement path never opens a cursor through it, but the comment asserted sameness instead of saying why the difference does not matter. Rewritten to name all three differences the skipped `InternalInitialize` causes — including the third, unmentioned one: the inner store's connection string is never normalized, so it carries no forced write-ahead-log journal mode and no busy timeout — and to say why each costs nothing here.
- **138 lines of never-compiled code removed.** `MSSQLStoreContractTests` sat behind `#if HAVE_MSSQL_STORE`, which nothing defines, in a file whose stated job is the behaviour of the stores we build. The handoff flagged it as the reviewer's call and argued both sides. Deleted: no compiler and no test would ever report it going stale, and `store-mssql-port` — a plan ticket whose output decides whether that store is kept at all — would have had to debug it rather than inherit it working. Its wiring knowledge was the real asset, so it moved to `tickets/plan/store-mssql-port.md` as a section answering that ticket's own research item 5: one database per fixture under a fixed name, `set single_user with rollback immediate` before the drop, the master-versus-database connection string swap, `ShouldEnsureDatabase = false`, and why `Assert.Ignore` beats a compile switch for the environment-variable gate. Three now-stale references to the deleted class in that ticket were corrected at the same time, and the `using System.Data.Common` that only the deleted block needed was dropped. The file went 663 → 531 lines, all of it now compiled.

### Checked and deliberately left alone

- **`CursorMovesFromDeletedRow`'s `deleted == 1`**, which the handoff asked a reviewer to judge. It is tight enough. Traced the state: the two earlier `Delete()` calls remove rows 2 and 1, so the final range on group 1 admits only row 3, and the loop's `deleted == 1` is then pinned on both sides by the `count(*) == 1` and `Values(4) == "d"` assertions that follow — together those say exactly which three rows went and which one stayed. No change.
- **`SeedSchemaReopens` asserting on `DAEServerInfo` alone**, the handoff's other question. A second table would not distinguish any realistic failure: the SQLite store's database is one file, so the reopen either finds what was written or finds nothing, and the test already asserts both the table's presence and a value it wrote itself. Assertions here would be cost without coverage. No change.
- **No new tests written.** The 11 are the right set and all are now demonstrably able to fail; the project's testing rule asks for a characterization test per subsystem ported, which this file is. The umbrella's two still-uncovered cases (an `Update()` that moves a row out of the cursor's range; a cursor opened without its column list) stay uncovered and stay named — `store-parameterized-statements`, already in `implement/`, owns the first and lists it in its TODO.
- **No docs changed.** Checked `docs/modernization-plan.md`, `docs/architecture.md`, `docs/migration-notes.md` and `AGENTS.md` against the diff. The plan's WS3 row for `test-store-contract` ("L2 store contract and restart persistence; replaces `SQLStoreTest`") is still accurate, and its WS4 row for `store-mssql-port` already says the contract runs "where a server is configured". The plan annotates a work-item row with **Done** only when the outcome carries something its scope does not — `test-parser-roundtrip-corpus` landed without one — which is not the case here. Nothing in this change is visible to an application, a deployment script or a C# extension, so `docs/migration-notes.md` gets no entry. `AGENTS.md` documents the two fixtures with unusual run semantics (the parser corpus and its allowlist, the coverage scripts and their manifest); an ordinary NUnit fixture does not belong in that list.

### Tripwires: none added, two inherited and still valid

Nothing new to park. The implement stage's two `NOTE:` tripwires in `StoreContractTests.cs` were read and both still hold: the cursor-delete path note on `CursorMovesFromDeletedRow`, and the one on `ConnectCatalogStore()` about a store that reported updatable cursors needing a running server — the latter survives inside the comment rewritten above. The new `NOTE:` on `OuterRollbackUndoesCommittedNestedLevel` is not a tripwire but a dead end marked so nobody walks it twice.

### Tickets filed: none, with the reason

Every finding was either fixed in this pass or a judgement that the code is already right, so nothing reached the filing bar: there is no class-level invariant to name here, and no current-release anchor a point ticket would serve. The deleted SQL Server subclass did not become a ticket either — `store-mssql-port` already exists, already lists this file, and now carries the notes.

## Verification run

Build and the three suite parts, each in the foreground, after the fixes:

- `dotnet build Dataphor.slnx --no-incremental` — 0 errors, 143 warnings, the same count as HEAD.
- Part 1 (`class != CoverageScripts`) — 366 passed, 0 failed in `DAE.Server.Tests`; 13 passed, 0 failed in `Common.Tests`.
- Part 2 (coverage scripts `[A-Ra-r]`) — 67 passed, 0 failed, 11 skipped.
- Part 3 (coverage scripts `[^A-Ra-r]`) — 48 passed, 0 failed, 14 skipped.
- `SQLiteStoreContractTests` — all 11 green, under that fixture name.

Every count matches the implement handoff's, so the fixes changed no test's outcome and removed no compiled test. Skipped counts in parts 2 and 3 are the pre-existing explicitly-ignored coverage scripts. No pre-existing failures surfaced, so no `tickets/.pre-existing-error.md` was written.

## What the next ticket on this file should know

- The contract runs against **one** store. Nothing in it has ever executed against SQL Server, and there is no subclass to inherit — `store-mssql-port` writes one from the notes now in that ticket.
- `ConnectCatalogStore()` builds a `CatalogStore` with `CreateStore()` alone because `CatalogStore.Initialize` needs a started engine, and `CatalogStore` exposes no way to initialize the inner store on its own. If a test ever needs a store initialized the way a server initializes it, that is the gap to close.
- The shared table declares its key `int`, not `integer`: `System.Data.SQLite` maps `integer` to `Int64` and `int` to `Int32`, the catalog's own tables are `int`, and `SQLStoreCursor` compares a read key against a range value through `IComparable`, which throws on mismatched integer types. This is documented at the site. The tests moved down from `CatalogStoreTest` therefore compare against `1` where the originals compared against `1L`.
