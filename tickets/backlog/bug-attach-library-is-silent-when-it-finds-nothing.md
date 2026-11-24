description: Telling the server to bring an application in from a folder reports success even when it found nothing there, so a deployment can finish cleanly with the application not installed.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Schema/LibraryUtility.cs, src/engine/Server/Runtime.Instructions/LibraryNodes.cs, tests/DAE.Server.Tests/LibraryAttachTest.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: Attaching is arguably meant to be a declarative "make sure this is in the list" step, and a deployment script that cares can check the catalog afterwards; adding an error changes what a D4 operator does, which needs a migration note and could break an installer that attaches optimistically over several candidate folders.
----
## What happens

A Dataphor *library* is the folder an application ships in: `<Name>/` holding `<Name>.d4l` and its documents. `AttachLibrary(AName, ADirectory)` is the operator a deployment calls to tell a running server about such a folder. When the folder holds no descriptor for the name it was given, the operator **does nothing and raises nothing**. The caller is told the attach succeeded; the library is not in the server's list.

Two ways a deployment reaches that:

- the directory is right but holds no `<Name>.d4l` (wrong folder, or an incomplete copy);
- the folder's name and the name being attached differ. The descriptor's file name is built from the *name the operator was asked for*, never from what the folder actually holds, so `AttachLibrary('AcmeApp', '...\AcmeApp-v2')` looks for `AcmeApp-v2\AcmeApp.d4l`, finds nothing, and is silent - even though `AcmeApp-v2\AcmeApp-v2.d4l` is sitting right there.

Both are reachable from the IDE as well as from a script: the `AttachLibrary` catalog view's insert handler (`src/engine/DAE/Schema/SystemCatalog.d4`) calls the same operator from the "Attach Library" form, so a user types a name and a directory, the form reports success, and nothing was attached.

Verified (ran it, saw it): `AttachingFromADirectoryWithNoDescriptorDoesNothing` and `AttachingALibraryUnderTheWrongNameDoesNothing` in `tests/DAE.Server.Tests/LibraryAttachTest.cs` pin exactly this - no exception, catalog unchanged. They were written by `debt-library-attach-from-a-directory`, which deliberately recorded the behaviour rather than changing it.

By contrast, pointing `AttachLibraries` (the plural, directory-scanning operator) at a directory that does not exist *does* raise, and the message names the path. So the engine is already inconsistent about this: a mistyped path is diagnosed in one operator and swallowed in the other.

## Why the obvious fix is not a one-liner

The silence is in a method the server also uses while starting up:

```
LibraryUtility.AttachLibrary(Program program, string libraryName, string libraryDirectory, bool isAttached)
```

It resolves the descriptor, and when there is none it returns without complaint. The `isAttached: true` caller is `ServerCatalogDeviceSession.AttachLibraries()`, which runs at every start over the directories recorded in the catalog store. If that call raised, a server whose library folder had been deleted or moved behind its back would **fail to start** - which is worse than the problem being fixed here.

So a fix has to separate the two callers: raise on the operator path (`isAttached` false, or in `SystemAttachLibraryNode` itself) and keep the startup path tolerant, probably logging what it could not re-attach instead. Whether the startup path should report the libraries it dropped is part of the same decision.

## Expected behaviour

Attaching a library whose descriptor is not found in the given directory should fail, saying which file it looked for, so that a deployment script or an installer stops rather than continuing with an application that is not installed. Starting a server should still tolerate a recorded directory that has gone.

This changes what a D4 operator does, so it needs a new error code, an entry in `docs/migration-notes.md`, and the two pinning test cases above re-pointed at the new behaviour in the same change.
