/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESQLCONNECTION

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.SqlServerCe;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Store;
	using Alphora.Dataphor.DAE.Store.MSSQL;
	using Alphora.Dataphor.DAE.Store.SQLCE;

    [TestFixture]
    public class SQLStoreTest
    {
		private void SQLStoreReadAfterUpdateTest(SQLStore ASQLStore)
		{
			using (SQLStoreConnection LConnection = ASQLStore.Connect())
			{
				List<string> LColumns = new List<string>() { "ID", "Name" };
				SQLIndex LIndex = new SQLIndex("PK_Test", new[] { new SQLIndexColumn("ID") });
				
				if (LConnection.HasTable("Test"))
					LConnection.ExecuteStatement("drop table Test");
				if (!LConnection.HasTable("Test"))
					LConnection.ExecuteStatement("create table Test ( ID int not null, Name nvarchar(20), constraint PK_Test primary key ( ID ) )");
				
				LConnection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				try
				{
					using (SQLStoreCursor LCursor = LConnection.OpenCursor("Test", LColumns, LIndex, true))
					{
						LCursor.Insert(new object[] { 1, "Joe" });
						LCursor.Insert(new object[] { 2, "Martha" });
						//LCursor.Insert(new object[] { 3, "Clair" });
					}
					LConnection.CommitTransaction();
				}
				catch
				{
					LConnection.RollbackTransaction();
					throw;
				}
				
				LConnection.BeginTransaction(SQLIsolationLevel.ReadCommitted);
				try
				{
					using (SQLStoreCursor LCursor = LConnection.OpenCursor("Test", LColumns, LIndex, true))
					{
						if (!LCursor.Next())
							throw new Exception("Expected row");
							
						if ((string)LCursor[1] != "Joe")
							throw new Exception("Excepted Joe row");
							
						LCursor[1] = "Joes";
						LCursor.Update();

						LCursor.SetRange(null, null);
						if (!LCursor.Next())
							throw new Exception("Expected row");
						
						if ((string)LCursor[1] != "Joes")
							throw new Exception(String.Format("Expected Joes row, found '{0}'.", (string)LCursor[1]));
							
						LCursor[1] = "Joe";
						LCursor.Update();
					}
					
					LConnection.CommitTransaction();
				}
				catch
				{
					LConnection.RollbackTransaction();
					throw;
				}
			}
		}
		
		[Test]
		public void SQLCEReadAfterUpdateTest()
		{
            SQLStore LSQLStore = new SQLCEStore();
			LSQLStore.ConnectionString = @"Data Source=TestDatabase.sdf";
			LSQLStore.Initialize();
			SQLStoreReadAfterUpdateTest(LSQLStore);
		}		
		
		[Test]
		public void SqlCeReadAfterUpdateTest()
		{
			SqlCeEngine LEngine = new SqlCeEngine(@"Data Source=TestDatabase.sdf");
			if (!File.Exists("TestDatabase.sdf"))
				LEngine.CreateDatabase();
				
			using (SqlCeConnection LConnection = new SqlCeConnection("Data Source=TestDatabase.sdf"))
			{
				LConnection.Open();
				using (SqlCeCommand LCommand = LConnection.CreateCommand())
				{
					LCommand.CommandText = "select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Test'";
					if ((int)LCommand.ExecuteScalar() != 0)
					{
						LCommand.CommandText = "drop table Test";
						LCommand.ExecuteNonQuery();
					}
					
					LCommand.CommandText = "create table Test ( ID int not null, Name nvarchar(20), constraint PK_Test primary key ( ID ) )";
					LCommand.ExecuteNonQuery();
					
					LCommand.CommandText = "insert into Test ( ID, Name ) values ( 1, 'Joe' )";
					LCommand.ExecuteNonQuery();
				}
				
				using (SqlCeTransaction LTransaction = LConnection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
				{
					try
					{
						using (SqlCeCommand LCommand = LConnection.CreateCommand())
						{
							LCommand.CommandType = System.Data.CommandType.TableDirect;
							LCommand.CommandText = "Test";
							LCommand.IndexName = "PK_Test";
							LCommand.SetRange(DbRangeOptions.Default, null, null);
							
							using (SqlCeResultSet LResultSet = LCommand.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Sensitive | ResultSetOptions.Updatable))
							{
								if (!LResultSet.Read())
									throw new Exception("Expected row");
									
								if ((string)LResultSet[1] != "Joe")
									throw new Exception("Expected Joe row");
									
								LResultSet.SetValue(1, "Joes");
								LResultSet.Update();
								
								LResultSet.ReadFirst();
								
								//if (!LResultSet.Read())
								//	throw new Exception("Expected row");
									
								if ((string)LResultSet[1] != "Joes")
									throw new Exception("Expected Joes row");
									
								LResultSet.SetValue(1, "Joe");
								LResultSet.Update();
							}
						}
						
						LTransaction.Commit(CommitMode.Immediate);
					}
					catch
					{
						LTransaction.Rollback();
						throw;
					}
				}
				
				using (SqlCeTransaction LTransaction = LConnection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
				{
				}
			}
		}
		
        [Test]        
        public void SQLCEStoreTest()
        {
            SQLStore LSQLStore = new SQLCEStore();
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
