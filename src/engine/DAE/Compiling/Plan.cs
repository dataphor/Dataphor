/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define CHECKCLASSDEPENDENCY // Indicates whether or not class dependencies will be checked
//#define USEPROCESSDISPOSED // Determines whether or not the plan and program listen to the process disposed event

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Alphora.Dataphor.DAE.Compiling
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	// Plan provides compile time state	for the compiler.
	public class Plan : Disposable
	{
		public Plan(ServerProcess serverProcess) : base()
		{
			SetServerProcess(serverProcess);
			_symbols = new Symbols(_serverProcess.ServerSession.SessionInfo.DefaultMaxStackDepth, _serverProcess.ServerSession.SessionInfo.DefaultMaxCallDepth);
			PushSecurityContext(new SecurityContext(_serverProcess.ServerSession.User));
			PushStatementContext(new StatementContext(StatementType.Select));
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					try
					{
						if (_statementContexts.Count > 0)
							PopStatementContext();
								
					}
					finally
					{
						if (_securityContexts.Count > 0)
							PopSecurityContext();
					}
				}
				finally
				{
					if (_symbols != null)
						_symbols = null;
				}
			}
			finally
			{
				SetServerProcess(null);

				base.Dispose(disposing);
			}
		}

		// Process        
		protected ServerProcess _serverProcess;
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
		
		public void BindToProcess(ServerProcess process)
		{
			PopSecurityContext();
			PushSecurityContext(new SecurityContext(process.ServerSession.User));
			
			SetServerProcess(process);
		}
		
		public void UnbindFromProcess()
		{
			#if USEPROCESSUNBIND
			SetServerProcess(null);
			#endif
		}
		
		public void CheckCompiled()
		{
			if (_messages.HasErrors)
				throw _messages.FirstError;
			// TODO: throw an AggregateException if there is more than 1 error
		}
		
		// Statistics
		private PlanStatistics _statistics = new PlanStatistics();
		public PlanStatistics Statistics { get { return _statistics; } }
		
		public void EnsureDeviceStarted(Schema.Device device)
		{
			_serverProcess.EnsureDeviceStarted(device);
		}
		
		public void CheckDeviceReconcile(Schema.BaseTableVar tableVar)
		{
			tableVar.Device.CheckReconcile(_serverProcess, tableVar);
		}
		
		public Schema.DeviceSession DeviceConnect(Schema.Device device)
		{
			return _serverProcess.DeviceConnect(device);
		}
		
		public RemoteSession RemoteConnect(Schema.ServerLink link)
		{
			return _serverProcess.RemoteConnect(link);
		}
		
		/// <summary>
		/// Used to evaluate literal arguments at compile-time. The given node must be literal, or an exception is raised.
		/// </summary>
		public object EvaluateLiteralArgument(PlanNode node, string argumentName)
		{
			if (!node.IsLiteral)
				throw new CompilerException(CompilerException.Codes.LiteralArgumentRequired, CompilerErrorLevel.NonFatal, argumentName);

			return ExecuteNode(node);
		}
		
		/// <summary>
		/// Used to execute arbitrary plan nodes at compile-time.
		/// </summary>
		public object ExecuteNode(PlanNode node)
		{
			var program = new Program(_serverProcess);
			program.Code = node;
			return program.Execute(null);
		}
		
		// Symbols        
		protected Symbols _symbols;
		public Symbols Symbols { get { return _symbols; } }
		
		protected List<Symbol> _newSymbols;
		public List<Symbol> NewSymbols { get { return _newSymbols; } }
		
		public void ReportProcessSymbols()
		{
			_newSymbols = new List<Symbol>();
			for (int index = _symbols.FrameCount - 1; index >= 0; index--)
				_newSymbols.Add(_symbols[index]);
		}
		
		// InErrorContext
		protected int _errorContextCount;
		public bool InErrorContext { get { return _errorContextCount > 0; } }
		
		public void EnterErrorContext()
		{
			_errorContextCount++;
		}
		
		public void ExitErrorContext()
		{
			_errorContextCount--;
		}
        
		// LoopContext
		protected int _loopCount;
		public bool InLoop { get { return _loopCount > 0; } }
        
		public void EnterLoop() 
		{ 
			_loopCount++; 
		}
        
		public void ExitLoop() 
		{ 
			_loopCount--; 
		}
		
		// RowContext
		public bool InRowContext { get { return _symbols.InRowContext; } }
		
		public void EnterRowContext()
		{
			_symbols.PushFrame(true);
		}
		
		public void ExitRowContext()
		{
			_symbols.PopFrame();
		}
		
		// ATCreationContext
		private int _aTCreationContext;
		/// <summary>Indicates whether the current plan is executing a statement to create an A/T translated object.</summary>
		/// <remarks>
		/// This context is needed because it is not always the case that A/T objects will be being created (or recreated 
		/// such as when view references are reinferred) inside of an A/T. By checking for this context, we are ensured
		/// that things that should not be checked for A/T objects (such as errors about derived references not existing in
		/// adorn expressions, etc.,.) will not occur.
		/// </remarks>
		public bool InATCreationContext { get { return _aTCreationContext > 0; } }
		
		public void PushATCreationContext()
		{
			_aTCreationContext++;
		}
		
		public void PopATCreationContext()
		{
			_aTCreationContext--;
		}
		
		// ApplicationTransactionID
		public Guid ApplicationTransactionID { get { return _serverProcess.ApplicationTransactionID; } }
		
		public ApplicationTransaction GetApplicationTransaction()
		{
			return _serverProcess.GetApplicationTransaction();
		}
		
		public bool IsInsert 
		{
			get { return _serverProcess.IsInsert; } 
			set { _serverProcess.IsInsert = value; }
		}
		
		public void EnsureApplicationTransactionOperator(Schema.Operator operatorValue)
		{
			if (ApplicationTransactionID != Guid.Empty && operatorValue.IsATObject)
			{
				ApplicationTransaction transaction = GetApplicationTransaction();
				try
				{
					transaction.EnsureATOperatorMapped(_serverProcess, operatorValue);
				}
				finally
				{
					Monitor.Exit(transaction);
				}
			}
		}
		
		public void EnsureApplicationTransactionTableVar(Schema.TableVar tableVar)
		{
			if (ApplicationTransactionID != Guid.Empty && tableVar.IsATObject)
			{
				ApplicationTransaction transaction = GetApplicationTransaction();
				try
				{
					transaction.EnsureATTableVarMapped(_serverProcess, tableVar);
				}
				finally
				{
					Monitor.Exit(transaction);
				}
			}
		}
		
		/// <summary>Indicates whether time stamps should be affected by alter and drop table variable and operator statements.</summary>
		public bool ShouldAffectTimeStamp { get { return _serverProcess.ShouldAffectTimeStamp; } }
		
		public void EnterTimeStampSafeContext()
		{
			_serverProcess.EnterTimeStampSafeContext();
		}
		
		public void ExitTimeStampSafeContext()
		{
			_serverProcess.ExitTimeStampSafeContext();
		}
		
		protected int _typeOfCount;
		public bool InTypeOfContext { get { return _typeOfCount > 0; } }
		
		public void PushTypeOfContext()
		{
			_typeOfCount++;
		}
		
		public void PopTypeOfContext()
		{
			_typeOfCount--;
		}
		
		// TypeContext
		protected System.Collections.Generic.Stack<Schema.IDataType> _typeStack = new System.Collections.Generic.Stack<Schema.IDataType>();
		
		public void PushTypeContext(Schema.IDataType dataType)
		{
			_typeStack.Push(dataType);
		}
		
		public void PopTypeContext(Schema.IDataType dataType)
		{
			Error.AssertFail(_typeStack.Count > 0, "Type stack underflow");
			_typeStack.Pop();
		}
		
		public bool InScalarTypeContext()
		{
			return (_typeStack.Count > 0) && (_typeStack.Peek() is Schema.IScalarType);
		}
		
		public bool InRowTypeContext()
		{
			return (_typeStack.Count > 0) && (_typeStack.Peek() is Schema.IRowType);
		}
		
		public bool InTableTypeContext()
		{
			return (_typeStack.Count > 0) && (_typeStack.Peek() is Schema.ITableType);
		}

		public bool InListTypeContext()
		{
			return (_typeStack.Count > 0) && (_typeStack.Peek() is Schema.IListType);
		}
		
		// CurrentStatement
		protected System.Collections.Generic.Stack<Statement> _statementStack = new System.Collections.Generic.Stack<Statement>();
		
		public void PushStatement(Statement statement)
		{
			_statementStack.Push(statement);
		}
		
		public void PopStatement()
		{
			Error.AssertFail(_statementStack.Count > 0, "Statement stack underflow");
			_statementStack.Pop();
		}
		
		/// <remarks>Returns the current statement in the abstract syntax tree being compiled.  Will return null if no statement is on the statement stack.</remarks>
		public Statement CurrentStatement()
		{
			if (_statementStack.Count > 0)
				return _statementStack.Peek();
			return null;
		}
		
		/// <summary>
		/// Returns the first non-empty LineInfo in the current statement stack, null if no LineInfo is found.
		/// </summary>
		public LineInfo GetCurrentLineInfo()
		{
			foreach (Statement statement in _statementStack)
				if (statement.Line != -1)
					return statement.LineInfo;
					
			return null;
		}

		// CursorContext
		protected CursorContexts _cursorContexts = new CursorContexts();
		public void PushCursorContext(CursorContext context)
		{
			_cursorContexts.Add(context);
		}
        
		public void PopCursorContext()
		{
			_cursorContexts.RemoveAt(_cursorContexts.Count - 1);
		}
        
		public CursorContext GetDefaultCursorContext()
		{
			return new CursorContext(CursorType.Dynamic, CursorCapability.Navigable | (ServerProcess.ServerSession.SessionInfo.ShouldElaborate ? CursorCapability.Elaborable : CursorCapability.None), CursorIsolation.None);
		}
        
		public CursorContext CursorContext
		{
			get
			{
				if (_cursorContexts.Count > 0)
					return _cursorContexts[_cursorContexts.Count - 1];
				else
					return GetDefaultCursorContext();
			}
		}
		
		// StatementContext
		protected StatementContexts _statementContexts = new StatementContexts();
		public void PushStatementContext(StatementContext context)
		{
			_statementContexts.Add(context);
		}
		
		public void PopStatementContext()
		{
			_statementContexts.RemoveAt(_statementContexts.Count - 1);
		}
		
		public StatementContext GetDefaultStatementContext()
		{
			return new StatementContext(StatementType.Select);
		}
		
		public bool HasStatementContext { get { return _statementContexts.Count > 0; } }

		public StatementContext StatementContext { get { return _statementContexts[_statementContexts.Count - 1]; } }
		
		// SecurityContext
		protected SecurityContexts _securityContexts = new SecurityContexts();
		public void PushSecurityContext(SecurityContext context)
		{
			_securityContexts.Add(context);
		}
		
		public void PopSecurityContext()
		{
			_securityContexts.RemoveAt(_securityContexts.Count - 1);
		}
		
		public bool HasSecurityContext { get { return _securityContexts.Count > 0; } }
		
		public SecurityContext SecurityContext { get { return _securityContexts[_securityContexts.Count - 1]; } }
		
		public void UpdateSecurityContexts(Schema.User user)
		{
			for (int index = 0; index < _securityContexts.Count; index++)
				if (_securityContexts[index].User.ID == user.ID)
					_securityContexts[index].SetUser(user);
		}
		
		// LoadingContext
		public void PushLoadingContext(LoadingContext context)
		{
			_serverProcess.PushLoadingContext(context);
		}
		
		public void PopLoadingContext()
		{
			_serverProcess.PopLoadingContext();
		}
		
		public LoadingContext CurrentLoadingContext()
		{
			return _serverProcess.CurrentLoadingContext();
		}
		
		public bool IsLoading()
		{
			return _serverProcess.IsLoading();
		}
		
		public bool InLoadingContext()
		{
			return _serverProcess.InLoadingContext();
		}

		// GlobalContext
		public void PushGlobalContext()
		{
			_serverProcess.PushGlobalContext();
		}

		public void PopGlobalContext()
		{
			_serverProcess.PopGlobalContext();
		}
		
		// A Temporary catalog where catalog objects are registered during compilation
		protected Schema.Catalog _planCatalog;
		public Schema.Catalog PlanCatalog
		{
			get
			{
				if (_planCatalog == null)
					_planCatalog = new Schema.Catalog();
				return _planCatalog;
			}
		}
		
		// A temporary list of session table variables created during compilation
		protected Schema.Objects _planSessionObjects;
		public Schema.Objects PlanSessionObjects 
		{
			get
			{
				if (_planSessionObjects  == null)
					_planSessionObjects = new Schema.Objects();
				return _planSessionObjects;
			}
		}
		
		protected Schema.Objects _planSessionOperators;
		public Schema.Objects PlanSessionOperators
		{
			get
			{
				if (_planSessionOperators == null)
					_planSessionOperators = new Schema.Objects();
				return _planSessionOperators;
			}
		}
		
		// SessionObjects
		public Schema.Objects SessionObjects { get { return _serverProcess.ServerSession.SessionObjects; } }
		
		public Schema.Objects SessionOperators { get { return _serverProcess.ServerSession.SessionOperators; } }
		
		public int SessionID { get { return _serverProcess.ServerSession.SessionID; } }
		
		// Catalog		
		public bool IsEngine { get { return _serverProcess.ServerSession.Server.IsEngine; } }
		
		public Schema.Catalog Catalog { get { return _serverProcess.ServerSession.Server.Catalog; } }
		
		public Schema.DataTypes DataTypes { get { return _serverProcess.DataTypes; } }
		
		public QueryLanguage Language { get { return _serverProcess.ProcessInfo.Language; } }
		
		public bool ShouldEmitIL { get { return _serverProcess.ServerSession.SessionInfo.ShouldEmitIL; } }
		
		public bool SuppressWarnings
		{
			get { return _serverProcess.SuppressWarnings; }
			set { _serverProcess.SuppressWarnings = value; }
		}
		
		public CatalogDeviceSession CatalogDeviceSession { get { return _serverProcess.CatalogDeviceSession; } }
		
		public Schema.User User { get { return SecurityContext.User; } }
		
		public Schema.LoadedLibrary CurrentLibrary { get { return _serverProcess.ServerSession.CurrentLibrary; } }
		
		public Schema.NameResolutionPath NameResolutionPath { get { return _serverProcess.ServerSession.NameResolutionPath; } }
		
		public IStreamManager StreamManager { get { return _serverProcess.StreamManager; } }
		
		public IValueManager ValueManager { get { return _serverProcess.ValueManager; } }

		public Schema.Device TempDevice { get { return _serverProcess.ServerSession.Server.TempDevice; } }
		
		public CursorManager CursorManager { get { return _serverProcess.ServerSession.CursorManager; } }

		public string DefaultTagNameSpace { get { return String.Empty; } }
		
		public string DefaultDeviceName { get { return GetDefaultDeviceName(_serverProcess.ServerSession.CurrentLibrary.Name, false); } }
		
		public string GetDefaultDeviceName(string libraryName, bool shouldThrow)
		{
			return GetDefaultDeviceName(Catalog.Libraries[libraryName], shouldThrow);
		}

		private SourceContexts _sourceContexts;
				
		public SourceContext SourceContext 
		{ 
			get 
			{ 
				if (_sourceContexts == null)
					return null;
					
				if (_sourceContexts.Count == 0)
					return null;
					
				return _sourceContexts.Peek();
			}
		}
		
		public void PushSourceContext(SourceContext sourceContext)
		{
			if (_sourceContexts == null)
				_sourceContexts = new SourceContexts();
				
			_sourceContexts.Push(sourceContext);
		}
		
		public void PopSourceContext()
		{
			_sourceContexts.Pop();
		}
		
		protected string GetDefaultDeviceName(Schema.Library library, bool shouldThrow)
		{
			if (library.DefaultDeviceName != String.Empty)
				return library.DefaultDeviceName;
			
			Schema.Libraries libraries = new Schema.Libraries();
			libraries.Add(library);
			return GetDefaultDeviceName(libraries, shouldThrow);
		}
		
		protected string GetDefaultDeviceName(Schema.Libraries libraries, bool shouldThrow)
		{
			while (libraries.Count > 0)
			{
				Schema.Library library = libraries[0];
				libraries.RemoveAt(0);
				
				string defaultDeviceName = String.Empty;
				Schema.Library requiredLibrary;
				foreach (Schema.LibraryReference libraryReference in library.Libraries)
				{
					requiredLibrary = Catalog.Libraries[libraryReference.Name];
					if (requiredLibrary.DefaultDeviceName != String.Empty)
					{
						if (defaultDeviceName != String.Empty)
							if (shouldThrow)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.AmbiguousDefaultDeviceName, library.Name);
							else
								return String.Empty;
						defaultDeviceName = requiredLibrary.DefaultDeviceName;
					}
					else
						if (!libraries.Contains(requiredLibrary))
							libraries.Add(requiredLibrary);
				}
					
				if (defaultDeviceName != String.Empty)
					return defaultDeviceName;
			}
			
			return String.Empty;
		}

		public void CheckRight(string right)
		{
			if (!InATCreationContext)
				_serverProcess.CatalogDeviceSession.CheckUserHasRight(SecurityContext.User.ID, right);
		}
		
		public bool HasRight(string right)
		{
			return InATCreationContext || CatalogDeviceSession.UserHasRight(SecurityContext.User.ID, right);
		}

		/// <summary>Raises an error if the current user is not authorized to administer the given user.</summary>		
		public void CheckAuthorized(string userID)
		{
			if (!User.IsAdminUser() || !User.IsSystemUser() || (!String.Equals(userID, User.ID, StringComparison.OrdinalIgnoreCase)))
				CheckRight(Schema.RightNames.AlterUser);
		}
		
		// During compilation, the creation object stack maintains a context for the
		// object currently being created.  References into the catalog register dependencies
		// against this object as they are encountered by the compiler.
		protected List<Schema.Object> _creationObjects;
		protected List<LineInfo> _compilingOffsets;

		public void PushCreationObject(Schema.Object objectValue, LineInfo lineInfo)
		{
			if (_creationObjects == null)
				_creationObjects = new List<Schema.Object>();
			_creationObjects.Add(objectValue);
			if (_compilingOffsets == null)
				_compilingOffsets = new List<LineInfo>();
			_compilingOffsets.Add(lineInfo);
		}
		
		public void PushCreationObject(Schema.Object objectValue)
		{
			PushCreationObject(objectValue, new LineInfo(LineInfo.StartingOffset));
		}
		
		public void PopCreationObject()
		{
			_creationObjects.RemoveAt(_creationObjects.Count - 1);
			_compilingOffsets.RemoveAt(_compilingOffsets.Count - 1);
		}
		
		public LineInfo CompilingOffset
		{
			get
			{
				if ((_compilingOffsets == null) || (_compilingOffsets.Count == 0))
					return null;
				return _compilingOffsets[_compilingOffsets.Count - 1];
			}
		}
		
		public Schema.Object CurrentCreationObject()
		{
			if ((_creationObjects == null) || (_creationObjects.Count == 0))
				return null;
			return _creationObjects[_creationObjects.Count - 1];
		}
		
		public bool IsOperatorCreationContext
		{
			get
			{
				return 
					(_creationObjects != null) && 
					(_creationObjects.Count > 0) && 
					(_creationObjects[_creationObjects.Count - 1] is Schema.Operator);
			}
		}
		
		public void CheckClassDependency(ClassDefinition classDefinition)
		{
			if ((_creationObjects != null) && (_creationObjects.Count > 0))
			{
				Schema.Object creationObject = (Schema.Object)_creationObjects[_creationObjects.Count - 1];
				if 
				(
					(creationObject is Schema.CatalogObject) && 
					(((Schema.CatalogObject)creationObject).Library != null) &&
					(((Schema.CatalogObject)creationObject).SessionObjectName == null) && 
					(
						!(creationObject is Schema.TableVar) || 
						(((Schema.TableVar)creationObject).SourceTableName == null)
					)
				)
				{
					CheckClassDependency(((Schema.CatalogObject)creationObject).Library, classDefinition);
				}
			}
		}
		
		[Conditional("CHECKCLASSDEPENDENCY")]
		public void CheckClassDependency(Schema.LoadedLibrary library, ClassDefinition classDefinition)
		{
			Schema.RegisteredClass classValue = Catalog.ClassLoader.GetClass(CatalogDeviceSession, classDefinition);
			if (!library.IsRequiredLibrary(classValue.Library))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.NonRequiredClassDependency, classValue.Name, library.Name, classValue.Library.Name); // TODO: Better exception
		}
		
		public void AttachDependencies(Schema.ObjectList dependencies)
		{
			for (int index = 0; index < dependencies.Count; index++)
				AttachDependency(dependencies.ResolveObject(CatalogDeviceSession, index));
		}
		
		public void AttachDependency(Schema.Object objectValue)
		{
			if (_creationObjects == null)
				_creationObjects = new List<Schema.Object>();
			if (_creationObjects.Count > 0)
			{
				// If this is a generated object, attach the dependency to the generator, rather than the object directly.
				// Unless it is a device object, in which case the dependency should be attached to the generated object.
				if (objectValue.IsGenerated && !(objectValue is Schema.DeviceObject) && (objectValue.GeneratorID >= 0))
					objectValue = objectValue.ResolveGenerator(CatalogDeviceSession);
					
				if (objectValue is Schema.Property)
					objectValue = ((Schema.Property)objectValue).Representation;
					
				Schema.Object localObjectValue = (Schema.Object)_creationObjects[_creationObjects.Count - 1];
				if ((localObjectValue != objectValue) && (!(objectValue is Schema.Reference) || (localObjectValue is Schema.TableVar)) && (!localObjectValue.HasDependencies() || !localObjectValue.Dependencies.Contains(objectValue.ID)))
				{
					if (!_serverProcess.ServerSession.Server.IsEngine)
					{
						if 
						(
							(localObjectValue.Library != null) &&
							!localObjectValue.IsSessionObject &&
							!localObjectValue.IsATObject
						)
						{
							Schema.LoadedLibrary library = localObjectValue.Library;
							Schema.LoadedLibrary dependentLibrary = objectValue.Library;
							if (dependentLibrary == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NonLibraryDependency, localObjectValue.Name, library.Name, objectValue.Name);
							if (!library.IsRequiredLibrary(dependentLibrary) && (!String.Equals(dependentLibrary.Name, Engine.SystemLibraryName, StringComparison.OrdinalIgnoreCase))) // Ignore dependencies to the system library, these are implicitly available
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NonRequiredLibraryDependency, localObjectValue.Name, library.Name, objectValue.Name, dependentLibrary.Name);
						}
						
						// if LObject is not a session object or an AT object, AObject cannot be a session object
						if ((localObjectValue is Schema.CatalogObject) && !localObjectValue.IsSessionObject && !localObjectValue.IsATObject && objectValue.IsSessionObject)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.SessionObjectDependency, localObjectValue.Name, ((Schema.CatalogObject)objectValue).SessionObjectName);
								
						// if LObject is not a generated or an AT object or a plan object (Name == SessionObjectName), AObject cannot be an AT object
						if (((localObjectValue is Schema.CatalogObject) && (((Schema.CatalogObject)localObjectValue).SessionObjectName != localObjectValue.Name)) && !localObjectValue.IsGenerated && !localObjectValue.IsATObject && objectValue.IsATObject)
							if (objectValue is Schema.TableVar)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.ApplicationTransactionObjectDependency, localObjectValue.Name, ((Schema.TableVar)objectValue).SourceTableName);
							else
								throw new Schema.SchemaException(Schema.SchemaException.Codes.ApplicationTransactionObjectDependency, localObjectValue.Name, ((Schema.Operator)objectValue).SourceOperatorName);
					}
							
					Error.AssertFail(objectValue.ID > 0, "Object {0} does not have an object id and cannot be tracked as a dependency.", objectValue.Name);
					localObjectValue.AddDependency(objectValue);
				}
			}
		}
		
		public void SetIsLiteral(bool isLiteral)
		{		
			if ((_creationObjects != null) && (_creationObjects.Count > 0))
			{
				Schema.Operator operatorValue = _creationObjects[_creationObjects.Count - 1] as Schema.Operator;
				if (operatorValue != null)
					operatorValue.IsLiteral = operatorValue.IsLiteral && isLiteral;
			}
		}
		
		public void SetIsFunctional(bool isFunctional)
		{
			if ((_creationObjects != null) && (_creationObjects.Count > 0))
			{
				Schema.Operator operatorValue = _creationObjects[_creationObjects.Count - 1] as Schema.Operator;
				if (operatorValue != null)
					operatorValue.IsFunctional = operatorValue.IsFunctional && isFunctional;
			}
		}
		
		public void SetIsDeterministic(bool isDeterministic)
		{
			if ((_creationObjects != null) && (_creationObjects.Count > 0))
			{
				Schema.Operator operatorValue = _creationObjects[_creationObjects.Count - 1] as Schema.Operator;
				if (operatorValue != null)
					operatorValue.IsDeterministic = operatorValue.IsDeterministic && isDeterministic;
			}
		}
		
		public void SetIsRepeatable(bool isRepeatable)
		{
			if ((_creationObjects != null) && (_creationObjects.Count > 0))
			{
				Schema.Operator operatorValue = _creationObjects[_creationObjects.Count - 1] as Schema.Operator;
				if (operatorValue != null)
					operatorValue.IsRepeatable = operatorValue.IsRepeatable && isRepeatable;
			}
		}
		
		public void SetIsNilable(bool isNilable)
		{
			if ((_creationObjects != null) && (_creationObjects.Count > 0))
			{
				Schema.Operator operatorValue = _creationObjects[_creationObjects.Count - 1] as Schema.Operator;
				if (operatorValue != null)
					operatorValue.IsNilable = operatorValue.IsNilable || isNilable;
			}
		}
		
		// True if the current expression is being evaluated as the target of an assignment		
		public bool IsAssignmentTarget
		{
			get 
			{ 
				return (StatementContext.StatementType == StatementType.Assignment);
			}
		}
		
		// Messages
		private CompilerMessages _messages = new CompilerMessages();
		public CompilerMessages Messages { get { return _messages; } }
		
		#if ACCUMULATOR
		// Performance Tracking
		public long Accumulator = 0;
		#endif

		public Type CreateType(ClassDefinition classDefinition)
		{
			return Catalog.ClassLoader.CreateType(CatalogDeviceSession, classDefinition);
		}
		
		public object CreateObject(ClassDefinition classDefinition, object[] actualParameters)
		{
			return Catalog.ClassLoader.CreateObject(CatalogDeviceSession, classDefinition, actualParameters);
		}
	}
}
