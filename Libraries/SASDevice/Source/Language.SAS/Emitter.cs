/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.SAS
{
	using System;
	using System.Text;
	
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	
	public class SASTextEmitter : SQLTextEmitter
	{
		public SASTextEmitter()
		{
			// this is the default for the SAS device, but it can be ovveridden.
			UseStatementTerminator = false;
		}

		protected override void EmitExpression(Expression AExpression)
		{
			if (AExpression is OuterJoinFieldExpression)
			{
				EmitQualifiedFieldExpression((QualifiedFieldExpression)AExpression);
				Append("(+)");
			}
			else
				base.EmitExpression(AExpression);
		}
		
		protected override void EmitAlterTableStatement(AlterTableStatement AStatement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Table);
			EmitIdentifier(AStatement.TableName);
			
			for (int LIndex = 0; LIndex < AStatement.AddColumns.Count; LIndex++)
			{
				AppendFormat(" {0} {1}", Keywords.Add, Keywords.BeginGroup);
				EmitColumnDefinition(AStatement.AddColumns[LIndex]);
				Append(Keywords.EndGroup);
			}
			
			for (int LIndex = 0; LIndex < AStatement.DropColumns.Count; LIndex++)
			{
				AppendFormat(" {0} {1}", Keywords.Drop, Keywords.BeginGroup);
				EmitIdentifier(AStatement.DropColumns[LIndex].ColumnName);
				Append(Keywords.EndGroup);
			}
		}

		protected override void EmitTableSpecifier(TableSpecifier ATableSpecifier)
		{
			if (ATableSpecifier.TableExpression is TableExpression)
				EmitTableExpression((TableExpression)ATableSpecifier.TableExpression);
			else
				EmitSubQueryExpression(ATableSpecifier.TableExpression);
				
			if (ATableSpecifier.TableAlias != String.Empty)
			{
				Append(" ");
				EmitIdentifier(ATableSpecifier.TableAlias);
			}
		}
	}
}
