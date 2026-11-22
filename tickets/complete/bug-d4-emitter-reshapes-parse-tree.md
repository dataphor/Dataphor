description: The server used to reorder metadata tags, add a sort clause nobody wrote, and split a multi-variable declaration when it printed stored D4. All three are fixed, and the nineteen files that were excluded from the parse-print-parse check are back in it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, src/engine/DAE/Language.D4/Language.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, docs/migration-notes.md, docs/modernization-plan.md
----
## What landed

Three independent defects in `src/engine/DAE/Language.D4/D4Emitter.cs`, all of the same kind: the printed text did not say what the parse tree said. The server prints D4 through this emitter whenever it stores catalog DDL, scripts a catalog out, or shows a user the D4 behind an object, so the output is a script somebody runs back.

**Arm A — a sort clause the source did not have.** `EmitQuotaExpression` printed the `by { … }` list unconditionally, ignoring `QuotaExpression.HasByClause`, so `T return 1` printed as `T return 1 by {  }` and read back with the flag set. That is not a cosmetic difference: the compiler orders a quota by no columns where the flag is set and by the table's clustering key where it is not (`Compiler.CompileQuotaExpression`). The list is now wrapped in `if (expression.HasByClause)`, as the three sibling `by`/`order by` sites in the file already were.

**Arm B — metadata tags came back in a different order.** `EmitMetaData` walked the tags twice, printing the dynamic ones as `tags { … }` and the static ones as `static tags { … }`, so a source that wrote them mixed got them back grouped. It is now one pass in source order: the grammar lets a tag carry its own `static`/`dynamic` modifier (`Parser.MetaDataTag`), so an all-static set still prints `static tags { … }`, an all-dynamic set still prints `tags { … }` — byte for byte as before — and only a mixed set changes shape, to one `tags { … }` group with `static` in front of the tags that need it. `EmitTags` took one extra parameter, `qualifyStaticTags`, for that. The filter is unchanged (a static tag always prints; a dynamic one only when `EmitMode` is `ForRemote` or it is not inherited) and is now covered by a test.

Measured output, mixed two tags: `create table T { … } static tags { A.Two = "2" } tags { A.One = "1" };` now prints as `} tags { static A.Two = "2", A.One = "1" };`.

**Arm C — a multi-variable declaration was split up.** `var a : X, b : Y` is one statement in the source but a plain `Block` of two `VariableStatement`s in the tree (`Parser.VariableStatement`), and `EmitBlock` printed that block's statements one per line. In a statement list that was only a reshape; in a single-statement position it changed what the script does — `if c then var a : Integer, b : String;` printed as a script that declares `b` unconditionally. Four changes, which only work together:

- `EmitStatement`'s `Block` branch routes a *variable-statement group* — a plain `Block` of two or more statements, all `VariableStatement` — to `EmitVariableStatementGroup`, which prints `var a : X := e, b : Y`. `EmitVariableStatement`'s body was split into an `EmitVariableDeclaration` helper that both callers use. The predicate is `IsVariableStatementGroup`.
- `EmitDelimitedBlock` prints the statements of its single plain-`Block` child directly instead of handing that child to `EmitStatement`. This is not needed for correctness (see *Finding 3*); what it buys is a stray `;` and a blank line removed from every `begin … end`.
- `D4TextEmitter` overrides `InternalEmit` so the outermost statement of a script — the statement list `Parser.ParseScript` builds, which no token in the source stands for — is printed as a list. Without it a script whose statements are *all* variable declarations (`tests/libraries/TestFramework.Coverage.Scripts/Documents/Conditionals.d4`, twelve `var`s) would meet the group rule and print as one statement.

The three-way test for "a plain `Block`, not a `begin … end` one" that these changes all turn on is now a named helper, `IsStatementList`, rather than three copies of `(x is Block) && !(x is DelimitedBlock)`. That predicate is the emitter's end of the canonical-shape rule written on `Parser.DelimitedBlock(string[])`, and naming it says so.

A `NOTE:` at `EmitMetaData` records the constraint behind Arm B: `Parser.MetaData` calls `MetaDataTags` exactly twice, so the grammar accepts at most two tag groups, and anything that printed more than one group here could produce text the parser cannot read back.

## Review findings

Read the implement diff (`ticket(implement): bug-d4-emitter-reshapes-parse-tree`) against the parser and compiler before the handoff summary, then re-derived each arm from `Parser.MetaDataTag`, `Parser.VariableStatement`, `Parser.DelimitedBlock`, `Parser.QuotaClause`, `Tags.Add`, `Compiler.CompileQuotaExpression` and `QuotaNode.EmitStatement`.

**Fixed in this pass.**

1. **A latent defect in how the `by` clause is represented** (`src/engine/DAE/Language.D4/Language.cs`). `QuotaExpression(Expression, Expression, OrderColumnDefinition[])` — the only public way to build such a tree with columns from C# — filled `Columns` but never set `HasByClause`, and both readers of the columns consult that flag. Before Arm A the emitter printed the columns regardless, so the hole was invisible on the print path; after it, a quota built that way printed with no `by` clause and lost its ordering. Worse, and already true before this ticket: the compiler ignored those columns and ordered by the clustering key instead. The constructor now sets the flag, which makes "columns present, flag clear" unreachable through the public surface. Nothing in the repository uses that constructor (the parser and `QuotaNode` both set the flag themselves), so this is for custom C# extensions; the migration row says so. Checked the two sibling types that carry a `HasByClause` (`D4IndexerExpression` and the aggregate/extractor column expression): neither has a constructor that takes order columns, so there is no class here to generalize — a point fix is right.

2. **One copy of a predicate, three readers.** Extracted `IsStatementList` as above. No behaviour change.

3. **The plan's own work-item row was never marked done** (`docs/modernization-plan.md`, WS3 work-item table). `test-parser-roundtrip-corpus` sat with a one-line scope and no **Done** paragraph, unlike every completed sibling row, while the fixture has existed for several tickets. The row now says what the pass does, that all three classes of defect it found are retired and which ticket retired each, and that what remains is one allow list of 38 files the parser cannot read, owned by `bug-d4-parser-rejects-active-sources`.

4. **The handoff's test arithmetic was wrong.** It read "370 corpus files (19 more than before)" and "12 emitter cases". The corpus makes one case per `.d4` file it finds minus the allow list, which is 350 today, and the emitter fixture held 32 cases, of which 12 were new. The total it reported, 384, was right. Corrected here so nobody later reconciles against the wrong figures.

**Tests.** The implementer's twelve cases all pin distinct behaviour at the lowest layer that shows it and none restate the implementation, so none were cut. Three cases added, each for a contract the handoff had left to inspection:

| Case | What it pins |
|---|---|
| `Quota_EmptyByClause_IsNotNoByClause` | `T return 1 by { }` — the flag set with no columns — still prints and still reads back with the flag set. This is the boundary of Arm A's new guard: a later "simplification" to `Columns.Count > 0` would read back as a quota ordered by the clustering key, a silent change of meaning |
| `QuotaExpressionBuiltWithColumnsPrintsItsByClause` | the reproduction of finding 1, at the constructor |
| `MetaDataFilter_ForCopy_…` / `MetaDataFilter_ForRemote_…` | the filter Arm B's one ordered pass had to carry over unchanged: a static tag prints in either mode, a dynamic one that is inherited prints only `ForRemote`. Nothing in the suite printed with `EmitMode.ForRemote` before — this was the handoff's *Gap 4* |

**Checked and found correct — no change.** Each of these is a claim the handoff flagged for a reviewer:

- *The `begin … end` change is not load-bearing* (handoff *Gap 1*). Confirmed, and the implementer's rebuttal of the implement ticket is the right one: `DelimitedBlock(string[])` returns its single statement rather than a one-statement block, so `begin var a, b; end` and `begin var a; var b; end` parse to the **identical** tree, and either text is a correct printing of it. The change is kept for what it does buy — `begin F(); G(); end` no longer gains a trailing `;;` and a blank line — and the comment at the site states that reason, not the implement ticket's.
- *A group inside `begin … end` prints as separate statements* (*Gap 2*). Declined, not deferred. Since both spellings give the identical tree, there is no source spelling to preserve and nothing is lost; the suggested two-line special case would add a branch to buy one layout over an equally correct one.
- *Routing a group to one statement is safe in every position it can occur.* `var a, b` is a valid single statement wherever a statement is allowed, so the risk runs the other way: a position where a plain `Block` must stay a *list*. There are exactly two — the script root and the single child of a `begin … end` — and both are handled. Walked every `EmitStatement`/`EmitTerminatedStatement` call site in the file (`if` arms, `while`, `do … until`, `case` arms, try/finally/except arms, `on` handlers, operator and accessor bodies) to confirm there is no third.
- *A nested plain `Block` can only be a variable group.* The other producers — the `for` and `try … commit` desugars from `bug-d4-parser-desugars-for-and-try-commit` — wrap their block in a `DelimitedBlock`, so no non-group block is ever flattened into its parent list.
- *The stray `;` the old path emitted was never a tree difference.* `DelimitedBlock(string[])` skips a bare statement terminator instead of building an `EmptyStatement`, so the old output round-tripped; removing it is tidiness, not a fix.
- *`EmitMetaData`'s one collection cannot collide.* `Tags.Add` throws on a duplicate name, but both kinds of tag now go into one list from `metaData.Tags`, which is already unique by name, and `Add` appends, so order is the source's.
- *A mixed group is readable back.* `Parser.MetaDataTag` accepts a per-tag `static`/`dynamic` modifier that overrides the group's, so the one-group output is in the grammar; the `NOTE:` that the grammar allows at most two groups is accurate (`Parser.MetaData` calls `MetaDataTags` exactly twice).
- *The plan-emitted `DelimitedBlock` shape falls through the new guard* (*Gap 5*). Confirmed: `DelimitedBlockNode.EmitStatement` holds its children directly, so the guard's "exactly one child and it is a plain `Block`" is false for it and the old path runs. Its existing `NOTE:` already records that nothing compares a plan-emitted tree with a re-parse of its text.
- *`RemoteServerPlan`* (`src/engine/Server/Server/RemoteServerPlan.cs:272`) hands a hand-built plain `Block` straight to `Emit`, so the `InternalEmit` override is what keeps it a statement list. Read it; the override covers it. In that particular degenerate case the group printing would in fact have compiled the same, but the override is correct and costs nothing.
- *Mixed metadata end to end* (*Gap 6*). Not separately tested, and does not need to be: `src/engine/DAE/Schema/DataTypes.d4` and `SystemCatalog.d4` both carry mixed metadata and are what the engine reads at bootstrap, so all 656 tests in part 1 exercise that path, and both files are back in the corpus pass.

**Considered and declined.** `EmitTags` now takes two booleans that encode three states, with one combination (`static` group *and* per-tag qualifiers) unreachable; both are functions of the `tags` argument and could be derived inside the method. Left as it is: the method is private with one caller, the impossible combination is benign if it ever arose (it would print `static tags { static A = "1" }`, which means the same thing), and deriving them means adding a counting loop inside the file's dead-and-broken `#if USEHASHTABLEFORTAGS` region. Not worth the churn on a reviewed diff.

**Filed as tickets: none.** Everything found was either a one-line fix made here or a claim that held up. The one latent defect (finding 1) is fixed rather than filed because its fix is one line at the one site; no finding named a class-level invariant worth a `debt-` ticket, and none was conditional, so there is nothing parked as a tripwire either.

## Tests

Nineteen `emitter-reshape` entries removed from `ParserRoundTripCorpusTest.AllowList`, so those files are now checked by the corpus pass. The category is gone and the fixture's header comment was rewritten to say so; the thirty-eight `parse` entries remain and the comment now names their owner, `bug-d4-parser-rejects-active-sources` (in `tickets/plan/`).

Cases added to `D4EmitterRoundTripTest` by the implement pass, each verifying that printing the tree and reading it back gives the same tree:

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

Plus the three the review added, in the table above. No test for the `Count >= 2` / all-`VariableStatement` arms of `IsVariableStatementGroup` on their own, or for a tag value containing a doubled quotation mark: the first is the predicate the cases above exercise from both sides, and the second is an unchanged code path.

One case was written and dropped during implement: `if c then F(); else var a, b;` is not D4 (the `;` ends the `if`), and the else arm is the same `EmitStatement` call as the then arm, so it would have been a duplicate rather than a second behaviour.

## Verification

Build clean, 0 errors, no new warnings. Measured on this tree:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~D4EmitterRoundTripTest"
```

36 passed, 0 failed in the emitter fixture (32 from implement, 3 cases and one new method from review). The combined corpus + emitter filter gave 386 passed, 0 failed: 350 corpus files (19 more than before this ticket), the two `TreesEqual` smokes, and the 34 emitter cases.

The three suite parts from the root `AGENTS.md`, each in the foreground:

| part | result | wall clock |
|---|---|---|
| 1 (everything but the coverage scripts) | 656 passed, 0 failed (+ 13 in `Common.Tests`) | 6 m 5 s |
| 2 (coverage scripts A–R) | 67 passed, 11 skipped, 0 failed | 3 m 5 s |
| 3 (coverage scripts, the rest) | 50 passed, 12 skipped, 0 failed | 3 m 6 s |

Part 1 was run again after the review's test additions (656 = the implement pass's 652 plus four new cases). Parts 2 and 3 are the `CoverageScripts` fixture alone, which neither the review's code change nor its tests touch; their figures are the run made after the code change and before the test additions. The skips are the pre-existing known-failure scripts; no new failure anywhere, and nothing written to `tickets/.pre-existing-error.md`.

No derivation golden moved (`FrontendDerivationGoldenTest` is in part 1), so no golden file was touched.

## Compatibility

`docs/migration-notes.md` has a new row under *D4, catalog and libraries* covering all three arms plus the constructor fix: nothing for an application, a library or a catalog script; regenerate catalog scripts produced by an earlier 4.0 preview; a custom C# extension that compares emitted D4 as text or parses it sees the three differences, and one that derives from `D4TextEmitter` gains `EmitVariableDeclaration`, `IsVariableStatementGroup`, `EmitVariableStatementGroup` and an `InternalEmit` override — and an override of `EmitVariableStatement` is no longer reached for the variables of a multi-variable declaration, which is the one source-compatible-but-behaviour-changing member on that list. The row also records that `QuotaExpression`'s three-argument constructor now sets `HasByClause`, so a quota built that way keeps its `by` clause in printed text and compiles to a plan ordered by the columns it was given rather than by the clustering key. `IsStatementList` is private and adds nothing to that surface.

No D4 semantics, operator signature, class name, assembly name, file format, error code, WCF contract or port changed.
