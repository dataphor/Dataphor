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
		protected internal SQLStoreCursor(SQLStoreConnection connection, string tableName, SQLIndex index, bool isUpdatable) : base()
		{
			_connection = connection;
			_tableName = tableName;
			_index = index;
			_isUpdatable = isUpdatable;
			_cursorName = _index.Name + isUpdatable.ToString();
		}
		
		protected internal SQLStoreCursor(SQLStoreConnection connection, string tableName, List<string> columns, SQLIndex index, bool isUpdatable)
			: this(connection, tableName, index, isUpdatable)
		{
			_columns = columns;
		}
		
		/// <summary>Sets the columns that identify a row of the table, normally its primary key; the cursor's updates and deletes name the row by them.</summary>
		/// <remarks>Called before the cursor is first used. Without it, the cursor uses the index's columns if the index is unique, otherwise every column.</remarks>
		internal void SetRowKey(List<string> rowKey)
		{
			Error.AssertFail(_key == null, "The row key of store cursor {0} was set after the cursor was first used.", _cursorName);
			_rowKey = rowKey;
		}
		
		private void EnsureKeys()
		{
			if (_key != null)
				return;
			
			if (_columns == null)
				_columns = ReadColumnNames();
			
			List<string> key = new List<string>(_index.Columns.Count);
			_keyColumns = new SQLIndexColumns();
			for (int index = 0; index < _index.Columns.Count; index++)
			{
				key.Add(_index.Columns[index].Name);
				_keyColumns.Add(_index.Columns[index]);
			}
			
			if (_rowKey == null)
				_rowKey = _index.IsUnique ? new List<string>(key) : new List<string>(_columns);
			
			// Ordered on the row key after the index's columns, the cursor's order is unique, so a seek from a row's key past
			// that row finds exactly the row after it, even on an index that does not identify rows.
			// NOTE: on SQLite the extra order terms sort each run of equal index values before its first row is read: measured on
			// 100,000 rows of one owner (SQLite 3.50, in memory), the first row took 24 ms instead of 0 and the whole scan 83 ms
			// instead of 62. If such a scan shows up as slow, keep read-only cursors on the index's own order (they re-seek only
			// when they change direction).
			for (int index = 0; index < _rowKey.Count; index++)
				if (!key.Contains(_rowKey[index]))
				{
					key.Add(_rowKey[index]);
					_keyColumns.Add(new SQLIndexColumn(_rowKey[index], true));
				}
			
			_keyIndexes = new List<int>(key.Count);
			for (int index = 0; index < key.Count; index++)
				_keyIndexes.Add(_columns.IndexOf(key[index]));
			
			_key = key;
		}
		
		private List<string> ReadColumnNames()
		{
			SQLCommand command;
			SQLCursor reader = _connection.ExecuteReader(String.Format("select * from {0} where 0 = 1", _tableName), out command);
			try
			{
				List<string> columns = new List<string>(reader.ColumnCount);
				for (int index = 0; index < reader.ColumnCount; index++)
					columns.Add(reader.GetColumnName(index));
				return columns;
			}
			finally
			{
				reader.Dispose();
				command.Dispose();
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
				
				_connection = null;
			}
		}

		#endregion
		
		private SQLStoreConnection _connection;
		public SQLStoreConnection Connection { get { return _connection; } }
		
		private SQLCommand _readerCommand;
		
		private SQLCursor _reader;
		protected SQLCursor Reader { get { return _reader; } }
		
		private string _tableName;
		public string TableName { get { return _tableName; } }
		
		private SQLIndex _index;
		public SQLIndex Index { get { return _index; } }
		
		public string IndexName { get { return _index.Name; } }
		
		private bool _isUpdatable;
		public bool IsUpdatable { get { return _isUpdatable; } }

		private string _cursorName;
		public string CursorName { get { return _cursorName; } }

		private List<String> _columns;
		// The columns that identify a row.
		private List<String> _rowKey;
		// The cursor's order and the key it positions by: the index's columns, then the row key's columns the index lacks.
		// Null until the cursor is first used.
		private List<String> _key;
		private SQLIndexColumns _keyColumns;
		private List<int> _keyIndexes;
		private object[] _currentRow;
		private object[] _editingRow;
		// The row the cursor is on while it has no reader: the updated row after an update, or the row as it was while a
		// write is executing. The next move seeks from it.
		private object[] _detachedRow;
		// After a delete, the deleted row's key: the cursor is between that row's neighbours, and the next move seeks from it.
		private object[] _deletedKey;
		private object[] _startValues;
		private object[] _endValues;
		private bool _isOnRow;
		private bool _isOnSimulatedCrack;
		
		private void DisposeReader()
		{
			if (_reader != null)
			{
				try
				{
					_reader.Dispose();
				}
				finally
				{
					_reader = null;
					_connection.ReaderClosed();
				}
			}

			if (_readerCommand != null)
			{
				_readerCommand.Dispose();
				_readerCommand = null;
			}
		}
		
		private void CreateReader(object[] origin, bool forward, bool inclusive)
		{
			EnsureKeys();
			_reader = InternalCreateReader(origin, forward, inclusive);
			_connection.ReaderOpened();
			_origin = origin;
			_forward = forward;
			_inclusive = inclusive;
		}
		
		protected virtual SQLCursor InternalCreateReader(object[] origin, bool forward, bool inclusive)
		{
			return _connection.ExecuteReader(GetReaderStatement(_tableName, _keyColumns, origin, forward, inclusive), out _readerCommand);
		}
		
		private object[] _origin;
		private bool _forward;
		private bool _inclusive;
		
		private void ClearWritePosition()
		{
			_detachedRow = null;
			_deletedKey = null;
		}
		
		protected void EnsureReader()
		{
			if (_reader == null)
				EnsureReader(null, true, true);
		}
		
		protected virtual void EnsureReader(object[] origin, bool forward, bool inclusive)
		{
			if (((_origin == null) != (origin == null)) || (CompareKeys(_origin, origin) != 0) || (_forward != forward) || (_inclusive != inclusive))
				DisposeReader();
				
			if (_reader == null)
				CreateReader(origin, forward, inclusive);
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
		
		// NOTE: a null in the origin compares as unknown, so a seek from a row with a null key value finds no row past it. Every
		// catalog store cursor is ranged on non-null values, which excludes such rows; if a cursor ever moves after an update,
		// a delete or a change of direction from a row with a null key value, generate "is null" terms here (and mind that the
		// back ends sort nulls differently).
		protected virtual string GetReaderStatement(string tableName, SQLIndexColumns indexColumns, object[] origin, bool forward, bool inclusive)
		{
			StringBuilder statement = new StringBuilder();
			
			statement.AppendFormat("select * from {0}", tableName);

			for (int index = indexColumns.Count - 1; index >= 0; index--)
			{
				if ((origin != null) && (index < origin.Length))
				{
					if (index == origin.Length - 1)
						statement.AppendFormat(" where ");
					else
						statement.Append(" or ");
					
					statement.Append("(");

					for (int originIndex = 0; originIndex < index; originIndex++)
					{
						if (originIndex > 0)
							statement.Append(" and ");
							
						statement.AppendFormat("({0} = {1})", indexColumns[originIndex].Name, _connection.NativeToLiteralValue(origin[originIndex]));
					}
					
					if (index > 0)
						statement.Append(" and ");
						
					statement.AppendFormat
					(
						"({0} {1} {2})", 
						indexColumns[index].Name, 
						(indexColumns[index].Ascending == forward)
							? (((index == (origin.Length - 1)) && inclusive) ? ">=" : ">")
							: (((index == (origin.Length - 1)) && inclusive) ? "<=" : "<"),
						_connection.NativeToLiteralValue(origin[index])
					);
					
					statement.Append(")");
				}
			}
			
			statement.AppendFormat(" order by ");
			
			for (int index = 0; index < indexColumns.Count; index++)
			{
				if (index > 0)
					statement.Append(", ");
				statement.AppendFormat("{0} {1}", indexColumns[index].Name, indexColumns[index].Ascending == forward ? "asc" : "desc");
			}
			
			return statement.ToString();
		}
		
		#endregion
		
		public void SetRange(object[] startValues, object[] endValues)
		{
			_currentRow = null;
			_startValues = startValues;
			_endValues = endValues;
			
			if (_startValues != null)
				_isOnSimulatedCrack = InternalSeek(startValues);
			else
				_isOnSimulatedCrack = InternalFirst();
			
			_isOnRow = false;
		}
		
		/// <summary>Returns true if ALeftValue is less than ARightValue, false otherwise.</summary>
		private bool IsLessThan(object leftValue, object rightValue)
		{
			if (leftValue is int)
				return (int)leftValue < (int)rightValue;
				
			if (leftValue is string)
				return String.Compare((string)leftValue, (string)rightValue, true) < 0;
				
			if (leftValue is bool)
				return !(bool)leftValue && (bool)rightValue;
				
			return false;
		}
		
		private int CompareKeyValues(object leftValue, object rightValue)
		{
			if (leftValue is string)
				return String.Compare((string)leftValue, (string)rightValue, true);
				
			if (leftValue is IComparable)
				return ((IComparable)leftValue).CompareTo(rightValue);
				
			Error.Fail("Storage keys not comaparable");

			return 0;
		}
		
		/// <summary>Returns -1 if ALeftKey is less than ARightKey, 1 if ALeftKey is greater than ARightKey, and 0 if ALeftKey is equal to ARightKey.</summary>
		/// <remarks>The values of ALeftKey and ARightKey are expected to be native, not store, values.</remarks>
		private int CompareKeys(object[] leftKey, object[] rightKey)
		{
			if ((leftKey == null) && (rightKey == null))
				return 0;
				
			if (leftKey == null)
				return -1;
				
			if (rightKey == null)
				return 1;
				
			int result = 0;
			for (int index = 0; index < (leftKey.Length >= rightKey.Length ? leftKey.Length : rightKey.Length); index++)
			{
				if ((index >= leftKey.Length) || (leftKey[index] == null))
					return -1;
				else if ((index >= rightKey.Length) || (rightKey[index] == null))
					return 1;
				else
				{
					result = CompareKeyValues(leftKey[index], rightKey[index]);
					if (result != 0)
						return result;
				}
			}
			return result;
		}
		
		/// <summary>Returns -1 if the key values for the current row are less than ACompareKey, 1 if the key values for the current row are greater than ACompareKey, and 0 is the key values for the current row are equal to ACompareKey.</summary>
		/// <remarks>The values of ACompareKey are expected to be native, not store, values.</remarks>
		private int CompareKeys(object[] compareKey)
		{
			if (!_isOnRow && (compareKey == null))
				return 0;
				
			if (!_isOnRow)
				return -1;
				
			if (compareKey == null)
				return 1;
				
			int result = 0;
			object indexValue;
			for (int index = 0; index < compareKey.Length; index++)
			{
				indexValue = InternalGetValue(_keyIndexes[index]);
				if (indexValue == null)
					return -1;
				else if ((index >= compareKey.Length) || (compareKey[index] == null))
					return 1;
				else
				{
					result = CompareKeyValues(indexValue, compareKey[index]);
					if (result != 0)
						return result;
				}
			}
			return result;
		}
		
		private object[] RowToKey(object[] row)
		{
			object[] key = new object[_key.Count];
			for (int index = 0; index < _key.Count; index++)
				key[index] = row[_keyIndexes[index]];
			return key;
		}
		
		private void CheckIsOnRow()
		{
			if (!_isOnRow)
				throw new StoreException(StoreException.Codes.CursorHasNoCurrentRow, ErrorSeverity.System);
		}
		
		protected virtual object InternalGetValue(int index)
		{
			if (_editingRow != null)
				return _editingRow[index];					
				
			if (_detachedRow != null)
				return _detachedRow[index];
				
			return StoreToNativeValue(_reader[index]);
		}
		
		protected virtual void InternalSetValue(int index, object tempValue)
		{
			if (_editingRow == null)
				_editingRow = InternalSelect();
				
			_editingRow[index] = tempValue;
		}
		
		public object this[int index]
		{
			get 
			{
				CheckIsOnRow();

				#if SQLSTORETIMING
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
				#endif

					return InternalGetValue(index);

				#if SQLSTORETIMING
				}
				finally
				{
					Connection.Store.Counters.Add(new SQLStoreCounter("GetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				}
				#endif
			}
			set
			{
				CheckIsOnRow();
				
				// The row as it was before the first edit: the update names the row by it, and a nested transaction's undo restores it
				if (_currentRow == null)
					_currentRow = Select();
					
				#if SQLSTORETIMING
				long startTicks = TimingUtility.CurrentTicks;
				#endif
				
				InternalSetValue(index, value);

				#if SQLSTORETIMING
				Connection.Store.Counters.Add(new SQLStoreCounter("SetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				#endif
			}
		}
		
		protected virtual object[] InternalSelect()
		{
			object[] row = new object[_columns.Count];
			for (int index = 0; index < row.Length; index++)
				row[index] = InternalGetValue(index);

			return row;
		}
		
		public object[] Select()
		{
			CheckIsOnRow();
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif

				return InternalSelect();

			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Select", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			}
			#endif
		}
		
		public object[] SelectKey()
		{
			if (!_isOnRow)
				return null;
				
			object[] key = new object[_key.Count];
			for (int index = 0; index < key.Length; index++)
				key[index] = InternalGetValue(_keyIndexes[index]);
				
			return key;
		}
		
		protected virtual object[] InternalReadRow()
		{
			object[] row = new object[_reader.ColumnCount];
			for (int index = 0; index < _reader.ColumnCount; index++)
				row[index] = StoreToNativeValue(_reader[index]);
				
			return row;
		}
		
		protected virtual bool InternalNext()
		{
			_editingRow = null;
			
			if ((_reader == null) || !_forward)
			{
				if (_isOnRow)
					EnsureReader(SelectKey(), true, false);
				else if (_deletedKey != null)
					EnsureReader(_deletedKey, true, false);
				else
					EnsureReader(_startValues, true, true);
				ClearWritePosition();
			}

			return _reader.Next();
		}
		
		public bool Next()
		{
			_currentRow = null;
	
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif

				if (_isOnSimulatedCrack)
				{
					_isOnSimulatedCrack = false;
					_isOnRow = true;
				}
				else
					_isOnRow = InternalNext();

			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Next", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			}
			#endif

			if ((_isOnRow) && (_endValues != null) && ((CompareKeys(_endValues) > 0) || (CompareKeys(_startValues) < 0)))
				_isOnRow = false;

			return _isOnRow;
		}
		
		protected virtual bool InternalLast()
		{
			_editingRow = null;			
			ClearWritePosition();
			DisposeReader();
			EnsureReader(null, false, true);
			return false;
		}
		
		public void Last()
		{
			_currentRow = null;

			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif

			if (_endValues != null)
				_isOnSimulatedCrack = InternalSeek(_endValues);
			else
				_isOnSimulatedCrack = InternalLast();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Last", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
			
			_isOnRow = false;
		}
		
		protected virtual bool InternalPrior()
		{
			_editingRow = null;
			
			if ((_reader == null) || _forward)
			{
				if (_isOnRow)
					EnsureReader(SelectKey(), false, false);
				else if (_deletedKey != null)
					EnsureReader(_deletedKey, false, false);
				else
					EnsureReader(_endValues, false, true);
				ClearWritePosition();
			}

			return _reader.Next();
		}
		
		public bool Prior()
		{
			_currentRow = null;

			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif

				if (_isOnSimulatedCrack)
				{	
					_isOnSimulatedCrack = false;
					_isOnRow = true;
				}
				else
					_isOnRow = InternalPrior();
				
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Prior", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			}
			#endif

			if (_isOnRow && (_startValues != null) && ((CompareKeys(_startValues) < 0) || (CompareKeys(_endValues) > 0)))
				_isOnRow = false;

			return _isOnRow;
		}

		protected virtual bool InternalFirst()
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif
			
			_editingRow = null;
			ClearWritePosition();
			DisposeReader();
			EnsureReader(null, true, true);
			return false;

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("First", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
		}
		
		public void First()
		{
			_currentRow = null;

			if (_startValues != null)
				_isOnSimulatedCrack = InternalSeek(_startValues);
			else
				_isOnSimulatedCrack = InternalFirst();
			
			_isOnRow = false;
		}
		
		public virtual object NativeToStoreValue(object tempValue)
		{			
			return tempValue;
		}

		public virtual object StoreToNativeValue(object tempValue)
		{			
			if (tempValue is DBNull)
				return null;
			return tempValue;
		}
		
		protected virtual bool InternalInsert(object[] row)
		{
			ClearWritePosition();
			DisposeReader();
			Connection.PerformInsert(_tableName, _columns, _rowKey, row);
			//EnsureReader(RowToKey(ARow), true, true);
			return false;
		}
		
		public void Insert(object[] row)
		{
			EnsureKeys();

			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			Connection.Store.Counters.Add(new SQLStoreCounter("CreateRecord", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			startTicks = TimingUtility.CurrentTicks;
			#endif
			
			_isOnRow = InternalInsert(row);

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Insert", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif

			if (_connection.TransactionCount > 1)
				_connection.LogInsert(_tableName, _columns, _rowKey, row);
		}
		
		protected virtual bool InternalUpdate()
		{
			if (_currentRow == null)
				_currentRow = InternalSelect();
			object[] newRow = _editingRow ?? _currentRow;
			
			DisposeReader();
			_detachedRow = _currentRow;
			Connection.PerformUpdate(_tableName, _columns, _rowKey, _currentRow, newRow);
			// Still on the row, as an updatable result set would be: Next and Prior seek from its new values.
			_detachedRow = newRow;
			
			return true;
		}
		
		public void Update()
		{
			CheckIsOnRow();
			
			object[] newRow = null;
			if (_connection.TransactionCount > 1)
			{
				if (_currentRow == null)
					_currentRow = InternalSelect();
					
				if (Connection.Store.SupportsUpdatableCursor)
					newRow = InternalSelect();
				else
					newRow = _editingRow ?? _currentRow;
			}

			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif
			
			_isOnRow = InternalUpdate();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Update", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif
			
			if (_connection.TransactionCount > 1)
				_connection.LogUpdate(_tableName, _columns, _rowKey, _currentRow, newRow);

			_currentRow = null;
			_editingRow = null;
		}
		
		protected virtual bool InternalDelete()
		{
			if (_currentRow == null)
				_currentRow = InternalSelect();
			
			DisposeReader();
			_detachedRow = _currentRow;
			Connection.PerformDelete(_tableName, _columns, _rowKey, _currentRow);
			_detachedRow = null;
			_deletedKey = RowToKey(_currentRow);
			
			return false;
		}
		
		public void Delete()
		{
			CheckIsOnRow();
			
			if ((_currentRow == null) && (_connection.TransactionCount > 1))
				_currentRow = InternalSelect();
				
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			#endif
			
			_isOnRow = InternalDelete();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Delete", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			#endif

			if (_connection.TransactionCount > 1)
				_connection.LogDelete(_tableName, _columns, _rowKey, _currentRow);

			_currentRow = null;
			_editingRow = null;
		}
		
		protected virtual bool InternalSeek(object[] key)
		{
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				_editingRow = null;
				ClearWritePosition();
				// A seek repositions even to the origin the reader was opened on: that reader may have read past it.
				DisposeReader();
				
				object[] localKey = new object[key.Length];
				for (int index = 0; index < localKey.Length; index++)
					localKey[index] = NativeToStoreValue(key[index]);
				EnsureReader(key, true, true);
				return false;
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Seek", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
			}
			#endif
		}
		
		public bool FindKey(object[] key)
		{
			_currentRow = null;
			_isOnSimulatedCrack = false;
			
			if ((_startValues != null) && ((CompareKeys(_startValues, key) < 0) || (CompareKeys(_endValues, key) > 0)))
				return false;
				
			if (InternalSeek(key))
			{
				_isOnRow = true;
				return _isOnRow;
			}
			
			_isOnRow = InternalNext();
			return _isOnRow && (CompareKeys(key) == 0);
		}
		
		public void FindNearest(object[] key)
		{
			_currentRow = null;
			_isOnSimulatedCrack = false;

			if (_startValues != null)
			{
				if (CompareKeys(_startValues, key) < 0)
				{
					_isOnRow = InternalSeek(_startValues);
					if (!_isOnRow)
						_isOnRow = InternalNext();
				}
				else if (CompareKeys(_endValues, key) > 0)
				{
					_isOnRow = InternalSeek(_endValues);
					if (!_isOnRow)
						_isOnRow = InternalPrior();
				}
			}
			else
			{
				_isOnRow = InternalSeek(key);
				if (!_isOnRow)
					_isOnRow = InternalNext();
			}
		}
	}
}
