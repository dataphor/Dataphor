/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language
{
	using System;
	using System.Text;
	
	public abstract class TextEmitter
	{
		private StringBuilder FText;
		private int FIndent;
		
		protected void IncreaseIndent()
		{
			FIndent++;
		}
		
		protected void DecreaseIndent()
		{
			if (FIndent > 0)
				FIndent--;
		}
		
		protected void NewLine()
		{
			FText.Append("\r\n");
		}
		
		protected void Indent()
		{
			for (int LIndex = 0; LIndex < FIndent; LIndex++)
				FText.Append("\t");
		}
		
		protected void Append(string AString)
		{
			FText.Append(AString);
		}
		
		protected void AppendFormat(string AString, params object[] AParams)
		{
			FText.AppendFormat(AString, AParams);
		}
		
		protected void AppendLine(string AString)
		{
			Indent();
			Append(AString);
			NewLine();
		}
		
		protected void AppendFormatLine(string AString, params object[] AParams)
		{
			Indent();
			AppendFormat(AString, AParams);
			NewLine();
		}

		protected abstract void InternalEmit(Statement AStatement);
		
		public string Emit(Statement AStatement)
		{
			FText = new StringBuilder();
			FIndent = 0;
			InternalEmit(AStatement);
			return FText.ToString();
		}
	}
	
	public class BasicTextEmitter : TextEmitter
	{
		protected virtual void EmitExpression(Expression AExpression)
		{
			if (AExpression is UnaryExpression)
				EmitUnaryExpression((UnaryExpression)AExpression);
			else if (AExpression is BinaryExpression)
				EmitBinaryExpression((BinaryExpression)AExpression);
			else if (AExpression is CallExpression)
				EmitCallExpression((CallExpression)AExpression);
			else if (AExpression is ValueExpression)
				EmitValueExpression((ValueExpression)AExpression);
			else if (AExpression is IdentifierExpression)
				EmitIdentifierExpression((IdentifierExpression)AExpression);
			else if (AExpression is CaseExpression)
				EmitCaseExpression((CaseExpression)AExpression);
			else if (AExpression is BetweenExpression)
				EmitBetweenExpression((BetweenExpression)AExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, AExpression == null ? "null" : AExpression.GetType().Name);
		}

		public static string InstructionNameToKeyword(string AInstruction)
		{
			switch (AInstruction)
			{
				case "iNot": return "not";
				case "iNegate": return "-";
				case "iBitwiseNot": return "~";
				case "iExists": return "exists";
				case "iAddition": return "+";
				case "iSubtraction": return "-";
				case "iMultiplication": return "*";
				case "iDivision": return "/";
				case "iDiv": return "div";
				case "iMod": return "mod";
				case "iPower": return "**";
				case "iEqual": return "=";
				case "iNotEqual": return "<>";
				case "iLess": return "<";
				case "iInclusiveLess": return "<=";
				case "iGreater": return ">";
				case "iInclusiveGreater": return ">=";
				case "iCompare": return "?=";
				case "iAnd": return "and";
				case "iOr": return "or";
				case "iXor": return "xor";
				case "iIn": return "in";
				case "iLike": return "like";
				case "iMatches": return "matches";
				case "iBitwiseAnd": return "&";
				case "iBitwiseOr": return "|";
				case "iBitwiseXor": return "^";
				case "iShiftLeft": return "<<";
				case "iShiftRight": return ">>";
				default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, AInstruction);
			}
		}
		
		protected virtual string GetInstructionKeyword(string AInstruction)
		{
			return InstructionNameToKeyword(AInstruction);
		}

		protected virtual void EmitUnaryExpression(UnaryExpression AExpression)
		{
			AppendFormat("{0}{1}", GetInstructionKeyword(Schema.Object.Unqualify(AExpression.Instruction)), "(");
			EmitExpression(AExpression.Expression);
			Append(")");
		}
		
		protected virtual void EmitBinaryExpression(BinaryExpression AExpression)
		{
			Append("(");
			EmitExpression(AExpression.LeftExpression);
			AppendFormat(" {0} ", GetInstructionKeyword(Schema.Object.Unqualify(AExpression.Instruction)));
			EmitExpression(AExpression.RightExpression);
			Append(")");
		}
		
		protected virtual void EmitCallExpression(CallExpression AExpression)
		{
			AppendFormat("{0}{1}", AExpression.Identifier, "(");
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					AppendFormat("{0} ", ",");
				EmitExpression(AExpression.Expressions[LIndex]);
			}
			Append(")");
		}
		
		protected virtual void EmitBetweenExpression(BetweenExpression AExpression)
		{
			Append("(");
			EmitExpression(AExpression.Expression);
			Append(" between ");
			EmitExpression(AExpression.LowerExpression);
			Append(" and ");
			EmitExpression(AExpression.UpperExpression);
			Append(")");
		}
		
		protected virtual void EmitValueExpression(ValueExpression AExpression)
		{
			switch (AExpression.Token)
			{
				case TokenType.Nil : Append(D4.Keywords.Nil); break;
				case TokenType.String : Append("'" + ((string)AExpression.Value).Replace("'", "''") + "'"); break;
				case TokenType.Decimal: Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}d", AExpression.Value)); break;
				case TokenType.Money : Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "${0}", AExpression.Value)); break;
				case TokenType.Boolean : Append(((bool)AExpression.Value ? "true" : "false")); break;
				case TokenType.Hex : ((long)AExpression.Value).ToString("X"); break;
				default : Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", AExpression.Value)); break;
			}
		}
		
		protected virtual void EmitIdentifierExpression(IdentifierExpression AExpression)
		{
			Append(AExpression.Identifier);
		}
		
		protected virtual void EmitCaseExpression(CaseExpression AExpression)
		{
			AppendFormat("{0}", "case");

			if (AExpression.Expression != null)
			{
				Append(" ");
				EmitExpression(AExpression.Expression);
			}
			
			for (int LIndex = 0; LIndex < AExpression.CaseItems.Count; LIndex++)
			{
				AppendFormat(" {0} ", "when");
				EmitExpression(AExpression.CaseItems[LIndex].WhenExpression);
				AppendFormat(" {0} ", "then");
				EmitExpression(AExpression.CaseItems[LIndex].ThenExpression);
			}
			
			if (AExpression.ElseExpression != null)
			{
				AppendFormat(" {0} ", "else");
				EmitExpression(((CaseElseExpression)AExpression.ElseExpression).Expression);
			}

			AppendFormat(" {0}", "end");
		}
		
		protected virtual void EmitStatement(Statement AStatement)
		{
			throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected override void InternalEmit(Statement AStatement)
		{
			if (AStatement is Expression)
				EmitExpression((Expression)AStatement);
			else if (AStatement is EmptyStatement)
			{
				// do nothing;
			}
			else
				EmitStatement(AStatement);
		}
		
		protected virtual void EmitListSeparator()
		{
			Append(", ");
		}
		
		protected virtual void EmitStatementTerminator()
		{
			Append(";");
		}
	}
}

