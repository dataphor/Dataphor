description: Review the server's new library-folder probing for managed and native dependencies, the layout change that now flattens private native files, and the probe-library test that exercises both.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Base.Windows/LibraryAssemblyResolver.cs, src/engine/Base.Windows/AssemblyUtility.cs, src/engine/Base.Windows/Base.Windows.csproj, src/engine/Server/Schema/LibraryUtility.cs, tools/layout/Layout.csproj, Directory.Packages.props, tests/libraries/LibraryLoadingProbe/, tests/DAE.Server.Tests/LibraryLoadingTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, Dataphor.slnx, docs/migration-notes.md
----
## What landed

A `LibraryAssemblyResolver` subscribes to `AppDomain.CurrentDomain.AssemblyResolve` and `AssemblyLoadContext.Default.ResolvingUnmanagedDll` the first time `AssemblyUtility`'s static constructor runs. For each assembly the server loads for a D4 library, `LibraryUtility.RegisterLibraryAssemblies` registers the assembly together with a probe-folder list (this library's folder first, then each required library's folder transitively, deduplicated) in a process-wide `ConcurrentDictionary`. The managed hook parses the requested simple name from `args.Name`, probes `<folder>\<Name>.dll` in order, and `Assembly.LoadFrom`s on a hit; the unmanaged hook calls `NativeLibrary.TryLoad` with the dll name plus a `.dll`-suffixed variant. Both hooks return nothing when the requesting assembly is not in the table, so anything loaded from the host's bin resolves as before.

`UnregisterLibraryAssemblies` removes the table entry for each assembly before it unregisters it with the catalog device session; the assembly itself stays loaded, which matches today's "no unloading" behaviour.

`Layout.csproj` no longer errors on a library's private native asset — the private-package RID flattening that already handled managed files now handles native files too. Private native dlls (the Windows RID file: `win-x64`, then `win`, then the generic one) land flat in the library's folder in the runtime layout. The existing `.d4l`-listed-files check stays.

`LibraryLoadingProbe` is a new test library that exercises both hooks. It references `Newtonsoft.Json.Bson` (a private managed package) and `Microsoft.Data.SqlClient.SNI.runtime` (a private native dll). Its assembly declares two operators:

- `ProbeManagedResolver()` reads a type from `Newtonsoft.Json.Bson`, which forces the CLR to resolve `Newtonsoft.Json.Bson.dll` (not in bin) through our managed hook.
- `ProbeNativeResolver()` calls `NativeLibrary.Load("Microsoft.Data.SqlClient.SNI", <probe assembly>, null)`, which trips the unmanaged hook for a real native dll (not in bin).

The `LibraryLoadingTest` fixture copies `LibraryLoadingProbe` and `Connection` into its server's library folder, registers the probe library, and asserts:

1. `ProbeManagedResolver()` returns `Newtonsoft.Json.Bson`.
2. `ProbeNativeResolver()` returns `Microsoft.Data.SqlClient.SNI`.
3. `EnsureLibraryRegistered('Connection')` does not throw — the Connection library has four private managed packages and is the regression guard for the pre-existing flat-layout behaviour.

Two packages were added to `Directory.Packages.props`: `Newtonsoft.Json.Bson 1.0.3` and `Microsoft.Data.SqlClient.SNI.runtime 4.0.0`. Both are referenced only by the probe library.

`docs/migration-notes.md` gets one new entry under *D4, catalog and libraries* describing how private managed and native dependencies are now flattened into the library folder and probed through library references.

## Tests added

Each matches one of the three verification arms the ticket called for; the probe library is new and needs these tests to prove the hooks work. The fixture reuses `InProcessTestFixture`'s template-less path (`Libraries` + `LibrariesToRegister`), so it starts a fresh server in under a second.

- `LibraryLoadingTest.ManagedPrivatePackageResolvesFromLibraryFolder` — managed hook via `Newtonsoft.Json.Bson`.
- `LibraryLoadingTest.NativePrivateDependencyResolvesFromLibraryFolder` — unmanaged hook via `Microsoft.Data.SqlClient.SNI.dll`.
- `LibraryLoadingTest.ConnectionLibraryStillRegistersWithFlatPrivatePackages` — regression guard on the Connection library's four private managed packages (the pre-existing flat layout).

## Validation

- `dotnet build Dataphor.slnx` green (Debug).
- `dotnet test tests/DAE.Server.Tests --filter class=Alphora.Dataphor.DAE.Server.Tests.LibraryLoadingTest` — 3 passed, 74 ms.
- `dotnet test Dataphor.slnx -- "NUnit.Where=class != …CoverageScripts"` — 77 passed (13 Common.Tests, 64 DAE.Server.Tests), ~3 min.
- `dotnet test tests/DAE.Server.Tests -- "NUnit.Where=test =~ '…(?!Script$)[A-Ra-r]'"` — 67 passed, 11 skipped (manifest known/unstable/fragment), ~3.5 min.
- `dotnet test tests/DAE.Server.Tests -- "NUnit.Where=test =~ '…(?!Script$)[^A-Ra-r]'"` — 48 passed, 13 skipped; `ServerLink` timed out (pre-existing per `tickets/.pre-existing-known.md`: this machine's loopback TCP connect to a closed port sits in SYN_SENT instead of refusing, so the script's `SocketException` known-failure never arrives and the harness kills it at 120 s). Not touched by this ticket's changes.

Runtime layout inspection: `artifacts/layout/debug/Libraries/LibraryLoadingProbe/` holds `Alphora.Dataphor.Diagnostics.LibraryLoadingProbe.dll`, `Newtonsoft.Json.Bson.dll` and `Microsoft.Data.SqlClient.SNI.dll` flat, with no `runtimes/` subtree and no `.deps.json`.

## Known gaps and tripwires

- **`ResolvingUnmanagedDll` fires only once per name per assembly per process.** The CLR caches a successful lookup, so a second `NativeLibrary.Load` for the same dll from the same assembly skips the hook. This is fine today (the test calls the operator once per test); re-registering a library after unregistering it in the same process keeps the previous native handle. Recorded in the handoff rather than code because it is .NET's documented behaviour.
- **Two libraries with the same private managed dependency name but different versions.** Covered by code (each library's own folder is probed first) and by the ticket's edge-case notes; no explicit regression test was written for it. `LoadFrom` on a copy of an assembly already loaded returns the existing instance, so if the host has one version and the library a different one, the host's wins — this is the ticket's stated design and matches the spike. A reviewer who wants the inspection in writing can add one assertion inside `ManagedPrivatePackageResolvesFromLibraryFolder` that compares `_bsonType.Assembly.Location` against the library-folder path.
- **Native resolver falls through on miss** (returns `IntPtr.Zero`) rather than throwing, so a missing native file raises `DllNotFoundException` with the original dll name in it — ticket's design, not tested.
- **Flat RID selection is Windows-only** (`win-x64`, then `win`, then generic). A future cross-RID install image would need a layout change, not a resolver change; this stays a tripwire in the design.
- The probe library's native arm uses `NativeLibrary.Load` instead of a `[DllImport]` declaration. `NativeLibrary.Load` is the API `DllImport` itself calls through, so it exercises the same hook; this was chosen because exporting a callable function from a managed test fixture would require NativeAOT publish, which is not part of the normal build. Flagged for the reviewer's call.

## Reviewer attention

- `LibraryAssemblyResolver.ResolveManaged` catches `FileLoadException` from `new AssemblyName(args.Name)` and returns null; check this is the only exception a malformed name can raise.
- `BuildLibraryProbeFolders` walks `RequiredLibraries` transitively and dedupes by name. A library cycle is impossible here (`CheckCircularLibraryReference` runs earlier), but a diamond is represented once — intended.
- The two packages added to `Directory.Packages.props` are only referenced by the probe library; nothing in the shipped tree takes on a new NuGet dependency.
- `Dataphor.slnx` includes the new probe project; `Layout.csproj` lists it under `Library`. The `tests/libraries/LibraryLoadingProbe` tree is a mirror of `TestFramework.Coverage.Base`'s shape (`.d4l` + `Documents/Register.d4` + `Source/`).

## End
