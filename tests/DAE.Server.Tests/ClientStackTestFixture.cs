/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The shipped client stack in front of the base fixture's in-process server: a <see cref="LocalServer"/> over a
	/// <see cref="RemoteServer"/> over the engine. One of each per fixture, and a client session and process per test
	/// beside the engine-side session and process the base fixture gives.
	/// </summary>
	/// <remarks>
	/// This is what makes <see cref="RemoteServerCursor"/> and <see cref="LocalCursor"/> reachable from a test: both are the
	/// same objects a client over the wire talks to, because the protocol layer only marshals the calls they already make.
	/// Deliberately *not* <see cref="ProtocolClient"/>, which goes over CoreWCF and needs a hosted instance with a port -
	/// a defect above the transport does not need one, and <see cref="CoreWcfProtocolTest"/> covers the transport itself.
	///
	/// The engine side is the second half of the point: a test can change the catalog, or read what the engine itself
	/// answers, while the client holds a cursor open. It is the base fixture's own session and process, which
	/// <see cref="EngineSession"/> and <see cref="EngineProcess"/> name for what they are - in a fixture with two of
	/// everything, a bare Session or Process does not say which stack it is on - and the inherited ExecuteScript runs
	/// there, which is where a fixture sets its tables up.
	/// </remarks>
	// Not [Parallelizable]: the base fixture starts a server and LocalServer starts an engine of its own, so every test
	// here runs two engines in one process, and two engines share the engine's static state (see the note on
	// TestServerInstance).
	public abstract class ClientStackTestFixture : InProcessTestFixture
	{
		private RemoteServer _remoteServer;
		private LocalServer _localServer;

		/// <summary>The client's view of the server - a LocalServer, as a client program holds.</summary>
		protected IServer ClientServer { get { return _localServer; } }

		private IServerSession _clientSession;
		protected IServerSession ClientSession { get { return _clientSession; } }

		private IServerProcess _clientProcess;
		/// <summary>A process on the client stack: its cursors are LocalCursors over RemoteServerCursors.</summary>
		protected IServerProcess ClientProcess { get { return _clientProcess; } }

		/// <summary>The base fixture's session, named for the side of the stack it is on.</summary>
		protected IServerSession EngineSession { get { return Session; } }

		/// <summary>A process on the engine itself, bypassing the client stack: its cursors are ServerCursors.</summary>
		protected IServerProcess EngineProcess { get { return Process; } }

		// Named apart from the base fixture's SetUpFixture and SetUp, rather than overriding them: NUnit runs a base
		// class's setup before a derived class's and its teardown after, which is the order this stack needs - the
		// server before the client over it, and the client torn down before the server - but a method that repeats a
		// base method's name replaces it instead of running beside it.
		[OneTimeSetUp]
		public void SetUpClientStack()
		{
			_remoteServer = new RemoteServer(Instance.Server);
			// A keep-alive well under the server's five-minute idle threshold, but long enough that no test waits for a ping.
			_localServer = new LocalServer(_remoteServer, false, "localhost", 60);
		}

		[OneTimeTearDown]
		public void TearDownClientStack()
		{
			// Outermost first: the LocalServer's connection is registered with the RemoteServer, which holds the engine
			// the base fixture disposes after this. Nested finally blocks, so that one failing step still runs the rest.
			try
			{
				if (_localServer != null)
				{
					_localServer.Dispose();
					_localServer = null;
				}
			}
			finally
			{
				if (_remoteServer != null)
				{
					_remoteServer.Dispose();
					_remoteServer = null;
				}
			}
		}

		[SetUp]
		public void SetUpClientSession()
		{
			_clientSession = _localServer.Connect(new SessionInfo("Admin", ""));
			_clientProcess = _clientSession.StartProcess(new ProcessInfo(_clientSession.SessionInfo));
		}

		[TearDown]
		public void TearDownClientSession()
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
