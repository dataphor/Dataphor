description: The list of changes that existing applications will notice has grown very long. Decide whether to split it into a page per audience, knowing that tickets are told to append to it as they land.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/migration-notes.md, tickets/rules/10-dataphor.md, AGENTS.md, docs/README.md, tools/docs-size-check/
tradeoffs: Nothing links to its sections by anchor, so a split is cheap, but it is read as one list by someone moving an application forward and it is about to be rewritten anyway — the plan's `docs-migration-guide-custom-code` work item turns it into a guide — so splitting it now may be work thrown away.
----
## Why this exists

`docs-architecture-hierarchy` made the architecture documentation hierarchical and gated it at 1,500 words a page with `tools/docs-size-check`. `docs/migration-notes.md` is far over that limit and was deliberately left out of the gate: it is an append-only record rather than a hierarchy, and the rule that produces it — every change an application, a deployment script or a custom extension could notice adds an entry in the same commit (`tickets/rules/10-dataphor.md`, `AGENTS.md`) — makes it grow by design.

## What it would cost

Measured 2026-11-23 — words outside fenced code blocks, the same rule the size check uses (`wc -w` gives the same 18,573 here):

- the whole document: **18,573 words** in five `##` sections;
- Platform and deployment 2,430; D4, catalog and libraries 8,343; Custom C# extensions 7,110; Forms and the Windows client 279; Building from source 240.

What points into it: 16 files reference the file, and **no reference uses an anchor** — so unlike the modernization plan, a split breaks nothing that exists today. What it does change is where a landing ticket appends: the rule would have to name the page per audience rather than the file, and an agent that guesses wrong puts an entry on the wrong page.

## What the owner has to decide

Whether to split it, and whether appends stay as easy as they are now. The shapes worth weighing:

- **Leave it.** It is a record someone reads once, end to end, when moving an application forward; its size is the point.
- **A page per audience** — application and D4 author, deployment and operations, custom C# extension author, building from source — with the root keeping the preamble and the index. The two large sections (D4 and catalog 8,343; custom extensions 7,110) are still over any 1,500-word limit on their own and would need splitting again, by subsystem or by release.
- **Wait for `docs-migration-guide-custom-code`** (a WS8 work item, needs `ui-windows-client-runs`), which turns these notes into a guide for moving an application and recompiling extensions. If that guide is where readers will go, splitting the raw notes first is wasted.

Whichever shape is chosen, decide at the same time whether the result joins the size check's `CHECKED` set, and what the append rule then says.

## Not in scope

The architecture hierarchy itself (done), and `docs/modernization-plan.md`, which has its own ticket (`debt-split-the-modernization-plan`).
