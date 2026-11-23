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
- `docs-link-check-ci` — link checker in CI (needs `build-ci`). The docs workflow already exists - `.github/workflows/docs.yml`, added by `docs-architecture-hierarchy` for the architecture page-size check - so this item adds a step to it rather than creating a workflow.
- `docs-reference-generator` — Markdown emitter for the catalog documentation operator; regenerate and diff. The operator's library (`Documentation`) was parked under `legacy/libraries/Documentation` by the reorganization, and `legacy/` is excluded from the build and "not to be fixed" (AGENTS.md). Planning this item has to decide whether to move the library back to `libraries/` (and into the build) or write the emitter somewhere else.
- `docs-admin-guide` — needs `store-rebuild-workflow` (`feat-ws4-store-followups`) and `installer-msi` (`feat-ws9-installer-and-release`).
  - Arm from `store-sqlite-concurrency` (planning): the SQLite catalog store always runs in write-ahead-log mode, so `DAECatalog.sqlite` has `-wal` and `-shm` files beside it and the instance's `Catalog` folder must be on a local disk (WAL does not work on network file systems); back up or copy the catalog only while the server is stopped, or all three files together. A session that waits longer than `BusyTimeout` (in `catalogstoreconnectionstring`, default 30 s) for another session's catalog write lock gets error 130103; say what that means and how to find the long transaction.
- `docs-migration-guide-custom-code` — needs `ui-windows-client-runs` (`feat-ws6-windows-client-and-ide`).

## Cutting rules

One ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`. Items whose prerequisites are not yet filed (likely `docs-admin-guide` and `docs-migration-guide-custom-code`) are parked as a new backlog ticket `feat-ws8-documentation-later` that names what they wait for.

## Notes from planning `docs-convert-asciidoc`

That work item was split into `docs-link-checker`, `docs-convert-tooling` and `docs-convert-asciidoc` (implement/). What the items here can rely on once they land:

- `docs-convert-legacy-reference`: `tools/docs-convert/convert.py docbook FILE.xml … --out DIR` is the standalone DocBook → Markdown stage (pandoc 3.14 DocBook reader plus the same clean-up pass), with the page map and fragment-resolution rules passed in. `tools/docs-convert/legacy-links.tsv` lists the links into legacy pages (204; the source survey counted 206 `.html` occurrences, two of which sit in broken markup in the `D4LanguageElements.adoc` operator-precedence table and are not links) (columns `page`, `line`, `text`, `legacy_page`, `target`, `source`); the converted pages already link to the computed `target` paths (`reference/system-library/operators/<Name>.md`, `reference/system-library/types/<Name>.md`, `reference/libraries/<Name>.md`, `reference/frontend-nodes/<Name>.md`; one API link goes to the placeholder `reference/data-access-components.md`), so this item creates those pages, or retargets from the list. Placeholder pages to fill or move: `docs/reference/libraries/README.md`, `docs/reference/frontend-nodes/README.md`, `docs/reference/call-level-interface.md`, `docs/reference/data-access-components.md`.
- `docs-link-check-ci`: the checker is `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` (stdlib Python, exit code 1 on errors, external links never fetched). Drop `--pending` once the legacy reference pages exist.
- `docs-remove-old-sources`: also delete `tools/docs-convert/` (the plan keeps conversion scripts only until the sources are deleted), but keep `tools/docs-link-check/`. Once `legacy-links.tsv` is gone, the CI invocation must not pass `--pending`.
- `docs-contributing`: replaces the unconverted `Documentation/AuthoringDocumentation.adoc` (GitBook editing workflow) with a "writing docs" page, and merges `docs/reference/Error Message Prefix Codes.txt` into the converted `docs/reference/error-codes.md`.
