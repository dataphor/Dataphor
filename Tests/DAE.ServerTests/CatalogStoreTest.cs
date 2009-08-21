﻿/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Alphora.Dataphor.DAE.ServerTests.Utilities;
using NUnit.Framework;

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Language.D4;

/*
create table SQLCETiming from
	GetStoreCounters()
		adorn 
		{ 
			Operation tags { Storage.Length = '200' },
			TableName tags { Storage.Length = '8000' },
			IndexName tags { Storage.Length = '8000' }
		};

create table SQLiteTiming from
	GetStoreCounters()
		adorn 
		{ 
			Operation tags { Storage.Length = '200' },
			TableName tags { Storage.Length = '8000' },
			IndexName tags { Storage.Length = '8000' }
		};

create table MSSQLTiming from
	GetStoreCounters()
		adorn 
		{ 
			Operation tags { Storage.Length = '200' },
			TableName tags { Storage.Length = '8000' },
			IndexName tags { Storage.Length = '8000' }
		};

select 
	(SQLCETiming group by { Operation } add { Count() SQLCECount, Sum(Duration) SQLCESumDuration, Avg(Duration) SQLCEAvgDuration })
		right join (SQLiteTiming group by { Operation } add { Count() SQLiteCount, Sum(Duration) SQLiteSumDuration, Avg(Duration) SQLiteAvgDuration })
		right join (MSSQLTiming group by { Operation } add { Count() MSSQLCount, Sum(Duration) MSSQLSumDuration, Avg(Duration) MSSQLAvgDuration })
*/

namespace Alphora.Dataphor.DAE.ServerTests
{
	[TestFixture]
	public class CatalogStoreTest
	{
		private void CatalogRegressionTest(ServerConfigurationManager AServerConfigurationManager)
		{
			// Create a test configuration
			ServerConfiguration LTestConfiguration = AServerConfigurationManager.GetTestConfiguration("TestInstance");

			// Reset the instance
			AServerConfigurationManager.ResetInstance();
				
			// Start a server based on the StoreRegression instance
			Server.Server LServer = AServerConfigurationManager.GetServer();
			LServer.Start();
			
			// Stop the server
			LServer.Stop();
			
			// Start the server
			LServer.Start();
			
			// Stop the server
			LServer.Stop();
			
			// Reset the instance
			AServerConfigurationManager.ResetInstance();
		}
		
		[Test]
		public void CatalogRegressionTest()
		{
			CatalogRegressionTest(new SQLCEServerConfigurationManager());
			CatalogRegressionTest(new SQLiteServerConfigurationManager());
			CatalogRegressionTest(new MSSQLServerConfigurationManager());
		}
	}
}