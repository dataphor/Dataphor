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
	/// a customer finds.
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
		/// per name rather than a list here because there are 308 of them.
		/// </summary>
		private static readonly string[] CatalogTypesWithNoPage =
		[
			"System.FileReference",
			"System.LibraryDescriptor",
			"System.LibraryReference",
			"System.Scalar"
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

		/// <summary>
		/// Generates the declarations if this fixture has not done it yet. On first use rather than in a OneTimeSetUp
		/// because the base fixture gives each test its own session and process, so there is no process yet when a
		/// OneTimeSetUp runs; the server is the fixture's, so one generation serves every case.
		/// </summary>
		[SetUp]
		public void GenerateDeclarations()
		{
			if (_operatorDeclarations == null)
			{
				_operatorDeclarations = OperatorDeclarations(Process, out _operatorOverloadCounts);
				_typeDeclarations = TypeDeclarations(Process);
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
		/// Strict on purpose: a heading with no fenced d4 block under it, or an unterminated fence, throws rather than
		/// coming back empty. An empty block would compare as a difference and be classified as one, when what has
		/// actually happened is that the page's shape changed.
		/// </remarks>
		private static string Block(string pageRelativePath, string pageText, string heading)
		{
			int headingAt = pageText.IndexOf("**" + heading + "**", StringComparison.Ordinal);
			if (headingAt < 0)
				return null;
			int fenceAt = pageText.IndexOf("```d4", headingAt, StringComparison.Ordinal);
			int bodyAt = fenceAt < 0 ? -1 : pageText.IndexOf('\n', fenceAt) + 1;
			int endAt = bodyAt <= 0 ? -1 : pageText.IndexOf("\n```", bodyAt, StringComparison.Ordinal);
			if (endAt < 0)
				throw new InvalidOperationException(String.Format("\"{0}\" has a **{1}** heading with no fenced d4 block under it. This pass reads that block; fix the page, or record the page in PagesWithNoDeclarations with the reason.", pageRelativePath, heading));
			return pageText.Substring(bodyAt, endAt - bodyAt);
		}

		/// <summary>
		/// The category index pages, read from the Operators list of the system library's README rather than named here.
		/// A category page carries links and no declarations, so it is not an object and has nothing to compare; reading
		/// the README is what keeps a new page from escaping the comparison by looking like a category.
		/// </summary>
		private static List<string> CategoryPages()
		{
			string readme = File.ReadAllText(Path.Combine(PagesDirectory, "README.md"));
			return
				Regex.Matches(readme, @"\]\(operators/(?<name>[^)]+)\.md\)")
					.Select(match => match.Groups["name"].Value)
					.Distinct(StringComparer.Ordinal)
					.OrderBy(name => name, StringComparer.Ordinal)
					.ToList();
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
			bool updating = Updating;
			var problems = new List<string>();
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

		/// <summary>Where two texts first differ, as a line number and the two lines, so that a failure says what changed.</summary>
		private static string FirstDifference(string expected, string actual)
		{
			string[] expectedLines = expected.Replace("\r\n", "\n").Split('\n');
			string[] actualLines = actual.Replace("\r\n", "\n").Split('\n');
			for (int index = 0; index < Math.Max(expectedLines.Length, actualLines.Length); index++)
			{
				string expectedLine = index < expectedLines.Length ? expectedLines[index] : "<end of text>";
				string actualLine = index < actualLines.Length ? actualLines[index] : "<end of text>";
				if (expectedLine != actualLine)
					return String.Format("First difference at line {0}:\n    published or golden: {1}\n    generated:           {2}", index + 1, Excerpt(expectedLine), Excerpt(actualLine));
			}
			return "The two texts differ although no line does, so the difference is in the line endings.";
		}

		private const int ExcerptLength = 200;

		private static string Excerpt(string line)
		{
			return line.Length <= ExcerptLength ? line : line.Substring(0, ExcerptLength) + "...";
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
			Dictionary<string, int> counts;
			Assert.That(OperatorDeclarations(Process, out counts), Is.EqualTo(_operatorDeclarations), "Asking the same catalog for its operator declarations twice gave two different answers.");
			Assert.That(TypeDeclarations(Process), Is.EqualTo(_typeDeclarations), "Asking the same catalog for its scalar type declarations twice gave two different answers.");

			using (var second = new TestServerInstance(GetType().Name + "Second"))
			{
				second.Server.Start();
				IServerSession session = second.Connect();
				try
				{
					IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
					Assert.That(OperatorDeclarations(process, out counts), Is.EqualTo(_operatorDeclarations), "A second server instance reported different operator declarations, so a golden depends on something other than the catalog.");
					Assert.That(TypeDeclarations(process), Is.EqualTo(_typeDeclarations), "A second server instance reported different scalar type declarations, so a golden depends on something other than the catalog.");
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
				string declaration = (string)((ServerProcess)Process).Evaluate("Script(ASpecifier)", parameters).AsNative;
				Assert.That(declaration, Does.Contain("create type"), String.Format("Script('{0}') did not return a create type statement.", name));
			}
		}
	}
}
