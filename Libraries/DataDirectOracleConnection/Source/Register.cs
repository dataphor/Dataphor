/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Connection.DataDirect.Oracle
{
	using System;
	using System.Reflection;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;

	class DAERegister
	{
		public static SettingsList GetClasses()
		{
			SettingsList classes = new SettingsList();
			
			classes.Add(new SettingsItem("Alphora.Dataphor.DAE.Connection.DataDirect.Oracle.OracleConnection,Alphora.Dataphor.DAE.Connection.DataDirect.Oracle", typeof(OracleConnection).AssemblyQualifiedName));
			
			return classes;
		}
	}
}