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
	/// One snippet per emitter site that used to render text the D4 parser could not read. The server scripts and
	/// stores catalog DDL through <see cref="D4TextEmitter"/>, so output the parser rejects is a script nobody can run
	/// back. <see cref="ParserRoundTripCorpusTest"/> covers the same ground over whole files, but a file that stays on
	/// its allow list for an unrelated reshape defect keeps no coverage of its malformed-output cause - hence these
	/// snippets, at the parser/emitter layer with no server.
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
	}
}
