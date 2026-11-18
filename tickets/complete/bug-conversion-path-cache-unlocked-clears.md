description: The server keeps one shared memo of how to convert between data types, and some statements that create types or conversions erased entries from it without taking its lock, so a session doing that while another session compiled could get a spurious internal error or damage the memo for everyone. The memo now locks itself.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Schema/Catalog.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/ConcurrentDDLTest.cs, docs/modernization-plan.md
repro: verified
----
## What changed

- `ScalarConversionPathCache` (`src/engine/DAE/Schema/ScalarType.cs`) takes a private `_syncRoot` lock inside its indexer, `Add` and the three `Clear` overloads, matching `OperatorResolutionCache` (K3). `Add` assigns instead of calling `Dictionary.Add`, so a duplicate key overwrites rather than throws. The compiler's outer `lock (plan.Catalog.ConversionPathCache)` in `Compiler.FindScalarConversionPath` is unchanged and still makes lookup, trace and add one step.
- `Catalog.ConversionPathCache` and `Catalog.OperatorResolutionCache` (`src/engine/DAE/Schema/Catalog.cs`) create their instance with `LazyInitializer.EnsureInitialized`, so concurrent first use yields one instance.
- `CatalogDeviceSession.InvalidateCachesAfterUndo` no longer takes an outer lock around `ConversionPathCache.Clear()`.
- K31 row in the known-defects table of `docs/modernization-plan.md`.
- Reproduction: `ConcurrentDDLTest.ConversionPathCacheAllowsConcurrentAddAndClear` (written in the fix stage).

## Review findings

Read the diff of `ticket(implement): bug-conversion-path-cache-unlocked-clears` before the handoff.

- **Correctness / locking.** Every member of the cache now locks; `ScalarConversionPath.Contains` (called under the lock) takes no lock of its own, so no lock-order risk. The only remaining outer lock on the instance is the compiler's; no other site relies on locking the instance to exclude `Clear` (checked every `ConversionPathCache` reference in `src/`, `tests/`, `libraries/`). The overwrite in `Add` is harmless: its only caller checks first under the outer lock. Nothing to fix.
- **Major, filed:** the conversion path cache and the operator resolution cache are both cleared when the DDL statement is *compiled*, but the catalog changes when it *executes*; another session compiling in between re-caches the old answer, which then outlives the change (a dropped conversion keeps being applied). Pre-existing and not something locking can close; it is a class across every compile-time clear in `Compiler.cs`, so it is filed once at the boundary rung (invalidate where the catalog changes) as `backlog/bug-schema-caches-cleared-before-ddl-executes` (repro: static). No open ticket or `NOTE:` claimed these sites.
- **Lazy initialization.** `EnsureInitialized` may construct two instances under a race but publishes one; the losing instance is never used. Correct. The comment on the conversion-cache getter says why one instance matters; fine.
- **Docs.** K31 row is accurate. `docs/architecture.md` does not describe these caches; nothing to update. No migration note needed: public signatures unchanged and no extension could rely on `Add` throwing on a duplicate.
- **Tests.** The single reproduction test is at the lowest layer (the cache class) and exercises the actual race; kept. No test added for lazy initialization (first-use timing window only; would be nondeterministic).
- **Source hygiene.** `ScalarType.cs` and `Catalog.cs` are CRLF without byte-order mark, `CatalogDeviceSession.cs` is CRLF with a byte-order mark (the handoff said all three lacked one; the file is unchanged in that respect). All lines CRLF after the change (counted with a byte scan). No narrated comments.
- **Error handling, resource cleanup, performance.** No resources involved; the private lock adds an uncontended acquire per cache access, inside a path already holding the outer lock; nothing to record.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors.
- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ConcurrentDDLTest"`: 4 passed, 0 failed.
- The full suite was not rerun in review (no code changed in review); the implement stage ran `dotnet test Dataphor.slnx` green (DAE.Server.Tests 159 passed, 25 skipped as the manifest records, 0 failed).
