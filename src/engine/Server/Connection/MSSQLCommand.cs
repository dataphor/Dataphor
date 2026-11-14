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
					mSSQLParameter.SqlDbType = SqlDbType.VarChar;
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
					mSSQLParameter.SqlDbType = SqlDbType.Text;
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

		/// <summary> Generates a SQL Server type descriptor given a parameter definition. </summary>
		private static string GetParamTypeDescriptor(SQLParameter parameter)
		{
			if (parameter.Type is SQLStringType)
				return "varchar(" + ((SQLStringType)parameter.Type).Length.ToString() + ")";
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
				return "text";		// Q: Should this be NText or text
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
