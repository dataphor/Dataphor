description: When several sessions change the same in-memory table at the same moment, one of them can fail with an internal error, and the table's index may be damaged, because the in-memory device does no locking of its own. It behaved the same before the port.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Memory/MemoryDevice.cs, src/engine/DAE/Runtime.Data/NativeTable.cs, src/engine/DAE/Runtime.Data/NativeRowTree.cs, src/engine/DAE/Runtime.Data/RowTreeScan.cs, src/engine/DAE/Device.Catalog/CatalogDevice.cs
repro: verified
severity: corruption
likelihood: unusual
tradeoffs: Applications usually keep shared data on a SQL Server or PostgreSQL device and use the memory device for scratch tables, so a maintainer may defer this until a real application writes a shared memory table from several sessions; any locking added costs every memory-device read and write.
----
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

## Expected behaviour

Concurrent sessions reading and writing one database-scoped memory table see the results of a serial order of their statements: no internal errors, no lost rows, and the key constraint still enforced. Reading `System.Sessions` and `System.Processes` while sessions come and go never fails.
