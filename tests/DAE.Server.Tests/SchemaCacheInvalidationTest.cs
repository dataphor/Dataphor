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
	/// them) and before it executed (which changes the catalog). PrepareStatement holds the statement between the two.
	/// </summary>
	/// <remarks>
	/// Each test's last expression differs in text from the one compiled between compile and execute, because the server's plan
	/// cache, keyed by statement text, also keeps the old answer after these statements (bug-plan-cache-survives-conversion-and-overload-ddl).
	/// </remarks>
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
	}
}
