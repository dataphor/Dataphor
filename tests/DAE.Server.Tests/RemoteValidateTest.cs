/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The third of the three questions a client asks about a row the user is editing - "is this row acceptable" - asked
	/// through the client stack, where it reaches the server as a remote call rather than being answered on the client.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The call takes the row as it was and the row as the user has it now, and the row to check is the second one.
	/// <c>RemoteServerCursor.Validate</c> passed them to its own body the other way round, so the row checked was the old
	/// one and the row sent back to the client was the old one too: a bad new value was accepted, a stored value that was
	/// already bad was reported against the user, and a validate handler that changed the row replaced the user's edit
	/// with the stored value. Its two siblings, <c>Default</c> and <c>Change</c>, pass their arguments straight through.
	/// </para>
	/// <para>
	/// Every case asks the same question twice - once through the client stack and once on the engine's own cursor - and
	/// asserts that the two answers agree, rather than against a transcribed expectation. What is pinned is therefore
	/// "the client stack gives the same answer as the engine", which is the property the remote proposal calls exist to
	/// have; there is no reference runtime to transcribe from.
	/// </para>
	/// <para>
	/// The remote path is reached only when the validation is *not* remotable: otherwise the client answers the question
	/// itself out of its own copy of the table variable and no call is made. So each table tags its edited column
	/// <c>DAE.IsValidateRemotable = "false"</c>, which is where <c>Compiler.CompileTableVarColumn</c> reads it - on a
	/// constraint the same tag is metadata of the constraint and leaves the column's own flag alone, which is why the
	/// constraint is added by a separate <c>alter</c>.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class RemoteValidateTest : ClientStackTestFixture
	{
		/// <summary>
		/// The value the user has just typed is the one checked. A new row that violates a column constraint must be
		/// refused even though the row it replaces was acceptable; before the fix the client stack accepted it, because
		/// the acceptable old row was what reached the engine.
		/// </summary>
		[Test]
		public void TestTheNewRowIsTheRowValidated()
		{
			CreateConstrainedTable("SwapTbl");

			AssertClientAgreesWithEngine("SwapTbl", 5, -1);
		}

		/// <summary>
		/// The row as it was is not checked. A stored value that already violates the constraint must not be reported
		/// against a user whose edit is acceptable; before the fix the client stack refused the edit.
		/// </summary>
		[Test]
		public void TestTheOldRowIsNotValidated()
		{
			CreateConstrainedTable("NoSwapTbl");

			AssertClientAgreesWithEngine("NoSwapTbl", -1, 5);
		}

		/// <summary>
		/// The row sent back to the client is the row that was checked. When a validate handler changes the row the call
		/// reports that it did, and the client writes the returned row over the row the user is editing; before the fix
		/// the returned row was the old one, so the user's edit vanished from the form with no error.
		/// </summary>
		[Test]
		public void TestTheValidatedRowIsTheRowReturned()
		{
			ExecuteScript("create table ClobberTbl { ID : Integer, Amount : Integer tags { DAE.IsValidateRemotable = \"false\" }, Note : String, key { ID } };");
			ExecuteScript
			(
				@"create operator ClobberTbl_Validate(const AOldRow : row { ID : Integer, Amount : Integer, Note : String }, var ANewRow : row { ID : Integer, Amount : Integer, Note : String }, const AColumnName : String) : Boolean
					begin result := true; ANewRow.Note := 'seen'; end;"
			);
			ExecuteScript("attach operator ClobberTbl_Validate to ClobberTbl on { validate };");

			ValidateOutcome throughClient = Validate(ClientProcess, "ClobberTbl", 10, 20, "old", "new");
			ValidateOutcome throughEngine = Validate(EngineProcess, "ClobberTbl", 10, 20, "old", "new");

			Assert.That(throughClient, Is.EqualTo(throughEngine), "The client stack and the engine must leave the same row behind.");
			// Named as well as compared, so that a regression that broke both paths the same way could not pass.
			Assert.That(throughClient.Amount, Is.EqualTo(20), "The handler must have run against the row the user is editing.");
			Assert.That(throughClient.Note, Is.EqualTo("seen"), "The handler's own change must survive.");
		}

		/// <summary>A table whose Amount column may not go negative, and whose validate is deliberately not remotable.</summary>
		private void CreateConstrainedTable(string name)
		{
			ExecuteScript(String.Format("create table {0} {{ ID : Integer, Amount : Integer tags {{ DAE.IsValidateRemotable = \"false\" }}, key {{ ID }} }};", name));
			ExecuteScript(String.Format("alter table {0} {{ alter column Amount {{ create constraint IsPositive value > 0 }} }};", name));
		}

		private void AssertClientAgreesWithEngine(string tableName, int oldAmount, int newAmount)
		{
			ValidateOutcome throughClient = Validate(ClientProcess, tableName, oldAmount, newAmount, null, null);
			ValidateOutcome throughEngine = Validate(EngineProcess, tableName, oldAmount, newAmount, null, null);

			Assert.That(throughClient, Is.EqualTo(throughEngine), "The client stack and the engine must give the same answer.");
		}

		/// <summary>
		/// What came of offering the engine an edit of Amount: whether it was refused, and what it left in the new row.
		/// A refusal carries no error text, because the client stack wraps the engine's exception and the two messages are
		/// not meant to be identical - what the cases compare is the verdict and the row.
		/// </summary>
		private readonly record struct ValidateOutcome(bool Refused, object Amount, object Note);

		/// <summary>
		/// Opens an updateable cursor on <paramref name="process"/> and asks it to check an edit of Amount, as a client
		/// does when the user leaves the field. Note is set only where the table has the column.
		/// </summary>
		private static ValidateOutcome Validate(IServerProcess process, string tableName, int oldAmount, int newAmount, string oldNote, string newNote)
		{
			IServerCursor cursor = process.OpenCursor(String.Format("select {0} capabilities {{ updateable }}", tableName), null);
			try
			{
				IRow oldRow = cursor.Plan.RequestRow();
				try
				{
					IRow newRow = cursor.Plan.RequestRow();
					try
					{
						FillRow(oldRow, oldAmount, oldNote);
						FillRow(newRow, newAmount, newNote);
						try
						{
							cursor.Validate(oldRow, newRow, "Amount");
						}
						catch (Exception)
						{
							return new ValidateOutcome(true, null, null);
						}
						return
							new ValidateOutcome
							(
								false,
								newRow.HasValue("Amount") ? newRow["Amount"] : null,
								newRow.DataType.Columns.ContainsName("Note") && newRow.HasValue("Note") ? newRow["Note"] : null
							);
					}
					finally
					{
						cursor.Plan.ReleaseRow(newRow);
					}
				}
				finally
				{
					cursor.Plan.ReleaseRow(oldRow);
				}
			}
			finally
			{
				process.CloseCursor(cursor);
			}
		}

		private static void FillRow(IRow row, int amount, string note)
		{
			row.ClearValues();
			row["ID"] = 1;
			row["Amount"] = amount;
			if (note != null)
				row["Note"] = note;
		}
	}
}
