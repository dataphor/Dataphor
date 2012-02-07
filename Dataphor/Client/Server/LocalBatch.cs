/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalBatch : LocalServerChildObject, IServerBatch
    {
		public LocalBatch(LocalScript script, IRemoteServerBatch batch) : base()
		{
			_script = script;
			_batch = batch;
		}
		
		protected override void Dispose(bool disposing)
		{
			_batch = null;
			_script = null;
			base.Dispose(disposing);
		}

		protected IRemoteServerBatch _batch;
		public IRemoteServerBatch RemoteBatch { get { return _batch; } }
		
		protected internal LocalScript _script;
		public IServerScript ServerScript { get { return _script; } }
		
		public IServerProcess ServerProcess { get { return _script.Process; } }
		
		public void Execute(DataParams paramsValue)
		{
			RemoteParamData localParamsValue = _script._process.DataParamsToRemoteParamData(paramsValue);
			_batch.Execute(ref localParamsValue, _script._process.GetProcessCallInfo());
			_script._process.RemoteParamDataToDataParams(paramsValue, localParamsValue);
		}
		
		public bool IsExpression()
		{
			return _batch.IsExpression();
		}
		
		public string GetText()
		{
			return _batch.GetText();
		}
		
		public int Line { get { return _batch.Line; } }

		public IServerPlan Prepare(DataParams paramsValue)
		{
			if (IsExpression())
				return PrepareExpression(paramsValue);
			else
				return PrepareStatement(paramsValue);
		}
		
		public void Unprepare(IServerPlan plan)
		{
			if (plan is IServerExpressionPlan)
				UnprepareExpression((IServerExpressionPlan)plan);
			else
				UnprepareStatement((IServerStatementPlan)plan);
		}
		
		public IServerExpressionPlan PrepareExpression(DataParams paramsValue)
		{
			#if LOGCACHEEVENTS
			FScript.FProcess.FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} preparing batched expression '{1}'.", Thread.CurrentThread.GetHashCode(), GetText()));
			#endif
			
			PlanDescriptor planDescriptor;
			IRemoteServerExpressionPlan plan = _batch.PrepareExpression(((LocalProcess)ServerProcess).DataParamsToRemoteParams(paramsValue), out planDescriptor);
			return new LocalExpressionPlan(_script._process, plan, planDescriptor, paramsValue);
		}
		
		public void UnprepareExpression(IServerExpressionPlan plan)
		{
			try
			{
				_batch.UnprepareExpression(((LocalExpressionPlan)plan).RemotePlan);
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalExpressionPlan)plan).Dispose();
		}
		
		public IServerStatementPlan PrepareStatement(DataParams paramsValue)
		{
			PlanDescriptor planDescriptor;
			IRemoteServerStatementPlan plan = _batch.PrepareStatement(((LocalProcess)ServerProcess).DataParamsToRemoteParams(paramsValue), out planDescriptor);
			return new LocalStatementPlan(_script._process, plan, planDescriptor);
		}
		
		public void UnprepareStatement(IServerStatementPlan plan)
		{
			try
			{
				_batch.UnprepareStatement(((LocalStatementPlan)plan).RemotePlan);
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalStatementPlan)plan).Dispose();
		}
    }
    
    public class LocalBatches : List<LocalBatch>, IServerBatches
    {
		public LocalBatches() : base() {}
		
		IServerBatch IServerBatches.this[int index]
		{
			get { return base[index]; }
			set { base[index] = (LocalBatch)value; }
		}
    }
}
