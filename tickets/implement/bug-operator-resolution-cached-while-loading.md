description: The server's shared memo of which overload each operator call should use now only records answers computed from the shared catalog, so a choice made while loading stored code (with only some overloads in memory), or a choice pointing at an operator that a failed or never-executed `create operator` added only to the compiling plan's private catalog, no longer becomes everyone's answer.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Compiling/OperatorResolutionCache.cs, src/engine/DAE/Schema/ScalarType.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, tests/DAE.Server.Tests/TestServerInstance.cs, docs/modernization-plan.md
----
## What to change

The shared `OperatorResolutionCache` and `ConversionPathCache` on `Schema.Catalog` must hold only answers computed from the shared catalog as every session sees it. Two cache-add sites violate that today.

### Compiler.ResolveOperator (Compiler.cs ~line 9570)

The block guarded by `#if USEOPERATORRESOLUTIONCACHE` reads the shared cache, falls through to resolve against `plan.PlanCatalog` then `plan.Catalog`, and adds the result to the shared cache. Change the add so it skips the cache in two cases:

- **Loading context.** When `plan.InLoadingContext()` is true, neither read nor write the shared cache. In a loading context the code already skips the "force all overloads of this name to load" step (because the stored dependency being loaded has already brought in the overload it needs). Caching that narrower answer is what leaks a loading-time choice to every session; reading the cache in a loading context is also wrong, because it would bind the loading operator to whichever overload another session's call last cached, rather than to the dependency the stored operator was compiled against. The loading arm of `ResolveOperator` therefore resolves against `plan.PlanCatalog` and `plan.Catalog` directly, without touching the shared memo.

- **Operator came only from the plan's private catalog.** After `ResolveCall(plan, plan.PlanCatalog, context)` and the optional `ResolveCall(plan, plan.Catalog, context)`, only add to the shared cache when the chosen operator is in `plan.Catalog`. The cleanest check is `plan.Catalog.Contains(context.Operator)`; a `plan.PlanCatalog.Contains(context.Operator) && !plan.Catalog.Contains(context.Operator)` is equivalent (nothing is added to `plan.PlanCatalog` that is also in `plan.Catalog` within one plan). Without this guard, a `create operator` whose body calls its own name and signature caches a phantom entry; if the statement fails to compile, or is prepared and never executed, the entry is the only record of that operator and every session can call it.

Keep the existing exclusions (session, A/T, non-scalar operators). The outer `lock (plan.Catalog)` and inner `lock (plan.Catalog.OperatorResolutionCache)` stay, but when loading we bypass both since we neither read nor write; the catalog read inside `ResolveCall(plan, plan.Catalog, context)` takes `lock (catalog)` on its own.

### Compiler.FindScalarConversionPath (Compiler.cs ~line 11604)

Same shape at `plan.Catalog.ConversionPathCache.Add(...)`: do not cache a path that uses a conversion not in `plan.Catalog`. The one exposure today is `create type ... like` (CompileCreateScalarTypeStatement ~line 5784): while it compiles, it adds a narrowing conversion to the shared `LikeType.ImplicitConversions` list and puts the conversion in `plan.PlanCatalog`. A path traced through it in that window — by this plan or another session's — must not be cached, because the target type is only in `plan.PlanCatalog` and the conversion may vanish if the statement fails or is never executed. Check: if the path contains any `Schema.Conversion` that is in `plan.PlanCatalog`, skip the add. Reading the cache stays unchanged: a hit is always a path through shared conversions.

### What stays the same

- `CatalogDeviceSession.CreateOperator`/`DropOperator` / `AddImplicitConversion`/`RemoveImplicitConversion` already clear the right cache entries when the catalog changes; the `InvalidateCachesAfterUndo` rollback clear is unchanged. Those are what makes the shared cache correct for the executed case.
- The compile-time `OperatorResolutionCache.Clear(...)` calls (notably `CompileCreateOperatorStatement` ~line 6540 and `CompileCreateScalarTypeStatement` ~line 5808) stay — they evict entries another session may have cached against the still-shared catalog before this plan's new object exists.
- `A/T` and session-operator handling, `InLoadingContext` behaviour outside `ResolveOperator`, and the lock order (catalog → cache) are not touched.

## Why the fix is here and not at the clears

`CatalogDeviceSession.CreateOperator` clears the operator name's resolutions, so any phantom entry is evicted once the real `create operator` executes. But if the statement fails to compile, or is prepared and then discarded, nothing clears the entry — it stays as long as the server runs. For the loading arm, the by-name clear only fires when `CreateOperator` runs on an overload lazily loaded later, and nothing forces that load while the stale entry keeps answering ordinary calls. The only way to make the invariant hold is to never put those entries in the cache in the first place.

## Tests

One per arm, lowest layer that reproduces the bug (an in-process `DAE.Server.Tests` server, no client or UI):

- **Arm 2 (plan-catalog phantom).** Add a test to `SchemaCacheInvalidationTest.cs`. Prepare `create operator G(const X : Integer) : Integer begin if X <= 0 then result := 0 else result := G(X - 1) + 1; result := NoSuchOperatorAtAll(); end;` (the unresolved call fails compilation — do not execute). Then on another session, evaluate `G(5)` and assert it throws `CompilerException` with code `OperatorNotFound`. Before the fix this returns `5`; a variant without a base case overflows the stack and kills the test host, so keep the base case. Use `Unique` names to avoid the plan cache.

- **Arm 1 (loading-context leak).** Add a test using the `TestServerInstance(name, template)` restart pattern already in the fixture infrastructure: build a template that runs `create operator Foo(const X : Decimal) : String begin result := 'Decimal'; end; create operator Bar() : String begin result := Foo(5); end; create operator Foo(const X : Integer) : String begin result := 'Integer'; end;`, stop the template, start a fresh instance from its directory, evaluate `Bar()` first (which lazily loads Bar and, inside Bar's loading-context compile, binds `Foo(Integer)` → `Foo(Decimal)`), then evaluate `Foo(5)` on another session and assert it returns `'Integer'`. Vary the second call's text (for example a leading harmless `var`) to avoid the plan cache — the operator resolution cache is keyed by the compiled call signature, not the literal, so varying the text avoids the plan cache without changing the key the resolution cache tests for. If a shared-static issue between two instances in one process prevents a clean restart (see the `NOTE:` on `TestServerInstance`), fall back to a prepared-but-never-executed variant: prepare the third `create operator Foo(const X : Integer)...` after Bar is cached, discard the prepare (which runs the compile-time clear once but does not change the catalog), and re-cache Bar's loading-time choice through another trigger — if even that is not reachable from the harness, record the arm's reproduction as a known gap in the ticket's handoff and park a tripwire at the fix site.

- **Conversion-path arm.** Static only (prepared `create type ... like` never executed, another session traces a path through the temporary narrowing, then the prepare is discarded and the cached path is the only record of the target type). If a reproduction is straightforward within `SchemaCacheInvalidationTest`, add it; otherwise record it in the handoff as a static defect fixed together with arm 2 and leave the one assertion the check site enforces as the regression guard.

No test for the cache-read path or the lock order (both checked by review); no test for the clears (the existing `SchemaCacheInvalidationTest` covers them).

## Docs

Update the K32 row (or whichever Known-defects row owns these shared caches today) in `docs/modernization-plan.md` to say the shared caches now also hold only answers computed from the shared catalog — not only answers kept across DDL. The `OperatorResolutionCache` and `ScalarConversionPathCache` XML remarks already describe who clears what; extend them to say that `Compiler.ResolveOperator` and `Compiler.FindScalarConversionPath` now skip the cache add when the resolution came from a plan's private catalog or (for operators) from a loading-context compile.

No migration note: no public signature changes, no observable behaviour changes for correct programs. The only observable change is that a `create operator` which fails to compile, or is prepared and discarded, no longer leaves a phantom overload callable on every session.

## Source hygiene

Edited files keep their tabs, CRLF line endings and byte-order-mark state. No reformatting of untouched lines. Comments state the invariant ("shared cache holds only shared-catalog answers"), not the mechanism.

## TODO

Phase 1 — fix

- Edit `Compiler.ResolveOperator` to bypass the shared cache (neither read nor write) when `plan.InLoadingContext()` is true.
- In the same method, gate the cache `Add` on `plan.Catalog.Contains(context.Operator)` so a resolution to an operator only in `plan.PlanCatalog` is not cached.
- Edit `Compiler.FindScalarConversionPath` to skip `plan.Catalog.ConversionPathCache.Add(...)` when the chosen path contains any conversion in `plan.PlanCatalog`.

Phase 2 — tests

- Add the arm-2 test to `SchemaCacheInvalidationTest` (prepared, never executed, phantom overload callable); assert it throws `OperatorNotFound` after the fix.
- Add the arm-1 test using the template restart pattern in `TestServerInstance`; assert `Foo(5)` returns `'Integer'` after Bar has loaded. If restart in-process is impractical, record the gap and park a tripwire at the fix site.
- Optional: add the conversion-path arm (prepared-never-executed `create type ... like`); otherwise note the static-only coverage in the handoff.

Phase 3 — docs and build

- Update K32 (or the owning row) in `docs/modernization-plan.md` and the XML remarks on `OperatorResolutionCache` and `ScalarConversionPathCache`.
- `dotnet build Dataphor.slnx` with no new warnings in the edited files.
- Run the suite in the three parts documented in `AGENTS.md`; confirm the two new tests pass and the existing `SchemaCacheInvalidationTest` tests still pass.
