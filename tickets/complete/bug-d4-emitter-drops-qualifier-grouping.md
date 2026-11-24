description: The server writes stored D4 back out as text, and it used to drop the brackets from an expression like (a.b).c, so the text it wrote was a differently grouped expression. The brackets are back, so what the server writes is now what it was asked to write.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4, docs/migration-notes.md, docs/modernization-plan.md
----
## What landed

`EmitQualifierOperand` in `src/engine/DAE/Language.D4/D4Emitter.cs` parenthesized its operand for one case - a numeric literal, because `1.` lexes as a malformed number. It now does the same for a qualifier operand, for the same kind of reason: `.` is right-associative in the grammar, so an unparenthesized `a.b.c` reads back as `a.(b.c)`, and a tree that was grouped the other way lost its grouping in the printed text. The one qualifier exempted is the shape `EmitQualifierExpression` already prints inside parentheses of its own, `(A.B from Y)`, where a second pair would only be redundant.

The implement stage (`ticket(implement): bug-d4-emitter-drops-qualifier-grouping`) made that change, added four `[TestCase]` entries to `D4EmitterRoundTripTest.EmittedTextReParsesToTheSameTree`, removed the two locals in `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4` that existed only to avoid the construct, and added a row to `docs/migration-notes.md` and a clause to the `test-parser-roundtrip-corpus` row of `docs/modernization-plan.md`. The review stage then made the four changes under *Found and fixed* below. No golden file, coverage-manifest entry, script expectation or allow list was edited in either stage.

## Review findings

### Checked

The emitter change was read against the grammar it claims to follow - `Parser.QualifiedFactor` and `Parser.QualifierExpression` in `src/engine/DAE/Language.D4/Parser.cs` - rather than against the handoff's description of it. That reading confirms the premise: the left operand of a dot is parsed as a *factor*, which can be a parenthesized expression, while the right side recursively swallows the rest of the chain, so a qualifier standing on the left genuinely cannot be spelled without parentheses.

Ten shapes that the narrow condition might mishandle were then put through the real parser and emitter in a throwaway fixture, since deleted: `((A.B).Name from Y)`, `(A.B).C.D`, `(A.B).C(1)`, `((A.B) as String).C`, `(A.B).C := 1` as a statement, `(A.B)[1].C`, `((A.B from Y).C from Z)`, `(A.B from Y).C.D`, an expression carrying metadata (which turns out not to be spellable in D4 at all), and - built in C# rather than parsed, because no D4 source spells it - a left-associative qualifier whose right side is a column extractor. Every one of them prints to text the parser reads back as the same tree. The last was the shape expected to break, since the prefix form of `EmitQualifierExpression` bypasses `EmitExpression`; it prints `((A.B).Name from Y)`, which parses correctly.

Also checked: every producer of a `QualifierExpression` in the engine, `Compiler.CompileQualifierExpression` and `CollapseQualifierExpression` for the claim that the new spelling cannot change a compiled result, the indentation, line endings and byte-order-mark state of all three edited files, and the file size of `D4Emitter.cs`.

### Found and fixed

**The migration note understated which printed text changes.** It said only a definition whose *source* groups a qualifier chain explicitly is affected, "which no library in this repository has" - which reads as "nothing is affected". But `ObjectNodes.EmitStatement` (reading a property) and `ExtractionNodes.EmitStatement` (reading a column of an extracted row) build the chain with the child's printed form on the left and the name on the right, which is grouped left to right whatever the source looked like. So a definition the server prints back from its compiled form - a scripted operator or view body that reads a property of a property - can now show parentheses its source never had. The grouping is the one the server compiled either way; only the spelling changed. The note now says both, because the second half is the one a reader comparing emitted D4 as text would act on.

**One of the new tests asserted something it cannot see.** The case named `QualifiedColumnExtractorLeftOfQualifier_MustNotBeDoublyParenthesized` compares parse trees, and `((A.B from Y)).C` is tree-equal to `(A.B from Y).C`, so the doubling it is named for could never fail it. The case is worth keeping for what it does check - an extractor chain standing to the left of a further dot - so it was renamed, and one new test, `EmittedTextSpellsQualifierOperandParenthesesExactly`, asserts the emitted *text* for the three arms the decision has. That also settles what the exemption is worth: it prevents redundant parentheses accumulating in printed catalog scripts, not a defect, since a doubled pair would re-parse to the same tree. The fourth new case, the plain dotted `A.B.C`, is not in this category and was kept as it stands: over-bracketing a right-associative tree *does* change the re-parsed tree, so that case fails if the condition ever widens.

**`EmitQualifierOperand` amended its own decision.** The new clause arrived as an or-assignment onto the numeric-literal test, which makes the second reason an afterthought on the first rather than its peer, and left the emit method carrying three lines of grammar rationale. The decision is now a named predicate, `QualifierOperandNeedsGroup`, with the two reasons as peers and the rationale on it. No behaviour change; the tests above hold that.

**The handoff's evidence list contains a wrong example.** It offers `(A.B)[1].C` as a look-alike that "already brackets itself". It does not - the emitter prints `A.B[1].C`. That is nonetheless correct, for a reason the handoff does not give: `Parser.QualifiedFactor` applies an indexer to everything parsed so far, so `A.B[1].C` already *is* the left-associative tree and needs no parentheses. Nothing to change in the code; recorded here so a later reader does not reuse the claim.

### Found and deliberately left

**`LSecondPausedLine` in `DebugBreakpoints.d4`,** which the handoff raises as an inconsistency. Left as a local: it is read twice - once in the comparison and once in the error message - so inlining it would evaluate `Debug.GetProcesses()` twice, and unlike the two the ticket inlined it never carried a comment naming this slug. It reads as ordinary style because that is what it is.

**`D4Emitter.cs` is 5,100 lines** (measured with `wc -l`). Genuine size debt, but it predates this diff by far, this change adds nine lines to it, and no current-release anchor or class-level invariant hangs on splitting it - so it does not clear the bar for a ticket filed from here.

### Not found

No shape was found where the new parentheses produce text the parser rejects, or text that reads back as a different tree - ten were tried, including the one expected to break.

No case was found where the new spelling fails to compile where the old one compiled. The one direction that could is a chain in which only the fully collapsed name resolves - `a.b.c` as a single catalog object name - because `Compiler.CompileQualifierExpression` only tries that collapse when the left side is an identifier, and `(a.b).c` puts a parenthesized expression there instead. Reaching it needs a left-associative tree over a *name*, and neither producer makes one: both take their left operand from a child node's printed form, and a child that resolved to a named object prints a single dotted identifier, not a nested qualifier. This is static reasoning over those two methods, as the handoff said of its own version of the claim; it was reached here from the other end - hunting the failing direction rather than looking for a divergence - and found nothing.

No tripwires. Nothing in the change is conditional on future growth: the condition is settled by the grammar, which is a compatibility surface and will not move.

### Tests run

`dotnet build Dataphor.slnx` - 0 errors, 32 warnings on the incremental rebuild of the touched projects (143 on a full build, all pre-existing).

Then, with `--no-build` against `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`:

- The implement stage's corpus set plus the two catalog fixtures a change to printed DDL would show up in: `--filter "FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest|FullyQualifiedName~FormDocumentCorpusTest|FullyQualifiedName~SamplesCorpusTest|FullyQualifiedName~FrontendDerivationGoldenTest|FullyQualifiedName~CatalogStoreTest|FullyQualifiedName~CatalogRollbackSnapshotTest"` - 639 tests, 620 passed, 19 skipped, 0 failed (2 m 25 s). The skips are the parked `legacy/` documents and the three samples that need a SQL Server.
- Every coverage script in the engine areas the diff touches - all the `Serialization*` scripts, the operator-text and parser/emitter scripts, every `Debug*` script, and the operator scripts around them - plus the debugger fixture: `--filter "Name=OperatorSourceText|Name=OperatorText|Name=ParserEmitter|Name=Serialization|Name=SerializationAggregateOperators|Name=SerializationConstraints|Name=SerializationDevices|Name=SerializationDomains|Name=SerializationEventHandlers|Name=SerializationOperators|Name=SerializationReferences|Name=SerializationTables|Name=SerializationTypes|Name=SerializationViews|Name=DebugBreakpoints|Name=DebugLibrary|Name=DebugStepping|Name=DebugTest|Name=Operators|Name=SystemCatalogOperators|Name=AggregateOperators|FullyQualifiedName~DebuggerSessionTest"` - 27 tests, 23 passed, 4 skipped, 0 failed (1 m 2 s). The skips are `DebugTest`, `Serialization`, `SerializationDomains` and `SerializationReferences`, all pre-3.1 scripts owned by `test-pre-3-1-scripts-policy`.

The three part commands were not run: the change is five lines inside one emitter method, with no build, infrastructure, lexer, parser or compiler surface in the diff. CI covers the rest.
