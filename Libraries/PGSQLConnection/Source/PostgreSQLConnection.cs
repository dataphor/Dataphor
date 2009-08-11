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
        public PostgreSQLConnection(string AConnection) : base(AConnection) { }
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			try
			{
                return new NpgsqlConnection(AConnectionString);
			}
			catch (Exception LException)
			{
				WrapException(LException, "connect", true);
				throw;
			}
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
            return new PostgreSQLCommand(this, CreateDbCommand());
		}

		protected override void InternalBeginTransaction(SQLIsolationLevel AIsolationLevel)
		{
            FIsolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (AIsolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : // all three will map to committed in this Optimisitc system
				case SQLIsolationLevel.RepeatableRead :
                case SQLIsolationLevel.ReadCommitted: FIsolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
                case SQLIsolationLevel.Serializable: FIsolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			FTransaction = FConnection.BeginTransaction(FIsolationLevel);
		}

		protected override Exception InternalWrapException(Exception AException, string AStatement)
		{
			// Wrap all exceptions coming back with a simple Exception so that it crosses the boundary.
			return new ConnectionException(ConnectionException.Codes.SQLException, ErrorSeverity.Application, new Exception(AException.Message), AStatement);
		}
	}
}

