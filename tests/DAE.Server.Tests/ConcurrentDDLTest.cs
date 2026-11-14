/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Device.Catalog;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Sessions creating objects at the same time must each store their own object's script (K1: the catalog device once
	/// shared one emitter between sessions, and a store that accepts any text kept the mixture silently).
	/// </summary>
	[TestFixture]
	public class ConcurrentDDLTest
	{
		private const int SessionCount = 16;
		private const int CycleCount = 15;
		// Sizes found by running against the shared emitter: 8 sessions caught it in 1 of 4 runs at 150 statements and 3 of 5 at
		// 400; 16 sessions at 400 caught it in 6 of 6. If it stops catching an unsafe emitter, raise the statement count first.
		private const int OperatorStatementCount = 400;

		private static string TableName(int session, int cycle) => String.Format("K1T_{0}_{1}", session, cycle);
		private static string ViewName(int session, int cycle) => String.Format("K1V_{0}_{1}", session, cycle);
		private static string OperatorName(int session, int cycle) => String.Format("K1O_{0}_{1}", session, cycle);

		private static string[] CycleScripts(int session, int cycle)
		{
			string table = TableName(session, cycle);
			StringBuilder body = new StringBuilder();
			for (int index = 0; index < OperatorStatementCount; index++)
				body.AppendFormat("\tvar V{0} : Integer := A + {0};\r\n\tresult := result + V{0};\r\n", index);
			return
				[
					String.Format("create table {0} {{ ID : Integer, Name : String, Description : String, Amount : Decimal, Quantity : Integer, key {{ ID }} }};", table),
					String.Format("create view {0} {1} where ID > 0 over {{ ID, Name, Amount }};", ViewName(session, cycle), table),
					String.Format("create operator {0}(const A : Integer) : Integer\r\nbegin\r\n\tresult := 0;\r\n{1}end;", OperatorName(session, cycle), body)
				];
		}

		// The operator's value for argument 1: the sum over index of (1 + index).
		private static int ExpectedOperatorResult => Enumerable.Range(0, OperatorStatementCount).Sum(index => 1 + index);

		private static bool IsStoreLockError(Exception exception)
		{
			Exception inner = exception;
			while (inner.InnerException != null)
				inner = inner.InnerException;
			return inner is SQLiteException sqliteException
				&& (sqliteException.ResultCode == SQLiteErrorCode.Busy || sqliteException.ResultCode == SQLiteErrorCode.Locked);
		}

		[Test]
		public void ConcurrentSessionsStoreTheirOwnScripts()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(ConcurrentDDLTest)))
			{
				instance.Server.Start();

				ConcurrentBag<string> created = new ConcurrentBag<string>();
				ConcurrentBag<string> failures = new ConcurrentBag<string>();
				int lockErrorCount = 0;
				RunConcurrently(instance, created, failures, ref lockErrorCount);

				// NOTE: store-sqlite-concurrency removes the lock errors; it then tightens this to "no exceptions at all"
				// by failing on lockErrorCount too.
				Assert.That(failures, Is.Empty, "Concurrent DDL failed other than on a store lock:\r\n" + String.Join("\r\n", failures));
				TestContext.Progress.WriteLine("{0} of {1} creates succeeded; {2} cycles stopped on a store lock.", created.Count, SessionCount * CycleCount * 3, lockErrorCount);

				// Expected text: each object emitted serially, from one session, after the concurrent phase.
				Dictionary<int, string> expectedScripts = ScriptCreatedObjects(instance, created.ToHashSet());

				instance.Server.Stop();

				Dictionary<int, string> storedScripts = ReadStoredScripts(instance.Server.GetCatalogDirectory(), expectedScripts.Keys);
				foreach (KeyValuePair<int, string> expected in expectedScripts)
				{
					Assert.That(storedScripts.ContainsKey(expected.Key), Is.True, String.Format("No stored script for object {0}.", expected.Key));
					Assert.That(storedScripts[expected.Key], Is.EqualTo(expected.Value), String.Format("Stored script of object {0} differs from its serial emission.", expected.Key));
				}

				// The stored scripts load, which is where silently stored garbage would show in production.
				instance.Server.Start();
				// One operator and one view per session: compiling every operator again would multiply the run time.
				for (int session = 0; session < SessionCount; session++)
				{
					string operatorName = created.Where(name => name.StartsWith(String.Format("K1O_{0}_", session))).Min();
					if (operatorName != null)
						Assert.That(Evaluate(instance, operatorName + "(1)"), Is.EqualTo(ExpectedOperatorResult), operatorName);
					string viewName = created.Where(name => name.StartsWith(String.Format("K1V_{0}_", session))).Min();
					if (viewName != null)
						Assert.That(Evaluate(instance, "Count(" + viewName + ")"), Is.EqualTo(0), viewName);
				}
			}
		}

		private static void RunConcurrently(TestServerInstance instance, ConcurrentBag<string> created, ConcurrentBag<string> failures, ref int lockErrorCount)
		{
			int lockErrors = 0;
			Barrier start = new Barrier(SessionCount);
			List<Thread> threads = new List<Thread>();
			for (int session = 0; session < SessionCount; session++)
			{
				int sessionNumber = session;
				Thread thread =
					new Thread(() =>
					{
						try
						{
							IServerSession serverSession = instance.Connect();
							IServerProcess process = serverSession.StartProcess(new ProcessInfo(serverSession.SessionInfo));
							try
							{
								start.SignalAndWait();
								for (int cycle = 0; cycle < CycleCount; cycle++)
								{
									string[] names = [TableName(sessionNumber, cycle), ViewName(sessionNumber, cycle), OperatorName(sessionNumber, cycle)];
									string[] scripts = CycleScripts(sessionNumber, cycle);
									for (int index = 0; index < scripts.Length; index++)
									{
										try
										{
											process.ExecuteScript(scripts[index]);
											created.Add(names[index]);
										}
										catch (Exception exception) when (IsStoreLockError(exception))
										{
											// The statement rolled back; the rest of the cycle depends on it.
											Interlocked.Increment(ref lockErrors);
											break;
										}
										catch (Exception exception)
										{
											failures.Add(String.Format("{0}: {1}", names[index], exception));
											break;
										}
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
							failures.Add(String.Format("session {0}: {1}", sessionNumber, exception));
						}
					});
				threads.Add(thread);
				thread.Start();
			}

			foreach (Thread thread in threads)
				thread.Join();
			lockErrorCount = lockErrors;
		}

		private static Dictionary<int, string> ScriptCreatedObjects(TestServerInstance instance, HashSet<string> created)
		{
			Dictionary<int, string> scripts = new Dictionary<int, string>();
			int checkedCount = 0;
			IServerSession session = instance.Connect();
			try
			{
				ServerProcess process = (ServerProcess)session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					ServerCatalogDeviceSession deviceSession = (ServerCatalogDeviceSession)process.CatalogDeviceSession;
					Schema.Catalog catalog = process.Catalog;
					lock (catalog)
					{
						for (int index = 0; index < catalog.Count; index++)
						{
							Schema.Object objectValue = catalog[index];
							string name = CreatedName(objectValue);
							if (name != null && created.Contains(name))
							{
								scripts.Add(objectValue.ID, deviceSession.ScriptPersistentObject(objectValue));
								checkedCount++;
							}
						}
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

			Assert.That(checkedCount, Is.EqualTo(created.Count), "Every successful create is in the catalog exactly once.");
			return scripts;
		}

		// The test's name for a catalog object it created, or null: catalog names are library-qualified, and an operator's
		// carries its signature.
		private static string CreatedName(Schema.Object objectValue)
		{
			string name;
			if (objectValue is Schema.Operator operatorValue)
				name = operatorValue.OperatorName;
			else if (objectValue is Schema.BaseTableVar || objectValue is Schema.DerivedTableVar)
				name = objectValue.Name;
			else
				return null;
			int qualifier = name.LastIndexOf('.');
			return qualifier >= 0 ? name.Substring(qualifier + 1) : name;
		}

		private static Dictionary<int, string> ReadStoredScripts(string catalogDirectory, IEnumerable<int> objectIDs)
		{
			Dictionary<int, string> scripts = new Dictionary<int, string>();
			HashSet<int> wanted = objectIDs.ToHashSet();
			// No pooling, so that nothing holds the file open when the instance directory is deleted.
			string connectionString = String.Format("Data Source={0};Read Only=True;Pooling=False", Path.Combine(catalogDirectory, "DAECatalog"));
			using (SQLiteConnection connection = new SQLiteConnection(connectionString))
			{
				connection.Open();
				using (SQLiteCommand command = new SQLiteCommand("select ID, ServerData from DAEObjects", connection))
				using (SQLiteDataReader reader = command.ExecuteReader())
					while (reader.Read())
					{
						int id = Convert.ToInt32(reader.GetValue(0));
						if (wanted.Contains(id))
							scripts.Add(id, reader.IsDBNull(1) ? null : reader.GetString(1));
					}
			}
			return scripts;
		}

		private static object Evaluate(TestServerInstance instance, string expression)
		{
			IServerSession session = instance.Connect();
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					return ((ServerProcess)process).Evaluate(expression, null).AsNative;
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
		}
	}
}
