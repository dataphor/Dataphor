/*
	Dataphor
	© Copyright 2000-2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define NILPROPOGATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.FHIR.Core
{
	/// <remarks> operator iEqual(Dynamic, Dynamic) : bool </remarks>
	public class DynamicEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return JToken.DeepEquals((JToken)argument1, (JToken)argument2);
		}
	}

	// operator FHIR.Dynamic(const ARow : row { Content : String }) : FHIR.Dynamic
	public class DynamicContentSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			var content = (String)((IRow)argument1)["Content"];
			#if NILPROPOGATION
			if (content == null)
				return null;
			#endif

			return JsonConvert.DeserializeObject(content);
		}
	}
	
	// operator FHIR.Dynamic(const Content : String) : FHIR.Dynamic
	public class DynamicSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return JsonConvert.DeserializeObject((string)argument1);
		}
	}

	// operator FHIR.Dynamic.ReadContent(const AValue : FHIR.Dynamic) : String
	public class DynamicContentReadAccessorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return JsonConvert.SerializeObject((JToken)argument1);
		}
	}
	
	// operator FHIR.Dynamic.WriteContent(const AInstance : FHIR.Dynamic, const AContent : String) : FHIR.Dynamic
	public class DynamicContentWriteAccessorNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif

			return JsonConvert.DeserializeObject((string)argument2);
		}
	}

	// operator Get(const AInstance : Dynamic, const AKey : scalar) : Dynamic
	public class DynamicGetNode: BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			#endif

			return ((JToken)argument1)[argument2];
		}
	}

	// operator Set(const AInstance : Dynamic, const AKey : scalar, const AValue : Dynamic) : Dynamic
	public class DynamicSetNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			#endif

			var instance = ((JToken)argument1).DeepClone();
			var jObject = instance as JObject;
			if (jObject != null)
			{
				jObject[(string)argument2] = (JToken)argument3;
				return instance;
			}

			var jArray = instance as JArray;
			if (jArray != null)
			{
				jArray[(int)argument2] = (JToken)argument3;
				return instance;
			}

			// TODO: Throw a runtime exception
			throw new InvalidOperationException("Set method can only be used on object or list types.");
		}
	}

	public class DynamicToListNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			ListValue listValue = new ListValue(program.ValueManager, (IListType)DataType);
			foreach (var value in (JArray)argument1)
			{
				listValue.Add(value);
			}

			return listValue;

			//return (JArray)argument1;
		}
	}

	public class DynamicToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (bool)((JValue)argument1);
		}
	}

	public class DynamicToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (byte)((JValue)argument1);
		}
	}

	public class DynamicToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (short)((JValue)argument1);
		}
	}

	public class DynamicToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (int)((JValue)argument1);
		}
	}

	public class DynamicToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (long)((JValue)argument1);
		}
	}

	public class DynamicToDecimalNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (decimal)((JValue)argument1);
		}
	}

	public class DynamicToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (string)((JValue)argument1);
		}
	}

	public class DynamicToTimeSpanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (TimeSpan)((JValue)argument1);
		}
	}

	public class DynamicToDateTimeNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return (DateTime)((JValue)argument1);
		}
	}
}
