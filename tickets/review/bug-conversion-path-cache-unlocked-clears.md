description: The server keeps one shared memo of how to convert between data types, and some statements that create types or conversions erased entries from it without taking its lock, so a session doing that while another session compiled could get a spurious internal error or damage the memo for everyone. The memo now locks itself.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Schema/Catalog.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/ConcurrentDDLTest.cs, docs/modernization-plan.md
repro: verified
----
## What changed

- `ScalarConversionPathCache` (`src/engine/DAE/Schema/ScalarType.cs`) has a private `readonly object _syncRoot`, taken inside the indexer, `Add`, `Clear()`, `Clear(ScalarType)` and `Clear(Conversion)`, plus a class doc comment in the same terms as `OperatorResolutionCache`'s. `Add` now assigns (`_paths[key] = path`) instead of `Dictionary.Add`, so a duplicate key no longer throws; the compiler's outer `lock (plan.Catalog.ConversionPathCache)` in `Compiler.FindScalarConversionPath` is unchanged and still keeps lookup-trace-add atomic.
- `Catalog.ConversionPathCache` and `Catalog.OperatorResolutionCache` (`src/engine/DAE/Schema/Catalog.cs`) create their instance with `LazyInitializer.EnsureInitialized`, so concurrent first use yields one instance (second arm; no test, as the ticket said — first-use timing window only).
- `CatalogDeviceSession.InvalidateCachesAfterUndo` no longer wraps `ConversionPathCache.Clear()` in an outer lock; its comment said the lock was needed, which is no longer true, and it now matches the `OperatorResolutionCache.Clear()` line above it.
- K31 row added to the known-defects table in `docs/modernization-plan.md`.

The four unlocked callers in `Compiler.cs` (create/drop conversion, create type like) and `SystemClearConversionPathCacheNode` were left as they are: they are now safe because the cache locks itself.

No migration note: public member signatures unchanged; the only observable change is that `Add` with an existing key overwrites instead of throwing, which no in-tree caller relies on (the only caller holds the outer lock and checks first).

## Tests

- No new test. The reproduction `ConcurrentDDLTest.ConversionPathCacheAllowsConcurrentAddAndClear` (written in the fix stage; failed 3 of 3 before) passed 4 of 4 runs after the change.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors.
- `dotnet test Dataphor.slnx`: green — Common.Tests 13/13; DAE.Server.Tests 159 passed, 25 skipped (the manifest's known-failure/unstable/fragment coverage cases), 0 failed. The suite took 12 m 26 s here, longer than the ~3.5 min AGENTS.md quotes; not investigated (the machine may have been loaded).

## For the reviewer

- Check no other site relies on locking the cache instance to exclude `Clear` (the only remaining outer lock is the compiler's, which is about trace-then-add atomicity, not exclusion of clears).
- Line endings: all three source files are CRLF without byte-order mark, unchanged.
