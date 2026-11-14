description: Review the fix for a race in which several users changing the database schema at the same time could mix up the saved definitions of their objects, plus the fix for keyword tables that could be read half-built, and the survey of other shared state.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Language.D4/Keywords.cs, src/engine/DAE/Language.D4/Instructions.cs, src/engine/DAE/Language.SQL/Keywords.cs, src/engine/DAE/Language.RealSQL/RealSQLKeywords.cs, tests/DAE.Server.Tests/ConcurrentDDLTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/TestServerInstance.cs, docs/modernization-plan.md, AGENTS.md, tools/net-spike/README.md
----
# Catalog emitter race (K1), keyword tables (K25), static-state survey — handoff

## What changed

- **K1.** `ServerCatalogDeviceSession` no longer has a `static D4TextEmitter _emitter`; `ScriptPersistentObject` and `ScriptCatalogObject` each do `return new D4TextEmitter().Emit(...)`. Same emit mode (`ForCopy`, the default constructor), so the text is unchanged.
- **K25.** The five lazily built keyword tables (D4 `Keywords`, `ReservedWords`, `Instructions`; SQL `Keywords`; RealSQL `Keywords`) are now `private static readonly string[] X = PopulateX();`, with `PopulateX` building a local array and returning it, and the null check removed from `Contains`. Same reflection over the same public `const string` fields, so the word sets are unchanged. Fixed by inspection: the race exists only on first use in a fresh process, so no reproduction test.
- Docs: plan Known defects K1 and K25 → fixed; the spike-patch paragraph and table row say K1 landed; AGENTS.md and `tools/net-spike/README.md` no longer predict garbled scripts from the spike's concurrency step (lock errors remain until `store-sqlite-concurrency`). The `NOTE:` on `TestServerInstance` about shared statics is updated.
- No `docs/migration-notes.md` entry: nothing observable changes except that the corruption stops.

## Test added

`tests/DAE.Server.Tests/ConcurrentDDLTest.cs`, `ConcurrentSessionsStoreTheirOwnScripts` — the K1 reproduction:

- One in-process server (SQLite store, no libraries copied). 16 sessions on their own threads, released by a `Barrier`, each run 15 cycles of: create table (5 columns, key), create view over it, create operator with a 400-statement body. Each statement is its own `ExecuteScript`; nothing is dropped.
- Error filter: an exception whose innermost exception is a `SQLiteException` with `Busy` or `Locked` result code is tolerated (the cycle stops; its later objects are not expected). Anything else fails the test. `NOTE:` at the filter says `store-sqlite-concurrency` tightens it to no exceptions.
- Store check: after the concurrent phase, one session walks `process.Catalog` for the test's tables, views and operators, asserts their count equals the count of successful creates, and records `ServerCatalogDeviceSession.ScriptPersistentObject(obj)` per object ID. The server is stopped and `DAEObjects.ServerData` is read **directly** from the SQLite catalog file (read-only, `Pooling=False`) with System.Data.SQLite (the second option the ticket offered; flows to the test project transitively through the `DAE.SQLite` project reference). Every stored script must equal its serial emission exactly.
- Restart check: the server is restarted on the same directory and, for each session, its first operator is evaluated (`(1)` must equal the sum 1..400) and its first view counted (0). Only one of each per session: evaluating all 240 operators/views after restart cost ~20 s.

How it failed on the unfixed code (static emitter restored temporarily, then fix re-applied):

- At the ticket's suggested sizes (8 sessions × 15 cycles, 40-statement body) it **passed** on unfixed code: emissions did not overlap enough.
- 8 sessions, 150 statements: failed 1 of 4 runs. 8 sessions, 400 statements: failed 3 of 5 (and 1 of 1 earlier).
- **16 sessions, 400 statements (as committed): failed 6 of 6.** Failures seen: "Stored script of object N differs from its serial emission" (e.g. operator `K1O_4_3`'s stored script was `create table .General.K1T_6_4 in System.Temp { ID : stati…`, 155 chars instead of 23,849) and `DataphorException: Destination is too short. (Parameter 'destination')`.
- The "Destination is too short" stack the `build-runtime-layout` review asked for: `StringBuilder.AppendWithExpansion` ← `StringBuilder.Append(String)` ← `TextEmitter.Append` (`Emitter.cs:40`) ← `D4TextEmitter.EmitSourceStatement` (`D4Emitter.cs:1921`) ← … ← `TextEmitter.Emit` ← `ServerCatalogDeviceSession.ScriptPersistentObject` ← `InternalInsertPersistentObject` ← `InsertCatalogObject`. So it is K1. It did not appear on the fixed code.
- No "database is locked" errors occurred in any run (720 of 720 creates succeeded every time), so the lock tolerance was never exercised here. K3 ("collection was modified") did not appear.

Fixed code: passed 3 of 3 runs at ~11 s each (concurrent phase ~9 s).

## Validation

- `dotnet build Dataphor.slnx`: 0 errors.
- `dotnet test Dataphor.slnx --no-build`: Common.Tests 12 passed; DAE.Server.Tests 95 passed, 46 skipped (known-failure/unstable coverage entries), 0 failed, total 141 — the new test is the one addition; the coverage manifest needed no change.

## Survey of shared mutable statics

Re-ran the search over `src/engine/DAE`, `src/engine/Server`, `src/engine/Base`, `src/engine/Store.SQLite`: static fields (including `static readonly` of mutable types, multi-line declarations, lazily initialized static properties), and `[ThreadStatic]`/`ThreadLocal`/`AsyncLocal` (none).

- Fixed here: K1 (catalog emitter); K25 (five keyword tables).
- Confirmed safe, as the ticket listed: `Schema.Object._nextID`, `SeedNode._random`, `StringLikeNodeBase._regexCache`, `ReflectionUtility._assemblyByName`, `ServerCheckTable` column-name statics, `Compiler.StatementEntries`, `TimingUtility._currentTiming` (dormant, `TIMING` undefined), `TagNameCache`/`TagNames` (`USETAGNAMECACHE` undefined), `TestUtility.FRandom` (file not compiled), `InstanceManager._instances` (host config), the immutable singletons and `LineInfo` statics (no writer found), `BitArrayEqualityComparer._default` (benign double-create).
- New, all safe: the per-exception-class `ResourceManager` statics (thread-safe); `Security._key/_iV/_stringKey/_stringIV` (byte arrays only read, passed to crypto constructors); `CRC32.CCRC32Lookup` (read-only table); `TimingUtility._ticksPerSecond` (set once in the static constructor); `Engine.SystemLibraryVersion`, `FixedSizeCache.DefaultCutoff` (readonly, never mutated); `ProcessCallInfo.Empty` (property returning a new value each call); `Tag.None = null` in `MetaData.cs`.
- Nothing filed: no finding needed a design change.

## Known gaps / for the reviewer

- The test needs 16 sessions and 400-statement operators to catch the shared emitter reliably on this 24-core machine; on a machine with few cores it may catch it less often. It is deterministic on the fixed code (no tolerance besides the lock filter).
- Lock tolerance is untested in practice (no lock errors occurred); the filter code is by inspection.
- `ScriptCatalogObject` (roles) is covered by inspection only.
- The L2 test reads the SQLite file directly, so it is tied to the SQLite store's table name and column (`DAEObjects.ServerData`) and file name (`DAECatalog`, from `TestServerInstance.CatalogStoreConnectionString`).
- During development a `sed -i` converted two files to LF; both were rebuilt byte-exact (`ServerCatalogDeviceSession.cs` from `git show HEAD:` plus a byte-level re-edit; the new test file rewritten as UTF-8 BOM + CRLF). `git diff` for `ServerCatalogDeviceSession.cs` is 6 lines; worth a glance.
