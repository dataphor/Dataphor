/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientProcess : IRemoteServerProcess
	{
		#region IRemoteServerProcess Members

		public IRemoteServerSession Session
		{
			get { throw new NotImplementedException(); }
		}

		public Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void PrepareApplicationTransaction(Guid AID, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void CommitApplicationTransaction(Guid AID, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void RollbackApplicationTransaction(Guid AID, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Guid ApplicationTransactionID
		{
			get { throw new NotImplementedException(); }
		}

		public void JoinApplicationTransaction(Guid AID, bool AIsInsert, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void LeaveApplicationTransaction(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerStatementPlan PrepareStatement(string AStatement, Alphora.Dataphor.DAE.Contracts.RemoteParam[] AParams, Alphora.Dataphor.DAE.Debug.DebugLocator ALocator, out Alphora.Dataphor.DAE.Contracts.PlanDescriptor APlanDescriptor, RemoteProcessCleanupInfo ACleanupInfo)
		{
			throw new NotImplementedException();
		}

		public void UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			throw new NotImplementedException();
		}

		public void Execute(string AStatement, ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerExpressionPlan PrepareExpression(string AExpression, Alphora.Dataphor.DAE.Contracts.RemoteParam[] AParams, Alphora.Dataphor.DAE.Debug.DebugLocator ALocator, out Alphora.Dataphor.DAE.Contracts.PlanDescriptor APlanDescriptor, RemoteProcessCleanupInfo ACleanupInfo)
		{
			throw new NotImplementedException();
		}

		public void UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			throw new NotImplementedException();
		}

		public byte[] Evaluate(string AExpression, ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out Alphora.Dataphor.DAE.Contracts.PlanDescriptor APlanDescriptor, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerCursor OpenCursor(string AExpression, ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out Alphora.Dataphor.DAE.Contracts.PlanDescriptor APlanDescriptor, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerCursor OpenCursor(string AExpression, ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out Alphora.Dataphor.DAE.Contracts.PlanDescriptor APlanDescriptor, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo, out Guid[] ABookmarks, int ACount, out Alphora.Dataphor.DAE.Contracts.RemoteFetchData AFetchData)
		{
			throw new NotImplementedException();
		}

		public void CloseCursor(IRemoteServerCursor ACursor, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerScript PrepareScript(string AScript, Alphora.Dataphor.DAE.Debug.DebugLocator ALocator)
		{
			throw new NotImplementedException();
		}

		public void UnprepareScript(IRemoteServerScript AScript)
		{
			throw new NotImplementedException();
		}

		public void ExecuteScript(string AScript, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public string GetCatalog(string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			throw new NotImplementedException();
		}

		public string GetClassName(string AClassName)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Server.ServerFileInfo[] GetFileNames(string AClassName)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Streams.IRemoteStream GetFile(string ALibraryName, string AFileName)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IServerProcessBase Members

		public int ProcessID
		{
			get { throw new NotImplementedException(); }
		}

		public ProcessInfo ProcessInfo
		{
			get { throw new NotImplementedException(); }
		}

		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			throw new NotImplementedException();
		}

		public void PrepareTransaction()
		{
			throw new NotImplementedException();
		}

		public void CommitTransaction()
		{
			throw new NotImplementedException();
		}

		public void RollbackTransaction()
		{
			throw new NotImplementedException();
		}

		public bool InTransaction
		{
			get { throw new NotImplementedException(); }
		}

		public int TransactionCount
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion

		#region IStreamManager Members

		public Alphora.Dataphor.DAE.Streams.StreamID Allocate()
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Streams.StreamID Reference(Alphora.Dataphor.DAE.Streams.StreamID AStreamID)
		{
			throw new NotImplementedException();
		}

		public void Deallocate(Alphora.Dataphor.DAE.Streams.StreamID AStreamID)
		{
			throw new NotImplementedException();
		}

		public System.IO.Stream Open(Alphora.Dataphor.DAE.Streams.StreamID AStreamID, Alphora.Dataphor.DAE.Runtime.LockMode AMode)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Streams.IRemoteStream OpenRemote(Alphora.Dataphor.DAE.Streams.StreamID AStreamID, Alphora.Dataphor.DAE.Runtime.LockMode AMode)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
