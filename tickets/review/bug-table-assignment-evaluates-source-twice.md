description: A script that filled a table in one step from a calculation whose answer can change used to run that calculation twice and keep the second set of answers. It now runs it once; the fix, its test and its migration note are in, and this ticket is the review pass over them.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/TableValue.cs, tests/DAE.Server.Tests/TableAssignmentEvaluationTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
----
## What landed

Four files, one behaviour change.

**The fix** - `src/engine/DAE/Runtime.Data/TableValue.cs`, in the `Table.CopyNativeAs` override (around line 800). The `else` branch of its open-or-reset decision is now guarded on the cursor not being at the beginning:

```csharp
			if (!Active)
				Open();
			else if (!BOF())
				Reset();
```

with a four-line comment above it saying why. `Table.Reset()` defaults to `InternalReset()`, which is `Close(); Open();` - a re-execution of the cursor's whole source pipeline. A table assignment (`MyTable := <expression>`) materializes its source through `TableToTableValueNode.InternalExecute`, which executes the source node, opens the resulting cursor and copies it here; every derived table node (`RestrictNode` and friends) returns an **already open** cursor and inherits the default `InternalReset`, so the old `else Reset()` re-ran the whole source and the rows copied were the second pass's. `TableSelectorNode` also returns an open cursor, but its `LocalTable` overrides `InternalReset` with a cheap rewind of the native scan, which is why a bare `table { row ..., row ... }` literal never doubled.

**The test** - a new `tests/DAE.Server.Tests/TableAssignmentEvaluationTest.cs`, one `[TestFixture]` deriving from `InProcessTestFixture` with one `[Test]`, `AssignmentEvaluatesANonDeterministicSourceOnce`. It needs no library and no device: `System.Temp` reproduces it. Registered in the `<Compile Include>` item group of `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` (that project lists its sources explicitly and does not glob, so an unregistered file is silently not compiled and its test silently does not exist).

**The migration note** - two rows in `docs/migration-notes.md`, both status `done`: one in `## D4, catalog and libraries` for the D4-visible change, one in `## Custom C# extensions` for `Table.CopyNativeAs` no longer resetting a cursor that is still at the beginning.

## What the test verifies, and that it is a real reproduction

| Test | What it verifies |
|---|---|
| `TableAssignmentEvaluationTest.AssignmentEvaluatesANonDeterministicSourceOnce` | The source expression of a table assignment is evaluated exactly once, and the rows stored are that evaluation's. It creates a one-row `Counter` table and a `Bump()` operator tagged `DAE.IsDeterministic = 'false'` that increments and returns the counter, runs `T := (table { row { Bump() ID, 1 V }, row { Bump() ID, 2 V } }) where V > 0;`, and asserts `Counter[1].N = 2` (how many times the source was evaluated), `Min(ID from T) = 1` and `Max(ID from T) = 2` (which evaluation was stored). |

Both halves are asserted on purpose: the count alone would pass an engine that evaluated once but stored the wrong pass, and the ids alone would pass one whose extra pass ran before the stored one rather than after.

**Confirmed to fail without the fix.** The guard was taken back out (`else if (!BOF())` back to a plain `else`), `DAE.csproj` and the test project rebuilt, and the case run: all three assertions failed, the counter reading 4 and `Max(ID from T)` 4 - the doubling the ticket describes. The guard was then restored and the full suite run on the restored build. So the readings are 2/1/2 with the guard against 4/3/4 without, which is exactly what the fix ticket measured.

One expression shape only, which is deliberate: the conversion `redefine` the compiler inserts for a user-defined scalar column, and the deeper nestings, reach the same copy by the same route, so a test per row of the fix ticket's matrix would buy nothing. The Rental sample covers the `redefine` shape end to end and is verified by hand below rather than automated - `SamplesCorpusTest` deliberately records `Sample.Rental` as a sample that exposes no table variable after registration (its `Documents/Register.d4` is a zero-byte file by design).

## Verified by hand: Sample.Rental registers clean

A throwaway fixture carrying `Frontend`, `SimpleDevice`, `SQLDevice`, `Connection`, `System.Internet` and `Sample.Rental`, registering `Sample.Rental`, then `SetLibrary('Sample.Rental');` and `ExecuteScript('Sample.Rental', 'RegisterCompleted');`. Every step ran with no error and left:

- `Movie`: 10 rows, `Min(ID)` 1, `Max(ID)` 10.
- `Actor`: 12 rows, `Min(ID)` 1, `Max(ID)` 12.

Without the fix the same run fails at step 7 with `The table Movie does not have a row with ID (1).` - measured when the bug was diagnosed, not re-measured here. The fixture and its `<Compile Include>` line were deleted afterwards; `git status` shows only the four files listed above.

## Build and test

`dotnet build Dataphor.slnx`: 0 errors. Then the three part commands from the root `AGENTS.md`, each as its own foreground command:

| Part | Result | Time |
|---|---|---|
| 1 (everything but the coverage scripts) | 13 passed / 0 failed (Common.Tests), 577 passed / 0 skipped / 0 failed (DAE.Server.Tests) | 6 m 4 s |
| 2 (coverage scripts A-R) | 67 passed / 11 skipped / 0 failed | 3 m 6 s |
| 3 (coverage scripts, the rest) | 50 passed / 12 skipped / 0 failed | 3 m 7 s |

No failures anywhere, and the counts match the fix ticket's baseline: part 1's 576 has become 577, the new case. Logs are in `tickets/.logs/bug-table-assignment-evaluates-source-twice.impl.part1.log` and its `part2` and `part3` siblings, for as long as that directory's pruner keeps them.

## Where to push on this

**The guard's one behaviour difference, which is in the migration note.** A caller that opens a cursor, leaves it at the beginning while the underlying data changes, and *then* copies it now gets the data as of the open rather than as of the copy. A cursor that has been moved off the beginning still gets its `Reset()`, so a mid-scan copy is unchanged. The fix ticket established by grep that `Table.CopyNativeAs` (the live-cursor override at line ~800, distinct from the native `TableValue.CopyNativeAs` at line 147 of the same file) is reached from exactly two places in this repository - `TableToTableValueNode.InternalExecute`, and `RemoteServerPlan.Evaluate` in `src/engine/Server/Server/RemoteServerPlan.cs` through `DataValue.CopyAs` - and that in both the cursor has just been executed and sits at the beginning. That grep was taken as given here and not re-run; re-running it is a reasonable review step, and so is deciding whether the migration-note wording is strong enough for a custom device whose table class is copied by code outside this tree.

**`BOF()` on a cursor that has not been read.** `InternalBOF` is abstract on `Table`; the fix ticket counted 29 overrides in the tree and reported that none of them throws. Not re-verified here beyond the suite passing - though the suite exercises the assignment path through every coverage script, so an `InternalBOF` that threw or answered wrongly on a class reaching this site would have shown up. A class that never reaches `CopyNativeAs` would not.

**The alternative design, recorded and not taken.** Copy from the cursor's current position at the one call site instead of guarding the general contract: split the row-copying loop out of `Table.CopyNativeAs` and have `TableToTableValueNode` call it directly. Narrower in behaviour terms, but it adds a public member to `Table`, which a custom device's table class derives from, and `TableToTableValueNode` holds an `ITable`, so it needs either a member on `ITable` (breaking any outside implementation of the interface) or a cast - and it carries a larger migration note. The guard held, so this stays unbuilt; the fix ticket carried the full reasoning if a reviewer wants to reopen the choice.

**Not a gap, but worth knowing.** Removing the redundant `table.Open()` from `TableToTableValueNode.InternalExecute` does *not* fix this - the derived node has already opened its own table, so the copy still took the `Reset()` branch. That was built and measured during the fix stage and the counts were unchanged. Don't spend review time there.

Nothing in `samples/Sample.Rental/Documents/` was touched, and no expected value in any existing D4 test script was edited.
