/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Data;

namespace Alphora.Dataphor.DAE.Connection
{
	public abstract class DotNetConnection : SQLConnection
	{
		/// <summary>
		/// Parameterless constructor used by descendents to establish state prior to establishing the connection.
		/// </summary>
		protected DotNetConnection() : base()
		{ }
		
		public DotNetConnection(string connectionString) : base()
		{
			InternalConnect(connectionString);
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_connection != null)
				{
					try
					{
						_connection.Dispose();
					}
					finally
					{
						SetState(SQLConnectionState.Closed);
						_connection = null;
					}
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		protected void InternalConnect(string connectionString)
		{
			_connection = CreateDbConnection(connectionString);
			try
			{
				_connection.Open();
			}
			catch (Exception exception)
			{
				WrapException(exception, "connect", false);
			}
			SetState(SQLConnectionState.Idle);
		}
		
		protected abstract IDbConnection CreateDbConnection(string connectionString);
		
		protected IDbCommand CreateDbCommand()
		{
			IDbCommand command = _connection.CreateCommand();
			if (_transaction != null)
				command.Transaction = _transaction;
			return command;
		}
		
		protected IDbConnection _connection;
		protected IDbTransaction _transaction;
		protected System.Data.IsolationLevel _isolationLevel;
		
		protected override void InternalBeginTransaction(SQLIsolationLevel isolationLevel)
		{
			_isolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (isolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : _isolationLevel = System.Data.IsolationLevel.ReadUncommitted; break;
				case SQLIsolationLevel.ReadCommitted : _isolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
				case SQLIsolationLevel.RepeatableRead : _isolationLevel = System.Data.IsolationLevel.RepeatableRead; break;
				case SQLIsolationLevel.Serializable : _isolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			_transaction = _connection.BeginTransaction(_isolationLevel);
		}

		protected override void InternalCommitTransaction()
		{
			_transaction.Commit();
			_transaction = null;
		}

		protected override void InternalRollbackTransaction()
		{
			try
			{
				_transaction.Rollback();
			}
			finally
			{
				_transaction = null;			
			}
		}
		
		public override bool IsConnectionValid()
		{
			try
			{
				return (_connection != null) && (_connection.State != ConnectionState.Closed);
			}
			catch 
			{
				return false;
			}
		}
	}
}

