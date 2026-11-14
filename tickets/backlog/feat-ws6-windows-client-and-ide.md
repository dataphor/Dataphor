description: Make the Windows client and the Dataphoria IDE actually run on .NET 10, looking and behaving as before, by replacing the old menu and toolbar controls, run-time script compilation and outdated third-party components.
architecture: docs/modernization-plan.md#ws6-windows-client-and-dataphoria
tradeoffs: This is the largest workstream and its long tail (designers, painting, resources) is hard to estimate; a maintainer may want the server side fully proven first even though the layering and control work does not depend on it.
----
## What this is

An umbrella for WS6. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then.

**Promote when** gate G2 holds (a hosted .NET 10 server passes the client/server tests). The owner may promote it earlier: `ui-client-layering` and `ui-controls-port` need only the core build.

## Work items

Scope, prerequisites and difficulty per the plan's WS6 issue table and work-item table.

- `ui-client-layering`, `ui-controls-port`, `ui-frontend-client-port`, `ui-scriptaction-roslyn` (not optional: applications use script actions heavily, Q6; fixes K8), `ui-windows-client-port`, `ui-windows-client-runs` (gate G3), `ui-editor-and-docking`, `ui-dataphoria-port`, `ui-dataphoria-designers`, `ui-config-utility`.
- `test-ui-smoke` (from WS3, layer L6).
- Known defects fixed here: K5 (Dataphoria's `ScriptExecutor`), K7, K8, K9, K24.

Rules that matter here: `WFDEV006` stays a visible warning until a project's legacy controls are replaced, then becomes an error in that project; never suppress it. Form node base classes and interfaces stay source-compatible for custom extensions, and every change to them goes into `docs/migration-notes.md` (D16). Start in the same DPI mode as today.

## Cutting rules

Re-read the WS6 section and the current code; one ticket per work item; *hard* items to `plan/`; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`; items waiting on an uncut umbrella are parked as a new backlog ticket `feat-ws6-windows-client-and-ide-later`.
