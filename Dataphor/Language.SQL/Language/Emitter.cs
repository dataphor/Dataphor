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
		private StringBuilder _text;
		private int _indent;
		
		protected void IncreaseIndent()
		{
			_indent++;
		}
		
		protected void DecreaseIndent()
		{
			if (_indent > 0)
				_indent--;
		}
		
		protected void NewLine()
		{
			_text.Append("\r\n");
		}
		
		protected void Indent()
		{
			for (int index = 0; index < _indent; index++)
				_text.Append("\t");
		}
		
		protected void Append(string stringValue)
		{
			_text.Append(stringValue);
		}
		
		protected void AppendFormat(string stringValue, params object[] paramsValue)
		{
			_text.AppendFormat(stringValue, paramsValue);
		}
		
		protected void AppendLine(string stringValue)
		{
			Indent();
			Append(stringValue);
			NewLine();
		}
		
		protected void AppendFormatLine(string stringValue, params object[] paramsValue)
		{
			Indent();
			AppendFormat(stringValue, paramsValue);
			NewLine();
		}

		protected abstract void InternalEmit(Statement statement);
		
		public string Emit(Statement statement)
		{
			_text = new StringBuilder();
			_indent = 0;
			InternalEmit(statement);
			return _text.ToString();
		}
	}
	
	public class BasicTextEmitter : TextEmitter
	{
		protected virtual void EmitExpression(Expression expression)
		{
			if (expression is UnaryExpression)
				EmitUnaryExpression((UnaryExpression)expression);
			else if (expression is BinaryExpression)
				EmitBinaryExpression((BinaryExpression)expression);
			else if (expression is CallExpression)
				EmitCallExpression((CallExpression)expression);
			else if (expression is ValueExpression)
				EmitValueExpression((ValueExpression)expression);
			else if (expression is IdentifierExpression)
				EmitIdentifierExpression((IdentifierExpression)expression);
			else if (expression is CaseExpression)
				EmitCaseExpression((CaseExpression)expression);
			else if (expression is BetweenExpression)
				EmitBetweenExpression((BetweenExpression)expression);
			else if (expression is QualifierExpression)
				EmitQualifierExpression((QualifierExpression)expression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, expression == null ? "null" : expression.GetType().Name);
		}

		public static string InstructionNameToKeyword(string instruction)
		{
			switch (instruction)
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
				default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, instruction);
			}
		}
		
		protected virtual string GetInstructionKeyword(string instruction)
		{
			return InstructionNameToKeyword(instruction);
		}

		protected virtual void EmitUnaryExpression(UnaryExpression expression)
		{
			AppendFormat("{0}{1}", GetInstructionKeyword(Schema.Object.Unqualify(expression.Instruction)), "(");
			EmitExpression(expression.Expression);
			Append(")");
		}
		
		protected virtual void EmitBinaryExpression(BinaryExpression expression)
		{
			Append("(");
			EmitExpression(expression.LeftExpression);
			AppendFormat(" {0} ", GetInstructionKeyword(Schema.Object.Unqualify(expression.Instruction)));
			EmitExpression(expression.RightExpression);
			Append(")");
		}
		
		protected virtual void EmitCallExpression(CallExpression expression)
		{
			AppendFormat("{0}{1}", expression.Identifier, "(");
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					AppendFormat("{0} ", ",");
				EmitExpression(expression.Expressions[index]);
			}
			Append(")");
		}
		
		protected virtual void EmitBetweenExpression(BetweenExpression expression)
		{
			Append("(");
			EmitExpression(expression.Expression);
			Append(" between ");
			EmitExpression(expression.LowerExpression);
			Append(" and ");
			EmitExpression(expression.UpperExpression);
			Append(")");
		}

		protected virtual void EmitQualifierExpression(QualifierExpression expression)
		{
			EmitExpression(expression.LeftExpression);
			Append(".");
			EmitExpression(expression.RightExpression);
		}
		
		protected virtual void EmitValueExpression(ValueExpression expression)
		{
			switch (expression.Token)
			{
				case TokenType.Nil : Append(SQL.Keywords.Null); break;
				case TokenType.String : Append("'" + ((string)expression.Value).Replace("'", "''") + "'"); break;
				case TokenType.Decimal: Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}d", expression.Value)); break;
				case TokenType.Money : Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "${0}", expression.Value)); break;
				case TokenType.Boolean : Append(((bool)expression.Value ? "true" : "false")); break;
				case TokenType.Hex: ((long)expression.Value).ToString("X"); break;
				default : Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", expression.Value)); break;
			}
		}
		
		protected virtual void EmitIdentifierExpression(IdentifierExpression expression)
		{
			Append(expression.Identifier);
		}
		
		protected virtual void EmitCaseExpression(CaseExpression expression)
		{
			AppendFormat("{0}", "case");

			if (expression.Expression != null)
			{
				Append(" ");
				EmitExpression(expression.Expression);
			}
			
			for (int index = 0; index < expression.CaseItems.Count; index++)
			{
				AppendFormat(" {0} ", "when");
				EmitExpression(expression.CaseItems[index].WhenExpression);
				AppendFormat(" {0} ", "then");
				EmitExpression(expression.CaseItems[index].ThenExpression);
			}
			
			if (expression.ElseExpression != null)
			{
				AppendFormat(" {0} ", "else");
				EmitExpression(((CaseElseExpression)expression.ElseExpression).Expression);
			}

			AppendFormat(" {0}", "end");
		}
		
		protected virtual void EmitStatement(Statement statement)
		{
			throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected override void InternalEmit(Statement statement)
		{
			if (statement is Expression)
				EmitExpression((Expression)statement);
			else if (statement is EmptyStatement)
			{
				// do nothing;
			}
			else
				EmitStatement(statement);
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

