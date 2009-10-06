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
	public abstract class ClientPlan : ClientObject, IRemoteServerPlan
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

		protected ProgramStatistics FProgramStatistics = new ProgramStatistics();
		public ProgramStatistics ProgramStatistics { get { return FProgramStatistics; } }

		#endregion
	}

	public class ClientStatementPlan : ClientPlan, IRemoteServerStatementPlan
	{
		public ClientStatementPlan(ClientProcess AClientProcess, PlanDescriptor APlanDescriptor) : base(AClientProcess, APlanDescriptor) { }
		
		#region IRemoteServerStatementPlan Members

		public void Execute(ref RemoteParamData AParams, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginExecutePlan(PlanHandle, ACallInfo, AParams, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			ExecuteResult LExecuteResult = GetServiceInterface().EndExecutePlan(LResult);
			AExecuteTime = LExecuteResult.ExecuteTime;
			AParams.Data = LExecuteResult.ParamData;
		}

		#endregion
	}

	public class ClientExpressionPlan : ClientPlan, IRemoteServerExpressionPlan
	{
		public ClientExpressionPlan(ClientProcess AClientProcess, PlanDescriptor APlanDescriptor) : base(AClientProcess, APlanDescriptor) { }
		
		#region IRemoteServerExpressionPlan Members

		public byte[] Evaluate(ref RemoteParamData AParams, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginEvaluatePlan(PlanHandle, ACallInfo, AParams, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			EvaluateResult LEvaluateResult = GetServiceInterface().EndEvaluatePlan(LResult);
			AExecuteTime = LEvaluateResult.ExecuteTime;
			FProgramStatistics = AExecuteTime;
			AParams.Data = LEvaluateResult.ParamData;
			return LEvaluateResult.Result;
		}

		public IRemoteServerCursor Open(ref RemoteParamData AParams, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginOpenPlanCursor(PlanHandle, ACallInfo, AParams, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			CursorResult LCursorResult = GetServiceInterface().EndOpenPlanCursor(LResult);
			AExecuteTime = LCursorResult.ExecuteTime;
			FProgramStatistics = AExecuteTime;
			AParams.Data = LCursorResult.ParamData;
			return new ClientCursor(this, LCursorResult.CursorDescriptor);
		}

		public IRemoteServerCursor Open(ref RemoteParamData AParams, out ProgramStatistics AExecuteTime, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginOpenPlanCursorWithFetch(PlanHandle, ACallInfo, AParams, ACount, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			CursorWithFetchResult LCursorResult = GetServiceInterface().EndOpenPlanCursorWithFetch(LResult);
			AExecuteTime = LCursorResult.ExecuteTime;
			FProgramStatistics = AExecuteTime;
			AParams.Data = LCursorResult.ParamData;
			ABookmarks = LCursorResult.Bookmarks;
			AFetchData = LCursorResult.FetchData;
			return new ClientCursor(this, LCursorResult.CursorDescriptor);
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
