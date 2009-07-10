using System;
using System.Data;

namespace Alphora.Dataphor.DAE.Connection.Oracle
{
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
                System.Data.OracleClient.OracleParameter LOracleParameter = (System.Data.OracleClient.OracleParameter)FCommand.CreateParameter();
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
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.VarChar;
                    LOracleParameter.Size = ((SQLStringType)LParameter.Type).Length;
                }
                else if (LParameter.Type is SQLBooleanType)
                {
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Int32;
                }
                else if (LParameter.Type is SQLIntegerType)
                {
                    switch (((SQLIntegerType)LParameter.Type).ByteCount)
                    {
                        case 1 : LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Byte; break;
                        case 2 : LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Int16; break;
                        case 8 : 
                            LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Number; 
                            LOracleParameter.Precision = 20;
                            LOracleParameter.Scale = 0;
                            break;
                        default : LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Int32; break;
                    }
                }
                else if (LParameter.Type is SQLNumericType)
                {
                    SQLNumericType LType = (SQLNumericType)LParameter.Type;
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Number;
                    LOracleParameter.Precision = LType.Precision;
                    LOracleParameter.Scale = LType.Scale;
                }
                else if (LParameter.Type is SQLBinaryType)
                {
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Blob;
                }
                else if (LParameter.Type is SQLTextType)
                {
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Clob;
                }
                else if (LParameter.Type is SQLDateTimeType)
                {
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.DateTime;
                }
                else if (LParameter.Type is SQLDateType)
                {
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.DateTime;
                }
                else if (LParameter.Type is SQLTimeType)
                {
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.DateTime;
                }
                else if (LParameter.Type is SQLGuidType)
                {
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Char;
                    LOracleParameter.Size = 24;
                }
                else if (LParameter.Type is SQLMoneyType)
                {
                    LOracleParameter.OracleType = System.Data.OracleClient.OracleType.Number;
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