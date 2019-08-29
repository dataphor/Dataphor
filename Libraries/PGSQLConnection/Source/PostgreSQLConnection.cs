/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using Npgsql;

namespace Alphora.Dataphor.DAE.Connection.PGSQL
{

	
	public class PostgreSQLConnection : DotNetConnection
	{
        public PostgreSQLConnection(string connection) : base(connection) { }

        protected override IDbConnection CreateDbConnection(string connectionString)
        {
            try
            {
                return new NpgsqlConnection(connectionString);
            }
            catch (Exception exception)
            {
                WrapException(exception, string.Format("connect ({0})", connectionString), true);
                throw;
            }
        }
		
		protected override SQLCommand InternalCreateCommand()
		{
            return new PostgreSQLCommand(this, CreateDbCommand());
		}

		protected override void InternalBeginTransaction(SQLIsolationLevel isolationLevel)
		{
            _isolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (isolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : // all three will map to committed in this Optimisitc system
				case SQLIsolationLevel.RepeatableRead :
                case SQLIsolationLevel.ReadCommitted: // _isolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
                case SQLIsolationLevel.Serializable: _isolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			_transaction = _connection.BeginTransaction(_isolationLevel);
		}

        protected override Exception InternalWrapException(Exception exception, string statement)
		{
            ErrorSeverity severity = GetExceptionSeverity(exception);
            if (severity == ErrorSeverity.User)
                return new DataphorException(ErrorSeverity.User, DataphorException.ApplicationError, exception.Message);

            return base.InternalWrapException(exception, statement);
		}

        protected override bool IsTransactionFailure(Exception exception)
        {
            NpgsqlException localException = exception as NpgsqlException;
            if (localException != null)
            {
                foreach (NpgsqlError error in localException.Errors)
                {
                    if (IsTransactionError(error.Code))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsTransactionError(string errorCode)
        {
            return !IsUserCorrectableError(errorCode);
        }

        private bool IsUserCorrectableError(string errorCode)
        {
            string classCode = errorCode.Substring(0, 2);

            switch (classCode)
            {
                // Warnings
                case "01":
                case "02":
                // Statement not yet complete
                case "03":
                // Data exception
                case "22":
                // Integrity constraint
                case "23": return true;
                // Transaction state
                case "25": return false;
                default: return false;
            }
        }

        private ErrorSeverity GetExceptionSeverity(Exception exception)
        {
            // If the error code indicates an integrity constraint violation or other user-correctable message, severity is user, otherwise, severity is application
            NpgsqlException localException = exception as NpgsqlException;
            if (localException != null)
            {
                foreach (NpgsqlError error in localException.Errors)
                {
                    if (!IsUserCorrectableError(error.Code))
                    {
                        return ErrorSeverity.Application;
                    }
                }

                return ErrorSeverity.User;
            }

            return ErrorSeverity.Application;
        }
    }
}

