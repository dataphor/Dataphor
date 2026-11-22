/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The test plan's layer 4: a Dataphor server running as its own program, reached over a real network connection by
	/// the client code the product ships.
	/// </summary>
	/// <remarks>
	/// <para>
	/// What the process boundary establishes, and nothing else in this suite does: a server started as a separate
	/// program, from an instance list of its own, serves the shipped client stack and stops when it is asked to. It is
	/// also the only place a second server, or a second client program, can exist without the two sharing the engine's
	/// remaining process-wide state - the object-id counter was part of that until
	/// bug-object-id-counter-is-process-wide gave each engine a generator of its own.
	/// </para>
	/// <para>
	/// Deliberately not repeated here: streams over the wire, the message-size quotas and typed faults. Those already
	/// run against the real host with the real client over a real socket in <see cref="CoreWcfProtocolTest"/>, and
	/// moving the host into another program changes no part of that path, so a second copy would cost suite time and
	/// establish nothing. The client-side stream manager and the server-side one are separate objects whether or not
	/// they share a process, which is why moving the host exercises nothing new there.
	/// </para>
	/// <para>
	/// Also not here: the clock by which a server reclaims a session whose client stopped pinging it. That needs a client
	/// that can be made to stop, so it gets a fixture of its own; and the locks such a reclaim releases are asserted in
	/// process by <see cref="AbandonedSessionCleanupTest"/> and are not repeated anywhere.
	/// </para>
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: the child servers share no engine state with this process, but the clients these cases
	// build do - each LocalServer starts a client-side engine here, and two engines in one process share whatever
	// engine statics are left (see the note on TestServerInstance). Nothing in this project is parallelizable.
	public class OutOfProcessProtocolTest
	{
		private const string InstanceName = "OutOfProcess";

		/// <summary>How many rows the round-trip case inserts through one prepared plan.</summary>
		private const int RowCount = 3;

		/// <summary>One child server for the whole fixture: a start is a server start plus a catalog store creation.</summary>
		private OutOfProcessInstance _instance;

		/// <summary>
		/// Counts the names handed out by <see cref="Unique"/>. The fixture's child server is shared by every case in this
		/// class and its catalog lives as long as the fixture, so each case needs names no other case has used; NUnit's order
		/// within a fixture is not something to depend on.
		/// </summary>
		private int _next;

		private string Unique(string prefix) { return prefix + (++_next).ToString(); }

		[OneTimeSetUp]
		public void SetUpFixture()
		{
			_instance = new OutOfProcessInstance(InstanceName);
		}

		/// <summary>Stops the child, and runs on the failure path too, or a server is left holding a port and a folder.</summary>
		[OneTimeTearDown]
		public void TearDownFixture()
		{
			if (_instance != null)
			{
				_instance.Dispose();
				_instance = null;
			}
		}

		private static ProtocolClient NewClient(OutOfProcessInstance instance)
		{
			return new ProtocolClient(instance.HostName, instance.InstanceName, instance.PortNumber);
		}

		/// <summary>
		/// The whole round trip against a server in another program: connect as the built-in administrator, start a
		/// process, create a table, execute one prepared plan several times, read the rows back through a cursor, and
		/// shut the session down - and then do it again on a second connection, which is how the server says it is still
		/// serving after the first client came and went.
		/// </summary>
		/// <remarks>
		/// The D4 stays on built-in operators and types over the memory device, for the reason CoreWcfProtocolTest
		/// records: a class alias the client-side engine cannot find sends LocalServer down the library-download path.
		/// </remarks>
		[Test]
		public void AServerInAnotherProgramServesTheShippedClient()
		{
			using (ProtocolClient client = NewClient(_instance))
				client.OnProcess
				(
					process =>
					{
						process.Execute("create table AcrossProcesses { ID : Integer, Name : String, key { ID } };", null);

						// One plan, prepared once and executed several times, which nothing else in this suite does:
						// CoreWcfProtocolTest prepares and executes once, so nothing holds the server to keeping a plan
						// handle usable between calls, or the client to using the handle it has rather than preparing again.
						IServerStatementPlan plan = process.PrepareStatement("insert row { AID ID, AName Name } into AcrossProcesses;", InsertParameters(process, 0));
						try
						{
							for (int id = 1; id <= RowCount; id++)
								plan.Execute(InsertParameters(process, id));
						}
						finally
						{
							process.UnprepareStatement(plan);
						}

						Assert.That(Names(process), Is.EqualTo(ExpectedNames()), "The rows the prepared plan inserted did not come back through the cursor.");
					}
				);

			using (ProtocolClient client = NewClient(_instance))
				client.OnProcess
				(
					process => Assert.That(Names(process), Is.EqualTo(ExpectedNames()), "A second connection did not find what the first one wrote, so the server did not survive the first client.")
				);
		}

		/// <summary>The parameters of one insert, by the names the prepared statement above uses.</summary>
		private static DataParams InsertParameters(IServerProcess process, int id)
		{
			DataParams result = new DataParams();
			result.Add(DataParam.Create(process, "AID", id));
			result.Add(DataParam.Create(process, "AName", Name(id)));
			return result;
		}

		private static string Name(int id)
		{
			return "row " + id.ToString();
		}

		private static List<string> ExpectedNames()
		{
			List<string> result = new List<string>();
			for (int id = 1; id <= RowCount; id++)
				result.Add(Name(id));
			return result;
		}

		/// <summary>Every name in the table, in key order, read back over the wire through a cursor.</summary>
		private static List<string> Names(IServerProcess process)
		{
			return ReadRows(process, "select AcrossProcesses order by { ID }", row => (string)row["Name"]);
		}

		/// <summary>Opens <paramref name="expression"/> as a cursor, projects every row it yields, and closes it on either path.</summary>
		private static List<T> ReadRows<T>(IServerProcess process, string expression, Func<IRow, T> ofRow)
		{
			List<T> result = new List<T>();
			IServerCursor cursor = process.OpenCursor(expression, null);
			try
			{
				while (cursor.Next())
					using (IRow row = cursor.Select())
						result.Add(ofRow(row));
			}
			finally
			{
				process.CloseCursor(cursor);
			}
			return result;
		}

		/// <summary>
		/// The child stops of its own accord when it is asked to, within a bounded wait and with exit code 0, and its
		/// port stops answering.
		/// </summary>
		/// <remarks>
		/// This is the only place in the suite where DataphorServiceHost.Stop() runs in a program whose exit can be
		/// observed, and it runs with a client channel open - the state Kestrel's graceful shutdown has to get out of,
		/// which NetTcpServiceHost gives it ten seconds to do before it aborts the connection. So the case is not a fast
		/// one (measured about six seconds), and what it establishes is not "the server stopped" but "the program
		/// stopped, by itself, without being killed". The teardown path of every other case depends on that.
		/// </remarks>
		[Test]
		public void TheChildServerStopsWhenItIsAsked()
		{
			// A child of its own: stopping the fixture's would leave the other cases in this class depending on the order
			// NUnit happens to run them in.
			using (OutOfProcessInstance instance = new OutOfProcessInstance(InstanceName + "Stop"))
			{
				Assert.That(HostedInstance.IsListeningOn(instance.PortNumber), Is.True, instance.Diagnostic("The child server is not listening on the port it reported."));

				// Still holding its channel open when the server is stopped below, which is the point. Disposing it
				// afterwards is safe: LocalServer.Dispose ignores a relinquish that cannot reach the server, and
				// ClientServer.Close aborts a channel it cannot close.
				using (ProtocolClient client = NewClient(instance))
				{
					client.OnProcess(process => Assert.That((int)process.Evaluate("1 + 1", null).AsNative, Is.EqualTo(2), "The child server does not answer."));

					instance.Stop();

					Assert.That(instance.ExitCode, Is.EqualTo(0), instance.Diagnostic("The child server did not stop by itself when it was asked to."));
					Assert.That(HostedInstance.IsListeningOn(instance.PortNumber), Is.False, "The child server has exited but something is still listening on its port.");
				}
			}
		}

		/// <summary>Which row of a walk a bookmark is taken on; the fifth, as the test this case replaces took it.</summary>
		private const int BookmarkPosition = 5;

		/// <summary>How many fetch batches the second pass of the cursor case walks over.</summary>
		private const int BatchesInTheSecondPass = 10;

		/// <summary>
		/// A cursor over more rows than the client holds at a time, walked, navigated and changed through, against a server
		/// in another program.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is what the deleted tests/DAE.Client.Tests/CLI/CLITest.cs's TestCLI did, and the reason it is worth keeping:
		/// the client reads a cursor in blocks of ProcessInfo.FetchCount and holds a block at a time in a buffer of its own
		/// (<c>LocalCursor</c>), so a cursor over more rows than that is the only thing that exercises the refetch at a block
		/// boundary, and bookmarks and in-place changes that cross one. <see cref="CoreWcfProtocolTest"/>'s cursor case reads
		/// two rows and so never leaves the first block.
		/// </para>
		/// <para>
		/// The forward sum is how the old test established that every row arrived exactly once. It would also pass for a
		/// cursor that returned the right rows in the wrong order, which is why the ordered walk is here as well; both are
		/// needed and neither replaces the other.
		/// </para>
		/// </remarks>
		[Test]
		public void ACursorCrossesFetchBatchesAndChangesThroughItself()
		{
			string table = Unique("AcrossBatches");
			using (ProtocolClient client = NewClient(_instance))
				client.OnProcess
				(
					process =>
					{
						// The block size the client-side cursor will actually use. ProcessInfo takes it from the session's
						// SessionInfo, which is where the default lives, so a change to that default changes how many rows
						// this case inserts rather than quietly stopping it from crossing a boundary.
						int fetchCount = process.ProcessInfo.FetchCount;
						Assert.That(fetchCount, Is.GreaterThan(1), "A FetchCount of 1 turns the client-side row buffer off, and the buffer is what this case is about.");

						process.Execute(String.Format("create table {0} {{ ID : Integer, key {{ ID }} }};", table), null);
						InsertIdentifiers(process, table, fetchCount);

						// Exactly one block: the old test's first pass, and where it made its changes through the cursor.
						OnCursor
						(
							process,
							table,
							cursor =>
							{
								Assert.That(SumIdentifiers(cursor), Is.EqualTo(ArithmeticSeries(fetchCount)), "The identifiers of one block of rows did not add up, so a row arrived twice, not at all, or wrong.");
								AssertIdentifiersInOrder(cursor, fetchCount, "One block of rows did not arrive in the cursor's order.");
								AssertChangesThroughTheCursor(cursor);
								AssertABookmarkSurvivesTheWalk(cursor, BookmarkPosition);
							}
						);

						// Ten blocks, which is the part that crosses boundaries: the client discards a block when it moves
						// off the end of it, so every assertion below is also an assertion about the refetch.
						int manyBlocks = fetchCount * BatchesInTheSecondPass;
						process.Execute(String.Format("delete {0};", table), null);
						InsertIdentifiers(process, table, manyBlocks);

						OnCursor
						(
							process,
							table,
							cursor =>
							{
								Assert.That(SumIdentifiers(cursor), Is.EqualTo(ArithmeticSeries(manyBlocks)), "The identifiers of ten blocks of rows did not add up, so the refetch at a block boundary lost, repeated or corrupted a row.");
								AssertIdentifiersInOrder(cursor, manyBlocks, "Ten blocks of rows did not arrive in the cursor's order.");
								AssertABookmarkSurvivesTheWalk(cursor, BookmarkPosition);
							}
						);
					}
				);
		}

		/// <summary>
		/// Two client connections to the same child server: a table one creates is visible to the other, in both directions,
		/// and a table one drops stops being visible to the other.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Each <c>LocalServer</c> keeps a client-side catalog cache in an engine of its own, and refreshes it when a prepare
		/// comes back carrying a catalog fragment and a newer cache time stamp. Nothing else in this suite reaches that: every
		/// other fixture has at most one client-side engine. Two clients against a server in another program is the
		/// configuration the product runs in, and the first place this behaviour is reachable.
		/// </para>
		/// <para>
		/// The refresh happens while the expression is prepared, not while it runs: the server compiles the text against its
		/// own catalog and returns the table variable's definition for the client to apply to its cache. So a client whose
		/// cache never refreshed fails before any row is read, and the drop at the end fails in the server's compiler with an
		/// unresolved identifier.
		/// </para>
		/// <para>
		/// Both clients are built before any of the statements below run, so that each one's cache predates every statement
		/// the other executes; B is built after A, which is the order that puts a second client-side engine's start between
		/// A's construction and A's first allocation. That order mattered while the object-id counter was shared by every
		/// engine in the process; bug-object-id-counter-is-process-wide gave each engine its own, so B's start no longer
		/// reaches A's identifiers either way.
		/// </para>
		/// </remarks>
		[Test]
		public void ASchemaChangeOnOneConnectionIsVisibleOnAnother()
		{
			string fromA = Unique("CacheFromA"), fromB = Unique("CacheFromB");

			using (ProtocolClient a = NewClient(_instance))
			using (ProtocolClient b = NewClient(_instance))
				a.OnProcess
				(
					processA => b.OnProcess
					(
						processB =>
						{
							CreateTableHolding(processA, fromA, 1);
							Assert.That(Identifiers(processB, fromA), Is.EqualTo(new List<int> { 1 }), "The second connection cannot see the table the first one created, so its client-side catalog cache never refreshed.");

							CreateTableHolding(processB, fromB, 2);
							Assert.That(Identifiers(processA, fromB), Is.EqualTo(new List<int> { 2 }), "The first connection cannot see the table the second one created; the cache is per connection, so a refresh in one is no evidence about the other.");

							processA.Execute(String.Format("drop table {0};", fromA), null);
							CompilerException error = Assert.Throws<CompilerException>(() => Identifiers(processB, fromA), "The second connection still resolves a table the first one dropped, which is what a cache that only ever grows would do.");
							Assert.That(error.Code, Is.EqualTo((int)CompilerException.Codes.UnknownIdentifier), error.ToString());
						}
					)
				);
		}

		/// <summary>The cursor the deleted test opened: every capability its navigation, bookmark and update steps need.</summary>
		private static string Selection(string table)
		{
			return String.Format("select {0} order by {{ ID }} capabilities {{ navigable, backwardsnavigable, bookmarkable, searchable, updateable }}", table);
		}

		/// <summary>Opens the cursor of <see cref="Selection"/> over <paramref name="table"/> and closes it on either path.</summary>
		private static void OnCursor(IServerProcess process, string table, Action<IServerCursor> body)
		{
			IServerCursor cursor = process.OpenCursor(Selection(table), null);
			try
			{
				body(cursor);
			}
			finally
			{
				process.CloseCursor(cursor);
			}
		}

		/// <summary>Inserts the identifiers 1..<paramref name="count"/>, in one statement, as the deleted test did.</summary>
		private static void InsertIdentifiers(IServerProcess process, string table, int count)
		{
			process.Execute(String.Format("for var LIndex := 1 to {0} do insert row {{ LIndex ID }} into {1};", count, table), null);
		}

		/// <summary>1 + 2 + ... + <paramref name="count"/>: what the identifiers of a table <see cref="InsertIdentifiers"/> filled add up to.</summary>
		private static long ArithmeticSeries(int count)
		{
			return ((long)count * (count + 1)) / 2;
		}

		/// <summary>
		/// Resets the cursor, walks it to end of file handing each row and its one-based position to
		/// <paramref name="onRow"/>, and returns how many rows it passed.
		/// </summary>
		/// <remarks>
		/// Selects into one row requested from the plan rather than into a row constructed per fetch, which is both how the
		/// shipped client reads a cursor and what keeps a walk of ten blocks from allocating a row for each of them. Released
		/// in a finally, or an assertion a caller makes mid-walk leaks it. The reset is here rather than at the call sites so
		/// that no walk depends on where the walk before it left the cursor.
		/// </remarks>
		private static int Walk(IServerCursor cursor, Action<IRow, int> onRow)
		{
			cursor.Reset();
			int position = 0;
			IRow row = cursor.Plan.RequestRow();
			try
			{
				while (cursor.Next())
				{
					cursor.Select(row);
					onRow(row, ++position);
				}
			}
			finally
			{
				cursor.Plan.ReleaseRow(row);
			}
			return position;
		}

		/// <summary>Walks the whole cursor, adding up the identifiers it passes.</summary>
		private static long SumIdentifiers(IServerCursor cursor)
		{
			long result = 0;
			Walk(cursor, (row, position) => result += (int)row[0]);
			return result;
		}

		/// <summary>Asserts that the cursor yields the identifiers 1..<paramref name="count"/> in that order.</summary>
		private static void AssertIdentifiersInOrder(IServerCursor cursor, int count, string because)
		{
			int walked = Walk(cursor, (row, position) => Assert.That((int)row[0], Is.EqualTo(position), because));
			Assert.That(walked, Is.EqualTo(count), because);
		}

		/// <summary>
		/// Updates, deletes and inserts through the cursor itself, asserting after each change which row the cursor then
		/// shows, and leaves the table holding the identifiers it started with.
		/// </summary>
		/// <remarks>
		/// Which row the cursor shows after a change is the behaviour being pinned, and these assertions are the only record
		/// of it: an update that moves the row to the front of the cursor's order leaves the cursor on it, a delete leaves the
		/// cursor on the row that followed, and an insert leaves it on the row inserted. Taken from the deleted
		/// tests/DAE.Client.Tests/CLI/CLITest.cs; they are not to be loosened to make a run green.
		/// </remarks>
		private static void AssertChangesThroughTheCursor(IServerCursor cursor)
		{
			cursor.Reset();
			Assert.That(cursor.Next(), Is.True, "The cursor has no first row.");
			Assert.That(cursor.Next(), Is.True, "The cursor has no second row.");

			// Ordered by ID, so an identifier below every other moves this row to the front of the order.
			using (IRow row = cursor.Select())
			{
				row[0] = -1;
				cursor.Update(row);
			}
			using (IRow row = cursor.Select())
				Assert.That((int)row[0], Is.EqualTo(-1), "The cursor does not show the value it updated through itself.");

			// Now the first row, so deleting it leaves the cursor on what was the second: identifier 1.
			cursor.Delete();
			using (IRow row = cursor.Select())
			{
				Assert.That((int)row[0], Is.EqualTo(1), "The cursor does not show the row that followed the one it deleted.");

				// Puts the identifier the update above moved out of the way back, so the table is 1..n again and the walks
				// that follow can assert the order without knowing what happened here.
				row[0] = 2;
				cursor.Insert(row);
			}
			using (IRow row = cursor.Select())
				Assert.That((int)row[0], Is.EqualTo(2), "The cursor does not show the row it inserted through itself.");
		}

		/// <summary>
		/// Takes a bookmark on the <paramref name="position"/>th row, walks on to end of file, then goes back to it.
		/// </summary>
		/// <remarks>
		/// On a walk of more than one block the bookmark is taken in a block the client has discarded by the time it is used,
		/// which is the case worth having; on a walk of exactly one block it is the same check the deleted test made.
		/// </remarks>
		private static void AssertABookmarkSurvivesTheWalk(IServerCursor cursor, int position)
		{
			Guid bookmark = Guid.Empty;
			int walked = Walk(cursor, (row, at) => { if (at == position) bookmark = cursor.GetBookmark(); });
			Assert.That(walked, Is.GreaterThanOrEqualTo(position), "The cursor has fewer rows than the bookmark position this case asked for.");

			try
			{
				Assert.That(cursor.GotoBookmark(bookmark, true), Is.True, "The cursor could not go back to a bookmark it took earlier in the same walk.");
				using (IRow current = cursor.Select())
					Assert.That((int)current[0], Is.EqualTo(position), "The bookmark landed on a different row than the one it was taken on.");
			}
			finally
			{
				cursor.DisposeBookmark(bookmark);
			}
		}

		/// <summary>Creates a one-column table holding the single identifier <paramref name="identifier"/>.</summary>
		private static void CreateTableHolding(IServerProcess process, string table, int identifier)
		{
			process.Execute(String.Format("create table {0} {{ ID : Integer, key {{ ID }} }};", table), null);
			process.Execute(String.Format("insert row {{ {0} ID }} into {1};", identifier, table), null);
		}

		/// <summary>Every identifier in <paramref name="table"/>, in key order, read over the wire through a cursor.</summary>
		private static List<int> Identifiers(IServerProcess process, string table)
		{
			return ReadRows(process, String.Format("select {0} order by {{ ID }}", table), row => (int)row[0]);
		}
	}
}
