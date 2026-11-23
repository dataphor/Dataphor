/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Threading;

using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>Stops the native CLI sessions of clients that have stopped calling.</summary>
	/// <remarks>
	/// <para>
	/// A native session's clock moves on every call that names its handle, so any call at all is the liveness signal:
	/// this interface hands out sessions rather than connections and has no connection object for a ping to land on, so
	/// the shipped client library <c>NativeCLISession</c> says "still here" by calling <c>GetTransactionCount</c> every
	/// two minutes. Stopping a session stops its process, which rolls back its open transactions - releasing, among
	/// other things, the catalog store's write lock, which an abandoned transaction that changed the catalog would
	/// otherwise hold against every other session's schema changes.
	/// </para>
	/// <para>
	/// A session with a call in flight on it is never stopped, which the main interface's <see cref="ConnectionManager"/>
	/// cannot promise of a connection: every native operation goes through one acquire, so the count of calls in flight
	/// is there for the sweep to read under the same lock it removes the session under. So, unlike a connection, a
	/// session is reclaimed for having no call on it rather than whatever it is doing - but a client that holds a session
	/// through idle time still needs its keep-alive, because holding it is not calling it.
	/// </para>
	/// <para>
	/// The watcher wakes every quarter of the idle threshold, so a session is stopped between the threshold and the
	/// threshold plus a quarter of it after its last call: 300 to 375 seconds at the default. A client that comes back to
	/// a stopped session finds its handle invalid, the same "Invalid session handle" error any handle this server does
	/// not hold produces; nothing remembers that a session was reclaimed, and the server's log is where a reclamation is
	/// visible (see "Reclaiming an abandoned session" in docs/modernization-plan.md for why).
	/// </para>
	/// </remarks>
	public class NativeSessionManager : IDisposable
	{
		// NOTE: the thread shape below - the signal, the background watcher, the wake period of a quarter of the
		// threshold, the idempotent Dispose and its bounded join - is ConnectionManager's, copied rather than shared, and
		// so are the remarks that explain it. Deliberate: port-abandoned-session-cleanup and test-client-server-abandoned
		// have only just settled and tested that mechanism, and extracting a shared watcher abstraction for tidiness now
		// would rework a freshly-proven path for no behaviour. If a third such watcher ever appears, extract one then.

		// NOTE: no instance-configuration setting for this, and the same number as ConnectionManager.IdleTimeInSeconds
		// without sharing its constant. Two constants because their floors are set by different clients - the
		// connection's by the WinForms client's compiled-in keep-alive interval, this one's by the native client
		// library's (NativeCLISession.DefaultKeepAliveInterval, 120 seconds) - and because a test has to be able to
		// shorten one without touching the other. Whether *either* should become something an operator can set in the
		// instance list is one question with one answer, recorded at ConnectionManager.IdleTimeInSeconds: not until a
		// deployment reports the problem, and then as an additive [DefaultValue] property on ServerConfiguration with a
		// validated floor, a user-guide section and a migration note.
		/// <summary>The default idle threshold: a native session nothing has called for this long is stopped.</summary>
		public const int IdleTimeInSeconds = 300; // 5 minutes

		/// <summary>How long <see cref="Dispose"/> waits for the watcher thread to finish a sweep it is in the middle of.</summary>
		public const int WatcherStopTimeoutInSeconds = 10;

		/// <summary>Reclaims sessions that have had no call on them for <see cref="IdleTimeInSeconds"/>.</summary>
		public NativeSessionManager(NativeServer nativeServer) : this(nativeServer, IdleTimeInSeconds) { }

		/// <summary>Reclaims sessions that have had no call on them for <paramref name="idleTimeInSeconds"/>.</summary>
		/// <remarks>
		/// The threshold must stay well above the keep-alive interval of the clients that connect, or a live client's
		/// session is reclaimed between two of its pings. A test that cannot wait five minutes for a sweep sets its own.
		/// </remarks>
		public NativeSessionManager(NativeServer nativeServer, int idleTimeInSeconds)
		{
			if (idleTimeInSeconds < 1)
				throw new ArgumentOutOfRangeException(nameof(idleTimeInSeconds), idleTimeInSeconds, "The idle threshold must be at least 1 second.");

			_nativeServer = nativeServer;
			_idleTimeInSeconds = idleTimeInSeconds;
			// A quarter of the threshold, with a floor of a second: see the bound in the class remarks. The cost of the
			// shorter period is four wakeups an hour rather than one, each one pass over the session table under the
			// server's session lock.
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

		private NativeServer _nativeServer;
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
					Error.Fail("Native session manager started more than once.");
				_lifetimeSignal = new ManualResetEvent(false);

				// NOTE: a background thread so that it can never hold the process open. It is signalled by Dispose, which
				// DataphorServiceHost.Stop calls - but Stop can return without reaching that call (a stage of it throws),
				// and the host program then exits on the failure; a foreground thread waiting here would keep the process
				// alive after Main returned. Nothing is lost by letting the runtime drop it at process exit: it only waits
				// on an event and reclaims idle sessions. It is also what makes the bounded wait in StopLifetimeWatcher
				// safe: a wait that gives up cannot cost the process its exit.
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
						_nativeServer.ReclaimIdleSessions(_idleTimeInSeconds);
				}

				// The sweeping is complete.  Clean up...
				lock (_lifetimeSyncHandle)
				{
					_lifetimeSignal.Close();
					_lifetimeSignal = null;
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled, the framework will terminate the server. ReclaimIdleSessions
				// guards its own logging and its own per-session failures, so reaching here means something else went
				// wrong and this watcher is over for the life of the process.
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
			// Joined at all so that a sweep in flight cannot still be running while the engine it stops sessions on is torn
			// down. DataphorServiceHost.Stop calls Dispose before it disposes the RemoteServer and stops the engine - that
			// ordering is what makes this wait sufficient. Bounded because a sweep can be waiting on a process that will
			// not stop (see the note in NativeServer.ReclaimIdleSessions).
			if ((watcher == null) || watcher.Join(TimeSpan.FromSeconds(WatcherStopTimeoutInSeconds)))
				return;

			try
			{
				// Qualified because the enclosing namespace Alphora.Dataphor.DAE wins over the type of the same name.
				Alphora.Dataphor.DAE.Server.Server server = _nativeServer.Server as Alphora.Dataphor.DAE.Server.Server;
				if (server != null)
					server.LogMessage(String.Format("The idle native session watcher did not stop within {0} seconds; it is a background thread and is dropped at process exit.", WatcherStopTimeoutInSeconds));
			}
			catch
			{
				// Reporting a late watcher must not fail the shutdown: DataphorServiceHost.Stop calls Dispose unguarded and
				// disposes the server and the engine afterwards.
			}
		}
	}
}
