/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.SQLite;

namespace Alphora.Dataphor.DAE.Connection
{
	public class SQLiteConnection : DotNetConnection
	{
		public SQLiteConnection(string connection) : base(connection) 
		{
			_supportsMARS = true;
		}
		
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
				return IsTransactionFailure(localException.ErrorCode);

			return false;
		}
		
		protected bool IsTransactionFailure(SQLiteErrorCode errorCode)
		{
			return false;
		}
		
		protected bool IsUserCorrectableError(SQLiteErrorCode errorCode)
		{
			return errorCode == SQLiteErrorCode.Constraint;
		}
		
		protected override Exception InternalWrapException(Exception exception, string statement)
		{
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
				if (!IsUserCorrectableError(localException.ErrorCode))
					return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
}