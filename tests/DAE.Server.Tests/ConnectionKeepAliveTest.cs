/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.Threading;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What keeps a client's connection from being reclaimed by the server: the client's keep-alive ping. The server
	/// measures one clock per connection and moves it only when a ping arrives, so a connection nothing pings is one the
	/// server relinquishes - and a connection can be held with no session open on it.
	/// </summary>
	/// <remarks>
	/// The lowest layer that shows it: a LocalServer - the client half the product ships - over an in-process RemoteServer.
	/// No WCF host, no port, no network. The server's idle watcher (ConnectionManager) is not in the path either; it reads
	/// the same clock these cases read, and what it does once the pings stop is port-abandoned-session-cleanup's subject.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: LocalServer starts an engine of its own, and two engines in one process share the engine's
	// static state (see the note on TestServerInstance).
	public class ConnectionKeepAliveTest
	{
		/// <summary>The keep-alive interval these cases run the client at; the shipped default is two minutes.</summary>
		private const int KeepAliveIntervalSeconds = 1;

		/// <summary>How long a case waits for the clock to move: several intervals, and still seconds rather than minutes.</summary>
		private static readonly TimeSpan PingWait = TimeSpan.FromSeconds(20);

		/// <summary>
		/// A connection with no session open is pinged, and so is one whose last session has been disconnected. Both are
		/// windows that a program holding a LocalServer across idle time sits in: ServerConnection in DAE.Client builds the
		/// LocalServer once and hands sessions out of it.
		/// </summary>
		[Test]
		public void TheConnectionIsPingedWithNoSessionOpen()
		{
			WithAServer
			(
				remoteServer =>
				{
					LocalServer localServer = NewClient(remoteServer);
					try
					{
						// The server's own clock, read from the object the server keeps it on; both the class and the property
						// are public, so nothing here reaches into internals.
						RemoteServerConnection connection = (RemoteServerConnection)localServer.ServerConnection;

						WaitForTheClockToMovePast(connection, connection.LastActivityTime, "No ping reached a connection with no session open.");

						// And again once a session has come and gone: the keep-alive belongs to the connection, not to a session.
						localServer.Disconnect(localServer.Connect(new SessionInfo("Admin", "")));
						WaitForTheClockToMovePast(connection, connection.LastActivityTime, "No ping reached the connection after its last session was disconnected.");
					}
					finally
					{
						localServer.Dispose();
					}
				}
			);
		}

		/// <summary>
		/// A ping that does not land must not end the keep-alive: the client rebuilds its channel after a communication
		/// failure, so a blip of a few seconds leaves the server still holding the connection, and a keep-alive that had
		/// stopped would then let the server reclaim it - the loss the ping exists to prevent. Through 3.1 the ping was per
		/// session and the next session started a new one, so a client recovered from this by accident; with one ping per
		/// connection there is no accident left to rely on.
		/// </summary>
		[Test]
		public void AFailedPingDoesNotEndTheKeepAlive()
		{
			WithAServer
			(
				remoteServer =>
				{
					ServerWithAFailingPing server = new ServerWithAFailingPing(remoteServer, 1);
					LocalServer localServer = NewClient(server);
					try
					{
						RemoteServerConnection connection = (RemoteServerConnection)server.Connection.Inner;

						WaitForTheClockToMovePast(connection, connection.LastActivityTime, "No ping reached the connection after one had failed.");

						// Pings are sequential and only a successful one moves the clock, so the clock moving proves the first
						// ping - the one that threw - was survived. Asserted anyway: without it the case would also pass on a
						// connection that never failed a ping at all.
						Assert.That(server.Connection.FailedPings, Is.EqualTo(1), "The case proved nothing: no ping failed, so the keep-alive was never asked to survive one.");
					}
					finally
					{
						localServer.Dispose();
					}
				}
			);
		}

		/// <summary>
		/// Disconnecting after the server has reclaimed the connection: the client has to be able to let go of a connection
		/// that is already gone, which is the state this whole mechanism produces once the pings stop arriving.
		/// </summary>
		/// <remarks>
		/// NOTE: this case reached the client's own guard only while the server's half was not idempotent, and that has
		/// changed: since port-abandoned-session-cleanup, RemoteServer.Relinquish tolerates a connection it has already
		/// released, so the in-process path here no longer exercises the guard and only the wired path - where a second
		/// release comes back as the service's unknown-object-handle fault - still needs it. The case is kept because it
		/// still asserts the client's contract; read a pass with that in mind.
		/// </remarks>
		[Test]
		public void DisposingAfterTheServerHasReclaimedTheConnectionDoesNotThrow()
		{
			WithAServer
			(
				remoteServer =>
				{
					LocalServer localServer = NewClient(remoteServer);

					// Behind the client's back, as the server's idle watcher does it.
					remoteServer.Relinquish(localServer.ServerConnection);

					Assert.DoesNotThrow(() => localServer.Dispose(), "A client could not disconnect from a server that had already reclaimed its connection.");
				}
			);
		}

		/// <summary>Runs <paramref name="body"/> against the remote CLI of a fresh in-process server, and stops both afterwards.</summary>
		private static void WithAServer(Action<RemoteServer> body)
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(ConnectionKeepAliveTest)))
			{
				instance.Server.Start();
				using (RemoteServer remoteServer = new RemoteServer(instance.Server))
					body(remoteServer);
			}
		}

		/// <summary>The client half the product ships, pinging fast enough for a test to watch it.</summary>
		private static LocalServer NewClient(IRemoteServer remoteServer)
		{
			LocalServer result = null;
			// The counter is handed back because LocalServer starts an engine of its own for its catalog cache. The third
			// argument is the client's own host name, which the server records against the connection; it is not a target.
			TestEnvironment.WithTheObjectIDCounterRestored(() => result = new LocalServer(remoteServer, false, "localhost", KeepAliveIntervalSeconds));
			return result;
		}

		private static void WaitForTheClockToMovePast(RemoteServerConnection connection, DateTime previous, string message)
		{
			Stopwatch waited = Stopwatch.StartNew();
			while (connection.LastActivityTime <= previous && waited.Elapsed < PingWait)
				Thread.Sleep(100);

			Assert.That
			(
				connection.LastActivityTime,
				Is.GreaterThan(previous),
				String.Format("{0} The server's LastActivityTime was still {1:O} after {2:0.0} s, with the client pinging every {3} s.", message, previous, waited.Elapsed.TotalSeconds, KeepAliveIntervalSeconds)
			);
		}

		/// <summary>The product's own server with one seam in it: the connection it hands out refuses its first pings.</summary>
		/// <remarks>
		/// A decorator, not a stand-in - every call but the ping is the product's own code - because the client offers no
		/// hook for a failed ping, and faulting a real channel would need the WCF host this fixture deliberately does
		/// without. The failure injected is the one a network blip produces: ClientConnection.Ping turns a
		/// CommunicationException into exactly this.
		/// </remarks>
		private sealed class ServerWithAFailingPing : IRemoteServer
		{
			public ServerWithAFailingPing(IRemoteServer inner, int pingsToFail)
			{
				_inner = inner;
				_pingsToFail = pingsToFail;
			}

			private readonly IRemoteServer _inner;
			private readonly int _pingsToFail;

			/// <summary>The connection the client established; null until it has.</summary>
			public ConnectionWithAFailingPing Connection { get; private set; }

			public IRemoteServerConnection Establish(string connectionName, string hostName)
			{
				Connection = new ConnectionWithAFailingPing(_inner.Establish(connectionName, hostName), _pingsToFail);
				return Connection;
			}

			public void Relinquish(IRemoteServerConnection connection)
			{
				// Unwrapped: RemoteServer.Relinquish only knows its own connections.
				_inner.Relinquish(((ConnectionWithAFailingPing)connection).Inner);
			}

			event EventHandler IDisposableNotify.Disposed { add { } remove { } }
			public string Name { get { return _inner.Name; } }
			public void Start() { _inner.Start(); }
			public void Stop() { _inner.Stop(); }
			public ServerState State { get { return _inner.State; } }
			public long CacheTimeStamp { get { return _inner.CacheTimeStamp; } }
			public long DerivationTimeStamp { get { return _inner.DerivationTimeStamp; } }
		}

		/// <summary>A connection that throws on its first <paramref name="pingsToFail"/> pings and then behaves.</summary>
		private sealed class ConnectionWithAFailingPing : IRemoteServerConnection
		{
			public ConnectionWithAFailingPing(IRemoteServerConnection inner, int pingsToFail)
			{
				Inner = inner;
				_pingsToFail = pingsToFail;
			}

			/// <summary>The product's own connection; the test reads the server's clock off it.</summary>
			public IRemoteServerConnection Inner { get; private set; }

			// Touched only by the client's keep-alive thread, and read from the test thread through Volatile.Read.
			private int _pingsToFail;
			private int _failedPings;

			/// <summary>How many pings this connection has refused.</summary>
			public int FailedPings { get { return Volatile.Read(ref _failedPings); } }

			public void Ping()
			{
				if (_pingsToFail > 0)
				{
					_pingsToFail--;
					Interlocked.Increment(ref _failedPings);
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, new Exception("A simulated communication failure."));
				}
				Inner.Ping();
			}

			event EventHandler IDisposableNotify.Disposed { add { } remove { } }
			public string ConnectionName { get { return Inner.ConnectionName; } }
			public IRemoteServerSession Connect(SessionInfo sessionInfo) { return Inner.Connect(sessionInfo); }
			public void Disconnect(IRemoteServerSession session) { Inner.Disconnect(session); }
		}
	}
}
