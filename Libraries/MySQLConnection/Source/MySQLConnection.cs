/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.MySQL
{
	using System;
	using System.Data;
	using EID.MySqlClient;
	
	public class MySQLConnection : DotNetConnection
	{
		public MySQLConnection(string connection) : base(connection) {}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new MySqlConnection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new MySQLCommand(this, CreateDbCommand());
		}
	}
	
	public class MySQLCommand : DotNetCommand
	{
		public MySQLCommand(MySQLConnection connection, IDbCommand command) : base(connection, command) {}
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			foreach (SQLParameter parameter in Parameters)
			{
				MySqlParameter mySQLParameter = (MySqlParameter)_command.CreateParameter();
				mySQLParameter.ParameterName = String.Format("@{0}", parameter.Name);
				switch (parameter.Direction)
				{
					case SQLDirection.Out : mySQLParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : mySQLParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : mySQLParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : mySQLParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (parameter.Type is SQLStringType)
				{
					mySQLParameter.DbType = DbType.String;
					mySQLParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					mySQLParameter.DbType = DbType.Boolean;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : mySQLParameter.DbType = DbType.Byte; break;
						case 2 : mySQLParameter.DbType = DbType.Int16; break;
						case 8 : mySQLParameter.DbType = DbType.Int64; break;
						default : mySQLParameter.DbType = DbType.Int32; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					mySQLParameter.DbType = DbType.Decimal;
					mySQLParameter.Scale = type.Scale;
					mySQLParameter.Precision = type.Precision;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					mySQLParameter.DbType = DbType.Binary;
				}
				else if (parameter.Type is SQLTextType)
				{
					mySQLParameter.DbType = DbType.String;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					mySQLParameter.DbType = DbType.DateTime;
				}
				else if (parameter.Type is SQLDateType)
				{
					mySQLParameter.DbType = DbType.DateTime;
				}
				else if (parameter.Type is SQLTimeType)
				{
					mySQLParameter.DbType = DbType.DateTime;
				}
				else if (parameter.Type is SQLGuidType)
				{
					mySQLParameter.DbType = DbType.Guid;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					mySQLParameter.DbType = DbType.Currency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Add(mySQLParameter);
			}
		}
	}
}

