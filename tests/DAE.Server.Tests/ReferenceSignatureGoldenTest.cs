/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>Why a published reference page and the running catalog disagree, and what was done about it.</summary>
	public enum ReferenceVerdict
	{
		/// <summary>
		/// The difference was already there in 3.1, before this port: the page records the catalog the 2007 help file was
		/// published from, and the product moved on before the modernization branch started. The page is left as the
		/// published record. This is the verdict that says the port changed nothing.
		/// </summary>
		PreThreeOne,

		/// <summary>The page publishes something no catalog has had, so the page is wrong and is corrected by this ticket.</summary>
		Stale,

		/// <summary>
		/// The signature changed between 3.x and this port. The page is updated to the generated text and
		/// docs/migration-notes.md gains an entry in the same commit.
		/// </summary>
		Changed,

		/// <summary>This port changed a signature it should not have. The page is left alone; the entry names the ticket filed for it.</summary>
		Defect,

		/// <summary>Not yet classified. While any entry carries this verdict the pass reports a warning naming the follow-up ticket.</summary>
		Unread
	}

	/// <summary>
	/// The families the measured differences fall into. Each is a statement about where a difference comes from, made
	/// once here rather than repeated on every entry; <see cref="ReferenceSignatureGoldenTest"/>'s difference table tags
	/// each entry with the families that explain it.
	/// </summary>
	public enum ReferenceDifferenceFamily
	{
		/// <summary>
		/// The page's static-tag list is older than the product's. Measured: System.Date carries all three of its tags in
		/// the 2008 open-source import (361ebd83) where the page shows one, and System.ToByte(String) gained
		/// DAE.IsUniquePreserving during 3.x (absent at 361ebd83, present at 418a5d45, the commit this branch starts
		/// from). The engine also writes the tag name it reads, so the source's DAE.IsRemotabe typo on System.Evaluate
		/// is emitted DAE.IsRemotable.
		/// </summary>
		TagsOlderThanTheProduct,

		/// <summary>
		/// The untyped list type specifier is emitted "list" where the 2007 tool wrote "generic list". The parser accepts
		/// both and builds the same specifier with IsGeneric set (Parser.ListTypeSpecifier), and the emitter method that
		/// writes "list" is byte-identical at 418a5d45, so this predates the port and denotes the same type.
		/// </summary>
		ListSpecifierSpelling,

		/// <summary>
		/// A D4-bodied operator's declaration carries the source text the engine stored, where the 2007 tool emitted the
		/// compiled tree - which was fully qualified and prefixed every identifier with a dot (".result :=" against
		/// "result :="). The coverage script OperatorSourceText.d4 pins the stored-text behaviour.
		/// </summary>
		OperatorBodyIsSourceText,

		/// <summary>
		/// The catalog has overloads the 2007 help file did not list. Every one of them is in the 2008 import or in 3.1:
		/// SystemCatalog.d4 at 418a5d45 differs from today's only by the two iRedefine operators that
		/// debt-unreachable-redefine-nodes removed, and neither of those has a page.
		/// </summary>
		OverloadsAddedAfterPublication,

		/// <summary>
		/// A scalar type's Script output is a catalog copy script - a bare "create type" followed by one "alter type" per
		/// representation - where the 2007 tool emitted one "create type" with its representations inline. The split is
		/// deliberate and is not about any particular type: Catalog.EmitChildren moves a representation out of the create
		/// statement when it has external dependencies, which every System type's representations have, because their
		/// properties are other System types. So no shipped surface emits the published shape. DataTypes.d4 is
		/// byte-identical to 418a5d45, so the types themselves have not changed.
		/// </summary>
		CopyScriptShape,

		/// <summary>The page carries a different operator's declarations altogether - a conversion error, not a catalog difference.</summary>
		WrongOperatorOnThePage
	}

	/// <summary>One recorded disagreement between a published page's declaration block and the running catalog.</summary>
	public sealed class ReferenceDifference
	{
		public ReferenceDifference(string blockPath, ReferenceVerdict verdict, params ReferenceDifferenceFamily[] families)
		{
			BlockPath = blockPath;
			Verdict = verdict;
			Families = families;
		}

		/// <summary>The block this entry is about, as <see cref="ReferenceBlock.Path"/> spells it.</summary>
		public string BlockPath { get; }

		public ReferenceVerdict Verdict { get; }

		/// <summary>The families that explain this difference; at least one.</summary>
		public ReferenceDifferenceFamily[] Families { get; }

		public override string ToString()
		{
			return String.Format("{0} ({1}: {2})", BlockPath, Verdict, String.Join(", ", Families));
		}
	}

	/// <summary>One declaration block of one published reference page: what it is called, and what the catalog is asked for.</summary>
	public sealed class ReferenceBlock
	{
		public ReferenceBlock(string folder, string objectName, string heading)
		{
			Folder = folder;
			ObjectName = objectName;
			Heading = heading;
		}

		/// <summary>"operators" or "types" - the folder under docs/reference/system-library and under the golden tree.</summary>
		public string Folder { get; }

		/// <summary>The catalog object the page is named after: an operator name, or a scalar type name.</summary>
		public string ObjectName { get; }

		/// <summary>The bold heading the block sits under: "Declarations" or "Type Declaration".</summary>
		public string Heading { get; }

		/// <summary>The page this block is on, below docs/reference/system-library, forward-slashed.</summary>
		public string PageRelativePath
		{
			get { return String.Format("{0}/{1}.md", Folder, ObjectName); }
		}

		/// <summary>The key the recorded difference table is written in, and what a message quotes.</summary>
		public string Path
		{
			get { return String.Format("{0}#{1}", PageRelativePath, Heading); }
		}

		/// <summary>
		/// This block's golden below the golden tree, forward-slashed. Keyed by what is generated rather than by the page
		/// it is on, so that a scalar type's two blocks get a golden each - its create statement under types, and the
		/// operators named after it under operators - and so that a type page and the operator page of the same name
		/// (System.Date, System.DateTime, System.Time, System.UserID all have both) compare against one file.
		/// </summary>
		public string GoldenRelativePath
		{
			get
			{
				return
					String.Format
					(
						"{0}/{1}{2}",
						Heading == ReferenceSignatureGoldenTest.TypeDeclarationHeading ? ReferenceSignatureGoldenTest.TypesFolder : ReferenceSignatureGoldenTest.OperatorsFolder,
						ObjectName,
						ReferenceSignatureGoldenTest.GoldenExtension
					);
			}
		}

		public override string ToString()
		{
			return Path;
		}
	}

	/// <summary>
	/// The reference half of WS8: every signature the 3.x documentation publishes is compared with the one a running
	/// 4.0 catalog reports, so that a disagreement is a reviewable file and a recorded verdict rather than a wrong page
	/// a customer finds. It also <em>writes</em> the one part of that reference the 3.x documentation never
	/// published - the system tables - which is a different job from comparing one, and
	/// <see cref="EverySystemTableHasAGeneratedPage"/> carries its own account of it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Every operator page under docs/reference/system-library/operators carries a <b>Declarations</b> block holding
	/// that operator's D4 signatures, and every page under types carries a <b>Type Declaration</b> block and usually a
	/// <b>Declarations</b> block too. Those blocks are the 3.x published record, extracted from the 2007 help file by
	/// docs-reference-tooling because the DocBook sources had placeholders where the signatures belong. An operator
	/// signature is a compatibility surface this port must not have changed, so the comparison is evidence about the
	/// port; until this pass there was no way to see a disagreement at all.
	/// </para>
	/// <para>
	/// The generator is one D4 query per object kind over the shipped System.Script operator, which emits a catalog
	/// object's declaration through D4TextEmitter. No new D4 operator, assembly or class registration is needed, so an
	/// administrator can ask their own catalog the same question. The parked DocBook generator in
	/// legacy/libraries/Documentation is deliberately left parked; the plan's WS8 section records that decision and the
	/// measurements behind it.
	/// </para>
	/// <para>
	/// The server carries no library at all - an empty Libraries list and an empty LibrariesToRegister list - so the
	/// System library's operator and type set is a function of the engine alone. That is what makes "the diff is clean"
	/// mean something, and it is also why no deployment's library names can reach a golden: nothing but this
	/// repository's own built-in catalog is ever loaded.
	/// </para>
	/// <para>
	/// <b>Three things are asserted.</b> First, the generated text equals its golden character for character: a change
	/// to a documented signature anywhere in the engine then becomes a reviewable diff in a file. Second, the golden
	/// equals the page's block after normalization, unless the difference table records that block with a verdict - so a
	/// difference that has been looked at is green and one that has not is red. Third, every recorded set agrees with
	/// the tree in both directions, so neither a page that escapes the comparison nor an entry that no longer matches
	/// anything can sit here unnoticed.
	/// </para>
	/// <para>
	/// <b>The normalizer, and why the second assertion needs one.</b> The committed blocks cannot be compared with the
	/// emitter's output character for character. The emitter indents with tabs and ends lines with CRLF (TextEmitter
	/// .Indent and .NewLine); no page under operators contains a single tab - measured with grep over all 322 files -
	/// and the pages are LF, because the 3.x text reached them through the help file, a DocBook programlisting and
	/// pandoc, which expanded the tabs at tab stops. That is why System.Abs has one space before "class" and
	/// System.Execute has four before each tag, from the same emitter at different nesting depths. So the second
	/// assertion compares text with line endings made LF, tabs expanded, runs of horizontal whitespace collapsed to one
	/// space, each line trimmed and blank lines dropped, <em>and nothing else</em>: no normalizing of case, punctuation,
	/// operand names or type names, which are the content. The normalizer exists because of that round trip and not
	/// because the two forms are allowed to drift - the first assertion is the exact one.
	/// </para>
	/// <para>
	/// <b>What was measured, and the conclusion.</b> Of the 290 operator pages with a declarations block, 214 agree with
	/// the running catalog after normalization and 76 do not; of the 19 type pages with a declarations block, 15 agree;
	/// and all 19 published type declarations differ. Every one of those differences was traced to a change that had
	/// already happened by 418a5d45, the commit this branch starts from - see <see cref="ReferenceDifferenceFamily"/>
	/// for the five families and the measurement behind each - except System.RequiredObjects, whose page carried
	/// System.RequiredLibraries' declarations and is corrected here. <b>No documented System operator signature was
	/// changed by this port.</b> The one catalog object this port did remove, iRedefine, has no page and is already in
	/// docs/migration-notes.md.
	/// </para>
	/// <para>
	/// NOTE: the pages are deliberately left as the 3.x published record rather than refreshed to the generated text,
	/// which is what the plan's WS8 section says they are for: overwriting them would compare 4.0 against 4.0 on the
	/// next run and prove nothing. The 4.0 record is the golden tree beside them. If the project ever decides the
	/// reference should track the product instead, the 19 CopyScriptShape entries and the
	/// OverloadsAddedAfterPublication entries are what to revisit, and the goldens are the text to revisit them with.
	/// </para>
	/// </remarks>
	// NOTE: two passes with different comparison rules live here - block-scoped against the published pages, whole-file
	// against the generated ones - in about 1,500 lines (wc -l). One coherent subject, what the reference says against
	// what the catalog says, so it was left whole; a third pass is the point at which to move the generated-page
	// machinery into its own fixture, taking Rows, Trim, Report, FirstDifference and the update variable with it.
	// Not [Parallelizable]: this fixture starts a server, and two engines in one process share the engine's static state
	// (see the note on TestServerInstance).
	[TestFixture]
	public class ReferenceSignatureGoldenTest : InProcessTestFixture
	{
		/// <summary>Set to 1 to write the goldens instead of comparing them; see AGENTS.md "Build and test".</summary>
		public const string UpdateVariable = "DATAPHOR_UPDATE_REFERENCE_GOLDENS";

		/// <summary>The extension a golden carries: these files hold D4 and nothing should mistake them for anything else.</summary>
		public const string GoldenExtension = ".d4";

		/// <summary>This pass's own tree below <see cref="TestEnvironment.GoldensDirectory"/>.</summary>
		public const string GoldensFolderName = "reference/system-library";

		/// <summary>The operator inventory's file name in the golden tree.</summary>
		public const string InventoryFileName = "operator-inventory.txt";

		/// <summary>The reference pages this pass reads, below the repository root.</summary>
		private const string PagesRelativePath = "docs/reference/system-library";

		/// <summary>The heading of the block holding an object's operator declarations.</summary>
		public const string DeclarationsHeading = "Declarations";

		/// <summary>The heading of the block holding a scalar type's own create statement.</summary>
		public const string TypeDeclarationHeading = "Type Declaration";

		public const string OperatorsFolder = "operators";
		public const string TypesFolder = "types";

		/// <summary>The folder of generated system table pages, below the pages directory.</summary>
		public const string TablesFolder = "tables";

		/// <summary>The generated section index of the tables folder.</summary>
		public const string TablesIndexFileName = "README.md";

		/// <summary>The Developer's Guide chapter a generated table page sends a reader to, from inside the tables folder.</summary>
		private const string CatalogChapterPath = "../../../developers-guide/d4-catalog.md";

		/// <summary>
		/// The pages that carry no declarations block, and why. These are absences, not disagreements: a page that loses
		/// or gains a block without this list changing fails the pass.
		/// </summary>
		/// <remarks>
		/// The first six operators and the type were added to the DocBook after the 2007 help file was published, so no
		/// signature was ever published for them (plan, WS8, "Design notes, the legacy reference"); System.iXor was
		/// converted from its single 2004 file, which had no signature block. A golden is generated for each of them
		/// anyway, so that the next reader can see what the engine says.
		/// </remarks>
		private static readonly string[] PagesWithNoDeclarations =
		[
			"operators/System.ActualDate.md",
			"operators/System.ActualDateTime.md",
			"operators/System.ActualTime.md",
			"operators/System.IsNotNil.md",
			"operators/System.MonthsBetween.md",
			"operators/System.YearsBetween.md",
			"operators/System.iXor.md",
			"types/System.GroupName.md"
		];

		/// <summary>
		/// The documented objects the catalog does not have, with the reason each. A page named here gets no golden and
		/// no comparison; a page not named here whose object is missing fails the pass.
		/// </summary>
		/// <remarks>
		/// System.GroupName is the only one. Its "create type GroupName" is inside a block comment in
		/// src/engine/DAE/Schema/SystemCatalog.d4 and is equally commented out at 418a5d45, so the type has not existed
		/// since before 3.1 - it went with the pre-3.0 group-based security the plan records. Nothing is owed to
		/// docs/migration-notes.md for it: that file records what this release changes, and this release did not change
		/// it.
		/// </remarks>
		private static readonly string[] DocumentedObjectsNotInTheCatalog =
		[
			"System.GroupName"
		];

		/// <summary>
		/// The System library's scalar types that no page documents, each one an internal type the engine uses to carry a
		/// value across the catalog or the library machinery rather than one a D4 script writes a literal of. The 2007
		/// reference documented none of them. The operator side of the same gap is the inventory file, which is a line
		/// per name rather than a list here because there are 301 of them.
		/// </summary>
		private static readonly string[] CatalogTypesWithNoPage =
		[
			"System.FileReference",
			"System.LibraryDescriptor",
			"System.LibraryReference",
			"System.Scalar"
		];

		/// <summary>
		/// The System library's base tables, as the catalog of a server carrying no library reports them. Held against
		/// the catalog in both directions, so that a table arriving or leaving is a line in this list for a reviewer to
		/// read, rather than only a page that appears or disappears under docs/ on an update run.
		/// </summary>
		/// <remarks>
		/// Seven more tables are declared in src/engine/DAE/Schema/SystemCatalog.d4 and are deliberately not here. Six -
		/// SourceDerivedReferences, TargetDerivedReferences, DerivedReferences, DerivedReferenceColumns,
		/// DerivedReferenceParents and DerivedReferenceDerivedParents - are inside a block comment there, as
		/// System.GroupName's create type is in DataTypes.d4, so no catalog has had them since before 3.1. The seventh,
		/// Generators, is created by the body of EnsureGenerators rather than by the script, and in the calling library's
		/// own namespace rather than in System, so a bare engine has no such table to document.
		/// </remarks>
		private static readonly string[] SystemTables =
		[
			"System.ApplicationTransactionOperatorMaps",
			"System.ApplicationTransactionOperatorNameMaps",
			"System.ApplicationTransactionTableMaps",
			"System.ApplicationTransactions",
			"System.BaseTableVars",
			"System.CatalogConstraints",
			"System.CatalogObjects",
			"System.Connections",
			"System.DerivedTableVars",
			"System.DeviceOperators",
			"System.DeviceScalarTypes",
			"System.DeviceSessions",
			"System.DeviceUsers",
			"System.Devices",
			"System.Libraries",
			"System.LibraryFileEnvironments",
			"System.LibraryFiles",
			"System.LibraryOwners",
			"System.LibraryRequisites",
			"System.LibrarySettings",
			"System.LibraryVersions",
			"System.LoadedLibraries",
			"System.ObjectDependencies",
			"System.Objects",
			"System.Operators",
			"System.Plans",
			"System.Processes",
			"System.References",
			"System.RegisteredAssemblies",
			"System.RegisteredClasses",
			"System.RemoteSessions",
			"System.Rights",
			"System.RoleRightAssignments",
			"System.Roles",
			"System.ScalarTypes",
			"System.Scripts",
			"System.ServerLinkUsers",
			"System.ServerLinks",
			"System.ServerSettings",
			"System.SessionCatalogObjects",
			"System.Sessions",
			"System.Sorts",
			"System.TableDee",
			"System.TableDum",
			"System.TableVars",
			"System.UserRightAssignments",
			"System.UserRoles",
			"System.Users"
		];

		/// <summary>
		/// The System library's views. Which table variables are views is read from the DerivedTableVars system table
		/// rather than from the emitted text, so the page says what the catalog says; this list is held against that
		/// table in both directions, so a table becoming a view is a reviewable change too.
		/// </summary>
		private static readonly string[] SystemViews =
		[
			"System.AttachLibraries",
			"System.AttachLibrary",
			"System.DetachLibrary",
			"System.DevicesNameAndLibrary",
			"System.LibraryView",
			"System.ListLogs",
			"System.ViewLog"
		];

		/// <summary>
		/// The recorded disagreements. An entry is permission for one block to differ, not a requirement that it does:
		/// an entry whose block now agrees fails this pass, so the table drains as pages are corrected, and a block that
		/// differs with no entry fails with its difference, so a new one cannot slip in unseen.
		/// </summary>
		/// <remarks>
		/// This is the port evidence. Every entry but one is <see cref="ReferenceVerdict.PreThreeOne"/>, each tagged with
		/// the families of <see cref="ReferenceDifferenceFamily"/> that explain it - which is the finding: the 3.x
		/// documentation records the catalog of 2007 and the product moved on before this port began.
		/// </remarks>
		private static readonly ReferenceDifference[] Differences =
		[
			// Overloads the 2007 help file did not list - the Money and Long arms of the arithmetic and aggregate operators.
			new ReferenceDifference("operators/System.Abs.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.Avg.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.Ceiling.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.Floor.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.Frac.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.Max.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.Min.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.Sum.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.Truncate.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),
			new ReferenceDifference("operators/System.iDivision.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication),

			// Overloads the 2007 help file did not list, on a page that also spells the untyped list type the old way.
			new ReferenceDifference("operators/System.IndexOf.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication, ReferenceDifferenceFamily.ListSpecifierSpelling),
			new ReferenceDifference("operators/System.LastIndexOf.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication, ReferenceDifferenceFamily.ListSpecifierSpelling),
			new ReferenceDifference("operators/System.ToList.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OverloadsAddedAfterPublication, ReferenceDifferenceFamily.ListSpecifierSpelling),

			// The untyped list type spelled "generic list" on the page and "list" by the emitter.
			new ReferenceDifference("operators/System.Add.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling),
			new ReferenceDifference("operators/System.Clear.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling),
			new ReferenceDifference("operators/System.Count.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling),
			new ReferenceDifference("operators/System.ToTable.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling),
			new ReferenceDifference("operators/System.iEqual.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling),
			new ReferenceDifference("operators/System.iIn.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling),
			new ReferenceDifference("operators/System.iIndexer.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling),

			// The same, where the catalog script also renamed the operator's own tag after the help file was published:
			// DAE.IsFunctional on the page, DAE.IsLocallyFunctional at 361ebd83, DAE.Functional at 418a5d45 and today.
			// Nothing in the tree reads either of the later two names, so the characteristic those three operators declare
			// has not been applied since at least 2008 - a defect of its own, owned by the backlog ticket
			// debt-misspelled-metadata-tags-are-silently-ignored, which also covers the DAE.IsRemotabe typo on
			// System.Evaluate below and the check that would have caught both.
			new ReferenceDifference("operators/System.Insert.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.Remove.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.RemoveAt.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.ListSpecifierSpelling, ReferenceDifferenceFamily.TagsOlderThanTheProduct),

			// Static tags the product has and the 2007 help file did not show.
			new ReferenceDifference("operators/System.Date.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.DateTime.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.DependentLibraries.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.DependentObjects.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.DeviceUserExists.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.Evaluate.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.GetDefaultDeviceName.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.GetLibraryDescriptor.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.IsGenerated.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.IsSystem.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.LibraryName.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.LoadUpgrade.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ObjectDescription.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ObjectDisplayName.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ObjectID.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ObjectMetaData.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ObjectName.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ProcessID.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.Reconcile.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.RequiredLibraries.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.RightExists.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ServerName.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.SessionID.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.SetDefaultDeviceName.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.StartProcess.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.SubString.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.Time.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToByte.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToDate.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToDateTime.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToDecimal.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToGuid.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToInteger.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToLong.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToMoney.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToShort.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToString.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToTime.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.ToTimeSpan.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.UpgradeVersions.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.UserExists.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.UserHasRight.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.UserName.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("operators/System.iCompare.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("types/System.Date.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("types/System.DateTime.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("types/System.Time.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),

			// System.UserID gained a type constraint after the help file was published, so its generated selector is the
			// validating one; the nilary UserID() overload on the operator page is the tag family above.
			new ReferenceDifference("operators/System.UserID.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),
			new ReferenceDifference("types/System.UserID.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.TagsOlderThanTheProduct),

			// Operators with a D4 body, whose declaration now carries the stored source text.
			new ReferenceDifference("operators/System.Concat.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OperatorBodyIsSourceText),
			new ReferenceDifference("operators/System.CreateGenerators.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OperatorBodyIsSourceText),
			new ReferenceDifference("operators/System.EnsureGenerators.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OperatorBodyIsSourceText),
			new ReferenceDifference("operators/System.IsSpecial.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OperatorBodyIsSourceText),
			new ReferenceDifference("operators/System.IsUndefined.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OperatorBodyIsSourceText),
			new ReferenceDifference("operators/System.LibrarySetting.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OperatorBodyIsSourceText),
			new ReferenceDifference("operators/System.SafeDrop.md#Declarations", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.OperatorBodyIsSourceText),

			// The one page that was wrong about what shipped: it carried System.RequiredLibraries' two declarations where
			// System.RequiredObjects' four belong. Corrected by this ticket, so the entry covers the tag family that is
			// left once the right operator is on the page.
			new ReferenceDifference("operators/System.RequiredObjects.md#Declarations", ReferenceVerdict.Stale, ReferenceDifferenceFamily.WrongOperatorOnThePage, ReferenceDifferenceFamily.TagsOlderThanTheProduct),

			// Every published type declaration: a copy script against an inline create statement. See the class remarks.
			new ReferenceDifference("types/System.Binary.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Boolean.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Byte.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Date.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.DateTime.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Decimal.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Error.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Graphic.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Guid.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Integer.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Long.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Money.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Name.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Short.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.String.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.Time.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.TimeSpan.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.UserID.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape),
			new ReferenceDifference("types/System.VersionNumber.md#Type Declaration", ReferenceVerdict.PreThreeOne, ReferenceDifferenceFamily.CopyScriptShape)
		];

		private static bool Updating
		{
			get { return Environment.GetEnvironmentVariable(UpdateVariable) == "1"; }
		}

		private static string GoldensDirectory
		{
			get { return Path.Combine(TestEnvironment.GoldensDirectory, GoldensFolderName.Replace('/', Path.DirectorySeparatorChar)); }
		}

		private static string PagesDirectory
		{
			get { return Path.Combine(TestEnvironment.RepositoryDirectory, PagesRelativePath.Replace('/', Path.DirectorySeparatorChar)); }
		}

		#region Generation

		/// <summary>
		/// What the catalog says, generated once for the whole fixture: the two queries cost about half a second between
		/// them, which is the whole pass, and every assertion reads the same answer.
		/// </summary>
		private Dictionary<string, string> _operatorDeclarations;
		private Dictionary<string, int> _operatorOverloadCounts;
		private Dictionary<string, string> _typeDeclarations;
		private Dictionary<string, string> _tableVarDefinitions;
		private List<string> _viewNames;

		/// <summary>
		/// Generates the declarations if this fixture has not done it yet. Called by the cases that need them rather
		/// than from a SetUp, so that the case holding the engine defect this ticket fixed fails on its own assertion:
		/// a SetUp generating every declaration would itself crash on the type that could not be scripted, and all three
		/// cases would then report the same SetUp error. Not a OneTimeSetUp either, because the base fixture gives each
		/// test its own session and process and there is no process yet when one runs; the server is the fixture's, so
		/// one generation serves every case.
		/// </summary>
		private void EnsureDeclarations()
		{
			if (_operatorDeclarations == null)
			{
				_operatorDeclarations = OperatorDeclarations(Process, out _operatorOverloadCounts);
				_typeDeclarations = TypeDeclarations(Process);
				_tableVarDefinitions = TableVarDefinitions(Process, out _viewNames);
			}
		}

		/// <summary>
		/// Every System operator name's declarations, from the catalog: the Operators system table with the shipped
		/// Script operator applied to each row, grouped by the name a page is named after.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Operators carries OperatorName - the name without the signature, which is what a page is named after - beside
		/// Name, which is the catalog's own mangled object name (System.Abs0000000643). So grouping a name's overloads is
		/// a grouping on a column and the whole set comes back in one pass; the backward-navigable cursor the old
		/// DocBook script needed was an artifact of walking a cursor that had only Name in it. Ordering by
		/// { OperatorName, Name } fixes the order of overloads within a page, and because the mangled names carry the
		/// object ids that order is declaration order.
		/// </para>
		/// <para>
		/// Script(Name) binds Script(const AName : Name), which resolves a catalog identifier - which is what the mangled
		/// name is. The overload is forced by the column's own type rather than by a conversion, Name being a Name-typed
		/// column. The other overload, Script(const ASpecifier : String), takes the "Abs(Decimal)" specifier form
		/// instead, which this table does not carry whole: OperatorName and Signature would have to be concatenated.
		/// </para>
		/// <para>
		/// Library_Name = 'System' rather than a test on the name's prefix: the old script's "'System.'.Pos(Name) = 0" is
		/// a name test that another library could satisfy.
		/// </para>
		/// <para>
		/// Name stays in the projection although nothing reads it: the order by is applied to what over produced, so a
		/// column the ordering names has to survive the projection.
		/// </para>
		/// <para>
		/// Script returns a Block, which D4Emitter.EmitBlock writes as a bare sequence of terminated statements, each
		/// preceded by a line break. Concatenating a name's overloads therefore gives the shape the pages carry once the
		/// first statement's leading break is removed, which is what Trim does.
		/// </para>
		/// </remarks>
		public static Dictionary<string, string> OperatorDeclarations(IServerProcess process, out Dictionary<string, int> overloadCounts)
		{
			var declarations = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
			var counts = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (string[] row in Rows(process, "select Operators where Library_Name = 'System' add { Script(Name) Declaration } over { OperatorName, Name, Declaration } order by { OperatorName, Name }", "OperatorName", "Declaration"))
			{
				StringBuilder text;
				if (!declarations.TryGetValue(row[0], out text))
				{
					text = new StringBuilder();
					declarations.Add(row[0], text);
					counts.Add(row[0], 0);
				}
				text.Append(row[1]);
				counts[row[0]]++;
			}
			overloadCounts = counts;
			return declarations.ToDictionary(entry => entry.Key, entry => Trim(entry.Value.ToString()), StringComparer.Ordinal);
		}

		/// <summary>Every System scalar type's declaration, from the ScalarTypes system table the same way.</summary>
		public static Dictionary<string, string> TypeDeclarations(IServerProcess process)
		{
			return
				Rows(process, "select ScalarTypes where Library_Name = 'System' add { Script(Name) Declaration } over { Name, Declaration } order by { Name }", "Name", "Declaration")
					.ToDictionary(row => row[0], row => Trim(row[1]), StringComparer.Ordinal);
		}

		/// <summary>
		/// Every System table variable's definition, from the TableVars system table the same way, and the names of the
		/// ones that are views.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Script(Name) on a table variable emits its create table or create view statement: the columns with their
		/// types and tags, the keys and orders, the constraints, and the table's own tags. It does not emit the
		/// references declared beside the table in SystemCatalog.d4. A reference is a catalog object of its own, and the
		/// References system table carries only each one's id, name, library and owner, so nothing a script can select
		/// reports which two tables a reference connects - Script of the reference's own name is the answer, and the
		/// generated index says so rather than leaving a reader to wonder where the references went.
		/// </para>
		/// <para>
		/// Library_Name = 'System' for the same reason as the operator query, although a fixture server carrying no
		/// library has nothing else: a table another library declares must not reach a page in this reference.
		/// </para>
		/// </remarks>
		public static Dictionary<string, string> TableVarDefinitions(IServerProcess process, out List<string> viewNames)
		{
			viewNames =
				Rows(process, "select DerivedTableVars where Library_Name = 'System' over { Name } order by { Name }", "Name")
					.Select(row => row[0])
					.ToList();
			return
				Rows(process, "select TableVars where Library_Name = 'System' add { Script(Name) Definition } over { Name, Definition } order by { Name }", "Name", "Definition")
					.ToDictionary(row => row[0], row => Trim(row[1]), StringComparer.Ordinal);
		}

		/// <summary>The named columns of every row the expression yields, as strings.</summary>
		private static List<string[]> Rows(IServerProcess process, string expression, params string[] columns)
		{
			var result = new List<string[]>();
			IServerCursor cursor = process.OpenCursor(expression, null);
			try
			{
				while (cursor.Next())
					using (IRow row = cursor.Select())
						result.Add(columns.Select(column => (string)row[column]).ToArray());
			}
			finally
			{
				process.CloseCursor(cursor);
			}
			return result;
		}

		/// <summary>
		/// The emitter's leading line break removed and one trailing CRLF put back, so that a golden is a text file
		/// rather than one that starts with a blank line. The CRLF endings inside are the emitter's own and are left
		/// alone: that is what the first assertion is exact about.
		/// </summary>
		private static string Trim(string declarations)
		{
			return declarations.Trim('\r', '\n') + "\r\n";
		}

		#endregion

		#region Pages

		/// <summary>Every page in a folder of the reference, by the object name it is named after.</summary>
		private static List<string> PageNames(string folder)
		{
			return
				Directory.EnumerateFiles(Path.Combine(PagesDirectory, folder), "*.md")
					.Select(Path.GetFileNameWithoutExtension)
					.OrderBy(name => name, StringComparer.Ordinal)
					.ToList();
		}

		/// <summary>The text inside the fenced d4 block under the given bold heading, or null if the page has no such heading.</summary>
		/// <remarks>
		/// Strict on purpose: a heading whose own first non-blank line is not a terminated d4 fence throws, rather than
		/// coming back empty or reaching forward for a later block. An empty block would compare as a difference and be
		/// classified as one, when what has actually happened is that the page's shape changed - and reaching forward
		/// would be worse: 273 of the 310 pages with a declaration heading carry further d4 blocks in their examples, so
		/// a declarations block that lost its fence would be compared against that page's first example instead,
		/// silently. The fence is on the first non-blank line after the heading on every page of the corpus, measured.
		/// </remarks>
		private static string Block(string pageRelativePath, string pageText, string heading)
		{
			string marker = "**" + heading + "**";
			int headingAt = pageText.IndexOf(marker, StringComparison.Ordinal);
			if (headingAt < 0)
				return null;
			int fenceAt = FirstNonBlank(pageText, headingAt + marker.Length);
			int bodyAt = String.CompareOrdinal(pageText, fenceAt, Fence, 0, Fence.Length) == 0 ? pageText.IndexOf('\n', fenceAt) + 1 : -1;
			int endAt = bodyAt <= 0 ? -1 : pageText.IndexOf("\n```", bodyAt, StringComparison.Ordinal);
			if (endAt < 0)
				throw new InvalidOperationException(String.Format("\"{0}\" has a **{1}** heading whose first non-blank line is not a terminated {2} block. This pass reads that block; fix the page, or record the page in PagesWithNoDeclarations with the reason.", pageRelativePath, heading, Fence));
			return pageText.Substring(bodyAt, endAt - bodyAt);
		}

		/// <summary>The opening fence of a declarations block.</summary>
		private const string Fence = "```d4";

		/// <summary>The index of the first non-whitespace character at or after the given one, or the end of the text.</summary>
		private static int FirstNonBlank(string text, int from)
		{
			int index = from;
			while (index < text.Length && Char.IsWhiteSpace(text[index]))
				index++;
			return index;
		}

		/// <summary>
		/// The category index pages, read from the Operators list of the system library's README rather than named here.
		/// A category page carries links and no declarations, so it is not an object and has nothing to compare; reading
		/// the README is what keeps a new page from escaping the comparison by looking like a category.
		/// </summary>
		/// <remarks>
		/// Reading the README can also take a page away: a link to an object page added to that list would classify the
		/// page as a category and drop it out of the comparison, and for one of the 214 pages that agree, nothing else
		/// would say so. So a page read as a category must carry no declarations block, and the run fails naming the page
		/// if one does.
		/// </remarks>
		private static List<string> CategoryPages()
		{
			string readme = File.ReadAllText(Path.Combine(PagesDirectory, "README.md"));
			var names =
				Regex.Matches(readme, @"\]\(operators/(?<name>[^)]+)\.md\)")
					.Select(match => match.Groups["name"].Value)
					.Distinct(StringComparer.Ordinal)
					.OrderBy(name => name, StringComparer.Ordinal)
					.ToList();
			foreach (string name in names)
			{
				string pageRelativePath = OperatorsFolder + "/" + name + ".md";
				if (Block(pageRelativePath, File.ReadAllText(Path.Combine(PagesDirectory, pageRelativePath.Replace('/', Path.DirectorySeparatorChar))), DeclarationsHeading) != null)
					throw new InvalidOperationException(String.Format("\"{0}\" is linked from the README's Operators list, which is how this pass tells a category index page from an object page, but it carries a **{1}** block. Link the category index instead, or this page is no longer compared with the catalog.", pageRelativePath, DeclarationsHeading));
			}
			return names;
		}

		#endregion

		/// <summary>
		/// The form the second assertion compares: line endings made LF, tabs expanded, runs of horizontal whitespace
		/// collapsed to one space, each line trimmed and blank lines dropped. Nothing else - see the class remarks for
		/// why this normalizer exists and why it stops here.
		/// </summary>
		public static string Normalize(string text)
		{
			var lines = new List<string>();
			foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
			{
				var builder = new StringBuilder();
				bool pendingSpace = false;
				foreach (char character in line)
					if (character == ' ' || character == '\t')
						pendingSpace = builder.Length > 0;
					else
					{
						if (pendingSpace)
							builder.Append(' ');
						pendingSpace = false;
						builder.Append(character);
					}
				if (builder.Length > 0)
					lines.Add(builder.ToString());
			}
			return String.Join("\n", lines);
		}

		#region Goldens

		private static string GoldenPath(string relativePath)
		{
			return Path.Combine(GoldensDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
		}

		private static string ReadGolden(string relativePath)
		{
			string path = GoldenPath(relativePath);
			return File.Exists(path) ? File.ReadAllText(path) : null;
		}

		/// <summary>Writes a golden: UTF-8 with no byte-order mark, carrying the CRLF endings the emitter put in it.</summary>
		private static void WriteGolden(string relativePath, string text)
		{
			string path = GoldenPath(relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, text, new UTF8Encoding(false));
		}

		/// <summary>
		/// Every file in the golden tree, forward-slashed and relative to it. Every file, not only the ones with the
		/// golden extension, so that a stray file is reported rather than left to accumulate unnoticed.
		/// </summary>
		private static List<string> ExistingGoldens()
		{
			if (!Directory.Exists(GoldensDirectory))
				return new List<string>();
			return
				Directory.EnumerateFiles(GoldensDirectory, "*", SearchOption.AllDirectories)
					.Select(path => Path.GetRelativePath(GoldensDirectory, path).Replace('\\', '/'))
					.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
					.ToList();
		}

		#endregion

		/// <summary>The blocks this pass compares: every operator and type page that is not a category index.</summary>
		private static List<ReferenceBlock> Blocks()
		{
			var result = new List<ReferenceBlock>();
			var categories = new HashSet<string>(CategoryPages(), StringComparer.Ordinal);
			foreach (string name in PageNames(OperatorsFolder))
				if (!categories.Contains(name))
					result.Add(new ReferenceBlock(OperatorsFolder, name, DeclarationsHeading));
			foreach (string name in PageNames(TypesFolder))
			{
				result.Add(new ReferenceBlock(TypesFolder, name, TypeDeclarationHeading));
				result.Add(new ReferenceBlock(TypesFolder, name, DeclarationsHeading));
			}
			return result;
		}

		/// <summary>What the catalog says about one block's object, or null if the catalog does not have it.</summary>
		private string Generated(ReferenceBlock block)
		{
			var declarations = block.Heading == TypeDeclarationHeading ? _typeDeclarations : _operatorDeclarations;
			string result;
			return declarations.TryGetValue(block.ObjectName, out result) ? result : null;
		}

		/// <summary>
		/// The pass: every documented object's declarations are generated, written to or compared with a golden, and held
		/// against the page's published block; and every recorded set is checked against the tree in both directions.
		/// </summary>
		/// <remarks>
		/// One case rather than one per page, following FrontendDerivationGoldenTest: the three questions this pass asks
		/// are answered by one walk of the corpus, the set comparisons need every page's answer, and a case that needed
		/// its siblings to have run first would give the wrong answer under the name filters AGENTS.md runs the suite
		/// with. A failure names the pages it found, up to the limit below.
		/// </remarks>
		[Test]
		public void EveryPublishedSignatureAgreesWithTheCatalog()
		{
			EnsureDeclarations();
			bool updating = Updating;
			var problems = new List<string>();
			// Case-insensitive because this is held against the names a Windows filesystem gives back, not against the
			// catalog. NOTE: two System names differing only in case would therefore share one golden file and the second
			// would overwrite the first; there is no such pair today (measured over the 311 names). If one ever arrives,
			// GoldenRelativePath needs to disambiguate before this set can tell the two apart.
			var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var differenceByPath = Differences.ToDictionary(difference => difference.BlockPath, StringComparer.Ordinal);
			var differencesMatched = new HashSet<string>(StringComparer.Ordinal);
			var noDeclarations = new HashSet<string>(PagesWithNoDeclarations, StringComparer.Ordinal);
			var noDeclarationsSeen = new HashSet<string>(StringComparer.Ordinal);
			var notInTheCatalog = new HashSet<string>(DocumentedObjectsNotInTheCatalog, StringComparer.Ordinal);
			var notInTheCatalogSeen = new HashSet<string>(StringComparer.Ordinal);
			int written = 0;
			int deleted = 0;

			foreach (ReferenceBlock block in Blocks())
			{
				string pageText = File.ReadAllText(Path.Combine(PagesDirectory, block.PageRelativePath.Replace('/', Path.DirectorySeparatorChar)));
				string published = Block(block.PageRelativePath, pageText, block.Heading);

				if (published == null)
				{
					// Only the Declarations block is optional: a type page with no Type Declaration is one that changed shape.
					if (block.Heading == DeclarationsHeading && noDeclarations.Contains(block.PageRelativePath))
						noDeclarationsSeen.Add(block.PageRelativePath);
					else
						problems.Add(String.Format("\"{0}\" has no **{1}** block. If that is right, record the page in PagesWithNoDeclarations with the reason.", block.PageRelativePath, block.Heading));
				}

				string generated = Generated(block);
				if (generated == null)
				{
					if (notInTheCatalog.Contains(block.ObjectName))
						notInTheCatalogSeen.Add(block.ObjectName);
					else
						problems.Add(String.Format("\"{0}\" documents {1}, which the System library does not have. If the object is gone for good, record it in DocumentedObjectsNotInTheCatalog with the reason - and in docs/migration-notes.md, if this release is what removed it.", block.PageRelativePath, block.ObjectName));
					continue;
				}

				// Claimed before anything can fail, not after: a golden belongs to the page named on it whether or not this
				// block's comparison worked. Claimed afterwards, a page that threw would leave its file unclaimed and an
				// update run would then delete it - and there is no reference runtime to regenerate a golden from.
				claimed.Add(block.GoldenRelativePath);

				if (updating)
				{
					// One golden per object, so a type page's two blocks write it once.
					if (ReadGolden(block.GoldenRelativePath) != generated)
					{
						WriteGolden(block.GoldenRelativePath, generated);
						written++;
					}
				}
				else
				{
					string golden = ReadGolden(block.GoldenRelativePath);
					if (golden == null)
						problems.Add(String.Format("{0}: no golden file. Create \"{1}\" by running with {2}=1.", block, block.GoldenRelativePath, UpdateVariable));
					else if (golden != generated)
						problems.Add(String.Format("{0}: the generated declarations no longer match \"{1}\". {2}", block, block.GoldenRelativePath, FirstDifference(golden, generated)));
				}

				if (published == null)
					continue;

				bool agrees = Normalize(published) == Normalize(generated);
				ReferenceDifference difference;
				bool recorded = differenceByPath.TryGetValue(block.Path, out difference);
				if (recorded)
					differencesMatched.Add(block.Path);

				if (agrees && recorded)
					problems.Add(String.Format("{0} now agrees with the catalog, so the recorded difference {1} is drained. Remove the entry.", block, difference));
				else if (!agrees && !recorded)
					problems.Add(String.Format("{0} disagrees with the catalog and nothing records it. Classify it and add an entry to the difference table, or correct the page. {1}", block, FirstDifference(Normalize(published), Normalize(generated))));
			}

			// Each recorded set, in both directions.
			foreach (string pageRelativePath in noDeclarations.Except(noDeclarationsSeen))
				problems.Add(String.Format("\"{0}\" is recorded as carrying no declarations block, but it has one now. Remove the entry from PagesWithNoDeclarations.", pageRelativePath));
			foreach (string objectName in notInTheCatalog.Except(notInTheCatalogSeen))
				problems.Add(String.Format("{0} is recorded as absent from the catalog, but the System library has it now. Remove the entry from DocumentedObjectsNotInTheCatalog.", objectName));
			foreach (ReferenceDifference difference in Differences.Where(entry => !differencesMatched.Contains(entry.BlockPath)))
				problems.Add(String.Format("The recorded difference {0} matches no block of any page. Remove the entry, or fix the path on it.", difference));

			problems.AddRange(TypeCoverageProblems());
			int inventoryWritten = 0;
			problems.AddRange(InventoryProblems(updating, claimed, ref inventoryWritten));
			written += inventoryWritten;

			foreach (string existing in ExistingGoldens())
			{
				if (claimed.Contains(existing))
					continue;
				if (updating)
				{
					File.Delete(GoldenPath(existing));
					deleted++;
				}
				else
					problems.Add(String.Format("\"{0}\" is a golden nothing produces. Delete it, or find out which page stopped being compared.", existing));
			}

			if (updating)
			{
				RemoveEmptyDirectories(GoldensDirectory);
				Assert.Fail(String.Format("{0}=1: wrote {1} golden file(s) and deleted {2}. An update run always fails, so that it cannot be mistaken for a passing one - the diff is the review, so read it before committing it.{3}", UpdateVariable, written, deleted, Report(problems)));
			}

			if (problems.Count > 0)
				Assert.Fail(String.Format("{0} published signature(s), golden file(s) or recorded entr(ies) do not agree with the tree. Run with {1}=1 to rewrite the goldens, then read the diff before committing it.{2}", problems.Count, UpdateVariable, Report(problems)));

			ReferenceDifference[] unread = Differences.Where(difference => difference.Verdict == ReferenceVerdict.Unread).ToArray();
			if (unread.Length > 0)
				Assert.Warn(String.Format("{0} recorded difference(s) are still unclassified: {1}", unread.Length, String.Join(", ", unread.Select(difference => difference.BlockPath))));
		}

		/// <summary>
		/// The inventory file: one line per System operator name with its overload count and whether a page documents it.
		/// This is the only place an operator with no documentation page is visible at all, so it is what tells a later
		/// reader that the reference is incomplete and by how much. One file rather than a file per name, because an
		/// undocumented name is worth a line where a documented one is worth a reviewable file.
		/// </summary>
		private IEnumerable<string> InventoryProblems(bool updating, HashSet<string> claimed, ref int written)
		{
			claimed.Add(InventoryFileName);

			var documented = new HashSet<string>(Blocks().Where(block => block.Folder == OperatorsFolder).Select(block => block.ObjectName), StringComparer.Ordinal);
			var text = new StringBuilder();
			foreach (string name in _operatorOverloadCounts.Keys.OrderBy(name => name, StringComparer.Ordinal))
				text.Append(String.Format("{0}\t{1}\t{2}\r\n", name, _operatorOverloadCounts[name], documented.Contains(name) ? "documented" : "undocumented"));
			string inventory = text.ToString();

			if (updating)
			{
				if (ReadGolden(InventoryFileName) != inventory)
				{
					WriteGolden(InventoryFileName, inventory);
					written++;
				}
				return [];
			}

			string existing = ReadGolden(InventoryFileName);
			if (existing == null)
				return [String.Format("there is no operator inventory. Create \"{0}\" by running with {1}=1.", InventoryFileName, UpdateVariable)];
			if (existing != inventory)
				return [String.Format("the operator inventory no longer matches \"{0}\". {1}", InventoryFileName, FirstDifference(existing, inventory))];
			return [];
		}

		/// <summary>
		/// The scalar types the catalog has and no page documents, held against the recorded list in both directions. The
		/// operator side of the same question is the inventory file.
		/// </summary>
		private IEnumerable<string> TypeCoverageProblems()
		{
			var documented = new HashSet<string>(PageNames(TypesFolder), StringComparer.Ordinal);
			var recorded = new HashSet<string>(CatalogTypesWithNoPage, StringComparer.Ordinal);
			foreach (string name in _typeDeclarations.Keys.Where(name => !documented.Contains(name)))
				if (!recorded.Contains(name))
					yield return String.Format("the System library has the scalar type {0} and no page documents it. Record it in CatalogTypesWithNoPage with the reason, or write the page.", name);
			foreach (string name in recorded.Where(name => !_typeDeclarations.ContainsKey(name) || documented.Contains(name)))
				yield return String.Format("{0} is recorded as an undocumented scalar type, but it is documented now, or the catalog no longer has it. Remove the entry from CatalogTypesWithNoPage.", name);
		}

		#region Table pages

		/// <summary>
		/// The framing sentence every generated table page carries, with "table" or "view" in it. The same on every page
		/// on purpose: a page whose only other content is generated has to carry the warning that it is generated, and a
		/// reader who arrives on one page should not have to find the index to learn that.
		/// </summary>
		private const string TablePageFraming =
			"A system {0} of the [System library](../README.md): part of the catalog, which a D4 script reads as data."
				+ " **This page is generated from a running server**, so a hand edit here is reverted the next time the"
				+ " pages are written; the [system tables index](README.md) says what the definition below does and does"
				+ " not carry.";

		/// <summary>
		/// The table these pages are generated from, and the one sentence that is not the same on every page: it is
		/// where a reader who wants to ask their own server the question these pages answer will look.
		/// </summary>
		private const string GeneratorTableName = "System.TableVars";

		/// <summary>
		/// The reference the generated index tells a reader to script, chosen because it is declared on the table
		/// these pages are generated from. Held against the catalog in <see cref="EverySystemTableHasAGeneratedPage"/>:
		/// the index is published documentation naming a catalog object, so a renamed reference would leave it
		/// telling a reader to run something that fails, with nothing else to catch it.
		/// </summary>
		private const string ExampleReferenceName = "System.TableVars_CatalogObjects";

		private const string GeneratorTableSentence =
			" This is also the table this reference is generated from: `select TableVars` lists every table variable a"
				+ " catalog holds, and `select TableVars add { Script(Name) Definition }` is the whole of what produces"
				+ " the definitions on these pages.";

		/// <summary>The pages under docs/ are LF; the D4 emitter writes CRLF (TextEmitter.NewLine).</summary>
		private static string Lf(string text)
		{
			return text.Replace("\r\n", "\n");
		}

		/// <summary>A link to another table page, from inside the tables folder.</summary>
		private static string TablePageLink(string name)
		{
			return String.Format("[{0}]({0}.md)", name);
		}

		/// <summary>
		/// The other System table variables a definition names, which for a view is what it is built from. A base
		/// table's definition names none, so the section this feeds appears only on the view pages.
		/// </summary>
		/// <remarks>
		/// A qualified-identifier scan of the emitted text rather than a reading of the view's plan: the names it finds
		/// are kept only when the catalog has a table variable of that name, so the operators a view calls
		/// (.System.IfNil, .System.Diagnostics.ShowLog) drop out. Nothing rests on it being complete - it produces links
		/// and no assertion - and a name it found is in the definition on the page either way.
		/// </remarks>
		private static List<string> NamedTableVars(string definition, string ownName, ICollection<string> tableVarNames)
		{
			var found = new SortedSet<string>(StringComparer.Ordinal);
			foreach (Match match in Regex.Matches(definition, @"(?<![A-Za-z0-9_.])\.(?<name>System\.[A-Za-z_][A-Za-z0-9_]*)"))
			{
				string name = match.Groups["name"].Value;
				if (name != ownName && tableVarNames.Contains(name))
					found.Add(name);
			}
			return found.ToList();
		}

		/// <summary>One generated table page: its name, the framing sentence, the definition, and where to read on.</summary>
		private static string TablePage(string name, bool isView, string definition, List<string> namedTableVars)
		{
			var text = new StringBuilder();
			text.Append("# ").Append(name).Append("\n\n");
			text.Append(String.Format(TablePageFraming, isView ? "view" : "table"));
			if (name == GeneratorTableName)
				text.Append(GeneratorTableSentence);
			text.Append("\n\n");
			text.Append("**Definition**\n\n").Append(Fence).Append("\n").Append(Lf(definition)).Append("```\n");
			if (namedTableVars.Count > 0)
			{
				text.Append("\n**Built From**\n\n");
				text.Append(String.Join(" \\| ", namedTableVars.Select(TablePageLink))).Append("\n");
			}
			text.Append("\n**See Also**\n\n");
			text.Append(String.Format("[System Tables]({0}) \\| [Table Variables]({1}#table-variables) \\| [System Library](../README.md)\n", TablesIndexFileName, CatalogChapterPath));
			return text.ToString();
		}

		/// <summary>
		/// The generated section index: what a system table is, that these pages are generated, that there are no
		/// descriptions and why, what a definition leaves out, and a link to every page.
		/// </summary>
		private static string TablesIndex(List<string> tables, List<string> views, int referenceCount)
		{
			var text = new StringBuilder();
			text.Append("# System Tables\n\n");
			text.Append(String.Format("The System library defines {0} table variables - {1} tables and {2} views - and they are the catalog itself, readable as data: an ordinary `select` against any of them answers a question about the server's own objects, and `select TableVars` is the list below. There is a page here for each one, carrying the definition a running server reports.\n\n", tables.Count + views.Count, tables.Count, views.Count));
			text.Append("**These pages are generated, and every one of them is compared whole.** `ReferenceSignatureGoldenTest` in `tests/DAE.Server.Tests` starts a server carrying nothing but the built-in System library, asks it for each table's definition through the `Script` operator any script can call, and holds the result against the whole of the matching page - this index included. So a hand edit in this folder fails the test run, and is reverted the next time the pages are written. Change what the generator writes, not the page.\n\n");
			text.Append("**There are no descriptions here, and none were invented.** The 3.x reference meant to carry a sentence about each table and each column; it never did - its `SystemTables` chapter was a four-line placeholder saying the section was generated, and the published help file listed the table names with nothing under them. The catalog has nothing to report either: no table or column of `src/engine/DAE/Schema/SystemCatalog.d4` carries a `Catalog.Comment` tag, where the scalar types in `DataTypes.d4` carry seventeen. A sentence per table written from a reading of the engine's source would be a new claim in the place of a converted record, so each page gives the definition and stops. For what the catalog holds and how a script uses it, read [Table Variables](" + CatalogChapterPath + "#table-variables) and the rest of the [Catalog Elements](" + CatalogChapterPath + ") chapter of the Developer's Guide.\n\n");
			text.Append(String.Format("**What a definition does not carry: the references.** `Script` emits a table's columns, keys, orders, constraints and tags, and not the references declared beside it - in this release a reference is a catalog object in its own right, and the [System.References](System.References.md) table carries each one's name, library and owner rather than the two tables it connects. There are {0} of them in the System library. `Script('" + ExampleReferenceName + "')` on a live server gives any one of them in full.\n\n", referenceCount));
			text.Append("## Tables\n\n");
			foreach (string name in tables)
				text.Append("- ").Append(TablePageLink(name)).Append("\n");
			text.Append("\n## Views\n\n");
			foreach (string name in views)
				text.Append("- ").Append(TablePageLink(name)).Append("\n");
			text.Append("\n**See Also**\n\n");
			text.Append(String.Format("[System Library](../README.md) \\| [Catalog Elements]({0}) \\| [Dataphor Reference](../../README.md)\n", CatalogChapterPath));
			return text.ToString();
		}

		private static string TablesDirectory
		{
			get { return Path.Combine(PagesDirectory, TablesFolder); }
		}

		/// <summary>
		/// Every file in the tables folder, by name. Every file, not only the Markdown ones, so that a stray file is
		/// reported rather than left to accumulate in a folder nobody edits by hand.
		/// </summary>
		private static List<string> ExistingTablePages()
		{
			if (!Directory.Exists(TablesDirectory))
				return new List<string>();
			return
				Directory.EnumerateFiles(TablesDirectory, "*", SearchOption.AllDirectories)
					.Select(path => Path.GetRelativePath(TablesDirectory, path).Replace('\\', '/'))
					.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
					.ToList();
		}

		/// <summary>
		/// A generated page as committed, with its line endings made LF before the comparison, so that a checkout or an
		/// editor that changed them is not reported as a difference in the generated text. Null if the page is missing.
		/// </summary>
		/// <remarks>
		/// NOTE: ReadAllText drops a UTF-8 byte-order mark without saying so, so a page that acquired one would compare
		/// equal and keep it, where the write path below always writes none. That needs a hand edit in a folder whose every
		/// page says not to edit it, and ReadGolden has had the same blind spot since this fixture was written; if a mark
		/// ever does arrive here, read the first bytes rather than the text.
		/// </remarks>
		private static string ReadTablePage(string fileName)
		{
			string path = Path.Combine(TablesDirectory, fileName);
			return File.Exists(path) ? Lf(File.ReadAllText(path)) : null;
		}

		/// <summary>Writes a generated page: UTF-8, no byte-order mark, LF, like the rest of docs/reference/system-library.</summary>
		private static void WriteTablePage(string fileName, string text)
		{
			Directory.CreateDirectory(TablesDirectory);
			File.WriteAllText(Path.Combine(TablesDirectory, fileName), text, new UTF8Encoding(false));
		}

		#endregion

		/// <summary>
		/// The system tables: the one part of the 3.x reference that had no text to convert, so these pages are
		/// generated whole and this case is the whole of what keeps them true.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The System library defines the tables and views a D4 script reads the catalog through, and the published
		/// reference documented none of them. Unlike every other part of this reference there is nothing to compare a
		/// replacement against: the DocBook chapter was a placeholder saying the section was generated and the help file
		/// carried the table names with nothing under them, so the only source is a running server. These pages are
		/// therefore generated whole, which is what makes this case different from
		/// <see cref="EveryPublishedSignatureAgreesWithTheCatalog"/>: there, a page is mostly published prose with one
		/// generated block inside it and only the block is compared; here the file is the generated text, so the file is
		/// the comparison and the pages are their own goldens. No copy under tests/goldens for them - a second copy of a
		/// fully generated file would only be another thing to keep in step.
		/// </para>
		/// <para>
		/// <b>Two things are asserted.</b> Every page in the tables folder equals what the catalog generates for it,
		/// character for character once the line endings are made LF; and the table variables the catalog reports are
		/// exactly <see cref="SystemTables"/> and <see cref="SystemViews"/>, in both directions, so that a table
		/// appearing, disappearing or changing between a table and a view is a reviewable change in this file rather
		/// than a page that quietly comes or goes under docs/.
		/// </para>
		/// <para>
		/// DATAPHOR_UPDATE_REFERENCE_GOLDENS=1 writes these pages and deletes the ones no table produces, as it does the
		/// goldens of the other case, and fails the run either way. The pages say on themselves that they are generated,
		/// because a hand edit to one of them is reverted by that run and a reader of the published documentation has no
		/// other way to know.
		/// </para>
		/// <para>
		/// NOTE: the pages carry the definition and nothing else - no reference list, because Script does not emit a
		/// table's references and the References system table does not say which tables one connects (it carries id,
		/// name, library, owner and the two generated flags), so pairing the System library's references with their
		/// tables would mean parsing the emitted create reference text. The index points at Script instead. If a later
		/// pass wants that section, the text to parse is what Script of a reference's own name returns.
		/// </para>
		/// </remarks>
		[Test]
		public void EverySystemTableHasAGeneratedPage()
		{
			EnsureDeclarations();
			bool updating = Updating;
			var problems = new List<string>();
			var views = new HashSet<string>(_viewNames, StringComparer.Ordinal);
			var tableVarNames = new HashSet<string>(_tableVarDefinitions.Keys, StringComparer.Ordinal);
			List<string> referenceNames = Rows(Process, "select References where Library_Name = 'System' over { Name }", "Name").Select(row => row[0]).ToList();
			if (!referenceNames.Contains(ExampleReferenceName))
				problems.Add(String.Format("the generated index tells a reader to script {0} and the System library has no reference of that name. Point ExampleReferenceName at one the catalog has.", ExampleReferenceName));

			problems.AddRange(TableSetProblems(views));

			// Case-insensitive, like the golden claim set of the other case and for the same reason: held against the
			// names a Windows filesystem gives back rather than against the catalog. NOTE: two table variable names
			// differing only in case would therefore share one page and the second would overwrite the first; the 55
			// names are case-insensitively distinct today (measured). If one ever arrives, the file name this builds
			// needs to disambiguate before this set can tell the two apart.
			var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var tables = new List<string>();
			var viewList = new List<string>();
			int written = 0;
			int deleted = 0;

			foreach (string name in _tableVarDefinitions.Keys.OrderBy(name => name, StringComparer.Ordinal))
			{
				bool isView = views.Contains(name);
				(isView ? viewList : tables).Add(name);
				string fileName = name + ".md";
				claimed.Add(fileName);
				string definition = _tableVarDefinitions[name];
				problems.AddRange(TablePageProblems(updating, fileName, TablePage(name, isView, definition, NamedTableVars(definition, name, tableVarNames)), ref written));
			}

			claimed.Add(TablesIndexFileName);
			problems.AddRange(TablePageProblems(updating, TablesIndexFileName, TablesIndex(tables, viewList, referenceNames.Count), ref written));

			foreach (string existing in ExistingTablePages())
			{
				if (claimed.Contains(existing))
					continue;
				if (updating)
				{
					File.Delete(Path.Combine(TablesDirectory, existing.Replace('/', Path.DirectorySeparatorChar)));
					deleted++;
				}
				else
					problems.Add(String.Format("\"{0}/{1}\" is a generated page nothing produces. Delete it, or find out which table stopped being documented.", TablesFolder, existing));
			}

			if (updating)
			{
				RemoveEmptyDirectories(TablesDirectory);
				Assert.Fail(String.Format("{0}=1: wrote {1} system table page(s) and deleted {2}. An update run always fails, so that it cannot be mistaken for a passing one - the diff is the review, so read it before committing it.{3}", UpdateVariable, written, deleted, Report(problems)));
			}

			if (problems.Count > 0)
				Assert.Fail(String.Format("{0} generated system table page(s) or recorded entr(ies) do not agree with the catalog. Run with {1}=1 to rewrite the pages, then read the diff before committing it.{2}", problems.Count, UpdateVariable, Report(problems)));
		}

		/// <summary>Writes or compares one generated page, and says what is wrong with it.</summary>
		private static IEnumerable<string> TablePageProblems(bool updating, string fileName, string page, ref int written)
		{
			string committed = ReadTablePage(fileName);
			if (updating)
			{
				if (committed != page)
				{
					WriteTablePage(fileName, page);
					written++;
				}
				return [];
			}
			if (committed == null)
				return [String.Format("there is no \"{0}/{1}\". Create it by running with {2}=1.", TablesFolder, fileName, UpdateVariable)];
			if (committed != page)
				return [String.Format("\"{0}/{1}\" is not what the catalog generates. {2}", TablesFolder, fileName, FirstDifference(committed, page))];
			return [];
		}

		/// <summary>
		/// The two recorded table-variable sets against the catalog, in both directions, and each name against the kind
		/// it is recorded as. Separate from the page comparison so that a server with an unexpected catalog says so once
		/// rather than once per page.
		/// </summary>
		private IEnumerable<string> TableSetProblems(HashSet<string> views)
		{
			var recordedTables = new HashSet<string>(SystemTables, StringComparer.Ordinal);
			var recordedViews = new HashSet<string>(SystemViews, StringComparer.Ordinal);
			foreach (string name in _tableVarDefinitions.Keys.OrderBy(name => name, StringComparer.Ordinal))
			{
				bool isView = views.Contains(name);
				if (!(isView ? recordedViews : recordedTables).Contains(name))
					yield return
						String.Format
						(
							"the System library has the {0} {1} and it is not recorded in {2}. Add it there - and to docs/migration-notes.md, if this release is what added it.",
							isView ? "view" : "table",
							name,
							isView ? "SystemViews" : "SystemTables"
						);
			}
			foreach (string name in recordedTables.OrderBy(name => name, StringComparer.Ordinal))
				if (!_tableVarDefinitions.ContainsKey(name))
					yield return String.Format("{0} is recorded in SystemTables and the System library does not have it. Remove the entry - and record the removal in docs/migration-notes.md, if this release is what removed it.", name);
				else if (views.Contains(name))
					yield return String.Format("{0} is recorded in SystemTables and the catalog reports it as a view. Move the entry to SystemViews.", name);
			foreach (string name in recordedViews.OrderBy(name => name, StringComparer.Ordinal))
				if (!_tableVarDefinitions.ContainsKey(name))
					yield return String.Format("{0} is recorded in SystemViews and the System library does not have it. Remove the entry - and record the removal in docs/migration-notes.md, if this release is what removed it.", name);
				else if (!views.Contains(name))
					yield return String.Format("{0} is recorded in SystemViews and the catalog reports it as a base table. Move the entry to SystemTables.", name);
		}

		/// <summary>At most this many problems are listed; a failure that names every page of the corpus is unreadable.</summary>
		private const int ReportLimit = 40;

		private static string Report(List<string> problems)
		{
			if (problems.Count == 0)
				return String.Empty;
			var result = new StringBuilder();
			foreach (string problem in problems.Take(ReportLimit))
				result.Append("\n  ").Append(problem);
			if (problems.Count > ReportLimit)
				result.Append(String.Format("\n  ... and {0} more.", problems.Count - ReportLimit));
			return result.ToString();
		}

		/// <summary>Where two texts first differ, as a line and column and a window of each side, so that a failure says what changed.</summary>
		private static string FirstDifference(string expected, string actual)
		{
			string[] expectedLines = expected.Replace("\r\n", "\n").Split('\n');
			string[] actualLines = actual.Replace("\r\n", "\n").Split('\n');
			for (int index = 0; index < Math.Max(expectedLines.Length, actualLines.Length); index++)
			{
				string expectedLine = index < expectedLines.Length ? expectedLines[index] : "<end of text>";
				string actualLine = index < actualLines.Length ? actualLines[index] : "<end of text>";
				if (expectedLine != actualLine)
				{
					int column = 0;
					while (column < expectedLine.Length && column < actualLine.Length && expectedLine[column] == actualLine[column])
						column++;
					return String.Format("First difference at line {0}, column {1}:\n    published or golden: {2}\n    generated:           {3}", index + 1, column + 1, Excerpt(expectedLine, column), Excerpt(actualLine, column));
				}
			}
			return "The two texts differ although no line does, so the difference is in the line endings.";
		}

		private const int ExcerptLength = 200;

		/// <summary>The part of a line around the column that differs, with an ellipsis where it is cut.</summary>
		/// <remarks>
		/// A window around the difference rather than the start of the line: the generated system table pages carry
		/// paragraphs of several hundred characters on one line, and a window taken from the start of such a line reports
		/// the same text for both sides and says nothing about what changed. Measured on the generated index.
		/// </remarks>
		/// <remarks>
		/// NOTE: FrontendDerivationGoldenTest and DeviceTranslationGoldenTests carry their own copies of this and of
		/// FirstDifference, and theirs are still the start-of-line form. That is adequate for what they compare - D4 and
		/// XML, which the emitters break into short lines - so the three were left as three. A third caller that needs the
		/// column is the point at which to unify them.
		/// </remarks>
		private static string Excerpt(string line, int column)
		{
			if (line.Length <= ExcerptLength)
				return line;
			int from = Math.Max(0, Math.Min(column - (ExcerptLength / 2), line.Length - ExcerptLength));
			int length = Math.Min(ExcerptLength, line.Length - from);
			return (from > 0 ? "..." : "") + line.Substring(from, length) + (from + length < line.Length ? "..." : "");
		}

		/// <summary>Removes the directories an update emptied, so that a folder that stops contributing leaves none behind.</summary>
		private static void RemoveEmptyDirectories(string directory)
		{
			if (!Directory.Exists(directory))
				return;
			foreach (string subdirectory in Directory.EnumerateDirectories(directory))
			{
				RemoveEmptyDirectories(subdirectory);
				if (!Directory.EnumerateFileSystemEntries(subdirectory).Any())
					Directory.Delete(subdirectory);
			}
		}

		/// <summary>
		/// The property the goldens rest on: the declarations a catalog reports are the same text every time it is asked,
		/// and the same on a second server built the same way. It holds by measurement rather than by construction - the
		/// old DocBook emitter wrote no timestamp and Script emits only catalog facts, so there should be nothing to
		/// strip - so a change that introduced one, or that made a tag collection iterate a hash table, should fail here
		/// rather than as a mismatch on every golden at once.
		/// </summary>
		[Test]
		public void TheSameCatalogAlwaysGeneratesTheSameDeclarations()
		{
			EnsureDeclarations();
			Dictionary<string, int> counts;
			Assert.That(OperatorDeclarations(Process, out counts), Is.EqualTo(_operatorDeclarations), "Asking the same catalog for its operator declarations twice gave two different answers.");
			Assert.That(TypeDeclarations(Process), Is.EqualTo(_typeDeclarations), "Asking the same catalog for its scalar type declarations twice gave two different answers.");
			List<string> viewNames;
			Assert.That(TableVarDefinitions(Process, out viewNames), Is.EqualTo(_tableVarDefinitions), "Asking the same catalog for its table variable definitions twice gave two different answers.");
			Assert.That(viewNames, Is.EqualTo(_viewNames), "Asking the same catalog which of its table variables are views twice gave two different answers.");

			using (var second = new TestServerInstance(GetType().Name + "Second"))
			{
				second.Server.Start();
				IServerSession session = second.Connect();
				try
				{
					IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
					Assert.That(OperatorDeclarations(process, out counts), Is.EqualTo(_operatorDeclarations), "A second server instance reported different operator declarations, so a golden depends on something other than the catalog.");
					Assert.That(TypeDeclarations(process), Is.EqualTo(_typeDeclarations), "A second server instance reported different scalar type declarations, so a golden depends on something other than the catalog.");
					Assert.That(TableVarDefinitions(process, out viewNames), Is.EqualTo(_tableVarDefinitions), "A second server instance reported different table variable definitions, so a generated page depends on something other than the catalog.");
					Assert.That(viewNames, Is.EqualTo(_viewNames), "A second server instance disagreed about which table variables are views.");
				}
				finally
				{
					second.Server.Disconnect(session);
				}
			}
		}

		/// <summary>
		/// Scripting a scalar type that has no specials. The catalog emitter passed such a type's null IsSpecialOperator
		/// straight to its own EmitDependencies, which dereferences what it is given, so Script('System.Boolean') threw a
		/// null reference from inside the engine instead of returning the type's declaration. Fixed in
		/// src/engine/DAE/Schema/Catalog.cs by this ticket; the defect is older than the port - the same unguarded call is
		/// at 418a5d45 - and the two types of a bare System library that reach it are the ones named here.
		/// </summary>
		[Test]
		public void ScriptingAScalarTypeWithNoSpecialsReturnsItsDeclaration()
		{
			foreach (string name in new string[] { "System.Boolean", "System.Scalar" })
			{
				var parameters = new DataParams();
				parameters.Add(DataParam.Create(Process, "ASpecifier", name));
				using (IDataValue value = ((ServerProcess)Process).Evaluate("Script(ASpecifier)", parameters))
					Assert.That((string)value.AsNative, Does.Contain("create type"), String.Format("Script('{0}') did not return a create type statement.", name));
			}
		}
	}
}
