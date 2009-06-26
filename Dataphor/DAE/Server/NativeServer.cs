using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Server
{
	/// <summary>
	/// Manages incoming Native CLI requests for a specific Dataphor server instance.
	/// </summary>
	public class NativeServer
	{
		public NativeServer(IServer AServer)
		{
			FServer = AServer;
		}
		
		private IServer FServer;
		public IServer Server { get { return FServer; } }
		
		private NativeSession StartNativeSession(NativeSessionInfo ASessionInfo)
		{
			NativeSession LNativeSession = new NativeSession(ASessionInfo);
			LNativeSession.Session = FServer.Connect(LNativeSession.SessionInfo);
			try
			{
				LNativeSession.Process = LNativeSession.Session.StartProcess(new ProcessInfo(LNativeSession.SessionInfo));
				return LNativeSession;
			}
			catch
			{
				StopNativeSession(LNativeSession);
				throw;
			}
		}
		
		private void StopNativeSession(NativeSession ASession)
		{
			if (ASession.Process != null)
			{
				ASession.Session.StopProcess(ASession.Process);
				ASession.Process = null;
			}
			
			if (ASession.Session != null)
			{
				FServer.Disconnect(ASession.Session);
				ASession.Session = null;
			}
		}
		
		private Dictionary<Guid, NativeSession> FNativeSessions = new Dictionary<Guid, NativeSession>();
		
		private NativeSessionHandle AddNativeSession(NativeSession ANativeSession)
		{
			lock (FNativeSessions)
			{
				FNativeSessions.Add(ANativeSession.ID, ANativeSession);
			}
			
			return new NativeSessionHandle(ANativeSession.ID);
		}
		
		private NativeSession GetNativeSession(NativeSessionHandle ASessionHandle)
		{
			NativeSession LNativeSession;
			if (FNativeSessions.TryGetValue(ASessionHandle.ID, out LNativeSession))
				return LNativeSession;
			
			throw new ArgumentException(String.Format("Invalid session handle: \"{0}\".", ASessionHandle.ID.ToString()));
		}
		
		private NativeSession RemoveNativeSession(NativeSessionHandle ASessionHandle)
		{
			lock (FNativeSessions)
			{
				NativeSession LNativeSession;
				if (FNativeSessions.TryGetValue(ASessionHandle.ID, out LNativeSession))
				{
					FNativeSessions.Remove(ASessionHandle.ID);
					return LNativeSession;
				}
			}

			throw new ArgumentException(String.Format("Invalid session handle: \"{0}\".", ASessionHandle.ID.ToString()));
		}
		
		public NativeSessionHandle StartSession(NativeSessionInfo ASessionInfo)
		{
			try
			{
				NativeSession LNativeSession = StartNativeSession(ASessionInfo);
				try
				{
					return AddNativeSession(LNativeSession);
				}
				catch
				{
					StopNativeSession(LNativeSession);
					throw;
				}
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public void StopSession(NativeSessionHandle ASessionHandle)
		{
			try
			{
				StopNativeSession(RemoveNativeSession(ASessionHandle));
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public void BeginTransaction(NativeSessionHandle ASessionHandle, System.Data.IsolationLevel AIsolationLevel)
		{
			try
			{
				GetNativeSession(ASessionHandle).Process.BeginTransaction(NativeCLIUtility.SystemDataIsolationLevelToIsolationLevel(AIsolationLevel));
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public void PrepareTransaction(NativeSessionHandle ASessionHandle)
		{
			try
			{
				GetNativeSession(ASessionHandle).Process.PrepareTransaction();
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public void CommitTransaction(NativeSessionHandle ASessionHandle)
		{
			try
			{
				GetNativeSession(ASessionHandle).Process.CommitTransaction();
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public void RollbackTransaction(NativeSessionHandle ASessionHandle)
		{
			try
			{
				GetNativeSession(ASessionHandle).Process.RollbackTransaction();
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public int GetTransactionCount(NativeSessionHandle ASessionHandle)
		{
			try
			{
				return GetNativeSession(ASessionHandle).Process.TransactionCount;
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public NativeResult Execute(NativeSessionInfo ASessionInfo, string AStatement, NativeParam[] AParams)
		{
			try
			{
				NativeSession LNativeSession = StartNativeSession(ASessionInfo);
				try
				{
					return LNativeSession.Execute(AStatement, AParams);
				}
				finally
				{
					StopNativeSession(LNativeSession);
				}
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public NativeResult[] Execute(NativeSessionInfo ASessionInfo, NativeExecuteOperation[] AOperations)
		{
			try
			{
				NativeSession LNativeSession = StartNativeSession(ASessionInfo);
				try
				{
					return LNativeSession.Execute(AOperations);
				}
				finally
				{
					StopNativeSession(LNativeSession);
				}
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public NativeResult Execute(NativeSessionHandle ASessionHandle, string AStatement, NativeParam[] AParams)
		{
			try
			{
				return GetNativeSession(ASessionHandle).Execute(AStatement, AParams);
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}

		public NativeResult[] Execute(NativeSessionHandle ASessionHandle, NativeExecuteOperation[] AOperations)
		{
			try
			{
				return GetNativeSession(ASessionHandle).Execute(AOperations);
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}
	}
}
