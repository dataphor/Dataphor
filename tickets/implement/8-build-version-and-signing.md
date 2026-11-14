description: Give every Dataphor assembly the same fixed version number (4.0.0 with a pre-release label) from one place, and sign every build with the project's existing key, so builds are reproducible and every assembly has one identity.
prereq: build-core-sdk-projects
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.props, Dataphor.snk, src/engine/Base/Properties/AssemblyInfo.cs, src/engine/Base.Windows/Properties/AssemblyInfo.cs, src/engine/DAE/Properties/AssemblyInfo.cs, src/protocol/Common/Properties/AssemblyInfo.cs, src/protocol/NativeCLI/Properties/AssemblyInfo.cs, src/protocol/Client/Properties/AssemblyInfo.cs, libraries/SQLDevice/Source/Properties/AssemblyInfo.cs, libraries/System.Platform/Source/Properties/AssemblyInfo.cs, libraries/System.Internet/Source/Properties/AssemblyInfo.cs, libraries/System.Integration/Source/Properties/AssemblyInfo.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/Base.Windows/PathUtility.cs, AGENTS.md, docs/migration-notes.md
difficulty: easy
----
# One version, signed everywhere

## Goal

Every legacy project carries its own `AssemblyInfo.cs` with `AssemblyVersion("3.1.*")`, so the build and revision numbers change on every build and the compiler cannot build deterministically; signing happens only in Release, through a `#if SIGNASSEMBLIES` block that points `AssemblyKeyFile` at a path relative to the legacy `obj` folder. Decision D7 makes the product version 4.0 and requires signing with `Dataphor.snk` in all configurations. This ticket moves version, company, product, copyright and signing into the root `Directory.Build.props`, turns on generated assembly attributes, removes the per-project copies, and leaves only the Dataphor-specific attributes (such as `DAERegister`) in each `AssemblyInfo.cs`. Builds become deterministic.

**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout from [Repository layout](../../docs/modernization-plan.md#repository-layout) (for example `src/engine/DAE` is today's `Dataphor/DAE`, `libraries/X` is `Libraries/X`). Use the plan's old → new table if needed.

## Scope

In scope: `Directory.Build.props`; the `AssemblyInfo.cs` of **every SDK-style project listed in `Dataphor.slnx` when this ticket runs** — at least the ten from `build-core-sdk-projects` (Base, Base.Windows, DAE, Common, NativeCLI, Client, SQLDevice, System.Platform, System.Internet, System.Integration), and also the server set (Server, Store.SQLite, Connection, MSSQLDevice, SimpleDevice, Frontend) if `build-server-project` has landed; any `SIGNASSEMBLIES` left in a project's `DefineConstants`; the `Deterministic=false` override that `build-core-sdk-projects` set for the wildcard version; the "Build and test" section of `AGENTS.md` (one paragraph on the convention).

Out of scope: projects not yet SDK-style (UI, hosts, tests, `TestFramework.Coverage.Base`); the ticket that converts each of them follows the convention written here. Release-time version bumps, the change log and dropping the pre-release label: `release-checklist-and-changelog` (`feat-ws9-installer-and-release`). Library versions in `.d4l` files (`version="3.0.0.0"` on 24 shipped libraries) are D4 library versions, not assembly versions, and do not change.

## Design (settled)

`Directory.Build.props` gains:

- `VersionPrefix` `4.0.0`, `VersionSuffix` `preview`. The SDK then generates `AssemblyVersion` `4.0.0.0`, `InformationalVersion` `4.0.0-preview+<commit>` (the .NET 10 SDK appends the source revision automatically) and `FileVersion`.
- `FileVersion` `$(VersionPrefix).$(DataphorBuildNumber)`, with `DataphorBuildNumber` defaulting to `0` when not passed. Local builds stay deterministic; CI may pass a run number later so that installers can tell builds apart. `AssemblyVersion` stays `4.0.0.0` regardless.
- `Company` `Alphora`, `Product` `Dataphor`, `Copyright` `© Copyright 2000-2015 Alphora` (the string most of the existing files use; the variants such as `Copyright © Alphora 2009-2015` in Base.Windows are metadata only — no code reads any of these attributes, verified by searching for `AssemblyCopyrightAttribute`, `AssemblyCompanyAttribute`, `AssemblyProductAttribute`, `Application.ProductName` and similar).
- `GenerateAssemblyInfo` `true`, with `GenerateAssemblyTitleAttribute` and `GenerateAssemblyDescriptionAttribute` set to `false`: titles and descriptions differ per project and stay in each `AssemblyInfo.cs`.
- `Deterministic` `true` (the SDK default; remove the override). `ContinuousIntegrationBuild` `true` when `$(GITHUB_ACTIONS)` is `true`, so CI builds map source paths.
- `SignAssembly` `true` and `AssemblyOriginatorKeyFile` `$(MSBuildThisFileDirectory)Dataphor.snk`, unconditionally (all configurations). `Dataphor.snk` is the repository-root key pair (596 bytes) that `AssemblyInfo.cs` files reference today; no `PublicSign`, no delay signing.

In each in-scope `AssemblyInfo.cs`, delete: `AssemblyVersion`, `AssemblyFileVersion` (where present), `AssemblyCompany`, `AssemblyProduct`, `AssemblyCopyright`, `AssemblyTrademark`, `AssemblyCulture`, `AssemblyConfiguration`, `AssemblyDelaySign`, the whole `#if SIGNASSEMBLIES … AssemblyKeyFile … #else … #endif` block, `AssemblyKeyName`, and the template comments that only describe those attributes. Keep: `AssemblyTitle`, `AssemblyDescription`, `[assembly: Alphora.Dataphor.DAE.Server.DAERegister("…")]` (load-bearing: library registration reads it), `ComVisible`, `Guid`, and anything else not generated by the SDK. Keep tabs, CRLF and the byte-order mark of each file (most have one).

## Edge cases & interactions

- **System library version.** The engine sets the D4 `System` library's version from its own assembly version (`src/engine/DAE/Server/Engine.cs:1141`, via `AssemblyNameUtility.GetVersion`). It was `3.1.<build>.<revision>`, different on every build; it becomes `4.0.0.0`. A new catalog records it in `DAELibraryVersions` (`src/engine/DAE/Device.Catalog/CatalogStore.cs:158`) and `DAEServerInfo.Version` (`CatalogStore.cs` ~147 and ~1123); `System.ServerSettings` reports the server assembly's version (`src/engine/Server/Device.Catalog/ServerCatalogDevice.cs:106`). Library references are checked with `VersionNumber.Compatible` (`src/engine/Server/Schema/LibraryUtility.cs` ~360), and every shipped `.d4l` references `System` with `version="*"`, so no reference can stop matching. Catalogs are rebuilt, not migrated (D3), so a stored 3.1 version never meets a 4.0 server. Verify by inspection plus one run: start the spike's smoke program (or the engine check from `build-core-sdk-projects`) against the new build and evaluate `System.Libraries` (or `LibraryVersions`) for `System`: it shows `4.0.0.0`, and the coverage run's library registrations still succeed.
- **Per-user and per-machine folders that include the version.** `PathUtility.BuildAppPath` (`src/engine/Base.Windows/PathUtility.cs` ~81–111) appends the Base.Windows assembly version. Major-version folders: the client alias files (`src/client/DAE.Client/AliasManager.cs` ~64, ~79: `%ProgramData%\Alphora\Dataphor\3\Aliases.config` and the `%AppData%` equivalent) move to `…\4\`. Major.minor folders (the default): Dataphoria settings and scratch pad (`src/windows/Dataphoria/Dataphoria.cs` ~172, ~177), the configuration utility's settings (`src/windows/ConfigurationUtility/ApplicationForm.cs` ~58, ~179) and the server's temporary stream files (`src/engine/Server/Streams/FileStreamProvider.cs:97`) move from `…\3.1\` to `…\4.0\`. No caller asks for the build-specific folder. Instance configuration and instance directories use no version (`src/engine/Server/Server/InstanceManager.cs:40`, `src/engine/Server/Server/Server.cs:725`) and do not move. Those UI projects are not SDK-style yet, but they read the Base.Windows version at run time, so the move happens as soon as they run against these assemblies. This is the intended side-by-side behaviour; it owes a migration-notes entry (aliases and IDE settings defined in 3.x are not seen by 4.0 until the files are copied from `3\`/`3.1\` to `4\`/`4.0\`). Verified by inspection.
- **No client/server version handshake.** The WCF contract namespace `http://dataphor.org/dataphor/3.0/` is a string literal, not derived from the assembly version; neither side compares versions when connecting. The client's library download (`src/protocol/Client/Server/LocalServer.cs` `GetFile`) compares file write times, not versions. Nothing to change; verified by inspection.
- **Strong-name identity in Debug.** Debug assemblies were unsigned (`PublicKeyToken=null`) and become signed. Class registrations are built at run time from `typeof(…).AssemblyQualifiedName` (for example `libraries/Connection/Source/Register.cs`), the catalog stores only class and library names (`DAEClasses`), and the resolve hook in `src/engine/Base.Windows/AssemblyUtility.cs` matches by full or simple name. A search of `.cs`, `.d4`, `.dfd`, `.dnt`, `.d4l` and `.config` files found no hard-coded Dataphor assembly identity with a version or token. No live project uses `InternalsVisibleTo`. Verify by the smoke/coverage run above passing unchanged.
- **Duplicate attributes.** Any attribute left in an `AssemblyInfo.cs` that the SDK also generates fails the build with CS0579; the build is the check.
- **Projects converted later.** A project that becomes SDK-style after this ticket and still has its old `AssemblyInfo.cs` hits CS0579 or CS8357 (`3.1.*` with deterministic builds). Document the convention in `AGENTS.md` so each conversion ticket strips the same attributes. `tests/libraries/TestFramework.Coverage.Base/Source/Properties/AssemblyInfo.cs` names a key file that does not exist (`Dataphor\KeyFile.snk`); if that project is already SDK-style when this runs, strip it here too.
- **Installer file versions.** With `DataphorBuildNumber` left at 0, every build has `FileVersion` `4.0.0.0`, and Windows Installer does not replace a file with an equal version during a minor upgrade. Record this as a `NOTE:` next to the property in `Directory.Build.props` for `installer-msi` (`feat-ws9-installer-and-release`): pass a build number, or schedule `RemoveExistingProducts` early.
- **Determinism.** Two clean builds of the same commit produce identical assemblies. Verify by building twice into separate clones (or after `git clean` in a scratch clone, not the working tree) and comparing hashes of the DLLs.
- **The key is public.** `Dataphor.snk` is a full key pair in a public repository; strong names here are identity, not a security boundary, which is how .NET treats them. No change.

## Tests

No new automated test: this is build configuration and attribute removal, which the compiler and the checks above cover. Verification: Debug and Release builds succeed; `[System.Reflection.AssemblyName]::GetAssemblyName(<dll>)` on each built assembly shows version `4.0.0.0` and the public key token of `Dataphor.snk` (record the token in the handoff); the determinism comparison; the smoke or engine check showing `System` at `4.0.0.0`; the spike's coverage result unchanged.

## Migration notes

Add to `docs/migration-notes.md`: Dataphor assemblies are version 4.0.0.0 and signed with the same `Dataphor.snk` key in every configuration (custom extensions reference them by that identity); client alias files and Dataphoria settings live in `…\Alphora\Dataphor\4\` and `…\4.0\` and are not read from the 3.x folders.

## TODO

- Add the version, company/product/copyright, generated-attribute, determinism and signing properties to `Directory.Build.props`; remove the `Deterministic=false` override and any `SIGNASSEMBLIES` constant.
- Strip the listed attributes and the `SIGNASSEMBLIES` block from each in-scope `AssemblyInfo.cs`, preserving encoding and line endings.
- Build Debug and Release from a clean clone; fix any CS0579.
- Check version and public key token of every built assembly; run the determinism comparison.
- Run the smoke or engine check and the spike coverage run; confirm `System` reports `4.0.0.0` and results are unchanged.
- Add the `NOTE:` on `DataphorBuildNumber` for the installer.
- Update the "Build and test" section of `AGENTS.md` with the convention for later conversions.
- Add the two migration-notes entries.
