/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerBatch
	public class RemoteServerBatch : RemoteServerChildObject, IRemoteServerBatch
	{
		internal RemoteServerBatch(RemoteServerScript script, ServerBatch batch) : base()
		{
			_script = script;
			_serverBatch = batch;
		}
		
		protected override void Dispose(bool disposing)
		{
			_script = null;
			_serverBatch = null;
			base.Dispose(disposing);
		}

		private RemoteServerScript _script;
		public RemoteServerScript Script { get { return _script; } }
		
		IRemoteServerScript IRemoteServerBatch.ServerScript { get { return _script; } }
		
		private ServerBatch _serverBatch;
		internal ServerBatch ServerBatch { get { return _serverBatch; } }
		
		private Exception WrapException(Exception exception)
		{
			return RemoteServer.WrapException(exception);
		}

		public int Line { get { return _serverBatch.Line; } }
		
		public bool IsExpression()
		{
			return _serverBatch.IsExpression();
		}
		
		public string GetText()
		{
			return _serverBatch.GetText();
		}
		
		public void Execute(ref RemoteParamData paramsValue, ProcessCallInfo callInfo)
		{
			_script.Process.ProcessCallInfo(callInfo);
			try
			{
				RemoteParam[] localParamsValue = new RemoteParam[paramsValue.Params == null ? 0 : paramsValue.Params.Length];
				for (int index = 0; index < (paramsValue.Params == null ? 0 : paramsValue.Params.Length); index++)
				{
					localParamsValue[index].Name = paramsValue.Params[index].Name;
					localParamsValue[index].TypeName = paramsValue.Params[index].TypeName;
					localParamsValue[index].Modifier = paramsValue.Params[index].Modifier;
				}
				if (IsExpression())
				{
					PlanDescriptor planDescriptor;
					IRemoteServerExpressionPlan plan = PrepareExpression(localParamsValue, out planDescriptor);
					try
					{
						ProgramStatistics programStatistics;
						plan.Close(plan.Open(ref paramsValue, out programStatistics, _script.Process.EmptyCallInfo()), _script.Process.EmptyCallInfo());
						// TODO: Provide a mechanism for determining whether or not an expression should be evaluated or opened through the remoting CLI.
					}
					finally
					{
						UnprepareExpression(plan);
					}
				}
				else
				{
					PlanDescriptor planDescriptor;
					IRemoteServerStatementPlan plan = PrepareStatement(localParamsValue, out planDescriptor);
					try
					{
						ProgramStatistics programStatistics;
						plan.Execute(ref paramsValue, out programStatistics, callInfo);
					}
					finally
					{
						UnprepareStatement(plan);
					}
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public IRemoteServerPlan Prepare(RemoteParam[] paramsValue)
		{
			PlanDescriptor planDescriptor;
			if (IsExpression())
				return PrepareExpression(paramsValue, out planDescriptor);
			else
				return PrepareStatement(paramsValue, out planDescriptor);
		}
		
		public void Unprepare(IRemoteServerPlan plan)
		{
			if (plan  is IRemoteServerExpressionPlan)
				UnprepareExpression((IRemoteServerExpressionPlan)plan);
			else
				UnprepareStatement((IRemoteServerStatementPlan)plan);
		}
		
		public IRemoteServerExpressionPlan PrepareExpression(RemoteParam[] paramsValue, out PlanDescriptor planDescriptor)
		{
			try
			{
				DataParams localParamsValue = _script.Process.RemoteParamsToDataParams(paramsValue);
				RemoteServerExpressionPlan remotePlan = new RemoteServerExpressionPlan(_script.Process, (ServerExpressionPlan)_serverBatch.PrepareExpression(localParamsValue));
				planDescriptor = _script.Process.GetPlanDescriptor(remotePlan, paramsValue);
				return remotePlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void UnprepareExpression(IRemoteServerExpressionPlan plan)
		{
			_script.Process.UnprepareExpression(plan);
		}
		
		public IRemoteServerStatementPlan PrepareStatement(RemoteParam[] paramsValue, out PlanDescriptor planDescriptor)
		{
			try
			{
				DataParams localParamsValue = _script.Process.RemoteParamsToDataParams(paramsValue);
				RemoteServerStatementPlan remotePlan = new RemoteServerStatementPlan(_script.Process, (ServerStatementPlan)_serverBatch.PrepareStatement(localParamsValue));
				planDescriptor = _script.Process.GetPlanDescriptor(remotePlan);
				return remotePlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void UnprepareStatement(IRemoteServerStatementPlan plan)
		{
			_script.Process.UnprepareStatement(plan);
		}
	}
	
	// RemoteServerBatches
	[Serializable]
	public class RemoteServerBatches : RemoteServerChildObjects, IRemoteServerBatches
	{		
		protected override void Validate(RemoteServerChildObject objectValue)
		{
			if (!(objectValue is RemoteServerBatch))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerBatch");
		}
		
		public new RemoteServerBatch this[int index]
		{
			get { return (RemoteServerBatch)base[index]; }
			set { base[index] = value; }
		}
		
		IRemoteServerBatch IRemoteServerBatches.this[int index]
		{
			get { return (IRemoteServerBatch)base[index]; } 
			set { base[index] = (RemoteServerBatch)value; } 
		}
		
		public RemoteServerBatch[] All
		{
			get
			{
				RemoteServerBatch[] array = new RemoteServerBatch[Count];
				for (int index = 0; index < Count; index++)
					array[index] = this[index];
				return array;
			}
			set
			{
				foreach (RemoteServerBatch batch in value)
					Add(batch);
			}
		}
	}
}
