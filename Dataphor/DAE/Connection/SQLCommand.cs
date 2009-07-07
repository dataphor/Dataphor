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
		
		public const int CDefaultCommandTimeout = -1;
		private int FCommandTimeout = CDefaultCommandTimeout;
		/// <summary>The amount of time to wait before timing out when waiting for a command to execute, expressed in seconds.</summary>
		/// <remarks>The default value for this property is 30 seconds. A value of 0 indicates an infinite timeout.</remarks>
		public int CommandTimeout
		{
			get { return FCommandTimeout; }
			set { FCommandTimeout = value; }
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
		
		public object Evaluate()
		{
			SQLCursor LCursor = Open(SQLCursorType.Dynamic, SQLIsolationLevel.Serializable);
			try
			{
				if (LCursor.Next())
					return LCursor[0];
				return null;
			}
			finally
			{
				Close(LCursor);
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
		
		private bool FShouldNormalizeWhitespace = true;
		public bool ShouldNormalizeWhitespace
		{
			get { return FShouldNormalizeWhitespace; }
			set { FShouldNormalizeWhitespace = value; }
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
						LInString = !LInString;
						break;
				}
					
				if (!LInParameter)
					LResult.Append((Char.IsWhiteSpace(AStatement[LIndex]) && FShouldNormalizeWhitespace) ? ' ' : AStatement[LIndex]);
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
}
