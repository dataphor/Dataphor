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

		// Files the corpus pass excludes, each with a one-line reason. The test ticket expects this to be empty; one
		// category is left, and it is tracked:
		//   (parse)        - the parser emits recovery messages on the first read; file is effectively not
		//                     parseable through today's parser (most of legacy/libraries/FHIR.Server/, device
		//                     SystemCatalog variants, and some Coverage.Scripts documents that use grammar the
		//                     parser does not yet accept). Owned by bug-d4-parser-rejects-active-sources.
		// The two emitter categories are gone. (emitter-malformed) - re-parse of the emitted text produced syntax
		// errors - was fixed by bug-d4-emitter-malformed-output. (emitter-reshape) - both parses succeeded but the
		// tree came back a different shape - was three defects fixed in two tickets:
		// bug-d4-parser-desugars-for-and-try-commit made the parser's own desugaring of "for" and "try ... commit"
		// build the shape the equivalent explicit D4 parses to, and bug-d4-emitter-reshapes-parse-tree stopped the
		// emitter reordering metadata tags, printing a "by" clause the source did not have, and splitting a
		// multi-variable declaration into separate statements. D4EmitterRoundTripTest holds all of them fixed.
		// NOTE: see ticket review/test-parser-roundtrip-corpus handoff for the follow-up tickets that drain this
		// list. New entries added here outside that workflow must carry a one-line reason, same shape as these.
		private static readonly Dictionary<string, string> AllowList = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			// (parse) - legacy FHIR / parked libraries whose documents use grammar the current parser rejects
			{ "legacy/libraries/FHIR.Core/Documents/UpdatesToSupportNestUnnest.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/BundleProcessing.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/CPGProcessing.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/Connectathon21.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/ELMAnalysis.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/FHIR.CreateModelInfo.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/FHIR.LoadCQMData.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/FHIR.ModelInfoScratchPad.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/PatientSchemaMusings.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/PostResources.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/QUICK.CreateModelInfo.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/QUICKFHIR.CreateModelInfo.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/SimpleFHIR.CreateModelInfo.d4", "parse" },
			{ "legacy/libraries/FHIR.Server/Documents/USCore.CreateModelInfo.d4", "parse" },
			{ "legacy/libraries/OracleDevice/Source/SystemCatalog.d4", "parse" },
			{ "legacy/libraries/PHINVADS.Test/Documents/TestDevice.d4", "parse" },
			{ "legacy/libraries/SASDevice/Source/Device.SAS/SASDeviceMappings.d4", "parse" },
			{ "legacy/libraries/TestCQL/Documents/Register.d4", "parse" },
			{ "legacy/libraries/TestCache/Documents/Device.d4", "parse" },
			{ "legacy/libraries/TestCache/Documents/DeviceCatalogExpressions.d4", "parse" },
			// (parse) - active SQL device SystemCatalog files use integer literals with device-type prefixes the parser rejects
			{ "libraries/MSSQLDevice/Source/SystemCatalog.d4", "parse" },
			{ "libraries/PGSQLDevice/Source/SystemCatalog.d4", "parse" },
			{ "src/engine/DAE/Schema/SQLDeviceMappings.d4", "parse" },
			// (parse) - active samples and tests with grammar the parser does not yet accept
			{ "samples/Sample.Common/Documents/D4LanguageTour.d4", "parse" },
			{ "samples/Sample.Shipping/Documents/Coverage.AdministrationRole.d4", "parse" },
			{ "samples/Sample.Shipping/Documents/Coverage.Controller.d4", "parse" },
			{ "samples/Sample.Shipping/Source/CoverageRoles/Administration.d4", "parse" },
			{ "samples/Sample.Shipping/Source/CoverageRoles/Sales.d4", "parse" },
			{ "samples/Sample.Shipping/Source/Shipping.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Base/Documents/UnsupportedOracleOperators.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/Domains.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/EntryLibrary.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/ForeignSuperKeysAndConstantReferences.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/IStringLibrary.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryDomains.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/PresentationLibrary.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/Serialization.d4", "parse" },
			{ "tests/libraries/TestFramework.Coverage.Scripts/Documents/SerializationDomains.d4", "parse" },
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
			string source = File.ReadAllText(absolutePath, Encoding.UTF8);

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
				Assert.Fail(String.Format("Parse of \"{0}\" produced {1} message(s); first: {2}", relativePath, firstMessages.Count, firstMessages[0].Message));

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
