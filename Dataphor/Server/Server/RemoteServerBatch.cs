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
		internal RemoteServerBatch(RemoteServerScript AScript, ServerBatch ABatch) : base()
		{
			FScript = AScript;
			FServerBatch = ABatch;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FScript = null;
			FServerBatch = null;
			base.Dispose(ADisposing);
		}

		private RemoteServerScript FScript;
		public RemoteServerScript Script { get { return FScript; } }
		
		IRemoteServerScript IRemoteServerBatch.ServerScript { get { return FScript; } }
		
		private ServerBatch FServerBatch;
		internal ServerBatch ServerBatch { get { return FServerBatch; } }
		
		private Exception WrapException(Exception AException)
		{
			return RemoteServer.WrapException(AException);
		}

		public int Line { get { return FServerBatch.Line; } }
		
		public bool IsExpression()
		{
			return FServerBatch.IsExpression();
		}
		
		public string GetText()
		{
			return FServerBatch.GetText();
		}
		
		public void Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo)
		{
			FScript.Process.ProcessCallInfo(ACallInfo);
			try
			{
				RemoteParam[] LParams = new RemoteParam[AParams.Params == null ? 0 : AParams.Params.Length];
				for (int LIndex = 0; LIndex < (AParams.Params == null ? 0 : AParams.Params.Length); LIndex++)
				{
					LParams[LIndex].Name = AParams.Params[LIndex].Name;
					LParams[LIndex].TypeName = AParams.Params[LIndex].TypeName;
					LParams[LIndex].Modifier = AParams.Params[LIndex].Modifier;
				}
				if (IsExpression())
				{
					PlanDescriptor LPlanDescriptor;
					IRemoteServerExpressionPlan LPlan = PrepareExpression(LParams, out LPlanDescriptor);
					try
					{
						LPlan.Close(LPlan.Open(ref AParams, out LPlanDescriptor.Statistics.ExecuteTime, FScript.Process.EmptyCallInfo()), FScript.Process.EmptyCallInfo());
						// TODO: Provide a mechanism for determining whether or not an expression should be evaluated or opened through the remoting CLI.
					}
					finally
					{
						UnprepareExpression(LPlan);
					}
				}
				else
				{
					PlanDescriptor LPlanDescriptor;
					IRemoteServerStatementPlan LPlan = PrepareStatement(LParams, out LPlanDescriptor);
					try
					{
						LPlan.Execute(ref AParams, out LPlanDescriptor.Statistics.ExecuteTime, ACallInfo);
					}
					finally
					{
						UnprepareStatement(LPlan);
					}
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public IRemoteServerPlan Prepare(RemoteParam[] AParams)
		{
			PlanDescriptor LPlanDescriptor;
			if (IsExpression())
				return PrepareExpression(AParams, out LPlanDescriptor);
			else
				return PrepareStatement(AParams, out LPlanDescriptor);
		}
		
		public void Unprepare(IRemoteServerPlan APlan)
		{
			if (APlan  is IRemoteServerExpressionPlan)
				UnprepareExpression((IRemoteServerExpressionPlan)APlan);
			else
				UnprepareStatement((IRemoteServerStatementPlan)APlan);
		}
		
		public IRemoteServerExpressionPlan PrepareExpression(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor)
		{
			try
			{
				DataParams LParams = FScript.Process.RemoteParamsToDataParams(AParams);
				RemoteServerExpressionPlan LRemotePlan = new RemoteServerExpressionPlan(FScript.Process, (ServerExpressionPlan)FServerBatch.PrepareExpression(LParams));
				APlanDescriptor = FScript.Process.GetPlanDescriptor(LRemotePlan, AParams);
				return LRemotePlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			FScript.Process.UnprepareExpression(APlan);
		}
		
		public IRemoteServerStatementPlan PrepareStatement(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor)
		{
			try
			{
				DataParams LParams = FScript.Process.RemoteParamsToDataParams(AParams);
				RemoteServerStatementPlan LRemotePlan = new RemoteServerStatementPlan(FScript.Process, (ServerStatementPlan)FServerBatch.PrepareStatement(LParams));
				APlanDescriptor = FScript.Process.GetPlanDescriptor(LRemotePlan);
				return LRemotePlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			FScript.Process.UnprepareStatement(APlan);
		}
	}
	
	// RemoteServerBatches
	[Serializable]
	public class RemoteServerBatches : RemoteServerChildObjects, IRemoteServerBatches
	{		
		protected override void Validate(RemoteServerChildObject AObject)
		{
			if (!(AObject is RemoteServerBatch))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerBatch");
		}
		
		public new RemoteServerBatch this[int AIndex]
		{
			get { return (RemoteServerBatch)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		IRemoteServerBatch IRemoteServerBatches.this[int AIndex]
		{
			get { return (IRemoteServerBatch)base[AIndex]; } 
			set { base[AIndex] = (RemoteServerBatch)value; } 
		}
		
		public RemoteServerBatch[] All
		{
			get
			{
				RemoteServerBatch[] LArray = new RemoteServerBatch[Count];
				for (int LIndex = 0; LIndex < Count; LIndex++)
					LArray[LIndex] = this[LIndex];
				return LArray;
			}
			set
			{
				foreach (RemoteServerBatch LBatch in value)
					Add(LBatch);
			}
		}
	}
}
