/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.SQLite;

using Alphora.Dataphor.DAE.Store;

namespace Alphora.Dataphor.DAE.Connection
{
	public class SQLiteConnection : DotNetConnection
	{
		// Connects after reading BusyTimeout, because opening can itself report a lock wait.
		public SQLiteConnection(string connection) : base() 
		{
			_supportsMARS = true;
			_busyTimeoutSeconds = TimeSpan.FromMilliseconds(new SQLiteConnectionStringBuilder(connection).BusyTimeout).TotalSeconds;
			InternalConnect(connection);
		}

		// For the lock-wait error's message.
		private readonly double _busyTimeoutSeconds;
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new System.Data.SQLite.SQLiteConnection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new SQLiteCommand(this, CreateDbCommand());
		}
		
		protected bool _inReadUncommittedTransaction;

		protected override void InternalBeginTransaction(SQLIsolationLevel isolationLevel)
		{
			if (isolationLevel == SQLIsolationLevel.ReadUncommitted)
				_inReadUncommittedTransaction = true;
			else
				base.InternalBeginTransaction(isolationLevel);
		}

		protected override void InternalCommitTransaction()
		{
			if (_inReadUncommittedTransaction)
				_inReadUncommittedTransaction = false;
			else
				base.InternalCommitTransaction();
		}

		protected override void InternalRollbackTransaction()
		{
			if (_inReadUncommittedTransaction)
				_inReadUncommittedTransaction = false;
			else
				base.InternalRollbackTransaction();
		}
		
		protected override bool IsTransactionFailure(Exception exception)
		{
			SQLiteException localException = exception as SQLiteException;
			if (localException != null)
				return IsTransactionFailure(localException.ResultCode);

			return false;
		}
		
		protected bool IsTransactionFailure(SQLiteErrorCode errorCode)
		{
			return false;
		}
		
		// NOTE: compares primary result codes; System.Data.SQLite 1.0.119 reports a unique violation as Constraint (19) unless extended result codes are turned on. If they ever are (SetExtendedResultCodes or a connection flag), mask with 0xFF first or Constraint_Unique and the like become Application errors.
		protected bool IsUserCorrectableError(SQLiteErrorCode errorCode)
		{
			return errorCode == SQLiteErrorCode.Constraint;
		}
		
		// A lock wait that outlasted BusyTimeout. The store is this class's only user, so a store error is the right report.
		private static bool IsLockTimeout(Exception exception)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is SQLiteException localException)
				{
					// The primary code, in case extended result codes are ever turned on (Busy_Recovery, Locked_SharedCache, ...).
					SQLiteErrorCode primaryCode = (SQLiteErrorCode)((int)localException.ResultCode & 0xFF);
					return primaryCode == SQLiteErrorCode.Busy || primaryCode == SQLiteErrorCode.Locked;
				}
			return false;
		}

		protected override Exception InternalWrapException(Exception exception, string statement)
		{
			if (IsLockTimeout(exception))
				return new StoreException(StoreException.Codes.StoreLocked, ErrorSeverity.Environment, exception, _busyTimeoutSeconds);

			ErrorSeverity severity = GetExceptionSeverity(exception);
			if (severity == ErrorSeverity.User)
				return new DataphorException(ErrorSeverity.User, DataphorException.ApplicationError, exception.Message);
			return base.InternalWrapException(exception, statement);
		}

		private ErrorSeverity GetExceptionSeverity(Exception exception)
		{
			// If the error code indicates an integrity constraint violation or other user-correctable message, severity is user, otherwise, severity is application
			SQLiteException localException = exception as SQLiteException;
			if (localException != null)
			{
				if (!IsUserCorrectableError(localException.ResultCode))
					return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
}