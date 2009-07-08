/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define TRACEEVENTS // Enable this to turn on tracing
#define ALLOWPROCESSCONTEXT
#define LOADFROMLIBRARIES

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Server
{
	// ServerBatch
	public class ServerBatch : ServerChildObject, IServerBatch, IRemoteServerBatch
	{
		internal ServerBatch(ServerScript AScript, Statement ABatch) : base()
		{
			FScript = AScript;
			FBatch = ABatch;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FScript = null;
			FBatch = null;
			base.Dispose(ADisposing);
		}

		private ServerScript FScript;

		IServerScript IServerBatch.ServerScript { get { return (IServerScript)FScript; } }
		
		IRemoteServerScript IRemoteServerBatch.ServerScript { get { return (IRemoteServerScript)FScript; } }
		
		private Statement FBatch;
		
		public int Line { get { return FBatch.Line; } }
		
		public bool IsExpression()
		{
			return FBatch is SelectStatement;
		}
		
		public string GetText()
		{
			return new D4TextEmitter().Emit(FBatch);
		}
		
		void IServerBatch.Execute(DataParams AParams)
		{
			try
			{
				if (IsExpression())
				{
					IServerExpressionPlan LPlan = ((IServerBatch)this).PrepareExpression(AParams);
					try
					{
						if (LPlan.DataType is Schema.TableType)
							LPlan.Close(LPlan.Open(AParams));
						else
							LPlan.Evaluate(AParams).Dispose();
					}
					finally
					{
						((IServerBatch)this).UnprepareExpression(LPlan);
					}
				}
				else
				{
					IServerStatementPlan LPlan = ((IServerBatch)this).PrepareStatement(AParams);
					try
					{
						LPlan.Execute(AParams);
					}
					finally
					{
						((IServerBatch)this).UnprepareStatement(LPlan);
					}
				}
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IRemoteServerBatch.Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo)
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
					IRemoteServerExpressionPlan LPlan = ((IRemoteServerBatch)this).PrepareExpression(LParams, out LPlanDescriptor);
					try
					{
						LPlan.Close(LPlan.Open(ref AParams, out LPlanDescriptor.Statistics.ExecuteTime, FScript.Process.EmptyCallInfo()), FScript.Process.EmptyCallInfo());
						// TODO: Provide a mechanism for determining whether or not an expression should be evaluated or opened through the remoting CLI.
					}
					finally
					{
						((IRemoteServerBatch)this).UnprepareExpression(LPlan);
					}
				}
				else
				{
					PlanDescriptor LPlanDescriptor;
					IRemoteServerStatementPlan LPlan = ((IRemoteServerBatch)this).PrepareStatement(LParams, out LPlanDescriptor);
					try
					{
						LPlan.Execute(ref AParams, out LPlanDescriptor.Statistics.ExecuteTime, ACallInfo);
					}
					finally
					{
						((IRemoteServerBatch)this).UnprepareStatement(LPlan);
					}
				}
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		IServerPlan IServerBatch.Prepare(DataParams AParams)
		{
			if (IsExpression())
				return ((IServerBatch)this).PrepareExpression(AParams);
			else
				return ((IServerBatch)this).PrepareStatement(AParams);
		}
		
		void IServerBatch.Unprepare(IServerPlan APlan)
		{
			if (APlan is IServerExpressionPlan)
				((IServerBatch)this).UnprepareExpression((IServerExpressionPlan)APlan);
			else
				((IServerBatch)this).UnprepareStatement((IServerStatementPlan)APlan);
		}
		
		IServerExpressionPlan IServerBatch.PrepareExpression(DataParams AParams)
		{
			try
			{
				FScript.CheckParsed();
				return (IServerExpressionPlan)((ServerProcess)FScript.Process).CompileExpression(FBatch, null, AParams);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IServerBatch.UnprepareExpression(IServerExpressionPlan APlan)
		{
			try
			{
				((IServerProcess)FScript.Process).UnprepareExpression(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		IServerStatementPlan IServerBatch.PrepareStatement(DataParams AParams)
		{
			try
			{
				FScript.CheckParsed();
				return (IServerStatementPlan)((ServerProcess)FScript.Process).CompileStatement(FBatch, null, AParams);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IServerBatch.UnprepareStatement(IServerStatementPlan APlan)
		{
			try
			{
				((IServerProcess)FScript.Process).UnprepareStatement(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		IRemoteServerPlan IRemoteServerBatch.Prepare(RemoteParam[] AParams)
		{
			PlanDescriptor LPlanDescriptor;
			if (IsExpression())
				return ((IRemoteServerBatch)this).PrepareExpression(AParams, out LPlanDescriptor);
			else
				return ((IRemoteServerBatch)this).PrepareStatement(AParams, out LPlanDescriptor);
		}
		
		void IRemoteServerBatch.Unprepare(IRemoteServerPlan APlan)
		{
			if (APlan  is IRemoteServerExpressionPlan)
				((IRemoteServerBatch)this).UnprepareExpression((IRemoteServerExpressionPlan)APlan);
			else
				((IRemoteServerBatch)this).UnprepareStatement((IRemoteServerStatementPlan)APlan);
		}
		
		IRemoteServerExpressionPlan IRemoteServerBatch.PrepareExpression(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor)
		{
			try
			{
				FScript.CheckParsed();
				IRemoteServerExpressionPlan LPlan =	(IRemoteServerExpressionPlan)((ServerProcess)FScript.Process).CompileRemoteExpression(FBatch, null, AParams);
				APlanDescriptor = ((ServerProcess)FScript.Process).GetPlanDescriptor(LPlan, AParams);
				return LPlan;
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IRemoteServerBatch.UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			try
			{
				((IRemoteServerProcess)FScript.Process).UnprepareExpression(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		IRemoteServerStatementPlan IRemoteServerBatch.PrepareStatement(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor)
		{
			try
			{
				FScript.CheckParsed();
				IRemoteServerStatementPlan LPlan = (IRemoteServerStatementPlan)((ServerProcess)FScript.Process).CompileRemoteStatement(FBatch, null, AParams);
				APlanDescriptor = ((ServerProcess)FScript.Process).GetPlanDescriptor(LPlan);
				return LPlan;
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IRemoteServerBatch.UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			try
			{
				((IRemoteServerProcess)FScript.Process).UnprepareStatement(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
	}
	
	// ServerBatches
	[Serializable]
	public class ServerBatches : ServerChildObjects, IServerBatches
	{		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerBatch))
				throw new ServerException(ServerException.Codes.ServerBatchContainer);
		}
		
		public new ServerBatch this[int AIndex]
		{
			get { return (ServerBatch)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		IServerBatch IServerBatches.this[int AIndex]
		{
			get { return (IServerBatch)base[AIndex]; } 
			set { base[AIndex] = (ServerBatch)value; } 
		}
		
		public ServerBatch[] All
		{
			get
			{
				ServerBatch[] LArray = new ServerBatch[Count];
				for (int LIndex = 0; LIndex < Count; LIndex++)
					LArray[LIndex] = this[LIndex];
				return LArray;
			}
			set
			{
				foreach (ServerBatch LBatch in value)
					Add(LBatch);
			}
		}
	}

	// ServerPlan
	public abstract class ServerPlan : ServerPlanBase, IServerPlan
	{
		protected internal ServerPlan(ServerProcess AProcess) : base(AProcess) {}

		public IServerProcess Process  { get { return (IServerProcess)FProcess; } }
		
		public CompilerMessages Messages { get { return Plan.Messages; } }
	}
}
