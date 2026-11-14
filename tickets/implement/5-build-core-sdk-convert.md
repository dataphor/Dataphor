description: Replace the old Visual Studio project files for the engine, the client protocol libraries and four system libraries with modern ones, with shared build settings in one place, so that this core builds on .NET 10 from a clean checkout with one command.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.props, Directory.Packages.props, Dataphor.slnx, src/engine/Base/Base.csproj, src/engine/Base/ThreadUtility.cs, src/engine/Base.Windows/Base.Windows.csproj, src/engine/DAE/DAE.csproj, src/engine/DAE/Runtime.Instructions/SecurityNodes.cs, src/protocol/Common/Common.csproj, src/protocol/Common/WCF/ServiceClient.cs, src/protocol/NativeCLI/DAE.NativeCLI.csproj, src/protocol/NativeCLI/packages.config, src/protocol/Client/Client.csproj, src/protocol/Client/Client/ClientServer.cs, libraries/SQLDevice/Source/DAE.Device.SQL.csproj, libraries/System.Platform/Source/System.Platform.csproj, libraries/System.Platform/Source/Strings.cs, libraries/System.Internet/Source/System.Internet.csproj, libraries/System.Internet/Source/packages.config, libraries/System.Integration/Source/System.Integration.csproj, tools/net-spike/EngineCheck/, AGENTS.md, docs/migration-notes.md, docs/modernization-plan.md
difficulty: hard
----
# Convert the core projects to SDK-style projects

This is the first half of the work item `build-core-sdk-projects` (plan WS1). It converts ten projects in place and adds the root build files. The second half, the ticket `build-core-sdk-projects`, updates the spike in `tools/net-spike` to work with the converted projects, re-runs the coverage scripts and rewrites the "Build and test" section of `AGENTS.md`. **After this ticket, `python tools/net-spike/run.py` stops working** (its converter reads only legacy project files, and its patches for the files fixed here no longer apply). That is expected; the next ticket repairs it. Do not touch `tools/net-spike/convert.py`, `run.py`, `projects.txt` or `stubs/` here.

## Goal

On a clean clone with only the .NET 10 SDK (pinned by the existing `global.json`; leave it as it is), `dotnet build Dataphor.slnx` compiles these ten projects with zero errors in Debug and in Release, writes nothing outside `artifacts/`, keeps every assembly name, namespace and embedded-resource name, and an in-process engine starts and evaluates D4 in both configurations.

| Project file | Assembly | Root namespace | References |
|---|---|---|---|
| `src/engine/Base/Base.csproj` | `Alphora.Dataphor` | `Alphora.Dataphor` | — |
| `src/engine/Base.Windows/Base.Windows.csproj` | `Alphora.Dataphor.Windows` | `Alphora.Dataphor.Windows` | — |
| `src/engine/DAE/DAE.csproj` | `Alphora.Dataphor.DAE` | `Alphora.Dataphor.DAE` | Base |
| `src/protocol/Common/Common.csproj` | `AlphoraDataphorCommon` | `Alphora.Dataphor.DAE` | WCF client packages |
| `src/protocol/NativeCLI/DAE.NativeCLI.csproj` | `Alphora.Dataphor.DAE.NativeCLI` | same | Common; WCF client packages; Newtonsoft.Json |
| `src/protocol/Client/Client.csproj` | `AlphoraDataphorClient` | `Alphora.Dataphor.DAE.Client` | Base, Base.Windows, Common, DAE; WCF client packages |
| `libraries/SQLDevice/Source/DAE.Device.SQL.csproj` | `Alphora.Dataphor.DAE.Device.SQL` | same | Base, DAE only (see below) |
| `libraries/System.Platform/Source/System.Platform.csproj` | `System.Platform` | `Alphora.Dataphor.Libraries.System.Platform` | Base, Base.Windows, DAE |
| `libraries/System.Internet/Source/System.Internet.csproj` | `System.Internet` | `Alphora.Dataphor.Libraries.System.Internet` | Base, DAE; Newtonsoft.Json |
| `libraries/System.Integration/Source/System.Integration.csproj` | `System.Integration` | `Alphora.Dataphor.Libraries.System.Integration` | Base, DAE |

Out of scope: the server and the five projects that need it (Store.SQLite, Connection, MSSQLDevice, SimpleDevice, Frontend) go to `build-server-project`; versions and signing to `build-version-and-signing`; `artifacts/layout/` to `build-runtime-layout`; tests to `test-harness-inprocess`; default file globbing to `build-globbing-cleanup`; UI and host projects to WS5/WS6.

## Decisions settled at plan time

**Engine constant set: the set 3.1 shipped, in both configurations.** 3.1 was built and deployed in Release, so users ran the engine without `ACCUMULATOR` and `USEVISIT`. Every project gets one feature-constant set used in both configurations, equal to its legacy Release set minus `SIGNASSEMBLIES` (becomes unconditional signing in `build-version-and-signing`) and `DISABLE_PERFORMANCE_COUNTERS` (no code reads it); Debug adds `DEBUG`; `TRACE` stays where legacy had it. A Debug test run therefore exercises the same code paths as the shipped build. **Measured at plan time:** a copy of the spike with DAE built with `DEBUG;TRACE;USENATIVE;NATIVEROW;USEOBJECTVALIDATE;NILPROPOGATION` and SQLDevice with `DEBUG;TRACE` gave smoke results identical to the baseline (1,080 operators, 23 scalar types, concurrency 0 errors, resolution cache 0 errors) and coverage `pass=62 fail=14`, with the same 14 scripts failing as listed in plan Appendix A. Record the choice and its reason as a comment next to the constants in `DAE.csproj`, and as one sentence in plan Appendix B, replacing "decide deliberately which set ships, and test with it" with the decision.

Set `DefineConstants` outright in each project (do not append to `$(DefineConstants)`), so the SDK's default `DEBUG;TRACE` / `RELEASE;TRACE` does not leak in. The SDK still appends its framework symbols (`NET`, `NET10_0`, `NETCOREAPP`, …); no code in these projects tests them, nor `RELEASE` (checked by grep).

| Project | Debug | Release |
|---|---|---|
| Base | `DEBUG;TRACE` | (empty) |
| Base.Windows | `DEBUG;TRACE` | `TRACE` |
| DAE | `DEBUG;TRACE;USENATIVE;NATIVEROW;USEOBJECTVALIDATE;NILPROPOGATION` | `USENATIVE;NATIVEROW;USEOBJECTVALIDATE;NILPROPOGATION` |
| Common | `DEBUG;TRACE` | `TRACE` |
| NativeCLI | `DEBUG;TRACE` | (empty) |
| Client | `DEBUG;TRACE` | `TRACE` |
| SQLDevice | `DEBUG;TRACE` | (empty) |
| System.Platform | `DEBUG;TRACE` | `TRACE` |
| System.Internet | `DEBUG;TRACE` | (empty) |
| System.Integration | `DEBUG;TRACE` | `TRACE` |

(`TRACE` only enables two `Trace.WriteLine` calls in these projects: `src/engine/DAE/Runtime.Data/OrderTables.cs:703` and `src/engine/Base/ExceptionUtility.cs:31`.)

**SQLDevice stays in this ticket.** Verified at plan time: SQLDevice's sources compile against Base and DAE alone (the spike's wrapper with the Server and Connection project references removed built with 0 errors). Drop those two references. SQLDevice's `.d4l` still requires the Connection library at the D4 level; that is unaffected.

**Endpoint configuration names throw `NotSupportedException`.** The .NET WCF client packages have no `ChannelFactory<T>(string endpointConfigurationName)`, and the plan (Q8) assumes WCF endpoint configuration from `app.config` is unused. Not a coded Dataphor error: it is a client-side configuration error, not a D4 runtime error, and it needs no error-code allocation. Change the two sites:

- `src/protocol/Common/WCF/ServiceClient.cs` ~57, `ServiceClient(string endpointConfigurationName)`: throw `NotSupportedException` with a message that says endpoint configuration names (WCF endpoints read from an application configuration file) are not supported on this version and to connect with a binding and endpoint address instead. This also covers `DataphorServiceClient<T>(string)` (`src/protocol/Common/Contracts/DataphorServiceClient.cs`) and `NativeCLIClient(hostName, instanceName, endpointConfigurationName)` (`src/protocol/NativeCLI/NativeCLI.cs` ~35), which chain to it. Keep the constructors (public API); only their behaviour changes.
- `src/protocol/Client/Client/ClientServer.cs` ~152, the `ClientConfigurationName` branch of `Open()`: throw the same exception type, naming the connection alias setting `ClientConfigurationName` ("Config Name" in the alias editor). `ServerConnection` (`src/client/DAE.Client/ServerConnection.cs`) already cleans up and rethrows, so the user sees the message on connect. Alias files that carry the setting still load; only connecting fails.

Leave `ChannelFactoryWrapper`'s `(string, ChannelFactory<T>)` constructor in place (internal; now unused).

**Packages.** Central package management, transitive pinning off (nothing needs pinning yet; `security-dependency-audit` decides that later). Versions current at planning (2026-11-14): `System.ServiceModel.Primitives`, `System.ServiceModel.NetTcp` and `System.ServiceModel.Http` 10.1.0; `Newtonsoft.Json` 13.0.4. Use these unless a newer stable patch exists. Common, NativeCLI and Client each reference the three WCF packages (the legacy projects each referenced `System.ServiceModel`); NativeCLI and System.Internet reference `Newtonsoft.Json`. No project here needs `System.Configuration.ConfigurationManager`, `System.Data.SqlClient`, `Odbc` or `OleDb` (grep found no use); do not copy the spike's `ALWAYS` list. System.Internet's legacy `System.Web` reference is only `HttpUtility`, which is part of .NET 10: no package. Delete `src/protocol/NativeCLI/packages.config` and `libraries/System.Internet/Source/packages.config`, which an SDK-style project does not read.

**Solution.** `Dataphor.slnx` at the root, with solution folders for `engine`, `protocol` and `libraries`, and the root build files as solution items. No solution filter yet: until a Windows UI project joins the solution, the solution *is* the engine/server set; the filters come with the first UI project (WS6, `ui-client-layering`). Keep the legacy `Dataphor.sln` (deleted by `build-remove-legacy-build`); always name `Dataphor.slnx` on the command line.

## Root build files

`Directory.Build.props` (comment each group in one line; this file is copied as the convention by every later conversion):

- `TargetFramework` `net10.0` (later Windows UI projects override it with `net10.0-windows`), `LangVersion` `latest`, `Nullable` `disable`, `ImplicitUsings` `disable`.
- `EnableDefaultItems` `false`: every project lists its `Compile` and `EmbeddedResource` items explicitly, copied from the legacy file, because stray sources sit next to compiled ones (for example `src/engine/DAE/Runtime.Instructions/GeneratorNodes.cs` is on disk and not compiled).
- `EmbeddedResourceUseDependentUponConvention` `false`. **Verified at plan time:** with the SDK default (`true`) a `Foo.resx` next to `Foo.cs` is named after the first class in `Foo.cs`, which legacy MSBuild did only when the item had explicit `DependentUpon` metadata. With the convention off and each item's `DependentUpon` copied from the legacy file, the SDK produces the legacy names (same `CreateCSharpManifestResourceName` task). Example where it matters: `libraries/System.Platform/Source/Strings.cs` declares namespace `…Libraries.System.Integration`, so the convention would rename System.Platform's resource.
- `GenerateAssemblyInfo` `false` (the `AssemblyInfo.cs` files carry `DAERegister` and the version attributes); `Deterministic` `false` with a `NOTE:` that it is required by `AssemblyVersion("3.1.*")` and removed by `build-version-and-signing`.
- `UseArtifactsOutput` `true` and `ArtifactsPath` `$(MSBuildThisFileDirectory)artifacts` (these must be in `Directory.Build.props`, not a project file). `.gitignore` already ignores `artifacts/`. Output lands in `artifacts/bin/<project file name>/<debug|release>/`.
- NLS globalization for every executable and test host built later: `<RuntimeHostConfigurationOption Include="System.Globalization.UseNls" Value="true" />` in an item group, with a comment. No project here writes a `runtimeconfig.json` except the throwaway engine check below; the migration-notes NLS entry stays **planned**.
- Warning baseline: `TreatWarningsAsErrors` stays off. A `NoWarn` list may hold only compiler noise the legacy build also produced (for example unused variables `CS0168`/`CS0219`, unassigned or unused fields `CS0649`/`CS0169`/`CS0414`, hiding `CS0108`/`CS0114`, unused events `CS0067`, unreachable code `CS0162`); add a code only if it actually appears, each with a one-line reason. **Never** suppress `CS0618`, any `SYSLIB*`, `WFDEV*`, `CA1416` or `NU1900`–`NU1904`. Expect many visible `SYSLIB0051` warnings (serialization constructors on the exception classes), some `CA1416` (Windows-only calls in Base.Windows and elsewhere) and `CS1699` from the `AssemblyKeyFile("")` attribute in the `#else` branch of each `AssemblyInfo.cs` (removed by `build-version-and-signing`; suppressing `CS1699` with that reason is acceptable). Do not switch any project to `net10.0-windows` to quiet `CA1416`.
- `NU1605` (package downgrade) is an error by default; leave it that way.

`Directory.Packages.props`: `ManagePackageVersionsCentrally` `true`, the four `PackageVersion` items above.

## Each project file

Generate from the legacy file and keep, exactly: `AssemblyName`, `RootNamespace`, `OutputType` (all `Library`), every `Compile` item (same order, same paths; legacy lists use `%28`/`%29` escapes in a few places elsewhere — check these ten), every `EmbeddedResource` item with its `DependentUpon` metadata, `ProjectReference`s (minus SQLDevice's two), and `AllowUnsafeBlocks` `true` where either legacy configuration had it (Base, DAE, SQLDevice). Drop: `TargetFrameworkVersion`, `OutputPath` (including the libraries' `..\`, which built into the library folder — `build-runtime-layout` places library assemblies from now on), `DocumentationFile` (Release only; nothing consumes it), `BaseAddress`, `FileAlignment`, `WarningLevel`, `DebugType`, `Prefer32Bit`, `PlatformTarget`, empty pre/post-build events, `Content` items (`BNF.txt` and the like; no copy-to-output, no build effect), `None` items for `app.config` (they only declare `supportedRuntime` .NET Framework 4.5.2) and `packages.config`, `Service`/`BootstrapperPackage` items, framework `Reference` items. A project file that keeps an explicit `Configuration`/`Platform` default is unnecessary in SDK style.

Use the legacy file's encoding for the new file only if convenient; the project files are being replaced, not edited, so UTF-8 with CRLF and tab or two-space indentation is fine. Keep `DAE.csproj`'s file name (`D14`: names are final).

## Expected embedded-resource names

Computed from the legacy rules (root namespace + folder path + file name; a `.resx` with `DependentUpon` takes the first class in that `.cs` file). Each built assembly's `GetManifestResourceNames()` must equal this list exactly; the engine check below prints them.

- `Alphora.Dataphor`: `Alphora.Dataphor.Disposable.resources` (`Base.resx` has `DependentUpon Base.cs`, whose first class is `Disposable`; nothing loads it), `Alphora.Dataphor.BaseException.resources`, `Alphora.Dataphor.BOP.BOPException.resources`.
- `Alphora.Dataphor.DAE` (22): `Alphora.Dataphor.DAE.` + `Compiling.CompilerException`, `Connection.ConnectionException`, `Device.ApplicationTransaction.ApplicationTransactionException`, `Device.Catalog.CatalogException`, `Device.Memory.MemoryDeviceException`, `Device.DeviceException`, `Language.LanguageException`, `Language.LexerException`, `Language.ParserException`, `Language.SyntaxException`, `Runtime.Data.IndexException`, `Runtime.Data.ScanException`, `Runtime.RuntimeException`, `Schema.SchemaException`, `Server.ServerException`, `Store.StoreException`, `Streams.ConveyorException`, `Streams.StreamsException`, `Strings` (each with `.resources`), and `Alphora.Dataphor.DAE.Schema.DataTypes.d4`, `Alphora.Dataphor.DAE.Schema.SystemCatalog.d4`, `Alphora.Dataphor.DAE.Debug.Debug.d4`.
- `Alphora.Dataphor.DAE.Device.SQL`: `Alphora.Dataphor.DAE.Device.SQL.SQLException.resources` (the stray `SystemCatalog.resx` in that folder is not in the legacy project and stays out).
- `System.Platform`: `Alphora.Dataphor.Libraries.System.Platform.Strings.resources`.
- `System.Integration`: `Alphora.Dataphor.Libraries.System.Integration.Strings.resources`.
- `Alphora.Dataphor.Windows`, `AlphoraDataphorCommon`, `Alphora.Dataphor.DAE.NativeCLI`, `AlphoraDataphorClient`, `System.Internet`: none.

The `type=`/`mimetype=` entries in the engine `.resx` files are inside the standard header comment, so no project should need preserialized resources. If the build reports `MSB3822`/`MSB3823` for a file, that file has a real non-string resource: add `System.Resources.Extensions` and `GenerateResourceUsePreserializedResources` to that project only, and say so in the handoff.

**System.Platform's string table (local fix).** `libraries/System.Platform/Source/Strings.cs` sets `BaseName = "Alphora.Dataphor.Libraries.System.Integration.Strings"` (copied from System.Integration), but its resource has always been named `…System.Platform.Strings`, so `Strings.Get` in System.Platform would throw `MissingManifestResourceException`. Nothing calls it today. Keep the legacy resource name (the rule) and change the `BaseName` constant to `"Alphora.Dataphor.Libraries.System.Platform.Strings"`. Leave the class's namespace alone (a public type name). The file has a byte-order mark or not — keep what it has. The engine check reproduces it (below).

## K4: thread names in Debug builds

`src/engine/Base/ThreadUtility.cs`: `SetThreadName` reflects on the private field `Thread.m_Name`, which does not exist on .NET 10, so the first process call in a Debug build throws `NullReferenceException` (`ServerProcess.cs` ~1750, ~1770, ~1805; `[Conditional("DEBUG")]` is evaluated in the caller and DAE defines `DEBUG` in Debug). **Verified at plan time on .NET 10.0.15:** `Thread.Name` can be assigned repeatedly, including `null` to clear it, both on the current thread and on another running thread; `typeof(Thread).GetField("m_Name", NonPublic | Instance)` returns null. Replace the reflection with `thread.Name = name;`, delete `GetThreadNameFieldInfo` and its field, drop `using System.Reflection` if unused, keep `[Conditional("DEBUG")]` and the method signature, and update the remark (the write-once workaround is no longer needed). The file has no byte-order mark, CRLF, tabs.

## Compile-only patch

`src/engine/DAE/Runtime.Instructions/SecurityNodes.cs:10`: delete the unused `using System.Runtime.Remoting.Messaging;` (no BOM, CRLF). Its twin in `src/engine/Server` belongs to `build-server-project`.

## Engine check (runtime verification and K4 reproduction)

There is no test harness yet (`test-harness-inprocess` owns it), so add a throwaway console program `tools/net-spike/EngineCheck/` (`EngineCheck.csproj` + `Program.cs`). It is a real SDK project inside the repository, so it inherits `Directory.Build.props` (artifacts output, NLS), and it is **not** listed in `Dataphor.slnx`. It is deleted with the rest of `tools/net-spike`. It references all ten projects and, in this order, each step reporting and continuing on failure:

- prints each of the ten assemblies' sorted `GetManifestResourceNames()` (load each by assembly name) and compares them with the expected lists above, embedded in the program; any difference is a failure;
- calls System.Platform's `Strings.Get` for the one entry in `libraries/System.Platform/Source/Strings.resx` through reflection (`Type.GetType("Alphora.Dataphor.Libraries.System.Integration.Strings, System.Platform")`; the same type name also exists in System.Integration, so do not name the type in code) and checks it returns the resource text — the reproduction for the `BaseName` fix;
- creates `new Alphora.Dataphor.DAE.Server.Engine()`, sets `Name`, calls `Start()` (as `src/protocol/Client/Server/LocalServer.cs` ~49–52 does; a bare engine is in-memory and writes no files), connects with `((IServer)engine).Connect(new SessionInfo("Admin", ""))`, starts a process and evaluates `1 + 1`, `'Hello' + ' ' + 'D4'`, `Count(System.Operators)`, `Count(System.ScalarTypes)` and `Sum(X from table { row { 1 X }, row { 2 X }, row { 3 X } })` (the spike's `Smoke/Program.cs` shows the plan/evaluate calls), then stops the process, disconnects and disposes;
- prepares a statement with a deliberate syntax error and prints the exception's code and message: a real message text shows the parser's `.resx` was found by name;
- prints the configuration (`#if DEBUG`), `RuntimeInformation.FrameworkDescription` and the operator and scalar-type counts; returns a non-zero exit code on any failure.

Run `dotnet run --project tools/net-spike/EngineCheck -c Debug` and `-c Release`. **Debug is K4's reproduction:** before the `ThreadUtility` change it fails at the first process call; after it, it passes. Record both runs' output in the handoff. The counts will be lower than the spike's full-server 1,080 operators; that is expected.

## Edge cases & interactions

- **Resource names** — a renamed resource fails only when that exception is raised or that script is loaded. Verified by the engine check's comparison (all ten assemblies, both configurations) and by engine start, which loads the three `.d4` scripts by name.
- **SDK default items** — with `EnableDefaultItems` false, nothing is compiled or embedded that the legacy list did not name. Verify by inspection: `Compile` count per project equals the legacy count (Base 35, Base.Windows 3, DAE 241, Common 13, NativeCLI 11, Client 24, SQLDevice 8, System.Platform 4, System.Internet 7, System.Integration 4) and `EmbeddedResource` counts (Base 3, DAE 22, SQLDevice 1, System.Platform 1, System.Integration 1, others 0).
- **Nothing written to source folders** — run `git status --porcelain --ignored` before and after `dotnet build Dataphor.slnx` (Debug and Release) and the two engine-check runs: the only new entry is `artifacts/`. Plain `git status` misses ignored `bin/`/`obj/` folders, so use `--ignored`. Do not use `git clean`.
- **Debug and Release both build** — the constants differ by `DEBUG`/`TRACE`; `#if DEBUG` code (and `[Conditional("DEBUG")]` callers) compiles only in Debug. Verified by building both.
- **Warnings stay visible** — verify by grepping the build output: `SYSLIB0051` (and any other `SYSLIB`), `CA1416` and `CS0618` occurrences are reported; the suppressed list in `Directory.Build.props` contains none of the forbidden codes. Record the warning counts by code in the handoff (they become the baseline later tickets compare against).
- **Endpoint configuration name passed** — `ServiceClient(string)` and `ClientServer.Open()` with a configuration name throw `NotSupportedException` naming the setting, before any channel factory is created; an empty or null name still takes the binding/address path in `ClientServer` (`String.IsNullOrEmpty` check unchanged). Verified by inspection; there is no server to connect to until WS5.
- **`Thread.Name` on another thread and null** — verified at plan time (see K4); the engine check's Debug run exercises the real call path.
- **Package versions vs. the spike** — the spike's wrappers pin the WCF packages at 8.1.2 and Newtonsoft.Json at 13.0.3; once they reference these projects, `NU1605` (downgrade, an error) would fire. That is the next ticket's to fix (it reads versions from `Directory.Packages.props`); do not lower the versions here to suit the spike.
- **Root `Directory.Build.props` reaches every project under the root**, including legacy-format project files that are not converted yet and the `legacy/` tree. Nothing builds those with `dotnet`, so there is no effect now; later conversions inherit the settings on purpose.
- **Duplicate project file names** — artifacts output is keyed by project file name. Among non-legacy project files only `DAE.Connection.ODBC.csproj` appears twice (`device-connection-cleanup` deletes the old one); none of the ten here collide.

## Documentation in this ticket

- `docs/migration-notes.md`, "Custom C# extensions": **done** — Dataphor references Newtonsoft.Json 13 instead of 9 (NativeCLI and System.Internet); extensions that use Newtonsoft.Json must build against 13. **done** — the WCF client constructors that took an endpoint configuration name (`ServiceClient<T>(string)`, `DataphorServiceClient<T>(string)`, `NativeCLIClient(host, instance, configurationName)`) and a connection alias's `ClientConfigurationName` setting throw `NotSupportedException`; connect with host, instance and port settings instead, and check deployed configuration files for `<system.serviceModel>` sections (Q8). Put the alias part under "Forms and the Windows client" if that reads better. Leave the NLS entry **planned**.
- `docs/modernization-plan.md` Appendix B: the constant-set decision (one sentence, see above).
- `AGENTS.md` "Build and test": add one interim paragraph at the top saying `dotnet build Dataphor.slnx` now builds the ten converted projects into `artifacts/`, and that the spike is being updated by `build-core-sdk-projects` (which rewrites the section). Do not rewrite the rest.

## TODO

- Write `Directory.Build.props`, `Directory.Packages.props` and `Dataphor.slnx` as above.
- Replace the ten legacy project files with SDK-style files (generate from the legacy XML with a throwaway script outside the repository, then check by hand; do not commit the script).
- Delete the two `packages.config` files.
- The dead `using` in DAE's `SecurityNodes.cs` and the two endpoint-configuration-name sites (needed to compile).
- Build `Dataphor.slnx` in Debug and Release; fix errors without suppressing the forbidden warnings; settle the `NoWarn` list with reasons.
- Add `tools/net-spike/EngineCheck/` and run it in Debug **before** changing `ThreadUtility.cs` and `Strings.cs`: it must fail at the first process call (K4) — record that output as the reproduction. Then make the K4 change and System.Platform's `BaseName` change, and run it again in Debug and in Release: both pass.
- Check `git status --porcelain --ignored` before/after.
- Migration notes, plan Appendix B sentence, interim `AGENTS.md` paragraph.
- Handoff: warning counts by code, both engine-check outputs, any project or resource that needed special handling, and the reminder that the spike is broken until `build-core-sdk-projects`.
