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
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class OracleTextEmitter : SQL.SQLTextEmitter
	{
		public OracleTextEmitter()
		{
			// this is the default for the Oracle device, but it can be ovveridden.
			UseStatementTerminator = false;
		}

		protected override void EmitExpression(Expression AExpression)
		{
			if (AExpression is OuterJoinFieldExpression)
			{
				EmitQualifiedFieldExpression((SQL.QualifiedFieldExpression)AExpression);
				Append("(+)");
			}
			else
				base.EmitExpression(AExpression);
		}
		
		protected override void EmitAlterTableStatement(SQL.AlterTableStatement AStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", SQL.Keywords.Alter, SQL.Keywords.Table);
			EmitIdentifier(AStatement.TableName);
			
			for (int LIndex = 0; LIndex < AStatement.AddColumns.Count; LIndex++)
			{
				AppendFormat(" {0} {1}", SQL.Keywords.Add, SQL.Keywords.BeginGroup);
				EmitColumnDefinition(AStatement.AddColumns[LIndex]);
				Append(SQL.Keywords.EndGroup);
			}
			
			for (int LIndex = 0; LIndex < AStatement.AlterColumns.Count; LIndex++)
			{
				SQL.AlterColumnDefinition LDefinition = AStatement.AlterColumns[LIndex];
				if (LDefinition.AlterNullable)
				{
					AppendFormat(" {0} ", "modify");
					EmitIdentifier(LDefinition.ColumnName);
					if (LDefinition.IsNullable)
						AppendFormat(" {0}", SQL.Keywords.Null);
					else
						AppendFormat(" {0} {1}", SQL.Keywords.Not, SQL.Keywords.Null);
				}
			}
			
			for (int LIndex = 0; LIndex < AStatement.DropColumns.Count; LIndex++)
			{
				AppendFormat(" {0} {1}", SQL.Keywords.Drop, SQL.Keywords.BeginGroup);
				EmitIdentifier(AStatement.DropColumns[LIndex].ColumnName);
				Append(SQL.Keywords.EndGroup);
			}
		}

		protected override void EmitTableSpecifier(SQL.TableSpecifier ATableSpecifier)
		{
			if (ATableSpecifier.TableExpression is SQL.TableExpression)
				EmitTableExpression((SQL.TableExpression)ATableSpecifier.TableExpression);
			else
				EmitSubQueryExpression(ATableSpecifier.TableExpression);
				
			if (ATableSpecifier.TableAlias != String.Empty)
			{
				Append(" ");
				EmitIdentifier(ATableSpecifier.TableAlias);
			}
		}

		protected override void EmitSelectExpression(SQL.SelectExpression AExpression)
		{
			AppendFormat("{0} /*+ {1}*/ ", SQL.Keywords.Select, AExpression is SelectExpression ? ((SelectExpression)AExpression).OptimizerHints : "FIRST_ROWS(20)");
			IncreaseIndent();
			EmitSelectClause(AExpression.SelectClause);
			EmitFromClause(AExpression.FromClause);
			EmitWhereClause(AExpression.WhereClause);
			EmitGroupClause(AExpression.GroupClause);
			EmitHavingClause(AExpression.HavingClause);
			DecreaseIndent();
		}

		protected override void EmitCreateIndexStatement(SQL.CreateIndexStatement AStatement)
		{
			Indent();
			AppendFormat("{0} ", SQL.Keywords.Create);
			if (AStatement.IsUnique)
				AppendFormat("{0} ", SQL.Keywords.Unique);
			//if (AStatement.IsClustered)
			//	AppendFormat("{0} ", SQL.Keywords.Clustered);
			AppendFormat("{0} ", SQL.Keywords.Index);
			if (AStatement.IndexSchema != String.Empty)
			{
				EmitIdentifier(AStatement.IndexSchema);
				Append(SQL.Keywords.Qualifier);
			}
			EmitIdentifier(AStatement.IndexName);
			AppendFormat(" {0} ", SQL.Keywords.On);
			if (AStatement.TableSchema != String.Empty)
			{
				EmitIdentifier(AStatement.TableSchema);
				Append(SQL.Keywords.Qualifier);
			}
			EmitIdentifier(AStatement.TableName);
			Append(SQL.Keywords.BeginGroup);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(AStatement.Columns[LIndex]);
			}
			Append(SQL.Keywords.EndGroup);
		}
	}
}
