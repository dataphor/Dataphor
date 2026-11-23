description: The architecture document was one long page; it is now a short overview linking to one page per subsystem. Review that no text was lost, that old links still work, and that the new sentences read well.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/architecture.md, docs/architecture/, docs/README.md, docs/modernization-plan.md, AGENTS.md, tickets/plan/library-ddl-inside-a-transaction.md, tickets/plan/bug-view-tags-lose-to-tags-inherited-from-the-source.md
difficulty: medium
----
## What landed

`docs/architecture.md` was one page of eleven `## ` sections, 3,633 words outside fenced code. It is now an overview of 657 words that keeps all eleven heading lines byte-identical, carries the preamble, the full *The idea in one paragraph* text and the fenced layer diagram, and under every other heading one summary sentence ending in a link to the child page that holds the detail. Ten child pages, flat in `docs/architecture/`, hold the moved text.

Nothing else was done: no behaviour changed, no source file was touched, no test was added or changed. This is the first of the owner's two tickets of 2026-11-23; the second, `docs-architecture-hierarchy`, adds the size check, the CI gate and the written rule, and is on the board ahead of this review (`tickets/implement/2-docs-architecture-hierarchy.md` when this was written).

## Measured results

**Word counts, plain `wc -w` (so the layer diagram counts against the root).** Every page is under the ticket's 1,500-word ceiling.

| Page | Words |
|---|---|
| `docs/architecture.md` | 657 |
| `docs/architecture/assemblies.md` | 310 |
| `docs/architecture/sessions-processes-plans.md` | 299 |
| `docs/architecture/catalog-and-store.md` | 1,025 |
| `docs/architecture/libraries-and-class-loading.md` | 215 |
| `docs/architecture/devices.md` | 116 |
| `docs/architecture/frontend.md` | 269 |
| `docs/architecture/hosting-protocol-discovery.md` | 984 |
| `docs/architecture/on-disk.md` | 123 |
| `docs/architecture/what-is-alive.md` | 216 |
| `docs/architecture/third-party-code.md` | 101 |

**No text lost.** The old document (from `git show HEAD:docs/architecture.md`) has 84 non-blank lines that are not headings. A script compared that multiset against the same lines gathered from the root and all ten children: all 84 occurrences matched, three of them after the expected link rewrite. 78 lines matched a single location outright; the other 6 occurrences are three lines that appear twice each and so cannot be located individually - the two code-fence lines, both now in the root, and the table separator rows `|---|---|---|` (the assembly table and the services table) and `|---|---|` (On disk and What is alive), one of each pair now on each of two pages. The counts agree, which is what the multiset comparison checks.

**New lines: 20, all of them expected.** Ten summary sentences in the root, one per section that moved, and ten opening sentences, one per child page. The same script listed every non-blank body line in the new set that is not in the old document and found exactly those 20. The scratch files it used were deleted.

**Anchors preserved.** All eleven `## ` lines of the old document are present byte-identical in the root, checked by comparing the extracted heading lists rather than by eye; the root has eleven and no more. So `#catalog-and-catalog-store`, `#hosting-protocol-discovery` and the rest still resolve against `docs/architecture.md`.

**Link checker.** `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv`

| | Files | Links | Errors | Pending | External |
|---|---|---|---|---|---|
| Before | 78 | 1,026 | 0 | 203 | 10 |
| After | 88 | 1,056 | 0 | 203 | 10 |

Ten more files and thirty more links, no new errors. Every link in moved text now resolves from one directory deeper, and the checker is what proves it - including the three the ticket found by reading, which were rewritten:

- `[modernization-plan.md](../modernization-plan.md#decisions)` in `hosting-protocol-discovery.md`
- `[Instance Configuration](../users-guide/dataphor-service.md#instance-configuration)` in `on-disk.md`
- `[Hosting, protocol, discovery](hosting-protocol-discovery.md)` in `what-is-alive.md`, which was an in-page anchor and is now a sibling page

**Build.** `dotnet build Dataphor.slnx` - 0 errors, 143 warnings, all pre-existing (CA2022, CS0618 in the test projects), 39 seconds. No file in the diff is compiled, so this only confirms the tree is as it was; no tests were run, because no behaviour moved and the project's testing rule asks for a characterization test only when it does.

**Source hygiene.** Every new page is CRLF, no byte-order mark, trailing newline, one paragraph per line - the same as the document they came from, checked per file rather than assumed. The repository stores these blobs with LF and checks them out as CRLF, so `git show` output is LF; the comparison above is newline-agnostic for that reason.

## Inbound links updated

- `docs/modernization-plan.md`, WS7 *Current state*: the anchor link became `[architecture/hosting-protocol-discovery.md](architecture/hosting-protocol-discovery.md)`. Line 5's plain link to `architecture.md` is unchanged, as the ticket says.
- `docs/modernization-plan.md`, the WS8 *Target tree* block: a line `architecture/` with the gloss "one page per subsystem, linked from architecture.md", aligned to the block's existing gloss column.
- `docs/README.md`, *Project documents*: the Architecture bullet now ends "One page per subsystem, under `architecture/`:" and carries a tight nested list of the ten pages with a few words each. The list is deliberately tight (no blank lines) so the outer list stays tight too.
- `AGENTS.md`, the Layout table's `docs/` row: now reads "Plan, architecture (`architecture.md` is the overview; `architecture/` holds one page per subsystem), migration notes, ...".
- `tickets/plan/library-ddl-inside-a-transaction.md`: all three instructions to write into `docs/architecture.md` now name `docs/architecture/libraries-and-class-loading.md`. The third one lost the phrase "in the section that describes libraries", which the page title now says.
- `tickets/plan/bug-view-tags-lose-to-tags-inherited-from-the-source.md`: now names `docs/architecture/catalog-and-store.md`, with the ticket's instruction that its planner may pick a better page if it finds one.
- Left alone, as the ticket directs: `tools/docs-convert/index-extra.md` (hand-written block for the by-hand converter, already out of step with `docs/README.md`, link still valid, and `docs-remove-old-sources` deletes it - confirmed untouched in the diff); `README.md` line 93 and `AGENTS.md` line 8 (plain links to the root, still correct); `tickets/plan/bug-prepared-create-statement-executes-twice.md`, `tickets/backlog/debt-ws12-hygiene-remainder.md`, `tickets/plan/debt-remote-session-lazy-enlistment.md`.

## What to check in review

The mechanical part is measured above and does not need re-measuring unless you distrust the method; what is worth a reader's judgment is the 20 new sentences, since they are the only text that was written rather than moved.

- **The ten root summaries.** Each must be 15-40 words, end in its link, and state nothing its child page does not. Two are worth particular attention: *Layers and assemblies* has to explain why its page is called `assemblies.md` when the diagram stayed behind, and *Catalog and catalog store* compresses a 1,025-word section covering both the in-memory catalog and the store's transaction rules into one sentence.
- **The ten child openers**, and whether each page title matches what the page holds. `assemblies.md` is the one named for its content rather than for the section it came from.
- **Mannered prose.** `AGENTS.md`'s rule applies to all 20 sentences. They were written to it, but a second reader is the test.
- **Where the three inventory sections ended up.** On disk, What is alive and Third-party code are short (101-216 words) and became child pages for uniformity rather than need, as the ticket decided. If a reviewer thinks the root reads worse for it, that is a judgment to raise rather than a defect - the ticket settled it deliberately.

## Known gaps and things deliberately not done

- **`catalog-and-store.md` (1,025 words) and `hosting-protocol-discovery.md` (984) are the two pages nearest the ceiling, and the ticket's survey says they are the two that grow** as tickets land. They were not split further: neither is over the limit, and splitting would mean inventing section titles and summaries that are not in the source text, which this ticket forbade. The next ticket's size gate is what will force the split. The shape to use then, decided by this ticket: a parent page `X.md` beside a folder `X/` holding its children, the same shape as `architecture.md` beside `architecture/`.
- **No size check is installed yet**, so nothing stops the next ticket that appends a paragraph to `catalog-and-store.md` from pushing it past 1,500 words unnoticed. That is `docs-architecture-hierarchy`'s whole purpose and it has not landed yet; the counts above are this ticket's substitute.
- **No `README.md` in `docs/architecture/`**, by the ticket's decision: the folder's index is `docs/architecture.md`, and a second index would compete with it. A reader who opens the folder in a file browser rather than following a link sees ten pages and no index.
- **No new test**, and none is owed: the change moves prose, not behaviour. The verification is the three checks above.
- **The two repointed plan tickets are now committed to pages chosen on this ticket's reading**, not on the eventual author's. The view-tags one in particular points at `catalog-and-store.md` because that is where schema objects and their metadata are described; its own text now says its planner may choose better.

## Review findings

- Nothing was fixed inline; the ticket's scope was the move itself and no defect was met along the way.
- Tripwire, parked as the first bullet of *Known gaps* above and in this ticket's measurement table rather than in code (there is no code site): the two large child pages are within the limit now and are the two that grow, so the first ticket to append a long paragraph to either can exceed 1,500 words with nothing to catch it until `docs-architecture-hierarchy` lands. No ticket filed - `docs-architecture-hierarchy` already owns the gate, and this is a condition that has not tripped.
