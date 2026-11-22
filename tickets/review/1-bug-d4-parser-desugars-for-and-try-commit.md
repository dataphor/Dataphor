description: The D4 parser used to rewrite "for" loops and "try … commit" blocks into a shape of parse tree that no D4 text can produce; it now builds the shape the equivalent explicit D4 produces, so printing such a script and reading it back gives the tree that was printed.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/Parser.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, docs/migration-notes.md
difficulty: medium
repro: verified
----
## What landed

The D4 parser rewrites two constructs as it reads them: `for i := 1 to 3 do <body>` becomes a variable declaration, an assignment and a `while` loop wrapped in a `begin … end`, and `try <body> commit;` becomes a call to `BeginTransaction()` followed by a `try … except RollbackTransaction(); raise; end`. In both cases the block the parser built held those statements **directly**, and a `begin … end` the parser reads from text always holds exactly one child — the statement list. So the tree had a shape no D4 text parses to, and printing it through `D4TextEmitter` and reading the text back could not give the same tree. Thirty-four `.d4` files in the repository were excluded from `ParserRoundTripCorpusTest` for that reason.

Three changes in `src/engine/DAE/Language.D4/Parser.cs`:

- **`ForStatement()`** now returns `DelimitedBlock { Block { … } }` — the outer `DelimitedBlock` is the `begin … end` (and is what gives the loop variable its own symbol frame), the inner plain `Block` is the statement list. The `TryFinallyStatement` that runs the body and then the increment keeps **bare** statements in both arms, because a one-statement list is that statement.
- **`ForStatement()`'s default step** is now the boxed `long` `1L`, matching the literal the lexer produces (`Lexer.Token.AsInteger` returns `long`). This is a tree-shape matter only: `Compiler.CompileValueExpression` converts, so both box types compiled identically.
- **The `commit` arm of `TryStatement()`** now returns `DelimitedBlock { Block { BeginTransaction(), TryExcept } }`. The body is **spliced** into the try arm when it is a plain `Block` (several body statements belong in the arm's own list) and **stays nested** when it is a `DelimitedBlock` (a `begin … end` the source wrote, whose children belong inside its own frame); a try arm left with one statement is unwrapped to that statement, which is the `try commit;` empty-body case. The handler block is now a plain `Block`, matching what `except RollbackTransaction(); raise; end` parses to.

The rule those three follow is written down as a block comment on `Parser.DelimitedBlock(string[])`, which is where all of it is read off, and both desugaring sites point at it. The next desugaring has to follow the same rule.

**Why no semantics moved.** `Compiler.CompileBlock` builds a `BlockNode` and pushes no symbol frame; only `CompileDelimitedBlock` pushes one. Adding or removing a plain `Block` level therefore changes neither variable scope nor lifetime, and statement order is identical in every case. The outer `DelimitedBlock` is deliberately kept so `for var i := …` still gets its frame.

Also landed: the thirty-four `AllowList` entries, the allow-list comment (its category description no longer blames the emitter for the parser's desugaring), and a row in `docs/migration-notes.md` under *D4, catalog and libraries* — nothing for an application, but a custom C# extension that walks a D4 parse tree sees one plain block level more under a `for` and one less inside a desugared `try … commit`.

## Tests

Nine cases added to `D4EmitterRoundTripTest.EmittedTextReParsesToTheSameTree`, which parses a snippet, prints the tree, re-parses and compares the trees. Each pins one arm of the new shape:

| Case | What it verifies |
|---|---|
| `try commit;` | empty body: the try arm is the single `CommitTransaction()` call, not a one-statement `Block` |
| `try F(); commit;` | one body statement: the arm is `Block { F(), CommitTransaction() }` |
| `try F(); G(); commit;` | several body statements: they are spliced into the arm, not nested in a `Block` |
| `try begin F(); G(); end; commit;` | a `begin … end` body stays nested inside the arm (the `is DelimitedBlock` guard) |
| `for i := 1 to 3 do F();` | no `var`: the inner block holds two statements, not three |
| `for var i := 1 to 3 do F();` | `var` with an inferred type specifier |
| `for var i : Integer := 1 to 3 do F();` | explicit type specifier |
| `for i := 10 downto 1 step 2 do F();` | `downto` with a parsed step literal |
| `for var i := 1 to 2 do for var j := 1 to 2 do F();` | a `for` nested in a `for` body |

The default step's `1L` is not pinned by a case of its own: every `for` case without `step` exercises it, and the round trip is what would catch `1` again.

The fixture's summary used to say its cases were about text the parser could not read; it now says they are about printing a tree and reading back something other than the tree that was printed, which covers both causes.

No new test for the thirty-four files: `ParserRoundTripCorpusTest` already covers them, and taking them off its allow list is the coverage.

## Validation

```
dotnet build Dataphor.slnx                                   0 errors, 143 warnings (all pre-existing)
dotnet test tests/DAE.Server.Tests --no-build \
  --filter "FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest"
                                                             352 passed, 0 failed
```

352 is 331 corpus files + the 19 emitter snippets + the 2 comparator smokes, so the corpus round-trips 331 of the 388 `.d4` files — up from 297, with 19 emitter reshapes and 38 unparseable files left, exactly the numbers the implement ticket measured. No file that round-tripped before stopped doing so.

The whole suite, in the three parts from the root `AGENTS.md`, all green:

```
part 1 (everything but the coverage scripts)   620 passed + 13 (Common.Tests), 0 failed, 6 m 6 s
part 2 (coverage scripts A-R)                   67 passed, 11 skipped, 0 failed, 3 m 7 s
part 3 (coverage scripts other)                 50 passed, 12 skipped, 0 failed, 3 m 7 s
```

The skips are the usual ones (scripts needing a SQL Server device, and the `parse`-category documents); no pre-existing failure surfaced, so `tickets/.pre-existing-error.md` was not written.

## For the reviewer

Worth a second pair of eyes:

- **The splice guard.** `tryStatement as Block` with `!(… is DelimitedBlock)` assumes `DelimitedBlock` is the only `Block` subclass; it is today (`src/engine/DAE/Language/Language.cs:704` is the only one). A new `Block` subclass used for a source-written block would be spliced wrongly.
- **Positions.** The synthesized inner `Block` of a `for` carries the same position as the outer `DelimitedBlock`, and the `commit` arm's inner `Block` takes the lexer's position where the old code took none. Breakpoints on `for` and `try … commit` lines still resolve (the debugger fixtures in part 1 pass), but nothing asserts the line numbers of a synthesized block, so it was checked by inspection, not by a test.
- **Printed text, until the emitter ticket lands.** Printing a `for` or `try … commit` tree now adds one `;` and a blank line per added block level. That is cosmetic, in text that is always regenerated, and the corpus comparator ignores empty statements; `2-bug-d4-emitter-reshapes-parse-tree` removes the noise with its `EmitDelimitedBlock` change. It is the one visible difference in this ticket's output.
- **Allow list overlap.** `bug-d4-parser-rejects-active-sources` (in `plan/`) edits the same file's thirty-eight `parse` entries. Different entries, untouched here.

## Review findings

- Noticed and parked as a tripwire: `DelimitedBlockNode.EmitStatement` (`src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs:85`) builds a `DelimitedBlock` holding its children directly — the same non-canonical shape, on the plan-to-statement path rather than the parse path. It prints correct D4 and nothing compares a plan-emitted tree with a re-parse of its text, so it is fine as it stands; a `NOTE:` at the site says what to do if something ever does compare them.
