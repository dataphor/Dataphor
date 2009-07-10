using System;
using System.Data;
using Npgsql;

namespace Alphora.Dataphor.DAE.Connection.PGSQL
{
    public class PostgreSQLCommand : DotNetCommand
    {
        public PostgreSQLCommand(PostgreSQLConnection AConnection, IDbCommand ACommand)
            : base(AConnection, ACommand) 
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
                var LNpgsqlParameter = (NpgsqlParameter)FCommand.CreateParameter();
                LNpgsqlParameter.ParameterName = String.Format(":{0}", LParameter.Name);
                LNpgsqlParameter.IsNullable = true;
                switch (LParameter.Direction)
                {
                    case SQLDirection.Out : LNpgsqlParameter.Direction = ParameterDirection.Output; break;
                    case SQLDirection.InOut : LNpgsqlParameter.Direction = ParameterDirection.InputOutput; break;
                    case SQLDirection.Result : LNpgsqlParameter.Direction = ParameterDirection.ReturnValue; break;
                    default : LNpgsqlParameter.Direction = ParameterDirection.Input; break;
                }

                if (LParameter.Type is SQLStringType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar;
                    LNpgsqlParameter.Size = ((SQLStringType)LParameter.Type).Length;
                }
                else if (LParameter.Type is SQLBooleanType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Boolean;
                }
                else if (LParameter.Type is SQLIntegerType)
                {
                    switch (((SQLIntegerType)LParameter.Type).ByteCount)
                    {
                        case 1: LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Char; break;
                        case 2: LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Smallint; break;
                        case 8 :
                            LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric; 
                            LNpgsqlParameter.Precision = 20;
                            LNpgsqlParameter.Scale = 0;
                            break;
                        default: LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer; break;
                    }
                }
                else if (LParameter.Type is SQLNumericType)
                {
                    var LType = (SQLNumericType)LParameter.Type;
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric;
                    LNpgsqlParameter.Precision = LType.Precision;
                    LNpgsqlParameter.Scale = LType.Scale;
                }
                else if (LParameter.Type is SQLBinaryType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea;
                }
                else if (LParameter.Type is SQLTextType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;
                }
                else if (LParameter.Type is SQLDateTimeType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Date;
                }
                else if (LParameter.Type is SQLDateType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Date;
                }
                else if (LParameter.Type is SQLTimeType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Time;
                }
                else if (LParameter.Type is SQLGuidType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid;                    
                }
                else if (LParameter.Type is SQLMoneyType)
                {
                    LNpgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Money;                    
                }
                else
                    throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
                FCommand.Parameters.Add(LNpgsqlParameter);
            }
        }
    }
}