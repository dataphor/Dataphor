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
	public class ClientBatch : IRemoteServerBatch
	{
		public ClientBatch(ClientScript AClientScript, BatchDescriptor ABatchDescriptor)
		{
			FClientScript = AClientScript;
			FBatchDescriptor = ABatchDescriptor;
		}
		
		private ClientScript FClientScript;
		public ClientScript ClientScript { get { return FClientScript; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return FClientScript.ClientProcess.ClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}
		
		private BatchDescriptor FBatchDescriptor;
		
		public int BatchHandle { get { return FBatchDescriptor.Handle; } }
		
		#region IRemoteServerBatch Members

		public IRemoteServerScript ServerScript
		{
			get { return FClientScript; }
		}

		public IRemoteServerPlan Prepare(RemoteParam[] AParams)
		{
			IAsyncResult LResult = GetServiceInterface().BeginPrepareBatch(BatchHandle, AParams, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			PlanDescriptor LPlanDescriptor = GetServiceInterface().EndPrepareBatch(LResult);
			if (IsExpression())
				return new ClientExpressionPlan(FClientScript.ClientProcess, LPlanDescriptor);
			else
				return new ClientStatementPlan(FClientScript.ClientProcess, LPlanDescriptor);
		}

		public void Unprepare(IRemoteServerPlan APlan)
		{
			IAsyncResult LResult = GetServiceInterface().BeginUnprepareBatch(((ClientPlan)APlan).PlanHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndUnprepareBatch(LResult);
		}

		public IRemoteServerExpressionPlan PrepareExpression(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor)
		{
			ClientPlan LPlan = (ClientPlan)Prepare(AParams);
			APlanDescriptor = LPlan.PlanDescriptor;
			return (IRemoteServerExpressionPlan)LPlan;
		}

		public void UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			Unprepare(APlan);
		}

		public IRemoteServerStatementPlan PrepareStatement(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor)
		{
			ClientPlan LPlan = (ClientPlan)Prepare(AParams);
			APlanDescriptor = LPlan.PlanDescriptor;
			return (IRemoteServerStatementPlan)LPlan;
		}

		public void UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			Unprepare(APlan);
		}

		public void Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo)
		{
			TimeSpan LExecuteTime;
			if (IsExpression())
				((IRemoteServerExpressionPlan)Prepare(AParams.Params)).Evaluate(ref AParams, out LExecuteTime, ACallInfo);
			else
				((IRemoteServerStatementPlan)Prepare(AParams.Params)).Execute(ref AParams, out LExecuteTime, ACallInfo);
		}

		#endregion

		#region IServerBatchBase Members

		public bool IsExpression()
		{
			return FBatchDescriptor.IsExpression;
		}

		public string GetText()
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetBatchText(BatchHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGetBatchText(LResult);
		}

		public int Line
		{
			get { return FBatchDescriptor.Line; }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion
	}
	
	public class ClientBatches : List<ClientBatch>, IRemoteServerBatches
	{
		#region IRemoteServerBatches Members

		public new IRemoteServerBatch this[int AIndex]
		{
			get { return this[AIndex]; }
			set { throw new NotImplementedException(); }
		}

		#endregion
	}
}
