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
		public OracleConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new DDTek.OracleConnection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new OracleCommand(this, CreateDbCommand());
		}
	}

	public class OracleCommand : DotNetCommand
	{
		public OracleCommand(OracleConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand){}
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			foreach (SQLParameter LParameter in Parameters)
			{
				DDTek.OracleParameter LOracleParameter = (DDTek.OracleParameter)FCommand.CreateParameter();
				LOracleParameter.ParameterName = String.Format("@{0}", LParameter.Name);
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LOracleParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LOracleParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LOracleParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LOracleParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LOracleParameter.OracleDbType = DDTek.OracleDbType.VarChar;
					LOracleParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LOracleParameter.OracleDbType = DDTek.OracleDbType.Number;
					LOracleParameter.Precision = 1;
					LOracleParameter.Scale = 0;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					LOracleParameter.OracleDbType = DDTek.OracleDbType.Number;
					LOracleParameter.Scale = 0;
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LOracleParameter.Precision = 3; break;
						case 2 : LOracleParameter.Precision = 5; break;
						case 8 : LOracleParameter.Precision = 20; break;
						default : LOracleParameter.Precision = 10; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LOracleParameter.OracleDbType = DDTek.OracleDbType.Number;
					LOracleParameter.Precision = LType.Precision;
					LOracleParameter.Scale = LType.Scale;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LOracleParameter.OracleDbType = DDTek.OracleDbType.Blob;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LOracleParameter.OracleDbType = DDTek.OracleDbType.Clob;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LOracleParameter.OracleDbType = DDTek.OracleDbType.Date;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LOracleParameter.OracleDbType = DDTek.OracleDbType.Char;
					LOracleParameter.Size = 24;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LOracleParameter.OracleDbType = DDTek.OracleDbType.Number;
					LOracleParameter.Precision = 28;
					LOracleParameter.Scale = 8;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LOracleParameter);
			}
		}
	}
}

