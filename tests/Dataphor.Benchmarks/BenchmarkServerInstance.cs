/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;

namespace DAE.Benchmarks
{
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;

	/// <summary>
	/// Where this program reads the runtime layout from, where BenchmarkDotNet writes its own artifacts, and the
	/// temporary folder the instances live in: one folder per process, under %TEMP%\Dataphor.Benchmarks. Nothing this
	/// program does should reach outside that folder, the layout being read-only to it.
	/// </summary>
	public static class BenchmarkEnvironment
	{
		/// <summary>The AssemblyMetadata key under which the project file records $(DataphorLayoutDir) of the configuration being built.</summary>
		public const string LayoutDirectoryMetadataKey = "DataphorLayoutDir";

		/// <summary>The AssemblyMetadata key under which the project file records $(ArtifactsPath), the build output root.</summary>
		public const string ArtifactsDirectoryMetadataKey = "DataphorArtifactsDir";

		public const string RunRootName = "Dataphor.Benchmarks";

		public const string LibrariesFolderName = "Libraries";

		/// <summary>What the project file recorded under <paramref name="key"/>, or null.</summary>
		private static string Metadata(string key)
		{
			string result = null;
			foreach (AssemblyMetadataAttribute attribute in typeof(BenchmarkEnvironment).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
				if (attribute.Key == key)
					result = attribute.Value;
			return result;
		}

		// Lazy<T> caches a failure, so every benchmark that needs the layout fails with the same message.
		private static readonly Lazy<string> _layoutDirectory = new Lazy<string>(FindLayoutDirectory);
		public static string LayoutDirectory => _layoutDirectory.Value;

		public static string LibrariesDirectory => Path.Combine(LayoutDirectory, LibrariesFolderName);

		private static string FindLayoutDirectory()
		{
			string layoutDirectory = Metadata(LayoutDirectoryMetadataKey);
			if (String.IsNullOrEmpty(layoutDirectory))
				throw new InvalidOperationException(String.Format("This program does not record the runtime layout directory (AssemblyMetadata \"{0}\"). Build it through its project file: dotnet build Dataphor.slnx -c Release", LayoutDirectoryMetadataKey));

			string librariesDirectory = Path.Combine(layoutDirectory, LibrariesFolderName);
			if (!Directory.Exists(librariesDirectory))
				throw new InvalidOperationException(String.Format("The runtime layout is missing: \"{0}\" does not exist. Run dotnet build Dataphor.slnx (with the same -c configuration) before running the benchmarks.", librariesDirectory));

			return layoutDirectory;
		}

		// Lazy<T> caches a failure, as above.
		private static readonly Lazy<string> _artifactsDirectory = new Lazy<string>(FindArtifactsDirectory);
		/// <summary>The build output root, under which this program points BenchmarkDotNet so that its export and its logs land in a git-ignored folder.</summary>
		public static string ArtifactsDirectory => _artifactsDirectory.Value;

		private static string FindArtifactsDirectory()
		{
			string artifactsDirectory = Metadata(ArtifactsDirectoryMetadataKey);
			if (String.IsNullOrEmpty(artifactsDirectory))
				throw new InvalidOperationException(String.Format("This program does not record the build output root (AssemblyMetadata \"{0}\"). Build it through its project file: dotnet build Dataphor.slnx -c Release", ArtifactsDirectoryMetadataKey));
			return artifactsDirectory;
		}

		private static readonly Lazy<string> _runDirectory = new Lazy<string>(CreateRunDirectory);
		/// <summary>This process's folder, %TEMP%\Dataphor.Benchmarks\&lt;process id&gt;-&lt;random&gt;.</summary>
		public static string RunDirectory => _runDirectory.Value;

		private static string CreateRunDirectory()
		{
			string root = Path.Combine(Path.GetTempPath(), RunRootName);
			Directory.CreateDirectory(root);
			DeleteStaleRunDirectories(root);

			string result = Path.Combine(root, String.Format("{0}-{1}", Environment.ProcessId, RandomHex(8)));
			Directory.CreateDirectory(result);

			// Removes the folder only when every instance directory in it was deleted: a library assembly stays mapped until
			// the process exits, so a folder holding one is left for the next run's sweep.
			AppDomain.CurrentDomain.ProcessExit += (sender, args) => { try { Directory.Delete(result, false); } catch (IOException) { } catch (UnauthorizedAccessException) { } };

			return result;
		}

		/// <summary>
		/// Deletes, best effort, the folders of earlier runs whose process is no longer running: a run cannot delete a folder
		/// that holds a library assembly it loaded, because the file stays mapped until the process exits. The same sweep the
		/// test suite does, over this program's own root.
		/// </summary>
		// NOTE: accepted tradeoff - a folder whose process id has been reused by a running process stays until a later run.
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
			return result;
		}

		/// <summary>
		/// Deletes an instance directory, best effort. A folder it cannot delete - typically because the process loaded a
		/// library assembly from it, which stays mapped until the process exits - is left for the next run's sweep, and
		/// silently: every iteration of every benchmark would otherwise print the same line in the middle of a measurement,
		/// and the sweep is what resolves it.
		/// </summary>
		public static void TryDeleteDirectory(string directory)
		{
			try
			{
				Directory.Delete(directory, true);
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		/// <summary>Copies each named library folder of the runtime layout, recursively, into <paramref name="targetDirectory"/>.</summary>
		public static void CopyLibraries(string targetDirectory, IEnumerable<string> names)
		{
			string librariesDirectory = LibrariesDirectory; // checks the layout even when no library is named
			Directory.CreateDirectory(targetDirectory);
			foreach (string name in names)
			{
				string source = Path.Combine(librariesDirectory, name);
				if (!Directory.Exists(source))
					throw new ArgumentException(String.Format("The runtime layout has no library \"{0}\" (expected the folder \"{1}\").", name, source), nameof(names));
				CopyDirectory(source, Path.Combine(targetDirectory, name));
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
	}

	/// <summary>
	/// An in-process server on a fresh temporary directory: Libraries\ holds copies of the runtime layout's library folders it
	/// was given (or of a template instance's), Instance\ its catalog. It opens no port and it does not log, so it writes
	/// neither a log file nor the Windows event log - a logging server in an elevated process creates the event-log source
	/// "Dataphor Server" and writes to it, and a benchmark has no business doing that.
	/// </summary>
	/// <remarks>
	/// Never point a server at the runtime layout or at the repository: a running server writes into its library directory -
	/// it creates General\ and may rewrite .d4l files - so each instance gets a copy of the library folders it needs. The
	/// root AGENTS.md records the rule under "Rules that are easy to break" and the whole test suite follows it.
	/// </remarks>
	// NOTE: accepted tradeoff - this duplicates the part of tests/DAE.Server.Tests/TestServerInstance.cs and TestEnvironment.cs
	// that a benchmark needs. Referencing the test project instead would drag NUnit and the test SDK into a console program,
	// and both of those types write to TestContext.Progress on their cleanup and keep-files paths, which is not a thing a
	// program outside an NUnit run can rely on. Weighed against those and kept. Revisit if a third consumer appears: then
	// extract the instance helpers into a small support project that the tests and this program both reference.
	public sealed class BenchmarkServerInstance : IDisposable
	{
		public const string LibrariesFolderName = BenchmarkEnvironment.LibrariesFolderName;
		public const string InstanceFolderName = "Instance";
		public const string CatalogStoreClassName = Server.DefaultCatalogStoreClassName;
		// No connection options: the SQLite store applies write-ahead logging and its busy timeout itself, so a benchmark
		// measures the options a fresh instance gets.
		public const string CatalogStoreConnectionString = @"Data Source=%CatalogPath%\DAECatalog";

		/// <summary>Copies the named libraries from the runtime layout. None is valid: System is built in, and the server creates General.</summary>
		public BenchmarkServerInstance(string name, params string[] libraryNames)
		{
			_nextObjectID = Alphora.Dataphor.DAE.Schema.Object.GetNextObjectID();
			_directory = BenchmarkEnvironment.NewDirectory(name);
			try
			{
				BenchmarkEnvironment.CopyLibraries(Path.Combine(_directory, LibrariesFolderName), libraryNames);
				_server = CreateServer(name);
			}
			catch
			{
				Release();
				throw;
			}
		}

		/// <summary>
		/// Copies the whole directory of a stopped instance (library folders, catalog and library data), so that the server
		/// starts with whatever the template registered. This is what makes a warm-catalog measurement possible: the copy's
		/// store already holds a catalog, so starting it is the store's read path rather than its write path.
		/// </summary>
		public BenchmarkServerInstance(string name, BenchmarkServerInstance template)
		{
			if (template.Server.State != ServerState.Stopped)
				throw new InvalidOperationException(String.Format("The template instance in {0} must be stopped before it is copied.", template._directory));

			_nextObjectID = Alphora.Dataphor.DAE.Schema.Object.GetNextObjectID();
			_directory = BenchmarkEnvironment.NewDirectory(name);
			try
			{
				BenchmarkEnvironment.CopyDirectory(template._directory, _directory);
				_server = CreateServer(name);
			}
			catch
			{
				Release();
				throw;
			}
		}

		private Server CreateServer(string name)
		{
			// No PortNumber: ApplyTo does not pass it, and an in-process server listens on nothing.
			ServerConfiguration configuration =
				new ServerConfiguration
				{
					Name = name,
					LibraryDirectories = Path.Combine(_directory, LibrariesFolderName),
					InstanceDirectory = Path.Combine(_directory, InstanceFolderName),
					CatalogStoreClassName = CatalogStoreClassName,
					CatalogStoreConnectionString = CatalogStoreConnectionString
				};
			Server server = new Server();
			configuration.ApplyTo(server);
			server.LoggingEnabled = false;
			return server;
		}

		private readonly string _directory;
		/// <summary>The absolute path of this instance's directory.</summary>
		public string Directory => _directory;

		private readonly Server _server;
		public Server Server => _server;

		/// <summary>The process-wide object-id counter as it stood before this instance existed, handed back when the instance is disposed.</summary>
		/// <remarks>
		/// Starting an engine sets the engine's process-wide object-id counter from that engine's own catalog
		/// (bug-object-id-counter-is-process-wide), so a program that starts dozens of engines in one process would
		/// otherwise leave later ones allocating ids an earlier catalog already holds. An engine that started and was then
		/// stopped has moved the counter just as much as one that stayed up, which is why the counter is captured per
		/// instance rather than around one call: a benchmark's start and its stop are in different methods.
		/// TestEnvironment.WithTheObjectIDCounterRestored is the same fix in the shape the test suite needs.
		/// </remarks>
		private readonly int _nextObjectID;

		public IServerSession Connect()
		{
			return _server.Connect(new SessionInfo("Admin", ""));
		}

		/// <summary>Runs D4 on a session and process of its own.</summary>
		public void ExecuteScript(string script)
		{
			IServerSession session = Connect();
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					process.ExecuteScript(script);
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				_server.Disconnect(session);
			}
		}

		private bool _disposed;

		/// <summary>Stops the server if it is running, deletes the directory and hands the process its object-id counter back.</summary>
		/// <remarks>
		/// A failure to stop is rethrown rather than reported and swallowed, as the test suite's equivalent does: a
		/// benchmark has no test output to report it to, and an iteration whose server would not stop has not left the
		/// process in the state the next iteration measures, so the run must end rather than publish the numbers after it.
		/// </remarks>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;

			try
			{
				if (_server != null && _server.State != ServerState.Stopped)
					_server.Stop();
			}
			finally
			{
				Release();
			}
		}

		private void Release()
		{
			BenchmarkEnvironment.TryDeleteDirectory(_directory);
			Alphora.Dataphor.DAE.Schema.Object.SetNextObjectID(_nextObjectID);
		}
	}
}
