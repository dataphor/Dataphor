/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// An in-process server with the shipped client stack in front of it: a <see cref="LocalServer"/> over a
	/// <see cref="RemoteServer"/> over the engine. One server per fixture; one client session and process, and one
	/// engine-side session and process, per test.
	/// </summary>
	/// <remarks>
	/// This is what makes <see cref="RemoteServerCursor"/> and <see cref="LocalCursor"/> reachable from a test: both are the
	/// same objects a client over the wire talks to, because the protocol layer only marshals the calls they already make.
	/// Deliberately *not* <see cref="ProtocolClient"/>, which goes over CoreWCF and needs a hosted instance with a port -
	/// a defect above the transport does not need one, and <see cref="CoreWcfProtocolTest"/> covers the transport itself.
	///
	/// The engine-side session is the second half of the point: a fixture derived from this one can change the catalog, or
	/// read what the engine itself answers, while the client holds a cursor open.
	/// </remarks>
	public abstract class ClientStackTestFixture
	{
		/// <summary>The runtime layout's library folders copied for this fixture's server.</summary>
		protected virtual string[] Libraries => [];

		private TestServerInstance _instance;
		protected TestServerInstance Instance { get { return _instance; } }

		private RemoteServer _remoteServer;
		private LocalServer _localServer;

		/// <summary>The client's view of the server - a LocalServer, as a client program holds.</summary>
		protected IServer ClientServer { get { return _localServer; } }

		private IServerSession _clientSession;
		protected IServerSession ClientSession { get { return _clientSession; } }

		private IServerProcess _clientProcess;
		/// <summary>A process on the client stack: its cursors are LocalCursors over RemoteServerCursors.</summary>
		protected IServerProcess ClientProcess { get { return _clientProcess; } }

		private IServerSession _engineSession;
		protected IServerSession EngineSession { get { return _engineSession; } }

		private IServerProcess _engineProcess;
		/// <summary>A process on the engine itself, bypassing the client stack: its cursors are ServerCursors.</summary>
		protected IServerProcess EngineProcess { get { return _engineProcess; } }

		[OneTimeSetUp]
		public void SetUpFixture()
		{
			_instance = new TestServerInstance(GetType().Name, Libraries);
			_instance.Server.Start();
			_remoteServer = new RemoteServer(_instance.Server);
			// A keep-alive well under the server's five-minute idle threshold, but long enough that no test waits for a ping.
			_localServer = new LocalServer(_remoteServer, false, "localhost", 60);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			// Outermost first: the LocalServer's connection is registered with the RemoteServer, which holds the engine.
			if (_localServer != null)
			{
				_localServer.Dispose();
				_localServer = null;
			}
			if (_remoteServer != null)
			{
				_remoteServer.Dispose();
				_remoteServer = null;
			}
			if (_instance != null)
			{
				_instance.Dispose();
				_instance = null;
			}
		}

		[SetUp]
		public void SetUp()
		{
			_clientSession = _localServer.Connect(new SessionInfo("Admin", ""));
			_clientProcess = _clientSession.StartProcess(new ProcessInfo(_clientSession.SessionInfo));
			_engineSession = _instance.Connect();
			_engineProcess = _engineSession.StartProcess(new ProcessInfo(_engineSession.SessionInfo));
		}

		[TearDown]
		public void TearDown()
		{
			try
			{
				if (_engineProcess != null)
				{
					_engineSession.StopProcess(_engineProcess);
					_engineProcess = null;
				}
			}
			finally
			{
				try
				{
					if (_engineSession != null)
					{
						_instance.Server.Disconnect(_engineSession);
						_engineSession = null;
					}
				}
				finally
				{
					try
					{
						if (_clientProcess != null)
						{
							_clientSession.StopProcess(_clientProcess);
							_clientProcess = null;
						}
					}
					finally
					{
						if (_clientSession != null)
						{
							_localServer.Disconnect(_clientSession);
							_clientSession = null;
						}
					}
				}
			}
		}

		/// <summary>Runs D4 on the engine-side process, which is where a fixture sets its tables up.</summary>
		protected void ExecuteScript(string script)
		{
			_engineProcess.ExecuteScript(script);
		}
	}
}
