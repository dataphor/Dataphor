/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A change to a data type reaching a form that is already open outside the server. The three questions a client asks
	/// about the row a user is editing - what should this column default to, what follows from the user having changed this
	/// column, is this row acceptable - stop being answered on the client once the client knows that the plan it holds was
	/// compiled against an older generation of the catalog.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Why a client answers any of these itself, and what makes a plan's answer stale, is at LocalPlan.IsSchemaCurrent.
	/// The first three cases are the three calls, one each: a cursor opened before the type changed now gives the same answer
	/// as the engine's own cursor afterwards, where before it went on giving the pre-change answer. The engine side of each
	/// comparison is TypeChangeSummaryTest's subject and is here only as the oracle, as in the two sibling fixtures on this
	/// stack - there is no reference runtime to transcribe an expected value from - and each case names the resulting value as
	/// well, so that both stacks being wrong the same way could not pass.
	/// </para>
	/// <para>
	/// Each of the three has the client evaluate an unrelated one-line expression after the type change. That is what tells
	/// the client the server has moved on, and it is what a real form supplies constantly: a lookup, a detail view, another
	/// form on the same session. The fourth case is the same situation with that one line left out, which is the window this
	/// fix deliberately leaves open - the NOTE: at the staleness test says why - and is also what holds the optimization in
	/// place: a client that believes its plan current must still answer out of its own copy, and no other case here could
	/// tell, because a current plan's local answer and the server's answer agree by construction.
	/// </para>
	/// <para>
	/// Every case asserts that the client's copy of the table variable still says the call is remotable, before asking.
	/// Without that a case could pass for the wrong reason - a client that went to the server for some unrelated reason would
	/// satisfy it while exercising nothing - and with it, what sends the proposal to the server is the staleness test alone.
	/// </para>
	/// <para>
	/// TypeChangeSummaryTest's view and restriction shapes are deliberately not mirrored: on the client those exercise the
	/// client's own node tree, which that fixture already pins, and what is new here is the permission rather than the descent.
	/// Nor are its two rolled-back cases: their client copy is taken at prepare time and therefore before the drop, so it is
	/// the correct copy either way and the case would exercise no invalidation on this side at all.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class RemoteProposalStalenessTest : ClientStackTestFixture
	{
		/// <summary>Told to the client's copy of the table variable, which is why a case that reaches the server proves something.</summary>
		private const string StillRemotable = "The client's copy must still say it may answer this itself, or the case exercises nothing.";

		/// <summary>
		/// A check created on a data type refuses the value it rejects, through a client cursor that was open before the
		/// statement. Nothing was ever stored wrong - the insert checks the constraint by a path that consults none of this -
		/// so what this restores is the report the user gets on leaving the field rather than on saving.
		/// </summary>
		[Test]
		public void TestCreatedTypeConstraintRefusesThroughAnOpenClientCursor()
		{
			ExecuteScript("create type RPSConstraintT { representation RPSConstraintT { Value : Integer } };");
			ExecuteScript("create table RPSConstraintTbl { ID : Integer, Amount : RPSConstraintT, key { ID } };");

			WhileBothCursorsStayOpen
			(
				"RPSConstraintTbl",
				"alter type RPSConstraintT { create constraint IsPositive value.Value > 0 };",
				true,
				(clientCursor, engineCursor) =>
				{
					Assert.That(clientCursor.Plan.TableVar.IsValidateCallRemotable(String.Empty), Is.True, StillRemotable);

					Exception refusedByClient = RefusalOf(clientCursor, 0);
					Assert.That(refusedByClient != null, Is.EqualTo(RefusalOf(engineCursor, 0) != null), "The client stack and the engine must give the same verdict.");
					Assert.That(refusedByClient, Is.Not.Null, "A value the new check rejects must be refused.");
					// Named, because this is the one case whose client answer now comes back over the remote path, where an
					// incidental failure would read as a refusal. TypeChangeSummaryTest names it the same way on the engine side.
					Assert.That(refusedByClient.ToString(), Does.Contain("IsPositive"), "The refusal must be the new check's.");
				}
			);
		}

		/// <summary>A default created on a data type is offered to a client cursor that was open before the statement.</summary>
		[Test]
		public void TestCreatedTypeDefaultIsOfferedThroughAnOpenClientCursor()
		{
			ExecuteScript("create type RPSDefaultT { representation RPSDefaultT { Value : Integer } };");
			ExecuteScript("create table RPSDefaultTbl { ID : Integer, Amount : RPSDefaultT, key { ID } };");

			WhileBothCursorsStayOpen
			(
				"RPSDefaultTbl",
				"alter type RPSDefaultT { create default RPSDefaultT(7) };",
				true,
				(clientCursor, engineCursor) =>
				{
					Assert.That(clientCursor.Plan.TableVar.IsDefaultCallRemotable(String.Empty), Is.True, StillRemotable);

					object defaultedByClient = DefaultOf(clientCursor, "Amount");
					Assert.That(defaultedByClient, Is.EqualTo(DefaultOf(engineCursor, "Amount")), "The client stack and the engine must propose the same value.");
					Assert.That(defaultedByClient, Is.EqualTo(7), "The new default must be the value proposed for a new row.");
				}
			);
		}

		/// <summary>
		/// A change handler attached to a data type runs for a client cursor that was open before the attach. This is the
		/// case that needs the attach to have moved Catalog.CacheTimeStamp at all, which it does since
		/// bug-attached-handler-never-reaches-a-remote-client.
		/// </summary>
		[Test]
		public void TestAttachedTypeChangeHandlerRunsForAnOpenClientCursor()
		{
			ExecuteScript("create type RPSChangeT { representation RPSChangeT { Value : Integer } };");
			ExecuteScript("create table RPSChangeTbl { ID : Integer, Amount : RPSChangeT, key { ID } };");
			ExecuteScript("create operator RPSChangeT_Change(var AValue : RPSChangeT) : Boolean begin result := true; AValue := RPSChangeT(99); end;");

			WhileBothCursorsStayOpen
			(
				"RPSChangeTbl",
				"attach operator RPSChangeT_Change to RPSChangeT on { change };",
				true,
				(clientCursor, engineCursor) =>
				{
					Assert.That(clientCursor.Plan.TableVar.IsChangeCallRemotable("Amount"), Is.True, StillRemotable);

					object changedByClient = AmountAfterChange(clientCursor, 1, 2);
					Assert.That(changedByClient, Is.EqualTo(AmountAfterChange(engineCursor, 1, 2)), "The client stack and the engine must leave the same value behind.");
					Assert.That(changedByClient, Is.EqualTo(99), "The attached handler must have run.");
				}
			);
		}

		/// <summary>
		/// The window left open, and the optimization that leaves it open. A client that has heard nothing from the server
		/// since its plan was prepared goes on answering out of its own copy, which is the whole reason the permission
		/// exists - and so it goes on giving the pre-change answer, and the user meets the new check on saving instead of on
		/// leaving the field. Identical to the first case but for the one unrelated expression it does not evaluate.
		/// </summary>
		/// <remarks>
		/// This pins a cost, not a goal. If the window is ever closed - a round trip per proposal became acceptable, or a
		/// server-to-client notification channel was built - this case is the one that fails, and the right response is to
		/// rewrite it to assert the new behaviour rather than to restore the old.
		/// </remarks>
		[Test]
		public void TestAClientThatHasHeardNothingStillAnswersFromItsOwnCopy()
		{
			ExecuteScript("create type RPSWindowT { representation RPSWindowT { Value : Integer } };");
			ExecuteScript("create table RPSWindowTbl { ID : Integer, Amount : RPSWindowT, key { ID } };");

			WhileBothCursorsStayOpen
			(
				"RPSWindowTbl",
				"alter type RPSWindowT { create constraint IsPositive value.Value > 0 };",
				false,
				(clientCursor, engineCursor) =>
				{
					Assert.That(clientCursor.Plan.TableVar.IsValidateCallRemotable(String.Empty), Is.True, StillRemotable);

					Assert.That(RefusalOf(engineCursor, 0), Is.Not.Null, "The engine must refuse the value, or this case says nothing about the client.");
					Assert.That
					(
						RefusalOf(clientCursor, 0),
						Is.Null,
						"A client told nothing about the change must still answer from its own copy; closing this window is what the NOTE: at the staleness test weighs."
					);
				}
			);
		}

		/// <summary>
		/// Opens a cursor over <paramref name="tableName"/> on each stack, runs <paramref name="alterStatement"/> on the
		/// engine while both stay open - which is how an open form reaches the table - and hands the two cursors to
		/// <paramref name="ask"/>.
		/// </summary>
		/// <param name="tellTheClient">
		/// Whether the client is given the one unrelated prepared expression that raises its highest-known catalog
		/// generation. A real form supplies that constantly; a form that sits idle - or that only posts statements, which do
		/// not carry the newer generation - supplies none, which is the fourth case.
		/// </param>
		private void WhileBothCursorsStayOpen(string tableName, string alterStatement, bool tellTheClient, Action<IServerCursor, IServerCursor> ask)
		{
			string expression = String.Format("select {0} capabilities {{ updateable }}", tableName);
			IServerCursor clientCursor = ClientProcess.OpenCursor(expression, null);
			try
			{
				IServerCursor engineCursor = EngineProcess.OpenCursor(expression, null);
				try
				{
					ExecuteScript(alterStatement);

					// Any prepared expression does it, because the server reports the catalog cache as changed on every
					// prepare (RemoteServerPlan.GetCatalog) and the client raises its own value from the descriptor
					// (LocalServer.EnsureCacheConsistent). One line of D4 that names nothing in this test is the smallest
					// thing that does, and makes the point that it need not be related to the table being edited. A
					// statement would not do: no descriptor on that path reaches EnsureCacheConsistent.
					if (tellTheClient)
						ClientProcess.Evaluate("1", null);

					ask(clientCursor, engineCursor);
				}
				finally
				{
					EngineProcess.CloseCursor(engineCursor);
				}
			}
			finally
			{
				ClientProcess.CloseCursor(clientCursor);
			}
		}

		/// <summary>Offers a whole new row for checking, as a client does before sending it, and returns the refusal, or null where the row was accepted.</summary>
		/// <remarks>
		/// The exception rather than a flag, so that a case can name what refused the row: the two stacks' messages are not
		/// meant to be identical - the client stack wraps the engine's exception - but both carry the constraint's name, and
		/// a bare catch would be satisfied by any failure at all on the remote path a stale plan now takes.
		/// </remarks>
		private static Exception RefusalOf(IServerCursor cursor, int amount)
		{
			IRow row = cursor.Plan.RequestRow();
			try
			{
				row.ClearValues();
				row["ID"] = 1;
				row["Amount"] = amount;
				try
				{
					cursor.Validate(null, row, String.Empty);
					return null;
				}
				catch (Exception exception)
				{
					return exception;
				}
			}
			finally
			{
				cursor.Plan.ReleaseRow(row);
			}
		}

		/// <summary>The value proposed for one column of a whole new row, or null where none is proposed.</summary>
		private static object DefaultOf(IServerCursor cursor, string columnName)
		{
			IRow row = cursor.Plan.RequestRow();
			try
			{
				row.ClearValues();
				cursor.Default(row, String.Empty);
				return row.HasValue(columnName) ? row[columnName] : null;
			}
			finally
			{
				cursor.Plan.ReleaseRow(row);
			}
		}

		/// <summary>Says the user has changed Amount, as a client does when the user leaves the field, and returns what was left in the row.</summary>
		private static object AmountAfterChange(IServerCursor cursor, int oldAmount, int newAmount)
		{
			IRow oldRow = cursor.Plan.RequestRow();
			try
			{
				IRow newRow = cursor.Plan.RequestRow();
				try
				{
					FillRow(oldRow, oldAmount);
					FillRow(newRow, newAmount);
					cursor.Change(oldRow, newRow, "Amount");
					return newRow.HasValue("Amount") ? newRow["Amount"] : null;
				}
				finally
				{
					cursor.Plan.ReleaseRow(newRow);
				}
			}
			finally
			{
				cursor.Plan.ReleaseRow(oldRow);
			}
		}

		private static void FillRow(IRow row, int amount)
		{
			row.ClearValues();
			row["ID"] = 1;
			row["Amount"] = amount;
		}
	}
}
