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

		protected override void EmitExpression(Expression expression)
		{
			if (expression is OuterJoinFieldExpression)
			{
				EmitQualifiedFieldExpression((QualifiedFieldExpression)expression);
				Append("(+)");
			}
			else
				base.EmitExpression(expression);
		}
		
		protected override void EmitAlterTableStatement(AlterTableStatement statement)
		{
			Indent();
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Table);
			EmitIdentifier(statement.TableName);
			
			for (int index = 0; index < statement.AddColumns.Count; index++)
			{
				AppendFormat(" {0} {1}", Keywords.Add, Keywords.BeginGroup);
				EmitColumnDefinition(statement.AddColumns[index]);
				Append(Keywords.EndGroup);
			}
			
			for (int index = 0; index < statement.DropColumns.Count; index++)
			{
				AppendFormat(" {0} {1}", Keywords.Drop, Keywords.BeginGroup);
				EmitIdentifier(statement.DropColumns[index].ColumnName);
				Append(Keywords.EndGroup);
			}
		}

		protected override void EmitTableSpecifier(TableSpecifier tableSpecifier)
		{
			if (tableSpecifier.TableExpression is TableExpression)
				EmitTableExpression((TableExpression)tableSpecifier.TableExpression);
			else
				EmitSubQueryExpression(tableSpecifier.TableExpression);
				
			if (tableSpecifier.TableAlias != String.Empty)
			{
				Append(" ");
				EmitIdentifier(tableSpecifier.TableAlias);
			}
		}
	}
}
