description: The server keeps a shared memo of which version of an operator each call should use, and it records choices that only one statement could see: choices made while loading stored code with only some versions loaded, and choices pointing at an operator that a failed or abandoned `create operator` never actually added. Every session then reuses them, so ordinary statements can call a worse-matching version, or an operator that does not exist.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: The loading arm is inferred from the code, not observed, and its likely fix (not caching choices made while loading) may make catalog warm-up slightly slower; the failed-create arm is verified but needs a create operator that calls its own name and then fails to compile (or is prepared and never run), which is mostly a development-time situation.
----
## What it is

The operator resolution cache is a memo, shared by every session of a catalog. For an operator name and the argument types of a call, it records which overload the compiler chose. `Compiler.ResolveOperator` (`src/engine/DAE/Compiling/Compiler.cs`, the `#if USEOPERATORRESOLUTIONCACHE` block near line 9570) stores every resolution it makes there, excluding only session, application-transaction and non-scalar operators. It does not check that the resolution was made from the shared catalog as every other session sees it. Two cases where it was not:

## Arm 1: choices made while loading (static)

`ResolveOperator` normally asks the catalog device session to load every stored overload of the name (`CatalogDeviceSession.ResolveOperatorName`) before choosing. It skips that step in a loading context, i.e. while the server deserializes a stored object, because "the required operator will be in the cache because of the dependency loading mechanism". Then it chooses among the overloads already in memory, and still adds the result to the shared cache.

Operators load lazily by name (`ServerCatalogDeviceSession.ResolveCatalogObject` loads on demand). So a plausible sequence is:

1. `create operator Foo(const X : Decimal) : String begin result := 'Decimal'; end;`
2. `create operator Bar() : String begin result := Foo(5); end;` (Bar depends on `Foo(Decimal)`).
3. `create operator Foo(const X : Integer) : String begin result := 'Integer'; end;`
4. Restart the server on the same catalog store.
5. Evaluate `Bar()` first: loading Bar compiles its body in a loading context, with only `Foo(Decimal)` loaded (Bar's dependency), and caches "`Foo` with `(Integer)` → `Foo(Decimal)`".
6. Another session compiles `Foo(5)`: it hits the cache and calls `Foo(Decimal)`, although `Foo(Integer)` exists and is the better match. Without step 5, the same statement would load both overloads and choose `Foo(Integer)`.

Bar itself binding to `Foo(Decimal)` is arguably right (that is the dependency it was stored with); the defect is that its loading-time choice becomes everyone's answer.

`bug-schema-caches-cleared-before-ddl-executes` added a by-name clear in `CatalogDeviceSession.CreateOperator`, which also runs when an overload is lazily loaded. That evicts such an entry once some other compile loads `Foo(Integer)`. But nothing forces that load while the stale entry keeps answering, so the entry can last as long as the server runs.

To confirm: a `DAE.Server.Tests` test running the sequence above, with a server restart on the same instance directory and catalog store between steps 3 and 5 (check whether `TestServerInstance` can restart, or what `CatalogStoreTest` does). Expect `Foo(5)` (or a different text, to avoid the plan cache: see `bug-plan-cache-survives-conversion-and-overload-ddl`) to return `'Integer'`.

## Arm 2: operators that exist only in one statement's compile (verified)

Found in the review of `bug-schema-caches-cleared-before-ddl-executes`. While compiling `create operator`, the compiler adds the new operator to the compiling plan's private catalog (`plan.PlanCatalog`, `CompileCreateOperatorStatement` ~line 6537) before compiling its body, so that the body can call itself. `ResolveOperator` resolves against `plan.PlanCatalog` first, so a call in the body to the operator's own name and signature resolves to the new, not yet created operator, and that resolution goes into the shared cache. If the statement then fails to compile, or is prepared and never executed, the operator never reaches the catalog, but the cached entry stays, and any session calling that name and signature gets the phantom operator.

Verified with a throwaway test on a `DAE.Server.Tests` in-process server: `create operator G(const X : Integer) : Integer begin if X <= 0 then result := 0 else result := G(X - 1) + 1; result := NoSuchOperatorAtAll(); end;` fails to compile ("Unable to resolve operator reference "NoSuchOperatorAtAll""), yet evaluating `G(5)` on another session afterwards returned `5` instead of failing with "operator not found". With a body that recursed without a base case, the same evaluation overflowed the stack and killed the test host process.

When the statement does execute, `CatalogDeviceSession.CreateOperator` now clears the name's entries (since `bug-schema-caches-cleared-before-ddl-executes`), so only the never-executed case remains. A transaction rollback clears both caches whole (`InvalidateCachesAfterUndo`).

The conversion path cache has the same shape at one site, static only: `create type ... like` (`CompileCreateScalarTypeStatement` ~line 5784) temporarily adds a narrowing conversion from the like type to the new type to the like type's shared conversion list while it compiles, and paths traced through it in that window (by this compile, or another session's) are cached by `Compiler.FindScalarConversionPath`. They lead only to the new type, which no other session can name, so this matters only if the statement does not execute and a type of the same name is created later (whether a cached path is then matched depends on how `ScalarType.Equals` compares).

## Expected behaviour

The shared caches hold only answers computed from the shared catalog as every session sees it:

- A compile outside a loading context resolves an operator call to the same overload whether or not some stored operator that calls the same name was loaded before it.
- After a `create operator` that failed to compile, or was prepared and never executed, calling that name and signature fails as it did before the statement (operator not found, or the previous overload).

## Likely fix site

The cache add in `Compiler.ResolveOperator`: do not add a resolution whose operator came from `plan.PlanCatalog` rather than `plan.Catalog` (arm 2), nor one made in a loading context or without forcing the name's overloads to load (arm 1). Whether a loading-context compile should also stop *reading* the shared cache, which could bind a loading operator to a different overload than its stored dependency, is part of the same question. For the conversion-path shape, the same rule at the cache add in `Compiler.FindScalarConversionPath`: do not cache a path that uses a conversion not in the shared catalog.
