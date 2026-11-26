/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The client types a freshly built catalog carries, and what the node-type operators do with one that is not there.
	/// Release 4.0 ships one client, so the shipped `Frontend.ClientTypes` holds one row; these cases are the record of the
	/// compatibility claim the migration note makes about the two rows that were removed with the browser clients.
	/// </summary>
	// Not [Parallelizable]: this fixture starts a server, and two engines in one process share the engine's static state
	// (see the note on TestServerInstance).
	[TestFixture]
	public class FrontendClientTypesTest : InProcessTestFixture
	{
		private const string EmptyNodeTypes = "<?xml version=\"1.0\" encoding=\"utf-8\" ?> <nodetypes xmlns=\"http://www.alphora.com/schemas/nodetypes\"></nodetypes>";

		protected override string[] Libraries => ["SimpleDevice", "Frontend"];

		protected override string[] LibrariesToRegister => ["Frontend"];

		private object Evaluate(string expression)
		{
			return ((ServerProcess)Process).Evaluate(expression, null).AsNative;
		}

		[Test]
		public void TheOnlyClientTypeIsWindows()
		{
			Assert.That(Evaluate("Count(Frontend.ClientTypes)"), Is.EqualTo(1));
			Assert.That(Evaluate("Frontend.ClientTypes['Windows'].Environment"), Is.EqualTo("WindowsClient"));
		}

		/// <summary>
		/// A client type that is not in the table is not an error: the node-type operators build a document name from the
		/// identifier and guard each load with DocumentExists, so a retired client now gets an empty document rather than a
		/// failure. Silence is the failure mode an application has to look for, which is why it is pinned here.
		/// </summary>
		[Test]
		public void AnUnlistedClientTypeGetsAnEmptyNodeTypesDocument()
		{
			Assert.That(Evaluate("Frontend.GetNodeTypes('Web', table { row { 'Frontend' Library_Name } })"), Is.EqualTo(EmptyNodeTypes));
			Assert.That(Evaluate("Frontend.GetLibraryNodeTypes('Silverlight', 'Frontend')"), Is.EqualTo(EmptyNodeTypes));
			Assert.That((string)Evaluate("Frontend.GetNodeTypes('Windows', table { row { 'Frontend' Library_Name } })"), Does.Contain("<nodetype name=\"textbox\"/>"));
		}
	}
}
