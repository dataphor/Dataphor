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
		public SQLiteConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new System.Data.SQLite.SQLiteConnection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new SQLiteCommand(this, CreateDbCommand());
		}
		
		protected override bool IsTransactionFailure(Exception AException)
		{
			SQLiteException LException = AException as SQLiteException;
			if (LException != null)
				return IsTransactionFailure(LException.ErrorCode);

			return false;
		}
		
		protected bool IsTransactionFailure(SQLiteErrorCode AErrorCode)
		{
			return false;
		}
		
		protected bool IsUserCorrectableError(SQLiteErrorCode AErrorCode)
		{
			return AErrorCode == SQLiteErrorCode.Constraint;
		}
		
		protected override Exception InternalWrapException(Exception AException, string AStatement)
		{
			ErrorSeverity LSeverity = GetExceptionSeverity(AException);
			if (LSeverity == ErrorSeverity.User)
				return new DataphorException(ErrorSeverity.User, DataphorException.CApplicationError, AException.Message);
			return base.InternalWrapException(AException, AStatement);
		}

		private ErrorSeverity GetExceptionSeverity(Exception AException)
		{
			// If the error code indicates an integrity constraint violation or other user-correctable message, severity is user, otherwise, severity is application
			SQLiteException LException = AException as SQLiteException;
			if (LException != null)
			{
				if (!IsUserCorrectableError(LException.ErrorCode))
					return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
}