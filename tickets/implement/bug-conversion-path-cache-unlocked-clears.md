description: The server keeps one shared memo of how to convert between data types, and some statements that create types or conversions erase entries from it without taking its lock, so a session doing that while another session compiles can get a spurious internal error or damage the memo for everyone.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Schema/Catalog.cs, src/engine/DAE/Compiling/OperatorResolutionCache.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/ConcurrentDDLTest.cs, docs/modernization-plan.md
difficulty: easy
repro: verified
----
## What is wrong

`Catalog.ConversionPathCache` (class `ScalarConversionPathCache`, `src/engine/DAE/Schema/ScalarType.cs` about line 907) is one plain `Dictionary` shared by every session of a catalog. It remembers, for a source and target scalar type, the chain of conversions the compiler found between them. The compiler reads and fills it under `lock (plan.Catalog.ConversionPathCache)` (`Compiler.FindScalarConversionPath`, `src/engine/DAE/Compiling/Compiler.cs` about line 11613; the cache is on because `USECONVERSIONPATHCACHE` is defined at the top of that file). Four call sites change it without that lock:

- `CompileCreateConversionStatement` (Compiler.cs about line 9787): `Clear(conversion.SourceScalarType)`, `Clear(conversion.TargetScalarType)`;
- `CompileDropConversionStatement` (about line 9812): `Clear(conversion)`;
- the create scalar type path (about line 5802): `Clear(node.ScalarType.LikeType)`;
- the admin operator `ClearConversionPathCache()` (`SystemClearConversionPathCacheNode`, `src/engine/DAE/Runtime.Instructions/SystemNodes.cs` about line 2528): `Clear()`.

The fifth caller, `CatalogDeviceSession.InvalidateCachesAfterUndo` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs` about line 1524), does take the compiler's lock.

`Clear(ScalarType)` and `Clear(Conversion)` enumerate the dictionary and then remove from it. Racing another session's `Add`, the enumeration throws "Collection was modified; enumeration operation may not execute." (an internal error on the DDL statement), and two unsynchronized writers can corrupt the dictionary's internal state for every session.

This is the same class as K3 in the plan's known defects: the operator resolution cache had this shape, and the fix gave `OperatorResolutionCache` a private lock taken inside every member while the compiler's outer lock on the instance stayed (see that class's doc comment).

## Reproduction (written in the fix stage, fails today)

`ConcurrentDDLTest.ConversionPathCacheAllowsConcurrentAddAndClear` in `tests/DAE.Server.Tests/ConcurrentDDLTest.cs`: 8 threads for 500 ms on one cache seeded with 200 entries; two of every four threads look up and add under `lock (cache)` (the compiler's discipline), one calls `Clear(ScalarType)` and one `Clear(Conversion)` without a lock. It failed in 3 of 3 runs, in under 70 ms each, with "Collection was modified; enumeration operation may not execute.":

```
dotnet test tests/DAE.Server.Tests --filter Name=ConversionPathCacheAllowsConcurrentAddAndClear
```

It needs no server and builds its `ScalarType`, `Conversion` and `ScalarConversionPath` objects directly (scalar types compare by name, so each thread uses distinct names).

## Fix

Give `ScalarConversionPathCache` a private `readonly object _syncRoot`, taken inside the indexer, `Add` and all three `Clear` overloads, exactly as `OperatorResolutionCache` does; add a class doc comment in the same terms as that class's (shared by every session, safe for concurrent use; the compiler's outer lock on the instance is separate and still makes lookup-trace-add atomic so two sessions do not both trace and then both `Add`; `_syncRoot` is held only inside these members, which call nothing that takes another lock, so it cannot take part in a deadlock). `ScalarConversionPath.Contains` only walks the path's own list of conversions, so calling it under `_syncRoot` is fine.

Keep the compiler's `lock (plan.Catalog.ConversionPathCache)` in `FindScalarConversionPath`: without it two sessions tracing the same pair would both `Add`, and `Dictionary.Add` throws on the duplicate key. Optionally make `Add` tolerate an existing key (`_paths[key] = path` or a `ContainsKey` check, as `OperatorResolutionCache.Add` does) so the cache no longer depends on its callers for that; either way the outer lock stays. `InvalidateCachesAfterUndo` can keep its outer lock (harmless) — leave it unless its comment becomes misleading; if it does, update the comment to say the cache is self-synchronizing.

Same root, second arm: `Catalog.ConversionPathCache` and `Catalog.OperatorResolutionCache` (`src/engine/DAE/Schema/Catalog.cs` about lines 230–250) create their cache lazily with an unsynchronized null check, so two sessions using a fresh catalog at the same moment can each create one; a session can then lock one instance and, re-reading the property inside the lock, use the other. Create each cache with `LazyInitializer.EnsureInitialized(ref _field)` (or initialize the field at construction if the catalog is not created in bulk) so the property always returns one instance. No test for this arm: the window is the first use only and a test would be timing-bound with no reliable failure.

Add a K31 row to the known-defects table in `docs/modernization-plan.md` (next free number after K30): the defect, `src/engine/DAE/Schema/ScalarType.cs` (`ScalarConversionPathCache`) and `Catalog.cs`, status fixed, "WS2 `bug-conversion-path-cache-unlocked-clears`", and that `ConcurrentDDLTest.ConversionPathCacheAllowsConcurrentAddAndClear` reproduces it. No migration note: no D4 behaviour, signature or public member changes (`ScalarConversionPathCache`'s public members keep their signatures).

Source hygiene: `ScalarType.cs`, `Catalog.cs` and `ConcurrentDDLTest.cs` use tabs and CRLF; check each file's byte-order-mark state before and after editing (`ConcurrentDDLTest.cs` has one).

## TODO

- Add `_syncRoot` to `ScalarConversionPathCache` and take it in the indexer, `Add`, `Clear()`, `Clear(ScalarType)`, `Clear(Conversion)`; add the class doc comment.
- Make the two lazy cache properties on `Catalog` create exactly one instance.
- Run `dotnet test tests/DAE.Server.Tests --filter Name=ConversionPathCacheAllowsConcurrentAddAndClear` several times: it must pass every time (it failed 3 of 3 before the fix).
- Run `dotnet build Dataphor.slnx` and the whole `dotnet test Dataphor.slnx` (the coverage scripts exercise conversions and `create conversion`).
- Add the K31 row to `docs/modernization-plan.md`.
