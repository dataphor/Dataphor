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
		public DB2400Connection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new IBM.iDB2Connection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new DB2400Command(this, CreateDbCommand());
		}
		
		protected override Exception InternalWrapException(Exception AException, string AStatement)
		{
			IBM.iDB2Exception LException = AException as IBM.iDB2Exception;
			if 
			(
				(LException != null) && 
				(
					(AStatement == "begin transaction") ||
					(AStatement == "commit transaction") || 
					(AStatement == "rollback transaction")
				) && 
				(LException.Errors.Count == 1) && 
				(LException.Errors[0].SqlState == "HY011")
			)
			{
				if (AStatement != "begin transaction")
				{
					FTransaction = null; // reset the transaction pointer because it was not cleared when the exception was thrown
					// clear the connection's internal pointer as well
					if (FConnection != null)
						FConnection.GetType().GetField("ad", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(FConnection, null);
				}
				return null;
			}
				
			return base.InternalWrapException(AException, AStatement);
		}
	}
	
	public class DB2400Command : DotNetCommand
	{
		public DB2400Command(DB2400Connection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) 
		{
			FUseOrdinalBinding = true;
		}
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			SQLParameter LParameter;
			for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
			{
				LParameter = Parameters[FParameterIndexes[LIndex]];
				IBM.iDB2Parameter LDB2Parameter = (IBM.iDB2Parameter)FCommand.CreateParameter();
				LDB2Parameter.ParameterName = String.Format("@{0}{1}", LParameter.Name, LIndex.ToString());
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LDB2Parameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LDB2Parameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LDB2Parameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LDB2Parameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2VarChar;
					LDB2Parameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Integer;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Integer; break;
						case 2 : LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2SmallInt; break;
						case 8 : LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2BigInt; break;
						default : LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Integer; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Decimal; // could not be decimal because of issue with DB2/400
					LDB2Parameter.Scale = LType.Scale;
					LDB2Parameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLFloatType)
				{
					SQLFloatType LType = (SQLFloatType)LParameter.Type;
					if (LType.Width == 1)
						LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Real;
					else
						LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Double;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2VarGraphic; 
					LDB2Parameter.Size = 16370; 
				}
				else if (LParameter.Type is SQLTextType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2VarChar;
					LDB2Parameter.Size = 32740; 
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2TimeStamp;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Date;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Time;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Char;
					LDB2Parameter.Size = 24;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LDB2Parameter.iDB2DbType = IBM.iDB2DbType.iDB2Decimal;
					LDB2Parameter.Scale = 28;
					LDB2Parameter.Precision = 8;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LDB2Parameter);
			}
		}
	}
}

