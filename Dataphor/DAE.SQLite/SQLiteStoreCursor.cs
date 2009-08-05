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
        public SQLiteStoreCursor(SQLiteStoreConnection AConnection, string ATableName, SQLIndex AIndex, bool AIsUpdatable) 
            : base(AConnection, ATableName, AIndex, AIsUpdatable)
        { 
            
        }

		public SQLiteStoreCursor(SQLiteStoreConnection AConnection, string ATableName, List<string> AColumns, SQLIndex AIndex, bool AIsUpdatable)
			: base(AConnection, ATableName, AColumns, AIndex, AIsUpdatable)
        {	
			
        }

		public override object NativeToStoreValue(object AValue)
		{
			if (AValue is bool)
				return (byte)((bool)AValue ? 1 : 0);
			return base.NativeToStoreValue(AValue);
		}

		public override object StoreToNativeValue(object AValue)
		{
			if (AValue is byte)
				return ((byte)AValue) == 1;
			return base.StoreToNativeValue(AValue);
		}
        
        
    }
}