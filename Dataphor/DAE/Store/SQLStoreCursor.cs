/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Abstract SQL Store Cursor
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
		
		protected internal SQLStoreCursor(SQLStoreConnection AConnection, string ATableName, List<string> AColumns, SQLIndex AIndex, bool AIsUpdatable)
			: this(AConnection, ATableName, AIndex, AIsUpdatable)
		{
			if (AColumns != null)
			{
				FColumns = AColumns;
				FKeyIndexes = new List<int>();
				for (int LIndex = 0; LIndex < FKey.Count; LIndex++)
					FKeyIndexes.Add(FColumns.IndexOf(FKey[LIndex]));
			}
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
		
		private SQLCommand FReaderCommand;
		
		private SQLCursor FReader;
		protected SQLCursor Reader { get { return FReader; } }
		
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
		private bool FIsOnSimulatedCrack;
		
		private void DisposeReader()
		{
			if (FReader != null)
			{
				FReader.Dispose();
				FReader = null;
			}

			if (FReaderCommand != null)
			{
				FReaderCommand.Dispose();
				FReaderCommand = null;
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
				for (int LIndex = 0; LIndex < FReader.ColumnCount; LIndex++)
					FColumns.Add(FReader.GetColumnName(LIndex));
				FKeyIndexes = new List<int>();
				for (int LIndex = 0; LIndex < FKey.Count; LIndex++)
					FKeyIndexes.Add(FColumns.IndexOf(FKey[LIndex]));
			}
		}
		
		protected virtual SQLCursor InternalCreateReader(object[] AOrigin, bool AForward, bool AInclusive)
		{
			return FConnection.ExecuteReader(GetReaderStatement(FTableName, FIndex.Columns, AOrigin, AForward, AInclusive), out FReaderCommand);
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
		
		#region Buffer Management
		
		private void ClearBuffer()
		{
			if (!FIndex.IsUnique)
			{
				if (FBuffer == null)
					FBuffer = new List<object[]>();
				else
					FBuffer.Clear();
				FBufferForward = true;
				FBufferIndex = -1;
			}
		}
		
		#endregion
		
		public void SetRange(object[] AStartValues, object[] AEndValues)
		{
			FCurrentRow = null;
			FStartValues = AStartValues;
			FEndValues = AEndValues;
			
			if (FStartValues != null)
				FIsOnSimulatedCrack = InternalSeek(AStartValues);
			else
				FIsOnSimulatedCrack = InternalFirst();
			
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
			if (FEditingRow != null)
				return FEditingRow[AIndex];					
				
			if ((FBuffer != null) && (FBufferIndex >= 0) && (FBufferIndex < FBuffer.Count))
				return FBuffer[FBufferIndex][AIndex];
				
			return StoreToNativeValue(FReader[AIndex]);
		}
		
		protected virtual void InternalSetValue(int AIndex, object AValue)
		{
			if (FEditingRow == null)
				FEditingRow = InternalSelect();
				
			FEditingRow[AIndex] = AValue;
		}
		
		public object this[int AIndex]
		{
			get 
			{
				CheckIsOnRow();

				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
				#endif

					return InternalGetValue(AIndex);

				#if SQLSTORETIMING
				}
				finally
				{
					Connection.Store.Counters.Add(new SQLStoreCounter("GetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				}
				#endif
			}
			set
			{
				CheckIsOnRow();
				
				// Save a copy of the current row before any edits if we are in a nested transaction
				if ((FCurrentRow == null) && (FConnection.TransactionCount > 1))
					FCurrentRow = Select();
					
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				#endif
				
				InternalSetValue(AIndex, value);

				#if SQLSTORETIMING
				Connection.Store.Counters.Add(new SQLStoreCounter("SetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
				#endif
			}
		}
		
		protected virtual object[] InternalSelect()
		{
			object[] LRow = new object[FColumns.Count];
			for (int LIndex = 0; LIndex < LRow.Length; LIndex++)
				LRow[LIndex] = InternalGetValue(LIndex);

			return LRow;
		}
		
		public object[] Select()
		{
			CheckIsOnRow();
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif

				return InternalSelect();

			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Select", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
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
			object[] LRow = new object[FReader.ColumnCount];
			for (int LIndex = 0; LIndex < FReader.ColumnCount; LIndex++)
				LRow[LIndex] = StoreToNativeValue(FReader[LIndex]);
				
			return LRow;
		}
		
		protected virtual bool InternalNext()
		{
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
				EnsureReader(FIsOnRow ? SelectKey() : FStartValues, true, !FIsOnRow);
			
			if ((FBuffer == null) && !FIndex.IsUnique)
			{
				FBuffer = new List<object[]>();
				FBufferForward = true;
				FBufferIndex = -1;
			}
			
			if (FReader.Next())
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
		}
		
		public bool Next()
		{
			FCurrentRow = null;
	
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif

				if (FIsOnSimulatedCrack)
				{
					FIsOnSimulatedCrack = false;
					FIsOnRow = true;
				}
				else
					FIsOnRow = InternalNext();

			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Next", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif

			if ((FIsOnRow) && (FEndValues != null) && ((CompareKeys(FEndValues) > 0) || (CompareKeys(FStartValues) < 0)))
				FIsOnRow = false;

			return FIsOnRow;
		}
		
		protected virtual bool InternalLast()
		{
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
			return false;
		}
		
		public void Last()
		{
			FCurrentRow = null;

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			if (FEndValues != null)
				FIsOnSimulatedCrack = InternalSeek(FEndValues);
			else
				FIsOnSimulatedCrack = InternalLast();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Last", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
			
			FIsOnRow = false;
		}
		
		protected virtual bool InternalPrior()
		{
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
				EnsureReader(FIsOnRow ? SelectKey() : FEndValues, false, !FIsOnRow);
			
			if (!FIndex.IsUnique && (FBuffer == null))
			{
				FBuffer = new List<object[]>();
				FBufferForward = false;
				FBufferIndex = FBuffer.Count;
			}
			
			if (FReader.Next())
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
		}
		
		public bool Prior()
		{
			FCurrentRow = null;

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif

				if (FIsOnSimulatedCrack)
				{	
					FIsOnSimulatedCrack = false;
					FIsOnRow = true;
				}
				else
					FIsOnRow = InternalPrior();
				
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Prior", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif

			if (FIsOnRow && (FStartValues != null) && ((CompareKeys(FStartValues) < 0) || (CompareKeys(FEndValues) > 0)))
				FIsOnRow = false;

			return FIsOnRow;
		}

		protected virtual bool InternalFirst()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			FEditingRow = null;
			DisposeReader();
			EnsureReader(null, true, true);
			ClearBuffer();
			return false;

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("First", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		public void First()
		{
			FCurrentRow = null;

			if (FStartValues != null)
				FIsOnSimulatedCrack = InternalSeek(FStartValues);
			else
				FIsOnSimulatedCrack = InternalFirst();
			
			FIsOnRow = false;
		}
		
		public virtual object NativeToStoreValue(object AValue)
		{			
			return AValue;
		}

		public virtual object StoreToNativeValue(object AValue)
		{			
			if (AValue is DBNull)
				return null;
			return AValue;
		}
		
		protected virtual bool InternalInsert(object[] ARow)
		{
			DisposeReader();
			Connection.PerformInsert(FTableName, FColumns, FKey, ARow);
			//EnsureReader(RowToKey(ARow), true, true);
			ClearBuffer();
			return false;
		}
		
		public void Insert(object[] ARow)
		{
			//EnsureReader();

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			Connection.Store.Counters.Add(new SQLStoreCounter("CreateRecord", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			FIsOnRow = InternalInsert(ARow);

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Insert", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif

			if (FConnection.TransactionCount > 1)
				FConnection.LogInsert(FTableName, FColumns, FKey, ARow);
		}
		
		protected virtual bool InternalUpdate()
		{
			if (FCurrentRow == null)
				FCurrentRow = InternalSelect();
			
			DisposeReader();
			Connection.PerformUpdate(FTableName, FColumns, FKey, FCurrentRow, FEditingRow);
			//EnsureReader(RowToKey(FEditingRow), true, true);
			ClearBuffer();
			
			return false;
		}
		
		public void Update()
		{
			CheckIsOnRow();
			
			object[] LNewRow = null;
			if (FConnection.TransactionCount > 1)
			{
				if (FCurrentRow == null)
					FCurrentRow = InternalSelect();
					
				if (Connection.Store.SupportsUpdatableCursor)
					LNewRow = InternalSelect();
				else
					LNewRow = FEditingRow;
			}

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			FIsOnRow = InternalUpdate();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Update", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
			
			if (FConnection.TransactionCount > 1)
				FConnection.LogUpdate(FTableName, FColumns, FKey, FCurrentRow, LNewRow);

			FCurrentRow = null;
			FEditingRow = null;
		}
		
		protected virtual bool InternalDelete()
		{
			if (FCurrentRow == null)
				FCurrentRow = InternalSelect();
			
			DisposeReader();
			Connection.PerformDelete(FTableName, FColumns, FKey, FCurrentRow);
			//EnsureReader(RowToKey(FCurrentRow), true, true);
			ClearBuffer();
			
			return false;
		}
		
		public void Delete()
		{
			CheckIsOnRow();
			
			if ((FCurrentRow == null) && (FConnection.TransactionCount > 1))
				FCurrentRow = InternalSelect();
				
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			FIsOnRow = InternalDelete();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Delete", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif

			if (FConnection.TransactionCount > 1)
				FConnection.LogDelete(FTableName, FColumns, FKey, FCurrentRow);

			FCurrentRow = null;
			FEditingRow = null;
		}
		
		protected virtual bool InternalSeek(object[] AKey)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				FEditingRow = null;
				ClearBuffer();
				
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
			FIsOnSimulatedCrack = false;
			
			if ((FStartValues != null) && ((CompareKeys(FStartValues, AKey) < 0) || (CompareKeys(FEndValues, AKey) > 0)))
				return false;
				
			if (InternalSeek(AKey))
			{
				FIsOnRow = true;
				return FIsOnRow;
			}
			
			FIsOnRow = InternalNext();
			return FIsOnRow && (CompareKeys(AKey) == 0);
		}
		
		public void FindNearest(object[] AKey)
		{
			FCurrentRow = null;
			FIsOnSimulatedCrack = false;

			if (FStartValues != null)
			{
				if (CompareKeys(FStartValues, AKey) < 0)
				{
					FIsOnRow = InternalSeek(FStartValues);
					if (!FIsOnRow)
						FIsOnRow = InternalNext();
				}
				else if (CompareKeys(FEndValues, AKey) > 0)
				{
					FIsOnRow = InternalSeek(FEndValues);
					if (!FIsOnRow)
						FIsOnRow = InternalPrior();
				}
			}
			else
			{
				FIsOnRow = InternalSeek(AKey);
				if (!FIsOnRow)
					FIsOnRow = InternalNext();
			}
		}
	}
}
