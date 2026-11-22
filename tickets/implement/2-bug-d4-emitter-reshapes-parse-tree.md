description: When the server prints a stored D4 script it reorders metadata tags, adds a sort clause nobody wrote, and splits a multi-variable declaration into separate statements — which in a conditional moves all but the first declaration outside the condition.
prereq: bug-d4-parser-desugars-for-and-try-commit
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, src/engine/DAE/Language/Emitter.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, docs/migration-notes.md
difficulty: medium
repro: verified
----
## What this is

`ParserRoundTripCorpusTest` parses every `.d4` file in the repository, prints the parse tree through `D4TextEmitter`, parses the printed text again and asserts the two trees match. Of the fifty-three files that used to fail that comparison, thirty-six were the parser's doing and are fixed by `bug-d4-parser-desugars-for-and-try-commit`, which this ticket follows. The remaining **nineteen** are three independent defects in `src/engine/DAE/Language.D4/D4Emitter.cs`, all of the same kind: the printed text does not say what the tree said.

Two of the nineteen are `src/engine/DAE/Schema/DataTypes.d4` and `src/engine/DAE/Schema/SystemCatalog.d4` — the catalog scripts the server reads at bootstrap. They ship.

The server prints D4 through this emitter whenever it stores catalog DDL, scripts a catalog out, or shows a user the D4 behind an object, so these three defects are visible wherever that text is read. One of them is more than cosmetic: see *Arm C*.

## Arm A — a sort clause the source did not have

`EmitQuotaExpression` (around line 1275) prints the `by { … }` list **unconditionally**, ignoring `QuotaExpression.HasByClause`. Every other `by`/`order by` site in the file guards on its flag (lines 416, 876, 1162). So `T return 1` prints as `T return 1 by {  }`, which reads back with `HasByClause` true: the reported difference is `{QuotaExpression}.hasByClause: value differs (False vs True)`.

The fix is to wrap the list in `if (expression.HasByClause)`, as the sibling sites do. A tree that has the flag set with no columns still prints `by {  }` and still reads back — that shape is what the three affected files were producing all along.

Clears three files: `libraries/System.Integration/Documents/Register.d4`, `tests/libraries/TestFramework.Coverage.Devices/Documents/Logic.d4`, `tests/libraries/TestFramework.Coverage.Scripts/Documents/SystemCatalog.d4`.

## Arm B — metadata tags come back in a different order

`EmitMetaData` (around line 90) walks the tags twice: once printing the non-static ones as `tags { … }`, once printing the static ones as `static tags { … }`. Tags written in one mixed order therefore come back grouped — the first tag of the re-parsed metadata is a different tag, which is what `{MetaData}.tags[0]: Tag differs (…)` reports.

The grammar can express the source order: `Parser.MetaDataTag` (line 1759) accepts a per-tag `static` or `dynamic` modifier that overrides the group's default, so `tags { Frontend.Title = "One", static Storage.Length = "14", Frontend.Width = "15" }` is legal and reads back in that order. The fix is one pass that keeps the order, in one group:

- collect the tags to print in source order, with the filter unchanged — a static tag always, a non-static one only when `EmitMode` is `ForRemote` or it is not inherited;
- all static → `static tags { … }`; none static → `tags { … }`; mixed → `tags { … }` with `static ` in front of each static tag.

So the printed text for the homogeneous cases — nearly all of them — is unchanged byte for byte, and only mixed metadata changes shape. `EmitTags` takes one more parameter for "qualify the static tags"; it is private.

Keep it to **one** group: `Parser.MetaData(IMetaData)` (line 1722) calls `MetaDataTags` exactly twice, so the grammar accepts at most two groups and a run-preserving scheme would be able to print text it could not read back. Worth a `NOTE:` at the site.

Clears five files: `samples/Sample.IssueTracker/Documents/Register.d4`, `samples/Sample.Shipping/Documents/Schema.DataTypes.d4`, `src/engine/DAE/Schema/DataTypes.d4`, `src/engine/DAE/Schema/SystemCatalog.d4`, `tests/libraries/TestFramework.Coverage.Scripts/Documents/SerializationTypes.d4`.

## Arm C — a multi-variable declaration is split up

`var a : X, b : Y;` parses to a plain `Block` holding two `VariableStatement`s (`Parser.VariableStatement()`, line 3619). `EmitBlock` prints a `Block`'s statements one per line with its own terminator and no `begin … end`, so the group is printed as two separate statements and reads back as two siblings of the enclosing list instead of one nested `Block`. That is the `list length differs (N vs N+1)` family.

In a statement list that is only a shape difference — a plain `Block` pushes no symbol frame (`Compiler.CompileBlock`, line 1057), so a group spliced into its parent runs identically. **In a single-statement position it is a behaviour change**: `if c then var a : X, b : Y;` prints as

```
if c then
	var a : X;
var b : Y;
```

and `var b` is then declared unconditionally. The same holds for a `while` body and an exception handler. No file in the repository has that shape, which is why it has never been reported, but it is wrong in the text the server stores, and the fix below removes it. This arm is the reason to fix rather than to teach the comparator to accept the reshape.

The fix has four parts, and they only work together:

- `EmitStatement`'s `Block` branch prints a **variable-statement group** — a plain `Block` of two or more statements, all `VariableStatement` — as `var a : X := e, b : Y`, via a new `EmitVariableStatementGroup`. Split the body of `EmitVariableStatement` into an `EmitVariableDeclaration` helper (name, type specifier, initializer) that both callers use.
- `EmitDelimitedBlock` prints the statements of its single plain-`Block` child **directly** instead of handing that child to `EmitStatement`:

```
			if ((statement.Statements.Count == 1) && (statement.Statements[0] is Block) && !(statement.Statements[0] is DelimitedBlock))
				EmitBlock((Block)statement.Statements[0]);
			else
				EmitBlock(statement);
```

  Without this, `begin var a; var b; end` — whose tree is `DelimitedBlock { Block { var a, var b } }` — would print as `begin var a, var b; end`, adding a level. (`begin var a, b; end` parses to that same tree, because `DelimitedBlock(string[])` unwraps a one-statement list, so either text is a correct printing of it.) This also removes the stray `;` and blank line that printing a nested block adds, including the ones the parser ticket's new `for`/`try … commit` shape introduces.
- `D4TextEmitter` overrides `InternalEmit` so that **the outermost statement of a script is treated as a statement list**:

```
		protected override void InternalEmit(Statement statement)
		{
			if ((statement is Block) && !(statement is DelimitedBlock))
				EmitBlock((Block)statement);
			else
				base.InternalEmit(statement);
		}
```

  `ParseScript` returns a `Block` of the script's statements; without this override a script whose statements are *all* variable declarations — `tests/libraries/TestFramework.Coverage.Scripts/Documents/Conditionals.d4`, twelve `var`s — would print as one `var` group and read back as a script of one statement. This was found as a regression during prototyping, so write the override and the group rule in the same change.

Clears eleven files: the ten `*Library.d4`/`Conversions.d4`/`CursorLibrary.d4` coverage scripts and `LanguageConstructs.d4`.

## Edge cases & interactions

| Case | How it is covered |
|---|---|
| a script that is nothing but variable declarations must not become one group | `Conditionals.d4` in the corpus (it is not allow-listed: a regression here fails the run) |
| `begin var a; var b; end` and `begin var a, b; end` parse to the same tree, so one printing serves both | round-trip cases in `D4EmitterRoundTripTest` |
| `if c then var a, b;`, `while c do var a, b;`, `on E do var a, b;` — the group must stay one statement | round-trip cases (this is Arm C's behaviour fix) |
| a `Block` with one `VariableStatement`, or one mixing declarations with other statements, is printed as today | by the `Count >= 2` and all-`VariableStatement` test in `IsVariableStatementGroup` |
| declarations with no type specifier, with an initializer, with both | round-trip case `var a := 1, b : Integer := 2;` |
| a `for` body that is a multi-variable group (`for i := 1 to 2 do var a, b;`) — Arm C meeting the parser ticket's shape | round-trip case |
| `EmitMode.ForRemote` and inherited tags: the filter must stay exactly as it is (static always; non-static only when `ForRemote` or not inherited) | by inspection against the current code; no test exercises `ForRemote` printing today |
| mixed metadata with more than two tags takes `EmitTags`'s multi-line layout branch | round-trip case with four mixed tags |
| a tag value containing a quotation mark (doubled on output) is untouched | by inspection |
| a quota tree with `HasByClause` set and no columns still prints `by {  }` and still reads back | by inspection; that is today's output for the three affected files |
| derived form documents embed printed D4 and `FrontendDerivationGoldenTest` compares 416 of them character for character | run that fixture; it passed with all three arms prototyped. If a golden does move, the diff is the review — read it before updating |
| catalog DDL is stored as printed text | `CatalogStoreTest`, `StoreRebuildWorkflowTest` (both passed with the prototype) |
| the `AllowList` is also edited by `bug-d4-parser-rejects-active-sources` (the thirty-eight `parse` entries) | different entries; re-read the file before editing if that ticket has landed meanwhile |

## Verification

Measured on this tree with each arm prototyped separately and then together (the corpus logic run over all 388 `.d4` files):

| state | round-trip | reshape | unparseable |
|---|---|---|---|
| today | 297 | 53 | 38 |
| Arm C alone | 307 | 43 | 38 |
| parser ticket alone | 331 | 19 | 38 |
| parser ticket + all three arms | **350** | **0** | 38 |

No file that round-trips today stops doing so under any of them. With all four changes applied a 438-case slice passed — the corpus, `D4EmitterRoundTripTest`, `CatalogStoreTest`, `StoreRebuildWorkflowTest`, `FormCustomizationTest`, `FormDocumentCorpusTest`, `InternalCoverageTests` — as did `FrontendDerivationGoldenTest` and the `LanguageConstructs`, `Conditionals` and `Cursors` coverage scripts.

The reproduction is the corpus test:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest"
```

## AllowList entries this ticket removes

The last nineteen with category `emitter-reshape` — after this, the category is empty:

```
libraries/System.Integration/Documents/Register.d4
samples/Sample.IssueTracker/Documents/Register.d4
samples/Sample.Shipping/Documents/Schema.DataTypes.d4
src/engine/DAE/Schema/DataTypes.d4
src/engine/DAE/Schema/SystemCatalog.d4
tests/libraries/TestFramework.Coverage.Devices/Documents/Logic.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/BooleanLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/ByteLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/Conversions.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/CursorLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/DecimalLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/GuidLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/IntegerLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/LanguageConstructs.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/LongLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/SerializationTypes.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/ShortLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/StringLibrary.d4
tests/libraries/TestFramework.Coverage.Scripts/Documents/SystemCatalog.d4
```

## Rejected alternative

Teach `ParserRoundTripCorpusTest.TreesEqual` to accept these three reshapes — splice a nested plain `Block` into its parent, compare tags by name, ignore `HasByClause` when the clause is empty. It is a smaller change, but it weakens the net for every future change (a genuine block-nesting or tag bug would then pass), and it would leave Arm C's conditional-declaration behaviour wrong. Declined.

## TODO

- Arm A: guard the `by` list in `EmitQuotaExpression` with `if (expression.HasByClause)`.
- Arm B: rewrite `EmitMetaData` as one ordered pass with the filter unchanged, add the "qualify the static tags" parameter to `EmitTags`, print one group (all-static, none-static, or mixed with per-tag `static`), and leave a `NOTE:` that the grammar accepts at most two tag groups so the output must stay one.
- Arm C: split `EmitVariableStatement` into an `EmitVariableDeclaration` helper; add `IsVariableStatementGroup` and `EmitVariableStatementGroup`; dispatch to them from `EmitStatement`'s `Block` branch; print a `DelimitedBlock`'s single plain-`Block` child's statements directly; override `InternalEmit` so the outermost `Block` of a script is printed as a statement list.
- Add round-trip cases to `D4EmitterRoundTripTest.EmittedTextReParsesToTheSameTree` for each case marked "round-trip case" in the table above — the conditional group, the two `begin … end` spellings, mixed metadata (two tags and four), a quota with and without `by`, declarations with and without type specifiers and initializers, and a `for` body that is a group.
- Remove the nineteen `AllowList` entries above, then rewrite the fixture's header comment: the `emitter-reshape` category is gone, the way `bug-d4-emitter-malformed-output` retired `emitter-malformed`. Only the thirty-eight `parse` entries remain, owned by `bug-d4-parser-rejects-active-sources`.
- Add a `docs/migration-notes.md` row under *D4, catalog and libraries*: printed D4 keeps the metadata-tag order it was given (mixed static and non-static tags now print as one `tags { … }` group with `static` on the tags that need it), a quota expression with no `by` clause no longer prints an empty one, and a multi-variable declaration prints as one statement. Nothing for an application; a custom C# extension that parses printed D4 or compares it as text sees these three differences, and one that derives from `D4TextEmitter` gets the new `EmitVariableStatementGroup` and `InternalEmit` members.
- Build, then run the three suite parts from the root `AGENTS.md` (never `dotnet test Dataphor.slnx` as one command), each as its own foreground command. `FrontendDerivationGoldenTest` is in part 1: read any golden diff rather than updating it blind.
