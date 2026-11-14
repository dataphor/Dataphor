/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.SQL;
using D4 = Alphora.Dataphor.DAE.Language.D4;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Language.RealSQL
{
	/*
		select <column list>								
			from <table specifier> [<join clause list>]
			where <condition>
			group by <column list>
			having <condition>
			
			::=
			
			non aggregate columns - columns referenced by expressions in <column list> outside of aggregate invocations
			aggregate columns - columns referenced by expressions in <column list> inside of aggregate invocations
			group by columns - columns referenced by expressions in <group by column list> (group by columns cannot be aggregate or subquery expressions) (must be a super set of nonaggregate columns)
			having columns - columns referenced by expression in <having condition> (must be in the aggregate or nonaggregate columns list)
			
			(<table> as <table alias>) [<join>] 
				where <condition> 
				group by <column list> add <aggregate column list> 
				where <having condition> (with aggregate column references translated to their aliases)
				add <expression columns>			
				over <column list>
	*/
	
	// given a RealSQL syntax tree, return the equivalent D4 syntax tree
	public class Compiler
	{
		protected class PlanContext : System.Object
		{
			private NamedExpressions preAggregateExpressions = new NamedExpressions();
			public NamedExpressions PreAggregateExpressions { get { return preAggregateExpressions; } }
			
			private NamedExpressions aggregateExpressions = new NamedExpressions();
			public NamedExpressions AggregateExpressions { get { return aggregateExpressions; } }
			
			private NamedExpressions extendExpressions = new NamedExpressions();
			public NamedExpressions ExtendExpressions { get { return extendExpressions; } }

			private NamedExpressions renameExpressions = new NamedExpressions();
			public NamedExpressions RenameExpressions { get { return renameExpressions; } }

			protected int expressionCount;
			protected string GetNextUniqueName()
			{
				expressionCount++;
				return String.Format("Expression{0}", expressionCount.ToString());
			}

			public string GetUniqueName()
			{
				string name;
				do
				{
					name = GetNextUniqueName();
				} while (IndexOf(name) >= 0);
				return name;
			}
			
			public string GetExpressionName(Expression expression)
			{
				int index = preAggregateExpressions.IndexOf(expression);
				if (index >= 0)
					return preAggregateExpressions[index].Name;
				index = aggregateExpressions.IndexOf(expression);
				if (index >= 0)
					return aggregateExpressions[index].Name;
				index = extendExpressions.IndexOf(expression);
				if (index >= 0)
					return extendExpressions[index].Name;
				index = renameExpressions.IndexOf(expression);
				if (index >= 0)
					return renameExpressions[index].Name;
				return GetUniqueName();
			}
			
			protected int IndexOf(string name)
			{
				int index = preAggregateExpressions.IndexOf(name);
				if (index >= 0)
					return index;
				index = aggregateExpressions.IndexOf(name);
				if (index >= 0)
					return index;
				index = extendExpressions.IndexOf(name);
				if (index >= 0)
					return index;
				index = renameExpressions.IndexOf(name);
				if (index >= 0)
					return index;
				return -1;
			}
			
			protected int IndexOf(Expression expression)
			{
				int index = preAggregateExpressions.IndexOf(expression);
				if (index >= 0)
					return index;
				index = aggregateExpressions.IndexOf(expression);
				if (index >= 0)
					return index;
				index = extendExpressions.IndexOf(expression);
				if (index >= 0)
					return index;
				index = renameExpressions.IndexOf(expression);
				if (index >= 0)
					return index;
				return -1;
			}
		}
		
		protected class Plan : System.Object
		{
			protected List contexts = new List();
			
			public void PushContext()
			{
				contexts.Add(new PlanContext());
			}
			
			public void PopContext()
			{
				contexts.RemoveAt(contexts.Count -1);
			}
			
			public PlanContext Context { get { return (PlanContext)contexts[contexts.Count - 1]; } }
		}
		
		public Statement Compile(Statement statement)
		{
			Plan plan = new Plan();
			return CompileStatement(plan, statement);
		}
		
		protected Statement CompileStatement(Plan plan, Statement statement)
		{
			if (statement is Batch)
				return CompileBatch(plan, (Batch)statement);
			else if (statement is SelectStatement)
				return CompileSelectStatement(plan, (SelectStatement)statement);
			else if (statement is InsertStatement)
				return CompileInsertStatement(plan, (InsertStatement)statement);
			else if (statement is UpdateStatement)
				return CompileUpdateStatement(plan, (UpdateStatement)statement);
			else if (statement is DeleteStatement)
				return CompileDeleteStatement(plan, (DeleteStatement)statement);
			else if (statement is Expression)
				return new D4.ExpressionStatement(CompileExpression(plan, (Expression)statement));
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected Statement CompileBatch(Plan plan, Batch statement)
		{
			Block block = new Block();
			foreach (Statement localStatement in statement.Statements)
				block.Statements.Add(CompileStatement(plan, localStatement));
			return block;
		}
		
		protected Statement CompileSelectStatement(Plan plan, SelectStatement statement)
		{
			D4.SelectStatement localStatement = new D4.SelectStatement();
			D4.CursorDefinition LDefinition = new D4.CursorDefinition();
			localStatement.CursorDefinition = LDefinition;
			LDefinition.Expression = CompileQueryExpression(plan, statement.QueryExpression);
			if (statement.OrderClause != null)
			{
				D4.OrderExpression LOrderExpression = new D4.OrderExpression();
				LOrderExpression.Expression = LDefinition.Expression;
				foreach (OrderFieldExpression LOrderField in statement.OrderClause.Columns)
					LOrderExpression.Columns.Add(new D4.OrderColumnDefinition(Schema.Object.Qualify(LOrderField.FieldName, LOrderField.TableAlias), LOrderField.Ascending));
				LDefinition.Expression = LOrderExpression;
			}
			return localStatement;
		}
		
		protected Expression CompileQueryExpression(Plan plan, QueryExpression expression)
		{
			Expression localExpression = CompileSelectExpression(plan, expression.SelectExpression);
			foreach (TableOperatorExpression LTableOperator in expression.TableOperators)
			{
				switch (LTableOperator.TableOperator)
				{
					case TableOperator.Union: localExpression = new D4.UnionExpression(localExpression, CompileSelectExpression(plan, LTableOperator.SelectExpression)); break;
					case TableOperator.Difference: localExpression = new D4.DifferenceExpression(localExpression, CompileSelectExpression(plan, LTableOperator.SelectExpression)); break;
					case TableOperator.Intersect: localExpression = new D4.IntersectExpression(localExpression, CompileSelectExpression(plan, LTableOperator.SelectExpression)); break;
					default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, LTableOperator.TableOperator.ToString());
				}
			}
			return localExpression;
		}
		
		protected class NamedExpression
		{
			public NamedExpression() : base(){}
			public NamedExpression(string name, Expression expression)
			{
				this.name = name;
				this.expression = expression;
			}
			
			private string name;
			public string Name 
			{
				get { return name; } 
				set { name = value; } 
			}
			
			private Expression expression;
			public Expression Expression 
			{ 
				get { return expression; } 
				set { expression = value; } 
			}
		}
		
		#if USETYPEDLIST
		protected class NamedExpressions : TypedList
		{
			public NamedExpressions() : base(typeof(NamedExpression)){}
			
			public new NamedExpression this[int AIndex]
			{ 
				get { return (NamedExpression)base[AIndex]; } 
				set { base[AIndex] = value; } 
			}
			
		#else
		protected class NamedExpressions : BaseList<NamedExpression>
		{
		#endif
			public NamedExpression this[string name]
			{
				get
				{
					int index = IndexOf(name);
					if (index >= 0)
						return this[index];
					else
						throw new LanguageException(LanguageException.Codes.NamedExpressionNotFound, name);
				}
			}
			
			public NamedExpression this[Expression expression]
			{
				get
				{
					int index = IndexOf(expression);
					if (index >= 0)
						return this[index];
					else
						throw new LanguageException(LanguageException.Codes.NamedExpressionNotFoundByExpression);
				}
			}
			
			public int IndexOf(string name)
			{
				for (int index = 0; index < Count; index++)
					if (String.Compare(this[index].Name, name) == 0)
						return index;
				return -1;
			}
			
			public int IndexOf(Expression expression)
			{
				for (int index = 0; index < Count; index++)
					if (Compiler.ExpressionsEqual(expression, this[index].Expression))
						return index;
				return -1;
			}
		}
		
		protected void GatherPreAggregateExpressions(Plan plan, Expression expression)
		{
			if (expression is AggregateCallExpression)
			{
				AggregateCallExpression aggregateCallExpression = (AggregateCallExpression)expression;
				if ((aggregateCallExpression.Expressions.Count > 0) && !(aggregateCallExpression.Expressions[0] is IdentifierExpression))
				{
					if (plan.Context.PreAggregateExpressions.IndexOf(aggregateCallExpression.Expressions[0]) < 0)
						plan.Context.PreAggregateExpressions.Add(new NamedExpression(plan.Context.GetExpressionName(aggregateCallExpression.Expressions[0]), aggregateCallExpression.Expressions[0]));
				}
			}
			else if (expression is UnaryExpression)
				GatherPreAggregateExpressions(plan, ((UnaryExpression)expression).Expression);
			else if (expression is BinaryExpression)
			{
				GatherPreAggregateExpressions(plan, ((BinaryExpression)expression).LeftExpression);
				GatherPreAggregateExpressions(plan, ((BinaryExpression)expression).RightExpression);
			}
			else if (expression is BetweenExpression)
			{
				BetweenExpression betweenExpression = (BetweenExpression)expression;
				GatherPreAggregateExpressions(plan, betweenExpression.Expression);
				GatherPreAggregateExpressions(plan, betweenExpression.LowerExpression);
				GatherPreAggregateExpressions(plan, betweenExpression.UpperExpression);
			}
			else if (expression is CaseExpression)
			{
				CaseExpression caseExpression = (CaseExpression)expression;
				if (caseExpression.Expression != null)
					GatherPreAggregateExpressions(plan, caseExpression.Expression);
				foreach (CaseItemExpression caseItemExpression in caseExpression.CaseItems)
				{
					GatherPreAggregateExpressions(plan, caseItemExpression.WhenExpression);
					GatherPreAggregateExpressions(plan, caseItemExpression.ThenExpression);
				}
				if (caseExpression.ElseExpression != null)
					GatherPreAggregateExpressions(plan, ((CaseElseExpression)caseExpression.ElseExpression).Expression);
			}
			else if (expression is CallExpression)
			{
				foreach (Expression localocalExpression in ((CallExpression)expression).Expressions)
					GatherPreAggregateExpressions(plan, localocalExpression);
			}
			else if (expression is IndexerExpression)
			{
				IndexerExpression indexerExpression = (IndexerExpression)expression;
				GatherPreAggregateExpressions(plan, indexerExpression.Expression);
				GatherPreAggregateExpressions(plan, indexerExpression.Indexer);
			}
			else if (expression is QualifierExpression)
			{
				QualifierExpression qualifierExpression = (QualifierExpression)expression;
				GatherPreAggregateExpressions(plan, qualifierExpression.LeftExpression);
				GatherPreAggregateExpressions(plan, qualifierExpression.RightExpression);
			}
		}
		
		protected void GatherAggregateExpressions(Plan plan, Expression expression)
		{
			if (expression is AggregateCallExpression)
			{
				if (plan.Context.AggregateExpressions.IndexOf(expression) < 0)
					plan.Context.AggregateExpressions.Add(new NamedExpression(plan.Context.GetExpressionName(expression), expression));
			}
			else if (expression is UnaryExpression)
				GatherAggregateExpressions(plan, ((UnaryExpression)expression).Expression);
			else if (expression is BinaryExpression)
			{
				GatherAggregateExpressions(plan, ((BinaryExpression)expression).LeftExpression);
				GatherAggregateExpressions(plan, ((BinaryExpression)expression).RightExpression);
			}
			else if (expression is BetweenExpression)
			{
				BetweenExpression LBetweenExpression = (BetweenExpression)expression;
				GatherAggregateExpressions(plan, LBetweenExpression.Expression);
				GatherAggregateExpressions(plan, LBetweenExpression.LowerExpression);
				GatherAggregateExpressions(plan, LBetweenExpression.UpperExpression);
			}
			else if (expression is CaseExpression)
			{
				CaseExpression caseExpression = (CaseExpression)expression;
				if (caseExpression.Expression != null)
					GatherAggregateExpressions(plan, caseExpression.Expression);
				foreach (CaseItemExpression caseItemExpression in caseExpression.CaseItems)
				{
					GatherAggregateExpressions(plan, caseItemExpression.WhenExpression);
					GatherAggregateExpressions(plan, caseItemExpression.ThenExpression);
				}
				if (caseExpression.ElseExpression != null)
					GatherAggregateExpressions(plan, ((CaseElseExpression)caseExpression.ElseExpression).Expression);
			}
			else if (expression is CallExpression)
			{
				foreach (Expression localocalExpression in ((CallExpression)expression).Expressions)
					GatherAggregateExpressions(plan, localocalExpression);
			}
			else if (expression is IndexerExpression)
			{
				IndexerExpression indexerExpression = (IndexerExpression)expression;
				GatherAggregateExpressions(plan, indexerExpression.Expression);
				GatherAggregateExpressions(plan, indexerExpression.Indexer);
			}
			else if (expression is QualifierExpression)
			{
				QualifierExpression qualifierExpression = (QualifierExpression)expression;
				GatherAggregateExpressions(plan, qualifierExpression.LeftExpression);
				GatherAggregateExpressions(plan, qualifierExpression.RightExpression);
			}
		}
		
		protected Expression CollapseQualifierExpressions(Expression expression)
		{
			if (expression is UnaryExpression)
			{
				UnaryExpression unaryExpression = (UnaryExpression)expression;
				unaryExpression.Expression = CollapseQualifierExpressions(unaryExpression.Expression);
				return unaryExpression;
			}
			else if (expression is BinaryExpression)
			{
				BinaryExpression binaryExpression = (BinaryExpression)expression;
				binaryExpression.LeftExpression = CollapseQualifierExpressions(binaryExpression.LeftExpression);
				binaryExpression.RightExpression = CollapseQualifierExpressions(binaryExpression.RightExpression);
				return binaryExpression;
			}
			else if (expression is BetweenExpression)
			{
				BetweenExpression betweenExpression = (BetweenExpression)expression;
				betweenExpression.Expression = CollapseQualifierExpressions(betweenExpression.Expression);
				betweenExpression.LowerExpression = CollapseQualifierExpressions(betweenExpression.LowerExpression);
				betweenExpression.UpperExpression = CollapseQualifierExpressions(betweenExpression.UpperExpression);
				return betweenExpression;
			}
			else if (expression is CaseExpression)
			{
				CaseExpression caseExpression = (CaseExpression)expression;
				if (caseExpression.Expression != null)
					caseExpression.Expression = CollapseQualifierExpressions(caseExpression.Expression);
				foreach (CaseItemExpression LCaseItemExpression in caseExpression.CaseItems)
				{
					LCaseItemExpression.WhenExpression = CollapseQualifierExpressions(LCaseItemExpression.WhenExpression);
					LCaseItemExpression.ThenExpression = CollapseQualifierExpressions(LCaseItemExpression.ThenExpression);
				}
				caseExpression.ElseExpression = (CaseElseExpression)CollapseQualifierExpressions(caseExpression.ElseExpression);
				return caseExpression;
			}
			else if (expression is CaseElseExpression)
			{
				CaseElseExpression caseElseExpression = (CaseElseExpression)expression;
				caseElseExpression.Expression = CollapseQualifierExpressions(caseElseExpression.Expression);
				return caseElseExpression;
			}
			else if (expression is AggregateCallExpression)
			{
				AggregateCallExpression aggregateCallExpression = (AggregateCallExpression)expression;
				for (int index = 0; index < aggregateCallExpression.Expressions.Count; index++)
					aggregateCallExpression.Expressions[index] = CollapseQualifierExpressions(aggregateCallExpression.Expressions[index]);
				return aggregateCallExpression;
			}
			else if (expression is CallExpression)
			{
				CallExpression CallExpression = (CallExpression)expression;
				for (int index = 0; index < CallExpression.Expressions.Count; index++)
					CallExpression.Expressions[index] = CollapseQualifierExpressions(CallExpression.Expressions[index]);
				return CallExpression;
			}
			else if (expression is IndexerExpression)
			{
				IndexerExpression indexerExpression = (IndexerExpression)expression;
				indexerExpression.Expression = CollapseQualifierExpressions(indexerExpression.Expression);
				indexerExpression.Indexer = CollapseQualifierExpressions(indexerExpression.Indexer);
				return indexerExpression;
			}
			else if (expression is QualifierExpression)
			{
				QualifierExpression qualifierExpression = (QualifierExpression)expression;
				qualifierExpression.LeftExpression = CollapseQualifierExpressions(qualifierExpression.LeftExpression);
				qualifierExpression.RightExpression = CollapseQualifierExpressions(qualifierExpression.RightExpression);
				if (qualifierExpression.LeftExpression is IdentifierExpression)
				{
					IdentifierExpression leftExpression = (IdentifierExpression)qualifierExpression.LeftExpression;
					if (qualifierExpression.RightExpression is IdentifierExpression)
					{
						leftExpression.Identifier =
							Schema.Object.Qualify(((IdentifierExpression)qualifierExpression.RightExpression).Identifier, leftExpression.Identifier);
						return leftExpression;
					}
					else if (qualifierExpression.RightExpression is CallExpression)
					{
						CallExpression CallExpression = (CallExpression)qualifierExpression.RightExpression;
						CallExpression.Identifier = Schema.Object.Qualify(CallExpression.Identifier, leftExpression.Identifier);
						return CallExpression;
					}
				}
				return qualifierExpression;
			}
			else
				return expression;
		}
		
		protected static bool ExpressionsEqual(Expression leftExpression, Expression rightExpression)
		{
			if (leftExpression is UnaryExpression)
				return (rightExpression is UnaryExpression) && UnaryExpressionsEqual((UnaryExpression)leftExpression, (UnaryExpression)rightExpression);
			else if (leftExpression is BinaryExpression)
				return (rightExpression is BinaryExpression) && BinaryExpressionsEqual((BinaryExpression)leftExpression, (BinaryExpression)rightExpression);
			else if (leftExpression is BetweenExpression)
				return (rightExpression is BetweenExpression) && BetweenExpressionsEqual((BetweenExpression)leftExpression, (BetweenExpression)rightExpression);
			else if (leftExpression is CaseExpression)
				return (rightExpression is CaseExpression) && CaseExpressionsEqual((CaseExpression)leftExpression, (CaseExpression)rightExpression);
			else if (leftExpression is AggregateCallExpression)
				return (rightExpression is AggregateCallExpression) && AggregateCallExpressionsEqual((AggregateCallExpression)leftExpression, (AggregateCallExpression)rightExpression);
			else if (leftExpression is CallExpression)
				return (rightExpression is CallExpression) && CallExpressionsEqual((CallExpression)leftExpression, (CallExpression)rightExpression);
			else if (leftExpression is IndexerExpression)
				return (rightExpression is IndexerExpression) && IndexerExpressionsEqual((IndexerExpression)leftExpression, (IndexerExpression)rightExpression);
			else if (leftExpression is QualifierExpression)
				return (rightExpression is QualifierExpression) && QualifierExpressionsEqual((QualifierExpression)leftExpression, (QualifierExpression)rightExpression);
			else if (leftExpression is IdentifierExpression)
				return (rightExpression is IdentifierExpression) && IdentifierExpressionsEqual((IdentifierExpression)leftExpression, (IdentifierExpression)rightExpression);
			else if (leftExpression is ValueExpression)
				return (rightExpression is ValueExpression) && ValueExpressionsEqual((ValueExpression)leftExpression, (ValueExpression)rightExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, leftExpression.GetType().Name);
		}
		
		protected static bool UnaryExpressionsEqual(UnaryExpression leftExpression, UnaryExpression rightExpression)
		{
			return (String.Compare(leftExpression.Instruction, rightExpression.Instruction) == 0) && ExpressionsEqual(leftExpression.Expression, rightExpression.Expression);
		}
		
		protected static bool BinaryExpressionsEqual(BinaryExpression leftExpression, BinaryExpression rightExpression)
		{
			return 
				(String.Compare(leftExpression.Instruction, rightExpression.Instruction) == 0) && 
				ExpressionsEqual(leftExpression.LeftExpression, rightExpression.LeftExpression) &&
				ExpressionsEqual(leftExpression.RightExpression, rightExpression.RightExpression);
		}
		
		protected static bool BetweenExpressionsEqual(BetweenExpression leftExpression, BetweenExpression rightExpression)
		{
			return 
				ExpressionsEqual(leftExpression.Expression, rightExpression.Expression) &&
				ExpressionsEqual(leftExpression.LowerExpression, rightExpression.LowerExpression) &&
				ExpressionsEqual(leftExpression.UpperExpression, rightExpression.UpperExpression);
		}
		
		protected static bool CaseExpressionsEqual(CaseExpression leftExpression, CaseExpression rightExpression)
		{
			bool LResult = 
				(
					(
						(leftExpression.Expression == null) && 
						(rightExpression.Expression == null)
					) ||
					(
						(leftExpression.Expression != null) && 
						(rightExpression.Expression != null) && 
						ExpressionsEqual(leftExpression.Expression, rightExpression.Expression)
					)
				);
			if (LResult)
			{
				for (int index = 0; index < leftExpression.CaseItems.Count; index++)
				{
					LResult = 
						LResult && 
						(index < rightExpression.CaseItems.Count) && 
						CaseItemExpressionsEqual(leftExpression.CaseItems[index], rightExpression.CaseItems[index]);
					if (!LResult)
						break;
				}
			}
			
			LResult =
				LResult &&
				CaseElseExpressionsEqual
				(
					(CaseElseExpression)leftExpression.ElseExpression, 
					(CaseElseExpression)rightExpression.ElseExpression
				);
			
			return LResult;
		}
		
		protected static bool CaseItemExpressionsEqual(CaseItemExpression leftExpression, CaseItemExpression rightExpression)
		{
			return 
				ExpressionsEqual(leftExpression.WhenExpression, rightExpression.WhenExpression) &&
				ExpressionsEqual(leftExpression.ThenExpression, rightExpression.ThenExpression);
		}
		
		protected static bool CaseElseExpressionsEqual(CaseElseExpression leftExpression, CaseElseExpression rightExpression)
		{
			return ExpressionsEqual(leftExpression.Expression, rightExpression.Expression);
		}
		
		protected static bool AggregateCallExpressionsEqual(AggregateCallExpression leftExpression, AggregateCallExpression rightExpression)
		{
			return 
				(String.Compare(leftExpression.Identifier, rightExpression.Identifier) == 0) && 
				(leftExpression.IsDistinct == rightExpression.IsDistinct) &&
				(leftExpression.IsRowLevel == rightExpression.IsRowLevel) &&
				(leftExpression.IsRowLevel || ExpressionsEqual(leftExpression.Expressions[0], rightExpression.Expressions[0]));
		}
		
		protected static bool CallExpressionsEqual(CallExpression leftExpression, CallExpression rightExpression)
		{
			bool LResult = String.Compare(leftExpression.Identifier, rightExpression.Identifier) == 0;
			for (int index = 0; index < leftExpression.Expressions.Count; index++)
			{
				LResult =
					LResult &&
					(index < rightExpression.Expressions.Count) &&
					ExpressionsEqual(leftExpression.Expressions[index], rightExpression.Expressions[index]);
				if (!LResult)
					break;
			}
			return LResult;
		}
		
		protected static bool IndexerExpressionsEqual(IndexerExpression leftExpression, IndexerExpression rightExpression)
		{
			return
				ExpressionsEqual(leftExpression.Expression, rightExpression.Expression) &&
				ExpressionsEqual(leftExpression.Indexer, rightExpression.Indexer);
		}
		
		protected static bool QualifierExpressionsEqual(QualifierExpression leftExpression, QualifierExpression rightExpression)
		{
			return
				ExpressionsEqual(leftExpression.LeftExpression, rightExpression.LeftExpression) &&
				ExpressionsEqual(leftExpression.RightExpression, rightExpression.RightExpression);
		}
		
		protected static bool IdentifierExpressionsEqual(IdentifierExpression leftExpression, IdentifierExpression rightExpression)
		{
			return String.Compare(leftExpression.Identifier, rightExpression.Identifier) == 0;
		}
		
		protected static bool ValueExpressionsEqual(ValueExpression leftExpression, ValueExpression rightExpression)
		{
			return 
				(
					(leftExpression.Value == null) && 
					(rightExpression.Value == null)
				) ||
				(
					(leftExpression.Value != null) && 
					(leftExpression.Value.Equals(rightExpression.Value))
				);
		}
		
		protected Expression CompileSelectExpression(Plan plan, SelectExpression expression)
		{
			plan.PushContext();
			Expression localExpression = CompileFromClause(plan, (AlgebraicFromClause)expression.FromClause);
			if (expression.WhereClause != null)
			{
				expression.WhereClause.Expression = CollapseQualifierExpressions(expression.WhereClause.Expression);
				D4.RestrictExpression restrictExpression = new D4.RestrictExpression();
				restrictExpression.Expression = localExpression;
				restrictExpression.Condition = CompileExpression(plan, expression.WhereClause.Expression);
				localExpression = restrictExpression;
			}
			
			// gather all expressions within aggregate invocations and the group and having clauses and name them if necessary, recursively
			// gather all aggregate invocation expressions within the select list and the having clause and name them if necessary, recursively
			foreach (ColumnExpression column in expression.SelectClause.Columns)
			{
				column.Expression = CollapseQualifierExpressions(column.Expression);
				GatherPreAggregateExpressions(plan, column.Expression);
				GatherAggregateExpressions(plan, column.Expression);
			}
				
			if (expression.GroupClause != null)
				for (int index = 0; index < expression.GroupClause.Columns.Count; index++)
				{
					expression.GroupClause.Columns[index] = CollapseQualifierExpressions(expression.GroupClause.Columns[index]);
					GatherPreAggregateExpressions(plan, expression.GroupClause.Columns[index]);
				}
					
			if (expression.HavingClause != null)
			{
				expression.HavingClause.Expression = CollapseQualifierExpressions(expression.HavingClause.Expression);
				GatherPreAggregateExpressions(plan, expression.HavingClause.Expression);
				GatherAggregateExpressions(plan, expression.HavingClause.Expression);
			}
			
			// add an extend expression with the pre aggregation extend columns, if necessary
			if (plan.Context.PreAggregateExpressions.Count > 0)
			{
				D4.ExtendExpression extendExpression = new D4.ExtendExpression();
				extendExpression.Expression = localExpression;
				foreach (NamedExpression LNamedExpression in plan.Context.PreAggregateExpressions)
					extendExpression.Expressions.Add(new D4.NamedColumnExpression(CompileExpression(plan, LNamedExpression.Expression), LNamedExpression.Name));
				localExpression = extendExpression;
			}
			
			// add a group expression with the prepared group by columns and the given aggregate invocations
			if ((expression.GroupClause != null) || (plan.Context.AggregateExpressions.Count > 0))
			{
				if (plan.Context.AggregateExpressions.Count > 0)
				{
					D4.AggregateExpression aggregateExpression = new D4.AggregateExpression();
					aggregateExpression.Expression = localExpression;

					if (expression.GroupClause != null)
					{
						foreach (Expression groupExpression in expression.GroupClause.Columns)
						{
							if (groupExpression is IdentifierExpression)
								aggregateExpression.ByColumns.Add(new D4.ColumnExpression(((IdentifierExpression)groupExpression).Identifier));
							else
								aggregateExpression.ByColumns.Add(new D4.ColumnExpression(plan.Context.PreAggregateExpressions[groupExpression].Name));
						}
					}

					foreach (NamedExpression namedExpression in plan.Context.AggregateExpressions)
					{
						D4.AggregateColumnExpression aggregateColumnExpression = new D4.AggregateColumnExpression();
						AggregateCallExpression aggregateCallExpression = (AggregateCallExpression)namedExpression.Expression;
						aggregateColumnExpression.AggregateOperator = aggregateCallExpression.Identifier;
						aggregateColumnExpression.Distinct = aggregateCallExpression.IsDistinct;
						if (aggregateCallExpression.Expressions.Count > 0)
							if (aggregateCallExpression.Expressions[0] is IdentifierExpression)
								aggregateColumnExpression.Columns.Add(new D4.ColumnExpression(((IdentifierExpression)aggregateCallExpression.Expressions[0]).Identifier));
							else
								aggregateColumnExpression.Columns.Add(new D4.ColumnExpression(plan.Context.PreAggregateExpressions[aggregateCallExpression.Expressions[0]].Name));
						aggregateColumnExpression.ColumnAlias = namedExpression.Name;
						aggregateExpression.ComputeColumns.Add(aggregateColumnExpression);
					}

					localExpression = aggregateExpression;
				}
				else
				{
					D4.ProjectExpression projectExpression = new D4.ProjectExpression();
					projectExpression.Expression = localExpression;
					foreach (Expression groupExpression in expression.GroupClause.Columns)
					{
						if (groupExpression is IdentifierExpression)
							projectExpression.Columns.Add(new D4.ColumnExpression(((IdentifierExpression)groupExpression).Identifier));
						else
							projectExpression.Columns.Add(new D4.ColumnExpression(plan.Context.PreAggregateExpressions[groupExpression].Name));
					}
					localExpression = projectExpression;
				}
			}
			
			if (expression.HavingClause != null)
			{
				D4.RestrictExpression restrictExpression = new D4.RestrictExpression();
				restrictExpression.Expression = localExpression;
				restrictExpression.Condition = CompileExpression(plan, expression.HavingClause.Expression);
				localExpression = restrictExpression;
			}
			
			// gather all extend expressions within the select columns and name them, if necessary
			foreach (ColumnExpression columnExpression in expression.SelectClause.Columns)
				if (columnExpression.Expression is IdentifierExpression)
				{
					if (columnExpression.ColumnAlias == String.Empty)
						columnExpression.ColumnAlias = ((IdentifierExpression)columnExpression.Expression).Identifier;
				}
				else if (columnExpression.Expression is AggregateCallExpression)
				{
					if (columnExpression.ColumnAlias == String.Empty)
						columnExpression.ColumnAlias = plan.Context.GetExpressionName(columnExpression.Expression);
				}
				else 
				{
					if (columnExpression.ColumnAlias == String.Empty)
						columnExpression.ColumnAlias = plan.Context.GetExpressionName(columnExpression.Expression);
					if (plan.Context.ExtendExpressions.IndexOf(columnExpression.Expression) < 0)
						plan.Context.ExtendExpressions.Add(new NamedExpression(columnExpression.ColumnAlias, columnExpression.Expression));
				}
		
			// add an extend expression with the post aggregation extend columns, if necessary
			if (plan.Context.ExtendExpressions.Count > 0)
			{
				D4.ExtendExpression extendExpression = new D4.ExtendExpression();
				extendExpression.Expression = localExpression;
				foreach (NamedExpression namedExpression in plan.Context.ExtendExpressions)
					extendExpression.Expressions.Add(new D4.NamedColumnExpression(CompileExpression(plan, namedExpression.Expression), namedExpression.Name));
				localExpression = extendExpression;
			}
			
			// gather all the rename expressions within the select columns
			foreach (ColumnExpression columnExpression in expression.SelectClause.Columns)
				if (columnExpression.ColumnAlias == String.Empty)
				{
					if (columnExpression.Expression is IdentifierExpression)
						columnExpression.ColumnAlias = ((IdentifierExpression)columnExpression.Expression).Identifier;
				}
				else if ((columnExpression.Expression is IdentifierExpression) && (String.Compare(((IdentifierExpression)columnExpression.Expression).Identifier, columnExpression.ColumnAlias) != 0))
				{
					plan.Context.RenameExpressions.Add(new NamedExpression(columnExpression.ColumnAlias, columnExpression.Expression));
				}
				
			// add a rename expression if necessary
			if (plan.Context.RenameExpressions.Count > 0)
			{
				D4.RenameExpression renameExpression = new D4.RenameExpression();
				renameExpression.Expression = localExpression;
				foreach (NamedExpression namedExpression in plan.Context.RenameExpressions)
					renameExpression.Expressions.Add(new D4.RenameColumnExpression(((IdentifierExpression)namedExpression.Expression).Identifier, namedExpression.Name));
				localExpression = renameExpression;
			}
			
			// project over the final columns, if necessary
			if (expression.SelectClause.Columns.Count > 0)
			{
				D4.ProjectExpression projectExpression = new D4.ProjectExpression();
				projectExpression.Expression = localExpression;
				foreach (ColumnExpression columnExpression in expression.SelectClause.Columns)
					projectExpression.Columns.Add(new D4.ColumnExpression(columnExpression.ColumnAlias));
				localExpression = projectExpression;
			}

			plan.PopContext();
			return localExpression;
		}
		
		protected Expression CompileFromClause(Plan plan, AlgebraicFromClause fromClause)
		{
			Expression localExpression = CompileTableSpecifier(plan, fromClause.TableSpecifier);
			foreach (JoinClause join in fromClause.Joins)
			{
				Expression rightExpression = CompileFromClause(plan, join.FromClause);
				switch (join.JoinType)
				{
					case JoinType.Cross: 
						D4.ProductExpression productExpression = new D4.ProductExpression();
						productExpression.LeftExpression = localExpression;
						productExpression.RightExpression = rightExpression;
						localExpression = productExpression;
					break;
					
					case JoinType.Inner:
						D4.InnerJoinExpression innerJoinExpression = new D4.InnerJoinExpression();
						innerJoinExpression.LeftExpression = localExpression;
						innerJoinExpression.RightExpression = rightExpression;
						innerJoinExpression.Condition = CompileExpression(plan, join.JoinExpression);
						localExpression = innerJoinExpression;
					break;
					
					case JoinType.Left:
						D4.LeftOuterJoinExpression leftJoinExpression = new D4.LeftOuterJoinExpression();
						leftJoinExpression.LeftExpression = localExpression;
						leftJoinExpression.RightExpression = rightExpression;
						leftJoinExpression.Condition = CompileExpression(plan, join.JoinExpression);
						localExpression = leftJoinExpression;
					break;
					
					case JoinType.Right:
						D4.RightOuterJoinExpression rightJoinExpression = new D4.RightOuterJoinExpression();
						rightJoinExpression.LeftExpression = localExpression;
						rightJoinExpression.RightExpression = rightExpression;
						rightJoinExpression.Condition = CompileExpression(plan, join.JoinExpression);
						localExpression = rightJoinExpression;
					break;
					
					default: throw new LanguageException(LanguageException.Codes.UnknownJoinType, join.JoinType.ToString());
				}
			}
			
			return localExpression;
		}
		
		protected Expression CompileTableSpecifier(Plan plan, TableSpecifier tableSpecifier)
		{
			string tableAlias;
			Expression localExpression;
			if (tableSpecifier.TableExpression is TableExpression)
			{
				localExpression = new IdentifierExpression(((TableExpression)tableSpecifier.TableExpression).TableName);
				if (tableSpecifier.TableAlias != String.Empty)
					tableAlias = tableSpecifier.TableAlias;
				else
					tableAlias = ((TableExpression)tableSpecifier.TableExpression).TableName;
			}
			else if (tableSpecifier.TableExpression is QueryExpression)
			{
				localExpression = CompileQueryExpression(plan, (QueryExpression)tableSpecifier.TableExpression);
				if (tableSpecifier.TableAlias != String.Empty)
					tableAlias = tableSpecifier.TableAlias;
				else
					throw new LanguageException(LanguageException.Codes.TableAliasRequired);
			}
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, tableSpecifier.TableExpression.GetType().Name);

			return new D4.RenameAllExpression(localExpression, tableAlias);
		}
		
		protected Expression CompileExpression(Plan plan, Expression expression)
		{
			if (expression is UnaryExpression)
				return CompileUnaryExpression(plan, (UnaryExpression)expression);
			else if (expression is BinaryExpression)
				return CompileBinaryExpression(plan, (BinaryExpression)expression);
			else if (expression is BetweenExpression)
				return CompileBetweenExpression(plan, (BetweenExpression)expression);
			else if (expression is CaseExpression)
				return CompileCaseExpression(plan, (CaseExpression)expression);
			else if (expression is AggregateCallExpression)
				return CompileAggregateCallExpression(plan, (AggregateCallExpression)expression);
			else if (expression is CallExpression)
				return CompileCallExpression(plan, (CallExpression)expression);
			else if (expression is IndexerExpression)
				return CompileIndexerExpression(plan, (IndexerExpression)expression);
			else if (expression is QualifierExpression)
				return CompileQualifierExpression(plan, (QualifierExpression)expression);
			else if (expression is IdentifierExpression)
				return expression;
			else if (expression is ValueExpression)
				return expression;
			else if (expression is QueryExpression)
				return CompileQueryExpression(plan, (QueryExpression)expression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, expression.GetType().Name);
		}
		
		protected Expression CompileUnaryExpression(Plan plan, UnaryExpression expression)
		{
			expression.Expression = CompileExpression(plan, expression.Expression);
			return expression;
		}
		
		protected Expression CompileBinaryExpression(Plan plan, BinaryExpression expression)
		{
			expression.LeftExpression = CompileExpression(plan, expression.LeftExpression);
			expression.RightExpression = CompileExpression(plan, expression.RightExpression);
			return expression;
		}
		
		protected Expression CompileBetweenExpression(Plan plan, BetweenExpression expression)
		{
			expression.Expression = CompileExpression(plan, expression.Expression);
			expression.LowerExpression = CompileExpression(plan, expression.LowerExpression);
			expression.UpperExpression = CompileExpression(plan, expression.UpperExpression);
			return expression;
		}
		
		protected Expression CompileCaseExpression(Plan plan, CaseExpression expression)
		{
			if (expression.Expression != null)
				expression.Expression = CompileExpression(plan, expression.Expression);
			foreach (CaseItemExpression caseItemExpression in expression.CaseItems)
			{
				caseItemExpression.WhenExpression = CompileExpression(plan, caseItemExpression.WhenExpression);
				caseItemExpression.ThenExpression = CompileExpression(plan, caseItemExpression.ThenExpression);
			}
			((CaseElseExpression)expression.ElseExpression).Expression = CompileExpression(plan, ((CaseElseExpression)expression.ElseExpression).Expression);
			return expression;
		}
		
		protected Expression CompileAggregateCallExpression(Plan plan, AggregateCallExpression expression)
		{
			return new IdentifierExpression(plan.Context.AggregateExpressions[expression].Name);
		}
		
		protected Expression CompileCallExpression(Plan plan, CallExpression expression)
		{
			for (int index = 0; index < expression.Expressions.Count; index++)
				expression.Expressions[index] = CompileExpression(plan, expression.Expressions[index]);
			return expression;
		}
		
		protected Expression CompileIndexerExpression(Plan plan, IndexerExpression expression)
		{
			expression.Expression = CompileExpression(plan, expression.Expression);
			expression.Indexer = CompileExpression(plan, expression.Indexer);
			return expression;
		}
		
		protected Expression CompileQualifierExpression(Plan plan, QualifierExpression expression)
		{
			expression.LeftExpression = CompileExpression(plan, expression.LeftExpression);
			expression.RightExpression = CompileExpression(plan, expression.RightExpression);
			return expression;
		}
		
		protected Statement CompileInsertStatement(Plan plan, InsertStatement statement)
		{
			D4.InsertStatement localStatement = new D4.InsertStatement();
			localStatement.Target = new IdentifierExpression(statement.InsertClause.TableExpression.TableName);
			if (statement.Values is QueryExpression)
				localStatement.SourceExpression = CompileQueryExpression(plan, (QueryExpression)statement.Values);
			else if (statement.Values is ValuesExpression)
			{
				D4.TableSelectorExpression tableExpression = new D4.TableSelectorExpression();
				D4.RowSelectorExpression rowExpression = new D4.RowSelectorExpression();
				ValuesExpression valuesExpression = (ValuesExpression)statement.Values;
				for (int index = 0; index < valuesExpression.Expressions.Count; index++)
					rowExpression.Expressions.Add(new D4.NamedColumnExpression(CompileExpression(plan, valuesExpression.Expressions[index]), statement.InsertClause.Columns[index].FieldName));
				tableExpression.Expressions.Add(rowExpression);
				tableExpression.Keys.Add(new D4.KeyDefinition());
				localStatement.SourceExpression = tableExpression;
			}
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, statement.Values.GetType().Name);
			return localStatement;
		}
		
		protected Statement CompileUpdateStatement(Plan plan, UpdateStatement statement)
		{
			D4.UpdateStatement localStatement = new D4.UpdateStatement();
			localStatement.Target = new IdentifierExpression(statement.UpdateClause.TableExpression.TableName);
			foreach (UpdateFieldExpression localExpression in statement.UpdateClause.Columns)
				localStatement.Columns.Add(new D4.UpdateColumnExpression(new IdentifierExpression(localExpression.FieldName), CompileExpression(plan, localExpression.Expression)));
			if (statement.WhereClause != null)
				localStatement.Condition = CompileExpression(plan, statement.WhereClause.Expression);
			return localStatement;
		}
		
		protected Statement CompileDeleteStatement(Plan plan, DeleteStatement statement)
		{
			D4.DeleteStatement localStatement = new D4.DeleteStatement();
			localStatement.Target = new IdentifierExpression(statement.DeleteClause.TableExpression.TableName);
			if (statement.WhereClause != null)
				localStatement.Target = new D4.RestrictExpression(localStatement.Target, CompileExpression(plan, statement.WhereClause.Expression));
			return localStatement;
		}
	}
	
	// operator RealSQLToD4(ARealSQlocalStatement : string) : string;
	public class RealSQLToD4Node : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return 
				new D4.D4TextEmitter().Emit
				(	
					new Compiler().Compile
					(
						new Parser().ParseStatement
						(
							(string)arguments[0]
						)
					)
				);
		}
	}
}

