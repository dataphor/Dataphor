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
        public SQLiteStoreConnection(SQLiteStore AStore) : base(AStore) { }

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

        public override bool HasTable(string ATableName)
        {
            return Convert.ToInt32(this.ExecuteScalar(String.Format("select count(*) from SQLITE_MASTER where type = 'table' and name = '{0}'", ATableName))) != 0;
        }
    }
}