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
                case SQLIsolationLevel.ReadCommitted: _isolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
                case SQLIsolationLevel.Serializable: _isolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			_transaction = _connection.BeginTransaction(_isolationLevel);
		}

		protected override Exception InternalWrapException(Exception exception, string statement)
		{
			// Wrap all exceptions coming back with a simple Exception so that it crosses the boundary.
			return new ConnectionException(ConnectionException.Codes.SQLException, ErrorSeverity.Application, new Exception(exception.Message), statement);
		}
	}
}

