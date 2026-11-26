/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;

using NUnit.Framework;

using Microsoft.Data.SqlClient;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Where the tests read the runtime layout from, and the temporary folder they write into: one per test process,
	/// under %TEMP%\Dataphor.Tests. Nothing a test does should reach outside that folder.
	/// </summary>
	public static class TestEnvironment
	{
		/// <summary>The AssemblyMetadata key under which the project file records $(DataphorLayoutDir) of the configuration being built.</summary>
		public const string LayoutDirectoryMetadataKey = "DataphorLayoutDir";

		/// <summary>The AssemblyMetadata key under which the project file records the test server program's build output.</summary>
		public const string TestHarnessPathMetadataKey = "DataphorTestHarnessPath";

		/// <summary>The AssemblyMetadata key under which the project file records the repository's root directory.</summary>
		public const string RepositoryDirectoryMetadataKey = "DataphorRepositoryDir";

		/// <summary>The AssemblyMetadata key under which the project file records the directory holding the checked-in golden files.</summary>
		public const string GoldensDirectoryMetadataKey = "DataphorGoldensDir";

		/// <summary>Set to 1 to turn on the engine's logging and keep every test directory; see AGENTS.md "Build and test".</summary>
		public const string LoggingVariable = "DATAPHOR_TEST_LOGGING";

		/// <summary>
		/// Set to a connection string naming a <em>SQL Server</em> - not a database - to run the passes that need a live one;
		/// see AGENTS.md "Build and test". Every pass that reads it puts its own database on whatever is set.
		/// </summary>
		public const string MSSQLVariable = "DATAPHOR_TEST_MSSQL";

		public const string RunRootName = "Dataphor.Tests";

		public static readonly bool KeepFiles = Environment.GetEnvironmentVariable(LoggingVariable) == "1";

		/// <summary>
		/// Whether test servers log. Off by default because a logging server also creates and writes the Windows event-log
		/// source when the process is elevated, as on CI runners.
		/// </summary>
		public static bool LoggingEnabled => KeepFiles;

		// Lazy<T> caches a failure, so every test that needs the layout fails with the same message.
		private static readonly Lazy<string> _layoutDirectory = new Lazy<string>(FindLayoutDirectory);
		public static string LayoutDirectory => _layoutDirectory.Value;

		public static string LibrariesDirectory => Path.Combine(LayoutDirectory, "Libraries");

		/// <summary>What the project file recorded under <paramref name="key"/>, or null.</summary>
		private static string Metadata(string key)
		{
			string result = null;
			foreach (AssemblyMetadataAttribute attribute in typeof(TestEnvironment).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
				if (attribute.Key == key)
					result = attribute.Value;
			return result;
		}

		private static string FindLayoutDirectory()
		{
			string layoutDirectory = Metadata(LayoutDirectoryMetadataKey);
			if (String.IsNullOrEmpty(layoutDirectory))
				throw new InvalidOperationException(String.Format("The test assembly does not record the runtime layout directory (AssemblyMetadata \"{0}\"). Build it through its project file: dotnet build Dataphor.slnx", LayoutDirectoryMetadataKey));

			string librariesDirectory = Path.Combine(layoutDirectory, "Libraries");
			if (!Directory.Exists(librariesDirectory))
				throw new InvalidOperationException(String.Format("The runtime layout is missing: \"{0}\" does not exist. Run dotnet build Dataphor.slnx (with the same -c configuration) before running the tests.", librariesDirectory));

			return layoutDirectory;
		}

		// Lazy<T> caches a failure, so every test that needs the program fails with the same message.
		private static readonly Lazy<string> _testHarnessPath = new Lazy<string>(FindTestHarnessPath);
		/// <summary>The test server program (tests/TestHarness), which a fixture starts as a child process.</summary>
		public static string TestHarnessPath => _testHarnessPath.Value;

		private static string FindTestHarnessPath()
		{
			string assemblyPath = Metadata(TestHarnessPathMetadataKey);
			if (String.IsNullOrEmpty(assemblyPath))
				throw new InvalidOperationException(String.Format("The test assembly does not record the test server program (AssemblyMetadata \"{0}\"). Build it through its project file: dotnet build Dataphor.slnx", TestHarnessPathMetadataKey));

			// The project file records the assembly, which is what MSBuild knows about; the program is the application host beside it.
			string result = Path.ChangeExtension(assemblyPath, ".exe");
			if (!File.Exists(result))
				throw new InvalidOperationException(String.Format("The test server program is missing: \"{0}\" does not exist. Build tests/TestHarness/DAE.Test.Harness.csproj - dotnet build Dataphor.slnx, with the same -c configuration.", result));

			return result;
		}

		// Lazy<T> caches a failure, so every test that needs the repository fails with the same message.
		private static readonly Lazy<string> _repositoryDirectory = new Lazy<string>(FindRepositoryDirectory);
		/// <summary>
		/// The repository's own root, for the corpus tests that read source files (D4 scripts, form documents) rather than
		/// the runtime layout. The layout holds only the libraries the build assembles, so a test that must see everything
		/// in the tree reads the tree.
		/// </summary>
		public static string RepositoryDirectory => _repositoryDirectory.Value;

		private static string FindRepositoryDirectory()
		{
			string repositoryDirectory = Metadata(RepositoryDirectoryMetadataKey);
			if (String.IsNullOrEmpty(repositoryDirectory))
				throw new InvalidOperationException(String.Format("The test assembly does not record the repository directory (AssemblyMetadata \"{0}\"). Build it through its project file: dotnet build Dataphor.slnx", RepositoryDirectoryMetadataKey));

			// The solution file is the marker the whole suite identifies the repository by; its absence means the recorded
			// path is stale (a moved or copied test assembly), which is worth saying plainly rather than failing later.
			if (!File.Exists(Path.Combine(repositoryDirectory, "Dataphor.slnx")))
				throw new InvalidOperationException(String.Format("The recorded repository directory \"{0}\" does not hold Dataphor.slnx. Rebuild the test project: dotnet build Dataphor.slnx", repositoryDirectory));

			return repositoryDirectory;
		}

		// Lazy<T> caches a failure, so every test that needs the goldens fails with the same message.
		private static readonly Lazy<string> _goldensDirectory = new Lazy<string>(FindGoldensDirectory);
		/// <summary>
		/// Where the checked-in golden files live (tests/goldens). One tree per kind of golden beneath it, so that the
		/// passes comparing generated output against a reviewed file - derived forms today, device SQL later - keep their
		/// files side by side rather than each inside its own test project.
		/// </summary>
		/// <remarks>
		/// Unlike the layout and the test program, the directory is not required to exist: the pass that owns a tree
		/// creates it the first time it writes one, and a missing tree is a set of missing goldens, which that pass
		/// reports file by file.
		/// </remarks>
		public static string GoldensDirectory => _goldensDirectory.Value;

		private static string FindGoldensDirectory()
		{
			string goldensDirectory = Metadata(GoldensDirectoryMetadataKey);
			if (String.IsNullOrEmpty(goldensDirectory))
				throw new InvalidOperationException(String.Format("The test assembly does not record the goldens directory (AssemblyMetadata \"{0}\"). Build it through its project file: dotnet build Dataphor.slnx", GoldensDirectoryMetadataKey));

			// The recorded path is a build-machine absolute path and a pass that updates its goldens writes to it, so a
			// moved or copied test assembly must not be allowed to write into a tree that is not the repository's.
			// RepositoryDirectory checks itself against Dataphor.slnx.
			if (!goldensDirectory.StartsWith(RepositoryDirectory, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException(String.Format("The recorded goldens directory \"{0}\" is not inside the repository \"{1}\". Rebuild the test project: dotnet build Dataphor.slnx", goldensDirectory, RepositoryDirectory));

			return goldensDirectory;
		}

		private static readonly Lazy<string> _runDirectory = new Lazy<string>(CreateRunDirectory);
		/// <summary>This process's folder, %TEMP%\Dataphor.Tests\&lt;process id&gt;-&lt;random&gt;.</summary>
		public static string RunDirectory => _runDirectory.Value;

		private static string CreateRunDirectory()
		{
			string root = Path.Combine(Path.GetTempPath(), RunRootName);
			Directory.CreateDirectory(root);

			// With KeepFiles the developer is looking at earlier runs' folders; the next run without it removes them.
			if (!KeepFiles)
				DeleteStaleRunDirectories(root);

			string result = Path.Combine(root, String.Format("{0}-{1}", Environment.ProcessId, RandomHex(8)));
			Directory.CreateDirectory(result);

			// Removes the folder only when every test directory in it was deleted: a library assembly stays mapped until the
			// process exits, so a folder holding one is left for the next run's sweep.
			AppDomain.CurrentDomain.ProcessExit += (sender, args) => { try { Directory.Delete(result, false); } catch (IOException) { } catch (UnauthorizedAccessException) { } };

			return result;
		}

		/// <summary>
		/// Deletes, best effort, the folders of earlier runs whose process is no longer running: a run cannot delete a folder
		/// that holds a library assembly it loaded, because the file stays mapped until the process exits.
		/// </summary>
		// NOTE: accepted tradeoff — a folder whose process id has been reused by a running process stays until a later run.
		private static void DeleteStaleRunDirectories(string root)
		{
			foreach (string directory in Directory.GetDirectories(root))
			{
				string name = Path.GetFileName(directory);
				int separator = name.IndexOf('-');
				if (separator <= 0 || !Int32.TryParse(name.Substring(0, separator), out int processId) || IsRunning(processId))
					continue;

				try
				{
					Directory.Delete(directory, true);
				}
				catch (IOException) { }
				catch (UnauthorizedAccessException) { }
			}
		}

		private static bool IsRunning(int processId)
		{
			try
			{
				using (Process process = Process.GetProcessById(processId))
					return !process.HasExited;
			}
			catch (ArgumentException)
			{
				return false; // no process with this id
			}
			catch (InvalidOperationException)
			{
				return false;
			}
			catch (System.ComponentModel.Win32Exception)
			{
				return true; // exists, but this process may not inspect it
			}
		}

		private static string RandomHex(int length)
		{
			return Convert.ToHexString(RandomNumberGenerator.GetBytes((length + 1) / 2)).Substring(0, length).ToLowerInvariant();
		}

		private static int _directoryCounter;

		/// <summary>Creates a new, empty, uniquely named folder in <see cref="RunDirectory"/> and returns its absolute path.</summary>
		public static string NewDirectory(string name)
		{
			foreach (char invalid in Path.GetInvalidFileNameChars())
				name = name.Replace(invalid, '_');
			string result = Path.Combine(RunDirectory, String.Format("{0}-{1}", name, Interlocked.Increment(ref _directoryCounter)));
			Directory.CreateDirectory(result);
			if (KeepFiles)
				TestContext.Progress.WriteLine("{0}=1: keeping {1}", LoggingVariable, result);
			return result;
		}

		/// <summary>Deletes a test directory, best effort, unless <see cref="KeepFiles"/>.</summary>
		/// <remarks>
		/// A folder it cannot delete - typically because the process loaded a library assembly from it, which stays mapped
		/// until the process exits - is reported to the test output and left for the next run's sweep.
		/// </remarks>
		public static void TryDeleteDirectory(string directory)
		{
			if (KeepFiles)
				return;

			try
			{
				Directory.Delete(directory, true);
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				TestContext.Progress.WriteLine("Could not delete {0}; the next test run deletes it. {1}", directory, exception.Message);
			}
		}

		/// <summary>Copies each named library folder of the runtime layout, recursively, into <paramref name="targetDirectory"/>.</summary>
		public static void CopyLibraries(string targetDirectory, IEnumerable<string> names)
		{
			string librariesDirectory = LibrariesDirectory; // checks the layout even when no library is named
			List<string> sourceDirectories = new List<string>();
			foreach (string name in names)
			{
				string source = Path.Combine(librariesDirectory, name);
				// Checked here rather than left to CopyLibraryFolders, so that the message names the library a fixture asked
				// for and not just the folder that is missing.
				if (!Directory.Exists(source))
					throw new ArgumentException(String.Format("The runtime layout has no library \"{0}\" (expected the folder \"{1}\").", name, source), nameof(names));
				sourceDirectories.Add(source);
			}
			CopyLibraryFolders(targetDirectory, sourceDirectories);
		}

		/// <summary>
		/// Copies each of <paramref name="sourceDirectories"/>, recursively, into <paramref name="targetDirectory"/>,
		/// keeping each folder's own name - which is the library name a server reads it as.
		/// </summary>
		/// <remarks>
		/// Takes folders rather than names so that a corpus whose libraries are not all in the runtime layout copies what
		/// it discovered rather than reaching into the layout for it: SamplesCorpusTest resolves its closure in the layout
		/// but the external corpus of compat-corpus-harness has no layout to resolve against.
		/// </remarks>
		public static void CopyLibraryFolders(string targetDirectory, IEnumerable<string> sourceDirectories)
		{
			Directory.CreateDirectory(targetDirectory);
			foreach (string source in sourceDirectories)
			{
				if (!Directory.Exists(source))
					throw new ArgumentException(String.Format("There is no library folder \"{0}\".", source), nameof(sourceDirectories));
				CopyDirectory(source, Path.Combine(targetDirectory, Path.GetFileName(Path.TrimEndingDirectorySeparator(source))));
			}
		}

		/// <summary>Copies a folder recursively; <paramref name="target"/> may exist, but must not already hold the files copied.</summary>
		public static void CopyDirectory(string source, string target)
		{
			Directory.CreateDirectory(target);
			foreach (string file in Directory.GetFiles(source))
				File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
			foreach (string directory in Directory.GetDirectories(source))
				CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
		}

		#region The configured SQL Server

		/// <summary>What <see cref="MSSQLVariable"/> holds, or null where it is unset or empty.</summary>
		public static string MSSQLConnectionString
		{
			get
			{
				string result = Environment.GetEnvironmentVariable(MSSQLVariable);
				return String.IsNullOrEmpty(result) ? null : result;
			}
		}

		/// <summary>Whether this machine has been given a SQL Server to test against.</summary>
		/// <remarks>
		/// The variable being set is the only test, and every pass that needs a server asks this before it does anything that
		/// could connect. Nothing may connect in order to decide whether to connect: reaching for a server that is not there
		/// costs the driver's whole timeout - measured 2026-11-22 at 33.6 s and 21.0 s for two sample libraries - which is
		/// more than the live passes take when a server is there.
		/// </remarks>
		public static bool MSSQLConfigured => MSSQLConnectionString != null;

		/// <summary>
		/// What a case says when it is skipped for want of a server. <paramref name="whatWouldRun"/> completes the sentence
		/// "Set DATAPHOR_TEST_MSSQL ... to &lt;whatWouldRun&gt;."
		/// </summary>
		public static string MSSQLNotConfigured(string whatWouldRun)
		{
			return
				String.Format
				(
					"Set {0} to a SQL Server connection string - a server, not a database, for example \"Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=SSPI\" - to {1}.",
					MSSQLVariable,
					whatWouldRun
				);
		}

		/// <summary>The configured server's connection string with <paramref name="databaseName"/> as its database.</summary>
		/// <remarks>
		/// Everything else is carried through as the variable holds it, as MSSQLStoreContractTests' own housekeeping
		/// connections are: what is set has to be reachable as it stands, so a server whose certificate this machine does not
		/// trust needs Encrypt=False said in the variable. Only the device goes through a connection-string builder that
		/// defaults that away for itself (MSSQLADODotNetConnectionStringBuilder).
		/// </remarks>
		public static string MSSQLConnectionStringFor(string databaseName)
		{
			SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(MSSQLConnectionString);
			builder.InitialCatalog = databaseName;
			return builder.ConnectionString;
		}

		/// <summary>
		/// The whole of the D4 that gives a server a SQL Server device named <paramref name="deviceName"/> against the
		/// configured server, holding its tables in <paramref name="databaseName"/> and reconciling in
		/// <paramref name="reconciliationMode"/> ("command" for a device that data-definition commands reach, "none" for one
		/// that is only read): the create-device statement, and after it the device user where the variable names a SQL login.
		/// Ends in a line break, so a caller can concatenate it into a script.
		/// </summary>
		/// <remarks>
		/// One operation rather than two, because the two statements are not separable: a device created from these attributes
		/// against a server that needs a SQL login carries no credentials of its own (see MSSQLDeviceUser), so a site that
		/// emitted the definition and forgot the device user would pass on integrated security and fail against a login - which
		/// is the shape of the defect that put the device user here in the first place. It is also the one place the device's
		/// registered class name is spelled: the alias is "MSSQLDevice.MSSQLDevice" - "MSSQLDevice" alone, as the
		/// commented-out definition in TestFramework.Coverage.Base's Device.d4 spells it, is not an alias of anything.
		/// </remarks>
		public static string MSSQLDeviceDefinition(string deviceName, string databaseName, string reconciliationMode)
		{
			return
				String.Format
				(
					"create device {0} reconciliation {{ mode = {{ {1} }}, master = device }} class \"MSSQLDevice.MSSQLDevice\" attributes {{ {2} }};\r\n{3}",
					deviceName,
					reconciliationMode,
					MSSQLDeviceAttributes(databaseName),
					MSSQLDeviceUser(deviceName)
				);
		}

		/// <summary>
		/// The attribute list of a D4 SQL Server device definition against the configured server, for a device holding its
		/// tables in <paramref name="databaseName"/>: the server name, the database name and whatever the variable says about
		/// authentication and encryption, and nothing else.
		/// </summary>
		/// <remarks>
		/// <para>Deliberately minimal - no ConnectionClass and no ConnectionStringBuilderClass - because a definition naming
		/// only its server and database is what most deployments have, so that is the one worth proving connects.</para>
		/// <para>Encrypt and TrustServerCertificate have no device tag of their own and would be dropped on the way through
		/// the device's connection-string builder, which for a server that requires encryption would mean the device could
		/// not connect at all. They are carried as the device's ConnectionParameters, which SQLDevice.GetConnectionParameters
		/// applies after the builder has run and which is therefore what overrides the builder's Encrypt=False default.</para>
		/// </remarks>
		// A SQL login is not among them: it goes to a device user instead (MSSQLDeviceUser below), so that no password is
		// stored as the literal text of a device definition - which is where one in the variable used to land, and where the
		// catalog of a run kept with DATAPHOR_TEST_LOGGING=1 would leave it on disk.
		private static string MSSQLDeviceAttributes(string databaseName)
		{
			SqlConnectionStringBuilder server = new SqlConnectionStringBuilder(MSSQLConnectionString);
			List<string> attributes = new List<string>();
			attributes.Add(DeviceAttribute("ServerName", server.DataSource));
			attributes.Add(DeviceAttribute("DatabaseName", databaseName));
			if (server.IntegratedSecurity)
				attributes.Add(DeviceAttribute("UseIntegratedSecurity", "True"));

			string connectionParameters = MSSQLConnectionParameters();
			if (connectionParameters.Length > 0)
				attributes.Add(DeviceAttribute("ConnectionParameters", connectionParameters));

			return String.Join(", ", attributes);
		}

		/// <summary>
		/// The statement that gives the device named <paramref name="deviceName"/> the SQL login the variable names, to run
		/// after the device has been created; empty where the variable asks for integrated security, which needs no login.
		/// </summary>
		/// <remarks>
		/// <para>
		/// A device user rather than credentials on the device itself: a device user's password is stored encrypted, where an
		/// attribute of a device definition is stored as the literal text of the definition. The Dataphor user is Admin
		/// because that is the user every server in this suite is reached as.
		/// </para>
		/// <para>
		/// It is a separate statement because a SQL login cannot be a device attribute at all: Schema.Device's own credential
		/// properties are UserID and Password, not the UserName and Password tag names the connection-string builders map -
		/// and MSSQLDeviceAttributes emitted those tag names until a run against a server that needed a login reported
		/// "Class MSSQLDevice does not contain a property named UserName". The device resolves a device user ahead of its own
		/// credentials (Device.GetDeviceSessionInfo), so a device user is the spelling that works whichever it is given.
		/// </para>
		/// </remarks>
		private static string MSSQLDeviceUser(string deviceName)
		{
			SqlConnectionStringBuilder server = new SqlConnectionStringBuilder(MSSQLConnectionString);
			if (server.IntegratedSecurity)
				return String.Empty;

			return
				String.Format
				(
					"CreateDeviceUser('{0}', '{1}', '{2}', '{3}');",
					Engine.AdminUserID,
					deviceName,
					D4StringLiteral(server.UserID),
					D4StringLiteral(server.Password)
				);
		}

		/// <summary>The body of a D4 string literal; a single quote in the value is doubled, which is the only escape it has.</summary>
		private static string D4StringLiteral(string value)
		{
			return (value ?? String.Empty).Replace("'", "''");
		}

		/// <summary>One D4 device attribute; a double quote in the value is doubled, which is the only escape a D4 string literal has.</summary>
		private static string DeviceAttribute(string name, string value)
		{
			return String.Format("\"{0}\" = \"{1}\"", name, (value ?? String.Empty).Replace("\"", "\"\""));
		}

		/// <summary>
		/// The device ConnectionParameters the variable calls for: its Encrypt and TrustServerCertificate settings, if it
		/// names them, in the name=value;name=value form SQLDevice.StringToTags reads. Empty when it names neither.
		/// </summary>
		/// <remarks>
		/// Read off the string the variable holds rather than off the parsed builder, so that the device gets what the
		/// developer wrote: the builder answers Encrypt=False for a variable that says nothing about encryption, which is the
		/// client's default rather than a request, and spelling it on the device would hide the device's own default from the
		/// one pass that can prove it.
		/// </remarks>
		private static string MSSQLConnectionParameters()
		{
			DbConnectionStringBuilder asWritten = new DbConnectionStringBuilder();
			asWritten.ConnectionString = MSSQLConnectionString;
			List<string> parameters = new List<string>();
			foreach (string keyword in new string[] { "Encrypt", "TrustServerCertificate" })
				if (asWritten.ContainsKey(keyword))
					parameters.Add(String.Format("{0}={1}", keyword, asWritten[keyword]));
			return String.Join(";", parameters);
		}

		/// <summary>
		/// Makes <paramref name="databaseName"/> exist on the configured server and empties it - every foreign key and every
		/// table dropped - so that the case about to run starts on a database with nothing of an earlier run's in it.
		/// </summary>
		/// <remarks>
		/// At the start of a case rather than at the end, which is the rule MSSQLStoreContractTests established for its own
		/// database: a run kept with DATAPHOR_TEST_LOGGING=1, or one that crashed, then leaves its database to be looked at
		/// and the next run is still not poisoned by it. Nothing drops these databases afterwards - unlike the store
		/// contract's, which its own teardown drops - because what a device left in one is the evidence of what the device
		/// did, which is the whole point of running against a real server. So a machine that has run the live passes keeps
		/// one empty database per pass on whatever server was configured.
		/// </remarks>
		// NOTE: the database name is a shared resource with a fixed name, as the store contract's is. Two runs against one
		// server at the same time - two developers, or a developer and a CI agent, with DATAPHOR_TEST_MSSQL pointed at one
		// shared instance - would empty each other's database mid-case. If that day comes, name the database after the run,
		// as the run directory above is named after the process, and accept that a crashed run then leaves one behind per run.
		// NOTE: the same fixed names collide across two LocalDB instances on one machine, and there they fail the create rather
		// than racing: every LocalDB instance defaults a new database's files into the user's profile folder, so once one
		// instance holds DataphorCoverageScripts the next instance cannot create it - "Cannot create file
		// 'C:\Users\...\DataphorCoverageScripts.mdf' because it already exists" (met twice while measuring
		// the supported-version matrix, 2026-11-26). Drop this suite's databases from the first instance before pointing the
		// variable at a second one; the alternative, giving the create an explicit file path, would move where a run leaves
		// its evidence, which is the one thing these fixed names are for.
		public static void EmptyMSSQLDatabase(string databaseName)
		{
			ExecuteOnMSSQL(MSSQLConnectionStringFor("master"), String.Format("if db_id('{0}') is null create database [{0}];", databaseName));
			ExecuteOnMSSQL
			(
				MSSQLConnectionStringFor(databaseName),
				"declare @statements nvarchar(max) = N'';" +
					" select @statements = @statements + N'alter table [' + object_name(parent_object_id) + N'] drop constraint [' + name + N'];' from sys.foreign_keys;" +
					" select @statements = @statements + N'drop table [' + name + N'];' from sys.tables;" +
					" exec sp_executesql @statements;"
			);
		}

		/// <summary>Runs one statement on the configured server, for a pass's own housekeeping.</summary>
		// Through the connection class the device and the store share, so that these housekeeping statements reach the server
		// the way the passes they prepare for do. Public because MSSQLStoreContractTests prepares its own database with it:
		// one place reads the variable, and one place reaches the server to prepare for a pass.
		public static void ExecuteOnMSSQL(string connectionString, string statement)
		{
			using (MSSQLConnection connection = new MSSQLConnection(connectionString))
				connection.Execute(statement);
		}

		/// <summary>
		/// The first column of the first row <paramref name="expression"/> returns on the configured server, as a string, or null
		/// where it returns no row or that column is null.
		/// </summary>
		// Beside ExecuteOnMSSQL and for the same reason: one place reads the variable and one place reaches the server, through
		// the connection class the device and the store share. The client-side cursor explicitly, because these expressions have
		// no from clause and sp_cursoropen - which DetectCursorLocation would choose for a statement starting with select - needs
		// something to range over.
		// IsNull is asked rather than letting Convert.ToString answer, because the cursor hands a null column back as
		// DBNull.Value and Convert.ToString turns that into the empty string - which a caller testing the result for null to
		// mean "the server had no answer" would read as an answer. The functions these expressions call return null for an
		// object that is not there (databasepropertyex of an absent database), so that distinction is the whole point.
		public static string QueryMSSQL(string connectionString, string expression)
		{
			using (MSSQLConnection connection = new MSSQLConnection(connectionString))
			using (SQLCommand command = connection.CreateCommand(true))
			{
				command.CursorLocation = SQLCursorLocation.Client;
				command.CommandType = SQLCommandType.Statement;
				command.LockType = SQLLockType.ReadOnly;
				command.Statement = expression;
				using (SQLCursor cursor = command.Open(SQLCursorType.Dynamic, SQLIsolationLevel.ReadCommitted))
					return cursor.Next() && !cursor.IsNull(0) ? Convert.ToString(cursor[0]) : null;
			}
		}

		#endregion

		#region The configured PostgreSQL server

		/// <summary>
		/// Set to a connection string naming a <em>PostgreSQL server</em> - not a database - to run the passes that need a
		/// live one; see AGENTS.md "Build and test". Every pass that reads it puts its own database on whatever is set.
		/// </summary>
		public const string PGSQLVariable = "DATAPHOR_TEST_PGSQL";

		/// <summary>What <see cref="PGSQLVariable"/> holds, or null where it is unset or empty.</summary>
		public static string PGSQLConnectionString
		{
			get
			{
				string result = Environment.GetEnvironmentVariable(PGSQLVariable);
				return String.IsNullOrEmpty(result) ? null : result;
			}
		}

		/// <summary>Whether this machine has been given a PostgreSQL server to test against.</summary>
		/// <remarks>
		/// The variable being set is the only test, for the reason given on <see cref="MSSQLConfigured"/>: nothing may connect
		/// in order to decide whether to connect.
		/// </remarks>
		public static bool PGSQLConfigured => PGSQLConnectionString != null;

		/// <summary>
		/// What a case says when it is skipped for want of a PostgreSQL server. <paramref name="whatWouldRun"/> completes the
		/// sentence "Set DATAPHOR_TEST_PGSQL ... to &lt;whatWouldRun&gt;."
		/// </summary>
		public static string PGSQLNotConfigured(string whatWouldRun)
		{
			return
				String.Format
				(
					"Set {0} to a PostgreSQL connection string - a server, not a database, for example \"Host=localhost;Port=5432;Username=postgres;Password=<a throwaway password>\" - to {1}.",
					PGSQLVariable,
					whatWouldRun
				);
		}

		/// <summary>
		/// The first of <paramref name="keywords"/> the variable names, or <paramref name="defaultValue"/> where it names none.
		/// </summary>
		/// <remarks>
		/// Read with the framework's own connection-string parser rather than with Npgsql's, because this project must not
		/// reference the PostgreSQL driver: the driver arrives with the D4 library libraries/PGSQLConnection, which the server
		/// loads from the runtime layout, and whether the layout puts it where that library can find it is one of the things a
		/// live run is here to prove. A package reference here would satisfy that load from the test assembly's own folder
		/// instead, because the library resolver is an AssemblyResolve handler and only fires after the default load path has
		/// failed (Base.Windows/LibraryAssemblyResolver.cs). So every keyword spelling the driver accepts has to be listed by
		/// the callers below; DbConnectionStringBuilder matches a key case-insensitively but not across a difference in
		/// spacing, so "User ID" and "UserID" are two entries.
		/// </remarks>
		private static string PGSQLSetting(string defaultValue, params string[] keywords)
		{
			DbConnectionStringBuilder asWritten = new DbConnectionStringBuilder();
			asWritten.ConnectionString = PGSQLConnectionString;
			foreach (string keyword in keywords)
				if (asWritten.ContainsKey(keyword))
					return Convert.ToString(asWritten[keyword]);
			return defaultValue;
		}

		/// <summary>The configured server's host, defaulted as the driver defaults it.</summary>
		public static string PGSQLHost => PGSQLSetting("localhost", "Host", "Server");

		/// <summary>The configured server's port, defaulted as the driver and the device both default it.</summary>
		public static string PGSQLPort => PGSQLSetting("5432", "Port");

		/// <summary>
		/// The login the configured server is reached as. PostgreSQL has no equivalent of the integrated security the SQL
		/// Server passes fall back on, so a device against it always carries a user.
		/// </summary>
		public static string PGSQLUserID => PGSQLSetting(String.Empty, "Username", "User ID", "UserID", "User Id", "User Name", "UserName");

		public static string PGSQLPassword => PGSQLSetting(String.Empty, "Password", "pwd", "psw");

		/// <summary>
		/// The whole of the D4 that gives a server a PostgreSQL device named <paramref name="deviceName"/> against the
		/// configured server, holding its tables in <paramref name="databaseName"/> and reconciling in
		/// <paramref name="reconciliationMode"/> ("command" for a device that data-definition commands reach, "none" for one
		/// that is only read): the create-device statement and the device user after it. Ends in a line break, so a caller can
		/// concatenate it into a script.
		/// </summary>
		/// <remarks>
		/// <para>
		/// One operation rather than two, for the reason <see cref="MSSQLDeviceDefinition"/> gives: a PostgreSQL device carries
		/// no credentials of its own that this driver would be given, so a definition emitted without its device user would
		/// connect as nobody. It is also the one place this device's registered class name is spelled -
		/// "PostgreSQLDevice.PostgreSQLDevice".
		/// </para>
		/// <para>
		/// ShouldDetermineVersion is turned on, where the device defaults it off. What the device does with the version it
		/// reads is almost nothing (MajorVersion decides no SQL this device emits today), but DetermineVersion parses the
		/// string <c>version()</c> returns and a live pass is the only thing that can show whether that still works against a
		/// server nobody has tried - a device that never asked would hide it. The other two connecting settings,
		/// ShouldEnsureDatabase and ShouldEnsureOperators, are left at their defaults, which are on: creating the database and
		/// the DAE_ support functions is what starting this device is expected to do, and the live passes rest on it.
		/// </para>
		/// </remarks>
		public static string PGSQLDeviceDefinition(string deviceName, string databaseName, string reconciliationMode)
		{
			return
				String.Format
				(
					"create device {0} reconciliation {{ mode = {{ {1} }}, master = device }} class \"PostgreSQLDevice.PostgreSQLDevice\" attributes {{ {2} }};\r\n{3}\r\n",
					deviceName,
					reconciliationMode,
					PGSQLDeviceAttributes(databaseName),
					PGSQLDeviceUser(deviceName)
				);
		}

		/// <summary>
		/// The attribute list of a D4 PostgreSQL device definition against the configured server, for a device holding its
		/// tables in <paramref name="databaseName"/>: the host, the port, the database and the version switch above.
		/// </summary>
		// The login is not among them: it goes to a device user instead, so that no password is stored as the literal text of
		// a device definition - which is where one in the variable would otherwise land, and where a run kept with
		// DATAPHOR_TEST_LOGGING=1 would leave it on disk.
		private static string PGSQLDeviceAttributes(string databaseName)
		{
			List<string> attributes = new List<string>();
			attributes.Add(DeviceAttribute("Server", PGSQLHost));
			attributes.Add(DeviceAttribute("Port", PGSQLPort));
			attributes.Add(DeviceAttribute("Database", databaseName));
			attributes.Add(DeviceAttribute("ShouldDetermineVersion", "True"));
			return String.Join(", ", attributes);
		}

		/// <summary>The statement that gives the device named <paramref name="deviceName"/> the login the variable names, to run after the device has been created.</summary>
		/// <remarks>
		/// A device user rather than credentials on the device itself, for the reason <see cref="MSSQLDeviceDefinition"/>
		/// gives: a device user's password is stored encrypted, and Schema.Device has no property the "User Id" tag name this
		/// device's connection-string builder maps would bind to anyway.
		/// </remarks>
		private static string PGSQLDeviceUser(string deviceName)
		{
			return
				String.Format
				(
					"CreateDeviceUser('{0}', '{1}', '{2}', '{3}');",
					Engine.AdminUserID,
					deviceName,
					D4StringLiteral(PGSQLUserID),
					D4StringLiteral(PGSQLPassword)
				);
		}

		/// <summary>
		/// The D4 that drops every table of every non-system schema of the database the started device named
		/// <paramref name="deviceName"/> is pointed at, so that the case about to run starts on a database with nothing of an
		/// earlier run's in it. Ends in a line break, so a caller can concatenate it into a script.
		/// </summary>
		/// <remarks>
		/// <para>
		/// At the start of a case rather than at the end, which is the rule the store contract established and
		/// <see cref="EmptyMSSQLDatabase"/> follows: a run kept with DATAPHOR_TEST_LOGGING=1, or one that crashed, then leaves
		/// its database to be looked at and the next run is still not poisoned by it. Nothing drops these databases afterwards,
		/// because what a device left in one is the evidence of what the device did.
		/// </para>
		/// <para>
		/// Through the device rather than through a connection of the harness's own, which is the opposite of what the SQL
		/// Server passes do and for the reason given on <see cref="PGSQLSetting"/>: this project does not reference the
		/// PostgreSQL driver, so the statement goes out over the device's own connection, through the library the server
		/// loaded. It therefore needs a started device, and a caller runs it after that start has created the database and the
		/// DAE_ support functions - which is also why it drops only tables and leaves those functions alone.
		/// </para>
		/// </remarks>
		// NOTE: the database name is a shared resource with a fixed name, as the SQL Server passes' are. Two runs against one
		// server at the same time - two developers, or a developer and a CI agent, with DATAPHOR_TEST_PGSQL pointed at one
		// shared server - would empty each other's database mid-case. If that day comes, name the database after the run.
		public static string PGSQLEmptyDatabaseStatement(string deviceName)
		{
			// relkind 'r' is an ordinary table, and cascade takes the foreign keys referencing it along, so no separate pass
			// over the constraints is needed. A do block because PostgreSQL has no "drop every table" statement and the
			// device's SQLExecute takes one statement.
			string statement =
				"do $$ declare r record; begin" +
					" for r in select n.nspname as s, c.relname as t from pg_class c" +
					" join pg_namespace n on n.oid = c.relnamespace" +
					" where c.relkind = 'r' and n.nspname not in ('pg_catalog', 'information_schema')" +
					" loop execute 'drop table ' || quote_ident(r.s) || '.' || quote_ident(r.t) || ' cascade'; end loop;" +
					" end $$;";
			return String.Format("SQLDevice.SQLExecute('{0}', '{1}');\r\n", deviceName, D4StringLiteral(statement));
		}

		#endregion
	}
}
