description: A query can pick a single row out of a table by its key, and the five errors the engine reports when that is written ambiguously or wrongly have never been triggered by any test - so the messages a developer would actually see are the part nobody checks. Add a test script that triggers all five.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/TableIndexer.d4, tests/DAE.Server.Tests/coverage-manifest.json, tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4, src/engine/DAE/Compiling/Compiler.cs
----
## What this builds

One new D4 coverage script, `tests/libraries/TestFramework.Coverage.Scripts/Documents/TableIndexer.d4`, with its entry in `tests/DAE.Server.Tests/coverage-manifest.json`. No engine source changes: this ticket adds a test, and the behaviour it records is whatever the engine does today.

The subject is the D4 **table indexer expression** - the `MyTable[...]` form that picks one row out of a table by a key. It is compiled in `CompileIndexerExpression` in `src/engine/DAE/Compiling/Compiler.cs` (the table branch starts at line 12,585 and runs to about 12,790), and it has two forms:

- **explicit key** - a `by` clause names the key columns: `MyTable[1 by { ID }]`; and
- **implicit key** - the terms are positional and the compiler works out which key is meant from the terms' types, trying every ordering of them and refusing if more than one fits: `MyTable[1]`, `MyTable[1, 'Alpha']`.

It is **not** a data structure. The name invites a reader to `src/engine/DAE/Runtime.Data/Index.cs`, which is a different thing and is not even compiled by `DAE.csproj`. Start at the `Compiler.cs` region above.

## Why the script is worth its length

The region is at 86.9 % line coverage (86 of 99 coverable lines, full three-part run of 2026-11-21 at commit `f6200026`) because ordinary scripts index tables by key constantly. What has never run is every one of its error paths. All five of its own error codes sit at zero hits:

| Line today | Error | Code |
|---:|---|---:|
| 12624 | `TooManyTermsForImplicitTableIndexer` | 105264 |
| 12636 | `PotentiallyAmbiguousImplicitTableIndexer` | 105275 |
| 12695 | `AmbiguousTableIndexerKey` | 105259 |
| 12720 | `UnresolvedTableIndexerKey` | 105260 |
| 12724 | `InvalidTableIndexerKey` | 105261 |

(The plan ticket quoted these ten lines lower, from the 2026-11-21 measurement; the file has grown since. Confirm the numbers before quoting them in the handoff.)

The 13 uncovered lines are those five throws plus the block at 12,687-12,695 that decides between two equally good key matches. Error codes are a compatibility surface here - an application can match on one, and they must not change without a ticket that says so - and nothing pins these five today.

## How a compile error is caught from inside a D4 script

This is the part to get right, and it is settled, not open.

These five errors are raised by the **compiler**, not at run time. The harness runs a whole coverage script as one nested `ExecuteScript`, and each batch of that script is compiled immediately before it is executed - so a `try ... except` written in the same batch as a bad expression never runs: the batch fails while compiling. Every error case therefore has to go through a **dynamic** compile, `Execute('...')`, which compiles its argument as a fresh script at run time. `ServerBatch.Execute` calls `ServerStatementPlan.Execute`, which calls `CheckCompiled()` and throws the first compile message, wrapped - so the error arrives in the enclosing script's `except` handler like any other.

The assertion convention is the one `LibraryAdministration.d4` already uses and documents in its header, and this script should repeat it:

```d4
var LCaught : String := '';
try
	Execute('var LRow := TableIndexerSingle[1, 2];');
except on E : Error do
	LCaught := E.GetDetailedErrorDescription();
end;
if IndexOf(LCaught, ':105260 --->') < 0 then
	raise Error('Indexing with more terms than any key has did not raise 105260: ' + LCaught);
```

`E.Code` is useless here: an error raised inside a plan arrives wrapped in `RuntimeException` 104229, so `E.Code` is always 104229. `GetDetailedErrorDescription()` flattens the whole chain and prints each link as `<severity>:<code> --->` (`ExceptionUtility.InternalDetailedDescription`, with `INVERTEXCEPTIONDESCRIPTIONS` defined at the top of `src/engine/Base/ExceptionUtility.cs`), so the code is findable in it. Search for `':<code> --->'` and not for the digits alone: the same text carries source paths and `:line NNN` fragments that bare digits would match.

Wrap each bad expression in `var LRow := <expression>;` rather than in a `select`: a `var` declaration is unambiguously a statement, it parses whatever the expression's type turns out to be, and compilation fails before the declaration means anything. The nested script is run on the same process inside the same implicit transaction, so it sees the tables this script creates (as `EventHandlers.d4` and `ReproContextError.d4` already rely on).

## The tables, and which case each one serves

Five tables, created at the top of the script after `SafeDrop` on each name, dropped at the end. The column types matter: key matching is by type signature, and the only implicit conversions in the system are between the numeric types, `Date` to `DateTime`, and `String` to and from `Name` (`src/engine/DAE/Schema/SystemCatalog.d4` around line 1,118). There is **no** `String` to `Integer` conversion, which is what makes the type-mismatch cases below resolve to nothing rather than quietly converting.

```d4
create table TableIndexerSingle { ID : Integer, Data : String, key { ID } };
create table TableIndexerTwoKeys { ID : Integer, Name : String, Data : String, key { ID }, key { Name } };
create table TableIndexerSameTypeKeys { Left : Integer, Right : Integer, Data : String, key { Left }, key { Right } };
create table TableIndexerPairKey { A : Integer, B : Integer, Data : String, key { A, B } };
create table TableIndexerWidths { ID : Integer, A : Integer, B : String, Data : String, key { ID }, key { A, B } };
```

**Working cases** (compiled in the script itself, no dynamic `Execute` - they must compile):

- **Explicit key.** `TableIndexerSingle[1 by { ID }]` returns the row with `ID = 1`. Assert a column of the returned row, not just that it did not raise. No coverage script uses the `by` clause on an indexer today.
- **Implicit key, one key fits by width and type.** `TableIndexerSingle[1]` returns the same row as the explicit form above.
- **Implicit key chosen by type between two keys of the same width.** `TableIndexerTwoKeys[1]` resolves to the `ID` key and `TableIndexerTwoKeys['Alpha']` to the `Name` key. Assert both return the row they should - a wrong key here would silently return a different row, which is the failure this case exists to catch.
- **Implicit key chosen by width.** `TableIndexerWidths[1]` resolves to `key { ID }` and `TableIndexerWidths[1, 'x']` to `key { A, B }`.
- **Implicit key with the terms in the other order.** `TableIndexerWidths['x', 1]` must return the same row as `TableIndexerWidths[1, 'x']`: the compiler tries every ordering of the terms, and for the matching ordering it rebuilds the key column by column, so `B = 'x'` and `A = 1`. This is the one assertion that covers the permutation machinery end to end, and nothing covers it today. Give the table a second row that would be returned if the columns were paired up the wrong way round, so a regression is a wrong row rather than an empty result.
- **The explicit form is the way out of an ambiguity.** `TableIndexerSameTypeKeys[1 by { Left }]` and `TableIndexerPairKey[1, 2 by { A, B }]` both work, where the implicit form on the same tables raises (next section). Worth asserting: it is the answer a developer who hits 105275 or 105259 needs.

**Error cases** (each through `Execute`, each asserting the code in the flattened chain):

| Expression inside `Execute` | Code | Why it raises |
|---|---:|---|
| `TableIndexerSingle[1, 2, 3, 4, 5, 6]` | 105264 | more than five terms in an implicit indexer; checked before any key is looked at |
| `TableIndexerSameTypeKeys[1]` | 105275 | two keys of the same width whose column *types* are the same, so no term could tell them apart. Key signatures compare by type and modifier only, never by column name (`Schema.Signature.Equals`), which is why `Left` and `Right` collide and `TableIndexerTwoKeys`' `ID` and `Name` do not |
| `TableIndexerPairKey[1, 2]` | 105259 | one key of two same-typed columns: both orderings of the terms match it equally well, with the same narrowing score and the same conversion path length |
| `TableIndexerSingle[1, 2]` | 105260 | no key of that width exists, so there is nothing to match |
| `TableIndexerSingle['One']` | 105260 | a key of that width exists but no ordering of the terms matches its types - a different path to the same throw, worth both arms |
| `TableIndexerSingle[1 by { ID, Data }]` | 105261 | the `by` clause names two columns where one term was given. **The plan ticket described this code wrongly** as "a term that is not usable as a key reference at all": the compiler raises it when the number of resolved key columns does not equal the number of terms (line 12,724), which only the explicit form can produce. Follow this ticket, not that sentence |

Note the asymmetry worth a comment in the script: `TableIndexerSameTypeKeys` can never be indexed implicitly at all, because 105275 is raised while the candidate keys are being collected, before the terms' types are considered.

## Edge cases & interactions

- **`T[]` with no terms must keep working.** The empty indexer is the row extractor every script uses; it takes the same `by`-clause branch with a zero-column key, and zero equals zero, so it must not reach 105261. Covered by the rest of the suite rather than by a new assertion here - the script would fail loudly if it broke. Verify by inspection that no case in the new script changes that path.
- **The non-unique explicit key raises a *warning*, not an error.** A `by` clause whose columns are not a superset of any key adds `InvalidRowExtractorExpression` at warning level and compiles on. That line is already covered and is out of scope; do not add a case for it, and do not let one of the cases above stumble into it - every `by` clause above is a superset of a real key, so none does. Verified by inspection against lines 12,599-12,617.
- **One arm stays uncovered deliberately.** Line 12,689 - two matching orderings with the same narrowing score but *different* conversion path lengths - needs a chain of user-defined implicit conversions to construct, and the construction would be more intricate than the behaviour it pins. Leave it, and say so in the script's header comment so the next reader knows it is a decision and not an oversight.
- **Transaction state.** The harness runs the whole document inside one implicit transaction, so `TransactionCount()` is 1 throughout and every statement here runs in that one transaction. A caught compile error from a nested `Execute` unwinds through `BeginTransactionalCall`/`EndTransactionalCall` to a savepoint, as every other caught error in the suite does; the script needs no explicit transaction and should open none. If a caught error turns out to poison the enclosing transaction, that is a real finding - report it, do not work around it by splitting the script.
- **Name collisions with other scripts.** Every script runs on its own fresh server copied from the template, so cross-script collision is not possible; `SafeDrop` at the top is still the convention, for a rerun against a server that kept state.
- **Five tables, one script.** Keep them in one script rather than splitting: the cases are all the same subject, the script is short, and the per-case cost in the suite is dominated by the server copy, not by the D4.

## Verification

Per this project's by-stage testing rule, targeted only - CI runs the full suite after the commit.

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --filter "Name=TableIndexer|Name=TableLibrary|Name=SpecifyClause" --logger "console;verbosity=normal"
```

`TableLibrary` and `SpecifyClause` come along as the neighbours that would notice a manifest mistake. Name the filter in the handoff.

Then prove the ticket's point rather than assuming it: rerun the single case with coverage collected and check those five lines are no longer at zero.

```
dotnet test tests/DAE.Server.Tests --filter Name=TableIndexer --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/tableindexer
```

The Cobertura XML carries `<line number="..." hits="..."/>` per file; find the `Compiler.cs` class elements and read the hit counts for the five throw lines (and for the 12,687-12,695 block). Record the before/after numbers in the handoff. If the XML's shape makes a per-line read awkward, say so and fall back to `python tools/coverage-report/coverage_report.py artifacts/coverage/tableindexer`, which reports the Compiler area and the files of each gap - but do not claim the lines are covered without having looked.

## Rules this ticket has to respect

- **Expected values are the specification.** There is no reference runtime for this engine, so what it does today is correct by definition. If a case raises a code other than the one in the table above, the engine is right and this ticket is wrong: record the code the engine actually raises, say in the handoff which expectation you changed and why, and never adjust an expected value later to make some other change pass.
- **Source hygiene.** Coverage scripts are UTF-8 with **no** byte-order mark, CRLF line endings, tabs for indentation. Match that.
- **Manifest.** Add `{ "script": "TableIndexer", "status": "pass" },` to `tests/DAE.Server.Tests/coverage-manifest.json`, sorted by script name in ordinal order - between `TableCallsWithStackReferences` and `TableLibrary`. A new script is the one case where that file is meant to change. The script needs nothing else: the layout copies the `Documents` folder as it stands, and the library's `.d4l` does not list documents. `TableIndexer` falls in the third of the three agent test parts (its name does not start with A-R).
- **No migration note.** This adds a test; nothing an application, a deployment script or a C# extension could notice changes. Do not add an entry to `docs/migration-notes.md`.
- **No architecture page.** Nothing here is new architecture.

## TODO

- Write `tests/libraries/TestFramework.Coverage.Scripts/Documents/TableIndexer.d4`: a header comment saying what the script covers, that compile errors are reached through a dynamic `Execute` and why, that codes are read out of the flattened error chain, and which arm is deliberately left uncovered; then the five tables, their rows, the working cases, the error cases, and the drops.
- Add the `TableIndexer` entry to `tests/DAE.Server.Tests/coverage-manifest.json` in ordinal position.
- Drop the stale `// TODO: Build this coverage` marker from the `TableIndexer` line of `tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4` (line 84), leaving the `ExecuteScript` call as it is. That file is the 2008 hand-wired run list and nothing executes it today - it is kept as the record of the original authors' intent - but leaving the marker there after building the coverage guarantees someone re-files this ticket. Change only that one line.
- Build, then run the targeted filter above; iterate until the case passes.
- Run the single case with coverage collected and read the hit counts for lines 12,624 / 12,636 / 12,695 / 12,720 / 12,724 of `src/engine/DAE/Compiling/Compiler.cs`.
- Hand off to review with: the filter you ran, the before/after hit counts, any expected code that differed from this ticket's table and what the engine actually raised, and the line numbers as they stand in the file you measured.
