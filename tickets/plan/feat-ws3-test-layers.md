description: Extend the automated test suite beyond the core engine scripts: a parser check over every D4 file, form-derivation snapshots, the sample applications, performance baselines, and a survey of what is still untested.
architecture: docs/modernization-plan.md#ws3-test-suite
tradeoffs: Each layer costs run time on every later change; a maintainer may prefer to add layers only when a regression shows a layer is missing.
----
## What this is

An umbrella for the WS3 work items left after Phase 1. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then.

**Promote when** gate G1 holds (`test-harness-inprocess` has landed; `test-coverage-gap-analysis` additionally needs `test-triage-known-failures`).

## Work items

Layers and scope per the plan's WS3 "Target: layers" table and work-item table.

- `test-parser-roundtrip-corpus` — L0: every `.d4` file in the repository parses, emits and re-parses to the same tree.
- `test-frontend-derivation-golden` — L5: derive every page type for the sample libraries and compare the form XML to golden files; load every `.dfd`/`.dfdx` in the repository through the deserializer; customization merge/difference round trip.
- `test-samples-corpus` — L7 for the repository samples (every sample registers and derives on the in-memory and Simple devices). Also the base for `compat-corpus-harness` in `feat-ws13-application-compat`.
- `test-perf-baseline` — timing scripts plus a small benchmark project; numbers published under `docs/`.
  - Arm from `test-triage-known-failures`: the coverage script `Timings` is `known-failure` against this umbrella (`Unable to resolve operator reference "BenchmarkExecute"`). It times inserts, updates and deletes with `BenchmarkExecute`, whose `create operator` line (class `System.SystemBenchmarkExecuteNode`, which the engine still has) is commented out in the script itself, and it has no assertions. `Timing` passes but is also a timing script with no assertions. Decide here whether the perf layer creates the operator (in the helper library, not by editing the script) and how the two scripts' numbers are recorded; flip `Timings`' manifest entry in the same change.
- `test-coverage-gap-analysis` — with coverage data, list engine areas with no script coverage and file a ticket per area.
  - Arm from `test-triage-known-failures`: these pass counts are not coverage. `TableIndexer` was named by a legacy ignored test ("Build this coverage") but has no script file. Nine scripts pass with no statements at all (manifest note "no assertions"): `Functions`, `LibraryFilesCoverage`, `LockAllocation`, `Register`, `RowAllocation`, `ScalarAllocation`, `SerializationDevices`, `TestGH37`, `Warnings`; their names (table indexers, library files, scalar, row and lock allocation, functions, warnings, device serialization) are areas the original authors meant to cover and did not. Also read `test-pre-3-1-scripts-policy` (in `blocked/` or wherever it has gone): the scripts it retires leave further gaps (error-code resource strings, stream leaks, row allocation, derivation).

Owned elsewhere: `test-store-contract` → `feat-ws4-store-followups`; `test-device-sql-golden` and `test-device-live` → `feat-ws7-devices-and-libraries`; `test-client-server` → `feat-ws5-protocol-and-hosting`; `test-ui-smoke` → `feat-ws6-windows-client-and-ide`.

## Cutting rules

Re-read the WS3 section, the harness as built, and `tickets/rules/10-dataphor.md` (testing rule); one ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`.
