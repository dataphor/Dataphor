/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime
{
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	
	public static class ValueUtility
	{
		public static object ValidateValue(Program AProgram, Schema.ScalarType AType, object AValue)
		{
			return ValidateValue(AProgram, AType, AValue, null);
		}
		
		public static object ValidateValue(Program AProgram, Schema.ScalarType AType, object AValue, Schema.Operator AFromOperator)
		{
			AProgram.Stack.Push(AValue);
			try
			{
				TableNode.ValidateScalarTypeConstraints(AProgram, AType, false);
				TableNode.ExecuteScalarTypeValidateHandlers(AProgram, AType, AFromOperator);
			}
			finally
			{
				AValue = AProgram.Stack.Pop();
			}
			
			return AValue;
		}
		
		public static object DefaultValue(Program AProgram, Schema.ScalarType AType)
		{
			// ScalarType level default trigger handlers
			AProgram.Stack.Push(null);
			try
			{
				if (AType.HasHandlers())
					foreach (Schema.EventHandler LHandler in AType.EventHandlers)
						if ((LHandler.EventType & EventType.Default) != 0)
						{
							object LResult = LHandler.PlanNode.Execute(AProgram);
							if ((LResult != null) && (bool)LResult)
								return AProgram.Stack.Peek(0);
						}
			}
			finally
			{
				AProgram.Stack.Pop();
			}

			if (AType.Default != null)
				return AType.Default.Node.Execute(AProgram);
			else
				return null;
		}
	}
}
