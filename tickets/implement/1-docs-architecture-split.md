description: The architecture document is one long page. Split it into a short overview that links to one page per subsystem, keeping every sentence and every link that points into it working.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/architecture.md, docs/architecture/, docs/README.md, docs/modernization-plan.md, AGENTS.md, tickets/plan/library-ddl-inside-a-transaction.md, tickets/plan/bug-view-tags-lose-to-tags-inherited-from-the-source.md, tools/docs-link-check/check_links.py
difficulty: medium
----
## What this is

First of two tickets for the owner's direction of 2026-11-23: architecture documentation is hierarchical — a page that outgrows a size limit is split into a parent that summarizes and links, and children that hold the detail. This ticket performs the split. The second, `docs-architecture-hierarchy`, adds the size check, the CI gate and the written rule.

`docs/architecture.md` today is one page of eleven `##` sections. Measured (words outside fenced code blocks, so the ASCII layer diagram is not counted; the figures in brackets are plain `wc -w`, which does count it):

| Section | Words |
|---|---|
| (preamble) | 58 |
| The idea in one paragraph | 116 |
| Layers and assemblies | 285 (433 with the diagram) |
| Sessions, processes, plans | 271 |
| Catalog and catalog store | 1,000 |
| Libraries and class loading | 187 |
| Devices | 95 |
| Frontend: from schema to screen | 241 |
| Hosting, protocol, discovery | 959 |
| On disk | 101 |
| What is alive | 194 |
| Third-party code in the tree | 77 |
| whole file | 3,633 (3,778) |

The two large sections are the two that keep growing: landing tickets add paragraphs to them.

## The shape to build

`docs/architecture.md` stays the root and the entry point. It keeps **all eleven `## ` heading lines exactly as they are written today**, so every anchor that exists now still resolves (`#catalog-and-catalog-store`, `#hosting-protocol-discovery`, …). Under each heading the root carries a short summary and a link to the child page that holds the detail. Two sections keep their text in the root instead: *The idea in one paragraph* (it is the root's own opening) and the fenced layer diagram under *Layers and assemblies* (it is the map the owner asked the root to carry, and fenced text costs nothing against the limit).

Ten child pages, one per section, flat in `docs/architecture/`:

| Child page | Holds | Words after the move |
|---|---|---|
| `docs/architecture/assemblies.md` | the assembly/project/role table and the sentence that the engine can run without the server layer | ~310 |
| `docs/architecture/sessions-processes-plans.md` | Sessions, processes, plans | ~300 |
| `docs/architecture/catalog-and-store.md` | Catalog and catalog store | ~1,025 |
| `docs/architecture/libraries-and-class-loading.md` | Libraries and class loading | ~210 |
| `docs/architecture/devices.md` | Devices | ~120 |
| `docs/architecture/frontend.md` | Frontend: from schema to screen | ~265 |
| `docs/architecture/hosting-protocol-discovery.md` | Hosting, protocol, discovery | ~985 |
| `docs/architecture/on-disk.md` | On disk | ~125 |
| `docs/architecture/what-is-alive.md` | What is alive | ~220 |
| `docs/architecture/third-party-code.md` | Third-party code in the tree | ~100 |

Every page must come out under **1,500 words by plain `wc -w`**, which is stricter than the gate the next ticket installs (that one excludes fenced code). The root lands around 700 words; the largest child, `catalog-and-store.md`, around 1,025.

### Decisions already taken — do not revisit

- **Flat, one level.** `catalog-and-store.md` (~1,025) and `hosting-protocol-discovery.md` (~985) are the two pages nearest the limit, and they are the two that grow. They are **not** split further in this pass: neither is over the limit, and splitting them would mean inventing section titles and summaries that are not in the source text, which this ticket forbids. The next ticket's gate is what will force the split when one of them actually outgrows the limit; the alternative considered and rejected was pre-splitting each into a parent page beside a folder of the same name.
- **When a page does have to be split later, the shape is: a parent page `X.md` sitting beside a folder `X/` holding its children** — the same shape as `architecture.md` beside `architecture/`. No `README.md` in `docs/architecture/`: the folder's index is `docs/architecture.md`, and a second index would compete with it.
- **The three inventory sections (On disk, What is alive, Third-party code) become child pages like the rest,** rather than keeping their text in the root. Uniformity: the rule "new architecture text goes on the child page that owns the subject" then has a page for every subject, and the root stays an overview.
- **Old anchors are preserved by keeping the root's heading lines, and inbound links are repointed at the child pages anyway.** No stub headings whose only purpose is to hold an anchor.
- `docs/architecture/assemblies.md` is named for what it holds (the assembly table) rather than `layers-and-assemblies.md`, because the layers diagram stays in the root. The root's sentence under `## Layers and assemblies` must say so, so the reader is not surprised by the page title.

### Page shape

Each child page opens with a `# ` title, then one line saying what it covers and linking back to the parent, then the moved text:

```
# Catalog and the catalog store

What the catalog is, how it is loaded, and the store it persists in. Part of the [Dataphor architecture](../architecture.md).

<the moved paragraphs, unchanged>
```

No other new headings inside a child page: the moved sections are flat prose, tables and bullet lists, and giving them sub-headings is a rewrite.

### Moving text, not rewriting it

Move the paragraphs verbatim. The only text this ticket writes is: the root's one-sentence summaries (one per section, 15–40 words, ending in the link), the children's opening sentences, and the `docs/README.md` index entries. A summary may condense the section's own opening sentence; it must not state a fact the child page does not. `AGENTS.md`'s *Mannered prose* section applies to every sentence written here.

`docs/architecture.md` is one paragraph per line (no hard wrapping) — keep it that way, in the root and in every child.

### Links that have to change

Inside the moved text (line numbers are those of the file today):

| Line | Today | On the child page |
|---|---|---|
| 105 | `[modernization-plan.md](modernization-plan.md#decisions)` | `../modernization-plan.md#decisions` |
| 119 | `[Instance Configuration](users-guide/dataphor-service.md#instance-configuration)` | `../users-guide/dataphor-service.md#instance-configuration` |
| 131 | `[Hosting, protocol, discovery](#hosting-protocol-discovery)` | `hosting-protocol-discovery.md` (a sibling page, not an anchor) |

Lines 3's two links to `modernization-plan.md` stay in the root unchanged.

Inbound, outside the document:

- `docs/modernization-plan.md` line 418: `[architecture.md](architecture.md#hosting-protocol-discovery)` → `[architecture/hosting-protocol-discovery.md](architecture/hosting-protocol-discovery.md)`. Line 5's plain link to `architecture.md` stays.
- `docs/modernization-plan.md`, the WS8 *Target tree* block: the line `  architecture.md, modernization-plan.md` gains `architecture/` with a short gloss (one page per subsystem).
- `docs/README.md`, *Project documents*: the Architecture bullet gains a nested list of the ten child pages, each with a few words saying what it covers.
- `AGENTS.md`, the Layout table's `docs/` row: name `architecture/` beside `architecture.md`.
- `README.md` (repository root) line 93 and `AGENTS.md` line 8 point at `docs/architecture.md` with no anchor: both still correct, leave them.
- Two open tickets instruct their implementer to write text into `docs/architecture.md`; repoint them so that text lands on the page that owns the subject:
  - `tickets/plan/library-ddl-inside-a-transaction.md` (three mentions, lines 7, 16, 35) → `docs/architecture/libraries-and-class-loading.md`.
  - `tickets/plan/bug-view-tags-lose-to-tags-inherited-from-the-source.md` (line 11) → `docs/architecture/catalog-and-store.md`, where schema objects and their metadata are described. Say in that edit that its planner may choose a better page if it finds one.
  - Leave alone: `tickets/plan/bug-prepared-create-statement-executes-twice.md` and `tickets/backlog/debt-ws12-hygiene-remainder.md` (both name the document generically, and the root still exists), and `tickets/plan/debt-remote-session-lazy-enlistment.md` (it names no path).

`tools/docs-convert/index-extra.md` also carries an Architecture bullet. It is the hand-written block the by-hand converter appends to `docs/README.md`, it is already out of step with `docs/README.md` (three bullets against five), its link stays valid, and `docs-remove-old-sources` deletes it. **Do not touch it**; say so in the handoff.

## Edge cases & interactions

- **No text lost.** The file is one paragraph per line, so this is checkable as a set comparison rather than by eye. In the scratchpad (never in the repository): `git show HEAD:docs/architecture.md` to one file, the root plus all ten children concatenated to another, then compare the sets of non-blank lines that are not `#` headings. Every old line must appear in exactly one new file, verbatim except the three link rewrites above. Report the count of old lines, the count accounted for, and the list of new lines (summaries, opening sentences) in the handoff. Delete the scratch files.
- **Anchor preservation.** After the edit, every `## ` line of the old file must still exist, byte-identical, in the root. Check with a grep of the eleven heading lines, not by eye.
- **Relative-link depth.** Every link in moved text resolves from `docs/architecture/`, one level deeper than before. The link checker is what proves it; the three rewrites above are the ones found by reading, not the ones to trust.
- **Link checker clean.** Record the before and after of `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` (files scanned, links checked, errors, pending, external) in the handoff. Errors must not increase; expect the file and link counts to rise.
- **Case and naming.** Lower-case, hyphenated file names, as the rest of `docs/` uses. The checker compares each path component against the real directory listing, so a wrong-case link is an error, not a Windows-only silent pass.
- **`docs/architecture/devices.md` is not `docs/developers-guide/devices.md`.** Different pages in different folders; do not disturb the existing links to the developers-guide one.
- **Source hygiene.** `docs/architecture.md` is CRLF with no byte-order mark and ends with a newline; give every new page the same, and do not re-encode the files you edit.
- **A concurrent board.** The ticket runner may be committing other tickets while this one runs. Touch only the files listed above; never revert or clean anything else.
- **No automated test.** This is a documentation move: the verification is the text-loss set comparison, the heading grep and the link checker. The project's testing rule asks for a characterization test when behaviour moves; no behaviour moves here. The size check that gates these pages is the next ticket's work, so this ticket records its own word counts instead.

## Done when

- `docs/architecture.md` is an overview: the preamble, the idea, the layer diagram, and one summary paragraph plus link per subsystem, under eleven unchanged `## ` headings.
- The ten child pages exist with the content above, each opening with its one-line "what this covers" and a link back to `../architecture.md`.
- Every page is under 1,500 words by `wc -w`; the handoff lists the measured count for each.
- The text-loss comparison accounts for every line of the old document, and the handoff says so with the numbers.
- The link checker reports no new errors, with before/after counts in the handoff.
- `docs/README.md`, the plan's WS8 target tree, the plan's line-418 link, `AGENTS.md`'s Layout row and the two open tickets are updated as listed.

## TODO

- Read `docs/architecture.md` end to end before moving anything; keep a copy of HEAD's version in the scratchpad for the comparison.
- Create `docs/architecture/` and the ten child pages, moving each section's text verbatim, with the opening sentence and the parent link.
- Rewrite the three relative links in moved text.
- Rewrite `docs/architecture.md` as the overview: preamble, `## The idea in one paragraph` (full text), `## Layers and assemblies` (diagram, one sentence, link to `assemblies.md`), then one summary-plus-link section per remaining child, headings unchanged.
- Run the heading grep and the text-loss set comparison; fix anything unaccounted for.
- Update `docs/README.md`'s Architecture bullet with the nested list of child pages.
- Update `docs/modernization-plan.md`: the line-418 link and the WS8 target tree.
- Update `AGENTS.md`'s Layout table `docs/` row.
- Repoint the two open tickets (`library-ddl-inside-a-transaction`, `bug-view-tags-lose-to-tags-inherited-from-the-source`).
- Run the link checker; record before/after counts.
- Measure every page with `wc -w`; record the counts for the handoff.
- Delete the scratchpad files you made.
