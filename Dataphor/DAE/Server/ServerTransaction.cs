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

namespace Alphora.Dataphor.DAE.Server
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;

	public class ServerTransaction : Disposable
	{
		public ServerTransaction(ServerProcess process, IsolationLevel isolationLevel) : base()
		{
			_process = process;
			_isolationLevel = isolationLevel;
			_startTime = DateTime.Now;
		}
		
		protected void UnprepareDeferredHandlers()
		{
			Program program = new Program(Process);
			program.Start(null);
			try
			{
				for (int index = _handlers.Count - 1; index >= 0; index--)
				{
					_handlers[index].Deallocate(program);
					_handlers.RemoveAt(index);
				}
			}
			finally
			{
				program.Stop(null);
			}
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					if (_handlers != null)
					{
						UnprepareDeferredHandlers();
						_handlers = null;
					}
				}
				finally
				{
					if (_catalogConstraints != null)
					{
						_catalogConstraints.Clear();
						_catalogConstraints = null;
					}
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		private ServerProcess _process;
		public ServerProcess Process { get { return _process; } }
		
		private IsolationLevel _isolationLevel;
		public IsolationLevel IsolationLevel { get { return _isolationLevel; } }

		private DateTime _startTime;
		public DateTime StartTime { get { return _startTime; } }

		private bool _prepared;
		public bool Prepared 
		{
			get { return _prepared; } 
			set { _prepared = value; } 
		}
		
		private bool _inRollback;
		public bool InRollback
		{
			get { return _inRollback; }
			set { _inRollback = value; }
		}
		
		private ServerTableVars _tableVars = new ServerTableVars();
		public ServerTableVars TableVars { get { return _tableVars; } }

		public void InvokeDeferredHandlers(Program program)
		{
			if (_handlers.Count > 0)
			{
				program.ServerProcess.InternalBeginTransaction(IsolationLevel.Isolated);
				try
				{
					foreach (ServerHandler handler in _handlers)
						handler.Invoke(program);
					program.ServerProcess.InternalCommitTransaction();
				}
				catch (Exception exception)
				{
					if (program.ServerProcess.InTransaction)
					{
						try
						{
							program.ServerProcess.InternalRollbackTransaction();
						}
						catch (Exception rollbackException)
						{
							throw new ServerException(ServerException.Codes.RollbackError, exception, rollbackException.ToString());
						}
					}
					throw exception;
				}
			}
		}
		
		private Schema.CatalogConstraints _catalogConstraints = new Schema.CatalogConstraints();
		public Schema.CatalogConstraints CatalogConstraints { get { return _catalogConstraints; } }
		
		public void RemoveCatalogConstraintCheck(Schema.CatalogConstraint constraint)
		{
			int index = _catalogConstraints.IndexOf(constraint.Name);
			if (index >= 0)
				_catalogConstraints.RemoveAt(index);
		}
		
		private ServerHandlers _handlers = new ServerHandlers();
		
		public void AddInsertHandler(Schema.TableVarEventHandler handler, IRow row)
		{
			_handlers.Add(new ServerInsertHandler(handler, row));
		}
		
		public void AddUpdateHandler(Schema.TableVarEventHandler handler, IRow oldRow, IRow newRow)
		{
			_handlers.Add(new ServerUpdateHandler(handler, oldRow, newRow));
		}
		
		public void AddDeleteHandler(Schema.TableVarEventHandler handler, IRow row)
		{
			_handlers.Add(new ServerDeleteHandler(handler, row));
		}
		
		public void RemoveDeferredHandlers(Schema.EventHandler handler)
		{
			for (int index = _handlers.Count - 1; index >= 0; index--)
				if (_handlers[index].Handler.Equals(handler))
					_handlers.RemoveAt(index);
		}
	}

	#if USETYPEDLIST	
	public class ServerTransactions : DisposableTypedList
	{
		public ServerTransactions() : base(typeof(ServerTransaction), true, false){}
		
		public new ServerTransaction this[int AIndex]
		{
			get { return (ServerTransaction)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
	#else
	public class ServerTransactions : DisposableList<ServerTransaction>
	{
	#endif
		public ServerTransaction BeginTransaction(ServerProcess process, IsolationLevel isolationLevel)
		{
			return this[Add(new ServerTransaction(process, isolationLevel))];
		}
		
		public void EndTransaction(bool success)
		{
			// On successful transaction commit, remove constraint checks that have been completed.
			// On failure, constraint checks that were logged by the transaction will be rolled back with the rest of the transaction.
			if (success)
				foreach (ServerTableVar tableVar in this[Count - 1].TableVars.Values)
					tableVar.DeleteCheckTableChecks(Count - 1);
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

		public void ValidateDeferredConstraints(Program program)
		{
			foreach (ServerTableVar tableVar in this[Count - 1].TableVars.Values)
				tableVar.Validate(program, Count - 1);
		}
		
		public void UnprepareDeferredConstraintChecks()
		{
			if (_tableVars != null)
			{
				Exception exception = null;
				while (_tableVars.Count > 0)
					try
					{
						foreach (Schema.TableVar tableVar in _tableVars.Keys)
						{
							RemoveDeferredConstraintChecks(tableVar);
							break;
						}
					}
					catch (Exception E)
					{
						exception = E;
					}
					
				_tableVars = null;
				if (exception != null)
					throw exception;
			}
		}
		
		private ServerTableVars _tableVars = new ServerTableVars();
		public ServerTableVars TableVars { get { return _tableVars; } }
		
		private ServerTableVar EnsureServerTableVar(Schema.TableVar tableVar)
		{
			ServerTableVar serverTableVar = null;
			ServerTransaction currentTransaction = CurrentTransaction();

			if (!_tableVars.ContainsKey(tableVar))
			{
				serverTableVar = new ServerTableVar(currentTransaction.Process, tableVar);
				_tableVars.Add(tableVar, serverTableVar);
			}
			else
				serverTableVar = _tableVars[tableVar];

			if (!currentTransaction.TableVars.ContainsKey(tableVar))
				currentTransaction.TableVars.Add(tableVar, serverTableVar);
				
			return serverTableVar;
		}

		public void AddInsertTableVarCheck(Schema.TableVar tableVar, IRow row)
		{
			EnsureServerTableVar(tableVar).AddInsertTableVarCheck(Count - 1, row);
		}
		
		public void AddUpdateTableVarCheck(Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			EnsureServerTableVar(tableVar).AddUpdateTableVarCheck(Count - 1, oldRow, newRow, valueFlags);
		}
		
		public void AddDeleteTableVarCheck(Schema.TableVar tableVar, IRow row)
		{
			EnsureServerTableVar(tableVar).AddDeleteTableVarCheck(Count - 1, row);
		}
		
		public void RemoveDeferredConstraintChecks(Schema.TableVar tableVar)
		{
			if (_tableVars != null)
			{
				ServerTableVar localTableVar;
				if (_tableVars.TryGetValue(tableVar, out localTableVar))
				{
					foreach (ServerTransaction transaction in this)
						if (transaction.TableVars.ContainsKey(tableVar))
							transaction.TableVars.Remove(tableVar);
					_tableVars.Remove(tableVar);
					localTableVar.Dispose();
				}
			}
		}
		
		public void RemoveDeferredHandlers(Schema.EventHandler handler)
		{
			foreach (ServerTransaction transaction in this)
				transaction.RemoveDeferredHandlers(handler);
		}
		
		public void RemoveCatalogConstraintCheck(Schema.CatalogConstraint constraint)
		{
			foreach (ServerTransaction transaction in this)
				transaction.RemoveCatalogConstraintCheck(constraint);
		}
	}

	public class ServerCheckTable : Disposable
	{
		static ServerCheckTable()
		{
			_transactionIndexColumnName = Schema.Object.GetUniqueName();
			_transitionColumnName = Schema.Object.GetUniqueName();
			_oldRowColumnName = Schema.Object.GetUniqueName();
			_newRowColumnName = Schema.Object.GetUniqueName();
		}
		
		public ServerCheckTable
		(
			ServerProcess process, 
			Schema.TableVar tableVar, 
			Schema.Key checkKey, 
			Schema.Constraints insertConstraints, 
			Schema.Constraints updateConstraints, 
			Schema.Constraints deleteConstraints
		) : base()
		{
			_process = process;
			_tableVar = tableVar;
			_checkKey = checkKey;
			_insertConstraints = insertConstraints;
			_updateConstraints = updateConstraints;
			_deleteConstraints = deleteConstraints;
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				UnprepareCheckTable();
			}
			finally
			{
				_tableVar = null;
				_checkKey = null;
				_insertConstraints = null;
				_updateConstraints = null;
				_deleteConstraints = null;
				base.Dispose(disposing);
				_process = null;
			}
		}

		protected ServerProcess _process;
		public ServerProcess Process { get { return _process; } }
		
		protected Schema.TableVar _tableVar;
		public Schema.TableVar TableVar { get { return _tableVar; } }

		protected Schema.Key _checkKey;
		public Schema.Key CheckKey { get { return _checkKey; } }

		protected Schema.Constraints _insertConstraints;
		public Schema.Constraints InsertConstraints { get { return _insertConstraints; } }

		protected Schema.Constraints _updateConstraints;
		public Schema.Constraints UpdateConstraints { get { return _updateConstraints; } }

		protected Schema.Constraints _deleteConstraints;
		public Schema.Constraints DeleteConstraints { get { return _deleteConstraints; } }

		private string _checkTableName;
		private static string _transactionIndexColumnName;
		private static string _transitionColumnName;
		private static string _oldRowColumnName;
		private static string _newRowColumnName;
		private Schema.TableVar _checkTable;
		private Schema.Key _checkTableKey;
		private Schema.RowType _checkRowType;
		#if INCLUDEKEYVALUECOMPARISON
		private Program _checkKeyEqualityProgram;
		#endif

		private Schema.IRowType _newRowType;
		public Schema.IRowType NewRowType { get { return _newRowType; } }

		private Schema.IRowType _oldRowType;
		public Schema.IRowType OldRowType { get { return _oldRowType; } }

		private IServerExpressionPlan _plan;
		private IServerExpressionPlan _transactionPlan;
		private DataParams _transactionParams;
		private DataParam _transactionParam;

		private void CreateCheckTable()
		{
			_checkTableName = Schema.Object.GetUniqueName();

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

			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("{0} {1} {2} {3} {4} {5} {{ ", Keywords.Create, Keywords.Session, Keywords.Table, _checkTableName, Keywords.In, Process.ServerSession.Server.TempDevice.Name);
			bool hasColumns = false;
			Schema.Key key = _checkKey;
			for (int index = 0; index < key.Columns.Count; index++)
			{
				if (hasColumns)
					builder.AppendFormat("{0} ", Keywords.ListSeparator);
				else
					hasColumns = true;
				builder.AppendFormat("{0} {1} {2}", key.Columns[index].Name, Keywords.TypeSpecifier, key.Columns[index].Column.DataType.Name);
			}
			if (hasColumns)
				builder.AppendFormat("{0} ", Keywords.ListSeparator);
			builder.AppendFormat("{0} : Integer, ", _transactionIndexColumnName);
			builder.AppendFormat("{0} : String static tags {{ DAE.StaticByteSize = '16' }}, ", _transitionColumnName);
			builder.AppendFormat("{0} : generic row {{ nil }}, ", _oldRowColumnName);
			builder.AppendFormat("{0} : generic row {{ nil }}, key {{ ", _newRowColumnName);
			for (int index = 0; index < key.Columns.Count; index++)
			{
				if (index > 0)
					builder.AppendFormat("{0} ", Keywords.ListSeparator);
				builder.Append(key.Columns[index].Name);
			}
			builder.Append(" }, order { ");
			builder.Append(_transactionIndexColumnName);
			for (int index = 0; index < key.Columns.Count; index++)
				builder.AppendFormat("{0} {1}", Keywords.ListSeparator, key.Columns[index].Name);
			builder.Append("} };");

			Process.PushGlobalContext();
			try
			{
				bool saveUsePlanCache = Process.ServerSession.SessionInfo.UsePlanCache;
				Process.ServerSession.SessionInfo.UsePlanCache = false;
				try
				{
					// Push a loading context to prevent the DDL from being logged.
					Process.PushLoadingContext(new LoadingContext(Process.ServerSession.User, true));
					try
					{
						IServerStatementPlan plan = ((IServerProcess)Process).PrepareStatement(builder.ToString(), null);
						try
						{
							plan.Execute(null);
						}
						finally
						{
							((IServerProcess)Process).UnprepareStatement(plan);
						}
					}
					finally
					{
						Process.PopLoadingContext();
					}
				}
				finally
				{
					Process.ServerSession.SessionInfo.UsePlanCache = saveUsePlanCache;
				}
			}
			finally
			{
				Process.PopGlobalContext();
			}
			
			Schema.SessionObject sessionObject = (Schema.SessionObject)Process.ServerSession.SessionObjects[_checkTableName];
			sessionObject.IsGenerated = true;
			_checkTable = Process.Catalog[sessionObject.GlobalName] as Schema.TableVar;
			_checkTable.IsGenerated = true;
			_checkTableKey = _checkTable.Keys.MinimumKey(true);
			_checkRowType = new Schema.RowType(_checkTable.DataType.Columns);
			#if INCLUDEKEYVALUECOMPARISON
			_checkKeyEqualityProgram = CompileCheckTableKeyEqualityProgram();
			#endif
		}
		
		private Program CompileCheckTableKeyEqualityProgram()
		{
			var plan = new Plan(Process);
			try
			{
				plan.Symbols.Push(new Symbol(Keywords.Old, _checkRowType));
				try
				{
					plan.Symbols.Push(new Symbol(Keywords.New, _checkRowType));
					try
					{
						var equalNode =
							Compiler.CompileExpression
							(
								plan, 
								Compiler.BuildKeyEqualExpression
								(
									plan, 
									Keywords.Old, 
									Keywords.New, 
									_checkTableKey.Columns, 
									_checkTableKey.Columns
								)
							);
						plan.CheckCompiled();
						var program = new Program(Process);
						program.Code = equalNode;
						return program;
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.Dispose();
			}
		}

		private void CopyKeyValues(IRow sourceRow, IRow targetRow)
		{
			int columnIndex;
			for (int index = 0; index < _checkTableKey.Columns.Count; index++)
			{
				columnIndex = sourceRow.IndexOfColumn(_checkTableKey.Columns[index].Name);
				if (sourceRow.HasValue(columnIndex))
					targetRow[index] = sourceRow[columnIndex];
				else
					targetRow.ClearValue(index);
			}
		}

		private void DeleteCheckTableChecks()
		{
			if (_plan != null)
			{
				IServerCursor cursor = _plan.Open(null);
				try
				{
					cursor.Next();
					IRow row = _plan.RequestRow();
					try
					{
						while (!cursor.EOF())
						{
							cursor.Select(row);
							if (row.HasValue(_oldRowColumnName))
								DataValue.DisposeNative(Process.ValueManager, OldRowType, ((NativeRow)row.AsNative).Values[row.DataType.Columns.IndexOfName(_oldRowColumnName)]);
							if (row.HasValue(_newRowColumnName))
								DataValue.DisposeNative(Process.ValueManager, NewRowType, ((NativeRow)row.AsNative).Values[row.DataType.Columns.IndexOfName(_newRowColumnName)]);
							cursor.Delete();
						}
					}
					finally
					{
						_plan.ReleaseRow(row);
					}
				}
				finally
				{
					_plan.Close(cursor);
				}
			}
		}
		
		public void DeleteCheckTableChecks(int transactionIndex)
		{
			IServerCursor cursor = _transactionPlan.Open(GetTransactionParams(transactionIndex));
			try
			{
				cursor.Next();
				IRow row = _transactionPlan.RequestRow();
				try
				{
					while (!cursor.EOF())
					{
						cursor.Select(row);
						if (row.HasValue(_oldRowColumnName))
							DataValue.DisposeNative(Process.ValueManager, OldRowType, ((NativeRow)row.AsNative).Values[row.DataType.Columns.IndexOfName(_oldRowColumnName)]);
						if (row.HasValue(_newRowColumnName))
							DataValue.DisposeNative(Process.ValueManager, NewRowType, ((NativeRow)row.AsNative).Values[row.DataType.Columns.IndexOfName(_newRowColumnName)]);
						cursor.Delete();
					}
				}
				finally
				{
					_transactionPlan.ReleaseRow(row);
				}
			}
			finally
			{
				_transactionPlan.Close(cursor);
			}
		}
		
		private DataParams GetTransactionParams(int transactionIndex)
		{
			_transactionParam.Value = transactionIndex;
			return _transactionParams;
		}
		
		private void OpenCheckTable()
		{
			Process.PushGlobalContext();
			try
			{
				bool saveUsePlanCache = Process.ServerSession.SessionInfo.UsePlanCache;
				Process.ServerSession.SessionInfo.UsePlanCache = false;
				try
				{
					_plan = ((IServerProcess)Process).PrepareExpression(String.Format("select {0} capabilities {{ navigable, searchable, updateable }} type dynamic;", _checkTableName), null);
					_transactionParams = new DataParams();
					_transactionParam = new DataParam("ATransactionIndex", Process.DataTypes.SystemInteger, Modifier.In, 0);
					_transactionParams.Add(_transactionParam);
					_transactionPlan = ((IServerProcess)Process).PrepareExpression(String.Format("select {0} where {1} = ATransactionIndex capabilities {{ navigable, searchable, updateable }} type dynamic;", _checkTableName, _transactionIndexColumnName), _transactionParams);
				}
				finally
				{
					Process.ServerSession.SessionInfo.UsePlanCache = saveUsePlanCache;
				}
			}
			finally
			{
				Process.PopGlobalContext();
			}
		}
		
		private void CloseCheckTable()
		{
			if (_plan != null)
			{
				((IServerProcess)Process).UnprepareExpression(_plan);
				_plan = null;
			}
			
			if (_transactionPlan != null)
			{
				((IServerProcess)Process).UnprepareExpression(_transactionPlan);
				_transactionPlan = null;
			}
		}
		
		private void DropCheckTable()
		{
			if (_checkTable != null)
			{
				Process.PushGlobalContext();
				try
				{
					Plan plan = new Plan(Process);
					try
					{
						plan.EnterTimeStampSafeContext();
						try
						{
							// Push a loading context to prevent the DDL from being logged.
							Process.PushLoadingContext(new LoadingContext(Process.ServerSession.User, true));
							try
							{
								PlanNode planNode = Compiler.Compile(plan, new DropTableStatement(_checkTableName));
								plan.CheckCompiled();
								Program program = new Program(Process);
								program.Code = planNode;
								program.Execute(null);
							}
							finally
							{
								Process.PopLoadingContext();
							}
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
				finally
				{
					Process.PopGlobalContext();
				}
			}
		}
		
		private void PrepareCheckTable()
		{
			if (_checkTable == null)
			{
				try
				{
					CreateCheckTable();
					OpenCheckTable();
				}
				catch (Exception E)
				{
					DataphorException dataphorException = E as DataphorException;
					throw new ServerException(ServerException.Codes.CouldNotCreateCheckTable, dataphorException != null ? dataphorException.Severity : ErrorSeverity.System, E, _tableVar == null ? "<unknown>" : _tableVar.DisplayName);
				}
			}
		}
		
		private void UnprepareCheckTable()
		{
			DeleteCheckTableChecks();
			CloseCheckTable();
			DropCheckTable();
		}
		
		public void AddInsertTableVarCheck(int transactionIndex, IRow row)
		{
			PrepareCheckTable();
			if (_newRowType == null)
				_newRowType = row.DataType;

			// Log ARow as an insert transition.
			IRow checkRow = _plan.RequestRow();
			try
			{
				IServerCursor cursor = _plan.Open(null);
				try
				{
					CopyKeyValues(row, checkRow);
					checkRow[_transactionIndexColumnName] = transactionIndex;
					checkRow[_transitionColumnName] = Keywords.Insert;
					checkRow[_newRowColumnName] = row;

					if (cursor.FindKey(checkRow))
						cursor.Delete();

					cursor.Insert(checkRow);
				}
				finally
				{
					_plan.Close(cursor);
				}
			}
			finally
			{
				_plan.ReleaseRow(checkRow);
			}
		}
		
		private bool UpdateInvolvesKey(BitArray valueFlags)
		{
			if (valueFlags != null)
			{
				for (int index = 0; index < valueFlags.Count; index++)
					if (valueFlags[index] && _checkTableKey.Columns.ContainsName(_oldRowType.Columns[index].Name))
						return true;
				return false;
			}

			// If we cannot determine if the update involves a key, we must assume that it does.			
			return true;
		}

		#if INCLUDEKEYVALUECOMPARISON
		private bool KeyValuesDifferent(IRow oldRow, IRow newRow)
		{
			_checkKeyEqualityProgram.Stack.PushWindow(0);
			try
			{
				_checkKeyEqualityProgram.Stack.Push(oldRow);
				try
				{
					_checkKeyEqualityProgram.Stack.Push(newRow);
					try
					{
						// NOTE: Cannot call Execute on the program because that pushes a Window,
						// Should probably be using an actual ExpressionPlan from the process, but
						// the problem is that to do that, I have to use named variables to do the
						// compilation, when what I'm passing in the actual execution context is
						// unnamed row variables. The binding should still work, but it's more of
						// a hack than I'm comfortable with.
						return !(bool)_checkKeyEqualityProgram.Code.Execute(_checkKeyEqualityProgram);
					}
					finally
					{
						_checkKeyEqualityProgram.Stack.Pop();
					}
				}
				finally
				{
					_checkKeyEqualityProgram.Stack.Pop();
				}
			}
			finally
			{
				_checkKeyEqualityProgram.Stack.PopWindow();
			}
		}
		#endif
		
		public void AddUpdateTableVarCheck(int transactionIndex, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			PrepareCheckTable();
			if (_oldRowType == null)
				_oldRowType = oldRow.DataType;
			if (_newRowType == null)
				_newRowType = newRow.DataType;

			// If the update involves the key of the check table, log it as a delete of AOldRow, followed by an insert of ANewRow
			#if INCLUDEKEYVALUECOMPARISON
			if (UpdateInvolvesKey(valueFlags) && KeyValuesDifferent(oldRow, newRow))
			#else
			if (UpdateInvolvesKey(valueFlags))
			#endif
			{
				AddDeleteTableVarCheck(transactionIndex, oldRow);
				AddInsertTableVarCheck(transactionIndex, newRow);
			}
			else
			{
				IServerCursor cursor = _plan.Open(null);
				try
				{
					// If there is an existing insert transition check for AOldRow, delete it and log ANewRow as an insert transition.
					// If there is an existing update transition check for AOldRow, delete it and log AOldRow from the existing check and ANewRow from the new check as an update transition.
					// Else log AOldRow, ANewRow as an update transition.
					IRow checkRow = _plan.RequestRow();
					try
					{
						// delete <check table name> where <key names> = <old key values>;
						CopyKeyValues(oldRow, checkRow);
						if (cursor.FindKey(checkRow))
						{
							using (IRow row = cursor.Select())
							{
								if (row.HasValue(_oldRowColumnName))
									oldRow = (IRow)row[_oldRowColumnName];
								else
									oldRow = null;
							}
							cursor.Delete();
						}

						CopyKeyValues(newRow, checkRow);
						checkRow[_transactionIndexColumnName] = transactionIndex;
						if (oldRow != null)
						{
							checkRow[_transitionColumnName] = Keywords.Update;
							checkRow[_oldRowColumnName] = oldRow;
						}
						else
							checkRow[_transitionColumnName] = Keywords.Insert;

						checkRow[_newRowColumnName] = newRow;
						cursor.Insert(checkRow);
					}
					finally
					{
						_plan.ReleaseRow(checkRow);
					}
				}
				finally
				{
					_plan.Close(cursor);
				}
			}
		}

		public void AddDeleteTableVarCheck(int transactionIndex, IRow row)
		{
			PrepareCheckTable();
			if (_oldRowType == null)
				_oldRowType = row.DataType;
			IServerCursor cursor = _plan.Open(null);
			try
			{
				// If there is an existing insert transition for ARow, delete it and don't log anything.
				// If there is an existing update transition for ARow, delete it and log AOldRow from the update transition as a delete transition.
				// Else log ARow as a delete transition.
				IRow checkRow = _plan.RequestRow();
				try
				{
					// delete <check table name> where <key names> = <key values>;
					CopyKeyValues(row, checkRow);
					if (cursor.FindKey(checkRow))
					{
						using (IRow localRow = cursor.Select())
						{
							if (localRow.HasValue(_oldRowColumnName))
								row = (IRow)localRow[_oldRowColumnName];
							else
								row = null;
						}
						cursor.Delete();
					}

					if (row != null)
					{
						checkRow[_transactionIndexColumnName] = transactionIndex;
						checkRow[_transitionColumnName] = Keywords.Delete;
						checkRow[_oldRowColumnName] = row;
						cursor.Insert(checkRow);
					}
				}
				finally
				{
					_plan.ReleaseRow(checkRow);
				}
			}
			finally
			{
				_plan.Close(cursor);
			}
		}
		
		public void Validate(Program program, int transactionIndex)
		{
			// cursor through each check and validate all constraints for each check
			IServerCursor cursor = _transactionPlan.Open(GetTransactionParams(transactionIndex));
			try
			{
				IRow checkRow = _transactionPlan.RequestRow();
				try
				{
					while (cursor.Next())
					{
						cursor.Select(checkRow);
						switch ((string)checkRow[_transitionColumnName])
						{
							case Keywords.Insert :
							{
								IRow row = (IRow)checkRow[_newRowColumnName];
								try
								{
									program.Stack.Push(row);
									try
									{
										ValidateCheck(program, Schema.Transition.Insert);
									}
									finally
									{
										program.Stack.Pop();
									}
								}
								finally
								{
									row.Dispose();
								}
							}
							break;
							
							case Keywords.Update :
							{
								IRow oldRow = (IRow)checkRow[_oldRowColumnName];
								try
								{
									program.Stack.Push(oldRow);
									try
									{
										IRow newRow = (IRow)checkRow[_newRowColumnName];
										try
										{
											program.Stack.Push(newRow);
											try
											{
												ValidateCheck(program, Schema.Transition.Update);
											}
											finally
											{
												program.Stack.Pop();
											}
										}
										finally
										{
											newRow.Dispose();
										}
									}
									finally
									{
										program.Stack.Pop();
									}
								}
								finally
								{
									oldRow.Dispose();
								}
							}
							break;
							
							case Keywords.Delete :
							{
								IRow row = (IRow)checkRow[_oldRowColumnName];
								try
								{
									program.Stack.Push(row);
									try
									{
										ValidateCheck(program, Schema.Transition.Delete);
									}
									finally
									{
										program.Stack.Pop();
									}
								}
								finally
								{
									row.Dispose();
								}
							}
							break;
						}
					}
				}
				finally
				{
					_transactionPlan.ReleaseRow(checkRow);
				}
			}
			finally
			{
				_transactionPlan.Close(cursor);
			}
		}
		
		protected void ValidateCheck(Program program, Schema.Transition transition)
		{
			switch (transition)
			{
				case Schema.Transition.Insert :
				{
					Row newRow = new Row(program.ValueManager, this.TableVar.DataType.RowType, (NativeRow)((IRow)program.Stack.Peek(0)).AsNative);
					try
					{
						program.Stack.Push(newRow);
						try
						{
							foreach (Schema.Constraint constraint in _insertConstraints)
								if (constraint.Enforced && constraint.IsDeferred && constraint is Schema.RowConstraint)
									constraint.Validate(program, transition);
						}
						finally
						{
							program.Stack.Pop();
						}
					}
					finally
					{
						newRow.Dispose();
					}

					foreach (Schema.Constraint constraint in _insertConstraints)
						if (constraint.Enforced && constraint.IsDeferred && constraint is Schema.TransitionConstraint)
							constraint.Validate(program, Schema.Transition.Insert);
				}
				break;

				case Schema.Transition.Update :
				{
					Row newRow = new Row(program.ValueManager, this.TableVar.DataType.RowType, (NativeRow)((IRow)program.Stack.Peek(0)).AsNative);
					try
					{
						program.Stack.Push(newRow);
						try
						{
							foreach (Schema.Constraint constraint in _updateConstraints)
								if (constraint.Enforced && constraint.IsDeferred && constraint is Schema.RowConstraint)
									constraint.Validate(program, Schema.Transition.Update);
						}
						finally
						{
							program.Stack.Pop();
						}
					}
					finally
					{
						newRow.Dispose();
					}
							
					foreach (Schema.Constraint constraint in _updateConstraints)
						if (constraint.Enforced && constraint.IsDeferred && constraint is Schema.TransitionConstraint)
							constraint.Validate(program, Schema.Transition.Update);
				}
				break;

				case Schema.Transition.Delete :
					foreach (Schema.Constraint constraint in _deleteConstraints)
						if (constraint.Enforced && constraint.IsDeferred && constraint is Schema.TransitionConstraint)
							constraint.Validate(program, Schema.Transition.Delete);
				break;
			}
		}
	}
	
	public class ServerCheckTables : Dictionary<Schema.Key, ServerCheckTable>
	{
	}

	public class ServerConstraintKeys : Dictionary<Schema.Key, Schema.Constraints>
	{
		public ServerConstraintKeys() : base()
		{
			keysByValueFlags = new Dictionary<BitArray, Schema.Keys>(BitArrayEqualityComparer.Default);
		}

		public void AddConstraintToKey(Schema.Key constraintKey, Schema.Constraint constraint)
		{
			var constraints = GetConstraintsForKey(constraintKey);

			if (!constraints.Contains(constraint))
				constraints.Add(constraint);
		}

		public Schema.Constraints GetConstraintsForKey(Schema.Key constraintKey)
		{
			Schema.Constraints constraints = null;
			if (!TryGetValue(constraintKey, out constraints))
			{
				constraints = new Schema.Constraints();
				Add(constraintKey, constraints);
			}

			return constraints;
		}

		private Dictionary<BitArray, Schema.Keys> keysByValueFlags;

		public Schema.Keys GetKeys(BitArray valueFlags)
		{
			Schema.Keys keys = null;

			if (valueFlags == null)
			{
				keys = new Schema.Keys();
				keys.AddRange(Keys);
				return keys;
			}

			if (!keysByValueFlags.TryGetValue(valueFlags, out keys))
			{
				keys = new Schema.Keys();
				keysByValueFlags.Add(valueFlags, keys);

				foreach (var entry in this)
				{
					foreach (Schema.Constraint constraint in entry.Value)
					{
						var rowConstraint = constraint as Schema.RowConstraint;
						if (rowConstraint != null && rowConstraint.ShouldValidate(valueFlags, Schema.Transition.Update))
						{
							if (!keys.Contains(entry.Key))
								keys.Add(entry.Key);
							break;
						}

						var transitionConstraint = constraint as Schema.TransitionConstraint;
						if (transitionConstraint != null && transitionConstraint.ShouldValidate(valueFlags, Schema.Transition.Update))
						{
							if (!keys.Contains(entry.Key))
								keys.Add(entry.Key);
							break;
						}
					}
				}
			}

			return keys;
		}
	}
	
	public class ServerTableVar : Disposable
	{
		public ServerTableVar(ServerProcess process, Schema.TableVar tableVar) : base()
		{
			_process = process;
			_tableVar = tableVar;
			_checkTables = new ServerCheckTables();
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				UnprepareCheckTables();
			}
			finally
			{
				_tableVar = null;
				base.Dispose(disposing);
			}
		}

		protected ServerProcess _process;
		public ServerProcess Process { get { return _process; } }
		
		protected Schema.TableVar _tableVar;
		public Schema.TableVar TableVar { get { return _tableVar; } }

		protected ServerCheckTables _checkTables;
		public ServerCheckTables CheckTables { get { return _checkTables; } }
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is ServerTableVar) && Schema.Object.NamesEqual(_tableVar.Name, ((ServerTableVar)objectValue).TableVar.Name);
		}

		public override int GetHashCode()
		{
			return _tableVar.GetHashCode();
		}

		protected void UnprepareCheckTables()
		{
			if (_checkTables != null)
			{
				foreach (var checkTable in _checkTables.Values)
				{
					checkTable.Dispose();
				}

				_checkTables = null;
			}
		}

		private ServerConstraintKeys _insertConstraints;
		private ServerConstraintKeys _updateConstraints;
		private ServerConstraintKeys _deleteConstraints;

		private Schema.TableVarColumnsBase GetColumns(BitArray columnFlags)
		{
			var result = new Schema.TableVarColumnsBase();

			for (int i = 0; i < _tableVar.Columns.Count; i++)
				if (columnFlags[i])
					result.Add(_tableVar.Columns[i]);
			return result;
		}

		private Schema.Key DetermineConstraintKey(Schema.Constraint constraint, BitArray columnFlags)
		{
			Schema.Key key = null;

			if (columnFlags != null)
				key = _tableVar.Keys.MinimumSubsetKey(GetColumns(columnFlags), false, false);

			if (key == null)
				key = _tableVar.Keys.MinimumKey(false, false);

			return key;
		}

		private void DetermineConstraintKeys()
		{
			// NOTE: Constraint keys must all be determined immediately due to the possibility that an update
			// transition may be checked as delete/insert.

			if (_insertConstraints == null)
			{
				_insertConstraints = new ServerConstraintKeys();
				_updateConstraints = new ServerConstraintKeys();
				_deleteConstraints = new ServerConstraintKeys();

				if (_tableVar.HasRowConstraints())
				{
					foreach (Schema.RowConstraint constraint in _tableVar.RowConstraints)
					{
						if (constraint.IsDeferred)
						{
							var constraintKey = DetermineConstraintKey(constraint, constraint.ColumnFlags);
							_insertConstraints.AddConstraintToKey(constraintKey, constraint);
							_updateConstraints.AddConstraintToKey(constraintKey, constraint);
						}
					}
				}

				if (_tableVar.HasInsertConstraints())
					foreach (Schema.TransitionConstraint constraint in _tableVar.InsertConstraints)
						if (constraint.IsDeferred)
							_insertConstraints.AddConstraintToKey(DetermineConstraintKey(constraint, constraint.InsertColumnFlags), constraint);

				if (_tableVar.HasUpdateConstraints())
					foreach (Schema.TransitionConstraint constraint in _tableVar.UpdateConstraints)
						if (constraint.IsDeferred)
							_updateConstraints.AddConstraintToKey(DetermineConstraintKey(constraint, constraint.UpdateColumnFlags), constraint);

				if (_tableVar.HasDeleteConstraints())
					foreach (Schema.TransitionConstraint constraint in _tableVar.DeleteConstraints)
						if (constraint.IsDeferred)
							_deleteConstraints.AddConstraintToKey(DetermineConstraintKey(constraint, constraint.DeleteColumnFlags), constraint);
			}
		}

		private ServerCheckTable EnsureCheckTable(Schema.Key checkKey)
		{
			ServerCheckTable checkTable;
			if (!_checkTables.TryGetValue(checkKey, out checkTable))
			{
				checkTable = 
					new ServerCheckTable
					(
						_process, 
						_tableVar, 
						checkKey, 
						_insertConstraints.GetConstraintsForKey(checkKey), 
						_updateConstraints.GetConstraintsForKey(checkKey), 
						_deleteConstraints.GetConstraintsForKey(checkKey)
					);

				_checkTables.Add(checkKey, checkTable);
			}

			return checkTable;
		}

		// TODO: This will need to be reviewed for potential caching issues under constraint alteration
		public void AddInsertTableVarCheck(int transactionIndex, IRow row)
		{
			DetermineConstraintKeys();
			foreach (var key in _insertConstraints.Keys)
			{
				EnsureCheckTable(key).AddInsertTableVarCheck(transactionIndex, row);
			}
		}

		public void AddUpdateTableVarCheck(int transactionIndex, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			DetermineConstraintKeys();
			foreach (var key in _updateConstraints.GetKeys(valueFlags))
			{
				EnsureCheckTable(key).AddUpdateTableVarCheck(transactionIndex, oldRow, newRow, valueFlags);
			}
		}

		public void AddDeleteTableVarCheck(int transactionIndex, IRow row)
		{
			DetermineConstraintKeys();
			foreach (var key in _deleteConstraints.Keys)
			{
				EnsureCheckTable(key).AddDeleteTableVarCheck(transactionIndex, row);
			}
		}

		public void Validate(Program program, int transactionIndex)
		{
			foreach (var checkTable in _checkTables.Values)
				checkTable.Validate(program, transactionIndex);
		}

		public void DeleteCheckTableChecks(int transactionIndex)
		{
			foreach (var checkTable in _checkTables.Values)
				checkTable.DeleteCheckTableChecks(transactionIndex);
		}
	}

	public class ServerTableVars : Dictionary<Schema.TableVar, ServerTableVar>
	{
	}
	
	public abstract class ServerHandler : System.Object
	{
		public ServerHandler(Schema.TableVarEventHandler handler) : base() 
		{
			_handler = handler;
		}
		
		protected Schema.IRowType _newRowType;
		protected Schema.IRowType _oldRowType;
		
		protected Schema.TableVarEventHandler _handler;
		public Schema.TableVarEventHandler Handler { get { return _handler; } }
		
		public abstract void Invoke(Program program);
		
		public abstract void Deallocate(Program program);
	}
	
	public class ServerHandlers : List<ServerHandler>
	{
		public void Deallocate(Program program)
		{
			for (int index = 0; index < Count; index++)
				this[index].Deallocate(program);
		}
	}
	
	public class ServerInsertHandler : ServerHandler
	{
		public ServerInsertHandler(Schema.TableVarEventHandler handler, IRow row) : base(handler)
		{
			_newRowType = row.DataType;
			NativeRow = (NativeRow)row.CopyNative();
		}
		
		public NativeRow NativeRow;
		
		public override void Invoke(Program program)
		{
			Row row = new Row(program.ValueManager, _newRowType, NativeRow);
			try
			{
				program.Stack.Push(row);
				try
				{
					_handler.PlanNode.Execute(program);
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			finally
			{
				row.Dispose();
			}
		}
		
		public override void Deallocate(Program program)
		{
			if (NativeRow != null)
			{
				DataValue.DisposeNative(program.ValueManager, _newRowType, NativeRow);
				NativeRow = null;
			}
		}
	}
	
	public class ServerUpdateHandler : ServerHandler
	{
		public ServerUpdateHandler(Schema.TableVarEventHandler handler, IRow oldRow, IRow newRow) : base(handler)
		{
			_newRowType = newRow.DataType;
			_oldRowType = oldRow.DataType;
			OldNativeRow = (NativeRow)oldRow.CopyNative();
			NewNativeRow = (NativeRow)newRow.CopyNative();
		}
		
		public NativeRow OldNativeRow;
		public NativeRow NewNativeRow;

		public override void Invoke(Program program)
		{
			Row oldRow = new Row(program.ValueManager, _oldRowType, OldNativeRow);
			try
			{
				Row newRow = new Row(program.ValueManager, _newRowType, NewNativeRow);
				try
				{
					program.Stack.Push(oldRow);
					try
					{
						program.Stack.Push(newRow);
						try
						{
							_handler.PlanNode.Execute(program);
						}
						finally
						{
							program.Stack.Pop();
						}
					}
					finally
					{
						program.Stack.Pop();
					}
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
		}
		
		public override void Deallocate(Program program)
		{
			if (OldNativeRow != null)
			{
				DataValue.DisposeNative(program.ValueManager, _oldRowType, OldNativeRow);
				OldNativeRow = null;
			}

			if (NewNativeRow != null)
			{
				DataValue.DisposeNative(program.ValueManager, _newRowType, NewNativeRow);
				NewNativeRow = null;
			}
		}
	}
	
	public class ServerDeleteHandler : ServerHandler
	{
		public ServerDeleteHandler(Schema.TableVarEventHandler handler, IRow row) : base(handler)
		{
			_oldRowType = row.DataType;
			NativeRow = (NativeRow)row.CopyNative();
		}
		
		public NativeRow NativeRow;

		public override void Invoke(Program program)
		{
			Row row = new Row(program.ValueManager, _oldRowType, NativeRow);
			try
			{
				program.Stack.Push(row);
				try
				{
					_handler.PlanNode.Execute(program);
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			finally
			{
				row.Dispose();
			}
		}
		
		public override void Deallocate(Program program)
		{
			if (NativeRow != null)
			{
				DataValue.DisposeNative(program.ValueManager, _oldRowType, NativeRow);
				NativeRow = null;
			}
		}
	}
}
