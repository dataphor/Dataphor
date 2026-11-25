/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.SQLRecorder
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
				if (type.IsSubclassOf(typeof(PlanNode)))
					classes.Add(new SettingsItem(String.Format("{0},{1}", type.FullName, AssemblyNameUtility.GetName(type.Assembly.FullName)), type.AssemblyQualifiedName));

			// The connection class is named by a create device statement's ConnectionClass attribute, which carries a
			// short alias rather than a type name, and the loop above only picks up plan nodes - so this one entry is
			// explicit, as the Connection library's own connection classes are.
			classes.Add(new SettingsItem(RecordingConnection.ClassName, typeof(RecordingConnection).AssemblyQualifiedName));

			return classes;
		}
	}
}
