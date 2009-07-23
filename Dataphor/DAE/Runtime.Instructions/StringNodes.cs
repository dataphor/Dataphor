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

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator iIndexer(const AString : string, const AIndex : integer) : string
	public class StringIndexerNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif
			
			string LString = (string)AArgument1;
			int LIndex = (int)AArgument2;
			
			if ((LIndex < 0) || (LIndex >= LString.Length))
				return String.Empty;
			
			return Convert.ToString(LString[LIndex]);
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			IndexerExpression LExpression = new D4IndexerExpression();
			LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.Indexer = (Expression)Nodes[1].EmitStatement(AMode);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
	}

	// operator Length(AString : System.String) : System.Integer
	public class StringLengthNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return ((string)AArgument1).Length;
		}
	}
	
	// operator SubString(AString : System.String, AStart : System.String) : System.String
	public class StringSubStringNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif
			
			string LString = (string)AArgument1;
			int LStartIndex = (int)AArgument2;
			if (LStartIndex > LString.Length)
				LStartIndex = LString.Length;
			else if (LStartIndex < 0)
				LStartIndex = 0;
			return LString.Substring(LStartIndex, LString.Length - LStartIndex);
		}
	}
	
	// operator SubString(AString : System.String, AStart : System.String, ALength : System.Integer) : System.String
	public class StringSubStringTernaryNode : TernaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null || AArgument3 == null)
				return null;
			#endif
			
			string LString = (string)AArgument1;
			int LStartIndex = (int)AArgument2;
			if (LStartIndex > LString.Length)
				LStartIndex = LString.Length;
			else if (LStartIndex < 0)
				LStartIndex = 0;

			int LLength = (int)AArgument3;
			if ((LStartIndex + LLength) > LString.Length)
				LLength = LString.Length - LStartIndex;
				
			return LString.Substring(LStartIndex, LLength);
		}
	}
	
	// operator Pos(ASubString : System.String, AString : System.String) : System.Integer
	public class StringPosNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return StringUtility.IndexOf((string)AArgument2, (string)AArgument1);
		}
	}

	#if USEISTRING	
	public class IStringPosNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
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
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || ((AArguments.Length == 3) && (AArguments[2] == null)))
				return null;
			#endif

			if (AArguments.Length == 3)
				return ((string)AArguments[0]).PadLeft((int)AArguments[1], ((string)AArguments[2])[0]);
			else
				return ((string)AArguments[0]).PadLeft((int)AArguments[1]);
		}
	}
	
	// operator PadRight(AString : System.String, ATotalLength : System.Integer)
	// operator PadRight(AString : System.String, ATotalLength : System.Integer, APadChar : System.String)
	public class StringPadRightNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || ((AArguments.Length == 3) && (AArguments[2] == null)))
				return null;
			#endif

			if (AArguments.Length == 3)
				return ((string)AArguments[0]).PadRight((int)AArguments[1], ((string)AArguments[2])[0]);
			else
				return ((string)AArguments[0]).PadRight((int)AArguments[1]);
		}
	}

	// operator Insert(AString : System.String, AStartIndex : System.Integer, AInsertString : System.String) : System.String
	public class StringInsertNode : TernaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null || AArgument3 == null)
				return null;
			#endif
			
			string LString = (string)AArgument1;
			int LStartIndex = (int)AArgument2;
			if (LStartIndex > LString.Length)
				LStartIndex = LString.Length;
			else if (LStartIndex < 0)
				LStartIndex = 0;
				
			return LString.Insert(LStartIndex, (string)AArgument3);
		}
	}
	
	// operator Remove(AString : System.String, AStartIndex : System.Integer, ALength : System.Integer) : System.String
	public class StringRemoveNode : TernaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null || AArgument3 == null)
				return null;
			#endif
			
			string LString = (string)AArgument1;

			int LStartIndex = (int)AArgument2;
			if (LStartIndex > LString.Length)
				LStartIndex = LString.Length;
			else if (LStartIndex < 0)
				LStartIndex = 0;

			int LLength = (int)AArgument3;
			if ((LStartIndex + LLength) > LString.Length)
				LLength = LString.Length - LStartIndex;

			return LString.Remove(LStartIndex, LLength);
		}
	}
	
	// operator Split(const AString : String) : list(String);
	// operator Split(const AString : String, const ADelimiter : String) : list(String);
	// operator Split(const AString : String, const ADelimiters : list(String)) : list(String);
	public class StringSplitNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || ((AArguments.Length > 1) && (AArguments[1] == null)))
				return null;
			#endif
			
			string LString = (string)AArguments[0];
			string[] LDelimiters;
			if (AArguments.Length == 1)
				LDelimiters = new string[]{",", ";"};
			else if (Operator.Operands[1].DataType is Schema.ListType)
			{
				ListValue LDelimiterList = (ListValue)AArguments[1];
				LDelimiters = new string[LDelimiterList.Count()];
				for (int LIndex = 0; LIndex < LDelimiterList.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LDelimiterList[LIndex] == null)
						return null;
					#endif

					LDelimiters[LIndex] = (string)LDelimiterList[LIndex];
				}
			}
			else
				LDelimiters = new string[]{(string)AArguments[1]};
				
			ListValue LValue = new ListValue(AProcess, (Schema.ListType)FDataType);
			
			int LStart = 0;
			int LFirst = 0;
			int LDelimeterLength = 0;
			do
			{
				LStart = LFirst + LDelimeterLength;
				LFirst = -1;
				LDelimeterLength = 0;
				for (int i = 0; i < LDelimiters.Length; i++)
				{
					int LIndex = StringUtility.IndexOf(LString, LDelimiters[i], LStart);
					if ((LIndex >= 0) && ((LFirst < 0) || (LIndex < LFirst)))
					{
						LFirst = LIndex;
						LDelimeterLength = LDelimiters[i].Length;
					}
				}
				LValue.Add(LString.Substring(LStart, (LFirst < 0 ? LString.Length : LFirst) - LStart));
			} while ((LFirst >= 0) && (((LFirst - LStart) + LDelimeterLength) > 0));
			
			return LValue;
		}
	}
	
	// operator Concat(const AStrings : list(String)) : String;
	// operator Concat(const AStrings : list(String), const ADelimiter : String) : String;
	public class StringConcatNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || ((AArguments.Length > 1) && (AArguments[1] == null)))
				return null;
			#endif

			string LDelimiter = AArguments.Length > 1 ? (string)AArguments[1] : "";			
			StringBuilder LResult = new StringBuilder();
			ListValue LStrings = (ListValue)AArguments[0];
			for (int LIndex = 0; LIndex < LStrings.Count(); LIndex++)
			{
				#if NILPROPOGATION
				if (LStrings[LIndex] == null)
					return null;
				#endif
				
				if (LIndex > 0)
					LResult.Append(LDelimiter);
				LResult.Append(LStrings[LIndex]);
			}

			return LResult.ToString();
		}
	}
	
	// operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String) : System.String
    // operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String, ACaseSensitive : System.Boolean) : System.String
    public class StringReplaceNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || (AArguments[1] == null) || (AArguments[2] == null) || ((AArguments.Length == 4) && (AArguments[3] == null)))
				return null;
			#endif
            if ((AArguments.Length == 3) || ((AArguments.Length) == 4 && (bool)AArguments[3]))
				return StringUtility.Replace((string)AArguments[0], (string)AArguments[1], (string)AArguments[2]);
            else
            {
                string LString = (string)AArguments[0];
			    string LOldString = ((string)AArguments[1]).ToUpper();
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
	}
	
	#if USEISTRING
	// operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String) : System.String
	public class IStringReplaceNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
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
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return ((string)AArgument1).Trim();
		}
	}
	
	// operator TrimLeft(AString : System.String) : System.String
	public class StringTrimLeftNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return ((string)AArgument1).TrimStart(null);
		}
	}
	
	// operator TrimRight(AString : System.String) : System.String
	public class StringTrimRightNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return ((string)AArgument1).TrimEnd(null);
		}
	}
	
	// operator LastPos(ASubString : System.String, AString : System.String) : System.Integer
	public class StringLastPosNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return StringUtility.LastIndexOf((string)AArgument2, (string)AArgument1);
		}
	}
	
	#if USEISTRING
	public class IStringLastPosNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
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
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return StringUtility.IndexOf((string)AArgument1, (string)AArgument2);
		}
	}

	public class StringIndexOfStartNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null)
				return null;
			#endif

			string LString = (string)AArguments[0];
			int LResult;
			if (LString == String.Empty)
				LResult = -1;
			else
			{
				int LStartIndex = (int)AArguments[2];
				if (LStartIndex > LString.Length)
					LStartIndex = LString.Length;
				else if (LStartIndex < 0)
					LStartIndex = 0;
				LResult = StringUtility.IndexOf(LString, (string)AArguments[1], LStartIndex);
			}
			return LResult;
		}
	}

	public class StringIndexOfStartLengthNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null || AArguments[3] == null)
				return null;
			#endif

			string LString = (string)AArguments[0];
			int LStartIndex = (int)AArguments[2];
			int LLength = (int)AArguments[3];
			int LResult;
			if ((LString == String.Empty) || (LStartIndex >= LString.Length))
			{
				if (LLength < 0)
					throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
				LResult = -1;
			}
			else
			{
				if (LStartIndex < 0)
					LStartIndex = 0;
				if ((LStartIndex + LLength) > LString.Length)
					LLength = LString.Length - LStartIndex;
				LResult = StringUtility.IndexOf(LString, (string)AArguments[1], LStartIndex, LLength);
			}
			return LResult;
		}
	}

	public class StringStartsWith : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return ((string)AArgument1).StartsWith((string)AArgument2);
		}
	}
	
	public class StringEndsWith : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return ((string)AArgument1).EndsWith((string)AArgument2);
		}
	}
	
	#if USEISTRING
	public class IStringIndexOfNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
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
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || ((AArguments.Length > 2) && (AArguments[2] == null)) || ((AArguments.Length > 3) && (AArguments[3] == null)))
				return null;
			#endif

			string LString = (string)AArguments[0];

			int LStartIndex;
			if (AArguments.Length > 2)
			{
				LStartIndex = (int)AArguments[2];
				if (LStartIndex < 0)
					LStartIndex = 0;
			}
			else
				LStartIndex = 0;
			
			int LLength;
			if (AArguments.Length > 3)
			{
				LLength = (int)AArguments[3];
				if (LLength < 0)
					throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
				if ((LStartIndex + LLength) > LString.Length)
					LLength = LString.Length - LStartIndex;
			}
			else
				LLength = LString.Length - LStartIndex;
			
			int LResult = -1;
			if ((LLength != 0) && (LString != String.Empty) && (LStartIndex < LString.Length))
			{
				ListValue LAnyOf = (ListValue)AArguments[1];
				for (int LIndex = 0; LIndex < LAnyOf.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LAnyOf[LIndex] == null)
						return null;
					#endif

					int LIndexOf = StringUtility.IndexOf(LString, (string)LAnyOf[LIndex], LStartIndex, LLength);
					if ((LIndexOf >= 0) && ((LResult < 0) || (LIndexOf < LResult)))
						LResult = LIndexOf;
				}
			}			

			return LResult;
		}
	}
	
	#if USEISTRING
	public class IStringIndexOfAnyNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
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
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return StringUtility.LastIndexOf((string)AArgument1, (string)AArgument2);
		}
	}

	public class StringLastIndexOfStartNode : TernaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null || AArgument3 == null)
				return null;
			#endif

			string LString = (string)AArgument1;
			int LResult;
			if (LString == String.Empty)
				LResult = -1;
			else
			{
				int LStartIndex = (int)AArgument3;
				if (LStartIndex > (LString.Length - 1))
					LStartIndex = (LString.Length - 1);
				else if (LStartIndex < -1)
					LStartIndex = -1;
				LResult = StringUtility.LastIndexOf(LString, (string)AArgument2, LStartIndex);
			}
			return LResult;
		}
	}

	public class StringLastIndexOfStartLengthNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null || AArguments[3] == null)
				return null;
			#endif

			string LString = (string)AArguments[0];
			int LStartIndex = (int)AArguments[2];
			int LLength = (int)AArguments[3];
			int LResult;
			if ((LStartIndex < 0) || (LString == String.Empty))
			{
				if (LLength < 0)
					throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
				LResult = -1;
			}
			else
			{
				if (LStartIndex > (LString.Length - 1))
					LStartIndex = (LString.Length - 1);
				if ((LStartIndex - LLength) < -1)
					LLength = LStartIndex + 1;
				LResult = StringUtility.LastIndexOf(LString, (string)AArguments[1], LStartIndex, LLength);	// will throw if ALength < 0
			}
			return LResult;
		}
	}

	#if USEISTRING
	public class IStringLastIndexOfNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
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
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || ((AArguments.Length > 2) && (AArguments[2] == null)) || ((AArguments.Length > 3) && (AArguments[3] == null)))
				return null;
			#endif

			string LString = (string)AArguments[0];

			int LStartIndex;
			if (AArguments.Length > 2)
			{
				LStartIndex = (int)AArguments[2];
				if (LStartIndex >= LString.Length)
					LStartIndex = LString.Length - 1;
			}
			else
				LStartIndex = LString.Length - 1;
			
			int LLength;
			if (AArguments.Length > 3)
			{
				LLength = (int)AArguments[3];
				if (LLength < 0)
					throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
				if ((LStartIndex - LLength) < -1)
					LLength = LStartIndex + 1;
			}
			else
				LLength = LStartIndex + 1;
			
			int LResult = -1;
			if ((LLength != 0) && (LString != String.Empty) && (LStartIndex >= 0))
			{
				ListValue LAnyOf = (ListValue)AArguments[1];
				for (int LIndex = 0; LIndex < LAnyOf.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LAnyOf[LIndex] == null)
						return null;
					#endif

					int LIndexOf = StringUtility.LastIndexOf(LString, (string)LAnyOf[LIndex], LStartIndex, LLength);
					if ((LIndexOf >= 0) && ((LResult < 0) || (LIndexOf > LResult)))
						LResult = LIndexOf;
				}
			}

			return LResult;
		}
	}
	
	#if USEISTRING
	public class IStringLastIndexOfAnyNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
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
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			int LFound = 0;
			int LTotalFinds = 0;
			string LString = (string)AArgument1;
			string LSubString = (string)AArgument2;

			for (int i = 0; i < LString.Length; i++) 
			{
				LFound = StringUtility.IndexOf(LString, LSubString, i);
				if (LFound >= 0) 
				{
					LTotalFinds++;
					i = LFound;
				}
				else
					break;
			}
			return LTotalFinds;
		}
	}

	// operator Upper(string) : string;
	public class StringUpperNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return ((string)AArgument1).ToUpper();
		}
	}
	
	// operator Lower(string) : string;
	public class StringLowerNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return ((string)AArgument1).ToLower();
		}
	}
	
	// operator iLike(string, string) : boolean
	public abstract class StringLikeNodeBase : BinaryInstructionNode
	{
		protected const int CRegexCacheSize = 20;
		protected static FixedSizeCache FRegexCache = new FixedSizeCache(CRegexCacheSize);

		public static Regex GetRegex(string APattern)
		{
			lock (FRegexCache)
			{
				Regex LRegex = FRegexCache[APattern] as Regex;
				if (LRegex == null)
				{
					LRegex = new Regex(APattern, RegexOptions.None);
					FRegexCache.Add(APattern, LRegex);
				}
				
				return LRegex;
			}
		}
		
		protected string TransformString(string AInput)
		{
			IList LRegexChars = (IList)new char[]{'.', '$', '{', '[', '(', '|', ')', '*', '+', '?'};
			StringBuilder LResult = new StringBuilder();
			int LIndex = 0;
			bool LIsRegexChar = false;
			while (LIndex < AInput.Length)
			{
				switch (AInput[LIndex])
				{
					case '_': LResult.Append('.'); LIsRegexChar = false; break;
					case '%': LResult.Append(".*"); LIsRegexChar = true; break;
					case '\\':
						LIndex++;
						if (LIndex == AInput.Length)
							LResult.Append('\\');
						else
						{
							if (!((AInput[LIndex] == '_') || (AInput[LIndex] == '%')))
								LResult.Append('\\');
							LResult.Append(AInput[LIndex]);
						}
						LIsRegexChar = false;
					break;
					
					default:
						if (LRegexChars.Contains(AInput[LIndex]))
							LResult.Append('\\');
						LResult.Append(AInput[LIndex]);
						LIsRegexChar = false;
					break;
				}
				LIndex++;
			}
			if (!LIsRegexChar)
				LResult.Append("$"); // If the end of the pattern is not a regex operator, force the match at the end of the string
			return LResult.ToString();
		}
	}
	
	public class StringLikeNode : StringLikeNodeBase
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			string LString = (string)AArgument1;
			string LPattern = TransformString((string)AArgument2);
			Regex LRegex = GetRegex(LPattern);
			Match LMatch = LRegex.Match(LString);
			return LMatch.Success && (LMatch.Index == 0);
		}
	}
	
	#if USEISTRING
	public class IStringLikeNode : StringLikeNodeBase
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
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
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return StringLikeNodeBase.GetRegex((string)AArgument2).IsMatch((string)AArgument1);
		}
	}
	
	#if USEISTRING
	public class IStringMatchesNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
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
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif

			return String.Compare((string)AArgument1, (string)AArgument2, true);
		}
	}
	
	// Unicode representation
	// operator System.String.Unicode(const AUnicode : list(System.Integer)) : System.String;
	public class SystemStringUnicodeNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			{
				ListValue LList = (ListValue)AArgument1;
				byte[] LEncodedValue = new byte[LList.Count() * 2];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LList[LIndex] == null)
						return null;
					#endif
					LEncodedValue[LIndex * 2] = (byte)((int)LList[LIndex]);
					LEncodedValue[LIndex * 2 + 1] = (byte)((int)LList[LIndex] >> 8);
				}
				Decoder LDecoder = UnicodeEncoding.Unicode.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new String(LDecodedValue);
			}
		}
	}
	
	// operator System.String.ReadUnicode(const AValue : System.String) : list(Sytem.Integer);
	public class SystemStringReadUnicodeNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			{
				string LString = (string)AArgument1;
				char[] LDecodedValue = new char[LString.Length];
				LString.CopyTo(0, LDecodedValue, 0, LString.Length);
				Encoder LEncoder = UnicodeEncoding.Unicode.GetEncoder();
				byte[] LEncodedValue = new byte[LEncoder.GetByteCount(LDecodedValue, 0, LDecodedValue.Length, false)];
				LEncoder.GetBytes(LDecodedValue, 0, LDecodedValue.Length, LEncodedValue, 0, true);
				ListValue LListValue = new ListValue(AProcess, (Schema.ListType)FDataType);
				for (int LIndex = 0; LIndex < LEncodedValue.Length; LIndex++)
					if ((LIndex % 2) == 1)
						LListValue.Add((LEncodedValue[LIndex - 1]) + (LEncodedValue[LIndex] << 8));
				return LListValue;
			}
		}
	}
	
	// operator System.String.WriteUnicode(const AValue : System.String, const AUnicode : list(System.Integer)) : System.String;
	public class SystemStringWriteUnicodeNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif

			{
				ListValue LList = (ListValue)AArgument2;
				byte[] LEncodedValue = new byte[LList.Count() * 2];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LList[LIndex] == null)
						return null;
					#endif
					LEncodedValue[LIndex * 2] = (byte)((int)LList[LIndex]);
					LEncodedValue[LIndex * 2 + 1] = (byte)((int)LList[LIndex] >> 8);
				}
				Decoder LDecoder = UnicodeEncoding.Unicode.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new String(LDecodedValue);
			}
		}
	}
	
	// UTF8 representation
	// operator System.String.UTF8(const AUTF8 : list(System.Byte)) : System.String;
	public class SystemStringUTF8Node : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			{
				ListValue LList = (ListValue)AArgument1;
				byte[] LEncodedValue = new byte[LList.Count()];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LList[LIndex] == null)
						return null;
					#endif
					LEncodedValue[LIndex] = (byte)LList[LIndex];
				}
				Decoder LDecoder = UnicodeEncoding.UTF8.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new String(LDecodedValue);
			}
		}
	}
	
	// operator System.String.ReadUTF8(const AValue : System.String) : list(System.Byte);
	public class SystemStringReadUTF8Node : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			{
				string LString = (string)AArgument1;
				char[] LDecodedValue = new char[LString.Length];
				LString.CopyTo(0, LDecodedValue, 0, LString.Length);
				Encoder LEncoder = UnicodeEncoding.UTF8.GetEncoder();
				byte[] LEncodedValue = new byte[LEncoder.GetByteCount(LDecodedValue, 0, LDecodedValue.Length, false)];
				LEncoder.GetBytes(LDecodedValue, 0, LDecodedValue.Length, LEncodedValue, 0, true);
				ListValue LListValue = new ListValue(AProcess, (Schema.ListType)FDataType);
				for (int LIndex = 0; LIndex < LDecodedValue.Length; LIndex++)
					LListValue.Add(LEncodedValue[LIndex]);
				return LListValue;
			}
		}
	}
	
	// operator System.String.WriteUTF8(const AValue : System.String, const AUTF8 : list(System.Byte)) : System.String;
	public class SystemStringWriteUTF8Node : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif

			{
				ListValue LList = (ListValue)AArgument2;
				byte[] LEncodedValue = new byte[LList.Count()];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LList[LIndex] == null)
						return null;
					#endif
					LEncodedValue[LIndex] = (byte)LList[LIndex];
				}
				Decoder LDecoder = UnicodeEncoding.UTF8.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new String(LDecodedValue);
			}
		}
	}
	
	// ASCII representation
	// operator System.String.ASCII(const AASCII : list(System.Byte)) : System.String;
	public class SystemStringASCIINode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			{
				ListValue LList = (ListValue)AArgument1;
				byte[] LEncodedValue = new byte[LList.Count()];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LList[LIndex] == null)
						return null;
					#endif
					LEncodedValue[LIndex] = (byte)LList[LIndex];
				}
				Decoder LDecoder = UnicodeEncoding.ASCII.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new String(LDecodedValue);
			}
		}
	}
	
	// operator System.String.ReadASCII(const AValue : System.String) : list(System.Byte);
	public class SystemStringReadASCIINode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			{
				string LString = (string)AArgument1;
				char[] LDecodedValue = new char[LString.Length];
				LString.CopyTo(0, LDecodedValue, 0, LString.Length);
				Encoder LEncoder = UnicodeEncoding.ASCII.GetEncoder();
				byte[] LEncodedValue = new byte[LEncoder.GetByteCount(LDecodedValue, 0, LDecodedValue.Length, false)];
				LEncoder.GetBytes(LDecodedValue, 0, LDecodedValue.Length, LEncodedValue, 0, true);
				ListValue LListValue = new ListValue(AProcess, (Schema.ListType)FDataType);
				for (int LIndex = 0; LIndex < LDecodedValue.Length; LIndex++)
					LListValue.Add(LEncodedValue[LIndex]);
				return LListValue;
			}
		}
	}
	
	// operator System.String.WriteASCII(const AValue : System.String, const AASCII : list(System.Byte)) : System.String;
	public class SystemStringWriteASCIINode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif

			{
				ListValue LList = (ListValue)AArgument2;
				byte[] LEncodedValue = new byte[LList.Count()];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if (LList[LIndex] == null)
						return null;
					#endif
					LEncodedValue[LIndex] = (byte)LList[LIndex];
				}
				Decoder LDecoder = UnicodeEncoding.ASCII.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new String(LDecodedValue);
			}
		}
	}
	
	// operator System.IsUpper(const AValue : String) : Boolean;
	// operator System.IsUpper(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsUpperNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if (AArguments[0] == null)
					return null;
				#endif
				
				string LValue = (string)AArguments[0];
				bool LIsUpper = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (Char.IsLetter(LValue, LIndex) && !Char.IsUpper(LValue, LIndex))
					{
						LIsUpper = false;
						break;
					}
				
				return LIsUpper;
			}
			else
			{
				#if NILPROPOGATION
				if (AArguments[0] == null || AArguments[1] == null)
					return null;
				#endif
				
				string LString = (string)AArguments[0];
				int LIndex = (int)AArguments[1];
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return false;

				return Char.IsUpper(LString, LIndex);
			}
		}
	}

	// operator System.IsLower(const AValue : String) : Boolean;	
	// operator System.IsLower(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLowerNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if (AArguments[0] == null)
					return null;
				#endif

				string LValue = (string)AArguments[0];
				bool LIsLower = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (Char.IsLetter(LValue, LIndex) && !Char.IsLower(LValue, LIndex))
					{
						LIsLower = false;
						break;
					}
					
				return LIsLower;	
			}
			else
			{
				#if NILPROPOGATION
				if (AArguments[0] == null || AArguments[1] == null)
					return null;
				#endif

				string LString = (string)AArguments[0];
				int LIndex = (int)AArguments[1];
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return false;

				return Char.IsLower(LString, LIndex);
			}
		}
	}
	
	// operator System.IsLetter(const AValue : String) : Boolean;	
	// operator System.IsLetter(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLetterNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if (AArguments[0] == null)
					return null;
				#endif

				string LValue = (string)AArguments[0];
				bool LIsLetter = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (!Char.IsLetter(LValue, LIndex))
					{
						LIsLetter = false;
						break;
					}
					
				return LIsLetter;	
			}
			else
			{
				#if NILPROPOGATION
				if (AArguments[0] == null || AArguments[1] == null)
					return null;
				#endif

				string LString = (string)AArguments[0];
				int LIndex = (int)AArguments[1];
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return false;

				return Char.IsLetter(LString, LIndex);
			}
		}
	}
	
	// operator System.IsDigit(const AValue : String) : Boolean;	
	// operator System.IsDigit(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsDigitNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if (AArguments[0] == null)
					return null;
				#endif

				string LValue = (string)AArguments[0];
				bool LIsDigit = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (!Char.IsDigit(LValue, LIndex))
					{
						LIsDigit = false;
						break;
					}
					
				return LIsDigit;	
			}
			else
			{
				#if NILPROPOGATION
				if (AArguments[0] == null || AArguments[1] == null)
					return null;
				#endif

				string LString = (string)AArguments[0];
				int LIndex = (int)AArguments[1];
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return false;

				return Char.IsDigit(LString, LIndex);
			}
		}
	}
	
	// operator System.IsLetterOrDigit(const AValue : String) : Boolean;	
	// operator System.IsLetterOrDigit(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLetterOrDigitNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if (AArguments[0] == null)
					return null;
				#endif

				string LValue = (string)AArguments[0];
				bool LIsLetterOrDigit = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (!Char.IsLetterOrDigit(LValue, LIndex))
					{
						LIsLetterOrDigit = false;
						break;
					}
					
				return LIsLetterOrDigit;	
			}
			else
			{
				#if NILPROPOGATION
				if (AArguments[0] == null || AArguments[1] == null)
					return null;
				#endif

				string LString = (string)AArguments[0];
				int LIndex = (int)AArguments[1];
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return false;

				return Char.IsLetterOrDigit(LString, LIndex);
			}
		}
	}
	
	#if USEISTRING
	// operator System.EnsureUpper(var AValue : IString);
	public class IStringEnsureUpperNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if ((AArguments[0].Value != null) && !AArguments[0].Value.IsNil)
				AArguments[0].Value = new Scalar(AProcess, (Schema.ScalarType)FDataType, (string)AArguments[0].ToUpper());
			return null;
		}
	}
	
	// operator System.EnsureLower(var AValue : IString);
	public class IStringEnsureLowerNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if ((AArguments[0].Value != null) && !AArguments[0].Value.IsNil)
				AArguments[0].Value = new Scalar(AProcess, (Schema.ScalarType)FDataType, (string)AArguments[0].ToLower());
			return null;
		}
	}
	#endif

	/// <remarks> The string searching routines in the StringUtility class treat the empty string as though it does not exist in any string.  The standard .NET routines treat the empty string as though it exists in all strings. </remarks>
	public sealed class StringUtility
	{
		public static int IndexOf(string AString, string ASearchFor)
		{
			return (ASearchFor == String.Empty ? -1 : AString.IndexOf(ASearchFor));
		}

		public static int IndexOf(string AString, string ASearchFor, int AStartIndex)
		{
			return (ASearchFor == String.Empty ? -1 : AString.IndexOf(ASearchFor, AStartIndex));
		}

		public static int IndexOf(string AString, string ASearchFor, int AStartIndex, int ALength)
		{
			return (ASearchFor == String.Empty ? -1 : AString.IndexOf(ASearchFor, AStartIndex, ALength));
		}

		public static int LastIndexOf(string AString, string ASearchFor)
		{
			return (ASearchFor == String.Empty ? -1 : AString.LastIndexOf(ASearchFor));
		}

		public static int LastIndexOf(string AString, string ASearchFor, int AStartIndex)
		{
			return (ASearchFor == String.Empty ? -1 : AString.LastIndexOf(ASearchFor, AStartIndex));
		}

		public static int LastIndexOf(string AString, string ASearchFor, int AStartIndex, int ALength)
		{
			return (ASearchFor == String.Empty ? -1 : AString.LastIndexOf(ASearchFor, AStartIndex, ALength));
		}

		public static string Replace(string AString, string ASearchFor, string AReplaceWith)
		{
			return (ASearchFor == String.Empty ? AString : AString.Replace(ASearchFor, AReplaceWith));
		}
	}
}
