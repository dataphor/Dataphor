/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.SqlClient;

namespace Alphora.Dataphor.DAE.Connection
{
	public class SQLCEConnection : DotNetConnection
	{
		public SQLCEConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new SqlConnection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new SQLCECommand(this, CreateDbCommand());
		}
		
		protected override bool IsTransactionFailure(Exception AException)
		{
            SqlException LException = AException as SqlException;
			if (LException != null)
				foreach (SqlError LError in LException.Errors)
					if (IsTransactionFailure(LError))
						return true;

			return false;
		}

        protected bool IsTransactionFailure(SqlError ASqlError)
		{
            switch (ASqlError.Number)
			{
				case 1205 : return true; // Transaction was deadlocked
				case 1211 : return true; // Process was chosen as deadlock victim
				case 3928 : return true; // The marked transaction failed
				case 8650 : return true; // Intra-query parallelism caused server command to deadlock
				case 8901 : return true; // Deadlock detected during DBCC
			}
			
			return false;
		}

        protected bool IsUserCorrectableError(SqlError ASqlError)
		{
            if (ASqlError.Number >= 50000)
				return true;

            switch (ASqlError.Number)
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
			SqlException LException = AException as SqlException;
			if (LException != null)
			{
				foreach (SqlError LError in LException.Errors)
					if (!IsUserCorrectableError(LError))
						return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
	
	public class SQLCECommand : DotNetCommand
	{
		public SQLCECommand(SQLCEConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) 
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
				SqlParameter LSQLCEParameter = (SqlParameter)FCommand.CreateParameter();
				LSQLCEParameter.ParameterName = String.Format("@{0}", LParameter.Name);
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LSQLCEParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LSQLCEParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LSQLCEParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LSQLCEParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.NVarChar;
					LSQLCEParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.Bit;
				}
				else if (LParameter.Type is SQLByteArrayType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.Binary;
					LSQLCEParameter.Size = ((SQLByteArrayType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LSQLCEParameter.SqlDbType = SqlDbType.TinyInt; break;
						case 2 : LSQLCEParameter.SqlDbType = SqlDbType.SmallInt; break;
						case 8 : LSQLCEParameter.SqlDbType = SqlDbType.BigInt; break;
						default : LSQLCEParameter.SqlDbType = SqlDbType.Int; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LSQLCEParameter.SqlDbType = SqlDbType.Decimal;
					LSQLCEParameter.Scale = LType.Scale;
					LSQLCEParameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLFloatType)
				{
					SQLFloatType LType = (SQLFloatType)LParameter.Type;
					if (LType.Width == 1)
						LSQLCEParameter.SqlDbType = SqlDbType.Real;
					else
						LSQLCEParameter.SqlDbType = SqlDbType.Float;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.Image;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.NText;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.UniqueIdentifier;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LSQLCEParameter.SqlDbType = SqlDbType.Money;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LSQLCEParameter);
			}
		}
	}
}