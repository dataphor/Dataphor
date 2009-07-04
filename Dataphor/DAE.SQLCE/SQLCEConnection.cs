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
		public SQLCEConnection(string AConnection) : base(AConnection) {}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new SqlCeConnection(AConnectionString);
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new SQLCECommand(this, CreateDbCommand());
		}
		
		protected override bool IsTransactionFailure(Exception AException)
		{
			SqlCeException LException = AException as SqlCeException;
			if (LException != null)
				foreach (SqlCeError LError in LException.Errors)
					if (IsTransactionFailure(LError.NativeError))
						return true;

			return false;
		}
		
		protected bool IsTransactionFailure(int AErrorCode)
		{
			switch (AErrorCode)
			{
				case 1205 : return true; // Transaction was deadlocked
				case 1211 : return true; // Process was chosen as deadlock victim
				case 3928 : return true; // The marked transaction failed
				case 8650 : return true; // Intra-query parallelism caused server command to deadlock
				case 8901 : return true; // Deadlock detected during DBCC
			}
			
			return false;
		}
		
		protected bool IsUserCorrectableError(int AErrorCode)
		{
			if (AErrorCode >= 50000)
				return true;
			
			switch (AErrorCode)
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
			SqlCeException LException = AException as SqlCeException;
			if (LException != null)
			{
				foreach (SqlCeError LError in LException.Errors)
					if (!IsUserCorrectableError(LError.NativeError))
						return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
	}
}