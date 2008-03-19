/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.OleDb;

namespace Alphora.Dataphor.DAE.Connection
{
	public class OLEDBConnection : DotNetConnection
	{
		public OLEDBConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new OleDbConnection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new OLEDBCommand(this, CreateDbCommand());
		}
		
		protected override bool IsTransactionFailure(Exception AException)
		{
			OleDbException LException = AException as OleDbException;
			if (LException != null)
				foreach (OleDbError LError in LException.Errors)
					if (IsTransactionFailure(LError.NativeError))
						return true;

			return false;
		}
		
		protected bool IsTransactionFailure(int AErrorCode)
		{
			switch (AErrorCode)
			{
				case 1205 : return true; // Transaction was deadlocked
				case 1211 : return true; // Process was chosen as deadlock victim
				case 3928 : return true; // The marked transaction failed
				case 8650 : return true; // Intra-query parallelism caused server command to deadlock
				case 8901 : return true; // Deadlock detected during DBCC
			}
			
			return false;
		}
		
		protected bool IsUserCorrectableError(int AErrorCode)
		{
			if (AErrorCode >= 50000)
				return true;
			
			switch (AErrorCode)
			{
				case 3621 : return true; // The statement has been terminated.
				case 2601 : return true; // Cannot insert duplicate key row...
				case 2627 : return true; // Violation of %ls constraint...
				case 10055 : return true; // The data violated the integrity constraint...
				case 10065 : return true; // The data violated the integrity constraint...
				case 11011 : return true; // The data violated the integrity constraint...
				case 11012 : return true; // The data violated the schema...
				case 11040 : return true; // Deleting the row violated...
				case 547 : return true; // %ls statement conflicted with...
				case 8152 : return true; // String or binary data would be truncated.
				default : return false;
			}
		}
		
		protected override Exception InternalWrapException(Exception AException, string AStatement)
		{
			ErrorSeverity LSeverity = GetExceptionSeverity(AException);
			if (LSeverity == ErrorSeverity.User)
				return new DataphorException(ErrorSeverity.User, DataphorException.CApplicationError, AException.Message);
			return base.InternalWrapException(AException, AStatement);
		}

		private ErrorSeverity GetExceptionSeverity(Exception AException)
		{
			// If the error code indicates an integrity constraint violation or other user-correctable message, severity is user, otherwise, severity is application
			OleDbException LException = AException as OleDbException;
			if (LException != null)
			{
				foreach (OleDbError LError in LException.Errors)
					if (!IsUserCorrectableError(LError.NativeError))
						return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
	
	public class OLEDBCommand : DotNetCommand
	{
		public OLEDBCommand(OLEDBConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) 
		{
			FUseOrdinalBinding = true;
		}
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			SQLParameter LParameter;
			for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
			{
				LParameter = Parameters[FParameterIndexes[LIndex]];
				OleDbParameter LOLEDBParameter = (OleDbParameter)FCommand.CreateParameter();
				LOLEDBParameter.ParameterName = String.Format("@{0}", LParameter.Name);
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LOLEDBParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LOLEDBParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LOLEDBParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LOLEDBParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LOLEDBParameter.OleDbType = OleDbType.VarChar;
					LOLEDBParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LOLEDBParameter.OleDbType = OleDbType.Boolean;
				}
				else if (LParameter.Type is SQLByteArrayType)
				{
					LOLEDBParameter.OleDbType = OleDbType.Binary;
					LOLEDBParameter.Size = ((SQLByteArrayType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LOLEDBParameter.OleDbType = OleDbType.UnsignedTinyInt; break;
						case 2 : LOLEDBParameter.OleDbType = OleDbType.SmallInt; break;
						case 8 : LOLEDBParameter.OleDbType = OleDbType.BigInt; break;
						default : LOLEDBParameter.OleDbType = OleDbType.Integer; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LOLEDBParameter.OleDbType = OleDbType.Numeric;
					LOLEDBParameter.Scale = LType.Scale;
					LOLEDBParameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLFloatType)
				{
					SQLFloatType LType = (SQLFloatType)LParameter.Type;
					if (LType.Width == 1)
						LOLEDBParameter.OleDbType = OleDbType.Single;
					else
						LOLEDBParameter.OleDbType = OleDbType.Double;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LOLEDBParameter.OleDbType = OleDbType.LongVarBinary;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LOLEDBParameter.OleDbType = OleDbType.LongVarChar;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LOLEDBParameter.OleDbType = OleDbType.DBDate;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LOLEDBParameter.OleDbType = OleDbType.Date;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LOLEDBParameter.OleDbType = OleDbType.Date;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LOLEDBParameter.OleDbType = OleDbType.Guid;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LOLEDBParameter.OleDbType = OleDbType.Currency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LOLEDBParameter);
			}
		}
	}
}