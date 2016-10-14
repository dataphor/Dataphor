/*
	Dataphor
	© Copyright 2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using CacheClient = InterSystems.Data.CacheClient;

namespace Alphora.Dataphor.DAE.Connection.Cache
{
	public class CacheConnection : DotNetConnection
	{
		public CacheConnection(string connection) : base(connection) { }
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			try
			{
				return new CacheClient.CacheConnection(connectionString);
			}
			catch (Exception exception)
			{
				WrapException(exception, string.Format("connect ({0})", connectionString), true);
				throw;
			}
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new CacheCommand(this, CreateDbCommand());
		}

		protected override void InternalBeginTransaction(SQLIsolationLevel isolationLevel)
		{
			_isolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (isolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : _isolationLevel = System.Data.IsolationLevel.ReadUncommitted; break;
				case SQLIsolationLevel.RepeatableRead : _isolationLevel = System.Data.IsolationLevel.RepeatableRead; break;
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

