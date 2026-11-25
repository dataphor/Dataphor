description: A test that has sixteen sessions change the catalog at the same time failed once in CI because some of them waited more than thirty seconds for their turn at the catalog file and gave up. Nothing was stuck; the test simply keeps the catalog so busy that an unlucky session can be passed over for longer than the engine's default wait allows, so this test needs to be given a longer wait.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: tests/DAE.Server.Tests/ConcurrentDDLTest.cs, docs/architecture/catalog-and-store.md, src/engine/Store.SQLite/SQLiteStore.cs
difficulty: easy
repro: verified
----
## The cause, with the measurements behind it

Nothing holds the catalog store's write lock too long. The test is simply loud enough to saturate that lock, and the wait for it is a lottery rather than a queue, so on a slow machine an unlucky session's wait crosses the engine's 30-second default.

All figures below are from this repository at `e3764acc`, Debug, on the development machine, measured 2026-11-25 with temporary timing code in `SQLStoreConnection.BeginTransaction`/`CommitTransaction`/`RollbackTransaction` (reverted; the working tree is clean).

**The longest single hold is under half a second.** Across three runs of `ConcurrentSessionsStoreTheirOwnScripts`, each of which takes the store's write lock 794 times (720 of those are the test's own DDL statements, 16 sessions x 15 cycles x 3 statements):

| | run 1 | run 2 | run 3 |
|---|---|---|---|
| longest single hold of the write lock | 366 ms | 377 ms | 490 ms |
| median hold | 20 ms | 19 ms | 19 ms |
| longest wait for the write lock | 3908 ms | 3843 ms | 3695 ms |
| 99th-percentile wait | 2224 ms | 2524 ms | 1720 ms |
| total time the lock was held | 20.2 s | 20.1 s | 19.8 s |
| concurrent phase, wall clock | 26.9 s | 27.8 s | 27.8 s |

So the longest hold is about 1/60th of the 30-second timeout, and no session holds the store transaction across anything it should not. The failing CI job was the Debug job, where the Debug-only lock-order assertion in `ServerCatalogDeviceSession.BeginStoreTransactions` is active; it did not fire, so the failure was not the known deadlock between the store's write lock and the catalog's lock either.

**The lock is saturated.** The write lock is held for about 20 of the concurrent phase's 28 seconds — a 63 % duty cycle with 16 sessions that do nothing but create schema objects. Only 13 % of that held time is spent inside SQLite statements; the rest is engine work done while the lock is held (emitting each object's D4 script, mutating the catalog, checking dependencies), because a DDL statement takes the store's lock *before* it runs. That ordering is deliberate and documented — it is what keeps DDL from deadlocking against the catalog's lock (`docs/architecture/catalog-and-store.md`, the lock-order bullet; the reasoning and the measured deadlock are in `tickets/complete/12.5-store-sqlite-concurrency.md`). It is not a defect to re-open here, but it is why this test saturates the lock so easily.

**The wait is unfair, so its tail is unbounded.** SQLite's `BusyTimeout` is a sleep-and-retry loop, not a queue: a waiting connection re-tries and may lose every time. Reconstructing the longest waits from timestamps shows how badly:

| waiting thread | wait | other acquisitions that completed *during* that wait |
|---|---|---|
| A | 3843 ms | 100 |
| B | 3669 ms | 101 |
| C | 3395 ms | 95 |
| D | 3180 ms | 88 |

Only 15 other threads were competing. A fair queue would let at most 15 through before the waiter's turn; instead the waiter was passed over about seven times by the whole field. What a session actually waits is therefore not "fifteen holds deep" but a lottery whose tail is bounded only by `BusyTimeout`.

**Why CI crossed 30 seconds and this machine does not.** On the failing CI Debug job the test took 2 m 48 s against 28 s here — about six times slower. Scaling the holds by six puts the expected tail in the tens of seconds, and the lottery adds a wide spread on top, which is exactly the observed shape: 16 of 720 statements failed, all late in the run (cycles 9-14) and spread across sessions, while the Release job on the same commit passed.

**Reproduced, by making the cliff reachable here.** With the store's `BusyTimeout` temporarily lowered to 2 s (just under the 3.7-3.9 s tail measured above) the test fails exactly as CI did: `130103` from the same thread body at `ConcurrentDDLTest.cs:294`, on statements late in the run across several sessions, differing only in the message's "2 seconds". That confirms the chain end to end — the failure appears precisely when the timeout drops below the starvation tail, and nothing else about the run changes.

## The fix

Give this one test a much longer store-lock wait, and say why in the code. The test's subject is that each session stores *its own* object script (hazard K1, the once-shared catalog emitter); the store's lock is incidental scenery for it. The real bound on 130103 is tested where it belongs, in `CatalogStoreConcurrencyTest.BusyAtTransactionStartLeavesNoTransaction`, which sets `BusyTimeout=1000` deliberately.

Two things this fix must not do:

- **Do not lower the load.** The plan's K1 row and the comment on `OperatorStatementCount` record the calibration: 8 sessions caught the shared emitter in 1 of 4 runs at 150 statements and 3 of 5 at 400; 16 sessions at 400 caught it in 6 of 6. Fewer sessions, cycles or statements means a weaker test.
- **Do not go back to tolerating a store-lock failure.** An earlier version of this test accepted 130103 and asserted only on "Concurrent DDL failed other than on a store lock" (see the 2026-11-14 entry for this test in `tickets/.pre-existing-known.md`). Keep the assertion strict: a 130103 here after the wait is raised would be real news.

The tradeoff to state in the comment: a longer wait means that if a future change does introduce a genuine store-lock deadlock, this test hangs for about two minutes per statement instead of failing in thirty seconds. The Debug lock-order assertion is what catches that shape early, and it is active in the job that failed.

## TODO

- In `ConcurrentDDLTest.ConcurrentSessionsStoreTheirOwnScripts`, immediately before `instance.Server.Start()`, set the instance's catalog store connection string to `TestServerInstance.CatalogStoreConnectionString + ";BusyTimeout=120000"`. Use the existing idiom — `CatalogStoreConcurrencyTest`, `NativeSessionCleanupTest` and `AbandonedSessionCleanupTest` all set `instance.Server.CatalogStoreConnectionString` on the server before starting it — rather than changing the shared default in `TestServerInstance`, whose comment deliberately keeps every other fixture on the options a fresh instance gets. 120 s is about 30 times the longest wait measured here and about 5 times the tail expected at CI's speed.
- Comment it with the reason, in brief: this test keeps the one catalog-store write lock busy about two thirds of the time, the longest single hold is under half a second, and SQLite's busy wait is a sleep-and-retry rather than a queue, so the default 30 s bounds a starvation tail and not a long transaction. Name the tradeoff above in one clause.
- Add one sentence to `docs/architecture/catalog-and-store.md`, to the bullet that already introduces 130103 (the "A wait for the write lock longer than the store's `BusyTimeout`" bullet): the wait is a sleep-and-retry rather than a queue, so under sustained concurrent DDL a session can be passed over repeatedly and see 130103 even though no transaction is long — measured at 100 other acquisitions during one 3.8-second wait with 15 competitors. Then run `python tools/docs-size-check/check_size.py` and split the page rather than raising its cap if it has outgrown 1,500 words.
- Add a one-line `NOTE:` at `SQLiteStore.DefaultBusyTimeout` saying the same thing in one sentence and pointing at that bullet, so a reader who meets the 30 s constant does not have to infer that it bounds an unfair wait. This is a tripwire, not queued work: it becomes real work only if an application is ever seen taking spurious store-lock timeouts under sustained concurrent DDL, and the fix then would be a fair gate in front of the store write transaction, not a bigger timeout.
- **No new test.** The reproduction of this bug is the existing test, and the fix is to that test's own configuration; a test asserting that a shorter busy timeout fails would be pinning SQLite's behaviour, not the engine's.
- **No migration note.** The change is a test's own connection string plus documentation; no application, deployment script or extension can observe it.

## Validation

- `dotnet build Dataphor.slnx` (Debug), 0 errors.
- `dotnet test tests/DAE.Server.Tests --no-build --filter Name=ConcurrentSessionsStoreTheirOwnScripts` five times in a row in Debug, all passing. Record the concurrent phase's reported time for each run (the test prints "16 sessions x 15 cycles in N ms"); it was 26.9-27.9 s here before the change and should not move, since the change alters only how long a session is willing to wait.
- `dotnet test tests/DAE.Server.Tests --no-build --filter FullyQualifiedName~CatalogStoreConcurrencyTest` — the fixture that owns store-lock behaviour and the same connection-string idiom, so that the change cannot have disturbed it.
- Report the filters used in the handoff, per the project's per-stage test rules. The full suite is CI's job.
