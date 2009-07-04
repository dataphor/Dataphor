using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Alphora.Dataphor.DAE.Store.MSSQL
{
    public class MSSQLStoreConnection : SQLStoreConnection
    {
        public MSSQLStoreConnection(MSSQLStore AStore) : base(AStore) { }

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