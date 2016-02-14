/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define LOADFROMLIBRARIES
#define PROCESSSTREAMSOWNED // Determines whether or not the process will deallocate all streams allocated by the process.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Alphora.Dataphor.DAE.Server
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using RealSQL = Alphora.Dataphor.DAE.Language.RealSQL;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Streams;

	// ServerProcess
	public class ServerProcess : ServerChildObject, IServerProcess
	{
		internal ServerProcess(ServerSession serverSession) : base()
		{
			InternalCreate(serverSession, new ProcessInfo(serverSession.SessionInfo));
		}
		
		internal ServerProcess(ServerSession serverSession, ProcessInfo processInfo) : base()
		{
			InternalCreate(serverSession, processInfo);
		}
		
		private void InternalCreate(ServerSession serverSession, ProcessInfo processInfo)
		{
			_serverSession = serverSession;
			_plans = new ServerPlans();
			_scripts = new ServerScripts();
			_parser = new Parser();
			_processID = GetHashCode();
			_processInfo = processInfo;
			_deviceSessions = new Schema.DeviceSessions(false);
			_streamManager = (IStreamManager)this;
			_valueManager = new ValueManager(this, this);
			#if PROCESSSTREAMSOWNED
			_ownedStreams = new Dictionary<StreamID, bool>();
			#endif

			_executingPrograms = new Programs();
			_processLocalStack = new List<DataParams>();
			PushProcessLocals();
			_processProgram = new Program(this);
			_processProgram.Start(null);
			
			if (_serverSession.DebuggedByID > 0)
				_serverSession.Server.GetDebugger(_serverSession.DebuggedByID).Attach(this);
		}
		
		private bool _disposed;
		
		protected void DeallocateProcessLocals(DataParams paramsValue)
		{
			foreach (DataParam param in paramsValue)
				DataValue.DisposeValue(_valueManager, param.Value);
		}
	
		protected void DeallocateProcessLocalStack()
		{
			foreach (DataParams paramsValue in _processLocalStack)
				DeallocateProcessLocals(paramsValue);
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					try
					{
						try
						{
							try
							{
								try
								{
									try
									{
										try
										{
											try
											{
												try
												{
													if (_debuggedBy != null)
														_debuggedBy.Detach(this);
														
													if (_applicationTransactionID != Guid.Empty)
														LeaveApplicationTransaction();
												}
												finally
												{
													if (_transactions != null)
													{
														// Rollback all active transactions
														while (InTransaction)
															RollbackTransaction();
															
														_transactions.UnprepareDeferredConstraintChecks();
													}
												}
											}
											finally
											{
												if (_plans != null)
													foreach (ServerPlan plan in _plans)
													{
														if (plan.ActiveCursor != null)
															plan.ActiveCursor.Dispose();
													}
											}
										}
										finally
										{
											if (_deviceSessions != null)
											{
												try
												{
													CloseDeviceSessions();
												}
												finally
												{
													_deviceSessions.Dispose();
													_deviceSessions = null;
												}
											}
										}
									}
									finally
									{
										if (_remoteSessions != null)
										{
											try
											{
												CloseRemoteSessions();
											}
											finally
											{
												_remoteSessions.Dispose();
												_remoteSessions = null;
											}
										}
									}
								}
								finally
								{
									if (_transactions != null)
									{
										_transactions.Dispose();
										_transactions = null;
									}
								}
							}
							finally
							{
								if (_processProgram != null)
								{
									_processProgram.Stop(null);
									_processProgram = null;
								}
							}
						}
						finally
						{
							if (_processLocalStack != null)
							{
								DeallocateProcessLocalStack();
								_processLocalStack = null;
							}
						}
					}
					finally
					{
						if (_scripts != null)
						{
							try
							{
								UnprepareScripts();
							}
							finally
							{
								_scripts.Dispose();
								_scripts = null;
							}
						}
					}
				}
				finally
				{
					if (_plans != null)
					{
						try
						{
							UnpreparePlans();
						}
						finally
						{
							_plans.Dispose();
							_plans = null;
						}
					}
					
					if (!_disposed)
						_disposed = true;
				}
			}
			finally
			{
				#if PROCESSSTREAMSOWNED
				ReleaseStreams();
				#endif
				
				_executingPrograms = null;
				_streamManager = null;
				_valueManager = null;
				_serverSession = null;
				_processID = -1;
				_parser = null;
				base.Dispose(disposing);
			}
		}
		
		// ServerSession
		private ServerSession _serverSession;
		public ServerSession ServerSession { get { return _serverSession; } }
		
		// This is an internal program used to ensure that there is always at
		// least on executing program on the process. At this point, this is used 
		// only by the value manager to evaluate sorts and perform scalar 
		// representation processing to avoid the overhead of having to create
		// a program each time a sort comparison or scalar transformation is
		// required. Even then, it is only necessary for out-of-process
		// processes, or in-process Dataphoria processes.
		private Program _processProgram;
		
		// ProcessID
		private int _processID = -1;
		public int ProcessID { get { return _processID; } }
		
		// ProcessInfo
		private ProcessInfo _processInfo;
		public ProcessInfo ProcessInfo { get { return _processInfo; } }
		
		/// <summary>Determines the default isolation level for transactions on this process.</summary>
		public IsolationLevel DefaultIsolationLevel
		{
			get { return _processInfo.DefaultIsolationLevel; }
			set { _processInfo.DefaultIsolationLevel = value; }
		}
		
		/// <summary>Returns the isolation level of the current transaction, if one is active. Otherwise, returns the default isolation level.</summary>
		public IsolationLevel CurrentIsolationLevel()
		{
			return _transactions.Count > 0 ? _transactions.CurrentTransaction().IsolationLevel : DefaultIsolationLevel;
		}
		
		// MaxStackDepth
		public int MaxStackDepth
		{
			get { return _executingPrograms.Count == 0 ? _serverSession.SessionInfo.DefaultMaxStackDepth : ExecutingProgram.Stack.MaxStackDepth; }
			set 
			{ 
				if (_executingPrograms.Count == 0)
					_serverSession.SessionInfo.DefaultMaxStackDepth = value;
				else
					ExecutingProgram.Stack.MaxStackDepth = value;
			}
		}
		
		// MaxCallDepth
		public int MaxCallDepth
		{
			get { return _executingPrograms.Count == 0 ? _serverSession.SessionInfo.DefaultMaxCallDepth : ExecutingProgram.Stack.MaxCallDepth; }
			set
			{
				if (_executingPrograms.Count == 0)
					_serverSession.SessionInfo.DefaultMaxCallDepth = value;
				else
					ExecutingProgram.Stack.MaxCallDepth = value;
			}
		}
		
		// NonLogged - Indicates whether logging is performed within the device.  
		// Is is an error to attempt non logged operations against a device that does not support non logged operations
		private int _nonLoggedCount;
		public bool NonLogged
		{
			get { return _nonLoggedCount > 0; }
			set { _nonLoggedCount += (value ? 1 : (NonLogged ? -1 : 0)); }
		}
		
		// IServerProcess.GetServerProcess()
		ServerProcess IServerProcess.GetServerProcess()
		{
			return this;
		}

		// IServerProcess.Session
		IServerSession IServerProcess.Session { get { return _serverSession; } }
		
		// Parsing
		private Parser _parser;		
		private RealSQL.Parser _sqlParser;
		private RealSQL.Compiler _sqlCompiler;

		private void EnsureSQLCompiler()
		{
			if (_sqlParser == null)
			{
				_sqlParser = new RealSQL.Parser();
				_sqlCompiler = new RealSQL.Compiler();
			}
		}
		
		public Statement ParseScript(string script, ParserMessages messages)
		{
			Statement statement;								  
			if (_processInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				statement = _sqlCompiler.Compile(_sqlParser.ParseScript(script));
			}
			else
				statement = _parser.ParseScript(script, messages);
			return statement;
		}
		
		public Statement ParseStatement(string statement, ParserMessages messages)
		{
			Statement localStatement;
			if (_processInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				localStatement = _sqlCompiler.Compile(_sqlParser.ParseStatement(statement));
			}
			else
				localStatement = _parser.ParseStatement(statement, messages);
			return localStatement;
		}
		
		public Expression ParseExpression(string expression)
		{
			Expression localExpression;
			if (_processInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				Statement localStatement = _sqlCompiler.Compile(_sqlParser.ParseStatement(expression));
				if (localStatement is SelectStatement)
					localExpression = ((SelectStatement)localStatement).CursorDefinition;
				else
					throw new CompilerException(CompilerException.Codes.TableExpressionExpected);
			}
			else
				localExpression = _parser.ParseCursorDefinition(expression);
			return localExpression;
		}

		// Plans
		private ServerPlans _plans;
		public ServerPlans Plans { get { return _plans; } }
        
		private void UnpreparePlans()
		{
			Exception exception = null;
			while (_plans.Count > 0)
			{
				try
				{
					ServerPlan plan = _plans.DisownAt(0) as ServerPlan;
					if (!ServerSession.ReleaseCachedPlan(this, plan))
						plan.Dispose();
					else
						plan.NotifyReleased();
				}
				catch (Exception E)
				{
					exception = E;
				}
			}
			if (exception != null)
				throw exception;
		}
        
		// Scripts
		private ServerScripts _scripts;
		public ServerScripts Scripts { get { return _scripts; } }

		private void UnprepareScripts()
		{
			Exception exception = null;
			while (_scripts.Count > 0)
			{
				try
				{
					_scripts.DisownAt(0).Dispose();
				}
				catch (Exception E)
				{
					exception = E;
				}
			}
			if (exception != null)
				throw exception;
		}
		
		#if USEROWMANAGER
		// RowManager
		public RowManager RowManager { get { return FServerSession.Server.RowManager; } }
		#endif
		
		#if USESCALARMANAGER
		// ScalarManager
		public ScalarManager ScalarManager { get { return FServerSession.Server.ScalarManager; } }
		#endif
		
		// IValueManager
		private IValueManager _valueManager;
		public IValueManager ValueManager { get { return _valueManager; } }
		
		// IStreamManager
		private IStreamManager _streamManager;
		public IStreamManager StreamManager { get { return _streamManager; } }
		
		#if PROCESSSTREAMSOWNED
		private Dictionary<StreamID, bool> _ownedStreams;
		
		private void ReleaseStreams()
		{
			if (_ownedStreams != null)
			{
				foreach (StreamID streamID in _ownedStreams.Keys)
					try
					{
						ServerSession.Server.StreamManager.Deallocate(streamID);
					}
					catch
					{
						// Just keep going, get rid of as many as possible.
					}

				_ownedStreams = null;
			}
		}
		#endif
		
		StreamID IStreamManager.Allocate()
		{
			StreamID streamID = ServerSession.Server.StreamManager.Allocate();
			#if PROCESSSTREAMSOWNED
			_ownedStreams.Add(streamID, false);
			#endif
			return streamID;
		}
		
		StreamID IStreamManager.Reference(StreamID streamID)
		{
			StreamID localStreamID = ServerSession.Server.StreamManager.Reference(streamID);
			#if PROCESSSTREAMSOWNED
			_ownedStreams.Add(localStreamID, true);
			#endif
			return localStreamID;
		}
		
		public StreamID Register(IStreamProvider streamProvider)
		{
			StreamID streamID = ServerSession.Server.StreamManager.Register(streamProvider);
			#if PROCESSSTREAMSOWNED
			_ownedStreams.Add(streamID, false);
			#endif
			return streamID;
		}
		
		void IStreamManager.Deallocate(StreamID streamID)
		{
			#if PROCESSSTREAMSOWNED
			_ownedStreams.Remove(streamID);
			#endif
			ServerSession.Server.StreamManager.Deallocate(streamID);
		}
		
		Stream IStreamManager.Open(StreamID streamID, LockMode lockMode)
		{
			return ServerSession.Server.StreamManager.Open(_processID, streamID, lockMode);
		}

		IRemoteStream IStreamManager.OpenRemote(StreamID streamID, LockMode lockMode)
		{
			return ServerSession.Server.StreamManager.OpenRemote(_processID, streamID, lockMode);
		}

		#if UNMANAGEDSTREAM
		void IStreamManager.Close(StreamID AStreamID)
		{
			ServerSession.Server.StreamManager.Close(FProcessID, AStreamID);
		}
		#endif
		
		// ClassLoader
		public object CreateObject(ClassDefinition classDefinition, object[] arguments)
		{
			return _serverSession.Server.Catalog.ClassLoader.CreateObject(CatalogDeviceSession, classDefinition, arguments);
		}
		
		public Type CreateType(ClassDefinition classDefinition)
		{
			return _serverSession.Server.Catalog.ClassLoader.CreateType(CatalogDeviceSession, classDefinition);
		}
		
		// DeviceSessions		
		private Schema.DeviceSessions _deviceSessions;
		internal Schema.DeviceSessions DeviceSessions { get { return _deviceSessions; } }

		public Schema.DeviceSession DeviceConnect(Schema.Device device)
		{
			int index = DeviceSessions.IndexOf(device);
			if (index < 0)
			{
				EnsureDeviceStarted(device);
				Schema.DeviceSession session = device.Connect(this, ServerSession.SessionInfo);
				try
				{
					#if REFERENCECOUNTDEVICESESSIONS
					session.FReferenceCount = 1;
					#endif
					while (session.Transactions.Count < _transactions.Count)
						session.BeginTransaction(_transactions[_transactions.Count - 1].IsolationLevel);
					DeviceSessions.Add(session);
					return session;
				}
				catch
				{
					device.Disconnect(session);
					throw;
				}
			}
			else
			{
				#if REFERENCECOUNTDEVICESESSIONS
				DeviceSessions[index].FReferenceCount++;
				#endif
				return DeviceSessions[index];
			}
		}
		
		internal void CloseDeviceSessions()
		{
			while (DeviceSessions.Count > 0)
				DeviceSessions[0].Device.Disconnect(DeviceSessions[0]);
		}
		
		public void DeviceDisconnect(Schema.Device device)
		{
			int index = DeviceSessions.IndexOf(device);
			if (index >= 0)
			{
				#if REFERENCECOUNTDEVICESESSIONS
				DeviceSessions[index].FReferenceCount--;
				if (DeviceSessions[index].FReferenceCount == 0)
				#endif
				device.Disconnect(DeviceSessions[index]);
			}
		}
		
		public void EnsureDeviceStarted(Schema.Device device)
		{
			ServerSession.Server.StartDevice(this, device);
		}
		
		// CatalogDeviceSession
		public CatalogDeviceSession CatalogDeviceSession
		{
			get
			{
				CatalogDeviceSession session = DeviceConnect(ServerSession.Server.CatalogDevice) as CatalogDeviceSession;
				Error.AssertFail(session != null, "Could not connect to catalog device");
				return session;
			}
		}
		
		// RemoteSessions
		private RemoteSessions _remoteSessions;
		internal RemoteSessions RemoteSessions
		{
			get 
			{ 
				if (_remoteSessions == null)
					_remoteSessions = new RemoteSessions();
				return _remoteSessions; 
			}
		}
		
		internal string RemoteSessionClassName = "Alphora.Dataphor.DAE.Server.RemoteSessionImplementation,AlphoraDataphorServer";
		
		internal RemoteSession RemoteConnect(Schema.ServerLink serverLink)
		{
			int index = RemoteSessions.IndexOf(serverLink);
			if (index < 0)
			{
				RemoteSession session = (RemoteSession)Activator.CreateInstance(Type.GetType(RemoteSessionClassName, true), this, serverLink);
				try
				{
					while (session.TransactionCount < _transactions.Count)
						session.BeginTransaction(_transactions[_transactions.Count - 1].IsolationLevel);
					RemoteSessions.Add(session);
					return session;
				}
				catch
				{
					session.Dispose();
					throw;
				}
			}
			else
				return RemoteSessions[index];
		}
		
		internal void RemoteDisconnect(Schema.ServerLink serverLink)
		{
			int index = RemoteSessions.IndexOf(serverLink);
			if (index >= 0)
				RemoteSessions[index].Dispose();
			else
				throw new ServerException(ServerException.Codes.NoRemoteSessionForServerLink, serverLink.Name);
		}
		
		internal void CloseRemoteSessions()
		{
			while (RemoteSessions.Count > 0)
				RemoteSessions[0].Dispose();
		}
		
		// Plans
		/// <summary>Indicates whether or not warnings encountered during compilation of plans on this process will be reported.</summary>
		public bool SuppressWarnings
		{
			get { return _processInfo.SuppressWarnings; }
			set { _processInfo.SuppressWarnings = value; }
		}
		
		// ProcessLocals
		private List<DataParams> _processLocalStack;
		public DataParams ProcessLocals { get { return _processLocalStack[_processLocalStack.Count - 1]; } }
		
		public void PushProcessLocals()
		{
			_processLocalStack.Add(new DataParams());
		}
		
		public void PopProcessLocals()
		{
			DeallocateProcessLocals(ProcessLocals);
			_processLocalStack.RemoveAt(_processLocalStack.Count - 1);
		}
		
		public void AddProcessLocal(DataParam LParam)
		{
			DataParams processLocals = ProcessLocals;
			int paramIndex = processLocals.IndexOf(LParam.Name);
			if (paramIndex >= 0)
			{
				DataValue.DisposeValue(_valueManager, processLocals[paramIndex].Value);
				processLocals[paramIndex].Value = LParam.Value;
				LParam.Value = null;
			}
			else
				processLocals.Add(LParam);
		}
		
		private void Compile(Plan plan, Program program, Statement statement, DataParams paramsValue, bool isExpression, SourceContext sourceContext)
		{
			#if TIMING
			DateTime startTime = DateTime.Now;
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerSession.Compile", DateTime.Now.ToString("hh:mm:ss.ffff")));
			#endif
			Schema.DerivedTableVar tableVar = null;
			tableVar = new Schema.DerivedTableVar(Schema.Object.GetNextObjectID(), Schema.Object.NameFromGuid(program.ID));
			tableVar.SessionObjectName = tableVar.Name;
			tableVar.SessionID = plan.SessionID;
			plan.PushCreationObject(tableVar);
			try
			{
				DataParams localParamsValue = new DataParams();
				if (program.ShouldPushLocals)
					foreach (DataParam param in ProcessLocals)
						localParamsValue.Add(param);

				if (paramsValue != null)
					foreach (DataParam param in paramsValue)
						localParamsValue.Add(param);

				program.Code = Compiler.Compile(plan, statement, localParamsValue, isExpression, sourceContext);
				program.SetSourceContext(sourceContext);

				// Include dependencies for any process context that may be being referenced by the plan
				// This is necessary to support the variable declaration statements that will be added to support serialization of the plan
				if (program.ShouldPushLocals && plan.NewSymbols != null)
					foreach (Symbol symbol in plan.NewSymbols)
					{
						program.ProcessLocals.Add(new DataParam(symbol.Name, symbol.DataType, Modifier.Var));
						plan.PlanCatalog.IncludeDependencies(CatalogDeviceSession, plan.Catalog, symbol.DataType, EmitMode.ForRemote);
					}

				if (!plan.Messages.HasErrors && isExpression)
				{
					if (program.Code.DataType == null)
						throw new CompilerException(CompilerException.Codes.ExpressionExpected);
					program.DataType = CopyDataType(plan, program.Code, tableVar);
				}
			}
			finally
			{
				plan.PopCreationObject();
			}
			#if TIMING
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerSession.Compile -- Compile Time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - startTime).ToString()));
			#endif
		}
        
		internal ServerPlan CompileStatement(Statement statement, ParserMessages messages, DataParams paramsValue, SourceContext sourceContext)
		{
			ServerPlan plan = new ServerStatementPlan(this);
			try
			{
				if (messages != null)
					plan.Plan.Messages.AddRange(messages);
				Compile(plan.Plan, plan.Program, statement, paramsValue, false, sourceContext);
				_plans.Add(plan);
				return plan;
			}
			catch
			{
				plan.Dispose();
				throw;
			}
		}

		private void InternalUnprepare(ServerPlan plan)
		{
			BeginCall();
			try
			{
				_plans.Disown(plan);
				if (!ServerSession.ReleaseCachedPlan(this, plan))
					plan.Dispose();
				else
					plan.NotifyReleased();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		private int GetContextHashCode(DataParams paramsValue)
		{
			StringBuilder contextName = new StringBuilder();
			for (int index = 0; index < ProcessLocals.Count; index++)
				contextName.Append(ProcessLocals[index].DataType.Name);
			if (paramsValue != null)
				for (int index = 0; index < paramsValue.Count; index++)
					contextName.Append(paramsValue[index].DataType.Name);
			return contextName.ToString().GetHashCode();
		}
		
		public IServerStatementPlan PrepareStatement(string statement, DataParams paramsValue)
		{
			return PrepareStatement(statement, paramsValue, null);
		}
		
		// PrepareStatement        
		public IServerStatementPlan PrepareStatement(string statement, DataParams paramsValue, DebugLocator locator)
		{
			BeginCall();
			try
			{
				int contextHashCode = GetContextHashCode(paramsValue);
				IServerStatementPlan plan = ServerSession.GetCachedPlan(this, statement, contextHashCode) as IServerStatementPlan;
				if (plan != null)
				{
					_plans.Add(plan);
					((ServerPlan)plan).Program.SetSourceContext(new SourceContext(statement, locator));
				}
				else
				{
					ParserMessages messages = new ParserMessages();
					Statement localStatement = ParseStatement(statement, messages);
					plan = (IServerStatementPlan)CompileStatement(localStatement, messages, paramsValue, new SourceContext(statement, locator));
					if (!plan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, statement, contextHashCode, (ServerPlan)plan);
				}
				return plan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// UnprepareStatement
		public void UnprepareStatement(IServerStatementPlan plan)
		{
			InternalUnprepare((ServerPlan)plan);
		}
        
		// Execute
		public void Execute(string statement, DataParams paramsValue)
		{
			IServerStatementPlan statementPlan = PrepareStatement(statement, paramsValue);
			try
			{
				statementPlan.Execute(paramsValue);
			}
			finally
			{
				UnprepareStatement(statementPlan);
			}
		}
		
		private Schema.IDataType CopyDataType(Plan plan, PlanNode code, Schema.DerivedTableVar tableVar)
		{
			#if TIMING
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerProcess.CopyDataType", DateTime.Now.ToString("hh:mm:ss.ffff")));
			#endif
			if (code.DataType is Schema.CursorType)
			{
				TableNode sourceNode = (TableNode)code.Nodes[0];	
				Schema.TableVar sourceTableVar = sourceNode.TableVar;

				AdornExpression adornExpression = new AdornExpression();
				adornExpression.MetaData = new MetaData();
				adornExpression.MetaData.Tags.Add(new Tag("DAE.IsChangeRemotable", (sourceTableVar.IsChangeRemotable || !sourceTableVar.ShouldChange).ToString()));
				adornExpression.MetaData.Tags.Add(new Tag("DAE.IsDefaultRemotable", (sourceTableVar.IsDefaultRemotable || !sourceTableVar.ShouldDefault).ToString()));
				adornExpression.MetaData.Tags.Add(new Tag("DAE.IsValidateRemotable", (sourceTableVar.IsValidateRemotable || !sourceTableVar.ShouldValidate).ToString()));

				if (sourceNode.Order != null)
				{
					if (!sourceTableVar.Orders.Contains(sourceNode.Order))
						sourceTableVar.Orders.Add(sourceNode.Order);
					adornExpression.MetaData.Tags.Add(new Tag("DAE.DefaultOrder", sourceNode.Order.Name));
				}
				
				foreach (Schema.TableVarColumn column in sourceNode.TableVar.Columns)
				{
					if (!((column.IsDefaultRemotable || !column.ShouldDefault) && (column.IsValidateRemotable || !column.ShouldValidate) && (column.IsChangeRemotable || !column.ShouldChange)))
					{
						AdornColumnExpression columnExpression = new AdornColumnExpression();
						columnExpression.ColumnName = Schema.Object.EnsureRooted(column.Name);
						columnExpression.MetaData = new MetaData();
						columnExpression.MetaData.Tags.AddOrUpdate("DAE.IsChangeRemotable", (column.IsChangeRemotable || !column.ShouldChange).ToString());
						columnExpression.MetaData.Tags.AddOrUpdate("DAE.IsDefaultRemotable", (column.IsDefaultRemotable || !column.ShouldDefault).ToString());
						columnExpression.MetaData.Tags.AddOrUpdate("DAE.IsValidateRemotable", (column.IsValidateRemotable || !column.ShouldValidate).ToString());
						adornExpression.Expressions.Add(columnExpression);
					}
				}
				
				PlanNode viewNode = sourceNode;
				while (viewNode is BaseOrderNode)
					viewNode = viewNode.Nodes[0];
					
				viewNode = Compiler.EmitAdornNode(plan, viewNode, adornExpression);

				tableVar.CopyTableVar((TableNode)viewNode);
				tableVar.CopyReferences((TableNode)viewNode);
				tableVar.InheritMetaData(((TableNode)viewNode).TableVar.MetaData);
				#if USEORIGINALEXPRESSION
				ATableVar.OriginalExpression = (Expression)viewNode.EmitStatement(EmitMode.ForRemote);
				#else
				tableVar.InvocationExpression = (Expression)viewNode.EmitStatement(EmitMode.ForRemote);
				#endif
				
				// Gather AT dependencies
				// Each AT object will be emitted remotely as the underlying object, so report the dependency so that the catalog emission happens in the correct order.
				Schema.Objects aTObjects = new Schema.Objects();
				if (tableVar.HasDependencies())
					for (int index = 0; index < tableVar.Dependencies.Count; index++)
					{
						Schema.Object objectValue = tableVar.Dependencies.ResolveObject(CatalogDeviceSession, index);
						if ((objectValue != null) && objectValue.IsATObject && ((objectValue is Schema.TableVar) || (objectValue is Schema.Operator)))
							aTObjects.Add(objectValue);
					}

				foreach (Schema.Object aTObject in aTObjects)
				{
					Schema.TableVar aTTableVar = aTObject as Schema.TableVar;
					if (aTTableVar != null)
					{
						Schema.TableVar localTableVar = (Schema.TableVar)plan.Catalog[plan.Catalog.IndexOfName(aTTableVar.SourceTableName)];
						tableVar.Dependencies.Ensure(localTableVar);
						continue;
					}

					Schema.Operator aTOperator = aTObject as Schema.Operator;
					if (aTOperator != null)
					{
						tableVar.Dependencies.Ensure(ServerSession.Server.ATDevice.ResolveSourceOperator(plan, aTOperator));
					}
				}
				
				plan.PlanCatalog.IncludeDependencies(CatalogDeviceSession, plan.Catalog, tableVar, EmitMode.ForRemote);
				plan.PlanCatalog.Remove(tableVar);
				plan.PlanCatalog.Add(tableVar);
				return tableVar.DataType;
			}
			else
			{
				plan.PlanCatalog.IncludeDependencies(CatalogDeviceSession, plan.Catalog, tableVar, EmitMode.ForRemote);
				plan.PlanCatalog.SafeRemove(tableVar);
				plan.PlanCatalog.IncludeDependencies(CatalogDeviceSession, plan.Catalog, code.DataType, EmitMode.ForRemote);
				return code.DataType;
			}
		}

		internal ServerPlan CompileExpression(Statement expression, ParserMessages messages, DataParams paramsValue, SourceContext sourceContext)
		{
			ServerPlan plan = new ServerExpressionPlan(this);
			try
			{
				if (messages != null)
					plan.Plan.Messages.AddRange(messages);
				Compile(plan.Plan, plan.Program, expression, paramsValue, true, sourceContext);
				_plans.Add(plan);
				return plan;
			}
			catch
			{
				plan.Dispose();
				throw;
			}
		}
		
		public IServerExpressionPlan PrepareExpression(string expression, DataParams paramsValue)
		{
			return PrepareExpression(expression, paramsValue, null);
		}
		
		// PrepareExpression
		public IServerExpressionPlan PrepareExpression(string expression, DataParams paramsValue, DebugLocator locator)
		{
			BeginCall();
			try
			{
				int contextHashCode = GetContextHashCode(paramsValue);
				IServerExpressionPlan plan = ServerSession.GetCachedPlan(this, expression, contextHashCode) as IServerExpressionPlan;
				if (plan != null)
				{
					_plans.Add(plan);
					((ServerPlan)plan).Program.SetSourceContext(new SourceContext(expression, locator));
				}
				else
				{
					ParserMessages messages = new ParserMessages();
					plan = (IServerExpressionPlan)CompileExpression(ParseExpression(expression), messages, paramsValue, new SourceContext(expression, locator));
					if (!plan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, expression, contextHashCode, (ServerPlan)plan);
				}
				return plan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// UnprepareExpression
		public void UnprepareExpression(IServerExpressionPlan plan)
		{
			InternalUnprepare((ServerPlan)plan);
		}
		
		// Evaluate
		public IDataValue Evaluate(string expression, DataParams paramsValue)
		{
			IServerExpressionPlan plan = PrepareExpression(expression, paramsValue);
			try
			{
				return plan.Evaluate(paramsValue);
			}
			finally
			{
				UnprepareExpression(plan);
			}
		}
		
		// OpenCursor
		public IServerCursor OpenCursor(string expression, DataParams paramsValue)
		{
			IServerExpressionPlan plan = PrepareExpression(expression, paramsValue);
			try
			{
				return plan.Open(paramsValue);
			}
			catch
			{
				UnprepareExpression(plan);
				throw;
			}
		}
		
		// CloseCursor
		public void CloseCursor(IServerCursor cursor)
		{
			IServerExpressionPlan plan = cursor.Plan;
			try
			{
				plan.Close(cursor);
			}
			finally
			{
				UnprepareExpression(plan);
			}
		}
		
		public IServerScript PrepareScript(string script)
		{
			return PrepareScript(script, null);
		}
        
		// PrepareScript
		public IServerScript PrepareScript(string script, DebugLocator locator)
		{
			BeginCall();
			try
			{
				ServerScript localScript = new ServerScript(this, script, locator);
				try
				{
					_scripts.Add(localScript);
					return localScript;
				}
				catch
				{
					localScript.Dispose();
					throw;
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// UnprepareScript
		public void UnprepareScript(IServerScript script)
		{
			BeginCall();
			try
			{
				((ServerScript)script).Dispose();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
		
		// ExecuteScript
		public void ExecuteScript(string script)
		{
			IServerScript localScript = PrepareScript(script);
			try
			{
				localScript.Execute(null);
			}
			finally
			{
				UnprepareScript(localScript);
			}
		}

		// Transaction		
		private IServerDTCTransaction _dTCTransaction;
		private ServerTransactions _transactions = new ServerTransactions();
		public ServerTransactions Transactions { get { return _transactions; } }

		internal ServerTransaction CurrentTransaction { get { return _transactions.CurrentTransaction(); } }
		internal ServerTransaction RootTransaction { get { return _transactions.RootTransaction(); } }
		
		internal void AddInsertTableVarCheck(Schema.TableVar tableVar, IRow row)
		{
			_transactions.AddInsertTableVarCheck(tableVar, row);
		}
		
		internal void AddUpdateTableVarCheck(Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			_transactions.AddUpdateTableVarCheck(tableVar, oldRow, newRow, valueFlags);
		}
		
		internal void AddDeleteTableVarCheck(Schema.TableVar tableVar, IRow row)
		{
			_transactions.AddDeleteTableVarCheck(tableVar, row);
		}
		
		internal void RemoveDeferredConstraintChecks(Schema.TableVar tableVar)
		{
			if (_transactions != null)
				_transactions.RemoveDeferredConstraintChecks(tableVar);
		}
		
		internal void RemoveDeferredHandlers(Schema.EventHandler handler)
		{
			if (_transactions != null)
				_transactions.RemoveDeferredHandlers(handler);
		}
		
		internal void RemoveCatalogConstraintCheck(Schema.CatalogConstraint constraint)
		{
			if (_transactions != null)
				_transactions.RemoveCatalogConstraintCheck(constraint);
		}
		
		// TransactionCount
		public int TransactionCount { get { return _transactions.Count; } }

		// InTransaction
		public bool InTransaction { get { return _transactions.Count > 0; } }

		private void CheckInTransaction()
		{
			if (!InTransaction)
				throw new ServerException(ServerException.Codes.NoTransactionActive);
		}
        
		// UseDTC
		public bool UseDTC
		{
			get { return _processInfo.UseDTC; }
			set
			{
				lock (this)
				{
					if (InTransaction)
						throw new ServerException(ServerException.Codes.TransactionActive);

					if (value && (Environment.OSVersion.Version.Major < 5))
						throw new ServerException(ServerException.Codes.DTCNotSupported);
					_processInfo.UseDTC = value;
				}
			}
		}
		
		// UseImplicitTransactions
		public bool UseImplicitTransactions
		{
			get { return _processInfo.UseImplicitTransactions; }
			set { _processInfo.UseImplicitTransactions = value; }
		}

		// BeginTransaction
		public void BeginTransaction(IsolationLevel isolationLevel)
		{
			BeginCall();
			try
			{
				InternalBeginTransaction(isolationLevel);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// PrepareTransaction
		public void PrepareTransaction()
		{
			BeginCall();
			try
			{
				InternalPrepareTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// CommitTransaction
		public void CommitTransaction()
		{
			BeginCall();
			try
			{
				InternalCommitTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}

		// RollbackTransaction        
		public void RollbackTransaction()
		{
			BeginCall();
			try
			{
				InternalRollbackTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
    
		protected internal void InternalBeginTransaction(IsolationLevel isolationLevel)
		{
			_transactions.BeginTransaction(this, isolationLevel);

			if (UseDTC && (_dTCTransaction == null))
			{
				CloseDeviceSessions();
				_dTCTransaction = (IServerDTCTransaction)Activator.CreateInstance(Type.GetType("Alphora.Dataphor.DAE.Server.ServerDTCTransaction,Alphora.Dataphor.DAE.Server"));
				_dTCTransaction.IsolationLevel = isolationLevel;
			}
			
			// Begin a transaction on all remote sessions
			foreach (RemoteSession remoteSession in RemoteSessions)
				remoteSession.BeginTransaction(isolationLevel);

			// Begin a transaction on all device sessions
			foreach (Schema.DeviceSession deviceSession in DeviceSessions)
				deviceSession.BeginTransaction(isolationLevel);
		}
		
		protected internal void InternalPrepareTransaction()
		{
			CheckInTransaction();

			ServerTransaction transaction = _transactions.CurrentTransaction();
			if (!transaction.Prepared)
			{
				foreach (RemoteSession remoteSession in RemoteSessions)
					remoteSession.PrepareTransaction();
					
				foreach (Schema.DeviceSession deviceSession in DeviceSessions)
					deviceSession.PrepareTransaction();
					
				// Create a new program to perform the deferred validation
				Program program = new Program(this);
				program.Start(null);
				try
				{
					// Invoke event handlers for work done during this transaction
					transaction.InvokeDeferredHandlers(program);

					// Validate Constraints which have been affected by work done during this transaction
					_transactions.ValidateDeferredConstraints(program);

					foreach (Schema.CatalogConstraint constraint in transaction.CatalogConstraints)
						constraint.Validate(program);
				}
				finally
				{
					program.Stop(null);
				}
				
				transaction.Prepared = true;
			}
		}
		
		protected internal void InternalCommitTransaction()
		{
			CheckInTransaction();

			// Prepare Commit Phase
			InternalPrepareTransaction();

			try
			{
				// Commit the DTC if this is the Top Level Transaction
				if (UseDTC && (_transactions.Count == 1))
				{
					try
					{
						_dTCTransaction.Commit();
					}
					finally
					{
						_dTCTransaction.Dispose();
						_dTCTransaction = null;
					}
				}
			}
			catch
			{
				RollbackTransaction();
				throw;
			}
			
			// Commit
			foreach (RemoteSession remoteSession in RemoteSessions)
				remoteSession.CommitTransaction();

			Schema.DeviceSession catalogDeviceSession = null;
			foreach (Schema.DeviceSession deviceSession in DeviceSessions)
			{
				if (deviceSession is CatalogDeviceSession)
					catalogDeviceSession = deviceSession;
				else
					deviceSession.CommitTransaction();
			}
			
			// Commit the catalog device session last. The catalog device session tracks devices
			// dropped during the session, and will defer the stop of those devices until after the
			// transaction has committed, allowing any active transactions in that device to commit.
			if (catalogDeviceSession != null)
				catalogDeviceSession.CommitTransaction();
				
			_transactions.EndTransaction(true);
		}

		protected internal void InternalRollbackTransaction()
		{
			CheckInTransaction();
			try
			{
				_transactions.CurrentTransaction().InRollback = true;
				try
				{
					if (UseDTC && (_transactions.Count == 1))
					{
						try
						{
							_dTCTransaction.Rollback();
						}
						finally
						{
							_dTCTransaction.Dispose();
							_dTCTransaction = null;
						}
					}

					Exception exception = null;
					
					foreach (RemoteSession remoteSession in RemoteSessions)
					{
						try
						{
							if (remoteSession.InTransaction)
								remoteSession.RollbackTransaction();
						}
						catch (Exception E)
						{
							exception = E;
							ServerSession.Server.LogError(E);
						}
					}
					
					Schema.DeviceSession catalogDeviceSession = null;
					foreach (Schema.DeviceSession deviceSession in DeviceSessions)
					{
						try
						{
							if (deviceSession is CatalogDeviceSession)
								catalogDeviceSession = deviceSession;
							else
								if (deviceSession.InTransaction)
									deviceSession.RollbackTransaction();
						}
						catch (Exception E)
						{
							exception = E;
							this.ServerSession.Server.LogError(E);
						}
					}
					
					try
					{
						// Catalog device session transaction must be rolled back last
						// Well, it's not really correct to roll it back last, or first, or in any particular order,
						// but I don't have another option unless I start managing all transaction logging for
						// all devices, so rolling it back last seems like the best option (even at that I have
						// to ignore errors that occur during the rollback - see CatalogDeviceSession.InternalRollbackTransaction).
						if ((catalogDeviceSession != null) && catalogDeviceSession.InTransaction)
							catalogDeviceSession.RollbackTransaction();
					}
					catch (Exception E)
					{
						exception = E;
						this.ServerSession.Server.LogError(E);
					}
					
					if (exception != null)
						throw exception;
				}
				finally
				{
					_transactions.CurrentTransaction().InRollback = false;
				}
			}
			finally
			{
				_transactions.EndTransaction(false);
			}
		}
		
		// Application Transactions
		private Guid _applicationTransactionID = Guid.Empty;
		public Guid ApplicationTransactionID { get { return _applicationTransactionID; } }
		
		private bool _isInsert;
		/// <summary>Indicates whether this process is an insert participant in an application transaction.</summary>
		public bool IsInsert 
		{ 
			get { return _isInsert; } 
			set { _isInsert = value; }
		}
		
		private bool _isOpeningInsertCursor;
		/// <summary>Indicates whether this process is currently opening an insert cursor.</summary>
		/// <remarks>
		/// This flag is used to implement an optimization to the insert process to prevent the actual
		/// opening of cursors unnecessarily. This flag is used by the devices involved in an expression
		/// to determine whether or not to issue the cursor open, or simply return an empty cursor.
		/// The optimization is valid because the insert cursor expression always has a contradictory
		/// restriction appended. In theory, the optimizers for the target systems should recognize the
		/// contradiction and the insert cursor open should not take any time, but in reality, it
		/// does take time with some complex cursors, and there is also the unnecessary network or RPC hit
		/// for extra-process devices.
		/// </remarks>
		public bool IsOpeningInsertCursor
		{
			get { return _isOpeningInsertCursor; }
			set { _isOpeningInsertCursor = value; }
		}
		
		// AddingTableVar
		private int _addingTableVar;
		/// <summary>Indicates whether the current process is adding an A/T table map.</summary>
		/// <remarks>This is used to prevent the resolution process from attempting to add a table map for the table variable being created before the table map is done.</remarks>
		public bool InAddingTableVar { get { return _addingTableVar > 0; } }
		
		public void PushAddingTableVar()
		{
			_addingTableVar++;
		}
		
		public void PopAddingTableVar()
		{
			_addingTableVar--;
		}
		
		public ApplicationTransaction GetApplicationTransaction()
		{
			return ApplicationTransactionUtility.GetTransaction(this, _applicationTransactionID);
		}
		
		// BeginApplicationTransaction
		public Guid BeginApplicationTransaction(bool shouldJoin, bool isInsert)
		{
			Exception exception = null;
			int nestingLevel = BeginTransactionalCall();
			try
			{
				Guid aTID = ApplicationTransactionUtility.BeginApplicationTransaction(this);
				
				if (shouldJoin)
					JoinApplicationTransaction(aTID, isInsert);

				return aTID;
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		// PrepareApplicationTransaction
		public void PrepareApplicationTransaction(Guid iD)
		{
			Exception exception = null;
			int nestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.PrepareApplicationTransaction(this, iD);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		// CommitApplicationTransaction
		public void CommitApplicationTransaction(Guid iD)
		{
			Exception exception = null;
			int nestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.CommitApplicationTransaction(this, iD);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		// RollbackApplicationTransaction
		public void RollbackApplicationTransaction(Guid iD)
		{
			Exception exception = null;
			int nestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.RollbackApplicationTransaction(this, iD);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(nestingLevel, exception);
			}
		}

		public void JoinApplicationTransaction(Guid iD, bool isInsert)
		{
			Exception exception = null;
			int nestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.JoinApplicationTransaction(this, iD);
				_applicationTransactionID = iD;
				_isInsert = isInsert;
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void LeaveApplicationTransaction()
		{
			Exception exception = null;
			int nestingLevel = BeginTransactionalCall();
			try
			{
				try
				{
					ApplicationTransactionUtility.LeaveApplicationTransaction(this);
				}
				finally
				{
					_applicationTransactionID = Guid.Empty;
					_isInsert = false;
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(nestingLevel, exception);
			}
		}

		// HandlerDepth
		protected int _handlerDepth;
		/// <summary>Indicates whether the process has entered an event handler.</summary>
		public bool InHandler { get { return _handlerDepth > 0; } }
		
		public void PushHandler()
		{
			lock (this)
			{
				_handlerDepth++;
			}
		}
		
		public void PopHandler()
		{
			lock (this)
			{
				_handlerDepth--;
			}
		}
        
		protected int _timeStampCount = 0;
		/// <summary>Indicates whether time stamps should be affected by alter and drop table variable and operator statements.</summary>
		public bool ShouldAffectTimeStamp { get { return _timeStampCount == 0; } }
		
		public void EnterTimeStampSafeContext()
		{
			_timeStampCount++;
		}
		
		public void ExitTimeStampSafeContext()
		{
			_timeStampCount--;
		}
		
		private Programs _executingPrograms;
		internal Programs ExecutingPrograms { get { return _executingPrograms; } }
		
		internal Program ExecutingProgram
		{
			get
			{
				if (_executingPrograms.Count == 0)
					throw new ServerException(ServerException.Codes.NoExecutingProgram);
				return _executingPrograms[_executingPrograms.Count - 1];
			}
		}
		
		internal void PushExecutingProgram(Program program)
		{
			_executingPrograms.Add(program);
		}
		
		internal void PopExecutingProgram(Program program)
		{
			if (ExecutingProgram.ID != program.ID)
				throw new ServerException(ServerException.Codes.ProgramNotExecuting, program.ID.ToString());
			_executingPrograms.RemoveAt(_executingPrograms.Count - 1);
		}
		
		// Execution
		private System.Threading.Thread _executingThread;
		public System.Threading.Thread ExecutingThread { get { return _executingThread; } }
		
		private int _executingThreadCount = 0;
		
		// SyncHandle is used to synchronize CLI calls to the process, ensuring that only one call originating in the CLI is allowed to run through the process at a time
		private object _syncHandle = new System.Object();
		public object SyncHandle { get { return _syncHandle; } }

		// ExecutionSyncHandle is used to synchronize server-level execution management services involving the executing thread of the process
		private object _executionSyncHandle = new System.Object();
		public object ExecutionSyncHandle { get { return _executionSyncHandle; } }
		
		// StopError is used to indicate that a system level stop error has occurred on the process, and all transactions should be rolled back
		private bool _stopError = false;
		public bool StopError 
		{ 
			get { return _stopError; } 
			set { _stopError = value; } 
		}
		
		private bool _isAborted = false;
		public bool IsAborted
		{
			get { return _isAborted; }
			set { lock (_executionSyncHandle) { _isAborted = value; } }
		}
		
		public void CheckAborted()
		{
			if (_isAborted)
				throw new ServerException(ServerException.Codes.ProcessAborted);
		}
		
		public bool IsRunning { get { return _executingThreadCount > 0; } }
		
		internal void BeginCall()
		{
			Monitor.Enter(_syncHandle);
			try
			{
				if ((_executingThreadCount == 0) && !ServerSession.User.IsAdminUser())
					ServerSession.Server.BeginProcessCall(this);
				Monitor.Enter(_executionSyncHandle);
				try
				{
					_executingThreadCount++;
					if (_executingThreadCount == 1)
					{
						_executingThread = System.Threading.Thread.CurrentThread;
						_isAborted = false;
						//FExecutingThread.ApartmentState = ApartmentState.STA; // This had no effect, the ADO thread lock is still occurring
						ThreadUtility.SetThreadName(_executingThread, String.Format("ServerProcess {0}", _processID.ToString()));
					}
				}
				finally
				{
					Monitor.Exit(_executionSyncHandle);
				}
			}
			catch
			{
				try
				{
					if (_executingThreadCount > 0)
					{
						Monitor.Enter(_executionSyncHandle);
						try
						{
							_executingThreadCount--;
							if (_executingThreadCount == 0)
							{
								ThreadUtility.SetThreadName(_executingThread, null);
								_executingThread = null;
							}
						}
						finally
						{
							Monitor.Exit(_executionSyncHandle);
						}
					}
				}
				finally
				{
					Monitor.Exit(_syncHandle);
				}
                throw;
            }
		}
		
		internal void EndCall()
		{
			try
			{
				if ((_executingThreadCount == 1) && !ServerSession.User.IsAdminUser())
					ServerSession.Server.EndProcessCall(this);
			}
			finally
			{
				try
				{
					Monitor.Enter(_executionSyncHandle);
					try
					{
						_executingThreadCount--;
						if (_executingThreadCount == 0)
						{
							ThreadUtility.SetThreadName(_executingThread, null);
							_executingThread = null;
						}
					}
					finally
					{
						Monitor.Exit(_executionSyncHandle);
					}
				}
				finally
				{
					Monitor.Exit(_syncHandle);
				}
			}
		}
		
		internal int BeginTransactionalCall()
		{
			BeginCall();
			try
			{
				_stopError = false;
				int nestingLevel = _transactions.Count;
				if ((_transactions.Count == 0) && UseImplicitTransactions)
					InternalBeginTransaction(DefaultIsolationLevel);
				if (_transactions.Count > 0)
					_transactions.CurrentTransaction().Prepared = false;
				return nestingLevel;
			}
			catch
			{
				EndCall();
				throw;
			}
		}
		
		internal void EndTransactionalCall(int nestingLevel, Exception exception)
		{
			try
			{
				if (StopError)
					while (_transactions.Count > 0)
						try
						{
							InternalRollbackTransaction();
						}
						catch (Exception E)
						{
							ServerSession.Server.LogError(E);
						}
				
				if (UseImplicitTransactions)
				{
					Exception commitException = null;
					while (_transactions.Count > nestingLevel)
						if ((exception != null) || (commitException != null))
							InternalRollbackTransaction();
						else
						{
							try
							{
								InternalPrepareTransaction();
								InternalCommitTransaction();
							}
							catch (Exception localException)
							{
								commitException = localException;
								InternalRollbackTransaction();
							}
						}

					if (commitException != null)
						throw commitException;
				}
			}
			catch (Exception E)
			{
				if (exception != null)
					// TODO: Should this actually attach the rollback exception as context information to the server exception object?
					throw new ServerException(ServerException.Codes.RollbackError, exception, E.ToString());
				else
					throw;
			}
			finally
			{
				EndCall();
			}
		}
		
		private Exception WrapException(Exception exception)
		{
			return _serverSession.WrapException(exception);
		}
		
		// Debugging
		
		private Debugger _debuggedBy;
		public Debugger DebuggedBy { get { return _debuggedBy; } }
		
		internal void SetDebuggedBy(Debugger debuggedBy)
		{
			if ((_debuggedBy != null) && (debuggedBy != null))
				throw new ServerException(ServerException.Codes.DebuggerAlreadyAttached, _processID);
			_debuggedBy = debuggedBy;
		}
		
		private bool _breakNext;
		private int _breakOnProgramDepth;
		private int _breakOnCallDepth;
		
		internal void SetStepOver()
		{
			_breakOnProgramDepth = ExecutingPrograms.Count;
			_breakOnCallDepth = ExecutingProgram.Stack.CallDepth;
		}
		
		internal void SetStepInto()
		{
			_breakNext = true;
		}
		
		internal void ClearBreakInfo()
		{
			_breakNext = false;
			_breakOnProgramDepth = -1;
			_breakOnCallDepth = -1;
		}
		
		private bool InternalShouldBreak()
		{
			if (_breakNext)
				return true;
				
			if (ExecutingPrograms.Count < _breakOnProgramDepth)
			{
				_breakOnProgramDepth = ExecutingPrograms.Count;
				_breakOnCallDepth = ExecutingProgram.Stack.CallDepth;
			}
			
			if (ExecutingProgram.Stack.CallDepth < _breakOnCallDepth)
			{
				_breakOnCallDepth = ExecutingProgram.Stack.CallDepth;
			}
				
			if ((_breakOnProgramDepth == ExecutingPrograms.Count) && (_breakOnCallDepth == ExecutingProgram.Stack.CallDepth))
				return true;
				
			return false;
		}
		
		internal bool ShouldBreak()
		{
			if (InternalShouldBreak())
			{
				ClearBreakInfo();
				return true;
			}
			
			return false;
		}
		
		// Catalog
		public Schema.Catalog Catalog { get { return _serverSession.Server.Catalog; } }
		
		public Schema.DataTypes DataTypes { get { return _serverSession.Server.Catalog.DataTypes; } }

		// Loading
		private LoadingContexts _loadingContexts;
		
		public void PushLoadingContext(LoadingContext context)
		{
			if (_loadingContexts == null)
				_loadingContexts = new LoadingContexts();
				
			if (context.LibraryName != String.Empty)
			{
				context._currentLibrary = ServerSession.CurrentLibrary;
				ServerSession.CurrentLibrary = ServerSession.Server.Catalog.LoadedLibraries[context.LibraryName];
			}
			
			context._suppressWarnings = SuppressWarnings;
			SuppressWarnings = true;

			if (!context.IsLoadingContext)
			{
				LoadingContext currentLoadingContext = CurrentLoadingContext();
				if ((currentLoadingContext != null) && currentLoadingContext.IsLoadingContext && !currentLoadingContext.IsInternalContext)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidLoadingContext, ErrorSeverity.System);
			}

			_loadingContexts.Add(context);
		}
		
		public void PopLoadingContext()
		{
			LoadingContext context = _loadingContexts[_loadingContexts.Count - 1];
			_loadingContexts.RemoveAt(_loadingContexts.Count - 1);
			if (context.LibraryName != String.Empty)
				ServerSession.CurrentLibrary = context._currentLibrary;
			SuppressWarnings = context._suppressWarnings;
		}
		
		/// <summary>Returns true if the Server-level loading flag is set, or this process is in a loading context</summary>
		public bool IsLoading()
		{
			return InLoadingContext();
		}
		
		/// <summary>Returns true if this process is in a loading context.</summary>
		public bool InLoadingContext()
		{
			return (_loadingContexts != null) && (_loadingContexts.Count > 0) && (_loadingContexts[_loadingContexts.Count - 1].IsLoadingContext);
		}
		
		public LoadingContext CurrentLoadingContext()
		{
			if ((_loadingContexts != null) && _loadingContexts.Count > 0)
				return _loadingContexts[_loadingContexts.Count - 1];
			return null;
		}

		// Global Context
		private System.Collections.Generic.Stack<ApplicationTransaction> _applicationTransactions = new System.Collections.Generic.Stack<ApplicationTransaction>();
		public void PushGlobalContext()
		{
			ApplicationTransaction transaction = null;
			if (ApplicationTransactionID != Guid.Empty)
			{
				transaction = GetApplicationTransaction();
				transaction.PushGlobalContext();
				_applicationTransactions.Push(transaction);
			}
		}

		public void PopGlobalContext()
		{
			if (_applicationTransactions.Count > 0)
			{
				ApplicationTransaction transaction = _applicationTransactions.Pop();
				transaction.PopGlobalContext();
				Monitor.Exit(transaction);
			}
		}

		private int _reconciliationDisabledCount = 0;
		public void DisableReconciliation()
		{
			_reconciliationDisabledCount++;
		}
		
		public void EnableReconciliation()
		{
			_reconciliationDisabledCount--;
		}
		
		public bool IsReconciliationEnabled()
		{
			return _reconciliationDisabledCount <= 0;
		}
		
		public int SuspendReconciliationState()
		{
			int result = _reconciliationDisabledCount;
			_reconciliationDisabledCount = 0;
			return result;
		}
		
		public void ResumeReconciliationState(int reconciliationDisabledCount)
		{
			_reconciliationDisabledCount = reconciliationDisabledCount;
		}
	}

	// ServerProcesses
	public class ServerProcesses : ServerChildObjects
	{		
		protected override void Validate(ServerChildObject objectValue)
		{
			if (!(objectValue is ServerProcess))
				throw new ServerException(ServerException.Codes.ServerProcessContainer);
		}
		
		public new ServerProcess this[int index]
		{
			get { return (ServerProcess)base[index]; } 
			set { base[index] = value; } 
		}
		
		public ServerProcess GetProcess(int processID)
		{
			foreach (ServerProcess process in this)
				if (process.ProcessID == processID)
					return process;
			throw new ServerException(ServerException.Codes.ProcessNotFound, processID);
		}
	}
}
