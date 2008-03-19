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
	public enum SQLIsolationLevel { ReadUncommitted, ReadCommitted, RepeatableRead, Serializable }
	public enum SQLCursorType { Static, Dynamic }
	public enum SQLCursorLocation { Automatic, Client, Server }
	public enum SQLConnectionState { Closed, Idle, Executing, Reading }
	public enum SQLCommandType { Statement, Table }
	public enum SQLLockType { ReadOnly, Optimistic, Pessimistic }

	[Flags]
	public enum SQLCommandBehavior { Default = 0, SchemaOnly = 1, KeyInfo = 2 }
	
	public class SQLDomain
	{
		public SQLDomain(Type AType, int ALength, int AScale, int APrecision, bool AIsLong) : base()
		{
			Type = AType;
			Length = ALength;
			Scale = AScale;
			Precision = APrecision;
			IsLong = AIsLong;
		}
		
		public Type Type;
		public int Length;
		public int Scale;
		public int Precision;
		public bool IsLong;
	}
	
	public class SQLColumn
	{
		public SQLColumn(string AName, SQLDomain ADomain) : base()
		{
			FName = AName;
			FDomain = ADomain;
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private SQLDomain FDomain;
		public SQLDomain Domain { get { return FDomain; } }
	}
	
	public class SQLColumns : TypedList
	{
		public SQLColumns() : base(typeof(SQLColumn)){}
		
		public new SQLColumn this[int AIndex]
		{
			get { return (SQLColumn)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (AName == this[LIndex].Name)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public class SQLIndexColumn
	{
		public SQLIndexColumn(string AName) : base()
		{
			FName = AName;
		}
		
		public SQLIndexColumn(string AName, bool AAscending) : base()
		{
			FName = AName;
			FAscending = AAscending;
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private bool FAscending = true;
		public bool Ascending { get { return FAscending; } }
	}
	
	public class SQLIndexColumns : TypedList
	{
		public SQLIndexColumns() : base(typeof(SQLIndexColumn)){}
		
		public new SQLIndexColumn this[int AIndex]
		{
			get { return (SQLIndexColumn)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (AName == this[LIndex].Name)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public class SQLIndex
	{
		public SQLIndex(string AName) : base()
		{
			FName = AName;
		}
		
		public SQLIndex(string AName, SQLIndexColumn[] AColumns)
		{
			FName = AName;
			FColumns.AddRange(AColumns);
		}
		
		public SQLIndex(string AName, SQLIndexColumns AColumns)
		{
			FName = AName;
			FColumns.AddRange(AColumns);
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private bool FIsUnique = false;
		public bool IsUnique 
		{ 
			get { return FIsUnique; } 
			set { FIsUnique = value; }
		}
		
		private SQLIndexColumns FColumns = new SQLIndexColumns();
		public SQLIndexColumns Columns { get { return FColumns; } }
	}
	
	public class SQLIndexes : TypedList
	{
		public SQLIndexes() : base(typeof(SQLIndex)){}
		
		public new SQLIndex this[int AIndex]
		{
			get { return (SQLIndex)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (AName == this[LIndex].Name)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public class SQLTableSchema
	{
		private SQLColumns FColumns = new SQLColumns();
		public SQLColumns Columns { get { return FColumns; } }
		
		private SQLIndexes FIndexes = new SQLIndexes();
		public SQLIndexes Indexes { get { return FIndexes; } }
	}
	
	// Provides the behavior template for a connection to an SQL-based DBMS
	public abstract class SQLConnection : Disposable
	{
		public abstract SQLCommand CreateCommand();

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
			using (SQLCommand LCommand = CreateCommand())
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
			SQLCommand LCommand = CreateCommand();
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

		private bool FUseParameters = true;
		public bool UseParameters 
		{ 
			get { return FUseParameters; } 
			set { FUseParameters = value; } 
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
	
	public enum SQLDirection { In, Out, InOut, Result }
	
	public class SQLParameter
	{
		public SQLParameter() : base(){}
		public SQLParameter(string AName, SQLType AType) : base()
		{
			FName = AName;
			FType = AType;
		}
		
		public SQLParameter(string AName, SQLType AType, object AValue) : base()
		{
			FName = AName;
			FType = AType;
			FValue = AValue;
		}
		
		public SQLParameter(string AName, SQLType AType, object AValue, SQLDirection ADirection) : base()
		{
			FName = AName;
			FType = AType;
			FValue = AValue;
			FDirection = ADirection;
		}
		
		public SQLParameter(string AName, SQLType AType, object AValue, SQLDirection ADirection, string AMarker) : base()
		{
			FName = AName;
			FType = AType;
			FValue = AValue;
			FDirection = ADirection;
			FMarker = AMarker;
		}
		
		public SQLParameter(string AName, SQLType AType, object AValue, SQLDirection ADirection, string AMarker, string ALiteral) : base()
		{
			FName = AName;
			FType = AType;
			FValue = AValue;
			FDirection = ADirection;
			FMarker = AMarker;
			FLiteral = ALiteral;
		}
		
		private string FName;
		public string Name
		{
			get { return FName; }
			set { FName = value; }
		}	

		private SQLType FType;
		public SQLType Type
		{
			get { return FType; }
			set { FType = value; }
		}

		private SQLDirection FDirection;
		public SQLDirection Direction
		{
			get { return FDirection; }
			set { FDirection = value; }
		}
		
		private object FValue;
		public object Value
		{
			get { return FValue; }
			set { FValue = value; }
		}	
		
		private string FMarker;
		public string Marker
		{
			get { return FMarker; }
			set { FMarker = value; }
		}
		
		private string FLiteral;
		public string Literal
		{
			get { return FLiteral; }
			set { FLiteral = value; }
		}
	}
	
	// Parameters are denoted by @<parameter name> in the Statement set on the command object, these will be replaced as necessary by each connection type
	
	public class SQLParameters : TypedList
	{
		public SQLParameters() : base(typeof(SQLParameter)){}
		
		public new SQLParameter this[int AIndex]
		{
			get { return (SQLParameter)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (AName == this[LIndex].Name)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public abstract class SQLCommand : Disposable
	{
		protected SQLCommand(SQLConnection AConnection) : base()
		{
			FConnection = AConnection;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FActiveCursor != null)
					Close(FActiveCursor);
			}
			finally
			{
				FConnection = null;
				base.Dispose(ADisposing);
			}
		}
		
		private SQLConnection FConnection;
		public SQLConnection Connection { get { return FConnection; } }
		
		private SQLCursor FActiveCursor;
		public SQLCursor ActiveCursor { get { return FActiveCursor; } }
		internal void SetActiveCursor(SQLCursor ACursor)
		{
			if (ACursor != null)
			{
				FConnection.SetActiveCommand(this);
				FConnection.SetActiveCursor(ACursor);
			}
			else
			{
				FConnection.SetActiveCursor(null);
				FConnection.SetActiveCommand(null);
			}
			FActiveCursor = ACursor;
		}
		
		private string FStatement;
		public string Statement
		{
			get { return FStatement; }
			set 
			{ 
				if (FStatement != value)
				{
					Unprepare();
					FStatement = value;
				}
			}
		}
		
		private SQLCommandType FCommandType = SQLCommandType.Statement;
		public SQLCommandType CommandType
		{
			get { return FCommandType; }
			set { FCommandType = value; }
		}
		
		private SQLCommandBehavior FCommandBehavior = SQLCommandBehavior.Default;
		public SQLCommandBehavior CommandBehavior
		{
			get { return FCommandBehavior; }
			set { FCommandBehavior = value; }
		}
		
		private SQLLockType FLockType = SQLLockType.ReadOnly;
		public SQLLockType LockType
		{
			get { return FLockType; }
			set { FLockType = value; }
		}
		
		private SQLParameters FParameters = new SQLParameters();
		public SQLParameters Parameters { get { return FParameters; } }
		
		protected abstract void InternalPrepare();
		public void Prepare()
		{
			if (!FPrepared)
			{
				try
				{
					InternalPrepare();
				}
				catch (Exception LException)
				{
					FConnection.WrapException(LException, "prepare", false);
				}
				FPrepared = true;
			}
		}
		
		protected abstract void InternalUnprepare();
		public void Unprepare()
		{
			if (FPrepared)
			{
				try
				{
					InternalUnprepare();
				}
				catch (Exception LException)
				{
					FConnection.WrapException(LException, "unprepare", false);
				}
				FPrepared = false;
			}
		}
		
		protected bool FPrepared;
		public bool Prepared
		{
			get { return FPrepared; }
			set
			{
				if (FPrepared != value)
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
				FConnection.SetActiveCommand(this);
				try
				{
					InternalExecute();
				}
				finally
				{
					FConnection.SetActiveCommand(null);
				}
			}
			catch (Exception LException)
			{
				FConnection.WrapException(LException, Statement, false);
			}
		}
		
		protected abstract SQLCursor InternalOpen(SQLCursorType ACursorType, SQLIsolationLevel ACursorIsolationLevel);
		public SQLCursor Open(SQLCursorType ACursorType, SQLIsolationLevel ACursorIsolationLevel)
		{
			Prepare();
			try
			{
				return InternalOpen(ACursorType, ACursorIsolationLevel);
			}
			catch (Exception LException)
			{
				FConnection.WrapException(LException, Statement, true);
				throw;
			}
		}
		
		protected internal abstract void InternalClose();
		public void Close(SQLCursor ACursor)
		{
			ACursor.Dispose();
		}

		// maps the parameters in the order they appear in the statement to the order they appear in the parameters list
		// if ordinal binding is not used, this mapping is a wash (i.e. N=N)
		protected int[] FParameterIndexes;
		
		private bool IsValidIdentifierCharacter(char AChar)
		{
			return (AChar == '_') || Char.IsLetterOrDigit(AChar);
		}

		protected string FParameterDelimiter = "@";

		// True if the prepared statement should use a ? placeholder for ordinal binding of parameters
		// False if the prepared statement should use @<param name> placeholders for name binding of parameters
		protected bool FUseOrdinalBinding = false;
		
		// True if the prepared statement should use parameterization and parameter markers to pass parameter values
		// False if the prepared statement should use parameter literals to pass parameter values
		protected bool FUseParameters = true;
		public bool UseParameters
		{
			get { return FUseParameters; }
			set { FUseParameters = value; }
		}

		private SQLCursorLocation FCursorLocation = SQLCursorLocation.Automatic;
		public SQLCursorLocation CursorLocation
		{
			get { return FCursorLocation; }
			set { FCursorLocation = value; }
		}
		
		/// <summary> Normalize whitespace, determine the parameter indexes, and change parameter formats/names as necessary. </summary>
		protected virtual string PrepareStatement(string AStatement)
		{
			List<int> LParameterIndexes = new List<int>();
			StringBuilder LResult = new StringBuilder(AStatement.Length + 10);
			StringBuilder LParameterName = null;
			bool LInParameter = false;
			bool LInString = false;
			for (int LIndex = 0; LIndex < AStatement.Length; LIndex++)
			{
				if (LInParameter && !IsValidIdentifierCharacter(AStatement[LIndex]))
					FinishParameter(LParameterIndexes, LResult, LParameterName, ref LInParameter);
					
				switch (AStatement[LIndex])
				{
					case '@' :
						if (!LInString)
						{
							LParameterName = new StringBuilder();
							LInParameter = true;
						}
						break;
					
					case '\'' :
						if (LInString)
						{
							if (((LIndex + 1) >= AStatement.Length) || (AStatement[LIndex + 1] != '\''))
								LInString = false;
						}
						else
							LInString = true;
						break;
				}
					
				if (!LInParameter)
					LResult.Append((Char.IsWhiteSpace(AStatement[LIndex]) ? ' ' : AStatement[LIndex]));
				else
					LParameterName.Append(AStatement[LIndex]);
			}
			
			if (LInParameter)
				FinishParameter(LParameterIndexes, LResult, LParameterName, ref LInParameter);

			if (FUseParameters)
			{
				FParameterIndexes = new int[LParameterIndexes.Count];
				for (int LIndex = 0; LIndex < LParameterIndexes.Count; LIndex++)
					FParameterIndexes[LIndex] = (int)LParameterIndexes[LIndex];
			}
			else
				FParameterIndexes = new int[0];

			return LResult.ToString();
		}

		private void FinishParameter(List<int> LParameterIndexes, StringBuilder LResult, StringBuilder LParameterName, ref bool LInParameter)
		{
			string LParameterNameString = LParameterName.ToString().Substring(1);
			int LParameterIndex = Parameters.IndexOf(LParameterNameString);
			if (LParameterIndex >= 0)
			{
				SQLParameter LParameter = Parameters[LParameterIndex];
				if (FUseParameters)
				{
					if (FUseOrdinalBinding || !LParameterIndexes.Contains(LParameterIndex))
						LParameterIndexes.Add(LParameterIndex);
					if (LParameter.Marker == null)
						if (FUseOrdinalBinding)
							LResult.Append("?");
						else
							LResult.Append(FParameterDelimiter + ConvertParameterName(LParameterNameString));
					else
						LResult.Append(LParameter.Marker);
				}
				else
					LResult.Append(LParameter.Literal);
			}
			else
				LResult.Append(LParameterName.ToString());
			LInParameter = false;
		}

		/// <summary> Allows descendant commands to change the parameter names used in statements. </summary>
		protected virtual string ConvertParameterName(string AParameterName)
		{
			return AParameterName;
		}
	}

	// Provides the behavior template for a forward only cursor
	// Values are returned in terms of native CLR values, with a null reference returned for a null column.
	// Deferred Read values are returned in terms of a random access read only stream on the data.
	// Deferred streams must be accessible after the Cursor is closed or navigated.
	public abstract class SQLCursor : Disposable
	{
		protected SQLCursor(SQLCommand ACommand) : base()
		{
			FCommand = ACommand;
			FCommand.SetActiveCursor(this);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FCommand != null)
				{
					try
					{
						FCommand.SetActiveCursor(null);
						FCommand.InternalClose();
					}
					finally
					{
						FCommand = null;
					}
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		protected SQLCommand FCommand;
		public SQLCommand Command { get { return FCommand; } }
		
		protected abstract SQLTableSchema InternalGetSchema();
		protected SQLTableSchema GetSchema()
		{
			try
			{
				return InternalGetSchema();
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "getschema", true);
				throw;
			}
		}
		
		protected SQLTableSchema FSchema;
		public SQLTableSchema Schema
		{
			get
			{
				if (FSchema == null)
					FSchema = GetSchema();
				return FSchema;
			}
		}
		
		protected abstract bool InternalNext();
		public bool Next()
		{
			try
			{
				return InternalNext();
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "next", true);
				throw;
			}
		}		

		protected abstract int InternalGetColumnCount();
		public int ColumnCount 
		{ 
			get
			{
				try
				{
					return InternalGetColumnCount();
				}
				catch (Exception LException)
				{
					FCommand.Connection.WrapException(LException, "getcolumncount", true);
					throw;
				}
			}
		}
		
		protected abstract object InternalGetColumnValue(int AIndex);
		public object this[int AIndex] 
		{ 
			get
			{
				try
				{
					return InternalGetColumnValue(AIndex);
				}
				catch (Exception LException)
				{
					FCommand.Connection.WrapException(LException, "getcolumnvalue", true);
					throw;
				}
			}
		}

		protected abstract bool InternalIsNull(int AIndex);
		public bool IsNull(int AIndex)
		{
			try
			{
				return InternalIsNull(AIndex);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "isnull", true);
				throw;
			}
		}
		
		protected abstract bool InternalIsDeferred(int AIndex);
		public bool IsDeferred(int AIndex)
		{
			try
			{
				return InternalIsDeferred(AIndex);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "isdeferred", true);
				throw;
			}
		}

		protected abstract Stream InternalOpenDeferredStream(int AIndex);
		public Stream OpenDeferredStream(int AIndex)
		{
			try
			{
				return InternalOpenDeferredStream(AIndex);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "opendeferredstream", true);
				throw;
			}
		}

		protected virtual bool InternalFindKey(object[] AKey)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public bool FindKey(object[] AKey)
		{
			try
			{
				return InternalFindKey(AKey);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "findkey", true);
				throw;
			}
		}

		protected virtual void InternalFindNearest(object[] AKey)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public void FindNearest(object[] AKey)
		{
			try
			{
				InternalFindNearest(AKey);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "findnearest", true);
				throw;
			}
		}
		
		protected virtual string InternalGetFilter()
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public string GetFilter()
		{
			try
			{
				return InternalGetFilter();
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "getfilter", true);
				throw;
			}
		}
		
		protected virtual bool InternalSetFilter(string AFilter)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public bool SetFilter(string AFilter)
		{
			try
			{
				return InternalSetFilter(AFilter);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "setfilter", true);
				throw;
			}
		}
		
		protected virtual void InternalInsert(string[] ANames, object[] AValues) 
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedUpdateableCall);
		}

		public void Insert(string[] ANames, object[] AValues) 
		{
			try
			{
				InternalInsert(ANames, AValues);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "insert", true);
				throw;
			}
		}

		protected virtual void InternalUpdate(string[] ANames, object[] AValues)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedUpdateableCall);
		}

		public void Update(string[] ANames, object[] AValues)
		{
			try
			{
				InternalUpdate(ANames, AValues);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "update", false);
			}
		}

		protected virtual void InternalDelete()
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedUpdateableCall);
		}

		public void Delete()
		{
			try
			{
				InternalDelete();
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "delete", false);
			}
		}
	}
}

