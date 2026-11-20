/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Abstract SQL Store Connection
*/

//#define SQLSTORETIMING

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store
{
	public abstract class SQLStoreConnection : System.Object, IDisposable
	{
		protected internal SQLStoreConnection(SQLStore store) : base()
		{
			_store = store;
			_connection = InternalCreateConnection();
		}
		
		protected abstract SQLConnection InternalCreateConnection();
		
		#region IDisposable Members
		
		protected virtual void InternalDispose()
		{
			DisposeExecuteCommand();

			if (_connection != null)
			{
				_connection.Dispose();
				_connection = null;
			}
		}

		public void Dispose()
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif
			
			InternalDispose();

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
			
			if (_store != null)
			{
				_store.ReportDisconnect();
				_store = null;
			}
		}

		#endregion

		private SQLStore _store;
		/// <summary>The store for this connection.</summary>
		public SQLStore Store { get { return _store; } }
		
		// Connection
		private SQLConnection _connection;
		/// <summary>This is the internal connection to the server housing the catalog store.</summary>
		protected SQLConnection Connection { get { return _connection; } }
		
		/// <summary>Returns whether or not the store has a table of the given name.</summary>
		public virtual bool HasTable(string tableName)
		{
			// TODO: Implement...
			// To implement this generically would require dealing with all the provider-specific 'schema collection' tables
			// returned from a call to GetSchema. ADO.NET does not define a common schema collection for tables.
			throw new NotSupportedException();
		}
		
		protected virtual SQLCommand InternalCreateCommand()
		{
			return _connection.CreateCommand(false);
		}

		// ExecuteCommand
		private SQLCommand _executeCommand;
		/// <summary>This is the internal command used to execute statements on this connection.</summary>
		protected SQLCommand ExecuteCommand
		{
			get
			{
				if (_executeCommand == null)
					_executeCommand = InternalCreateCommand();
				return _executeCommand;
			}
		}
		
		/// <summary>Returns a new command that can be used to open readers on this connection.</summary>
		public SQLCommand GetReaderCommand()
		{
			return InternalCreateCommand();
		}

		protected void DisposeExecuteCommand()
		{
			if (_executeCommand != null)
			{
				_executeCommand.Dispose();
				_executeCommand = null;
			}
		}
		
		public void ExecuteScript(string script)
		{
			bool saveShouldNormalizeWhitespace = ExecuteCommand.ShouldNormalizeWhitespace;
			ExecuteCommand.ShouldNormalizeWhitespace = false;
			try
			{
				List<String> statements = SQLStore.ProcessBatches(script);
				for (int index = 0; index < statements.Count; index++)
					ExecuteStatement(statements[index]);
			}
			finally
			{
				ExecuteCommand.ShouldNormalizeWhitespace = saveShouldNormalizeWhitespace;
			}
		}

		public void ExecuteStatement(string statement)
		{
			ExecuteStatement(statement, null);
		}

		// The execute command lives as long as the connection, so each statement replaces the parameters of the one before it
		// rather than adding to them: a parameter left behind would bind to an @name in a later statement that did not expect it.
		private void PrepareExecuteCommand(string statement, SQLParameters parameters)
		{
			ExecuteCommand.CommandType = SQLCommandType.Statement;
			ExecuteCommand.Statement = statement;
			ExecuteCommand.Parameters.Clear();
			if (parameters != null)
				ExecuteCommand.Parameters.AddRange(parameters);
		}

		/// <summary>Executes a statement that carries its values as parameters; <paramref name="parameters"/> may be null for a statement with none.</summary>
		public void ExecuteStatement(string statement, SQLParameters parameters)
		{
			PrepareExecuteCommand(statement, parameters);

			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif

			ExecuteCommand.Execute();

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("ExecuteNonQuery", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
		}
		
		public object ExecuteScalar(string statement)
		{
			PrepareExecuteCommand(statement, null);

			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
			
				return ExecuteCommand.Evaluate();
			
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteScalar", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			}
			#endif
		}
		
		protected internal SQLCursor ExecuteReader(string statement, out SQLCommand readerCommand)
		{
			readerCommand = GetReaderCommand();
			readerCommand.CommandType = SQLCommandType.Statement;
			readerCommand.Statement = statement;
			
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return readerCommand.Open(SQLCursorType.Dynamic, SQLIsolationLevel.Serializable);
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteReader", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			}
			#endif
		}

		private int _openReaderCount;
		/// <summary>The number of store cursors on this connection with a reader open (a cursor's reader lives until the cursor moves to another range or is disposed).</summary>
		public int OpenReaderCount { get { return _openReaderCount; } }

		/// <summary>Called by a store cursor when it has opened its reader.</summary>
		protected internal void ReaderOpened()
		{
			_openReaderCount++;
		}

		/// <summary>Called by a store cursor when it has disposed its reader.</summary>
		protected internal void ReaderClosed()
		{
			_openReaderCount--;
		}
		
		protected virtual SQLStoreCursor InternalOpenCursor(string tableName, List<string> columns, SQLIndex index, bool isUpdatable)
		{
			return
				new SQLStoreCursor
				(
					this,
					tableName,
					columns,
					index,
					isUpdatable
				);
		}
		
		/// <summary>Opens a cursor whose updates and deletes name the row by the index's columns if the index is unique, otherwise by every column.</summary>
		public SQLStoreCursor OpenCursor(string tableName, List<string> columns, SQLIndex index, bool isUpdatable)
		{
			return InternalOpenCursor(tableName, columns, index, isUpdatable);
		}
		
		/// <summary>Opens a cursor whose updates and deletes name the row by <paramref name="rowKey"/>, normally the table's primary key columns.</summary>
		/// <remarks>The cursor is ordered on the index's columns and then on the row key's other columns.</remarks>
		public SQLStoreCursor OpenCursor(string tableName, List<string> columns, SQLIndex index, List<string> rowKey, bool isUpdatable)
		{
			// Set on the cursor that InternalOpenCursor returns, so that a store which overrides it keeps its own cursor class.
			SQLStoreCursor cursor = InternalOpenCursor(tableName, columns, index, isUpdatable);
			try
			{
				cursor.SetRowKey(rowKey);
				return cursor;
			}
			catch
			{
				cursor.Dispose();
				throw;
			}
		}
		
		private int _transactionCount = 0;
		public int TransactionCount { get { return _transactionCount; } }
		
		/// <summary>Whether the connection to the database is in a transaction.</summary>
		/// <remarks>
		/// The same as TransactionCount > 0, except after a commit or rollback that failed in the database: the count has
		/// already gone down, and the database transaction may still be open.
		/// </remarks>
		public bool InDatabaseTransaction { get { return (_connection != null) && _connection.InTransaction; } }

		internal abstract class SQLStoreOperation : System.Object
		{
			public virtual void Undo(SQLStoreConnection connection) {}
		}
		
		internal class SQLStoreOperationLog : List<SQLStoreOperation> {}
		
		private SQLStoreOperationLog _operationLog = new SQLStoreOperationLog();
		
		internal class BeginTransactionOperation : SQLStoreOperation {}
		
		// The inverse of one write, replayed as text when a nested transaction level rolls back. The log is the one place a
		// generated write keeps its values as literals rather than parameters: it is built once per write inside a nested level,
		// replayed at most once on one connection against one back end, so carrying a parameter list through the rollback path
		// would buy nothing.
		internal class ModifyOperation : SQLStoreOperation
		{
			public ModifyOperation(string undoStatement) : base()
			{
				_undoStatement = undoStatement;
			}
			
			private string _undoStatement;

			public override void Undo(SQLStoreConnection connection)
			{
				connection.ExecuteStatement(_undoStatement);
			}
		}
		
		protected virtual void InternalBeginTransaction(SQLIsolationLevel isolationLevel)
		{
			_connection.BeginTransaction(isolationLevel);
		}

		// BeginTransaction
		public virtual void BeginTransaction(SQLIsolationLevel isolationLevel)
		{
			if (_transactionCount == 0)
			{
				#if SQLSTORETIMING
				long startTicks = TimingUtility.CurrentTicks;
				#endif
				
				DisposeExecuteCommand();
				InternalBeginTransaction(isolationLevel);
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("BeginTransaction", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				#endif

			}
			// Counted only once the begin has succeeded: a begin that fails (a lock wait that timed out) leaves no level for the
			// caller's rollback to end.
			_transactionCount++;
			
			_operationLog.Add(new BeginTransactionOperation());
		}
		
		public virtual void CommitTransaction()
		{
			_transactionCount--;
			if (_transactionCount <= 0)
			{
				// Emptied before the commit, which may fail: the entries that nested levels committed into the outermost level are
				// never replayed, and would otherwise stay for the life of the connection.
				_operationLog.Clear();
				
				#if SQLSTORETIMING
				long startTicks = TimingUtility.CurrentTicks;
				#endif
				
				if (_connection.InTransaction)
					_connection.CommitTransaction();
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				#endif
			}
			else
			{
				for (int index = _operationLog.Count - 1; index >= 0; index--)
					if (_operationLog[index] is BeginTransactionOperation)
					{
						_operationLog.RemoveAt(index);
						break;
					}
			}
		}
		
		public virtual void RollbackTransaction()
		{
			_transactionCount--;

			if (_transactionCount <= 0)
			{
				// The database rollback undoes the writes of every level, so the log is emptied, not replayed: an undo statement that
				// failed here would leave the database transaction open.
				_operationLog.Clear();
				
				#if SQLSTORETIMING
				long startTicks = TimingUtility.CurrentTicks;
				#endif
				
				if (_connection.InTransaction)
					_connection.RollbackTransaction();
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				#endif
			}
			else
			{
				for (int index = _operationLog.Count - 1; index >= 0; index--)
				{
					SQLStoreOperation operation = _operationLog[index];
					_operationLog.RemoveAt(index);
					if (operation is BeginTransactionOperation)
						break;
					else
						operation.Undo(this);
				}
			}
		}

		// NOTE: anything but bool, string and null is written with ToString(), which is exact for the integer and text columns the
		// catalog tables have today; if a catalog table gains a double or date column, format it invariantly, or the ranged reader
		// statements and the rows a nested rollback re-inserts become culture-dependent. The insert, update and delete a write
		// executes no longer come through here: they carry their values as parameters (NativeToParameter).
		public virtual object NativeToLiteralValue(object tempValue)
		{
			if (tempValue is bool)
				return ((bool)tempValue ? 1 : 0).ToString();
			if (tempValue is string)
				return String.Format("'{0}'", ((string)tempValue).Replace("'", "''"));
			if (tempValue == null)
				return "null";
			return tempValue.ToString();
		}

		/// <summary>Returns the parameter that carries <paramref name="tempValue"/>, a native value, into a generated statement under the given name.</summary>
		/// <remarks>
		/// <para>
		/// A boolean goes as the integer 1 or 0 because the catalog declares its boolean columns tinyint: a provider handed a .NET
		/// bool would bind its own boolean type, which a back end may refuse against an integer column. That is the mapping the
		/// catalog device's read path already uses (ServerCatalogDevice.TranslateScalarParameter). A store whose catalog declares
		/// those columns boolean overrides this, as the PostgreSQL store does.
		/// </para>
		/// <para>
		/// Never called with null: a null is written as the literal null, which needs no type (see AppendValue). The types mapped
		/// are the ones the catalog writes and the ones a store cursor reads back and writes again; anything else is an internal
		/// error rather than a silent ToString(), which is the culture-dependent conversion the parameters are here to remove.
		/// </para>
		/// </remarks>
		public virtual SQLParameter NativeToParameter(string name, object tempValue)
		{
			if (tempValue is bool)
				return new SQLParameter(name, new SQLIntegerType(1), (bool)tempValue ? 1 : 0);
			if (tempValue is string)
				return new SQLParameter(name, new SQLStringType(((string)tempValue).Length), tempValue);
			if (tempValue is int)
				return new SQLParameter(name, new SQLIntegerType(4), tempValue);
			if (tempValue is long)
				return new SQLParameter(name, new SQLIntegerType(8), tempValue);
			if (tempValue is short)
				return new SQLParameter(name, new SQLIntegerType(2), tempValue);
			if (tempValue is byte)
				return new SQLParameter(name, new SQLIntegerType(1), tempValue);
			Error.Fail("A catalog store write has no parameter for a value of type {0}.", tempValue.GetType().Name);
			return null;
		}

		/// <summary>Appends one value of a generated statement: a parameter marker, with the value collected into <paramref name="parameters"/>, or a literal when parameters is null.</summary>
		/// <remarks>
		/// A null is the literal null in either form. Every back end spells it the same, it needs no quoting and no culture, and a
		/// parameter bound to DBNull would have to be given a type that the value itself does not say.
		/// </remarks>
		private void AppendValue(StringBuilder statement, SQLParameters parameters, object value)
		{
			if ((parameters == null) || (value == null))
				statement.Append(NativeToLiteralValue(value));
			else
			{
				// Named by position in the order the values appear in the statement: a provider that binds by position (SQLite's does)
				// needs that order, and one that binds by name needs the names distinct.
				// NOTE: a parameterized write allocates a parameter and a type per value here, and the provider then builds its own
				// parameter object per value on every execute (DotNetCommand.PrepareParameters), where a literal write only appended to
				// a string. Measured when this landed and no change was detectable either way: StoreRebuildWorkflowTest's window read
				// 975 / 1461 / 1441 / 1466 ms before and 916 / 1420 / 1361 / 1328 ms after, and suite part 1, which is mostly servers
				// starting and populating their catalogs, 2 m 54 s / 3 m 01 s before and 2 m 59 s / 3 m 14 s / 3 m 24 s / 3 m 25 s /
				// 3 m 35 s after - overlapping bands on a machine whose spread is wider than any effect. So this is not known to cost
				// anything; if catalog write throughput ever matters, measure on a quiet machine before changing anything here, and
				// check first whether the provider re-prepares the statement on every execute: DotNetCommand.PrepareCommand assigns
				// IDbCommand.CommandText on every execute even when the text has not changed, which would throw away the reuse of one
				// prepared statement across rows that parameters were expected to bring.
				SQLParameter parameter = NativeToParameter("P" + parameters.Count.ToString(CultureInfo.InvariantCulture), value);
				parameters.Add(parameter);
				statement.Append('@').Append(parameter.Name);
			}
		}

		/// <summary>Executes a generated write, naming the statement with its values rendered in if it fails.</summary>
		/// <remarks>
		/// The command carries parameter markers, so a provider error would otherwise report "insert into DAEUsers values (@P0,
		/// @P1)" without saying which row failed. Only the connection's generic SQL wrap is re-raised: a store lock and a
		/// constraint violation come back as other exception types, carry no statement, and must reach the caller unchanged.
		/// </remarks>
		private void ExecuteWriteStatement(string statement, SQLParameters parameters, Func<string> literalStatement)
		{
			try
			{
				ExecuteStatement(statement, parameters);
			}
			catch (ConnectionException exception)
			{
				throw new ConnectionException(ConnectionException.Codes.SQLException, exception.Severity, exception, literalStatement());
			}
		}

		internal void PerformInsert(string tableName, List<string> columns, List<string> key, object[] row)
		{
			SQLParameters parameters = new SQLParameters();
			ExecuteWriteStatement
			(
				GenerateInsertStatement(tableName, columns, key, row, parameters),
				parameters,
				() => GenerateInsertStatement(tableName, columns, key, row, null)
			);
		}

		internal void LogInsert(string tableName, List<string> columns, List<string> key, object[] row)
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif

			_operationLog.Add(new ModifyOperation(GenerateDeleteStatement(tableName, columns, key, row, null)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogInsert", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
		}

		/// <summary>Generates the insert, with its values as parameters collected into <paramref name="parameters"/>, or as literals when parameters is null.</summary>
		protected virtual string GenerateInsertStatement(string tableName, List<string> columns, List<string> key, object[] row, SQLParameters parameters)
		{
			StringBuilder statement = new StringBuilder();
			statement.AppendFormat("insert into {0} values (", tableName);
			for (int index = 0; index < columns.Count; index++)
			{
				if (index > 0)
					statement.Append(", ");
				AppendValue(statement, parameters, ValueAt(row, index));
			}
			statement.Append(")");
			return statement.ToString();
		}

		// A row may be shorter than the table, from a caller that leaves the trailing columns out; the insert stores those as null,
		// so the key condition of the logged inverse delete must read them as null too.
		private static object ValueAt(object[] row, int index)
		{
			return index < row.Length ? row[index] : null;
		}

		internal void PerformUpdate(string tableName, List<string> columns, List<string> key, object[] oldRow, object[] newRow)
		{
			SQLParameters parameters = new SQLParameters();
			ExecuteWriteStatement
			(
				GenerateUpdateStatement(tableName, columns, key, oldRow, newRow, parameters),
				parameters,
				() => GenerateUpdateStatement(tableName, columns, key, oldRow, newRow, null)
			);
		}

		internal void LogUpdate(string tableName, List<string> columns, List<string> key, object[] oldRow, object[] newRow)
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif

			_operationLog.Add(new ModifyOperation(GenerateUpdateStatement(tableName, columns, key, newRow, oldRow, null)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogUpdate", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
		}

		/// <summary>Generates the update, with its values as parameters collected into <paramref name="parameters"/>, or as literals when parameters is null.</summary>
		protected virtual string GenerateUpdateStatement(string tableName, List<string> columns, List<string> key, object[] oldRow, object[] newRow, SQLParameters parameters)
		{
			StringBuilder statement = new StringBuilder();
			statement.AppendFormat("update {0} set", tableName);
			for (int index = 0; index < columns.Count; index++)
			{
				if (index > 0)
					statement.Append(",");
				statement.AppendFormat(" {0} = ", columns[index]);
				AppendValue(statement, parameters, newRow[index]);
			}
			AppendKeyCondition(statement, columns, key, oldRow, parameters);
			return statement.ToString();
		}
		
		/// <summary>Appends a where clause that names the row by the values of its key columns.</summary>
		private void AppendKeyCondition(StringBuilder statement, List<string> columns, List<string> key, object[] row, SQLParameters parameters)
		{
			statement.Append(" where");
			for (int index = 0; index < key.Count; index++)
			{
				if (index > 0)
					statement.Append(" and");
				object value = ValueAt(row, columns.IndexOf(key[index]));
				// NOTE: a null key value stays the literal "is null" in the parameterized form too. A parameter would make the term
				// "= @P", which against a null is unknown rather than true in standard SQL, so the statement would name no row. This
				// is the one place in a generated write where a literal is the correct form and a parameter would silently break it.
				if (value == null)
					statement.AppendFormat(" {0} is null", key[index]);
				else
				{
					statement.AppendFormat(" {0} = ", key[index]);
					AppendValue(statement, parameters, value);
				}
			}
		}
		
		internal void PerformDelete(string tableName, List<string> columns, List<string> key, object[] row)
		{
			SQLParameters parameters = new SQLParameters();
			ExecuteWriteStatement
			(
				GenerateDeleteStatement(tableName, columns, key, row, parameters),
				parameters,
				() => GenerateDeleteStatement(tableName, columns, key, row, null)
			);
		}

		internal void LogDelete(string tableName, List<string> columns, List<string> key, object[] row)
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif

			_operationLog.Add(new ModifyOperation(GenerateInsertStatement(tableName, columns, key, row, null)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogDelete", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
		}

		/// <summary>Generates the delete, with its key values as parameters collected into <paramref name="parameters"/>, or as literals when parameters is null.</summary>
		protected virtual string GenerateDeleteStatement(string tableName, List<string> columns, List<string> key, object[] row, SQLParameters parameters)
		{
			StringBuilder statement = new StringBuilder();
			statement.AppendFormat("delete from {0}", tableName);
			AppendKeyCondition(statement, columns, key, row, parameters);
			return statement.ToString();
		}
	}
}
