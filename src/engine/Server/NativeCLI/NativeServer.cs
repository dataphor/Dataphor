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
