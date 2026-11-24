/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

using Runtime = Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Bringing a library into a running server from a folder on disk: <c>AttachLibrary</c>, <c>AttachLibraries</c> and what
	/// <c>DetachLibrary</c> does to a library that arrived that way. This is the deployment step - an installer or an
	/// administrator puts an application's folder somewhere and tells the server about it - and none of it had ever run.
	/// See debt-library-attach-from-a-directory.
	/// </summary>
	/// <remarks>
	/// Why this is C# and not a D4 coverage script: both attach operators take a server-side directory path, and a script has
	/// no way to learn one. For a library in the server's default library directory the Directory column of the Libraries
	/// catalog table reports the empty string - GetAvailableLibraries fills Directory in only for the second and later entries
	/// of a multi-directory LibraryDirectories setting - and nothing else a script can see exposes a path. A test that knows
	/// its own instance's directory can call these operators; a script cannot.
	///
	/// Every case builds the folder it attaches the same way, through <see cref="DeployLibraryFolder"/>: the server writes the
	/// descriptor, DetachLibrary takes the library out of the catalog and leaves the folder, and the folder is then moved out
	/// of the server's library directory. What is attached is therefore a real descriptor the server itself wrote, sitting
	/// where the server does not scan - which is what a deployment presents. Writing a .d4l by hand would pin this fixture's
	/// idea of the format rather than the server's.
	///
	/// No library here has files. An attached library with a file marked as an assembly loads it on registration and a loaded
	/// assembly is never unloaded from this process; LibraryLoadingTest owns that ground. Each case gets a TestServerInstance
	/// of its own rather than sharing InProcessTestFixture's server, because they stop and start the server and rearrange its
	/// directory.
	/// </remarks>
	[TestFixture]
	public class LibraryAttachTest
	{
		/// <summary>A library attached from an explicit directory is in the catalog with that directory, and registers out of it.</summary>
		[Test]
		public void AttachedLibraryRegistersFromItsOwnFolder()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AttachedLibraryRegistersFromItsOwnFolder)))
			{
				instance.Server.Start();
				string folder = DeployLibraryFolder(instance, "AttachOne");

				instance.ExecuteScript(String.Format("AttachLibrary({0}, {1});", Literal("AttachOne"), Literal(folder)));

				Assert.That(IsAttached(instance, "AttachOne"), Is.True);
				Assert.That(AttachedDirectory(instance, "AttachOne"), Is.EqualTo(folder));

				// Attached, not copied: the server's own library directory still has no folder for it.
				Assert.That(Directory.Exists(DefaultLibraryFolder(instance, "AttachOne")), Is.False);

				// Attaching is pointless if the library cannot then be registered where it lies.
				instance.ExecuteScript(String.Format("RegisterLibrary({0});", Literal("AttachOne")));
				Assert.That(IsRegistered(instance, "AttachOne"), Is.True);
			}
		}

		/// <summary>
		/// The whole point of attaching rather than copying: AttachLibrary records the path through SetLibraryDirectory, and the
		/// next start attaches the library from it again (ServerCatalogDeviceSession.AttachLibraries). A library outside the
		/// server's library directory is in no other way reachable after a restart - the startup scan of that directory does not
		/// see it - so this is the only assertion here that reaches the stored directory at all.
		/// </summary>
		[Test]
		public void AttachedDirectorySurvivesARestart()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AttachedDirectorySurvivesARestart)))
			{
				instance.Server.Start();
				string folder = DeployLibraryFolder(instance, "AttachRestart");
				instance.ExecuteScript(String.Format("AttachLibrary({0}, {1}); RegisterLibrary({0});", Literal("AttachRestart"), Literal(folder)));

				instance.Server.Stop();
				instance.Server.Start(); // the second start reopens the catalog the first created; see CatalogStoreTest

				Assert.That(IsAttached(instance, "AttachRestart"), Is.True);
				Assert.That(AttachedDirectory(instance, "AttachRestart"), Is.EqualTo(folder));
				Assert.That(IsRegistered(instance, "AttachRestart"), Is.True);
			}
		}

		/// <summary>
		/// AttachLibraries scans a directory and attaches every library folder in it the catalog does not already have. The skip
		/// is the branch an administrator hits on every re-run of a deployment script, so one of the two is attached - and
		/// registered - before the scan.
		/// </summary>
		[Test]
		public void AttachLibrariesAttachesWhatIsNewAndSkipsWhatIsAlreadyThere()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AttachLibrariesAttachesWhatIsNewAndSkipsWhatIsAlreadyThere)))
			{
				instance.Server.Start();
				string first = DeployLibraryFolder(instance, "AttachScanOne");
				string second = DeployLibraryFolder(instance, "AttachScanTwo");

				instance.ExecuteScript(String.Format("AttachLibrary({0}, {1}); RegisterLibrary({0});", Literal("AttachScanOne"), Literal(first)));

				Assert.DoesNotThrow(() => instance.ExecuteScript(String.Format("AttachLibraries({0});", Literal(DeploymentDirectory(instance)))));

				Assert.That(AttachedDirectory(instance, "AttachScanOne"), Is.EqualTo(first));
				Assert.That(AttachedDirectory(instance, "AttachScanTwo"), Is.EqualTo(second));
				Assert.That(IsRegistered(instance, "AttachScanOne"), Is.True, "The scan disturbed the library that was already attached.");
			}
		}

		/// <summary>
		/// Pinned rather than fixed: attaching a name that has no descriptor in the given directory raises nothing and changes
		/// nothing. AttachLibrary goes through GetAvailableLibrary, which returns null when the .d4l is absent, and the operator
		/// then does nothing at all - so an administrator who points it at the wrong folder gets silence. The parent plan ticket
		/// expected an error here; this records what the engine does.
		/// </summary>
		[Test]
		public void AttachingFromADirectoryWithNoDescriptorDoesNothing()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AttachingFromADirectoryWithNoDescriptorDoesNothing)))
			{
				instance.Server.Start();
				string empty = Path.Combine(DeploymentDirectory(instance), "AttachNothing");
				Directory.CreateDirectory(empty);

				Assert.DoesNotThrow(() => instance.ExecuteScript(String.Format("AttachLibrary({0}, {1});", Literal("AttachNothing"), Literal(empty))));
				Assert.That(IsAttached(instance, "AttachNothing"), Is.False);
			}
		}

		/// <summary>
		/// The same silence, reached by the mistake a deployment actually makes: GetAvailableLibrary builds the file name from
		/// the library name it is asked for and not from what the folder holds, so AttachLibrary('A', '...\B') looks for
		/// B\A.d4l. Neither the name asked for nor the name on disk is attached.
		/// </summary>
		[Test]
		public void AttachingALibraryUnderTheWrongNameDoesNothing()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AttachingALibraryUnderTheWrongNameDoesNothing)))
			{
				instance.Server.Start();
				string folder = DeployLibraryFolder(instance, "AttachNamed");

				Assert.DoesNotThrow(() => instance.ExecuteScript(String.Format("AttachLibrary({0}, {1});", Literal("AttachMisnamed"), Literal(folder))));
				Assert.That(IsAttached(instance, "AttachMisnamed"), Is.False);
				Assert.That(IsAttached(instance, "AttachNamed"), Is.False);
			}
		}

		/// <summary>
		/// Detaching a library that was attached from an explicit directory removes the recorded directory as well as the
		/// catalog row (DeleteLibraryDirectory), so the next start does not bring it back. The folder itself stays on disk -
		/// that is what makes a re-attach possible, and what distinguishes a detach from a drop.
		/// </summary>
		[Test]
		public void DetachingAnAttachedLibraryLeavesItsFolderAndForgetsItsDirectory()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(DetachingAnAttachedLibraryLeavesItsFolderAndForgetsItsDirectory)))
			{
				instance.Server.Start();
				string folder = DeployLibraryFolder(instance, "AttachForget");
				instance.ExecuteScript(String.Format("AttachLibrary({0}, {1}); DetachLibrary({0});", Literal("AttachForget"), Literal(folder)));

				Assert.That(IsAttached(instance, "AttachForget"), Is.False);
				Assert.That(File.Exists(Path.Combine(folder, "AttachForget.d4l")), Is.True);

				instance.Server.Stop();
				instance.Server.Start();

				Assert.That(IsAttached(instance, "AttachForget"), Is.False);

				// The folder is still a deployment the administrator can attach again.
				instance.ExecuteScript(String.Format("AttachLibrary({0}, {1});", Literal("AttachForget"), Literal(folder)));
				Assert.That(AttachedDirectory(instance, "AttachForget"), Is.EqualTo(folder));
			}
		}

		/// <summary>
		/// RefreshLibraries rescans the server's own library directory and removes from the catalog every library it no longer
		/// finds there - but it only considers a library whose Directory is empty, which is to say one that lives in that
		/// directory. A library attached from a folder of its own is never reconsidered, so when its deployment folder is
		/// removed by hand the catalog keeps pointing at the folder that has gone, and only DetachLibrary clears the entry.
		/// The script in debt-library-administration-coverage covers the re-attaching half and left this half here, because
		/// deleting a folder behind the server's back needs a path and a script has none.
		/// </summary>
		[Test]
		public void RefreshLibrariesForgetsAMissingDefaultLibraryAndKeepsAnAttachedOne()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(RefreshLibrariesForgetsAMissingDefaultLibraryAndKeepsAnAttachedOne)))
			{
				instance.Server.Start();
				string attached = DeployLibraryFolder(instance, "RefreshAttached");
				instance.ExecuteScript(String.Format("AttachLibrary({0}, {1});", Literal("RefreshAttached"), Literal(attached)));
				string inLibraryDirectory = CreateLibraryFolder(instance, "RefreshDefault");
				Assert.That(IsAttached(instance, "RefreshDefault"), Is.True, "The premise of the removal below: the library is in the catalog to start with.");

				// Both folders go behind the server's back; neither library is registered, so nothing holds them open.
				Directory.Delete(inLibraryDirectory, true);
				Directory.Delete(attached, true);

				instance.ExecuteScript("RefreshLibraries();");

				Assert.That(IsAttached(instance, "RefreshDefault"), Is.False, "RefreshLibraries kept a library whose folder in the library directory had gone.");
				Assert.That(AttachedDirectory(instance, "RefreshAttached"), Is.EqualTo(attached), "RefreshLibraries removed a library that was attached from a folder of its own.");
			}
		}

		/// <summary>
		/// AttachLibraries on a directory that does not exist surfaces an I/O exception rather than a Dataphor error, because
		/// GetAvailableLibraries hands what it is given straight to Directory.GetDirectories. What the caller sees is the plan's
		/// wrapper, RuntimeError (104229), over a DirectoryNotFoundException whose message names the path - so an administrator
		/// who mistypes a path is told which path, which is the thing worth holding here.
		/// </summary>
		[Test]
		public void AttachLibrariesOnADirectoryThatDoesNotExistReportsThePath()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AttachLibrariesOnADirectoryThatDoesNotExistReportsThePath)))
			{
				instance.Server.Start();
				string missing = Path.Combine(DeploymentDirectory(instance), "NoSuchDeployment");

				Exception exception = Assert.Catch(() => instance.ExecuteScript(String.Format("AttachLibraries({0});", Literal(missing))));

				DataphorException error = FindError(exception, (int)Runtime.RuntimeException.Codes.RuntimeError);
				Assert.That(error, Is.Not.Null, exception.ToString());
				Assert.That(error.Message, Does.Contain(missing));
				Assert.That(error.InnerException, Is.InstanceOf<DirectoryNotFoundException>(), exception.ToString());
			}
		}

		/// <summary>The directory the cases move their library folders into: a sibling of the server's own Libraries folder, which the server does not scan.</summary>
		private static string DeploymentDirectory(TestServerInstance instance)
		{
			return Path.Combine(instance.Directory, "Deployment");
		}

		/// <summary>Where the server puts a library of this name when it is given no directory of its own.</summary>
		private static string DefaultLibraryFolder(TestServerInstance instance, string name)
		{
			return Path.Combine(instance.Directory, TestServerInstance.LibrariesFolderName, name);
		}

		/// <summary>
		/// Has the server create an empty library in its own library directory and returns the folder it wrote the descriptor
		/// into. Empty by design: a library with a file marked as an assembly would load it on registration.
		/// </summary>
		private static string CreateLibraryFolder(TestServerInstance instance, string name)
		{
			instance.ExecuteScript
			(
				String.Format
				(
					"CreateLibrary(LibraryDescriptor({0}, VersionNumber(1, 0, 0), \"\", list(FileReference){{}}, list(LibraryReference){{}}));",
					Literal(name)
				)
			);
			return DefaultLibraryFolder(instance, name);
		}

		/// <summary>
		/// Produces the folder a deployment would present: the server creates the library in its own library directory, the
		/// library is detached (which leaves the folder), and the folder is moved into <see cref="DeploymentDirectory"/>.
		/// Returns the folder's absolute path, which is what the attach operators are given.
		/// </summary>
		private static string DeployLibraryFolder(TestServerInstance instance, string name)
		{
			string created = CreateLibraryFolder(instance, name);

			// The reason this fixture is C# rather than a coverage script: a library in the server's default library directory
			// has no path in the catalog at all, so a script has nothing to pass to the attach operators.
			Assert.That(AttachedDirectory(instance, name), Is.Empty, "A library created in the default library directory should have no recorded directory.");

			instance.ExecuteScript(String.Format("DetachLibrary({0});", Literal(name)));

			string target = Path.Combine(DeploymentDirectory(instance), name);
			Directory.CreateDirectory(DeploymentDirectory(instance));
			Directory.Move(created, target);
			return target;
		}

		private static bool IsAttached(TestServerInstance instance, string name)
		{
			return (bool)instance.Evaluate(String.Format("exists (Libraries where Name = {0})", Literal(name)));
		}

		/// <summary>The Directory column of the library's Libraries row; call it only for a library that is attached.</summary>
		private static string AttachedDirectory(TestServerInstance instance, string name)
		{
			Assert.That(IsAttached(instance, name), Is.True, String.Format("Library {0} is not in the catalog.", name));
			return (string)instance.Evaluate(String.Format("Directory from row from (Libraries where Name = {0})", Literal(name)));
		}

		private static bool IsRegistered(TestServerInstance instance, string name)
		{
			return (bool)instance.Evaluate(String.Format("exists (LoadedLibraries where Name = {0})", Literal(name)));
		}

		/// <summary>A D4 string literal. The lexer has no escape character - a quote is written twice - so a Windows path goes in as it is.</summary>
		private static string Literal(string value)
		{
			return "'" + value.Replace("'", "''") + "'";
		}

		private static DataphorException FindError(Exception exception, int code)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is DataphorException dataphorException && dataphorException.Code == code)
					return dataphorException;
			return null;
		}
	}
}
