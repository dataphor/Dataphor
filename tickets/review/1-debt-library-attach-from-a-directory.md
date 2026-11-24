description: A characterization test now covers bringing an application into a running server from a folder on disk - the deployment step that had never been run by any test.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/LibraryAttachTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tickets/backlog/bug-attach-library-is-silent-when-it-finds-nothing.md
difficulty: medium
----
## What landed

One new test fixture, `tests/DAE.Server.Tests/LibraryAttachTest.cs` (289 lines, 8 cases), and the one `<Compile Include="LibraryAttachTest.cs" />` entry it needs in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`. **No engine change, no coverage-manifest change, no documentation change.** The engine code the fixture covers - `SystemAttachLibraryNode`, `SystemAttachLibrariesNode`, `SystemDetachLibraryNode`, `LibraryUtility.AttachLibrary` (both overloads), `LibraryUtility.DetachLibrary`, `LibraryUtility.GetAvailableLibrary`, `ServerCatalogDeviceSession.SetLibraryDirectory` / `DeleteLibraryDirectory` / `AttachLibraries` - is untouched.

The fixture is a characterization test, which is what the project's testing rule asks of a subsystem being re-hosted with no coverage. The behaviour it records is the specification: nothing in it was chosen, it was all observed by running.

## How every case builds what it attaches

There is one helper, `DeployLibraryFolder`, and every case goes through it. It does what a deployment does, using the server itself rather than a hand-written descriptor:

1. `CreateLibrary(...)` - the server writes `Libraries\<Name>\<Name>.d4l` in its own library directory.
2. It then asserts the library's `Directory` column is **empty**. This is the fixture's reason for existing in C# at all: a library in the default library directory has no path in the catalog, so a D4 script has nothing to hand the attach operators. If that ever changes, this assertion fails and the work can move to a coverage script.
3. `DetachLibrary(...)` - the library leaves the catalog, the folder stays on disk.
4. The folder is moved to `<instance directory>\Deployment\<Name>`, a sibling of `Libraries\` that the server does not scan.

So what each case attaches is a real descriptor the server itself wrote, sitting where a deployment would put it. No library has files - an attached library with a file marked as an assembly would load it on registration and a loaded assembly is never unloaded from this process; `LibraryLoadingTest` owns that ground.

Each case gets its own `TestServerInstance` in a `using`, not a shared fixture server, because three of them stop and start the server and all of them rearrange its directory. Everything happens inside that instance's own temporary folder.

## The cases, and what each one pins

| Case | What it pins |
|---|---|
| `AttachedLibraryRegistersFromItsOwnFolder` | `AttachLibrary` from an explicit directory puts the library in `Libraries` with that path in its `Directory` column; the server's own library directory still has **no** folder for it (attached, not copied); and `RegisterLibrary` then succeeds out of that folder, reaching `LoadedLibraries`. |
| `AttachedDirectorySurvivesARestart` | Stop, start: the library is still attached, with the same `Directory`, and still registered. This is the only assertion in the fixture that reaches the stored directory - `AttachLibrary` records it through `SetLibraryDirectory` and `ServerCatalogDeviceSession.AttachLibraries()` re-attaches from it at the next start. A library outside the library directory is reachable no other way after a restart, because the startup scan of that directory never sees it. |
| `AttachLibrariesAttachesWhatIsNewAndSkipsWhatIsAlreadyThere` | Two library folders in one directory; one is attached by name **and registered** first; `AttachLibraries` on the directory then does not raise, both end up in the catalog with their own directories, and the one that was already there is still registered. That skip (`!program.Catalog.Libraries.ContainsName(...)`) is the branch an administrator hits on every re-run of a deployment script. |
| `AttachingFromADirectoryWithNoDescriptorDoesNothing` | Attaching a name whose `.d4l` is not in the given directory raises **nothing** and changes nothing. Pinned, not fixed - see below. |
| `AttachingALibraryUnderTheWrongNameDoesNothing` | The same silence by the route a real deployment takes: `GetAvailableLibrary` builds the file name from the name it was asked for, not from what the folder holds, so `AttachLibrary('A', '...\B')` looks for `B\A.d4l`. Neither the name asked for nor the name on disk is attached. |
| `DetachingARegisteredLibraryIsRefused` | `CannotDetachRegisteredLibrary` (113231), asserted on the code and on the exact message the engine produces (`Library "X" is registered and cannot be detached.`), and the library is still attached afterwards. |
| `DetachingAnAttachedLibraryLeavesItsFolderAndForgetsItsDirectory` | Detaching a library that came from an explicit directory removes the recorded directory as well as the catalog row (`DeleteLibraryDirectory`): the folder is still on disk, but a restart does **not** bring the library back - unlike a library in the default library directory, which the startup scan re-attaches (that half is covered from a script by `debt-library-administration-coverage` via `RefreshLibraries`). The folder is then attached again, which is what makes a detach different from a drop. |
| `AttachLibrariesOnADirectoryThatDoesNotExistReportsThePath` | `AttachLibraries` on a missing directory surfaces an I/O exception, not a Dataphor error: `RuntimeError` (104229) wrapping a `DirectoryNotFoundException`, and the message names the path. |

## Two behaviours pinned rather than fixed

The ticket asked for these to be named explicitly.

**1. Attaching from a directory with no descriptor raises nothing** (`AttachingFromADirectoryWithNoDescriptorDoesNothing`, `AttachingALibraryUnderTheWrongNameDoesNothing`). The parent plan ticket expected an error code here; there is none. `LibraryUtility.AttachLibrary(program, name, directory, false)` resolves the descriptor through `GetAvailableLibrary`, which returns null when `<directory>\<name>.d4l` is absent, and the operator then returns normally. The caller is told the attach succeeded.

**This does look like a defect worth its own ticket, and one is filed:** `tickets/backlog/bug-attach-library-is-silent-when-it-finds-nothing.md` (`severity: wrong-result`, `likelihood: unusual`, `repro: verified`). It is not fixed here for a reason that is worth knowing before anyone tries: the same method is what `ServerCatalogDeviceSession.AttachLibraries()` calls at **every server start**, over the directories the catalog store recorded. If the null case raised, a server whose library folder had been deleted or moved behind its back would fail to start - worse than the problem. A fix has to separate the operator path from the startup path, which makes it a design change with a new error code and a migration note, not a one-liner. The ticket says all of that, and says the two cases above must be re-pointed in the same change.

The reviewer may reasonably disagree about the severity or about filing it to `backlog/` rather than `fix/`; the analysis is in the ticket, and nothing in the fixture depends on the outcome.

**2. `AttachLibraries` on a directory that does not exist** reports `RuntimeError` (104229) over a `DirectoryNotFoundException` whose message is `Could not find a part of the path '<the path>'.`, with the plan's `(program:<guid>@1:0)` suffix. **This one is readable** - an administrator who mistypes a path is told which path - so it is recorded and not filed. It is worth noting the engine is inconsistent about this pair: the plural, directory-scanning operator diagnoses a bad path while the singular one swallows it. That observation is in the backlog ticket above rather than in a second ticket, because the fix is at the same site.

## Known gaps

Honest list of what this fixture does **not** cover, for the reviewer to weigh rather than discover:

- **`AttachLibraries`' second skip clause.** The node skips a library whose name is in `program.Catalog.Libraries` *or* in `program.Catalog` (a catalog object of that name). Only the first arm is covered; making a catalog object collide with a library name to reach the second was judged not worth a case.
- **A multi-directory `LibraryDirectories` setting.** The three-argument `GetAvailableLibraries(instanceDirectory, libraryDirectory, libraries)` splits its argument on `;` and sets `Directory` only for the second and later entries - the behaviour that makes the `Directory` column empty for a default-directory library. Nothing here sets a semicolon-separated library directory, so that split is still uncovered.
- **Attaching a library with requisites or documents.** Every library here is empty by design (no files, no requisites, no documents). Attaching a library whose requisites are absent, and registering a library with real content out of an attached folder, are both untested.
- **`AttachLibrary` reached from the `AttachLibrary` / `AttachLibraries` catalog views** (`src/engine/DAE/Schema/SystemCatalog.d4` defines an insert handler on each, which is how the IDE's "Attach Library" form calls these operators). The fixture calls the operators directly; the views' handlers are not exercised.
- **Cost.** Eight cases, eight servers, three of them started twice: about 40 s for the fixture on this machine. That is the price of needing a server whose directory can be rearranged; it is the shape the ticket called for.

## Verification

Build, then the ticket's filter, both from the repository root:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~LibraryAttachTest|FullyQualifiedName~LibraryLoadingTest" --logger "console;verbosity=normal"
```

Result: **11 passed, 0 failed** (the 8 new cases plus `LibraryLoadingTest`'s 3), 39.6 s. `Could not delete ...LibraryLoadingTest-9` in that output is the documented behaviour for a directory holding a loaded library assembly, not a failure.

Because the fixture covers library administration, the neighbouring coverage scripts were run as well, with the same build:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=LibraryAdministration|Name=LibraryCoverage|Name=LibraryDependencies|Name=LibraryFilesCoverage|Name=LibraryRename" --logger "console;verbosity=normal"
```

Result: **5 passed, 0 failed**, 20.6 s (template built in 7.1 s). No pre-existing failure was met anywhere; `tickets/.pre-existing-error.md` was not written. The full suite was not run - CI runs it after the push.

## For the reviewer

Worth a second opinion on:

- Whether the `Directory` column being empty for a default-directory library belongs as an assertion inside the shared helper (where it runs six times) or as a case of its own. It is in the helper because it is the premise the whole fixture rests on, and failing it should fail every case.
- Whether `DetachingAnAttachedLibraryLeavesItsFolderAndForgetsItsDirectory` is one behaviour or two (the folder surviving, and the recorded directory being forgotten). They are asserted together because the restart is the only way to see the second, and paying for a second server to split them looked like the wrong trade.
- The backlog ticket's severity and stage, as above.
