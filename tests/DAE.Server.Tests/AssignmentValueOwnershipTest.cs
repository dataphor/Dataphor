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
	/// to two callee slots. The four cases from AConstOperandReleasesTheValueComputedForIt through
	/// ARowsColumnReadIntoSomethingThatOwnsItStaysReadable are the operand half of the same rule: whether a call owns
	/// the value it was handed for an operand is a property of the plan node that produced the argument - a node that
	/// reads a stack slot, or a column of a row in one, can lend the value it already holds, where a literal, a
	/// conversion or another call produces one nothing else holds - and not of the operand's modifier, which is all
	/// the call used to look at (bug-an-operator-argument-is-released-by-the-wrong-owner). Reading ownership off the
	/// modifier held a stream for the life of the session on every call with a const operand, which is most calls,
	/// and released a row's own column out from under the row whenever one was read.
	/// ACallWhoseLaterArgumentRaisesReleasesTheArgumentsAlreadyEvaluated is the error path of that same operand
	/// cleanup: it used to be skipped altogether when an argument of a call raised, because the execute shapes
	/// evaluated their arguments before entering the try whose finally cleans them up, so every argument evaluated
	/// before the one that raised was orphaned (bug-an-argument-is-orphaned-when-a-later-one-raises). The last six
	/// cases are the other half of the same ownership rule: a scope releases the values of the variables it declared.
	/// That release used to be a statement the compiler appended to the end of the block, so only control reaching the
	/// end of the block ran it, and a raise, an exit, a break or a continue left every value behind
	/// (bug-an-error-inside-a-block-leaks-its-binary-values). The last two of those cover the one variable such a
	/// release can miss: an operator's result, which the paths that do reach the caller hand over rather than release.
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
