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
		public MSSQLConnection(string AConnectionString) : base(AConnectionString) { }

		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			return new SqlConnection(AConnectionString);
		}

		public override SQLCommand CreateCommand()
		{
			return new MSSQLCommand(this, CreateDbCommand());
		}

		#region Exception Wrapping

		protected override bool IsTransactionFailure(Exception AException)
		{
			SqlException LException = AException as SqlException;
			if (LException != null)
				foreach (SqlError LError in LException.Errors)
					if (IsTransactionFailure(LError.Number))
						return true;

			return false;
		}

		protected bool IsTransactionFailure(int AErrorCode)
		{
			switch (AErrorCode)
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

		protected bool IsUserCorrectableError(int AErrorCode)
		{
			if (AErrorCode >= 50000)
				return true;

			switch (AErrorCode)
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
			SqlException LException = AException as SqlException;
			if (LException != null)
			{
				foreach (SqlError LError in LException.Errors)
					if (!IsUserCorrectableError(LError.Number))
						return ErrorSeverity.Application;
				return ErrorSeverity.User;
			}
			return ErrorSeverity.Application;
		}
		
		#endregion
	}
	
	public class MSSQLCommand : DotNetCommand
	{
		public const int CDefaultFetchCount = 20;

		protected internal MSSQLCommand(DotNetConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) { }

		private int FFetchCount = CDefaultFetchCount;
		/// <summary> Represents the number of rows to fetch at a time when using server-side cursors. </summary>
		/// <remarks> The default is 20. </remarks>
		public int FetchCount
		{
			get { return FFetchCount; }
			set { FFetchCount = value; }
		}

		#region Parameters

		/// <summary> Creates non-internal parameters on the provider. </summary>
		protected override void PrepareParameters()
		{
			// Prepare parameters
			for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
			{
				SQLParameter LParameter = Parameters[FParameterIndexes[LIndex]];
				SqlParameter LMSSQLParameter = (SqlParameter)FCommand.CreateParameter();
				LMSSQLParameter.ParameterName = String.Format("@{0}", ConvertParameterName(LParameter.Name));
				switch (LParameter.Direction)
				{
					case SQLDirection.Out:
						LMSSQLParameter.Direction = System.Data.ParameterDirection.Output;
						break;
					case SQLDirection.InOut:
						LMSSQLParameter.Direction = System.Data.ParameterDirection.InputOutput;
						break;
					case SQLDirection.Result:
						LMSSQLParameter.Direction = System.Data.ParameterDirection.ReturnValue;
						break;
					default:
						LMSSQLParameter.Direction = System.Data.ParameterDirection.Input;
						break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.VarChar;
					LMSSQLParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.Bit;
				}
				else if (LParameter.Type is SQLByteArrayType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.Binary;
					LMSSQLParameter.Size = ((SQLByteArrayType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1:
							LMSSQLParameter.SqlDbType = SqlDbType.TinyInt;
							break;
						case 2:
							LMSSQLParameter.SqlDbType = SqlDbType.SmallInt;
							break;
						case 8:
							LMSSQLParameter.SqlDbType = SqlDbType.BigInt;
							break;
						default:
							LMSSQLParameter.SqlDbType = SqlDbType.Int;
							break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LMSSQLParameter.SqlDbType = SqlDbType.Decimal;
					LMSSQLParameter.Scale = LType.Scale;
					LMSSQLParameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLFloatType)
				{
					SQLFloatType LType = (SQLFloatType)LParameter.Type;
					if (LType.Width == 1)
						LMSSQLParameter.SqlDbType = SqlDbType.Real;
					else
						LMSSQLParameter.SqlDbType = SqlDbType.Float;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.Image;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.Text;
				}
				else if (LParameter.Type is SQLDateType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.UniqueIdentifier;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LMSSQLParameter.SqlDbType = SqlDbType.Money;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LMSSQLParameter);
			}
		}

		/// <remarks> If the parameter is named "Pnnn" there may be a conflict with the provider's automatic parameter scheme, so escape (with an extra P) under those circumstances. </remarks>
		protected override string ConvertParameterName(string AParameterName)
		{
			if (AParameterName.StartsWith("P", StringComparison.OrdinalIgnoreCase) && (AParameterName.Length > 1) && Char.IsDigit(AParameterName[1]))
				return "P" + AParameterName;
			else
				return AParameterName;
		}

		/// <summary> The number of internal parameters so that we can "hide" the internal parameters. </summary>
		private int FInternalParameterCount;

		/// <summary> Accesses a given non-internal parameter. </summary>
		/// <remarks> This method hides the presence of the internal parameters so that generic command routines can deal with parameters ordinally starting from 0.
		/// This method should be used rather than access the provider's parameters directly by index. </remarks>
		protected override IDataParameter ParameterByIndex(int AIndex)
		{
			// Internal Parameters are kept before regular parameters (because sp_cursoropen requires this)
			return base.ParameterByIndex(AIndex + FInternalParameterCount);
		}

		/// <summary> Creates an internal parameter. </summary>
		/// <remarks> Internal parameters are only part of the provider's parameter space.  They are used as part of the sp_cursor calls. </remarks>
		internal SqlParameter InternalParameterCreate(string AName, SqlDbType AType, ParameterDirection ADirection)
		{
			SqlParameter LParameter = (SqlParameter)FCommand.CreateParameter();
			LParameter.ParameterName = AName;
			LParameter.SqlDbType = AType;
			LParameter.Direction = ADirection;
			FCommand.Parameters.Add(LParameter);
			FInternalParameterCount++;
			return LParameter;
		}

		/// <summary> Clears all internal and non-internal parameters. </summary>
		protected override void ClearParameters()
		{
			base.ClearParameters();
			FInternalParameterCount = 0;
		}

		/// <summary> Determines whether parameters are being used and that there are any non-internal parameters. </summary>
		protected bool ParametersActive()
		{
			return (FParameterIndexes.Length > 0) && UseParameters;
		}

		#endregion

		#region Server Cursor Open preparation

		/// <summary> Prepares for invocation of a server-side cursor. </summary>
		private void PrepareCursorStatement(string AStatement, SQLCursorType ACursorType, SQLIsolationLevel AIsolationLevel)
		{
			// Cannot use the provider's command type behavior because we are always enclosing the expression in sp_cursor calls
			if (CommandType == SQLCommandType.Table)
				AStatement = "select * from " + AStatement;

			InternalParameterCreate("@retval", SqlDbType.Int, ParameterDirection.ReturnValue);
			InternalParameterCreate("@cursor", SqlDbType.Int, ParameterDirection.Output).Value = 0;
			InternalParameterCreate("@stmt", SqlDbType.NVarChar, ParameterDirection.Input).Value = AStatement;
			InternalParameterCreate("@scrollopt", SqlDbType.Int, ParameterDirection.Input).Value = GetScrollOptions(ACursorType, AIsolationLevel);
			InternalParameterCreate("@ccopt", SqlDbType.Int, ParameterDirection.Input).Value = GetConcurrencyOptions();
			InternalParameterCreate("@rowcount", SqlDbType.Int, ParameterDirection.InputOutput).Value = FetchCount;	// This must be an output parameter or an error will occur

			// Create the parameter definition parameter here (before setting its value) to ensure that it comes in the right order (otherwise sp_cursoropen complains)
			if (ParametersActive())
				InternalParameterCreate("@paramdef", SqlDbType.NVarChar, ParameterDirection.Input).Value = "";

			FCommand.CommandText = "sp_cursoropen";
		}

		/// <summary> Gets the SQL Server server side cursor scroll options. </summary>
		private SQLServerCursorScrollOptions GetScrollOptions(SQLCursorType ACursorType, SQLIsolationLevel AIsolationLevel)
		{
			SQLServerCursorScrollOptions LResults = SQLServerCursorScrollOptions.AutoFetch | SQLServerCursorScrollOptions.AutoClose;

			if (ParametersActive())
				LResults |= SQLServerCursorScrollOptions.Parameterized;

			if (LockType == SQLLockType.ReadOnly)
				LResults |= SQLServerCursorScrollOptions.FastForward;
			else
				LResults |= SQLServerCursorScrollOptions.ForwardOnly;

			/*
			Setting these is not agreeable to the server
			if (ACursorType == SQLCursorType.Dynamic)
				LResults |= SQLServerCursorScrollOptions.Dynamic;
			else
				LResults |= SQLServerCursorScrollOptions.Static;
			*/
			// TODO: Set additional "acceptible" options 

			return LResults;
		}

		/// <summary> Gets the SQL Server server side cursor concurrency options.</summary>
		private SQLServerCursorConcurrencyOptions GetConcurrencyOptions()
		{
			SQLServerCursorConcurrencyOptions LResult = SQLServerCursorConcurrencyOptions.OpenOnAnySQL;

			switch (LockType)
			{
				case SQLLockType.ReadOnly: LResult |= SQLServerCursorConcurrencyOptions.ReadOnly; break;
				case SQLLockType.Pessimistic: LResult |= SQLServerCursorConcurrencyOptions.ScrollLocks; break;
				case SQLLockType.Optimistic: LResult |= SQLServerCursorConcurrencyOptions.OptimisticValues; break;
			}

			return LResult;
		}

		/// <summary> Sets the parameter specifying the names and types of the non-internal parameters. </summary>
		private void SetParameterDefinition()
		{
			StringBuilder LResult = new StringBuilder();
			for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
			{
				SQLParameter LParameter = Parameters[FParameterIndexes[LIndex]];
				if (LParameter.Direction != SQLDirection.Result)
				{
					if (LResult.Length > 0)
						LResult.Append(", ");

					LResult.Append("@" + ConvertParameterName(LParameter.Name) + " " + GetParamTypeDescriptor(LParameter));
					if (LParameter.Direction != SQLDirection.In)
						LResult.Append(" out");
				}
			}
			((SqlParameter)FCommand.Parameters["@paramdef"]).Value = LResult.ToString();
		}

		/// <summary> Generates a SQL Server type descriptor given a parameter definition. </summary>
		private static string GetParamTypeDescriptor(SQLParameter AParameter)
		{
			if (AParameter.Type is SQLStringType)
				return "varchar(" + ((SQLStringType)AParameter.Type).Length.ToString() + ")";
			else if (AParameter.Type is SQLBooleanType)
				return "bit";
			else if (AParameter.Type is SQLByteArrayType)
				return "binary(" + ((SQLByteArrayType)AParameter.Type).Length.ToString() + ")";
			else if (AParameter.Type is SQLIntegerType)
			{
				switch (((SQLIntegerType)AParameter.Type).ByteCount)
				{
					case 1:
						return "tinyint";
					case 2:
						return "smallint";
					case 8:
						return "bigint";
					default:
						return "int";
				}
			}
			else if (AParameter.Type is SQLNumericType)
			{
				SQLNumericType LType = (SQLNumericType)AParameter.Type;
				return "decimal(" + LType.Precision.ToString() + "," + LType.Scale.ToString() + ")";
			}
			else if (AParameter.Type is SQLFloatType)
			{
				SQLFloatType LType = (SQLFloatType)AParameter.Type;
				if (LType.Width == 1)
					return "real";
				else
					return "float";
			}
			else if (AParameter.Type is SQLBinaryType)
				return "image";
			else if (AParameter.Type is SQLTextType)
				return "text";		// Q: Should this be NText or text
			else if ((AParameter.Type is SQLDateType) || (AParameter.Type is SQLTimeType) || (AParameter.Type is SQLDateTimeType))
				return "datetime";
			else if (AParameter.Type is SQLGuidType)
				return "uniqueidentifier";
			else if (AParameter.Type is SQLMoneyType)
				return "money";
			else
				throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, AParameter.Type.GetType().Name);
		}

		#endregion

		#region Open and prepare

		protected override void InternalPrepare()
		{
			// There seems to be no advantage to preparing
		}

		/// <summary> Prepares for a server cursor open. </summary>
		protected void PrepareServerCursorCommand(SQLCursorType ACursorType, SQLIsolationLevel AIsolationLevel)
		{
			// All commands will be stored procedures when using server-side cursors
			FCommand.CommandType = System.Data.CommandType.StoredProcedure;	// always use stored procedure because queries are wrapped in a server-side cursor stored procedure call

			// Call PrepareStatement first to determine the set of non-internal parameters and normalize the statement
			string LStatement = PrepareStatement(Statement);

			// Call PrepareCursorStatement before creating the non-internal parameters because sp_cursoropen is fickle about the order of the arguments (even though they are passed by name)
			PrepareCursorStatement(LStatement, ACursorType, AIsolationLevel);
			
			if (ParametersActive())
			{
				// Prepare non-internal parameters
				PrepareParameters();

				// One of the already created internal parameters is a list of the non-internal parameters; set it now that we have the non-internal parameters configured.
				SetParameterDefinition();

				//FCommand.Prepare();	// Do not prepare against SQL Server.  It seems that it is perhaps never faster to do so.
			}
		}

		protected override SQLCursor InternalOpen(SQLCursorType ACursorType, SQLIsolationLevel AIsolationLevel)
		{
			// Prepare the connection
			SQLCursorLocation LLocation = GetCursorLocation();
			if (LLocation == SQLCursorLocation.Server)
				PrepareServerCursorCommand(ACursorType, AIsolationLevel);
			else
				PrepareCommand(false, AIsolationLevel);

			// Set the non-internal parameters
			SetParameters();

			// Open the reader
			IDataReader LCursor = FCommand.ExecuteReader(SQLCommandBehaviorToCommandBehavior(CommandBehavior));

			// Return the appropriate cursor
			if (LLocation == SQLCursorLocation.Server)
				return new MSSQLServerCursor(this, LCursor);
			else
				return new DotNetCursor(this, LCursor);
		}

		#endregion

		#region Cursor Location

		/// <summary> Returns either server or client side cursor. </summary>
		private SQLCursorLocation GetCursorLocation()
		{
			if (CursorLocation == SQLCursorLocation.Automatic)
				return DetectCursorLocation();
			else
				return CursorLocation;
		}

		/// <summary> When the cursor location is set to Automatic, determine whether it should actually be server or client. </summary>
		/// <remarks> If the command type is table or if the statement begin with select, a server-side cursor is used. </remarks>
		private SQLCursorLocation DetectCursorLocation()
		{
			return
			(
				(CommandType == SQLCommandType.Table)
					||
					(
						Statement.TrimStart
						(
							new char[] 
							{ 
								'\t', '\n', '\v', '\f', '\r', ' ', '\x0085', '\x00a0', '\u1680', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', 
								'\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200a', '\u200b', '\u2028', '\u2029', '\u3000', '\ufeff' 
							}
						).ToLowerInvariant().StartsWith("select")
					)
				? SQLCursorLocation.Server
				: SQLCursorLocation.Client
			);
		}

		#endregion

		#region Helpers

		internal void CloseServerCursor(int ACursorHandle)
		{
			ClearParameters();
			InternalParameterCreate("@cursor", SqlDbType.Int, ParameterDirection.Input).Value = ACursorHandle;
			FCommand.CommandText = "sp_cursorclose";
			FCommand.ExecuteNonQuery();
		}

		internal IDataReader FetchServerCursor(int ACursorHandle)
		{
			ClearParameters();
			InternalParameterCreate("@retval", SqlDbType.Int, ParameterDirection.ReturnValue);
			InternalParameterCreate("@cursor", SqlDbType.Int, ParameterDirection.Input).Value = ACursorHandle;
			InternalParameterCreate("@fetchtype", SqlDbType.Int, ParameterDirection.Input).Value = 0x0002 /* next */;
			InternalParameterCreate("@rownum", SqlDbType.Int, ParameterDirection.Input).Value = 0;
			InternalParameterCreate("@nrows", SqlDbType.Int, ParameterDirection.Input).Value = FetchCount;
			FCommand.CommandText = "sp_cursorfetch";
			return FCommand.ExecuteReader(SQLCommandBehaviorToCommandBehavior(CommandBehavior));
		}

		internal object GetParameterValue(string AParameterName)
		{
			return ((SqlParameter)FCommand.Parameters[AParameterName]).Value;
		}

		internal void ParametersAreAvailable()
		{
			GetParameters();
		}

		#endregion
	}
	
	public class MSSQLServerCursor : DotNetCursor
	{
		public MSSQLServerCursor(MSSQLCommand ACommand, IDataReader ACursor) : base(ACommand, ACursor) { }

		protected override void Dispose(bool ADisposing)
		{
			if ((FCursor != null) && (FCursorHandle != 0))
			{
				// If most recent open or fetch indicates EOF, then the server cursor is already closed.  In order to determine this, 
				//  however, we must get the result param which can only be obtained after closing the current reader.  Additionally,
				//  if the current reader is from the open, we must fetch the server cursor handle so that we have a handle to close.
				CloseAndProcessFetchResult();

				if (!FEOF)
				{
					// The most recent open or fetch did not indicate EOF, so the server cursor remains open.  Use "our" connection 
					//  to close the server cursor now that the latest reader is closed.
					((MSSQLCommand)FCommand).CloseServerCursor(FCursorHandle);
					FCursorHandle = 0;
				}
			}

			base.Dispose(ADisposing);
		}

		private int FCursorHandle;
		private bool FEOF;	// Indicates that the end of the current fetch cursor is know to be the end of the entire dataset

		protected override bool InternalNext()
		{
			bool LGotRow = FCursor.Read();
			if (!LGotRow)
			{
				// Either we are at the end of the data set or we are just at the end of the current fetch buffer.  We must close the
				//  current reader so that we can get the output params and determine the server cursor handle and the EOF flag.
				CloseAndProcessFetchResult();

				// The EOF flag has been updated, so we can tell whether or not to proceed with a fetch
				if (!FEOF)
				{
					FCursor = ((MSSQLCommand)FCommand).FetchServerCursor(FCursorHandle);
					FRecord = (IDataRecord)FCursor;

					// Attempt to read the first row
					LGotRow = FCursor.Read();
				}
			}
			return LGotRow;
		}

		/// <summary> Closes the current reader and examines the EOF and server cursor handle output parameters. </summary>
		private void CloseAndProcessFetchResult()
		{
			FCursor.Dispose();
			FCursor = null;

			switch ((int)((MSSQLCommand)FCommand).GetParameterValue("@retval"))
			{
				case 16: FEOF = true; break;
				case 0: FEOF = false; break;
				default: throw new ConnectionException(ConnectionException.Codes.UnableToOpenCursor);
			}

			if (FCursorHandle == 0)
			{
				MSSQLCommand LCommand = (MSSQLCommand)FCommand;
				LCommand.ParametersAreAvailable();	// Indicate to the command that now is the time to read any output (user) parameters
				FCursorHandle = (int)LCommand.GetParameterValue("@cursor");
			}
		}

/*
		TODO: Implement cursor updates?

		protected override void InternalInsert(string[] ANames, object[] AValues)
		{
		}

		protected override void InternalUpdate(string[] ANames, object[] AValues)
		{
		}

		protected override void InternalDelete()
		{
		}
*/
	}

	public enum SQLServerCursorScrollOptions : int
	{
		Keyset = 0x0001,
		Dynamic = 0x0002,
		ForwardOnly = 0x0004,
		Static = 0x0008,
		FastForward = 0x0010,
		Parameterized = 0x1000,
		AutoFetch = 0x2000,
		AutoClose = 0x4000,
		CheckAcceptable = 0x8000,
		KeysetAcceptable = 0x10000,
		DynamicAcceptable = 0x20000,
		ForwardOnlyAcceptable = 0x40000,
		StaticAcceptable = 0x80000,
		FastForwardAcceptable = 0x100000
	}

	public enum SQLServerCursorConcurrencyOptions : int
	{
		ReadOnly = 0x0001,
		ScrollLocks = 0x0002,
		OptimisticTimeStamps = 0x0004,
		OptimisticValues = 0x0008,
		OpenOnAnySQL = 0x2000,
		OpenKeysetInPlace = 0x4000,
		ReadOnlyAcceptable = 0x10000,
		LocksAcceptable = 0x20000,
		OptimisticAcceptable = 0x40000
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