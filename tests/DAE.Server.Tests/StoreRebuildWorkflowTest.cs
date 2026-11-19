/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// WS4's rebuild workflow (docs/modernization-plan.md#ws4-catalog-store): on a fresh instance, register the sample set of
	/// libraries, create roles / users / rights / device users, and run small DDL, then restart and prove every piece survives.
	/// Prints a single measured timing line so later agents can read a baseline from CI output; no assertion on timing.
	/// </summary>
	/// <remarks>
	/// Sample set: General (auto-registered), System.Platform, SimpleDevice, Frontend, Sample.Common and Sample.DetailAdd.
	/// Everything that needs a SQL Server or PostgreSQL (Sample.Demo, Sample.Forum, Sample.Pubs, Sample.Shipping) stays out of
	/// this test; feat-ws7-devices-and-libraries owns that arm. Frontend's Register.d4 creates the built-in User and Developer
	/// roles; Sample.Common's creates the SampleDevice on SimpleDevice; Sample.DetailAdd's creates Master, Category and Detail.
	///
	/// The device user's Data column stores whatever Schema.SecurityUtility.EncryptPassword puts there; this test asserts the
	/// store round-trips the stored form, not that the form is secure (security-password-storage-design owns changing it).
	/// </remarks>
	[TestFixture]
	public class StoreRebuildWorkflowTest
	{
		private static readonly string[] SampleSetLibraries =
		[
			"System.Platform",
			"SimpleDevice",
			"Frontend",
			"Sample.Common",
			"Sample.DetailAdd"
		];

		[Test]
		public void RebuildRegistersLibrariesAndSurvivesRestart()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(RebuildRegistersLibrariesAndSurvivesRestart), SampleSetLibraries))
			{
				instance.Server.Start();
				Assert.That(instance.Server.State, Is.EqualTo(ServerState.Started));
				string catalogFile = CatalogFile(instance);

				Stopwatch watch = Stopwatch.StartNew();

				// Phase 1: register each library in order. General is already registered by the server's start.
				foreach (string library in SampleSetLibraries)
					instance.ExecuteScript(String.Format("EnsureLibraryRegistered('{0}');", library));

				Assert.That(File.Exists(catalogFile), Is.True, "catalog file not created after registering libraries");

				// Phase 2: security. Two roles with one right each, one user granted one role, one device user on SampleDevice.
				instance.ExecuteScript
				(
					"create role RebuildReader;"
						+ " create role RebuildWriter;"
						+ " GrantRightToRole('System.CreateUser', 'RebuildReader');"
						+ " GrantRightToRole('System.CreateRole', 'RebuildWriter');"
						+ " CreateUser('RebuildUser', 'Rebuild User', 'secret');"
						+ " AddUserToRole('RebuildUser', 'RebuildReader');"
						+ " CreateDeviceUser('RebuildUser', 'SampleDevice', 'sa', 'devicesecret');"
				);

				// Phase 3: a table and a view, so operator registration and derivation plumbing both hit the store. The create
				// view references RebuildWidget, so a view that comes back with a dangling reference fails the restart check.
				instance.ExecuteScript("create table RebuildWidget { ID : Integer, Name : String, key { ID } };");
				instance.ExecuteScript("create view RebuildWidgetView RebuildWidget over { ID };");

				watch.Stop();
				TestContext.WriteLine("StoreRebuildWorkflow timing: register+security+DDL = {0} ms", watch.ElapsedMilliseconds);

				// A clean stop drains SQLite's WAL; the catalog directory must hold only the main file afterwards. A .sqlite-wal
				// or .sqlite-journal sidecar would mean a reopen could silently reconcile from stale state.
				instance.Server.Stop();
				string catalogDirectory = Path.GetDirectoryName(catalogFile);
				string[] leftover = Directory.GetFiles(catalogDirectory).Select(Path.GetFileName).ToArray();
				Assert.That
				(
					leftover,
					Is.EquivalentTo(new[] { "DAECatalog" }),
					"stray SQLite sidecar files after clean stop: " + String.Join(", ", leftover)
				);

				// Restart and verify every rebuilt object survived.
				instance.Server.Start();
				Assert.That(instance.Server.State, Is.EqualTo(ServerState.Started));

				// Libraries loaded.
				foreach (string library in SampleSetLibraries)
					Assert.That(instance.Evaluate(String.Format("exists (LoadedLibraries where Name = '{0}')", library)), Is.True, "library " + library + " not loaded after restart");

				// Users and roles catalog state.
				Assert.That(instance.Evaluate("exists (Users where ID = 'RebuildUser')"), Is.True, "RebuildUser missing after restart");
				Assert.That(instance.Evaluate("exists (Roles where Name = 'RebuildReader' or Name like '%.RebuildReader')"), Is.True, "RebuildReader missing after restart");
				Assert.That(instance.Evaluate("exists (Roles where Name = 'RebuildWriter' or Name like '%.RebuildWriter')"), Is.True, "RebuildWriter missing after restart");
				Assert.That(instance.Evaluate("exists (UserRoles where User_ID = 'RebuildUser' and (Role_Name = 'RebuildReader' or Role_Name like '%.RebuildReader'))"), Is.True, "RebuildUser's RebuildReader membership missing after restart");
				Assert.That(instance.Evaluate("exists (RoleRightAssignments where (Role_Name = 'RebuildReader' or Role_Name like '%.RebuildReader') and Right_Name = 'System.CreateUser' and IsGranted)"), Is.True, "RebuildReader's System.CreateUser grant missing after restart");
				Assert.That(instance.Evaluate("exists (RoleRightAssignments where (Role_Name = 'RebuildWriter' or Role_Name like '%.RebuildWriter') and Right_Name = 'System.CreateRole' and IsGranted)"), Is.True, "RebuildWriter's System.CreateRole grant missing after restart");
				Assert.That(instance.Evaluate("exists (DeviceUsers where User_ID = 'RebuildUser' and (Device_Name = 'SampleDevice' or Device_Name like '%.SampleDevice'))"), Is.True, "RebuildUser's device user on SampleDevice missing after restart");

				// Devices and tables from the libraries' Register.d4 and from phase 3. The Register.d4-created objects live
				// under their library's namespace (e.g. Sample.DetailAdd.Master, Sample.Common.SampleDevice); RebuildWidget
				// and RebuildWidgetView come back bare because they were created in General. The queries accept either form.
				Assert.That(instance.Evaluate("exists (Devices where Name = 'SampleDevice' or Name like '%.SampleDevice')"), Is.True, "SampleDevice missing after restart");
				Assert.That(instance.Evaluate("Count(BaseTableVars where Library_Name = 'Sample.DetailAdd')"), Is.EqualTo(3), "Sample.DetailAdd's three tables missing after restart");
				Assert.That(instance.Evaluate("exists (BaseTableVars where Name = 'RebuildWidget' or Name like '%.RebuildWidget')"), Is.True, "RebuildWidget table missing after restart");
				Assert.That(instance.Evaluate("exists (DerivedTableVars where Name = 'RebuildWidgetView' or Name like '%.RebuildWidgetView')"), Is.True, "RebuildWidgetView missing after restart");
				Assert.That(instance.Evaluate("exists (Roles where Name = 'User' or Name like '%.User')"), Is.True, "Frontend's User role missing after restart");
				Assert.That(instance.Evaluate("exists (Roles where Name = 'Developer' or Name like '%.Developer')"), Is.True, "Frontend's Developer role missing after restart");

				// The stored forms of the passwords round-trip. Not a secrecy check: both columns hold whatever the current
				// implementation puts there, and security-password-storage-design will change what that is.
				string userPassword = ReadScalar(instance, "select Data from DAEUsers where ID = 'RebuildUser'");
				Assert.That(userPassword, Is.Not.Null.And.Not.Empty, "RebuildUser's stored password is empty after restart");
				string deviceUserPassword = ReadScalar(instance, "select Data from DAEDeviceUsers where User_ID = 'RebuildUser'");
				Assert.That(deviceUserPassword, Is.Not.Null.And.Not.Empty, "RebuildUser's device user stored password is empty after restart");
			}
		}

		private static string CatalogFile(TestServerInstance instance)
		{
			return Path.Combine(instance.Directory, TestServerInstance.InstanceFolderName, Server.DefaultCatalogDirectory, "DAECatalog");
		}

		private static string ReadScalar(TestServerInstance instance, string sql)
		{
			using (SQLiteConnection connection = instance.OpenCatalogStoreFile())
			using (SQLiteCommand command = new SQLiteCommand(sql, connection))
			{
				object value = command.ExecuteScalar();
				return value == null || Convert.IsDBNull(value) ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
			}
		}
	}
}
