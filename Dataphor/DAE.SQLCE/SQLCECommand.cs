/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.SqlServerCe;

namespace Alphora.Dataphor.DAE.Connection
{
    public class SQLCECommand : DotNetCommand
    {
        public SQLCECommand(SQLCEConnection connection, IDbCommand command) : base(connection, command) 
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
                SqlCeParameter sQLCEParameter = (SqlCeParameter)_command.CreateParameter();
                sQLCEParameter.ParameterName = String.Format("@{0}", parameter.Name);
                switch (parameter.Direction)
                {
                    case SQLDirection.Out : sQLCEParameter.Direction = System.Data.ParameterDirection.Output; break;
                    case SQLDirection.InOut : sQLCEParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
                    case SQLDirection.Result : sQLCEParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
                    default : sQLCEParameter.Direction = System.Data.ParameterDirection.Input; break;
                }

                if (parameter.Type is SQLStringType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.NVarChar;
                    sQLCEParameter.Size = ((SQLStringType)parameter.Type).Length;
                }
                else if (parameter.Type is SQLBooleanType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.Bit;
                }
                else if (parameter.Type is SQLByteArrayType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.Binary;
                    sQLCEParameter.Size = ((SQLByteArrayType)parameter.Type).Length;
                }
                else if (parameter.Type is SQLIntegerType)
                {
                    switch (((SQLIntegerType)parameter.Type).ByteCount)
                    {
                        case 1 : sQLCEParameter.SqlDbType = SqlDbType.TinyInt; break;
                        case 2 : sQLCEParameter.SqlDbType = SqlDbType.SmallInt; break;
                        case 8 : sQLCEParameter.SqlDbType = SqlDbType.BigInt; break;
                        default : sQLCEParameter.SqlDbType = SqlDbType.Int; break;
                    }
                }
                else if (parameter.Type is SQLNumericType)
                {
                    SQLNumericType type = (SQLNumericType)parameter.Type;
                    sQLCEParameter.SqlDbType = SqlDbType.Decimal;
                    sQLCEParameter.Scale = type.Scale;
                    sQLCEParameter.Precision = type.Precision;
                }
                else if (parameter.Type is SQLFloatType)
                {
                    SQLFloatType type = (SQLFloatType)parameter.Type;
                    if (type.Width == 1)
                        sQLCEParameter.SqlDbType = SqlDbType.Real;
                    else
                        sQLCEParameter.SqlDbType = SqlDbType.Float;
                }
                else if (parameter.Type is SQLBinaryType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.Image;
                }
                else if (parameter.Type is SQLTextType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.NText;
                }
                else if (parameter.Type is SQLDateType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.DateTime;
                }
                else if (parameter.Type is SQLTimeType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.DateTime;
                }
                else if (parameter.Type is SQLDateTimeType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.DateTime;
                }
                else if (parameter.Type is SQLGuidType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.UniqueIdentifier;
                }
                else if (parameter.Type is SQLMoneyType)
                {
                    sQLCEParameter.SqlDbType = SqlDbType.Money;
                }
                else
                    throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
                _command.Parameters.Add(sQLCEParameter);
            }
        }
        
        protected SqlCeCommand Command { get { return (SqlCeCommand)_command; } }
        
        public SQLCECursor ExecuteResultSet(string tableName, string indexName, DbRangeOptions rangeOptions, object[] startValues, object[] endValues, ResultSetOptions resultSetOptions)
        {
			Command.CommandType = System.Data.CommandType.TableDirect;
			Command.CommandText= tableName;
			Command.IndexName = indexName;
			Command.SetRange(rangeOptions, startValues, endValues);
			#if SQLSTORETIMING
			long startTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif

	            return new SQLCECursor(this, Command.ExecuteResultSet(resultSetOptions));
			
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteResultSet", ATableName, AIndexName, AStartValues != null && AEndValues == null, AStartValues != null && AEndValues != null, (ResultSetOptions.Updatable & AResultSetOptions) != 0, TimingUtility.TimeSpanFromTicks(startTicks)));
			}
			#endif
        }
    }
}