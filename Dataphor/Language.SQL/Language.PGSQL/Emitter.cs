/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.PGSQL
{
	using System;
	using System.Text;
	
	using Alphora.Dataphor.DAE.Language;
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class TSQLTextEmitter : SQL.SQLTextEmitter
	{
		protected override void EmitSelectExpression(SQL.SelectExpression expression)
		{
			AppendFormat("{0} ", Alphora.Dataphor.DAE.Language.SQL.Keywords.Select);
			IncreaseIndent();
			EmitSelectClause(expression.SelectClause);
			EmitFromClause(expression.FromClause);
			EmitWhereClause(expression.WhereClause);
			EmitGroupClause(expression.GroupClause);
			EmitHavingClause(expression.HavingClause);

			// Add the "for" clause
			var pgSelect = expression as SelectExpression;
			if (pgSelect != null)
				EmitForClause(pgSelect.ForSpecifier);

			DecreaseIndent();
		}

		protected override void EmitAlgebraicFromClause(Alphora.Dataphor.DAE.Language.SQL.AlgebraicFromClause clause)
		{
			// The "from" clause is optional for PG, omit the dummy from
			if (clause.TableSpecifier.TableAlias != "dummy1")
				base.EmitAlgebraicFromClause(clause);
		}

		protected virtual void EmitForClause(ForSpecifier forSpecifier)
		{
			if (forSpecifier != ForSpecifier.None)
			{
				NewLine();
				Indent();
				Append("for ");
				if (forSpecifier == ForSpecifier.Update)
					Append("update ");
				else
					Append("share ");
			}
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

		protected override void EmitAlterColumnDefinition(SQL.AlterColumnDefinition column)
		{
			if (column.DomainName != null)
			{
				AppendFormat("{0} {1} ", SQL.Keywords.Alter, SQL.Keywords.Column);
				EmitIdentifier(column.ColumnName);
				AppendFormat(" type {0}", column.DomainName);
				if (column.AlterNullable)
					Append(", ");
			}
			if (column.AlterNullable)
			{
				AppendFormat("{0} {1} ", SQL.Keywords.Alter, SQL.Keywords.Column);
				EmitIdentifier(column.ColumnName);
				if (column.IsNullable)
					AppendFormat(" set {0}", SQL.Keywords.Null);
				else
					AppendFormat(" set {0} {1}", SQL.Keywords.Not, SQL.Keywords.Null);
			}
		}
	}
}