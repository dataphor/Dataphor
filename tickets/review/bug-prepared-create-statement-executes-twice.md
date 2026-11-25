description: A program can ask the server to compile a statement once and then run it as many times as it likes. For a statement that creates something, the second run put back the very thing the first run made — carrying any change made to it since — instead of making what the statement describes. Such a statement is now compiled again before each run after the first.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerPlan.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/PreparedPlanRecompileTest.cs, tests/DAE.Server.Tests/DeviceClassAttributeRerunTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/modernization-plan.md, docs/migration-notes.md
difficulty: hard
repro: verified
----
## What landed

**The rule: a compiled plan containing a statement that changes the catalog is used for at most one execution; executing it again compiles its statement afresh first, and executes that.**

Four code changes, one new test fixture, three changed tests, two documentation entries.

**The predicate** — `DDLNode.ContainsDDL(PlanNode)` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:31`), a static on `DDLNode` itself because that class is already the engine's name for "statements that change the catalog" and `DDLNode.InternalBeforeExecute` already relies on that. Recursive over `PlanNode.NodeCount`/`Nodes`, so a statement nested in a block, an `if` or a loop counts. It uses `NodeCount` rather than `Nodes.Count` because the `Nodes` getter allocates the collection for a node that has none.

**The rule's site** — `ServerStatementPlan.Execute` (`src/engine/DAE/Server/ServerPlan.cs:277`) and nowhere else, with two new private fields on `ServerStatementPlan`: `_compilationExecuted` and the once-computed `_needsRecompilation`. The recompilation is asked for only when the plan has already been executed once, and the mark is set **before** `_program.Execute`, not after.

**The recompilation** — `ServerProcess.RecompileStatementPlan` (`src/engine/DAE/Server/ServerProcess.cs:893`), beside the private `Compile` it needs. It builds a fresh `Plan` and a fresh `Program` carrying the plan's existing `ID` and `IsCached` and `ShouldPushLocals = true`, compiles the retained statement with the execute-time parameters and the plan's source context, calls `plan.CheckCompiled()` — because `Compiler.Compile` collects its errors into `Plan.Messages` rather than throwing them — and only then calls `ServerPlan.SwapCompilation`, which installs the new pair and disposes the old `Plan`. On any failure the new `Plan` is disposed and the plan keeps the compilation it had. After a successful swap the plan's `PlanCacheTimeStamp` is re-stamped from `Catalog.PlanCacheTimeStamp`.

**What the plan keeps** — two new properties on `ServerPlan`: `SourceStatement` (the parsed statement) and `SourceContext`. Both are set in `CompileStatement` and `CompileExpression`, which covers both routes that build a statement plan (`ServerProcess.PrepareStatement` and `ServerBatch.PrepareStatement` both pass through `CompileStatement`). The two cached-plan branches of `PrepareStatement` and `PrepareExpression` now record the source context they hand the program, so a recompilation uses the context the plan was last prepared with rather than the one it was first compiled with.

**The tripwire** — a `NOTE:` at `ServerSession.AddCachedPlan` (`src/engine/DAE/Server/ServerSession.cs:149`): a DDL plan returned to the cache is now recompiled whenever it is reused, so its slot buys nothing; admission is deliberately unchanged because `DeviceClassAttributeRerunTest`'s plan-cache case is the only case over what a client sees through that path, and "stop admitting plans whose node tree contains a DDL node" is named as the thing to reach for if cache pressure is ever measured.

**Documentation** — `K46` in the plan's *Known defects* register, carrying the measurements and the answer to the owner's plan-cache-key question; K42's pointer sentence rewritten to name K46 and to say in one sentence why a key could not have been the answer. One `done` row in the "D4, catalog and libraries" section of `docs/migration-notes.md`. No architecture page text: `docs/architecture/sessions-processes-plans.md` owns the subject and is at 1,491 of 1,500 words, so a paragraph there would force a split. `python tools/docs-size-check/check_size.py` — 13 files, 0 over the limit.

## One place the implement ticket's expectation was wrong, and the handling changed because of it

The ticket predicted that `DeviceClassAttributeRerunTest.TestPreparedPlanExecutedAgainAppliesItsOwnValue` would stop reproducing the class-definition-sharing defect once this change landed, and offered "keep both and rewrite their comments" as the expected answer.

**Measured, it still reproduces it.** With `AlterNode.AlterClassDefinition`'s `attributeDefinition.Clone()` reverted and this change in place, that case is red (`Expected: 100 / But was: 200`). The reason is that the recompilation source is the **parsed statement**, and the clone defect is precisely an alteration writing its new value *into that statement*: the second compilation then reads 200 out of the statement and applies 200. The implement ticket's own reasoning — "the clone invariant means an alter in between cannot have written into the statement the plan holds" — is what makes the recompilation correct, and is also why that case keeps its reproduction value when the invariant is reverted.

So what landed is both halves of the ticket's choice:

- Both existing cases **kept**, with their comments rewritten to say what each covers now. The first (`TestStatementRunAgainFromThePlanCacheAppliesItsOwnValue`) is no longer a reproduction of anything on its own and says so, as it already did.
- One case **added** at the layer the ticket named: `TestOneProgramRunningTheSameAlterTwiceAppliesItsOwnValue`, a `while` loop over create-100 / alter-200 / drop-attribute inside one compiled block, which is a single program executing one node twice and so is reached by neither recompilation nor cache eviction. Measured both ways: red with the clone reverted, green with it in place.

**The judgement a reviewer should weigh:** there are now two executable guards for the clone invariant where the ticket expected zero or one. The prepared-plan case's reproduction value is *coupled to this ticket's choice of recompilation source* — change it to the statement text and that case goes green with the clone reverted. The loop case is uncoupled. That is the argument for keeping both; the argument against is one test per behaviour. Cut the loop case if you disagree, but then say in the review notes that the clone invariant's only guard depends on the recompilation reading the parsed statement.

## Tests

New fixture `tests/DAE.Server.Tests/PreparedPlanRecompileTest.cs`, on `DeviceClassAttributeFixture`; the whole fixture runs in about 90 ms.

| Case | What it pins | Seen to fail without the fix? |
|---|---|---|
| `TestPreparedCreateExecutedAgainBuildsWhatItSays` | The reproduction: prepare a `create device` with `MaxRowCount = 100`, execute, alter to 200, drop, execute the held plan again. Asserts the live row limit is 100, the emitted create statement says 100, and the re-created device's object identifier differs from the first execution's — the identifier is what proves the object was built afresh rather than put back. | Yes: `Expected: 100 / But was: 200` against the unfixed engine, and again with the recompilation call disabled after the fix |
| `TestPreparedDropExecutedAgainDropsWhatIsThereNow` | The drop arm. Prepare `drop device D`, execute, create `D` afresh, execute the held plan again, then create `D` once more. | Yes, and the unfixed outcome is worse than the create arm's: the second execution raised nothing, took the re-created device out of the running catalog, and left its row in the catalog store, so the third create failed with "An object named "General.DropArmDevice" is already defined." Catalog and store disagreed from then on |
| `TestPreparedCreateExecutedAgainOverAnExistingObjectFails` | The refused recompilation: re-execute a held create plan while its object still exists. Asserts the error carries `CompilerException.Codes.CreatingDuplicateObjectName` (105214) and that the process still executes a following statement. | Not as a defect — it fails before and after. It pins the error *change* the migration note records: the code assertion is red against the unfixed engine, which raised a `RuntimeException` wrapping schema error 113106 |

Changed in `DeviceClassAttributeRerunTest`: one case added (above), two comment blocks rewritten, no assertions touched.

**No test for** the tree walk, the identity plumbing, the disposal order or the source-context retention: each is either exercised by the three cases above or is what the compiler checks. Nothing mocks anything.

## Every item of *Edge cases & interactions*, and how it was verified

- **A failed first execution still consumes the compilation** — by inspection at the call site: `_compilationExecuted = true` is the statement before `_program.Execute(paramsValue)`, inside the same `try`.
- **A failed recompilation executes nothing and leaves the plan usable** — by test (`TestPreparedCreateExecutedAgainOverAnExistingObjectFails`, whose second half executes a further statement on the same process) and by inspection: the swap is the last thing `RecompileStatementPlan` does before the stamp, and `plan.CheckCompiled()` precedes it because `Compiler.Compile` collects errors instead of throwing.
- **The plan's identity does not move** — by inspection: `new Program(this, serverPlan.ID)`, and `SwapCompilation` touches neither `ServerPlan._iD` nor `Header`. Also checked that nothing *stores* a reference to `ServerPlan.Plan` across calls: every read of `.Plan.PlanCatalog` in `RemoteServerPlan.cs` is on `ServerExpressionPlan`, read through the property each time, and expression plans never recompile; statement plans expose only `PlanStatistics` and `Messages`, both properties.
- **Disposal order** — by inspection: `SwapCompilation` assigns the new pair and then disposes the old `Plan`, so a throw from `Dispose` leaves the plan holding the new pair. `Program` is not `IDisposable`, so the old program is released by dropping the reference. `ShouldPushLocals = true` and `IsCached` are carried; the source context reaches the new program through `Compile`, which calls `program.SetSourceContext`.
- **Parameters** — by inspection: `RecompileStatementPlan` takes the `DataParams` that `Execute` was given and passes them to `Compile`, which folds them in with the process locals. Nothing is kept from prepare time.
- **Compiling inside a transactional call** — checked, not assumed: the recompilation runs between `BeginTransactionalCall` and `EndTransactionalCall`, which is the same position every `ExecuteScript` and every library registration compiles in. The three new cases all run through it.
- **Compilation writes into the statement it compiles** — measured, not reasoned about alone. A throwaway probe prepared `create table ProbeTable { ID : Integer, Amount : Integer, key { ID } } tags { DAE.IsDefaultRemotable = "False", DAE.IsChangeRemotable = "True" }`, executed it, printed the statement's own `MetaData.Tags`, dropped the table and executed the held plan again: the statement's tags are byte-identical after both compilations (`DAE.IsDefaultRemotable=False DAE.IsChangeRemotable=True`), the table built both times carries the same tags, and the object identifier moved (2222 → 2236). Reading the code says why it must converge: each of the three `MetaData.Tags.Update` calls in `Compiler.cs` is guarded by `!(computed ^ tagValue)`, so it only ever writes back the value the tag already holds. The probe was deleted; it earns no kept test.
- **Application transactions** — `ApplicationTransaction.AddTableMap` is **not** idempotent: a second call for the same source table name takes the `else` branch and assigns `DeletedTableVar` instead of adding a map. No scoping was needed, for a reachability reason: `CompileCreateTableStatement` calls `CheckValidCatalogObjectName` *before* `AddTableMap`, so a recompilation only gets that far when the table name is free — the table the first execution created has been dropped. And whatever `AddTableMap` then does is exactly what compiling the same statement text afresh does, which is what `ServerProcess.Execute` has always done and what the plan cache's own eviction already makes happen for this sequence. Verified by inspection of `Compiler.cs:4204`–`4230` and `ApplicationTransaction.cs:1384`; not measured, because reaching it needs a hand-written `create table` carrying the engine's internal `DAE.SourceTableName` tag inside an application transaction, executed twice from a held plan.
- **A batch plan carries a statement** — by inspection: `ServerBatch.PrepareStatement` calls `_script.Process.CompileStatement(_batch, …)` and `_batch` is the parsed `Statement` for that batch (`ServerBatch.cs:39`), so `SourceStatement` is that statement and not the script. This is the whole reason the recompilation source is the parsed statement: `_script.SourceContext.Script` is the *entire script*, so recompiling a batch plan from its text would compile all of it. `ServerBatch.Execute` prepares and unprepares around each execution, so no script path executes one compilation twice.
- **A plan with compiler messages but no errors** — in the migration note rather than in a test, as directed. Checked additionally that nothing is lost by not carrying the parser messages over: `ParserMessages.HasErrors()` is `Count > 0` and `CompilerMessages.HasErrors` counts a non-compiler exception as an error, so any parser message makes `CheckCompiled` throw and such a plan never executes.

Also checked, as the ticket required, that the rule is in the one right place: `RemoteServerStatementPlan.Execute` delegates to `ServerStatementPlan.Execute` (so a client over the wire is covered), `ServerProcess.Execute` is prepare + execute + unprepare over it (so a cached plan handed back, which has already been executed once, recompiles on its next execution), and `ServerSession.DropSessionObjects` assigns `plan.Program.Code` and calls `plan.Program.Execute(null)` once per drop statement with no statement anywhere — it never calls `ServerStatementPlan.Execute`, and `NeedsRecompilation()` would answer false for it anyway because `SourceStatement` is null.

## What a reader should be adversarial about

- **The two-guards decision above.** It is the one place this implementation departs from what the ticket predicted, and the departure is based on a mutation measurement that is worth repeating.
- **`SourceStatement` is set for expression plans too** (`CompileExpression`), where nothing reads it. Deliberate — the property is on the shared `ServerPlan` base, `SourceContext` has to be there for the two cached-plan branches, and `bug-a-held-query-plan-keeps-answering-from-a-stale-catalog` will want the statement — but it is dead weight today and a reasonable thing to cut.
- **`SourceStatement` and `SourceContext` are public settable properties on `ServerPlan`.** That is new public surface on a type a custom extension can see; it is additive, so the migration note does not mention it. If you would rather they were `internal` or `protected internal`, nothing outside `Alphora.Dataphor.DAE` sets them today.
- **The recompilation is unconditional for a DDL plan, including inside an open transaction.** A rollback that undoes the first execution's DDL does not clear `_compilationExecuted`, so the next execution recompiles anyway. That is one wasted compilation, never a wrong answer — but it is reasoning, not a measurement, and no test covers a prepared DDL plan executed twice inside one transaction with a rollback between.
- **What `PlanCacheTimeStamp` re-stamping buys.** It is re-stamped after a successful swap, and then the execution's own `DDLNode` bump makes the plan stale again, so in practice the plan is evicted on its next cache hit either way. The re-stamp is there so the field does not lie about what the plan holds; it is not load-bearing.

## Commands run

Build: `dotnet build Dataphor.slnx` — 0 errors. (Warning counts are not comparable across the incremental builds done during the ticket; the first full build of the branch reported 144 and no new warning code appeared in any later build.)

Targeted tests, per this project's implement-stage rule — the fixtures over the changed files and their neighbours:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~PreparedPlanRecompileTest|FullyQualifiedName~DeviceClassAttributeRerunTest|FullyQualifiedName~DeviceClassAttributeRollbackTest|FullyQualifiedName~SchemaCacheInvalidationTest|FullyQualifiedName~ApplicationTransactionTest|FullyQualifiedName~LibraryLoadingTest|FullyQualifiedName~ConcurrentDDLTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~RemoteValidateTest|FullyQualifiedName~RemoteEventHandlerCacheTest"
```

**141 passed, 0 failed, 0 skipped, 1 m 25 s.**

The coverage scripts for the engine areas a DDL plan passes through, with the filters named exactly so they match scripts and not fixtures:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=PlanCache|Name=Devices|Name=Sorts|Name=Constraints|Name=EventHandlers|Name=Tables|Name=Types|Name=LibraryCoverage|Name=ApplicationTransactions|Name=SessionTables|Name=SessionAndProcessScopedTables"
```

**11 passed, 0 failed, 0 skipped, 26 s** — exactly the eleven scripts, no fixture over-match.

Mutation runs used to prove the cases, each a build plus one filtered run:

- `attributeDefinition.Clone()` → `attributeDefinition` at `DDLNodes.cs:1138`: `TestOneProgramRunningTheSameAlterTwiceAppliesItsOwnValue` and `TestPreparedPlanExecutedAgainAppliesItsOwnValue` red, `TestStatementRunAgainFromThePlanCacheAppliesItsOwnValue` green. Restored.
- The `if (_compilationExecuted && NeedsRecompilation())` guard forced false: `TestPreparedCreateExecutedAgainBuildsWhatItSays` and `TestPreparedDropExecutedAgainDropsWhatIsThereNow` red, all three `DeviceClassAttributeRerunTest` cases green. Restored.

No `tickets/.pre-existing-error.md` was written: nothing failed anywhere. No full three-part run — the change does not touch the build or test infrastructure, the lexer, parser or compiler, so per this project's stage rule CI runs the whole suite after the push.

Nothing was left in `tickets/.logs/` and no artifact file was written: every run was foreground with no redirection.

## Not done here, deliberately

- `ServerExpressionPlan` is untouched. A held *expression* plan re-opens and re-evaluates its compilation with no validity check of any kind; `backlog/bug-a-held-query-plan-keeps-answering-from-a-stale-catalog` owns it, and the answer there may have to be to raise rather than recompile, because recompiling under a client that has already read the plan's row type is not safe.
- The plan cache, its key, its admission and the timestamp bumps K33, K42 and K45 landed are unchanged.
- Metadata sharing between a create statement and the object it builds is unchanged: a tag an `alter … alter tags` wrote into the object's metadata still comes back with the rebuilt object, because all nine create sites hand the object the statement's own `MetaData`. That is an arm of `plan/bug-view-tags-lose-to-tags-inherited-from-the-source`; once it lands, recompiling from the parsed statement is exact.
