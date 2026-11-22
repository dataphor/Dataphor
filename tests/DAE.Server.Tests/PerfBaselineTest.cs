/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>One row of the perf pass's scenario table: a D4 script whose elapsed time is the measurement.</summary>
	public sealed class PerfScenario
	{
		public PerfScenario(string libraryFolder, string name, string what)
		{
			LibraryFolder = libraryFolder;
			Name = name;
			What = what;
		}

		/// <summary>The library folder whose Documents/ holds the script, as this fixture's server copied it.</summary>
		public string LibraryFolder { get; }

		/// <summary>The script's file name without ".d4", which is also the key its own elapsed time is recorded under.</summary>
		public string Name { get; }

		/// <summary>One line on what the script does, for the run's output and for whoever reads a baseline diff.</summary>
		public string What { get; }

		public override string ToString() => Name;
	}

	/// <summary>One label's recorded numbers in the checked-in baseline file.</summary>
	public sealed class PerfBaselineEntry
	{
		[JsonPropertyName("label")] public string Label { get; set; }

		/// <summary>Debug or Release. A number measured in one configuration says nothing about the other.</summary>
		[JsonPropertyName("configuration")] public string Configuration { get; set; }

		/// <summary>The date the entry was recorded, yyyy-MM-dd.</summary>
		[JsonPropertyName("recorded")] public string Recorded { get; set; }

		/// <summary>Hand-written: what machine class the label stands for and what else was running. An update run keeps it.</summary>
		[JsonPropertyName("note")] public string Note { get; set; }

		/// <summary>Scenario key to milliseconds.</summary>
		[JsonPropertyName("scenarios")] public Dictionary<string, long> Scenarios { get; set; }
	}

	/// <summary>
	/// The checked-in perf baseline, tests/goldens/perf/perf-baseline.json: one entry per label, where a label stands for a
	/// machine class rather than for a machine. See docs/perf-baseline.md for why, and for how to record one.
	/// </summary>
	public sealed class PerfBaselineFile
	{
		[JsonPropertyName("schema")] public int Schema { get; set; }
		[JsonPropertyName("baselines")] public List<PerfBaselineEntry> Baselines { get; set; }
	}

	/// <summary>How bad the worst of a comparison's findings is.</summary>
	public enum PerfOutcome { Pass, Warn, Fail }

	/// <summary>What comparing one run against one label's recorded numbers found.</summary>
	public sealed class PerfComparison
	{
		public PerfComparison(PerfOutcome outcome, IReadOnlyList<string> findings)
		{
			Outcome = outcome;
			Findings = findings;
		}

		public PerfOutcome Outcome { get; }

		/// <summary>One line per finding, in scenario order; empty when every scenario is inside tolerance.</summary>
		public IReadOnlyList<string> Findings { get; }
	}

	/// <summary>
	/// The engine's recorded speed: each script of the scenario table below is run batch by batch, every number it yields is
	/// written to a run file and printed, and - when the run asks for it by naming a baseline label - each number is compared
	/// with the one checked in under that label, so that a large slowdown shows up as a difference rather than as a hunch.
	/// See docs/perf-baseline.md.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Three behaviours, selected by two environment variables. With neither set the pass records and passes, and it must
	/// never do anything else: recording a baseline is half of what this pass is for, and a run nobody asked to gate cannot
	/// be allowed to go red on a slow machine. With DATAPHOR_PERF_BASELINE set it also compares. With
	/// DATAPHOR_UPDATE_PERF_BASELINE=1 and a label it rewrites that label's entry and then fails anyway, exactly as
	/// DATAPHOR_UPDATE_FRONTEND_GOLDENS=1 does, so that an update cannot be mistaken for a pass and its diff gets read.
	/// </para>
	/// <para>
	/// The numbers are script time, not suite time: the fixture measures batches on an already-started server, so nothing
	/// here includes the seconds a coverage case spends copying library folders and starting an engine. Say which is which
	/// wherever a number is published, or the two sets of figures get compared with each other.
	/// </para>
	/// </remarks>
	// Not [Parallelizable]: this fixture starts a server, and two engines in one process share the engine's static state
	// (see the note on TestServerInstance).
	[TestFixture]
	public class PerfBaselineTest : InProcessTestFixture
	{
		/// <summary>Set to a label to compare this run against that label's recorded numbers; see AGENTS.md "Build and test".</summary>
		public const string LabelVariable = "DATAPHOR_PERF_BASELINE";

		/// <summary>Set to 1, with <see cref="LabelVariable"/>, to rewrite that label's entry instead of comparing against it.</summary>
		public const string UpdateVariable = "DATAPHOR_UPDATE_PERF_BASELINE";

		/// <summary>The D4 library carrying the BenchmarkExecute operator the timing scripts call.</summary>
		public const string PerfLibrary = "TestFramework.Perf";

		/// <summary>This pass's own tree below <see cref="TestEnvironment.GoldensDirectory"/>.</summary>
		public const string GoldensFolderName = "perf";

		public const string BaselineFileName = "perf-baseline.json";

		/// <summary>How a message names the checked-in file; the path itself comes from TestEnvironment.</summary>
		public const string BaselineRelativePath = "tests/goldens/" + GoldensFolderName + "/" + BaselineFileName;

		/// <summary>The shape of the baseline file. A reader that meets another version says so rather than guessing.</summary>
		public const int BaselineSchema = 1;

		/// <summary>Where a run's own numbers go: git-ignored, so a CI job can upload the file and nothing litters the tree.</summary>
		public const string RunRelativePath = "artifacts/perf/perf-run.json";

		/// <summary>Past this many times a recorded number the run warns, and past <see cref="FailFactor"/> it fails.</summary>
		/// <remarks>
		/// Chosen policy, not measured thresholds. 3 is meant to sit above the machine-to-machine and run-to-run spread so
		/// that a warning means something - the figures in the root AGENTS.md already show roughly a threefold spread
		/// between this project's fast and slow development machines - and 10 is meant to catch the kind of change that is
		/// wrong rather than slow. The way to tune them is to collect a few runs' numbers under one label and look at the
		/// spread; docs/perf-baseline.md says so.
		/// </remarks>
		public const double WarnFactor = 3.0;
		public const double FailFactor = 10.0;

#if DEBUG
		private const string BuildConfiguration = "Debug";
#else
		private const string BuildConfiguration = "Release";
#endif

		/// <summary>
		/// The scenarios this pass measures, in order. A script contributes its own elapsed time under its name and one
		/// number per expression batch under "name#ordinal" - the ordinal, not the line number, which would move if a script
		/// were ever reformatted. A scenario added later (research-catalog-load-profile is the next one) is one row here,
		/// not a second fixture.
		/// </summary>
		/// <remarks>
		/// Timings is the one scenario whose numbers can be read against a figure from outside this project: its own 2008
		/// comments record roughly 5 s, 9.5 s, 8 s and 650 ms for its four blocks on the hardware of that year. It joined the
		/// table once bug-engine-delete-quota-scans-the-table made its third block - the drain, "while exists (T) do delete T
		/// return 1 by { ID };" - cost time proportional to the table's size rather than to its square; until then the script
		/// needed about a quarter of an hour and was a recorded known failure of the coverage pass.
		/// </remarks>
		// NOTE: accepted tradeoff - each scenario's script runs twice per suite: once as a coverage case, which checks only that
		// it does not raise, and once here, which checks the same and also captures the number. The alternatives were to give the
		// measurement to the coverage runner, which would put a gating concern inside the suite's most load-bearing fixture
		// and force it to reason about the three-part split measuring only some scenarios, or to move the script out of the
		// coverage corpus, which would shrink the audited script set and change the counts published in
		// docs/coverage-report.md. Paying the measured 15 s was judged cheaper than either. Revisit if this table grows
		// enough that the duplication is minutes rather than seconds.
		private static readonly PerfScenario[] Scenarios =
		[
			new PerfScenario(CoverageScripts.ScriptsLibrary, "Timing", "a ten-million-iteration counting loop: the interpreter's per-node cost"),
			new PerfScenario(CoverageScripts.ScriptsLibrary, "Timings", "10,000 single-row inserts into an in-memory table, one update of all of them, a one-row-at-a-time drain, and 100,000 integer additions"),
		];

		/// <summary>The scripts library, for the script text, and the perf library, for the operator. Neither requires anything but System.</summary>
		protected override string[] Libraries => [CoverageScripts.ScriptsLibrary, PerfLibrary];

		/// <summary>
		/// Only the perf library: the scripts are read as text and run here, not through the scripts library.
		/// </summary>
		/// <remarks>
		/// Timings needs it twice over: for the BenchmarkExecute operator its four blocks call, and for the lifted row cap on
		/// System.Temp that its 10,000 rows need (the default is 1,000 rows per table). Timing needs neither, and the library
		/// would be registered for it alone anyway, as the one place in the suite where a breakage in that library's Register.d4
		/// fails plainly rather than only changing the text of a failure.
		/// </remarks>
		protected override string[] LibrariesToRegister => [PerfLibrary];

		private static bool Updating => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

		/// <summary>This pass's checked-in file, tests/goldens/perf/perf-baseline.json.</summary>
		private static string BaselinePath => Path.Combine(TestEnvironment.GoldensDirectory, GoldensFolderName, BaselineFileName);

		private static string RunPath => Path.Combine(TestEnvironment.RepositoryDirectory, RunRelativePath.Replace('/', Path.DirectorySeparatorChar));

		private static readonly JsonSerializerOptions ReadOptions =
			new JsonSerializerOptions
			{
				UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
				AllowDuplicateProperties = false
			};

		// The relaxed encoder because the default escapes an apostrophe as ', and these files are read in a diff: the
		// escaping it leaves out matters only for JSON embedded in HTML, which this is not.
		private static readonly JsonSerializerOptions WriteOptions =
			new JsonSerializerOptions
			{
				WriteIndented = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			};

		/// <summary>
		/// The pass itself: every scenario runs, its numbers are recorded, and - when a label was named - each is inside
		/// tolerance of the number checked in under that label.
		/// </summary>
		/// <remarks>
		/// One case looping the table rather than one case per scenario, so that the record is written once, from one run's
		/// complete set. A filtered run therefore always measures the whole table; if a later ticket splits this into a case
		/// per scenario, the write-and-compare step has to learn that a run may hold only part of the set.
		/// </remarks>
		[Test]
		public void TheTimingScriptsRunAndTheirNumbersMatchTheBaseline()
		{
			string label = Environment.GetEnvironmentVariable(LabelVariable);
			bool updating = Updating;
			if (updating && String.IsNullOrEmpty(label))
				Assert.Fail(String.Format("{0}=1 needs {1}=<label> as well: an entry is recorded under a label standing for a machine class, never for a machine. See docs/perf-baseline.md.", UpdateVariable, LabelVariable));

			// The scripts run with the perf library current, which is the library the coverage runner leaves current for an
			// entry that registers it, so that a script creating a table puts it on the same default device there and here.
			ExecuteScript(String.Format("SetLibrary('{0}');", PerfLibrary));

			SortedDictionary<string, long> measured = new SortedDictionary<string, long>(StringComparer.Ordinal);
			Progress(String.Format("Perf baseline: {0} scenario(s), {1}, {2}", Scenarios.Length, BuildConfiguration, String.IsNullOrEmpty(label) ? String.Format("recording only (set {0}=<label> to compare)", LabelVariable) : String.Format("comparing against \"{0}\"", label)));
			foreach (PerfScenario scenario in Scenarios)
				Measure(scenario, measured);

			WriteRun(label, measured);
			Progress(Report(measured));

			if (updating)
			{
				int recorded = Update(label, measured);
				Assert.Fail(String.Format("{0}=1: recorded {1} scenario(s) under \"{2}\" in {3}. An update run always fails, so that it cannot be mistaken for a passing one - read the diff before committing it, and fill in the entry's note.", UpdateVariable, recorded, label, BaselineRelativePath));
			}

			if (String.IsNullOrEmpty(label))
				return;

			PerfComparison comparison = Compare(measured, ReadBaseline(), label, WarnFactor, FailFactor);
			string report = String.Format("{0} finding(s) comparing this run with \"{1}\" in {2}:{3}", comparison.Findings.Count, label, BaselineRelativePath, String.Concat(comparison.Findings.Select(finding => Environment.NewLine + "  " + finding)));
			if (comparison.Outcome == PerfOutcome.Fail)
				Assert.Fail(report);
			else if (comparison.Outcome == PerfOutcome.Warn)
				Assert.Warn(report);
		}

		/// <summary>
		/// Compares a run's numbers with one label's recorded numbers. Pure, so that the bands can be tested without a
		/// server: it is given the numbers, the whole baseline file, the label and the two factors, and returns the findings.
		/// </summary>
		/// <remarks>
		/// A scenario the file does not list and a listed scenario the run did not measure are both findings, as they are for
		/// the derived-form goldens: the two sets must agree, or a scenario can stop being measured without anything going
		/// red. A run faster than its baseline is never a finding - the numbers are printed either way.
		/// </remarks>
		public static PerfComparison Compare(IReadOnlyDictionary<string, long> measured, PerfBaselineFile baseline, string label, double warnFactor, double failFactor)
		{
			PerfBaselineEntry entry = baseline?.Baselines?.FirstOrDefault(candidate => String.Equals(candidate?.Label, label, StringComparison.Ordinal));
			if (entry == null)
				return new PerfComparison(PerfOutcome.Fail, [String.Format("{0} has no entry labelled \"{1}\". Record one: set {2}=1 and {3}={1}, read the diff, and commit the file.", BaselineRelativePath, label, UpdateVariable, LabelVariable)]);

			Dictionary<string, long> recorded = entry.Scenarios ?? new Dictionary<string, long>();
			List<string> findings = new List<string>();
			PerfOutcome outcome = PerfOutcome.Pass;

			void Finding(PerfOutcome severity, string text)
			{
				findings.Add(text);
				if (severity > outcome)
					outcome = severity;
			}

			foreach (string scenario in measured.Keys.Union(recorded.Keys).OrderBy(name => name, StringComparer.Ordinal))
			{
				bool wasMeasured = measured.TryGetValue(scenario, out long now);
				bool wasRecorded = recorded.TryGetValue(scenario, out long then);

				if (!wasRecorded)
					Finding(PerfOutcome.Fail, String.Format("{0}: measured {1} ms, but \"{2}\" records no such scenario. Re-record the baseline.", scenario, now, label));
				else if (!wasMeasured)
					Finding(PerfOutcome.Fail, String.Format("{0}: \"{1}\" records {2} ms, but this run did not measure it. Find out which scenario stopped being measured, or re-record the baseline.", scenario, label, then));
				else if (then <= 0)
					Finding(PerfOutcome.Fail, String.Format("{0}: \"{1}\" records {2} ms, which nothing can be compared with. Re-record the baseline.", scenario, label, then));
				else
				{
					double factor = (double)now / then;
					if (factor > failFactor)
						Finding(PerfOutcome.Fail, String.Format("{0}: {1} ms against the recorded {2} ms, {3:F1}x - past the {4:F0}x failure factor.", scenario, now, then, factor, failFactor));
					else if (factor > warnFactor)
						Finding(PerfOutcome.Warn, String.Format("{0}: {1} ms against the recorded {2} ms, {3:F1}x - past the {4:F0}x warning factor.", scenario, now, then, factor, warnFactor));
				}
			}

			return new PerfComparison(outcome, findings);
		}

		/// <summary>
		/// Runs one scenario's script batch by batch, as Dataphoria runs a script and as the manifest's batches entries do,
		/// and records its numbers.
		/// </summary>
		/// <remarks>
		/// Batch by batch rather than through one ExecuteScript statement for two reasons: it is what gives the per-block
		/// numbers, because each BenchmarkExecute call is its own batch, and it is the shape a person running these scripts
		/// in the IDE sees - one implicit transaction per batch rather than one for the whole script.
		/// </remarks>
		// NOTE: no wall-clock cap on a scenario, and a line is printed only when a batch ends, so a batch that runs away prints
		// nothing for its whole duration - which the ticket runner kills at ten minutes of silence, and which is how Timings was
		// found. Harmless while every batch is seconds. If a scenario is ever added that can legitimately run for minutes, or a
		// regression makes one do so, give this loop a timeout of its own, the way a coverage entry has timeoutSeconds.
		private void Measure(PerfScenario scenario, SortedDictionary<string, long> measured)
		{
			Progress(String.Format("{0}: {1}", scenario.Name, scenario.What));
			IServerScript script = Process.PrepareScript(File.ReadAllText(ScriptPath(scenario)));
			try
			{
				Stopwatch whole = Stopwatch.StartNew();
				int ordinal = 0;
				foreach (IServerBatch batch in script.Batches)
				{
					ordinal++;
					Stopwatch batchWatch = Stopwatch.StartNew();
					TimeSpan? value;
					try
					{
						value = ExecuteBatch(batch);
					}
					catch (Exception exception)
					{
						// Stop the whole pass rather than go on to the next scenario: one server serves the whole table, and a
						// script that failed part way through can leave objects behind - Timings creates the table T in its
						// first batch and drops it in its third - so a later scenario would measure a dirty catalog.
						Assert.Fail(String.Format("{0}, batch {1} (line {2}) failed, so neither it nor any later scenario was measured: {3}", scenario.Name, ordinal, batch.Line, CoverageScripts.ErrorText(exception)));
						return;
					}
					batchWatch.Stop();

					// Only an expression batch gets a number of its own. A statement batch's cost is in the script's total,
					// which is what Timing - one statement batch - is measured by.
					if (batch.IsExpression())
					{
						long milliseconds = Milliseconds(value ?? batchWatch.Elapsed);
						measured[Key(scenario, ordinal)] = milliseconds;
						Progress(String.Format("  batch {0} (line {1}): {2} ms{3}", ordinal, batch.Line, milliseconds, value.HasValue ? " (the value the batch evaluated to)" : " (wall clock: the batch did not evaluate to a TimeSpan)"));
					}
					else
						Progress(String.Format("  batch {0} (line {1}): {2} ms (wall clock of a statement batch, counted only in the script's total)", ordinal, batch.Line, Milliseconds(batchWatch.Elapsed)));
				}
				whole.Stop();
				measured[scenario.Name] = Milliseconds(whole.Elapsed);
				Progress(String.Format("  {0}: {1} ms for the whole script", scenario.Name, Milliseconds(whole.Elapsed)));
			}
			finally
			{
				Process.UnprepareScript(script);
			}
		}

		/// <summary>The key one batch's number is recorded under: the script's name and the batch's ordinal.</summary>
		private static string Key(PerfScenario scenario, int ordinal)
		{
			return String.Format("{0}#{1}", scenario.Name, ordinal);
		}

		/// <summary>
		/// Runs one batch and returns the TimeSpan it evaluated to, or null when it did not evaluate to one. A batch of
		/// select BenchmarkExecute(...) returns the engine's own measurement, which is a better number than the wall clock
		/// around it; everything else is timed by the caller.
		/// </summary>
		/// <remarks>
		/// ServerBatch.Execute disposes the value it evaluates, so a caller that evaluates the plan itself has to dispose
		/// what it reads. A table-valued expression is opened and closed instead, because a table has no value to evaluate
		/// to; neither timing script has one, and the branch is here so that a scenario added later cannot fail obscurely.
		/// </remarks>
		private static TimeSpan? ExecuteBatch(IServerBatch batch)
		{
			if (!batch.IsExpression())
			{
				batch.Execute(null);
				return null;
			}

			IServerExpressionPlan plan = (IServerExpressionPlan)batch.Prepare(null);
			try
			{
				if (plan.DataType is Schema.TableType)
				{
					plan.Close(plan.Open(null));
					return null;
				}

				IDataValue value = plan.Evaluate(null);
				if (value == null)
					return null;
				try
				{
					return value.AsNative as TimeSpan?;
				}
				finally
				{
					value.Dispose();
				}
			}
			finally
			{
				batch.Unprepare(plan);
			}
		}

		/// <summary>Sub-millisecond precision is meaningless at this scale, so a number is a whole millisecond.</summary>
		private static long Milliseconds(TimeSpan elapsed)
		{
			return (long)Math.Round(elapsed.TotalMilliseconds);
		}

		/// <summary>The script's file in this fixture's server's own copy of the library, which is where the coverage runner reads it too.</summary>
		private string ScriptPath(PerfScenario scenario)
		{
			return Path.Combine(Instance.Directory, TestServerInstance.LibrariesFolderName, scenario.LibraryFolder, "Documents", scenario.Name + CoverageScripts.ScriptExtension);
		}

		/// <summary>
		/// Written as each number is taken rather than collected and printed at the end, for two reasons: a reader of a run
		/// that was killed or timed out still sees how far it got and what the blocks cost, and the ticket runner kills an
		/// agent whose command prints nothing for ten minutes, which a silent multi-minute case would do.
		/// </summary>
		private static void Progress(string line)
		{
			TestContext.Progress.WriteLine(line);
		}

		/// <summary>The run's closing summary: the numbers in one block, and where the run file went.</summary>
		private static string Report(IReadOnlyDictionary<string, long> measured)
		{
			StringBuilder result = new StringBuilder();
			foreach (KeyValuePair<string, long> entry in measured)
				result.AppendLine(String.Format("  {0} = {1} ms", entry.Key, entry.Value));
			result.Append(String.Format("  written to {0}", RunRelativePath));
			return result.ToString();
		}

		/// <summary>Writes this run's numbers where a CI job can upload them and a person can read them.</summary>
		private static void WriteRun(string label, IReadOnlyDictionary<string, long> measured)
		{
			PerfBaselineEntry run = NewEntry(String.IsNullOrEmpty(label) ? "this-run" : label, measured, "one run's numbers, written by PerfBaselineTest; not a baseline, and never read by a comparison");
			Write(RunPath, new PerfBaselineFile { Schema = BaselineSchema, Baselines = [run] });
		}

		/// <summary>Rewrites one label's entry, leaving every other label untouched; returns how many scenarios it recorded.</summary>
		/// <remarks>
		/// The entry's note is hand-written - what machine class the label stands for, what else was running - so a
		/// re-recording keeps the note it finds rather than replacing it with the placeholder.
		/// </remarks>
		private static int Update(string label, IReadOnlyDictionary<string, long> measured)
		{
			PerfBaselineFile file = ReadBaseline() ?? new PerfBaselineFile();
			file.Schema = BaselineSchema;
			file.Baselines ??= new List<PerfBaselineEntry>();

			int index = file.Baselines.FindIndex(candidate => String.Equals(candidate?.Label, label, StringComparison.Ordinal));
			string note = index < 0 || String.IsNullOrEmpty(file.Baselines[index].Note) ? NotePlaceholder : file.Baselines[index].Note;
			PerfBaselineEntry entry = NewEntry(label, measured, note);
			if (index < 0)
				file.Baselines.Add(entry);
			else
				file.Baselines[index] = entry;
			file.Baselines.Sort((left, right) => String.CompareOrdinal(left?.Label, right?.Label));

			Write(BaselinePath, file);
			return entry.Scenarios.Count;
		}

		/// <summary>What a new entry's note says until a person replaces it. No machine's name: this repository is public.</summary>
		private const string NotePlaceholder = "replace this with the machine class this label stands for and what else was running; never a machine's name, this repository is public";

		private static PerfBaselineEntry NewEntry(string label, IReadOnlyDictionary<string, long> measured, string note)
		{
			Dictionary<string, long> scenarios = new Dictionary<string, long>();
			foreach (KeyValuePair<string, long> entry in measured.OrderBy(entry => entry.Key, StringComparer.Ordinal))
				scenarios.Add(entry.Key, entry.Value);
			return
				new PerfBaselineEntry
				{
					Label = label,
					Configuration = BuildConfiguration,
					Recorded = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
					Note = note,
					Scenarios = scenarios
				};
		}

		/// <summary>The checked-in file, or null when there is none.</summary>
		/// <remarks>
		/// Every reader comes through here, so that the schema is checked on the update path as well as the comparing one: an
		/// update that rewrote a file of a later shape as this one would silently discard whatever that shape carries.
		/// </remarks>
		private static PerfBaselineFile ReadBaseline()
		{
			string path = BaselinePath;
			if (!File.Exists(path))
				return null;
			PerfBaselineFile file;
			using (FileStream stream = File.OpenRead(path))
				file = JsonSerializer.Deserialize<PerfBaselineFile>(stream, ReadOptions);
			if (file != null && file.Schema != BaselineSchema)
				Assert.Fail(String.Format("{0} has schema {1}; this pass reads schema {2}. Update the reader, or the file.", BaselineRelativePath, file.Schema, BaselineSchema));
			return file;
		}

		/// <summary>
		/// Writes a baseline or run file: indented JSON, UTF-8 with no byte-order mark, LF line endings, ending in a newline.
		/// LF because .gitattributes pins tests/goldens/** to LF, so a file written with CRLF would be rewritten on checkout
		/// and show as a change nobody made.
		/// </summary>
		private static void Write(string path, PerfBaselineFile file)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, JsonSerializer.Serialize(file, WriteOptions).Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
		}
	}

	/// <summary>
	/// The bands of <see cref="PerfBaselineTest.Compare"/>, on a fabricated baseline and with factors of this test's own:
	/// no server, which is the lowest layer that shows them, and no dependence on the pass's chosen thresholds.
	/// </summary>
	[TestFixture]
	public class PerfComparisonTest
	{
		private const double WarnFactor = 2.0;
		private const double FailFactor = 5.0;
		private const string Label = "fabricated";
		private const long Recorded = 100;

		private static readonly PerfBaselineFile Baseline =
			new PerfBaselineFile
			{
				Schema = PerfBaselineTest.BaselineSchema,
				Baselines =
				[
					new PerfBaselineEntry { Label = "another-machine-class", Scenarios = new Dictionary<string, long> { { "A", 1 } } },
					new PerfBaselineEntry { Label = Label, Scenarios = new Dictionary<string, long> { { "A", Recorded } } },
				]
			};

		private static PerfComparison Compare(string label, params (string Scenario, long Milliseconds)[] measured)
		{
			return PerfBaselineTest.Compare(measured.ToDictionary(entry => entry.Scenario, entry => entry.Milliseconds), Baseline, label, WarnFactor, FailFactor);
		}

		[Test]
		public void ComparingTellsTheBandsApart()
		{
			Assert.Multiple
			(
				() =>
				{
					Assert.That(Compare(Label, ("A", Recorded)).Findings, Is.Empty, "A run at the recorded number reports nothing.");
					Assert.That(Compare(Label, ("A", (long)(Recorded * WarnFactor))).Outcome, Is.EqualTo(PerfOutcome.Pass), "A run at exactly the warning factor is inside tolerance.");
					Assert.That(Compare(Label, ("A", (long)(Recorded * WarnFactor) + 1)).Outcome, Is.EqualTo(PerfOutcome.Warn), "A run just past the warning factor warns.");
					Assert.That(Compare(Label, ("A", (long)(Recorded * FailFactor) + 1)).Outcome, Is.EqualTo(PerfOutcome.Fail), "A run just past the failure factor fails.");
					Assert.That(Compare("no-such-label", ("A", Recorded)).Outcome, Is.EqualTo(PerfOutcome.Fail), "A label the file does not carry fails, rather than passing silently.");
					Assert.That(Compare(Label, ("A", Recorded), ("B", Recorded)).Outcome, Is.EqualTo(PerfOutcome.Fail), "A measured scenario the file does not list fails: the two sets must agree.");
					Assert.That(Compare(Label).Outcome, Is.EqualTo(PerfOutcome.Fail), "A recorded scenario the run did not measure fails: the two sets must agree.");
				}
			);
		}
	}
}
