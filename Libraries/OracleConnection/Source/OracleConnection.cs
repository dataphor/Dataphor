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
		public OracleConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new OraHome.Client.OracleConnection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new OracleCommand(this, CreateDbCommand());
		}

		protected override void InternalBeginTransaction(SQLIsolationLevel AIsolationLevel)
		{
			FIsolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (AIsolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : // all three will map to committed in this Optimisitc system
				case SQLIsolationLevel.RepeatableRead : 
				case SQLIsolationLevel.ReadCommitted : FIsolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
				case SQLIsolationLevel.Serializable : FIsolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			FTransaction = FConnection.BeginTransaction(FIsolationLevel);
		}
	}
	
	public class OracleCommand : DotNetCommand
	{
		public OracleCommand(OracleConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) 
		{
			FParameterDelimiter = ":";
		}

		protected override string PrepareStatement(string AStatement)
		{
			return base.PrepareStatement(AStatement).Replace("@", ":");
		}
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			foreach (SQLParameter LParameter in Parameters)
			{
				OraHome.Client.OracleParameter LOracleParameter = (OraHome.Client.OracleParameter)FCommand.CreateParameter();
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
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Varchar2;
					LOracleParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Int32;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Byte; break;
						case 2 : LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Int16; break;
						case 8 : 
							LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Decimal;
							LOracleParameter.Precision = 20;
							LOracleParameter.Scale = 0;
						break;
						default : LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Int32; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Decimal;
					LOracleParameter.Precision = LType.Precision;
					LOracleParameter.Scale = LType.Scale;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Blob;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Clob;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Date;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Date;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Date;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Char;
					LOracleParameter.Size = 24;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LOracleParameter.OracleDbType = OraHome.Client.OracleDbType.Decimal;
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

