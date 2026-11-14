description: Replace the old Visual Studio project files for the engine, the client protocol libraries and the system libraries with modern ones, so that the core of the product builds on .NET 10 from a clean checkout with one command.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.props, Directory.Packages.props, Dataphor.slnx, global.json, src/engine/Base/, src/engine/Base.Windows/, src/engine/DAE/DAE.csproj, src/engine/DAE/Runtime.Instructions/SecurityNodes.cs, src/protocol/Common/, src/protocol/Common/WCF/ServiceClient.cs, src/protocol/NativeCLI/, src/protocol/Client/, src/protocol/Client/Client/ClientServer.cs, libraries/SQLDevice/Source/DAE.Device.SQL.csproj, libraries/System.Platform/Source/System.Platform.csproj, libraries/System.Internet/Source/System.Internet.csproj, libraries/System.Internet/Source/packages.config, libraries/System.Integration/Source/System.Integration.csproj, tools/net-spike/convert.py, tools/net-spike/run.py, tools/net-spike/projects.txt, tools/net-spike/stubs/Engine.cs, AGENTS.md, docs/migration-notes.md
difficulty: hard
----
# Core projects as SDK-style projects

## Goal

Nothing in this repository builds with the `dotnet` command today. The legacy project files need Visual Studio and .NET Framework 4.5.2 targeting packs. This ticket creates the real build: SDK-style project files for the engine and the libraries around it, a root `Directory.Build.props` and `Directory.Packages.props` that hold the shared settings, and a root solution `Dataphor.slnx`. After it, `dotnet build Dataphor.slnx` on a clean clone with only the .NET 10 SDK (pinned by `global.json`) compiles these projects with no errors. Every later ticket in WS1, WS2 and WS3 builds on this, and the conventions it sets (constants, resources, warnings, packages, output location) are copied by every project converted after it.

**Paths.** This ticket runs after `repo-move-to-new-layout`. All paths here are in the target layout from [Repository layout](../../docs/modernization-plan.md#repository-layout). If the move has not happened, find the old location through the plan's old → new table (for example `src/engine/DAE/DAE.csproj` is today's `Dataphor/DAE/Engine.csproj`; `libraries/X` is `Libraries/X`; `external/` is today's `Dataphor/bin`). Project file names other than `DAE.csproj` are whatever the move ticket gave them.

## Scope

In scope:

- SDK-style projects, same assembly names and root namespaces, for: Base (`src/engine/Base`), Base.Windows (`src/engine/Base.Windows`), DAE (`src/engine/DAE`), Common (`src/protocol/Common`), NativeCLI (`src/protocol/NativeCLI`), Client (`src/protocol/Client`), and the libraries SQLDevice (see the dependency finding below), System.Platform, System.Internet and System.Integration (`libraries/<Name>/Source`).
- Root `Directory.Build.props`, `Directory.Packages.props` and `Dataphor.slnx` (with a solution filter for the engine and server set if the planner wants it now; the Windows UI filter comes with WS6).
- The compile-only patches from [Appendix A](../../docs/modernization-plan.md#appendix-a-spike-results) whose files are in these projects: the dead `using System.Runtime.Remoting.Messaging` in `src/engine/DAE/Runtime.Instructions/SecurityNodes.cs`, and the `ChannelFactory<T>(endpointConfigurationName)` constructors in `src/protocol/Common/WCF/ServiceClient.cs` (line ~57) and `src/protocol/Client/Client/ClientServer.cs` (line ~152). The .NET 10 WCF client packages have no constructor that reads an endpoint from configuration; the replacement must fail clearly when a configuration name is passed (see open choices), not silently pass `null` as the spike does.
- K4 (moved here from `build-server-project` because the file is in Base and a Debug run of the engine fails without it): `ThreadUtility.SetThreadName` (`src/engine/Base/ThreadUtility.cs`) reflects on the private field `Thread.m_Name`, which does not exist on .NET 10; `GetField` returns null and `SetValue` throws, so the first process execution in a Debug build throws `NullReferenceException` (`[Conditional("DEBUG")]` is evaluated in the caller, `ServerProcess.cs` ~1750, ~1770, ~1805, and DAE defines `DEBUG` in Debug). On .NET Core 3.0 and later `Thread.Name` may be assigned more than once (confirm, including assigning null to clear it), so the reflection can be replaced by `thread.Name = name`; the spike's null check is the fallback.
- Keeping `tools/net-spike` working (see "The spike after this ticket").
- Rewriting the "Build and test" section of `AGENTS.md` with the real commands, the output location and the conversion conventions.

Out of scope:

- `src/engine/Server` and everything that needs it: `build-server-project`. **This includes Store.SQLite, Connection, MSSQLDevice, SimpleDevice and Frontend** — see the next section. Their compile-only patches (`SQLiteException.ErrorCode` → `ResultCode`, the `IStreamProvider` collision in `libraries/Frontend/Source/Device/FrontendDevice.cs`, K19 in `libraries/Connection/Source/Register.cs`, and the `SecurityNodes.cs` twin in `src/engine/Server/Runtime.Instructions/`) move with them.
- Version numbers, signing, and removing per-project version attributes: `build-version-and-signing`.
- The runtime layout under `artifacts/layout/`: `build-runtime-layout`.
- Tests and the test harness: `test-harness-inprocess`.
- `DAE.Client`, `DAE.Client.Provider`, `D4Runner` and the UI projects (`feat-ws6-windows-client-and-ide`, starting with `ui-client-layering`); `DAEServer` and `DAE.Service` (`feat-ws5-protocol-and-hosting`; K23 lives in their vendored parser and is not needed here); PostgreSQL, ODBC and other connections (`feat-ws7-devices-and-libraries`); `TestFramework.Coverage.Base` (`build-runtime-layout`).
- Switching to default file globbing and deleting stray sources (`build-globbing-cleanup`, in `debt-ws12-hygiene-remainder`).

## Verified facts

### Six of the named projects cannot compile without the server

The work-item row in the plan lists Store.SQLite, Connection, MSSQLDevice, SimpleDevice and Frontend in this ticket, but each of them uses types from the server assembly (`AlphoraDataphorServer`, `src/engine/Server`), which `build-server-project` converts after this ticket:

| Project | What it uses from the server |
|---|---|
| Store.SQLite | `SQLiteConnection : DotNetConnection` (`src/engine/Server/Connection/DotNetConnection.cs`) |
| Connection | compiled `OLEDBConnection.cs` derives from `DotNetConnection`; `Register.cs` registers the server's `MSSQLConnection` |
| MSSQLDevice | `MSSQLConnection`, `DotNetConnection` (`Device.MSSQL/MSSQLDevice.cs` ~717–789, `MSSQLConnectionStringBuilder.cs`) |
| SimpleDevice | `((Server.Server)process.ServerSession.Server).InstanceDirectory` (`SimpleDevice.cs:115`) |
| Frontend | `Schema.LibraryUtility` (server's `Schema/LibraryUtility.cs`) and casts to `DAE.Server.Server` (`Device/FrontendDevice.cs` ~66, ~639) |

So these five move to `build-server-project`, which converts them together with the server. SQLDevice's legacy project also references the server and Connection projects, but a static scan found no use of a server-only or Connection-library type (`SQLConnection` is the engine's, in `src/engine/DAE/Connection`; `DAERegisterAttribute` is in `src/engine/DAE/Server/DAERegisterAttribute.cs`). Confirm by compiling SQLDevice without those two references; if it does not compile, SQLDevice moves to `build-server-project` as well. Removing an unused project reference has no runtime effect; SQLDevice's `.d4l` still requires the Connection library at the D4 level.

The projects that do not need the server (verified from their legacy references): Base, Base.Windows, DAE, Common, NativeCLI, Client, System.Platform, System.Internet, System.Integration.

### Per-project facts from the legacy project files

| Project | Assembly | Root namespace | Debug constants | Release constants | References |
|---|---|---|---|---|---|
| Base | `Alphora.Dataphor` | `Alphora.Dataphor` | `DEBUG;TRACE` | `SIGNASSEMBLIES` | — |
| Base.Windows | `Alphora.Dataphor.Windows` | `Alphora.Dataphor.Windows` | `DEBUG;TRACE` | `TRACE;SIGNASSEMBLIES` | — |
| DAE | `Alphora.Dataphor.DAE` | `Alphora.Dataphor.DAE` | `TRACE;DEBUG;USENATIVE;NATIVEROW;ACCUMULATOR;USEOBJECTVALIDATE;NILPROPOGATION;USEVISIT` | `SIGNASSEMBLIES;USENATIVE;NATIVEROW;USEOBJECTVALIDATE;NILPROPOGATION;DISABLE_PERFORMANCE_COUNTERS` | Base |
| Common | `AlphoraDataphorCommon` | `Alphora.Dataphor.DAE` | `DEBUG;TRACE` | `TRACE;SIGNASSEMBLIES` | System.ServiceModel |
| NativeCLI | `Alphora.Dataphor.DAE.NativeCLI` | same | `DEBUG;TRACE` | `SIGNASSEMBLIES` | Common; Newtonsoft.Json 9.0.1 |
| Client | `AlphoraDataphorClient` | `Alphora.Dataphor.DAE.Client` | `DEBUG;TRACE` | `TRACE;SIGNASSEMBLIES` | Base, Base.Windows, Common, DAE; System.ServiceModel |
| SQLDevice | `Alphora.Dataphor.DAE.Device.SQL` | same | `TRACE;DEBUG;USEVISIT` | `SIGNASSEMBLIES` | Base, DAE (plus the two probably unused) |
| System.Platform | `System.Platform` | `Alphora.Dataphor.Libraries.System.Platform` | `DEBUG;TRACE` | `TRACE;SIGNASSEMBLIES` | Base, Base.Windows, DAE |
| System.Internet | `System.Internet` | `Alphora.Dataphor.Libraries.System.Internet` | `DEBUG;TRACE` | `SIGNASSEMBLIES` | Base, DAE; Newtonsoft.Json 9.0.1; System.Web (only `HttpUtility`, which exists on .NET 10) |
| System.Integration | `System.Integration` | `Alphora.Dataphor.Libraries.System.Integration` | `DEBUG;TRACE` | `TRACE;SIGNASSEMBLIES` | Base, DAE |

`AllowUnsafeBlocks` differs by configuration in some projects (Base and DAE: Release only; SQLDevice: both); enabling it wherever either configuration had it changes nothing at run time. Several projects generated XML documentation in Release only (`DocumentationFile`); nothing consumes those files.

**Newtonsoft.Json 9.0.1 is not a tracked binary.** NativeCLI, System.Internet (and the server) reference it through `packages.config` with a `HintPath` into the git-ignored `packages/` restore folder. It becomes a central package reference; the spike used 13.0.3. System.Internet's `.d4l` also lists `Newtonsoft.Json.dll` as a library file (`isassembly="False"`), which matters for `build-runtime-layout`, not here.

**Tracked third-party DLLs.** None of this ticket's projects references a tracked binary. The four DLLs in today's `Dataphor/bin` (`external/` after the move) are `System.Data.SQLite.DLL` (Store.SQLite; replaced by a package in `build-server-project`), `Npgsql.dll` and `Mono.Security.dll` (PostgreSQL; `device-pgsql-port`), and `System.Data.SqlServerCe.dll` (SQL CE; deleted by the SQL CE tickets). So nothing is added to `external/` here.

**Engine constants differ between Debug and Release, and 3.1 shipped Release.** `BuildEXEs.bat` and `BuildSetup.bat` build `Configuration=Release`, so the deployed 3.1 binaries ran without `ACCUMULATOR` and `USEVISIT`. `USEVISIT` selects code paths in about 20 engine files (for example `Compiling/Compiler.cs` ~714–760, `Compiling/Visitors/PlanNodeVisitor.cs`, `Runtime.Instructions/AggregateNode*.cs`) and is also in SQLDevice's Debug set. `ACCUMULATOR` only subtracts compile time inside the timing operators (`Compiling/Plan.cs:868`, `Runtime.Instructions/SystemNodes.cs` ~2336–2373). `DISABLE_PERFORMANCE_COUNTERS` is read by no code. The spike's 62/14 coverage result was measured with the **Debug** set (convert.py reads only the Debug constants). Many legacy Release configurations also omit `TRACE`, which compiles out `System.Diagnostics.Trace` calls.

**Embedded resources are loaded by exact name.** DAE loads `Alphora.Dataphor.DAE.Schema.SystemCatalog.d4`, `…Schema.DataTypes.d4` and `…Debug.Debug.d4` (`Server/Engine.cs` ~1175–1296), and every exception class builds a `ResourceManager` from a literal base name (for example `"Alphora.Dataphor.DAE.Compiling.CompilerException"`). Legacy MSBuild named a `.resx` from the class in its `DependentUpon` source file when that metadata was present, otherwise from root namespace plus folder path. Two cases where the spike's generated names are wrong, so its output is not a reference for naming: SimpleDevice's `SimpleException.resx` has `DependentUpon SimpleException.cs`, whose class is `Alphora.Dataphor.DAE.Device.Simple.SimpleDeviceException` (the spike computes `DAE.Device.Simple.SimpleException`); and MSSQLDevice has an empty `RootNamespace` and loads `"SystemCatalog.d4"` and `"SystemCatalog.sql"` unqualified. Both projects are converted in `build-server-project`, but the rule this ticket sets is what they will follow. The `type=`/`mimetype=` entries in the engine `.resx` files are the standard header comment, not data, so no project here appears to need preserialized resources (`System.Resources.Extensions`); confirm.

**Signing today.** 60 `AssemblyInfo.cs` files contain `#if SIGNASSEMBLIES [assembly: AssemblyKeyFile("..\\..\\..\\..\\Dataphor.snk")] #else [assembly: AssemblyKeyFile("")] #endif`. The relative path is resolved from the legacy `obj\<Configuration>` folder and will not resolve from an SDK intermediate folder. `Dataphor.snk` (root, 596 bytes, a full key pair) is the key. No live project uses `InternalsVisibleTo`.

**Version attributes.** Every `AssemblyInfo.cs` here has `AssemblyVersion("3.1.*")`; a wildcard version forces `Deterministic=false` (compiler error CS8357 otherwise) until `build-version-and-signing` replaces it.

### What the spike gives you

`tools/net-spike` builds these projects out of tree today; read it before designing.

- `convert.py` reads each legacy project (`AssemblyName`, `RootNamespace`, `OutputType`, Debug `DefineConstants`, `Compile` and `EmbeddedResource` items, framework references, `HintPath` binaries, project references) and writes an SDK-style wrapper that compiles the same files by absolute path. Reusable: the overall approach, the explicit item lists, and `PACKAGE_MAP` (framework reference → NuGet package; a known-working set of versions, not a choice of current ones). Not to copy: the `ALWAYS` list that adds SqlClient, Odbc, OleDb and ConfigurationManager to every project; the `NoWarn` list, which hides `CS0618`, `SYSLIB0011`, `CA1416` and the NuGet vulnerability warnings `NU1902`–`NU1904`; reading only Debug constants; the resource naming described above; adding `GenerateResourceUsePreserializedResources` and `System.Resources.Extensions` everywhere.
- `run.py` holds `PATCHES` (exact old → new strings for all seven Appendix A patches), `STUBS` (which wrapper gets which stub file) and `COVERAGE_LIBS` (which library needs which assembly); `projects.txt` is the project list. `stubs/Engine.cs` exists only for the dead `using` in DAE's `SecurityNodes.cs`.
- `Smoke/` and `Cov/` start a full `Server`, so they cannot run against this ticket's output alone.

## Settled decisions that constrain this ticket

- D1: `net10.0` for everything here (none of these projects references WinForms or WPF); latest C# language version; nullable off; implicit usings off; no style or naming churn.
- D2: no .NET Framework build. Select NLS globalization (`System.Globalization.UseNls`, as a `RuntimeHostConfigurationOption` in `Directory.Build.props`) so every executable and test host built later inherits it. No project here produces a `runtimeconfig.json`, so the setting is first observable in `build-server-project` or `test-harness-inprocess`.
- D7: assembly names, namespaces and D4 class names unchanged; signing with `Dataphor.snk` in all configurations is the target, done by `build-version-and-signing`.
- D11: NuGet with central package management (`Directory.Packages.props`, `ManagePackageVersionsCentrally`); a binary stays in git only when no package exists.
- D14: directories and project file names are already final; do not rename again.
- The plan's WS1 design notes: generate from the legacy file and keep the exact `Compile` list (`EnableDefaultItems=false`, or at least `EnableDefaultCompileItems=false` plus the resource and `None` equivalents, because strays exist next to compiled files); `GenerateAssemblyInfo=false`; preserve `DefineConstants` (Appendix B); preserve embedded resource manifest names.
- Warning baseline: the build must leave `WFDEV006`, every `SYSLIB` obsoletion warning and `CA1416` visible (AGENTS.md; plan WS2 "Windows-only API"). Do not suppress `CS0618` wholesale either: it is the same class of signal for obsolete members that lack a diagnostic ID. Suppressing compiler noise the legacy build also produced (unused variables, hidden members and the like) is fine; list each suppressed code in `Directory.Build.props` with a one-line reason. `TreatWarningsAsErrors` stays off.
- Output location: nothing is built into source folders. Use the SDK's artifacts output (`UseArtifactsOutput` with `ArtifactsPath` at the repository's `artifacts/`, set in `Directory.Build.props`), which `.gitignore` already ignores. Do not reproduce the legacy library `OutputPath` of `..\` (into the library folder next to the `.d4l`). Until `build-runtime-layout` assembles `artifacts/layout/`, nothing in the repository loads library assemblies from library folders (the spike copies assemblies into a scratch library tree itself).
- `SIGNASSEMBLIES` is the one constant not carried over: Appendix B says it becomes unconditional signing, and keeping it would compile the broken relative `AssemblyKeyFile` path. Until `build-version-and-signing`, assemblies are unsigned in every configuration, as Debug builds were before.

## Open choices for the planner

- **Which engine constant set ships** (the plan requires this to be decided here, deliberately, and tested). Recommended default: one feature-constant set per project for both configurations, equal to the legacy Release set without `SIGNASSEMBLIES` and `DISABLE_PERFORMANCE_COUNTERS` (engine: `USENATIVE;NATIVEROW;USEOBJECTVALIDATE;NILPROPOGATION`; SQLDevice: none), with `DEBUG` only in Debug. Reason: that is what 3.1 users run, and a test run in Debug should exercise the same code paths as the shipped build. The alternative is to keep the legacy Debug/Release difference exactly. Either way, record the choice and its reason in `Directory.Build.props` or the project file, and re-measure the coverage scripts with the chosen set (see "Done"). Also decide whether to keep the legacy Release omission of `TRACE`; the default is to keep it as legacy had it per project.
- **Endpoint configuration names.** Default: the two constructors throw `NotSupportedException` naming the unsupported setting (the plan's Q8 default is that endpoint configuration from `app.config` is not used). If a coded Dataphor error is preferred, allocate it the way the existing codes are allocated (prefix table in `Error Message Prefix Codes.txt`, next free number in the owning exception class and its `.resx`). Either way it is a migration-notes entry.
- **Generator.** Whether to keep a conversion script in `tools/` for the projects converted later, or commit hand-checked output only. Default: commit the project files; extend the spike's converter if a script helps, and do not keep a second copy.
- **Package versions.** Current stable versions of `System.ServiceModel.Primitives`, `.NetTcp`, `.Http` (what Common and Client need), `Newtonsoft.Json` 13.x, and `System.Configuration.ConfigurationManager` only if a project here needs it. Whether to enable transitive pinning.

## The spike after this ticket

The spike must keep working, because `build-server-project` and other tickets in between use it to show that nothing changed. Converting a project in place breaks it twice: `convert.py` parses the legacy MSBuild 2003 XML namespace and reads `Debug` property groups, so an SDK-style file yields a wrapper with no sources and no constants; and `run.py` exits with "patch no longer applies" for each patch already made in the repository. Update the spike so that it either reads the new project files (no XML namespace; constants wherever this ticket puts them) or references the real SDK projects for the converted ones; remove landed patches from `PATCHES` and the `Engine` stub from `STUBS`. Keep its K4, K1 and K10 patches working for the code they patch (Base's `ThreadUtility.cs` is now in a converted project; if the spike references the real Base project, its Debug server start hits K4, so wrapping Base, or running in Release, is needed until `build-server-project` lands K4).

## What done looks like

- On a clean clone with only the .NET 10 SDK: `dotnet build Dataphor.slnx` succeeds with zero errors in Debug and in Release; no file appears outside `artifacts/` (`git status` clean after the build).
- The warning output still shows `WFDEV006`, `SYSLIB*` and `CA1416` where they occur; the suppressed list is in `Directory.Build.props` with reasons.
- Each built assembly's manifest resource names equal the names the legacy rules produce (check by listing `GetManifestResourceNames()` for each assembly and comparing with the expected list; record the command).
- Runtime check, since no harness exists yet (`test-harness-inprocess` owns the tests): a throwaway program under `tools/net-spike/` that references the real DAE project starts an in-process engine (`Alphora.Dataphor.DAE.Server.Engine`, as `src/protocol/Client/Server/LocalServer.cs` ~49–52 does), connects as `new SessionInfo("Admin", "")`, and evaluates a few D4 expressions. Starting the engine loads the embedded system catalog scripts by name, so this also checks resource names. Run it in both Debug and Release; Debug is the reproduction for K4 (below). The operator and type counts will be lower than the spike's full-server numbers; record what it prints.
- `python tools/net-spike/run.py <work-dir> --smoke --coverage` still runs and reports engine/server/libraries 0 errors, concurrency 0 errors and coverage `pass=62 fail=14`, with the constant set this ticket chose. If the chosen set changes any coverage result, stop and report it in the handoff rather than adjusting scripts.
- The "Build and test" section of `AGENTS.md` states the real commands, the output location (`artifacts/`), which projects are converted and which still need the spike, and the conventions a later conversion must follow (explicit item lists, constants, resource names, warning baseline, central packages). The layout table is not this ticket's.
- `docs/migration-notes.md`: add entries for Newtonsoft.Json moving from 9.0.1 to 13 (custom extensions that reference it must use 13) and for the WCF client constructors that took an endpoint configuration name. Leave the NLS entry planned until an executable built here carries the setting.

## Risks

- A resource name that changes silently fails only when that exception is raised or that script is loaded. The manifest comparison above is the guard.
- A constant set that differs from what users ran changes D4 behaviour with no reference runtime to compare against (D2). The coverage re-run is the guard; it covers the engine, not SQLDevice's translation (`USEVISIT` there is unexercised until `test-device-sql-golden`).
- `CA1416` will report Windows-only calls in `net10.0` projects (event log, registry, `Base.Windows`). That is intended; do not switch these projects to `net10.0-windows` to quiet it.
- NuGet vulnerability warnings for old packages are expected and stay visible; the audit is `security-dependency-audit` (`feat-ws11-security`).

If this splits, the ticket that finishes the work item keeps the slug `build-core-sdk-projects`; earlier pieces get new slugs and are chained to it with `prereq:`.
