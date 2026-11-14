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
		protected const string D4ClassDefinitionNameSpace = "PostgreSQLDevice";

		public static SettingsList GetClasses()
		{
			var classes = new SettingsList();

			Type[] types = typeof (DAERegister).Assembly.GetTypes();

			foreach (Type type in types)
			{
				// Nodes
				if (type.IsSubclassOf(typeof (InstructionNodeBase)))
					classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, Object.Unqualify(type.Name)),
					                              type.AssemblyQualifiedName));

				// Devices
				if (type.IsSubclassOf(typeof (Schema.Device)))
					classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, Object.Unqualify(type.Name)),
					                              type.AssemblyQualifiedName));

				// Conveyors
				if (type.IsSubclassOf(typeof (Conveyor)))
					classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, Object.Unqualify(type.Name)),
					                              type.AssemblyQualifiedName));

				// DeviceOperator
				if (type.IsSubclassOf(typeof (DeviceOperator)))
					classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, Object.Unqualify(type.Name)),
					                              type.AssemblyQualifiedName));

				// DeviceScalarType
				if (type.IsSubclassOf(typeof (DeviceScalarType)))
					classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, Object.Unqualify(type.Name)),
					                              type.AssemblyQualifiedName));

				// ConnectionStringBuilder
				if (type.IsSubclassOf(typeof (ConnectionStringBuilder)))
					classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, Object.Unqualify(type.Name)),
					                              type.AssemblyQualifiedName));
			}

			return classes;
		}
	}
}