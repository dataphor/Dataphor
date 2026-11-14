/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

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
	}
}
