description: The server used to reorder metadata tags, add a sort clause nobody wrote, and split a multi-variable declaration when it printed stored D4. All three are fixed, and the nineteen files that were excluded from the parse-print-parse check are back in it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, docs/migration-notes.md
----
## What landed

Three independent defects in `src/engine/DAE/Language.D4/D4Emitter.cs`, all of the same kind: the printed text did not say what the parse tree said. The server prints D4 through this emitter whenever it stores catalog DDL, scripts a catalog out, or shows a user the D4 behind an object, so the output is a script somebody runs back.

**Arm A — a sort clause the source did not have.** `EmitQuotaExpression` printed the `by { … }` list unconditionally, ignoring `QuotaExpression.HasByClause`, so `T return 1` printed as `T return 1 by {  }` and read back with the flag set. The list is now wrapped in `if (expression.HasByClause)`, as the three sibling `by`/`order by` sites in the file already were.

**Arm B — metadata tags came back in a different order.** `EmitMetaData` walked the tags twice, printing the dynamic ones as `tags { … }` and the static ones as `static tags { … }`, so a source that wrote them mixed got them back grouped. It is now one pass in source order: the grammar lets a tag carry its own `static`/`dynamic` modifier (`Parser.MetaDataTag`), so an all-static set still prints `static tags { … }`, an all-dynamic set still prints `tags { … }` — byte for byte as before — and only a mixed set changes shape, to one `tags { … }` group with `static` in front of the tags that need it. `EmitTags` took one extra parameter, `qualifyStaticTags`, for that. The filter is unchanged (a static tag always prints; a dynamic one only when `EmitMode` is `ForRemote` or it is not inherited).

Measured output, mixed two tags: `create table T { … } static tags { A.Two = "2" } tags { A.One = "1" };` now prints as `} tags { static A.Two = "2", A.One = "1" };`.

**Arm C — a multi-variable declaration was split up.** `var a : X, b : Y` is one statement in the source but a plain `Block` of two `VariableStatement`s in the tree (`Parser.VariableStatement`), and `EmitBlock` printed that block's statements one per line. In a statement list that was only a reshape; in a single-statement position it changed what the script does — `if c then var a : Integer, b : String;` printed as a script that declares `b` unconditionally. Four changes, which only work together:

- `EmitStatement`'s `Block` branch now routes a *variable-statement group* — a plain `Block` of two or more statements, all `VariableStatement` — to a new `EmitVariableStatementGroup`, which prints `var a : X := e, b : Y`. `EmitVariableStatement`'s body was split into an `EmitVariableDeclaration` helper that both callers use. The predicate is `IsVariableStatementGroup`.
- `EmitDelimitedBlock` prints the statements of its single plain-`Block` child directly instead of handing that child to `EmitStatement`. See *Gap 1* — this is not needed for correctness, and what it actually buys is a stray `;` and a blank line removed from every `begin … end`.
- `D4TextEmitter` overrides `InternalEmit` so the outermost statement of a script — the statement list `Parser.ParseScript` builds, which no token in the source stands for — is printed as a list. Without it a script whose statements are *all* variable declarations (`tests/libraries/TestFramework.Coverage.Scripts/Documents/Conditionals.d4`, twelve `var`s) would meet the group rule and print as one statement.

A `NOTE:` at `EmitMetaData` records the constraint behind Arm B: `Parser.MetaData` calls `MetaDataTags` exactly twice, so the grammar accepts at most two tag groups, and anything that printed more than one group here could produce text the parser cannot read back.

## Tests

Nineteen `emitter-reshape` entries removed from `ParserRoundTripCorpusTest.AllowList`, so those files are now checked by the corpus pass. The category is gone and the fixture's header comment was rewritten to say so; the thirty-eight `parse` entries remain and the comment now names their owner, `bug-d4-parser-rejects-active-sources` (in `tickets/plan/`).

Cases added to `D4EmitterRoundTripTest.EmittedTextReParsesToTheSameTree`, each verifying that printing the tree and reading it back gives the same tree:

| Case | What it pins |
|---|---|
| `if c then var a, b;` | Arm C's behaviour fix: the group stays one statement in an `if` arm (this is the case that used to declare `b` unconditionally) |
| `while c do var a, b;` | the same in a loop body |
| `try F(); except on E do var a, b; end;` | the same in an exception handler |
| `for var i := 1 to 2 do var a, b;` | the same in a `for` body — Arm C meeting the shape `bug-d4-parser-desugars-for-and-try-commit` introduced |
| `begin var a; var b; end;` and `begin var a, b; end;` | the two spellings that parse to the same tree, so one printing serves both |
| `var a; var b;` | the `InternalEmit` override: a script of only declarations must stay a list of statements, not become one group |
| `var a := 1, b : Integer, c : Integer := 2;` | `EmitVariableDeclaration` with an initializer only, a type specifier only, and both |
| mixed metadata, two tags and four | Arm B, on both of `EmitTags`'s layout branches (two tags single-line, four multi-line) |
| `var x := T return 1;` and `var x := T return 1 by { ID };` | Arm A, with and without the clause |

No test for the `Count >= 2` / all-`VariableStatement` arms of `IsVariableStatementGroup` on their own, for `EmitMode.ForRemote`, or for a tag value containing a doubled quotation mark: the first is the predicate the cases above exercise from both sides, and the other two are unchanged code paths.

One case was written and dropped: `if c then F(); else var a, b;` is not D4 (the `;` ends the `if`), and the else arm is the same `EmitStatement` call as the then arm, so it would have been a duplicate rather than a second behaviour.

## Verification

Build clean, 0 errors, 0 new warnings. Measured on this tree:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest"
```

384 passed, 0 failed — 370 corpus files (19 more than before), the two `TreesEqual` smokes, and 12 emitter cases. A 515-case run adding `FrontendDerivationGoldenTest`, `CatalogStoreTest`, `StoreRebuildWorkflowTest`, `FormCustomizationTest` and `FormDocumentCorpusTest` also passed; no derivation golden moved, so no golden file was touched.

The three suite parts from the root `AGENTS.md`, each in the foreground:

| part | result | wall clock |
|---|---|---|
| 1 (everything but the coverage scripts) | 652 passed, 0 failed (+ 13 in `Common.Tests`) | 6 m 7 s |
| 2 (coverage scripts A–R) | 67 passed, 11 skipped, 0 failed | 3 m 7 s |
| 3 (coverage scripts, the rest) | 50 passed, 12 skipped, 0 failed | 3 m 12 s |

The skips are the pre-existing known-failure scripts; no new failure anywhere, and nothing written to `tickets/.pre-existing-error.md`.

## Gaps and things to look at

**Gap 1 — the stated reason for one part of Arm C does not hold, though the change was kept.** The implement ticket said that without the `EmitDelimitedBlock` change, `begin var a; var b; end` "would print as `begin var a, var b; end`, adding a level". It would not: `DelimitedBlock(string[])` unwraps a one-statement list, so `begin var a, b; end` parses to the same `DelimitedBlock { Block { var a, var b } }`, and either text round-trips — the ticket's own parenthetical says as much. What the change actually buys is that a `begin … end` no longer gains a stray `;` and a blank line from printing its child as a nested statement: `begin F(); G(); end` now prints as three clean lines. That is worth having, so it stayed, but a reviewer should weigh it on that basis rather than on the ticket's claim.

**Gap 2 — a consequence of that change, worth a judgment call.** Inside `begin … end` a multi-variable declaration is now printed as separate statements: `begin var a, b; end` comes out as `begin var a; var b; end`. It round-trips to the same tree and runs identically (a plain `Block` pushes no symbol frame — `Compiler.CompileBlock`), but the printed text is not the source's spelling, where everywhere else Arm C goes the other way. Keeping the group there instead would mean testing for the group before taking the direct-print path in `EmitDelimitedBlock`; it is a two-line change if the reviewer prefers it.

**Gap 3 — a formatting change in mixed metadata beyond what the ticket described.** `EmitTags` switches to its multi-line layout at more than two tags. Metadata with, say, two dynamic and two static tags used to print as two single-line groups and now prints as one four-tag multi-line group. Only mixed metadata is affected (an all-static or all-dynamic set keeps its old count and its old layout), it round-trips, and no repository file's printing regressed — but it is a wider text change than "mixed sets print as one group" suggests.

**Gap 4 — the reason the `InternalEmit` override matters is wider than the script case.** `RemoteServerPlan` (`src/engine/Server/Server/RemoteServerPlan.cs:272`) hand-builds a plain `Block`, appends `VariableStatement`s for the process locals, and hands the block straight to `Emit`. The override is what keeps that printing as a statement list rather than, in the degenerate case where the block holds nothing but those declarations, as one `var` statement. Nothing tests that path; `EmitMode.ForRemote` printing has no coverage at all today.

**Gap 5 — plan-emitted trees are a separate shape and were not touched.** `DelimitedBlockNode.EmitStatement` (`src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs:84`) builds a `DelimitedBlock` holding its children directly, which is not the shape the parser gives; its existing `NOTE:` already records that nothing compares a plan-emitted tree with a re-parse of its text. My `EmitDelimitedBlock` change only takes its new path when there is exactly one child and it is a plain `Block`, so a plan-emitted block with any other child count falls through to the old code. Worth confirming that reading of the guard.

**Gap 6 — no test covers mixed metadata through the server.** The round-trip cases are at the parser/emitter layer. `CatalogStoreTest` and `StoreRebuildWorkflowTest` pass, but I did not check whether either stores an object whose metadata mixes static and dynamic tags, so the end-to-end path for Arm B (store printed DDL, read it back, compare the tags) is covered only by the corpus files that happen to have that shape — `src/engine/DAE/Schema/DataTypes.d4` and `SystemCatalog.d4` among them, which is what the bootstrap reads.

## Compatibility

`docs/migration-notes.md` has a new row under *D4, catalog and libraries* covering all three arms: nothing for an application, a library or a catalog script; regenerate catalog scripts produced by an earlier 4.0 preview; a custom C# extension that compares emitted D4 as text or parses it sees the three differences, and one that derives from `D4TextEmitter` gains `EmitVariableDeclaration`, `IsVariableStatementGroup`, `EmitVariableStatementGroup` and an `InternalEmit` override — and an override of `EmitVariableStatement` is no longer reached for the variables of a multi-variable declaration, which is the one source-compatible-but-behaviour-changing member on that list.

No D4 semantics, operator signature, class name, assembly name, file format, error code, WCF contract or port changed.
