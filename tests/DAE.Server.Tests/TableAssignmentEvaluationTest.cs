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
	/// copying it, and a reset defaults to Close(); Open(); - a second run of the whole source pipeline - for every derived
	/// table that does not override it with a rewind. The rows copied were that second pass's, so an assignment from a
	/// non-deterministic expression stored values the script had never computed, silently
	/// (bug-table-assignment-evaluates-source-twice).
	/// </summary>
	/// <remarks>
	/// Both halves of the property are asserted. The evaluation count alone would pass an engine that evaluated once and stored
	/// the wrong pass, and the stored values alone would pass one whose extra pass ran before the stored one rather than after.
	/// </remarks>
	[TestFixture]
	public class TableAssignmentEvaluationTest : InProcessTestFixture
	{
		/// <summary>
		/// Two source shapes, because the extra evaluation reached them by different routes. A restriction the compiler can
		/// serve from an ordered scan resets by closing and re-opening its own cursor. A redefine - the shape the compiler also
		/// inserts to convert a column of a user-defined scalar type - is rewritten as an add, a remove and a rename, and resets
		/// by delegating down that chain to the add, which closes and re-opens. A restriction the compiler cannot turn into a
		/// scan rewinds its source instead and never re-evaluated it, so there is nothing to assert for that shape.
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
				create table TScan in System.Temp { ID : Integer, V : Integer, key { ID } };
				create table TRedefine in System.Temp { ID : Integer, V : Integer, key { ID } };
			");

			// One Assert.Multiple over both shapes, so that a shape-specific regression is not hidden by the other shape's failure.
			Assert.Multiple
			(
				() =>
				{
					AssertSourceEvaluatedOnce("TScan", "(table { row { Bump() ID, 1 V }, row { Bump() ID, 2 V } }) where V > 0");
					AssertSourceEvaluatedOnce("TRedefine", "(table { row { Bump() ID, 1 V }, row { Bump() ID, 2 V } }) redefine { V := V + 0 }");
				}
			);
		}

		/// <summary>
		/// Assigns the given two-row source to the named table and asserts that the source was evaluated exactly once and that
		/// the rows stored are that evaluation's. Bump() returns the counter after incrementing it, so one evaluation leaves the
		/// counter at 2 and stores the ids 1 and 2, where a second evaluation would leave 4 and store 3 and 4.
		/// </summary>
		private void AssertSourceEvaluatedOnce(string tableName, string source)
		{
			ExecuteScript(String.Format("update Counter set {{ N := 0 }}; {0} := {1};", tableName, source));

			Assert.That(Evaluate("Counter[1].N"), Is.EqualTo(2), source + ": the two-row source expression was not evaluated exactly once.");
			Assert.That(Evaluate(String.Format("Min(ID from {0})", tableName)), Is.EqualTo(1), source + ": the assignment stored an evaluation other than the first.");
			Assert.That(Evaluate(String.Format("Max(ID from {0})", tableName)), Is.EqualTo(2), source + ": the assignment stored an evaluation other than the first.");
		}

		/// <summary>Reads an Integer expression on the fixture's own process, so that the whole case runs in one session.</summary>
		private int Evaluate(string expression)
		{
			return (int)Process.Evaluate(expression, null).AsNative;
		}
	}
}
