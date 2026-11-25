description: Delete the stale duplicate source files and the obsolete project file that sit unused in the connection library, and bring the ODBC connection library into the build so the ODBC driver support that shipped in 3.1 ships again.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/Connection/Source/ConnectionException.cs, libraries/Connection/Source/ConnectionException.resx, libraries/Connection/Source/DotNetConnection.cs, libraries/Connection/Source/MSSQLConnection.cs, libraries/Connection/Source/ODBCConnection.cs, libraries/Connection/Source/SQLConnection.cs, libraries/Connection/Source/SQLTypes.cs, libraries/Connection/Source/DAE.Connection.ODBC.csproj, libraries/Connection/Source/DAE.Connection.csproj, libraries/ODBCConnection/Source/DAE.Connection.ODBC.csproj, libraries/ODBCConnection/Source/Properties/AssemblyInfo.cs, libraries/ODBCConnection/ODBCConnection.d4l, Dataphor.slnx, tools/layout/Layout.csproj, Directory.Packages.props, AGENTS.md, docs/architecture/devices.md, docs/migration-notes.md, tickets/backlog/debt-ws12-hygiene-remainder.md
difficulty: easy
----
## What this is

Two things at one site: the `Connection` library and its ODBC sibling.

**The duplicates.** `libraries/Connection/Source/` holds seven files that no project compiles. Each is a stale copy of a file that now lives under `src/`:

| Uncompiled copy in `libraries/Connection/Source/` | Lines | The file that is compiled | Compiled by |
|---|---:|---|---|
| `ConnectionException.cs` + `ConnectionException.resx` | 139 + 83 | `src/engine/DAE/Connection/ConnectionException.cs` | `DAE.csproj` |
| `SQLConnection.cs` | 1139 | `src/engine/DAE/Connection/SQLConnection.cs` | `DAE.csproj` |
| `SQLTypes.cs` | 93 | `src/engine/DAE/Connection/SQLTypes.cs` | `DAE.csproj` |
| `DotNetConnection.cs` | 408 | `src/engine/Server/Connection/DotNetConnection.cs` | `Server.csproj` |
| `MSSQLConnection.cs` | 718 | `src/engine/Server/Connection/MSSQLConnection.cs` | `Server.csproj` |
| `ODBCConnection.cs` | 115 | `libraries/ODBCConnection/Source/ODBCConnection.cs` | the ODBC project, below |

2,695 lines in all (measured 2026-11-25 with `wc -l`), which is the "2,600 lines of uncompiled duplicates" the plan's WS7 library table names. `DAE.Connection.csproj` already carries a comment saying these are leftovers for this ticket to delete, and `debt-ws12-hygiene-remainder`'s `build-globbing-cleanup` arm waits on them going before it switches that project to default globbing: a globbing switch with them still there would compile two `SQLConnection` classes.

Nothing is lost by deleting them. `libraries/Connection/Source/Register.cs` says `typeof(MSSQLConnection)`, which resolves to the *compiled* `Alphora.Dataphor.DAE.Connection.MSSQLConnection` in the server assembly, so the class alias `Connection.MSSQLConnection` keeps pointing where it points today. Compare each copy against its live counterpart before deleting it, and say in the handoff whether any of the six has drifted: a copy that differs would mean a change was once made on the wrong one, and the difference is worth reading before it goes.

**The Visual Studio 2003 project file.** `libraries/Connection/Source/DAE.Connection.ODBC.csproj` is a 2003-format copy of `libraries/ODBCConnection/Source/DAE.Connection.ODBC.csproj` - same `ProjectGuid`, same assembly name - in no solution, not converted by the spike, naming a shared build-output folder that no longer exists. It is also the one duplicate project *file name* in the tree, which matters because the build keys output folders by project file name (`AGENTS.md`, *Converting another project*). Delete it here, with the real one converted in the same change.

That arm is currently written down twice: `debt-ws12-hygiene-remainder` lists this file under `build-remove-legacy-build`, whose own prerequisites (`ui-dataphoria-port`, `installer-msi`) are a long way off. This ticket is the right owner - the file is a copy of the project this ticket converts - so **edit that bullet in `tickets/backlog/debt-ws12-hygiene-remainder.md` in the same commit** to say the file has already gone, and drop the "today only `DAE.Connection.ODBC.csproj` appears twice, until `device-connection-cleanup`" clause from `AGENTS.md`.

**The ODBC connection library.** `libraries/ODBCConnection/` is a D4 library whose descriptor lists one assembly and whose C# project is still in the .NET Framework format, so the layout leaves the library out (`tools/layout/Layout.csproj`, the `DataphorD4Library Remove` line naming this ticket). The plan's WS7 table says to convert it if that is a project-file change only. It is: `ODBCConnection.cs` (115 lines) and `Register.cs` compile against `System.Data.Odbc`, which is a NuGet package (`System.Data.Odbc`) on .NET 10, and the project references only Base, DAE, Server and the Connection library. Convert it the way `AGENTS.md`'s *Converting another project* section says, add `System.Data.Odbc` to `Directory.Packages.props`, put the project in `Dataphor.slnx` and in the layout's Library list, and remove the exclusion.

**OLE DB needs nothing.** The same plan row pairs ODBC with OLE DB; OLE DB is already done. `OLEDBConnection.cs` is compiled by `DAE.Connection.csproj` against the `System.Data.OleDb` package. Say so in the handoff so the plan row can be closed truthfully rather than left half open.

## Edge cases & interactions

- **The layout's `.d4l` check is one-way.** It fails the build when a `.d4l` lists a file the layout does not provide, and says nothing about files the layout provides that no `.d4l` lists. `ODBCConnection.d4l` lists only `Alphora.Dataphor.DAE.Connection.ODBC.dll`, so the check passes as long as the assembly lands; the `System.Data.Odbc` package's runtime files land beside it only if the package is *private* to this library (no project it references has it). It is - no other project in the solution references `System.Data.Odbc` - but verify it in the assembled layout rather than assuming, by listing `Libraries/ODBCConnection/` after a build. Verified by inspection of the folder, not by a test.
- **Registering the library must not need an ODBC driver.** The connection class is constructed only when a device names it as its `ConnectionClass`, so a server should be able to register `ODBCConnection` on a machine with no driver manager configured. Confirm it: register the library once on a `TestServerInstance` in a throwaway run and report what happened. If registration does turn out to need something external, leave the library out of the layout and park the conversion, saying why - that is the "otherwise park" half of the plan row.
- **Two projects, one file name, until the 2003 copy goes.** Delete the 2003 copy in the same change that puts the converted project in `Dataphor.slnx`. The other order leaves a window in which two projects named `DAE.Connection.ODBC.csproj` are reachable from the build.
- **`System.Data.Odbc` is cross-platform but needs a driver manager at run time.** Nothing here runs it and the repository is Windows-only today, so this is a documentation point rather than a code one: say in `docs/architecture/devices.md` that the ODBC connection is available again and what it needs at run time.
- **Deleting the duplicates removes no type from any assembly** (none of them is compiled), so **no migration note is owed for that**. One *is* owed for ODBC: a deployment that had given up on `Connection.ODBC.ODBCConnection` gets it back, and `docs/migration-notes.md` should carry one line saying the ODBC connection library ships again and which package it uses. Verified by reading the note, not by a test.
- **`libraries/ODBCConnection/Source/obj/` holds untracked leftovers** from the old format (`git ls-files` shows only the five tracked files). Leave them; they are ignored, and the new project builds into `artifacts/obj/`.
- **No new test.** This ticket deletes dead files and converts a project file; the project rules say no test for wiring or for what the compiler already checks. The build and the layout's `.d4l` check are the guards. Say in the handoff that no behaviour changed and that nothing in the suite registers or exercises the ODBC connection - that gap belongs to whoever gives ODBC a live test, and this ticket should not invent one.

## TODO

- Read each of the six uncompiled `.cs` files against the compiled file of the same name under `src/` (and `ODBCConnection.cs` against `libraries/ODBCConnection/Source/ODBCConnection.cs`); record in the handoff whether any has drifted, and what the difference was.
- Delete the seven files (six `.cs` plus `ConnectionException.resx`) and drop the leftovers comment from `libraries/Connection/Source/DAE.Connection.csproj`.
- Delete `libraries/Connection/Source/DAE.Connection.ODBC.csproj`.
- Convert `libraries/ODBCConnection/Source/DAE.Connection.ODBC.csproj` to SDK format, modelled on `libraries/Connection/Source/DAE.Connection.csproj`: assembly name `Alphora.Dataphor.DAE.Connection.ODBC`, root namespace `Alphora.Dataphor.DAE.Connection.ODBC`, the constants block, project references to Base, DAE, Server and `DAE.Connection.csproj`, explicit `Compile` items for the three files.
- Trim `libraries/ODBCConnection/Source/Properties/AssemblyInfo.cs` the way every converted project's is (keep title, description, `ComVisible`, `Guid`; delete the version, company, product, copyright, signing and `#if SIGNASSEMBLIES` entries and the template comments), preserving the file's line endings and byte-order-mark state.
- Add `System.Data.Odbc` to `Directory.Packages.props` at the current stable version for .NET 10 and reference it from the project with no version.
- Add the project to `Dataphor.slnx` and to the Library list of `tools/layout/Layout.csproj`, and remove the `DataphorD4Library Remove` line for `ODBCConnection.d4l`.
- Build Debug and Release; list `artifacts/layout/<config>/Libraries/ODBCConnection/` and record what is in it.
- Register the library once on a throwaway in-process server and record whether registration needs anything external.
- Update `AGENTS.md`: `ODBCConnection` joins the converted list, and the duplicate-project-file-name clause goes.
- Add the ODBC line to `docs/migration-notes.md` and the sentence to `docs/architecture/devices.md` (244 words of its 1,500-word cap today; run `python tools/docs-size-check/check_size.py` afterwards).
- Edit the `build-remove-legacy-build` bullet in `tickets/backlog/debt-ws12-hygiene-remainder.md` so it no longer claims the 2003 ODBC project file, and its `build-globbing-cleanup` bullet so the Connection set reads as already deleted.
- Record the result in the plan's WS7 library table and work-item table rows for `device-connection-cleanup`, including that OLE DB needed nothing.
- Targeted tests: the build is the main guard. Run `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~LibraryLoadingTest|FullyQualifiedName~HostProgramTest|FullyQualifiedName~FormCustomizationTest"` - the first two read the layout and the hosts' dependency files, the third reads the library set - and name the filter in the handoff.
