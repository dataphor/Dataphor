/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Alphora.Dataphor.DAE.Device.ApplicationTransaction
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	public sealed class ApplicationTransactionUtility : System.Object
	{
		public static string NameFromID(Guid AID)
		{
			return String.Format("AT_{0}", AID.ToString().Replace("-", "_"));
		}
		
		/// <summary>Gets the application transaction and acquires a lock on it. The caller is responsible for releasing the lock.</summary>
		public static ApplicationTransaction GetTransaction(ServerProcess AProcess, Guid AID)
		{
			ApplicationTransaction LTransaction = AProcess.ServerSession.Server.ATDevice.ApplicationTransactions[AID];
			if (LTransaction == null)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.InvalidApplicationTransactionID, AID);
				
			Monitor.Enter(LTransaction);
			return LTransaction;
		}

		private static void Cleanup(ServerProcess AProcess, ApplicationTransaction ATransaction)
		{
			Exception LException = null;
			foreach (Operation LOperation in ATransaction.Operations)
				try
				{
					LOperation.Dispose(AProcess.ValueManager);
				}
				catch (Exception E)
				{
					LException = E;
				}
				
			foreach (TableMap LTableMap in ATransaction.TableMaps)
			{
				try
				{
					if (LTableMap.TableVar is Schema.BaseTableVar)
						ATransaction.Tables[LTableMap.TableVar].Drop(AProcess.ValueManager);
				}
				catch (Exception E)
				{
					LException = E;
				}
				try
				{
					if (LTableMap.DeletedTableVar is Schema.BaseTableVar)
						ATransaction.Tables[LTableMap.DeletedTableVar].Drop(AProcess.ValueManager);
				}
				catch (Exception E)
				{
					LException = E;
				}
			}
			
			ServerProcess[] LProcesses = new ServerProcess[ATransaction.Processes.Count];
			int LCounter = 0;
			foreach (ServerProcess LProcess in ATransaction.Processes.Values)
			{
				LProcesses[LCounter] = LProcess;
				LCounter++;
			}
			
			for (int LIndex = 0; LIndex < LProcesses.Length; LIndex++)
				try
				{
					LProcesses[LIndex].LeaveApplicationTransaction();
				}
				catch (Exception E)
				{
					LException = E;
				}
		}

		public static Guid BeginApplicationTransaction(ServerProcess AProcess)
		{
			ApplicationTransaction LTransaction = new ApplicationTransaction(AProcess.ServerSession);
			LTransaction.Session.ApplicationTransactions.Add(LTransaction.ID, LTransaction);
			AProcess.ServerSession.Server.ATDevice.ApplicationTransactions.Add(LTransaction.ID, LTransaction);
			return LTransaction.ID;
		}
		
		private static void EndApplicationTransaction(ServerProcess AProcess, Guid AID)
		{
			ApplicationTransaction LTransaction = GetTransaction(AProcess, AID);
			try
			{
				try
				{
					try
					{
						if (!LTransaction.Closed)
							RollbackApplicationTransaction(AProcess, AID);
					}
					finally
					{
						Cleanup(AProcess, LTransaction);
					}
				}
				finally
				{
					LTransaction.Session.ApplicationTransactions.Remove(AID);
					LTransaction.Device.ApplicationTransactions.Remove(AID);
				}
			}
			finally
			{
				Monitor.Exit(LTransaction);
			}
		}
		
		public static void JoinApplicationTransaction(ServerProcess AProcess, Guid AID)
		{
			if (AProcess.ApplicationTransactionID != Guid.Empty)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ProcessAlreadyParticipating);
			
			ApplicationTransaction LTransaction = GetTransaction(AProcess, AID);
			try
			{
				if (LTransaction.Closed)
					throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ApplicationTransactionClosed);
				LTransaction.Processes.Add(AProcess.ProcessID, AProcess);
			}
			finally
			{
				Monitor.Exit(LTransaction);
			}
		}
		
		public static void LeaveApplicationTransaction(ServerProcess AProcess)
		{
			if (AProcess.ApplicationTransactionID != Guid.Empty)
			{
				ApplicationTransaction LTransaction = GetTransaction(AProcess, AProcess.ApplicationTransactionID);
				try
				{
					LTransaction.Processes.Remove(AProcess.ProcessID);
					AProcess.DeviceDisconnect(LTransaction.Device); // Disconnect the session to ensure that the saved pointer to this AT is cleared
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
		}
		
		public static void SetExplicitBind(PlanNode ANode)
		{
			if ((ANode is TableVarNode) && (((TableVarNode)ANode).TableVar.IsATObject))
				((TableVarNode)ANode).ExplicitBind = true;
				
			for (int LIndex = 0; LIndex < ANode.NodeCount; LIndex++)
				SetExplicitBind(ANode.Nodes[LIndex]);
		}
		
		public static TableNode PrepareJoinExpression(Plan APlan, TableNode ASourceNode, out TableNode APopulateNode)
		{
			ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
			try
			{
				LTransaction.PushGlobalContext();
				try
				{
					APlan.PushATCreationContext();
					try
					{
						APopulateNode = (TableNode)Compiler.CompileExpression(APlan, (Expression)ASourceNode.EmitStatement(EmitMode.ForCopy));
					}
					finally
					{
						APlan.PopATCreationContext();
					}
				}
				finally
				{
					LTransaction.PopGlobalContext();
				}

				SetExplicitBind(ASourceNode);
				return ASourceNode;
			}
			finally
			{
				Monitor.Exit(LTransaction);
			}
		}
		
		public static void JoinExpression(Program AProgram, TableNode APopulateNode, TableNode ATranslatedNode)
		{
			ApplicationTransaction LTransaction = AProgram.ServerProcess.GetApplicationTransaction();
			try
			{
				LTransaction.BeginPopulateSource(AProgram.ServerProcess);
				try
				{
					using (Table LTable = (Table)APopulateNode.Execute(AProgram))
					{
						Row LRow = new Row(AProgram.ValueManager, LTable.DataType.RowType);
						try
						{
							while (LTable.Next())
							{
								LTable.Select(LRow);
								ATranslatedNode.JoinApplicationTransaction(AProgram, LRow);
							}
						}
						finally
						{
							LRow.Dispose();
						}
					}
				}
				finally
				{
					LTransaction.EndPopulateSource();
				}
			}
			finally
			{
				Monitor.Exit(LTransaction);
			}
		}
		
		public static void PrepareApplicationTransaction(ServerProcess AProcess, Guid AID)
		{
			Program LProgram = new Program(AProcess);
			LProgram.Start(null);
			try
			{
				PrepareApplicationTransaction(LProgram, AID);
			}
			finally
			{
				LProgram.Stop(null);
			}
		}
		
		public static void PrepareApplicationTransaction(Program AProgram, Guid AID)
		{
			Guid LSaveATID = AProgram.ServerProcess.ApplicationTransactionID;
			if (AProgram.ServerProcess.ApplicationTransactionID != AID)
				AProgram.ServerProcess.JoinApplicationTransaction(AID, true);
			try
			{
				ApplicationTransaction LTransaction = GetTransaction(AProgram.ServerProcess, AID);
				try
				{
					LTransaction.PushGlobalContext();
					try
					{
						ApplicationTransactionDeviceSession LSession = (ApplicationTransactionDeviceSession)AProgram.DeviceConnect(LTransaction.Device);
						ApplicationTransactionDeviceTransaction LDeviceTransaction = null;
						if (LSession.InTransaction)
							LDeviceTransaction = LSession.Transactions.CurrentTransaction();
						if (!LTransaction.Prepared)
						{
							LTransaction.EnterATReplayContext();
							try
							{
								// This code relies on the assumption that operations will only ever be added to the end of the list of operations
								Operation LOperation;
								for (int LIndex = 0; LIndex < LTransaction.Operations.Count; LIndex++)
								{
									LOperation = LTransaction.Operations[LIndex];
									LOperation.Apply(AProgram);
									LDeviceTransaction.AppliedOperations.Add(LOperation);
								}
								LTransaction.Prepared = true;
							}
							finally
							{
								LTransaction.ExitATReplayContext();
							}
						}
					}
					finally
					{
						LTransaction.PopGlobalContext();
					}
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
			finally
			{
				if (AProgram.ServerProcess.ApplicationTransactionID != LSaveATID)
					AProgram.ServerProcess.LeaveApplicationTransaction();
			}
		}
		
		public static void CommitApplicationTransaction(ServerProcess AProcess, Guid AID)
		{
			Program LProgram = new Program(AProcess);
			LProgram.Start(null);
			try
			{
				CommitApplicationTransaction(LProgram, AID);
			}
			finally
			{
				LProgram.Stop(null);
			}
		}
		
		public static void CommitApplicationTransaction(Program AProgram, Guid AID)
		{
			ApplicationTransaction LTransaction = GetTransaction(AProgram.ServerProcess, AID);
			try
			{
				if (!LTransaction.Prepared)
					PrepareApplicationTransaction(AProgram, AID);
					
				LTransaction.Closed = true;
				
				EndApplicationTransaction(AProgram.ServerProcess, AID);
			}
			finally
			{
				Monitor.Exit(LTransaction);
			}
		}
		
		public static void RollbackApplicationTransaction(ServerProcess AProcess, Guid AID)
		{
			Program LProgram = new Program(AProcess);
			LProgram.Start(null);
			try
			{
				RollbackApplicationTransaction(LProgram, AID);
			}
			finally
			{
				LProgram.Stop(null);
			}
		}
		
		public static void RollbackApplicationTransaction(Program AProgram, Guid AID)
		{
			ApplicationTransaction LTransaction = GetTransaction(AProgram.ServerProcess, AID);
			try
			{
				Exception LException = null;

				if (LTransaction.Prepared)
				{
					LTransaction.PushGlobalContext();
					try
					{
						for (int LIndex = LTransaction.Operations.Count - 1; LIndex >= 0; LIndex--)
						{
							try
							{
								LTransaction.Operations[LIndex].Undo(AProgram);
							}
							catch (Exception E)
							{
								LException = E;
								AProgram.ServerProcess.ServerSession.Server.LogError(E);
							}
						}
						
						LTransaction.Prepared = false;

					}
					finally
					{
						LTransaction.PopGlobalContext();
					}
				}
				
				LTransaction.Closed = true;

				EndApplicationTransaction(AProgram.ServerProcess, AID);

				if (LException != null)
					throw LException;
			}
			finally
			{
				Monitor.Exit(LTransaction);
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments.Length > 0)
				return AProgram.ServerProcess.BeginApplicationTransaction((bool)AArguments[0], (bool)AArguments[1]);
			else
				return AProgram.ServerProcess.BeginApplicationTransaction(false, false);
		}
	}
	
	/// <remarks>
	///	operator JoinApplicationTransaction(AID : Guid, AIsInsert : Boolean);
	///	Joins this process to the given application transaction.
	///	</remarks>
	public class JoinApplicationTransactionNode : ApplicationTransactionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			Guid LApplicationTransactionID = (Guid)AArguments[0];
			bool LIsInsert = (bool)AArguments[1];
			AProgram.ServerProcess.JoinApplicationTransaction(LApplicationTransactionID, LIsInsert);
			return null;
		}
	}
	
	/// <remarks>
	///	operator LeaveApplicationTransaction(): String;
	///	Leaves the application transaction this process is participating in.
	///	</remarks>
	public class LeaveApplicationTransactionNode : ApplicationTransactionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.LeaveApplicationTransaction();
			return null;
		}
	}

	public class PrepareApplicationTransactionNode : ApplicationTransactionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.PrepareApplicationTransaction((Guid)AArguments[0]);
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			Guid LApplicationTransactionID = (Guid)AArguments[0];
			AProgram.ServerProcess.CommitApplicationTransaction(LApplicationTransactionID);
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			Guid LApplicationTransactionID = (Guid)AArguments[0];
			AProgram.ServerProcess.RollbackApplicationTransaction(LApplicationTransactionID);
			return null;
		}
	}
	
	public class TableMapHeader : System.Object
	{
		public TableMapHeader(int ASourceTableVarID, int ATranslatedTableVarID, int ADeletedTableVarID) : base()
		{
			SourceTableVarID = ASourceTableVarID;
			TranslatedTableVarID = ATranslatedTableVarID;
			DeletedTableVarID = ADeletedTableVarID;
		}
		
		public int SourceTableVarID;
		public int TranslatedTableVarID;
		public int DeletedTableVarID;
	}

	public class TableMap : Schema.Object
	{
		public TableMap(string ATableVarName) : base(ATableVarName) {}
		
		private TableVar FTableVar;
		public TableVar TableVar 
		{ 
			get { return FTableVar; } 
			set { FTableVar = value; }
		}
		
		public Schema.IRowType RowType { get { return TableVar.DataType.RowType; } }
		
		private BitArray FValueFlags;
		public BitArray ValueFlags
		{
			get
			{
				if (FValueFlags == null)
				{
					FValueFlags = new BitArray(RowType.Columns.Count);
					for (int LIndex = 0; LIndex < FValueFlags.Length; LIndex++)
						FValueFlags[LIndex] = true;
				}
				
				return FValueFlags;
			}
		}

		private TableVar FDeletedTableVar;
		public TableVar DeletedTableVar 
		{ 
			get { return FDeletedTableVar; } 
			set { FDeletedTableVar = value; }
		}
		
		private TableVar FSourceTableVar;
		public TableVar SourceTableVar 
		{ 
			get { return FSourceTableVar; } 
			set { FSourceTableVar = value; }
		}
		
		private BaseTableVarNode FRetrieveNode;
		public BaseTableVarNode RetrieveNode
		{
			get { return FRetrieveNode; }
			set { FRetrieveNode = value; }
		}
		
		private PlanNode FHasRowNode;
		public PlanNode HasRowNode
		{
			get { return FHasRowNode; }
			set { FHasRowNode = value; }
		}
		
		private PlanNode FHasDeletedRowNode;
		public PlanNode HasDeletedRowNode
		{
			get { return FHasDeletedRowNode; }
			set { FHasDeletedRowNode = value; }
		}
		
		private BaseTableVarNode FDeletedRetrieveNode;
		public BaseTableVarNode DeletedRetrieveNode
		{
			get { return FDeletedRetrieveNode; }
			set { FDeletedRetrieveNode = value; }
		}
		
		private bool FDropped;
		public bool Dropped
		{
			get { return FDropped; }
			set { FDropped = value; }
		}
	}
	
	public class TableMaps : Schema.Objects
	{
		public TableMap this[TableVar ATableVar] { get { return this[ATableVar.Name]; } }
		
		public new TableMap this[int AIndex] { get { return (TableMap)base[AIndex]; } }
		
		public new TableMap this[string ATableName] { get { return (TableMap)base[ATableName]; } }
	}
	
	public class OperatorMap : Schema.Object
	{
		public OperatorMap(string AOperatorName, string ATranslatedOperatorName) : base(AOperatorName)
		{
			FTranslatedOperatorName = ATranslatedOperatorName;
		}
		
		private string FTranslatedOperatorName;
		public string TranslatedOperatorName
		{
			get { return FTranslatedOperatorName; }
			set { FTranslatedOperatorName = value; }
		}
		
		private Schema.Objects FOperators = new Schema.Objects();
		public Schema.Objects Operators { get { return FOperators; } }
		
		/// <summary>Returns the translated operator for the given source operator, if it exists. Null otherwise.</summary>
		public Schema.Operator ResolveTranslatedOperator(Schema.Operator ASourceOperator)
		{
			foreach (Schema.Operator LTranslatedOperator in FOperators)
				if (LTranslatedOperator.Signature.Equals(ASourceOperator.Signature))
					return LTranslatedOperator;
			return null;
		}
		
		private bool FDropped;
		public bool Dropped
		{
			get { return FDropped; }
			set { FDropped = value; }
		}
	}
	
	public class OperatorMaps : Schema.Objects
	{
		public OperatorMap this[Operator AOperator] { get { return this[AOperator.OperatorName]; } }
		
		public new OperatorMap this[int AIndex] { get { return (OperatorMap)base[AIndex]; } }
		
		public new OperatorMap this[string AOperatorName] { get { return (OperatorMap)base[AOperatorName]; } }
	}
	
	public class ApplicationTransaction : System.Object
	{
		public ApplicationTransaction(ServerSession ASession) : base()
		{
			FSession = ASession;
			FDevice = ASession.Server.ATDevice;
		}
		
		private Guid FID = Guid.NewGuid();
		/// <summary>The unique identifier for this application transaction.</summary>
		public Guid ID { get { return FID; } }
		
		private ServerSession FSession;
		/// <summary>The server session managing this application transaction.</summary>
		public ServerSession Session { get { return FSession; } }
		
		private ApplicationTransactionDevice FDevice;
		/// <summary>The application transaction device for the server.</summary>
		public ApplicationTransactionDevice Device { get { return FDevice; } }
		
		// List of tables in the application transaction, and the deleted table and source mapping for each
		private TableMaps FTableMaps = new TableMaps();
		public TableMaps TableMaps { get { return FTableMaps; } }
		
		// List of operators in the application transaction
		private OperatorMaps FOperatorMaps = new OperatorMaps();
		public OperatorMaps OperatorMaps { get { return FOperatorMaps; } }
		
		private NativeTables FTables = new NativeTables();
		/// <summary>The storage tables for the table variables within the application transaction.</summary>
		public NativeTables Tables { get { return FTables; } }

		// List of operations taking place in the transaction
		private Operations FOperations = new Operations();
		public Operations Operations { get { return FOperations; } }
		
		// List of processes participating in the transaction
		private Hashtable FProcesses = new Hashtable();
		public Hashtable Processes { get { return FProcesses; } }
		
		// List of event handlers that have fired in this application transaction.  Event handlers in this list will not fire during an ATReplay
		private Schema.EventHandlers FInvokedHandlers = new Schema.EventHandlers();
		public Schema.EventHandlers InvokedHandlers { get { return FInvokedHandlers; } }
		
		// Returns true if the application transaction specific equivalent of the given event-handler was invoked during this application transaction
		public bool WasInvoked(Schema.EventHandler AHandler)
		{
			foreach (Schema.EventHandler LHandler in FInvokedHandlers)
				if ((LHandler.ATHandlerName != null) && Schema.Object.NamesEqual(LHandler.ATHandlerName, AHandler.Name))
					return true;
			return false;
		}
		
		// Prepared
		private bool FPrepared;
		public bool Prepared 
		{
			get { return FPrepared; } 
			set { FPrepared = value; } 
		}
		
		// Closed
		private bool FClosed;
		public bool Closed 
		{
			get { return FClosed; } 
			set { FClosed = value; } 
		}
		
		private ServerProcess FPopulatingProcess;
		public void BeginPopulateSource(ServerProcess AProcess)
		{
			lock (this)
			{
				if (FPopulatingProcess != null)
					throw new ApplicationTransactionException(ApplicationTransactionException.Codes.SourceAlreadyPopulating, ID.ToString());
				FPopulatingProcess = AProcess;
			}
		}
		
		public void EndPopulateSource()
		{
			lock (this)
			{
				FPopulatingProcess = null;
			}
		}
		
		public bool IsPopulatingSource { get { return FPopulatingProcess != null; } }
		
		// ATReplayContext
		protected int FATReplayCount;
		/// <summary>Indicates whether this device is replaying an application transaction.</summary>
		public bool InATReplayContext { get { return FATReplayCount > 0; } }
		
		public void EnterATReplayContext()
		{
			FATReplayCount++;
		}
		
		public void ExitATReplayContext()
		{
			FATReplayCount--;
		}
		
		// IsGlobalContext
		private int FGlobalContextCount;
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
		public bool IsGlobalContext { get { return FGlobalContextCount > 0; } }
		
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
				FGlobalContextCount++;
			}
		}
		
		public void PopGlobalContext()
		{
			lock (this)
			{
				FGlobalContextCount--;
			}
		}
		
		// IsLookup
		private int FLookupCount = 0;
		/// <summary>Indicates whether or not we are currently in a lookup context and should not resolve or enlist A/T variables.</summary>
		/// <remarks>
		/// A lookup context is entered when compiling the right side of a left lookup, and is used to prevent the compiler from entering
		/// an A/T on a table that is not going to be modified by the current A/T.
		/// </remarks>
		public bool IsLookup { get { return FLookupCount > 0; } }
		
		public void PushLookup()
		{
			lock (this)
			{
				FLookupCount++;
			}
		}
		
		public void PopLookup()
		{
			lock (this)
			{
				FLookupCount--;
			}
		}
		
		private void AddTableMap(ServerProcess AProcess, TableMap ATableMap)
		{
			int LTableMapIndex = FTableMaps.IndexOfName(ATableMap.SourceTableVar.Name);
			if (LTableMapIndex >= 0)
			{
				if (ATableMap.SourceTableVar is Schema.BaseTableVar)
				{
					if (!Tables.Contains(ATableMap.TableVar))
						Tables.Add(new NativeTable(AProcess.ValueManager, ATableMap.TableVar));
						
					if ((ATableMap.DeletedTableVar != null) && !Tables.Contains(ATableMap.DeletedTableVar))
						Tables.Add(new NativeTable(AProcess.ValueManager, ATableMap.DeletedTableVar));
				}
			}
			else
			{
				FTableMaps.Add(ATableMap);
				if (ATableMap.SourceTableVar is Schema.BaseTableVar)
				{
					Tables.Add(new NativeTable(AProcess.ValueManager, ATableMap.TableVar));
					if (ATableMap.DeletedTableVar != null)
						Tables.Add(new NativeTable(AProcess.ValueManager, ATableMap.DeletedTableVar));
				}
			}
		}
		
		private void AddDependencies(ServerProcess AProcess, Schema.Object AObject)
		{
			Schema.Object LObject;
			if (AObject.HasDependencies())
				for (int LIndex = 0; LIndex < AObject.Dependencies.Count; LIndex++)
				{
					LObject = AObject.Dependencies.ResolveObject(AProcess.CatalogDeviceSession, LIndex);
					if (LObject.IsATObject)
					{
						Schema.TableVar LTableVar = LObject as Schema.TableVar;
						if (LTableVar != null)
						{
							EnsureATTableVarMapped(AProcess, LTableVar);
							continue;
						}
						
						Schema.Operator LOperator = LObject as Schema.Operator;
						if (LOperator != null)
						{
							EnsureATOperatorMapped(AProcess, LOperator);
							continue;
						}
					}
				}
		}
		
		private void AddDependencies(ServerProcess AProcess, TableMap ATableMap)
		{
			AddDependencies(AProcess, ATableMap.TableVar);
			
			// Add dependencies for default expressions, event handlers, and column-level event handlers
			foreach (Schema.TableVarColumn LColumn in ATableMap.TableVar.Columns)
			{
				if (LColumn.Default != null)
					AddDependencies(AProcess, LColumn.Default);

				if (LColumn.HasHandlers())
					foreach (Schema.EventHandler LHandler in LColumn.EventHandlers)
						if (LHandler.Operator.IsATObject)
							EnsureATOperatorMapped(AProcess, LHandler.Operator);
			}

			if (ATableMap.TableVar.HasHandlers())			
				foreach (Schema.EventHandler LHandler in ATableMap.TableVar.EventHandlers)
					if (LHandler.Operator.IsATObject)
						EnsureATOperatorMapped(AProcess, LHandler.Operator);
		}
		
		public void EnsureATTableVarMapped(ServerProcess AProcess, Schema.TableVar AATTableVar)
		{
			lock (AProcess.Catalog)
			{
				lock (Device)
				{
					int LIndex = Device.TableMaps.IndexOfName(AATTableVar.SourceTableName);
					if (LIndex >= 0)
					{
						TableMap LTableMap = Device.TableMaps[AATTableVar.SourceTableName];
						if (!FTableMaps.ContainsName(LTableMap.SourceTableVar.Name))
						{
							AddTableMap(AProcess, LTableMap);
							AddDependencies(AProcess, LTableMap);
						}
					}
					else
					{
						Device.AddTableVar(AProcess, (Schema.TableVar)AProcess.CatalogDeviceSession.ResolveName(Schema.Object.EnsureRooted(AATTableVar.SourceTableName), AProcess.ServerSession.NameResolutionPath, new StringCollection()));
						AddTableMap(AProcess, Device.TableMaps[AATTableVar.SourceTableName]);
					}
				}
			}
		}
		
		public Schema.TableVar AddTableVar(ServerProcess AProcess, Schema.TableVar ASourceTableVar)
		{
			lock (AProcess.Catalog)
			{
				lock (Device)
				{
					int LIndex = Device.TableMaps.IndexOfName(ASourceTableVar.Name);
					if (LIndex >= 0)
					{
						TableMap LTableMap = Device.TableMaps[LIndex];
						AddTableMap(AProcess, LTableMap);
						AddDependencies(AProcess, LTableMap);
						return LTableMap.TableVar;
					}
					else
					{
						Schema.TableVar LResult = Device.AddTableVar(AProcess, ASourceTableVar);
						AddTableMap(AProcess, Device.TableMaps[ASourceTableVar.Name]);
						return LResult;
					}
				}
			}
		}
		
		public OperatorMap EnsureOperatorMap(ServerProcess AProcess, OperatorMap ADeviceOperatorMap)
		{
			int LIndex = FOperatorMaps.IndexOfName(ADeviceOperatorMap.Name);
			if (LIndex >= 0)
				return FOperatorMaps[LIndex];
			else
			{
				OperatorMap LOperatorMap = new OperatorMap(ADeviceOperatorMap.Name, ADeviceOperatorMap.TranslatedOperatorName);
				FOperatorMaps.Add(LOperatorMap);
				return LOperatorMap;
			}
		}
		
		public void EnsureATOperatorMapped(ServerProcess AProcess, Schema.Operator AATOperator)
		{
			OperatorMap LDeviceOperatorMap = Device.EnsureOperatorMap(AProcess, AATOperator.SourceOperatorName, AATOperator.OperatorName);
			OperatorMap LTransactionOperatorMap = EnsureOperatorMap(AProcess, LDeviceOperatorMap);
			if (!LTransactionOperatorMap.Operators.ContainsName(AATOperator.Name))
			{
				LTransactionOperatorMap.Operators.Add(AATOperator);
				AddDependencies(AProcess, AATOperator);
			}
		}

		public Schema.Operator AddOperator(ServerProcess AProcess, Schema.Operator ASourceOperator)
		{
			lock (AProcess.Catalog)
			{
				lock (Device)
				{
					int LIndex = Device.OperatorMaps.IndexOfName(ASourceOperator.OperatorName);
					if (LIndex >= 0)
					{
						OperatorMap LDeviceOperatorMap = Device.OperatorMaps[LIndex];
						OperatorMap LTransactionOperatorMap = EnsureOperatorMap(AProcess, Device.OperatorMaps[ASourceOperator.OperatorName]);
						Schema.Operator LTranslatedOperator = LDeviceOperatorMap.ResolveTranslatedOperator(ASourceOperator);
						if (LTranslatedOperator != null)
						{
							LTransactionOperatorMap.Operators.Add(LTranslatedOperator);
							AddDependencies(AProcess, LTranslatedOperator);
						}
						else
						{
							LTranslatedOperator = Device.AddOperator(AProcess, ASourceOperator);
							LTransactionOperatorMap.Operators.Add(LTranslatedOperator);
						}
						
						return LTranslatedOperator;
					}
					else
					{
						Schema.Operator LATOperator = Device.AddOperator(AProcess, ASourceOperator);
						OperatorMap LOperatorMap = EnsureOperatorMap(AProcess, Device.OperatorMaps[ASourceOperator.OperatorName]);
						LOperatorMap.Operators.Add(LATOperator);
						return LATOperator;
					}
				}
			}
		}
	}
	
	public class ApplicationTransactions : Hashtable
	{
		public ApplicationTransactions() : base() {}
		
		public new ApplicationTransaction this[object AKey]
		{
			get { return (ApplicationTransaction)base[AKey]; }
			set { lock(SyncRoot) { base[AKey] = value; } }
		}
		
		public override void Add(object AKey, object AValue)
		{
			lock (SyncRoot)
			{
				base.Add(AKey, AValue);
			}
		}
		
		public override void Remove(object AKey)
		{
			lock (SyncRoot)
			{
				base.Remove(AKey);
			}
		}

		public override void Clear()
		{
			lock (SyncRoot)
			{
				base.Clear();
			}
		}
	}
	
	public class ApplicationTransactionDevice : MemoryDevice
	{
		public ApplicationTransactionDevice(int AID, string AName, int AResourceManagerID) : base(AID, AName, AResourceManagerID)
		{
			IgnoreUnsupported = true;
		}
		
		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new ApplicationTransactionDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}
		
		// List of currently active application transactions
		private ApplicationTransactions FApplicationTransactions = new ApplicationTransactions();
		public ApplicationTransactions ApplicationTransactions { get { return FApplicationTransactions; } }
		
		// List of tables in the device, and the deleted table and source mapping for each
		private TableMaps FTableMaps = new TableMaps();
		public TableMaps TableMaps { get { return FTableMaps; } }
		
		// List of operators in the device
		private OperatorMaps FOperatorMaps = new OperatorMaps();
		public OperatorMaps OperatorMaps { get { return FOperatorMaps; } }
		
		protected void CopyTableVar(ServerProcess AProcess, Schema.TableVar ASourceTableVar, bool AIsMainTableVar)
		{
			string LTableVarName = String.Format(".AT_{0}{1}", ASourceTableVar.Name.Replace('.', '_'), AIsMainTableVar ? String.Empty : "_Deleted");
			Block LBlock = new Block();
			CreateTableVarStatement LStatement = (CreateTableVarStatement)ASourceTableVar.EmitStatement(EmitMode.ForCopy);
			LStatement.TableVarName = LTableVarName;
			if (ASourceTableVar is Schema.BaseTableVar)
				((CreateTableStatement)LStatement).DeviceName = new IdentifierExpression(Name);
			LStatement.IsSession = false;
			if (LStatement.MetaData == null)
				LStatement.MetaData = new MetaData();
			LStatement.MetaData.Tags.SafeRemove("DAE.GlobalObjectName");
			LStatement.MetaData.Tags.AddOrUpdate("DAE.SourceTableName", ASourceTableVar.Name, true);
			LStatement.MetaData.Tags.AddOrUpdate("DAE.IsDeletedTable", (!AIsMainTableVar).ToString(), true);

			LBlock.Statements.Add(LStatement);

			Plan LPlan = new Plan(AProcess);
			try
			{
				LPlan.PushSecurityContext(new SecurityContext(ASourceTableVar.Owner));
				try
				{
					LPlan.PushATCreationContext();
					try
					{
						Program LProgram = new Program(AProcess);
						LProgram.Code = Compiler.Bind(LPlan, Compiler.CompileStatement(LPlan, LBlock));
						LPlan.CheckCompiled();
						LProgram.Execute(null);
					}
					finally
					{
						LPlan.PopATCreationContext();
					}
				}
				finally
				{
					LPlan.PopSecurityContext();
				}
			}
			finally
			{
				LPlan.Dispose();
			}

			if (AIsMainTableVar)
			{
				TableMap LTableMap = TableMaps[ASourceTableVar.Name];

				LBlock = new Block();

				foreach (Schema.TableVarColumn LColumn in ASourceTableVar.Columns)
				{
					if ((LColumn.Default != null) && !LColumn.Default.IsRemotable)
					{
						AlterTableStatement LAlterStatement = new AlterTableStatement();
						LAlterStatement.TableVarName = Schema.Object.EnsureRooted(LTableMap.TableVar.Name);
						AlterColumnDefinition LDefinition = new AlterColumnDefinition();
						LDefinition.ColumnName = LColumn.Name;
						LDefinition.Default = LColumn.Default.EmitDefinition(EmitMode.ForCopy);
						((DefaultDefinition)LDefinition.Default).IsGenerated = true;
						LAlterStatement.AlterColumns.Add(LDefinition);
						LBlock.Statements.Add(LAlterStatement);
					}
				}
				
				if (ASourceTableVar.HasHandlers())
					foreach (Schema.EventHandler LHandler in ASourceTableVar.EventHandlers)
						if (!LHandler.IsGenerated && LHandler.ShouldTranslate)
						{
							AttachStatement LAttachStatement = (AttachStatement)LHandler.EmitTableVarHandler(ASourceTableVar, EmitMode.ForCopy);
							if (LAttachStatement.MetaData == null)
								LAttachStatement.MetaData = new MetaData();
							LAttachStatement.MetaData.Tags.RemoveTag("DAE.ObjectID");
							LAttachStatement.MetaData.Tags.AddOrUpdate("DAE.ATHandlerName", LHandler.Name, true);
							((ObjectEventSourceSpecifier)LAttachStatement.EventSourceSpecifier).ObjectName = Schema.Object.EnsureRooted(LTableMap.TableVar.Name);
							if (LHandler.Operator.ShouldTranslate)
								LAttachStatement.OperatorName = Schema.Object.EnsureRooted(EnsureOperator(AProcess, LHandler.Operator).OperatorName);
							LAttachStatement.IsGenerated = true;
							LBlock.Statements.Add(LAttachStatement);
						}
					
				foreach (Schema.TableVarColumn LColumn in ASourceTableVar.Columns)
					if (LColumn.HasHandlers())
						foreach (Schema.EventHandler LHandler in LColumn.EventHandlers)
							if (!LHandler.IsGenerated && LHandler.ShouldTranslate)
							{
								AttachStatement LAttachStatement = (AttachStatement)LHandler.EmitColumnHandler(ASourceTableVar, LColumn, EmitMode.ForCopy);
								if (LAttachStatement.MetaData == null)
									LAttachStatement.MetaData = new MetaData();
								LAttachStatement.MetaData.Tags.RemoveTag("DAE.ObjectID");
								LAttachStatement.MetaData.Tags.AddOrUpdate("DAE.ATHandlerName", LHandler.Name, true);
								((ColumnEventSourceSpecifier)LAttachStatement.EventSourceSpecifier).TableVarName = Schema.Object.EnsureRooted(LTableMap.TableVar.Name);
								if (LHandler.Operator.ShouldTranslate)
									LAttachStatement.OperatorName = Schema.Object.EnsureRooted(EnsureOperator(AProcess, LHandler.Operator).OperatorName);
								LAttachStatement.IsGenerated = true;
								LBlock.Statements.Add(LAttachStatement);
							}

				LPlan = new Plan(AProcess);
				try
				{
					LPlan.PushSecurityContext(new SecurityContext(ASourceTableVar.Owner));
					try
					{
						LPlan.EnterTimeStampSafeContext();
						try
						{
							Program LProgram = new Program(AProcess);
							LProgram.Code = Compiler.Bind(LPlan, Compiler.CompileStatement(LPlan, LBlock));
							LPlan.CheckCompiled();
							LProgram.Execute(null);
						}
						finally
						{
							LPlan.ExitTimeStampSafeContext();
						}
					}
					finally
					{
						LPlan.PopSecurityContext();
					}
				}
				finally
				{
					LPlan.Dispose();
				}
			}
		}
		
		public void AddTableMap(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			// Called by the compiler during processing of the create table statement created by CopyTableVar.
			// CreatingTableVarName is the name of the table variable being added to the application transaction.
			// If the table map is already present for this table, then this is the deleted tracking table.
			string LSourceTableVarName = MetaData.GetTag(ATableVar.MetaData, "DAE.SourceTableName", ATableVar.Name);
			int LIndex = FTableMaps.IndexOfName(LSourceTableVarName);
			if (LIndex < 0)
			{
				TableMap LTableMap = new TableMap(LSourceTableVarName);
				LTableMap.TableVar = ATableVar;
				LTableMap.SourceTableVar = (TableVar)AProcess.Catalog[LSourceTableVarName];
				AProcess.CatalogDeviceSession.AddTableMap(this, LTableMap);
			}
			else
			{
				TableMap LTableMap = FTableMaps[LIndex];
				LTableMap.DeletedTableVar = ATableVar;
			}
		}
		
		private void CheckNotParticipating(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			lock (ApplicationTransactions.SyncRoot)
			{
				foreach (ApplicationTransaction LTransaction in ApplicationTransactions.Values)
					if (LTransaction.TableMaps.ContainsName(ATableVar.Name))
						throw new ApplicationTransactionException(ApplicationTransactionException.Codes.TableVariableParticipating, ATableVar.Name);
			}
		}
		
		public void ReportTableChange(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			// If the table var is a source table var for an A/T table
				// if there are active A/Ts for the table var
					// throw an error
				// otherwise
					// detach any A/T handlers that may be attached to the A/T table var (safely)
					// drop the A/T table vars (safely)
					// remove the table map
					
			if (!AProcess.ServerSession.Server.IsRepository)
			{
				lock (AProcess.Catalog)
				{
					lock (this)
					{
						StringCollection LObjectNames = new StringCollection();
						int LTableMapIndex = FTableMaps.IndexOfName(ATableVar.Name);
						if (LTableMapIndex >= 0)
						{
							CheckNotParticipating(AProcess, ATableVar);
							
							// Drop the table var and deleted table var
							TableMap LTableMap = FTableMaps[LTableMapIndex];
							if (LTableMap.TableVar != null)
							{
								for (int LIndex = 0; LIndex < LTableMap.TableVar.EventHandlers.Count; LIndex++)
									LObjectNames.Add(LTableMap.TableVar.EventHandlers[LIndex].Name);
								LObjectNames.Add(LTableMap.TableVar.Name);
							}

							if (LTableMap.DeletedTableVar != null)
								LObjectNames.Add(LTableMap.DeletedTableVar.Name);
							
							AProcess.CatalogDeviceSession.RemoveTableMap(this, LTableMap);
						}

						if (LObjectNames.Count > 0)
						{
							string[] LObjectNameArray = new string[LObjectNames.Count];
							LObjectNames.CopyTo(LObjectNameArray, 0);
							Plan LPlan = new Plan(AProcess);
							try
							{
								LPlan.EnterTimeStampSafeContext();
								try
								{
									Program LProgram = new Program(AProcess);
									LProgram.Code = 
										Compiler.Bind
										(
											LPlan, 
											Compiler.Compile
											(
												LPlan, 
												AProcess.Catalog.EmitDropStatement
												(
													AProcess.CatalogDeviceSession, 
													LObjectNameArray, 
													String.Empty, 
													true, 
													false, 
													true, 
													true
												)
											)
										);
									LPlan.CheckCompiled();
									LProgram.Execute(null);
								}
								finally
								{
									LPlan.ExitTimeStampSafeContext();
								}
							}
							finally
							{
								LPlan.Dispose();
							}
						}
					}
				}
			}
		}
		
		public Schema.Operator ResolveSourceOperator(Plan APlan, Schema.Operator ATranslatedOperator)
		{
			return Compiler.ResolveOperator(APlan, ATranslatedOperator.SourceOperatorName, ATranslatedOperator.Signature, false, false);
		}
		
		public void ReportOperatorChange(ServerProcess AProcess, Schema.Operator AOperator)
		{
			// If the operator is a source operator for an A/T operator
				// if there are active A/Ts for the operator, or for any A/T table var that may have a handler attached to this operator
					// throw an error
				// otherwise
					// detach the A/T operators from any A/T table vars they may be attached to (safely)
					// drop the A/T operators (safely)
					// remove the operator map
					// report table changes on the A/T table vars
					
			if (!AProcess.ServerSession.Server.IsRepository)
			{
				lock (AProcess.Catalog)
				{
					lock (this)
					{
						StringCollection LObjectNames = new StringCollection();
						Schema.Operator LATOperator = null;
						int LOperatorMapIndex = FOperatorMaps.IndexOfName(AOperator.OperatorName);
						if (LOperatorMapIndex >= 0)
						{
							OperatorMap LOperatorMap = FOperatorMaps[LOperatorMapIndex];
							
							LATOperator = LOperatorMap.ResolveTranslatedOperator(AOperator);
							if (LATOperator != null)
							{
								// Check for tables with event handlers attached to this operator
								List<int> LHandlerIDs = AProcess.CatalogDeviceSession.SelectOperatorHandlers(AOperator.ID);

								for (int LIndex = 0; LIndex < LHandlerIDs.Count; LIndex++)
								{
									Schema.Object LHandler = AProcess.CatalogDeviceSession.ResolveCatalogObject(LHandlerIDs[LIndex]);
									Schema.TableVar LTableVar = null;
									if (LHandler is TableVarEventHandler)
										LTableVar = ((TableVarEventHandler)LHandler).TableVar;
									if (LHandler is TableVarColumnEventHandler)
										LTableVar = ((TableVarColumnEventHandler)LHandler).TableVarColumn.TableVar;
									
									if (LTableVar != null)
									{
										int LTableMapIndex = FTableMaps.IndexOfName(LTableVar.Name);
										if (LTableMapIndex >= 0)
										{
											bool LShouldCheckTableVar = false;
											TableMap LTableMap = FTableMaps[LTableMapIndex];
											foreach (Schema.TableVarEventHandler LTableVarEventHandler in LTableMap.TableVar.EventHandlers)
												if (LTableVarEventHandler.Operator.Name == LATOperator.Name)
													LShouldCheckTableVar = true;
											
											foreach (Schema.TableVarColumn LTableVarColumn in LTableMap.TableVar.Columns)
												foreach (Schema.TableVarColumnEventHandler LTableVarColumnEventHandler in LTableVarColumn.EventHandlers)
													if (LTableVarColumnEventHandler.Operator.Name == LATOperator.Name)
														LShouldCheckTableVar = true;
											
											if (LShouldCheckTableVar)
												ReportTableChange(AProcess, LTableVar);
										}
									}	
								}
								
								lock (ApplicationTransactions.SyncRoot)
								{
									foreach (ApplicationTransaction LTransaction in ApplicationTransactions.Values)
									{
										int LTransactionOperatorMapIndex = LTransaction.OperatorMaps.IndexOfName(AOperator.OperatorName);
										if (LTransactionOperatorMapIndex >= 0)
										{
											OperatorMap LTransactionOperatorMap = LTransaction.OperatorMaps[LTransactionOperatorMapIndex];
											if (LTransactionOperatorMap.Operators.ContainsName(LATOperator.Name))
												throw new ApplicationTransactionException(ApplicationTransactionException.Codes.OperatorParticipating, AOperator.OperatorName);
										}
									}
								}
								
								// Drop the operator
								AProcess.CatalogDeviceSession.RemoveOperatorMap(LOperatorMap, LATOperator);
								// BTR 2/1/2007 -> Operator Map should never hurt to leave around, so leave it instead of trying to manage the complexity of making it transactional (any process could add the operator map)
								//if (LOperatorMap.Operators.Count == 0)
								//	FOperatorMaps.RemoveAt(LOperatorMapIndex);
							}
						}
						
						if (LATOperator != null)
						{
							LObjectNames.Add(LATOperator.Name);
							string[] LObjectNameArray = new string[LObjectNames.Count];
							LObjectNames.CopyTo(LObjectNameArray, 0);
							Plan LPlan = new Plan(AProcess);
							try
							{
								LPlan.EnterTimeStampSafeContext();
								try
								{
									Program LProgram = new Program(AProcess);
									LProgram.Code = Compiler.Bind(LPlan, Compiler.Compile(LPlan, AProcess.Catalog.EmitDropStatement(AProcess.CatalogDeviceSession, LObjectNameArray, String.Empty)));
									LPlan.CheckCompiled();
									LProgram.Execute(null);
								}
								finally
								{
									LPlan.ExitTimeStampSafeContext();
								}
							}
							finally
							{
								LPlan.Dispose();
							}
						}
					}
				}
			}
		}
		
		// AddTableVar - Creates a TableMap for the given source table and adds it to the table maps, if it is not already present, then returns the name of the mapped table
		public Schema.TableVar AddTableVar(ServerProcess AProcess, Schema.TableVar ASourceTableVar)
		{
			if (ASourceTableVar.SourceTableName != null)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.AlreadyApplicationTransactionVariable, ASourceTableVar.Name, ASourceTableVar.SourceTableName);
				
			string LSourceTableName = ASourceTableVar.Name;
			TableMap LTableMap = null;
			
			// Push an adding table var context
			AProcess.PushAddingTableVar();
			try
			{
				int LTableIndex = FTableMaps.IndexOf(LSourceTableName);
				if (LTableIndex >= 0)
					LTableMap = FTableMaps[LTableIndex];
				else
				{
					bool LSaveIsInsert = AProcess.IsInsert;
					AProcess.IsInsert = false;
					try
					{
						CopyTableVar(AProcess, ASourceTableVar, true);
						LTableMap = FTableMaps[LSourceTableName];
						if (ASourceTableVar is Schema.BaseTableVar)
						{
							BaseTableVar LTargetTableVar = (BaseTableVar)LTableMap.TableVar;
							try
							{
								CopyTableVar(AProcess, ASourceTableVar, false);
								BaseTableVar LDeletedTableVar = (BaseTableVar)LTableMap.DeletedTableVar;
								try
								{
									CompileTableMapRetrieveNodes(AProcess, ASourceTableVar, LTableMap);
								}
								catch
								{
									Plan LPlan = new Plan(AProcess);
									try							 
									{
										Program LProgram = new Program(AProcess);
										LProgram.Code = Compiler.Bind(LPlan, new DropTableNode(LDeletedTableVar, false));
										LProgram.Execute(null);
									}
									finally
									{
										LPlan.Dispose();
									}
									throw;
								}
							}
							catch
							{
								Plan LPlan = new Plan(AProcess);
								try							 
								{
									Program LProgram = new Program(AProcess);
									LProgram.Code = Compiler.Bind(LPlan, new DropTableNode(LTargetTableVar, false));
									LProgram.Execute(null);
								}
								finally
								{
									LPlan.Dispose();
								}
								throw;
							}
						}
					}
					finally
					{
						AProcess.IsInsert = LSaveIsInsert;
					}
				}
			}
			finally
			{
				AProcess.PopAddingTableVar();
			}

			return LTableMap.TableVar; // return the supporting table variable (generated unique name)
		}

		private void CompileTableMapRetrieveNodes(ServerProcess AProcess, Schema.TableVar ASourceTableVar, TableMap ATableMap)
		{
			ApplicationTransactions[AProcess.ApplicationTransactionID].PushGlobalContext();
			try
			{
				Plan LPlan = new Plan(AProcess);
				try
				{
					LPlan.PushCursorContext(new CursorContext(DAE.CursorType.Static, CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated));
					try
					{
						LPlan.PushSecurityContext(new SecurityContext(ASourceTableVar.Owner));
						try
						{
							ATableMap.RetrieveNode = (BaseTableVarNode)Compiler.Bind(LPlan, Compiler.EmitBaseTableVarNode(LPlan, ASourceTableVar));
							ATableMap.DeletedRetrieveNode = (BaseTableVarNode)Compiler.Bind(LPlan, Compiler.EmitBaseTableVarNode(LPlan, ATableMap.DeletedTableVar));
						}
						finally
						{
							LPlan.PopSecurityContext();
						}
					}
					finally
					{
						LPlan.PopCursorContext();
					}

					Schema.Key LClusteringKey = Compiler.FindClusteringKey(LPlan, ATableMap.TableVar);
					Schema.RowType LOldRowType = new Schema.RowType(ATableMap.TableVar.DataType.Columns, Keywords.Old);
					Schema.RowType LOldKeyType = new Schema.RowType(LClusteringKey.Columns, Keywords.Old);
					Schema.RowType LKeyType = new Schema.RowType(LClusteringKey.Columns);
					LPlan.EnterRowContext();
					try
					{
						LPlan.Symbols.Push(new Symbol(LOldRowType));
						try
						{
							ATableMap.HasDeletedRowNode =
								Compiler.Bind
								(
									LPlan,
									Compiler.EmitUnaryNode
									(
										LPlan,
										Instructions.Exists,
										Compiler.EmitRestrictNode
										(
											LPlan,
											Compiler.EmitBaseTableVarNode(LPlan, ATableMap.DeletedTableVar),
											Compiler.BuildKeyEqualExpression(LPlan, LOldKeyType.Columns, LKeyType.Columns)
										)
									)
								);

							ATableMap.HasRowNode =
								Compiler.Bind
								(
									LPlan,
									Compiler.EmitBinaryNode
									(
										LPlan,
										Compiler.EmitUnaryNode
										(
											LPlan,
											Instructions.Exists,
											Compiler.EmitRestrictNode
											(
												LPlan,
												Compiler.EmitBaseTableVarNode(LPlan, ATableMap.TableVar),
												Compiler.BuildKeyEqualExpression(LPlan, LOldKeyType.Columns, LKeyType.Columns)
											)
										),
										Instructions.Or,
										ATableMap.HasDeletedRowNode
									)
								);
						}
						finally
						{
							LPlan.Symbols.Pop();
						}
					}
					finally
					{
						LPlan.ExitRowContext();
					}
				}
				finally
				{
					LPlan.Dispose();
				}
			}
			finally
			{
				ApplicationTransactions[AProcess.ApplicationTransactionID].PopGlobalContext();
			}
		}
		
		public OperatorMap EnsureOperatorMap(ServerProcess AProcess, string AOperatorName, string ATranslatedOperatorName)
		{
			OperatorMap LOperatorMap;
			int LIndex = FOperatorMaps.IndexOfName(AOperatorName);
			if (LIndex >= 0)
				LOperatorMap = FOperatorMaps[LIndex];
			else
			{
				if (ATranslatedOperatorName != null)
					LOperatorMap = new OperatorMap(AOperatorName, ATranslatedOperatorName);
				else
					LOperatorMap = new OperatorMap(AOperatorName, String.Format("AT_{0}", AOperatorName.Replace('.', '_')));
				FOperatorMaps.Add(LOperatorMap);
			}
			return LOperatorMap;
		}
		
		public Schema.Operator AddOperator(ServerProcess AProcess, Schema.Operator AOperator)
		{
			// Recompile the operator in the application transaction process
			OperatorMap LOperatorMap = EnsureOperatorMap(AProcess, AOperator.OperatorName, null);
			CreateOperatorStatementBase LStatement = (CreateOperatorStatementBase)AOperator.EmitStatement(EmitMode.ForCopy);
			LStatement.OperatorName = String.Format(".{0}", LOperatorMap.TranslatedOperatorName);
			LStatement.IsSession = false;
			if (LStatement.MetaData == null)
				LStatement.MetaData = new MetaData();
			LStatement.MetaData.Tags.SafeRemove("DAE.GlobalObjectName");
			LStatement.MetaData.Tags.AddOrUpdate("DAE.SourceOperatorName", LOperatorMap.Name, true);

			Plan LPlan = new Plan(AProcess);
			try
			{
				bool LSaveIsInsert = AProcess.IsInsert;
				AProcess.IsInsert = false;
				try
				{
					LPlan.PushSecurityContext(new SecurityContext(AOperator.Owner));
					try
					{
						LPlan.PushATCreationContext();
						try
						{
							Program LProgram = new Program(AProcess);
							LProgram.Code = Compiler.Bind(LPlan, Compiler.CompileStatement(LPlan, LStatement));
							LPlan.CheckCompiled();
							LProgram.Execute(null);
							Schema.Operator LOperator = ((CreateOperatorNode)LProgram.Code).CreateOperator;
							AProcess.CatalogDeviceSession.AddOperatorMap(LOperatorMap, LOperator);
							return LOperator;
						}
						finally
						{
							LPlan.PopATCreationContext();
						}
					}
					finally
					{
						LPlan.PopSecurityContext();
					}
				}
				finally
				{
					AProcess.IsInsert = LSaveIsInsert;
				}
			}
			finally
			{
				LPlan.Dispose();
			}
		}
		
		public Schema.Operator EnsureOperator(ServerProcess AProcess, Schema.Operator AOperator)
		{
			using (Plan LPlan = new Plan(AProcess))
			{
				OperatorBindingContext LContext = new OperatorBindingContext(null, AOperator.OperatorName, LPlan.NameResolutionPath, AOperator.Signature, false);
				Compiler.ResolveOperator(LPlan, LContext);
				Error.AssertFail(LContext.Operator != null, @"Operator ""{0}"" was not translated into the application transaction as expected");
				return LContext.Operator;
			}
		}
	}

	public abstract class Operation : System.Object
	{
		public Operation(ApplicationTransaction ATransaction, TableVar ATableVar) : base()
		{
			FTransaction = ATransaction;
			FTableVar = ATableVar;
		}
		
		private ApplicationTransaction FTransaction;
		public ApplicationTransaction Transaction { get { return FTransaction; } }
		
		private TableVar FTableVar;
		public TableVar TableVar { get { return FTableVar; } }
		
		private TableMap FTableMap;
		protected TableMap TableMap
		{
			get
			{
				if (FTableMap == null)
				{
					FTableMap = FTransaction.Device.TableMaps[FTableVar.SourceTableName];
					if (FTableMap == null)
						throw new ApplicationTransactionException(ApplicationTransactionException.Codes.TableMapNotFound, FTableVar.Name);
				}
				return FTableMap;
			}
		}
		
		protected bool FApplied = false;
		
		public void ResetApplied()
		{
			FApplied = false;
		}
		
		public abstract void Apply(Program AProgram);
		
		public abstract void Undo(Program AProgram);
		
		public abstract void Dispose(IValueManager AManager);
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
		public InsertOperation(ApplicationTransaction ATransaction, TableVar ATableVar, NativeRow ARow) : base(ATransaction, ATableVar)
		{
			FRow = ARow;
		}
		
		private NativeRow FRow;
		public NativeRow Row { get { return FRow; } }
		
		public override void Apply(Program AProgram)
		{
			if (!FApplied)
			{
				Row LRow = new Row(AProgram.ValueManager, TableMap.RowType, FRow);
				try
				{
					TableMap.RetrieveNode.Insert(AProgram, null, LRow, TableMap.ValueFlags, false);
				}
				finally
				{
					LRow.Dispose();
				}
				FApplied = true;
			}
		}
		
		public override void Undo(Program AProgram)
		{
			if (FApplied)
			{
				Row LRow = new Row(AProgram.ValueManager, TableMap.RowType, FRow);
				try
				{
					TableMap.RetrieveNode.Delete(AProgram, LRow, false, false);
				}
				finally
				{
					LRow.Dispose();
				}
				FApplied = false;
			}
		}

		public override void Dispose(IValueManager AManager)
		{
			DataValue.DisposeNative(AManager, TableMap.RowType, FRow);
		}
	}
	
	public class UpdateOperation : Operation
	{
		public UpdateOperation(ApplicationTransaction ATransaction, TableVar ATableVar, NativeRow AOldRow, NativeRow ANewRow) : base(ATransaction, ATableVar)
		{
			FOldRow = AOldRow;
			FNewRow = ANewRow;
		}

		private NativeRow FOldRow;
		public NativeRow OldRow { get { return FOldRow; } }
		
		private NativeRow FNewRow; 
		public NativeRow NewRow { get { return FNewRow; } }

		public override void Apply(Program AProgram)
		{
			if (!FApplied)
			{
				Row LOldRow = new Row(AProgram.ValueManager, TableMap.RowType, FOldRow);
				try
				{
					Row LNewRow = new Row(AProgram.ValueManager, TableMap.RowType, FNewRow);
					try
					{
						TableMap.RetrieveNode.Update(AProgram, LOldRow, LNewRow, null, true, false);
					}
					finally
					{
						LNewRow.Dispose();
					}
				}
				finally
				{
					LOldRow.Dispose();
				}
				FApplied = true;
			}
		}
		
		public override void Undo(Program AProgram)
		{
			if (FApplied)
			{
				Row LOldRow = new Row(AProgram.ValueManager, TableMap.RowType, FOldRow);
				try
				{
					Row LNewRow = new Row(AProgram.ValueManager, TableMap.RowType, FNewRow);
					try
					{
						TableMap.RetrieveNode.Update(AProgram, LNewRow, LOldRow, null, false, true);
					}
					finally
					{
						LNewRow.Dispose();
					}
				}
				finally
				{
					LOldRow.Dispose();
				}
				FApplied = false;
			}
		}

		public override void Dispose(IValueManager AManager)
		{
			DataValue.DisposeNative(AManager, TableMap.RowType, FOldRow);
			DataValue.DisposeNative(AManager, TableMap.RowType, FNewRow);
		}
	}
	
	public class DeleteOperation : Operation
	{
		public DeleteOperation(ApplicationTransaction ATransaction, TableVar ATableVar, NativeRow ARow) : base(ATransaction, ATableVar)
		{
			FRow = ARow;
		}
		
		private NativeRow FRow;
		public NativeRow Row { get { return FRow; } }
		
		public override void Apply(Program AProgram)
		{
			if (!FApplied)
			{
				Row LRow = new Row(AProgram.ValueManager, TableMap.RowType, FRow);
				try
				{
					TableMap.RetrieveNode.Delete(AProgram, LRow, true, false);
				}
				finally
				{
					LRow.Dispose();
				}
				FApplied = true;
			}
		}
		
		public override void Undo(Program AProgram)
		{
			if (FApplied)
			{
				Row LRow = new Row(AProgram.ValueManager, TableMap.RowType, FRow);
				try
				{
					TableMap.RetrieveNode.Insert(AProgram, null, LRow, TableMap.ValueFlags, true);
				}
				finally
				{
					LRow.Dispose();
				}
				FApplied = false;
			}
		}

		public override void Dispose(IValueManager AManager)
		{
			DataValue.DisposeNative(AManager, TableMap.RowType, FRow);
		}
	}

	public class ApplicationTransactionDeviceTransaction : System.Object
	{
		public ApplicationTransactionDeviceTransaction(IsolationLevel AIsolationLevel) : base()
		{
			FIsolationLevel = AIsolationLevel;
		}
		
		private IsolationLevel FIsolationLevel;
		public IsolationLevel IsolationLevel { get { return FIsolationLevel; } }
		
		private Operations FOperations = new Operations();
		public Operations Operations { get { return FOperations; } }
		
		private Operations FAppliedOperations = new Operations();
		public Operations AppliedOperations { get { return FAppliedOperations; } }
		
		// Committed nested transactions which must be rolled back if this transaction rolls back
		private ApplicationTransactionDeviceTransactions FTransactions;
		public ApplicationTransactionDeviceTransactions Transactions 
		{ 
			get 
			{
				if (FTransactions == null)
					FTransactions = new ApplicationTransactionDeviceTransactions();
				return FTransactions; 
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
		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			Add(new ApplicationTransactionDeviceTransaction(AIsolationLevel));
		}
		
		// ASuccess indicates whether the transaction ended successfully
		public void EndTransaction(bool ASuccess)
		{
			if (ASuccess && (Count > 1))
			{
				ApplicationTransactionDeviceTransaction LTransaction = this[Count - 1];
				this[Count - 2].Transactions.Add(LTransaction);
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
			Schema.Device ADevice, 
			ServerProcess AServerProcess, 
			DeviceSessionInfo ADeviceSessionInfo
		) : base(ADevice, AServerProcess, ADeviceSessionInfo) {}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				EnsureTransactionsRolledback();
			}
			finally
			{
				FTransaction = null;
				base.Dispose(ADisposing);
			}
		}
		
		private ApplicationTransactionDeviceTransactions FTransactions = new ApplicationTransactionDeviceTransactions();
		public new ApplicationTransactionDeviceTransactions Transactions { get { return FTransactions; } }
		
		public new ApplicationTransactionDevice Device { get { return (ApplicationTransactionDevice)base.Device; } }
		
		private ApplicationTransaction FTransaction;
		public ApplicationTransaction Transaction 
		{ 
			get 
			{ 
				if (FTransaction == null)
					FTransaction = Device.ApplicationTransactions[ServerProcess.ApplicationTransactionID];
				return FTransaction;
			} 
		}
		
		protected override void InternalBeginTransaction(IsolationLevel AIsolationLevel)
		{
			FTransactions.BeginTransaction(AIsolationLevel);
		}

		protected override void InternalPrepareTransaction()
		{
			// do nothing, vote yes
		}
		
		protected override void InternalCommitTransaction()
		{
			FTransactions.EndTransaction(true);
		}
		
		protected void InternalRollbackTransaction(ApplicationTransactionDeviceTransaction ATransaction)
		{
			Exception LException = null;
			int LOperationIndex;
			foreach (Operation LOperation in ATransaction.Operations)
			{
				try
				{
					LOperationIndex = Transaction.Operations.IndexOf(LOperation);
					if (LOperationIndex >= 0)
						Transaction.Operations.RemoveAt(LOperationIndex);
					LOperation.Dispose(ServerProcess.ValueManager);
				}
				catch (Exception E)
				{
					LException = E;
					ServerProcess.ServerSession.Server.LogError(E);
				}
			}
			
			foreach (Operation LOperation in ATransaction.AppliedOperations)
				LOperation.ResetApplied();
			
			foreach (ApplicationTransactionDeviceTransaction LTransaction in ATransaction.Transactions)
			{
				try
				{
					InternalRollbackTransaction(LTransaction);
				}
				catch (Exception E)
				{
					LException = E;
					ServerProcess.ServerSession.Server.LogError(E);
				}
			}
			
			if (LException != null)
				throw LException;
		}

		protected override void InternalRollbackTransaction()
		{
			InternalRollbackTransaction(FTransactions.CurrentTransaction());
			FTransactions.EndTransaction(false);
		}

		private TableMap GetTableMap(TableVar ATableVar)
		{
			TableMap LTableMap = Device.TableMaps[ATableVar.SourceTableName];
			if (LTableMap == null)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.TableMapNotFound, ATableVar.Name);
			return LTableMap;
		}
		
		public override NativeTables GetTables(Schema.TableVarScope AScope) { return Transaction.Tables; }
		
		protected override object InternalExecute(Program AProgram, DevicePlan ADevicePlan)
		{
			if (ADevicePlan.Node is CreateTableVarBaseNode)
			{
				return null; // Actual storage will be allocated by the application transaction
			}
			else if (ADevicePlan.Node is DropTableNode)
			{
				return null; // Actual storage will be deallocated by the application transaction
			}
			else
				return base.InternalExecute(AProgram, ADevicePlan);
		}

		protected override void InternalInsertRow(Program AProgram, TableVar ATableVar, Row ARow, BitArray AValueFlags)
		{
			InsertOperation LOperation = null;
			if (Transaction.IsPopulatingSource)
			{
				TableMap LTableMap = GetTableMap(ATableVar);

				// Don't insert a row if it is in the DeletedTableVar or the TableVar of the TableMap for this TableVar
				// if not(exists(DeletedTableVar where ARow) or exists(TableVar where ARow))
				Row LRow = new Row(AProgram.ValueManager, ATableVar.DataType.OldRowType, (NativeRow)ARow.AsNative);
				try
				{
					AProgram.Stack.Push(LRow);
					try
					{
						if (!(bool)LTableMap.HasRowNode.Execute(AProgram))
							base.InternalInsertRow(AProgram, ATableVar, ARow, AValueFlags);
					}
					finally
					{
						AProgram.Stack.Pop();
					}
				}
				finally
				{
					LRow.Dispose();
				}
			}
			else
			{
				if (Transaction.Closed)
					throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ApplicationTransactionClosed, Transaction.ID.ToString());

				base.InternalInsertRow(AProgram, ATableVar, ARow, AValueFlags);
				Transaction.Prepared = false;

				if (!InTransaction || !ServerProcess.CurrentTransaction.InRollback)
				{
					// If this is the deleted table var for a table map, do not log the operation as part of the application transaction
					if (!ATableVar.IsDeletedTable)
					{
						LOperation = new InsertOperation(Transaction, ATableVar, (NativeRow)ARow.CopyNative());
						Transaction.Operations.Add(LOperation);
					}
				}
			}

			if (InTransaction && !ServerProcess.NonLogged && (LOperation != null) && !ServerProcess.CurrentTransaction.InRollback)
				FTransactions.CurrentTransaction().Operations.Add(LOperation);
		}
		
		protected override void InternalUpdateRow(Program AProgram, TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			if (Transaction.Closed)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ApplicationTransactionClosed);

			base.InternalUpdateRow(AProgram, ATableVar, AOldRow, ANewRow, AValueFlags);
			Transaction.Prepared = false;

			if (!InTransaction || !ServerProcess.CurrentTransaction.InRollback)
			{			
				UpdateOperation LOperation = new UpdateOperation(Transaction, ATableVar, (NativeRow)AOldRow.CopyNative(), (NativeRow)ANewRow.CopyNative());
				Transaction.Operations.Add(LOperation);
				if (InTransaction && !ServerProcess.NonLogged)
					FTransactions.CurrentTransaction().Operations.Add(LOperation);

				LogDeletedRow(AProgram, ATableVar, AOldRow);
			}
		}
		
		protected void LogDeletedRow(Program AProgram, TableVar ATableVar, Row ARow)
		{
			TableMap LTableMap = GetTableMap(ATableVar);
			Row LRow = new Row(AProgram.ValueManager, ATableVar.DataType.OldRowType, (NativeRow)ARow.AsNative);
			try
			{
				AProgram.Stack.Push(LRow);
				try
				{
					if (!(bool)LTableMap.HasDeletedRowNode.Execute(AProgram))
						LTableMap.DeletedRetrieveNode.Insert(AProgram, null, ARow, null, true);
				}
				finally
				{
					AProgram.Stack.Pop();
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		protected override void InternalDeleteRow(Program AProgram, TableVar ATableVar, Row ARow)
		{
			if (Transaction.Closed)
				throw new ApplicationTransactionException(ApplicationTransactionException.Codes.ApplicationTransactionClosed);

			base.InternalDeleteRow(AProgram, ATableVar, ARow);
			Transaction.Prepared = false;
			
			if (!InTransaction || !ServerProcess.CurrentTransaction.InRollback)
			{
				DeleteOperation LOperation = new DeleteOperation(Transaction, ATableVar, (NativeRow)ARow.CopyNative());
				Transaction.Operations.Add(LOperation);
				if (InTransaction && !ServerProcess.NonLogged)
					FTransactions.CurrentTransaction().Operations.Add(LOperation);

				LogDeletedRow(AProgram, ATableVar, ARow);
			}
		}
	}
}

