description: The build now assembles a ready-to-run folder with the server's binaries and every library in the shape the server expects, so tests, local runs and the installer can start from the same files instead of from the source tree. Review the mechanism, its checks and the spike's switch to it.
prereq: build-server-project
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.targets (new), tools/layout/Layout.csproj (new), Dataphor.slnx, tests/libraries/TestFramework.Coverage.Base/Source/DAE.Diagnostics.csproj, tests/libraries/TestFramework.Coverage.Base/Source/Properties/AssemblyInfo.cs, tools/net-spike/run.py, tools/net-spike/Cov/Program.cs, tools/net-spike/README.md, tools/net-spike/EngineCheck/EngineCheck.csproj, tools/net-spike/EngineCheck/Program.cs, AGENTS.md, docs/modernization-plan.md, docs/migration-notes.md, tickets/plan/10-test-harness-inprocess.md, tickets/backlog/feat-ws2-server-port-remainder.md
difficulty: medium
----
# Runtime layout under artifacts/layout — review handoff

## What was built

`dotnet build Dataphor.slnx` (Debug or Release) now also writes:

```
artifacts/layout/<debug|release>/
  bin/                 core and server assemblies + package runtime files, SDK copy-local shape (no host)
  Libraries/<Name>/    tracked library files + the library's assembly/.pdb + its private package files (flattened, Windows RID)
```

- **`Directory.Build.targets`** (new, root; no BOM, CRLF like `Directory.Build.props`):
  - `DataphorLayoutDir` = `NormalizeDirectory(ArtifactsPath, 'layout', Configuration.ToLowerInvariant())`.
  - `_DataphorCollectPackageFiles`: `RuntimeCopyLocalItems`, `RuntimeTargetsCopyLocalItems`, `NativeCopyLocalItems` (minus the apphost) and `ResourceCopyLocalItems`. It keeps `CopyLocal=true` items only and removes `@(_ConflictPackageFiles)`, the way the SDK's `ResolveLockFileCopyLocalFiles` and `GenerateBuildDependencyFile` do. It depends on `ResolvePackageAssets;ResolveAssemblyReferences`, so the SDK's conflict resolution has already run. No project has a conflict today; I checked with `-t:_HandlePackageFileConflicts -getItem:_ConflictPackageFiles` on Server, Connection, Client and DAE.SQLite.
  - `GetDataphorPackageIds`: the package ids in a project's closure.
  - `GetDataphorLayoutItems`: returns `Output` items (`$(TargetPath)`, the `.pdb` from `DebugSymbolsProjectOutputGroupOutput`, satellites from `SatelliteDllsProjectOutputGroup`) and `Package` items. Each item carries `DataphorLayoutPath` (`DestinationSubPath`, or the file name when that is empty), plus `DataphorPrivate` and `DataphorLibrary`. `DataphorPrivate` is computed with `Remove … MatchOnMetadata="NuGetPackageId"` against the ids returned by the project's references. Those references are called with the same `SetConfiguration`/`SetPlatform`/`SetTargetFramework`/`GlobalPropertiesToRemove` handling as `GetCopyToOutputDirectoryItems`.
- **`tools/layout/Layout.csproj`** (new, in `Dataphor.slnx` under `/tools/`): plain SDK class library, no Compile items, `DisableFastUpToDateCheck=true` (Visual Studio must always call it, because D4 documents are not project inputs). Project references are `ReferenceOutputAssembly=false`/`Private=false` (item definition), with `DataphorLayoutRole` Bin (8 projects) or Library (9). The D4 library list is a static glob of `*\*.d4l` in the three trees, with four `Remove` lines, each commented with its owner. The targets:
  - `_DataphorCheckLayoutDir` is the deletion guard. The normalized `DataphorLayoutDir` must lie strictly inside `<ArtifactsPath>\layout\`. Every write and delete uses that normalized path.
  - `_DataphorGetLayoutItems` makes one `MSBuild` call per role.
  - `_DataphorListLibraries` checks that `<Name>.d4l` sits in `<Name>\`, and that no two trees have a folder with the same name.
  - `_DataphorComputeLayout`:
    - bin destinations;
    - library source globs, excluding `Source\**`, `bin\**`, `obj\**`, `**\*.dll`, `**\*.pdb` and `**\*.exe`;
    - library outputs, and private packages with RID ranking win-x64 > win > generic (`Remove … MatchOnMetadata="DataphorDestination"`);
    - errors: a library project outside a `Source` folder, a library project whose folder is excluded, a private native asset;
    - the duplicate-destination check (`Metadata('DataphorSource')->Distinct()->Count()`), then `RemoveDuplicates`.
  - `_DataphorReadLibraryFileList` is batched per library and runs `XmlPeek` over `files.filereference/@filename`. `_DataphorCheckLibraryFiles` raises one error that lists every missing file.
  - `AssembleDataphorLayout` (`AfterTargets="Build"`): `Copy` with `SkipUnchangedFiles`, no links; deletes stale files; removes stray `Libraries\<X>` folders.
  - `CleanDataphorLayout` (`AfterTargets="Clean"`).
- **K15 / TestFramework.Coverage.Base** converted to SDK style. The project targets plain `net10.0`, constants are `TRACE` (Debug: `DEBUG;TRACE`), and it references Base, DAE and Server (Base.Windows was not needed). `AssemblyInfo.cs` is stripped to title, description and `DAERegister`, with its BOM and CRLF kept. Build: 0 errors, 8 warnings (CA2022 ×6, SYSLIB0051 ×2), none suppressed.
- **Spike**:
  - `run.py --coverage` builds `Layout.csproj` (logged as `Layout.log` and summarized like the other builds), asks MSBuild for `DataphorLayoutDir`, and copies 9 library folders from `<layout>\Libraries`. The 9 are the previous 8 plus `System.Internet`. `COVERAGE_LIBS` is now a list of names.
  - The `CONVERTED` branch of `find_dll` is removed: after this change only the Smoke and Cov wrappers use `find_dll`.
  - `Cov/Program.cs` has a `Register` helper. After the summary line (and after the timeout exit, so a hung script cannot hang the registrations) it registers `Connection` and `System.Internet`.
  - EngineCheck lists `Alphora.Dataphor.DAE.Diagnostics` with no resources.
- **Docs**:
  - AGENTS.md: a "Runtime layout" paragraph, the converted list, the spike expectations (13 wrappers, the two `REGISTERED` lines), a new "Converting another project" bullet, and the "server writes into its library directory" rule now pointing at a copy of the layout's `Libraries/`.
  - Plan: a WS1 design note, the `build-runtime-layout` row marked **Done**, and K15 marked fixed.
  - `docs/migration-notes.md`: the existing version/signing note now says seventeen projects and names the test helper.
  - Context notes appended to `test-harness-inprocess` and to `port-library-loading` in `feat-ws2-server-port-remainder`.

## Layout produced (Debug and Release identical: 545 files each)

- `bin/`, 30 files at the root:
  - the Dataphor assemblies with their `.pdb`s: `Alphora.Dataphor`, `.Windows`, `.DAE`, `.DAE.SQLite`, `.DAE.NativeCLI`, `AlphoraDataphorServer`, `AlphoraDataphorCommon`, `AlphoraDataphorClient`;
  - packages: `Microsoft.Extensions.ObjectPool`, `Newtonsoft.Json`, `System.Data.SQLite`, `System.Data.SqlClient`, `System.Diagnostics.EventLog`, `System.Security.Cryptography.Pkcs` and `.Xml`, and six `System.ServiceModel.*` files;
  - 13 culture folders × 4 ServiceModel resource assemblies;
  - `runtimes/` with 12 files: SQLite.Interop for linux-x64, osx-x64, win-x64 and win-x86; `sni.dll` for win-arm64, win-x64 and win-x86; the win and unix `System.Data.SqlClient.dll`; the win EventLog (+ Messages) and Pkcs.
- `Libraries/` holds 28 folders. Every one equals its tracked files minus `Source/`, checked by diffing `git ls-files` against the layout. Built files per library:
  - Connection: `Alphora.Dataphor.DAE.Connection.dll` and `.pdb`, `System.Data.OleDb.dll`, `System.Diagnostics.PerformanceCounter.dll`, `System.Configuration.ConfigurationManager.dll`, `System.Security.Cryptography.ProtectedData.dll`.
  - System.Internet: `System.Internet.dll` and `.pdb`, `Newtonsoft.Json.dll`.
  - Frontend, MSSQLDevice, SQLDevice, SimpleDevice, System.Platform, System.Integration, TestFramework.Coverage.Base: their own `.dll` and `.pdb` only.
  - General, Security, the 11 included samples and the other 6 test libraries: tracked files only.
- Private dependencies match the planning expectations exactly. No differences.

## Verification run (all by hand; results)

- **Clean clone, both configurations.** I deleted `artifacts/layout` (never `git clean`), then built Debug and Release: 0 errors, both layouts present, the same file list in each. `git status` shows only this ticket's files.
- **Stale files.** I added `Libraries\Connection\Documents\Stray.d4`, a `Libraries\Stray\` folder (`Stray.d4l` + `Documents\A.d4`) and `bin\stray.txt`, and appended text to the layout's `Connection.d4l`. After a rebuild, all three strays were gone and `Connection.d4l` was byte-identical to the source (`cmp`).
- **Single project.** `dotnet build libraries/Connection/Source/DAE.Connection.csproj` left every layout file's timestamp unchanged.
- **No double builds.** I built with `-v n` and `-v d` and parsed the `is building "<proj>" (<id>)` lines: each of the 18 projects has exactly one configuration id. In the `-v d` log, the `GetDataphorLayoutItems` and `GetDataphorPackageIds` calls reach those same ids. Note that a library's calls also reach its transitive references (Connection → Base.Windows, Common, NativeCLI), because the SDK adds transitive project references to `_MSBuildProjectReferenceExistent`. This matches the build's own calls.
- **RID choice.** SHA-1 of `Libraries\Connection\System.Data.OleDb.dll` and `System.Diagnostics.PerformanceCounter.dll` equals the `runtimes\win\lib\net10.0\` files of the 10.0.15 packages, not the `lib\net10.0\` stubs.
- **`.d4l` check.** With System.Internet temporarily removed from the Library list, the build failed with: "The layout does not provide files that a .d4l lists: System.Internet.dll (listed in …System.Internet.d4l); Newtonsoft.Json.dll (listed in …System.Internet.d4l)." I then restored the list. The first version of this error was batched per file and stopped after the first file, so it named only `System.Internet.dll`; it is now one error listing all missing files.
- **Duplicate-destination logic.** A throwaway MSBuild project in the scratchpad (since deleted) used the same `Metadata('DataphorSource')->Distinct()->Count()` condition. It flagged two different sources for one destination and did not flag the same source repeated.
- **Deletion guard.** `-t:_DataphorCheckLayoutDir` with `-p:DataphorLayoutDir=` set to `C:\temp\x\`, `<artifacts>\layout\` and `<artifacts>\layout\debug\..\..\..\src\` errors each time; the default passes. Nothing was deleted in these runs, because only the check target ran.
- **Clean hook.** `dotnet clean tools/layout/Layout.csproj` removed `artifacts/layout/debug` (Release left alone); the next build recreated it.
- **No-change rebuild cost** (`-clp:PerformanceSummary`, two runs): `Layout.csproj` 90–100 ms in total. Of that, `_DataphorGetLayoutItems` takes 61 ms (it includes the 17 `GetDataphorLayoutItems` calls), `_DataphorComputeLayout` 22 ms, `AssembleDataphorLayout` 18 ms and the per-library `XmlPeek` 11 ms. The whole solution's no-change build took 1.6 s.
- **Spike** (`python tools/net-spike/run.py <scratchpad>\spike --smoke --coverage`, fresh work dir):
  - builds: the converted projects and `Layout.log` 0 errors; the `DataphorServiceHost` errors on the `DAE.Client` wrappers, DAEServer and DAE.Service as expected;
  - smoke: 1,080 operators, 23 scalar types, System 3.1.0.0; concurrency 15 errors (in the expected 10–25 range); resolution cache 0 errors;
  - coverage: `SUMMARY pass=63 fail=14 timeout=0 skipped-ignored=13`, the same 14 as Appendix A (the 12 K14 scripts, SecurityLibrary, DebugLibrary), then `REGISTERED Connection` and `REGISTERED System.Internet`.
- **EngineCheck**: `RESULT: pass` in Debug and in Release (1,060 operators, 23 scalar types; `Alphora.Dataphor.DAE.Diagnostics (0)` resources).
- **Docs link check**: `python tools/docs-link-check/check_links.py --pending tools/docs-convert/legacy-links.tsv` gives 0 errors and 203 pending (K17).

Checked by inspection only: the private-native-asset error (no such asset exists), the name-collision and folder-name errors (no collision exists; the folder-name branch evaluates per bucket, otherwise the current build would fail), and the "library project in an excluded folder" error.

## Tests

None added. The layout is build wiring (project rule: no tests for wiring), and the `.d4l` check turns the class of defect that matters (a file a library lists but does not get) into a build error. The characterization is the spike's coverage run from the layout plus the two registration lines, together with EngineCheck. `test-harness-inprocess` makes the coverage run permanent.

## Things the reviewer should look at (known gaps, judgment calls)

- **Private targets used**: `_HandlePackageFileConflicts` (through `ResolveAssemblyReferences`), `_ConflictPackageFiles`, `_NativeRestoredAppHostNETCore`, `_MSBuildProjectReferenceExistent`, `_AddOutputPathToGlobalPropertiesToRemove` and `_GlobalPropertiesToRemoveFromProjectReferences`. These are SDK-internal names and could change between SDK feature bands. The SDK's own targets use them the same way. `global.json` pins 10.0.100 with `latestFeature` roll-forward, and the build used 10.0.404.
- **Batched wildcard include**: `_DataphorLibrarySourceFile` uses a batched wildcard `Include="%(_DataphorLibrary.Folder)**"`. It works (verified: every library folder equals its tracked files), but it relies on MSBuild expanding wildcards after metadata batching inside targets.
- **Empty folders** are not removed by the mirror (stray `Libraries\<X>` folders are). The tripwire is parked as a `NOTE:` at the mirror step in `Layout.csproj`.
- **bin/ has no host**, by design (plan note, AGENTS.md). bin/ includes non-Windows runtime assets (linux and osx SQLite interop, unix SqlClient) because it keeps the SDK's RID-less copy-local shape. `installer-publish-profiles` decides whether the install image trims them.
- **Satellite resources of a private package** would keep their culture subfolder inside the library folder (the flattening applies only to RID-specific files). No library has one today.
- **`DataphorLayoutDir` in non-SDK projects**: `Directory.Build.targets` is also imported by the legacy csproj files (through `Microsoft.Common.targets`). It only defines a property and targets that nothing calls there. The spike's wrappers live outside the repository and do not import it.
- **The AGENTS.md "Bin list for anything else that ships (test projects stay out …)"** wording goes slightly beyond the ticket's "Bin list otherwise", so that a future converted NUnit project is not put into `bin/`.
- **Migration notes**: no new entry. The layout changes nothing an application, deployment or extension sees: no `.d4l` edit, and the server finds libraries the same way. I only updated the count in the existing version/signing entry, because DAE.Diagnostics is now 4.0.0.0 and signed.

## Review findings index (for the reviewer to extend)

- Tripwire: empty folders after mirroring, `NOTE:` at the mirror step in `tools/layout/Layout.csproj`.
- Tripwire: the `net10.0-windows` reference restriction, `NOTE:` in the header comment of `tools/layout/Layout.csproj` (from the plan).
