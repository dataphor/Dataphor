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
	public class StringIndexerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			string LString = AArguments[0].Value.AsString;
			int LIndex = AArguments[1].Value.AsInt32;
			
			if ((LIndex < 0) || (LIndex >= LString.Length))
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, String.Empty));
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToString(LString[LIndex])));
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
	public class StringLengthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.Length));
		}
	}
	
	// operator SubString(AString : System.String, AStart : System.String) : System.String
	// operator SubString(AString : System.String, AStart : System.String, ALength : System.Integer) : System.String
	public class StringSubStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || ((AArguments.Length == 3) && ((AArguments[2].Value == null) || AArguments[2].Value.IsNil)))
				return new DataVar(FDataType, null);
			#endif
			
			string LString = AArguments[0].Value.AsString;
			int LStartIndex = AArguments[1].Value.AsInt32;
			if (LStartIndex > LString.Length)
				LStartIndex = LString.Length;
			else if (LStartIndex < 0)
				LStartIndex = 0;
			int LLength;
			if (AArguments.Length > 2)
			{
				LLength = AArguments[2].Value.AsInt32;
				if ((LStartIndex + LLength) > LString.Length)
					LLength = LString.Length - LStartIndex;
			}
			else
				LLength = LString.Length - LStartIndex;
				
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString.Substring(LStartIndex, LLength)));
		}
	}
	
	// operator Pos(ASubString : System.String, AString : System.String) : System.Integer
	public class StringPosNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					StringUtility.IndexOf(AArguments[1].Value.AsString, AArguments[0].Value.AsString)
				)
			);
		}
	}

	#if USEISTRING	
	public class IStringPosNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					AArguments[1].Value.AsString.ToUpper().IndexOf
					(
						AArguments[0].Value.AsString.ToUpper()
					)
				)
			);
		}
	}
	#endif
	
	// operator PadLeft(AString : System.String, ATotalLength : System.Integer)
	// operator PadLeft(AString : System.String, ATotalLength : System.Integer, APadChar : System.String)
	public class StringPadLeftNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || ((AArguments.Length == 3) && ((AArguments[2].Value == null) || AArguments[2].Value.IsNil)))
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			if (AArguments.Length == 3)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString.PadLeft(AArguments[1].Value.AsInt32, AArguments[2].Value.AsString[0])));
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString.PadLeft(AArguments[1].Value.AsInt32)));
		}
	}
	
	// operator PadRight(AString : System.String, ATotalLength : System.Integer)
	// operator PadRight(AString : System.String, ATotalLength : System.Integer, APadChar : System.String)
	public class StringPadRightNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || ((AArguments.Length == 3) && ((AArguments[2].Value == null) || AArguments[2].Value.IsNil)))
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			if (AArguments.Length == 3)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString.PadRight(AArguments[1].Value.AsInt32, AArguments[2].Value.AsString[0])));
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString.PadRight(AArguments[1].Value.AsInt32)));
		}
	}

	// operator Insert(AString : System.String, AStartIndex : System.Integer, AInsertString : System.String) : System.String
	public class StringInsertNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			string LString = AArguments[0].Value.AsString;
			int LStartIndex = AArguments[1].Value.AsInt32;
			if (LStartIndex > LString.Length)
				LStartIndex = LString.Length;
			else if (LStartIndex < 0)
				LStartIndex = 0;
				
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString.Insert(LStartIndex, AArguments[2].Value.AsString)));
		}
	}
	
	// operator Remove(AString : System.String, AStartIndex : System.Integer, ALength : System.Integer) : System.String
	public class StringRemoveNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			string LString = AArguments[0].Value.AsString;

			int LStartIndex = AArguments[1].Value.AsInt32;
			if (LStartIndex > LString.Length)
				LStartIndex = LString.Length;
			else if (LStartIndex < 0)
				LStartIndex = 0;

			int LLength = AArguments[2].Value.AsInt32;
			if ((LStartIndex + LLength) > LString.Length)
				LLength = LString.Length - LStartIndex;

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString.Remove(LStartIndex, LLength)));
		}
	}
	
	// operator Split(const AString : String) : list(String);
	// operator Split(const AString : String, const ADelimiter : String) : list(String);
	// operator Split(const AString : String, const ADelimiters : list(String)) : list(String);
	public class StringSplitNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || ((AArguments.Length > 1) && ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)))
				return new DataVar(FDataType, null);
			#endif
			
			string LString = AArguments[0].Value.AsString;
			string[] LDelimiters;
			if (AArguments.Length == 1)
				LDelimiters = new string[]{",", ";"};
			else if (AArguments[1].DataType is Schema.ListType)
			{
				ListValue LDelimiterList = (ListValue)AArguments[1].Value;
				LDelimiters = new string[LDelimiterList.Count()];
				for (int LIndex = 0; LIndex < LDelimiterList.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if ((LDelimiterList[LIndex] == null) || LDelimiterList[LIndex].IsNil)
						return new DataVar(FDataType, null);
					#endif

					LDelimiters[LIndex] = LDelimiterList[LIndex].AsString;
				}
			}
			else
				LDelimiters = new string[]{AArguments[1].Value.AsString};
				
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
				LValue.Add(new Scalar(AProcess, AProcess.DataTypes.SystemString, LString.Substring(LStart, (LFirst < 0 ? LString.Length : LFirst) - LStart)));
			} while ((LFirst >= 0) && (((LFirst - LStart) + LDelimeterLength) > 0));
			
			return new DataVar(FDataType, LValue);
		}
	}
	
	// operator Concat(const AStrings : list(String)) : String;
	// operator Concat(const AStrings : list(String), const ADelimiter : String) : String;
	public class StringConcatNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || ((AArguments.Length > 1) && ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)))
				return new DataVar(FDataType, null);
			#endif

			string LDelimiter = AArguments.Length > 1 ? AArguments[1].Value.AsString : "";			
			StringBuilder LResult = new StringBuilder();
			ListValue LStrings = (ListValue)AArguments[0].Value;
			for (int LIndex = 0; LIndex < LStrings.Count(); LIndex++)
			{
				#if NILPROPOGATION
				if ((LStrings[LIndex] == null) || LStrings[LIndex].IsNil)
					return new DataVar(FDataType, null);
				#endif
				
				if (LIndex > 0)
					LResult.Append(LDelimiter);
				LResult.Append(LStrings[LIndex].AsString);
			}

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LResult.ToString()));
		}
	}
	
	// operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String) : System.String
    // operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String, ACaseSensitive : System.Boolean) : System.String
    public class StringReplaceNode : InstructionNode
	{
        private bool NilFound(DataValue A)
        {
            if ((A == null) || A.IsNil) return true; else return false;
        }
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if (
                NilFound(AArguments[0].Value) ||
                NilFound(AArguments[1].Value) ||
                NilFound(AArguments[2].Value) ||
                (AArguments.Length == 4) && (NilFound(AArguments[3].Value)) // NOTE: relies on short-circuit evaluation
               )
				return new DataVar(FDataType, null);
			#endif
            if ((AArguments.Length == 3) || ((AArguments.Length) == 4 && (AArguments[3].Value.AsBoolean == true)))
            {
                return new DataVar(FDataType, 
                                   new Scalar(AProcess, (Schema.ScalarType)FDataType, StringUtility.Replace(AArguments[0].Value.AsString, 
                                                                                                            AArguments[1].Value.AsString, 
                                                                                                            AArguments[2].Value.AsString)));
            }
            else
            {
                string LString = AArguments[0].Value.AsString;
			    string LOldString = AArguments[1].Value.AsString.ToUpper();
			    if (LOldString.Length > 0)
			    {
				    string LNewString = AArguments[2].Value.AsString;
				    int LCurrentIndex = LString.ToUpper().IndexOf(LOldString);
				    while ((LCurrentIndex >= 0) && (LCurrentIndex < LString.Length))
				    {
					    LString = LString.Remove(LCurrentIndex, Math.Min(LOldString.Length, LString.Length - LCurrentIndex)).Insert(LCurrentIndex, LNewString);
					    LCurrentIndex = LString.ToUpper().IndexOf(LOldString, LCurrentIndex + LNewString.Length);
				    }
			    }
			    return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString));
            }
        }
	}
	
	#if USEISTRING
	// operator Replace(AString : System.String, AOldString : System.String, ANewString : System.String) : System.String
	public class IStringReplaceNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			string LOldString = AArguments[1].Value.AsString.ToUpper();
			if (LOldString.Length > 0)
			{
				string LNewString = AArguments[2].Value.AsString;
				int LCurrentIndex = LString.ToUpper().IndexOf(LOldString);
				while ((LCurrentIndex >= 0) && (LCurrentIndex < LString.Length))
				{
					LString = LString.Remove(LCurrentIndex, Math.Min(LOldString.Length, LString.Length - LCurrentIndex)).Insert(LCurrentIndex, LNewString);
					LCurrentIndex = LString.ToUpper().IndexOf(LOldString, LCurrentIndex + LNewString.Length);
				}
			}
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LString));
		}
	}
	#endif
	
	// operator Trim(AString : System.String) : System.String
	public class StringTrimNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.Trim()));
		}
	}
	
	// operator TrimLeft(AString : System.String) : System.String
	public class StringTrimLeftNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.TrimStart(null)));
		}
	}
	
	// operator TrimRight(AString : System.String) : System.String
	public class StringTrimRightNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.TrimEnd(null)));
		}
	}
	
	// operator LastPos(ASubString : System.String, AString : System.String) : System.Integer
	public class StringLastPosNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					StringUtility.LastIndexOf(AArguments[1].Value.AsString, AArguments[0].Value.AsString)
				)
			);
		}
	}
	
	#if USEISTRING
	public class IStringLastPosNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					AArguments[1].Value.AsString.ToUpper().LastIndexOf
					(
						AArguments[0].Value.AsString.ToUpper()
					)
				)
			);
		}
	}
	#endif
	
	public class StringIndexOfNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					StringUtility.IndexOf(AArguments[0].Value.AsString, AArguments[1].Value.AsString)
				)
			);
		}
	}

	public class StringIndexOfStartNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			int LResult;
			if (LString == String.Empty)
				LResult = -1;
			else
			{
				int LStartIndex = AArguments[2].Value.AsInt32;
				if (LStartIndex > LString.Length)
					LStartIndex = LString.Length;
				else if (LStartIndex < 0)
					LStartIndex = 0;
				LResult = StringUtility.IndexOf(LString, AArguments[1].Value.AsString, LStartIndex);
			}
			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					LResult
				)
			);
		}
	}

	public class StringIndexOfStartLengthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil || (AArguments[3].Value == null) || AArguments[3].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			int LStartIndex = AArguments[2].Value.AsInt32;
			int LLength = AArguments[3].Value.AsInt32;
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
				LResult = StringUtility.IndexOf(LString, AArguments[1].Value.AsString, LStartIndex, LLength);
			}
			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					LResult
				)
			);
		}
	}

	public class StringStartsWith : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess,
					(Schema.ScalarType)FDataType,
					AArguments[0].Value.AsString.StartsWith(AArguments[1].Value.AsString)
				)
			);
		}
	}
	
	public class StringEndsWith : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess,
					(Schema.ScalarType)FDataType,
					AArguments[0].Value.AsString.EndsWith(AArguments[1].Value.AsString)
				)
			);
		}
	}
	
	#if USEISTRING
	public class IStringIndexOfNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					AArguments[0].Value.AsString.ToUpper().IndexOf
					(
						AArguments[1].Value.AsString.ToUpper()
					)
				)
			);
		}
	}
	#endif
	
	public class StringIndexOfAnyNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || ((AArguments.Length > 2) && ((AArguments[2].Value == null) || AArguments[2].Value.IsNil)) || ((AArguments.Length > 3) && ((AArguments[3].Value == null) || AArguments[3].Value.IsNil)))
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;

			int LStartIndex;
			if (AArguments.Length > 2)
			{
				LStartIndex = AArguments[2].Value.AsInt32;
				if (LStartIndex < 0)
					LStartIndex = 0;
			}
			else
				LStartIndex = 0;
			
			int LLength;
			if (AArguments.Length > 3)
			{
				LLength = AArguments[3].Value.AsInt32;
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
				ListValue LAnyOf = (ListValue)AArguments[1].Value;
				for (int LIndex = 0; LIndex < LAnyOf.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if ((LAnyOf[LIndex] == null) || LAnyOf[LIndex].IsNil)
						return new DataVar(FDataType, null);
					#endif

					int LIndexOf = StringUtility.IndexOf(LString, LAnyOf[LIndex].AsString, LStartIndex, LLength);
					if ((LIndexOf >= 0) && ((LResult < 0) || (LIndexOf < LResult)))
						LResult = LIndexOf;
				}
			}			

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LResult));
		}
	}
	
	#if USEISTRING
	public class IStringIndexOfAnyNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			int LIndexOf = -1;
			string LString = AArguments[0].Value.AsString.ToUpper();
			ListValue LAnyOf = (ListValue)AArguments[1].Value;
			for (int LIndex = 0; LIndex < LAnyOf.Length(); LIndex++)
			{
				#if NILPROPOGATION
				if ((LAnyOf[LIndex] == null) || LAnyOf[LIndex].IsNil)
					return new DataVar(FDataType, null);
				#endif

				LIndexOf = LString.IndexOf(LAnyOf[LIndex].AsString.ToUpper());
				if (LIndexOf >= 0)
					break;
			}
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LIndexOf));
		}
	}
	#endif
	
	public class StringLastIndexOfNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					StringUtility.LastIndexOf(AArguments[0].Value.AsString, AArguments[1].Value.AsString)
				)
			);
		}
	}

	public class StringLastIndexOfStartNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			int LResult;
			if (LString == String.Empty)
				LResult = -1;
			else
			{
				int LStartIndex = AArguments[2].Value.AsInt32;
				if (LStartIndex > (LString.Length - 1))
					LStartIndex = (LString.Length - 1);
				else if (LStartIndex < -1)
					LStartIndex = -1;
				LResult = StringUtility.LastIndexOf(LString, AArguments[1].Value.AsString, LStartIndex);
			}
			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					LResult
				)
			);
		}
	}

	public class StringLastIndexOfStartLengthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil || (AArguments[3].Value == null) || AArguments[3].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			int LStartIndex = AArguments[2].Value.AsInt32;
			int LLength = AArguments[3].Value.AsInt32;
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
				LResult = StringUtility.LastIndexOf(LString, AArguments[1].Value.AsString, LStartIndex, LLength);	// will throw if ALength < 0
			}
			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					LResult
				)
			);
		}
	}

	#if USEISTRING
	public class IStringLastIndexOfNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar
				(
					AProcess, 
					(Schema.ScalarType)FDataType,
					AArguments[0].Value.AsString.ToUpper().LastIndexOf
					(
						AArguments[1].Value.AsString.ToUpper()
					)
				)
			);
		}
	}
	#endif
	
	public class StringLastIndexOfAnyNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || ((AArguments.Length > 2) && ((AArguments[2].Value == null) || AArguments[2].Value.IsNil)) || ((AArguments.Length > 3) && ((AArguments[3].Value == null) || AArguments[3].Value.IsNil)))
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;

			int LStartIndex;
			if (AArguments.Length > 2)
			{
				LStartIndex = AArguments[2].Value.AsInt32;
				if (LStartIndex >= LString.Length)
					LStartIndex = LString.Length - 1;
			}
			else
				LStartIndex = LString.Length - 1;
			
			int LLength;
			if (AArguments.Length > 3)
			{
				LLength = AArguments[3].Value.AsInt32;
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
				ListValue LAnyOf = (ListValue)AArguments[1].Value;
				for (int LIndex = 0; LIndex < LAnyOf.Count(); LIndex++)
				{
					#if NILPROPOGATION
					if ((LAnyOf[LIndex] == null) || LAnyOf[LIndex].IsNil)
						return new DataVar(FDataType, null);
					#endif

					int LIndexOf = StringUtility.LastIndexOf(LString, LAnyOf[LIndex].AsString, LStartIndex, LLength);
					if ((LIndexOf >= 0) && ((LResult < 0) || (LIndexOf > LResult)))
						LResult = LIndexOf;
				}
			}

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LResult));
		}
	}
	
	#if USEISTRING
	public class IStringLastIndexOfAnyNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			int LLastIndexOf = -1;
			string LString = AArguments[0].Value.AsString.ToUpper();
			ListValue LAnyOf = (ListValue)AArguments[1].Value;
			for (int LIndex = 0; LIndex < LAnyOf.Count(); LIndex++)
			{
				#if NILPROPOGATION
				if ((LAnyOf[LIndex] == null) || LAnyOf[LIndex].IsNil)
					return new DataVar(FDataType, null);
				#endif

				LLastIndexOf = LString.LastIndexOf(LAnyOf[LIndex].AsString.ToUpper());
				if (LLastIndexOf >= 0)
					break;
			}
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLastIndexOf));
		}
	}
	#endif
	
	// operator CountOf(AString : System.String, ASubString : System.String) : System.Integer
	public class StringCountOfNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
#endif

			int LFound = 0;
			int LTotalFinds = 0;
			string LString = AArguments[0].Value.AsString;
			string LSubString = AArguments[1].Value.AsString;

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
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LTotalFinds));
		}
	}

	// operator Upper(string) : string;
	public class StringUpperNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.ToUpper())
			);
		}
	}
	
	// operator Lower(string) : string;
	public class StringLowerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar
			(
				FDataType,
				new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.ToLower())
			);
		}
	}
	
	// operator iLike(string, string) : boolean
	public class StringLikeNodeBase : InstructionNode
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			string LPattern = TransformString(AArguments[1].Value.AsString);
			Regex LRegex = GetRegex(LPattern);
			Match LMatch = LRegex.Match(LString);
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LMatch.Success && (LMatch.Index == 0)));
		}
	}
	
	#if USEISTRING
	public class IStringLikeNode : StringLikeNodeBase
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			string LString = AArguments[0].Value.AsString;
			string LPattern = TransformString(AArguments[1].Value.AsString);
			Match LMatch = Regex.Match(LString, LPattern, RegexOptions.IgnoreCase);
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LMatch.Success && (LMatch.Index == 0)));
		}
	}
	#endif
	
	// operator iMatches(string, string) : boolean
	public class StringMatchesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, StringLikeNodeBase.GetRegex(AArguments[1].Value.AsString).IsMatch(AArguments[0].Value.AsString)));
		}
	}
	
	#if USEISTRING
	public class IStringMatchesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Regex.IsMatch(AArguments[0].Value.AsString, AArguments[1].Value.AsString, RegexOptions.IgnoreCase)));
		}
	}
	#endif
	
	// operator CompareText(string, string) : integer
	public class StringCompareTextNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			string LLeftValue = AArguments[0].Value.AsString;
			string LRightValue = AArguments[1].Value.AsString;
			return new DataVar
			(
				FDataType,
				new Scalar(AProcess, (Schema.ScalarType)FDataType, String.Compare(LLeftValue, LRightValue, true))
			);
		}
	}
	
	// Unicode representation
	// operator System.String.Unicode(const AUnicode : list(System.Integer)) : System.String;
	public class SystemStringUnicodeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				ListValue LList = (ListValue)AArguments[0].Value;
				byte[] LEncodedValue = new byte[LList.Count() * 2];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					LEncodedValue[LIndex * 2] = (byte)(LList[LIndex].AsInt32);
					LEncodedValue[LIndex * 2 + 1] = (byte)(LList[LIndex].AsInt32 >> 8);
				}
				Decoder LDecoder = UnicodeEncoding.Unicode.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new String(LDecodedValue)));
			}
		}
	}
	
	// operator System.String.ReadUnicode(const AValue : System.String) : list(Sytem.Integer);
	public class SystemStringReadUnicodeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				string LString = AArguments[0].Value.AsString;
				char[] LDecodedValue = new char[LString.Length];
				LString.CopyTo(0, LDecodedValue, 0, LString.Length);
				Encoder LEncoder = UnicodeEncoding.Unicode.GetEncoder();
				byte[] LEncodedValue = new byte[LEncoder.GetByteCount(LDecodedValue, 0, LDecodedValue.Length, false)];
				LEncoder.GetBytes(LDecodedValue, 0, LDecodedValue.Length, LEncodedValue, 0, true);
				ListValue LListValue = new ListValue(AProcess, (Schema.ListType)FDataType);
				for (int LIndex = 0; LIndex < LEncodedValue.Length; LIndex++)
					if ((LIndex % 2) == 1)
						LListValue.Add(new Scalar(AProcess, AProcess.DataTypes.SystemInteger, (LEncodedValue[LIndex - 1]) + (LEncodedValue[LIndex] << 8)));
				return new DataVar(FDataType, LListValue);
			}
		}
	}
	
	// operator System.String.WriteUnicode(const AValue : System.String, const AUnicode : list(System.Integer)) : System.String;
	public class SystemStringWriteUnicodeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				ListValue LList = (ListValue)AArguments[1].Value;
				byte[] LEncodedValue = new byte[LList.Count() * 2];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					LEncodedValue[LIndex * 2] = (byte)(LList[LIndex].AsInt32);
					LEncodedValue[LIndex * 2 + 1] = (byte)(LList[LIndex].AsInt32 >> 8);
				}
				Decoder LDecoder = UnicodeEncoding.Unicode.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new String(LDecodedValue)));
			}
		}
	}
	
	// UTF8 representation
	// operator System.String.UTF8(const AUTF8 : list(System.Byte)) : System.String;
	public class SystemStringUTF8Node : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				ListValue LList = (ListValue)AArguments[0].Value;
				byte[] LEncodedValue = new byte[LList.Count()];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
					LEncodedValue[LIndex] = LList[LIndex].AsByte;
				Decoder LDecoder = UnicodeEncoding.UTF8.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new String(LDecodedValue)));
			}
		}
	}
	
	// operator System.String.ReadUTF8(const AValue : System.String) : list(System.Byte);
	public class SystemStringReadUTF8Node : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				string LString = AArguments[0].Value.AsString;
				char[] LDecodedValue = new char[LString.Length];
				LString.CopyTo(0, LDecodedValue, 0, LString.Length);
				Encoder LEncoder = UnicodeEncoding.UTF8.GetEncoder();
				byte[] LEncodedValue = new byte[LEncoder.GetByteCount(LDecodedValue, 0, LDecodedValue.Length, false)];
				LEncoder.GetBytes(LDecodedValue, 0, LDecodedValue.Length, LEncodedValue, 0, true);
				ListValue LListValue = new ListValue(AProcess, (Schema.ListType)FDataType);
				for (int LIndex = 0; LIndex < LDecodedValue.Length; LIndex++)
					LListValue.Add(new Scalar(AProcess, AProcess.DataTypes.SystemByte, LEncodedValue[LIndex]));
				return new DataVar(FDataType, LListValue);
			}
		}
	}
	
	// operator System.String.WriteUTF8(const AValue : System.String, const AUTF8 : list(System.Byte)) : System.String;
	public class SystemStringWriteUTF8Node : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				ListValue LList = (ListValue)AArguments[1].Value;
				byte[] LEncodedValue = new byte[LList.Count()];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
					LEncodedValue[LIndex] = LList[LIndex].AsByte;
				Decoder LDecoder = UnicodeEncoding.UTF8.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new String(LDecodedValue)));
			}
		}
	}
	
	// ASCII representation
	// operator System.String.ASCII(const AASCII : list(System.Byte)) : System.String;
	public class SystemStringASCIINode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				ListValue LList = (ListValue)AArguments[0].Value;
				byte[] LEncodedValue = new byte[LList.Count()];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
					LEncodedValue[LIndex] = LList[LIndex].AsByte;
				Decoder LDecoder = UnicodeEncoding.ASCII.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new String(LDecodedValue)));
			}
		}
	}
	
	// operator System.String.ReadASCII(const AValue : System.String) : list(System.Byte);
	public class SystemStringReadASCIINode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				string LString = AArguments[0].Value.AsString;
				char[] LDecodedValue = new char[LString.Length];
				LString.CopyTo(0, LDecodedValue, 0, LString.Length);
				Encoder LEncoder = UnicodeEncoding.ASCII.GetEncoder();
				byte[] LEncodedValue = new byte[LEncoder.GetByteCount(LDecodedValue, 0, LDecodedValue.Length, false)];
				LEncoder.GetBytes(LDecodedValue, 0, LDecodedValue.Length, LEncodedValue, 0, true);
				ListValue LListValue = new ListValue(AProcess, (Schema.ListType)FDataType);
				for (int LIndex = 0; LIndex < LDecodedValue.Length; LIndex++)
					LListValue.Add(new Scalar(AProcess, AProcess.DataTypes.SystemByte, LEncodedValue[LIndex]));
				return new DataVar(FDataType, LListValue);
			}
		}
	}
	
	// operator System.String.WriteASCII(const AValue : System.String, const AASCII : list(System.Byte)) : System.String;
	public class SystemStringWriteASCIINode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			{
				ListValue LList = (ListValue)AArguments[1].Value;
				byte[] LEncodedValue = new byte[LList.Count()];
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
					LEncodedValue[LIndex] = LList[LIndex].AsByte;
				Decoder LDecoder = UnicodeEncoding.ASCII.GetDecoder();
				char[] LDecodedValue = new char[LDecoder.GetCharCount(LEncodedValue, 0, LEncodedValue.Length)];
				LDecoder.GetChars(LEncodedValue, 0, LEncodedValue.Length, LDecodedValue, 0);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new String(LDecodedValue)));
			}
		}
	}
	
	// operator System.IsUpper(const AValue : String) : Boolean;
	// operator System.IsUpper(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsUpperNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif
				
				string LValue = AArguments[0].Value.AsString;
				bool LIsUpper = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (Char.IsLetter(LValue, LIndex) && !Char.IsUpper(LValue, LIndex))
					{
						LIsUpper = false;
						break;
					}
				
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LIsUpper));
			}
			else
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif
				
				string LString = AArguments[0].Value.AsString;
				int LIndex = AArguments[1].Value.AsInt32;
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, false));

				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Char.IsUpper(LString, LIndex)));
			}
		}
	}

	// operator System.IsLower(const AValue : String) : Boolean;	
	// operator System.IsLower(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLowerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif

				string LValue = AArguments[0].Value.AsString;
				bool LIsLower = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (Char.IsLetter(LValue, LIndex) && !Char.IsLower(LValue, LIndex))
					{
						LIsLower = false;
						break;
					}
					
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LIsLower));	
			}
			else
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif

				string LString = AArguments[0].Value.AsString;
				int LIndex = AArguments[1].Value.AsInt32;
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, false));

				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Char.IsLower(LString, LIndex)));
			}
		}
	}
	
	// operator System.IsLetter(const AValue : String) : Boolean;	
	// operator System.IsLetter(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLetterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif

				string LValue = AArguments[0].Value.AsString;
				bool LIsLetter = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (!Char.IsLetter(LValue, LIndex))
					{
						LIsLetter = false;
						break;
					}
					
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LIsLetter));	
			}
			else
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif

				string LString = AArguments[0].Value.AsString;
				int LIndex = AArguments[1].Value.AsInt32;
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, false));

				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Char.IsLetter(LString, LIndex)));
			}
		}
	}
	
	// operator System.IsDigit(const AValue : String) : Boolean;	
	// operator System.IsDigit(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsDigitNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif

				string LValue = AArguments[0].Value.AsString;
				bool LIsDigit = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (!Char.IsDigit(LValue, LIndex))
					{
						LIsDigit = false;
						break;
					}
					
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LIsDigit));	
			}
			else
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif

				string LString = AArguments[0].Value.AsString;
				int LIndex = AArguments[1].Value.AsInt32;
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, false));

				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Char.IsDigit(LString, LIndex)));
			}
		}
	}
	
	// operator System.IsLetterOrDigit(const AValue : String) : Boolean;	
	// operator System.IsLetterOrDigit(const AValue : String, const AIndex : Integer) : Boolean;
	public class StringIsLetterOrDigitNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif

				string LValue = AArguments[0].Value.AsString;
				bool LIsLetterOrDigit = true;
				for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
					if (!Char.IsLetterOrDigit(LValue, LIndex))
					{
						LIsLetterOrDigit = false;
						break;
					}
					
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LIsLetterOrDigit));	
			}
			else
			{
				#if NILPROPOGATION
				if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				#endif

				string LString = AArguments[0].Value.AsString;
				int LIndex = AArguments[1].Value.AsInt32;
				if ((LIndex < 0) || (LIndex >= LString.Length))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, false));

				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Char.IsLetterOrDigit(LString, LIndex)));
			}
		}
	}
	
	#if USEISTRING
	// operator System.EnsureUpper(var AValue : IString);
	public class IStringEnsureUpperNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value != null) && !AArguments[0].Value.IsNil)
				AArguments[0].Value = new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.ToUpper());
			return null;
		}
	}
	
	// operator System.EnsureLower(var AValue : IString);
	public class IStringEnsureLowerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value != null) && !AArguments[0].Value.IsNil)
				AArguments[0].Value = new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.ToLower());
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
