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
					var property = _classType.GetProperty(row.DataType.Columns[i].Name);
					if (property == null)
					{
						throw new CompilerException(CompilerException.Codes.UnknownPropertyReference, DataType.Name, row.DataType.Columns[i].Name);
					}

					ObjectMarshal.SetHostProperty(result, property, row[i]);
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

			ObjectMarshal.SetHostProperty(instance, property, value);

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
