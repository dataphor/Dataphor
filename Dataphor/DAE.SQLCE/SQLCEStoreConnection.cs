using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store.SQLCE
{
    public class SQLCEStoreConnection : SQLStoreConnection
    {
        public SQLCEStoreConnection(SQLCEStore AStore) : base(AStore)
        { }
		
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

        public override bool HasTable(string ATableName)
        {
            return ((int)this.ExecuteScalar(String.Format("select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{0}'", ATableName)) != 0);
        }

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

        protected override SQLStoreCursor InternalOpenCursor(string ATableName, SQLIndex AIndex, bool AIsUpdatable)
        {
            return
                new SQLCEStoreCursor
                    (
                    this,
                    ATableName,
                    AIndex,
                    AIsUpdatable
                    );
        }
    }
}