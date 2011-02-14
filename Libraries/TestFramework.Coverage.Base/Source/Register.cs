/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Diagnostics
{
	using System;
	using System.Reflection;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	class DAERegister
	{
		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			
			Type[] types = typeof(DAERegister).Assembly.GetTypes();

			foreach (Type type in types)
			{
				// Nodes
				if (type.IsSubclassOf(typeof(PlanNode)))
					classes.Add(new SettingsItem(String.Format("{0},{1}", type.FullName, AssemblyNameUtility.GetName(type.Assembly.FullName)), type.AssemblyQualifiedName));
				
				// Devices
				if (type.IsSubclassOf(typeof(Schema.Device)))
					classes.Add(new SettingsItem(String.Format("{0},{1}", type.FullName, AssemblyNameUtility.GetName(type.Assembly.FullName)), type.AssemblyQualifiedName));
				
				// Conveyors
				if (type.IsSubclassOf(typeof(Conveyor)))
					classes.Add(new SettingsItem(String.Format("{0},{1}", type.FullName, AssemblyNameUtility.GetName(type.Assembly.FullName)), type.AssemblyQualifiedName));
				
				// DeviceOperator
				if (type.IsSubclassOf(typeof(Schema.DeviceOperator)))
					classes.Add(new SettingsItem(String.Format("{0},{1}", type.FullName, AssemblyNameUtility.GetName(type.Assembly.FullName)), type.AssemblyQualifiedName));
					
				// DeviceScalarType
				if (type.IsSubclassOf(typeof(Schema.DeviceScalarType)))
					classes.Add(new SettingsItem(String.Format("{0},{1}", type.FullName, AssemblyNameUtility.GetName(type.Assembly.FullName)), type.AssemblyQualifiedName));
			}
			
			return classes;
		}
	}
}