description: A program can ask the server to compile a statement once and then run it as many times as it likes. For a statement that creates something, the second run put back the very thing the first run made — carrying any change made to it since — instead of making what the statement describes. Compile such a statement again before running it a second time.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerPlan.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/PlanCache.cs, src/engine/DAE/Server/ServerSession.cs, src/engine/DAE/Server/ServerBatch.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/Server/Server/RemoteServerPlan.cs, tests/DAE.Server.Tests/DeviceClassAttributeFixture.cs, tests/DAE.Server.Tests/DeviceClassAttributeRerunTest.cs, docs/modernization-plan.md, docs/migration-notes.md
difficulty: hard
repro: verified
----
## The rule to build

**A compiled plan that contains a statement changing the catalog is used for at most one execution. Executing such a plan again compiles its statement afresh first, and executes that.**

The owner settled the direction on 2026-11-23: a compiled create statement builds a fresh catalog object on each execution. This is that decision, stated as one invariant over the whole family of catalog statements rather than as a change to each create statement, because create is not the only statement whose compiled form holds a catalog object the catalog has since mutated or removed (see *Why the rule covers alter and drop too*).

Nothing is taken away from a caller: `IServerProcess.PrepareStatement` keeps meaning what `src/engine/DAE/CLI.cs` says it means, a prepared statement may still be executed as often as the caller likes, and no new error code appears. What changes is that the second and later executions mean what the statement says.

## What happens today

A caller holds a compiled statement through `IServerProcess.PrepareStatement` and executes it as often as it likes. For a statement that creates a catalog object, the compiled form does not hold the statement — it holds **the object the compiler already built**. `Compiler.CompileCreateDeviceStatement` constructs the `Schema.Device` while compiling (`src/engine/DAE/Compiling/Compiler.cs:9359`) and executing the node is just `program.CatalogDeviceSession.CreateDevice(_newDevice)` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:889`). Every `Create…Node` in that file is built the same way. So the second execution registers the identical object: same identity, same object ID, and any change made to it in between.

Measured on 2026-11-23 against an in-process server: prepare `create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" }`, execute it, then `alter device D alter class { alter "MaxRowCount" = "200" }`, then `drop device D`, then execute the prepared plan again — the device comes back with a row limit of 200, from a statement that says 100, and no error is raised. The two landed sibling fixes do not touch this path: `bug-remembered-statement-applies-a-changed-setting` made alter statements own their class attributes, and `bug-recreated-device-comes-back-as-the-one-that-was-dropped` throws away *cached* plans when a device is dropped, which a plan the caller is holding is not.

Without a drop in between, the second execution fails anyway because the object already exists. So the reachable sequence is precisely: prepare a create statement, drop what it made, execute it again.

## Where the rule goes, and where it must not go

The public execution path for a held statement plan is `ServerStatementPlan.Execute` (`src/engine/DAE/Server/ServerPlan.cs:215`), and it is the **only** place this rule belongs. Checked by reading every other route into a DDL node:

- `RemoteServerStatementPlan.Execute` (`src/engine/Server/Server/RemoteServerPlan.cs:116`) delegates to it, so a client over the wire is covered by the same change.
- `ServerProcess.Execute(statement, params)` is prepare + execute + unprepare over the same method, so a statement run that way is covered too (and a plan handed back by the plan cache has already been executed once, so its next execution recompiles).
- Every *internal* DDL path builds its own `Plan` and `Program` and calls `Program.Execute` directly, never `ServerStatementPlan.Execute`: the catalog's lazy load (`CatalogDeviceSession.CreateDeviceTable`), an application transaction's copy of each table it joins and the teardown of those copies (`ApplicationTransaction.cs`, the `new Plan(process)` / `Compiler.Compile` / `program.Execute` blocks), and the disconnect of a session holding session objects (`ServerSession.DropSessionObjects`, `ServerSession.cs:253`). None of them needs recompiling and none of them must get it.

That last one is the trap. `ServerSession.DropSessionObjects` creates **one** `ServerStatementPlan` and then assigns `plan.Program.Code` and calls `plan.Program.Execute(null)` once per drop statement, with no source statement anywhere in sight. Putting the rule in `Program.Execute` would therefore break the disconnect of every session that holds session objects. Put it in `ServerStatementPlan.Execute`.

## The recompilation source: the parsed statement, not the text

Keep the parsed `Statement` the plan was compiled from on the `ServerPlan`, and recompile from that. Not from the statement text:

- A plan prepared through `ServerProcess.PrepareStatement` does have its exact text (`new SourceContext(statement, locator)`), but a plan prepared through `ServerBatch.PrepareStatement` (`src/engine/DAE/Server/ServerBatch.cs:137`) is given `_script.SourceContext`, whose `Script` is the **whole script**, not that batch's statement. Recompiling a batch plan from its source context would compile the entire script.
- `Program.SetSourceContext` keeps the script text only when no locator was supplied (`src/engine/DAE/Runtime/Program.cs:190`), so the text is not reliably retained at all, and `CachedPlanHeader.Statement` exists only for a plan the cache accepted.

Recompiling from the parsed statement is sound for the two things that look as though they might make a parsed statement single-use, both checked:

- **Object IDs.** `ObjectIDGenerator.Allocate(MetaData)` *removes* the `DAE.ObjectID` tag it honours (`src/engine/DAE/Schema/ObjectIDGenerator.cs:76`), so a second compilation of the same statement finds no tag and allocates a fresh identifier — which is what this ticket wants. The only path that supplies identifiers by tag is a catalog store load, and that path never goes through `ServerStatementPlan.Execute`.
- **Class definitions.** `bug-remembered-statement-applies-a-changed-setting` landed the invariant that a catalog object owns its class definition and never shares one with a statement (six cloning setters, plus `AlterNode.AlterClassDefinition`), so an alter in between cannot have written into the statement the plan holds. This is why recompiling from the parsed statement closes the measured reproduction rather than reproducing it.

One residue stays, and is **not** this ticket's to fix: a create statement still hands the object it builds its own `MetaData` object, so a tag an `alter … alter tags` wrote into that metadata comes back with the rebuilt object. All nine such sites are already an arm of `plan/bug-view-tags-lose-to-tags-inherited-from-the-source`; once that lands, recompiling from the parsed statement is exact. Do not change metadata sharing here.

## Why the rule covers alter and drop too

The narrow version of this rule — recompile only a plan that contains a `Create…Node` — would need a list of which node kinds hold compile-time catalog state. The history of this exact area says not to draw that list: K33 named three nodes, K42 named nine more and its review found a tenth that the implement stage had missed, K45 reopened an arm K42 had declined. A compiled drop holds a resolved object just as a compiled create holds a built one — `DropDeviceNode` keeps the `Schema.Device` the compiler resolved (`DDLNodes.cs:3423`) and executing it calls `StopDevice` and `DropDevice` on that instance, so a prepared drop plan executed again after the object was dropped and created afresh acts on an instance the catalog no longer holds.

`DDLNode` is already the engine's own name for this set, and its `InternalBeforeExecute` already relies on that ("Every statement that changes the catalog is a DDLNode, so invalidating here needs no per-statement discipline and no statement added later can be forgotten about"). Use the same seam: **the plan needs recompiling if its node tree contains a `DDLNode`.** Walk the tree once, on the first execution that needs the answer, and keep it; `PlanNode.Nodes` is the only recursion needed, and a DDL node nested inside a block, an `if` or a loop must count. There are no `DDLNode` subclasses outside `src/engine/DAE/Runtime.Instructions/` — the library and catalog operations in `src/engine/Server/Runtime.Instructions/` are `InstructionNode`s that read their arguments at run time and hold nothing from compilation — so the walk needs no knowledge of the server assembly.

The cost of recompiling a DDL plan is one compilation of one statement, paid only on the second and later executions of one plan, on a path that is already writing the catalog store.

## The plan cache key, which the owner asked about

The owner's reading was that the plan cache key is insufficient: a plan is reused by its statement text, but it also depends on catalog state the text does not capture. Here is what is there, and the answer.

The key is `CachedPlanHeader` (`src/engine/DAE/Server/PlanCache.cs:13`): statement text, the session's current library, a hash of the type names on the process context, and whether the process is in an application transaction. Validity is not part of the key — it is one monotonic counter, `Catalog.PlanCacheTimeStamp`, compared when the cache hands a plan out (`PlanCache.Get`): a plan compiled before the last bump is disposed instead of reused. The counter is bumped by hand, statement by statement, at 24 sites in `DDLNodes.cs` plus `CatalogDeviceSession.InvalidateCachesAfterUndo` for a rollback that undid DDL and the `System.UpdateTimeStamps()` operator; K33, K42 and K45 drew that list, and K42 records six statements deliberately left off it with a reason each.

**A better key, or a dependency check on reuse, cannot close this route.** A key identifies a plan's *inputs*. This defect is in an *output*: the `Schema` object the compiler built and the node kept, which the catalog then mutated and removed. No input changed — nothing a key could hash and nothing a dependency check could compare, because the plan depends on nothing that moved. The only two answers are to rebuild the object or to refuse the second execution, and the owner chose the first.

**The landed targeted invalidation is therefore neither redundant nor narrower, and nothing in it should be undone.** It guards a different route with a different stale thing: a plan the *cache* hands back, where what has gone stale is either the object the plan holds (the create face) or an answer the plan resolved while compiling — a device's reply about which nodes it can run, a scalar type's sort, the description a client is given about which proposals it may answer out of its own copy. That face is an input face, and a dependency-based validity check *would* be the principled replacement for the hand-drawn bump list. It is a larger change with no measured need, it would not have closed this ticket, and K42's register row already records which statements are off the list and why; so it is recorded here as the answer to the question and not filed as a ticket.

One thing to leave alone deliberately: **do not change what the cache admits.** A DDL plan returned to the cache will now be recompiled whenever it is reused, so its slot buys nothing — but refusing to cache DDL plans would also remove the premise of `DeviceClassAttributeRerunTest.TestStatementRunAgainFromThePlanCacheAppliesItsOwnValue`, which is the only case over what a client sees through the cache's path. Record the wasted slot as a `NOTE:` tripwire at the admission site (`ServerSession.AddCachedPlan`) naming "stop admitting plans that contain a DDL node" as the thing to reach for if plan-cache pressure is ever measured, and change nothing.

## Edge cases & interactions

Each of these must be handled, and the handoff must say how each was verified — by inspection, by a type, or by a test.

- **A failed first execution still consumes the compilation.** Mark the compilation used *before* executing, not after: a create statement that threw half-way may already have registered its object, and the next execution must not reuse that node. Verify by inspection at the call site.
- **A failed recompilation must execute nothing and must leave the plan in a usable state.** Compile into a fresh `Plan`/`Program` pair first and swap only on success, so a compilation error leaves the previous pair in place and surfaces to the caller the way a prepare-time error does. This is covered by a test (below), because the commonest way to reach it is re-executing a create plan whose object still exists.
- **The plan's identity must not move.** Give the new `Program` the existing plan's `ID` (`ServerPlan._iD`): a remote client holds a `PlanDescriptor` carrying it, `DebugLocator.ProgramLocator` is built from it, and `ServerExpressionPlan` names its plan catalog's table variable after it. Keep `Header` as it is — the text, library, context and application-transaction flag are unchanged — and re-stamp `PlanCacheTimeStamp` from `process.Catalog.PlanCacheTimeStamp` after a recompile so the cache's own staleness check stays truthful for the recompiled plan. Carry `Program.IsCached` across.
- **Disposal order.** Dispose the old `Plan` (and release the old `Program`) only after the new pair has compiled. `Plan` is cheap to construct and dispose (`Plan.cs:33`); the new `Program` needs `ShouldPushLocals = true` as `ServerPlan`'s constructor sets it, and `SetSourceContext` with the plan's own source context so a debugger locator still resolves.
- **Parameters.** Recompile with the `DataParams` passed to `Execute`, not a copy kept from prepare time: `ServerProcess.Compile` folds the process locals and the parameters into the compilation, and the execute-time parameters are what the execution will bind.
- **Compiling inside a transactional call.** The recompile happens inside `BeginTransactionalCall`/`EndTransactionalCall`. This is ordinary — every `ExecuteScript` and every library registration compiles inside a running program — but say in the handoff that it was checked rather than assumed.
- **Compilation writes into the statement it compiles.** `CompileCreateTableStatement` writes `DAE.IsDefaultRemotable`, `DAE.IsChangeRemotable` and `DAE.IsValidateRemotable` back into the statement's own `MetaData` (the three `MetaData.Tags.Update` calls in `Compiler.cs`). Check that a second compilation of the same statement converges — it recomputes the same value and writes it again — and say so. A create-table arm in the probing you do is the way to see it; it does not need a kept test if it converges.
- **Application transactions.** `CompileCreateTableStatement` calls `transaction.Device.AddTableMap` while compiling, when the process is in an application transaction and the statement carries a `DAE.SourceTableName`. A recompile inside an application transaction would run that again for the newly built table: check `AddTableMap` is guarded or idempotent, and if it is not, scope the recompile so this case is safe and say what you did.
- **A batch plan.** `ServerBatch.PrepareStatement` plans are the second route to a held statement plan; they recompile from the retained statement, which is why the retained statement and not the source context is the recompilation source. Confirm by inspection that a batch plan carries one.
- **A plan with compiler messages but no errors.** A warning-carrying plan is cached and executed today; after a recompile the caller reads the *new* compilation's `Messages`, `PlanStatistics` and `ProgramStatistics`. That is observable through `IServerPlan`; it belongs in the migration note rather than in a test.

## Tests

Project rule: a bug fix owes its reproduction, at the lowest layer that shows it. These need `IServerProcess.PrepareStatement` rather than `ExecuteScript`, no database server, and ran in under 100 ms when the reproduction was measured. Put them in a new fixture deriving from `DeviceClassAttributeFixture` (`tests/DAE.Server.Tests/`, suggested `PreparedPlanRecompileTest.cs`), which already has `MaxRowCount`, `Device`, `CatalogObject<T>` and `Script`.

- **The reproduction.** Prepare `create device … attributes { "MaxRowCount" = "100" }`, execute, `alter device … alter "MaxRowCount" = "200"`, `drop device`, execute the held plan again. Assert the live row limit is 100, that the create statement the catalog emits says 100, and that the re-created device's `ID` differs from the one the first execution registered — the identifier is what proves the object was built afresh rather than patched. Must be seen to fail against the unfixed engine.
- **The existing object.** Prepare a create statement, execute it, and execute it again *without* dropping. Assert the error names the duplicate object name, and that the process still works afterwards (a following statement executes). This pins both the failed-recompilation path and the error change the migration note records.
- **The drop arm, only if it is wrong today.** Prepare `drop device D`, execute it, create `D` again, execute the held plan again, and see what the unfixed engine does with an instance the catalog no longer holds (`CatalogDeviceSession.DropDevice` is `DeleteCatalogObject`, and `StopDevice` runs on the stale instance first). If it produces a wrong outcome, keep a case for it; if it already raises a clean error, report what you measured in the handoff and keep no case. Do not guess either way in writing.

Nothing else earns a test here: the tree walk, the identity plumbing and the disposal order are either what the two cases above exercise or what the compiler already checks.

## Documentation

- A new row in the plan's *Known defects* register (`docs/modernization-plan.md`, after K45) describing the defect, the rule, the measurement, and the answer to the cache-key question above in brief. Extend K42's row with one sentence pointing at the new row, replacing its "`bug-prepared-create-statement-executes-twice` owns that question".
- A row in `docs/migration-notes.md`, in the section its neighbours use for D4, catalog and plan behaviour: what an application notices is one recompilation when it executes a held statement plan a second time, a create statement that now builds what it says under a new object identifier, a different error when a create plan is re-executed while its object still exists (name both the old and the new error, measured), and statistics and messages that report the second compilation.
- **No architecture page text.** `docs/architecture/sessions-processes-plans.md` is the page that owns this subject and it is at 1,491 words of a 1,500-word limit, so a paragraph there would force a split that is far outside this ticket. The register row, the migration note and comments at the two code sites carry it. Run `python tools/docs-size-check/check_size.py` anyway if you touch any page under `docs/architecture/`.

## Tests to run

Per this project's implement-stage rule, the targeted set only, and name the filters in the handoff. Build first (`dotnet build Dataphor.slnx`); CI runs the full suite after the push.

- The new fixture, plus the fixtures over the files this changes and their neighbours: `DeviceClassAttributeRerunTest`, `DeviceClassAttributeRollbackTest`, `SchemaCacheInvalidationTest`, `ApplicationTransactionTest`, `LibraryLoadingTest`, `ConcurrentDDLTest`, `CatalogRollbackSnapshotTest`, and the client-stack fixtures that reach `RemoteServerStatementPlan` (`RemoteValidateTest`, `RemoteEventHandlerCacheTest`).
- The coverage scripts for the areas a DDL plan passes through: `PlanCache`, `Devices`, `Sorts`, `Constraints`, `EventHandlers`, `Tables`, `Types`, `LibraryCoverage`, `ApplicationTransactions`, `SessionTables`, `SessionAndProcessScopedTables`. Name the filters exactly (`--filter "Name=PlanCache"` and so on): short words such as `Types` and `DAE` also match fixture names and pull in most of a part.

## What this ticket does not do

- It does not touch `ServerExpressionPlan`. A held *expression* plan also re-opens and re-evaluates its compilation with no validity check of any kind, which is the same shape of defect at a different site and with a different answer available (recompiling under a client that has already read the plan's row type is not safe, so that one may have to raise instead). `backlog/bug-a-held-query-plan-keeps-answering-from-a-stale-catalog` owns it.
- It does not change the plan cache, its key, its admission or the timestamp bumps K33, K42 and K45 landed.
- It does not change metadata sharing between a create statement and the object it builds (`plan/bug-view-tags-lose-to-tags-inherited-from-the-source`).

## The two existing cases this change neutralizes

`DeviceClassAttributeRerunTest` has two cases, and this change takes the reproduction value out of both. The first already carries a `NOTE:` saying it stopped reaching a cached plan when its sibling landed. The second, `TestPreparedPlanExecutedAgainAppliesItsOwnValue`, re-executes a held `alter device` plan — which under this rule recompiles, so it will pass whether or not `AlterNode.AlterClassDefinition` clones the statement's attribute. Decide and record, as that ticket's review did:

- **Keep both and rewrite their comments** to say what they now cover (what a client sees when it runs one statement twice, through each of the two routes) and what they are no longer (a reproduction of the class-definition sharing defect). This is the expected answer, and it is the cheap one.
- **Or re-point the clone reproduction to a layer where it still reproduces**, if you judge that the clone invariant should keep an executable guard: a single compiled program that executes the same alter node more than once, which a loop in one script gives you (a `while` loop over create-100 / alter-200 / drop, two iterations — the second iteration applies 200 if the clone is reverted). Only keep such a case if you actually saw it fail with the clone reverted and pass with it restored; a case that passes either way is worse than no case.

Do not simply delete either case.

## TODO

- Read the two landed sibling tickets in `tickets/complete/` (`bug-remembered-statement-applies-a-changed-setting`, `bug-recreated-device-comes-back-as-the-one-that-was-dropped`) and K33, K42 and K45 in `docs/modernization-plan.md` before touching anything: they establish the invariants this ticket builds on.
- Reproduce first, as a failing test: the prepared create plan executed again after an alter and a drop, asserting the row limit, the emitted create statement and a new object identifier.
- Keep the parsed statement the plan was compiled from on `ServerPlan`, for both routes that build one (`ServerProcess.CompileStatement` and `ServerBatch.PrepareStatement`).
- Add the "this compilation has been used" mark on `ServerStatementPlan`, set before execution.
- Add the recompilation: a fresh `Plan` and `Program` compiled from the retained statement with the execute-time parameters, swapped in only on success, keeping the plan's ID, header, `IsCached` and source context, re-stamping `PlanCacheTimeStamp`, disposing the old pair afterwards. The compile itself needs a method on `ServerProcess`, which owns the private `Compile`.
- Add the predicate: the node tree contains a `DDLNode`, computed once and kept.
- Work through every item in *Edge cases & interactions* and say in the handoff how each was verified.
- Add the second test case (re-executing a create plan whose object still exists) and measure the error it raises before and after.
- Measure the drop arm; keep a case only if the unfixed engine produces a wrong outcome.
- Decide what happens to the two cases in `DeviceClassAttributeRerunTest` and record the decision in their comments.
- Add the `NOTE:` tripwire at `ServerSession.AddCachedPlan` about the slot a DDL plan now occupies for nothing.
- Add the register row, extend K42's row, add the migration-notes row.
- Build, run the targeted tests named above, and name the filters in the handoff.
