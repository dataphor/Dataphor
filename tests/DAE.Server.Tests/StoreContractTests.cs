/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Store.MSSQL;
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
	/// Each test creates the schema it needs on an empty database. A store whose database is a file gets a fresh one per
	/// test, in that test's own directory; a store whose database lives on a server may share one, and then it is the store's
	/// fixture that empties it (see <see cref="CreateStore"/>).
	/// </para>
	/// </remarks>
	public abstract class StoreContractTests
	{
		/// <summary>Returns an initialized store on an empty database for the test <paramref name="directory"/> belongs to.</summary>
		/// <remarks>
		/// <para>
		/// The directory is this test's own, newly made and empty, and is where a store that keeps its database in a file puts
		/// it. A store that shares one database across the fixture empties it here, at the start of a test, rather than at the
		/// end of the one before: the test directories are what the base deletes, and emptying at the start is also what leaves
		/// the last test's rows in place for a run kept with DATAPHOR_TEST_LOGGING=1.
		/// </para>
		/// <para>
		/// <see cref="SeedSchemaReopens"/> calls this a second time with the same directory to reopen the database a store has
		/// already written, so an implementation must open an existing database rather than replace or empty it; the directory,
		/// fresh per test, is what tells a new test from a reopen.
		/// </para>
		/// </remarks>
		protected abstract SQLStore CreateStore(string directory);

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
			string directory = _directory;
			_store = null;
			_directory = null;

			// The whole directory, because SQLite's write-ahead logging leaves -wal and -shm files beside the database file.
			// SQLStore holds no disposable state of its own, so this is everything a store leaves inside its directory.
			if ((directory != null) && !TestEnvironment.KeepFiles)
				Directory.Delete(directory, true);
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

		// An updatable cursor on T's non-unique index on G, naming rows by V: the shape of a catalog table whose primary key is
		// text (DAEUsers.ID, DAELoadedLibraries.Library_Name), so that a generated write's key condition carries a string.
		private static SQLStoreCursor OpenCursorKeyedOnValue(SQLStoreConnection connection)
		{
			return
				connection.OpenCursor
				(
					TableName,
					new List<string> { "K", "G", "V" },
					new SQLIndex("IDX_T_G", new SQLIndexColumn[] { new SQLIndexColumn("G", true) }),
					new List<string> { "V" },
					true
				);
		}
		
		// B { K primary key, F }, where F holds a boolean: the shape of every catalog table that stores one (DAEObjects.IsSystem,
		// DAERoleRightAssignments.IsGranted). F is declared tinyint, as the catalog declares them, and the key is a named primary
		// key constraint, which is where the catalog's index names come from.
		private static void CreateBooleanTable(SQLStoreConnection connection)
		{
			connection.ExecuteStatement("create table B (K int not null, F tinyint not null, constraint PK_B primary key (K))");
		}

		private static SQLStoreCursor OpenBooleanCursor(SQLStoreConnection connection)
		{
			return
				connection.OpenCursor
				(
					"B",
					new List<string> { "K", "F" },
					new SQLIndex("PK_B", new SQLIndexColumn[] { new SQLIndexColumn("K", true) }),
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
		// Built with CreateStore() alone, because CatalogStore.Initialize needs a started engine. The inner store's
		// InternalInitialize therefore never runs, which leaves three differences from a server's store, all checked harmless:
		// SupportsUpdatableCursor false sends InsertRow and DeleteRows down their statement path, which is the path a server
		// takes too since every store we build reports false anyway; SupportsMARS false where an initialized SQLiteStore sets it
		// true, which gates only the cursor cache that the statement path never opens a cursor through; and an un-normalized
		// connection string (no forced WAL journal, no BusyTimeout), on a file the fixture's own initialized Store has already
		// put into WAL, in single-threaded tests where nothing waits on a write lock.
		// NOTE: a store that reported updatable cursors would take the cursor path under a server and the statement path here,
		// so the two nested rollback tests below would then have to drive it through a server to test what a server does.
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
		/// An update that moves the cursor's row out of the range it is scanning ends the scan. The cursor seeks from the row's
		/// new key, which is past the range's end, so the row it finds there does not become current.
		/// </summary>
		[Test]
		public void CursorEndsScanWhenUpdateMovesRowOutOfRange()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);
				// A row in a later group, so that the seek from the moved row has something to find and the range check is what
				// ends the scan rather than the table running out.
				connection.ExecuteStatement("insert into T values (5, 3, 'e')");

				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
				{
					// Group 2 holds row 4 alone, so the range admits the current row and nothing else.
					cursor.SetRange(new object[] { 2 }, new object[] { 2 });
					Assert.That(cursor.Next() ? cursor[0] : null, Is.EqualTo(4));

					cursor[1] = 3;
					cursor.Update();

					Assert.That(cursor.Next(), Is.False, "the cursor served a row from beyond its range after its own row moved out of it");
				}

				Assert.That(Count(connection, "select count(*) from T where G = 3"), Is.EqualTo(2), "the update did not move the row");
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
		/// The sequence both string round-trip cases run: insert a row carrying <paramref name="inserted"/>, read it back,
		/// update it to <paramref name="updated"/>, read that back, and delete the row through a cursor that names rows by their
		/// string value rather than by the integer key - so that a generated insert, a generated update's set list and a generated
		/// key condition each carry the string. What makes a case is the pair of strings; what each pair is for is on the test
		/// that passes it.
		/// </summary>
		private void AssertStringRoundTripsThroughWrites(string inserted, string updated)
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateGroupedTable(connection);

				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
					cursor.Insert(new object[] { 5, 3, inserted });

				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
				{
					cursor.SetRange(new object[] { 3 }, new object[] { 3 });
					Assert.That(cursor.Next(), Is.True, "the inserted row was not found");
					Assert.That(cursor[2], Is.EqualTo(inserted), "the value did not survive the insert");
					cursor[2] = updated;
					cursor.Update();
				}

				using (SQLStoreCursor cursor = OpenGroupedCursor(connection))
				{
					cursor.SetRange(new object[] { 3 }, new object[] { 3 });
					Assert.That(cursor.Next(), Is.True, "the updated row was not found");
					Assert.That(cursor[2], Is.EqualTo(updated), "the value did not survive the update");
				}

				using (SQLStoreCursor cursor = OpenCursorKeyedOnValue(connection))
				{
					cursor.SetRange(new object[] { 3 }, new object[] { 3 });
					Assert.That(cursor.Next(), Is.True, "the row was not found by the cursor that names it by V");
					cursor.Delete();
				}

				Assert.That(Count(connection, "select count(*) from T where G = 3"), Is.EqualTo(0), "the row named by its string key was not deleted");
			}
		}

		/// <summary>
		/// A string whose characters would end a quoted literal early - single quotes, a quote beside a statement separator, a
		/// closing parenthesis and an embedded NUL - survives an insert, an update, a key condition that names the row by it and
		/// a delete. The generated writes pass values as parameters; the quote-doubled literals they replaced cannot carry a NUL,
		/// because the provider prepares the statement from text that the NUL ends.
		/// </summary>
		[Test]
		public void AwkwardStringRoundTripsThroughWrites()
		{
			AssertStringRoundTripsThroughWrites("a';'b');'c\0d", "x'';'y\0z");
		}

		/// <summary>
		/// A string holding characters no single-byte code page has - Chinese, Greek and Cyrillic beside a letter a Latin-1 code page
		/// does hold - survives an insert, an update, and a key condition that names the row by it. Reproduction of
		/// device-mssql-string-parameters: MSSQLCommand described every string parameter to the server as varchar, and SQL Server
		/// converts a varchar value to the database's collation code page on its way in, so each character that code page has no room
		/// for arrived as a question mark - silently, and with nothing to read back afterwards but the question marks.
		/// </summary>
		/// <remarks>
		/// The column has to be able to hold the characters for the parameter's own type to be what decides this, and it can: the
		/// contract's T.V is nvarchar(20), as the catalog's own text columns are (41 nvarchar columns in SystemStoreCatalog.sql). The
		/// accented letters are there to tell two failures apart - a value that comes back with only those intact was converted to a
		/// Latin-1 code page, where one that comes back garbled throughout was lost for some other reason.
		/// </remarks>
		[Test]
		public void NonASCIIStringRoundTripsThroughWrites()
		{
			AssertStringRoundTripsThroughWrites("中文 αβγ Привет é", "Ωμέγα 漢字 Ж ü");
		}

		/// <summary>
		/// A boolean written through a store cursor comes back as a boolean, through both the values of an insert and the set list
		/// of an update. The catalog declares its boolean columns tinyint, so a generated write passes 1 or 0 rather than a .NET
		/// bool (SQLStoreConnection.NativeToParameter). On SQLite either mapping passes, because the provider stores a bool as an
		/// integer anyway; this pins the round trip for every store the contract is run against, and it is the store whose catalog
		/// declares the column with a boolean type, or whose provider binds a declared type strictly, that fails here unmapped.
		/// </summary>
		[Test]
		public void BooleanRoundTripsThroughWrites()
		{
			using (SQLStoreConnection connection = Store.Connect())
			{
				CreateBooleanTable(connection);

				using (SQLStoreCursor cursor = OpenBooleanCursor(connection))
				{
					cursor.Insert(new object[] { 1, true });
					cursor.Insert(new object[] { 2, false });
				}

				using (SQLStoreCursor cursor = OpenBooleanCursor(connection))
				{
					cursor.SetRange(null, null);
					Assert.That(cursor.Next() ? cursor[1] : null, Is.EqualTo(true), "the true row did not survive the insert");
					Assert.That(cursor.Next() ? cursor[1] : null, Is.EqualTo(false), "the false row did not survive the insert");
					Assert.That(cursor.Next(), Is.False);
				}

				using (SQLStoreCursor cursor = OpenBooleanCursor(connection))
				{
					cursor.SetRange(new object[] { 2 }, new object[] { 2 });
					Assert.That(cursor.Next(), Is.True);
					cursor[1] = true;
					cursor.Update();
				}

				using (SQLStoreCursor cursor = OpenBooleanCursor(connection))
				{
					cursor.SetRange(new object[] { 2 }, new object[] { 2 });
					Assert.That(cursor.Next() ? cursor[1] : null, Is.EqualTo(true), "the updated row did not come back true");
				}
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
		/// nothing for a later connection to see. A store whose outermost begin is not a real database transaction fails here,
		/// as does one whose nested commit commits to the database.
		/// </summary>
		// NOTE: this does not pin the other half of the outermost rollback, that the undo log is cleared rather than replayed,
		// and no test through the store's surface can: the database rollback that follows erases whatever a replay did, so the
		// final state is the same either way (measured by mutation during review - replaying the log here leaves all 11 green).
		// SQLStoreConnection.RollbackTransaction clears the log for a reason that is not observable state: a failing undo
		// statement would leave the database transaction open. Read that method, do not try to test it from here.
		[Test]
		public void OuterRollbackUndoesCommittedNestedLevel()
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
	}

	/// <summary>
	/// The contract against the SQL Server catalog store, on the server named by the environment variable
	/// DATAPHOR_TEST_MSSQL; the whole fixture is ignored where that is not set.
	/// </summary>
	/// <remarks>
	/// One database, with a fixed name, serves the whole fixture: creating and dropping one per test cost between 1.5 and 45
	/// seconds on top of a 4.9 second fixture when it was measured, and a fixed name means a crashed run leaves one database
	/// behind rather than one per run. <see cref="CreateStore"/> empties it at the start of each test.
	/// </remarks>
	// NOTE: that fixed name is the fixture's one shared resource. Two runs of this fixture against the same server at the
	// same time - two developers, or a developer and a CI agent, with DATAPHOR_TEST_MSSQL pointed at one shared instance -
	// would empty each other's database mid-test. If that day comes, name the database after the run, as TestEnvironment's
	// run directory is named after the process, and accept that a crashed run then leaves one database behind per run.
	[TestFixture]
	public class MSSQLStoreContractTests : StoreContractTests
	{
		private const string DatabaseName = "DataphorStoreContract";

		/// <summary>The test directory the fixture's one database was last emptied for; see <see cref="CreateStore"/>.</summary>
		private static string _emptiedForDirectory;

		[OneTimeSetUp]
		public void RequireServer()
		{
			if (!TestEnvironment.MSSQLConfigured)
				Assert.Ignore(TestEnvironment.MSSQLNotConfigured("run the catalog store contract against SQL Server"));

			// A run that crashed before its teardown, or one kept with DATAPHOR_TEST_LOGGING=1, leaves the database behind;
			// dropping it outright here starts this run on a database with nothing of another run's in it. The first
			// CreateStore makes it again, through the store's own create-database probe.
			_emptiedForDirectory = null;
			DropDatabase();
		}

		[OneTimeTearDown]
		public void DropDatabaseAfterFixture()
		{
			if (TestEnvironment.MSSQLConfigured && !TestEnvironment.KeepFiles)
				DropDatabase();
		}

		/// <summary>The store creates the database itself, through the <c>create database</c> probe in its own InternalInitialize.</summary>
		protected override SQLStore CreateStore(string directory)
		{
			MSSQLStore store = new MSSQLStore();
			store.ConnectionString = TestEnvironment.MSSQLConnectionStringFor(DatabaseName);
			store.Initialize();
			EmptyDatabaseForNewTest(directory);
			return store;
		}

		// After the store, so that its probe has made the database on the first test of the run. A second call on the same
		// directory is SeedSchemaReopens reopening the database it just wrote, and must leave what is in it alone.
		private static void EmptyDatabaseForNewTest(string directory)
		{
			if (directory == _emptiedForDirectory)
				return;
			DropTables();
			_emptiedForDirectory = directory;
		}

		// Single user first: a pooled connection left over from an earlier run blocks DROP DATABASE otherwise.
		private static void DropDatabase()
		{
			TestEnvironment.ExecuteOnMSSQL
			(
				TestEnvironment.MSSQLConnectionStringFor("master"),
				String.Format
				(
					"if db_id('{0}') is not null begin alter database [{0}] set single_user with rollback immediate; drop database [{0}]; end",
					DatabaseName
				)
			);
		}

		// Everything an earlier test left behind, so that this one starts on an empty database.
		// NOTE: one batch in no particular order, because nothing this fixture's tests create - the contract's own tables and
		// the catalog seed script's - has a foreign key to anything else. A contract test that added one would break this
		// teardown rather than itself: drop the constraints first, or order the drops, if that day comes.
		private static void DropTables()
		{
			TestEnvironment.ExecuteOnMSSQL
			(
				TestEnvironment.MSSQLConnectionStringFor(DatabaseName),
				"declare @statements nvarchar(max) = N''; select @statements = @statements + N'drop table [' + name + N'];' from sys.tables; exec sp_executesql @statements;"
			);
		}
	}
}
