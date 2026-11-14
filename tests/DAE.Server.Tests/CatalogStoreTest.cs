/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Device.Catalog;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	[TestFixture]
	public class CatalogStoreTest
	{
		[Test]
		public void SQLiteCatalogRegressionTest()
		{
			// A server on a fresh instance, with a SQLite catalog store
			using (TestServerInstance LInstance = new TestServerInstance("CatalogStoreTest"))
			{
				Server LServer = LInstance.Server;
				LServer.Start();
				LServer.Stop();

				// The second start reopens the catalog the first one created
				LServer.Start();
				LServer.Stop();
			}
		}

		[Test]
		public void DefaultStoreIsSQLite()
		{
			using (TestServerInstance instance = new TestServerInstance("DefaultStoreIsSQLite"))
			{
				Server server = instance.Server;
				server.CatalogStoreClassName = null;
				server.CatalogStoreConnectionString = null;
				server.Start();

				Assert.That(instance.Evaluate("1 + 1"), Is.EqualTo(2));
				Assert.That(File.Exists(CatalogFileName(instance)), Is.True);
			}
		}

		[TestCase("Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore,Alphora.Dataphor.DAE.SQLCE", null)]
		[TestCase("alphora.dataphor.dae.store.sqlce.sqlcestore, Alphora.Dataphor.DAE.SQLCE", null)]
		[TestCase(null, @"Data Source=%CatalogPath%\DAECatalog.sdf;Password=;Mode=Read Write")]
		public void SQLCESettingsAreRejected(string className, string connectionString)
		{
			using (TestServerInstance instance = new TestServerInstance("SQLCESettingsAreRejected"))
			{
				Server server = instance.Server;
				server.CatalogStoreClassName = className;
				server.CatalogStoreConnectionString = connectionString;

				Exception exception = Assert.Catch(() => server.Start());
				Assert.That(FindCatalogErrorCode(exception), Is.EqualTo((int)CatalogException.Codes.SQLCEStoreNotSupported), exception.ToString());
				Assert.That(server.State, Is.EqualTo(ServerState.Stopped));
				Assert.That(File.Exists(CatalogFileName(instance)), Is.False);

				// Once the settings are removed, the same instance starts on the default store
				server.CatalogStoreClassName = null;
				server.CatalogStoreConnectionString = null;
				server.Start();
				Assert.That(File.Exists(CatalogFileName(instance)), Is.True);
			}
		}

		[Test]
		public void UnloadableStoreClassIsReported()
		{
			using (TestServerInstance instance = new TestServerInstance("UnloadableStoreClassIsReported"))
			{
				Server server = instance.Server;
				server.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.Missing.MissingStore,Alphora.Dataphor.DAE.Missing";

				Exception exception = Assert.Catch(() => server.Start());
				Assert.That(FindCatalogErrorCode(exception), Is.EqualTo((int)CatalogException.Codes.CatalogStoreClassNotFound), exception.ToString());
				Assert.That(server.State, Is.EqualTo(ServerState.Stopped));
			}
		}

		private static readonly TimeSpan ScriptLimit = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan AbandonWait = TimeSpan.FromSeconds(10);

		/// <summary>
		/// SetObjectOwner changes the owner of each of a table's six rights through a store cursor on an index the rights share
		/// (their catalog object). When the cursor named the row by that index's columns, the first update wrote one right's
		/// name into all six and failed with "UNIQUE constraint failed: DAERights.Name"; with the row named but the cursor
		/// reopened at the start of its range after each update, it updated the first right without end.
		/// </summary>
		[Test]
		public void SetObjectOwnerChangesEveryRight()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(SetObjectOwnerChangesEveryRight)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('OwnerUser', 'Owner User', ''); create table OwnedT { ID : Integer, key { ID } };");
				List<string> rightNames = StoredRights(instance, "OwnedT").ConvertAll(right => right[0]);
				Assert.That(rightNames, Has.Count.EqualTo(6));

				RunWithin(instance, () => instance.ExecuteScript("SetObjectOwner('OwnedT', 'OwnerUser');"));

				List<string[]> rights = StoredRights(instance, "OwnedT");
				Assert.That(rights.ConvertAll(right => right[0]), Is.EqualTo(rightNames));
				Assert.That(rights.ConvertAll(right => right[1]), Is.All.EqualTo("OwnerUser"));
				Assert.That(StoredOwner(instance, "OwnedT"), Is.EqualTo("OwnerUser"));
			}
		}

		/// <summary>
		/// A nested transaction's rollback restores the owner of each right that SetObjectOwner changed in it: the undo log names
		/// each row by its primary key, not by the columns of the index the cursor was opened on.
		/// </summary>
		[Test]
		public void NestedRollbackRestoresEveryRightsOwner()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(NestedRollbackRestoresEveryRightsOwner)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('OwnerUser', 'Owner User', ''); create table NestedOwnedT { ID : Integer, key { ID } };");

				// Through the process: with implicit transactions on, each statement of a script ends the transactions it began.
				RunWithin
				(
					instance,
					() =>
					{
						IServerSession session = instance.Connect();
						try
						{
							IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
							try
							{
								process.BeginTransaction(IsolationLevel.CursorStability);
								process.ExecuteScript("SetObjectOwner('NestedOwnedT', 'Admin');");
								process.BeginTransaction(IsolationLevel.CursorStability);
								process.ExecuteScript("SetObjectOwner('NestedOwnedT', 'OwnerUser');");
								process.RollbackTransaction();
								process.CommitTransaction();
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
				);

				List<string[]> rights = StoredRights(instance, "NestedOwnedT");
				Assert.That(rights, Has.Count.EqualTo(6));
				Assert.That(rights.ConvertAll(right => right[1]), Is.All.EqualTo("Admin"));
				Assert.That(StoredOwner(instance, "NestedOwnedT"), Is.EqualTo("Admin"));
			}
		}

		/// <summary>SetObjectOwner on a name that resolves to no catalog object reports CatalogObjectExpected, not a null reference.</summary>
		[Test]
		public void SetObjectOwnerOfUnknownObjectReportsCatalogObjectExpected()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(SetObjectOwnerOfUnknownObjectReportsCatalogObjectExpected)))
			{
				instance.Server.Start();

				Exception exception = Assert.Catch(() => instance.ExecuteScript("SetObjectOwner('NoSuchObject', 'Admin');"));
				Assert.That(FindError(exception, (int)Schema.SchemaException.Codes.CatalogObjectExpected), Is.Not.Null, exception.ToString());
			}
		}

		// Runs on a task, so that a store cursor that never leaves its range fails the test instead of hanging the run.
		private static void RunWithin(TestServerInstance instance, Action action)
		{
			Task task = Task.Run(action);
			if (Task.WhenAny(task, Task.Delay(ScriptLimit)).Result != task)
			{
				instance.Abandon(AbandonWait);
				Assert.Fail(String.Format("SetObjectOwner did not return within {0} s.", ScriptLimit.TotalSeconds));
			}
			task.GetAwaiter().GetResult();
		}

		// The stored rights of the named catalog object as { Name, Owner_User_ID }, ordered by name.
		private static List<string[]> StoredRights(TestServerInstance instance, string objectName)
		{
			List<string[]> rights = new List<string[]>();
			using (SQLiteConnection connection = instance.OpenCatalogStoreFile())
			using
			(
				SQLiteCommand command =
					new SQLiteCommand
					(
						"select R.Name, R.Owner_User_ID from DAERights R join DAECatalogObjects C on C.ID = R.Catalog_Object_ID where C.Name = @Name or C.Name like '%.' || @Name order by R.Name",
						connection
					)
			)
			{
				command.Parameters.AddWithValue("@Name", objectName);
				using (SQLiteDataReader reader = command.ExecuteReader())
					while (reader.Read())
						rights.Add(new string[] { reader.GetString(0), reader.GetString(1) });
			}
			return rights;
		}

		private static string StoredOwner(TestServerInstance instance, string objectName)
		{
			using (SQLiteConnection connection = instance.OpenCatalogStoreFile())
			using (SQLiteCommand command = new SQLiteCommand("select Owner_User_ID from DAECatalogObjects where Name = @Name or Name like '%.' || @Name", connection))
			{
				command.Parameters.AddWithValue("@Name", objectName);
				return (string)command.ExecuteScalar();
			}
		}

		private static DataphorException FindError(Exception exception, int code)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is DataphorException dataphorException && dataphorException.Code == code)
					return dataphorException;
			return null;
		}

		private static string CatalogFileName(TestServerInstance instance)
		{
			return Path.Combine(instance.Directory, TestServerInstance.InstanceFolderName, Server.DefaultCatalogDirectory, "DAECatalog.sqlite");
		}

		private static int? FindCatalogErrorCode(Exception exception)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is CatalogException)
					return ((CatalogException)exception).Code;
			return null;
		}
	}
}
