using System;
using System.Data;

namespace Alphora.Dataphor.DAE.Connection.Oracle
{
    public class OracleCommand : DotNetCommand
    {
        public OracleCommand(OracleConnection connection, IDbCommand command) : base(connection, command) 
        {
            _parameterDelimiter = ":";
        }

        protected override void PrepareParameters()
        {
            // Prepare parameters
            SQLParameter parameter;
            for (int index = 0; index < _parameterIndexes.Length; index++)
            {
                parameter = Parameters[_parameterIndexes[index]];
                System.Data.OracleClient.OracleParameter oracleParameter = (System.Data.OracleClient.OracleParameter)_command.CreateParameter();
                oracleParameter.ParameterName = String.Format(":{0}", parameter.Name);
                oracleParameter.IsNullable = true;
                switch (parameter.Direction)
                {
                    case SQLDirection.Out : oracleParameter.Direction = System.Data.ParameterDirection.Output; break;
                    case SQLDirection.InOut : oracleParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
                    case SQLDirection.Result : oracleParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
                    default : oracleParameter.Direction = System.Data.ParameterDirection.Input; break;
                }

                if (parameter.Type is SQLStringType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.VarChar;
                    oracleParameter.Size = ((SQLStringType)parameter.Type).Length;
                }
                else if (parameter.Type is SQLBooleanType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.Int32;
                }
                else if (parameter.Type is SQLIntegerType)
                {
                    switch (((SQLIntegerType)parameter.Type).ByteCount)
                    {
                        case 1 : oracleParameter.OracleType = System.Data.OracleClient.OracleType.Byte; break;
                        case 2 : oracleParameter.OracleType = System.Data.OracleClient.OracleType.Int16; break;
                        case 8 : 
                            oracleParameter.OracleType = System.Data.OracleClient.OracleType.Number; 
                            oracleParameter.Precision = 20;
                            oracleParameter.Scale = 0;
                            break;
                        default : oracleParameter.OracleType = System.Data.OracleClient.OracleType.Int32; break;
                    }
                }
                else if (parameter.Type is SQLNumericType)
                {
                    SQLNumericType type = (SQLNumericType)parameter.Type;
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.Number;
                    oracleParameter.Precision = type.Precision;
                    oracleParameter.Scale = type.Scale;
                }
                else if (parameter.Type is SQLBinaryType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.Blob;
                }
                else if (parameter.Type is SQLTextType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.Clob;
                }
                else if (parameter.Type is SQLDateTimeType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.DateTime;
                }
                else if (parameter.Type is SQLDateType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.DateTime;
                }
                else if (parameter.Type is SQLTimeType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.DateTime;
                }
                else if (parameter.Type is SQLGuidType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.Char;
                    oracleParameter.Size = 24;
                }
                else if (parameter.Type is SQLMoneyType)
                {
                    oracleParameter.OracleType = System.Data.OracleClient.OracleType.Number;
                    oracleParameter.Precision = 28;
                    oracleParameter.Scale = 8;
                }
                else
                    throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
                _command.Parameters.Add(oracleParameter);
            }
        }
    }
}