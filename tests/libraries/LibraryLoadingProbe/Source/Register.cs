/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.Diagnostics.LibraryLoadingProbe
{
	using System;

	using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	public static class DAERegister
	{
		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			foreach (Type type in typeof(DAERegister).Assembly.GetTypes())
			{
				if (type.IsSubclassOf(typeof(PlanNode)))
					classes.Add(new SettingsItem(String.Format("{0},{1}", type.FullName, AssemblyNameUtility.GetName(type.Assembly.FullName)), type.AssemblyQualifiedName));
			}
			return classes;
		}
	}
}
