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
		
		// NOTE: the current driver rejects a connection-string keyword it does not know, where 2.0.4 ignored one, and it
		// spells the schema search path "Search Path" with a space. PostgreSQLDeviceSession builds a string carrying
		// "SearchPath=", so a device connection fails here on the keyword before it ever reaches a server. The mapping is
		// PostgreSQLConnectionStringBuilder's, in PGSQLDevice; device-pgsql-device-golden is the first ticket to create a
		// device and device-pgsql-port the first to connect one.
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
                case SQLIsolationLevel.ReadCommitted: _isolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
                case SQLIsolationLevel.Serializable: _isolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			_transaction = _connection.BeginTransaction(_isolationLevel);
		}

		protected override Exception InternalWrapException(Exception exception, string statement)
		{
			// NOTE: flattening loses PostgresException.SqlState, which is the only thing that tells a constraint violation
			// from a connection failure - the distinction MSSQLConnection.IsUserCorrectableError makes. Left as 3.1 had it:
			// the reason below predates the current serialization rules, so the flattening may not be needed at all, and
			// nothing can see the difference until a device runs. device-pgsql-port is the first ticket that would.
			// Wrap all exceptions coming back with a simple Exception so that it crosses the boundary.
			return new ConnectionException(ConnectionException.Codes.SQLException, ErrorSeverity.Application, new Exception(exception.Message), statement);
		}
	}
}

