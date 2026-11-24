/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// One snippet per site where printing a parse tree through <see cref="D4TextEmitter"/> and reading the text back
	/// used to give something other than the tree that was printed - either text the parser could not read at all, or
	/// a tree the parser builds in a shape no D4 text can express. The server scripts and stores catalog DDL through
	/// this emitter, so its output is a script somebody runs back. <see cref="ParserRoundTripCorpusTest"/> covers the
	/// same ground over whole files, but a file that stays on its allow list for an unrelated defect keeps no coverage
	/// of its own cause - hence these snippets, at the parser/emitter layer with no server.
	/// </summary>
	[TestFixture]
	public class D4EmitterRoundTripTest
	{
		// The emitted spelling each case used to produce is named in the test name; the fix is in D4TextEmitter
		// (or, for the money and hexadecimal literals, in its base BasicTextEmitter.EmitValueExpression).
		[TestCase("var x := ToTable(foreach var r in Y do row { r Value });", TestName = "ForEachExpression_RowSelectorBody_WasDorow")]
		[TestCase("create operator Get(const AKey : scalar) : Integer class \"X.Y\";", TestName = "GenericScalarTypeSpecifier_WasGenericScalar")]
		[TestCase("var a : Integer := 0x1F;", TestName = "HexLiteral_WasDropped")]
		[TestCase("var a : Integer := -0x1;", TestName = "NegatedHexLiteral_WasDropped")]
		[TestCase("var a : Money := -$10.50;", TestName = "NegativeMoneyLiteral_WasDollarBeforeSign")]
		[TestCase("grant all on T to user 'U';", TestName = "UserGrantee_WasCaseInsensitiveStringLiteral")]
		[TestCase("revoke all on T from group 'G' inherited;", TestName = "GroupGrantee_WasCaseInsensitiveStringLiteral")]
		[TestCase("var x := (C.Name from row from (V where C.ID = '1'));", TestName = "QualifiedColumnExtractor_WasQualifierOutsideParentheses")]
		// Two qualifiers before the extractor: the only arm of the fix that walks the qualifier chain rather than
		// handling a single dot, and no .d4 file in the repository has the shape.
		[TestCase("var x := (A.B.Name from Y);", TestName = "DoublyQualifiedColumnExtractor_WasQualifiersOutsideParentheses")]
		[TestCase("var x := (1).Max(50);", TestName = "NumericLiteralLeftOfQualifier_WasUnparenthesized")]
		// A qualifier left of a "." needs the same parentheses, for the same kind of reason: "." is right-associative,
		// so an unparenthesized "a.b.c" reads back as a.(b.c). The "from" arm must not get a second pair - the prefix
		// form of EmitQualifierExpression already prints that shape inside parentheses of its own - and a plain dotted
		// name must keep getting none, which is what makes the condition worth narrowing.
		[TestCase("var x := (A.B).C;", TestName = "QualifierLeftOfQualifier_WasUnparenthesized")]
		[TestCase("var x := ((A.B).C).D;", TestName = "NestedQualifierLeftOfQualifier_WasUnparenthesized")]
		[TestCase("var x := (A.B from Y).C;", TestName = "QualifiedColumnExtractorLeftOfQualifier_MustNotBeDoublyParenthesized")]
		[TestCase("var x := A.B.C;", TestName = "DottedName_MustStayUnparenthesized")]
		// The parser desugars "for" and "try ... commit" as it reads them, so the shape it builds has to be the shape
		// the equivalent explicit D4 parses to - see the canonical-shape rule on Parser.DelimitedBlock(string[]).
		// These cases pin the arms of that shape: an empty, one-statement and multi-statement "try ... commit" body, a
		// begin..end body (which stays nested rather than being spliced into the try arm), and the "for" variants.
		[TestCase("try commit;", TestName = "TryCommit_EmptyBody")]
		[TestCase("try F(); commit;", TestName = "TryCommit_OneStatementBody")]
		[TestCase("try F(); G(); commit;", TestName = "TryCommit_TwoStatementBody")]
		[TestCase("try begin F(); G(); end; commit;", TestName = "TryCommit_DelimitedBlockBody")]
		// A multi-variable "var" is the other producer of a plain Block, so it is the other body the try arm
		// splices; VariableStatement builds that block directly rather than returning it from DelimitedBlock.
		[TestCase("try var a, b; commit;", TestName = "TryCommit_MultiVariableDeclarationBody")]
		[TestCase("for i := 1 to 3 do F();", TestName = "For_NoVariableDeclaration")]
		[TestCase("for var i := 1 to 3 do F();", TestName = "For_VariableDeclaration")]
		[TestCase("for var i : Integer := 1 to 3 do F();", TestName = "For_VariableDeclarationWithTypeSpecifier")]
		[TestCase("for i := 10 downto 1 step 2 do F();", TestName = "For_DowntoWithExplicitStep")]
		[TestCase("for var i := 1 to 2 do for var j := 1 to 2 do F();", TestName = "For_Nested")]
		// A multi-variable "var" is one statement in the source but a plain Block of VariableStatements in the tree,
		// and the emitter printed that block's statements one per line. In a statement list that only changed the
		// shape; in a single-statement position it changed what the script does - "if c then var a, b;" came back
		// with "var b" declared unconditionally. These cases cover each single-statement position, the two spellings
		// of a begin..end that parse to the same tree, and a script whose statements are all declarations (which must
		// stay a list of statements, not become one group).
		[TestCase("if c then var a, b;", TestName = "VariableGroup_IfArm_WasSplitOutOfTheCondition")]
		[TestCase("while c do var a, b;", TestName = "VariableGroup_WhileBody_WasSplitOutOfTheLoop")]
		[TestCase("try F(); except on E do var a, b; end;", TestName = "VariableGroup_ErrorHandler_WasSplitOutOfTheHandler")]
		[TestCase("for var i := 1 to 2 do var a, b;", TestName = "VariableGroup_ForBody_WasSplitOutOfTheLoop")]
		[TestCase("begin var a; var b; end;", TestName = "VariableGroup_BeginEnd_SeparateDeclarations")]
		[TestCase("begin var a, b; end;", TestName = "VariableGroup_BeginEnd_OneDeclaration")]
		[TestCase("var a; var b;", TestName = "VariableGroup_ScriptOfOnlyDeclarations_MustStayAList")]
		[TestCase("var a := 1, b : Integer, c : Integer := 2;", TestName = "VariableGroup_WithAndWithoutTypeSpecifierAndInitializer")]
		// Printing the static and the dynamic tags as two groups reordered a mixed set. Two tags take EmitTags's
		// single-line layout, four take its multi-line branch.
		[TestCase("create table T { ID : Integer, key { ID } } static tags { A.Two = \"2\" } tags { A.One = \"1\" };", TestName = "MixedMetaData_TwoTags_WasRegrouped")]
		[TestCase("create table T { ID : Integer, key { ID } } static tags { A.Two = \"2\", A.Four = \"4\" } tags { A.One = \"1\", A.Three = \"3\" };", TestName = "MixedMetaData_FourTags_WasRegrouped")]
		// The "by" clause of a quota expression is optional, and the emitter printed an empty one regardless.
		[TestCase("var x := T return 1;", TestName = "Quota_NoByClause_WasPrintedWithAnEmptyOne")]
		[TestCase("var x := T return 1 by { ID };", TestName = "Quota_ByClause")]
		// A "by" clause that is present and empty is not the same tree as none: the compiler orders by no columns
		// for the first and by the clustering key for the second (Compiler.CompileQuotaExpression), so the guard
		// on the clause has to read the flag rather than the column count.
		[TestCase("var x := T return 1 by { };", TestName = "Quota_EmptyByClause_IsNotNoByClause")]
		public void EmittedTextReParsesToTheSameTree(string source)
		{
			Statement first = new Parser().ParseScript(source, null);
			string emitted = new D4TextEmitter().Emit(first);

			ParserMessages messages = new ParserMessages();
			Statement second;
			try
			{
				second = new Parser().ParseScript(emitted, messages);
			}
			catch (Exception exception)
			{
				Assert.Fail(String.Format("The emitter produced text the parser could not read.\nSource:  {0}\nEmitted: {1}\nError:   {2}", source, emitted, exception.Message));
				return;
			}
			if (messages.Count > 0)
				Assert.Fail(String.Format("Re-parse of the emitted text produced {0} message(s).\nSource:  {1}\nEmitted: {2}\nFirst:   {3}", messages.Count, source, emitted, messages[0].Message));

			Assert.That(
				ParserRoundTripCorpusTest.TreesEqual(first, second, out string difference), Is.True,
				String.Format("Round-trip changed the parse tree.\nSource:  {0}\nEmitted: {1}\nAt:      {2}", source, emitted, difference));
		}

		/// <summary>
		/// A quota expression built in C# rather than parsed - through the constructor that takes order columns -
		/// prints its "by" clause. The columns are read only where HasByClause is set, so leaving the flag unset
		/// dropped them from the printed text once the emitter started honouring it.
		/// </summary>
		[Test]
		public void QuotaExpressionBuiltWithColumnsPrintsItsByClause()
		{
			QuotaExpression expression =
				new QuotaExpression
				(
					new IdentifierExpression("T"),
					new ValueExpression(1L),
					new OrderColumnDefinition[] { new OrderColumnDefinition("ID", true) }
				);

			Assert.That(new D4TextEmitter().Emit(expression), Does.Contain("by { ID asc }"));
		}

		/// <summary>
		/// The filter EmitMetaData applies: a static tag always prints, a dynamic one only where the mode is
		/// ForRemote or the tag is not inherited. Printing the static and the dynamic tags as two groups reordered a
		/// mixed set, and the one ordered pass that replaced those two had to keep this filter exactly as it was;
		/// nothing else in the suite prints with EmitMode.ForRemote, which is the mode the server sends a remote
		/// client its plan catalog in.
		/// </summary>
		[TestCase(EmitMode.ForCopy, false, TestName = "MetaDataFilter_ForCopy_DropsAnInheritedDynamicTag")]
		[TestCase(EmitMode.ForRemote, true, TestName = "MetaDataFilter_ForRemote_KeepsAnInheritedDynamicTag")]
		public void InheritedDynamicTagPrintsOnlyForRemote(EmitMode mode, bool keepsInheritedDynamic)
		{
			Block script = (Block)new Parser().ParseScript("create table T { ID : Integer, key { ID } };", null);
			CreateTableStatement statement = (CreateTableStatement)script.Statements[0];
			statement.MetaData =
				new MetaData
				(
					new Tag[]
					{
						new Tag("A.InheritedDynamic", "1", true, false),
						new Tag("A.OwnDynamic", "2", false, false),
						new Tag("A.InheritedStatic", "3", true, true)
					}
				);

			string emitted = new D4TextEmitter(mode).Emit(script);

			Assert.That(emitted, Does.Contain("A.OwnDynamic"), "a dynamic tag the object owns prints in every mode");
			Assert.That(emitted, Does.Contain("A.InheritedStatic"), "a static tag prints in every mode, inherited or not");
			Assert.That(emitted.Contains("A.InheritedDynamic"), Is.EqualTo(keepsInheritedDynamic));
		}
	}
}
