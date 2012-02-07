/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace Alphora.Dataphor.DAE.Server.Tests.Utilities
{
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.Windows;

	class MSSQLServerConfigurationManager: ServerConfigurationManager
	{
		public override ServerConfiguration GetTestConfiguration(string AInstanceName)
		{
			var LResult = base.GetTestConfiguration(AInstanceName);
			
			LResult.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.MSSQL.MSSQLStore,Alphora.Dataphor.DAE.MSSQL";
			LResult.CatalogStoreConnectionString = "Data Source=localhost;Initial Catalog=DAECatalog;Integrated Security=True";

			return LResult;
		}

		public override void ResetInstance()
		{
			base.ResetInstance();

			// Delete the DAECatalog database if this is an MSSQL store regression test
			ResetMSSQLCatalog();
		}
		
		public static void ResetDatabase(string ADatabaseName)
		{
			using (SqlConnection LConnection = new SqlConnection("Data Source=localhost;Initial Catalog=master;Integrated Security=True"))
			{
				LConnection.Open();
				using (SqlCommand LCommand = LConnection.CreateCommand())
				{
					LCommand.CommandType = CommandType.Text;
					LCommand.CommandText = String.Format("if exists (select * from sysdatabases where name = '{0}') drop database {0}", ADatabaseName);
					LCommand.ExecuteNonQuery();
				}
			}
		}

		private void ResetMSSQLCatalog()
		{
			DbConnectionStringBuilder LBuilder = new DbConnectionStringBuilder();
			LBuilder.ConnectionString = TestConfiguration.CatalogStoreConnectionString;

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

				using (SqlConnection LConnection = new SqlConnection(LBuilder.ConnectionString))
				{
					LConnection.Open();
					using (SqlCommand LCommand = LConnection.CreateCommand())
					{
						LCommand.CommandType = CommandType.Text;
						LCommand.CommandText = String.Format("if exists (select * from sysdatabases where name = '{0}') drop database {0}", LDatabaseName);
						LCommand.ExecuteNonQuery();
					}
				}
			}
		}
	}
}
