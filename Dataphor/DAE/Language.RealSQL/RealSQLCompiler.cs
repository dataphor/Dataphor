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
			private NamedExpressions FPreAggregateExpressions = new NamedExpressions();
			public NamedExpressions PreAggregateExpressions { get { return FPreAggregateExpressions; } }
			
			private NamedExpressions FAggregateExpressions = new NamedExpressions();
			public NamedExpressions AggregateExpressions { get { return FAggregateExpressions; } }
			
			private NamedExpressions FExtendExpressions = new NamedExpressions();
			public NamedExpressions ExtendExpressions { get { return FExtendExpressions; } }

			private NamedExpressions FRenameExpressions = new NamedExpressions();
			public NamedExpressions RenameExpressions { get { return FRenameExpressions; } }

			protected int FExpressionCount;
			protected string GetNextUniqueName()
			{
				FExpressionCount++;
				return String.Format("Expression{0}", FExpressionCount.ToString());
			}

			public string GetUniqueName()
			{
				string LName;
				do
				{
					LName = GetNextUniqueName();
				} while (IndexOf(LName) >= 0);
				return LName;
			}
			
			public string GetExpressionName(Expression AExpression)
			{
				int LIndex = FPreAggregateExpressions.IndexOf(AExpression);
				if (LIndex >= 0)
					return FPreAggregateExpressions[LIndex].Name;
				LIndex = FAggregateExpressions.IndexOf(AExpression);
				if (LIndex >= 0)
					return FAggregateExpressions[LIndex].Name;
				LIndex = FExtendExpressions.IndexOf(AExpression);
				if (LIndex >= 0)
					return FExtendExpressions[LIndex].Name;
				LIndex = FRenameExpressions.IndexOf(AExpression);
				if (LIndex >= 0)
					return FRenameExpressions[LIndex].Name;
				return GetUniqueName();
			}
			
			protected int IndexOf(string AName)
			{
				int LIndex = FPreAggregateExpressions.IndexOf(AName);
				if (LIndex >= 0)
					return LIndex;
				LIndex = FAggregateExpressions.IndexOf(AName);
				if (LIndex >= 0)
					return LIndex;
				LIndex = FExtendExpressions.IndexOf(AName);
				if (LIndex >= 0)
					return LIndex;
				LIndex = FRenameExpressions.IndexOf(AName);
				if (LIndex >= 0)
					return LIndex;
				return -1;
			}
			
			protected int IndexOf(Expression AExpression)
			{
				int LIndex = FPreAggregateExpressions.IndexOf(AExpression);
				if (LIndex >= 0)
					return LIndex;
				LIndex = FAggregateExpressions.IndexOf(AExpression);
				if (LIndex >= 0)
					return LIndex;
				LIndex = FExtendExpressions.IndexOf(AExpression);
				if (LIndex >= 0)
					return LIndex;
				LIndex = FRenameExpressions.IndexOf(AExpression);
				if (LIndex >= 0)
					return LIndex;
				return -1;
			}
		}
		
		protected class Plan : System.Object
		{
			protected List FContexts = new List();
			
			public void PushContext()
			{
				FContexts.Add(new PlanContext());
			}
			
			public void PopContext()
			{
				FContexts.RemoveAt(FContexts.Count -1);
			}
			
			public PlanContext Context { get { return (PlanContext)FContexts[FContexts.Count - 1]; } }
		}
		
		public Statement Compile(Statement AStatement)
		{
			Plan LPlan = new Plan();
			return CompileStatement(LPlan, AStatement);
		}
		
		protected Statement CompileStatement(Plan APlan, Statement AStatement)
		{
			if (AStatement is Batch)
				return CompileBatch(APlan, (Batch)AStatement);
			else if (AStatement is SelectStatement)
				return CompileSelectStatement(APlan, (SelectStatement)AStatement);
			else if (AStatement is InsertStatement)
				return CompileInsertStatement(APlan, (InsertStatement)AStatement);
			else if (AStatement is UpdateStatement)
				return CompileUpdateStatement(APlan, (UpdateStatement)AStatement);
			else if (AStatement is DeleteStatement)
				return CompileDeleteStatement(APlan, (DeleteStatement)AStatement);
			else if (AStatement is Expression)
				return new D4.ExpressionStatement(CompileExpression(APlan, (Expression)AStatement));
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected Statement CompileBatch(Plan APlan, Batch AStatement)
		{
			Block LBlock = new Block();
			foreach (Statement LStatement in AStatement.Statements)
				LBlock.Statements.Add(CompileStatement(APlan, LStatement));
			return LBlock;
		}
		
		protected Statement CompileSelectStatement(Plan APlan, SelectStatement AStatement)
		{
			D4.SelectStatement LStatement = new D4.SelectStatement();
			D4.CursorDefinition LDefinition = new D4.CursorDefinition();
			LStatement.CursorDefinition = LDefinition;
			LDefinition.Expression = CompileQueryExpression(APlan, AStatement.QueryExpression);
			if (AStatement.OrderClause != null)
			{
				D4.OrderExpression LOrderExpression = new D4.OrderExpression();
				LOrderExpression.Expression = LDefinition.Expression;
				foreach (OrderFieldExpression LOrderField in AStatement.OrderClause.Columns)
					LOrderExpression.Columns.Add(new D4.OrderColumnDefinition(Schema.Object.Qualify(LOrderField.FieldName, LOrderField.TableAlias), LOrderField.Ascending));
				LDefinition.Expression = LOrderExpression;
			}
			return LStatement;
		}
		
		protected Expression CompileQueryExpression(Plan APlan, QueryExpression AExpression)
		{
			Expression LExpression = CompileSelectExpression(APlan, AExpression.SelectExpression);
			foreach (TableOperatorExpression LTableOperator in AExpression.TableOperators)
			{
				switch (LTableOperator.TableOperator)
				{
					case TableOperator.Union: LExpression = new D4.UnionExpression(LExpression, CompileSelectExpression(APlan, LTableOperator.SelectExpression)); break;
					case TableOperator.Difference: LExpression = new D4.DifferenceExpression(LExpression, CompileSelectExpression(APlan, LTableOperator.SelectExpression)); break;
					case TableOperator.Intersect: LExpression = new D4.IntersectExpression(LExpression, CompileSelectExpression(APlan, LTableOperator.SelectExpression)); break;
					default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, LTableOperator.TableOperator.ToString());
				}
			}
			return LExpression;
		}
		
		protected class NamedExpression
		{
			public NamedExpression() : base(){}
			public NamedExpression(string AName, Expression AExpression)
			{
				FName = AName;
				FExpression = AExpression;
			}
			
			private string FName;
			public string Name 
			{
				get { return FName; } 
				set { FName = value; } 
			}
			
			private Expression FExpression;
			public Expression Expression 
			{ 
				get { return FExpression; } 
				set { FExpression = value; } 
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
			public NamedExpression this[string AName]
			{
				get
				{
					int LIndex = IndexOf(AName);
					if (LIndex >= 0)
						return this[LIndex];
					else
						throw new LanguageException(LanguageException.Codes.NamedExpressionNotFound, AName);
				}
			}
			
			public NamedExpression this[Expression AExpression]
			{
				get
				{
					int LIndex = IndexOf(AExpression);
					if (LIndex >= 0)
						return this[LIndex];
					else
						throw new LanguageException(LanguageException.Codes.NamedExpressionNotFoundByExpression);
				}
			}
			
			public int IndexOf(string AName)
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (String.Compare(this[LIndex].Name, AName) == 0)
						return LIndex;
				return -1;
			}
			
			public int IndexOf(Expression AExpression)
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (Compiler.ExpressionsEqual(AExpression, this[LIndex].Expression))
						return LIndex;
				return -1;
			}
		}
		
		protected void GatherPreAggregateExpressions(Plan APlan, Expression AExpression)
		{
			if (AExpression is AggregateCallExpression)
			{
				AggregateCallExpression LAggregateCallExpression = (AggregateCallExpression)AExpression;
				if ((LAggregateCallExpression.Expressions.Count > 0) && !(LAggregateCallExpression.Expressions[0] is IdentifierExpression))
				{
					if (APlan.Context.PreAggregateExpressions.IndexOf(LAggregateCallExpression.Expressions[0]) < 0)
						APlan.Context.PreAggregateExpressions.Add(new NamedExpression(APlan.Context.GetExpressionName(LAggregateCallExpression.Expressions[0]), LAggregateCallExpression.Expressions[0]));
				}
			}
			else if (AExpression is UnaryExpression)
				GatherPreAggregateExpressions(APlan, ((UnaryExpression)AExpression).Expression);
			else if (AExpression is BinaryExpression)
			{
				GatherPreAggregateExpressions(APlan, ((BinaryExpression)AExpression).LeftExpression);
				GatherPreAggregateExpressions(APlan, ((BinaryExpression)AExpression).RightExpression);
			}
			else if (AExpression is BetweenExpression)
			{
				BetweenExpression LBetweenExpression = (BetweenExpression)AExpression;
				GatherPreAggregateExpressions(APlan, LBetweenExpression.Expression);
				GatherPreAggregateExpressions(APlan, LBetweenExpression.LowerExpression);
				GatherPreAggregateExpressions(APlan, LBetweenExpression.UpperExpression);
			}
			else if (AExpression is CaseExpression)
			{
				CaseExpression LCaseExpression = (CaseExpression)AExpression;
				if (LCaseExpression.Expression != null)
					GatherPreAggregateExpressions(APlan, LCaseExpression.Expression);
				foreach (CaseItemExpression LCaseItemExpression in LCaseExpression.CaseItems)
				{
					GatherPreAggregateExpressions(APlan, LCaseItemExpression.WhenExpression);
					GatherPreAggregateExpressions(APlan, LCaseItemExpression.ThenExpression);
				}
				if (LCaseExpression.ElseExpression != null)
					GatherPreAggregateExpressions(APlan, ((CaseElseExpression)LCaseExpression.ElseExpression).Expression);
			}
			else if (AExpression is CallExpression)
			{
				foreach (Expression LExpression in ((CallExpression)AExpression).Expressions)
					GatherPreAggregateExpressions(APlan, LExpression);
			}
			else if (AExpression is IndexerExpression)
			{
				IndexerExpression LIndexerExpression = (IndexerExpression)AExpression;
				GatherPreAggregateExpressions(APlan, LIndexerExpression.Expression);
				GatherPreAggregateExpressions(APlan, LIndexerExpression.Indexer);
			}
			else if (AExpression is QualifierExpression)
			{
				QualifierExpression LQualifierExpression = (QualifierExpression)AExpression;
				GatherPreAggregateExpressions(APlan, LQualifierExpression.LeftExpression);
				GatherPreAggregateExpressions(APlan, LQualifierExpression.RightExpression);
			}
		}
		
		protected void GatherAggregateExpressions(Plan APlan, Expression AExpression)
		{
			if (AExpression is AggregateCallExpression)
			{
				if (APlan.Context.AggregateExpressions.IndexOf(AExpression) < 0)
					APlan.Context.AggregateExpressions.Add(new NamedExpression(APlan.Context.GetExpressionName(AExpression), AExpression));
			}
			else if (AExpression is UnaryExpression)
				GatherAggregateExpressions(APlan, ((UnaryExpression)AExpression).Expression);
			else if (AExpression is BinaryExpression)
			{
				GatherAggregateExpressions(APlan, ((BinaryExpression)AExpression).LeftExpression);
				GatherAggregateExpressions(APlan, ((BinaryExpression)AExpression).RightExpression);
			}
			else if (AExpression is BetweenExpression)
			{
				BetweenExpression LBetweenExpression = (BetweenExpression)AExpression;
				GatherAggregateExpressions(APlan, LBetweenExpression.Expression);
				GatherAggregateExpressions(APlan, LBetweenExpression.LowerExpression);
				GatherAggregateExpressions(APlan, LBetweenExpression.UpperExpression);
			}
			else if (AExpression is CaseExpression)
			{
				CaseExpression LCaseExpression = (CaseExpression)AExpression;
				if (LCaseExpression.Expression != null)
					GatherAggregateExpressions(APlan, LCaseExpression.Expression);
				foreach (CaseItemExpression LCaseItemExpression in LCaseExpression.CaseItems)
				{
					GatherAggregateExpressions(APlan, LCaseItemExpression.WhenExpression);
					GatherAggregateExpressions(APlan, LCaseItemExpression.ThenExpression);
				}
				if (LCaseExpression.ElseExpression != null)
					GatherAggregateExpressions(APlan, ((CaseElseExpression)LCaseExpression.ElseExpression).Expression);
			}
			else if (AExpression is CallExpression)
			{
				foreach (Expression LExpression in ((CallExpression)AExpression).Expressions)
					GatherAggregateExpressions(APlan, LExpression);
			}
			else if (AExpression is IndexerExpression)
			{
				IndexerExpression LIndexerExpression = (IndexerExpression)AExpression;
				GatherAggregateExpressions(APlan, LIndexerExpression.Expression);
				GatherAggregateExpressions(APlan, LIndexerExpression.Indexer);
			}
			else if (AExpression is QualifierExpression)
			{
				QualifierExpression LQualifierExpression = (QualifierExpression)AExpression;
				GatherAggregateExpressions(APlan, LQualifierExpression.LeftExpression);
				GatherAggregateExpressions(APlan, LQualifierExpression.RightExpression);
			}
		}
		
		protected Expression CollapseQualifierExpressions(Expression AExpression)
		{
			if (AExpression is UnaryExpression)
			{
				UnaryExpression LUnaryExpression = (UnaryExpression)AExpression;
				LUnaryExpression.Expression = CollapseQualifierExpressions(LUnaryExpression.Expression);
				return LUnaryExpression;
			}
			else if (AExpression is BinaryExpression)
			{
				BinaryExpression LBinaryExpression = (BinaryExpression)AExpression;
				LBinaryExpression.LeftExpression = CollapseQualifierExpressions(LBinaryExpression.LeftExpression);
				LBinaryExpression.RightExpression = CollapseQualifierExpressions(LBinaryExpression.RightExpression);
				return LBinaryExpression;
			}
			else if (AExpression is BetweenExpression)
			{
				BetweenExpression LBetweenExpression = (BetweenExpression)AExpression;
				LBetweenExpression.Expression = CollapseQualifierExpressions(LBetweenExpression.Expression);
				LBetweenExpression.LowerExpression = CollapseQualifierExpressions(LBetweenExpression.LowerExpression);
				LBetweenExpression.UpperExpression = CollapseQualifierExpressions(LBetweenExpression.UpperExpression);
				return LBetweenExpression;
			}
			else if (AExpression is CaseExpression)
			{
				CaseExpression LCaseExpression = (CaseExpression)AExpression;
				if (LCaseExpression.Expression != null)
					LCaseExpression.Expression = CollapseQualifierExpressions(LCaseExpression.Expression);
				foreach (CaseItemExpression LCaseItemExpression in LCaseExpression.CaseItems)
				{
					LCaseItemExpression.WhenExpression = CollapseQualifierExpressions(LCaseItemExpression.WhenExpression);
					LCaseItemExpression.ThenExpression = CollapseQualifierExpressions(LCaseItemExpression.ThenExpression);
				}
				LCaseExpression.ElseExpression = (CaseElseExpression)CollapseQualifierExpressions(LCaseExpression.ElseExpression);
				return LCaseExpression;
			}
			else if (AExpression is CaseElseExpression)
			{
				CaseElseExpression LCaseElseExpression = (CaseElseExpression)AExpression;
				LCaseElseExpression.Expression = CollapseQualifierExpressions(LCaseElseExpression.Expression);
				return LCaseElseExpression;
			}
			else if (AExpression is AggregateCallExpression)
			{
				AggregateCallExpression LAggregateCallExpression = (AggregateCallExpression)AExpression;
				for (int LIndex = 0; LIndex < LAggregateCallExpression.Expressions.Count; LIndex++)
					LAggregateCallExpression.Expressions[LIndex] = CollapseQualifierExpressions(LAggregateCallExpression.Expressions[LIndex]);
				return LAggregateCallExpression;
			}
			else if (AExpression is CallExpression)
			{
				CallExpression LCallExpression = (CallExpression)AExpression;
				for (int LIndex = 0; LIndex < LCallExpression.Expressions.Count; LIndex++)
					LCallExpression.Expressions[LIndex] = CollapseQualifierExpressions(LCallExpression.Expressions[LIndex]);
				return LCallExpression;
			}
			else if (AExpression is IndexerExpression)
			{
				IndexerExpression LIndexerExpression = (IndexerExpression)AExpression;
				LIndexerExpression.Expression = CollapseQualifierExpressions(LIndexerExpression.Expression);
				LIndexerExpression.Indexer = CollapseQualifierExpressions(LIndexerExpression.Indexer);
				return LIndexerExpression;
			}
			else if (AExpression is QualifierExpression)
			{
				QualifierExpression LQualifierExpression = (QualifierExpression)AExpression;
				LQualifierExpression.LeftExpression = CollapseQualifierExpressions(LQualifierExpression.LeftExpression);
				LQualifierExpression.RightExpression = CollapseQualifierExpressions(LQualifierExpression.RightExpression);
				if (LQualifierExpression.LeftExpression is IdentifierExpression)
				{
					IdentifierExpression LLeftExpression = (IdentifierExpression)LQualifierExpression.LeftExpression;
					if (LQualifierExpression.RightExpression is IdentifierExpression)
					{
						LLeftExpression.Identifier =
							Schema.Object.Qualify(((IdentifierExpression)LQualifierExpression.RightExpression).Identifier, LLeftExpression.Identifier);
						return LLeftExpression;
					}
					else if (LQualifierExpression.RightExpression is CallExpression)
					{
						CallExpression LCallExpression = (CallExpression)LQualifierExpression.RightExpression;
						LCallExpression.Identifier = Schema.Object.Qualify(LCallExpression.Identifier, LLeftExpression.Identifier);
						return LCallExpression;
					}
				}
				return LQualifierExpression;
			}
			else
				return AExpression;
		}
		
		protected static bool ExpressionsEqual(Expression ALeftExpression, Expression ARightExpression)
		{
			if (ALeftExpression is UnaryExpression)
				return (ARightExpression is UnaryExpression) && UnaryExpressionsEqual((UnaryExpression)ALeftExpression, (UnaryExpression)ARightExpression);
			else if (ALeftExpression is BinaryExpression)
				return (ARightExpression is BinaryExpression) && BinaryExpressionsEqual((BinaryExpression)ALeftExpression, (BinaryExpression)ARightExpression);
			else if (ALeftExpression is BetweenExpression)
				return (ARightExpression is BetweenExpression) && BetweenExpressionsEqual((BetweenExpression)ALeftExpression, (BetweenExpression)ARightExpression);
			else if (ALeftExpression is CaseExpression)
				return (ARightExpression is CaseExpression) && CaseExpressionsEqual((CaseExpression)ALeftExpression, (CaseExpression)ARightExpression);
			else if (ALeftExpression is AggregateCallExpression)
				return (ARightExpression is AggregateCallExpression) && AggregateCallExpressionsEqual((AggregateCallExpression)ALeftExpression, (AggregateCallExpression)ARightExpression);
			else if (ALeftExpression is CallExpression)
				return (ARightExpression is CallExpression) && CallExpressionsEqual((CallExpression)ALeftExpression, (CallExpression)ARightExpression);
			else if (ALeftExpression is IndexerExpression)
				return (ARightExpression is IndexerExpression) && IndexerExpressionsEqual((IndexerExpression)ALeftExpression, (IndexerExpression)ARightExpression);
			else if (ALeftExpression is QualifierExpression)
				return (ARightExpression is QualifierExpression) && QualifierExpressionsEqual((QualifierExpression)ALeftExpression, (QualifierExpression)ARightExpression);
			else if (ALeftExpression is IdentifierExpression)
				return (ARightExpression is IdentifierExpression) && IdentifierExpressionsEqual((IdentifierExpression)ALeftExpression, (IdentifierExpression)ARightExpression);
			else if (ALeftExpression is ValueExpression)
				return (ARightExpression is ValueExpression) && ValueExpressionsEqual((ValueExpression)ALeftExpression, (ValueExpression)ARightExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, ALeftExpression.GetType().Name);
		}
		
		protected static bool UnaryExpressionsEqual(UnaryExpression ALeftExpression, UnaryExpression ARightExpression)
		{
			return (String.Compare(ALeftExpression.Instruction, ARightExpression.Instruction) == 0) && ExpressionsEqual(ALeftExpression.Expression, ARightExpression.Expression);
		}
		
		protected static bool BinaryExpressionsEqual(BinaryExpression ALeftExpression, BinaryExpression ARightExpression)
		{
			return 
				(String.Compare(ALeftExpression.Instruction, ARightExpression.Instruction) == 0) && 
				ExpressionsEqual(ALeftExpression.LeftExpression, ARightExpression.LeftExpression) &&
				ExpressionsEqual(ALeftExpression.RightExpression, ARightExpression.RightExpression);
		}
		
		protected static bool BetweenExpressionsEqual(BetweenExpression ALeftExpression, BetweenExpression ARightExpression)
		{
			return 
				ExpressionsEqual(ALeftExpression.Expression, ARightExpression.Expression) &&
				ExpressionsEqual(ALeftExpression.LowerExpression, ARightExpression.LowerExpression) &&
				ExpressionsEqual(ALeftExpression.UpperExpression, ARightExpression.UpperExpression);
		}
		
		protected static bool CaseExpressionsEqual(CaseExpression ALeftExpression, CaseExpression ARightExpression)
		{
			bool LResult = 
				(
					(
						(ALeftExpression.Expression == null) && 
						(ARightExpression.Expression == null)
					) ||
					(
						(ALeftExpression.Expression != null) && 
						(ARightExpression.Expression != null) && 
						ExpressionsEqual(ALeftExpression.Expression, ARightExpression.Expression)
					)
				);
			if (LResult)
			{
				for (int LIndex = 0; LIndex < ALeftExpression.CaseItems.Count; LIndex++)
				{
					LResult = 
						LResult && 
						(LIndex < ARightExpression.CaseItems.Count) && 
						CaseItemExpressionsEqual(ALeftExpression.CaseItems[LIndex], ARightExpression.CaseItems[LIndex]);
					if (!LResult)
						break;
				}
			}
			
			LResult =
				LResult &&
				CaseElseExpressionsEqual
				(
					(CaseElseExpression)ALeftExpression.ElseExpression, 
					(CaseElseExpression)ARightExpression.ElseExpression
				);
			
			return LResult;
		}
		
		protected static bool CaseItemExpressionsEqual(CaseItemExpression ALeftExpression, CaseItemExpression ARightExpression)
		{
			return 
				ExpressionsEqual(ALeftExpression.WhenExpression, ARightExpression.WhenExpression) &&
				ExpressionsEqual(ALeftExpression.ThenExpression, ARightExpression.ThenExpression);
		}
		
		protected static bool CaseElseExpressionsEqual(CaseElseExpression ALeftExpression, CaseElseExpression ARightExpression)
		{
			return ExpressionsEqual(ALeftExpression.Expression, ARightExpression.Expression);
		}
		
		protected static bool AggregateCallExpressionsEqual(AggregateCallExpression ALeftExpression, AggregateCallExpression ARightExpression)
		{
			return 
				(String.Compare(ALeftExpression.Identifier, ARightExpression.Identifier) == 0) && 
				(ALeftExpression.IsDistinct == ARightExpression.IsDistinct) &&
				(ALeftExpression.IsRowLevel == ARightExpression.IsRowLevel) &&
				(ALeftExpression.IsRowLevel || ExpressionsEqual(ALeftExpression.Expressions[0], ARightExpression.Expressions[0]));
		}
		
		protected static bool CallExpressionsEqual(CallExpression ALeftExpression, CallExpression ARightExpression)
		{
			bool LResult = String.Compare(ALeftExpression.Identifier, ARightExpression.Identifier) == 0;
			for (int LIndex = 0; LIndex < ALeftExpression.Expressions.Count; LIndex++)
			{
				LResult =
					LResult &&
					(LIndex < ARightExpression.Expressions.Count) &&
					ExpressionsEqual(ALeftExpression.Expressions[LIndex], ARightExpression.Expressions[LIndex]);
				if (!LResult)
					break;
			}
			return LResult;
		}
		
		protected static bool IndexerExpressionsEqual(IndexerExpression ALeftExpression, IndexerExpression ARightExpression)
		{
			return
				ExpressionsEqual(ALeftExpression.Expression, ARightExpression.Expression) &&
				ExpressionsEqual(ALeftExpression.Indexer, ARightExpression.Indexer);
		}
		
		protected static bool QualifierExpressionsEqual(QualifierExpression ALeftExpression, QualifierExpression ARightExpression)
		{
			return
				ExpressionsEqual(ALeftExpression.LeftExpression, ARightExpression.LeftExpression) &&
				ExpressionsEqual(ALeftExpression.RightExpression, ARightExpression.RightExpression);
		}
		
		protected static bool IdentifierExpressionsEqual(IdentifierExpression ALeftExpression, IdentifierExpression ARightExpression)
		{
			return String.Compare(ALeftExpression.Identifier, ARightExpression.Identifier) == 0;
		}
		
		protected static bool ValueExpressionsEqual(ValueExpression ALeftExpression, ValueExpression ARightExpression)
		{
			return 
				(
					(ALeftExpression.Value == null) && 
					(ARightExpression.Value == null)
				) ||
				(
					(ALeftExpression.Value != null) && 
					(ALeftExpression.Value.Equals(ARightExpression.Value))
				);
		}
		
		protected Expression CompileSelectExpression(Plan APlan, SelectExpression AExpression)
		{
			APlan.PushContext();
			Expression LExpression = CompileFromClause(APlan, (AlgebraicFromClause)AExpression.FromClause);
			if (AExpression.WhereClause != null)
			{
				AExpression.WhereClause.Expression = CollapseQualifierExpressions(AExpression.WhereClause.Expression);
				D4.RestrictExpression LRestrictExpression = new D4.RestrictExpression();
				LRestrictExpression.Expression = LExpression;
				LRestrictExpression.Condition = CompileExpression(APlan, AExpression.WhereClause.Expression);
				LExpression = LRestrictExpression;
			}
			
			// gather all expressions within aggregate invocations and the group and having clauses and name them if necessary, recursively
			// gather all aggregate invocation expressions within the select list and the having clause and name them if necessary, recursively
			foreach (ColumnExpression LColumn in AExpression.SelectClause.Columns)
			{
				LColumn.Expression = CollapseQualifierExpressions(LColumn.Expression);
				GatherPreAggregateExpressions(APlan, LColumn.Expression);
				GatherAggregateExpressions(APlan, LColumn.Expression);
			}
				
			if (AExpression.GroupClause != null)
				for (int LIndex = 0; LIndex < AExpression.GroupClause.Columns.Count; LIndex++)
				{
					AExpression.GroupClause.Columns[LIndex] = CollapseQualifierExpressions(AExpression.GroupClause.Columns[LIndex]);
					GatherPreAggregateExpressions(APlan, AExpression.GroupClause.Columns[LIndex]);
				}
					
			if (AExpression.HavingClause != null)
			{
				AExpression.HavingClause.Expression = CollapseQualifierExpressions(AExpression.HavingClause.Expression);
				GatherPreAggregateExpressions(APlan, AExpression.HavingClause.Expression);
				GatherAggregateExpressions(APlan, AExpression.HavingClause.Expression);
			}
			
			// add an extend expression with the pre aggregation extend columns, if necessary
			if (APlan.Context.PreAggregateExpressions.Count > 0)
			{
				D4.ExtendExpression LExtendExpression = new D4.ExtendExpression();
				LExtendExpression.Expression = LExpression;
				foreach (NamedExpression LNamedExpression in APlan.Context.PreAggregateExpressions)
					LExtendExpression.Expressions.Add(new D4.NamedColumnExpression(CompileExpression(APlan, LNamedExpression.Expression), LNamedExpression.Name));
				LExpression = LExtendExpression;
			}
			
			// add a group expression with the prepared group by columns and the given aggregate invocations
			if ((AExpression.GroupClause != null) || (APlan.Context.AggregateExpressions.Count > 0))
			{
				if (APlan.Context.AggregateExpressions.Count > 0)
				{
					D4.AggregateExpression LAggregateExpression = new D4.AggregateExpression();
					LAggregateExpression.Expression = LExpression;

					if (AExpression.GroupClause != null)
					{
						foreach (Expression LGroupExpression in AExpression.GroupClause.Columns)
						{
							if (LGroupExpression is IdentifierExpression)
								LAggregateExpression.ByColumns.Add(new D4.ColumnExpression(((IdentifierExpression)LGroupExpression).Identifier));
							else
								LAggregateExpression.ByColumns.Add(new D4.ColumnExpression(APlan.Context.PreAggregateExpressions[LGroupExpression].Name));
						}
					}

					foreach (NamedExpression LNamedExpression in APlan.Context.AggregateExpressions)
					{
						D4.AggregateColumnExpression LAggregateColumnExpression = new D4.AggregateColumnExpression();
						AggregateCallExpression LAggregateCallExpression = (AggregateCallExpression)LNamedExpression.Expression;
						LAggregateColumnExpression.AggregateOperator = LAggregateCallExpression.Identifier;
						LAggregateColumnExpression.Distinct = LAggregateCallExpression.IsDistinct;
						if (LAggregateCallExpression.Expressions.Count > 0)
							if (LAggregateCallExpression.Expressions[0] is IdentifierExpression)
								LAggregateColumnExpression.Columns.Add(new D4.ColumnExpression(((IdentifierExpression)LAggregateCallExpression.Expressions[0]).Identifier));
							else
								LAggregateColumnExpression.Columns.Add(new D4.ColumnExpression(APlan.Context.PreAggregateExpressions[LAggregateCallExpression.Expressions[0]].Name));
						LAggregateColumnExpression.ColumnAlias = LNamedExpression.Name;
						LAggregateExpression.ComputeColumns.Add(LAggregateColumnExpression);
					}

					LExpression = LAggregateExpression;
				}
				else
				{
					D4.ProjectExpression LProjectExpression = new D4.ProjectExpression();
					LProjectExpression.Expression = LExpression;
					foreach (Expression LGroupExpression in AExpression.GroupClause.Columns)
					{
						if (LGroupExpression is IdentifierExpression)
							LProjectExpression.Columns.Add(new D4.ColumnExpression(((IdentifierExpression)LGroupExpression).Identifier));
						else
							LProjectExpression.Columns.Add(new D4.ColumnExpression(APlan.Context.PreAggregateExpressions[LGroupExpression].Name));
					}
					LExpression = LProjectExpression;
				}
			}
			
			if (AExpression.HavingClause != null)
			{
				D4.RestrictExpression LRestrictExpression = new D4.RestrictExpression();
				LRestrictExpression.Expression = LExpression;
				LRestrictExpression.Condition = CompileExpression(APlan, AExpression.HavingClause.Expression);
				LExpression = LRestrictExpression;
			}
			
			// gather all extend expressions within the select columns and name them, if necessary
			foreach (ColumnExpression LColumnExpression in AExpression.SelectClause.Columns)
				if (LColumnExpression.Expression is IdentifierExpression)
				{
					if (LColumnExpression.ColumnAlias == String.Empty)
						LColumnExpression.ColumnAlias = ((IdentifierExpression)LColumnExpression.Expression).Identifier;
				}
				else if (LColumnExpression.Expression is AggregateCallExpression)
				{
					if (LColumnExpression.ColumnAlias == String.Empty)
						LColumnExpression.ColumnAlias = APlan.Context.GetExpressionName(LColumnExpression.Expression);
				}
				else 
				{
					if (LColumnExpression.ColumnAlias == String.Empty)
						LColumnExpression.ColumnAlias = APlan.Context.GetExpressionName(LColumnExpression.Expression);
					if (APlan.Context.ExtendExpressions.IndexOf(LColumnExpression.Expression) < 0)
						APlan.Context.ExtendExpressions.Add(new NamedExpression(LColumnExpression.ColumnAlias, LColumnExpression.Expression));
				}
		
			// add an extend expression with the post aggregation extend columns, if necessary
			if (APlan.Context.ExtendExpressions.Count > 0)
			{
				D4.ExtendExpression LExtendExpression = new D4.ExtendExpression();
				LExtendExpression.Expression = LExpression;
				foreach (NamedExpression LNamedExpression in APlan.Context.ExtendExpressions)
					LExtendExpression.Expressions.Add(new D4.NamedColumnExpression(CompileExpression(APlan, LNamedExpression.Expression), LNamedExpression.Name));
				LExpression = LExtendExpression;
			}
			
			// gather all the rename expressions within the select columns
			foreach (ColumnExpression LColumnExpression in AExpression.SelectClause.Columns)
				if (LColumnExpression.ColumnAlias == String.Empty)
				{
					if (LColumnExpression.Expression is IdentifierExpression)
						LColumnExpression.ColumnAlias = ((IdentifierExpression)LColumnExpression.Expression).Identifier;
				}
				else if ((LColumnExpression.Expression is IdentifierExpression) && (String.Compare(((IdentifierExpression)LColumnExpression.Expression).Identifier, LColumnExpression.ColumnAlias) != 0))
				{
					APlan.Context.RenameExpressions.Add(new NamedExpression(LColumnExpression.ColumnAlias, LColumnExpression.Expression));
				}
				
			// add a rename expression if necessary
			if (APlan.Context.RenameExpressions.Count > 0)
			{
				D4.RenameExpression LRenameExpression = new D4.RenameExpression();
				LRenameExpression.Expression = LExpression;
				foreach (NamedExpression LNamedExpression in APlan.Context.RenameExpressions)
					LRenameExpression.Expressions.Add(new D4.RenameColumnExpression(((IdentifierExpression)LNamedExpression.Expression).Identifier, LNamedExpression.Name));
				LExpression = LRenameExpression;
			}
			
			// project over the final columns, if necessary
			if (AExpression.SelectClause.Columns.Count > 0)
			{
				D4.ProjectExpression LProjectExpression = new D4.ProjectExpression();
				LProjectExpression.Expression = LExpression;
				foreach (ColumnExpression LColumnExpression in AExpression.SelectClause.Columns)
					LProjectExpression.Columns.Add(new D4.ColumnExpression(LColumnExpression.ColumnAlias));
				LExpression = LProjectExpression;
			}

			APlan.PopContext();
			return LExpression;
		}
		
		protected Expression CompileFromClause(Plan APlan, AlgebraicFromClause AFromClause)
		{
			Expression LExpression = CompileTableSpecifier(APlan, AFromClause.TableSpecifier);
			foreach (JoinClause LJoin in AFromClause.Joins)
			{
				Expression LRightExpression = CompileFromClause(APlan, LJoin.FromClause);
				switch (LJoin.JoinType)
				{
					case JoinType.Cross: 
						D4.ProductExpression LProductExpression = new D4.ProductExpression();
						LProductExpression.LeftExpression = LExpression;
						LProductExpression.RightExpression = LRightExpression;
						LExpression = LProductExpression;
					break;
					
					case JoinType.Inner:
						D4.InnerJoinExpression LInnerJoinExpression = new D4.InnerJoinExpression();
						LInnerJoinExpression.LeftExpression = LExpression;
						LInnerJoinExpression.RightExpression = LRightExpression;
						LInnerJoinExpression.Condition = CompileExpression(APlan, LJoin.JoinExpression);
						LExpression = LInnerJoinExpression;
					break;
					
					case JoinType.Left:
						D4.LeftOuterJoinExpression LLeftJoinExpression = new D4.LeftOuterJoinExpression();
						LLeftJoinExpression.LeftExpression = LExpression;
						LLeftJoinExpression.RightExpression = LRightExpression;
						LLeftJoinExpression.Condition = CompileExpression(APlan, LJoin.JoinExpression);
						LExpression = LLeftJoinExpression;
					break;
					
					case JoinType.Right:
						D4.RightOuterJoinExpression LRightJoinExpression = new D4.RightOuterJoinExpression();
						LRightJoinExpression.LeftExpression = LExpression;
						LRightJoinExpression.RightExpression = LRightExpression;
						LRightJoinExpression.Condition = CompileExpression(APlan, LJoin.JoinExpression);
						LExpression = LRightJoinExpression;
					break;
					
					default: throw new LanguageException(LanguageException.Codes.UnknownJoinType, LJoin.JoinType.ToString());
				}
			}
			
			return LExpression;
		}
		
		protected Expression CompileTableSpecifier(Plan APlan, TableSpecifier ATableSpecifier)
		{
			string LTableAlias;
			Expression LExpression;
			if (ATableSpecifier.TableExpression is TableExpression)
			{
				LExpression = new IdentifierExpression(((TableExpression)ATableSpecifier.TableExpression).TableName);
				if (ATableSpecifier.TableAlias != String.Empty)
					LTableAlias = ATableSpecifier.TableAlias;
				else
					LTableAlias = ((TableExpression)ATableSpecifier.TableExpression).TableName;
			}
			else if (ATableSpecifier.TableExpression is QueryExpression)
			{
				LExpression = CompileQueryExpression(APlan, (QueryExpression)ATableSpecifier.TableExpression);
				if (ATableSpecifier.TableAlias != String.Empty)
					LTableAlias = ATableSpecifier.TableAlias;
				else
					throw new LanguageException(LanguageException.Codes.TableAliasRequired);
			}
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, ATableSpecifier.TableExpression.GetType().Name);

			return new D4.RenameAllExpression(LExpression, LTableAlias);
		}
		
		protected Expression CompileExpression(Plan APlan, Expression AExpression)
		{
			if (AExpression is UnaryExpression)
				return CompileUnaryExpression(APlan, (UnaryExpression)AExpression);
			else if (AExpression is BinaryExpression)
				return CompileBinaryExpression(APlan, (BinaryExpression)AExpression);
			else if (AExpression is BetweenExpression)
				return CompileBetweenExpression(APlan, (BetweenExpression)AExpression);
			else if (AExpression is CaseExpression)
				return CompileCaseExpression(APlan, (CaseExpression)AExpression);
			else if (AExpression is AggregateCallExpression)
				return CompileAggregateCallExpression(APlan, (AggregateCallExpression)AExpression);
			else if (AExpression is CallExpression)
				return CompileCallExpression(APlan, (CallExpression)AExpression);
			else if (AExpression is IndexerExpression)
				return CompileIndexerExpression(APlan, (IndexerExpression)AExpression);
			else if (AExpression is QualifierExpression)
				return CompileQualifierExpression(APlan, (QualifierExpression)AExpression);
			else if (AExpression is IdentifierExpression)
				return AExpression;
			else if (AExpression is ValueExpression)
				return AExpression;
			else if (AExpression is QueryExpression)
				return CompileQueryExpression(APlan, (QueryExpression)AExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, AExpression.GetType().Name);
		}
		
		protected Expression CompileUnaryExpression(Plan APlan, UnaryExpression AExpression)
		{
			AExpression.Expression = CompileExpression(APlan, AExpression.Expression);
			return AExpression;
		}
		
		protected Expression CompileBinaryExpression(Plan APlan, BinaryExpression AExpression)
		{
			AExpression.LeftExpression = CompileExpression(APlan, AExpression.LeftExpression);
			AExpression.RightExpression = CompileExpression(APlan, AExpression.RightExpression);
			return AExpression;
		}
		
		protected Expression CompileBetweenExpression(Plan APlan, BetweenExpression AExpression)
		{
			AExpression.Expression = CompileExpression(APlan, AExpression.Expression);
			AExpression.LowerExpression = CompileExpression(APlan, AExpression.LowerExpression);
			AExpression.UpperExpression = CompileExpression(APlan, AExpression.UpperExpression);
			return AExpression;
		}
		
		protected Expression CompileCaseExpression(Plan APlan, CaseExpression AExpression)
		{
			if (AExpression.Expression != null)
				AExpression.Expression = CompileExpression(APlan, AExpression.Expression);
			foreach (CaseItemExpression LCaseItemExpression in AExpression.CaseItems)
			{
				LCaseItemExpression.WhenExpression = CompileExpression(APlan, LCaseItemExpression.WhenExpression);
				LCaseItemExpression.ThenExpression = CompileExpression(APlan, LCaseItemExpression.ThenExpression);
			}
			((CaseElseExpression)AExpression.ElseExpression).Expression = CompileExpression(APlan, ((CaseElseExpression)AExpression.ElseExpression).Expression);
			return AExpression;
		}
		
		protected Expression CompileAggregateCallExpression(Plan APlan, AggregateCallExpression AExpression)
		{
			return new IdentifierExpression(APlan.Context.AggregateExpressions[AExpression].Name);
		}
		
		protected Expression CompileCallExpression(Plan APlan, CallExpression AExpression)
		{
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
				AExpression.Expressions[LIndex] = CompileExpression(APlan, AExpression.Expressions[LIndex]);
			return AExpression;
		}
		
		protected Expression CompileIndexerExpression(Plan APlan, IndexerExpression AExpression)
		{
			AExpression.Expression = CompileExpression(APlan, AExpression.Expression);
			AExpression.Indexer = CompileExpression(APlan, AExpression.Indexer);
			return AExpression;
		}
		
		protected Expression CompileQualifierExpression(Plan APlan, QualifierExpression AExpression)
		{
			AExpression.LeftExpression = CompileExpression(APlan, AExpression.LeftExpression);
			AExpression.RightExpression = CompileExpression(APlan, AExpression.RightExpression);
			return AExpression;
		}
		
		protected Statement CompileInsertStatement(Plan APlan, InsertStatement AStatement)
		{
			D4.InsertStatement LStatement = new D4.InsertStatement();
			LStatement.Target = new IdentifierExpression(AStatement.InsertClause.TableExpression.TableName);
			if (AStatement.Values is QueryExpression)
				LStatement.SourceExpression = CompileQueryExpression(APlan, (QueryExpression)AStatement.Values);
			else if (AStatement.Values is ValuesExpression)
			{
				D4.TableSelectorExpression LTableExpression = new D4.TableSelectorExpression();
				D4.RowSelectorExpression LRowExpression = new D4.RowSelectorExpression();
				ValuesExpression LValuesExpression = (ValuesExpression)AStatement.Values;
				for (int LIndex = 0; LIndex < LValuesExpression.Expressions.Count; LIndex++)
					LRowExpression.Expressions.Add(new D4.NamedColumnExpression(CompileExpression(APlan, LValuesExpression.Expressions[LIndex]), AStatement.InsertClause.Columns[LIndex].FieldName));
				LTableExpression.Expressions.Add(LRowExpression);
				LTableExpression.Keys.Add(new D4.KeyDefinition());
				LStatement.SourceExpression = LTableExpression;
			}
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, AStatement.Values.GetType().Name);
			return LStatement;
		}
		
		protected Statement CompileUpdateStatement(Plan APlan, UpdateStatement AStatement)
		{
			D4.UpdateStatement LStatement = new D4.UpdateStatement();
			LStatement.Target = new IdentifierExpression(AStatement.UpdateClause.TableExpression.TableName);
			foreach (UpdateFieldExpression LExpression in AStatement.UpdateClause.Columns)
				LStatement.Columns.Add(new D4.UpdateColumnExpression(new IdentifierExpression(LExpression.FieldName), CompileExpression(APlan, LExpression.Expression)));
			if (AStatement.WhereClause != null)
				LStatement.Condition = CompileExpression(APlan, AStatement.WhereClause.Expression);
			return LStatement;
		}
		
		protected Statement CompileDeleteStatement(Plan APlan, DeleteStatement AStatement)
		{
			D4.DeleteStatement LStatement = new D4.DeleteStatement();
			LStatement.Target = new IdentifierExpression(AStatement.DeleteClause.TableExpression.TableName);
			if (AStatement.WhereClause != null)
				LStatement.Target = new D4.RestrictExpression(LStatement.Target, CompileExpression(APlan, AStatement.WhereClause.Expression));
			return LStatement;
		}
	}
	
	// operator RealSQLToD4(ARealSQLStatement : string) : string;
	public class RealSQLToD4Node : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return 
				new D4.D4TextEmitter().Emit
				(	
					new Compiler().Compile
					(
						new Parser().ParseStatement
						(
							(string)AArguments[0]
						)
					)
				);
		}
	}
}

