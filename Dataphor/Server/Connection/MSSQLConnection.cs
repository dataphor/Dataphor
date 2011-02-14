/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Data;
using System.Data.SqlClient;
using System.Data.Common;

/*
	MSSQLConnection - implements client or server-side SQL Server cursors

		SQL Server Server-Side Cursor API Notes:
			-When using forward-only, beware of implicit conversion to "static" cursors (though never in MSSQL2005).  These cursors entail a copy of the data to a tempdb table.
			 (See "Implicit Cursor Conversions" in the SQL Server 2000 docs: 
			  mk:@MSITStore:C:\Program%20Files\Microsoft%20SQL%20Server\80\Tools\Books\acdata.chm::/ac_8_con_07_66sz.htm)
			-SQL Server Server Side Cursor API Documentation:
			 http://jtds.sourceforge.net/apiCursors.html#_sp_cursoropen
			-The "declare cursor" TSQL system lacks several things provided by the unsupported sp_cursor api; Specifically: 
				parameterization, auto-open, auto-fetch, and fetch next of more than one row.
			 Additionally, the declare cursor API requires that "globally named" cursors be given a unique name (rather than
			  providing a mechanism for getting a handle from the server.
			-An attempt to simulate ADO recordsets using "declare cursor" server side cursors:
			 http://www.codeproject.com/vb/net/SimulatingRecordsets.asp
			-Some in-depth detail about the SQL Server 7 query processor:
			 http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnsql7/html/sqlquerproc.asp
			-sp_cursorXXX function notes (assuming auto-fetch and auto-close):
				-If the set contains fewer rows that are requested to fetch, the result of open or fetch is 16 and the cursor is closed
				-If the set contains exactly the number requested to fetch, the result is 0 and the cursor remains open; the subsequent fetch will result in 0 rows.
*/

namespace Alphora.Dataphor.DAE.Connection
{
	public class MSSQLConnection : DotNetConnection
	{
		public MSSQLConnection(string connectionString) : base(connectionString) 
		{ 
			DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
			builder.ConnectionString = connectionString;
			if (builder.ContainsKey("MultipleActiveResultSets"))
			{
				_supportsMARS = "True" == ((string)builder["MultipleActiveResultSets"]);
			}
		}

		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}

		protected override SQLCommand InternalCreateCommand()
		{
			return new MSSQLCommand(this, CreateDbCommand());
		}

		#region Exception Wrapping

		protected override bool IsTransactionFailure(Exception exception)
		{
			SqlException localException = exception as SqlException;
			if (localException != null)
				foreach (SqlError error in localException.Errors)
					if (IsTransactionFailure(error.Number))
						return true;

			return false;
		}

		protected bool IsTransactionFailure(int errorCode)
		{
			switch (errorCode)
			{
				case 1205:
					return true; // Transaction was deadlocked
				case 1211:
					return true; // Process was chosen as deadlock victim
				case 3928:
					return true; // The marked transaction failed
				case 8650:
					return true; // Intra-query parallelism caused server command to deadlock
				case 8901:
					return true; // Deadlock detected during DBCC
			}

			return false;
		}

		protected bool IsUserCorrectableError(int errorCode)
		{
			if (errorCode >= 50000)
				return true;

			switch (errorCode)
			{
				case 3621:
					return true; // The statement has been terminated.
				case 2601:
					return true; // Cannot insert duplicate key row...
				case 2627:
					return true; // Violation of %ls constraint...
				case 10055:
					return true; // The data violated the integrity constraint...
				case 10065:
					return true; // The data violated the integrity constraint...
				case 11011:
					return true; // The data violated the integrity constraint...
				case 11012:
					return true; // The data violated the schema...
				case 11040:
					return true; // Deleting the row violated...
				case 547:
					return true; // %ls statement conflicted with...
				case 8152:
					return true; // String or binary data would be truncated.
				default:
					return false;
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
			SqlException localException = exception as SqlException;
			if (localException != null)
			{
				foreach (SqlError error in localException.Errors)
					if (!IsUserCorrectableError(error.Number))
						return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
		
		#endregion
	}
	
}

/*
	Server side cursor test case:

		use pubs

		declare @HC int
		declare @retval int
		declare @SO int
		set @SO = 24592
		declare @CO int
		set @CO = 8193
		declare @RC int
		set @RC = 4	-- adjust this
		exec @retval = sp_cursoropen @HC out, N'select * from discounts', @SO out, @CO out, @RC out
		select @retval Result, @RC TheRowCount, @HC Handle

		if @retval = 0
		begin
			exec @retval = sp_cursorfetch @HC, 2, 0, @RC
			select @retval Result, @RC TheRowCount

			if @retval = 0
				exec sp_cursorclose @HC
		end

*/