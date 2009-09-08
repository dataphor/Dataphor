/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language
{
    using System;
    using System.Text;
    using System.Collections;
    using System.ComponentModel;
    
    using Alphora.Dataphor;
    
    /*
		Class Hierarchy ->
		
			Statement
				|- Expression
				|	|- UnaryExpression
				|	|- BinaryExpression
				|	|- ValueExpression
				|	|- ParameterExpression
				|	|- IdentifierExpression
				|   |- ListExpression
				|	|- CallExpression
				|	|- IfExpression
				|	|- BetweenExpression
				|	|- CaseExpression
				|	|- CaseItemExpression
				|	|- CaseElseExpression
				|- Block
				|	|- DelimitedBlock
				|- IfStatement
    */
    
    public class LineInfo
    {
		public LineInfo() : this(-1, -1, -1, -1) { }
		public LineInfo(int ALine, int ALinePos, int AEndLine, int AEndLinePos)
		{
			Line = ALine;
			LinePos = ALinePos;
			EndLine = AEndLine;
			EndLinePos = AEndLinePos;
		}
		
		public LineInfo(LineInfo ALineInfo)
		{
			SetFromLineInfo(ALineInfo == null ? StartingOffset : ALineInfo);
		}
		
		public int Line;
		public int LinePos;
		public int EndLine;
		public int EndLinePos;
		
		public void SetFromLineInfo(LineInfo ALineInfo)
		{
			Line = ALineInfo.Line;
			LinePos = ALineInfo.LinePos;
			EndLine = ALineInfo.EndLine;
			EndLinePos = ALineInfo.EndLinePos;
		}

		public static LineInfo Empty = new LineInfo();		
		public static LineInfo StartingOffset = new LineInfo(0, 0, 0, 0);
		public static LineInfo StartingLine = new LineInfo(1, 1, 1, 1);
    }
    
	[Serializable]
	public abstract class Statement : Object 
	{
		public Statement() : base() {}
		public Statement(Lexer ALexer) : base()
		{
			SetPosition(ALexer);
		}
		
		// The line and position of the starting token for this element in the syntax tree
		private LineInfo FLineInfo;
		public LineInfo LineInfo { get { return FLineInfo; } }
		
		public int Line
		{
			get { return FLineInfo == null ? -1 : FLineInfo.Line; }
			set
			{
				if (FLineInfo == null)
					FLineInfo = new LineInfo();
				FLineInfo.Line = value;
			}
		}
		
		public int LinePos
		{
			get { return FLineInfo == null ? -1 : FLineInfo.LinePos; }
			set
			{
				if (FLineInfo == null)
					FLineInfo = new LineInfo();
				FLineInfo.LinePos = value;
			}
		}
		
		public int EndLine
		{
			get { return FLineInfo == null ? -1 : FLineInfo.EndLine; }
			set
			{
				if (FLineInfo == null)
					FLineInfo = new LineInfo();
				FLineInfo.EndLine = value;
			}
		}
		
		public int EndLinePos
		{
			get { return FLineInfo == null ? -1 : FLineInfo.EndLinePos; }
			set
			{
				if (FLineInfo == null)
					FLineInfo = new LineInfo();
				FLineInfo.EndLinePos = value;
			}
		}
		
		public void SetPosition(Lexer ALexer)
		{
			Line = ALexer[0, false].Line;
			LinePos = ALexer[0, false].LinePos;
		}
		
		public void SetEndPosition(Lexer ALexer)
		{
			EndLine = ALexer[0, false].Line;
			LexerToken LToken = ALexer[0, false];
			EndLinePos = LToken.LinePos + (LToken.Token == null ? 0 : LToken.Token.Length);
		}
		
		public void SetLineInfo(LineInfo ALineInfo)
		{
			if (ALineInfo != null)
			{
				Line = ALineInfo.Line;
				LinePos = ALineInfo.LinePos;
				EndLine = ALineInfo.EndLine;
				EndLinePos = ALineInfo.EndLinePos;
			}
		}

		private LanguageModifiers FModifiers;
		public LanguageModifiers Modifiers 
		{ 
			get { return FModifiers; } 
			set { FModifiers = value; } 
		}
	}
	
	public class EmptyStatement : Statement {}
    
	#if USENOTIFYLISTFORMETADATA
    public class Statements : NotifyList
    #else
    public class Statements : List
    #endif
    {
		public new Statement this[int AIndex]
		{
			get { return (Statement)base[AIndex]; }
			set	{ base[AIndex] = value; }
		}
    }
    
    public abstract class Expression : Statement {}

	[Serializable]    
	public enum Modifier : byte {In, Var, Out, Const}
	
	public class ParameterExpression : Expression
	{
		public ParameterExpression() : base(){}
		public ParameterExpression(Modifier AModifier, Expression AExpression)
		{
			FModifier = AModifier;
			FExpression = AExpression;
		}
		
		protected Modifier FModifier;
		public Modifier Modifier
		{
			get	{ return FModifier; }
			set { FModifier = value; }
		}
		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
    public class Expressions : Statements
    {		
		public new Expression this[int AIndex]
		{
			get { return (Expression)base[AIndex]; }
			set { base[AIndex] = value; }
		}
    }
    
    public class UnaryExpression : Expression
    {
        public UnaryExpression() : base(){}
        public UnaryExpression(string AInstruction, Expression AExpression) : base()
        {
            Instruction = AInstruction;
            Expression = AExpression;
        }
        
        // Instruction
        protected string FInstruction = String.Empty;
        public string Instruction
        {
            get { return FInstruction; }
            set { FInstruction = value; }
        }
        
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
    }
    
    public class BinaryExpression : Expression
    {
        public BinaryExpression() : base(){}
        public BinaryExpression(Expression ALeftExpression, string AInstruction, Expression ARightExpression) : base()
        {
            LeftExpression = ALeftExpression;
            Instruction = AInstruction;
            RightExpression = ARightExpression;
        }
    
        // Instruction
        protected string FInstruction = String.Empty;
        public string Instruction
        {
            get { return FInstruction; }
            set { FInstruction = value; }
        }
        
        // LeftExpression
        private Expression FLeftExpression;
        public Expression LeftExpression
        {
            get { return FLeftExpression; }
            set { FLeftExpression = value; }
        }

        // RightExpression        
        private Expression FRightExpression;
        public Expression RightExpression
        {
            get { return FRightExpression; }
            set { FRightExpression = value; }
        }
    }
    
    public class QualifierExpression : Expression
    {
        public QualifierExpression() : base(){}
        public QualifierExpression(Expression ALeftExpression, Expression ARightExpression) : base()
        {
            LeftExpression = ALeftExpression;
            RightExpression = ARightExpression;
        }
    
        // LeftExpression
        private Expression FLeftExpression;
        public Expression LeftExpression
        {
            get { return FLeftExpression; }
            set { FLeftExpression = value; }
        }

        // RightExpression        
        private Expression FRightExpression;
        public Expression RightExpression
        {
            get { return FRightExpression; }
            set { FRightExpression = value; }
        }
    }
    
    public class ValueExpression : Expression
    {
        public ValueExpression() : base(){}
        public ValueExpression(object AValue) : base()
        {
            Value = AValue;
            if (AValue is decimal)
				Token = TokenType.Decimal;
			else if (AValue is long)
				Token = TokenType.Integer;
			else if (AValue is int)
			{
				Value = Convert.ToInt64(AValue);
				Token = TokenType.Integer;
			}
			else if (AValue is double)
				Token = TokenType.Float;
			else if (AValue is bool)
				Token = TokenType.Boolean;
			else if (AValue is string)
				Token = TokenType.String;
        }
        
        public ValueExpression(object AValue, TokenType AToken) : base()
        {
			Value = AValue;
			Token = AToken;
        }
        
        // Value
        private object FValue;
        public object Value
        {
            get { return FValue; }
            set { FValue = value; }
        }
        
        // Token
        private TokenType FToken;
        public TokenType Token
        {
			get { return FToken; }
			set { FToken = value; }
        }
    }
    
    public class IdentifierExpression : Expression
    {
        public IdentifierExpression() : base(){}
        public IdentifierExpression(string AIdentifier) : base()
        {
            Identifier = AIdentifier;
        }
        
        // Identifier
        protected string FIdentifier = String.Empty;
        public string Identifier
        {
            get { return FIdentifier; }
            set { FIdentifier = value; }
        }
    }
    
    public class ListExpression : Expression
    {
        public ListExpression() : base()
        {
            FExpressions = new Expressions();
        }
        
        public ListExpression(Expression[] AExpressions) : base()
        {
			FExpressions = new Expressions();
			FExpressions.AddRange(AExpressions);
        }
        
        // Expressions
        protected Expressions FExpressions;
        public Expressions Expressions { get { return FExpressions; } }
    }
    
    public class IndexerExpression : Expression
    {
        // Expression
        private Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Indexer
        public Expression Indexer
        {
			get { return FExpressions[0]; }
			set 
			{
				if (FExpressions.Count == 0)
					FExpressions.Add(value);
				else
					FExpressions[0] = value;
			}
        }
        
        // Expressions
        protected Expressions FExpressions = new Expressions();
        public Expressions Expressions { get { return FExpressions; } }
    }
   
    public class CallExpression : Expression
    {
        public CallExpression() : base()
        {
            FExpressions = new Expressions();
        }
        
        public CallExpression(string AIdentifier, Expression[] AArguments) : base()
        {
            FExpressions = new Expressions();
            FExpressions.AddRange(AArguments);
            Identifier = AIdentifier;
        }
        
        // Identifier
        protected string FIdentifier = String.Empty;
        public string Identifier
        {
            get { return FIdentifier; }
            set { FIdentifier = value; }
        }
        
        // Expressions
        protected Expressions FExpressions;
        public Expressions Expressions { get { return FExpressions; } }
    }
   
    public class IfExpression : Expression
    {
        public IfExpression() : base(){}
        public IfExpression(Expression AExpression, Expression ATrueExpression, Expression AFalseExpression) : base()
        {
            Expression = AExpression;
            TrueExpression = ATrueExpression;
            FalseExpression = AFalseExpression;
        }
        
        // Expression
        private Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }

        // TrueExpression
        private Expression FTrueExpression;
        public Expression TrueExpression
        {
            get { return FTrueExpression; }
            set { FTrueExpression = value; }
        }
        
        // FalseExpression
        private Expression FFalseExpression;
        public Expression FalseExpression
        {
            get { return FFalseExpression; }
            set { FFalseExpression = value; }
        }
    }
    
    public class CaseExpression : Expression
    {		
        public CaseExpression() : base(){}
        public CaseExpression(CaseItemExpression[] AItems, Expression AElseExpression) : base()
        {
			FCaseItems.AddRange(AItems);
			FElseExpression = AElseExpression;
        }
        
        public CaseExpression(Expression AExpression, CaseItemExpression[] AItems, Expression AElseExpression)
        {
			FExpression = AExpression;
			FCaseItems.AddRange(AItems);
			FElseExpression = AElseExpression;
        }
        
        // Expression        
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // CaseItems
        protected CaseItemExpressions FCaseItems = new CaseItemExpressions();
        public CaseItemExpressions CaseItems { get { return FCaseItems; } }
        
        // ElseExpression
        protected Expression FElseExpression;
        public Expression ElseExpression
        {
            get { return FElseExpression; }
            set { FElseExpression = value; }
        }
    }
    
    public class CaseItemExpression : Expression
    {
        public CaseItemExpression() : base(){}
        public CaseItemExpression(Expression AWhenExpression, Expression AThenExpression) : base()
        {
            WhenExpression = AWhenExpression;
            ThenExpression = AThenExpression;
        }
    
        // WhenExpression
        private Expression FWhenExpression;
        public Expression WhenExpression
        {
            get { return FWhenExpression; }
            set { FWhenExpression = value; }
        }

        // ThenExpression        
        private Expression FThenExpression;
        public Expression ThenExpression
        {
            get { return FThenExpression; }
            set { FThenExpression = value; }
        }
    }
    
    public class CaseItemExpressions : Expressions
    {		
		protected override void Validate(object AItem)
		{
			if (!(AItem is CaseItemExpression))
				throw new LanguageException(LanguageException.Codes.CaseItemExpressionContainer);
			base.Validate(AItem);
		}
		
		public new CaseItemExpression this[int AIndex]
		{
			get { return (CaseItemExpression)base[AIndex]; }
			set { base[AIndex] = value; }
		}
    }
    
    public class CaseElseExpression : Expression
    {
		public CaseElseExpression() : base(){}
		public CaseElseExpression(Expression AExpression) : base()
		{
			Expression = AExpression;
		}
		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
    }
    
    public class BetweenExpression : Expression
    {
		public BetweenExpression() : base(){}
		public BetweenExpression(Expression AExpression, Expression ALowerExpression, Expression AUpperExpression) : base()
		{
			FExpression = AExpression;
			FLowerExpression = ALowerExpression;
			FUpperExpression = AUpperExpression;
		}
		
		private Expression FExpression;
		public Expression Expression { get { return FExpression; } set { FExpression = value; } }
		
		private Expression FLowerExpression;
		public Expression LowerExpression { get { return FLowerExpression; } set { FLowerExpression = value; } }
		
		private Expression FUpperExpression;
		public Expression UpperExpression { get { return FUpperExpression; } set { FUpperExpression = value; } }
    }
    
	public class IfStatement : Statement
    {
        public IfStatement() : base(){}
        public IfStatement(Expression AExpression, Statement ATrueStatement, Statement AFalseStatement)
        {
            Expression = AExpression;
            TrueStatement = ATrueStatement;
            FalseStatement = AFalseStatement;
        }

        // Expression        
        private Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // TrueStatement
        private Statement FTrueStatement;
        public Statement TrueStatement
        {
            get { return FTrueStatement; }
            set { FTrueStatement = value; }
        }

        // FalseStatement        
        private Statement FFalseStatement;
        public Statement FalseStatement
        {
            get { return FFalseStatement; }
            set { FFalseStatement = value; }
        }
    }
    
    public class CaseStatement : Statement
    {		
        public CaseStatement() : base(){}
        public CaseStatement(CaseItemStatement[] AItems, Statement AElseStatement) : base()
        {
			FCaseItems.AddRange(AItems);												
			FElseStatement = AElseStatement;
        }
        
        public CaseStatement(Expression AExpression, CaseItemStatement[] AItems, Statement AElseStatement)
        {
			FExpression = AExpression;
			FCaseItems.AddRange(AItems);
			FElseStatement = AElseStatement;
        }
        
        // Expression        
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // CaseItems
        protected CaseItemStatements FCaseItems = new CaseItemStatements();
        public CaseItemStatements CaseItems { get { return FCaseItems; } }
        
        // ElseStatement
        protected Statement FElseStatement;
        public Statement ElseStatement
        {
            get { return FElseStatement; }
            set { FElseStatement = value; }
        }
    }
    
    public class CaseItemStatement : Statement
    {
        public CaseItemStatement() : base(){}
        public CaseItemStatement(Expression AWhenExpression, Statement AThenStatement) : base()
        {
            WhenExpression = AWhenExpression;
            ThenStatement = AThenStatement;
        }
    
        // WhenExpression
        private Expression FWhenExpression;
        public Expression WhenExpression
        {
            get { return FWhenExpression; }
            set { FWhenExpression = value; }
        }

        // ThenStatement        
        private Statement FThenStatement;
        public Statement ThenStatement
        {
            get { return FThenStatement; }
            set { FThenStatement = value; }
        }
    }
    
    public class CaseItemStatements : Statements
    {		
		protected override void Validate(object AItem)
		{
			if (!(AItem is CaseItemStatement))
				throw new LanguageException(LanguageException.Codes.CaseItemExpressionContainer);
			base.Validate(AItem);
		}
		
		public new CaseItemStatement this[int AIndex]
		{
			get { return (CaseItemStatement)base[AIndex]; }
			set { base[AIndex] = value; }
		}
    }
    
    public class Block : Statement
    {
		public Block() : base(){}
		
		// Statements
		protected Statements FStatements = new Statements();
		public Statements Statements { get { return FStatements; } }
    }
    
    public class DelimitedBlock : Block{}
    
    public class ExitStatement : Statement{}
    
    public class WhileStatementBase : Statement
    {
		public WhileStatementBase() : base() {}

		protected Expression FCondition;
		public Expression Condition
		{
			get { return FCondition; }
			set { FCondition = value; }
		}
		
		protected Statement FStatement;
		public Statement Statement
		{
			get { return FStatement; }
			set { FStatement = value; }
		}
    }
    
    public class WhileStatement : WhileStatementBase {}
    
    public class DoWhileStatement : WhileStatementBase {}
    
    public class BreakStatement : Statement{}
    
    public class ContinueStatement : Statement{}
    
    public class RaiseStatement : Statement
    {
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
    }
    
    public class TryFinallyStatement : Statement
    {
		protected Statement FTryStatement;
		public Statement TryStatement
		{
			get { return FTryStatement; }
			set { FTryStatement = value; }
		}
		
		protected Statement FFinallyStatement;
		public Statement FinallyStatement
		{
			get { return FFinallyStatement; }
			set { FFinallyStatement = value; }
		}
    }
    
    public class GenericErrorHandler : Statement
    {
		public GenericErrorHandler() : base(){}
		public GenericErrorHandler(Statement AStatement) : base() 
		{
			FStatement = AStatement;
		}
		
		protected Statement FStatement;
		public Statement Statement
		{
			get { return FStatement; }
			set { FStatement = value; }
		}
    }
    
	public class SpecificErrorHandler : GenericErrorHandler
	{
		public SpecificErrorHandler(string AErrorTypeName) : base()
		{
			FErrorTypeName = AErrorTypeName;
		}
		
		protected string FErrorTypeName = String.Empty;
		public string ErrorTypeName
		{
			get { return FErrorTypeName; }
			set { FErrorTypeName = value == null ? String.Empty : value; }
		}
	}
    
	public class ParameterizedErrorHandler : SpecificErrorHandler
	{
		public ParameterizedErrorHandler(string AErrorTypeName, string AVariableName) : base(AErrorTypeName)
		{
			FVariableName = AVariableName;
		}
		
		protected string FVariableName = String.Empty;
		public string VariableName
		{
			get { return FVariableName; }
			set { FVariableName = value == null ? String.Empty : value; }
		}
	}
    
	public class ErrorHandlers : Statements
	{		
		protected override void Validate(object AItem)
		{
			if (!(AItem is GenericErrorHandler))
				throw new LanguageException(LanguageException.Codes.ErrorHandlerContainer);
			base.Validate(AItem);
		}
		
		public new GenericErrorHandler this[int AIndex]
		{
			get { return (GenericErrorHandler)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class TryExceptStatement : Statement
    {
		protected Statement FTryStatement;
		public Statement TryStatement
		{
			get { return FTryStatement; }
			set { FTryStatement = value; }
		}
		
		protected ErrorHandlers FErrorHandlers = new ErrorHandlers();
		public ErrorHandlers ErrorHandlers { get { return FErrorHandlers; } }
    }
}
