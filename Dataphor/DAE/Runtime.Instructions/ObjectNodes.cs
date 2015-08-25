/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator Selector<T>(const AArg1 : AType1, ... const AArgN : ATypeN) : T
	public class ObjectSelectorNode : InstructionNode
	{
		public string ClassName { get; set; }
		public string[] PropertyNames { get; set; }

		public override object InternalExecute(Program program, object[] arguments)
		{
			var resultType = Type.GetType(ClassName, true);
			var result = Activator.CreateInstance(Type.GetType(ClassName, true));
			for (int i = 0; i < arguments.Length; i++)
			{
				resultType.GetProperty(PropertyNames[i]).SetValue(result, arguments[i], null);
			}
			return result;
		}
	}
	
	// operator ReadProperty<T, P>(const AInstance : T) : P
	public class ObjectPropertyReadNode : InstructionNode
	{
		public string PropertyName { get; set; }

		public override object InternalExecute(Program program, object[] arguments)
		{
			var instance = arguments[0];
			#if NILPROPOGATION
			if (instance == null)
				return null;
			#endif
			return instance.GetType().GetProperty(PropertyName).GetValue(instance, null);
		}
	}
	
	// operator WriteProperty<T, P>(AInstance : T, const AValue : P) : T
	public class ObjectPropertyWriteNode : InstructionNode
	{
		public string PropertyName { get; set; }

		public override object InternalExecute(Program program, object[] arguments)
		{
			var instance = arguments[0];
			#if NILPROPOGATION
			if (instance == null)
				return null;
			#endif
			instance.GetType().GetProperty(PropertyName).SetValue(instance, arguments[1], null);
			return instance;
		}
	}
}
