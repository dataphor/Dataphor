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
		public ADOConnection(string AConnectionString) : base() 
		{
			FConnection = new ADODB.Connection();
			FConnection.Open(AConnectionString, String.Empty, String.Empty, -1);
			FConnection.CursorLocation = ADODB.CursorLocationEnum.adUseServer;
			SetState(SQLConnectionState.Idle);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FConnection != null)
			{
				try
				{
					while (FNestingLevel > 0)
						InternalRollbackTransaction();

					if (FConnection.State != (int)ADODB.ObjectStateEnum.adStateClosed)
						FConnection.Close();
				}
				catch
				{
					// Who cares.
				}

				while (System.Runtime.InteropServices.Marshal.ReleaseComObject(FConnection) > 0);
				SetState(SQLConnectionState.Closed);
				FConnection = null;
			}
			
			base.Dispose(ADisposing);
		}
		
		private ADODB.Connection FConnection;
		
		public override bool IsConnectionValid()
		{
			try
			{
				return (FConnection != null) && (FConnection.State != 0);
			}
			catch
			{
				return false;
			}
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			ADODB.Command LCommand = new ADODB.Command();
			LCommand.ActiveConnection = FConnection;
			return new ADOCommand(this, LCommand);
		}

		private int FNestingLevel;
		/// <summary>Returns the current nesting level of the connection.</summary>
		public int NestingLevel { get { return FNestingLevel; } }
		
		protected override void InternalBeginTransaction(SQLIsolationLevel AIsolationLevel)
		{
			switch (AIsolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : FConnection.IsolationLevel = ADODB.IsolationLevelEnum.adXactReadUncommitted; break;
				case SQLIsolationLevel.ReadCommitted : FConnection.IsolationLevel = ADODB.IsolationLevelEnum.adXactReadCommitted; break;
				case SQLIsolationLevel.RepeatableRead : FConnection.IsolationLevel = ADODB.IsolationLevelEnum.adXactRepeatableRead; break;
				case SQLIsolationLevel.Serializable : FConnection.IsolationLevel = ADODB.IsolationLevelEnum.adXactSerializable; break;
				default : FConnection.IsolationLevel = ADODB.IsolationLevelEnum.adXactUnspecified; break;
			}
			FNestingLevel = FConnection.BeginTrans();
		}
		
		protected override void InternalCommitTransaction()
		{
			FConnection.CommitTrans();
			FNestingLevel--;
		}
		
		protected override void InternalRollbackTransaction()
		{
			FConnection.RollbackTrans();
			FNestingLevel--;
		}
	}
	
	public class ADOCommand : SQLCommand
	{
		protected internal ADOCommand(ADOConnection AConnection, ADODB.Command ACommand) : base(AConnection)
		{
			FCommand = ACommand;
			FUseOrdinalBinding = true;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			//UnprepareCommand();
			if (FCommand != null)
			{
				while (System.Runtime.InteropServices.Marshal.ReleaseComObject(FCommand) > 0);
				FCommand = null;
			}

			base.Dispose(ADisposing);
		}
		
		public new ADOConnection Connection { get { return (ADOConnection)base.Connection; } }
		
		private ADODB.Command FCommand;
		
		protected void PrepareCommand(SQLIsolationLevel AIsolationLevel)
		{
			FCommand.CommandText = PrepareStatement(Statement);
			FCommand.CommandTimeout = CommandTimeout; 
			if (CommandType == SQLCommandType.Statement)
				FCommand.CommandType = CommandTypeEnum.adCmdText;
			if (UseParameters)
				PrepareParameters();
			//FCommand.Prepared = true; // this causes a 'syntax error or access violation' in some cases ???
		}
		
		protected void UnprepareCommand()
		{
			if (UseParameters && (Parameters.Count > 0))
				while (FCommand.Parameters.Count > 0)
					FCommand.Parameters.Delete(0);
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
			SQLParameter LParameter;
			for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
			{
				LParameter = Parameters[FParameterIndexes[LIndex]];
				ADODB.Parameter LADOParameter = FCommand.CreateParameter(LParameter.Name, ADODB.DataTypeEnum.adInteger, ADODB.ParameterDirectionEnum.adParamInput, 0, Missing.Value) ;
				switch (LParameter.Direction)
				{
					case SQLDirection.In : LADOParameter.Direction = ADODB.ParameterDirectionEnum.adParamInput; break;
					case SQLDirection.Out : LADOParameter.Direction = ADODB.ParameterDirectionEnum.adParamOutput; break;
					case SQLDirection.InOut : LADOParameter.Direction = ADODB.ParameterDirectionEnum.adParamInputOutput; break;
					case SQLDirection.Result : LADOParameter.Direction = ADODB.ParameterDirectionEnum.adParamReturnValue; break;
					default : LADOParameter.Direction = ADODB.ParameterDirectionEnum.adParamUnknown; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adVarChar;
					LADOParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adBoolean;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LADOParameter.Type = ADODB.DataTypeEnum.adTinyInt; break;
						case 2 : LADOParameter.Type = ADODB.DataTypeEnum.adSmallInt; break;
						case 8 : LADOParameter.Type = ADODB.DataTypeEnum.adBigInt; break;
						default : LADOParameter.Type = ADODB.DataTypeEnum.adInteger; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LADOParameter.Type = ADODB.DataTypeEnum.adNumeric;
					LADOParameter.NumericScale = LType.Scale;
					LADOParameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLFloatType)
				{
					SQLFloatType LType = (SQLFloatType)LParameter.Type;
					if (LType.Width == 1)
						LADOParameter.Type = ADODB.DataTypeEnum.adSingle;
					else
						LADOParameter.Type = ADODB.DataTypeEnum.adDouble;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adLongVarBinary;	
					LADOParameter.Attributes |= (int)ADODB.ParameterAttributesEnum.adParamLong;
					LADOParameter.Size = 255;
				}
				else if (LParameter.Type is SQLByteArrayType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adBinary;
					LADOParameter.Size = ((SQLByteArrayType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adLongVarChar;
					LADOParameter.Size = 255;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adDBTimeStamp;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adDBDate;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adDBTime;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adChar;
					LADOParameter.Size = 38;
					//LADOParameter.Type = ADODB.DataTypeEnum.adGUID;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LADOParameter.Type = ADODB.DataTypeEnum.adCurrency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Append(LADOParameter);
			}
		}
		
		private void SetParameters()
		{
			if (UseParameters)
			{
				SQLParameter LSQLParameter;
				for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
				{
					LSQLParameter = Parameters[FParameterIndexes[LIndex]];
					if ((LSQLParameter.Direction == SQLDirection.In) || (LSQLParameter.Direction == SQLDirection.InOut))
					{
						ADODB.Parameter LParameter = FCommand.Parameters[LIndex];
						if ((LParameter.Attributes & (int)ADODB.ParameterAttributesEnum.adParamLong) != 0)
						{
							object LValue = LSQLParameter.Value;
							if (LValue is byte[])
								LParameter.Size = ((byte[])LValue).Length;
							else if (LValue is string)
								LParameter.Size = ((string)LValue).Length;

							if (LValue == null)
								LParameter.Value = DBNull.Value;
							else
								LParameter.Value = LValue;
						}
						else
						{
							// TODO: Better story for the length of string parameters.  This usage prevents the use of prepared commands in general
							object LValue = LSQLParameter.Value;
							if ((LValue is string) && (LParameter.Size < ((string)LValue).Length))
								LParameter.Size = ((string)LValue).Length;

							if (LValue == null)
								LParameter.Value = DBNull.Value;
							else
							{
								if (LSQLParameter.Type is SQLGuidType)
									LParameter.Value = LValue.ToString();
								else
									LParameter.Value = LValue;
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
				SQLParameter LSQLParameter;
				for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
				{
					LSQLParameter = Parameters[FParameterIndexes[LIndex]];
					if ((LSQLParameter.Direction == SQLDirection.InOut) || (LSQLParameter.Direction == SQLDirection.Out) || (LSQLParameter.Direction == SQLDirection.Result))
						LSQLParameter.Value = FCommand.Parameters[LIndex].Value;
				}
			}
		}
		
		protected override void InternalExecute()
		{
			PrepareCommand(SQLIsolationLevel.Serializable);
			try
			{
				SetParameters();
				object LRecordsAffected;
				object LParameters = Missing.Value;
				FCommand.Execute(out LRecordsAffected, ref LParameters, -1);
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
		protected override SQLCursor InternalOpen(SQLCursorType ACursorType, SQLIsolationLevel AIsolationLevel)
		{
			PrepareCommand(AIsolationLevel);
			SetParameters();
			ADODB.Recordset LRecordset = new ADODB.Recordset();
			LRecordset.Source = FCommand;
			LRecordset.CacheSize = 20;
			//LRecordset.Properties["Preserve on Commit"].Value = true;
			//LRecordset.Properties["Preserve on Abort"].Value = true;
			LRecordset.Open
			(
				Missing.Value, 
				Missing.Value, 
				(
					(ACursorType == SQLCursorType.Static) ? 
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
			SQLCursor LCursor = new ADOCursor(this, LRecordset);
			GetParameters();
			return LCursor;
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
		public ADOCursor(ADOCommand ACommand, ADODB.Recordset ARecordset) : base(ACommand)
		{
			FRecordset = ARecordset;
			FValues = new ADOValue[FRecordset.Fields.Count];
			for (int LIndex = 0; LIndex < FValues.Length; LIndex++)
				FValues[LIndex] = new ADOValue();
			FBOF = true;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FRecordset != null)
			{
				if (FRecordset.State != 0)
					try
					{
						FRecordset.Close();
					}
					catch
					{
						// ignore this exception
						// if we can't close it, oh well... it shouldn't be throwing anyway
					}

				while (System.Runtime.InteropServices.Marshal.ReleaseComObject(FRecordset) > 0);
				FRecordset = null;
			}
			
			base.Dispose(ADisposing);
		}
	
		private bool FBOF;	
		private ADODB.Recordset FRecordset;
		private ADOValue[] FValues;
		
		protected override bool InternalNext()
		{
			if (FBOF)
				FBOF = false;
			else
				if (!FRecordset.EOF)
					FRecordset.MoveNext();
			ClearValues();
			return !FRecordset.EOF;
		}
		
		private void ClearValues()
		{
			for (int LIndex = 0; LIndex < FValues.Length; LIndex++)
				FValues[LIndex].IsCached = false;
		}
		
		private object GetValue(int AIndex)
		{
			if (!FValues[AIndex].IsCached)
			{
				FValues[AIndex].Value = FRecordset.Fields[AIndex].Value;
				FValues[AIndex].IsCached = true;
			}
			return FValues[AIndex].Value;
		}

		protected override SQLTableSchema InternalGetSchema()
		{
			throw new NotSupportedException();
		}		
		
		protected override int InternalGetColumnCount()
		{
			return FValues.Length;
		}

		protected override string InternalGetColumnName(int AIndex)
		{
			return FRecordset.Fields[AIndex].Name;
		}
		
		protected override object InternalGetColumnValue(int AIndex)
		{
			return GetValue(AIndex);
		}
		
		protected override bool InternalIsNull(int AIndex)
		{
			object LValue = GetValue(AIndex);
			return 
				(
					(
						(LValue == null) || 
						(LValue == System.DBNull.Value)
					)
				);
		}
		
		protected override bool InternalIsDeferred(int AIndex)
		{
			return ((FRecordset.Fields[AIndex].Attributes & (int)FieldAttributeEnum.adFldLong) != 0);
		}
		
		protected override System.IO.Stream InternalOpenDeferredStream(int AIndex)
		{
			object LValue = GetValue(AIndex);
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
		
		protected override bool InternalFindKey(object[] AKey)
		{
			FRecordset.Seek(AKey, ADODB.SeekEnum.adSeekFirstEQ);
			return !FRecordset.EOF;
		}

		protected override void InternalFindNearest(object[] AKey)
		{
			FRecordset.Seek(AKey, ADODB.SeekEnum.adSeekAfterEQ);
		}
		
		protected override string InternalGetFilter()
		{
			String LValue = FRecordset.Filter as String;
			return LValue == null ? String.Empty : LValue;
		}
		
		protected override bool InternalSetFilter(string AFilter)
		{
			FRecordset.Filter = AFilter;
			return !FRecordset.EOF;
		}

		protected override void InternalInsert(string[] ANames, object[] AValues)
		{
			FRecordset.AddNew(System.Reflection.Missing.Value, System.Reflection.Missing.Value);
			for (int LIndex = 0; LIndex < ANames.Length; LIndex++)
				FRecordset.Fields[ANames[LIndex]].Value = AValues[LIndex];
			FRecordset.Update(System.Reflection.Missing.Value, System.Reflection.Missing.Value);
		}

		protected override void InternalUpdate(string[] ANames, object[] AValues)
		{
			for (int LIndex = 0; LIndex < ANames.Length; LIndex++)
				FRecordset.Fields[ANames[LIndex]].Value = AValues[LIndex];
			FRecordset.Update(System.Reflection.Missing.Value, System.Reflection.Missing.Value);
		}

		protected override void InternalDelete()
		{
			FRecordset.Delete(ADODB.AffectEnum.adAffectCurrent);
		}
	}
}

