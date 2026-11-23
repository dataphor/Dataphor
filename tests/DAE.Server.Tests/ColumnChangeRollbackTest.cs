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
	/// What a rolled-back change to a column's default or to one of its checks leaves in the running server. Next to the
	/// table and next to each column the engine keeps a summary of the definition - "is there a default worth asking for",
	/// "is there a check worth running" - and skips the work entirely when the summary says there is nothing there. The
	/// summary is derived from the definition, so a rollback that puts the definition back has to recompute it.
	/// </summary>
	/// <remarks>
	/// Each case asks through a cursor that was opened before the transaction and is still open after the rollback, which is
	/// how an open form reaches the table. That is the state in which the defect is visible: every compilation of an
	/// expression over a table recomputes the summary from the live definition, so a cursor opened afresh after the rollback
	/// repairs it on its way in, while a cursor already open goes on reading the stale summary for as long as it is open.
	/// </remarks>
	[TestFixture]
	public class ColumnChangeRollbackTest : InProcessTestFixture
	{
		/// <summary>
		/// A rolled-back drop of a column's default still defaults a new row. The table-level summary alone decides for a whole
		/// new row, so a stale one costs every column of the table its default, not only the column that was altered. The
		/// summary only goes false when the altered column's default was the table's only one, which is why Name's is.
		/// </summary>
		[Test]
		public void TestRolledBackDropDefaultStillDefaultsANewRow()
		{
			ExecuteScript("create table DropDefaultT { ID : Integer, Name : String, key { ID } };");
			ExecuteScript("alter table DropDefaultT { alter column Name { create default \"abc\" } };");

			IServerCursor cursor = Process.OpenCursor("select DropDefaultT capabilities { updateable }", null);
			try
			{
				Process.BeginTransaction(IsolationLevel.Isolated);
				ExecuteScript("alter table DropDefaultT { alter column Name { drop default } };");
				Process.RollbackTransaction();

				Assert.That(DefaultOf(cursor, "Name"), Is.EqualTo("abc"));
			}
			finally
			{
				Process.CloseCursor(cursor);
			}
		}

		/// <summary>
		/// A rolled-back drop of a column's constraint still refuses a value the constraint rejects. This is the check a client
		/// is offered before it sends the row; the insert itself is refused by a different path, so a stale summary does not
		/// corrupt the table - it loses the early refusal, and the user meets the error only on saving.
		/// </summary>
		[Test]
		public void TestRolledBackDropColumnConstraintStillRefusesTheValueItRejects()
		{
			ExecuteScript("create table DropConstraintT { ID : Integer, Name : String, key { ID } };");
			ExecuteScript("alter table DropConstraintT { alter column Name { create constraint NameNotEmpty value <> \"\" } };");

			IServerCursor cursor = Process.OpenCursor("select DropConstraintT capabilities { updateable }", null);
			try
			{
				Process.BeginTransaction(IsolationLevel.Isolated);
				ExecuteScript("alter table DropConstraintT { alter column Name { drop constraint NameNotEmpty } };");
				Process.RollbackTransaction();

				Exception exception = Assert.Catch(() => Validate(cursor, 1, ""));
				Assert.That(exception.ToString(), Does.Contain("NameNotEmpty"));
			}
			finally
			{
				Process.CloseCursor(cursor);
			}
		}

		/// <summary>
		/// An inner rollback leaves the summary matching the definition the outer level holds, not the one the table started
		/// with, and the outer rollback then leaves it matching the original. Recomputing from the restored definition gives
		/// this; restoring a recorded flag value alongside each change would not, because the recorded value belongs to the
		/// level that recorded it.
		/// </summary>
		[Test]
		public void TestNestedRollbackLeavesTheOuterLevelsDefault()
		{
			ExecuteScript("create table NestedDefaultT { ID : Integer, Name : String, key { ID } };");

			IServerCursor cursor = Process.OpenCursor("select NestedDefaultT capabilities { updateable }", null);
			try
			{
				Process.BeginTransaction(IsolationLevel.Isolated);
				try
				{
					ExecuteScript("alter table NestedDefaultT { alter column Name { create default \"outer\" } };");

					Process.BeginTransaction(IsolationLevel.Isolated);
					ExecuteScript("alter table NestedDefaultT { alter column Name { drop default } };");
					Process.RollbackTransaction();

					Assert.That(DefaultOf(cursor, "Name"), Is.EqualTo("outer"));
				}
				finally
				{
					Process.RollbackTransaction();
				}

				Assert.That(DefaultOf(cursor, "Name"), Is.Null);
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
		private static void Validate(IServerCursor cursor, int id, string name)
		{
			IRow row = cursor.Plan.RequestRow();
			try
			{
				row.ClearValues();
				row["ID"] = id;
				row["Name"] = name;
				cursor.Validate(null, row, String.Empty);
			}
			finally
			{
				cursor.Plan.ReleaseRow(row);
			}
		}
	}
}
