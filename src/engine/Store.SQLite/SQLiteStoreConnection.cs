/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESQLCONNECTION

using System;
using System.Data.Common;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store.SQLite
{
    public class SQLiteStoreConnection : SQLStoreConnection
    {
        public SQLiteStoreConnection(SQLiteStore store) : base(store) { }

		#if USESQLCONNECTION
		protected override SQLConnection InternalCreateConnection()
		{
			return new SQLiteConnection(Store.ConnectionString);
		}
		#else
        protected override DbConnection InternalCreateConnection()
        {
            return new System.Data.SQLite.SQLiteConnection(Store.ConnectionString);
        }
        #endif

        public override bool HasTable(string tableName)
        {
            return Convert.ToInt32(this.ExecuteScalar(String.Format("select count(*) from SQLITE_MASTER where type = 'table' and name = '{0}'", tableName))) != 0;
        }

		protected override SQLStoreCursor InternalOpenCursor(string tableName, System.Collections.Generic.List<string> columns, SQLIndex index, bool isUpdatable)
		{

			return
				new SQLiteStoreCursor
				(
					this,
					tableName,
					columns,
					index,
					isUpdatable
				);
		}
		
    }
}