/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.Threading;

using NUnit.Framework;

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
		/// Disconnecting after the server has reclaimed the connection: the client has to be able to let go of a connection
		/// that is already gone, which is the state this whole mechanism produces once the pings stop arriving.
		/// </summary>
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
		private static LocalServer NewClient(RemoteServer remoteServer)
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
	}
}
