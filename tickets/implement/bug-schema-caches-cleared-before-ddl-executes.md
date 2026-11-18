description: The server's shared memos of conversion paths and operator resolutions are cleared when a statement that changes conversions or operators is compiled, not when it runs, so another session compiling in between puts the old answer back and every session keeps using it after the change. Clear them again at the moment the catalog actually changes.
architecture: docs/modernization-plan.md#known-defects
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Compiling/OperatorResolutionCache.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/modernization-plan.md
repro: verified
----
## The defect

Each catalog keeps two memos shared by all its sessions:

- the **conversion path cache** (`Catalog.ConversionPathCache`, class `ScalarConversionPathCache` in `src/engine/DAE/Schema/ScalarType.cs`): for a pair of scalar types, the chain of implicit conversions the compiler found between them;
- the **operator resolution cache** (`Catalog.OperatorResolutionCache`, `src/engine/DAE/Compiling/OperatorResolutionCache.cs`): for an operator name and call signature, which overload was chosen and the conversions applied to the arguments.

Both are invalidated only by the compiler, while it compiles the DDL statement that makes them stale (`Compiler.CompileCreateConversionStatement` ~line 9787, `CompileDropConversionStatement` ~9812, `create type ... like` ~5802, and the `OperatorResolutionCache.Clear(...)` calls for create/drop operator ~6532, ~6864, ~10298 and the generated selectors, accessors and specials ~4767–5444). The catalog itself changes later, when the plan executes:

- `CreateConversionNode` / `DropConversionNode` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs` ~296–335) → `CatalogDeviceSession.CreateConversion` / `DropConversion` → `AddImplicitConversion` / `RemoveImplicitConversion` (`CatalogDeviceSession.cs` ~2194–2230);
- `CreateOperatorNode` / `DropOperatorNode` → `CatalogDeviceSession.CreateOperator` / `DropOperator` (~2321, ~2369). Every operator create and drop goes through these two methods, including the generated operators of `create type` and `alter type` (they run `new CreateOperatorNode(...)` / `new DropOperatorNode(...)`; checked every caller).

A session that compiles between the two finds the memo empty, works the answer out from the catalog as it still stands, and stores it; nothing clears it after the change. Only rollback invalidates at the point of change today (`CatalogDeviceSession.InvalidateCachesAfterUndo`). `DropScalarTypeNode.InternalExecute` already clears the operator resolution cache at execute time — the pattern this ticket extends.

### Reproduction (verified in the fix stage)

Holding the DDL between compile and execute needs no threads: `Process.PrepareStatement(ddl, null)` compiles it (and runs the compile-time clears), then another session compiles a use, then `plan.Execute(null)` changes the catalog. Both tests below failed at HEAD (`DropConversion…`: the expression still compiled through the dropped conversion; `CreateOperator…`: returned `"Decimal"` instead of `"Integer"`). The same sequences with the use compiled *before* the DDL is prepared passed, so the failure is the window. Prototyping the TODO's four execute-time clears (without the locking change) made both pass and kept `ConcurrentDDLTest` green; the prototype was reverted.

Each test's last expression uses different text from the one compiled in the window (`(6)` vs `(5)`), because the server's plan cache, keyed by statement text, also keeps the old answer after these DDL statements — a separate defect filed as `backlog/bug-plan-cache-survives-conversion-and-overload-ddl`. Do not change the texts to match.

```csharp
/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The catalog's shared conversion path and operator resolution caches must not keep an answer computed from the catalog as it
	/// stood before a DDL statement changed it, including one another session computed after the statement compiled (which clears
	/// them) and before it executed (which changes the catalog). PrepareStatement holds the statement between the two.
	/// </summary>
	[TestFixture]
	public class SchemaCacheInvalidationTest : InProcessTestFixture
	{
		private int _next;

		private string Unique(string prefix) { return prefix + (++_next).ToString(); }

		// Evaluate on a session of its own: the answer it caches is shared with every session.
		private object Evaluate(string expression)
		{
			return Instance.Evaluate(expression);
		}

		[Test]
		public void DropConversionInvalidatesAnswersCachedBetweenCompileAndExecute()
		{
			string a = Unique("SCA"), b = Unique("SCB"), takes = Unique("SCTakes");
			ExecuteScript(String.Format(@"
				create type {0} {{ representation {0} {{ V : Integer }} }};
				create type {1} {{ representation {1} {{ V : Integer }} }};
				create operator {0}To{1}(const X : {0}) : {1} begin result := {1}(X.V); end;
				create conversion {0} to {1} using {0}To{1} widening;
				create operator {2}(const X : {1}) : Integer begin result := X.V; end;
			", a, b, takes));

			IServerStatementPlan drop = Process.PrepareStatement(String.Format("drop conversion {0} to {1};", a, b), null);
			try
			{
				Assert.That(Evaluate(String.Format("{0}({1}(5))", takes, a)), Is.EqualTo(5));
				drop.Execute(null);
			}
			finally
			{
				Process.UnprepareStatement(drop);
			}

			Assert.Throws(Is.InstanceOf<Exception>(), () => Evaluate(String.Format("{0}({1}(6))", takes, a)), "the dropped conversion is still applied");
		}

		[Test]
		public void CreateOperatorInvalidatesAnswersCachedBetweenCompileAndExecute()
		{
			string op = Unique("SCOp");
			ExecuteScript(String.Format("create operator {0}(const X : Decimal) : String begin result := 'Decimal'; end;", op));

			IServerStatementPlan create = Process.PrepareStatement(String.Format("create operator {0}(const X : Integer) : String begin result := 'Integer'; end;", op), null);
			try
			{
				Assert.That(Evaluate(String.Format("{0}(5)", op)), Is.EqualTo("Decimal"));
				create.Execute(null);
			}
			finally
			{
				Process.UnprepareStatement(create);
			}

			Assert.That(Evaluate(String.Format("{0}(6)", op)), Is.EqualTo("Integer"));
		}
	}
}
```

Tighten the `Assert.Throws` to the actual exception type/code once seen (the fix stage only checked that it throws after the prototype fix; it did not record the type).

## Design

**Keep the compile-time clears; add execute-time ones.** The compile-time clears serve the compiling session: the compiler looks the resolution up in the shared cache *before* it consults the plan's own catalog (`Compiler.ResolveOperator`, ~line 9566: cache lookup, then `ResolveCall(plan, plan.PlanCatalog, …)`), so without the clear a later statement in the same plan would get the old cached overload instead of the operator the plan just created. They do not serve anyone else, because the catalog has not changed yet.

**Where to clear at execute time:**

- `AddImplicitConversion`: inside its existing `lock (Catalog)`, after the add, clear **both caches whole** (`Catalog.ConversionPathCache.Clear()`, `Catalog.OperatorResolutionCache.Clear()`). The per-type overloads the compiler uses are incomplete for an added conversion: `ScalarConversionPathCache.Clear(scalarType)` removes paths that *contain* the type, but a new conversion A→B can also shorten a cached path Z→…→C that passes through neither A nor B before the change (Z→A and B→C already existing); likewise `OperatorResolutionCache.Clear(source, target)`. Conversion DDL is rare, and `InvalidateCachesAfterUndo` already clears whole for the same reason. (Static reasoning, not run.)
- `RemoveImplicitConversion`: inside its `lock (Catalog)`, after the remove, `ConversionPathCache.Clear(conversion)` and `OperatorResolutionCache.Clear(conversion)` — complete for a removal, since every affected entry contains the conversion.
- `CreateOperator` and `DropOperator`: after `InsertCatalogObject` / `DeleteCatalogObject`, `Catalog.OperatorResolutionCache.Clear(operatorValue.OperatorName)`. Clearing after the catalog change (outside the lock) is enough here: the compiler holds `lock (plan.Catalog)` across lookup, resolve and add, and `CacheCatalogObject`/`ClearCatalogObject` change the catalog under the same lock, so any entry is either computed after the change (correct) or before it (removed by this clear). Do not put the clear in `CacheCatalogObject`: the server also uses it for lazy loads of stored operators, which change no answer.
- These methods are also called in loading context (catalog load at startup, library registration) and by undo instructions; the clears are harmless there (empty or soon-rebuilt caches). If library registration shows conversion-path churn, skip the clears when `ServerProcess.InLoadingContext()`, with a `NOTE:`.

**Close the remaining race for conversion paths.** The operator resolution path is safe with the above because it runs under `lock (plan.Catalog)`. `Compiler.FindScalarConversionPath` (~line 11605) is also called outside any catalog lock (from `FindConversionPath` for type conversions, e.g. ~2519, ~11140, ~12282) and holds only `lock (plan.Catalog.ConversionPathCache)`: a trace that read `ImplicitConversions` before `AddImplicitConversion`/`RemoveImplicitConversion` can store its path after the new clear. The same unlocked trace can also iterate a source type's `ImplicitConversions` while the DDL modifies it. Take `lock (plan.Catalog)` around the existing `lock (plan.Catalog.ConversionPathCache)` block in `FindScalarConversionPath`. Lock order is Catalog → cache instance, which the operator resolution path already uses (it calls `ResolveCall` → `FindConversionPath` while holding both); nothing takes the cache instance lock and then the catalog lock, so this adds no deadlock (check `TraceScalarConversionPath` and its callees take no other lock first). Keep the `#if USECONVERSIONPATHCACHE` structure; the catalog lock should cover the trace in both branches. No deterministic test for this arm (timing only); say so in the known-defects row.

## TODO

- Add `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs` with the two tests above (UTF-8 with byte-order mark, tabs, CRLF, like the neighbouring test files) and its `<Compile Include="SchemaCacheInvalidationTest.cs" />` to `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` (explicit items; CRLF with byte-order mark). Run them and confirm both fail before the fix.
- `src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs` (UTF-8 with byte-order mark, CRLF): execute-time clears in `AddImplicitConversion`, `RemoveImplicitConversion`, `CreateOperator`, `DropOperator` as described. Update the `InvalidateCachesAfterUndo` remarks, which say the compiler clears these caches when it compiles a create or drop.
- `src/engine/DAE/Compiling/Compiler.cs` (no byte-order mark, CRLF): `lock (plan.Catalog)` around the cache block in `FindScalarConversionPath`. A one-line comment at `CompileCreateConversionStatement`'s and `CompileDropConversionStatement`'s clears saying they serve the compiling plan and the catalog session clears again at execution keeps the next reader from "fixing" one or the other.
- Add a K32 row (or the next free number) to the known-defects table in `docs/modernization-plan.md`: the compile-time-only invalidation, the two reproductions, the static locking arm; status fixed; workstream WS2 `bug-schema-caches-cleared-before-ddl-executes`.
- No migration note: no public signature changes, and the only visible effect is that compiles after conversion/operator DDL now see the change.
- Run `dotnet build Dataphor.slnx`, the new tests and `ConcurrentDDLTest`, then `dotnet test Dataphor.slnx` (coverage scripts must stay as the manifest records).
