/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESQLCONNECTION

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Store.MSSQL
{
    public class MSSQLStore : SQLStore
	{
		/// <summary>Initializes the store, ensuring that an instance of the server is running and a database is attached.</summary>
        protected override void InternalInitialize()
		{
			DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
			builder.ConnectionString = ConnectionString;

			// The catalog opens a second store cursor while a first one is still open (CatalogStore reads PK_DAECatalogObjects
			// and opens PK_DAEBaseCatalogObjects inside the loop, and does the same around the two name tables), and SQL Server
			// refuses a second reader on one connection unless the connection asked for multiple active result sets. The error
			// it gives then - "There is already an open DataReader associated with this Command which must be closed first." -
			// names nothing a deployment could act on, so the option is forced rather than defaulted, and a configured False is
			// ignored: the store cannot work under it. Same reasoning as SQLiteStore.NormalizeConnectionString forcing WAL.
			// The base's ConnectionString setter allows this because InternalInitialize runs before the store is initialized,
			// and every store connection, the catalog device's own included, is built from ConnectionString.
			builder["MultipleActiveResultSets"] = "True";

			// Microsoft.Data.SqlClient, which replaced System.Data.SqlClient in release 4 (device-mssql-port), defaults Encrypt to
			// on and then validates the server's certificate; System.Data.SqlClient did neither. A catalogstoreconnectionstring a
			// deployment wrote for release 3 names a server, a database and its credentials and nothing else, so it would stop
			// connecting to a SQL Server with a self-signed or absent certificate - the ordinary state of an internal one - and the
			// server would fail to start. Encrypt=False is what release 3 negotiated, so that is the default; unlike the option
			// above it is only a default, because a deployment that has chosen either of these two settings has chosen how this
			// connection is protected and must be left alone. TrustServerCertificate counts as having chosen: it is the other way
			// to let an untrusted certificate through, and defaulting Encrypt beside it would contradict it.
			if (!builder.ContainsKey("Encrypt") && !builder.ContainsKey("TrustServerCertificate"))
				builder["Encrypt"] = "False";

			ConnectionString = builder.ConnectionString;
			_supportsMARS = true;

            _supportsUpdatableCursor = false;
            
            if (_shouldEnsureDatabase)
            {
				string databaseName = null;
				if (builder.ContainsKey("Initial Catalog"))
				{
					databaseName = (string)builder["Initial Catalog"];
					builder["Initial Catalog"] = "master";
				}
				else if (builder.ContainsKey("Database"))
				{
					databaseName = (string)builder["Database"];
					builder["Database"] = "master";
				}
				
				if (!String.IsNullOrEmpty(databaseName))
				{
					if (!Parser.IsValidIdentifier(databaseName))
						throw new ArgumentException("Database name specified in store connection string is not a valid identifier.");
						
					try
					{
						#if USESQLCONNECTION
						using (MSSQLConnection connection = new MSSQLConnection(builder.ConnectionString))
						{
							connection.Execute(String.Format("if not exists (select * from sysdatabases where name = '{0}') create database {0}", databaseName));
							#else
							SqlConnection connection = new SqlConnection(builder.ConnectionString);
							connection.Open();
							SqlCommand command = connection.CreateCommand();
							command.CommandType = CommandType.Text;
							command.CommandText = String.Format("if not exists (select * from sysdatabases where name = '{0}') create database {0}", databaseName);
							command.ExecuteNonQuery();
							#endif
						}
					}
					catch
					{
						// Do not rethrow, this does not necessarily indicate failure, let the non-existence of the database throw later
					}
				}
			}
		}
		
		private bool _shouldEnsureDatabase = true;
		public bool ShouldEnsureDatabase
		{
			get { return _shouldEnsureDatabase; }
			set { _shouldEnsureDatabase = value; }
		}

	    public override SQLConnection GetSQLConnection()
		{
            return new MSSQLConnection(ConnectionString);
		}

		protected override SQLStoreConnection InternalConnect()
		{
            return new MSSQLStoreConnection(this);
		}
	}
}
