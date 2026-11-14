/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Device.Catalog;

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

		[Test]
		public void DefaultStoreIsSQLite()
		{
			using (TestServerInstance instance = new TestServerInstance("DefaultStoreIsSQLite"))
			{
				Server server = instance.Server;
				server.CatalogStoreClassName = null;
				server.CatalogStoreConnectionString = null;
				server.Start();

				Assert.That(Evaluate(instance, "1 + 1"), Is.EqualTo(2));
				Assert.That(File.Exists(CatalogFileName(instance)), Is.True);
			}
		}

		[TestCase("Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore,Alphora.Dataphor.DAE.SQLCE", null)]
		[TestCase("alphora.dataphor.dae.store.sqlce.sqlcestore, Alphora.Dataphor.DAE.SQLCE", null)]
		[TestCase(null, @"Data Source=%CatalogPath%\DAECatalog.sdf;Password=;Mode=Read Write")]
		public void SQLCESettingsAreRejected(string className, string connectionString)
		{
			using (TestServerInstance instance = new TestServerInstance("SQLCESettingsAreRejected"))
			{
				Server server = instance.Server;
				server.CatalogStoreClassName = className;
				server.CatalogStoreConnectionString = connectionString;

				Exception exception = Assert.Catch(() => server.Start());
				Assert.That(FindCatalogErrorCode(exception), Is.EqualTo((int)CatalogException.Codes.SQLCEStoreNotSupported), exception.ToString());
				Assert.That(server.State, Is.EqualTo(ServerState.Stopped));
				Assert.That(File.Exists(CatalogFileName(instance)), Is.False);

				// Once the settings are removed, the same instance starts on the default store
				server.CatalogStoreClassName = null;
				server.CatalogStoreConnectionString = null;
				server.Start();
				Assert.That(File.Exists(CatalogFileName(instance)), Is.True);
			}
		}

		private static string CatalogFileName(TestServerInstance instance)
		{
			return Path.Combine(instance.Directory, TestServerInstance.InstanceFolderName, Server.DefaultCatalogDirectory, "DAECatalog.sqlite");
		}

		private static int? FindCatalogErrorCode(Exception exception)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is CatalogException)
					return ((CatalogException)exception).Code;
			return null;
		}

		private static object Evaluate(TestServerInstance instance, string expression)
		{
			IServerSession session = instance.Connect();
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					return ((ServerProcess)process).Evaluate(expression, null).AsNative;
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				instance.Server.Disconnect(session);
			}
		}
	}
}
