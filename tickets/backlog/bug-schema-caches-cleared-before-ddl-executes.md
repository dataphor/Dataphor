description: The server's shared memos of conversion paths and operator resolutions are cleared when a statement that changes conversions, operators or types is compiled, not when it runs, so another session compiling in between can put the old answer back and every session keeps using it after the change.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Compiling/OperatorResolutionCache.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: The window is between compiling and executing one DDL statement and needs another session to compile the same type pair or operator name inside it, so it may never be seen in practice, and moving the invalidation means touching every create/drop path for operators, conversions and types.
----
## What is wrong

Each catalog keeps two memos shared by all its sessions: the conversion path cache (`ScalarConversionPathCache`, the chain of implicit conversions the compiler found from one scalar type to another) and the operator resolution cache (`OperatorResolutionCache`). Both are invalidated by the compiler while it compiles the DDL statement that makes them stale:

- `create conversion` clears conversion paths through its source and target types (`Compiler.CompileCreateConversionStatement`, around line 9787), `drop conversion` clears paths that use the conversion (`CompileDropConversionStatement`, around line 9812), `create type ... like` clears paths through the like type (around line 5802);
- the many `OperatorResolutionCache.Clear(...)` calls in `Compiler.cs` for create/drop operator, representations, specials and conversions.

The catalog itself changes later, when the plan executes (`CreateConversionNode` → `CatalogDeviceSession.CreateConversion` → `AddImplicitConversion`; `DropConversionNode` → `DropConversion` → `RemoveImplicitConversion`). Between the two, another session that compiles an expression needing the same conversion (or resolving the same operator) finds the memo empty, works the answer out from the catalog as it still stands, and stores it. After the DDL executes, that stored answer is stale and nothing clears it again:

- after `drop conversion`, the cached path still contains the dropped conversion, so later compiles keep applying it implicitly;
- after `create conversion`, the cached path ignores the new conversion (a missing or worse path), until something else clears the cache.

The only invalidation at the point of change today is `CatalogDeviceSession.InvalidateCachesAfterUndo`, for rollbacks.

This was noticed in the review of `bug-conversion-path-cache-unlocked-clears` (K31), which made the conversion path cache's members thread-safe but deliberately kept the clears from waiting on a trace in progress; that locking cannot close this window, which is about when the clear happens, not whether it is synchronized.

## Expected

After a DDL statement that adds or removes a conversion, operator or type commits its catalog change, no session compiles against a memo entry computed from the catalog as it stood before the change. The natural site is the point where the catalog changes (the `CatalogDeviceSession` create/drop methods, or `AddImplicitConversion`/`RemoveImplicitConversion` and their operator counterparts), keeping or dropping the compile-time clears as the design requires.

To confirm: a test that holds one session between compiling and executing `drop conversion` (for example a device or hook that blocks in execute), compiles an expression needing that conversion in a second session, lets the drop finish, and then checks the cache or a fresh compile no longer uses the dropped conversion.
