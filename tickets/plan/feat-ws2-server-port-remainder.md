description: Finish the parts of the engine and server port that are not needed for the first green test run: loading library assemblies with their own dependencies, and tracking down a rare concurrency error.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
tradeoffs: Neither item blocks a gate; a maintainer may hold them until a real library with native dependencies or a recurrence of the concurrency error forces the issue.
----
## What this is

An umbrella for the WS2 work items left after Phase 1. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then.

**Promote when** gate G1 holds (`build-runtime-layout`, `test-harness-inprocess` and `port-catalog-emitter-race` have landed).

## Work items

Scope and difficulty per the plan's WS2 table and design note "Assembly loading".

- `port-library-loading` — resolver that probes registered library directories for managed and native dependencies, everything in the default load context, no unloading; test with a library whose assembly has a private NuGet dependency and a native dependency.
  - Context from `build-runtime-layout`: each library folder in the runtime layout (`artifacts/layout/<debug|release>/Libraries/<Name>/`) holds the library's assembly and its *private* managed package files (packages that no project the library references has), flattened into the folder for the Windows runtime: the `win-x64` file, else `win`, else the generic one, one file per name and no `.deps.json`. Today Connection's private set is System.Data.OleDb, System.Configuration.ConfigurationManager, System.Diagnostics.PerformanceCounter and System.Security.Cryptography.ProtectedData, System.Internet's is Newtonsoft.Json, and the other libraries have none. A private **native** asset is a build error in `tools/layout/Layout.csproj` (target `_DataphorComputeLayout`); this work item lifts it when it adds native probing, and decides where native files go in a library folder. No library has a private native asset yet.
- `port-concurrent-ddl-investigation` — K3 ("collection was modified" under concurrent DDL, site unknown): stress harness with stack capture until the site is found; fix or file. Seen again on 2026-11-14 by the `repo-delete-dead-trees` review: 1 error in the spike's 8×15 concurrency step, with store writes already serialized (the plan's K3 row says it was seen only before serialization); the implementer's two runs on the same tree had 0. The spike prints only the message, not the stack.

Moved elsewhere: Phase 1 owns `port-catalog-emitter-race` and `port-engine-runtime-fixes`; K4 was fixed in `build-server-project`; `port-abandoned-session-cleanup` and `port-server-on-net10` are in `feat-ws5-protocol-and-hosting`, because both need the CoreWCF host.

## Cutting rules

Re-read the WS2 section and the current code; one ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`.
