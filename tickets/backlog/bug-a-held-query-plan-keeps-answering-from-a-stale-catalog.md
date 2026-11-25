description: A program can ask the server to compile a query once and then run it as often as it likes. If the database definition changes in between — a new ordering for a type, a new rule on a table — the program keeps getting answers worked out from the definition as it was, with no error and no sign that anything is out of date.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerPlan.cs, src/engine/DAE/Server/PlanCache.cs, src/engine/DAE/Server/ServerProcess.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: The only safe answer may be to refuse the stale plan rather than silently recompile it, because a caller that has already been told the shape of the result cannot have that shape change underneath it — and refusing takes away a call that works today; the shipped client stack also has its own way of noticing a catalog change, so the callers left exposed are the ones inside the server process.
----
## What is wrong

The engine keeps compiled plans in a memo shared by every session, and checks before handing one back whether the catalog has changed since it was compiled: `PlanCache.Get` compares the plan's stamp with `Catalog.PlanCacheTimeStamp` and throws the plan away if the catalog has moved on (`src/engine/DAE/Server/PlanCache.cs`). That check happens **only** when the memo hands a plan out. Once a caller is holding a plan, nothing checks it again.

A caller gets one through `IServerProcess.PrepareExpression` and may then `Open` a cursor, close it, `Open` another, or `Evaluate`, as often as it likes — the published interface in `src/engine/DAE/CLI.cs` offers exactly that. Neither `ServerExpressionPlan.Open` nor `ServerExpressionPlan.Evaluate` consults the stamp (`src/engine/DAE/Server/ServerPlan.cs`); both only check that the plan compiled. So every answer after the first comes out of the compilation, however much of the catalog it captured has since been replaced.

What a compilation captures is not small. The known-defects register records three kinds already, each measured for the memo's route: a scalar type's ordering is resolved into the plan's order and key columns, so a plan compiled before `create sort` keeps ordering by the ordering it replaced (K42); the reply a storage device gives about which parts of the plan it can run, and the device's translation of them, are kept on the nodes (K42); and the description a client is handed about which proposals it may answer out of its own copy of the schema is built once, when the plan is compiled (K45). The fixes for all three work by advancing the stamp — which closes the memo's route and leaves the held plan's route untouched.

## How it would be confirmed

The reproduction the register already has for the memo, re-run against a plan the caller holds instead of one the memo hands back: create a scalar type, a table over it and three rows; prepare `select T order by { N }`; open a cursor and read the first row; close it; `create sort` on the type comparing the other way round; open a second cursor from the **same** plan and read the first row again. The memo's version of this is a case in `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs` (`CreateSortInvalidatesCachedPlans`) and it was measured on 2026-11-23; this is the same sequence with one held plan in place of two prepares. Nothing here needs a database server.

Read from the code, not run: the engine's own callers never re-open a held plan (every internal path compiles, runs and disposes), so nothing in this repository's tests or coverage scripts reaches it today, which is also why no test will tell us if it gets worse.

## Why it is filed separately from the statement side

`bug-prepared-create-statement-executes-twice` closes the same shape of defect for a held **statement** plan, and does it by compiling the statement afresh before a second execution. That answer is safe there because a statement has no result whose shape a caller has already been told. It is not obviously safe here: a caller that has read the plan's `DataType`, `TableVar`, `Capabilities` or `Order` — which is what a client does, and what a row buffer is sized from — cannot have those change underneath it, so recompiling a held query plan may have to be refused rather than performed. That is a decision about what the interface promises, not an implementation detail, and it is why this is its own ticket rather than an arm on that one.

Whoever picks this up should also price doing nothing. The shipped client stack has a second mechanism for exactly this problem: `Catalog.CacheTimeStamp`, which a client compares against the value its plan was prepared at so that a plan which is behind can be recognised on the client side (K45 describes it, and `bug-remote-form-is-never-told-about-a-type-change` taught the three proposal calls to consult it). So a form in the Windows client is not the exposed case. What is exposed is a caller inside the server process — the Frontend library's server side, the script runner, a custom C# extension, a test harness — that holds one plan across a definition change.
