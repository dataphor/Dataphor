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

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalBatch : LocalServerChildObject, IServerBatch
    {
		public LocalBatch(LocalScript AScript, IRemoteServerBatch ABatch) : base()
		{
			FScript = AScript;
			FBatch = ABatch;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FBatch = null;
			FScript = null;
			base.Dispose(ADisposing);
		}

		protected IRemoteServerBatch FBatch;
		public IRemoteServerBatch RemoteBatch { get { return FBatch; } }
		
		protected internal LocalScript FScript;
		public IServerScript ServerScript { get { return FScript; } }
		
		public IServerProcess ServerProcess { get { return FScript.Process; } }
		
		public void Execute(DataParams AParams)
		{
			RemoteParamData LParams = FScript.FProcess.DataParamsToRemoteParamData(AParams);
			FBatch.Execute(ref LParams, FScript.FProcess.GetProcessCallInfo());
			FScript.FProcess.RemoteParamDataToDataParams(AParams, LParams);
		}
		
		public bool IsExpression()
		{
			return FBatch.IsExpression();
		}
		
		public string GetText()
		{
			return FBatch.GetText();
		}
		
		public int Line { get { return FBatch.Line; } }

		public IServerPlan Prepare(DataParams AParams)
		{
			if (IsExpression())
				return PrepareExpression(AParams);
			else
				return PrepareStatement(AParams);
		}
		
		public void Unprepare(IServerPlan APlan)
		{
			if (APlan is IServerExpressionPlan)
				UnprepareExpression((IServerExpressionPlan)APlan);
			else
				UnprepareStatement((IServerStatementPlan)APlan);
		}
		
		public IServerExpressionPlan PrepareExpression(DataParams AParams)
		{
			#if LOGCACHEEVENTS
			FScript.FProcess.FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} preparing batched expression '{1}'.", Thread.CurrentThread.GetHashCode(), GetText()));
			#endif
			
			PlanDescriptor LPlanDescriptor;
			IRemoteServerExpressionPlan LPlan = FBatch.PrepareExpression(((LocalProcess)ServerProcess).DataParamsToRemoteParams(AParams), out LPlanDescriptor);
			return new LocalExpressionPlan(FScript.FProcess, LPlan, LPlanDescriptor, AParams);
		}
		
		public void UnprepareExpression(IServerExpressionPlan APlan)
		{
			try
			{
				FBatch.UnprepareExpression(((LocalExpressionPlan)APlan).RemotePlan);
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalExpressionPlan)APlan).Dispose();
		}
		
		public IServerStatementPlan PrepareStatement(DataParams AParams)
		{
			PlanDescriptor LPlanDescriptor;
			IRemoteServerStatementPlan LPlan = FBatch.PrepareStatement(((LocalProcess)ServerProcess).DataParamsToRemoteParams(AParams), out LPlanDescriptor);
			return new LocalStatementPlan(FScript.FProcess, LPlan, LPlanDescriptor);
		}
		
		public void UnprepareStatement(IServerStatementPlan APlan)
		{
			try
			{
				FBatch.UnprepareStatement(((LocalStatementPlan)APlan).RemotePlan);
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalStatementPlan)APlan).Dispose();
		}
    }
    
    public class LocalBatches : List<LocalBatch>, IServerBatches
    {
		public LocalBatches() : base() {}
		
		IServerBatch IServerBatches.this[int AIndex]
		{
			get { return base[AIndex]; }
			set { base[AIndex] = (LocalBatch)value; }
		}
    }
}
