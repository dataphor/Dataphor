description: The architecture document has grown into one long page. Split it into a short overview that links to one page per subsystem, and add a check that fails when any architecture page grows past a size limit, so the documentation stays hierarchical instead of growing back into one page.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/architecture.md, docs/architecture/, docs/README.md, tools/docs-size-check/, .github/workflows/, AGENTS.md, tickets/rules/10-dataphor.md, docs/modernization-plan.md
difficulty: medium
----
## Owner direction (2026-11-23)

Deconstruct `docs/architecture.md`, and make it a **gated** policy that every architecture document stays under a size threshold: documentation is hierarchical. A page that outgrows the limit is split into a parent that summarizes and links, and children that hold the detail.

## Where things stand

`docs/architecture.md` is 3,778 words in eleven `##` sections, written one paragraph per line (so line counts mislead; count words). By section: The idea in one paragraph 121, Layers and assemblies 433, Sessions, processes, plans 274, **Catalog and catalog store 1,004**, Libraries and class loading 191, Devices 96, Frontend 246, **Hosting, protocol, discovery 962**, On disk 103, What is alive 197, Third-party code 82. The two large sections are the ones that keep growing: tickets add paragraphs to them as they land.

Inbound links that the split must keep working: `AGENTS.md`, `tickets/rules/10-dataphor.md` (tickets are told to set `architecture:` to the plan or this document), `docs/README.md`, `docs/modernization-plan.md#…` links into it (at least `#hosting-protocol-discovery`), and the `architecture:` headers and body text of open tickets (`tickets/plan`, `fix`, `implement`, `review`, `backlog`; three in plan right now are told to add a paragraph to it: `library-ddl-inside-a-transaction`, `bug-view-tags-lose-to-tags-inherited-from-the-source`, and `debt-remote-session-lazy-enlistment` via its server-link docs). Completed tickets are history; leave their links alone.

## What to build

1. **The hierarchy.** `docs/architecture.md` becomes the root: the one-paragraph idea, a map of the layers, and one short paragraph per subsystem with a link to its page. Children go in `docs/architecture/` (for example `catalog-and-store.md`, `hosting-protocol-discovery.md`, `sessions-processes-plans.md`, …), one per current section where the section is big enough to stand alone; small sections may stay in the root or be grouped. A child that is itself over the limit is split again into a folder of its own with the same parent/children shape. Move text; do not rewrite or summarize away facts while moving it. Every page opens with one sentence saying what it covers and links back to its parent.
2. **Keep links working.** Update every inbound link listed above to the new page and anchor. For anchors other documents use (e.g. `architecture.md#hosting-protocol-discovery`), keep a short stub heading in the root that links on, or update the referrer; do not leave a dangling anchor. `python tools/docs-link-check/check_links.py` must report no new errors.
3. **The size check.** A small stdlib-Python script (beside the link checker: `tools/docs-size-check/`, with a README and unit tests like `tools/docs-link-check` has) that counts the words of each Markdown file in the configured set, excluding fenced code blocks, and exits non-zero for any file over the limit. Default limit: **1,500 words per file** (one place to change it; a planner who finds a better number from the split itself may propose it in the handoff). The set it checks is a list in one place: `docs/architecture.md` and everything under `docs/architecture/`. Per-file exceptions are allowed only through an explicit list in the script's config, each with a reason, and the list should be empty when this ticket lands.
4. **The gate.** Run the check in CI. `.github/workflows/build.yml` ignores `docs/**` on purpose and its NOTE says a docs job must get its own workflow, so add one (e.g. `.github/workflows/docs.yml`, triggered on pushes and pull requests that touch `docs/**` or the check itself, read-only token, no `pull_request_target`, matching build.yml's conventions). If the backlog item `docs-link-check-ci` (in `feat-ws8-documentation-remainder`) has not landed, it is fine to put only the size check in the new workflow and leave the link checker to that item; say which in the handoff.
5. **The policy in writing.** Add the rule to the root `AGENTS.md` (docs bullet or *Rules that are easy to break*) and to `tickets/rules/10-dataphor.md`: architecture documentation is hierarchical; a page over the limit is split into parent and children rather than allowed to grow; put new architecture text on the child page that owns the subject, not in the root; run the size check after editing docs. Update `docs/README.md`'s index for the new pages.

## Scope note: the other large documents

`docs/modernization-plan.md` (34,257 words) and `docs/migration-notes.md` (17,004 words) are far over any such limit, and the plan is the main `architecture:` anchor for tickets. They are out of scope here because splitting the plan moves every ticket anchor. Do not add them to the checked set. File one backlog ticket for each, recording its size and which anchors it would move, so the owner can decide whether and how to bring them under the same rule.

## Done when

- Every file in the checked set is under the limit, and the root reads as an overview someone can take in at once.
- No text was lost in the move: every paragraph of the old document is in exactly one page (diff the concatenated pages against the old file, ignoring the new linking sentences and headings, and say so in the handoff).
- The link checker reports no new errors; inbound links from open tickets and docs point at the new pages.
- The size check has tests for: under the limit, over the limit, fenced code excluded, an excepted file, a missing file in the set.
- The docs workflow runs the check, and fails a push that puts a checked file over the limit (show this with a deliberately oversized scratch file in the test, not a pushed commit).
- Rule text is in `AGENTS.md` and `tickets/rules/10-dataphor.md`; the two backlog tickets for the plan and the migration notes exist.

