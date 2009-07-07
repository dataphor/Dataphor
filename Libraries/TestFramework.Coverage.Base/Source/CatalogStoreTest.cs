using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Diagnostics
{
	[TestFixture]
	public class CatalogStoreTest
	{
		private void CatalogRegressionTest(string ACatalogStoreType)
		{
			// Delete the instance directory
			// Delete the StoreRegressionCatalog database if this is an MSSQL store regression test
			// Configure the StoreRegression instance for the test type
			// Start a server based on the StoreRegression instance
			// Stop the server
			// Start the server
			// Stop the server
			// Delete the instance directory
			// Delete the StoreRegressionCatalog database if this is an MSSQL store regression test
		}
		
		[Test]
		public void CatalogRegressionTest()
		{
			CatalogRegressionTest("SQLCE");
			CatalogRegressionTest("SQLite");
			CatalogRegressionTest("MSSQL");
		}
	}
}
