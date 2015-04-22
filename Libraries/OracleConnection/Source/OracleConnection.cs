/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
	//using Oracle = System.Data.OracleClient;
using OraHome = Oracle.DataAccess;
using Alphora.Dataphor.DAE.Connection;
	
namespace Alphora.Dataphor.DAE.Connection.Oracle.Oracle
{
	public class OracleConnection : DotNetConnection
	{
		public OracleConnection(string connection) : base(connection) {}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new OraHome.Client.OracleConnection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new OracleCommand(this, CreateDbCommand());
		}

		protected override void InternalBeginTransaction(SQLIsolationLevel isolationLevel)
		{
			_isolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (isolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : // all three will map to committed in this Optimisitc system
				case SQLIsolationLevel.RepeatableRead : 
				case SQLIsolationLevel.ReadCommitted : _isolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
				case SQLIsolationLevel.Serializable : _isolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			_transaction = _connection.BeginTransaction(_isolationLevel);
		}
	}
	
	public class OracleCommand : DotNetCommand
	{
		public OracleCommand(OracleConnection connection, IDbCommand command) : base(connection, command) 
		{
			_parameterDelimiter = ":";
		}

		protected override void PrepareParameters()
		{
			// Prepare parameters
			foreach (SQLParameter parameter in Parameters)
			{
				OraHome.Client.OracleParameter oracleParameter = (OraHome.Client.OracleParameter)_command.CreateParameter();
				oracleParameter.ParameterName = String.Format("@{0}", parameter.Name);
				switch (parameter.Direction)
				{
					case SQLDirection.Out : oracleParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : oracleParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : oracleParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : oracleParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (parameter.Type is SQLStringType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Varchar2;
					oracleParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Int32;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Byte; break;
						case 2 : oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Int16; break;
						case 8 : 
							oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Decimal;
							oracleParameter.Precision = 20;
							oracleParameter.Scale = 0;
						break;
						default : oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Int32; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Decimal;
					oracleParameter.Precision = type.Precision;
					oracleParameter.Scale = type.Scale;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Blob;
				}
				else if (parameter.Type is SQLTextType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Clob;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Date;
				}
				else if (parameter.Type is SQLDateType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Date;
				}
				else if (parameter.Type is SQLTimeType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Date;
				}
				else if (parameter.Type is SQLGuidType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Char;
					oracleParameter.Size = 24;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					oracleParameter.OracleDbType = OraHome.Client.OracleDbType.Decimal;
					oracleParameter.Precision = 28;
					oracleParameter.Scale = 8;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Add(oracleParameter);
			}
		}
	}
}

