description: Finish cleaning the repository once the new build exists: remove dead Silverlight code branches, stray source files and the old build files, and replace the remaining checked-in third-party binaries with packages.
architecture: docs/modernization-plan.md#ws12-repository-hygiene
tradeoffs: None of this changes behaviour; a maintainer may let it trail the release, since each item only gets cheaper once its last consumer is converted.
----
## What this is

An umbrella for the WS12 items after Phase 1, plus the two late WS1 clean-up items. When promoted to `plan/`, the planning agent replaces it with one ticket per item whose prerequisites are on the board, parks the rest as a new backlog ticket `debt-ws12-hygiene-later`, and deletes this file.

**Promote when** gate G1 holds, for `hygiene-silverlight-conditionals`; the rest wait on WS6 and WS9 tickets existing.

## Work items

- `hygiene-silverlight-conditionals` — remove the `#if SILVERLIGHT` branches (the plan counts 46 in 26 files) and the `AllowSilverlightClients` setting (migration-notes entry exists as "planned"). Needs `test-harness-inprocess`.
- `build-globbing-cleanup` (WS1) — delete source files no project compiles, then switch projects to default globbing. Needs `ui-dataphoria-port` and `device-connection-cleanup`.
- `build-remove-legacy-build` (WS1) — delete the remaining legacy solutions and `packages.config` files. Needs `ui-dataphoria-port` and `installer-msi`.
- `hygiene-vendored-binaries` — final sweep: `external/` is empty or each remaining file has a recorded reason (D11). Needs `ui-editor-and-docking` and `installer-msi`.

- `hygiene-remove-net-spike` — delete `tools/net-spike/` once nothing depends on it: its README says it is throwaway, but until the in-repo build, the harness and the store concurrency test exist, it is the only reproduction of the baseline. Update the references to it in `AGENTS.md`, the plan's Appendix A and `docs/architecture.md`. Needs `test-harness-inprocess` and `store-sqlite-concurrency`.

Dead `#define` switches and unused `using` directives are cleaned only in files otherwise being edited, never as a sweep.

## Cutting rules

`prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`; most of these suit lower-capability agents and can carry `difficulty: easy`.
