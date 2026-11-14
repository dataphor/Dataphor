description: Move every remaining product folder, library, sample, test and third-party binary into the repository's new folder structure using pure file renames with no content changes, so that each file's history follows it; the paths that point at the old locations are fixed by the two tickets after this one.
architecture: docs/modernization-plan.md#repository-layout
files: Dataphor/, Libraries/, Tests/, External/, Tooling/, dataphor_license.txt, src/, libraries/, samples/, tests/, tests/libraries/, external/, docs/reference/, tools/editor-support/bbedit/, legacy/libraries/Documentation/, LICENSE, Dataphor.sln
difficulty: easy
review: skip
----
# Move to the new layout — the rename-only step

First of three tickets for the work item `repo-move-to-new-layout` (plan decision D14). This ticket makes **only `git mv` operations** (plus one spike run before them, for the baseline). It edits no file. The next ticket, `repo-move-fix-references`, rewrites the paths inside project files, the legacy solution, `.gitignore`, the code-search configuration and the spike; the last, `repo-move-to-new-layout`, rewrites the documentation.

**Run with nothing else in flight.** Every other ticket that touches the tree waits on `repo-move-to-new-layout`, so the runner will not schedule them alongside this one; a human should not start other work in the tree until the work item lands.

Between this ticket and `repo-move-fix-references` the tree is knowingly inconsistent: the legacy solution, every project file's cross-folder references and the spike all point at old paths. That is expected; do not fix any of it here.

## Step 1 — baseline spike, before any move

The proof that the move changed nothing is the spike giving the same results before and after (plan, "Rules for the move"). Capture "before" now, at the current HEAD, with a work directory **outside** the repository (your scratchpad), in the foreground:

```
python tools/net-spike/run.py <scratchpad>/spike-before --smoke --coverage 2>&1 | tee tickets/.logs/repo-move-renames.baseline.spike.log
```

Expected (the baseline every recent ticket has reproduced): engine, server and library projects 0 errors; `Concurrency: … 0 errors`; `Resolution cache: … 0 errors`; `SUMMARY pass=62 fail=14 timeout=0 skipped-ignored=13`; non-zero compile counts only for Dataphoria 1, DAEServer 4, DAE.Service 7, D4Runner 1 and the three test projects. If it does not match, stop and record what you saw in the handoff — the next ticket compares against this log, so a different baseline must be known, not silently used. Delete the `spike-before` work directory afterwards; keep the log.

## Step 2 — the moves

Inventory re-measured at planning (after `repo-delete-dead-trees`, `library-remove-dead-devices`, `library-park-side-projects`): top level is `.gitignore .gitmodules .mcp.json AGENTS.md Dataphor Dataphor.snk Documentation External Images Libraries README.md Setup Tests Tooling Utilities book.json dataphor_license.txt docs global.json legacy tess tickets tools`. Before moving, re-run `git ls-files | cut -d/ -f1,2 | sort | uniq -c` for `Dataphor`, `Libraries`, `Tests`, `External`, `Tooling` and compare with the counts below; any entry not in this table is a planning gap — stop and record it rather than guessing a destination.

### The table (decisions are final; see the plan's Repository layout section)

| From | To | Files |
|---|---|---|
| `Dataphor/Base`, `Base.Windows`, `Server` | `src/engine/Base`, `src/engine/Base.Windows`, `src/engine/Server` | 39, 5, 53 |
| `Dataphor/DAE` | `src/engine/DAE`, **and** `Engine.csproj` renamed to `DAE.csproj` | 282 |
| `Dataphor/DAE.SQLite`, `DAE.MSSQL`, `DAE.PGSQL` | `src/engine/Store.SQLite`, `Store.MSSQL`, `Store.PGSQL` (project file names unchanged) | 8, 6, 5 |
| `Dataphor/Common`, `Client`, `DAE.NativeCLI` | `src/protocol/Common`, `Client`, `NativeCLI` | 14, 25, 15 |
| `Dataphor/DAE.Client`, `DAE.Client.Provider`, `Frontend.Client` | `src/client/` same names | 27, 15, 36 |
| `Dataphor/DAE.Client.Controls`, `Frontend.Client.Windows`, `Frontend.Client.WPF` | `src/windows/` same names | 86, 132, 21 |
| `Dataphor/Dataphoria` (minus its four DLLs, below) | `src/windows/Dataphoria` | 189 |
| `Dataphor/Frontend.Client.Windows.Executable` | `src/windows/WindowsClient` | 8 |
| `Dataphor/DAE.Service.ConfigurationUtility` | `src/windows/ConfigurationUtility` | 22 |
| `Dataphor/DAEServer`, `DAE.Service`, `D4Runner` | `src/hosts/DAEServer`, `DAEService`, `D4Runner` | 5, 12, 5 |
| `Dataphor/Dataphor.sln` | `Dataphor.sln` (repository root) | 1 |
| `Dataphor/.editorconfig` | `src/.editorconfig` (still covers the same source files; `hygiene-gitattributes-editorconfig` replaces it) | 1 |
| `Dataphor/CompilerDefines.txt`, `Dataphor/Error Message Prefix Codes.txt` | `docs/reference/` (unchanged; WS8 converts them) | 2 |
| `dataphor_license.txt` (root) | `LICENSE` | 1 |
| `External/` | `external/` (two-step case rename) | 2 |
| `Dataphor/bin/Mono.Security.dll`, `Npgsql.dll`, `System.Data.SQLite.DLL` | `external/` (keep the `.DLL` spelling) | 3 |
| `Dataphor/Dataphoria/Ascend.dll`, `Ascend.Resources.dll`, `Ascend.Windows.Forms.dll`, `WeifenLuo.WinFormsUI.Docking.dll` | `external/` | 4 |
| `Tests/` (all: `Common.Tests`, `DAE.Server.Tests`, `DAE.Client.Tests`, `Frontend.Client.WPF.Tests`, `Dataphor.Tests.nunit`) | `tests/` (two-step case rename). `Dataphor.Tests.nunit` is deleted by the next ticket | 50 |
| `Libraries/` — Connection, SQLDevice, MSSQLDevice, PGSQLDevice, PGSQLConnection, SimpleDevice, Frontend, Security, General, System.Platform, System.Internet, System.Integration, **ODBCConnection** | `libraries/<Name>` | 290 in all |
| `Libraries/Sample.*` (12: Common, Components, Demo, DetailAdd, Forms, Forum, IssueTracker, Pubs, Rental, ScriptAction, SendEmail, Shipping) | `samples/<Name>` | 168 |
| `Libraries/TestFramework*` (7: TestFramework, .Controls, .Coverage, .Coverage.Base, .Coverage.Devices, .Coverage.Scripts, .StressTest) | `tests/libraries/<Name>` | 186 |
| `Libraries/Documentation` | `legacy/libraries/Documentation` (next to the 19 libraries parked there already) | 12 |
| `Tooling/BBEdit` | `tools/editor-support/bbedit` | 2 |

**Total 1,732 renames.** Area totals: `src/` 1,011 (engine 398, protocol 54, client 78, windows 458, hosts 22, `src/.editorconfig` 1); `libraries/` 290; `samples/` 168; `tests/` 50 + `tests/libraries/` 186; `external/` 9; `legacy/libraries/Documentation` 12; `docs/reference/` 2; `tools/editor-support/bbedit` 2; root `Dataphor.sln` and `LICENSE` 2.

**Stays where it is:** `Dataphor/dataphor_license.txt` (byte-identical duplicate of the licence; the next ticket deletes it) and `Dataphor/netsh_address_reservations.txt` (`hygiene-large-binaries` deletes it). `Images/`, `Documentation/`, `Setup/`, `Utilities/`, `legacy/` (apart from the one addition above), `tess/`, `tickets/`, `docs/` (apart from `docs/reference/`), `tools/` (apart from `editor-support/`), `Dataphor.snk`, `book.json`, `global.json`, `AGENTS.md`, `README.md`, `.gitignore`, `.gitmodules`, `.mcp.json` are not touched.

### Command sequence

Run from the repository root in Git Bash. The order matters: the DLLs leave `Dataphor/Dataphoria` before the folder moves, `tests/` exists before the test libraries go into it, and the case renames go through a temporary name because this checkout is case-insensitive (`core.ignorecase=true`; `Libraries` and `libraries` are the same folder on disk).

```bash
set -e
mkdir -p src/engine src/protocol src/client src/windows src/hosts docs/reference tools/editor-support samples
git mv dataphor_license.txt LICENSE
git mv Dataphor/Dataphor.sln Dataphor.sln
git mv Dataphor/.editorconfig src/.editorconfig
git mv Dataphor/CompilerDefines.txt "Dataphor/Error Message Prefix Codes.txt" docs/reference/
# third-party binaries
git mv External external.tmp
git mv external.tmp external
git mv Dataphor/bin/Mono.Security.dll Dataphor/bin/Npgsql.dll Dataphor/bin/System.Data.SQLite.DLL external/
git mv Dataphor/Dataphoria/Ascend.dll Dataphor/Dataphoria/Ascend.Resources.dll Dataphor/Dataphoria/Ascend.Windows.Forms.dll Dataphor/Dataphoria/WeifenLuo.WinFormsUI.Docking.dll external/
# engine
git mv Dataphor/Base src/engine/Base
git mv Dataphor/Base.Windows src/engine/Base.Windows
git mv Dataphor/Server src/engine/Server
git mv Dataphor/DAE src/engine/DAE
git mv src/engine/DAE/Engine.csproj src/engine/DAE/DAE.csproj
git mv Dataphor/DAE.SQLite src/engine/Store.SQLite
git mv Dataphor/DAE.MSSQL src/engine/Store.MSSQL
git mv Dataphor/DAE.PGSQL src/engine/Store.PGSQL
# protocol
git mv Dataphor/Common src/protocol/Common
git mv Dataphor/Client src/protocol/Client
git mv Dataphor/DAE.NativeCLI src/protocol/NativeCLI
# client
git mv Dataphor/DAE.Client src/client/DAE.Client
git mv Dataphor/DAE.Client.Provider src/client/DAE.Client.Provider
git mv Dataphor/Frontend.Client src/client/Frontend.Client
# windows
git mv Dataphor/DAE.Client.Controls src/windows/DAE.Client.Controls
git mv Dataphor/Frontend.Client.Windows src/windows/Frontend.Client.Windows
git mv Dataphor/Frontend.Client.WPF src/windows/Frontend.Client.WPF
git mv Dataphor/Dataphoria src/windows/Dataphoria
git mv Dataphor/Frontend.Client.Windows.Executable src/windows/WindowsClient
git mv Dataphor/DAE.Service.ConfigurationUtility src/windows/ConfigurationUtility
# hosts
git mv Dataphor/DAEServer src/hosts/DAEServer
git mv Dataphor/DAE.Service src/hosts/DAEService
git mv Dataphor/D4Runner src/hosts/D4Runner
# tests
git mv Tests tests.tmp
git mv tests.tmp tests
mkdir -p tests/libraries
# libraries: split, then case-rename what is left
git mv Libraries libraries.tmp
for d in libraries.tmp/Sample.*; do git mv "$d" samples/; done
for d in libraries.tmp/TestFramework*; do git mv "$d" tests/libraries/; done
git mv libraries.tmp/Documentation legacy/libraries/Documentation
git mv libraries.tmp libraries
# editor support
git mv Tooling/BBEdit tools/editor-support/bbedit
```

Git moves tracked files even where the destination matches an ignore rule (`*.dll`, `[Bb]in/`); this was checked in a scratch repository at planning. If a `git mv` fails with "Permission denied", a process (an IDE, the code-search indexer, a virus scanner) holds a handle in that folder: find it, then rerun from the failed line — `set -e` stops at the failure, and the earlier moves are already staged and correct.

Afterwards remove the empty directories left on disk (`Dataphor/bin`, `Tooling`, and any other): `find Dataphor Tooling -type d -empty -delete`. Do not delete anything that is not empty.

## Edge cases & interactions

- **Case-insensitive file system.** `Libraries`→`libraries`, `Tests`→`tests`, `External`→`external` must go through `*.tmp`. Verify against the index, not `ls`: `git ls-files | grep -c -E '^(Libraries|Tests|External|Tooling)/'` prints 0, and `git ls-files | cut -d/ -f1 | sort -u` shows the lower-case names only. (Verified by the check below.)
- **Prefix families.** `Dataphor/DAE` is moved as one folder by name; `DAE.Client`, `DAE.SQLite` and the rest are separate folders and are moved by their own lines. After the moves, `git ls-files Dataphor` must list exactly the two files that stay. (Check.)
- **Untracked or ignored files inside a moved folder** move with it when Git renames the whole directory. At planning `git status --short --ignored` showed nothing outside `tickets/`; if a build left `bin/`/`obj/` folders since, they travel along harmlessly (they stay ignored). Do not clean them.
- **`git mv libraries.tmp libraries` fails if `libraries.tmp` is empty** — it is not (12 shipped libraries plus ODBCConnection remain), but if the inventory changed, the error tells you.
- **The project file rename.** `Engine.csproj` → `DAE.csproj` is the only project file renamed (D14 allows file renames; the plan names this one). Every other project file keeps its name, so the folder and the file name differ in `Store.*`, `NativeCLI`, `WindowsClient`, `ConfigurationUtility` and `DAEService`. Assembly names and namespaces are not touched.
- **Rename detection.** Exact renames are found by content hash; the check below passes `-l0` so no rename limit applies to the display.

## Verification (against the index, before handing off)

- `git diff --cached -M -l0 --name-status | cut -f1 | sort | uniq -c` prints exactly `1732 R100` and nothing else (ticket-board files are the runner's and are not staged yet).
- `git status --short --untracked-files=all` shows only `R` entries, apart from `tickets/` changes.
- `git ls-files | cut -d/ -f1 | sort -u` is: `.gitignore .gitmodules .mcp.json AGENTS.md Dataphor Dataphor.sln Dataphor.snk Documentation Images LICENSE README.md Setup Utilities book.json docs external global.json legacy libraries samples src tess tests tickets tools`.
- `git ls-files Dataphor` lists only `Dataphor/dataphor_license.txt` and `Dataphor/netsh_address_reservations.txt`.
- Per-area counts match the table: `for p in src/engine src/protocol src/client src/windows src/hosts libraries samples tests/libraries external legacy/libraries/Documentation docs/reference tools/editor-support; do echo "$p $(git ls-files $p | wc -l)"; done` (expect 398, 54, 78, 458, 22, 290, 168, 186, 9, 12, 2, 2; `tests` overall 236).
- `git ls-files src/engine/DAE/DAE.csproj` prints the file; `git ls-files '*Engine.csproj'` prints nothing outside `legacy/`.

No test is owed: the change is file renames only, and the spike comparison after the reference fixes (`repo-move-fix-references`) is the proof the plan prescribes. `git log --follow` can only be checked once the runner has committed; the next ticket does it.

## Handoff

Write the `complete/` ticket with: the baseline spike result line by line (or the difference from the expected baseline, if any), the moved-file table with measured counts, the verification output, any `git mv` retries, and a "Known transient state" list (legacy solution, project references, spike, `.gitignore`'s `Dataphor/packages`, `tickets/index-config.json`, docs — all still on old paths until the next two tickets).

## TODO

- Run the baseline spike at HEAD into `tickets/.logs/repo-move-renames.baseline.spike.log`; compare with the expected baseline; delete the work directory.
- Re-measure the inventory counts; stop on any entry not in the table.
- Run the command sequence; remove empty directories left on disk.
- Run every verification check above against the index.
- Write the handoff.
