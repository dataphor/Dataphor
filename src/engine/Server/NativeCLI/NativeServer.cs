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
		
		private NativeSession GetNativeSession(NativeSessionHandle sessionHandle)
		{
			NativeSession nativeSession;
			if (_nativeSessions.TryGetValue(sessionHandle.ID, out nativeSession))
				return nativeSession;
			
			throw new ArgumentException(String.Format("Invalid session handle: \"{0}\".", sessionHandle.ID.ToString()));
		}
		
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
				GetNativeSession(sessionHandle).Process.BeginTransaction(NativeCLIUtility.NativeIsolationLevelToIsolationLevel(isolationLevel));
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
				GetNativeSession(sessionHandle).Process.PrepareTransaction();
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
				GetNativeSession(sessionHandle).Process.CommitTransaction();
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
				GetNativeSession(sessionHandle).Process.RollbackTransaction();
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
				return GetNativeSession(sessionHandle).Process.TransactionCount;
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}

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
				return GetNativeSession(sessionHandle).Execute(statement, paramsValue, options);
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
				return GetNativeSession(sessionHandle).Execute(operations);
			}
			catch (Exception exception)
			{
				throw NativeCLIUtility.WrapException(exception);
			}
		}
	}
}
