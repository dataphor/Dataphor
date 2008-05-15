using System;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.DAE.Connection;
using System.Data.Common;

namespace Alphora.Dataphor.DAE.Store.SQLite
{
	public class SQLiteStore : SQLStore
	{
		protected override void InternalInitialize()
		{
			// Nothing to do, database creation can be accomplished in the connection string.
		}

		public override SQLConnection GetSQLConnection()
		{
			return new SQLiteConnection(ConnectionString);
		}

		protected override SQLStoreConnection InternalConnect()
		{
			return new SQLiteStoreConnection(this);
		}
	}
	
	public class SQLiteStoreConnection : SQLStoreConnection
	{
		public SQLiteStoreConnection(SQLiteStore AStore) : base(AStore) { }

		protected override DbConnection InternalCreateConnection()
		{
			return new System.Data.SQLite.SQLiteConnection(Store.ConnectionString);
		}

		public override bool HasTable(string ATableName)
		{
			return Convert.ToInt32(this.ExecuteScalar(String.Format("select count(*) from SQLITE_MASTER where type = 'table' and name = '{0}'", ATableName))) != 0;
		}
	}
}
