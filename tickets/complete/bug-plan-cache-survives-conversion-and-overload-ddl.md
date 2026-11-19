description: The server's plan cache now evicts stale compiled plans after create conversion, drop conversion and create operator.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, docs/modernization-plan.md
repro: verified
----
## What landed

`CreateConversionNode`, `DropConversionNode` and `CreateOperatorNode` in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs` each call `program.Catalog.UpdatePlanCacheTimeStamp()` right after the catalog-changing call. The plan cache in `src/engine/DAE/Server/PlanCache.cs` compares a cached plan's stored timestamp against `Catalog.PlanCacheTimeStamp` at fetch (`PlanCache.cs:120`) and evicts any plan older than the current value, so the bump drops every plan compiled before the DDL. The matching schema caches below (K31 conversion-path memo, K3 operator-resolution memo) were already invalidated by `bug-schema-caches-cleared-before-ddl-executes`; this change plugs the plan cache that sat above them.

Three reproduction tests in `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs` — `DropConversionInvalidatesCachedPlans`, `CreateConversionInvalidatesCachedPlans`, `CreateOperatorInvalidatesCachedPlans` — evaluate the identical statement text before and after the DDL, which is the window the plan cache owns. The fixture's older two tests still cover the compile-to-execute window.

A tripwire `NOTE:` at the first of the three bumps covers the whole class: the bump flushes every cached plan; if a workload mixing these DDL statements with repeated statements shows plan-cache churn, the mitigation is to invalidate only plans that traced a path through the affected types or resolved the affected operator name. Review reworded it from the operator-centric wording that landed (see finding below).

K33 was added to the known-defects table in `docs/modernization-plan.md`; K32's last sentence now cites K33 for the plan-cache arm.

## Review findings

### Fixed inline

- The tripwire `NOTE:` at `CreateConversionNode.InternalExecute` (`DDLNodes.cs:308`) conditioned plan-cache churn on "a workload creating operators", but it is placed on the conversion node and is meant as the single tripwire for all three bumps in the class. Reworded to name conversions and operators together, and to say the NOTE also covers the sibling bumps in `DropConversionNode` and `CreateOperatorNode`, so a reader meeting it at the conversion site is not misled.

### Checked, no finding

- **Each bump is after the catalog change, not before.** `CreateConversionNode` bumps after `CreateConversion`; `DropConversionNode` bumps after `DropConversion` inside the matching-conversion loop, before `break`; `CreateOperatorNode` bumps after `CreateOperator`. A plan compiled between a pre-change bump and the actual change would otherwise survive — the pre-change version of the bug this ticket fixes.
- **No other DDL nodes with this gap.** Reread `DDLNodes.cs`. `DropOperatorNode` (`DDLNodes.cs:3107`), `AlterOperatorNode` (`DDLNodes.cs:2384`), `AlterAggregateOperatorNode` (`DDLNodes.cs:2495`), `AlterTypeNode`, `DropTableNode`/`DropViewNode` (`DDLNodes.cs:1716`, `1789`), reference and key nodes (`DDLNodes.cs:2308`, `2618`) and the rollback path already bump the plan cache timestamp. The three nodes this ticket touches were the only ones missing a bump.
- **Re-entrancy under `DropConversionNode`'s catalog lock.** The node holds `lock (program.Catalog)` and `UpdatePlanCacheTimeStamp` itself takes `lock (this)` on the same catalog (`Catalog.cs:129`). .NET `Monitor` is re-entrant, so the nested acquire returns immediately. Confirmed by the test running through without hang.
- **`CreateOperatorNode` is also invoked from generated-operator paths** — `CreateTypeNode`, `AlterTypeNode`'s selectors/comparers (`DDLNodes.cs:1986`, `1990`, `2016`, `2036-37`, `2089-93`, `2127`, `2176-81`, `2197-2202`, `2223`, `2289`). Those paths now bump the plan cache once per generated operator, which is intended: the generated operator is a real operator a repeated statement can resolve to.
- **`ShouldAffectDerivationTimeStamp` gating on the pre-existing bumps is not a new issue.** `DropOperatorNode` and the `Alter*OperatorNode`s skip both the derivation-cache and the plan-cache bump when `ShouldAffectDerivationTimeStamp` is false — which turns on during server bootstrap, catalog device session lazy loads, and application-transaction and server-transaction plan preparation. The three new bumps are unconditional, which is what the ticket intends: conversions never route through those contexts (only the `CatalogDeviceSession.AddImplicitConversion`/`RemoveImplicitConversion` warm-up paths do, and they are deliberately not changed), and the new `CreateOperatorNode` bump is wanted even inside AT shadow-operator paths because the shadow operator still participates in resolution. If the pre-existing gating on the drop/alter operator nodes is itself a defect, it predates this ticket and is not in scope.
- **Not placed inside `CatalogDeviceSession` methods.** `ServerCatalogDeviceSession.ResolveCatalogObject` calls `AddImplicitConversion`, `RemoveImplicitConversion` and `CreateOperator` during ordinary lazy warm-up of existing catalog objects. A load cannot invalidate an already-compiled plan, so placing the bump at the node is right.
- **Docs are accurate.** K33 cites the three nodes and matches the bump placement; K32's trailing sentence now hands off to K33 for the plan-cache arm; `SchemaCacheInvalidationTest`'s `<remarks>` paragraph explains which tests measure which window.
- **Tests pay for themselves.** Each new test reproduces a specific user-visible defect this ticket fixes (dropped conversion still applied, longer path still taken when a shorter one exists, worse overload still called when a better one exists) at the lowest layer that reproduces it (the in-process engine harness). No test restates implementation or verifies a mock.
- **Source hygiene preserved.** `DDLNodes.cs` is UTF-8 without BOM; `SchemaCacheInvalidationTest.cs` is UTF-8 with BOM; both files remain CRLF. The edits do not re-encode or reformat.
- **Migration-note entry is correctly absent.** An application can only notice that a repeated statement now behaves as the same statement compiled fresh would — not a visible change in contract.

### Open items

None.

## Validation

- `dotnet build Dataphor.slnx` — green, 0 errors.
- `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~SchemaCacheInvalidationTest"` — 5/5 pass, including the three `...InvalidatesCachedPlans` tests this ticket adds behaviour for, run both before and after the inline NOTE rewording.
- Part 1 of the suite (`NUnit.Where=class != Alphora.Dataphor.DAE.Server.Tests.CoverageScripts`) — 57 passed / 0 failed / 0 skipped in 2 m 35 s, plus `Common.Tests` 13/13 in 1 s.
- Parts 2 and 3 were run by the implementer (handoff ticket): 67 passed + 11 matched known-failures in part 2; 48 passed + 10 matched known-failures + 1 known pre-existing machine-level `ServerLink` HarnessTimeout in part 3. The `ServerLink` mismatch is recorded as a one-off in `tickets/.pre-existing-known.md` for 2026-11-18 and is unrelated to this change.
## End
