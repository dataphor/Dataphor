description: The automated build now runs the documentation link checker, so a broken link in the documentation fails the build instead of waiting for someone to notice it.
architecture: docs/modernization-plan.md#ws8-documentation
files: .github/workflows/docs.yml, .github/workflows/build.yml, tools/docs-link-check/README.md, docs/modernization-plan.md, AGENTS.md
----
## What landed

`tools/docs-link-check/check_links.py` runs in CI as the *Documentation links* step of the existing *Docs* workflow (`.github/workflows/docs.yml`), after the two architecture page-size steps:

```
python3 tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv
```

A plain `run:` step with no `continue-on-error` and no pipe, so the checker's non-zero exit fails the job. `tools/docs-link-check/**` was added to both the `push` and the `pull_request` path lists so that a change to the checker runs it; `tools/docs-convert/**` was deliberately left out, because the pending list changes only when the hand-run converter is rerun and a change there cannot break a link under `docs/`. Nothing else about the workflow changed: `ubuntu-latest`, `timeout-minutes: 10`, `permissions: contents: read`, the concurrency group, the pinned checkout and the `pull_request` (not `pull_request_target`) trigger are as they were.

`--pending` names the tab-separated list of link targets that do not exist yet - the links into the DocBook reference pages that `docs-convert-legacy-reference` will create (defect K17 in the plan). Without it the check fails with 203 errors today, so gating everything *except* those tracked-missing links was chosen over gating nothing until that conversion lands. The argument has an owner: `docs-remove-old-sources` deletes `tools/docs-convert/` with the list and drops the argument in the same commit, and takes this item as a `prereq:` so the two cannot land out of order.

The rest of the change is documentation catching up: the header comment of `docs.yml` and the note in `build.yml` that explains why docs checks live in a separate workflow no longer describe this work as pending; `tools/docs-link-check/README.md` says the step exists and names the `--pending` argument and its removal owner; the `docs-link-check-ci` row of the WS8 work-item table in `docs/modernization-plan.md` is marked **Done**; and the `tools/` layout row for `docs-link-check/` in `AGENTS.md` says CI gates it.

Implemented by `ticket(implement): docs-link-check-ci`. The review pass added two things, below.

## Review findings

**What was checked.** The implement diff read before the handoff summary; then the workflow file as it stands; `check_links.py`'s `main()` and `read_pending()` for the exit codes and the pending-file contract the step depends on; the downstream ticket that owns removing `--pending`; the claims in the handoff against the tree; and every command the step and its neighbours run.

Ran, all green: the step's exact command (`90 files scanned, 1079 links checked, 0 errors, 203 pending, 12 external`, exit 0 - matching the figure the implement ticket measured at `e414e316`); `python tools/docs-size-check/check_size.py` (`13 files checked, 0 over the limit, 0 excepted`); both Python tools' unit tests (14 and 7, OK); a `yaml.safe_load` of the workflow confirming the step order and both path lists; and `dotnet build Dataphor.slnx` - 0 errors, 127 pre-existing warnings - because the project rules say always build, even though this diff has no C#, no project file and no test in it. No `dotnet test`: no file any test reads is touched.

**Verified, rather than assumed:**

- **The removal of `--pending` still has an owner.** `tickets/implement/23.9-docs-remove-old-sources.md` names `docs-link-check-ci` in its `prereq:`, explains that an out-of-order landing would leave the step pointing at a deleted file and exiting 2 on every push, lists both `.github/workflows/docs.yml` and `tools/docs-link-check/README.md` in its `files:`, and has removing the argument as a TODO item with the bare command to run first. The handoff's main worry is answered: the clause is intact.
- **A missing or malformed `--pending` file really does exit 2** - `read_pending` raises and `main` routes it through `parser.error`, which is argparse's exit 2. So the failure mode the ordering protects against is loud, not silent.
- **Line endings are not a first-run-on-Linux risk.** The pending list is CRLF in a Windows working tree and LF in the committed blob (`core.autocrlf` is on, `.gitattributes` sets nothing for it), and `read_pending` opens it with `newline=""` and parses it with the `csv` module, which terminates rows on either ending. `read_pending` returns 204 targets with no stray carriage return and no blank, on this checkout. Letter case was already gated on Windows too, since the checker compares each path component against the real directory listing, so the Ubuntu runner's case-sensitive file system should produce no surprise either.

**Fixed in this pass (minor):**

- **Nothing told a contributor the gate exists.** The page-size gate has a *Rules that are easy to break* bullet in `AGENTS.md` that names the command to run locally; the link gate had no equivalent, so the first anyone would learn of it is a red build. Added a bullet there: what the gate rejects (missing file, wrong letter case, missing heading anchor), and to run the command in the workflow's *Documentation links* step after moving, renaming or linking a page under `docs/`. It names the step rather than repeating the command, so `docs-remove-old-sources` has nothing extra to update when the `--pending` argument goes.

**Parked as a tripwire, not filed (conditional):**

- **Only `docs/` is scanned.** Links from `AGENTS.md`, the root `README.md` and the tool READMEs into `docs/` are not gated, so a page rename could break them silently. All of them resolve today - measured by running the checker over the whole repository - and widening the root is not free: it also reports the `tess/` paths and the `tools/docs-convert/index-extra.md` fragments as errors, and `--pending` targets resolve relative to each root, so the 203 exemptions would stop applying. Recorded as a `NOTE:` comment above the step in `docs.yml` with those specifics, so the next person to consider widening it starts from the measurement rather than repeating it.

**Considered and not changed:**

- **Step ordering.** The page-size steps run first, so a docs change that trips both gates needs two CI round-trips. Adding `if: always()` to the later steps would report everything at once, but it changes the semantics of steps this item does not own, and the whole job is about two seconds of work on a rare double failure. Left fail-fast.
- **No new test**, agreeing with the implementer. The step is wiring, which the project's test rules exclude; nothing here ports or changes a subsystem's behaviour, so no characterization test is owed; and the checker's own 14 tests already exist. Getting those tests run by CI is `debt-tool-tests-not-run-by-ci` in `tickets/backlog/`, which owns doing it by discovery rather than by a hard-coded list - adding one `unittest discover` step here would pre-empt exactly that. Confirmed that ticket is still on the board.
- **No `docs/migration-notes.md` entry.** Nothing here is observable by an application, a deployment script or a custom C# extension; it is a CI gate on the repository's own documentation.

**Nothing found** in these categories, each looked at rather than skipped: error handling (the only failure path is the step's exit status, and it propagates); resource cleanup (none involved); type safety and DRY (no code was written); file size (no source file grew); comments narrating statements (the two comment blocks state why `--pending` is there and what widening the root would cost - neither restates its line); and compatibility surfaces (no D4, class name, assembly, file format, error code, contract or port is anywhere near this change).

**One handoff claim corrected, for the record:** the `AGENTS.md` wording "gated by `.github/workflows/docs.yml`" matches the hierarchy rule bullet in *Rules that are easy to break*, not the `docs-size-check/` row of the layout table, which carries no such clause. No change needed; the new wording is right either way.

## Known gap

The step has not yet run on GitHub Actions - everything above is local. The inclusion-filter caveat already written into both workflow files stands unchanged: a push touching none of the listed paths produces no run at all, so this must not become a required status check while the filter is there.
