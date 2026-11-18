description: When several sessions change the same in-memory table at the same moment, one of them can fail with an internal error, and the table's index may be damaged, because the in-memory device does no locking of its own. It behaved the same before the port.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Memory/MemoryDevice.cs, src/engine/DAE/Runtime.Data/NativeTable.cs, src/engine/DAE/Runtime.Data/NativeRowTree.cs, src/engine/DAE/Runtime.Data/RowTreeScan.cs, src/engine/DAE/Device.Catalog/CatalogDevice.cs
repro: verified
severity: corruption
likelihood: unusual
tradeoffs: Applications usually keep shared data on a SQL Server or PostgreSQL device and use the memory device for scratch tables, so a maintainer may defer this until a real application writes a shared memory table from several sessions; any locking added costs every memory-device read and write.
----
**Blocked: two design choices.** Unblocks when the owner answers both questions below (or edits the recommendations).

1. **Should the memory device provide transaction isolation** (the third arm: an uncommitted transaction's changes block other transactions, or are hidden from them)? **Recommended: no.** `docs/developers-guide/devices.md` promises the memory device only "minimal concurrency protection", and applications keep shared data on SQL Server or PostgreSQL. Record that in a `NOTE:` in `MemoryDevice.cs`, and move the `Transaction.Isolation.Run` manifest entry to `feat-ws7-devices-and-libraries`, to run against a device that locks. The alternative, row locks held for the life of a transaction and honouring the isolation levels, is a feature-sized change.
2. **How much locking for the first two arms** (concurrent statements must not corrupt a table's index; reading `System.Sessions`/`System.Processes` must not fail while sessions come and go)? **Recommended:** one reader-writer lock per `NativeTable`, held for the length of each device-level read or write, so readers share and writers exclude. Also lock the server's session and process lists, or take a snapshot of them under a lock, while the catalog device fills those tables. Cost: an uncontended lock on every memory-device row operation. Rejected: a lock per tree node (much harder to get right), and one lock for the whole device (serializes unrelated tables).

NTA decisions:
1. no, but put it as a low priority debt- ticket in the backlog.  Can draw inspiration from ../quereus, another database engine I own if it helps
2. yes to reader-writer lock per NativeTable

(Tending agent, 2026-11-18: the low-priority ticket is `tickets/backlog/debt-memory-device-transaction-isolation.md`. Point the `Transaction.Isolation.Run` manifest entry at `debt-memory-device-transaction-isolation`, not `feat-ws7-devices-and-libraries`, and keep the `NOTE:` in `MemoryDevice.cs` that records the memory device gives no transaction isolation.)

If question 1 is answered "no", the first two arms become an ordinary fix with the reproduction below.

## What happens

The memory device (`System.Temp`: the `System` library's default device, where a table goes when its library names no default device, as `General` does; also the base class of SimpleDevice) keeps each database-scoped table as one `NativeTable` with a B-tree index per key (`NativeRowTree`), shared by every session. Nothing serializes access to it: `MemoryDevice.EnsureNativeTable` locks only the list of tables, and inserts, updates, deletes and scans walk and rearrange the tree's nodes with no lock. A locking path exists (`#if LOCKROWTREE` in `RowTreeNode`'s constructor) but nothing defines the symbol and its code refers to a parameter that no longer exists, so it has not compiled for years. The engine's process and transaction locking does not cover the tree either.

So two sessions writing the same table at the same moment (or one reading while another writes) race inside the tree. Observed effect: one of the writes fails with a `NullReferenceException` inside `NativeRowTree.Compare`, reported as "A nil was encountered where a value was expected" while validating the table's key constraint. Plausible worse effect, not observed: a lost or misplaced row, since two threads split and move the same nodes.

This is not new: 3.1 had the same code, and its `ExecuteMultiple` ran sessions in parallel on thread-pool threads just as the ported version does on dedicated threads.

## Reproduction (verified)

Found by `port-engine-runtime-fixes` while writing the `AsyncExecution` coverage script. Run as a coverage script with implicit transactions off (manifest `preamble`: `SetUseImplicitTransactions(false);`, so the table is committed before the other sessions look for it):

```
create table AsyncMultiple { ID : Guid, key { ID } };
ExecuteMultiple('insert row { NewGuid() ID } into AsyncMultiple', 3);
if Count(AsyncMultiple) <> 3 then
	raise Error('a session did not insert its row: ' + Count(AsyncMultiple).ToString() + ' row(s)');
```

`ExecuteMultiple` runs the insert on three new sessions at once and, by design, does not return their errors, so the symptom in the script is a missing row (2 of 3). It failed in 1 of 4 uninstrumented runs, and in 5 of 6 runs with the swallowed exceptions written to a file (which changes the timing); every recorded error was the stack above (`NativeRowTree.Compare` ← `RowTreeNode.NodeSearch` ← `NativeRowTree.FindKey` ← `Scan.FindKey` ← `SeekTable.InternalOpen` ← key constraint validation of the insert). The committed `AsyncExecution` script avoids the race by giving each session a table of its own.

## Second arm (static, not reproduced)

`CatalogDevice.PopulateSessions` and `PopulateProcesses` (the `System.Sessions` and `System.Processes` tables) enumerate the server's session list and each session's process list without a lock, while other threads connect, disconnect, start and stop. The lists (`ServerChildObjects`) enumerate by index with no version check, so a concurrent removal can skip a row or index past the end. To confirm: read `System.Processes` in a loop on one session while another thread starts and stops processes. Same class of defect as the first arm: server-wide in-memory structures shared across sessions without synchronization.

## Third arm: transaction isolation (verified, coverage script `Transaction.Isolation.Run`)

Found by `test-case-result-helpers`, which made the coverage script `Transaction.Isolation.Run` compile. The script puts a one-row table on the session's default device (`System.Temp` in the coverage harness) and, for each pair of isolation levels (`Browse`, `CursorStability`, `Isolated`), runs two processes against it. Its first case: process 1 updates the row and sleeps 1 s before committing; 400 ms later process 2 updates the same row with a 200 ms timeout, and the script expects process 2 to fail because process 1's change is uncommitted. On the memory device process 2's update succeeds at once, so the script fails at its first case, `Lost Update (Browse, Browse)`, in every run (3 of 3).

Cause: nothing locks a memory-device row for a transaction. `MemoryDeviceSession.InternalInsertRow`, `InternalUpdateRow` and `InternalDeleteRow` change the shared `NativeTable` directly; `InternalBeginTransaction` ignores the isolation level (its code is behind `#if USEMEMORYDEVICETRANSACTIONS`, which nothing defines, and logs operations for undo, not locks); the engine's table nodes take no row locks, relying on the device (on a SQL device the database server locks).

This is a wider requirement than the first arm. The first arm asks that concurrent *statements* not corrupt the index; this one asks that an uncommitted *transaction's* changes block, or be hidden from, other transactions according to their isolation levels. `docs/developers-guide/devices.md` promises the memory device only "minimal concurrency protection", so a maintainer may decide the memory device should not provide transaction isolation at all; in that case this arm is declined, the decision goes in a `NOTE:` in `MemoryDevice.cs`, and the script's manifest entry moves to `feat-ws7-devices-and-libraries` to run against a device that locks.

For whoever runs the script against a locking device later: the cases after the first two blocks have defects of their own that only show once those blocks pass. In the "Non Repeatable Read" block `LShouldThrow := LI = 3` is never true (the indices run 0 to 2) and contradicts the block's comment, and both the asynchronous select and the timed update run on the same process (`LTransaction1`); each block also stops only one of its two processes. Coverage scripts are not edited to make them pass, so these need their own decision.

## Expected behaviour

Concurrent sessions reading and writing one database-scoped memory table see the results of a serial order of their statements: no internal errors, no lost rows, and the key constraint still enforced. Reading `System.Sessions` and `System.Processes` while sessions come and go never fails. If the third arm is accepted: a transaction's uncommitted changes to a memory table block or are hidden from other transactions as their isolation levels require, so `Transaction.Isolation.Run`'s first two blocks pass on `System.Temp`.
