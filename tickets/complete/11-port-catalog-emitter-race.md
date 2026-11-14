description: Fixed a race in which several users changing the database schema at the same time could mix up the saved definitions of their objects, fixed keyword tables that could be read half-built, and checked the engine for other shared state with the same problem.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Language.D4/Keywords.cs, src/engine/DAE/Language.D4/Instructions.cs, src/engine/DAE/Language.SQL/Keywords.cs, src/engine/DAE/Language.RealSQL/RealSQLKeywords.cs, tests/DAE.Server.Tests/ConcurrentDDLTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/TestServerInstance.cs, docs/modernization-plan.md, AGENTS.md, tools/net-spike/README.md
----
# Catalog emitter race (K1), keyword tables (K25), static-state survey

## What landed

- **K1.** `ServerCatalogDeviceSession` no longer shares one `static D4TextEmitter` across sessions; `ScriptPersistentObject` and `ScriptCatalogObject` each emit with `new D4TextEmitter()`. Same emit mode (`ForCopy`, the default constructor), so the stored text is unchanged.
- **K25.** The five lazily built keyword tables (D4 `Keywords`, `ReservedWords`, `Instructions`; SQL `Keywords`; RealSQL `Keywords`) are `static readonly` fields set by their initializer (`PopulateX()` builds a local array and returns it), which the runtime runs once. Same reflection over the same `const string` fields, so the word sets are unchanged. Fixed by inspection: the race exists only on first use in a fresh process.
- **Tests** (`tests/DAE.Server.Tests/ConcurrentDDLTest.cs`):
  - `ConcurrentSessionsStoreTheirOwnScripts`, the K1 reproduction. 16 sessions × 15 cycles of create table / view / 400-statement operator on one in-process server with the SQLite store, released together by a barrier; every stored script (`DAEObjects.ServerData`, read directly from the SQLite file after stopping the server) must equal the object's serial emission, and after a restart one operator and one view per session must evaluate. Failed 6 of 6 runs on the shared emitter (garbled stored scripts; "Destination is too short" from the shared `StringBuilder`, stack `TextEmitter.Append` ← `D4TextEmitter.EmitSourceStatement` ← … ← `ScriptPersistentObject` ← `InsertCatalogObject`). About 11 s.
  - `OperatorResolutionCacheAllowsConcurrentAddAndClear`, the K3 reproduction, added in review (below). About 0.5 s.
- Docs: plan Known defects K1 and K25 marked fixed; the WS2 work-item row marked done; the spike-patch text and table, AGENTS.md and `tools/net-spike/README.md` no longer predict garbled scripts from the spike's concurrency step (lock errors remain until `store-sqlite-concurrency`). No `docs/migration-notes.md` entry: nothing an application or extension can observe changes except that the corruption stops.

## Survey of shared mutable statics

Searched `src/engine/DAE`, `src/engine/Server`, `src/engine/Base`, `src/engine/Store.SQLite` for static fields (including `static readonly` of mutable types and lazily set static properties) and `[ThreadStatic]`/`ThreadLocal`/`AsyncLocal` (none). Fixed: K1, K25. Confirmed safe: `Schema.Object._nextID` (interlocked), `SeedNode._random` (locked), `StringLikeNodeBase._regexCache` (locked), `ReflectionUtility._assemblyByName` (locked), `ServerCheckTable` column names and `Compiler.StatementEntries` (static constructor, read-only afterwards), `TimingUtility._currentTiming` (dormant, `TIMING` undefined), `TagNameCache`/`TagNames` (`USETAGNAMECACHE` undefined), `TestUtility.FRandom` (file not compiled), `InstanceManager._instances` (host configuration), the per-exception `ResourceManager` statics (thread-safe), the `Security` key arrays and `CRC32.CCRC32Lookup` (only read), the immutable singletons (`Symbol.Empty`, `Tag.None`, `NativeAccessor.As*`, `StreamID.Null`, the `LineInfo` statics; no writer found), `BitArrayEqualityComparer._default` (a harmless double create). Nothing filed.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors, no warning from the new test file.
- `dotnet test Dataphor.slnx --no-build`: Common.Tests 12 passed; DAE.Server.Tests 96 passed, 46 skipped (the manifest's known-failure and unstable entries), 0 failed, 142 total (the implement handoff's 141 plus the K3 test added in review).

## Review findings

Read the `ticket(implement): port-catalog-emitter-race` diff first, then the handoff, then ran the build, the new tests and the full suite.

**Correctness of the fix: checked, nothing found.** Both emit sites use a per-call emitter; nothing else referenced the deleted field (the build would fail). The keyword initializers: the classes have no static constructor and no other static field that the populate methods read; the private array field is not a literal, so `GetFields()` (public fields) does not pick it up; the field initializer gives the once-only, fully built publication the ticket asked for. Single-session output is unchanged: the coverage manifest results are unchanged, and the new test compares every stored script with serial emission.

**Source hygiene: checked, nothing found.** BOM state preserved in every edited file. The SQL and RealSQL keyword files are stored LF in the index and CRLF in the working tree through `core.autocrlf`, as before (`git ls-files --eol`); the D4 keyword and instruction files are CRLF in both. The `ServerCatalogDeviceSession.cs` diff is the six expected lines. The new test file is UTF-8 BOM + CRLF. The handoff's `sed -i` incident left no trace.

**Test code: three minor defects, fixed here.**
- If a session failed to connect or start its process before the barrier, the other 15 threads waited at the barrier forever and the test hung instead of failing. The thread now removes itself from the barrier on such a failure, and the barrier is disposed.
- The store check spelled the catalog file name (`DAECatalog`) a second time, beside `TestServerInstance.CatalogStoreConnectionString`. It now takes the data source from the server's own `GetCatalogStoreConnectionString()` and opens it read-only without pooling, so it follows the harness's connection string. The unused `System.IO` import went with it.
- `RunConcurrently` passed its lock-error count out through a `ref` parameter; it now returns it.

**Contract left unverified: K3 reproduction added.** The plan's `port-concurrent-ddl-investigation` row said to carry the K3 check (the operator resolution cache, fixed during triage) into the in-process concurrency tests once they exist; nothing in `tests/` exercised it, and its only reproduction was the spike's smoke step. Added `OperatorResolutionCacheAllowsConcurrentAddAndClear` (8 threads, 500 ms of `Add` and `Clear(name)` on one cache). Checked that it fails on an unsynchronized cache (each `lock (_syncRoot)` in `OperatorResolutionCache.cs` temporarily replaced by a no-op, the file restored byte-exact afterwards): 3 of 3 runs failed with "Collection was modified". It passes on the real code. The plan row now names it.

**Test cost: accepted as is.** `ConcurrentSessionsStoreTheirOwnScripts` adds about 11 s to every run; the handoff's measurements show that smaller sizes stop catching the shared emitter, and the comment on its constants says which size to raise first.

**Lock-error tolerance: inspected, not exercised.** No lock error occurred in any run (720 of 720 creates). The filter accepts only an innermost `SQLiteException` with `Busy` or `Locked`, and a cycle stopped by one leaves its later objects out of both the expected and the stored set, so a partial cycle cannot cause a false failure. The `NOTE:` at the filter already tells `store-sqlite-concurrency` to tighten it, and that ticket lists this one as a prereq.

**`ScriptCatalogObject` (roles): covered by inspection only,** as the handoff says. It is the same one-line change as the tested site; a role test would only check the constructor call again.

**Docs: read every file the change touches and the plan's WS2 table.** Fixed here: the plan's WS2 work-item row for this ticket was not marked done (the plan marks landed rows **Done**, as the `build-server-project` row is). Not edited, for the next gardening pass: `tickets/plan/12-store-sqlite-concurrency.md` and `tickets/backlog/feat-ws2-server-port-remainder.md` still describe K3 as "site unknown" and `port-concurrent-ddl-investigation` as open, while the plan's K3 row and WS2 table say it is fixed and done; other tickets' text is not this ticket's to change.

**Tripwires:** none new. The existing `NOTE:`s on `TestServerInstance` (re-check statics before fixtures run in parallel) and at the lock filter stand.

**Tickets filed:** none; no finding needed a design change.
