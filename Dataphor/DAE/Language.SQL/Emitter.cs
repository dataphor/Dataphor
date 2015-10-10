/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.SQL
{
	using System;
	using System.Text;
	
	using Alphora.Dataphor.DAE.Language;
	
	public class SQLTextEmitter : BasicTextEmitter, ICloneable
	{
		private bool _useStatementTerminator = true;
		public bool UseStatementTerminator
		{
			set { _useStatementTerminator = value; }
			get { return _useStatementTerminator; }
		}

		private bool _useQuotedIdentifiers = true;
		public bool UseQuotedIdentifiers
		{
			set { _useQuotedIdentifiers = value;}
			get { return _useQuotedIdentifiers;}
		}
		
		protected override void EmitExpression(Expression expression)
		{
			if (expression is QualifiedFieldExpression)
				EmitQualifiedFieldExpression((QualifiedFieldExpression)expression);
			else if (expression is TableExpression)
				EmitTableExpression((TableExpression)expression);
			else if (expression is AggregateCallExpression)
				EmitAggregateCallExpression((AggregateCallExpression)expression);
			else if (expression is UserExpression)
				EmitUserExpression((UserExpression)expression);
			else if (expression is QueryParameterExpression)
				EmitQueryParameterExpression((QueryParameterExpression)expression);
			else if ((expression is QueryExpression) || (expression is SelectExpression))
				EmitSubQueryExpression(expression);
			else if (expression is CastExpression)
				EmitCastExpression((CastExpression)expression);
			else if (expression is ListExpression)
				EmitListExpression((ListExpression)expression);
			else
				base.EmitExpression(expression);
		}
		
		protected virtual void EmitQueryParameterExpression(QueryParameterExpression expression)
		{
			AppendFormat("@{0}", expression.ParameterName);
		}

		protected virtual void EmitCastExpression(CastExpression expression)
		{
			AppendFormat("cast(");
			EmitExpression(expression.Expression);
			AppendFormat(" as {0})", expression.DomainName);
		}
		
		protected virtual void EmitListExpression(ListExpression expression)
		{
			AppendFormat("( ");
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitExpression(expression.Expressions[index]);
			}
			AppendFormat(" )");
		}
		
		protected override void EmitValueExpression(ValueExpression expression)
		{
			if (expression.Value == null)
				Append(Keywords.Null);
			else if (expression.Token == TokenType.Decimal)
			{
				decimal tempValue = (decimal)expression.Value;
				if (Decimal.Truncate(tempValue) == tempValue)
					Append(String.Format(System.Globalization.NumberFormatInfo.InvariantInfo, "{0:F0}.0", tempValue));
				else
					Append(String.Format(System.Globalization.NumberFormatInfo.InvariantInfo, "{0:G}", tempValue));
			}
			else
				base.EmitValueExpression(expression);
		}
		
		protected override void EmitUnaryExpression(UnaryExpression expression)
		{
			if (expression.Instruction == "iIsNull")
			{
				EmitExpression(expression.Expression);
				AppendFormat(" {0}", " is null");
			}
			else if (expression.Instruction == "iIsNotNull")
			{
				EmitExpression(expression.Expression);
				AppendFormat(" {0}", " is not null");
			}
			else
			{
				base.EmitUnaryExpression(expression);
			}
		}

		protected virtual void EmitTerminatedStatement(Statement statement)
		{
			if (statement is SelectStatement)
				EmitSelectStatement((SelectStatement)statement);
			else if (statement is InsertStatement)
				EmitInsertStatement((InsertStatement)statement);
			else if (statement is UpdateStatement)
				EmitUpdateStatement((UpdateStatement)statement);
			else if (statement is DeleteStatement)
				EmitDeleteStatement((DeleteStatement)statement);
			else if (statement is CreateTableStatement)
				EmitCreateTableStatement((CreateTableStatement)statement);
			else if (statement is AlterTableStatement)
				EmitAlterTableStatement((AlterTableStatement)statement);
			else if (statement is DropTableStatement)
				EmitDropTableStatement((DropTableStatement)statement);
			else if (statement is CreateIndexStatement)
				EmitCreateIndexStatement((CreateIndexStatement)statement);
			else if (statement is DropIndexStatement)
				EmitDropIndexStatement((DropIndexStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
			EmitStatementTerminator();
		}
		
		protected override void EmitStatement(Statement statement)
		{
			if (statement is Batch)
				EmitBatch((Batch)statement);
			else
				EmitTerminatedStatement(statement);
		}
		
		protected virtual void EmitBatch(Batch batch)
		{
			for (int index = 0; index < batch.Statements.Count; index++)
			{
				if (index > 0)
					NewLine();
				EmitStatement(batch.Statements[index]);
			}
		}

		protected virtual void EmitSelectStatement(SelectStatement selectStatement)
		{
			Indent();
			EmitQueryExpression(selectStatement.QueryExpression);
			if (selectStatement.OrderClause != null)
			{
				NewLine();
				Indent();
				EmitOrderClause(selectStatement.OrderClause);
			}
		}

		protected virtual void EmitOrderClause(OrderClause orderClause)
		{
			AppendFormat("{0} {1} ", Keywords.Order, Keywords.By);
			for (int index = 0; index < orderClause.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderFieldExpression(orderClause.Columns[index]);
			}
		}

		protected virtual void EmitOrderFieldExpression(OrderFieldExpression orderFieldExpression)
		{
			EmitQualifiedFieldExpression(orderFieldExpression);
			AppendFormat(" {0}", orderFieldExpression.Ascending ? Keywords.Asc : Keywords.Desc);
			if (orderFieldExpression.NullsFirst != null)
			{
				AppendFormat(" {0} {1}", Keywords.Nulls, (bool)orderFieldExpression.NullsFirst ? Keywords.First : Keywords.Last);
			}
		}
		
		protected virtual void EmitIdentifier(string identifier)
		{
			if (identifier == "*")
				Append(identifier);
			else
			{
				string appendString = "{0}";
				if (_useQuotedIdentifiers)
					appendString = "\"" + appendString + "\"";
				AppendFormat(appendString, identifier);
			}
		}
		
		protected virtual void EmitQualifiedFieldExpression(QualifiedFieldExpression expression)
		{
			if (expression.TableAlias != String.Empty)
			{
				EmitIdentifier(expression.TableAlias);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(expression.FieldName);
		}
		
		protected virtual void EmitQueryExpression(QueryExpression queryExpression)
		{
			EmitSelectExpression(queryExpression.SelectExpression);
			foreach (TableOperatorExpression tableOperatorExpression in queryExpression.TableOperators)
				EmitTableOperatorExpression(tableOperatorExpression);
		}
		
		protected virtual string GetTableOperatorKeyword(TableOperator tableOperator)
		{
			switch (tableOperator)
			{
				case TableOperator.Union: return Keywords.Union;
				case TableOperator.Intersect: return Keywords.Intersect;
				case TableOperator.Difference: return Keywords.Minus;
				default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, tableOperator.ToString());
			}
		}
		
		protected virtual void EmitTableOperatorExpression(TableOperatorExpression tableOperatorExpression)
		{
			NewLine();
			Indent();
			Append(GetTableOperatorKeyword(tableOperatorExpression.TableOperator));
			if (!tableOperatorExpression.Distinct)
				AppendFormat(" {0}", Keywords.All);
			
			NewLine();
			Indent();
			EmitSelectExpression(tableOperatorExpression.SelectExpression);
		}
		
		protected virtual void EmitSelectExpression(SelectExpression expression)
		{
			AppendFormat("{0} ", Keywords.Select);
			IncreaseIndent();
			EmitSelectClause(expression.SelectClause);
			EmitFromClause(expression.FromClause);
			EmitWhereClause(expression.WhereClause);
			EmitGroupClause(expression.GroupClause);
			EmitHavingClause(expression.HavingClause);
			DecreaseIndent();
		}
		
		protected virtual void EmitSelectClause(SelectClause clause)
		{
			NewLine();
			Indent();
			if (clause.Distinct)
				AppendFormat("{0} ", Keywords.Distinct);
			if (clause.Columns.Count == 0)
				Append(Keywords.Star);
			for (int index = 0; index < clause.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitColumnExpression(clause.Columns[index]);
			}
		}
		
		protected virtual void EmitColumnExpression(ColumnExpression expression)
		{
			EmitExpression(expression.Expression);
			if (expression.ColumnAlias != String.Empty)
			{
				AppendFormat(" {0} ", Keywords.As);
				EmitIdentifier(expression.ColumnAlias);
			}
		}
		
		protected virtual void EmitAggregateCallExpression(AggregateCallExpression expression)
		{
			AppendFormat("{0}{1}", expression.Identifier, Keywords.BeginGroup);
			if (expression.IsDistinct)
				AppendFormat("{0} ", Keywords.Distinct);
            for (int index = 0; index < expression.Expressions.Count; index++)
            {
                if (index > 0)
                    EmitListSeparator();
                EmitExpression(expression.Expressions[index]);
            }         
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitUserExpression(UserExpression expression)
		{
			string[] arguments = new string[expression.Expressions.Count];
			SQLTextEmitter emitter = Clone();
			for (int index = 0; index < arguments.Length; index++)
				arguments[index] = emitter.Emit(expression.Expressions[index]);
				
			AppendFormat(expression.TranslationString, arguments);
		}
		
		protected virtual void EmitFromClause(FromClause clause)
		{
			if (clause is AlgebraicFromClause)
				EmitAlgebraicFromClause((AlgebraicFromClause)clause);
			else if (clause is CalculusFromClause)
				EmitCalculusFromClause((CalculusFromClause)clause);
		}
		
		protected virtual void EmitAlgebraicFromClause(AlgebraicFromClause clause)
		{
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.From);
			EmitTableSpecifier(clause.TableSpecifier);
			IncreaseIndent();
			for (int index = 0; index < clause.Joins.Count; index++)
				EmitJoinClause(clause.Joins[index]);
			DecreaseIndent();
		}
		
		protected virtual void EmitJoinClause(JoinClause clause)
		{
			NewLine();
			Indent();
			if (clause.JoinType == JoinType.Cross)
			{
				AppendFormat("{0} {1} ", Keywords.Cross, Keywords.Join);
				EmitTableSpecifier(clause.FromClause.TableSpecifier);
			}
			else
			{
				switch (clause.JoinType)
				{
					case JoinType.Inner : Append(Keywords.Inner); break;
					case JoinType.Left : AppendFormat("{0} {1}", Keywords.Left, Keywords.Outer); break;
					case JoinType.Right : AppendFormat("{0} {1}", Keywords.Right, Keywords.Outer); break;
					default : throw new LanguageException(LanguageException.Codes.InvalidJoinType, clause.JoinType.ToString());
				}
				
				AppendFormat(" {0} ", Keywords.Join);
				EmitTableSpecifier(clause.FromClause.TableSpecifier);
				AppendFormat(" {0} ", Keywords.On);
				EmitExpression(clause.JoinExpression);
			}
			
			IncreaseIndent();
			for (int index = 0; index < clause.FromClause.Joins.Count; index++)
				EmitJoinClause(clause.FromClause.Joins[index]);
			DecreaseIndent();
		}
		
		protected virtual void EmitCalculusFromClause(CalculusFromClause clause)
		{
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.From);
			for (int index = 0; index < clause.TableSpecifiers.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitTableSpecifier(clause.TableSpecifiers[index]);
			}
		}
		
		protected virtual void EmitSubQueryExpression(Expression expression)
		{
			Append(Keywords.BeginGroup);
			if (expression is QueryExpression)
				EmitQueryExpression((QueryExpression)expression);
			else if (expression is SelectExpression)
				EmitSelectExpression((SelectExpression)expression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, expression.GetType().Name);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitTableSpecifier(TableSpecifier tableSpecifier)
		{
			if (tableSpecifier.TableExpression is TableExpression)
				EmitTableExpression((TableExpression)tableSpecifier.TableExpression);
			else
				EmitSubQueryExpression(tableSpecifier.TableExpression);
				
			if (tableSpecifier.TableAlias != String.Empty)
			{
				AppendFormat(" {0} ", Keywords.As);
				EmitIdentifier(tableSpecifier.TableAlias);
			}
		}
		
		protected virtual void EmitWhereClause(WhereClause clause)
		{
			if (clause != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Where);
				EmitExpression(clause.Expression);
			}
		}
		
		protected virtual void EmitGroupClause(GroupClause clause)
		{
			if (clause != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Group, Keywords.By);
				for (int index = 0; index < clause.Columns.Count; index++)
				{
					if (index > 0)
						EmitListSeparator();
					EmitExpression(clause.Columns[index]);
				}
			}
		}
		
		protected virtual void EmitHavingClause(HavingClause clause)
		{
			if (clause != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Having);
				EmitExpression(clause.Expression);
			}
		}
		
		protected virtual void EmitInsertStatement(InsertStatement insertStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Insert, Keywords.Into);
			EmitExpression(insertStatement.InsertClause.TableExpression);
			Append(Keywords.BeginGroup);
			for (int index = 0; index < insertStatement.InsertClause.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitInsertFieldExpression(insertStatement.InsertClause.Columns[index]);
			}
			Append(Keywords.EndGroup);
			NewLine();
			IncreaseIndent();
			Indent();
			if (insertStatement.Values is ValuesExpression)
				EmitValuesExpression((ValuesExpression)insertStatement.Values);
			else if (insertStatement.Values is QueryExpression)
				EmitQueryExpression((QueryExpression)insertStatement.Values);
			else if (insertStatement.Values is SelectExpression)
				EmitSelectExpression((SelectExpression)insertStatement.Values);
			DecreaseIndent();
		}
		
		protected virtual void EmitInsertFieldExpression(InsertFieldExpression expression)
		{
			EmitIdentifier(expression.FieldName);
		}
		
		protected virtual void EmitTableExpression(TableExpression expression)
		{
			if (expression.TableSchema != String.Empty)
			{
				EmitIdentifier(expression.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(expression.TableName);
		}
		
		protected virtual void EmitValuesExpression(ValuesExpression expression)
		{
			AppendFormat("{0}{1}", Keywords.Values, Keywords.BeginGroup);
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitExpression(expression.Expressions[index]);
			}
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitUpdateStatement(UpdateStatement updateStatement)
		{
			Indent();
			AppendFormat("{0} ", Keywords.Update);
			EmitExpression(updateStatement.UpdateClause.TableExpression);
			NewLine();
			IncreaseIndent();
			Indent();
			AppendFormat("{0} ", Keywords.Set);
			for (int index = 0; index < updateStatement.UpdateClause.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitUpdateFieldExpression(updateStatement.UpdateClause.Columns[index]);
			}
			
			EmitWhereClause(updateStatement.WhereClause);
			DecreaseIndent();
		}
		
		protected virtual void EmitUpdateFieldExpression(UpdateFieldExpression expression)
		{
			EmitIdentifier(expression.FieldName);
			AppendFormat(" {0} ", Keywords.Equal);
			EmitExpression(expression.Expression);
		}
		
		protected virtual void EmitDeleteStatement(DeleteStatement deleteStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Delete, Keywords.From);
			EmitExpression(deleteStatement.DeleteClause.TableExpression);
			IncreaseIndent();
			EmitWhereClause(deleteStatement.WhereClause);
			DecreaseIndent();
		}
		
		protected virtual void EmitCreateTableStatement(CreateTableStatement statement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Create, Keywords.Table);
			if (statement.TableSchema != String.Empty)
			{
				EmitIdentifier(statement.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(statement.TableName);
			NewLine();
			Indent();
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitColumnDefinition(statement.Columns[index]);
			}
			
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitColumnDefinition(ColumnDefinition column)
		{
			EmitIdentifier(column.ColumnName);
			AppendFormat(" {0}", column.DomainName);
			if (column.IsNullable)
				AppendFormat(" {0}", Keywords.Null);
			else
				AppendFormat(" {0} {1}", Keywords.Not, Keywords.Null);
		}
		
		protected virtual void EmitAlterColumnDefinition(AlterColumnDefinition column)
		{
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Column);
			EmitIdentifier(column.ColumnName);
			if (column.DomainName != null)
				AppendFormat(" {0}", column.DomainName);
			if (column.AlterNullable)
				if (column.IsNullable)
					AppendFormat(" {0}", Keywords.Null);
				else
					AppendFormat(" {0} {1}", Keywords.Not, Keywords.Null);
		}
		
		protected virtual void EmitAlterTableStatement(AlterTableStatement statement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Table);
			if (statement.TableSchema != String.Empty)
			{
				EmitIdentifier(statement.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(statement.TableName);
			Append(" ");
			
			bool first = true;
			for (int index = 0; index < statement.AddColumns.Count; index++)
			{
				if (!first)
					AppendFormat("{0} ", Keywords.ListSeparator);
				else
					first = false;
				AppendFormat("{0} ", Keywords.Add);
				EmitColumnDefinition(statement.AddColumns[index]);
			}
			
			for (int index = 0; index < statement.AlterColumns.Count; index++)
			{
				if (!first)
					AppendFormat("{0} ", Keywords.ListSeparator);
				else
					first = false;
				EmitAlterColumnDefinition(statement.AlterColumns[index]);
			}
			
			for (int index = 0; index < statement.DropColumns.Count; index++)
			{
				if (!first)
					AppendFormat("{0} ", Keywords.ListSeparator);
				else
					first = false;
				AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Column);
				EmitIdentifier(statement.DropColumns[index].ColumnName);
			}
		}
		
		protected virtual void EmitDropTableStatement(DropTableStatement statement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Table);
			if (statement.TableSchema != String.Empty)
			{
				EmitIdentifier(statement.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(statement.TableName);
		}
		
		protected virtual void EmitCreateIndexStatement(CreateIndexStatement statement)
		{
			Indent();
			AppendFormat("{0} ", Keywords.Create);
			if (statement.IsUnique)
				AppendFormat("{0} ", Keywords.Unique);
			if (statement.IsClustered)
				AppendFormat("{0} ", Keywords.Clustered);
			AppendFormat("{0} ", Keywords.Index);
			if (statement.IndexSchema != String.Empty)
			{
				EmitIdentifier(statement.IndexSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(statement.IndexName);
			AppendFormat(" {0} ", Keywords.On);
			if (statement.TableSchema != String.Empty)
			{
				EmitIdentifier(statement.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(statement.TableName);
			Append(Keywords.BeginGroup);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(statement.Columns[index]);
			}
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitOrderColumnDefinition(OrderColumnDefinition column)
		{
			EmitIdentifier(column.ColumnName);
			if (!column.Ascending)
				AppendFormat(" {0}", Keywords.Desc);
		}
		
		protected virtual void EmitDropIndexStatement(DropIndexStatement statement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Index);
			if (statement.IndexSchema != String.Empty)
			{
				EmitIdentifier(statement.IndexSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(statement.IndexName);
		}

		override protected void EmitStatementTerminator()
		{
			if (UseStatementTerminator)
				Append(";");
		}

		protected override string GetInstructionKeyword(string instruction)
		{
			if (instruction == "iConcatenation")
				return "||";
			else
				return base.GetInstructionKeyword(instruction);
		}

		public virtual SQLTextEmitter Clone()
		{
			SQLTextEmitter emitter = (SQLTextEmitter)Activator.CreateInstance(GetType());
			emitter.UseQuotedIdentifiers = UseQuotedIdentifiers;
			emitter.UseStatementTerminator = UseStatementTerminator;
			return emitter;
		}
		
		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
