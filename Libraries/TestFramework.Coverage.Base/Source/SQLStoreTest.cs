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
            LSQLStore.ConnectionString = @"Data Source=E:\Users\Luxspes\Documents\Visual Studio 2008\SqlCE\MyDatabase#1.sdf";
            SQLStoreConnection LConnection = LSQLStore.Connect();
            string ATableName = "TableTest";
            SQLIndex AIndex = new SQLIndex("PK_TableTest", new[] { new SQLIndexColumn("ID") });
            bool AisUpdatable = true;
            SQLStoreCursor LCursor = LConnection.OpenCursor(ATableName, AIndex, AisUpdatable);
            //The table has 2 columns ID (integer) and NAME (nvarchar 50)
            var LRow = new object[] { 1, "Hi" };
            LCursor.Insert(LRow);
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
            SQLIndex AIndex = new SQLIndex("PK_TableTest", new[] { new SQLIndexColumn("ID") });
            bool AisUpdatable = true;
            SQLStoreCursor LCursor = LConnection.OpenCursor(ATableName, AIndex, AisUpdatable);
            //The table has 2 columns ID (integer) and NAME (nvarchar 50)
            var LRow=new object[]{1,"Hi"};
            LCursor.Insert(LRow);
        }
    }
}
