description: Fix a race in which several users changing the database schema at the same time can mix up the saved definitions of their objects, which can silently corrupt the stored catalog, and check the engine for other shared state with the same problem.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Language/Emitter.cs, src/engine/DAE/Language.D4/D4Emitter.cs, src/engine/DAE/Language.D4/Keywords.cs, src/engine/DAE/Language.D4/Instructions.cs, src/engine/DAE/Language.SQL/Keywords.cs, src/engine/DAE/Language.RealSQL/RealSQLKeywords.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, tools/net-spike/Smoke/Program.cs, tools/net-spike/run.py
difficulty: easy
----
# Catalog emitter race (K1) and a survey of shared mutable statics

**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout of the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout) section (`src/engine/Server` is today's `Dataphor/Server`, `src/engine/DAE` is `Dataphor/DAE`). `tools/net-spike/` may already be deleted; if so, the patch and loop quoted below are the record of what it did.

## Goal

When a session creates, alters or drops a persistent object, the catalog device writes the object's D4 script into the catalog store; on a later name lookup or a restart the engine re-compiles the object from that stored script. The script is produced by `ServerCatalogDeviceSession.ScriptPersistentObject` and `ScriptCatalogObject`, which both call one `static D4TextEmitter _emitter` shared by every session in the process (`src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs`, field at ~line 2615, methods just below it). `D4TextEmitter` is not thread-safe: its base class `TextEmitter` (`src/engine/DAE/Language/Emitter.cs`) keeps the output `StringBuilder _text` and `int _indent` in instance fields and `Emit` resets them on entry. Two sessions emitting at once write into the same builder, and each returns a mixture of both scripts.

With the SQLite store the mixture usually broke the generated `insert` statement ("SQL logic error … unrecognized token"), so it surfaced as an error. A store that inserts through a cursor or with parameters (SQL CE did; the SQL Server store and a future parameterized SQLite store, `store-parameterized-statements`, would) stores the garbled text silently, and the damage appears only when the object is next loaded — possibly after a restart, in production, as a compile error or a wrong definition. That is why this is a corruption defect, not a concurrency nuisance.

## Scope

In scope:

- K1: make script emission in the catalog device session safe under concurrent sessions, with a reproduction test that inspects the stored scripts.
- A first-pass survey of other shared mutable statics reachable from session code in `src/engine/DAE` and `src/engine/Server` (below). The implementer confirms each candidate, fixes the ones that are obvious local fixes in this ticket, and files a ticket for any that is not (per "Bugs met along the way" in `tickets/rules/10-dataphor.md`).

Out of scope:

- SQLite lock behaviour ("database is locked", K10), write-ahead logging, transaction begin mode: `store-sqlite-concurrency`.
- K3 ("collection was modified" under concurrent DDL, site unknown) and shared *instance* state such as the name-resolution caches and the catalog itself: `port-concurrent-ddl-investigation`, held in `feat-ws2-server-port-remainder`. If the reproduction test below hits K3, record the stack in the handoff for that ticket; do not chase it here.
- Device libraries under `libraries/`: a quick check found only `FrontendServer._servers` (`libraries/Frontend/Source/Server.cs`), which is locked. Not in scope.

## Settled decisions and known defects

- K1 (plan, Known defects; verified by the spike).
- D2: the D4 scripts and stored catalog format are the oracle; the stored script text for an object must be exactly what single-threaded emission produces.
- No compatibility surface changes: the emitted text, the store format and all public signatures stay the same.

## The fix

Make the emitter local to each call: `return new D4TextEmitter().Emit(...)` in both `ScriptPersistentObject` and `ScriptCatalogObject`, and delete the static field. This is exactly the spike's patch (`tools/net-spike/run.py`, `PATCHES`, entry for `ServerCatalogDeviceSession.cs`: `return _emitter.Emit(` → `return new D4TextEmitter().Emit(`, applied to both methods). `D4TextEmitter`'s default constructor sets `EmitMode.ForCopy`, the same as the static instance had, so output is unchanged. The emitter is cheap to construct (a mode field; the builder is created in `Emit`). Do not add a lock instead: emission of a large operator holds it for the whole emit, and the per-call object is simpler. Do not use `[ThreadStatic]`: a per-call object is just as cheap and has no reentrancy question.

## Reproduction test (L2, concurrent sessions)

An NUnit test on the in-process harness from `test-harness-inprocess` (temporary instance directory, libraries from the runtime layout, SQLite store). It must fail on the unfixed code and pass on the fixed code; run it against the unfixed code once and say in the handoff how it failed.

- Start one server. Open N sessions (at least 8) each with its own process, on their own threads, released together by a barrier so emission overlaps.
- Each session runs M cycles (at least 15) of DDL that produces long object scripts, with names unique to the session and cycle: a table with several columns and a key, a view over it, and an operator whose body is long (for example 30–50 statements) so that each emit takes long enough to overlap. Model it on the spike's loop in `tools/net-spike/Smoke/Program.cs` (the threads section: create table/insert/view/operator, evaluate, drop), but **do not drop** the objects: they must still be in the store when the test inspects it.
- Errors during DDL: until `store-sqlite-concurrency` lands, the SQLite store still fails some cycles with "database is locked" (the spike saw 33 such errors in 120 cycles with only the emitter fixed). Collect every exception from every thread; the test fails on any exception whose innermost message is not the SQLite busy/locked condition. Garbling shows up as other errors (for example "unrecognized token" from the generated insert). A cycle that failed with a lock error is rolled back; skip its objects in the next step.
- Store check (this is what catches corruption that a store would accept silently): for every object of every successful cycle, read the script stored for it in the catalog store and compare it with the script emitted for the same object single-threaded after the concurrent phase. The stored script is the `ServerData` column of the `DAEObjects` store table (written by `CatalogStoreConnection.InsertPersistentObject`, `src/engine/DAE/Device.Catalog/CatalogStore.cs`). Two ways to read it, pick one and say which:
  - through the engine: `CatalogStoreConnection.SelectPersistentObject(objectID)` (`CatalogStore.cs` ~line 2131) returns a `PersistentObjectHeader` whose script is the `ServerData` column; reach a store connection from an in-process `ServerCatalogDeviceSession` (`((ServerProcess)process).CatalogDeviceSession`), adding an `internal` accessor visible to the test assembly only if none is reachable; or
  - directly: open the SQLite catalog file read-only with the store's own ADO.NET provider after stopping the server and `select Name, ServerData from DAEObjects`.
  The expected text is `ScriptPersistentObject(object)` called serially from one session for each object (deterministic for an unchanged object). Assert equality for every object, and assert the count of checked objects equals the count of successful creates. Equality is the check; "it parses" is not enough, because two interleaved fragments can still parse.
- Optionally (cheap and store-agnostic): stop and restart the server on the same instance directory and assert each object resolves and evaluates (the operator returns its expected value). This shows the stored scripts load, which is how silent corruption would surface in production.
- Keep the run short enough for every commit: N × M around 120 cycles took 1.6–8 s in the spike once the store was serialized; with deferred SQLite transactions expect tens of seconds at most. If the test does not fail reliably on the unfixed code with these sizes, raise the operator body length before raising N or M.

`store-sqlite-concurrency` tightens the error filter of this test to "no exceptions at all" once lock errors are gone. Leave a `NOTE:` at the filter saying so.

## Survey: other shared mutable statics reachable from session code

First pass done while writing this ticket, by searching `src/engine/DAE`, `src/engine/Server` and `src/engine/Base` for static fields of non-immutable types and checking each use. Confirm each line by reading the code.

Candidates that look like real races (each is a local fix):

- **Publish-before-populate lazy initialization** in five keyword tables: `Keywords._keywords` and `ReservedWords._reservedWords` in `src/engine/DAE/Language.D4/Keywords.cs` (~lines 217, 348), `Instructions._instructions` in `src/engine/DAE/Language.D4/Instructions.cs` (~line 136), `Keywords._keywords` in `src/engine/DAE/Language.SQL/Keywords.cs` (~line 109), `Keywords.keywords` in `src/engine/DAE/Language.RealSQL/RealSQLKeywords.cs` (~line 98). Each `PopulateX()` assigns `new string[count]` to the static field *before* filling it, and `Contains` populates only when the field is null. A second thread that calls `Contains` during the first call sees a non-null array of nulls and gets `false` for a real keyword. The D4 parser calls `Keywords.Contains`/`ReservedWords.Contains` (`Parser.cs` ~lines 2101, 2106, 2955, 3046, 3055, 3488) and the compiler calls `Instructions.Contains` when creating operators (`Compiler.cs` ~lines 6534, 6601, 7030), so the first concurrent parses after start can misparse or mis-flag an operator as built-in. Fix: build the array in a local and assign it last, or better, initialize the field from a static initializer (`private static readonly string[] _keywords = PopulateKeywords();` with `PopulateKeywords` returning the array), which the runtime runs exactly once under a lock. Behaviour after initialization is unchanged. A reliable reproduction needs a fresh process per attempt (the race exists only on first use), so fix these by inspection and say so in the handoff.

Checked and safe (no action):

- `Schema.Object._nextID` (`Schema/Schema.cs` ~line 763): `Interlocked.Increment`; setter locked.
- `SeedNode._random` (`Runtime.Instructions/MathNodes.cs` ~line 215): every use under `lock (typeof(SeedNode))`.
- `StringLikeNodeBase._regexCache` (`Runtime.Instructions/StringNodes.cs` ~line 866): under `lock (_regexCache)`; cached `Regex` objects are safe for concurrent matching.
- `ReflectionUtility._assemblyByName` (`src/engine/Base/ReflectionUtility.cs` ~line 339): created with `Interlocked.CompareExchange`, all reads and writes locked.
- `ServerCheckTable` column-name statics (`DAE/Server/ServerTransaction.cs` ~lines 369–372): set once in a static constructor.
- `Compiler.StatementEntries` (`Compiling/Compiler.cs` ~line 842): filled in the static constructor, read-only afterwards.
- `TimingUtility._currentTiming` (`src/engine/Base/TimingUtility.cs` ~line 64): only touched by methods marked `[Conditional("TIMING")]`, which nothing defines. Dormant; if `TIMING` is ever defined it is a race (add a `NOTE:` only if you are editing the file anyway — you are not, so leave it).
- `TagNameCache` / `TagNames` tag caches (`Language.D4/MetaData.cs` ~lines 30, 1425): compiled only under `USETAGNAMECACHE`, which is not defined (the file's own comment says it would need to be made thread-safe).
- `TestUtility.FRandom` (`Runtime.Instructions/TestNodes.cs` ~line 919): unsynchronized `Random`, but `TestNodes.cs` is not in the engine's compile list (a stray file). No action.
- `InstanceManager._instances` (`Server/Server/InstanceManager.cs` ~line 28): host-side configuration, not session code.
- Remaining statics are immutable singletons (`Symbol.Empty`, `Tag.None`, `LineInfo.Empty`/`StartingOffset`/`StartingLine`, `NativeAccessor.As*`, `StreamID.Null`) or lazily created stateless objects (`BitArrayEqualityComparer._default`). `LineInfo` has public mutable fields, so `LineInfo.Empty` etc. are shared mutable objects in principle; a search found no code that writes into them (the only use in the engine and server is `new LineInfo(LineInfo.StartingOffset)` in `Compiling/Plan.cs` ~line 681, a copy). Safe as long as that stays true.

The search was by field declaration; it can miss statics declared on several lines, `[ThreadStatic]` fields (none found), or shared state held through a static property. Repeat the search in your own words before trusting the list, and add what you find to the handoff.

## Edge cases & interactions

- Single-session behaviour: stored scripts must be byte-identical before and after the change. Verified by the L1 coverage scripts (unchanged pass count) and by the store-check equality in the new test.
- `ScriptCatalogObject` (roles, via `InsertRole`) has the same fix; the reproduction test exercises persistent objects. Covered by inspection: the same one-line change.
- Concurrent emit while another session is loading objects from the store: loading parses with a per-call `Parser`; unaffected. Inspection.
- The keyword-table fix must not change which words are keywords: the arrays are built from the same reflection over `const string` fields. Verified by the L1 suite (the `Lexer` and `ParserEmitter` scripts once they run) and inspection.
- Test flakiness: the test must pass deterministically on fixed code. The only nondeterministic tolerance is the lock-error filter; it must not accept any other error class. Inspect the filter.
- Test isolation: temporary instance directory, no fixed port in use (the server is in-process and not listening), objects created in the test's own library or the default `General` library of the temporary instance only.

## Done when

- The static emitter is gone; both script methods use a per-call emitter.
- The five keyword tables initialize safely.
- The reproduction test exists, failed on the unfixed code (handoff says how), and passes.
- The L1 suite results are unchanged (same passes, same known failures).
- The handoff lists each survey candidate with its disposition (fixed here, filed as a ticket with slug, or confirmed safe), including anything new found.
- No `docs/migration-notes.md` entry: nothing an application or extension can observe changes, other than the corruption no longer happening.

## TODO

- Replace `_emitter` with a per-call `D4TextEmitter` in `ScriptPersistentObject` and `ScriptCatalogObject`; delete the field.
- Write the concurrent-DDL reproduction test with the stored-script equality check; run it before making the fix and record how it fails, then make the fix and run it again.
- Fix the five lazy keyword tables with static initializers.
- Re-run the static-field search; confirm or extend the candidate list.
- File a ticket for any finding that is not a local fix (climb the "Architecture first" ladder in the ticket rules first).
- Run the L1 suite; record results and the survey dispositions in the handoff.
