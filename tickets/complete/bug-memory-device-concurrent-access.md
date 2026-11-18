description: Several sessions changing the same in-memory table at once could fail with an internal error and damage the table's index, and reading the server's session and process lists could fail while sessions came and went. Each in-memory table now has its own lock, the lists are read from a copy, and the catalog skips sessions and processes that are shutting down.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/NativeTable.cs, src/engine/DAE/Runtime.Data/RowTreeScan.cs, src/engine/DAE/Runtime.Data/TableValue.cs, src/engine/DAE/Server/ServerObject.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Device.Catalog/CatalogDevice.cs, src/engine/DAE/Device.Memory/MemoryDevice.cs, tests/DAE.Server.Tests/MemoryDeviceConcurrencyTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/coverage-manifest.json, docs/developers-guide/devices.md, docs/migration-notes.md
repro: verified
----
## What was wrong

- **In-memory tables.** The memory device (`System.Temp` and any `System.MemoryDevice`) keeps each table as a `NativeTable` with one B-tree per key or order. Every session shares a database-scoped table, and nothing serialized access to it. Concurrent inserts split the same nodes, and the key check read half-moved nodes. Sessions got "A nil was encountered where a value was expected" (a `NullReferenceException` in `NativeRowTree.Compare`) or "Key not found" during rollback, and rows were lost.
- **Session and process lists.** The catalog device builds `System.Sessions`, `System.Processes`, `System.Plans`, `System.Scripts`, `System.SessionCatalogObjects`, `System.DeviceSessions` and `System.RemoteSessions` by enumerating the server's `ServerChildObjects` lists, and the enumerator read a live list with no lock.

## What changed

Implemented in `ticket(implement): bug-memory-device-concurrent-access`; one further fix in review (below).

- **Per-table lock.** `NativeTable` owns a `ReaderWriterLockSlim` (no recursion). `Insert`, `Update`, `Delete`, `Truncate` and `Drop` hold the write lock for the call; `HasRow` and every call on a `Scan` hold the read lock for the call. The scan's row-moved and row-deleted handlers run on the writer's thread inside the write lock and use unlocked bodies. `RowCount` stays unlocked (advisory).
- **Scans and `Truncate`.** The catalog device repopulates its tables with `Truncate` plus `Insert`, which replaces all of a table's trees. `Scan.Open` re-resolves its tree under the lock (`NativeTable.CurrentIndex`), and `First`, `Last`, `FindKey` and `FindNearest` move a scan held open across a truncate onto the current tree (`Scan.FollowTruncate`).
- **Lists.** `ServerChildObjectEnumerator` walks a copy taken under the list's lock. The catalog device's populate methods read each member that `Dispose` clears once into a local and skip half-disposed sessions and processes. `ServerProcess.PushExecutingProgram`/`PopExecutingProgram` lock the executing-program list, and the new `TryGetStackLimits` reads a process's stack limits under that lock.
- **Transaction isolation (owner decision).** Not provided, by decision: an accepted-tradeoff `NOTE:` at `MemoryDeviceSession.InternalBeginTransaction`, the memory-device paragraph of `docs/developers-guide/devices.md`, and the `Transaction.Isolation.Run` manifest entry point to `debt-memory-device-transaction-isolation` (in `backlog/`).
- **Review fix: choosing the tree for an ordered scan.** `TableScan.InternalOpen` picked the tree of a non-clustered order with `NonClusteredIndexes[_key]` before any lock, while another session's `Truncate` was emptying and refilling that list. An ordered read of a catalog table with a second key, for example `System.ServerLinks order by { Name }` (rebuilt on every read), failed with "Index was outside the bounds of the array". The new `internal NativeTable.IndexFor(Schema.Order)` chooses the tree under the read lock, and `TableScan.InternalOpen` uses it.

## Tests

`tests/DAE.Server.Tests/MemoryDeviceConcurrencyTest.cs`, about 6 s for the fixture:

- `ConcurrentInsertsIntoOneMemoryTableAllLand` reproduces the in-memory table race (8 sessions × 500 inserts into one `System.Temp` table, with a reader).
- `SessionAndProcessListsReadWhileSessionsComeAndGo` reproduces the list race and the half-disposed-entry failures.
- `CursorOnCatalogTableRepositionsAfterAnotherSessionRereadsIt`: deterministic, single-threaded check that a cursor held open across another session's read can reposition.
- `OrderedReadsOfCatalogTableWhileOthersRereadIt` (added in review) reproduces the ordered-scan race. One session counts `System.ServerLinks` 2,001 times while two others open and close cursors on `System.ServerLinks order by { Name }`. Before the review fix both readers failed in the first run, with `IndexOutOfRangeException`. With the fix the fixture passed 5 of 5 runs.

Validation: `dotnet build Dataphor.slnx` gives 0 errors. `dotnet test Dataphor.slnx --no-build` is green: Common.Tests 13 passed; DAE.Server.Tests 177 tests, 152 passed and 25 skipped (the known-failure, unstable and fragment coverage cases), in 5 min 14 s.

## Review findings

Checked by reading the implement diff and the code around it, then running the fixture and the full suite.

- **Correctness of the lock.**
  - Checked every direct use of `ClusteredIndex`/`NonClusteredIndexes` outside `NativeTable` (grep over `src` and `libraries`). Every tree mutation goes through the locked `NativeTable` methods.
  - Tree events are field-like, so subscribing to them under a shared read lock is thread-safe.
  - `RowTreeNode`'s lock mode is a no-op unless `LOCKROWTREE` is defined, so readers share no mutable state on a node.
  - The catalog populate (`Truncate` plus inserts) runs under `lock (Device.Headers)`, so two populates of one table cannot interleave.
  - **Found:** the ordered-scan race above. **Fixed** inline, with its reproduction test, and the migration-notes row extended to mention it.
  - `LocalTable.InternalOpen` uses the same unlocked pattern. Left alone: its table is a private buffer, never shared.
- **Dropped tree read by `Next`/`Prior` after a truncate.** The implementer listed this as untested. I confirmed it from the code: `NativeRowTree.Drop` frees stream values but leaves node links and scalar values readable, so `Next`/`Prior` keep working on the old rows. A scan on a non-clustered order then looks its row up in the new clustered tree, and raises `ClusteredRowNotFound` only if the repopulate left that row out. This is conditional and narrow, so I parked it as a `NOTE:` tripwire at the clustered lookup in `RowTreeScan.InternalGetRow`.
- **Half-disposed sessions and processes.** I checked every member the populate methods read against `ServerSession.Dispose` and `ServerProcess.Dispose`.
  - `CurrentLibrary` and `ProcessInfo` are never cleared, so reading them directly is safe.
  - `RemoteSessions` is created lazily on a disposed process. That is harmless, and already covered by the implementer's `NOTE:`.
  - No further finding.
- **List snapshot.** Every writer of `ServerChildObjects` takes `lock (this)`, the same lock the enumerator copies under, so the snapshot is consistent. Each enumeration now allocates an array; that cost is acceptable for lists of this size. No note added.
- **Resource cleanup.** `NativeTable`'s `ReaderWriterLockSlim` is never disposed. Declined to add disposal:
  - The lock creates wait handles only under contention, and the GC finalizes them.
  - Disposing it in `Drop` would break a scan that closes after the table is dropped.
  - Recorded here only.
- **Error handling.** Every lock acquisition is paired with a release in a `finally`. The public `Scan` methods never nest the lock; internal self-calls use the unlocked bodies. `Scan.Dispose` takes the read lock, so disposing a scan on a thread that holds the write lock would throw. The only route there is the contrived sort-operator case, already in the `NOTE:` at the lock's declaration.
- **Performance.** The measured cost, about 16% on large memory-table scans, sits in a `NOTE:` at the lock's declaration with a remedy. Not re-measured.
- **Tests.** All three implementer tests reproduce a defect that was fixed (two races, one deterministic), so all three were kept. One test was added for the review fix. No test was cut.
- **Docs.**
  - `devices.md`: the new paragraph matches the behaviour.
  - `migration-notes.md`: both rows are accurate. The D4 row was extended for the review fix, and the custom-extension row about `ServerChildObjects` enumeration was kept.
  - `docs/architecture.md` describes no memory-device locking, so it needs no change.
- **Compatibility.** No public signature changed: `IndexFor`, `CurrentIndex`, `EnterReadLock`/`ExitReadLock` and `TryGetStackLimits` are all `internal`. No D4 semantics, class names, file formats or error codes changed.
- **Left as recorded by the implementer.**
  - The `MaxStackDepth`/`MaxCallDepth` setters in `ServerCatalogDeviceSession`, which write another process's executing program without the new lock. An existing `NOTE:` covers them.
  - The `PopulateRemoteSessions`/`PopulateDeviceSessions` tripwires.
  - The lock-order notes.
  - The unexplained timing outliers. They did not recur in this review's 5 fixture runs or the full suite run.
- **Source hygiene.** CRLF and byte-order-mark state preserved in the edited files (`core.autocrlf=true`; the Markdown stays LF). File sizes: `RowTreeScan.cs` 812 lines, `NativeTable.cs` 691 lines; neither warrants a split.
