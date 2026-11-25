description: A query can pick a single row out of a table by its key, and the five errors the engine reports when that is written ambiguously or wrongly had never been triggered by any test. A new test script now triggers all five, and checks the working forms return the right row.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/TableIndexer.d4, tests/DAE.Server.Tests/coverage-manifest.json, tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4, src/engine/DAE/Compiling/Compiler.cs
----
## What landed

One new D4 coverage script and two one-line edits. **No engine source changed** — the behaviour the script records is whatever the engine does today.

- `tests/libraries/TestFramework.Coverage.Scripts/Documents/TableIndexer.d4` (new, 209 lines) — covers the D4 table indexer expression, `MyTable[...]`, which picks one row out of a table by a key. Compiled by `CompileIndexerExpression` in `src/engine/DAE/Compiling/Compiler.cs`.
- `tests/DAE.Server.Tests/coverage-manifest.json` — `{ "script": "TableIndexer", "status": "pass" },` inserted in ordinal position, between `TableCallsWithStackReferences` and `TableLibrary`.
- `tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4` line 84 — dropped the stale `// TODO: Build this coverage` marker, leaving the `ExecuteScript` call untouched. That file is the 2008 hand-wired run list and nothing executes it today; the marker would otherwise have invited someone to re-file this ticket.

No migration note and no architecture page: this adds a test, and nothing an application, deployment script or C# extension could notice changed.

## What the script verifies, case by case

**Working cases**, compiled in the script's own batches, each asserting a column of the row returned rather than merely that nothing raised. Five tables are created with rows arranged so that picking the wrong key, or pairing a term with the wrong column, returns a *visibly different row* instead of an empty result:

- `TableIndexerSingle[1 by { ID }]` — the explicit form. **No coverage script used the indexer's `by` clause before this one**, which is why lines 12603-12604 of `Compiler.cs` were uncovered and now are not.
- `TableIndexerSingle[1]` — the implicit form reaching the same key, same row.
- `TableIndexerTwoKeys[1]` and `TableIndexerTwoKeys['Alpha']` — two keys of the same width, different column types, so the term's *type* decides which key is meant. The table's two orderings deliberately disagree (ID 1 carries Name 'Beta'), so a wrong resolution returns the other row.
- `TableIndexerWidths[2]` and `TableIndexerWidths[20, 'x']` — keys of width 1 and 2, so the number of terms decides.
- `TableIndexerWidths['x', 20]` — the same two terms in the other order, which must return the same row. The compiler tries every ordering of the terms and, for the ordering that matches, rebuilds the key column by column; this is the only assertion anywhere that covers that permutation machinery end to end. All four combinations of A and B are rows in the table, so a resolution that dropped a term or crossed a pairing lands on a different row.
- `TableIndexerSameTypeKeys[1 by { Alpha }]` and `[1 by { Beta }]`, `TableIndexerPairKey[1, 2 by { A, B }]` and `[2, 1 by { A, B }]` — the explicit form as the way out of both ambiguities below. The values overlap between the two keys and the two rows are each other's reverse, so these pin that the `by` clause picks the key and term order it names, not merely that it compiles.
- `TableIndexerSingle[2]` read *after* the six caught compile errors — asserts that a caught compile error from a nested `Execute` leaves the enclosing implicit transaction usable. The ticket asked for this to be reported if it failed; it does not fail, and the assertion now records that rather than leaving it as a claim.

**Error cases**, each through a dynamic `Execute('...')` because all five are raised by the *compiler* and the harness compiles each batch immediately before executing it — a `try ... except` in the same batch as a bad expression would never run. Each asserts the code in the flattened error chain (`IndexOf(LCaught, ':<code> --->')`), which is the convention `LibraryAdministration.d4` documents:

| Expression | Code asserted | Raised because |
|---|---:|---|
| `TableIndexerSingle[1, 2, 3, 4, 5, 6]` | 105264 | more than five terms; checked before any key is looked at |
| `TableIndexerSameTypeKeys[1]` | 105275 | two keys of the same width *and* the same column types, so no term could tell them apart |
| `TableIndexerPairKey[1, 2]` | 105259 | one key of two same-typed columns: both orderings match it equally well |
| `TableIndexerSingle[1, 2]` | 105260 | no key has that many columns |
| `TableIndexerSingle['One']` | 105260 | a key of that width exists but no ordering of the terms matches its types — the other arm of the same throw |
| `TableIndexerSingle[1 by { ID, Data }]` | 105261 | the `by` clause names two columns where one term was given |

**Every one of the six raised the code the ticket predicted** — no expected value had to be changed, and the ticket's own correction about 105261 (a resolved-key/term-count mismatch, not "a term unusable as a key reference") held.

## Measurements

Build clean: `dotnet build Dataphor.slnx` — 0 errors.

Targeted run (the filter the ticket named; `TableLibrary` and `SpecifyClause` are the neighbours that would notice a manifest mistake):

```
dotnet test tests/DAE.Server.Tests --filter "Name=TableIndexer|Name=TableLibrary|Name=SpecifyClause" --logger "console;verbosity=normal"
```

3 passed, 0 failed, 14.9 s wall clock (Debug, slow development laptop). The new case is 2 s. Run twice — once before and once after a header-comment correction — green both times.

Per-line hit counts for the five throws, read out of the Cobertura XML (`--filter Name=TableIndexer --collect:"Code Coverage;Format=cobertura"`). The **before** column is a comparable collected run of the two neighbour scripts alone (`--filter "Name=TableLibrary|Name=SpecifyClause"`), which never reach these paths:

| `Compiler.cs` line | Error | Code | before | after |
|---:|---|---:|---:|---:|
| 12624 | `TooManyTermsForImplicitTableIndexer` | 105264 | 0 | 1 |
| 12636 | `PotentiallyAmbiguousImplicitTableIndexer` | 105275 | 0 | 1 |
| 12695 | `AmbiguousTableIndexerKey` | 105259 | 0 | 1 |
| 12720 | `UnresolvedTableIndexerKey` | 105260 | 0 | 1 |
| 12724 | `InvalidTableIndexerKey` | 105261 | 0 | 1 |

**The line numbers are unchanged from the ticket's table** — confirmed against the file as measured, at the working-tree state of this change.

Whole function (`Compiler.cs` lines 12582-12790, both branches of `CompileIndexerExpression`): **141/163 coverable lines (86.5 %) before, 151/163 (92.6 %) after**, on those same two collected runs. Beyond the five throws, the new script also covers 12603-12604 (the explicit `by` clause building the resolved key) and 12687 / 12689 / 12694 (the tie-break comparisons that lead to 105259).

## One correction to the ticket's text

The ticket said line 12689 — "two matching orderings with the same narrowing score but *different* conversion path lengths" — would stay uncovered. The measurement shows 12689 is the *comparison*, and it is now evaluated (0 → 1); what stays uncovered is its **body**, lines 12690-12693, together with the sibling arm at 12682-12686 (a later ordering with a *better* narrowing score). Both need a chain of user-defined implicit conversions to construct, which is still more intricate than the behaviour they pin, so both are left alone — and the script's header comment now names 12682-12686 and 12690-12693 rather than 12689, so a future reader meets the decision at the right lines.

## Known gaps — treat this as a starting point

- **The script asserts error codes, not message wording.** Deliberate, and stated in the script header: codes are the compatibility surface, messages are not. `LibraryAdministration.d4` asserts both; a reviewer who wants the wording pinned too should say so, and note that it then becomes a thing every future wording change has to update.
- **"No other script reaches these five lines" rests on the ticket's prior measurement, not on anything re-measured here.** That claim came from the full three-part collected run of 2026-11-21 at commit `f6200026`. The before/after above is a local two-script run, which proves these lines were at zero *in that run*, not across the whole suite. A full collected run is the only thing that would re-prove it, and it exceeds the agent's ten-minute budget; CI deliberately does not collect coverage.
- **Still uncovered in the same function, and out of this ticket's scope:** line 12617, the `InvalidRowExtractorExpression` *warning* for a `by` clause that is not a superset of any key (the ticket says it is covered elsewhere in the suite; verified by inspection that every `by` clause in the new script is a superset of a real key, so none of these cases stumbles into it, but *which* script covers 12617 was not chased down); and line 12780, `InvalidIndexerExpression` in the **non-table** branch of the same function, which is a different subject from this ticket's five codes and was not investigated.
- **Column names deviate from the ticket's draft.** The ticket's sketch used `Left` and `Right` for the same-type-keys table; the script uses `Alpha` and `Beta` instead, to keep clear of anything the D4 lexer might treat as a keyword. Nothing else about that table changed.
- **Debug only, one machine.** CI runs Debug and Release after the push.
- The script creates five tables and drops them with plain `drop table` at the end (`SafeDrop` at the top, per convention, for a rerun against a server that kept state). Every script runs on its own fresh server, so cross-script name collision is not possible.

## Suggested review pass

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --filter "Name=TableIndexer|Name=TableLibrary|Name=SpecifyClause" --logger "console;verbosity=normal"
```

One ring wider, per the project's by-stage rule, would add the other `Table*` coverage scripts (`Name=Tables|Name=TableAndRowValuedAttributes|Name=TableCallsWithStackReferences`) — a manifest or layout mistake would show up there. The diff touches no lexer, parser or compiler *source*, so the three-part suite is not warranted.

The thing worth an adversarial eye is not the plumbing but **whether each assertion's expected value is what the engine actually does for the reason the comment gives** — a case that passes for a different reason than its comment claims is the failure mode here. Read each error case against the `Compiler.cs` region (12582-12790) rather than against this ticket.
