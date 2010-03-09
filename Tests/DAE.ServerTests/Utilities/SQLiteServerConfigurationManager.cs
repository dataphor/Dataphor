using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.DAE.ServerTests.Utilities
{
	class SQLiteServerConfigurationManager : ServerConfigurationManager
	{
		private ServerConfiguration FTestConfiguration;
		
		public ServerConfiguration GetTestConfiguration(string AInstanceName)
		{
			FTestConfiguration = new ServerConfiguration();
			FTestConfiguration.Name = AInstanceName;
			FTestConfiguration.LibraryDirectories = Path.Combine(Path.GetDirectoryName(PathUtility.GetInstallationDirectory()), "Libraries");
			FTestConfiguration.PortNumber = 8061;
			FTestConfiguration.SecurePortNumber = 8601;
			
			FTestConfiguration.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite";
			FTestConfiguration.CatalogStoreConnectionString = @"Data Source=%CatalogPath%\DAECatalog";		

			return FTestConfiguration;
		}

		public void ResetInstance()
		{
			// Delete the instance directory
			string LInstanceDirectory = Path.Combine(Path.Combine(PathUtility.CommonAppDataPath(string.Empty, VersionModifier.None), Server.Server.CDefaultInstanceDirectory), FTestConfiguration.Name);
			if (Directory.Exists(LInstanceDirectory))
				Directory.Delete(LInstanceDirectory, true);
		}

		public Server.Server GetServer()
		{
			Server.Server LServer = new Server.Server();
			FTestConfiguration.ApplyTo(LServer);
			return LServer;
		}
	}
}
