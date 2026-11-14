/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.SQLite;

namespace Alphora.Dataphor.DAE.Connection
{
    public class SQLiteCommand : DotNetCommand
    {
        public SQLiteCommand(SQLiteConnection connection, IDbCommand command) : base(connection, command) 
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
                SQLiteParameter sQLiteParameter = (SQLiteParameter)_command.CreateParameter();
                sQLiteParameter.ParameterName = String.Format("@{0}", parameter.Name);
                switch (parameter.Direction)
                {
                    case SQLDirection.Out : sQLiteParameter.Direction = System.Data.ParameterDirection.Output; break;
                    case SQLDirection.InOut : sQLiteParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
                    case SQLDirection.Result : sQLiteParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
                    default : sQLiteParameter.Direction = System.Data.ParameterDirection.Input; break;
                }

                if (parameter.Type is SQLStringType)
                {
                    sQLiteParameter.DbType = DbType.String;
                    sQLiteParameter.Size = ((SQLStringType)parameter.Type).Length;
                }
                else if (parameter.Type is SQLBooleanType)
                {
                    sQLiteParameter.DbType = DbType.Boolean;
                }
                else if (parameter.Type is SQLByteArrayType)
                {
                    sQLiteParameter.DbType = DbType.Binary;
                    sQLiteParameter.Size = ((SQLByteArrayType)parameter.Type).Length;
                }
                else if (parameter.Type is SQLIntegerType)
                {
                    switch (((SQLIntegerType)parameter.Type).ByteCount)
                    {
                        case 1 : sQLiteParameter.DbType = DbType.Byte; break;
                        case 2 : sQLiteParameter.DbType = DbType.Int16; break;
                        case 8 : sQLiteParameter.DbType = DbType.Int64; break;
                        default : sQLiteParameter.DbType = DbType.Int32; break;
                    }
                }
                else if (parameter.Type is SQLNumericType)
                {
                    SQLNumericType type = (SQLNumericType)parameter.Type;
                    sQLiteParameter.DbType = DbType.Decimal;
                    //LSQLiteParameter.Scale = LType.Scale;
                    //LSQLiteParameter.Precision = LType.Precision;
                }
                else if (parameter.Type is SQLFloatType)
                {
                    SQLFloatType type = (SQLFloatType)parameter.Type;
                    if (type.Width == 1)
                        sQLiteParameter.DbType = DbType.Single;
                    else
                        sQLiteParameter.DbType = DbType.Double;
                }
                else if (parameter.Type is SQLBinaryType)
                {
                    sQLiteParameter.DbType = DbType.Binary;
                }
                else if (parameter.Type is SQLTextType)
                {
                    sQLiteParameter.DbType = DbType.String;
                }
                else if (parameter.Type is SQLDateType)
                {
                    sQLiteParameter.DbType = DbType.Date;
                }
                else if (parameter.Type is SQLTimeType)
                {
                    sQLiteParameter.DbType = DbType.Time;
                }
                else if (parameter.Type is SQLDateTimeType)
                {
                    sQLiteParameter.DbType = DbType.DateTime;
                }
                else if (parameter.Type is SQLGuidType)
                {
                    sQLiteParameter.DbType = DbType.Guid;
                }
                else if (parameter.Type is SQLMoneyType)
                {
                    sQLiteParameter.DbType = DbType.Currency;
                }
                else
                    throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
                _command.Parameters.Add(sQLiteParameter);
            }
        }
    }
}