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
	// Provides the behavior template for a connection to an SQL-based DBMS
	public abstract class SQLConnection : Disposable
	{
		protected abstract SQLCommand InternalCreateCommand();
		public SQLCommand CreateCommand(bool AIsCursor)
		{
			SQLCommand LCommand = InternalCreateCommand();
			if (FDefaultCommandTimeout >= 0)
				LCommand.CommandTimeout = FDefaultCommandTimeout;
			if (AIsCursor)
				LCommand.UseParameters = FDefaultUseParametersForCursors;
			LCommand.ShouldNormalizeWhitespace = FDefaultShouldNormalizeWhitespace;
			return LCommand;
		}

		private SQLConnectionState FState;
		public SQLConnectionState State { get { return FState; } }
		protected void SetState(SQLConnectionState AState)
		{
			FState = AState;
		}
		
		private SQLCommand FActiveCommand;
		public SQLCommand ActiveCommand { get { return FActiveCommand; } }
		internal void SetActiveCommand(SQLCommand ACommand)
		{
			if ((ACommand != null) && (FActiveCommand != null))
				throw new ConnectionException(ConnectionException.Codes.ConnectionBusy);
			FActiveCommand = ACommand;
			if (FActiveCommand != null)
				FState = SQLConnectionState.Executing;
			else
				FState = SQLConnectionState.Idle;
		}
		
		private SQLCursor FActiveCursor;
		public SQLCursor ActiveCursor { get { return FActiveCursor; } }
		internal void SetActiveCursor(SQLCursor ACursor)
		{
			if ((ACursor != null) && (FActiveCursor != null))
				throw new ConnectionException(ConnectionException.Codes.ConnectionBusy);
			FActiveCursor = ACursor;
			if (FActiveCursor != null)
				FState = SQLConnectionState.Reading;
			else
				if (FActiveCommand != null)
					FState = SQLConnectionState.Executing;
				else
					FState = SQLConnectionState.Idle;
		}
		
		public void Execute(string AStatement, SQLParameters AParameters)
		{
			CheckConnectionValid();
			using (SQLCommand LCommand = CreateCommand(false))
			{
				LCommand.Statement = AStatement;
				LCommand.Parameters.AddRange(AParameters);
				LCommand.Execute();
			}
		}
		
		public void Execute(string AStatement)
		{
			Execute(AStatement, new SQLParameters());
		}
		
		public SQLCursor Open(string AStatement, SQLParameters AParameters, SQLCursorType ACursorType, SQLIsolationLevel ACursorIsolationLevel, SQLCommandBehavior ABehavior)
		{
			CheckConnectionValid();
			SQLCommand LCommand = CreateCommand(true);
			try
			{
				LCommand.Statement = AStatement;
				LCommand.Parameters.AddRange(AParameters);
				LCommand.CommandBehavior = ABehavior;
				return LCommand.Open(ACursorType, ACursorIsolationLevel);
			}
			catch
			{
				LCommand.Dispose();
				throw;
			}
		}
		
		public SQLCursor Open(string AStatement)
		{
			return Open(AStatement, new SQLParameters(), SQLCursorType.Dynamic, SQLIsolationLevel.ReadUncommitted, SQLCommandBehavior.Default);
		}
		
		public void Close(SQLCursor ACursor)
		{
			SQLCommand LCommand = ACursor.Command;
			try
			{
				ACursor.Command.Close(ACursor);
			}
			finally
			{
				LCommand.Dispose();
			}
		}

		private bool FInTransaction;
		public bool InTransaction { get { return FInTransaction; } }

		protected void CheckNotInTransaction()
		{
			if (FInTransaction)
				throw new ConnectionException(ConnectionException.Codes.TransactionInProgress);
		}
		
		protected void CheckInTransaction()
		{
			if (!FInTransaction)
				throw new ConnectionException(ConnectionException.Codes.NoTransactionInProgress);
		}
		
		protected abstract void InternalBeginTransaction(SQLIsolationLevel AIsolationLevel);
		public void BeginTransaction(SQLIsolationLevel AIsolationLevel)
		{
			CheckConnectionValid();
			CheckNotInTransaction();
			try
			{
				InternalBeginTransaction(AIsolationLevel);
			}
			catch (Exception LException)
			{							
				WrapException(LException, "begin transaction", false);
			}
			FInTransaction = true;
			FTransactionFailure = false;
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
			catch (Exception LException)
			{
				WrapException(LException, "commit transaction", false);
			}
			FInTransaction = false;
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
			catch (Exception LException)
			{
				WrapException(LException, "rollback transaction", false);
			}
			FInTransaction = false;
		}

		public const int CDefaultCommandTimeout = -1;
		private int FDefaultCommandTimeout = CDefaultCommandTimeout;
		/// <summary>The amount of time to wait before timing out when waiting for a command to execute, expressed in seconds.</summary>
		/// <remarks>The default value for this property is 30 seconds. A value of 0 indicates an infinite timeout.</remarks>
		public int DefaultCommandTimeout
		{
			get { return FDefaultCommandTimeout; }
			set { FDefaultCommandTimeout = value; }
		}
		
		private bool FDefaultUseParametersForCursors = true;
		public bool DefaultUseParametersForCursors
		{
			get { return FDefaultUseParametersForCursors; }
			set { FDefaultUseParametersForCursors = value; }
		}
		
		private bool FDefaultShouldNormalizeWhitespace = true;
		public bool DefaultShouldNormalizeWhitespace
		{
			get { return FDefaultShouldNormalizeWhitespace; }
			set { FDefaultShouldNormalizeWhitespace = value; }
		}

		protected virtual Exception InternalWrapException(Exception AException, string AStatement)
		{
			return new ConnectionException(ConnectionException.Codes.SQLException, ErrorSeverity.Application, AException, AStatement);
		}

		public void WrapException(Exception AException, string AStatement, bool AMustThrow)
		{
			if (!IsConnectionValid())
			{
				if (InTransaction)
					FTransactionFailure = true;
				
				FState = SQLConnectionState.Closed;
			}
			else
			{
				if (InTransaction && IsTransactionFailure(AException))
					FTransactionFailure = true;
			}
			
			Exception LException = InternalWrapException(AException, AStatement);
			if (LException != null)
				throw LException;
				
			if (AMustThrow)
				throw AException;
		}

		/// <summary>Indicates whether the given exception indicates a transaction failure such as a deadlock or rollback on the target system.</summary>		
		protected virtual bool IsTransactionFailure(Exception AException)
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
					FTransactionFailure = true;
				
				FState = SQLConnectionState.Closed;
				
				throw new ConnectionException(ConnectionException.Codes.ConnectionClosed);
			}
		}

		protected bool FTransactionFailure;		
		/// <summary>Indicates that the currently active transaction has been rolled back by the target system due to a deadlock, or connection failure.</summary>
		public bool TransactionFailure { get { return FTransactionFailure; } }
		
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

