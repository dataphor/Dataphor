/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Server
{
	public abstract class ServerPlan : ServerChildObject, IServerPlanBase, IServerPlan
	{        
		protected internal ServerPlan(ServerProcess process) : base()
		{
			_process = process;
			_plan = new Plan(process);
			_program = new Program(process, _iD);
			_program.ShouldPushLocals = true;
		}
		
		private bool _disposed;
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					if (_activeCursor != null)
						_activeCursor.Dispose();
				}
				finally
				{
					if (_plan != null)
					{
						_plan.Dispose();
						_plan = null;
					}
					
					if (!_disposed)
						_disposed = true;
				}
			}
			finally
			{
				_program = null;
				_process = null;
	            
				base.Dispose(disposing);
			}
		}

		// ID		
		private Guid _iD = Guid.NewGuid();
		public Guid ID { get { return _iD; } }
		
		// CachedPlanHeader
		private CachedPlanHeader _header;
		public CachedPlanHeader Header
		{ 
			get { return _header; } 
			set { _header = value; }
		}
		
		// PlanCacheTimeStamp
		public long PlanCacheTimeStamp; // Server.PlanCacheTimeStamp when the plan was compiled

		// Plan		
		protected Plan _plan;
		public Plan Plan { get { return _plan; } }
		
		// Program
		protected Program _program;
		public Program Program { get { return _program; } }
		
		// Statistics
		public PlanStatistics PlanStatistics { get { return _plan.Statistics; } }
		public ProgramStatistics ProgramStatistics { get { return _program.Statistics; } }
		
		// Process        
		protected ServerProcess _process;
		public ServerProcess ServerProcess { get { return _process; } }
		
		public IServerProcess Process  { get { return (IServerProcess)_process; } }
		
		public CompilerMessages Messages { get { return _plan.Messages; } }

		public void BindToProcess(ServerProcess process)
		{
			_process = process;
			_plan.BindToProcess(process);
			_program.BindToProcess(process, _plan);
		}
		
		public void UnbindFromProcess()
		{
			_process = null;
			_plan.UnbindFromProcess();
			_program.UnbindFromProcess();
		}
		
		// Released
		/// <summary>
		/// Used to indicate that the plan has been released back to the cache and should be considered disposed for anything looking at the plan external to the server.
		/// </summary>
		public event EventHandler Released;
		
		protected void DoReleased()
		{
			if (Released != null)
				Released(this, EventArgs.Empty);
		}
		
		public void NotifyReleased()
		{
			DoReleased();
		}

		// ActiveCursor
		protected ServerCursor _activeCursor;
		public ServerCursor ActiveCursor { get { return _activeCursor; } }
		
		public void SetActiveCursor(ServerCursor activeCursor)
		{
			if (_activeCursor != null)
				throw new ServerException(ServerException.Codes.PlanCursorActive);
			_activeCursor = activeCursor;
			//FProcess.SetActiveCursor(FActiveCursor);
		}
		
		public void ClearActiveCursor()
		{
			_activeCursor = null;
			//FProcess.ClearActiveCursor();
		}
		
		public virtual Statement EmitStatement()
		{
			CheckCompiled();
			return _program.Code.EmitStatement(EmitMode.ForCopy);
		}

		public void WritePlan(System.Xml.XmlWriter writer)
		{
			CheckCompiled();
			_program.Code.WritePlan(writer);
		}
		
		protected Exception WrapException(Exception exception)
		{
			return _process.ServerSession.WrapException(exception);
		}
		
		public void CheckCompiled()
		{
			_plan.CheckCompiled();
		}
	}
	
	// ServerPlans    
	public class ServerPlans : ServerChildObjects
	{		
		protected override void Validate(ServerChildObject objectValue)
		{
			if (!(objectValue is ServerPlan))
				throw new ServerException(ServerException.Codes.ServerPlanContainer);
		}
		
		public new ServerPlan this[int index]
		{
			get { return (ServerPlan)base[index]; }
			set { base[index] = value; }
		}
		
		public int IndexOf(Guid planID)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].ID == planID)
					return index;
			return -1;
		}
		
		public bool Contains(Guid planID)
		{
			return IndexOf(planID) >= 0;
		}
		
		public ServerPlan this[Guid planID]
		{
			get
			{
				int index = IndexOf(planID);
				if (index >= 0)
					return this[index];
				throw new ServerException(ServerException.Codes.PlanNotFound, planID);
			}
		}
	}

	// ServerStatementPlan	
	public class ServerStatementPlan : ServerPlan, IServerStatementPlan
	{
		public ServerStatementPlan(ServerProcess process) : base(process) {}
		
		public void Execute(DataParams paramsValue)
		{
			Exception exception = null;
			int nestingLevel = _process.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				_program.Execute(paramsValue);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_process.EndTransactionalCall(nestingLevel, exception);
			}
		}
	}
	
	// ServerExpressionPlan
	public class ServerExpressionPlan : ServerPlan, IServerExpressionPlan
	{
		protected internal ServerExpressionPlan(ServerProcess process) : base(process) {}
		
		public Schema.IDataType DataType
		{
			get
			{
				CheckCompiled();
				return _program.DataType;
			}
		}
		
		public Schema.IDataType ActualDataType
		{
			get
			{
				CheckCompiled();
				return _program.Code.DataType;
			}
		}
		
		public IDataValue Evaluate(DataParams paramsValue)
		{
			Exception exception = null;
			int nestingLevel = _process.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				object result = _program.Execute(paramsValue);
				DataValue dataValue = result as DataValue;
				if (dataValue != null)
					return dataValue;

				return DataValue.FromNative(_program.ValueManager, _program.DataType, result);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(exception);
			}
			finally
			{
				_process.EndTransactionalCall(nestingLevel, exception);
			}
		}

		public IServerCursor Open(DataParams paramsValue)
		{
			IServerCursor serverCursor;
			//ServerProcess.RaiseTraceEvent(TraceCodes.BeginOpenCursor, "Begin Open Cursor");
			Exception exception = null;
			int nestingLevel = _process.BeginTransactionalCall();
			try
			{
				CheckCompiled();

				#if TIMING
				DateTime startTime = DateTime.Now;
				System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerExpressionPlan.Open", DateTime.Now.ToString("hh:mm:ss.ffff")));
				#endif
				ServerCursor cursor = new ServerCursor(this, _program, paramsValue);
				try
				{
					cursor.Open();
					#if TIMING
					System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerExpressionPlan.Open -- Open Time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - startTime).ToString()));
					#endif
					serverCursor = (IServerCursor)cursor;
				}
				catch
				{
					Close((IServerCursor)cursor);
					throw;
				}
			}
			catch (Exception E)
			{
				if (Header != null)
					Header.IsInvalidPlan = true;

				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_process.EndTransactionalCall(nestingLevel, exception);
			}
			//ServerProcess.RaiseTraceEvent(TraceCodes.EndOpenCursor, "End Open Cursor");
			return serverCursor;
		}
		
		public void Close(IServerCursor cursor)
		{
			Exception exception = null;
			int nestingLevel = _process.BeginTransactionalCall();
			try
			{
				((ServerCursor)cursor).Dispose();
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_process.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		private Schema.TableVar _tableVar;
		Schema.TableVar IServerExpressionPlan.TableVar 
		{ 
			get 
			{ 
				CheckCompiled();
				if (_tableVar == null)
					_tableVar = (Schema.TableVar)_plan.PlanCatalog[Schema.Object.NameFromGuid(ID)];
				return _tableVar; 
			} 
		}
		
		Schema.Catalog IServerExpressionPlan.Catalog { get { return _plan.PlanCatalog; } }
		
		public IRow RequestRow()
		{
			CheckCompiled();
			return new Row(_process.ValueManager, SourceNode.DataType.RowType);
		}
		
		public void ReleaseRow(IRow row)
		{
			CheckCompiled();
			row.Dispose();
		}
		
		public override Statement EmitStatement()
		{
			CheckCompiled();
			return _program.Code.EmitStatement(EmitMode.ForRemote);
		}

		// SourceNode
		internal TableNode SourceNode { get { return (TableNode)_program.Code.Nodes[0]; } }
		
		internal CursorNode CursorNode { get { return (CursorNode)_program.Code; } }
        
		// Isolation
		public CursorIsolation Isolation 
		{
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorIsolation; 
			}
		}
		
		// CursorType
		public CursorType CursorType 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorType; 
			} 
		}

		// Capabilities		
		public CursorCapability Capabilities 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorCapabilities; 
			} 
		}

		public bool Supports(CursorCapability capability)
		{
			return (Capabilities & capability) != 0;
		}
		
		// Order
		public Schema.Order Order
		{
			get
			{
				CheckCompiled();
				return SourceNode.Order;
			}
		}
	}
}
