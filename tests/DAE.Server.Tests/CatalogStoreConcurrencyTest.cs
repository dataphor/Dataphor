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
	/// The SQLite catalog store under sessions that change the schema at the same time (K10), its lock-wait error, and its
	/// rollback at every isolation level (K26).
	/// </summary>
	[TestFixture]
	public class CatalogStoreConcurrencyTest
	{
		private const int SessionCount = 8;
		private const int CycleCount = 15;

		/// <summary>
		/// K10: the spike's concurrency loop. Each session creates, queries and drops objects of its own; any exception fails the
		/// test. On the rollback journal with deferred store transactions, 27 of the 120 cycles failed with "database is locked".
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
				Assert.That(CountStoredRows(instance.Server.GetCatalogStoreConnectionString(), "DAECatalogObjects", "K26T"), Is.EqualTo(0), "DAECatalogObjects");
				Assert.That(CountStoredRows(instance.Server.GetCatalogStoreConnectionString(), "DAEObjects", "K26T"), Is.EqualTo(0), "DAEObjects");

				instance.Server.Start();
				Exception exception = Assert.Catch(() => instance.Evaluate("Count(K26T)"));
				Assert.That(FindError(exception, (int)CompilerException.Codes.UnknownIdentifier), Is.Not.Null, exception.ToString());
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
		private static int CountStoredRows(string storeConnectionString, string tableName, string objectName)
		{
			// Without pooling, so that nothing holds the file open when the instance directory is deleted.
			SQLiteConnectionStringBuilder connectionString =
				new SQLiteConnectionStringBuilder
				{
					DataSource = new SQLiteConnectionStringBuilder(storeConnectionString).DataSource,
					ReadOnly = true,
					Pooling = false
				};
			using (SQLiteConnection connection = new SQLiteConnection(connectionString.ConnectionString))
			{
				connection.Open();
				using (SQLiteCommand command = new SQLiteCommand(String.Format("select count(*) from {0} where Name = @Name or Name like '%.' || @Name", tableName), connection))
				{
					command.Parameters.AddWithValue("@Name", objectName);
					return Convert.ToInt32(command.ExecuteScalar());
				}
			}
		}
	}
}
