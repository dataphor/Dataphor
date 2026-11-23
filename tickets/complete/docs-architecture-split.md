description: The architecture document was one long page; it is now a short overview linking to one page per subsystem. Every sentence and every link that pointed into it still works.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/architecture.md, docs/architecture/, docs/README.md, docs/modernization-plan.md, AGENTS.md, tools/docs-convert/README.md, tickets/plan/library-ddl-inside-a-transaction.md, tickets/plan/bug-view-tags-lose-to-tags-inherited-from-the-source.md
----
## What landed

`docs/architecture.md` was one page of eleven `## ` sections, 3,633 words outside fenced code. It is now an overview of 657 words that keeps all eleven heading lines byte-identical, carries the preamble, the full *The idea in one paragraph* text and the fenced layer diagram, and under every other heading one summary sentence ending in a link to the child page that holds the detail. Ten child pages, flat in `docs/architecture/`, hold the moved text. Six inbound references were repointed: the plan's WS7 anchor link and its WS8 target tree, `docs/README.md`'s Architecture bullet (now with a nested list of the ten pages), `AGENTS.md`'s Layout row, and the two open plan tickets that instruct their implementer to write into the architecture document.

No behaviour changed, no source file was touched, no test was added or changed. This was the first of the owner's two tickets of 2026-11-23; the second, `docs-architecture-hierarchy`, adds the size check, the continuous-integration gate and the written rule, and was in `implement/` when this review finished.

Implemented in `ticket(implement): docs-architecture-split`; the review pass fixed three sentences in the root page and added one note to the converter's README (below).

## Measured results

Word counts, plain `wc -w` (so the layer diagram counts against the root). Every page is well under the 1,500-word ceiling the next ticket installs.

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

**No text lost**, re-verified independently in review rather than taken from the handoff. The old document (`git show HEAD~1:docs/architecture.md`) has 84 non-blank lines that are not headings; the root plus all ten children have 104, which is 84 plus the 20 sentences this ticket wrote. Comparing the two as multisets leaves exactly three old lines unaccounted for, and a character-level diff of each against its new counterpart shows the only change is the link: `../` inserted in two of them, `#hosting-protocol-discovery` becoming `hosting-protocol-discovery.md` in the third. Nothing else in the moved text differs by a character.

**Anchors preserved.** The eleven `## ` lines of the old document are present byte-identical in the root, and the root has eleven and no more, checked by diffing the two extracted heading lists. So `#catalog-and-catalog-store`, `#hosting-protocol-discovery` and the rest still resolve against `docs/architecture.md` for anyone holding an old link or bookmark.

**No stale anchor links anywhere in the tree.** A grep for `architecture.md#` over the whole repository finds only `storage-integration-architecture.md#` references in the user documentation, which are a different document, plus matches inside `tickets/.logs/`. The one live inbound anchor link (the plan's WS7 *Current state*) was repointed to the child page.

**Link checker**, `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv`:

| | Files | Links | Errors | Pending | External |
|---|---|---|---|---|---|
| Before the split | 78 | 1,026 | 0 | 203 | 10 |
| After, re-run in review | 88 | 1,056 | 0 | 203 | 10 |

Ten more files and thirty more links, no new errors. The checker resolves heading anchors with GitHub's own slug algorithm, so it is what proves both that every link in moved text still resolves from one directory deeper and that no fragment went stale. Its own unit tests also pass (14 cases).

**Source hygiene.** Every page is CRLF on every line, carries no byte-order mark and ends with a newline, checked per file. The repository stores these blobs with LF and checks them out as CRLF, which is why the text comparisons above are newline-agnostic.

**Build.** `dotnet build Dataphor.slnx` - 0 errors, 143 warnings, all pre-existing (CA2022, CS0618 in the test projects), 22 seconds. No file in the diff is compiled, so this only confirms the tree is as it was.

## Review findings

**What was checked.** The implement diff read before the handoff summary; the text-loss comparison, heading identity, inbound-reference sweep, word counts and per-file line endings all re-measured rather than accepted; the link checker and its unit tests run; all 20 newly written sentences read against `AGENTS.md`'s *Mannered prose* rule and against the child page each one summarizes; the ten child page titles read against what each page holds; moved text grepped for positional references (`above`, `below`, `earlier`, `this document`, `the section`) that a move would have broken - two hits, both still correct; the build run.

**Fixed inline.**

- `docs/architecture.md`, the *Catalog and catalog store* summary stated something false. As written - "The catalog is the set of all schema objects, loaded lazily from the catalog store ... and the transactions that write it" - the final clause coordinated with the predicate, so the sentence said the catalog *is* the transactions that write it. Recast so that the clause naming what the child page holds is its own: "... which also holds the users, rights and settings that exist nowhere else; the store and the transactions that write it are in [catalog-and-store.md]".
- `docs/architecture.md`, the *Layers and assemblies* summary had a compound subject with a singular verb ("the table ..., and the note ..., is in"). Rewritten to name the content rather than the document's furniture - "what each assembly holds, which project builds it, and that the engine runs without the server layer are all in [assemblies.md]" - which also matches the child page's own opening sentence and still tells the reader why the page is called `assemblies.md`.
- `docs/architecture.md`, the *Devices* summary had the same agreement error: "which devices ship, and what they build on, is in" became "are in".
- `tools/docs-convert/README.md`: the `index-extra.md` row now says that the file is behind the `## Project documents` section it generates and that a converter run overwrites that section with the shorter one, so the diff must be read before committing. The divergence is older than this ticket (three bullets against five) and the split deliberately left `index-extra.md` alone, but the split added a ten-item nested list to the section at risk and nothing in the tree recorded the hazard. The note went in the converter's README rather than in `index-extra.md` itself because that file is Markdown with no comment syntax - a note inside it would be appended into `docs/README.md` by the next run.

All three summaries stayed inside the ticket's 15-40 word window after the edits (29, 38 and 32 words); the ten now run 19-38.

**Tickets filed: none.** Nothing found reaches the filing bar - no finding serves a current-release anchor that is not already served, and the one class-level invariant in play here (an architecture page growing back past the limit) is already owned by `docs-architecture-hierarchy`, which was in `implement/` when this review ran.

**Tripwires: none newly parked.** The handoff parked one - the two largest child pages are the two that grow, and until the size gate lands nothing catches a page that crosses 1,500 words. It was verified accurate (1,025 and 984 words measured) and it already has a home: `docs-architecture-hierarchy` requires that exact note, verbatim, in its tool's README. Recording it a second time would create two copies to keep in step.

**Considered and declined.**

- *No cross-links between sibling child pages.* Only `what-is-alive.md` links to a sibling, because that link was an in-page anchor before the move. A reader on `catalog-and-store.md` who wants the hosting page goes up to the root, which lists all ten with a one-line summary each. Adding sibling links means editing moved paragraphs, which the ticket forbade so that the text-loss comparison could be exact, and the cost to the reader is one click. Not changed, not filed.
- *The hosting summary joins its link with a dash where the other nine use a colon or a verb.* It already carries a colon mid-sentence, so a second one would read worse. Left as written.
- *`docs/README.md` and `docs/architecture.md` now both list the ten pages.* The duplication was the ticket's explicit requirement: the documentation index is the entry point for a reader who never opens `architecture.md`. Not a defect to collapse.
- *The root preamble frames the whole document as "the last 3.1-era commit (2019)", and a child page opened directly carries no such framing.* The framing was already imprecise before the split - `catalog-and-store.md` and `hosting-protocol-discovery.md` describe post-port behaviour, .NET 10 and all - so it is a pre-existing question about what the architecture document claims to describe, not something the split broke, and rewording ten openers would not settle it.

**Tests: none added, none cut, none run beyond the documentation checks.** The diff is Markdown only: no compiled file, no test input and no coverage script changed, and nothing under `tests/` or `tools/layout/` reads `docs/` (checked by grep), so no engine or client test can observe this change and the project's "one ring wider" rule has no ring to widen. The automated checks that do cover it were run and are recorded above: the link checker over all of `docs/` and the checker's own 14 unit tests. The project's testing rule asks for a characterization test when a subsystem's behaviour moves; none moved.

## Known gaps carried forward

- **No size check is installed yet**, so nothing stops the next ticket that appends a paragraph to `catalog-and-store.md` from pushing it past 1,500 words unnoticed. That is `docs-architecture-hierarchy`'s whole purpose; the counts in this ticket are the stand-in until it lands.
- **The two large pages were not split further.** Neither is over the limit, and splitting them would mean inventing section titles and summaries that are not in the source text, which the ticket forbade. The shape to use when one does trip the check, decided by this ticket: a parent page `X.md` beside a folder `X/` holding its children, the same shape as `architecture.md` beside `architecture/`.
- **No `README.md` in `docs/architecture/`**, by the ticket's decision: the folder's index is `docs/architecture.md`, and a second index would compete with it. A reader who opens the folder in a file browser rather than following a link sees ten pages and no index.
- **The two repointed plan tickets now name pages chosen on this ticket's reading**, not on their eventual author's. The view-tags one points at `catalog-and-store.md` because that is where schema objects and their metadata are described, and its own text now says its planner may choose a better page.
