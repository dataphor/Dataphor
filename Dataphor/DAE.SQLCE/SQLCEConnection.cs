/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.SqlServerCe;

namespace Alphora.Dataphor.DAE.Connection
{
	public class SQLCEConnection : DotNetConnection
	{
		public SQLCEConnection(string connection) : base(connection) 
		{
			_supportsMARS = true;
		}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new SqlCeConnection(connectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new SQLCECommand(this, CreateDbCommand());
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
			SqlCeException localException = exception as SqlCeException;
			if (localException != null)
				foreach (SqlCeError error in localException.Errors)
					if (IsTransactionFailure(error.NativeError))
						return true;

			return false;
		}
		
		protected bool IsTransactionFailure(int errorCode)
		{
			switch (errorCode)
			{
				case 1205 : return true; // Transaction was deadlocked
				case 1211 : return true; // Process was chosen as deadlock victim
				case 3928 : return true; // The marked transaction failed
				case 8650 : return true; // Intra-query parallelism caused server command to deadlock
				case 8901 : return true; // Deadlock detected during DBCC
			}
			
			return false;
		}
		
		protected bool IsUserCorrectableError(int errorCode)
		{
			if (errorCode >= 50000)
				return true;
			
			switch (errorCode)
			{
				case 3621 : return true; // The statement has been terminated.
				case 2601 : return true; // Cannot insert duplicate key row...
				case 2627 : return true; // Violation of %ls constraint...
				case 10055 : return true; // The data violated the integrity constraint...
				case 10065 : return true; // The data violated the integrity constraint...
				case 11011 : return true; // The data violated the integrity constraint...
				case 11012 : return true; // The data violated the schema...
				case 11040 : return true; // Deleting the row violated...
				case 547 : return true; // %ls statement conflicted with...
				case 8152 : return true; // String or binary data would be truncated.
				default : return false;
			}
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
			SqlCeException localException = exception as SqlCeException;
			if (localException != null)
			{
				foreach (SqlCeError error in localException.Errors)
					if (!IsUserCorrectableError(error.NativeError))
						return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
}