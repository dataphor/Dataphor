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
		protected override void EmitBinaryExpression(BinaryExpression AExpression)
		{
			if (AExpression.Instruction == "iNullValue")
			{
				Append("IsNull(");
				EmitExpression(AExpression.LeftExpression);
				Append(", ");
				EmitExpression(AExpression.RightExpression);
				Append(")");
			}
			else
				base.EmitBinaryExpression(AExpression);
		}

		
		protected override void EmitTableSpecifier(SQL.TableSpecifier ATableSpecifier)
		{
			base.EmitTableSpecifier(ATableSpecifier);
			if (ATableSpecifier.TableExpression is TableExpression)
				AppendFormat(" {0}", ((TableExpression)ATableSpecifier.TableExpression).OptimizerHints);
		}
		
		protected override void EmitSelectStatement(Alphora.Dataphor.DAE.Language.SQL.SelectStatement ASelectStatement)
		{
			base.EmitSelectStatement(ASelectStatement);
			
			if ((ASelectStatement.Modifiers != null) && ASelectStatement.Modifiers.Contains("OptimizerHints"))
			{
				NewLine();
				Indent();
				Append(ASelectStatement.Modifiers["OptimizerHints"].Value);
			}
		}

		
		protected override void EmitDropIndexStatement(SQL.DropIndexStatement AStatement)
		{
			if (AStatement is DropIndexStatement)
			{
				Indent();
				AppendFormat("{0} {1} ", SQL.Keywords.Drop, SQL.Keywords.Index);
				if (((DropIndexStatement)AStatement).TableSchema != String.Empty)
				{
					EmitIdentifier(((DropIndexStatement)AStatement).TableSchema);
					Append(SQL.Keywords.Qualifier);
				}
				EmitIdentifier(((DropIndexStatement)AStatement).TableName);
				Append(SQL.Keywords.Qualifier);
				EmitIdentifier(AStatement.IndexName);
			}
			else
				base.EmitDropIndexStatement(AStatement);
		}

		protected override string GetInstructionKeyword(string AInstruction)
		{
			if (AInstruction == "iConcatenation")
				return "+";
				
			else if (AInstruction == "iMod")
				return "%";

			else 
				return base.GetInstructionKeyword(AInstruction);
		}
	}
}