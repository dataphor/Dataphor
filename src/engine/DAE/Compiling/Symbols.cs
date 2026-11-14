/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Compiling
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
		public Symbols() : base() 
		{ 
			// Push an empty window onto the stack
			PushWindow(0);
		}
		
		public Symbols(int maxStackDepth, int maxCallDepth) : base(maxStackDepth, maxCallDepth) 
		{ 
			// Push an empty window onto the stack
			PushWindow(0);
		}
		
		public void SetIsModified(int offset)
		{
			#if DEBUG 
			int index = _count - 1 - offset;
			if ((index >= _count) || (!AllowExtraWindowAccess && (index < Base)))
				throw new BaseException(BaseException.Codes.InvalidStackIndex, offset.ToString());
			_stack[index].FIsModified = true;
			#else
			_stack[_count - 1 - offset].FIsModified = true;
			#endif
		}

		public bool IsValidVariableIdentifier(string identifier, List<string> names)
		{
			// Returns true if the given identifier is a valid identifier in the current stack window.
			// If the return value is false, ANames will contain the offending identifier.
			// This only validates top-level variable names.  It is legitimate to declare a row variable
			// that contains a column name that effectively hides a variable further down the stack.
			for (int index = _count - 1; index >= (AllowExtraWindowAccess ? 0 : Base); index--)
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(_stack[index].Name, AIdentifier) || Schema.Object.NamesEqual(AIdentifier, _stack[index].Name))
				{
					ANames.Add(_stack[index].Name);
					return false;
				}
				#else
				if (String.Compare(_stack[index].Name, identifier) == 0)
				{
					names.Add(_stack[index].Name);
					return false;
				}
				#endif
				
			}
			return true;
		}

		public int ResolveVariableIdentifier(string identifier, out int columnIndex, List<string> names)
		{
			columnIndex = -1;
			int variableIndex = -1;
			int rowBase = AllowExtraWindowAccess ? _windows.FrameRowBase : _windows.CurrentStackWindow.FrameRowBase;
			if (rowBase < 0)
				rowBase = _count;
			for (int index = _count - 1; index >= (AllowExtraWindowAccess ? 0 : Base); index--)
			{
				// if it's a row type check each of the columns
				if ((index >= rowBase) && (_stack[index].DataType is Schema.RowType))
				{
					columnIndex = ((Schema.RowType)_stack[index].DataType).Columns.IndexOf(identifier, names);
					if (columnIndex >= 0)
					{
						variableIndex = _count - 1 - index;
						break;
					}
					else
						if (names.Count > 0)
							break;
				}

				// check the object itself
				if (Schema.Object.NamesEqual(_stack[index].Name, identifier))
				{
					if (variableIndex >= 0)
					{
						names.Add(this[variableIndex].Name);
						names.Add(_stack[index].Name);
						variableIndex = -1;
						break;
					}
					variableIndex = _count - 1 - index;

					// If AllowExtraWindowAccess is true, we are binding a known good aggregate call, so allow variable hiding.
					if (AllowExtraWindowAccess)
						break;
				}
			}

			return variableIndex;
		}
		
		public bool HasCursorTypeVariables()
		{
			for (int index = _count - 1; index >= (AllowExtraWindowAccess ? 0 : Base); index--)
				if (_stack[index].DataType is Schema.ICursorType)
					return true;
			return false;
		}
	}
}
