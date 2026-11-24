/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Session-level debugging: the debugger attached to a whole login rather than to one task at a time, so that every task
	/// that login is already running, and every task it starts afterwards, is debugged without being named one at a time.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the IDE's "attach to session" (<c>src/windows/Dataphoria/Debug/Debugger.cs</c>, AttachSession and
	/// DetachSession), and it is an NUnit fixture rather than a D4 coverage script because no D4 operator opens a second
	/// session and hands back its identifier: Debug.AttachSession needs the identifier of a session other than the
	/// debugger's own, ExecuteMultiple keeps the sessions it opens to itself, and ExecuteAsync and StartProcess both work
	/// inside the caller's session. The block commented out in DebugLibrary.d4 asked for exactly this fixture; only the
	/// harness can hold two sessions at once.
	/// </para>
	/// <para>
	/// The debugger is driven the way the IDE drives it, by executing D4 on the debugger session's own process, so that what
	/// is tested is the surface an application reaches and not a C# back door. What this fixture owns is which processes end
	/// up attached, and when: nothing here stops a program, because DebugBreakpoints.d4 covers getting one stopped and
	/// reading it while it is held, DebugStepping.d4 what a person does with a stopped program, and DebugLibrary.d4 the half
	/// that needs no stop at all.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class DebuggerSessionTest : InProcessTestFixture
	{
		/// <summary>How long a bounded poll waits for something that should happen within milliseconds.</summary>
		private const int WaitMilliseconds = 5000;
		private const int PollMilliseconds = 25;

		/// <summary>
		/// How long a case waits for the work it gave the debugged session to end. Generous, because it is not a measurement
		/// of anything: a loop that has not ended by here is one that never will, and the wait is what turns that into a
		/// named failure rather than a teardown that spends half of ProcessTerminationTimeout trying to stop the process.
		/// </summary>
		private const int WorkWaitMilliseconds = 30000;

		/// <summary>
		/// The work a case gives the debugged session so that it has a task genuinely executing when the debugger attaches
		/// it. One statement, so one call, and a million iterations: the same loop DebugStepping.d4 pauses, measured at about
		/// a second here on one slow development laptop in Debug, with the debugger attached for the second half of it, where
		/// every breakable node takes the debugger's lock. Integer arithmetic and no table access, so a task the debugger
		/// holds cannot hold a lock the asserting session then waits for.
		/// </summary>
		// NOTE: the margin between that second and the window it has to outlast - one Debug.AttachSession and one read of the
		// Processes table, which is tens of milliseconds, since the three cases below each do several such executions and
		// reads and run in 5 to 65 ms in all - is this fixture's one timing assumption. If a faster machine or a Release
		// build ever lets the loop finish inside that window, AttachingASessionAttachesTheTaskItIsAlreadyRunning fails on its
		// own assertion, reporting the task as no longer running, and the fix is to raise the iteration count here.
		private const string RunningWork = "begin var LIndex := 0; while LIndex < 1000000 do LIndex := LIndex + 1; end;";

		/// <summary>The login the debugger attaches, which is not the fixture's own: a debugger refuses its own session.</summary>
		private IServerSession _debugged;

		/// <summary>The processes started on that login, which this fixture owns from the moment it starts one.</summary>
		private readonly List<IServerProcess> _debuggedProcesses = new List<IServerProcess>();

		/// <summary>The task running <see cref="RunningWork"/>, for the one case that needs a task in flight.</summary>
		private Task _work;

		/// <summary>
		/// The second login, opened after the base fixture's own (NUnit runs the base SetUp first) and closed before it.
		/// </summary>
		/// <remarks>
		/// Its SessionInfo names a catalog cache, because the engine refuses to attach a session that has none:
		/// ServerSession.SetDebuggedByID raises CannotAttachToAnInProcessSession when SessionInfo.CatalogCacheName is empty,
		/// and a session opened straight off the engine - which is what InProcessTestFixture's own session and
		/// TestServerInstance.Connect give - has it empty. Every client session carries one, because LocalServer.Connect
		/// sets it to the client's internal server name for every connection it makes, so naming one here is what makes this
		/// session stand in for the client session the IDE debugs rather than a way around the check.
		/// </remarks>
		[SetUp]
		public void OpenTheDebuggedSession()
		{
			SessionInfo sessionInfo = new SessionInfo("Admin", "");
			sessionInfo.CatalogCacheName = GetType().Name;
			_debugged = Server.Connect(sessionInfo);
		}

		/// <summary>
		/// Stops the debugger before the debugged session goes, whatever the case did.
		/// </summary>
		/// <remarks>
		/// Either order is safe - the engine detaches a debugged session from its debugger as the session is disposed, which
		/// is what <see cref="DisconnectingADebuggedSessionDetachesItFromTheDebugger"/> asserts - so this one is simply the
		/// reverse of the setup, keeping the debugger's lifetime inside the debugged session's and leaving each case to be
		/// read without it. The base fixture's TearDown, which stops its own process and disconnects its own session, runs
		/// after this one.
		/// </remarks>
		[TearDown]
		public void StopTheDebuggerAndCloseTheDebuggedSession()
		{
			try
			{
				ExecuteScript("if exists (Debug.GetDebuggers() where Session_ID = SessionID()) then Debug.Stop();");
			}
			finally
			{
				CloseTheDebuggedSession();
			}
		}

		/// <summary>
		/// Attaching a session attaches the task it is already running: Debugger.AttachSession walks the session's processes,
		/// which is what makes attaching a login retrospective rather than only forward-looking.
		/// </summary>
		[Test]
		public void AttachingASessionAttachesTheTaskItIsAlreadyRunning()
		{
			IServerProcess running = StartProcessOnTheDebuggedSession();
			ExecuteScript("Debug.Start();");

			_work = Task.Run(() => running.ExecuteScript(RunningWork));
			WaitUntil
			(
				"the task the debugged session was given to start running",
				() => ProcessIsRunning(running.ProcessID)
			);

			AttachTheDebuggedSession();

			// First, so that the case cannot pass on a loop that had already ended: what is asserted below is that a running
			// task was attached, and only this says the task was still running when it was attached.
			Assert.That
			(
				ProcessIsRunning(running.ProcessID),
				Is.True,
				"The task the debugged session was given ended before the debugger attached its session, so this case no longer tests a task that was already running. Raise the iteration count in RunningWork."
			);
			Assert.That
			(
				DebuggerHoldsSession(_debugged.SessionID),
				Is.True,
				"Debug.GetSessions does not list the session that was just attached."
			);
			Assert.That
			(
				DebuggerHoldsProcess(running.ProcessID),
				Is.True,
				"Debug.GetProcesses does not list the task the attached session was already running, so attaching the session did not attach it."
			);

			WaitForTheWorkToEnd();
		}

		/// <summary>
		/// A task started after the attach is attached too, without the debugger naming it: the ServerProcess constructor
		/// reads its session's DebuggedByID and attaches itself through Engine.GetDebugger. This is the whole point of
		/// attaching a login rather than a task, and the coverage run that produced docs/coverage-report.md reached none of
		/// those three lines.
		/// </summary>
		[Test]
		public void ATaskStartedAfterTheAttachIsAttachedToo()
		{
			ExecuteScript("Debug.Start();");
			AttachTheDebuggedSession();

			IServerProcess started = StartProcessOnTheDebuggedSession();

			Assert.That
			(
				DebuggerHoldsProcess(started.ProcessID),
				Is.True,
				"Debug.GetProcesses does not list a task started on the attached session after the attach, so a new task on a debugged session does not attach itself."
			);
		}

		/// <summary>
		/// Detaching a session detaches the tasks it is running and stops its new tasks from attaching:
		/// Debugger.DetachSession detaches the processes it holds and sets the session's DebuggedByID back to zero.
		/// </summary>
		[Test]
		public void DetachingASessionDetachesItsTasks()
		{
			IServerProcess before = StartProcessOnTheDebuggedSession();
			ExecuteScript("Debug.Start();");
			AttachTheDebuggedSession();
			IServerProcess after = StartProcessOnTheDebuggedSession();

			Assert.That
			(
				DebuggerHoldsProcess(before.ProcessID) && DebuggerHoldsProcess(after.ProcessID),
				Is.True,
				"The attached session's two tasks are not both attached, so this case would not be detaching anything."
			);

			ExecuteScript(String.Format("Debug.DetachSession({0});", _debugged.SessionID));

			Assert.That
			(
				DebuggedSessionCount(),
				Is.EqualTo(0),
				"Debug.GetSessions still lists the detached session."
			);
			Assert.That
			(
				DebuggerHoldsProcess(before.ProcessID),
				Is.False,
				"Debug.GetProcesses still lists the task the detached session was running when it was attached."
			);
			Assert.That
			(
				DebuggerHoldsProcess(after.ProcessID),
				Is.False,
				"Debug.GetProcesses still lists the task that attached itself to the detached session."
			);

			// The session's DebuggedByID being back to zero, observed where it is observable: a task started afterwards finds
			// nothing to attach itself to. Reading the field would assert the same thing one layer below the operators.
			IServerProcess afterTheDetach = StartProcessOnTheDebuggedSession();
			Assert.That
			(
				DebuggerHoldsProcess(afterTheDetach.ProcessID),
				Is.False,
				"A task started on the detached session attached itself, so detaching the session left it marked as debugged."
			);
		}

		/// <summary>
		/// Stopping the debugger detaches the session it was attached to: Debugger.Dispose detaches every session it holds.
		/// </summary>
		/// <remarks>
		/// The failure this guards against is a session left marked as debugged by a debugger that no longer exists, which
		/// would make every task started on it afterwards fail to attach - Engine.GetDebugger raises DebuggerNotStarted for
		/// the stopped debugger's session - and make the session itself hard to get rid of.
		/// </remarks>
		[Test]
		public void StoppingTheDebuggerDetachesTheSession()
		{
			ExecuteScript("Debug.Start();");
			AttachTheDebuggedSession();
			IServerProcess attached = StartProcessOnTheDebuggedSession();

			Assert.That
			(
				DebuggerHoldsProcess(attached.ProcessID),
				Is.True,
				"The attached session's task is not attached, so this case would not be stopping a debugger that holds anything."
			);

			ExecuteScript("Debug.Stop();");

			Assert.That
			(
				(bool)Evaluate("exists (Debug.GetDebuggers() where Session_ID = SessionID())"),
				Is.False,
				"Debug.Stop did not stop the debugger."
			);
			// Debug.GetSessions answers rather than raising when there is no debugger: DebugGetSessionsNode checks the
			// session's debugger for null before asking it, where most of the other operators go through CheckedDebugger and
			// raise DebuggerNotStarted.
			Assert.That
			(
				DebuggedSessionCount(),
				Is.EqualTo(0),
				"Debug.GetSessions still lists a session after the debugger that held it was stopped."
			);

			// Each of these is an assertion in its own right: a task started on a session still marked as debugged raises
			// DebuggerNotStarted from its own constructor, and stopping and disconnecting a session in that state is what a
			// person meets as a login that cannot be got rid of.
			Assert.DoesNotThrow
			(
				() => StartProcessOnTheDebuggedSession(),
				"Starting a task on the session after the debugger was stopped failed, so stopping the debugger left the session marked as debugged."
			);
			Assert.DoesNotThrow
			(
				() => CloseTheDebuggedSession(),
				"Stopping the session's tasks and disconnecting it after the debugger was stopped failed."
			);
		}

		/// <summary>
		/// Disconnecting a debugged session detaches it, and the tasks it was running, from the debugger that holds it: the
		/// other way round from the case above, and the one direction no code in Debugger.cs performs.
		/// </summary>
		/// <remarks>
		/// Nothing in Debugger.cs does this: the detach comes from ServerChildObjects, whose collections subscribe to each
		/// child's Disposed event and disown it when it fires, so the debugger's session and process lists empty themselves
		/// as the session and its processes go. DebugLibrary.d4 asserts the process half of that ("Disposed process did not
		/// detach"); this is the session half, which needs a second session and so can only be asserted here. What it rules
		/// out is a debugger left holding a disposed session, which would raise from Debugger.DetachSession the next time
		/// that debugger was stopped: a disposed session has given up both its process list and its SessionInfo, and
		/// DetachSession locks the one and SetDebuggedByID reads the other.
		/// </remarks>
		[Test]
		public void DisconnectingADebuggedSessionDetachesItFromTheDebugger()
		{
			ExecuteScript("Debug.Start();");
			AttachTheDebuggedSession();
			IServerProcess attached = StartProcessOnTheDebuggedSession();

			Assert.That
			(
				DebuggerHoldsProcess(attached.ProcessID),
				Is.True,
				"The attached session's task is not attached, so this case would not be disconnecting a session the debugger holds anything of."
			);

			CloseTheDebuggedSession();

			Assert.That
			(
				DebuggedSessionCount(),
				Is.EqualTo(0),
				"Debug.GetSessions still lists the session after it was disconnected."
			);
			Assert.That
			(
				DebuggerHoldsProcess(attached.ProcessID),
				Is.False,
				"Debug.GetProcesses still lists a task of the session after it was disconnected."
			);
			// Where a stale session would show: DetachSession reads the session's process list, which a disposed session no
			// longer has.
			Assert.DoesNotThrow
			(
				() => ExecuteScript("Debug.Stop();"),
				"Stopping the debugger after the session it held was disconnected failed, so disconnecting the session left the debugger holding it."
			);
		}

		/// <summary>
		/// A debugger refuses the session it is running on: ServerSession.SetDebuggedByID raises
		/// CannotAttachToDebuggerSession, since a pause parks every attached process that is running and would therefore
		/// park the one process that has to resume them.
		/// </summary>
		/// <remarks>
		/// DebugLibrary.d4 has a block that looks like this one, but under the coverage harness its Debug.AttachSession
		/// trips the earlier guard instead - CannotAttachToAnInProcessSession, since a session opened straight off the engine
		/// has no catalog cache name - and it asserts only that *some* error was raised, so it passes either way. Reaching
		/// this refusal needs a session that has a catalog cache name and a debugger of its own, which is the debugged
		/// session here with a debugger started on it; hence the error code rather than the type is what is asserted.
		/// </remarks>
		[Test]
		public void ADebuggerRefusesToAttachTheSessionItRunsOn()
		{
			IServerProcess itsOwnDebugger = StartProcessOnTheDebuggedSession();
			itsOwnDebugger.ExecuteScript("Debug.Start();");

			Exception refusal =
				Assert.Catch(() => itsOwnDebugger.ExecuteScript(String.Format("Debug.AttachSession({0});", _debugged.SessionID)));

			Assert.That
			(
				RefusalCode(refusal),
				Is.EqualTo((int)ServerException.Codes.CannotAttachToDebuggerSession),
				String.Format("Attaching the debugger's own session was refused for the wrong reason: {0}", refusal.Message)
			);
		}

		/// <summary>
		/// The engine error code of a refusal, which arrives wrapped in a RuntimeException naming the statement that raised
		/// it, so that a case can say which refusal it expected rather than only that something was raised.
		/// </summary>
		private static int RefusalCode(Exception exception)
		{
			int code = 0;
			for (Exception wrapped = exception; wrapped != null; wrapped = wrapped.InnerException)
				if (wrapped is DataphorException dataphorException)
					code = dataphorException.Code;
			return code;
		}

		/// <summary>Attaches the debugged session the way the IDE does, by executing D4 on the debugger session's process.</summary>
		private void AttachTheDebuggedSession()
		{
			ExecuteScript(String.Format("Debug.AttachSession({0});", _debugged.SessionID));
		}

		/// <summary>Starts a task on the debugged session, recorded so that this fixture stops it however the case ends.</summary>
		private IServerProcess StartProcessOnTheDebuggedSession()
		{
			IServerProcess process = _debugged.StartProcess(new ProcessInfo(_debugged.SessionInfo));
			_debuggedProcesses.Add(process);
			return process;
		}

		/// <summary>
		/// Stops the debugged session's tasks and disconnects it. Idempotent, because the case that asserts this can be done
		/// at all does it itself and the teardown then has nothing left to do.
		/// </summary>
		private void CloseTheDebuggedSession()
		{
			try
			{
				try
				{
					// Before the processes go: stopping one aborts the loop it is running, which faults the task, and an
					// abort is not the failure this fixture is about.
					WaitForTheWorkToEnd(false);
				}
				finally
				{
					while (_debuggedProcesses.Count > 0)
					{
						IServerProcess process = _debuggedProcesses[_debuggedProcesses.Count - 1];
						_debuggedProcesses.RemoveAt(_debuggedProcesses.Count - 1);
						_debugged.StopProcess(process);
					}
				}
			}
			finally
			{
				if (_debugged != null)
				{
					IServerSession debugged = _debugged;
					_debugged = null;
					Server.Disconnect(debugged);
				}
			}
		}

		private void WaitForTheWorkToEnd()
		{
			WaitForTheWorkToEnd(true);
		}

		/// <summary>
		/// Waits for the task running <see cref="RunningWork"/> to end, if a case started one.
		/// </summary>
		/// <param name="assert">
		/// True where the case is asserting that the loop ended on its own; false in the teardown, where the case may
		/// already have failed and a loop still running is about to be aborted by the process being stopped - so what it did
		/// is written to the test output rather than asserted, where it cannot hide the case's own failure.
		/// </param>
		private void WaitForTheWorkToEnd(bool assert)
		{
			if (_work == null)
				return;

			Task work = _work;
			_work = null;

			// WaitAny rather than Wait: it reports the timeout as a value and does not throw what the task threw, so both
			// outcomes can be reported in the words of this fixture.
			bool ended = Task.WaitAny(new Task[] { work }, assert ? WorkWaitMilliseconds : PollMilliseconds) >= 0;
			string failure =
				!ended
					? "The loop the debugged session was given never ended."
					:
					(
						work.IsFaulted
							? String.Format("The loop the debugged session was given failed: {0}", work.Exception.GetBaseException().Message)
							: null
					);

			if (failure == null)
				return;

			if (assert)
				Assert.Fail(failure);
			else
				TestContext.Progress.WriteLine(failure);
		}

		/// <summary>
		/// Polls until the condition holds, and fails naming what it was waiting for if it never does, so that a case which
		/// never gets there reads as an assertion rather than as a test that hung.
		/// </summary>
		private static void WaitUntil(string what, Func<bool> condition)
		{
			Stopwatch waited = Stopwatch.StartNew();
			while (true)
			{
				if (condition())
					return;

				if (waited.ElapsedMilliseconds >= WaitMilliseconds)
					Assert.Fail(String.Format("Waited {0:0.0} s for {1}, which never happened.", waited.Elapsed.TotalSeconds, what));

				Thread.Sleep(PollMilliseconds);
			}
		}

		private bool DebuggerHoldsSession(int sessionID)
		{
			return (bool)Evaluate(String.Format("exists (Debug.GetSessions() where Session_ID = {0})", sessionID));
		}

		private bool DebuggerHoldsProcess(int processID)
		{
			return (bool)Evaluate(String.Format("exists (Debug.GetProcesses() where Process_ID = {0})", processID));
		}

		private int DebuggedSessionCount()
		{
			return (int)Evaluate("Count(Debug.GetSessions())");
		}

		/// <summary>Whether the engine reports the process as executing something, read from the Processes catalog table.</summary>
		private bool ProcessIsRunning(int processID)
		{
			return (bool)Evaluate(String.Format("exists (Processes where ID = {0} and IsRunning)", processID));
		}

		/// <summary>Evaluates on the debugger session's process, which is the fixture's own.</summary>
		private object Evaluate(string expression)
		{
			return ((ServerProcess)Process).Evaluate(expression, null).AsNative;
		}
	}
}
