/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESQLCONNECTION

using System;
using System.Collections.Generic;


using Alphora.Dataphor.DAE.Connection;


namespace Alphora.Dataphor.DAE.Store.SQLite
{
    public class SQLiteStoreCursor : SQLStoreCursor
    {
        public SQLiteStoreCursor(SQLiteStoreConnection connection, string tableName, SQLIndex index, bool isUpdatable) 
            : base(connection, tableName, index, isUpdatable)
        { 
            
        }

		public SQLiteStoreCursor(SQLiteStoreConnection connection, string tableName, List<string> columns, SQLIndex index, bool isUpdatable)
			: base(connection, tableName, columns, index, isUpdatable)
        {	
			
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