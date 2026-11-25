/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data.Common;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Store.MSSQL;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What the SQL Server catalog store does to the connection string a deployment gave it, before it connects to
	/// anything. No server: the store's initialization only rewrites the string when it has been told not to make the
	/// database, and that rewriting is the whole behaviour here.
	/// </summary>
	/// <remarks>
	/// The forced MultipleActiveResultSets option is store-mssql-port's and is asserted here because the two rewrites
	/// share one method and one order; the Encrypt default is device-mssql-port's, where the client moved from
	/// System.Data.SqlClient to Microsoft.Data.SqlClient, which encrypts by default and then validates the server's
	/// certificate. The second rewrite is a *default* rather than a forced value, and that difference is the thing worth
	/// holding: a deployment that has said how its catalog connection is protected has said it, and the store must not
	/// quietly say otherwise - which is why the two cases below that set one of the settings exist at all. The rest of
	/// what this store does needs a server and is in MSSQLStoreContractTests.
	///
	/// Every assertion reads the result back through DbConnectionStringBuilder rather than looking for text: that class
	/// lower-cases every keyword it parsed out of the supplied string while leaving the ones the store added alone, so
	/// the same setting appears under either spelling depending on who put it there.
	/// </remarks>
	[Parallelizable(ParallelScope.Self)]
	[TestFixture]
	public class MSSQLStoreConnectionStringTest
	{
		/// <summary>A server and database no connection is ever opened to; nothing in this fixture connects.</summary>
		private const string DeploymentConnectionString = "Data Source=no.such.server.invalid;Initial Catalog=DAECatalog;Integrated Security=SSPI";

		/// <summary>The connection string of an initialized store, with the database probe off so that initialization contacts nothing.</summary>
		private static DbConnectionStringBuilder InitializedSettings(string connectionString)
		{
			MSSQLStore store = new MSSQLStore();
			store.ShouldEnsureDatabase = false;
			store.ConnectionString = connectionString;
			store.Initialize();

			DbConnectionStringBuilder settings = new DbConnectionStringBuilder();
			settings.ConnectionString = store.ConnectionString;
			return settings;
		}

		[Test]
		public void AConnectionStringThatSaysNothingAboutEncryptionGetsTheOldClientsBehaviour()
		{
			Assert.That
			(
				InitializedSettings(DeploymentConnectionString)["Encrypt"],
				Is.EqualTo("False"),
				"A catalogstoreconnectionstring written for release 3 says nothing about encryption, and must still reach a SQL Server whose certificate is self-signed or absent."
			);
		}

		[Test]
		public void AConnectionStringThatAsksForEncryptionKeepsIt()
		{
			Assert.That
			(
				InitializedSettings(DeploymentConnectionString + ";Encrypt=True")["Encrypt"],
				Is.EqualTo("True"),
				"The store overrode a deployment's own choice of how the catalog connection is protected."
			);
		}

		[Test]
		public void AConnectionStringThatTrustsTheServerCertificateIsLeftAlone()
		{
			Assert.That
			(
				InitializedSettings(DeploymentConnectionString + ";TrustServerCertificate=True").ContainsKey("Encrypt"),
				Is.False,
				"TrustServerCertificate is the other way to let an untrusted certificate through, so a deployment that set it has already chosen; defaulting Encrypt beside it would contradict that choice."
			);
		}

		/// <summary>
		/// The option the catalog cannot work without, which is forced rather than defaulted: the catalog opens a second
		/// store cursor while a first is still open, and SQL Server refuses that on a connection that did not ask for
		/// multiple active result sets. See the comment at the site for the error a deployment would otherwise see.
		/// </summary>
		[Test]
		public void MultipleActiveResultSetsIsForcedOverADeploymentsFalse()
		{
			Assert.That(InitializedSettings(DeploymentConnectionString + ";MultipleActiveResultSets=False")["MultipleActiveResultSets"], Is.EqualTo("True"));
		}
	}
}
