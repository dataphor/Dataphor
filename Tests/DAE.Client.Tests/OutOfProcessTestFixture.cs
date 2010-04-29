/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Client.Tests
{
	using Alphora.Dataphor.Windows;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Server.Tests.Utilities;
	using Alphora.Dataphor.DAE.Client;
	using Alphora.Dataphor.DAE.Runtime.Data;

	public class OutOfProcessTestFixture
	{
		private ServerConfigurationManager FConfigurationManager;
		private ServerConfiguration FConfiguration;
		private Process FProcess;

		private DataSession FDataSession;
		protected DataSession DataSession { get { return FDataSession; } }

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			InstanceManager.Load();

			FConfigurationManager = new SQLCEServerConfigurationManager();
			FConfiguration = FConfigurationManager.GetTestConfiguration("TestOOPInstance");
			
			if (InstanceManager.Instances.HasInstance("TestOOPInstance"))
				InstanceManager.Instances.Remove("TestOOPInstance");
			
			InstanceManager.Instances.Add("TestOOPInstance", FConfiguration);
			InstanceManager.Save();
			
			ProcessStartInfo LProcessStartInfo = new ProcessStartInfo();
			LProcessStartInfo.FileName = Path.Combine(ServerConfigurationManager.GetInstallationDirectory(), "Dataphor\\bin\\DAEServer.exe");
			LProcessStartInfo.WorkingDirectory = Path.GetDirectoryName(LProcessStartInfo.FileName);
			LProcessStartInfo.Arguments = "-n \"TestOOPInstance\"";
			LProcessStartInfo.UseShellExecute = false;
			LProcessStartInfo.RedirectStandardInput = true;
			FProcess = Process.Start(LProcessStartInfo);
			
			// TODO: This should be a wait for the process, but WaitForInputIdle only works on GUI apps
			//Thread.Sleep(10000);
			
			ConnectionAlias LAlias = new ConnectionAlias();
			LAlias.Name = "TestOOPInstanceConnection";
			LAlias.InstanceName = "TestOOPInstance";
			
			int LRetryCount = 0;
			while ((FDataSession == null) && (LRetryCount++ < 3))
			{
				Thread.Sleep(500);
				try
				{
					FDataSession = new DataSession();
					FDataSession.Alias = LAlias;
					FDataSession.Open();
				}
				catch
				{
					FDataSession = null;
				}
			}
		}
		
		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			if (FDataSession != null)
			{
				// Close the data session
				FDataSession.Close();
			}
			
			// Send to stop the server
			FProcess.StandardInput.WriteLine();
			
			// Wait for the process
			FProcess.WaitForExit(30000);
			
			if (!FProcess.HasExited)
				FProcess.Kill();
				
			// Wait a little bit longer
			FProcess.WaitForExit(5000);
			
			// Try to reset the instance
			FConfigurationManager.ResetInstance();
		}
	}
}
