description: A server can be told to look for applications in more than one folder, and that setting has never been tested; the server treats applications found in the extra folders differently from the ones in the first folder, in ways nothing checks.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/TestServerInstance.cs, tests/DAE.Server.Tests/LibraryAttachTest.cs, src/engine/Server/Schema/LibraryUtility.cs, src/engine/Server/Server/Server.cs
difficulty: medium
tradeoffs: Most deployments configure one library directory, so this covers a setting few installations use, and covering it means teaching the shared test server fixture to take more than one directory - infrastructure work for one configuration.
----
## What this is

A *library* is the folder an application ships in. A server is told where to look for them by one configuration setting, `LibraryDirectories`, which may name **several** folders separated by semicolons. Nothing in the test suite ever sets more than one, so every behaviour that depends on the difference between the first folder and the rest is unverified.

The difference is real, not incidental. When the server scans its library folders it records a path for a library only if the library came from the **second or later** folder; a library in the first folder is recorded with no path at all. Three consequences follow, none of them tested:

- The `Libraries` catalog table reports a directory for a library in a later folder and the empty string for one in the first folder. A D4 script can therefore read a usable path for the one and not the other, which is the reason `tests/DAE.Server.Tests/LibraryAttachTest.cs` had to be written in C# at all - a fact that holds only for a server with a single library folder.
- `RefreshLibraries` removes from the server's list every library it can no longer find on disk, but it only ever considers libraries recorded with no path. A library in a later folder whose folder is deleted stays in the list for ever, pointing at nothing.
- `DetachLibrary` on a library in a later folder deletes a recorded path that was never written, and the next server start finds the folder again and puts the library back. So detaching such a library does not survive a restart, while detaching one from the first folder does.

The third of those is the one most likely to surprise an administrator: the same operator, on two libraries that look identical in the catalog, has a different outcome across a restart depending on which configured folder the library happens to sit in.

## Why it is not covered today

The shared in-process test server (`tests/DAE.Server.Tests/TestServerInstance.cs`) hard-codes exactly one library folder - the `Libraries` folder inside the instance's own temporary directory - and offers no way to ask for more. So the first piece of this work is in the test infrastructure, not in a test: let a fixture create a server with two library folders, both inside the instance's own directory, without disturbing any of the callers that want one.

`debt-library-attach-from-a-directory` recorded this as a known gap when it pinned the attach operators, and its review agreed the gap is real but out of that ticket's scope.

## Expected behaviour

What the suite should be able to state after this work, for a server configured with two library folders:

- A library in each folder is in the server's list after a start, and the one in the second folder reports its folder in the catalog while the one in the first folder reports nothing.
- Deleting a library's folder behind the server's back and refreshing removes the library from the list when the folder was the first one, and leaves it when the folder was a later one.
- Detaching a library and restarting: the library stays gone when it came from the first folder, and comes back when it came from a later one.

Whichever of those turns out to be wrong rather than merely surprising is then its own ticket; this one is to find out, and to leave the answers written down. None of the behaviour above should be changed under this ticket - a server configuration is a compatibility surface, and an installation may be relying on exactly these outcomes.
