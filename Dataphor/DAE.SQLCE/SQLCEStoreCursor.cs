using System.Data.SqlServerCe;
using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store.SQLCE
{
    public class SQLCEStoreCursor : SQLStoreCursor
    {
        public SQLCEStoreCursor(SQLCEStoreConnection AConnection, string ATableName, SQLIndex AIndex, bool AIsUpdatable) 
            : base(AConnection, ATableName, AIndex, AIsUpdatable)
        { 
            EnsureReader(null, true, true);
        }

        protected override System.Data.Common.DbDataReader InternalCreateReader(object[] AOrigin, bool AForward, bool AInclusive)
        {
            FResultSet =
                Connection.ExecuteResultSet
                    (
                    TableName,
                    IndexName,
                    DbRangeOptions.Default,
                    null,
                    null,
                    ResultSetOptions.Scrollable | ResultSetOptions.Sensitive | (IsUpdatable ? ResultSetOptions.Updatable : ResultSetOptions.None)
                    );
				
            return FResultSet;
        }

        protected override void InternalDispose()
        {
            // This is the same reference as FReader in the base, so no need to dispose it, just clear it.
            FResultSet = null;
			
            base.InternalDispose();
        }
		
        private SqlCeResultSet FResultSet;
		
        public new SQLCEStoreConnection Connection { get { return (SQLCEStoreConnection)base.Connection; } }
		
        protected override bool InternalNext()
        {
            return FResultSet.Read();
        }

        protected override void InternalLast()
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
#endif
			
            FResultSet.ReadLast();

#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Last", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
#endif
        }

        protected override bool InternalPrior()
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
#endif
            return FResultSet.ReadPrevious();
#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Prior", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
#endif
        }

        protected override void InternalFirst()
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
#endif

            FResultSet.ReadFirst();

#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("First", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
#endif
        }

        protected override bool InternalSeek(object[] AKey)
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
#endif
            EnsureReader(null, true, true);
            object[] LKey = new object[AKey.Length];
            for (int LIndex = 0; LIndex < LKey.Length; LIndex++)
                LKey[LIndex] = NativeToStoreValue(AKey[LIndex]);
            return FResultSet.Seek(DbSeekOptions.FirstEqual, LKey);
#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Seek", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
#endif
        }
		
        protected override object InternalGetValue(int AIndex)
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
#endif
            return StoreToNativeValue(FResultSet.GetValue(AIndex)); 
#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("GetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
#endif
        }
		
        protected override void InternalSetValue(int AIndex, object AValue)
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
#endif

            FResultSet.SetValue(AIndex, NativeToStoreValue(AValue));

#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("SetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
#endif
        }
		
        protected override void InternalInsert(object[] ARow)
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
#endif

            SqlCeUpdatableRecord LRecord = FResultSet.CreateRecord();
            for (int LIndex = 0; LIndex < ARow.Length; LIndex++)
                LRecord.SetValue(LIndex, NativeToStoreValue(ARow[LIndex]));

#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("CreateRecord", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			LStartTicks = TimingUtility.CurrentTicks;
#endif

            FResultSet.Insert(LRecord, DbInsertOptions.KeepCurrentPosition);

#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Insert", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
#endif
        }

        protected override void InternalUpdate()
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
#endif

            FResultSet.Update();

#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Update", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
#endif
        }

        protected override void InternalDelete()
        {
#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
#endif

            FResultSet.Delete();

#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Delete", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
#endif
        }
    }
}