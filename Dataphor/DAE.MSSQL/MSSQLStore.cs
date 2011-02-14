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

			if (builder.ContainsKey("MultipleActiveResultSets"))
			{
				_supportsMARS = "True" == ((string)builder["MultipleActiveResultSets"]);
			}

			
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
						MSSQLConnection connection = new MSSQLConnection(builder.ConnectionString);
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
