/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.DB2
{
	using System;
	using System.Text;
	
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using DB2 = Alphora.Dataphor.DAE.Language.DB2;
	
	public class DB2TextEmitter : SQLTextEmitter
	{		
		public const string UDB = "UDB";
		public const string ISeries = "iSeries";
		
		// Product
		private string _product;
		public string Product
		{
			get { return _product; }
			set { _product = value; }
		}
		
		protected override string GetTableOperatorKeyword(TableOperator tableOperator)
		{
			switch (tableOperator)
			{
				case TableOperator.Union: return SQL.Keywords.Union;
				case TableOperator.Intersect: return SQL.Keywords.Intersect;
				case TableOperator.Difference: return SQL.Keywords.Except;
				default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, tableOperator.ToString());
			}
		}
		
		protected override void EmitColumnDefinition(ColumnDefinition column)
		{
			EmitIdentifier(column.ColumnName);
			AppendFormat(" {0}", column.DomainName);
			if (!column.IsNullable)
				AppendFormat(" {0} {1}", SQL.Keywords.Not, SQL.Keywords.Null);
		}

		protected override void EmitTableSpecifier(TableSpecifier tableSpecifier)
		{
			if (tableSpecifier.TableExpression is TableExpression)
				EmitTableExpression((TableExpression)tableSpecifier.TableExpression);
			else
			{
				if (_product == UDB)
					AppendFormat("{0} ", SQL.Keywords.Table);
				EmitSubQueryExpression(tableSpecifier.TableExpression);
			}	
			if (tableSpecifier.TableAlias != String.Empty)
			{
				AppendFormat(" {0} ", SQL.Keywords.As);
				EmitIdentifier(tableSpecifier.TableAlias);
			}
		}
			
		protected override void EmitCreateIndexStatement(CreateIndexStatement statement)
		{   
			Indent();
			AppendFormat("{0} ", SQL.Keywords.Create);
			if (statement.IsUnique)
				AppendFormat("{0} ", SQL.Keywords.Unique);
			
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
			if ((_product == UDB) && statement.IsClustered)
					AppendFormat("{0} ", DB2.Keywords.Cluster);			
		}
		
		protected override void EmitBinaryExpression(BinaryExpression expression)
		{
			if (expression.Instruction == "iMod")
			{
				Append("Mod(");
				EmitExpression(expression.LeftExpression);
				Append(", ");
				EmitExpression(expression.RightExpression);
				Append(")");
			}
			else
				base.EmitBinaryExpression (expression);
		}
		
		protected override void EmitJoinClause(JoinClause clause)
		{			
			if (clause.JoinType == JoinType.Cross)
			{
				NewLine();
				Indent();				
				AppendFormat("{0} {1} ", Alphora.Dataphor.DAE.Language.SQL.Keywords.Inner, Alphora.Dataphor.DAE.Language.SQL.Keywords.Join);
				EmitTableSpecifier(clause.FromClause.TableSpecifier);
				AppendFormat(" {0} 1 = 1", Alphora.Dataphor.DAE.Language.SQL.Keywords.On);
			}
			else
			{
				base.EmitJoinClause(clause);
			}
		}
	}
}