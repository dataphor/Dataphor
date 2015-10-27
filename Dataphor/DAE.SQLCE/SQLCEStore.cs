/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Simple SQL CE Store
	
	A simple storage device that uses a SQL Server Everywhere instance as it's backend.
	The store is capable of storing integers, strings, booleans, and long text and binary data.
	The store also manages logging and rollback of nested transactions to make up for the lack of savepoint support in SQL Server Everywhere.
*/

using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store.SQLCE
{
	public class SQLCEStore : SQLStore
	{
		/// <summary>Initializes the store, ensuring that an instance of the server is running and a database is attached.</summary>
		protected override void InternalInitialize()
		{
			_supportsMARS = true;
			_supportsUpdatableCursor = true;
			
			DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
			builder.ConnectionString = ConnectionString;
			if (builder.ContainsKey("Data Source"))
			{
				string databaseFileName = (string)builder["Data Source"];
				if (!File.Exists(databaseFileName))
				{
					using (SqlCeEngine engine = new SqlCeEngine(ConnectionString))
					{
						engine.CreateDatabase();
					}
				}
			}
		}

		public override SQLConnection GetSQLConnection()
		{
			return new SQLCEConnection(ConnectionString);
		}

		protected override SQLStoreConnection InternalConnect()
		{
			return new SQLCEStoreConnection(this);
		}
	}
}
