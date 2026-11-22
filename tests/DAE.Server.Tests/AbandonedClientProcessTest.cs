/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.Threading;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What the server does about a client program that is killed outright while it holds a session in a transaction: it
	/// takes the connection back, and it goes on serving everyone else.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The one case of the test plan's layer 4 that needs two child processes - a server, and a client that was alive and
	/// pinging and then stopped existing. That is a different state from a connection that was never pinged, which
	/// <see cref="AbandonedSessionCleanupTest"/> covers in process, at the lowest layer that shows it: a client that
	/// never pinged and a client whose pings stop look the same to the server's idle watcher, but only the second is what
	/// happens in the field, and only the second shows that the pings really do stop when the process dies.
	/// </para>
	/// <para>
	/// Deliberately not repeated here, because <see cref="AbandonedSessionCleanupTest"/> asserts all of it and a second
	/// copy would be two tests for one behaviour: that the abandoned transaction is rolled back rather than committed,
	/// that the catalog store's write lock it held is released, and what a call on the reclaimed process sees. What this
	/// case asserts is the end state a second process is needed for - the killed client's session is gone and the server
	/// still answers - and not the mechanism that produced it, because there were two candidates: the server's idle
	/// watcher noticing that the pings stopped, and CoreWCF or Kestrel noticing the socket the dead process left behind.
	/// Measured while this was written, the watcher is the one that acts and the channel's death does nothing; the plan's
	/// <em>Reclaiming an abandoned session</em> section records the measurement.
	/// </para>
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: the child processes share no engine state with this one, but the clients this case builds do
	// - each LocalServer starts a client-side engine here, and those share the object-id counter (see ProtocolClient).
	// Nothing in this project is parallelizable.
	public class AbandonedClientProcessTest
	{
		private const string InstanceName = "AbandonedClientProcess";

		/// <summary>The idle threshold the child server runs at; the shipped default is five minutes.</summary>
		/// <remarks>
		/// <para>
		/// The watcher wakes every quarter of this with a floor of a second, so a connection whose pings have stopped is
		/// relinquished between five and six and a quarter seconds after its last one. This case measures from the kill
		/// rather than from the last ping, and the last ping was up to <see cref="KeepAliveIntervalSeconds"/> before it, so
		/// what it sees is four to six and a quarter seconds: a measurement under the threshold is the expected thing.
		/// </para>
		/// <para>
		/// It has to exceed <see cref="KeepAliveIntervalSeconds"/> by enough to survive a ping that is late or lost - the
		/// rule <c>LocalServer</c>'s own keep-alive strategy comment states - because the observing client this case
		/// watches the server through is a client of the same server and is measured by the same clock. Five seconds
		/// against a one-second ping leaves four pings' worth of slack; this suite's part 1 runs the case on a machine
		/// that is starting servers in other fixtures at the same time, and two seconds of case time is the cheaper side
		/// of that trade.
		/// </para>
		/// </remarks>
		private const int IdleThresholdSeconds = 5;

		/// <summary>How often both clients here ping; the shipped default is two minutes, which no case can wait for.</summary>
		private const int KeepAliveIntervalSeconds = 1;

		/// <summary>How long the case waits for the reclamation; generous, so that a loaded machine does not fail it.</summary>
		private static readonly TimeSpan ReclamationWait = TimeSpan.FromSeconds(30);

		private const int PollMilliseconds = 100;

		/// <summary>The table the killed client creates inside the transaction it never commits.</summary>
		private const string HeldTableName = "HeldByAKilledClient";

		/// <summary>
		/// A client program connects, opens a transaction that changes the catalog, and is then killed outright with its
		/// keep-alive thread still pinging - a crashed process, a switched-off workstation. The server notices, takes the
		/// connection and its session back, and still answers both the client that was watching and a new one.
		/// </summary>
		[Test]
		public void AKilledClientProcessIsReclaimedAndTheServerKeepsServing()
		{
			// A child server of its own rather than the shared out-of-process one: its idle threshold is not the threshold
			// the other cases want, and a short one would have their clients reclaimed mid-case.
			using (OutOfProcessInstance instance = new OutOfProcessInstance(InstanceName, IdleThresholdSeconds))
			using (ProtocolClient observer = NewClient(instance))
			{
				observer.OnProcess
				(
					observing =>
					{
						using (OutOfProcessInstance.HeldTransactionClient client = instance.StartAClientHoldingATransaction(HeldTableName, KeepAliveIntervalSeconds))
						{
							// The client reported which session it holds; this is the observer agreeing, from the other
							// side of the wire, that the session is really there before anything is killed.
							Assert.That
							(
								SessionCount(observing, client.SessionID),
								Is.EqualTo(1),
								client.Diagnostic(String.Format("The child server does not have the session ({0}) the client process says it holds, so there would be nothing for this case to watch being reclaimed.", client.SessionID))
							);

							Stopwatch sinceTheKill = Stopwatch.StartNew();
							client.Kill();

							// Without this the case would also pass for a client that had died on its own: that too
							// stops the pings, so the session goes the same way and nothing below could tell them apart.
							Assert.That
							(
								client.HadAlreadyExited,
								Is.False,
								client.Diagnostic("The client process had already gone by the time this case killed it, so what follows would not be about a client killed while it held its session.")
							);

							TimeSpan waited = WaitForTheSessionToGo(observing, client, sinceTheKill);

							TestContext.Progress.WriteLine
							(
								"The killed client's session ({0}) was gone {1:0.0} s after the kill, at an idle threshold of {2} s and a keep-alive interval of {3} s. The client exited with code {4}.",
								client.SessionID,
								waited.TotalSeconds,
								IdleThresholdSeconds,
								KeepAliveIntervalSeconds,
								client.ExitCode
							);
						}

						// The client that was watching all of this is still usable, which is the half of "still serving"
						// that a connection established before the reclamation can show.
						Assert.That((int)observing.Evaluate("1 + 1", null).AsNative, Is.EqualTo(2), instance.Diagnostic("The observing client cannot reach the child server after the killed client was reclaimed."));
					}
				);

				// And the other half: a connection opened after it, which exercises the server's connect path rather than a
				// handle it minted earlier.
				using (ProtocolClient afterwards = NewClient(instance))
					afterwards.OnProcess
					(
						process => Assert.That((int)process.Evaluate("Count(Sessions)", null).AsNative, Is.GreaterThan(0), instance.Diagnostic("A connection opened after the reclamation could not read the child server's session count."))
					);
			}
		}

		/// <summary>A client of the child server that pings fast enough not to be reclaimed at this case's threshold.</summary>
		private static ProtocolClient NewClient(OutOfProcessInstance instance)
		{
			return new ProtocolClient(instance.HostName, instance.InstanceName, instance.PortNumber, KeepAliveIntervalSeconds);
		}

		/// <summary>
		/// Polls the observing client until the killed client's session is gone, and returns how long that took from the
		/// kill.
		/// </summary>
		private static TimeSpan WaitForTheSessionToGo(IServerProcess observing, OutOfProcessInstance.HeldTransactionClient client, Stopwatch sinceTheKill)
		{
			while (sinceTheKill.Elapsed < ReclamationWait)
			{
				if (SessionCount(observing, client.SessionID) == 0)
					return sinceTheKill.Elapsed;

				Thread.Sleep(PollMilliseconds);
			}

			Assert.Fail
			(
				client.Diagnostic
				(
					String.Format
					(
						"The killed client's session ({0}) was still on the child server {1:0.0} s after the client process was killed. The server's idle threshold is {2} s and the client pinged every {3} s, so the connection should have been relinquished within about {4} s of the kill.",
						client.SessionID,
						sinceTheKill.Elapsed.TotalSeconds,
						IdleThresholdSeconds,
						KeepAliveIntervalSeconds,
						IdleThresholdSeconds + (IdleThresholdSeconds / 4)
					)
				)
			);
			return sinceTheKill.Elapsed; // Assert.Fail throws; the compiler needs this.
		}

		/// <summary>How many sessions the server has with this identifier - one or none - asked over the wire as a scalar.</summary>
		/// <remarks>
		/// A count rather than a read of <c>System.Sessions</c> itself. Reading a table over the wire makes the client
		/// build a view from a catalog fragment the server sends, and a handful of those followed by a change to the
		/// server's catalog - which this case produces by design - trips a defect in that path, owned by
		/// bug-client-catalog-rebuild-reuses-object-ids. A count sends no catalog fragment and has nothing to do with it.
		/// </remarks>
		private static int SessionCount(IServerProcess observing, int sessionID)
		{
			try
			{
				return (int)observing.Evaluate(String.Format("Count(Sessions where ID = {0})", sessionID), null).AsNative;
			}
			catch (Exception exception)
			{
				// The observer is a client of the same server, with the same clock running against it, so the one way this
				// read fails is the one the case has to keep clear of.
				Assert.Fail
				(
					String.Format
					(
						"The observing client could not count the child server's sessions. If it was itself reclaimed, the margin is too thin on this machine: the server's idle threshold ({0} s) has to exceed the observer's keep-alive interval ({1} s) by enough to survive a ping that is late or lost. The error was: {2}",
						IdleThresholdSeconds,
						KeepAliveIntervalSeconds,
						exception
					)
				);
				throw; // Assert.Fail throws; the compiler needs this.
			}
		}
	}
}
