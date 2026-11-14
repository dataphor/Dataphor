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

namespace Alphora.Dataphor.CQF.Model
{
	class DAERegister
	{
		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			
			Type[] types = typeof(GenerateTypesNode).Assembly.GetTypes();

			foreach (Type type in types)
			{
				if (GenerateTypesNode.ShouldGenerateType(type))
					classes.Add(new SettingsItem(type.FullName, type.AssemblyQualifiedName));
			}

			classes.Add(new SettingsItem("CQF.Model.ModelObjectConveyor", typeof(ModelObjectConveyor).AssemblyQualifiedName));
			classes.Add(new SettingsItem("CQF.Model.GenerateTypesNode", typeof(GenerateTypesNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("CQF.Model.ModelAsXMLSelectorNode", typeof(ModelAsXMLSelectorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("CQF.Model.ModelAsXMLReadAccessorNode", typeof(ModelAsXMLReadAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("CQF.Model.ModelAsXMLWriteAccessorNode", typeof(ModelAsXMLWriteAccessorNode).AssemblyQualifiedName));
			classes.Add(new SettingsItem("CQF.Model.ModelEqualNode", typeof(ModelEqualNode).AssemblyQualifiedName));

			return classes;
		}
	}
}