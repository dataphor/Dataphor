description: When starting a transaction fails part-way, the session is left inside a half-started transaction that never ends on its own; later work then runs outside the transaction on some devices and is half-kept when the transaction is rolled back. Make starting a transaction all or nothing.
prereq: store-sqlite-concurrency
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Schema/Device.cs, src/engine/DAE/Server/ServerTransaction.cs, tests/DAE.Server.Tests/TransactionBeginTest.cs (new), tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
----
## The defect

A process transaction has participants: the process itself, each remote session (a server link) and each device session the process has connected, the catalog device session among them. `ServerProcess.InternalBeginTransaction` (`src/engine/DAE/Server/ServerProcess.cs`, about line 1263) pushes the process transaction, then calls `BeginTransaction` on each remote session and then each device session, in list order. If one throws, nothing is undone:

- the process transaction stays pushed (`TransactionCount` one higher);
- the participants before the failing one keep their new level;
- the failing device session keeps its new level too: `Schema.DeviceSession.BeginTransaction` (`src/engine/DAE/Schema/Device.cs`, about line 1659) pushes its `DeviceTransaction` before calling `InternalBeginTransaction` and does not pop it when that throws;
- the participants after the failing one never begin.

`BeginTransactionalCall` (`ServerProcess.cs`, about line 1815) begins an implicit transaction only when the process has none, so every later statement runs inside the half-started transaction and none commits until the client rolls back, commits, or closes the session. Work on a device session that never began that level runs with no device transaction, so a later rollback does not undo it.

## Reproduction (verified in this fix pass, probe not kept)

A device class in the test assembly, derived from `MemoryDevice`, whose session (derived from `MemoryDeviceSession`, whose constructor is `protected internal` and so reachable from a subclass) throws from `InternalBeginTransaction` when a static switch says so: always, or only when `Transactions.Count` (after the push) equals a given level. The test registers the class with the class loader directly, after `instance.Server.Start()`, and this works:

```csharp
Schema.LoadedLibrary system = instance.Server.Catalog.LoadedLibraries[Engine.SystemLibraryName];
Schema.RegisteredAssembly assembly = new Schema.RegisteredAssembly(typeof(FailingBeginDevice).Assembly, system);
instance.Server.Catalog.ClassLoader.Classes.Add(new Schema.RegisteredClass("Test.FailingBeginDevice", system, assembly, typeof(FailingBeginDevice).AssemblyQualifiedName));
```

then `create device FailDevice class "Test.FailingBeginDevice";`, `create table T in FailDevice { ID : Integer, key { ID } };`, `create table M { ID : Integer, key { ID } };` (on the default temporary memory device) and `insert row { 1 ID } into T;`. The process's device sessions are then, in order: the catalog session, the failing session, the temporary memory device's session. The order matters: the memory session must come after the failing one to show the half-kept work.

Observed on HEAD:

- **Implicit begin.** With the switch on, `insert row { 2 ID } into M` throws the device's error. Afterwards `InTransaction` is true and `TransactionCount` 1; the catalog and failing sessions are at level 1, the memory session at 0. With the switch off, `insert row { 3 ID } into M` succeeds and the process is still in the same transaction (count 1). After `RollbackTransaction`, M still has 2 rows: row 3 was written on a memory session that was in no transaction, so the rollback did not undo it.
- **Nested explicit begin.** After `BeginTransaction`, a second `BeginTransaction` failing at level 2 leaves `TransactionCount` 2; the catalog and failing sessions at 2, the memory session at 1.
- **Deferred event handler.** `ServerTransaction.InvokeDeferredHandlers` (`src/engine/DAE/Server/ServerTransaction.cs`, about line 115) calls `InternalBeginTransaction` *inside* its `try`, and its `catch` rolls back if `InTransaction`. Today, with the begin half-done, that rollback happens to unwind the half level for the sessions that began it, but rolls back the memory session's *outer* level. Once the begin becomes all or nothing, the same `catch` would roll back the caller's outer transaction instead (for an explicit transaction, the client's own transaction disappears on a failed commit; for an implicit one, `EndTransactionalCall` then reports a "not in transaction" failure in place of the device's error). The begin has to move above the `try`.

The SQLite catalog store route in the original report (the store beginning its write transaction at the start of each transaction) is closed by `store-sqlite-concurrency`; no in-tree participant other than a remote session (`RemoteSessionImplementation.BeginTransaction`, a call to another server) can fail at begin today. SQL devices defer the database begin to first use. Custom devices from extensions can fail at begin.

## Expected behaviour

Beginning a transaction is all or nothing. If any participant fails to begin, the participants that began this level are rolled back, the failing one keeps no level, the process transaction is removed, and the participant's error reaches the caller with the process as it was before the call. This holds for the implicit begin in `BeginTransactionalCall`, for an explicit `BeginTransaction` at any nesting level, and for the nested begin of deferred event handlers.

## Design

**Device session (`Schema.DeviceSession.BeginTransaction`).** If `InternalBeginTransaction` throws, pop the level just pushed (`_transactions.EndTransaction(false)`; it holds no operations yet) before wrapping and rethrowing. The contract for `InternalBeginTransaction` overrides becomes: leave nothing begun when you throw. The in-tree overrides already meet it (`CatalogDeviceSession` adds its `BeginTransactionInstruction` last; `ApplicationTransactionDeviceSession` pushes its own list last; `MemoryDeviceSession` and `SQLDeviceSession` do nothing that can fail). Document the contract on the abstract method.

**Process (`ServerProcess.InternalBeginTransaction`).** Keep the `UseDTC` check first. After pushing the process transaction, count how many remote sessions and device sessions began. In a `catch`: mark the new process transaction `InRollback` (as `InternalRollbackTransaction` does; device rollback code reads it), roll back only the participants that began this call, logging (`ServerSession.Server.LogError`) and not rethrowing their rollback errors, then `_transactions.EndTransaction(false)` and rethrow the original error with `throw;`. Order: device sessions in reverse begin order with the catalog device session last (the rule `InternalRollbackTransaction` follows), then remote sessions in reverse. Each participant's `RollbackTransaction` pops only its current level, which is the one this call began, so outer levels are untouched. A shared helper with `InternalRollbackTransaction` is fine if it stays readable; do not change `InternalRollbackTransaction`'s behaviour.

**Deferred handlers (`ServerTransaction.InvokeDeferredHandlers`).** Move `InternalBeginTransaction(IsolationLevel.Isolated)` above the `try`, so that a failed begin propagates without rolling back the enclosing transaction.

**Unchanged, checked.** `ServerProcess.DeviceConnect` and `RemoteConnect` catch up a newly connected session's levels in a loop and disconnect (dispose) the session if a begin fails; disposing rolls back the levels it began (`EnsureTransactionsRolledback`), and with the device fix the failed level no longer lingers. The other callers of the public `BeginTransaction` (`Engine.RegisterCoreSystemObjects`, `System.Integration`'s copy loop, `System.Internet`'s `LoadXML`, the native CLI) call it before their `try`, which is correct once the begin is atomic. `BeginTransactionalCall` callers call it before their `try` and `EndTransactionalCall` is not reached on its failure.

**Left alone.** A device's `WrapException` sets `StopError` on a connection failure, which during a statement makes `EndTransactionalCall` roll back the whole transaction stack. A connection failure during a begin sets it too, but the next `BeginTransactionalCall` clears it, so the outer levels are kept. That is today's behaviour and the begin is now atomic; changing it is a separate decision. Add a `NOTE:` tripwire at the begin's `catch` saying so.

## Tests

A new fixture, `tests/DAE.Server.Tests/TransactionBeginTest.cs` (add it to the project's explicit `Compile` list), holding the failing-begin device and session classes and a helper that starts a server and registers the class as above. Reset the static switch in `finally`. Assert through public members (`TransactionCount`, `InTransaction`) and through D4 results, not through the process's internal device session list. Three tests, each failing on HEAD:

- **Implicit begin fails.** The statement throws the device's message; `TransactionCount` is 0; with the switch off, the next insert into M leaves `InTransaction` false (it committed its own implicit transaction), and M has exactly the rows that committed.
- **Nested explicit begin fails.** After `BeginTransaction` and inserts into T and M, a second `BeginTransaction` failing at level 2 throws the device's message and leaves `TransactionCount` 1; one `RollbackTransaction` then leaves `InTransaction` false and neither T nor M has the inserted rows.
- **Deferred handler begin fails.** M carries an after-insert handler tagged `DAE.IsDeferred = "true"` (in the probe, `attach operator H to M on after insert tags { DAE.IsDeferred = "true" };` with `H` inserting into a log table compiled and ran). Inside an explicit transaction, insert into M, then switch the device to fail at level 2 and call `CommitTransaction`: it throws the device's message and `TransactionCount` is still 1 (the client's transaction survives the failed commit); rolling back leaves neither the row nor a log row.

## TODO

- Pop the pushed level in `Schema.DeviceSession.BeginTransaction` when `InternalBeginTransaction` throws; document the contract on the abstract `InternalBeginTransaction`.
- Unwind the participants begun so far in `ServerProcess.InternalBeginTransaction` on failure, as described; add the `NOTE:` about `StopError` there.
- Move the begin above the `try` in `ServerTransaction.InvokeDeferredHandlers`.
- Add `TransactionBeginTest.cs` with the three tests; confirm each fails before the fix and passes after.
- Add a row to the plan's Known defects table (`docs/modernization-plan.md`, next free number, K30 at the time of writing): a failed transaction begin left a half-started transaction; fixed, with the test and this slug.
- Add a **done** entry under "Custom C# extensions" in `docs/migration-notes.md`: when a device session's `InternalBeginTransaction` throws, the engine now removes that level and rolls back the other participants' new level; an override must leave nothing begun when it throws.
- Preserve each edited file's tabs, CRLF line endings and byte-order-mark state.
- Run `dotnet build Dataphor.slnx` and `dotnet test Dataphor.slnx`.
