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

#if USESQLCONNECTION
using Alphora.Dataphor.DAE.Connection;
#else
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
#endif

namespace Alphora.Dataphor.DAE.Store.MSSQL
{
    public class MSSQLStoreConnection : SQLStoreConnection
    {
        public MSSQLStoreConnection(MSSQLStore store) : base(store) { }

		#if USESQLCONNECTION
		protected override SQLConnection InternalCreateConnection()
		{
			return new MSSQLConnection(Store.ConnectionString);
		}
		#else
        protected override DbConnection InternalCreateConnection()
        {
            return new SqlConnection(Store.ConnectionString);
        }
        #endif

        public override bool HasTable(string tableName)
        {
            return ((int)this.ExecuteScalar(String.Format("select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{0}'", tableName)) != 0);
        }

		protected override SQLStoreCursor InternalOpenCursor(string tableName, List<string> columns, SQLIndex index, bool isUpdatable)
		{
			return
				new MSSQLStoreCursor
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