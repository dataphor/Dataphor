/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator Selector<T>(const AValues : row) : T
	public class ObjectSelectorNode : InstructionNode
	{
		private Type _classType;

		public string ClassName { get; set; }

		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);
			_classType = plan.CreateType(new ClassDefinition(ClassName));
		}

		public override object InternalExecute(Program program, object[] arguments)
		{
			var result = Activator.CreateInstance(_classType);
			var row = arguments[0] as IRow;
			if (row != null)
			{
				for (int i = 0; i < row.DataType.Columns.Count; i++)
				{
					_classType.GetProperty(row.DataType.Columns[i].Name).SetValue(result, row[i], null);
				}
			}
			return result;
		}
	}
	
	// operator ReadProperty<T, P>(const AInstance : T) : P
	public class ObjectPropertyReadNode : PlanNode
	{
		public string PropertyName { get; set; }

		public override object InternalExecute(Program program)
		{
			var instance = Nodes[0].Execute(program);
			#if NILPROPOGATION
			if (instance == null)
				return null;
			#endif

			var result = instance.GetType().GetProperty(PropertyName).GetValue(instance, null);

			return ObjectMarshal.ToNativeOf(program.ValueManager, DataType, result);
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			return new QualifierExpression((Expression)Nodes[0].EmitStatement(mode), new IdentifierExpression(PropertyName));
		}
	}
	
	// operator WriteProperty<T, P>(AInstance : T, const AValue : P) : T
	public class ObjectPropertyWriteNode : PlanNode
	{
		public string PropertyName { get; set; }

		public override object InternalExecute(Program program)
		{
			var instance = Nodes[0].Execute(program);
			#if NILPROPOGATION
			if (instance == null)
				return null;
			#endif
			var value = Nodes[1].Execute(program);
			var property = instance.GetType().GetProperty(PropertyName);

			if (value != null && !property.PropertyType.IsAssignableFrom(value.GetType()))
			{
				// In theory, the only way this is possible is if the source is a ListValue and the target is a native list representation such as List<T> or other IList
				var sourceListValue = value as IList;
				if (sourceListValue == null)
					throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for source value: {0}", value.GetType().FullName));

				IList targetListValue = null;

				if (property.CanWrite)
				{
					// If the target property supports assignment, we construct a new list of the type of the property, and perform the assignment
					targetListValue = Activator.CreateInstance(property.PropertyType) as IList;
					if (targetListValue == null)
						throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for target property: {0}", property.PropertyType.FullName));
				}
				else
				{
					// Otherwise, we attempt to clear the current list and then add the elements to it
					targetListValue = property.GetValue(instance, null) as IList;
					if (targetListValue == null)
						throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for target property: {0}", property.PropertyType.FullName));

					targetListValue.Clear();
				}

				for (int index = 0; index < sourceListValue.Count; index++)
					targetListValue.Add(sourceListValue[index]);

				if (property.CanWrite)
					property.SetValue(instance, targetListValue, null);
			}
			else
				property.SetValue(instance, value, null);

			return instance;
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			return new AssignmentStatement(new QualifierExpression((Expression)Nodes[0].EmitStatement(mode), new IdentifierExpression(PropertyName)), (Expression)Nodes[1].EmitStatement(mode));
		}
	}

    // operator ReadAsString<T>(AInstance : T) : String
    public class ObjectAsStringReadAccessorNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			
			return arguments[0].ToString();
		}
    }
}
