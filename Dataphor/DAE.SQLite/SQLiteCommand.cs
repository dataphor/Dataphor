/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace Alphora.Dataphor.DAE.Connection{

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
                SqliteParameter sqliteParameter = (SqliteParameter)_command.CreateParameter();
                sqliteParameter.ParameterName = String.Format("@{0}", parameter.Name);
                switch (parameter.Direction)
                {
                    case SQLDirection.Out : sqliteParameter.Direction = System.Data.ParameterDirection.Output; break;
                    case SQLDirection.InOut : sqliteParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
                    case SQLDirection.Result : sqliteParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
                    default : sqliteParameter.Direction = System.Data.ParameterDirection.Input; break;
                }

                if (parameter.Type is SQLStringType)
                {
                    sqliteParameter.DbType = DbType.String;
                    sqliteParameter.Size = ((SQLStringType)parameter.Type).Length;
                }
                else if (parameter.Type is SQLBooleanType)
                {
                    sqliteParameter.DbType = DbType.Boolean;
                }
                else if (parameter.Type is SQLByteArrayType)
                {
                    sqliteParameter.DbType = DbType.Binary;
                    sqliteParameter.Size = ((SQLByteArrayType)parameter.Type).Length;
                }
                else if (parameter.Type is SQLIntegerType)
                {
                    switch (((SQLIntegerType)parameter.Type).ByteCount)
                    {
                        case 1 : sqliteParameter.DbType = DbType.Byte; break;
                        case 2 : sqliteParameter.DbType = DbType.Int16; break;
                        case 8 : sqliteParameter.DbType = DbType.Int64; break;
                        default : sqliteParameter.DbType = DbType.Int32; break;
                    }
                }
                else if (parameter.Type is SQLNumericType)
                {
                    SQLNumericType type = (SQLNumericType)parameter.Type;
                    sqliteParameter.DbType = DbType.Decimal;
                    //LSQLiteParameter.Scale = LType.Scale;
                    //LSQLiteParameter.Precision = LType.Precision;
                }
                else if (parameter.Type is SQLFloatType)
                {
                    SQLFloatType type = (SQLFloatType)parameter.Type;
                    if (type.Width == 1)
                        sqliteParameter.DbType = DbType.Single;
                    else
                        sqliteParameter.DbType = DbType.Double;
                }
                else if (parameter.Type is SQLBinaryType)
                {
                    sqliteParameter.DbType = DbType.Binary;
                }
                else if (parameter.Type is SQLTextType)
                {
                    sqliteParameter.DbType = DbType.String;
                }
                else if (parameter.Type is SQLDateType)
                {
                    sqliteParameter.DbType = DbType.Date;
                }
                else if (parameter.Type is SQLTimeType)
                {
                    sqliteParameter.DbType = DbType.Time;
                }
                else if (parameter.Type is SQLDateTimeType)
                {
                    sqliteParameter.DbType = DbType.DateTime;
                }
                else if (parameter.Type is SQLGuidType)
                {
                    sqliteParameter.DbType = DbType.Guid;
                }
                else if (parameter.Type is SQLMoneyType)
                {
                    sqliteParameter.DbType = DbType.Currency;
                }
                else
                    throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
                _command.Parameters.Add(sqliteParameter);
            }
        }
    }
}