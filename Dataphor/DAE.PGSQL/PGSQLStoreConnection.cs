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
using Alphora.Dataphor.DAE.Connection.PGSQL;

#else
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
#endif

namespace Alphora.Dataphor.DAE.Store.PGSQL
{
    public class PostgreSQLStoreConnection : SQLStoreConnection
    {
        public PostgreSQLStoreConnection(PostgreSQLStore AStore) : base(AStore) { }

		#if USESQLCONNECTION
		protected override SQLConnection InternalCreateConnection()
		{
			return new PostgreSQLConnection(Store.ConnectionString);
		}
		#else
        protected override DbConnection InternalCreateConnection()
        {
            return new SqlConnection(Store.ConnectionString);
        }
        #endif

        public override bool HasTable(string ATableName)
        {
            string LStatement = String.Format("select count(*)>0 from PG_TABLES where TABLENAME = '{0}'", ATableName.ToLower());
            bool LTableExists = (bool) this.ExecuteScalar(LStatement);
            return LTableExists;           
        }

		public override object NativeToLiteralValue(object AValue)
		{
			if (AValue is bool)
				return ((bool)AValue ? "'true'" : "'false'");			
			return base.NativeToLiteralValue(AValue);
		}
    }
}