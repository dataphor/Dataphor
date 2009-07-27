/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Alphora.Dataphor.DAE.Runtime
{
	/// <summary>
	/// Defines the structure for a compile-time symbol
	/// </summary>
	public struct Symbol
	{
		public Symbol(string AName, Schema.IDataType ADataType, bool AIsConstant, bool AIsModified)
		{
			FName = AName;
			FDataType = ADataType;
			FIsConstant = AIsConstant;
			FIsModified = AIsModified;
		}
		
		public Symbol(string AName, Schema.IDataType ADataType, bool AIsConstant)
		{
			FName = AName;
			FDataType = ADataType;
			FIsConstant = AIsConstant;
			FIsModified = false;
		}
		
		public Symbol(string AName, Schema.IDataType ADataType)
		{
			FName = AName;
			FDataType = ADataType;
			FIsConstant = false;
			FIsModified = false;
		}
		
		public Symbol(Schema.IDataType ADataType)
		{
			FName = String.Empty;
			FDataType = ADataType;
			FIsConstant = false;
			FIsModified = false;
		}
		
		public Symbol(Symbol ASymbol)
		{
			FName = ASymbol.Name;
			FDataType = ASymbol.DataType;
			FIsConstant = ASymbol.IsConstant;
			FIsModified = ASymbol.IsModified;
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private Schema.IDataType FDataType;
		public Schema.IDataType DataType { get { return FDataType; } }
		
		private bool FIsConstant;
		public bool IsConstant { get { return FIsConstant; } }
		
		internal bool FIsModified;
		public bool IsModified { get { return FIsModified; } }

		public override string ToString()
		{
			return 
				String.Format
				(
					"{0}{1} : {2}{3}", 
					IsConstant ? "const " : "", 
					Name == null ? "<unnamed>" : Name, 
					DataType == null ? "<untyped>" : DataType.Name, 
					IsModified ? " (modified)" : ""
				);
		}
		
		public static Symbol Empty = new Symbol(null, null, false, false);
	}

	public class Symbols : Stack<Symbol>
	{
		public Symbols() : base() { }
		public Symbols(int AMaxStackDepth, int AMaxCallDepth) : base(AMaxStackDepth, AMaxCallDepth) { }
		
		public void SetIsModified(int AOffset)
		{
			#if DEBUG 
			int LIndex = FCount - 1 - AOffset;
			if ((LIndex >= FCount) || (!AllowExtraWindowAccess && (LIndex < Base)))
				throw new RuntimeException(RuntimeException.Codes.InvalidStackIndex, AOffset.ToString());
			FStack[LIndex].FIsModified = true;
			#else
			FStack[FCount - 1 - AOffset].FIsModified = true;
			#endif
		}
		
		public bool IsValidVariableIdentifier(string AIdentifier, StringCollection ANames)
		{
			// Returns true if the given identifier is a valid identifier in the current stack window.
			// If the return value is false, ANames will contain the offending identifier.
			// This only validates top-level variable names.  It is legitimate to declare a row variable
			// that contains a column name that effectively hides a variable further down the stack.
			for (int LIndex = FCount - 1; LIndex >= (AllowExtraWindowAccess ? 0 : Base); LIndex--)
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(FStack[LIndex].Name, AIdentifier) || Schema.Object.NamesEqual(AIdentifier, FStack[LIndex].Name))
				{
					ANames.Add(FStack[LIndex].Name);
					return false;
				}
				#else
				if (String.Compare(FStack[LIndex].Name, AIdentifier) == 0)
				{
					ANames.Add(FStack[LIndex].Name);
					return false;
				}
				#endif
				
			}
			return true;
		}
		
		public int ResolveVariableIdentifier(string AIdentifier, out int AColumnIndex, StringCollection ANames)
		{
			AColumnIndex = -1;
			int LVariableIndex = -1;
			int LRowBase = AllowExtraWindowAccess ? FWindows.FrameRowBase : FWindows.CurrentStackWindow.FrameRowBase;
			if (LRowBase < 0)
				LRowBase = FCount;
			for (int LIndex = FCount - 1; LIndex >= (AllowExtraWindowAccess ? 0 : Base); LIndex--)
			{
				// if it's a row type check each of the columns
				if ((LIndex >= LRowBase) && (FStack[LIndex].DataType is Schema.RowType))
				{
					AColumnIndex = ((Schema.RowType)FStack[LIndex].DataType).Columns.IndexOf(AIdentifier, ANames);
					if (AColumnIndex >= 0)
					{
						LVariableIndex = FCount - 1 - LIndex;
						break;
					}
					else
						if (ANames.Count > 0)
							break;
				}

				// check the object itself
				if (Schema.Object.NamesEqual(FStack[LIndex].Name, AIdentifier))
				{
					if (LVariableIndex >= 0)
					{
						ANames.Add(this[LVariableIndex].Name);
						ANames.Add(FStack[LIndex].Name);
						LVariableIndex = -1;
						break;
					}
					LVariableIndex = FCount - 1 - LIndex;

					// If AllowExtraWindowAccess is true, we are binding a known good aggregate call, so allow variable hiding.
					if (AllowExtraWindowAccess)
						break;
				}
			}

			return LVariableIndex;
		}
		
		public bool HasCursorTypeVariables()
		{
			for (int LIndex = FCount - 1; LIndex >= (AllowExtraWindowAccess ? 0 : Base); LIndex--)
				if (FStack[LIndex].DataType is Schema.ICursorType)
					return true;
			return false;
		}
	}
}
