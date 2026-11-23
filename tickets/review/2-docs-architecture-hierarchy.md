description: A check now counts the words of each architecture documentation page and fails when one grows past 1,500, runs in continuous integration, and the rule is written down, so the documentation stays hierarchical instead of growing back into one long page.
architecture: docs/modernization-plan.md#ws8-documentation
files: tools/docs-size-check/check_size.py, tools/docs-size-check/test_check_size.py, tools/docs-size-check/README.md, .github/workflows/docs.yml, .github/workflows/build.yml, AGENTS.md, tickets/rules/10-dataphor.md, docs/modernization-plan.md, tools/docs-link-check/README.md, tickets/backlog/feat-ws8-documentation-remainder.md
difficulty: medium
----
## What landed

The second of the two tickets for the owner's direction of 2026-11-23. `docs-architecture-split` had already made the architecture document a short root page plus one page per subsystem; this ticket makes that shape hold.

**The check — `tools/docs-size-check/`** (one Python file, standard library only, no network, beside `tools/docs-link-check/` and following its conventions):

- `check_size.py`. Configuration at the top, each value in one place: `LIMIT = 1500`; `CHECKED` = `docs/architecture.md` and `docs/architecture/**/*.md` as repository-relative globs; `EXCEPTIONS = {}` — **empty**, as the owner required.
- A word is a whitespace-separated token outside fenced code blocks, counted as `wc -w` counts. The fence rule is the link checker's, including its guard that a backtick fence whose info string holds a backtick is inline code rather than a fence — the guard matters because mistaking inline code for a fence *undercounts* a page, which is the one failure mode that lets an oversized page pass.
- Findings: `path: N words (limit 1500)` (error), `path: N words (exception: reason)` (not an error), `path: missing` for a literal path in the set that does not exist (error), `checked set matched no files` (error), then a summary line. Exit 0 / 1 / 2 (bad arguments), the link checker's convention. Paths repository-relative with forward slashes.
- `--root DIR` resolves the set elsewhere (this is how the tests exercise the real configuration without writing into `docs/`); `--limit N` is a what-if. No arguments is the gate: the root comes from the script's own path, so it runs from any working directory and needs no `.git` — checked by running it from `C:\`.
- `check(root, limit, checked, exceptions)` returns a report, so the tests do not parse output.
- `README.md` carries the rules, the word rule and its deliberate imprecision ("a budget, not a measurement"), what to do when the check fails (split the page into a parent page beside a folder of the same name — do not raise the limit, do not add an exception), the verbatim tripwire NOTE about the two pages nearest the limit, and the table of documents deliberately outside the set.

**The gate — `.github/workflows/docs.yml`** (new; `build.yml` ignores `docs/**` on purpose and its NOTE says a docs job must not go there). Push and pull request to `master` and `modernization`, `paths:` `docs/**`, `tools/docs-size-check/**` and the workflow file itself, plus `workflow_dispatch`; `permissions: contents: read` with the public-repository comment, never `pull_request_target`; concurrency group with `cancel-in-progress`; one `ubuntu-latest` job, `timeout-minutes: 10`, `actions/checkout` pinned to the same commit `build.yml` pins, then `python3 tools/docs-size-check/check_size.py` and `python3 -m unittest discover -s tools/docs-size-check`. LF line endings, like `build.yml`.

**The rule in writing.** A new bullet in `AGENTS.md` *Rules that are easy to break* (hierarchy, the number 1,500 *and* the `LIMIT` symbol and file it lives in, the commands, and that a page that outgrows the limit is split rather than excepted); the Layout table's `tools/` row now names `docs-size-check/`; a short rule beside *Migration notes* in `tickets/rules/10-dataphor.md`; a `docs-architecture-hierarchy` work-item row in the plan's WS8 table and a clause in its design notes beside the sentence about the link checker.

**Pointers, not work.** `build.yml`'s NOTE now names `.github/workflows/docs.yml` (comment only; nothing it runs changed). `tools/docs-link-check/README.md` and the `docs-link-check-ci` item of `tickets/backlog/feat-ws8-documentation-remainder.md` each gained one line saying the docs workflow exists and that item adds a step to it rather than creating one.

## Measured word counts (the whole checked set)

Prose outside fenced code blocks, measured 2026-11-23 by the tool itself (`--limit 0` lists every page):

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

Eleven pages, none over the limit, exception list empty. `docs/architecture.md` counts 514 here against 659 by a plain `wc -w`, the difference being its two fenced diagrams. The four documents deliberately outside the set measure as the ticket predicted with this tool's own rule: `modernization-plan.md` 36,794, `migration-notes.md` 18,573, `perf-baseline.md` 3,565, `coverage-report.md` 3,270.

## Tests and checks run

| Command | Result |
|---|---|
| `python tools/docs-size-check/check_size.py` | `11 files checked, 0 over the limit, 0 excepted`, exit 0 |
| `python -m unittest discover -s tools/docs-size-check` | 6 tests, OK |
| `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` | `88 files scanned, 1056 links checked, 0 errors, 203 pending, 10 external` — unchanged by this ticket |
| `dotnet build Dataphor.slnx` | 0 errors, 143 pre-existing warnings, 26 s (no C# changed; run because the project rule says always build first) |

No C# test was run: this diff is Python, Markdown and YAML only, and no existing fixture covers any of it.

The six tests, one per behaviour, each building a small tree in a temporary folder and pointing the tool at it with `--root`:

| Test | What it pins |
|---|---|
| `test_page_under_the_limit_passes` | A page under the limit produces no finding; exit 0 |
| `test_page_over_the_limit_fails` | A page over the limit is reported with its count and the limit, and **exits 1** — the oversized-page demonstration the owner asked for, done on a scratch page in a temporary folder rather than a pushed commit |
| `test_fenced_code_is_not_counted` | The same words pass inside fences (backtick fence with an info string, tilde fence not closed by backticks, unclosed fence running to end of file) and fail outside them (5 words against 165); and a backtick line whose info string holds a backtick is inline code, not a fence |
| `test_excepted_page_is_reported_but_not_an_error` | An excepted page over the limit is reported with its reason and exits 0 |
| `test_missing_literal_path_is_an_error` | A literal path in the set that does not exist is an error naming it; exit 1 — this is what stops a renamed page dropping out of the gate |
| `test_set_matching_no_files_is_an_error` | A set that matches no files is an error; exit 1 |

No test was written for the configuration values or for output formatting, as the ticket directed.

The fence test was checked against a mutant: with the inline-code guard removed from `check_size.py`, `test_fenced_code_is_not_counted` fails and the other five still pass, so that assertion really does discriminate. (The mutant was run in memory, nothing on disk changed.)

## Known gaps — read these first

- **`docs.yml` has never run, and was not machine-parsed.** No YAML parser is installed on this machine (no `pyyaml`, no node `yaml` module) and installing one was out of scope, so the file was verified by eye against `build.yml` plus a tab/trailing-space/odd-indent lint. The first push proves it: check that a **Docs** workflow run appears for this commit (`gh run list --branch modernization`). A YAML error shows up as *no run at all*, not as a red run, so absence is the failure signal to look for.
- **`python3` vs `python`.** The workflow uses `python3` (what the Ubuntu image guarantees); every README and `AGENTS.md` says `python` (what a Windows developer runs). Deliberate, and commented in the workflow.
- **The fence rule is copied from the link checker, not imported**, so that each tool stays one standalone file. Parked as a `NOTE:` tripwire at the rule in `check_size.py:49`: if the link checker's fence rule changes, this copy has to change with it or the two tools stop agreeing on what is prose.
- **A fence indented four spaces or more inside a list item counts as prose here**, where the link checker treats it as a fence. No page in the set has one (checked: the only two fences in the whole set are unindented, in `docs/architecture.md`). The direction is safe — over-counting can fail a page early but never let one through — and it is commented at the rule.
- **The limit is named in three places** by design: the value lives in `LIMIT`, and `AGENTS.md` and `tickets/rules/10-dataphor.md` each name the number *and* the symbol and file, so a drifted copy is findable by grep for `docs-size-check`. Verified by inspection, not by a check.
- **Only the size check is gated.** The link checker is **not** added here: `docs-link-check-ci` owns it and still has its `--pending` argument to retire. Both READMEs and the backlog item now say the step goes into `.github/workflows/docs.yml`.
- **A concurrent board move touched the two tickets this ticket's README cites.** `debt-split-the-modernization-plan` and `debt-split-the-migration-notes` moved from `backlog/` to `blocked/` while this ticket was being worked (not by me). The README therefore names them by slug only, without a stage folder, which is the convention anyway. Nothing else about them was touched.

## What a reviewer should poke at

- Run the gate and the tests; then make a scratch page over the limit under `docs/architecture/` (or a temporary tree with `--root`), confirm exit 1 and the message, and delete it.
- Read `docs.yml` against `build.yml`: the pin on `actions/checkout`, the read-only token, no `pull_request_target`, the `paths:` filter covering `docs/**`, the tool and the workflow file (a change to the tool that the filter missed would ship untested).
- Check the fence rule in `check_size.py` against `tools/docs-link-check/check_links.py` for a real divergence beyond the documented list-indent one — an undercount is the only way an oversized page passes.
- Check that `AGENTS.md`, `tickets/rules/10-dataphor.md` and the plan's WS8 section say the same thing as each other and as the README, and that the number 1,500 appears nowhere without the `LIMIT` pointer beside it.
