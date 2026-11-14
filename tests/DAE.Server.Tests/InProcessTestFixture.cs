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
	/// <summary>
	/// One in-process server per fixture, on a TestServerInstance, and one session and process per test.
	/// </summary>
	public abstract class InProcessTestFixture
	{
		/// <summary>The runtime layout's library folders copied for this fixture's server.</summary>
		protected virtual string[] Libraries => [];

		/// <summary>Libraries registered once the server has started; each must be in Libraries, with the libraries it requires.</summary>
		protected virtual string[] LibrariesToRegister => [];

		private TestServerInstance FInstance;
		protected TestServerInstance Instance { get { return FInstance; } }

		protected IServer Server { get { return FInstance.Server; } }

		private IServerSession FSession;
		protected IServerSession Session { get { return FSession; } }

		private IServerProcess FProcess;
		protected IServerProcess Process { get { return FProcess; } }

		[OneTimeSetUp]
		public void SetUpFixture()
		{
			FInstance = new TestServerInstance(GetType().Name, Libraries);
			FInstance.Server.Start();

			foreach (string LLibrary in LibrariesToRegister)
				FInstance.ExecuteScript(String.Format("EnsureLibraryRegistered('{0}');", LLibrary));
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			if (FInstance != null)
			{
				FInstance.Dispose();
				FInstance = null;
			}
		}

		[SetUp]
		public void SetUp()
		{
			FSession = FInstance.Connect();
			FProcess = FSession.StartProcess(new ProcessInfo(FSession.SessionInfo));
		}

		[TearDown]
		public void TearDown()
		{
			try
			{
				if (FProcess != null)
				{
					FSession.StopProcess(FProcess);
					FProcess = null;
				}
			}
			finally
			{
				if (FSession != null)
				{
					Server.Disconnect(FSession);
					FSession = null;
				}
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
