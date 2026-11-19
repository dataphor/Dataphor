description: Design a pass that measures what the engine's test scripts actually exercise — with coverage data from a run of the harness, list the engine areas that have no script coverage, and file one ticket per area so the gap closes by work on the board rather than by waiting for a regression.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/coverage-manifest.json, tests/libraries/TestFramework.Coverage.Scripts/Documents/, src/engine/
difficulty: medium
----
## What this is

The plan's WS3 work item `test-coverage-gap-analysis`: "With coverage data: list engine areas with no script coverage (candidates: application transactions, server links, security, library upgrade scripts, streams) and file a ticket per area." Pass counts in the coverage manifest are not coverage — a script that passes without exercising a path is worthless as a regression signal; this ticket inverts the question and asks "what in the engine is not exercised by any script that passed?"

## Known gaps (starting evidence)

The umbrella's arm carried these into planning; they are the first candidates to confirm with coverage data:

- **`TableIndexer`** — named by a legacy ignored test ("Build this coverage"), but the script file does not exist. Named in `test-harness-inprocess`'s handoff for this ticket to list.
- **Nine scripts that pass with no statements** (manifest note "no assertions: the file holds only comments"): `Functions`, `LibraryFilesCoverage`, `LockAllocation`, `Register`, `RowAllocation`, `ScalarAllocation`, `SerializationDevices`, `TestGH37`, `Warnings`. The names tell what the original authors meant to cover and did not (table indexers, library files, scalar/row/lock allocation, functions, warnings, device serialization).
- **Scripts retired by `test-pre-3-1-scripts-policy`** — read that ticket (in `blocked/` at filing, later elsewhere): the scripts it retires leave further gaps (error-code resource strings, stream leaks, row allocation, derivation). `Exceptions` and `StreamAllocation` are flagged there for this ticket to pick up.
- **Plan candidates** (from the WS3 work-item table, unverified until coverage runs): application transactions, server links, security, library upgrade scripts, streams.

## Open questions this plan must settle

1. **Coverage tool.** `coverlet` (standard for `dotnet test`) collects line coverage on managed code during the harness run. Decide whether:
   - run `coverlet` as part of `dotnet test` with an option (`--collect:"XPlat Code Coverage"`), ship the result as a build artifact;
   - or run a separate `dotnet-coverage` or `JetBrains dotCover` pass.
   Default: `coverlet` with the standard XPlat collector. The report format is Cobertura.
2. **Granularity.** Report per-assembly line coverage, with a drill-down for classes under 50% in the "engine" assemblies (`Alphora.Dataphor.DAE.*`, the server). Classes not in the engine (devices with their own tests, client, UI) are listed but not gated. Settle the exact assembly include/exclude list and the thresholds for "needs a ticket".
3. **What counts as "an area".** A class or a namespace, not a file; e.g. "application transactions" is `Alphora.Dataphor.DAE.Server.ApplicationTransactions.*`. Decide the mapping from Cobertura classes to areas, probably a short checked-in `.json` map.
4. **The output artefact.** Markdown under `docs/coverage-report.md` (hand-maintained after each pass), or a JSON that a small script turns into the markdown. Default: run the gap pass, hand-write the markdown with the current numbers, file one `debt-` ticket per area that falls below threshold, delete this plan ticket.
5. **Which tickets to file.** For each area under threshold: file `debt-<area>-coverage` in `backlog/` carrying `architecture: docs/modernization-plan.md#ws3-test-suite`, the area name, the current coverage percentage, and the kinds of behaviour a new coverage script would exercise. Do not write the scripts here — that is the debt tickets' job.

## Expected output

This ticket is a *planning* ticket that produces further tickets (one `debt-` ticket per area). The implement step runs the coverage pass once, produces the report, files the `debt-` tickets, and delete-promotes this plan ticket. The slug `test-coverage-gap-analysis` stays on the implement ticket that files the last batch (or on the single implement ticket that does the whole pass).

Candidate implement-ticket shape:

- `test-coverage-collect` (implement) — add `coverlet` to the `dotnet test` run, produce the Cobertura report, do nothing with it.
- `test-coverage-gap-analysis` (implement, prereq above, keeps the slug) — read the report, write `docs/coverage-report.md`, file one `debt-` ticket per area under threshold.

## Edge cases & interactions

- **Dead code in the engine.** Several engine areas are intentionally unused in Phase 1 (removed transports, deferred WCF code, SQL CE paths). These show as 0% coverage but do not warrant `debt-` tickets; carry an allow-list for "code known to be dead", pointing at the ticket that owns its removal (`hygiene-silverlight-conditionals`, `server-remove-remoting-and-dtc`, …).
- **Per-script coverage.** `coverlet` can produce per-test coverage; useful later for pruning the manifest ("script X adds no new lines over the rest"). Not in scope for this ticket — mention as a tripwire if the data costs little to collect.
- **The nine no-assertion scripts.** Each is a candidate for retirement under `test-pre-3-1-scripts-policy`'s pattern, or for rewriting with real assertions. This ticket files `debt-` tickets for the *behaviour* the script named; whether the retired file stays in the library is `test-pre-3-1-scripts-policy`'s call, not this one.
- **`TableIndexer`.** No script file, no entry in the manifest. File a `debt-tableindexer-coverage` ticket, with the original ignored test's intent ("Build this coverage") copied forward.
- **Overlap with `test-perf-baseline`.** Perf numbers and line coverage are independent; the two ticket streams do not block each other, but running them in one `dotnet test` invocation is convenient. Note the option without acting on it.
- **CI cost.** A full coverage run is slower than a plain run; the plan allows coverage "reported (not gated)" so it does not have to be on every PR. Decide whether CI runs it nightly, on-demand, or on every push; default to nightly.

## TODO

- Settle each open question above with a chosen default.
- Run a one-off coverage pass locally to confirm the tool produces a usable Cobertura report on the current harness (sanity check; results not kept).
- Emit the implement ticket(s) keeping the slug `test-coverage-gap-analysis` on the ticket that files the last batch.
- Delete this plan ticket on the way out.
