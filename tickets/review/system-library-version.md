description: The built-in D4 "System" library now reports version 4.0.0.0, matching the 4.0 product; applications whose libraries require System version 3 are told to change that reference.
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: src/engine/DAE/Server/Engine.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryCoverage.d4, docs/migration-notes.md
----
## What was done (owner decision 2026-11-18: option B)

- `Engine.SystemLibraryVersion` (`src/engine/DAE/Server/Engine.cs`, around line 1127) is derived again from the engine assembly version via `typeof(Engine).Assembly.GetName().Version` (the old `AssemblyNameUtility.GetVersion` Silverlight workaround was not restored). The assembly version is the fixed 4.0.0.0 from `Directory.Build.props`, so `System` is 4.0.0.0 on every build. The `NOTE:` above it now says the version follows the assembly version and that its major version is a compatibility surface because library references can pin it.
- `CatalogStore.cs` needed no change: it already writes `server.Catalog.Libraries[System].Version` into `DAELibraryVersions` and `DAEServerInfo.Version`, so both now hold 4.0.0.0.
- **Authorized script edit (under this ticket, owner exception to "never edit a coverage script"):** `LibraryCoverage.d4` — the three `LibraryReference('System', VersionNumber(3))` occurrences (lines 91, 92, 215) changed to `VersionNumber(4)`. Nothing else in the script changed; CRLF and no-BOM preserved. The commit message should say this edit is under `system-library-version`.
- `docs/migration-notes.md`: the version-and-signing row's `System` 3.1 sentence now points to a new row in *D4, catalog and libraries* saying `System` is 4.0.0.0, that a `.d4l` requiring `System` 3 or 3.1 fails with error 113207 `Library "System" is required to be version "3.*", but is available as version "4.0.0.0"`, and to change the reference to `*` or 4. No other doc (`docs/`, `AGENTS.md`) said `System` stays 3.1.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors.
- `dotnet test Dataphor.slnx`: DAE.Server.Tests 142 passed / 25 skipped / 0 failed; Common.Tests 13 passed. `LibraryCoverage` and `LibraryRename` stay `pass` in the manifest (no manifest change).

## Tests

No new test. `LibraryCoverage` is the characterization: it now registers a library requiring `System` at `VersionNumber(4)`, which matches only if `System` reports 4.x, so it covers both "System reports 4.x" and "a library requiring System 4 registers". It does not pin the minor/build/revision (4.0.0.0 exactly); a reviewer may judge whether that is worth a dedicated check.

## For the reviewer

- `System.ServerSettings` and the `System` library now report the same version again; if the assembly major version ever changes, pinned references break — that is stated in the `NOTE:`.
- Existing instance catalogs are rebuilt rather than migrated (decision D3), so a stored 3.1 value for `System` is not converted; check that is acceptable for local developer instances (they are expected to be rebuilt).
