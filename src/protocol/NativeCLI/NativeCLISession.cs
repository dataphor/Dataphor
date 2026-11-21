/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public class NativeCLISession : IDisposable
	{
		public NativeCLISession(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode, NativeSessionInfo nativeSessionInfo)
		{
			_nativeCLI = new NativeSessionCLIClient(hostName, instanceName, overridePortNumber, securityMode, overrideListenerPortNumber, listenerSecurityMode);
			_nativeSessionInfo = nativeSessionInfo;
			_sessionHandle = _nativeCLI.StartSession(nativeSessionInfo);
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
		
		#region IDisposable Members

		/// <summary>Ends the session on the far server and releases this side of the connection.</summary>
		/// <remarks>
		/// The local half is released whatever the network does. A failed StopSession - a far server that is down, a
		/// channel already faulted - still propagates, so a caller that wants to report it can; what it must not do is
		/// leave this object looking alive, because the handle it left set would make a second Dispose call the far
		/// server again, and the client it left undisposed holds the WCF channel and its cached channel factory open for
		/// the life of the process.
		/// </remarks>
		public void Dispose()
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

		#endregion
		
		public void BeginTransaction(NativeIsolationLevel isolationLevel)
		{
			_nativeCLI.BeginTransaction(_sessionHandle, isolationLevel);
		}
		
		public void PrepareTransaction()
		{
			_nativeCLI.PrepareTransaction(_sessionHandle);
		}
		
		public void CommitTransaction()
		{
			_nativeCLI.CommitTransaction(_sessionHandle);
		}
		
		public void RollbackTransaction()
		{
			_nativeCLI.RollbackTransaction(_sessionHandle);
		}
		
		public int GetTransactionCount()
		{
			return _nativeCLI.GetTransactionCount(_sessionHandle);
		}
		
		public NativeResult Execute(string statement, NativeParam[] paramsValue)
		{
			return _nativeCLI.Execute(_sessionHandle, statement, paramsValue, NativeExecutionOptions.Default);
		}
		
		public NativeResult Execute(string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			return _nativeCLI.Execute(_sessionHandle, statement, paramsValue, options);
		}
		
		public NativeResult[] Execute(NativeExecuteOperation[] operations)
		{
			return _nativeCLI.Execute(_sessionHandle, operations);
		}
	}
}
