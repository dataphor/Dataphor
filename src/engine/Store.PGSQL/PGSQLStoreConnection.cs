/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Connection.PGSQL;

namespace Alphora.Dataphor.DAE.Store.PGSQL
{
    public class PostgreSQLStoreConnection : SQLStoreConnection
    {
        public PostgreSQLStoreConnection(PostgreSQLStore store) : base(store) { }

		// 3.1 carried a second form of this method behind #if USESQLCONNECTION, a constant this file defined at the top of
		// itself and never undefined: the other form returned a System.Data.SqlClient SqlConnection - a SQL Server connection
		// from the PostgreSQL store - and was one #define away from compiling. The constant and the dead form are both gone,
		// so the only server this store reaches is the one the PostgreSQL connection class connects to.
		protected override SQLConnection InternalCreateConnection()
		{
			return new PostgreSQLConnection(Store.ConnectionString);
		}

		// Lower-cased because PostgreSQL folds an unquoted identifier to lower case, and every table this store creates - the
		// catalog's own, from SystemStoreCatalog.sql - is created unquoted, so pg_tables holds the folded name. Invariant rather
		// than current culture, which is what 3.1 had: the server's folding is not the current culture's, and on a Turkish or
		// Azeri machine ToLower() turns the I of DAEServerInfo into a dotless i, so the catalog store would report that it has
		// no catalog and seed itself again over the one it already wrote.
		public override bool HasTable(string tableName)
		{
			string statement = String.Format("select count(*)>0 from PG_TABLES where TABLENAME = '{0}'", tableName.ToLowerInvariant());
			bool tableExists = (bool)this.ExecuteScalar(statement);
			return tableExists;
		}

		public override object NativeToLiteralValue(object tempValue)
		{
			if (tempValue is bool)
				return ((bool)tempValue ? "'true'" : "'false'");			
			return base.NativeToLiteralValue(tempValue);
		}

		// This store's catalog declares its boolean columns boolean, not tinyint, so a parameter carries the bool itself; the base
		// class sends 1 or 0, which is what the integer columns the other stores declare need.
		public override SQLParameter NativeToParameter(string name, object tempValue)
		{
			if (tempValue is bool)
				return new SQLParameter(name, new SQLBooleanType(), tempValue);
			return base.NativeToParameter(name, tempValue);
		}
    }
}