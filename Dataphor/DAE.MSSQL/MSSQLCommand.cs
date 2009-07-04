using System;
using System.Data;
using System.Data.SqlClient;

namespace Alphora.Dataphor.DAE.Connection
{
    public class MSSQLCommand : DotNetCommand
    {
        public MSSQLCommand(MSSQLConnection AConnection, IDbCommand ACommand)
            : base(AConnection, ACommand) 
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
                SqlParameter LSQLParameter = (SqlParameter)FCommand.CreateParameter();
                LSQLParameter.ParameterName = String.Format("@{0}", LParameter.Name);
                switch (LParameter.Direction)
                {
                    case SQLDirection.Out : LSQLParameter.Direction = System.Data.ParameterDirection.Output; break;
                    case SQLDirection.InOut : LSQLParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
                    case SQLDirection.Result : LSQLParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
                    default : LSQLParameter.Direction = System.Data.ParameterDirection.Input; break;
                }

                if (LParameter.Type is SQLStringType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.NVarChar;
                    LSQLParameter.Size = ((SQLStringType)LParameter.Type).Length;
                }
                else if (LParameter.Type is SQLBooleanType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.Bit;
                }
                else if (LParameter.Type is SQLByteArrayType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.Binary;
                    LSQLParameter.Size = ((SQLByteArrayType)LParameter.Type).Length;
                }
                else if (LParameter.Type is SQLIntegerType)
                {
                    switch (((SQLIntegerType)LParameter.Type).ByteCount)
                    {
                        case 1 : LSQLParameter.SqlDbType = SqlDbType.TinyInt; break;
                        case 2 : LSQLParameter.SqlDbType = SqlDbType.SmallInt; break;
                        case 8 : LSQLParameter.SqlDbType = SqlDbType.BigInt; break;
                        default : LSQLParameter.SqlDbType = SqlDbType.Int; break;
                    }
                }
                else if (LParameter.Type is SQLNumericType)
                {
                    SQLNumericType LType = (SQLNumericType)LParameter.Type;
                    LSQLParameter.SqlDbType = SqlDbType.Decimal;
                    LSQLParameter.Scale = LType.Scale;
                    LSQLParameter.Precision = LType.Precision;
                }
                else if (LParameter.Type is SQLFloatType)
                {
                    SQLFloatType LType = (SQLFloatType)LParameter.Type;
                    if (LType.Width == 1)
                        LSQLParameter.SqlDbType = SqlDbType.Real;
                    else
                        LSQLParameter.SqlDbType = SqlDbType.Float;
                }
                else if (LParameter.Type is SQLBinaryType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.Image;
                }
                else if (LParameter.Type is SQLTextType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.NText;
                }
                else if (LParameter.Type is SQLDateType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.DateTime;
                }
                else if (LParameter.Type is SQLTimeType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.DateTime;
                }
                else if (LParameter.Type is SQLDateTimeType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.DateTime;
                }
                else if (LParameter.Type is SQLGuidType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.UniqueIdentifier;
                }
                else if (LParameter.Type is SQLMoneyType)
                {
                    LSQLParameter.SqlDbType = SqlDbType.Money;
                }
                else
                    throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
                FCommand.Parameters.Add(LSQLParameter);
            }
        }
    }
}