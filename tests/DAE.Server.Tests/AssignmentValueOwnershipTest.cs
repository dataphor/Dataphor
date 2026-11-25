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
	/// (bug-assigning-a-binary-variable-leaks-its-stream). The cases from TheSameVariablePassedTwiceByReference
	/// through AssigningThroughAScalarProperty are the limits of that rule: slots whose value belongs to something
	/// else, which an assignment must leave alone, and the two shapes in which a call used to hand one caller value
	/// to two callee slots. The three cases from AssigningAfterAForEachReleasesTheValueItReplaces are the two ends of
	/// that loan, each of which the engine got wrong: the mark on the slot was set and never cleared, so once a loop
	/// over a variable the script had declared itself had finished, every assignment to that variable for the rest of
	/// the scope released nothing; and the loop lent the slot with a store that releases nothing, so starting such a
	/// loop orphaned whatever the variable already held (bug-a-loop-variable-stays-borrowed-after-the-loop). The
	/// nested case is the one a fix to the second of those could have broken: two loops over one variable are legal
	/// D4, and the inner loop displaces an element the outer loop's source still owns, so a release there would be a
	/// release too many. AForEachOverAnOperandLeavesTheCallersValueAlone is the other case that release could have broken,
	/// and the one that fixing it turned up: a loop over one of the enclosing operator's own operands, where the value
	/// displaced may be the caller's. A const operand's value is never the loop's to release - the call site settles
	/// whether the call owns it - and an in operand's is, because the loop writing the slot is what stops the compiler
	/// promoting the operand to const and passing it by reference. The var shape of that case passes only because the
	/// caller's slot owns the value it passed, and AVarOperandGivenTheCallersLoopVariableLeavesTheLoopsValueAlone is
	/// the shape where it does not: the caller's own loop variable as the argument, holding a row or element the
	/// iterator owns and the loop goes on reading. A var operand's slot used to be an alias of the caller's whatever
	/// the caller passed, so an assignment in the body - or a foreach inside it - released a value the caller's loop
	/// was still using and the loop then read a released one; such an operand is now copied for the call, so the
	/// callee's slot owns what it releases
	/// (bug-a-var-parameter-written-in-the-body-can-release-a-callers-loop-value). Two of that case's shapes are
	/// measured against the equivalent direct assignment to a loop variable rather than against nought, because the
	/// write-back hands the caller's loop variable the operator's output and the next plain store into that slot
	/// displaces it without releasing it - the same accepted tradeoff as the direct assignment, and now the same size. The four cases from
	/// AConstOperandReleasesTheValueComputedForIt through ARowsColumnReadIntoSomethingThatOwnsItStaysReadable are the
	/// operand half of the same rule: whether a call owns the value it was handed for an operand is a property of the plan
	/// node that produced the argument - a node that reads a stack slot, or a column of a row in one, can lend the value
	/// it already holds, where a literal, a conversion or another call produces one nothing else holds - and not of the
	/// operand's modifier, which is all the call used to look at
	/// (bug-an-operator-argument-is-released-by-the-wrong-owner). Reading ownership off the modifier held a stream for the
	/// life of the session on every call with a const operand, which is most calls, and released a row's own column out
	/// from under the row whenever one was read.
	/// The two IfNil cases are the same rule at the one site that sits outside the machinery enforcing it: a node that
	/// overrides InternalExecute(Program) itself is never reached by CleanupOperand, and IfNil copied the argument it
	/// chose and released nothing, so every call over a value the call owned held a stream for the life of the session
	/// (bug-ifnil-leaks-the-value-it-copies); the second of the two is the loan, which that fix must not start releasing.
	/// The three row-equality cases are the same site again, and the pair of nodes that missed both halves of the rule at
	/// once: nothing asked either argument node to lend, so both copied the row they were handed, and the execute
	/// override then released neither copy - a stream per stream-backed column per side on every comparison, whether the
	/// rows were computed or read out of a variable (bug-row-equality-leaks-the-rows-it-compares). The first case's four
	/// shapes each reach a different part of that fix, the generic row operand among them, which is the run-time branch
	/// that plans a second comparison over the values it has just evaluated; the second case is the loan, read rather
	/// than counted; and the third is the equality the compiler generates for a compound scalar type, which leaked over
	/// the compound row behind the value for the same two reasons.
	/// ACallWhoseLaterArgumentRaisesReleasesTheArgumentsAlreadyEvaluated is the error path of that same operand
	/// cleanup: it used to be skipped altogether when an argument of a call raised, because the execute shapes
	/// evaluated their arguments before entering the try whose finally cleans them up, so every argument evaluated
	/// before the one that raised was orphaned (bug-an-argument-is-orphaned-when-a-later-one-raises).
	/// AFailedCallLeavesAVarOperandEvaluatedBeforeTheRaiseAlone is the limit of that release, and the reason the
	/// failure path cannot reuse the cleanup that runs after a call: a var operand has no callee value to write back
	/// when the call never happened, and writing one runs the type's validate handlers - user D4 - while the
	/// argument's error is unwinding. The last six
	/// cases are the other half of the same ownership rule: a scope releases the values of the variables it declared.
	/// That release used to be a statement the compiler appended to the end of the block, so only control reaching the
	/// end of the block ran it, and a raise, an exit, a break or a continue left every value behind
	/// (bug-an-error-inside-a-block-leaks-its-binary-values). The last two of those cover the one variable such a
	/// release can miss: an operator's result, which the paths that do reach the caller hand over rather than release.
	/// AForEachOverACursorReleasesTheStackItCopied is a fourth owner of values on the stack, beside a slot, a call and a
	/// scope: a cursor, which takes a private copy of the whole stack window it is opened over so that its plan can read
	/// the variables it was compiled against after the scope that opened it has gone. Closing the cursor threw that copy
	/// away without releasing it, so every cursor a script opened held one stream per stream-backed variable then in
	/// scope, for the life of the session (bug-a-cursor-copies-the-stack-and-never-gives-it-back).
	/// </summary>
	/// <remarks>
	/// The leak cases assert on Diagnostics.StreamCount(), which reports how many streams the server is holding, read
	/// before and after each script: a script that declares its own variables must leave the count where it found it,
	/// whether it ran to completion or raised. Exact numbers, because the assertion is arithmetic rather than a
	/// threshold. The loop cases assign, or continue, many times rather than once, so that a regression reports the
	/// full count of orphans instead of one and cannot be mistaken for an off-by-one somewhere else.
	///
	/// Every case whose script raises also asserts on the error - that it reached the caller, or that the script's own
	/// handler saw it - because a case that silently stopped raising would otherwise pass while testing nothing.
	/// </remarks>
	// NOTE: this fixture is the index of one rule and every ownership ticket adds to it - about 1,760 lines and a class
	// summary of sixty at bug-row-equality-leaks-the-rows-it-compares. It is past the roughly 1,500 lines at which it
	// should be split along the three rules the summary already names - a slot, a call's operands, a scope - rather than
	// growing the summary. The split is still deferred, and still waiting on sibling ownership tickets with cases in
	// flight against this file: bug-a-cursor-loop-variable-stays-stale-once-written and
	// bug-a-loop-over-a-read-only-parameter-orphans-a-computed-argument, both in the backlog, both naming cases here.
	// Whichever of them is taken first should do the split before adding its cases, as the first of the two already says.
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
		/// is compiled, so an operand that is still in by the time the call runs is one the body does assign to. The
		/// const case AConstOperandReleasesTheValueComputedForIt is the other shape in which that cleanup releases.
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

		/// <summary>
		/// A foreach lends its loop variable's slot for the loop and leaves the slot nil on the way out, so the slot
		/// owns its value again once the loop has finished and an assignment after the loop has to release what it
		/// replaces. All four forms the compiler can produce are covered, because the mark that says a slot is lent
		/// belongs to the variable's symbol rather than to the statement: the statement and the expression shape of
		/// foreach, each over a list and over a cursor.
		/// </summary>
		/// <remarks>
		/// Each form assigns many times over, so that a regression reports a count rather than an off-by-one. The
		/// slot is nil coming out of the loop, so the first assignment displaces nothing and a per-assignment
		/// regression leaks one fewer value than it assigns. One Assert.Multiple over the four forms, so that one
		/// form's failure does not hide the forms after it.
		/// </remarks>
		[Test]
		public void AssigningAfterAForEachReleasesTheValueItReplaces()
		{
			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"the statement form over a list",
						String.Format
						(
							@"
								begin
									var LB : Binary;
									var LCount : Integer := 0;
									foreach LB in {{ Binary('0001'), Binary('0002') }} do
										LCount := LCount + 1;
									if LCount <> 2 then
										raise Error('The loop did not see both elements.');
									for LI : Integer := 1 to {0} do
										LB := Binary('0003');
									if LB.Value <> '0003' then
										raise Error('The last assignment did not reach the variable.');
								end;
							",
							AssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"the statement form over a cursor",
						String.Format
						(
							@"
								begin
									var LR : row {{ B : Binary }};
									var LCount : Integer := 0;
									foreach LR in cursor(table {{ row {{ Binary('0001') B }} }}) do
										LCount := LCount + 1;
									if LCount <> 1 then
										raise Error('The loop did not see the row.');
									for LI : Integer := 1 to {0} do
										LR := row {{ Binary('0002') B }};
									if LR.B.Value <> '0002' then
										raise Error('The last assignment did not reach the variable.');
								end;
							",
							RowAssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"the expression form over a list",
						String.Format
						(
							@"
								begin
									var LB : Binary;
									var LL : list(Integer) := foreach LB in {{ Binary('0001'), Binary('0002') }} do 1;
									if LL.Count() <> 2 then
										raise Error('The loop did not see both elements.');
									for LI : Integer := 1 to {0} do
										LB := Binary('0003');
									if LB.Value <> '0003' then
										raise Error('The last assignment did not reach the variable.');
								end;
							",
							AssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"the expression form over a cursor",
						String.Format
						(
							@"
								begin
									var LR : row {{ B : Binary }};
									var LL : list(Integer) := foreach LR in cursor(table {{ row {{ Binary('0001') B }} }}) do 1;
									if LL.Count() <> 1 then
										raise Error('The loop did not see the row.');
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
			);
		}

		/// <summary>
		/// The other end of the same loan: a foreach over a variable that already holds a value displaces that value,
		/// which is the slot's own, so the loop has to release it. One release per loop rather than one per iteration,
		/// because only the first lend displaces a value the slot owns - every later one displaces the row or element
		/// the loop itself lent.
		/// </summary>
		/// <remarks>
		/// The cursor arm asserts that two shapes leave the same number of streams behind rather than asserting a count,
		/// because opening a cursor used to copy every stream-backed value on the stack and never release the copies
		/// (bug-a-cursor-copies-the-stack-and-never-gives-it-back). Both shapes paid that cost equally - one row with one
		/// Binary column is on the stack when the cursor opens either way - so the difference between them was this
		/// defect alone. That is fixed and both numbers are now nought; the comparison is kept because what this case is
		/// about is the difference between the two shapes, which is still the thing being measured.
		/// </remarks>
		[Test]
		public void AForEachOverAVariableThatAlreadyHasAValueReleasesIt()
		{
			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"over a list",
						@"
							begin
								var LB : Binary := Binary('00FF');
								var LCount : Integer := 0;
								foreach LB in { Binary('0001'), Binary('0002'), Binary('0003') } do
									LCount := LCount + 1;
								if LCount <> 3 then
									raise Error('The loop did not see every element.');
							end;
						"
					);

					int inTheLoopVariable =
						StreamsLeftBy
						(
							@"
								begin
									var LR : row { B : Binary } := row { Binary('00FF') B };
									var LCount : Integer := 0;
									foreach LR in cursor(table { row { Binary('0001') B } }) do
										LCount := LCount + 1;
									if LCount <> 1 then
										raise Error('The loop did not see the row.');
								end;
							"
						);

					int inAnotherVariable =
						StreamsLeftBy
						(
							@"
								begin
									var LR : row { B : Binary };
									var LOther : row { B : Binary } := row { Binary('00FF') B };
									var LCount : Integer := 0;
									foreach LR in cursor(table { row { Binary('0001') B } }) do
										LCount := LCount + 1;
									if LCount <> 1 then
										raise Error('The loop did not see the row.');
								end;
							"
						);

					Assert.That(inTheLoopVariable, Is.EqualTo(inAnotherVariable), "over a cursor: the loop left behind the value its variable already held.");
				}
			);
		}

		/// <summary>
		/// Two foreach loops over one variable, which is legal D4 and runs correctly. The inner loop lends a slot the
		/// outer loop is already lending, and the value it displaces is the row or element the outer loop's source
		/// still owns, so the inner loop must release nothing: this is the case that a release without the compiler's
		/// already-lent answer turns into a release too many.
		/// </summary>
		[Test]
		public void NestedForEachesOverOneVariableLeaveTheOuterLoopsValueAlone()
		{
			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"over two lists",
						@"
							begin
								var LB : Binary;
								var LCount : Integer := 0;
								foreach LB in { Binary('0001'), Binary('0002') } do
									foreach LB in { Binary('0003'), Binary('0004') } do
										LCount := LCount + 1;
								if LCount <> 4 then
									raise Error('The nested loops did not run four times.');
							end;
						"
					);

					AssertShapeLeavesNoStreams
					(
						"over two cursors",
						@"
							begin
								var LR : row { ID : Integer, B : Binary };
								var LCount : Integer := 0;
								foreach LR in cursor(table { row { 1 ID, Binary('0001') B }, row { 2 ID, Binary('0002') B }, key { ID } }) do
									foreach LR in cursor(table { row { 3 ID, Binary('0003') B }, row { 4 ID, Binary('0004') B }, key { ID } }) do
										LCount := LCount + 1;
								if LCount <> 4 then
									raise Error('The nested loops did not run four times.');
							end;
						"
					);
				}
			);
		}

		/// <summary>
		/// A foreach over one of the enclosing operator's own operands, which is legal D4 and the one shape where the
		/// value the loop displaces may belong to the caller rather than to the slot. Three modifiers, three answers.
		/// A const operand is not the loop's to release at all: its value may have been lent to the call by the
		/// argument that produced it, and the call site settles whether the call owns it. An in operand's slot holds a
		/// value of its own, so the loop releases it - and the loop writing the slot is what keeps the compiler from
		/// promoting the operand to const and passing it by reference, which is how an in operand came to be holding
		/// the caller's own value here. A var operand's slot is an alias of the caller's, which the caller's slot owns
		/// until the write-back after the call, so the loop releases it exactly as an assignment to a var parameter
		/// does.
		/// </summary>
		/// <remarks>
		/// Each shape asserts the caller's value as well as the count, because the failure this guards against is a
		/// release of a value someone else still names: the count balances in that case and the caller's next read
		/// fails with "StreamID (n) not found". The var shape reads nil instead, the loop having nulled the slot whose
		/// final value the write-back hands back.
		/// </remarks>
		[Test]
		public void AForEachOverAnOperandLeavesTheCallersValueAlone()
		{
			ExecuteScript("create operator ZForEachIn(AB : Binary) : Integer begin var LCount : Integer := 0; foreach AB in { Binary('0001'), Binary('0002') } do LCount := LCount + 1; result := LCount; end;");
			ExecuteScript("create operator ZForEachConst(const AB : Binary) : Integer begin var LCount : Integer := 0; foreach AB in { Binary('0001'), Binary('0002') } do LCount := LCount + 1; result := LCount; end;");
			ExecuteScript("create operator ZForEachVar(var AB : Binary) : Integer begin var LCount : Integer := 0; foreach AB in { Binary('0001'), Binary('0002') } do LCount := LCount + 1; result := LCount; end;");

			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"an in operand",
						@"
							begin
								var LB : Binary := Binary('00FF');
								var LI : Integer := ZForEachIn(LB);
								if LI <> 2 then
									raise Error('The loop did not see both elements.');
								if LB.Value <> '00FF' then
									raise Error('The call did not leave the caller''s value alone.');
							end;
						"
					);

					AssertShapeLeavesNoStreams
					(
						"a const operand",
						@"
							begin
								var LB : Binary := Binary('00FF');
								var LI : Integer := ZForEachConst(LB);
								if LI <> 2 then
									raise Error('The loop did not see both elements.');
								if LB.Value <> '00FF' then
									raise Error('The call did not leave the caller''s value alone.');
							end;
						"
					);

					AssertShapeLeavesNoStreams
					(
						"a var operand",
						@"
							begin
								var LB : Binary := Binary('00FF');
								var LI : Integer := ZForEachVar(var LB);
								if LI <> 2 then
									raise Error('The loop did not see both elements.');
								if not(IsNil(LB)) then
									raise Error('A loop over a var parameter must hand the caller back the nil the slot holds once the loop has finished.');
							end;
						"
					);
				}
			);
		}

		/// <summary>
		/// A var operand whose argument is the caller's own loop variable, which is the one shape where the slot the
		/// callee writes to holds a value the slot does not own. A var operand's slot is an alias of the caller's, so an
		/// assignment in the body - or a foreach inside it - released whatever that slot held; where the caller passed
		/// its loop variable, the value released is the row or element the iterator owns and the loop goes on reading,
		/// so the loop read a released value one iteration later. The callee cannot tell which it was given: an
		/// operator is compiled once, independently of its call sites. The call site can, and now copies the value for
		/// the callee's slot, so the callee owns what it releases and the caller's loop keeps what the iterator lent it.
		/// </summary>
		/// <remarks>
		/// The two shapes whose body assigns are measured against the equivalent direct assignment to a loop variable
		/// rather than against nought, because the write-back into the loop variable's slot leaks the value it hands
		/// over exactly as that assignment does - the next plain store into the slot displaces it without releasing it,
		/// the accepted tradeoff recorded on the borrowed branch of AssignmentNode. What this case pins is that the two now
		/// leak equally: what went was the double release, not that leak. The read-only and inner-foreach shapes
		/// balance outright, and are what would catch a fix that paid a copy per call on the read path.
		/// </remarks>
		[Test]
		public void AVarOperandGivenTheCallersLoopVariableLeavesTheLoopsValueAlone()
		{
			ExecuteScript("create operator ZVarLoopRowAssigns(var ARow : row { ID : Integer, B : Binary }) begin ARow := row { 9 ID, Binary('0009') B }; end;");
			ExecuteScript("create operator ZVarLoopAssigns(var AB : Binary) begin AB := Binary('0009'); end;");
			ExecuteScript("create operator ZVarLoopRowReads(var ARow : row { ID : Integer, B : Binary }) : String begin result := ARow.B.Value; end;");
			ExecuteScript("create operator ZVarLoopReads(var AB : Binary) : String begin result := AB.Value; end;");
			ExecuteScript("create operator ZVarLoopInnerForEach(var AB : Binary) : Integer begin var LCount : Integer := 0; foreach AB in { Binary('0001'), Binary('0002') } do LCount := LCount + 1; result := LCount; end;");

			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"over a cursor, the operator only reading the row",
						@"
							begin
								var LR : row { ID : Integer, B : Binary };
								var LCount : Integer := 0;
								foreach LR in cursor(table { row { 1 ID, Binary('00F1') B }, row { 2 ID, Binary('00F2') B }, key { ID } }) do
								begin
									if ZVarLoopRowReads(var LR) <> LR.B.Value then
										raise Error('The operator did not read the row it was given, or the call left it unreadable.');
									LCount := LCount + 1;
								end;
								if LCount <> 2 then
									raise Error('The loop did not see both rows.');
							end;
						"
					);

					AssertShapeLeavesNoStreams
					(
						"over a list, the operator only reading the element",
						@"
							begin
								var LB : Binary;
								var LCount : Integer := 0;
								foreach LB in { Binary('00F1'), Binary('00F2') } do
								begin
									if ZVarLoopReads(var LB) <> LB.Value then
										raise Error('The operator did not read the element it was given, or the call left it unreadable.');
									LCount := LCount + 1;
								end;
								if LCount <> 2 then
									raise Error('The loop did not see both elements.');
							end;
						"
					);

					AssertShapeLeavesNoStreams
					(
						"over a list, with a foreach inside the operator",
						@"
							begin
								var LB : Binary;
								var LCount : Integer := 0;
								foreach LB in { Binary('00F1'), Binary('00F2') } do
								begin
									if ZVarLoopInnerForEach(var LB) <> 2 then
										raise Error('The loop inside the operator did not see both of its elements.');
									LCount := LCount + 1;
								end;
								if LCount <> 2 then
									raise Error('The outer loop did not see both elements.');
							end;
						"
					);

					int throughTheCallOverACursor =
						StreamsLeftBy
						(
							@"
								begin
									var LR : row { ID : Integer, B : Binary };
									var LCount : Integer := 0;
									foreach LR in cursor(table { row { 1 ID, Binary('00F1') B }, row { 2 ID, Binary('00F2') B }, key { ID } }) do
									begin
										ZVarLoopRowAssigns(var LR);
										if LR.B.Value <> '0009' then
											raise Error('The operator''s assignment did not reach the caller''s variable.');
										LCount := LCount + 1;
									end;
									if LCount <> 2 then
										raise Error('The loop did not see both rows.');
								end;
							"
						);

					int directlyOverACursor =
						StreamsLeftBy
						(
							@"
								begin
									var LR : row { ID : Integer, B : Binary };
									var LCount : Integer := 0;
									foreach LR in cursor(table { row { 1 ID, Binary('00F1') B }, row { 2 ID, Binary('00F2') B }, key { ID } }) do
									begin
										LR := row { 9 ID, Binary('0009') B };
										if LR.B.Value <> '0009' then
											raise Error('The assignment did not reach the variable.');
										LCount := LCount + 1;
									end;
									if LCount <> 2 then
										raise Error('The loop did not see both rows.');
								end;
							"
						);

					Assert.That(throughTheCallOverACursor, Is.EqualTo(directlyOverACursor), "over a cursor, the operator assigning: the call left behind more than the same assignment made directly.");

					int throughTheCallOverAList =
						StreamsLeftBy
						(
							@"
								begin
									var LB : Binary;
									var LCount : Integer := 0;
									foreach LB in { Binary('00F1'), Binary('00F2') } do
									begin
										ZVarLoopAssigns(var LB);
										if LB.Value <> '0009' then
											raise Error('The operator''s assignment did not reach the caller''s variable.');
										LCount := LCount + 1;
									end;
									if LCount <> 2 then
										raise Error('The loop did not see both elements.');
								end;
							"
						);

					int directlyOverAList =
						StreamsLeftBy
						(
							@"
								begin
									var LB : Binary;
									var LCount : Integer := 0;
									foreach LB in { Binary('00F1'), Binary('00F2') } do
									begin
										LB := Binary('0009');
										if LB.Value <> '0009' then
											raise Error('The assignment did not reach the variable.');
										LCount := LCount + 1;
									end;
									if LCount <> 2 then
										raise Error('The loop did not see both elements.');
								end;
							"
						);

					Assert.That(throughTheCallOverAList, Is.EqualTo(directlyOverAList), "over a list, the operator assigning: the call left behind more than the same assignment made directly.");
				}
			);
		}

		/// <summary>
		/// An operator with a return type whose body assigns to one of its own in parameters. The compiler promotes an
		/// in operand the body never assigns to to const, which is passed by reference rather than copied, and it read
		/// the wrong operand's symbol to decide that whenever the operator had a result - so an operand the body does
		/// assign to could be promoted, and the body's assignment then released the caller's value out from under the
		/// caller. Two operands and a result is the smallest shape that shows it; the arguments are deliberately two
		/// different variables, because this needs no repeated argument.
		/// </summary>
		[Test]
		public void AnOperatorWithAResultThatAssignsToAnInParameterLeavesTheCallersValueAlone()
		{
			ExecuteScript("create operator ZResultAssignsIn(AB : Binary, AC : Binary) : Integer begin AB := Binary('0008'); result := 1; end;");

			AssertScriptLeavesNoStreams
			(
				@"
					begin
						var LB : Binary := Binary('0001');
						var LC : Binary := Binary('0002');
						var LI : Integer := ZResultAssignsIn(LB, LC);
						if LB.Value <> '0001' then
							raise Error('A by-value call reached the caller''s variable.');
					end;
				"
			);
		}

		/// <summary>
		/// One variable passed to two const parameters of one call. Neither slot can be assigned to - the compiler makes
		/// a const operand's symbol constant - so neither can release anything and neither needs a value of its own.
		/// Giving the second one a copy leaked it: the caller takes a const operand's value back, so nothing released
		/// the copy. This is an ordinary call - two parameters of the same type, the same variable for both - and it is
		/// also what every operator whose body assigns to neither parameter compiles to, because those are promoted
		/// from in to const.
		/// </summary>
		[Test]
		public void TheSameVariablePassedTwiceToConstParametersLeavesNoStreamBehind()
		{
			ExecuteScript("create operator ZTwoConst(const AB : Binary, const AC : Binary) : Integer begin result := 0; if AB.Value = AC.Value then result := 1; end;");

			AssertScriptLeavesNoStreams
			(
				@"
					begin
						var LB : Binary := Binary('0001');
						var LI : Integer := ZTwoConst(LB, LB);
						if LI <> 1 then
							raise Error('The call did not see the value in both parameters.');
					end;
				"
			);
		}

		/// <summary>
		/// Assigning through a property of a scalar type - LB.Value := ... - compiles to the property's write
		/// accessor applied to the variable and an assignment of its result back to the variable, so the source
		/// expression reads the value the assignment is about to replace. The written value is a new stream, so
		/// the replaced one must still be released exactly once.
		/// </summary>
		[Test]
		public void AssigningThroughAScalarPropertyReleasesTheValueItReplaces()
		{
			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LB : Binary := Binary('0001');
							for LI : Integer := 1 to {0} do
								LB.Value := '0002';
							if LB.Value <> '0002' then
								raise Error('The last assignment did not reach the variable.');
						end;
					",
					AssignmentCount
				)
			);
		}

		/// <summary>
		/// A const operand given a value computed for the call - here a Binary selector, the shape every literal large
		/// value takes. Nothing but the call ever names that value, so the call has to release it; it used to release
		/// nothing at all for a const operand, holding one stream per call for the life of the session. Both a function
		/// and a procedure, because the two were different bugs historically - the promotion of a by-value operand to
		/// const read the wrong operand's symbol for every operator with a result until
		/// bug-assigning-a-binary-variable-leaks-its-stream - and a regression could reach one and not the other. The
		/// function's result and the procedure's own raise are the value assertions: a call that stopped seeing its
		/// argument would fail rather than pass quietly.
		/// </summary>
		/// <remarks>
		/// What makes the case worth its run time beyond the two operators it declares: the compiler promotes a
		/// by-value operand the body never assigns to to const, and every comparison, conversion, selector and property
		/// accessor the engine generates for a scalar type declares const outright, so nearly every D4 expression
		/// reaches this decision.
		/// </remarks>
		[Test]
		public void AConstOperandReleasesTheValueComputedForIt()
		{
			ExecuteScript("create operator ZConstSees(const AB : Binary) : Integer begin result := 0; if AB.Value = '0001' then result := 1; end;");
			ExecuteScript("create operator ZConstChecks(const AB : Binary) begin if AB.Value <> '0001' then raise Error('The procedure did not see the value it was given.'); end;");

			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"a function",
						String.Format
						(
							@"
								begin
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
										LSeen := LSeen + ZConstSees(Binary('0001'));
									if LSeen <> {0} then
										raise Error('A call did not see the value computed for it.');
								end;
							",
							AssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"a procedure",
						String.Format
						(
							@"
								begin
									for LI : Integer := 1 to {0} do
										ZConstChecks(Binary('0001'));
								end;
							",
							AssignmentCount
						)
					);
				}
			);
		}

		/// <summary>
		/// The other side of the same decision, and the path that must keep working: a const operand given a variable is
		/// handed the slot's own value rather than a copy, so the call must release nothing. Releasing it would leave the
		/// caller's variable naming a released stream - which is what reading ownership off the modifier the other way
		/// round would do - and nothing covered this before. The loop repeats the call so that a per-call release reports
		/// a full count, and both the result and the variable are read afterwards.
		/// </summary>
		[Test]
		public void AConstOperandGivenAVariableLeavesTheCallersValueAlone()
		{
			ExecuteScript("create operator ZConstSeesVariable(const AB : Binary) : Integer begin result := 0; if AB.Value = '0001' then result := 1; end;");

			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LB : Binary := Binary('0001');
							var LSeen : Integer := 0;
							for LI : Integer := 1 to {0} do
								LSeen := LSeen + ZConstSeesVariable(LB);
							if LSeen <> {0} then
								raise Error('A call did not see the variable''s value.');
							if LB.Value <> '0001' then
								raise Error('A call released the caller''s value.');
						end;
					",
					AssignmentCount
				)
			);
		}

		/// <summary>
		/// A by-value operand given a row variable's column. The node that reads a column used to hand over the row's own
		/// value with no way to say otherwise, and by-value cleanup then released it, so the row was left naming a
		/// released stream and the next read of the column failed. The operand is still by value at run time because the
		/// body assigns to it; an operand the body leaves alone is promoted to const, which the two cases above cover.
		/// </summary>
		[Test]
		public void AByValueOperandGivenARowsColumnLeavesTheRowsColumnAlone()
		{
			ExecuteScript("create operator ZByValueReplaces(AB : Binary) begin AB := Binary('0002'); end;");

			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LR : row {{ B : Binary }} := row {{ Binary('0001') B }};
							for LI : Integer := 1 to {0} do
								ZByValueReplaces(LR.B);
							if LR.B.Value <> '0001' then
								raise Error('A by-value call reached the row''s column.');
						end;
					",
					RowAssignmentCount
				)
			);
		}

		/// <summary>
		/// Reading a row variable's column into something that takes ownership of what it is given - a new variable, an
		/// existing variable, a row selector. This is the worst of the three shapes the one missing fact broke, because
		/// the D4 is entirely ordinary: no operator and no modifier, just reading a field. The read succeeded and then
		/// the receiver and the row both named one value, so whichever was released first left the other naming a
		/// released stream. Every shape reads both the copy and the row's own column afterwards, because a read that
		/// quietly stopped producing a usable value would otherwise balance the count and pass.
		/// </summary>
		[Test]
		public void ARowsColumnReadIntoSomethingThatOwnsItStaysReadable()
		{
			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"into a new variable",
						String.Format
						(
							@"
								begin
									var LR : row {{ B : Binary }} := row {{ Binary('0001') B }};
									for LI : Integer := 1 to {0} do
									begin
										var LB : Binary := LR.B;
										if LB.Value <> '0001' then
											raise Error('The copy did not carry the column''s value.');
									end;
									if LR.B.Value <> '0001' then
										raise Error('Reading the column released the row''s own value.');
								end;
							",
							RowAssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"over an existing variable",
						String.Format
						(
							@"
								begin
									var LR : row {{ B : Binary }} := row {{ Binary('0001') B }};
									var LB : Binary := Binary('0002');
									for LI : Integer := 1 to {0} do
										LB := LR.B;
									if LB.Value <> '0001' then
										raise Error('The assignment did not carry the column''s value.');
									if LR.B.Value <> '0001' then
										raise Error('Reading the column released the row''s own value.');
								end;
							",
							RowAssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"into a row selector",
						String.Format
						(
							@"
								begin
									var LR : row {{ B : Binary }} := row {{ Binary('0001') B }};
									for LI : Integer := 1 to {0} do
									begin
										var LS : row {{ B : Binary }} := row {{ LR.B B }};
										if LS.B.Value <> '0001' then
											raise Error('The selected row did not carry the column''s value.');
									end;
									if LR.B.Value <> '0001' then
										raise Error('Selecting the column released the row''s own value.');
								end;
							",
							RowAssignmentCount
						)
					);
				}
			);
		}

		/// <summary>
		/// IfNil, the one node with a const operand that is outside the rule's own machinery: it overrides
		/// InternalExecute(Program) itself, so InstructionNodeBase.CleanupOperand never runs for it, and it copied the
		/// argument it chose and released nothing - one stream for the life of the session per call over a value the
		/// call owned (bug-ifnil-leaks-the-value-it-copies). Both shapes that leaked are here: the first argument
		/// computed, and a nil first argument with a computed replacement, because the two paths read different nodes
		/// and a fix that reached one need not reach the other. Each reads the result afterwards, which is the half
		/// the count cannot see: releasing the chosen value without an independent copy of it would hand back a
		/// released stream and balance the count all the same.
		/// </summary>
		[Test]
		public void IfNilReleasesTheValueComputedForIt()
		{
			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"a computed first argument",
						String.Format
						(
							@"
								begin
									var LB : Binary;
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
									begin
										LB := IfNil(Binary('0001'), Binary('0002'));
										if LB.Value = '0001' then
											LSeen := LSeen + 1;
									end;
									if LSeen <> {0} then
										raise Error('IfNil did not return the value computed for it.');
								end;
							",
							AssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"a nil first argument",
						String.Format
						(
							@"
								begin
									var LNil : Binary;
									var LB : Binary;
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
									begin
										LB := IfNil(LNil, Binary('0002'));
										if LB.Value = '0002' then
											LSeen := LSeen + 1;
									end;
									if LSeen <> {0} then
										raise Error('IfNil did not return the replacement computed for it.');
								end;
							",
							AssignmentCount
						)
					);
				}
			);
		}

		/// <summary>
		/// The other side of IfNil's decision, and the path a fix the other way round breaks: given a variable, IfNil
		/// is lent the slot's own value, so it must release nothing. This one passed before the fix and is here to keep
		/// passing - a release here would leave the caller's variable naming a released stream, and the count would
		/// balance either way, so the assertion that matters is that the variable is still readable afterwards.
		/// </summary>
		[Test]
		public void IfNilGivenAVariableLeavesTheCallersValueAlone()
		{
			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LS : Binary := Binary('0001');
							var LB : Binary;
							var LSeen : Integer := 0;
							for LI : Integer := 1 to {0} do
							begin
								LB := IfNil(LS, Binary('0002'));
								if LB.Value = '0001' then
									LSeen := LSeen + 1;
							end;
							if LSeen <> {0} then
								raise Error('IfNil did not return the variable''s value.');
							if LS.Value <> '0001' then
								raise Error('IfNil released the caller''s value.');
						end;
					",
					AssignmentCount
				)
			);
		}

		/// <summary>
		/// The same loan, where the compiler has rewritten IfNil's own arguments. Two rows whose columns differ by a type
		/// one converts to the other are legal arguments to IfNil, and it gets there by replacing one of its two argument
		/// nodes with the conversion. The loan is granted to whichever node is in the operand slot when the question is
		/// asked, so asking before that rewrite granted it to the node underneath the conversion, and a row conversion is
		/// built out of the row-extend node, which disposes the source row it is handed - destroying the row in the
		/// caller's variable, so the next read of that variable failed. Found reviewing
		/// bug-row-equality-change-breaks-implicitconversions, which is the same defect in the two row-comparison nodes.
		/// The nilable side is left unassigned so that the replacement - the side the conversion wraps - is the one
		/// actually evaluated; the Binary column is what gives the stream count something to measure, and it is the same
		/// type on both sides, which is why the conversion touches only the other column.
		/// </summary>
		[Test]
		public void IfNilWithAnImplicitConversionLeavesTheCallersRowAlone()
		{
			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LNil : row {{ X : Binary, N : Long }};
							var LReplacement : row {{ X : Binary, N : Byte }} := row {{ Binary('0001') X, ToByte(5) N }};
							var LResult : row {{ X : Binary, N : Long }};
							var LSeen : Integer := 0;
							for LI : Integer := 1 to {0} do
							begin
								LResult := IfNil(LNil, LReplacement);
								if LResult.N = 5 then
									LSeen := LSeen + 1;
							end;
							if LSeen <> {0} then
								raise Error('IfNil did not return the replacement row.');
							if LReplacement.X.Value <> '0001' then
								raise Error('IfNil destroyed the replacement row in the caller''s variable.');
						end;
					",
					RowAssignmentCount
				)
			);
		}

		/// <summary>
		/// Row equality - the pair of nodes outside that same machinery, and the pair that missed both halves of the rule
		/// at once: nothing asked either argument node to lend, so both copied the row they were given, and the execute
		/// override then released neither copy - a managed stream per stream-backed column per side, on every comparison
		/// (bug-row-equality-leaks-the-rows-it-compares). Row equality is what a transition constraint, a key comparison
		/// and in over a row-valued list all compile to, so this is ordinary application code rather than a script that
		/// says = between two rows. All four shapes are here because each reaches a different part of the fix: the
		/// computed rows are released by the half that releases, the variable's row is left alone by the half that asks,
		/// the two-column row is the per-column arithmetic, and the generic row operand is the run-time branch, which
		/// plans a second comparison over the values it has just evaluated and so needs argument nodes that say they are
		/// lending.
		/// </summary>
		[Test]
		public void RowEqualityReleasesTheRowsItCompares()
		{
			ExecuteScript("create operator ZGenericRowsEqual(const ALeft : row, const ARight : row) : Boolean begin result := ALeft = ARight; end;");

			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"two computed rows",
						String.Format
						(
							@"
								begin
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
										if row {{ Binary('0001') X }} = row {{ Binary('0001') X }} then
											LSeen := LSeen + 1;
									if LSeen <> {0} then
										raise Error('Equal computed rows did not compare equal.');
								end;
							",
							RowAssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"a row variable",
						String.Format
						(
							@"
								begin
									var LR : row {{ X : Binary }} := row {{ Binary('0001') X }};
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
										if LR = LR then
											LSeen := LSeen + 1;
									if LSeen <> {0} then
										raise Error('A row variable did not compare equal to itself.');
								end;
							",
							RowAssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"a two-column row",
						String.Format
						(
							@"
								begin
									var LR : row {{ X : Binary, Y : Binary }} := row {{ Binary('0001') X, Binary('0002') Y }};
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
										if LR = LR then
											LSeen := LSeen + 1;
									if LSeen <> {0} then
										raise Error('A two-column row variable did not compare equal to itself.');
								end;
							",
							RowAssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"a generic row operand",
						String.Format
						(
							@"
								begin
									var LR : row {{ X : Binary }} := row {{ Binary('0001') X }};
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
										if ZGenericRowsEqual(LR, LR) then
											LSeen := LSeen + 1;
									if LSeen <> {0} then
										raise Error('A generic row operand did not compare equal to itself.');
								end;
							",
							RowAssignmentCount
						)
					);
				}
			);
		}

		/// <summary>
		/// The loan side of row equality, which the count alone cannot see: lending without releasing and releasing
		/// without copying both leave the count in balance, and only a read afterwards tells them apart. So this reads
		/// both: that the comparison answered true every time, and that the columns of both rows are still there to read
		/// once the loop has finished. Two variables rather than one, so that each argument node is a separate loan.
		/// </summary>
		[Test]
		public void RowEqualityGivenVariablesLeavesTheCallersRowsAlone()
		{
			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LLeft : row {{ X : Binary }} := row {{ Binary('0001') X }};
							var LRight : row {{ X : Binary }} := row {{ Binary('0001') X }};
							var LSeen : Integer := 0;
							for LI : Integer := 1 to {0} do
								if LLeft = LRight then
									LSeen := LSeen + 1;
							if LSeen <> {0} then
								raise Error('Equal rows did not compare equal.');
							if LLeft.X.Value <> '0001' then
								raise Error('The comparison released the left row''s column.');
							if LRight.X.Value <> '0001' then
								raise Error('The comparison released the right row''s column.');
						end;
					",
					RowAssignmentCount
				)
			);
		}

		/// <summary>
		/// The same loan, where the compiler has rewritten the comparison's own arguments. Two rows whose columns differ
		/// by a type one converts to the other compare as equal in D4, and the comparison gets there by replacing one of
		/// its two argument nodes with the conversion. The loan is granted to whichever node is in the operand slot when
		/// the question is asked, so asking before that rewrite grants it to the node underneath the conversion, and the
		/// row-extend node the conversion is built from disposes the source row it is handed - destroying the row in the
		/// variable's slot, so the next read of that variable failed
		/// (bug-row-equality-change-breaks-implicitconversions). Both directions, because the compiler rewrites whichever
		/// side it finds a conversion path for and only one of the two is rewritten, and both variables are read after the
		/// loop, which is the half the stream count cannot see. The Binary column is what gives the count something to
		/// measure at all - a row of number columns has no stream to lose - so the release half stays pinned in the same
		/// case; it is the same type on both sides, which is why the conversion touches only the other column.
		/// </summary>
		[Test]
		public void RowEqualityWithAnImplicitConversionLeavesTheCallersRowsAlone()
		{
			AssertScriptLeavesNoStreams
			(
				String.Format
				(
					@"
						begin
							var LLeft : row {{ X : Binary, N : Long }} := row {{ Binary('0001') X, 5 N }};
							var LRight : row {{ X : Binary, N : Byte }} := row {{ Binary('0001') X, ToByte(5) N }};
							var LSeen : Integer := 0;
							for LI : Integer := 1 to {0} do
							begin
								if LLeft = LRight then
									LSeen := LSeen + 1;
								if LRight = LLeft then
									LSeen := LSeen + 1;
							end;
							if LSeen <> {1} then
								raise Error('Rows whose columns differ by a convertible type did not compare equal.');
							if LLeft.X.Value <> '0001' then
								raise Error('The comparison destroyed the left row.');
							if LRight.X.Value <> '0001' then
								raise Error('The comparison destroyed the right row.');
						end;
					",
					RowAssignmentCount,
					RowAssignmentCount * 2
				)
			);
		}

		/// <summary>
		/// The equality the compiler generates for a compound scalar type - one whose single representation has more than
		/// one property - which leaked the same way and for the same two reasons, over the compound row behind the value
		/// rather than over a row the script wrote. Both shapes are here for the same reason as above, and each reads its
		/// result; the variable shape also reads the variable's property afterwards, which is the half the count cannot
		/// see. The existing coverage of this operator
		/// (tests/libraries/TestFramework.Coverage.Scripts/Documents/Types.CompoundEquality.d4) is all-Integer, so it
		/// cannot see a leak at all - a stream is what a leak costs, and only a stream-backed property has one.
		/// </summary>
		[Test]
		public void CompoundScalarEqualityReleasesTheValuesItCompares()
		{
			ExecuteScript("create type ZCompoundBinary { representation ZCompoundBinary { X : Binary, N : Integer } };");

			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"two computed compound values",
						String.Format
						(
							@"
								begin
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
										if ZCompoundBinary(Binary('0001'), 1) = ZCompoundBinary(Binary('0001'), 1) then
											LSeen := LSeen + 1;
									if LSeen <> {0} then
										raise Error('Equal computed compound values did not compare equal.');
								end;
							",
							RowAssignmentCount
						)
					);

					AssertShapeLeavesNoStreams
					(
						"a compound variable",
						String.Format
						(
							@"
								begin
									var LC : ZCompoundBinary := ZCompoundBinary(Binary('0001'), 1);
									var LSeen : Integer := 0;
									for LI : Integer := 1 to {0} do
										if LC = LC then
											LSeen := LSeen + 1;
									if LSeen <> {0} then
										raise Error('A compound variable did not compare equal to itself.');
									if LC.X.Value <> '0001' then
										raise Error('The comparison released the caller''s property.');
								end;
							",
							RowAssignmentCount
						)
					);
				}
			);
		}

		/// <summary>
		/// A call one of whose arguments raises, with an earlier argument that produced a value the call owns. The
		/// arguments used to be evaluated outside the try whose finally cleans them up, so nothing released what the
		/// earlier argument produced and the call orphaned it. All three of the execute shapes that can have an
		/// argument before another one are covered, because each evaluates its arguments in its own code and a fix
		/// applied to one is no evidence about the others: the general shape of a declared operator's call, the
		/// two-argument shape of a generated comparison, and the three-argument shape of a list insert. The
		/// one-argument shape needs nothing - there is no argument before the one that raises.
		/// </summary>
		/// <remarks>
		/// The first shape puts its var operand after the raising argument and asserts that the caller's variable is
		/// neither nil nor changed, which is the half of the rule the count cannot see: the cleanup that runs after a
		/// call writes a var operand's value back to the caller, and there is no such value when the call never
		/// happened. Reusing that cleanup on this path would poke null into the caller's variable - a leak turned into
		/// data loss - so this case fails on the value under that fix and on the count without any fix. Each script
		/// catches its own error in D4 and asserts that it caught one, so a shape that silently stopped raising cannot
		/// pass while testing nothing.
		/// </remarks>
		[Test]
		public void ACallWhoseLaterArgumentRaisesReleasesTheArgumentsAlreadyEvaluated()
		{
			ExecuteScript("create operator ZProbeRaises() : Binary begin raise Error('ZProbeRaises'); end;");
			ExecuteScript("create operator ZProbeRaisesInt() : Integer begin raise Error('ZProbeRaisesInt'); end;");
			ExecuteScript("create operator ZProbeVarLast(const AB : Binary, const AC : Binary, var AD : Binary) : Integer begin AD := Binary('0003'); result := 1; end;");

			// One Assert.Multiple over the three shapes, so that a shape-specific regression is not hidden by another shape's failure.
			Assert.Multiple
			(
				() =>
				{
					AssertShapeLeavesNoStreams
					(
						"a declared operator's call",
						@"
							begin
								var LB : Binary := Binary('0001');
								var LCaught : Integer := 0;
								var LI : Integer := 0;
								try
									LI := ZProbeVarLast(Binary('0002'), ZProbeRaises(), var LB);
								except
									on E : Error do
										LCaught := 1;
								end;
								if LCaught <> 1 then
									raise Error('The call did not raise.');
								if IsNil(LB) then
									raise Error('The failed call nilled the caller''s variable.');
								if LB.Value <> '0001' then
									raise Error('The failed call changed the caller''s variable.');
							end;
						"
					);

					AssertShapeLeavesNoStreams
					(
						"a generated comparison",
						@"
							begin
								var LEqual : Boolean;
								var LCaught : Integer := 0;
								try
									LEqual := Binary('0001') = ZProbeRaises();
								except
									on E : Error do
										LCaught := 1;
								end;
								if LCaught <> 1 then
									raise Error('The comparison did not raise.');
							end;
						"
					);

					AssertShapeLeavesNoStreams
					(
						"a list insert",
						@"
							begin
								var LList : list(Binary) := { Binary('0001') };
								var LCaught : Integer := 0;
								try
									Insert(var LList, Binary('0002'), ZProbeRaisesInt());
								except
									on E : Error do
										LCaught := 1;
								end;
								if LCaught <> 1 then
									raise Error('The insert did not raise.');
								if LList.Count() <> 1 then
									raise Error('The failed insert changed the list.');
							end;
						"
					);
				}
			);
		}

		/// <summary>
		/// The half of a failed call's rule that a stream count cannot see: a var operand evaluated before the argument
		/// that raised is left exactly as the caller left it. The cleanup that runs after a call writes a var operand's
		/// value back to the caller through the type's constraints and validate handlers, which is user D4; a failed
		/// call has no callee value to write back, so running that cleanup on this path would re-validate a value
		/// nothing assigned to - and could raise from inside a handler while the argument's own error was unwinding.
		/// </summary>
		/// <remarks>
		/// The handler counts its own runs in a table rather than changing the value it is given, because a validate
		/// handler that assigns to its operand is validated again and only a handler that stops changing the value
		/// terminates. The count is read once after the variable is assigned and compared with itself after the failed
		/// call, rather than against a literal, so the case does not also encode how many times assigning a variable
		/// validates it. Reusing the after-the-call cleanup on this path makes this case fail with one extra run; the
		/// stream-count case above passes under that same change, which is why this one is here.
		/// </remarks>
		[Test]
		public void AFailedCallLeavesAVarOperandEvaluatedBeforeTheRaiseAlone()
		{
			ExecuteScript("create type ZCounted like String;");
			ExecuteScript("create table ZValidateRuns { Runs : Integer, key { } };");
			ExecuteScript("insert row { 0 Runs } into ZValidateRuns;");
			ExecuteScript("create operator ZCounted_Validate(var AValue : ZCounted) : Boolean begin update ZValidateRuns set { Runs := Runs + 1 }; result := false; end;");
			ExecuteScript("attach operator ZCounted_Validate to ZCounted on { validate };");
			ExecuteScript("create operator ZCountedRaises() : Binary begin raise Error('ZCountedRaises'); end;");
			ExecuteScript("create operator ZCountedVarFirst(var AV : ZCounted, const AB : Binary) : Integer begin result := 1; end;");

			ExecuteScript
			(
				@"
					begin
						update ZValidateRuns set { Runs := 0 };
						var LV : ZCounted := 'a';
						var LAssigned : Integer := ZValidateRuns[].Runs;
						var LCaught : Integer := 0;
						var LI : Integer := 0;
						try
							LI := ZCountedVarFirst(var LV, ZCountedRaises());
						except
							on E : Error do
								LCaught := 1;
						end;
						if LCaught <> 1 then
							raise Error('The call did not raise.');
						if ZValidateRuns[].Runs <> LAssigned then
							raise Error('The failed call ran the type''s validate handler ' + (ZValidateRuns[].Runs - LAssigned).ToString() + ' more time(s).');
					end;
				"
			);
		}

		/// <summary>
		/// A block that declares a Binary and then raises, with nothing inside the engine catching the error. The block
		/// is left by the raise rather than through its end, which is the one path the compiled release ever covered.
		/// </summary>
		[Test]
		public void ABlockLeftByARaiseReleasesTheValuesItDeclared()
		{
			AssertRaisingScriptLeavesNoStreams
			(
				@"
					begin
						var LB : Binary := Binary('0001');
						raise Error('ZBlockRaise');
					end;
				",
				"ZBlockRaise"
			);
		}

		/// <summary>
		/// The same raise, caught by a D4 try ... except outside the block that declared the variable, so that the claim
		/// is about the block being left rather than about an error that happens to reach the client.
		/// </summary>
		[Test]
		public void ABlockLeftByARaiseCaughtInD4ReleasesTheValuesItDeclared()
		{
			AssertScriptLeavesNoStreams
			(
				@"
					begin
						var LCaught : Integer := 0;
						try
							begin
								var LB : Binary := Binary('0001');
								raise Error('ZInnerRaise');
							end;
						except
							on E : Error do
								LCaught := 1;
						end;
						if LCaught <> 1 then
							raise Error('The inner block did not raise.');
					end;
				"
			);
		}

		/// <summary>
		/// A loop body that declares a Binary and leaves itself by continue - ordinary, non-error D4, and the shape a
		/// filtering loop is naturally written in. This is the exit path that leaks once per iteration, so the loop runs
		/// three times and the count assertion is arithmetic over all three. LCount asserts that the body ran on every
		/// element and that the continue fired every time.
		/// </summary>
		[Test]
		public void AForEachBodyLeftByContinueReleasesTheValuesItDeclared()
		{
			AssertScriptLeavesNoStreams
			(
				@"
					begin
						var LCount : Integer := 0;
						foreach var LI in { 1, 2, 3 } do
						begin
							var LB : Binary := Binary('0001');
							LCount := LCount + 1;
							if LI < 10 then
								continue;
							LCount := LCount + 100;
						end;
						if LCount <> 3 then
							raise Error('The loop did not continue on each of its three elements.');
					end;
				"
			);
		}

		/// <summary>
		/// An operator body that declares a Binary and raises. An operator's begin ... end body is not the same thing as
		/// a begin ... end statement - the parser hands the body's statement list over directly, so the body's variables
		/// are leftovers in the call's stack window rather than frame variables - and the engine releases them at a
		/// different site, which none of the cases above reaches. The script calls the operator with no block of its own
		/// around it, so the operator body is the only scope in play.
		/// </summary>
		[Test]
		public void AnOperatorBodyLeftByARaiseReleasesTheValuesItDeclared()
		{
			ExecuteScript("create operator ZBodyRaise() begin var LB : Binary := Binary('0001'); raise Error('ZBodyRaise'); end;");

			AssertRaisingScriptLeavesNoStreams("ZBodyRaise();", "ZBodyRaise");
		}

		/// <summary>
		/// A function that assigns its result and then raises. The result is a variable of the body like any other, but
		/// the two sites that release a body's variables have to leave it alone on the path that hands it to the caller,
		/// so it is the one slot a release driven by stack depth can miss. This is the operator-call site.
		/// </summary>
		[Test]
		public void AFunctionLeftByARaiseReleasesTheResultItAssigned()
		{
			ExecuteScript("create operator ZResultRaise() : Binary begin result := Binary('0001'); raise Error('ZResultRaise'); end;");

			AssertRaisingScriptLeavesNoStreams
			(
				@"
					begin
						var LB : Binary := ZResultRaise();
					end;
				",
				"ZResultRaise"
			);
		}

		/// <summary>
		/// The same for the aggregate-call site, whose result slot is filled by one block and read after another: the
		/// initialization assigns it and the aggregation then raises, so the finalization and the read never happen. The
		/// aggregate is invoked over a table literal, so the case needs no table of its own.
		/// </summary>
		[Test]
		public void AnAggregateLeftByARaiseReleasesTheResultItAssigned()
		{
			ExecuteScript
			(
				@"
					create aggregate operator ZAggResultRaise(AValue : Integer) : Binary
					initialization
					begin
						result := Binary('0001');
					end
					aggregation
					begin
						raise Error('ZAggResultRaise');
					end
					finalization
					begin
					end;
				"
			);

			AssertRaisingScriptLeavesNoStreams
			(
				@"
					begin
						var LB : Binary := ZAggResultRaise(AValue from table { row { 1 AValue }, row { 2 } });
					end;
				",
				"ZAggResultRaise"
			);
		}

		/// <summary>
		/// Opening a cursor copies every value on the stack into a stack of the cursor's own, and those copies are the
		/// cursor's to release: closing it threw them away unreleased, holding one stream per stream-backed variable in
		/// scope, per cursor opened, for the life of the session
		/// (bug-a-cursor-copies-the-stack-and-never-gives-it-back).
		/// </summary>
		/// <remarks>
		/// Three Binary variables and two loops, so that a regression reports a count of six rather than something that
		/// could be mistaken for an off-by-one. The source has no stream-backed column of its own, so every stream the
		/// case could count is one the snapshot copied, and the variables are declared before the loops, which is what
		/// puts them in the window the snapshot copies - one declared after the cursor opened was never on the stack to
		/// be copied and never cost anything.
		/// </remarks>
		[Test]
		public void AForEachOverACursorReleasesTheStackItCopied()
		{
			AssertScriptLeavesNoStreams
			(
				@"
					begin
						var LB1 : Binary := Binary('0001');
						var LB2 : Binary := Binary('0002');
						var LB3 : Binary := Binary('0003');
						var LCount : Integer := 0;
						foreach var LRow in cursor(table { row { 1 ID } }) do
							LCount := LCount + 1;
						foreach var LOtherRow in cursor(table { row { 2 ID } }) do
							LCount := LCount + 1;
						if LCount <> 2 then
							raise Error('The loops did not see their rows.');
					end;
				"
			);
		}

		/// <summary>Runs one of the loop forms above, failing with the form's name rather than only the engine's error.</summary>
		private void AssertForEachSeesBothRows(string form, string script)
		{
			Assert.That(() => ExecuteScript(script), Throws.Nothing, form + ": the loop did not survive an assignment to its variable.");
		}

		/// <summary>
		/// Runs one shape of a case inside an Assert.Multiple, naming the shape in both assertions. Neither assertion
		/// throws, so one shape's failure does not hide the shapes after it; each reads its own baseline count, so a
		/// shape that ran after a failing one is still measured against the count it started from.
		/// </summary>
		private void AssertShapeLeavesNoStreams(string shape, string script)
		{
			int before = StreamCount();
			Assert.That(() => ExecuteScript(script), Throws.Nothing, shape + ": the script raised.");
			Assert.That(StreamCount(), Is.EqualTo(before), shape + ": the script left streams behind.");
		}

		/// <summary>
		/// How many streams the script left behind, for a case that compares two shapes against each other rather
		/// than against nought - the shape to reach for when another, open defect makes both shapes leak equally.
		/// </summary>
		private int StreamsLeftBy(string script)
		{
			int before = StreamCount();
			ExecuteScript(script);
			return StreamCount() - before;
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

		/// <summary>
		/// The same, for a script that must raise: the error has to carry the given text, and the count has to be back
		/// in balance afterwards. Asserting the text is what keeps the case honest if the script ever stops raising.
		/// </summary>
		private void AssertRaisingScriptLeavesNoStreams(string script, string errorText)
		{
			int before = StreamCount();
			Assert.That(() => ExecuteScript(script), Throws.Exception.With.Message.Contains(errorText), "The script did not raise.");
			Assert.That(StreamCount(), Is.EqualTo(before), "The script left streams behind.");
		}
	}
}
