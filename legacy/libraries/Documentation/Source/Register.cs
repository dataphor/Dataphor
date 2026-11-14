/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace DocSamples
{
	using System;
	using System.Reflection;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;
	
	class DAERegister
	{
		protected const string D4ClassDefinitionNameSpace = "DocSamples";

		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			
			classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, "DocOperator"), typeof(DocOperator).AssemblyQualifiedName));
			classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, "DocLibrary"), typeof(DocLibrary).AssemblyQualifiedName));
			// register ObjectMetaDataNode for testing/proof of concept, todo: move to System later
			classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, "ObjectMetaDataNode"), typeof(ObjectMetaDataNode).AssemblyQualifiedName));
			
			return classes;
		}
	}
}