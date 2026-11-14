description: When starting a transaction fails part-way (for example because the catalog database is locked by another session), the session is left inside a half-started transaction that never ends on its own, so its later work is neither committed nor kept consistent and is rolled back, or half-kept, when the session closes.
prereq: store-sqlite-concurrency
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Schema/Device.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreConcurrencyTest.cs
repro: verified
severity: corruption
likelihood: unusual
tradeoffs: Once store-sqlite-concurrency lands, the catalog store can no longer fail at the start of a transaction, which leaves only device sessions whose begin fails (typically a lost connection to a SQL Server or PostgreSQL device, after which the session is often unusable anyway), and the fix changes transaction coordination that every device goes through.
----
## What happens

A process transaction has several participants: the process itself, every remote session and every device session the process has connected (the catalog device session among them). `ServerProcess.InternalBeginTransaction` (`src/engine/DAE/Server/ServerProcess.cs`, about line 1263) first pushes the process transaction, then calls `BeginTransaction` on each remote session and each device session in turn. Nothing undoes the earlier steps if a later one throws:

- the process transaction stays pushed (`InTransaction` is true, `TransactionCount` 1);
- the participants that began before the failing one stay begun;
- the failing device session has already counted the level: `Schema.DeviceSession.BeginTransaction` (`src/engine/DAE/Schema/Device.cs`, about line 1660) pushes its own transaction before it calls `InternalBeginTransaction`, and does not remove it when that throws.

`BeginTransactionalCall` (the implicit transaction every statement runs in, `ServerProcess.cs` about line 1785) begins only when the process has no transaction, so after the failure no later statement of that process gets an implicit transaction of its own, and none is committed: they all run inside the half-started one until the client explicitly rolls back or commits, or the session closes and the transaction is rolled back. The client is not told: the error it got was the participant's (for example 130103), and the process reports itself in a transaction.

## Reproduction (verified, during `store-sqlite-wal-busy`)

With the SQLite catalog store, which until `store-sqlite-concurrency` begins a store write transaction at the start of every transaction of a session that has written to the catalog before (an in-process probe, not kept):

1. Server with `BusyTimeout=1000` appended to the catalog store connection string. Session B runs `create table B1 …` (B now begins a store transaction with each of its transactions).
2. Session A begins an explicit transaction and runs `create table A1 …` (A holds the store's write lock).
3. B runs `create table B2 …`: fails after about 1 s with 130103, raised by the catalog device session's begin. Afterwards `processB.InTransaction` is true, `TransactionCount` 1.
4. A commits. B runs `create table B3 …`: succeeds, and B is still in the same transaction (count 1). The catalog store connection is not in a SQLite transaction, so B3's rows are written in autocommit.
5. B disconnects: its open transaction is rolled back, the in-memory catalog forgets B3, the store keeps its rows; after a restart B3 is back.

After `store-sqlite-concurrency` the catalog store begins its transaction at the first write instead, inside a statement, so this route closes (that ticket carries a test for it). The engine defect stays for any other participant whose begin can fail: a SQL device whose connection fails while beginning, a remote session.

## Expected

Beginning a transaction is all or nothing: if any participant fails to begin, the participants that began are rolled back, the failing one keeps no level for this transaction, the process transaction is removed, and the error reaches the caller with the process exactly as it was before the call (`TransactionCount` unchanged). This holds for the implicit begin in `BeginTransactionalCall` and for an explicit `BeginTransaction`.

## Notes for the fix

- The failing participant's own partial begin is its business: `DeviceSession.BeginTransaction` should remove the level it pushed when `InternalBeginTransaction` throws, and each `InternalBeginTransaction` override should leave nothing begun when it throws (for `ServerCatalogDeviceSession`, after `store-sqlite-concurrency`, the begin no longer touches the store; check `CatalogDeviceSession`'s DDL instruction log).
- Unwinding at a nested level must not touch the outer level of participants that never began the inner one; roll back only the participants this call began, in reverse order.
- The reproduction test needs a participant whose begin fails on demand. After `store-sqlite-concurrency` the catalog store no longer fails at begin, so this likely means a small device class in a test library whose session throws from `InternalBeginTransaction` when told to. Check the process's `TransactionCount` after the failure, and that a later statement runs in, and commits, an implicit transaction of its own.
