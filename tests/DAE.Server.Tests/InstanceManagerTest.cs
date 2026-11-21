/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Where <see cref="InstanceManager"/> looks for this machine's instance list (ServerConfigurations.config): that the
	/// folder carries the major version, so that a 4.0 installation does not read and write a 3.1 installation's list
	/// (known defect K27, and the reason the two can be installed side by side), that an instance's own data folder -
	/// catalog, logs and library data - defaults under the same folder, and that a process can point the whole thing
	/// elsewhere. The redirection is what lets a test or an operator start a server without touching machine state:
	/// DataphorServiceHost.Start() loads the list it resolves and, when the named instance is not in it, adds a default
	/// entry and saves the file.
	/// </summary>
	/// <remarks>
	/// What the file itself does with settings a release has added or removed is InstanceConfigurationTest's subject,
	/// not this fixture's; here the file is only written and read back to show that the redirection reaches it rather
	/// than only the resolved string.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: InstanceManager is static, so each case here changes process-wide state - the override, the
	// environment variable and the loaded list - that another fixture's server would otherwise see mid-run.
	public class InstanceManagerTest
	{
		private const string InstanceName = "InstanceManagerTest";

		/// <summary>Whatever the machine or the build agent had in the environment variable, so that teardown restores it exactly.</summary>
		private string _savedVariable;

		[SetUp]
		public void SaveTheRedirection()
		{
			_savedVariable = Environment.GetEnvironmentVariable(InstanceManager.InstanceDirectoryVariableName);
		}

		[TearDown]
		public void RestoreTheRedirection()
		{
			// The property is cleared rather than put back: its getter resolves the whole order rather than reporting what
			// was set, so there is no prior value to save, and nothing else in this assembly sets it.
			InstanceManager.InstanceDirectory = null;
			Environment.SetEnvironmentVariable(InstanceManager.InstanceDirectoryVariableName, _savedVariable);
		}

		[Test]
		public void TheDefaultInstanceDirectoryIsVersioned()
		{
			// The case states its own preconditions: a developer's machine or a build agent may carry the variable.
			Environment.SetEnvironmentVariable(InstanceManager.InstanceDirectoryVariableName, null);
			InstanceManager.InstanceDirectory = null;

			string commonApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string majorVersion = typeof(PathUtility).Assembly.GetName().Version.Major.ToString();
			string versioned = Path.Combine(commonApplicationData, "Alphora", "Dataphor", majorVersion, Server.DefaultInstanceDirectory);
			string unversioned = Path.Combine(commonApplicationData, "Alphora", "Dataphor", Server.DefaultInstanceDirectory);

			// Read before resolving, because the folder is already there on a machine with 4.0 installed: the claim is that
			// resolving the path does not create it, not that it is absent.
			bool existedBeforeResolution = Directory.Exists(versioned);

			string resolved = InstanceManager.InstanceDirectory;

			Assert.That(resolved, Is.EqualTo(versioned));
			Assert.That(resolved, Is.Not.EqualTo(unversioned), "the instance folder carries no version, so a 4.0 server reads and writes a 3.1 server's instance list (K27)");
			Assert.That(Directory.Exists(resolved), Is.EqualTo(existedBeforeResolution), "reading the instance directory created the folder; only GetInstanceDirectory may create it");
		}

		[Test]
		public void AnExplicitInstanceDirectoryBeatsTheEnvironmentVariable()
		{
			string fromVariable = TestEnvironment.NewDirectory("InstanceDirectoryFromVariable");
			string fromProperty = TestEnvironment.NewDirectory("InstanceDirectoryFromProperty");
			try
			{
				InstanceManager.InstanceDirectory = null;
				Environment.SetEnvironmentVariable(InstanceManager.InstanceDirectoryVariableName, fromVariable);
				Assert.That(InstanceManager.InstanceDirectory, Is.EqualTo(fromVariable), "the environment variable did not redirect the instance directory");

				InstanceManager.InstanceDirectory = fromProperty;
				Assert.That(InstanceManager.InstanceDirectory, Is.EqualTo(fromProperty), "a value set in the process must win over whatever the environment happens to carry");
			}
			finally
			{
				TestEnvironment.TryDeleteDirectory(fromVariable);
				TestEnvironment.TryDeleteDirectory(fromProperty);
			}
		}

		[Test]
		public void ASavedInstanceListIsReadBackFromTheOverriddenDirectory()
		{
			string directory = TestEnvironment.NewDirectory(nameof(ASavedInstanceListIsReadBackFromTheOverriddenDirectory));
			try
			{
				Environment.SetEnvironmentVariable(InstanceManager.InstanceDirectoryVariableName, null);
				InstanceManager.InstanceDirectory = directory;

				InstanceManager.Load();
				Assert.That(InstanceManager.Instances.HasInstance(InstanceName), Is.False, "the override points at a folder with no instance list, so nothing should have loaded");

				InstanceManager.Instances.Add(ServerConfiguration.DefaultInstance(InstanceName));
				InstanceManager.Save();

				string fileName = Path.Combine(directory, InstanceManager.InstanceConfigurationFileName);
				Assert.That(File.Exists(fileName), Is.True, "the instance list was not saved into the overridden folder");

				// Loaded again rather than read from the list still in memory: what is being shown is that the redirection
				// reaches the file, which is what an out-of-process test and an operator trying a configuration depend on.
				InstanceManager.Load();
				Assert.That(InstanceManager.Instances.HasInstance(InstanceName), Is.True, "the instance saved into the overridden folder was not read back from it");
				Assert.That(InstanceManager.Instances.GetInstance(InstanceName).PortNumber, Is.EqualTo(ServerConfiguration.DefaultPortNumber));
			}
			finally
			{
				TestEnvironment.TryDeleteDirectory(directory);
			}
		}

		[Test]
		public void AnInstanceWithNoDirectoryOfItsOwnLandsUnderTheResolvedInstanceDirectory()
		{
			string directory = TestEnvironment.NewDirectory(nameof(AnInstanceWithNoDirectoryOfItsOwnLandsUnderTheResolvedInstanceDirectory));
			try
			{
				Environment.SetEnvironmentVariable(InstanceManager.InstanceDirectoryVariableName, null);
				InstanceManager.InstanceDirectory = directory;

				// InstanceDirectory left unset, which is what ServerConfiguration.DefaultInstance leaves it as, so the server
				// defaults it. This is the second half of K27 and the consequential one: the default is what pointed a 4.0
				// server's catalog, logs and library data at a 3.1 installation's folders, and redirecting the list alone would
				// be worth little if an instance's own data did not follow it.
				ServerConfiguration configuration =
					new ServerConfiguration
					{
						Name = InstanceName,
						LibraryDirectories = Directory.CreateDirectory(Path.Combine(directory, TestServerInstance.LibrariesFolderName)).FullName
					};

				Server server = new Server();
				configuration.ApplyTo(server);
				server.LoggingEnabled = TestEnvironment.LoggingEnabled;
				server.Start();
				try
				{
					Assert.That(server.InstanceDirectory, Is.EqualTo(Path.Combine(directory, InstanceName)));
					Assert.That(File.Exists(server.GetCatalogStoreDatabaseFileName()), Is.True, "the catalog store file is not under the instance's folder in the resolved instance directory");
				}
				finally
				{
					server.Stop();
				}
			}
			finally
			{
				TestEnvironment.TryDeleteDirectory(directory);
			}
		}
	}
}
