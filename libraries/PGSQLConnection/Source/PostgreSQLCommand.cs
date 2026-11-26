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

        /// <remarks>
        /// Three things the current driver does that the Npgsql 2.0.4 this was written against did not, each read off the
        /// pinned version (10.0.4):
        ///
        /// Size is acted on. NpgsqlParameter truncates a string, char array or byte array longer than Size for a text or
        /// bytea parameter, where 2.0.4 only recorded the number. Nothing is truncated here: the truncation is skipped
        /// while Size is 0, and DotNetCommand.SetParameters raises Size to the value's own length before each execution.
        ///
        /// Precision and Scale are still present and not obsolete, but the driver stores them and never reads them. A
        /// numeric parameter is sent with the value's own precision and scale, so the two set below say what is intended
        /// and decide nothing.
        ///
        /// Timestamp is timestamp without time zone, which refuses a DateTime whose Kind is Utc (timestamptz is the one
        /// that demands Utc). That is the right target for D4's DateTime, which carries no time zone, and no engine value
        /// can trip the check: DateTimeConveyor rebuilds every DateTime from its ticks, so Kind is Unspecified, and the one
        /// place that does not (DateTime.Today, behind the Date and ActualDate operators) gives Local, which it accepts.
        /// </remarks>
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
                    // NOTE: the current driver accepts only TimeOnly, TimeSpan and long for a time parameter, where 2.0.4 took
                    // a DateTime - and PostgreSQLTime.FromScalar, in PGSQLDevice, hands one over, so a D4 Time parameter is
                    // refused when the driver binds it. Not changed here: what to send is the device scalar type's decision, and
                    // device-pgsql-port is the first ticket that runs a device against a server and sees it.
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