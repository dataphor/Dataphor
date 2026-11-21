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
	/// re-created or altered: a link is followed by its identity rather than by its name, and a drop or an alter ends the
	/// issuing process's connection to it.
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

		/// <summary>The instance served by the child one case starts and stops itself, so that it can drop a link whose server has gone.</summary>
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
			return (int)Evaluate("Count(RemoteSessions where Process_ID = ProcessID())");
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
