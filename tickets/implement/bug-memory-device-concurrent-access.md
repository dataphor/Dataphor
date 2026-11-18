description: When several sessions change the same in-memory table at the same moment, one of them can fail with an internal error and the table's index may be damaged, because the in-memory device does no locking of its own; reading the server's session and process lists can also fail while sessions come and go. Add a lock per in-memory table and read those lists from a copy.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/NativeTable.cs, src/engine/DAE/Runtime.Data/RowTreeScan.cs, src/engine/DAE/Runtime.Data/NativeRowTree.cs, src/engine/DAE/Device.Memory/MemoryDevice.cs, src/engine/DAE/Server/ServerObject.cs, src/engine/DAE/Device.Catalog/CatalogDevice.cs, tests/DAE.Server.Tests/coverage-manifest.json, tests/DAE.Server.Tests/ConcurrentDDLTest.cs, tests/DAE.Server.Tests/TestServerInstance.cs, docs/developers-guide/devices.md
repro: verified
----
## Decisions (owner, 2026-11-18)

1. The memory device does **not** provide transaction isolation (an uncommitted transaction's changes are neither locked against nor hidden from other transactions). The gap is kept on record in `tickets/backlog/debt-memory-device-transaction-isolation.md` (which names this ticket as its prereq). This ticket records the limit in a `NOTE:` in `MemoryDevice.cs` and points the `Transaction.Isolation.Run` manifest entry at `debt-memory-device-transaction-isolation`.
2. Statement-level safety: **one reader-writer lock per `NativeTable`**, held for the length of each device-level read or write call, so readers share and writers exclude. Rejected: a lock per tree node (much harder to get right), one lock for the whole device (serializes unrelated tables).

## The defect

The memory device (`System.Temp`, the `System` library's default device and the base of SimpleDevice) keeps each database-scoped table as one `NativeTable` (`src/engine/DAE/Runtime.Data/NativeTable.cs`) with one B-tree (`NativeRowTree`) per key or order, shared by every session. Session-scoped tables (`ServerSession.Tables`) are shared by that session's processes. Nothing serializes access: `MemoryDeviceSession.EnsureNativeTable` locks only the list of tables, and `NativeTable.Insert`/`Update`/`Delete`/`Truncate` and the cursor class `Scan` (`RowTreeScan.cs`) walk and split the tree's nodes with no lock. The `#if LOCKROWTREE` path in `RowTreeNode` is dead (nothing defines it and it no longer compiles); the `LockMode.Shared` arguments that `Scan` passes to `RowTreeNode` do nothing.

Observed (`port-engine-runtime-fixes`, while writing the `AsyncExecution` coverage script, implicit transactions off):

```
create table AsyncMultiple { ID : Guid, key { ID } };
ExecuteMultiple('insert row { NewGuid() ID } into AsyncMultiple', 3);
// Count(AsyncMultiple) was 2 in 1 of 4 plain runs, 5 of 6 instrumented runs
```

Every captured error was a `NullReferenceException` in `NativeRowTree.Compare` ← `RowTreeNode.NodeSearch` ← `NativeRowTree.FindKey` ← `Scan.FindKey` ← `SeekTable.InternalOpen` ← the insert's key-constraint check, reported as "A nil was encountered where a value was expected". `ExecuteMultiple` swallows its sessions' errors, so in D4 the symptom is a missing row. 3.1 had the same code.

Second arm (static): `CatalogDevice.PopulateSessions`, `PopulateProcesses`, `PopulateScripts`, `PopulatePlans`, `PopulateRemoteSessions` and `PopulateDeviceSessions` (`src/engine/DAE/Device.Catalog/CatalogDevice.cs`, about lines 98–365) enumerate `Server.Sessions` and each session's `Processes` (and their `Plans`, `Scripts`) without a lock while other threads connect, disconnect, start and stop. Those lists are `ServerChildObjects` (`src/engine/DAE/Server/ServerObject.cs`): `Add`/`Remove`/`Insert` lock the list, but `ServerChildObjectEnumerator` reads `_objects[_current]` against a live `Count` with no lock or version check, so a concurrent removal skips an item, returns one twice, or reads a slot past the end (`null`, then an exception in the populate code).

## Design

### Per-table lock

`NativeTable` owns a `ReaderWriterLockSlim` (created in both constructors). Writers take the write lock for the whole call: `Insert`, `Update`, `Delete`, `Truncate`, `Drop`. Readers take the read lock for the whole call: `HasRow`, and every public method of `Scan`.

The constraint that decides how `Scan` is locked: a `Scan` keeps its position (`_indexNode`, `_entryNumber`) across calls, and the tree keeps it valid by raising `OnRowsMoved`/`OnRowDeleted` (`NativeRowTree.RowsMoved`/`RowDeleted`), which run `Scan.RowTreeRowsMoved`/`RowTreeRowDeleted` **on the writer's thread**, inside `NativeTable.Insert`/`Delete`/`Update`. Those handlers call `Next()` (through `UpdateScanPointer`) and replace `_indexNode`. So:

- Every public `Scan` method takes the read lock: `Open`, `Close` (and so `Dispose` and the `Active` setter), `Reset`, `First`, `Last`, `Next`, `Prior`, `BOF`, `EOF`, `GetRow` (both overloads), `GetKey`, `GetData`, `FindKey`, `FindNearest`, `CompareKeys` (only if it reads the tree). `Open`/`Close` must be included: `InternalOpen` subscribes and calls `First()`, and `InternalClose` sets `_indexNode` to null, which a handler running on a writer's thread at that moment would dereference.
- The handlers, which already run under the writer's write lock, must not take the read lock. Either split each public method into a locking wrapper and an unlocked body that the handlers and the other bodies call, or create the lock with `LockRecursionPolicy.SupportsRecursion` (a write-lock holder may then enter the read lock). Prefer the split: it keeps `NoRecursion` and makes an accidental read-then-write on the same table (which `ReaderWriterLockSlim` cannot upgrade and would throw on even with recursion) fail loudly in tests. Whichever is chosen, the methods `Scan` calls on itself from inside a locked method (`First` → `Next`, `Next` → `Last`, `Prior` → `First`, …) must call the unlocked bodies.
- The lock is held for one call only, never between calls: a cursor left open on a table never blocks writers, and two tables' locks are never held across a call boundary, so lock ordering between tables does not arise from this code. A sort's compare operator can run D4 during a tree operation; if it reads a *different* memory table it takes that table's read lock, which is fine; reading the same table from inside its own write would throw `LockRecursionException` with `NoRecursion` (contrived; acceptable, leave a `NOTE:` at the lock's declaration).
- `RowCount` stays an unlocked read (the `MaxRowCount` check in `MemoryDeviceSession.InternalInsertRow` is advisory).

Consequences to accept and state in the code comment: every `NativeTable` gets the lock, including the private buffers that sorts, unions and explodes create (`OrderTables.cs`, `UnionTable.cs`, `ExplodeTable.cs`, `TableValue.cs`, `DataValue.cs`), which are never shared, so their lock is always uncontended. Measure the cost roughly (for example time the full coverage run, or a loop of inserts into a memory table, before and after) and record the measured figure in the handoff; don't claim a magnitude you didn't measure.

The catalog device's tables are `NativeTable`s too (`CatalogDeviceSession.InternalExecute` repopulates a shared header's table under `lock (Device.Headers)` with `Truncate` + `Insert`, then opens a scan on it). With the per-table lock, a second session repopulating while a first one scans is structurally safe; the first scan sees the rows change under it as any dynamic cursor does. That is acceptable; no further change.

### Session and process lists

Root cause is one site: `ServerChildObjects.GetEnumerator()` (`ServerObject.cs`, about line 386). Make it enumerate a snapshot: copy `_items[0.._count)` to an array under `lock (this)` and enumerate the copy. That fixes every `foreach` over `Sessions`, `Processes`, `Plans`, `Scripts` and `Batches` at once, including `ServerSessions.GetSession`, without touching each populate method and without holding the list's lock while the catalog device inserts rows (which takes a table write lock under `lock (Device.Headers)`; a snapshot avoids creating a new lock-order edge). Check the other enumerations of these lists (`find_references` on `.Sessions`/`.Processes`) for any that rely on seeing items added during the loop; none is expected. The indexer `this[int]` getter stays unlocked.

`process.DeviceSessions` (`Schema.DeviceSessions`) and `process.RemoteSessions` are plain lists that their own process changes without a lock; `PopulateDeviceSessions` and `PopulateRemoteSessions` read them from another thread. Fixing them means locking their writers too, which is outside the two arms the owner approved. Leave a `NOTE:` tripwire at those two populate methods ("enumerates another process's list without a lock; if `System.DeviceSessions`/`System.RemoteSessions` reads ever fail under load, lock the list's writers and copy it here").

### Transaction isolation: record the limit

Add at `MemoryDeviceSession.InternalBeginTransaction` (or the `MemoryDevice` class comment) a `NOTE:` saying the memory device serializes statements per table but locks nothing for a transaction, so an uncommitted transaction's changes are visible to and can be overwritten by other transactions at every isolation level; owner decision 2026-11-18; tracked by `debt-memory-device-transaction-isolation`. In `docs/developers-guide/devices.md`, "Memory Device" section, replace "minimal concurrency protection" with a plain statement of the same guarantee (concurrent statements on one table are serialized; transactions are not isolated from each other). Leave "full transactional support" alone: rollback is done by the engine's undo log, not the device.

`tests/DAE.Server.Tests/coverage-manifest.json`, entry `Transaction.Isolation.Run`: change `ticket` to `debt-memory-device-transaction-isolation`, keep `status` and `error`, and adjust the `note` to say the memory device does not isolate transactions by decision. The script's own defects in its later blocks are recorded in `debt-memory-device-transaction-isolation` and are not this ticket's concern.

No migration note expected: no public signature changes, and the only behaviour an application can notice is that concurrent writes to a shared memory table now succeed. If the split changes a public `Scan` or `NativeTable` member's signature (it shouldn't), add a `docs/migration-notes.md` entry.

## Reproduction test

The ticket owes its reproduction. Put it in `DAE.Server.Tests` as an NUnit test modelled on `ConcurrentDDLTest` (`TestServerInstance`, one session per thread, failures gathered in a `ConcurrentBag`), not as a coverage script: `ExecuteMultiple` hides errors and three inserts hit the race in only about 1 of 4 runs. Shape: create one database-scoped table on `System.Temp` with a key (for example `{ ID : Integer, Session : Integer, key { ID } }`); then N sessions (about 8) each insert M rows with distinct keys (about 500) while one more session repeatedly reads `Count(T)` and a keyed lookup; assert no failures, the final count is N × M, and every inserted key is found. Size it as `ConcurrentDDLTest` did: run it against the current code until it fails reliably (record the observed rate in a comment, as that test does), then confirm it passes after the fix. Keep its run time to a few seconds.

The second arm stays static unless a cheap test falls out (a reader of `System.Processes` in a loop while other threads open and close sessions); if one reproduces the failure reliably in a few seconds, add it to the same test class; otherwise say in the handoff that the second arm is unverified.

## TODO

- Add the `ReaderWriterLockSlim` to `NativeTable`; take the write lock in `Insert`, `Update`, `Delete`, `Truncate`, `Drop`, the read lock in `HasRow`; comment the lock (what it covers, held per call, the recursion `NOTE:`).
- Lock every public `Scan` method with the table's read lock, split into locked wrappers and unlocked bodies so the row-moved/row-deleted handlers and internal self-calls use the unlocked bodies.
- Make `ServerChildObjects.GetEnumerator()` enumerate a snapshot taken under the list's lock; add the `NOTE:` tripwires at `PopulateDeviceSessions` and `PopulateRemoteSessions`.
- Add the transaction-isolation `NOTE:` in `MemoryDevice.cs`; update the memory-device paragraph in `docs/developers-guide/devices.md`.
- Repoint the `Transaction.Isolation.Run` manifest entry at `debt-memory-device-transaction-isolation`.
- Write the concurrent-insert reproduction test; confirm it fails on the unfixed code and passes with the fix.
- Measure the lock's cost roughly and record it in the handoff.
- `dotnet build Dataphor.slnx` and `dotnet test Dataphor.slnx` green (coverage run included: `AsyncExecution` and the cursor tests exercise `Scan` heavily).
- Preserve each edited file's tabs, CRLF and byte-order-mark state.
