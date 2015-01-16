/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.TSQL
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Alphora.Dataphor.DAE.Language;
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class TSQLTextEmitter : SQL.SQLTextEmitter
	{
		protected Stack<TopClause> _topClauses = new Stack<TopClause>();
		protected override void EmitBinaryExpression(BinaryExpression expression)
		{
			if (expression.Instruction == "iNullValue")
			{
				Append("IsNull(");
				EmitExpression(expression.LeftExpression);
				Append(", ");
				EmitExpression(expression.RightExpression);
				Append(")");
			}
			else
				base.EmitBinaryExpression(expression);
		}

		protected override void EmitTableSpecifier(SQL.TableSpecifier tableSpecifier)
		{
			base.EmitTableSpecifier(tableSpecifier);
			if (tableSpecifier.TableExpression is TableExpression)
				AppendFormat(" {0}", ((TableExpression)tableSpecifier.TableExpression).OptimizerHints);
		}
		
		protected override void EmitSelectStatement(SQL.SelectStatement selectStatement)
		{
			var topStatement = selectStatement as SelectStatement;
			_topClauses.Push(topStatement != null ? topStatement.TopClause : null);
			try
			{
				base.EmitSelectStatement(selectStatement);
			}
			finally
			{
				_topClauses.Pop();
			}
			
			if ((selectStatement.Modifiers != null) && selectStatement.Modifiers.Contains("OptimizerHints"))
			{
				NewLine();
				Indent();
				Append(selectStatement.Modifiers["OptimizerHints"].Value);
			}
		}

		protected override void EmitSelectClause(SQL.SelectClause clause)
		{
			var topClause = _topClauses.Count > 0 ? _topClauses.Peek() : null;
			if (topClause != null)
			{
				AppendFormat("top({0})", topClause.IsPercent ? topClause.Quota.ToString() : ((int)topClause.Quota).ToString());
				if (topClause.IsPercent)
				{
					AppendFormat(" percent");
				}

				if (topClause.WithTies)
				{
					AppendFormat(" with ties");
				}
			}

			base.EmitSelectClause(clause);
		}
		
		protected override void EmitDropIndexStatement(SQL.DropIndexStatement statement)
		{
			if (statement is DropIndexStatement)
			{
				Indent();
				AppendFormat("{0} {1} ", SQL.Keywords.Drop, SQL.Keywords.Index);
				if (!String.IsNullOrEmpty(((DropIndexStatement)statement).TableSchema))
				{
					EmitIdentifier(((DropIndexStatement)statement).TableSchema);
					Append(SQL.Keywords.Qualifier);
				}
				EmitIdentifier(((DropIndexStatement)statement).TableName);
				Append(SQL.Keywords.Qualifier);
				EmitIdentifier(statement.IndexName);
			}
			else
				base.EmitDropIndexStatement(statement);
		}

		protected override string GetInstructionKeyword(string instruction)
		{
			if (instruction == "iConcatenation")
				return "+";
				
			else if (instruction == "iMod")
				return "%";

			else 
				return base.GetInstructionKeyword(instruction);
		}
	}
}