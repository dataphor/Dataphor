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
	/// this interface has no in-process half to measure it at instead. Nothing reclaims a native session yet: that is
	/// bug-native-cli-sessions-never-reclaimed's subject, and the watcher it adds will read the same clock this case
	/// reads.
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

		/// <summary>
		/// A session the client holds and never calls is kept young anyway. That is what lets a server link hold a session
		/// on another Dataphor server through hours of a user session's idle time without it looking abandoned.
		/// </summary>
		[Test]
		public void TheSessionsClockMovesWhileTheClientHoldsIt()
		{
			using (HostedInstance instance = new HostedInstance(nameof(NativeSessionKeepAliveTest), false, 0))
			{
				using
				(
					new NativeCLISession
					(
						HostedInstance.LoopbackAddress,
						instance.InstanceName,
						instance.PortNumber,
						ConnectionSecurityMode.Default,
						0,
						ConnectionSecurityMode.Default,
						new NativeSessionInfo(),
						KeepAliveIntervalSeconds
					)
				)
				{
					// The case makes no call of its own on the session, so nothing but the keep-alive can move the clock -
					// and the clock is read where the idle watcher to come will read it, on the server, through the host.
					WaitForTheClock(instance, idleSeconds => idleSeconds >= AgedSeconds, String.Format("the session's clock to age to {0} s", AgedSeconds));
					WaitForTheClock(instance, idleSeconds => idleSeconds < AgedSeconds, "a ping to move the session's clock back");
				}
			}
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
