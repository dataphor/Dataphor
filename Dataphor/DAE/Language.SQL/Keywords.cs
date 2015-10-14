/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.SQL
{
	using System;
	using System.Collections;
	using System.Reflection;
	
    /// <summary>SQL keywords</summary>    
    public class Keywords
    {
        public const string Select = "select";
        public const string Insert = "insert";
        public const string Update = "update";
        public const string Delete = "delete";
        public const string Old = "old";
        public const string New = "new";
        public const string Where = "where";
		public const string Having = "having";
		public const string Group = "group";
        public const string Order = "order";
        public const string By = "by";
        public const string On = "on";
        public const string As = "as";
        public const string Union = "union";
        public const string Intersect = "intersect";
        public const string Minus = "minus";
        public const string Except = "except";
        public const string Cross = "cross";
        public const string Inner = "inner";
        public const string Outer = "outer";
        public const string Left = "left";
        public const string Right = "right";
        public const string Full = "full";
        public const string Join = "join";
        public const string In = "in";
        public const string Or = "or";
        public const string Xor = "xor";
        public const string Like = "like";
        public const string Between = "between";
        public const string Matches = "matches";
        public const string And = "and";
        public const string Not = "not";
        public const string Exists = "exists";
        public const string Is = "is";
        public const string Null = "null";
        public const string BitwiseAnd = "&";
        public const string BitwiseOr = "|";
        public const string BitwiseXor = "^";
        public const string ShiftLeft = "<<";
        public const string ShiftRight = ">>";
        public const string Equal = "=";
        public const string NotEqual = "<>";
        public const string Less = "<";
        public const string Greater = ">";
        public const string InclusiveLess = "<=";
        public const string InclusiveGreater = ">=";
        public const string Addition = "+";
        public const string Subtraction = "-";
        public const string Multiplication = "*";
        public const string Division = "/";
        public const string Modulus = "%";
        public const string BitwiseNot = "~";
        public const string BeginGroup = "(";
        public const string EndGroup = ")";
        public const string From = "from";
        public const string When = "when";
        public const string Then = "then";
        public const string Else = "else";
        public const string Case = "case";
        public const string End = "end";
        public const string Qualifier = ".";
        public const string Star = "*";
        public const string ListSeparator = ",";
        public const string StatementTerminator = ";";
        public const string Distinct = "distinct";
        public const string All = "all";
        public const string Asc = "asc";
        public const string Desc = "desc";
        public const string Nulls = "nulls";
        public const string First = "first";
        public const string Last = "last";
        public const string Into = "into";
        public const string Set = "set";
        public const string Primary = "primary";
        public const string Foreign = "foreign";
        public const string Key = "key";
        public const string Create = "create";
        public const string Alter = "alter";
        public const string Drop = "drop";
        public const string Add = "add";
        public const string Table = "table";
        public const string View = "view";
        public const string Column = "column";
        public const string Index = "index";
        public const string Unique = "unique";
        public const string Values = "values";
        public const string Check = "check";
        public const string Constraint = "constraint";
        public const string References = "references";
        public const string Value = "value";
        public const string Cascade = "cascade";
        public const string Default = "default";
        public const string Clustered = "clustered";
        
        private static string[] _keywords;
        
        private static void PopulateKeywords()
        {
			FieldInfo[] fields = typeof(Keywords).GetFields();

			int fieldCount = 0;
			foreach (FieldInfo field in fields)
				if (field.FieldType.Equals(typeof(string)) && field.IsLiteral)
					fieldCount++;

			_keywords = new string[fieldCount];

			int fieldCounter = 0;
			foreach (FieldInfo field in fields)
				if (field.FieldType.Equals(typeof(string)) && field.IsLiteral)
				{
					_keywords[fieldCounter] = (string)field.GetValue(null);
					fieldCounter++;
				}
        }
        
        public static bool Contains(string identifier)
        {
			if (_keywords == null)
				PopulateKeywords();
				
			return ((IList)_keywords).Contains(identifier);
        }
    }
}
