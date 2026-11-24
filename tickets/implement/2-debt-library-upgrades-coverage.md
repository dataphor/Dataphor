description: Taking a new version of an installed application is the riskiest thing a Dataphor customer does - the engine runs a numbered series of change scripts against their live database - and not one line of that code has ever been run by a test.
prereq: debt-library-administration-coverage, debt-library-attach-from-a-directory
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryUpgrades.d4, tests/DAE.Server.Tests/coverage-manifest.json, src/engine/Server/Runtime.Instructions/LibraryNodes.cs, docs/architecture/libraries-and-class-loading.md
difficulty: hard
----
## What this is

A Dataphor *library* is the folder an application ships in. Beside its descriptor and documents it may hold an `Upgrades/` folder of numbered D4 scripts - `Upgrade.1.0.1.d4`, `Upgrade.1.0.2.d4` - each one the set of changes that takes the application from one revision to the next. When a customer takes a new version, the new folder arrives with a higher version in its descriptor, and the engine runs the scripts between the version their database is at and the version the new descriptor claims. That is the whole of the upgrade machinery, and **every line of it is at exactly 0 %** (measured on the full three-part run of 2026-11-21 at commit `f6200026`, `docs/coverage-report.md`): `UpgradeLibrary` (119 coverable lines), `UpgradeVersions` (47), `UpgradeUtility` (34), `SaveUpgrade` (19), `InjectUpgrade` (18), `LoadUpgrade` (17), `UpgradeLibraries` and `DeleteUpgrade` (9) - about 263 lines, none of which has ever run.

This is the last and heaviest of the three pieces the parent plan ticket split into; the other two are `debt-library-administration-coverage` and `debt-library-attach-from-a-directory`, both of which must land first.

Add one D4 coverage script, `LibraryUpgrades.d4`, in `tests/libraries/TestFramework.Coverage.Scripts/Documents/`, with its entry in `tests/DAE.Server.Tests/coverage-manifest.json`; add a short paragraph to `docs/architecture/libraries-and-class-loading.md`; add one `NOTE:` comment. No engine change.

## How the machinery works, and the recipe that reaches it

This was worked out and **run** during planning; follow it rather than re-deriving it.

**Where the two version numbers live.** A library has a *descriptor* version (the `Version` column of the `Libraries` catalog table, from its `.d4l` file) and a *recorded* version - the version the catalog believes is actually applied, kept in the `LibraryVersions` catalog table. `UpgradeLibrary(name)` runs every `Upgrades/Upgrade.*.d4` whose version is greater than the recorded version and less than or equal to the descriptor version, each in its own transaction on a session of its own owned by the library's owner, updating `LibraryVersions` inside that transaction; when it is done it records the descriptor version. `UpgradeLibraries()` does that for every registered library, requisites first. `LibraryView` exposes the pair as `LoadedVersion` and `UpgradeRequired`.

**`InjectUpgrade` records an upgrade; it does not apply one.** `InjectUpgrade(name, script)` bumps the descriptor version's revision by one, writes the script as `Upgrades/Upgrade.<major>.<minor>.<revision>.d4`, and returns that version. Because the library is registered, the version bump goes through `SystemSetLibraryDescriptorNode.ChangeLibraryVersion`, **which also advances the recorded version** - so after an inject there is nothing pending. That is right for what inject is for: a developer captures a change they have already made on their own server. It is also why a script cannot reach `UpgradeLibrary`'s real work by injecting alone, which is what made this area look unreachable.

**The one setup move.** The state an upgrade actually runs in - registered, descriptor ahead of recorded - is what a customer's server is in after the new library files are copied in and the server restarts (`LibraryUtility.LoadLibrary` reads the recorded version before loading and writes the same value back afterwards, deliberately, so a restart does not swallow a pending upgrade). A coverage script cannot restart its server, but it can put the catalog in that state directly:

```
update LibraryVersions set { Version := VersionNumber(1, 0, 0) } where LibraryName = 'UpgradeTest';
```

This works as the `Admin` user the harness connects as - `ServerCatalogDeviceSession` has an update handler for `System.LibraryVersions` - and was run during planning. It is the script's only artificial step and it must carry a comment saying what it stands for.

**The manifest entry needs `"preamble": "SetUseImplicitTransactions(false);"`, and this is not optional.** Measured during planning: without it the script fails after 30 s with "The catalog store is locked by another session's open transaction and did not become available within 30 seconds" - the harness runs a whole document inside one implicit transaction, that transaction holds the catalog store's write lock, and the session `UpgradeLibrary` starts to run the upgrade waits for it and gives up. The note on the entry should say so, because a 30-second timeout with a SQLite lock message is otherwise a long diagnosis.

**Reading what an upgrade did.** A rooted reference to an object in a library the current library does not require does *not* resolve: `.UpgradeTest.UpgradeTrace` failed with `Unknown identifier` during planning. Either check the catalog - `exists (CatalogObjects where Name = 'UpgradeTest.UpgradeTrace')` - or `SetLibrary('UpgradeTest')` and then name the object unqualified. `SetLibrary` back to `General` at the end, not to `TestFramework.Coverage.Scripts`, which is not registered in the coverage server.

**Errors arrive wrapped.** `E.Code` on a caught error is 104229 (the wrapping `RuntimeException`), not the schema code. Assert on text, from `E.Message` or - better for an error raised several layers down, as a failing upgrade's is - `Diagnostics.GetDetailedErrorDescription(E)`.

## What the script must cover

**Version arithmetic, and that nothing is applied twice.** Create a library at `VersionNumber(1, 0, 0)` and register it; the recorded version is 1.0.0. Inject an upgrade; it returns 1.0.1, both versions are 1.0.1, and `UpgradeVersions` lists 1.0.1. `LoadUpgrade` returns the script text that was injected. Roll the recorded version back to 1.0.0; `LibraryView` now says `UpgradeRequired`. `UpgradeLibrary` applies the upgrade, the recorded version becomes 1.0.1 and `UpgradeRequired` is false. Then `UpgradeLibrary` again *and* `UpgradeLibraries()`, and check nothing happened a second time. The whole of this sequence was run during planning and passes.

**Ordering.** Inject three upgrades, 1.0.1 to 1.0.3, roll the recorded version back to 1.0.0, and apply them with one `UpgradeLibrary`. Prove the order rather than only the outcome: have 1.0.1 create a one-row table with a string column and seed it `'1.0.1;'`, and 1.0.2 and 1.0.3 each append their own version to that string. Assert the string is exactly `'1.0.1;1.0.2;1.0.3;'` - that pins the order and that each script ran exactly once, and if 1.0.2 ever ran first the table would not exist and the failure would be loud. Note for the reader that file-name order and version order differ past ten revisions (`Upgrade.1.0.10.d4` sorts before `Upgrade.1.0.2.d4`), and that the ordering comes from `UpgradeVersionsNode` keying its result on `Version`, not from the directory listing - a second library taken past revision 9 would pin that too and is worth one short case.

**Requisites first.** `UpgradeLibrary` gathers a library's requisites transitively and upgrades them before the library itself. Create `UpgradeBase` and `UpgradeDependent` (which requires it), register both, inject an upgrade into each - `UpgradeBase`'s creating the trace table and appending `'base;'`, `UpgradeDependent`'s appending `'dependent;'` to the same table, which it can see because it requires `UpgradeBase` - roll both recorded versions back, and call `UpgradeLibrary('UpgradeDependent')`. The trace must read `'base;dependent;'`.

**A failing upgrade leaves a coherent state.** This is the case worth the most. On the three-upgrade library above, inject a fourth upgrade whose *first* statement raises, and a fifth that appends to the trace. Roll the recorded version back to 1.0.3 and call `UpgradeLibrary`. Assert: it raises and `Diagnostics.GetDetailedErrorDescription` names the deliberate failure; the recorded version is still 1.0.3, the last version that actually applied; the trace is unchanged, so the fifth upgrade did not run; and the library is still in `LoadedLibraries`. Then `DeleteUpgrade` the fourth and call `UpgradeLibrary` again: it resumes from 1.0.3, applies the fifth, and the recorded version becomes 1.0.5. That last step is what proves the recorded version was left usable rather than merely unchanged, and it is also the only meaningful cover for `DeleteUpgrade`.

Make the failing upgrade raise **before it changes anything**. Whether a half-finished upgrade's own catalog changes roll back is a different subject with its own tickets (`library-ddl-inside-a-transaction`, in `plan/`, establishes that library DDL is not transactional), and this script must not accidentally become the test for it.

**Upgrade version numbers that are refused.** `UpgradeUtility.CheckValidVersionNumber` requires a major, minor and revision and no build. Cover both refusals: a library created at `VersionNumber(1, 0)` has no revision and so cannot have an upgrade injected (`InvalidUpgradeLibraryVersionNumber`, 113228, "Upgrades cannot be tracked for library ..."), and a version that *does* carry a build is refused by `SaveUpgrade` (`InvalidUpgradeVersionNumber`, 113227). The D4 `VersionNumber` selector takes at most three arguments, so the second case needs `ToVersionNumber('1.0.1.1')` to produce a version with a build at all. Run each once and assert on the text the engine produces.

**`SaveUpgrade` with an explicit version.** `SaveUpgrade(name, version, script)` writes the file without touching either version number, which is how the IDE's upgrade editor saves an edit. Save a script at a version above the descriptor, check `UpgradeVersions` lists it and `LoadUpgrade` returns it, and check that `UpgradeLibrary` does **not** run it, because the descriptor version does not reach it - an upgrade is bounded by the descriptor, and that is the rule that stops a half-shipped upgrade from running. `SaveUpgradeNode` also has an arm that treats a nil version as a call to `InjectUpgrade`: try `SaveUpgrade('UpgradeTest', nil, '...')`; if the compiler will not type the nil for a `const AVersion : VersionNumber` parameter, that arm is unreachable from D4 - say so in the handoff and leave it.

## Edge cases & interactions

- **Every library the script creates must be dropped at the end**, which also clears its `LibraryVersions` row (`DropLibrary` calls `ClearCurrentLibraryVersion`). Each coverage case gets its own instance directory and its own copy of the library folders, so a leak cannot reach another case - the parent plan ticket's claim that scripts in one run share a library directory is wrong (`tests/DAE.Server.Tests/TestServerInstance.cs`, the template-copy constructor). Clean up anyway, so the script can be re-run against one server and its failures stay readable.
- **The upgrade runs on a session of its own**, connected as the library's owner with an empty password (`Admin` for a library the script creates). That is why implicit transactions must be off, and it also means an upgrade script cannot see the outer script's session tables or uncommitted work.
- **An upgrade script runs with the library current**, via `SetLibrary` inside the engine's own wrapper, so names in it resolve through that library's path. An upgrade for `UpgradeDependent` can name an object of `UpgradeBase` unqualified; it cannot name one of a library it does not require.
- **`UpgradeLibrary` requires the library to be registered.** It resolves the loaded library first, so calling it on a library that exists but is not registered raises. Pin that - it is a plausible administrator mistake - and run it once to see what the message is.
- **The descriptor version is the ceiling, the recorded version the floor.** A descriptor at 1.0.5 with only `Upgrade.1.0.1.d4` present still ends with 1.0.5 recorded: `UpgradeLibrary` records the descriptor version when it finishes, whether or not any script ran. Worth one assertion, because it is the behaviour that lets a release skip revisions, and a reader who has not seen it will read it as a bug.
- **Keep the script out of explicit transactions.** It needs none, and `library-ddl-inside-a-transaction` may make `DropLibrary` refuse inside one. State in the handoff that this script drops libraries outside any explicit transaction - that ticket's checklist asks for the survey.
- **Leave the commented-out block in `LibraryNodes.cs` alone.** Lines around 1300-1440 hold a commented-out `UpgradeRequisitesNode` and a second `UpgradeVersionsNode` from a per-upgrade requisite design that was never finished; the long comment at the head of `SystemUpgradeLibraryNode` describes it. `UpgradeUtility.GetRequisitesFileNameFromVersion` is the live remnant of it and has no caller, so it cannot be covered. Do not delete or revive any of that here.
- **Manifest entries are sorted by script name in ordinal order**, so `LibraryUpgrades` goes between `LibraryTypes` and `ListLibrary`. A script with no entry, or an entry with no script, fails the run.
- **Which test part the new case lands in.** The three-part agent split puts scripts whose names begin A-R in part 2 (best measured 3 m 28 s, `AGENTS.md`). A probe covering the version-arithmetic and no-op sequence took 2 s of case time in Debug on one slow development laptop during planning; the full script will be several times that and still small against the part.

## Tests

The script is the test, and it is the characterization test the project's testing rule asks of a subsystem being re-hosted with no coverage. One case per behaviour; no C# test is owed.

## Verification

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=LibraryUpgrades|Name=LibraryAdministration|Name=LibraryCoverage|Name=LibraryDependencies|Name=LibraryRename|Name=VersionNumber" --logger "console;verbosity=normal"
python tools/docs-size-check/check_size.py
```

Name the filter you used in the handoff. Do not run the whole suite; CI does that after the push.

## TODO

- Write `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryUpgrades.d4` covering the sections above, in this order: version arithmetic and the no-op re-run; the refused upgrade version numbers; `SaveUpgrade` with an explicit version above the descriptor; ordering; requisites first; the failing upgrade and the resume after `DeleteUpgrade`. Comment the `update LibraryVersions` setup line with what it stands for. Tabs, CRLF, no byte-order mark, as the other scripts in that folder have.
- Add the manifest entry for `LibraryUpgrades` in ordinal position, with `"preamble": "SetUseImplicitTransactions(false);"` and a `note` saying why the preamble is needed (the upgrade runs on a session of its own and the harness's implicit transaction holds the catalog store's write lock; without it the case fails after 30 s with a SQLite lock message).
- Add a paragraph on the upgrade mechanism to `docs/architecture/libraries-and-class-loading.md`, which owns libraries: the `Upgrades/` folder and its file naming, the descriptor version against the recorded version in `LibraryVersions`, what `UpgradeLibrary` runs and in what order, that each upgrade runs in its own transaction on a session owned by the library, that a restart preserves the recorded version so a pending upgrade is not swallowed, and that `InjectUpgrade` records a change already made rather than applying one. The page was 215 words of a 1,500-word limit when this was planned, so there is room; run `python tools/docs-size-check/check_size.py` after editing it.
- Add a one-line `NOTE:` comment at `UpgradeUtility.GetRequisitesFileNameFromVersion` in `src/engine/Server/Runtime.Instructions/LibraryNodes.cs` recording that it has no caller because the per-upgrade requisite design described in the comment block above `SystemUpgradeLibraryNode` was never implemented, so it cannot be covered.
- Build, run the filter above and the documentation size check.
- In the handoff: list what the script pins, the filter you ran, whether the nil-version arm of `SaveUpgrade` turned out to be reachable from D4, anything you could not cover and why, and that the script drops libraries outside any explicit transaction.
