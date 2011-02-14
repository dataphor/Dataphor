/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.DB2
{
	using System;
	using System.Data;
	using System.Reflection;
	
	using IBM = IBM.Data.DB2;
		
	using Alphora.Dataphor.DAE.Connection;
	

	public class DB2Connection : DotNetConnection
	{
		public DB2Connection(string connection) : base(connection) {}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new IBM.DB2Connection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new DB2Command(this, CreateDbCommand());
		}
		
		protected override Exception InternalWrapException(Exception exception, string statement)
		{
			IBM.DB2Exception localException = exception as IBM.DB2Exception;
			if 
			(
				(localException != null) && 
				(
					(statement == "begin transaction") ||
					(statement == "commit transaction") || 
					(statement == "rollback transaction")
				) && 
				(localException.Errors.Count == 1) && 
				(localException.Errors[0].SQLState == "HY011")
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
	
	public class DB2Command : DotNetCommand
	{
		public DB2Command(DB2Connection connection, IDbCommand command) : base(connection, command) 
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
				IBM.DB2Parameter dB2Parameter = (IBM.DB2Parameter)_command.CreateParameter();
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
					dB2Parameter.DB2Type = IBM.DB2Type.VarChar;
					dB2Parameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					dB2Parameter.DB2Type = IBM.DB2Type.Integer;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : dB2Parameter.DB2Type = IBM.DB2Type.Integer; break;
						case 2 : dB2Parameter.DB2Type = IBM.DB2Type.SmallInt; break;
						case 8 : dB2Parameter.DB2Type = IBM.DB2Type.BigInt; break;
						default : dB2Parameter.DB2Type = IBM.DB2Type.Integer; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					dB2Parameter.DB2Type = IBM.DB2Type.Decimal; // could not be decimal because of issue with DB2/400
					dB2Parameter.Scale = type.Scale;
					dB2Parameter.Precision = type.Precision;
				}
				else if (parameter.Type is SQLFloatType)
				{
					SQLFloatType type = (SQLFloatType)parameter.Type;
					if (type.Width == 1)
						dB2Parameter.DB2Type = IBM.DB2Type.Real;
					else
						dB2Parameter.DB2Type = IBM.DB2Type.Double;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					dB2Parameter.DB2Type = IBM.DB2Type.Blob;
				}
				else if (parameter.Type is SQLTextType)
				{
					dB2Parameter.DB2Type = IBM.DB2Type.Clob;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					dB2Parameter.DB2Type = IBM.DB2Type.Timestamp;
				}
				else if (parameter.Type is SQLDateType)
				{
					dB2Parameter.DB2Type = IBM.DB2Type.Date;
				}
				else if (parameter.Type is SQLTimeType)
				{
					dB2Parameter.DB2Type = IBM.DB2Type.Time;
				}
				else if (parameter.Type is SQLGuidType)
				{
					dB2Parameter.DB2Type = IBM.DB2Type.Char;
					dB2Parameter.Size = 24;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					dB2Parameter.DB2Type = IBM.DB2Type.Decimal;
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

