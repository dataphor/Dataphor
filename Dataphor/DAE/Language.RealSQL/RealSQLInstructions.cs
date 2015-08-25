/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.RealSQL
{
    /// <summary>RealSQL instruction set</summary>    
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
        public const string Aggregate = "iAggregate";
        public const string Order = "iOrder";
        public const string Union = "iUnion";
        public const string Intersect = "iIntersect";
        public const string Difference = "iDifference";
        public const string Product = "iProduct";
        public const string Join = "iJoin";
        public const string LeftJoin = "iLeftJoin";
        public const string RightJoin = "iRightJoin";
        public const string Case = "iCase";
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
        public const string Parameter = "iParameter";
        public const string EndStatement = "iEndStatement";
	}
}