/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.Oracle
{
	using System;
	using System.Data;
	using Oracle = System.Data.OracleClient;
	using Alphora.Dataphor.DAE.Connection;
	
	public class OracleConnection : DotNetConnection
	{
		public OracleConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			try
			{
				return new Oracle.OracleConnection(AConnectionString);
			}
			catch (Exception LException)
			{
				WrapException(LException, "connect", true);
				throw;
			}
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

		protected override Exception InternalWrapException(Exception AException, string AStatement)
		{
			// Wrap all exceptions coming back with a simple Exception so that it crosses the boundary.
			return new ConnectionException(ConnectionException.Codes.SQLException, ErrorSeverity.Application, new Exception(AException.Message), AStatement);
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
			SQLParameter LParameter;
			for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
			{
				LParameter = Parameters[FParameterIndexes[LIndex]];
				Oracle.OracleParameter LOracleParameter = (Oracle.OracleParameter)FCommand.CreateParameter();
				LOracleParameter.ParameterName = String.Format(":{0}", LParameter.Name);
				LOracleParameter.IsNullable = true;
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LOracleParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LOracleParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LOracleParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LOracleParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.VarChar;
					LOracleParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.Int32;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LOracleParameter.OracleType = Oracle.OracleType.Byte; break;
						case 2 : LOracleParameter.OracleType = Oracle.OracleType.Int16; break;
						case 8 : 
							LOracleParameter.OracleType = Oracle.OracleType.Number; 
							LOracleParameter.Precision = 20;
							LOracleParameter.Scale = 0;
						break;
						default : LOracleParameter.OracleType = Oracle.OracleType.Int32; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LOracleParameter.OracleType = Oracle.OracleType.Number;
					LOracleParameter.Precision = LType.Precision;
					LOracleParameter.Scale = LType.Scale;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.Blob;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.Clob;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.DateTime;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.DateTime;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.DateTime;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.Char;
					LOracleParameter.Size = 24;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LOracleParameter.OracleType = Oracle.OracleType.Number;
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

