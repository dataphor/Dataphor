description: After a conversion is dropped or a better-matching operator overload is created, the server keeps reusing already-compiled copies of statements it has run before, so a repeated statement still applies the dropped conversion or still calls the old overload.
prereq: bug-schema-caches-cleared-before-ddl-executes
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Server/PlanCache.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: Flushing every cached plan on each created operator or conversion makes library registration and long rebuild scripts recompile everything repeatedly, and 3.1 behaved this way without known complaints, so a maintainer may prefer to document it or invalidate only plans that depend on the changed name.
----

**Tending agent, 2026-11-18:** promoted to `fix/`. The expected behaviour below is settled; the choice between options is a performance one, to decide by measurement, not an owner decision. Start with the simple fix (bump `PlanCacheTimeStamp` in the three nodes) and measure library registration and the coverage run before and after. If registration gets measurably slower, invalidate only when the change can affect a cached plan (the operator name already has cached resolutions, or the conversion's types do). Do not skip the bump during library loading unless no plan cached before the load can survive it. Record the numbers in the ticket.

## What is wrong

The server keeps a plan cache (`src/engine/DAE/Server/PlanCache.cs`): compiled plans keyed by statement text, reused while `Catalog.PlanCacheTimeStamp` is not newer than the plan's. Some DDL bumps that timestamp (`DropOperatorNode`, `DropScalarTypeNode`, table/view drops, several alter nodes in `DDLNodes.cs`); these do not:

- `DropConversionNode` (`DDLNodes.cs` ~312): a cached plan that applied the conversion keeps applying it after the drop. Verified: create types A and B, a conversion A→B, and an operator taking B; evaluate `TakesB(A(5))`; `drop conversion A to B`; evaluating the identical text `TakesB(A(5))` from a new session still succeeds, while `TakesB(A(6))` fails as it should.
- `CreateOperatorNode` (~587) when the new operator is a better overload for an existing call: verified with `Op(const X : Decimal)` then `Op(5)` evaluated (returns `"Decimal"`), then `create operator Op(const X : Integer)`; the identical text `Op(5)` still returns `"Decimal"`, `Op(6)` returns `"Integer"`.
- `CreateConversionNode` (~296), by the same reasoning (static): a cached plan keeps a worse conversion path that a new conversion would replace.

Found in the fix stage of `bug-schema-caches-cleared-before-ddl-executes`, whose reproductions had to use different statement text after the DDL to avoid this. That ticket moves the conversion path and operator resolution cache invalidation to the moment the catalog changes; this is the same kind of problem in a third memo, at neighbouring sites, but the fix carries a performance tradeoff the other does not, hence separate.

## Expected

After `drop conversion`, `create conversion` or `create operator` executes, a statement compiled before the change is not reused if the change would make it compile differently. Options, for whoever works this: bump `PlanCacheTimeStamp` in those three nodes (simple, flushes everything; check the cost during library registration, which creates thousands of operators — possibly skip in loading context); or bump it only when the created operator's name already has cached resolutions / the conversion's types appear in cached plans. The rollback path (`CatalogDeviceSession.InvalidateCachesAfterUndo`) already bumps the timestamp.
