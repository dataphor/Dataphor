/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Language.D4;
using System.IO;

namespace Alphora.Dataphor.DAE.Diagnostics
{
	public enum CatalogStoreType { SQLCE, SQLite, MSSQL };
	
	public class ServerTestUtility
	{
		private static ServerTestUtility FInstance;

		public static ServerTestUtility Instance
		{
			get
			{
				if (FInstance == null) 
					FInstance = new ServerTestUtility();
				return FInstance;
			}
		}



		public ServerConfiguration GetTestConfiguration()
		{
			return GetTestConfiguration("TestInstance");
		}
		
		public ServerConfiguration GetTestConfiguration(string AInstanceName)
		{
			return GetTestConfiguration(AInstanceName, CatalogStoreType.SQLCE);
		}
		
		public ServerConfiguration GetTestConfiguration(string AInstanceName, CatalogStoreType ACatalogStoreType)
		{
			ServerConfiguration LTestConfiguration = new ServerConfiguration();
			LTestConfiguration.Name = AInstanceName;
			LTestConfiguration.LibraryDirectories = Path.Combine(Path.GetDirectoryName(PathUtility.GetInstallationDirectory()), "Libraries");
			LTestConfiguration.PortNumber = 8090;
			switch (ACatalogStoreType)
			{
				case CatalogStoreType.SQLite :
					LTestConfiguration.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite";
					LTestConfiguration.CatalogStoreConnectionString = @"Data Source=%CatalogPath%\DAECatalog";
				break;
				
				case CatalogStoreType.MSSQL :
					LTestConfiguration.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.MSSQL.MSSQLStore,Alphora.Dataphor.DAE.MSSQL";
					LTestConfiguration.CatalogStoreConnectionString = "Data Source=localhost;Initial Catalog=DAECatalog;Integrated Security=True";
				break;
			}
			
			return LTestConfiguration;
		}
		
		public void ResetInstance(ServerConfiguration ATestConfiguration)
		{
			ResetInstance(ATestConfiguration, CatalogStoreType.SQLCE);
		}
		
		public void ResetInstance(ServerConfiguration ATestConfiguration, CatalogStoreType ACatalogStoreType)
		{
			// Delete the instance directory
			string LInstanceDirectory = Path.Combine(Path.Combine(PathUtility.CommonAppDataPath(string.Empty, VersionModifier.None), Server.Server.CDefaultInstanceDirectory), ATestConfiguration.Name);
			if (Directory.Exists(LInstanceDirectory))
				Directory.Delete(LInstanceDirectory, true);

			// Delete the DAECatalog database if this is an MSSQL store regression test
			if (ACatalogStoreType == CatalogStoreType.MSSQL)
				ResetMSSQLCatalog(ATestConfiguration);
		}

		public void ResetMSSQLCatalog(ServerConfiguration ATestConfiguration)
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
		
		public Server.Server GetServer(ServerConfiguration AServerConfiguration)
		{
			Server.Server LServer = new Server.Server();
			AServerConfiguration.ApplyTo(LServer);
			return LServer;
		}
	}
}
