description: A test that has sixteen sessions change the catalog at once failed in CI because unlucky sessions waited more than thirty seconds for their turn at the catalog file and gave up; the test now waits much longer, and the reason the wait can be that long is written down where the next reader will meet it.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: tests/DAE.Server.Tests/ConcurrentDDLTest.cs, src/engine/Store.SQLite/SQLiteStore.cs, docs/architecture/catalog-and-store.md
difficulty: easy
----
## What changed

Three edits, 12 inserted lines, no production behaviour touched.

**`tests/DAE.Server.Tests/ConcurrentDDLTest.cs`** (`ConcurrentSessionsStoreTheirOwnScripts`) — immediately before `instance.Server.Start()`, the fixture now sets `instance.Server.CatalogStoreConnectionString = TestServerInstance.CatalogStoreConnectionString + ";BusyTimeout=120000"`, with a comment giving the reason (the test keeps the one catalog-store write lock busy about two thirds of the concurrent phase; the longest single hold measured is under half a second while the longest wait is nearly four, so the default 30 s bounds a starvation tail and not a long transaction) and naming the tradeoff (a genuine store-lock deadlock introduced later would hang here two minutes per statement instead of failing in thirty seconds; the Debug lock-order assertion in `ServerCatalogDeviceSession.BeginStoreTransactions` is what catches that shape early). This is the same per-instance idiom `CatalogStoreConcurrencyTest`, `NativeSessionCleanupTest` and `AbandonedSessionCleanupTest` already use; the shared default in `TestServerInstance` is untouched, so no other fixture moves.

The load was **not** reduced: `SessionCount` 16, `CycleCount` 15 and `OperatorStatementCount` 400 are unchanged. The assertion stays strict — `Assert.That(failures, Is.Empty, ...)`; nothing was added that tolerates 130103.

**`src/engine/Store.SQLite/SQLiteStore.cs`** — a `NOTE:` tripwire above `DefaultBusyTimeout`: this constant bounds an unfair wait rather than a long transaction, pointing at the architecture bullet, and recording that the fix *if* applications are ever seen taking spurious store-lock timeouts under sustained concurrent DDL would be a fair gate in front of the store write transaction, not a bigger timeout. No code change in this file.

**`docs/architecture/catalog-and-store.md`** — one sentence added to the existing 130103 bullet: the wait is sleep-and-retry rather than a queue, so a session can be passed over repeatedly and see 130103 with no transaction long, with the measurement (one 3.8 s wait spanned 100 other acquisitions of the same lock against 15 competitors, where a fair queue would have let at most 15 through). `python tools/docs-size-check/check_size.py`: 13 files checked, 0 over the limit — the page did not outgrow its cap, so no split.

## Tests

**No new test, deliberately.** The reproduction of this bug *is* `ConcurrentSessionsStoreTheirOwnScripts`, and the fix is to that test's own configuration. The engine-side bound on 130103 is already owned by `CatalogStoreConcurrencyTest.StoreLockWaitReportsStoreLocked`, which sets `BusyTimeout=1000` on purpose. A test asserting that a shorter busy timeout fails would pin SQLite's behaviour, not the engine's.

**No migration note.** A test's own connection string plus documentation and a comment; nothing an application, deployment script or extension can observe.

## Validation run

- `dotnet build Dataphor.slnx` (Debug) — 0 errors, 143 warnings (all pre-existing).
- `dotnet test tests/DAE.Server.Tests --no-build --filter Name=ConcurrentSessionsStoreTheirOwnScripts` — five consecutive runs, all passing (36-38 s wall each, which includes fixture setup and the serial scripting phase).
- One further run with `--logger "console;verbosity=detailed"` to read the test's own timing line: **`16 sessions x 15 cycles in 27294 ms`**, inside the 26.9-27.9 s range measured before the change. The change alters only how long a session is willing to wait, and the concurrent phase did not move — which is the expected result and also evidence that the longer timeout is not being consumed in the normal case.
- `dotnet test tests/DAE.Server.Tests --no-build --filter FullyQualifiedName~CatalogStoreConcurrencyTest` — 7 passed. That fixture owns store-lock behaviour and the same connection-string idiom, so this is the check that the change disturbed nothing there.

No pre-existing failures surfaced; `tickets/.pre-existing-error.md` was not written. The full suite is CI's job.

## Honest gaps for the reviewer

- **The failure itself cannot be reproduced on this machine at the new setting, and was not re-reproduced in this stage.** The fix stage established the chain by temporarily lowering `BusyTimeout` to 2 s; that instrumentation is reverted and was not re-applied here. So the evidence that 120 s is enough is arithmetic (about 30x the 3.9 s tail measured here, about 5x the tail expected at CI's measured 6x slowdown) plus five clean local runs, not a run at CI's speed. The real confirmation is CI's Debug job staying green over subsequent pushes.
- **120000 is a bare literal in the test.** It is commented, but nothing links it to `SQLiteStore.DefaultBusyTimeout` or to the measurements, and nothing fails if someone later lowers it. Judged not worth a named constant for a single call site; worth a second opinion.
- **The comment asserts measured figures** ("about two thirds", "under half a second", "nearly four") that came from temporary timing code in the fix stage and are recorded in that ticket, now archived in `tickets/complete/`. The code comment is the only remaining place those numbers appear alongside the decision; if the reviewer thinks a comment should not carry un-rerunnable measurements, the alternative is to trim it to the mechanism and leave the numbers to the archived ticket.
- The reduction in `OperatorStatementCount`/`SessionCount`/`CycleCount` that the ticket forbade was not made, and the strict assertion was not loosened — both worth a direct check in the diff, since both are exactly the shortcuts a future agent under time pressure would take.

## Review findings seed (tripwires parked this stage)

- SQLite's busy wait is a lottery, not a queue, so the store's 30 s default bounds a starvation tail; parked as a `NOTE:` at `src/engine/Store.SQLite/SQLiteStore.cs` `DefaultBusyTimeout` and as a sentence in `docs/architecture/catalog-and-store.md`. It becomes real work only if an application is seen taking spurious store-lock timeouts under sustained concurrent DDL.
