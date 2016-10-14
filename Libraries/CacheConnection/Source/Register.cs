/*
	Dataphor
	© Copyright 2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.Cache
{
	using System;
	using System.Reflection;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;

	class DAERegister
	{
		protected const string D4ClassDefinitionNameSpace = "CacheConnection";

		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			
			classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, "CacheConnection"), typeof(CacheConnection).AssemblyQualifiedName));
			classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, "CacheConnectionStringBuilder"), typeof(CacheConnectionStringBuilder).AssemblyQualifiedName));
			
			return classes;
		}
	}
}