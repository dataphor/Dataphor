/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.IO;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.SQL;

namespace Alphora.Dataphor.DAE.Language.RealSQL
{
    /*
		HEADER:
		The following non terminals in the Lexer BNF are referenced by the D4 BNF with equivalent meaning:
			<identifier>
			<literal>
			<string>

		The same conventions found in the The Third Manifesto are used here, namely:
			<XYZ list> ::= {<XYZ>}
			<XYZ commalist> ::= [<XYZ>{,<XYZ>}]
			<ne XYZ list> ::= <XYZ>{<XYZ>}
			<ne XYZ commalist> ::= <XYZ>{,<XYZ>}
    */
    
	// given an arbitrary input string, return a syntactically valid RealSQL parse tree
	public class Parser
	{
		/*
			BNF:
			<script> ::=
				<statement>{; <statement>}
		*/
		public Statement ParseScript(string AInput)
		{
			Lexer LLexer = new Lexer(AInput);
			try
			{
				Batch LBatch = new Batch();
				do
				{
					if (LLexer[1].Type == TokenType.EOF)
						break;
					LBatch.Statements.Add(Statement(LLexer));
					if (LLexer[1].Type == TokenType.EOF)
						break;
					else
						LLexer.NextToken().CheckSymbol(Keywords.StatementTerminator);
				} while (true);
				return LBatch;
			}
			catch (Exception E)
			{
				throw new SyntaxException(LLexer, E);
			}
		}
		
        public Statement ParseStatement(string AInput)
        {
			Lexer LLexer = new Lexer(AInput);
			try
			{
				return Statement(LLexer);
			}
			catch (Exception E)
			{
				throw new SyntaxException(LLexer, E);
			}
        }
        
		/*
			BNF:
			<statement> ::=
				<select statement> |
				<insert statement> |
				<update statement> |
				<delete statement> |
				<expression>
		*/
		protected Statement Statement(Lexer ALexer)
		{
			switch (ALexer[1].Token)
			{
				case Keywords.Select: return SelectStatement(ALexer);
				case Keywords.Insert: return InsertStatement(ALexer);
				case Keywords.Update: return UpdateStatement(ALexer);
				case Keywords.Delete: return DeleteStatement(ALexer);
				default: return Expression(ALexer);
			}
		}
		
		/*
			BNF:
			<select statement> ::=
				<query expression>
				[<order by clause>]
		*/
		protected Statement SelectStatement(Lexer ALexer)
		{
			ALexer.NextToken();
			SelectStatement LStatement = new SelectStatement();
			LStatement.QueryExpression = QueryExpression(ALexer);
			if (ALexer[1].Token == Keywords.Order)
				LStatement.OrderClause = OrderClause(ALexer);
			return LStatement;
		}
		
		/*
			BNF:
			<order by clause> ::=
				order by <order column expression commalist>
				
			<order column expression> ::=
				<column identifier> [asc | desc]
				
			<column identifier> ::=
				<qualified identifier>
		*/
		protected OrderClause OrderClause(Lexer ALexer)
		{
			ALexer.NextToken();
			ALexer.NextToken().CheckSymbol(Keywords.By);
			OrderClause LClause = new OrderClause();
			while (true)
			{
				LClause.Columns.Add(OrderFieldExpression(ALexer));
				if (ALexer[1].Token == Keywords.ListSeparator)
					ALexer.NextToken();
				else
					break;
			}
			return LClause;
		}
		
		protected OrderFieldExpression OrderFieldExpression(Lexer ALexer)
		{
			OrderFieldExpression LExpression = new OrderFieldExpression();
			QualifiedFieldExpression(ALexer, LExpression);
			switch (ALexer[1].Token)
			{
				case Keywords.Asc: 
					ALexer.NextToken(); 
					LExpression.Ascending = true; 
				break;

				case Keywords.Desc: ALexer.NextToken(); break;
				default: LExpression.Ascending = true; break;
			}
			return LExpression;
		}
		
		protected void QualifiedFieldExpression(Lexer ALexer, QualifiedFieldExpression AExpression)
		{
			string LIdentifier = Identifier(ALexer);
			if (ALexer[1].Token == Keywords.Qualifier)
			{
				AExpression.TableAlias = LIdentifier;
				ALexer.NextToken();
				AExpression.FieldName = Identifier(ALexer);
			}
			else
				AExpression.FieldName = LIdentifier;
		}

		/*
			BNF:
			<query expression> ::=
				<select expression> [<binary table expression>]

			<binary table expression> ::=
				[union | intersect | minus] <select expression>
		*/
		protected QueryExpression QueryExpression(Lexer ALexer)
		{
			QueryExpression LExpression = new QueryExpression();
			LExpression.SelectExpression = SelectExpression(ALexer);
			bool LDone = false;
			while (!LDone)
			{
				switch (ALexer[1].Token)
				{
					case Keywords.Union:
						ALexer.NextToken();
						ALexer.NextToken();
						LExpression.TableOperators.Add(new TableOperatorExpression(TableOperator.Union, SelectExpression(ALexer)));
					break;

					case Keywords.Intersect:
						ALexer.NextToken();
						ALexer.NextToken();
						LExpression.TableOperators.Add(new TableOperatorExpression(TableOperator.Intersect, SelectExpression(ALexer)));
					break;

					case Keywords.Minus:
						ALexer.NextToken();
						ALexer.NextToken();
						LExpression.TableOperators.Add(new TableOperatorExpression(TableOperator.Difference, SelectExpression(ALexer)));
					break;

					default: 
						LDone = true; 
					break;
				}
			}

			return LExpression;
		}
		
		/*
			BNF:
			<select expression> ::=
				select * | <column expression commalist>
					<from clause>
					[<where clause>]
					[<group by clause>]
					[<having clause>]
		*/
		protected SelectExpression SelectExpression(Lexer ALexer)
		{
			ALexer.NextToken().CheckSymbol(Keywords.Select);
			SelectExpression LExpression = new SelectExpression();
			LExpression.SelectClause = new SelectClause();
			if (ALexer[1].Token == Keywords.Star)
			{
				ALexer.NextToken();
				LExpression.SelectClause.NonProject = true;
			}
			else
			{
				do
				{
					LExpression.SelectClause.Columns.Add(ColumnExpression(ALexer));
					if (ALexer[1].Token == Keywords.ListSeparator)
						ALexer.NextToken();
					else
						break;
				} while (true);
			}

			ALexer.NextToken();
			ALexer.NextToken().CheckSymbol(Keywords.From);
			LExpression.FromClause = FromClause(ALexer);
			
			if (ALexer[1].Token == Keywords.Where)
				LExpression.WhereClause = WhereClause(ALexer);
			
			if (ALexer[1].Token == Keywords.Group)
				LExpression.GroupClause = GroupClause(ALexer);
				
			if (ALexer[1].Token == Keywords.Having)
				LExpression.HavingClause = HavingClause(ALexer);
				
			return LExpression;
		}

		/*
			BNF:
			<column expression> ::=
				<expression> [as <identifier>]
		*/		
		protected ColumnExpression ColumnExpression(Lexer ALexer)
		{
			ColumnExpression LExpression = new ColumnExpression();
			LExpression.Expression = Expression(ALexer);
			if (ALexer[1].Token == Keywords.As)
			{
				ALexer.NextToken();
				LExpression.ColumnAlias = Identifier(ALexer);
			}
			return LExpression;
		}
		
		/*
			BNF:
			<from clause> ::=
				from <table specifier> [<join clause list>]

			<table specifier> ::=
				(<table identifier> | "("<query expression>")") [as <table identifier>]
				
			<table identifier> ::=
				<identifier>

			<join clause> ::=
				[cross | inner | ((left | right) [outer])] join <table specifier> [on <expression>]
		*/
		protected AlgebraicFromClause FromClause(Lexer ALexer)
		{
			AlgebraicFromClause LFromClause = new AlgebraicFromClause();
			if (ALexer[1].Token == Keywords.BeginGroup)
			{
				ALexer.NextToken();
				ALexer.NextToken();
				LFromClause.TableSpecifier = new TableSpecifier(QueryExpression(ALexer));
				ALexer.NextToken().CheckSymbol(Keywords.EndGroup);
			}
			else
				LFromClause.TableSpecifier = new TableSpecifier(new TableExpression(QualifiedIdentifier(ALexer)));

			if (ALexer[1].Token == Keywords.As)
			{
				ALexer.NextToken();
				LFromClause.TableSpecifier.TableAlias = Identifier(ALexer);
			}

			bool LDone = false;
			JoinClause LJoinClause;
			while (!LDone)
			{
				switch (ALexer[1].Token)
				{
					case Keywords.Cross:
						ALexer.NextToken();
						ALexer.NextToken().CheckSymbol(Keywords.Join);
						LJoinClause = new JoinClause();
						LJoinClause.FromClause = FromClause(ALexer);
						LJoinClause.JoinType = JoinType.Cross;
						LFromClause.Joins.Add(LJoinClause);
					break;
					
					case Keywords.Inner:
					case Keywords.Join:
						if (ALexer[1].Token == Keywords.Inner)
							ALexer.NextToken();
						ALexer.NextToken().CheckSymbol(Keywords.Join);
						LJoinClause = new JoinClause();
						LJoinClause.FromClause = FromClause(ALexer);
						LJoinClause.JoinType = JoinType.Inner;
						ALexer.NextToken().CheckSymbol(Keywords.On);
						LJoinClause.JoinExpression = Expression(ALexer);
						LFromClause.Joins.Add(LJoinClause);
					break;
					
					case Keywords.Left:
						ALexer.NextToken();
						if (ALexer[1].Token == Keywords.Outer)
							ALexer.NextToken();
						ALexer.NextToken().CheckSymbol(Keywords.Join);
						LJoinClause = new JoinClause();
						LJoinClause.FromClause = FromClause(ALexer);
						LJoinClause.JoinType = JoinType.Left;
						ALexer.NextToken().CheckSymbol(Keywords.On);
						LJoinClause.JoinExpression = Expression(ALexer);
						LFromClause.Joins.Add(LJoinClause);
					break;
						
					case Keywords.Right:
						ALexer.NextToken();
						if (ALexer[1].Token == Keywords.Outer)
							ALexer.NextToken();
						ALexer.NextToken().CheckSymbol(Keywords.Join);
						LJoinClause = new JoinClause();
						LJoinClause.FromClause = FromClause(ALexer);
						LJoinClause.JoinType = JoinType.Right;
						ALexer.NextToken().CheckSymbol(Keywords.On);
						LJoinClause.JoinExpression = Expression(ALexer);
						LFromClause.Joins.Add(LJoinClause);
					break;
					
					default: LDone = true; break;
				}
			}

			return LFromClause;
		}
		
		/*
			BNF:
			<where clause> ::=
				where <expression>
		*/
		protected WhereClause WhereClause(Lexer ALexer)
		{
			ALexer.NextToken();
			return new WhereClause(Expression(ALexer));
		}
		
		/*
			BNF:
			<group by clause> ::=
				group by <expression commalist>
		*/
		protected GroupClause GroupClause(Lexer ALexer)
		{
			ALexer.NextToken();
			ALexer.NextToken().CheckSymbol(Keywords.By);
			GroupClause LGroupClause = new GroupClause();
			do
			{
				LGroupClause.Columns.Add(Expression(ALexer));
				if (ALexer[1].Token == Keywords.ListSeparator)
					ALexer.NextToken();
				else
					break;
			} while (true);
			return LGroupClause;
		}
		
		/*
			BNF:
			<having clause> ::=
				having <expression>
		*/
		protected HavingClause HavingClause(Lexer ALexer)
		{
			ALexer.NextToken();
			return new HavingClause(Expression(ALexer));
		}
		
		/*
			BNF:
			<insert statement> ::=
				insert into <table identifier>"("<column identifier commalist>")"
					(<values clause> | <query expression>)
					
			<values clause> ::=
				values"("<expression commalist>")"
		*/
		protected Statement InsertStatement(Lexer ALexer)
		{
			ALexer.NextToken();
			ALexer.NextToken().CheckSymbol(Keywords.Into);
			InsertStatement LStatement = new InsertStatement();
			LStatement.InsertClause = new InsertClause();
			LStatement.InsertClause.TableExpression = new TableExpression(QualifiedIdentifier(ALexer));
			ALexer.NextToken().CheckSymbol(Keywords.BeginGroup);
			do
			{
				LStatement.InsertClause.Columns.Add(new InsertFieldExpression(Identifier(ALexer)));
				if (ALexer[1].Token == Keywords.ListSeparator)
					ALexer.NextToken();
				else
					break;
			} while (true);
			ALexer.NextToken().CheckSymbol(Keywords.EndGroup);
			if (ALexer[1].Token == Keywords.Values)
			{
				ValuesExpression LValues = new ValuesExpression();
				ALexer.NextToken();							   
				ALexer.NextToken().CheckSymbol(Keywords.BeginGroup);
				do
				{
					LValues.Expressions.Add(Expression(ALexer));
					if (ALexer[1].Token == Keywords.ListSeparator)
						ALexer.NextToken();
					else
						break;
				} while (true);
				ALexer.NextToken().CheckSymbol(Keywords.EndGroup);
				LStatement.Values = LValues;
			}
			else
			{
				ALexer.NextToken();
				LStatement.Values = QueryExpression(ALexer);
			}

			return LStatement;
		}
		
		/*
			BNF:
			<update statement> ::=
				update <table identifier> 
						set <update column expression commalist>
					[<where clause>]
		*/
		protected Statement UpdateStatement(Lexer ALexer)
		{
			UpdateStatement LStatement = new UpdateStatement();
			ALexer.NextToken();
			LStatement.UpdateClause = new UpdateClause();
			LStatement.UpdateClause.TableExpression = new TableExpression(QualifiedIdentifier(ALexer));
			ALexer.NextToken().CheckSymbol(Keywords.Set);
			do
			{
				LStatement.UpdateClause.Columns.Add(UpdateColumnExpression(ALexer));
				if (ALexer[1].Token == Keywords.ListSeparator)
					ALexer.NextToken();
				else
					break;
			} while (true);
			if (ALexer[1].Token == Keywords.Where)
				LStatement.WhereClause = WhereClause(ALexer);
			return LStatement;
		}
		
		/*
			BNF:
			<update column expression> ::=
				<identifier> = <expression>
		*/
		protected UpdateFieldExpression UpdateColumnExpression(Lexer ALexer)
		{
			UpdateFieldExpression LExpression = new UpdateFieldExpression();
			LExpression.FieldName = Identifier(ALexer);
			ALexer.NextToken().CheckSymbol(Keywords.Equal);
			LExpression.Expression = Expression(ALexer);
			return LExpression;
		}
		
		/*
			BNF:
			<delete statement> ::=
				delete <table identifier>
					[<where clause>]
		*/
		protected Statement DeleteStatement(Lexer ALexer)
		{
			DeleteStatement LStatement = new DeleteStatement();
			ALexer.NextToken();
			LStatement.DeleteClause = new DeleteClause();
			LStatement.DeleteClause.TableExpression = new TableExpression(QualifiedIdentifier(ALexer));
			if (ALexer[1].Token == Keywords.Where)
				LStatement.WhereClause = WhereClause(ALexer);
			return LStatement;
		}
		
        protected bool IsLogicalOperator(string AOperator)
        {
            switch (AOperator)
            {
                case Keywords.In: 
                case Keywords.Or:
                case Keywords.Xor:
                case Keywords.Like: 
                case Keywords.Matches: 
                case Keywords.Between: return true;
                default: return false;
            }
        }
        
		/*
			BNF:
			<expression> ::=
                <logical and expression> <logical operator clause list>
                
            <logical operator clause> ::=
				<logical ternary clause> |
				<logical binary clause>
				
			<logical ternary clause> ::=
				<logical ternary operator> <additive expression> and <additive expression>
				
			<logical ternary operator> ::=
				between
                
            <logical binary clause> ::=
				<logical binary operator> <logical and expression>
                
            <logical binary operator> ::=
                in | or | xor | like | matches
		*/
		protected Expression Expression(Lexer ALexer)
		{
            Expression LExpression = LogicalAndExpression(ALexer);
			while (IsLogicalOperator(ALexer[1].Token))
			{
				if (ALexer[1].Token == Keywords.Between)
				{
					ALexer.NextToken();
					BetweenExpression LBetweenExpression = new BetweenExpression();
					LBetweenExpression.Expression = LExpression;
					LBetweenExpression.LowerExpression = AdditiveExpression(ALexer);
					ALexer.NextToken().CheckSymbol(Keywords.And);
					LBetweenExpression.UpperExpression = AdditiveExpression(ALexer);
					LExpression = LBetweenExpression;
				}
				else
				{
					BinaryExpression LBinaryExpression = new BinaryExpression();
					LBinaryExpression.LeftExpression = LExpression;
					switch (ALexer.NextToken().Token)
					{
						case Keywords.In: LBinaryExpression.Instruction = Instructions.In; break;
						case Keywords.Or: LBinaryExpression.Instruction = Instructions.Or; break;
						case Keywords.Xor: LBinaryExpression.Instruction = Instructions.Xor; break;
						case Keywords.Like: LBinaryExpression.Instruction = Instructions.Like; break;
						case Keywords.Matches: LBinaryExpression.Instruction = Instructions.Matches; break;
					}
					LBinaryExpression.RightExpression = LogicalAndExpression(ALexer);
					LExpression = LBinaryExpression;
				}
			}
			return LExpression;
		}
		
		/* 
			BNF:
            <logical and expression> ::= 
                <bitwise binary expression> {<logical and operator> <bitwise binary expression>}
                
            <logical and operator> ::=
                and
        */
        protected Expression LogicalAndExpression(Lexer ALexer)
        {
            Expression LExpression = BitwiseBinaryExpression(ALexer);
            while (ALexer[1].Token == Keywords.And)
            {
                BinaryExpression LBinaryExpression = new BinaryExpression();
                LBinaryExpression.LeftExpression = LExpression;
                ALexer.NextToken();
                LBinaryExpression.Instruction = Instructions.And;
                LBinaryExpression.RightExpression = BitwiseBinaryExpression(ALexer);
                LExpression = LBinaryExpression;
            }
            return LExpression;
        }
        
        protected bool IsBitwiseBinaryOperator(string AOperator)
        {
            switch (AOperator)
            {
                case Keywords.BitwiseOr:
                case Keywords.BitwiseAnd:
                case Keywords.BitwiseXor:
                case Keywords.ShiftLeft:
                case Keywords.ShiftRight: return true;
                default: return false;
            }
        }
        
		/* 
			BNF:
            <bitwise binary expression> ::= 
                <comparison expression> {<bitwise binary operator> <comparison expression>}
                
            <bitwise binary operator> ::=
                ^ | & | "|" | "<<" | ">>"
        */
        protected Expression BitwiseBinaryExpression(Lexer ALexer)
        {
            Expression LExpression = ComparisonExpression(ALexer);
            while (IsBitwiseBinaryOperator(ALexer[1].Token))
            {
                BinaryExpression LBinaryExpression = new BinaryExpression();
                LBinaryExpression.LeftExpression = LExpression;
                switch (ALexer.NextToken().Token)
                {
					case Keywords.BitwiseXor: LBinaryExpression.Instruction = Instructions.BitwiseXor; break;
					case Keywords.BitwiseAnd: LBinaryExpression.Instruction = Instructions.BitwiseAnd; break;
					case Keywords.BitwiseOr: LBinaryExpression.Instruction = Instructions.BitwiseOr; break;
					case Keywords.ShiftLeft: LBinaryExpression.Instruction = Instructions.ShiftLeft; break;
					case Keywords.ShiftRight: LBinaryExpression.Instruction = Instructions.ShiftRight; break;
                }
                LBinaryExpression.RightExpression = ComparisonExpression(ALexer);
                LExpression = LBinaryExpression;
            }
            return LExpression;
        }
        
        protected bool IsComparisonOperator(string AOperator)
        {
            switch (AOperator)
            {
                case Keywords.Equal:
                case Keywords.NotEqual:
                case Keywords.Less:
                case Keywords.Greater:
                case Keywords.InclusiveLess:
                case Keywords.InclusiveGreater: 
                case Keywords.Compare: return true;
                default: return false;
            }
        }
        
		/* 
			BNF:
            <comparison expression> ::= 
                <additive expression> {<comparison operator> <additive expression>}
                
            <comparison operator> ::=
                = | "<>" | "<" | ">" | "<=" | ">=" | ?=
        */
        protected Expression ComparisonExpression(Lexer ALexer)
        {
            Expression LExpression = AdditiveExpression(ALexer);
            while (IsComparisonOperator(ALexer[1].Token))
            {
                BinaryExpression LBinaryExpression = new BinaryExpression();
                LBinaryExpression.LeftExpression = LExpression;
                switch (ALexer.NextToken().Token)
                {
					case Keywords.Equal: LBinaryExpression.Instruction = Instructions.Equal; break;
					case Keywords.NotEqual: LBinaryExpression.Instruction = Instructions.NotEqual; break;
					case Keywords.Less: LBinaryExpression.Instruction = Instructions.Less; break;
					case Keywords.Greater: LBinaryExpression.Instruction = Instructions.Greater; break;
					case Keywords.InclusiveLess: LBinaryExpression.Instruction = Instructions.InclusiveLess; break;
					case Keywords.InclusiveGreater: LBinaryExpression.Instruction = Instructions.InclusiveGreater; break;
					case Keywords.Compare: LBinaryExpression.Instruction = Instructions.Compare; break;
                }
                LBinaryExpression.RightExpression = AdditiveExpression(ALexer);
                LExpression = LBinaryExpression;
            }
            return LExpression;
        }
       
        protected bool IsAdditiveOperator(string AOperator)
        {
            switch (AOperator)
            {
                case Keywords.Addition:
                case Keywords.Subtraction: return true;
                default: return false;
            }
        }
        
		/* 
			BNF:
            <additive expression> ::= 
                <multiplicative expression> {<additive operator> <multiplicative expression>}
                
            <additive operator> ::=
                + | -
        */
        protected Expression AdditiveExpression(Lexer ALexer)
        {
            Expression LExpression = MultiplicativeExpression(ALexer);
            while (IsAdditiveOperator(ALexer[1].Token))
            {
                BinaryExpression LBinaryExpression = new BinaryExpression();
                LBinaryExpression.LeftExpression = LExpression;
                switch (ALexer.NextToken().Token)
                {
					case Keywords.Addition: LBinaryExpression.Instruction = Instructions.Addition; break;
					case Keywords.Subtraction: LBinaryExpression.Instruction = Instructions.Subtraction; break;
                }
                LBinaryExpression.RightExpression = MultiplicativeExpression(ALexer);
                LExpression = LBinaryExpression;
            }
            return LExpression;
        }
        
        protected bool IsMultiplicativeOperator(string AOperator)
        {
            switch (AOperator)
            {
                case Keywords.Multiplication:
                case Keywords.Division:
                case Keywords.Div:
                case Keywords.Mod: return true;
                default: return false;
            }
        }

		/*                 
			BNF:
            <multiplicative expression> ::= 
                <exponent expression> {<multiplicative operator> <exponent expression>}
                
            <multiplicative operator> ::=
                * | / | div | mod
        */
        protected Expression MultiplicativeExpression(Lexer ALexer)
        {
            Expression LExpression = ExponentExpression(ALexer);
            while (IsMultiplicativeOperator(ALexer[1].Token))
            {
                BinaryExpression LBinaryExpression = new BinaryExpression();
                LBinaryExpression.LeftExpression = LExpression;
                switch (ALexer.NextToken().Token)
                {
					case Keywords.Multiplication: LBinaryExpression.Instruction = Instructions.Multiplication; break;
					case Keywords.Division: LBinaryExpression.Instruction = Instructions.Division; break;
					case Keywords.Div: LBinaryExpression.Instruction = Instructions.Div; break;
					case Keywords.Mod: LBinaryExpression.Instruction = Instructions.Mod; break;
                }
                LBinaryExpression.RightExpression = ExponentExpression(ALexer);
                LExpression = LBinaryExpression;
            }
            return LExpression;
        }

		/* 
			BNF:
            <exponent expression> ::= 
                <unary expression> {<exponent operator> <unary expression>}
                
            <exponent operator> ::=
                **
        */
        protected Expression ExponentExpression(Lexer ALexer)
        {
            Expression LExpression = UnaryExpression(ALexer);
            while (ALexer[1].Token == Keywords.Power)
            {
                BinaryExpression LBinaryExpression = new BinaryExpression();
                LBinaryExpression.LeftExpression = LExpression;
                ALexer.NextToken();
                LBinaryExpression.Instruction = Instructions.Power;
                LBinaryExpression.RightExpression = UnaryExpression(ALexer);
                LExpression = LBinaryExpression;
            }
            return LExpression;
        }
        
        protected bool IsUnaryOperator(string AOperator)
        {
            switch (AOperator)
            {
                case Keywords.Addition: 
                case Keywords.Subtraction:
                case Keywords.BitwiseNot: 
                case Keywords.Not:
                case Keywords.Exists: return true;
                default: return false;
            }
        }

		/* 
			BNF:
			<unary expression> ::=
				{<unary operator>} <qualified factor>
				
			<unary operator> ::=
				+ | - | ~ | not | exists
        */
        protected Expression UnaryExpression(Lexer ALexer)
        {
			if (IsUnaryOperator(ALexer[1].Token))
			{
				switch (ALexer.NextToken().Token)
				{
					case Keywords.Addition: return UnaryExpression(ALexer);
					case Keywords.Subtraction: return new UnaryExpression(Instructions.Negate, UnaryExpression(ALexer));
					case Keywords.BitwiseNot: return new UnaryExpression(Instructions.BitwiseNot, UnaryExpression(ALexer));
					case Keywords.Not: return new UnaryExpression(Instructions.Not, UnaryExpression(ALexer));
					case Keywords.Exists: return new UnaryExpression(Instructions.Exists, UnaryExpression(ALexer));
				}
			}
			return QualifiedFactor(ALexer);
        }
        
		/* 
			BNF:
			<qualified factor> ::=
				(([.]<identifier>) | <qualifier expression>){"["<expression>"]"[.<qualifier expression>]}
        */
        protected Expression QualifiedFactor(Lexer ALexer)
        {
			Expression LExpression;
			if (ALexer[1].Token == Keywords.Qualifier)
			{
				ALexer.NextToken();
				LExpression = new IdentifierExpression(String.Format("{0}{1}", Keywords.Qualifier, Identifier(ALexer)));
			}
			else
				LExpression = QualifierExpression(ALexer);
				
			while (ALexer[1].Token == Keywords.BeginIndexer)
			{
				IndexerExpression LIndexerExpression = new IndexerExpression();
				LIndexerExpression.SetPosition(ALexer);
				LIndexerExpression.Expression = LExpression;
				ALexer.NextToken();
				LIndexerExpression.Indexer = Expression(ALexer);
				ALexer.NextToken().CheckSymbol(Keywords.EndIndexer);
				LExpression = LIndexerExpression;
				
				if (ALexer[1].Token == Keywords.Qualifier)
				{
					ALexer.NextToken();
					QualifierExpression LQualifierExpression = new QualifierExpression();
					LQualifierExpression.SetPosition(ALexer);
					LQualifierExpression.LeftExpression = LExpression;
					LQualifierExpression.RightExpression = QualifierExpression(ALexer);
					LExpression = LQualifierExpression;
				}
			}

			return LExpression;
        }
        
        /*
			BNF:
			<qualifier expression> ::=
				<factor>[.<qualifier expression>]
		*/
        protected Expression QualifierExpression(Lexer ALexer)
        {
			Expression LExpression = Factor(ALexer);
			if (ALexer[1].Token == Keywords.Qualifier)
			{
				ALexer.NextToken();
				QualifierExpression LQualifierExpression = new QualifierExpression();
				LQualifierExpression.SetPosition(ALexer);
				LQualifierExpression.LeftExpression = LExpression;
				LQualifierExpression.RightExpression = QualifierExpression(ALexer);
				return LQualifierExpression;
			}
			return LExpression;
        }
        
        protected bool IsAggregateOperator(string AIdentifier)
        {
			switch (AIdentifier)
			{
				case Keywords.Sum:
				case Keywords.Min:
				case Keywords.Max:
				case Keywords.Avg:
				case Keywords.Count:
					return true;
					
				default:
					return false;
			}
        }
        
		/* 
			BNF:
            <factor> ::= 
                "("<expression>")" |
                "("<query expression>")" |
                <literal> |
                <identifier> |
                <identifier>"("<actual parameter commalist>")" |
                <identifier>"("[distinct] <expression>")" |
                <case expression>
        */
        protected Expression Factor(Lexer ALexer)
        {
            if (ALexer[1].Type != TokenType.Symbol)
				switch (ALexer.NextToken().Type)
				{
					case TokenType.Boolean: return new ValueExpression(ALexer[0].AsBoolean, TokenType.Boolean); 
					case TokenType.Integer: return new ValueExpression(ALexer[0].AsInteger, TokenType.Integer); 
					case TokenType.Decimal: return new ValueExpression(ALexer[0].AsDecimal, TokenType.Decimal); 
					case TokenType.Money: return new ValueExpression(ALexer[0].AsDecimal, TokenType.Money);
					case TokenType.String: return new ValueExpression(ALexer[0].AsString, TokenType.String); 
					default: throw new ParserException(ParserException.Codes.UnknownTokenType, Enum.GetName(typeof(TokenType), ALexer[0].Type));
				}
            else
            {
                switch (ALexer[1].Token)
                {
                    case Keywords.BeginGroup:
                        ALexer.NextToken();
                        Expression LExpression;
                        if (ALexer[1].Token == Keywords.Select)
                        {
							ALexer.NextToken();
							LExpression = QueryExpression(ALexer);
						}
						else
							LExpression = Expression(ALexer);
                        ALexer.NextToken().CheckSymbol(Keywords.EndGroup);
                        return LExpression;
                        
                    case Keywords.Case: return CaseExpression(ALexer);

                    default:
                    {
                        string LIdentifier = Identifier(ALexer);
                        switch (ALexer[1].Token)
                        {
                            case Keywords.BeginGroup: 
								if (IsAggregateOperator(LIdentifier))
									return AggregateCallExpression(ALexer, LIdentifier);
								else
									return CallExpression(ALexer, LIdentifier);
						    default: return new IdentifierExpression(LIdentifier);
                        }
                    }
                }
            }
        }
        
        protected Expression AggregateCallExpression(Lexer ALexer, string AIdentifier)
        {
			AggregateCallExpression LCallExpression = new AggregateCallExpression();
			LCallExpression.Identifier = AIdentifier;
			ALexer.NextToken().CheckSymbol(Keywords.BeginGroup);
			if (ALexer[1].Token == Keywords.Distinct)
			{
				ALexer.NextToken();
				LCallExpression.IsDistinct = true;
			}
			
			if (ALexer[1].Token == Keywords.Star)
			{
				ALexer.NextToken();
				LCallExpression.IsRowLevel = true;
			}
			else
				LCallExpression.Expressions.Add(Expression(ALexer));
			ALexer.NextToken().CheckSymbol(Keywords.EndGroup);
			return LCallExpression;
        }

        protected Expression CallExpression(Lexer ALexer, string AIdentifier)
        {
			CallExpression LCallExpression = new CallExpression();
			LCallExpression.Identifier = AIdentifier;
			ALexer.NextToken().CheckSymbol(Keywords.BeginGroup);
			if (ALexer[1].Token != Keywords.EndGroup)
			{
				bool LDone = false;
				do
				{
					LCallExpression.Expressions.Add(ActualParameter(ALexer));
					if (ALexer.NextToken().Type == TokenType.Symbol)
					{
						switch (ALexer[0].AsSymbol)
						{
							case Keywords.ListSeparator: break;
							case Keywords.EndGroup: LDone = true; break;
							default: throw new ParserException(ParserException.Codes.GroupTerminatorExpected);
						}
					}
					else
						throw new ParserException(ParserException.Codes.GroupTerminatorExpected);
				}
				while (!LDone);
			}
			else
				ALexer.NextToken();
			return LCallExpression;
		}
		
		/*
			BNF:
			<actual parameter> ::=
				[var] <expression>
		*/		
		protected Expression ActualParameter(Lexer ALexer)
		{
			Expression LExpression;
			switch (ALexer[1].Token)
			{
				case Keywords.Var:
					ALexer.NextToken();
					LExpression = new ParameterExpression(Modifier.Var, Expression(ALexer));
				break;
					
				default:
					LExpression = Expression(ALexer);
				break;
			}

			return LExpression;
		}
		
		/* 
			BNF:
            <case expression> ::=
                case [<expression>]
                    <ne case item expression commalist>
                    else <expression>
                end
                
            <case item expression> ::=
                when <expression> then <expression>
        */
        protected CaseExpression CaseExpression(Lexer ALexer)
        {
            ALexer.NextToken().CheckSymbol(Keywords.Case);
            CaseExpression LCaseExpression = new CaseExpression();
			if (!(ALexer[1].Token == Keywords.When))
	            LCaseExpression.Expression = Expression(ALexer);
            bool LDone = false;
            do
            {
                LCaseExpression.CaseItems.Add(CaseItemExpression(ALexer));
                switch (ALexer[1].AsSymbol)
                {
                    case Keywords.When: break;
                    case Keywords.Else: 
                        LCaseExpression.ElseExpression = CaseElseExpression(ALexer);
                        LDone = true;
                        break;
                    default: throw new ParserException(ParserException.Codes.CaseItemExpressionExpected);
                }
            }
            while (!LDone);
            ALexer.NextToken().CheckSymbol(Keywords.End);
            return LCaseExpression;
        }
        
        protected CaseItemExpression CaseItemExpression(Lexer ALexer)
        {
            ALexer.NextToken().CheckSymbol(Keywords.When);
            CaseItemExpression LExpression = new CaseItemExpression();
            LExpression.WhenExpression = Expression(ALexer);
            ALexer.NextToken().CheckSymbol(Keywords.Then);
            LExpression.ThenExpression = Expression(ALexer);
            return LExpression;
        }
        
        protected CaseElseExpression CaseElseExpression(Lexer ALexer)
        {
            ALexer.NextToken().CheckSymbol(Keywords.Else);
            return new CaseElseExpression(Expression(ALexer));
        }
        
		/* 
			BNF:
            <qualified identifier> ::=
                [.]{<identifier>.}<identifier>
        */        
        protected string QualifiedIdentifier(Lexer ALexer)
        {
			StringBuilder LIdentifier = new StringBuilder();
			if (ALexer[1].Token == Keywords.Qualifier)
				LIdentifier.Append(ALexer.NextToken().Token);

			LIdentifier.Append(Identifier(ALexer));
            while (ALexer[1].Token == Keywords.Qualifier)
				LIdentifier.AppendFormat("{0}{1}", ALexer.NextToken().Token, Identifier(ALexer));

            return LIdentifier.ToString();
        }
        
        protected bool IsValidIdentifier(string AIdentifier)
        {
			for (int LIndex = 0; LIndex < AIdentifier.Length; LIndex++)
				if 
					(
						(
							(LIndex == 0) && 
							!(Char.IsLetter(AIdentifier[LIndex]) || (AIdentifier[LIndex] == '_'))
						) || 
						(
							(LIndex != 0) && 
							!(Char.IsLetterOrDigit(AIdentifier[LIndex]) || (AIdentifier[LIndex] == '_'))
						)
					)
					return false;
			return true;
        }
        
        protected bool IsReservedWord(string AIdentifier)
        {
			//return ReservedWords.Contains(AIdentifier);
			return false;
        }
        
 		protected string Identifier(Lexer ALexer)
		{
			ALexer.NextToken().CheckType(TokenType.Symbol);
			if (!IsValidIdentifier(ALexer[0].Token))
				throw new ParserException(ParserException.Codes.InvalidIdentifier, ALexer[0].Token);
			// TODO: Reserved words in SQL
			if (IsReservedWord(ALexer[0].Token))
				throw new ParserException(ParserException.Codes.ReservedWordIdentifier, ALexer[0].Token);

			return ALexer[0].Token;
		}
	}
}

