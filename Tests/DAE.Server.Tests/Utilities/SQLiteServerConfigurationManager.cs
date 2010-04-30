/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Alphora.Dataphor.DAE.Server.Tests.Utilities
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.Windows;

	class SQLiteServerConfigurationManager : ServerConfigurationManager
	{
		public override ServerConfiguration GetTestConfiguration(string AInstanceName)
		{
			ServerConfiguration LResult = base.GetTestConfiguration(AInstanceName);
			
			LResult.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite";
			LResult.CatalogStoreConnectionString = @"Data Source=%CatalogPath%\DAECatalog";		

			return LResult;
		}
	}
}
