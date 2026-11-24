description: The server writes stored D4 back out as text, and it used to drop the brackets from an expression like (a.b).c, so the text it wrote was a differently grouped expression. The brackets are back, so what the server writes is now what it was asked to write.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4, docs/migration-notes.md, docs/modernization-plan.md
difficulty: easy
----
## What landed

**The fix.** `EmitQualifierOperand` (`src/engine/DAE/Language.D4/D4Emitter.cs:384`) parenthesized its operand for one case - a numeric literal, because `1.` lexes as a malformed number. It now does the same for a qualifier operand, for the same kind of reason: `.` is right-associative in the grammar, so an unparenthesized `a.b.c` reads back as `a.(b.c)` and the left-associative tree the emitter was given would be lost. Five lines, exactly the change the implement ticket specified, including the `TrailingColumnExtractor(qualifier) == null` guard that keeps the one self-bracketing shape - `(A.B from Y)`, printed by the prefix form of `EmitQualifierExpression` - from getting a second redundant pair. Both callers of `EmitQualifierOperand` go through the new condition, so the plain qualifier chain and the column-extractor form are both covered.

**Tests added** - four `[TestCase]` entries on `D4EmitterRoundTripTest.EmittedTextReParsesToTheSameTree`, which prints a snippet, re-parses it and compares the trees:

- `var x := (A.B).C;` - the reproduction. Pre-fix the emitter had no path that could bracket a qualifier operand, so the printed text was `A.B.C`, which re-parses to the right-associative tree and fails the comparison.
- `var x := ((A.B).C).D;` - the nested case, which exercises the condition on the inner operand as well as the outer.
- `var x := (A.B from Y).C;` - the arm that must *not* gain a second pair of brackets. Tree equality alone would not catch a doubled pair (`((A.B from Y)).C` is tree-equal), so this case is about the stored text, not the tree; a reviewer wanting that pinned directly would have to assert on the emitted string.
- `var x := A.B.C;` - pins that an ordinary dotted name still prints unbracketed. This is the regression the narrow condition exists to prevent, and the reason the condition tests for `QualifierExpression` rather than for "anything compound".

**The workaround removed.** `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4` read two line numbers into locals (`LPausedLine`, `LFrameLine`) with a comment naming this slug, because the inline `((... )[].Line).ToString()` form was text the emitter could not print back unchanged. Both assertions are now inline, matching the `Locator`, `DidBreak` and `Statement` assertions around them, and the comment is gone. `Name=DebugBreakpoints` and the file's `ParserRoundTripCorpusTest` case both pass, which is the end-to-end evidence that the bracketed text the emitter now writes is text the engine runs.

**Documentation.** A row in the *D4, catalog and libraries* table of `docs/migration-notes.md`, in the voice of the two `D4TextEmitter` rows already there, naming the spelling change and saying to regenerate catalog scripts from an earlier 4.0 preview and check code that compares emitted D4 as text. The `test-parser-roundtrip-corpus` row of `docs/modernization-plan.md` now names this slug as a third *instance* of the reshaped-tree class; the count of three classes is unchanged, which is the point to check if the wording reads as a fourth class.

## Deliberate deviation from the implement ticket

The ticket said three assertions in `DebugBreakpoints.d4` printed a line number through this construct and named two of them (`LPausedLine`, `LFrameLine`) as the ones to restore. There is a third local of the same shape, `LSecondPausedLine`, around line 177, which was left as a local: the ticket's TODO named two, and nothing about it needs the fix. It now reads as ordinary style rather than as a workaround, but a reviewer who wants the file internally consistent would restore it too. Low stakes either way.

## What to check

- The condition's shape. It is deliberately narrow: only a `QualifierExpression` operand, and only when it has no trailing single-column extractor. The fix stage measured that every shape which failed to round-trip had a qualifier on the left of a dot and every shape that round-tripped did not - including look-alikes that already bracket themselves (`(A.B)[1].C`, `(T where ID = 1).X`, `(1 + 2).ToString()`, `row { 1 Z }.Z`). Worth a sceptical read for a shape that argument misses.
- The claim that this cannot change a compiled result. `Compiler.CompileQualifierExpression` (`src/engine/DAE/Compiling/Compiler.cs:2308`) resolves a dotted chain either by applying the dots left to right (which is the bracketed grouping) or by collapsing the chain to the shortest resolving prefix (which agrees with the bracketed grouping wherever the bracketed form compiles at all). The conclusion - the old text was *more* permissive than its tree, never differently valued - comes from reading those two methods, not from a test exhibiting a divergence; no divergence was found. If that reading is wrong, this is a wrong-result bug rather than an emitter-correctness fix, and the migration note understates it.
- Source hygiene: tabs, CRLF and no byte-order mark in `D4Emitter.cs` and the `.d4`; the migration-notes row is CRLF like its neighbours. Verified by writing bytes directly rather than through a formatter.

## Validation run

`dotnet build Dataphor.slnx` - 0 errors, 143 pre-existing warnings. Then, with `--no-build` against `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`:

- `--filter "FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest|FullyQualifiedName~FormDocumentCorpusTest|FullyQualifiedName~SamplesCorpusTest|FullyQualifiedName~FrontendDerivationGoldenTest"` - 510 tests, 491 passed, 19 skipped, 0 failed (48 s). The skips are the parked `legacy/` documents and the three samples that need a SQL Server.
- `--filter "Name=OperatorSourceText|Name=OperatorText|Name=ParserEmitter|Name=Serialization|Name=SerializationAggregateOperators|Name=SerializationConstraints|Name=SerializationDevices|Name=SerializationDomains|Name=SerializationEventHandlers|Name=SerializationOperators|Name=SerializationReferences|Name=SerializationTables|Name=SerializationTypes|Name=SerializationViews"` - 11 passed, 3 skipped (`Serialization`, `SerializationDomains`, `SerializationReferences`, already known failures owned by `test-pre-3-1-scripts-policy`), 0 failed.
- `--filter "FullyQualifiedName~DebuggerSessionTest|Name=DebugBreakpoints|Name=DebugStepping"` - 8 passed, 0 failed.

No golden file, coverage-manifest entry, script expectation or allow list was edited. No `.d4` file in the repository contains the construct, which is why nothing else moved.
