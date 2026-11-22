description: A query can pick a single row out of a table by its key, and the five errors the engine reports when that is written ambiguously or wrongly have never been triggered by any test - so the messages a developer would actually see are the part nobody checks.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Compiling/CompilerException.cs, tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/
tradeoffs: The feature itself works and is exercised constantly, so this only adds error-path coverage to an already well-covered area - a maintainer may rank it below the areas sitting at zero; against that, it is cheap, it pins five error codes that are a compatibility surface, and it closes a request the test library has been carrying unanswered since the 3.x days.
----
## Where this came from

The hand-wired coverage test list, deleted by `test-harness-inprocess`, named a script that has never existed. The request is still in the test library, at `tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4` line 84, and this is it verbatim:

```
ExecuteScript("Coverage.Scripts", "TableIndexer"); // TODO: Build this coverage
```

`test-harness-inprocess` recorded that there is no `TableIndexer.d4` and therefore no manifest entry for it, and passed the question here. This ticket is the answer: build that coverage.

## What the subject is - and is not

The subject is the D4 **table indexer expression**, the `MyTable[...]` form that picks one row out of a table by a key. It is compiled in `src/engine/DAE/Compiling/Compiler.cs`, in the region around lines 12,610-12,715, and it has two forms:

- **an explicit key** - the terms name the key columns; and
- **an implicit key** - the terms are positional and the compiler has to work out which of the table's keys is meant, refusing if more than one fits.

**It is not a data structure.** The name invites a reader to `src/engine/DAE/Runtime.Data/Index.cs`, which is the wrong file twice over: it is a different thing, and `DAE.csproj` does not even compile it (`build-globbing-cleanup` lists it among the files to delete). Anyone picking this up should start at the `Compiler.cs` region above.

## The measurement, which is the reason to file

This is not a low-coverage area. The Compiler area is at 80.9 % and the table-indexer region itself is at **86.9 % (86 of 99 coverable lines)** on the full three-part run of 2026-11-21 at commit `f6200026`. The happy path is exercised constantly, because ordinary scripts index tables by key all the time.

What is missing is every error path, and that was measured directly rather than inferred. All five of the expression's own error codes sit at **zero hits**:

| Line | Error | Code |
|---:|---|---:|
| 12614 | `TooManyTermsForImplicitTableIndexer` | 105264 |
| 12625 | `PotentiallyAmbiguousImplicitTableIndexer` | 105275 |
| 12684 | `AmbiguousTableIndexerKey` | 105259 |
| 12709 | `UnresolvedTableIndexerKey` | 105260 |
| 12713 | `InvalidTableIndexerKey` | 105261 |

The 13 uncovered lines in the region are those five throws plus the block at 12,677-12,685, which is the loop that detects an ambiguous key. So the whole of the compiler's "this indexer does not resolve, and here is why" logic has never run.

Error codes are a compatibility surface in this repository: an application can match on one, and they must not change without a ticket that says so. A script that pins these five is worth more than its length, and nothing pins them today.

## What a coverage script should exercise

A D4 script named `TableIndexer`, in `tests/libraries/TestFramework.Coverage.Scripts/Documents/`, run through the in-process harness. It needs two or three tables of its own: one with a single key, one with two keys of the same column count (which is what makes an implicit indexer ambiguous), and one with keys of differing widths.

- **Indexing by a declared key**, naming the key columns explicitly, and asserting the row that comes back is the right one.
- **Indexing by an implicit key** on a table where exactly one key fits the terms given, asserting the same.
- **More than five terms** in an implicit indexer, which is the literal condition at line 12,614: `TooManyTermsForImplicitTableIndexer` (105264).
- **Terms that fit more than one key of the same width**: `PotentiallyAmbiguousImplicitTableIndexer` (105275) and `AmbiguousTableIndexerKey` (105259). These two are distinct conditions in the compiler - a script should provoke each and check it gets the right one, not just that it got an error.
- **Terms that match no key at all**: `UnresolvedTableIndexerKey` (105260).
- **A term that is not usable as a key reference at all**: `InvalidTableIndexerKey` (105261).

Each error case should assert on the code, not on the message text, and the script's own assertions follow the `raise Error(...)` convention the other scripts use. Add the script to `tests/DAE.Server.Tests/coverage-manifest.json` as part of this work - a new script is the one case where that file is meant to change.

Keep the expected codes exactly as the compiler raises them: there is no reference runtime for this engine, so what it does today is the specification, and an expected value here must never be edited later to make a change pass.
