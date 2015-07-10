/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ServiceModel;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientBatch : ClientObject, IRemoteServerBatch
	{
		public ClientBatch(ClientScript clientScript, BatchDescriptor batchDescriptor)
		{
			_clientScript = clientScript;
			_batchDescriptor = batchDescriptor;
		}
		
		private ClientScript _clientScript;
		public ClientScript ClientScript { get { return _clientScript; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return _clientScript.ClientProcess.ClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}

		private void ReportCommunicationError()
		{
			_clientScript.ClientProcess.ClientSession.ClientConnection.ClientServer.ReportCommunicationError();
		}
		
		private BatchDescriptor _batchDescriptor;
		
		public int BatchHandle { get { return _batchDescriptor.Handle; } }
		
		#region IRemoteServerBatch Members

		public IRemoteServerScript ServerScript
		{
			get { return _clientScript; }
		}

		public IRemoteServerPlan Prepare(RemoteParam[] paramsValue)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginPrepareBatch(BatchHandle, paramsValue, null, null);
				result.AsyncWaitHandle.WaitOne();
				PlanDescriptor planDescriptor = channel.EndPrepareBatch(result);
				if (IsExpression())
					return new ClientExpressionPlan(_clientScript.ClientProcess, planDescriptor);
				else
					return new ClientStatementPlan(_clientScript.ClientProcess, planDescriptor);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void Unprepare(IRemoteServerPlan plan)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginUnprepareBatch(((ClientPlan)plan).PlanHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndUnprepareBatch(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public IRemoteServerExpressionPlan PrepareExpression(RemoteParam[] paramsValue, out PlanDescriptor planDescriptor)
		{
			ClientPlan plan = (ClientPlan)Prepare(paramsValue);
			planDescriptor = plan.PlanDescriptor;
			return (IRemoteServerExpressionPlan)plan;
		}

		public void UnprepareExpression(IRemoteServerExpressionPlan plan)
		{
			Unprepare(plan);
		}

		public IRemoteServerStatementPlan PrepareStatement(RemoteParam[] paramsValue, out PlanDescriptor planDescriptor)
		{
			ClientPlan plan = (ClientPlan)Prepare(paramsValue);
			planDescriptor = plan.PlanDescriptor;
			return (IRemoteServerStatementPlan)plan;
		}

		public void UnprepareStatement(IRemoteServerStatementPlan plan)
		{
			Unprepare(plan);
		}

		public void Execute(ref RemoteParamData paramsValue, ProcessCallInfo callInfo)
		{
			ProgramStatistics executeTime;
			if (IsExpression())
				((IRemoteServerExpressionPlan)Prepare(paramsValue.Params)).Evaluate(ref paramsValue, out executeTime, callInfo);
			else
				((IRemoteServerStatementPlan)Prepare(paramsValue.Params)).Execute(ref paramsValue, out executeTime, callInfo);
		}

		#endregion

		#region IServerBatchBase Members

		public bool IsExpression()
		{
			return _batchDescriptor.IsExpression;
		}

		public string GetText()
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetBatchText(BatchHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetBatchText(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public int Line
		{
			get { return _batchDescriptor.Line; }
		}

		#endregion
	}
	
	public class ClientBatches : List<ClientBatch>, IRemoteServerBatches
	{
		#region IRemoteServerBatches Members

		public new IRemoteServerBatch this[int index]
		{
			get { return this[index]; }
			set { throw new NotImplementedException(); }
		}

		#endregion
	}
}
