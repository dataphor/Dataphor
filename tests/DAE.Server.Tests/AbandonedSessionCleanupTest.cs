/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Service;
using Alphora.Dataphor.DAE.Store;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What the server does about a client that has vanished: its idle watcher relinquishes the connection, which rolls back
	/// the transaction the client left open and releases the catalog store's write lock other sessions' schema changes were
	/// waiting for.
	/// </summary>
	/// <remarks>
	/// The lowest layer that shows it: a RemoteServer - the remote CLI the service contract is written against - plus the
	/// ConnectionManager, both in process. No WCF host, no port, no network. What keeps a live client's connection from
	/// being reclaimed is the other half of the same mechanism and belongs to ConnectionKeepAliveTest; a real client process
	/// killed outright belongs to test-client-server.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: these cases start servers, and two engines in one process share the engine's static state (see
	// the note on TestServerInstance).
	public class AbandonedSessionCleanupTest
	{
		/// <summary>The idle threshold the watcher runs at here; the shipped default is five minutes.</summary>
		/// <remarks>
		/// The watcher wakes every quarter of this with a floor of a second, so at one second a connection that has never
		/// been pinged is relinquished within about two.
		/// </remarks>
		private const int IdleThresholdSeconds = 1;

		/// <summary>How long a case waits for the reclamation; generous, so that a loaded machine does not fail it.</summary>
		private static readonly TimeSpan ReclamationWait = TimeSpan.FromSeconds(30);

		/// <summary>
		/// A client establishes a connection, opens a transaction that changes the catalog, and then stops pinging - which is
		/// all a crashed process, a pulled cable or a switched-off workstation looks like from the server's side. The server
		/// relinquishes the connection, the abandoned transaction is rolled back rather than committed, and the store's write
		/// lock it held is released, so other sessions' schema changes run again.
		/// </summary>
		[Test]
		public void AnAbandonedTransactionIsRolledBackAndTheCatalogWriteLockReleased()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AnAbandonedTransactionIsRolledBackAndTheCatalogWriteLockReleased)))
			{
				// So that a session blocked by the abandoned transaction reports it in a second rather than in the store's
				// default thirty, which is what lets the wait below poll.
				instance.Server.CatalogStoreConnectionString = TestServerInstance.CatalogStoreConnectionString + ";BusyTimeout=1000";
				instance.Server.Start();

				using (RemoteServer remoteServer = new RemoteServer(instance.Server))
				{
					// The client that vanishes. Through the remote CLI directly rather than through a LocalServer: a
					// LocalServer pings its connection for as long as it is held, which is exactly what this case must not
					// have, and it would start an engine of its own for its catalog cache.
					IRemoteServerConnection connection = remoteServer.Establish("AbandonedClient", "localhost");
					IRemoteServerSession session = connection.Connect(new SessionInfo("Admin", ""));
					int processID;
					IRemoteServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo), out processID);
					process.BeginTransaction(IsolationLevel.CursorStability);
					process.ExecuteScript("create table AbandonedT { ID : Integer, key { ID } };", ProcessCallInfo.Empty);

					// Before the watcher exists, so that nothing can reclaim the connection while this is measured: without
					// it the final wait would pass on a server where no write lock was ever taken.
					AssertTheCatalogWriteLockIsHeld(instance);

					using (ConnectionManager connectionManager = new ConnectionManager(remoteServer, IdleThresholdSeconds))
					{
						TimeSpan waited = WaitForACatalogChangeToSucceed(instance, "AfterReclaimT");
						TestContext.Progress.WriteLine("A catalog change succeeded {0:0.0} s after the watcher started, at an idle threshold of {1} s.", waited.TotalSeconds, IdleThresholdSeconds);
					}

					// Rolled back, not committed. The expression has never been compiled - the abandoned client created the
					// table and never read it - so no cached plan can answer it; TransactionReadsItsOwnStoreWrites makes the
					// same point at length.
					Exception unknown = Assert.Catch(() => instance.Evaluate("Count(AbandonedT)"));
					Assert.That(FindError(unknown, (int)CompilerException.Codes.UnknownIdentifier), Is.Not.Null, "The abandoned transaction's table survived its rollback: " + unknown);

					// And the work done after the reclamation is real.
					Assert.That(instance.Evaluate("Count(AfterReclaimT)"), Is.EqualTo(0));

					// A call on the reclaimed client's process fails rather than reaching the disposed engine objects behind
					// it. In process that is whatever the dead CLI object raises; over the wire the service's handle table
					// has withdrawn the handle from the process's Disposed event and the call comes back as error 106205,
					// unknown object handle.
					//
					// NOTE: the type is not asserted because in process it is a NullReferenceException, not a Dataphor
					// error: every Remote* object nulls its inner reference when it is disposed and no member checks for
					// it. Class-wide and older than the reclamation mechanism, and unreachable in the product (over the
					// wire the handle goes first, and the only in-process client of the remote CLI is LocalServer, which
					// pings its connection and so is never reaped), which is why it is recorded here rather than filed. If
					// it ever becomes reachable the fix is a representation change across the Remote* family - a disposed
					// object that still answers with its own error - not a check at one site.
					Exception onADeadProcess = Assert.Catch(() => process.ExecuteScript("create table TooLateT { ID : Integer, key { ID } };", ProcessCallInfo.Empty), "A call on the reclaimed client's process succeeded.");
					TestContext.Progress.WriteLine("A call on the reclaimed process raised {0}: {1}", onADeadProcess.GetType().Name, onADeadProcess.Message);
				}
			}
		}

		/// <summary>
		/// Relinquishing a connection twice does nothing the second time. The idle watcher and a client's own CloseConnection
		/// can arrive together, as can the watcher and CloseConnections at shutdown; before the guard in
		/// RemoteServerConnection.Dispose the second call dereferenced the fields the first had nulled and raised a
		/// NullReferenceException, wrapped as a DataphorException.
		/// </summary>
		[Test]
		public void RelinquishingAConnectionTwiceDoesNothing()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(RelinquishingAConnectionTwiceDoesNothing)))
			{
				instance.Server.Start();

				using (RemoteServer remoteServer = new RemoteServer(instance.Server))
				{
					IRemoteServerConnection connection = remoteServer.Establish("TwiceRelinquished", "localhost");
					connection.Connect(new SessionInfo("Admin", ""));

					remoteServer.Relinquish(connection);
					Assert.DoesNotThrow(() => remoteServer.Relinquish(connection), "Relinquishing an already relinquished connection failed.");
				}
			}
		}

		/// <summary>
		/// Establishes that the abandoned transaction really holds the catalog store's write lock: an ordinary in-process
		/// session's catalog change waits for it and fails with 130103 at the busy timeout.
		/// </summary>
		private static void AssertTheCatalogWriteLockIsHeld(TestServerInstance instance)
		{
			Stopwatch watch = Stopwatch.StartNew();
			Exception exception = Assert.Catch(() => instance.ExecuteScript("create table LockedOutT { ID : Integer, key { ID } };"));
			watch.Stop();
			TestContext.Progress.WriteLine("A catalog change was refused after {0} ms while the abandoned transaction held the write lock.", watch.ElapsedMilliseconds);

			Assert.That
			(
				FindError(exception, (int)StoreException.Codes.StoreLocked),
				Is.Not.Null,
				"The abandoned transaction did not hold the catalog store's write lock, so this case would prove nothing about releasing it: " + exception
			);
		}

		/// <summary>
		/// Creates a table on a fresh in-process session, retrying for as long as the store's write lock is still held, and
		/// returns how long that took. Only the lock-wait error is retried; anything else fails the case.
		/// </summary>
		private static TimeSpan WaitForACatalogChangeToSucceed(TestServerInstance instance, string tableName)
		{
			// Each attempt takes about the store's busy timeout to fail, so the loop paces itself.
			Stopwatch waited = Stopwatch.StartNew();
			while (true)
			{
				try
				{
					instance.ExecuteScript(String.Format("create table {0} {{ ID : Integer, key {{ ID }} }};", tableName));
					return waited.Elapsed;
				}
				catch (Exception exception) when (FindError(exception, (int)StoreException.Codes.StoreLocked) != null)
				{
					if (waited.Elapsed > ReclamationWait)
						Assert.Fail(String.Format("The abandoned connection was not reclaimed: its transaction still held the catalog store's write lock {0:0.0} s after the idle watcher started, at an idle threshold of {1} s.", waited.Elapsed.TotalSeconds, IdleThresholdSeconds));
				}
			}
		}

		private static DataphorException FindError(Exception exception, int code)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is DataphorException dataphorException && dataphorException.Code == code)
					return dataphorException;
			return null;
		}
	}
}
