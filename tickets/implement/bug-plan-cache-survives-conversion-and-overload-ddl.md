description: After a conversion is dropped or created, or a better-matching operator overload is created, the server keeps reusing already-compiled copies of statements it has run before, so a repeated statement still applies the dropped conversion, takes the old conversion route, or calls the old overload.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, docs/modernization-plan.md
repro: verified
----
## What is wrong

The server keeps a plan cache (`src/engine/DAE/Server/PlanCache.cs`): compiled plans keyed by statement text (plus current library, context and application-transaction flag), shared by every session. `PlanCache.Get` discards a cached plan when `Catalog.PlanCacheTimeStamp` is newer than the stamp the plan was cached with. DDL that can change how an already-compiled statement binds bumps that timestamp (`Catalog.UpdatePlanCacheTimeStamp()`): `DropOperatorNode`, `DropScalarTypeNode`, table and view drops, several alter nodes in `DDLNodes.cs`, `CreateReferenceNode`, and the rollback path (`CatalogDeviceSession.InvalidateCachesAfterUndo`). Three nodes that change operator resolution or conversion paths do not:

- `CreateConversionNode.InternalExecute` (`DDLNodes.cs` ~305): a cached plan keeps a longer conversion path that a new, direct conversion replaces.
- `DropConversionNode.InternalExecute` (~314): a cached plan keeps applying the dropped conversion.
- `CreateOperatorNode.InternalExecute` (~632): a cached plan keeps calling a worse overload than the one just created.

`bug-schema-caches-cleared-before-ddl-executes` fixed the two schema memos underneath (the conversion path cache and the operator resolution cache) so that a fresh compile gets the right answer; the plan cache sits above them and skips the compile entirely when the text repeats.

## Reproduction (verified, in the tree)

Three tests at the end of `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs` evaluate the identical statement text on a fresh session before and after the DDL. All three fail today:

- `DropConversionInvalidatesCachedPlans`: types A, B, conversion A→B, `Takes(const X : B)`; `Takes(A(5))` returns 5; after `drop conversion A to B` the same text still returns 5 instead of failing with operator-not-found.
- `CreateConversionInvalidatesCachedPlans`: conversions A→B (+100) and B→C (+1000), `Takes(const X : C)`; `Takes(A(5))` returns 1105; after `create conversion A to C` (adds nothing) the same text still returns 1105 instead of 5. This was the static arm of the fix ticket; it is now observed.
- `CreateOperatorInvalidatesCachedPlans`: `Op(const X : Decimal)`; `Op(5)` returns `'Decimal'`; after `create operator Op(const X : Integer)` the same text still returns `'Decimal'` instead of `'Integer'`.

```
dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~SchemaCacheInvalidationTest"
```

## Fix: bump the timestamp in the three nodes (measured, no slowdown found)

In each of the three `InternalExecute` methods, call `program.Catalog.UpdatePlanCacheTimeStamp()` right after the `CatalogDeviceSession` call that changes the catalog (after, not before: a plan compiled before the change and cached between the bump and the change would otherwise survive). In `DropConversionNode` that is inside the loop, after `DropConversion(conversion)` and before `break`. `UpdatePlanCacheTimeStamp` takes the catalog lock itself, which `DropConversionNode` already holds (re-entrant).

Put it in the nodes, not in `CatalogDeviceSession.CreateOperator`/`AddImplicitConversion`/`RemoveImplicitConversion`: the server's lazy catalog loading (`ServerCatalogDeviceSession.ResolveCatalogObject`) also calls those session methods for every operator and conversion it loads, and a bump there would flush the whole plan cache during ordinary warm-up although a load cannot change what an already-compiled plan binds to. The node is also what every DDL path goes through: `create operator`, `create aggregate operator` (`Compiler.cs` ~6450, ~6826), the operators and conversions `create type` generates (~5855), and the `alter type` paths that re-create selectors and comparers (`DDLNodes.cs` ~1982–2285). Those alter paths may bump several times per statement; that is harmless.

**Measurement (fix stage, this machine, 2026-11-18).** The fix as above was applied temporarily and the coverage run (`dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~CoverageScripts" --logger "console;verbosity=normal"`) compared with the unchanged code. Its template step registers the `Frontend` and `TestFramework.Coverage.Base` libraries, which is library registration (each `create operator` in their scripts goes through `CreateOperatorNode`):

| Run | Template (library registration) | Whole coverage run |
|---|---|---|
| Unchanged, run 1 | 2.8 s | 3.45 min |
| Unchanged, run 2 | 3.3 s | 4.41 min |
| With the bump | 2.3 s | 4.25 min |

All 115 passing scripts still passed and every known failure matched, with the bump. Run-to-run noise (0.5 s on registration, a minute on the whole run, per-script timings drifting upward across consecutive runs regardless of code) is larger than any difference the bump makes, so no slowdown was measured. That is expected: during registration nearly every statement is new text (each `create` names a different object), so there are few cached plans to lose. The cost lands only on a server that reuses cached plans while operators or conversions are being created, which is the case where reuse is wrong anyway. So take the simple fix; do not add per-name invalidation or skip the bump in a loading context.

Leave a tripwire `NOTE:` at one of the bumps, e.g. `// NOTE: flushes every cached plan, measured as no slower for library registration; if a workload creating operators while serving repeated statements shows plan-cache churn, invalidate only plans that resolved this operator name or traced a path through these types.`

## Also

- Update the `<remarks>` on `SchemaCacheInvalidationTest`: the first two tests still use different text after the DDL (keep them as they are; they test the compile-to-execute window, not the plan cache), but the remark naming this ticket as the reason should say the plan cache case is now covered by the three `...InvalidatesCachedPlans` tests below.
- Add a known-defect row (K33) to `docs/modernization-plan.md#known-defects`, in the format of K32, and change K32's closing sentence, which names this ticket as outstanding.
- No migration note: an application can only notice that a repeated statement now behaves as the same statement compiled fresh would.

## TODO

- Add `program.Catalog.UpdatePlanCacheTimeStamp()` after the catalog change in `CreateConversionNode`, `DropConversionNode` and `CreateOperatorNode` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`; tabs, CRLF, preserve its byte-order-mark state), with the tripwire `NOTE:` above.
- Run `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~SchemaCacheInvalidationTest"`: all five pass.
- Update the `SchemaCacheInvalidationTest` remarks.
- Add K33 to the known-defects table and update K32's last sentence.
- Run `dotnet build Dataphor.slnx` and `dotnet test Dataphor.slnx`; the coverage manifest needs no change (no entry names this ticket).
