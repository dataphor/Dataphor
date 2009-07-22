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
using System.Data.SqlClient;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Connection.PGSQL;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Store.PGSQL
{
    public class PostgreSQLStore : SQLStore
	{
		/// <summary>Initializes the store, ensuring that an instance of the server is running and a database is attached.</summary>
        protected override void InternalInitialize()
		{
			DbConnectionStringBuilder LBuilder = new DbConnectionStringBuilder();
			LBuilder.ConnectionString = ConnectionString;
			
            FSupportsMARS = LBuilder.ContainsKey("MultipleActiveResultSets") && (bool)LBuilder["MultipleActiveResultSets"];
            FSupportsUpdatableCursor = false;
            
            if (FShouldEnsureDatabase)
            {
				string LDatabaseName = null;
				if (LBuilder.ContainsKey("Initial Catalog"))
				{
					LDatabaseName = (string)LBuilder["Initial Catalog"];
					LBuilder["Initial Catalog"] = "master";
				}
				else if (LBuilder.ContainsKey("Database"))
				{
					LDatabaseName = (string)LBuilder["Database"];
					LBuilder["Database"] = "master";
				}
				
				if (!String.IsNullOrEmpty(LDatabaseName))
				{
					if (!Parser.IsValidIdentifier(LDatabaseName))
						throw new ArgumentException("Database name specified in store connection string is not a valid identifier.");
						
					try
					{
						#if USESQLCONNECTION
                        PostgreSQLConnection LConnection = new PostgreSQLConnection(LBuilder.ConnectionString);
						LConnection.Execute(String.Format("if not exists (select * from sysdatabases where name = '{0}') create database {0}", LDatabaseName));
						#else
						SqlConnection LConnection = new SqlConnection(LBuilder.ConnectionString);
						LConnection.Open();
						SqlCommand LCommand = LConnection.CreateCommand();
						LCommand.CommandType = CommandType.Text;
						LCommand.CommandText = String.Format("if not exists (select * from sysdatabases where name = '{0}') create database {0}", LDatabaseName);
						LCommand.ExecuteNonQuery();
						#endif
					}
					catch
					{
						// Do not rethrow, this does not necessarily indicate failure, let the non-existence of the database throw later
					}
				}
			}
		}
		
		private bool FShouldEnsureDatabase = true;
		public bool ShouldEnsureDatabase
		{
			get { return FShouldEnsureDatabase; }
			set { FShouldEnsureDatabase = value; }
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
