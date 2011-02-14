/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USECONNECTIONPOOLING
#define USESQLOLEDB
//#define USEOLEDBCONNECTION
//#define USEADOCONNECTION

using Alphora.Dataphor.DAE.Connection;

using Alphora.Dataphor.DAE.Language.D4;


namespace Alphora.Dataphor.DAE.Device.MSSQL
{
	/// <summary>
	/// This class is the tag translator for ADO
	/// </summary>
	public class MSSQLOLEDBConnectionStringBuilder : ConnectionStringBuilder
	{
		public MSSQLOLEDBConnectionStringBuilder()
		{
#if USESQLOLEDB
			_parameters.AddOrUpdate("Provider", "SQLOLEDB");
#else
		FParameters.AddOrUpdate("Provider", "MSDASQL");
#endif

#if !USECONNECTIONPOOLING
		FParameters.AddOrUpdate("OLE DB Services", "-2"); // Turn off OLEDB resource pooling
#endif
			_legend.AddOrUpdate("ServerName", "Data source");
			_legend.AddOrUpdate("DatabaseName", "initial catalog");
			_legend.AddOrUpdate("UserName", "user id");
			_legend.AddOrUpdate("Password", "password");
			_legend.AddOrUpdate("ApplicationName", "app name");
		}

		public override Tags Map(Tags tags)
		{
			Tags localTags = base.Map(tags);
			Tag tag = localTags.GetTag("IntegratedSecurity");
			if (tag != Tag.None)
			{
				localTags.Remove(tag);
				localTags.AddOrUpdate("Integrated Security", "SSPI");
			}
			return localTags;
		}
	}

	public class MSSQLADODotNetConnectionStringBuilder : ConnectionStringBuilder
	{
		public MSSQLADODotNetConnectionStringBuilder()
		{
			_legend.AddOrUpdate("ServerName", "server");
			_legend.AddOrUpdate("DatabaseName", "database");
			_legend.AddOrUpdate("UserName", "user id");
			_legend.AddOrUpdate("Password", "password");
			_legend.AddOrUpdate("IntegratedSecurity", "integrated security");
			_legend.AddOrUpdate("ApplicationName", "application name");
		}
	}

	public class MSSQLODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public MSSQLODBCConnectionStringBuilder()
		{
			_legend.AddOrUpdate("ServerName", "DSN");
			_legend.AddOrUpdate("DatabaseName", "Database");
			_legend.AddOrUpdate("UserName", "UID");
			_legend.AddOrUpdate("Password", "PWD");
			_legend.AddOrUpdate("ApplicationName", "APPNAME");
		}

		public override Tags Map(Tags tags)
		{
			Tags localTags = base.Map(tags);
			Tag tag = localTags.GetTag("IntegratedSecurity");
			if (tag != Tag.None)
			{
				localTags.Remove(tag.Name);
				localTags.AddOrUpdate("Trusted_Connection", "Yes");
			}
			return localTags;
		}
	}

	public class AccessConnectionStringBuilder : ConnectionStringBuilder
	{
		public override Tags Map(Tags tags)
		{
			Tags localTags = base.Map(tags);
			return localTags;
		}
	}
}