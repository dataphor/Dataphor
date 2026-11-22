/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Layer L1 of the test suite (docs/modernization-plan.md, WS3): one test case per D4 script in the runtime layout's copy of
	/// TestFramework.Coverage.Scripts/Documents, each run on a server of its own and checked against its entry in
	/// <see cref="CoverageManifest"/>. Every server is a copy of one template instance, built once per run, so that a script's
	/// result does not depend on which scripts ran before it or on whether it runs alone.
	/// </summary>
	// Not [Parallelizable]: each case starts a server, and two engines in one process share the engine's static state (see
	// the note on TestServerInstance). A linkedServer case also sets DataphorServiceUtility.ListenerPortNumber, which is
	// process-wide, for the length of its own script - which is safe only because one case runs at a time.
	[TestFixture]
	public class CoverageScripts
	{
		/// <summary>
		/// The library folders copied for every script's server: a fixed list rather than all of Libraries, so that the libraries
		/// a script can see stay the same. The set the spike measured, without MSSQLDevice: nothing the template registers needs
		/// a SQL Server (K14), plus TestFramework.Perf, which is two text files and is registered only by the entries that name
		/// it in <c>register</c> (the timing scripts' BenchmarkExecute operator; test-perf-timing-scripts).
		/// </summary>
		public static readonly string[] Libraries =
		[
			"Frontend",
			"SimpleDevice",
			"TestFramework.Coverage.Base",
			"SQLDevice",
			"Connection",
			"TestFramework",
			"TestFramework.Coverage.Scripts",
			"TestFramework.Perf",
			"System.Internet"
		];

		public const string ScriptsLibrary = "TestFramework.Coverage.Scripts";
		public const string ScriptExtension = ".d4";

		/// <summary>
		/// The instance name a linkedServer entry's child server is started on: <see cref="Engine.DefaultServerName"/>, not
		/// a name of this suite's choosing, because that is what a server link with no InstanceName tag resolves to.
		/// </summary>
		private const string LinkedServerInstanceName = Engine.DefaultServerName;

		// The spike's size. A stack overflow kills the whole test host, and whether 1 MB suffices for every script is unmeasured.
		private const int WorkerStackSize = 16 * 1024 * 1024;

		// How long a timed-out script's thread gets to end once its process is aborted, and an abandoned server to stop.
		private static readonly TimeSpan AbortWait = TimeSpan.FromSeconds(10);

		public enum CaseKind { Script, NotInManifest, FileMissing, FragmentNotMentioned, ManifestInvalid, LayoutMissing }

		public sealed class CoverageCase
		{
			public CoverageCase(CaseKind kind, string name, CoverageEntry entry, string message)
			{
				Kind = kind;
				Name = name;
				Entry = entry;
				Message = message;
			}

			public CaseKind Kind { get; }
			public string Name { get; }
			public CoverageEntry Entry { get; }
			public string Message { get; }

			public override string ToString() => Name;
		}

		/// <summary>
		/// The test cases, in ordinal order of name. NUnit evaluates this at discovery and again at execution, so it reads only
		/// the embedded manifest, the layout's folder listing and the scripts a fragment entry names in partOf, and starts no server.
		/// </summary>
		public static IEnumerable<TestCaseData> Cases()
		{
			return Discover().Select(coverageCase => new TestCaseData(coverageCase).SetName(coverageCase.Name));
		}

		private static List<CoverageCase> Discover()
		{
			List<CoverageCase> problems = new List<CoverageCase>();

			CoverageManifest manifest = CoverageManifest.Load(Libraries);
			if (manifest.Problems.Count > 0)
				problems.Add
				(
					new CoverageCase
					(
						CaseKind.ManifestInvalid,
						"Harness.ManifestInvalid",
						null,
						String.Format("{0} has {1} problem(s):{2}", CoverageManifest.ResourceName, manifest.Problems.Count, String.Concat(manifest.Problems.Select(problem => Environment.NewLine + "  " + problem)))
					)
				);

			string layoutProblem = FindScripts(out string documents, out List<string> scripts);
			if (layoutProblem != null)
				problems.Add(new CoverageCase(CaseKind.LayoutMissing, "Harness.LayoutMissing", null, layoutProblem));

			if (problems.Count > 0)
				return problems;

			Dictionary<string, CoverageEntry> entries = manifest.Entries.ToDictionary(entry => entry.Script, StringComparer.Ordinal);
			HashSet<string> files = new HashSet<string>(scripts, StringComparer.Ordinal);
			List<CoverageCase> cases = new List<CoverageCase>();
			foreach (string name in files.Union(entries.Keys).OrderBy(name => name, StringComparer.Ordinal))
			{
				bool hasFile = files.Contains(name);
				if (!entries.TryGetValue(name, out CoverageEntry entry))
					cases.Add(new CoverageCase(CaseKind.NotInManifest, name, null, String.Format("The coverage script {0}{1} is not in the manifest; add an entry for it to tests/DAE.Server.Tests/{2}.", name, ScriptExtension, CoverageManifest.ResourceName)));
				else if (!hasFile)
					cases.Add(new CoverageCase(CaseKind.FileMissing, name, entry, String.Format("The manifest has an entry for {0}, but the script file is missing from {1}/Documents.", name, ScriptsLibrary)));
				else if (entry.Status == CoverageStatus.Fragment && FindUnmentioningParent(documents, files, entry) is string parent)
					cases.Add(new CoverageCase(CaseKind.FragmentNotMentioned, name, entry, String.Format("{0} is listed as part of {1}, but {1}{2} does not mention it; correct partOf or the entry's status.", name, parent, ScriptExtension)));
				else
					cases.Add(new CoverageCase(CaseKind.Script, name, entry, null));
			}
			return cases;
		}

		/// <summary>
		/// The first script in a fragment's partOf whose file text does not mention the fragment as a D4 string literal, or null.
		/// A text check, so a mention in a comment counts; it catches a parent that renamed or dropped the step. A parent with no
		/// file is skipped: its own case reports that.
		/// </summary>
		private static string FindUnmentioningParent(string documents, HashSet<string> files, CoverageEntry fragment)
		{
			foreach (string parent in fragment.PartOf)
			{
				if (!files.Contains(parent))
					continue;
				string text = File.ReadAllText(Path.Combine(documents, parent + ScriptExtension));
				if (!text.Contains("\"" + fragment.Script + "\"", StringComparison.Ordinal) && !text.Contains("'" + fragment.Script + "'", StringComparison.Ordinal))
					return parent;
			}
			return null;
		}

		/// <summary>Returns null and the script names, or a description of what is wrong with the layout.</summary>
		private static string FindScripts(out string documents, out List<string> scripts)
		{
			scripts = new List<string>();
			documents = null;
			try
			{
				documents = Path.Combine(TestEnvironment.LibrariesDirectory, ScriptsLibrary, "Documents");
			}
			catch (InvalidOperationException exception)
			{
				return exception.Message;
			}

			if (Directory.Exists(documents))
				foreach (string file in Directory.GetFiles(documents))
					if (String.Equals(Path.GetExtension(file), ScriptExtension, StringComparison.OrdinalIgnoreCase))
						scripts.Add(Path.GetFileNameWithoutExtension(file));

			if (scripts.Count == 0)
				return String.Format("The runtime layout has no coverage scripts: \"{0}\" is missing or holds no {1} file. Run dotnet build Dataphor.slnx (with the same -c configuration) before running the tests.", documents, ScriptExtension);
			return null;
		}

		private TestServerInstance _template;
		private string _templateError;
		private readonly Summary _summary = new Summary();

		[OneTimeSetUp]
		public void BuildTemplate()
		{
			if (!Discover().Any(coverageCase => coverageCase.Kind == CaseKind.Script))
				return;

			Stopwatch watch = Stopwatch.StartNew();
			try
			{
				_template = new TestServerInstance("Template", Libraries);
				_template.Server.Start();
				RegisterInTemplate("Frontend");
				RegisterInTemplate("TestFramework.Coverage.Base");
				CheckNoExternalDevice();
				_template.Server.Stop();
			}
			catch (Exception exception)
			{
				_templateError = ErrorText(exception);
			}
			TestContext.Progress.WriteLine("Coverage template instance {0} in {1:F1} s.", _templateError == null ? "built" : "failed: " + _templateError, watch.Elapsed.TotalSeconds);
		}

		private void RegisterInTemplate(string library)
		{
			try
			{
				_template.ExecuteScript(String.Format("EnsureLibraryRegistered('{0}');", library));
			}
			catch (Exception exception)
			{
				throw new InvalidOperationException(String.Format("Registering {0} failed.", library), exception);
			}
			TestContext.Progress.WriteLine("Coverage template instance: registered {0}.", library);
		}

		/// <summary>
		/// K14: the helper library must register with no external DBMS. It once required TestFramework to load MSSQLDevice and
		/// create the SQL Server device TestingDevice, which now belongs to TestFramework.Coverage.Devices; this fails the run if
		/// an edit brings either back into the template.
		/// </summary>
		private void CheckNoExternalDevice()
		{
			if ((bool)_template.Evaluate("exists (LoadedLibraries where Name = 'MSSQLDevice')"))
				throw new InvalidOperationException("Registering TestFramework.Coverage.Base loaded MSSQLDevice; it must register with no external DBMS.");
			if ((bool)_template.Evaluate("exists (Devices where Name like '%TestingDevice')"))
				throw new InvalidOperationException("Registering TestFramework.Coverage.Base created a device named TestingDevice; it must register with no external DBMS.");
		}

		[OneTimeTearDown]
		public void WriteSummary()
		{
			try
			{
				TestContext.Progress.WriteLine(_summary.ToString());
			}
			finally
			{
				if (_template != null)
				{
					_template.Dispose();
					_template = null;
				}
			}
		}

		[TestCaseSource(nameof(Cases))]
		public void Script(CoverageCase coverageCase)
		{
			_summary.Cases++;
			if (coverageCase.Kind != CaseKind.Script)
			{
				_summary.DiscoveryProblems++;
				Assert.Fail(coverageCase.Message);
				return;
			}

			if (coverageCase.Entry.Status == CoverageStatus.Fragment)
			{
				_summary.Fragments++;
				Assert.Warn(String.Format("fragment: run by {0}; not run on its own", String.Join(", ", coverageCase.Entry.PartOf)));
				return;
			}

			if (_templateError != null)
			{
				_summary.TemplateFailures++;
				Assert.Fail("The coverage template instance could not be built: " + _templateError);
				return;
			}

			Evaluate(coverageCase.Entry, Execute(coverageCase.Entry));
		}

		/// <summary>Runs the script, with the second Dataphor server its entry asks for; returns null if it succeeded, else its error text.</summary>
		private string Execute(CoverageEntry entry)
		{
			if (entry.LinkedServer != true)
				return RunScript(entry);

			// Process-wide, which is why it is restored at the end rather than left for the next case to find: nothing else
			// in the suite resolves through a listener without naming its port, so a leak would break the first case that
			// ever did, far from here. One case at a time: this fixture is not Parallelizable and Execute runs one script
			// per call.
			int listenerPortNumber = DataphorServiceUtility.ListenerPortNumber;
			try
			{
				// A child process, not a second server in this one: the script resolves the link through the listener
				// service, so the linked server has to be hosted on a port, which an in-process server is not. (It was
				// also the only safe arrangement while two engines in one process shared the object-id counter and each
				// script's own server reset it as it started; bug-object-id-counter-is-process-wide removed that reason by
				// giving each engine a generator of its own.) The cost falls on this one case.
				using (OutOfProcessInstance linkedServer = new OutOfProcessInstance(LinkedServerInstanceName, OutOfProcessInstance.ServerDefaultConnectionIdleTime, true))
				{
					DataphorServiceUtility.ListenerPortNumber = linkedServer.ListenerPortNumber;
					return RunScript(entry);
				}
			}
			finally
			{
				// After the child is stopped, not before. On the abandon path the script's worker thread is still running,
				// and a link it resolves once this is back at the product's own 8060 would be answered by a real listener
				// on a machine with a real Dataphor installation - pointing the script at a real instance, which is the
				// one outcome this whole arrangement exists to prevent. Left at the dead child's port, that resolution
				// fails instead.
				DataphorServiceUtility.ListenerPortNumber = listenerPortNumber;
			}
		}

		/// <summary>Runs the script on a server copied from the template; returns null if it succeeded, else its error text.</summary>
		private string RunScript(CoverageEntry entry)
		{
			TestServerInstance instance = new TestServerInstance(entry.Script, _template);
			bool abandoned = false;
			try
			{
				instance.Server.Start();
				string error = RunOnWorker(instance, entry, out abandoned);
				if (abandoned)
				{
					// NOTE: an abandoned server can still be stopping on its background task while later cases run, and servers share
					// static engine state (see TestServerInstance). No script reaches this today; if one does and a later case then
					// fails, run that case alone before diagnosing it.
					instance.Abandon(AbortWait);
					return error;
				}

				try
				{
					instance.Server.Stop();
				}
				catch (Exception exception)
				{
					error = Append(error, "HarnessStop: " + ErrorText(exception));
				}
				return error;
			}
			finally
			{
				if (!abandoned)
					instance.Dispose();
			}
		}

		private sealed class Worker
		{
			public volatile IServerProcess Process;
			public string Error;
		}

		private static string RunOnWorker(TestServerInstance instance, CoverageEntry entry, out bool abandoned)
		{
			abandoned = false;
			Worker worker = new Worker();
			Thread thread = new Thread(() => RunInProcess(instance, entry, worker), WorkerStackSize);
			thread.IsBackground = true;
			thread.Name = "Coverage " + entry.Script;
			thread.Start();

			if (thread.Join(TimeSpan.FromSeconds(entry.Timeout)))
				return worker.Error;

			// NUnit's Timeout attribute needs thread abort, which .NET 10 lacks. The engine checks IsAborted at every plan
			// node, so TerminateProcessThread ends a looping script cooperatively; it throws (ProcessNotResponding) when the
			// thread does not leave the process in time. Not StopProcess: it also disposes the process, which the worker's own
			// cleanup does at the same moment once the script returns.
			ServerProcess process = (ServerProcess)worker.Process;
			if (process != null)
			{
				try
				{
					instance.Server.TerminateProcessThread(process);
				}
				catch (Exception exception)
				{
					TestContext.Progress.WriteLine("Aborting {0} after its timeout: {1}", entry.Script, ErrorText(exception));
				}
			}

			string error = String.Format("HarnessTimeout: script did not finish within {0} s", entry.Timeout);
			if (!thread.Join(AbortWait))
			{
				abandoned = true;
				error += "; it did not stop when aborted and its server was abandoned";
			}
			return error;
		}

		/// <summary>
		/// The worker thread's body. One session and one process: TestProcessExecutes requires that the script's session is the
		/// only one besides System's.
		/// </summary>
		private static void RunInProcess(TestServerInstance instance, CoverageEntry entry, Worker worker)
		{
			IServerSession session = null;
			IServerProcess process = null;
			try
			{
				session = instance.Connect();
				process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				worker.Process = process;

				foreach (string library in entry.Register ?? [])
				{
					try
					{
						process.ExecuteScript(String.Format("EnsureLibraryRegistered('{0}');", library));
					}
					catch (Exception exception)
					{
						worker.Error = String.Format("HarnessRegister: {0} <= {1}", library, ErrorText(exception));
						return;
					}
				}

				if (entry.Preamble != null)
				{
					process.ExecuteScript(entry.Preamble);

					// A preamble of SetUseImplicitTransactions(false) runs inside the implicit transaction of its own call, which the
					// engine then leaves open (it ends implicit transactions only while they are on), so the script would run inside it
					// and the objects it creates would be invisible to the other sessions and processes it starts.
					while (process.InTransaction)
						process.CommitTransaction();
				}

				if (entry.Batches == true)
					process.ExecuteScript(File.ReadAllText(ServerScriptPath(instance, entry.Script)));
				else
					process.ExecuteScript(String.Format("ExecuteScript('Coverage.Scripts', '{0}');", entry.Script));
			}
			catch (Exception exception)
			{
				worker.Error = ErrorText(exception);
			}
			finally
			{
				// Nothing may escape: an unhandled exception on this thread would end the test host. After a timeout the test
				// has already moved on, and the errors here (the abort disposed the process) are not read.
				try
				{
					if (process != null)
						session.StopProcess(process);
				}
				catch (Exception exception)
				{
					worker.Error = Append(worker.Error, "HarnessCleanup: " + ErrorText(exception));
				}

				try
				{
					if (session != null)
						instance.Server.Disconnect(session);
				}
				catch (Exception exception)
				{
					worker.Error = Append(worker.Error, "HarnessCleanup: " + ErrorText(exception));
				}
			}
		}

		/// <summary>
		/// The script's file in the server's own copy of the scripts library, so that a batches entry runs the text that the
		/// nested ExecuteScript('Coverage.Scripts', name) would load.
		/// </summary>
		private static string ServerScriptPath(TestServerInstance instance, string script)
		{
			return Path.Combine(instance.Directory, TestServerInstance.LibrariesFolderName, ScriptsLibrary, "Documents", script + ScriptExtension);
		}

		private void Evaluate(CoverageEntry entry, string error)
		{
			switch (entry.Status)
			{
				case CoverageStatus.Pass:
					if (error == null)
					{
						_summary.PassPassed++;
						return;
					}
					_summary.PassFailed++;
					Assert.Fail(error);
					return;

				case CoverageStatus.KnownFailure:
					if (error == null)
					{
						_summary.Count(_summary.KnownPassing, entry.Ticket);
						Assert.Fail(String.Format("{0} now passes; change its manifest entry to pass (it is known-failure against {1}).", entry.Script, entry.Ticket));
						return;
					}
					if (!CoverageManifest.Matches(error, entry.Error))
					{
						_summary.Count(_summary.KnownMismatched, entry.Ticket);
						Assert.Fail(String.Format("known-failure ({0}): expected the error to contain \"{1}\", but it was: {2}", entry.Ticket, entry.Error, error));
						return;
					}
					_summary.Count(_summary.KnownMatched, entry.Ticket);
					Assert.Warn(String.Format("known-failure ({0}): {1}", entry.Ticket, error));
					return;

				case CoverageStatus.Unstable:
					_summary.Count(error == null ? _summary.UnstablePassed : _summary.UnstableFailed, entry.Ticket);
					Assert.Warn(String.Format("unstable ({0}): {1}", entry.Ticket, error == null ? "passed" : "failed: " + error));
					return;
			}
		}

		/// <summary>
		/// The text a known-failure entry's error is matched against: the exception and each InnerException below it as
		/// "TypeName: Message", joined with " &lt;= ", on one line.
		/// </summary>
		public static string ErrorText(Exception exception)
		{
			StringBuilder text = new StringBuilder();
			for (Exception current = exception; current != null; current = current.InnerException)
			{
				if (text.Length > 0)
					text.Append(" <= ");
				text.Append(current.GetType().Name).Append(": ").Append(current.Message);
			}
			return text.Replace('\r', ' ').Replace('\n', ' ').ToString();
		}

		private static string Append(string error, string addition)
		{
			return error == null ? addition : error + "; " + addition;
		}

		private sealed class Summary
		{
			public int Cases;
			public int PassPassed;
			public int PassFailed;
			public int DiscoveryProblems;
			public int TemplateFailures;
			public int Fragments;
			public readonly SortedDictionary<string, int> KnownMatched = new SortedDictionary<string, int>(StringComparer.Ordinal);
			public readonly SortedDictionary<string, int> KnownMismatched = new SortedDictionary<string, int>(StringComparer.Ordinal);
			public readonly SortedDictionary<string, int> KnownPassing = new SortedDictionary<string, int>(StringComparer.Ordinal);
			public readonly SortedDictionary<string, int> UnstablePassed = new SortedDictionary<string, int>(StringComparer.Ordinal);
			public readonly SortedDictionary<string, int> UnstableFailed = new SortedDictionary<string, int>(StringComparer.Ordinal);

			public void Count(SortedDictionary<string, int> counts, string ticket)
			{
				counts[ticket] = counts.GetValueOrDefault(ticket) + 1;
			}

			public override string ToString()
			{
				StringBuilder text = new StringBuilder();
				text.AppendLine(String.Format("Coverage scripts: {0} case(s)", Cases));
				text.AppendLine(String.Format("  pass: {0} passed, {1} failed", PassPassed, PassFailed));
				text.AppendLine(String.Format("  known-failure: {0} matched, {1} mismatched, {2} unexpectedly passing", KnownMatched.Values.Sum(), KnownMismatched.Values.Sum(), KnownPassing.Values.Sum()));
				foreach (string ticket in KnownMatched.Keys.Union(KnownMismatched.Keys).Union(KnownPassing.Keys).OrderBy(ticket => ticket, StringComparer.Ordinal))
					text.AppendLine(String.Format("    {0}: {1} matched, {2} mismatched, {3} unexpectedly passing", ticket, KnownMatched.GetValueOrDefault(ticket), KnownMismatched.GetValueOrDefault(ticket), KnownPassing.GetValueOrDefault(ticket)));
				text.AppendLine(String.Format("  unstable: {0} passed, {1} failed", UnstablePassed.Values.Sum(), UnstableFailed.Values.Sum()));
				foreach (string ticket in UnstablePassed.Keys.Union(UnstableFailed.Keys).OrderBy(ticket => ticket, StringComparer.Ordinal))
					text.AppendLine(String.Format("    {0}: {1} passed, {2} failed", ticket, UnstablePassed.GetValueOrDefault(ticket), UnstableFailed.GetValueOrDefault(ticket)));
				text.AppendLine(String.Format("  fragment: {0} (run only as part of other scripts)", Fragments));
				text.Append(String.Format("  discovery problems: {0}", DiscoveryProblems));
				if (TemplateFailures > 0)
					text.Append(String.Format(Environment.NewLine + "  not run, template instance failed: {0}", TemplateFailures));
				return text.ToString();
			}
		}
	}
}
