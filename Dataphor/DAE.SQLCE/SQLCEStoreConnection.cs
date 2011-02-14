/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Simple SQL CE Store Connection
*/

//#define SQLSTORETIMING
#define USESQLCONNECTION

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store.SQLCE
{
    public class SQLCEStoreConnection : SQLStoreConnection
    {
        public SQLCEStoreConnection(SQLCEStore store) : base(store)
        { }

		#if USESQLCONNECTION
		protected override SQLConnection InternalCreateConnection()
		{
			return new SQLCEConnection(Store.ConnectionString);
		}
		#else
        protected override DbConnection InternalCreateConnection()
        {
            return new SqlCeConnection(Store.ConnectionString);
        }

        protected override DbTransaction InternalBeginTransaction(System.Data.IsolationLevel AIsolationLevel)
        {
            // SQLServerCE does not support the ReadUncommitted isolation level because it uses versioning
            if (AIsolationLevel == System.Data.IsolationLevel.ReadUncommitted)
                AIsolationLevel = System.Data.IsolationLevel.ReadCommitted;
            return base.InternalBeginTransaction(AIsolationLevel);
        }
        #endif

        public override bool HasTable(string tableName)
        {
            return ((int)this.ExecuteScalar(String.Format("select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{0}'", tableName)) != 0);
        }
        
        #if USESQLCONNECTION
		public new SQLCECommand ExecuteCommand { get { return (SQLCECommand)base.ExecuteCommand; } }
		#else
        internal SqlCeResultSet ExecuteResultSet(string ATableName, string AIndexName, DbRangeOptions ARangeOptions, object[] AStartValues, object[] AEndValues, ResultSetOptions AResultSetOptions)
        {
            ExecuteCommand.CommandType = CommandType.TableDirect;
            ExecuteCommand.CommandText = ATableName;
            ExecuteCommand.IndexName = AIndexName;
            ExecuteCommand.SetRange(ARangeOptions, AStartValues, AEndValues);

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
			
	            return ExecuteCommand.ExecuteResultSet(AResultSetOptions);
			
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteResultSet", ATableName, AIndexName, AStartValues != null && AEndValues == null, AStartValues != null && AEndValues != null, (ResultSetOptions.Updatable & AResultSetOptions) != 0, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
        }
		
        public new SqlCeCommand ExecuteCommand { get { return (SqlCeCommand)base.ExecuteCommand; } }
		#endif
		
        protected override SQLStoreCursor InternalOpenCursor(string tableName, List<string> columns, SQLIndex index, bool isUpdatable)
        {
            return
                new SQLCEStoreCursor
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