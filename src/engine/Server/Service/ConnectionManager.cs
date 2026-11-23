/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Server;
using System.Threading;

namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>Relinquishes the connections of clients that have stopped saying they are still there.</summary>
	/// <remarks>
	/// <para>
	/// A connection's clock (<see cref="RemoteServerConnection.LastActivityTime"/>) moves only when the client pings it, so
	/// the ping is the liveness signal and nothing else is. Two consequences, both deliberate: a client that has not pinged
	/// for the threshold is relinquished whatever it is doing, and a client in the middle of a long call keeps its
	/// connection only because it pings from a thread of its own, which the shipped client does (every
	/// LocalServer.DefaultKeepAliveInterval seconds, two minutes). Relinquishing a connection closes its sessions, which
	/// aborts each process's executing thread and rolls back its open transactions - releasing, among other things, the
	/// catalog store's write lock, which an abandoned transaction that changed the catalog would otherwise hold against
	/// every other session's schema changes.
	/// </para>
	/// <para>
	/// The watcher wakes every quarter of the idle threshold, so a connection is relinquished between the threshold and the
	/// threshold plus a quarter of it after its last ping: 300 to 375 seconds at the default. Through 3.1 the wake period
	/// and the threshold were one constant, which made that bound 300 to 600 seconds.
	/// </para>
	/// </remarks>
	public class ConnectionManager : IDisposable
	{
		// NOTE: no instance-configuration setting for this, deliberately. It is 3.1's value, and its usable range is narrow
		// in a way that is easy to get wrong: the floor is the keep-alive interval of the shipped client
		// (LocalServer.DefaultKeepAliveInterval, 120 seconds, a constant compiled into the client), so an operator who set
		// 60 would quietly start killing live sessions. The case that actually needed a shorter threshold - a deterministic
		// test - is served by the second constructor instead. Revisit if a deployment reports schema changes blocked for
		// minutes by vanished clients: the step then is an additive [DefaultValue] property on ServerConfiguration with a
		// validated floor above the client's keep-alive interval, plus the user-guide section and a migration note.
		// The native CLI's idle threshold is the same number in a constant of its own, NativeSessionManager.IdleTimeInSeconds,
		// whose note points here: the two are separate because their floors are set by different clients' keep-alive
		// intervals, but a decision to make either an operator setting is this one decision and covers both.
		/// <summary>The default idle threshold: a connection nothing has pinged for this long is relinquished.</summary>
		public const int IdleTimeInSeconds = 300; // 5 minutes

		/// <summary>How long <see cref="Dispose"/> waits for the watcher thread to finish a sweep it is in the middle of.</summary>
		public const int WatcherStopTimeoutInSeconds = 10;

		/// <summary>Reaps connections that have not been pinged for <see cref="IdleTimeInSeconds"/>.</summary>
		public ConnectionManager(RemoteServer remoteServer) : this(remoteServer, IdleTimeInSeconds) { }

		/// <summary>Reaps connections that have not been pinged for <paramref name="idleTimeInSeconds"/>.</summary>
		/// <remarks>
		/// The threshold must stay well above the keep-alive interval of the clients that connect, or a live client is
		/// reclaimed between two of its pings. A test that cannot wait five minutes for a sweep sets its own.
		/// </remarks>
		public ConnectionManager(RemoteServer remoteServer, int idleTimeInSeconds)
		{
			if (idleTimeInSeconds < 1)
				throw new ArgumentOutOfRangeException(nameof(idleTimeInSeconds), idleTimeInSeconds, "The idle threshold must be at least 1 second.");

			_remoteServer = remoteServer;
			_idleTimeInSeconds = idleTimeInSeconds;
			// A quarter of the threshold, with a floor of a second: see the bound in the class remarks. The cost of the
			// shorter period is four wakeups an hour rather than one, each an array copy under the server's lock and one
			// DateTime comparison per connection.
			_wakePeriodInSeconds = Math.Max(1, idleTimeInSeconds / 4);
			StartLifetimeWatcher();
		}


		#region IDisposable Members

		private bool _disposed;

		/// <summary>Stops the watcher, waiting <see cref="WatcherStopTimeoutInSeconds"/> for a sweep in flight.</summary>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;

			StopLifetimeWatcher();
		}

		#endregion

		private RemoteServer _remoteServer;
		private readonly int _idleTimeInSeconds;
		private readonly int _wakePeriodInSeconds;

		private ManualResetEvent _lifetimeSignal;
		private Thread _lifetimeWatcher;
		private object _lifetimeSyncHandle = new object();

		private void StartLifetimeWatcher()
		{
			lock (_lifetimeSyncHandle)
			{
				if (_lifetimeSignal != null)
					Error.Fail("Lifetime manager started more than once.");
				_lifetimeSignal = new ManualResetEvent(false);

				// NOTE: a background thread so that it can never hold the process open. It is signalled by Dispose, which
				// DataphorService.Dispose calls, which DataphorServiceHost.Stop calls - but Stop can return without reaching
				// that call (a stage of it throws), and the host program then exits on the failure; a foreground thread
				// waiting here would keep the process alive after Main returned. Nothing is lost by letting the runtime drop
				// it at process exit: it only waits on an event and reaps idle connections. It is also what makes the bounded
				// wait in StopLifetimeWatcher safe: a wait that gives up cannot cost the process its exit.
				_lifetimeWatcher = new Thread(new ThreadStart(LifetimeWatcher));
				_lifetimeWatcher.IsBackground = true;
				_lifetimeWatcher.Start();
			}
		}

		private void LifetimeWatcher()
		{
			try
			{
				bool signaled = false;
				while (!signaled)
				{
					// Wait for either a signal or a time-out
					signaled = _lifetimeSignal.WaitOne(_wakePeriodInSeconds * 1000);

					if (!signaled)
						RelinquishIdleConnections(DateTime.Now.AddSeconds(-_idleTimeInSeconds));
				}

				// The keep alive processing is complete.  Clean up...
				lock (_lifetimeSyncHandle)
				{
					_lifetimeSignal.Close();
					_lifetimeSignal = null;
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled, the framework will terminate the server.
			}
		}

		/// <summary>Relinquishes every connection whose last ping is older than <paramref name="oldestActivityTime"/>.</summary>
		/// <remarks>
		/// Not public: nothing outside needs to force a sweep, and a test drives the real watcher through a short threshold
		/// instead.
		/// </remarks>
		private void RelinquishIdleConnections(DateTime oldestActivityTime)
		{
			// Read once, and nothing below reaches for it again: the RemoteServer's Server is null from the moment it has
			// been disposed, so a sweep that read it a second time - as the logging here used to - raised a
			// NullReferenceException inside its own error handling, which the catch in LifetimeWatcher swallowed, silently
			// killing the watcher thread. Qualified because the enclosing namespace Alphora.Dataphor.DAE.Server wins over
			// the type of the same name.
			Alphora.Dataphor.DAE.Server.Server server = _remoteServer.Server;
			if (server == null)
				return;

			// NOTE: the sweep is serial, and relinquishing a connection whose process will not stop when it is aborted waits
			// half of Engine.ProcessTerminationTimeout (15 s by default) for that process, delaying every later connection's
			// reclamation by that much. If that is ever seen, relinquish each connection on a task of its own rather than
			// shortening the wait, which is what gives an aborted process the chance to unwind.
			RemoteServerConnection[] connections = _remoteServer.GetCurrentConnections();
			for (int index = 0; index < connections.Length; index++)
				if (connections[index].LastActivityTime < oldestActivityTime)
				{
					try
					{
						// Connection has been idle for longer than the activity time, kill it
						server.LogMessage(String.Format("Connection relinquished due to inactivity: ConnectionName={0}, HostName={1}, LastActivity={2}", connections[index].ConnectionName, connections[index].HostName, connections[index].LastActivityTime.ToShortTimeString()));
						_remoteServer.Relinquish(connections[index]);
					}
					catch (Exception exception)
					{
						try
						{
							server.LogError(exception);
						}
						catch
						{
							// Guarded because the sweep's own error reporting is the one thing here that must not end the
							// watcher: logging writes to the engine's log file, which the server closes when it stops, so a
							// sweep overlapping a stop can fail here - and an exception out of this catch reaches the one in
							// LifetimeWatcher, which ends the thread for the life of the process. Dropping one log line is
							// the cheaper loss. Reading Server once above covers the RemoteServer's teardown; this covers
							// the engine's.
						}
					}
				}
		}

		private void StopLifetimeWatcher()
		{
			Thread watcher;
			lock (_lifetimeSyncHandle)
			{
				if (_lifetimeSignal != null)
					_lifetimeSignal.Set();
				watcher = _lifetimeWatcher;
				_lifetimeWatcher = null;
			}

			// Joined outside the lock: the watcher takes the same lock on its way out, to close the signal.
			//
			// Joined at all so that a sweep in flight cannot still be running while the server it reads is torn down.
			// DataphorService.Dispose calls Dispose, and DataphorServiceHost.Stop calls that before it disposes the
			// RemoteServer and stops the engine - that ordering is what makes this wait sufficient. Bounded because a sweep
			// can be waiting on a process that will not stop (see the note in RelinquishIdleConnections).
			if ((watcher == null) || watcher.Join(TimeSpan.FromSeconds(WatcherStopTimeoutInSeconds)))
				return;

			try
			{
				Alphora.Dataphor.DAE.Server.Server server = _remoteServer.Server;
				if (server != null)
					server.LogMessage(String.Format("The idle connection watcher did not stop within {0} seconds; it is a background thread and is dropped at process exit.", WatcherStopTimeoutInSeconds));
			}
			catch
			{
				// Reporting a late watcher must not fail the shutdown: DataphorServiceHost.Stop calls Dispose unguarded and
				// disposes the server and the engine afterwards.
			}
		}
	}
}
