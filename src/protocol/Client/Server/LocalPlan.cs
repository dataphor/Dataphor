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
		
		/// <summary>
		/// Whether the schema this plan was compiled against is still the newest generation of the catalog the client has
		/// been told about, and so may be trusted to answer a row proposal - Default, Change or Validate - out of the
		/// client's own copy of the schema rather than by calling the server. LocalCursor's three proposal calls are the
		/// callers.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Permission to answer locally is computed once, when the plan is prepared, and sent with the plan descriptor as
		/// the DAE.IsChangeRemotable, DAE.IsDefaultRemotable and DAE.IsValidateRemotable tags of the plan's table variable
		/// (ServerProcess.CopyDataType), each meaning "the work is remotable, **or** there is no work". A form holds its
		/// plan for as long as the user keeps the form open, and nothing re-sends either the permission or the copy of the
		/// schema it is exercised against - so a data type that gains a check, a default or a handler after the plan was
		/// prepared is invisible to that plan, and the user meets the error on saving rather than on leaving the field.
		/// Denying the local branch sends the proposal to the server, which answers from the live schema.
		/// </para>
		/// <para>
		/// The two values compared are the generation recorded in this plan's own descriptor and the highest generation the
		/// client has been told about, which it already keeps in LocalServer. Deliberately not LocalServer.CacheTimeStamp,
		/// which asks the server for its current value and so costs the round trip this permission exists to avoid.
		/// </para>
		/// <para>
		/// The client learns of a newer generation in LocalServer.EnsureCacheConsistent, which every prepared
		/// *expression* reaches because RemoteServerPlan.GetCatalog reports the catalog cache as changed on every
		/// prepare (a catalog lookup miss on the client, LocalProcess.CatalogLookupFailed, is the other way in). So any
		/// other expression the session prepares or evaluates - a lookup, a detail view, another form - brings this up to
		/// date at no cost; a *statement* prepared or executed does not, because nothing on that path reaches
		/// EnsureCacheConsistent. That also makes the test worth having for a second reason: raising the client's value
		/// *clears the client's whole catalog copy* and rebuilds it, while an open plan's table variable and plan node
		/// are not rebuilt with it, so a local proposal on such a plan would be running against schema objects detached
		/// from the catalog they were compiled against.
		/// </para>
		/// <para>
		/// Catalog.CacheTimeStamp rather than Catalog.ShouldCallProposablesTimeStamp, which is the key the server side uses
		/// for this same summary: that one is deliberately coarse, bumped by DDLNode.InternalBeforeExecute for every
		/// statement that changes the catalog - including the create and drop of the per-table copies an application
		/// transaction makes, which is a form's own edit cycle. Server-side a recompute costs microseconds, so coarse is
		/// affordable there; here the cost is a network round trip, and a client keyed on that counter would be sent remote
		/// for almost every proposal almost immediately, which is this optimization gone. CacheTimeStamp is gated on
		/// ServerProcess.ShouldAffectTimeStamp and therefore means "a change a client could care about".
		/// </para>
		/// <para>
		/// The read is deliberately unsynchronized, where every write to that field is made under the client's exclusive
		/// cache lock: taking even a shared lock here would be a lock per proposal taken to decide whether to take a lock,
		/// and because the field only ever rises, a stale read can only read it low - which makes the test more permissive,
		/// never less. So the worst it can do is answer one proposal the way it would have been answered before this test
		/// existed; it can never report a current plan as behind, and so can never cost a round trip that is not needed.
		/// </para>
		/// </remarks>
		internal bool IsSchemaCurrent
		{
			get
			{
				// NOTE: a session that prepares or evaluates no expression at all between the catalog change and the user's edit
				// still takes the local branch, and still reports the change only when the row is saved - a form that only posts
				// statements included, because nothing on that path raises the client's value. Closing that needs either a round
				// trip per proposal - the exact cost this permission exists to avoid - or the server telling the client unasked,
				// for which this architecture has no channel: every client-cache mechanism in it is pull-based, carried on the
				// response to a request the client made.
				// Revisit if a per-proposal round trip ever becomes acceptable, or if a server-to-client notification channel is
				// built for some other reason and this could ride on it.
				return _descriptor.CacheTimeStamp >= LocalServer._serverCacheTimeStamp;
			}
		}
		
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
