/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public class NativeCLISession : IDisposable
	{
		/// <summary>Starts a session on the given instance, pinged every <see cref="DefaultKeepAliveInterval"/> seconds.</summary>
		public NativeCLISession(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode, NativeSessionInfo nativeSessionInfo)
			: this(hostName, instanceName, overridePortNumber, securityMode, overrideListenerPortNumber, listenerSecurityMode, nativeSessionInfo, DefaultKeepAliveInterval) { }

		/// <summary>Starts a session on the given instance, pinged every <paramref name="keepAliveIntervalSeconds"/> seconds; 0 for no keep-alive.</summary>
		/// <remarks>
		/// The interval has to stay well under the idle threshold of the server being connected to, so that a session this
		/// object is holding but not using is not mistaken for an abandoned one. A host embedding this client against a
		/// server whose threshold is not the default sets its own; 0 turns the keep-alive off altogether, which is what a
		/// test watching a reclamation needs.
		/// </remarks>
		public NativeCLISession(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode, NativeSessionInfo nativeSessionInfo, int keepAliveIntervalSeconds)
		{
			// Both bounds matter: the wait is expressed in milliseconds, so an interval above MaximumKeepAliveInterval
			// would overflow to a negative time-out and leave the session with no keep-alive at all - which is what 0 asks
			// for, and asks for visibly.
			if ((keepAliveIntervalSeconds < 0) || (keepAliveIntervalSeconds > MaximumKeepAliveInterval))
				throw new ArgumentOutOfRangeException(nameof(keepAliveIntervalSeconds), keepAliveIntervalSeconds, String.Format("The keep-alive interval must be 0 (no keep-alive) or between 1 and {0} seconds.", MaximumKeepAliveInterval));

			_keepAliveInterval = keepAliveIntervalSeconds;
			_nativeCLI = new NativeSessionCLIClient(hostName, instanceName, overridePortNumber, securityMode, overrideListenerPortNumber, listenerSecurityMode);
			_nativeSessionInfo = nativeSessionInfo;
			_sessionHandle = _nativeCLI.StartSession(nativeSessionInfo);
			StartKeepAlive();
		}
		
		private NativeSessionCLIClient _nativeCLI;
		
		public string HostName { get { return _nativeCLI.HostName; } }
		
		public string InstanceName { get { return _nativeCLI.InstanceName; } }

		public int OverridePortNumber { get { return _nativeCLI.OverridePortNumber; } }
		
		public ConnectionSecurityMode SecurityMode { get { return _nativeCLI.SecurityMode; } }
		
		public int OverrideListenerPortNumber { get { return _nativeCLI.OverrideListenerPortNumber; } }
		
		public ConnectionSecurityMode ListenerSecurityMode { get { return _nativeCLI.ListenerSecurityMode; } }
		
		private NativeSessionInfo _nativeSessionInfo;
		public NativeSessionInfo NativeSessionInfo { get { return _nativeSessionInfo; } }
		
		private NativeSessionHandle _sessionHandle;

		/// <summary>Serializes this session's operations against each other and against the keep-alive ping.</summary>
		/// <remarks>
		/// The far server identifies the session by a handle rather than by a connection, so two threads calling through
		/// one of these objects would otherwise reach it at once. Taking it around every operation is also what keeps the
		/// one new concurrent caller this class has - its own keep-alive thread - off the unsynchronized lazy channel field
		/// in ServiceClient&lt;T&gt;.GetInterface; bug-wcf-client-channel-field-is-unsynchronized owns that pre-existing
		/// race, and nothing here depends on its being fixed.
		/// NOTE: the cost is that an application call can now wait for a ping in flight. On a network that has stopped
		/// delivering, that means waiting about as long as the call would itself have waited, the client's binding setting
		/// no send time-out: it is the same wait, not a new one.
		/// </remarks>
		private readonly object _syncHandle = new object();
		
		#region IDisposable Members

		/// <summary>Ends the session on the far server and releases this side of the connection.</summary>
		/// <remarks>
		/// The local half is released whatever the network does. A failed StopSession - a far server that is down, a
		/// channel already faulted - still propagates, so a caller that wants to report it can; what it must not do is
		/// leave this object looking alive, because the handle it left set would make a second Dispose call the far
		/// server again, and the client it left undisposed holds the WCF channel and its cached channel factory open for
		/// the life of the process.
		/// One wait was added to this by the keep-alive: a ping already in flight holds the lock below, so the stop
		/// waits for that ping's round trip before it sends its own. On a network that has stopped delivering without
		/// closing the socket that is unbounded - the binding sets no send or receive time-out - but so is the stop
		/// itself on the same channel, which is what this method waited on before there was a ping. The release of the
		/// local half is therefore delayed by at most one more round trip than it already was, and a caller that cannot
		/// afford either wait needs a bounded time-out on the binding, not a change here.
		/// </remarks>
		public void Dispose()
		{
			// Signalled before the lock is taken, as LocalServer.Dispose signals before it relinquishes its connection, and
			// not waited on: a ping already past its wait either holds the lock, in which case the stop below waits for it,
			// or takes it after this method has released it and then finds the handle null and does nothing. Either way no
			// ping can follow the end of the session.
			StopKeepAlive();

			lock (_syncHandle)
			{
				try
				{
					if (_sessionHandle != null)
						_nativeCLI.StopSession(_sessionHandle);
				}
				finally
				{
					_sessionHandle = null;
					if (_nativeCLI != null)
					{
						_nativeCLI.Dispose();
						_nativeCLI = null;
					}
				}
			}
		}

		#endregion
		
		public void BeginTransaction(NativeIsolationLevel isolationLevel)
		{
			lock (_syncHandle)
			{
				_nativeCLI.BeginTransaction(_sessionHandle, isolationLevel);
			}
		}
		
		public void PrepareTransaction()
		{
			lock (_syncHandle)
			{
				_nativeCLI.PrepareTransaction(_sessionHandle);
			}
		}
		
		public void CommitTransaction()
		{
			lock (_syncHandle)
			{
				_nativeCLI.CommitTransaction(_sessionHandle);
			}
		}
		
		public void RollbackTransaction()
		{
			lock (_syncHandle)
			{
				_nativeCLI.RollbackTransaction(_sessionHandle);
			}
		}
		
		public int GetTransactionCount()
		{
			lock (_syncHandle)
			{
				return _nativeCLI.GetTransactionCount(_sessionHandle);
			}
		}
		
		public NativeResult Execute(string statement, NativeParam[] paramsValue)
		{
			lock (_syncHandle)
			{
				return _nativeCLI.Execute(_sessionHandle, statement, paramsValue, NativeExecutionOptions.Default);
			}
		}
		
		public NativeResult Execute(string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			lock (_syncHandle)
			{
				return _nativeCLI.Execute(_sessionHandle, statement, paramsValue, options);
			}
		}
		
		public NativeResult[] Execute(NativeExecuteOperation[] operations)
		{
			lock (_syncHandle)
			{
				return _nativeCLI.Execute(_sessionHandle, operations);
			}
		}

		#region Keep Alive

		/*
			Strategy: this object pings the far server every _keepAliveInterval seconds for as long as it is held, so that a
			session a live client is holding but not currently using keeps its clock moving on that server and is not
			mistaken for an abandoned one. That is the arrangement LocalServer has on the main client interface, and the
			shape here is deliberately that one's, with two differences the native interface forces.

			The first is what the ping is. The main interface has a connection object with a Ping operation on it; this
			interface has neither, so the ping is GetTransactionCount - already on the contract, taking nothing but the
			session handle, and with no side effect, since it reads the length of the session process's transaction list. It
			reaches the far server through the same path every other operation does, which is where that server stamps its
			session clock, so no server-side special case and no contract change is needed to carry it. A reader who does
			not know that will otherwise take the call for a leftover.

			The second is that there is one of these threads per session rather than one per client process: a server link
			builds one NativeCLISession per remote session and holds it for the life of the local session's process
			(RemoteSessionImplementation), so a server that links out from many user sessions pays a thread for each.
			NOTE: a System.Threading.Timer would avoid that and must not be used - the client's binding sets
			SendTimeout = TimeSpan.MaxValue (DataphorServiceUtility), so a ping to a server that has stopped answering
			without closing its socket never returns, and on the thread pool that would hold a pool thread for the life of
			the process, one per hung session. If the thread count ever matters, the step is a single shared keep-alive
			thread with a bounded wait per ping, not the pool.

			NOTE: the constants, the fields and the three methods below are LocalServer's, copied rather than shared,
			though both assemblies reference Common and a shared primitive could live there. Deliberate, and the same
			call bug-native-cli-sessions-never-reclaimed records for the two idle watchers: extracting one now would
			rework a path that port-abandoned-session-cleanup has only just proven, for no behaviour. If a third
			keep-alive ever appears, extract one then - a shared one would also be where the single-thread change above
			belongs.
		*/

		/// <summary>The number of seconds between keep-alive pings, unless the constructor is given another.</summary>
		/// <remarks>
		/// The main client's interval (<c>LocalServer.DefaultKeepAliveInterval</c>) and for the same reason: far enough
		/// under a server's idle threshold to survive a ping that is late or lost.
		/// </remarks>
		public const int DefaultKeepAliveInterval = 120;	// 2 Minutes

		/// <summary>The largest interval the constructor accepts: beyond it the millisecond wait would overflow.</summary>
		private const int MaximumKeepAliveInterval = Int32.MaxValue / 1000;

		private readonly int _keepAliveInterval;

		/// <summary>Signal used to indicate that the keep-alive thread can terminate; null when there is no such thread.</summary>
		private ManualResetEvent _keepAliveSignal;
		private readonly object _keepAliveReferenceLock = new object();

		private void StartKeepAlive()
		{
			if (_keepAliveInterval == 0)
				return;

			lock (_keepAliveReferenceLock)
			{
				_keepAliveSignal = new ManualResetEvent(false);
			}

			// Don't use the thread pool... long running thread. A background thread, so that a session that is never
			// disposed can never hold the client process open after Main returns, which is also why LocalServer's
			// keep-alive and the server's own idle watcher are background threads.
			Thread keepAliveThread = new Thread(new ThreadStart(KeepAlive));
			keepAliveThread.IsBackground = true;
			keepAliveThread.Start();
		}

		private void KeepAlive()
		{
			try
			{
				bool signaled = false;
				while (!signaled)
				{
					// Wait for either a signal or a time-out
					signaled = _keepAliveSignal.WaitOne(_keepAliveInterval * 1000);

					// If WaitOne ended due to timeout, send a keep-alive message (then wait again)
					if (!signaled)
						SendKeepAlive();
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application.
			}
			finally
			{
				// The keep alive processing is complete.  Clean up...
				lock (_keepAliveReferenceLock)
				{
					if (_keepAliveSignal != null)
					{
						((IDisposable)_keepAliveSignal).Dispose();
						_keepAliveSignal = null;	// Free the reference
					}
				}
			}
		}

		/// <summary>Pings the far server, unless this session is busy or already ended; a ping that does not land is ignored.</summary>
		/// <remarks>
		/// <para>
		/// Skipped rather than queued when the session is busy: its owner is inside a call, so the far server already
		/// counts the session as in use and the ping has nothing to add - and it must not sit behind a long statement,
		/// because a ping that waited would arrive after the thing it was meant to announce.
		/// </para>
		/// <para>
		/// A failed ping must not end the keep-alive, for the reasons written out at LocalServer.SendKeepAlive: the client
		/// rebuilds its channel on the next call after a communication failure, so a blip lasting seconds leaves the far
		/// server still holding the session, and a keep-alive that stopped at the first failed ping would then let it be
		/// reclaimed minutes later - the loss this exists to prevent. That includes a ping that comes back "Invalid session
		/// handle" because the far server has already reclaimed the session: the owner learns that from its next real call,
		/// which fails with the same error, and a background thread has nowhere to report it to. Nothing is logged, as
		/// there.
		/// </para>
		/// </remarks>
		private void SendKeepAlive()
		{
			if (!Monitor.TryEnter(_syncHandle))
				return;

			try
			{
				// Both read under the lock, which is what Dispose clears them under.
				if ((_nativeCLI != null) && (_sessionHandle != null))
					_nativeCLI.GetTransactionCount(_sessionHandle);
			}
			catch
			{
				// See the remarks above.
			}
			finally
			{
				Monitor.Exit(_syncHandle);
			}
		}

		private void StopKeepAlive()
		{
			lock (_keepAliveReferenceLock)
			{
				if (_keepAliveSignal != null)
					_keepAliveSignal.Set();
			}
		}

		#endregion
	}
}
