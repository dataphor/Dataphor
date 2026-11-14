description: Reorganize the repository into its new folder structure (product code, shipped libraries, samples, tests, documentation, tools and third-party binaries each in one clearly named place) using pure file moves that keep every file's history, then fix every path that pointed at the old locations.
prereq: repo-delete-dead-trees, library-remove-dead-devices, library-park-side-projects
architecture: docs/modernization-plan.md#repository-layout
files: Dataphor/, Libraries/, Tests/, External/, Tooling/, Images/, Dataphor/Dataphor.sln, Dataphor/.editorconfig, Dataphor/bin/, Dataphor/Dataphoria/, Dataphor/CompilerDefines.txt, Dataphor/Error Message Prefix Codes.txt, dataphor_license.txt, Dataphor/dataphor_license.txt, Tests/Dataphor.Tests.nunit, Tests/Frontend.Client.WPF.Tests/, tools/net-spike/projects.txt, tools/net-spike/run.py, tools/net-spike/Smoke/Smoke.csproj.txt, tools/net-spike/Cov/Cov.csproj.txt, tools/net-spike/README.md, AGENTS.md, README.md, docs/architecture.md, docs/modernization-plan.md, docs/migration-notes.md, tickets/index-config.json, .gitignore
difficulty: hard
----
# Move to the new repository layout

## Goal

The repository's layout is historical: product code under `Dataphor/`, shipped libraries mixed with samples and test libraries under `Libraries/`, binaries in several places. D14 settles that it is reorganized to the target in the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout) section **before** any project is converted, so every later ticket works in the final structure once, and so that `git log --follow` and `git blame` keep working across the move. Every Phase 1 ticket after this one already writes its paths in the target layout (`tickets/rules/10-dataphor.md`, "Paths after the reorganization").

This is a planning ticket because the move needs decisions the plan leaves open: several entries in the tree have no row in the plan's old → new table, and the work must be split into rename-only commits followed by reference-fix commits in an order that keeps the spike usable as proof.

## Settled decisions and rules

- D14: reorganize first, by pure `git mv` commits; assembly names, namespaces and D4 library names do not change; only directories and project file names do.
- The plan's "Rules for the move" (quoted here because they are the acceptance criteria): delete and park first (the three prereqs do that); a move commit contains **only renames**, no content edits, so Git records exact renames; reference fixes go in the commits after it; case-only renames (`Libraries` → `libraries`, `Tests` → `tests`, and also `External` → `external`) need two steps on Windows through a temporary name; proof that the move changed nothing is the spike giving the same results before and after, with its path tables updated in the reference-fix commit; the same ticket updates `AGENTS.md`, `README.md`, `docs/architecture.md`, `tickets/index-config.json` and the paths cited in the plan.
- D12: no history rewrite. D11 and `debt-ws12-hygiene-remainder` own replacing the binaries that land in `external/`; this ticket only moves them.
- Source hygiene: moving and editing a file are separate commits; edited files keep tabs, CRLF and their byte-order mark state.
- The server finds libraries as the immediate subfolders of each directory in `LibraryDirectories` (semicolon-separated), so splitting `Libraries/` into `libraries/`, `samples/` and `tests/libraries/` is safe at run time; the runtime layout (`build-runtime-layout`) recombines them.

No known defect is fixed here.

## Inventory after the three prereqs (verified against today's tree)

What each prereq removes or parks is in its own ticket. What is left, and where the plan's table sends it:

**Mapped by the plan's table** (move as the table says):

- `Dataphor/Base`, `Base.Windows`, `Server` → `src/engine/…`; `Dataphor/DAE` (`Engine.csproj`) → `src/engine/DAE` (`DAE.csproj`); `Dataphor/DAE.SQLite`, `DAE.MSSQL`, `DAE.PGSQL` → `src/engine/Store.SQLite`, `Store.MSSQL`, `Store.PGSQL`.
- `Dataphor/Common`, `Client`, `DAE.NativeCLI` → `src/protocol/Common`, `Client`, `NativeCLI`.
- `Dataphor/DAE.Client`, `DAE.Client.Provider`, `Frontend.Client` → `src/client/…`.
- `Dataphor/DAE.Client.Controls`, `Frontend.Client.Windows`, `Frontend.Client.WPF`, `Dataphoria` → `src/windows/…`; `Frontend.Client.Windows.Executable` → `src/windows/WindowsClient`; `DAE.Service.ConfigurationUtility` → `src/windows/ConfigurationUtility`.
- `Dataphor/DAEServer`, `DAE.Service`, `D4Runner` → `src/hosts/DAEServer`, `DAEService`, `D4Runner`.
- `Dataphor/CompilerDefines.txt`, `Dataphor/Error Message Prefix Codes.txt` → `docs/reference/` (the table marks this "(WS8)"; since `Dataphor/` is being emptied, the default is to move them here, unchanged, and let WS8 convert them).
- `Libraries/` shipped set → `libraries/<Name>`: `Connection`, `SQLDevice`, `MSSQLDevice`, `PGSQLDevice`, `PGSQLConnection`, `SimpleDevice`, `Frontend`, `Security`, `General`, `System.Platform`, `System.Internet`, `System.Integration` (all twelve exist).
- `Libraries/Sample.*` → `samples/`: `Sample.Common`, `Sample.Components`, `Sample.Demo`, `Sample.DetailAdd`, `Sample.Forms`, `Sample.Forum`, `Sample.IssueTracker`, `Sample.Pubs`, `Sample.Rental`, `Sample.ScriptAction`, `Sample.SendEmail`, `Sample.Shipping` (12; `Sample.Shipping` has C# in `Source/Shipping.csproj`).
- `Libraries/TestFramework*` → `tests/libraries/`: `TestFramework`, `TestFramework.Controls`, `TestFramework.Coverage`, `TestFramework.Coverage.Base`, `TestFramework.Coverage.Devices`, `TestFramework.Coverage.Scripts`, `TestFramework.StressTest` (7).
- `Libraries/Documentation` (D4 operator that generates DocBook reference pages, with `Source/DocSamples.csproj` and `DocSamples.sln`) → `legacy/libraries/Documentation` per the table (not parked by `library-park-side-projects`, whose assignment did not include it).
- `Tests/Common.Tests`, `Tests/DAE.Server.Tests`, `Tests/DAE.Client.Tests` → `tests/…`.
- `External/` → `external/`; `Dataphor/bin/` (after the SQL CE DLL is deleted: `Mono.Security.dll`, `Npgsql.dll`, `System.Data.SQLite.DLL`) → `external/`; `Dataphor/Dataphoria/*.dll` (`Ascend.dll`, `Ascend.Resources.dll`, `Ascend.Windows.Forms.dll`, `WeifenLuo.WinFormsUI.Docking.dll`) → `external/`.
- `Tooling/BBEdit` → `tools/editor-support/bbedit`.
- Unchanged in place: `Documentation/` (WS8), `Setup/` and `Utilities/Wix/` (WS9; `Setup/Setup.wixproj` reaches `..\Utilities\Wix\3.5\` by relative path, so they must stay together), `tess/` (git submodule), `tickets/`, `docs/`, `tools/`, `global.json`, `AGENTS.md`, `README.md`, `.gitignore`, `.gitmodules`, `.mcp.json`.

**Not accounted for by the table — the planner settles each** (defaults in brackets):

| Entry | What it is | Default |
|---|---|---|
| `Dataphor/Dataphor.sln` | the legacy solution, kept until `build-remove-legacy-build` (in `debt-ws12-hygiene-remainder`); lists every live project by relative path, plus solution items `.editorconfig`, `CompilerDefines.txt`, `Error Message Prefix Codes.txt`, `..\Tests\Dataphor.Tests.nunit` | [move to the repository root as `Dataphor.sln` and rewrite its paths in the reference-fix commit; it coexists with the future `Dataphor.slnx`] |
| `Dataphor/.editorconfig` | applies today to everything under `Dataphor/`; its settings disagree with the tree in places (`hygiene-gitattributes-editorconfig` measured this) | [move to `src/.editorconfig` so it still covers the same files; `hygiene-gitattributes-editorconfig` replaces it with one root file and deletes it] |
| `Dataphor/dataphor_license.txt` | byte-identical copy of the root `dataphor_license.txt` (checked with `cmp`) | [delete in a separate delete commit] |
| `dataphor_license.txt` (root) | the licence; the target root lists `LICENSE`; `README.md` links it; source headers cite the URL `http://dataphor.org/dataphor_license.txt`, which is not a repository path | [rename to `LICENSE` and fix the README link; or keep the name — either is fine, but decide] |
| `Dataphor.snk` (root) | signing key (D7) | [stays at the root] |
| `book.json` (root) | GitBook configuration for `Documentation/` | [stays; `docs-convert-asciidoc` reads it and `docs-remove-old-sources` (in `feat-ws8-documentation-remainder`) deletes it] |
| `Dataphor/netsh_address_reservations.txt` | K21; no reference anywhere | [not moved; `hygiene-large-binaries` deletes it at this path] |
| `Images/` | 228 files, 11.2 MB of source art and icon masters; **nothing in the repository references any file under it by path**, and the projects embed their own copies | [not moved; `hygiene-large-binaries` deletes the unreferenced files and moves anything it finds referenced to `assets/` in its own rename-only commit. `assets/` is therefore not created by this ticket] |
| `Libraries/ODBCConnection` | ODBC connection (`Alphora.Dataphor.DAE.Connection.ODBC`), in `Dataphor.sln`. The layout table sends ODBC to `legacy/libraries/`; WS7 says "convert if it is a project-file change only, otherwise park", and `device-connection-cleanup` (in `feat-ws7-devices-and-libraries`) owns that decision | [`libraries/ODBCConnection`, leaving the convert-or-park call to `device-connection-cleanup`; report the conflict in the plan] |
| `Tests/Frontend.Client.WPF.Tests` | a manual WPF test application for the one WPF scheduler control (`Window1.xaml`, `Window2.xaml`, a vendored `WPFToolkit.dll`); in no solution | [`tests/Frontend.Client.WPF.Tests`; WS6 decides its future] |
| `Tests/Dataphor.Tests.nunit` | an NUnit 2 GUI project file listing `bin\Debug` test assemblies | [delete, and its solution-item line] |
| `Legacy/Documentation/` | DocBook sources (WS8) | [whatever `library-park-side-projects` did: its default renames `Legacy/` → `legacy/` because `legacy/` and `Legacy/` collide on case-insensitive file systems. Check `git ls-files | cut -d/ -f1 | sort -u` before planning; do not rename it again] |
| `Dataphor/packages/` | git-ignored NuGet restore folder for `packages.config` projects (`.gitignore` line `Dataphor/packages`); `HintPath`s in `DAE.NativeCLI`, `Server`, `System.Internet` and the three test projects point into it | [follows the solution: restore goes next to the moved `.sln`; update those `HintPath`s and the `.gitignore` line] |

Re-run the inventory before planning (`git ls-files | cut -d/ -f1,2 | sort -u` for the top two levels of `Dataphor/`, `Libraries/` and `Tests/`); the prereqs may have landed differently from their defaults, and any entry not covered above or in the table goes into this list.

## What holds old paths (the reference-fix commits must update)

- **Legacy project files** (`*.csproj` of every live project): `ProjectReference Include="..\..\…"`, `HintPath` to `Dataphor\bin`, `External\`, `Dataphor\packages\`, the Dataphoria folder's own DLLs, and `Content`/`None` items with relative paths. Count them with `git grep -c -E '(ProjectReference Include|HintPath)' -- '*.csproj'` before and after. Library projects' `OutputPath ..\` is relative to the project and moves with it; the core projects' `OutputPath ..\bin\` will point at `src/<area>/bin/` — the legacy build is not used any more, so record this rather than redesign it (`build-core-sdk-projects` replaces output locations).
- **`AssemblyInfo.cs` signing paths** (`AssemblyKeyFile("..\\..\\..\\..\\Dataphor.snk")` under `#if SIGNASSEMBLIES`, 60 files): the relative depth changes. Do not edit them here; `build-core-sdk-projects` and `build-version-and-signing` move signing into the props file. Note it in the handoff.
- **`Dataphor.sln`** (wherever it goes): every project path and the solution items; remove the `DocSamples` entry when `Libraries/Documentation` is parked.
- **The spike**, which must work after the move because it is the proof:
  - `tools/net-spike/projects.txt` — every line.
  - `tools/net-spike/run.py` — `PATCHES` paths (`Dataphor\Common\WCF\ServiceClient.cs`, `Dataphor\Client\Client\ClientServer.cs`, `Dataphor\Base\ThreadUtility.cs`, `Dataphor\DAE.SQLite\SQLiteConnection.cs`, `Dataphor\Server\Device.Catalog\ServerCatalogDeviceSession.cs`, `Libraries\Frontend\Source\Device\FrontendDevice.cs`, `Libraries\Connection\Source\Register.cs`); `STUBS` keys, which are wrapper names taken from project file names (`"Engine"` becomes `"DAE"` when `Engine.csproj` is renamed); `COVERAGE_LIBS` values (wrapper names, which change if a project file is renamed); the coverage copy step, which copies every library from `REPO/Libraries/<lib>` and must now copy `Frontend`, `SimpleDevice`, `SQLDevice`, `MSSQLDevice`, `Connection` from `libraries/` and `TestFramework`, `TestFramework.Coverage.Base`, `TestFramework.Coverage.Scripts` from `tests/libraries/`; and the `Tests/DAE.Server.Tests/CoverageTest.cs` path passed to the coverage harness.
  - `tools/net-spike/Smoke/Smoke.csproj.txt` and `Cov/Cov.csproj.txt` — `ProjectReference` to `..\Server\Server.csproj` and `..\DAE.SQLite\DAE.SQLite.csproj`, which are wrapper names: they change if those project files are renamed.
  - `tools/net-spike/convert.py` takes paths from `projects.txt` and resolves each project's references relative to the project, so it needs no change once the project files are fixed; confirm.
  - `tools/net-spike/README.md`.
- **`AGENTS.md`**: the "Layout" table **must be rewritten by this ticket** (its own text says so: "the ticket that performs the move rewrites this table"); also the "Build and test" paths, and the rules that cite paths (`Dataphor/Error Message Prefix Codes.txt`, `Dataphor/CompilerDefines.txt`, `Dataphor/Server/Contracts` + `Dataphor/Client/Contracts`, `DAE.Client/View-263.cs`, `Libraries/Connection/Source`, "same-named files in both `Dataphor/DAE` and `Dataphor/Server`", the library-directory warning).
- **`README.md`**: the link to the sample libraries (`Libraries/` → `samples/`), the sentence naming `Dataphor/Dataphor.sln`, and the licence link if renamed. Its links into `Documentation/` stay (WS8 repoints them).
- **`docs/architecture.md`**: the assemblies table, the "On disk" table, the "What is alive" table and every inline path.
- **`docs/modernization-plan.md`**: the known-defects table "Where" column, Appendix A and B paths, the layout section (mark the move done; record the decisions taken on the unmapped entries), and any other inline path. Keep historical statements historical (Appendix A describes a run made on the old layout; say so rather than rewriting its results).
- **`docs/migration-notes.md`**: the "Building from source" row — split so that "The repository is reorganized" is **done** while "builds with `dotnet build`; old solutions are gone" stays **planned**.
- **`tickets/index-config.json`**: rewrite the `exclude` list for the new paths (drop entries for deleted paths; keep the noisy-data exclusions such as `tests/libraries/TestFramework.Coverage.Devices/Documents/`; exclude `legacy/`, `external/`, `Images/`).
- **`.gitignore`**: `Dataphor/packages`, and any line that named a moved path.
- **Run-time relative paths in code**: `Dataphor/DAE/Runtime.Instructions/TestNodes.cs` lines 584–598 build paths such as `..\..\..\Base\bin\{0}\Alphora.Dataphor.dll` for an old exception-resource test operator. These are strings evaluated at run time against legacy build output, already wrong today; do not edit them in this ticket (code edits are out of scope); note them for `port-engine-runtime-fixes` or the test triage. `Tests/DAE.Server.Tests/Utilities/ServerConfigurationManager.cs:57` uses the *installation* directory plus `Libraries`, not a repository path; no change.
- **D4 scripts and configuration**: no live D4 script or `.config` file contains a repository-relative path (searched `Libraries/**/*.d4`, `*.config` for `..\`, `Libraries\`, `Dataphor\bin`); `Libraries/Documentation/Documents/DocLibraries.d4` uses a documentation root variable but is parked. Re-check after the prereqs.
- **Other tickets on the board** already use target-layout paths; nothing to change there, except that their prose may name a project file (`Engine.csproj`) whose new name this ticket sets: record every project file rename in the plan's layout section so readers can map them.

## Open choices for the planner

1. **Project file renames.** The table renames `Engine.csproj` to `DAE.csproj` and moves the store folders to `Store.*`; it does not say whether `DAE.SQLite.csproj` becomes `Store.SQLite.csproj`, whether `Frontend.Client.Windows.Executable.csproj` becomes `WindowsClient.csproj`, and so on. A rename of the file only (assembly name unchanged) is allowed by D14. Default: rename a project file only where the table renames it explicitly (`Engine.csproj` → `DAE.csproj`), keeping every other project file name, which keeps the spike's wrapper names and `build-core-sdk-projects`' expectations stable. Record the result in the plan.
2. **Commit split.** Rename-only commits must contain nothing else; a sensible split is one rename commit per target area (`src/engine`, `src/protocol`, `src/client`, `src/windows`, `src/hosts`, `libraries` + `samples` + `tests/libraries`, `tests`, `external`, `tools`, `docs/reference`, plus the delete commit for duplicates), then one reference-fix commit for project files and the solution, then one for the spike, then one for docs and configuration. The runner commits once per ticket stage, so if the planner wants separate commits it must either have the implementing agent make them itself (allowed: the commits are part of the work) or split the work into several implement tickets chained by `prereq:`. Decide which, and say how each rename commit is verified: `git diff --cached -M100% --name-status` (or `git show --stat -M` after committing) shows only `R100` lines.
3. **Case-only renames.** `Libraries` is not a pure case rename because it splits into four destinations, and on a case-insensitive file system `libraries/` *is* `Libraries/`. Default sequence: `git mv Libraries libraries.tmp`; move samples, test libraries and the parked documentation library out of `libraries.tmp`; `git mv libraries.tmp libraries`. Same pattern for `Tests` → `tests.tmp` → `tests` (with `tests/libraries/` added) and `External` → `external.tmp` → `external` (with the `Dataphor/bin` and Dataphoria DLLs added). Verify with `git ls-files | cut -d/ -f1 | sort -u` that no old-case directory remains.
4. **Defaults in the unaccounted table** above.

## Done looks like

- `git ls-files | cut -d/ -f1 | sort -u` lists only the target top-level entries (`src`, `libraries`, `samples`, `tests`, `docs`, `tools`, `external`, `legacy`, `tess`, `tickets`) plus the root files, plus the entries deliberately left in place (`Documentation`, `Setup`, `Utilities`, `Images`, and `Dataphor` holding only `netsh_address_reservations.txt` until `hygiene-large-binaries`).
- Every move commit is rename-only (`R100`); `git log --follow` on a sample of moved files (one per area) shows pre-move history.
- The spike, run before the move and after the reference fixes with `python tools/net-spike/run.py <work-dir> --smoke --coverage` (work dir outside the repository), gives the same results: engine, server and libraries 0 errors, concurrency 0 errors, coverage `pass=62 fail=14` with the same failing scripts, and the same per-project error summaries for the projects that already fail.
- `git grep -n -I -E "Dataphor[\\/](Base|DAE|Server|Common|Client|Frontend|Dataphoria|bin)|Libraries[\\/]|Tests[\\/]|External[\\/]|Tooling[\\/]"` finds old paths only in historical text (the plan's Appendix A, explicitly marked), `Setup/`, `Documentation/`, `legacy/`, and `TestNodes.cs`' run-time strings. Put the trimmed output in the handoff.
- `AGENTS.md`'s layout table describes the new tree, and its "Build and test" section and rules cite new paths.
- No test is owed: moves change no behaviour, and the spike is the proof the plan prescribes. Embedded-resource manifest names are unaffected because legacy MSBuild derives them from the root namespace and the path inside the project, not from the repository path; the coverage run confirms it.

## Risks

- A relative path that only matters at run time (a library loading a file next to itself, a test reading a fixture by relative path) breaks silently. Mitigation: the spike's coverage run exercises library registration; the `git grep` above; reading every `Path.Combine` with `".."` in live code once.
- Case handling on Windows recording paths in a mixed case. Mitigation: check `git ls-files`, not the file system.
- Concurrent work: the plan says nothing else should be in flight while paths move. The runner orders this ticket after its prereqs; the planner should state in the emitted tickets that they must run with no other ticket in progress.
- A huge single commit that hides a content edit among renames. Mitigation: the `R100` check per commit.

If this splits, the ticket that finishes the work item keeps the slug `repo-move-to-new-layout`; earlier pieces get new slugs and are chained to it with `prereq:`.
