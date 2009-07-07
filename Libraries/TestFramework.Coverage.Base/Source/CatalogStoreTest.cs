using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;
using Alphora.Dataphor.DAE.Server;
using System.Data.Common;
using Alphora.Dataphor.DAE.Language.D4;
using System.Data.SqlClient;
using System.Data;

/*
create table SQLCETiming from
	GetStoreCounters()
		adorn 
		{ 
			Operation tags { Storage.Length = '200' },
			TableName tags { Storage.Length = '8000' },
			IndexName tags { Storage.Length = '8000' }
		};

create table SQLiteTiming from
	GetStoreCounters()
		adorn 
		{ 
			Operation tags { Storage.Length = '200' },
			TableName tags { Storage.Length = '8000' },
			IndexName tags { Storage.Length = '8000' }
		};

create table MSSQLTiming from
	GetStoreCounters()
		adorn 
		{ 
			Operation tags { Storage.Length = '200' },
			TableName tags { Storage.Length = '8000' },
			IndexName tags { Storage.Length = '8000' }
		};

select 
	(SQLCETiming group by { Operation } add { Count() SQLCECount, Sum(Duration) SQLCESumDuration, Avg(Duration) SQLCEAvgDuration })
		right join (SQLiteTiming group by { Operation } add { Count() SQLiteCount, Sum(Duration) SQLiteSumDuration, Avg(Duration) SQLiteAvgDuration })
		right join (MSSQLTiming group by { Operation } add { Count() MSSQLCount, Sum(Duration) MSSQLSumDuration, Avg(Duration) MSSQLAvgDuration })
*/

namespace Alphora.Dataphor.DAE.Diagnostics
{
	[TestFixture]
	public class CatalogStoreTest
	{
		private void ResetMSSQLCatalog(ServerConfiguration ATestConfiguration)
		{
			DbConnectionStringBuilder LBuilder = new DbConnectionStringBuilder();
			LBuilder.ConnectionString = ATestConfiguration.CatalogStoreConnectionString;
			
			string LDatabaseName = null;
			if (LBuilder.ContainsKey("Initial Catalog"))
			{
				LDatabaseName = (string)LBuilder["Initial Catalog"];
				LBuilder["Initial Catalog"] = "master";
			}
			else if (LBuilder.ContainsKey("Database"))
			{
				LDatabaseName = (string)LBuilder["Database"];
				LBuilder["Database"] = "master";
			}
			
			if (!String.IsNullOrEmpty(LDatabaseName))
			{
				if (!Parser.IsValidIdentifier(LDatabaseName))
					throw new ArgumentException("Database name specified in store connection string is not a valid identifier.");
					
				SqlConnection LConnection = new SqlConnection(LBuilder.ConnectionString);
				LConnection.Open();
				SqlCommand LCommand = LConnection.CreateCommand();
				LCommand.CommandType = CommandType.Text;
				LCommand.CommandText = String.Format("if exists (select * from sysdatabases where name = '{0}') drop database {0}", LDatabaseName);
				LCommand.ExecuteNonQuery();
			}
		}

		private ServerConfiguration GetTestConfiguration(string ACatalogStoreType)
		{
			ServerConfiguration LTestConfiguration = new ServerConfiguration();
			LTestConfiguration.Name = "TestInstance";
			LTestConfiguration.LibraryDirectories = PathUtility.GetInstallationDirectory();
			LTestConfiguration.PortNumber = 8090;
			switch (ACatalogStoreType)
			{
				case "SQLite" :
					LTestConfiguration.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite";
					LTestConfiguration.CatalogStoreConnectionString = @"Data Source=%CatalogPath%\DAECatalog";
				break;
				
				case "MSSQL" :
					LTestConfiguration.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.MSSQL.MSSQLStore,Alphora.Dataphor.DAE.MSSQL";
					LTestConfiguration.CatalogStoreConnectionString = "Data Source=localhost;Initial Catalog=DAECatalog;Integrated Security=True";
				break;
			}
			
			return LTestConfiguration;
		}
		
		private void CatalogRegressionTest(string ACatalogStoreType)
		{
			// Create a test configuration
			ServerConfiguration LTestConfiguration = GetTestConfiguration(ACatalogStoreType);

			// Delete the instance directory
			string LInstanceDirectory = Path.Combine(Path.Combine(PathUtility.CommonAppDataPath(string.Empty, VersionModifier.None), Server.Server.CDefaultInstanceDirectory), LTestConfiguration.Name);
			if (Directory.Exists(LInstanceDirectory))
				Directory.Delete(LInstanceDirectory, true);

			// Delete the StoreRegressionCatalog database if this is an MSSQL store regression test
			if (ACatalogStoreType == "MSSQL")
				ResetMSSQLCatalog(LTestConfiguration);
				
			// Start a server based on the StoreRegression instance
			Server.Server LServer = new Server.Server();
			LTestConfiguration.ApplyTo(LServer);
			LServer.Start();
			
			// Stop the server
			LServer.Stop();
			
			// Start the server
			LServer.Start();
			
			// Stop the server
			LServer.Stop();
			
			// Delete the instance directory
			Directory.Delete(LInstanceDirectory, true);
			
			// Delete the StoreRegressionCatalog database if this is an MSSQL store regression test
			if (ACatalogStoreType == "MSSQL")
				ResetMSSQLCatalog(LTestConfiguration);
		}
		
		[Test]
		public void CatalogRegressionTest()
		{
			CatalogRegressionTest("SQLCE");
			CatalogRegressionTest("SQLite");
			CatalogRegressionTest("MSSQL");
		}
	}
}
