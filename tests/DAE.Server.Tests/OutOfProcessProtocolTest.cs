/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using NUnit.Framework;

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
	/// program, from an instance list of its own, serves the shipped client stack and stops when it is asked to; and the
	/// two engines are genuinely separate, which in one process they are not (starting a second engine resets a counter
	/// the first is still using - bug-object-id-counter-is-process-wide). It is also the only place a second server, or
	/// a second client program, can exist without the two sharing the engine's process-wide state.
	/// </para>
	/// <para>
	/// Deliberately not repeated here: streams over the wire, the message-size quotas and typed faults. Those already
	/// run against the real host with the real client over a real socket in <see cref="CoreWcfProtocolTest"/>, and
	/// moving the host into another program changes no part of that path, so a second copy would cost suite time and
	/// establish nothing.
	/// </para>
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: the child servers share no engine state with this process, but the clients these cases
	// build do - each LocalServer starts a client-side engine here, and those share the object-id counter (see
	// ProtocolClient). Nothing in this project is parallelizable.
	public class OutOfProcessProtocolTest
	{
		private const string InstanceName = "OutOfProcess";

		/// <summary>How many rows the round-trip case inserts through one prepared plan.</summary>
		private const int RowCount = 3;

		/// <summary>One child server for the whole fixture: a start is a server start plus a catalog store creation.</summary>
		private OutOfProcessInstance _instance;

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
			List<string> result = new List<string>();
			IServerCursor cursor = process.OpenCursor("select AcrossProcesses order by { ID }", null);
			try
			{
				while (cursor.Next())
					using (IRow row = cursor.Select())
						result.Add((string)row["Name"]);
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
	}
}
