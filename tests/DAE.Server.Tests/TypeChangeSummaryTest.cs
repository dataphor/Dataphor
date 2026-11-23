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
	/// What a change to a data type's checks, default or handlers leaves in the running server, for a table that has a
	/// column of that type. Beside the table and beside each of its columns the engine keeps a summary of the definition -
	/// "is there a default worth asking for", "is there a check worth running", "is there a change handler worth calling" -
	/// and skips the work entirely when the summary says there is nothing there. A column's summary is derived from the
	/// column's data type as well as from the column, so a change to the type makes it stale.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each case asks through a cursor that was opened before the change and is still open after it, which is how an open
	/// form reaches the table. That is the state in which these are visible: compiling an expression over a table
	/// recomputes its summary as a side effect, so a cursor opened afresh repairs the summary on its way in, while a cursor
	/// already open goes on reading the stale one for as long as it stays open.
	/// </para>
	/// <para>
	/// Half of the cases change the type and commit, with no transaction at all, because this is not a rollback defect: the
	/// statement that changes a type holds only the type, while the summaries that change with it belong to every table
	/// with a column of that type. The rolled-back cases are here as well because they reach the invalidation through a
	/// different place - the catalog undo path rather than the statement - the view case because a view recomputes its
	/// summary by a different rule than a table does, and the event-handler case because attaching a handler bumps none of
	/// the catalog's other timestamps.
	/// </para>
	/// <para>
	/// Deliberately not covered: the same changes asked through a restriction or any other operator over the table. Those
	/// put a plan-local table variable at the top of the plan, whose summary starts with every flag raised, so the proposal
	/// descends and meets the real definition whatever the summary says. A case there would pin that accident of the plan
	/// rather than a behaviour.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class TypeChangeSummaryTest : InProcessTestFixture
	{
		/// <summary>
		/// A rolled-back drop of a check on a data type still refuses a value the check rejects, through a cursor that was
		/// open across the rollback. Reading the table inside the transaction is what left the summary lowered, and the undo
		/// instruction that restores the check holds the type, not the tables with a column of it.
		/// </summary>
		[Test]
		public void TestRolledBackDropTypeConstraintStillRefusesTheValueItRejects()
		{
			ExecuteScript("create type DropTypeConstraintT { representation DropTypeConstraintT { Value : Integer }, constraint IsPositive value.Value > 0 };");
			ExecuteScript("create table DropTypeConstraintTbl { ID : Integer, Amount : DropTypeConstraintT, key { ID } };");

			IServerCursor cursor = Process.OpenCursor("select DropTypeConstraintTbl capabilities { updateable }", null);
			try
			{
				Process.BeginTransaction(IsolationLevel.Isolated);
				ExecuteScript("alter type DropTypeConstraintT { drop constraint IsPositive };");
				// Compiling an expression over the table recomputes its summary from the definition as it stands, which
				// inside this transaction is the one without the check. Without this read the summary would still be the
				// one computed when the cursor was opened, and the rollback would have nothing to put back.
				ExecuteScript("select DropTypeConstraintTbl;");
				Process.RollbackTransaction();

				Exception exception = Assert.Catch(() => Validate(cursor, 1, 0));
				Assert.That(exception.ToString(), Does.Contain("IsPositive"));
			}
			finally
			{
				Process.CloseCursor(cursor);
			}
		}

		/// <summary>
		/// A committed creation of a check on a data type refuses a value the check rejects, through a cursor that was open
		/// before the statement. No transaction is involved: this is the ordinary way an application evolves a type, and
		/// every form already open on a table with a column of that type was told there was nothing to check.
		/// </summary>
		[Test]
		public void TestCommittedCreateTypeConstraintRefusesTheValueItRejects()
		{
			ExecuteScript("create type CreateTypeConstraintT { representation CreateTypeConstraintT { Value : Integer } };");
			ExecuteScript("create table CreateTypeConstraintTbl { ID : Integer, Amount : CreateTypeConstraintT, key { ID } };");

			IServerCursor cursor = Process.OpenCursor("select CreateTypeConstraintTbl capabilities { updateable }", null);
			try
			{
				ExecuteScript("alter type CreateTypeConstraintT { create constraint IsPositive value.Value > 0 };");

				Exception exception = Assert.Catch(() => Validate(cursor, 1, 0));
				Assert.That(exception.ToString(), Does.Contain("IsPositive"));
			}
			finally
			{
				Process.CloseCursor(cursor);
			}
		}

		/// <summary>
		/// A rolled-back drop of a data type's default still defaults a new row. The table-level summary alone decides for a
		/// whole new row, so a stale one costs every column of the table its default, not only the column whose type was
		/// altered.
		/// </summary>
		[Test]
		public void TestRolledBackDropTypeDefaultStillDefaultsANewRow()
		{
			ExecuteScript("create type DropTypeDefaultT { representation DropTypeDefaultT { Value : Integer }, default DropTypeDefaultT(7) };");
			ExecuteScript("create table DropTypeDefaultTbl { ID : Integer, Amount : DropTypeDefaultT, key { ID } };");

			IServerCursor cursor = Process.OpenCursor("select DropTypeDefaultTbl capabilities { updateable }", null);
			try
			{
				Process.BeginTransaction(IsolationLevel.Isolated);
				ExecuteScript("alter type DropTypeDefaultT { drop default };");
				ExecuteScript("select DropTypeDefaultTbl;");
				Process.RollbackTransaction();

				Assert.That(DefaultOf(cursor, "Amount"), Is.EqualTo(7));
			}
			finally
			{
				Process.CloseCursor(cursor);
			}
		}

		/// <summary>A committed creation of a data type's default is offered to a cursor that was open before the statement.</summary>
		[Test]
		public void TestCommittedCreateTypeDefaultDefaultsANewRow()
		{
			ExecuteScript("create type CreateTypeDefaultT { representation CreateTypeDefaultT { Value : Integer } };");
			ExecuteScript("create table CreateTypeDefaultTbl { ID : Integer, Amount : CreateTypeDefaultT, key { ID } };");

			IServerCursor cursor = Process.OpenCursor("select CreateTypeDefaultTbl capabilities { updateable }", null);
			try
			{
				ExecuteScript("alter type CreateTypeDefaultT { create default CreateTypeDefaultT(7) };");

				Assert.That(DefaultOf(cursor, "Amount"), Is.EqualTo(7));
			}
			finally
			{
				Process.CloseCursor(cursor);
			}
		}

		/// <summary>
		/// The same through a view rather than the table itself. A view's summary is recomputed without being cleared first,
		/// so it can be raised but not lowered; this case is what says the raise is enough - the view's columns carry the
		/// source columns' data types, so recomputing sees the type's new check.
		/// </summary>
		[Test]
		public void TestCommittedCreateTypeConstraintRefusesThroughAView()
		{
			ExecuteScript("create type ViewConstraintT { representation ViewConstraintT { Value : Integer } };");
			ExecuteScript("create table ViewConstraintTbl { ID : Integer, Amount : ViewConstraintT, key { ID } };");
			ExecuteScript("create view ViewConstraintVw ViewConstraintTbl;");

			IServerCursor cursor = Process.OpenCursor("select ViewConstraintVw capabilities { updateable }", null);
			try
			{
				ExecuteScript("alter type ViewConstraintT { create constraint IsPositive value.Value > 0 };");

				Exception exception = Assert.Catch(() => Validate(cursor, 1, 0));
				Assert.That(exception.ToString(), Does.Contain("IsPositive"));
			}
			finally
			{
				Process.CloseCursor(cursor);
			}
		}

		/// <summary>
		/// A change handler attached to a data type runs for a cursor that was open before it was attached. Attaching a
		/// handler bumps none of the catalog's other coordination timestamps, which is why the summary needs a validity key
		/// of its own rather than borrowing one of them. Asked behaviourally - does the handler run - rather than by reading
		/// the flag, so that what it pins is what a user meets.
		/// </summary>
		[Test]
		public void TestCommittedAttachTypeChangeHandlerRunsForAnOpenCursor()
		{
			ExecuteScript("create type ChangeHandlerT { representation ChangeHandlerT { Value : Integer } };");
			ExecuteScript("create table ChangeHandlerTbl { ID : Integer, Amount : ChangeHandlerT, key { ID } };");
			ExecuteScript("create operator ChangeHandlerT_Change(var AValue : ChangeHandlerT) : Boolean begin result := true; AValue := ChangeHandlerT(99); end;");

			IServerCursor cursor = Process.OpenCursor("select ChangeHandlerTbl capabilities { updateable }", null);
			try
			{
				ExecuteScript("attach operator ChangeHandlerT_Change to ChangeHandlerT on { change };");

				Assert.That(AmountAfterChange(cursor, 1, 2), Is.EqualTo(99));
			}
			finally
			{
				Process.CloseCursor(cursor);
			}
		}

		/// <summary>The value the engine proposes for the column of a whole new row, or null where it proposes none.</summary>
		private static object DefaultOf(IServerCursor cursor, string columnName)
		{
			IRow row = cursor.Plan.RequestRow();
			try
			{
				row.ClearValues();
				cursor.Default(row, String.Empty);
				return row.HasValue(columnName) ? row[columnName] : null;
			}
			finally
			{
				cursor.Plan.ReleaseRow(row);
			}
		}

		/// <summary>Offers a whole new row to the engine for checking, as a client does before sending it.</summary>
		private static void Validate(IServerCursor cursor, int id, int amount)
		{
			IRow row = cursor.Plan.RequestRow();
			try
			{
				row.ClearValues();
				row["ID"] = id;
				row["Amount"] = amount;
				cursor.Validate(null, row, String.Empty);
			}
			finally
			{
				cursor.Plan.ReleaseRow(row);
			}
		}

		/// <summary>Tells the engine the user has changed Amount, as a client does when the user leaves the field, and returns what the engine left in the row.</summary>
		private static object AmountAfterChange(IServerCursor cursor, int oldAmount, int newAmount)
		{
			IRow oldRow = cursor.Plan.RequestRow();
			try
			{
				IRow newRow = cursor.Plan.RequestRow();
				try
				{
					oldRow.ClearValues();
					oldRow["ID"] = 1;
					oldRow["Amount"] = oldAmount;
					newRow.ClearValues();
					newRow["ID"] = 1;
					newRow["Amount"] = newAmount;
					cursor.Change(oldRow, newRow, "Amount");
					return newRow.HasValue("Amount") ? newRow["Amount"] : null;
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
	}
}
