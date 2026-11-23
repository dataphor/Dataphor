description: The modernization plan is one very long document. Decide whether to split it into a short overview plus a page per part of the work, knowing that every ticket points at a section of it and those pointers would have to move.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/modernization-plan.md, tickets/, AGENTS.md, docs/README.md, tools/docs-size-check/
tradeoffs: Every ticket on the board names a section of this document as its anchor, and the ticket runner refuses a ticket whose anchor does not resolve, so a split touches the whole board at once and buys nothing for anyone who is only reading it; leaving it as one long file is defensible for as long as the modernization is in flight.
----
## Why this exists

`docs-architecture-hierarchy` made the architecture documentation hierarchical: `docs/architecture.md` is a short overview, the detail lives in one page per subsystem under `docs/architecture/`, and `tools/docs-size-check` fails a page over 1,500 words. The modernization plan is far over any such limit and was deliberately left out of that gate, because splitting it moves the anchor that nearly every ticket points at. This ticket records the size, the cost, and what the owner has to decide.

## What it would cost

Measured 2026-11-23 — words outside fenced code blocks, the same rule the size check uses (`python tools/docs-size-check/check_size.py` counts this way; `wc -w docs/modernization-plan.md` gives 37,160, which counts code blocks too):

- the whole document: **36,794 words**, 26 `##` sections and 5 `###` subsections;
- the largest sections: Known defects 10,641, WS5 Client server protocol and hosting 5,723, WS3 Test suite 3,802, WS2 Engine and server port 3,721, WS4 Catalog store 1,553, WS1 Build and workspace 1,404, Repository layout 984, Decisions 921;
- everything else is under 800 words, and eleven sections are under 400.

What points into it, outside `tickets/complete/` and the run logs: **39 links to 14 distinct anchors**. Thirty of those are the `architecture:` header of an open ticket, concentrated on the workstream sections — `#ws3-test-suite` (12), `#ws2-engine-and-server-port` (6), `#ws7-devices-and-libraries` (3), `#ws8-documentation` (2), and one each for WS6 and WS9 through WS14. The rest are prose links from `AGENTS.md`, `docs/architecture.md`, `docs/README.md`, the root `README.md` and other tickets, including `#repository-layout` (3, the old → new path table that tickets are told to use) and `#decisions`.

The project's ticket rules require an anchor on every ticket and the runner will not work a ticket without one, so a split has to keep each workstream anchor resolving or rewrite every ticket that names it, on a board that other agents are committing to at the same time.

## What the owner has to decide

Whether the plan is split at all while the modernization is in flight, and if so along which seam. The shapes worth weighing:

- **Leave it.** It is read front to back by newcomers and section by section by agents; its size costs reading time, not correctness.
- **Split off only the two parts that are not plan prose** — Known defects (10,641) and the appendices — leaving the workstream sections, and therefore every ticket anchor, where they are. This removes a third of the document for the smallest disturbance.
- **One page per workstream**, with the root keeping the goals, decisions, sequencing and the work-item tables. Every `#wsN-…` anchor moves, so every open ticket's `architecture:` field is rewritten in the same commit, and the ticket rules' example anchor changes.
- **Split and keep the anchors**, by leaving a short stub section per workstream in the root that links on. The anchors resolve and no ticket changes, at the price of a root that is a list of stubs.

Whichever shape is chosen, the question of whether the plan then joins the size check's `CHECKED` set is part of the decision: a 1,500-word limit on plan pages would bite Known defects and WS5 immediately.

## Not in scope

The architecture hierarchy itself (done), and `docs/migration-notes.md`, which has its own ticket (`debt-split-the-migration-notes`).
