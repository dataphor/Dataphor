/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESQLCONNECTION

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Store.MSSQL;
using Alphora.Dataphor.DAE.Store.SQLCE;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Diagnostics
{
    [TestFixture]
    public class SQLStoreTest
    {

        [Test]        
        public void SQLCEStoreTest()
        {
            SQLStore LSQLStore;
            LSQLStore = new SQLCEStore();
            LSQLStore.ConnectionString = @"Data Source=E:\Users\Luxspes\Documents\Visual Studio 2008\SqlCE\MyDatabase1.sdf";
            SQLStoreConnection LConnection = LSQLStore.Connect();
            string ATableName = "TableTest";
            List<string> AColumns = new List<string>();
            AColumns.Add("ID");
            AColumns.Add("NAME");
            SQLIndex AIndex = new SQLIndex("PK_TableTest", new[] { new SQLIndexColumn("ID") });

            //The table has 2 columns ID (integer) and NAME (nvarchar 50)

			#if USESQLCONNECTION
			LConnection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
			#else
            LConnection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            #endif

            SQLStoreCursor LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            var LRow = new object[] { 1, "Hi" };
            LCursor.Insert(LRow);
            LCursor.Dispose();

            LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            LRow = new object[] { 2, "Bye" };
            LCursor.Insert(LRow);
            LCursor.Dispose();

            LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            LCursor.SetRange(null, null);
            LCursor.Dispose();


            LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            LCursor.Next();
            LCursor.Dispose();


            LConnection.CommitTransaction();  
        }

        //Data Source=

        [Test]
        public void MSSQLStoreTest()
        {
            SQLStore LSQLStore;
            LSQLStore = new MSSQLStore();
            LSQLStore.ConnectionString = "Data Source=HUITZILOPOCHTLI;Initial Catalog=Tests;Integrated Security=True;";
            SQLStoreConnection LConnection = LSQLStore.Connect();
            string ATableName = "TableTest";
            List<string> AColumns = new List<string>();
            AColumns.Add("ID");
            AColumns.Add("NAME");
            SQLIndex AIndex = new SQLIndex("PK_TableTest", new[] { new SQLIndexColumn("ID") });

            //The table has 2 columns ID (integer) and NAME (nvarchar 50)

			#if USESQLCONNECTION
			LConnection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
			#else
            LConnection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            #endif

            SQLStoreCursor LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);            
            var LRow=new object[]{1,"Hi"};
            LCursor.Insert(LRow);
            LCursor.Dispose();

            LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            LRow = new object[] { 2, "Bye" };
            LCursor.Insert(LRow);
            LCursor.Dispose();
            
            LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            LCursor.SetRange(null, null);
            LCursor.Dispose();
            

            LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            LCursor.Next();
            LCursor.Dispose();

            SQLStoreCursor LOtherCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            LCursor = LConnection.OpenCursor(ATableName, AColumns, AIndex, true);
            
            LCursor.SetRange(null,null);
            
            
            LCursor.Dispose();
            LOtherCursor.Dispose();
            
            LConnection.CommitTransaction();                                   
        }
    }
}
