description: Add a check that counts the words of each architecture page and fails when one grows past a limit, run it in continuous integration, and write the rule down, so the documentation stays hierarchical instead of growing back into one long page.
prereq: docs-architecture-split
architecture: docs/modernization-plan.md#ws8-documentation
files: tools/docs-size-check/, .github/workflows/docs.yml, .github/workflows/build.yml, AGENTS.md, tickets/rules/10-dataphor.md, docs/modernization-plan.md, tools/docs-link-check/README.md, tickets/backlog/feat-ws8-documentation-remainder.md
difficulty: medium
----
## What this is

Second of two tickets for the owner's direction of 2026-11-23. `docs-architecture-split` makes the architecture document a root page plus one child page per subsystem under `docs/architecture/`. This ticket makes the shape hold: a check that fails when a page in that set grows past a word limit, that check run in continuous integration, and the rule written where agents and people read it.

The split has to land first because the check cannot be green before it: the unsplit `docs/architecture.md` is 3,633 words against a 1,500-word limit, and the owner requires the exception list to be empty when this lands.

## The limit: 1,500 words

Keep the owner's default. After the split the largest page is `docs/architecture/catalog-and-store.md` at about 1,025 words and the root is about 700, so 1,500 leaves every page roughly a third to a half of its budget for growth before a split is required, and no page needs an exception. Measured against the two pages that actually grow (`catalog-and-store.md`, `hosting-protocol-discovery.md` at about 985), that is several landing tickets' worth of paragraphs.

## The check

`tools/docs-size-check/`, beside `tools/docs-link-check/` and following its conventions: one Python file, standard library only, no network, plus a `README.md` and unit tests.

- `check_size.py`. Configuration at the top of the file, each value in exactly one place:
  - `LIMIT = 1500`.
  - `CHECKED` — the set to check, as repository-relative glob patterns: `docs/architecture.md` and `docs/architecture/**/*.md`. Nothing else is in the set; see *What is deliberately outside the set* below.
  - `EXCEPTIONS` — a mapping of repository-relative path to the reason that page is allowed over the limit. **Empty when this ticket lands.**
- A word is a whitespace-separated token outside fenced code blocks, counted as `wc -w` counts, so Markdown punctuation (a `##` marker, a table's `|`) counts too. This is a budget, not a measurement; say so in the README. The point of excluding fenced code is that a diagram or a long code sample must not force a page to split.
- Fenced code blocks follow the link checker's rule, so that the two tools agree on what is prose: a line indented at most three spaces whose content starts with three or more backticks or tildes opens a block; a line that is the same character repeated at least as many times and nothing else closes it; a tilde fence is not closed by backticks; an unclosed fence runs to the end of the file. Keep the link checker's one guard — a backtick fence whose info string contains a backtick is inline code, not a fence — because mistaking inline code for a fence would silently *undercount* and let an oversized page pass.
- Files are read as UTF-8 with or without a byte-order mark, with CRLF handled. A file that is not valid UTF-8 is an error, not a crash.
- Command line: no arguments checks the configured set, resolved against the repository root (the folder two levels above the script, so the tool runs from any working directory and needs no `.git`). `--root DIR` resolves the set against another folder instead — this is what lets the tests exercise the real configuration without writing into `docs/`. `--limit N` overrides the limit for a what-if; the gate uses the default.
- Output, one line per finding, paths repository-relative with forward slashes as the link checker prints them:
  - over the limit: `docs/architecture/x.md: 1623 words (limit 1500)` — an error;
  - excepted and over: `docs/architecture/x.md: 1623 words (exception: <reason>)` — not an error;
  - a literal (non-glob) path in the set that does not exist: `docs/architecture/x.md: missing` — an error, so that a page renamed without updating the configuration cannot drop out of the gate unnoticed;
  - a glob that matches nothing is not an error, but a set that matches no files at all is: `checked set matched no files`.
  - then a summary line: files checked, how many over the limit, how many excepted.
- Exit code 0 when there is no error, 1 when there is, 2 for bad arguments — the link checker's convention.
- Expose `check(...)` returning the findings so the tests do not have to parse output for every case.

### `README.md`

What the tool checks, how to run it (`python tools/docs-size-check/check_size.py`, and `python -m unittest discover -s tools/docs-size-check`), the word rule and its known imprecision, the exception list and that it is meant to stay empty, and what to do when the check fails: split the page into a parent page beside a folder of the same name holding its children — the shape `docs/architecture.md` has beside `docs/architecture/` — rather than raising the limit or adding an exception.

Include this tripwire line verbatim, so the person meeting a red gate finds it:

```
NOTE: catalog-and-store.md (~1,025 words) and hosting-protocol-discovery.md (~985) are the pages nearest
the limit and the two that grow; when one trips the check, split that page into a parent page beside a
folder of the same name rather than raising the limit.
```

### Tests

`tools/docs-size-check/test_check_size.py`, `unittest`, each case building a small tree in a temporary folder and pointing the tool at it with `--root`:

- a page under the limit: no findings, exit 0;
- a page over the limit: one finding naming the path, the count and the limit, **exit 1** — this is the demonstration the owner asked for (a deliberately oversized scratch page, not a pushed commit), so it must assert the exit code and not only the message;
- fenced code excluded: a page whose words sit inside a fence passes while the same words outside a fence fail; one case covers the fence shapes that matter — a backtick fence with an info string, a tilde fence, and an unclosed fence whose remainder is excluded;
- a page in the exception list and over the limit: not an error, reported with its reason, exit 0;
- a literal path in the set that does not exist: error naming it, exit 1;
- a set that matches no files: error, exit 1.

That is one case per behaviour, which is what the project's testing rule asks of a new tool with branching logic. Do not add tests for the configuration values themselves or for the output formatting.

## The gate

`.github/workflows/docs.yml` — a new workflow, because `build.yml` ignores `docs/**` on purpose and its own NOTE says a docs job must not be added to it.

- `on: push` and `pull_request` to `master` and `modernization`, with `paths:` `docs/**`, `tools/docs-size-check/**` and `.github/workflows/docs.yml` (the tool's own path is in the filter so that editing the tool runs its tests), plus `workflow_dispatch`.
- `permissions: contents: read`, and never `pull_request_target` — the repository is public; `build.yml` says the same and the reason belongs in a comment here too.
- `concurrency` group and `cancel-in-progress`, as `build.yml` has.
- One job on `ubuntu-latest` (the check is standard-library Python; no `actions/setup-python` step, and no Windows runner needed), `timeout-minutes: 10`, steps: `actions/checkout` pinned to the same commit `build.yml` pins, then `python3 tools/docs-size-check/check_size.py`, then `python3 -m unittest discover -s tools/docs-size-check`. Use `python3`, not `python`, which is what the Ubuntu runner image guarantees; the READMEs and `AGENTS.md` keep saying `python`, which is what a Windows developer runs.
- A comment at the top saying what the workflow is for and that the link checker's step (`docs-link-check-ci`) belongs in it.
- `build.yml`: extend its existing NOTE to name `.github/workflows/docs.yml` as the docs workflow, so the next reader is not told to create one that exists. Comment only — do not change what `build.yml` runs.
- `.github/workflows/build.yml` has LF line endings; give `docs.yml` the same.

The link checker is **not** added in this ticket: `docs-link-check-ci` (a work item of `feat-ws8-documentation-remainder`) owns it, and it has its own `--pending` argument to retire. Add one line to that backlog item saying the workflow now exists and the item adds a step to it rather than creating a workflow, and one line to `tools/docs-link-check/README.md` saying where that step goes. Say in the handoff that only the size check is gated.

## The rule in writing

- `AGENTS.md`, a new bullet in *Rules that are easy to break*: architecture documentation is hierarchical — `docs/architecture.md` is a short overview and the detail lives in one page per subsystem under `docs/architecture/`; put new architecture text on the child page that owns the subject, not in the root; every page in that set has a word limit (1,500 today, `LIMIT` in `tools/docs-size-check/check_size.py`), checked by `python tools/docs-size-check/check_size.py` and gated by `.github/workflows/docs.yml`; a page that outgrows the limit is split into a parent page beside a folder of the same name holding its children, not allowed to grow and not excepted. Name the number once, with the pointer to where it lives, so a later change to the limit is findable.
- `AGENTS.md`, the Layout table's `tools/` row: name `docs-size-check/` beside `docs-link-check/`.
- `tickets/rules/10-dataphor.md`, a short rule beside the *Migration notes* paragraph: a ticket that adds architecture text names the child page under `docs/architecture/` that the text goes on, never the root; run `python tools/docs-size-check/check_size.py` after editing those pages.
- `docs/modernization-plan.md`, WS8: a work-item row for `docs-architecture-hierarchy` — split `architecture.md` into a root plus one page per subsystem and gate every page at 1,500 words with `tools/docs-size-check` run by `.github/workflows/docs.yml`, done as two chained tickets (`docs-architecture-split`, then this one), no prerequisite, medium — and a clause in the WS8 design notes beside the sentence about the link checker saying that a page-size check gates the architecture pages.

## What is deliberately outside the set

Measured with the same rule (words outside fenced code blocks):

| Document | Words | Why it is outside |
|---|---|---|
| `docs/modernization-plan.md` | 36,794 | It is the anchor for every ticket (39 references to 14 of its anchors on the open board and in the docs); splitting it moves those anchors. Owner's call, filed as `debt-split-the-modernization-plan` in `backlog/`. |
| `docs/migration-notes.md` | 18,573 | A running append-only record, not a hierarchy. Filed as `debt-split-the-migration-notes` in `backlog/`. |
| `docs/perf-baseline.md` | 3,565 | A record of measurements with its procedure, not architecture prose. |
| `docs/coverage-report.md` | 3,270 | Same: the output of one measured run. |
| the converted user, developer and reference documentation | — | Its structure comes from the books it was converted from. |

Do not add any of these to `CHECKED`, and say in the README that leaving them out is deliberate and where the two backlog tickets are. The two backlog tickets already exist — planning filed them; this ticket does not create them.

## Edge cases & interactions

- **A renamed or deleted page silently leaving the gate.** Handled by making a missing literal path in the set an error, and by the empty-set error. Both have tests.
- **Undercounting through a misdetected fence.** The failure mode of a fence bug is a page that passes when it should not, which is why the fence rule copies the link checker's, including its inline-code guard. Verified by the fence test case.
- **The limit named in three places** (the script, `AGENTS.md`, the ticket rules). The script holds the value; the prose names the number *and* the symbol and file it lives in, so a drifting copy is findable by grep. Verified by inspection.
- **The workflow not running for the change that matters.** The `paths:` filter must cover `docs/**`, the tool and the workflow file itself; a change to the tool that is not covered would ship untested. Verified by reading the filter against the files this ticket adds.
- **Fork pull requests.** Read-only token, no secrets, no `pull_request_target`. Verified by inspection against `build.yml`.
- **CRLF, byte-order marks and non-UTF-8 input.** The docs are CRLF without a byte-order mark; read with `utf-8-sig` and normalise line endings, and report an unreadable file rather than crashing the gate.
- **Output on Windows.** Repository-relative paths with forward slashes, as the link checker prints them, so a finding can be pasted into a link or a grep.
- **Determinism.** Sort the matched files so the output and the tests do not depend on directory order.
- **Running from any directory.** The root is derived from the script's own path, not the working directory; `--root` overrides it. The tests cover the override; the plain invocation is covered by the gate itself.
- **Source hygiene.** `tools/docs-link-check/*.py` is CRLF without a byte-order mark; match it for the new Python files. `.github/workflows/*.yml` is LF.
- **A concurrent board.** Touch only the files listed above.

## Done when

- `python tools/docs-size-check/check_size.py` exits 0 on the current tree, with every page of the set listed as under the limit and an empty exception list.
- `python -m unittest discover -s tools/docs-size-check` passes, with one case per behaviour above, including the oversized-page case that asserts exit code 1.
- `.github/workflows/docs.yml` exists and runs both commands under the path filter, with a read-only token.
- `build.yml`'s NOTE names the new workflow; `tools/docs-link-check/README.md` and the `docs-link-check-ci` item of `feat-ws8-documentation-remainder` say where the link-checker step goes.
- The rule is in `AGENTS.md` and `tickets/rules/10-dataphor.md`; the plan's WS8 section carries the work item and the design-note clause; `AGENTS.md`'s `tools/` row names the new tool.
- The handoff reports: the measured word count of every page in the set, the test command and its result, and that only the size check (not the link checker) is gated.

## TODO

- Write `tools/docs-size-check/check_size.py` with the configuration, the fence-aware word count, the findings and the exit codes above.
- Write `tools/docs-size-check/test_check_size.py` with one case per behaviour, each using `--root` against a temporary tree.
- Write `tools/docs-size-check/README.md`, including the tripwire NOTE and the list of documents deliberately outside the set.
- Run the check against the tree and the tests; record the per-page counts.
- Add `.github/workflows/docs.yml`; extend `build.yml`'s NOTE.
- Add the rule to `AGENTS.md` (new bullet, and the `tools/` row) and to `tickets/rules/10-dataphor.md`.
- Add the WS8 work-item row and the design-note clause to `docs/modernization-plan.md`.
- Add the one-line pointers to `tools/docs-link-check/README.md` and to the `docs-link-check-ci` item of `tickets/backlog/feat-ws8-documentation-remainder.md`.
