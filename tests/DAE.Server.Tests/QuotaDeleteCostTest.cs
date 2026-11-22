/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Draining a table one row at a time with "delete &lt;table&gt; return &lt;n&gt; by { &lt;order&gt; }" costs the same per row whatever
	/// the table's size. The in-memory device used to refuse to serve an ordered scan from an index it already had whenever the
	/// asking plan wanted a static cursor, which every data-modification statement does, so the engine sorted the whole table
	/// once per delete and the drain was quadratic (bug-engine-delete-quota-scans-the-table).
	/// </summary>
	[TestFixture]
	public class QuotaDeleteCostTest : InProcessTestFixture
	{
		private const string TableName = "QuotaDeleteCost";

		/// <summary>Drained first and never measured, so that neither measured drain pays a first-time cost the other does not.</summary>
		private const int WarmRows = 50;

		private const int SmallRows = 250;
		private const int LargeRows = 1000;

		/// <summary>Each size is drained twice and the cheaper of the two counted, so that one hiccup cannot fail the case.</summary>
		private const int Attempts = 2;

		/// <summary>
		/// How much dearer one row of the larger drain may be than one row of the smaller before this case fails.
		/// </summary>
		/// <remarks>
		/// A ratio, never an absolute time, so that the case says nothing about how fast the machine is. Measured on one slow
		/// development laptop in Debug, 2026-11-22, by putting the device's old rule back: with the defect the two drains cost
		/// 415 and 8,665 ms, a per-row ratio of 5.2 and a failing run of 19 s; without it the ratio came out between 0.95 and
		/// 1.18 over five runs (0.92 to 1.35 when each size was drained once rather than twice), and the case takes 0.4 s. Two
		/// sits above the widest of those and well below the defect.
		/// </remarks>
		private const double MaxPerRowGrowth = 2.0;

		/// <summary>
		/// The smaller drain counts as at least this many milliseconds in the comparison, so that a fast machine measuring it at
		/// the timer's resolution cannot fail the case by dividing by nearly nothing.
		/// </summary>
		/// <remarks>
		/// It costs nothing in sensitivity: with this floor the larger drain may take 80 ms, and a quadratic drain of 1,000 rows
		/// took 7,866 ms on the machine above - a machine would have to be a hundred times faster than that one for a quadratic
		/// drain to come in under the bound.
		/// </remarks>
		private const double SmallFloorMilliseconds = 10.0;

		/// <summary>
		/// Carried and registered for one thing: its Register.d4 lifts the in-memory device's row cap, which otherwise stops a
		/// table on System.Temp at 1,000 rows.
		/// </summary>
		/// <remarks>
		/// The larger drain is exactly at that cap and would fit without the lift, so this is headroom for the sizes above, and
		/// it reuses the one library in the suite that owns the lift rather than altering the device here as well.
		/// </remarks>
		protected override string[] Libraries => [PerfBaselineTest.PerfLibrary];

		protected override string[] LibrariesToRegister => [PerfBaselineTest.PerfLibrary];

		[Test]
		public void DrainingATableOneRowAtATimeCostsTheSamePerRowAtEverySize()
		{
			DrainMilliseconds(WarmRows);

			long small = CheapestDrainMilliseconds(SmallRows);
			long large = CheapestDrainMilliseconds(LargeRows);

			double smallPerRow = Math.Max(small, SmallFloorMilliseconds) / SmallRows;
			double largePerRow = large / (double)LargeRows;
			TestContext.Progress.WriteLine
			(
				"Drain of {0} rows: {1} ms ({2:F3} ms/row); of {3} rows: {4} ms ({5:F3} ms/row); ratio {6:F2}.",
				SmallRows, small, smallPerRow, LargeRows, large, largePerRow, largePerRow / smallPerRow
			);

			Assert.That
			(
				largePerRow,
				Is.LessThanOrEqualTo(MaxPerRowGrowth * smallPerRow),
				String.Format
				(
					"One row of a {0}-row drain cost {1:F3} ms against {2:F3} ms of a {3}-row drain ({4} ms against {5} ms in total), more than {6:F0}x - the cost of \"delete <table> return <n>\" is growing with the table's size again.",
					LargeRows, largePerRow, smallPerRow, SmallRows, large, small, MaxPerRowGrowth
				)
			);
		}

		/// <summary>Drains a table of the given size <see cref="Attempts"/> times and returns the cheapest of those drains.</summary>
		private long CheapestDrainMilliseconds(int rows)
		{
			long cheapest = Int64.MaxValue;
			for (int attempt = 0; attempt < Attempts; attempt++)
				cheapest = Math.Min(cheapest, DrainMilliseconds(rows));
			return cheapest;
		}

		/// <summary>Fills a table of the given size and returns how long draining it one row at a time took, in milliseconds.</summary>
		/// <remarks>
		/// One table name for every size, so that the statement whose cost is measured is the same text each time: a create and a
		/// drop flush the plan cache, so each drain compiles its own plan and all of them carry that cost alike.
		/// </remarks>
		private long DrainMilliseconds(int rows)
		{
			ExecuteScript(String.Format("create table {0} in System.Temp {{ ID : Integer, key {{ ID }} }};", TableName));
			try
			{
				ExecuteScript(String.Format("for var LIndex := 1 to {0} do insert row {{ LIndex ID }} into {1};", rows, TableName));
				Stopwatch watch = Stopwatch.StartNew();
				ExecuteScript(String.Format("while exists ({0}) do delete {0} return 1 by {{ ID }};", TableName));
				watch.Stop();
				return watch.ElapsedMilliseconds;
			}
			finally
			{
				ExecuteScript(String.Format("drop table {0};", TableName));
			}
		}
	}
}
