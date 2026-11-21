/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// One Dataphor server running as a separate program, for the life of a fixture: a temporary root with an instance
	/// list of its own, a free loopback port, and the test server program (tests/TestHarness) started on it and stopped
	/// again.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Shaped like <see cref="HostedInstance"/>, which does the same for a host in the test's own process, and it reuses
	/// that class's port reservation and configuration. What the separate program adds is that the server's engine is
	/// genuinely not this process's: no static the engine keeps - the object-id counter among them - is shared between
	/// the two sides, which is how the product runs and what no in-process fixture can show.
	/// </para>
	/// <para>
	/// The child is stopped by a line on its standard input, and also stops when that input reaches its end, which is
	/// what happens when this process dies; see tests/TestHarness/Program.cs. Both paths matter, because either alone
	/// can fail: a crashed test run never reaches <see cref="Stop"/>, and a child wedged before it reads its input
	/// would not notice the pipe close.
	/// </para>
	/// </remarks>
	internal sealed class OutOfProcessInstance : IDisposable
	{
		/// <summary>The folder under the root holding the child's instance list, which is what DATAPHOR_INSTANCE_DIRECTORY names.</summary>
		private const string InstancesFolderName = "Instances";

		// The test server program's command line. Spelled here because this project does not compile against that
		// program; tests/TestHarness/Program.cs declares the same strings as constants and is the one to change.
		private const string ServeMode = "serve";
		private const string InstanceOption = "--instance";
		private const string HostNameOption = "--host-name";
		private const string LoggingOption = "--logging";
		private const string ReadyPrefix = "READY";

		/// <summary>How long the child is given to report itself ready, counted from the moment it was started.</summary>
		/// <remarks>Generous: a first start creates the catalog store from scratch and registers the General library.</remarks>
		private const int ReadyTimeoutMilliseconds = 120000;

		/// <summary>How often the wait for readiness looks; it only bounds how long a child that failed to start is waited on.</summary>
		private const int ReadyPollMilliseconds = 50;

		/// <summary>How long the child is given to exit after it is asked to.</summary>
		/// <remarks>
		/// Well above the ten seconds NetTcpServiceHost gives Kestrel to close connections a client still holds open,
		/// plus the engine's own shutdown: the point of asking is to find out that the child stops by itself.
		/// </remarks>
		private const int StopTimeoutMilliseconds = 60000;

		/// <summary>How long a killed child is given to go.</summary>
		private const int KillTimeoutMilliseconds = 15000;

		public OutOfProcessInstance(string instanceName)
		{
			_instanceName = instanceName;
			_directory = TestEnvironment.NewDirectory(instanceName);
			try
			{
				// ShouldListen false: one listener per machine serves every instance on it, so a test instance has no
				// business taking that port. The child therefore opens exactly one port, the one reserved here.
				_portNumber = HostedInstance.ReserveLoopbackPorts(1)[0];

				// No libraries named: System is built in, and the server creates General - in this copy, never in the
				// runtime layout or the repository. This is also what makes the library directory the server requires.
				TestEnvironment.CopyLibraries(Path.Combine(_directory, TestServerInstance.LibrariesFolderName), new string[0]);

				_instanceDirectory = Path.Combine(_directory, InstancesFolderName);
				Directory.CreateDirectory(_instanceDirectory);
				WriteInstanceList(HostedInstance.Configuration(_instanceName, _directory, _portNumber, false, 0));

				Start();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		/// <summary>The instance's own temporary root: its instance list, its library copy and its catalog are all under it.</summary>
		private readonly string _directory;

		private readonly string _instanceDirectory;

		private readonly string _instanceName;
		public string InstanceName => _instanceName;

		/// <summary>The host name the child listens on and a client connects to; the loopback literal, so no other machine can reach it.</summary>
		public string HostName => HostedInstance.LoopbackAddress;

		private readonly int _portNumber;
		/// <summary>The child's port; never 8060 or 8061, and never a port a real instance could own.</summary>
		public int PortNumber => _portNumber;

		private Process _process;

		private int? _exitCode;
		/// <summary>The child's exit code once it has stopped, or null while it is still running.</summary>
		public int? ExitCode => _exitCode;

		private bool _wasStarted;
		private bool _wasKilled;

		private readonly object _outputLock = new object();
		private readonly StringBuilder _output = new StringBuilder();
		private readonly StringBuilder _error = new StringBuilder();

		/// <summary>The child's ready line, set by the output reader on a thread of its own.</summary>
		private volatile string _readyLine;

		/// <summary>Writes the child's whole instance list: one instance, the one it is started on.</summary>
		/// <remarks>
		/// A plain file write, through the same serializer the product reads the file with, rather than
		/// InstanceManager.Save - which writes wherever the *test* process resolves its instance directory to. Nothing
		/// here touches the static InstanceManager.InstanceDirectory, so no other fixture in this process is disturbed
		/// by this one, and this one depends on nothing another fixture has set.
		/// </remarks>
		private void WriteInstanceList(ServerConfiguration configuration)
		{
			InstanceConfiguration instances = new InstanceConfiguration();
			instances.Instances.Add(configuration);
			instances.Save(Path.Combine(_instanceDirectory, InstanceManager.InstanceConfigurationFileName));
		}

		private void Start()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(TestEnvironment.TestHarnessPath);
			startInfo.ArgumentList.Add(ServeMode);
			startInfo.ArgumentList.Add(InstanceOption);
			startInfo.ArgumentList.Add(_instanceName);
			startInfo.ArgumentList.Add(HostNameOption);
			startInfo.ArgumentList.Add(HostName);
			// The suite's own diagnosis switch reaches the child too, so its instance log is in the folder that is kept.
			if (TestEnvironment.LoggingEnabled)
				startInfo.ArgumentList.Add(LoggingOption);

			// The child's whole view of where instances live. Its environment is otherwise this process's, so a
			// developer who has this variable set for their own use has it overwritten here, which is what we want.
			startInfo.Environment[InstanceManager.InstanceDirectoryVariableName] = _instanceDirectory;
			// So that anything the child resolves relative to its working directory lands under this temporary root.
			startInfo.WorkingDirectory = _directory;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;

			_process = new Process();
			_process.StartInfo = startInfo;
			_process.OutputDataReceived += OnOutputDataReceived;
			_process.ErrorDataReceived += OnErrorDataReceived;
			_process.Start();
			_wasStarted = true;

			// Drained from the moment the process starts, and asynchronously: a child that fills a pipe buffer nobody
			// reads stops, and reading one stream to its end while the other fills up deadlocks.
			_process.BeginOutputReadLine();
			_process.BeginErrorReadLine();

			if (!WaitForReadyOrExit())
				throw new InvalidOperationException(Diagnostic(String.Format("The test server did not report itself ready within {0} seconds.", ReadyTimeoutMilliseconds / 1000)));

			CheckReadyLine();

			// A child that printed its ready line and then went is a ready line with no server behind it: without this
			// every case would fail on a refused connection instead of on what actually happened.
			if (_process.HasExited)
				throw new InvalidOperationException(Diagnostic("The test server reported itself ready and then exited."));
		}

		/// <summary>Waits until the child reports itself ready, exits, or runs out of time; false if it is not ready.</summary>
		/// <remarks>
		/// Polled rather than driven by Process.Exited, which fires on a thread-pool thread and so could reach this
		/// object after it has been disposed.
		/// </remarks>
		private bool WaitForReadyOrExit()
		{
			for (int waited = 0; waited < ReadyTimeoutMilliseconds; waited += ReadyPollMilliseconds)
			{
				if (_readyLine != null)
					return true;

				if (_process.HasExited)
				{
					// The overload with no timeout also waits for the asynchronous readers, so a ready line printed just
					// before the exit is in hand, and the diagnostic has everything the child said. See the NOTE in Stop
					// for what that wait depends on.
					_process.WaitForExit();
					return _readyLine != null;
				}

				Thread.Sleep(ReadyPollMilliseconds);
			}
			return _readyLine != null;
		}

		/// <summary>Checks that the child is serving the configuration this class wrote, which is what its ready line says.</summary>
		private void CheckReadyLine()
		{
			string expected = String.Format("{0} {1} {2} listening=false", ReadyPrefix, _instanceName, _portNumber);
			if (_readyLine != expected)
				throw new InvalidOperationException
				(
					Diagnostic
					(
						String.Format
						(
							"The test server reported \"{0}\" rather than \"{1}\", so it is not serving the instance, the port or the listener setting this fixture wrote for it.",
							_readyLine,
							expected
						)
					)
				);
		}

		private void OnOutputDataReceived(object sender, DataReceivedEventArgs arguments)
		{
			if (arguments.Data == null)
				return;

			lock (_outputLock)
				_output.AppendLine(arguments.Data);

			if ((_readyLine == null) && arguments.Data.StartsWith(ReadyPrefix + " ", StringComparison.Ordinal))
				_readyLine = arguments.Data;
		}

		private void OnErrorDataReceived(object sender, DataReceivedEventArgs arguments)
		{
			if (arguments.Data == null)
				return;

			lock (_outputLock)
				_error.AppendLine(arguments.Data);
		}

		/// <summary>Everything there is to say about the child, for the message of a failing assertion.</summary>
		/// <remarks>Its own output included: a child that failed to start said why there, and nowhere else.</remarks>
		public string Diagnostic(string problem)
		{
			string state = DescribeState();
			lock (_outputLock)
				return String.Format
				(
					"{0}{1}The test server process ({2}) {3}.{1}Its standard output:{1}{4}Its standard error:{1}{5}",
					problem,
					Environment.NewLine,
					TestEnvironment.TestHarnessPath,
					state,
					_output,
					_error
				);
		}

		private string DescribeState()
		{
			if (_process == null)
			{
				if (!_wasStarted)
					return "was never started";
				if (_exitCode == null)
					return _wasKilled ? "was killed and was not seen to exit within the time allowed" : "was not seen to exit";
				return String.Format("exited with code {0}{1}", _exitCode, _wasKilled ? " after being killed, because it did not stop when it was asked to" : "");
			}

			try
			{
				return _process.HasExited ? String.Format("exited with code {0}", _process.ExitCode) : "is still running";
			}
			catch (InvalidOperationException exception)
			{
				return "could not be inspected: " + exception.Message;
			}
		}

		/// <summary>
		/// Stops the child: a line on its standard input, then a bounded wait, then a kill of it and anything it
		/// started. Does nothing the second time, so a case may stop the child and the fixture still dispose this object.
		/// </summary>
		public void Stop()
		{
			Process process = _process;
			if (process == null)
				return;
			_process = null;

			using (process)
			{
				AskToStop(process);
				if (!process.WaitForExit(StopTimeoutMilliseconds))
				{
					_wasKilled = true;
					try
					{
						process.Kill(entireProcessTree: true);
					}
					// It exited between the wait above and here, so there is nothing left to kill.
					catch (InvalidOperationException) { }
					process.WaitForExit(KillTimeoutMilliseconds);
				}

				if (process.HasExited)
				{
					// The overload with no timeout also waits for the asynchronous readers, so the captured output is complete.
					// NOTE: that wait ends when both pipes reach end of file, which is when the last handle to them closes - so
					// it would not end if the child ever started a process of its own that outlived it and inherited them. The
					// test server starts none; if it is ever given a mode that does, bound this wait.
					process.WaitForExit();
					_exitCode = process.ExitCode;
				}
			}
		}

		/// <summary>Writes the line that asks the child to stop; see tests/TestHarness/Program.cs.</summary>
		/// <remarks>
		/// A child that has already gone leaves a broken pipe, which is not a failure here: the wait that follows finds
		/// it exited and the kill is skipped.
		/// </remarks>
		private static void AskToStop(Process process)
		{
			try
			{
				process.StandardInput.WriteLine();
				process.StandardInput.Flush();
			}
			catch (IOException) { }
			catch (ObjectDisposedException) { }
		}

		/// <summary>Stops the child and removes its folder, on the constructor's failure path as well as a fixture's teardown.</summary>
		public void Dispose()
		{
			try
			{
				Stop();
			}
			finally
			{
				// Only after the exit wait: a folder the child still has files open in cannot be removed, and
				// TryDeleteDirectory reports and leaves one it cannot remove for the next run's sweep.
				TestEnvironment.TryDeleteDirectory(_directory);
			}
		}
	}
}
