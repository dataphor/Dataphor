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
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Alphora.Dataphor.FHIR.Core
{
	// operator FHIR.Base.AsJSON(const AsJSON : String) : FHIR.Base
	public class FHIRAsJSONSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return FhirParser.ParseFromJson((string)argument1);
		}
	}
	
	// operator FHIR.Base.ReadAsJSON(const AValue : FHIR.Base) : String
	public class FHIRAsJSONReadAccessorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return FhirSerializer.SerializeToJson((Base)argument1);
		}
	}
	
	// operator FHIR.Base.WriteAsJSON(const AInstance : FHIR.Base, const AValue : String) : FHIR.Base
	public class FHIRAsJSONWriteAccessorNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif

			return FhirParser.ParseFromJson((string)argument2);
		}
	}

	// operator FHIR.Base.AsXML(const AsXML : String) : FHIR.Base
	public class FHIRAsXMLSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return FhirParser.ParseFromXml((string)argument1);
		}
	}
	
	// operator FHIR.Base.ReadAsXML(const AValue : FHIR.Base) : String
	public class FHIRAsXMLReadAccessorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return FhirSerializer.SerializeToXml((Base)argument1);
		}
	}
	
	// operator FHIR.Base.WriteAsXML(const AInstance : FHIR.Base, const AValue : String) : FHIR.Base
	public class FHIRAsXMLWriteAccessorNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif

			return FhirParser.ParseFromXml((string)argument2);
		}
	}
}
