description: Build a harness that checks an external application's libraries and forms against the new release, then run it against the real application when the owner starts that work.
architecture: docs/modernization-plan.md#ws13-customer-application-compatibility
tradeoffs: The owner has deferred application work until Dataphor itself is modernized (D16); this should not be promoted without the owner saying so.
----
## What this is

An umbrella for WS13. **Do not promote without the owner's go-ahead** (D16). When promoted to `plan/`, the planning agent replaces it with tickets for the items below and deletes this file.

## Work items

- `compat-corpus-harness` — layer L7 harness that takes a corpus path from an environment variable: every library registers on a fresh instance through the application's own rebuild scripts; every stored form loads; every table and view derives each standard page type; a report format. Developed against the repository samples, building on `test-samples-corpus` (`feat-ws3-test-layers`).
- `compat-application-run` — stays in `backlog/` until the owner starts it: run against the real corpus, one ticket per finding. Needs `compat-corpus-harness` and `docs-migration-guide-custom-code`.

**Privacy.** The repository is public. The corpus is never copied into it, and no customer name, path, library name, schema or data goes into any tracked file, ticket or commit message; findings are described generically.
