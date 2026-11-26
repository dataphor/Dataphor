description: The two old documentation trees and the publishing config they needed are deleted, and everything that referred to them now points at the converted pages.
architecture: docs/modernization-plan.md#ws8-documentation
files: README.md, AGENTS.md, docs/modernization-plan.md, docs/contributing/writing-docs.md, docs/reference/libraries/README.md, docs/developers-guide/d4-comparison-with-sql.md, .github/workflows/docs.yml, tools/docs-link-check/README.md, tools/docs-link-check/check_links.py, tickets/index-config.json
----
## What landed

The last WS8 clean-up. Four paths are gone from the working tree, 997 files and 110 MB:

- `Documentation/` — the AsciiDoc GitBook sources, converted by `docs-convert-asciidoc`.
- `legacy/Documentation/` — the DocBook 4.3 originals, their build toolchain, jars, a compiled help file and a PDF, converted by the `docs-convert-legacy-reference` chain.
- `book.json` — the GitBook configuration whose root was `./Documentation`.
- `tools/docs-convert/` — the converters, whose only inputs were the two trees.

**The content is still in git history.** This is 110 MB out of the working tree, not out of the repository; the clone size does not change. `hygiene-large-binaries` decided deliberately not to rewrite history.

Everything that pointed at the deleted paths was repointed: the root `README.md`'s nine links into `Documentation/` now name the converted pages under `docs/` (each mapped through the converter's `pages.tsv` before the tool was deleted, not guessed); the CI *Documentation links* step runs `check_links.py docs` with no `--pending` list and its comment says the check has no exemptions; `tools/docs-link-check/README.md` keeps `--pending` documented as a general option but no longer presents a deleted file as its example; `AGENTS.md`'s layout table loses the `Documentation/` row and the `docs-convert/` mention; `tickets/index-config.json` drops the `Documentation/` exclusion (`legacy/` stays); and `docs/modernization-plan.md` marks the WS8 and WS12 rows done. Two live pages the ticket had not listed were repointed too — `docs/contributing/writing-docs.md`, which told a human to run the checker against a deleted pending list, and `docs/reference/libraries/README.md`, which cited two converter tables as where its own facts are recorded.

Two records that lived only in the converter's README were moved into the plan's WS8 design notes before it was deleted, because they say that a published page deliberately differs from what its source said: the hand-repair of the *EDITING* table in `docs/users-guide/tutorials.md` (both legacy sources carried a spurious fourth cell in a three-column table, which shifted every following cell and dropped a row off the end), and the two chapter links added to `docs/developers-guide/README.md`. The rest of that README was either a warning about rerunning a converter whose inputs no longer exist, or already recorded elsewhere — the implement handoff lists it item by item.

No `docs/migration-notes.md` entry: no application, deployment script or custom C# extension reads a file in these trees.

## Review findings

The implement work is in the commit `tess: salvage uncommitted working tree (no ticket in progress)`, **not** in a `ticket(implement): docs-remove-old-sources` commit — a grep for that subject finds nothing. The diff was read in full from that commit before the handoff summary.

**Checked.** That nothing in the build reads either tree — `dotnet build Dataphor.slnx` passing with the trees gone is stronger proof than the handoff's grep over the project files. All nineteen repository-relative links in `README.md`, re-verified against the git index for case-exact existence, because the link checker's root is `docs/` only and a Windows working tree is not case-sensitive; all resolve, and the file has no anchor links. A fresh sweep of every tracked file outside `tickets/` for `tools/docs-convert`, `book.json`, `Documentation/`, and for *GitBook*, *AsciiDoc*, *pandoc*, *DocBook* and `.adoc`. That no Markdown file under `docs/` has acquired a byte-order mark, which `docs/contributing/writing-docs.md` asserts. That `tickets/index-config.json` still parses.

**Fixed in this pass — five sites, all of them documentation that the deletion made false.**

- `docs/modernization-plan.md`, the WS8 bullet on the five device libraries this release does not ship, still said the reason for leaving their links as plain text is "recorded in `tools/docs-convert/reference-links.tsv`" — a present-tense pointer at a file this commit deletes. The implementer fixed the identical sentence in `docs/reference/libraries/README.md` and missed the plan's copy. It now states the reason inline and points at the page that carries it.
- `docs/modernization-plan.md`, the signature-provenance paragraph, said the operator signatures "are extracted into `tools/docs-convert/declarations.tsv`". Now past tense, with one sentence saying the table went with the converter and the reference pages are the only copy of those signatures. The measurements in the paragraph are untouched.
- `docs/modernization-plan.md`, the WS8 **Current state** paragraph, described both now-deleted trees in the present tense — the thing a reader of a plan is most likely to believe. Relabelled **Starting state** with one clause saying the trees are gone and this is the inventory WS8 worked from. Every number in it is left as measured.
- `AGENTS.md`'s `docs/` row still described `reference/` as "D4 grammar, tags, error codes, compiler defines and stubs". The handoff flagged this and declined it as `docs-convert-legacy-reference`'s row to have updated. It is one clause in a table cell this diff already edits, and 365 converted pages make it plainly false, so it is fixed here: it now names the three converted subfolders.
- `docs/developers-guide/d4-comparison-with-sql.md` carried one unconverted AsciiDoc cross-reference in published prose — `\<\<D4ImperativeStatements.adoc#Assignment_Statement, :⇒\>`, where the `>>` of the cross-reference close had been eaten by AsciiDoc's `=>` substitution. The escaping means it is not a Markdown link, so no gate ever saw it. It is now an ordinary relative link to `d4-imperative-statements.md#assignment-statement` with `:=` as its text. It is a `docs-convert-asciidoc` defect rather than this ticket's, but the source it would have been regenerated from is what this commit deletes, so it had to be read and fixed rather than left. A grep for `\<\<` across `docs/` finds no other leftover: the three remaining are deliberately escaped `<<` shift operators, two in operator tables and one as a link label.

**Recorded in the two tickets that depend on the deletion, not filed as new work.** `feat-revive-dropped-reference-documents` and `feat-dac-developers-guide` both tell a reader to recover a file from "the commit before `docs-remove-old-sources`", and no commit message names that slug. Their `git log --diff-filter=D` recipes were run and do resolve to the salvage commit, so the recipes work as written; one sentence is added to each saying which commit it is and to use the recipe rather than searching commit messages.

**Found, nothing owed.** The handoff says `.gitattributes`'s now-unused `binary` patterns "match nothing": four do (`*.cdr`, `*.clk`, `*.emf`, `*.jar` — zero tracked files each), but `*.chm` still matches four files and `*.cub` two, elsewhere in the tree. A `binary` pattern matching nothing is harmless and re-arms if such a file is ever added, so there is no work here — worth knowing only because the handoff names `hygiene-gitattributes-editorconfig` as the owner and that work item has already landed, so no open ticket holds it. Not recorded as a tripwire either: a tripwire names a condition that creates work, and this one creates none.

**Left alone deliberately.** `docs/modernization-plan.md`'s Phase 0 survey table still asserts the DocBook sources exist. Every row of that table is a finding from before WS1 and several are equally stale — the first says nothing in the repository builds with `dotnet build` — so editing one row would make a consistent snapshot inconsistent. `tickets/backlog/debt-ws12-hygiene-remainder.md` lists a file in the deleted tree as one of six inherited-identifier sites; that arm is resolved by this deletion and the ticket already says it "needs nothing here", so it stands as written.

**No ticket filed, and no tripwire.** Nothing found was conditional, and nothing reached the filing bar: the five findings are all one-line documentation corrections at a named site, which is what a review pass fixes rather than queues.

## Tests

**None added, none run, and there was nothing to run** — verified rather than assumed. Neither the implement diff nor this review pass touches a `.cs`, `.d4`, `.d4l` or test-input file; the only Python change is a module docstring. Per the project's staged test rule the targeted set for this diff is empty, and so is review's one-ring-wider set: there is no engine area in the diff to pick neighbouring fixtures or coverage scripts for. The gates below are the whole acceptance surface, and the link checker's own 14 unit tests cover the one Python file whose text changed.

| Check | Result |
|---|---|
| `python tools/docs-link-check/check_links.py docs` (no `--pending`) | 452 files, 3434 links, **0 errors, 0 pending**, 17 external |
| `python tools/docs-size-check/check_size.py` | 13 files, 0 over the limit |
| `python -m unittest discover -s tools/docs-link-check` | 14 tests, OK |
| `dotnet build Dataphor.slnx` | **0 errors**, 127 warnings (unchanged), 24 s |

The link count is one higher than the implement handoff's 3433: the AsciiDoc cross-reference fixed above became a real Markdown link.

## Still true after this ticket

- `legacy/Documentation/Docs2.0/Images/Working/` held nine CorelDRAW files, the only editable originals of the published entity diagrams. Nothing referenced them and no converted page uses one. If a published diagram ever needs changing, `git log --diff-filter=D -- <path>` recovers the source from the salvage commit.
- Only `docs/` is link-gated. Links from `AGENTS.md` and the root `README.md` into `docs/` are not, which is why they were checked by hand here. Widening the checker's root is cheaper now that `tools/docs-convert/`'s fragments are gone — the remaining obstacle is the `tess/` paths — but that is `docs-link-check-ci`'s territory, not a finding against this ticket. The `NOTE:` in `.github/workflows/docs.yml` says so at the site.
