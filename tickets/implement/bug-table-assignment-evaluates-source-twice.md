description: When a script fills a table in one step from a calculation whose answer can change, the database engine runs that calculation twice and keeps the second set of answers, so the table ends up holding different values from the ones the script computed first - silently, with no error. The cause is now found and a one-line correction measured; this ticket lands it with its test.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/TableValue.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
repro: verified
----
## What is wrong

In D4, `MyTable := <expression>` replaces a table variable's contents with the rows of an expression. The engine evaluates that expression **twice** whenever it is anything other than a bare `table { row …, row … }` literal, and the rows it stores are the second evaluation's. When the expression is *non-deterministic* - it calls `GetNextGenerator`, `Random`, `Now`, or any operator tagged `DAE.IsDeterministic = "false"` - the two evaluations produce different values, the first set is computed and thrown away, and the table ends up holding values the script never asked for. Nothing is reported.

The same expression written as `insert <expression> into MyTable` is evaluated once, which is the behaviour the assignment should have.

## The cause

One site: the `Table.CopyNativeAs` override in `src/engine/DAE/Runtime.Data/TableValue.cs` (line 787, in `public abstract class Table : DataValue, ITable` at line 202). It begins

```csharp
if (!Active)
    Open();
else
    Reset();
```

`Table.Reset()` (same file, around line 336) defaults to `InternalReset()`, which is `Close(); Open();` - a full re-execution of the cursor's whole source pipeline. Classes with a cheap rewind override `InternalReset`; derived table classes (a restriction, a redefine) do not.

The assignment path reaches it like this. `Compiler.EmitTableAssignmentNode` materializes the source expression into a temporary variable through `EnsureTableValueNode`, which wraps the source `TableNode` in a `TableToTableValueNode`. That node's `InternalExecute` (`src/engine/DAE/Runtime.Instructions/TableNodes.cs`, line 1321) executes the source, calls `Open()` on the resulting cursor, and copies it with `CopyNative()`.

So whether the source is evaluated once or twice turns entirely on whether the cursor is already `Active` when `CopyNativeAs` runs:

- `TableSelectorNode.InternalExecute` (TableNodes.cs line 1231) returns an **open** `LocalTable` whose rows are already materialized in a native table. It is `Active`, so `CopyNativeAs` calls `Reset()` - but `LocalTable` overrides `InternalReset` with a cheap rewind of the native scan, so nothing is re-evaluated. Hence a bare table literal: **once**.
- `RestrictNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/RestrictNode.cs`, line 853) also returns an **open** table, and so do the other derived table nodes, but they inherit the default `InternalReset`. `CopyNativeAs` sees `Active`, calls `Reset()`, and the whole pipeline underneath - including the table selector that calls the generator - runs a second time. The rows copied are the second pass's. Hence `where`, `redefine`, and the conversion `redefine` the compiler inserts for a user-defined scalar column: **doubles**.

That also accounts for two things the fix ticket's matrix had isolated but not explained. The extra pass is exactly one however deeply the operators nest, because one `Close(); Open();` at the top re-opens the whole tree once. And `insert <expression> into <table>` never doubles because it scans the source cursor directly and never goes through `CopyNativeAs`.

**Ruled out by measurement.** Removing the redundant `table.Open()` from `TableToTableValueNode.InternalExecute` does **not** fix it: the derived node has already opened its own table, so `CopyNativeAs` still takes the `Reset()` branch. That change was built and measured and the counts were unchanged. Do not spend time on it.

## The correction, measured

Guard the reset on the cursor not having been read yet - at the beginning of the cursor, a `Reset()` can only re-fetch rows nobody has consumed:

```csharp
			if (!Active)
				Open();
			else if (!BOF())
				Reset();
```

`BOF()` is safe to call on every cursor: `InternalBOF` is abstract on `Table`, 29 classes in the tree override it and none of them throws.

This was built and measured on 2026-11-22 (Debug, development machine):

- The counting probe below went from four generator calls per two-row assignment to two, for `where`, for `redefine`, and for nested `where`, with the bare selector and the `insert … into` form unchanged at two.
- `samples/Sample.Rental` registers clean. `SetLibrary('Sample.Rental')` then `ExecuteScript('Sample.Rental', 'RegisterCompleted')` ran all 18 steps with no error and left `Movie` holding ids 1 to 10 (ten rows) and `Actor` 1 to 12. Without the change the same run fails at step 7 with `The table Movie does not have a row with ID (1).`
- The whole test suite passes: the three part commands from the root `AGENTS.md` gave 13 + 576 passed / 0 failed, 67 passed / 11 skipped / 0 failed, and 50 passed / 12 skipped / 0 failed, in 6 m 7 s, 3 m 7 s and 3 m 11 s. Logs are in `tickets/.logs/bug-table-assignment-evaluates-source-twice.part1.log` and its `part2` and `part3` siblings, for as long as that directory's pruner keeps them.

**Blast radius.** `Table.CopyNativeAs` is the live-cursor override, distinct from the native `TableValue.CopyNativeAs` at line 147 of the same file. In this repository it is reached from exactly two places: `TableToTableValueNode.InternalExecute`, and `RemoteServerPlan.Evaluate` (`src/engine/Server/Server/RemoteServerPlan.cs` line 164) through `DataValue.CopyAs`, which converts the result of a remote expression evaluation when its type differs from the plan's. In both the cursor was just executed and sits at the beginning, so the guard removes a re-execution and changes no result. Nothing else in `src/`, `libraries/` or `tools/` calls `Copy`, `CopyAs`, `CopyNative` or `CopyNativeAs` on an `ITable`.

The one behaviour difference to be honest about: a caller that opens a cursor, leaves it at the beginning while the underlying data changes, and *then* copies it now gets the data as of the open rather than as of the copy. A cursor that has been moved off the beginning still gets its `Reset()`, so a mid-scan copy is unchanged. That difference belongs in the migration note; no caller in the tree is in that position.

**The alternative, if the guard turns out not to hold.** Copy from the cursor's current position at the one call site instead: split the row-copying loop of `Table.CopyNativeAs` into a method of its own, leave `CopyNativeAs` calling it after its existing `Open`/`Reset` decision, and have `TableToTableValueNode` call that method directly. It is the narrowest change in behaviour terms - the general `CopyNativeAs` contract is untouched - but it adds a public member to `Table`, which a custom device's table class derives from, and `TableToTableValueNode` holds an `ITable`, so it needs either a member on `ITable` (breaking any outside implementation of the interface) or a cast. Prefer the guard; this is the fallback, and it carries a larger migration note.

## The test

One case, for the general property: the source expression of a table assignment is evaluated exactly once whatever its shape. Lowest layer that shows it, which here is a D4 script on the in-process engine harness, in a new `[TestFixture]` deriving from `InProcessTestFixture` (`tests/DAE.Server.Tests/InProcessTestFixture.cs`; `QuotaDeleteCostTest.cs` is the closest model). No library and no device are needed - the memory device `System.Temp` reproduces it, which the probe below established, so the fixture needs neither `Libraries` nor `LibrariesToRegister`.

**`tests/DAE.Server.Tests/DAE.Server.Tests.csproj` lists its source files explicitly and does not glob.** A new test file must be added to the `<Compile Include>` item group or it is silently not compiled and the test silently does not exist.

Assert **both** the number of evaluations and the stored values. The ids alone would pass if the extra pass happened before the stored one rather than after, and the count alone would pass if the engine evaluated once but stored the wrong pass. This is the probe that was used, with its measured readings:

```csharp
ExecuteScript(@"
create table Counter in System.Temp { ID : Integer, N : Integer, key { ID } };
insert table { row { 1 ID, 0 N } } into Counter;
create operator Bump() : Integer
begin
	update Counter set { N := N + 1 } where ID = 1;
	result := Counter[1].N;
end tags { DAE.IsDeterministic = 'false', DAE.IsRepeatable = 'false' };
create table T in System.Temp { ID : Integer, V : Integer, key { ID } };
");
ExecuteScript("T := (table { row { Bump() ID, 1 V }, row { Bump() ID, 2 V } }) where V > 0;");
// Process.Evaluate("Counter[1].N", null).AsNative   - 2 with the fix, 4 without
// Process.Evaluate("Min(ID from T)", null).AsNative - 1 with the fix, 3 without
// Process.Evaluate("Max(ID from T)", null).AsNative - 2 with the fix, 4 without
```

Two notes from writing it. `Instance.Evaluate` runs on a session of its own; read the values back on the fixture's own process (`Process.Evaluate(expression, null).AsNative`) so that everything is one session. And the aggregate form is `Min(ID from T)`, not `Min(T over { ID }, ID)`.

The `where` shape is enough on its own: the conversion `redefine` the compiler inserts for a user-defined scalar column reaches `CopyNativeAs` by the same route, and the Rental run covers that shape end to end by hand. Do **not** turn the fix ticket's matrix into a test per row - it is evidence for the diagnosis, not a test plan - and do not add a second automated case for the Rental sample: `SamplesCorpusTest` deliberately records `Sample.Rental` as a sample that exposes no table variable after registration (its `Documents/Register.d4` is a zero-byte file by design), and nothing in WS3 waits on this.

Nothing in `samples/Sample.Rental/Documents/` is edited. Those scripts are the record of what correct behaviour looks like.

## TODO

- Apply the `BOF()` guard to `Table.CopyNativeAs` in `src/engine/DAE/Runtime.Data/TableValue.cs` (line 787). Preserve the file's tabs, CRLF endings and byte-order-mark state. Leave the existing comment about `Schema.ResultTableVar` and defect #33978 alone - it is about the inferred keys, not about this.
- Add a short comment at the guard saying why the reset is guarded: `Reset()` defaults to `Close(); Open();`, which re-executes the cursor's source, so resetting a cursor nobody has read from evaluates the source a second time and the copy keeps the second pass's values.
- Add the characterization test described above in a new file under `tests/DAE.Server.Tests/`, and add that file to the `<Compile Include>` item group of `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`.
- Verify by hand, and record the numbers in the handoff, that `samples/Sample.Rental` registers clean: a fixture carrying `Frontend`, `SimpleDevice`, `SQLDevice`, `Connection`, `System.Internet` and `Sample.Rental`, registering `Sample.Rental`, then `SetLibrary('Sample.Rental')` and `ExecuteScript('Sample.Rental', 'RegisterCompleted')`, leaves `Movie` with ten rows and ids 1 to 10 and `Actor` with ids 1 to 12. Delete the throwaway fixture afterwards, including its `<Compile Include>` line.
- Add a row to the `## D4, catalog and libraries` section of `docs/migration-notes.md`, status `done`: a table assignment whose source expression is not a bare table literal used to evaluate that expression twice and store the second pass's rows, and now evaluates it once, so a script that relies on the values the discarded pass produced - or on the generator values it burned - sees different values. Say in the same row, or in a `## Custom C# extensions` row if it reads better there, that `Table.CopyNativeAs` no longer resets a cursor that is still at the beginning, so a copy taken of such a cursor reflects the data as of the open rather than as of the copy.
- Build with `dotnet build Dataphor.slnx`, then run the three part commands from the root `AGENTS.md`, each as its own foreground command. Do not run `dotnet test Dataphor.slnx` as one command.
