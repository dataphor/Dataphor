/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define NILPROPOGATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.CQF.Model
{
	// operator CQF.Model.AsXML(const AsXML : String) : CQF.Model.Base
	public class ModelAsXMLSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ModelObjectConveyor.Deserialize((string)argument1);
		}
	}
	
	// operator CQF.Model.ReadAsXML(const AValue : CQF.Model) : String
	public class ModelAsXMLReadAccessorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ModelObjectConveyor.Serialize(argument1);
		}
	}
	
	// operator CQF.Model.WriteAsXML(const AInstance : CQF.Model.Base, const AValue : String) : CQF.Model.Base
	public class ModelAsXMLWriteAccessorNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif

			return ModelObjectConveyor.Deserialize((string)argument2);
		}
	}

	// operator CQF.Model.iEqual(CQF.Model.Base, CQF.Model.Base): Boolean
	public class ModelEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif

			return argument1.Equals(argument2);
		}
	}
}
