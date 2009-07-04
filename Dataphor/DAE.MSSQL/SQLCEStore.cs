/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Simple SQL CE Store
	
	A simple storage device that uses a SQL Server Everywhere instance as it's backend.
	The store is capable of storing integers, strings, booleans, and long text and binary data.
	The store also manages logging and rollback of nested transactions to make up for the lack of savepoint support in SQL Server Everywhere.
*/

using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store.SQLCE
{
	public class SQLCEStore : SQLStore
	{
		/// <summary>Initializes the store, ensuring that an instance of the server is running and a database is attached.</summary>
        protected override void InternalInitialize()
		{
            // Nothing to do, database creation can be accomplished with external management tools.
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
	
	public class SQLCEStoreConnection : SQLStoreConnection
	{
        public SQLCEStoreConnection(SQLCEStore AStore) : base(AStore) { }

		protected override DbConnection InternalCreateConnection()
		{
			return new SqlConnection(Store.ConnectionString);
		}

        public override bool HasTable(string ATableName)
        {
            return ((int)this.ExecuteScalar(String.Format("select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{0}'", ATableName)) != 0);
        }
	}
}
