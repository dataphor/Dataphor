/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace DAE.Benchmarks
{
	/// <summary>
	/// The benchmark program's entry point and the BenchmarkDotNet configuration every measurement runs under.
	/// <see cref="EngineBenchmarks"/> holds the measurements themselves; README.md says how to run this and what a run
	/// costs, and docs/perf-baseline.md carries the numbers.
	/// </summary>
	public static class Program
	{
		/// <summary>The folder under the build output root (artifacts/) that BenchmarkDotNet writes its export and logs to.</summary>
		/// <remarks>
		/// Inside artifacts/ rather than the BenchmarkDotNet.Artifacts/ folder it would otherwise create beside the working
		/// directory, because artifacts/ is already git-ignored: nothing new has to be ignored and a run leaves nothing in
		/// the tree.
		/// </remarks>
		public const string ArtifactsFolderName = "benchmarks";

		/// <summary>
		/// One warmup iteration and <see cref="IterationCount"/> measured ones, each one invocation of the benchmark.
		/// </summary>
		/// <remarks>
		/// BenchmarkDotNet's default job is built for operations measured in microseconds: it runs a pilot to find how many
		/// invocations fit in a time slice, then many warmup and measurement iterations. Every iteration here starts a
		/// Dataphor engine, which costs seconds, so the default job would run for a very long time and tell us nothing
		/// extra - hence <see cref="RunStrategy.Monitoring"/> with these counts stated outright. One warmup iteration is
		/// there to pay the one-off costs a process has on its first engine (assembly loading, the SQLite native library,
		/// the engine's lazily built tables) outside the measured set.
		/// </remarks>
		public const int WarmupCount = 1;

		// NOTE: four benchmarks at ten iterations cost about 100 seconds of wall clock on the slow machine class, which is
		// inside what a ticket can run. The cost is proportional in both directions: if benchmarks are added or this count
		// is raised and a run grows past about ten minutes, it stops being agent-runnable and becomes a by-hand step, which
		// docs/perf-baseline.md and README.md would then have to say.
		/// <summary>
		/// How many measured iterations each benchmark runs; see <see cref="WarmupCount"/> for why the count is stated
		/// rather than found.
		/// </summary>
		/// <remarks>
		/// Ten, not five: the two engine-start benchmarks vary by about a sixth of their mean between iterations on the
		/// slow machine class, and at five iterations BenchmarkDotNet's 99.9 % confidence interval came out wider than
		/// half the mean, which is too wide for a number anybody would compare against. Ten keeps the whole run near two
		/// minutes of wall clock, which the README records.
		/// </remarks>
		public const int IterationCount = 10;

		/// <summary>What a run with no arguments does: every benchmark. BenchmarkDotNet would otherwise ask interactively, which a scripted or agent-driven run cannot answer.</summary>
		private static readonly string[] EveryBenchmark = ["--filter", "*"];

		public static int Main(string[] args)
		{
#if DEBUG
			// The validator in BenchmarkDotNet's default configuration warns about an unoptimized assembly, but it warns
			// among a lot of other output. A Debug number is not comparable with a Release one and docs/perf-baseline.md
			// keeps them in separate tables, so say it plainly before anything else is printed.
			Console.Error.WriteLine("This is a Debug build: its numbers belong in no baseline table. Build and run with -c Release.");
#endif

			IEnumerable<Summary> summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args.Length == 0 ? EveryBenchmark : args, Configuration());

			// A benchmark whose iterations threw is reported in the summary and would otherwise leave the process exiting 0,
			// which is how a half-failed run gets published. Nothing measured at all - a filter that matched nothing - is a
			// failure for the same reason.
			int measured = 0;
			bool failed = false;
			foreach (Summary summary in summaries)
			{
				if (summary.HasCriticalValidationErrors)
					failed = true;
				foreach (BenchmarkReport report in summary.Reports)
				{
					measured++;
					if (!report.Success)
						failed = true;
				}
			}

			if (measured == 0)
			{
				Console.Error.WriteLine("No benchmark ran. Check the filter: DAE.Benchmarks --list flat shows the names.");
				failed = true;
			}

			return failed ? 1 : 0;
		}

		/// <summary>The configuration: the default one's columns, loggers and exporters, this program's job, and an artifacts path inside artifacts/.</summary>
		private static IConfig Configuration()
		{
			Job job =
				Job.Default
					// An in-process toolchain, and not optional. By default BenchmarkDotNet generates a project per
					// benchmark and builds it with the SDK; a generated project under this repository inherits
					// Directory.Build.props, which switches the SDK's default compile items off (uncompiled files sit
					// beside compiled ones in this codebase) and redirects ArtifactsPath - so the generated project
					// compiles nothing and the run fails in a way that looks like a BenchmarkDotNet defect. An in-process
					// toolchain generates and builds nothing. It costs nothing here either: every iteration starts an
					// engine, so process-level isolation would buy little, and the engine has process-wide static state in
					// any case (BenchmarkServerInstance's object-id counter).
					.WithToolchain(InProcessNoEmitToolchain.Instance)
					.WithStrategy(RunStrategy.Monitoring)
					.WithWarmupCount(WarmupCount)
					.WithIterationCount(IterationCount)
					// One invocation per iteration, so that an iteration is exactly one engine's life and the iteration
					// setup and cleanup run once around it. BenchmarkDotNet requires the invocation count to be a multiple
					// of the unroll factor, so both are 1.
					.WithInvocationCount(1)
					.WithUnrollFactor(1);

			return
				ManualConfig.Create(DefaultConfig.Instance)
					.AddJob(job)
					.WithArtifactsPath(Path.Combine(BenchmarkEnvironment.ArtifactsDirectory, ArtifactsFolderName));
		}
	}
}
