/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Connection
{
	// Provides the behavior template for a connection to an SQL-based DBMS
	public abstract class SQLConnection : Disposable
	{
		protected abstract SQLCommand InternalCreateCommand();
		public SQLCommand CreateCommand(bool isCursor)
		{
			SQLCommand command = InternalCreateCommand();
			if (_defaultCommandTimeout >= 0)
				command.CommandTimeout = _defaultCommandTimeout;
			if (isCursor)
				command.UseParameters = _defaultUseParametersForCursors;
			command.ShouldNormalizeWhitespace = _defaultShouldNormalizeWhitespace;
			return command;
		}
		
		protected bool _supportsMARS;
		public bool SupportsMARS { get { return _supportsMARS; } }

		private SQLConnectionState _state;
		public SQLConnectionState State { get { return _state; } }
		protected void SetState(SQLConnectionState state)
		{
			_state = state;
		}
		
		private List<SQLCommand> _activeCommands = new List<SQLCommand>();
		private SQLCommand _activeCommand;
		public SQLCommand ActiveCommand { get { return _activeCommand; } }
		internal void SetActiveCommand(SQLCommand command)
		{
			if ((command != null) && (_activeCommand != null) && !_supportsMARS)
				throw new ConnectionException(ConnectionException.Codes.ConnectionBusy);
			if ((_activeCommand != null) && _activeCommands.Contains(_activeCommand))
				_activeCommands.Remove(_activeCommand);
			_activeCommand = command;
			if ((_activeCommand != null) && !_activeCommands.Contains(_activeCommand))
				_activeCommands.Add(_activeCommand);
			if (_activeCommands.Count > 0)
				_state = SQLConnectionState.Executing;
			else
				_state = SQLConnectionState.Idle;
		}
		
		private List<SQLCursor> _activeCursors = new List<SQLCursor>();
		private SQLCursor _activeCursor;
		public SQLCursor ActiveCursor { get { return _activeCursor; } }
		internal void SetActiveCursor(SQLCursor cursor)
		{
			if ((cursor != null) && (_activeCursor != null) && !_supportsMARS)
				throw new ConnectionException(ConnectionException.Codes.ConnectionBusy);
			if ((_activeCursor != null) && _activeCursors.Contains(_activeCursor))
				_activeCursors.Remove(_activeCursor);
			_activeCursor = cursor;
			if ((_activeCursor != null) && !_activeCursors.Contains(_activeCursor))
				_activeCursors.Add(_activeCursor);
			if (_activeCursors.Count > 0)
				_state = SQLConnectionState.Reading;
			else if (_activeCommands.Count > 0)
				_state = SQLConnectionState.Executing;
			else
				_state = SQLConnectionState.Idle;
		}
		
		public void Execute(string statement, SQLParameters parameters)
		{
			CheckConnectionValid();
			using (SQLCommand command = CreateCommand(false))
			{
				command.Statement = statement;
				command.Parameters.AddRange(parameters);
				command.Execute();
			}
		}
		
		public void Execute(string statement)
		{
			Execute(statement, new SQLParameters());
		}
		
		public SQLCursor Open(string statement, SQLParameters parameters, SQLCursorType cursorType, SQLIsolationLevel cursorIsolationLevel, SQLCommandBehavior behavior)
		{
			CheckConnectionValid();
			SQLCommand command = CreateCommand(true);
			try
			{
				command.Statement = statement;
				command.Parameters.AddRange(parameters);
				command.CommandBehavior = behavior;
				return command.Open(cursorType, cursorIsolationLevel);
			}
			catch
			{
				command.Dispose();
				throw;
			}
		}
		
		public SQLCursor Open(string statement)
		{
			return Open(statement, new SQLParameters(), SQLCursorType.Dynamic, SQLIsolationLevel.ReadUncommitted, SQLCommandBehavior.Default);
		}
		
		public void Close(SQLCursor cursor)
		{
			SQLCommand command = cursor.Command;
			try
			{
				cursor.Command.Close(cursor);
			}
			finally
			{
				command.Dispose();
			}
		}

		private bool _inTransaction;
		public bool InTransaction { get { return _inTransaction; } }

		protected void CheckNotInTransaction()
		{
			if (_inTransaction)
				throw new ConnectionException(ConnectionException.Codes.TransactionInProgress);
		}
		
		protected void CheckInTransaction()
		{
			if (!_inTransaction)
				throw new ConnectionException(ConnectionException.Codes.NoTransactionInProgress);
		}
		
		protected abstract void InternalBeginTransaction(SQLIsolationLevel isolationLevel);
		public void BeginTransaction(SQLIsolationLevel isolationLevel)
		{
			CheckConnectionValid();
			CheckNotInTransaction();
			try
			{
				InternalBeginTransaction(isolationLevel);
			}
			catch (Exception exception)
			{							
				WrapException(exception, "begin transaction", false);
			}
			_inTransaction = true;
			_transactionFailure = false;
		}

		protected abstract void InternalCommitTransaction();
		public void CommitTransaction()
		{
			CheckConnectionValid();
			CheckInTransaction();
			try
			{
				InternalCommitTransaction();
			}
			catch (Exception exception)
			{
				WrapException(exception, "commit transaction", false);
			}
			_inTransaction = false;
		}

		protected abstract void InternalRollbackTransaction();
		public void RollbackTransaction()
		{
			CheckConnectionValid();
			CheckInTransaction();
			try
			{
				InternalRollbackTransaction();
			}
			catch (Exception exception)
			{
				WrapException(exception, "rollback transaction", false);
			}
			_inTransaction = false;
		}

		public const int DefaultDefaultCommandTimeout = -1;
		private int _defaultCommandTimeout = DefaultDefaultCommandTimeout;
		/// <summary>The amount of time to wait before timing out when waiting for a command to execute, expressed in seconds.</summary>
		/// <remarks>The default value for this property is 30 seconds. A value of 0 indicates an infinite timeout.</remarks>
		public int DefaultCommandTimeout
		{
			get { return _defaultCommandTimeout; }
			set { _defaultCommandTimeout = value; }
		}
		
		private bool _defaultUseParametersForCursors = true;
		public bool DefaultUseParametersForCursors
		{
			get { return _defaultUseParametersForCursors; }
			set { _defaultUseParametersForCursors = value; }
		}
		
		private bool _defaultShouldNormalizeWhitespace = true;
		public bool DefaultShouldNormalizeWhitespace
		{
			get { return _defaultShouldNormalizeWhitespace; }
			set { _defaultShouldNormalizeWhitespace = value; }
		}

		protected virtual Exception InternalWrapException(Exception exception, string statement)
		{
			return new ConnectionException(ConnectionException.Codes.SQLException, ErrorSeverity.Application, exception, statement);
		}

		public void WrapException(Exception exception, string statement, bool mustThrow)
		{
            if (!IsConnectionValid())
			{
				if (InTransaction)
					_transactionFailure = true;
				
				_state = SQLConnectionState.Closed;
			}
			else
			{
				if (InTransaction && IsTransactionFailure(exception))
					_transactionFailure = true;
			}
			
			Exception localException = InternalWrapException(exception, statement);
			if (localException != null)
				throw localException;
				
			if (mustThrow)
				throw exception;
		}

		/// <summary>Indicates whether the given exception indicates a transaction failure such as a deadlock or rollback on the target system.</summary>		
		protected virtual bool IsTransactionFailure(Exception exception)
		{
			return false;
		}
		
		/// <summary>Indicates whether or not this connection is still valid.</summary>		
		public abstract bool IsConnectionValid();
		
		public void CheckConnectionValid()
		{
			if (!IsConnectionValid())
			{
				if (InTransaction)
					_transactionFailure = true;
				
				_state = SQLConnectionState.Closed;
				
				throw new ConnectionException(ConnectionException.Codes.ConnectionClosed);
			}
		}

		protected bool _transactionFailure;		
		/// <summary>Indicates that the currently active transaction has been rolled back by the target system due to a deadlock, or connection failure.</summary>
		public bool TransactionFailure { get { return _transactionFailure; } }
		
/*
		public void CleanupConnectionState(bool AShouldThrow)
		{
			if (!IsConnectionValid())
			{
				try
				{
					try
					{
						switch (FState)
						{
							case SQLConnectionState.Executing :
								ActiveCommand.Dispose();
							break;
							
							case SQLConnectionState.Reading :
								Close(ActiveCursor);
							break;
						}
					}
					finally
					{
						try
						{
							if (InTransaction)
								RollbackTransaction();
						}
						finally
						{
							SetState(SQLConnectionState.Closed);
						}
					}
				}
				catch
				{
					if (AShouldThrow)
						throw;
				}
			}
		}
*/
	}
}

