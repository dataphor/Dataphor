/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.ADO
{
	using System;
	using System.IO;
	using System.Text;
	using System.Reflection;
	
	using ADODB;
	using Alphora.Dataphor.DAE.Connection;
	
	public class ADOConnection : SQLConnection
	{
		public ADOConnection(string connectionString) : base() 
		{
			_connection = new ADODB.Connection();
			_connection.Open(connectionString, String.Empty, String.Empty, -1);
			_connection.CursorLocation = ADODB.CursorLocationEnum.adUseServer;
			SetState(SQLConnectionState.Idle);
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_connection != null)
			{
				try
				{
					while (_nestingLevel > 0)
						InternalRollbackTransaction();

					if (_connection.State != (int)ADODB.ObjectStateEnum.adStateClosed)
						_connection.Close();
				}
				catch
				{
					// Who cares.
				}

				while (System.Runtime.InteropServices.Marshal.ReleaseComObject(_connection) > 0);
				SetState(SQLConnectionState.Closed);
				_connection = null;
			}
			
			base.Dispose(disposing);
		}
		
		private ADODB.Connection _connection;
		
		public override bool IsConnectionValid()
		{
			try
			{
				return (_connection != null) && (_connection.State != 0);
			}
			catch
			{
				return false;
			}
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			ADODB.Command command = new ADODB.Command();
			command.ActiveConnection = _connection;
			return new ADOCommand(this, command);
		}

		private int _nestingLevel;
		/// <summary>Returns the current nesting level of the connection.</summary>
		public int NestingLevel { get { return _nestingLevel; } }
		
		protected override void InternalBeginTransaction(SQLIsolationLevel isolationLevel)
		{
			switch (isolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : _connection.IsolationLevel = ADODB.IsolationLevelEnum.adXactReadUncommitted; break;
				case SQLIsolationLevel.ReadCommitted : _connection.IsolationLevel = ADODB.IsolationLevelEnum.adXactReadCommitted; break;
				case SQLIsolationLevel.RepeatableRead : _connection.IsolationLevel = ADODB.IsolationLevelEnum.adXactRepeatableRead; break;
				case SQLIsolationLevel.Serializable : _connection.IsolationLevel = ADODB.IsolationLevelEnum.adXactSerializable; break;
				default : _connection.IsolationLevel = ADODB.IsolationLevelEnum.adXactUnspecified; break;
			}
			_nestingLevel = _connection.BeginTrans();
		}
		
		protected override void InternalCommitTransaction()
		{
			_connection.CommitTrans();
			_nestingLevel--;
		}
		
		protected override void InternalRollbackTransaction()
		{
			_connection.RollbackTrans();
			_nestingLevel--;
		}
	}
	
	public class ADOCommand : SQLCommand
	{
		protected internal ADOCommand(ADOConnection connection, ADODB.Command command) : base(connection)
		{
			_command = command;
			_useOrdinalBinding = true;
		}
		
		protected override void Dispose(bool disposing)
		{
			//UnprepareCommand();
			if (_command != null)
			{
				while (System.Runtime.InteropServices.Marshal.ReleaseComObject(_command) > 0);
				_command = null;
			}

			base.Dispose(disposing);
		}
		
		public new ADOConnection Connection { get { return (ADOConnection)base.Connection; } }
		
		private ADODB.Command _command;
		
		protected void PrepareCommand(SQLIsolationLevel isolationLevel)
		{
			_command.CommandText = PrepareStatement(Statement);
			_command.CommandTimeout = CommandTimeout; 
			if (CommandType == SQLCommandType.Statement)
				_command.CommandType = CommandTypeEnum.adCmdText;
			if (UseParameters)
				PrepareParameters();
			//FCommand.Prepared = true; // this causes a 'syntax error or access violation' in some cases ???
		}
		
		protected void UnprepareCommand()
		{
			if (UseParameters && (Parameters.Count > 0))
				while (_command.Parameters.Count > 0)
					_command.Parameters.Delete(0);
		}
		
		protected override void InternalPrepare()
		{
		}
		
		protected override void InternalUnprepare()
		{
		}
		
		private void PrepareParameters()
		{
			// Prepare parameters
			SQLParameter parameter;
			for (int index = 0; index < _parameterIndexes.Length; index++)
			{
				parameter = Parameters[_parameterIndexes[index]];
				ADODB.Parameter aDOParameter = _command.CreateParameter(parameter.Name, ADODB.DataTypeEnum.adInteger, ADODB.ParameterDirectionEnum.adParamInput, 0, Missing.Value) ;
				switch (parameter.Direction)
				{
					case SQLDirection.In : aDOParameter.Direction = ADODB.ParameterDirectionEnum.adParamInput; break;
					case SQLDirection.Out : aDOParameter.Direction = ADODB.ParameterDirectionEnum.adParamOutput; break;
					case SQLDirection.InOut : aDOParameter.Direction = ADODB.ParameterDirectionEnum.adParamInputOutput; break;
					case SQLDirection.Result : aDOParameter.Direction = ADODB.ParameterDirectionEnum.adParamReturnValue; break;
					default : aDOParameter.Direction = ADODB.ParameterDirectionEnum.adParamUnknown; break;
				}

				if (parameter.Type is SQLStringType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adVarChar;
					aDOParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adBoolean;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : aDOParameter.Type = ADODB.DataTypeEnum.adTinyInt; break;
						case 2 : aDOParameter.Type = ADODB.DataTypeEnum.adSmallInt; break;
						case 8 : aDOParameter.Type = ADODB.DataTypeEnum.adBigInt; break;
						default : aDOParameter.Type = ADODB.DataTypeEnum.adInteger; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					aDOParameter.Type = ADODB.DataTypeEnum.adNumeric;
					aDOParameter.NumericScale = type.Scale;
					aDOParameter.Precision = type.Precision;
				}
				else if (parameter.Type is SQLFloatType)
				{
					SQLFloatType type = (SQLFloatType)parameter.Type;
					if (type.Width == 1)
						aDOParameter.Type = ADODB.DataTypeEnum.adSingle;
					else
						aDOParameter.Type = ADODB.DataTypeEnum.adDouble;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adLongVarBinary;	
					aDOParameter.Attributes |= (int)ADODB.ParameterAttributesEnum.adParamLong;
					aDOParameter.Size = 255;
				}
				else if (parameter.Type is SQLByteArrayType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adBinary;
					aDOParameter.Size = ((SQLByteArrayType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLTextType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adLongVarChar;
					aDOParameter.Size = 255;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adDBTimeStamp;
				}
				else if (parameter.Type is SQLDateType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adDBDate;
				}
				else if (parameter.Type is SQLTimeType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adDBTime;
				}
				else if (parameter.Type is SQLGuidType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adChar;
					aDOParameter.Size = 38;
					//LADOParameter.Type = ADODB.DataTypeEnum.adGUID;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					aDOParameter.Type = ADODB.DataTypeEnum.adCurrency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Append(aDOParameter);
			}
		}
		
		private void SetParameters()
		{
			if (UseParameters)
			{
				SQLParameter sQLParameter;
				for (int index = 0; index < _parameterIndexes.Length; index++)
				{
					sQLParameter = Parameters[_parameterIndexes[index]];
					if ((sQLParameter.Direction == SQLDirection.In) || (sQLParameter.Direction == SQLDirection.InOut))
					{
						ADODB.Parameter parameter = _command.Parameters[index];
						if ((parameter.Attributes & (int)ADODB.ParameterAttributesEnum.adParamLong) != 0)
						{
							object tempValue = sQLParameter.Value;
							if (tempValue is byte[])
								parameter.Size = ((byte[])tempValue).Length;
							else if (tempValue is string)
								parameter.Size = ((string)tempValue).Length;

							if (tempValue == null)
								parameter.Value = DBNull.Value;
							else
								parameter.Value = tempValue;
						}
						else
						{
							// TODO: Better story for the length of string parameters.  This usage prevents the use of prepared commands in general
							object tempValue = sQLParameter.Value;
							if ((tempValue is string) && (parameter.Size < ((string)tempValue).Length))
								parameter.Size = ((string)tempValue).Length;

							if (tempValue == null)
								parameter.Value = DBNull.Value;
							else
							{
								if (sQLParameter.Type is SQLGuidType)
									parameter.Value = tempValue.ToString();
								else
									parameter.Value = tempValue;
							}
						}
					}
				}
			}
/*
			else
			{
				int LParameterIndex = -1;
				string LStatement = FCommand.CommandText;
				StringBuilder LResultStatement = new StringBuilder();
				bool LInString = false;
				for (int LIndex = 0; LIndex < LStatement.Length; LIndex++)
				{
					switch (LStatement[LIndex])
					{
						case '?' :
							if (!LInString)
							{
								LParameterIndex++;
								SQLParameter LParameter = Parameters[FParameterIndexes[LParameterIndex]];
								if (LParameter.Value == null)
								{
									LResultStatement.Replace("=", "is", LResultStatement.ToString().LastIndexOf('='), 1);
									LResultStatement.AppendFormat("null");
								}
								else
								{
									if (LParameter.Type is SQLStringType)
										LResultStatement.AppendFormat(@"'{0}'", LParameter.Value.ToString().Replace(@"""", @""""""));
									else if (LParameter.Type is SQLGuidType)
										LResultStatement.AppendFormat(@"'{0}'", LParameter.Value.ToString());
									else if (LParameter.Type is SQLDateType)
										LResultStatement.AppendFormat(@"'{0}'d", new DateTime(1960, 1, 1).AddDays(Convert.ToDouble(LParameter.Value)).ToString("ddMMMyyyy"));
									else if (LParameter.Type is SQLTimeType)
										LResultStatement.AppendFormat(@"'{0}'t", new DateTime(1960, 1, 1).AddSeconds(Convert.ToDouble(LParameter.Value)).ToString("hh:mm:ss tt"));
									else if (LParameter.Type is SQLDateTimeType)
										LResultStatement.AppendFormat(@"'{0}'dt", new DateTime(1960, 1, 1).AddSeconds(Convert.ToDouble(LParameter.Value)).ToString("ddMMMyyyy hh:mm:ss tt")); // TODO: Mechanism for providing literal parameter values.  This code is specific to the SAS device.
									else
										LResultStatement.AppendFormat(@"{0}", LParameter.Value.ToString());
								}
							}
							else
								goto default;
						break;
						
						case '\'' :
							if (LInString)
							{
								if (((LIndex + 1) >= LStatement.Length) || (LStatement[LIndex + 1] != '\''))
									LInString = false;
							}
							else
								LInString = true;
							goto default;
						
						default : LResultStatement.Append(LStatement[LIndex]); break;
					}
				}

				FCommand.CommandText = LResultStatement.ToString();
			}
*/
		}
		
		private void GetParameters()
		{
			if (UseParameters)
			{
				SQLParameter sQLParameter;
				for (int index = 0; index < _parameterIndexes.Length; index++)
				{
					sQLParameter = Parameters[_parameterIndexes[index]];
					if ((sQLParameter.Direction == SQLDirection.InOut) || (sQLParameter.Direction == SQLDirection.Out) || (sQLParameter.Direction == SQLDirection.Result))
						sQLParameter.Value = _command.Parameters[index].Value;
				}
			}
		}
		
		protected override void InternalExecute()
		{
			PrepareCommand(SQLIsolationLevel.Serializable);
			try
			{
				SetParameters();
				object recordsAffected;
				object parameters = Missing.Value;
				_command.Execute(out recordsAffected, ref parameters, -1);
				GetParameters();
			}
			finally
			{
				UnprepareCommand();
			}
		}
		
		/*
			NOTE: ADO WORKAROUND: The adOpenForwardOnly and adLockReadOnly combination will always discard the recordset on a commit or abort, so it cannot be used.
			NOTE: ADO WORKAROUND only required when the isolation level > read uncommitted because the browse Cursors are all opened on a separate Cursor connection which runs its own transaction.
			NOTE: ADO WORKAROUND not required at all because the only cursors that span transactions are browse Cursors
		*/
		protected override SQLCursor InternalOpen(SQLCursorType cursorType, SQLIsolationLevel isolationLevel)
		{
			PrepareCommand(isolationLevel);
			SetParameters();
			ADODB.Recordset recordset = new ADODB.Recordset();
			recordset.Source = _command;
			recordset.CacheSize = 20;
			//LRecordset.Properties["Preserve on Commit"].Value = true;
			//LRecordset.Properties["Preserve on Abort"].Value = true;
			recordset.Open
			(
				Missing.Value, 
				Missing.Value, 
				(
					(cursorType == SQLCursorType.Static) ? 
						ADODB.CursorTypeEnum.adOpenStatic : 
						(
							(LockType == SQLLockType.ReadOnly) ?
								ADODB.CursorTypeEnum.adOpenForwardOnly :
								ADODB.CursorTypeEnum.adOpenDynamic
						)
				), 
				(LockType == SQLLockType.Optimistic) ?
					ADODB.LockTypeEnum.adLockOptimistic :
					(
						(LockType == SQLLockType.Pessimistic) ?
							ADODB.LockTypeEnum.adLockPessimistic :
							ADODB.LockTypeEnum.adLockReadOnly
					),
				CommandType == SQLCommandType.Table ? (int)CommandTypeEnum.adCmdTableDirect : -1
			);
			SQLCursor cursor = new ADOCursor(this, recordset);
			GetParameters();
			return cursor;
		}
		
		protected override void InternalClose()
		{
			UnprepareCommand();
		}
	}
	
	internal class ADOValue
	{
		public bool IsCached;
		public object Value;
	}
	
	public class ADOCursor : SQLCursor
	{
		public ADOCursor(ADOCommand command, ADODB.Recordset recordset) : base(command)
		{
			_recordset = recordset;
			_values = new ADOValue[_recordset.Fields.Count];
			for (int index = 0; index < _values.Length; index++)
				_values[index] = new ADOValue();
			_bOF = true;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_recordset != null)
			{
				if (_recordset.State != 0)
					try
					{
						_recordset.Close();
					}
					catch
					{
						// ignore this exception
						// if we can't close it, oh well... it shouldn't be throwing anyway
					}

				while (System.Runtime.InteropServices.Marshal.ReleaseComObject(_recordset) > 0);
				_recordset = null;
			}
			
			base.Dispose(disposing);
		}
	
		private bool _bOF;	
		private ADODB.Recordset _recordset;
		private ADOValue[] _values;
		
		protected override bool InternalNext()
		{
			if (_bOF)
				_bOF = false;
			else
				if (!_recordset.EOF)
					_recordset.MoveNext();
			ClearValues();
			return !_recordset.EOF;
		}
		
		private void ClearValues()
		{
			for (int index = 0; index < _values.Length; index++)
				_values[index].IsCached = false;
		}
		
		private object GetValue(int index)
		{
			if (!_values[index].IsCached)
			{
				_values[index].Value = _recordset.Fields[index].Value;
				_values[index].IsCached = true;
			}
			return _values[index].Value;
		}

		protected override SQLTableSchema InternalGetSchema()
		{
			throw new NotSupportedException();
		}		
		
		protected override int InternalGetColumnCount()
		{
			return _values.Length;
		}

		protected override string InternalGetColumnName(int index)
		{
			return _recordset.Fields[index].Name;
		}
		
		protected override object InternalGetColumnValue(int index)
		{
			return GetValue(index);
		}
		
		protected override bool InternalIsNull(int index)
		{
			object tempValue = GetValue(index);
			return 
				(
					(
						(tempValue == null) || 
						(tempValue == System.DBNull.Value)
					)
				);
		}
		
		protected override bool InternalIsDeferred(int index)
		{
			return ((_recordset.Fields[index].Attributes & (int)FieldAttributeEnum.adFldLong) != 0);
		}
		
		protected override System.IO.Stream InternalOpenDeferredStream(int index)
		{
			object tempValue = GetValue(index);
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
		
		protected override bool InternalFindKey(object[] key)
		{
			_recordset.Seek(key, ADODB.SeekEnum.adSeekFirstEQ);
			return !_recordset.EOF;
		}

		protected override void InternalFindNearest(object[] key)
		{
			_recordset.Seek(key, ADODB.SeekEnum.adSeekAfterEQ);
		}
		
		protected override string InternalGetFilter()
		{
			String tempValue = _recordset.Filter as String;
			return tempValue == null ? String.Empty : tempValue;
		}
		
		protected override bool InternalSetFilter(string filter)
		{
			_recordset.Filter = filter;
			return !_recordset.EOF;
		}

		protected override void InternalInsert(string[] names, object[] values)
		{
			_recordset.AddNew(System.Reflection.Missing.Value, System.Reflection.Missing.Value);
			for (int index = 0; index < names.Length; index++)
				_recordset.Fields[names[index]].Value = values[index];
			_recordset.Update(System.Reflection.Missing.Value, System.Reflection.Missing.Value);
		}

		protected override void InternalUpdate(string[] names, object[] values)
		{
			for (int index = 0; index < names.Length; index++)
				_recordset.Fields[names[index]].Value = values[index];
			_recordset.Update(System.Reflection.Missing.Value, System.Reflection.Missing.Value);
		}

		protected override void InternalDelete()
		{
			_recordset.Delete(ADODB.AffectEnum.adAffectCurrent);
		}
	}
}

