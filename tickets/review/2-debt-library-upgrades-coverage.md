description: A test now covers what happens when a customer takes a new version of an installed application - the step where the database engine runs the application's change scripts against their live data - including what it leaves behind when one of those scripts fails.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryUpgrades.d4, tests/DAE.Server.Tests/coverage-manifest.json, src/engine/Server/Runtime.Instructions/LibraryNodes.cs, docs/architecture/libraries-and-class-loading.md
difficulty: hard
----
## What landed

One new D4 coverage script, `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryUpgrades.d4` (385 lines, 8 sections), its one entry in `tests/DAE.Server.Tests/coverage-manifest.json`, two paragraphs on the upgrade mechanism in `docs/architecture/libraries-and-class-loading.md`, and two `NOTE:` comments in `src/engine/Server/Runtime.Instructions/LibraryNodes.cs`. **No engine change**: the C# diff is 8 lines, all comment.

The script is a characterization test, which is what the project's testing rule asks of a subsystem being re-hosted with no coverage. Nothing in it was chosen - every expected value was observed by running the engine.

## What it pins

One section per behaviour, each on its own library, each dropping what it created.

| Library | What the section pins |
|---|---|
| `UpgradeArith` | Registering a library records its descriptor version as the applied version, so nothing is pending on a fresh register. `InjectUpgrade` returns the next revision, advances the descriptor version **and** carries the recorded version with it. `UpgradeVersions` lists exactly the injected upgrade; `LoadUpgrade` returns, character for character, the text `InjectUpgrade` wrote. With the recorded version rolled back, `LibraryView` reports `LoadedVersion` and `UpgradeRequired`; `UpgradeLibrary` then applies the upgrade, records the descriptor version, clears `UpgradeRequired`, and the upgrade's table is in `CatalogObjects`. A second `UpgradeLibrary` and an `UpgradeLibraries()` change nothing. |
| `UpgradeNoRevision` | A library whose version stops at the minor cannot have upgrades tracked for it: `InjectUpgrade` raises `InvalidUpgradeLibraryVersionNumber` (113228), and the refusal comes before anything is written - no upgrade file, descriptor unmoved. |
| `UpgradeBadVersion` | An upgrade version that carries a build is refused by `SaveUpgrade` with `InvalidUpgradeVersionNumber` (113227). Then the nil-version arm of `SaveUpgrade`: it is an inject, and it advances the descriptor and writes the script at the next revision. Left unregistered deliberately - nothing here needs it registered. |
| `UpgradeUnregistered` | `UpgradeLibrary` resolves the loaded library first, so a library that exists but is not registered raises `LibraryNotRegistered` (113242). A plausible administrator mistake. |
| `UpgradeTeens` | `SaveUpgrade` with an explicit version writes the file and touches neither version number. Four upgrades saved at 1.0.1, 1.0.2, 1.0.10 and 1.0.11 on a library whose descriptor is 1.0.10: the trace reads `1;2;10;`, which pins **version order rather than file-name order** (`Upgrade.1.0.10.d4` sorts before `Upgrade.1.0.2.d4`) and pins that the upgrade above the descriptor version did not run even though `UpgradeVersions` lists it and `LoadUpgrade` returns it. The descriptor is the ceiling. |
| `UpgradeSkip` | Descriptor 1.0.5, one upgrade at 1.0.1: `UpgradeLibraries()` runs it and then records **1.0.5**, the descriptor version, because the recorded version is a statement about which files are applied and not about which scripts happened to exist. This is the behaviour a reader mistakes for a bug, and it is what lets a release skip revisions. `UpgradeLibraries()` rather than `UpgradeLibrary` here, so the all-libraries operator is covered doing real work as well as doing nothing. |
| `UpgradeOrder` | The same ordering through the operator a developer actually uses: three successive `InjectUpgrade` calls, one `UpgradeLibrary`, trace `1.0.1;1.0.2;1.0.3;`. The string pins both the order and that each script ran exactly once. |
| `UpgradeBase` / `UpgradeDependent` | Requisites are upgraded first, transitively: the dependent's upgrade appends to a table the requisite's upgrade creates, and the trace reads `base;dependent;`. Both recorded versions advance. |
| `UpgradeFail` | **The case that matters most.** Three upgrades, the second raising on its first statement. `UpgradeLibrary` raises and carries the upgrade script's own error text out; the recorded version is left at **1.0.1** - the last upgrade that actually applied, not the descriptor version and not the version the run started from; the trace holds only the first upgrade's work, so the third upgrade did not run; and the library is still in `LoadedLibraries`. Then `DeleteUpgrade` on the broken upgrade and a second `UpgradeLibrary`: it resumes from 1.0.1, applies only the third, and records 1.0.3. That resume is what proves the recorded version was left *usable* rather than merely unchanged, and it is the only meaningful cover for `DeleteUpgrade`. |

A final sweep asserts no library and no `LibraryVersions` row is left behind.

## Coverage moved from zero

Measured on this change with `--collect:"Code Coverage;Format=cobertura"` over the one case, read per class out of the Cobertura file (sequence-point lines, so the denominators differ from the `docs/coverage-report.md` figures the ticket quoted, which count coverable lines):

| Class | Lines covered |
|---|---|
| `SystemUpgradeLibraryNode` | 218/238 |
| `UpgradeVersionsNode` | 86/94 |
| `UpgradeUtility` | 54/68 |
| `SaveUpgradeNode` | 38/38 |
| `LoadUpgradeNode` | 34/34 |
| `InjectUpgradeNode` | 28/36 |
| `DeleteUpgradeNode` | 18/18 |
| `SystemUpgradeLibrariesNode` | 8/8 |

484 of 534, from 0. The 50 uncovered lines are four blocks, and three of them cannot be reached:

- **`SystemUpgradeLibraryNode.GatherDependents`** (10 lines) - nothing calls it but itself. It belongs to the unload-the-dependents-and-reload recovery sketched in the comment block above the class, which was never implemented; a failing upgrade simply raises and leaves the library registered, which the `UpgradeFail` section now pins. A `NOTE:` is at the method.
- **`UpgradeUtility.GetRequisitesFileNameFromVersion` and `GetRequisitesFileName`** (7 lines) - the live remnant of the per-upgrade requisite design, whose `UpgradeRequisitesNode` and second `UpgradeVersionsNode` are commented out below. The second has no caller either, so neither can be covered. A `NOTE:` is at the first, as the ticket asked.
- **`InjectUpgradeNode`'s catch that restores the descriptor version** (4 lines) - after the bump, the upgrade version it builds always has a revision and no build, so `CheckValidVersionNumber` cannot refuse it; only a file-system failure writing the script reaches this, which a D4 script cannot arrange.
- **`UpgradeVersionsNode`'s dispose-and-rethrow** (4 lines) - reachable only if opening or populating the result table throws.

The ticket said not to delete or revive the commented-out block, and nothing here does. **Whether the whole remnant set should go is a call for a human, not something this ticket filed.** If the reviewer thinks it should be a ticket, the single site is `src/engine/Server/Runtime.Instructions/LibraryNodes.cs` lines ~1003 (`GatherDependents`), ~1203-1233 (the two file-name helpers) and ~1306-1450 (the commented-out nodes), and the two `NOTE:` comments are the evidence.

## Where the ticket's recipe turned out to be wrong, and what was done instead

The ticket's plan was worked out during planning and mostly held. Five things did not, and the reviewer should know which claims in the script's header are mine rather than the ticket's:

1. **No `SetLibrary` is needed, and the ticket's `ToVersionNumber` workaround is not needed either.** The ticket reported that a rooted reference to an object in another library "does not resolve: `.UpgradeTest.UpgradeTrace` failed with `Unknown identifier`". It does resolve: `Objects<T>.ResolveName` and `ServerCatalogDeviceSession.ResolveName` both apply the library name resolution path **only to unrooted names**. What planning actually hit is a batching effect - a script is executed one top-level statement at a time and each is compiled only when it is reached, so a `begin … end` block that creates an object cannot also name it. Every section therefore has the shape "a block that does the work, then a bare assertion", and the script header explains why. This is worth the reviewer's attention because it is the one structural decision in the file.
2. **`VersionNumber(1, 0, 1, 1)` compiles.** The ticket said the D4 selector takes at most three arguments and that `ToVersionNumber('1.0.1.1')` would be needed; the type's representation gives a four-argument selector, which `VersionNumber.d4` already exercises. The 113227 case uses it directly.
3. **The nil-version arm of `SaveUpgrade` is reachable from D4** - `SaveUpgrade('X', nil as VersionNumber, script)`, the form D4 uses for a typed nil everywhere else. It is asserted, not left.
4. **`SetLibraryDescriptor` cannot be used to raise a registered library's descriptor version** - it refuses with `CannotChangeRegisteredLibraryVersion`. So the ceiling and skipped-revision sections use libraries *created* at the higher descriptor version (1.0.10 and 1.0.5) with `SaveUpgrade` writing the files, rather than bumping a descriptor after the fact. That also made the past-revision-nine case short, as the ticket wanted, and folded `SaveUpgrade`-with-an-explicit-version into it.
5. **The failing-upgrade section fails at 1.0.2 of three rather than at 1.0.4 of five.** Same shape, fewer moving parts, and it pins one thing more than the ticket asked: that the recorded version **advanced** to 1.0.1 during the failed run. The ticket's version (roll back to 1.0.3, fail at 1.0.4) would have left the recorded version at the value it started from, where "unchanged" and "left at the last applied upgrade" are indistinguishable.

One requisite detail the ticket did not mention: `UpgradeDependent` pins its requisite at `VersionNumber(1, 0)`, not `1.0.0`. A reference that names a revision is not `Compatible` with the revision bump that injecting into the required library makes, and `ChangeLibraryVersion` would refuse it with `RegisteredLibraryHasDependents`.

## The manifest entry, and the preamble claim

The entry carries `"preamble": "SetUseImplicitTransactions(false);"` with a note saying why. **The claim was re-verified rather than taken from the ticket**: with the preamble removed the case fails after 35 s with `RuntimeException … The catalog store is locked by another session's open transaction and did not become available within 30 seconds … SQLiteException: database is locked`. The upgrade runs on a session of its own and the harness's single implicit transaction holds the catalog store's write lock.

The entry is in ordinal position between `LibraryTypes` and `ListLibrary`. The case lands in test part 2 (scripts A-R).

## The one artificial step, for the reviewer to weigh

Each section reaches the pending state - registered, descriptor ahead of recorded - with

```
update LibraryVersions set { Version := VersionNumber(1, 0, 0) } where LibraryName = '...';
```

which stands for "the files of a newer version have arrived and the server has been restarted on them". A coverage script cannot restart its server, and `InjectUpgrade` cannot produce a pending upgrade because on a registered library the descriptor bump carries the recorded version with it. Every such line in the script carries a comment saying what it stands for, and the header says it is the only contrived step. **If the reviewer thinks a C# fixture that actually stops and starts a server would be the better home for some of these sections, that is the argument to make here** - `LibraryAttachTest` is the precedent, and the trade is a few seconds of server start per case against an `update` that no application would ever write.

## Tests added

| Test | What it verifies |
|---|---|
| `LibraryUpgrades.d4` (coverage script, 8 sections) | The whole library upgrade mechanism, as tabulated above. |

No C# test is owed: the script is the characterization test, and a C# fixture would reach the same operators through a thicker harness. Nothing was added for wiring, for the manifest entry, or for the two comments.

## Verification

Run on this change, all green:

```
dotnet build Dataphor.slnx                                  # 0 errors
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=LibraryUpgrades|Name=LibraryAdministration|Name=LibraryCoverage|Name=LibraryDependencies|Name=LibraryRename|Name=VersionNumber" --logger "console;verbosity=normal"
                                                            # 6 passed; LibraryUpgrades 3 s of case time
python tools/docs-size-check/check_size.py                  # 11 files checked, 0 over the limit
```

The architecture page is 575 words of its 1,500-word limit after the two new paragraphs.

Two sensitivity checks were run and then reverted, so the assertions are known to bite rather than merely not raise: flipping the `UpgradeTeens` expectation to `1;10;2;` fails with `The upgrades did not run in version order, or the upgrade above the descriptor version ran: 1;2;10;`, and removing the preamble produces the lock failure quoted above.

The full suite was not run; per the project's testing rule that is CI's job after the push. Nothing outside the coverage-script harness is touched by the diff, so the wider rings a reviewer would normally add are the other library scripts, which are in the filter above.

## For the reviewer

- **The script drops its libraries outside any explicit transaction**, and opens none at all. `library-ddl-inside-a-transaction` asked for that survey.
- **No migration note is owed**: no public type or member changed, no D4 semantics, no file format, no error code.
- **One pre-existing oddity met and left alone.** Every coverage run ends with `Could not delete …\Template-1; the next test run deletes it. Access to the path 'Alphora.Dataphor.DAE.Connection.dll' is denied.` That is the documented behaviour of the harness (a folder it cannot delete because the process still holds a library assembly from it) and is unrelated to this change.
- **Honest gaps.** `UpgradeLibrary`'s behaviour when a *dependent* library has a pending upgrade is not covered, because the dependent-upgrade design was never implemented (see the dead `GatherDependents` above). Nor is an upgrade that changes a device or a real application table - every upgrade here creates or updates one small table in the memory device, which is enough to pin order and exactly-once but is not a realistic upgrade payload. Nor is an upgrade on a library with an assembly, for the reason `LibraryAttachTest` records: a loaded assembly is never unloaded from the test process.
