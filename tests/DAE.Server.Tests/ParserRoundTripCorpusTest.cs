/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// L0 of the WS3 test layer: every .d4 file in the repository parses, and re-emitting the parse tree then parsing
	/// the result gives the same tree. Exercises the lexer, parser and D4TextEmitter in isolation, with no server.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Three kinds of .d4 file are not a whole D4 script, and each has its own rule here. A device's SystemCatalog
	/// resource is a String.Format template, so <see cref="FormatTemplates"/> formats it first, as the device does. A
	/// document under the parked trees that the parser cannot read is Ignored rather than failed, with
	/// <see cref="TheParkedTreeIsNoLessReadableThanItWas"/> holding the number of them from rising. Everything else that
	/// is not a script is named in <see cref="AllowList"/> with the line the parser stops at, and
	/// <see cref="EveryExcludedFileIsStillWhatItsReasonSays"/> holds those entries against the tree: each file it names
	/// must still fail, and still fail at the line its entry records.
	/// </para>
	/// <para>
	/// The reference signature goldens (tests/goldens/reference) are in this corpus on purpose, not by accident of the
	/// walk: they are what the engine's own catalog emitter writes for every System operator and scalar type, so
	/// parsing and round-tripping them asks whether a catalog copy script the product generates can be read back - the
	/// property an administrator depends on when they script a catalog and run it elsewhere.
	/// ReferenceSignatureGoldenTest generates them; a change there changes this corpus's case count.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class ParserRoundTripCorpusTest
	{
		// Folder names (case-insensitive match) pruned during the directory walk. These are build artifacts, submodules
		// and the agent-run log directory; a .d4 beneath any of them is not a repository source file.
		private static readonly HashSet<string> ExcludedDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"artifacts", "obj", "bin", ".git", "node_modules", "external",
		};

		// Paths (repository-relative, forward slashes) pruned during the walk; "tickets/.logs" is relative so matches
		// only that subtree, unlike the bare names above.
		private static readonly HashSet<string> ExcludedRelativeDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"tickets/.logs",
		};

		// The repository folder holding the parked trees - the libraries and prototypes this modernization set aside
		// (library-park-move, library-park-side-projects). Nobody owns the D4 in them, so a parked document the parser
		// cannot read is not a defect anyone will fix: its case is Ignored rather than failed (see RoundTrips), and the
		// number of such documents is held by TheParkedTreeIsNoLessReadableThanItWas.
		private const string ParkedTreeDirectory = "legacy";

		// The .d4 files that are not scripts but String.Format templates. Each is a SQL device's SystemCatalog resource,
		// where "{0}" is the device name and "{{" / "}}" are escaped braces; the device formats the resource before
		// executing it, so the corpus formats it before parsing it. The arguments mirror what that device's own C# call
		// site passes, faithfully, because the count has to cover the template's highest placeholder or String.Format
		// throws - Oracle's call site passes two where the SQL Server and PostgreSQL ones pass four. The values
		// themselves change nothing the parser sees beyond the substitution, so a placeholder device name is enough.
		private static readonly Dictionary<string, object[]> FormatTemplates = new Dictionary<string, object[]>(StringComparer.OrdinalIgnoreCase)
		{
			// libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLDevice.cs RegisterSystemObjectMaps:
			// String.Format(text, Name, "false", IsMSSQL70, IsAccess)
			{ "libraries/MSSQLDevice/Source/SystemCatalog.d4", new object[] { "TestDevice", "false", "false", "false" } },
			// libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLDevice.cs RegisterSystemObjectMaps:
			// String.Format(script, Name, "false", false, false)
			{ "libraries/PGSQLDevice/Source/SystemCatalog.d4", new object[] { "TestDevice", "false", "false", "false" } },
			// legacy/libraries/OracleDevice/Source/Device.Oracle/OracleDevice.cs RegisterSystemObjectMaps:
			// String.Format(text, Name, "false") - two arguments, not four
			{ "legacy/libraries/OracleDevice/Source/SystemCatalog.d4", new object[] { "TestDevice", "false" } },
		};

		// Files the corpus pass excludes, each with the reason the parser stops on it. All fifteen were verified by
		// parsing them one at a time on 2026-11-22, and not one is a parser defect: each is a document that is not a
		// whole D4 script - a missing ";" or "then", or a type specifier the language does not have - and the parser is
		// right to refuse it. Repairing that text is not this list's call to make. The eight Coverage.Scripts entries are
		// owned by test-pre-3-1-scripts-policy (and recorded as known-failure against it in coverage-manifest.json, against
		// the same causes - the text there is the engine's error from running the script, not the parser's own), and that
		// ticket carries the seven below as a second arm; it asks the owner before editing any D4 document to make it run,
		// because without a reference runtime the text is the record of correct behaviour.
		//
		// This list held 38 entries in three categories, all now resolved rather than suppressed.
		// (emitter-malformed) - the re-parse of emitted text produced syntax errors - was fixed by
		// bug-d4-emitter-malformed-output. (emitter-reshape) - both parses succeeded but the tree came back a different
		// shape - was three defects, fixed by bug-d4-parser-desugars-for-and-try-commit (the parser's own desugaring of
		// "for" and "try ... commit" built a shape the equivalent explicit D4 does not) and by
		// bug-d4-emitter-reshapes-parse-tree (reordered metadata tags, a "by" clause the source did not have, a split
		// multi-variable declaration); D4EmitterRoundTripTest holds all of them fixed. (parse) - 38 files the parser
		// would not read - was bug-d4-parser-rejects-active-sources, which found no parser gap behind any of them: three
		// were String.Format templates and are covered through FormatTemplates above (Oracle's is also a parked
		// document), nineteen more were parked documents now Ignored by the rule above, one was an orphan using a
		// "create device { ... }" form removed from the grammar in 2006 and was deleted, and these fifteen are what is left.
		//
		// A new entry carries a reason in the same shape: the line the parser stops at, then what the file is and what the
		// parser will not accept in it. The stop line is held against the parser by
		// EveryExcludedFileIsStillWhatItsReasonSays, so it cannot drift from the file the way a number buried in prose can;
		// the prose beside it still cannot be checked by anything, so keep it to what the stop line does not already say.
		private static readonly Dictionary<string, (int StopLine, string Reason)> AllowList = new Dictionary<string, (int StopLine, string Reason)>(StringComparer.OrdinalIgnoreCase)
		{
			// Owned by test-pre-3-1-scripts-policy: documents in the pre-3.1 coverage script set, whose fate that ticket is
			// deciding. Changing them, or their coverage-manifest.json entries, is its job and not this list's.
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/Domains.d4", (9, "\"create domain TestDomain like Integer;\": \"domain\" was the pre-3.0 spelling of \"type\" and is not a keyword today. Owned by test-pre-3-1-scripts-policy.") },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/EntryLibrary.d4", (8, "\"entry {ID : Integer, Name : String}\": D4 has no \"entry\" type specifier (TypeSpecifier offers generic, scalar, row, table, list, cursor and typeof). Owned by test-pre-3-1-scripts-policy.") },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/ForeignSuperKeysAndConstantReferences.d4", (24, "\"alter table A\", because the \"create constraint\" ending at line 19 has no \";\". Owned by test-pre-3-1-scripts-policy.") },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/IStringLibrary.d4", (12, "the IString literal \"Blah\"i: the \"...\"i suffix lives behind the USEISTRING conditional in Parser.cs, which no project defines. Owned by test-pre-3-1-scripts-policy.") },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryDomains.d4", (10, "an IString literal, the same USEISTRING conditional as IStringLibrary.d4. Owned by test-pre-3-1-scripts-policy.") },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/PresentationLibrary.d4", (10, "\"presentation{entry{}}\": neither type specifier exists in D4. Owned by test-pre-3-1-scripts-policy.") },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/Serialization.d4", (18, "a \"select\", because the debugging \"select\" on line 17 has no \";\". Owned by test-pre-3-1-scripts-policy.") },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/SerializationDomains.d4", (13, "\"create domain SimpleDomain like String;\", the pre-3.0 spelling of \"type\". Owned by test-pre-3-1-scripts-policy.") },
			// Documents in the supported tree that are not whole scripts. test-pre-3-1-scripts-policy carries these seven as
			// its second arm: five want one or two characters added, two want a statement rewritten.
			{ "tests/libraries/TestFramework.Coverage.Base/Documents/UnsupportedOracleOperators.d4", (8, "\"EnsureDeviceStarted(...)\", because the \"select\" on line 6 has no \";\" (nor does line 8's own statement, but the parser never reaches it); the document also targets the Oracle device, which is parked.") },
			{ "samples/Sample.Common/Documents/D4LanguageTour.d4", (133, "a \"select\", because the \"select ... union (...)\" on line 131 has no \";\" - a guided tour of D4 meant to be run a statement at a time in the IDE, so most of its statements are terminated and a handful are not.") },
			{ "samples/Sample.Shipping/Documents/Coverage.Controller.d4", (16, "a \"select\", because the \"select ServerSettings\" on line 14 has no \";\".") },
			{ "samples/Sample.Shipping/Documents/Coverage.AdministrationRole.d4", (17, "the \"(\" of the statement on line 17, because the \"if exists (...)\" on line 16 has no \"then\".") },
			{ "samples/Sample.Shipping/Source/CoverageRoles/Administration.d4", (17, "the same missing \"then\" at the same line - this file and Coverage.AdministrationRole.d4 are byte-identical copies.") },
			{ "samples/Sample.Shipping/Source/CoverageRoles/Sales.d4", (179, "\"table { ..., key { Number } }\": a table type specifier takes a column list only, since keys belong to table variables and not to table types.") },
			{ "samples/Sample.Shipping/Source/Shipping.d4", (658, "\"entry { ... }\" as an operator's parameter type: D4 has no \"entry\" type specifier.") },
		};

		public static IEnumerable<TestCaseData> D4Files()
		{
			string repoRoot = TestEnvironment.RepositoryDirectory;
			foreach (string path in EnumerateD4Files(repoRoot))
			{
				string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
				if (AllowList.ContainsKey(relative))
					continue;
				yield return new TestCaseData(path, relative).SetName(relative);
			}
		}

		[TestCaseSource(nameof(D4Files))]
		public void RoundTrips(string absolutePath, string relativePath)
		{
			string source = ReadSourceForParsing(absolutePath, relativePath);

			Parser firstParser = new Parser();
			ParserMessages firstMessages = new ParserMessages();
			Statement first;
			try
			{
				first = firstParser.ParseScript(source, firstMessages);
			}
			catch (Exception exception)
			{
				Assert.Fail(String.Format(
					"Parse of \"{0}\" failed: {1}\nFirst 200 chars of input:\n{2}",
					relativePath, exception.Message, Excerpt(source, 200)));
				return;
			}
			if (firstMessages.Count > 0)
			{
				// Keyed on the first parse having produced messages and not on the path alone: a parked document that does
				// parse still owes a faithful round-trip, and the 58 that do are a good part of what tests the emitter.
				// NOTE: TheParkedTreeIsNoLessReadableThanItWas bounds how many parked documents may be unreadable, not which
				// ones, so a regression that breaks one parked document while a different one starts parsing is invisible to
				// it. If the parked trees ever matter more than that, hold the set of paths rather than their number.
				if (IsUnderTheParkedTree(relativePath))
					Assert.Ignore(String.Format(
						"Parked document \"{0}\" does not parse, and nobody owns the D4 in the parked trees: {1}",
						relativePath, firstMessages[0].Message));
				Assert.Fail(String.Format("Parse of \"{0}\" produced {1} message(s); first: {2}", relativePath, firstMessages.Count, firstMessages[0].Message));
			}

			D4TextEmitter emitter = new D4TextEmitter();
			string emitted = emitter.Emit(first);

			Parser secondParser = new Parser();
			ParserMessages secondMessages = new ParserMessages();
			Statement second;
			try
			{
				second = secondParser.ParseScript(emitted, secondMessages);
			}
			catch (Exception exception)
			{
				TestContext.AddTestAttachment(WriteEmittedToArtifact(relativePath, emitted), "emitted text");
				Assert.Fail(String.Format(
					"Harness bug: the emitter produced text the parser could not read for \"{0}\": {1}\nEmitted text is attached.",
					relativePath, exception.Message));
				return;
			}
			if (secondMessages.Count > 0)
			{
				TestContext.AddTestAttachment(WriteEmittedToArtifact(relativePath, emitted), "emitted text");
				Assert.Fail(String.Format("Harness bug: re-parse of emitted text for \"{0}\" produced {1} message(s); first: {2}",
					relativePath, secondMessages.Count, secondMessages[0].Message));
			}

			if (!TreesEqual(first, second, out string difference))
			{
				TestContext.AddTestAttachment(WriteEmittedToArtifact(relativePath, emitted), "emitted text");
				Assert.Fail(String.Format("Round-trip changed the parse tree for \"{0}\": {1}", relativePath, difference));
			}
		}

		// Two hand-crafted smokes: one positive, one negative, against TreesEqual, so a regression in the comparator is
		// caught independently of the corpus.
		[Test]
		public void TreesEqual_HandCrafted_EqualTrees()
		{
			Statement a = new Parser().ParseScript("select 1 + 2;", null);
			Statement b = new Parser().ParseScript("select 1 + 2;", null);
			Assert.That(TreesEqual(a, b, out string _), Is.True);
		}

		[Test]
		public void TreesEqual_HandCrafted_DifferentTrees()
		{
			Statement a = new Parser().ParseScript("select 1 + 2;", null);
			Statement b = new Parser().ParseScript("select 1 + 3;", null);
			Assert.That(TreesEqual(a, b, out string _), Is.False);
		}

		/// <summary>
		/// The number of .d4 files under the parked trees whose first parse produces messages, measured 2026-11-22 by
		/// <c>dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~ParserRoundTripCorpusTest"</c> and counting
		/// the Ignored cases: 20 of the 77 parked documents, less the Oracle SystemCatalog template, which is in
		/// <see cref="FormatTemplates"/> and parses once formatted.
		/// </summary>
		private const int ParkedDocumentsTheParserCannotRead = 19;

		/// <summary>
		/// A ratchet over the parked trees: the number of parked documents the parser cannot read may fall but not rise.
		/// Those documents' own cases are Ignored, so without this nothing would notice a parser regression that broke one
		/// of the readable ones, nor a newly parked document that does not parse arriving unremarked. Deleting a parked
		/// document only makes this pass, which is intended - the assertion is an upper bound.
		/// </summary>
		[Test]
		public void TheParkedTreeIsNoLessReadableThanItWas()
		{
			string repoRoot = TestEnvironment.RepositoryDirectory;
			List<string> parked = new List<string>();
			List<string> unreadable = new List<string>();
			foreach (string path in EnumerateD4Files(repoRoot))
			{
				string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
				if (!IsUnderTheParkedTree(relative))
					continue;
				parked.Add(relative);
				ParserMessages messages = new ParserMessages();
				new Parser().ParseScript(ReadSourceForParsing(path, relative), messages);
				if (messages.Count > 0)
					unreadable.Add(relative);
			}

			Assert.Multiple(() =>
			{
				// Without this the ratchet passes vacuously if the parked trees are moved or renamed out from under it.
				Assert.That(parked, Is.Not.Empty, String.Format("The walk found no .d4 file under \"{0}/\"; if the parked trees moved, this fixture's rule for them has to move with them.", ParkedTreeDirectory));
				Assert.That
				(
					unreadable.Count,
					Is.LessThanOrEqualTo(ParkedDocumentsTheParserCannotRead),
					String.Format("{0} of the {1} parked documents do not parse, up from {2}. Either the parser has regressed on one that used to read, or a parked document that does not parse was added - in which case raise {3} in the same commit. The unreadable ones are:\n{4}",
						unreadable.Count, parked.Count, ParkedDocumentsTheParserCannotRead, nameof(ParkedDocumentsTheParserCannotRead), String.Join("\n", unreadable))
				);
			});
		}

		/// <summary>
		/// Holds the two hand-written lists against the tree, each arm against a way an entry rots. A path neither list's
		/// file exists at is an entry a rename or a deletion left stale. A path in both lists is a template silently
		/// excluded rather than formatted. A file that now parses is a reason that has outlived its cause, which is how a
		/// suppression quietly becomes permanent. A file that still fails but not at the line its entry records is a reason
		/// still true of the file and no longer of the line, which is how the prose beside an entry goes quietly wrong.
		/// </summary>
		[Test]
		public void EveryExcludedFileIsStillWhatItsReasonSays()
		{
			string repoRoot = TestEnvironment.RepositoryDirectory;
			List<string> nowParses = new List<string>();
			List<string> stopsElsewhere = new List<string>();
			foreach (KeyValuePair<string, (int StopLine, string Reason)> entry in AllowList)
			{
				string absolute = Path.Combine(repoRoot, entry.Key);
				if (!File.Exists(absolute))
					continue;
				ParserMessages messages = new ParserMessages();
				new Parser().ParseScript(ReadSourceForParsing(absolute, entry.Key), messages);
				if (messages.Count == 0)
				{
					nowParses.Add(entry.Key);
					continue;
				}
				// A syntax error carries the position the parser gave up at; anything else does not locate itself, and a
				// first message that is not a syntax error is itself news about the file.
				int stoppedAt = messages[0] is ILocatedException located ? located.Line : -1;
				if (stoppedAt != entry.Value.StopLine)
					stopsElsewhere.Add(String.Format("{0}: entry says line {1}, parser stopped at {2} with \"{3}\"",
						entry.Key, entry.Value.StopLine, stoppedAt < 0 ? "no reported line" : stoppedAt.ToString(), messages[0].Message));
			}

			Assert.Multiple(() =>
			{
				Assert.That
				(
					AllowList.Keys.Concat(FormatTemplates.Keys).Where(relative => !File.Exists(Path.Combine(repoRoot, relative))),
					Is.Empty,
					String.Format("These entries name no file under \"{0}\". Delete the entry, or find out what became of the file.", repoRoot)
				);

				// A path in both lists would be silently excluded rather than formatted, since D4Files skips the allow list.
				Assert.That
				(
					AllowList.Keys.Where(FormatTemplates.ContainsKey),
					Is.Empty,
					String.Format("These files are in both {0} and {1}. A template is an ordinary case once formatted, so delete the {0} entry.", nameof(AllowList), nameof(FormatTemplates))
				);

				Assert.That
				(
					nowParses,
					Is.Empty,
					String.Format("These files now parse; remove their {0} entries so the corpus covers them.", nameof(AllowList))
				);

				Assert.That
				(
					stopsElsewhere,
					Is.Empty,
					String.Format("These files still fail, but not where their {0} entry says. Either the file changed or the parser now gives up somewhere else; re-read the file and correct the entry's line and its prose together.", nameof(AllowList))
				);
			});
		}

		/// <summary>Whether this repository-relative path is inside the parked trees.</summary>
		private static bool IsUnderTheParkedTree(string relativePath)
		{
			return relativePath.StartsWith(ParkedTreeDirectory + "/", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// The text the corpus parses for this file: the file as it stands, or - for one of the <see cref="FormatTemplates"/>
		/// - the file formatted with its own device's argument list, which is the text that device executes.
		/// </summary>
		private static string ReadSourceForParsing(string absolutePath, string relativePath)
		{
			string source = File.ReadAllText(absolutePath, Encoding.UTF8);
			if (!FormatTemplates.TryGetValue(relativePath, out object[] arguments))
				return source;
			try
			{
				return String.Format(source, arguments);
			}
			catch (FormatException exception)
			{
				// A template referencing a placeholder the argument list does not reach throws rather than failing a parse,
				// and String.Format's own message names neither the file nor what to do about it.
				throw new FormatException(String.Format(
					"\"{0}\" is a String.Format template and the {1} argument(s) {2} gives it do not fit it. The argument list has to match the one its device's C# call site passes. String.Format said: {3}",
					relativePath, arguments.Length, nameof(FormatTemplates), exception.Message), exception);
			}
		}

		private static IEnumerable<string> EnumerateD4Files(string repoRoot)
		{
			System.Collections.Generic.Stack<string> stack = new System.Collections.Generic.Stack<string>();
			stack.Push(repoRoot);
			while (stack.Count > 0)
			{
				string current = stack.Pop();
				IEnumerable<string> subdirectories;
				try { subdirectories = Directory.EnumerateDirectories(current); }
				catch (UnauthorizedAccessException) { continue; }
				catch (DirectoryNotFoundException) { continue; }
				foreach (string subdirectory in subdirectories)
				{
					string name = Path.GetFileName(subdirectory);
					if (ExcludedDirectoryNames.Contains(name))
						continue;
					string relative = Path.GetRelativePath(repoRoot, subdirectory).Replace('\\', '/');
					if (ExcludedRelativeDirectories.Contains(relative))
						continue;
					stack.Push(subdirectory);
				}
				IEnumerable<string> files;
				try { files = Directory.EnumerateFiles(current, "*.d4"); }
				catch (UnauthorizedAccessException) { continue; }
				catch (DirectoryNotFoundException) { continue; }
				foreach (string file in files)
					yield return file;
			}
		}

		private static string Excerpt(string value, int max)
		{
			if (value == null) return "<null>";
			value = value.Replace("\r", " ").Replace("\n", " ");
			return value.Length <= max ? value : value.Substring(0, max) + "...";
		}

		private static string WriteEmittedToArtifact(string relativePath, string emitted)
		{
			string sanitized = relativePath.Replace('/', '_').Replace('\\', '_');
			string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "emitted-" + sanitized + ".d4");
			File.WriteAllText(path, emitted, Encoding.UTF8);
			return path;
		}

		/// <summary>
		/// Deep value comparison over the AST. Walks fields reflectively from <see cref="Statement"/>; excludes the
		/// private <c>_lineInfo</c> backing field on <see cref="Statement"/> and anything typed as <see cref="LineInfo"/>,
		/// since those carry source positions. The emitter is allowed to normalize whitespace and relayout, so the two
		/// emitted strings need not match - the trees must.
		/// </summary>
		internal static bool TreesEqual(object left, object right, out string difference)
		{
			return CompareNode(left, right, "root", out difference);
		}

		private static bool CompareNode(object left, object right, string path, out string difference)
		{
			if (ReferenceEquals(left, right)) { difference = null; return true; }
			// Treat a null AST node as equivalent to an empty collection: emitting a tree may touch lazy getters
			// (MetaData.Tags, for one) that materialize an empty collection on first access, so the pre-emit tree ends
			// up with "Tags { Count == 0 }" where the post-emit tree still has "null". Both mean "no entries".
			if (left == null && IsEmptyCollection(right)) { difference = null; return true; }
			if (right == null && IsEmptyCollection(left)) { difference = null; return true; }
			if (left == null || right == null)
			{
				difference = String.Format("{0}: one side is null (left={1}, right={2})", path, Describe(left), Describe(right));
				return false;
			}

			Type leftType = left.GetType();
			Type rightType = right.GetType();
			if (leftType != rightType)
			{
				difference = String.Format("{0}: type differs ({1} vs {2})", path, leftType.FullName, rightType.FullName);
				return false;
			}

			if (left is LineInfo)
			{
				// Source positions: ignored.
				difference = null;
				return true;
			}

			if (IsLeaf(leftType))
			{
				if (!Object.Equals(left, right))
				{
					difference = String.Format("{0}: value differs ({1} vs {2})", path, Describe(left), Describe(right));
					return false;
				}
				difference = null;
				return true;
			}

			// Tags: compare through the indexer, not reflectively. The backing array is oversized (doubling growth)
			// and the structural fields would compare unequal between equivalent tag sets.
			if (left is Tags leftTags && right is Tags rightTags)
			{
				if (leftTags.Count != rightTags.Count)
				{
					difference = String.Format("{0}: Tags count differs ({1} vs {2})", path, leftTags.Count, rightTags.Count);
					return false;
				}
				for (int index = 0; index < leftTags.Count; index++)
				{
					Tag leftTag = leftTags[index];
					Tag rightTag = rightTags[index];
					if (leftTag != rightTag || leftTag.IsStatic != rightTag.IsStatic || leftTag.IsInherited != rightTag.IsInherited)
					{
						difference = String.Format("{0}[{1}]: Tag differs ({2}={3} static={4} vs {5}={6} static={7})",
							path, index, leftTag.Name, leftTag.Value, leftTag.IsStatic, rightTag.Name, rightTag.Value, rightTag.IsStatic);
						return false;
					}
				}
				difference = null;
				return true;
			}

			if (left is IList leftList && right is IList rightList)
			{
				// EmptyStatement is syntactically a stray ";" and carries no semantics; filter from Statement lists.
				// This is the "trailing whitespace" the ticket allows; it shows up because the emitter terminates
				// nested blocks with ";" and the enclosing block appends another ";", which the re-parse reads as
				// one real statement followed by an empty one.
				IList leftEffective = FilterEmptyStatements(leftList);
				IList rightEffective = FilterEmptyStatements(rightList);
				if (leftEffective.Count != rightEffective.Count)
				{
					difference = String.Format("{0}: list length differs ({1} vs {2})", path, leftEffective.Count, rightEffective.Count);
					return false;
				}
				for (int index = 0; index < leftEffective.Count; index++)
					if (!CompareNode(leftEffective[index], rightEffective[index], path + "[" + index + "]", out difference))
						return false;
				difference = null;
				return true;
			}

			if (left is IEnumerable leftSequence && right is IEnumerable rightSequence && !(left is string))
			{
				IEnumerator leftEnumerator = leftSequence.GetEnumerator();
				IEnumerator rightEnumerator = rightSequence.GetEnumerator();
				int index = 0;
				while (true)
				{
					bool leftHas = leftEnumerator.MoveNext();
					bool rightHas = rightEnumerator.MoveNext();
					if (leftHas != rightHas)
					{
						difference = String.Format("{0}: sequence length differs at index {1}", path, index);
						return false;
					}
					if (!leftHas) break;
					if (!CompareNode(leftEnumerator.Current, rightEnumerator.Current, path + "[" + index + "]", out difference))
						return false;
					index++;
				}
				difference = null;
				return true;
			}

			foreach (FieldInfo field in GetComparableFields(leftType))
			{
				object leftValue = field.GetValue(left);
				object rightValue = field.GetValue(right);
				if (!CompareNode(leftValue, rightValue, path + "." + FieldDisplayName(field), out difference))
					return false;
			}
			difference = null;
			return true;
		}

		private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();

		private static FieldInfo[] GetComparableFields(Type type)
		{
			lock (FieldCache)
			{
				if (FieldCache.TryGetValue(type, out FieldInfo[] cached))
					return cached;
				List<FieldInfo> accumulator = new List<FieldInfo>();
				for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
					foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
					{
						if (field.IsStatic) continue;
						// Source-position fields. _lineInfo is the backing field on Statement; MetaData and LineInfo
						// hold their positions as public Line/LinePos/EndLine/EndLinePos fields directly.
						if (field.Name == "_lineInfo") continue;
						if (field.Name == "Line" || field.Name == "LinePos" || field.Name == "EndLine" || field.Name == "EndLinePos") continue;
						accumulator.Add(field);
					}
				FieldInfo[] result = accumulator.ToArray();
				FieldCache[type] = result;
				return result;
			}
		}

		private static string FieldDisplayName(FieldInfo field)
		{
			string name = field.Name;
			if (name.StartsWith("_")) return name.Substring(1);
			return name;
		}

		private static IList FilterEmptyStatements(IList list)
		{
			bool any = false;
			foreach (object item in list)
				if (IsNoop(item)) { any = true; break; }
			if (!any) return list;
			List<object> result = new List<object>(list.Count);
			foreach (object item in list)
				if (!IsNoop(item))
					result.Add(item);
			return result;
		}

		// A syntactic no-op that the emitter can introduce and the parser can produce from a stray ";" - either
		// EmptyStatement, or a Block with no statements (parser returns an empty Block when it sees ";" where a
		// statement was expected). Both are "trailing whitespace" in tree form and must not drive a false positive.
		private static bool IsNoop(object item)
		{
			if (item is EmptyStatement) return true;
			if (item is Block block && !(item is DelimitedBlock) && block.Statements.Count == 0) return true;
			return false;
		}

		private static bool IsEmptyCollection(object value)
		{
			if (value == null) return false;
			if (value is IList list) return list.Count == 0;
			// Tags, and anything else "collection-shaped": has a public int Count property and is IEnumerable.
			if (value is IEnumerable)
			{
				PropertyInfo countProperty = value.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
				if (countProperty != null && countProperty.PropertyType == typeof(int))
					return (int)countProperty.GetValue(value) == 0;
			}
			else
			{
				// Tags holds Tag[] internally and exposes Count but isn't IEnumerable; recognise it by name.
				PropertyInfo countProperty = value.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
				if (countProperty != null && countProperty.PropertyType == typeof(int) && value.GetType().Name == "Tags")
					return (int)countProperty.GetValue(value) == 0;
			}
			return false;
		}

		private static bool IsLeaf(Type type)
		{
			if (type.IsPrimitive) return true;
			if (type.IsEnum) return true;
			if (type == typeof(string)) return true;
			if (type == typeof(decimal)) return true;
			if (type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid)) return true;
			return false;
		}

		private static string Describe(object value)
		{
			if (value == null) return "null";
			if (value is string s) return "\"" + (s.Length > 40 ? s.Substring(0, 40) + "..." : s) + "\"";
			return value.ToString();
		}
	}
}
