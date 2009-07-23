/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.EnterpriseServices;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;

namespace Alphora.Dataphor.DAE.Server
{
	[Transaction(TransactionOption.RequiresNew)]
	public class ServerDTCTransaction : ServicedComponent
	{
		protected override void Dispose(bool ADisposing)
		{
			Rollback();
			base.Dispose(ADisposing);
		}
		
		public void Commit()
		{
			ContextUtil.SetComplete();
		}
		
		public void Rollback()
		{
			ContextUtil.SetAbort();
		}
		
		private IsolationLevel FIsolationLevel;
		public IsolationLevel IsolationLevel
		{
			get { return FIsolationLevel; }
			set { FIsolationLevel = value; }
		}
	}	

	public class ServerTransaction : Disposable
	{
		public ServerTransaction(ServerProcess AProcess, IsolationLevel AIsolationLevel) : base()
		{
			FProcess = AProcess;
			FIsolationLevel = AIsolationLevel;
			FStartTime = DateTime.Now;
		}
		
		protected void UnprepareDeferredHandlers()
		{
			for (int LIndex = FHandlers.Count - 1; LIndex >= 0; LIndex--)
			{
				FHandlers[LIndex].Deallocate(Process);
				FHandlers.RemoveAt(LIndex);
			}
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					if (FHandlers != null)
					{
						UnprepareDeferredHandlers();
						FHandlers = null;
					}
				}
				finally
				{
					if (FCatalogConstraints != null)
					{
						FCatalogConstraints.Clear();
						FCatalogConstraints = null;
					}
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		private ServerProcess FProcess;
		public ServerProcess Process { get { return FProcess; } }
		
		private IsolationLevel FIsolationLevel;
		public IsolationLevel IsolationLevel { get { return FIsolationLevel; } }

		private DateTime FStartTime;
		public DateTime StartTime { get { return FStartTime; } }

		private bool FPrepared;
		public bool Prepared 
		{
			get { return FPrepared; } 
			set { FPrepared = value; } 
		}
		
		private bool FInRollback;
		public bool InRollback
		{
			get { return FInRollback; }
			set { FInRollback = value; }
		}
		
		private ServerTableVars FTableVars = new ServerTableVars();
		public ServerTableVars TableVars { get { return FTableVars; } }

		public void InvokeDeferredHandlers(ServerProcess AProcess)
		{
			if (FHandlers.Count > 0)
			{
				AProcess.InternalBeginTransaction(IsolationLevel.Isolated);
				try
				{
					foreach (ServerHandler LHandler in FHandlers)
						LHandler.Invoke(AProcess);
					AProcess.InternalCommitTransaction();
				}
				catch (Exception LException)
				{
					if (AProcess.InTransaction)
					{
						try
						{
							AProcess.InternalRollbackTransaction();
						}
						catch (Exception LRollbackException)
						{
							throw new ServerException(ServerException.Codes.RollbackError, LException, LRollbackException.ToString());
						}
					}
					throw LException;
				}
			}
		}
		
		private Schema.CatalogConstraints FCatalogConstraints = new Schema.CatalogConstraints();
		public Schema.CatalogConstraints CatalogConstraints { get { return FCatalogConstraints; } }
		
		public void RemoveCatalogConstraintCheck(Schema.CatalogConstraint AConstraint)
		{
			int LIndex = FCatalogConstraints.IndexOf(AConstraint.Name);
			if (LIndex >= 0)
				FCatalogConstraints.RemoveAt(LIndex);
		}
		
		private ServerHandlers FHandlers = new ServerHandlers();
		
		public void AddInsertHandler(Schema.TableVarEventHandler AHandler, Row ARow)
		{
			FHandlers.Add(new ServerInsertHandler(AHandler, ARow));
		}
		
		public void AddUpdateHandler(Schema.TableVarEventHandler AHandler, Row AOldRow, Row ANewRow)
		{
			FHandlers.Add(new ServerUpdateHandler(AHandler, AOldRow, ANewRow));
		}
		
		public void AddDeleteHandler(Schema.TableVarEventHandler AHandler, Row ARow)
		{
			FHandlers.Add(new ServerDeleteHandler(AHandler, ARow));
		}
		
		public void RemoveDeferredHandlers(Schema.EventHandler AHandler)
		{
			for (int LIndex = FHandlers.Count - 1; LIndex >= 0; LIndex--)
				if (FHandlers[LIndex].Handler.Equals(AHandler))
					FHandlers.RemoveAt(LIndex);
		}
	}
	
	public class ServerTransactions : DisposableTypedList
	{
		public ServerTransactions() : base(typeof(ServerTransaction), true, false){}
		
		public void UnprepareDeferredConstraintChecks()
		{
			if (FTableVars != null)
			{
				Exception LException = null;
				while (FTableVars.Count > 0)
					try
					{
						foreach (Schema.TableVar LTableVar in FTableVars.Keys)
						{
							RemoveDeferredConstraintChecks(LTableVar);
							break;
						}
					}
					catch (Exception E)
					{
						LException = E;
					}
					
				FTableVars = null;
				if (LException != null)
					throw LException;
			}
		}
		
		public new ServerTransaction this[int AIndex]
		{
			get { return (ServerTransaction)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public ServerTransaction BeginTransaction(ServerProcess AProcess, IsolationLevel AIsolationLevel)
		{
			return this[Add(new ServerTransaction(AProcess, AIsolationLevel))];
		}
		
		public void EndTransaction(bool ASuccess)
		{
			// On successful transaction commit, remove constraint checks that have been completed.
			// On failure, constraint checks that were logged by the transaction will be rolled back with the rest of the transaction.
			if (ASuccess)
				foreach (ServerTableVar LTableVar in this[Count - 1].TableVars.Values)
					LTableVar.DeleteCheckTableChecks(Count - 1);
			RemoveAt(Count - 1);
		}
		
		public ServerTransaction CurrentTransaction()
		{
			return this[Count - 1];
		}
		
		public ServerTransaction RootTransaction()
		{
			return this[0];
		}

		public void ValidateDeferredConstraints(ServerProcess AProcess)
		{
			foreach (ServerTableVar LTableVar in this[Count - 1].TableVars.Values)
				LTableVar.Validate(AProcess, Count - 1);
		}
		
		private ServerTableVars FTableVars = new ServerTableVars();
		public ServerTableVars TableVars { get { return FTableVars; } }
		
		private ServerTableVar EnsureServerTableVar(Schema.TableVar ATableVar)
		{
			ServerTableVar LServerTableVar = null;
			ServerTransaction LCurrentTransaction = CurrentTransaction();

			if (!FTableVars.Contains(ATableVar))
			{
				LServerTableVar = new ServerTableVar(LCurrentTransaction.Process, ATableVar);
				FTableVars.Add(ATableVar, LServerTableVar);
			}
			else
				LServerTableVar = FTableVars[ATableVar];

			if (!LCurrentTransaction.TableVars.Contains(ATableVar))
				LCurrentTransaction.TableVars.Add(ATableVar, LServerTableVar);
				
			return LServerTableVar;
		}

		public void AddInsertTableVarCheck(Schema.TableVar ATableVar, Row ARow)
		{
			EnsureServerTableVar(ATableVar).AddInsertTableVarCheck(Count - 1, ARow);
		}
		
		public void AddUpdateTableVarCheck(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			EnsureServerTableVar(ATableVar).AddUpdateTableVarCheck(Count - 1, AOldRow, ANewRow);
		}
		
		public void AddDeleteTableVarCheck(Schema.TableVar ATableVar, Row ARow)
		{
			EnsureServerTableVar(ATableVar).AddDeleteTableVarCheck(Count - 1, ARow);
		}
		
		public void RemoveDeferredConstraintChecks(Schema.TableVar ATableVar)
		{
			if (FTableVars != null)
			{
				ServerTableVar LTableVar = FTableVars[ATableVar];
				if (LTableVar != null)
				{
					foreach (ServerTransaction LTransaction in this)
						if (LTransaction.TableVars.Contains(ATableVar))
							LTransaction.TableVars.Remove(ATableVar);
					FTableVars.Remove(ATableVar);
					LTableVar.Dispose();
				}
			}
		}
		
		public void RemoveDeferredHandlers(Schema.EventHandler AHandler)
		{
			foreach (ServerTransaction LTransaction in this)
				LTransaction.RemoveDeferredHandlers(AHandler);
		}
		
		public void RemoveCatalogConstraintCheck(Schema.CatalogConstraint AConstraint)
		{
			foreach (ServerTransaction LTransaction in this)
				LTransaction.RemoveCatalogConstraintCheck(AConstraint);
		}
	}
	
	public class ServerTableVar : Disposable
	{
		static ServerTableVar()
		{
			FTransactionIndexColumnName = Schema.Object.GetUniqueName();
			FTransitionColumnName = Schema.Object.GetUniqueName();
			FOldRowColumnName = Schema.Object.GetUniqueName();
			FNewRowColumnName = Schema.Object.GetUniqueName();
		}
		
		public ServerTableVar(ServerProcess AProcess, Schema.TableVar ATableVar) : base()
		{
			FProcess = AProcess;
			FTableVar = ATableVar;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				UnprepareCheckTable();
			}
			finally
			{
				FTableVar = null;
				base.Dispose(ADisposing);
			}
		}
		
		protected ServerProcess FProcess;
		public ServerProcess Process { get { return FProcess; } }
		
		protected Schema.TableVar FTableVar;
		public Schema.TableVar TableVar { get { return FTableVar; } }
		
		public override bool Equals(object AObject)
		{
			return (AObject is ServerTableVar) && Schema.Object.NamesEqual(FTableVar.Name, ((ServerTableVar)AObject).TableVar.Name);
		}

		public override int GetHashCode()
		{
			return FTableVar.GetHashCode();
		}

		private string FCheckTableName;
		private static string FTransactionIndexColumnName;
		private static string FTransitionColumnName;
		private static string FOldRowColumnName;
		private static string FNewRowColumnName;
		private Schema.TableVar FCheckTable;
		private Schema.Key FCheckTableKey;
		private Schema.RowType FCheckRowType;

		private Schema.IRowType FNewRowType;
		public Schema.IRowType NewRowType { get { return FNewRowType; } }

		private Schema.IRowType FOldRowType;
		public Schema.IRowType OldRowType { get { return FOldRowType; } }

		private IServerExpressionPlan FPlan;
		private IServerExpressionPlan FTransactionPlan;
		private DataParams FTransactionParams;
		private DataParam FTransactionParam;

		private void CreateCheckTable()
		{
			FCheckTableName = Schema.Object.GetUniqueName();

			/*
				create session table <check table name> in System.Temp
				{ 
					<key columns>, 
					<transaction index column name> : Integer,
					<transition column name> : String static tags { DAE.StaticByteSize = '16' }, 
					<old row column name> : row { nil }, 
					<new row column name> : row { nil }, 
					key { <key column names> },
					order { <transaction index column name>, <key columns> }
				};
			*/

			StringBuilder LBuilder = new StringBuilder();
			LBuilder.AppendFormat("{0} {1} {2} {3} {4} {5} {{ ", Keywords.Create, Keywords.Session, Keywords.Table, FCheckTableName, Keywords.In, Process.ServerSession.Server.TempDevice.Name);
			bool LHasColumns = false;
			Schema.Key LKey = FTableVar.Keys.MinimumKey(false, false);
			for (int LIndex = 0; LIndex < LKey.Columns.Count; LIndex++)
			{
				if (LHasColumns)
					LBuilder.AppendFormat("{0} ", Keywords.ListSeparator);
				else
					LHasColumns = true;
				LBuilder.AppendFormat("{0} {1} {2}", LKey.Columns[LIndex].Name, Keywords.TypeSpecifier, LKey.Columns[LIndex].Column.DataType.Name);
			}
			if (LHasColumns)
				LBuilder.AppendFormat("{0} ", Keywords.ListSeparator);
			LBuilder.AppendFormat("{0} : Integer, ", FTransactionIndexColumnName);
			LBuilder.AppendFormat("{0} : String static tags {{ DAE.StaticByteSize = '16' }}, ", FTransitionColumnName);
			LBuilder.AppendFormat("{0} : generic row {{ nil }}, ", FOldRowColumnName);
			LBuilder.AppendFormat("{0} : generic row {{ nil }}, key {{ ", FNewRowColumnName);
			for (int LIndex = 0; LIndex < LKey.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LBuilder.AppendFormat("{0} ", Keywords.ListSeparator);
				LBuilder.Append(LKey.Columns[LIndex].Name);
			}
			LBuilder.Append(" }, order { ");
			LBuilder.Append(FTransactionIndexColumnName);
			for (int LIndex = 0; LIndex < LKey.Columns.Count; LIndex++)
				LBuilder.AppendFormat("{0} {1}", Keywords.ListSeparator, LKey.Columns[LIndex].Name);
			LBuilder.Append("} };");

			ApplicationTransaction LTransaction = null;
			if (Process.ApplicationTransactionID != Guid.Empty)
				LTransaction = Process.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushGlobalContext();
				try
				{
					bool LSaveUsePlanCache = Process.ServerSession.SessionInfo.UsePlanCache;
					Process.ServerSession.SessionInfo.UsePlanCache = false;
					try
					{
						// Push a loading context to prevent the DDL from begin logged.
						Process.PushLoadingContext(new LoadingContext(Process.ServerSession.User, true));
						try
						{
							IServerStatementPlan LPlan = ((IServerProcess)Process).PrepareStatement(LBuilder.ToString(), null);
							try
							{
								LPlan.Execute(null);
							}
							finally
							{
								((IServerProcess)Process).UnprepareStatement(LPlan);
							}
						}
						finally
						{
							Process.PopLoadingContext();
						}
					}
					finally
					{
						Process.ServerSession.SessionInfo.UsePlanCache = LSaveUsePlanCache;
					}
				}
				finally
				{
					if (LTransaction != null)
						LTransaction.PopGlobalContext();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
			
			FCheckTable = Process.Plan.Catalog[((Schema.SessionObject)Process.ServerSession.SessionObjects[FCheckTableName]).GlobalName] as Schema.TableVar;
			FCheckTableKey = FCheckTable.Keys.MinimumKey(true);
			FCheckRowType = new Schema.RowType(FCheckTable.DataType.Columns);
		}
		
		private void CopyKeyValues(Row ASourceRow, Row ATargetRow)
		{
			int LColumnIndex;
			for (int LIndex = 0; LIndex < FCheckTableKey.Columns.Count; LIndex++)
			{
				LColumnIndex = ASourceRow.IndexOfColumn(FCheckTableKey.Columns[LIndex].Name);
				if (ASourceRow.HasValue(LColumnIndex))
					ATargetRow[LIndex] = ASourceRow[LColumnIndex];
				else
					ATargetRow.ClearValue(LIndex);
			}
		}

		private void DeleteCheckTableChecks()
		{
			if (FPlan != null)
			{
				IServerCursor LCursor = FPlan.Open(null);
				try
				{
					LCursor.Next();
					Row LRow = FPlan.RequestRow();
					try
					{
						while (!LCursor.EOF())
						{
							LCursor.Select(LRow);
							if (LRow.HasValue(FOldRowColumnName))
								DataValue.DisposeNative(Process, OldRowType, ((NativeRow)LRow.AsNative).Values[LRow.DataType.Columns.IndexOfName(FOldRowColumnName)]);
							if (LRow.HasValue(FNewRowColumnName))
								DataValue.DisposeNative(Process, NewRowType, ((NativeRow)LRow.AsNative).Values[LRow.DataType.Columns.IndexOfName(FNewRowColumnName)]);
							LCursor.Delete();
						}
					}
					finally
					{
						FPlan.ReleaseRow(LRow);
					}
				}
				finally
				{
					FPlan.Close(LCursor);
				}
			}
		}
		
		public void DeleteCheckTableChecks(int ATransactionIndex)
		{
			IServerCursor LCursor = FTransactionPlan.Open(GetTransactionParams(ATransactionIndex));
			try
			{
				LCursor.Next();
				Row LRow = FTransactionPlan.RequestRow();
				try
				{
					while (!LCursor.EOF())
					{
						LCursor.Select(LRow);
						if (LRow.HasValue(FOldRowColumnName))
							DataValue.DisposeNative(Process, OldRowType, ((NativeRow)LRow.AsNative).Values[LRow.DataType.Columns.IndexOfName(FOldRowColumnName)]);
						if (LRow.HasValue(FNewRowColumnName))
							DataValue.DisposeNative(Process, NewRowType, ((NativeRow)LRow.AsNative).Values[LRow.DataType.Columns.IndexOfName(FNewRowColumnName)]);
						LCursor.Delete();
					}
				}
				finally
				{
					FTransactionPlan.ReleaseRow(LRow);
				}
			}
			finally
			{
				FTransactionPlan.Close(LCursor);
			}
		}
		
		private DataParams GetTransactionParams(int ATransactionIndex)
		{
			FTransactionParam.Value = ATransactionIndex;
			return FTransactionParams;
		}
		
		private void OpenCheckTable()
		{
			ApplicationTransaction LTransaction = null;
			if (Process.ApplicationTransactionID != Guid.Empty)
				LTransaction = Process.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushGlobalContext();
				try
				{
					bool LSaveUsePlanCache = Process.ServerSession.SessionInfo.UsePlanCache;
					Process.ServerSession.SessionInfo.UsePlanCache = false;
					try
					{
						Process.Context.PushWindow(0);
						try
						{
							FPlan = ((IServerProcess)Process).PrepareExpression(String.Format("select {0} capabilities {{ navigable, searchable, updateable }} type dynamic;", FCheckTableName), null);
							FTransactionParams = new DataParams();
							FTransactionParam = new DataParam("ATransactionIndex", Process.DataTypes.SystemInteger, Modifier.In, 0);
							FTransactionParams.Add(FTransactionParam);
							FTransactionPlan = ((IServerProcess)Process).PrepareExpression(String.Format("select {0} where {1} = ATransactionIndex capabilities {{ navigable, searchable, updateable }} type dynamic;", FCheckTableName, FTransactionIndexColumnName), FTransactionParams);
						}
						finally
						{
							Process.Context.PopWindow();
						}
					}
					finally
					{
						Process.ServerSession.SessionInfo.UsePlanCache = LSaveUsePlanCache;
					}
				}
				finally
				{
					if (LTransaction != null)
						LTransaction.PopGlobalContext();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
		}
		
		private void CloseCheckTable()
		{
			if (FPlan != null)
			{
				((IServerProcess)Process).UnprepareExpression(FPlan);
				FPlan = null;
			}
			
			if (FTransactionPlan != null)
			{
				((IServerProcess)Process).UnprepareExpression(FTransactionPlan);
				FTransactionPlan = null;
			}
		}
		
		private void DropCheckTable()
		{
			if (FCheckTable != null)
			{
				ApplicationTransaction LTransaction = null;
				if (Process.ApplicationTransactionID != Guid.Empty)
					LTransaction = Process.GetApplicationTransaction();
				try
				{
					if (LTransaction != null)
						LTransaction.PushGlobalContext();
					try
					{
						ServerStatementPlan LPlan = new ServerStatementPlan(Process);
						try
						{
							LPlan.Plan.EnterTimeStampSafeContext();
							try
							{
								// Push a loading context to prevent the DDL from being logged.
								Process.PushLoadingContext(new LoadingContext(Process.ServerSession.User, true));
								try
								{
									Process.PushExecutingPlan(LPlan);
									try
									{
										PlanNode LPlanNode = Compiler.BindNode(LPlan.Plan, Compiler.CompileStatement(LPlan.Plan, new DropTableStatement(FCheckTableName)));
										LPlan.Plan.CheckCompiled();
										LPlanNode.Execute(Process);
									}
									finally
									{
										Process.PopExecutingPlan(LPlan);
									}
								}
								finally
								{
									Process.PopLoadingContext();
								}
							}
							finally
							{
								LPlan.Plan.ExitTimeStampSafeContext();
							}
						}
						finally
						{
							LPlan.Dispose();
						}
					}
					finally
					{
						if (LTransaction != null)
							LTransaction.PopGlobalContext();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
		}
		
		private void PrepareCheckTable()
		{
			if (FCheckTable == null)
			{
				try
				{
					CreateCheckTable();
					OpenCheckTable();
				}
				catch (Exception E)
				{
					DataphorException LDataphorException = E as DataphorException;
					throw new ServerException(ServerException.Codes.CouldNotCreateCheckTable, LDataphorException != null ? LDataphorException.Severity : ErrorSeverity.System, E, FTableVar == null ? "<unknown>" : FTableVar.DisplayName);
				}
			}
		}
		
		private void UnprepareCheckTable()
		{
			DeleteCheckTableChecks();
			CloseCheckTable();
			DropCheckTable();
		}
		
		public void AddInsertTableVarCheck(int ATransactionIndex, Row ARow)
		{
			PrepareCheckTable();
			if (FNewRowType == null)
				FNewRowType = ARow.DataType;

			// Log ARow as an insert transition.
			Row LCheckRow = FPlan.RequestRow();
			try
			{
				IServerCursor LCursor = FPlan.Open(null);
				try
				{
					CopyKeyValues(ARow, LCheckRow);
					LCheckRow[FTransactionIndexColumnName] = ATransactionIndex;
					LCheckRow[FTransitionColumnName] = Keywords.Insert;
					LCheckRow[FNewRowColumnName] = ARow;

					if (LCursor.FindKey(LCheckRow))
						LCursor.Delete();

					LCursor.Insert(LCheckRow);
				}
				finally
				{
					FPlan.Close(LCursor);
				}
			}
			finally
			{
				FPlan.ReleaseRow(LCheckRow);
			}
		}
		
		public void AddUpdateTableVarCheck(int ATransactionIndex, Row AOldRow, Row ANewRow)
		{
			PrepareCheckTable();
			if (FOldRowType == null)
				FOldRowType = AOldRow.DataType;
			if (FNewRowType == null)
				FNewRowType = ANewRow.DataType;
			IServerCursor LCursor = FPlan.Open(null);
			try
			{
				// If there is an existing insert transition check for AOldRow, delete it and log ANewRow as an insert transition.
				// If there is an existing update transition check for AOldRow, delete it and log AOldRow from the existing check and ANewRow from the new check as an update transition.
				// Else log AOldRow, ANewRow as an update transition.
				Row LCheckRow = FPlan.RequestRow();
				try
				{
					// delete <check table name> where <key names> = <old key values>;
					CopyKeyValues(AOldRow, LCheckRow);
					if (LCursor.FindKey(LCheckRow))
					{
						using (Row LRow = LCursor.Select())
						{
							if (LRow.HasValue(FOldRowColumnName))
								AOldRow = (Row)LRow[FOldRowColumnName];
							else
								AOldRow = null;
						}
						LCursor.Delete();
					}

					CopyKeyValues(ANewRow, LCheckRow);
					LCheckRow[FTransactionIndexColumnName] = ATransactionIndex;
					if (AOldRow != null)
					{
						LCheckRow[FTransitionColumnName] = Keywords.Update;
						LCheckRow[FOldRowColumnName] = AOldRow;
					}
					else
						LCheckRow[FTransitionColumnName] = Keywords.Insert;

					LCheckRow[FNewRowColumnName] = ANewRow;
					LCursor.Insert(LCheckRow);
				}
				finally
				{
					FPlan.ReleaseRow(LCheckRow);
				}
			}
			finally
			{
				FPlan.Close(LCursor);
			}
		}

		public void AddDeleteTableVarCheck(int ATransactionIndex, Row ARow)
		{
			PrepareCheckTable();
			if (FOldRowType == null)
				FOldRowType = ARow.DataType;
			IServerCursor LCursor = FPlan.Open(null);
			try
			{
				// If there is an existing insert transition for ARow, delete it and don't log anything.
				// If there is an existing update transition for ARow, delete it and log AOldRow from the update transition as a delete transition.
				// Else log ARow as a delete transition.
				Row LCheckRow = FPlan.RequestRow();
				try
				{
					// delete <check table name> where <key names> = <key values>;
					CopyKeyValues(ARow, LCheckRow);
					if (LCursor.FindKey(LCheckRow))
					{
						using (Row LRow = LCursor.Select())
						{
							if (LRow.HasValue(FOldRowColumnName))
								ARow = (Row)LRow[FOldRowColumnName];
							else
								ARow = null;
						}
						LCursor.Delete();
					}

					if (ARow != null)
					{
						LCheckRow[FTransactionIndexColumnName] = ATransactionIndex;
						LCheckRow[FTransitionColumnName] = Keywords.Delete;
						LCheckRow[FOldRowColumnName] = ARow;
						LCursor.Insert(LCheckRow);
					}
				}
				finally
				{
					FPlan.ReleaseRow(LCheckRow);
				}
			}
			finally
			{
				FPlan.Close(LCursor);
			}
		}
		
		public void Validate(ServerProcess AProcess, int ATransactionIndex)
		{
			// cursor through each check and validate all constraints for each check
			IServerCursor LCursor = FTransactionPlan.Open(GetTransactionParams(ATransactionIndex));
			try
			{
				Row LCheckRow = FTransactionPlan.RequestRow();
				try
				{
					while (LCursor.Next())
					{
						LCursor.Select(LCheckRow);
						switch ((string)LCheckRow[FTransitionColumnName])
						{
							case Keywords.Insert :
							{
								Row LRow = (Row)LCheckRow[FNewRowColumnName];
								try
								{
									AProcess.Context.Push(LRow);
									try
									{
										ValidateCheck(Schema.Transition.Insert);
									}
									finally
									{
										AProcess.Context.Pop();
									}
								}
								finally
								{
									LRow.Dispose();
								}
							}
							break;
							
							case Keywords.Update :
							{
								Row LOldRow = (Row)LCheckRow[FOldRowColumnName];
								try
								{
									AProcess.Context.Push(LOldRow);
									try
									{
										Row LNewRow = (Row)LCheckRow[FNewRowColumnName];
										try
										{
											AProcess.Context.Push(LNewRow);
											try
											{
												ValidateCheck(Schema.Transition.Update);
											}
											finally
											{
												AProcess.Context.Pop();
											}
										}
										finally
										{
											LNewRow.Dispose();
										}
									}
									finally
									{
										AProcess.Context.Pop();
									}
								}
								finally
								{
									LOldRow.Dispose();
								}
							}
							break;
							
							case Keywords.Delete :
							{
								Row LRow = (Row)LCheckRow[FOldRowColumnName];
								try
								{
									AProcess.Context.Push(LRow);
									try
									{
										ValidateCheck(Schema.Transition.Delete);
									}
									finally
									{
										AProcess.Context.Pop();
									}
								}
								finally
								{
									LRow.Dispose();
								}
							}
							break;
						}
					}
				}
				finally
				{
					FTransactionPlan.ReleaseRow(LCheckRow);
				}
			}
			finally
			{
				FTransactionPlan.Close(LCursor);
			}
		}
		
		public void ValidateCheck(Schema.Transition ATransition)
		{
			switch (ATransition)
			{
				case Schema.Transition.Insert :
				{
					Row LNewRow = new Row(Process, this.TableVar.DataType.RowType, (NativeRow)((Row)Process.Context.Peek(0)).AsNative);
					try
					{
						Process.Context.Push(LNewRow);
						try
						{
							foreach (Schema.RowConstraint LConstraint in FTableVar.RowConstraints)
								if (LConstraint.Enforced && LConstraint.IsDeferred)
									LConstraint.Validate(Process, ATransition);
						}
						finally
						{
							Process.Context.Pop();
						}
					}
					finally
					{
						LNewRow.Dispose();
					}

					foreach (Schema.TransitionConstraint LConstraint in FTableVar.InsertConstraints)
						if (LConstraint.Enforced && LConstraint.IsDeferred)
							LConstraint.Validate(Process, Schema.Transition.Insert);
				}
				break;

				case Schema.Transition.Update :
				{
					Row LNewRow = new Row(Process, this.TableVar.DataType.RowType, (NativeRow)((Row)Process.Context.Peek(0)).AsNative);
					try
					{
						Process.Context.Push(LNewRow);
						try
						{
							foreach (Schema.RowConstraint LConstraint in FTableVar.RowConstraints)
								if (LConstraint.Enforced && LConstraint.IsDeferred)
									LConstraint.Validate(Process, Schema.Transition.Update);
						}
						finally
						{
							Process.Context.Pop();
						}
					}
					finally
					{
						LNewRow.Dispose();
					}
							
					foreach (Schema.TransitionConstraint LConstraint in FTableVar.UpdateConstraints)
						if (LConstraint.Enforced && LConstraint.IsDeferred)
							LConstraint.Validate(Process, Schema.Transition.Update);
				}
				break;

				case Schema.Transition.Delete :
					foreach (Schema.TransitionConstraint LConstraint in FTableVar.DeleteConstraints)
						if (LConstraint.Enforced && LConstraint.IsDeferred)
							LConstraint.Validate(Process, Schema.Transition.Delete);
				break;
			}
		}
	}
	
	public class ServerTableVars : Hashtable
	{
		public ServerTableVar this[Schema.TableVar ATableVar] { get { return (ServerTableVar)base[ATableVar]; } }
	}
	
	public abstract class ServerHandler : System.Object
	{
		public ServerHandler(Schema.TableVarEventHandler AHandler) : base() 
		{
			FHandler = AHandler;
		}
		
		protected Schema.IRowType FNewRowType;
		protected Schema.IRowType FOldRowType;
		
		protected Schema.TableVarEventHandler FHandler;
		public Schema.TableVarEventHandler Handler { get { return FHandler; } }
		
		public abstract void Invoke(ServerProcess AProcess);
		
		public abstract void Deallocate(ServerProcess AProcess);
	}
	
	public class ServerHandlers : List<ServerHandler>
	{
		public void Deallocate(ServerProcess AProcess)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				this[LIndex].Deallocate(AProcess);
		}
	}
	
	public class ServerInsertHandler : ServerHandler
	{
		public ServerInsertHandler(Schema.TableVarEventHandler AHandler, Row ARow) : base(AHandler)
		{
			FNewRowType = ARow.DataType;
			NativeRow = (NativeRow)ARow.CopyNative();
		}
		
		public NativeRow NativeRow;
		
		public override void Invoke(ServerProcess AProcess)
		{
			Row LRow = new Row(AProcess, FNewRowType, NativeRow);
			try
			{
				AProcess.Context.Push(LRow);
				try
				{
					FHandler.PlanNode.Execute(AProcess);
				}
				finally
				{
					AProcess.Context.Pop();
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		public override void Deallocate(ServerProcess AProcess)
		{
			if (NativeRow != null)
			{
				DataValue.DisposeNative(AProcess, FNewRowType, NativeRow);
				NativeRow = null;
			}
		}
	}
	
	public class ServerUpdateHandler : ServerHandler
	{
		public ServerUpdateHandler(Schema.TableVarEventHandler AHandler, Row AOldRow, Row ANewRow) : base(AHandler)
		{
			FNewRowType = ANewRow.DataType;
			FOldRowType = AOldRow.DataType;
			OldNativeRow = (NativeRow)AOldRow.CopyNative();
			NewNativeRow = (NativeRow)ANewRow.CopyNative();
		}
		
		public NativeRow OldNativeRow;
		public NativeRow NewNativeRow;

		public override void Invoke(ServerProcess AProcess)
		{
			Row LOldRow = new Row(AProcess, FOldRowType, OldNativeRow);
			try
			{
				Row LNewRow = new Row(AProcess, FNewRowType, NewNativeRow);
				try
				{
					AProcess.Context.Push(LOldRow);
					try
					{
						AProcess.Context.Push(LNewRow);
						try
						{
							FHandler.PlanNode.Execute(AProcess);
						}
						finally
						{
							AProcess.Context.Pop();
						}
					}
					finally
					{
						AProcess.Context.Pop();
					}
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
		}
		
		public override void Deallocate(ServerProcess AProcess)
		{
			if (OldNativeRow != null)
			{
				DataValue.DisposeNative(AProcess, FOldRowType, OldNativeRow);
				OldNativeRow = null;
			}

			if (NewNativeRow != null)
			{
				DataValue.DisposeNative(AProcess, FNewRowType, NewNativeRow);
				NewNativeRow = null;
			}
		}
	}
	
	public class ServerDeleteHandler : ServerHandler
	{
		public ServerDeleteHandler(Schema.TableVarEventHandler AHandler, Row ARow) : base(AHandler)
		{
			FOldRowType = ARow.DataType;
			NativeRow = (NativeRow)ARow.CopyNative();
		}
		
		public NativeRow NativeRow;

		public override void Invoke(ServerProcess AProcess)
		{
			Row LRow = new Row(AProcess, FOldRowType, NativeRow);
			try
			{
				AProcess.Context.Push(LRow);
				try
				{
					FHandler.PlanNode.Execute(AProcess);
				}
				finally
				{
					AProcess.Context.Pop();
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		public override void Deallocate(ServerProcess AProcess)
		{
			if (NativeRow != null)
			{
				DataValue.DisposeNative(AProcess, FOldRowType, NativeRow);
				NativeRow = null;
			}
		}
	}
}
