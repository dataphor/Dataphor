description: Reviewer verifies that the shared operator resolution and conversion path caches hold only answers computed from the shared catalog — not a resolution to an operator only in a plan's private catalog, not a resolution made during loading-context compile, and not a conversion path through a conversion only in a plan's private catalog.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Compiling/OperatorResolutionCache.cs, src/engine/DAE/Schema/ScalarType.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, docs/modernization-plan.md
----
## What changed

Two call sites in `src/engine/DAE/Compiling/Compiler.cs` now narrow what the shared caches accept.

`Compiler.ResolveOperator` (~line 9568). The block guarded by `#if USEOPERATORRESOLUTIONCACHE` now splits in two. When `plan.InLoadingContext()` is true it bypasses the shared cache entirely (neither reads nor writes), resolves against `plan.PlanCatalog` and then `plan.Catalog`, and does not call `CatalogDeviceSession.ResolveOperatorName` (the loading arm never did). Otherwise it uses the previous lookup-then-resolve-then-add shape under the outer `lock (plan.Catalog)` + inner `lock (plan.Catalog.OperatorResolutionCache)`, but the add is gated on `plan.Catalog.Contains(context.Operator)` — a resolution to an operator only in `plan.PlanCatalog` is no longer cached. The existing session / A/T / non-scalar exclusions, both locks, and the lock order are unchanged.

`Compiler.FindScalarConversionPath` (~line 11604). Before `plan.Catalog.ConversionPathCache.Add(...)` the chosen `context.BestPath` is now scanned for any `Schema.Conversion` in `plan.PlanCatalog`; if one is present the add is skipped. The read side and the catalog/cache locks are unchanged.

The XML remarks on `OperatorResolutionCache` and `ScalarConversionPathCache` state the new invariant. `docs/modernization-plan.md` gets a new K34 row describing the defect and the fix; K32 is left intact. No migration note was added: no public signature changes, no observable behaviour changes for correct programs, and the only observable change is that a `create operator` which fails to compile, or is prepared and discarded, no longer leaves a phantom overload callable on every session.

## Tests

Two new tests in `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs`, both verified to fail without the fix and pass with it:

- `LoadingContextOperatorResolutionDoesNotPoisonSharedCache` — builds a `TestServerInstance` template with `Foo(Decimal)`, `Bar() := Foo(5)`, `Foo(Integer)` created in that order, stops it, starts a fresh instance from the template, evaluates `Bar()` (whose loading-context compile resolves `Foo(5)` against the catalog as the load has it — only `Foo(Decimal)`), then evaluates `Foo(5)` on another session and asserts it returns `'Integer'`. Before the fix the second call returned `'Decimal'` because Bar's loading-context resolution leaked into the shared cache; after it returns `'Integer'` because the loading arm never writes. Confirmed by temporarily disabling the loading-context bypass: the assertion fails with `Expected: "Integer" But was: "Decimal"`.
- `FailedCreateOperatorDoesNotCachePhantomResolution` — prepares `create operator G(const X : Integer) : Integer begin if X <= 0 then result := 0 else result := G(X - 1) + 1; result := NoSuchOperatorAtAll_<unique>(); end;`; the second body statement cannot resolve, so `CompileStatement` replaces it with a `NoOpNode` and records the error on `plan.Messages`. The first statement's recursive call compiled cleanly and populated the shared cache. The test does not execute the prepared plan (so no by-name clear runs) and asserts that `G(5)` on another session throws `CompilerException.Codes.OperatorNotFound`. Before the fix, `G(5)` returned `5` from the phantom overload; after the fix the cache entry is never written and the operator is not in the shared catalog. Confirmed by temporarily reverting the `plan.Catalog.Contains` gate: the assertion fails with `Expected: <CompilerException> But was: null`.

No regression test for the conversion path arm: reproducing it inside `SchemaCacheInvalidationTest` would need a prepared-but-never-executed `create type ... like` through which another session traces a path before the first session discards its prepare. The one call to `plan.PlanCatalog.Contains(conversion)` at the check site enforces the invariant as its own regression guard; the arm is fixed together with the operator arm. If the reviewer wants that arm covered by a test, filing a small debt ticket is reasonable, but the ticket argued it was static-only and the compiler code backs that up.

No test for the cache-read path, the lock order, or the three existing clears: all are either enforced by the same code that the review will inspect, or already covered by the five existing tests in `SchemaCacheInvalidationTest`, which all still pass.

## Build and test

`dotnet build Dataphor.slnx` completes with 30 warnings and 0 errors; no new warnings in the edited files.

The three-part suite was run as documented in `AGENTS.md`:

- Part 1 (everything except coverage scripts) — 13 tests in Common.Tests, 61 in DAE.Server.Tests, all passed.
- Part 2 (coverage scripts A–R) — 67 passed, 11 skipped (fragments / known failures), 0 failed.
- Part 3 (coverage scripts not in A–R) — 48 passed, 13 skipped, 1 failed. The failure is the coverage script `ServerLink` — a pre-existing known machine-specific failure in `tickets/.pre-existing-known.md`. The confirm protocol (`(New-Object Net.Sockets.TcpClient).ConnectAsync('127.0.0.1',8061).Wait(5000)` returns `False` after 5 s) was run and matches the record: the machine drops loopback connects to closed ports instead of refusing them, so the script's server link waits past the harness timeout. Not caused by this ticket.

## What to look at

- The restructured `ResolveOperator` keeps two parallel code paths for loading vs non-loading context; the non-loading path matches the previous structure one-for-one, so a careful diff should show only the `Contains` gate added and a new preceding `if (plan.InLoadingContext()) { ... } else { ... }` wrapping the existing locked region. Verify the `#if USEOPERATORRESOLUTIONCACHE` arrangement still strips cleanly when the define is off (the three ResolveCall statements and the duplicate loading-context arm must leave only one plain ResolveCall sequence).
- The added `plan.Catalog.Contains(context.Operator)` uses the base `Objects<T>.InternalIndexOfName(item.Name)` — operators are keyed in the catalog by their signature-qualified name, so a hit means the specific overload is in the shared catalog. A session or A/T operator with a scalar signature would have been excluded already by the existing check; the new gate adds the stricter "in `plan.Catalog`" requirement.
- The `FindScalarConversionPath` scan walks `context.BestPath` under the outer `lock (plan.Catalog)` so `plan.PlanCatalog` is not changing underneath. A cheaper check (one conversion) exists if the path always has length 1 for the `create type ... like` case, but K31 / K32 made no such assumption and neither does this fix.
- The K34 row and the two cache remarks state the same invariant three times. If the reviewer prefers consolidating into K32, say so and I will fold them.

## Known gaps

- **Conversion-path arm has no test.** Fixed by inspection; the check-site assertion is the regression guard. Filed in the handoff as a known gap, not as a tripwire.
- **The loading-context test's first evaluation assumes `Bar()` is lazy-loaded on first use** (the normal catalog behaviour). If a change elsewhere made the server eagerly load every stored operator on startup, Bar would be compiled during `fresh.Server.Start()`, before the test observes it; the test would still detect the bug (the poisoned cache entry persists into the second Evaluate) but the "lazy load" wording in the test comment would read wrong. No action today; worth flagging if that behaviour ever changes.
