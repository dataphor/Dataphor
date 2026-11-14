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
		public OLEDBConnection(string connection) : base(connection) {}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new OleDbConnection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new OLEDBCommand(this, CreateDbCommand());
		}
		
		protected override bool IsTransactionFailure(Exception exception)
		{
			OleDbException localException = exception as OleDbException;
			if (localException != null)
				foreach (OleDbError error in localException.Errors)
					if (IsTransactionFailure(error.NativeError))
						return true;

			return false;
		}
		
		protected bool IsTransactionFailure(int errorCode)
		{
			switch (errorCode)
			{
				case 1205 : return true; // Transaction was deadlocked
				case 1211 : return true; // Process was chosen as deadlock victim
				case 3928 : return true; // The marked transaction failed
				case 8650 : return true; // Intra-query parallelism caused server command to deadlock
				case 8901 : return true; // Deadlock detected during DBCC
			}
			
			return false;
		}
		
		protected bool IsUserCorrectableError(int errorCode)
		{
			if (errorCode >= 50000)
				return true;
			
			switch (errorCode)
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
		
		protected override Exception InternalWrapException(Exception exception, string statement)
		{
			ErrorSeverity severity = GetExceptionSeverity(exception);
			if (severity == ErrorSeverity.User)
				return new DataphorException(ErrorSeverity.User, DataphorException.ApplicationError, exception.Message);
			return base.InternalWrapException(exception, statement);
		}

		private ErrorSeverity GetExceptionSeverity(Exception exception)
		{
			// If the error code indicates an integrity constraint violation or other user-correctable message, severity is user, otherwise, severity is application
			OleDbException localException = exception as OleDbException;
			if (localException != null)
			{
				foreach (OleDbError error in localException.Errors)
					if (!IsUserCorrectableError(error.NativeError))
						return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
	
	public class OLEDBCommand : DotNetCommand
	{
		public OLEDBCommand(OLEDBConnection connection, IDbCommand command) : base(connection, command) 
		{
			_useOrdinalBinding = true;
		}
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			SQLParameter parameter;
			for (int index = 0; index < _parameterIndexes.Length; index++)
			{
				parameter = Parameters[_parameterIndexes[index]];
				OleDbParameter oLEDBParameter = (OleDbParameter)_command.CreateParameter();
				oLEDBParameter.ParameterName = String.Format("@{0}", parameter.Name);
				switch (parameter.Direction)
				{
					case SQLDirection.Out : oLEDBParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : oLEDBParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : oLEDBParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : oLEDBParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (parameter.Type is SQLStringType)
				{
					oLEDBParameter.OleDbType = OleDbType.VarChar;
					oLEDBParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					oLEDBParameter.OleDbType = OleDbType.Boolean;
				}
				else if (parameter.Type is SQLByteArrayType)
				{
					oLEDBParameter.OleDbType = OleDbType.Binary;
					oLEDBParameter.Size = ((SQLByteArrayType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : oLEDBParameter.OleDbType = OleDbType.UnsignedTinyInt; break;
						case 2 : oLEDBParameter.OleDbType = OleDbType.SmallInt; break;
						case 8 : oLEDBParameter.OleDbType = OleDbType.BigInt; break;
						default : oLEDBParameter.OleDbType = OleDbType.Integer; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					oLEDBParameter.OleDbType = OleDbType.Numeric;
					oLEDBParameter.Scale = type.Scale;
					oLEDBParameter.Precision = type.Precision;
				}
				else if (parameter.Type is SQLFloatType)
				{
					SQLFloatType type = (SQLFloatType)parameter.Type;
					if (type.Width == 1)
						oLEDBParameter.OleDbType = OleDbType.Single;
					else
						oLEDBParameter.OleDbType = OleDbType.Double;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					oLEDBParameter.OleDbType = OleDbType.LongVarBinary;
				}
				else if (parameter.Type is SQLTextType)
				{
					oLEDBParameter.OleDbType = OleDbType.LongVarChar;
				}
				else if (parameter.Type is SQLDateType)
				{
					oLEDBParameter.OleDbType = OleDbType.DBDate;
				}
				else if (parameter.Type is SQLTimeType)
				{
					oLEDBParameter.OleDbType = OleDbType.Date;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					oLEDBParameter.OleDbType = OleDbType.Date;
				}
				else if (parameter.Type is SQLGuidType)
				{
					oLEDBParameter.OleDbType = OleDbType.Guid;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					oLEDBParameter.OleDbType = OleDbType.Currency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Add(oLEDBParameter);
			}
		}
	}
}