/*
	Dataphor
	© Copyright 2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Connection.Cache
{
	public class CacheConnectionStringBuilder : ConnectionStringBuilder
	{
		public const string CDefaultPort = "1972";

		public CacheConnectionStringBuilder()
		{
			_legend.AddOrUpdate("ServerName", "Server");
			_legend.AddOrUpdate("DatabaseName", "Namespace");
			_legend.AddOrUpdate("UserName", "User ID");
			_legend.AddOrUpdate("Password", "Password");
		}

		private string _port = CDefaultPort;
		public string Port
		{
			get { return _port; }
			set { _port = value; }
		}

		public override Tags Map(Tags tags)
		{
			Tags localTags = base.Map(tags);
			localTags.AddOrUpdate("Port", Port);
			return localTags;
		}
	}
}
