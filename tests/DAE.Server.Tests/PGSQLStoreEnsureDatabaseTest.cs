/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data.Common;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Connection.PGSQL;
using Alphora.Dataphor.DAE.Store.PGSQL;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// That initializing the PostgreSQL catalog store creates the database its connection string names, whichever of the
	/// driver's two keywords that connection string spells it with. Gated on DATAPHOR_TEST_PGSQL like the store's contract
	/// fixture, and the store makes its own database, so a bare server is enough.
	/// </summary>
	/// <remarks>
	/// The reproduction of a defect store-pgsql-port's review found: the probe looked only for the keyword Database, and
	/// Npgsql also accepts DB for it - not as an unknown keyword it would reject, but as the same setting, so a
	/// catalogstoreconnectionstring that spelled it DB got no database made and the first start of the server failed with
	/// PostgreSQL 3D000 naming the database it was supposed to create. The canonical spelling is proved by
	/// PGSQLStoreContractTests, whose own database is made by this probe on the first case of its run; this case is the
	/// alias, which nothing else exercises.
	/// </remarks>
	[TestFixture]
	public class PGSQLStoreEnsureDatabaseTest
	{
		/// <summary>Lower case, and this fixture's alone, for the reasons PGSQLStoreContractTests gives for its own.</summary>
		private const string DatabaseName = "dataphorstoreensuredatabase";

		[Test]
		public void TheStoreCreatesTheDatabaseNamedByTheDriversAliasForTheDatabaseKeyword()
		{
			if (!TestEnvironment.PGSQLConfigured)
				Assert.Ignore(TestEnvironment.PGSQLNotConfigured("prove that the PostgreSQL catalog store creates its database"));

			TestEnvironment.DropPGSQLDatabase(DatabaseName);
			try
			{
				PostgreSQLStore store = new PostgreSQLStore();
				store.ConnectionString = ConnectionStringWithAliasedDatabase();
				store.Initialize();

				Assert.That(DatabaseExists(), Is.True, "the store left the database its connection string named unmade, so a server configured this way would not start");
			}
			finally
			{
				if (!TestEnvironment.KeepFiles)
					TestEnvironment.DropPGSQLDatabase(DatabaseName);
			}
		}

		/// <summary>The configured server with <see cref="DatabaseName"/> under the driver's alias <c>DB</c> rather than under <c>Database</c>.</summary>
		private static string ConnectionStringWithAliasedDatabase()
		{
			DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
			builder.ConnectionString = TestEnvironment.PGSQLConnectionString;
			builder["DB"] = DatabaseName;
			return builder.ConnectionString;
		}

		private static bool DatabaseExists()
		{
			using (PostgreSQLConnection connection = new PostgreSQLConnection(TestEnvironment.PGSQLConnectionStringFor(TestEnvironment.PGSQLStockDatabaseName)))
			using (SQLCommand command = connection.CreateCommand(false))
			{
				command.CommandType = SQLCommandType.Statement;
				command.Statement = String.Format("select count(*) from pg_database where datname = '{0}'", DatabaseName);
				return Convert.ToInt64(command.Evaluate()) > 0;
			}
		}
	}
}
