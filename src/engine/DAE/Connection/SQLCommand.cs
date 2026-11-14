/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Connection
{
	public abstract class SQLCommand : Disposable
	{
		protected SQLCommand(SQLConnection connection) : base()
		{
			_connection = connection;
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_activeCursor != null)
					Close(_activeCursor);
			}
			finally
			{
				_connection = null;
				base.Dispose(disposing);
			}
		}
		
		private SQLConnection _connection;
		public SQLConnection Connection { get { return _connection; } }
		
		private SQLCursor _activeCursor;
		public SQLCursor ActiveCursor { get { return _activeCursor; } }
		internal void SetActiveCursor(SQLCursor cursor)
		{
			if (cursor != null)
			{
				_connection.SetActiveCommand(this);
				_connection.SetActiveCursor(cursor);
			}
			else
			{
				_connection.SetActiveCursor(null);
				_connection.SetActiveCommand(null);
			}
			_activeCursor = cursor;
		}
		
		private string _statement;
		public string Statement
		{
			get { return _statement; }
			set 
			{ 
				if (_statement != value)
				{
					Unprepare();
					_statement = value;
				}
			}
		}
		
		public const int DefaultCommandTimeout = -1;
		private int _commandTimeout = DefaultCommandTimeout;
		/// <summary>The amount of time to wait before timing out when waiting for a command to execute, expressed in seconds.</summary>
		/// <remarks>The default value for this property is 30 seconds. A value of 0 indicates an infinite timeout.</remarks>
		public int CommandTimeout
		{
			get { return _commandTimeout; }
			set { _commandTimeout = value; }
		}
		
		private SQLCommandType _commandType = SQLCommandType.Statement;
		public SQLCommandType CommandType
		{
			get { return _commandType; }
			set { _commandType = value; }
		}
		
		private SQLCommandBehavior _commandBehavior = SQLCommandBehavior.Default;
		public SQLCommandBehavior CommandBehavior
		{
			get { return _commandBehavior; }
			set { _commandBehavior = value; }
		}
		
		private SQLLockType _lockType = SQLLockType.ReadOnly;
		public SQLLockType LockType
		{
			get { return _lockType; }
			set { _lockType = value; }
		}
		
		private SQLParameters _parameters = new SQLParameters();
		public SQLParameters Parameters { get { return _parameters; } }
		
		protected abstract void InternalPrepare();
		public void Prepare()
		{
			if (!_prepared)
			{
				try
				{
					InternalPrepare();
				}
				catch (Exception exception)
				{
					_connection.WrapException(exception, "prepare", false);
				}
				_prepared = true;
			}
		}
		
		protected abstract void InternalUnprepare();
		public void Unprepare()
		{
			if (_prepared)
			{
				try
				{
					InternalUnprepare();
				}
				catch (Exception exception)
				{
					_connection.WrapException(exception, "unprepare", false);
				}
				_prepared = false;
			}
		}
		
		protected bool _prepared;
		public bool Prepared
		{
			get { return _prepared; }
			set
			{
				if (_prepared != value)
				{
					if (value)
						Prepare();
					else
						Unprepare();
				}
			}
		}

		protected abstract void InternalExecute();
		public void Execute()
		{
			Prepare();
			try
			{
				_connection.SetActiveCommand(this);
				try
				{
					InternalExecute();
				}
				finally
				{
					_connection.SetActiveCommand(null);
				}
			}
			catch (Exception exception)
			{
				_connection.WrapException(exception, Statement, false);
			}
		}
		
		public object Evaluate()
		{
			SQLCursor cursor = Open(SQLCursorType.Dynamic, SQLIsolationLevel.Serializable);
			try
			{
				if (cursor.Next())
					return cursor[0];
				return null;
			}
			finally
			{
				Close(cursor);
			}
		}
		
		protected abstract SQLCursor InternalOpen(SQLCursorType cursorType, SQLIsolationLevel cursorIsolationLevel);
		public SQLCursor Open(SQLCursorType cursorType, SQLIsolationLevel cursorIsolationLevel)
		{
			Prepare();
			try
			{
				return InternalOpen(cursorType, cursorIsolationLevel);
			}
			catch (Exception exception)
			{
				_connection.WrapException(exception, Statement, true);
				throw;
			}
		}
		
		protected internal abstract void InternalClose();
		public void Close(SQLCursor cursor)
		{
			cursor.Dispose();
		}

		// maps the parameters in the order they appear in the statement to the order they appear in the parameters list
		// if ordinal binding is not used, this mapping is a wash (i.e. N=N)
		protected int[] _parameterIndexes;
		
		private bool IsValidIdentifierCharacter(char charValue)
		{
			return (charValue == '_') || Char.IsLetterOrDigit(charValue);
		}

		protected string _parameterDelimiter = "@";

		// True if the prepared statement should use a ? placeholder for ordinal binding of parameters
		// False if the prepared statement should use @<param name> placeholders for name binding of parameters
		protected bool _useOrdinalBinding = false;
		
		// True if the prepared statement should use parameterization and parameter markers to pass parameter values
		// False if the prepared statement should use parameter literals to pass parameter values
		protected bool _useParameters = true;
		public bool UseParameters
		{
			get { return _useParameters; }
			set { _useParameters = value; }
		}

		private SQLCursorLocation _cursorLocation = SQLCursorLocation.Automatic;
		public SQLCursorLocation CursorLocation
		{
			get { return _cursorLocation; }
			set { _cursorLocation = value; }
		}
		
		private bool _shouldNormalizeWhitespace = true;
		public bool ShouldNormalizeWhitespace
		{
			get { return _shouldNormalizeWhitespace; }
			set { _shouldNormalizeWhitespace = value; }
		}
		
		/// <summary> Normalize whitespace, determine the parameter indexes, and change parameter formats/names as necessary. </summary>
		protected virtual string PrepareStatement(string statement)
		{
			List<int> parameterIndexes = new List<int>();
			StringBuilder result = new StringBuilder(statement.Length + 10);
			StringBuilder parameterName = null;
			bool inParameter = false;
			bool inString = false;
			for (int index = 0; index < statement.Length; index++)
			{
				if (inParameter && !IsValidIdentifierCharacter(statement[index]))
					FinishParameter(parameterIndexes, result, parameterName, ref inParameter);
					
				switch (statement[index])
				{
					case '@' :
						if (!inString)
						{
							parameterName = new StringBuilder();
							inParameter = true;
						}
						break;
					
					case '\'' :
						inString = !inString;
						break;
				}
					
				if (!inParameter)
					result.Append((Char.IsWhiteSpace(statement[index]) && !inString && _shouldNormalizeWhitespace) ? ' ' : statement[index]);
				else
					parameterName.Append(statement[index]);
			}
			
			if (inParameter)
				FinishParameter(parameterIndexes, result, parameterName, ref inParameter);

			if (_useParameters)
			{
				_parameterIndexes = new int[parameterIndexes.Count];
				for (int index = 0; index < parameterIndexes.Count; index++)
					_parameterIndexes[index] = (int)parameterIndexes[index];
			}
			else
				_parameterIndexes = new int[0];

			return result.ToString();
		}

		private void FinishParameter(List<int> LParameterIndexes, StringBuilder LResult, StringBuilder LParameterName, ref bool LInParameter)
		{
			string parameterNameString = LParameterName.ToString().Substring(1);
			int parameterIndex = Parameters.IndexOf(parameterNameString);
			if (parameterIndex >= 0)
			{
				SQLParameter parameter = Parameters[parameterIndex];
				if (_useParameters)
				{
					if (_useOrdinalBinding || !LParameterIndexes.Contains(parameterIndex))
						LParameterIndexes.Add(parameterIndex);
					if (parameter.Marker == null)
						if (_useOrdinalBinding)
							LResult.Append("?");
						else
							LResult.Append(_parameterDelimiter + ConvertParameterName(parameterNameString));
					else
						LResult.Append(parameter.Marker);
				}
				else
					LResult.Append(parameter.Literal);
			}
			else
				LResult.Append(LParameterName.ToString());
			LInParameter = false;
		}

		/// <summary> Allows descendant commands to change the parameter names used in statements. </summary>
		protected virtual string ConvertParameterName(string parameterName)
		{
			return parameterName;
		}
	}
}
