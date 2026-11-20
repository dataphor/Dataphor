description: The D4 text emitter used to write text the D4 parser could not read for eight documents in the repository; seven emitter sites were fixed, so anything that turns a parsed script back into text — including the server when it stores or scripts a catalog object — now produces a script that still runs.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, src/engine/DAE/Language/Emitter.cs, src/engine/DAE/Language.D4/Parser.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, tickets/plan/bug-d4-emitter-reshapes-parse-tree.md
----
## What landed

Seven emitter defects, each one site. The server stores and scripts catalog DDL through `D4TextEmitter` (`ServerCatalogDeviceSession.ScriptObject`/`ScriptCatalog` at `ServerCatalogDeviceSession.cs:2726` and `:2731`, `Engine.ScriptCatalog`/`ScriptDropCatalog`), so each of these produced a script the parser could not read back.

| # | Site | Fix |
|---|---|---|
| A | `D4Emitter.cs` `EmitForEachExpression` | `AppendFormat(" {0} ", Keywords.Do)` — the body follows on the same line, so without the trailing space `do` + `row { … }` lexed as the identifier `dorow`. The statement form at the method above is unchanged; it starts a new line. |
| B | `D4Emitter.cs` `EmitScalarTypeSpecifier` | emits the bare `scalar` keyword for the generic scalar type, not `generic scalar`. The parser's `ScalarTypeSpecifier` accepts only the bare keyword — which is also how it already reads a bare `list`, `row`, `table` or `cursor`. |
| C | `Emitter.cs` `EmitValueExpression`, `TokenType.Hex` | the case formatted the value and threw the string away; it now appends `0x` + the hexadecimal digits. |
| D | `Emitter.cs` `EmitValueExpression`, `TokenType.Money` | new private `BasicTextEmitter.FormatMoney` puts the sign before the `$` (`-$10.50`, not `$-10.50`); the lexer reads `$` as the start of a number and rejects a following `-`. |
| E | `D4Emitter.cs` `EmitSecuritySpecifier` | the user and group grantee is written as a plain single-quoted string, without the trailing `i`. That suffix is case-insensitive-string syntax from the `USEISTRING` build, which is off and whose lexer support does not exist. The `Role` arm was already correct and is untouched; the group arm keeps its `inherited`, `apply recursively` and `include users` clauses. |
| F | `D4Emitter.cs` `EmitQualifierExpression` | a qualifier chain ending in a single-column extractor is emitted with the qualifier **inside** the extractor's parentheses — `(C.Name from X)`, not `C.(Name from X)`. The parser builds `C.Name from X` as a qualifier whose right side *is* the extractor, so that is the only spelling that round-trips. |
| G | `D4Emitter.cs` `EmitQualifierExpression` | a numeric literal left of a `.` is parenthesized — `(1).Max(50)`, since `1.` lexes as a malformed number. |

C and D are in the shared base `BasicTextEmitter`, where the defect is, not in a D4-only override. `SQLTextEmitter` inherits them: its `EmitValueExpression` override handles only the `Nil` and `Decimal` cases and falls through for the rest, and nothing in the SQL device path builds a `ValueExpression` with `TokenType.Hex` or `TokenType.Money`.

Supporting refactor: `EmitColumnExtractorExpression` now emits `(`, calls the new private `EmitColumnExtractorBody`, and emits `)`. The body holds the column list, `from`, the expression and the optional `order by` clause, written once and reachable from the qualifier path. `EmitColumnExtractorExpression` is unchanged as the `protected virtual` entry point.

Also landed: the migration-notes row (`## D4, catalog and libraries`, status `done`); the `NOTE:` at `Parser.cs` `ScalarTypeSpecifier` recording that `docs/reference/d4-grammar.md` gives the production as `[generic] scalar` while this parser accepts only the bare keyword (the reconciliation, if ever wanted, belongs to `bug-d4-parser-rejects-active-sources`); and the count and file names in `tickets/plan/bug-d4-emitter-reshapes-parse-tree.md`, now fifty-three.

## Tests

`tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs`, `EmittedTextReParsesToTheSameTree` with ten `TestCase` snippets: for each cause, the emitted text re-parses with no `ParserMessages` entries and to a tree equal under `ParserRoundTripCorpusTest.TreesEqual`. Lowest layer that shows the defect — parser and emitter, no server. C gets two snippets (a bare `0x1F` and `-0x1`, which the parser keeps as a negate over a hexadecimal literal), E two (the user arm and the group arm with a trailing `inherited`), and F two (one qualifier, and the two-qualifier chain the review added).

`ParserRoundTripCorpusTest`'s `AllowList` is the integration-level confirmation. Six entries deleted (those files now round-trip outright):

- `legacy/libraries/FHIR.Server/Documents/FHIR.LoadConformanceCache.d4`
- `legacy/libraries/FHIR.Server/Documents/ModelInfo.Operators.d4`
- `libraries/System.Internet/Documents/Dynamic.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/Lexer.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/SecurityLibrary.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/ViewUpdatability.d4`

Two recategorized `emitter-malformed` → `emitter-reshape` (their malformed output is gone; what is left is the multi-variable-declaration flattening that `bug-d4-emitter-reshapes-parse-tree` owns): `IntegerLibrary.d4` and `SerializationOperators.d4`. The `(emitter-malformed)` bullet is gone from the comment block above the list, replaced by one line saying the category was drained by this ticket.

## Review findings

Read the implement diff (`ticket(implement): bug-d4-emitter-malformed-output`) before the handoff summary.

**Checked.** Each of the seven causes against the parser code that reads the emitted spelling (`SecuritySpecifier`, `TypeSpecifier`/`ScalarTypeSpecifier`, `ColumnExtractor`, `QualifierExpression`, `QualifiedFactor`, and the lexer's `AsHex`/`AsMoney`/`AsString`). The two casts the base emitter now makes: every producer of a `Money` value expression passes a `decimal` (the D4 parser's `AsMoney`, the RealSQL parser's `AsDecimal`, `ValueNode.EmitStatement` for `System.Money`, `CatalogNodes`, and a `System.Money` parameter through `DataAction`), and the only producer of a `Hex` one is the D4 parser's `AsHex`, which returns `long` — so neither cast can meet another payload type. Negative hexadecimal values round-trip: `AsHex` parses with `AllowHexSpecifier`, which wraps, and `ToString("X")` of a negative `long` gives back the same sixteen digits, so `0xFFFFFFFFFFFFFFFF` and `-1` map to each other with no overflow. The two bail-outs in `TrailingColumnExtractor` really are unreachable through the grammar: `ColumnExtractor` never calls `LanguageModifiers`, nor does any code path that builds a qualifier node, and a multi-column extractor can only come from `ListSelector`, which starts with `{` and so cannot follow a `.`. Leaving those guards emitting the old spelling rather than throwing is the right call for an emitter. The other `generic` type specifiers (`row`, `table`, `list`, `cursor`) all round-trip; `scalar` was the only broken one. Swept all three emitters for the same discarded-expression-statement shape as the hexadecimal defect (`x.ToString("X");` with no `Append`): none remain.

**Verified by running, not by reading.** Commented the two recategorized entries out of the `AllowList`, rebuilt and re-ran the corpus fixture: each fails only with a tree difference (`list length differs (45 vs 46)` for `IntegerLibrary.d4`, `(3 vs 1)` for `SerializationOperators.d4`) and no "Harness bug" message, so the recategorization is honest. Restored the file; `git status` clean.

**Found and fixed in this pass.** Three minor defects:

- The `NOTE:` at `Parser.cs` `ScalarTypeSpecifier` claimed that `TypeSpecifier`'s `case Keywords.Type` branch is unreachable. It is not: `type` is not a reserved word, and a probe through the built parser showed `var x : generic type;` parses and re-emits as `var x : type;` — the branch is entered, `isGeneric` is still ignored, and the `generic` is silently dropped. Rewrote the NOTE to the verified facts, keeping the pointer to `bug-d4-parser-rejects-active-sources`.
- `BasicTextEmitter.FormatMoney` tested the sign with `StartsWith("-")`, the culture-sensitive overload, on a string it had just formatted with `InvariantCulture`. Changed to the ordinal `StartsWith('-')`, and recorded in the comment why the test is on the text rather than on `value < 0`: negating would overflow `decimal.MinValue`, and a negative zero formats with a sign while comparing equal to zero. That reasoning was in the handoff but not in the code.
- Added a tenth `TestCase`, `var x := (A.B.Name from Y);`. The handoff named this as the arm nothing exercises, and it is the only branch the ticket added with no coverage at all: the `while` loop in `TrailingColumnExtractor` and the recursive call in `EmitQualifierPrefix` both run only when two or more qualifiers precede the extractor.

**Tickets filed: none.** Nothing reached the filing bar — no finding named a class-level invariant, and the three defects above were a wrong claim in a comment, a comparison overload and one missing test, all cheaper to fix than to file. Two findings were weighed and deliberately left alone:

- `EmitColumnExtractorExpression` stays `protected virtual`, but the qualified path calls the private `EmitColumnExtractorBody` directly, so a subclass that overrode the public entry point would not be consulted for a qualified extractor. Nothing in the tree overrides it, and the XML doc on the body method says why it exists; a ticket for a bypass nobody can observe would be queue noise.
- On the storage path a negative money value reaches the emitter as a single `ValueExpression(-10.50m, Money)` from `ValueNode.EmitStatement` and comes back from a re-parse as a negate over a positive literal — parseable and semantically identical, where before it was unparseable. `CatalogNodes.cs:404` already builds that negate-over-positive shape by hand, so it is the established spelling, and nothing compares trees on that path.

**Tripwires: none newly parked.** The one conditional concern weighed was that `AllowList` entries are skipped rather than asserted to still fail, so a file whose defect is fixed stays quarantined until someone retries it by hand — which is exactly what this ticket had to do. That is already the documented reproduction step in both `bug-d4-emitter-reshapes-parse-tree` and `bug-d4-parser-rejects-active-sources`, which own that file; a comment would only restate them.

**Tests kept.** All nine of the implementer's cases stand. The two that look like duplicates were checked and kept: `NegatedHexLiteral` pins the negate-over-literal tree the parser builds for `-0x1`, and `GroupGrantee` covers the group arm with its `inherited` clause, which is separate code from the user arm.

**Docs.** `docs/reference/d4-grammar.md` is the historical BNF, the record of intended behaviour, with no convention for noting where the parser diverges — so leaving it untouched and putting the divergence in a `NOTE:` at the parser is right, not an omission. `docs/migration-notes.md` carries the row; `docs/architecture.md` and the plan need nothing. The review's own three changes are invisible to applications and extensions, so they add no migration-notes entry.

**Size.** `D4Emitter.cs` is 5,025 lines (`wc -l`), 88 of them from this ticket. Pre-existing, and every method in it is `protected virtual`, so a split is a compatibility surface for custom C# extensions — not review cleanup, and not something to file off the back of an 88-line change.

## Validation

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~D4EmitterRoundTripTest"
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest"
```

Observed after the review's changes: build 0 errors, 143 warnings, none of them in the touched files. `D4EmitterRoundTripTest` 10 passed. `ParserRoundTripCorpusTest` 298 passed (296 corpus files plus the two hand-crafted `TreesEqual` smokes), against 290 before the ticket.

Full suite, the three parts from the root `AGENTS.md`, all green and nothing skipped beyond the usual coverage-manifest known/unstable/fragment cases:

- part 1 (everything but the coverage scripts): `Common.Tests` 13 passed; `DAE.Server.Tests` 385 passed, 0 skipped, 2 m 57 s
- part 2 (coverage scripts A–R): 67 passed, 11 skipped, 3 m 8 s
- part 3 (the rest): 48 passed, 14 skipped, 2 m 58 s

No `coverage-manifest.json` change was needed: no `known-failure` entry flipped to passing (the run fails if one does). No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written.

## Source hygiene

`D4Emitter.cs`, `Emitter.cs` and `Parser.cs` kept tabs, CRLF and no byte-order mark. `ParserRoundTripCorpusTest.cs` kept tabs and LF. `DAE.Server.Tests.csproj` kept spaces, CRLF and its byte-order mark. The new `D4EmitterRoundTripTest.cs` is tabs with a byte-order mark, CRLF in the working tree and LF in the index — the state most other files in that folder have. `docs/migration-notes.md` gained exactly one line, CRLF, unwrapped. Verified with `git ls-files --eol` and `git diff` after the review's edits.
