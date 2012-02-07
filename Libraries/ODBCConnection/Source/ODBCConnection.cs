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
		public ODBCConnection(string connection) : base(connection) {}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new OdbcConnection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new ODBCCommand(this, CreateDbCommand());
		}
	}
	
	public class ODBCCommand : DotNetCommand
	{
		public ODBCCommand(ODBCConnection connection, IDbCommand command) : base(connection, command) 
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
				OdbcParameter oDBCParameter = (OdbcParameter)_command.CreateParameter();
				oDBCParameter.ParameterName = String.Format("@{0}", parameter.Name);
				switch (parameter.Direction)
				{
					case SQLDirection.Out : oDBCParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : oDBCParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : oDBCParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : oDBCParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (parameter.Type is SQLStringType)
				{
					oDBCParameter.OdbcType = OdbcType.VarChar;
					oDBCParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					oDBCParameter.OdbcType = OdbcType.Bit;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : oDBCParameter.OdbcType = OdbcType.TinyInt; break;
						case 2 : oDBCParameter.OdbcType = OdbcType.SmallInt; break;
						case 8 : oDBCParameter.OdbcType = OdbcType.BigInt; break;
						default : oDBCParameter.OdbcType = OdbcType.Int; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					oDBCParameter.OdbcType = OdbcType.Decimal; // could not be decimal because of issue with DB2/400
					oDBCParameter.Scale = type.Scale;
					oDBCParameter.Precision = type.Precision;
				}
				else if (parameter.Type is SQLFloatType)
				{
					SQLFloatType type = (SQLFloatType)parameter.Type;
					if (type.Width == 1)
						oDBCParameter.OdbcType = OdbcType.Real;
					else
						oDBCParameter.OdbcType = OdbcType.Double;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					oDBCParameter.OdbcType = OdbcType.Image;
				}
				else if (parameter.Type is SQLTextType)
				{
					oDBCParameter.OdbcType = OdbcType.Text;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					oDBCParameter.OdbcType = OdbcType.DateTime;
				}
				else if (parameter.Type is SQLDateType)
				{
					oDBCParameter.OdbcType = OdbcType.Date;
				}
				else if (parameter.Type is SQLTimeType)
				{
					oDBCParameter.OdbcType = OdbcType.Time;
				}
				else if (parameter.Type is SQLGuidType)
				{
					oDBCParameter.OdbcType = OdbcType.UniqueIdentifier;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					oDBCParameter.OdbcType = OdbcType.Decimal;
					oDBCParameter.Scale = 28;
					oDBCParameter.Precision = 8;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Add(oDBCParameter);
			}
		}
	}
}

