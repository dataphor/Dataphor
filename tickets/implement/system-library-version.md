description: Make the built-in D4 "System" library report version 4.0.0.0, matching the 4.0 product, and tell applications that a library requiring System version 3 must change that reference.
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: src/engine/DAE/Server/Engine.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryCoverage.d4, docs/migration-notes.md
difficulty: easy
----
## Decision (owner, 2026-11-18): option B

The owner chose **option B**: `System` moves to 4.0.0.0. This was blocked; the history below is kept for context.

What to do:

- `Engine.SystemLibraryVersion` becomes 4.0.0.0. Derive it from the engine assembly version, as before `build-version-and-signing`; that is now a fixed 4.0.0.0 (`Directory.Build.props`), so it is the same on every build. Replace the `NOTE:` above it with one that says the version follows the assembly version and why the major version matters (pinned `System` references).
- The catalog store keeps writing that value for `System` in `DAELibraryVersions` and `DAEServerInfo.Version`.
- **Authorized script edit:** in `LibraryCoverage.d4`, change the `System` library reference that pins major version 3 (the ticket's survey put it at lines 91, 92 and 215; find each place that references `System` at version 3) to version 4. The owner authorized this one edit as an exception to "never edit a coverage script". Change nothing else in the script, and say in the commit message that the edit is under this ticket.
- `LibraryCoverage` and `LibraryRename` stay `pass` in the manifest; the full `dotnet test Dataphor.slnx` run must be green.
- `docs/migration-notes.md`: change the version-and-signing row's sentence about `System` 3.1. Add an entry: the D4 `System` library is version 4.0.0.0, so a library whose `.d4l` file requires `System` at version 3 or 3.1 fails to register with `Library "System" is required to be version "3.*"…`; change that reference to `*` or 4.
- Update any other doc that says `System` stays 3.1 (search `docs/` and `AGENTS.md`).

A characterization test: check that `System` reports 4.0.0.0 and that a library requiring `System` 4 registers. `LibraryCoverage` may already cover this; if it does, don't add a test.

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
