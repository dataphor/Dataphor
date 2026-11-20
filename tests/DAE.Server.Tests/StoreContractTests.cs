/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Store.SQLite;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The behaviour every catalog store must have, run once against each store we build by a subclass of this class.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The server persists its catalog through an <see cref="SQLStore"/>: cursors ranged on an index, writes by statement where
	/// the store has no updatable cursors, and nested transaction levels undone from an operation log because the back ends have
	/// no savepoints. These tests pin that surface below the catalog, so that a change to a store, or a new store, is measured
	/// against one description of it. The tests that drive the store through a running server stay in CatalogStoreTest.
	/// </para>
	/// <para>
	/// Each test gets its own database and creates the schema it needs, so nothing carries between tests.
	/// </para>
	/// </remarks>
	public abstract class StoreContractTests
	{
		/// <summary>Returns an initialized store on the database under <paramref name="directory"/>, created if it is not there.</summary>
		/// <remarks>
		/// <see cref="SeedSchemaReopens"/> calls this a second time with the same directory to reopen the database a store has
		/// already written, so an implementation must open an existing database rather than replace it.
		/// </remarks>
		protected abstract SQLStore CreateStore(string directory);

		/// <summary>Releases what <see cref="CreateStore"/> made: a file to delete, a database to drop, or nothing.</summary>
		protected abstract void DisposeStore(SQLStore store, string directory);

		private string _directory;
		private SQLStore _store;

		/// <summary>This test's store.</summary>
		protected SQLStore Store { get { return _store; } }

		[SetUp]
		public void SetUpStore()
		{
			_directory = TestEnvironment.NewDirectory("StoreContract." + TestContext.CurrentContext.Test.Name);
			_store = CreateStore(_directory);
		}

		[TearDown]
		public void TearDownStore()
		{
			SQLStore store = _store;
			_store = null;
			if (store != null)
				DisposeStore(store, _directory);
		}

		/// <summary>Opens this test's database again, as a server restarting on an existing instance does, and makes that the test's store.</summary>
		protected SQLStore ReopenStore()
		{
			_store = CreateStore(_directory);
			return _store;
		}

		#region Schema and reads the tests share

		private const string TableName = "T";

		/// <summary>The name every store assembly embeds the catalog's seed script under; CatalogStore.Initialize reads it by this name.</summary>
		private const string SeedResourceName = "Alphora.Dataphor.DAE.SystemStoreCatalog.sql";

		// T { K primary key, G, V }, rows 1, 2 and 3 in group 1 and row 4 in group 2: the shape every catalog store cursor is
		// opened in, ordered on a non-unique index with rows named by the primary key.
		// The key is declared int, not integer, so that the value a cursor reads back has the same .NET type as the range values
		// the tests pass: System.Data.SQLite maps a column declared "integer" to Int64 and one declared "int" to Int32, the
		// catalog's own tables are int, and SQLStoreCursor compares a read key value with a range value by IComparable, which
		// throws when the two are different integer types.
		private static void CreateGroupedTable(SQLStoreConnection connection)
		{
			connection.ExecuteStatement("create table T (K int not null primary key, G int not null, V nvarchar(20))");
			connection.ExecuteStatement("create index IDX_T_G on T (G)");
			connection.ExecuteStatement("insert into T values (1, 1, 'a'), (2, 1, 'b'), (3, 1, 'c'), (4, 2, 'd')");
		}

		// An updatable cursor on T's non-unique index on G, naming rows by K.
		private static SQLStoreCursor OpenGroupedCursor(SQLStoreConnection connection)
		{
			return
				connection.OpenCursor
				(
					TableName,
					new List<string> { "K", "G", "V" },
					new SQLIndex("IDX_T_G", new SQLIndexColumn[] { new SQLIndexColumn("G", true) }),
					new List<string> { "K" },
					true
				);
		}

		// Runs the store's own seed script, as CatalogStore.Initialize does on a database with no DAEServerInfo table.
		private void SeedCatalogSchema(SQLStoreConnection connection)
		{
			using (Stream stream = Store.GetType().Assembly.GetManifestResourceStream(SeedResourceName))
			{
				Assert.That(stream, Is.Not.Null, String.Format("The store assembly {0} embeds no resource \"{1}\".", Store.GetType().Assembly.GetName().Name, SeedResourceName));
				connection.ExecuteScript(new StreamReader(stream).ReadToEnd());
			}
		}

		// The V values of the rows named by the given keys, comma separated, with "null" where there is no such row: one scalar
		// select per key, so that the assertion does not depend on a back end's string aggregate.
		private static string Values(SQLStoreConnection connection, params int[] keys)
		{
			string[] values = new string[keys.Length];
			for (int index = 0; index < keys.Length; index++)
			{
				object value = connection.ExecuteScalar(String.Format("select V from T where K = {0}", keys[index]));
				values[index] = value == null || value is DBNull ? "null" : Convert.ToString(value);
			}
			return String.Join(",", values);
		}

		// A count read by statement; the back ends disagree on the integer type a count comes back as.
		private static long Count(SQLStoreConnection connection, string statement)
		{
			return Convert.ToInt64(connection.ExecuteScalar(statement));
		}

		// A CatalogStore over the same database as Store, for the catalog's own write-by-statement paths (InsertRow, DeleteRows).
		// NOTE: built with CreateStore() alone, because CatalogStore.Initialize needs a started engine. That leaves the inner
		// store's SupportsMARS and SupportsUpdatableCursor at false, so InsertRow and DeleteRows take their statement path here.
		// Every store we build reports SupportsUpdatableCursor false, so that is the path a server takes too; a store that
		// reported updatable cursors would take the cursor path under a server and the statement path here, and the two nested
		// rollback tests below would then have to drive it through a server instead.
		private CatalogStore ConnectCatalogStore()
		{
			CatalogStore catalogStore = new CatalogStore();
			catalogStore.StoreClassName = Store.GetType().AssemblyQualifiedName;
			catalogStore.StoreConnectionString = Store.ConnectionString;
			catalogStore.CreateStore();
			return catalogStore;
		}

		#endregion

		/// <summary>
		/// A new database seeds itself from the store's SystemStoreCatalog script, and a store opened on that database again finds
		/// the catalog the first one wrote: what a server restarting on an existing instance depends on.
		/// </summary>
		[Test]
		public void SeedSchemaReopens()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				Assert.That(connection.HasTable("DAEServerInfo"), Is.False, "A store on a new database already has the catalog's tables.");
				SeedCatalogSchema(connection);
				connection.ExecuteStatement("insert into DAEServerInfo (Name, Version, MaxConcurrentProcesses, ProcessWaitTimeout, ProcessTerminationTimeout, PlanCacheSize) values ('StoreContract', '1.0.0.0', 10, 1000, 1000, 100)");
			}

			using (SQLStoreConnection connection = ReopenStore().Connect())
			{
				Assert.That(connection.HasTable("DAEServerInfo"), Is.True, "The reopened store does not have the catalog's tables.");
				Assert.That(connection.ExecuteScalar("select Name from DAEServerInfo"), Is.EqualTo("StoreContract"));
			}
		}

		/// <summary>
		/// A row updated through a cursor is read back through that same cursor once it is re-ranged: the cursor serves the row
		/// it wrote, not what its reader held before the write.
		/// </summary>
		[Test]
		public void RowReadThroughSameCursorAfterUpdate()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				connection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				try
				{
					using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
					{
						cursor.SetRange(null, null);
						Assert.That(cursor.Next(), Is.True);
						Assert.That(cursor[2], Is.EqualTo("a"));

						cursor[2] = "x";
						cursor.Update();

						cursor.SetRange(null, null);
						Assert.That(cursor.Next(), Is.True);
						Assert.That(cursor[2], Is.EqualTo("x"));
					}
					connection.CommitTransaction();
				}
				catch
				{
					connection.RollbackTransaction();
					throw;
				}
			}
		}

		/// <summary>A row inserted through one cursor is read by another cursor on the same index and connection, inside one transaction.</summary>
		[Test]
		public void RowInsertedThroughOneCursorReadByAnother()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				connection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				try
				{
					using (SQLStoreCursor inserting = OpenGroupedCursor(connection))
					using (SQLStoreCursor reading = OpenGroupedCursor(connection))
					{
						inserting.Insert(new object[] { 5, 2, "e" });

						reading.SetRange(new object[] { 2 }, new object[] { 2 });
						Assert.That(reading.Next() ? reading[0] : null, Is.EqualTo(4));
						Assert.That(reading.Next() ? reading[0] : null, Is.EqualTo(5));
						Assert.That(reading.Next(), Is.False);
					}
					connection.CommitTransaction();
				}
				catch
				{
					connection.RollbackTransaction();
					throw;
				}
			}
		}

		/// <summary>
		/// Cursors opened one after another on the same table and connection, inside one transaction, each see what the ones
		/// before them wrote: the catalog opens a cursor per write and reads back through later ones.
		/// </summary>
		[Test]
		public void CursorsInSequenceWithinOneTransaction()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				connection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				try
				{
					using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
						cursor.Insert(new object[] { 5, 3, "e" });
					using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
						cursor.Insert(new object[] { 6, 3, "f" });

					using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
					{
						cursor.SetRange(new object[] { 3 }, new object[] { 3 });
						Assert.That(cursor.Next() ? cursor[0] : null, Is.EqualTo(5));
					}

					using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
					{
						cursor.SetRange(null, null);
						List<object> keys = new List<object>();
						while (cursor.Next())
							keys.Add(cursor[0]);
						Assert.That(keys, Is.EqualTo(new object[] { 1, 2, 3, 4, 5, 6 }));
					}

					connection.CommitTransaction();
				}
				catch
				{
					connection.RollbackTransaction();
					throw;
				}
			}
		}

		/// <summary>
		/// A second cursor opened, read and closed while a first one is part way through its range does not move the first one:
		/// the catalog reads one table through several cursors at once.
		/// </summary>
		[Test]
		public void TwoCursorsOpenOnOneTableInOneTransaction()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				connection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				try
				{
					using (SQLStoreCursor iterating = OpenGroupedCursor(connection))
					{
						iterating.SetRange(null, null);
						Assert.That(iterating.Next() ? iterating[0] : null, Is.EqualTo(1));
						Assert.That(iterating.Next() ? iterating[0] : null, Is.EqualTo(2));

						using (SQLStoreCursor reading = OpenGroupedCursor(connection))
						{
							reading.SetRange(new object[] { 2 }, new object[] { 2 });
							Assert.That(reading.Next() ? reading[0] : null, Is.EqualTo(4));
						}

						Assert.That(iterating.Next() ? iterating[0] : null, Is.EqualTo(3));
						Assert.That(iterating.Next() ? iterating[0] : null, Is.EqualTo(4));
						Assert.That(iterating.Next(), Is.False);
					}
					connection.CommitTransaction();
				}
				catch
				{
					connection.RollbackTransaction();
					throw;
				}
			}
		}

		/// <summary>
		/// After an update a cursor on a non-unique index stays on the updated row, and Prior and Next continue from it. No
		/// catalog code calls Prior after an update; this pins the cursor's contract below the catalog.
		/// </summary>
		[Test]
		public void CursorMovesFromUpdatedRow()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
				{
					cursor.SetRange(new object[] { 1 }, new object[] { 1 });
					Assert.That(cursor.Next() && cursor.Next(), Is.True);
					cursor[2] = "x";
					cursor.Update();
					Assert.That(new object[] { cursor[0], cursor[2] }, Is.EqualTo(new object[] { 2, "x" }));
					Assert.That(cursor.Prior() ? cursor[0] : null, Is.EqualTo(1));
					Assert.That(cursor.Next() ? cursor[0] : null, Is.EqualTo(2));
					Assert.That(cursor.Next() ? cursor[0] : null, Is.EqualTo(3));
					Assert.That(cursor.Next(), Is.False);
				}
				Assert.That(Values(connection, 1, 2, 3, 4), Is.EqualTo("a,x,c,d"));
			}
		}

		/// <summary>
		/// After a delete a cursor on a non-unique index is between the deleted row's neighbours, so a loop of Next and Delete
		/// removes exactly the range.
		/// </summary>
		// NOTE: no store we build supports updatable cursors, so Delete here goes through the statement path. A store that did
		// support them would exercise the other path with the same assertions; there is nothing to skip.
		[Test]
		public void CursorMovesFromDeletedRow()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
				{
					cursor.SetRange(null, null);
					Assert.That(cursor.Next() && cursor.Next(), Is.True);
					cursor.Delete();
					Assert.That(cursor.Prior() ? cursor[0] : null, Is.EqualTo(1));
					cursor.Delete();
					Assert.That(cursor.Next() ? cursor[0] : null, Is.EqualTo(3));

					cursor.SetRange(new object[] { 1 }, new object[] { 1 });
					int deleted = 0;
					for (; cursor.Next(); deleted++)
						cursor.Delete();
					Assert.That(deleted, Is.EqualTo(1));
				}
				Assert.That(Count(connection, "select count(*) from T"), Is.EqualTo(1));
				Assert.That(Values(connection, 4), Is.EqualTo("d"));
			}
		}

		/// <summary>
		/// A nested level's rollback undoes a row the catalog inserted by statement in it, and the outer commit then leaves that
		/// row out of the store. Reproduction, below the server, of bug-store-nested-rollback-keeps-rows: a nested level is no
		/// database transaction, so its rollback runs the inverse of each write logged in it, and statement writes logged none.
		/// </summary>
		[Test]
		public void NestedRollbackUndoesStatementInsert()
		{
			using (SQLStoreConnection connection = Store.Connect())
				SeedCatalogSchema(connection);

			using (CatalogStore catalogStore = ConnectCatalogStore())
			{
				CatalogStoreConnection connection = catalogStore.Connect();
				try
				{
					connection.BeginTransaction(IsolationLevel.CursorStability);
					connection.BeginTransaction(IsolationLevel.CursorStability);
					connection.InsertRow("DAEUsers", "ContractUser", "Contract User", "");
					connection.RollbackTransaction();
					connection.CommitTransaction();
				}
				finally
				{
					connection.Dispose();
				}
			}

			using (SQLStoreConnection connection = Store.Connect())
				Assert.That(Count(connection, "select count(*) from DAEUsers where ID = 'ContractUser'"), Is.EqualTo(0));
		}

		/// <summary>
		/// A nested level's rollback puts back a row the catalog deleted by statement in it, with the values it had. The delete
		/// arm of bug-store-nested-rollback-keeps-rows: the inverse of a delete is an insert of the whole row, so a nested level
		/// must read the row before deleting it.
		/// </summary>
		[Test]
		public void NestedRollbackUndoesStatementDelete()
		{
			using (SQLStoreConnection connection = Store.Connect())
				SeedCatalogSchema(connection);

			using (CatalogStore catalogStore = ConnectCatalogStore())
			{
				CatalogStoreConnection connection = catalogStore.Connect();
				try
				{
					connection.BeginTransaction(IsolationLevel.CursorStability);
					connection.InsertRow("DAEUsers", "ContractUser", "Contract User", "");
					connection.BeginTransaction(IsolationLevel.CursorStability);
					connection.DeleteRows("PK_DAEUsers", "ContractUser");
					connection.RollbackTransaction();
					connection.CommitTransaction();
				}
				finally
				{
					connection.Dispose();
				}
			}

			using (SQLStoreConnection connection = Store.Connect())
				Assert.That(connection.ExecuteScalar("select Name from DAEUsers where ID = 'ContractUser'"), Is.EqualTo("Contract User"));
		}

		/// <summary>
		/// A nested level's rollback of a cursor update restores that row alone. The row-identity half of
		/// bug-set-object-owner-rollback-keeps-owner: the undo names the row by the primary key it was opened with, not by the
		/// columns of the non-unique index it is ordered on, which every row of the group shares.
		/// </summary>
		[Test]
		public void NestedRollbackReinsertsCursorUpdates()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				connection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				connection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
				{
					cursor.SetRange(new object[] { 1 }, new object[] { 1 });
					Assert.That(cursor.Next() && cursor.Next(), Is.True);
					cursor[2] = "x";
					cursor.Update();
				}
				connection.RollbackTransaction();
				connection.CommitTransaction();

				Assert.That(Values(connection, 1, 2, 3, 4), Is.EqualTo("a,b,c,d"));
			}
		}

		/// <summary>
		/// The outermost level's rollback undoes every write in the transaction, a committed nested level's included, and leaves
		/// nothing for a later connection to see: the undo log is cleared at the outermost end and the database's own rollback
		/// does the work. A store whose outermost begin is not a real database transaction fails here.
		/// </summary>
		[Test]
		public void OuterRollbackDoesNotReplayUndoLog()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				connection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
					cursor.Insert(new object[] { 5, 3, "e" });
				connection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
					cursor.Insert(new object[] { 6, 3, "f" });
				connection.CommitTransaction();
				connection.RollbackTransaction();
			}

			using (SQLStoreConnection connection = Store.Connect())
			{
				Assert.That(Count(connection, "select count(*) from T"), Is.EqualTo(4));
				Assert.That(Values(connection, 5, 6), Is.EqualTo("null,null"));
			}
		}
	}

	/// <summary>The contract against the SQLite catalog store, the default and today the only one we build.</summary>
	[TestFixture]
	public class SQLiteStoreContractTests : StoreContractTests
	{
		protected override SQLStore CreateStore(string directory)
		{
			SQLiteStore store = new SQLiteStore();
			store.ConnectionString = "Data Source=" + Path.Combine(directory, "Store.sqlite");
			store.Initialize();
			return store;
		}

		// The whole directory, because write-ahead logging leaves -wal and -shm files beside the database file.
		protected override void DisposeStore(SQLStore store, string directory)
		{
			if (!TestEnvironment.KeepFiles)
				Directory.Delete(directory, true);
		}
	}

#if HAVE_MSSQL_STORE
	/// <summary>The contract against the SQL Server catalog store, on a database the fixture creates and drops.</summary>
	/// <remarks>
	/// <para>
	/// Not compiled and never run as written. DAE.MSSQL.csproj is still the legacy VS2003 project and is not in Dataphor.slnx,
	/// so this test project cannot reference the SQL Server store. The plan ticket store-mssql-port decides whether that store
	/// is kept at all; if it is, it converts the project, adds it to the solution, adds the project reference to
	/// DAE.Server.Tests.csproj and defines HAVE_MSSQL_STORE. This class is the shape it switches on, not verified code.
	/// </para>
	/// <para>
	/// One database per fixture, cleared by CreateStore between tests: a CREATE DATABASE and DROP DATABASE per test would
	/// dominate the run.
	/// </para>
	/// </remarks>
	[TestFixture]
	[Category("mssql")]
	public class MSSQLStoreContractTests : StoreContractTests
	{
		/// <summary>The environment variable holding a connection string to a SQL Server the fixture may create a database on.</summary>
		public const string ConnectionVariable = "DAE_MSSQL_CONNECTION";

		/// <summary>The database the fixture creates. Named after the fixture so that a run which crashed leaves one to drop, not many.</summary>
		private const string DatabaseName = "DataphorStoreContract";

		private string _serverConnectionString;

		[OneTimeSetUp]
		public void CreateDatabase()
		{
			_serverConnectionString = Environment.GetEnvironmentVariable(ConnectionVariable);

			// Ignored rather than failed: a developer without a SQL Server still runs the rest of the suite. NUnit reports the
			// case as skipped with this reason, which says what to set to run it.
			if (String.IsNullOrEmpty(_serverConnectionString))
				Assert.Ignore(String.Format("Set {0} to a SQL Server connection string to run the store contract against the SQL Server store.", ConnectionVariable));

			DropDatabaseIfPresent();
			ExecuteOnServer(String.Format("create database {0}", DatabaseName));
		}

		[OneTimeTearDown]
		public void DropDatabase()
		{
			if (!String.IsNullOrEmpty(_serverConnectionString))
				DropDatabaseIfPresent();
		}

		protected override SQLStore CreateStore(string directory)
		{
			Alphora.Dataphor.DAE.Store.MSSQL.MSSQLStore store = new Alphora.Dataphor.DAE.Store.MSSQL.MSSQLStore();
			store.ShouldEnsureDatabase = false; // OneTimeSetUp created it
			store.ConnectionString = ConnectionStringFor(DatabaseName);
			store.Initialize();
			ClearDatabase(store);
			return store;
		}

		// Nothing per test: the fixture's database is dropped in OneTimeTearDown and emptied by CreateStore.
		protected override void DisposeStore(SQLStore store, string directory)
		{
		}

		// Drops every table the tests created, so that the next test starts on an empty database.
		private static void ClearDatabase(SQLStore store)
		{
			using (SQLStoreConnection connection = store.Connect())
			{
				List<string> tableNames = new List<string>();
				SQLConnection sqlConnection = store.GetSQLConnection();
				try
				{
					using (SQLCommand command = sqlConnection.CreateCommand(false))
					{
						command.CommandType = SQLCommandType.Statement;
						command.Statement = "select name from sys.tables";
						using (SQLCursor cursor = command.Open(SQLCursorType.Dynamic, SQLIsolationLevel.ReadCommitted))
							while (cursor.Next())
								tableNames.Add((string)cursor[0]);
					}
				}
				finally
				{
					sqlConnection.Dispose();
				}

				// Foreign keys first would matter on a schema with references; the contract's tables have none.
				foreach (string tableName in tableNames)
					connection.ExecuteStatement(String.Format("drop table [{0}]", tableName));
			}
		}

		private void DropDatabaseIfPresent()
		{
			// Single user first: another connection, this run's or a pooled one from an earlier run, blocks the drop.
			ExecuteOnServer
			(
				String.Format
				(
					"if db_id('{0}') is not null begin alter database [{0}] set single_user with rollback immediate; drop database [{0}]; end",
					DatabaseName
				)
			);
		}

		// Runs one statement on the configured server, connected to master rather than to the fixture's database.
		private void ExecuteOnServer(string statement)
		{
			Alphora.Dataphor.DAE.Store.MSSQL.MSSQLStore store = new Alphora.Dataphor.DAE.Store.MSSQL.MSSQLStore();
			store.ShouldEnsureDatabase = false;
			store.ConnectionString = ConnectionStringFor("master");
			store.Initialize();

			SQLConnection connection = store.GetSQLConnection();
			try
			{
				using (SQLCommand command = connection.CreateCommand(false))
				{
					command.CommandType = SQLCommandType.Statement;
					command.Statement = statement;
					command.Execute();
				}
			}
			finally
			{
				connection.Dispose();
			}
		}

		// The configured connection string with its database replaced; the variable names the server, not a database.
		private string ConnectionStringFor(string databaseName)
		{
			DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
			builder.ConnectionString = _serverConnectionString;
			builder.Remove("Database");
			builder["Initial Catalog"] = databaseName;
			return builder.ConnectionString;
		}
	}
#endif
}
