/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define USEPROCESSDISPOSED // Determines whether or not the plan and program listen to the process disposed event

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	/// <summary>
	/// Represents the run-time aspects of a compiled D4 program.
	/// </summary>
	public class Program
	{
		public Program(ServerProcess process) : this(process, Guid.NewGuid()) { }
		public Program(ServerProcess process, Guid iD)
		{
			SetServerProcess(process);
			_iD = iD;
			_stack = new Stack(_serverProcess.MaxStackDepth, _serverProcess.MaxCallDepth);
		}
		
		private ServerProcess _serverProcess;
		public ServerProcess ServerProcess { get { return _serverProcess; } }
		 
		private void SetServerProcess(ServerProcess serverProcess)
		{
			#if USEPROCESSDISPOSED 
			if (FServerProcess != null)
				FServerProcess.Disposed -= new EventHandler(ServerProcessDisposed);
			#endif
			
			_serverProcess = serverProcess;
			
			#if USEPROCESSDISPOSED
			if (FServerProcess != null)
				FServerProcess.Disposed += new EventHandler(ServerProcessDisposed);
			#endif
		}
		
		private void ServerProcessDisposed(object sender, EventArgs args)
		{
			SetServerProcess(null);
		}
		
		private bool _isCached;
		public bool IsCached
		{
			get { return _isCached; }
			set { _isCached = value; }
		}
		
		public void BindToProcess(ServerProcess process, Plan plan)
		{
			SetServerProcess(process);
			
			if (_code != null)
				_code.BindToProcess(plan);

			if (_plan != null)
				_plan.BindToProcess(_serverProcess);
			
			// Reset execution time
			_statistics.ExecuteTime = TimeSpan.Zero;
			_statistics.DeviceExecuteTime = TimeSpan.Zero;
		}
		
		public void UnbindFromProcess()
		{
			#if USEPROCESSUNBIND
			SetServerProcess(null);
			if (_plan != null)
				_plan.UnbindFromProcess();
			#endif
		}
		
		private Guid _iD;
		public Guid ID { get { return _iD; } }
		
		public int DefaultMaxStackDepth
		{
			get { return _serverProcess.ServerSession.SessionInfo.DefaultMaxStackDepth; }
			set { _serverProcess.ServerSession.SessionInfo.DefaultMaxStackDepth = value; }
		}
		
		public int DefaultMaxCallDepth
		{
			get { return _serverProcess.ServerSession.SessionInfo.DefaultMaxCallDepth; }
			set { _serverProcess.ServerSession.SessionInfo.DefaultMaxCallDepth = value; }
		}
		
		private Stack _stack;
		public Stack Stack { get { return _stack; } }
		
		public Stack SwitchContext(Stack context)
		{
			Stack localContext = _stack;
			_stack = context;
			return localContext;
		}

		private ProgramStatistics _statistics = new ProgramStatistics();
		public ProgramStatistics Statistics { get { return _statistics; } }

		// Code
		protected PlanNode _code;
		public PlanNode Code
		{
			get { return _code; }
			set { _code = value; }
		}
		
		// DataType
		protected Schema.IDataType _dataType;
		public Schema.IDataType DataType 
		{ 
			get { return _dataType; }
			set { _dataType = value; } 
		}
		
		// ProcessLocals - New local variables declared by allocation statements in the program
		private DataParams _processLocals = new DataParams();
		public DataParams ProcessLocals { get { return _processLocals; } }
		
		private bool _shouldPushLocals;
		/// <summary>
		/// Indicates whether or not process local variables should be pushed onto the program's stack.
		/// </summary>
		public bool ShouldPushLocals
		{
			get { return _shouldPushLocals; }
			set { _shouldPushLocals = value; }
		}
		
		// Used to track the set of process local variables pushed when the program was started.
		private DataParams _localParams;
		
		// Source
		protected string _source;
		/// <summary>
		/// Contains the source text for the program. Only present if no debug locator is provided.
		/// </summary>
		public string Source
		{ 
			get 
			{
				if (_source != null) 
					return _source;
					
				if (_code != null)
					return _code.SafeEmitStatementAsString(false);
					
				return "<Program has no source>";
			} 
		}
		
		// Locator
		protected DebugLocator _locator;
		/// <summary>
		/// Provides a reference for identifying the source text for the program. May be null for dynamic or ad-hoc execution.
		/// </summary>
		public DebugLocator Locator 
		{ 
			get 
			{ 
				if (_locator == null)
					_locator = new DebugLocator(DebugLocator.ProgramLocator(_iD), -1, -1);
				return _locator; 
			} 
		}
		
		/// <summary>
		/// Sets the source context for the program.
		/// </summary>
		public void SetSourceContext(SourceContext sourceContext)
		{
			// Clear existing context
			_source = null;
			_locator = null;
			
			if (sourceContext.Locator != null)
				_locator = sourceContext.Locator;
			else
			{
				_locator = new DebugLocator(DebugLocator.ProgramLocator(this.ID), 1, 1);
				_source = sourceContext.Script;
			}
		}
		
		// Devices
		public Schema.DeviceSession DeviceConnect(Schema.Device device)
		{
			return _serverProcess.DeviceConnect(device);
		}
		
		public object DeviceExecute(Schema.Device device, PlanNode planNode)
		{	
			if (_serverProcess.IsReconciliationEnabled() || (planNode.DataType != null))
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return DeviceConnect(device).Execute(this, planNode);
				}
				finally
				{
					_statistics.DeviceExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}

			return null;
		}
		
		// Remote Sessions
		public RemoteSession RemoteConnect(Schema.ServerLink link)
		{
			return _serverProcess.RemoteConnect(link);
		}
		
		// Plan
		private Plan _plan;
		public Plan Plan 
		{ 
			get 
			{ 
				if (_plan == null)
					_plan = new Plan(_serverProcess);
				return _plan;
			}
		}
		
		public Schema.LoadedLibrary CurrentLibrary { get { return _serverProcess.ServerSession.CurrentLibrary; } }
		
		public Schema.User User { get { return Plan.User; } }
		
		// Catalog
		public Schema.Catalog Catalog { get { return _serverProcess.Catalog; } }
		
		public CatalogDeviceSession CatalogDeviceSession { get { return _serverProcess.CatalogDeviceSession; } }
		
		public Schema.DataTypes DataTypes { get { return _serverProcess.DataTypes; } }
		
		public Schema.Device TempDevice { get { return _serverProcess.ServerSession.Server.TempDevice; } }
		
		// Values
		public IValueManager ValueManager { get { return _serverProcess.ValueManager; } }
		
		// Streams
		public IStreamManager StreamManager { get { return _serverProcess.StreamManager; } }
		
		// Cursors
		public CursorManager CursorManager { get { return _serverProcess.ServerSession.CursorManager; } }

		// Execution
		public void Start(DataParams paramsValue)
		{
			_stack.PushWindow(0, null, Locator);
			try
			{
				_serverProcess.PushExecutingProgram(this);
				try
				{
					_localParams = new DataParams();
					DataParams localParamsValue = new DataParams();
					if (_shouldPushLocals)
						foreach (DataParam param in _serverProcess.ProcessLocals)
							if (!ProcessLocals.Contains(param.Name))
							{
								_localParams.Add(param);
								localParamsValue.Add(param);
							}
					
					if (paramsValue != null)
						foreach (DataParam param in paramsValue)
							localParamsValue.Add(param);
							
					foreach (DataParam param in localParamsValue)
						_stack.Push(param.Modifier == Modifier.In ? DataValue.CopyValue(ValueManager, param.Value) : param.Value);
						
					// Set the BreakNext flag for the process if the debugger is set to Break On Start
					ReportStart();
				}
				catch
				{
					_serverProcess.PopExecutingProgram(this);
					throw;
				}
			}
			catch
			{
				_stack.PopWindow();
				throw;
			}
		}
		
		public void Stop(DataParams paramsValue)
		{
			try
			{
				try
				{
					DataParams localParamsValue = new DataParams();
					foreach (DataParam param in _localParams)
						localParamsValue.Add(param);
						
					if (paramsValue != null)
						foreach (DataParam param in paramsValue)
							localParamsValue.Add(param);
							
					for (int index = ProcessLocals.Count - 1; index >= 0; index--)
					{
						ProcessLocals[index].Value = _stack.Pop();
						_serverProcess.AddProcessLocal(ProcessLocals[index]);
					}
							
					for (int index = localParamsValue.Count - 1; index >= 0; index--)
					{
						object tempValue = _stack.Pop();
						if (localParamsValue[index].Modifier != Modifier.In)
							localParamsValue[index].Value = tempValue;
					}
				}
				finally
				{
					_serverProcess.PopExecutingProgram(this);
				}
			}
			finally
			{
				_stack.PopWindow();
			}
		}
		
		public object Execute(DataParams paramsValue)
		{	
			object result;
			Start(paramsValue);
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				result = _code.Execute(this);
				_statistics.ExecuteTime = TimingUtility.TimeSpanFromTicks(startTicks);
			}
			finally
			{
				Stop(paramsValue);
			}
			return result;
		}
		
		private PlanNode _currentNode;
		public PlanNode CurrentNode { get { return _currentNode; } }
		
		private bool _afterNode;
		public bool AfterNode { get { return _afterNode; } }
		
		public void ReportStart()
		{
			Debugger debugger = _serverProcess.DebuggedBy;
			if ((debugger != null) && debugger.BreakOnStart)
				_serverProcess.SetStepInto();
		}
		
		public void ReportThrow()
		{
			Debugger debugger = _serverProcess.DebuggedBy;
			if ((debugger != null) && debugger.BreakOnException)
				_serverProcess.SetStepInto();
		}
		
		public void Yield(PlanNode planNode, bool afterNode)
		{
			if (_serverProcess.IsAborted)
				throw new ServerException(ServerException.Codes.ProcessAborted);

			// Double-check debugger here to optimize for the case that there is no debugger
			// With this check first, if there is no debugger we've saved an assignment
			if (_serverProcess.DebuggedBy != null)
			{
				Debugger debugger = _serverProcess.DebuggedBy;
				if (debugger != null)
				{
					_currentNode = planNode;
					_afterNode = afterNode;
					debugger.Yield(_serverProcess, planNode);
				}
			}
		}
		
		public void CheckAborted()
		{
			if (_serverProcess.IsAborted)
				throw new ServerException(ServerException.Codes.ProcessAborted);
		}
		
		public DebugLocator GetLocation(PlanNode planNode, bool afterNode)
		{
			try
			{
				// Current location is the line/linepos of the current node, with the locator as the current locator on the call stack.
				return 
					new DebugLocator
					(
						((RuntimeStackWindow)_stack.CurrentStackWindow).Locator, 
						planNode == null ? -1 : (afterNode && planNode.EndLine >= 0 ? planNode.EndLine : planNode.Line), 
						planNode == null ? -1 : ((afterNode && planNode.Line != planNode.EndLine && planNode.EndLinePos >= 0) ? planNode.EndLinePos : planNode.LinePos)
					);
			}
			catch (Exception E)
			{
				throw new ServerException(ServerException.Codes.CouldNotDetermineProgramLocation, E, _iD);
			}
		}
		
		public DebugLocator GetCurrentLocation()
		{
			return GetLocation(_currentNode, _afterNode);
		}
		
		public DebugLocator SafeGetCurrentLocation()
		{
			try
			{
				return GetCurrentLocation();
			}
			catch (Exception E)
			{
				return new DebugLocator(E.Message, -1, -1);
			}
		}
		
		// Run-time Compilation
		public void EnsureKey(Schema.TableVar tableVar)
		{
			Compiler.EnsureKey(Plan, tableVar);
		}
		
		public Schema.Key FindKey(Schema.TableVar tableVar, KeyDefinitionBase keyDefinition)
		{
			return Compiler.FindKey(Plan, tableVar, keyDefinition);
		}
		
		public Schema.Key FindClusteringKey(Schema.TableVar tableVar)
		{
			return Compiler.FindClusteringKey(Plan, tableVar);
		}
		
		public Schema.Order OrderFromKey(Schema.Key key)
		{
			return Compiler.OrderFromKey(Plan, key);
		}
		
		public Schema.Order FindClusteringOrder(Schema.TableVar tableVar)
		{
			return Compiler.FindClusteringOrder(Plan, tableVar);
		}
		
		public Schema.Object ResolveCatalogObjectSpecifier(string specifier)
		{
			return Compiler.ResolveCatalogObjectSpecifier(Plan, specifier);
		}
		
		public Schema.Object ResolveCatalogObjectSpecifier(string specifier, bool mustResolve)
		{
			return Compiler.ResolveCatalogObjectSpecifier(Plan, specifier, mustResolve);
		}
		
		public Schema.Object ResolveCatalogIdentifier(string identifier)
		{
			return Compiler.ResolveCatalogIdentifier(Plan, identifier);
		}

		public Schema.Object ResolveCatalogIdentifier(string identifier, bool mustResolve)
		{
			return Compiler.ResolveCatalogIdentifier(Plan, identifier, mustResolve);
		}
	}
	
	public class Programs : List<Program> { }
}
