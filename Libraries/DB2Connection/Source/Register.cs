/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.DB2
{
	using System;
	using System.Reflection;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;

	class DAERegister
	{
		protected const string D4ClassDefinitionNameSpace = "DB2Connection";

		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			
			classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, "DB2Connection"), typeof(DB2Connection).AssemblyQualifiedName));
			
			return classes;
		}
	}
}