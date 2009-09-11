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
		private bool FUseStatementTerminator = true;
		public bool UseStatementTerminator
		{
			set { FUseStatementTerminator = value; }
			get { return FUseStatementTerminator; }
		}

		private bool FUseQuotedIdentifiers = true;
		public bool UseQuotedIdentifiers
		{
			set { FUseQuotedIdentifiers = value;}
			get { return FUseQuotedIdentifiers;}
		}
		
		protected override void EmitExpression(Expression AExpression)
		{
			if (AExpression is QualifiedFieldExpression)
				EmitQualifiedFieldExpression((QualifiedFieldExpression)AExpression);
			else if (AExpression is TableExpression)
				EmitTableExpression((TableExpression)AExpression);
			else if (AExpression is AggregateCallExpression)
				EmitAggregateCallExpression((AggregateCallExpression)AExpression);
			else if (AExpression is UserExpression)
				EmitUserExpression((UserExpression)AExpression);
			else if (AExpression is QueryParameterExpression)
				EmitQueryParameterExpression((QueryParameterExpression)AExpression);
			else if ((AExpression is QueryExpression) || (AExpression is SelectExpression))
				EmitSubQueryExpression(AExpression);
			else if (AExpression is CastExpression)
				EmitCastExpression((CastExpression)AExpression);
			else if (AExpression is ListExpression)
				EmitListExpression((ListExpression)AExpression);
			else
				base.EmitExpression(AExpression);
		}
		
		protected virtual void EmitQueryParameterExpression(QueryParameterExpression AExpression)
		{
			AppendFormat("@{0}", AExpression.ParameterName);
		}

		protected virtual void EmitCastExpression(CastExpression AExpression)
		{
			AppendFormat("cast(");
			EmitExpression(AExpression.Expression);
			AppendFormat(" as {0})", AExpression.DomainName);
		}
		
		protected virtual void EmitListExpression(ListExpression AExpression)
		{
			AppendFormat("( ");
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitExpression(AExpression.Expressions[LIndex]);
			}
			AppendFormat(" )");
		}
		
		protected override void EmitValueExpression(ValueExpression AExpression)
		{
			if (AExpression.Value == null)
				Append(Keywords.Null);
			else if (AExpression.Token == TokenType.Decimal)
			{
				decimal LValue = (decimal)AExpression.Value;
				if (Decimal.Truncate(LValue) == LValue)
					Append(String.Format(System.Globalization.NumberFormatInfo.InvariantInfo, "{0:F0}.0", LValue));
				else
					Append(String.Format(System.Globalization.NumberFormatInfo.InvariantInfo, "{0:G}", LValue));
			}
			else
				base.EmitValueExpression(AExpression);
		}
		
		protected override void EmitUnaryExpression(UnaryExpression AExpression)
		{
			if (AExpression.Instruction == "iIsNull")
			{
				EmitExpression(AExpression.Expression);
				AppendFormat(" {0}", " is null");
			}
			else if (AExpression.Instruction == "iIsNotNull")
			{
				EmitExpression(AExpression.Expression);
				AppendFormat(" {0}", " is not null");
			}
			else
			{
				base.EmitUnaryExpression(AExpression);
			}
		}

		protected virtual void EmitTerminatedStatement(Statement AStatement)
		{
			if (AStatement is SelectStatement)
				EmitSelectStatement((SelectStatement)AStatement);
			else if (AStatement is InsertStatement)
				EmitInsertStatement((InsertStatement)AStatement);
			else if (AStatement is UpdateStatement)
				EmitUpdateStatement((UpdateStatement)AStatement);
			else if (AStatement is DeleteStatement)
				EmitDeleteStatement((DeleteStatement)AStatement);
			else if (AStatement is CreateTableStatement)
				EmitCreateTableStatement((CreateTableStatement)AStatement);
			else if (AStatement is AlterTableStatement)
				EmitAlterTableStatement((AlterTableStatement)AStatement);
			else if (AStatement is DropTableStatement)
				EmitDropTableStatement((DropTableStatement)AStatement);
			else if (AStatement is CreateIndexStatement)
				EmitCreateIndexStatement((CreateIndexStatement)AStatement);
			else if (AStatement is DropIndexStatement)
				EmitDropIndexStatement((DropIndexStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
			EmitStatementTerminator();
		}
		
		protected override void EmitStatement(Statement AStatement)
		{
			if (AStatement is Batch)
				EmitBatch((Batch)AStatement);
			else
				EmitTerminatedStatement(AStatement);
		}
		
		protected virtual void EmitBatch(Batch ABatch)
		{
			for (int LIndex = 0; LIndex < ABatch.Statements.Count; LIndex++)
			{
				if (LIndex > 0)
					NewLine();
				EmitStatement(ABatch.Statements[LIndex]);
			}
		}

		protected virtual void EmitSelectStatement(SelectStatement ASelectStatement)
		{
			Indent();
			EmitQueryExpression(ASelectStatement.QueryExpression);
			if (ASelectStatement.OrderClause != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Order, Keywords.By);
				for (int LIndex = 0; LIndex < ASelectStatement.OrderClause.Columns.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					EmitOrderFieldExpression(ASelectStatement.OrderClause.Columns[LIndex]);
				}
			}
		}
		
		protected virtual void EmitOrderFieldExpression(OrderFieldExpression AOrderFieldExpression)
		{
			EmitQualifiedFieldExpression(AOrderFieldExpression);
			AppendFormat(" {0}", AOrderFieldExpression.Ascending ? Keywords.Asc : Keywords.Desc);
		}
		
		protected virtual void EmitIdentifier(string AIdentifier)
		{
			if (AIdentifier == "*")
				Append(AIdentifier);
			else
			{
				string LAppendString = "{0}";
				if (FUseQuotedIdentifiers)
					LAppendString = "\"" + LAppendString + "\"";
				AppendFormat(LAppendString, AIdentifier);
			}
		}
		
		protected virtual void EmitQualifiedFieldExpression(QualifiedFieldExpression AExpression)
		{
			if (AExpression.TableAlias != String.Empty)
			{
				EmitIdentifier(AExpression.TableAlias);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(AExpression.FieldName);
		}
		
		protected virtual void EmitQueryExpression(QueryExpression AQueryExpression)
		{
			EmitSelectExpression(AQueryExpression.SelectExpression);
			foreach (TableOperatorExpression LTableOperatorExpression in AQueryExpression.TableOperators)
				EmitTableOperatorExpression(LTableOperatorExpression);
		}
		
		protected virtual string GetTableOperatorKeyword(TableOperator ATableOperator)
		{
			switch (ATableOperator)
			{
				case TableOperator.Union: return Keywords.Union;
				case TableOperator.Intersect: return Keywords.Intersect;
				case TableOperator.Difference: return Keywords.Minus;
				default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, ATableOperator.ToString());
			}
		}
		
		protected virtual void EmitTableOperatorExpression(TableOperatorExpression ATableOperatorExpression)
		{
			NewLine();
			Indent();
			Append(GetTableOperatorKeyword(ATableOperatorExpression.TableOperator));
			if (!ATableOperatorExpression.Distinct)
				AppendFormat(" {0}", Keywords.All);
			
			NewLine();
			Indent();
			EmitSelectExpression(ATableOperatorExpression.SelectExpression);
		}
		
		protected virtual void EmitSelectExpression(SelectExpression AExpression)
		{
			AppendFormat("{0} ", Keywords.Select);
			IncreaseIndent();
			EmitSelectClause(AExpression.SelectClause);
			EmitFromClause(AExpression.FromClause);
			EmitWhereClause(AExpression.WhereClause);
			EmitGroupClause(AExpression.GroupClause);
			EmitHavingClause(AExpression.HavingClause);
			DecreaseIndent();
		}
		
		protected virtual void EmitSelectClause(SelectClause AClause)
		{
			NewLine();
			Indent();
			if (AClause.Distinct)
				AppendFormat("{0} ", Keywords.Distinct);
			if (AClause.Columns.Count == 0)
				Append(Keywords.Star);
			for (int LIndex = 0; LIndex < AClause.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitColumnExpression(AClause.Columns[LIndex]);
			}
		}
		
		protected virtual void EmitColumnExpression(ColumnExpression AExpression)
		{
			EmitExpression(AExpression.Expression);
			if (AExpression.ColumnAlias != String.Empty)
			{
				AppendFormat(" {0} ", Keywords.As);
				EmitIdentifier(AExpression.ColumnAlias);
			}
		}
		
		protected virtual void EmitAggregateCallExpression(AggregateCallExpression AExpression)
		{
			AppendFormat("{0}{1}", AExpression.Identifier, Keywords.BeginGroup);
			if (AExpression.IsDistinct)
				AppendFormat("{0} ", Keywords.Distinct);
			EmitExpression(AExpression.Expressions[0]);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitUserExpression(UserExpression AExpression)
		{
			string[] LArguments = new string[AExpression.Expressions.Count];
			SQLTextEmitter LEmitter = Clone();
			for (int LIndex = 0; LIndex < LArguments.Length; LIndex++)
				LArguments[LIndex] = LEmitter.Emit(AExpression.Expressions[LIndex]);
				
			AppendFormat(AExpression.TranslationString, LArguments);
		}
		
		protected virtual void EmitFromClause(FromClause AClause)
		{
			if (AClause is AlgebraicFromClause)
				EmitAlgebraicFromClause((AlgebraicFromClause)AClause);
			else if (AClause is CalculusFromClause)
				EmitCalculusFromClause((CalculusFromClause)AClause);
		}
		
		protected virtual void EmitAlgebraicFromClause(AlgebraicFromClause AClause)
		{
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.From);
			EmitTableSpecifier(AClause.TableSpecifier);
			IncreaseIndent();
			for (int LIndex = 0; LIndex < AClause.Joins.Count; LIndex++)
				EmitJoinClause(AClause.Joins[LIndex]);
			DecreaseIndent();
		}
		
		protected virtual void EmitJoinClause(JoinClause AClause)
		{
			NewLine();
			Indent();
			if (AClause.JoinType == JoinType.Cross)
			{
				AppendFormat("{0} {1} ", Keywords.Cross, Keywords.Join);
				EmitTableSpecifier(AClause.FromClause.TableSpecifier);
			}
			else
			{
				switch (AClause.JoinType)
				{
					case JoinType.Inner : Append(Keywords.Inner); break;
					case JoinType.Left : AppendFormat("{0} {1}", Keywords.Left, Keywords.Outer); break;
					case JoinType.Right : AppendFormat("{0} {1}", Keywords.Right, Keywords.Outer); break;
					default : throw new LanguageException(LanguageException.Codes.InvalidJoinType, AClause.JoinType.ToString());
				}
				
				AppendFormat(" {0} ", Keywords.Join);
				EmitTableSpecifier(AClause.FromClause.TableSpecifier);
				AppendFormat(" {0} ", Keywords.On);
				EmitExpression(AClause.JoinExpression);
			}
			
			IncreaseIndent();
			for (int LIndex = 0; LIndex < AClause.FromClause.Joins.Count; LIndex++)
				EmitJoinClause(AClause.FromClause.Joins[LIndex]);
			DecreaseIndent();
		}
		
		protected virtual void EmitCalculusFromClause(CalculusFromClause AClause)
		{
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.From);
			for (int LIndex = 0; LIndex < AClause.TableSpecifiers.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitTableSpecifier(AClause.TableSpecifiers[LIndex]);
			}
		}
		
		protected virtual void EmitSubQueryExpression(Expression AExpression)
		{
			Append(Keywords.BeginGroup);
			if (AExpression is QueryExpression)
				EmitQueryExpression((QueryExpression)AExpression);
			else if (AExpression is SelectExpression)
				EmitSelectExpression((SelectExpression)AExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, AExpression.GetType().Name);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitTableSpecifier(TableSpecifier ATableSpecifier)
		{
			if (ATableSpecifier.TableExpression is TableExpression)
				EmitTableExpression((TableExpression)ATableSpecifier.TableExpression);
			else
				EmitSubQueryExpression(ATableSpecifier.TableExpression);
				
			if (ATableSpecifier.TableAlias != String.Empty)
			{
				AppendFormat(" {0} ", Keywords.As);
				EmitIdentifier(ATableSpecifier.TableAlias);
			}
		}
		
		protected virtual void EmitWhereClause(WhereClause AClause)
		{
			if (AClause != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Where);
				EmitExpression(AClause.Expression);
			}
		}
		
		protected virtual void EmitGroupClause(GroupClause AClause)
		{
			if (AClause != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Group, Keywords.By);
				for (int LIndex = 0; LIndex < AClause.Columns.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					EmitExpression(AClause.Columns[LIndex]);
				}
			}
		}
		
		protected virtual void EmitHavingClause(HavingClause AClause)
		{
			if (AClause != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Having);
				EmitExpression(AClause.Expression);
			}
		}
		
		protected virtual void EmitInsertStatement(InsertStatement AInsertStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Insert, Keywords.Into);
			EmitExpression(AInsertStatement.InsertClause.TableExpression);
			Append(Keywords.BeginGroup);
			for (int LIndex = 0; LIndex < AInsertStatement.InsertClause.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitInsertFieldExpression(AInsertStatement.InsertClause.Columns[LIndex]);
			}
			Append(Keywords.EndGroup);
			NewLine();
			IncreaseIndent();
			Indent();
			if (AInsertStatement.Values is ValuesExpression)
				EmitValuesExpression((ValuesExpression)AInsertStatement.Values);
			else if (AInsertStatement.Values is QueryExpression)
				EmitQueryExpression((QueryExpression)AInsertStatement.Values);
			else if (AInsertStatement.Values is SelectExpression)
				EmitSelectExpression((SelectExpression)AInsertStatement.Values);
			DecreaseIndent();
		}
		
		protected virtual void EmitInsertFieldExpression(InsertFieldExpression AExpression)
		{
			EmitIdentifier(AExpression.FieldName);
		}
		
		protected virtual void EmitTableExpression(TableExpression AExpression)
		{
			if (AExpression.TableSchema != String.Empty)
			{
				EmitIdentifier(AExpression.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(AExpression.TableName);
		}
		
		protected virtual void EmitValuesExpression(ValuesExpression AExpression)
		{
			AppendFormat("{0}{1}", Keywords.Values, Keywords.BeginGroup);
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitExpression(AExpression.Expressions[LIndex]);
			}
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitUpdateStatement(UpdateStatement AUpdateStatement)
		{
			Indent();
			AppendFormat("{0} ", Keywords.Update);
			EmitExpression(AUpdateStatement.UpdateClause.TableExpression);
			NewLine();
			IncreaseIndent();
			Indent();
			AppendFormat("{0} ", Keywords.Set);
			for (int LIndex = 0; LIndex < AUpdateStatement.UpdateClause.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitUpdateFieldExpression(AUpdateStatement.UpdateClause.Columns[LIndex]);
			}
			
			EmitWhereClause(AUpdateStatement.WhereClause);
			DecreaseIndent();
		}
		
		protected virtual void EmitUpdateFieldExpression(UpdateFieldExpression AExpression)
		{
			EmitIdentifier(AExpression.FieldName);
			AppendFormat(" {0} ", Keywords.Equal);
			EmitExpression(AExpression.Expression);
		}
		
		protected virtual void EmitDeleteStatement(DeleteStatement ADeleteStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Delete, Keywords.From);
			EmitExpression(ADeleteStatement.DeleteClause.TableExpression);
			IncreaseIndent();
			EmitWhereClause(ADeleteStatement.WhereClause);
			DecreaseIndent();
		}
		
		protected virtual void EmitCreateTableStatement(CreateTableStatement AStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Create, Keywords.Table);
			if (AStatement.TableSchema != String.Empty)
			{
				EmitIdentifier(AStatement.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(AStatement.TableName);
			NewLine();
			Indent();
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitColumnDefinition(AStatement.Columns[LIndex]);
			}
			
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitColumnDefinition(ColumnDefinition AColumn)
		{
			EmitIdentifier(AColumn.ColumnName);
			AppendFormat(" {0}", AColumn.DomainName);
			if (AColumn.IsNullable)
				AppendFormat(" {0}", Keywords.Null);
			else
				AppendFormat(" {0} {1}", Keywords.Not, Keywords.Null);
		}
		
		protected virtual void EmitAlterColumnDefinition(AlterColumnDefinition AColumn)
		{
			EmitIdentifier(AColumn.ColumnName);
			if (AColumn.DomainName != null)
				AppendFormat(" {0}", AColumn.DomainName);
			if (AColumn.AlterNullable)
				if (AColumn.IsNullable)
					AppendFormat(" {0}", Keywords.Null);
				else
					AppendFormat(" {0} {1}", Keywords.Not, Keywords.Null);
		}
		
		protected virtual void EmitAlterTableStatement(AlterTableStatement AStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Table);
			if (AStatement.TableSchema != String.Empty)
			{
				EmitIdentifier(AStatement.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(AStatement.TableName);
			Append(" ");
			
			bool LFirst = true;
			for (int LIndex = 0; LIndex < AStatement.AddColumns.Count; LIndex++)
			{
				if (!LFirst)
					AppendFormat("{0} ", Keywords.ListSeparator);
				else
					LFirst = false;
				AppendFormat("{0} ", Keywords.Add);
				EmitColumnDefinition(AStatement.AddColumns[LIndex]);
			}
			
			for (int LIndex = 0; LIndex < AStatement.AlterColumns.Count; LIndex++)
			{
				if (!LFirst)
					AppendFormat("{0} ", Keywords.ListSeparator);
				else
					LFirst = false;
				AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Column);
				EmitAlterColumnDefinition(AStatement.AlterColumns[LIndex]);
			}
			
			for (int LIndex = 0; LIndex < AStatement.DropColumns.Count; LIndex++)
			{
				if (!LFirst)
					AppendFormat("{0} ", Keywords.ListSeparator);
				else
					LFirst = false;
				AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Column);
				EmitIdentifier(AStatement.DropColumns[LIndex].ColumnName);
			}
		}
		
		protected virtual void EmitDropTableStatement(DropTableStatement AStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Table);
			if (AStatement.TableSchema != String.Empty)
			{
				EmitIdentifier(AStatement.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(AStatement.TableName);
		}
		
		protected virtual void EmitCreateIndexStatement(CreateIndexStatement AStatement)
		{
			Indent();
			AppendFormat("{0} ", Keywords.Create);
			if (AStatement.IsUnique)
				AppendFormat("{0} ", Keywords.Unique);
			if (AStatement.IsClustered)
				AppendFormat("{0} ", Keywords.Clustered);
			AppendFormat("{0} ", Keywords.Index);
			if (AStatement.IndexSchema != String.Empty)
			{
				EmitIdentifier(AStatement.IndexSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(AStatement.IndexName);
			AppendFormat(" {0} ", Keywords.On);
			if (AStatement.TableSchema != String.Empty)
			{
				EmitIdentifier(AStatement.TableSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(AStatement.TableName);
			Append(Keywords.BeginGroup);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(AStatement.Columns[LIndex]);
			}
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitOrderColumnDefinition(OrderColumnDefinition AColumn)
		{
			EmitIdentifier(AColumn.ColumnName);
			if (!AColumn.Ascending)
				AppendFormat(" {0}", Keywords.Desc);
		}
		
		protected virtual void EmitDropIndexStatement(DropIndexStatement AStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Index);
			if (AStatement.IndexSchema != String.Empty)
			{
				EmitIdentifier(AStatement.IndexSchema);
				Append(Keywords.Qualifier);
			}
			EmitIdentifier(AStatement.IndexName);
		}

		override protected void EmitStatementTerminator()
		{
			if (UseStatementTerminator)
				Append(";");
		}

		protected override string GetInstructionKeyword(string AInstruction)
		{
			if (AInstruction == "iConcatenation")
				return "||";
			else
				return base.GetInstructionKeyword(AInstruction);
		}

		public virtual SQLTextEmitter Clone()
		{
			SQLTextEmitter LEmitter = (SQLTextEmitter)Activator.CreateInstance(GetType());
			LEmitter.UseQuotedIdentifiers = UseQuotedIdentifiers;
			LEmitter.UseStatementTerminator = UseStatementTerminator;
			return LEmitter;
		}
		
		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
