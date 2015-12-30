/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.Oracle
{
	using System;
	using System.Data;
	using Oracle = System.Data.OracleClient;
	using Alphora.Dataphor.DAE.Connection;
	
	public class OracleConnection : DotNetConnection
	{
		public OracleConnection(string connection) : base(connection) {}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			try
			{
				return new Oracle.OracleConnection(connectionString);
			}
			catch (Exception exception)
			{
				WrapException(exception, "connect", true);
				throw;
			}
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new OracleCommand(this, CreateDbCommand());
		}

		protected override void InternalBeginTransaction(SQLIsolationLevel isolationLevel)
		{
			_isolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (isolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : // all three will map to committed in this Optimisitc system
				case SQLIsolationLevel.RepeatableRead : 
				case SQLIsolationLevel.ReadCommitted : _isolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
				case SQLIsolationLevel.Serializable : _isolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			_transaction = _connection.BeginTransaction(_isolationLevel);
		}

		protected override Exception InternalWrapException(Exception exception, string statement)
		{
			Oracle.OracleException oracleException = exception as Oracle.OracleException;
			if (oracleException != null)
			{
				if (oracleException.Code == 1422)
					return new Runtime.RuntimeException(Runtime.RuntimeException.Codes.InvalidRowExtractorExpression);
			}

			// Wrap all exceptions coming back with a simple Exception so that it crosses the boundary.
			return new ConnectionException(ConnectionException.Codes.SQLException, ErrorSeverity.Application, new Exception(exception.Message), statement);
		}
	}
}

