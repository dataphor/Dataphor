description: Move the side projects that will not ship (healthcare-standard libraries, web and cloud prototypes, Oracle, ADO and SAS database libraries) into a "legacy" folder, renaming the existing "Legacy" documentation folder to lower case first, using pure file moves with no content changes so file history follows them.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: Legacy/, Libraries/FHIR.API/, Libraries/FHIR.Core/, Libraries/FHIR.Device/, Libraries/FHIR.Server/, Libraries/FHIR2.Core/, Libraries/FHIR3.Core/, Libraries/CQF.Model/, Libraries/CQF.Model.FHIR/, Libraries/CQF.Model.QUICK/, Libraries/CQF.Model.USCore/, Libraries/PHINVADS.Core/, Libraries/PHINVADS.Device/, Libraries/PHINVADS.Test/, Libraries/TestCQL/, Libraries/TestCache/, Libraries/OracleDevice/, Libraries/MSOracleConnection/, Libraries/ADOConnection/, Libraries/SASDevice/, Dataphoria/, Processor/, Design/
difficulty: easy
review: skip
----
# Park side projects under `legacy/` — the rename-only step

This is the first of two tickets for the work item `library-park-side-projects` (plan decisions D6 and D15; layout table rows in [Repository layout](../../docs/modernization-plan.md#repository-layout)). This ticket does **only** `git mv` operations. Every reference fix (legacy solution, `.gitignore`, code-search index config, the `Sample.Demo` library descriptor, docs, other tickets) is done by the follow-up ticket `library-park-side-projects`, so that the commit the runner makes for this ticket contains renames and nothing else.

## Why two tickets

The plan's "Rules for the move" say a move commit contains only renames (`git mv`), with reference fixes in later commits, so Git records exact renames and `git log --follow` / `git blame` keep working. Under tess the implementing agent does not commit; the runner makes one commit per ticket stage. Splitting the work item is the only way to get a rename-only commit. The runner's commit will also contain this ticket's own board file moving under `tickets/`; that is the only non-rename entry allowed.

## Decisions already made (do not reopen)

- **`Legacy/` becomes `legacy/`.** The plan's layout table (row for `Documentation/`, `Legacy/Documentation/`) says this ticket first renames `Legacy/` to `legacy/` with a two-step case rename, so the DocBook sources sit at `legacy/Documentation/` until WS8 deletes them. Reason: this machine runs Git with `core.ignorecase=true`, so `legacy/` and `Legacy/` are the same folder on disk here but two folders on a case-sensitive checkout.
- **Every parked D4 library goes under `legacy/libraries/<same name>`**; the non-library trees go directly under `legacy/`. The plan's table gives `legacy/libraries/` for the Oracle/ADO/SAS libraries and just `legacy/` for the FHIR family; putting every parked D4 library in one place is the simpler reading and `repo-move-to-new-layout` already plans `legacy/libraries/Documentation` next to them. Nothing will move these again.
- **No code moves for the four `FHIRCore.*` class names.** Plan Q7 and the WS7 table record that no code in the repository registers them (they appear only inside `//` comments); the general-purpose `FHIR.Core.`-prefixed dynamic/JSON nodes already ship from `Libraries/System.Internet` and stay there. Nothing in this ticket touches `System.Internet`.

## Moves

Tracked file counts measured with `git ls-files <path> | wc -l` at the start of planning; re-measure before moving.

| From | To | Files |
|---|---|---|
| `Legacy/` (only `Legacy/Documentation/`) | `legacy/` | 786 |
| `Libraries/FHIR.API`, `FHIR.Core`, `FHIR.Device`, `FHIR.Server`, `FHIR2.Core`, `FHIR3.Core` | `legacy/libraries/<same name>` | 8, 31, 9, 31, 16, 16 |
| `Libraries/CQF.Model`, `CQF.Model.FHIR`, `CQF.Model.QUICK`, `CQF.Model.USCore` | `legacy/libraries/<same name>` | 16, 4, 4, 2 |
| `Libraries/PHINVADS.Core`, `PHINVADS.Device`, `PHINVADS.Test` | `legacy/libraries/<same name>` | 11, 7, 4 |
| `Libraries/TestCQL`, `Libraries/TestCache` | `legacy/libraries/<same name>` | 2, 4 |
| `Libraries/OracleDevice`, `MSOracleConnection`, `ADOConnection`, `SASDevice` | `legacy/libraries/<same name>` | 15, 6, 6, 11 |
| `Dataphoria/` (top level, not `Dataphor/Dataphoria`) | `legacy/Dataphoria/` | 379 |
| `Processor/` | `legacy/Processor/` | 4 |
| `Design/` | `legacy/Design/` | 3 |

Total 1,375 renamed files (786 + 203 + 379 + 4 + 3).

Not moved here (owned elsewhere): `Libraries/Documentation` and `Libraries/ODBCConnection` (`repo-move-to-new-layout`), `Dataphor/Dataphoria` (the IDE, live), `Documentation/` (WS8), `Setup/` (WS9; its `Files.wxs` still lists the parked libraries and is left as it is, like the earlier delete tickets left it).

Shell sketch (Git Bash, from the repository root):

```
git mv Legacy legacy-rename-tmp
git mv legacy-rename-tmp legacy
mkdir legacy/libraries
for d in FHIR.API FHIR.Core FHIR.Device FHIR.Server FHIR2.Core FHIR3.Core CQF.Model CQF.Model.FHIR CQF.Model.QUICK CQF.Model.USCore PHINVADS.Core PHINVADS.Device PHINVADS.Test TestCQL TestCache OracleDevice MSOracleConnection ADOConnection SASDevice; do git mv "Libraries/$d" "legacy/libraries/$d" || break; done
git mv Dataphoria legacy/Dataphoria
git mv Processor legacy/Processor
git mv Design legacy/Design
```

## Edge cases & interactions

- **Case-only rename on a case-insensitive file system.** Go through the temporary name in two `git mv` steps; a direct `git mv Legacy legacy` is what the plan forbids. Verify with `git ls-files | cut -d/ -f1 | sort -u` (the index, not the file system): exactly one `legacy` entry, no `Legacy`, no `Dataphoria`, `Processor` or `Design`. Also `git ls-files | grep -c '^Legacy/'` must be 0 and `git ls-files | grep -c '^legacy/'` must be 1,375.
- **Destination parent must exist.** `git mv` does not create `legacy/libraries/`; create it after the case rename (creating it before would recreate a `legacy` folder that collides with `Legacy` on disk).
- **Partial failure.** If a `git mv` fails (typically "Permission denied" because a process holds a file open), stop, do not fall back to copy-and-delete, and do not undo the moves already made: report which paths moved and which did not in the handoff. Re-running the remaining `git mv` lines is safe.
- **Line-ending normalization.** `core.autocrlf=true` here and there is no `.gitattributes` yet. A renamed file must not show as modified. Verify `git diff --cached -M --name-status | grep -v '^R100'` prints nothing, and `git status --short` shows no ` M`/`M ` entries. If rename detection reports `D`/`A` pairs instead of `R100` because of the size of the change, re-run with `-l0`; exact renames do not depend on content edits, so any non-`R100` line is a real problem to fix before handoff.
- **Untracked and ignored files** (build output under `Source/bin`, `obj`) travel with a directory `git mv`. That is fine; check `git status --short` shows no untracked leftovers under the old paths, and delete nothing that is untracked.
- **The spike is unaffected.** `tools/net-spike/projects.txt` and `COVERAGE_LIBS` in `tools/net-spike/run.py` name none of the moved paths (checked at planning). Confirm with a grep for the moved names in those two files; running the spike is left to `library-park-side-projects`, which runs it after both tickets.
- **Transient dangling references between the two commits.** After this commit, `Dataphor/Dataphor.sln` lists projects at paths that no longer exist, `Sample.Demo.d4l` requires a library that is no longer under `Libraries/`, and the code-search index config excludes `Legacy/` rather than `legacy/`. All of that is fixed by `library-park-side-projects`, which runs next. The legacy solution does not build today anyway, and the spike does not read it.
- **No content edits anywhere**, including docs and this ticket's own files outside `tickets/`. Verified by the `R100` check.

## TODO

- Re-measure `git ls-files | cut -d/ -f1 | sort -u` and the per-directory counts; if a prerequisite or a concurrent ticket changed the tree, adjust the list and say so in the handoff.
- Run the two-step case rename of `Legacy/` to `legacy/` and check `git ls-files` shows only `legacy/`.
- Create `legacy/libraries/` and `git mv` the 19 library folders.
- `git mv` `Dataphoria/`, `Processor/`, `Design/` under `legacy/`.
- Verify: top-level entries, 1,375 `legacy/` paths, only `R100` in `git diff --cached -M --name-status`, no modified files in `git status --short`.
- Write the `complete/` handoff with the verification output summarized (counts, the top-level listing) so `library-park-side-projects` can start from it.
