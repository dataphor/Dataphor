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
		public MySQLConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new MySqlConnection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new MySQLCommand(this, CreateDbCommand());
		}
	}
	
	public class MySQLCommand : DotNetCommand
	{
		public MySQLCommand(MySQLConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) {}
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			foreach (SQLParameter LParameter in Parameters)
			{
				MySqlParameter LMySQLParameter = (MySqlParameter)FCommand.CreateParameter();
				LMySQLParameter.ParameterName = String.Format("@{0}", LParameter.Name);
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LMySQLParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LMySQLParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LMySQLParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LMySQLParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LMySQLParameter.DbType = DbType.String;
					LMySQLParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LMySQLParameter.DbType = DbType.Boolean;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LMySQLParameter.DbType = DbType.Byte; break;
						case 2 : LMySQLParameter.DbType = DbType.Int16; break;
						case 8 : LMySQLParameter.DbType = DbType.Int64; break;
						default : LMySQLParameter.DbType = DbType.Int32; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LMySQLParameter.DbType = DbType.Decimal;
					LMySQLParameter.Scale = LType.Scale;
					LMySQLParameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LMySQLParameter.DbType = DbType.Binary;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LMySQLParameter.DbType = DbType.String;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LMySQLParameter.DbType = DbType.DateTime;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LMySQLParameter.DbType = DbType.DateTime;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LMySQLParameter.DbType = DbType.DateTime;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LMySQLParameter.DbType = DbType.Guid;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LMySQLParameter.DbType = DbType.Currency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LMySQLParameter);
			}
		}
	}
}

