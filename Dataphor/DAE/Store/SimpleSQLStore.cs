//#define SQLSTORETIMING

/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Simple SQL Store
	
	A simple storage device that uses a SQL DBMS as it's backend.
	The store is capable of storing integers, strings, booleans, and long text and binary data.
	The store also manages logging and rollback of nested transactions to make up for the lack of savepoint support in the target DBMS.
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class SimpleSQLStoreCounter
	{
		public SimpleSQLStoreCounter(string AOperation, string ATableName, string AIndexName, bool AIsMatched, bool AIsRanged, bool AIsUpdatable, TimeSpan ADuration)
		{
			FOperation = AOperation;
			FTableName = ATableName;
			FIndexName = AIndexName;
			FIsMatched = AIsMatched;
			FIsRanged = AIsRanged;
			FIsUpdatable = AIsUpdatable;
			FDuration = ADuration;
		}
		
		private string FOperation;
		public string Operation { get { return FOperation; } }
		
		private string FTableName;
		public string TableName { get { return FTableName; } }
		
		private string FIndexName;
		public string IndexName { get { return FIndexName; } }
		
		private bool FIsMatched;
		public bool IsMatched { get { return FIsMatched; } }
		
		private bool FIsRanged;
		public bool IsRanged { get { return FIsRanged; } }
		
		private bool FIsUpdatable;
		public bool IsUpdatable { get { return FIsUpdatable; } }
		
		private TimeSpan FDuration;
		public TimeSpan Duration { get { return FDuration; } }
	}
	
	public class SimpleSQLStoreCounters : List<SimpleSQLStoreCounter> {}

	public abstract class SimpleSQLStore : System.Object
	{
		public abstract string GetConnectionString();
		
		public abstract SQLConnection GetSQLConnection();

		public abstract void Initialize();
		
		/// <summary> Returns the set of batches in the given script, delimited by the default 'go' batch terminator. </summary>
		public static List<String> ProcessBatches(string AScript)
		{
			return ProcessBatches(AScript, "go");
		}
		
		/// <summary>Returns the set of batches in the given script, delimited by the given terminator.</summary>
		public static List<String> ProcessBatches(string AScript, string ATerminator)
		{
			// NOTE: This is the same code as SQLUtility.ProcessBatches, duplicated to avoid the dependency
			List<String> LBatches = new List<String>();
			
			string[] LLines = AScript.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
			StringBuilder LBatch = new StringBuilder();
			for (int LIndex = 0; LIndex < LLines.Length; LIndex++)
			{
				if (LLines[LIndex].IndexOf("go", StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					LBatches.Add(LBatch.ToString());
					LBatch = new StringBuilder();
				}
				else
				{
					LBatch.Append(LLines[LIndex]);
					LBatch.Append("\r\n");
				}
			}

			if (LBatch.Length > 0)
				LBatches.Add(LBatch.ToString());
				
			return LBatches;
		}
		
		private SimpleSQLStoreCounters FCounters = new SimpleSQLStoreCounters();
		public SimpleSQLStoreCounters Counters { get { return FCounters; } }
		
		protected virtual SimpleSQLStoreConnection InternalConnect()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return new SimpleSQLStoreConnection(this);
			#if SQLSTORETIMING
			}
			finally
			{
				Counters.Add(new SimpleSQLStoreCounter("Connect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}

		/// <summary>Default maximum number of connections to an SSCE server.</summary>
		/// <remarks>
		/// According to Microsoft, the ceiling for connections to an SSCE device is around 70:
		/// <para>
		/// In SSCE technically we support 256 connections. But do not scale that well when you cross 70 connections.  
		/// To get good performance with 70 concurrent connections, you need to increase the lock time out period in connection string.  
		/// Data Source = �./local.sdf�;Max Buffer Size = 10240;Default Lock Timeout = 5000;Flush Interval = 20; AutoShrink Threshold = 10
		///	
		/// You need to make sure that the connections, sessions are properly disposed in your application.  
		/// Dispose it explicitly and don�t depend on GC to Dispose it, since it may take longer time to dispose. 
		/// </para>
		/// Based on this, it may be worthwhile to investigate some of these other settings as well, however,
		/// with the connection pooling and name resolution cache we are implementing in the catalog device,
		/// we should never get even close to this kind of concurrent access.
		/// </remarks>
		public const int CDefaultMaxConnections = 60;

		private int FMaxConnections = CDefaultMaxConnections;
		/// <summary>Maximum number of connections to allow to this store.</summary>
		/// <remarks>
		/// Set this value to 0 to allow unlimited connections.
		/// </remarks>
		public int MaxConnections { get { return FMaxConnections; } set { FMaxConnections = value; } }
		
		private int FConnectionCount;
		
		/// <summary>Establishes a connection to the store.</summary>		
		public SimpleSQLStoreConnection Connect()
		{
			lock (this)
			{
				if ((FMaxConnections > 0) && (FConnectionCount >= FMaxConnections))
					throw new CatalogException(CatalogException.Codes.MaximumConnectionsExceeded, ErrorSeverity.Environment);
				FConnectionCount++;
			}
			
			try
			{
				return InternalConnect();
			}
			catch
			{
				ReportDisconnect();
				throw;
			}
		}
		
		internal void ReportDisconnect()
		{
			lock (this)
			{
				FConnectionCount--;
			}
		}
		
		// DatabaseFileName
		private string FDatabaseFileName = "DAEStore.sdf";
		/// <summary>The name of the database file.</summary>
		/// <remarks>
		/// The default for this property is 'DAEStore.sdf'.
		/// </remarks>
		public string DatabaseFileName
		{
			get { return FDatabaseFileName; }
			set { FDatabaseFileName = value; }
		}
		
		// Password
		private string FPassword = String.Empty;
		/// <summary>The password to use to connect to the server housing the store.</summary>
		/// <remarks>
		/// The default for this property is ''.
		/// </remarks>
		public string Password
		{
			get { return FPassword; }
			set { FPassword = value; }
		}
	}
	
	public class SimpleSQLStoreConnection : System.Object, IDisposable
	{
		internal SimpleSQLStoreConnection(SimpleSQLStore AStore) : base()
		{
			FStore = AStore;
			FConnection = InternalCreateConnection();
			FConnection.Open();
		}
		
		protected virtual DbConnection InternalCreateConnection()
		{
			throw new NotSupportedException();
		}
		
		#region IDisposable Members
		
		protected virtual void InternalDispose()
		{
			if (FExecuteCommand != null)
			{
				FExecuteCommand.Dispose();
				FExecuteCommand = null;
			}

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
			Store.Counters.Add(new SimpleSQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
			
			if (FStore != null)
			{
				FStore.ReportDisconnect();
				FStore = null;
			}
		}

		#endregion

		private SimpleSQLStore FStore;
		/// <summary>The store for this connection.</summary>
		public SimpleSQLStore Store { get { return FStore; } }
		
		// Connection
		/// <summary>This is the internal connection to the server housing the catalog store.</summary>
		private DbConnection FConnection;
		
		/// <summary>The transaction object for this connection.</summary>
		private DbTransaction FTransaction;
		
		// ExecuteCommand
		private DbCommand FExecuteCommand;
		/// <summary>This is the internal command used to execute statements on this connection.</summary>
		protected DbCommand ExecuteCommand
		{
			get
			{
				if (FExecuteCommand == null)
					FExecuteCommand = FConnection.CreateCommand();
				return FExecuteCommand;
			}
		}
		
		public void ExecuteScript(string AScript)
		{
			List<String> LStatements = SimpleSQLStore.ProcessBatches(AScript);
			for (int LIndex = 0; LIndex < LStatements.Count; LIndex++)
				ExecuteStatement(LStatements[LIndex]);
		}

		public void ExecuteStatement(string AStatement)
		{
			ExecuteCommand.CommandType = CommandType.Text;
			ExecuteCommand.CommandText = AStatement;

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			ExecuteCommand.ExecuteNonQuery();

			#if SQLSTORETIMING
			Store.Counters.Add(new SimpleSQLStoreCounter("ExecuteNonQuery", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public object ExecuteScalar(string AStatement)
		{
			ExecuteCommand.CommandType = CommandType.Text;
			ExecuteCommand.CommandText = AStatement;

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
			
				return ExecuteCommand.ExecuteScalar();
			
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SimpleSQLStoreCounter("ExecuteScalar", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		protected DbDataReader ExecuteReader(string AStatement)
		{
			ExecuteCommand.CommandType = CommandType.Text;
			ExecuteCommand.CommandText = AStatement;

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return ExecuteCommand.ExecuteReader();
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SimpleSQLStoreCounter("ExecuteReader", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		protected virtual SimpleSQLStoreCursor InternalOpenCursor(string ATableName, string AIndexName, List<string> AKey, bool AIsUpdatable)
		{
			return
				new SimpleSQLStoreCursor
				(
					this,
					ATableName,
					AIndexName,
					AKey,
					AIsUpdatable
				);
		}
		
		public SimpleSQLStoreCursor OpenCursor(string ATableName, string AIndexName, List<string> AKey, bool AIsUpdatable)
		{
			return InternalOpenCursor(ATableName, AIndexName, AKey, AIsUpdatable);
		}
		
		private int FTransactionCount = 0;
		public int TransactionCount { get { return FTransactionCount; } }
		
		internal abstract class SimpleSQLStoreOperation : System.Object
		{
			public virtual void Undo(SimpleSQLStoreConnection AConnection) {}
		}
		
		internal class SimpleSQLStoreOperationLog : List<SimpleSQLStoreOperation> {}
		
		private SimpleSQLStoreOperationLog FOperationLog = new SimpleSQLStoreOperationLog();
		
		internal class BeginTransactionOperation : SimpleSQLStoreOperation {}
		
		internal class ModifyOperation : SimpleSQLStoreOperation
		{
			public ModifyOperation(string AUndoStatement) : base()
			{
				FUndoStatement = AUndoStatement;
			}
			
			private string FUndoStatement;

			public override void Undo(SimpleSQLStoreConnection AConnection)
			{
				AConnection.ExecuteStatement(FUndoStatement);
			}
		}

		// BeginTransaction
		public virtual void BeginTransaction(System.Data.IsolationLevel AIsolationLevel)
		{
			if (FTransaction == null)
			{
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				#endif
				
				FTransaction = FConnection.BeginTransaction(AIsolationLevel);
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SimpleSQLStoreCounter("BeginTransaction", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				#endif

				ExecuteCommand.Transaction = FTransaction;
			}
			FTransactionCount++;
			
			FOperationLog.Add(new BeginTransactionOperation());
		}
		
		public virtual void CommitTransaction()
		{
			FTransactionCount--;
			if ((FTransaction != null) && (FTransactionCount <= 0))
			{
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				#endif
				
				FTransaction.Commit();
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SimpleSQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				#endif

				FTransaction.Dispose();
				FTransaction = null;
				if (FExecuteCommand != null)
					FExecuteCommand.Transaction = null;
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
				SimpleSQLStoreOperation LOperation = FOperationLog[LIndex];
				FOperationLog.RemoveAt(LIndex);
				if (LOperation is BeginTransactionOperation)
					break;
				else
					LOperation.Undo(this);
			}

			if ((FTransaction != null) && (FTransactionCount <= 0))
			{
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				#endif
				
				FTransaction.Rollback();
				
				#if SQLSTORETIMING
				Store.Counters.Add(new SimpleSQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				#endif

				FTransaction.Dispose();
				FTransaction = null;
				if (FExecuteCommand != null)
					FExecuteCommand.Transaction = null;
			}
		}

		private object NativeToLiteralValue(object AValue)
		{
			if (AValue is bool)
				return ((bool)AValue ? 1 : 0).ToString();
			if (AValue is string)
				return String.Format("'{0}'", ((string)AValue).Replace("'", "''"));
			if (AValue == null)
				return "null";
			return AValue.ToString();
		}

		// TODO: Parameterized statements for undo?
		internal void LogInsert(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FOperationLog.Add(new ModifyOperation(GenerateUndoInsertStatement(ATableName, AColumns, AKey, ARow)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SimpleSQLStoreCounter("LogInsert", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		private string GenerateUndoInsertStatement(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
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

		internal void LogUpdate(string ATableName, List<string> AColumns, List<string> AKey, object[] AOldRow, object[] ANewRow)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FOperationLog.Add(new ModifyOperation(GenerateUndoUpdateStatement(ATableName, AColumns, AKey, AOldRow, ANewRow)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SimpleSQLStoreCounter("LogUpdate", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		private string GenerateUndoUpdateStatement(string ATableName, List<string> AColumns, List<string> AKey, object[] AOldRow, object[] ANewRow)
		{
			StringBuilder LStatement = new StringBuilder();
			LStatement.AppendFormat("update {0} set", ATableName);
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
			{
				if (LIndex > 0)
					LStatement.Append(",");
				LStatement.AppendFormat(" {0} = {1}", AColumns[LIndex], NativeToLiteralValue(AOldRow[LIndex]));
			}
			LStatement.AppendFormat(" where");
			for (int LIndex = 0; LIndex < AKey.Count; LIndex++)
			{
				if (LIndex > 0)
					LStatement.Append(" and");
				LStatement.AppendFormat(" {0} = {1}", AKey[LIndex], NativeToLiteralValue(ANewRow[AColumns.IndexOf(AKey[LIndex])]));
			}
			return LStatement.ToString();
		}

		internal void LogDelete(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FOperationLog.Add(new ModifyOperation(GenerateUndoDeleteStatement(ATableName, AColumns, AKey, ARow)));

			#if SQLSTORETIMING
			Store.Counters.Add(new SimpleSQLStoreCounter("LogDelete", ATableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		private string GenerateUndoDeleteStatement(string ATableName, List<string> AColumns, List<string> AKey, object[] ARow)
		{
			StringBuilder LStatement = new StringBuilder();
			LStatement.AppendFormat("insert into {0} values (", ATableName);
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
			{
				if (LIndex > 0)
					LStatement.Append(", ");
				LStatement.Append(NativeToLiteralValue(ARow[LIndex]));
			}
			LStatement.Append(")");
			return LStatement.ToString();
		}
	}
	
	public class SimpleSQLStoreCursor : System.Object, IDisposable
	{
		internal SimpleSQLStoreCursor(SimpleSQLStoreConnection AConnection, string ATableName, string AIndexName, List<String> AKey, bool AIsUpdatable) : base()
		{
			FConnection = AConnection;
			FTableName = ATableName;
			FIndexName = AIndexName;
			FKey = AKey;
			FIsUpdatable = AIsUpdatable;
			FCursorName = AIndexName + AIsUpdatable.ToString();
			FReader = InternalCreateReader();
			FColumns = new List<string>();
			for (int LIndex = 0; LIndex < FReader.FieldCount; LIndex++)
				FColumns.Add(FReader.GetName(LIndex));
			FKeyIndexes = new List<int>();
			for (int LIndex = 0; LIndex < FKey.Count; LIndex++)
				FKeyIndexes.Add(FColumns.IndexOf(FKey[LIndex]));
		}
		
		protected virtual DbDataReader InternalCreateReader()
		{
			// TODO: Implement...
			throw new NotSupportedException();
		}
		
		#region IDisposable Members
		
		protected virtual void InternalDispose()
		{
		}

		public void Dispose()
		{
			try
			{
				InternalDispose();
			}
			finally
			{
				if (FReader != null)
				{
					FReader.Dispose();
					FReader = null;
				}
				
				FConnection = null;
			}
		}

		#endregion
		
		private SimpleSQLStoreConnection FConnection;
		public SimpleSQLStoreConnection Connection { get { return FConnection; } }

		private DbDataReader FReader;
		protected DbDataReader Reader { get { return FReader; } }
		
		private string FTableName;
		public string TableName { get { return FTableName; } }
		
		private string FIndexName;
		public string IndexName { get { return FIndexName; } }
		
		private bool FIsUpdatable;
		public bool IsUpdatable { get { return FIsUpdatable; } }

		private string FCursorName;
		public string CursorName { get { return FCursorName; } }

		private List<String> FColumns;
		private List<String> FKey;
		private List<int> FKeyIndexes;
		private object[] FCurrentRow;
		private object[] FStartValues;
		private object[] FEndValues;
		private bool FIsOnRow;
		
		public void SetRange(object[] AStartValues, object[] AEndValues)
		{
			FCurrentRow = null;
			FStartValues = AStartValues;
			FEndValues = AEndValues;
			
			if (FStartValues != null)
				InternalSeek(AStartValues);
			else
				InternalFirst();
				
			FIsOnRow = false;
		}
		
		/// <summary>Returns true if ALeftValue is less than ARightValue, false otherwise.</summary>
		private bool IsLessThan(object ALeftValue, object ARightValue)
		{
			if (ALeftValue is int)
				return (int)ALeftValue < (int)ARightValue;
				
			if (ALeftValue is string)
				return String.Compare((string)ALeftValue, (string)ARightValue, true) < 0;
				
			if (ALeftValue is bool)
				return !(bool)ALeftValue && (bool)ARightValue;
				
			return false;
		}
		
		private int CompareKeyValues(object ALeftValue, object ARightValue)
		{
			if (ALeftValue is string)
				return String.Compare((string)ALeftValue, (string)ARightValue, true);
				
			if (ALeftValue is IComparable)
				return ((IComparable)ALeftValue).CompareTo(ARightValue);
				
			Error.Fail("Storage keys not comaparable");

			return 0;
		}
		
		/// <summary>Returns -1 if ALeftKey is less than ARightKey, 1 if ALeftKey is greater than ARightKey, and 0 if ALeftKey is equal to ARightKey.</summary>
		/// <remarks>The values of ALeftKey and ARightKey are expected to be native, not store, values.</remarks>
		private int CompareKeys(object[] ALeftKey, object[] ARightKey)
		{
			int LResult = 0;
			for (int LIndex = 0; LIndex < (ALeftKey.Length >= ARightKey.Length ? ALeftKey.Length : ARightKey.Length); LIndex++)
			{
				if ((LIndex >= ALeftKey.Length) || (ALeftKey[LIndex] == null))
					return -1;
				else if ((LIndex >= ARightKey.Length) || (ARightKey[LIndex] == null))
					return 1;
				else
				{
					LResult = CompareKeyValues(ALeftKey[LIndex], ARightKey[LIndex]);
					if (LResult != 0)
						return LResult;
				}
			}
			return LResult;
		}
		
		/// <summary>Returns -1 if the key values for the current row are less than ACompareKey, 1 if the key values for the current row are greater than ACompareKey, and 0 is the key values for the current row are equal to ACompareKey.</summary>
		/// <remarks>The values of ACompareKey are expected to be native, not store, values.</remarks>
		private int CompareKeys(object[] ACompareKey)
		{
			int LResult = 0;
			object LIndexValue;
			for (int LIndex = 0; LIndex < ACompareKey.Length; LIndex++)
			{
				LIndexValue = InternalGetValue(FKeyIndexes[LIndex]);
				if (LIndexValue == null)
					return -1;
				else if ((LIndex >= ACompareKey.Length) || (ACompareKey[LIndex] == null))
					return 1;
				else
				{
					LResult = CompareKeyValues(LIndexValue, ACompareKey[LIndex]);
					if (LResult != 0)
						return LResult;
				}
			}
			return LResult;
		}
		
		private void CheckIsOnRow()
		{
			if (!FIsOnRow)
				throw new CatalogException(CatalogException.Codes.CursorHasNoCurrentRow, ErrorSeverity.System);
		}
		
		private object InternalGetValue(int AIndex)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return StoreToNativeValue(FReader.GetValue(AIndex)); 
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SimpleSQLStoreCounter("GetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		protected virtual void InternalSetValue(int AIndex, object AValue)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			// TODO: Implement...
			throw new NotSupportedException();
			//FReader.SetValue(AIndex, NativeToStoreValue(AValue));

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("SetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public object this[int AIndex]
		{
			get 
			{
				CheckIsOnRow();

				return InternalGetValue(AIndex);
			}
			set
			{
				CheckIsOnRow();
				
				// Save a copy of the current row before any edits if we are in a nested transaction
				if ((FCurrentRow == null) && (FConnection.TransactionCount > 1))
					FCurrentRow = Select();
					
				InternalSetValue(AIndex, value);
			}
		}
		
		private object[] InternalSelect()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			object[] LRow = new object[FReader.FieldCount];
			for (int LIndex = 0; LIndex < LRow.Length; LIndex++)
				LRow[LIndex] = StoreToNativeValue(FReader.GetValue(LIndex));

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Select", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif

			return LRow;
		}
		
		public object[] Select()
		{
			CheckIsOnRow();
			return InternalSelect();
		}
		
		protected virtual bool InternalNext()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				// TODO: Need to introduce ReaderDirection...
				return FReader.Read();
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Next", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		public bool Next()
		{
			FCurrentRow = null;
			FIsOnRow = InternalNext();
			if ((FIsOnRow) && (FEndValues != null) && ((CompareKeys(FEndValues) > 0) || (CompareKeys(FStartValues) < 0)))
				FIsOnRow = false;

			return FIsOnRow;
		}
		
		protected virtual void InternalLast()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			// TODO: Implement...
			throw new NotSupportedException();
			//FResultSet.ReadLast();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Last", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public void Last()
		{
			FCurrentRow = null;
			if (FEndValues != null)
				InternalSeek(FEndValues);
			else
				InternalLast();
			FIsOnRow = false;
		}
		
		protected virtual bool InternalPrior()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				// TODO: Implement...
				throw new NotSupportedException();
				// FResultSet.ReadPrevious();
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Prior", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		public bool Prior()
		{
			FCurrentRow = null;
			FIsOnRow = InternalPrior();
			if (FIsOnRow && (FStartValues != null) && ((CompareKeys(FStartValues) < 0) || (CompareKeys(FEndValues) > 0)))
				FIsOnRow = false;

			return FIsOnRow;
		}

		protected virtual void InternalFirst()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			// TODO: Implement...
			throw new NotSupportedException();
			//FResultSet.ReadFirst();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("First", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public void First()
		{
			FCurrentRow = null;
			if (FStartValues != null)
				InternalSeek(FStartValues);
			else
				InternalFirst();
			FIsOnRow = false;
		}
		
		public object NativeToStoreValue(object AValue)
		{
			if (AValue is bool)
				return (byte)((bool)AValue ? 1 : 0);
			return AValue;
		}
		
		public object StoreToNativeValue(object AValue)
		{
			if (AValue is byte)
				return ((byte)AValue) == 1;
			if (AValue is DBNull)
				return null;
			return AValue;
		}
		
		protected virtual void InternalInsert(object[] ARow)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			// TODO: Implement...
			throw new NotSupportedException();

			//SqlCeUpdatableRecord LRecord = FResultSet.CreateRecord();
			//for (int LIndex = 0; LIndex < ARow.Length; LIndex++)
			//	LRecord.SetValue(LIndex, NativeToStoreValue(ARow[LIndex]));

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("CreateRecord", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			LStartTicks = TimingUtility.CurrentTicks;
			#endif

			//FResultSet.Insert(LRecord, DbInsertOptions.KeepCurrentPosition);

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Insert", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public void Insert(object[] ARow)
		{
			InternalInsert(ARow);
			FIsOnRow = false;

			if (FConnection.TransactionCount > 1)
				FConnection.LogInsert(FTableName, FColumns, FKey, ARow);
		}
		
		protected virtual void InternalUpdate()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			// TODO: Implement...
			throw new NotSupportedException();
			//FResultSet.Update();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Update", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public void Update()
		{
			CheckIsOnRow();
			
			object[] LNewRow = null;
			if (FConnection.TransactionCount > 1)
			{
				if (FCurrentRow == null)
					FCurrentRow = InternalSelect();
				LNewRow = InternalSelect();
			}

			InternalUpdate();

			if (FConnection.TransactionCount > 1)
				FConnection.LogUpdate(FTableName, FColumns, FKey, FCurrentRow, LNewRow);

			FCurrentRow = null;
		}
		
		protected virtual void InternalDelete()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			// TODO: Implement...
			throw new NotSupportedException();
			//FResultSet.Delete();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Delete", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public void Delete()
		{
			CheckIsOnRow();
			
			if ((FCurrentRow == null) && (FConnection.TransactionCount > 1))
				FCurrentRow = Select();
				
			InternalDelete();

			if (FConnection.TransactionCount > 1)
				FConnection.LogDelete(FTableName, FColumns, FKey, FCurrentRow);

			FCurrentRow = null;
			FIsOnRow = false;
		}
		
		protected virtual bool InternalSeek(object[] AKey)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				// TODO: Implement...
				throw new NotSupportedException();

				//object[] LKey = new object[AKey.Length];
				//for (int LIndex = 0; LIndex < LKey.Length; LIndex++)
				//	LKey[LIndex] = NativeToStoreValue(AKey[LIndex]);
				//return FResultSet.Seek(DbSeekOptions.FirstEqual, LKey);
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Seek", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		public bool FindKey(object[] AKey)
		{
			FCurrentRow = null;
			
			if ((FStartValues != null) && ((CompareKeys(FStartValues, AKey) < 0) || (CompareKeys(FEndValues, AKey) > 0)))
				return false;
				
			if (InternalSeek(AKey))
			{
				// TODO: I am fairly certain this code is wrong and it's only not been a problem because we don't use this behavior in the catalog store
				FIsOnRow = InternalNext();
				return FIsOnRow;
			}
			
			FIsOnRow = InternalNext();
			return false;
		}
		
		public void FindNearest(object[] AKey)
		{
			FCurrentRow = null;
			if (FStartValues != null)
			{
				if (CompareKeys(FStartValues, AKey) < 0)
				{
					InternalSeek(FStartValues);
					FIsOnRow = InternalNext();
				}
				else if (CompareKeys(FEndValues, AKey) > 0)
				{
					InternalSeek(FEndValues);
					FIsOnRow = InternalPrior();
				}
			}
			else
			{
				InternalSeek(AKey);
				FIsOnRow = InternalNext();
			}
		}
	}
}

