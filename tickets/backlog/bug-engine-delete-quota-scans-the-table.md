description: Deleting one row at a time from a table with the "delete the first row" form of the delete statement gets slower the bigger the table is, so a loop that empties a table takes time proportional to the square of its size — about a quarter of an hour for ten thousand rows where the same work used to take eight seconds.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Runtime.Instructions/TableNodes.cs, src/engine/DAE/Runtime.Instructions/QuotaNode.cs, tests/DAE.Server.Tests/coverage-manifest.json, tests/libraries/TestFramework.Coverage.Scripts/Documents/Timings.d4, docs/perf-baseline.md
repro: verified
severity: edge-case
likelihood: unusual
tradeoffs: It is slow rather than wrong, in a statement form most applications do not use, and the one place in this repository that does is a timing script with no assertions — so a maintainer may reasonably leave it until someone meets it in an application.
difficulty: medium
----
## What happens

A `delete` statement whose target is a *quota* expression — in D4, `<table> return <n>`, optionally `by { <order> }`, which means "the first n rows" — costs time proportional to the number of rows in the table, every time it runs. A loop that empties a table one row at a time with that form therefore costs time proportional to the square of the table's size.

Reading the same quota expression is not slow. Deleting through it is. That is the finding, and it is what makes this a defect rather than a cost.

## How it was measured

Measured on 2026-11-21, in Debug on one slow development laptop, through the engine's own `BenchmarkExecute` operator (which times a script and returns the elapsed time) on a table `T { ID : Integer }` on the in-memory `System.Temp` device.

Draining a table of N rows with the statement the timing script uses, `while exists (T) do delete T return 1 by { ID };`:

| Rows | Drain | Cost per row |
|---|---|---|
| 250 | 397 ms | 1.6 ms |
| 500 | 1,885 ms | 3.8 ms |
| 1,000 | 7,866 ms | 7.9 ms |
| 2,000 | 34,308 ms | 17.2 ms |
| 4,000 | 150,879 ms | 37.7 ms |

Each doubling of the row count costs about 4.4 times as much, and the cost of one delete grows linearly with the table's size — roughly 9 microseconds per row in the table, per statement. Extrapolating to 10,000 rows gives about a quarter of an hour.

Four controls, all at 2,000 rows, which is where the drain costs 34 seconds:

| What | Cost | What it rules out |
|---|---|---|
| `while exists (T) do delete T return 1;` (no order clause) | 34,136 ms | the order clause: it is not the cause |
| `for LIndex : Integer := 1 to 2000 do delete (T where ID = LIndex);` | 130 ms | per-row deletion, the device, and the transaction: 2,000 single-row deletes are fast when the target is a restriction over the key |
| `delete T;` (the whole table in one statement) | 66 ms | deleting many rows: that is cheap, once |
| reading the same quota 2,000 times (`Count(T return 1 by { ID })` in a loop, nothing deleted) | 39 ms | the quota expression itself: opening it is about 20 microseconds and does not grow with the table |

So the per-statement cost appears only when a quota is the target of a delete, and it is about 870 times the cost of reading the same expression.

## Where to look first

`DeleteNode.InternalBindingTraversal`, `src/engine/DAE/Runtime.Instructions/TableNodes.cs` (around line 1104). It binds the delete's target inside a cursor context asking for a static, navigable, updateable, *isolated* cursor, and when the target's cursor type is not already static it replaces the target with `Compiler.EmitCopyNode(...)` — that is, the rows to be deleted are copied before any of them is deleted. For `delete T` that copy happens once and every row is then deleted, which is the 66 ms above. For `delete T where ID = k` the copy is one row, because the restriction over the key is satisfied from the device's index, which is the 130 ms above. The measurements say that for a quota over a base table variable the copy is of the whole table rather than of the quota's n rows, but **which node is copied was not isolated** — that is the first thing this ticket should establish, and `QuotaNode.DetermineCursorBehavior` (`src/engine/DAE/Runtime.Instructions/QuotaNode.cs`, around line 195), which takes its cursor type straight from its source, is the other half of the picture.

Everything above the paragraph break is measured. This paragraph is inference from the measurements and from reading the code; treat it as the lead, not the answer.

## Why it matters, and what it blocks

The coverage corpus's `Timings` script exists only to be timed, and its third block is this drain over 10,000 rows. Its own comments, written in 2008, record about 8 seconds for that block; the extrapolation above is about a quarter of an hour. The script's other blocks are now several times *faster* than those comments record (inserting the 10,000 rows takes 431 ms against about 5 s; updating them 1,461 ms against about 9.5 s), and the interpreter itself runs at about the speed the comments record, so the drain is a genuine outlier and not an artefact of the build configuration or of the machine.

`test-perf-timing-scripts` therefore could not put `Timings` in the performance baseline. It left:

- the `Timings` entry in `tests/DAE.Server.Tests/coverage-manifest.json` as a `known-failure` against this ticket, with a deliberately short timeout so that the suite does not pay for the grind;
- a row missing from `PerfBaselineTest`'s scenario table, with a comment saying that fixing this makes it a one-line addition;
- the numbers and the curve published in `docs/perf-baseline.md`.

Fixing this drains all three.

## What "fixed" looks like

The cost of one `delete <table> return <n>` does not grow with the number of rows in the table, so the drain loop is linear in the table's size. Concretely: the 250-to-4,000-row table above becomes roughly flat in cost per row instead of quadrupling with every doubling, and the `Timings` script finishes in a time of the same order as its 2008 comments record.

The reproduction belongs at the lowest layer that shows it, which is a D4 script through the in-process engine harness rather than anything above it. One case asserting that the per-row cost of the drain does not grow with the table size — two table sizes, a ratio, generous bounds — would hold the class rather than one instance; a case asserting an absolute time would be a machine-dependent test and should not be written.

Note for whoever picks this up: never edit `Timings.d4` to make it pass. There is no reference runtime, so a coverage script's assertions and its shape are the record of correct behaviour.
