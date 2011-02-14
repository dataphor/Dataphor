/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System.Reflection;
using System.Collections;

namespace Alphora.Dataphor.DAE.Language.D4
{
    /// <summary>DAE instruction set</summary>    
    public sealed class Instructions
    {
        public const string Select = "iSelect";
        public const string Insert = "iInsert";
        public const string Update = "iUpdate";
        public const string Delete = "iDelete";
        public const string UpdateCondition = "iUpdateCondition"; // update or delete condition
        public const string Retrieve = "iRetrieve";
        public const string Restrict = "iRestrict";
        public const string Project = "iProject";
        public const string Extend = "iExtend";
        public const string Rename = "iRename";
        public const string Remove = "iRemove";
        public const string Aggregate = "iAggregate";
        public const string Order = "iOrder";
        public const string Copy = "iCopy";
        public const string Browse = "iBrowse";
        public const string Quota = "iQuota";
        public const string Explode = "iExplode";
        public const string On = "iOn";
        public const string Adorn = "iAdorn";
        public const string Redefine = "iRedefine";
        public const string Union = "iUnion";
        public const string Intersect = "iIntersect";
        public const string Difference = "iDifference";
        public const string Product = "iProduct";
        public const string Divide = "iDivide";
        public const string Join = "iJoin";
        public const string Lookup = "iLookup";
        public const string LeftJoin = "iLeftJoin";
        public const string LeftLookup = "iLeftLookup";
        public const string RightJoin = "iRightJoin";
        public const string RightLookup = "iRightLookup";
        public const string Having = "iHaving";
        public const string Without = "iWithout";
        public const string ExtractRow = "iExtractRow";
        public const string ExtractEntry = "iExtractEntry";
        public const string ExtractColumn = "iExtractColumn";
        public const string If = "iIf";
        public const string Condition = "iCondition";
        public const string Case = "iCase";
        public const string List = "iList";
        public const string Row = "iRow";
        public const string Entry = "iEntry";
        public const string Table = "iTable";
        public const string Presentation = "iPresentation";
        public const string Cursor = "iCursor";
        public const string Not = "iNot";
        public const string And = "iAnd";
        public const string Or = "iOr";
        public const string Xor = "iXor";
        public const string Like = "iLike";
        public const string Matches = "iMatches";
        public const string Between = "iBetween";
        public const string In = "iIn";
        public const string Exists = "iExists";
        public const string BitwiseNot = "iBitwiseNot";
        public const string BitwiseAnd = "iBitwiseAnd";
        public const string BitwiseOr = "iBitwiseOr";
        public const string BitwiseXor = "iBitwiseXor";
        public const string ShiftLeft = "iShiftLeft";
        public const string ShiftRight = "iShiftRight";
        public const string Equal = "iEqual";
        public const string NotEqual = "iNotEqual";
        public const string Less = "iLess";
        public const string Greater = "iGreater";
        public const string InclusiveLess = "iInclusiveLess";
        public const string InclusiveGreater = "iInclusiveGreater";
        public const string Compare = "iCompare";
        public const string Addition = "iAddition";
        public const string Subtraction = "iSubtraction";
        public const string Multiplication = "iMultiplication";
        public const string Division = "iDivision";
        public const string Div = "iDiv";
        public const string Mod = "iMod";
        public const string Negate = "iNegate";
        public const string Power = "iPower";
        public const string Indexer = "iIndexer";
        public const string Qualifier = "iQualifier";
        public const string Parent = "iParent";
        public const string CreateTable = "iCreateTable";
        public const string CreateView = "iCreateView";
        public const string CreateScalarType = "iCreateScalarType";
        public const string CreateOperator = "iCreateOperator";
        public const string CreateAggregateOperator = "iCreateAggregateOperator";
        public const string CreateReference = "iCreateReference";
        public const string CreateConstraint = "iCreateConstraint";
        public const string CreateServer = "iCreateServer";
        public const string AlterServer = "iAlterServer";
        public const string DropServer = "iDropServer";
        public const string CreateDevice = "iCreateDevice";
        public const string AlterTable = "iAlterTable";
        public const string AlterView = "iAlterView";
        public const string AlterScalarType = "iAlterScalarType";
        public const string AlterOperator = "iAlterOperator";
        public const string AlterAggregateOperator = "iAlterAggregateOperator";
        public const string AlterReference = "iAlterReference";
        public const string AlterConstraint = "iAlterConstraint";
        public const string AlterDevice = "iAlterDevice";
        public const string DropTable = "iDropTable";
        public const string DropView = "iDropView";
        public const string DropScalarType = "iDropScalarType";
        public const string DropOperator = "iDropOperator";
        public const string DropReference = "iDropReference";
        public const string DropConstraint = "iDropConstraint";
        public const string DropDevice = "iDropDevice";
        public const string Declare = "iDeclare";
        public const string Assign = "iAssign";
        public const string Parameter = "iParameter";
        public const string BeginBlock = "iBeginBlock";
        public const string EndBlock = "iEndBlock";
        public const string EndStatement = "iEndStatement";
        public const string Exit = "iExit";
		public const string While = "iWhile";
		public const string DoWhile = "iDoWhile";
		public const string Break = "iBreak";
		public const string Continue = "iContinue";
		public const string TryFinally = "iTryFinally";
		public const string TryExcept = "iTryExcept";
		public const string Raise = "iRaise";
		public const string ReRaise = "iReRaise";
		public const string ToPresentation = "iToPresentation";
		
        private static string[] _instructions;
        
        private static void PopulateInstructions()
        {
			FieldInfo[] fields = typeof(Instructions).GetFields();

			int fieldCount = 0;
			foreach (FieldInfo field in fields)
				if (field.FieldType.Equals(typeof(string)) && field.IsLiteral)
					fieldCount++;

			_instructions = new string[fieldCount];

			int fieldCounter = 0;
			foreach (FieldInfo field in fields)
				if (field.FieldType.Equals(typeof(string)) && field.IsLiteral)
				{
					_instructions[fieldCounter] = (string)field.GetValue(null);
					fieldCounter++;
				}
        }
        
        public static bool Contains(string identifier)
        {
			if (_instructions == null)
				PopulateInstructions();
				
			return ((IList)_instructions).Contains(identifier);
        }

		public static bool IsLessInstruction(string instruction)
		{
			switch (instruction)
			{
				case Instructions.Less :
				case Instructions.InclusiveLess : return true;
				default : return false;
			}
		}
		
		public static bool IsGreaterInstruction(string instruction)
		{
			switch (instruction)
			{
				case Instructions.Greater :
				case Instructions.InclusiveGreater : return true;
				default : return false;
			}
		}
		
		public static bool IsExclusiveInstruction(string instruction)
		{
			switch (instruction)
			{
				case Instructions.Less :
				case Instructions.Greater : return true;
				default : return false;
			}
		}
	}
}