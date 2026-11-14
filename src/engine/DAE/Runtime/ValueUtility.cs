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
		public static object ValidateValue(Program program, Schema.ScalarType type, object tempValue)
		{
			return ValidateValue(program, type, tempValue, null);
		}
		
		public static object ValidateValue(Program program, Schema.ScalarType type, object tempValue, Schema.Operator fromOperator)
		{
			program.Stack.Push(tempValue);
			try
			{
				TableNode.ValidateScalarTypeConstraints(program, type, false);
				TableNode.ExecuteScalarTypeValidateHandlers(program, type, fromOperator);
			}
			finally
			{
				tempValue = program.Stack.Pop();
			}
			
			return tempValue;
		}
		
		public static object DefaultValue(Program program, Schema.ScalarType type)
		{
			// ScalarType level default trigger handlers
			program.Stack.Push(null);
			try
			{
				if (type.HasHandlers())
					foreach (Schema.EventHandler handler in type.EventHandlers)
						if ((handler.EventType & EventType.Default) != 0)
						{
							object result = handler.PlanNode.Execute(program);
							if ((result != null) && (bool)result)
								return program.Stack.Peek(0);
						}
			}
			finally
			{
				program.Stack.Pop();
			}

			if (type.Default != null)
				return type.Default.Node.Execute(program);
			else
				return null;
		}
	}
}
