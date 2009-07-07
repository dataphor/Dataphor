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
			if (CommandTimeout >= 0)
				FCommand.CommandTimeout = CommandTimeout;

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
		
		internal bool ContainsParameter(string AParameterName)
		{
			return (FCommand != null) && FCommand.Parameters.Contains(AParameterName);
		}

		internal void ParametersAreAvailable()
		{
			GetParameters();
		}

		#endregion
	}
}
