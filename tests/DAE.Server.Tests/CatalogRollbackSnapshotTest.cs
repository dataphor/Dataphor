/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// One general check of a whole family of defects: a rolled-back catalog change has to leave the running server's
	/// in-memory catalog exactly as it was, not only the saved copy in the catalog store. Each case snapshots the live
	/// catalog, runs one catalog-changing statement inside a transaction, requires that snapshot to have moved - a case
	/// whose statement the snapshot cannot see would pass whatever the rollback did - then rolls the transaction back,
	/// snapshots a third time and requires that one to equal the first.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Every defect in this family has the same shape: a change made in memory has no undo record, or its record
	/// restores the definition but not the state derived from the definition. The server and its own catalog then
	/// disagree, and the server believes the version that was rolled back until it is restarted. Six instances were
	/// found by reading code; this fixture replaces the reading with a check that runs on every build.
	/// </para>
	/// <para>
	/// What a passing run does <em>not</em> say:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// Library descriptors (<c>Catalog.Libraries</c>) are deliberately left out of the snapshot, and the four library
	/// operators (<c>CreateLibrary</c>, <c>DropLibrary</c>, <c>SetDefaultDeviceName</c>, <c>SetLibraryDescriptor</c>)
	/// out of the matrix. Those write the library folder and its <c>.d4l</c> file as they go and a rollback undoes
	/// neither memory nor disk, so memory and disk agree and a difference there is not this family. Library DDL is
	/// deliberately not transactional, and only <c>DropLibrary</c> refuses inside a transaction the session opened
	/// itself (error 113251), because it deletes the library's directory and a rollback cannot restore it; see the
	/// "Library DDL is not transactional" section of docs/architecture/libraries-and-class-loading.md.
	/// </item>
	/// <item>
	/// Anything fifteen of the cases change. Users, rights, the assignments between them and roles, a right's owner and
	/// an operator's body are each outside every dimension the snapshot has: users and rights are rows in the catalog
	/// store with a cache in front of them rather than objects in <c>Catalog</c>, the cache is not reachable from this
	/// assembly and this fixture deliberately does not widen access to reach it, and an operator's emitted statement
	/// carries its signature and not its body. Those cases run their statement and check that the rollback leaves the
	/// rest of the catalog alone; they say nothing about the state the statement itself touched. They are declared
	/// through <c>CaseTheSnapshotCannotSee</c>, so the list is the exact one, and
	/// <c>debt-snapshot-cannot-see-users-rights-and-operator-bodies</c> is the work that would shorten it. Every one of
	/// these statements does record an undo instruction, and the rights-cache questions were settled by
	/// <c>bug-right-cache-stale-across-transactions</c> and <c>bug-right-cache-shared-during-open-transaction</c>.
	/// </item>
	/// <item>
	/// A child object's own derived state. A table's constraints over the row and a type's constraints and default have
	/// a dimension of their own, because the parent's statement does not carry them; columns, keys and representations
	/// are compared only through that statement and through the parent's own scalar properties, so a column's
	/// <c>ShouldDefault</c> flag, for instance, is not compared on its own.
	/// </item>
	/// <item>
	/// The caches <c>InvalidateCachesAfterUndo</c> clears wholesale and the catalog store's own rows:
	/// <c>CatalogStoreTest.NestedRollbackUndoesStatementWrites</c> already compares every row of every store table
	/// around a rolled-back create and drop.
	/// </item>
	/// <item>
	/// A rolled-back <c>alter view V { create constraint ... }</c>, the one statement whose rollback is known not to put
	/// the catalog back as it was. It is left out of the matrix rather than asserted; the reason is in
	/// <see cref="Cases"/>'s remarks.
	/// </item>
	/// <item>
	/// NOTE: a change to a data <em>type</em> that a table has a column of. The matrix changes types
	/// (<c>CreateTypeConstraint</c> and its siblings) but never pairs one with such a table, and deliberately so: a
	/// dependent table's stored <c>ShouldDefault</c>/<c>ShouldValidate</c>/<c>ShouldChange</c> flags are left alone by
	/// both the statement and its undo, because the read path recomputes them for itself before every proposal
	/// (<c>Schema.TableVar.EnsureShouldCallProposables</c>). A case here would reflect over the stored flags and report a
	/// difference the engine no longer acts on. What a user meets in that situation is pinned behaviourally by
	/// <c>TypeChangeSummaryTest</c> instead.
	/// </item>
	/// <item>
	/// The forward direction: these cases only ask what a rollback leaves. The one exception is
	/// <see cref="CreatedTableConstraintRaisesTheValidateSummary"/>, which is here because this fixture is what found
	/// that defect - a rollback that recomputes derived state the forward path left stale shows up as a difference.
	/// </item>
	/// </list>
	/// <para>
	/// The snapshot reads nothing but memory. <c>ScriptCatalog()</c> would not do: its object selection comes from the
	/// catalog store rather than from memory, so an object wrongly left in memory would not appear in the script at
	/// all, and the state this family leaves behind is mostly not in an object's D4 statement.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class CatalogRollbackSnapshotTest : InProcessTestFixture
	{
		/// <summary>Differences reported before the message is cut short; a failure names the object and the property that differed.</summary>
		private const int MaxReportedDifferences = 25;

		/// <summary>
		/// Characters of an entry's value shown in the difference report. A whole emitted create statement, flattened onto
		/// one line, can run to thousands, which is why entries that appear or disappear are cut here. A <em>changed</em>
		/// entry is reported line by line instead, and that is what makes a difference readable: measured over the 1,254
		/// objects that emit on a bare server, the longest single line of any emitted statement is 268 characters, so no
		/// differing line reaches this limit and none is cut short.
		/// </summary>
		private const int MaxReportedValueLength = 400;

		[TestCaseSource(nameof(Cases))]
		public void RolledBackStatementLeavesTheCatalogAsItWas(string setup, string statement, bool isVisible)
		{
			if (!String.IsNullOrEmpty(setup))
				ExecuteScript(setup);

			Dictionary<string, string> before = Snapshot();

			// A statement that does not execute would compare two identical snapshots and prove nothing, so its error
			// is left to fail the case rather than caught.
			Process.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				ExecuteScript(statement);

				// The same trap one step further in: a statement that executes but changes nothing the snapshot can see
				// also compares two identical snapshots afterwards, and then passes whatever the rollback did. Each case
				// declares whether it is visible, and a visible one is held to it here - so a statement that quietly
				// stops changing the catalog fails rather than going green. The cases that are genuinely outside the
				// snapshot are declared through CaseTheSnapshotCannotSee and say why.
				if (isVisible)
					AssertDifferent(before, Snapshot(), statement);
			}
			finally
			{
				Process.RollbackTransaction();
			}

			AssertSame(before, Snapshot(), statement);
		}

		/// <summary>
		/// A rollback undoes the changes of its own level only: the inner rollback has to leave the catalog as the outer
		/// change left it, and the outer rollback as it was before either.
		/// </summary>
		[Test]
		public void InnerRollbackLeavesTheOuterChange()
		{
			ExecuteScript("create table NestedSnapshotTable { ID : Integer, Name : String, key { ID } };");

			Dictionary<string, string> beforeAnyChange = Snapshot();

			Process.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				ExecuteScript("alter table NestedSnapshotTable alter tags { create Frontend.Title = \"Outer\" };");
				Dictionary<string, string> afterOuterChange = Snapshot();

				Process.BeginTransaction(IsolationLevel.Isolated);
				try
				{
					ExecuteScript("alter table NestedSnapshotTable { create order { Name } };");
				}
				finally
				{
					Process.RollbackTransaction();
				}

				AssertSame(afterOuterChange, Snapshot(), "the inner rollback");
			}
			finally
			{
				Process.RollbackTransaction();
			}

			AssertSame(beforeAnyChange, Snapshot(), "the outer rollback");
		}

		/// <summary>
		/// Two snapshots of an untouched server are equal. A guard on the snapshot itself rather than on the engine: a
		/// property with a lazily computed or time-dependent value would make every case above flaky, and it is cheaper
		/// to catch that here than to read it as a rollback defect.
		/// </summary>
		[Test]
		public void SnapshotOfAnUntouchedServerIsStable()
		{
			AssertSame(Snapshot(), Snapshot(), "two snapshots in a row");
		}

		/// <summary>
		/// A row constraint created by <c>alter table</c> raises the table's derived validate summary straight away.
		/// The reproduction of the defect the matrix above surfaced, in the forward direction where the defect is: the
		/// undo of a dropped constraint recomputed the summary, the statement that created it did not, so a rollback
		/// left the table in a *better* state than it was in before - which is how a rollback test found a forward bug.
		/// </summary>
		/// <remarks>
		/// Asserted on the live <c>Schema.TableVar</c> rather than through a cursor, because the summary is repaired as
		/// a side effect of compiling any expression over the table and opening a cursor is such a compilation - so a
		/// client-level assertion would pass either way. <c>ColumnChangeRollbackTest</c> covers what a stale summary
		/// costs a client, for the undo direction, where an already-open cursor keeps reading the stale value.
		/// </remarks>
		[Test]
		public void CreatedTableConstraintRaisesTheValidateSummary()
		{
			ExecuteScript("create table ForwardSummaryTable { ID : Integer, key { ID } };");
			Assert.That(TableVar("ForwardSummaryTable").ShouldValidate, Is.False, "a table with nothing to validate");

			ExecuteScript("alter table ForwardSummaryTable { create constraint ForwardSummaryCheck not exists (ForwardSummaryTable where ID < 0) };");

			Assert.That(TableVar("ForwardSummaryTable").ShouldValidate, Is.True);
		}

		private Schema.TableVar TableVar(string tableName)
		{
			foreach (Schema.Object objectValue in Instance.Server.Catalog)
				if ((objectValue is Schema.TableVar) && (Schema.Object.Unqualify(objectValue.Name) == tableName))
					return (Schema.TableVar)objectValue;
			Assert.Fail(String.Format("Table {0} is not in the catalog.", tableName));
			return null;
		}

		#region The case matrix

		/// <summary>
		/// One case per kind of catalog-changing statement: a setup run outside the transaction, and the one statement
		/// run inside it. Objects are named per case because the fixture shares one server across its tests and nothing
		/// drops them afterwards - a teardown that dropped them would itself change the catalog.
		/// </summary>
		/// <remarks>
		/// Four statements are deliberately absent, so that they are not retried blind. <c>alter table T { create column
		/// ... }</c> and <c>{ drop column ... }</c> are refused for a table in a memory device ("Columns in a memory
		/// device cannot be created in this release"), so they need a device that supports them. A device scalar type map
		/// (<c>alter device D { create type Integer }</c>) is refused for the same reason - "Unable to determine a default
		/// scalar type map for scalar type System.Integer" - because a memory device maps no types; it needs a SQL device,
		/// and so a database server. The library operators are absent for the reason in the fixture's remarks.
		/// </para>
		/// <para>
		/// The fourth, <c>alter view V { create constraint ... }</c>, is absent because the rollback genuinely does not put
		/// the view back as it was and is not meant to: the statement raises the view's derived validate summary, and a
		/// view's recompute raises flags without resetting them (<c>Schema.DerivedTableVar.DetermineRemotable</c>), so the
		/// undo cannot lower it again. A flag left raised costs a call that finds nothing to do, which
		/// <c>CatalogDeviceSession.RedetermineRemotable</c>'s own remarks call not a defect; the alternative would be to
		/// leave a view with the lowered flag that <c>AlterTableVarNode.CreateConstraints</c> fixes for a table. Both sites
		/// carry a <c>NOTE:</c> saying so. The equivalent statement on a table, <c>AlterTableConstraint</c>, is in the
		/// matrix.
		/// </remarks>
		private static IEnumerable<TestCaseData> Cases()
		{
			// Tables and views
			yield return Case("CreateTable", null,
				"create table CreateTableSnap { ID : Integer, key { ID } };");
			yield return Case("DropTable",
				"create table DropTableSnap { ID : Integer, key { ID } };",
				"drop table DropTableSnap;");
			yield return Case("CreateColumnTag",
				"create table ColumnTagSnap { ID : Integer, Name : String, key { ID } };",
				"alter table ColumnTagSnap { alter column Name alter tags { create Frontend.Title = \"Changed\" } };");
			yield return Case("CreateKey",
				"create table CreateKeySnap { ID : Integer, Name : String, key { ID } };",
				"alter table CreateKeySnap { create key { Name } };");
			yield return Case("DropKey",
				"create table DropKeySnap { ID : Integer, Name : String, key { ID }, key { Name } };",
				"alter table DropKeySnap { drop key { Name } };");
			yield return Case("AlterKey",
				"create table AlterKeySnap { ID : Integer, key { ID } };",
				"alter table AlterKeySnap { alter key { ID } alter tags { create Frontend.Title = \"Changed\" } };");
			yield return Case("CreateOrder",
				"create table CreateOrderSnap { ID : Integer, Name : String, key { ID } };",
				"alter table CreateOrderSnap { create order { Name } };");
			yield return Case("DropOrder",
				"create table DropOrderSnap { ID : Integer, Name : String, key { ID }, order { Name } };",
				"alter table DropOrderSnap { drop order { Name } };");
			yield return Case("AlterOrder",
				"create table AlterOrderSnap { ID : Integer, Name : String, key { ID }, order { Name } };",
				"alter table AlterOrderSnap { alter order { Name } alter tags { create Frontend.Title = \"Changed\" } };");
			yield return Case("CreateTableConstraint",
				"create table TableConstraintSnap { ID : Integer, key { ID } };",
				"alter table TableConstraintSnap { create constraint TableConstraintSnapCheck not exists (TableConstraintSnap where ID < 0) };");
			yield return Case("DropTableConstraint",
				"create table DropTableConstraintSnap { ID : Integer, key { ID } }; alter table DropTableConstraintSnap { create constraint DropTableConstraintSnapCheck not exists (DropTableConstraintSnap where ID < 0) };",
				"alter table DropTableConstraintSnap { drop constraint DropTableConstraintSnapCheck };");
			yield return Case("AlterTableConstraint",
				"create table AlterTableConstraintSnap { ID : Integer, key { ID } }; alter table AlterTableConstraintSnap { create constraint AlterTableConstraintSnapCheck not exists (AlterTableConstraintSnap where ID < 0) };",
				"alter table AlterTableConstraintSnap { alter constraint AlterTableConstraintSnapCheck not exists (AlterTableConstraintSnap where ID < -1) };");
			yield return Case("CreateTransitionConstraint",
				"create table TransitionConstraintSnap { ID : Integer, key { ID } };",
				"alter table TransitionConstraintSnap { create transition constraint TransitionConstraintSnapCheck on insert new.ID >= 0 };");
			yield return Case("DropTransitionConstraint",
				"create table DropTransitionConstraintSnap { ID : Integer, key { ID } }; alter table DropTransitionConstraintSnap { create transition constraint DropTransitionConstraintSnapCheck on insert new.ID >= 0 };",
				"alter table DropTransitionConstraintSnap { drop transition constraint DropTransitionConstraintSnapCheck };");
			yield return Case("AlterTransitionConstraint",
				"create table AlterTransitionConstraintSnap { ID : Integer, key { ID } }; alter table AlterTransitionConstraintSnap { create transition constraint AlterTransitionConstraintSnapCheck on insert new.ID >= 0 };",
				"alter table AlterTransitionConstraintSnap { alter transition constraint AlterTransitionConstraintSnapCheck alter on insert new.ID >= 1 };");
			yield return Case("AlterTableTag",
				"create table TableTagSnap { ID : Integer, key { ID } }; alter table TableTagSnap alter tags { create Frontend.Title = \"Before\" };",
				"alter table TableTagSnap alter tags { alter Frontend.Title = \"After\" };");
			yield return Case("CreateColumnDefault",
				"create table ColumnDefaultSnap { ID : Integer, Name : String, key { ID } };",
				"alter table ColumnDefaultSnap { alter column Name { create default \"x\" } };");
			yield return Case("DropColumnDefault",
				"create table DropColumnDefaultSnap { ID : Integer, Name : String { default \"x\" }, key { ID } };",
				"alter table DropColumnDefaultSnap { alter column Name { drop default } };");
			yield return Case("CreateColumnConstraint",
				"create table ColumnConstraintSnap { ID : Integer, Name : String, key { ID } };",
				"alter table ColumnConstraintSnap { alter column Name { create constraint ColumnConstraintSnapCheck value <> \"\" } };");
			yield return Case("DropColumnConstraint",
				"create table DropColumnConstraintSnap { ID : Integer, Name : String { constraint DropColumnConstraintSnapCheck value <> \"\" }, key { ID } };",
				"alter table DropColumnConstraintSnap { alter column Name { drop constraint DropColumnConstraintSnapCheck } };");
			yield return Case("CreateView",
				"create table ViewSourceSnap { ID : Integer, Name : String, key { ID } };",
				"create view CreateViewSnap ViewSourceSnap over { ID };");
			yield return Case("DropView",
				"create table DropViewSourceSnap { ID : Integer, key { ID } }; create view DropViewSnap DropViewSourceSnap;",
				"drop view DropViewSnap;");
			yield return Case("AlterViewTag",
				"create table ViewTagSourceSnap { ID : Integer, key { ID } }; create view ViewTagSnap ViewTagSourceSnap;",
				"alter view ViewTagSnap alter tags { create Frontend.Title = \"Changed\" };");

			// Types
			yield return Case("CreateType", null,
				"create type CreateTypeSnap { representation CreateTypeSnap { Value : Integer } };");
			yield return Case("DropType",
				"create type DropTypeSnap { representation DropTypeSnap { Value : Integer } };",
				"drop type DropTypeSnap;");
			yield return Case("CreateTypeLike", null,
				"create type CreateTypeLikeSnap like String;");
			yield return Case("CreateTypeConstraint",
				"create type TypeConstraintSnap { representation TypeConstraintSnap { Value : Integer } };",
				"alter type TypeConstraintSnap { create constraint TypeConstraintSnapCheck value.Value > 0 };");
			yield return Case("DropTypeConstraint",
				"create type DropTypeConstraintSnap { representation DropTypeConstraintSnap { Value : Integer }, constraint DropTypeConstraintSnapCheck value.Value > 0 };",
				"alter type DropTypeConstraintSnap { drop constraint DropTypeConstraintSnapCheck };");
			yield return Case("CreateTypeDefault",
				"create type TypeDefaultSnap { representation TypeDefaultSnap { Value : Integer } };",
				"alter type TypeDefaultSnap { create default TypeDefaultSnap(0) };");
			yield return Case("DropTypeDefault",
				"create type DropTypeDefaultSnap { representation DropTypeDefaultSnap { Value : Integer }, default DropTypeDefaultSnap(0) };",
				"alter type DropTypeDefaultSnap { drop default };");
			yield return Case("CreateTypeSpecial",
				"create type TypeSpecialSnap { representation TypeSpecialSnap { Value : Integer } };",
				"alter type TypeSpecialSnap { create special Maximum TypeSpecialSnap(255) };");
			yield return Case("DropTypeSpecial",
				"create type DropTypeSpecialSnap { representation DropTypeSpecialSnap { Value : Integer }, special Maximum DropTypeSpecialSnap(255) };",
				"alter type DropTypeSpecialSnap { drop special Maximum };");
			yield return Case("AlterTypeTag",
				"create type TypeTagSnap { representation TypeTagSnap { Value : Integer } }; alter type TypeTagSnap alter tags { create Frontend.Width = \"5\" };",
				"alter type TypeTagSnap alter tags { alter Frontend.Width = \"10\" };");
			yield return Case("CreateSort",
				"create type SortSnap like String;",
				"create sort SortSnap using CompareText(left.value, right.value);");
			yield return Case("DropSort",
				"create type DropSortSnap like String; create sort DropSortSnap using CompareText(left.value, right.value);",
				"drop sort DropSortSnap;");
			yield return Case("AlterSort",
				"create type AlterSortSnap like String; create sort AlterSortSnap using CompareText(left.value, right.value);",
				"alter sort AlterSortSnap using CompareText(left.value, right.value);");
			yield return Case("CreateRepresentation",
				"create type RepresentationSnap { representation RepresentationSnap { Value : Integer } };",
				"alter type RepresentationSnap { create representation Doubled { Doubled : Integer read value.Value * 2 write RepresentationSnap(Doubled div 2) } selector RepresentationSnap(Doubled div 2) };");
			yield return Case("DropRepresentation",
				"create type DropRepresentationSnap { representation DropRepresentationSnap { Value : Integer }, representation Doubled { Doubled : Integer read value.Value * 2 write DropRepresentationSnap(Doubled div 2) } selector DropRepresentationSnap(Doubled div 2) };",
				"alter type DropRepresentationSnap { drop representation Doubled };");
			yield return Case("CreateConversion",
				"create type ConversionSnap { representation ConversionSnap { Value : Integer } };",
				"create conversion Integer to ConversionSnap using ConversionSnap;");
			yield return Case("DropConversion",
				"create type DropConversionSnap { representation DropConversionSnap { Value : Integer } }; create conversion Integer to DropConversionSnap using DropConversionSnap;",
				"drop conversion Integer to DropConversionSnap;");
			yield return Case("AttachOperatorToType",
				"create type TypeHandlerSnap like String; create operator TypeHandlerSnap_Validate(var AValue : TypeHandlerSnap) : Boolean begin result := false; end;",
				"attach operator TypeHandlerSnap_Validate to TypeHandlerSnap on { validate };");
			yield return Case("DetachOperatorFromType",
				"create type TypeDetachSnap like String; create operator TypeDetachSnap_Validate(var AValue : TypeDetachSnap) : Boolean begin result := false; end; attach operator TypeDetachSnap_Validate to TypeDetachSnap on { validate };",
				"detach operator TypeDetachSnap_Validate from TypeDetachSnap on { validate };");

			// Operators
			yield return Case("CreateOperator", null,
				"create operator CreateOperatorSnap() : Integer begin result := 1; end;");
			// An operator's emitted statement carries its signature, not its body, and no property of the operator object
			// reflects the body either, so a replaced body is outside the snapshot.
			yield return CaseTheSnapshotCannotSee("AlterOperator",
				"create operator AlterOperatorSnap() : Integer begin result := 1; end;",
				"alter operator AlterOperatorSnap() begin result := 2; end;");
			yield return Case("DropOperator",
				"create operator DropOperatorSnap() : Integer begin result := 1; end;",
				"drop operator DropOperatorSnap();");
			yield return Case("CreateAggregateOperator", null,
				"create aggregate operator CreateAggregateSnap(V : Integer) : Integer initialization begin end aggregation begin end finalization begin end;");
			yield return Case("DropAggregateOperator",
				"create aggregate operator DropAggregateSnap(V : Integer) : Integer initialization begin end aggregation begin end finalization begin end;",
				"drop operator DropAggregateSnap(Integer);");
			yield return Case("AlterOperatorTag",
				"create operator OperatorTagSnap() : Integer begin result := 1; end;",
				"alter operator OperatorTagSnap() alter tags { create Frontend.Title = \"Changed\" };");

			// References and catalog constraints
			yield return Case("CreateReference",
				"create table ReferenceTargetSnap { ID : Integer, key { ID } }; create table ReferenceSourceSnap { ID : Integer, Target_ID : Integer, key { ID } };",
				"create reference CreateReferenceSnap ReferenceSourceSnap { Target_ID } references ReferenceTargetSnap { ID };");
			yield return Case("DropReference",
				"create table DropReferenceTargetSnap { ID : Integer, key { ID } }; create table DropReferenceSourceSnap { ID : Integer, Target_ID : Integer, key { ID } }; create reference DropReferenceSnap DropReferenceSourceSnap { Target_ID } references DropReferenceTargetSnap { ID };",
				"drop reference DropReferenceSnap;");
			yield return Case("AlterReference",
				"create table AlterReferenceTargetSnap { ID : Integer, key { ID } }; create table AlterReferenceSourceSnap { ID : Integer, Target_ID : Integer, key { ID } }; create reference AlterReferenceSnap AlterReferenceSourceSnap { Target_ID } references AlterReferenceTargetSnap { ID };",
				"alter reference AlterReferenceSnap alter tags { create Frontend.Title = \"Changed\" };");
			yield return Case("CreateCatalogConstraint",
				"create table CatalogConstraintSourceSnap { ID : Integer, key { ID } };",
				"create constraint CreateCatalogConstraintSnap not exists (CatalogConstraintSourceSnap where ID < 0);");
			yield return Case("DropCatalogConstraint",
				"create table DropCatalogConstraintSourceSnap { ID : Integer, key { ID } }; create constraint DropCatalogConstraintSnap not exists (DropCatalogConstraintSourceSnap where ID < 0);",
				"drop constraint DropCatalogConstraintSnap;");
			yield return Case("AlterCatalogConstraint",
				"create table AlterCatalogConstraintSourceSnap { ID : Integer, key { ID } }; create constraint AlterCatalogConstraintSnap not exists (AlterCatalogConstraintSourceSnap where ID < 0);",
				"alter constraint AlterCatalogConstraintSnap not exists (AlterCatalogConstraintSourceSnap where ID < -1);");

			// Devices
			yield return Case("CreateDevice", null,
				"create device CreateDeviceSnap class \"System.MemoryDevice\";");
			yield return Case("DropDevice",
				"create device DropDeviceSnap class \"System.MemoryDevice\";",
				"drop device DropDeviceSnap;");
			yield return Case("AlterDeviceClass",
				"create device DeviceClassSnap class \"System.MemoryDevice\";",
				"alter device DeviceClassSnap alter class { create \"MaxRowCount\" = \"100\" };");
			yield return Case("AlterDeviceReconciliationMode",
				"create device DeviceModeSnap class \"System.MemoryDevice\";",
				"alter device DeviceModeSnap alter reconciliation { mode = { none } };");
			yield return Case("AlterDeviceReconciliationMaster",
				"create device DeviceMasterSnap class \"System.MemoryDevice\";",
				"alter device DeviceMasterSnap alter reconciliation { master = both };");

			// Server links. A link naming a host that does not answer is enough: none of these operators connects.
			yield return Case("CreateServer", null,
				"create server CreateServerSnap tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");
			yield return Case("AlterServerTag",
				"create server AlterServerSnap tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };",
				"alter server AlterServerSnap alter tags { alter HostName = \"elsewhere\" };");
			yield return Case("DropServer",
				"create server DropServerSnap tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };",
				"drop server DropServerSnap;");
			yield return Case("CreateServerLinkUser",
				"create server ServerLinkUserSnap tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };",
				"CreateServerLinkUser('Admin', Name('ServerLinkUserSnap'), 'Remote', 'secret');");
			yield return Case("DropServerLinkUser",
				"create server DropServerLinkUserSnap tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" }; CreateServerLinkUser('Admin', Name('DropServerLinkUserSnap'), 'Remote', 'secret');",
				"DropServerLinkUser('Admin', Name('DropServerLinkUserSnap'));");
			yield return Case("SetServerLinkUserID",
				"create server SetServerLinkUserIDSnap tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" }; CreateServerLinkUser('Admin', Name('SetServerLinkUserIDSnap'), 'Remote', 'secret');",
				"SetServerLinkUserID('Admin', Name('SetServerLinkUserIDSnap'), 'Changed');");
			yield return Case("SetServerLinkUserPassword",
				"create server SetServerLinkUserPasswordSnap tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" }; CreateServerLinkUser('Admin', Name('SetServerLinkUserPasswordSnap'), 'Remote', 'secret');",
				"SetServerLinkUserPassword('Admin', Name('SetServerLinkUserPasswordSnap'), 'changed');");

			// Event handlers
			yield return Case("AttachOperatorToTable",
				"create table AttachTableSnap { ID : Integer, key { ID } }; create operator AttachTableSnap_AfterInsert(const ARow : typeof(AttachTableSnap[])) begin end;",
				"attach operator AttachTableSnap_AfterInsert to AttachTableSnap on { after insert };");
			yield return Case("DetachOperatorFromTable",
				"create table DetachTableSnap { ID : Integer, key { ID } }; create operator DetachTableSnap_AfterInsert(const ARow : typeof(DetachTableSnap[])) begin end; attach operator DetachTableSnap_AfterInsert to DetachTableSnap on { after insert };",
				"detach operator DetachTableSnap_AfterInsert from DetachTableSnap on { after insert };");
			yield return Case("AttachOperatorToColumn",
				"create table AttachColumnSnap { ID : Integer, Name : String, key { ID } }; create operator AttachColumnSnap_Name_Default(var AValue : String) : Boolean begin result := true; AValue := \"x\"; end;",
				"attach operator AttachColumnSnap_Name_Default to Name in AttachColumnSnap on { default };");
			yield return Case("DetachOperatorFromColumn",
				"create table DetachColumnSnap { ID : Integer, Name : String, key { ID } }; create operator DetachColumnSnap_Name_Default(var AValue : String) : Boolean begin result := true; AValue := \"x\"; end; attach operator DetachColumnSnap_Name_Default to Name in DetachColumnSnap on { default };",
				"detach operator DetachColumnSnap_Name_Default from Name in DetachColumnSnap on { default };");

			// Security. Users, rights and the assignments between them and roles are rows in the catalog store with a cache
			// in front of them, not objects in Catalog, and this fixture does not reach that cache - so most of these
			// statements are outside the snapshot. The two that are not say so by being ordinary cases: a role is a catalog
			// object, and an object's owner is a dimension of its own.
			// Security
			yield return Case("CreateRole", null,
				"create role CreateRoleSnap;");
			yield return Case("DropRole",
				"create role DropRoleSnap;",
				"drop role DropRoleSnap;");
			yield return CaseTheSnapshotCannotSee("CreateRight", null,
				"CreateRight('CreateRightSnap');");
			yield return CaseTheSnapshotCannotSee("DropRight",
				"CreateRight('DropRightSnap');",
				"DropRight('DropRightSnap');");
			yield return CaseTheSnapshotCannotSee("CreateUser", null,
				"CreateUser('CreateUserSnap', 'Create User Snap', 'secret');");
			yield return CaseTheSnapshotCannotSee("DropUser",
				"CreateUser('DropUserSnap', 'Drop User Snap', 'secret');",
				"DropUser('DropUserSnap');");
			yield return CaseTheSnapshotCannotSee("SetUserName",
				"CreateUser('SetUserNameSnap', 'Before', 'secret');",
				"SetUserName('SetUserNameSnap', 'After');");
			yield return CaseTheSnapshotCannotSee("SetPassword",
				"CreateUser('SetPasswordSnap', 'Set Password Snap', 'secret');",
				"SetPassword('SetPasswordSnap', 'changed');");
			yield return CaseTheSnapshotCannotSee("AddUserToRole",
				"CreateUser('AddToRoleSnap', 'Add To Role Snap', 'secret'); create role AddToRoleSnapRole;",
				"AddUserToRole('AddToRoleSnap', 'AddToRoleSnapRole');");
			yield return CaseTheSnapshotCannotSee("RemoveUserFromRole",
				"CreateUser('RemoveFromRoleSnap', 'Remove From Role Snap', 'secret'); create role RemoveFromRoleSnapRole; AddUserToRole('RemoveFromRoleSnap', 'RemoveFromRoleSnapRole');",
				"RemoveUserFromRole('RemoveFromRoleSnap', 'RemoveFromRoleSnapRole');");
			yield return CaseTheSnapshotCannotSee("GrantRightToUser",
				"CreateUser('GrantRightSnap', 'Grant Right Snap', 'secret'); CreateRight('GrantRightSnapRight');",
				"GrantRightToUser('GrantRightSnapRight', 'GrantRightSnap');");
			yield return CaseTheSnapshotCannotSee("RevokeRightFromUser",
				"CreateUser('RevokeRightSnap', 'Revoke Right Snap', 'secret'); CreateRight('RevokeRightSnapRight'); GrantRightToUser('RevokeRightSnapRight', 'RevokeRightSnap');",
				"RevokeRightFromUser('RevokeRightSnapRight', 'RevokeRightSnap');");
			yield return CaseTheSnapshotCannotSee("RevertRightForUser",
				"CreateUser('RevertRightSnap', 'Revert Right Snap', 'secret'); CreateRight('RevertRightSnapRight'); GrantRightToUser('RevertRightSnapRight', 'RevertRightSnap');",
				"RevertRightForUser('RevertRightSnapRight', 'RevertRightSnap');");
			yield return CaseTheSnapshotCannotSee("GrantRightToRole",
				"create role GrantRightToRoleSnapRole; CreateRight('GrantRightToRoleSnapRight');",
				"GrantRightToRole('GrantRightToRoleSnapRight', 'GrantRightToRoleSnapRole');");
			yield return CaseTheSnapshotCannotSee("RevokeRightFromRole",
				"create role RevokeRightFromRoleSnapRole; CreateRight('RevokeRightFromRoleSnapRight'); GrantRightToRole('RevokeRightFromRoleSnapRight', 'RevokeRightFromRoleSnapRole');",
				"RevokeRightFromRole('RevokeRightFromRoleSnapRight', 'RevokeRightFromRoleSnapRole');");
			yield return Case("SetObjectOwner",
				"CreateUser('SetObjectOwnerSnap', 'Set Object Owner Snap', 'secret'); create table SetObjectOwnerSnapTable { ID : Integer, key { ID } };",
				"SetObjectOwner('SetObjectOwnerSnapTable', 'SetObjectOwnerSnap');");
			yield return CaseTheSnapshotCannotSee("SetRightOwner",
				"CreateUser('SetRightOwnerSnap', 'Set Right Owner Snap', 'secret'); CreateRight('SetRightOwnerSnapRight');",
				"SetRightOwner('SetRightOwnerSnapRight', 'SetRightOwnerSnap');");
			yield return Case("CreateDeviceUser",
				"CreateUser('CreateDeviceUserSnap', 'Create Device User Snap', 'secret'); create device CreateDeviceUserSnapDevice class \"System.MemoryDevice\";",
				"CreateDeviceUser('CreateDeviceUserSnap', 'CreateDeviceUserSnapDevice', 'Remote', 'secret');");
			yield return Case("SetDeviceUserID",
				"CreateUser('SetDeviceUserIDSnap', 'Set Device User ID Snap', 'secret'); create device SetDeviceUserIDSnapDevice class \"System.MemoryDevice\"; CreateDeviceUser('SetDeviceUserIDSnap', 'SetDeviceUserIDSnapDevice', 'Remote', 'secret');",
				"SetDeviceUserID('SetDeviceUserIDSnap', 'SetDeviceUserIDSnapDevice', 'Changed');");
			yield return Case("SetDeviceUserPassword",
				"CreateUser('SetDeviceUserPasswordSnap', 'Set Device User Password Snap', 'secret'); create device SetDeviceUserPasswordSnapDevice class \"System.MemoryDevice\"; CreateDeviceUser('SetDeviceUserPasswordSnap', 'SetDeviceUserPasswordSnapDevice', 'Remote', 'secret');",
				"SetDeviceUserPassword('SetDeviceUserPasswordSnap', 'SetDeviceUserPasswordSnapDevice', 'changed');");
			yield return Case("SetDeviceUserConnectionParameters",
				"CreateUser('SetDeviceUserParametersSnap', 'Set Device User Parameters Snap', 'secret'); create device SetDeviceUserParametersSnapDevice class \"System.MemoryDevice\"; CreateDeviceUser('SetDeviceUserParametersSnap', 'SetDeviceUserParametersSnapDevice', 'Remote', 'secret');",
				"SetDeviceUserConnectionParameters('SetDeviceUserParametersSnap', 'SetDeviceUserParametersSnapDevice', 'x=1');");
			yield return Case("DropDeviceUser",
				"CreateUser('DropDeviceUserSnap', 'Drop Device User Snap', 'secret'); create device DropDeviceUserSnapDevice class \"System.MemoryDevice\"; CreateDeviceUser('DropDeviceUserSnap', 'DropDeviceUserSnapDevice', 'Remote', 'secret');",
				"DropDeviceUser('DropDeviceUserSnap', 'DropDeviceUserSnapDevice');");

			// Session objects. These live in the fixture's per-test session, so each test starts with none of them.
			yield return Case("CreateSessionTable", null,
				"create session table CreateSessionTableSnap { ID : Integer, key { ID } };");
			yield return Case("DropSessionTable",
				"create session table DropSessionTableSnap { ID : Integer, key { ID } };",
				"drop table DropSessionTableSnap;");
			yield return Case("CreateSessionView",
				"create table SessionViewSourceSnap { ID : Integer, key { ID } };",
				"create session view CreateSessionViewSnap SessionViewSourceSnap;");
			yield return Case("CreateSessionOperator", null,
				"create session operator CreateSessionOperatorSnap() : Integer begin result := 1; end;");
			yield return Case("DropSessionOperator",
				"create session operator DropSessionOperatorSnap() : Integer begin result := 1; end;",
				"drop operator DropSessionOperatorSnap();");
		}

		private static TestCaseData Case(string name, string setup, string statement)
		{
			return new TestCaseData(setup, statement, true).SetName(name);
		}

		/// <summary>
		/// A case whose statement really does change the catalog but changes nothing any dimension of the snapshot
		/// reaches, so that only the rollback's effect on the <em>rest</em> of the catalog is checked. Every one of these
		/// is a dimension the snapshot does not have yet, not a statement that does nothing - see
		/// <c>debt-snapshot-cannot-see-users-rights-and-operator-bodies</c>, which is the work that would retire the
		/// distinction. Declared case by case rather than inferred, so that a statement which stops changing anything
		/// cannot hide among them.
		/// </summary>
		private static TestCaseData CaseTheSnapshotCannotSee(string name, string setup, string statement)
		{
			return new TestCaseData(setup, statement, false).SetName(name);
		}

		#endregion

		#region The snapshot

		/// <summary>
		/// The running server's in-memory catalog as entry key to value, read from memory alone. One entry per dimension
		/// per object, so that a difference names the object and the dimension it is in rather than a position in one
		/// very long string.
		/// </summary>
		private Dictionary<string, string> Snapshot()
		{
			Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (Schema.Object objectValue in Instance.Server.Catalog)
			{
				// Name and ID both, so that an object dropped and recreated under the same name is a different entry.
				string key = String.Format("{0} #{1}", objectValue.Name, objectValue.ID);

				// The object's own D4 statement, which carries its metadata tags, class definition, keys, orders,
				// constraints, representations and defaults, and reaches its children - which are not in the catalog
				// themselves. Emitted here rather than through Script(), whose object selection reads the store.
				result["emit:" + key] = Emit(objectValue);

				// The sensitive dimension: the state derived from a definition rather than the definition itself.
				// Reflected rather than listed by hand - a hand-written list is a list of the defects already known.
				result["props:" + key] = ScalarProperties(objectValue);

				// The children the parent's own statement leaves out, each emitted on one line of its own.
				Schema.TableVar tableVar = objectValue as Schema.TableVar;
				if (tableVar != null)
					result["children:" + key] = TableVarChildren(tableVar);

				Schema.ScalarType scalarType = objectValue as Schema.ScalarType;
				if (scalarType != null)
					result["children:" + key] = ScalarTypeChildren(scalarType);

				Schema.CatalogObject catalogObject = objectValue as Schema.CatalogObject;
				if (catalogObject != null)
					result["owner:" + key] = catalogObject.Owner == null ? "<none>" : catalogObject.Owner.ID;

				Schema.ServerLink serverLink = objectValue as Schema.ServerLink;
				if (serverLink != null)
					result["linkusers:" + key] = LinkUsers(serverLink);

				Schema.Device device = objectValue as Schema.Device;
				if (device != null)
					result["deviceusers:" + key] = DeviceUsers(device);
			}

			foreach (Schema.LoadedLibrary library in Instance.Server.Catalog.LoadedLibraries)
			{
				List<string> required = new List<string>();
				foreach (Schema.LoadedLibrary requiredLibrary in library.RequiredLibraries)
					required.Add(requiredLibrary.Name);
				result["loadedlibrary:" + library.Name] = Sorted(required);
			}

			// The session's own name mappings, which nothing else in the snapshot sees: this is the dimension a
			// rolled-back session operator was left behind in.
			ServerSession session = (ServerSession)Session;
			result["sessionobjects"] = SessionEntries(session.SessionObjects);
			result["sessionoperators"] = SessionEntries(session.SessionOperators);

			return result;
		}

		/// <summary>
		/// The object's create statement as D4 text. An object that cannot emit records its error as the entry's value,
		/// so that one such object does not blind the whole snapshot.
		/// </summary>
		private static string Emit(Schema.Object objectValue)
		{
			try
			{
				Statement statement = objectValue.EmitStatement(EmitMode.ForCopy);
				return statement == null ? "<none>" : new D4TextEmitter(EmitMode.ForCopy).Emit(statement);
			}
			catch (Exception exception)
			{
				return String.Format("<threw {0}: {1}>", exception.GetType().Name, exception.Message);
			}
		}

		/// <summary>
		/// A table variable's constraints on the whole row, which its own statement does not carry: measured on this
		/// server, <c>create table</c> emits each column with its default and its constraints, but a constraint over the
		/// row is emitted as a separate <c>alter table</c> by whoever scripts the catalog, because its expression can name
		/// the table itself. Without this a dropped or altered row constraint changed nothing the snapshot could see.
		/// </summary>
		private static string TableVarChildren(Schema.TableVar tableVar)
		{
			List<string> lines = new List<string>();
			foreach (Schema.TableVarConstraint constraint in tableVar.Constraints)
				lines.Add("constraint " + constraint.Name + ": " + OneLine(Emit(constraint)));
			return Sorted(lines);
		}

		/// <summary>
		/// A scalar type's constraints and its default, which its own statement does not carry either - measured, it
		/// carries only the representations. These are what a cancelled <c>alter type</c> strands, so the dimension exists
		/// for the same reason as <see cref="TableVarChildren"/>. Specials are left out: each one creates a selector and a
		/// comparer that are catalog objects in their own right, so the snapshot already sees them.
		/// </summary>
		private static string ScalarTypeChildren(Schema.ScalarType scalarType)
		{
			List<string> lines = new List<string>();
			foreach (Schema.ScalarTypeConstraint constraint in scalarType.Constraints)
				lines.Add("constraint " + constraint.Name + ": " + OneLine(Emit(constraint)));
			if (scalarType.Default != null)
				lines.Add("default: " + OneLine(Emit(scalarType.Default)));
			return Sorted(lines);
		}

		// One line per child, so that the difference report can pick out the child that changed.
		private static string OneLine(string value)
		{
			return value.Replace("\r", String.Empty).Replace("\n", " ");
		}

		/// <summary>
		/// Every readable public instance property of simple type, as sorted <c>Name=value</c> lines. A property that
		/// throws records that it threw: the value of this dimension is in the properties that can be read, and one that
		/// cannot must not hide them. No property is excluded: measured on a bare server, none changed between two
		/// consecutive snapshots, so none is lazily computed or time-dependent. Exclude one by name here, with the
		/// reason, if a property ever turns out to start something or to change on first read - rather than dropping
		/// this dimension, which is what caught the three defects the prerequisite tickets fixed.
		/// </summary>
		private static string ScalarProperties(object objectValue)
		{
			List<string> lines = new List<string>();
			foreach (PropertyInfo property in objectValue.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (!property.CanRead || (property.GetIndexParameters().Length > 0) || !IsSimpleType(property.PropertyType))
					continue;
				string value;
				try
				{
					object propertyValue = property.GetValue(objectValue, null);
					value = propertyValue == null ? "<null>" : Convert.ToString(propertyValue, CultureInfo.InvariantCulture);
				}
				catch (Exception exception)
				{
					value = String.Format("<threw {0}>", exception.GetType().Name);
				}
				lines.Add(property.Name + "=" + value);
			}
			return Sorted(lines);
		}

		private static bool IsSimpleType(Type type)
		{
			Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
			return
				underlyingType.IsPrimitive
					|| underlyingType.IsEnum
					|| (underlyingType == typeof(string))
					|| (underlyingType == typeof(decimal))
					|| (underlyingType == typeof(DateTime))
					|| (underlyingType == typeof(TimeSpan))
					|| (underlyingType == typeof(Guid));
		}

		// Sorted, because ServerLink.Users is a dictionary: a mapping removed and put back by an undo can change
		// enumeration order without changing meaning.
		private static string LinkUsers(Schema.ServerLink serverLink)
		{
			List<string> lines = new List<string>();
			foreach (Schema.ServerLinkUser user in serverLink.Users.Values)
				lines.Add(String.Format("{0} -> {1} / {2}", user.UserID, user.ServerLinkUserID, user.ServerLinkPassword));
			return Sorted(lines);
		}

		// Device.Users is a dictionary too, and a device user carries settings of its own (user id, password,
		// connection parameters), so each is reflected rather than named.
		private static string DeviceUsers(Schema.Device device)
		{
			List<string> lines = new List<string>();
			foreach (KeyValuePair<string, Schema.DeviceUser> entry in device.Users)
				lines.Add(entry.Key + " -> " + ScalarProperties(entry.Value).Replace("\n", "; "));
			return Sorted(lines);
		}

		private static string SessionEntries(Schema.Objects objects)
		{
			List<string> lines = new List<string>();
			for (int index = 0; index < objects.Count; index++)
			{
				Schema.SessionObject sessionObject = objects[index] as Schema.SessionObject;
				lines.Add(sessionObject == null ? objects[index].Name : sessionObject.Name + " -> " + sessionObject.GlobalName);
			}
			return Sorted(lines);
		}

		private static string Sorted(List<string> lines)
		{
			lines.Sort(StringComparer.Ordinal);
			return String.Join("\n", lines);
		}

		#endregion

		#region The difference report

		/// <summary>
		/// Compares two snapshots by key and fails naming what differed. An equality assertion on two 1.4 MB strings
		/// would not be a usable failure: what the reader needs is the object and the property.
		/// </summary>
		/// <summary>
		/// Fails when two snapshots are equal. The guard on the case matrix itself: a case is only worth running while
		/// its statement still moves something the snapshot reads.
		/// </summary>
		private static void AssertDifferent(Dictionary<string, string> before, Dictionary<string, string> after, string what)
		{
			foreach (KeyValuePair<string, string> entry in before)
			{
				string afterValue;
				if (!after.TryGetValue(entry.Key, out afterValue) || !String.Equals(afterValue, entry.Value, StringComparison.Ordinal))
					return;
			}
			foreach (KeyValuePair<string, string> entry in after)
				if (!before.ContainsKey(entry.Key))
					return;

			Assert.Fail(String.Format(
				"Nothing the snapshot reads changed when this ran, so the comparison after the rollback would pass whatever the rollback did: {0}"
					+ "\n  If the statement has stopped changing the catalog, that is the defect - fix the statement, not this check."
					+ "\n  If the snapshot has stopped reading what it changes, give the snapshot that dimension, or declare the case"
					+ "\n  with CaseTheSnapshotCannotSee and say there which dimension is missing.",
				what));
		}

		private static void AssertSame(Dictionary<string, string> before, Dictionary<string, string> after, string what)
		{
			List<string> differences = new List<string>();

			foreach (KeyValuePair<string, string> entry in before)
			{
				string afterValue;
				if (!after.TryGetValue(entry.Key, out afterValue))
					differences.Add(String.Format("gone after: {0}\n    was: {1}", entry.Key, Truncate(entry.Value)));
				else if (!String.Equals(afterValue, entry.Value, StringComparison.Ordinal))
					differences.Add(String.Format("changed: {0}{1}", entry.Key, Changes(entry.Value, afterValue)));
			}

			foreach (KeyValuePair<string, string> entry in after)
				if (!before.ContainsKey(entry.Key))
					differences.Add(String.Format("new after: {0}\n    now: {1}", entry.Key, Truncate(entry.Value)));

			if (differences.Count == 0)
				return;

			differences.Sort(StringComparer.Ordinal);

			StringBuilder message = new StringBuilder();
			message.AppendFormat("The in-memory catalog is not as it was before {0}: {1} differences.", what, differences.Count);
			for (int index = 0; index < Math.Min(differences.Count, MaxReportedDifferences); index++)
			{
				message.AppendLine();
				message.Append("  ");
				message.Append(differences[index]);
			}
			if (differences.Count > MaxReportedDifferences)
			{
				message.AppendLine();
				message.AppendFormat("  ... and {0} more.", differences.Count - MaxReportedDifferences);
			}
			Assert.Fail(message.ToString());
		}

		/// <summary>
		/// What changed between two values of one entry. A value is a list of lines (one property, one link user, one
		/// session mapping each), so the differing lines are what the reader needs; a one-line value - an emitted
		/// statement - has no lines to pick from and is shown whole.
		/// </summary>
		private static string Changes(string beforeValue, string afterValue)
		{
			string[] beforeLines = beforeValue.Split('\n');
			string[] afterLines = afterValue.Split('\n');
			if ((beforeLines.Length <= 1) && (afterLines.Length <= 1))
				return String.Format("\n    was: {0}\n    now: {1}", Truncate(beforeValue), Truncate(afterValue));

			HashSet<string> onlyBefore = new HashSet<string>(beforeLines, StringComparer.Ordinal);
			onlyBefore.ExceptWith(afterLines);
			HashSet<string> onlyAfter = new HashSet<string>(afterLines, StringComparer.Ordinal);
			onlyAfter.ExceptWith(beforeLines);

			List<string> lines = new List<string>();
			foreach (string line in onlyBefore)
				lines.Add("    was: " + Truncate(line));
			foreach (string line in onlyAfter)
				lines.Add("    now: " + Truncate(line));
			lines.Sort(StringComparer.Ordinal);
			return "\n" + String.Join("\n", lines);
		}

		private static string Truncate(string value)
		{
			if (value == null)
				return "<null>";
			string oneLine = value.Replace("\r", String.Empty).Replace("\n", " \\n ");
			return oneLine.Length <= MaxReportedValueLength ? oneLine : oneLine.Substring(0, MaxReportedValueLength) + " ...";
		}

		#endregion
	}
}
