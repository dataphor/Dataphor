/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.D4
{
	using System;
	using System.Text;
	using System.Collections;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Runtime.Data;
	
	/*
		Parameterize
		Restrict
		Order

		Parameter: {@<name>}
	*/
	public class Dataphormulator : Object
	{
		public static string Parameterize(string AStatement, Row AValues)
		{
			StringBuilder LResult = new StringBuilder();
			string LParamName;
			char LQuoteChar = '\0';
			int LIndex = 0;
			while (LIndex < AStatement.Length)
			{
				// Skip strings
				if ((AStatement[LIndex] == '\'') || (AStatement[LIndex] == '"'))
				{
					if (LQuoteChar != '\0')
					{
						if ((AStatement[LIndex] == LQuoteChar) && ((LIndex >= AStatement.Length - 1) || (AStatement[LIndex + 1] != LQuoteChar)))
							LQuoteChar = '\0';
					}
					else
						LQuoteChar = AStatement[LIndex];
				}
				
				// If this is a parameter, output the appropriate value, else output the current character
				if ((LQuoteChar == '\0') && (AStatement[LIndex] == '{') && (LIndex < AStatement.Length - 1) && (AStatement[LIndex + 1] == '@'))
				{
					LParamName = AStatement.Substring(LIndex + 2, AStatement.IndexOf('}', LIndex) - (LIndex + 2));
					if (AValues.DataType.Columns[LParamName].DataType.Is(Schema.DataType.SystemString))
					{
						LResult.Append(@"""");
						LResult.Append(((Scalar)AValues[LParamName]).ToString().Replace(@"""", @""""""));
						LResult.Append(@"""");
					}
					else
						LResult.Append(((Scalar)AValues[LParamName]).ToString());
					LIndex = AStatement.IndexOf('}', LIndex);
				}
				else
					LResult.Append(AStatement[LIndex]);

				LIndex++;
			}
			return LResult.ToString();
		}
		
		public static Expression GetRestrictCondition(Schema.Key ADetailKey, Schema.Key AMasterKey, Row AMasterValues)
		{
			Expression LCondition = null;
			Expression LValueExpression;
			Expression LEqualExpression;
			int LColumnIndex;
			for (int LIndex = 0; LIndex < ADetailKey.Columns.Count; LIndex++)
			{
				LColumnIndex = AMasterValues.DataType.Columns.IndexOf(AMasterKey.Columns[LIndex].Name);
				if (AMasterValues.HasValue(LColumnIndex))
				{
					if (AMasterValues.DataType.Columns[LColumnIndex].DataType.Is(Schema.DataType.SystemString))
						LValueExpression = new ValueExpression(((Scalar)AMasterValues[LColumnIndex]).ToString(), LexerToken.String);
					else if (AMasterValues.DataType.Columns[LColumnIndex].DataType.Is(Schema.DataType.SystemInteger))
						LValueExpression = new ValueExpression(((Scalar)AMasterValues[LColumnIndex]).ToInt32(), LexerToken.Integer);
					//else if (AMasterValues.DataType.Columns[LColumnIndex].DataType.Is(Schema.DataType.SystemDouble))
					//	LValueExpression = new ValueExpression(((Scalar)AMasterValues[LColumnIndex]).ToDouble(), LexerToken.Float);
					else if (AMasterValues.DataType.Columns[LColumnIndex].DataType.Is(Schema.DataType.SystemDecimal))
						LValueExpression = new ValueExpression(((Scalar)AMasterValues[LColumnIndex]).ToDecimal(), LexerToken.Decimal);
					else if (AMasterValues.DataType.Columns[LColumnIndex].DataType.Is(Schema.DataType.SystemBoolean))
						LValueExpression = new ValueExpression(((Scalar)AMasterValues[LColumnIndex]).ToBoolean(), LexerToken.Boolean);
					else if (AMasterValues.DataType.Columns[LColumnIndex].DataType.Is(Schema.DataType.SystemGuid))
						LValueExpression = new ValueExpression(((Scalar)AMasterValues[LColumnIndex]).ToGuid());
					else
						LValueExpression = new ValueExpression(((Scalar)AMasterValues[LColumnIndex]).ToString(), LexerToken.String);

					LEqualExpression = new BinaryExpression(new IdentifierExpression(ADetailKey.Columns[LIndex].Name), Instructions.Equal, LValueExpression);
				}
				else
					LEqualExpression = new CallExpression("IsNull", new Expression[]{new IdentifierExpression(ADetailKey.Columns[LIndex].Name)});

				if (LCondition == null)
					LCondition = LEqualExpression;
				else
					LCondition = new BinaryExpression(LCondition, Instructions.And, LEqualExpression);
			}
			return LCondition;
		}
		
		public static RestrictExpression GetRestrictExpression(Schema.Key ADetailKey, Schema.Key AMasterKey, Row AMasterValues)
		{
			RestrictExpression LRestrictExpression = new RestrictExpression();
			LRestrictExpression.Condition = GetRestrictCondition(ADetailKey, AMasterKey, AMasterValues);
			return LRestrictExpression;
		}
		
		public static OrderExpression GetOrderExpression(Schema.Order AOrder)
		{
			OrderExpression LOrderExpression = new OrderExpression();
			foreach (Schema.OrderColumn LOrderColumn in AOrder.Columns)
				LOrderExpression.Columns.Add(new OrderColumnDefinition(LOrderColumn.Column.Name, LOrderColumn.Ascending));
			return LOrderExpression;
		}
		
		public static BrowseExpression GetBrowseExpression(Schema.Order AOrder)
		{
			BrowseExpression LBrowseExpression = new BrowseExpression();
			foreach (Schema.OrderColumn LOrderColumn in AOrder.Columns)
				LBrowseExpression.Columns.Add(new OrderColumnDefinition(LOrderColumn.Column.Name, LOrderColumn.Ascending));
			return LBrowseExpression;
		}
		
		public static RestrictExpression MergeRestrictExpression(RestrictExpression ARestrictExpression, Expression AExpression)
		{
			if (AExpression is RestrictExpression)
			{
				((RestrictExpression)AExpression).Condition = new BinaryExpression(((RestrictExpression)AExpression).Condition, Instructions.And, ARestrictExpression.Condition);
				return (RestrictExpression)AExpression;
			}
			else
			{
				ARestrictExpression.Expression = AExpression;
				return ARestrictExpression;
			}
		}
		
		public static OrderExpression MergeOrderExpression(OrderExpression AOrderExpression, Expression AExpression)
		{
			if (AExpression is OrderExpression)
				AOrderExpression.Expression = ((OrderExpression)AExpression).Expression;
			else if (AExpression is BrowseExpression)
				AOrderExpression.Expression = ((BrowseExpression)AExpression).Expression;
			else
				AOrderExpression.Expression = AExpression;
			return AOrderExpression;
		}
		
		public static BrowseExpression MergeBrowseExpression(BrowseExpression ABrowseExpression, Expression AExpression)
		{
			if (AExpression is OrderExpression)
				ABrowseExpression.Expression = ((OrderExpression)AExpression).Expression;
			else if (AExpression is BrowseExpression)
				ABrowseExpression.Expression = ((BrowseExpression)AExpression).Expression;
			else
				ABrowseExpression.Expression = AExpression;
			return ABrowseExpression;
		}

		public static Expression MergeRestrictionCondition(Expression AExpression, Expression ACondition)
		{
			if (AExpression is RestrictExpression)
			{
				if (((RestrictExpression)AExpression).Condition != null)
					((RestrictExpression)AExpression).Condition = new BinaryExpression(((RestrictExpression)AExpression).Condition, Instructions.And, ACondition);
				else
					((RestrictExpression)AExpression).Condition = ACondition;
				return AExpression;
			}
			else
				return new RestrictExpression(AExpression, ACondition);
		}

		//	Parse AStatement
		//	if the top node is an OnExpression
		//		insert the nodes before it
		//	else
		//		insert the nodes as the top
		//		
		//	if the top node is an order, replace it with this order
		//	if the top node is a restrict, add our restrict to it
		//	Parse AStatement
		//	if the top node is an OnExpression
		//		insert the RestrictExpression before it
		//	else
		//		insert the RestrictExpression as the top node
		public static string RestrictAndOrder(string AStatement, Schema.Key ADetailKey, Schema.Key AMasterKey, Row AMasterValues, Schema.Order AOrder, CursorIsolation AIsolation, CursorCapability ACapabilities, CursorType ACursorType)
		{
			RestrictExpression LRestrictExpression = GetRestrictExpression(ADetailKey, AMasterKey, AMasterValues);
			OrderExpression LOrderExpression = GetOrderExpression(AOrder);
			Parser LParser = new Parser();
			Expression LExpression = LParser.ParseCursorDefinition(AStatement);
			CursorDefinition LCursorExpression;
			if (LExpression is CursorDefinition)
			{
				LCursorExpression = (CursorDefinition)LExpression;
				LExpression = LCursorExpression.Expression;
			}
			else
				LCursorExpression = new CursorDefinition(LExpression);

			// View level settings for isolation and capabilities always override those in the expression
			LCursorExpression.Isolation = AIsolation;
			LCursorExpression.Capabilities = ACapabilities;
			LCursorExpression.CursorType = ACursorType;

			#if OnExpression			
			if (LExpression is OnExpression)
			{
				Expression LRemoteExpression = ((OnExpression)LExpression).Expression;
				if (LRemoteExpression is OrderExpression)
					LRemoteExpression = ((OrderExpression)LRemoteExpression).Expression;

				LOrderExpression.Expression = MergeRestrictExpression(LRestrictExpression, LRemoteExpression);
				((OnExpression)LExpression).Expression = LOrderExpression;
			}
			else
			#endif
			{
				if (LExpression is OrderExpression)
					LExpression = ((OrderExpression)LExpression).Expression;
				else 
				{
					if (LExpression is BrowseExpression)
						LExpression = ((BrowseExpression)LExpression).Expression;
				}

				LOrderExpression.Expression = MergeRestrictExpression(LRestrictExpression, LExpression);
				LExpression = LOrderExpression;
			}
			
			LCursorExpression.Expression = LExpression;
			return EmitExpression(LCursorExpression);
		}
		
		public static string Restrict(string AStatement, Schema.Key ADetailKey, Schema.Key AMasterKey, Row AMasterValues, CursorIsolation AIsolation, CursorCapability ACapabilities, CursorType ACursorType)
		{
			RestrictExpression LRestrictExpression = GetRestrictExpression(ADetailKey, AMasterKey, AMasterValues);
			Parser LParser = new Parser();
			Expression LExpression = LParser.ParseCursorDefinition(AStatement);
			CursorDefinition LCursorExpression;
			if (LExpression is CursorDefinition)
			{
				LCursorExpression = (CursorDefinition)LExpression;
				LExpression = LCursorExpression.Expression;
			}
			else
				LCursorExpression = new CursorDefinition(LExpression);
			
			// View level settings for isolation and capabilities always override those in the expression
			LCursorExpression.Isolation = AIsolation;
			LCursorExpression.Capabilities = ACapabilities;
			LCursorExpression.CursorType = ACursorType;

			#if OnExpression			
			if (LExpression is OnExpression)
			{
				if (((OnExpression)LExpression).Expression is OrderExpression)
					((OrderExpression)((OnExpression)LExpression).Expression).Expression = MergeRestrictExpression(LRestrictExpression, ((OrderExpression)((OnExpression)LExpression).Expression).Expression);
				else
					((OnExpression)LExpression).Expression = MergeRestrictExpression(LRestrictExpression, ((OnExpression)LExpression).Expression);
			}
			else
			#endif
			{
				if (LExpression is OrderExpression)
					((OrderExpression)LExpression).Expression = MergeRestrictExpression(LRestrictExpression, ((OrderExpression)LExpression).Expression);
				else 
				{
					if (LExpression is BrowseExpression)
						((BrowseExpression)LExpression).Expression = MergeRestrictExpression(LRestrictExpression, ((BrowseExpression)LExpression).Expression);
					else
						LExpression = MergeRestrictExpression(LRestrictExpression, LExpression);
				}
			}
			
			LCursorExpression.Expression = LExpression;
			return EmitExpression(LCursorExpression);
		}
		
		public static string Order(string AStatement, Schema.Order AOrder, CursorIsolation AIsolation, CursorCapability ACapabilities, CursorType ACursorType)
		{
			OrderExpression LOrderExpression = GetOrderExpression(AOrder);
			Parser LParser = new Parser();
			Expression LExpression = LParser.ParseCursorDefinition(AStatement);
			CursorDefinition LCursorExpression;
			if (LExpression is CursorDefinition)
			{
				LCursorExpression = (CursorDefinition)LExpression;
				LExpression = LCursorExpression.Expression;
			}
			else
				LCursorExpression = new CursorDefinition(LExpression);
				
			// View level settings for isolation and capabilities always override those in the expression
			LCursorExpression.Isolation = AIsolation;
			LCursorExpression.Capabilities = ACapabilities;
			LCursorExpression.CursorType = ACursorType;
			
			#if OnExpression
			if (LExpression is OnExpression)
				((OnExpression)LExpression).Expression = MergeOrderExpression(LOrderExpression, ((OnExpression)LExpression).Expression);
			else
			#endif
				LExpression = MergeOrderExpression(LOrderExpression, LExpression);
				
			LCursorExpression.Expression = LExpression;
			return EmitExpression(LCursorExpression);			
		}
		
		public static string Browse(string AStatement, Schema.Order AOrder, CursorIsolation AIsolation, CursorCapability ACapabilities, CursorType ACursorType)
		{
			BrowseExpression LBrowseExpression = GetBrowseExpression(AOrder);
			Parser LParser = new Parser();
			Expression LExpression = LParser.ParseCursorDefinition(AStatement);

			CursorDefinition LCursorExpression;
			if (LExpression is CursorDefinition)
			{
				LCursorExpression = (CursorDefinition)LExpression;
				LExpression = LCursorExpression.Expression;
			}
			else
				LCursorExpression = new CursorDefinition(LExpression);
				
			// View level settings for isolation and capabilities always override those in the expression
			LCursorExpression.Isolation = AIsolation;
			LCursorExpression.Capabilities = ACapabilities;
			LCursorExpression.CursorType = ACursorType;
			
			#if OnExpression
			if (LExpression is OnExpression)
				((OnExpression)LExpression).Expression = MergeBrowseExpression(LBrowseExpression, ((OnExpression)LExpression).Expression);
			else
			#endif
				LExpression = MergeBrowseExpression(LBrowseExpression, LExpression);
				
			LCursorExpression.Expression = LExpression;
			return EmitExpression(LCursorExpression);			
		}
		
		public static string Cursor(string AStatement, CursorIsolation AIsolation, CursorCapability ACapabilities, CursorType ACursorType)
		{
			Parser LParser = new Parser();
			CursorDefinition LExpression = (CursorDefinition)LParser.ParseCursorDefinition(AStatement);
			LExpression.Isolation = AIsolation;
			LExpression.Capabilities = ACapabilities;
			LExpression.CursorType = ACursorType;
			return EmitExpression(LExpression);
		}
		
		public static string EmitExpression(Expression AExpression)
		{
			return new D4TextEmitter().Emit(AExpression);
		}
	}
}

