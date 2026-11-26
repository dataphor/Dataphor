/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	using D4 = Alphora.Dataphor.DAE.Language.D4;

	/// <summary>
	/// What the SQL Server device makes of the server it is pointed at, on the server named by the environment variable
	/// DATAPHOR_TEST_MSSQL; the whole fixture is ignored where that is not set.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the fixture the supported-version matrix in docs/users-guide/dataphor-service.md is measured with: pointed at
	/// one SQL Server version after another it asks the questions that are version-sensitive and that no other pass asks.
	/// First, whether the device reads the server's version at all - DetermineVersion is the first statement the device
	/// issues and it parses the string serverproperty('productversion') returns, so an unexpected version string is the
	/// first thing that can fail against a server nobody has tried. Second, whether starting the device still creates its
	/// database, which goes through the sysdatabases compatibility view and is therefore one more thing a later SQL Server
	/// could take away. Third, what the device makes of a table that already exists on the server, which is how a
	/// deployment adopts a schema it did not create: SQL Server has had date, time, datetime2 and datetimeoffset since
	/// 2008 and the device's column filter accepts only the first two, so a schema using the other two loses columns, on
	/// every version in the matrix.
	/// </para>
	/// <para>
	/// Both devices this suite otherwise drives live are reconciliation masters that create their own tables
	/// (LiveDeviceTest); nothing before this fixture read an existing schema off a real server, so nothing exercised
	/// ShouldIncludeColumn, FindScalarType or the sys.columns query the device builds against anything but the golden files.
	/// </para>
	/// <para>
	/// The device is created in the MSSQLDevice library and the reconciled table variable therefore lands there too. A library
	/// of its own would be tidier, but an object in General - the session's default library - may not reference a device in a
	/// library General does not require, and MSSQLDevice requires itself trivially.
	/// </para>
	/// </remarks>
	// Not [Parallelizable]: it starts a server, and two engines in one process share the engine's static state (see the note
	// on TestServerInstance).
	[TestFixture]
	public class MSSQLVersionMatrixTest
	{
		/// <summary>The database this pass puts its device and its one table in, on the server DATAPHOR_TEST_MSSQL names.</summary>
		private const string DatabaseName = "DataphorVersionMatrix";

		/// <summary>The library the device and the reconciled table variable are created in; see the fixture's remarks.</summary>
		private const string LibraryName = "MSSQLDevice";

		private const string DeviceName = "MatrixDevice";
		private static string QualifiedDeviceName => LibraryName + "." + DeviceName;

		/// <summary>The table created on the server directly, for the device to find when it reconciles.</summary>
		private const string TableName = "MatrixTypes";

		/// <summary>
		/// What the reconciled table variable is called: the library it landed in, the SQL schema the table is in, and the
		/// table's own name.
		/// </summary>
		/// <remarks>
		/// The SQL schema is part of the D4 name because this device's Schema attribute is not set - SQLDevice.GetServerTableName
		/// leaves the schema out of the name only for the one schema that attribute names - so a deployment adopting a dbo
		/// schema gets table variables called Library.dbo.Table unless it sets Schema = "dbo" on the device.
		/// </remarks>
		private static string QualifiedTableName => LibraryName + ".dbo." + TableName;

		/// <summary>The library folders the server carries: the device and the two libraries it requires.</summary>
		private static readonly string[] Libraries = ["Connection", "SQLDevice", "MSSQLDevice"];

		/// <summary>
		/// One column per SQL Server type whose reconciliation is in question, created on the server by this pass rather than
		/// by the device. Every type here has existed since SQL Server 2008, so every version in the matrix has all of them.
		/// </summary>
		private const string CreateTheTable =
			"create table " + TableName + " (" +
				" Id int not null primary key," +
				" DateTimeColumn datetime null," +
				" DateColumn date null," +
				" TimeColumn time null," +
				" DateTime2Column datetime2 null," +
				" DateTimeOffsetColumn datetimeoffset null," +
				" VarcharColumn varchar(30) null," +
				" NVarcharColumn nvarchar(30) null" +
			");";

		/// <summary>
		/// What the device is expected to make of each column of the table above: the D4 type it reconciles to and the
		/// Storage.Length tag it carries, or null for a column the device drops.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is a characterization of what the device does today, not a statement of what it ought to do, and two of the
		/// entries record a defect rather than a design.
		/// </para>
		/// <para>
		/// datetime2 and datetimeoffset are dropped. MSSQLDevice.ShouldIncludeColumn has had both commented out of its case
		/// list since 2008 (MSSQLTypes.cs carries the matching "TODO: Support for datetime2"), and a column the filter
		/// rejects is left out of the reconciled table variable silently - SQLDevice.GetDeviceTables simply does not add it.
		/// So a deployment adopting a schema that uses either type gets a table variable missing those columns with nothing
		/// said about it. bug-mssql-device-drops-datetime2-columns owns it; a fix changes the two entries below, and this case
		/// then says so by name.
		/// </para>
		/// <para>
		/// NVarcharColumn's length is 60 for an nvarchar(30). The device's reconciliation query selects sys.columns.max_length,
		/// which is a count of bytes, and FindScalarType writes it into Storage.Length, which is a count of characters - so
		/// every Unicode column reconciles twice as wide as it is, and the length constraint the engine then compiles admits
		/// strings the server cannot store. The fourth arm of bug-mssql-device-text-columns-lose-characters owns it; a fix
		/// makes this entry 30.
		/// </para>
		/// </remarks>
		private static readonly Dictionary<string, (string D4Type, string StorageLength)> ExpectedColumns =
			new Dictionary<string, (string, string)>
			{
				{ "Id", ("System.Integer", null) },
				{ "DateTimeColumn", ("System.DateTime", null) },
				{ "DateColumn", ("System.Date", null) },
				{ "TimeColumn", ("System.Time", null) },
				{ "DateTime2Column", (null, null) },
				{ "DateTimeOffsetColumn", (null, null) },
				{ "VarcharColumn", ("System.String", "30") },
				{ "NVarcharColumn", ("System.String", "60") }
			};

		private TestServerInstance _instance;

		/// <summary>What the server says it is, recorded for the matrix row this run produces.</summary>
		private string _productVersion;
		private string _edition;
		private string _collation;

		/// <summary>Whatever Reconcile returned, one message per row, read once in the setup because reconciling twice is not the same act.</summary>
		private List<string> _reconciliationErrors;

		[OneTimeSetUp]
		public void BuildTheServerAndReconcile()
		{
			if (!TestEnvironment.MSSQLConfigured)
				Assert.Ignore(TestEnvironment.MSSQLNotConfigured("measure what the SQL Server device makes of a SQL Server version"));

			string master = TestEnvironment.MSSQLConnectionStringFor("master");
			_productVersion = TestEnvironment.QueryMSSQL(master, "select cast(serverproperty('productversion') as nvarchar(128))");
			_edition = TestEnvironment.QueryMSSQL(master, "select cast(serverproperty('edition') as nvarchar(128))");

			// Dropped rather than emptied, so that starting the device below exercises ShouldEnsureDatabase - the device's
			// create-the-database-if-absent path, which reads the sysdatabases compatibility view and is therefore one more
			// thing a later SQL Server could take away. Single-user first because a connection left by an earlier run would
			// otherwise block the drop; this database is this pass's own, as the databases EmptyMSSQLDatabase prepares are.
			TestEnvironment.ExecuteOnMSSQL
			(
				master,
				String.Format
				(
					"if db_id('{0}') is not null begin alter database [{0}] set single_user with rollback immediate; drop database [{0}]; end",
					DatabaseName
				)
			);

			_instance = new TestServerInstance("VersionMatrix", Libraries);
			try
			{
				_instance.Server.Start();
				_instance.ExecuteScript
				(
					String.Format
					(
						"EnsureLibraryRegistered('{0}');\r\n" +
							"SetLibrary('{0}');\r\n" +
							"{2}" +
							"Diagnostics.EnsureDeviceStarted('{1}');\r\n",
						LibraryName,
						DeviceName,
						// Reconciliation mode none: this pass reconciles once, by the Reconcile call below, so that what the
						// device made of the schema is read at a point of this fixture's choosing rather than as a side effect.
						TestEnvironment.MSSQLDeviceDefinition(DeviceName, DatabaseName, "none")
					)
				);

				// After the device has started, because the device is what created the database. The collation recorded is the
				// database's rather than the server's: it is the database collation that decides what a varchar column holds.
				_collation = TestEnvironment.QueryMSSQL(master, String.Format("select cast(databasepropertyex('{0}', 'collation') as nvarchar(128))", DatabaseName));
				TestContext.Progress.WriteLine
				(
					"SQL Server {0}, {1}, database collation {2}",
					_productVersion,
					_edition,
					_collation ?? "(the device did not create the database)"
				);

				// The schema the device did not make, for the reconciliation case to read.
				TestEnvironment.ExecuteOnMSSQL(TestEnvironment.MSSQLConnectionStringFor(DatabaseName), CreateTheTable);
				_reconciliationErrors = Reconcile();
			}
			catch
			{
				_instance.Dispose();
				_instance = null;
				throw;
			}
		}

		[OneTimeTearDown]
		public void StopTheServer()
		{
			if (_instance != null)
			{
				_instance.Dispose();
				_instance = null;
			}
		}

		private List<string> Reconcile()
		{
			List<string> errors = new List<string>();
			IServerSession session = _instance.Server.Connect(new SessionInfo("Admin", String.Empty, LibraryName));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					// No transaction of this pass's own, which is how a deployment reconciles: the whole reconciliation runs in
					// SystemReconcileNode.InternalExecute while the cursor is opened, so it is the open call's own implicit
					// transaction that carries the table variables into the catalog, and ServerProcess.EndTransactionalCall
					// commits that at the end of the call.
					//
					// Reconcile returns its failures rather than raising them - one row per object it could not reconcile - so a
					// pass that only executed it would report a silent partial reconciliation as a success.
					IServerCursor cursor = process.OpenCursor(String.Format("Reconcile('{0}')", QualifiedDeviceName), null);
					try
					{
						while (cursor.Next())
							using (IRow row = cursor.Select())
								errors.Add(Convert.ToString(row["Error"]));
					}
					finally
					{
						process.CloseCursor(cursor);
					}
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				_instance.Server.Disconnect(session);
			}
			return errors;
		}

		/// <summary>
		/// The device reads the version of the server it is pointed at, and does not take it for SQL Server 7.0.
		/// DetermineVersion is the device's first statement and it takes the characters before the first dot of
		/// serverproperty('productversion') as an integer, so a server whose version string it cannot parse fails the device's
		/// start outright; MajorVersion then decides which catalog views the reconciliation queries use (sys.objects from 9,
		/// the sysobjects compatibility views below it) and IsMSSQL70 decides whether the device emits its support operators
		/// at all.
		/// </summary>
		/// <remarks>
		/// MajorVersion is read off the started device by reflection because the device assembly is loaded from the runtime
		/// layout at run time - that is what a library is - so this project cannot reference it. It is a device attribute like
		/// any other (a device definition may set "MajorVersion"), which is to say the engine sets it by reflection too.
		/// </remarks>
		[Test]
		public void TheDeviceReadsTheServerVersion()
		{
			// To this test's own output rather than only to the progress stream, so that the version a run was pointed at is in
			// the TRX a CI run uploads: CI supplies whatever LocalDB its image carries, and nothing else records which that was.
			TestContext.Out.WriteLine("SQL Server {0}, {1}, database collation {2}", _productVersion, _edition, _collation);

			Assert.That(_productVersion, Does.Match(@"^[0-9]+\."), "the server did not report a version of the shape the device parses, which is what DetermineVersion would fail on.");
			int reported = Int32.Parse(_productVersion.Substring(0, _productVersion.IndexOf('.')));

			Assert.That(DeviceProperty("MajorVersion"), Is.EqualTo(reported), "the device did not determine the server's major version.");
			Assert.That(DeviceProperty("IsMSSQL70"), Is.False, "the device took the server for SQL Server 7.0, which changes the SQL it emits.");
		}

		/// <summary>
		/// Starting the device created the database it was given, which is the device's ShouldEnsureDatabase path (on by
		/// default) and the normal one for a container or a fresh server. The setup dropped the database first, so the database
		/// existing now is the device's work.
		/// </summary>
		[Test]
		public void TheDeviceCreatesTheDatabaseItIsGiven()
		{
			Assert.That(_collation, Is.Not.Null, "the device did not create its database; EnsureDatabase reads the sysdatabases compatibility view, which this server may no longer have.");
		}

		/// <summary>
		/// What the device makes of a table that already existed on the server: which columns reach the reconciled table
		/// variable, as what D4 type, and with what Storage.Length. See ExpectedColumns for what is characterization and what
		/// is a recorded defect.
		/// </summary>
		[Test]
		public void ReconcilesAPreExistingSchema()
		{
			Assert.That(_reconciliationErrors, Is.Empty, "Reconcile reported errors.");

			int index = _instance.Server.Catalog.IndexOfName(QualifiedTableName);
			Assert.That
			(
				index,
				Is.GreaterThanOrEqualTo(0),
				String.Format("the device did not reconcile {0} into the catalog at all. Table variables in the catalog: {1}.", QualifiedTableName, TableVarNames())
			);
			Schema.BaseTableVar tableVar = (Schema.BaseTableVar)_instance.Server.Catalog[index];

			Dictionary<string, (string, string)> reconciled = new Dictionary<string, (string, string)>();
			StringBuilder report = new StringBuilder();
			foreach (Schema.TableVarColumn column in tableVar.Columns)
			{
				string storageName = D4.MetaData.GetTag(column.MetaData, "Storage.Name", column.Name);
				string storageLength =
					column.MetaData == null || !column.MetaData.Tags.Contains("Storage.Length")
						? null
						: column.MetaData.Tags["Storage.Length"].Value;
				reconciled.Add(storageName, (column.DataType.Name, storageLength));
				report.AppendFormat("  {0} -> {1}{2}\r\n", storageName, column.DataType.Name, storageLength == null ? String.Empty : " Storage.Length " + storageLength);
			}
			TestContext.Progress.WriteLine("{0} reconciled on SQL Server {1} as:\r\n{2}", TableName, _productVersion, report);

			foreach (KeyValuePair<string, (string D4Type, string StorageLength)> expected in ExpectedColumns)
				if (expected.Value.D4Type == null)
					Assert.That(reconciled.ContainsKey(expected.Key), Is.False, String.Format("{0} now reconciles, which it did not before: record what the device makes of it.", expected.Key));
				else
					Assert.That(reconciled, Contains.Key(expected.Key).WithValue((expected.Value.D4Type, expected.Value.StorageLength)), String.Format("{0} did not reconcile as it did before.", expected.Key));

			Assert.That(reconciled.Keys, Is.SubsetOf(ExpectedColumns.Keys), "the device reconciled a column this fixture does not know about.");
		}

		/// <summary>Every table variable the catalog holds, for the failure message when the one expected is not among them.</summary>
		private string TableVarNames()
		{
			List<string> names = new List<string>();
			foreach (Schema.Object objectValue in _instance.Server.Catalog)
				if (objectValue is Schema.BaseTableVar)
					names.Add(objectValue.Name);
			names.Sort();
			return names.Count == 0 ? "none" : String.Join(", ", names);
		}

		/// <summary>One property of the started device, by reflection; see TheDeviceReadsTheServerVersion.</summary>
		private object DeviceProperty(string name)
		{
			int index = _instance.Server.Catalog.IndexOfName(QualifiedDeviceName);
			Assert.That(index, Is.GreaterThanOrEqualTo(0), String.Format("{0} is not in the catalog.", QualifiedDeviceName));
			object device = _instance.Server.Catalog[index];
			PropertyInfo property = device.GetType().GetProperty(name);
			Assert.That(property, Is.Not.Null, String.Format("{0} has no property {1}; it has been renamed, and this fixture has to follow it.", device.GetType().Name, name));
			return property.GetValue(device);
		}
	}
}
