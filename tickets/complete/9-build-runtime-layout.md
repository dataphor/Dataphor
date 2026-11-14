description: The build now assembles a ready-to-run folder with the server's binaries and every library in the shape the server expects, so tests, local runs and the installer can start from the same files instead of from the source tree.
prereq: build-server-project
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.targets (new), tools/layout/Layout.csproj (new), Dataphor.slnx, tests/libraries/TestFramework.Coverage.Base/Source/DAE.Diagnostics.csproj, tests/libraries/TestFramework.Coverage.Base/Source/Properties/AssemblyInfo.cs, tools/net-spike/run.py, tools/net-spike/Cov/Program.cs, tools/net-spike/README.md, tools/net-spike/EngineCheck/EngineCheck.csproj, tools/net-spike/EngineCheck/Program.cs, AGENTS.md, docs/modernization-plan.md, docs/migration-notes.md, tickets/plan/10-test-harness-inprocess.md, tickets/backlog/feat-ws2-server-port-remainder.md
difficulty: medium
----
# Runtime layout under artifacts/layout — complete

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

## Review findings

Read the `ticket(implement): build-runtime-layout` diff first, then the handoff above. Ran `dotnet build Dataphor.slnx` (0 errors), EngineCheck Debug (`RESULT: pass`, 1,060 operators, 23 scalar types) and the spike (`run.py <scratchpad>\spike --smoke --coverage`, fresh work dir).

**Spike results.** Builds as expected: converted projects and `Layout.log` 0 errors; the `DataphorServiceHost` errors on the `DAE.Client` wrappers, DAEServer and DAE.Service. Smoke: 1,080 operators, 23 scalar types, System 3.1.0.0. Resolution cache: 0 errors. Coverage: `SUMMARY pass=63 fail=14 timeout=0 skipped-ignored=13`, with the same 14 failures as Appendix A, then `REGISTERED Connection` and `REGISTERED System.Internet`. Concurrency: 26 errors, one above the documented 10–25: 24 "database is locked", 1 "unrecognized token" and 1 `ArgumentException: Destination is too short`, a message no document mentioned. The smoke step does not touch the layout or anything else in this diff. Concurrency counts vary from run to run while K1 and K10 are open (`port-catalog-emitter-race` in implement, `store-sqlite-concurrency` in plan). So I did not write a pre-existing-error report. Instead:
- AGENTS.md's expected range is now 10–30, and it names that message as a shared-buffer error.
- `port-catalog-emitter-race` gets a line asking its reproduction test to confirm the message is gone after the fix (the message fits two threads writing one `StringBuilder`; the spike prints no stack).

**Correctness.**
- Read every target in `Directory.Build.targets` and `tools/layout/Layout.csproj`: the batching, the `MatchOnMetadata` removals, the RID ranking, the duplicate-destination check, the `.d4l` check and the mirror step.
- The `.d4l` check requires every listed file, including environment-restricted ones. That is correct: the server serves client-environment files from the library folder (`Server.cs` around line 887), and it loads only files with no environment or with `WindowsServer` (`LibraryUtility.RegisterLibraryFiles`).
- The `.pdb` items come from a statically evaluated item (`DebugSymbolsProjectOutputGroupOutput` in `Microsoft.Common.CurrentVersion.targets`), not from a target the layout fails to depend on. I confirmed this by calling `GetDataphorLayoutItems` on System.Internet alone: dll, pdb and packages are returned.
- No untracked or ignored files exist in the library folders apart from build output, which is excluded. No tracked `.dll`, `.pdb` or `.exe` files sit outside `Source/`.
- No defects found.

**Latent and conditional concerns, parked as `NOTE:` tripwires:**
- *A library that references another library's project* (MSSQLDevice → Connection) gets neither that library's assembly nor the packages that come through it. This works only because MSSQLDevice's D4 library references reach Connection (MSSQLDevice → SQLDevice → Connection), so those files are loaded first. No run registers MSSQLDevice from the layout yet. Parked at the private-package rule in `Directory.Build.targets`.
- *RID names.* Only `win-x64` and `win` are ranked. A private package that ships its Windows file as `win7-x64` or `win10-x64` would silently fall back to the generic stub (no such package today: Connection's private RIDs are `win`, plus `win-x64`, `win-x86`, `win-arm64` and `unix` that rank out). Parked at the ranking comment in `tools/layout/Layout.csproj`.
- *Untracked files.* The library glob copies what is on disk, untracked files included. That is fine on a clean checkout and matters if the installer harvests from the layout. Parked at the glob in `tools/layout/Layout.csproj`.
- From the implementer, kept as they were: empty folders after mirroring (`NOTE:` at the mirror step) and the `net10.0-windows` reference restriction (`NOTE:` in the `Layout.csproj` header).

**Minor fixes made.** The `Directory.Build.targets` header comment said item definitions in a project body see `DataphorLayoutDir`. That is true of items too, but a *property* in a project body does not see it, because the file is imported after the body. The comment now says this, so nobody writes `<X>$(DataphorLayoutDir)…</X>` in a `PropertyGroup` and gets an empty string.

**Private SDK names** (`_ConflictPackageFiles`, `_MSBuildProjectReferenceExistent`, `_GlobalPropertiesToRemoveFromProjectReferences`, …). Accepted as the implementer described: the SDK's own targets use them the same way, and an SDK band that renames one breaks the layout build loudly, not silently. No `NOTE:` needed beyond the handoff.

**Tests.** None added, none cut. The implementer added none. The layout is build wiring, and its one important behaviour (a library gets every file its `.d4l` lists) is a build-time error, not a test. The characterization of the subsystem change is the spike's coverage run from the layout plus the two `REGISTERED` lines, and `test-harness-inprocess` makes that permanent.

**Docs.** I read AGENTS.md, `tools/net-spike/README.md`, the plan's WS1 note and table row, the K15 row, `docs/migration-notes.md` and both appended ticket notes; all match the code. `docs/architecture.md` describes the 3.1-era system and has no build-output section, so it needed no change. Migration notes: I agree no new entry is needed. The layout changes nothing an application, a deployment or an extension sees, and the version/signing entry's count (seventeen) is correct.

**Source hygiene.** `DAE.Diagnostics.csproj` and `AssemblyInfo.cs` keep their BOM and CRLF line endings; the two new MSBuild files are CRLF without a BOM, like `Directory.Build.props`. File sizes are reasonable (`Layout.csproj` 253 lines, `Directory.Build.targets` 96 lines, measured with `wc -l`). The comments state why, not what. The build with my edits is 0 errors, and the layout is unchanged at 545 files.

**Tickets filed.** None. No finding met the bar of a current-release anchor or a class-level invariant.
