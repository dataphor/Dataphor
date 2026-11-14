/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define USEINCLUSIVEMULTIPLEX // Controls whether an inclusive or exclusive restriction is used to reposition the cursor when multiplexing on a connection

namespace Alphora.Dataphor.DAE.Device.SQL
{
	using System;
	using System.IO;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	
	public class SQLRow : System.Object
	{
		public SQLRow(int columnCount) : base()
		{
			_data = new object[columnCount];
		}
		
		private object[] _data;
		public object this[int index]
		{
			get { return _data[index]; }
			set { _data[index] = value; }
		}
	}
	
	public class SQLRows : System.Object
	{
		public const int InitialCapacity = SQLDeviceCursor.DefaultBufferSize;

		private SQLRow[] _rows = new SQLRow[InitialCapacity];

		private int _count = 0;		
		public int Count { get { return _count; } }
		
		public SQLRow this[int index] 
		{ 
			get { return _rows[index]; } 
			set { _rows[index] = value; } 
		}
		
        private void EnsureCapacity(int requiredCapacity)
        {
			if (_rows.Length <= requiredCapacity)
			{
				SQLRow[] newRows = new SQLRow[_rows.Length * 2];
				Array.Copy(_rows, 0, newRows, 0, _rows.Length);
				_rows = newRows;
			}
        }
        
        public void Add(SQLRow row)
        {
			EnsureCapacity(_count);
			_rows[_count] = row;
			_count++;
        }
        
        public void Clear()
        {
			_rows = new SQLRow[InitialCapacity];
			_count = 0;
        }
	}
	
	public class SQLDeviceCursor : Disposable
	{
		public const int DefaultBufferSize = 10;
		
		public SQLDeviceCursor
		(
			SQLDeviceSession deviceSession, 
			SelectStatement statement, 
			bool isAggregate,
			SQLParameters parameters, 
			int[] keyIndexes,
			SQLScalarType[] keyTypes,
			SQLParameters keyParameters,
			SQLLockType lockType,
			SQLCursorType cursorType, 
			SQLIsolationLevel isolationLevel
		) : base()
		{
			_deviceSession = deviceSession;
			_statement = statement;
			_isAggregate = isAggregate;
			_originalWhereClause = _statement.QueryExpression.SelectExpression.WhereClause;
			_originalHavingClause = _statement.QueryExpression.SelectExpression.HavingClause;
			_originalParameters = parameters;
			_parameters = new SQLParameters();
			_parameters.AddRange(parameters);
			_keyIndexes = keyIndexes;
			_keyTypes = keyTypes;
			_keyParameters = keyParameters;
			_lockType = lockType;
			_cursorType = cursorType;
			_isolationLevel = isolationLevel;
			EnsureConnection();
			_isDeferred = new bool[ColumnCount];
			for (int index = 0; index < ColumnCount; index++)
				_isDeferred[index] = _cursor.IsDeferred(index);
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_connection != null)
				_deviceSession.ReleaseConnection(_connection, true);
			base.Dispose(disposing);
		}
		
		// SQLDeviceSession
		private SQLDeviceSession _deviceSession;
		public SQLDeviceSession DeviceSession { get { return _deviceSession; } }

		private SQLLockType _lockType;
		private SQLCursorType _cursorType;
		private SQLIsolationLevel _isolationLevel;
		private SelectStatement _statement;
		private SQLParameters _originalParameters;
		private SQLParameters _parameters;
		private WhereClause _originalWhereClause;
		private HavingClause _originalHavingClause;
		private WhereClause _restrictedWhereClause;
		private HavingClause _restrictedHavingClause;
		private int[] _keyIndexes;
		private SQLScalarType[] _keyTypes;
		private SQLParameters _keyParameters;
		private bool _isRestricted; // indicates whether the statement is in its original state, or has been modified with a restriction condition based on last known position
		private bool _isAggregate; // indicates whether the statement is an aggregate statement

		private SQLConnection _connection;
		private SQLCommand _command;
		private SQLCursor _cursor;
		
		// BufferSize - 0 indicates a disconnected recordset
		private int _bufferSize = DefaultBufferSize;
		public int BufferSize
		{
			get { return _bufferSize; }
			set { _bufferSize = value; }
		}
		
		// Buffer
		private SQLRows _buffer = new SQLRows();
		private int _bufferIndex = -1;
		private bool _eOF;

		// ReleaseConnection(SQLConnectionHeader) // clears the SQLDeviceCursor reference for the SQLConnectionHeader record in the SQLDeviceSession
		public void ReleaseConnection(SQLConnectionHeader connectionHeader)
		{
			ReleaseConnection(connectionHeader, false);
		}
		
		public void ReleaseConnection(SQLConnectionHeader connectionHeader, bool disposing)		
		{
			if (!disposing)
				EnsureStatic();
			_cursor.Dispose();
			_cursor = null;
			_command.Dispose();
			_command = null;
			_connection = null;
			connectionHeader.DeviceCursor = null;
		}
		
		// AcquireConnection(SQLConnectionHeader) // sets the SQLDeviceCursor reference for the SQLConnectionHeader record in the SQLDeviceSession
		public void AcquireConnection(SQLConnectionHeader connectionHeader)
		{
			_connection = connectionHeader.Connection;
			try
			{
				_command = _connection.CreateCommand(true);
				try
				{
					if (_buffer.Count > 0)
						RestrictStatement();
					else
						UnrestrictStatement();
					PrepareStatement();

					_command.Statement = _deviceSession.Device.Emitter.Emit(_statement);
					_command.Parameters.AddRange(_parameters);
					_command.LockType = _lockType;

					try
					{
						_cursor = _command.Open(_cursorType, _isolationLevel);
						try
						{
							#if USEINCLUSIVEMULTIPLEX
							if (_buffer.Count > 0)
								_cursor.Next();
							#endif
							connectionHeader.DeviceCursor = this;
						}
						catch
						{
							_command.Close(_cursor);
							_cursor = null;
							throw;
						}
					}
					catch (Exception exception)
					{
						_deviceSession.TransactionFailure = _connection.TransactionFailure;
						throw _deviceSession.WrapException(exception);
					}
				}
				catch
				{
					_command.Dispose();
					_command = null;
					throw;
				}
			}
			catch
			{
				_connection = null;
				throw;
			}
		}
		
/*
		private bool ContainsAggregateExpressions(Statement AExpression)
		{
			if (AExpression is UnaryExpression)
				return ContainsAggregateExpressions(((UnaryExpression)AExpression).Expression);
			else if (AExpression is BinaryExpression)
				return 
					ContainsAggregateExpressions(((BinaryExpression)AExpression).LeftExpression) || 
					ContainsAggregateExpressions(((BinaryExpression)AExpression).RightExpression);
			else if (AExpression is AggregateCallExpression)
				return true;
			else if (AExpression is CallExpression)
			{
				foreach (Expression LExpression in ((CallExpression)AExpression).Expressions)
					if (ContainsAggregateExpressions(LExpression))
						return true;
				return false;
			}
			else if (AExpression is CaseExpression)
			{
				CaseExpression LExpression = (CaseExpression)AExpression;
				if (ContainsAggregateExpressions(LExpression.Expression))
					return true;
				foreach (CaseItemExpression LCaseItem in LExpression.CaseItems)
					if (ContainsAggregateExpressions(LCaseItem))
						return true;
				if (ContainsAggregateExpressions(LExpression.ElseExpression))
					return true;
				return false;
			}
			else if (AExpression is BetweenExpression)
			{
				BetweenExpression LExpression = (BetweenExpression)AExpression;
				return 
					ContainsAggregateExpressions(LExpression.Expression) || 
					ContainsAggregateExpressions(LExpression.LowerExpression) || 
					ContainsAggregateExpressions(LExpression.UpperExpression);
			}
			else if (AExpression is UserExpression)
			{
				foreach (Expression LExpression in ((UserExpression)AExpression).Expressions)
					if (ContainsAggregateExpressions(LExpression))
						return true;
				return false;
			}
			else if (AExpression is QueryExpression)
			{
				QueryExpression LExpression = (QueryExpression)AExpression;
				if (ContainsAggregateExpressions(LExpression.SelectExpression))
					return true;
				foreach (TableOperatorExpression LTableOperator in LExpression.TableOperators)
					if (ContainsAggregateExpressions(LTableOperator.SelectExpression))
						return true;
				return false;
			}
			else if (AExpression is SelectExpression)
			{
				SelectExpression LExpression = (SelectExpression)AExpression;
				return
					ContainsAggregateExpressions(LExpression.SelectClause) ||
					ContainsAggregateExpressions(LExpression.FromClause) ||
					ContainsAggregateExpressions(LExpression.WhereClause) ||
					ContainsAggregateExpressions(LExpression.GroupClause) ||
					ContainsAggregateExpressions(LExpression.HavingClause);
			}
			else if (AExpression is SelectClause)
			{
				SelectClause LClause = (SelectClause)AExpression;
				foreach (ColumnExpression LColumn in LClause.Columns)
					if (ContainsAggregateExpressions(LColumn.Expression))
						return true;
				return false;
			}
			else if (AExpression is AlgebraicFromClause)
			{
				AlgebraicFromClause LClause = (AlgebraicFromClause)AExpression;
				if (ContainsAggregateExpressions(LClause.TableSpecifier.TableExpression))
					return true;
				foreach (JoinClause LJoin in LClause.Joins)
					if (ContainsAggregateExpressions(LJoin.FromClause) || ContainsAggregateExpressions(LJoin.JoinExpression))
						return true;
				return false;
			}
			else if (AExpression is CalculusFromClause)
			{
				CalculusFromClause LClause = (CalculusFromClause)AExpression;
				foreach (TableSpecifier LTable in LClause.TableSpecifiers)
					if (ContainsAggregateExpressions(LTable.TableExpression))
						return true;
				return false;
			}
			else if (AExpression is FilterClause)
				return ContainsAggregateExpressions(((FilterClause)AExpression).Expression);
			else if (AExpression is GroupClause)
			{
				GroupClause LClause = (GroupClause)AExpression;
				foreach (Expression LExpression in LClause.Columns)
					if (ContainsAggregateExpressions(LExpression))
						return true;
				return false;
			}
			return false;
		}
*/
		
		private void EnsureRestrictedWhereClause()
		{
			if ((_restrictedWhereClause == null) && (_restrictedHavingClause == null))
			{
				// modify the statement to return everything greater than the last row in the set, inclusive, for all key colummns
				/*
					for each column in the key, descending
						[or]
						for each column in the origin less than the current key column
							[and] ((current origin column is null and current origin value is null) or (current origin column = current origin value))
							
						[and]
						if the current key column is the last origin column
							((Current key column is null and current origin value is null) or (current key column >= current origin value))
						else
							current key column > current origin value
				*/
				Expression columnExpression;
				Expression keyExpression;
				Expression restrictExpression = null;
				
				for (int keyIndex = _keyIndexes.Length - 1; keyIndex >= 0; keyIndex--)
				{
					keyExpression = null;
					for (int columnIndex = 0; columnIndex < keyIndex; columnIndex++)
					{
						columnExpression =
							new BinaryExpression
							(
								new BinaryExpression
								(
									new UnaryExpression("iIsNull", _statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[columnIndex]].Expression),
									"iAnd",
									new UnaryExpression("iIsNull", new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[columnIndex]].ColumnAlias))
								),
								"iOr",
								new BinaryExpression
								(
									_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[columnIndex]].Expression,
									"iEqual", 
									new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[columnIndex]].ColumnAlias)
								)
							);
							
						if (keyExpression != null)
							keyExpression = new BinaryExpression(keyExpression, "iAnd", columnExpression);
						else
							keyExpression = columnExpression;
					}
					
					if (_statement.OrderClause.Columns[keyIndex].Ascending)
					{
						#if USEINCLUSIVEMULTIPLEX
						if (keyIndex == (_keyIndexes.Length - 1))
							columnExpression =
								new BinaryExpression
								(
									new UnaryExpression("iIsNull", new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].ColumnAlias)),
									"iOr",
									new BinaryExpression
									(
										_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].Expression,
										"iInclusiveGreater",
										new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].ColumnAlias)
									)
								);
						else
						#endif
							columnExpression =
								new BinaryExpression
								(
									new BinaryExpression
									(
										new UnaryExpression("iNot", new UnaryExpression("iIsNull", _statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].Expression)),
										"iAnd",
										new UnaryExpression("iIsNull", new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].ColumnAlias))
									),
									"iOr",
									new BinaryExpression
									(
										_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].Expression,
										"iGreater",
										new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].ColumnAlias)
									)
								);
					}
					else
					{
						#if USEINCLUSIVEMULTIPLEX
						if (keyIndex == (_keyIndexes.Length - 1))
							columnExpression =
								new BinaryExpression
								(
									new UnaryExpression("iIsNull", _statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].Expression),
									"iOr",
									new BinaryExpression
									(
										_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].Expression,
										"iInclusiveLess",
										new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].ColumnAlias)
									)
								);
						else
						#endif
							columnExpression =
								new BinaryExpression
								(
									new BinaryExpression
									(
										new UnaryExpression("iIsNull", _statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].Expression),
										"iAnd",
										new UnaryExpression("iNot", new UnaryExpression("iIsNull", new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].ColumnAlias)))
									),
									"iOr",
									new BinaryExpression
									(
										_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].Expression,
										"iLess", 
										new QueryParameterExpression(_statement.QueryExpression.SelectExpression.SelectClause.Columns[_keyIndexes[keyIndex]].ColumnAlias)
									)
								);
					}
							
					if (keyExpression != null)
						keyExpression = new BinaryExpression(keyExpression, "iAnd", columnExpression);
					else
						keyExpression = columnExpression;
						
					if (restrictExpression != null)
						restrictExpression = new BinaryExpression(restrictExpression, "iOr", keyExpression);
					else
						restrictExpression = keyExpression;
				}
				
				if (_isAggregate)
				{
					_restrictedHavingClause = new HavingClause();
					if (_originalHavingClause != null)
						_restrictedHavingClause.Expression = new BinaryExpression(_originalHavingClause.Expression, "iAnd", restrictExpression);
					else
						_restrictedHavingClause.Expression = restrictExpression;
				}
				else
				{
					_restrictedWhereClause = new WhereClause();
					if (_originalWhereClause != null)
						_restrictedWhereClause.Expression = new BinaryExpression(_originalWhereClause.Expression, "iAnd", restrictExpression);
					else
						_restrictedWhereClause.Expression = restrictExpression;
				}
			}
		}
		
		private void RestrictStatement()
		{
			if (!_isRestricted)
			{
				EnsureRestrictedWhereClause();
				if (_restrictedWhereClause != null)
					_statement.QueryExpression.SelectExpression.WhereClause = _restrictedWhereClause;
				if (_restrictedHavingClause != null)
					_statement.QueryExpression.SelectExpression.HavingClause = _restrictedHavingClause;
				_parameters.Clear();
				_parameters.AddRange(_originalParameters);
				_parameters.AddRange(_keyParameters);

				_isRestricted = true;
			}
		}
		
		private void UnrestrictStatement()
		{
			if (_isRestricted)
			{
				_statement.QueryExpression.SelectExpression.WhereClause = _originalWhereClause;
				_statement.QueryExpression.SelectExpression.HavingClause = _originalHavingClause;
				_parameters.Clear();
				_parameters.AddRange(_originalParameters);
				
				_isRestricted = false;
			}
		}
		
		private void PrepareStatement()
		{
			if (_isRestricted)
				for (int index = 0; index < _keyIndexes.Length; index++)
					if (DeviceSession.Device.UseParametersForCursors)
						_keyParameters[index].Value = _buffer[_buffer.Count - 1][_keyIndexes[index]];
					else
					{
						object tempValue = _buffer[_buffer.Count - 1][_keyIndexes[index]];
						object scalar;
						if (IsNull(tempValue))
							scalar = null;
						else
							scalar = _keyTypes[index].ToScalar(DeviceSession.ServerProcess.ValueManager, tempValue);
						_keyParameters[index].Literal = _keyTypes[index].ToLiteral(DeviceSession.ServerProcess.ValueManager, scalar);
					}
		}

		private void EnsureConnection()
		{
			if (_connection == null)
				AcquireConnection(_deviceSession.RequestConnection(_isolationLevel == SQLIsolationLevel.ReadUncommitted));
		}
		
		public SQLRow ReadRow()
		{
			SQLRow row = new SQLRow(ColumnCount);
			for (int index = 0; index < ColumnCount; index++)
				row[index] = _cursor[index];
			return row;
		}

		// ColumnCount
		private int _columnCount = -1;
		public int ColumnCount 
		{ 
			get 
			{ 
				if (_columnCount == -1)
					_columnCount = _cursor.ColumnCount;
				return _columnCount;
			} 
		}
		
		// IsDeferred
		private bool[] _isDeferred;
		public bool IsDeferred(int index)
		{
			return _isDeferred[index];
		}

		// Next
		public bool Next() 
		{
			_bufferIndex++;
			if (_bufferIndex >= _buffer.Count)
				FetchNext(true);
			return _bufferIndex < _buffer.Count;
		}
		
		private void FetchNext(bool clear)
		{
			if (!_eOF)
			{
				EnsureConnection();
				if (clear)
					_buffer.Clear();

				try
				{
					// Fill the buffer
					while (true)
					{
						_deviceSession.ServerProcess.CheckAborted();	// Yield to proces abort
						if (_cursor.Next())
						{
							_buffer.Add(ReadRow());
							if ((_bufferSize > 0) && _buffer.Count >= _bufferSize)
								break;
						}
						else
						{
							_eOF = true;
							// OPTIMIZATION: if browse isolation and EOF, release the connection
							if (_isolationLevel == SQLIsolationLevel.ReadUncommitted)
								_deviceSession.ReleaseConnection(_connection, false);
							break;
						}
					}
				}
				catch (Exception exception)
				{
					if (_connection != null)
						_deviceSession.TransactionFailure = _connection.TransactionFailure;
					throw _deviceSession.WrapException(exception);
				}
				finally
				{
					if (clear)
						_bufferIndex = 0;
				}
			}
		}
		
		// EnsureStatic
		private void EnsureStatic()
		{
			if (_cursorType == SQLCursorType.Static)
			{
				if (!_eOF)
				{
					_bufferSize = 0;
					FetchNext(false);
				}
			}
		}
		
		// Data 
		public object this[int index] { get { return _buffer[_bufferIndex][index]; } }
		
		protected bool IsNull(object tempValue)
		{
			return (tempValue == null) || (tempValue == System.DBNull.Value);
		}
		
		// IsNull
		public bool IsNull(int index)
		{
			return IsNull(this[index]);
		}
		
		// OpenDeferredStream
		public Stream OpenDeferredStream(int index)
		{
			object tempValue = this[index];
			if (!((tempValue == null) || (tempValue == System.DBNull.Value)))
			{
				if (tempValue is string)
				{
					MemoryStream memoryStream = new MemoryStream();
					using (StreamWriter writer = new StreamWriter(memoryStream))
					{
						writer.Write(tempValue);
						writer.Flush();
						return new MemoryStream(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length, false, true);
					}
				}
				else if (tempValue is byte[])
				{
					byte[] byteValue = (byte[])tempValue;
					return new MemoryStream(byteValue, 0, byteValue.Length, false, true);
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnableToConvertDeferredStreamValue);
			}
			else
				return new MemoryStream();
		}
	}
}

