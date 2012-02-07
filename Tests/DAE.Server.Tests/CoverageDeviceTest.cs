/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Server.Tests.Utilities;

	[TestFixture]
	public class CoverageDeviceTest
	{
		private ServerConfigurationManager FServerConfigurationManager;
		private ServerConfiguration FConfiguration;
		private IServer FServer;
		private IServerSession FSession;
		private IServerProcess FProcess;
		
		[TestFixtureSetUp]
		public void SetUpFixture()
		{
			MSSQLServerConfigurationManager.ResetDatabase("DataphorTests");
			
			FServerConfigurationManager = new SQLCEServerConfigurationManager();
			FConfiguration = FServerConfigurationManager.GetTestConfiguration("TestInstance");
			FServerConfigurationManager.ResetInstance();
			FServer = FServerConfigurationManager.GetServer();
			FServer.Start();
			
			IServerSession LSession = FServer.Connect(new SessionInfo("Admin", ""));
			try
			{
				IServerProcess LProcess = LSession.StartProcess(new ProcessInfo(LSession.SessionInfo));
				try
				{
					LProcess.ExecuteScript("EnsureLibraryRegistered('Frontend');");
					LProcess.ExecuteScript("EnsureLibraryRegistered('TestFramework');");
					LProcess.ExecuteScript("EnsureLibraryRegistered('TestFramework.Coverage.Base');");
					LProcess.ExecuteScript("EnsureLibraryRegistered('TestFramework.Coverage.Devices');");
				}
				finally
				{
					LSession.StopProcess(LProcess);
				}
			}
			finally
			{
				FServer.Disconnect(LSession);
			}
		}
		
		[TestFixtureTearDown]
		public void TearDownFixture()
		{
			FServer.Stop();
			FServerConfigurationManager.ResetInstance();
		}
		
		[SetUp]
		public void SetUp()
		{
			FSession = FServer.Connect(new SessionInfo("Admin", "", "TestFramework.Coverage.Devices"));
			FProcess = FSession.StartProcess(new ProcessInfo(FSession.SessionInfo));
		}
		
		[TearDown]
		public void TearDown()
		{
			if (FProcess != null)
			{
				FSession.StopProcess(FProcess);
				FProcess = null;
			}
			
			if (FSession != null)
			{
				FServer.Disconnect(FSession);
				FSession = null;
			}
		}
		
		private void ExecuteScript(string ALibraryName, string AScriptName)
		{
			FProcess.ExecuteScript(String.Format("ExecuteScript('{0}', '{1}');", ALibraryName, AScriptName));
		}
		
		[Test]
		public void TestMSSQLDevice()
		{
			ExecuteScript("TestFramework.Coverage.Devices", "TestMSSQLDevice");
		}

		[Test]
		public void Run()
		{
			
		}
	}
}
