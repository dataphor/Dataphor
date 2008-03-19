/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection
{
	using System;
	using System.Reflection;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;

	class DAERegister
	{
		protected const string CD4ClassDefinitionNameSpace = "Connection";

		public static SettingsList GetClasses()
		{
			SettingsList LClasses = new SettingsList();
			
			LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, "MSSQLConnection"), typeof(MSSQLConnection).AssemblyQualifiedName));
			LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, "OLEDBConnection"), typeof(OLEDBConnection).AssemblyQualifiedName));
			LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, "SQLCEConnection"), typeof(SQLCEConnection).AssemblyQualifiedName));
			LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, "ClearAllPoolsNode"), typeof(ClearAllPoolsNode).AssemblyQualifiedName));
			
			return LClasses;
		}
	}
}