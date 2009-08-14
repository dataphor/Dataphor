
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.ServerTests.Utilities
{
	class SQLCEServerConfigurationManager: ServerConfigurationManager
	{
		ServerConfiguration FTestConfiguration;

		public ServerConfiguration GetTestConfiguration(string AInstanceName)
		{
			FTestConfiguration = new ServerConfiguration();
			FTestConfiguration.Name = AInstanceName;
			FTestConfiguration.LibraryDirectories = Path.Combine(Path.GetDirectoryName(PathUtility.GetInstallationDirectory()), "Libraries");
			FTestConfiguration.PortNumber = 8090;			

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
