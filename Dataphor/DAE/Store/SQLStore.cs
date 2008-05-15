//#define SQLSTORETIMING

/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Abstract SQL Store
	
	Defines the expected behavior for a simple storage device that uses a SQL DBMS as it's backend.
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

namespace Alphora.Dataphor.DAE.Store
{
	public class SQLStoreCounter
	{
		public SQLStoreCounter(string AOperation, string ATableName, string AIndexName, bool AIsMatched, bool AIsRanged, bool AIsUpdatable, TimeSpan ADuration)
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
	
	public class SQLStoreCounters : List<SQLStoreCounter> {}

	public abstract class SQLStore : System.Object
	{
		private string FConnectionString;
		public string ConnectionString
		{
			get { return FConnectionString; }
			set 
			{ 
				if (FInitialized)
					throw new StoreException(StoreException.Codes.StoreInitialized);
				FConnectionString = value; 
			}
		}
		
		private bool FInitialized;
		public bool Initialized { get { return FInitialized; } }

		protected abstract void InternalInitialize();
		
		public void Initialize()
		{
			if (FInitialized)
				throw new StoreException(StoreException.Codes.StoreInitialized);
				
			InternalInitialize();
			
			FInitialized = true;
		}
		
		public abstract SQLConnection GetSQLConnection();

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
		
		private SQLStoreCounters FCounters = new SQLStoreCounters();
		public SQLStoreCounters Counters { get { return FCounters; } }
		
		protected abstract SQLStoreConnection InternalConnect();

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
		public SQLStoreConnection Connect()
		{
			lock (this)
			{
				if ((FMaxConnections > 0) && (FConnectionCount >= FMaxConnections))
					throw new StoreException(StoreException.Codes.MaximumConnectionsExceeded, ErrorSeverity.Environment);
				FConnectionCount++;
			}
			
			try
			{
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
				#endif

					return InternalConnect();

				#if SQLSTORETIMING
				}
				finally
				{
					Counters.Add(new SQLStoreCounter("Connect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				}
				#endif
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
	}
	
	// TODO: Move timing functionality into the base, not the internals
	public abstract class SQLStoreConnection : System.Object, IDisposable
	{
		protected internal SQLStoreConnection(SQLStore AStore) : base()
		{
			FStore = AStore;
			FConnection = InternalCreateConnection();
			FConnection.Open();
		}
		
		protected abstract DbConnection InternalCreateConnection();
		
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
		private DbConnection FConnection;
		/// <summary>This is the internal connection to the server housing the catalog store.</summary>
		protected DbConnection Connection { get { return FConnection; } }
		
		/// <summary>The transaction object for this connection.</summary>
		private DbTransaction FTransaction;
		
		/// <summary>Returns whether or not the store has a table of the given name.</summary>
		public virtual bool HasTable(string ATableName)
		{
			// TODO: Implement...
			// To implement this generically would require dealing with all the provider-specific 'schema collection' tables
			// returned from a call to GetSchema. ADO.NET does not define a common schema collection for tables.
			throw new NotSupportedException();
		}

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
		
		/// <summary>Returns a new command that can be used to open readers on this connection.</summary>
		public DbCommand GetReaderCommand()
		{
			return FConnection.CreateCommand();
		}
		
		public void ExecuteScript(string AScript)
		{
			List<String> LStatements = SQLStore.ProcessBatches(AScript);
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
			Store.Counters.Add(new SQLStoreCounter("ExecuteNonQuery", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
				Store.Counters.Add(new SQLStoreCounter("ExecuteScalar", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		protected internal DbDataReader ExecuteReader(string AStatement)
		{
			DbCommand LReaderCommand = GetReaderCommand();
			LReaderCommand.CommandType = CommandType.Text;
			LReaderCommand.CommandText = AStatement;

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return LReaderCommand.ExecuteReader();
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteReader", AStatement, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		protected virtual SQLStoreCursor InternalOpenCursor(string ATableName, SQLIndex AIndex, bool AIsUpdatable)
		{
			return
				new SQLStoreCursor
				(
					this,
					ATableName,
					AIndex,
					AIsUpdatable
				);
		}
		
		public SQLStoreCursor OpenCursor(string ATableName, SQLIndex AIndex, bool AIsUpdatable)
		{
			return InternalOpenCursor(ATableName, AIndex, AIsUpdatable);
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
				Store.Counters.Add(new SQLStoreCounter("BeginTransaction", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
				Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
				SQLStoreOperation LOperation = FOperationLog[LIndex];
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
				Store.Counters.Add(new SQLStoreCounter("Disconnect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				#endif

				FTransaction.Dispose();
				FTransaction = null;
				if (FExecuteCommand != null)
					FExecuteCommand.Transaction = null;
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
	
	// TODO: Move timing functionality into the base, not the internals
	public class SQLStoreCursor : System.Object, IDisposable
	{
		protected internal SQLStoreCursor(SQLStoreConnection AConnection, string ATableName, SQLIndex AIndex, bool AIsUpdatable) : base()
		{
			FConnection = AConnection;
			FTableName = ATableName;
			FIndex = AIndex;
			FKey = new List<string>(AIndex.Columns.Count);
			for (int LIndex = 0; LIndex < AIndex.Columns.Count; LIndex++)
				FKey.Add(AIndex.Columns[LIndex].Name);
			FIsUpdatable = AIsUpdatable;
			FCursorName = FIndex.Name + AIsUpdatable.ToString();
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
				DisposeReader();
				
				FConnection = null;
			}
		}

		#endregion
		
		private SQLStoreConnection FConnection;
		public SQLStoreConnection Connection { get { return FConnection; } }

		private DbDataReader FReader;
		protected DbDataReader Reader { get { return FReader; } }
		
		private string FTableName;
		public string TableName { get { return FTableName; } }
		
		private SQLIndex FIndex;
		public SQLIndex Index { get { return FIndex; } }
		
		public string IndexName { get { return FIndex.Name; } }
		
		private bool FIsUpdatable;
		public bool IsUpdatable { get { return FIsUpdatable; } }

		private string FCursorName;
		public string CursorName { get { return FCursorName; } }

		private List<String> FColumns;
		private List<String> FKey;
		private List<int> FKeyIndexes;
		private object[] FCurrentRow;
		private object[] FEditingRow;
		private object[] FStartValues;
		private object[] FEndValues;
		private bool FIsOnRow;
		
		private void DisposeReader()
		{
			if (FReader != null)
			{
				FReader.Dispose();
				FReader = null;
			}
		}
		
		private void CreateReader(object[] AOrigin, bool AForward, bool AInclusive)
		{
			FReader = InternalCreateReader(AOrigin, AForward, AInclusive);
			FOrigin = AOrigin;
			FForward = AForward;
			FInclusive = AInclusive;
			if (FColumns == null)
			{
				FColumns = new List<string>();
				for (int LIndex = 0; LIndex < FReader.FieldCount; LIndex++)
					FColumns.Add(FReader.GetName(LIndex));
				FKeyIndexes = new List<int>();
				for (int LIndex = 0; LIndex < FKey.Count; LIndex++)
					FKeyIndexes.Add(FColumns.IndexOf(FKey[LIndex]));
			}
		}
		
		protected virtual DbDataReader InternalCreateReader(object[] AOrigin, bool AForward, bool AInclusive)
		{
			return FConnection.ExecuteReader(GetReaderStatement(FTableName, FIndex.Columns, AOrigin, AForward, AInclusive));
		}
		
		private object[] FOrigin;
		private bool FForward;
		private bool FInclusive;
		private List<object[]> FBuffer;
		private bool FBufferForward;
		private int FBufferIndex;
		
		protected void EnsureReader()
		{
			if (FReader == null)
				EnsureReader(null, true, true);
		}
		
		protected virtual void EnsureReader(object[] AOrigin, bool AForward, bool AInclusive)
		{
			if (((FOrigin == null) != (AOrigin == null)) || (CompareKeys(FOrigin, AOrigin) != 0) || (FForward != AForward) || (FInclusive != AInclusive))
				DisposeReader();
				
			if (FReader == null)
				CreateReader(AOrigin, AForward, AInclusive);
		}
		
		#region Ranged Reader Statement Generation

		/*
			for each column in the order descending
				if the current order column is also in the origin
					[or]
					for each column in the origin less than the current order column
						[and] 
						current origin column = current origin value
                        
					[and]
					if the current order column is ascending xor the requested set is forward
						if requested set is inclusive and current order column is the last origin column
							current order column <= current origin value
						else
							current order column < current origin value
					else
						if requested set is inclusive and the current order column is the last origin column
							current order column >= current origin value
						else
							current order column > current origin value
                            
					for each column in the order greater than the current order column
						if the current order column does not include nulls
							and current order column is not null
				else
					if the current order column does not include nulls
						[and] current order column is not null
		*/
		
		protected virtual string GetReaderStatement(string ATableName, SQLIndexColumns AIndexColumns, object[] AOrigin, bool AForward, bool AInclusive)
		{
			StringBuilder LStatement = new StringBuilder();
			
			LStatement.AppendFormat("select * from {0}", ATableName);

			for (int LIndex = AIndexColumns.Count - 1; LIndex >= 0; LIndex--)
			{
				if ((AOrigin != null) && (LIndex < AOrigin.Length))
				{
					if (LIndex == AOrigin.Length - 1)
						LStatement.AppendFormat(" where ");
					else
						LStatement.Append(" or ");
					
					LStatement.Append("(");

					for (int LOriginIndex = 0; LOriginIndex < LIndex; LOriginIndex++)
					{
						if (LOriginIndex > 0)
							LStatement.Append(" and ");
							
						LStatement.AppendFormat("({0} = {1})", AIndexColumns[LOriginIndex].Name, FConnection.NativeToLiteralValue(AOrigin[LOriginIndex]));
					}
					
					if (LIndex > 0)
						LStatement.Append(" and ");
						
					LStatement.AppendFormat
					(
						"({0} {1} {2})", 
						AIndexColumns[LIndex].Name, 
						(AIndexColumns[LIndex].Ascending == AForward)
							? (((LIndex == (AOrigin.Length - 1)) && AInclusive) ? ">=" : ">")
							: (((LIndex == (AOrigin.Length - 1)) && AInclusive) ? "<=" : "<"),
						FConnection.NativeToLiteralValue(AOrigin[LIndex])
					);
					
					LStatement.Append(")");
				}
			}
			
			LStatement.AppendFormat(" order by ");
			
			for (int LIndex = 0; LIndex < AIndexColumns.Count; LIndex++)
			{
				if (LIndex > 0)
					LStatement.Append(", ");
				LStatement.AppendFormat("{0} {1}", AIndexColumns[LIndex].Name, AIndexColumns[LIndex].Ascending == AForward ? "asc" : "desc");
			}
			
			return LStatement.ToString();
		}
		
		#endregion
		
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
			if ((ALeftKey == null) && (ARightKey == null))
				return 0;
				
			if (ALeftKey == null)
				return -1;
				
			if (ARightKey == null)
				return 1;
				
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
			if (!FIsOnRow && (ACompareKey == null))
				return 0;
				
			if (!FIsOnRow)
				return -1;
				
			if (ACompareKey == null)
				return 1;
				
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
		
		private object[] RowToKey(object[] ARow)
		{
			object[] LKey = new object[FKey.Count];
			for (int LIndex = 0; LIndex < FKey.Count; LIndex++)
				LKey[LIndex] = ARow[FKeyIndexes[LIndex]];
			return LKey;
		}
		
		/// <summary>
		/// Returns -1 if the key values of the left row are less than the key values of the right row, 1 if they are greater, and 0 if they are equal.
		/// </summary>
		/// <remarks>The values of ALeftRow and ARightRow are expected to be native, not store, values.</remarks>
		private int CompareRows(object[] ALeftRow, object[] ARightRow)
		{
			return CompareKeys(RowToKey(ALeftRow), RowToKey(ARightRow));
		}
		
		private void CheckIsOnRow()
		{
			if (!FIsOnRow)
				throw new StoreException(StoreException.Codes.CursorHasNoCurrentRow, ErrorSeverity.System);
		}
		
		protected virtual object InternalGetValue(int AIndex)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				if (FEditingRow != null)
					return FEditingRow[AIndex];					
					
				if ((FBuffer != null) && (FBufferIndex >= 0) && (FBufferIndex < FBuffer.Count))
					return FBuffer[FBufferIndex][AIndex];
					
				return StoreToNativeValue(FReader.GetValue(AIndex)); 
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("GetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		protected virtual void InternalSetValue(int AIndex, object AValue)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			if (FEditingRow == null)
				FEditingRow = InternalSelect();
				
			FEditingRow[AIndex] = AValue;

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("SetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
		
		protected virtual object[] InternalSelect()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			object[] LRow = new object[FColumns.Count];
			for (int LIndex = 0; LIndex < LRow.Length; LIndex++)
				LRow[LIndex] = InternalGetValue(LIndex);

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Select", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif

			return LRow;
		}
		
		public object[] Select()
		{
			CheckIsOnRow();
			return InternalSelect();
		}
		
		public object[] SelectKey()
		{
			if (!FIsOnRow)
				return null;
				
			object[] LKey = new object[FKey.Count];
			for (int LIndex = 0; LIndex < LKey.Length; LIndex++)
				LKey[LIndex] = InternalGetValue(FKeyIndexes[LIndex]);
				
			return LKey;
		}
		
		protected virtual object[] InternalReadRow()
		{
			object[] LRow = new object[FReader.FieldCount];
			for (int LIndex = 0; LIndex < FReader.FieldCount; LIndex++)
				LRow[LIndex] = StoreToNativeValue(FReader.GetValue(LIndex));
				
			return LRow;
		}
		
		protected virtual bool InternalNext()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif

				FEditingRow = null;
				
				if ((FBuffer != null) && ((FBufferForward && (FBufferIndex < FBuffer.Count - 1)) || (!FBufferForward && (FBufferIndex > 0))))
				{
					if (FBufferForward)
						FBufferIndex++;
					else
						FBufferIndex--;
					return true;
				}
				
				if ((FReader == null) || !FForward)
					EnsureReader(SelectKey(), true, !FIsOnRow);
				
				if ((FBuffer == null) && !FIndex.IsUnique)
				{
					FBuffer = new List<object[]>();
					FBufferForward = true;
					FBufferIndex = -1;
				}
				
				if (FReader.Read())
				{
					if (!FIndex.IsUnique)
					{
						object[] LRow = InternalReadRow();
						if ((FBuffer != null) && (FBufferIndex >= 0) && (FBufferIndex < FBuffer.Count))
							if (CompareRows(FBuffer[FBufferIndex], LRow) != 0)
							{
								FBuffer.Clear();
								FBufferForward = true;
								FBufferIndex = -1;
							}

						if (FBufferForward)
							FBuffer.Insert(++FBufferIndex, LRow);
						else
							FBuffer.Insert(FBufferIndex, LRow);
					}

					return true;
				}
				
				return false;

			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Next", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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

			FEditingRow = null;			
			DisposeReader();
			EnsureReader(null, false, true);
			if (!FIndex.IsUnique)
			{
				if (FBuffer == null)
					FBuffer = new List<object[]>();
				else
					FBuffer.Clear();
				FBufferForward = false;
				FBufferIndex = FBuffer.Count;
			}

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Last", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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

				FEditingRow = null;
				
				if ((FBuffer != null) && ((FBufferForward && (FBufferIndex > 0)) || (!FBufferForward && (FBufferIndex < FBuffer.Count))))
				{
					if (FBufferForward)
						FBufferIndex--;
					else
						FBufferIndex++;
					return true;
				}
				
				if ((FReader == null) || FForward)
					EnsureReader(SelectKey(), false, !FIsOnRow);
				
				if (!FIndex.IsUnique && (FBuffer == null))
				{
					FBuffer = new List<object[]>();
					FBufferForward = false;
					FBufferIndex = FBuffer.Count;
				}
				
				if (FReader.Read())
				{
					if (!FIndex.IsUnique)
					{
						object[] LRow = InternalReadRow();
						if ((FBuffer != null) && (FBufferIndex >= 0) && (FBufferIndex < FBuffer.Count))
							if (CompareRows(FBuffer[FBufferIndex], LRow) != 0)
							{
								FBuffer.Clear();
								FBufferForward = false;
								FBufferIndex = FBuffer.Count;
							}

						if (FBufferForward)
							FBuffer.Insert(FBufferIndex, LRow);
						else
							FBuffer.Insert(++FBufferIndex, LRow);
					}
					
					return true;
				}
				
				return false;
				
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Prior", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
			
			FEditingRow = null;
			DisposeReader();
			EnsureReader(null, true, true);
			
			if (!FIndex.IsUnique)
			{
				if (FBuffer == null)
					FBuffer = new List<object[]>();
				else
					FBuffer.Clear();
				FBufferForward = true;
				FBufferIndex = -1;
			}

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("First", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
			
			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("CreateRecord", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			// TODO: This will need to dispose and re-open the reader, as well as clear the buffer
			Connection.PerformInsert(FTableName, FColumns, FKey, ARow);
			
			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Insert", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public void Insert(object[] ARow)
		{
			EnsureReader();
			InternalInsert(ARow);
			FIsOnRow = false; // TODO: Is this correct?

			if (FConnection.TransactionCount > 1)
				FConnection.LogInsert(FTableName, FColumns, FKey, ARow);
		}
		
		protected virtual void InternalUpdate()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			if (FCurrentRow == null)
				FCurrentRow = InternalSelect();
			
			// TODO: This will need to dispose and re-open the reader, as well as clear the buffer
			Connection.PerformUpdate(FTableName, FColumns, FKey, FCurrentRow, FEditingRow);

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Update", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
			
			FEditingRow = null;
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
			
			if (FCurrentRow == null)
				FCurrentRow = InternalSelect();
			
			// TODO: This will need to dispose and re-open the reader, as well as clear the buffer	
			Connection.PerformDelete(FTableName, FColumns, FKey, FCurrentRow);
			
			FEditingRow = null;

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Delete", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public void Delete()
		{
			CheckIsOnRow();
			
			if ((FCurrentRow == null) && (FConnection.TransactionCount > 1))
				FCurrentRow = InternalSelect();
				
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
				FEditingRow = null;
				
				if (!FIndex.IsUnique)
				{
					if (FBuffer == null)
						FBuffer = new List<object[]>();
					else
						FBuffer.Clear();
					FBufferForward = true;
					FBufferIndex = -1;
				}
				
				object[] LKey = new object[AKey.Length];
				for (int LIndex = 0; LIndex < LKey.Length; LIndex++)
					LKey[LIndex] = NativeToStoreValue(AKey[LIndex]);
				EnsureReader(AKey, true, true);
				return false;
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Seek", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
				FIsOnRow = InternalNext();
				return FIsOnRow;
			}
			
			FIsOnRow = InternalNext();
			return FIsOnRow && (CompareKeys(AKey) == 0);
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

