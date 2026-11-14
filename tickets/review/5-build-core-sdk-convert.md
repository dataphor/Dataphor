description: The engine, the client protocol libraries and four system libraries now build on .NET 10 from a clean checkout with one command, using modern project files and shared build settings; review the conversion, the two runtime fixes it needed, and the engine check that shows the result runs.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.props, Directory.Packages.props, Dataphor.slnx, src/engine/Base/Base.csproj, src/engine/Base/ThreadUtility.cs, src/engine/Base.Windows/Base.Windows.csproj, src/engine/DAE/DAE.csproj, src/engine/DAE/Runtime.Instructions/SecurityNodes.cs, src/protocol/Common/Common.csproj, src/protocol/Common/WCF/ServiceClient.cs, src/protocol/NativeCLI/DAE.NativeCLI.csproj, src/protocol/Client/Client.csproj, src/protocol/Client/Client/ClientServer.cs, libraries/SQLDevice/Source/DAE.Device.SQL.csproj, libraries/System.Platform/Source/System.Platform.csproj, libraries/System.Platform/Source/Strings.cs, libraries/System.Internet/Source/System.Internet.csproj, libraries/System.Integration/Source/System.Integration.csproj, tools/net-spike/EngineCheck/EngineCheck.csproj, tools/net-spike/EngineCheck/Program.cs, AGENTS.md, docs/migration-notes.md, docs/modernization-plan.md, tickets/backlog/feat-ws7-devices-and-libraries.md, tickets/plan/7-build-server-project.md
----
# Review: convert the core projects to SDK-style projects

First half of the work item `build-core-sdk-projects` (plan WS1). The second half, the ticket `build-core-sdk-projects` (in `implement/`), repairs the spike, re-runs the coverage scripts and rewrites `AGENTS.md` "Build and test". **`python tools/net-spike/run.py` is broken from this change until that ticket lands** (its converter reads only legacy project files, and three of its patches now find their target already fixed). That is expected; `convert.py`, `run.py`, `projects.txt` and `stubs/` were not touched.

## What changed

- **Root build files.** `Directory.Build.props` (net10.0, `LangVersion latest`, nullable and implicit usings off, `EnableDefaultItems=false`, `EmbeddedResourceUseDependentUponConvention=false`, `GenerateAssemblyInfo=false`, `Deterministic=false` with a `NOTE:` for `build-version-and-signing`, artifacts output under `artifacts/`, the `System.Globalization.UseNls` host option, `TreatWarningsAsErrors=false`, a comment stating the constants convention and the never-suppress list). `Directory.Packages.props` (central versions, transitive pinning off). `Dataphor.slnx` (folders `engine`, `protocol`, `libraries`; the three root build files as solution items). Legacy `Dataphor.sln` kept.
- **Ten project files replaced**, generated from the legacy XML by a script outside the repository (not committed) and checked by hand. Each keeps `AssemblyName`, `RootNamespace`, `OutputType`, every `Compile` item in legacy order and path, every `EmbeddedResource` with its `DependentUpon`, the project references (SQLDevice drops Server and Connection), and `AllowUnsafeBlocks` where either legacy configuration had it (Base, DAE, SQLDevice). Constants set outright per the ticket's table, Release set unconditional and the Debug set under `'$(Configuration)' == 'Debug'`; DAE's comment records the decision. The files keep the legacy UTF-8 byte-order mark; CRLF, two-space indent (as `.editorconfig` asks for MSBuild files).
- **Packages.** WCF client packages 10.1.0 (Common, NativeCLI, Client), Newtonsoft.Json 13.0.4 (NativeCLI, System.Internet), and — not in the ticket — **System.Data.SqlClient 4.9.1 in DAE** (see deviations). Both `packages.config` files deleted (`git rm`, so the deletions are staged).
- **Source fixes.** Dead `using System.Runtime.Remoting.Messaging` removed from DAE's `SecurityNodes.cs`. `ServiceClient(string)` and the `ClientConfigurationName` branch of `ClientServer.Open()` throw `NotSupportedException` with a message naming what is unsupported and what to do instead; constructors kept. K4: `ThreadUtility.SetThreadName` assigns `Thread.Name` (reflection helper and its field deleted, `using System.Reflection` dropped, `[Conditional("DEBUG")]` and signature kept). System.Platform's `Strings.BaseName` now names `…System.Platform.Strings` (namespace of the class unchanged). Each edited file keeps its byte-order-mark state, CRLF and tabs.
- **Engine check** `tools/net-spike/EngineCheck/` (real SDK project, not in the solution, deleted with the spike). See below for how it differs from the ticket's script.
- **Docs.** `docs/migration-notes.md`: two **done** rows under "Custom C# extensions" (Newtonsoft.Json 13; the endpoint-configuration-name constructors) and one under "Forms and the Windows client" (the alias `ClientConfigurationName` setting); the NLS row stays **planned**. Plan Appendix B: the constant-set decision replaces "decide deliberately…", and the SQLDevice `USEVISIT` / Dataphoria `TRACEFOCUS` items are marked Debug-only; the WS1 design-note bullet "preserve each project's `DefineConstants` exactly" now points at Appendix B instead of contradicting it. `AGENTS.md`: interim paragraph at the top of "Build and test", and the paragraph below it reworded so it no longer says nothing builds with `dotnet build`.
- **Board.** `feat-ws7-devices-and-libraries` (backlog): arm added to `device-mssql-port` for the engine's `ClearAllPools` call; its Newtonsoft.Json bullet marked done. `build-server-project` (plan): notes that System.Data.SqlClient 4.9.1 is already central.

## Deviations from the ticket, and why

1. **DAE needs System.Data.SqlClient.** The plan-time grep missed `Engine.Dispose` (`src/engine/DAE/Server/Engine.cs:142`), which calls `System.Data.SqlClient.SqlConnection.ClearAllPools()` fully qualified (added in 2019 to release pooled SQL Server connections on stop). On .NET 10 that type is not in the framework, so DAE did not compile (`CS1069`). I kept the 3.1 behaviour and referenced the package (4.9.1, latest stable, not deprecated, no audit warning) with a `NOTE:` in `DAE.csproj`; the alternative, deleting the call, changes behaviour for SQL Server catalog stores and devices. The warning `CS0618` ("use Microsoft.Data.SqlClient") stays visible. `build-server-project` needs the same package for `Connection/MSSQL*.cs` anyway. When SQL Server access moves to Microsoft.Data.SqlClient the call must follow (arm on `device-mssql-port`).
2. **A bare `Engine` cannot evaluate D4 as the ticket assumed.** `Engine.IsEngine` is `true`: on its own the engine is the client-side catalog cache, and `RegisterCatalog` runs `SystemCatalog.d4` and `Debug.d4` only when `!IsEngine` (the `Server` subclass). Overriding `IsEngine` does not work either (the compiler then expects the server's persistent catalog store: `NullReferenceException` in `CatalogDeviceSession.ResolveOperatorName`). And `SystemCatalog.d4` names classes that only the Server assembly registers. So the check:
   - connects as `Admin` with default library `System` (the default `General` library does not exist in a bare engine; `LocalSession` does the same with the system user);
   - runs both scripts itself, loaded by resource name, **batch by batch**, skipping batches that fail with "Registered class … not found" (3 of 20 in `SystemCatalog.d4`: lines 543, 1131 and 1133, the last being the block from line 1133 to the end that holds the security grants and the catalog tables); any other batch error is a failure;
   - counts operators and scalar types from `engine.Catalog` instead of `Count(System.Operators)` / `Count(System.ScalarTypes)`, which need the catalog tables in the skipped block;
   - raises the syntax error with `plan.CheckCompiled()` (prepare records compile errors in the plan's messages and does not throw);
   - has an opt-in `ENGINECHECK_TRACE=1` that prints first-chance exceptions with their stacks, because `Engine.Start` rewraps exceptions and loses the origin (that is how the K4 site below was found).
3. **No `NoWarn` list.** Only `CS0168` (2) and `CS0162` (1) of the allowed noise appear; not worth hiding, and setting `NoWarn` in `Directory.Build.props` would also drop the SDK's default `1701;1702`. `CS1699` (from `AssemblyKeyFile("")`) does not appear at all.
4. Nothing needed preserialized resources (no `MSB3822`/`MSB3823`).

## Verification (all run in this ticket)

- `dotnet build Dataphor.slnx` and `-c Release` from an empty `artifacts/`: **0 errors** each, 61 warnings each. `git status --porcelain --ignored` before and after both builds and both engine-check runs: the only new entry is `artifacts/`.
- Evaluated items (`dotnet msbuild -getItem`): Compile Base 35, Base.Windows 3, DAE 241, Common 13, NativeCLI 11, Client 24, SQLDevice 8, System.Platform 4, System.Internet 7, System.Integration 4; EmbeddedResource Base 3, DAE 22, SQLDevice 1, System.Platform 1, System.Integration 1, others 0; no `None`/`Content` items. Same as legacy.
- Evaluated `DefineConstants` (`-getProperty`) equal the ticket's table in both configurations, plus the SDK's own `DEBUG`/`RELEASE` (framework symbols are added later, at build time). No code in these projects tests `RELEASE` or `NET*` (grep).
- Engine check, `dotnet run --project tools/net-spike/EngineCheck -c Debug` and `-c Release`: **pass** in both. Logs: `tickets/.logs/build-core-sdk-convert.enginecheck-{Debug,Release}.log`.

```
== embedded resource names           (all ten assemblies equal the expected lists)
== System.Platform string table
Strings.Get("Results") => {0} Rows Copied, {1} Rows Failed
== engine
Engine started
  Alphora.Dataphor.DAE.Schema.SystemCatalog.d4: 17 batches run, 3 skipped (need the Server assembly)
    skipped line 543: Runtime error: Registered class "System.CatalogDeviceOperator" not found.
    skipped line 1131: Registered class "System.SystemCreateRoleNode" not found.
    skipped line 1133: Registered class "System.SystemGrantRightToRoleNode" not found.
  Alphora.Dataphor.DAE.Debug.Debug.d4: 21 batches run, 0 skipped (need the Server assembly)
  1 + 1  =>  2
  'Hello' + ' ' + 'D4'  =>  Hello D4
  operators in the catalog: 1060
  scalar types in the catalog: 23
  Sum(X from table { row { 1 X }, row { 2 X }, row { 3 X } })  =>  6
  SyntaxException 103100: Syntax error near "".
  ParserException 109117: ";" is not a valid identifier.
Engine stopped and disposed
Configuration: Debug | Release
Runtime: .NET 10.0.15
Operators: 1060, scalar types: 23
RESULT: pass
```

- **Reproductions (Debug, final check program, original `ThreadUtility.cs` and `Strings.cs` put back from `HEAD` temporarily):** exit 1 with two failures. `System.Platform string table: MissingManifestResourceException: Could not find the resource "Alphora.Dataphor.Libraries.System.Integration.Strings.resources" among the resources "Alphora.Dataphor.Libraries.System.Platform.Strings.resources"…` and `engine: NullReferenceException`. With `ENGINECHECK_TRACE=1` the K4 origin is `ThreadUtility.SetThreadName` ← `ServerProcess.BeginCall` ← `ServerProcess.BeginTransaction` ← `Engine.RegisterCoreSystemObjects` ← … ← `Engine.Start`, i.e. the first process call. Logs: `…enginecheck-debug-before.log`, `…enginecheck-debug-before-trace.log` (the trace log was taken with an earlier draft of the check; the steps up to `Engine.Start` were the same).

## Warning baseline (unique warnings; identical in Debug and Release; 61 total)

| Code | Count | Where |
|---|---|---|
| CA2022 (inexact `Stream.Read`) | 16 | Base 4 (`StreamUtility`, `TemporaryStream`), DAE 11 (`ScalarValue`, `RowValue`, `ListValue`, `BinaryNodes`, `Conveyor`), System.Platform 1 |
| SYSLIB0021 (`AesManaged`) | 12 | DAE 8 (`Schema/Security.cs`), NativeCLI 4 (`SecurityUtility.cs`) |
| SYSLIB0014 (`WebRequest`) | 9 | System.Internet (`FTPNodes.cs`, `InternetNodes.cs`) |
| CA2200 (rethrow loses stack) | 8 | DAE 7, Client 1 |
| SYSLIB0051 (formatter serialization) | 5 | Base 2 (`DataphorException`), NativeCLI 2, DAE 1 |
| CS0672 (overrides obsolete `GetObjectData`) | 3 | Base, NativeCLI, DAE |
| CA1416 (Windows-only API) | 3 | System.Platform (`ProcessStartInfo.Password/Domain/LoadUserProfile`) |
| CS0168 (unused variable) | 2 | Common (`ServiceClient.cs`) |
| CS0162 (unreachable code) | 1 | Base (`BOP/Dilx.cs`) |
| CS0618 (obsolete) | 1 | DAE (`SqlConnection` in `Engine.Dispose`) |
| SYSLIB0023 (`RNGCryptoServiceProvider`) | 1 | DAE (`Schema/Security.cs`) |

Far fewer `SYSLIB0051` than the ticket expected: most exception classes derive from `DataphorException`, and only its own serialization constructor and `GetObjectData` call the obsolete base members. No NuGet audit (`NU190x`) warnings.

## For the reviewer: gaps and things worth a second look

- **The engine check is narrower than the ticket's script** (deviation 2): the counts (1,060 operators, 23 scalar types) come from the in-memory catalog with three system batches skipped, not from D4 catalog tables, and they are not comparable to the spike's full-server 1,080. Whether batch-skipping is an acceptable throwaway check is a judgement call; the alternative is to wait for `build-server-project`.
- **System.Data.SqlClient in the engine** (deviation 1) is a dependency the ticket said not to add. Reviewer may prefer deleting the `ClearAllPools` call; I judged it a behaviour change (SQL Server pooled connections would survive an engine stop) that belongs with `device-mssql-port`.
- **CA2022 is a .NET behaviour difference, not noise:** these call sites assume `Stream.Read` fills the buffer. Today they read the engine's own streams and files, so nothing fails; if any of them is handed a stream that returns partial reads (network, compression), values would be truncated. I did not file it; the reviewer may want it recorded as a `NOTE:` tripwire or a WS2 item.
- `SyntaxException 103100: Syntax error near "".` — the empty token in the outer message is how the parser reports an error at end of input; it shows the resource was found and I did not investigate the wording.
- No new automated test: there is no harness until `test-harness-inprocess`; the engine check is this ticket's characterization, and it is deleted with the spike. The ticket asked for exactly this.
- Dropped legacy items with no build effect: DAE's `None` items (`app.config`, `Schema\SQLDeviceMappings.d4`) and `Content` items (BNF texts), SQLDevice's `UnsupportedOperators.txt` content item, NativeCLI's empty `Folder Include="Thrift\"`, `app.config`/`packages.config` `None` items, bootstrapper packages, framework references.
- `#if !SILVERLIGHT` left in `ThreadUtility.SetThreadName` (out of scope).
- The constants comment in each project says "Set outright; see Directory.Build.props"; the SDK still appends `DEBUG`/`RELEASE` and framework symbols, as `Directory.Build.props` explains.

## How to validate

- `dotnet build Dataphor.slnx` and `dotnet build Dataphor.slnx -c Release` → 0 errors, 61 warnings; output only under `artifacts/bin/<project file name>/<debug|release>/`.
- `dotnet run --project tools/net-spike/EngineCheck -c Debug` and `-c Release` → `RESULT: pass`, exit 0.
- `git status --porcelain --ignored` → no `bin/` or `obj/` under source folders.
- K4 regression: revert `ThreadUtility.cs` locally, run the Debug check → `engine: NullReferenceException`; with `ENGINECHECK_TRACE=1` the first frame is `ThreadUtility.SetThreadName`.
- Endpoint configuration names: by inspection, `ServiceClient(string)` throws before any channel factory is created; `ClientServer.Open()` throws only when `ClientConfigurationName` is non-empty (`String.IsNullOrEmpty` check unchanged); no server exists to connect to until WS5.
