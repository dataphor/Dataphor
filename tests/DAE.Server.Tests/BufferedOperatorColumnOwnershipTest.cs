/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A run-time buffer describes itself with copies of the columns of the plan it reads, never with the plan's own column
	/// objects: adding a column to a table variable's column list makes that table variable the column's parent
	/// (Schema.TableVarColumns.Adding), so a buffer that borrows a source's columns reparents them onto itself and leaves them
	/// pointing at a table variable that is discarded when the cursor closes. A column's parent is what its CatalogObjectID
	/// and ParentObjectID report, and those are what the catalog store records for the column's default and constraints, so
	/// the borrowed column's recorded parent is wrong for the rest of the engine's life.
	/// </summary>
	/// <remarks>
	/// Union is the operator that had it: ExplodeTable and OrderTables build their buffers from column copies, UnionTable
	/// computed a copy and then added the source's column instead. Only the left side's columns go into the buffer's column
	/// list, so only the left side reproduced it; the right side is asserted too because nothing in D4 distinguishes the two
	/// sides, and a buffer that started borrowing the right side's columns would be the same defect.
	/// </remarks>
	[TestFixture]
	public class BufferedOperatorColumnOwnershipTest : InProcessTestFixture
	{
		[Test]
		public void BufferingAUnionLeavesItsSourcesColumnsOnTheirOwnTableVars()
		{
			ExecuteScript
			(
				@"
					create table UnionLeft { ID : Integer, Name : String, key { ID } };
					create table UnionRight { ID : Integer, Name : String, key { ID } };
					insert table { row { 1 ID, 'left' Name } } into UnionLeft;
					insert table { row { 2 ID, 'right' Name } } into UnionRight;
				"
			);

			Schema.BaseTableVar left = ResolveBaseTableVar("UnionLeft");
			Schema.BaseTableVar right = ResolveBaseTableVar("UnionRight");

			Process.Evaluate("Count(UnionLeft union UnionRight)", null);

			Assert.Multiple
			(
				() =>
				{
					AssertColumnsOwnedBy(left);
					AssertColumnsOwnedBy(right);
				}
			);
		}

		private static void AssertColumnsOwnedBy(Schema.BaseTableVar tableVar)
		{
			foreach (Schema.TableVarColumn column in tableVar.Columns)
				Assert.That
				(
					column.TableVar,
					Is.SameAs(tableVar),
					String.Format("column {0} of {1} was reparented onto {2} by the union's buffer.", column.Name, tableVar.Name, column.TableVar == null ? "nothing" : column.TableVar.Name)
				);
		}

		/// <summary>Finds the test's own table by its unqualified name, which is how the script names it.</summary>
		private Schema.BaseTableVar ResolveBaseTableVar(string name)
		{
			foreach (Schema.Object objectValue in Instance.Server.Catalog)
			{
				Schema.BaseTableVar tableVar = objectValue as Schema.BaseTableVar;
				if (tableVar != null && Schema.Object.Unqualify(tableVar.Name) == name)
					return tableVar;
			}

			Assert.Fail("the test's own table " + name + " is not in the catalog.");
			return null;
		}
	}
}
