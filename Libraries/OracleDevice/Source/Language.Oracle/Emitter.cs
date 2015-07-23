/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.Oracle
{
	using System;
	using System.Text;
	using Alphora.Dataphor.DAE.Language;
	using Language.Oracle;
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class OracleTextEmitter : SQL.SQLTextEmitter
	{
		public OracleTextEmitter()
		{
			// this is the default for the Oracle device, but it can be ovveridden.
			UseStatementTerminator = false;
		}

		protected override void EmitExpression(Expression expression)
		{
			if (expression is OuterJoinFieldExpression)
			{
				EmitQualifiedFieldExpression((SQL.QualifiedFieldExpression)expression);
				Append("(+)");
			}
			else
				base.EmitExpression(expression);
		}
		
		protected override void EmitAlterTableStatement(SQL.AlterTableStatement statement)
		{
			Indent();
			AppendFormat("{0} {1} ", SQL.Keywords.Alter, SQL.Keywords.Table);
			EmitIdentifier(statement.TableName);
			
			for (int index = 0; index < statement.AddColumns.Count; index++)
			{
				AppendFormat(" {0} {1}", SQL.Keywords.Add, SQL.Keywords.BeginGroup);
				EmitColumnDefinition(statement.AddColumns[index]);
				Append(SQL.Keywords.EndGroup);
			}
			
			for (int index = 0; index < statement.AlterColumns.Count; index++)
			{
				SQL.AlterColumnDefinition definition = statement.AlterColumns[index];
				if (definition.AlterNullable)
				{
					AppendFormat(" {0} ", "modify");
					EmitIdentifier(definition.ColumnName);
					if (definition.IsNullable)
						AppendFormat(" {0}", SQL.Keywords.Null);
					else
						AppendFormat(" {0} {1}", SQL.Keywords.Not, SQL.Keywords.Null);
				}
			}
			
			for (int index = 0; index < statement.DropColumns.Count; index++)
			{
				AppendFormat(" {0} {1}", SQL.Keywords.Drop, SQL.Keywords.BeginGroup);
				EmitIdentifier(statement.DropColumns[index].ColumnName);
				Append(SQL.Keywords.EndGroup);
			}
		}

		protected override void EmitTableSpecifier(SQL.TableSpecifier tableSpecifier)
		{
			if (tableSpecifier.TableExpression is SQL.TableExpression)
				EmitTableExpression((SQL.TableExpression)tableSpecifier.TableExpression);
			else
				EmitSubQueryExpression(tableSpecifier.TableExpression);
				
			if (tableSpecifier.TableAlias != String.Empty)
			{
				Append(" ");
				EmitIdentifier(tableSpecifier.TableAlias);
			}
		}

		protected override void EmitSelectExpression(SQL.SelectExpression expression)
		{
			AppendFormat("{0} /*+ {1}*/ ", SQL.Keywords.Select, expression is SelectExpression ? ((SelectExpression)expression).OptimizerHints : "FIRST_ROWS(20)");
			IncreaseIndent();
			EmitSelectClause(expression.SelectClause);
			EmitFromClause(expression.FromClause);
			EmitWhereClause(expression.WhereClause);
			EmitGroupClause(expression.GroupClause);
			EmitHavingClause(expression.HavingClause);
			DecreaseIndent();
		}

		protected override void EmitCreateIndexStatement(SQL.CreateIndexStatement statement)
		{
			Indent();
			AppendFormat("{0} ", SQL.Keywords.Create);
			if (statement.IsUnique)
				AppendFormat("{0} ", SQL.Keywords.Unique);
			//if (AStatement.IsClustered)
			//	AppendFormat("{0} ", SQL.Keywords.Clustered);
			AppendFormat("{0} ", SQL.Keywords.Index);
			if (statement.IndexSchema != String.Empty)
			{
				EmitIdentifier(statement.IndexSchema);
				Append(SQL.Keywords.Qualifier);
			}
			EmitIdentifier(statement.IndexName);
			AppendFormat(" {0} ", SQL.Keywords.On);
			if (statement.TableSchema != String.Empty)
			{
				EmitIdentifier(statement.TableSchema);
				Append(SQL.Keywords.Qualifier);
			}
			EmitIdentifier(statement.TableName);
			Append(SQL.Keywords.BeginGroup);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(statement.Columns[index]);
			}
			Append(SQL.Keywords.EndGroup);
		}

		protected override void EmitAggregateCallExpression(SQL.AggregateCallExpression expression)
		{
			base.EmitAggregateCallExpression(expression);

			var ace = expression as OracleAggregateCallExpression;
			if (ace != null && ace.OrderClause != null)
			{
				AppendFormat(" {0} {1}", Keywords.Within, SQL.Keywords.Group);
				Append(SQL.Keywords.BeginGroup);
				EmitOrderClause(ace.OrderClause);
				Append(SQL.Keywords.EndGroup);
			}
		}
	}
}
