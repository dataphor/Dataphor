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
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	// ServerBatch
	public class ServerBatch : ServerChildObject, IServerBatch
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
		public ServerScript Script { get { return FScript; } }
		
		IServerScript IServerBatch.ServerScript { get { return FScript; } }

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
		
		public void Execute(DataParams AParams)
		{
			try
			{
				if (IsExpression())
				{
					IServerExpressionPlan LPlan = PrepareExpression(AParams);
					try
					{
						if (LPlan.DataType is Schema.TableType)
							LPlan.Close(LPlan.Open(AParams));
						else
							DataValue.DisposeValue(this.Script.Process.ValueManager, LPlan.Evaluate(AParams));
					}
					finally
					{
						UnprepareExpression(LPlan);
					}
				}
				else
				{
					IServerStatementPlan LPlan = PrepareStatement(AParams);
					try
					{
						LPlan.Execute(AParams);
					}
					finally
					{
						UnprepareStatement(LPlan);
					}
				}
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
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
			try
			{
				FScript.CheckParsed();
				return (IServerExpressionPlan)FScript.Process.CompileExpression(FBatch, null, AParams, FScript.SourceContext);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		public void UnprepareExpression(IServerExpressionPlan APlan)
		{
			try
			{
				FScript.Process.UnprepareExpression(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		public IServerStatementPlan PrepareStatement(DataParams AParams)
		{
			try
			{
				FScript.CheckParsed();
				return (IServerStatementPlan)FScript.Process.CompileStatement(FBatch, null, AParams, FScript.SourceContext);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		public void UnprepareStatement(IServerStatementPlan APlan)
		{
			try
			{
				FScript.Process.UnprepareStatement(APlan);
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
}
