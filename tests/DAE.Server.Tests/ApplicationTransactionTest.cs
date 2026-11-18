/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Device.ApplicationTransaction;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	[TestFixture]
	public class ApplicationTransactionTest : InProcessTestFixture
	{
		// A statement that runs inside another statement's process transaction (here through Execute) and first references a
		// table in an application transaction makes the transaction's copy of the table inside the catalog's lock. The copy's
		// DDL writes nothing to the catalog store, so it must not begin the store's write transaction there: in Debug that
		// fails an assertion, and in Release it waits for the store's write lock while holding the catalog's lock.
		[Test]
		public void TestEnlistInsideStatement()
		{
			ExecuteScript("create table ATEnlistInsideStatement { ID : Integer, key { ID } };");
			ExecuteScript("create table ATEnlistInsideStatementID { ID : Guid, key { } };");
			ExecuteScript("ATEnlistInsideStatementID := table { row { BeginApplicationTransaction() ID } };");
			ExecuteScript("JoinApplicationTransaction(ATEnlistInsideStatementID[].ID, false);");
			ExecuteScript("Execute('select ATEnlistInsideStatement where ID = 1;');");
		}

		// Leaving an application transaction disconnects its device session, which rolls back the transactions open on that session.
		// Leaving while a process transaction is open would discard the work done in the application transaction during it, so the
		// leave is refused (125111) and the process stays joined with its work intact. Each line runs as its own batch.
		[Test]
		public void TestLeaveInsideTransactionRefused()
		{
			ExecuteScript("create table ATLeaveInTransaction { ID : Integer, key { ID } };");
			ExecuteScript("create table ATLeaveInTransactionID { ID : Guid, key { } };");
			ExecuteScript("ATLeaveInTransactionID := table { row { BeginApplicationTransaction() ID } };");
			// This batch runs in the implicit transaction it began, which stays open because implicit transactions are off when the batch ends
			ExecuteScript("SetUseImplicitTransactions(false);");
			ExecuteScript("BeginTransaction();");
			ExecuteScript("JoinApplicationTransaction(ATLeaveInTransactionID[].ID, false);");
			ExecuteScript("insert row { 1 ID } into ATLeaveInTransaction;");

			AssertLeaveRefused(() => ExecuteScript("LeaveApplicationTransaction();"));
			AssertLeaveRefused(() => Process.LeaveApplicationTransaction());
			Assert.That(Process.ApplicationTransactionID, Is.Not.EqualTo(Guid.Empty));
			ExecuteScript("if not exists (ATLeaveInTransaction) then raise Error('The refused leave discarded the application transaction''s work.');");

			// The transaction SetUseImplicitTransactions left open now holds the insert
			ExecuteScript("CommitTransaction();");
			AssertLeaveRefused(() => ExecuteScript("LeaveApplicationTransaction();"));
			ExecuteScript("CommitTransaction();");

			ExecuteScript("SetUseImplicitTransactions(true);");
			ExecuteScript("LeaveApplicationTransaction();");
			ExecuteScript("if exists (ATLeaveInTransaction) then raise Error('The insert reached the table before the application transaction committed.');");
			ExecuteScript("CommitApplicationTransaction(ATLeaveInTransactionID[].ID);");
			ExecuteScript("if not exists (ATLeaveInTransaction) then raise Error('The application transaction commit lost the insert.');");
		}

		// With implicit transactions off and no transaction open, committing an application transaction replays its operations
		// outside any transaction. It used to fail with a NullReferenceException, recording each operation for a rollback that
		// cannot happen.
		[Test]
		public void TestCommitOutsideTransaction()
		{
			ExecuteScript("create table ATCommitOutsideTransaction { ID : Integer, key { ID } };");
			ExecuteScript("create table ATCommitOutsideTransactionID { ID : Guid, key { } };");
			ExecuteScript("ATCommitOutsideTransactionID := table { row { BeginApplicationTransaction() ID } };");
			ExecuteScript("JoinApplicationTransaction(ATCommitOutsideTransactionID[].ID, false);");
			ExecuteScript("insert row { 1 ID } into ATCommitOutsideTransaction;");
			ExecuteScript("LeaveApplicationTransaction();");
			// Not SetUseImplicitTransactions(false): its own batch's implicit transaction would stay open
			Process.ProcessInfo.UseImplicitTransactions = false;
			ExecuteScript("CommitApplicationTransaction(ATCommitOutsideTransactionID[].ID);");
			ExecuteScript("if not exists (ATCommitOutsideTransaction) then raise Error('The application transaction commit lost the insert.');");
		}

		private static void AssertLeaveRefused(TestDelegate leave)
		{
			Exception exception = Assert.Catch(leave);
			for (Exception inner = exception; inner != null; inner = inner.InnerException)
				if (inner is DataphorException dataphorException && dataphorException.Code == (int)ApplicationTransactionException.Codes.LeaveInTransaction)
					return;
			Assert.Fail(exception.ToString());
		}
	}
}
