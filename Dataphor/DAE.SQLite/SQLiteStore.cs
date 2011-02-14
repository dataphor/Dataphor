/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Connection;

// To specify that a SQLite store be used for catalog persistence, add the following attributes to the alias definition in the ServerAliases.config file:
// catalogstoreclassname="Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite" 
// catalogstoreconnectionstring="Data Source={0}" where {0} is the file name of SQLite database

namespace Alphora.Dataphor.DAE.Store.SQLite
{
	public class SQLiteStore : SQLStore
	{
		protected override void InternalInitialize()
		{
			_supportsMARS = true;
			_supportsUpdatableCursor = false;
		}

		public override SQLConnection GetSQLConnection()
		{
			return new SQLiteConnection(ConnectionString);
		}

		protected override SQLStoreConnection InternalConnect()
		{
			return new SQLiteStoreConnection(this);
		}
	}
}
