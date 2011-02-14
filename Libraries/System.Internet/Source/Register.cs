/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Reflection;
	
using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Schema = Alphora.Dataphor.DAE.Schema;

[assembly: Alphora.Dataphor.DAE.Server.DAERegister("Alphora.Dataphor.Libraries.System.Internet.DAERegister")]

namespace Alphora.Dataphor.Libraries.System.Internet
{
	class DAERegister
	{
		public const string D4ClassDefinitionNameSpace = "System.Internet";

		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			
			Type[] types = typeof(DAERegister).Assembly.GetTypes();

			foreach (Type type in types)
			{
				// Nodes
				if (type.IsSubclassOf(typeof(PlanNode)))
					classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, Schema.Object.Unqualify(type.Name)), type.AssemblyQualifiedName));
				
				// Devices
//				if (LType.IsSubclassOf(typeof(Schema.Device)))
//					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Schema.Object.Unqualify(LType.Name)), LType.AssemblyQualifiedName));
//				
//				// Conveyors
//				if (LType.IsSubclassOf(typeof(Conveyor)))
//					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Schema.Object.Unqualify(LType.Name)), LType.AssemblyQualifiedName));
//				
//				// DeviceOperator
//				if (LType.IsSubclassOf(typeof(Schema.DeviceOperator)))
//					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Schema.Object.Unqualify(LType.Name)), LType.AssemblyQualifiedName));
//					
//				// DeviceScalarType
//				if (LType.IsSubclassOf(typeof(Schema.DeviceScalarType)))
//					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Schema.Object.Unqualify(LType.Name)), LType.AssemblyQualifiedName));
			}
			
			return classes;
		}
	}
}