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
		/// <remarks>
		/// Microsoft.Data.SqlClient, which replaced System.Data.SqlClient in release 4 (device-mssql-port), defaults Encrypt
		/// to on and then validates the server's certificate; System.Data.SqlClient did neither. So a device definition that
		/// named a server and a database and nothing else - which is most of them - would stop connecting to a SQL Server with
		/// a self-signed or absent certificate, which is the ordinary state of an internal one. Encrypt=False rather than
		/// TrustServerCertificate=True because the two are not the same thing and only the first is what 3.1 did: the second
		/// encrypts and accepts any certificate, which is a *new* behaviour, and choosing it would quietly change what every
		/// existing deployment negotiates. A deployment that wants encryption sets it on the device or the device session
		/// rather than losing it here: 'ConnectionParameters' = 'Encrypt=True' - or 'Encrypt=Strict', or
		/// 'Encrypt=True;TrustServerCertificate=True' to encrypt without checking the certificate - is applied by
		/// SQLDevice.GetConnectionParameters after this builder has run, so the device's own setting wins. Setting
		/// TrustServerCertificate alone changes nothing: the Encrypt=False below still stands beside it, and an
		/// unencrypted connection presents no certificate to trust.
		/// </remarks>
		public MSSQLADODotNetConnectionStringBuilder()
		{
			_parameters.AddOrUpdate("Encrypt", "False");
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