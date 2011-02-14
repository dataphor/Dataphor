/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;

	// operator iIndexer(const AString : string, const AIndex : integer) : string
	public class StringIndexerNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif
			
			string stringValue = (string)argument1;
			int index = (int)argument2;
			
			if ((index < 0) || (index >= stringValue.Length))
				return String.Empty;
			
			return Convert.ToString(stringValue[index]);
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			IndexerExpression expression = new D4IndexerExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			expression.Indexer = (Expression)Nodes[1].EmitStatement(mode);
			expression.Modifiers = Modifiers;
			return expression;
		}
	}

	// operator Length(AString : System.String) : System.Integer
	public class StringLengthNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ((string)argument1).Length;
		}
	}
	
	// operator SubString(AString : System.String, AStart : System.String) : System.String
	public class StringSubStringNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif
			
			string stringValue = (string)argument1;
			int startIndex = (int)argument2;
			if (startIndex > stringValue.Length)
				startIndex = stringValue.Length;
			else if (startIndex < 0)
				startIndex = 0;
			return stringValue.Substring(startIndex, stringValue.Length - startIndex);
		}
	}
	
	// operator SubString(AString : System.String, AStart : System.String, ALength : System.Integer) : System.String
	public class StringSubStringTernaryNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null || argument3 == null)
				return null;
			#endif
			
			string stringValue = (string)argument1;
			int startIndex = (int)argument2;
			if (startIndex > stringValue.Length)
				startIndex = stringValue.Length;
			else if (startIndex < 0)
				startIndex = 0;

			int length = (int)argument3;
			if ((startIndex + length) > stringValue.Length)
				length = stringValue.Length - startIndex;
				
			return stringValue.Substring(startIndex, length);
		}
	}
	
	// operator Pos(ASubString : System.String, AString : System.String) : System.Integer
	public class StringPosNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return StringUtility.IndexOf((string)argument2, (string)argument1);
		}
	}

	#if USEISTRING	
	public class IStringPosNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return ((string)AArgument2).ToUpper().IndexOf(((string)AArgument1).ToUpper());
		}
	}
	#endif
	
	// operator PadLeft(AString : System.String, ATotalLength : System.Integer)
	// operator PadLeft(AString : System.String, ATotalLength : System.Integer, APadChar : System.String)
	public class StringPadLeftNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null || ((arguments.Length == 3) && (arguments[2] == null)))
				return null;
			#endif

			if (arguments.Length == 3)
				return ((string)arguments[0]).PadLeft((int)arguments[1], ((string)arguments[2])[0]);
			else
				return ((string)arguments[0]).PadLeft((int)arguments[1]);
		}
	}
	
	// operator PadRight(AString : System.String, ATotalLength : System.Integer)
	// operator PadRight(AString : System.String, ATotalLength : System.Integer, APadChar : System.String)
	public class StringPadRightNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null || ((arguments.Length == 3) && (arguments[2] == null)))
				return null;
			#endif

			if (arguments.Length == 3)
				return ((string)arguments[0]).PadRight((int)arguments[1], ((string)arguments[2])[0]);
			else
				return ((string)arguments[0]).PadRight((int)arguments[1]);
		}
	}

	// operator Insert(AString : System.String, AStartIndex : System.Integer, AInsertString : System.String) : System.String
	public class StringInsertNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null || argument3 == null)
				return null;
			#endif
			
			string stringValue = (string)argument1;
			int startIndex = (int)argument2;
			if (startIndex > stringValue.Length)
				startIndex = stringValue.Length;
			else if (startIndex < 0)
				startIndex = 0;
				
			return stringValue.Insert(startIndex, (string)argument3);
		}
	}
	
	// operator Remove(AString : System.String, AStartIndex : System.Integer, ALength : System.Integer) : System.String
	public class StringRemoveNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null || argument3 == null)
				return null;
			#endif
			
			string stringValue = (string)argument1;

			int startIndex = (int)argument2;
			if (startIndex > stringValue.Length)
				startIndex = stringValue.Length;
			else if (startIndex < 0)
				startIndex = 0;

			int length = (int)argument3;
			if ((startIndex + length) > stringValue.Length)
				length = stringValue.Length - startIndex;

			return stringValue.Remove(startIndex, length);
		}
	}
	
	// operator Split(const AString : String) : list(String);
	// operator Split(const AString : String, const ADelimiter : String) : list(String);
	// operator Split(const AString : String, const ADelimiters : list(String)) : list(String);
	public class StringSplitNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || ((arguments.Length > 1) && (arguments[1] == null)))
				return null;
			#endif
			
			string stringValue = (string)arguments[0];
			string[] delimiters;
			if (arguments.Length == 1)
				delimiters = new string[]{",", ";"};
			else if (Operator.Operands[1].DataType is Schema.ListType)
			{
				ListValue delimiterList = (ListValue)arguments[1];
				delimiters = new string[delimiterList.Count()];
				for (int index = 0; index < delimiterList.Count(); index++)
				{
					#if NILPROPOGATION
					if (delimiterList[index] == null)
						return null;
					#endif

					delimiters[index] = (string)delimiterList[index];
				}
			}
			else
				delimiters = new string[]{(string)arguments[1]};
				
			ListValue tempValue = new ListValue(program.ValueManager, (Schema.ListType)_dataType);
			
			int start = 0;
			int first = 0;
			int delimeterLength = 0;
			do
			{
				start = first + delimeterLength;
				first = -1;
				delimeterLength = 0;
				for (int i = 0; i < delimiters.Length; i++)
				{
					int index = StringUtility.IndexOf(stringValue, delimiters[i], start);
					if ((index >= 0) && ((first < 0) || (index < first)))
					{
						first = index;
						delimeterLength = delimiters[i].Length;
					}
				}
				tempValue.Add(stringValue.Substring(start, (first < 0 ? stringValue.Length : first) - start));
			} while ((first >= 0) && (((first - start) + delimeterLength) > 0));
			
			return tempValue;
		}
	}
	
	// operator Concat(const AStrings : list(String)) : String;
	// operator Concat(const AStrings : list(String), const ADelimiter : String) : String;
	public class StringConcatNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || ((arguments.Length > 1) && (arguments[1] == null)))
				return null;
			#endif

			string delimiter = arguments.Length > 1 ? (string)arguments[1] : "";			
			StringBuilder result = new StringBuilder();
			ListValue strings = (ListValue)arguments[0];
			for (int index = 0; index < strings.Count(); index++)
			{
				#if NILPROPOGATION
				if (strings[index] == null)
					return null;
				#endif
				
				if (index > 0)
					result.Append(delimiter);
				result.Append(strings[index]);
			}

			return result.ToString();
		}
	}
	
	// operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String) : System.String
    // operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String, ACaseSensitive : System.Boolean) : System.String
    public class StringReplaceNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || (arguments[1] == null) || (arguments[2] == null) || ((arguments.Length == 4) && (arguments[3] == null)))
				return null;
			#endif
            if ((arguments.Length == 3) || ((arguments.Length) == 4 && (bool)arguments[3]))
				return StringUtility.Replace((string)arguments[0], (string)arguments[1], (string)arguments[2]);
            else
            {
                string stringValue = (string)arguments[0];
			    string oldString = ((string)arguments[1]).ToUpper();
			    if (oldString.Length > 0)
			    {
				    string newString = (string)arguments[2];
				    int currentIndex = stringValue.ToUpper().IndexOf(oldString);
				    while ((currentIndex >= 0) && (currentIndex < stringValue.Length))
				    {
					    stringValue = stringValue.Remove(currentIndex, Math.Min(oldString.Length, stringValue.Length - currentIndex)).Insert(currentIndex, newString);
					    currentIndex = stringValue.ToUpper().IndexOf(oldString, currentIndex + newString.Length);
				    }
			    }
			    return stringValue;
            }
        }
	}
	
	#if USEISTRING
	// operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String) : System.String
	public class IStringReplaceNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null)
				return null;
			#endif

			string LString = (string)AArguments[0];
			string LOldString = (string)AArguments[1].ToUpper();
			if (LOldString.Length > 0)
			{
				string LNewString = (string)AArguments[2];
				int LCurrentIndex = LString.ToUpper().IndexOf(LOldString);
				while ((LCurrentIndex >= 0) && (LCurrentIndex < LString.Length))
				{
					LString = LString.Remove(LCurrentIndex, Math.Min(LOldString.Length, LString.Length - LCurrentIndex)).Insert(LCurrentIndex, LNewString);
					LCurrentIndex = LString.ToUpper().IndexOf(LOldString, LCurrentIndex + LNewString.Length);
				}
			}
			return LString;
		}
	}
	#endif
	
	// operator Trim(AString : System.String) : System.String
	public class StringTrimNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ((string)argument1).Trim();
		}
	}
	
	// operator TrimLeft(AString : System.String) : System.String
	public class StringTrimLeftNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ((string)argument1).TrimStart(null);
		}
	}
	
	// operator TrimRight(AString : System.String) : System.String
	public class StringTrimRightNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ((string)argument1).TrimEnd(null);
		}
	}
	
	// operator LastPos(ASubString : System.String, AString : System.String) : System.Integer
	public class StringLastPosNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return StringUtility.LastIndexOf((string)argument2, (string)argument1);
		}
	}
	
	#if USEISTRING
	public class IStringLastPosNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return (string)AArgument2.ToUpper().LastIndexOf((string)AArgument1.ToUpper());
		}
	}
	#endif
	
	public class StringIndexOfNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return StringUtility.IndexOf((string)argument1, (string)argument2);
		}
	}

	public class StringIndexOfStartNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null || arguments[2] == null)
				return null;
			#endif

			string stringValue = (string)arguments[0];
			int result;
			if (stringValue == String.Empty)
				result = -1;
			else
			{
				int startIndex = (int)arguments[2];
				if (startIndex > stringValue.Length)
					startIndex = stringValue.Length;
				else if (startIndex < 0)
					startIndex = 0;
				result = StringUtility.IndexOf(stringValue, (string)arguments[1], startIndex);
			}
			return result;
		}
	}

	public class StringIndexOfStartLengthNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null || arguments[2] == null || arguments[3] == null)
				return null;
			#endif

			string stringValue = (string)arguments[0];
			int startIndex = (int)arguments[2];
			int length = (int)arguments[3];
			int result;
			if ((stringValue == String.Empty) || (startIndex >= stringValue.Length))
			{
				if (length < 0)
					throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
				result = -1;
			}
			else
			{
				if (startIndex < 0)
					startIndex = 0;
				if ((startIndex + length) > stringValue.Length)
					length = stringValue.Length - startIndex;
				result = StringUtility.IndexOf(stringValue, (string)arguments[1], startIndex, length);
			}
			return result;
		}
	}

	public class StringStartsWith : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return ((string)argument1).StartsWith((string)argument2);
		}
	}
	
	public class StringEndsWith : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return ((string)argument1).EndsWith((string)argument2);
		}
	}
	
	#if USEISTRING
	public class IStringIndexOfNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return ((string)AArgument1).ToUpper().IndexOf((string)AArgument2.ToUpper());
		}
	}
	#endif
	
	public class StringIndexOfAnyNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null || ((arguments.Length > 2) && (arguments[2] == null)) || ((arguments.Length > 3) && (arguments[3] == null)))
				return null;
			#endif

			string stringValue = (string)arguments[0];

			int startIndex;
			if (arguments.Length > 2)
			{
				startIndex = (int)arguments[2];
				if (startIndex < 0)
					startIndex = 0;
			}
			else
				startIndex = 0;
			
			int length;
			if (arguments.Length > 3)
			{
				length = (int)arguments[3];
				if (length < 0)
					throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
				if ((startIndex + length) > stringValue.Length)
					length = stringValue.Length - startIndex;
			}
			else
				length = stringValue.Length - startIndex;
			
			int result = -1;
			if ((length != 0) && (stringValue != String.Empty) && (startIndex < stringValue.Length))
			{
				ListValue anyOf = (ListValue)arguments[1];
				for (int index = 0; index < anyOf.Count(); index++)
				{
					#if NILPROPOGATION
					if (anyOf[index] == null)
						return null;
					#endif

					int indexOf = StringUtility.IndexOf(stringValue, (string)anyOf[index], startIndex, length);
					if ((indexOf >= 0) && ((result < 0) || (indexOf < result)))
						result = indexOf;
				}
			}			

			return result;
		}
	}
	
	#if USEISTRING
	public class IStringIndexOfAnyNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif

			int LIndexOf = -1;
			string LString = ((string)AArguments[0]).ToUpper();
			ListValue LAnyOf = (ListValue)AArguments[1];
			for (int LIndex = 0; LIndex < LAnyOf.Length(); LIndex++)
			{
				#if NILPROPOGATION
				if ((LAnyOf[LIndex] == null))
					return null;
				#endif

				LIndexOf = LString.IndexOf(LAnyOf[LIndex].AsString.ToUpper());
				if (LIndexOf >= 0)
					break;
			}
			
			return LIndexOf;
		}
	}
	#endif
	
	public class StringLastIndexOfNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return StringUtility.LastIndexOf((string)argument1, (string)argument2);
		}
	}

	public class StringLastIndexOfStartNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null || argument3 == null)
				return null;
			#endif

			string stringValue = (string)argument1;
			int result;
			if (stringValue == String.Empty)
				result = -1;
			else
			{
				int startIndex = (int)argument3;
				if (startIndex > (stringValue.Length - 1))
					startIndex = (stringValue.Length - 1);
				else if (startIndex < -1)
					startIndex = -1;
				result = StringUtility.LastIndexOf(stringValue, (string)argument2, startIndex);
			}
			return result;
		}
	}

	public class StringLastIndexOfStartLengthNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null || arguments[2] == null || arguments[3] == null)
				return null;
			#endif

			string stringValue = (string)arguments[0];
			int startIndex = (int)arguments[2];
			int length = (int)arguments[3];
			int result;
			if ((startIndex < 0) || (stringValue == String.Empty))
			{
				if (length < 0)
					throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
				result = -1;
			}
			else
			{
				if (startIndex > (stringValue.Length - 1))
					startIndex = (stringValue.Length - 1);
				if ((startIndex - length) < -1)
					length = startIndex + 1;
				result = StringUtility.LastIndexOf(stringValue, (string)arguments[1], startIndex, length);	// will throw if ALength < 0
			}
			return result;
		}
	}

	#if USEISTRING
	public class IStringLastIndexOfNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return ((string)AArgument1).ToUpper().LastIndexOf(((string)AArgument2).ToUpper());
		}
	}
	#endif
	
	public class StringLastIndexOfAnyNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null || ((arguments.Length > 2) && (arguments[2] == null)) || ((arguments.Length > 3) && (arguments[3] == null)))
				return null;
			#endif

			string stringValue = (string)arguments[0];

			int startIndex;
			if (arguments.Length > 2)
			{
				startIndex = (int)arguments[2];
				if (startIndex >= stringValue.Length)
					startIndex = stringValue.Length - 1;
			}
			else
				startIndex = stringValue.Length - 1;
			
			int length;
			if (arguments.Length > 3)
			{
				length = (int)arguments[3];
				if (length < 0)
					throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
				if ((startIndex - length) < -1)
					length = startIndex + 1;
			}
			else
				length = startIndex + 1;
			
			int result = -1;
			if ((length != 0) && (stringValue != String.Empty) && (startIndex >= 0))
			{
				ListValue anyOf = (ListValue)arguments[1];
				for (int index = 0; index < anyOf.Count(); index++)
				{
					#if NILPROPOGATION
					if (anyOf[index] == null)
						return null;
					#endif

					int indexOf = StringUtility.LastIndexOf(stringValue, (string)anyOf[index], startIndex, length);
					if ((indexOf >= 0) && ((result < 0) || (indexOf > result)))
						result = indexOf;
				}
			}

			return result;
		}
	}
	
	#if USEISTRING
	public class IStringLastIndexOfAnyNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif

			int LLastIndexOf = -1;
			string LString = (string)AArguments[0].ToUpper();
			ListValue LAnyOf = (ListValue)AArguments[1];
			for (int LIndex = 0; LIndex < LAnyOf.Count(); LIndex++)
			{
				#if NILPROPOGATION
				if (LAnyOf[LIndex] == null)
					return null;
				#endif

				LLastIndexOf = LString.LastIndexOf(LAnyOf[LIndex].AsString.ToUpper());
				if (LLastIndexOf >= 0)
					break;
			}
			
			return LLastIndexOf;
		}
	}
	#endif
	
	// operator CountOf(AString : System.String, ASubString : System.String) : System.Integer
	public class StringCountOfNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			int found = 0;
			int totalFinds = 0;
			string stringValue = (string)argument1;
			string subString = (string)argument2;

			for (int i = 0; i < stringValue.Length; i++) 
			{
				found = StringUtility.IndexOf(stringValue, subString, i);
				if (found >= 0) 
				{
					totalFinds++;
					i = found;
				}
				else
					break;
			}
			return totalFinds;
		}
	}

	// operator Upper(string) : string;
	public class StringUpperNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ((string)argument1).ToUpper();
		}
	}
	
	// operator Lower(string) : string;
	public class StringLowerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ((string)argument1).ToLower();
		}
	}
	
	// operator iLike(string, string) : boolean
	public abstract class StringLikeNodeBase : BinaryInstructionNode
	{
		protected const int RegexCacheSize = 20;
		protected static FixedSizeCache<string, Regex> _regexCache = new FixedSizeCache<string, Regex>(RegexCacheSize);

		public static Regex GetRegex(string pattern)
		{
			lock (_regexCache)
			{
				Regex regex;
				if (!_regexCache.TryGetValue(pattern, out regex))
				{
					regex = new Regex(pattern, RegexOptions.None);
					_regexCache.Add(pattern, regex);
				}
				
				return regex;
			}
		}
		
		protected string TransformString(string input)
		{
			IList regexChars = (IList)new char[]{'.', '$', '{', '[', '(', '|', ')', '*', '+', '?'};
			StringBuilder result = new StringBuilder();
			int index = 0;
			bool isRegexChar = false;
			while (index < input.Length)
			{
				switch (input[index])
				{
					case '_': result.Append('.'); isRegexChar = false; break;
					case '%': result.Append(".*"); isRegexChar = true; break;
					case '\\':
						index++;
						if (index == input.Length)
							result.Append('\\');
						else
						{
							if (!((input[index] == '_') || (input[index] == '%')))
								result.Append('\\');
							result.Append(input[index]);
						}
						isRegexChar = false;
					break;
					
					default:
						if (regexChars.Contains(input[index]))
							result.Append('\\');
						result.Append(input[index]);
						isRegexChar = false;
					break;
				}
				index++;
			}
			if (!isRegexChar)
				result.Append("$"); // If the end of the pattern is not a regex operator, force the match at the end of the string
			return result.ToString();
		}
	}
	
	public class StringLikeNode : StringLikeNodeBase
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			string stringValue = (string)argument1;
			string pattern = TransformString((string)argument2);
			Regex regex = GetRegex(pattern);
			Match match = regex.Match(stringValue);
			return match.Success && (match.Index == 0);
		}
	}
	
	#if USEISTRING
	public class IStringLikeNode : StringLikeNodeBase
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			string LString = (string)AArgument1;
			string LPattern = TransformString((string)AArgument2);
			Match LMatch = Regex.Match(LString, LPattern, RegexOptions.IgnoreCase);
			return LMatch.Success && (LMatch.Index == 0);
		}
	}
	#endif
	
	// operator iMatches(string, string) : boolean
	public class StringMatchesNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return StringLikeNodeBase.GetRegex((string)argument2).IsMatch((string)argument1);
		}
	}
	
	#if USEISTRING
	public class IStringMatchesNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return Regex.IsMatch((string)AArgument1, (string)AArgument2, RegexOptions.IgnoreCase);
		}
	}
	#endif
	
	// operator CompareText(string, string) : integer
	public class StringCompareTextNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return String.Compare((string)argument1, (string)argument2, StringComparison.OrdinalIgnoreCase);
		}
	}
	
	// Unicode representation
	// operator System.String.Unicode(const AUnicode : list(System.Integer)) : System.String;
	public class SystemStringUnicodeNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			{
				ListValue list = (ListValue)argument1;
				byte[] encodedValue = new byte[list.Count() * 2];
				for (int index = 0; index < list.Count(); index++)
				{
					#if NILPROPOGATION
					if (list[index] == null)
						return null;
					#endif
					encodedValue[index * 2] = (byte)((int)list[index]);
					encodedValue[index * 2 + 1] = (byte)((int)list[index] >> 8);
				}
				Decoder decoder = UnicodeEncoding.Unicode.GetDecoder();
				char[] decodedValue = new char[decoder.GetCharCount(encodedValue, 0, encodedValue.Length)];
				decoder.GetChars(encodedValue, 0, encodedValue.Length, decodedValue, 0);
				return new String(decodedValue);
			}
		}
	}
	
	// operator System.String.ReadUnicode(const AValue : System.String) : list(Sytem.Integer);
	public class SystemStringReadUnicodeNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			{
				string stringValue = (string)argument1;
				char[] decodedValue = new char[stringValue.Length];
				stringValue.CopyTo(0, decodedValue, 0, stringValue.Length);
				Encoder encoder = UnicodeEncoding.Unicode.GetEncoder();
				byte[] encodedValue = new byte[encoder.GetByteCount(decodedValue, 0, decodedValue.Length, false)];
				encoder.GetBytes(decodedValue, 0, decodedValue.Length, encodedValue, 0, true);
				ListValue listValue = new ListValue(program.ValueManager, (Schema.ListType)_dataType);
				for (int index = 0; index < encodedValue.Length; index++)
					if ((index % 2) == 1)
						listValue.Add((encodedValue[index - 1]) + (encodedValue[index] << 8));
				return listValue;
			}
		}
	}
	
	// operator System.String.WriteUnicode(const AValue : System.String, const AUnicode : list(System.Integer)) : System.String;
	public class SystemStringWriteUnicodeNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif

			{
				ListValue list = (ListValue)argument2;
				byte[] encodedValue = new byte[list.Count() * 2];
				for (int index = 0; index < list.Count(); index++)
				{
					#if NILPROPOGATION
					if (list[index] == null)
						return null;
					#endif
					encodedValue[index * 2] = (byte)((int)list[index]);
					encodedValue[index * 2 + 1] = (byte)((int)list[index] >> 8);
				}
				Decoder decoder = UnicodeEncoding.Unicode.GetDecoder();
				char[] decodedValue = new char[decoder.GetCharCount(encodedValue, 0, encodedValue.Length)];
				decoder.GetChars(encodedValue, 0, encodedValue.Length, decodedValue, 0);
				return new String(decodedValue);
			}
		}
	}
	
	// UTF8 representation
	// operator System.String.UTF8(const AUTF8 : list(System.Byte)) : System.String;
	public class SystemStringUTF8Node : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			{
				ListValue list = (ListValue)argument1;
				byte[] encodedValue = new byte[list.Count()];
				for (int index = 0; index < list.Count(); index++)
				{
					#if NILPROPOGATION
					if (list[index] == null)
						return null;
					#endif
					encodedValue[index] = (byte)list[index];
				}
				Decoder decoder = UnicodeEncoding.UTF8.GetDecoder();
				char[] decodedValue = new char[decoder.GetCharCount(encodedValue, 0, encodedValue.Length)];
				decoder.GetChars(encodedValue, 0, encodedValue.Length, decodedValue, 0);
				return new String(decodedValue);
			}
		}
	}
	
	// operator System.String.ReadUTF8(const AValue : System.String) : list(System.Byte);
	public class SystemStringReadUTF8Node : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			{
				string stringValue = (string)argument1;
				char[] decodedValue = new char[stringValue.Length];
				stringValue.CopyTo(0, decodedValue, 0, stringValue.Length);
				Encoder encoder = UnicodeEncoding.UTF8.GetEncoder();
				byte[] encodedValue = new byte[encoder.GetByteCount(decodedValue, 0, decodedValue.Length, false)];
				encoder.GetBytes(decodedValue, 0, decodedValue.Length, encodedValue, 0, true);
				ListValue listValue = new ListValue(program.ValueManager, (Schema.ListType)_dataType);
				for (int index = 0; index < decodedValue.Length; index++)
					listValue.Add(encodedValue[index]);
				return listValue;
			}
		}
	}
	
	// operator System.String.WriteUTF8(const AValue : System.String, const AUTF8 : list(System.Byte)) : System.String;
	public class SystemStringWriteUTF8Node : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif

			{
				ListValue list = (ListValue)argument2;
				byte[] encodedValue = new byte[list.Count()];
				for (int index = 0; index < list.Count(); index++)
				{
					#if NILPROPOGATION
					if (list[index] == null)
						return null;
					#endif
					encodedValue[index] = (byte)list[index];
				}
				Decoder decoder = UnicodeEncoding.UTF8.GetDecoder();
				char[] decodedValue = new char[decoder.GetCharCount(encodedValue, 0, encodedValue.Length)];
				decoder.GetChars(encodedValue, 0, encodedValue.Length, decodedValue, 0);
				return new String(decodedValue);
			}
		}
	}
	
	// ASCII representation
	// operator System.String.ASCII(const AASCII : list(System.Byte)) : System.String;
	public class SystemStringASCIINode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			{
				ListValue list = (ListValue)argument1;
				byte[] encodedValue = new byte[list.Count()];
				for (int index = 0; index < list.Count(); index++)
				{
					#if NILPROPOGATION
					if (list[index] == null)
						return null;
					#endif
					encodedValue[index] = (byte)list[index];
				}
				#if SILVERLIGHT
				Decoder decoder = UnicodeEncoding.UTF8.GetDecoder();
				#else
				Decoder decoder = UnicodeEncoding.ASCII.GetDecoder();
				#endif
				char[] decodedValue = new char[decoder.GetCharCount(encodedValue, 0, encodedValue.Length)];
				decoder.GetChars(encodedValue, 0, encodedValue.Length, decodedValue, 0);
				return new String(decodedValue);
			}
		}
	}
	
	// operator System.String.ReadASCII(const AValue : System.String) : list(System.Byte);
	public class SystemStringReadASCIINode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			{
				string stringValue = (string)argument1;
				char[] decodedValue = new char[stringValue.Length];
				stringValue.CopyTo(0, decodedValue, 0, stringValue.Length);
				#if SILVERLIGHT
				Encoder encoder = UnicodeEncoding.UTF8.GetEncoder();
				#else
				Encoder encoder = UnicodeEncoding.ASCII.GetEncoder();
				#endif
				byte[] encodedValue = new byte[encoder.GetByteCount(decodedValue, 0, decodedValue.Length, false)];
				encoder.GetBytes(decodedValue, 0, decodedValue.Length, encodedValue, 0, true);
				ListValue listValue = new ListValue(program.ValueManager, (Schema.ListType)_dataType);
				for (int index = 0; index < decodedValue.Length; index++)
					listValue.Add(encodedValue[index]);
				return listValue;
			}
		}
	}
	
	// operator System.String.WriteASCII(const AValue : System.String, const AASCII : list(System.Byte)) : System.String;
	public class SystemStringWriteASCIINode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif

			{
				ListValue list = (ListValue)argument2;
				byte[] encodedValue = new byte[list.Count()];
				for (int index = 0; index < list.Count(); index++)
				{
					#if NILPROPOGATION
					if (list[index] == null)
						return null;
					#endif
					encodedValue[index] = (byte)list[index];
				}
				#if SILVERLIGHT
				Decoder decoder = UnicodeEncoding.UTF8.GetDecoder();
				#else
				Decoder decoder = UnicodeEncoding.ASCII.GetDecoder();
				#endif
				char[] decodedValue = new char[decoder.GetCharCount(encodedValue, 0, encodedValue.Length)];
				decoder.GetChars(encodedValue, 0, encodedValue.Length, decodedValue, 0);
				return new String(decodedValue);
			}
		}
	}
	
	// operator System.IsUpper(const AValue : String) : Boolean;
	// operator System.IsUpper(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsUpperNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 1)
			{
				#if NILPROPOGATION
				if (arguments[0] == null)
					return null;
				#endif
				
				string tempValue = (string)arguments[0];
				bool isUpper = true;
				for (int index = 0; index < tempValue.Length; index++)
					if (Char.IsLetter(tempValue, index) && !Char.IsUpper(tempValue, index))
					{
						isUpper = false;
						break;
					}
				
				return isUpper;
			}
			else
			{
				#if NILPROPOGATION
				if (arguments[0] == null || arguments[1] == null)
					return null;
				#endif
				
				string stringValue = (string)arguments[0];
				int index = (int)arguments[1];
				if ((index < 0) || (index >= stringValue.Length))
					return false;

				return Char.IsUpper(stringValue, index);
			}
		}
	}

	// operator System.IsLower(const AValue : String) : Boolean;	
	// operator System.IsLower(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLowerNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 1)
			{
				#if NILPROPOGATION
				if (arguments[0] == null)
					return null;
				#endif

				string tempValue = (string)arguments[0];
				bool isLower = true;
				for (int index = 0; index < tempValue.Length; index++)
					if (Char.IsLetter(tempValue, index) && !Char.IsLower(tempValue, index))
					{
						isLower = false;
						break;
					}
					
				return isLower;	
			}
			else
			{
				#if NILPROPOGATION
				if (arguments[0] == null || arguments[1] == null)
					return null;
				#endif

				string stringValue = (string)arguments[0];
				int index = (int)arguments[1];
				if ((index < 0) || (index >= stringValue.Length))
					return false;

				return Char.IsLower(stringValue, index);
			}
		}
	}
	
	// operator System.IsLetter(const AValue : String) : Boolean;	
	// operator System.IsLetter(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLetterNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 1)
			{
				#if NILPROPOGATION
				if (arguments[0] == null)
					return null;
				#endif

				string tempValue = (string)arguments[0];
				bool isLetter = true;
				for (int index = 0; index < tempValue.Length; index++)
					if (!Char.IsLetter(tempValue, index))
					{
						isLetter = false;
						break;
					}
					
				return isLetter;	
			}
			else
			{
				#if NILPROPOGATION
				if (arguments[0] == null || arguments[1] == null)
					return null;
				#endif

				string stringValue = (string)arguments[0];
				int index = (int)arguments[1];
				if ((index < 0) || (index >= stringValue.Length))
					return false;

				return Char.IsLetter(stringValue, index);
			}
		}
	}
	
	// operator System.IsDigit(const AValue : String) : Boolean;	
	// operator System.IsDigit(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsDigitNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 1)
			{
				#if NILPROPOGATION
				if (arguments[0] == null)
					return null;
				#endif

				string tempValue = (string)arguments[0];
				bool isDigit = true;
				for (int index = 0; index < tempValue.Length; index++)
					if (!Char.IsDigit(tempValue, index))
					{
						isDigit = false;
						break;
					}
					
				return isDigit;	
			}
			else
			{
				#if NILPROPOGATION
				if (arguments[0] == null || arguments[1] == null)
					return null;
				#endif

				string stringValue = (string)arguments[0];
				int index = (int)arguments[1];
				if ((index < 0) || (index >= stringValue.Length))
					return false;

				return Char.IsDigit(stringValue, index);
			}
		}
	}
	
	// operator System.IsLetterOrDigit(const AValue : String) : Boolean;	
	// operator System.IsLetterOrDigit(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLetterOrDigitNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 1)
			{
				#if NILPROPOGATION
				if (arguments[0] == null)
					return null;
				#endif

				string tempValue = (string)arguments[0];
				bool isLetterOrDigit = true;
				for (int index = 0; index < tempValue.Length; index++)
					if (!Char.IsLetterOrDigit(tempValue, index))
					{
						isLetterOrDigit = false;
						break;
					}
					
				return isLetterOrDigit;	
			}
			else
			{
				#if NILPROPOGATION
				if (arguments[0] == null || arguments[1] == null)
					return null;
				#endif

				string stringValue = (string)arguments[0];
				int index = (int)arguments[1];
				if ((index < 0) || (index >= stringValue.Length))
					return false;

				return Char.IsLetterOrDigit(stringValue, index);
			}
		}
	}
	
	#if USEISTRING
	// operator System.EnsureUpper(var AValue : IString);
	public class IStringEnsureUpperNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value != null) && !AArguments[0].Value.IsNil)
				AArguments[0].Value = new Scalar(AProgram.ValueManager, (Schema.ScalarType)FDataType, (string)AArguments[0].ToUpper());
			return null;
		}
	}
	
	// operator System.EnsureLower(var AValue : IString);
	public class IStringEnsureLowerNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value != null) && !AArguments[0].Value.IsNil)
				AArguments[0].Value = new Scalar(AProgram.ValueManager, (Schema.ScalarType)FDataType, (string)AArguments[0].ToLower());
			return null;
		}
	}
	#endif

	/// <remarks> The string searching routines in the StringUtility class treat the empty string as though it does not exist in any string.  The standard .NET routines treat the empty string as though it exists in all strings. </remarks>
	public sealed class StringUtility
	{
		public static int IndexOf(string stringValue, string searchFor)
		{
			return (searchFor == String.Empty ? -1 : stringValue.IndexOf(searchFor));
		}

		public static int IndexOf(string stringValue, string searchFor, int startIndex)
		{
			return (searchFor == String.Empty ? -1 : stringValue.IndexOf(searchFor, startIndex));
		}

		public static int IndexOf(string stringValue, string searchFor, int startIndex, int length)
		{
			return (searchFor == String.Empty ? -1 : stringValue.IndexOf(searchFor, startIndex, length));
		}

		public static int LastIndexOf(string stringValue, string searchFor)
		{
			return (searchFor == String.Empty ? -1 : stringValue.LastIndexOf(searchFor));
		}

		public static int LastIndexOf(string stringValue, string searchFor, int startIndex)
		{
			return (searchFor == String.Empty ? -1 : stringValue.LastIndexOf(searchFor, startIndex));
		}

		public static int LastIndexOf(string stringValue, string searchFor, int startIndex, int length)
		{
			return (searchFor == String.Empty ? -1 : stringValue.LastIndexOf(searchFor, startIndex, length));
		}

		public static string Replace(string stringValue, string searchFor, string replaceWith)
		{
			return (searchFor == String.Empty ? stringValue : stringValue.Replace(searchFor, replaceWith));
		}
	}
}
