description: The test that has many sessions change the catalog at the same time failed once in CI because sessions waited more than 30 seconds for the catalog store and gave up. The commit it failed on changed only a comment and a test script, so either the test is too tight for a slow machine or something holds the store lock longer than it should.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: tests/DAE.Server.Tests/ConcurrentDDLTest.cs, src/engine/Store.SQLite/SQLiteConnection.cs, src/engine/DAE/Store/StoreException.cs
----
## What failed

CI run 45546301771 (`ticket(review): bug-a-stored-binary-is-never-given-back`, commit `42383e93`), **Debug job only**; Release passed on the same commit. One failing test out of 1,106:

- `Alphora.Dataphor.DAE.Server.Tests.ConcurrentDDLTest.ConcurrentSessionsStoreTheirOwnScripts` - 2 m 48 s, then `Assert.That(failures, Is.Empty)` with 16 entries, every one the same:

  > `K1O_4_13: Alphora.Dataphor.DAE.Runtime.RuntimeException: Runtime error: The catalog store is locked by another session's open transaction and did not become available within 30 seconds.`

  (names `K1O_*`, `K1V_*`, `K1T_*` - operators, views and tables, across sessions 1-15, cycles 9-14, i.e. late in the run), thrown from the thread body at `ConcurrentDDLTest.cs:294`.

## Commit range

- Last green: `fcdfcd71` (`ticket(implement): bug-a-stored-binary-is-never-given-back`, run 45545026357, both jobs green).
- First red: `42383e93`. Its only source change is a comment in `src/engine/DAE/Schema/Device.cs` plus the coverage script `StreamValues.d4`, neither of which this test runs. So the red is not caused by the commit; it is intermittent, and the question is why.

## Where to look

- Error 130103 (`StoreException.cs`) is raised by the SQLite store when it cannot get its write lock within the timeout (`SQLiteConnection.IsLockTimeout`). The test itself has its own wording for "failed other than on a store lock" (see `tickets/.pre-existing-known.md`, the 2026-11-14 entry on this test), which suggests a store-lock failure was once tolerated and is not now - check what the test is meant to accept.
- Debug is slower than Release, and CI runners vary (job times this week ranged 18-28 min). Find out whether 15 sessions × 15 cycles can legitimately queue past 30 s behind one another on a slow runner (test is then too tight: lower the load or raise the wait for this test, with the reason) or whether a session holds the store transaction across something it should not (a real defect: fix it). Measure the longest single hold of the store lock in a Debug run before choosing.
- Prior work on this test and the store's locking: `complete/12-store-sqlite-wal-busy.md`, `complete/12.5-store-sqlite-concurrency.md`, `complete/11-port-catalog-emitter-race.md`.

## Done when

The cause is named with evidence, the fix (to the test or the engine) is in, and `ConcurrentSessionsStoreTheirOwnScripts` passes repeatedly in Debug locally (run it several times in a row with `--filter Name=ConcurrentSessionsStoreTheirOwnScripts`).
