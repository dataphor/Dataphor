description: Every converted Dataphor assembly now gets the same fixed version (4.0.0, pre-release label "preview") from one place and is signed with the project's key in every build, so builds are reproducible; review the change and one deviation from the plan about the built-in D4 System library's version.
prereq: build-core-sdk-projects
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.props, src/engine/Base/Properties/AssemblyInfo.cs, src/engine/Base.Windows/Properties/AssemblyInfo.cs, src/engine/DAE/Properties/AssemblyInfo.cs, src/protocol/Common/Properties/AssemblyInfo.cs, src/protocol/NativeCLI/Properties/AssemblyInfo.cs, src/protocol/Client/Properties/AssemblyInfo.cs, libraries/SQLDevice/Source/Properties/AssemblyInfo.cs, libraries/System.Platform/Source/Properties/AssemblyInfo.cs, libraries/System.Internet/Source/Properties/AssemblyInfo.cs, libraries/System.Integration/Source/Properties/AssemblyInfo.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, tools/net-spike/Smoke/Program.cs, tools/net-spike/README.md, AGENTS.md, docs/migration-notes.md, tickets/blocked/system-library-version.md
difficulty: easy
----
# One version, signed everywhere — review handoff

## What changed

- **`Directory.Build.props`**: `VersionPrefix` 4.0.0, `VersionSuffix` preview; `FileVersion` = `$(VersionPrefix).$(DataphorBuildNumber)` with `DataphorBuildNumber` defaulting to 0 (with the `NOTE:` for `installer-msi` about equal file versions in minor upgrades); `Company`, `Product`, `Copyright` (`© Copyright 2000-2015 Alphora`); `GenerateAssemblyInfo` true with title and description generation off; `Deterministic` true (the `false` override is gone); `ContinuousIntegrationBuild` when `GITHUB_ACTIONS` is `true`; `SignAssembly` + `AssemblyOriginatorKeyFile` = root `Dataphor.snk`, unconditional. The props file now contains a `©` (UTF-8, no BOM; MSBuild reads it as UTF-8).
- **Ten `AssemblyInfo.cs` files** (the projects in `Dataphor.slnx`; `build-server-project` has not landed, so the server set is untouched): removed `AssemblyVersion`, `AssemblyCompany`, `AssemblyProduct`, `AssemblyCopyright`, `AssemblyTrademark`, `AssemblyCulture`, `AssemblyConfiguration`, `AssemblyDelaySign`, the `#if SIGNASSEMBLIES` `AssemblyKeyFile` block, `AssemblyKeyName` and the template comments for them. Kept titles, descriptions, `DAERegister`, `ComVisible`, `Guid`. BOM and CRLF preserved; Common and NativeCLI still have no final newline, as before. The now-unused `using System.Runtime.CompilerServices;` lines were left in place to keep the diff small.
- **Ten converted `.csproj` files**: only the constants comment changed ("signing comes with build-version-and-signing" → "signing is set in Directory.Build.props"). None of them defined `SIGNASSEMBLIES`.
- **AGENTS.md**: a bullet under "Converting another project" listing what to strip from `AssemblyInfo.cs`.
- **docs/migration-notes.md**: one entry for assembly version/signing (token below), one for the version-named settings folders moving `3\`→`4\` and `3.1\`→`4.0\`.

## Deviation from the ticket: the D4 `System` library stays 3.1

The ticket said `System` would become 4.0.0.0 and that no library reference could stop matching. Running the spike disproved that: coverage went from `pass=62 fail=14` to `pass=60 fail=16`, because `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryCoverage.d4` (lines 91, 92, 215) references `System` at version 3, and `LibraryRename` then failed on the leftover library. Making it pass would mean editing expected values in a D4 test script (forbidden by project rules), and application libraries can pin `System` the same way.

So `Engine.cs` now sets `System`'s version from a private constant `SystemLibraryVersion = 3.1.0.0` (with a `NOTE:` pointing at the blocked ticket), instead of from the assembly version. `CatalogStore.cs` writes the `System` library version, not the assembly version, into `DAELibraryVersions` (new catalog) and `DAEServerInfo.Version` (the update path in `SaveServerSettings`; the insert path already used the library version), which keeps the old equality of those three values. The decision on whether to move to 4.0 is in `tickets/blocked/system-library-version.md` with option A (keep 3.1) as the recommended default. Reviewer: check you agree this is the conservative reading; the alternative is a one-line change.

Side effect worth noticing: the version was 3.1.<days>.<seconds> before, different on each build; it is now the same on every build, so exact-version references (all four parts) to `System` behave differently — they never matched reliably before.

## Verification done

- `dotnet build Dataphor.slnx` and `-c Release`: 0 errors, 30 warnings per configuration, the same warning codes as before (CA1416, CA2022, CA2200, CS0162, CS0168, CS0618, CS0672, SYSLIB0014/0021/0023/0051); no CS0579 or CS8357.
- Every built assembly of the ten projects, Debug and Release: `AssemblyVersion` 4.0.0.0, **public key token `5202040c6ce50aea`**, `FileVersion` 4.0.0.0, `ProductVersion` `4.0.0-preview+<commit sha>`.
- Determinism, in two scratch clones of HEAD with this diff applied (not the working tree): Release with `-p:ContinuousIntegrationBuild=true` — clone A twice (artifacts deleted in between) and clone B, 25 DLL hashes identical across all three; Debug without the CI flag, clone A twice — 25 hashes identical. Without `ContinuousIntegrationBuild`, builds at different paths differ (embedded PDB path), which is the expected SDK behaviour.
- `dotnet run --project tools/net-spike/EngineCheck -c Debug`: `RESULT: pass`, 1,060 operators, 23 scalar types.
- `python tools/net-spike/run.py <scratch> --smoke --coverage`: engine/server/libraries 0 errors; smoke 1,080 operators, 23 scalar types, `System.Libraries['System'].Version` and `System.LibraryVersions['System'].Version` both `3.1.0.0`; concurrency 0 errors; resolution cache 0 errors; coverage `pass=62 fail=14` (unchanged; the `TestFramework.Coverage.Base` register failure is K14, pre-existing). The two smoke expressions printing the version were added to `tools/net-spike/Smoke/Program.cs` and the README results row.
- `dotnet build src/engine/Base/Base.csproj -p:DataphorBuildNumber=7`: `FileVersion` 4.0.0.7, `AssemblyVersion` still 4.0.0.0; rebuilt without it afterwards (4.0.0.0).
- Debug assemblies are now signed; the spike's legacy-format wrappers (unsigned, built outside the repo props) reference them without trouble — that is what the smoke and coverage runs exercise.

## Tests

No new automated test: build configuration and attribute removal, checked by the compiler and the runs above. The `System` version behaviour is covered by the existing `LibraryCoverage` coverage script, which is what caught the problem.

## Known gaps / for the reviewer

- The settings-folder move (`PathUtility.BuildAppPath`, aliases, Dataphoria settings) was verified by inspection only, as the ticket said; those UI projects are not SDK-style yet, so nothing exercised it.
- `System.ServerSettings` reports the Server assembly's version; it stays 3.1.* until `build-server-project` converts Server, then reports 4.0.0.0. That is intended (product version) but now differs from `System.Libraries['System'].Version`.
- `TestFramework.Coverage.Base` is not SDK-style yet, so its bad `AssemblyKeyFile` (`Dataphor\KeyFile.snk`) was left for its conversion ticket, which the AGENTS.md bullet covers.
