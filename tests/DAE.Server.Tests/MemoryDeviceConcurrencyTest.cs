/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Sessions using in-memory tables at the same moment. The memory device keeps each database-scoped table as B-trees that
	/// every session shares, and once did no locking of its own: concurrent inserts split the same nodes, and a key check read
	/// a half-moved node (a NullReferenceException in the key compare, reported as "A nil was encountered where a value was
	/// expected"). The catalog device builds System.Sessions and System.Processes from the server's lists while other threads
	/// add, remove and dispose their entries.
	/// </summary>
	[TestFixture]
	public class MemoryDeviceConcurrencyTest
	{
		private const string TableName = "MemoryConcurrent";
		// Sizes found by running against the unlocked device: 8 sessions inserting 500 rows each, with one session reading,
		// failed in 4 of 4 runs, within 200 ms in each run that was timed. If it stops catching an unlocked table, raise RowCount first.
		private const int WriterCount = 8;
		private const int RowCount = 500;

		[Test]
		public void ConcurrentInsertsIntoOneMemoryTableAllLand()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(MemoryDeviceConcurrencyTest)))
			{
				instance.Server.Start();
				// System.Temp holds at most 1000 rows per table by default.
				instance.ExecuteScript("alter device System.Temp alter class { alter \"MaxRowCount\" = \"-1\" };");
				instance.ExecuteScript(String.Format("create table {0} in System.Temp {{ ID : Integer, Session : Integer, key {{ ID }} }};", TableName));

				ConcurrentBag<string> failures = new ConcurrentBag<string>();
				Stopwatch watch = Stopwatch.StartNew();
				RunWithReaders
				(
					instance,
					WriterCount,
					writer =>
						// Keys interleave across writers, so that every writer inserts into the same leaf nodes.
						String.Format("for var LIndex := 0 to {0} do insert row {{ LIndex * {1} + {2} ID, {2} Session }} into {3};", RowCount - 1, WriterCount, writer, TableName),
					process =>
					{
						process.Evaluate(String.Format("Count({0})", TableName), null);
						process.Evaluate(String.Format("exists ({0} where ID = {1})", TableName, RowCount), null);
					},
					1,
					failures
				);
				TestContext.Progress.WriteLine("{0} sessions x {1} inserts in {2} ms.", WriterCount, RowCount, watch.ElapsedMilliseconds);

				Assert.That(failures, Is.Empty, "Concurrent inserts failed:\r\n" + String.Join("\r\n", failures));
				Assert.That(instance.Evaluate(String.Format("Count({0})", TableName)), Is.EqualTo(WriterCount * RowCount));
				// Every key is found through the key's index, not only counted along the leaves.
				Assert.That
				(
					instance.Evaluate(String.Format("Count(({0} over {{ ID }}) where not exists ({0} rename {{ ID KeyID }} where KeyID = ID))", TableName)),
					Is.EqualTo(0),
					"Rows whose key lookup fails."
				);
				for (int writer = 0; writer < WriterCount; writer++)
					Assert.That(instance.Evaluate(String.Format("Count({0} where Session = {1})", TableName, writer)), Is.EqualTo(RowCount), "Rows of session " + writer);
			}
		}

		/// <summary>
		/// Two sessions read System.Sessions and System.Processes, each seeing the other's process run, while four connect,
		/// start a process and disconnect in a loop. Against the unfixed server this failed in 4 of 4 runs: the list enumerator
		/// read past the end of a list that shrank, and a session or process that was being disposed had already cleared what the
		/// catalog device read from it (a session's User, a process's executing programs).
		/// </summary>
		[Test]
		public void SessionAndProcessListsReadWhileSessionsComeAndGo()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(MemoryDeviceConcurrencyTest)))
			{
				instance.Server.Start();

				ConcurrentBag<string> failures = new ConcurrentBag<string>();
				RunWithReaders
				(
					instance,
					4,
					writer => null,
					process =>
					{
						process.Evaluate("Count(System.Sessions)", null);
						process.Evaluate("Count(System.Processes)", null);
					},
					2,
					failures
				);

				Assert.That(failures, Is.Empty, "Reading the session and process lists failed:\r\n" + String.Join("\r\n", failures));
			}
		}

		/// <summary>
		/// The catalog device rebuilds System.Sessions on every read by truncating its table, which replaces the table's trees.
		/// A cursor opened before another session's read kept the dropped tree, and positioning it again (First, Last, a seek)
		/// failed with a NullReferenceException.
		/// </summary>
		[Test]
		public void CursorOnCatalogTableRepositionsAfterAnotherSessionRereadsIt()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(MemoryDeviceConcurrencyTest)))
			{
				instance.Server.Start();
				IServerSession session = instance.Connect();
				try
				{
					IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
					try
					{
						IServerCursor cursor = process.OpenCursor("select System.Sessions", null);
						try
						{
							Assert.That(cursor.Next(), Is.True);
							instance.Evaluate("Count(System.Sessions)");
							cursor.First();
							int count = 0;
							while (cursor.Next())
								count++;
							Assert.That(count, Is.GreaterThan(0), "Rows after First.");
							cursor.Last();
							Assert.That(cursor.Prior(), Is.True);
						}
						finally
						{
							process.CloseCursor(cursor);
						}
					}
					finally
					{
						session.StopProcess(process);
					}
				}
				finally
				{
					instance.Server.Disconnect(session);
				}
			}
		}

		/// <summary>
		/// An ordered read of a catalog table opens a scan on the tree of the requested order, while another session's read of
		/// the same table truncates it, which rebuilds the table's list of trees. Choosing the tree from that list without the
		/// table's lock failed with an ArgumentOutOfRangeException. System.ServerLinks is rebuilt on every read and has a second
		/// key, { Name }, kept in its own tree.
		/// </summary>
		[Test]
		public void OrderedReadsOfCatalogTableWhileOthersRereadIt()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(MemoryDeviceConcurrencyTest)))
			{
				instance.Server.Start();

				ConcurrentBag<string> failures = new ConcurrentBag<string>();
				RunWithReaders
				(
					instance,
					1,
					writer => "for var LIndex := 0 to 2000 do var LCount := Count(System.ServerLinks);",
					process => process.CloseCursor(process.OpenCursor("select System.ServerLinks order by { Name }", null)),
					2,
					failures
				);

				Assert.That(failures, Is.Empty, "Ordered reads of System.ServerLinks failed:\r\n" + String.Join("\r\n", failures));
			}
		}

		/// <summary>
		/// Runs <paramref name="writerCount"/> sessions at once, each executing its script, while <paramref name="readerCount"/>
		/// further sessions repeat <paramref name="read"/> until every writer has finished. A null script means the writer
		/// instead connects, starts a process and disconnects in a loop for a second. Failures are gathered, not thrown.
		/// </summary>
		private static void RunWithReaders
		(
			TestServerInstance instance,
			int writerCount,
			Func<int, string> script,
			Action<IServerProcess> read,
			int readerCount,
			ConcurrentBag<string> failures
		)
		{
			using Barrier start = new Barrier(writerCount + readerCount);
			int writersRunning = writerCount;
			List<Thread> threads = new List<Thread>();
			for (int writer = 0; writer < writerCount; writer++)
			{
				int writerNumber = writer;
				threads.Add
				(
					new Thread(() =>
					{
						try
						{
							RunSession(instance, start, failures, "writer " + writerNumber, process =>
							{
								string text = script(writerNumber);
								if (text != null)
									process.ExecuteScript(text);
								else
									for (Stopwatch watch = Stopwatch.StartNew(); watch.ElapsedMilliseconds < 1000 && failures.IsEmpty;)
									{
										IServerSession session = instance.Connect();
										try
										{
											session.StopProcess(session.StartProcess(new ProcessInfo(session.SessionInfo)));
										}
										finally
										{
											instance.Server.Disconnect(session);
										}
									}
							});
						}
						finally
						{
							Interlocked.Decrement(ref writersRunning);
						}
					})
				);
			}
			for (int reader = 0; reader < readerCount; reader++)
			{
				int readerNumber = reader;
				threads.Add
				(
					new Thread(() =>
						RunSession(instance, start, failures, "reader " + readerNumber, process =>
						{
							while (Volatile.Read(ref writersRunning) > 0 && failures.IsEmpty)
								read(process);
						})
					)
				);
			}

			foreach (Thread thread in threads)
				thread.Start();
			foreach (Thread thread in threads)
				thread.Join();
		}

		private static void RunSession(TestServerInstance instance, Barrier start, ConcurrentBag<string> failures, string name, Action<IServerProcess> body)
		{
			bool reachedStart = false;
			try
			{
				IServerSession session = instance.Connect();
				try
				{
					IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
					try
					{
						reachedStart = true;
						start.SignalAndWait();
						body(process);
					}
					finally
					{
						session.StopProcess(process);
					}
				}
				finally
				{
					instance.Server.Disconnect(session);
				}
			}
			catch (Exception exception)
			{
				failures.Add(String.Format("{0}: {1}", name, exception));
				// Otherwise the other sessions would wait at the barrier for this one forever.
				if (!reachedStart)
					start.RemoveParticipant();
			}
		}
	}
}
