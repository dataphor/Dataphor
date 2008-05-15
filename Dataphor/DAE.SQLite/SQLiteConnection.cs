/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.SQLite;

namespace Alphora.Dataphor.DAE.Connection
{
	public class SQLiteConnection : DotNetConnection
	{
		public SQLiteConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new System.Data.SQLite.SQLiteConnection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new SQLiteCommand(this, CreateDbCommand());
		}
		
		protected override bool IsTransactionFailure(Exception AException)
		{
			SQLiteException LException = AException as SQLiteException;
			if (LException != null)
				return IsTransactionFailure(LException.ErrorCode);

			return false;
		}
		
		protected bool IsTransactionFailure(SQLiteErrorCode AErrorCode)
		{
			return false;
		}
		
		protected bool IsUserCorrectableError(SQLiteErrorCode AErrorCode)
		{
			return AErrorCode == SQLiteErrorCode.Constraint;
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
			SQLiteException LException = AException as SQLiteException;
			if (LException != null)
			{
				if (!IsUserCorrectableError(LException.ErrorCode))
					return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
	
	public class SQLiteCommand : DotNetCommand
	{
		public SQLiteCommand(SQLiteConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) 
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
				SQLiteParameter LSQLiteParameter = (SQLiteParameter)FCommand.CreateParameter();
				LSQLiteParameter.ParameterName = String.Format("@{0}", LParameter.Name);
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LSQLiteParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LSQLiteParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LSQLiteParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LSQLiteParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LSQLiteParameter.DbType = DbType.String;
					LSQLiteParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LSQLiteParameter.DbType = DbType.Boolean;
				}
				else if (LParameter.Type is SQLByteArrayType)
				{
					LSQLiteParameter.DbType = DbType.Binary;
					LSQLiteParameter.Size = ((SQLByteArrayType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LSQLiteParameter.DbType = DbType.Byte; break;
						case 2 : LSQLiteParameter.DbType = DbType.Int16; break;
						case 8 : LSQLiteParameter.DbType = DbType.Int64; break;
						default : LSQLiteParameter.DbType = DbType.Int32; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LSQLiteParameter.DbType = DbType.Decimal;
					//LSQLiteParameter.Scale = LType.Scale;
					//LSQLiteParameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLFloatType)
				{
					SQLFloatType LType = (SQLFloatType)LParameter.Type;
					if (LType.Width == 1)
						LSQLiteParameter.DbType = DbType.Single;
					else
						LSQLiteParameter.DbType = DbType.Double;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LSQLiteParameter.DbType = DbType.Binary;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LSQLiteParameter.DbType = DbType.String;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LSQLiteParameter.DbType = DbType.Date;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LSQLiteParameter.DbType = DbType.Time;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LSQLiteParameter.DbType = DbType.DateTime;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LSQLiteParameter.DbType = DbType.Guid;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LSQLiteParameter.DbType = DbType.Currency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LSQLiteParameter);
			}
		}
	}
}