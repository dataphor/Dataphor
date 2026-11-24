description: Everything an administrator does to an installed application after it is working - reading and changing its settings, taking it out of the server and putting it back, and the errors they meet when they get it wrong - has never been exercised by a test, so a change to any of it would break silently.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4, tests/DAE.Server.Tests/coverage-manifest.json, src/engine/Server/Runtime.Instructions/LibraryNodes.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Schema/SystemCatalog.d4
difficulty: medium
----
## What this is

A Dataphor *library* is the folder an application ships in. `src/engine/Server/Runtime.Instructions/LibraryNodes.cs` holds the implementation of the D4 operators that manage one, and three quarters of that file has never run (24.0 % of 854 coverable lines, measured on the full three-part run of 2026-11-21 at commit `f6200026`, `docs/coverage-report.md`). The parent plan ticket `debt-library-upgrades-coverage` split that gap in three. This piece is the administration surface: library *settings*, taking a library out of the server's list and putting it back, the three operators that now only say "use the other one instead", and the errors an administrator meets. The upgrade machinery is `debt-library-upgrades-coverage`; attaching a library from a directory is `debt-library-attach-from-a-directory`.

Add one D4 coverage script, `LibraryAdministration.d4`, in `tests/libraries/TestFramework.Coverage.Scripts/Documents/`, with its entry in `tests/DAE.Server.Tests/coverage-manifest.json`. No C# and no engine change, apart from one comment named in the TODO list. Every library the script touches is one it creates itself.

## What is already covered, so the script must not restate it

`tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryCoverage.d4` already covers: the `System` and `General` libraries refusing to be modified, renamed, unregistered or dropped; `CreateLibrary`, `RenameLibrary`, `DropLibrary`, `GetLibraryDescriptor`, `SetLibraryDescriptor`, `RegisterLibrary`, `UnregisterLibrary`; the default device name, including inheriting it from a required library; rename through `SetLibraryDescriptor`; the requisite version mismatch; and the four refusals on a registered library (rename, version change, requisite removal, circular reference). `LibraryDependencies.d4` covers adding a requisite through the `LibraryRequisites` table. `tests/DAE.Server.Tests/LibraryLoadingTest.cs` covers loading a library with a real assembly. None of that is to be repeated.

## What the script must cover

Each item below is new ground.

**Settings.** A library's settings are rows of the `LibrarySettings` catalog table; writing them goes through `SystemSetLibraryDescriptorNode.AddLibrarySetting` / `UpdateLibrarySetting` / `RemoveLibrarySetting` by way of `ServerCatalogDeviceSession.InsertLibrarySetting` / `UpdateLibrarySetting` / `DeleteLibrarySetting`. Insert a setting on a library the script created, read it back with the two-argument `LibrarySetting(library, name)`, change its value with an `update` and read it again, rename it with an `update` of the `Name` column (that is the branch in `UpdateLibrarySetting` that removes and re-adds rather than updating in place), then `delete` it and check it is gone. The insert, update and read-back were run during planning and work.

**Resolving a setting by name alone.** `LibrarySetting(name)` (one argument, `SystemLibrarySettingNode`, 33 lines, never run) walks the current session's library name-resolution path and returns the first unambiguous value. Cover both outcomes: a library `C` that requires `A` and `B`, with the setting defined only on `A`, resolves; with the setting defined on both `A` and `B`, which sit at the same level of `C`'s path, it fails with `Setting "<name>" is defined in at least the following libraries: A, B.` (`SchemaException.Codes.AmbiguousLibrarySetting`, 113230). The ambiguous case was run during planning and produces exactly that message. Resolution is relative to the *session's* current library, so the script has to `SetLibrary('C')` first - and `SetLibrary` back to `General` afterwards, not to `TestFramework.Coverage.Scripts`, which is not registered in the coverage server (it is a document holder only, and `SetLibrary` on it raises "Library 'TestFramework.Coverage.Scripts' is not registered").

**Taking a library out and putting it back.** `DetachLibrary(name)` removes a library from the server's list and leaves its folder on disk; `RefreshLibraries()` rescans the library directory and attaches whatever it finds that the catalog does not have. Create a library, register it, unregister it, detach it, check it has left `Libraries`, call `RefreshLibraries()` and check it is back. Validated during planning. `RefreshLibraries` is at 0 % today and this is the only way a script can reach it. Its other half - dropping a library whose folder has disappeared - needs a folder deleted behind the server's back and belongs to `debt-library-attach-from-a-directory`; say so in a comment rather than leaving the reader to wonder.

**The three operators that are now only a message.** `LoadLibrary`, `UnloadLibrary` and `SaveLibraryCatalog` each raise "<name> is obsolete. Use <other> instead." and each is 3 lines at 0 %. Assert that each raises and that the message says so. Validated during planning.

**Registering a library that is already registered is not an error.** `SystemRegisterLibraryNode` checks first and does nothing, so `RegisterLibrary` on a registered library returns quietly rather than raising `LibraryAlreadyRegistered` (113187); `EnsureLibraryRegistered` behaves the same way. The parent ticket assumed this was an error. Pin what it actually does: both calls return, and the library is still registered exactly once.

**The errors an administrator meets.** Error codes are a compatibility surface, so pin the ones a person would actually hit, each as its own small case: dropping a registered library (`CannotDropRegisteredLibrary`, 113196), detaching a registered library (`CannotDetachRegisteredLibrary`, 113231), unregistering a library that is not registered, dropping a library that does not exist, and registering a library that does not exist. For the last three, run the call first and record the message and code the engine actually produces, then write the assertion from that - do not pick a code out of `src/engine/DAE/Schema/SchemaException.cs` and assert it untested.

**File references on a library that is not registered.** `SetLibraryDescriptor` with a descriptor carrying a `FileReference` exercises the `AddLibraryFile` and `RemoveLibraryFile` arms and the `LibraryFiles` catalog table, which nothing reaches today. Do this only while the library is *unregistered*: on a registered library `SetLibraryDescriptor` calls `LibraryUtility.RegisterLibraryFiles`, which tries to load every file marked as an assembly, and the script has no assembly to offer. Add a file reference, read it back through `GetLibraryDescriptor` and through `LibraryFiles`, then remove it and check both agree. This is the ground the empty `LibraryFilesCoverage.d4` was named for; real assembly loading stays with `LibraryLoadingTest.cs`.

**Required and dependent libraries.** With `A`, `B` and `C` set up as above, check `RequiredLibraries('C')` lists `A` and `B`, and `DependentLibraries('A')` lists `C`, in both the one-argument and the two-argument (recursive) forms. These are in `src/engine/DAE/Runtime.Instructions/LibraryNodes.cs` (78.1 %) and are cheap to pin while the libraries exist.

## Edge cases & interactions

- **Order matters between detach and drop.** `DetachLibrary` leaves the folder, which is what makes the `RefreshLibraries` re-attach work; `DropLibrary` deletes the folder. A library that has been detached is no longer in the catalog, so `DropLibrary` on it raises. Re-attach before dropping. Read from `LibraryUtility.DetachLibrary` and `DropLibrary`; the re-attach path was run.
- **The script must drop every library it creates.** Each coverage case gets its own instance directory and its own copy of the library folders (`TestServerInstance`, the template-copy constructor), so a library left behind cannot reach another case - the parent plan ticket's claim that scripts in one run share a library directory is wrong. Clean up anyway: a script that leaves libraries behind cannot be re-run against one server, and its failure messages get harder to read. Verified by inspection of `tests/DAE.Server.Tests/TestServerInstance.cs`.
- **An error raised inside a plan arrives wrapped.** `E.Code` on the caught error is 104229 (the wrapping `RuntimeException`), not the schema code - measured during planning. Assert on text: either `E.Message`, which carried the inner message for every error tried during planning, or `Diagnostics.GetDetailedErrorDescription(E)`, which flattens the whole chain. Where this ticket names a numeric code, assert it by finding the code in that text or by walking `E.InnerError` - never by comparing `E.Code`.
- **Keep the script out of explicit transactions.** Library DDL is not transactional, and `library-ddl-inside-a-transaction` (in `plan/`) may make `DropLibrary` refuse inside an explicit transaction. The script needs no `BeginTransaction`, so it must not have one. That ticket's checklist asks for a survey of the coverage scripts that drop libraries, so state in the handoff that this one drops libraries outside any explicit transaction.
- **No preamble is needed.** Nothing here starts a session of its own, so the harness's default implicit transactions are fine. The upgrade script in `debt-library-upgrades-coverage` does need one; that is a different ticket and a different reason.
- **Leave the two empty scripts alone.** `LibraryFilesCoverage.d4` and `Register.d4` hold only comments and their manifest entries say so. Whether they stay is `test-pre-3-1-scripts-policy`'s call: do not edit, retire or delete them, and do not change their manifest entries.
- **Manifest entries are sorted by script name in ordinal order**, so `LibraryAdministration` goes between `Lexer` and `LibraryCoverage`. A script with no entry, or an entry with no script, fails the run.
- **Which test part the new case lands in.** The three-part agent split puts scripts whose names begin A-R in part 2, whose best measured time is 3 m 28 s (`AGENTS.md`), so one more script is comfortable. A probe script of about this shape took 2 s of case time in Debug on one slow development laptop during planning.

## Verification

The script is the test; no C# test is owed. Build (`dotnet build Dataphor.slnx`), then run the new case with its neighbours:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=LibraryAdministration|Name=LibraryCoverage|Name=LibraryDependencies|Name=LibraryRename" --logger "console;verbosity=normal"
```

Name the filter you used in the handoff. Do not run the whole suite; CI does that after the push.

## TODO

- Write `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4` covering the sections above, in this order: settings; resolving a setting by name; detach and refresh; the three obsolete operators; re-registering; the administrator errors; file references on an unregistered library; required and dependent libraries. Drop every library it creates at the end. Tabs, CRLF, no byte-order mark, as the other scripts in that folder have.
- For each error case, run it once and read the message and code the engine actually produces before writing the assertion.
- Add the manifest entry `{ "script": "LibraryAdministration", "status": "pass" }` in ordinal position, with a `note` only if something about the script needs explaining.
- Add a one-line `NOTE:` comment beside `SystemRefreshLibrariesNode` in `src/engine/Server/Runtime.Instructions/LibraryNodes.cs` recording that the comment above the class describes a `RefreshLibraries(const ALibraryName : Name)` that does not exist: the operator is declared with no arguments in `src/engine/DAE/Schema/SystemCatalog.d4` and the node ignores arguments. Do not change the operator or its declaration - the signature is a compatibility surface.
- Build, run the filter above, and report in the handoff what passed, what you chose to leave uncovered and why, and that the script drops libraries outside any explicit transaction.
