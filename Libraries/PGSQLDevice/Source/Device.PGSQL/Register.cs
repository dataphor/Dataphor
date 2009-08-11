/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Object=Alphora.Dataphor.DAE.Schema.Object;

namespace Alphora.Dataphor.DAE.Device.PGSQL
{
	internal class DAERegister
	{
		protected const string CD4ClassDefinitionNameSpace = "PostgreSQLDevice";

		public static SettingsList GetClasses()
		{
			var LClasses = new SettingsList();

			Type[] LTypes = typeof (DAERegister).Assembly.GetTypes();

			foreach (Type LType in LTypes)
			{
				// Nodes
				if (LType.IsSubclassOf(typeof (InstructionNodeBase)))
					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Object.Unqualify(LType.Name)),
					                              LType.AssemblyQualifiedName));

				// Devices
				if (LType.IsSubclassOf(typeof (Schema.Device)))
					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Object.Unqualify(LType.Name)),
					                              LType.AssemblyQualifiedName));

				// Conveyors
				if (LType.IsSubclassOf(typeof (Conveyor)))
					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Object.Unqualify(LType.Name)),
					                              LType.AssemblyQualifiedName));

				// DeviceOperator
				if (LType.IsSubclassOf(typeof (DeviceOperator)))
					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Object.Unqualify(LType.Name)),
					                              LType.AssemblyQualifiedName));

				// DeviceScalarType
				if (LType.IsSubclassOf(typeof (DeviceScalarType)))
					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Object.Unqualify(LType.Name)),
					                              LType.AssemblyQualifiedName));

				// ConnectionStringBuilder
				if (LType.IsSubclassOf(typeof (ConnectionStringBuilder)))
					LClasses.Add(new SettingsItem(String.Format("{0}.{1}", CD4ClassDefinitionNameSpace, Object.Unqualify(LType.Name)),
					                              LType.AssemblyQualifiedName));
			}

			return LClasses;
		}
	}
}