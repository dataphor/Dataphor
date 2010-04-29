/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Server.Tests.Utilities
{
	using Alphora.Dataphor.Windows;
	using Alphora.Dataphor.DAE.Server;
	
	public abstract class ServerConfigurationManager
	{
		private ServerConfiguration FTestConfiguration;

		public string InstanceName { get { return FTestConfiguration.Name; } }
		
		protected void CheckTestConfiguration()
		{
			if (FTestConfiguration == null)
				throw new Exception("Test configuration has not been established.");
		}

		public virtual ServerConfiguration GetTestConfiguration(string AInstanceName)
		{
			FTestConfiguration = new ServerConfiguration();
			FTestConfiguration.Name = AInstanceName;
			var LDirectoryName = AppDomain.CurrentDomain.BaseDirectory;
			if (Path.GetFileName(LDirectoryName) != "Tests")
				LDirectoryName = PathUtility.GetInstallationDirectory();
			FTestConfiguration.LibraryDirectories = Path.Combine(Path.GetDirectoryName(LDirectoryName), "Libraries");
			FTestConfiguration.PortNumber = 8061;			
			FTestConfiguration.SecurePortNumber = 8601;

			return FTestConfiguration;
		}

		public virtual void ResetInstance()
		{
			CheckTestConfiguration();
			
			// Delete the instance directory
			string LInstanceDirectory = Path.Combine(Path.Combine(PathUtility.CommonAppDataPath(string.Empty, VersionModifier.None), Server.CDefaultInstanceDirectory), FTestConfiguration.Name);
			if (Directory.Exists(LInstanceDirectory))
				Directory.Delete(LInstanceDirectory, true);
		}

		public Server GetServer()
		{
			CheckTestConfiguration();
			
			Server LServer = new Server();
			FTestConfiguration.ApplyTo(LServer);
			return LServer;
		}
	}
}
