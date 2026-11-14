/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// An in-process server on a fresh temporary directory: Libraries\ holds copies of the runtime layout's library folders it
	/// was given (or of a template instance's), Instance\ its catalog. It opens no port, and with logging off (the default) it
	/// writes neither a log file nor the Windows event log.
	/// </summary>
	// NOTE: two servers in one process share the engine's static state. port-catalog-emitter-race fixed the shared catalog
	// emitter (K1) and the lazily built keyword tables and found no other unsafe static; re-check before fixtures run in parallel.
	public sealed class TestServerInstance : IDisposable
	{
		public const string LibrariesFolderName = "Libraries";
		public const string InstanceFolderName = "Instance";
		public const string CatalogStoreClassName = Server.DefaultCatalogStoreClassName;
		// The spike's connection string, so that results compare with its baseline; store-sqlite-concurrency revisits it.
		public const string CatalogStoreConnectionString = @"Data Source=%CatalogPath%\DAECatalog;Journal Mode=WAL;BusyTimeout=30000";

		/// <summary>Copies the named libraries from the runtime layout. None is valid: System is built in, and the server creates General.</summary>
		public TestServerInstance(string name, params string[] libraryNames)
		{
			_directory = TestEnvironment.NewDirectory(name);
			try
			{
				TestEnvironment.CopyLibraries(Path.Combine(_directory, LibrariesFolderName), libraryNames);
				_server = CreateServer(name);
			}
			catch
			{
				DeleteDirectory();
				throw;
			}
		}

		/// <summary>
		/// Copies the whole directory of a stopped instance (library folders, catalog and library data), so that the server
		/// starts with whatever the template registered. A registered library's assembly is still loaded only once per process,
		/// from the first copy that loaded it.
		/// </summary>
		public TestServerInstance(string name, TestServerInstance template)
		{
			if (template._server.State != ServerState.Stopped)
				throw new InvalidOperationException(String.Format("The template instance in {0} must be stopped before it is copied.", template._directory));

			_directory = TestEnvironment.NewDirectory(name);
			try
			{
				TestEnvironment.CopyDirectory(template._directory, _directory);
				_server = CreateServer(name);
			}
			catch
			{
				DeleteDirectory();
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
			server.LoggingEnabled = TestEnvironment.LoggingEnabled;
			return server;
		}

		private readonly string _directory;
		/// <summary>The absolute path of this instance's directory.</summary>
		public string Directory => _directory;

		private readonly Server _server;
		public Server Server => _server;

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

		/// <summary>Evaluates a D4 expression on a session and process of its own and returns its native value.</summary>
		public object Evaluate(string expression)
		{
			IServerSession session = Connect();
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
				_server.Disconnect(session);
			}
		}

		private bool _disposed;

		/// <summary>
		/// Stops the server if it is running and deletes the directory (unless KeepFiles). A failure to stop is written to the
		/// test output and rethrown unless another exception is already propagating, so that it never hides the test's own failure.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;

			Exception stopError = null;
			if (_server != null && _server.State != ServerState.Stopped)
			{
				try
				{
					_server.Stop();
				}
				catch (Exception exception)
				{
					stopError = exception;
					TestContext.Progress.WriteLine("Stopping the test server in {0} failed: {1}", _directory, exception);
				}
			}

			DeleteDirectory();

			// Non-zero while an exception is unwinding through the caller, as when Dispose runs in the finally of a using block
			// whose body threw.
			if (stopError != null && Marshal.GetExceptionPointers() == IntPtr.Zero)
				ExceptionDispatchInfo.Capture(stopError).Throw();
		}

		/// <summary>
		/// Gives up on a server that a thread still runs a script on: stops it on a background task, waits at most
		/// <paramref name="wait"/> for that, and leaves the directory for the next run's sweep. Dispose then does nothing.
		/// </summary>
		public void Abandon(TimeSpan wait)
		{
			if (_disposed)
				return;
			_disposed = true;

			Task stop = Task.Run(() => _server.Stop());
			try
			{
				if (!stop.Wait(wait))
					TestContext.Progress.WriteLine("Abandoned the test server in {0}; it did not stop within {1} s.", _directory, wait.TotalSeconds);
			}
			catch (AggregateException exception)
			{
				TestContext.Progress.WriteLine("Abandoned the test server in {0}; stopping it failed: {1}", _directory, exception.InnerException.Message);
			}
		}

		private void DeleteDirectory()
		{
			if (TestEnvironment.KeepFiles)
				return;

			try
			{
				System.IO.Directory.Delete(_directory, true);
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				// Typically a library assembly the process loaded from this copy: it stays mapped until the process exits.
				TestContext.Progress.WriteLine("Could not delete {0}; the next test run deletes it. {1}", _directory, exception.Message);
			}
		}
	}
}
