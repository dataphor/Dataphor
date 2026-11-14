/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	[TestFixture]
	public class CatalogStoreTest
	{
		[Test]
		public void SQLiteCatalogRegressionTest()
		{
			// A server on a fresh instance, with a SQLite catalog store
			using (TestServerInstance LInstance = new TestServerInstance("CatalogStoreTest"))
			{
				Server LServer = LInstance.Server;
				LServer.Start();
				LServer.Stop();

				// The second start reopens the catalog the first one created
				LServer.Start();
				LServer.Stop();
			}
		}
	}
}
