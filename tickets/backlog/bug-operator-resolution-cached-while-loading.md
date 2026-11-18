description: When the server loads a stored operator from its catalog, it may pick which version of a called operator to use while only some versions are loaded, and it shares that choice with every session, so ordinary statements can end up calling a worse-matching version than the one they should get.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: Inferred from reading the code, not observed; confirming it needs a test that restarts a server on the same catalog store, and the likely fix (not caching choices made while loading) may make catalog warm-up slightly slower.
----
## What happens

The operator resolution cache is a memo, shared by every session of a catalog. For an operator name and the argument types of a call, it records which overload the compiler chose. `Compiler.ResolveOperator` (`src/engine/DAE/Compiling/Compiler.cs`, the `#if USEOPERATORRESOLUTIONCACHE` block near line 9570) normally asks the catalog device session to load every stored overload of the name (`CatalogDeviceSession.ResolveOperatorName`) before choosing. It skips that step in a loading context, i.e. while the server deserializes a stored object, because "the required operator will be in the cache because of the dependency loading mechanism". Then it chooses among the overloads already in memory, and still adds the result to the shared cache (the only exclusions are session, application-transaction and non-scalar operators).

Operators load lazily by name (`ServerCatalogDeviceSession.ResolveCatalogObject` loads on demand). So a plausible sequence is:

1. `create operator Foo(const X : Decimal) : String begin result := 'Decimal'; end;`
2. `create operator Bar() : String begin result := Foo(5); end;` (Bar depends on `Foo(Decimal)`).
3. `create operator Foo(const X : Integer) : String begin result := 'Integer'; end;`
4. Restart the server on the same catalog store.
5. Evaluate `Bar()` first: loading Bar compiles its body in a loading context, with only `Foo(Decimal)` loaded (Bar's dependency), and caches "`Foo` with `(Integer)` → `Foo(Decimal)`".
6. Another session compiles `Foo(5)`: it hits the cache and calls `Foo(Decimal)`, although `Foo(Integer)` exists and is the better match. Without step 5, the same statement would load both overloads and choose `Foo(Integer)`.

Bar itself binding to `Foo(Decimal)` is arguably right (that is the dependency it was stored with); the defect is that its loading-time choice becomes everyone's answer.

`bug-schema-caches-cleared-before-ddl-executes` added a by-name clear in `CatalogDeviceSession.CreateOperator`, which also runs when an overload is lazily loaded. That evicts such an entry once some other compile loads `Foo(Integer)`. But nothing forces that load while the stale entry keeps answering, so the entry can last as long as the server runs.

## Expected behaviour

A compile outside a loading context resolves an operator call to the same overload whether or not some stored operator that calls the same name was loaded before it.

## To confirm

A `DAE.Server.Tests` test running the sequence above, with a server restart on the same instance directory and catalog store between steps 3 and 5 (check whether `TestServerInstance` can restart, or what `CatalogStoreTest` does). Expect `Foo(5)` (or a different text, to avoid the plan cache: see `bug-plan-cache-survives-conversion-and-overload-ddl`) to return `'Integer'`.

## Likely fix site

The cache add in `Compiler.ResolveOperator`: do not add a resolution made in a loading context (or made without forcing the name's overloads to load). Whether a loading-context compile should also stop *reading* the shared cache, which could bind a loading operator to a different overload than its stored dependency, is part of the same question.
