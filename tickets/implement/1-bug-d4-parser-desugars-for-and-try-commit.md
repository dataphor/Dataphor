description: The D4 parser rewrites "for" loops and "try … commit" blocks into a shape of parse tree that no D4 text can produce, so printing such a script and reading it back gives a different tree than the one that was printed.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/Parser.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, docs/migration-notes.md
difficulty: medium
repro: verified
----
## What this is

`ParserRoundTripCorpusTest` parses every `.d4` file in the repository, prints the parse tree through `D4TextEmitter`, parses the printed text again and asserts the two trees match. Fifty-three files fail that comparison and sit on the fixture's `AllowList` under the category `emitter-reshape`. Thirty-six of them fail for a reason that is **not** in the emitter: the parser does not build the tree the source describes. It *rewrites* two constructs as it reads them, and the rewritten tree is one that parsing D4 text can never produce — so the emitter has no text it could print that would read back as the same tree, and the round trip cannot be a fixed point for any file containing a `for` loop or a `try … commit` block.

The original ticket (`bug-d4-emitter-reshapes-parse-tree`) attributed this to the emitter. It is the parser; the emitter prints these trees faithfully. The remaining seventeen files are the emitter's own three defects and stay with that ticket, which runs after this one.

Nothing a user sees is wrong today: both shapes compile to the same plan and run the same way (see *Why this is safe* below). What is wrong is that a stored or printed script is not a faithful copy of the tree it came from, and the whole-repository regression net that would catch a future parser or emitter change has to stay switched off for thirty-six files.

## Root cause

Two sites in `src/engine/DAE/Language.D4/Parser.cs`:

- **`ForStatement()`** (around line 3754) rewrites `for i := 1 to 3 do <body>` into a `DelimitedBlock` whose `Statements` holds **three statements directly** — a `VariableStatement` for the loop variable, an `AssignmentStatement` for its initial value and a `WhileStatement` for the loop — plus, inside the loop, a `TryFinallyStatement` that runs the body and then the increment.
- **`TryStatement()`**, the `case Keywords.Commit:` arm (around line 4089), rewrites `try <body> commit;` into a `DelimitedBlock` holding **two statements directly** — a call to `BeginTransaction()` and a `TryExceptStatement` whose try arm is a `Block` holding the body `Block` plus a call to `CommitTransaction()`, and whose handler is a `DelimitedBlock` holding `RollbackTransaction()` and `raise`.

A `DelimitedBlock` is what `begin … end` parses to, and the parser always gives it **exactly one** child — the statement list inside it. So "a `DelimitedBlock` with three children" and "a `Block` directly inside a `Block`" are shapes the grammar cannot express, and the emitter, which can only print text, cannot reproduce them. The reported differences are exactly that: `statements: list length differs (3 vs 1)` and `(2 vs 1)` on a `DelimitedBlock`.

## The canonical-shape rule

The rule a synthesized statement has to follow — worth a comment in the source, because the next desugaring will have to follow it too — is that **a statement the parser builds itself must have the shape that parsing the equivalent explicit D4 text would give**. Three consequences, all of them read off `Parser.DelimitedBlock(string[])` (line 408) and `Parser.VariableStatement()` (line 3619):

- A statement list of exactly one statement **is** that statement. `DelimitedBlock(string[])` ends with `return block.Statements.Count == 1 ? block.Statements[0] : block;`, so the arms of `try`/`finally`/`except`, a `while` body and the inside of `begin … end` hold a bare statement when there is one and a `Block` when there are several. Wrapping a single statement in a `Block` is as wrong as flattening several.
- `begin … end` is a `DelimitedBlock` holding one statement: the list.
- An integer literal is a **boxed `long`** — `Lexer.Token.AsInteger` returns `long` and `Parser` line 1563 boxes that into the `ValueExpression`. A synthesized integer literal must carry `1L`, not `1`, or it compares unequal to the same literal read from text. (`Compiler.CompileValueExpression` goes through `Convert.ToInt32`/`ToInt64`, so both box types compile identically — this is a tree-shape matter only.)

## The change

All four parts below were prototyped against this tree and measured; the numbers under *Verification* are from that run.

**`ForStatement()`** — give the outer `DelimitedBlock` one `Block` child and fill the `Block`:

```
			DelimitedBlock outerBlock = new DelimitedBlock();
			Block block = new Block();
			outerBlock.Statements.Add(block);
			_lexer.NextToken();
			outerBlock.SetPosition(_lexer);
			block.SetPosition(_lexer);
```

and at the end of the method, after `block.SetEndPosition(_lexer);`, add `outerBlock.SetEndPosition(_lexer);` and `return outerBlock;`. Everything in between keeps adding to `block`. **Do not** wrap the `TryFinallyStatement`'s two arms in `Block`s: each arm is a single statement, so a bare statement is the canonical shape (an early prototype wrapped them and nineteen files failed one level deeper).

**`ForStatement()`, the default step** — `stepExpression = new ValueExpression(1L, TokenType.Integer);` where it says `1`.

**`TryStatement()`, the `commit` arm** — same wrapping for the outer block, and the body of the try arm is **spliced**, not nested:

```
					Block commitBlock = new Block();
					commitBlock.SetPosition(_lexer);
					block.Statements.Add(commitBlock);
					commitBlock.Statements.Add(new ExpressionStatement(new CallExpression("BeginTransaction", new Expression[]{})));
					...
					Block tryStatements = tryStatement as Block;
					if ((tryStatements != null) && !(tryStatements is DelimitedBlock))
						for (int tryIndex = 0; tryIndex < tryStatements.Statements.Count; tryIndex++)
							tryBlock.Statements.Add(tryStatements.Statements[tryIndex]);
					else
						tryBlock.Statements.Add(tryStatement);
					tryBlock.Statements.Add(new ExpressionStatement(new CallExpression("CommitTransaction", new Expression[]{})));
					if (tryBlock.Statements.Count == 1)
						commitStatement.TryStatement = tryBlock.Statements[0];
					else
						commitStatement.TryStatement = tryBlock;
```

The `is DelimitedBlock` guard matters: `try begin … end commit;` gives a `DelimitedBlock` for the body, whose children belong inside its own frame and must not be spliced out of it. The `Count == 1` arm matters for `try commit;` with an empty body (`LanguageConstructs.d4` line 77), where the try arm ends up holding only the `CommitTransaction()` call.

**The handler block** becomes a plain `Block` rather than a `DelimitedBlock`: `except RollbackTransaction(); raise; end` is what the emitter prints for it, and that text parses to a `Block`.

## Why this is safe

`Compiler.CompileBlock` (line 1057) builds a `BlockNode` and pushes **no** symbol frame; only `CompileDelimitedBlock` (line 1067) pushes one. So adding or removing a plain `Block` level changes neither variable scope nor lifetime: the loop variable of a `for` still lives in — and is deallocated at the end of — the outer `DelimitedBlock`'s frame, and `CompileDeallocateFrameVariablesNode` reads `plan.Symbols`, not the node tree, so one more `BlockNode` between does not move it. Statement order is identical in every case. The outer `DelimitedBlock` is deliberately kept so that `for var i := …` still gets its frame.

## Edge cases & interactions

| Case | How it is covered |
|---|---|
| `try commit;` — empty body, try arm is one statement | round-trip case in `D4EmitterRoundTripTest`; `LanguageConstructs.d4` in the corpus |
| `try X; commit;` — one body statement, arm is `Block{X, Commit}` | round-trip case |
| `try begin X; Y; end commit;` — body is a `DelimitedBlock`, must not be spliced | round-trip case (verified: passes with the guard) |
| `for` with no `var` and no type specifier — the block has two statements, not three | round-trip case |
| `for … downto … step <expr>` — the step is a parsed literal, so only the default step needs `1L` | round-trip case |
| nested `for` inside a `for` body | round-trip case (verified) |
| loop-variable scope and deallocation | by inspection of `CompileBlock`/`CompileDelimitedBlock`/`CompileDeallocateFrameVariablesNode` (above), and by the `for`-using coverage scripts, which pass |
| debugger line info — the synthesized inner `Block` carries the same position as the outer, so a breakpoint on the `for` line still resolves | by inspection: `SetPosition`/`SetEndPosition` are called on both; the engine tests pass |
| printed text for a `for`/`try … commit` gains one `;` and a blank line per added level until the emitter ticket lands | cosmetic only, in printed text that is always regenerated; the corpus comparator ignores empty statements. `2-bug-d4-emitter-reshapes-parse-tree` removes the noise with its `EmitDelimitedBlock` change |
| no derived-form golden holds imperative D4 (checked: no golden contains `BeginTransaction`, `CommitTransaction` or `while (`), so `FrontendDerivationGoldenTest` cannot move | by inspection + run the fixture |
| the `AllowList` is also edited by `bug-d4-parser-rejects-active-sources` (the thirty-eight `parse` entries) | different entries; re-read the file before editing if that ticket has landed meanwhile |

## Verification

Measured on this tree with the change prototyped (`tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs` logic run over all 388 `.d4` files):

- before: 297 files round-trip, 53 reshape, 38 do not parse
- with this change alone: **331** files round-trip, 19 reshape, 38 do not parse — no file that round-tripped before stopped doing so
- the 34 entries this clears are listed below; the other 19 belong to the emitter ticket

The reproduction is the corpus test itself:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest"
```

With the prototype applied, a wider slice also passed (438 cases: the corpus, the emitter snapshots, `CatalogStoreTest`, `StoreRebuildWorkflowTest`, `FormCustomizationTest`, `FormDocumentCorpusTest`, `InternalCoverageTests`), as did the `LanguageConstructs`, `Conditionals` and `Cursors` coverage scripts and `FrontendDerivationGoldenTest`.

## AllowList entries this ticket removes

All thirty-four with category `emitter-reshape`:

```
legacy/libraries/FHIR.Core/Documents/Structure.Definition.d4
legacy/libraries/FHIR.Server/Documents/FHIR.TestData.d4
legacy/libraries/PHINVADS.Test/Documents/InsertTablePerformance.d4
samples/Sample.Shipping/Documents/Coverage.Common.d4
samples/Sample.Shipping/Documents/Coverage.ManagementRole.d4
samples/Sample.Shipping/Documents/Coverage.ProcessingRole.d4
samples/Sample.Shipping/Documents/Coverage.PurchasingRole.d4
samples/Sample.Shipping/Documents/Coverage.SalesRole.d4
samples/Sample.Shipping/Documents/Schema.ContactTables.d4
samples/Sample.Shipping/Documents/Schema.InvoiceProcessing.d4
samples/Sample.Shipping/Source/CoverageRoles/Common.d4
samples/Sample.Shipping/Source/CoverageRoles/Management.d4
samples/Sample.Shipping/Source/CoverageRoles/Processing.d4
samples/Sample.Shipping/Source/CoverageRoles/Purchasing.d4
tests/libraries/TestFramework.Coverage.Base/Documents/DiagnosticsLogic.d4
tests/libraries/TestFramework.Coverage.Base/Documents/Logic.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/AggregateOperators.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/ApplicationTransactions.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/Cursors.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/DML.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/DeferredConstraintChecking.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/DeferredConstraintsWithKeys.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/EventHandlers.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/KeyAffectingUpdate.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/LongAggregates.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/MaximumRowCount.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/PlanCache.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/SerializationOperators.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/TableLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/TestSargabilityWithConversion.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/Timing.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/Transaction.Atomicity.Run.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/Transaction.Isolation.Run.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/TransitionConstraints.d4
```

## Rejected alternative

Give `for` and `try … commit` their own statement classes and desugar them in the compiler instead of the parser. That is the cleaner design — the parse tree would then mirror the source and the emitter could print `for` and `try … commit` as written — but it means new AST classes, new emitter methods, new compiler entries, and printed catalog text that changes for every loop in every script. The fix above costs about twenty-five lines, changes no printed text that matters and no semantics. Worth revisiting only if a reason to keep the source form in the tree appears (a source-level debugger view, say).

## TODO

- Read `Parser.DelimitedBlock(string[])` (line 408) and `Parser.VariableStatement()` (line 3619) first: they define the canonical shapes this ticket matches.
- Rewrite `ForStatement()` to build `DelimitedBlock { Block { … } }`, keeping the `TryFinallyStatement` arms as bare statements, and change the default step literal to `1L`.
- Rewrite the `case Keywords.Commit:` arm of `TryStatement()` to build `DelimitedBlock { Block { BeginTransaction(), TryExcept } }`, splicing a plain-`Block` body into the try arm, keeping a `DelimitedBlock` body nested, unwrapping a single-statement try arm, and making the handler block a plain `Block`.
- Record the canonical-shape rule as a comment on `Parser.DelimitedBlock(string[])` (one statement is itself, `begin … end` holds one child, an integer literal is a boxed `long`) and point the two desugaring sites at it.
- Add round-trip cases to `D4EmitterRoundTripTest.EmittedTextReParsesToTheSameTree` for: `try commit;`, `try X; commit;`, `try begin X; Y; end commit;`, `for` with and without `var`, `downto` with an explicit `step`, and a nested `for`. Widen the fixture's summary, which today says the cases are about text the parser could not read.
- Remove the thirty-four `AllowList` entries above; leave the other nineteen and all thirty-eight `parse` entries alone.
- Add a `docs/migration-notes.md` row under *D4, catalog and libraries*: the parse tree for `for` and `try … commit` now has the shape the equivalent explicit D4 has; nothing for an application, but a custom C# extension that walks a D4 parse tree sees one `Block` level more under a `for` and one less inside a desugared `try … commit`.
- Build, then run the three suite parts from the root `AGENTS.md` (never `dotnet test Dataphor.slnx` as one command), each as its own foreground command.
