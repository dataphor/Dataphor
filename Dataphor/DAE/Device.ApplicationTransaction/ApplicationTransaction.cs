/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USENAMEDROWVARIABLES // Can be disabled independently in this unit without being disabled globally

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Alphora.Dataphor.DAE.Device.ApplicationTransaction
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Debug;

	public sealed class ApplicationTransactionUtility : System.Object
	{
		public static string NameFromID(Guid iD)
		{
			return String.Format("AT_{0}", iD.ToString().Replace("-", "_"));
		}
		
		/// <summary>Gets the application transaction and acquires a lock on it. The caller is responsible for releasing the lock.</summary>
		public static ApplicationTransaction GetTransaction(ServerProcess process, Guid iD)
		{
			ApplicationTransaction transaction;
			if (!process.ServerSession.Server.ATDevice.ApplicationTransactions.TryGetValue(iD, out transaction))
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.InvalidApplicationTransactionID, iD);
				
			Monitor.Enter(transaction);
			return transaction;
		}

		private static void Cleanup(ServerProcess process, ApplicationTransaction transaction)
		{
			Exception exception = null;
			foreach (Operation operation in transaction.Operations)
				try
				{
					operation.Dispose(process.ValueManager);
				}
				catch (Exception E)
				{
					exception = E;
				}
				
			foreach (TableMap tableMap in transaction.TableMaps)
			{
				try
				{
					if (tableMap.TableVar is Schema.BaseTableVar)
						transaction.Tables[tableMap.TableVar].Drop(process.ValueManager);
				}
				catch (Exception E)
				{
					exception = E;
				}
				try
				{
					if (tableMap.DeletedTableVar is Schema.BaseTableVar)
						transaction.Tables[tableMap.DeletedTableVar].Drop(process.ValueManager);
				}
				catch (Exception E)
				{
					exception = E;
				}
			}
			
			ServerProcess[] processes = new ServerProcess[transaction.Processes.Count];
			int counter = 0;
			foreach (ServerProcess localProcess in transaction.Processes.Values)
			{
				processes[counter] = localProcess;
				counter++;
			}
			
			for (int index = 0; index < processes.Length; index++)
				try
				{
					processes[index].LeaveApplicationTransaction();
				}
				catch (Exception E)
				{
					exception = E;
				}
		}

		public static Guid BeginApplicationTransaction(ServerProcess process)
		{
			ApplicationTransaction transaction = new ApplicationTransaction(process.ServerSession);
			transaction.Session.ApplicationTransactions.Add(transaction.ID, transaction);
			process.ServerSession.Server.ATDevice.ApplicationTransactions.Add(transaction.ID, transaction);
			return transaction.ID;
		}
		
		private static void EndApplicationTransaction(ServerProcess process, Guid iD)
		{
			ApplicationTransaction transaction = GetTransaction(process, iD);
			try
			{
				try
				{
					try
					{
						if (!transaction.Closed)
							RollbackApplicationTransaction(process, iD);
					}
					finally
					{
						Cleanup(process, transaction);
					}
				}
				finally
				{
					transaction.Session.ApplicationTransactions.Remove(iD);
					transaction.Device.ApplicationTransactions.Remove(iD);
				}
			}
			finally
			{
				Monitor.Exit(transaction);
			}
		}
		
		public static void JoinApplicationTransaction(ServerProcess process, Guid iD)
		{
			if (process.ApplicationTransactionID != Guid.Empty)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ProcessAlreadyParticipating);
			
			ApplicationTransaction transaction = GetTransaction(process, iD);
			try
			{
				if (transaction.Closed)
					throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ApplicationTransactionClosed);
				transaction.Processes.Add(process.ProcessID, process);
			}
			finally
			{
				Monitor.Exit(transaction);
			}
		}
		
		public static void LeaveApplicationTransaction(ServerProcess process)
		{
			if (process.ApplicationTransactionID != Guid.Empty)
			{
				ApplicationTransaction transaction = GetTransaction(process, process.ApplicationTransactionID);
				try
				{
					transaction.Processes.Remove(process.ProcessID);
					process.DeviceDisconnect(transaction.Device); // Disconnect the session to ensure that the saved pointer to this AT is cleared
				}
				finally
				{
					Monitor.Exit(transaction);
				}
			}
		}

		public static void SetExplicitBind(PlanNode node)
		{
			SetExplicitBind(node, true);
		}

		public static void ClearExplicitBind(PlanNode node)
		{
			SetExplicitBind(node, false);
		}
		
		public static void SetExplicitBind(PlanNode node, bool shouldBind)
		{
			var tableVarNode = node as TableVarNode;
			if (tableVarNode != null && tableVarNode.TableVar.IsATObject)
				tableVarNode.ExplicitBind = shouldBind;
				
			for (int index = 0; index < node.NodeCount; index++)
				SetExplicitBind(node.Nodes[index], shouldBind);
		}

		public static TableNode PrepareJoinExpression(Plan plan, TableNode sourceNode, out TableNode populateNode)
		{
			ApplicationTransaction transaction = plan.GetApplicationTransaction();
			try
			{
				transaction.PushGlobalContext();
				try
				{
					plan.PushATCreationContext();
					try
					{
						populateNode = Compiler.EnsureTableNode(plan, Compiler.CompileExpression(plan, (Expression)sourceNode.EmitStatement(EmitMode.ForCopy)));
					}
					finally
					{
						plan.PopATCreationContext();
					}
				}
				finally
				{
					transaction.PopGlobalContext();
				}

				SetExplicitBind(sourceNode);
				return sourceNode;
			}
			finally
			{
				Monitor.Exit(transaction);
			}
		}
		
		public static void JoinExpression(Program program, TableNode populateNode, TableNode translatedNode)
		{
			ApplicationTransaction transaction = program.ServerProcess.GetApplicationTransaction();
			try
			{
				transaction.BeginPopulateSource(program.ServerProcess);
				try
				{
					using (ITable table = (ITable)populateNode.Execute(program))
					{
						Row row = new Row(program.ValueManager, table.DataType.RowType);
						try
						{
							while (table.Next())
							{
								table.Select(row);
								translatedNode.JoinApplicationTransaction(program, row);
							}
						}
						finally
						{
							row.Dispose();
						}
					}
				}
				finally
				{
					transaction.EndPopulateSource();
				}
			}
			finally
			{
				Monitor.Exit(transaction);
			}
		}
		
		public static void PrepareApplicationTransaction(ServerProcess process, Guid iD)
		{
			Program program = new Program(process);
			program.Start(null);
			try
			{
				PrepareApplicationTransaction(program, iD);
			}
			finally
			{
				program.Stop(null);
			}
		}
		
		public static void PrepareApplicationTransaction(Program program, Guid iD)
		{
			Guid saveATID = program.ServerProcess.ApplicationTransactionID;
			if (program.ServerProcess.ApplicationTransactionID != iD)
				program.ServerProcess.JoinApplicationTransaction(iD, true);
			try
			{
				ApplicationTransaction transaction = GetTransaction(program.ServerProcess, iD);
				try
				{
					transaction.PushGlobalContext();
					try
					{
						ApplicationTransactionDeviceSession session = (ApplicationTransactionDeviceSession)program.DeviceConnect(transaction.Device);
						ApplicationTransactionDeviceTransaction deviceTransaction = null;
						if (session.InTransaction)
							deviceTransaction = session.Transactions.CurrentTransaction();
						if (!transaction.Prepared)
						{
							transaction.EnterATReplayContext();
							try
							{
								// This code relies on the assumption that operations will only ever be added to the end of the list of operations
								Operation operation;
								for (int index = 0; index < transaction.Operations.Count; index++)
								{
									operation = transaction.Operations[index];
									operation.Apply(program);
									deviceTransaction.AppliedOperations.Add(operation);
								}
								transaction.Prepared = true;
							}
							finally
							{
								transaction.ExitATReplayContext();
							}
						}
					}
					finally
					{
						transaction.PopGlobalContext();
					}
				}
				finally
				{
					Monitor.Exit(transaction);
				}
			}
			finally
			{
				if (program.ServerProcess.ApplicationTransactionID != saveATID)
					program.ServerProcess.LeaveApplicationTransaction();
			}
		}
		
		public static void CommitApplicationTransaction(ServerProcess process, Guid iD)
		{
			Program program = new Program(process);
			program.Start(null);
			try
			{
				CommitApplicationTransaction(program, iD);
			}
			finally
			{
				program.Stop(null);
			}
		}
		
		public static void CommitApplicationTransaction(Program program, Guid iD)
		{
			ApplicationTransaction transaction = GetTransaction(program.ServerProcess, iD);
			try
			{
				if (!transaction.Prepared)
					PrepareApplicationTransaction(program, iD);
					
				transaction.Closed = true;
				
				EndApplicationTransaction(program.ServerProcess, iD);
			}
			finally
			{
				Monitor.Exit(transaction);
			}
		}
		
		public static void RollbackApplicationTransaction(ServerProcess process, Guid iD)
		{
			Program program = new Program(process);
			program.Start(null);
			try
			{
				RollbackApplicationTransaction(program, iD);
			}
			finally
			{
				program.Stop(null);
			}
		}
		
		public static void RollbackApplicationTransaction(Program program, Guid iD)
		{
			ApplicationTransaction transaction = GetTransaction(program.ServerProcess, iD);
			try
			{
				Exception exception = null;

				if (transaction.Prepared)
				{
					transaction.PushGlobalContext();
					try
					{
						for (int index = transaction.Operations.Count - 1; index >= 0; index--)
						{
							try
							{
								transaction.Operations[index].Undo(program);
							}
							catch (Exception E)
							{
								exception = E;
								program.ServerProcess.ServerSession.Server.LogError(E);
							}
						}
						
						transaction.Prepared = false;

					}
					finally
					{
						transaction.PopGlobalContext();
					}
				}
				
				transaction.Closed = true;

				EndApplicationTransaction(program.ServerProcess, iD);

				if (exception != null)
					throw exception;
			}
			finally
			{
				Monitor.Exit(transaction);
			}
		}
	}
	
	public abstract class ApplicationTransactionNode : InstructionNode {}

	/// <remarks>
	/// operator BeginApplicationTransaction() : Guid;
	///	operator BeginApplicationTransaction(const AShouldJoin : Boolean, const AShouldInsert : Boolean) : Guid;
	///	Initiates an application transaction in the server and returns the ID 
	///	of that transaction to be used in subsequent application transaction calls.
	///	</remarks>
	public class BeginApplicationTransactionNode : ApplicationTransactionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length > 0)
				return program.ServerProcess.BeginApplicationTransaction((bool)arguments[0], (bool)arguments[1]);
			else
				return program.ServerProcess.BeginApplicationTransaction(false, false);
		}
	}
	
	/// <remarks>
	///	operator JoinApplicationTransaction(AID : Guid, AIsInsert : Boolean);
	///	Joins this process to the given application transaction.
	///	</remarks>
	public class JoinApplicationTransactionNode : ApplicationTransactionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Guid applicationTransactionID = (Guid)arguments[0];
			bool isInsert = (bool)arguments[1];
			program.ServerProcess.JoinApplicationTransaction(applicationTransactionID, isInsert);
			return null;
		}
	}
	
	/// <remarks>
	///	operator LeaveApplicationTransaction(): String;
	///	Leaves the application transaction this process is participating in.
	///	</remarks>
	public class LeaveApplicationTransactionNode : ApplicationTransactionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.LeaveApplicationTransaction();
			return null;
		}
	}

	public class PrepareApplicationTransactionNode : ApplicationTransactionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.PrepareApplicationTransaction((Guid)arguments[0]);
			return null;
		}
	}
	
	public abstract class CompleteApplicationTransactionNode : ApplicationTransactionNode{}

	/// <remarks>
	///	operator CommitApplicationTransaction(AID : Guid);
	///	Accepts all the changes made during the application transaction.
	/// Once an application transaction has been successfully committed, it is an error to attempt any further manipulations in the transaction.
	/// </remarks>
	public class CommitApplicationTransactionNode : CompleteApplicationTransactionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Guid applicationTransactionID = (Guid)arguments[0];
			program.ServerProcess.CommitApplicationTransaction(applicationTransactionID);
			return null;
		}
	}

	/// <remarks>
	///	operator RollbackApplicationTransaction(AID : Guid);
	///	Rejects all the changes made during the application transaction.
	/// Once an application transaction has been successfully rolled back, it is an error to attempt any further manipulations in the transaction.
	///	</remarks>
	public class RollbackApplicationTransactionNode : CompleteApplicationTransactionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Guid applicationTransactionID = (Guid)arguments[0];
			program.ServerProcess.RollbackApplicationTransaction(applicationTransactionID);
			return null;
		}
	}
	
	public class TableMapHeader : System.Object
	{
		public TableMapHeader(int sourceTableVarID, int translatedTableVarID, int deletedTableVarID) : base()
		{
			SourceTableVarID = sourceTableVarID;
			TranslatedTableVarID = translatedTableVarID;
			DeletedTableVarID = deletedTableVarID;
		}
		
		public int SourceTableVarID;
		public int TranslatedTableVarID;
		public int DeletedTableVarID;
	}

	public class TableMap : Schema.Object
	{
		public TableMap(string tableVarName) : base(tableVarName) {}
		
		private TableVar _tableVar;
		public TableVar TableVar 
		{ 
			get { return _tableVar; } 
			set { _tableVar = value; }
		}
		
		public Schema.IRowType RowType { get { return TableVar.DataType.RowType; } }
		
		private BitArray _valueFlags;
		public BitArray ValueFlags
		{
			get
			{
				if (_valueFlags == null)
				{
					_valueFlags = new BitArray(RowType.Columns.Count);
					for (int index = 0; index < _valueFlags.Length; index++)
						_valueFlags[index] = true;
				}
				
				return _valueFlags;
			}
		}

		private TableVar _deletedTableVar;
		public TableVar DeletedTableVar 
		{ 
			get { return _deletedTableVar; } 
			set { _deletedTableVar = value; }
		}
		
		private TableVar _sourceTableVar;
		public TableVar SourceTableVar 
		{ 
			get { return _sourceTableVar; } 
			set { _sourceTableVar = value; }
		}
		
		private BaseTableVarNode _retrieveNode;
		public BaseTableVarNode RetrieveNode
		{
			get { return _retrieveNode; }
			set { _retrieveNode = value; }
		}
		
		private PlanNode _hasRowNode;
		public PlanNode HasRowNode
		{
			get { return _hasRowNode; }
			set { _hasRowNode = value; }
		}
		
		private PlanNode _hasDeletedRowNode;
		public PlanNode HasDeletedRowNode
		{
			get { return _hasDeletedRowNode; }
			set { _hasDeletedRowNode = value; }
		}
		
		private BaseTableVarNode _deletedRetrieveNode;
		public BaseTableVarNode DeletedRetrieveNode
		{
			get { return _deletedRetrieveNode; }
			set { _deletedRetrieveNode = value; }
		}
		
		private bool _dropped;
		public bool Dropped
		{
			get { return _dropped; }
			set { _dropped = value; }
		}
	}
	
	public class TableMaps : Schema.Objects<TableMap>
	{
		public TableMap this[TableVar tableVar] { get { return this[tableVar.Name]; } }
	}
	
	public class OperatorMap : Schema.Object
	{
		public OperatorMap(string operatorName, string translatedOperatorName) : base(operatorName)
		{
			_translatedOperatorName = translatedOperatorName;
		}
		
		private string _translatedOperatorName;
		public string TranslatedOperatorName
		{
			get { return _translatedOperatorName; }
			set { _translatedOperatorName = value; }
		}
		
		private Schema.Objects _operators = new Schema.Objects();
		public Schema.Objects Operators { get { return _operators; } }
		
		/// <summary>Returns the translated operator for the given source operator, if it exists. Null otherwise.</summary>
		public Schema.Operator ResolveTranslatedOperator(Schema.Operator sourceOperator)
		{
			foreach (Schema.Operator translatedOperator in _operators)
				if (translatedOperator.Signature.Equals(sourceOperator.Signature))
					return translatedOperator;
			return null;
		}
		
		private bool _dropped;
		public bool Dropped
		{
			get { return _dropped; }
			set { _dropped = value; }
		}
	}
	
	public class OperatorMaps : Schema.Objects<OperatorMap>
	{
		public OperatorMap this[Operator operatorValue] { get { return this[operatorValue.OperatorName]; } }
	}
	
	public class ApplicationTransaction : System.Object
	{
		public ApplicationTransaction(ServerSession session) : base()
		{
			_session = session;
			_device = session.Server.ATDevice;
		}
		
		private Guid _iD = Guid.NewGuid();
		/// <summary>The unique identifier for this application transaction.</summary>
		public Guid ID { get { return _iD; } }
		
		private ServerSession _session;
		/// <summary>The server session managing this application transaction.</summary>
		public ServerSession Session { get { return _session; } }
		
		private ApplicationTransactionDevice _device;
		/// <summary>The application transaction device for the server.</summary>
		public ApplicationTransactionDevice Device { get { return _device; } }
		
		// List of tables in the application transaction, and the deleted table and source mapping for each
		private TableMaps _tableMaps = new TableMaps();
		public TableMaps TableMaps { get { return _tableMaps; } }
		
		// List of operators in the application transaction
		private OperatorMaps _operatorMaps = new OperatorMaps();
		public OperatorMaps OperatorMaps { get { return _operatorMaps; } }
		
		private NativeTables _tables = new NativeTables();
		/// <summary>The storage tables for the table variables within the application transaction.</summary>
		public NativeTables Tables { get { return _tables; } }

		// List of operations taking place in the transaction
		private Operations _operations = new Operations();
		public Operations Operations { get { return _operations; } }
		
		// Dictionary of processes (by ID) participating in the transaction
		private Dictionary<int, ServerProcess> _processes = new Dictionary<int, ServerProcess>();
		public Dictionary<int, ServerProcess> Processes { get { return _processes; } }
		
		// List of event handlers that have fired in this application transaction.  Event handlers in this list will not fire during an ATReplay
		private Schema.EventHandlers _invokedHandlers = new Schema.EventHandlers();
		public Schema.EventHandlers InvokedHandlers { get { return _invokedHandlers; } }
		
		// Returns true if the application transaction specific equivalent of the given event-handler was invoked during this application transaction
		public bool WasInvoked(Schema.EventHandler handler)
		{
			foreach (Schema.EventHandler localHandler in _invokedHandlers)
				if ((localHandler.ATHandlerName != null) && Schema.Object.NamesEqual(localHandler.ATHandlerName, handler.Name))
					return true;
			return false;
		}
		
		// Prepared
		private bool _prepared;
		public bool Prepared 
		{
			get { return _prepared; } 
			set { _prepared = value; } 
		}
		
		// Closed
		private bool _closed;
		public bool Closed 
		{
			get { return _closed; } 
			set { _closed = value; } 
		}
		
		private ServerProcess _populatingProcess;
		private int _populatingCount;
		public void BeginPopulateSource(ServerProcess process)
		{
			lock (this)
			{
				if (_populatingProcess != process)
				{
					if (_populatingProcess != null)
						throw new ApplicationTransactionException(ApplicationTransactionException.Codes.SourceAlreadyPopulating, ID.ToString());
					_populatingProcess = process;
				}
				_populatingCount++;
			}
		}
		
		public void EndPopulateSource()
		{
			lock (this)
			{
				if (IsPopulatingSource)
				{
					_populatingCount--;

					if (_populatingCount == 0)
						_populatingProcess = null;
				}
			}
		}
		
		public bool IsPopulatingSource { get { return _populatingProcess != null; } }
		
		// ATReplayContext
		protected int _aTReplayCount;
		/// <summary>Indicates whether this device is replaying an application transaction.</summary>
		public bool InATReplayContext { get { return _aTReplayCount > 0; } }
		
		public void EnterATReplayContext()
		{
			_aTReplayCount++;
		}
		
		public void ExitATReplayContext()
		{
			_aTReplayCount--;
		}
		
		// IsGlobalContext
		private int _globalContextCount;
		/// <summary>Indicates whether or not the A/T is currently in a global context, preventing enlistment and resolution of A/T objects.</summary>
		/// <remarks>
		/// A global context is used to indicate that the process or plan is currently in a context in which no resolution
		/// should result in a resolve of an A/T object, or enlistment of an existing object into the A/T. Global context 
		/// is checked when:
		///    - Resolving a catalog identifier in an A/T, it prevents resolution of the identifier to the A/T variable
		///    - Determining whether to enlist a non-A/T resolved catalog object, it prevents the enlistment
		///    - Resolving an operator invocation in an A/T, it prevents resolution of the invocation to the A/T operator
		///    - Determining whether to enlist a non-A/T resolved operator, it prevents the enlistment
		///    - Compiling an A/T populate node, it prevents the creation of the node
		/// </remarks>
		public bool IsGlobalContext { get { return _globalContextCount > 0; } }
		
		/// <summary>Pushes a global context to prevent resolution and enlistment into this A/T.</summary>
		/// <remarks>
		/// A global context is used to indicate that the process or plan is currently in a context in which no resolution
		/// should result in a resolve of an A/T object, or enlistment of an existing object into the A/T. Global context 
		/// pushed when:
		///    - compiling a source populate node to prevent resolution of identifiers to the A/T variables
		///    - Preparing an A/T to ensure that no new enlistment into the A/T occurs during the prepare phase
		///    - Rolling back an A/T, to ensure that no new enlistment into the A/T occurs during the rollback
		///    - Compiling table map retrieve nodes to prevent recursion into the enlistment process
		///    - Loading a non-A/T object from the catalog while in an A/T, to prevent A/T enlistment
		///    - Compiling table var keys, orders, and constraints, to prevent A/T enlistment
		///    - Reinferring view references for a non-A/T view while in an A/T, to prevent A/T enlistment
		///    - Recompiling a non-A/T operator while in an A/T, to prevent A/T enlistment
		///    - Ensuring a given node is searchable, to prevent recreation of an existing populate node
		///    - Compiling a browse variant, to prevent A/T enlistment
		///    - Determining capabilities for a cursor node, to prevent recreation of an existing populate node
		///    - Invoking an operator that should not translate into an A/T, to prevent A/T enlistment
		///    - Binding an A/T populate node, to prevent A/T enlistment and recursion
		///    - Compiling the select node for a table node, to prevent A/T enlistment
		///    - Binding the source node for an insert statement, it prevents creation of the populate node
		///    - Ensuring the source node for an update or delete statement is static, it prevents duplication of the populate node
		///    - Compiling the JoinATNode used to insert data during A/T population, to prevent A/T enlistment
		///    - Accessing or maintaining a check table for constraint checks, to prevent A/T enlistment and resolution
		/// </remarks>
		public void PushGlobalContext()
		{
			lock (this)
			{
				_globalContextCount++;
			}
		}
		
		public void PopGlobalContext()
		{
			lock (this)
			{
				_globalContextCount--;
			}
		}
		
		// IsLookup
		private int _lookupCount = 0;
		/// <summary>Indicates whether or not we are currently in a lookup context and should not resolve or enlist A/T variables.</summary>
		/// <remarks>
		/// A lookup context is entered when compiling the right side of a left lookup, and is used to prevent the compiler from entering
		/// an A/T on a table that is not going to be modified by the current A/T.
		/// </remarks>
		public bool IsLookup { get { return _lookupCount > 0; } }
		
		public void PushLookup()
		{
			lock (this)
			{
				_lookupCount++;
			}
		}
		
		public void PopLookup()
		{
			lock (this)
			{
				_lookupCount--;
			}
		}
		
		private void AddTableMap(ServerProcess process, TableMap tableMap)
		{
			int tableMapIndex = _tableMaps.IndexOfName(tableMap.SourceTableVar.Name);
			if (tableMapIndex >= 0)
			{
				if (tableMap.SourceTableVar is Schema.BaseTableVar)
				{
					if (!Tables.Contains(tableMap.TableVar))
						Tables.Add(new NativeTable(process.ValueManager, tableMap.TableVar));
						
					if ((tableMap.DeletedTableVar != null) && !Tables.Contains(tableMap.DeletedTableVar))
						Tables.Add(new NativeTable(process.ValueManager, tableMap.DeletedTableVar));
				}
			}
			else
			{
				_tableMaps.Add(tableMap);
				if (tableMap.SourceTableVar is Schema.BaseTableVar)
				{
					Tables.Add(new NativeTable(process.ValueManager, tableMap.TableVar));
					if (tableMap.DeletedTableVar != null)
						Tables.Add(new NativeTable(process.ValueManager, tableMap.DeletedTableVar));
				}
			}
		}
		
		private void AddDependencies(ServerProcess process, Schema.Object objectValue)
		{
			Schema.Object localObjectValue;
			if (objectValue.HasDependencies())
				for (int index = 0; index < objectValue.Dependencies.Count; index++)
				{
					localObjectValue = objectValue.Dependencies.ResolveObject(process.CatalogDeviceSession, index);
					if (localObjectValue.IsATObject)
					{
						Schema.TableVar tableVar = localObjectValue as Schema.TableVar;
						if (tableVar != null)
						{
							EnsureATTableVarMapped(process, tableVar);
							continue;
						}
						
						Schema.Operator operatorValue = localObjectValue as Schema.Operator;
						if (operatorValue != null)
						{
							EnsureATOperatorMapped(process, operatorValue);
							continue;
						}
					}
				}
		}
		
		private void AddDependencies(ServerProcess process, TableMap tableMap)
		{
			AddDependencies(process, tableMap.TableVar);
			
			// Add dependencies for default expressions, event handlers, and column-level event handlers
			foreach (Schema.TableVarColumn column in tableMap.TableVar.Columns)
			{
				if (column.Default != null)
					AddDependencies(process, column.Default);

				if (column.HasHandlers())
					foreach (Schema.EventHandler handler in column.EventHandlers)
						if (handler.Operator.IsATObject)
							EnsureATOperatorMapped(process, handler.Operator);
			}

			if (tableMap.TableVar.HasHandlers())			
				foreach (Schema.EventHandler handler in tableMap.TableVar.EventHandlers)
					if (handler.Operator.IsATObject)
						EnsureATOperatorMapped(process, handler.Operator);
		}
		
		public void EnsureATTableVarMapped(ServerProcess process, Schema.TableVar aTTableVar)
		{
			lock (process.Catalog)
			{
				lock (Device)
				{
					int index = Device.TableMaps.IndexOfName(aTTableVar.SourceTableName);
					if (index >= 0)
					{
						TableMap tableMap = Device.TableMaps[aTTableVar.SourceTableName];
						if (!_tableMaps.ContainsName(tableMap.SourceTableVar.Name))
						{
							AddTableMap(process, tableMap);
							AddDependencies(process, tableMap);
						}
					}
					else
					{
						Device.AddTableVar(process, (Schema.TableVar)process.CatalogDeviceSession.ResolveName(Schema.Object.EnsureRooted(aTTableVar.SourceTableName), process.ServerSession.NameResolutionPath, new List<string>()));
						AddTableMap(process, Device.TableMaps[aTTableVar.SourceTableName]);
					}
				}
			}
		}
		
		public Schema.TableVar AddTableVar(ServerProcess process, Schema.TableVar sourceTableVar)
		{
			lock (process.Catalog)
			{
				lock (Device)
				{
					int index = Device.TableMaps.IndexOfName(sourceTableVar.Name);
					if (index >= 0)
					{
						TableMap tableMap = Device.TableMaps[index];
						AddTableMap(process, tableMap);
						AddDependencies(process, tableMap);
						return tableMap.TableVar;
					}
					else
					{
						Schema.TableVar result = Device.AddTableVar(process, sourceTableVar);
						AddTableMap(process, Device.TableMaps[sourceTableVar.Name]);
						return result;
					}
				}
			}
		}
		
		public OperatorMap EnsureOperatorMap(ServerProcess process, OperatorMap deviceOperatorMap)
		{
			int index = _operatorMaps.IndexOfName(deviceOperatorMap.Name);
			if (index >= 0)
				return _operatorMaps[index];
			else
			{
				OperatorMap operatorMap = new OperatorMap(deviceOperatorMap.Name, deviceOperatorMap.TranslatedOperatorName);
				_operatorMaps.Add(operatorMap);
				return operatorMap;
			}
		}

		public void EnsureATOperatorMapped(ServerProcess process, Schema.Operator aTOperator)
		{
			OperatorMap deviceOperatorMap = Device.EnsureOperatorMap(process, aTOperator.SourceOperatorName, aTOperator.OperatorName);
			OperatorMap transactionOperatorMap = EnsureOperatorMap(process, deviceOperatorMap);
			if (!transactionOperatorMap.Operators.ContainsName(aTOperator.Name))
			{
				transactionOperatorMap.Operators.Add(aTOperator);
				AddDependencies(process, aTOperator);
			}
		}

		public Schema.Operator AddOperator(ServerProcess process, Schema.Operator sourceOperator)
		{
			lock (process.Catalog)
			{
				lock (Device)
				{
					int index = Device.OperatorMaps.IndexOfName(sourceOperator.OperatorName);
					if (index >= 0)
					{
						OperatorMap deviceOperatorMap = Device.OperatorMaps[index];
						OperatorMap transactionOperatorMap = EnsureOperatorMap(process, Device.OperatorMaps[sourceOperator.OperatorName]);
						Schema.Operator translatedOperator = deviceOperatorMap.ResolveTranslatedOperator(sourceOperator);
						if (translatedOperator != null)
						{
							transactionOperatorMap.Operators.Add(translatedOperator);
							AddDependencies(process, translatedOperator);
						}
						else
						{
							translatedOperator = Device.AddOperator(process, sourceOperator);
							transactionOperatorMap.Operators.Add(translatedOperator);
						}
						
						return translatedOperator;
					}
					else
					{
						Schema.Operator aTOperator = Device.AddOperator(process, sourceOperator);
						OperatorMap operatorMap = EnsureOperatorMap(process, Device.OperatorMaps[sourceOperator.OperatorName]);
						operatorMap.Operators.Add(aTOperator);
						return aTOperator;
					}
				}
			}
		}
	}

	public class SyncedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		private Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

		private readonly object FSyncRoot = new object();
		
		public object SyncRoot
		{
			get { return FSyncRoot; }
		}
		
		public void Add(TKey key, TValue tempValue)
		{
			lock (FSyncRoot)
			{
				_dictionary.Add(key, tempValue);
			}
		}

		public bool ContainsKey(TKey key)
		{
			return _dictionary.ContainsKey(key);
		}

		public ICollection<TKey> Keys
		{
			get
			{
				lock (FSyncRoot)
				{
					return _dictionary.Keys;
				}
			}
		}

		public bool Remove(TKey key)
		{
			lock (FSyncRoot)
			{
				return _dictionary.Remove(key);
			}
		}

		public bool TryGetValue(TKey key, out TValue tempValue)
		{
			lock (FSyncRoot)
			{
				return _dictionary.TryGetValue(key, out tempValue);
			}
		}

		public ICollection<TValue> Values
		{
			get
			{
				lock (FSyncRoot)
				{
					return _dictionary.Values;
				}
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				return _dictionary[key];
			}
			set
			{
				lock (FSyncRoot)
				{
					_dictionary[key] = value;
				}
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			lock (FSyncRoot)
			{
				((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Add(item);
			}
		}

		public void Clear()
		{
			lock (FSyncRoot)
			{
				_dictionary.Clear();
			}
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			lock (FSyncRoot)
			{
				((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
			}
		}

		public int Count
		{
			get
			{
				return _dictionary.Count;
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			lock (FSyncRoot)
			{
				return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Remove(item);
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_dictionary).GetEnumerator();
		}
	}
	
	public class ApplicationTransactions : SyncedDictionary<Guid, ApplicationTransaction>
	{
	}
	
	public class ApplicationTransactionDevice : MemoryDevice
	{
		public ApplicationTransactionDevice(int iD, string name) : base(iD, name)
		{
			IgnoreUnsupported = true;
		}
		
		protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
		{
			return new ApplicationTransactionDeviceSession(this, serverProcess, deviceSessionInfo);
		}
		
		// List of currently active application transactions
		private ApplicationTransactions _applicationTransactions = new ApplicationTransactions();
		public ApplicationTransactions ApplicationTransactions { get { return _applicationTransactions; } }
		
		// List of tables in the device, and the deleted table and source mapping for each
		private TableMaps _tableMaps = new TableMaps();
		public TableMaps TableMaps { get { return _tableMaps; } }
		
		// List of operators in the device
		private OperatorMaps _operatorMaps = new OperatorMaps();
		public OperatorMaps OperatorMaps { get { return _operatorMaps; } }
		
		protected void CopyTableVar(ServerProcess process, Schema.TableVar sourceTableVar, bool isMainTableVar)
		{
			string tableVarName = String.Format(".AT_{0}{1}", sourceTableVar.Name.Replace('.', '_'), isMainTableVar ? String.Empty : "_Deleted");
			Block block = new Block();
			CreateTableVarStatement statement = (CreateTableVarStatement)sourceTableVar.EmitStatement(EmitMode.ForCopy);
			statement.TableVarName = tableVarName;
			if (sourceTableVar is Schema.BaseTableVar)
				((CreateTableStatement)statement).DeviceName = new IdentifierExpression(Name);
			statement.IsSession = false;
			if (statement.MetaData == null)
				statement.MetaData = new MetaData();
			statement.MetaData.Tags.SafeRemove("DAE.GlobalObjectName");
			statement.MetaData.Tags.AddOrUpdate("DAE.SourceTableName", sourceTableVar.Name, true);
			statement.MetaData.Tags.AddOrUpdate("DAE.IsDeletedTable", (!isMainTableVar).ToString(), true);

			block.Statements.Add(statement);

			Plan plan = new Plan(process);
			try
			{
				plan.PushSecurityContext(new SecurityContext(sourceTableVar.Owner));
				try
				{
					plan.PushATCreationContext();
					try
					{
						Program program = new Program(process);
						program.Code = Compiler.Compile(plan, block);
						plan.CheckCompiled();
						program.Execute(null);
					}
					finally
					{
						plan.PopATCreationContext();
					}
				}
				finally
				{
					plan.PopSecurityContext();
				}
			}
			finally
			{
				plan.Dispose();
			}

			if (isMainTableVar)
			{
				try
				{
					TableMap tableMap = TableMaps[sourceTableVar.Name];

					block = new Block();

					foreach (Schema.TableVarColumn column in sourceTableVar.Columns)
					{
						if ((column.Default != null) && !column.Default.IsRemotable)
						{
							AlterTableStatement alterStatement = new AlterTableStatement();
							alterStatement.TableVarName = Schema.Object.EnsureRooted(tableMap.TableVar.Name);
							AlterColumnDefinition definition = new AlterColumnDefinition();
							definition.ColumnName = column.Name;
							definition.Default = column.Default.EmitDefinition(EmitMode.ForCopy);
							((DefaultDefinition)definition.Default).IsGenerated = true;
							alterStatement.AlterColumns.Add(definition);
							block.Statements.Add(alterStatement);
						}
					}
				
					if (sourceTableVar.HasHandlers())
						foreach (Schema.EventHandler handler in sourceTableVar.EventHandlers)
							if (!handler.IsGenerated && handler.ShouldTranslate)
							{
								AttachStatement attachStatement = (AttachStatement)handler.EmitTableVarHandler(sourceTableVar, EmitMode.ForCopy);
								if (attachStatement.MetaData == null)
									attachStatement.MetaData = new MetaData();
								attachStatement.MetaData.Tags.RemoveTag("DAE.ObjectID");
								attachStatement.MetaData.Tags.AddOrUpdate("DAE.ATHandlerName", handler.Name, true);
								((ObjectEventSourceSpecifier)attachStatement.EventSourceSpecifier).ObjectName = Schema.Object.EnsureRooted(tableMap.TableVar.Name);
								if (handler.Operator.ShouldTranslate)
									attachStatement.OperatorName = Schema.Object.EnsureRooted(EnsureOperator(process, handler.Operator).OperatorName);
								attachStatement.IsGenerated = true;
								block.Statements.Add(attachStatement);
							}
					
					foreach (Schema.TableVarColumn column in sourceTableVar.Columns)
						if (column.HasHandlers())
							foreach (Schema.EventHandler handler in column.EventHandlers)
								if (!handler.IsGenerated && handler.ShouldTranslate)
								{
									AttachStatement attachStatement = (AttachStatement)handler.EmitColumnHandler(sourceTableVar, column, EmitMode.ForCopy);
									if (attachStatement.MetaData == null)
										attachStatement.MetaData = new MetaData();
									attachStatement.MetaData.Tags.RemoveTag("DAE.ObjectID");
									attachStatement.MetaData.Tags.AddOrUpdate("DAE.ATHandlerName", handler.Name, true);
									((ColumnEventSourceSpecifier)attachStatement.EventSourceSpecifier).TableVarName = Schema.Object.EnsureRooted(tableMap.TableVar.Name);
									if (handler.Operator.ShouldTranslate)
										attachStatement.OperatorName = Schema.Object.EnsureRooted(EnsureOperator(process, handler.Operator).OperatorName);
									attachStatement.IsGenerated = true;
									block.Statements.Add(attachStatement);
								}

					plan = new Plan(process);
					try
					{
						plan.PushSecurityContext(new SecurityContext(sourceTableVar.Owner));
						try
						{
							plan.EnterTimeStampSafeContext();
							try
							{
								Program program = new Program(process);
								program.Code = Compiler.Compile(plan, block);
								plan.CheckCompiled();
								program.Execute(null);
							}
							finally
							{
								plan.ExitTimeStampSafeContext();
							}
						}
						finally
						{
							plan.PopSecurityContext();
						}
					}
					finally
					{
						plan.Dispose();
					}
				}
				catch (Exception e)
				{
					try
					{
						ReportTableChange(process, sourceTableVar, false);
					}
					catch (Exception ne)
					{
						// Ignore errors here, this is cleanup code
					}
					throw;
				}
			}
		}
		
		public void AddTableMap(ServerProcess process, Schema.TableVar tableVar)
		{
			// Called by the compiler during processing of the create table statement created by CopyTableVar.
			// CreatingTableVarName is the name of the table variable being added to the application transaction.
			// If the table map is already present for this table, then this is the deleted tracking table.
			string sourceTableVarName = MetaData.GetTag(tableVar.MetaData, "DAE.SourceTableName", tableVar.Name);
			int index = _tableMaps.IndexOfName(sourceTableVarName);
			if (index < 0)
			{
				TableMap tableMap = new TableMap(sourceTableVarName);
				tableMap.TableVar = tableVar;
				tableMap.SourceTableVar = (TableVar)process.Catalog[sourceTableVarName];
				tableMap.Library = tableMap.SourceTableVar.Library;
				process.CatalogDeviceSession.AddTableMap(this, tableMap);
			}
			else
			{
				TableMap tableMap = _tableMaps[index];
				tableMap.DeletedTableVar = tableVar;
			}
		}
		
		private void CheckNotParticipating(ServerProcess process, Schema.TableVar tableVar)
		{
			lock (ApplicationTransactions.SyncRoot)
			{
				foreach (ApplicationTransaction transaction in ApplicationTransactions.Values)
					if (transaction.TableMaps.ContainsName(tableVar.Name))
						throw new ApplicationTransactionException(ApplicationTransactionException.Codes.TableVariableParticipating, tableVar.Name);
			}
		}

		public void ReportTableChange(ServerProcess process, Schema.TableVar tableVar)
		{
			ReportTableChange(process, tableVar, true);
		}
		
		public void ReportTableChange(ServerProcess process, Schema.TableVar tableVar, bool checkParticipants)
		{
			// If the table var is a source table var for an A/T table
				// if there are active A/Ts for the table var
					// throw an error
				// otherwise
					// detach any A/T handlers that may be attached to the A/T table var (safely)
					// drop the A/T table vars (safely)
					// remove the table map
					
			if (!process.ServerSession.Server.IsEngine)
			{
				lock (process.Catalog)
				{
					lock (this)
					{
						List<string> objectNames = new List<string>();
						int tableMapIndex = _tableMaps.IndexOfName(tableVar.Name);
						if (tableMapIndex >= 0)
						{
							if (checkParticipants)
							{
								CheckNotParticipating(process, tableVar);
							}
							
							// Drop the table var and deleted table var
							TableMap tableMap = _tableMaps[tableMapIndex];
							if (tableMap.TableVar != null)
							{
								for (int index = 0; index < tableMap.TableVar.EventHandlers.Count; index++)
									objectNames.Add(tableMap.TableVar.EventHandlers[index].Name);
								objectNames.Add(tableMap.TableVar.Name);
							}

							if (tableMap.DeletedTableVar != null)
								objectNames.Add(tableMap.DeletedTableVar.Name);
							
							process.CatalogDeviceSession.RemoveTableMap(this, tableMap);
						}

						if (objectNames.Count > 0)
						{
							string[] objectNameArray = new string[objectNames.Count];
							objectNames.CopyTo(objectNameArray, 0);
							Plan plan = new Plan(process);
							try
							{
								plan.EnterTimeStampSafeContext();
								try
								{
									Program program = new Program(process);
									program.Code =
										Compiler.Compile
										(
											plan,
											process.Catalog.EmitDropStatement
											(
												process.CatalogDeviceSession,
												objectNameArray,
												String.Empty,
												true,
												false,
												true,
												true
											)
										);
									plan.CheckCompiled();
									program.Execute(null);
								}
								finally
								{
									plan.ExitTimeStampSafeContext();
								}
							}
							finally
							{
								plan.Dispose();
							}
						}
					}
				}
			}
		}
		
		public Schema.Operator ResolveSourceOperator(Plan plan, Schema.Operator translatedOperator)
		{
			return Compiler.ResolveOperator(plan, translatedOperator.SourceOperatorName, translatedOperator.Signature, false, false);
		}
		
		public void ReportOperatorChange(ServerProcess process, Schema.Operator operatorValue)
		{
			// If the operator is a source operator for an A/T operator
				// if there are active A/Ts for the operator, or for any A/T table var that may have a handler attached to this operator
					// throw an error
				// otherwise
					// detach the A/T operators from any A/T table vars they may be attached to (safely)
					// drop the A/T operators (safely)
					// remove the operator map
					// report table changes on the A/T table vars
					
			if (!process.ServerSession.Server.IsEngine)
			{
				lock (process.Catalog)
				{
					lock (this)
					{
						List<string> objectNames = new List<string>();
						Schema.Operator aTOperator = null;
						int operatorMapIndex = _operatorMaps.IndexOfName(operatorValue.OperatorName);
						if (operatorMapIndex >= 0)
						{
							OperatorMap operatorMap = _operatorMaps[operatorMapIndex];
							
							aTOperator = operatorMap.ResolveTranslatedOperator(operatorValue);
							if (aTOperator != null)
							{
								// Check for tables with event handlers attached to this operator
								List<int> handlerIDs = process.CatalogDeviceSession.SelectOperatorHandlers(operatorValue.ID);

								for (int index = 0; index < handlerIDs.Count; index++)
								{
									Schema.Object handler = process.CatalogDeviceSession.ResolveCatalogObject(handlerIDs[index]);
									Schema.TableVar tableVar = null;
									if (handler is TableVarEventHandler)
										tableVar = ((TableVarEventHandler)handler).TableVar;
									if (handler is TableVarColumnEventHandler)
										tableVar = ((TableVarColumnEventHandler)handler).TableVarColumn.TableVar;
									
									if (tableVar != null)
									{
										int tableMapIndex = _tableMaps.IndexOfName(tableVar.Name);
										if (tableMapIndex >= 0)
										{
											bool shouldCheckTableVar = false;
											TableMap tableMap = _tableMaps[tableMapIndex];
											foreach (Schema.TableVarEventHandler tableVarEventHandler in tableMap.TableVar.EventHandlers)
												if (tableVarEventHandler.Operator.Name == aTOperator.Name)
													shouldCheckTableVar = true;
											
											foreach (Schema.TableVarColumn tableVarColumn in tableMap.TableVar.Columns)
												foreach (Schema.TableVarColumnEventHandler tableVarColumnEventHandler in tableVarColumn.EventHandlers)
													if (tableVarColumnEventHandler.Operator.Name == aTOperator.Name)
														shouldCheckTableVar = true;
											
											if (shouldCheckTableVar)
												ReportTableChange(process, tableVar);
										}
									}	
								}
								
								lock (ApplicationTransactions.SyncRoot)
								{
									foreach (ApplicationTransaction transaction in ApplicationTransactions.Values)
									{
										int transactionOperatorMapIndex = transaction.OperatorMaps.IndexOfName(operatorValue.OperatorName);
										if (transactionOperatorMapIndex >= 0)
										{
											OperatorMap transactionOperatorMap = transaction.OperatorMaps[transactionOperatorMapIndex];
											if (transactionOperatorMap.Operators.ContainsName(aTOperator.Name))
												throw new ApplicationTransactionException(ApplicationTransactionException.Codes.OperatorParticipating, operatorValue.OperatorName);
										}
									}
								}
								
								// Drop the operator
								process.CatalogDeviceSession.RemoveOperatorMap(operatorMap, aTOperator);
								// BTR 2/1/2007 -> Operator Map should never hurt to leave around, so leave it instead of trying to manage the complexity of making it transactional (any process could add the operator map)
								//if (LOperatorMap.Operators.Count == 0)
								//	FOperatorMaps.RemoveAt(LOperatorMapIndex);
							}
						}
						
						if (aTOperator != null)
						{
							objectNames.Add(aTOperator.Name);
							string[] objectNameArray = new string[objectNames.Count];
							objectNames.CopyTo(objectNameArray, 0);
							Plan plan = new Plan(process);
							try
							{
								plan.EnterTimeStampSafeContext();
								try
								{
									Program program = new Program(process);
									program.Code = Compiler.Compile(plan, process.Catalog.EmitDropStatement(process.CatalogDeviceSession, objectNameArray, String.Empty));
									plan.CheckCompiled();
									program.Execute(null);
								}
								finally
								{
									plan.ExitTimeStampSafeContext();
								}
							}
							finally
							{
								plan.Dispose();
							}
						}
					}
				}
			}
		}
		
		// AddTableVar - Creates a TableMap for the given source table and adds it to the table maps, if it is not already present, then returns the name of the mapped table
		public Schema.TableVar AddTableVar(ServerProcess process, Schema.TableVar sourceTableVar)
		{
			if (sourceTableVar.SourceTableName != null)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.AlreadyApplicationTransactionVariable, sourceTableVar.Name, sourceTableVar.SourceTableName);
				
			string sourceTableName = sourceTableVar.Name;
			TableMap tableMap = null;
			
			// Push an adding table var context
			process.PushAddingTableVar();
			try
			{
				int tableIndex = _tableMaps.IndexOf(sourceTableName);
				if (tableIndex >= 0)
					tableMap = _tableMaps[tableIndex];
				else
				{
					bool saveIsInsert = process.IsInsert;
					process.IsInsert = false;
					try
					{
						CopyTableVar(process, sourceTableVar, true);
						tableMap = _tableMaps[sourceTableName];
						if (sourceTableVar is Schema.BaseTableVar)
						{
							BaseTableVar targetTableVar = (BaseTableVar)tableMap.TableVar;
							try
							{
								CopyTableVar(process, sourceTableVar, false);
								BaseTableVar deletedTableVar = (BaseTableVar)tableMap.DeletedTableVar;
								try
								{
									CompileTableMapRetrieveNodes(process, sourceTableVar, tableMap);
								}
								catch
								{
									Plan plan = new Plan(process);
									try							 
									{
										Program program = new Program(process);
										program.Code = Compiler.Compile(plan, process.Catalog.EmitDropStatement(process.CatalogDeviceSession, new string[] { deletedTableVar.Name }, String.Empty));
										plan.CheckCompiled();
										program.Execute(null);
									}
									finally
									{
										plan.Dispose();
									}
									throw;
								}
							}
							catch
							{
								Plan plan = new Plan(process);
								try							 
								{
									Program program = new Program(process);
									program.Code = Compiler.Compile(plan, process.Catalog.EmitDropStatement(process.CatalogDeviceSession, new string[] { targetTableVar.Name }, String.Empty));
									plan.CheckCompiled();
									program.Execute(null);
								}
								finally
								{
									plan.Dispose();
								}
								throw;
							}
						}
					}
					finally
					{
						process.IsInsert = saveIsInsert;
					}
				}
			}
			finally
			{
				process.PopAddingTableVar();
			}

			return tableMap.TableVar; // return the supporting table variable (generated unique name)
		}

		private void CompileTableMapRetrieveNodes(ServerProcess process, Schema.TableVar sourceTableVar, TableMap tableMap)
		{
			ApplicationTransactions[process.ApplicationTransactionID].PushGlobalContext();
			try
			{
				// RetrieveNode
				using (var plan = new Plan(process))
				{
					plan.PushSecurityContext(new SecurityContext(sourceTableVar.Owner));
					try
					{
						var planNode = Compiler.Compile(plan, new CursorDefinition(new IdentifierExpression(sourceTableVar.Name), CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated, DAE.CursorType.Static));
						plan.CheckCompiled();
						tableMap.RetrieveNode = planNode.ExtractNode<BaseTableVarNode>();
					}
					finally
					{
						plan.PopSecurityContext();
					}
				}

				// DeletedRetrieveNode
				using (var plan = new Plan(process))
				{
					plan.PushSecurityContext(new SecurityContext(sourceTableVar.Owner));
					try
					{
						var planNode = Compiler.Compile(plan, new CursorDefinition(new IdentifierExpression(Object.EnsureRooted(tableMap.DeletedTableVar.Name)), CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated, DAE.CursorType.Static));
						plan.CheckCompiled();
						tableMap.DeletedRetrieveNode = planNode.ExtractNode<BaseTableVarNode>();
					}
					finally
					{
						plan.PopSecurityContext();
					}
				}

				// HasRowNode/HasDeletedRowNode
				using (var plan = new Plan(process))
				{
					plan.PushSecurityContext(new SecurityContext(sourceTableVar.Owner));
					try
					{
						Schema.Key clusteringKey = Compiler.FindClusteringKey(plan, tableMap.TableVar);
						#if !USENAMEDROWVARIABLES
						Schema.RowType oldRowType = new Schema.RowType(ATableMap.TableVar.DataType.Columns, Keywords.Old);
						Schema.RowType oldKeyType = new Schema.RowType(clusteringKey.Columns, Keywords.Old);
						Schema.RowType keyType = new Schema.RowType(clusteringKey.Columns);
						#endif
						plan.EnterRowContext();
						try
						{
							#if USENAMEDROWVARIABLES
							plan.Symbols.Push(new Symbol(Keywords.Old, tableMap.TableVar.DataType.RowType));
							#else
							plan.Symbols.Push(new Symbol(String.Empty, oldRowType));
							#endif
							try
							{
								var deletedRowNodeExpression =
									new UnaryExpression
									(
										Instructions.Exists,
										new RestrictExpression
										(
											new IdentifierExpression(Object.EnsureRooted(tableMap.DeletedTableVar.Name)),
											#if USENAMEDROWVARIABLES
											Compiler.BuildKeyEqualExpression(plan, Keywords.Old, String.Empty, clusteringKey.Columns, clusteringKey.Columns)
											#else
											Compiler.BuildKeyEqualExpression(LPlan, LOldKeyType.Columns, LKeyType.Columns)
											#endif
										)
									);
							
								var planNode = Compiler.Compile(plan, deletedRowNodeExpression);
								plan.CheckCompiled();

								tableMap.HasDeletedRowNode = planNode;

								planNode =
									Compiler.Compile
									(
										plan,
										new BinaryExpression
										(
											new UnaryExpression
											(
												Instructions.Exists,
												new RestrictExpression
												(
													new IdentifierExpression(Object.EnsureRooted(tableMap.TableVar.Name)),
													#if USENAMEDROWVARIABLES
													Compiler.BuildKeyEqualExpression(plan, Keywords.Old, String.Empty, clusteringKey.Columns, clusteringKey.Columns)
													#else
													Compiler.BuildKeyEqualExpression(LPlan, LOldKeyType.Columns, LKeyType.Columns)
													#endif
												)
											),
											Instructions.Or,
											deletedRowNodeExpression
										)
									);

								plan.CheckCompiled();
								tableMap.HasRowNode = planNode;
							}
							finally
							{
								plan.Symbols.Pop();
							}
						}
						finally
						{
							plan.ExitRowContext();
						}
					}
					finally
					{
						plan.PopSecurityContext();
					}
				}
			}
			finally
			{
				ApplicationTransactions[process.ApplicationTransactionID].PopGlobalContext();
			}
		}
		
		public OperatorMap EnsureOperatorMap(ServerProcess process, string operatorName, string translatedOperatorName)
		{
			OperatorMap operatorMap;
			int index = _operatorMaps.IndexOfName(operatorName);
			if (index >= 0)
				operatorMap = _operatorMaps[index];
			else
			{
				if (translatedOperatorName != null)
					operatorMap = new OperatorMap(operatorName, translatedOperatorName);
				else
					operatorMap = new OperatorMap(operatorName, String.Format("AT_{0}", operatorName.Replace('.', '_')));
				_operatorMaps.Add(operatorMap);
			}
			return operatorMap;
		}

		private void RemoveOperatorMap(ServerProcess process, OperatorMap operatorMap)
		{
			_operatorMaps.SafeRemove(operatorMap);
		}
		
		public Schema.Operator AddOperator(ServerProcess process, Schema.Operator operatorValue)
		{
			// Recompile the operator in the application transaction process
			OperatorMap operatorMap = EnsureOperatorMap(process, operatorValue.OperatorName, null);
			try
			{
				Statement sourceStatement = operatorValue.EmitStatement(EmitMode.ForCopy);
				SourceContext sourceContext = null;
				CreateOperatorStatement statement = sourceStatement as CreateOperatorStatement;
				if (statement == null)
				{
					sourceContext = new SourceContext(((SourceStatement)sourceStatement).Source, null);
					statement = (CreateOperatorStatement)new Parser().ParseStatement(sourceContext.Script, null);
				}
				statement.OperatorName = String.Format(".{0}", operatorMap.TranslatedOperatorName);
				statement.IsSession = false;
				if (statement.MetaData == null)
					statement.MetaData = new MetaData();
				statement.MetaData.Tags.SafeRemove("DAE.GlobalObjectName");
				statement.MetaData.Tags.AddOrUpdate("DAE.SourceOperatorName", operatorMap.Name, true);
				statement.MetaData.Tags.AddOrUpdate("DAE.SourceObjectName", operatorValue.Name, true);

				Plan plan = new Plan(process);
				try
				{
					bool saveIsInsert = process.IsInsert;
					process.IsInsert = false;
					try
					{
						if (sourceContext != null)
							plan.PushSourceContext(sourceContext);
						try
						{
							// A loading context is required because the operator text is using the original text
							// so it must be bound with the original resolution path (the owning library).
							plan.PushLoadingContext(new LoadingContext(operatorValue.Owner, operatorValue.Library.Name, false));
							try
							{
								plan.PushSecurityContext(new SecurityContext(operatorValue.Owner));
								try
								{
									plan.PushATCreationContext();
									try
									{
										Program program = new Program(process);
										program.Code = Compiler.Compile(plan, statement);
										plan.CheckCompiled();
										program.Execute(null);
										Schema.Operator localOperatorValue = ((CreateOperatorNode)program.Code).CreateOperator;
										process.CatalogDeviceSession.AddOperatorMap(operatorMap, localOperatorValue);
										return localOperatorValue;
									}
									finally
									{
										plan.PopATCreationContext();
									}
								}
								finally
								{
									plan.PopSecurityContext();
								}
							}
							finally
							{
								plan.PopLoadingContext();
							}
						}
						finally
						{
							if (sourceContext != null)
								plan.PopSourceContext();
						}
					}
					finally
					{
						process.IsInsert = saveIsInsert;
					}
				}
				finally
				{
					plan.Dispose();
				}
			}
			catch (Exception e)
			{
				RemoveOperatorMap(process, operatorMap);
				throw;
			}
		}
		
		public Schema.Operator EnsureOperator(ServerProcess process, Schema.Operator operatorValue)
		{
			using (Plan plan = new Plan(process))
			{
				OperatorBindingContext context = new OperatorBindingContext(null, operatorValue.OperatorName, plan.NameResolutionPath, operatorValue.Signature, false);
				Compiler.ResolveOperator(plan, context);
				Error.AssertFail(context.Operator != null, @"Operator ""{0}"" was not translated into the application transaction as expected");
				return context.Operator;
			}
		}
	}

	public abstract class Operation : System.Object
	{
		public Operation(ApplicationTransaction transaction, TableVar tableVar) : base()
		{
			_transaction = transaction;
			_tableVar = tableVar;
		}
		
		private ApplicationTransaction _transaction;
		public ApplicationTransaction Transaction { get { return _transaction; } }
		
		private TableVar _tableVar;
		public TableVar TableVar { get { return _tableVar; } }
		
		private TableMap _tableMap;
		protected TableMap TableMap
		{
			get
			{
				if (_tableMap == null)
				{
					_tableMap = _transaction.Device.TableMaps[_tableVar.SourceTableName];
					if (_tableMap == null)
						throw new ApplicationTransactionException(ApplicationTransactionException.Codes.TableMapNotFound, _tableVar.Name);
				}
				return _tableMap;
			}
		}
		
		protected bool _applied = false;
		
		public void ResetApplied()
		{
			_applied = false;
		}
		
		public abstract void Apply(Program program);
		
		public abstract void Undo(Program program);
		
		public abstract void Dispose(IValueManager manager);
	}
	
	#if USETYPEDLIST
	public class Operations : TypedList
	{
		public Operations() : base(typeof(Operation)){}
		
		public new Operation this[int AIndex]
		{
			get { return (Operation)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	#else
	public class Operations : BaseList<Operation>
	{
	#endif
	}
	
	public class InsertOperation : Operation
	{
		public InsertOperation(ApplicationTransaction transaction, TableVar tableVar, NativeRow row, BitArray valueFlags) : base(transaction, tableVar)
		{
			_row = row;
			_valueFlags = valueFlags;
		}
		
		private NativeRow _row;
		public NativeRow Row { get { return _row; } }

		private BitArray _valueFlags;
		public BitArray ValueFlags { get { return _valueFlags; } }
		
		public override void Apply(Program program)
		{
			if (!_applied)
			{
				Row row = new Row(program.ValueManager, TableMap.RowType, _row);
				try
				{
					TableMap.RetrieveNode.Insert(program, null, row, _valueFlags, false);
				}
				finally
				{
					row.Dispose();
				}
				_applied = true;
			}
		}
		
		public override void Undo(Program program)
		{
			if (_applied)
			{
				Row row = new Row(program.ValueManager, TableMap.RowType, _row);
				try
				{
					TableMap.RetrieveNode.Delete(program, row, false, false);
				}
				finally
				{
					row.Dispose();
				}
				_applied = false;
			}
		}

		public override void Dispose(IValueManager manager)
		{
			DataValue.DisposeNative(manager, TableMap.RowType, _row);
		}
	}
	
	public class UpdateOperation : Operation
	{
		public UpdateOperation(ApplicationTransaction transaction, TableVar tableVar, NativeRow oldRow, NativeRow newRow, BitArray valueFlags) : base(transaction, tableVar)
		{
			_oldRow = oldRow;
			_newRow = newRow;
			_valueFlags = valueFlags;
		}

		private NativeRow _oldRow;
		public NativeRow OldRow { get { return _oldRow; } }
		
		private NativeRow _newRow; 
		public NativeRow NewRow { get { return _newRow; } }

		private BitArray _valueFlags;
		public BitArray ValueFlags { get { return _valueFlags; } }

		public override void Apply(Program program)
		{
			if (!_applied)
			{
				Row oldRow = new Row(program.ValueManager, TableMap.RowType, _oldRow);
				try
				{
					Row newRow = new Row(program.ValueManager, TableMap.RowType, _newRow);
					try
					{
						TableMap.RetrieveNode.Update(program, oldRow, newRow, _valueFlags, true, false);
					}
					finally
					{
						newRow.Dispose();
					}
				}
				finally
				{
					oldRow.Dispose();
				}
				_applied = true;
			}
		}
		
		public override void Undo(Program program)
		{
			if (_applied)
			{
				Row oldRow = new Row(program.ValueManager, TableMap.RowType, _oldRow);
				try
				{
					Row newRow = new Row(program.ValueManager, TableMap.RowType, _newRow);
					try
					{
						TableMap.RetrieveNode.Update(program, newRow, oldRow, _valueFlags, false, true);
					}
					finally
					{
						newRow.Dispose();
					}
				}
				finally
				{
					oldRow.Dispose();
				}
				_applied = false;
			}
		}

		public override void Dispose(IValueManager manager)
		{
			DataValue.DisposeNative(manager, TableMap.RowType, _oldRow);
			DataValue.DisposeNative(manager, TableMap.RowType, _newRow);
		}
	}
	
	public class DeleteOperation : Operation
	{
		public DeleteOperation(ApplicationTransaction transaction, TableVar tableVar, NativeRow row) : base(transaction, tableVar)
		{
			_row = row;
		}
		
		private NativeRow _row;
		public NativeRow Row { get { return _row; } }
		
		public override void Apply(Program program)
		{
			if (!_applied)
			{
				Row row = new Row(program.ValueManager, TableMap.RowType, _row);
				try
				{
					TableMap.RetrieveNode.Delete(program, row, true, false);
				}
				finally
				{
					row.Dispose();
				}
				_applied = true;
			}
		}
		
		public override void Undo(Program program)
		{
			if (_applied)
			{
				Row row = new Row(program.ValueManager, TableMap.RowType, _row);
				try
				{
					TableMap.RetrieveNode.Insert(program, null, row, TableMap.ValueFlags, true);
				}
				finally
				{
					row.Dispose();
				}
				_applied = false;
			}
		}

		public override void Dispose(IValueManager manager)
		{
			DataValue.DisposeNative(manager, TableMap.RowType, _row);
		}
	}

	public class ApplicationTransactionDeviceTransaction : System.Object
	{
		public ApplicationTransactionDeviceTransaction(IsolationLevel isolationLevel) : base()
		{
			_isolationLevel = isolationLevel;
		}
		
		private IsolationLevel _isolationLevel;
		public IsolationLevel IsolationLevel { get { return _isolationLevel; } }
		
		private Operations _operations = new Operations();
		public Operations Operations { get { return _operations; } }
		
		private Operations _appliedOperations = new Operations();
		public Operations AppliedOperations { get { return _appliedOperations; } }
		
		// Committed nested transactions which must be rolled back if this transaction rolls back
		private ApplicationTransactionDeviceTransactions _transactions;
		public ApplicationTransactionDeviceTransactions Transactions 
		{ 
			get 
			{
				if (_transactions == null)
					_transactions = new ApplicationTransactionDeviceTransactions();
				return _transactions; 
			} 
		}
	}
	
	#if USETYPEDLIST
	public class ApplicationTransactionDeviceTransactions : TypedList
	{
		public ApplicationTransactionDeviceTransactions() : base(typeof(ApplicationTransactionDeviceTransaction)){}
		
		public new ApplicationTransactionDeviceTransaction this[int AIndex]
		{
			get { return (ApplicationTransactionDeviceTransaction)base[AIndex]; }
			set { base[AIndex] = value; }
		}

	#else
	public class ApplicationTransactionDeviceTransactions : BaseList<ApplicationTransactionDeviceTransaction>
	{
	#endif
		public void BeginTransaction(IsolationLevel isolationLevel)
		{
			Add(new ApplicationTransactionDeviceTransaction(isolationLevel));
		}
		
		// ASuccess indicates whether the transaction ended successfully
		public void EndTransaction(bool success)
		{
			if (success && (Count > 1))
			{
				ApplicationTransactionDeviceTransaction transaction = this[Count - 1];
				this[Count - 2].Transactions.Add(transaction);
				RemoveAt(Count - 1);
			}
			else
				RemoveAt(Count - 1);
		}
		
		public ApplicationTransactionDeviceTransaction CurrentTransaction()
		{
			return this[Count - 1];
		}
	}
	
	public class ApplicationTransactionDeviceSession : MemoryDeviceSession
	{
		protected internal ApplicationTransactionDeviceSession
		(
			Schema.Device device, 
			ServerProcess serverProcess, 
			DeviceSessionInfo deviceSessionInfo
		) : base(device, serverProcess, deviceSessionInfo) {}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				EnsureTransactionsRolledback();
			}
			finally
			{
				_transaction = null;
				base.Dispose(disposing);
			}
		}
		
		private ApplicationTransactionDeviceTransactions _transactions = new ApplicationTransactionDeviceTransactions();
		public new ApplicationTransactionDeviceTransactions Transactions { get { return _transactions; } }
		
		public new ApplicationTransactionDevice Device { get { return (ApplicationTransactionDevice)base.Device; } }
		
		private ApplicationTransaction _transaction;
		public ApplicationTransaction Transaction 
		{ 
			get 
			{ 
				if (_transaction == null)
					Device.ApplicationTransactions.TryGetValue(ServerProcess.ApplicationTransactionID, out _transaction);
				return _transaction;
			} 
		}
		
		protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
		{
			_transactions.BeginTransaction(isolationLevel);
		}

		protected override void InternalPrepareTransaction()
		{
			// do nothing, vote yes
		}
		
		protected override void InternalCommitTransaction()
		{
			_transactions.EndTransaction(true);
		}
		
		protected void InternalRollbackTransaction(ApplicationTransactionDeviceTransaction transaction)
		{
			Exception exception = null;
			int operationIndex;
			foreach (Operation operation in transaction.Operations)
			{
				try
				{
					operationIndex = Transaction.Operations.IndexOf(operation);
					if (operationIndex >= 0)
						Transaction.Operations.RemoveAt(operationIndex);
					operation.Dispose(ServerProcess.ValueManager);
				}
				catch (Exception E)
				{
					exception = E;
					ServerProcess.ServerSession.Server.LogError(E);
				}
			}
			
			foreach (Operation operation in transaction.AppliedOperations)
				operation.ResetApplied();
			
			foreach (ApplicationTransactionDeviceTransaction localTransaction in transaction.Transactions)
			{
				try
				{
					InternalRollbackTransaction(localTransaction);
				}
				catch (Exception E)
				{
					exception = E;
					ServerProcess.ServerSession.Server.LogError(E);
				}
			}
			
			if (exception != null)
				throw exception;
		}

		protected override void InternalRollbackTransaction()
		{
			try
			{
				InternalRollbackTransaction(_transactions.CurrentTransaction());
			}
			finally
			{
				_transactions.EndTransaction(false);
			}
		}

		private TableMap GetTableMap(TableVar tableVar)
		{
			TableMap tableMap = Device.TableMaps[tableVar.SourceTableName];
			if (tableMap == null)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.TableMapNotFound, tableVar.Name);
			return tableMap;
		}
		
		public override NativeTables GetTables(Schema.TableVarScope scope) { return Transaction.Tables; }
		
		protected override object InternalExecute(Program program, PlanNode planNode)
		{
			if (planNode is CreateTableVarBaseNode)
			{
				return null; // Actual storage will be allocated by the application transaction
			}
			else if (planNode is DropTableNode)
			{
				return null; // Actual storage will be deallocated by the application transaction
			}
			else
				return base.InternalExecute(program, planNode);
		}

		protected override void InternalInsertRow(Program program, TableVar tableVar, IRow row, BitArray valueFlags)
		{
			InsertOperation operation = null;
			if (Transaction.IsPopulatingSource)
			{
				TableMap tableMap = GetTableMap(tableVar);

				// Don't insert a row if it is in the DeletedTableVar or the TableVar of the TableMap for this TableVar
				// if not(exists(DeletedTableVar where ARow) or exists(TableVar where ARow))
				Row localRow = new Row(program.ValueManager, tableVar.DataType.RowType, (NativeRow)row.AsNative);
				try
				{
					program.Stack.Push(localRow);
					try
					{
						if (!(bool)tableMap.HasRowNode.Execute(program))
							base.InternalInsertRow(program, tableVar, row, valueFlags);
					}
					finally
					{
						program.Stack.Pop();
					}
				}
				finally
				{
					localRow.Dispose();
				}
			}
			else
			{
				if (Transaction.Closed)
					throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ApplicationTransactionClosed, Transaction.ID.ToString());

				base.InternalInsertRow(program, tableVar, row, valueFlags);
				Transaction.Prepared = false;

				if (!InTransaction || !ServerProcess.CurrentTransaction.InRollback)
				{
					// If this is the deleted table var for a table map, do not log the operation as part of the application transaction
					if (!tableVar.IsDeletedTable)
					{
						operation = new InsertOperation(Transaction, tableVar, (NativeRow)row.CopyNative(), valueFlags);
						Transaction.Operations.Add(operation);
					}
				}
			}

			if (InTransaction && !ServerProcess.NonLogged && (operation != null) && !ServerProcess.CurrentTransaction.InRollback)
				_transactions.CurrentTransaction().Operations.Add(operation);
		}
		
		protected override void InternalUpdateRow(Program program, TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			if (Transaction.Closed)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ApplicationTransactionClosed);

			base.InternalUpdateRow(program, tableVar, oldRow, newRow, valueFlags);
			Transaction.Prepared = false;

			if (!InTransaction || !ServerProcess.CurrentTransaction.InRollback)
			{			
				UpdateOperation operation = new UpdateOperation(Transaction, tableVar, (NativeRow)oldRow.CopyNative(), (NativeRow)newRow.CopyNative(), valueFlags);
				Transaction.Operations.Add(operation);
				if (InTransaction && !ServerProcess.NonLogged)
					_transactions.CurrentTransaction().Operations.Add(operation);

				LogDeletedRow(program, tableVar, oldRow);
			}
		}
		
		protected void LogDeletedRow(Program program, TableVar tableVar, IRow row)
		{
			TableMap tableMap = GetTableMap(tableVar);
			Row localRow = new Row(program.ValueManager, tableVar.DataType.RowType, (NativeRow)row.AsNative);
			try
			{
				program.Stack.Push(localRow);
				try
				{
					if (!(bool)tableMap.HasDeletedRowNode.Execute(program))
						tableMap.DeletedRetrieveNode.Insert(program, null, row, null, true);
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			finally
			{
				localRow.Dispose();
			}
		}
		
		protected override void InternalDeleteRow(Program program, TableVar tableVar, IRow row)
		{
			if (Transaction.Closed)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ApplicationTransactionClosed, Transaction.ID);

			base.InternalDeleteRow(program, tableVar, row);
			Transaction.Prepared = false;
			
			if (!InTransaction || !ServerProcess.CurrentTransaction.InRollback)
			{
				DeleteOperation operation = new DeleteOperation(Transaction, tableVar, (NativeRow)row.CopyNative());
				Transaction.Operations.Add(operation);
				if (InTransaction && !ServerProcess.NonLogged)
					_transactions.CurrentTransaction().Operations.Add(operation);

				LogDeletedRow(program, tableVar, row);
			}
		}
	}
}

