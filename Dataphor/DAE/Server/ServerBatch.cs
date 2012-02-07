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
		internal ServerBatch(ServerScript script, Statement batch) : base()
		{
			_script = script;
			_batch = batch;
		}
		
		protected override void Dispose(bool disposing)
		{
			_script = null;
			_batch = null;
			base.Dispose(disposing);
		}

		private ServerScript _script;
		public ServerScript Script { get { return _script; } }
		
		IServerScript IServerBatch.ServerScript { get { return _script; } }

		private Statement _batch;
		
		public int Line { get { return _batch.Line; } }
		
		public bool IsExpression()
		{
			return _batch is SelectStatement;
		}
		
		public string GetText()
		{
			return new D4TextEmitter().Emit(_batch);
		}
		
		public void Execute(DataParams paramsValue)
		{
			try
			{
				if (IsExpression())
				{
					IServerExpressionPlan plan = PrepareExpression(paramsValue);
					try
					{
						if (plan.DataType is Schema.TableType)
							plan.Close(plan.Open(paramsValue));
						else
							DataValue.DisposeValue(this.Script.Process.ValueManager, plan.Evaluate(paramsValue));
					}
					finally
					{
						UnprepareExpression(plan);
					}
				}
				else
				{
					IServerStatementPlan plan = PrepareStatement(paramsValue);
					try
					{
						plan.Execute(paramsValue);
					}
					finally
					{
						UnprepareStatement(plan);
					}
				}
			}
			catch (Exception E)
			{
				throw _script.Process.ServerSession.WrapException(E);
			}
		}
		
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
			try
			{
				_script.CheckParsed();
				return (IServerExpressionPlan)_script.Process.CompileExpression(_batch, null, paramsValue, _script.SourceContext);
			}
			catch (Exception E)
			{
				throw _script.Process.ServerSession.WrapException(E);
			}
		}
		
		public void UnprepareExpression(IServerExpressionPlan plan)
		{
			try
			{
				_script.Process.UnprepareExpression(plan);
			}
			catch (Exception E)
			{
				throw _script.Process.ServerSession.WrapException(E);
			}
		}
		
		public IServerStatementPlan PrepareStatement(DataParams paramsValue)
		{
			try
			{
				_script.CheckParsed();
				return (IServerStatementPlan)_script.Process.CompileStatement(_batch, null, paramsValue, _script.SourceContext);
			}
			catch (Exception E)
			{
				throw _script.Process.ServerSession.WrapException(E);
			}
		}
		
		public void UnprepareStatement(IServerStatementPlan plan)
		{
			try
			{
				_script.Process.UnprepareStatement(plan);
			}
			catch (Exception E)
			{
				throw _script.Process.ServerSession.WrapException(E);
			}
		}
	}
	
	// ServerBatches
	public class ServerBatches : ServerChildObjects, IServerBatches
	{		
		protected override void Validate(ServerChildObject objectValue)
		{
			if (!(objectValue is ServerBatch))
				throw new ServerException(ServerException.Codes.ServerBatchContainer);
		}
		
		public new ServerBatch this[int index]
		{
			get { return (ServerBatch)base[index]; }
			set { base[index] = value; }
		}
		
		IServerBatch IServerBatches.this[int index]
		{
			get { return (IServerBatch)base[index]; } 
			set { base[index] = (ServerBatch)value; } 
		}
		
		public ServerBatch[] All
		{
			get
			{
				ServerBatch[] array = new ServerBatch[Count];
				for (int index = 0; index < Count; index++)
					array[index] = this[index];
				return array;
			}
			set
			{
				foreach (ServerBatch batch in value)
					Add(batch);
			}
		}
	}
}
