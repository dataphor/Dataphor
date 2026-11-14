/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.ODBC
{
	using System;
	using System.Data;
	using System.Data.Odbc;
	using Alphora.Dataphor.DAE.Connection;
	
	public class ODBCConnection : DotNetConnection
	{
		public ODBCConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new OdbcConnection(AConnectionString);
		}
		
		public override SQLCommand CreateCommand()
		{
			return new ODBCCommand(this, CreateDbCommand());
		}
	}
	
	public class ODBCCommand : DotNetCommand
	{
		public ODBCCommand(ODBCConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) 
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
				OdbcParameter LODBCParameter = (OdbcParameter)FCommand.CreateParameter();
				LODBCParameter.ParameterName = String.Format("@{0}", LParameter.Name);
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LODBCParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LODBCParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LODBCParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LODBCParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LODBCParameter.OdbcType = OdbcType.VarChar;
					LODBCParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LODBCParameter.OdbcType = OdbcType.Bit;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LODBCParameter.OdbcType = OdbcType.TinyInt; break;
						case 2 : LODBCParameter.OdbcType = OdbcType.SmallInt; break;
						case 8 : LODBCParameter.OdbcType = OdbcType.BigInt; break;
						default : LODBCParameter.OdbcType = OdbcType.Int; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LODBCParameter.OdbcType = OdbcType.Int; // could not be decimal because of issue with DB2/400
					LODBCParameter.Scale = LType.Scale;
					LODBCParameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LODBCParameter.OdbcType = OdbcType.Image;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LODBCParameter.OdbcType = OdbcType.Text;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LODBCParameter.OdbcType = OdbcType.DateTime;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LODBCParameter.OdbcType = OdbcType.DateTime;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LODBCParameter.OdbcType = OdbcType.DateTime;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LODBCParameter.OdbcType = OdbcType.UniqueIdentifier;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LODBCParameter.OdbcType = OdbcType.Decimal;
					LODBCParameter.Scale = 28;
					LODBCParameter.Precision = 8;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LODBCParameter);
			}
		}
	}
}

