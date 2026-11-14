description: Measure whether compiling catalogs or query plans into .NET code would make the server meaningfully faster, and recommend whether to build it.
architecture: docs/modernization-plan.md#ws14-research-compiled-catalog-and-precompilation
prereq: feat-ws6-windows-client-and-ide
tradeoffs: It is an optimization and a redesign with no current performance complaint behind it; the measurements may show it buys little.
----
## What this is

An umbrella for WS14, time-boxed research (D13). When promoted to `plan/`, the planning agent replaces it with tickets for the items below and deletes this file. The `prereq:` above orders planning only; the real precondition is below.

**Promote when** the Windows client runs on .NET 10 (gate G3) and `test-perf-baseline` has landed. Not before (D13).

## Work items

Questions and scope per the plan's WS14 section. There is no IL-emission path to turn back on; the `ShouldEmitIL` flag is unused.

- `research-catalog-load-profile` — measure catalog load and plan compile cost on realistic catalogs with the SQLite store; report under `docs/`.
- `research-expression-compilation-spike` — prototype delegate compilation (`System.Linq.Expressions`) for a handful of scalar node types behind the existing `PlanNode` interface; benchmark on the memory device; recommend. Only if the load profile leaves room for a gain.

Output is a recommendation for the owner (`blocked/` with the proposal), not production code.
