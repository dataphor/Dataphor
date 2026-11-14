description: Make the build assemble a ready-to-run folder with the server's binaries and every library in the shape the server expects, so tests, local runs and the installer all start from the same files instead of from the source tree.
prereq: build-server-project
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.props, Directory.Build.targets, Dataphor.slnx, tests/libraries/TestFramework.Coverage.Base/Source/DAE.Diagnostics.csproj, tests/libraries/TestFramework.Coverage.Base/Source/Properties/AssemblyInfo.cs, tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs, libraries/System.Internet/System.Internet.d4l, src/engine/Server/Schema/LibraryUtility.cs, src/engine/Server/Server/Server.cs, src/engine/Server/Server/ServerConfiguration.cs, src/engine/Base.Windows/PathUtility.cs, src/engine/Base.Windows/AssemblyUtility.cs, tools/net-spike/run.py, AGENTS.md
difficulty: medium
----
# Runtime layout under artifacts/layout

## Goal

A D4 library is a folder: `<Name>.d4l`, `Documents/`, optionally `Upgrades/`, and the library's assembly with whatever it needs. In the legacy build, library projects wrote their assemblies straight into those source folders, and `Dataphor.proj` plus two batch files then assembled a `Deploy/` tree by wildcard copy. After `build-core-sdk-projects` nothing is built into source folders any more (outputs go to `artifacts/`), so nothing currently produces a folder the server can load libraries from. This ticket makes the build produce the runtime layout from the plan's WS1 target:

- `artifacts/layout/<Configuration>/bin/` — hosts and core assemblies with their runtime dependencies;
- `artifacts/layout/<Configuration>/Libraries/<Name>/` — each library's `.d4l`, `Documents/`, `Upgrades/`, its assembly and that assembly's private dependencies, recombining the shipped (`libraries/`), sample (`samples/`) and test (`tests/libraries/`) source trees into one directory.

`test-harness-inprocess` reads libraries from this layout, and later `installer-publish-profiles` builds the install image from it. It also fixes K15 for the projects still in the build.

**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout from [Repository layout](../../docs/modernization-plan.md#repository-layout) (for example `libraries/X` is today's `Libraries/X`, `tests/libraries/TestFramework*` is `Libraries/TestFramework*`, `samples/` holds `Libraries/Sample.*`). Use the plan's old → new table if needed.

## Scope

In scope:

- The mechanism that assembles `artifacts/layout/<Configuration>/` as part of `dotnet build Dataphor.slnx`, for Debug and Release.
- Every library that can be registered from the build: the converted C# libraries and the D4-only libraries listed below.
- K15: of the six projects the plan names, five (CQF.Model, FHIR.API, FHIR.Server, PHINVADS.Core, PHINVADS.Device) are parked under `legacy/` by `library-park-side-projects` and leave the build. The one still live is `TestFramework.Coverage.Base` (`tests/libraries/TestFramework.Coverage.Base/Source/DAE.Diagnostics.csproj`, Release `OutputPath` `bin\Release\` where Debug used `..\`). No other Phase 1 ticket converts this project, so this ticket converts it to SDK style with the conventions in `AGENTS.md`; that removes the per-configuration output path. Its `AssemblyInfo.cs` names a key file that does not exist (`Dataphor\KeyFile.snk`); strip the signing and version attributes the way `build-version-and-signing` describes.
- Pointing the spike's coverage run at the layout instead of the scratch library tree it builds itself, as the proof.
- A short description of the layout in the "Build and test" section of `AGENTS.md`.

Out of scope:

- Probing library folders for native assets and for managed dependencies that a `.d4l` does not list: `port-library-loading` (`feat-ws2-server-port-remainder`), which needs this layout.
- `dotnet publish` profiles, self-contained output and the install image: `installer-publish-profiles` (`feat-ws9-installer-and-release`).
- Libraries whose C# projects are not converted in Phase 1: PGSQLConnection and PGSQLDevice (`device-pgsql-port`), ODBCConnection (`device-connection-cleanup`), Sample.Shipping (`test-samples-corpus`, `feat-ws3-test-layers`). See the open choice on partial libraries.
- Registering `TestFramework.Coverage.Base` without a SQL Server (K14): `test-decouple-coverage-base`.
- The test fixture itself: `test-harness-inprocess`.

## Facts verified in the tree

**How the server finds libraries.** The instance setting `LibraryDirectories` is a semicolon-separated list. `Server.LibraryDirectory` (`src/engine/Server/Server/Server.cs` ~207–237) resolves each relative entry against `PathUtility.GetBinDirectory()` and creates missing directories. `LibraryUtility.GetAvailableLibraries` (`src/engine/Server/Schema/LibraryUtility.cs` ~70) treats each immediate subfolder holding a `<Name>.d4l` as a library. The **first** entry is the default library directory: the server creates `General/` there if no `General` library exists (`Server.cs` ~257–271) and saves changed `.d4l` files there, so a running server writes into the layout's `Libraries/` (AGENTS.md: tests must not point a server at the repository, and should not point it at a shared build output either; `test-harness-inprocess` decides whether it copies the layout).

**How library assemblies load.** `LOADFROMLIBRARIES` is `#define`d at the top of `src/engine/Server/Schema/LibraryUtility.cs`, both `Runtime.Instructions/LibraryNodes.cs` files and `src/engine/DAE/Server/ServerProcess.cs`. `RegisterLibraryFiles` (`LibraryUtility.cs` ~378–428) resolves each `<files.filereference filename="…">` of the `.d4l` against the library folder, calls `Assembly.LoadFrom` on every file that is an assembly, and registers classes (through `DAERegister` → `GetClasses()`) only from files marked `isassembly="True"`. A listed file that is missing makes `FileUtility.IsAssembly` throw `FileNotFoundException` (`src/engine/Base.Windows/PathUtility.cs` ~42–60 catches only `FileLoadException` and `BadImageFormatException`), so registration fails. The `AppDomain.AssemblyResolve` hook (`src/engine/Base.Windows/AssemblyUtility.cs`) only matches assemblies **already loaded**, by full or simple name; that is how MSSQLDevice finds SQLDevice and Connection (their libraries are required first). Nothing probes a library folder for an unlisted file.

**Default library directory when no instance setting is given.** `ServerConfiguration.DefaultInstance` and `DefaultLocalInstance` (`src/engine/Server/Server/ServerConfiguration.cs` ~133–148) use `PathUtility.GetInstallationDirectory()` + `Libraries`. `GetInstallationDirectory` (`PathUtility.cs` ~175–186) takes the base directory, steps up once more if that folder is named `bin`, then steps up once. For `artifacts/layout/<Configuration>/bin/` that gives `artifacts/layout/Libraries` — one level above the layout's `Libraries/`. The server's own fallback when `LibraryDirectory` is empty is `<bin>\Libraries` (`Server.cs` ~103–106), also not the layout's folder. The legacy `Deploy/` tree (`Dataphor.proj` ~25–59) put binaries in `Deploy\Dataphor\` and libraries in `Deploy\Libraries\`, which these rules do match.

**Libraries in the build after Phase 1, and what each folder needs** (file names from each `.d4l`):

| Library | Source tree | Files listed in `.d4l` | Private runtime dependencies |
|---|---|---|---|
| Connection | `libraries/` | `Alphora.Dataphor.DAE.Connection.dll` | `System.Data.OleDb` package (used by the compiled `OLEDBConnection.cs`; not listed in the `.d4l`) |
| SQLDevice | `libraries/` | `Alphora.Dataphor.DAE.Device.SQL.dll` | none |
| MSSQLDevice | `libraries/` | `AlphoraMSSQLDevice.dll` | none (`MSSQLConnection` and its SQL client are in the server, i.e. `bin/`) |
| SimpleDevice | `libraries/` | `Alphora.Dataphor.DAE.Device.Simple.dll` | none |
| Frontend | `libraries/` | `Alphora.Dataphor.Frontend.Server.dll` | none |
| System.Platform | `libraries/` | `System.Platform.dll` | none |
| System.Internet | `libraries/` | `System.Internet.dll`, `Newtonsoft.Json.dll` (`isassembly="False"`) | Newtonsoft.Json, which the server and NativeCLI also use from `bin/` |
| System.Integration | `libraries/` | `System.Integration.dll` | none |
| TestFramework.Coverage.Base | `tests/libraries/` | `Alphora.Dataphor.DAE.Diagnostics.dll` | references WinForms (`net10.0-windows`), used only for `Application.StartupPath` in `TestNodes.cs:751` |

D4-only (no assembly): `General`, `Security`; samples `Sample.Common`, `.Components`, `.Demo`, `.DetailAdd`, `.Forms`, `.Forum`, `.IssueTracker`, `.Pubs`, `.Rental`, `.ScriptAction`, `.SendEmail`; tests `TestFramework`, `TestFramework.Controls`, `TestFramework.Coverage`, `TestFramework.Coverage.Devices`, `TestFramework.Coverage.Scripts`, `TestFramework.StressTest`. Libraries with an unconverted C# project: `PGSQLConnection` (also lists `Npgsql.dll` and `Mono.Security.dll`, tracked under `external/`), `PGSQLDevice`, `ODBCConnection`, `Sample.Shipping` (`Alphora.Shipping.dll`).

Folder contents to copy: `<Name>.d4l`, `Documents/`, `Upgrades/` where present. Not to copy: `Source/`.

The spike's `COVERAGE_LIBS` table in `tools/net-spike/run.py` maps libraries to assemblies for the coverage run and is the starting point for the table above; its copy step (`shutil.copytree` with `ignore_patterns("Source", "*.dll", "bin", "obj")`, then one assembly copied in) is what this ticket replaces.

## Settled decisions that constrain this ticket

- Plan WS1 target: the two folder shapes above; tests, local runs and the installer consume the layout; nothing is built into source folders.
- D14 and the plan's layout rules: shipped, sample and test libraries live in separate source trees and the layout recombines them.
- Plan WS2 "Assembly loading": everything stays in the default load context and assemblies are never unloaded; the resolver for library directories is `port-library-loading`'s.
- D11: package dependencies come from NuGet through central package management; `external/` binaries stay only until replaced.

## Open choices for the planner

- **Mechanism.** (a) A shared target in `Directory.Build.targets`, keyed by a project property (for example the library name, or `bin`), that runs after `Build` and copies the project's own output and private dependencies plus its library folder into the layout; D4-only libraries then need a separate item list. (b) A separate layout project in `Dataphor.slnx` that references the library and core projects (without referencing their assemblies) and copies everything, including the D4-only libraries, in one place. Recommended default: (b), because D4-only libraries need a home anyway and a single writer avoids two projects copying the same dependency into `bin/` concurrently in a parallel build. Either way the layout must be correct after `dotnet build Dataphor.slnx` alone, incremental builds must not leave files from deleted documents behind (mirror or clean the library folders), and building one project on its own must not corrupt the layout.
- **Where library dependencies go before `port-library-loading`.** A dependency the `.d4l` does not list (Connection's `System.Data.OleDb`) is found only if the host process already has it. Options: copy it into the library folder (the plan's target) and also make sure the in-process test host carries it; or list it in the `.d4l` (a change to a shipped library file, so it needs a migration-notes entry). Default: copy into the library folder, do not edit `.d4l` files, and state in the handoff which dependencies the test host must reference until `port-library-loading` lands.
- **Newtonsoft.Json in two places.** System.Internet's `.d4l` lists `Newtonsoft.Json.dll`, so the file must exist in its folder or registration fails, and `RegisterLibraryFiles` calls `Assembly.LoadFrom` on it while the server may already have loaded the same assembly from `bin/`. Check, by registering System.Internet from the layout, that this returns the loaded assembly rather than failing; central package management guarantees the two copies are the same version. If it fails, record it for `port-library-loading` and choose the narrowest workaround.
- **Libraries with an unconverted project.** Default: leave them out of the layout until their project is converted, since registering them would fail on the missing assembly. Alternative: copy their D4 files anyway so that listing works.
- **`bin/` contents in Phase 1.** No host executable is converted yet (`host-generic-host` comes later), so `bin/` has no `.deps.json` of its own and the in-process test host references the engine projects directly. Decide how complete `bin/` must be now (core and server assemblies plus their runtime dependencies, including the SQLite native interop) versus leaving its final shape to `installer-publish-profiles`.
- **The default-instance rule.** Either accept that `GetInstallationDirectory` resolves `artifacts/layout/Libraries` and have every consumer set `LibraryDirectories` explicitly (default; no code change), or choose a layout shape the existing rule resolves correctly. Changing `PathUtility` is a deployment-visible change and out of scope here.
- **TestFramework.Coverage.Base's WinForms reference.** Keep `net10.0-windows` (the test host then needs the Windows Desktop framework, as the spike's `Cov` host has), or replace the single `Application.StartupPath` use (if that code is compiled) with `AppContext.BaseDirectory` so the library targets `net10.0`. `test-decouple-coverage-base` edits the same library later.

## What done looks like

- `dotnet build Dataphor.slnx` (Debug and Release) on a clean clone produces `artifacts/layout/<Configuration>/bin/` and `artifacts/layout/<Configuration>/Libraries/<Name>/` for every library in the table and list above, and writes nothing into source folders (`git status` clean).
- A rebuild after deleting a document from a library removes it from the layout.
- K15: `TestFramework.Coverage.Base`'s assembly lands in its layout folder in both configurations.
- The spike's coverage run, with `LibraryDirectories` pointed at a copy of the layout's `Libraries/` instead of its own scratch tree, gives the same result as after `build-server-project`; System.Internet and Connection register from the layout (add both to the registration step of the check, or register them by hand once and record the outcome).
- `AGENTS.md` "Build and test" describes the layout, which consumers use it, and that a server must be pointed at a copy of it.
- No test is owed for the copy wiring itself (project rule: no tests for wiring); the coverage run is the check. No migration-notes entry is owed unless the planner changes a `.d4l` or how the server locates libraries.

## Risks

- Parallel builds copying shared dependencies into `bin/` from several projects can fail intermittently on locked files; option (b) avoids it.
- A layout that silently misses a private dependency fails only when the class that needs it is first loaded (for Connection, when an OLE DB connection is created). The registration check above catches assemblies, not lazily loaded dependencies; list each private dependency in the handoff.
- If the harness points a server directly at the layout, the server writes `General/` into build output and later runs see it. That is `test-harness-inprocess`'s call, but the layout's description in `AGENTS.md` should say it.

If this splits, the ticket that finishes the work item keeps the slug `build-runtime-layout`; earlier pieces get new slugs and are chained to it with `prereq:`.
