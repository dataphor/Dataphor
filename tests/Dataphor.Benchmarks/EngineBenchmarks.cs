/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using BenchmarkDotNet.Attributes;

namespace DAE.Benchmarks
{
	/// <summary>
	/// The four measurements: engine start, a representative mix of database work on the in-memory device, and the
	/// catalog store cold and warm. Every iteration of every benchmark gets an instance of its own - sharing one across
	/// iterations would measure a warm engine in one iteration and a cold one in the next - and the work that is not
	/// being measured (creating the directory, copying library folders, starting the server where the start is not the
	/// measurement) is in an iteration setup, which BenchmarkDotNet does not time.
	/// </summary>
	/// <remarks>
	/// One class rather than one per measurement, so that a run produces one summary and therefore one markdown export
	/// to carry into docs/perf-baseline.md. The per-measurement setup is told apart by the Target of each setup and
	/// cleanup method.
	/// </remarks>
	public class EngineBenchmarks
	{
		/// <summary>The table the mix creates, on the in-memory device.</summary>
		private const string MixTableName = "BenchmarkMix";

		/// <summary>
		/// How many rows <see cref="DdlDmlMix"/> inserts, updates, reads and deletes.
		/// </summary>
		/// <remarks>
		/// Three thousand, for one measured reason: BenchmarkDotNet warns that an iteration shorter than 100 ms is too
		/// short to measure well (its MinIterationTime warning), and on the slow machine class a thousand rows took 55
		/// to 104 ms an iteration and two thousand took 97 to 221 ms - both still dipping under the floor. Three
		/// thousand clears it, is enough that the per-row cost dominates the compile of the six statements, and is small
		/// enough that one iteration is a fraction of a second, so a run of the whole program stays well inside the ten
		/// minutes the root AGENTS.md calls the limit of what an agent can run. It needs the row cap lifted (see
		/// <see cref="LiftTheMemoryDeviceRowCapScript"/>). The coverage script Timings already measures the individual
		/// table operations at 10,000 rows, which is where to look for a larger table. Changing this number invalidates
		/// every recorded number for this benchmark: re-record, do not compare.
		/// </remarks>
		public const int MixRowCount = 3000;

		/// <summary>
		/// Lifts the in-memory device's row cap, so that <see cref="MixRowCount"/> is a free choice rather than one
		/// bounded by a device setting. System.Temp holds at most Engine.TempDeviceMaxRowCount (1,000) rows per table by
		/// default, which is fewer than the mix inserts: without this the mix fails with error 114113 rather than
		/// measuring. Run once per instance, outside the measured region.
		/// </summary>
		private const string LiftTheMemoryDeviceRowCapScript = "alter device System.Temp alter class { alter \"MaxRowCount\" = \"-1\" };";

		/// <summary>
		/// The D4 the mix runs, in one place so that the next person to change it can see what the published numbers were
		/// measured against. A silent change to the mix invalidates every recorded number for it.
		/// </summary>
		/// <remarks>
		/// Six operations, one of each kind an application does to a table: create it, fill it a row at a time, update
		/// every row in one statement, read every row back through a cursor, empty it in one statement and drop it. They
		/// are measured as one number on purpose - the point is a tripwire on the compiler and the memory device
		/// together, not six numbers, which Timings already gives for the individual operations.
		///
		/// The delete is the whole table in one statement, deliberately not the "delete T return 1 by { ID }" form the
		/// Timings script uses. That form cost time quadratic in the table's size when this mix was written and would have
		/// measured that defect rather than a mix; it costs time proportional to the size since
		/// bug-engine-delete-quota-scans-the-table, and the mix still does not use it, because Timings measures the drain
		/// and a whole-table delete is the shape an application uses. Neither the mix nor its recorded numbers were
		/// affected by that fix: no statement here narrows a table in an order, which is the only shape it changed.
		///
		/// The cursor loop counts the rows it reads and raises if the count is not the number inserted, so that an
		/// iteration which silently did less work than the one before it fails instead of publishing a faster number.
		/// </remarks>
		private static readonly string DdlDmlMixScript =
			String.Format
			(
@"create table {0} in System.Temp {{ ID : Integer, Value : Integer, key {{ ID }} }};

for var LIndex := 1 to {1} do
	insert row {{ LIndex ID, LIndex * 2 Value }} into {0};

update {0} set {{ Value := Value + 1 }};

begin
	var LCount : Integer := 0;
	var LValue : Integer;
	var LCursor : cursor(typeof({0})) := cursor({0});
	try
		while LCursor.Next() do
		begin
			LCount := LCount + 1;
			LValue := Value from LCursor.Select();
		end;
	finally
		LCursor.Close();
	end;
	if LCount <> {1} then
		raise Error('The mix read ' + ToString(LCount) + ' rows, not {1}.');
end;

delete {0};

drop table {0};
",
				MixTableName,
				MixRowCount
			);

		/// <summary>The instance the current iteration measures. Created by an iteration setup or by the benchmark body, and disposed by the iteration cleanup.</summary>
		private BenchmarkServerInstance _instance;

		/// <summary>The stopped instance whose populated directory <see cref="CatalogStoreWarm"/> copies for each of its iterations.</summary>
		private BenchmarkServerInstance _warmTemplate;

		private void DisposeTheInstance()
		{
			BenchmarkServerInstance instance = _instance;
			_instance = null;
			if (instance != null)
				instance.Dispose();
		}

		/// <summary>
		/// The whole cost of bringing an engine up the way a test case or a tool invocation does: a fresh instance
		/// directory with a copy of the library folders it needs - none, because System is built in and the server creates
		/// General - then Start and Stop. This is the number someone asks about when a test run feels slow.
		/// </summary>
		/// <remarks>
		/// It overlaps <see cref="CatalogStoreCold"/> on purpose: the engine's start on a new instance directory is also
		/// the catalog store's write path, and the two are the numbers two different questions ask for. The difference
		/// between them is this one's directory setup, which is inside the measured region here and outside it there.
		/// </remarks>
		[Benchmark]
		public void EngineStart()
		{
			_instance = new BenchmarkServerInstance(nameof(EngineStart));
			_instance.Server.Start();
			_instance.Server.Stop();
		}

		[IterationCleanup(Target = nameof(EngineStart))]
		public void CleanUpAfterEngineStart()
		{
			DisposeTheInstance();
		}

		/// <summary>
		/// A representative mix of database work on the in-memory device, on a server started outside the measured
		/// region. One number that moves when the compiler or the memory device gets slower; <see cref="DdlDmlMixScript"/>
		/// is what it runs and why.
		/// </summary>
		[Benchmark]
		public void DdlDmlMix()
		{
			_instance.ExecuteScript(DdlDmlMixScript);
		}

		[IterationSetup(Target = nameof(DdlDmlMix))]
		public void StartAServerForTheMix()
		{
			_instance = new BenchmarkServerInstance(nameof(DdlDmlMix));
			_instance.Server.Start();
			_instance.ExecuteScript(LiftTheMemoryDeviceRowCapScript);
		}

		[IterationCleanup(Target = nameof(DdlDmlMix))]
		public void CleanUpAfterTheMix()
		{
			DisposeTheInstance();
		}

		/// <summary>
		/// The catalog store's write path: a server started on a new instance directory, so that the SQLite catalog is
		/// created and the system library registered, then stopped.
		/// </summary>
		[Benchmark]
		public void CatalogStoreCold()
		{
			_instance.Server.Start();
			_instance.Server.Stop();
		}

		[IterationSetup(Target = nameof(CatalogStoreCold))]
		public void CreateAFreshInstance()
		{
			_instance = new BenchmarkServerInstance(nameof(CatalogStoreCold));
		}

		[IterationCleanup(Target = nameof(CatalogStoreCold))]
		public void CleanUpAfterCatalogStoreCold()
		{
			DisposeTheInstance();
		}

		/// <summary>
		/// The catalog store's read path: a server started on an instance directory a previous start already populated,
		/// then stopped. This is the number research-catalog-load-profile extends to realistic catalogs.
		/// </summary>
		[Benchmark]
		public void CatalogStoreWarm()
		{
			_instance.Server.Start();
			_instance.Server.Stop();
		}

		/// <summary>Populates one catalog, outside any measured region, for the iterations to copy.</summary>
		[GlobalSetup(Target = nameof(CatalogStoreWarm))]
		public void PopulateACatalogToCopy()
		{
			_warmTemplate = new BenchmarkServerInstance("CatalogStoreWarmTemplate");
			_warmTemplate.Server.Start();
			_warmTemplate.Server.Stop();
		}

		/// <summary>
		/// Copies the populated directory for this iteration, which is also what restores it: a start writes to the
		/// catalog it reads, so an iteration must never be given the directory a previous iteration started on.
		/// </summary>
		[IterationSetup(Target = nameof(CatalogStoreWarm))]
		public void CopyThePopulatedInstance()
		{
			_instance = new BenchmarkServerInstance(nameof(CatalogStoreWarm), _warmTemplate);
		}

		[IterationCleanup(Target = nameof(CatalogStoreWarm))]
		public void CleanUpAfterCatalogStoreWarm()
		{
			DisposeTheInstance();
		}

		[GlobalCleanup(Target = nameof(CatalogStoreWarm))]
		public void DeleteThePopulatedCatalog()
		{
			BenchmarkServerInstance template = _warmTemplate;
			_warmTemplate = null;
			if (template != null)
				template.Dispose();
		}
	}
}
