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
using Microsoft.Data.SqlClient;

namespace Alphora.Dataphor.DAE.Connection
{
	public class MSSQLCommand : DotNetCommand
	{
		public const int DefaultFetchCount = 20;

		protected internal MSSQLCommand(DotNetConnection connection, IDbCommand command) : base(connection, command) { }

		private int _fetchCount = DefaultFetchCount;
		/// <summary> Represents the number of rows to fetch at a time when using server-side cursors. </summary>
		/// <remarks> The default is 20. </remarks>
		public int FetchCount
		{
			get { return _fetchCount; }
			set { _fetchCount = value; }
		}

		#region Parameters

		/// <summary> Creates non-internal parameters on the provider. </summary>
		protected override void PrepareParameters()
		{
			// Prepare parameters
			for (int index = 0; index < _parameterIndexes.Length; index++)
			{
				SQLParameter parameter = Parameters[_parameterIndexes[index]];
				SqlParameter mSSQLParameter = (SqlParameter)_command.CreateParameter();
				mSSQLParameter.ParameterName = String.Format("@{0}", ConvertParameterName(parameter.Name));
				switch (parameter.Direction)
				{
					case SQLDirection.Out:
						mSSQLParameter.Direction = System.Data.ParameterDirection.Output;
						break;
					case SQLDirection.InOut:
						mSSQLParameter.Direction = System.Data.ParameterDirection.InputOutput;
						break;
					case SQLDirection.Result:
						mSSQLParameter.Direction = System.Data.ParameterDirection.ReturnValue;
						break;
					default:
						mSSQLParameter.Direction = System.Data.ParameterDirection.Input;
						break;
				}

				if (parameter.Type is SQLStringType)
				{
					// Unicode, not varchar: see the reasoning on GetParamTypeDescriptor, which has to say the same thing for the
					// server-side cursor path and is where the decision and its measured cost are recorded.
					mSSQLParameter.SqlDbType = SqlDbType.NVarChar;
					mSSQLParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					mSSQLParameter.SqlDbType = SqlDbType.Bit;
				}
				else if (parameter.Type is SQLByteArrayType)
				{
					mSSQLParameter.SqlDbType = SqlDbType.Binary;
					mSSQLParameter.Size = ((SQLByteArrayType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1:
							mSSQLParameter.SqlDbType = SqlDbType.TinyInt;
							break;
						case 2:
							mSSQLParameter.SqlDbType = SqlDbType.SmallInt;
							break;
						case 8:
							mSSQLParameter.SqlDbType = SqlDbType.BigInt;
							break;
						default:
							mSSQLParameter.SqlDbType = SqlDbType.Int;
							break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					mSSQLParameter.SqlDbType = SqlDbType.Decimal;
					mSSQLParameter.Scale = type.Scale;
					mSSQLParameter.Precision = type.Precision;
				}
				else if (parameter.Type is SQLFloatType)
				{
					SQLFloatType type = (SQLFloatType)parameter.Type;
					if (type.Width == 1)
						mSSQLParameter.SqlDbType = SqlDbType.Real;
					else
						mSSQLParameter.SqlDbType = SqlDbType.Float;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					mSSQLParameter.SqlDbType = SqlDbType.Image;
				}
				else if (parameter.Type is SQLTextType)
				{
					// ntext for the same reason as nvarchar above, and with none of the precedence cost: a text column can be
					// neither compared nor indexed in T-SQL, so no plan can change shape over it.
					mSSQLParameter.SqlDbType = SqlDbType.NText;
				}
				else if (parameter.Type is SQLDateType)
				{
					mSSQLParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (parameter.Type is SQLTimeType)
				{
					mSSQLParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					mSSQLParameter.SqlDbType = SqlDbType.DateTime;
				}
				else if (parameter.Type is SQLGuidType)
				{
					mSSQLParameter.SqlDbType = SqlDbType.UniqueIdentifier;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					mSSQLParameter.SqlDbType = SqlDbType.Money;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Add(mSSQLParameter);
			}
		}

		/// <remarks> If the parameter is named "Pnnn" there may be a conflict with the provider's automatic parameter scheme, so escape (with an extra P) under those circumstances. </remarks>
		protected override string ConvertParameterName(string parameterName)
		{
			if (parameterName.StartsWith("P", StringComparison.OrdinalIgnoreCase) && (parameterName.Length > 1) && Char.IsDigit(parameterName[1]))
				return "P" + parameterName;
			else
				return parameterName;
		}

		/// <summary> The number of internal parameters so that we can "hide" the internal parameters. </summary>
		private int _internalParameterCount;

		/// <summary> Accesses a given non-internal parameter. </summary>
		/// <remarks> This method hides the presence of the internal parameters so that generic command routines can deal with parameters ordinally starting from 0.
		/// This method should be used rather than access the provider's parameters directly by index. </remarks>
		protected override IDataParameter ParameterByIndex(int index)
		{
			// Internal Parameters are kept before regular parameters (because sp_cursoropen requires this)
			return base.ParameterByIndex(index + _internalParameterCount);
		}

		/// <summary> Creates an internal parameter. </summary>
		/// <remarks> Internal parameters are only part of the provider's parameter space.  They are used as part of the sp_cursor calls. </remarks>
		internal SqlParameter InternalParameterCreate(string name, SqlDbType type, ParameterDirection direction)
		{
			SqlParameter parameter = (SqlParameter)_command.CreateParameter();
			parameter.ParameterName = name;
			parameter.SqlDbType = type;
			parameter.Direction = direction;
			_command.Parameters.Add(parameter);
			_internalParameterCount++;
			return parameter;
		}

		/// <summary> Clears all internal and non-internal parameters. </summary>
		protected override void ClearParameters()
		{
			base.ClearParameters();
			_internalParameterCount = 0;
		}

		/// <summary> Determines whether parameters are being used and that there are any non-internal parameters. </summary>
		protected bool ParametersActive()
		{
			return (_parameterIndexes.Length > 0) && UseParameters;
		}

		#endregion

		#region Server Cursor Open preparation

		/// <summary> Prepares for invocation of a server-side cursor. </summary>
		private void PrepareCursorStatement(string statement, SQLCursorType cursorType, SQLIsolationLevel isolationLevel)
		{
			// Cannot use the provider's command type behavior because we are always enclosing the expression in sp_cursor calls
			if (CommandType == SQLCommandType.Table)
				statement = "select * from " + statement;

			InternalParameterCreate("@retval", SqlDbType.Int, ParameterDirection.ReturnValue);
			InternalParameterCreate("@cursor", SqlDbType.Int, ParameterDirection.Output).Value = 0;
			InternalParameterCreate("@stmt", SqlDbType.NVarChar, ParameterDirection.Input).Value = statement;
			InternalParameterCreate("@scrollopt", SqlDbType.Int, ParameterDirection.Input).Value = GetScrollOptions(cursorType, isolationLevel);
			InternalParameterCreate("@ccopt", SqlDbType.Int, ParameterDirection.Input).Value = GetConcurrencyOptions();
			InternalParameterCreate("@rowcount", SqlDbType.Int, ParameterDirection.InputOutput).Value = FetchCount;	// This must be an output parameter or an error will occur

			// Create the parameter definition parameter here (before setting its value) to ensure that it comes in the right order (otherwise sp_cursoropen complains)
			if (ParametersActive())
				InternalParameterCreate("@paramdef", SqlDbType.NVarChar, ParameterDirection.Input).Value = "";

			_command.CommandText = "sp_cursoropen";
		}

		/// <summary> Gets the SQL Server server side cursor scroll options. </summary>
		private SQLServerCursorScrollOptions GetScrollOptions(SQLCursorType cursorType, SQLIsolationLevel isolationLevel)
		{
			SQLServerCursorScrollOptions results = SQLServerCursorScrollOptions.AutoFetch | SQLServerCursorScrollOptions.AutoClose;

			if (ParametersActive())
				results |= SQLServerCursorScrollOptions.Parameterized;

			if (LockType == SQLLockType.ReadOnly)
				results |= SQLServerCursorScrollOptions.FastForward;
			else
				results |= SQLServerCursorScrollOptions.ForwardOnly;

			/*
			Setting these is not agreeable to the server
			if (ACursorType == SQLCursorType.Dynamic)
				LResults |= SQLServerCursorScrollOptions.Dynamic;
			else
				LResults |= SQLServerCursorScrollOptions.Static;
			*/
			// TODO: Set additional "acceptible" options 

			return results;
		}

		/// <summary> Gets the SQL Server server side cursor concurrency options.</summary>
		private SQLServerCursorConcurrencyOptions GetConcurrencyOptions()
		{
			SQLServerCursorConcurrencyOptions result = SQLServerCursorConcurrencyOptions.OpenOnAnySQL;

			switch (LockType)
			{
				case SQLLockType.ReadOnly: result |= SQLServerCursorConcurrencyOptions.ReadOnly; break;
				case SQLLockType.Pessimistic: result |= SQLServerCursorConcurrencyOptions.ScrollLocks; break;
				case SQLLockType.Optimistic: result |= SQLServerCursorConcurrencyOptions.OptimisticValues; break;
			}

			return result;
		}

		/// <summary> Sets the parameter specifying the names and types of the non-internal parameters. </summary>
		private void SetParameterDefinition()
		{
			StringBuilder result = new StringBuilder();
			for (int index = 0; index < _parameterIndexes.Length; index++)
			{
				SQLParameter parameter = Parameters[_parameterIndexes[index]];
				if (parameter.Direction != SQLDirection.Result)
				{
					if (result.Length > 0)
						result.Append(", ");

					result.Append("@" + ConvertParameterName(parameter.Name) + " " + GetParamTypeDescriptor(parameter));
					if (parameter.Direction != SQLDirection.In)
						result.Append(" out");
				}
			}
			((SqlParameter)_command.Parameters["@paramdef"]).Value = result.ToString();
		}

		/// <summary>The longest nvarchar a type descriptor can name; past it the descriptor has to say max instead.</summary>
		/// <remarks>
		/// varchar counts bytes to 8,000 and nvarchar counts characters to 4,000, so a width that was describable as varchar(n)
		/// is not always describable as nvarchar(n). A parameter past this is nvarchar(max), which has no length to be wrong.
		/// </remarks>
		private const int MaxNVarCharLength = 4000;

		/// <summary>The nvarchar descriptor for a declared character length, as max where the length cannot be named.</summary>
		/// <remarks>A length of zero or less is a caller that did not say how wide the value is; max is the only honest answer.</remarks>
		private static string NVarCharDescriptor(int length)
		{
			return ((length <= 0) || (length > MaxNVarCharLength)) ? "nvarchar(max)" : "nvarchar(" + length.ToString() + ")";
		}

		/// <summary> Generates a SQL Server type descriptor given a parameter definition. </summary>
		/// <remarks>
		/// <para>
		/// This is the type SQL Server is told the parameter has in the @paramdef string that sp_cursoropen is given, for the
		/// server-side cursor path. PrepareParameters above says the same thing to the provider for the client-side path, and the
		/// two must agree: which of the two a statement takes is decided by DetectCursorLocation, so a difference between them
		/// would be a device whose behaviour depended on whether the statement began with "select".
		/// </para>
		/// <para>
		/// Text goes as Unicode, which is what a varchar descriptor here cost: SQL Server converts a single-byte value to the
		/// database's collation code page on its way in, and every character that code page has no room for becomes a question
		/// mark, silently and with nothing left to read back. That fell on a correct schema, because this device accepts nvarchar,
		/// nchar and ntext columns as D4 strings and SQLText (MSSQLDevice.FindScalarType) and the catalog store's own columns are
		/// nvarchar throughout. A varchar column still holds only what a varchar column can hold - the server converts the
		/// parameter on the way into it - so this costs a varchar schema no data, only, sometimes, a query plan.
		/// </para>
		/// </remarks>
		// NOTE: accepted tradeoff - an nvarchar parameter compared against an indexed varchar column in a *SQL* collation
		// (SQL_Latin1_General_CP1_CI_AS and its siblings) cannot seek, because nvarchar is the higher-precedence type and the
		// server converts the column rather than the parameter. Measured 2026-11-25 on SQL Server 2016 SP1 Express LocalDB
		// (13.0.4001.0), database collation SQL_Latin1_General_CP1_CI_AS, 200,000 rows, an index on a varchar(50) column, one
		// row matched: a varchar parameter gave Index Seek and 3 logical reads, an nvarchar parameter gave Index Scan with
		// CONVERT_IMPLICIT(nvarchar(50), column) and 625 logical reads - 208 times the reads. The same measurement against a
		// column in a *Windows* collation (Latin1_General_CI_AS) gave Index Seek through GetRangeThroughConvert and 3 logical
		// reads, so the regression is specific to SQL collations. Unicode was kept: a character lost on the way in cannot be
		// recovered, where a slow query can be seen and fixed, and the alternative - sending Unicode only where the receiving
		// column is nvarchar - means carrying each column's native type down to its parameter, which neither SQLParameter nor
		// the SQL syntax tree the devices build has any room for today. The two remedies a deployment has are in
		// docs/migration-notes.md. Revisit if a deployment reports the regression and neither remedy is open to it, and then
		// look at plumbing the column's type rather than at going back to varchar. The first remedy is not open to a table the
		// device itself created: its own DDL for a D4 string column is varchar(n) (SQLString.InternalNativeDomainName), which
		// is a second half of this defect rather than a schema choice - bug-mssql-device-text-columns-lose-characters owns it.
		private static string GetParamTypeDescriptor(SQLParameter parameter)
		{
			if (parameter.Type is SQLStringType)
				return NVarCharDescriptor(((SQLStringType)parameter.Type).Length);
			else if (parameter.Type is SQLBooleanType)
				return "bit";
			else if (parameter.Type is SQLByteArrayType)
				return "binary(" + ((SQLByteArrayType)parameter.Type).Length.ToString() + ")";
			else if (parameter.Type is SQLIntegerType)
			{
				switch (((SQLIntegerType)parameter.Type).ByteCount)
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
			else if (parameter.Type is SQLNumericType)
			{
				SQLNumericType type = (SQLNumericType)parameter.Type;
				return "decimal(" + type.Precision.ToString() + "," + type.Scale.ToString() + ")";
			}
			else if (parameter.Type is SQLFloatType)
			{
				SQLFloatType type = (SQLFloatType)parameter.Type;
				if (type.Width == 1)
					return "real";
				else
					return "float";
			}
			else if (parameter.Type is SQLBinaryType)
				return "image";
			else if (parameter.Type is SQLTextType)
				return "ntext";		// ntext for the same reason as nvarchar above; the question this answers had stood here since 2008
			else if ((parameter.Type is SQLDateType) || (parameter.Type is SQLTimeType) || (parameter.Type is SQLDateTimeType))
				return "datetime";
			else if (parameter.Type is SQLGuidType)
				return "uniqueidentifier";
			else if (parameter.Type is SQLMoneyType)
				return "money";
			else
				throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
		}

		#endregion

		#region Open and prepare

		protected override void InternalPrepare()
		{
			// There seems to be no advantage to preparing
		}

		/// <summary> Prepares for a server cursor open. </summary>
		protected void PrepareServerCursorCommand(SQLCursorType cursorType, SQLIsolationLevel isolationLevel)
		{
			// All commands will be stored procedures when using server-side cursors
			_command.CommandType = System.Data.CommandType.StoredProcedure;	// always use stored procedure because queries are wrapped in a server-side cursor stored procedure call
			if (CommandTimeout >= 0)
				_command.CommandTimeout = CommandTimeout;

			// Call PrepareStatement first to determine the set of non-internal parameters and normalize the statement
			string statement = PrepareStatement(Statement);

			// Call PrepareCursorStatement before creating the non-internal parameters because sp_cursoropen is fickle about the order of the arguments (even though they are passed by name)
			PrepareCursorStatement(statement, cursorType, isolationLevel);
			
			if (ParametersActive())
			{
				// Prepare non-internal parameters
				PrepareParameters();

				// One of the already created internal parameters is a list of the non-internal parameters; set it now that we have the non-internal parameters configured.
				SetParameterDefinition();

				//FCommand.Prepare();	// Do not prepare against SQL Server.  It seems that it is perhaps never faster to do so.
			}
		}

		protected override SQLCursor InternalOpen(SQLCursorType cursorType, SQLIsolationLevel isolationLevel)
		{
			// Prepare the connection
			SQLCursorLocation location = GetCursorLocation();
			if (location == SQLCursorLocation.Server)
				PrepareServerCursorCommand(cursorType, isolationLevel);
			else
				PrepareCommand(false, isolationLevel);

			// Set the non-internal parameters
			SetParameters();

			// Open the reader
			IDataReader cursor = _command.ExecuteReader(SQLCommandBehaviorToCommandBehavior(CommandBehavior));

			// Return the appropriate cursor
			if (location == SQLCursorLocation.Server)
				return new MSSQLServerCursor(this, cursor);
			else
				return new DotNetCursor(this, cursor);
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

		internal void CloseServerCursor(int cursorHandle)
		{
			ClearParameters();
			InternalParameterCreate("@cursor", SqlDbType.Int, ParameterDirection.Input).Value = cursorHandle;
			_command.CommandText = "sp_cursorclose";
			_command.ExecuteNonQuery();
		}

		internal IDataReader FetchServerCursor(int cursorHandle)
		{
			ClearParameters();
			InternalParameterCreate("@retval", SqlDbType.Int, ParameterDirection.ReturnValue);
			InternalParameterCreate("@cursor", SqlDbType.Int, ParameterDirection.Input).Value = cursorHandle;
			InternalParameterCreate("@fetchtype", SqlDbType.Int, ParameterDirection.Input).Value = 0x0002 /* next */;
			InternalParameterCreate("@rownum", SqlDbType.Int, ParameterDirection.Input).Value = 0;
			InternalParameterCreate("@nrows", SqlDbType.Int, ParameterDirection.Input).Value = FetchCount;
			_command.CommandText = "sp_cursorfetch";
			return _command.ExecuteReader(SQLCommandBehaviorToCommandBehavior(CommandBehavior));
		}

		internal object GetParameterValue(string parameterName)
		{
			return ((SqlParameter)_command.Parameters[parameterName]).Value;
		}
		
		internal bool ContainsParameter(string parameterName)
		{
			return (_command != null) && _command.Parameters.Contains(parameterName);
		}

		internal void ParametersAreAvailable()
		{
			GetParameters();
		}

		#endregion
	}
}
