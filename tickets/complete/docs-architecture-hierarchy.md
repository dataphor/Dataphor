description: A check now counts the words of each architecture documentation page and fails when one grows past 1,500, runs in continuous integration, and the rule is written down, so the documentation stays hierarchical instead of growing back into one long page.
architecture: docs/modernization-plan.md#ws8-documentation
files: tools/docs-size-check/check_size.py, tools/docs-size-check/test_check_size.py, tools/docs-size-check/README.md, .github/workflows/docs.yml, .github/workflows/build.yml, AGENTS.md, tickets/rules/10-dataphor.md, docs/modernization-plan.md, tools/docs-link-check/README.md, tickets/backlog/feat-ws8-documentation-remainder.md
----
## What landed

The second of the owner's two tickets of 2026-11-23. `docs-architecture-split` had already made the architecture document a short root page plus one page per subsystem; this ticket makes that shape hold, by measuring it and failing when it slips.

**The check.** `tools/docs-size-check/` holds one Python file (`check_size.py`, standard library only, no network), its tests and a README, beside `tools/docs-link-check/` and following its conventions. Configuration sits at the top of the script, each value in one place: `LIMIT = 1500`, `CHECKED` (`docs/architecture.md` and `docs/architecture/**/*.md` as repository-relative globs) and `EXCEPTIONS`, which is empty and meant to stay empty. A word is a whitespace-separated token outside fenced code blocks and outside HTML comments, counted as `wc -w` counts. Findings are one line each — `path: N words (limit 1500)`, `path: N words (exception: reason)`, `path: missing`, `checked set matched no files` — then a summary; exit 0, 1, or 2 for bad arguments, the link checker's convention. `--root DIR` resolves the set elsewhere (this is how the tests exercise the real configuration without writing into `docs/`) and `--limit N` is a what-if. No arguments is the gate, and the root comes from the script's own path, so it runs from any working directory and needs no `.git`.

**The gate.** `.github/workflows/docs.yml` is new, and deliberately separate from `build.yml`, which ignores `docs/**` on purpose. It runs the check and the checker's tests on push and pull request to `master` and `modernization` when `docs/**`, `tools/docs-size-check/**` or the workflow file changes, plus on demand; one `ubuntu-latest` job with a read-only token, a pinned `actions/checkout`, and a ten-minute timeout.

**The rule in writing.** A bullet in `AGENTS.md` *Rules that are easy to break*, a rule beside *Migration notes* in `tickets/rules/10-dataphor.md`, a work-item row and a design-notes clause in the plan's WS8 section, and the tool's own README, which carries the word rule, what to do when the check fails (split the page; do not raise the limit, do not add an exception) and the table of documents deliberately outside the set. `build.yml`'s note, `tools/docs-link-check/README.md` and the `docs-link-check-ci` item of `tickets/backlog/feat-ws8-documentation-remainder.md` now point at the docs workflow, so that item adds a step to it rather than creating one.

Implemented in `ticket(implement): docs-architecture-hierarchy`; the review pass closed one hole in the word rule and corrected three stale or wrong statements (below).

## Measured results

The eleven pages of the set, prose outside fenced code, measured by the tool itself:

| Page | Words | Share of 1,500 |
|---|---|---|
| `docs/architecture.md` | 514 | 34 % |
| `docs/architecture/assemblies.md` | 310 | 21 % |
| `docs/architecture/catalog-and-store.md` | 1,025 | 68 % |
| `docs/architecture/devices.md` | 116 | 8 % |
| `docs/architecture/frontend.md` | 269 | 18 % |
| `docs/architecture/hosting-protocol-discovery.md` | 984 | 66 % |
| `docs/architecture/libraries-and-class-loading.md` | 215 | 14 % |
| `docs/architecture/on-disk.md` | 123 | 8 % |
| `docs/architecture/sessions-processes-plans.md` | 299 | 20 % |
| `docs/architecture/third-party-code.md` | 101 | 7 % |
| `docs/architecture/what-is-alive.md` | 216 | 14 % |

No page over the limit, exception list empty. The review's change to the word rule moved no count in the repository: nothing under `docs/` contains an HTML comment.

**The continuous-integration gate has run.** The implement commit was pushed and the **Docs** workflow succeeded on it in 18 seconds (run 45274682154), with both steps executing: `11 files checked, 0 over the limit, 0 excepted` and `Ran 6 tests ... OK`. That retires the implement handoff's largest stated gap, which was that `docs.yml` had never run and could not be machine-parsed locally (no YAML parser is installed on this machine, and none was installed for this review either).

## Review findings

Read the implement diff first, then the handoff. The tool was also exercised directly against adversarial inputs rather than only through its tests: an invalid-UTF-8 page, a page nested one folder deeper, a page exactly at the limit, a page with a byte-order mark and CRLF, a directory named `*.md`, a `--root` pointing at a file, a negative `--limit`, and an unknown flag. All behaved as the README documents. The gate was also made to fail end to end on a real oversized page written under `docs/architecture/` and then deleted (exit 1, naming the page, its count and the limit); the working tree was clean afterwards.

**Fixed in this pass.**

- *A line of backticks inside an HTML comment opened a code block, so everything after it dropped out of the count.* The one failure mode the implementation set out to prevent is an undercount, because an undercount lets an oversized page pass, and the README asserted that every divergence from the link checker's rule could only over-count. This one under-counts. A 1,501-word page whose first four lines were `<!--`, a line of three backticks, some text, `-->` passed the gate with exit 0. The link checker blanks HTML comment text *before* testing a line for a fence; the size check did not. Fixed by copying the link checker's `_strip_comments` and calling it in the same order, so the two tools now agree on comments as well as fences; the copy NOTE at the rule covers both functions. No count in the repository changes, because no file under `docs/` has an HTML comment — this was a latent hole, not a live miscount. Pinned by one new test, `test_a_fence_inside_an_html_comment_is_not_a_fence`, which fails against the pre-fix code and is the only one of the seven that does (checked by running the suite against that mutant in memory).
- *The README claimed the limit was written down in two other places; it is in three.* `AGENTS.md` names the number, the `LIMIT` symbol and its file; `tickets/rules/10-dataphor.md` and WS8 of the plan name the number. The README and the comment above `LIMIT` now say all three and name the grep handle (`grep -rn docs-size-check`) that finds every one of them, rather than claiming a symbol pointer that two of the three do not carry.
- *The README's word count for `docs/modernization-plan.md` was stale on arrival* — 36,794, measured before the same commit's own edits to that file added about a hundred words. Corrected to 36,895, verified against the file at the commit's parent and at the commit.

**Parked as a tripwire, not a ticket.**

- `docs.yml` uses an inclusion `paths:` filter, so a pull request touching none of those paths gets no Docs run at all. If branch protection is ever set to require the check, it would wait for a run that never happens. `build.yml` already carries that note for its own exclusion filter; `docs.yml` now carries the matching one at its filter. Nothing to do until someone turns on branch protection.
- The remaining divergence from the link checker is unchanged and still safe: a fence indented four spaces or more inside a list item counts as prose here, where the link checker treats it as a fence. That direction can only fail a page early. No page in the set has one.

**Checked and found correct, no change.**

- The exit-code contract, the finding texts and the paths printed match the README and the link checker's conventions; `check()` returns a report so the tests need not parse output.
- `docs.yml` against `build.yml`: same pinned `actions/checkout` commit, same `permissions: contents: read` with the public-repository comment, no `pull_request_target`, same concurrency group and `cancel-in-progress`, same branches, `workflow_dispatch` present. The `paths:` filter covers the tool as well as `docs/`, so a change to the checker runs its own tests. `python3` in the workflow against `python` in the prose is deliberate and commented.
- Source hygiene: both Python files are CRLF without a byte-order mark, matching `check_links.py` and `coverage_report.py`; `docs.yml` is LF, as `.gitattributes` pins for `*.yml`. `check_size.py` is 228 lines. The comments say why rather than narrating statements. `AGENTS.md`'s new bullet uses the spaced hyphen that file already uses in thirty other places, so it was left alone; the tool README uses em dashes throughout and the review's additions there match.
- `AGENTS.md`, `tickets/rules/10-dataphor.md`, the plan's WS8 section and the tool README agree with each other and with the code on the rule, the limit, the commands and the instruction to split rather than except.
- The two tickets the README names, `debt-split-the-modernization-plan` and `debt-split-the-migration-notes`, are named by slug only; both were moved from `backlog/` to `blocked/` by a concurrent board move during the implement stage and are untouched here.

**Considered and declined.**

- *A pointer to the size rule inside `docs/architecture.md`.* A human editing a child page who never reads `AGENTS.md` meets the limit only when the gate fails. Declined: it would make a fourth place naming the mechanism, for a reader who gets the same answer from the gate's own message, and the repository already directs contributors to `AGENTS.md`.
- *A test asserting the documented number matches `LIMIT`.* Drift between the code and the three documents is real, but the test could not catch it: `docs.yml` does not run on a change to `AGENTS.md` or `tickets/`, so a drift introduced by editing prose alone would never reach the check. The grep handle named above is the cheaper answer.

**Nothing found** in error handling or resource cleanup: every file read goes through one `_read_or_none` that turns a read failure into a reported finding, each test builds its tree under `tempfile.mkdtemp()` and registers `shutil.rmtree` as cleanup, and the tool holds no state between pages.

**Tests.** Seven now, one per behaviour: under the limit, over the limit, fenced code not counted (three shapes plus the inline-code guard), the new HTML-comment case, an excepted page, a missing literal path, and a set matching no files. None was cut — none restates the implementation or verifies a mock, and each pins a documented contract. No test was added for the configuration values, the output formatting or the unreadable-file branch.

| Command | Result |
|---|---|
| `python tools/docs-size-check/check_size.py` | `11 files checked, 0 over the limit, 0 excepted`, exit 0 |
| `python -m unittest discover -s tools/docs-size-check` | 7 tests, OK |
| `python -m unittest discover -s tools/docs-link-check` | 14 tests, OK — the neighbouring tool whose fence and comment rules this one copies |
| `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` | `88 files scanned, 1056 links checked, 0 errors, 203 pending, 10 external` — unchanged |
| `dotnet build Dataphor.slnx` | 0 errors, 143 pre-existing warnings, 25 s |
| GitHub Actions **Docs** on the implement commit | success, 18 s, both steps run |

No C# test was run: the diff is Python, Markdown and YAML only, no existing fixture covers any of it, and the build was run because the project rule asks for it. No pre-existing failure was met.
