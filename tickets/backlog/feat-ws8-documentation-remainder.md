description: Finish moving the documentation to Markdown: convert the old reference material, delete the old sources, and write the administration, contributing and migration guides.
architecture: docs/modernization-plan.md#ws8-documentation
prereq: docs-convert-asciidoc
tradeoffs: Several items (admin guide, migration guide) can only be written once installer and client work has landed, so cutting them early produces tickets that sit deferred for a long time.
----
## What this is

An umbrella for the WS8 work items after `docs-convert-asciidoc`. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then.

**Promote when** `docs-convert-asciidoc` has landed. WS8 writes only under `docs/` and `tools/`, so most of it can run beside other work and suits lower-capability agents.

## Work items

Scope per the plan's WS8 section and work-item table.

- `docs-convert-legacy-reference` — the DocBook reference material in `legacy/Documentation/` into `docs/reference/`; repair the 206 dead links (K17), using the list the asciidoc conversion tooling produced.
- `docs-remove-old-sources` — delete `Documentation/`, `legacy/Documentation/` and `book.json`; repoint the root `README.md`.
- `docs-contributing` — build, test, layout, release checklist; fold `CompilerDefines.txt` and the error prefix list into `docs/reference/`.
- `docs-link-check-ci` — link checker in CI (needs `build-ci`).
- `docs-reference-generator` — Markdown emitter for the catalog documentation operator; regenerate and diff. The operator's library (`Documentation`) was parked under `legacy/libraries/Documentation` by the reorganization, and `legacy/` is excluded from the build and "not to be fixed" (AGENTS.md). Planning this item has to decide whether to move the library back to `libraries/` (and into the build) or write the emitter somewhere else.
- `docs-admin-guide` — needs `store-rebuild-workflow` (`feat-ws4-store-followups`) and `installer-msi` (`feat-ws9-installer-and-release`).
- `docs-migration-guide-custom-code` — needs `ui-windows-client-runs` (`feat-ws6-windows-client-and-ide`).

## Cutting rules

One ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`. Items whose prerequisites are not yet filed (likely `docs-admin-guide` and `docs-migration-guide-custom-code`) are parked as a new backlog ticket `feat-ws8-documentation-later` that names what they wait for.
