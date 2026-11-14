/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Data;

namespace Alphora.Dataphor.DAE.Connection
{
	public abstract class DotNetConnection : SQLConnection
	{
		public DotNetConnection(string AConnectionString) : base()
		{
			FConnection = CreateDbConnection(AConnectionString);
			try
			{
				FConnection.Open();
			}
			catch (Exception LException)
			{
				WrapException(LException, "connect", false);
			}
			SetState(SQLConnectionState.Idle);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FConnection != null)
				{
					try
					{
						FConnection.Dispose();
					}
					finally
					{
						SetState(SQLConnectionState.Closed);
						FConnection = null;
					}
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		protected abstract IDbConnection CreateDbConnection(string AConnectionString);
		
		protected IDbCommand CreateDbCommand()
		{
			IDbCommand LCommand = FConnection.CreateCommand();
			if (FTransaction != null)
				LCommand.Transaction = FTransaction;
			return LCommand;
		}
		
		protected IDbConnection FConnection;
		protected IDbTransaction FTransaction;
		protected System.Data.IsolationLevel FIsolationLevel;
		
		protected override void InternalBeginTransaction(SQLIsolationLevel AIsolationLevel)
		{
			FIsolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (AIsolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : FIsolationLevel = System.Data.IsolationLevel.ReadUncommitted; break;
				case SQLIsolationLevel.ReadCommitted : FIsolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
				case SQLIsolationLevel.RepeatableRead : FIsolationLevel = System.Data.IsolationLevel.RepeatableRead; break;
				case SQLIsolationLevel.Serializable : FIsolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			FTransaction = FConnection.BeginTransaction(FIsolationLevel);
		}

		protected override void InternalCommitTransaction()
		{
			FTransaction.Commit();
			FTransaction = null;
		}

		protected override void InternalRollbackTransaction()
		{
			try
			{
				FTransaction.Rollback();
			}
			finally
			{
				FTransaction = null;			
			}
		}
		
		public override bool IsConnectionValid()
		{
			try
			{
				return (FConnection != null) && (FConnection.State != ConnectionState.Closed);
			}
			catch 
			{
				return false;
			}
		}
	}
	
	public abstract class DotNetCommand : SQLCommand
	{
		public DotNetCommand(DotNetConnection AConnection, IDbCommand ACommand) : base(AConnection)
		{
			FCommand = ACommand;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				UnprepareCommand();
			}
			finally
			{
				base.Dispose(ADisposing);	
			}
		}
		
		public new DotNetConnection Connection { get { return (DotNetConnection)base.Connection; } }

		protected IDbCommand FCommand;
		
		protected void PrepareCommand(bool AExecute, SQLIsolationLevel AIsolationLevel)
		{
			FCommand.CommandText = PrepareStatement(Statement);
			switch (CommandType)
			{
				case SQLCommandType.Statement : FCommand.CommandType = System.Data.CommandType.Text; break;
				case SQLCommandType.Table : FCommand.CommandType = System.Data.CommandType.TableDirect; break;
			}
			if (UseParameters)
				PrepareParameters();
			//FCommand.Prepare();//commented out because the MS Oracle Provider gets an iteration exception when prepared
			// Todo: put back in when MS Oracle can prepare
		}

		protected void UnprepareCommand()
		{
			ClearParameters();
		}
		
		protected override void InternalPrepare()
		{
		}
		
		protected override void InternalUnprepare()
		{
		}

		#region Parameters

		protected abstract void PrepareParameters();

		protected virtual void SetParameters()
		{
			if (UseParameters)
			{
				SQLParameter LParameter;
				for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
				{
					LParameter = Parameters[FParameterIndexes[LIndex]];
					if ((LParameter.Direction == SQLDirection.In) || (LParameter.Direction == SQLDirection.InOut))
						if (LParameter.Value == null)
							ParameterByIndex(LIndex).Value = DBNull.Value;
						else
						{
							// TODO: Better story for the length of string parameters.  This usage prevents the use of prepared commands in general
							IDbDataParameter LDataParameter = (IDbDataParameter)ParameterByIndex(LIndex);
							if ((LParameter.Value is string) && (LDataParameter.Size < ((string)LParameter.Value).Length))
								LDataParameter.Size = ((string)LParameter.Value).Length;
							LDataParameter.Value = LParameter.Value;
						}
				}
			}
		}

		protected virtual void GetParameters()
		{
			if (UseParameters)
			{
				SQLParameter LParameter;
				for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
				{
					LParameter = Parameters[FParameterIndexes[LIndex]];
					if ((LParameter.Direction == SQLDirection.InOut) || (LParameter.Direction == SQLDirection.Out) || (LParameter.Direction == SQLDirection.Result))
					{
						if (ParameterByIndex(LIndex).Value == DBNull.Value)
							LParameter.Value = null;
						else
							LParameter.Value = ParameterByIndex(LIndex).Value;
					}
				}
			}
		}

		/// <summary> Access parameters by index through this routine to allow descendants to change the semantics. </summary>
		protected virtual IDataParameter ParameterByIndex(int AIndex)
		{
			return (IDataParameter)FCommand.Parameters[AIndex];
		}

		protected virtual void ClearParameters()
		{
			FCommand.Parameters.Clear();
		}

		#endregion

		protected override void InternalExecute()
		{
			PrepareCommand(true, SQLIsolationLevel.Serializable);
			try
			{
				SetParameters();
				FCommand.ExecuteNonQuery();
				GetParameters();
			}
			finally
			{
				UnprepareCommand();
			}
		}
		
		protected virtual CommandBehavior SQLCommandBehaviorToCommandBehavior(SQLCommandBehavior ACommandBehavior)
		{
			System.Data.CommandBehavior LBehavior = System.Data.CommandBehavior.Default | System.Data.CommandBehavior.SingleResult;
			
			if ((ACommandBehavior & SQLCommandBehavior.KeyInfo) != 0)
				LBehavior |= System.Data.CommandBehavior.KeyInfo;
				
			if ((ACommandBehavior & SQLCommandBehavior.SchemaOnly) != 0)
				LBehavior |= System.Data.CommandBehavior.SchemaOnly;
				
			return LBehavior;
		}
		
		protected override SQLCursor InternalOpen(SQLCursorType ACursorType, SQLIsolationLevel AIsolationLevel)
		{
			PrepareCommand(false, AIsolationLevel);
			SetParameters();
			IDataReader LCursor = FCommand.ExecuteReader(SQLCommandBehaviorToCommandBehavior(CommandBehavior));
			GetParameters();
			return new DotNetCursor(this, LCursor);
		}
		
		protected internal override void InternalClose()
		{
			UnprepareCommand();
		}
		
		public void Cancel()
		{
			if ((FCommand != null) && (Connection != null) && Connection.IsConnectionValid())
				FCommand.Cancel();
		}
	}
	
	public class DotNetCursor : SQLCursor
	{
		public DotNetCursor(DotNetCommand ACommand, IDataReader ACursor) : base(ACommand)
		{
			FCursor = ACursor;
			FRecord = (IDataRecord)ACursor;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FCursor != null)
			{
                try
                {
                    FCursor.Dispose();
                }
                finally
                {
					FCursor = null;
					FRecord = null;
                }
			}
			base.Dispose(ADisposing);
		}
		
		protected IDataReader FCursor;
		protected IDataRecord FRecord;
		
		protected bool IsNull(object AValue)
		{
			return (AValue == null) || (AValue == DBNull.Value);
		}
		
		protected override SQLTableSchema InternalGetSchema()
		{
			DataTable LDataTable = FCursor.GetSchemaTable();
			SQLTableSchema LSchema = new SQLTableSchema();
			
			if (LDataTable.Rows.Count > 0)
			{
				SQLIndex LIndex = new SQLIndex("");
				LIndex.IsUnique = true;
				
				int LColumnIndex = 0;
				foreach (System.Data.DataRow LRow in LDataTable.Rows)
				{
					string LColumnName = (string)LRow["ColumnName"];

					object LValue = LRow["IsUnique"];
					bool LIsUnique = IsNull(LValue) ? false : (bool)LValue;

					LValue = LRow["IsKey"];
					bool LIsKey = IsNull(LValue) ? false : (bool)LValue;
					
					if (LIsUnique || LIsKey)
						LIndex.Columns.Add(new SQLIndexColumn(LColumnName));
					
					Type LDataType = FCursor.GetFieldType(LColumnIndex);
					
					LValue = LRow["ColumnSize"];
					int LLength = IsNull(LValue) ? 0 : (int)LValue;
					
					LValue = LRow["NumericPrecision"];
					int LPrecision = IsNull(LValue) ? 0 : Convert.ToInt32(LValue);
					
					LValue = LRow["NumericScale"];
					int LScale = IsNull(LValue) ? 0 : Convert.ToInt32(LValue);
					
					LValue = LRow["IsLong"];
					bool LIsLong = IsNull(LValue) ? false : (bool)LValue;

                    LValue = LRow["IsHidden"];
                    bool LIsHidden = IsNull(LValue) ? false : (bool)LValue;

                    if (!LIsHidden)
    					LSchema.Columns.Add(new SQLColumn(LColumnName, new SQLDomain(LDataType, LLength, LPrecision, LScale, LIsLong)));
					
					LColumnIndex++;
				}

				if (LIndex.Columns.Count > 0)
					LSchema.Indexes.Add(LIndex);
			}
			else
			{
				for (int LIndex = 0; LIndex < FCursor.FieldCount; LIndex++)
					LSchema.Columns.Add(new SQLColumn(FCursor.GetName(LIndex), new SQLDomain(FCursor.GetFieldType(LIndex), 0, 0, 0, false)));
			}
			
			return LSchema;
		}
		
		protected override int InternalGetColumnCount()
		{
			return ((System.Data.Common.DbDataReader)FCursor).VisibleFieldCount;	// Visible field count hides internal fields like "rowstat" returned by server side cursors
		}
		
		protected override bool InternalNext()
		{
			return FCursor.Read();
		}
		
		protected override object InternalGetColumnValue(int AIndex)
		{
			return FRecord.GetValue(AIndex);
		}
		
		protected override bool InternalIsNull(int AIndex)
		{
			return FRecord.IsDBNull(AIndex);
		}
		
		protected override bool InternalIsDeferred(int AIndex)
		{
			switch (FRecord.GetDataTypeName(AIndex).ToLower())
			{
				case "image": 
					return true;
				default: return false;
			}
		}
		
		protected override Stream InternalOpenDeferredStream(int AIndex)
		{
			switch (FRecord.GetDataTypeName(AIndex).ToLower())
			{
				case "image": 
					long LLength = FRecord.GetBytes(AIndex, 0, null, 0, 0);
					if (LLength > Int32.MaxValue)
						throw new ConnectionException(ConnectionException.Codes.DeferredOverflow);
						
					byte[] LData = new byte[(int)LLength];
					FRecord.GetBytes(AIndex, 0, LData, 0, LData.Length);
					return new MemoryStream(LData);

				default: throw new ConnectionException(ConnectionException.Codes.NonDeferredDataType, FRecord.GetDataTypeName(AIndex));
			}
		}
	}
}

