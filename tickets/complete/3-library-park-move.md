description: Moved the side projects that will not ship (healthcare-standard libraries, web and cloud prototypes, Oracle, ADO and SAS database libraries) into a "legacy" folder, after renaming the existing "Legacy" documentation folder to lower case, using pure file moves with no content changes so file history follows them.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: legacy/, legacy/libraries/, legacy/Dataphoria/, legacy/Processor/, legacy/Design/
difficulty: easy
review: skip
----
# Park side projects under `legacy/` — the rename-only step (done)

First of two tickets for the work item `library-park-side-projects` (plan decisions D6 and D15). This ticket made only `git mv` operations; every reference fix (legacy solution `Dataphor/Dataphor.sln`, `.gitignore`, `tickets/index-config.json` which still excludes `Legacy/`, the `Sample.Demo` library descriptor, `AGENTS.md` layout table, docs, other tickets) is left to `library-park-side-projects`.

## What moved

Per-directory tracked file counts were re-measured before moving and matched the plan exactly; nothing in the tree had changed.

| From | To | Files |
|---|---|---|
| `Legacy/` (only `Documentation/`) | `legacy/` (two-step case rename via `legacy-rename-tmp`) | 786 |
| `Libraries/FHIR.API`, `FHIR.Core`, `FHIR.Device`, `FHIR.Server`, `FHIR2.Core`, `FHIR3.Core` | `legacy/libraries/<same name>` | 8, 31, 9, 31, 16, 16 |
| `Libraries/CQF.Model`, `CQF.Model.FHIR`, `CQF.Model.QUICK`, `CQF.Model.USCore` | `legacy/libraries/<same name>` | 16, 4, 4, 2 |
| `Libraries/PHINVADS.Core`, `PHINVADS.Device`, `PHINVADS.Test` | `legacy/libraries/<same name>` | 11, 7, 4 |
| `Libraries/TestCQL`, `TestCache` | `legacy/libraries/<same name>` | 2, 4 |
| `Libraries/OracleDevice`, `MSOracleConnection`, `ADOConnection`, `SASDevice` | `legacy/libraries/<same name>` | 15, 6, 6, 11 |
| `Dataphoria/` (top level) | `legacy/Dataphoria/` | 379 |
| `Processor/` | `legacy/Processor/` | 4 |
| `Design/` | `legacy/Design/` | 3 |

All 19 library moves and the three tree moves succeeded; no partial failure.

## Verification (run after the moves, against the index)

- `git ls-files | cut -d/ -f1 | sort -u`: `.gitignore .gitmodules .mcp.json AGENTS.md Dataphor Dataphor.snk Documentation External Images Libraries README.md Setup Tests Tooling Utilities book.json dataphor_license.txt docs global.json legacy tess tickets tools` — one `legacy`, no `Legacy`, `Dataphoria`, `Processor` or `Design`.
- `git ls-files | grep -c '^Legacy/'` = 0; `git ls-files | grep -c '^legacy/'` = 1375.
- `git diff --cached -M -l0 --name-status`: 1375 lines, all `R100`; no other status.
- `git status --short --untracked-files=all`: only `R` entries; no modified files, no untracked leftovers under old paths.
- `tools/net-spike/projects.txt` and `tools/net-spike/run.py` name none of the moved paths (grep, excluding the live `Dataphor/Dataphoria`). The spike was not run; `library-park-side-projects` runs it after the reference fixes.

No tests were added: the change is file moves only, verified by the rename checks above.

## Known transient state for the follow-up

Until `library-park-side-projects` lands: `Dataphor/Dataphor.sln` lists projects at paths that no longer exist; `Sample.Demo.d4l` requires a library no longer under `Libraries/`; `tickets/index-config.json` excludes `Legacy/` rather than `legacy/`; `AGENTS.md` still describes `Legacy/Documentation/` and top-level `Dataphoria/`; `Setup/Files.wxs` still lists the parked libraries (left alone, owned by WS9). On this case-insensitive checkout `ls Legacy` still resolves to the same folder; the index is the source of truth.
