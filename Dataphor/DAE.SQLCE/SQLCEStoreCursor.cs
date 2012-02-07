/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESQLCONNECTION

using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store.SQLCE
{
    public class SQLCEStoreCursor : SQLStoreCursor
    {
        public SQLCEStoreCursor(SQLCEStoreConnection connection, string tableName, SQLIndex index, bool isUpdatable) 
            : base(connection, tableName, index, isUpdatable)
        { 
            EnsureReader(null, true, true);
        }
        
        public SQLCEStoreCursor(SQLCEStoreConnection connection, string tableName, List<string> columns, SQLIndex index, bool isUpdatable)
			: base(connection, tableName, columns, index, isUpdatable)
        {	
			EnsureReader(null, true, true);
        }
        
        public new SQLCEStoreConnection Connection { get { return (SQLCEStoreConnection)base.Connection; } }

		#if USESQLCONNECTION
		protected override SQLCursor InternalCreateReader(object[] origin, bool forward, bool inclusive)
		{
			SQLCECursor cursor =
				Connection.ExecuteCommand.ExecuteResultSet
				(
					TableName, 
					IndexName, 
					DbRangeOptions.Default, 
					null, 
					null, 
					ResultSetOptions.Scrollable | ResultSetOptions.Sensitive | (IsUpdatable ? ResultSetOptions.Updatable : ResultSetOptions.None)
				);

			_resultSet = cursor.ResultSet;
			return cursor;
		}
		#else
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
        #endif

        protected override void InternalDispose()
        {
            // This is the same reference as FReader in the base, so no need to dispose it, just clear it.
            _resultSet = null;
			
            base.InternalDispose();
        }
		
        private SqlCeResultSet _resultSet;
		
        protected override bool InternalNext()
        {
            return _resultSet.Read();
        }

        protected override bool InternalLast()
        {
            return _resultSet.ReadLast();
        }

        protected override bool InternalPrior()
        {
			return _resultSet.ReadPrevious();
        }

        protected override bool InternalFirst()
        {
            return _resultSet.ReadFirst();
        }

        protected override bool InternalSeek(object[] key)
        {
            EnsureReader(null, true, true);
			object[] localKey = new object[key.Length];
			for (int index = 0; index < localKey.Length; index++)
				localKey[index] = NativeToStoreValue(key[index]);
			_resultSet.Seek(DbSeekOptions.FirstEqual, localKey);
			// Despite the fact that the documentation says that the
			// result value of the Seek operation indicates whether or
			// not the cursor is positioned on a row, this does not
			// seem to be the case. (As of 3.5 sp1)
			return false;
        }
		
        protected override object InternalGetValue(int index)
        {
            return StoreToNativeValue(_resultSet.GetValue(index)); 
        }
		
        protected override void InternalSetValue(int index, object tempValue)
        {
            _resultSet.SetValue(index, NativeToStoreValue(tempValue));
        }
		
        protected override bool InternalInsert(object[] row)
        {
            SqlCeUpdatableRecord record = _resultSet.CreateRecord();
            for (int index = 0; index < row.Length; index++)
                record.SetValue(index, NativeToStoreValue(row[index]));

            _resultSet.Insert(record, DbInsertOptions.KeepCurrentPosition);

            return false;
        }

        protected override bool InternalUpdate()
        {
            _resultSet.Update();

            return true;
        }

        protected override bool InternalDelete()
        {
            _resultSet.Delete();
            
            return true;
        }

		public override object NativeToStoreValue(object tempValue)
		{
			if (tempValue is bool)
				return (byte)((bool)tempValue ? 1 : 0);
			return base.NativeToStoreValue(tempValue);
		}

		public override object StoreToNativeValue(object tempValue)
		{
			if (tempValue is byte)
				return ((byte)tempValue) == 1;			
			return base.StoreToNativeValue(tempValue);
		}
    }
}