/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Resources;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.Oracle;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Device.Oracle
{
	public class OracleOLEDBConnectionStringBuilder : ConnectionStringBuilder
	{
		public OracleOLEDBConnectionStringBuilder()
		{
			FParameters.AddOrUpdate("Provider", "MSDAORA");
			FLegend.AddOrUpdate("HostName", "Data source");
			FLegend.AddOrUpdate("UserName", "user id");
			FLegend.AddOrUpdate("Password", "password");
		}
	}

	// for the Oracle.NET data provider
	public class OracleConnectionStringBuilder : ConnectionStringBuilder
	{
		public OracleConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("HostName", "Data Source");
			FLegend.AddOrUpdate("UserName", "User Id");
		}
	}

	public class OracleODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public OracleODBCConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("HostName", "DSN");
			FLegend.AddOrUpdate("UserName", "UID");
			FLegend.AddOrUpdate("Password", "PWD");
		}
	}

}