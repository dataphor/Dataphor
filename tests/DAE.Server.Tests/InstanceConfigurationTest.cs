/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The machine instance list (ServerConfigurations.config, written by BOP) is an upgrade surface: an operator upgrading to 4.0
	/// keeps the file an earlier release wrote, and settings have been removed from <see cref="ServerConfiguration"/> since
	/// (UseServiceConfiguration and AllowSilverlightClients, by protocol-corewcf-host-prepare) and will be again. This pins the
	/// claim that removing a setting does not cost the operator the file: BOP records an attribute with no matching property as a
	/// deserialization error and carries on, InstanceConfiguration.Load never looks at that error list, and every other setting on
	/// the instance is still read.
	/// </summary>
	[TestFixture]
	public class InstanceConfigurationTest
	{
		/// <summary>
		/// An instance element as an earlier release wrote it, plus one attribute no release ever wrote: the claim is about any
		/// attribute the current class does not declare, not about the two that were just removed.
		/// </summary>
		private const string LegacyInstanceConfiguration =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<instanceconfiguration xmlns:bop=""www.alphora.com/schemas/bop"" xmlns=""Alphora.Dataphor.DAE.Server,AlphoraDataphorServer"">
	<serverconfiguration allowsilverlightclients=""False"" catalogstoreclassname=""Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.Store.SQLite"" catalogstoreconnectionstring="""" instancedirectory=""C:\Legacy\Instances"" librarydirectories=""C:\Legacy\Libraries;C:\Legacy\More"" name=""Legacy"" neverexisted=""Flurb"" overridelistenerportnumber=""8075"" portnumber=""8061"" requiresecureconnection=""True"" requiresecurelistenerconnection=""True"" shouldlisten=""False"" useserviceconfiguration=""True"" />
</instanceconfiguration>
";

		[Test]
		public void LoadReadsAFileCarryingSettingsThatNoLongerExist()
		{
			string directory = TestEnvironment.NewDirectory(nameof(LoadReadsAFileCarryingSettingsThatNoLongerExist));
			try
			{
				string fileName = Path.Combine(directory, InstanceManager.InstanceConfigurationFileName);
				File.WriteAllText(fileName, LegacyInstanceConfiguration);

				InstanceConfiguration configuration = InstanceConfiguration.Load(fileName);

				Assert.That(configuration.Instances.HasInstance("Legacy"), Is.True, "the instance in the file was not loaded");
				ServerConfiguration instance = configuration.Instances.GetInstance("Legacy");
				Assert.That(instance.Name, Is.EqualTo("Legacy"));
				Assert.That(instance.PortNumber, Is.EqualTo(8061));
				Assert.That(instance.ShouldListen, Is.False);
				Assert.That(instance.OverrideListenerPortNumber, Is.EqualTo(8075));
				Assert.That(instance.RequireSecureConnection, Is.True);
				Assert.That(instance.RequireSecureListenerConnection, Is.True);
				Assert.That(instance.LibraryDirectories, Is.EqualTo(@"C:\Legacy\Libraries;C:\Legacy\More"));
				Assert.That(instance.InstanceDirectory, Is.EqualTo(@"C:\Legacy\Instances"));
				Assert.That(instance.CatalogStoreClassName, Is.EqualTo("Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.Store.SQLite"));
				Assert.That(instance.CatalogStoreConnectionString, Is.Empty);
			}
			finally
			{
				if (!TestEnvironment.KeepFiles)
					Directory.Delete(directory, true);
			}
		}
	}
}
