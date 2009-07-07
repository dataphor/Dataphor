/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Abstract SQL Store
	
	Defines the expected behavior for a simple storage device that uses a SQL DBMS as it's backend.
	The store is capable of storing integers, strings, booleans, and long text and binary data.
	The store also manages logging and rollback of nested transactions to make up for the lack of savepoint support in the target DBMS.
*/

using System.Collections.Generic;
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
        
        public SQLCEStoreCursor(SQLCEStoreConnection AConnection, string ATableName, List<string> AColumns, SQLIndex AIndex, bool AIsUpdatable)
			: base(AConnection, ATableName, AColumns, AIndex, AIsUpdatable)
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
            FResultSet.ReadLast();
        }

        protected override bool InternalPrior()
        {
			return FResultSet.ReadPrevious();
        }

        protected override void InternalFirst()
        {
            FResultSet.ReadFirst();
        }

        protected override bool InternalSeek(object[] AKey)
        {
            EnsureReader(null, true, true);
			object[] LKey = new object[AKey.Length];
			for (int LIndex = 0; LIndex < LKey.Length; LIndex++)
				LKey[LIndex] = NativeToStoreValue(AKey[LIndex]);
			return FResultSet.Seek(DbSeekOptions.FirstEqual, LKey);
        }
		
        protected override object InternalGetValue(int AIndex)
        {
            return StoreToNativeValue(FResultSet.GetValue(AIndex)); 
        }
		
        protected override void InternalSetValue(int AIndex, object AValue)
        {
            FResultSet.SetValue(AIndex, NativeToStoreValue(AValue));
        }
		
        protected override bool InternalInsert(object[] ARow)
        {
            SqlCeUpdatableRecord LRecord = FResultSet.CreateRecord();
            for (int LIndex = 0; LIndex < ARow.Length; LIndex++)
                LRecord.SetValue(LIndex, NativeToStoreValue(ARow[LIndex]));

            FResultSet.Insert(LRecord, DbInsertOptions.KeepCurrentPosition);

            return false;
        }

        protected override bool InternalUpdate()
        {
            FResultSet.Update();

            return true;
        }

        protected override bool InternalDelete()
        {
            FResultSet.Delete();
            
            return true;
        }
    }
}