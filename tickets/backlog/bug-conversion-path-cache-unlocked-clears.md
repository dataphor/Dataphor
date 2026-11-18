description: The server keeps one shared memo of how to convert between data types, and some statements that create types or conversions erase entries from it without taking its lock, so a session doing that while another session compiles can get a spurious internal error or damage the memo for everyone.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, src/engine/DAE/Compiling/OperatorResolutionCache.cs
repro: static
severity: edge-case
likelihood: unusual
tradeoffs: It needs concurrent type or conversion DDL while other sessions compile code that needs an implicit conversion, which is rare outside library loading, and no failure has been observed; a maintainer may defer it until concurrent DDL is exercised harder.
----
## What is wrong

`Catalog.ConversionPathCache` (class `ScalarConversionPathCache` in `src/engine/DAE/Schema/ScalarType.cs`) is one plain `Dictionary` shared by every session. The compiler reads and fills it under `lock (plan.Catalog.ConversionPathCache)` (`Compiler.FindScalarConversionPath`, about line 11613), but four call sites remove entries without that lock:

- `CompileCreateConversionStatement` (about line 9787): `Clear(conversion.SourceScalarType)`, `Clear(conversion.TargetScalarType)`;
- `CompileDropConversionStatement` (about line 9812): `Clear(conversion)`;
- the create scalar type path (about line 5802): `Clear(node.ScalarType.LikeType)`;
- the admin operator `ClearConversionPathCache()` (`SystemClearConversionPathCacheNode` in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs`, about line 2528): `Clear()`. Added by the review of `bug-ddl-rollback-keeps-cached-plans`.

The two per-object `Clear` overloads enumerate the dictionary and then remove from it, and the whole `Clear()` writes to it. Run against another session's `Add` under the lock, the enumeration can throw "Collection was modified" (an internal error on the DDL statement), and two concurrent writers can leave the dictionary's internal state inconsistent, after which lookups can misbehave for every session.

This is the same class as K3 in the plan's known defects: the operator resolution cache had the same shape, and the fix there gave `OperatorResolutionCache` a private lock taken inside every member, leaving the compiler's outer lock in place (see the class comment in `src/engine/DAE/Compiling/OperatorResolutionCache.cs`).

Found while working `bug-ddl-rollback-keeps-cached-plans`, whose rollback invalidation clears this cache under the compiler's lock; not observed failing.

## Expected

Every member of `ScalarConversionPathCache` is safe to call from any session without the caller holding a lock, as `OperatorResolutionCache` is now; the compiler's outer lock around lookup-then-trace-then-add stays. Confirming the defect would take a concurrency test in the style of `CatalogStoreConcurrencyTest.ConcurrentDDLWithQueriesAndDrops`: sessions repeatedly creating and dropping a conversion between their own types while others compile expressions that need an implicit conversion.
