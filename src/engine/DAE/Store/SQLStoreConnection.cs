/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Abstract SQL Store Connection
*/

//#define SQLSTORETIMING

using System;
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
			ExecuteCommand.CommandType = SQLCommandType.Statement;
			ExecuteCommand.Statement = statement;

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
			ExecuteCommand.CommandType = SQLCommandType.Statement;
			ExecuteCommand.Statement = statement;

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
		
		public SQLStoreCursor OpenCursor(string tableName, List<string> columns, SQLIndex index, bool isUpdatable)
		{
			return InternalOpenCursor(tableName, columns, index, isUpdatable);
		}
		
		private int _transactionCount = 0;
		public int TransactionCount { get { return _transactionCount; } }
		
		internal abstract class SQLStoreOperation : System.Object
		{
			public virtual void Undo(SQLStoreConnection connection) {}
		}
		
		internal class SQLStoreOperationLog : List<SQLStoreOperation> {}
		
		private SQLStoreOperationLog _operationLog = new SQLStoreOperationLog();
		
		internal class BeginTransactionOperation : SQLStoreOperation {}
		
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
				_connection.BeginTransaction(isolationLevel);
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("BeginTransaction", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				#endif

			}
			_transactionCount++;
			
			_operationLog.Add(new BeginTransactionOperation());
		}
		
		public virtual void CommitTransaction()
		{
			_transactionCount--;
			if (_transactionCount <= 0)
			{
				#if SQLSTORETIMING
				long startTicks = TimingUtility.CurrentTicks;
				#endif
				
				if (_connection.InTransaction)
					_connection.CommitTransaction();
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				#endif
			}

			for (int index = _operationLog.Count - 1; index >= 0; index--)
				if (_operationLog[index] is BeginTransactionOperation)
				{
					_operationLog.RemoveAt(index);
					break;
				}
		}
		
		public virtual void RollbackTransaction()
		{
			_transactionCount--;

			for (int index = _operationLog.Count - 1; index >= 0; index--)
			{
				SQLStoreOperation operation = _operationLog[index];
				_operationLog.RemoveAt(index);
				if (operation is BeginTransactionOperation)
					break;
				else
					operation.Undo(this);
			}

			if (_transactionCount <= 0)
			{
				#if SQLSTORETIMING
				long startTicks = TimingUtility.CurrentTicks;
				#endif
				
				if (_connection.InTransaction)
					_connection.RollbackTransaction();
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				#endif
			}
		}

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
		
		// TODO: Statement parameterization?
		internal void PerformInsert(string tableName, List<string> columns, List<string> key, object[] row)
		{
			ExecuteStatement(GenerateInsertStatement(tableName, columns, key, row));
		}

		internal void LogInsert(string tableName, List<string> columns, List<string> key, object[] row)
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif

			_operationLog.Add(new ModifyOperation(GenerateDeleteStatement(tableName, columns, key, row)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogInsert", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
		}

		protected virtual string GenerateInsertStatement(string tableName, List<string> columns, List<string> key, object[] row)
		{
			StringBuilder statement = new StringBuilder();
			statement.AppendFormat("insert into {0} values (", tableName);
			for (int index = 0; index < columns.Count; index++)
			{
				if (index > 0)
					statement.Append(", ");
				statement.Append(NativeToLiteralValue(index >= row.Length ? null : row[index]));
			}
			statement.Append(")");
			return statement.ToString();
		}

		internal void PerformUpdate(string tableName, List<string> columns, List<string> key, object[] oldRow, object[] newRow)
		{
			ExecuteStatement(GenerateUpdateStatement(tableName, columns, key, oldRow, newRow));
		}

		internal void LogUpdate(string tableName, List<string> columns, List<string> key, object[] oldRow, object[] newRow)
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif

			_operationLog.Add(new ModifyOperation(GenerateUpdateStatement(tableName, columns, key, newRow, oldRow)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogUpdate", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
		}

		protected virtual string GenerateUpdateStatement(string tableName, List<string> columns, List<string> key, object[] oldRow, object[] newRow)
		{
			StringBuilder statement = new StringBuilder();
			statement.AppendFormat("update {0} set", tableName);
			for (int index = 0; index < columns.Count; index++)
			{
				if (index > 0)
					statement.Append(",");
				statement.AppendFormat(" {0} = {1}", columns[index], NativeToLiteralValue(newRow[index]));
			}
			statement.AppendFormat(" where");
			for (int index = 0; index < key.Count; index++)
			{
				if (index > 0)
					statement.Append(" and");
				statement.AppendFormat(" {0} = {1}", key[index], NativeToLiteralValue(oldRow[columns.IndexOf(key[index])]));
			}
			return statement.ToString();
		}
		
		internal void PerformDelete(string tableName, List<string> columns, List<string> key, object[] row)
		{
			ExecuteStatement(GenerateDeleteStatement(tableName, columns, key, row));
		}

		internal void LogDelete(string tableName, List<string> columns, List<string> key, object[] row)
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif

			_operationLog.Add(new ModifyOperation(GenerateInsertStatement(tableName, columns, key, row)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogDelete", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
		}

		protected virtual string GenerateDeleteStatement(string tableName, List<string> columns, List<string> key, object[] row)
		{
			StringBuilder statement = new StringBuilder();
			statement.AppendFormat("delete from {0} where", tableName);
			for (int index = 0; index < key.Count; index++)
			{
				if (index > 0)
					statement.Append(" and");
				statement.AppendFormat(" {0} = {1}", key[index], NativeToLiteralValue(row[columns.IndexOf(key[index])]));
			}
			return statement.ToString();
		}
	}
}
