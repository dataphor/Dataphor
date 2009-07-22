/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESQLCONNECTION

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Connection.PGSQL;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Store.PGSQL
{
    public class PostgreSQLStore : SQLStore
	{
		/// <summary>Initializes the store, ensuring that an instance of the server is running and a database is attached.</summary>
        protected override void InternalInitialize()
        {
            FSupportsMARS = false;
            FSupportsUpdatableCursor = false;
        }
		
		private bool FShouldEnsureDatabase = true;
		public bool ShouldEnsureDatabase
		{
			get { return FShouldEnsureDatabase; }
			set { FShouldEnsureDatabase = value; }
		}

	    public override SQLConnection GetSQLConnection()
		{
            return new PostgreSQLConnection(ConnectionString);
		}

		protected override SQLStoreConnection InternalConnect()
		{
            return new PostgreSQLStoreConnection(this);
		}
	}
}
