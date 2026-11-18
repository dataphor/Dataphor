description: The built-in D4 "System" library now reports version 4.0.0.0, matching the 4.0 product; applications whose libraries require System version 3 are told to change that reference.
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: src/engine/DAE/Server/Engine.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryCoverage.d4, docs/migration-notes.md
----
## What was done (owner decision 2026-11-18: option B)

- `Engine.SystemLibraryVersion` (`src/engine/DAE/Server/Engine.cs`, around line 1131) is derived from the engine assembly version again (`typeof(Engine).Assembly.GetName().Version`, with the same component mapping as the original 3.1 code). The assembly version is the fixed 4.0.0.0 from `Directory.Build.props`, so `System` is 4.0.0.0 on every build. The `NOTE:` above it says that the major version is a compatibility surface, because library references can pin it.
- `CatalogStore.cs` needed no change: it writes the `System` library's version into `DAELibraryVersions` and `DAEServerInfo.Version`, so a new catalog stores 4.0.0.0.
- Authorized script edit (an owner exception to "never edit a coverage script", made under this ticket): in `LibraryCoverage.d4` the three `LibraryReference('System', VersionNumber(3))` occurrences became `VersionNumber(4)`. Nothing else changed.
- `docs/migration-notes.md`: a new row in *D4, catalog and libraries* says that `System` is 4.0.0.0 and that a library requiring `System` 3 or 3.1 fails to register with error 113207. It tells users to change the reference to `*` or 4. The version-and-signing row points to it.

## Review findings

Checked against the implement diff (`ticket(implement): system-library-version`).

- **Correctness.** I compared the new derivation with the original from before `build-version-and-signing`. `VersionNumber(major, minor, revision, build)` receives `Version.Major, Minor, Build, Revision`, the same mapping as 3.1, so the value is unchanged in form. `VersionNumber.Compatible` matches a reference of major version W against any W.x.y.z, so `VersionNumber(4)` matches 4.0.0.0 and `VersionNumber(3)` does not. Found nothing.
- **Shipped libraries.** All 18 tracked `.d4l` files reference `System` at version `*`, so no shipped, sample or test library is affected. Other coverage scripts pin `System` at version 1 (`DerivationMaps.d4`, `LibraryDomains.d4`). That never matched 3.1 either, and those scripts compare descriptors without registering against `System`, or are known failures for unrelated causes. No change.
- **Docs.** Found one inaccuracy and fixed it: the migration note quoted the error as `"3.*"` for both 3 and 3.1 references, but `VersionNumber.ToString` renders a 3.1 reference as `"3.1.*"`. The note now gives both and names error code 113207. I grepped `docs/` and `AGENTS.md` for claims that `System` stays 3.1 or for pinned `System` 3 references and found none. The plan's decision D7 (product version 4.0) is consistent.
- **Existing catalogs.** A catalog built before this change keeps `System` = 3.1.0.0 in `DAELibraryVersions` and `DAEServerInfo.Version`. Nothing reads those values back for `System`: `LoadServerSettings` skips the version column, and stored library versions are read only when a non-system library is loaded or upgraded. The stale value is only cosmetic, and decision D3 (rebuild catalogs, don't migrate them) covers it. No action.
- **Tests.** No new test. `LibraryCoverage` registers a library that requires `System` at `VersionNumber(4)`, so it fails unless `System` reports major version 4. A test pinning the exact value 4.0.0.0 would only restate `Directory.Build.props`, so I declined it.
- **Code hygiene, error handling, resource cleanup, performance.** The change is one static field plus a helper, computed once per process. No resources and no error paths. Nothing found.
- **Tripwires.** None new. The existing `NOTE:` at `Engine.cs` already says that changing the assembly's major version breaks pinned references.
- **Validation.** `dotnet build Dataphor.slnx`: 0 errors. `dotnet test Dataphor.slnx`: DAE.Server.Tests 142 passed, 25 skipped, 0 failed. Common.Tests: 13 passed.
