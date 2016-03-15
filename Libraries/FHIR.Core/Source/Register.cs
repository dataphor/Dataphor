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

namespace Alphora.Dataphor.FHIR.Core
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

			classes.Add(new SettingsItem("FHIR.Core.FHIRObjectConveyor", typeof(FHIRObjectConveyor).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.GenerateTypesNode", typeof(GenerateTypesNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.FHIRAsJSONSelectorNode", typeof(FHIRAsJSONSelectorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.FHIRAsJSONReadAccessorNode", typeof(FHIRAsJSONReadAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.FHIRAsJSONWriteAccessorNode", typeof(FHIRAsJSONWriteAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.FHIRAsXMLSelectorNode", typeof(FHIRAsXMLSelectorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.FHIRAsXMLReadAccessorNode", typeof(FHIRAsXMLReadAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.FHIRAsXMLWriteAccessorNode", typeof(FHIRAsXMLWriteAccessorNode).AssemblyQualifiedName));

			classes.Add(new SettingsItem("FHIR.Core.SatisfiesSearchParamNode", typeof(SatisfiesSearchParamNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.CurrentNode", typeof(NilaryCurrentNode).AssemblyQualifiedName));

			classes.Add(new SettingsItem("Newtonsoft.Json.Linq.JObject", typeof(JObject).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.JSONObjectConveyor", typeof(JSONObjectConveyor).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicEqualNode", typeof(DynamicEqualNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicSelectorNode", typeof(DynamicSelectorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicContentSelectorNode", typeof(DynamicContentSelectorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicContentReadAccessorNode", typeof(DynamicContentReadAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicContentWriteAccessorNode", typeof(DynamicContentWriteAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicGetNode", typeof(DynamicGetNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicSetNode", typeof(DynamicSetNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToListNode", typeof(DynamicToListNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToBooleanNode", typeof(DynamicToBooleanNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToByteNode", typeof(DynamicToByteNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToShortNode", typeof(DynamicToShortNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToIntegerNode", typeof(DynamicToIntegerNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToLongNode", typeof(DynamicToLongNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToStringNode", typeof(DynamicToStringNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToDecimalNode", typeof(DynamicToDecimalNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToTimeSpanNode", typeof(DynamicToTimeSpanNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("FHIR.Core.DynamicToDateTimeNode", typeof(DynamicToDateTimeNode).AssemblyQualifiedName));

			return classes;
		}
	}
}