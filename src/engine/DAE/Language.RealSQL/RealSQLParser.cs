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
		public Statement ParseScript(string input)
		{
			Lexer lexer = new Lexer(input);
			try
			{
				Batch batch = new Batch();
				do
				{
					if (lexer.PeekToken(1).Type == TokenType.EOF)
						break;
					batch.Statements.Add(Statement(lexer));
					if (lexer.PeekToken(1).Type == TokenType.EOF)
						break;
					else
						lexer.NextToken().CheckSymbol(Keywords.StatementTerminator);
				} while (true);
				return batch;
			}
			catch (Exception E)
			{
				throw new SyntaxException(lexer, E);
			}
		}
		
        public Statement ParseStatement(string input)
        {
			Lexer lexer = new Lexer(input);
			try
			{
				return Statement(lexer);
			}
			catch (Exception E)
			{
				throw new SyntaxException(lexer, E);
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
		protected Statement Statement(Lexer lexer)
		{
			switch (lexer.PeekTokenSymbol(1))
			{
				case Keywords.Select: return SelectStatement(lexer);
				case Keywords.Insert: return InsertStatement(lexer);
				case Keywords.Update: return UpdateStatement(lexer);
				case Keywords.Delete: return DeleteStatement(lexer);
				default: return Expression(lexer);
			}
		}
		
		/*
			BNF:
			<select statement> ::=
				<query expression>
				[<order by clause>]
		*/
		protected Statement SelectStatement(Lexer lexer)
		{
			SelectStatement statement = new SelectStatement();
			statement.QueryExpression = QueryExpression(lexer);
			if (lexer.PeekTokenSymbol(1) == Keywords.Order)
				statement.OrderClause = OrderClause(lexer);
			return statement;
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
		protected OrderClause OrderClause(Lexer lexer)
		{
			lexer.NextToken();
			lexer.NextToken().CheckSymbol(Keywords.By);
			OrderClause clause = new OrderClause();
			while (true)
			{
				clause.Columns.Add(OrderFieldExpression(lexer));
				if (lexer.PeekTokenSymbol(1) == Keywords.ListSeparator)
					lexer.NextToken();
				else
					break;
			}
			return clause;
		}
		
		protected OrderFieldExpression OrderFieldExpression(Lexer lexer)
		{
			OrderFieldExpression localExpression = new OrderFieldExpression();
			QualifiedFieldExpression(lexer, localExpression);
			switch (lexer.PeekTokenSymbol(1))
			{
				case Keywords.Asc: 
					lexer.NextToken(); 
					localExpression.Ascending = true; 
				break;

				case Keywords.Desc: lexer.NextToken(); break;
				default: localExpression.Ascending = true; break;
			}
			return localExpression;
		}
		
		protected void QualifiedFieldExpression(Lexer lexer, QualifiedFieldExpression expression)
		{
			string identifier = Identifier(lexer);
			if (lexer.PeekTokenSymbol(1) == Keywords.Qualifier)
			{
				expression.TableAlias = identifier;
				lexer.NextToken();
				expression.FieldName = Identifier(lexer);
			}
			else
				expression.FieldName = identifier;
		}

		/*
			BNF:
			<query expression> ::=
				<select expression> [<binary table expression>]

			<binary table expression> ::=
				[union | intersect | minus] <select expression>
		*/
		protected QueryExpression QueryExpression(Lexer lexer)
		{
			QueryExpression localExpression = new QueryExpression();
			localExpression.SelectExpression = SelectExpression(lexer);
			bool done = false;
			while (!done)
			{
				switch (lexer.PeekTokenSymbol(1))
				{
					case Keywords.Union:
						lexer.NextToken();
						localExpression.TableOperators.Add(new TableOperatorExpression(TableOperator.Union, SelectExpression(lexer)));
					break;

					case Keywords.Intersect:
						lexer.NextToken();
						localExpression.TableOperators.Add(new TableOperatorExpression(TableOperator.Intersect, SelectExpression(lexer)));
					break;

					case Keywords.Minus:
						lexer.NextToken();
						localExpression.TableOperators.Add(new TableOperatorExpression(TableOperator.Difference, SelectExpression(lexer)));
					break;

					default: 
						done = true; 
					break;
				}
			}

			return localExpression;
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
		protected SelectExpression SelectExpression(Lexer lexer)
		{
			lexer.NextToken().CheckSymbol(Keywords.Select);
			SelectExpression localExpression = new SelectExpression();
			localExpression.SelectClause = new SelectClause();
			if (lexer.PeekTokenSymbol(1) == Keywords.Star)
			{
				lexer.NextToken();
				localExpression.SelectClause.NonProject = true;
			}
			else
			{
				do
				{
					localExpression.SelectClause.Columns.Add(ColumnExpression(lexer));
					if (lexer.PeekTokenSymbol(1) == Keywords.ListSeparator)
						lexer.NextToken();
					else
						break;
				} while (true);
			}

			lexer.NextToken().CheckSymbol(Keywords.From);
			localExpression.FromClause = FromClause(lexer);
			
			if (lexer.PeekTokenSymbol(1) == Keywords.Where)
				localExpression.WhereClause = WhereClause(lexer);
			
			if (lexer.PeekTokenSymbol(1) == Keywords.Group)
				localExpression.GroupClause = GroupClause(lexer);
				
			if (lexer.PeekTokenSymbol(1) == Keywords.Having)
				localExpression.HavingClause = HavingClause(lexer);
				
			return localExpression;
		}

		/*
			BNF:
			<column expression> ::=
				<expression> [as <identifier>]
		*/		
		protected ColumnExpression ColumnExpression(Lexer lexer)
		{
			ColumnExpression localExpression = new ColumnExpression();
			localExpression.Expression = Expression(lexer);
			if (lexer.PeekTokenSymbol(1) == Keywords.As)
			{
				lexer.NextToken();
				localExpression.ColumnAlias = Identifier(lexer);
			}
			return localExpression;
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
		protected AlgebraicFromClause FromClause(Lexer lexer)
		{
			AlgebraicFromClause fromClause = new AlgebraicFromClause();
			if (lexer.PeekTokenSymbol(1) == Keywords.BeginGroup)
			{
				lexer.NextToken();
				fromClause.TableSpecifier = new TableSpecifier(QueryExpression(lexer));
				lexer.NextToken().CheckSymbol(Keywords.EndGroup);
			}
			else
				fromClause.TableSpecifier = new TableSpecifier(new TableExpression(QualifiedIdentifier(lexer)));

			if (lexer.PeekTokenSymbol(1) == Keywords.As)
			{
				lexer.NextToken();
				fromClause.TableSpecifier.TableAlias = Identifier(lexer);
			}

			bool done = false;
			JoinClause joinClause;
			while (!done)
			{
				switch (lexer.PeekTokenSymbol(1))
				{
					case Keywords.Cross:
						lexer.NextToken();
						lexer.NextToken().CheckSymbol(Keywords.Join);
						joinClause = new JoinClause();
						joinClause.FromClause = FromClause(lexer);
						joinClause.JoinType = JoinType.Cross;
						fromClause.Joins.Add(joinClause);
					break;
					
					case Keywords.Inner:
					case Keywords.Join:
						if (lexer.PeekTokenSymbol(1) == Keywords.Inner)
							lexer.NextToken();
						lexer.NextToken().CheckSymbol(Keywords.Join);
						joinClause = new JoinClause();
						joinClause.FromClause = FromClause(lexer);
						joinClause.JoinType = JoinType.Inner;
						lexer.NextToken().CheckSymbol(Keywords.On);
						joinClause.JoinExpression = Expression(lexer);
						fromClause.Joins.Add(joinClause);
					break;
					
					case Keywords.Left:
						lexer.NextToken();
						if (lexer.PeekTokenSymbol(1) == Keywords.Outer)
							lexer.NextToken();
						lexer.NextToken().CheckSymbol(Keywords.Join);
						joinClause = new JoinClause();
						joinClause.FromClause = FromClause(lexer);
						joinClause.JoinType = JoinType.Left;
						lexer.NextToken().CheckSymbol(Keywords.On);
						joinClause.JoinExpression = Expression(lexer);
						fromClause.Joins.Add(joinClause);
					break;
						
					case Keywords.Right:
						lexer.NextToken();
						if (lexer.PeekTokenSymbol(1) == Keywords.Outer)
							lexer.NextToken();
						lexer.NextToken().CheckSymbol(Keywords.Join);
						joinClause = new JoinClause();
						joinClause.FromClause = FromClause(lexer);
						joinClause.JoinType = JoinType.Right;
						lexer.NextToken().CheckSymbol(Keywords.On);
						joinClause.JoinExpression = Expression(lexer);
						fromClause.Joins.Add(joinClause);
					break;
					
					default: done = true; break;
				}
			}

			return fromClause;
		}
		
		/*
			BNF:
			<where clause> ::=
				where <expression>
		*/
		protected WhereClause WhereClause(Lexer lexer)
		{
			lexer.NextToken();
			return new WhereClause(Expression(lexer));
		}
		
		/*
			BNF:
			<group by clause> ::=
				group by <expression commalist>
		*/
		protected GroupClause GroupClause(Lexer lexer)
		{
			lexer.NextToken();
			lexer.NextToken().CheckSymbol(Keywords.By);
			GroupClause groupClause = new GroupClause();
			do
			{
				groupClause.Columns.Add(Expression(lexer));
				if (lexer.PeekTokenSymbol(1) == Keywords.ListSeparator)
					lexer.NextToken();
				else
					break;
			} while (true);
			return groupClause;
		}
		
		/*
			BNF:
			<having clause> ::=
				having <expression>
		*/
		protected HavingClause HavingClause(Lexer lexer)
		{
			lexer.NextToken();
			return new HavingClause(Expression(lexer));
		}
		
		/*
			BNF:
			<insert statement> ::=
				insert into <table identifier>"("<column identifier commalist>")"
					(<values clause> | <query expression>)
					
			<values clause> ::=
				values"("<expression commalist>")"
		*/
		protected Statement InsertStatement(Lexer lexer)
		{
			lexer.NextToken();
			lexer.NextToken().CheckSymbol(Keywords.Into);
			InsertStatement statement = new InsertStatement();
			statement.InsertClause = new InsertClause();
			statement.InsertClause.TableExpression = new TableExpression(QualifiedIdentifier(lexer));
			lexer.NextToken().CheckSymbol(Keywords.BeginGroup);
			do
			{
				statement.InsertClause.Columns.Add(new InsertFieldExpression(Identifier(lexer)));
				if (lexer.PeekTokenSymbol(1) == Keywords.ListSeparator)
					lexer.NextToken();
				else
					break;
			} while (true);
			lexer.NextToken().CheckSymbol(Keywords.EndGroup);
			if (lexer.PeekTokenSymbol(1) == Keywords.Values)
			{
				ValuesExpression values = new ValuesExpression();
				lexer.NextToken();							   
				lexer.NextToken().CheckSymbol(Keywords.BeginGroup);
				do
				{
					values.Expressions.Add(Expression(lexer));
					if (lexer.PeekTokenSymbol(1) == Keywords.ListSeparator)
						lexer.NextToken();
					else
						break;
				} while (true);
				lexer.NextToken().CheckSymbol(Keywords.EndGroup);
				statement.Values = values;
			}
			else
			{
				lexer.NextToken();
				statement.Values = QueryExpression(lexer);
			}

			return statement;
		}
		
		/*
			BNF:
			<update statement> ::=
				update <table identifier> 
						set <update column expression commalist>
					[<where clause>]
		*/
		protected Statement UpdateStatement(Lexer lexer)
		{
			UpdateStatement statement = new UpdateStatement();
			lexer.NextToken();
			statement.UpdateClause = new UpdateClause();
			statement.UpdateClause.TableExpression = new TableExpression(QualifiedIdentifier(lexer));
			lexer.NextToken().CheckSymbol(Keywords.Set);
			do
			{
				statement.UpdateClause.Columns.Add(UpdateColumnExpression(lexer));
				if (lexer.PeekTokenSymbol(1) == Keywords.ListSeparator)
					lexer.NextToken();
				else
					break;
			} while (true);
			if (lexer.PeekTokenSymbol(1) == Keywords.Where)
				statement.WhereClause = WhereClause(lexer);
			return statement;
		}
		
		/*
			BNF:
			<update column expression> ::=
				<identifier> = <expression>
		*/
		protected UpdateFieldExpression UpdateColumnExpression(Lexer lexer)
		{
			UpdateFieldExpression localExpression = new UpdateFieldExpression();
			localExpression.FieldName = Identifier(lexer);
			lexer.NextToken().CheckSymbol(Keywords.Equal);
			localExpression.Expression = Expression(lexer);
			return localExpression;
		}
		
		/*
			BNF:
			<delete statement> ::=
				delete <table identifier>
					[<where clause>]
		*/
		protected Statement DeleteStatement(Lexer lexer)
		{
			DeleteStatement statement = new DeleteStatement();
			lexer.NextToken();
			statement.DeleteClause = new DeleteClause();
			statement.DeleteClause.TableExpression = new TableExpression(QualifiedIdentifier(lexer));
			if (lexer.PeekTokenSymbol(1) == Keywords.Where)
				statement.WhereClause = WhereClause(lexer);
			return statement;
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
		protected Expression Expression(Lexer lexer)
		{
            Expression localExpression = LogicalAndExpression(lexer);
			while (IsLogicalOperator(lexer.PeekTokenSymbol(1)))
			{
				if (lexer.PeekTokenSymbol(1) == Keywords.Between)
				{
					lexer.NextToken();
					BetweenExpression LBetweenExpression = new BetweenExpression();
					LBetweenExpression.Expression = localExpression;
					LBetweenExpression.LowerExpression = AdditiveExpression(lexer);
					lexer.NextToken().CheckSymbol(Keywords.And);
					LBetweenExpression.UpperExpression = AdditiveExpression(lexer);
					localExpression = LBetweenExpression;
				}
				else
				{
					BinaryExpression binaryExpression = new BinaryExpression();
					binaryExpression.LeftExpression = localExpression;
					switch (lexer.NextToken().Token)
					{
						case Keywords.In: binaryExpression.Instruction = Instructions.In; break;
						case Keywords.Or: binaryExpression.Instruction = Instructions.Or; break;
						case Keywords.Xor: binaryExpression.Instruction = Instructions.Xor; break;
						case Keywords.Like: binaryExpression.Instruction = Instructions.Like; break;
						case Keywords.Matches: binaryExpression.Instruction = Instructions.Matches; break;
					}
					binaryExpression.RightExpression = LogicalAndExpression(lexer);
					localExpression = binaryExpression;
				}
			}
			return localExpression;
		}
		
		/* 
			BNF:
            <logical and expression> ::= 
                <bitwise binary expression> {<logical and operator> <bitwise binary expression>}
                
            <logical and operator> ::=
                and
        */
        protected Expression LogicalAndExpression(Lexer lexer)
        {
            Expression localExpression = BitwiseBinaryExpression(lexer);
            while (lexer.PeekTokenSymbol(1) == Keywords.And)
            {
                BinaryExpression binaryExpression = new BinaryExpression();
                binaryExpression.LeftExpression = localExpression;
                lexer.NextToken();
                binaryExpression.Instruction = Instructions.And;
                binaryExpression.RightExpression = BitwiseBinaryExpression(lexer);
                localExpression = binaryExpression;
            }
            return localExpression;
        }
        
        protected bool IsBitwiseBinaryOperator(string aOperator)
        {
            switch (aOperator)
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
        protected Expression BitwiseBinaryExpression(Lexer lexer)
        {
            Expression localExpression = ComparisonExpression(lexer);
            while (IsBitwiseBinaryOperator(lexer.PeekTokenSymbol(1)))
            {
                BinaryExpression binaryExpression = new BinaryExpression();
                binaryExpression.LeftExpression = localExpression;
                switch (lexer.NextToken().Token)
                {
					case Keywords.BitwiseXor: binaryExpression.Instruction = Instructions.BitwiseXor; break;
					case Keywords.BitwiseAnd: binaryExpression.Instruction = Instructions.BitwiseAnd; break;
					case Keywords.BitwiseOr: binaryExpression.Instruction = Instructions.BitwiseOr; break;
					case Keywords.ShiftLeft: binaryExpression.Instruction = Instructions.ShiftLeft; break;
					case Keywords.ShiftRight: binaryExpression.Instruction = Instructions.ShiftRight; break;
                }
                binaryExpression.RightExpression = ComparisonExpression(lexer);
                localExpression = binaryExpression;
            }
            return localExpression;
        }
        
        protected bool IsComparisonOperator(string aOperator)
        {
            switch (aOperator)
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
        protected Expression ComparisonExpression(Lexer lexer)
        {
            Expression localExpression = AdditiveExpression(lexer);
            while (IsComparisonOperator(lexer.PeekTokenSymbol(1)))
            {
                BinaryExpression binaryExpression = new BinaryExpression();
                binaryExpression.LeftExpression = localExpression;
                switch (lexer.NextToken().Token)
                {
					case Keywords.Equal: binaryExpression.Instruction = Instructions.Equal; break;
					case Keywords.NotEqual: binaryExpression.Instruction = Instructions.NotEqual; break;
					case Keywords.Less: binaryExpression.Instruction = Instructions.Less; break;
					case Keywords.Greater: binaryExpression.Instruction = Instructions.Greater; break;
					case Keywords.InclusiveLess: binaryExpression.Instruction = Instructions.InclusiveLess; break;
					case Keywords.InclusiveGreater: binaryExpression.Instruction = Instructions.InclusiveGreater; break;
					case Keywords.Compare: binaryExpression.Instruction = Instructions.Compare; break;
                }
                binaryExpression.RightExpression = AdditiveExpression(lexer);
                localExpression = binaryExpression;
            }
            return localExpression;
        }
       
        protected bool IsAdditiveOperator(string aOperator)
        {
            switch (aOperator)
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
        protected Expression AdditiveExpression(Lexer lexer)
        {
            Expression localExpression = MultiplicativeExpression(lexer);
            while (IsAdditiveOperator(lexer.PeekTokenSymbol(1)))
            {
                BinaryExpression binaryExpression = new BinaryExpression();
                binaryExpression.LeftExpression = localExpression;
                switch (lexer.NextToken().Token)
                {
					case Keywords.Addition: binaryExpression.Instruction = Instructions.Addition; break;
					case Keywords.Subtraction: binaryExpression.Instruction = Instructions.Subtraction; break;
                }
                binaryExpression.RightExpression = MultiplicativeExpression(lexer);
                localExpression = binaryExpression;
            }
            return localExpression;
        }
        
        protected bool IsMultiplicativeOperator(string aOperator)
        {
            switch (aOperator)
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
        protected Expression MultiplicativeExpression(Lexer lexer)
        {
            Expression localExpression = ExponentExpression(lexer);
            while (IsMultiplicativeOperator(lexer.PeekTokenSymbol(1)))
            {
                BinaryExpression binaryExpression = new BinaryExpression();
                binaryExpression.LeftExpression = localExpression;
                switch (lexer.NextToken().Token)
                {
					case Keywords.Multiplication: binaryExpression.Instruction = Instructions.Multiplication; break;
					case Keywords.Division: binaryExpression.Instruction = Instructions.Division; break;
					case Keywords.Div: binaryExpression.Instruction = Instructions.Div; break;
					case Keywords.Mod: binaryExpression.Instruction = Instructions.Mod; break;
                }
                binaryExpression.RightExpression = ExponentExpression(lexer);
                localExpression = binaryExpression;
            }
            return localExpression;
        }

		/* 
			BNF:
            <exponent expression> ::= 
                <unary expression> {<exponent operator> <unary expression>}
                
            <exponent operator> ::=
                **
        */
        protected Expression ExponentExpression(Lexer lexer)
        {
            Expression localExpression = UnaryExpression(lexer);
            while (lexer.PeekTokenSymbol(1) == Keywords.Power)
            {
                BinaryExpression binaryExpression = new BinaryExpression();
                binaryExpression.LeftExpression = localExpression;
                lexer.NextToken();
                binaryExpression.Instruction = Instructions.Power;
                binaryExpression.RightExpression = UnaryExpression(lexer);
                localExpression = binaryExpression;
            }
            return localExpression;
        }
        
        protected bool IsUnaryOperator(string aOperator)
        {
            switch (aOperator)
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
        protected Expression UnaryExpression(Lexer lexer)
        {
			if (IsUnaryOperator(lexer.PeekTokenSymbol(1)))
			{
				switch (lexer.NextToken().Token)
				{
					case Keywords.Addition: return UnaryExpression(lexer);
					case Keywords.Subtraction: return new UnaryExpression(Instructions.Negate, UnaryExpression(lexer));
					case Keywords.BitwiseNot: return new UnaryExpression(Instructions.BitwiseNot, UnaryExpression(lexer));
					case Keywords.Not: return new UnaryExpression(Instructions.Not, UnaryExpression(lexer));
					case Keywords.Exists: return new UnaryExpression(Instructions.Exists, UnaryExpression(lexer));
				}
			}
			return QualifiedFactor(lexer);
        }
        
		/* 
			BNF:
			<qualified factor> ::=
				(([.]<identifier>) | <qualifier expression>){"["<expression>"]"[.<qualifier expression>]}
        */
        protected Expression QualifiedFactor(Lexer lexer)
        {
			Expression localExpression;
			if (lexer.PeekTokenSymbol(1) == Keywords.Qualifier)
			{
				lexer.NextToken();
				localExpression = new IdentifierExpression(String.Format("{0}{1}", Keywords.Qualifier, Identifier(lexer)));
			}
			else
				localExpression = QualifierExpression(lexer);
				
			while (lexer.PeekTokenSymbol(1) == Keywords.BeginIndexer)
			{
				IndexerExpression indexerExpression = new IndexerExpression();
				indexerExpression.SetPosition(lexer);
				indexerExpression.Expression = localExpression;
				lexer.NextToken();
				indexerExpression.Indexer = Expression(lexer);
				lexer.NextToken().CheckSymbol(Keywords.EndIndexer);
				localExpression = indexerExpression;
				
				if (lexer.PeekTokenSymbol(1) == Keywords.Qualifier)
				{
					lexer.NextToken();
					QualifierExpression qualifierExpression = new QualifierExpression();
					qualifierExpression.SetPosition(lexer);
					qualifierExpression.LeftExpression = localExpression;
					qualifierExpression.RightExpression = QualifierExpression(lexer);
					localExpression = qualifierExpression;
				}
			}

			return localExpression;
        }
        
        /*
			BNF:
			<qualifier expression> ::=
				<factor>[.<qualifier expression>]
		*/
        protected Expression QualifierExpression(Lexer lexer)
        {
			Expression localExpression = Factor(lexer);
			if (lexer.PeekTokenSymbol(1) == Keywords.Qualifier)
			{
				lexer.NextToken();
				QualifierExpression qualifierExpression = new QualifierExpression();
				qualifierExpression.SetPosition(lexer);
				qualifierExpression.LeftExpression = localExpression;
				qualifierExpression.RightExpression = QualifierExpression(lexer);
				return qualifierExpression;
			}
			return localExpression;
        }
        
        protected bool IsAggregateOperator(string identifier)
        {
			switch (identifier)
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
        protected Expression Factor(Lexer lexer)
        {
            if (lexer[1].Type != TokenType.Symbol)
				switch (lexer.NextToken().Type)
				{
					case TokenType.Boolean: return new ValueExpression(lexer[0].AsBoolean, TokenType.Boolean); 
					case TokenType.Integer: return new ValueExpression(lexer[0].AsInteger, TokenType.Integer); 
					case TokenType.Decimal: return new ValueExpression(lexer[0].AsDecimal, TokenType.Decimal); 
					case TokenType.Money: return new ValueExpression(lexer[0].AsDecimal, TokenType.Money);
					case TokenType.String: return new ValueExpression(lexer[0].AsString, TokenType.String); 
					default: throw new ParserException(ParserException.Codes.UnknownTokenType, Enum.GetName(typeof(TokenType), lexer[0].Type));
				}
            else
            {
                switch (lexer.PeekTokenSymbol(1))
                {
                    case Keywords.BeginGroup:
                        lexer.NextToken();
                        Expression localExpression;
                        if (lexer.PeekTokenSymbol(1) == Keywords.Select)
                        {
							localExpression = QueryExpression(lexer);
						}
						else
							localExpression = Expression(lexer);
                        lexer.NextToken().CheckSymbol(Keywords.EndGroup);
                        return localExpression;
                        
                    case Keywords.Case: return CaseExpression(lexer);

                    default:
                    {
                        string identifier = Identifier(lexer);
                        switch (lexer.PeekTokenSymbol(1))
                        {
                            case Keywords.BeginGroup: 
								if (IsAggregateOperator(identifier))
									return AggregateCallExpression(lexer, identifier);
								else
									return CallExpression(lexer, identifier);
						    default: return new IdentifierExpression(identifier);
                        }
                    }
                }
            }
        }
        
        protected Expression AggregateCallExpression(Lexer lexer, string identifier)
        {
			AggregateCallExpression callExpression = new AggregateCallExpression();
			callExpression.Identifier = identifier;
			lexer.NextToken().CheckSymbol(Keywords.BeginGroup);
			if (lexer.PeekTokenSymbol(1) == Keywords.Distinct)
			{
				lexer.NextToken();
				callExpression.IsDistinct = true;
			}
			
			if (lexer.PeekTokenSymbol(1) == Keywords.Star)
			{
				lexer.NextToken();
				callExpression.IsRowLevel = true;
			}
			else
				callExpression.Expressions.Add(Expression(lexer));
			lexer.NextToken().CheckSymbol(Keywords.EndGroup);
			return callExpression;
        }

        protected Expression CallExpression(Lexer lexer, string identifier)
        {
			CallExpression callExpression = new CallExpression();
			callExpression.Identifier = identifier;
			lexer.NextToken().CheckSymbol(Keywords.BeginGroup);
			if (lexer.PeekTokenSymbol(1) != Keywords.EndGroup)
			{
				bool done = false;
				do
				{
					callExpression.Expressions.Add(ActualParameter(lexer));
					if (lexer.NextToken().Type == TokenType.Symbol)
					{
						switch (lexer[0].AsSymbol)
						{
							case Keywords.ListSeparator: break;
							case Keywords.EndGroup: done = true; break;
							default: throw new ParserException(ParserException.Codes.GroupTerminatorExpected);
						}
					}
					else
						throw new ParserException(ParserException.Codes.GroupTerminatorExpected);
				}
				while (!done);
			}
			else
				lexer.NextToken();
			return callExpression;
		}
		
		/*
			BNF:
			<actual parameter> ::=
				[var] <expression>
		*/		
		protected Expression ActualParameter(Lexer lexer)
		{
			Expression localExpression;
			switch (lexer.PeekTokenSymbol(1))
			{
				case Keywords.Var:
					lexer.NextToken();
					localExpression = new ParameterExpression(Modifier.Var, Expression(lexer));
				break;
					
				default:
					localExpression = Expression(lexer);
				break;
			}

			return localExpression;
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
        protected CaseExpression CaseExpression(Lexer lexer)
        {
            lexer.NextToken().CheckSymbol(Keywords.Case);
            CaseExpression caseExpression = new CaseExpression();
			if (!(lexer.PeekTokenSymbol(1) == Keywords.When))
	            caseExpression.Expression = Expression(lexer);
            bool done = false;
            do
            {
                caseExpression.CaseItems.Add(CaseItemExpression(lexer));
                switch (lexer[1].AsSymbol)
                {
                    case Keywords.When: break;
                    case Keywords.Else: 
                        caseExpression.ElseExpression = CaseElseExpression(lexer);
                        done = true;
                        break;
                    default: throw new ParserException(ParserException.Codes.CaseItemExpressionExpected);
                }
            }
            while (!done);
            lexer.NextToken().CheckSymbol(Keywords.End);
            return caseExpression;
        }
        
        protected CaseItemExpression CaseItemExpression(Lexer lexer)
        {
            lexer.NextToken().CheckSymbol(Keywords.When);
            CaseItemExpression localExpression = new CaseItemExpression();
            localExpression.WhenExpression = Expression(lexer);
            lexer.NextToken().CheckSymbol(Keywords.Then);
            localExpression.ThenExpression = Expression(lexer);
            return localExpression;
        }
        
        protected CaseElseExpression CaseElseExpression(Lexer lexer)
        {
            lexer.NextToken().CheckSymbol(Keywords.Else);
            return new CaseElseExpression(Expression(lexer));
        }
        
		/* 
			BNF:
            <qualified identifier> ::=
                [.]{<identifier>.}<identifier>
        */        
        protected string QualifiedIdentifier(Lexer lexer)
        {
			StringBuilder identifier = new StringBuilder();
			if (lexer.PeekTokenSymbol(1) == Keywords.Qualifier)
				identifier.Append(lexer.NextToken().Token);

			identifier.Append(Identifier(lexer));
            while (lexer.PeekTokenSymbol(1) == Keywords.Qualifier)
				identifier.AppendFormat("{0}{1}", lexer.NextToken().Token, Identifier(lexer));

            return identifier.ToString();
        }
        
        protected bool IsValidIdentifier(string identifier)
        {
			for (int LIndex = 0; LIndex < identifier.Length; LIndex++)
				if 
					(
						(
							(LIndex == 0) && 
							!(Char.IsLetter(identifier[LIndex]) || (identifier[LIndex] == '_'))
						) || 
						(
							(LIndex != 0) && 
							!(Char.IsLetterOrDigit(identifier[LIndex]) || (identifier[LIndex] == '_'))
						)
					)
					return false;
			return true;
        }
        
        protected bool IsReservedWord(string identifier)
        {
			//return ReservedWords.Contains(identifier);
			return false;
        }
        
 		protected string Identifier(Lexer lexer)
		{
			lexer.NextToken().CheckType(TokenType.Symbol);
			if (!IsValidIdentifier(lexer[0].Token))
				throw new ParserException(ParserException.Codes.InvalidIdentifier, lexer[0].Token);
			// TODO: Reserved words in SQL
			if (IsReservedWord(lexer[0].Token))
				throw new ParserException(ParserException.Codes.ReservedWordIdentifier, lexer[0].Token);

			return lexer[0].Token;
		}
	}
}

