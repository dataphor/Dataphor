/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.DataDirect.Oracle
{
	using System;
	using System.Data;
	using DDTek = DDTek.Oracle;
	using Alphora.Dataphor.DAE.Connection;
	
	public class OracleConnection : DotNetConnection
	{
		public OracleConnection(string connection) : base(connection) {}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new DDTek.OracleConnection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new OracleCommand(this, CreateDbCommand());
		}
	}

	public class OracleCommand : DotNetCommand
	{
		public OracleCommand(OracleConnection connection, IDbCommand command) : base(connection, command){}
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			foreach (SQLParameter parameter in Parameters)
			{
				DDTek.OracleParameter oracleParameter = (DDTek.OracleParameter)_command.CreateParameter();
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
					oracleParameter.OracleDbType = DDTek.OracleDbType.VarChar;
					oracleParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					oracleParameter.OracleDbType = DDTek.OracleDbType.Number;
					oracleParameter.Precision = 1;
					oracleParameter.Scale = 0;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					oracleParameter.OracleDbType = DDTek.OracleDbType.Number;
					oracleParameter.Scale = 0;
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : oracleParameter.Precision = 3; break;
						case 2 : oracleParameter.Precision = 5; break;
						case 8 : oracleParameter.Precision = 20; break;
						default : oracleParameter.Precision = 10; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					oracleParameter.OracleDbType = DDTek.OracleDbType.Number;
					oracleParameter.Precision = type.Precision;
					oracleParameter.Scale = type.Scale;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					oracleParameter.OracleDbType = DDTek.OracleDbType.Blob;
				}
				else if (parameter.Type is SQLTextType)
				{
					oracleParameter.OracleDbType = DDTek.OracleDbType.Clob;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					oracleParameter.OracleDbType = DDTek.OracleDbType.Date;
				}
				else if (parameter.Type is SQLGuidType)
				{
					oracleParameter.OracleDbType = DDTek.OracleDbType.Char;
					oracleParameter.Size = 24;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					oracleParameter.OracleDbType = DDTek.OracleDbType.Number;
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

