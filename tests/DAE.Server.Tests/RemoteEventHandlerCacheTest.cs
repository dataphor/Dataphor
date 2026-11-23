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
	/// Attaching or detaching an event handler must reach a client outside the server, without that client reconnecting or
	/// having to word its query differently.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An event handler never runs on a client: a handler is not remotable, so it is deliberately left out of the catalog a
	/// client is sent (Schema.TableVar.IncludeHandlers filters on that, and Schema.EventHandler.DetermineRemotable makes it
	/// false for every handler). What a client is sent instead, with every prepared plan, is a description of the result that
	/// says for each column whether the client may answer a Change, Default or Validate proposal out of its own copy of the
	/// definition or has to call the server for it - ServerProcess.CopyDataType emits those as DAE.Is*Remotable tags on the
	/// expression the client compiles. Attaching a handler flips that answer from "answer it yourself" to "call the server".
	/// </para>
	/// <para>
	/// That description is built when the plan is compiled and kept with the plan in the server's plan cache, which is keyed
	/// by statement text and evicted by Schema.Catalog.PlanCacheTimeStamp. CreateEventHandlerNode and AlterEventHandlerNode
	/// moved no such counter, so a client re-preparing the same statement text after the attach was handed the description
	/// built before it, went on answering the proposal itself out of a copy with no handler, and the handler never ran for
	/// that user - not in the form that was open at the time, and not in one opened afterwards either. A client whose query
	/// happened to be worded differently from any prepared before the attach got the right answer, which is why this is
	/// invisible to anything that connects, does one thing and disconnects.
	/// </para>
	/// <para>
	/// A plan the client still holds open is the other half, and no eviction can reach it - nothing re-sends an open plan its
	/// description. What the client has for free there is Schema.Catalog.CacheTimeStamp, which these nodes now move as well:
	/// the client keeps the highest value it has heard of and each plan carries the value it was prepared at, so a plan that
	/// is behind can be recognized locally. Consulting it in the proposal calls is bug-remote-form-is-never-told-about-a-type-change,
	/// and the cases for that belong with it; what is pinned here is that the counter moves, through the gate case below.
	/// </para>
	/// <para>
	/// Every case asks the same question of both stacks - the client's LocalCursor and the engine's own ServerCursor - and
	/// asserts that the two agree, as RemoteValidateTest does, and names the resulting value as well, so that a regression
	/// breaking both paths the same way could not pass. Each case also asks it once *before* the DDL statement, with the same
	/// statement text, which is both what puts the plan in the cache and the proof that the cached one is what the second
	/// question reaches.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class RemoteEventHandlerCacheTest : ClientStackTestFixture
	{
		private const int OldAmount = 1;
		private const int NewAmount = 2;

		/// <summary>The value every handler in this fixture puts in Amount, so that "the handler ran" is one value.</summary>
		private const int HandledAmount = 99;

		/// <summary>A handler attached to one column of a table variable.</summary>
		[Test]
		public void TestAttachingToAColumnReachesAClientHoldingIt()
		{
			ExecuteScript("create table HCColumnTbl { ID : Integer, Amount : Integer, key { ID } };");
			ExecuteScript("create operator HCColumnTbl_Amount_Change(var ARow : typeof(HCColumnTbl[])) : Boolean begin result := true; update ARow set { Amount := 99 }; end;");

			AssertClientAgreesWithEngine("HCColumnTbl", false, NewAmount, "Before the attach there is no handler for either stack to run.");

			ExecuteScript("attach operator HCColumnTbl_Amount_Change to Amount in HCColumnTbl on { change };");

			AssertClientAgreesWithEngine("HCColumnTbl", true, HandledAmount, "A handler attached to the column must run for a client that prepared this query before the attach.");
		}

		/// <summary>A handler attached to the table variable itself, the other kind of event source a client can meet this way.</summary>
		[Test]
		public void TestAttachingToATableReachesAClientHoldingIt()
		{
			ExecuteScript("create table HCTableTbl { ID : Integer, Amount : Integer, key { ID } };");
			ExecuteScript("create operator HCTableTbl_Change(var ARow : typeof(HCTableTbl[]), const AColumnName : String) : Boolean begin result := true; update ARow set { Amount := 99 }; end;");

			AssertClientAgreesWithEngine("HCTableTbl", false, NewAmount, "Before the attach there is no handler for either stack to run.");

			ExecuteScript("attach operator HCTableTbl_Change to HCTableTbl on { change };");

			AssertClientAgreesWithEngine("HCTableTbl", true, HandledAmount, "A handler attached to the table must run for a client that prepared this query before the attach.");
		}

		/// <summary>
		/// The other direction. DropEventHandlerNode has moved the counter since the nine nodes of
		/// bug-recreated-device-comes-back-as-the-one-that-was-dropped, for a different reason - a cached create handler plan
		/// re-attaching the handler just dropped - and this is the case that holds that bump in place for this reason too: a
		/// client whose plan still says "call the server" keeps paying a round trip for a handler that is gone.
		/// </summary>
		[Test]
		public void TestDetachingFromAColumnReachesAClientHoldingIt()
		{
			ExecuteScript("create table HCDetachTbl { ID : Integer, Amount : Integer, key { ID } };");
			ExecuteScript("create operator HCDetachTbl_Amount_Change(var ARow : typeof(HCDetachTbl[])) : Boolean begin result := true; update ARow set { Amount := 99 }; end;");
			ExecuteScript("attach operator HCDetachTbl_Amount_Change to Amount in HCDetachTbl on { change };");

			AssertClientAgreesWithEngine("HCDetachTbl", true, HandledAmount, "The client must be reaching the handler before it is detached, or the case proves nothing.");

			ExecuteScript("detach operator HCDetachTbl_Amount_Change from Amount in HCDetachTbl on { change };");

			AssertClientAgreesWithEngine("HCDetachTbl", false, NewAmount, "A detached handler must stop running for a client that prepared this query before the detach.");
		}

		/// <summary>
		/// What the gate on these bumps excludes. The server attaches handlers of its own to the copy an application
		/// transaction makes of each table it joins and detaches them when the transaction ends, both inside a
		/// time-stamp-safe context; a form edit cycle is made of application transactions, so an ungated bump would flush
		/// every session's cached plans, and make every connected client refetch its catalog copy, on every edit.
		/// </summary>
		[Test]
		public void TestAnApplicationTransactionMovesNeitherTimeStamp()
		{
			ExecuteScript("create table HCATTbl { ID : Integer, Amount : Integer, key { ID } };");
			ExecuteScript("create operator HCATTbl_Change(var ARow : typeof(HCATTbl[]), const AColumnName : String) : Boolean begin result := true; update ARow set { Amount := 99 }; end;");
			// The handler is what makes this case mean anything: the transaction's copy of the table attaches only the handlers
			// the source table has (ApplicationTransactionDevice.AddTableVar), so without one the copy runs no handler node.
			ExecuteScript("attach operator HCATTbl_Change to HCATTbl on { change };");
			ExecuteScript("create table HCATTblID { ID : Guid, key { } };");

			long planCacheBefore = Instance.Server.Catalog.PlanCacheTimeStamp;
			long cacheBefore = Instance.Server.Catalog.CacheTimeStamp;

			ExecuteScript("HCATTblID := table { row { BeginApplicationTransaction() ID } };");
			ExecuteScript("JoinApplicationTransaction(HCATTblID[].ID, false);");
			// Reaching the table inside the transaction is what makes its copy, with the attach of the copy's own handler.
			ExecuteScript("insert row { 1 ID, 1 Amount } into HCATTbl;");
			ExecuteScript("LeaveApplicationTransaction();");
			ExecuteScript("CommitApplicationTransaction(HCATTblID[].ID);");

			Assert.That
			(
				Instance.Server.Catalog.PlanCacheTimeStamp,
				Is.EqualTo(planCacheBefore),
				"An application transaction making and dropping its own copies of a table and its handlers must not evict any session's compiled plans."
			);
			Assert.That
			(
				Instance.Server.Catalog.CacheTimeStamp,
				Is.EqualTo(cacheBefore),
				"Nor must it make every connected client throw its catalog copy away."
			);
		}

		/// <summary>
		/// Asks both stacks what follows from the user changing Amount from 1 to 2 and asserts they agree, then names the
		/// outcome so that both stacks being wrong the same way could not pass. The client is asked first, because its plan is
		/// the one that has to notice the catalog change.
		/// </summary>
		private void AssertClientAgreesWithEngine(string tableName, bool changed, int amount, string because)
		{
			ChangeOutcome throughClient = Change(ClientProcess, tableName);
			ChangeOutcome throughEngine = Change(EngineProcess, tableName);

			Assert.That(throughClient, Is.EqualTo(throughEngine), because);
			Assert.That(throughClient.Changed, Is.EqualTo(changed), because);
			Assert.That(throughClient.Amount, Is.EqualTo(amount), because);
		}

		/// <summary>What came of offering the engine an edit of Amount: whether it proposed a change, and what Amount holds afterwards.</summary>
		private readonly record struct ChangeOutcome(bool Changed, object Amount);

		/// <summary>
		/// Opens an updateable cursor on <paramref name="process"/> and asks what follows from the user changing Amount, as a
		/// client does when the user leaves the field. The statement text is the same every time on purpose: the whole
		/// question is what a plan the server has already compiled for that text goes on saying.
		/// </summary>
		private static ChangeOutcome Change(IServerProcess process, string tableName)
		{
			IServerCursor cursor = process.OpenCursor(String.Format("select {0} capabilities {{ updateable }}", tableName), null);
			try
			{
				IRow oldRow = cursor.Plan.RequestRow();
				try
				{
					IRow newRow = cursor.Plan.RequestRow();
					try
					{
						FillRow(oldRow, OldAmount);
						FillRow(newRow, NewAmount);
						bool changed = cursor.Change(oldRow, newRow, "Amount");
						return new ChangeOutcome(changed, newRow.HasValue("Amount") ? newRow["Amount"] : null);
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
			finally
			{
				process.CloseCursor(cursor);
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
