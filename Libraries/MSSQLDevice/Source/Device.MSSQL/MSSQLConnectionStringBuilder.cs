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
			FParameters.AddOrUpdate("Provider", "SQLOLEDB");
#else
		FParameters.AddOrUpdate("Provider", "MSDASQL");
#endif

#if !USECONNECTIONPOOLING
		FParameters.AddOrUpdate("OLE DB Services", "-2"); // Turn off OLEDB resource pooling
#endif
			FLegend.AddOrUpdate("ServerName", "Data source");
			FLegend.AddOrUpdate("DatabaseName", "initial catalog");
			FLegend.AddOrUpdate("UserName", "user id");
			FLegend.AddOrUpdate("Password", "password");
			FLegend.AddOrUpdate("ApplicationName", "app name");
		}

		public override Tags Map(Tags ATags)
		{
			Tags LTags = base.Map(ATags);
			Tag LTag = LTags.GetTag("IntegratedSecurity");
			if (LTag != Tag.None)
			{
				LTags.Remove(LTag);
				LTags.AddOrUpdate("Integrated Security", "SSPI");
			}
			return LTags;
		}
	}

	public class MSSQLADODotNetConnectionStringBuilder : ConnectionStringBuilder
	{
		public MSSQLADODotNetConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("ServerName", "server");
			FLegend.AddOrUpdate("DatabaseName", "database");
			FLegend.AddOrUpdate("UserName", "user id");
			FLegend.AddOrUpdate("Password", "password");
			FLegend.AddOrUpdate("IntegratedSecurity", "integrated security");
			FLegend.AddOrUpdate("ApplicationName", "application name");
		}
	}

	public class MSSQLODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public MSSQLODBCConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("ServerName", "DSN");
			FLegend.AddOrUpdate("DatabaseName", "Database");
			FLegend.AddOrUpdate("UserName", "UID");
			FLegend.AddOrUpdate("Password", "PWD");
			FLegend.AddOrUpdate("ApplicationName", "APPNAME");
		}

		public override Tags Map(Tags ATags)
		{
			Tags LTags = base.Map(ATags);
			Tag LTag = LTags.GetTag("IntegratedSecurity");
			if (LTag != Tag.None)
			{
				LTags.Remove(LTag.Name);
				LTags.AddOrUpdate("Trusted_Connection", "Yes");
			}
			return LTags;
		}
	}

	public class AccessConnectionStringBuilder : ConnectionStringBuilder
	{
		public override Tags Map(Tags ATags)
		{
			Tags LTags = base.Map(ATags);
			return LTags;
		}
	}
}