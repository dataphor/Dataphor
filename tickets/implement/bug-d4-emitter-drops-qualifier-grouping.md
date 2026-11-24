description: When the server writes stored D4 back out as text it drops the brackets from an expression like (a.b).c, so the text it writes is a differently grouped expression. Put the brackets back, so that what the server writes is what it was asked to write.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4, docs/migration-notes.md, docs/modernization-plan.md
difficulty: easy
repro: verified
----
## What is wrong

`D4TextEmitter` prints a qualifier chain without the grouping the parse tree carries. The `.` operator is right-associative in the grammar, so `a.b.c` parses as `a.(b.c)`; a source that writes `(a.b).c` gets the left-associative tree instead. The emitter prints that tree as `a.b.c`, which reads back as the right-associative one, so the printed text is not a printing of the tree it was given.

The site is one method, `EmitQualifierOperand` (`src/engine/DAE/Language.D4/D4Emitter.cs:384`). It parenthesises its operand for exactly one case today - a numeric literal, because `1.` lexes as a malformed number - and a qualifier operand needs the same treatment for the same kind of reason. Both callers of that method go through it (`EmitQualifierExpression` at line 345 and `EmitQualifierPrefix` at line 378), so the one change covers the plain qualifier chain and the column-extractor form.

This is the third instance of the class that `bug-d4-emitter-malformed-output` and `bug-d4-emitter-reshapes-parse-tree` retired: the printed text did not say what the parse tree said. The generalized guard for the class already exists - `ParserRoundTripCorpusTest`, which prints and re-parses every `.d4` file in the repository - and is what caught this, so the fix needs no new mechanism, only a case of its own in `D4EmitterRoundTripTest`.

## What the fix stage settled

Both questions the bug ticket left open are answered, by probing the parser and emitter directly (a throw-away test in `D4EmitterRoundTripTest` that printed each shape, re-parsed it and compared trees; it is not part of this ticket's deliverable).

**Only a qualifier operand needs brackets, and a dotted name is never one.** In expression context the parser builds a dotted name as a chain of `QualifierExpression` nodes, not as a single `IdentifierExpression` - but it builds them right-associatively, so the *left* operand of every dot in a parser-produced chain is an `IdentifierExpression`. Measured: `System.Integer`, `A.B.C` and `A.B.C.D` print unchanged and round-trip today, and still print unchanged with the fix in place. The feared `(System).Integer` cannot arise. Of the shapes probed, every one that failed to round-trip had a `QualifierExpression` on the left of a dot and every one that round-tripped did not, including shapes that look similar: `(A.B)[1].C`, `(T where ID = 1).X`, `(1 + 2).ToString()`, `row { 1 Z }.Z` and `((1 + 2)).ToString()` all round-trip today, because an indexer, a restriction, a binary expression and a row selector are not qualifiers and each already prints with whatever brackets it needs.

**One operand shape brackets itself, and must not be bracketed twice.** `EmitQualifierExpression` has a prefix form for a qualifier whose right spine ends in a single-column extractor: it prints the whole thing inside its own brackets as `(A.B from Y)`. A plain `expression is QualifierExpression` test makes `(A.B from Y).C` print as `((A.B from Y)).C` - tree-equal, but a redundant pair of brackets in text the server stores. Guarding on `TrailingColumnExtractor(qualifier) == null` prints it back exactly as written. Both variants were measured; the guarded one is the change below.

**The regrouping has not been shown to change any result, and reading the compiler says it cannot.** `Compiler.CompileQualifierExpression` (`src/engine/DAE/Compiling/Compiler.cs:2308`) resolves a dotted chain in two ways. If the leading identifier resolves, `CompileDotOperator` applies the dots left to right, which is the grouping the brackets ask for. If it does not resolve, the compiler collapses the chain into a name one segment at a time and stops at the shortest prefix that resolves, which also agrees with the bracketed grouping wherever the bracketed form compiles at all. The difference is only that the unbracketed form has extra fallbacks the bracketed one does not: it can collapse a prefix that the bracketed form would require to resolve standalone. So the text the emitter writes today is *more* permissive than the tree it was given, never differently valued. That makes this a fix to the emitter's correctness - the printed text must denote the tree - rather than a wrong-result bug, and it is why the severity on the bug ticket was `edge-case`. This claim comes from reading those two methods, not from a test that exhibits a divergence; no divergence was found.

## The change

In `EmitQualifierOperand`, extend the one condition:

```
			// A numeric literal left of a "." has to be parenthesized: "1." lexes as a malformed number.
			ValueExpression value = expression as ValueExpression;
			bool needsGroup =
				(value != null)
					&& ((value.Token == TokenType.Integer) || (value.Token == TokenType.Hex) || (value.Token == TokenType.Float)
						|| (value.Token == TokenType.Decimal) || (value.Token == TokenType.Money));
			// A qualifier left of a "." also has to be parenthesized: "." is right-associative in the grammar, so
			// "a.b.c" reads back as a.(b.c) and would lose this tree's grouping. The one qualifier that does not need
			// it is the form EmitQualifierExpression prints inside its own parentheses, "(A.B from Y)".
			QualifierExpression qualifier = expression as QualifierExpression;
			needsGroup = needsGroup || ((qualifier != null) && (TrailingColumnExtractor(qualifier) == null));
```

`TrailingColumnExtractor` is already a private static in the same class, just above this method. The file uses tabs and CRLF line endings and has no byte-order mark; keep all three.

## Measured blast radius: none

With exactly that change built into `Dataphor.slnx`, every one of these passed, with no golden, script or allow-list edit of any kind:

- `--filter "FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest|FullyQualifiedName~FormDocumentCorpusTest|FullyQualifiedName~SamplesCorpusTest|FullyQualifiedName~FrontendDerivationGoldenTest"` - 507 tests, 488 passed, 19 skipped (the samples that need a SQL Server, and `Sample.Shipping`).
- The engine coverage scripts that print and compare D4: `--filter "Name=OperatorSourceText|Name=OperatorText|Name=ParserEmitter|Name=Serialization|Name=SerializationAggregateOperators|Name=SerializationConstraints|Name=SerializationDevices|Name=SerializationDomains|Name=SerializationEventHandlers|Name=SerializationOperators|Name=SerializationReferences|Name=SerializationTables|Name=SerializationTypes|Name=SerializationViews"` - 11 passed, 3 skipped as known failures already owned by `test-pre-3-1-scripts-policy` (`Serialization`, `SerializationDomains`, `SerializationReferences`), none mismatched.
- `--filter "FullyQualifiedName~DebuggerSessionTest|Name=DebugBreakpoints|Name=DebugStepping"` - all passed.

No `.d4` file in the repository contains the construct, which is why nothing moved. Re-run these during implement rather than trusting the numbers above; they were measured on an unstaged copy of the change on 2026-11-23 at `aa26c464`.

## The workaround this removes

`debt-debugger-breakpoint-script` met this defect while writing `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4`: three assertions printed a line number with `((table where c)[].Line).ToString()`, the file's corpus case failed, and two of them were rewritten to read the value into a local (`LPausedLine`, `LFrameLine`) first, with a comment at that site naming this slug. With the fix in place both were restored to the inline form and `Name=DebugBreakpoints` and the file's corpus case both pass - verified, not assumed. Restore them and delete the comment: a comment that says the emitter cannot print a construct is false once it can, and the restored assertions are the end-to-end evidence that the bracketed text the emitter now writes is text the engine runs.

## Documentation

**Migration note.** Emitted D4 is text an application, a deployment script or a custom extension can read, so this needs an entry in `docs/migration-notes.md`, in the same table and the same voice as the two entries already there for `D4TextEmitter` (the "Text produced by the D4 emitter ... re-parses where it used to fail" and "Printed D4 keeps the metadata tags in the order it was given them" rows, under *The engine and D4*). Say what changed in the spelling - a qualifier left of a `.` is now parenthesized, so `(a.b).c` is written `(a.b).c` rather than `a.b.c` - that only a definition containing the construct is affected, and what to do: regenerate catalog scripts produced by an earlier 4.0 preview, and check code that compares emitted D4 as text.

**The plan's record.** The `test-parser-roundtrip-corpus` row in `docs/modernization-plan.md` (WS3, around line 342) says the corpus pass found three classes of defect and names them with the tickets that retired them. This is a third *instance* of the reshaped-tree class, not a fourth class, so do not change the count: add this slug to that clause so the record names every instance the pass found.

## TODO

- Apply the change to `EmitQualifierOperand` in `src/engine/DAE/Language.D4/D4Emitter.cs`, preserving the file's tabs, CRLF endings and absence of a byte-order mark.
- Add the reproduction to `tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs` as `[TestCase]` entries on `EmittedTextReParsesToTheSameTree`, in the style of the cases already there (one line each, a `TestName` naming the spelling the case used to produce, with a short comment above the group): `var x := (A.B).C;` for the plain defect, `var x := ((A.B).C).D;` for a nested one, `var x := (A.B from Y).C;` for the arm that must *not* add a second pair of brackets, and `var x := A.B.C;` to pin that a dotted name still prints unbracketed, which is the regression the narrow condition exists to prevent. No test for the numeric-literal arm: `NumericLiteralLeftOfQualifier_WasUnparenthesized` already covers it.
- Restore the two inline reads in `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4` and delete the comment naming this slug.
- Add the migration-notes entry and amend the plan's `test-parser-roundtrip-corpus` row.
- Build (`dotnet build Dataphor.slnx`) and run the targeted sets listed under *Measured blast radius*; name the filters in the handoff.
