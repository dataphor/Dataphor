/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.D4
{
	using System;
	using System.Collections;
    using System.Reflection;
	
    /// <summary>Dataphor keywords</summary>    
    public sealed class Keywords
    {
		public const string Add = "add";                       
		public const string Addition = "+";                    
		public const string Adorn = "adorn";                   
		public const string After = "after";                   
		public const string Aggregate = "aggregate";           
		public const string Aggregation = "aggregation";       
		public const string All = "all";                       
		public const string Alter = "alter";                   
		public const string And = "and";                       
		public const string Apply = "apply";                   
		public const string As = "as";                         
		public const string Asc = "asc";                       
		public const string Assign = ":=";                     
		public const string Attach = "attach";                 
		public const string Attributes = "attributes";         
		public const string Bag = "bag";                       
		public const string Before = "before";                 
		public const string Begin = "begin";                   
		public const string BeginGroup = "(";                  
		public const string BeginIndexer = "[";                
		public const string BeginList = "{";                   
		public const string Between = "between";               
		public const string BitwiseAnd = "&";                  
		public const string BitwiseNot = "~";                  
		public const string BitwiseOr = "|";                   
		public const string BitwiseXor = "^";                  
		public const string Break = "break";                   
		public const string Browse = "browse";                 
		public const string By = "by";                         
		public const string Capabilities = "capabilities";     
		public const string Cascade = "cascade";               
		public const string Case = "case";                     
		public const string Change = "change";                 
		public const string Class = "class";                   
		public const string Clear = "clear";                   
		public const string Column = "column";                 
		public const string Commit = "commit";                 
		public const string Compare = "?=";                    
		public const string Const = "const";                   
		public const string Constraint = "constraint";         
		public const string Continue = "continue";             
		public const string Conversion = "conversion";         
		public const string Create = "create";                 
		public const string Cursor = "cursor";                 
		public const string Default = "default";               
		public const string Delete = "delete";                 
		public const string Desc = "desc";                     
		public const string Detach = "detach";                 
		public const string Device = "device";                 
		public const string Distinct = "distinct";             
		public const string Div = "div";                       
		public const string Divide = "divide";                 
		public const string Division = "/";                    
		public const string Do = "do";                         
		public const string Downto = "downto";                 
		public const string Drop = "drop";                     
		public const string Dynamic = "dynamic";
		public const string Else = "else";                     
		public const string End = "end";                       
		public const string EndGroup = ")";                    
		public const string EndIndexer = "]";                  
		public const string EndList = "}";                     
		public const string Equal = "=";                       
		public const string Except = "except";                 
		public const string Exclude = "exclude";
		public const string Exists = "exists";                 
		public const string Exit = "exit";                     
		public const string Explode = "explode";               
		public const string False = Tokenizer.False;              
		public const string Finalization = "finalization";     
		public const string Finally = "finally";               
		public const string For = "for";                       
		public const string ForEach = "foreach";
		public const string From = "from";                     
		public const string Generic = "generic";               
		public const string Grant = "grant";                   
		public const string Greater = ">";                     
		public const string Group = "group";                   
		public const string Having = "having";
		public const string If = "if";                         
		public const string In = "in";                         
		public const string Include = "include";               
		public const string InclusiveGreater = ">=";           
		public const string InclusiveLess = "<=";              
		public const string Index = "index";                   
		public const string Indexes = "indexes";               
		public const string Inherited = "inherited";           
		public const string Initialization = "initialization"; 
		public const string Insert = "insert";                 
		public const string Intersect = "intersect";           
		public const string Into = "into";                     
		public const string Invoke = "invoke";
		public const string Is = "is";                         
		public const string Isolation = "isolation";           
		public const string Join = "join";                     
		public const string Key = "key";                       
		public const string Left = "left";                     
		public const string Less = "<";                        
		public const string Level = "level";                   
		public const string Like = "like";                     
		public const string List = "list";                     
		public const string ListSeparator = ",";               
		public const string Lookup = "lookup";                 
		public const string Master = "master";                 
		public const string Matches = "matches";               
		public const string Minus = "minus";                   
		public const string Mod = "mod";                       
		public const string Mode = "mode";                     
		public const string Modify = "modify";                 
		public const string Multiplication = "*";              
		public const string Narrowing = "narrowing";
		public const string New = "new";                       
		public const string Nil = Tokenizer.Nil;
		public const string Not = "not";                       
		public const string NotEqual = "<>";                   
		public const string Of = "of";                         
		public const string Old = "old";                       
		public const string On = "on";                         
		public const string Operator = "operator";             
		public const string Or = "or";                         
		public const string Order = "order";                   
		public const string Origin = "origin";
		public const string Over = "over";                     
		public const string Parent = "parent";                 
		public const string Power = "**";                      
		public const string Qualifier = ".";                   
		public const string Raise = "raise";                   
		public const string Read = "read";                     
		public const string Reconciliation = "reconciliation"; 
		public const string Recursively = "recursively";       
		public const string Redefine = "redefine";             
		public const string Reference = "reference";           
		public const string References = "references";         
		public const string Remove = "remove";                 
		public const string Rename = "rename";                 
		public const string Repeat = "repeat";                 
		public const string Representation = "representation"; 
		public const string Require = "require";               
		public const string Result = "result";                 
		public const string Return = "return";                 
		public const string Revert = "revert";                 
		public const string Revoke = "revoke";                 
		public const string Right = "right";                   
		public const string Role = "role";                     
		public const string Row = "row";                       
		public const string RowExists = "rowexists";           
		public const string Scalar = "scalar";
		public const string Select = "select";                 
		public const string Server = "server";
		public const string Selector = "selector";
		public const string Sequence = "sequence";             
		public const string Session = "session";               
		public const string Set = "set";                       
		public const string ShiftLeft = "<<";                  
		public const string ShiftRight = ">>";                 
		public const string Sort = "sort";                     
		public const string Source = "source";                 
		public const string Special = "special";               
		public const string StatementTerminator = ";";         
		public const string Static = "static";                 
		public const string Step = "step";                     
		public const string Store = "store";                   
		public const string Subtraction = "-";                 
		public const string Table = "table";                   
		public const string Tags = "tags";                     
		public const string Target = "target";                 
		public const string Then = "then";                     
		public const string Times = "times";                   
		public const string To = "to";                         
		public const string Transition = "transition";         
		public const string True = Tokenizer.True;                
		public const string Try = "try";                       
		public const string Type = "type";                     
		public const string TypeOf = "typeof";                 
		public const string TypeSpecifier = ":";               
		public const string Union = "union";                   
		public const string Until = "until";                   
		public const string Update = "update";                 
		public const string Usage = "usage";                   
		public const string User = "user";                     
		public const string Users = "users";                   
		public const string Using = "using";                   
		public const string Validate = "validate";             
		public const string Value = "value";                   
		public const string Var = "var";                       
		public const string View = "view";                     
		public const string When = "when";                     
		public const string Where = "where";                   
		public const string While = "while";        
		public const string Widening = "widening";           
		public const string With = "with";                     
		public const string Without = "without";
		public const string Write = "write";                   
		public const string Xor = "xor";                       
        
        #if VirtualSupport
        public const string Abstract = "abstract";
        public const string Virtual = "virtual";
        public const string Override = "override";
        public const string Reintroduce = "reintroduce";
        #endif

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
    
    public sealed class ReservedWords
    {
		public const string Add = "add";                   
		public const string Adorn = "adorn";               
		public const string Alter = "alter";               
		public const string And = "and";                   
		public const string As = "as";                     
		public const string Attach = "attach";             
		public const string Begin = "begin";               
		public const string Between = "between";           
		public const string Break = "break";               
		public const string Case = "case";                 
		public const string Commit = "commit";             
		public const string Const = "const";               
		public const string Constraint = "constraint";     
		public const string Continue = "continue";         
		public const string Create = "create";             
		public const string Cursor = "cursor";             
		public const string Delete = "delete";             
		public const string Detach = "detach";             
		public const string Div = "div";                   
		public const string Divide = "divide";             
		public const string Do = "do";                     
		public const string Downto = "downto";             
		public const string Drop = "drop";                 
		public const string Else = "else";                 
		public const string End = "end";                   
		public const string Except = "except";             
		public const string Exists = "exists";             
		public const string Exit = "exit";                 
		public const string Explode = "explode";           
		public const string False = Tokenizer.False;
		public const string Finally = "finally";           
		public const string For = "for";                   
		public const string ForEach = "foreach";
		public const string Grant = "grant";               
		public const string Group = "group";               
		public const string Having = "having";
		public const string If = "if";                     
		public const string In = "in";                     
		public const string Include = "include";           
		public const string Inherited = "inherited";       
		public const string Insert = "insert";             
		public const string Intersect = "intersect";       
		public const string Invoke = "invoke";
		public const string Is = "is";                     
		public const string Join = "join";                 
		public const string Key = "key";                   
		//public const string Left = "left";                 
		public const string Like = "like";                 
		public const string List = "list";                 
		public const string Lookup = "lookup";             
		public const string Matches = "matches";           
		public const string Minus = "minus";               
		public const string Mod = "mod";                   
		public const string Not = "not";                   
		public const string Nil = Tokenizer.Nil;
		public const string On = "on";                     
		public const string Or = "or";                     
		public const string Order = "order";  
		public const string Origin = "origin";             
		public const string Over = "over";                 
		//public const string Parent = "parent";             
		public const string Raise = "raise";               
		public const string Read = "read";
		public const string Redefine = "redefine";         
		public const string Reference = "reference";       
		public const string Remove = "remove";             
		public const string Rename = "rename";             
		public const string Repeat = "repeat";             
		public const string Return = "return";             
		public const string Revert = "revert";             
		public const string Revoke = "revoke";             
		//public const string Right = "right";               
		public const string Row = "row";                   
		public const string Select = "select";    
		public const string Selector = "selector";         
		public const string Source = "source";             
		public const string Step = "step";                 
		public const string Table = "table";               
		public const string Tags = "tags";                 
		public const string Target = "target";             
		public const string Times = "times";               
		public const string To = "to";                     
		public const string Transition = "transition";     
		public const string True = Tokenizer.True;
		public const string Try = "try";                   
		public const string TypeOf = "typeof";             
		public const string Union = "union";               
		public const string Until = "until";               
		public const string Update = "update";             
		//public const string Value = "value";               
		public const string Var = "var";                   
		public const string Where = "where";               
		public const string While = "while";               
		public const string With = "with";                 
		public const string Without = "without";
		public const string Write = "write";
		public const string Xor = "xor";                   

        private static string[] _reservedWords;
        
        private static void PopulateReservedWords()
        {
			FieldInfo[] fields = typeof(ReservedWords).GetFields();

			int fieldCount = 0;
			foreach (FieldInfo field in fields)
				if (field.FieldType.Equals(typeof(string)) && field.IsLiteral)
					fieldCount++;

			_reservedWords = new string[fieldCount];

			int fieldCounter = 0;
			foreach (FieldInfo field in fields)
				if (field.FieldType.Equals(typeof(string)) && field.IsLiteral)
				{
					_reservedWords[fieldCounter] = (string)field.GetValue(null);
					fieldCounter++;
				}
        }
        
        public static bool Contains(string identifier)
        {
			if (_reservedWords == null)
				PopulateReservedWords();
				
			return ((IList)_reservedWords).Contains(identifier);
        }
	}
}