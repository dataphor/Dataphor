description: A new test script exercises everything an administrator does to an installed application once it is working - reading and changing its settings, taking it out of the server and putting it back, and the errors they meet when they get it wrong - none of which any test had ever touched.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4, tests/DAE.Server.Tests/coverage-manifest.json, src/engine/Server/Runtime.Instructions/LibraryNodes.cs
difficulty: medium
----
## What landed

One new D4 coverage script, `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4` (299 lines, 36 assertions), its manifest entry `{ "script": "LibraryAdministration", "status": "pass" }` in ordinal position between `Lexer` and `LibraryCoverage`, and a two-line `NOTE:` comment above `SystemRefreshLibrariesNode` in `src/engine/Server/Runtime.Instructions/LibraryNodes.cs`. No other engine change. No migration note: nothing an application, a deployment script or a C# extension can notice moved.

**No engine defect was found.** Every behaviour the ticket named works, including the two the ticket was unsure about. Every assertion in the script was written from a measurement, not from reading the code: each error case and each read-back was run once in a throwaway probe that reported what the engine actually produced, and the assertion was written from that output. The script passes, and because the assertions compare measured values a mismatch would raise - it is not passing vacuously.

The script opens **no explicit transaction**, as the ticket asked. It drops libraries inside the implicit per-statement transaction only, so the survey that `library-ddl-inside-a-transaction` (in `plan/`) asks for can record this script as needing no change if that ticket makes `DropLibrary` refuse inside an explicit transaction.

Every library the script touches is one it creates, and each of its eight sections creates and drops its own libraries rather than sharing them across sections. The ticket suggested reusing the `AdminSetA`/`B`/`C` set for the required-and-dependent checks; the script instead builds a separate `AdminDepA`..`AdminDepD` chain, because the setting-resolution shape (C requires A *and* B, so they sit at the same level) and the recursion shape (D requires C requires A and B, so there are two levels to tell apart) are different shapes, and four extra `CreateLibrary` calls are cheaper than coupling two sections. A final top-level statement asserts that none of the twelve library names is left in `Libraries`.

## One of the ticket's premises about the mechanism was wrong

**`SetLibraryDescriptor` does not reach `AddLibraryFile` or `RemoveLibraryFile`.** The ticket says a descriptor carrying a `FileReference` "exercises the `AddLibraryFile` and `RemoveLibraryFile` arms and the `LibraryFiles` catalog table". It does not: `SystemSetLibraryDescriptorNode.SetLibraryDescriptor` removes the old library object from the catalog and adds the new one wholesale, and never calls either arm. The arms are reached by **writing the `LibraryFiles` table**, the way `LibraryDependencies.d4` reaches the requisite arms through `LibraryRequisites`: `InsertLibraryFile` and `DeleteLibraryFile` in `ServerCatalogDeviceSession` are what call them.

The script therefore covers that ground twice, deliberately, with a comment at the site saying which is which: first `insert ... into LibraryFiles` / `delete LibraryFiles where ...`, which is the route through `AddLibraryFile` and `RemoveLibraryFile`, and then `SetLibraryDescriptor` with and without a file reference, which is the descriptor route. Measured: a file set either way shows up in the `LibraryFiles` table and in `GetLibraryDescriptor`, and is gone from both after the matching removal - so the two routes agree, which is worth pinning on its own.

## Two measurements that correct or sharpen the ticket

**Registering an already-registered library is not an error - and `LibraryAlreadyRegistered` (113187) is unreachable from every caller.** The ticket already expected `RegisterLibrary` to return quietly, and it does; `EnsureLibraryRegistered` does too. Going further than the ticket asked: the throw site is `LibraryUtility.RegisterLibrary` at `src/engine/Server/Schema/LibraryUtility.cs:506`, and it has exactly three callers - `SystemRegisterLibraryNode` (which checks `IsLoadedLibrary` first) and the two overloads of `LibraryUtility.EnsureLibraryRegistered` (each of which checks `ResolveLoadedLibrary(..., false) == null` first). So nothing can reach that `throw` today. It is dormant dead code rather than a defect, and nothing was changed or commented at the site - error codes and this guard are a compatibility surface, and a reviewer may reasonably want the observation recorded there or want it left alone. The script pins the *observable* behaviour instead: both calls return and `LoadedLibraries` holds exactly one row for the library afterwards.

**An error raised inside a plan is wrapped, and the numeric code is in the detailed description but not in the message.** Measured: `E.Code` is 104229 (the wrapping `RuntimeException`) for every error in this script, `E.Message` carries the inner message plus the document locator but no code, and `E.GetDetailedErrorDescription()` flattens the chain and prints each link as `Application:<code>`. Where the ticket names a numeric code the script asserts on both the code and the message text inside the detailed description; where it does not, the script asserts on `E.Message`. The script's header comment records this so the next person does not reach for `E.Code`.

## What the script asserts, section by section

| Section | Asserted | New ground |
|---|---|---|
| Settings | a row inserted into `LibrarySettings` reads back through `LibrarySetting(library, name)`; an `update` of `Value` changes it; an `update` of `Name` renames it, leaving no row under the old name and carrying the value across; a `delete` removes it | `AddLibrarySetting`, `UpdateLibrarySetting` (both arms - update in place and remove-then-add), `RemoveLibrarySetting`, and the `LibrarySettings` table's three update handlers |
| Resolving a setting by name | with `AdminSetC` requiring `AdminSetA` and `AdminSetB` and the session's current library set to `AdminSetC`: a setting defined only on `AdminSetA` resolves to its value; a setting no library in the path defines returns nil; a setting defined on both raises `Setting "Theme" is defined in at least the following libraries: AdminSetA, AdminSetB.`; `SetLibrary('General')` afterwards leaves `LibraryName()` as `General` | `SystemLibrarySettingNode.ResolveSetting`, all three of its outcomes (33 lines, previously 0 %) |
| Detach and refresh | `DetachLibrary` takes an unregistered library out of `Libraries`; `RefreshLibraries()` puts it back from the folder detach left on disk; `DropLibrary` then succeeds | `SystemDetachLibraryNode`, `LibraryUtility.DetachLibrary`, `SystemRefreshLibrariesNode`, and the attaching half of `LibraryUtility.RefreshLibraries` |
| The three obsolete operators | `LoadLibrary`, `UnloadLibrary` and `SaveLibraryCatalog` each raise, and each message names the operator to use instead | three nodes previously at 0 % |
| Re-registering | `RegisterLibrary` on a registered library returns, `EnsureLibraryRegistered` on it returns, and `LoadedLibraries` then holds exactly one row for it | the early-return branches of `SystemRegisterLibraryNode` and `SystemEnsureLibraryRegisteredNode` |
| The administrator errors | dropping a registered library → 113196; detaching a registered library → 113231; unregistering a library that is not registered → 113242; dropping a library that does not exist → 113104 `Object "AdminNoSuchLibrary" not found.`; registering a library that does not exist → 113104. Each pinned by both code and message | five error paths, and the codes as a compatibility surface |
| File references | insert into `LibraryFiles` → the row is there with `IsAssembly` false and the file is in `GetLibraryDescriptor`; delete → gone from both; `SetLibraryDescriptor` with a file reference → the row appears; with an empty file list → the row goes | `AddLibraryFile`, `RemoveLibraryFile`, the `LibraryFiles` table, and the descriptor's `Files` round trip |
| Required and dependent libraries | with `AdminDepD` → `AdminDepC` → {`AdminDepA`, `AdminDepB`}: `RequiredLibraries('AdminDepD')` is 3 rows with `AdminDepC` at level 1 and `AdminDepA` at level 2, `RequiredLibraries('AdminDepD', false)` is 1 row; `DependentLibraries('AdminDepA')` is 2 rows with `AdminDepC` at level 1 and `AdminDepD` at level 2, `DependentLibraries('AdminDepA', false)` is 1 row | the `Level` column and the recursive/non-recursive split of both operators (78.1 % before) |

Two things a reader would otherwise take for test bugs are recorded as comments in the script: `SetLibrary` goes back to `General` and not to `TestFramework.Coverage.Scripts`, which holds this document but is not registered on the coverage server; and the detached library is re-attached before being dropped, because a detached library is not in the catalog and `DropLibrary` on it would raise while leaving the folder behind.

One more measured fact, learned the hard way and worth knowing: **`FileReference(const AName : Name, ...)` validates its argument as an identifier at parse time**, so a file name with a hyphen (`admin-notes.txt`) fails the whole script at compile with `"admin-notes.txt" is not a valid identifier` before a single statement runs. The script uses `AdminNotes.txt` and `AdminManual.txt`. Dots are fine; hyphens are not.

## The `NOTE:` comment

`src/engine/Server/Runtime.Instructions/LibraryNodes.cs` carried `// operator RefreshLibraries(const ALibraryName : Name);` above `SystemRefreshLibrariesNode`. There is no such signature: `src/engine/DAE/Schema/SystemCatalog.d4:894` declares `RefreshLibraries()` with no arguments, and the node ignores `arguments` entirely. The comment now says so, and says that the declaration stays as it is because the signature is a compatibility surface. The operator and its declaration were not touched.

## What is left uncovered, and why

- **`RefreshLibraries`'s removing half** - `RemoveLibrary`, `EnsureLibraryUnregistered` and the `oldLibraries` loop, which drop a library whose folder has disappeared - is still unrun. Reaching it needs a folder deleted behind the server's back, which a D4 script cannot do. It belongs to `debt-library-attach-from-a-directory`; the script says so in a comment at the detach section rather than leaving the reader to wonder.
- **`AttachLibrary` and `AttachLibraries`** (attaching from a named directory) are untouched here - `debt-library-attach-from-a-directory`.
- **The upgrade machinery** (`SystemUpgradeLibraryNode`, `SystemUpgradeLibrariesNode`, `UpgradeUtility`, which is most of the remaining gap in the file) is untouched - `debt-library-upgrades-coverage`.
- **`LibraryFileEnvironments`** and the four `...LibraryFileEnvironment` arms are untouched. The ticket did not name them, and they are the knob that makes `RemoveLibraryFile` refuse (`File '<name>' in library '<library>' cannot be removed because it has environments defined.`) - so the refusal branch of `RemoveLibraryFile` is also still unrun. This is the clearest remaining gap in the administration surface and is a candidate for a follow-up, but no ticket was filed for it: it is one table and four small methods, and whether it earns its own ticket is a judgement a reviewer is better placed to make than this stage is.
- **Real assembly loading** stays with `tests/DAE.Server.Tests/LibraryLoadingTest.cs`, as the ticket directs. The script's file references are deliberately `IsAssembly` false on an unregistered library, so `RegisterLibraryFiles` is never asked to load anything.
- **`LibraryFilesCoverage.d4` and `Register.d4` were not touched**, nor were their manifest entries - `test-pre-3-1-scripts-policy` owns whether they stay.

## Tests

| Test | What it verifies |
|---|---|
| `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4` (coverage script, manifest `pass`) | the eight-section table above: library settings can be inserted, changed, renamed and deleted and read back by name; a setting resolves through the current session's library path and is ambiguous when two libraries at the same level define it; a library can be detached and re-attached by a directory rescan; the three obsolete operators refuse with the right advice; registering an already-registered library is a no-op; the five errors an administrator meets carry their documented codes and messages; a file reference round-trips through both the `LibraryFiles` table and the library descriptor; and `RequiredLibraries`/`DependentLibraries` report levels and respect the recursion flag |

No NUnit test added, and none is owed: the whole subject is reached from D4, and a coverage script through the in-process harness is the lowest layer that shows it.

## Validation run

`dotnet build Dataphor.slnx` - 0 errors, twice: a full build before the script was first run (143 warnings, all of them the pre-existing `CA1416`/`CS0618` set) and an incremental one after the `NOTE:` comment was added (26 warnings, the same set from the projects it rebuilt). No new warning in either. Then, targeted, exactly the filter the ticket named:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=LibraryAdministration|Name=LibraryCoverage|Name=LibraryDependencies|Name=LibraryRename" --logger "console;verbosity=normal"
```

4 passed, 0 failed, 0 skipped; 19.6 s total, of which the template instance is 8.6 s and each script case 2 s. `LibraryAdministration` measured 2 s and 3 s across three runs on this slow development laptop in Debug, so the ticket's estimate holds and part 2 of the three-part split (names `[A-Ra-r]`, 3 m 28 s at best) gains a couple of seconds.

The three part commands were **not** run: the change touches no build or test infrastructure, no `Directory.Build.props` or `Directory.Packages.props`, not the coverage runner or manifest format, and no lexer, parser or compiler code. CI runs the whole suite after the push.

No pre-existing failure was met, so `tickets/.pre-existing-error.md` was not written. `docs/` was not edited, so `tools/docs-size-check/check_size.py` was not run.

## For the reviewer

Worth re-deriving rather than taking on trust:

- **the two row counts and the levels in the last section** (3/1 for `RequiredLibraries`, 2/1 for `DependentLibraries`), against `SystemRequiredLibrariesNode.PopulateRequiredLibraries` and `SystemDependentLibrariesNode.PopulateDependentLibraries` in `src/engine/Server/Runtime.Instructions/CatalogNodes.cs`. The counts were derived from that code and then confirmed by the passing run; an error in the derivation would have shown as a failure, but the *shape* of the chain is the thing that makes 3 and 1 different numbers, and a reviewer should check that the chain is the shape the comment claims;
- **that `IndexOf(detail, '<code>')` cannot match something other than the error code** - the detailed description also carries file paths and `:line NNN` fragments. Six assertions depend on this. Each also checks the message text, so a coincidental digit match alone cannot make an assertion pass with the wrong error, but a reviewer may prefer walking `E.InnerError` to a text search;
- **whether `LibraryAlreadyRegistered` (113187) being unreachable from all three callers deserves a `NOTE:` at the throw site**, or a ticket, or neither;
- **whether `LibraryFileEnvironments` and the refusal branch of `RemoveLibraryFile` should be a follow-up ticket** (see *What is left uncovered*).

The ticket's own instructions were followed on the two points it was firm about: no explicit transaction anywhere in the script, and `LibraryFilesCoverage.d4` and `Register.d4` left exactly as they were.
