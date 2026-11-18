/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Store;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The SQLite catalog store under sessions that change the schema at the same time (K10), its lock-wait error, its
	/// rollback at every isolation level (K26), and its write transactions, which begin at a transaction's first store write
	/// and through which that transaction reads.
	/// </summary>
	[TestFixture]
	public class CatalogStoreConcurrencyTest
	{
		private const int SessionCount = 8;
		private const int CycleCount = 15;

		/// <summary>
		/// K10: the spike's concurrency loop. Each session creates, queries and drops objects of its own; any exception fails the
		/// test. On the rollback journal with deferred store transactions, 27 of the 120 cycles failed with "database is locked".
		/// With the store's write transaction begun only at a transaction's first store write, a drop waited for the write lock
		/// inside the catalog's lock while the lock's holder waited for the catalog's lock: 42 cycles failed with 130103, in 264 s.
		/// </summary>
		[Test]
		public void ConcurrentDDLWithQueriesAndDrops()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(ConcurrentDDLWithQueriesAndDrops)))
			{
				instance.Server.Start();

				ConcurrentBag<string> failures = new ConcurrentBag<string>();
				Exception firstFailure = null;
				Stopwatch watch = Stopwatch.StartNew();
				using Barrier start = new Barrier(SessionCount);
				List<Thread> threads = new List<Thread>();
				for (int session = 0; session < SessionCount; session++)
				{
					int sessionNumber = session;
					Thread thread =
						new Thread(() =>
						{
							bool reachedStart = false;
							try
							{
								IServerSession serverSession = instance.Connect();
								IServerProcess process = serverSession.StartProcess(new ProcessInfo(serverSession.SessionInfo));
								try
								{
									reachedStart = true;
									start.SignalAndWait();
									for (int cycle = 0; cycle < CycleCount; cycle++)
									{
										try
										{
											RunCycle(process, sessionNumber, cycle);
										}
										catch (Exception exception)
										{
											Interlocked.CompareExchange(ref firstFailure, exception, null);
											failures.Add(String.Format("session {0} cycle {1}: {2}", sessionNumber, cycle, Innermost(exception)));
										}
									}
								}
								finally
								{
									serverSession.StopProcess(process);
									instance.Server.Disconnect(serverSession);
								}
							}
							catch (Exception exception)
							{
								Interlocked.CompareExchange(ref firstFailure, exception, null);
								failures.Add(String.Format("session {0}: {1}", sessionNumber, Innermost(exception)));
								// Otherwise the other sessions would wait at the barrier for this one forever.
								if (!reachedStart)
									start.RemoveParticipant();
							}
						});
					threads.Add(thread);
					thread.Start();
				}
				foreach (Thread thread in threads)
					thread.Join();

				TestContext.Progress.WriteLine("{0} sessions x {1} cycles in {2} ms; {3} failed.", SessionCount, CycleCount, watch.ElapsedMilliseconds, failures.Count);
				Assert.That(failures, Is.Empty, String.Join("\r\n", failures) + "\r\nFirst failure in full:\r\n" + firstFailure);
			}
		}

		private static void RunCycle(IServerProcess process, int session, int cycle)
		{
			string suffix = String.Format("{0}_{1}", session, cycle);
			process.ExecuteScript
			(
				String.Format
				(
					"create table CT{0} {{ ID : Integer, Name : String, key {{ ID }} }};\r\n" +
					"insert table {{ row {{ 1 ID, 'a' Name }} }} into CT{0};\r\n" +
					"create view CV{0} CT{0} where ID > 0;\r\n" +
					"create operator CO{0}(const A : Integer) : Integer\r\nbegin\r\n\tresult := A + Count(CV{0});\r\nend;",
					suffix
				)
			);
			// The spike's query: the operator reads its view's one row, and the restriction on System.Operators goes to the store
			// (it matches no row, so the expected value is the operator's alone).
			object value = Evaluate(process, String.Format("CO{0}(1) + Count(System.Operators where Name = 'System.iAddition')", suffix));
			if (Convert.ToInt32(value) != 2)
				throw new InvalidOperationException(String.Format("CO{0}(1) + Count(...) was {1}; expected 2.", suffix, value));
			process.ExecuteScript(String.Format("drop operator CO{0}(Integer);\r\ndrop view CV{0};\r\ndrop table CT{0};", suffix));
		}

		/// <summary>
		/// The store runs in write-ahead-logging mode, so that readers never wait for the writer (K10), even when the connection
		/// string asks for the rollback journal.
		/// </summary>
		[Test]
		public void StoreForcesWriteAheadLog()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(StoreForcesWriteAheadLog)))
			{
				instance.Server.CatalogStoreConnectionString = TestServerInstance.CatalogStoreConnectionString + ";journal mode=Delete;JournalMode=Truncate";
				instance.Server.Start();

				using (SQLiteConnection connection = instance.OpenCatalogStoreFile())
				using (SQLiteCommand command = new SQLiteCommand("pragma journal_mode", connection))
					Assert.That(command.ExecuteScalar(), Is.EqualTo("wal"));
			}
		}

		/// <summary>
		/// A session that waits for the store's write lock longer than BusyTimeout fails with StoreException 130103, within about
		/// that time, and can retry once the lock is free.
		/// </summary>
		[Test]
		public void StoreLockWaitReportsStoreLocked()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(StoreLockWaitReportsStoreLocked)))
			{
				instance.Server.CatalogStoreConnectionString = TestServerInstance.CatalogStoreConnectionString + ";BusyTimeout=1000";
				instance.Server.Start();

				IServerSession sessionA = instance.Connect();
				IServerSession sessionB = instance.Connect();
				IServerProcess processA = sessionA.StartProcess(new ProcessInfo(sessionA.SessionInfo));
				IServerProcess processB = sessionB.StartProcess(new ProcessInfo(sessionB.SessionInfo));
				try
				{
					// A's open transaction has written to the store, so it holds the write lock until it ends.
					processA.BeginTransaction(IsolationLevel.CursorStability);
					processA.ExecuteScript("create table LockA { ID : Integer, key { ID } };");

					Stopwatch watch = Stopwatch.StartNew();
					Exception exception = Assert.Catch(() => processB.ExecuteScript("create table LockB { ID : Integer, key { ID } };"));
					watch.Stop();
					TestContext.Progress.WriteLine("B failed after {0} ms.", watch.ElapsedMilliseconds);

					DataphorException storeLocked = FindError(exception, (int)StoreException.Codes.StoreLocked);
					Assert.That(storeLocked, Is.Not.Null, exception.ToString());
					Assert.That(storeLocked.Message, Does.Contain("within 1 seconds"));
					Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(10)));

					processA.CommitTransaction();
					processB.ExecuteScript("create table LockB { ID : Integer, key { ID } };");
					Assert.That(Evaluate(processB, "Count(LockA) + Count(LockB)"), Is.EqualTo(0));
				}
				finally
				{
					sessionA.StopProcess(processA);
					sessionB.StopProcess(processB);
					instance.Server.Disconnect(sessionA);
					instance.Server.Disconnect(sessionB);
				}
			}
		}

		/// <summary>
		/// K26: under Browse, the lowest isolation level, the store once opened no transaction of its own, so a rollback undid
		/// the in-memory catalog but kept the stored rows, and the rolled-back table came back from the store.
		/// </summary>
		[Test]
		public void BrowseRollbackRollsBackStore()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(BrowseRollbackRollsBackStore)))
			{
				instance.Server.Start();

				IServerSession session = instance.Connect();
				try
				{
					IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
					try
					{
						process.BeginTransaction(IsolationLevel.Browse);
						process.ExecuteScript("create table K26T { ID : Integer, key { ID } };");
						process.RollbackTransaction();
					}
					finally
					{
						session.StopProcess(process);
					}
				}
				finally
				{
					instance.Server.Disconnect(session);
				}

				instance.Server.Stop();
				Assert.That(CountStoredRows(instance, "DAECatalogObjects", "K26T"), Is.EqualTo(0), "DAECatalogObjects");
				Assert.That(CountStoredRows(instance, "DAEObjects", "K26T"), Is.EqualTo(0), "DAEObjects");

				instance.Server.Start();
				Exception exception = Assert.Catch(() => instance.Evaluate("Count(K26T)"));
				Assert.That(FindError(exception, (int)CompilerException.Codes.UnknownIdentifier), Is.Not.Null, exception.ToString());
			}
		}

		/// <summary>
		/// K10, the long transaction: while one session's transaction holds the store's write lock (it has changed the catalog
		/// and stays open), other sessions' catalog reads run without waiting, in explicit transactions too and from a session
		/// that has written before; only another catalog write waits, and completes once the lock is free. When every
		/// transaction of a session that had written once began a store write transaction, B's transaction waited for A's and
		/// failed with 130103 after 30 s.
		/// </summary>
		[Test]
		public void LongDDLTransactionBlocksOnlyWriters()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(LongDDLTransactionBlocksOnlyWriters)))
			{
				instance.Server.Start();
				instance.ExecuteScript
				(
					"create table LReadT { ID : Integer, key { ID } };\r\n" +
					"create operator LReadO(const A : Integer) : Integer\r\nbegin\r\n\tresult := A + 1;\r\nend;"
				);
				// So that the readers below resolve the table and the operator from the store, not from memory. (The table's rows
				// are in the memory device and do not survive the restart.)
				instance.Server.Stop();
				instance.Server.Start();

				IServerSession sessionA = instance.Connect();
				IServerSession sessionB = instance.Connect();
				IServerSession sessionC = instance.Connect();
				IServerSession sessionD = instance.Connect();
				IServerProcess processA = sessionA.StartProcess(new ProcessInfo(sessionA.SessionInfo));
				IServerProcess processB = sessionB.StartProcess(new ProcessInfo(sessionB.SessionInfo));
				IServerProcess processC = sessionC.StartProcess(new ProcessInfo(sessionC.SessionInfo));
				IServerProcess processD = sessionD.StartProcess(new ProcessInfo(sessionD.SessionInfo));
				Thread writerD = null;
				try
				{
					processB.ExecuteScript("create table LB0 { ID : Integer, key { ID } };");

					processA.BeginTransaction(IsolationLevel.CursorStability);
					processA.ExecuteScript("create table TA { ID : Integer, key { ID } };");

					Stopwatch watch = Stopwatch.StartNew();
					processB.BeginTransaction(IsolationLevel.CursorStability);
					RunCatalogReads(processB);
					processB.CommitTransaction();
					TestContext.Progress.WriteLine("B's read transaction took {0} ms.", watch.ElapsedMilliseconds);
					Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(10)), "B's read transaction");

					watch.Restart();
					RunCatalogReads(processC);
					TestContext.Progress.WriteLine("C's reads took {0} ms.", watch.ElapsedMilliseconds);
					Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(10)), "C's reads");

					Exception failureD = null;
					writerD =
						new Thread(() =>
						{
							try
							{
								processD.ExecuteScript("create table TD { ID : Integer, key { ID } };");
							}
							catch (Exception exception)
							{
								failureD = exception;
							}
						});
					writerD.Start();
					Assert.That(writerD.Join(1000), Is.False, "D's create table did not wait for A's transaction: " + failureD);

					processA.CommitTransaction();
					Assert.That(writerD.Join(TimeSpan.FromSeconds(20)), Is.True, "D's create table did not complete after A committed.");
					writerD = null;
					Assert.That(failureD, Is.Null, failureD?.ToString());

					Assert.That(Evaluate(processB, "Count(TA) + Count(TD)"), Is.EqualTo(0));
				}
				finally
				{
					if (processA.InTransaction)
						processA.RollbackTransaction();
					// A's rollback releases the lock D waits for.
					writerD?.Join(TimeSpan.FromSeconds(40));
					sessionA.StopProcess(processA);
					sessionB.StopProcess(processB);
					sessionC.StopProcess(processC);
					sessionD.StopProcess(processD);
					instance.Server.Disconnect(sessionA);
					instance.Server.Disconnect(sessionB);
					instance.Server.Disconnect(sessionC);
					instance.Server.Disconnect(sessionD);
				}
			}
		}

		// Resolves and counts a table, queries a catalog table that the store answers, and calls an operator.
		private static void RunCatalogReads(IServerProcess process)
		{
			Assert.That(Evaluate(process, "Count(LReadT)"), Is.EqualTo(0), "Count(LReadT)");
			Assert.That(Evaluate(process, "Count(System.Objects where Name = 'General.LReadT')"), Is.EqualTo(1), "System.Objects");
			Assert.That(Evaluate(process, "LReadO(1)"), Is.EqualTo(2), "LReadO(1)");
		}

		/// <summary>
		/// A transaction reads its own uncommitted store writes: the dependents check of a drop reads DAEObjectDependencies from
		/// the store (CatalogDeviceSession.SelectObjectDependents), and must see the dependency of a view created earlier in the
		/// same transaction. After the rollback nothing of it remains, in memory or in the store, nor in the server's caches: the
		/// texts compiled in the transaction, and new calls of its operator and conversion, fail. Before the rollback invalidated
		/// the plan, operator resolution and conversion path caches, Count(RWT), RWO(1), RWO(3) + 0 and 2 + true still evaluated.
		/// </summary>
		[Test]
		public void TransactionReadsItsOwnStoreWrites()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(TransactionReadsItsOwnStoreWrites)))
			{
				instance.Server.Start();

				IServerSession session = instance.Connect();
				try
				{
					IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
					try
					{
						process.BeginTransaction(IsolationLevel.CursorStability);
						process.ExecuteScript("create table RWT { ID : Integer, key { ID } };\r\ninsert table { row { 1 ID } } into RWT;");
						Assert.That(Evaluate(process, "Count(RWT)"), Is.EqualTo(1));

						process.ExecuteScript("create view RWV RWT where ID > 0;");
						Exception exception = Assert.Catch(() => process.ExecuteScript("drop table RWT;"));
						Assert.That(FindError(exception, (int)CompilerException.Codes.ObjectHasDependents), Is.Not.Null, exception.ToString());

						process.ExecuteScript("create operator RWO(const A : Integer) : Integer\r\nbegin\r\n\tresult := A + Count(RWV);\r\nend;");
						Assert.That(Evaluate(process, "RWO(1)"), Is.EqualTo(2));

						process.ExecuteScript("create operator RWC(const A : Boolean) : Integer\r\nbegin\r\n\tresult := if A then 1 else 0;\r\nend;\r\ncreate conversion Boolean to Integer using RWC widening;");
						Assert.That(Evaluate(process, "1 + true"), Is.EqualTo(2));
						process.RollbackTransaction();

						foreach (string expression in new[] { "Count(RWT)", "Count(RWT where ID = 1)", "Count(RWV where ID = 1)" })
						{
							Exception unknown = Assert.Catch(() => Evaluate(process, expression), expression);
							Assert.That(FindError(unknown, (int)CompilerException.Codes.UnknownIdentifier), Is.Not.Null, unknown.ToString());
						}

						// RWO(3) + 0 and 2 + true are new texts, so only the operator resolution and conversion path caches could
						// answer them.
						foreach (string expression in new[] { "RWO(1)", "RWO(3) + 0", "2 + true" })
						{
							Exception notFound = Assert.Catch(() => Evaluate(process, expression), expression);
							Assert.That(FindError(notFound, (int)CompilerException.Codes.OperatorNotFound), Is.Not.Null, notFound.ToString());
						}
					}
					finally
					{
						session.StopProcess(process);
					}
				}
				finally
				{
					instance.Server.Disconnect(session);
				}

				instance.Server.Stop();
				Assert.That(CountStoredRows(instance, "DAECatalogObjects", "RWT"), Is.EqualTo(0), "RWT");
				Assert.That(CountStoredRows(instance, "DAECatalogObjects", "RWV"), Is.EqualTo(0), "RWV");
				Assert.That(CountStoredOperatorRows(instance, "RWO"), Is.EqualTo(0), "RWO");

				instance.Server.Start();
				Exception unknownOperator = Assert.Catch(() => instance.Evaluate("RWO(1)"));
				Assert.That(FindError(unknownOperator, (int)CompilerException.Codes.OperatorNotFound), Is.Not.Null, unknownOperator.ToString());
			}
		}

		/// <summary>
		/// A session whose statement fails waiting for the store's write lock is left outside any transaction, and its later
		/// statements commit normally. When a session that had written once began a store write transaction at the start of each
		/// transaction, the busy error came from that begin and left the process half-begun (InTransaction true, count 1): the
		/// next statement then ran inside it, and B3 was lost when B disconnected.
		/// </summary>
		[Test]
		public void BusyAtTransactionStartLeavesNoTransaction()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(BusyAtTransactionStartLeavesNoTransaction)))
			{
				instance.Server.CatalogStoreConnectionString = TestServerInstance.CatalogStoreConnectionString + ";BusyTimeout=1000";
				instance.Server.Start();

				IServerSession sessionA = instance.Connect();
				IServerSession sessionB = instance.Connect();
				IServerProcess processA = sessionA.StartProcess(new ProcessInfo(sessionA.SessionInfo));
				IServerProcess processB = sessionB.StartProcess(new ProcessInfo(sessionB.SessionInfo));
				try
				{
					processB.ExecuteScript("create table B1 { ID : Integer, key { ID } };");

					processA.BeginTransaction(IsolationLevel.CursorStability);
					processA.ExecuteScript("create table A1 { ID : Integer, key { ID } };");

					Exception exception = Assert.Catch(() => processB.ExecuteScript("create table B2 { ID : Integer, key { ID } };"));
					Assert.That(FindError(exception, (int)StoreException.Codes.StoreLocked), Is.Not.Null, exception.ToString());
					Assert.That(processB.InTransaction, Is.False, "B is in a transaction after the busy error.");

					processA.CommitTransaction();
					processB.ExecuteScript("create table B3 { ID : Integer, key { ID } };");
				}
				finally
				{
					if (processA.InTransaction)
						processA.RollbackTransaction();
					sessionA.StopProcess(processA);
					sessionB.StopProcess(processB);
					instance.Server.Disconnect(sessionA);
					instance.Server.Disconnect(sessionB);
				}

				instance.Server.Stop();
				instance.Server.Start();
				Assert.That(instance.Evaluate("Count(B3)"), Is.EqualTo(0));
				Exception unknown = Assert.Catch(() => instance.Evaluate("Count(B2)"));
				Assert.That(FindError(unknown, (int)CompilerException.Codes.UnknownIdentifier), Is.Not.Null, unknown.ToString());
			}
		}

		private static object Evaluate(IServerProcess process, string expression)
		{
			return ((ServerProcess)process).Evaluate(expression, null).AsNative;
		}

		private static DataphorException FindError(Exception exception, int code)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is DataphorException dataphorException && dataphorException.Code == code)
					return dataphorException;
			return null;
		}

		private static string Innermost(Exception exception)
		{
			while (exception.InnerException != null)
				exception = exception.InnerException;
			return exception.GetType().Name + ": " + exception.Message;
		}

		// Rows of the given store table whose Name is the object name, library-qualified or not.
		private static int CountStoredRows(TestServerInstance instance, string tableName, string objectName)
		{
			using (SQLiteConnection connection = instance.OpenCatalogStoreFile())
			{
				using (SQLiteCommand command = new SQLiteCommand(String.Format("select count(*) from {0} where Name = @Name or Name like '%.' || @Name", tableName), connection))
				{
					command.Parameters.AddWithValue("@Name", objectName);
					return Convert.ToInt32(command.ExecuteScalar());
				}
			}
		}

		// Catalog object rows of operators of the given name, library-qualified or not (an operator's object name is mangled
		// with its signature; DAEOperators keeps the name as written).
		private static int CountStoredOperatorRows(TestServerInstance instance, string operatorName)
		{
			using (SQLiteConnection connection = instance.OpenCatalogStoreFile())
			{
				using
				(
					SQLiteCommand command =
						new SQLiteCommand
						(
							"select count(*) from DAECatalogObjects C join DAEOperators O on O.ID = C.ID where O.OperatorName = @Name or O.OperatorName like '%.' || @Name",
							connection
						)
				)
				{
					command.Parameters.AddWithValue("@Name", operatorName);
					return Convert.ToInt32(command.ExecuteScalar());
				}
			}
		}
	}
}
