/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What a process's connection to another Dataphor server does when the server link naming that server is dropped,
	/// re-created or altered, and when that server itself goes away: a link is followed by its identity rather than by its
	/// name, a drop or an alter ends the issuing process's connection to it, and a connection whose server has gone is let
	/// go by the next statement that uses the link, unless it is a participant in a transaction the process still has open.
	/// </summary>
	/// <remarks>
	/// The other server is a separate program rather than a second server in this process, for the reason given on
	/// <see cref="OutOfProcessInstance"/>: two engines in one process share the engine's static state, and this fixture's
	/// own server is one of them. Each link names the child by host and port, so no listener is involved.
	/// </remarks>
	[TestFixture]
	public class ServerLinkConnectionTest : InProcessTestFixture
	{
		/// <summary>The instance the child server serves; a link's InstanceName tag has to name it, because the connection URI carries it.</summary>
		private const string TargetInstanceName = "ServerLinkTarget";

		/// <summary>The instance served by the child a case starts and stops itself, so that the case can watch what happens to a link whose server has gone.</summary>
		/// <remarks>Reused by every such case: each gets a child of its own, with its own temporary root and its own reserved port, and the cases of a fixture run one at a time.</remarks>
		private const string DoomedTargetInstanceName = "ServerLinkDoomedTarget";

		/// <summary>The call depth a remote session gets from a link with no DefaultMaxCallDepth tag: this session's own default, which is Engine.DefaultMaxCallDepth.</summary>
		private const int DefaultCallDepth = 100;

		/// <summary>What a link's DefaultMaxCallDepth tag asks for here; any value but DefaultCallDepth would do.</summary>
		private const int TaggedCallDepth = 128;

		private OutOfProcessInstance _linkedServer;

		[OneTimeSetUp]
		public void StartLinkedServer()
		{
			_linkedServer = new OutOfProcessInstance(TargetInstanceName);
		}

		[OneTimeTearDown]
		public void StopLinkedServer()
		{
			if (_linkedServer != null)
			{
				_linkedServer.Dispose();
				_linkedServer = null;
			}
		}

		/// <summary>A link created after a drop, under the name the dropped link used, is a different link and has to be connected to afresh.</summary>
		/// <remarks>
		/// Dropped and re-created from a second process, so that this process's connection is one the drop deliberately
		/// leaves open (see TestDropClosesTheDroppingProcessConnection for the one it closes). The only thing that can then
		/// make the new link's settings take effect is matching the held connection against the link itself rather than
		/// against its name.
		/// </remarks>
		[Test]
		public void TestLinkRecreatedUnderTheSameNameConnectsAfresh()
		{
			ExecuteScript(CreateLink("RecreatedLink", null));
			Assert.That(RemoteCallDepth("RecreatedLink"), Is.EqualTo(DefaultCallDepth));

			IServerProcess other = Session.StartProcess(new ProcessInfo(Session.SessionInfo));
			try
			{
				other.ExecuteScript("drop server RecreatedLink;");
				other.ExecuteScript(CreateLink("RecreatedLink", TaggedCallDepth));
			}
			finally
			{
				Session.StopProcess(other);
			}

			Assert.That(RemoteCallDepth("RecreatedLink"), Is.EqualTo(TaggedCallDepth));
		}

		/// <summary>A drop ends the dropping process's connection to the link, so it is not left holding a session on the other server.</summary>
		[Test]
		public void TestDropClosesTheDroppingProcessConnection()
		{
			ExecuteScript(CreateLink("DroppedLink", null));
			Assert.That(RemoteCallDepth("DroppedLink"), Is.EqualTo(DefaultCallDepth));
			Assert.That(ConnectionCount(), Is.EqualTo(1));

			ExecuteScript("drop server DroppedLink;");

			Assert.That(ConnectionCount(), Is.EqualTo(0));
		}

		/// <summary>A drop inside an open transaction waits for the outermost commit: the connection may be a participant in that transaction.</summary>
		[Test]
		public void TestDropInTransactionDefersTheCloseToTheCommit()
		{
			ExecuteScript(CreateLink("DeferredLink", null));
			Assert.That(RemoteCallDepth("DeferredLink"), Is.EqualTo(DefaultCallDepth));

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("drop server DeferredLink;");
			Assert.That(ConnectionCount(), Is.EqualTo(1));

			Process.CommitTransaction();
			Assert.That(ConnectionCount(), Is.EqualTo(0));
		}

		/// <summary>A rollback undoes the drop, so the close it was waiting for is discarded and the link and its connection are both still there.</summary>
		[Test]
		public void TestRolledBackDropKeepsTheConnection()
		{
			ExecuteScript(CreateLink("RolledBackLink", null));
			Assert.That(RemoteCallDepth("RolledBackLink"), Is.EqualTo(DefaultCallDepth));

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("drop server RolledBackLink;");
			Process.RollbackTransaction();

			Assert.That(ConnectionCount(), Is.EqualTo(1));
			Assert.That(RemoteCallDepth("RolledBackLink"), Is.EqualTo(DefaultCallDepth));
		}

		/// <summary>An alter ends the altering process's connection as well, so its next use of the link connects with the new settings; an open connection never re-reads them.</summary>
		[Test]
		public void TestAlterClosesTheAlteringProcessConnection()
		{
			ExecuteScript(CreateLink("AlteredLink", null));
			Assert.That(RemoteCallDepth("AlteredLink"), Is.EqualTo(DefaultCallDepth));

			ExecuteScript(String.Format("alter server AlteredLink alter tags {{ create DefaultMaxCallDepth = \"{0}\" }};", TaggedCallDepth));

			Assert.That(RemoteCallDepth("AlteredLink"), Is.EqualTo(TaggedCallDepth));
		}

		/// <summary>Changing which credentials a link connects with ends the issuing process's connection too, so its next use of the link connects with the new ones.</summary>
		/// <remarks>
		/// One case for all six link user operators: each of them, like an alter, reaches the link through
		/// CatalogDeviceSession.UpdateCatalogObject, which is where the close now lives, so a second operator would exercise the same
		/// code. The credentials this gives the link are ones the other server refuses, which is how the case can tell a fresh
		/// connection from a kept one. Only that it throws is asserted: a refused connection arrives wrapped, and the message is the
		/// reflection invocation's rather than the far server's.
		/// </remarks>
		[Test]
		public void TestLinkUserChangeClosesTheChangingProcessConnection()
		{
			ExecuteScript(CreateLink("RecredentialedLink", null));
			Assert.That(RemoteCallDepth("RecredentialedLink"), Is.EqualTo(DefaultCallDepth));
			Assert.That(ConnectionCount(), Is.EqualTo(1));

			ExecuteScript("CreateServerLinkUser('Admin', Name('RecredentialedLink'), 'Admin', 'wrong');");

			Assert.That(ConnectionCount(), Is.EqualTo(0));
			Assert.That(() => { RemoteCallDepth("RecredentialedLink"); }, Throws.Exception);
		}

		/// <summary>A process whose connection to another server has died must still be able to stop.</summary>
		/// <remarks>
		/// Ending the far session is a call over the wire, and a failed one used to leave the session handle set, the WCF
		/// client undisposed and the entry in the process's list, throwing out of the process teardown that was letting the
		/// connection go. A server of its own, stopped inside the case: the other cases need the shared one still running.
		/// </remarks>
		[Test]
		public void TestStoppingAProcessWhoseOtherServerHasGoneDoesNotThrow()
		{
			using (OutOfProcessInstance doomedServer = new OutOfProcessInstance(DoomedTargetInstanceName))
			{
				ExecuteScript(CreateLink("DoomedLink", doomedServer, null));

				// A process of its own, so that the connection this case kills is not the one the fixture's own teardown stops.
				IServerProcess holder = Session.StartProcess(new ProcessInfo(Session.SessionInfo));
				Assert.That(RemoteCallDepth(holder, "DoomedLink"), Is.EqualTo(DefaultCallDepth));

				doomedServer.Stop();

				Assert.DoesNotThrow(() => Session.StopProcess(holder), doomedServer.Diagnostic("Stopping a process failed because the server its link named was down."));
			}
		}

		/// <summary>A connection whose server has gone stops failing the process's statements: a statement that does not use the link never speaks to it, and the next statement that does use the link lets it go.</summary>
		/// <remarks>
		/// Every transaction a process began used to begin one on every connection to another server it held, whether the
		/// statement used that connection or not, so a dead connection failed every statement the process issued - including
		/// <c>drop server</c>, the one statement that would have cleared it. Since lazy enlistment such a statement never reaches
		/// the connection at all, so it is also not what releases it: the connection is kept until a statement uses the link, and
		/// that use lets it go and then fails on the reconnect while the far server is still down.
		/// </remarks>
		[Test]
		public void TestADeadConnectionStopsFailingStatementsAndIsLetGoAtTheNextUse()
		{
			using (OutOfProcessInstance doomedServer = new OutOfProcessInstance(DoomedTargetInstanceName))
			{
				ExecuteScript(CreateLink("ReclaimedLink", doomedServer, null));

				// A process of its own, so that the connection this case kills is not the one the fixture's own teardown stops.
				IServerProcess holder = Session.StartProcess(new ProcessInfo(Session.SessionInfo));
				try
				{
					Assert.That(RemoteCallDepth(holder, "ReclaimedLink"), Is.EqualTo(DefaultCallDepth));
					Assert.That(ConnectionCount(holder), Is.EqualTo(1));

					doomedServer.Stop();

					Assert.That(Evaluate(holder, "1 + 1"), Is.EqualTo(2), doomedServer.Diagnostic("A statement with nothing to do with the link failed because the server the link named was down."));
					Assert.That(ConnectionCount(holder), Is.EqualTo(1), "A statement that never spoke to the connection released it.");

					// The use finds the connection dead and lets it go; the reconnect it then makes fails, because the far server
					// is still down.
					Assert.Catch(() => { RemoteCallDepth(holder, "ReclaimedLink"); }, doomedServer.Diagnostic("Using a link whose server was down succeeded."));
					Assert.That(ConnectionCount(holder), Is.EqualTo(0));

					Assert.DoesNotThrow(() => holder.ExecuteScript("drop server ReclaimedLink;"), doomedServer.Diagnostic("Dropping the link failed because the server it named was down."));
				}
				finally
				{
					Session.StopProcess(holder);
				}
			}
		}

		/// <summary>Only the connection whose server has gone is let go; a live one on the same process keeps its session on the other server.</summary>
		[Test]
		public void TestOnlyTheConnectionWhoseServerHasGoneIsLetGo()
		{
			using (OutOfProcessInstance doomedServer = new OutOfProcessInstance(DoomedTargetInstanceName))
			{
				ExecuteScript(CreateLink("SurvivingLink", null));
				ExecuteScript(CreateLink("DyingLink", doomedServer, null));

				IServerProcess holder = Session.StartProcess(new ProcessInfo(Session.SessionInfo));
				try
				{
					Assert.That(RemoteCallDepth(holder, "SurvivingLink"), Is.EqualTo(DefaultCallDepth));
					Assert.That(RemoteCallDepth(holder, "DyingLink"), Is.EqualTo(DefaultCallDepth));
					Assert.That(ConnectionCount(holder), Is.EqualTo(2));

					doomedServer.Stop();

					Assert.That(Evaluate(holder, "1 + 1"), Is.EqualTo(2), doomedServer.Diagnostic("A statement with nothing to do with either link failed because the server one of them named was down."));
					Assert.That(ConnectionCount(holder), Is.EqualTo(2), "A statement that spoke to neither connection released one of them.");

					// Only the dead one is let go, and only by the use that found it dead; the live one is untouched by that use.
					Assert.Catch(() => { RemoteCallDepth(holder, "DyingLink"); }, doomedServer.Diagnostic("Using the link whose server was down succeeded."));
					Assert.That(ConnectionCount(holder), Is.EqualTo(1));
					Assert.That(RemoteCallDepth(holder, "SurvivingLink"), Is.EqualTo(DefaultCallDepth));
				}
				finally
				{
					Session.StopProcess(holder);
				}
			}
		}

		/// <summary>A connection that is a participant in a transaction the process still has open may not be let go: the use that finds it dead fails naming the link, and the session recovers once the transaction is rolled back.</summary>
		/// <remarks>
		/// Work has been done on such a connection at a level that is still open, so forgetting it would let the local
		/// commit believe the remote half committed. The failure was at a nested begin until lazy enlistment stopped the begin
		/// speaking to connections at all; it is now at the use, which is where the connection is brought up to the process's
		/// depth. The failure is asserted by error code rather than by message: a failure from this layer can arrive wrapped, as
		/// the remark on TestLinkUserChangeClosesTheChangingProcessConnection records, and matching the message would pin the
		/// wrapping rather than the behaviour.
		/// </remarks>
		[Test]
		public void TestAUseOfADeadParticipantFailsNamingTheLink()
		{
			using (OutOfProcessInstance doomedServer = new OutOfProcessInstance(DoomedTargetInstanceName))
			{
				ExecuteScript(CreateLink("ParticipantLink", doomedServer, null));

				IServerProcess holder = Session.StartProcess(new ProcessInfo(Session.SessionInfo));
				try
				{
					// Used inside the transaction while the child is alive, so the connection is a participant at depth 1 when
					// the child goes. Enlistment is by use, so a connection opened outside the transaction would hold no level.
					holder.BeginTransaction(IsolationLevel.Isolated);
					Assert.That(RemoteCallDepth(holder, "ParticipantLink"), Is.EqualTo(DefaultCallDepth));
					doomedServer.Stop();

					// The nested begin speaks to no connection, so it now succeeds where it used to fail.
					Assert.DoesNotThrow(() => holder.BeginTransaction(IsolationLevel.Isolated), doomedServer.Diagnostic("Beginning a nested transaction spoke to a connection the begin has nothing to do with."));

					Exception failedUse = Assert.Catch(() => { RemoteCallDepth(holder, "ParticipantLink"); }, doomedServer.Diagnostic("Using a link whose server was down, and whose connection was a participant in the open transaction, succeeded."));
					Assert.That(FindError(failedUse, (int)ServerException.Codes.RemoteSessionTransactionFailed), Is.Not.Null, "The failed use did not report the server link.");
					Assert.That(holder.TransactionCount, Is.EqualTo(2));
					Assert.That(ConnectionCount(holder), Is.EqualTo(1));

					// The inner level is one the connection does not hold, so ending it reaches nothing and is quiet; the outer
					// level is one it does hold, so that rollback is the one that reports the remote half could not be rolled back.
					Assert.DoesNotThrow(() => holder.RollbackTransaction(), doomedServer.Diagnostic("Rolling back a level the dead connection did not hold spoke to it anyway."));
					Assert.Catch(() => holder.RollbackTransaction(), doomedServer.Diagnostic("Rolling back a transaction whose remote half could not be rolled back reported nothing."));
					Assert.That(holder.InTransaction, Is.False);

					// The session works again, and the connection is kept until a statement uses the link and lets it go.
					Assert.That(Evaluate(holder, "1 + 1"), Is.EqualTo(2), doomedServer.Diagnostic("The session did not recover after the transaction holding the dead connection was rolled back."));
					Assert.That(ConnectionCount(holder), Is.EqualTo(1));
				}
				finally
				{
					Session.StopProcess(holder);
				}
			}
		}

		/// <summary>The DataphorException of the given code in the exception and its inner exception chain, or null if there is none.</summary>
		/// <remarks>
		/// Looked for down the chain rather than taken off the top: the failure now comes out of a D4 statement, so the engine
		/// has wrapped it in RuntimeException 104229 "Runtime error" by the time a caller sees it.
		/// </remarks>
		private static DataphorException FindError(Exception exception, int code)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is DataphorException dataphorException && dataphorException.Code == code)
					return dataphorException;
			return null;
		}

		/// <summary>A link naming the shared child server; the overload says what the tags are.</summary>
		private string CreateLink(string name, int? defaultMaxCallDepth)
		{
			return CreateLink(name, _linkedServer, defaultMaxCallDepth);
		}

		/// <summary>A link naming a child server by host and port, asking for a remote session with a call depth of its own when one is given.</summary>
		private static string CreateLink(string name, OutOfProcessInstance server, int? defaultMaxCallDepth)
		{
			return
				String.Format
				(
					"create server {0} tags {{ HostName = \"{1}\", InstanceName = \"{2}\", OverridePortNumber = \"{3}\"{4} }};",
					name,
					server.HostName,
					server.InstanceName,
					server.PortNumber,
					defaultMaxCallDepth == null ? String.Empty : String.Format(", DefaultMaxCallDepth = \"{0}\"", defaultMaxCallDepth.Value)
				);
		}

		/// <summary>The call depth of the session this process holds on the other server, which the link's session settings fixed when the connection was made.</summary>
		private int RemoteCallDepth(string linkName)
		{
			return RemoteCallDepth(Process, linkName);
		}

		/// <summary>The same, for a process other than the fixture's own.</summary>
		private static int RemoteCallDepth(IServerProcess process, string linkName)
		{
			return (int)Evaluate(process, String.Format("EvaluateOn('{0}', 'select Processes[ProcessID()].MaxNestingLevel') as Integer", linkName));
		}

		/// <summary>How many connections to other servers this process holds.</summary>
		private int ConnectionCount()
		{
			return ConnectionCount(Process);
		}

		/// <summary>The same, for a process other than the fixture's own.</summary>
		private static int ConnectionCount(IServerProcess process)
		{
			return (int)Evaluate(process, "Count(RemoteSessions where Process_ID = ProcessID())");
		}

		private object Evaluate(string expression)
		{
			return Evaluate(Process, expression);
		}

		private static object Evaluate(IServerProcess process, string expression)
		{
			return ((ServerProcess)process).Evaluate(expression, null).AsNative;
		}
	}
}
