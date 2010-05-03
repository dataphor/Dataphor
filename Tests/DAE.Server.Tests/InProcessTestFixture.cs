/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	using Alphora.Dataphor.DAE.Server.Tests.Utilities;
	
	public abstract class InProcessTestFixture
	{
		private ServerConfigurationManager FServerConfigurationManager;
		protected ServerConfigurationManager ServerConfigurationManager { get { return FServerConfigurationManager; } }

		private ServerConfiguration FConfiguration;
		protected ServerConfiguration Configuration { get { return FConfiguration; } }

		private IServer FServer;
		protected IServer Server { get { return FServer; } }
		
		private IServerSession FSession;
		protected IServerSession Session { get { return FSession; } }
		
		private IServerProcess FProcess;
		protected IServerProcess Process { get { return FProcess; } }
		
		[TestFixtureSetUp]
		public void SetUpFixture()
		{
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
					LProcess.ExecuteScript("EnsureLibraryRegistered('TestFramework.Coverage.Base');");
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
			FSession = FServer.Connect(new SessionInfo("Admin", ""));
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
		
		protected void ExecuteScript(string AScript)
		{
			FProcess.ExecuteScript(AScript);
		}
		
		protected void ExecuteScript(string ALibraryName, string AScriptName)
		{
			FProcess.ExecuteScript(String.Format("ExecuteScript('{0}', '{1}');", ALibraryName, AScriptName));
		}
	}
}
