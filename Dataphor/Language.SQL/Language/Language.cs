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
		public LineInfo(int line, int linePos, int endLine, int endLinePos)
		{
			Line = line;
			LinePos = linePos;
			EndLine = endLine;
			EndLinePos = endLinePos;
		}
		
		public LineInfo(LineInfo lineInfo)
		{
			SetFromLineInfo(lineInfo == null ? StartingOffset : lineInfo);
		}
		
		public int Line;
		public int LinePos;
		public int EndLine;
		public int EndLinePos;
		
		public void SetFromLineInfo(LineInfo lineInfo)
		{
			Line = lineInfo.Line;
			LinePos = lineInfo.LinePos;
			EndLine = lineInfo.EndLine;
			EndLinePos = lineInfo.EndLinePos;
		}

		public static LineInfo Empty = new LineInfo();		
		public static LineInfo StartingOffset = new LineInfo(0, 0, 0, 0);
		public static LineInfo StartingLine = new LineInfo(1, 1, 1, 1);
    }
    
	public abstract class Statement : Object 
	{
		public Statement() : base() {}
		public Statement(Lexer lexer) : base()
		{
			SetPosition(lexer);
		}
		
		// The line and position of the starting token for this element in the syntax tree
		private LineInfo _lineInfo;
		public LineInfo LineInfo { get { return _lineInfo; } }
		
		public int Line
		{
			get { return _lineInfo == null ? -1 : _lineInfo.Line; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.Line = value;
			}
		}
		
		public int LinePos
		{
			get { return _lineInfo == null ? -1 : _lineInfo.LinePos; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.LinePos = value;
			}
		}
		
		public int EndLine
		{
			get { return _lineInfo == null ? -1 : _lineInfo.EndLine; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.EndLine = value;
			}
		}
		
		public int EndLinePos
		{
			get { return _lineInfo == null ? -1 : _lineInfo.EndLinePos; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.EndLinePos = value;
			}
		}
		
		public void SetPosition(Lexer lexer)
		{
			Line = lexer[0, false].Line;
			LinePos = lexer[0, false].LinePos;
		}
		
		public void SetEndPosition(Lexer lexer)
		{
			EndLine = lexer[0, false].Line;
			LexerToken token = lexer[0, false];
			EndLinePos = token.LinePos + (token.Token == null ? 0 : token.Token.Length);
		}
		
		public void SetLineInfo(LineInfo lineInfo)
		{
			if (lineInfo != null)
			{
				Line = lineInfo.Line;
				LinePos = lineInfo.LinePos;
				EndLine = lineInfo.EndLine;
				EndLinePos = lineInfo.EndLinePos;
			}
		}

		private LanguageModifiers _modifiers;
		public LanguageModifiers Modifiers 
		{ 
			get { return _modifiers; } 
			set { _modifiers = value; } 
		}
	}
	
	public class EmptyStatement : Statement {}
    
    public class Statements : List
    {
		public new Statement this[int index]
		{
			get { return (Statement)base[index]; }
			set	{ base[index] = value; }
		}
    }
    
    public abstract class Expression : Statement {}

	public enum Modifier : byte { In, Var, Out, Const }
	
	public class ParameterExpression : Expression
	{
		public ParameterExpression() : base(){}
		public ParameterExpression(Modifier modifier, Expression expression)
		{
			_modifier = modifier;
			_expression = expression;
		}
		
		protected Modifier _modifier;
		public Modifier Modifier
		{
			get	{ return _modifier; }
			set { _modifier = value; }
		}
		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
    public class Expressions : Statements
    {		
		public new Expression this[int index]
		{
			get { return (Expression)base[index]; }
			set { base[index] = value; }
		}
    }
    
    public class UnaryExpression : Expression
    {
        public UnaryExpression() : base(){}
        public UnaryExpression(string instruction, Expression expression) : base()
        {
            Instruction = instruction;
            Expression = expression;
        }
        
        // Instruction
        protected string _instruction = String.Empty;
        public string Instruction
        {
            get { return _instruction; }
            set { _instruction = value; }
        }
        
        // Expression
        protected Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
    }
    
    public class BinaryExpression : Expression
    {
        public BinaryExpression() : base(){}
        public BinaryExpression(Expression leftExpression, string instruction, Expression rightExpression) : base()
        {
            LeftExpression = leftExpression;
            Instruction = instruction;
            RightExpression = rightExpression;
        }
    
        // Instruction
        protected string _instruction = String.Empty;
        public string Instruction
        {
            get { return _instruction; }
            set { _instruction = value; }
        }
        
        // LeftExpression
        private Expression _leftExpression;
        public Expression LeftExpression
        {
            get { return _leftExpression; }
            set { _leftExpression = value; }
        }

        // RightExpression        
        private Expression _rightExpression;
        public Expression RightExpression
        {
            get { return _rightExpression; }
            set { _rightExpression = value; }
        }
    }
    
    public class QualifierExpression : Expression
    {
        public QualifierExpression() : base(){}
        public QualifierExpression(Expression leftExpression, Expression rightExpression) : base()
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
        }
    
        // LeftExpression
        private Expression _leftExpression;
        public Expression LeftExpression
        {
            get { return _leftExpression; }
            set { _leftExpression = value; }
        }

        // RightExpression        
        private Expression _rightExpression;
        public Expression RightExpression
        {
            get { return _rightExpression; }
            set { _rightExpression = value; }
        }
    }
    
    public class ValueExpression : Expression
    {
        public ValueExpression() : base(){}
        public ValueExpression(object tempValue) : base()
        {
            Value = tempValue;
            if (tempValue is decimal)
				Token = TokenType.Decimal;
			else if (tempValue is long)
				Token = TokenType.Integer;
			else if (tempValue is int)
			{
				Value = Convert.ToInt64(tempValue);
				Token = TokenType.Integer;
			}
			else if (tempValue is double)
				Token = TokenType.Float;
			else if (tempValue is bool)
				Token = TokenType.Boolean;
			else if (tempValue is string)
				Token = TokenType.String;
        }
        
        public ValueExpression(object tempValue, TokenType token) : base()
        {
			Value = tempValue;
			Token = token;
        }
        
        // Value
        private object _value;
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }
        
        // Token
        private TokenType _token;
        public TokenType Token
        {
			get { return _token; }
			set { _token = value; }
        }
    }
    
    public class IdentifierExpression : Expression
    {
        public IdentifierExpression() : base(){}
        public IdentifierExpression(string identifier) : base()
        {
            Identifier = identifier;
        }
        
        // Identifier
        protected string _identifier = String.Empty;
        public string Identifier
        {
            get { return _identifier; }
            set { _identifier = value; }
        }
    }
    
    public class ListExpression : Expression
    {
        public ListExpression() : base()
        {
            _expressions = new Expressions();
        }
        
        public ListExpression(Expression[] expressions) : base()
        {
			_expressions = new Expressions();
			_expressions.AddRange(expressions);
        }
        
        // Expressions
        protected Expressions _expressions;
        public Expressions Expressions { get { return _expressions; } }
    }
    
    public class IndexerExpression : Expression
    {
        // Expression
        private Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
        
        // Indexer
        public Expression Indexer
        {
			get { return _expressions[0]; }
			set 
			{
				if (_expressions.Count == 0)
					_expressions.Add(value);
				else
					_expressions[0] = value;
			}
        }
        
        // Expressions
        protected Expressions _expressions = new Expressions();
        public Expressions Expressions { get { return _expressions; } }
    }
   
    public class CallExpression : Expression
    {
        public CallExpression() : base()
        {
            _expressions = new Expressions();
        }
        
        public CallExpression(string identifier, Expression[] arguments) : base()
        {
            _expressions = new Expressions();
            _expressions.AddRange(arguments);
            Identifier = identifier;
        }
        
        // Identifier
        protected string _identifier = String.Empty;
        public string Identifier
        {
            get { return _identifier; }
            set { _identifier = value; }
        }
        
        // Expressions
        protected Expressions _expressions;
        public Expressions Expressions { get { return _expressions; } }
    }
   
    public class IfExpression : Expression
    {
        public IfExpression() : base(){}
        public IfExpression(Expression expression, Expression trueExpression, Expression falseExpression) : base()
        {
            Expression = expression;
            TrueExpression = trueExpression;
            FalseExpression = falseExpression;
        }
        
        // Expression
        private Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }

        // TrueExpression
        private Expression _trueExpression;
        public Expression TrueExpression
        {
            get { return _trueExpression; }
            set { _trueExpression = value; }
        }
        
        // FalseExpression
        private Expression _falseExpression;
        public Expression FalseExpression
        {
            get { return _falseExpression; }
            set { _falseExpression = value; }
        }
    }
    
    public class CaseExpression : Expression
    {		
        public CaseExpression() : base(){}
        public CaseExpression(CaseItemExpression[] items, Expression elseExpression) : base()
        {
			_caseItems.AddRange(items);
			_elseExpression = elseExpression;
        }
        
        public CaseExpression(Expression expression, CaseItemExpression[] items, Expression elseExpression)
        {
			_expression = expression;
			_caseItems.AddRange(items);
			_elseExpression = elseExpression;
        }
        
        // Expression        
        protected Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
        
        // CaseItems
        protected CaseItemExpressions _caseItems = new CaseItemExpressions();
        public CaseItemExpressions CaseItems { get { return _caseItems; } }
        
        // ElseExpression
        protected Expression _elseExpression;
        public Expression ElseExpression
        {
            get { return _elseExpression; }
            set { _elseExpression = value; }
        }
    }
    
    public class CaseItemExpression : Expression
    {
        public CaseItemExpression() : base(){}
        public CaseItemExpression(Expression whenExpression, Expression thenExpression) : base()
        {
            WhenExpression = whenExpression;
            ThenExpression = thenExpression;
        }
    
        // WhenExpression
        private Expression _whenExpression;
        public Expression WhenExpression
        {
            get { return _whenExpression; }
            set { _whenExpression = value; }
        }

        // ThenExpression        
        private Expression _thenExpression;
        public Expression ThenExpression
        {
            get { return _thenExpression; }
            set { _thenExpression = value; }
        }
    }
    
    public class CaseItemExpressions : Expressions
    {		
		protected override void Validate(object item)
		{
			if (!(item is CaseItemExpression))
				throw new LanguageException(LanguageException.Codes.CaseItemExpressionContainer);
			base.Validate(item);
		}
		
		public new CaseItemExpression this[int index]
		{
			get { return (CaseItemExpression)base[index]; }
			set { base[index] = value; }
		}
    }
    
    public class CaseElseExpression : Expression
    {
		public CaseElseExpression() : base(){}
		public CaseElseExpression(Expression expression) : base()
		{
			Expression = expression;
		}
		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
    }
    
    public class BetweenExpression : Expression
    {
		public BetweenExpression() : base(){}
		public BetweenExpression(Expression expression, Expression lowerExpression, Expression upperExpression) : base()
		{
			_expression = expression;
			_lowerExpression = lowerExpression;
			_upperExpression = upperExpression;
		}
		
		private Expression _expression;
		public Expression Expression { get { return _expression; } set { _expression = value; } }
		
		private Expression _lowerExpression;
		public Expression LowerExpression { get { return _lowerExpression; } set { _lowerExpression = value; } }
		
		private Expression _upperExpression;
		public Expression UpperExpression { get { return _upperExpression; } set { _upperExpression = value; } }
    }
    
	public class IfStatement : Statement
    {
        public IfStatement() : base(){}
        public IfStatement(Expression expression, Statement trueStatement, Statement falseStatement)
        {
            Expression = expression;
            TrueStatement = trueStatement;
            FalseStatement = falseStatement;
        }

        // Expression        
        private Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
        
        // TrueStatement
        private Statement _trueStatement;
        public Statement TrueStatement
        {
            get { return _trueStatement; }
            set { _trueStatement = value; }
        }

        // FalseStatement        
        private Statement _falseStatement;
        public Statement FalseStatement
        {
            get { return _falseStatement; }
            set { _falseStatement = value; }
        }
    }
    
    public class CaseStatement : Statement
    {		
        public CaseStatement() : base(){}
        public CaseStatement(CaseItemStatement[] items, Statement elseStatement) : base()
        {
			_caseItems.AddRange(items);												
			_elseStatement = elseStatement;
        }
        
        public CaseStatement(Expression expression, CaseItemStatement[] items, Statement elseStatement)
        {
			_expression = expression;
			_caseItems.AddRange(items);
			_elseStatement = elseStatement;
        }
        
        // Expression        
        protected Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
        
        // CaseItems
        protected CaseItemStatements _caseItems = new CaseItemStatements();
        public CaseItemStatements CaseItems { get { return _caseItems; } }
        
        // ElseStatement
        protected Statement _elseStatement;
        public Statement ElseStatement
        {
            get { return _elseStatement; }
            set { _elseStatement = value; }
        }
    }
    
    public class CaseItemStatement : Statement
    {
        public CaseItemStatement() : base(){}
        public CaseItemStatement(Expression whenExpression, Statement thenStatement) : base()
        {
            WhenExpression = whenExpression;
            ThenStatement = thenStatement;
        }
    
        // WhenExpression
        private Expression _whenExpression;
        public Expression WhenExpression
        {
            get { return _whenExpression; }
            set { _whenExpression = value; }
        }

        // ThenStatement        
        private Statement _thenStatement;
        public Statement ThenStatement
        {
            get { return _thenStatement; }
            set { _thenStatement = value; }
        }
    }
    
    public class CaseItemStatements : Statements
    {		
		protected override void Validate(object item)
		{
			if (!(item is CaseItemStatement))
				throw new LanguageException(LanguageException.Codes.CaseItemExpressionContainer);
			base.Validate(item);
		}
		
		public new CaseItemStatement this[int index]
		{
			get { return (CaseItemStatement)base[index]; }
			set { base[index] = value; }
		}
    }
    
    public class Block : Statement
    {
		public Block() : base(){}
		
		// Statements
		protected Statements _statements = new Statements();
		public Statements Statements { get { return _statements; } }
    }
    
    public class DelimitedBlock : Block{}
    
    public class ExitStatement : Statement{}
    
    public class WhileStatementBase : Statement
    {
		public WhileStatementBase() : base() {}

		protected Expression _condition;
		public Expression Condition
		{
			get { return _condition; }
			set { _condition = value; }
		}
		
		protected Statement _statement;
		public Statement Statement
		{
			get { return _statement; }
			set { _statement = value; }
		}
    }
    
    public class WhileStatement : WhileStatementBase {}
    
    public class DoWhileStatement : WhileStatementBase {}
    
    public class BreakStatement : Statement{}
    
    public class ContinueStatement : Statement{}
    
    public class RaiseStatement : Statement
    {
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
    }
    
    public class TryFinallyStatement : Statement
    {
		protected Statement _tryStatement;
		public Statement TryStatement
		{
			get { return _tryStatement; }
			set { _tryStatement = value; }
		}
		
		protected Statement _finallyStatement;
		public Statement FinallyStatement
		{
			get { return _finallyStatement; }
			set { _finallyStatement = value; }
		}
    }
    
    public class GenericErrorHandler : Statement
    {
		public GenericErrorHandler() : base(){}
		public GenericErrorHandler(Statement statement) : base() 
		{
			_statement = statement;
		}
		
		protected Statement _statement;
		public Statement Statement
		{
			get { return _statement; }
			set { _statement = value; }
		}
    }
    
	public class SpecificErrorHandler : GenericErrorHandler
	{
		public SpecificErrorHandler(string errorTypeName) : base()
		{
			_errorTypeName = errorTypeName;
		}
		
		protected string _errorTypeName = String.Empty;
		public string ErrorTypeName
		{
			get { return _errorTypeName; }
			set { _errorTypeName = value == null ? String.Empty : value; }
		}
	}
    
	public class ParameterizedErrorHandler : SpecificErrorHandler
	{
		public ParameterizedErrorHandler(string errorTypeName, string variableName) : base(errorTypeName)
		{
			_variableName = variableName;
		}
		
		protected string _variableName = String.Empty;
		public string VariableName
		{
			get { return _variableName; }
			set { _variableName = value == null ? String.Empty : value; }
		}
	}
    
	public class ErrorHandlers : Statements
	{		
		protected override void Validate(object item)
		{
			if (!(item is GenericErrorHandler))
				throw new LanguageException(LanguageException.Codes.ErrorHandlerContainer);
			base.Validate(item);
		}
		
		public new GenericErrorHandler this[int index]
		{
			get { return (GenericErrorHandler)base[index]; }
			set { base[index] = value; }
		}
	}
    
	public class TryExceptStatement : Statement
    {
		protected Statement _tryStatement;
		public Statement TryStatement
		{
			get { return _tryStatement; }
			set { _tryStatement = value; }
		}
		
		protected ErrorHandlers _errorHandlers = new ErrorHandlers();
		public ErrorHandlers ErrorHandlers { get { return _errorHandlers; } }
    }
}
