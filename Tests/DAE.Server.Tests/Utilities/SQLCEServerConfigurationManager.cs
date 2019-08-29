/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace Alphora.Dataphor.DAE.Server.Tests.Utilities
{
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.Windows;

	public class SQLCEServerConfigurationManager: ServerConfigurationManager
	{
        public override ServerConfiguration GetTestConfiguration(string AInstanceName)
        {
            ServerConfiguration LResult = base.GetTestConfiguration(AInstanceName);

            LResult.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore,Alphora.Dataphor.DAE.SQLCE";
            LResult.CatalogStoreConnectionString = String.Format("Data Source={0};Password={1};Mode={2}", "%CatalogPath%" + Path.DirectorySeparatorChar + "DAECatalog.sdf", String.Empty, "Read Write");

            return LResult;
        }
    }
}
