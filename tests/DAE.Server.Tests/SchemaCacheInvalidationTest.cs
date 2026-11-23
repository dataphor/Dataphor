/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Compiling;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The catalog's shared conversion path and operator resolution caches must not keep an answer computed from the catalog as it
	/// stood before a DDL statement changed it, including one another session computed after the statement compiled (which clears
	/// them) and before it executed (which changes the catalog). PrepareStatement holds the statement between the two. The plan
	/// cache, keyed by statement text, is the third such memo, and a compiled create statement is its sharpest case: the plan
	/// holds the catalog object the compiler built, so a drop has to evict it or the next run of the same text re-registers the
	/// object that was dropped.
	/// </summary>
	/// <remarks>
	/// The first two tests use different expression text after the DDL so they measure the compile-to-execute window, not the
	/// plan cache; the ...InvalidatesCachedPlans tests below cover the plan cache case by running the identical text twice -
	/// three over the caches above, and four over a create statement repeated after the object it makes has been dropped.
	/// </remarks>
	[TestFixture]
	public class SchemaCacheInvalidationTest : DeviceClassAttributeFixture
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

			CompilerException exception = Assert.Throws<CompilerException>(() => Evaluate(String.Format("{0}({1}(6))", takes, a)), "the dropped conversion is still applied");
			Assert.That(exception.Code, Is.EqualTo((int)CompilerException.Codes.OperatorNotFound), exception.ToString());
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

		// The plan cache keys compiled plans by statement text, so these evaluate the identical text before and after the DDL.

		[Test]
		public void DropConversionInvalidatesCachedPlans()
		{
			string a = Unique("PCA"), b = Unique("PCB"), takes = Unique("PCTakes");
			ExecuteScript(String.Format(@"
				create type {0} {{ representation {0} {{ V : Integer }} }};
				create type {1} {{ representation {1} {{ V : Integer }} }};
				create operator {0}To{1}(const X : {0}) : {1} begin result := {1}(X.V); end;
				create conversion {0} to {1} using {0}To{1} widening;
				create operator {2}(const X : {1}) : Integer begin result := X.V; end;
			", a, b, takes));

			string call = String.Format("{0}({1}(5))", takes, a);
			Assert.That(Evaluate(call), Is.EqualTo(5));
			ExecuteScript(String.Format("drop conversion {0} to {1};", a, b));

			CompilerException exception = Assert.Throws<CompilerException>(() => Evaluate(call), "a cached plan still applies the dropped conversion");
			Assert.That(exception.Code, Is.EqualTo((int)CompilerException.Codes.OperatorNotFound), exception.ToString());
		}

		[Test]
		public void CreateConversionInvalidatesCachedPlans()
		{
			// A to C through B adds 1100; the direct conversion created afterwards adds nothing.
			string a = Unique("PCA"), b = Unique("PCB"), c = Unique("PCC"), takes = Unique("PCTakes");
			ExecuteScript(String.Format(@"
				create type {0} {{ representation {0} {{ V : Integer }} }};
				create type {1} {{ representation {1} {{ V : Integer }} }};
				create type {2} {{ representation {2} {{ V : Integer }} }};
				create operator {0}To{1}(const X : {0}) : {1} begin result := {1}(X.V + 100); end;
				create conversion {0} to {1} using {0}To{1} widening;
				create operator {1}To{2}(const X : {1}) : {2} begin result := {2}(X.V + 1000); end;
				create conversion {1} to {2} using {1}To{2} widening;
				create operator {0}To{2}(const X : {0}) : {2} begin result := {2}(X.V); end;
				create operator {3}(const X : {2}) : Integer begin result := X.V; end;
			", a, b, c, takes));

			string call = String.Format("{0}({1}(5))", takes, a);
			Assert.That(Evaluate(call), Is.EqualTo(1105));
			ExecuteScript(String.Format("create conversion {0} to {1} using {0}To{1} widening;", a, c));

			Assert.That(Evaluate(call), Is.EqualTo(5), "a cached plan still converts through the longer path");
		}

		[Test]
		public void CreateOperatorInvalidatesCachedPlans()
		{
			string op = Unique("PCOp");
			ExecuteScript(String.Format("create operator {0}(const X : Decimal) : String begin result := 'Decimal'; end;", op));

			string call = String.Format("{0}(5)", op);
			Assert.That(Evaluate(call), Is.EqualTo("Decimal"));
			ExecuteScript(String.Format("create operator {0}(const X : Integer) : String begin result := 'Integer'; end;", op));

			Assert.That(Evaluate(call), Is.EqualTo("Integer"), "a cached plan still calls the worse overload");
		}

		// A create statement is compiled into the catalog object it makes, not into a copy of its own text, so a cached create
		// plan re-executed after the object has been dropped re-registers the object the first run built - carrying whatever the
		// statements in between did to it. These run the create through IServerProcess.Execute, which consults the plan cache;
		// ExecuteScript does not.

		[Test]
		public void DropDeviceInvalidatesCachedPlans()
		{
			string device = Unique("PCDevice");
			string create = String.Format("create device {0} class \"System.MemoryDevice\" attributes {{ \"MaxRowCount\" = \"100\" }};", device);

			Process.Execute(create, null);
			Process.Execute(String.Format("alter device {0} alter class {{ alter \"MaxRowCount\" = \"200\" }};", device), null);
			Process.Execute(String.Format("drop device {0};", device), null);
			Process.Execute(create, null);

			Assert.That(MaxRowCount(device), Is.EqualTo(100), "a cached plan brought the dropped device back with the altered row limit");
			Assert.That(Script(device), Does.Contain("\"MaxRowCount\" = \"100\""));
		}

		// The tags arm of the same statement. It goes wrong for a reason of its own: a create statement hands the object it builds
		// its own MetaData, so an alter of the object's tags writes into the statement as well (an arm of
		// blocked/bug-view-tags-lose-to-tags-inherited-from-the-source). Either defect alone brings the altered tag back.
		[Test]
		public void DropDeviceInvalidatesCachedPlansThatCarryTags()
		{
			string device = Unique("PCTagDevice");
			string create = String.Format("create device {0} class \"System.MemoryDevice\" tags {{ Scenario = \"first\" }};", device);

			Process.Execute(create, null);
			Process.Execute(String.Format("alter device {0} alter tags {{ alter Scenario = \"second\" }};", device), null);
			Process.Execute(String.Format("drop device {0};", device), null);
			Process.Execute(create, null);

			Assert.That(Script(device), Does.Contain("Scenario = \"first\""), "a cached plan brought the dropped device back with the altered tag");
		}

		// A server link, where the same defect sends the work to another server: the link's host, instance and port are read off
		// its tags into properties of the live object when the create statement executes.
		[Test]
		public void DropServerLinkInvalidatesCachedPlans()
		{
			string link = Unique("PCLink");
			string create = String.Format("create server {0} tags {{ HostName = \"localhost\", InstanceName = \"Dataphor\" }};", link);

			Process.Execute(create, null);
			Process.Execute(String.Format("alter server {0} alter tags {{ alter HostName = \"elsewhere\" }};", link), null);
			Process.Execute(String.Format("drop server {0};", link), null);
			Process.Execute(create, null);

			Assert.That(
				CatalogObject<Schema.ServerLink>(link).HostName,
				Is.EqualTo("localhost"),
				"a cached plan brought the dropped link back pointing at the host the alter gave it");
			Assert.That(Script(link), Does.Contain("HostName = \"localhost\""));
		}

		// A role, the third object kind whose create statement carries metadata a later statement can alter.
		[Test]
		public void DropRoleInvalidatesCachedPlans()
		{
			string role = Unique("PCRole");
			string create = String.Format("create role {0} tags {{ Scenario = \"first\" }};", role);

			Process.Execute(create, null);
			Process.Execute(String.Format("alter role {0} alter tags {{ alter Scenario = \"second\" }};", role), null);
			Process.Execute(String.Format("drop role {0};", role), null);
			Process.Execute(create, null);

			Assert.That(Script(role), Does.Contain("Scenario = \"first\""), "a cached plan brought the dropped role back with the altered tag");
		}

		// Loading a stored operator from the catalog compiles its body in a loading context, where only the
		// overloads the catalog has brought in so far are visible. A resolution computed against that narrower
		// view must not be written to the shared cache, or one session's loading-time choice becomes every
		// session's answer and the better overload created later is never picked.
		[Test]
		public void LoadingContextOperatorResolutionDoesNotPoisonSharedCache()
		{
			string foo = Unique("LCFoo"), bar = Unique("LCBar");
			// Bar is created when only Foo(Decimal) exists, so its stored dependency is Foo(Decimal). The newer
			// Foo(Integer) overload, created afterwards, is the exact match a direct call to Foo(5) must find.
			string create = String.Format(
				@"create operator {0}(const X : Decimal) : String begin result := 'Decimal'; end;
				  create operator {1}() : String begin result := {0}(5); end;
				  create operator {0}(const X : Integer) : String begin result := 'Integer'; end;",
				foo, bar);

			using (TestServerInstance template = new TestServerInstance("LoadingContextTemplate"))
			{
				template.Server.Start();
				template.ExecuteScript(create);
				template.Server.Stop();

				using (TestServerInstance fresh = new TestServerInstance("LoadingContextFresh", template))
				{
					fresh.Server.Start();
					// Evaluating Bar lazily loads it. Its loading-context compile resolves Foo(5) against the catalog
					// as the load has it so far — Foo(Decimal) only — and returns 'Decimal'. Before this fix that
					// resolution was written to the shared cache, so another session asking for Foo(5) got the
					// cached Foo(Decimal) instead of the newer, exact-match Foo(Integer).
					Assert.That(fresh.Evaluate(String.Format("{0}()", bar)), Is.EqualTo("Decimal"));
					Assert.That(
						fresh.Evaluate(String.Format("{0}(5)", foo)),
						Is.EqualTo("Integer"),
						"a loading-context resolution leaked into the shared operator resolution cache");
				}
			}
		}

		// A create operator whose body references its own new signature puts the new operator in plan.PlanCatalog
		// while compiling. If the compile reports errors (or the statement is prepared and never executed), the
		// operator never reaches plan.Catalog and the compile-time by-name clear of the shared resolution cache
		// never re-fires on commit; the recursive call's resolution must therefore not be cached, or the entry is
		// the only record of a phantom overload any session can call.
		[Test]
		public void FailedCreateOperatorDoesNotCachePhantomResolution()
		{
			string op = Unique("SCPhantom"), noSuch = "NoSuchOperatorAtAll_" + op;
			string create = String.Format(
				"create operator {0}(const X : Integer) : Integer begin if X <= 0 then result := 0 else result := {0}(X - 1) + 1; result := {1}(); end;",
				op, noSuch);

			IServerStatementPlan prepared = Process.PrepareStatement(create, null);
			try
			{
				// The second body statement fails to resolve, so CompileStatement replaces it with a NoOpNode and
				// records the error on plan.Messages. The create reports errors from Execute (never called) but the
				// recursive first statement compiled cleanly and populated the shared cache before the error.
				CompilerException exception = Assert.Throws<CompilerException>(
					() => Evaluate(String.Format("{0}(5)", op)),
					"the recursive call left a phantom entry in the shared operator resolution cache");
				Assert.That(exception.Code, Is.EqualTo((int)CompilerException.Codes.OperatorNotFound), exception.ToString());
			}
			finally
			{
				Process.UnprepareStatement(prepared);
			}
		}
	}
}
