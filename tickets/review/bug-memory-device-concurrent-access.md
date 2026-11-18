description: Several sessions changing the same in-memory table at once could fail with an internal error and damage the table's index, and reading the server's session and process lists could fail while sessions came and went. Each in-memory table now has its own lock, the lists are read from a copy, and the catalog skips sessions and processes that are shutting down; this review checks that work.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/NativeTable.cs, src/engine/DAE/Runtime.Data/RowTreeScan.cs, src/engine/DAE/Server/ServerObject.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Device.Catalog/CatalogDevice.cs, src/engine/DAE/Device.Memory/MemoryDevice.cs, tests/DAE.Server.Tests/MemoryDeviceConcurrencyTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/coverage-manifest.json, docs/developers-guide/devices.md, docs/migration-notes.md
repro: verified
----
## What was wrong

- **In-memory tables (arm 1).** The memory device (`System.Temp` and any `System.MemoryDevice`) keeps each table as a `NativeTable` with one B-tree per key or order. Every session shares a database-scoped table, and nothing serialized access to it. Concurrent inserts split the same nodes, and the key-constraint check read half-moved nodes. The result was a `NullReferenceException` in `NativeRowTree.Compare`, reported as "A nil was encountered where a value was expected", or "Key not found" during rollback, plus lost rows.
- **Session and process lists (arm 2).** The catalog device builds `System.Sessions`, `System.Processes`, `System.Plans`, `System.Scripts`, `System.SessionCatalogObjects`, `System.DeviceSessions` and `System.RemoteSessions` by enumerating the server's `ServerChildObjects` lists. The enumerator read a live list with no lock.

## What changed

**Per-table lock (owner decision 2).** `NativeTable` owns a `ReaderWriterLockSlim` (`NoRecursion`), created by a field initializer and so present on every table.
- `Insert`, `Update`, `Delete`, `Truncate` and `Drop` hold the write lock for the call. `HasRow` holds the read lock.
- The bodies moved to private `Internal*` methods. `Truncate` calls `InternalDrop` and `Create` under one write lock.
- `RowCount` stays unlocked, as the ticket said; a remark on it says so.
- `NativeTable.EnterReadLock`/`ExitReadLock` and `CurrentIndex` are `internal`. No public signature changed.

**Scan (`RowTreeScan.cs`).** Every public method that reads the tree or the position is now a wrapper that takes the table's read lock and calls an unlocked `Internal*` body: `Active` (so `Open`, `Close`, `Dispose`), `Reset`, `First`, `Last`, `Next`, `Prior`, `BOF`, `EOF`, `GetRow(IRow)`, `GetKey`, `GetData`, `FindKey` and `FindNearest`.
- `GetRow()` creates its row and then calls the locked `GetRow(IRow)`; the lock is never nested.
- The row-moved and row-deleted handlers (which run on the writer's thread inside the write lock) and every internal self-call use the unlocked bodies.
- `CompareKeys` stays unlocked: it compares the two given rows by the tree's fixed order and reads no node. A comment says so.

**Beyond the ticket's design: scans and `Truncate`.** The catalog device repopulates `System.Sessions` and the other `CacheLevel = None` tables with `Truncate` plus `Insert` on every read, and `Truncate` replaces all of a table's trees.
- *Gap between choosing a tree and locking.* A scan's access path is read from the table before `Scan.Open` takes the lock, so a scan could open on a tree another session had just dropped. `InternalOpen` now re-resolves it with `NativeTable.CurrentIndex`. Without this line the list test fails 6 of 6 runs.
- *A cursor held open across another session's read.* It kept the dropped tree, and `First`, `Last`, `Reset` or a seek failed with a null reference. The new `Scan.FollowTruncate`, called at the start of `InternalFirst`, `InternalLast`, `InternalFindKey` and `InternalFindNearest`, moves the scan to the current tree and its events. `Next` and `Prior` deliberately keep reading the old tree's rows until the scan repositions.
- The ticket said "no further change" for this case; both are local fixes of the same race.

**Session and process lists (arm 2).** `ServerChildObjects.ServerChildObjectEnumerator`'s constructor copies the items under the list's lock, and the enumerator walks the copy. The public constructor signature is kept.
- No foreach over these lists relies on seeing items added during the loop. Checked: `Engine.cs`, `ServerSession.cs`, `ServerProcess.cs`, `PlanCache.cs`, `Debugger.cs` (its own lists), `SecurityNodes.cs`, `LocalServer.cs`.

**Beyond the ticket's design: half-disposed entries.** A snapshot alone did not fix the reproduction. A session or process stays in its list until its `Dispose` finishes, and `Dispose` first nulls the members the populate methods read: a session's `User`, `SessionInfo`, `Processes`, `SessionObjects` and its ID (set to -1); a process's `Plans`, `Scripts`, `DeviceSessions`, `_executingPrograms`, `_serverSession` and its ID.
- `CatalogDevice`'s populate methods now read each such member once into a local and skip the entry if it was cleared. A new `ConnectedSessions` helper yields each session's ID and process list.
- `PopulateProcesses` skips ID -1, because two disposing processes would otherwise both insert key -1.
- `PopulateProcesses` read `MaxStackDepth`/`MaxCallDepth`, which read the top of the process's executing-program list while that process's own thread pushes and pops it. This was hit in 2 of 9 runs. The fix:
  - `ServerProcess.PushExecutingProgram`/`PopExecutingProgram` now `lock (_executingPrograms)`.
  - A new `internal bool ServerProcess.TryGetStackLimits(out int, out int)` reads under that lock and returns false for a process or session being disposed. `PopulateProcesses` uses it.
  - Push and pop run once per program start and stop, so the lock is uncontended in practice.

**Transaction isolation (owner decision 1).**
- An accepted-tradeoff `NOTE:` at `MemoryDeviceSession.InternalBeginTransaction` points to `debt-memory-device-transaction-isolation`.
- In the memory-device paragraph of `docs/developers-guide/devices.md`, "minimal concurrency protection" is replaced by a plain statement: statements on one table are serialized, and transactions are not isolated from each other.
- The `Transaction.Isolation.Run` manifest entry now names `debt-memory-device-transaction-isolation`, with a note that this is by decision. Its status and error are unchanged, and it still fails in its recorded way.

**Migration notes.** The ticket expected none. I added two rows because the doc already records comparable fixes (for example the `CommitApplicationTransaction` nil-error row): one under "D4, catalog and libraries" (concurrent statements on in-memory tables now succeed; catalog list reads skip sessions that are disconnecting) and one under "Custom C# extensions" (`ServerChildObjects` enumeration walks a copy). Trim them if the reviewer disagrees.

## Tests added (`tests/DAE.Server.Tests/MemoryDeviceConcurrencyTest.cs`)

- `ConcurrentInsertsIntoOneMemoryTableAllLand` reproduces arm 1. It lifts `System.Temp`'s default 1000-row cap, then 8 sessions each insert 500 rows into one `System.Temp` table with interleaved keys while 1 session repeatedly runs `Count` and a keyed `exists`. It asserts no failures, the total count, every key found through the key index, and each session's row count. On the unfixed code it failed 4 of 4 runs with the nil-value error. With the fix it passed in every run (about 20).
- `SessionAndProcessListsReadWhileSessionsComeAndGo` reproduces arm 2 and the half-disposed and gap-before-lock fixes. 2 sessions read `Count(System.Sessions)` and `Count(System.Processes)` while 4 sessions connect, start a process and disconnect for one second. Failure rates before each fix:

  | Code state | Runs failed |
  |---|---|
  | Unfixed | 4 of 4 |
  | Snapshot only | 3 of 3 (`session.User` null) |
  | Without the executing-program lock | 2 of 9 |
  | With `CurrentIndex` disabled | 6 of 6 |

  With every fix it passed in every run (about 20).
- `CursorOnCatalogTableRepositionsAfterAnotherSessionRereadsIt` reproduces the `FollowTruncate` fix. It is deterministic and single-threaded: open a cursor on `System.Sessions`, let another session read `System.Sessions`, then call `First`, `Next`, `Last` and `Prior`. Before the fix it failed with a `NullReferenceException`.

Validation: `dotnet test tests/DAE.Server.Tests --filter FullyQualifiedName~MemoryDeviceConcurrencyTest` (about 10 s). `dotnet build Dataphor.slnx` gives 0 errors, with no new warnings in the edited files. `dotnet test Dataphor.slnx` is green: 176 tests, 151 passed and 25 skipped, the known, unstable and fragment coverage cases as before. `AsyncExecution`, `Cursors`, `CursorLibrary`, `CursorTest` and `SystemCatalog` pass.

## Measured cost of the lock

Method: a throwaway probe in a scratch worktree. HEAD and HEAD plus this patch were built in Release side by side and alternated, 3 runs each with 3 measured rounds per run, so 9 samples each. The workload was 50,000 inserts into a `System.Temp` table, 20 scans of it (`Max(V from B)`), 50,000 keyed `exists`, and 5 cursors over `B add { -ID N } order by { N }` (a private sort buffer).

| Workload | Median before | Median after | Change |
|---|---|---|---|
| Scans (1M rows read) | 584 ms | 678 ms | +16%, about 95 ns per row |
| Inserts | 715 ms | 708 ms | none measurable |
| Keyed lookups | 138 ms | 140 ms | none measurable |
| Sorts | 2874 ms | 2899 ms | none measurable |

Baseline-only runs varied by up to 40% on this machine, so treat the figures as rough. The result is recorded in a `NOTE:` at the lock's declaration, with the remedy if it ever shows in profiles: let private buffers skip the lock.

## Known gaps and things to look at

- **Scan on a dropped tree between repositions.** After another session's `Truncate`, a scan that only calls `Next`/`Prior` keeps reading the dropped tree's rows; its position stays valid until it repositions. A scan on a non-clustered access path then looks up rows in the new clustered tree in `GetRow`, which can raise `ClusteredRowNotFound`. Untested. Reaching it takes an ordered cursor on a table that is truncated while the cursor is open (catalog tables; `FrontendDevice` and `SimpleDevice` truncates).
- **`MaxStackDepth`/`MaxCallDepth` setters** (`ServerCatalogDeviceSession` updating another process's row in `System.Processes`) still write another process's executing program without the new lock. Untouched; a `NOTE:` near line 2674 there already discusses those setters.
- **Tripwires parked as `NOTE:`.**
  - `PopulateRemoteSessions` and `PopulateDeviceSessions` read plain lists their own process changes without a lock (as the ticket asked).
  - At the lock's declaration: a D4 sort operator that reads the same table from inside a tree operation throws `LockRecursionException`, and two such operators reading each other's tables could deadlock.
  - The measured lock cost, also at the lock's declaration.
- **Enumeration allocates.** Every enumeration of a `ServerChildObjects` list now allocates an array.
- **Unexplained timing outliers.** Early in the work, one list-test run took 67 s and one insert-test run 24 s, both passing; neither recurred in 16 later runs. Instrumented runs showed the concurrent phase at about 1 s and server start at 2.5 to 3.3 s. One full-suite run took 7.4 min against 4.8 min for the final one. In isolation `ConcurrentDDLWithQueriesAndDrops` cycled in 2.9 to 3.7 s, against 3.3 s in an earlier ticket's run. I attribute the outliers to machine load but did not prove it.
- **Lock order.** Locks are held for one call only, and two tables' locks are never held together except through a D4 sort, as in the note above.
  - `MemoryDeviceSession`'s drop path takes `lock (tables)` and then the table's write lock. The reverse order needs that contrived sort case.
  - The catalog device takes `lock (Device.Headers)` and then the header table's write lock.
- Source hygiene: CRLF line endings and byte-order-mark state are preserved in every edited file (the `.md` and `.json` files were LF before and still are).
