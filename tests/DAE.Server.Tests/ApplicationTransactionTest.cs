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

		// A statement that changes the application transaction and then leaves in the same statement loses the change silently
		// on 3.1: the leaving statement's own implicit transaction holds the change, and the leave disconnects the application
		// transaction's session, whose rollback discards everything open on it. The leave now raises 125111 for this case too,
		// stays joined, and leaves the change in the implicit transaction intact; a try/except around the leave lets the implicit
		// transaction commit the insert into the application transaction, which then reaches the base table at commit.
		[Test]
		public void TestLeaveAfterSameStatementChangeRefused()
		{
			ExecuteScript("create table ATSameStatementChange { ID : Integer, key { ID } };");
			ExecuteScript("create table ATSameStatementChangeID { ID : Guid, key { } };");
			ExecuteScript("ATSameStatementChangeID := table { row { BeginApplicationTransaction() ID } };");
			ExecuteScript("JoinApplicationTransaction(ATSameStatementChangeID[].ID, false);");

			// One statement inserts into the application transaction's copy of the table and then tries to leave. The insert and
			// the leave share one implicit transaction; the leave raises 125111. The except block swallows that one code so the
			// implicit transaction commits normally, carrying the insert into the application transaction.
			ExecuteScript(@"
				begin
					insert row { 1 ID } into ATSameStatementChange;
					var LCaughtCode : Integer := 0;
					try
						LeaveApplicationTransaction();
					except
						on E : Error do
						begin
							LCaughtCode := E.Code;
							if LCaughtCode <> 125111 and not IsNil(E.InnerError) then
								LCaughtCode := E.InnerError.Code;
							if LCaughtCode <> 125111 then
								raise;
						end;
					end;
					if LCaughtCode <> 125111 then
						raise Error('The same-statement leave did not raise 125111.');
					if not exists (ATSameStatementChange where ID = 1) then
						raise Error('The refused leave discarded the insert made earlier in the same statement.');
				end;
			");

			// The refused leave did not disconnect; a separate-statement leave now succeeds and the insert reaches the base table at commit.
			Assert.That(Process.ApplicationTransactionID, Is.Not.EqualTo(Guid.Empty));
			ExecuteScript("LeaveApplicationTransaction();");
			ExecuteScript("if exists (ATSameStatementChange) then raise Error('The insert reached the base table before the application transaction committed.');");
			ExecuteScript("CommitApplicationTransaction(ATSameStatementChangeID[].ID);");
			ExecuteScript("if not exists (ATSameStatementChange where ID = 1) then raise Error('The application transaction commit lost the insert.');");
		}

		// A statement that joins and then leaves in one block, without changing anything in the application transaction, still
		// succeeds: only the leaving statement's own implicit transaction is open, and it has recorded no work.
		[Test]
		public void TestLeaveAfterSameStatementReadSucceeds()
		{
			ExecuteScript("create table ATSameStatementRead { ID : Integer, key { ID } };");
			ExecuteScript("create table ATSameStatementReadID { ID : Guid, key { } };");
			ExecuteScript("ATSameStatementReadID := table { row { BeginApplicationTransaction() ID } };");

			// One statement joins the application transaction, reads from one of its tables (empty, so no rows are copied in) and leaves.
			ExecuteScript(@"
				begin
					JoinApplicationTransaction(ATSameStatementReadID[].ID, false);
					if exists (ATSameStatementRead) then
						raise Error('The empty table reported a row.');
					LeaveApplicationTransaction();
				end;
			");
			Assert.That(Process.ApplicationTransactionID, Is.EqualTo(Guid.Empty));

			ExecuteScript("RollbackApplicationTransaction(ATSameStatementReadID[].ID);");
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
