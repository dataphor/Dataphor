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

using NUnit.Framework;

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

		/// <summary>Set to 1 to turn on the engine's logging and keep every test directory; see AGENTS.md "Build and test".</summary>
		public const string LoggingVariable = "DATAPHOR_TEST_LOGGING";

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

		private static string FindLayoutDirectory()
		{
			string layoutDirectory = null;
			foreach (AssemblyMetadataAttribute attribute in typeof(TestEnvironment).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
				if (attribute.Key == LayoutDirectoryMetadataKey)
					layoutDirectory = attribute.Value;

			if (String.IsNullOrEmpty(layoutDirectory))
				throw new InvalidOperationException(String.Format("The test assembly does not record the runtime layout directory (AssemblyMetadata \"{0}\"). Build it through its project file: dotnet build Dataphor.slnx", LayoutDirectoryMetadataKey));

			string librariesDirectory = Path.Combine(layoutDirectory, "Libraries");
			if (!Directory.Exists(librariesDirectory))
				throw new InvalidOperationException(String.Format("The runtime layout is missing: \"{0}\" does not exist. Run dotnet build Dataphor.slnx (with the same -c configuration) before running the tests.", librariesDirectory));

			return layoutDirectory;
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
}
