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
		protected const string CD4ClassDefinitionNameSpace = "DocSamples";

		public static SettingsList GetClasses()
		{
			SettingsList LClasses = new SettingsList();
			
			LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, "DocOperator"), typeof(DocOperator).AssemblyQualifiedName));
			LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, "DocLibrary"), typeof(DocLibrary).AssemblyQualifiedName));
			// register ObjectMetaDataNode for testing/proof of concept, todo: move to System later
			LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, "ObjectMetaDataNode"), typeof(ObjectMetaDataNode).AssemblyQualifiedName));
			
			return LClasses;
		}
	}
}