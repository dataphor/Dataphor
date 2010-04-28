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

	public class SQLCEServerConfigurationManager: ServerConfigurationManager
	{
		private ServerConfiguration FTestConfiguration;

		public ServerConfiguration GetTestConfiguration(string AInstanceName)
		{
			FTestConfiguration = new ServerConfiguration();
			FTestConfiguration.Name = AInstanceName;
			FTestConfiguration.LibraryDirectories = Path.Combine(Path.GetDirectoryName(PathUtility.GetInstallationDirectory()), "Libraries");
			FTestConfiguration.PortNumber = 8061;			
			FTestConfiguration.SecurePortNumber = 8601;

			return FTestConfiguration;
		}

		public void ResetInstance()
		{
			// Delete the instance directory
			string LInstanceDirectory = Path.Combine(Path.Combine(PathUtility.CommonAppDataPath(string.Empty, VersionModifier.None), Server.CDefaultInstanceDirectory), FTestConfiguration.Name);
			if (Directory.Exists(LInstanceDirectory))
				Directory.Delete(LInstanceDirectory, true);
		}

		public Server GetServer()
		{
			Server LServer = new Server();
			FTestConfiguration.ApplyTo(LServer);
			return LServer;
		}
	}
}
