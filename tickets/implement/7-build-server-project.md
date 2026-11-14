description: Build the full Dataphor server, its embedded SQLite catalog store and the four libraries that depend on the server as modern .NET 10 projects, leaving the network-hosting classes out for now, so that a server can be started inside a test process from the real build output.
prereq: server-remove-remoting-and-dtc
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: src/engine/Server/Server.csproj, src/engine/Server/Properties/AssemblyInfo.cs, src/engine/Store.SQLite/DAE.SQLite.csproj, src/engine/Store.SQLite/SQLiteConnection.cs, src/engine/Store.SQLite/Properties/AssemblyInfo.cs, libraries/Connection/Source/DAE.Connection.csproj, libraries/Connection/Source/Register.cs, libraries/MSSQLDevice/Source/DAEDeviceMSSQL.csproj, libraries/SimpleDevice/Source/DAE.Device.Simple.csproj, libraries/Frontend/Source/Frontend.Server.csproj, libraries/Frontend/Source/Device/FrontendDevice.cs, external/System.Data.SQLite.DLL, Directory.Packages.props, Dataphor.slnx, tools/net-spike/run.py, tools/net-spike/stubs/Server.cs, tools/net-spike/Smoke/Smoke.csproj.txt, tools/net-spike/Cov/Cov.csproj.txt, tools/net-spike/EngineCheck/EngineCheck.csproj, tools/net-spike/EngineCheck/Program.cs, tools/net-spike/README.md, AGENTS.md, docs/migration-notes.md, docs/modernization-plan.md
difficulty: hard
----
# Server project on .NET 10

## Goal

The in-process test harness (`test-harness-inprocess`) has to start a real `Server`: the persistent catalog, library registration and the SQLite catalog store live in the server assembly (`AlphoraDataphorServer`) or depend on it. This ticket converts the server and the five projects that cannot compile without it to SDK-style `net10.0` projects in `Dataphor.slnx`, leaves the WCF service and host classes out of compilation (CoreWCF hosting is WS5), applies the Appendix A compile patches that sit in these projects, and points the spike's smoke and coverage programs at the real build output.

Done means: `dotnet build Dataphor.slnx` builds them in Debug and Release; the spike's smoke program starts a Debug `Server` on a SQLite catalog in a temporary instance directory and runs D4 from the real `Server` and `Store.SQLite` projects; coverage gives the same result as before this ticket.

The prerequisite `server-remove-remoting-and-dtc` has already deleted the .NET Remoting lifetime code (K6), the COM+ distributed-transaction class (K11), the dead `using System.Runtime.Remoting.Messaging` in `SecurityNodes.cs`, and trimmed `tools/net-spike/stubs/Server.cs` to the `System.ServiceModel*` stand-ins, and added the `DistributedTransactions` coverage script (coverage expected `pass=63 fail=14` from then on). If any of that is not in the tree, stop and say so; do not redo it here.

Follow the conversion rules in AGENTS.md "Build and test → Converting another project" exactly (explicit `Compile`/`EmbeddedResource` lists in legacy order, `DependentUpon` copied, constants, no `OutputPath`, versionless `PackageReference`s with versions in `Directory.Packages.props`, `AssemblyInfo.cs` attribute cleanup, no warning suppression, add to `Dataphor.slnx`). Look at a project `build-core-sdk-convert` converted (for example `src/engine/DAE/DAE.csproj` and `libraries/SQLDevice/Source/DAE.Device.SQL.csproj`) and copy its shape.

## Projects

All `net10.0` (none of them uses WinForms; `Base.Windows` is `net10.0` too). Constants per the AGENTS.md rule (legacy Release set minus `SIGNASSEMBLIES`/`DISABLE_PERFORMANCE_COUNTERS`, same in both configurations, plus `DEBUG;TRACE` in Debug) — read each legacy file's Release group; the values below are what the planner saw.

| Project file | Assembly / root namespace | Facts |
|---|---|---|
| `src/engine/Server/Server.csproj` | `AlphoraDataphorServer` / `Alphora.Dataphor.DAE` | Project references Base, Base.Windows, Common, NativeCLI, DAE. Packages: `System.ServiceModel.Primitives` (and `.Http`/`.NetTcp` only if a compiled file needs them — the contracts in `Contracts/` use `ServiceContract`/`OperationContract`/`DataContract`), `Newtonsoft.Json` (`REST/RESTMarshal.cs`), `System.Data.SqlClient` (`Connection/MSSQL*.cs`); all three already have versions in `Directory.Packages.props`. Drop the legacy framework references (`System.EnterpriseServices`, `System.Runtime.Remoting`, `System.ServiceModel.Web`, `System.configuration` — used only by excluded files), the `clientaccesspolicy.xml` and `Contracts\Thrift_NativeCLI.idl` content items and `packages.config`. Legacy Release constants `TRACE;SIGNASSEMBLIES` → `TRACE` |
| `src/engine/Store.SQLite/DAE.SQLite.csproj` | `Alphora.Dataphor.DAE.SQLite` / `Alphora.Dataphor.DAE` | References Base, DAE, Server. The one `EmbeddedResource`, `SystemStoreCatalog.sql`, must come out as `Alphora.Dataphor.DAE.SystemStoreCatalog.sql` (loaded by `src/engine/DAE/Device.Catalog/CatalogStore.cs:138`). Replace the `HintPath` reference to `external/System.Data.SQLite.DLL` (1.0.60, x86) with package `System.Data.SQLite.Core` 1.0.119.0 (what the spike ran on) and delete `external/System.Data.SQLite.DLL` (nothing else references it; `Setup/Files.wxs:244` names a `System.Data.SQLite.DLL` under `..\Deploy\`, a legacy build-output copy that the WiX installer replacement in WS9 owns — leave it; re-grep `*.csproj`, `*.wxs`, `*.props`, `*.targets`, `*.py` before deleting, and update the `external/` row in AGENTS.md's layout table and any `external/` listing in the plan's layout section). Legacy Release constants `SIGNASSEMBLIES` → none |
| `libraries/Connection/Source/DAE.Connection.csproj` | `Alphora.Dataphor.DAE.Connection` | Compiles only `ConnectionStringBuilder.cs`, `OLEDBConnection.cs`, `Operators.cs`, `Register.cs`, `Properties/AssemblyInfo.cs`; the other `.cs` files in the folder are strays (`device-connection-cleanup` deletes them) — keep them uncompiled. Package `System.Data.OleDb` (current stable 10.0.x; Windows-only, so expect `CA1416` warnings — leave them visible). Remove the project reference to the deleted SQL CE store project (K19) |
| `libraries/MSSQLDevice/Source/DAEDeviceMSSQL.csproj` | `AlphoraMSSQLDevice` / empty root namespace | References SQLDevice (already converted), Connection, Server, DAE, Base. `SystemCatalog.d4` and `SystemCatalog.sql` are loaded unqualified (`MSSQLDevice.cs` ~192, ~288): with an empty root namespace their manifest names must be exactly `SystemCatalog.d4` and `SystemCatalog.sql`. Set `<RootNamespace></RootNamespace>` explicitly so the SDK does not default it to the project name |
| `libraries/SimpleDevice/Source/DAE.Device.Simple.csproj` | `Alphora.Dataphor.DAE.Device.Simple` / `DAE.Device.Simple` | `SimpleException.resx` has `DependentUpon SimpleException.cs`, so its manifest name is the namespace-qualified name of the first class in `SimpleException.cs`: `Alphora.Dataphor.DAE.Device.Simple.SimpleDeviceException.resources` (what `SimpleException.cs:32` loads). The spike's wrapper gets this one wrong; the real project must get it right |
| `libraries/Frontend/Source/Frontend.Server.csproj` | `Alphora.Dataphor.Frontend.Server` | Constant `USEINCLUDETAG` in both configurations. `Device/FrontendDeviceException.resx` has `DependentUpon`; `ServerException.resx` and `Strings.resx` — copy whatever metadata the legacy items carry. The `System.Web` leftovers stay as they are (`library-frontend-web-leftovers`); if they do not compile on .NET 10, stop and report rather than patching around them |

SQLDevice is already converted and does not reference the server; nothing to do there.

Each `Properties/AssemblyInfo.cs` gets the attribute cleanup from AGENTS.md (keep `AssemblyTitle`, `AssemblyDescription`, `DAERegister`, `ComVisible`, `Guid`; preserve encoding and line endings).

## Source patches (Appendix A, compile-only)

- `src/engine/Store.SQLite/SQLiteConnection.cs` (~60, ~89): `SQLiteException.ErrorCode` → `ResultCode`. Only that half of the spike's SQLite patch; the "begin immediate" half (K10) is `store-sqlite-concurrency`'s and is **not** applied here.
- `libraries/Frontend/Source/Device/FrontendDevice.cs`: `FrontendDeviceSession : MemoryDeviceSession, IStreamProvider` collides with `System.Xml.IStreamProvider`; qualify it as `Alphora.Dataphor.DAE.Streams.IStreamProvider` (or add a `using` alias — whichever reads better in the file; same meaning).
- K19, `libraries/Connection/Source/Register.cs`: delete the `SQLCEConnection` registration line (and anything else in the file that names `SQLCEConnection`). Other SQL CE mentions (server default store class name, instance forms, docs) stay for `store-remove-sqlce-references`.
- Do **not** carry the K1 emitter patch (`ServerCatalogDeviceSession.cs`, `port-catalog-emitter-race`) into the repository.

Fix anything else the real compile turns up that the spike's stubs hid, if the fix is local and obvious (list each one in the handoff). A fix that changes a compatibility surface (AGENTS.md "Rules that are easy to break") stops the ticket and goes to `blocked/`.

## WCF service and host classes: left out of compilation

These six files use WCF hosting types that .NET 10 does not have (`ServiceHost`, `ServiceBehaviorAttribute`, `System.ServiceModel.Web`); CoreWCF hosting is `protocol-corewcf-host` (`feat-ws5-protocol-and-hosting`):

- `src/engine/Server/Service/CustomServiceHost.cs`
- `src/engine/Server/Service/DataphorService.cs`
- `src/engine/Server/Service/DataphorServiceHost.cs`
- `src/engine/Server/Service/ListenerService.cs`
- `src/engine/Server/Service/ListenerServiceHost.cs`
- `src/engine/Server/NativeCLI/NativeCLIService.cs`

No other file in the server project references them (verified during planning). Omit them from the `Compile` list, each with an XML comment in place naming `protocol-corewcf-host`, so the list still reads in legacy order. Keep `Service/ConnectionManager.cs` and `Service/HandleManager.cs` compiled (no WCF types; `port-abandoned-session-cleanup` needs `ConnectionManager`). Keep everything in `Contracts/` compiled and unchanged (compatibility surface; the client copies in `src/protocol/Client/Contracts` must stay in step). The plan's WS5 row says "the four files in `Server/Service`": correct it to these six.

`DAE.Client`, `DAEServer` and `DAE.Service` reference `DataphorServiceHost` (`src/client/DAE.Client/ServerConnection.cs:49`, `src/hosts/DAEServer/Program.cs:32`, `src/hosts/DAEService/Service.cs:122`). They stay unconverted; the spike wraps `DAE.Client` and will now fail to compile it against the real server. Handle that in the spike (see below), not by touching those files.

## The spike after this ticket

Since `build-core-sdk-projects`, `run.py` builds every project in `projects.txt` that is SDK-style from its real project file, `convert.py` points wrappers of still-legacy projects at converted ones by absolute path, and `find_dll` asks MSBuild for a converted project's Debug `TargetPath`, so `COVERAGE_LIBS` needs no change. `run.py` also stops with a message while a `PATCHES` entry names a file a converted project compiles, or `STUBS` names a converted project. So:

- Remove from `run.py`: the whole SQLite `PATCHES` entry (the `ResultCode` half lands here; the "begin immediate" half is deliberately dropped, owned by `store-sqlite-concurrency`), the K1 `ServerCatalogDeviceSession.cs` entry (owned by `port-catalog-emitter-race`), the Frontend entry and the `Register.cs` (K19) entry; `STUBS` becomes empty. Delete `tools/net-spike/stubs/Server.cs` (and the `stubs/` folder and the `STUBS` copying code if nothing is left to stub — your call; leave the mechanism if a remaining wrapped project could need it).
- `Smoke/Smoke.csproj.txt` and `Cov/Cov.csproj.txt` reference `..\Server\Server.csproj` and `..\DAE.SQLite\DAE.SQLite.csproj`, relative to the work directory's wrappers. **Decision:** keep the templates naming projects that way, and have `run.py`, when it writes each template, replace every `..\<Name>\<Name>.csproj` whose `<Name>` is in `CONVERTED` with the converted project's absolute path (the same mapping `convert.py` already does for wrappers). This stays correct as more projects convert, without hard-coded repository paths in the templates.
- Wrapped projects that now fail because the real server lacks the WCF host classes (at least `DAE.Client`, `DAEServer`, `DAE.Service`, and anything that depends on them — the UI and IDE wrappers): the spike reports their build errors and carries on (`build()` does not stop the run). Neither the smoke nor the coverage program needs them; check that `COVERAGE_LIBS`' `TestFramework.Coverage.Base` (`DAE.Diagnostics`) still builds. Record in the handoff which wrappers now fail and why; update the expected-results text in AGENTS.md "Build and test" and `tools/net-spike/README.md` to match (they currently say "engine/server/libraries 0 errors" and list 10 converted projects and "the other 20 of its 30").
- Update AGENTS.md's "Converted so far" list (add Server, Store.SQLite, Connection, MSSQLDevice, SimpleDevice, Frontend) and the layout table's `external/` row (System.Data.SQLite gone).
- Smoke and Cov run outside the repository, so they do not inherit `Directory.Build.props`; NLS stays off there as before (the 62/14 → 63/14 baseline was measured that way). Do not change that here.

## Engine check

Extend `tools/net-spike/EngineCheck` so its resource-name step covers the six new assemblies: add `ProjectReference`s for them to `EngineCheck.csproj` and entries to `ExpectedResources` in `Program.cs`, with the expected names taken from the **legacy** rules (list each legacy `EmbeddedResource` and compute its legacy manifest name: `RootNamespace` + folder path + file name for non-`.resx` files; for a `.resx` with `DependentUpon`, the namespace-qualified first class in the `.cs` file plus `.resources`; without `DependentUpon`, `RootNamespace` + folder path + base name + `.resources`). At minimum: `Alphora.Dataphor.DAE.SystemStoreCatalog.sql`, `SystemCatalog.d4`, `SystemCatalog.sql`, `Alphora.Dataphor.DAE.Device.Simple.SimpleDeviceException.resources`, the three Frontend resources, and the server's resources (the planner found no `EmbeddedResource` in the legacy server project; if so its entry is an empty array). A name that differs is a bug to fix in the project file, never in the expectation. If adding the server makes the bare-engine D4 step behave differently, report it.

## K4 (thread names in Debug)

Fixed already in Base by `build-core-sdk-convert`. The smoke run in this ticket, built in Debug against the real server (which defines `DEBUG`, so `SetThreadName` runs at server start), is the first full-server run that exercises it. If it fails there, fix it here and say so in the handoff.

## Migration notes and plan

`docs/migration-notes.md`:

- D4 section: `Connection.SQLCEConnection` row → **done**.
- Custom C# extensions: **done** — "The server assembly (`AlphoraDataphorServer`) does not contain the WCF service and host classes (`DataphorService`, `DataphorServiceHost`, `ListenerService`, `ListenerServiceHost`, `CustomServiceHost`, `NativeCLIService`) until CoreWCF hosting lands; an in-process `Server` is unaffected." What to do: "Host the server in-process, or wait for the CoreWCF hosting release". (Mark it so `protocol-corewcf-host` can turn it into the final wording.)
- Platform and deployment, **done**: the SQLite catalog store uses `System.Data.SQLite.Core` 1.0.119 (was 1.0.60) with its native interop shipped next to the server, any architecture. What to do: "Nothing, unless a deployment copied `System.Data.SQLite.dll` by hand: remove that step".

`docs/modernization-plan.md`: K19 → fixed (owner this ticket); the WS1 `build-server-project` row → **Done**, naming the split (`server-remove-remoting-and-dtc` then this); the WS5 row's file count (six files, see above); the Appendix A patch table rows for `SQLiteConnection.cs` (ResultCode half landed, BEGIN IMMEDIATE half pending `store-sqlite-concurrency`), `FrontendDevice.cs` and `Register.cs`.

## Edge cases & interactions

- **Resource names** change silently and break only when that exception is raised or that script loaded — verified by the extended engine check (a test of a named contract, worth keeping).
- **MSSQLDevice root namespace**: an SDK project with no `RootNamespace` defaults it to the project name, which would rename `SystemCatalog.d4` to `DAEDeviceMSSQL.SystemCatalog.d4` — engine check.
- **SQLite native interop**: `System.Data.SQLite.Core` puts `SQLite.Interop.dll` under `runtimes/<rid>/native/` in the consumer's output; the smoke run must load the package's managed DLL and its interop, not a stray copy. Check the smoke work dir's output for any second `System.Data.SQLite.dll` and confirm the loaded version (print `typeof(SQLiteConnection).Assembly` location/version once, by hand, or inspect the deps file) — by inspection, recorded in the handoff.
- **Debug and Release both build** with zero errors: `dotnet build Dataphor.slnx` and `-c Release`. Warning counts: report new warnings by code (expect `CA1416` from OleDb, possibly `SYSLIB`/obsolete ones from `System.Data.SqlClient`); never suppress them.
- **No `System.*` stubs in product code** and no compiled source referencing `System.Runtime.Remoting*` or `System.EnterpriseServices`: grep the files in the six projects' `Compile` lists (not every file on disk — strays exist).
- **Concurrency phase regresses on purpose**: without the K1 emitter patch and K10's immediate transactions, the smoke's concurrent DDL phase (8 threads × 15 cycles) returns to roughly the "as found" error rate in the plan's WS4 table. Record its error count; do not gate on it. The single-session part (server start, expressions, DDL/DML script, clean stop) is the gate.
- **Coverage**: `pass=63 fail=14` with the same failing set as after `server-remove-remoting-and-dtc`. The K1 emitter patch was in place for every earlier coverage run; if dropping it changes a single-session script's result, investigate and report — do not adjust, and never edit an existing script's expected values.
- **Newtonsoft.Json 13** (from 9) changed defaults, for example a default `MaxDepth` of 64 when reading; `RESTMarshal.cs` and NativeCLI serialization are not exercised here. Note it for `test-client-server` in the handoff (no test here).
- **`DAE.Client`'s in-process `DataphorServiceHost` path** is unavailable until WS5; `ui-client-layering` will meet it. Say so in the handoff.
- **Project file name collision**: output is keyed by project file name; none of the six collides with an existing converted project (`DAE.Connection.csproj` is distinct from the duplicated `DAE.Connection.ODBC.csproj`) — check once.

## Validation (foreground, work dir outside the repo)

```
dotnet build Dataphor.slnx
dotnet build Dataphor.slnx -c Release
dotnet run --project tools/net-spike/EngineCheck -c Debug
python tools/net-spike/run.py <work-dir> --smoke --coverage
```

Expected: both builds 0 errors; engine check `RESULT: pass`; smoke starts the Debug server on SQLite and finishes the single-session part (1,080 operators, 23 scalar types, as today), concurrency error count recorded; coverage `pass=63 fail=14`.

## TODO

- Convert Server (WCF files excluded with comments), Store.SQLite (package `System.Data.SQLite.Core`), Connection (K19), MSSQLDevice (empty root namespace), SimpleDevice, Frontend; `AssemblyInfo.cs` cleanups; package versions in `Directory.Packages.props`; add all six to `Dataphor.slnx`
- Apply `ResultCode` and `IStreamProvider` patches; K19 in `Register.cs`
- Delete `external/System.Data.SQLite.DLL` after re-grepping for references
- Build Debug and Release; fix what the real compile reveals (local fixes only, listed in the handoff)
- Extend EngineCheck with the six assemblies' legacy resource names; run it
- Spike: drop the four `PATCHES` entries and the server stub; map template project references to converted projects in `run.py`; run smoke (Debug) and coverage; record the concurrency error count and which wrappers now fail
- AGENTS.md (converted list, expected spike results, `external/` row), `tools/net-spike/README.md`, migration notes, plan rows
