/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Service;
using Alphora.Dataphor.DAE.Store;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What the server does about a native CLI client that has vanished: its idle watcher stops the session, which rolls
	/// back the transaction the client left open and releases the catalog store's write lock other sessions' schema
	/// changes were waiting for - and what it does not do, which is touch a session with a call in flight on it.
	/// </summary>
	/// <remarks>
	/// The lowest layer that shows it: a NativeServer - the native CLI the service contract is written against - plus the
	/// NativeSessionManager, both in process. No WCF host, no port, no network. What keeps a live client's session from
	/// being reclaimed is the other half of the same mechanism and belongs to NativeSessionKeepAliveTest, which needs a
	/// real client to send the keep-alive.
	///
	/// Deliberately a close parallel of AbandonedSessionCleanupTest, which does this for the main interface; read that
	/// file beside this one.
	/// NOTE: the three helpers at the foot of this fixture are that file's, copied rather than shared. Each is a few
	/// lines, and each carries a failure message that names what this fixture drives (a native session, not a
	/// connection), which is most of what they are. If a third fixture ever needs to watch the catalog store's write
	/// lock, extract them then.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: these cases start servers, and two engines in one process share the engine's static state (see
	// the note on TestServerInstance).
	public class NativeSessionCleanupTest
	{
		/// <summary>The idle threshold the watcher runs at here; the shipped default is five minutes.</summary>
		/// <remarks>
		/// The watcher wakes every quarter of this with a floor of a second, and a session is reclaimed once its clock
		/// reads more than the threshold, so at one second a session nothing calls is stopped within about three.
		/// </remarks>
		private const int IdleThresholdSeconds = 1;

		/// <summary>How long a case waits for the reclamation; generous, so that a loaded machine does not fail it.</summary>
		private static readonly TimeSpan ReclamationWait = TimeSpan.FromSeconds(30);

		/// <summary>How long the one call this fixture makes in flight runs for: several sweeps at the threshold above.</summary>
		private const int LongCallMilliseconds = 5000;

		/// <summary>How long the case waits for that call; far longer than the call, because a case that hangs says nothing.</summary>
		private static readonly TimeSpan LongCallWait = TimeSpan.FromSeconds(60);

		/// <summary>
		/// A client starts a native session, opens a transaction that changes the catalog, and then makes no further call -
		/// which is all a crashed process, a pulled cable or a switched-off workstation looks like from the server's side,
		/// this interface having no connection whose loss it could notice. The server stops the session, the abandoned
		/// transaction is rolled back rather than committed, and the store's write lock it held is released, so other
		/// sessions' schema changes run again.
		/// </summary>
		[Test]
		public void AnAbandonedNativeTransactionIsRolledBackAndTheCatalogWriteLockReleased()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AnAbandonedNativeTransactionIsRolledBackAndTheCatalogWriteLockReleased)))
			{
				// So that a session blocked by the abandoned transaction reports it in a second rather than in the store's
				// default thirty, which is what lets the wait below poll.
				instance.Server.CatalogStoreConnectionString = TestServerInstance.CatalogStoreConnectionString + ";BusyTimeout=1000";
				instance.Server.Start();

				NativeServer nativeServer = new NativeServer(instance.Server);

				// The client that vanishes. Through the native CLI directly rather than through a NativeCLISession: that
				// client pings its session for as long as it is held, which is exactly what this case must not have.
				NativeSessionHandle sessionHandle = nativeServer.StartSession(new NativeSessionInfo { HostName = "AbandonedNativeClient" });
				nativeServer.BeginTransaction(sessionHandle, NativeIsolationLevel.CursorStability);
				nativeServer.Execute(sessionHandle, "create table AbandonedNativeT { ID : Integer, key { ID } };", null, NativeExecutionOptions.Default);

				// Before the watcher exists, so that nothing can reclaim the session while this is measured: without it the
				// final wait would pass on a server where no write lock was ever taken.
				AssertTheCatalogWriteLockIsHeld(instance);

				using (NativeSessionManager sessionManager = new NativeSessionManager(nativeServer, IdleThresholdSeconds))
				{
					TimeSpan waited = WaitForACatalogChangeToSucceed(instance, "AfterNativeReclaimT");
					TestContext.Progress.WriteLine("A catalog change succeeded {0:0.0} s after the watcher started, at an idle threshold of {1} s.", waited.TotalSeconds, IdleThresholdSeconds);
				}

				// Rolled back, not committed. The expression has never been compiled - the abandoned client created the
				// table and never read it - so no cached plan can answer it; TransactionReadsItsOwnStoreWrites makes the
				// same point at length.
				Exception unknown = Assert.Catch(() => instance.Evaluate("Count(AbandonedNativeT)"));
				Assert.That(FindError(unknown, (int)CompilerException.Codes.UnknownIdentifier), Is.Not.Null, "The abandoned transaction's table survived its rollback: " + unknown);

				// And the work done after the reclamation is real.
				Assert.That(instance.Evaluate("Count(AfterNativeReclaimT)"), Is.EqualTo(0));

				// A call on the reclaimed handle fails. No new error and no new error code for a reclaimed session: the
				// handle simply no longer names anything, which is the "Invalid session handle" ArgumentException the
				// dictionary lookup raises for any handle this server does not hold, wrapped as every native error is.
				Exception onAReclaimedHandle = Assert.Catch(() => nativeServer.GetTransactionCount(sessionHandle), "A call on the reclaimed session's handle succeeded.");
				TestContext.Progress.WriteLine("A call on the reclaimed session raised {0}: {1}", onAReclaimedHandle.GetType().Name, onAReclaimedHandle.Message);
			}
		}

		/// <summary>
		/// A session with a call in flight on it is left alone, however long that call runs and however old the threshold
		/// makes it look: the server counts the calls in flight on each session and the watcher reads that count under the
		/// same lock it would remove the session under. Without the count this case fails - the watcher stops the session's
		/// process underneath the running call.
		/// </summary>
		[Test]
		public void ASessionWithACallInFlightIsNotReclaimed()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(ASessionWithACallInFlightIsNotReclaimed)))
			{
				instance.Server.Start();

				NativeServer nativeServer = new NativeServer(instance.Server);
				NativeSessionHandle sessionHandle = nativeServer.StartSession(new NativeSessionInfo { HostName = "BusyNativeClient" });

				// Not stopped at the end of the case: the server this instance stops closes every session it holds, native
				// ones included, and a StopSession here would be racing the watcher for a session that has by then been idle
				// for longer than the threshold.
				using (NativeSessionManager sessionManager = new NativeSessionManager(nativeServer, IdleThresholdSeconds))
				{
					// Sleep is a built-in operator (src/engine/DAE/Schema/SystemCatalog.d4), so this needs no library. On a
					// task of its own because the case has to let the watcher wake several times while the call runs, and
					// nothing else may call the session in the meantime - a call of its own would stamp the clock and the
					// case would prove nothing.
					Task longCall = Task.Run(() => nativeServer.Execute(sessionHandle, String.Format("Sleep({0});", LongCallMilliseconds), null, NativeExecutionOptions.Default));

					bool completed;
					Exception callError = null;
					try
					{
						completed = longCall.Wait(LongCallWait);
					}
					catch (AggregateException exception)
					{
						completed = true;
						callError = exception.InnerException;
					}

					Assert.That(completed, Is.True, String.Format("The {0} ms call on the native session had not finished {1:0} s after it started.", LongCallMilliseconds, LongCallWait.TotalSeconds));
					Assert.That(callError, Is.Null, "The call in flight on the native session failed, which is what a watcher stopping the session underneath it looks like: " + callError);

					// And the session is still there afterwards, because the call's release stamped its clock on the way out.
					// Read at once: at a one-second threshold the session is a legitimate candidate again a second from here.
					Assert.DoesNotThrow(() => nativeServer.GetTransactionCount(sessionHandle), "The session was reclaimed while a call was in flight on it: its handle no longer names a session.");
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
			Exception exception = Assert.Catch(() => instance.ExecuteScript("create table LockedOutByANativeSessionT { ID : Integer, key { ID } };"));
			watch.Stop();
			TestContext.Progress.WriteLine("A catalog change was refused after {0} ms while the abandoned native session's transaction held the write lock.", watch.ElapsedMilliseconds);

			Assert.That
			(
				FindError(exception, (int)StoreException.Codes.StoreLocked),
				Is.Not.Null,
				"The abandoned native session's transaction did not hold the catalog store's write lock, so this case would prove nothing about releasing it: " + exception
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
						Assert.Fail(String.Format("The abandoned native session was not reclaimed: its transaction still held the catalog store's write lock {0:0.0} s after the idle watcher started, at an idle threshold of {1} s.", waited.Elapsed.TotalSeconds, IdleThresholdSeconds));
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
