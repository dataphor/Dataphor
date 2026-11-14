description: Moved every remaining product folder, library, sample, test and third-party binary into the repository's new folder structure using pure file renames with no content changes, so that each file's history follows it; the paths that point at the old locations are fixed by the two tickets after this one.
architecture: docs/modernization-plan.md#repository-layout
files: src/, libraries/, samples/, tests/, tests/libraries/, external/, docs/reference/, tools/editor-support/bbedit/, legacy/libraries/Documentation/, LICENSE, Dataphor.sln
difficulty: easy
review: skip
----
# Move to the new layout — the rename-only step (done)

First of three tickets for the work item `repo-move-to-new-layout` (plan decision D14). Only `git mv` operations were made; no file content was edited. The next ticket, `repo-move-fix-references`, rewrites paths inside project files, the legacy solution, `.gitignore`, `tickets/index-config.json` and the spike; `repo-move-to-new-layout` rewrites the documentation.

## Baseline spike (before any move, at HEAD 6e8f85ca)

Log: `tickets/.logs/repo-move-renames.baseline.spike.log` (work directory was in the scratchpad and has been deleted). Matches the expected baseline exactly:

- Smoke: server started on .NET 10.0.15, all smoke expressions as expected, server stopped cleanly.
- `Concurrency: 8 threads x 15 iterations of DDL+query … 0 errors`
- `Resolution cache: 8 threads x 1000 ms of add/clear; 0 errors`
- `SUMMARY pass=62 fail=14 timeout=0 skipped-ignored=13`
- Engine, server and library projects 0 errors. Non-zero compile counts only: Dataphoria 1 (CS0115 `ContextMenu`), DAEServer 4, DAE.Service 7, D4Runner 1 (NU1201), DAE.Server.Tests 1 (NU1201), Common.Tests 15 (NUnit missing), DAE.Client.Tests 1 (NU1201).

## Moves (measured counts, all matched the planning inventory)

| From | To | Files |
|---|---|---|
| `Dataphor/Base`, `Base.Windows`, `Server`, `DAE` (+ `Engine.csproj` → `DAE.csproj`), `DAE.SQLite`/`MSSQL`/`PGSQL` | `src/engine/Base`, `Base.Windows`, `Server`, `DAE`, `Store.SQLite`/`Store.MSSQL`/`Store.PGSQL` | 398 |
| `Dataphor/Common`, `Client`, `DAE.NativeCLI` | `src/protocol/Common`, `Client`, `NativeCLI` | 54 |
| `Dataphor/DAE.Client`, `DAE.Client.Provider`, `Frontend.Client` | `src/client/` same names | 78 |
| `Dataphor/DAE.Client.Controls`, `Frontend.Client.Windows`, `Frontend.Client.WPF`, `Dataphoria` (189 after its 4 DLLs left), `Frontend.Client.Windows.Executable`, `DAE.Service.ConfigurationUtility` | `src/windows/` …, `WindowsClient`, `ConfigurationUtility` | 458 |
| `Dataphor/DAEServer`, `DAE.Service`, `D4Runner` | `src/hosts/DAEServer`, `DAEService`, `D4Runner` | 22 |
| `Dataphor/.editorconfig` | `src/.editorconfig` | 1 |
| `Dataphor/Dataphor.sln`, `dataphor_license.txt` | `Dataphor.sln`, `LICENSE` | 2 |
| `Dataphor/CompilerDefines.txt`, `Error Message Prefix Codes.txt` | `docs/reference/` | 2 |
| `External/` (2) + `Dataphor/bin` DLLs (3) + Dataphoria DLLs (4) | `external/` | 9 |
| `Tests/` | `tests/` | 50 |
| `Libraries/` 13 shipped libraries incl. ODBCConnection | `libraries/` | 290 |
| `Libraries/Sample.*` (12) | `samples/` | 168 |
| `Libraries/TestFramework*` (7) | `tests/libraries/` | 186 |
| `Libraries/Documentation` | `legacy/libraries/Documentation` | 12 |
| `Tooling/BBEdit` | `tools/editor-support/bbedit` | 2 |

No `git mv` failed; no retries. Empty directories `Dataphor/bin` and `Tooling` removed from disk. On-disk folder names are lower-case (`external`, `libraries`, `tests`).

## Verification output (against the index)

- `git diff --cached -M -l0 --name-status | cut -f1 | sort | uniq -c` → `1732 R100`, nothing else.
- `git status --short --untracked-files=all` → only `R` entries outside `tickets/`.
- `git ls-files | cut -d/ -f1 | sort -u` → `.gitignore .gitmodules .mcp.json AGENTS.md Dataphor Dataphor.sln Dataphor.snk Documentation Images LICENSE README.md Setup Utilities book.json docs external global.json legacy libraries samples src tess tests tickets tools`.
- `git ls-files Dataphor` → `Dataphor/dataphor_license.txt`, `Dataphor/netsh_address_reservations.txt`.
- Area counts: src/engine 398, src/protocol 54, src/client 78, src/windows 458, src/hosts 22, libraries 290, samples 168, tests/libraries 186, external 9, legacy/libraries/Documentation 12, docs/reference 2, tools/editor-support 2; tests overall 236; src overall 1011.
- `git ls-files src/engine/DAE/DAE.csproj` prints the file; `git ls-files '*Engine.csproj'` prints nothing.
- `git ls-files | grep -c -E '^(Libraries|Tests|External|Tooling)/'` → 0.

No test added: renames only. The spike comparison after `repo-move-fix-references` is the proof; `git log --follow` is checked there once this is committed.

## Known transient state (fixed by the next two tickets)

- `Dataphor.sln` (now at the root) still references `Dataphor\…` project paths.
- Every project file's cross-folder `ProjectReference`/`HintPath`/linked-source paths point at old locations (including `Engine.csproj` by name, and DLLs formerly in `Dataphor/bin` and `Dataphor/Dataphoria`).
- `tools/net-spike/run.py` still uses old paths — the spike will not run until `repo-move-fix-references`.
- `.gitignore`'s `Dataphor/packages` rule and any other old-path rules.
- `tickets/index-config.json` code-search include/exclude paths.
- All docs (`AGENTS.md`, `docs/architecture.md`, plan, READMEs) still describe the old layout.
- `Dataphor/dataphor_license.txt` (duplicate licence) and `tests/Dataphor.Tests.nunit` remain for the next ticket to delete; `Dataphor/netsh_address_reservations.txt` waits on `hygiene-large-binaries`.
