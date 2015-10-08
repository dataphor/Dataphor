/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.TSQL
{
	using System;
	using System.Text;
	
	using Alphora.Dataphor.DAE.Language;
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class TSQLTextEmitter : SQL.SQLTextEmitter
	{
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
		
		protected override void EmitSelectStatement(Alphora.Dataphor.DAE.Language.SQL.SelectStatement selectStatement)
		{
			base.EmitSelectStatement(selectStatement);
			
			if ((selectStatement.Modifiers != null) && selectStatement.Modifiers.Contains("OptimizerHints"))
			{
				NewLine();
				Indent();
				Append(selectStatement.Modifiers["OptimizerHints"].Value);
			}
		}

		
		protected override void EmitDropIndexStatement(SQL.DropIndexStatement statement)
		{
			if (statement is DropIndexStatement)
			{
				Indent();
				AppendFormat("{0} {1} ", SQL.Keywords.Drop, SQL.Keywords.Index);
				if (((DropIndexStatement)statement).TableSchema != String.Empty)
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