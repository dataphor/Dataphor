/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A stack slot owns the value in it: whatever overwrites a slot releases what it displaced. Assignment did not,
	/// so every assignment to a variable of a type the engine keeps in a managed stream - Binary and Graphic - orphaned
	/// the stream behind the value it replaced, for the life of the session
	/// (bug-assigning-a-binary-variable-leaks-its-stream). The last three cases are the limits of that rule: slots
	/// whose value belongs to something else, which an assignment must leave alone.
	/// </summary>
	/// <remarks>
	/// The leak cases assert on Diagnostics.StreamCount(), which reports how many streams the server is holding, read
	/// before and after each script: a script that declares its own variables and runs to completion must leave the
	/// count where it found it. Exact numbers, because the assertion is arithmetic rather than a threshold. The loop
	/// cases assign many times rather than once, so that a regression reports the full count of orphans instead of one
	/// and cannot be mistaken for an off-by-one somewhere else.
	///
	/// No script here leaves the block that declared its variables by a raise, and the one case that does raise
	/// catches the error inside that block. A block left by a raise leaks the values of the variables it declared
	/// through a different site - the frame pop, which nulls the slots without releasing them, skipping the
	/// deallocation statements the compiler put at the end of the block. That is
	/// bug-an-error-inside-a-block-leaks-its-binary-values, which this fixture deliberately does not depend on.
	/// </remarks>
	[TestFixture]
	public class AssignmentValueOwnershipTest : InProcessTestFixture
	{
		/// <summary>How many times the loop cases assign, enough that a per-assignment leak is unmistakable.</summary>
		private const int AssignmentCount = 100;

		/// <summary>The row cases are an order of magnitude smaller: a row costs a stream per stream-backed column plus the row itself.</summary>
		private const int RowAssignmentCount = 10;

		[Test]
		public void AssigningABinaryVariableReleasesTheValueItReplaces()
		{
			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LB : Binary := Binary('0001');
							for LI : Integer := 1 to {0} do
								LB := Binary('0002');
							if LB.Value <> '0002' then
								raise Error('The last assignment did not reach the variable.');
						end;
					",
					AssignmentCount
				)
			);
		}

		[Test]
		public void AssigningARowVariableReleasesTheRowItReplaces()
		{
			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LR : row {{ B : Binary }} := row {{ Binary('0001') B }};
							for LI : Integer := 1 to {0} do
								LR := row {{ Binary('0002') B }};
							if LR.B.Value <> '0002' then
								raise Error('The last assignment did not reach the variable.');
						end;
					",
					RowAssignmentCount
				)
			);
		}

		/// <summary>
		/// A var parameter's slot is an alias of the caller's slot, not a copy, so the body's assignment releases the
		/// value the caller's slot still points at and the write-back after the call is what hands the new value over.
		/// Asserting the value as well as the count is what would catch a later change that gave the callee a copy:
		/// the count would still balance and the caller would silently stop seeing the assignment.
		/// </summary>
		[Test]
		public void AnOperatorThatAssignsToAVarParameterReleasesTheValueItReplaces()
		{
			ExecuteScript("create operator ZReplace(var AB : Binary) begin AB := Binary('0003'); end;");

			AssertScriptLeavesNoStreams
			(
				@"
					begin
						var LB : Binary := Binary('0001');
						ZReplace(var LB);
						if LB.Value <> '0003' then
							raise Error('The call did not hand the assigned value back to the caller.');
					end;
				"
			);
		}

		/// <summary>
		/// The same, for a body that raises after assigning. The value asserted here is the new behaviour: the caller
		/// now sees the callee's last assignment even though the call raised, where before this fix it saw the value
		/// the variable had before the call. The write-back used to sit on the success path only, which is both why
		/// the old value survived and why the assigned one leaked; it now runs on every exit path, and releasing what
		/// an assignment displaces would be a use-after-free without that. docs/migration-notes.md records the change.
		/// </summary>
		[Test]
		public void AnOperatorThatRaisesAfterAssigningToAVarParameterReleasesTheValueItReplaces()
		{
			ExecuteScript("create operator ZRaiseAfterAssign(var AB : Binary) begin AB := Binary('0004'); raise Error('ZRaiseAfterAssign'); end;");

			AssertScriptLeavesNoStreams
			(
				@"
					begin
						var LB : Binary := Binary('0001');
						var LCaught : Integer := 0;
						try
							ZRaiseAfterAssign(var LB);
						except
							on E : Error do
								LCaught := 1;
						end;
						if LCaught <> 1 then
							raise Error('The operator did not raise.');
						if LB.Value <> '0004' then
							raise Error('The raising call did not hand the assigned value back to the caller.');
					end;
				"
			);
		}

		/// <summary>
		/// An in parameter's slot holds a copy the callee owns, so a body that assigns to one releases that copy - and
		/// the cleanup the call ends with releases the operand value too. Both must name the same value, or the call
		/// releases the copy twice; the engine turns a second release of one stream into an error, so this case fails
		/// loudly rather than quietly. An in operand the body never assigns to is promoted to const when the operator
		/// is compiled, so an operand that is still in by the time the call runs is one the body does assign to: this
		/// is the only shape in which that cleanup releases anything.
		/// </summary>
		[Test]
		public void AnOperatorThatAssignsToAnInParameterReleasesTheValueItReplaces()
		{
			ExecuteScript("create operator ZReplaceIn(AB : Binary) begin AB := Binary('0005'); end;");

			AssertScriptLeavesNoStreams
			(
				@"
					begin
						var LB : Binary := Binary('0001');
						ZReplaceIn(LB);
						if LB.Value <> '0001' then
							raise Error('A by-value call reached the caller''s variable.');
					end;
				"
			);
		}

		/// <summary>
		/// The same variable passed to two by-reference parameters of one call. Each of the callee's slots gets its
		/// own value, so the body's two assignments release two different values rather than one value twice; the
		/// caller ends with the last write-back, which is what the call did before any of this. Such a call is a
		/// programming error - one of the two writes is always lost - and it still leaks the write that is lost, so
		/// the assertion here is on the value and on the call completing, not on the count.
		/// </summary>
		[Test]
		public void TheSameVariablePassedTwiceByReferenceStillRuns()
		{
			ExecuteScript("create operator ZTwo(var AB : Binary, var AC : Binary) begin AB := Binary('0006'); AC := Binary('0007'); end;");

			ExecuteScript
			(
				@"
					begin
						var LB : Binary := Binary('0001');
						ZTwo(var LB, var LB);
						if LB.Value <> '0007' then
							raise Error('The last write-back did not reach the caller.');
					end;
				"
			);
		}

		/// <summary>
		/// A foreach over a cursor lends its loop variable's slot the one row value it reads each row into, and goes
		/// on reading into that same value, so an assignment to the loop variable must not release it: releasing it
		/// left the next iteration selecting into a disposed row. Both forms of the loop variable are covered, because
		/// they are lent two different slots - one the loop pushes for itself, one belonging to a variable the script
		/// already declared - and are marked as lending them in two different places.
		/// </summary>
		/// <remarks>
		/// Assigning to a foreach loop variable is a programming error - the next iteration overwrites it - and it
		/// still leaks the value assigned, so these assertions are that the loop completes and sees every row, not on
		/// the stream count. A foreach over a list is not covered: a list operand is copied into the loop, so the
		/// elements it lends belong to that copy and no caller value is at risk.
		/// </remarks>
		[Test]
		public void AssigningToTheVariableOfAForEachOverACursorLeavesTheLoopIntact()
		{
			ExecuteScript("create table ZT in System.Temp { ID : Integer, B : Binary, key { ID } }; insert table { row { 1 ID, Binary('0001') B }, row { 2 ID, Binary('0002') B } } into ZT;");

			// One Assert.Multiple over both forms, so that a form-specific regression is not hidden by the other form's failure.
			Assert.Multiple
			(
				() =>
				{
					AssertForEachSeesBothRows
					(
						"a loop variable the loop declares",
						@"
							begin
								var LCount : Integer := 0;
								foreach var LRow in cursor(ZT) do
								begin
									LCount := LCount + 1;
									LRow := row { 9 ID, Binary('000B') B };
								end;
								if LCount <> 2 then
									raise Error('The loop did not see both rows.');
							end;
						"
					);

					AssertForEachSeesBothRows
					(
						"a loop variable the script declares",
						@"
							begin
								var LRow : row { ID : Integer, B : Binary };
								var LCount : Integer := 0;
								foreach LRow in cursor(ZT) do
								begin
									LCount := LCount + 1;
									LRow := row { 9 ID, Binary('000B') B };
								end;
								if LCount <> 2 then
									raise Error('The loop did not see both rows.');
							end;
						"
					);
				}
			);
		}

		/// <summary>Runs one of the loop forms above, failing with the form's name rather than only the engine's error.</summary>
		private void AssertForEachSeesBothRows(string form, string script)
		{
			Assert.That(() => ExecuteScript(script), Throws.Nothing, form + ": the loop did not survive an assignment to its variable.");
		}

		/// <summary>
		/// Runs the script on the fixture's own process and asserts that the server is holding the same number of
		/// streams afterwards as before. The script must declare and release its own variables, so that the count
		/// is back in balance by the time it returns.
		/// </summary>
		private void AssertScriptLeavesNoStreams(string script)
		{
			int before = StreamCount();
			ExecuteScript(script);
			Assert.That(StreamCount(), Is.EqualTo(before), "The script left streams behind.");
		}

		/// <summary>How many streams the server is holding, from Diagnostics.StreamCount(), which resolves unqualified.</summary>
		private int StreamCount()
		{
			return (int)Process.Evaluate("StreamCount()", null).AsNative;
		}
	}
}
