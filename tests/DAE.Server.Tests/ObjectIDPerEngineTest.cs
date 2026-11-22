/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Starting a second engine in this process does not disturb the catalog of an engine that is already running.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The reproduction of bug-object-id-counter-is-process-wide. Catalog object identifiers used to come from one
	/// counter shared by the whole program, and <c>Engine.InitializeCatalog</c> resets the counter as it numbers the
	/// engine's own built-in objects - so a second engine's start handed an engine that was already running identifiers
	/// its catalog already held, and that engine's next schema change failed. Each engine now has a
	/// <c>Schema.ObjectIDGenerator</c> of its own.
	/// </para>
	/// <para>
	/// Measured on the unfixed tree, by putting every engine back on one shared generator and running this case: the
	/// second <c>create table</c> fails in <c>CatalogDeviceSession.CacheCatalogObject</c> with "An item with the same
	/// key has already been added", because the in-memory catalog index refuses the reused identifier before the
	/// statement reaches the store. The store's own "UNIQUE constraint failed: DAEObjects.ID" - what the protocol
	/// fixtures showed - is the same collision arriving by a different route, where the colliding object is not already
	/// in the engine's cache.
	/// </para>
	/// <para>
	/// A second engine in one process is not contrived: <c>LocalServer</c> - the client side of every network connection
	/// - starts one to hold the catalog cache it compiles against, so a program that runs an in-process server and then
	/// connects to it over the network has two, and so does the IDE with an embedded alias open beside a remote one.
	/// This case starts the second engine the way <c>LocalServer</c> does, and stops it again before the first server's
	/// next schema change, because a stopped engine used to have moved the shared counter just as far as a running one.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class ObjectIDPerEngineTest
	{
		[Test]
		public void StartingASecondEngineLeavesTheFirstServersCatalogAlone()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(ObjectIDPerEngineTest)))
			{
				instance.Server.Start();
				instance.ExecuteScript("create table ObjectIDPerEngineFirst { ID : Integer, key { ID } };");

				// As LocalServer builds its client-side engine: a name of its own, no library directory and no catalog store,
				// so starting it writes nothing to disk. Logging off for the reason every fixture here keeps it off - a logging
				// server in an elevated process creates the Windows event-log source.
				Engine second = new Engine();
				try
				{
					second.Name = Schema.Object.NameFromGuid(Guid.NewGuid());
					second.LoggingEnabled = false;
					second.Start();
				}
				finally
				{
					second.Dispose();
				}

				Assert.DoesNotThrow
				(
					() => instance.ExecuteScript("create table ObjectIDPerEngineSecond { ID : Integer, key { ID } };"),
					"A second engine's start reached the first server's object identifiers."
				);
			}
		}
	}
}
