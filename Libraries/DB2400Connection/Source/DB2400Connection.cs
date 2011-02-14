/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.DB2400
{
	using System;
	using System.Data;
	using System.Reflection;	
	using IBM = IBM.Data.DB2.iSeries;		
	using Alphora.Dataphor.DAE.Connection;
	

	public class DB2400Connection : DotNetConnection
	{
		public DB2400Connection(string connection) : base(connection) {}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new IBM.iDB2Connection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new DB2400Command(this, CreateDbCommand());
		}
		
		protected override Exception InternalWrapException(Exception exception, string statement)
		{
			IBM.iDB2Exception localException = exception as IBM.iDB2Exception;
			if 
			(
				(localException != null) && 
				(
					(statement == "begin transaction") ||
					(statement == "commit transaction") || 
					(statement == "rollback transaction")
				) && 
				(localException.Errors.Count == 1) && 
				(localException.Errors[0].SqlState == "HY011")
			)
			{
				if (statement != "begin transaction")
				{
					_transaction = null; // reset the transaction pointer because it was not cleared when the exception was thrown
					// clear the connection's internal pointer as well
					if (_connection != null)
						_connection.GetType().GetField("ad", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_connection, null);
				}
				return null;
			}
				
			return base.InternalWrapException(exception, statement);
		}
	}
	
	public class DB2400Command : DotNetCommand
	{
		public DB2400Command(DB2400Connection connection, IDbCommand command) : base(connection, command) 
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
				IBM.iDB2Parameter dB2Parameter = (IBM.iDB2Parameter)_command.CreateParameter();
				dB2Parameter.ParameterName = String.Format("@{0}{1}", parameter.Name, index.ToString());
				switch (parameter.Direction)
				{
					case SQLDirection.Out : dB2Parameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : dB2Parameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : dB2Parameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : dB2Parameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (parameter.Type is SQLStringType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2VarChar;
					dB2Parameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Integer;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Integer; break;
						case 2 : dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2SmallInt; break;
						case 8 : dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2BigInt; break;
						default : dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Integer; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Decimal; // could not be decimal because of issue with DB2/400
					dB2Parameter.Scale = type.Scale;
					dB2Parameter.Precision = type.Precision;
				}
				else if (parameter.Type is SQLFloatType)
				{
					SQLFloatType type = (SQLFloatType)parameter.Type;
					if (type.Width == 1)
						dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Real;
					else
						dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Double;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2VarGraphic; 
					dB2Parameter.Size = 16370; 
				}
				else if (parameter.Type is SQLTextType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2VarChar;
					dB2Parameter.Size = 32740; 
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2TimeStamp;
				}
				else if (parameter.Type is SQLDateType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Date;
				}
				else if (parameter.Type is SQLTimeType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Time;
				}
				else if (parameter.Type is SQLGuidType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Char;
					dB2Parameter.Size = 24;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					dB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Decimal;
					dB2Parameter.Scale = 28;
					dB2Parameter.Precision = 8;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Add(dB2Parameter);
			}
		}
	}
}

