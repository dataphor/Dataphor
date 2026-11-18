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
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Store.SQLite;
using SQLIndex = Alphora.Dataphor.DAE.Connection.SQLIndex;
using SQLIndexColumn = Alphora.Dataphor.DAE.Connection.SQLIndexColumn;

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
					() => RunAs
					(
						instance,
						"Admin",
						process =>
						{
							process.BeginTransaction(IsolationLevel.CursorStability);
							process.ExecuteScript("SetObjectOwner('NestedOwnedT', 'Admin');");
							process.BeginTransaction(IsolationLevel.CursorStability);
							process.ExecuteScript("SetObjectOwner('NestedOwnedT', 'OwnerUser');");
							process.RollbackTransaction();
							process.CommitTransaction();
						}
					)
				);

				List<string[]> rights = StoredRights(instance, "NestedOwnedT");
				Assert.That(rights, Has.Count.EqualTo(6));
				Assert.That(rights.ConvertAll(right => right[1]), Is.All.EqualTo("Admin"));
				Assert.That(StoredOwner(instance, "NestedOwnedT"), Is.EqualTo("Admin"));
			}
		}

		/// <summary>
		/// A nested transaction's rollback undoes the catalog rows that a create or a drop wrote in it, and the outer transaction's
		/// commit then leaves the stored catalog exactly as it was. Most catalog rows are written by statement rather than through a
		/// store cursor, and those writes were not logged for a nested rollback: the rolled-back table's rows stayed in the store,
		/// and a rolled-back drop lost them, while the server's catalog in memory was restored.
		/// </summary>
		[TestCase(false)]
		[TestCase(true)]
		public void NestedRollbackUndoesStatementWrites(bool drop)
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(NestedRollbackUndoesStatementWrites)))
			{
				instance.Server.Start();
				if (drop)
					instance.ExecuteScript("create table NestedT { ID : Integer, key { ID } };");
				List<string> storedBefore = StoredCatalog(instance);

				RunWithin
				(
					instance,
					() => RunAs
					(
						instance,
						"Admin",
						process =>
						{
							process.BeginTransaction(IsolationLevel.CursorStability);
							process.BeginTransaction(IsolationLevel.CursorStability);
							process.ExecuteScript(drop ? "drop table NestedT;" : "create table NestedT { ID : Integer, key { ID } };");
							process.RollbackTransaction();
							process.CommitTransaction();
						}
					)
				);

				Assert.That(instance.Server.Catalog.Contains("NestedT"), Is.EqualTo(drop));
				Assert.That(StoredCatalog(instance), Is.EqualTo(storedBefore));
			}
		}

		/// <summary>
		/// A rollback of SetObjectOwner, outermost or nested, restores the owner the server holds in memory, of the table, the
		/// scalar type and the operators generated for the type. Operator bodies, views and table handlers are compiled and run
		/// under that owner's rights, and SetObjectOwner lets that owner reassign the object.
		/// </summary>
		[TestCase(false)]
		[TestCase(true)]
		public void RollbackRestoresCachedOwner(bool nested)
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(RollbackRestoresCachedOwner)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('OwnerUser', 'Owner User', ''); create type OwnedType { representation OwnedType { V : Integer } }; create table RolledBackT { ID : Integer, key { ID } };");
				Schema.CatalogObject table = (Schema.CatalogObject)instance.Server.Catalog["RolledBackT"];
				Schema.ScalarType type = (Schema.ScalarType)instance.Server.Catalog["OwnedType"];
				Schema.CatalogObject[] owned = { table, type, type.Representations["OwnedType"].Selector, type.EqualityOperator };
				string[] ownersInTransaction = null;

				RunWithin
				(
					instance,
					() => RunAs
					(
						instance,
						"Admin",
						process =>
						{
							if (nested)
								process.BeginTransaction(IsolationLevel.CursorStability);
							process.BeginTransaction(IsolationLevel.CursorStability);
							process.ExecuteScript("SetObjectOwner('RolledBackT', 'OwnerUser'); SetObjectOwner('OwnedType', 'OwnerUser');");
							ownersInTransaction = Array.ConvertAll(owned, objectValue => objectValue.Owner.ID);
							process.RollbackTransaction();
							if (nested)
								process.CommitTransaction();
						}
					)
				);

				Assert.That(ownersInTransaction, Is.All.EqualTo("OwnerUser"));
				Assert.That(Array.ConvertAll(owned, objectValue => objectValue.Owner.ID), Is.All.EqualTo("Admin"));
			}
		}

		/// <summary>
		/// Once SetObjectOwner gives a table back to Admin, the previous owner is refused the rights it held as owner: its cached
		/// answers are dropped along with the new owner's. Both inserts are the same statement, so the refusal comes from the
		/// plan cached for the first one re-checking the right as it is bound to the second.
		/// </summary>
		[Test]
		public void PreviousOwnerLosesObjectRights()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(PreviousOwnerLosesObjectRights)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('OwnerUser', 'Owner User', ''); create table FormerOwnedT { ID : Integer, key { ID } }; SetObjectOwner('FormerOwnedT', 'OwnerUser');");
				const string insert = "insert row { 1 ID } into FormerOwnedT;";
				RunAs(instance, "OwnerUser", process => process.Execute(insert, null));

				instance.ExecuteScript("SetObjectOwner('FormerOwnedT', 'Admin');");

				Exception exception = Assert.Catch(() => RunAs(instance, "OwnerUser", process => process.Execute(insert, null)));
				Assert.That(FindError(exception, (int)ServerException.Codes.UnauthorizedRight), Is.Not.Null, exception.ToString());
			}
		}

		/// <summary>
		/// A right revoked in a transaction is refused once that transaction commits, even when the user used it in another
		/// session while the transaction was open: that session reads the committed store and caches the answer it finds.
		/// </summary>
		[Test]
		public void RevokeRefusedAfterCommitDespiteCheckDuringTransaction()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(RevokeRefusedAfterCommitDespiteCheckDuringTransaction)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('RightsUser', 'Rights User', ''); create table RevokedT { ID : Integer, key { ID } }; GrantRightToUser('General.RevokedTInsert', 'RightsUser');");

				RunAs
				(
					instance,
					"Admin",
					process =>
					{
						process.BeginTransaction(IsolationLevel.CursorStability);
						process.ExecuteScript("RevokeRightFromUser('General.RevokedTInsert', 'RightsUser');");
						RunAs(instance, "RightsUser", userProcess => userProcess.Execute("insert row { 1 ID } into RevokedT;", null));
						process.CommitTransaction();
					}
				);

				Exception exception = Assert.Catch(() => RunAs(instance, "RightsUser", process => process.Execute("insert row { 2 ID } into RevokedT;", null)));
				Assert.That(FindError(exception, (int)ServerException.Codes.UnauthorizedRight), Is.Not.Null, exception.ToString());
			}
		}

		/// <summary>
		/// A right granted in a transaction that rolls back is not granted afterwards, even when it was checked inside the
		/// transaction, where the changing session's store connection sees the uncommitted grant.
		/// </summary>
		[TestCase(false)]
		[TestCase(true)]
		public void GrantNotKeptAfterRollbackDespiteCheckDuringTransaction(bool nested)
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(GrantNotKeptAfterRollbackDespiteCheckDuringTransaction)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('RightsUser', 'Rights User', ''); create table GrantedT { ID : Integer, key { ID } };");
				object inTransaction = null;
				object afterRollback = null;

				RunAs
				(
					instance,
					"Admin",
					process =>
					{
						if (nested)
							process.BeginTransaction(IsolationLevel.CursorStability);
						process.BeginTransaction(IsolationLevel.CursorStability);
						process.ExecuteScript("GrantRightToUser('General.GrantedTInsert', 'RightsUser');");
						inTransaction = ((ServerProcess)process).Evaluate("UserHasRight('RightsUser', 'General.GrantedTInsert')", null).AsNative;
						process.RollbackTransaction();
						afterRollback = ((ServerProcess)process).Evaluate("UserHasRight('RightsUser', 'General.GrantedTInsert')", null).AsNative;
						if (nested)
							process.CommitTransaction();
					}
				);

				Assert.That(inTransaction, Is.True);
				Assert.That(afterRollback, Is.False);
			}
		}

		/// <summary>
		/// A table created in a transaction is refused to a user without rights on it once that transaction commits, even when the
		/// user's right was checked by name in another session while the transaction was open: the committed store has no row
		/// for the right yet, and a right with no row is granted.
		/// </summary>
		[Test]
		public void CreatedTableRefusedAfterCommitDespiteCheckDuringTransaction()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(CreatedTableRefusedAfterCommitDespiteCheckDuringTransaction)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('RightsUser', 'Rights User', '');");
				object inTransaction = null;

				RunAs
				(
					instance,
					"Admin",
					process =>
					{
						process.BeginTransaction(IsolationLevel.CursorStability);
						process.ExecuteScript("create table CreatedT { ID : Integer, key { ID } };");
						RunAs(instance, "Admin", otherProcess => inTransaction = ((ServerProcess)otherProcess).Evaluate("UserHasRight('RightsUser', 'General.CreatedTInsert')", null).AsNative);
						process.CommitTransaction();
					}
				);

				Assert.That(inTransaction, Is.True);
				Exception exception = Assert.Catch(() => RunAs(instance, "RightsUser", process => process.Execute("insert row { 1 ID } into CreatedT;", null)));
				Assert.That(FindError(exception, (int)ServerException.Codes.UnauthorizedRight), Is.Not.Null, exception.ToString());
			}
		}

		/// <summary>
		/// A role membership added in a transaction that rolls back grants nothing afterwards, even when the member's right was
		/// checked inside the transaction, where the changing session's store connection sees the uncommitted membership.
		/// </summary>
		[Test]
		public void RoleMembershipNotKeptAfterRollbackDespiteCheckDuringTransaction()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(RoleMembershipNotKeptAfterRollbackDespiteCheckDuringTransaction)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('RightsUser', 'Rights User', ''); create table MemberT { ID : Integer, key { ID } }; create role MemberRole; GrantRightToRole('General.MemberTInsert', 'MemberRole');");
				object inTransaction = null;
				object afterRollback = null;

				RunAs
				(
					instance,
					"Admin",
					process =>
					{
						process.BeginTransaction(IsolationLevel.CursorStability);
						process.ExecuteScript("AddUserToRole('RightsUser', 'MemberRole');");
						inTransaction = ((ServerProcess)process).Evaluate("UserHasRight('RightsUser', 'General.MemberTInsert')", null).AsNative;
						process.RollbackTransaction();
						afterRollback = ((ServerProcess)process).Evaluate("UserHasRight('RightsUser', 'General.MemberTInsert')", null).AsNative;
					}
				);

				Assert.That(inTransaction, Is.True);
				Assert.That(afterRollback, Is.False);
			}
		}

		/// <summary>Dropping a role takes the rights it granted from its members, who had used them before the drop.</summary>
		[Test]
		public void DroppedRoleRightRefusedToFormerMember()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(DroppedRoleRightRefusedToFormerMember)))
			{
				instance.Server.Start();
				instance.ExecuteScript("CreateUser('RightsUser', 'Rights User', ''); create table RoleT { ID : Integer, key { ID } }; create role RightsRole; GrantRightToRole('General.RoleTInsert', 'RightsRole'); AddUserToRole('RightsUser', 'RightsRole');");
				RunAs(instance, "RightsUser", process => process.Execute("insert row { 1 ID } into RoleT;", null));

				instance.ExecuteScript("drop role RightsRole;");

				Exception exception = Assert.Catch(() => RunAs(instance, "RightsUser", process => process.Execute("insert row { 2 ID } into RoleT;", null)));
				Assert.That(FindError(exception, (int)ServerException.Codes.UnauthorizedRight), Is.Not.Null, exception.ToString());
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

		/// <summary>
		/// After an update a store cursor on a non-unique index stays on the updated row, and Prior and Next continue from it.
		/// No catalog code calls Prior after an update; this pins the cursor's contract below the catalog.
		/// </summary>
		[Test]
		public void StoreCursorMovesFromUpdatedRow()
		{
			using (SQLStoreConnection connection = ConnectGroupedStore())
			{
				SQLStoreCursor cursor = OpenGroupedCursor(connection);
				try
				{
					cursor.SetRange(new object[] { 1L }, new object[] { 1L });
					Assert.That(cursor.Next() && cursor.Next(), Is.True);
					cursor[2] = "x";
					cursor.Update();
					Assert.That(new object[] { cursor[0], cursor[2] }, Is.EqualTo(new object[] { 2L, "x" }));
					Assert.That(cursor.Prior() ? cursor[0] : null, Is.EqualTo(1L));
					Assert.That(cursor.Next() ? cursor[0] : null, Is.EqualTo(2L));
					Assert.That(cursor.Next() ? cursor[0] : null, Is.EqualTo(3L));
					Assert.That(cursor.Next(), Is.False);
				}
				finally
				{
					cursor.Dispose();
				}
				Assert.That(connection.ExecuteScalar("select group_concat(V, '') from (select V from T order by K)"), Is.EqualTo("axcd"));
			}
		}

		/// <summary>
		/// After a delete a store cursor on a non-unique index is between the deleted row's neighbours, so a loop of Next and
		/// Delete removes exactly the range. No store deletes through a cursor today (none supports updatable cursors).
		/// </summary>
		[Test]
		public void StoreCursorMovesFromDeletedRow()
		{
			using (SQLStoreConnection connection = ConnectGroupedStore())
			{
				SQLStoreCursor cursor = OpenGroupedCursor(connection);
				try
				{
					cursor.SetRange(null, null);
					Assert.That(cursor.Next() && cursor.Next(), Is.True);
					cursor.Delete();
					Assert.That(cursor.Prior() ? cursor[0] : null, Is.EqualTo(1L));
					cursor.Delete();
					Assert.That(cursor.Next() ? cursor[0] : null, Is.EqualTo(3L));

					cursor.SetRange(new object[] { 1L }, new object[] { 1L });
					int deleted = 0;
					for (; cursor.Next(); deleted++)
						cursor.Delete();
					Assert.That(deleted, Is.EqualTo(1));
				}
				finally
				{
					cursor.Dispose();
				}
				Assert.That(connection.ExecuteScalar("select group_concat(K, '') from T"), Is.EqualTo("4"));
			}
		}

		// A SQLite store with table T { K primary key, G, V } holding rows 1, 2 and 3 in group 1 and row 4 in group 2.
		private static SQLStoreConnection ConnectGroupedStore()
		{
			SQLiteStore store = new SQLiteStore();
			store.ConnectionString = "Data Source=" + Path.Combine(TestEnvironment.NewDirectory("StoreCursor"), "Store.sqlite");
			store.Initialize();
			SQLStoreConnection connection = store.Connect();
			connection.ExecuteStatement("create table T (K integer not null primary key, G integer not null, V text)");
			connection.ExecuteStatement("create index IDX_T_G on T (G)");
			connection.ExecuteStatement("insert into T values (1, 1, 'a'), (2, 1, 'b'), (3, 1, 'c'), (4, 2, 'd')");
			return connection;
		}

		// An updatable cursor on T's non-unique index on G, naming rows by K.
		private static SQLStoreCursor OpenGroupedCursor(SQLStoreConnection connection)
		{
			return
				connection.OpenCursor
				(
					"T",
					new List<string> { "K", "G", "V" },
					new SQLIndex("IDX_T_G", new SQLIndexColumn[] { new SQLIndexColumn("G", true) }),
					new List<string> { "K" },
					true
				);
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

		// Runs the action on a process of its own, in a session of the given user (whose password is empty).
		private static void RunAs(TestServerInstance instance, string userID, Action<IServerProcess> action)
		{
			IServerSession session = instance.Server.Connect(new SessionInfo(userID, ""));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					action(process);
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

		// Every row of every table in the catalog store, as "Table: value | value | ...", sorted.
		private static List<string> StoredCatalog(TestServerInstance instance)
		{
			List<string> rows = new List<string>();
			using (SQLiteConnection connection = instance.OpenCatalogStoreFile())
			{
				List<string> tableNames = new List<string>();
				using (SQLiteCommand command = new SQLiteCommand("select name from sqlite_master where type = 'table'", connection))
				using (SQLiteDataReader reader = command.ExecuteReader())
					while (reader.Read())
						tableNames.Add(reader.GetString(0));

				foreach (string tableName in tableNames)
					using (SQLiteCommand command = new SQLiteCommand(String.Format("select * from \"{0}\"", tableName), connection))
					using (SQLiteDataReader reader = command.ExecuteReader())
						while (reader.Read())
						{
							string[] values = new string[reader.FieldCount];
							for (int index = 0; index < values.Length; index++)
								values[index] = reader.IsDBNull(index) ? "null" : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture);
							rows.Add(tableName + ": " + String.Join(" | ", values));
						}
			}
			rows.Sort(StringComparer.Ordinal);
			return rows;
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
