/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data.Common;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Connection.PGSQL;

namespace Alphora.Dataphor.DAE.Store.PGSQL
{
    public class PostgreSQLStore : SQLStore
	{
		/// <summary>The stock database every PostgreSQL installation has, which a connection that is about to create the store's own database is pointed at.</summary>
		private const string StockDatabaseName = "postgres";

		/// <summary>
		/// Every keyword the driver accepts for the database to connect to, canonical name first.
		/// </summary>
		/// <remarks>
		/// Both spellings have to be read, as MSSQLStore reads both of SQL Server's ("Initial Catalog" and "Database"): the
		/// alias is not an unknown keyword the driver would reject, it connects to the named database just as the canonical
		/// spelling does, so a probe that looked only for the canonical one would skip the create silently and leave a first
		/// start failing with PostgreSQL 3D000 on a database it was supposed to make. Measured against Npgsql 10.0.4 by
		/// starting a server whose catalogstoreconnectionstring spelled it DB. DbConnectionStringBuilder matches a key
		/// case-insensitively, so each spelling is one entry.
		/// </remarks>
		private static readonly string[] DatabaseKeywords = new string[] { "Database", "DB" };

		/// <summary>Initializes the store, ensuring that an instance of the server is running and a database is attached.</summary>
        protected override void InternalInitialize()
        {
			// Both false, where the SQL Server and SQLite stores report MARS true. Npgsql opens one reader at a time on a
			// connection, so the store must not be told it can hold a second one: _supportsMARS false is what turns off
			// CatalogStore.UseCursorCache, which is the only thing that would keep a finished cursor's reader open past
			// CloseCursor and so leave a second one to collide with it. The catalog opens its cursors one at a time
			// otherwise, which is why this store needs no MARS of its own where the SQL Server store forces it on.
			// _supportsUpdatableCursor false sends the catalog's inserts and deletes through generated statements, as it
			// does for every store we build.
			// NOTE: with the cursor cache off, every catalog read opens and closes a cursor of its own where the other two
			// stores reuse one within a transaction, so a read costs one more statement here. Not measured against a large
			// catalog; if catalog reads on this store ever show up as slow, that is the difference to measure first, and the
			// way out is a cache that keeps the cursor without keeping its reader rather than this flag.
            _supportsMARS = false;
            _supportsUpdatableCursor = false;

			if (_shouldEnsureDatabase)
				EnsureDatabase();
        }

		/// <summary>Creates the database the connection string names if the server does not have it yet.</summary>
		/// <remarks>
		/// What MSSQLStore.InternalInitialize does with master, against the stock postgres database instead: PostgreSQL has no
		/// create-if-absent form and cannot create a database from a connection to the one being created, so the name is swapped
		/// out of the connection string for the probe and the create. The PostgreSQL device does the same thing in
		/// PostgreSQLDevice.EnsureDatabase, with the same two statements.
		/// The failure is swallowed for the reason the SQL Server store gives: a login without CREATEDB, or a server that is not
		/// reachable at all, is not distinguishable here, and the next connection - the one the catalog actually needs - reports
		/// it against the real database name.
		/// </remarks>
		private void EnsureDatabase()
		{
			DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
			builder.ConnectionString = ConnectionString;

			// Replaced under the spelling the connection string used, rather than added under the canonical one: setting both
			// keywords would hand the driver two names for one database.
			string databaseKeyword = Array.Find(DatabaseKeywords, keyword => builder.ContainsKey(keyword));
			if (databaseKeyword == null)
				return;

			string databaseName = Convert.ToString(builder[databaseKeyword]);
			if (String.IsNullOrEmpty(databaseName))
				return;

			builder[databaseKeyword] = StockDatabaseName;

			// Both statements carry the name as text the server parses, so both quote it the way that position needs: doubled
			// single quotes inside the probe's string literal, doubled double quotes inside the create statement's quoted
			// identifier. Quoted rather than checked against Parser.IsValidIdentifier, which is what the SQL Server store does
			// because it interpolates the name unquoted: that guard would also refuse an ordinary PostgreSQL database name with
			// a hyphen or a space in it, which a deployment may already have. Quoting also means the name is created in the case
			// it is written in, and the connection then names the same database, so the two agree whatever the case.
			string databaseLiteral = databaseName.Replace("'", "''");
			string databaseIdentifier = databaseName.Replace("\"", "\"\"");

			try
			{
				using (PostgreSQLConnection connection = new PostgreSQLConnection(builder.ConnectionString))
				{
					bool exists;
					using (SQLCommand command = connection.CreateCommand(false))
					{
						command.CommandType = SQLCommandType.Statement;
						command.Statement = String.Format("select count(*) from pg_database where datname = '{0}'", databaseLiteral);
						exists = Convert.ToInt64(command.Evaluate()) > 0;
					}

					if (!exists)
						connection.Execute(String.Format("create database \"{0}\"", databaseIdentifier));
				}
			}
			catch
			{
				// Do not rethrow, this does not necessarily indicate failure, let the non-existence of the database throw later
			}
		}

		private bool _shouldEnsureDatabase = true;
		/// <summary>Whether the store creates the database its connection string names when the server does not have it (default true).</summary>
		public bool ShouldEnsureDatabase
		{
			get { return _shouldEnsureDatabase; }
			set { _shouldEnsureDatabase = value; }
		}

	    public override SQLConnection GetSQLConnection()
		{
            return new PostgreSQLConnection(ConnectionString);
		}

		protected override SQLStoreConnection InternalConnect()
		{
            return new PostgreSQLStoreConnection(this);
		}
	}
}
