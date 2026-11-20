description: The D4 text emitter used to write text the D4 parser could not read for eight documents in the repository; seven emitter sites were fixed, so anything that turns a parsed script back into text — including the server when it stores or scripts a catalog object — now produces a script that still runs.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, src/engine/DAE/Language/Emitter.cs, src/engine/DAE/Language.D4/Parser.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, tickets/plan/bug-d4-emitter-reshapes-parse-tree.md
----
## What landed

Seven emitter defects, each one site. The server stores and scripts catalog DDL through `D4TextEmitter` (`ServerCatalogDeviceSession.ScriptObject`/`ScriptCatalog`, `Engine.ScriptCatalog`/`ScriptDropCatalog`), so each of these produced a script the parser could not read back.

| # | Site | Fix |
|---|---|---|
| A | `D4Emitter.cs` `EmitForEachExpression` | `AppendFormat(" {0} ", Keywords.Do)` — the body follows on the same line, so without the trailing space `do` + `row { … }` lexed as the identifier `dorow`. The statement form at the method above is unchanged; it starts a new line. |
| B | `D4Emitter.cs` `EmitScalarTypeSpecifier` | emits the bare `scalar` keyword for the generic scalar type, not `generic scalar`. The parser's `ScalarTypeSpecifier` accepts only the bare keyword. |
| C | `Emitter.cs` `EmitValueExpression`, `TokenType.Hex` | the case formatted the value and threw the string away; it now appends `0x` + the hexadecimal digits. |
| D | `Emitter.cs` `EmitValueExpression`, `TokenType.Money` | new private `BasicTextEmitter.FormatMoney` puts the sign before the `$` (`-$10.50`, not `$-10.50`); the lexer reads `$` as the start of a number and rejects a following `-`. |
| E | `D4Emitter.cs` `EmitSecuritySpecifier` | the user and group grantee is written as a plain single-quoted string, without the trailing `i`. That suffix is case-insensitive-string syntax from the `USEISTRING` build, which is off and whose lexer support does not exist. The `Role` arm was already correct and is untouched; the group arm keeps its `inherited`, `apply recursively` and `include users` clauses. |
| F | `D4Emitter.cs` `EmitQualifierExpression` | a qualifier chain ending in a single-column extractor is emitted with the qualifier **inside** the extractor's parentheses — `(C.Name from X)`, not `C.(Name from X)`. The parser builds `C.Name from X` as a qualifier whose right side *is* the extractor, so that is the only spelling that round-trips. |
| G | `D4Emitter.cs` `EmitQualifierExpression` | a numeric literal left of a `.` is parenthesized — `(1).Max(50)`, since `1.` lexes as a malformed number. |

C and D are in the shared base `BasicTextEmitter`, where the defect is, not in a D4-only override. `SQLTextEmitter` inherits them; it overrides the `Nil` and `Decimal` cases itself and nothing in the SQL device path builds a `ValueExpression` with `TokenType.Hex` or `TokenType.Money` (checked: every `Money` construction in the tree passes a `decimal`, and `Hex` appears only in `Compiler.cs`, `Lexer.cs`, `Parser.cs` and these emitters).

Supporting refactor: `EmitColumnExtractorExpression` now emits `(`, calls the new private `EmitColumnExtractorBody`, and emits `)`. The body holds the column list, `from`, the expression and the optional `order by` clause, written once and reachable from the qualifier path. `EmitColumnExtractorExpression` is unchanged as the `protected virtual` entry point, in case a subclass overrides it.

Also landed: the migration-notes row (`## D4, catalog and libraries`, status `done`); the `NOTE:` at `Parser.cs` `ScalarTypeSpecifier` recording that `docs/reference/d4-grammar.md` gives the production as `[generic] scalar` while this parser accepts only the bare keyword (the reconciliation, if ever wanted, belongs to `bug-d4-parser-rejects-active-sources`); and the count and file names in `tickets/plan/bug-d4-emitter-reshapes-parse-tree.md`, now fifty-three.

## Tests

| Test | What it verifies |
|---|---|
| `tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs`, `EmittedTextReParsesToTheSameTree` with nine `TestCase` snippets | the reproduction: for each of the seven causes, the emitted text re-parses with no `ParserMessages` entries and to a tree equal under `ParserRoundTripCorpusTest.TreesEqual`. C gets two snippets (a bare `0x1F` and `-0x1`, which the parser keeps as a negate over a hexadecimal literal) and E two (the user arm and the group arm with a trailing `inherited`). Lowest layer that shows the defect: parser and emitter, no server. |

`ParserRoundTripCorpusTest`'s `AllowList` is the integration-level confirmation. Six entries deleted (those files now round-trip outright):

- `legacy/libraries/FHIR.Server/Documents/FHIR.LoadConformanceCache.d4`
- `legacy/libraries/FHIR.Server/Documents/ModelInfo.Operators.d4`
- `libraries/System.Internet/Documents/Dynamic.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/Lexer.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/SecurityLibrary.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/ViewUpdatability.d4`

Two recategorized `emitter-malformed` → `emitter-reshape` (their malformed output is gone; what is left is the multi-variable-declaration flattening that `bug-d4-emitter-reshapes-parse-tree` owns): `IntegerLibrary.d4` and `SerializationOperators.d4`. The `(emitter-malformed)` bullet is gone from the comment block above the list, replaced by one line saying the category was drained by this ticket.

## How to validate

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~D4EmitterRoundTripTest"
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest"
```

Observed: build 0 errors, 143 warnings (all pre-existing). `D4EmitterRoundTripTest` 9 passed. `ParserRoundTripCorpusTest` 298 passed (296 corpus files plus the two hand-crafted `TreesEqual` smokes) — matching the measurement in the implement ticket, which predicted 296 corpus passes against 290 before.

Full suite, the three parts from the root `AGENTS.md`, all green and nothing skipped beyond the usual coverage-manifest known/unstable/fragment cases:

- part 1 (everything but the coverage scripts): `Common.Tests` 13 passed; `DAE.Server.Tests` 384 passed, 0 skipped, 3 m 9 s
- part 2 (coverage scripts A–R): 67 passed, 11 skipped, 3 m 14 s
- part 3 (the rest): 48 passed, 14 skipped, 3 m 8 s

No `coverage-manifest.json` change was needed: no `known-failure` entry flipped to passing (the run fails if one does). The emitter change does reach the catalog store — DDL is stored as emitted text — and the engine tests and both coverage-script parts cover that path.

## Known gaps and things worth a reviewer's eye

- **Nothing in the repository exercises the recursive qualifier prefix.** `EmitQualifierPrefix` handles `A.B.Name from X` by walking the right spine, and the reasoning is in the code, but every corpus instance and every snippet has exactly one qualifier before the extractor. A reviewer who wants that arm pinned would add a tenth snippet; I judged the one-per-cause rule to cover it and did not.
- **`TrailingColumnExtractor` bails to the old (malformed) spelling in two cases**, both documented in its `<remarks>`: a node on the right spine carrying language modifiers, and a multi-column extractor. Neither shape is reachable through the grammar — the parser cannot put `{A, B} from X` to the right of a `.` — so the guards keep the old behaviour rather than inventing a spelling. If a reviewer thinks an unreachable shape should throw rather than emit text the parser rejects, that is a judgement call I left as-is.
- **`BasicTextEmitter.FormatMoney` formats the value and moves the sign** rather than negating it, so `decimal.MinValue` does not overflow. Slightly more string work than the arithmetic form; the emitter is not on a hot path.
- **The `NOTE:` at `Parser.cs` claims the `case Keywords.Type` branch of `TypeSpecifier` is unreachable.** That is read off the code (`Keywords.Scalar` has no branch under `generic`, and `Keywords.Type` as a token after `generic` is never produced by anything in the tree); it is not covered by a test. Worth a second read.
- **`statement.Grantee.Replace("'", "''")` is the escaping for the new plain-string form.** The old code escaped `"` for the double-quoted form. No corpus file has a quote in a grantee name, so the escaping itself is untested.
- The migration-notes row says catalog scripts produced by an earlier 4.0 preview may need regenerating. That is advice, not a code path; no upgrade step was added, and the plan already rebuilds catalogs from scripts.

## Source hygiene

`D4Emitter.cs`, `Emitter.cs` and `Parser.cs` kept tabs, CRLF, no byte-order mark. `ParserRoundTripCorpusTest.cs` kept tabs and LF (verified with `git ls-files --eol`: `i/lf w/lf`). `DAE.Server.Tests.csproj` kept spaces, CRLF and its byte-order mark. The new `D4EmitterRoundTripTest.cs` is tabs, CRLF, with a byte-order mark, matching most files in that folder. `docs/migration-notes.md` gained exactly one line (`git diff --numstat`: `1 0`), CRLF, unwrapped.
