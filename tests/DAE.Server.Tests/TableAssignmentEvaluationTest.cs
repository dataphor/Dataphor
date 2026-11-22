/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A table assignment ("MyTable := &lt;expression&gt;") evaluates its source expression exactly once, whatever shape that
	/// expression has. Materializing the source went through Table.CopyNativeAs, which reset an already-open cursor before
	/// copying it, and a reset of a derived table (a restriction, a redefine) is Close(); Open(); - a second run of the whole
	/// source pipeline. The rows copied were that second pass's, so an assignment from a non-deterministic expression stored
	/// values the script had never computed, silently (bug-table-assignment-evaluates-source-twice).
	/// </summary>
	/// <remarks>
	/// Both halves of the property are asserted. The evaluation count alone would pass an engine that evaluated once and stored
	/// the wrong pass, and the stored values alone would pass one whose extra pass ran before the stored one rather than after.
	/// </remarks>
	[TestFixture]
	public class TableAssignmentEvaluationTest : InProcessTestFixture
	{
		/// <summary>
		/// The restriction shape stands for every non-literal source: the conversion redefine the compiler inserts for a
		/// user-defined scalar column, and the deeper nestings, reach the same copy by the same route.
		/// </summary>
		[Test]
		public void AssignmentEvaluatesANonDeterministicSourceOnce()
		{
			ExecuteScript(@"
				create table Counter in System.Temp { ID : Integer, N : Integer, key { ID } };
				insert table { row { 1 ID, 0 N } } into Counter;
				create operator Bump() : Integer
				begin
					update Counter set { N := N + 1 } where ID = 1;
					result := Counter[1].N;
				end tags { DAE.IsDeterministic = 'false', DAE.IsRepeatable = 'false' };
				create table T in System.Temp { ID : Integer, V : Integer, key { ID } };
			");

			ExecuteScript("T := (table { row { Bump() ID, 1 V }, row { Bump() ID, 2 V } }) where V > 0;");

			Assert.Multiple
			(
				() =>
				{
					Assert.That(Evaluate("Counter[1].N"), Is.EqualTo(2), "The two-row source expression was not evaluated exactly once.");
					Assert.That(Evaluate("Min(ID from T)"), Is.EqualTo(1), "The assignment stored an evaluation other than the first.");
					Assert.That(Evaluate("Max(ID from T)"), Is.EqualTo(2), "The assignment stored an evaluation other than the first.");
				}
			);
		}

		/// <summary>Reads an Integer expression on the fixture's own process, so that the whole case runs in one session.</summary>
		private int Evaluate(string expression)
		{
			return (int)Process.Evaluate(expression, null).AsNative;
		}
	}
}
