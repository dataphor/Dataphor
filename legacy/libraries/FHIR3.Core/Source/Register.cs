/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Reflection;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Schema = Alphora.Dataphor.DAE.Schema;
using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.FHIR3.Core
{
	class DAERegister
	{
		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			
			Type[] types = typeof(Base).Assembly.GetTypes();

			foreach (Type type in types)
			{
				if (type.Equals(typeof(Base)) || type.IsSubclassOf(typeof(Base)))
					classes.Add(new SettingsItem(type.FullName, type.AssemblyQualifiedName));
			}

			classes.Add(new SettingsItem("FHIR3.Core.FHIRObjectConveyor", typeof(FHIRObjectConveyor).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR3.Core.GenerateTypesNode", typeof(GenerateTypesNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR3.Core.FHIRAsJSONSelectorNode", typeof(FHIRAsJSONSelectorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR3.Core.FHIRAsJSONReadAccessorNode", typeof(FHIRAsJSONReadAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR3.Core.FHIRAsJSONWriteAccessorNode", typeof(FHIRAsJSONWriteAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR3.Core.FHIRAsXMLSelectorNode", typeof(FHIRAsXMLSelectorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR3.Core.FHIRAsXMLReadAccessorNode", typeof(FHIRAsXMLReadAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR3.Core.FHIRAsXMLWriteAccessorNode", typeof(FHIRAsXMLWriteAccessorNode).AssemblyQualifiedName));

			classes.Add(new SettingsItem("FHIR3.Core.SatisfiesSearchParamNode", typeof(SatisfiesSearchParamNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR3.Core.CurrentNode", typeof(NilaryCurrentNode).AssemblyQualifiedName));

			return classes;
		}
	}
}