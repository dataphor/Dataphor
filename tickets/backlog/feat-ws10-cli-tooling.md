description: Provide one command-line tool for running D4, managing server instances and services, rebuilding catalogs and testing libraries, keeping the old tool names working as thin wrappers.
architecture: docs/modernization-plan.md#ws10-utilities-and-developer-tooling
prereq: feat-ws5-protocol-and-hosting, feat-ws6-windows-client-and-ide
tradeoffs: The existing tools still work once ported; a maintainer may ship 4.0 with them and treat the unified tool as a later convenience.
----
## What this is

An umbrella for WS10. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then. The `prereq:` above orders planning only: the CLI needs `ui-client-layering` (WS6) and `host-generic-host` (WS5) to exist as tickets first.

**Promote when** gate G2 holds; Phase 4 by the plan's sequencing.

## Work items

Commands and scope per the plan's WS10 section.

- `tool-cli-skeleton` (`dataphor` on `System.CommandLine`; `d4 run/eval` in-process and remote; replaces the two vendored argument parsers, which fixes K23), `tool-cli-server-commands`, `tool-cli-catalog-commands`, `tool-cli-library-test`, `tool-d4-repl`.
- `D4Runner`, `DAEServer` and `DAEService` remain as thin shims with their old names and switches; existing scripts and service registrations depend on them.
- `tool-vscode-d4-syntax` stays in `backlog/` as `feat-tool-vscode-d4-syntax`; so do the further ideas the plan lists (language server, health endpoints, structured logs).

## Cutting rules

Re-read the WS10 section and the hosts as built; one ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`.
