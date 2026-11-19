description: Make the server load a library's assemblies and their private managed and native dependencies from the library's own folder in the runtime layout, with a short test that exercises both.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
prereq: build-runtime-layout
files: src/engine/Base.Windows/AssemblyUtility.cs, src/engine/Server/Schema/LibraryUtility.cs, tools/layout/Layout.csproj, tests/DAE.Server.Tests
difficulty: medium
----
## What this is

The runtime layout gives every library folder its assembly and that assembly's *private* managed package files (the files flattened for the Windows runtime; see `build-runtime-layout`), with no `.deps.json`. Today's assembly resolver (`AssemblyUtility.ResolveAssembly`, an `AppDomain.AssemblyResolve` hook) only re-matches an already-loaded assembly by simple name; it does not probe the library folder, and it does not touch native libraries at all. A library whose assembly has a managed package dependency that is not already resolvable from `bin/` fails to load its dependencies on first use, and no library with a native dependency can register.

This ticket formalises the design the plan settled on (`#ws2-engine-and-server-port`, design note "Assembly loading"): everything stays in the default load context, no unloading; add a resolver that probes the registered library directories for managed **and** native dependencies when an assembly loaded from a library folder asks for one.

## Design

**Where probing attaches.**

- Managed: subscribe to `AppDomain.CurrentDomain.AssemblyResolve` in addition to the existing by-name-against-loaded-assemblies handler. When `args.RequestingAssembly` was loaded from a known library folder, probe that folder for `<SimpleName>.dll`. If that assembly is not the requesting assembly's, probe the library folders of the *libraries this library requires* (see "Which folders to probe" below), then give up and return `null`. Call `Assembly.LoadFrom` on a hit; `LoadFrom` on a copy of an assembly already loaded returns the existing instance (verified in the spike with Newtonsoft.Json in `bin/` and in System.Internet's folder).
- Native: subscribe to `AssemblyLoadContext.Default.ResolvingUnmanagedDll`. When the requesting assembly was loaded from a known library folder, probe that folder for the library name (bare, with `.dll`, and prefix/suffix variants `NativeLibrary.TryLoad` already walks on Windows) and return the handle; fall through on miss. Prereq libraries are probed the same way as for managed resolution.

**Which library folders are known.** `LibraryUtility.RegisterLibraryAssemblies` already calls `Assembly.LoadFrom(sourceFile)` where `sourceFile` is inside the library folder. Record, in a process-wide table owned by the resolver, the mapping `Assembly → Schema.LoadedLibrary` (library folder path + required library names) for every assembly it loads. `UnregisterLibraryAssemblies` removes the entries. The resolver's handlers read this table from any thread.

**Which folders to probe for a given request.** The requesting assembly identifies one library through the table. Probe that library's folder first; if the library has `Schema.LibraryReference`s, walk them transitively and probe each required library's folder in the order the required-libraries list already defines. This mirrors D4's existing library-reference semantics and means a library's private dependency can still live in the folder of a library it requires (the private-package flattening in the layout does not share files between libraries today, but transitively following references keeps a future shared dependency working without redesign).

**Native layout inside a library folder.** Match the managed layout: native files sit flat in `artifacts/layout/<debug|release>/Libraries/<Name>/`, the Windows RID choice is already made by `Layout.csproj` the same way it is made for managed files (`win-x64` if present, else `win`, else the generic file). This keeps one shape in a library folder, keeps `Layout.csproj` from needing a RID-aware probe branch at load time, and matches the plan's design note that native probing is done "for registered library directories". **Tradeoff declined:** `runtimes/<rid>/native/` inside each library folder would survive a future cross-RID install image without re-laying out, but no consumer reads a library folder cross-RID (the installer owns the install image, see `installer-publish-profiles`), and the second shape in the same folder is a trap for a reader; revisit if a non-Windows install image is ever required.

**Lifting the Layout.csproj native-asset error.** `tools/layout/Layout.csproj`'s `_DataphorComputeLayout` target errors on a private *native* asset today ("no library has a private native asset yet"). Replace that error with the same flattening rule used for managed files, writing the Windows RID choice into the library folder. Leave the managed path untouched. The target's existing error on a `.d4l` that lists a file its folder does not get stays.

**Default load context, no unloading.** The resolver installs itself once at process start (through `AssemblyUtility.Initialize`, which `Server` already calls before any library load). There is no per-library `AssemblyLoadContext`, no `IsCollectible`. `UnregisterLibraryAssemblies` removes the table entry but does not unload the assembly (today's behaviour).

## Edge cases & interactions

- **Two libraries with the same private dependency name but different versions.** The flattening rule gives each library its own copy; the resolver probes the requesting library's folder first, so each library sees its own version. `LoadFrom` on a second copy of an assembly the *host* already has returns the host's (`bin/`); that is deliberate and matches what the layout design decided. Assert and record this in the test by including a package that is both private to the test library and present in `bin/` of the test harness, and showing both libraries see the same instance — verified **by inspection in the test**.
- **An assembly requested by name alone (no requesting assembly, e.g. by `Assembly.Load(name)`).** `args.RequestingAssembly` is `null`. The managed handler returns `null` in that case; the existing by-name-against-loaded handler still runs. **By inspection** — no test, no change in behaviour vs today.
- **A native dependency is a `.dll` that is also a managed assembly.** `NativeLibrary.TryLoad` on a managed assembly succeeds on Windows; this is fine — we only probe when the requesting assembly was loaded from a library folder and the name is being asked for through the unmanaged resolver, so the request is already a `DllImport` call. **By inspection.**
- **Concurrent first-use of two libraries.** The table is populated under `LibraryUtility`'s existing per-catalog lock (the only writer is `RegisterLibraryAssemblies`/`UnregisterLibraryAssemblies`). Readers (the two handlers) lock-read the table. **By inspection** — no concurrency test; the table is a standard `ConcurrentDictionary`.
- **A library is unregistered while another library's assembly still references its native dependency.** The native handle returned by `NativeLibrary.Load` is pinned by the CLR for the process lifetime; `UnregisterLibraryAssemblies` does not free it. This matches "no unloading" and is **by inspection**.
- **A library folder listed in `LibraryDirectories` with no C# assembly (pure D4).** `RegisterLibraryAssemblies` iterates the library's `Files` list and only loads what is marked `IsAssembly`; nothing is added to the table and the resolver cannot be asked about that folder. **By inspection.**
- **The requesting assembly is in `bin/`, not a library folder.** The table lookup misses; the handler returns `null` and the default `bin/` probe continues to resolve from `bin/`. **By inspection.**
- **A `DllNotFoundException` after this ticket** must still carry the original library name in its message for a user running an installed instance; do not swallow it inside `ResolvingUnmanagedDll` — return `IntPtr.Zero` on a miss and let the runtime raise.

## Verification

One characterization test under `tests/DAE.Server.Tests`. The test adds a probe library to the runtime layout (a `.d4l` plus a compiled assembly plus one private managed package plus one native file), registers it through the in-process server, and runs one D4 line per arm:

- **Private managed dependency resolves from the library folder.** The library's `Register` returns a type whose static initializer loads something from the package. A D4 call into the library succeeds.
- **Native dependency resolves from the library folder.** The library's `Register` declares a `DllImport` on the native file; a D4 call that reaches it succeeds.
- **Pre-existing flat layout still works** (regression guard): add one assert that the base `Connection` library (which carries four private managed packages today) still registers, so the private-managed path is pinned beyond the probe library.

The probe library's native file is a tiny C file built once and checked in under `tests/libraries/<name>/native/`; if that is impractical on CI (compile toolchain), substitute a tiny C# assembly built for `NativeAOT` and record the substitution in the test comment. No cross-RID coverage; Windows only.

## Tasks

Phase A — resolver

- Add `LibraryAssemblyResolver` under `src/engine/Base.Windows/` holding the `Assembly → (libraryFolder, requiredLibraryFolders)` table and the two handlers.
- Wire the two handlers in `AssemblyUtility`'s static constructor alongside the existing one.
- In `LibraryUtility.RegisterLibraryAssemblies`, after `Assembly.LoadFrom`, call `LibraryAssemblyResolver.Register(assembly, loadedLibrary)`; in `UnregisterLibraryAssemblies`, call `Unregister`. Build the required-library-folder list from `loadedLibrary.RequiredLibraries` once per register call.

Phase B — layout

- In `tools/layout/Layout.csproj`'s `_DataphorComputeLayout` target, replace the native-asset error with the Windows-RID flattening rule, written into the library folder. Keep the `.d4l` file-listing error.

Phase C — test

- Add `tests/libraries/LibraryLoadingProbe/` with a D4 library, one C# project referencing one private NuGet package, and one native file (or the NativeAOT substitute described above).
- Add `LibraryLoadingTest` to `tests/DAE.Server.Tests` with the three arms named in "Verification".

Phase D — migration note

- Add an entry to `docs/migration-notes.md` describing how a library with a native dependency now ships (flat in its library folder, Windows RID selection by the layout) and that the resolver probes transitively through library references for managed and native dependencies.
