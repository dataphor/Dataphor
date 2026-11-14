/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Connection;

// To specify that a SQLite store be used for catalog persistence, add the following attributes to the alias definition in the ServerAliases.config file:
// catalogstoreclassname="Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite" 
// catalogstoreconnectionstring="Data Source={0}" where {0} is the file name of SQLite database

namespace Alphora.Dataphor.DAE.Store.SQLite
{
	public class SQLiteStore : SQLStore
	{
		/// <summary>How long, in milliseconds, a store connection waits for another session's write lock when the connection string sets no BusyTimeout.</summary>
		public const int DefaultBusyTimeout = 30000;

		protected override void InternalInitialize()
		{
			_supportsMARS = true;
			_supportsUpdatableCursor = false;
			ConnectionString = NormalizeConnectionString(ConnectionString);
		}

		// Every store connection, the catalog device's own included, is built from ConnectionString, so all of them get these options.
		private static string NormalizeConnectionString(string connectionString)
		{
			System.Data.SQLite.SQLiteConnectionStringBuilder builder = new System.Data.SQLite.SQLiteConnectionStringBuilder(connectionString);

			// The provider accepts each of these keywords with and without the space, and the spaced one wins when both are given;
			// the keywords are set by name because the builder's typed properties do not write the same spelling on every runtime.

			// Write-ahead logging, so that readers never wait for the one writer (K10). Forced, and a configured Journal Mode is
			// ignored: under the rollback journal a writer blocks every reader. WAL keeps -wal and -shm files beside the catalog
			// file and does not work on a network file system, so the catalog folder must be on a local disk.
			builder.Remove("JournalMode");
			builder["Journal Mode"] = "Wal";

			if (!builder.ContainsKey("BusyTimeout"))
				builder["BusyTimeout"] = DefaultBusyTimeout;

			// The provider retries a statement that SQLite reports busy until the command timeout (whole seconds) has passed, and
			// each retry waits up to BusyTimeout again. A command timeout just below BusyTimeout ends the wait after the first busy
			// report, so that BusyTimeout alone bounds it.
			if (!builder.ContainsKey("Default Timeout") && !builder.ContainsKey("DefaultTimeout"))
				builder["Default Timeout"] = builder.BusyTimeout > 0 ? (builder.BusyTimeout - 1) / 1000 : 0;

			return builder.ConnectionString;
		}

		public override SQLConnection GetSQLConnection()
		{
			return new SQLiteConnection(ConnectionString);
		}

		protected override SQLStoreConnection InternalConnect()
		{
			return new SQLiteStoreConnection(this);
		}
	}
}
