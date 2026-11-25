/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// DropLibrary refuses inside a transaction the session opened itself (113251), in the one shape of such a transaction that
	/// nobody opens on purpose: the implicit transaction the engine leaves open because <c>SetUseImplicitTransactions(false)</c>
	/// switched implicit transactions off during its own call.
	/// </summary>
	/// <remarks>
	/// Why this is C# and not a D4 coverage script: the engine resets that transaction's ImplicitCallDepth - the stamp the
	/// refusal reads - only for a *top-level* transactional call on the process (EndTransactionalCall), and the coverage harness
	/// runs every script as a nested ExecuteScript inside one statement of its own, so a script never gets a top-level call. The
	/// ordinary explicit transaction, opened with BeginTransaction() inside a block, is a script's to cover and lives in
	/// LibraryAdministration.d4's error section; so does the positive half, every other DropLibrary in the library scripts,
	/// which runs inside the harness's implicit transaction and must keep working.
	///
	/// The case gets a session and process of its own rather than this fixture's, because it leaves implicit transactions
	/// switched off and a transaction open on whatever process it runs on.
	/// </remarks>
	[TestFixture]
	public class LibraryDropTransactionTest : InProcessTestFixture
	{
		private const string LibraryName = "DropInLeftOpenTransaction";

		[Test]
		public void DropIsRefusedInTheTransactionLeftOpenByTurningImplicitTransactionsOff()
		{
			Instance.ExecuteScript
			(
				String.Format
				(
					"CreateLibrary(LibraryDescriptor('{0}', VersionNumber(1, 0, 0), \"\", list(FileReference){{}}, list(LibraryReference){{}}));",
					LibraryName
				)
			);

			IServerSession session = Instance.Connect();
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					// This statement runs in the implicit transaction of its own call; the engine ends implicit transactions only
					// while they are on, so the transaction outlives the call and its stamp is reset to that of an explicit one.
					process.ExecuteScript("SetUseImplicitTransactions(false);");
					Assert.That(process.TransactionCount, Is.EqualTo(1), "The premise of this case: turning implicit transactions off leaves its own transaction open.");

					Exception exception = Assert.Catch(() => process.ExecuteScript(String.Format("DropLibrary('{0}');", LibraryName)));
					DataphorException error = FindError(exception, (int)Schema.SchemaException.Codes.CannotDropLibraryInExplicitTransaction);
					Assert.That(error, Is.Not.Null, exception.ToString());
					Assert.That(error.Message, Does.Contain(LibraryName));

					// The refusal is raised before anything is removed, and the drop runs once the transaction is gone - with
					// implicit transactions still off, so this is also the no-transaction-at-all shape.
					process.RollbackTransaction();
					Assert.That(LibraryExists(), Is.True, "The refused drop removed the library from the catalog anyway.");

					process.ExecuteScript(String.Format("DropLibrary('{0}');", LibraryName));
					Assert.That(LibraryExists(), Is.False);
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				Server.Disconnect(session);
			}
		}

		/// <summary>Asked on a process of its own, so that the answer does not depend on the state of the process under test.</summary>
		private bool LibraryExists()
		{
			return (bool)Instance.Evaluate(String.Format("exists (Libraries where Name = '{0}')", LibraryName));
		}
	}
}
