description: Decide whether the built-in D4 "System" library should report version 4.0 now that the product is 4.0, given that library definitions can require System version 3 and would then stop loading.
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: src/engine/DAE/Server/Engine.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryCoverage.d4, docs/migration-notes.md
difficulty: easy
----
**Blocked: the specification is contradictory.** Unblocks when a human picks one of the options below (or edits the proposed text); the code change for either is a few lines.

## What happened

Decision D7 makes the product version 4.0, and `build-version-and-signing` set every Dataphor assembly to version 4.0.0.0. Until then the engine gave the D4 `System` library (the built-in library every other library depends on) the engine assembly's version, 3.1.<build>.<revision>. That ticket's design said `System` would simply become 4.0.0.0, on the premise that no library reference could stop matching because every shipped `.d4l` file references `System` with version `*`.

The premise is incomplete. A library reference can pin a major version: the reference `System` version `3` (written `3.*` in error messages) matches only 3.x. The coverage script `LibraryCoverage` does exactly this (it sets a test library's descriptor to reference `System` at `VersionNumber(3)` and registers it). With `System` at 4.0.0.0 the spike's coverage run went from 62 pass / 14 fail to 60 / 16: `LibraryCoverage` failed with `Library "System" is required to be version "3.*", but is available as version "4.0.0.0"`, and `LibraryRename` failed after it because the first script did not clean up. Project rules forbid editing expected values in an existing D4 test script, and an application's own `.d4l` files can carry the same kind of pinned reference (version strings are typed by hand in Dataphoria's library editor), so moving to 4.0 is a compatibility decision, not a build detail.

## What the code does now (the interim default)

`Engine.SystemLibraryVersion` is a fixed `3.1.0.0`, no longer derived from the assembly version. The catalog store writes the same value for `System` in `DAELibraryVersions` and `DAEServerInfo.Version` (these used the assembly version, which used to equal the library version). Coverage is back to 62 / 14. `System.ServerSettings` still reports the server assembly version, which becomes 4.0.0.0 once the Server project is converted.

## Proposed text (recommended default: option A)

> The D4 `System` library keeps version 3.1.0.0 in Dataphor 4.0. Its version describes the D4 language and catalog contract that libraries depend on, not the product build; it changes only when that contract changes in a way that should stop old library references from matching. The product and assembly version (4.0.0.0) is reported separately by `System.ServerSettings`.

## Options

- **A. Keep `System` at 3.1.0.0 (what the code does now).** No application library or test script that references `System` 3 or 3.1 breaks. Cost: the number no longer tracks the product version, which may surprise someone reading `System.Libraries`. A one-line migration-notes entry already says so.
- **B. Move `System` to 4.0.0.0 (the original ticket design).** Requires editing the `System` reference in `LibraryCoverage.d4` (lines 91, 92 and 215) from 3 to 4, overriding the "never edit an expected value" rule for this one script, plus a migration-notes entry telling applications to change any `System` reference pinned to 3 or 3.1 to `*` or 4. Applications that miss it fail at library registration with a clear error message (no silent misbehaviour).
- **C. Report 4.0.0.0 but treat 3.x references to `System` as compatible.** Rejected: it changes `VersionNumber.Compatible` semantics or special-cases `System` in library loading, which is more D4 behaviour change than either A or B.

## If nothing is decided

Option A stays in place; nothing breaks. Reversibility: either choice is a one-line change in `Engine.cs` plus, for B, the script edit and a migration note. Catalogs are rebuilt rather than migrated (D3), so no stored version needs converting.
