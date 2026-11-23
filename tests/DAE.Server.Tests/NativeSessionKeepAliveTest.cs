/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.Threading;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What keeps a native CLI session from looking abandoned while a live client is holding it: the client's keep-alive
	/// ping. The server measures one clock per session and moves it on every call that names that session's handle, so a
	/// session nothing calls grows old - and a session a client holds across idle time stays young only because that
	/// client pings it from a thread of its own.
	/// </summary>
	/// <remarks>
	/// A hosted server on loopback, because the ping belongs to the client library and reaches the server over the wire;
	/// this interface has no in-process half to measure it at instead. The first case measures the clock; the two below it
	/// measure what the clock is for, against a server whose idle watcher is running at a threshold a case can wait for -
	/// a session the client pings survives it, and the same session without the ping does not. What that watcher does to
	/// an abandoned transaction, and what it leaves alone, is NativeSessionCleanupTest's, in process.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: this fixture hosts a server of its own, and two servers in one process share the engine's
	// static state (see the note on TestServerInstance).
	public class NativeSessionKeepAliveTest
	{
		/// <summary>The keep-alive interval this case runs the client at; the shipped default is two minutes.</summary>
		/// <remarks>
		/// Not one second, as ConnectionKeepAliveTest can use: idle time is reported in whole seconds, so at a one-second
		/// interval the clock would barely be seen to age and a ping arriving would prove nothing. Three seconds leaves a
		/// window, about a second wide, in which the clock reads <see cref="AgedSeconds"/> before the next ping resets it.
		/// </remarks>
		private const int KeepAliveIntervalSeconds = 3;

		/// <summary>The idle time the clock must be seen to reach, so that a later reading below it is a ping having landed.</summary>
		private const long AgedSeconds = 2;

		/// <summary>How long a wait on the clock gives up after: many intervals, and still seconds rather than minutes.</summary>
		private static readonly TimeSpan ClockWait = TimeSpan.FromSeconds(30);

		/// <summary>The idle threshold the hosted server runs at in the two reclamation cases; the shipped default is five minutes.</summary>
		/// <remarks>
		/// Three seconds, so that the one-second keep-alive below has to miss three pings running before a live session is
		/// reclaimed - a margin a loaded machine can be trusted with, where a one-second threshold could not be.
		/// </remarks>
		private const int IdleThresholdSeconds = 3;

		/// <summary>The keep-alive interval the two reclamation cases run the client at, when they run it with one at all.</summary>
		private const int FastKeepAliveIntervalSeconds = 1;

		/// <summary>How long those cases leave the session alone before calling it: several thresholds, so a sweep that was going to happen has.</summary>
		/// <remarks>
		/// Waited out rather than polled, which is the one thing this pair cannot do: every call a case could poll with
		/// would itself stamp the session's clock and keep the session alive.
		/// </remarks>
		private static readonly TimeSpan IdleTime = TimeSpan.FromSeconds(IdleThresholdSeconds * 3);

		/// <summary>
		/// A session the client holds and never calls is kept young anyway. That is what lets a server link hold a session
		/// on another Dataphor server through hours of a user session's idle time without it looking abandoned.
		/// </summary>
		[Test]
		public void TheSessionsClockMovesWhileTheClientHoldsIt()
		{
			using (HostedInstance instance = new HostedInstance(nameof(NativeSessionKeepAliveTest), false, 0))
			{
				using (NewSession(instance, KeepAliveIntervalSeconds))
				{
					// The case makes no call of its own on the session, so nothing but the keep-alive can move the clock -
					// and the clock is read where the idle watcher reads it, on the server, through the host.
					WaitForTheClock(instance, idleSeconds => idleSeconds >= AgedSeconds, String.Format("the session's clock to age to {0} s", AgedSeconds));
					WaitForTheClock(instance, idleSeconds => idleSeconds < AgedSeconds, "a ping to move the session's clock back");
				}
			}
		}

		/// <summary>
		/// A session the client holds and pings, against a server that is reclaiming uncalled sessions, is still there
		/// after several thresholds have passed with no call from the test at all. This is the case that says a server
		/// link survives an idle user session, and the compatibility claim the whole design rests on.
		/// </summary>
		[Test]
		public void ASessionTheClientKeepsAliveIsNotReclaimed()
		{
			using (HostedInstance instance = new HostedInstance(nameof(ASessionTheClientKeepsAliveIsNotReclaimed), false, 0, IdleThresholdSeconds))
			{
				using (NativeCLISession session = NewSession(instance, FastKeepAliveIntervalSeconds))
				{
					Thread.Sleep(IdleTime);

					// The first call the case makes since the session was started. Nothing but the keep-alive can have kept
					// the session alive across the wait above.
					Assert.DoesNotThrow(() => session.GetTransactionCount(), String.Format("A session the client pinged every {0} s was reclaimed by a server at a {1} s idle threshold.", FastKeepAliveIntervalSeconds, IdleThresholdSeconds));
				}
			}
		}

		/// <summary>
		/// The same session with the keep-alive turned off is reclaimed. Its only job is to keep the case above from
		/// passing for the wrong reason - a watcher that was not running, or a threshold the wait never reached.
		/// </summary>
		[Test]
		public void ASessionWithNoKeepAliveIsReclaimed()
		{
			using (HostedInstance instance = new HostedInstance(nameof(ASessionWithNoKeepAliveIsReclaimed), false, 0, IdleThresholdSeconds))
			{
				NativeCLISession session = NewSession(instance, 0);
				try
				{
					Thread.Sleep(IdleTime);

					Exception onAReclaimedSession = Assert.Catch(() => session.GetTransactionCount(), String.Format("A session with no keep-alive was still there {0:0} s after it was started, at a {1} s idle threshold.", IdleTime.TotalSeconds, IdleThresholdSeconds));
					TestContext.Progress.WriteLine("A call on the reclaimed session raised {0}: {1}", onAReclaimedSession.GetType().Name, onAReclaimedSession.Message);
				}
				finally
				{
					try
					{
						session.Dispose();
					}
					catch
					{
						// Not a using, because Dispose ends the session on the far server and propagates the failure when it
						// cannot - which is this case's whole point. The local half is released either way (see
						// NativeCLISession.Dispose), so there is nothing left to do about it here.
					}
				}
			}
		}

		/// <summary>A native client session on the given hosted instance, pinging every <paramref name="keepAliveIntervalSeconds"/> seconds; 0 for no keep-alive.</summary>
		private static NativeCLISession NewSession(HostedInstance instance, int keepAliveIntervalSeconds)
		{
			return
				new NativeCLISession
				(
					HostedInstance.LoopbackAddress,
					instance.InstanceName,
					instance.PortNumber,
					ConnectionSecurityMode.Default,
					0,
					ConnectionSecurityMode.Default,
					new NativeSessionInfo(),
					keepAliveIntervalSeconds
				);
		}

		/// <summary>Polls the server's clock for this case's one session until <paramref name="condition"/> holds of it, and fails if it never does.</summary>
		private static void WaitForTheClock(HostedInstance instance, Func<long, bool> condition, string what)
		{
			Stopwatch waited = Stopwatch.StartNew();
			long idleSeconds = IdleSecondsOfTheOneSession(instance);
			while (!condition(idleSeconds) && (waited.Elapsed < ClockWait))
			{
				Thread.Sleep(100);
				idleSeconds = IdleSecondsOfTheOneSession(instance);
			}

			TestContext.Progress.WriteLine("Waited {0:0.0} s for {1}; the session's idle time then read {2} s.", waited.Elapsed.TotalSeconds, what, idleSeconds);
			Assert.That
			(
				condition(idleSeconds),
				Is.True,
				String.Format("Gave up after {0:0.0} s waiting for {1}, with the client pinging every {2} s; the session's idle time read {3} s.", waited.Elapsed.TotalSeconds, what, KeepAliveIntervalSeconds, idleSeconds)
			);
		}

		/// <summary>How long the one native session this case opened has been without a call, as the server measures it.</summary>
		private static long IdleSecondsOfTheOneSession(HostedInstance instance)
		{
			long[] idleSeconds = instance.Host.NativeServer.GetSessionIdleSeconds();
			Assert.That(idleSeconds.Length, Is.EqualTo(1), "The server was not holding exactly the one native session this case opened.");
			return idleSeconds[0];
		}
	}
}
