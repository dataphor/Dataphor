description: Build the full Dataphor server, its embedded catalog store and the device and system libraries that depend on it for .NET 10, removing two old Windows-only mechanisms that no longer exist there, so that a server can be started inside a test process and run D4.
prereq: build-core-sdk-projects
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: src/engine/Server/, src/engine/Server/Server/RemoteServerObject.cs, src/engine/Server/Server/RemoteServer.cs, src/engine/Server/Server/RemoteServerConnection.cs, src/engine/Server/Streams/FileStreamProvider.cs, src/engine/Server/Server/ServerDTCTransaction.cs, src/engine/Server/Service/, src/engine/Server/NativeCLI/NativeCLIService.cs, src/engine/Server/Runtime.Instructions/SecurityNodes.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/IServerDTCTransaction.cs, src/engine/DAE/Server/ServerException.cs, src/engine/DAE/Server/ServerException.resx, src/engine/Base/ThreadUtility.cs, src/engine/Store.SQLite/, src/engine/Store.SQLite/SQLiteConnection.cs, libraries/Connection/Source/DAE.Connection.csproj, libraries/Connection/Source/Register.cs, libraries/MSSQLDevice/Source/DAEDeviceMSSQL.csproj, libraries/SimpleDevice/Source/DAE.Device.Simple.csproj, libraries/Frontend/Source/Frontend.Server.csproj, libraries/Frontend/Source/Device/FrontendDevice.cs, external/System.Data.SQLite.DLL, tools/net-spike/stubs/Server.cs, tools/net-spike/run.py, tools/net-spike/Smoke/, tools/net-spike/Cov/, tests/libraries/TestFramework.Coverage.Scripts/Documents/, tests/DAE.Server.Tests/CoverageTest.cs, Dataphor.slnx, docs/migration-notes.md
difficulty: hard
----
# Server project on .NET 10

## Goal

The test harness (`test-harness-inprocess`) has to start a real `Server` in-process: the persistent catalog, library registration and the SQLite catalog store all live in the server assembly (`AlphoraDataphorServer`) or depend on it. WCF hosting cannot wait for WS5, so this ticket compiles the server for .NET 10 with the WCF service and host classes left out of compilation, removes the .NET Remoting lifetime code (K6) and the COM+ distributed-transaction class (K11) outright, and converts the projects that cannot compile without the server. Done means an in-process `Server` starts on a SQLite catalog and runs D4 from the real build output, as the spike's smoke program does today from its out-of-tree wrappers.

**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout from [Repository layout](../../docs/modernization-plan.md#repository-layout) (for example `src/engine/Server` is today's `Dataphor/Server`, `src/engine/Store.SQLite` is `Dataphor/DAE.SQLite`, `libraries/X` is `Libraries/X`, `tests/libraries/X` is `Libraries/TestFramework*`, `external/` is `Dataphor/bin`). Use the plan's old → new table if the move has not happened. Follow the conventions `build-core-sdk-projects` set in `Directory.Build.props` and the "Build and test" section of `AGENTS.md` (explicit item lists, constants, resource names, warning baseline, central packages, artifacts output).

## Scope

In scope:

- SDK-style projects for the server (`src/engine/Server`), the SQLite catalog store (`src/engine/Store.SQLite`) and the libraries that use server types: Connection, MSSQLDevice, SimpleDevice, Frontend (and SQLDevice if `build-core-sdk-projects` found that it needs the server). `build-core-sdk-projects` verified the dependencies: `SQLiteConnection : DotNetConnection`; Connection's compiled `OLEDBConnection.cs` and `Register.cs` use the server's `DotNetConnection` and `MSSQLConnection`; MSSQLDevice uses `MSSQLConnection`; SimpleDevice casts to `Server.Server`; Frontend uses the server's `Schema.LibraryUtility`. Add them to `Dataphor.slnx`.
- The compile-only Appendix A patches whose files are in these projects: the dead `using System.Runtime.Remoting.Messaging` in `src/engine/Server/Runtime.Instructions/SecurityNodes.cs:10`; `SQLiteException.ErrorCode` → `ResultCode` in `src/engine/Store.SQLite/SQLiteConnection.cs` (lines ~60 and ~89); the `IStreamProvider` ambiguity in `libraries/Frontend/Source/Device/FrontendDevice.cs` (`FrontendDeviceSession : MemoryDeviceSession, IStreamProvider` collides with `System.Xml.IStreamProvider`; qualify it as `Alphora.Dataphor.DAE.Streams.IStreamProvider`); and K19, the `SQLCEConnection` registration in `libraries/Connection/Source/Register.cs` plus Connection's project reference to the deleted SQL CE store project.
- K6, K11 as described below (K4 is fixed earlier, by `build-core-sdk-projects`), and excluding the WCF service and host classes from compilation.
- Retargeting the spike's smoke and coverage programs at the real build output.

Out of scope:

- Hosting the WCF services on CoreWCF, and everything else in `Service/` that needs it: `protocol-corewcf-proof`, `protocol-corewcf-host` (`feat-ws5-protocol-and-hosting`). K12 (configuration section name in `DataphorService.cs`) belongs there too.
- What reclaims sessions abandoned by a vanished client once remoting leases are gone: `port-abandoned-session-cleanup` (`feat-ws2-server-port-remainder`).
- K1 (the shared catalog emitter): `port-catalog-emitter-race`. K10's "begin immediate" store transactions (the second half of the spike's SQLite patch): `store-sqlite-concurrency`. K2, K5, K22: `port-engine-runtime-fixes`. Do not carry those spike patches into the repository here.
- Choosing between `System.Data.SQLite` and `Microsoft.Data.Sqlite`: `store-sqlite-concurrency` (plan WS4). Here the store keeps `System.Data.SQLite`, from its NuGet package.
- `Microsoft.Data.SqlClient` and the SQL Server device's connection defaults: `device-mssql-port` (`feat-ws7-devices-and-libraries`). The SQL Server and PostgreSQL catalog stores: `store-mssql-port` (`feat-ws4-store-followups`) and `device-pgsql-port`. Neither is needed in-process.
- `TestFramework.Coverage.Base`'s project (`tests/libraries/TestFramework.Coverage.Base/Source/DAE.Diagnostics.csproj`): `build-runtime-layout`. Every other SQL CE mention (server default store class name, instance forms, documentation): `store-remove-sqlce-references`.
- `DAE.Client`, `DAEServer` and `DAE.Service` also reference `DataphorServiceHost` (`src/client/DAE.Client/ServerConnection.cs:49`, `src/hosts/DAEServer/Program.cs:32`, `src/hosts/DAEService/Service.cs:122`); they stay unconverted (`feat-ws6-windows-client-and-ide`, `feat-ws5-protocol-and-hosting`).

## What the spike stubs, member by member

`tools/net-spike/stubs/Server.cs` declares empty stand-ins so the compiler gets past missing API. Every stubbed member maps to a real site; none of the stubs may be committed into product code.

| Stubbed API | Real sites in `src/engine/Server` | Treatment |
|---|---|---|
| namespace `System.Runtime.Remoting.Messaging` | `Runtime.Instructions/SecurityNodes.cs:10` (unused `using`) | delete the line |
| `System.Runtime.Remoting.Services.ITrackingHandler`, `TrackingServices.RegisterTrackingHandler/UnregisterTrackingHandler` | `Server/RemoteServer.cs`: `using` at 21, class declaration at 42, register at 50, unregister at 94, `DisconnectedObject` 237–248, `DisposeConnection` 250–277, `UnmarshaledObject`/`MarshaledObject` 279–287 | remove (K6) |
| `System.Runtime.Remoting.ObjRef`, `RemotingServices.Disconnect` | `RemoteServer.cs` 19, 279, 284; `Server/RemoteServerObject.cs` 12, 45, 92 | remove (K6) |
| `System.Runtime.Remoting.Lifetime.LeaseState`, `LifetimeServices.LeaseManagerPollTime/LeaseTime`, `ILease`, `ISponsor` | `RemoteServerObject.cs`: static constructor 21–25, `Dispose` lease cancel 38–46, `RemoteServerChildObject.Dispose` 89–92, constant `LeaseManagerPollTimeSeconds`; `Server/RemoteServerConnection.cs`: `InitializeLifetimeService` override and constants `InitialLeaseTimeSeconds`/`RenewOnCallTimeSeconds` 133–153; `Streams/FileStreamProvider.cs`: `FileStreamHeader : Disposable, ISponsor` and `ISponsor.Renewal` 15, 41–45; `RemoteServer.cs:20` | remove (K6) |
| `System.EnterpriseServices.ServicedComponent`, `TransactionOption`, `TransactionAttribute`, `ContextUtil` | `Server/ServerDTCTransaction.cs` (whole file); unused `using` at `RemoteServer.cs:15` | delete the class (K11) |
| namespace `System.ServiceModel.Web` | `Service/ListenerServiceHost.cs` | exclude from compilation |
| `System.ServiceModel.ServiceBehaviorAttribute`, `InstanceContextMode`, `ConcurrencyMode` | `Service/DataphorService.cs`, `Service/ListenerService.cs`, `NativeCLI/NativeCLIService.cs` | exclude from compilation |
| `System.ServiceModel.ServiceHost` (and `StubDescription`) | `Service/CustomServiceHost.cs`, `Service/DataphorServiceHost.cs`, `Service/ListenerServiceHost.cs` | exclude from compilation |

The six excluded files are referenced by no other file in the server project (verified), so leaving them out of the `Compile` list compiles. Mark each excluded entry in the project file with a comment naming `protocol-corewcf-host`. Keep `Service/ConnectionManager.cs` and `Service/HandleManager.cs` compiled: they use no WCF type, and `port-abandoned-session-cleanup` needs `ConnectionManager`. The contracts in `Contracts/` use only `ServiceContract`/`OperationContract`/`DataContract`, which the client packages provide; they stay compiled and unchanged (compatibility surface; the client copies in `src/protocol/Client/Contracts` must stay in step). The plan's WS5 row says "the four files in `Server/Service`"; it is five files there plus `NativeCLI/NativeCLIService.cs`.

## K6: remoting leftovers

`RemoteServerObject` and `RemoteServerChildObject` derive from `MarshalByRefObject` and, in `Dispose`, call `GetLifetimeService()` and `RemotingServices.Disconnect`, which throw `PlatformNotSupportedException` on .NET 10 (and `GetLifetimeService`/`InitializeLifetimeService` are `SYSLIB0010` obsolete). Remove all lease, sponsor and tracking code listed above; `Dispose` keeps raising `Disposed` (`DoDispose`). `RemoteServer.DisconnectedObject` was how a connection abandoned by a remoting client got disposed; with WCF that path was already dead, and nothing in-process uses it. Default for the base class: keep `MarshalByRefObject` (it still exists, has no behaviour on .NET 10, and removing it changes a public type hierarchy); the planner may remove it if nothing depends on it. The public constants and the `ITrackingHandler` members disappear; that is a migration-notes entry for custom extensions.

## K11: distributed transactions

`ServerProcess.InternalBeginTransaction` (`src/engine/DAE/Server/ServerProcess.cs` ~1265–1270) creates the DTC object with `Type.GetType("Alphora.Dataphor.DAE.Server.ServerDTCTransaction,Alphora.Dataphor.DAE.Server")`. No assembly has that name (the server is `AlphoraDataphorServer`), so `Type.GetType` returns null and `Activator.CreateInstance(null)` throws `ArgumentNullException`; `ServerDTCTransaction` also does not implement `IServerDTCTransaction`, so the cast would fail anyway. Every transaction begun with `UseDTC` set has failed this way since the 2009 engine/server split. Other DTC sites in the same file: field ~1112, the `UseDTC` property ~1165–1180, commit ~1326–1330 and rollback ~1376.

The D4 and API surfaces that reach it:

- `update System.Processes set { UseDTC := true } where ID = ProcessID()` → `ServerCatalogDeviceSession` (~2561) → the `ServerProcess.UseDTC` setter.
- `ProcessInfo.UseDTC` passed to `StartProcess` (in-process, NativeCLI and WCF), defaulted from `SessionInfo.DefaultUseDTC` (`src/engine/DAE/SessionInfo.cs` ~305, ~403), which D4 can set with `update System.Sessions set { DefaultUseDTC := true }` (`ServerCatalogDeviceSession` ~2524) and server links can set through the metadata tag `DefaultUseDTC` (`Server/RemoteSessionImplementation.cs` ~118). These set the flag without the setter; the failure happens at the next `BeginTransaction`.

Required: delete `ServerDTCTransaction`, the `_dTCTransaction` code paths, and the DTC paragraph of the comment at `src/engine/DAE/Server/Engine.cs` ~51–56; make both surfaces raise a clear "not supported" error; keep the `UseDTC` and `DefaultUseDTC` properties, the WCF data-contract members and the `System.Processes`/`System.Sessions` catalog columns (compatibility surfaces), so that `false` keeps working everywhere. Raise the error before any state changes (in `InternalBeginTransaction`, before `_transactions.BeginTransaction`), and in the setter check the value before the existing `InTransaction` check if the D4 `update` runs inside a transaction (verify).

**Error code.** An existing code fits: `ServerException.Codes.DTCNotSupported = 106119` (`src/engine/DAE/Server/ServerException.cs:75`; message in `ServerException.resx` `<data name="106119">`: "Microsoft Distributed Transaction Coordinator support is only available on Windows 2000 or later."), raised today by the setter only when the OS major version is below 5. Recommended default: reuse 106119 and reword its message to say distributed transactions are not supported; the code stays stable. If the planner prefers a new code instead: codes are allocated per exception class, prefix from `Error Message Prefix Codes.txt` (`106: DAE.Server`; the file is at `Dataphor/` before WS8 moves it to `docs/reference/`), next number in the class's `Codes` enum (the last one in `ServerException.cs` is 106211), message under the same number in the `.resx`. `IServerDTCTransaction` (`src/engine/DAE/Server/IServerDTCTransaction.cs`, public, implemented by nothing) — default: delete it with the class, with a migration-notes line.

## K4: thread names in Debug builds

Fixed by `build-core-sdk-projects` (the file is in Base). This ticket's Debug smoke run is the first full-server run that exercises the fix; if it still fails there, fix it here and say so in the handoff.

## Projects and their facts

| Project | Assembly / root namespace | Notes |
|---|---|---|
| Server | `AlphoraDataphorServer` / `Alphora.Dataphor.DAE` | References Base, Base.Windows, Common, NativeCLI, DAE. Packages: `System.ServiceModel.*` (contracts), `Newtonsoft.Json` (`REST/RESTMarshal.cs`), `System.Data.SqlClient` (`Connection/MSSQL*.cs`; the spike used 4.9.0; `build-core-sdk-convert` already put 4.9.1 in `Directory.Packages.props` because DAE's `Engine.Dispose` needs it). `System.Configuration` is used only by the excluded `Service/` files, so `System.Configuration.ConfigurationManager` is not needed yet. Drop the Silverlight `clientaccesspolicy.xml` and the Thrift `.idl` content items. Legacy constants `DEBUG;TRACE` / `TRACE;SIGNASSEMBLIES` |
| Store.SQLite | `Alphora.Dataphor.DAE.SQLite` / `Alphora.Dataphor.DAE` | Embedded `SystemStoreCatalog.sql` must be named `Alphora.Dataphor.DAE.SystemStoreCatalog.sql` (loaded by `src/engine/DAE/Device.Catalog/CatalogStore.cs:138`). Legacy references the tracked `System.Data.SQLite.DLL` 1.0.60 (x86); replace with the `System.Data.SQLite.Core` package (spike: 1.0.119, which brings its native interop) and delete `external/System.Data.SQLite.DLL`, which nothing else references |
| Connection | `Alphora.Dataphor.DAE.Connection` | Compiles only `ConnectionStringBuilder.cs`, `OLEDBConnection.cs`, `Operators.cs`, `Register.cs`, `AssemblyInfo.cs`; the other `.cs` files in the folder are strays (`device-connection-cleanup`). Package `System.Data.OleDb` (Windows-only; expect `CA1416`) |
| MSSQLDevice | `AlphoraMSSQLDevice` / empty root namespace | Embedded `SystemCatalog.d4` and `SystemCatalog.sql` are loaded unqualified (`MSSQLDevice.cs` ~192, ~288); keep those exact names. References SQLDevice and Connection |
| SimpleDevice | `Alphora.Dataphor.DAE.Device.Simple` / `DAE.Device.Simple` | `SimpleException.resx` has `DependentUpon SimpleException.cs`, so its resource name is `Alphora.Dataphor.DAE.Device.Simple.SimpleDeviceException` (what `SimpleException.cs:32` loads); the spike gets this one wrong |
| Frontend | `Alphora.Dataphor.Frontend.Server` | Constant `USEINCLUDETAG` in both configurations; `Device/FrontendDeviceException.resx` has `DependentUpon`. The `System.Web` leftovers stay (`library-frontend-web-leftovers`) |

All `net10.0`. Each needs the Release/Debug constant decision `build-core-sdk-projects` made.

## The spike after this ticket

`tools/net-spike/Smoke/Smoke.csproj.txt` and `Cov/Cov.csproj.txt` reference the wrapper projects `..\Server\Server.csproj` and `..\DAE.SQLite\DAE.SQLite.csproj`, and `run.py`'s `find_dll` looks for library assemblies under the wrappers' `bin`. Point both at the real projects and their artifacts output, remove the `Server` stub and the landed patches from `run.py`, and keep wrapping only what is still unconverted (`TestFramework.Coverage.Base`, the UI and host projects). Two consequences to state in the handoff: the real server has neither the K1 emitter fix nor K10's immediate transactions, so the concurrency phase returns to roughly the "as found" error rate of the plan's WS4 table; and the spike's hosts live outside the repository, so they do not inherit `Directory.Build.props` (NLS is off there unless the template sets it — the 62/14 baseline was measured that way; if you turn NLS on in the template, report any change in results).

## What done looks like

- `dotnet build Dataphor.slnx` on a clean clone builds the server set with zero errors in Debug and Release, with no stub of a `System.*` namespace anywhere in product code, and no compiled source referencing `System.Runtime.Remoting*` or `System.EnterpriseServices` (grep).
- The spike's smoke program, built against the real `Server` and `Store.SQLite` projects in **Debug**, starts a server on a SQLite catalog in a temporary instance directory, evaluates its expressions and runs its DDL/DML script. Run the single-session part as the gate; run the concurrency phase (8 threads × 15 cycles) and record its error count without gating on it. Before the K4 change, the same Debug run fails at the first process execution; that run is K4's reproduction (the harness in `test-harness-inprocess` will start a Debug server in every fixture and so keeps it pinned).
- The spike's coverage run against the real library assemblies gives the previous result plus the new script below (`pass=63 fail=14`). Any other change is investigated and reported, not adjusted; never edit an existing script's expected values.
- DTC characterization test (the project testing rule): a new script in `tests/libraries/TestFramework.Coverage.Scripts/Documents/` that sets `UseDTC := true` on the current process through `System.Processes` and asserts the error code (the coverage scripts' `try … except on E : Error do` pattern; see `Exceptions.d4` for reading `.Code`), and asserts that setting it to false still works. Wire it into `tests/DAE.Server.Tests/CoverageTest.cs` so the spike's coverage run executes it; `test-harness-inprocess` picks it up through discovery.
- K6 is verified by inspection and the grep above: no in-process path constructs `RemoteServer*` objects, and a hosted test belongs to `test-client-server` (`feat-ws3-test-layers`).
- `docs/migration-notes.md`: the DTC entry becomes **done** (with the error code); `Connection.SQLCEConnection` becomes **done**; add entries for the removed public remoting members (`ITrackingHandler` on `RemoteServer`, `ISponsor` on `FileStreamHeader`, the lease constants) and for the removed `IServerDTCTransaction`.

## Risks

- A resource name that changes breaks only when that exception is raised or that script is loaded: list the manifest names of each new assembly and compare, as `build-core-sdk-projects` did.
- `System.Data.SQLite.Core` 1.0.119 against 1.0.60: the spike ran on 1.0.119 with only the `ResultCode` rename, but its native interop must be found at run time from the build output; check the smoke run uses the package's DLL, not a stray copy.
- `Newtonsoft.Json` 13 changed defaults since 9 (for example a default `MaxDepth` of 64 for reading). `RESTMarshal.cs` and NativeCLI serialization are not exercised by the smoke run; note it for `test-client-server`.
- Excluding the WCF files leaves `DAE.Client`'s in-process `DataphorServiceHost` path unavailable until WS5; `ui-client-layering` will meet it.

If this splits, the ticket that finishes the work item keeps the slug `build-server-project`; earlier pieces get new slugs and are chained to it with `prereq:`.
