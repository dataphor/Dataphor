/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
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
		public SQLRow(int AColumnCount) : base()
		{
			FData = new object[AColumnCount];
		}
		
		private object[] FData;
		public object this[int AIndex]
		{
			get { return FData[AIndex]; }
			set { FData[AIndex] = value; }
		}
	}
	
	public class SQLRows : System.Object
	{
		public const int CInitialCapacity = SQLDeviceCursor.CDefaultBufferSize;

		private SQLRow[] FRows = new SQLRow[CInitialCapacity];

		private int FCount = 0;		
		public int Count { get { return FCount; } }
		
		public SQLRow this[int AIndex] 
		{ 
			get { return FRows[AIndex]; } 
			set { FRows[AIndex] = value; } 
		}
		
        private void EnsureCapacity(int ARequiredCapacity)
        {
			if (FRows.Length <= ARequiredCapacity)
			{
				SQLRow[] LNewRows = new SQLRow[FRows.Length * 2];
				Array.Copy(FRows, 0, LNewRows, 0, FRows.Length);
				FRows = LNewRows;
			}
        }
        
        public void Add(SQLRow ARow)
        {
			EnsureCapacity(FCount);
			FRows[FCount] = ARow;
			FCount++;
        }
        
        public void Clear()
        {
			FRows = new SQLRow[CInitialCapacity];
			FCount = 0;
        }
	}
	
	public class SQLDeviceCursor : Disposable
	{
		public const int CDefaultBufferSize = 10;
		
		public SQLDeviceCursor
		(
			SQLDeviceSession ADeviceSession, 
			SelectStatement AStatement, 
			bool AIsAggregate,
			SQLParameters AParameters, 
			int[] AKeyIndexes,
			SQLScalarType[] AKeyTypes,
			SQLParameters AKeyParameters,
			SQLLockType ALockType,
			SQLCursorType ACursorType, 
			SQLIsolationLevel AIsolationLevel
		) : base()
		{
			FDeviceSession = ADeviceSession;
			FStatement = AStatement;
			FIsAggregate = AIsAggregate;
			FOriginalWhereClause = FStatement.QueryExpression.SelectExpression.WhereClause;
			FOriginalHavingClause = FStatement.QueryExpression.SelectExpression.HavingClause;
			FOriginalParameters = AParameters;
			FParameters = new SQLParameters();
			FParameters.AddRange(AParameters);
			FKeyIndexes = AKeyIndexes;
			FKeyTypes = AKeyTypes;
			FKeyParameters = AKeyParameters;
			FLockType = ALockType;
			FCursorType = ACursorType;
			FIsolationLevel = AIsolationLevel;
			EnsureConnection();
			FIsDeferred = new bool[ColumnCount];
			for (int LIndex = 0; LIndex < ColumnCount; LIndex++)
				FIsDeferred[LIndex] = FCursor.IsDeferred(LIndex);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FConnection != null)
				FDeviceSession.ReleaseConnection(FConnection, true);
			base.Dispose(ADisposing);
		}
		
		// SQLDeviceSession
		private SQLDeviceSession FDeviceSession;
		public SQLDeviceSession DeviceSession { get { return FDeviceSession; } }

		private SQLLockType FLockType;
		private SQLCursorType FCursorType;
		private SQLIsolationLevel FIsolationLevel;
		private SelectStatement FStatement;
		private SQLParameters FOriginalParameters;
		private SQLParameters FParameters;
		private WhereClause FOriginalWhereClause;
		private HavingClause FOriginalHavingClause;
		private WhereClause FRestrictedWhereClause;
		private HavingClause FRestrictedHavingClause;
		private int[] FKeyIndexes;
		private SQLScalarType[] FKeyTypes;
		private SQLParameters FKeyParameters;
		private bool FIsRestricted; // indicates whether the statement is in its original state, or has been modified with a restriction condition based on last known position
		private bool FIsAggregate; // indicates whether the statement is an aggregate statement

		private SQLConnection FConnection;
		private SQLCommand FCommand;
		private SQLCursor FCursor;
		
		// BufferSize - 0 indicates a disconnected recordset
		private int FBufferSize = CDefaultBufferSize;
		public int BufferSize
		{
			get { return FBufferSize; }
			set { FBufferSize = value; }
		}
		
		// Buffer
		private SQLRows FBuffer = new SQLRows();
		private int FBufferIndex = -1;
		private bool FEOF;

		// ReleaseConnection(SQLConnectionHeader) // clears the SQLDeviceCursor reference for the SQLConnectionHeader record in the SQLDeviceSession
		public void ReleaseConnection(SQLConnectionHeader AConnectionHeader)
		{
			ReleaseConnection(AConnectionHeader, false);
		}
		
		public void ReleaseConnection(SQLConnectionHeader AConnectionHeader, bool ADisposing)		
		{
			if (!ADisposing)
				EnsureStatic();
			FCursor.Dispose();
			FCursor = null;
			FCommand.Dispose();
			FCommand = null;
			FConnection = null;
			AConnectionHeader.DeviceCursor = null;
		}
		
		// AcquireConnection(SQLConnectionHeader) // sets the SQLDeviceCursor reference for the SQLConnectionHeader record in the SQLDeviceSession
		public void AcquireConnection(SQLConnectionHeader AConnectionHeader)
		{
			FConnection = AConnectionHeader.Connection;
			try
			{
				FCommand = FConnection.CreateCommand(true);
				try
				{
					if (FBuffer.Count > 0)
						RestrictStatement();
					else
						UnrestrictStatement();
					PrepareStatement();

					FCommand.Statement = FDeviceSession.Device.Emitter.Emit(FStatement);
					FCommand.Parameters.AddRange(FParameters);
					FCommand.LockType = FLockType;

					try
					{
						FCursor = FCommand.Open(FCursorType, FIsolationLevel);
						try
						{
							if (FBuffer.Count > 0)
								FCursor.Next();
							AConnectionHeader.DeviceCursor = this;
						}
						catch
						{
							FCommand.Close(FCursor);
							FCursor = null;
							throw;
						}
					}
					catch (Exception LException)
					{
						FDeviceSession.TransactionFailure = FConnection.TransactionFailure;
						throw FDeviceSession.WrapException(LException);
					}
				}
				catch
				{
					FCommand.Dispose();
					FCommand = null;
					throw;
				}
			}
			catch
			{
				FConnection = null;
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
			if ((FRestrictedWhereClause == null) && (FRestrictedHavingClause == null))
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
				Expression LColumnExpression;
				Expression LKeyExpression;
				Expression LRestrictExpression = null;
				
				for (int LKeyIndex = FKeyIndexes.Length - 1; LKeyIndex >= 0; LKeyIndex--)
				{
					LKeyExpression = null;
					for (int LColumnIndex = 0; LColumnIndex < LKeyIndex; LColumnIndex++)
					{
						LColumnExpression =
							new BinaryExpression
							(
								new BinaryExpression
								(
									new UnaryExpression("iIsNull", FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LColumnIndex]].Expression),
									"iAnd",
									new UnaryExpression("iIsNull", new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LColumnIndex]].ColumnAlias))
								),
								"iOr",
								new BinaryExpression
								(
									FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LColumnIndex]].Expression,
									"iEqual", 
									new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LColumnIndex]].ColumnAlias)
								)
							);
							
						if (LKeyExpression != null)
							LKeyExpression = new BinaryExpression(LKeyExpression, "iAnd", LColumnExpression);
						else
							LKeyExpression = LColumnExpression;
					}
					
					if (FStatement.OrderClause.Columns[LKeyIndex].Ascending)
					{
						if (LKeyIndex == (FKeyIndexes.Length - 1))
							LColumnExpression =
								new BinaryExpression
								(
									new UnaryExpression("iIsNull", new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].ColumnAlias)),
									"iOr",
									new BinaryExpression
									(
										FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].Expression,
										"iInclusiveGreater",
										new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].ColumnAlias)
									)
								);
						else
							LColumnExpression =
								new BinaryExpression
								(
									new BinaryExpression
									(
										new UnaryExpression("iNot", new UnaryExpression("iIsNull", FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].Expression)),
										"iAnd",
										new UnaryExpression("iIsNull", new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].ColumnAlias))
									),
									"iOr",
									new BinaryExpression
									(
										FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].Expression,
										"iGreater",
										new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].ColumnAlias)
									)
								);
					}
					else
					{
						if (LKeyIndex == (FKeyIndexes.Length - 1))
							LColumnExpression =
								new BinaryExpression
								(
									new UnaryExpression("iIsNull", FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].Expression),
									"iOr",
									new BinaryExpression
									(
										FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].Expression,
										"iInclusiveLess", 
										new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].ColumnAlias)
									)
								);
						else
							LColumnExpression =
								new BinaryExpression
								(
									new BinaryExpression
									(
										new UnaryExpression("iIsNull", FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].Expression),
										"iAnd",
										new UnaryExpression("iNot", new UnaryExpression("iIsNull", new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].ColumnAlias)))
									),
									"iOr",
									new BinaryExpression
									(
										FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].Expression,
										"iLess", 
										new QueryParameterExpression(FStatement.QueryExpression.SelectExpression.SelectClause.Columns[FKeyIndexes[LKeyIndex]].ColumnAlias)
									)
								);
					}
							
					if (LKeyExpression != null)
						LKeyExpression = new BinaryExpression(LKeyExpression, "iAnd", LColumnExpression);
					else
						LKeyExpression = LColumnExpression;
						
					if (LRestrictExpression != null)
						LRestrictExpression = new BinaryExpression(LRestrictExpression, "iOr", LKeyExpression);
					else
						LRestrictExpression = LKeyExpression;
				}
				
				if (FIsAggregate)
				{
					FRestrictedHavingClause = new HavingClause();
					if (FOriginalHavingClause != null)
						FRestrictedHavingClause.Expression = new BinaryExpression(FOriginalHavingClause.Expression, "iAnd", LRestrictExpression);
					else
						FRestrictedHavingClause.Expression = LRestrictExpression;
				}
				else
				{
					FRestrictedWhereClause = new WhereClause();
					if (FOriginalWhereClause != null)
						FRestrictedWhereClause.Expression = new BinaryExpression(FOriginalWhereClause.Expression, "iAnd", LRestrictExpression);
					else
						FRestrictedWhereClause.Expression = LRestrictExpression;
				}
			}
		}
		
		private void RestrictStatement()
		{
			if (!FIsRestricted)
			{
				EnsureRestrictedWhereClause();
				if (FRestrictedWhereClause != null)
					FStatement.QueryExpression.SelectExpression.WhereClause = FRestrictedWhereClause;
				if (FRestrictedHavingClause != null)
					FStatement.QueryExpression.SelectExpression.HavingClause = FRestrictedHavingClause;
				FParameters.Clear();
				FParameters.AddRange(FOriginalParameters);
				FParameters.AddRange(FKeyParameters);

				FIsRestricted = true;
			}
		}
		
		private void UnrestrictStatement()
		{
			if (FIsRestricted)
			{
				FStatement.QueryExpression.SelectExpression.WhereClause = FOriginalWhereClause;
				FStatement.QueryExpression.SelectExpression.HavingClause = FOriginalHavingClause;
				FParameters.Clear();
				FParameters.AddRange(FOriginalParameters);
				
				FIsRestricted = false;
			}
		}
		
		private void PrepareStatement()
		{
			if (FIsRestricted)
				for (int LIndex = 0; LIndex < FKeyIndexes.Length; LIndex++)
					if (DeviceSession.Device.UseParametersForCursors)
						FKeyParameters[LIndex].Value = FBuffer[FBuffer.Count - 1][FKeyIndexes[LIndex]];
					else
					{
						object LValue = FBuffer[FBuffer.Count - 1][FKeyIndexes[LIndex]];
						object LScalar;
						if (IsNull(LValue))
							LScalar = null;
						else
							LScalar = FKeyTypes[LIndex].ToScalar(DeviceSession.ServerProcess.ValueManager, LValue);
						FKeyParameters[LIndex].Literal = FKeyTypes[LIndex].ToLiteral(DeviceSession.ServerProcess.ValueManager, LScalar);
					}
		}
		
		private void EnsureConnection()
		{
			if (FConnection == null)
				AcquireConnection(FDeviceSession.RequestConnection(FIsolationLevel == SQLIsolationLevel.ReadUncommitted));
		}
		
		public SQLRow ReadRow()
		{
			SQLRow LRow = new SQLRow(ColumnCount);
			for (int LIndex = 0; LIndex < ColumnCount; LIndex++)
				LRow[LIndex] = FCursor[LIndex];
			return LRow;
		}

		// ColumnCount
		private int FColumnCount = -1;
		public int ColumnCount 
		{ 
			get 
			{ 
				if (FColumnCount == -1)
					FColumnCount = FCursor.ColumnCount;
				return FColumnCount;
			} 
		}
		
		// IsDeferred
		private bool[] FIsDeferred;
		public bool IsDeferred(int AIndex)
		{
			return FIsDeferred[AIndex];
		}

		// Next
		public bool Next() 
		{
			FBufferIndex++;
			if (FBufferIndex >= FBuffer.Count)
				FetchNext(true);
			return FBufferIndex < FBuffer.Count;
		}
		
		private void FetchNext(bool AClear)
		{
			if (!FEOF)
			{
				EnsureConnection();
				if (AClear)
					FBuffer.Clear();

				try
				{
					// Fill the buffer
					while (true)
					{
						FDeviceSession.ServerProcess.CheckAborted();	// Yield to proces abort
						if (FCursor.Next())
						{
							FBuffer.Add(ReadRow());
							if ((FBufferSize > 0) && FBuffer.Count >= FBufferSize)
								break;
						}
						else
						{
							FEOF = true;
							// OPTIMIZATION: if browse isolation and EOF, release the connection
							if (FIsolationLevel == SQLIsolationLevel.ReadUncommitted)
								FDeviceSession.ReleaseConnection(FConnection, false);
							break;
						}
					}
				}
				catch (Exception LException)
				{
					if (FConnection != null)
						FDeviceSession.TransactionFailure = FConnection.TransactionFailure;
					throw FDeviceSession.WrapException(LException);
				}
				finally
				{
					if (AClear)
						FBufferIndex = 0;
				}
			}
		}
		
		// EnsureStatic
		private void EnsureStatic()
		{
			if (FCursorType == SQLCursorType.Static)
			{
				if (!FEOF)
				{
					FBufferSize = 0;
					FetchNext(false);
				}
			}
		}
		
		// Data 
		public object this[int AIndex] { get { return FBuffer[FBufferIndex][AIndex]; } }
		
		protected bool IsNull(object AValue)
		{
			return (AValue == null) || (AValue == System.DBNull.Value);
		}
		
		// IsNull
		public bool IsNull(int AIndex)
		{
			return IsNull(this[AIndex]);
		}
		
		// OpenDeferredStream
		public Stream OpenDeferredStream(int AIndex)
		{
			object LValue = this[AIndex];
			if (!((LValue == null) || (LValue == System.DBNull.Value)))
			{
				if (LValue is string)
				{
					MemoryStream LMemoryStream = new MemoryStream();
					using (StreamWriter LWriter = new StreamWriter(LMemoryStream))
					{
						LWriter.Write(LValue);
						LWriter.Flush();
						return new MemoryStream(LMemoryStream.GetBuffer(), 0, LMemoryStream.GetBuffer().Length, false, true);
					}
				}
				else if (LValue is byte[])
				{
					byte[] LByteValue = (byte[])LValue;
					return new MemoryStream(LByteValue, 0, LByteValue.Length, false, true);
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnableToConvertDeferredStreamValue);
			}
			else
				return new MemoryStream();
		}
	}
}

