/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The two ways an engine's catalog object identifiers used to be handed out twice: by another engine's start, and
	/// by the engine's own catalog rebuild.
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

		/// <summary>
		/// The reproduction of bug-client-catalog-rebuild-reuses-object-ids, at the lowest layer that shows it. A
		/// repository engine - the catalog cache on the client side of every network connection - throws its catalog away
		/// and rebuilds it whenever the server's catalog moves on, and the rebuild used to restart the engine's generator,
		/// so the rebuilt catalog was handed the identifiers the catalog it replaced had used. Those are not free: every
		/// prepared table expression the client still holds owns a view in the replaced catalog, and dropping one of those
		/// stale views removed the *rebuilt* object's entry from the catalog device's identifier-to-name index, leaving a
		/// live object in the catalog that could no longer be resolved by its identifier.
		/// </summary>
		/// <remarks>
		/// Measured on the unfixed tree: both tables below are given identifier 359. A bare engine rather than a client
		/// over the wire, because <c>LocalServer.EnsureCacheConsistent</c> rebuilds by calling exactly this, and an
		/// out-of-process client fixture would take seconds to say the same thing.
		/// </remarks>
		[Test]
		public void RebuildingAnEnginesCatalogDoesNotReissueTheIdentifiersItHasAlreadyGivenOut()
		{
			// As LocalServer builds its client-side engine; see the case above for why the name and the logging flag.
			Engine engine = new Engine();
			try
			{
				engine.Name = Schema.Object.NameFromGuid(Guid.NewGuid());
				engine.LoggingEnabled = false;
				engine.Start();

				ExecuteScript(engine, "create table ObjectIDBeforeRebuild { ID : Integer, key { ID } };");
				int beforeRebuild = engine.Catalog["ObjectIDBeforeRebuild"].ID;

				engine.ClearCatalog();

				ExecuteScript(engine, "create table ObjectIDAfterRebuild { ID : Integer, key { ID } };");
				int afterRebuild = engine.Catalog["ObjectIDAfterRebuild"].ID;

				Assert.That
				(
					afterRebuild,
					Is.Not.EqualTo(beforeRebuild),
					"The rebuilt catalog reissued an identifier this engine had already given out, so the objects of the catalog it replaced - which its clients still hold - collide with it."
				);
			}
			finally
			{
				engine.Dispose();
			}
		}

		/// <summary>Runs D4 on a session and process of its own, as TestServerInstance does for a full server.</summary>
		/// <remarks>
		/// <para>
		/// The session is the one <c>LocalSession</c> opens on the catalog cache, verbatim: the system user in the System
		/// library. A repository engine has neither the Admin user nor the General library a default <c>SessionInfo</c>
		/// asks for, so connecting the way an ordinary client does fails with a schema error naming General.
		/// </para>
		/// <para>
		/// Nothing is left connected across the rebuild deliberately: <c>ClearCatalog</c> replaces the users and the
		/// catalog a live session holds, and <c>LocalServer</c> has fix-up code for that which is not what this case is
		/// about.
		/// </para>
		/// </remarks>
		private static void ExecuteScript(Engine engine, string script)
		{
			IServerSession session = ((IServer)engine).Connect(new SessionInfo(Engine.SystemUserID, String.Empty, Engine.SystemLibraryName, false));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					process.ExecuteScript(script);
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				engine.Disconnect(session);
			}
		}
	}
}
