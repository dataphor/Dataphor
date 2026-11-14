description: The engine, the client protocol libraries and four system libraries now build on .NET 10 from a clean checkout with one command, using modern project files and shared build settings; two runtime defects the conversion exposed were fixed, and a throwaway engine check shows the result runs.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: Directory.Build.props, Directory.Packages.props, Dataphor.slnx, src/engine/Base/Base.csproj, src/engine/Base/ThreadUtility.cs, src/engine/Base/StreamUtility.cs, src/engine/Base.Windows/Base.Windows.csproj, src/engine/DAE/DAE.csproj, src/engine/DAE/Runtime.Instructions/SecurityNodes.cs, src/protocol/Common/Common.csproj, src/protocol/Common/WCF/ServiceClient.cs, src/protocol/NativeCLI/DAE.NativeCLI.csproj, src/protocol/Client/Client.csproj, src/protocol/Client/Client/ClientServer.cs, libraries/SQLDevice/Source/DAE.Device.SQL.csproj, libraries/System.Platform/Source/System.Platform.csproj, libraries/System.Platform/Source/Strings.cs, libraries/System.Internet/Source/System.Internet.csproj, libraries/System.Integration/Source/System.Integration.csproj, tools/net-spike/EngineCheck/EngineCheck.csproj, tools/net-spike/EngineCheck/Program.cs, AGENTS.md, README.md, docs/architecture.md, docs/migration-notes.md, docs/modernization-plan.md, tickets/backlog/feat-ws7-devices-and-libraries.md, tickets/plan/7-build-server-project.md
----
# Complete: convert the core projects to SDK-style projects

This is the first half of the work item `build-core-sdk-projects` (plan WS1). The second half is the ticket `build-core-sdk-projects` (in `implement/`). It repairs the spike, re-runs the coverage scripts and rewrites `AGENTS.md` "Build and test". As planned, `python tools/net-spike/run.py` stays broken until that ticket lands.

## What landed

Implemented in `ticket(implement): build-core-sdk-convert`:

- **Root build files.** `Directory.Build.props` sets:
  - net10.0, `LangVersion latest`, nullable and implicit usings off;
  - explicit item lists, and legacy resource naming through `EmbeddedResourceUseDependentUponConvention=false`;
  - `GenerateAssemblyInfo=false`, and `Deterministic=false` with a `NOTE:` for `build-version-and-signing`;
  - build output under `artifacts/` and the `System.Globalization.UseNls` host option;
  - warnings left visible, with no `NoWarn`.

  `Directory.Packages.props` holds the central package versions: WCF client packages 10.1.0, Newtonsoft.Json 13.0.4 and System.Data.SqlClient 4.9.1. `Dataphor.slnx` has `engine`, `protocol` and `libraries` folders. The legacy `Dataphor.sln` is kept.
- **Ten SDK-style project files.** Each keeps its assembly name, root namespace and project references (SQLDevice drops Server and Connection), every `Compile` and `EmbeddedResource` item (order, paths, `DependentUpon`), and `AllowUnsafeBlocks`. Both configurations use the constant set 3.1 shipped: the legacy Release set minus `SIGNASSEMBLIES` and `DISABLE_PERFORMANCE_COUNTERS`. Debug adds `DEBUG;TRACE`. The decision is recorded in `DAE.csproj` and plan Appendix B. Both `packages.config` files are deleted.
- **Source fixes.**
  - K4: `ThreadUtility.SetThreadName` now assigns `Thread.Name`. It used to reflect on the private field `m_Name`, which does not exist on .NET 10, so the first process call of a Debug engine threw `NullReferenceException`.
  - System.Platform's `Strings.BaseName` now names its own resource. It named System.Integration's, so `Strings.Get` threw `MissingManifestResourceException`.
  - The constructors that take an endpoint configuration name, and the alias `ClientConfigurationName` path, throw `NotSupportedException` with a message that says what to do instead.
  - A dead remoting `using` is removed from DAE's `SecurityNodes.cs`.
- **DAE references System.Data.SqlClient** (not in the plan). `Engine.Dispose` (`src/engine/DAE/Server/Engine.cs`) calls `SqlConnection.ClearAllPools()`, added in 2019 to release pooled SQL Server connections on stop. The call is kept, with a `NOTE:` in `DAE.csproj`. An arm on `device-mssql-port` (in `feat-ws7-devices-and-libraries`) moves it when SQL Server access switches to Microsoft.Data.SqlClient.
- **Engine check** `tools/net-spike/EngineCheck/` (throwaway, not in the solution). It:
  - compares all ten assemblies' manifest resource names with the legacy names;
  - calls System.Platform's string table;
  - starts a bare in-process engine and runs `SystemCatalog.d4` and `Debug.d4` batch by batch, skipping the 3 batches that name classes only the Server assembly registers;
  - evaluates D4, and checks that a syntax error carries the parser's resource message.

  A bare `Engine` is the client-side catalog cache and cannot run the full system catalog. That is why batches are skipped and the counts come from the in-memory catalog (1,060 operators, 23 scalar types). They cannot be compared with the spike's full-server 1,080.
- **Docs and board.**
  - Migration notes: Newtonsoft.Json 13, endpoint configuration names, the alias setting.
  - Plan Appendix B and the WS1 design note.
  - The interim `AGENTS.md` paragraph.
  - Updates to `feat-ws7-devices-and-libraries` and `build-server-project`.

Warning baseline: 61 unique warnings, the same in Debug and Release.

| Code | Count |
|---|---|
| CA2022 | 16 |
| SYSLIB0021 | 12 |
| SYSLIB0014 | 9 |
| CA2200 | 8 |
| SYSLIB0051 | 5 |
| CS0672 | 3 |
| CA1416 | 3 |
| CS0168 | 2 |
| CS0162 | 1 |
| CS0618 | 1 |
| SYSLIB0023 | 1 |

## Review findings

I read the full implement diff first, then the handoff.

### Checked, no defect found

- **Project-file fidelity.** A script parsed all ten legacy files (from the parent commit) and the new ones.
  - The `Compile` and `EmbeddedResource` lists are identical in order, path and `DependentUpon`. Counts (compile/resource): Base 35/3, Base.Windows 3/0, DAE 241/22, Common 13/0, NativeCLI 11/0, Client 24/0, SQLDevice 8/1, System.Platform 4/1, System.Internet 7/0, System.Integration 4/1.
  - Project references match, minus SQLDevice's two, as planned.
  - The only extra item metadata in legacy is `LastGenOutput` on two DAE `.resx` files. It is a hint for the resource designer and has no build effect.
  - Every other legacy property that had a value is false, empty or the SDK default: `CheckForOverflowUnderflow`, `NoStdLib`, `RemoveIntegerChecks`, `PlatformTarget AnyCPU`, `DelaySign`, `RegisterForComInterop`, `TreatWarningsAsErrors`, and the empty `NoWarn` and key-file properties. So the conversion dropped nothing that changes arithmetic, platform or signing.
- **Constants.** I ran `dotnet msbuild -getProperty:DefineConstants` for Base, DAE, SQLDevice and Common in both configurations. Each gives the decided set plus the SDK's own `DEBUG`/`RELEASE`. The SDK's default `TRACE` does not leak into Release builds where legacy lacked it.
- **Builds.**
  - `dotnet build Dataphor.slnx` and `-c Release`: 0 errors and 61 warnings each, with the counts by code shown above.
  - A clean Debug build into an empty scratch folder (`-p:ArtifactsPath=<scratch>`): 0 errors, and all ten output folders were created.
  - `git status --porcelain --ignored` before and after both builds and both engine-check runs: no change. The only build output is `artifacts/`.
- **Engine check.**
  - `dotnet run --project tools/net-spike/EngineCheck -c Debug` and `-c Release`: `RESULT: pass` in both, with the same output as the handoff.
  - The program skips a batch only when its error is "Registered class … not found"; any other batch error fails the check.
  - The missing-resource text it checks for matches `DataphorException.MessageNotFound` and `ManifestNotFound`.
  - This is acceptable for a throwaway check. Full verification comes from the spike's coverage re-run in `build-core-sdk-projects` and from `build-server-project`.
- **Endpoint configuration names.**
  - `ServiceClient(string)` throws before it creates a channel factory. `DataphorServiceClient<T>(string)` and the three-argument `NativeCLIClient` both call it.
  - `ClientServer.Open()` throws only when the setting is non-empty. The alias editor's label is "Config Name" (`EditAliasForm.cs:254`), as the message says.
  - `ServerConnection` uses the two-argument `ClientServer` constructor only when the alias has the setting. Clearing it therefore takes the host/instance/port path that the message points to.
- **K4 and the string table.** Both fixes are the ones the plan specified. The implementer's before-change logs show the two failures and trace K4 to `ThreadUtility.SetThreadName`. I did not re-run them: the K4 fix is a one-line assignment, and the Debug engine check now runs that code on every process call.
- **System.Data.SqlClient in the engine.** I agree with keeping the call: deleting it would change what a SQL Server-backed server does on stop. The cost is that every assembly that references DAE, including the Windows client, now depends on the package. On .NET Framework the type was part of the framework, so the only new thing loaded at run time is that one assembly. It is already tracked as an arm on `device-mssql-port`, so no new ticket.
- **Source hygiene.** Each edited `.cs` file has the same byte-order-mark, CRLF and final-newline state as in the parent commit. The project files keep their byte-order mark and are all CRLF. Comments explain reasons rather than restating the code. `Program.cs` is 310 lines and throwaway.

### Found and fixed in this pass (minor)

- `README.md` "Status" still said there is no one-command build. It now says `dotnet build Dataphor.slnx` builds the converted core, and the old solution is still needed for the rest.
- `docs/architecture.md` "Third-party code" listed Newtonsoft.Json 9.0.1. It now says 13.0.4 for the converted projects (from the central versions file) and 9.0.1 for the unconverted server project.
- `docs/migration-notes.md` "Building from source" has a new **done** row for custom extensions that reference these projects by path:
  - output moved from the shared `bin` folder and the library folders to `artifacts/bin/<project file name>/<config>/`;
  - projects that reference them must be SDK-style `net10.0`;
  - a Debug engine no longer defines `ACCUMULATOR` or `USEVISIT`.

### Tripwires recorded

- **CA2022** flags 16 call sites that assume a single `Stream.Read` fills the buffer. For the memory, file and engine streams these sites read today, .NET 10 behaves the same as .NET Framework, so this is a conditional concern. It is parked as a `NOTE:` on `StreamUtility.CopyStream` (`src/engine/Base/StreamUtility.cs`) that names the other flagged sites. The warning stays visible at each site.

### Noted, no action

- The legacy `Dataphor.sln` can no longer build. Its net452 projects now reference SDK-style `net10.0` projects, and legacy-format projects also import the root `Directory.Build.props`. This is expected: nothing builds it, and `build-remove-legacy-build` deletes it.
- Left as planned: `#if !SILVERLIGHT` in `ThreadUtility.SetThreadName`, and System.Platform's `Strings` class in the `…System.Integration` namespace (a public type name).
- `Syntax error near "".` for a statement that ends early is the parser's existing wording at end of input. It is not caused by the conversion.

### Tests

No automated test was added or cut. There is no in-process harness until `test-harness-inprocess`. As the plan asked, the engine check is this ticket's characterization test, and it is deleted with the spike. The review's fixes are documentation only.

### Pre-existing failures

None met.
