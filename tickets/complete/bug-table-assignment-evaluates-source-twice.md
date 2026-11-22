description: A script that filled a table in one step from a calculation whose answer can change used to run that calculation twice and keep the second set of answers. It now runs it once, and the review pass confirmed the fix, narrowed the claim about which kinds of calculation were affected, and added a second case to the test.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/TableValue.cs, tests/DAE.Server.Tests/TableAssignmentEvaluationTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
----
## What landed

A table assignment (`MyTable := <expression>`) now evaluates its source expression once.

**The fix** - `src/engine/DAE/Runtime.Data/TableValue.cs`, in the `Table.CopyNativeAs` override (the live-cursor one, around line 800; distinct from the native `TableValue.CopyNativeAs` earlier in the same file). The `else` branch of its open-or-reset decision is guarded on the cursor not being at the beginning:

```csharp
			if (!Active)
				Open();
			else if (!BOF())
				Reset();
```

`Table.Reset()` defaults to `InternalReset()`, which is `Close(); Open();` - a re-execution of the cursor's whole source pipeline. A table assignment materializes its source through `TableToTableValueNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/TableNodes.cs`), which executes the source node, opens the resulting cursor and copies it here; the cursor is at the beginning at that moment, so the old unconditional `Reset()` re-ran the source and the rows copied were the second pass's. For a deterministic source that is invisible; for one calling `GetNextGenerator`, `Random`, `Now` or any operator tagged `DAE.IsDeterministic = 'false'` the stored values were ones the script never computed.

**The test** - `tests/DAE.Server.Tests/TableAssignmentEvaluationTest.cs`, one `[TestFixture]` on `InProcessTestFixture` with one `[Test]`, `AssignmentEvaluatesANonDeterministicSourceOnce`, registered in the `<Compile Include>` group of `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` (that project lists its sources and does not glob, so an unregistered file compiles into nothing and its test silently does not exist). It needs no library and no device: `System.Temp` reproduces it. It creates a one-row `Counter` table and a non-deterministic `Bump()` operator that increments and returns the counter, then assigns the same two-row source to two tables - once through a restriction and once through a `redefine` - and for each asserts the counter reads 2 (how many times the source ran) and that the stored ids are 1 and 2 (which run was kept). Both halves are asserted on purpose: the count alone would pass an engine that ran the source once but stored the wrong pass, and the ids alone would pass one whose extra pass ran before the stored one.

**The migration note** - two rows in `docs/migration-notes.md`, both status `done`: one in `## D4, catalog and libraries` for the behaviour an application can see, one in `## Custom C# extensions` for `Table.CopyNativeAs` no longer resetting a cursor that is still at the beginning.

**A note on the `Reset()` contract** - a `NOTE:` comment on `Table.InternalReset` saying that a reset means two different things depending on the class (the default re-executes the source; a class over an already-materialized table overrides it with a cheap rewind), which is the ambiguity this defect came out of.

## Review findings

The implement-stage commit is `ticket(implement): bug-table-assignment-evaluates-source-twice`. The diff was read before the handoff summary.

### Confirmed by re-doing the work

- **The reproduction is real.** Took the guard back out, rebuilt the solution and ran the case: all three assertions failed, the counter reading 4 and the stored ids 3 and 4. Restored, and it passes. So the readings are 2/1/2 with the guard against 4/3/4 without.
- **`BOF()` is a safe predicate.** All 28 `InternalBOF` implementations in the tree were read (`src/engine/DAE/Runtime.Data/*.cs`, `src/engine/Server/Device.Catalog/CatalogDeviceTable.cs`, `libraries/SQLDevice/Source/SQLDevice.cs`). Every one is either a plain read of a flag or a delegation to a source cursor; none throws, and none fetches as a side effect, so the guard cannot break a class by asking it a question it cannot answer. Every one of them also reports *true* immediately after open: the flag-based classes set it in `InternalOpen`, or in the `InternalFirst` that `InternalOpen` calls, and the scan-based ones get it from `Scan.First()`, which sets its own flag. So the guard does not skip a reset that a cursor positioned off the beginning needs, and it does not fail to skip one for a freshly opened cursor.
- **The two call sites.** The fix ticket's grep was re-run rather than taken as given. `Table.CopyNativeAs` is reached only from `TableToTableValueNode.InternalExecute` (through `CopyNative()`) and from `RemoteServerPlan.Evaluate` in `src/engine/Server/Server/RemoteServerPlan.cs` (through `DataValue.CopyAs`). Every other `.Copy()`, `.CopyNative()` and `.CopyAs(` call site in `src/` and `libraries/` is on a scalar or a row - the remaining candidates were checked one by one (`AggregateNodes.cs`, `SystemNodes.cs`, `DataVar.cs`, `ListValue.cs`, `RowValue.cs`, `ApplicationTransaction.cs`, `ServerTransaction.cs`).
- **Nothing else in the engine resets a cursor in order to read it from the start.** Every other `.Reset()` call is inside a table class's own navigation or its `InternalReset` - a nested-loop join rewinding its inner cursor per outer row, a difference or union table rewinding a side - where re-execution is what a reset is for.
- **Build and the whole suite.** `dotnet build Dataphor.slnx`: 0 errors. The three part commands from the root `AGENTS.md`, each as its own foreground command: part 1 13 passed (Common.Tests) and 577 passed / 0 failed (DAE.Server.Tests) in 6 m 5 s; part 2 67 passed / 11 skipped / 0 failed in 3 m 6 s; part 3 50 passed / 12 skipped / 0 failed in 3 m 8 s. The counts match the implement stage's. Part 1 was run again after the last edit, which was comment text only, and gave the same result. No pre-existing failures surfaced, so nothing was written to `tickets/.pre-existing-error.md`.
- **Nothing else owed.** No entry in `tests/DAE.Server.Tests/coverage-manifest.json` names this slug, so there is no `known-failure` to flip to `pass`. `SamplesCorpusTest`'s `Sample.Rental` entry is about that sample's zero-byte `Register.d4` and is unrelated to this defect. `docs/migration-notes.md` is the only document in `docs/` that mentions this behaviour at all; `docs/architecture.md` has no section on the cursor contract to amend. Line endings, indentation and byte-order marks of both edited files match what they had.

### Found and fixed in this pass

- **The claim about which expressions were affected was too broad, in three places.** The ticket, the test's documentation and the D4 row of the migration note all said that any source other than a bare `table { row …, row … }` literal doubled - "a `where`" flatly. Measured, with the guard removed and the test's condition swapped: `where V > 0` doubles (4/3/4), and `where V + 0 > 0` does **not** - the case passes. The reason is which restriction class the compiler picks. A condition comparing a column with a value becomes a `ScanTable`, whose `InternalReset` is `Close(); Open();`; one the compiler cannot turn into an ordered scan becomes a `FilterTable`, whose `InternalReset` rewinds its source instead. The migration note now names which shapes re-ran and which rewound, with an example of each, and the test's documentation says the same. This matters because that note is what tells an application owner how much of their own code to re-check.
- **`redefine` reaches the defect by a different route than the handoff said, and is now covered.** The handoff said the conversion `redefine` reaches the same copy "by the same route" as the restriction, so one test shape stood for both. It does not: the compiler rewrites `redefine` into an add, a remove and a rename (`RedefineNode` itself is unreachable, which `tickets/blocked/debt-unreachable-redefine-nodes.md` documents with coverage data), so the reset delegates `RenameTable` → `ProjectTable` → `ExtendTable`, and `ExtendTable` is the one that inherits the default `Close(); Open();`. Measured independently: that shape doubles without the guard (4/3/4) and is fixed with it. Since it exercises a different reset topology, and since it is the shape the real-world `Sample.Rental` failure took, it is now a second arm of the same test. The three assertions moved into one `AssertSourceEvaluatedOnce` helper so there is a single copy of them, and both arms sit inside one `Assert.Multiple` so a regression in one shape is not hidden by the other's failure.
- **A `NOTE:` on `Table.InternalReset`.** The root of this defect is that `Reset()` means "re-execute" on a class that inherits the default and "rewind" on one that overrides it, and the next caller who resets a cursor merely to read it from the start will walk into it again. One comment at that site records the two meanings and points at the guard. The first draft of it ended "Rewind with First()", which was wrong and was removed: `Table.InternalFirst` throws `NotBackwardsNavigable` by default and `First()` checks the `BackwardsNavigable` capability, so a forward-only cursor has no general rewind.

### Found and deliberately left

- **A fixture-level `Evaluate` helper is now duplicated four times.** `private object Evaluate(string)` over the fixture's own process appears in `SchemaCacheInvalidationTest`, `ServerLinkConnectionTest`, `ServerLinkUserRollbackTest` and this test. Pulling it onto `InProcessTestFixture` would remove copies, but three of the four predate this ticket and `SchemaCacheInvalidationTest`'s copy deliberately evaluates on a *separate* session (the answer it reads is cached across sessions, and its comment says so), so it could not be unified with the rest. Left alone rather than widening this change into three unrelated test files for a three-line helper.
- **`TableToTableValueNode.InternalExecute` opens a cursor its source node has already opened**, which makes the `if (!Active) Open();` branch of `Table.CopyNativeAs` dead from that path. Harmless, and the implement stage already measured that removing it fixes nothing; not touched.

### Empty categories, and why

- **No tickets filed.** Nothing found clears the filing bar. The two real findings were documentation accuracy and a missing test arm, both fixed here; the one architectural question the investigation surfaced - that `redefine` has an unreachable node implementation alongside the live rewrite - is already owned by `debt-unreachable-redefine-nodes` in `blocked/`, and this defect runs entirely through the live rewrite path that ticket does not propose to change, so there was no arm to append to it.
- **No tripwires beyond the one above.** The `Reset()` ambiguity is the only concern that is fine today and becomes work only if something changes later, and it is parked at its site.
- **No accepted-tradeoff `NOTE:` was found at any site touched**, so nothing was re-filed against a decision a human had already made.

### The behaviour difference the guard introduces

A caller that opens a cursor, leaves it at the beginning while the underlying data changes, and *then* copies it now gets the data as of the open rather than as of the copy. A cursor that has been moved off the beginning still gets its `Reset()`, so a mid-scan copy is unchanged. Neither of the two call sites can hit this - in both, the cursor has just been executed - and the `## Custom C# extensions` row of the migration note states it for code outside this tree that copies a cursor of its own.

### The alternative design, recorded and not taken

Copy from the cursor's current position at the one call site instead of guarding the general contract: split the row-copying loop out of `Table.CopyNativeAs` and have `TableToTableValueNode` call it directly. Narrower in behaviour terms, but it adds a public member to `Table`, which a custom device's table class derives from, and `TableToTableValueNode` holds an `ITable`, so it needs either a member on `ITable` (breaking any outside implementation of that interface) or a cast - and it carries a larger migration note. The guard held under review, so this stays unbuilt; the fix ticket carried the full reasoning if anyone wants to reopen the choice.

Nothing in `samples/` was touched, and no expected value in any existing D4 test script was edited.
