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

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalPlan : LocalServerChildObject, IServerPlan
    {
		public LocalPlan(LocalProcess process, IRemoteServerPlan plan, PlanDescriptor planDescriptor) : base()
		{
			_process = process;
			_plan = plan;
			_descriptor = planDescriptor;
		}
		
		protected override void Dispose(bool disposing)
		{
			_descriptor = null;
			_process = null;
			_plan = null;
			base.Dispose(disposing);
		}
		
		private IRemoteServerPlan _plan;
		
		protected PlanDescriptor _descriptor;
		
		protected internal LocalProcess _process;
        /// <value>Returns the <see cref="IServerProcess"/> instance for this plan.</value>
        public IServerProcess Process { get { return _process; } } 

		public LocalServer LocalServer { get { return _process._session._server; } }
		
		public Guid ID { get { return _descriptor.ID; } }
		
		private CompilerMessages _messages;
		public CompilerMessages Messages
		{
			get
			{
				if (_messages == null)
				{
					_messages = new CompilerMessages();
					foreach (DataphorFault fault in _descriptor.Messages)
						_messages.Add(DataphorFaultUtility.FaultToException(fault));
				}
				return _messages;
			}
		}
		
		public void CheckCompiled()
		{
			_plan.CheckCompiled();
		}
		
		// Statistics
		internal bool _planStatisticsCached = true;
		public PlanStatistics PlanStatistics 
		{ 
			get 
			{ 
				if (!_planStatisticsCached)
				{
					_descriptor.Statistics = _plan.PlanStatistics;
					_planStatisticsCached = true;
				}
				return _descriptor.Statistics; 
			} 
		}
		
		internal bool _programStatisticsCached = true;
		internal ProgramStatistics _programStatistics = new ProgramStatistics();
		public ProgramStatistics ProgramStatistics 
		{ 
			get 
			{ 
				if (!_programStatisticsCached)
				{
					_programStatistics = _plan.ProgramStatistics; 
					_programStatisticsCached = true;
				}
				return _programStatistics;
			}
		}
	}
    
    public class LocalExpressionPlan : LocalPlan, IServerExpressionPlan
    {
		public LocalExpressionPlan(LocalProcess process, IRemoteServerExpressionPlan plan, PlanDescriptor planDescriptor, DataParams paramsValue, ProgramStatistics executeTime) : this(process, plan, planDescriptor, paramsValue)
		{
			_programStatistics = executeTime;
			_programStatisticsCached = true;
		}
		
		public LocalExpressionPlan(LocalProcess process, IRemoteServerExpressionPlan plan, PlanDescriptor planDescriptor, DataParams paramsValue) : base(process, plan, planDescriptor)
		{
			_plan = plan;
			_params = paramsValue;
			_internalPlan = new Plan(_process._internalProcess);
			GetDataType();
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_dataType != null)
					DropDataType();
			}
			finally
			{
				if (_internalPlan != null)
				{
					_internalPlan.Dispose();
					_internalPlan = null;
				}

				_plan = null;
				_params = null;
				_dataType = null;
				base.Dispose(disposing);
			}
		}

		protected DataParams _params;
		protected IRemoteServerExpressionPlan _plan;
		public IRemoteServerExpressionPlan RemotePlan { get { return _plan; } }

		private Plan _internalPlan;
		
		// Isolation
		public CursorIsolation Isolation { get { return _descriptor.CursorIsolation; } }
		
		// CursorType
		public CursorType CursorType { get { return _descriptor.CursorType; } }
		
		// Capabilities		
		public CursorCapability Capabilities { get { return _descriptor.Capabilities; } }

		public bool Supports(CursorCapability capability)
		{
			return (Capabilities & capability) != 0;
		}
		
		public IDataValue Evaluate(DataParams paramsValue)
		{
			RemoteParamData localParamsValue = _process.DataParamsToRemoteParamData(paramsValue);
			byte[] result = _plan.Evaluate(ref localParamsValue, out _programStatistics, _process.GetProcessCallInfo());
			_programStatisticsCached = false;
			_process.RemoteParamDataToDataParams(paramsValue, localParamsValue);
			return result == null ? null : DataValue.FromPhysical(_process.ValueManager, DataType, result, 0);
		}

        /// <summary>Opens a server-side cursor based on the prepared statement this plan represents.</summary>        
        /// <returns>An <see cref="IServerCursor"/> instance for the prepared statement.</returns>
        public IServerCursor Open(DataParams paramsValue)
        {
			RemoteParamData localParamsValue = ((LocalProcess)_process).DataParamsToRemoteParamData(paramsValue);
			IRemoteServerCursor serverCursor;
			LocalCursor cursor;
			if (_process.ProcessInfo.FetchAtOpen && (_process.ProcessInfo.FetchCount > 1) && Supports(CursorCapability.Bookmarkable))
			{
				Guid[] bookmarks;
				RemoteFetchData fetchData;
				serverCursor = _plan.Open(ref localParamsValue, out _programStatistics, out bookmarks, _process.ProcessInfo.FetchCount, out fetchData, _process.GetProcessCallInfo());
				_programStatisticsCached = false;
				cursor = new LocalCursor(this, serverCursor);
				cursor.ProcessFetchData(fetchData, bookmarks, true);
			}
			else
			{
				serverCursor = _plan.Open(ref localParamsValue, out _programStatistics, _process.GetProcessCallInfo());
				_programStatisticsCached = false;
				cursor = new LocalCursor(this, serverCursor);
			}
			((LocalProcess)_process).RemoteParamDataToDataParams(paramsValue, localParamsValue);
			return cursor;
		}
		
        /// <summary>Closes a server-side cursor previously created using Open.</summary>
        /// <param name="cursor">The cursor to close.</param>
        public void Close(IServerCursor cursor)
        {
			try
			{
				_plan.Close(((LocalCursor)cursor).RemoteCursor, _process.GetProcessCallInfo());
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalCursor)cursor).Dispose();
		}
		
		private void DropDataType()
		{
			if (_tableVar is Schema.DerivedTableVar)
			{
				LocalServer.AcquireCacheLock(_process, LockMode.Exclusive);
				try
				{
					Program program = new Program(_process._internalProcess);
					program.Code = new DropViewNode((Schema.DerivedTableVar)_tableVar);
					program.Execute(null);
				}
				finally
				{
					LocalServer.ReleaseCacheLock(_process, LockMode.Exclusive);
				}
			}
		}
		
		private Schema.IDataType GetDataType()
		{
			bool timeStampSet = false;
			try
			{
				LocalServer.WaitForCacheTimeStamp(_process, _descriptor.CacheChanged ? _descriptor.ClientCacheTimeStamp - 1 : _descriptor.ClientCacheTimeStamp);
				LocalServer.AcquireCacheLock(_process, _descriptor.CacheChanged ? LockMode.Exclusive : LockMode.Shared);
				try
				{
					if (_descriptor.CacheChanged)
					{
						LocalServer.EnsureCacheConsistent(_descriptor.CacheTimeStamp);
						try
						{
							if (_descriptor.Catalog != String.Empty)
							{
								IServerScript script = ((IServerProcess)_process._internalProcess).PrepareScript(_descriptor.Catalog);
								try
								{
									script.Execute(_params);
								}
								finally
								{
									((IServerProcess)_process._internalProcess).UnprepareScript(script);
								}
							}
						}
						finally
						{
							LocalServer.SetCacheTimeStamp(_process, _descriptor.ClientCacheTimeStamp);
							timeStampSet = true;
						}
					}
					
					if (LocalServer.Catalog.ContainsName(_descriptor.ObjectName))
					{
						Schema.Object objectValue = LocalServer.Catalog[_descriptor.ObjectName];
						if (objectValue is Schema.TableVar)
						{
							_tableVar = (Schema.TableVar)objectValue;
							Plan plan = new Plan(_process._internalProcess);
							try
							{
								if (_params != null)
									foreach (DataParam param in _params)
										plan.Symbols.Push(new Symbol(param.Name, param.DataType));
										
								foreach (DataParam param in _process._internalProcess.ProcessLocals)
									plan.Symbols.Push(new Symbol(param.Name, param.DataType));
										
								_tableNode = (TableNode)Compiler.EmitTableVarNode(plan, _tableVar);
							}
							finally
							{
								plan.Dispose();
							}
							_dataType = _tableVar.DataType;			
						}
						else
							_dataType = (Schema.IDataType)objectValue;
					}
					else
					{
						try
						{
							Plan plan = new Plan(_process._internalProcess);
							try
							{
								_dataType = Compiler.CompileTypeSpecifier(plan, new DAE.Language.D4.Parser().ParseTypeSpecifier(_descriptor.ObjectName));
							}
							finally
							{
								plan.Dispose();
							}
						}
						catch
						{
							// Notify the server that the client cache is out of sync
							Process.Execute(".System.UpdateTimeStamps();", null);
							throw;
						}
					}
					
					return _dataType;
				}
				finally
				{
					LocalServer.ReleaseCacheLock(_process, _descriptor.CacheChanged ? LockMode.Exclusive : LockMode.Shared);
				}
			}
			catch (Exception E)
			{
				// Notify the server that the client cache is out of sync
				Process.Execute(".System.UpdateTimeStamps();", null);
				E = new ServerException(ServerException.Codes.CacheDeserializationError, E, _descriptor.ClientCacheTimeStamp);
				LocalServer._internalServer.LogError(E);
				throw E;
			}
			finally
			{
				if (!timeStampSet)
					LocalServer.SetCacheTimeStamp(_process, _descriptor.ClientCacheTimeStamp);
			}
		}
		
		public Schema.Catalog Catalog 
		{ 
			get 
			{ 
				if (_dataType == null)
					GetDataType();
				return LocalServer.Catalog;
			} 
		}

		protected Schema.TableVar _tableVar;		
		public Schema.TableVar TableVar
		{
			get
			{
				if (_dataType == null)
					GetDataType();
				return _tableVar;
			}
		}
		
		protected TableNode _tableNode;
		public TableNode TableNode
		{
			get
			{
				if (_dataType == null)
					GetDataType();
				return _tableNode;
			}
		}
		
		protected Schema.IDataType _dataType;
		public Schema.IDataType DataType
		{
			get
			{
				if (_dataType == null)
					return GetDataType(); 
				return _dataType;
			}
		}

		private Schema.Order _order;
		public Schema.Order Order
        {
			get 
			{
				if ((_order == null) && (_descriptor.Order != String.Empty))
					_order = Compiler.CompileOrderDefinition(_internalPlan, TableVar, new Parser().ParseOrderDefinition(_descriptor.Order), false);
				return _order; 
			}
		}
		
		Statement IServerExpressionPlan.EmitStatement()
		{
			throw new ServerException(ServerException.Codes.Unsupported);
		}
		
		public IRow RequestRow()
		{
			return new Row(_process.ValueManager, TableVar.DataType.RowType);
		}
		
		public void ReleaseRow(IRow row)
		{
			row.Dispose();
		}
    }
    
    public class LocalStatementPlan : LocalPlan, IServerStatementPlan
    {
		public LocalStatementPlan(LocalProcess process, IRemoteServerStatementPlan plan, PlanDescriptor planDescriptor) : base(process, plan, planDescriptor)
		{
			_plan = plan;
		}

		protected override void Dispose(bool disposing)
		{
			_plan = null;
			base.Dispose(disposing);
		}

		protected IRemoteServerStatementPlan _plan;
		public IRemoteServerStatementPlan RemotePlan { get { return _plan; } }
		
        /// <summary>Executes the prepared statement this plan represents.</summary>
        public void Execute(DataParams paramsValue)
        {
			RemoteParamData localParamsValue = _process.DataParamsToRemoteParamData(paramsValue);
			_plan.Execute(ref localParamsValue, out _programStatistics, _process.GetProcessCallInfo());
			_programStatisticsCached = false;
			_process.RemoteParamDataToDataParams(paramsValue, localParamsValue);
		}
    }
}
