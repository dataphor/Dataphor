/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Client
{
	public abstract class ClientPlan : IRemoteServerPlan
	{
		public ClientPlan(ClientProcess AClientProcess, PlanDescriptor APlanDescriptor)
		{
			FClientProcess = AClientProcess;
			FPlanDescriptor = APlanDescriptor;
		}
		
		private ClientProcess FClientProcess;
		public ClientProcess ClientProcess { get { return FClientProcess; } }
		
		protected IClientDataphorService GetServiceInterface()
		{
			return FClientProcess.ClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}
		
		protected PlanDescriptor FPlanDescriptor;
		public PlanDescriptor PlanDescriptor { get { return FPlanDescriptor; } }
		
		public int PlanHandle { get { return FPlanDescriptor.Handle; } }
		
		#region IRemoteServerPlan Members

		public IRemoteServerProcess Process
		{
			get { return FClientProcess; }
		}

		public Exception[] Messages
		{
			get { return FPlanDescriptor.Messages; }
		}

		#endregion

		#region IServerPlanBase Members

		public Guid ID
		{
			get { return FPlanDescriptor.ID; }
		}

		public void CheckCompiled()
		{
			throw new NotImplementedException();
		}

		public PlanStatistics PlanStatistics
		{
			get { return FPlanDescriptor.Statistics; }
		}

		public ProgramStatistics ProgramStatistics
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion
	}

	public class ClientStatementPlan : ClientPlan, IRemoteServerStatementPlan
	{
		public ClientStatementPlan(ClientProcess AClientProcess, PlanDescriptor APlanDescriptor) : base(AClientProcess, APlanDescriptor) { }
		
		#region IRemoteServerStatementPlan Members

		public void Execute(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginExecutePlan(PlanHandle, ACallInfo, ref AParams, out AExecuteTime, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndExecutePlan(LResult);
		}

		#endregion
	}

	public class ClientExpressionPlan : ClientPlan, IRemoteServerExpressionPlan
	{
		public ClientExpressionPlan(ClientProcess AClientProcess, PlanDescriptor APlanDescriptor) : base(AClientProcess, APlanDescriptor) { }
		
		#region IRemoteServerExpressionPlan Members

		public byte[] Evaluate(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginEvaluatePlan(PlanHandle, ACallInfo, ref AParams, out AExecuteTime, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndEvaluatePlan(LResult);
		}

		public IRemoteServerCursor Open(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerCursor Open(ref RemoteParamData AParams, out TimeSpan AExecuteTime, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginOpenPlanCursor(PlanHandle, ACallInfo, ref AParams, out AExecuteTime, out ABookmarks, ACount, out AFetchData, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			CursorDescriptor LDescriptor = GetServiceInterface().EndOpenPlanCursor(LResult);
			return new ClientCursor(this, LDescriptor);
		}

		public void Close(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginCloseCursor(((ClientCursor)ACursor).CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndCloseCursor(LResult);
		}

		#endregion

		#region IServerCursorBehavior Members

		public CursorCapability Capabilities
		{
			get { return FPlanDescriptor.Capabilities; }
		}

		public CursorType CursorType
		{
			get { return FPlanDescriptor.CursorType; }
		}

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}

		public CursorIsolation Isolation
		{
			get { return FPlanDescriptor.CursorIsolation; }
		}

		#endregion
	}
}
