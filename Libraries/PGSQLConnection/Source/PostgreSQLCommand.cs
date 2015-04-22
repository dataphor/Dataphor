using System;
using System.Data;
using Npgsql;

namespace Alphora.Dataphor.DAE.Connection.PGSQL
{
    public class PostgreSQLCommand : DotNetCommand
    {
        public PostgreSQLCommand(PostgreSQLConnection connection, IDbCommand command)
            : base(connection, command) 
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
                var npgsqlParameter = (NpgsqlParameter)_command.CreateParameter();
                npgsqlParameter.ParameterName = String.Format(":{0}", parameter.Name);
                npgsqlParameter.IsNullable = true;
                switch (parameter.Direction)
                {
                    case SQLDirection.Out : npgsqlParameter.Direction = ParameterDirection.Output; break;
                    case SQLDirection.InOut : npgsqlParameter.Direction = ParameterDirection.InputOutput; break;
                    case SQLDirection.Result : npgsqlParameter.Direction = ParameterDirection.ReturnValue; break;
                    default : npgsqlParameter.Direction = ParameterDirection.Input; break;
                }

                if (parameter.Type is SQLStringType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar;
                    npgsqlParameter.Size = ((SQLStringType)parameter.Type).Length;
                }
                else if (parameter.Type is SQLBooleanType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Boolean;
                }
                else if (parameter.Type is SQLIntegerType)
                {
                    switch (((SQLIntegerType)parameter.Type).ByteCount)
                    {
                        case 1: npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Smallint; break;
                        case 2: npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Smallint; break;
                        case 8 :
                            npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric; 
                            npgsqlParameter.Precision = 20;
                            npgsqlParameter.Scale = 0;
                            break;
                        default: npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer; break;
                    }
                }
                else if (parameter.Type is SQLNumericType)
                {
                    var type = (SQLNumericType)parameter.Type;
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric;
                    npgsqlParameter.Precision = type.Precision;
                    npgsqlParameter.Scale = type.Scale;
                }
                else if (parameter.Type is SQLBinaryType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea;
                }
                else if (parameter.Type is SQLTextType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;
                }
                else if (parameter.Type is SQLDateTimeType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Timestamp;
                }
                else if (parameter.Type is SQLDateType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Date;
                }
                else if (parameter.Type is SQLTimeType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Time;
                }
                else if (parameter.Type is SQLGuidType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid;                    
                }
                else if (parameter.Type is SQLMoneyType)
                {
                    npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Money;                    
                }
                else
                    throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
                _command.Parameters.Add(npgsqlParameter);
            }
        }
    }
}