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
			_parameters.AddOrUpdate("Provider", "MSDAORA");
			_legend.AddOrUpdate("HostName", "Data source");
			_legend.AddOrUpdate("UserName", "user id");
			_legend.AddOrUpdate("Password", "password");
		}
	}

	// for the Oracle.NET data provider
	public class OracleConnectionStringBuilder : ConnectionStringBuilder
	{
		public OracleConnectionStringBuilder()
		{
			_legend.AddOrUpdate("HostName", "Data Source");
			_legend.AddOrUpdate("UserName", "User Id");
		}
	}

	public class OracleODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public OracleODBCConnectionStringBuilder()
		{
			_legend.AddOrUpdate("HostName", "DSN");
			_legend.AddOrUpdate("UserName", "UID");
			_legend.AddOrUpdate("Password", "PWD");
		}
	}

}