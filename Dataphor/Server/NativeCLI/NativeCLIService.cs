/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Contracts;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	/// <summary>
	/// Provides a single-call remote servicing object for the Native CLI.
	/// </summary>
	/// <remarks>
	/// Rather than explicitly exposing the NativeServer as a remotable object,
	/// this class provides a remotable wrapper that routes Native CLI calls
	/// to the NativeServer established by the ServerHost.
	/// </remarks>
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
	public class NativeCLIService : INativeCLIService
	{
		public NativeCLIService(NativeServer nativeServer)
		{
			_nativeServer = nativeServer;
		}
		
		private NativeServer _nativeServer;
		public NativeServer NativeServer { get { return _nativeServer; } }
		
		#region INativeCLI Members

		public NativeSessionHandle StartSession(NativeSessionInfo sessionInfo)
		{
			try
			{
				return _nativeServer.StartSession(sessionInfo);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void StopSession(NativeSessionHandle sessionHandle)
		{
			try
			{
				_nativeServer.StopSession(sessionHandle);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void BeginTransaction(NativeSessionHandle sessionHandle, NativeIsolationLevel isolationLevel)
		{
			try
			{
				_nativeServer.BeginTransaction(sessionHandle, isolationLevel);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void PrepareTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				_nativeServer.PrepareTransaction(sessionHandle);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void CommitTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				_nativeServer.CommitTransaction(sessionHandle);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void RollbackTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				_nativeServer.RollbackTransaction(sessionHandle);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public int GetTransactionCount(NativeSessionHandle sessionHandle)
		{
			try
			{
				return _nativeServer.GetTransactionCount(sessionHandle);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public NativeResult ExecuteStatement(NativeSessionInfo sessionInfo, string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			try
			{
				return _nativeServer.Execute(sessionInfo, statement, paramsValue, options);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public NativeResult[] ExecuteStatements(NativeSessionInfo sessionInfo, NativeExecuteOperation[] operations)
		{
			try
			{
				return _nativeServer.Execute(sessionInfo, operations);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public NativeResult SessionExecuteStatement(NativeSessionHandle sessionHandle, string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			try
			{
				return _nativeServer.Execute(sessionHandle, statement, paramsValue, options);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public NativeResult[] SessionExecuteStatements(NativeSessionHandle sessionHandle, NativeExecuteOperation[] operations)
		{
			try
			{
				return _nativeServer.Execute(sessionHandle, operations);
			}
			catch (NativeCLIException exception)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		#endregion
	}
}
