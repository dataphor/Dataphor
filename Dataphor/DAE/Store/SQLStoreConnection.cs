/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Abstract SQL Store Connection
*/

//#define SQLSTORETIMING
#define USESQLCONNECTION

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

#if !USESQLCONNECTION
using System.Data;
using System.Data.Common;
#endif

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store
{
	public abstract class SQLStoreConnection : System.Object, IDisposable
	{
		protected internal SQLStoreConnection(SQLStore AStore) : base()
		{
			FStore = AStore;
			FConnection = InternalCreateConnection();
			#if !USESQLCONNECTION
			FConnection.Open();
			#endif
		}
		
		#if USESQLCONNECTION
		protected abstract SQLConnection InternalCreateConnection();
		#else
		protected abstract DbConnection InternalCreateConnection();
		#endif
		
		#region IDisposable Members
		
		protected virtual void InternalDispose()
		{
			DisposeExecuteCommand();

			if (FConnection != null)
			{
				FConnection.Dispose();
				FConnection = null;
			}
		}

		public void Dispose()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			InternalDispose();

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
			
			if (FStore != null)
			{
				FStore.ReportDisconnect();
				FStore = null;
			}
		}

		#endregion

		private SQLStore FStore;
		/// <summary>The store for this connection.</summary>
		public SQLStore Store { get { return FStore; } }
		
		// Connection
		#if USESQLCONNECTION
		private SQLConnection FConnection;
		/// <summary>This is the internal connection to the server housing the catalog store.</summary>
		protected SQLConnection Connection { get { return FConnection; } }
		#else
		private DbConnection FConnection;
		/// <summary>This is the internal connection to the server housing the catalog store.</summary>
		protected DbConnection Connection { get { return FConnection; } }
		
		/// <summary>The transaction object for this connection.</summary>
		private DbTransaction FTransaction;
		#endif
		
		/// <summary>Returns whether or not the store has a table of the given name.</summary>
		public virtual bool HasTable(string ATableName)
		{
			// TODO: Implement...
			// To implement this generically would require dealing with all the provider-specific 'schema collection' tables
			// returned from a call to GetSchema. ADO.NET does not define a common schema collection for tables.
			throw new NotSupportedException();
		}
		
		#if USESQLCONNECTION
		protected virtual SQLCommand InternalCreateCommand()
		{
			return FConnection.CreateCommand(false);
		}

		// ExecuteCommand
		private SQLCommand FExecuteCommand;
		/// <summary>This is the internal command used to execute statements on this connection.</summary>
		protected SQLCommand ExecuteCommand
		{
			get
			{
				if (FExecuteCommand == null)
					FExecuteCommand = InternalCreateCommand();
				return FExecuteCommand;
			}
		}
		
		/// <summary>Returns a new command that can be used to open readers on this connection.</summary>
		public SQLCommand GetReaderCommand()
		{
			return InternalCreateCommand();
		}
		#else
		protected virtual DbCommand InternalCreateCommand()
		{
			DbCommand LCommand = FConnection.CreateCommand();
			if ((FTransaction != null) && (LCommand.Transaction == null))
				LCommand.Transaction = FTransaction;
			return LCommand;
		}

		// ExecuteCommand
		private DbCommand FExecuteCommand;
		/// <summary>This is the internal command used to execute statements on this connection.</summary>
		protected DbCommand ExecuteCommand
		{
			get
			{
				if (FExecuteCommand == null)
					FExecuteCommand = InternalCreateCommand();
				return FExecuteCommand;
			}
		}
		
		/// <summary>Returns a new command that can be used to open readers on this connection.</summary>
		public DbCommand GetReaderCommand()
		{
			return InternalCreateCommand();
		}
		#endif
		
		protected void DisposeExecuteCommand()
		{
			if (FExecuteCommand != null)
			{
				FExecuteCommand.Dispose();
				FExecuteCommand = null;
			}
		}
		
		public void ExecuteScript(string AScript)
		{
			#if USESQLCONNECTION
			bool LSaveShouldNormalizeWhitespace = ExecuteCommand.ShouldNormalizeWhitespace;
			ExecuteCommand.ShouldNormalizeWhitespace = false;
			try
			{
			#endif
			
				List<String> LStatements = SQLStore.ProcessBatches(AScript);
				for (int LIndex = 0; LIndex < LStatements.Count; LIndex++)
					ExecuteStatement(LStatements[LIndex]);
					
			#if USESQLCONNECTION
			}
			finally
			{
				ExecuteCommand.ShouldNormalizeWhitespace = LSaveShouldNormalizeWhitespace;
			}
			#endif
		}

		public void ExecuteStatement(string AStatement)
		{
			#if USESQLCONNECTION
			ExecuteCommand.CommandType = SQLCommandType.Statement;
			ExecuteCommand.Statement = AStatement;
			#else
			ExecuteCommand.CommandType = CommandType.Text;
			ExecuteCommand.CommandText = AStatement;
			#endif

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			#if USESQLCONNECTION
			ExecuteCommand.Execute();
			#else
			ExecuteCommand.ExecuteNonQuery();
			#endif

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("ExecuteNonQuery", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public object ExecuteScalar(string AStatement)
		{
			#if USESQLCONNECTION
			ExecuteCommand.CommandType = SQLCommandType.Statement;
			ExecuteCommand.Statement = AStatement;
			#else
			ExecuteCommand.CommandType = CommandType.Text;
			ExecuteCommand.CommandText = AStatement;
			#endif

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
			
				#if USESQLCONNECTION
				return ExecuteCommand.Evaluate();
				#else
				return ExecuteCommand.ExecuteScalar();
				#endif
			
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteScalar", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		#if USESQLCONNECTION
		protected internal SQLCursor ExecuteReader(string AStatement, out SQLCommand AReaderCommand)
		{
			AReaderCommand = GetReaderCommand();
			AReaderCommand.CommandType = SQLCommandType.Statement;
			AReaderCommand.Statement = AStatement;
			
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return AReaderCommand.Open(SQLCursorType.Dynamic, SQLIsolationLevel.Serializable);
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteReader", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		#else
		protected internal DbDataReader ExecuteReader(string AStatement, out DbCommand AReaderCommand)
		{
			AReaderCommand = GetReaderCommand();
			AReaderCommand.CommandType = CommandType.Text;
			AReaderCommand.CommandText = AStatement;

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return AReaderCommand.ExecuteReader();
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteReader", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		#endif
		
		protected virtual SQLStoreCursor InternalOpenCursor(string ATableName, List<string> AColumns, SQLIndex AIndex, bool AIsUpdatable)
		{
			return
				new SQLStoreCursor
				(
					this,
					ATableName,
					AColumns,
					AIndex,
					AIsUpdatable
				);
		}
		
		public SQLStoreCursor OpenCursor(string ATableName, List<string> AColumns, SQLIndex AIndex, bool AIsUpdatable)
		{
			return InternalOpenCursor(ATableName, AColumns, AIndex, AIsUpdatable);
		}
		
		private int FTransactionCount = 0;
		public int TransactionCount { get { return FTransactionCount; } }
		
		internal abstract class SQLStoreOperation : System.Object
		{
			public virtual void Undo(SQLStoreConnection AConnection) {}
		}
		
		internal class SQLStoreOperationLog : List<SQLStoreOperation> {}
		
		private SQLStoreOperationLog FOperationLog = new SQLStoreOperationLog();
		
		internal class BeginTransactionOperation : SQLStoreOperation {}
		
		internal class ModifyOperation : SQLStoreOperation
		{
			public ModifyOperation(string AUndoStatement) : base()
			{
				FUndoStatement = AUndoStatement;
			}
			
			private string FUndoStatement;

			public override void Undo(SQLStoreConnection AConnection)
			{
				AConnection.ExecuteStatement(FUndoStatement);
			}
		}
		
		#if USESQLCONNECTION
		protected virtual void InternalBeginTransaction(SQLIsolationLevel AIsolationLevel)
		{
			FConnection.BeginTransaction(AIsolationLevel);
		}
		#else
		protected virtual DbTransaction InternalBeginTransaction(System.Data.IsolationLevel AIsolationLevel)
		{
			return FConnection.BeginTransaction(AIsolationLevel);
		}
		#endif

		// BeginTransaction
		#if USESQLCONNECTION
		public virtual void BeginTransaction(SQLIsolationLevel AIsolationLevel)
		#else
		public virtual void BeginTransaction(System.Data.IsolationLevel AIsolationLevel)
		#endif
		{
			if (FTransactionCount == 0)
			{
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				#endif
				
				#if USESQLCONNECTION
				DisposeExecuteCommand();
				FConnection.BeginTransaction(AIsolationLevel);
				#else
				FTransaction = InternalBeginTransaction(AIsolationLevel);
				ExecuteCommand.Transaction = FTransaction;
				#endif
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("BeginTransaction", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				#endif

			}
			FTransactionCount++;
			
			FOperationLog.Add(new BeginTransactionOperation());
		}
		
		public virtual void CommitTransaction()
		{
			FTransactionCount--;
			if (FTransactionCount <= 0)
			{
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				#endif
				
				#if USESQLCONNECTION
				if (FConnection.InTransaction)
					FConnection.CommitTransaction();
				#else
				if (FTransaction != null)
				{
					FTransaction.Commit();
					FTransaction.Dispose();
					FTransaction = null;
				}

				if (FExecuteCommand != null)
					FExecuteCommand.Transaction = null;
				#endif
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				#endif
			}

			for (int LIndex = FOperationLog.Count - 1; LIndex >= 0; LIndex--)
				if (FOperationLog[LIndex] is BeginTransactionOperation)
				{
					FOperationLog.RemoveAt(LIndex);
					break;
				}
		}
		
		public virtual void RollbackTransaction()
		{
			FTransactionCount--;

			for (int LIndex = FOperationLog.Count - 1; LIndex >= 0; LIndex--)
			{
				SQLStoreOperation LOperation = FOperationLog[LIndex];
				FOperationLog.RemoveAt(LIndex);
				if (LOperation is BeginTransactionOperation)
					break;
				else
					LOperation.Undo(this);
			}

			if (FTransactionCount <= 0)
			{
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				#endif
				
				#if USESQLCONNECTION
				if (FConnection.InTransaction)
					FConnection.RollbackTransaction();
				#else
				if (FTransaction != null)
				{
					FTransaction.Rollback();
					FTransaction.Dispose();
					FTransaction = null;
				}
				
				if (FExecuteCommand != null)
					FExecuteCommand.Transaction = null;
				#endif
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				#endif
			}
		}

		internal object NativeToLiteralValue(object AValue)
		{
			if (AValue is bool)
				return ((bool)AValue ? 1 : 0).ToString();
			if (AValue is string)
				return String.Format("'{0}'", ((string)AValue).Replace("'", "''"));
			if (AValue == null)
				return "null";
			return AValue.ToString();
		}
		
		// TODO: Statement parameterization?
		internal void PerformInsert(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			ExecuteStatement(GenerateInsertStatement(ATableName, AColumns, AKey, ARow));
		}

		internal void LogInsert(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FOperationLog.Add(new ModifyOperation(GenerateDeleteStatement(ATableName, AColumns, AKey, ARow)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogInsert", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected virtual string GenerateInsertStatement(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			StringBuilder LStatement = new StringBuilder();
			LStatement.AppendFormat("insert into {0} values (", ATableName);
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
			{
				if (LIndex > 0)
					LStatement.Append(", ");
				LStatement.Append(NativeToLiteralValue(LIndex >= ARow.Length ? null : ARow[LIndex]));
			}
			LStatement.Append(")");
			return LStatement.ToString();
		}

		internal void PerformUpdate(string ATableName, List<string> AColumns, List<string> AKey, object[] AOldRow, object[] ANewRow)
		{
			ExecuteStatement(GenerateUpdateStatement(ATableName, AColumns, AKey, AOldRow, ANewRow));
		}

		internal void LogUpdate(string ATableName, List<string> AColumns, List<string> AKey, object[] AOldRow, object[] ANewRow)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FOperationLog.Add(new ModifyOperation(GenerateUpdateStatement(ATableName, AColumns, AKey, ANewRow, AOldRow)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogUpdate", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected virtual string GenerateUpdateStatement(string ATableName, List<string> AColumns, List<string> AKey, object[] AOldRow, object[] ANewRow)
		{
			StringBuilder LStatement = new StringBuilder();
			LStatement.AppendFormat("update {0} set", ATableName);
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
			{
				if (LIndex > 0)
					LStatement.Append(",");
				LStatement.AppendFormat(" {0} = {1}", AColumns[LIndex], NativeToLiteralValue(ANewRow[LIndex]));
			}
			LStatement.AppendFormat(" where");
			for (int LIndex = 0; LIndex < AKey.Count; LIndex++)
			{
				if (LIndex > 0)
					LStatement.Append(" and");
				LStatement.AppendFormat(" {0} = {1}", AKey[LIndex], NativeToLiteralValue(AOldRow[AColumns.IndexOf(AKey[LIndex])]));
			}
			return LStatement.ToString();
		}
		
		internal void PerformDelete(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			ExecuteStatement(GenerateDeleteStatement(ATableName, AColumns, AKey, ARow));
		}

		internal void LogDelete(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FOperationLog.Add(new ModifyOperation(GenerateInsertStatement(ATableName, AColumns, AKey, ARow)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SQLStoreCounter("LogDelete", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected virtual string GenerateDeleteStatement(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			StringBuilder LStatement = new StringBuilder();
			LStatement.AppendFormat("delete from {0} where", ATableName);
			for (int LIndex = 0; LIndex < AKey.Count; LIndex++)
			{
				if (LIndex > 0)
					LStatement.Append(" and");
				LStatement.AppendFormat(" {0} = {1}", AKey[LIndex], NativeToLiteralValue(ARow[AColumns.IndexOf(AKey[LIndex])]));
			}
			return LStatement.ToString();
		}
	}
}
