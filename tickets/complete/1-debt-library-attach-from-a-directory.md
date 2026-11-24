description: A characterization test now covers bringing an application into a running server from a folder on disk - the deployment step that had never been run by any test.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/LibraryAttachTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4, tickets/backlog/bug-attach-library-is-silent-when-it-finds-nothing.md, tickets/backlog/debt-multiple-library-directories-are-untested.md
difficulty: medium
----
## What landed

One new test fixture, `tests/DAE.Server.Tests/LibraryAttachTest.cs` (314 lines, 8 cases), the one `<Compile Include="LibraryAttachTest.cs" />` entry it needs in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`, and three lines added to an existing coverage script, `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4`. **No engine change, no coverage-manifest change, no documentation change.** The engine code the fixture covers - `SystemAttachLibraryNode`, `SystemAttachLibrariesNode`, `SystemDetachLibraryNode`, `LibraryUtility.AttachLibrary` (both overloads), `LibraryUtility.DetachLibrary`, `LibraryUtility.RefreshLibraries`, `LibraryUtility.GetAvailableLibrary`, `ServerCatalogDeviceSession.SetLibraryDirectory` / `DeleteLibraryDirectory` / `AttachLibraries` - is untouched.

The fixture is a characterization test, which is what the project's testing rule asks of a subsystem being re-hosted with no coverage. The behaviour it records is the specification: nothing in it was chosen, it was all observed by running.

## How every case builds what it attaches

There is one helper chain, `CreateLibraryFolder` then `DeployLibraryFolder`, and every case goes through it. It does what a deployment does, using the server itself rather than a hand-written descriptor:

1. `CreateLibrary(...)` - the server writes `Libraries\<Name>\<Name>.d4l` in its own library directory.
2. `DeployLibraryFolder` then asserts the library's `Directory` column is **empty**. This is the fixture's reason for existing in C# at all: a library in the default library directory has no path in the catalog, so a D4 script has nothing to hand the attach operators. If that ever changes, this assertion fails and the work can move to a coverage script.
3. `DetachLibrary(...)` - the library leaves the catalog, the folder stays on disk.
4. The folder is moved to `<instance directory>\Deployment\<Name>`, a sibling of `Libraries\` that the server does not scan.

So what each case attaches is a real descriptor the server itself wrote, sitting where a deployment would put it. No library has files - an attached library with a file marked as an assembly would load it on registration and a loaded assembly is never unloaded from this process; `LibraryLoadingTest` owns that ground.

Each case gets its own `TestServerInstance` in a `using`, not a shared fixture server, because two of them stop and start the server and all of them rearrange its directory. Everything happens inside that instance's own temporary folder.

## The cases, and what each one pins

| Case | What it pins |
|---|---|
| `AttachedLibraryRegistersFromItsOwnFolder` | `AttachLibrary` from an explicit directory puts the library in `Libraries` with that path in its `Directory` column; the server's own library directory still has **no** folder for it (attached, not copied); and `RegisterLibrary` then succeeds out of that folder, reaching `LoadedLibraries`. |
| `AttachedDirectorySurvivesARestart` | Stop, start: the library is still attached, with the same `Directory`, and still registered. This is the only assertion in the fixture that reaches the stored directory - `AttachLibrary` records it through `SetLibraryDirectory` and `ServerCatalogDeviceSession.AttachLibraries()` re-attaches from it at the next start. A library outside the library directory is reachable no other way after a restart, because the startup scan of that directory never sees it. |
| `AttachLibrariesAttachesWhatIsNewAndSkipsWhatIsAlreadyThere` | Two library folders in one directory; one is attached by name **and registered** first; `AttachLibraries` on the directory then does not raise, both end up in the catalog with their own directories, and the one that was already there is still registered. That skip (`!program.Catalog.Libraries.ContainsName(...)`) is the branch an administrator hits on every re-run of a deployment script. |
| `AttachingFromADirectoryWithNoDescriptorDoesNothing` | Attaching a name whose `.d4l` is not in the given directory raises **nothing** and changes nothing. Pinned, not fixed - see below. |
| `AttachingALibraryUnderTheWrongNameDoesNothing` | The same silence by the route a real deployment takes: `GetAvailableLibrary` builds the file name from the name it was asked for, not from what the folder holds, so `AttachLibrary('A', '...\B')` looks for `B\A.d4l`. Neither the name asked for nor the name on disk is attached. |
| `DetachingAnAttachedLibraryLeavesItsFolderAndForgetsItsDirectory` | Detaching a library that came from an explicit directory removes the recorded directory as well as the catalog row (`DeleteLibraryDirectory`): the folder is still on disk, but a restart does **not** bring the library back - unlike a library in the default library directory, which the startup scan re-attaches. The folder is then attached again, which is what makes a detach different from a drop. |
| `RefreshLibrariesForgetsAMissingDefaultLibraryAndKeepsAnAttachedOne` | Added by the review. Both folders are deleted behind the server's back and `RefreshLibraries` is run: the library that lived in the server's own library directory is dropped from the catalog, and the library attached from a folder of its own is kept. The removal only ever considers a library whose `Directory` is empty, so an attached library whose deployment folder has gone keeps a catalog entry pointing at nothing, and only `DetachLibrary` clears it. |
| `AttachLibrariesOnADirectoryThatDoesNotExistReportsThePath` | `AttachLibraries` on a missing directory surfaces an I/O exception, not a Dataphor error: `RuntimeError` (104229) wrapping a `DirectoryNotFoundException`, and the message names the path. |

`LibraryAdministration.d4` gained one check in the block that already pins the refused detach: after `DetachLibrary` raises `CannotDetachRegisteredLibrary` (113231), the library is still in the catalog - a refused detach leaves nothing half-removed.

## Two behaviours pinned rather than fixed

**1. Attaching from a directory with no descriptor raises nothing** (`AttachingFromADirectoryWithNoDescriptorDoesNothing`, `AttachingALibraryUnderTheWrongNameDoesNothing`). The parent plan ticket expected an error code here; there is none. `LibraryUtility.AttachLibrary(program, name, directory, false)` resolves the descriptor through `GetAvailableLibrary`, which returns null when `<directory>\<name>.d4l` is absent, and the operator then returns normally. The caller is told the attach succeeded.

This does look like a defect worth its own ticket, and one is filed: `tickets/backlog/bug-attach-library-is-silent-when-it-finds-nothing.md`. It is not fixed here because the same method is what `ServerCatalogDeviceSession.AttachLibraries()` calls at **every server start**, over the directories the catalog store recorded. If the null case raised, a server whose library folder had been deleted or moved behind its back would fail to start - worse than the problem. A fix has to separate the operator path from the startup path, which makes it a design change with a new error code and a migration note, not a one-liner.

**2. `AttachLibraries` on a directory that does not exist** reports `RuntimeError` (104229) over a `DirectoryNotFoundException` whose message is `Could not find a part of the path '<the path>'.`. This one is readable - an administrator who mistypes a path is told which path - so it is recorded and not filed. The engine is inconsistent about the pair: the plural, directory-scanning operator diagnoses a bad path while the singular one swallows it. That observation is in the backlog ticket above rather than in a second ticket, because the fix is at the same site.

## Review findings

**Read first, summary second.** The implement diff (`ticket(implement): debt-library-attach-from-a-directory`) was read before the handoff, together with the engine it covers (`LibraryUtility.AttachLibrary`/`DetachLibrary`/`RefreshLibraries`/`GetAvailableLibrar*`, `SystemAttach*`/`SystemDetachLibraryNode`, `ServerCatalogDeviceSession.AttachLibraries`, `Server.InternalLoadAvailableLibraries`/`InternalInitializeCatalog`) and the neighbouring `LibraryAdministration.d4`.

**Fixed in this pass (minor):**

- **A duplicated test, removed.** `DetachingARegisteredLibraryIsRefused` pinned `CannotDetachRegisteredLibrary` (113231) and its message - which `LibraryAdministration.d4`'s `AdminErrors` block already pins, by code and by the same message string, at the lower layer the project's testing rule prefers. The refusal does not depend on where the library came from (`DetachLibrary` checks `IsLoadedLibrary` before it looks at any directory), so the C# case was a second copy at a higher layer costing a server of its own. Removed, and its one novel assertion - the library is still in the catalog after the refusal - added to the script's existing block, which is three lines rather than five seconds.
- **An uncovered branch the sibling ticket handed to this one, added.** `LibraryAdministration.d4` says in so many words that the other half of `RefreshLibraries` - dropping a library whose folder has gone - "needs a folder deleted behind the server's back and belongs to debt-library-attach-from-a-directory". It was not covered. `RefreshLibrariesForgetsAMissingDefaultLibraryAndKeepsAnAttachedOne` now covers it, and covers the attach-specific asymmetry at the same time: `RemoveLibrary` is only ever reached for a library whose `Directory` is empty, so an attached library survives the refresh while a default-directory one does not. That path (`oldLibraries` / `RemoveLibrary`) had no coverage at all. The case asserts the premise (the library is in the catalog before the folder is deleted) so that the negative assertion cannot pass vacuously.
- **Duplication in the fixture's own helpers.** The library-directory path `Path.Combine(instance.Directory, LibrariesFolderName, name)` was spelled out in three places and the `CreateLibrary` descriptor literal in one place that two cases now need. Extracted `DefaultLibraryFolder` and `CreateLibraryFolder`; `DeployLibraryFolder` is now the deploy step alone.
- **Inconsistent quoting of D4 arguments.** Paths went through the `Literal` helper and library names were interpolated raw into the script text, in the same statements. All string arguments now go through `Literal`. No behaviour change - the names are fixture constants - but the inconsistency invited the question of which one was right.
- **One assertion that tested the fixture, removed.** `AttachLibrariesAttachesWhatIsNewAndSkipsWhatIsAlreadyThere` asserted that the two deployed folders share a parent directory, three lines above handing that same directory to the scan. It restated `DeployLibraryFolder` rather than checking the engine.

**Filed (major):** one ticket, `tickets/backlog/debt-multiple-library-directories-are-untested.md`. The `LibraryDirectories` server setting may name several folders, the engine treats the first folder differently from the rest (it records a path for a library only from the second folder onward), and nothing in the suite ever sets more than one. Three consequences are unverified, the sharpest being that `DetachLibrary` does not survive a restart for a library in a later folder because the startup scan finds it again. It is filed rather than fixed here because covering it needs `TestServerInstance` taught to build a server with two library folders - test infrastructure, not a case - and because it is a configuration this ticket's subject does not use. The implement handoff had already named it a known gap; this agrees and gives it a home. Site-claim grep over `tickets/backlog|fix|plan|implement|review` for `TestServerInstance`, `GetAvailableLibraries` and `LibraryDirectories` found nothing open that touches it.

**Weighed and left as filed:** `bug-attach-library-is-silent-when-it-finds-nothing`, which the implement stage filed and asked for a second opinion on. `severity: wrong-result` is right - the operator reports success for work it did not do, which is worse than an edge case and short of corruption. `likelihood: unusual` is right - it takes a mistyped path or a folder whose name and descriptor disagree. `backlog/` rather than `fix/` is right: the shape is settled (raise on the operator path, keep the startup path tolerant) but it needs a new error code and a migration note, so the timing is a human's call, which is exactly what `backlog/` is for.

**Tripwires: none recorded.** Nothing here is of the "fine now, becomes work if X" kind. The two behaviours the fixture pins rather than fixes are reachable today, which is why one is a ticket and the other is recorded in that ticket's prose; the `LibraryDirectories` gap is a present hole rather than a conditional one, which is why it is a ticket and not a comment.

**Checked and found clean:**

- **Happy path and the restart path** were re-read against the engine. `AttachLibrary` records a directory only when `library.Directory` is non-empty and `isAttached` is false, which is what makes the restart assertion meaningful rather than circular; `AttachLibraries()` at start reads the recorded rows before attaching, which the fixture exercises by stopping and starting.
- **Error paths.** `FindError` walks the whole `InnerException` chain and the two cases that use it report the full exception when no matching code is found, so a changed wrapping fails with something readable rather than a null-reference.
- **Resource cleanup.** Every case holds its `TestServerInstance` in a `using`; the instance stops the server and deletes its directory, and everything the fixture writes (`Libraries\`, `Deployment\`) is inside that directory. Nothing points at the runtime layout or the repository. No file handle is left on a folder the fixture deletes - the two libraries whose folders are deleted behind the server's back are deliberately left unregistered.
- **The `Literal` helper's claim** that the D4 lexer has no escape character was verified against `src/engine/DAE/Language/Lexer.cs`: a quote inside a string is written twice and nothing else is special, so a Windows path with backslashes goes in as it is. Doubling the quote is the correct and complete escaping.
- **Type safety.** The casts on `Evaluate` results (`(bool)`, `(string)`) are over expressions that cannot be nil - `exists(...)` and a `Directory` column the engine fills with the empty string, which the fixture's own premise assertion pins.
- **Source hygiene.** 314 lines, well inside anything worth splitting; no byte-order mark and CRLF endings preserved in both edited files; the comments say why rather than narrating statements. The `Schema` using alias became unused when the duplicated case went and was removed with it.
- **Documentation: nothing was owed, and this was checked rather than assumed.** The change adds no public type or member, so `docs/migration-notes.md` takes no entry; it adds no architecture text, so no page under `docs/architecture/` is involved and `tools/docs-size-check/check_size.py` has nothing to check; `tests/DAE.Server.Tests/coverage-manifest.json` lists D4 coverage scripts and `LibraryAdministration` is already in it, so amending that script needs no manifest change and a C# fixture needs no entry at all. The two sibling tickets in this split landed with no documentation change either.

**Not covered, deliberately** - the honest remainder, for whoever reads this next:

- **`AttachLibraries`' second skip clause.** The node skips a library whose name is in `program.Catalog.Libraries` *or* in `program.Catalog` (any catalog object of that name). Only the first arm is covered; contriving a catalog object that collides with a library folder's name to reach the second is not worth a case, and the two arms are one `if`.
- **Attaching a library with requisites or documents.** Every library here is empty by design. Attaching a library whose requisites are absent, and registering a library with real content out of an attached folder, are both untested.
- **The `AttachLibrary` / `AttachLibraries` catalog views.** `src/engine/DAE/Schema/SystemCatalog.d4` defines an insert handler on each, which is how the IDE's "Attach Library" form calls these operators. The fixture calls the operators directly; the handlers are one statement each and are not exercised.
- **A multi-directory `LibraryDirectories` setting** - now `debt-multiple-library-directories-are-untested`.

## Verification

Build, then the implement stage's filter widened by one ring - the other library fixture and every library coverage script - all from the repository root:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~LibraryAttachTest|FullyQualifiedName~LibraryLoadingTest|Name=LibraryAdministration|Name=LibraryCoverage|Name=LibraryDependencies|Name=LibraryFilesCoverage|Name=LibraryRename" --logger "console;verbosity=normal"
```

Result: **16 passed, 0 failed**, 50.7 s. `LibraryAdministration`, `LibraryCoverage`, `LibraryDependencies`, `LibraryFilesCoverage` and `LibraryRename` are the coverage scripts for every engine area the diff touches, including the one this pass amended; `LibraryLoadingTest` is the neighbouring C# fixture. The `Could not delete ...Template-1` and `...LibraryLoadingTest-15` lines in that output are the documented behaviour for a directory holding a loaded library assembly, not failures.

After the premise assertion was added to the new case, the build and the fixture were run once more: **8 passed, 0 failed**, 30.7 s. Build: 0 errors both times. No pre-existing failure was met anywhere; `tickets/.pre-existing-error.md` was not written. The full suite was not run - the change is a test fixture and one coverage script, not infrastructure, and CI runs everything after the push.
