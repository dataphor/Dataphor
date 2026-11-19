description: Review the fix that makes the server's plan cache evict stale compiled plans after create conversion, drop conversion and create operator.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, docs/modernization-plan.md
repro: verified
----
## What changed

Three DDL execute nodes now call `program.Catalog.UpdatePlanCacheTimeStamp()` right after the catalog change, so the server's plan cache (keyed by statement text, current library, context and application-transaction flag) evicts every cached plan compiled before the DDL. Without the bump, a session that had already compiled a statement kept getting the old plan after a `create conversion`, `drop conversion` or `create operator`, so a repeated statement still applied a dropped conversion, took the old longer conversion path when a shorter one had been created, or called the old overload when a better one had been created. The underlying schema memos (the conversion path cache and the operator resolution cache) were already fixed by `bug-schema-caches-cleared-before-ddl-executes`; this ticket flushes the plan cache that sits above them.

Three `InternalExecute` methods in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs`:

- `CreateConversionNode` (~305): bump after `CreateConversion`.
- `DropConversionNode` (~319): bump after `DropConversion`, inside the loop, before `break`, under the catalog lock the node already holds (`UpdatePlanCacheTimeStamp` re-takes it; the catalog's lock is re-entrant).
- `CreateOperatorNode` (~637): bump after `CreateOperator`.

One of the bumps carries a tripwire `NOTE:` so a future reader meets it at the site: the bump flushes every cached plan; measured as no slower for library registration (template step 2.3 s with the fix, 2.8–3.3 s without, both inside run-to-run noise); if a workload creating operators while serving repeated statements shows plan-cache churn, invalidate only plans that resolved this operator name or traced a path through these types.

Not placed in the `CatalogDeviceSession` methods (`AddImplicitConversion`, `RemoveImplicitConversion`, `CreateOperator`) because `ServerCatalogDeviceSession.ResolveCatalogObject` also calls them during ordinary lazy warm-up of every operator and conversion, and a load cannot invalidate an already-compiled plan.

## Validation

- `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~SchemaCacheInvalidationTest"` — 5/5 pass, including the three reproductions this ticket adds behaviour for: `DropConversionInvalidatesCachedPlans`, `CreateConversionInvalidatesCachedPlans`, `CreateOperatorInvalidatesCachedPlans`. The other two in the fixture are from the sibling ticket and still pass.
- `dotnet build Dataphor.slnx` — green, 0 warnings, 0 errors.
- Full suite in three parts (per `AGENTS.md`):
  - Part 1 (non-coverage): 57 passed / 0 failed / 0 skipped in 2.5 min.
  - Part 2 (coverage scripts A–R): 67 passed, 11 matched known-failures, 0 mismatches in 3.2 min.
  - Part 3 (coverage scripts non-A–R): 48 passed, 10 matched known-failures, **1 mismatch** on `CoverageScripts.ServerLink` (HarnessTimeout instead of SocketException), 13 skipped in 5.7 min. This mismatch is the machine-level loopback-connect issue already documented in `tickets/.pre-existing-known.md` for 2026-11-18 — unrelated to this change.

## What a reviewer should check

- The three bumps are after the catalog-changing call, not before. A plan compiled between a pre-change bump and the actual change would otherwise survive.
- No other DDL nodes that can change operator resolution or conversion paths are missing a bump. Table/view drops, operator drops, alter nodes, reference creates and the rollback invalidation path were already covered; this ticket fills the three listed gaps and no others were found in a reread of `DDLNodes.cs`.
- The `CreateOperatorNode` path is shared by `create operator`, `create aggregate operator`, the operators that `create type` generates and the `alter type` paths that re-create selectors and comparers. Those alter paths may bump several times per statement — intended, harmless.
- `UpdatePlanCacheTimeStamp` takes `Catalog`'s lock itself; `DropConversionNode` holds the same lock and relies on re-entrancy.

## Known gaps / notes

- The tripwire `NOTE:` names the invalidation as whole-cache. If later someone wants per-name invalidation, the call site is the natural place to narrow it.
- `K32`'s row now points to `K33` for the plan-cache arm; `K33` was added to `docs/modernization-plan.md`. Verify the table parses correctly and the row cites the three nodes.
- The `SchemaCacheInvalidationTest` `<remarks>` paragraph was updated: the first two tests use different text after the DDL (window: compile-to-execute), the three `...InvalidatesCachedPlans` tests use identical text (window: plan cache).
- Tests added, in `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs`, each the lowest-layer reproduction of the specific defect: `DropConversionInvalidatesCachedPlans` (dropped A→B conversion must stop being applied), `CreateConversionInvalidatesCachedPlans` (new direct A→C must replace a longer A→B→C), `CreateOperatorInvalidatesCachedPlans` (a new better overload must be chosen). They were authored by the sibling fix ticket and pinned here with the correct behaviour.
- No migration-note entry: an application can only notice that a repeated statement now behaves as the same statement compiled fresh would — not a visible change in contract.
## End
