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
		public const string CUDB = "UDB";
		public const string CISeries = "iSeries";
		
		// Product
		private string FProduct;
		public string Product
		{
			get { return FProduct; }
			set { FProduct = value; }
		}
		
		protected override string GetTableOperatorKeyword(TableOperator ATableOperator)
		{
			switch (ATableOperator)
			{
				case TableOperator.Union: return SQL.Keywords.Union;
				case TableOperator.Intersect: return SQL.Keywords.Intersect;
				case TableOperator.Difference: return SQL.Keywords.Except;
				default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, ATableOperator.ToString());
			}
		}
		
		protected override void EmitColumnDefinition(ColumnDefinition AColumn)
		{
			EmitIdentifier(AColumn.ColumnName);
			AppendFormat(" {0}", AColumn.DomainName);
			if (!AColumn.IsNullable)
				AppendFormat(" {0} {1}", SQL.Keywords.Not, SQL.Keywords.Null);
		}

		protected override void EmitTableSpecifier(TableSpecifier ATableSpecifier)
		{
			if (ATableSpecifier.TableExpression is TableExpression)
				EmitTableExpression((TableExpression)ATableSpecifier.TableExpression);
			else
			{
				if (FProduct == CUDB)
					AppendFormat("{0} ", SQL.Keywords.Table);
				EmitSubQueryExpression(ATableSpecifier.TableExpression);
			}	
			if (ATableSpecifier.TableAlias != String.Empty)
			{
				AppendFormat(" {0} ", SQL.Keywords.As);
				EmitIdentifier(ATableSpecifier.TableAlias);
			}
		}
			
		protected override void EmitCreateIndexStatement(CreateIndexStatement AStatement)
		{   
			Indent();
			AppendFormat("{0} ", SQL.Keywords.Create);
			if (AStatement.IsUnique)
				AppendFormat("{0} ", SQL.Keywords.Unique);
			
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
			if ((FProduct == CUDB) && AStatement.IsClustered)
					AppendFormat("{0} ", DB2.Keywords.Cluster);			
		}
		
		protected override void EmitBinaryExpression(BinaryExpression AExpression)
		{
			if (AExpression.Instruction == "iMod")
			{
				Append("Mod(");
				EmitExpression(AExpression.LeftExpression);
				Append(", ");
				EmitExpression(AExpression.RightExpression);
				Append(")");
			}
			else
				base.EmitBinaryExpression (AExpression);
		}
		
		protected override void EmitJoinClause(JoinClause AClause)
		{			
			if (AClause.JoinType == JoinType.Cross)
			{
				NewLine();
				Indent();				
				AppendFormat("{0} {1} ", Alphora.Dataphor.DAE.Language.SQL.Keywords.Inner, Alphora.Dataphor.DAE.Language.SQL.Keywords.Join);
				EmitTableSpecifier(AClause.FromClause.TableSpecifier);
				AppendFormat(" {0} 1 = 1", Alphora.Dataphor.DAE.Language.SQL.Keywords.On);
			}
			else
			{
				base.EmitJoinClause(AClause);
			}
		}
	}
}