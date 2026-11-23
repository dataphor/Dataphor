/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	/// <summary>
	/// Manages incoming Native CLI requests for a specific Dataphor server instance.
	/// </summary>
	public class NativeServer
	{
		public NativeServer(IServer server)
		{
			_server = server;
		}
		
		private IServer _server;
		public IServer Server { get { return _server; } }
		
		private NativeSession StartNativeSession(NativeSessionInfo sessionInfo)
		{
			NativeSession nativeSession = new NativeSession(sessionInfo);
			nativeSession.Session = _server.Connect(nativeSession.SessionInfo);
			try
			{
				nativeSession.Process = nativeSession.Session.StartProcess(new ProcessInfo(nativeSession.SessionInfo));
				return nativeSession;
			}
			catch
			{
				StopNativeSession(nativeSession);
				throw;
			}
		}
		
		private void StopNativeSession(NativeSession session)
		{
			if (session.Process != null)
			{
				session.Session.StopProcess(session.Process);
				session.Process = null;
			}
			
			if (session.Session != null)
			{
				_server.Disconnect(session.Session);
				session.Session = null;
			}
		}
		
		private Dictionary<Guid, NativeSession> _nativeSessions = new Dictionary<Guid, NativeSession>();
		
		private NativeSessionHandle AddNativeSession(NativeSession nativeSession)
		{
			lock (_nativeSessions)
			{
				_nativeSessions.Add(nativeSession.ID, nativeSession);
			}
			
			return new NativeSessionHandle(nativeSession.ID);
		}
		
		/// <summary>Looks up the session a handle names, counts one call as started on it, and stamps its clock.</summary>
		/// <remarks>
		/// <para>
		/// Every public operation of this class that takes a handle acquires its session here, does its work, and releases
		/// it in a finally - so this is the one place the native interface's liveness clock has to be stamped, and no
		/// change to the service contract <see cref="Alphora.Dataphor.DAE.Contracts.INativeCLIService"/> is needed to carry a ping: a client says "still
		/// here" by making any call at all (<c>NativeCLISession</c>'s keep-alive calls GetTransactionCount).
		/// NOTE: an operation added here that takes a handle and does not release in a finally would leave its session
		/// counted in for ever, which once the idle sweep lands means a session nothing can reclaim - so the acquire,
		/// the try and the finally are written out at each of the seven call sites rather than wrapped in a helper,
		/// there being no delegate or guard idiom anywhere else in this assembly to wrap them with. If the set of
		/// handle-taking operations ever grows past these seven, introduce the guard then.
		/// </para>
		/// <para>
		/// Wholly under the session lock, which is also the fix for a pre-existing data race: the lookup this replaces
		/// read the dictionary with no lock at all while AddNativeSession and RemoveNativeSession wrote it under one, and
		/// the service runs under ConcurrencyMode.Multiple, so two clients' calls genuinely raced - a Dictionary read
		/// concurrent with a write can return a wrong value or throw.
		/// </para>
		/// </remarks>
		private NativeSession AcquireNativeSession(NativeSessionHandle sessionHandle)
		{
			lock (_nativeSessions)
			{
				NativeSession nativeSession;
				if (!_nativeSessions.TryGetValue(sessionHandle.ID, out nativeSession))
					throw new ArgumentException(String.Format("Invalid session handle: \"{0}\".", sessionHandle.ID.ToString()));

				nativeSession.EnterCall();
				return nativeSession;
			}
		}

		/// <summary>Counts the call finished and stamps the session's clock again.</summary>
		/// <remarks>
		/// Takes a session rather than a handle so that it cannot fail: a release that had to look the handle up again
		/// could find the session gone - StopSession can remove it while a call is in flight - and would then throw out of
		/// the finally that called it, replacing whatever the operation itself was reporting.
		/// </remarks>
		private void ReleaseNativeSession(NativeSession nativeSession)
		{
			lock (_nativeSessions)
			{
				nativeSession.ExitCall();
			}
		}

		/// <summary>How long each session this server holds has been without a call, in seconds; a session with a call in flight reports 0.</summary>
		/// <remarks>
		/// The one public read of the session table, and public only for the test that watches a client's keep-alive move
		/// a session's clock from outside this assembly: the repository has no InternalsVisibleTo convention to use
		/// instead (the precedent, with the same reason, is <c>DataphorHost.BuildHost</c>). An idle watcher is in this
		/// assembly and reads the sessions and their counts together under the lock above instead; one entry per session,
		/// in no particular order, is all an observer needs.
		/// </remarks>
		public long[] GetSessionIdleSeconds()
		{
			lock (_nativeSessions)
			{
				long[] result = new long[_nativeSessions.Count];
				int index = 0;
				foreach (NativeSession nativeSession in _nativeSessions.Values)
					result[index++] = nativeSession.IdleSeconds;
				return result;
			}
		}
		
		/// <summary>Stops every session that has had no call on it for longer than the given number of seconds, and returns how many it stopped.</summary>
		/// <remarks>
		/// <para>
		/// Public like the members of <see cref="Alphora.Dataphor.DAE.Server.RemoteServer"/> that the main interface's idle
		/// watcher calls - GetCurrentConnections and Relinquish - and for the reason <see cref="GetSessionIdleSeconds"/>
		/// gives as well: this class is reached from outside the server assembly through
		/// <c>DataphorServiceHost.NativeServer</c> and the repository has no InternalsVisibleTo convention. The watcher that
		/// actually calls it, <c>Alphora.Dataphor.DAE.Service.NativeSessionManager</c>, is in this assembly and would not
		/// need it to be.
		/// </para>
		/// <para>
		/// Stopping a session stops its process, which rolls back its open transactions - releasing, among other things, the
		/// catalog store's write lock, which an abandoned transaction that changed the catalog would otherwise hold against
		/// every other session's schema changes. The sweep works in two steps, one under the session lock and one outside
		/// it; the comments in the body say why each has to be where it is.
		/// </para>
		/// </remarks>
		public int ReclaimIdleSessions(int idleSeconds)
		{
			// A session with a call in flight reports 0 idle seconds (NativeSession.IdleSeconds), so a threshold of at least
			// one second is what makes the single test below cover both conditions the sweep has to apply: no call in flight,
			// and a clock older than the threshold. Refused rather than clamped, as NativeSessionManager's constructor
			// refuses the same value, because a caller asking for 0 is asking for something this method does not mean.
			if (idleSeconds < 1)
				throw new ArgumentOutOfRangeException(nameof(idleSeconds), idleSeconds, "The idle threshold must be at least 1 second.");

			List<ReclaimedSession> reclaimed = null;

			// Deciding and removing are one atomic step, deliberately: a session found idle must not be able to take a call
			// between the decision and the removal. This is where the native interface has an advantage over the main one,
			// where "barring the watcher from a connection with a call in flight would need the handle-to-connection plumbing
			// that does not exist" (docs/modernization-plan.md). Here every operation goes through AcquireNativeSession, so
			// the count of calls in flight is there to read and the watcher can simply leave a busy session alone.
			lock (_nativeSessions)
			{
				foreach (NativeSession nativeSession in _nativeSessions.Values)
				{
					long sessionIdleSeconds = nativeSession.IdleSeconds;
					if (sessionIdleSeconds > idleSeconds)
					{
						if (reclaimed == null)
							reclaimed = new List<ReclaimedSession>();
						reclaimed.Add(new ReclaimedSession(nativeSession, sessionIdleSeconds));
					}
				}

				if (reclaimed == null)
					return 0;

				// Collected first and removed here rather than removed as they are found: a Dictionary cannot be written while
				// it is being enumerated.
				foreach (ReclaimedSession session in reclaimed)
					_nativeSessions.Remove(session.Session.ID);
			}

			// The log lives on the product's Server, and this class holds an IServer, so it is null for an engine that is not
			// one - nothing below depends on it. This field, unlike the RemoteServer.Server that ConnectionManager reads, is
			// never nulled, so reading it once here is style rather than necessity; what the engine's own teardown can still
			// do to a sweep in flight is covered by the guard in LogReclamation.
			// Qualified because the enclosing namespace Alphora.Dataphor.DAE wins over the type of the same name.
			Alphora.Dataphor.DAE.Server.Server server = _server as Alphora.Dataphor.DAE.Server.Server;

			// Outside the lock, because stopping a session stops its process, which waits half of
			// Engine.ProcessTerminationTimeout (15 s by default) for a thread that will not stop - and every operation of this
			// class takes the session lock, so holding it for that would stall every other client's call.
			// NOTE: the sweep is serial, so one session whose process will not stop delays every later reclamation in the same
			// sweep by that much, exactly as a connection sweep is delayed (see ConnectionManager.RelinquishIdleConnections).
			// If that is ever seen, stop each session on a task of its own rather than shortening the wait, which is what
			// gives an aborted process the chance to unwind.
			foreach (ReclaimedSession session in reclaimed)
			{
				// Read before the stop, which rolls the transaction back and would leave nothing to report.
				string transactionCount = TransactionCountOf(session.Session);

				Exception stopError = null;
				try
				{
					StopNativeSession(session.Session);
				}
				catch (Exception exception)
				{
					// Captured rather than thrown, so that one session that will not stop does not end the sweep and leave the
					// sessions after it removed from the table and never stopped.
					stopError = exception;
				}

				LogReclamation(server, session, transactionCount, stopError);
			}

			return reclaimed.Count;
		}

		/// <summary>A session the sweep has taken out of the table, with the idle time it was taken for.</summary>
		/// <remarks>
		/// The idle time is captured with the decision itself because <c>NativeSession.IdleSeconds</c> is only correct under
		/// the session lock, and the log line below reports it.
		/// </remarks>
		private struct ReclaimedSession
		{
			public ReclaimedSession(NativeSession session, long idleSeconds)
			{
				Session = session;
				IdleSeconds = idleSeconds;
			}

			public readonly NativeSession Session;
			public readonly long IdleSeconds;
		}

		/// <summary>How many transactions a session about to be stopped has open, for the log line; "unknown" if that cannot be read.</summary>
		/// <remarks>
		/// This is the part of the log line that tells an operator whether anyone else was being blocked, and it has to be
		/// read before the stop rolls the transaction back. Tolerant of a session with no process, and of a process that
		/// refuses the read, because a detail of a log line must not be able to skip the stop that follows it - which is what
		/// the sweep exists to do.
		/// </remarks>
		private static string TransactionCountOf(NativeSession nativeSession)
		{
			try
			{
				return nativeSession.Process == null ? "unknown" : nativeSession.Process.TransactionCount.ToString();
			}
			catch
			{
				return "unknown";
			}
		}

		/// <summary>Writes the log record of one reclamation: the line naming the session, and the error if stopping it failed.</summary>
		/// <remarks>
		/// The server log is where a reclamation is visible - there is no new error and no new error code for it, and a
		/// client that comes back to a reclaimed session simply finds its handle invalid. Nothing here may throw: the sweep
		/// runs on the watcher's thread, whose own catch would swallow the exception and end that thread for the life of the
		/// process, and the engine's log file is closed when the server stops, which a sweep can overlap. Dropping one log
		/// line is the cheaper loss; ConnectionManager.RelinquishIdleConnections writes that out at length.
		/// Written after the stop rather than before it, which is where ConnectionManager writes its, because by this point
		/// the session is already out of the table and nothing else will ever stop it.
		/// </remarks>
		private static void LogReclamation(Alphora.Dataphor.DAE.Server.Server server, ReclaimedSession session, string transactionCount, Exception stopError)
		{
			if (server == null)
				return;

			try
			{
				server.LogMessage(String.Format("Native session reclaimed due to inactivity: SessionID={0}, UserID={1}, HostName={2}, IdleSeconds={3}, TransactionCount={4}", session.Session.ID, session.Session.NativeSessionInfo.UserID, session.Session.NativeSessionInfo.HostName, session.IdleSeconds, transactionCount));

				if (stopError != null)
					server.LogError(stopError);
			}
			catch
			{
				// See the remarks.
			}
		}
		
		/// <summary>Takes the session a handle names out of the table, whatever is happening on it.</summary>
		/// <remarks>
		/// NOTE: a StopSession arriving while a call is in flight on the same session removes it and leaves that call
		/// running against a session the table no longer holds - the call completes, and its release stamps a clock
		/// nothing will read again. That is how it has always behaved and this is not the ticket that changes it; the
		/// in-use count AcquireNativeSession keeps does not cover it, and must not be read as covering it.
		/// </remarks>
		private NativeSession RemoveNativeSession(NativeSessionHandle sessionHandle)
		{
			lock (_nativeSessions)
			{
				NativeSession nativeSession;
				if (_nativeSessions.TryGetValue(sessionHandle.ID, out nativeSession))
				{
					_nativeSessions.Remove(sessionHandle.ID);
					return nativeSession;
				}
			}

			throw new ArgumentException(String.Format("Invalid session handle: \"{0}\".", sessionHandle.ID.ToString()));
		}
		
		public NativeSessionHandle StartSession(NativeSessionInfo sessionInfo)
		{
			try
			{
				NativeSession nativeSession = StartNativeSession(sessionInfo);
				try
				{
					return AddNativeSession(nativeSession);
				}
				catch
				{
					StopNativeSession(nativeSession);
					throw;
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public void StopSession(NativeSessionHandle sessionHandle)
		{
			try
			{
				StopNativeSession(RemoveNativeSession(sessionHandle));
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public void BeginTransaction(NativeSessionHandle sessionHandle, NativeIsolationLevel isolationLevel)
		{
			try
			{
				NativeSession nativeSession = AcquireNativeSession(sessionHandle);
				try
				{
					nativeSession.Process.BeginTransaction(NativeCLIUtility.NativeIsolationLevelToIsolationLevel(isolationLevel));
				}
				finally
				{
					ReleaseNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public void PrepareTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				NativeSession nativeSession = AcquireNativeSession(sessionHandle);
				try
				{
					nativeSession.Process.PrepareTransaction();
				}
				finally
				{
					ReleaseNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public void CommitTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				NativeSession nativeSession = AcquireNativeSession(sessionHandle);
				try
				{
					nativeSession.Process.CommitTransaction();
				}
				finally
				{
					ReleaseNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public void RollbackTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				NativeSession nativeSession = AcquireNativeSession(sessionHandle);
				try
				{
					nativeSession.Process.RollbackTransaction();
				}
				finally
				{
					ReleaseNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public int GetTransactionCount(NativeSessionHandle sessionHandle)
		{
			try
			{
				NativeSession nativeSession = AcquireNativeSession(sessionHandle);
				try
				{
					return nativeSession.Process.TransactionCount;
				}
				finally
				{
					ReleaseNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		/// <summary>Runs one statement on a session of its own, which exists only for the duration of this call.</summary>
		/// <remarks>
		/// No liveness accounting, because there is nothing to account for: the session is created, used and stopped
		/// inside this call and never enters the session table, so nothing outside can name it and no idle watcher can
		/// ever see it. The same holds for the overload below.
		/// </remarks>
		public NativeResult Execute(NativeSessionInfo sessionInfo, string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			try
			{
				NativeSession nativeSession = StartNativeSession(sessionInfo);
				try
				{
					return nativeSession.Execute(statement, paramsValue, options);
				}
				finally
				{
					StopNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public NativeResult[] Execute(NativeSessionInfo sessionInfo, NativeExecuteOperation[] operations)
		{
			try
			{
				NativeSession nativeSession = StartNativeSession(sessionInfo);
				try
				{
					return nativeSession.Execute(operations);
				}
				finally
				{
					StopNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public NativeResult Execute(NativeSessionHandle sessionHandle, string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			try
			{
				NativeSession nativeSession = AcquireNativeSession(sessionHandle);
				try
				{
					return nativeSession.Execute(statement, paramsValue, options);
				}
				finally
				{
					ReleaseNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

		public NativeResult[] Execute(NativeSessionHandle sessionHandle, NativeExecuteOperation[] operations)
		{
			try
			{
				NativeSession nativeSession = AcquireNativeSession(sessionHandle);
				try
				{
					return nativeSession.Execute(operations);
				}
				finally
				{
					ReleaseNativeSession(nativeSession);
				}
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}
	}
}
