description: Bringing an application into a running server from a folder on disk is how a Dataphor installation is deployed, and nothing has ever tested it, so a deployment could fail in a way no test would catch.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/LibraryAttachTest.cs, tests/DAE.Server.Tests/TestServerInstance.cs, src/engine/Server/Runtime.Instructions/LibraryNodes.cs, src/engine/Server/Schema/LibraryUtility.cs
difficulty: medium
----
## What this is

A Dataphor *library* is the folder an application ships in: `<Name>/` with `<Name>.d4l`, `Documents/` and optionally `Upgrades/`. A server keeps a list of the libraries it knows about. **Attaching** is how a folder that exists on disk but is not in that list gets into it, and it is the deployment step: an installer or an administrator puts the application's folder somewhere and tells the server about it. Three D4 operators do that work and none of them has ever run:

- `AttachLibrary(const AName : Name, const ADirectory : String)` - `SystemAttachLibraryNode`, 0 %.
- `AttachLibraries(const ADirectory : String)` - `SystemAttachLibrariesNode`, 0 %; scans a directory and attaches every library folder in it that the catalog does not already have.
- `DetachLibrary(const AName : Name)` - the reverse; covered from a script by `debt-library-administration-coverage`, so here only its refusal on a registered library and its effect on a library attached from an explicit directory.

This is one of the three pieces the parent plan ticket `debt-library-upgrades-coverage` split into. The other two are D4 coverage scripts; **this one has to be a C# fixture**, and that is a finding from the planning stage rather than a preference: both attach operators take a server-side directory path, and a D4 script has no way to learn one. Measured during planning: for a library in the server's default library directory the `Libraries` catalog table reports `Directory` as the empty string (`LibraryUtility.GetAvailableLibraries` only fills `Directory` in for the second and later entries of a multi-directory `LibraryDirectories` setting), and nothing else in the coverage server's registered libraries exposes a path. A test that knows the instance's own directory can call these operators; a script cannot.

Add `tests/DAE.Server.Tests/LibraryAttachTest.cs`. No engine change.

## How to get a library folder to attach

Do not hand-write a `.d4l`. Let the server write one, then move the folder out from under it:

- Start a `TestServerInstance` (its `Directory` property is the instance's own temporary folder; `Libraries\` beneath it is the server's library directory).
- `CreateLibrary(LibraryDescriptor('AttachTest', VersionNumber(1, 0, 0), "", list(FileReference){}, list(LibraryReference){}))` - the server creates `Libraries\AttachTest\AttachTest.d4l`.
- `DetachLibrary('AttachTest')` - the library leaves the catalog and the folder stays on disk.
- `Directory.Move` the folder to a sibling of `Libraries\`, for example `<instance.Directory>\Elsewhere\AttachTest`.

The folder is now exactly what a deployment presents: a real descriptor the server itself wrote, in a directory the server does not scan. `TestServerInstance` exposes `Server`, `Connect()` and `Directory`, and `tests/DAE.Server.Tests/CatalogStoreTest.cs` shows the stop-then-start idiom on one instance.

## What the fixture must cover

- **`AttachLibrary` from an explicit directory.** After attaching, the library is in `Libraries` and its `Directory` column holds the path that was passed. Then `RegisterLibrary` it from there and check it reaches `LoadedLibraries` - attaching is pointless if the library cannot then be registered out of its own folder.
- **The directory survives a restart.** `LibraryUtility.AttachLibrary` records the path through `ServerCatalogDeviceSession.SetLibraryDirectory`. Stop the server, start it again, and check the library is still in `Libraries` with the same `Directory` and still registered. This is the whole point of attaching rather than copying, and it is the only assertion here that reaches the stored directory at all.
- **`AttachLibraries` scanning a directory.** Put two library folders in one directory (create and detach two libraries, move both), attach one of them by name first, then call `AttachLibraries` on the directory: both end up in the catalog, the already-attached one is untouched, and the call does not raise. `SystemAttachLibrariesNode` skips a library whose name is already in `program.Catalog.Libraries` or in `program.Catalog` - cover that skip, because it is the branch an administrator hits on every re-run of a deployment script.
- **A directory with no library in it.** `LibraryUtility.AttachLibrary(program, name, directory, false)` goes through `GetAvailableLibrary`, which returns null when `<directory>\<name>.d4l` is absent, and the operator then does nothing - **no error**. The parent plan ticket expected an error code here. Pin what it actually does, so the next reader is not surprised: attaching a name that has no descriptor in the given directory leaves the catalog unchanged and raises nothing. If you judge that this silence is a defect worth fixing, do not fix it here - pin it, and say so in the handoff.
- **`DetachLibrary` on a registered library is refused** with `CannotDetachRegisteredLibrary` (113231). Run it once and assert on the message the engine actually produces.
- **`AttachLibraries` on a directory that does not exist.** `GetAvailableLibraries` calls `Directory.GetDirectories`, so this surfaces an I/O exception rather than a Dataphor error. Record what the caller actually sees and assert that - an administrator who mistypes a path should get a diagnosis, and if the result is unreadable that is worth a sentence in the handoff.

## Edge cases & interactions

- **The folder name and the descriptor name must agree.** `GetAvailableLibrary` builds the file name from the *library name it is asked for*, not from what it finds, so `AttachLibrary('Foo', '...\Bar')` looks for `Bar\Foo.d4l` and finds nothing. Worth one small case, because it is the shape of a real deployment mistake and it lands on the silent-no-op path above.
- **A server writes into its library directory.** Everything here happens inside one `TestServerInstance`'s own temporary folder, which is what the fixture must stay within; never point a test at the runtime layout or at the repository (`AGENTS.md`, *Rules that are easy to break*).
- **Assembly loading is out of scope.** An attached library with a file marked as an assembly would load it on registration, and loaded assemblies are never unloaded in this process. Give the attached libraries no files. `tests/DAE.Server.Tests/LibraryLoadingTest.cs` owns assembly loading.
- **Do not attach anything twice under two names.** `program.Catalog.Libraries` is keyed by name, and a second attach of the same name is the skip case above, not a second library.
- **Restart and the catalog store.** The restart assertion needs the server stopped cleanly before it is started again; `CatalogStoreTest` does exactly that and its comment records that the second start reopens the catalog the first created. If the restart turns out to lose the library's directory, that is a real defect and its own ticket - report it in the handoff with the assertion that shows it rather than loosening the test.
- **One fixture, not `InProcessTestFixture`.** The cases here need a server they can stop, start and whose directory they can rearrange, so each case (or a small group) wants its own `TestServerInstance` in a `using`, not a shared fixture server.

## Tests

The fixture is the test. It is a characterization test for a subsystem that is being re-hosted with no coverage, which the project's testing rule asks for. Keep it to one case per behaviour named above; do not add cases for the thin node classes themselves.

## Verification

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~LibraryAttachTest|FullyQualifiedName~LibraryLoadingTest" --logger "console;verbosity=normal"
```

Name the filter you used in the handoff. Do not run the whole suite; CI does that after the push.

## TODO

- Write `tests/DAE.Server.Tests/LibraryAttachTest.cs` with the cases above, each on its own `TestServerInstance`, each library built by the server and moved out of its library directory as described.
- For every error or no-op case, run it once and record what the caller actually sees before writing the assertion.
- Build and run the filter above.
- In the handoff: list the cases, say what each pins, and name explicitly the two behaviours this ticket *pins rather than fixes* - attaching from a directory with no descriptor raises nothing, and whatever `AttachLibraries` on a missing directory turns out to report. Say whether either looks like a defect worth its own ticket.
