description: The D4 parser used to rewrite "for" loops and "try … commit" blocks into a shape of parse tree that no D4 text can produce; it now builds the shape the equivalent explicit D4 produces, so printing such a script and reading it back gives the tree that was printed.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/Parser.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, docs/migration-notes.md
repro: verified
----
## What landed

The D4 parser rewrites two constructs as it reads them: `for i := 1 to 3 do <body>` becomes a variable declaration, an assignment and a `while` loop wrapped in a `begin … end`, and `try <body> commit;` becomes a call to `BeginTransaction()` followed by a `try … except RollbackTransaction(); raise; end`. In both cases the block the parser built held those statements **directly**, and a `begin … end` the parser reads from text always holds exactly one child — the statement list. So the tree had a shape no D4 text parses to, and printing it through `D4TextEmitter` and reading the text back could not give the same tree. Thirty-four `.d4` files in the repository were excluded from `ParserRoundTripCorpusTest` for that reason.

`ticket(implement): bug-d4-parser-desugars-for-and-try-commit` made three changes in `src/engine/DAE/Language.D4/Parser.cs`:

- **`ForStatement()`** now returns `DelimitedBlock { Block { … } }` — the outer `DelimitedBlock` is the `begin … end` (and is what gives the loop variable its own symbol frame), the inner plain `Block` is the statement list. The `TryFinallyStatement` that runs the body and then the increment keeps **bare** statements in both arms, because a one-statement list is that statement.
- **`ForStatement()`'s default step** is now the boxed `long` `1L`, matching the literal the lexer produces (`Lexer.Token.AsInteger` returns `long`).
- **The `commit` arm of `TryStatement()`** now returns `DelimitedBlock { Block { BeginTransaction(), TryExcept } }`. The body is **spliced** into the try arm when it is a plain `Block` (several body statements belong in the arm's own list) and **stays nested** when it is a `DelimitedBlock` (a `begin … end` the source wrote, whose children belong inside its own frame); a try arm left with one statement is unwrapped to that statement, which is the `try commit;` empty-body case. The handler block is now a plain `Block`, matching what `except RollbackTransaction(); raise; end` parses to.

The rule those three follow is written down as a block comment on `Parser.DelimitedBlock(string[])`, which is where all of it is read off, and both desugaring sites point at it.

Also landed: the thirty-four `AllowList` entries removed from `ParserRoundTripCorpusTest`, nine cases in `D4EmitterRoundTripTest.EmittedTextReParsesToTheSameTree` pinning the arms of the new shape, a `NOTE:` tripwire on `DelimitedBlockNode.EmitStatement`, and a row in `docs/migration-notes.md`.

The review pass then made three small corrections and added one test case; they are in `## Review findings`.

## Why no semantics moved

`Compiler.CompileBlock` builds a `BlockNode` and pushes no symbol frame; only `CompileDelimitedBlock` pushes one. Adding or removing a plain `Block` level therefore changes neither variable scope nor lifetime, and statement order is identical in every case. Two further seams were checked during review and hold:

- `Compiler.CompileTryExceptStatement` wraps the try arm in `CompileFrameNode` and pushes a frame of its own around every error handler, so splicing the body into the try arm, and demoting the handler's `DelimitedBlock` to a plain `Block`, cannot move a declaration's scope — the frame the handler's block used to push was redundant.
- `Compiler.CompileDeallocateFrameVariablesNode` walks `plan.Symbols.FrameCount`, not the statement tree, so the extra block level does not change what gets deallocated.

The outer `DelimitedBlock` is deliberately kept so `for var i := …` still gets its frame.

## Where it is verified

`ParserRoundTripCorpusTest` round-trips 331 of the 388 `.d4` files in the repository (up from 297); the 57 still on its allow list are 38 that the parser cannot read and 19 the emitter reshapes, each with its reason. `D4EmitterRoundTripTest.EmittedTextReParsesToTheSameTree` holds the two desugarings' shapes with ten snippet cases — the empty, one-statement, several-statement, `begin … end` and multi-variable-`var` bodies of `try … commit`, and the `for` variants (no `var`, `var` with an inferred and with an explicit type specifier, `downto` with a step, and nested).

## Validation

```
dotnet build Dataphor.slnx --no-incremental                  0 errors, 143 warnings (all pre-existing)
dotnet test tests/DAE.Server.Tests --no-build \
  --filter "FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest"
                                                             353 passed, 0 failed
```

The whole suite, in the three parts from the root `AGENTS.md`, all green:

```
part 1 (everything but the coverage scripts)   621 passed + 13 (Common.Tests), 0 failed, 6 m 7 s
part 2 (coverage scripts A-R)                   67 passed, 11 skipped, 0 failed, 3 m 7 s
part 3 (coverage scripts other)                 50 passed, 12 skipped, 0 failed, 3 m 6 s
```

The skips are the usual ones (scripts needing a SQL Server device, and the `parse`-category documents). No pre-existing failure surfaced, so `tickets/.pre-existing-error.md` was not written.

## Review findings

### Checked

- **The shape claim itself.** For every arm of both desugarings, worked out what parsing the equivalent explicit D4 gives and compared it with what the parser now builds. They agree, including the two cases the implementation treats specially: a several-statement `try … commit` body (spliced, because the equivalent text puts those statements in the try arm's own list) and a `begin … end` body (nested, because the equivalent text keeps them inside their own block).
- **Thirteen interaction cases the suite does not cover**, run through a throwaway fixture and then deleted: a `try … commit` whose body is `var a, b`, a lone `;`, three statements, a `for`, or another `try … commit`; a `for` whose body is empty, a `begin … end`, a `var a, b`, a `try … finally`, or another `for`; a `for` inside a `begin … end`; a `for var` whose body declares its own variable; and a statement following a `try … commit`. All round-trip. One of them is now a permanent case (below).
- **The boxed `1L`.** `Compiler.CompileValueExpression` converts with `Convert.ToInt64`, so `1` and `1L` compile identically, and the round-trip comparator compares `GetType()` before value — so the change is load-bearing, not cosmetic. The only other places that synthesize an integer `ValueExpression` are on the plan-to-statement path, which nothing re-parses.
- **The allow list, entry by entry.** Re-ran the round trip over all 57 remaining entries: every one still fails for exactly the category it claims, so nothing is stale. The three causes the reshape comment now names are the only ones present — metadata tag ordering (5 files), a `by` clause the source did not have (3), and the statement-count growth from the emitter splitting a multi-variable `var` (11).
- **Every other desugaring in the parser.** `RepeatStatement()` builds a `DoWhileStatement` with a `not` condition, which re-parses to the identical shape, and `VariableStatement()`'s multi-variable block is itself a canonical statement list. No third site needs the rule applied.
- **The debugger.** `IsBreakable` is a per-plan-node flag and neither `ServerProcess.InternalShouldBreak` nor `Debugger.ShouldBreak` dedupes by source location, so the extra `BlockNode` means one more step stop per `for` or `try … commit` **entry** — not per iteration. A `begin … end` written in the source already does exactly this, so the change makes a `for` step the way its explicit equivalent steps. Worth knowing; not a defect, and not worth a comment at the site.
- **Build and lint.** `dotnet build Dataphor.slnx --no-incremental`: 0 errors, 143 warnings, the same count as before the ticket. The project has no lint step beyond the analyzers the build runs.

### Fixed in this pass (minor)

- **The splice guard no longer depends on `DelimitedBlock` being the only `Block` subclass**, which is the fragility the implement handoff flagged. `tryStatement as Block` with `!(… is DelimitedBlock)` became `tryStatement.GetType() == typeof(Block)`: right whatever subclasses appear later, and the same notion of a statement's shape that `Compiler.InternalCompileStatement` uses (it dispatches on `GetType().Name`, so a new `Block` subclass needs an arm there anyway). No behaviour change today.
- **The inner `Block` of the `commit` arm carries the construct's own position**, as `ForStatement`'s pair already does and as parsing `begin … end` gives, instead of wherever the lexer happened to be (the `commit` token). Nothing asserts positions — the comparator ignores them — so the visible effect is only that the extra breakable node lands on the `try` line rather than the `commit` line.
- **`docs/migration-notes.md` row corrected.** It said the compiled plan was unchanged and described the tree change as "one plain block level more under a `for` and one less inside a desugared `try … commit`". All of that understates it: the extra plain block level appears under the `try … commit` too, the `except` handler's block changed from a `begin … end` block to a plain one, and the plan tree gains a `BlockNode` under each construct — which shows up in the plan XML `PlanNode.WritePlan` writes, a surface Dataphoria displays. The row now says what actually changed and that what the plan *does* is unchanged.

### Added (one test)

- **`TryCommit_MultiVariableDeclarationBody`** (`try var a, b; commit;`). `VariableStatement()` is the second producer of a plain `Block` — it builds one directly for a multi-variable `var` rather than returning it from `DelimitedBlock` — so it is the other body the splice arm meets, and the arm the guard change above has to keep right. Nothing pinned it. The other nine cases each pin a distinct branch and none was cut.

### Nothing filed, and why

- **No major findings, so no tickets.** The implementation does what it claims, in the place the claim is read off, and the three corrections above are all small enough to belong in this pass.
- **No new tripwires.** The handoff's `NOTE:` on `DelimitedBlockNode.EmitStatement` is in place and still accurate. The same non-canonical shape sits on two more plan-to-statement paths — `ValueNode.EmitStatement` boxes an `int` for a `SystemInteger`, and `Compiler.CompileUpdateStatement`'s table-update rewrite builds a `DelimitedBlock` holding statements directly — but all three are one fact, and that one comment already says what to do if anything ever compares a plan-emitted tree with a re-parse of its text. A second and third copy of it would add nothing.
- **The emitted text is still noisy** (`end;;;` and a blank line per block level, confirmed by eye). It is cosmetic, in text that is always regenerated, and the corpus comparator ignores empty statements; `2-bug-d4-emitter-reshapes-parse-tree`, in `plan/`, owns it. Not re-filed.
- **The `parse`-category allow-list entries** are untouched here; `bug-d4-parser-rejects-active-sources`, in `plan/`, edits those.
