# Dataphor architecture

How the system fits together as of the last 3.1-era commit (2019), written as the orientation map for the modernization. Where the modernization changes something, the change is described in [modernization-plan.md](modernization-plan.md); this document describes what exists. Paths are those of the reorganized repository; the plan's [old → new table](modernization-plan.md#repository-layout) maps the paths used before it.

## The idea in one paragraph

An application is written as a **D4 library**: scripts that declare types, tables, views, constraints, references and operators, plus optional form documents. The **Dataphor server** compiles D4, stores the resulting catalog, and executes queries by pushing as much as it can down to SQL back ends through **devices** and doing the rest itself. The **Frontend** library looks at the catalog — keys, references, metadata tags — and *derives* user-interface forms (browse, edit, add, lookups, master/detail) as XML documents. A **client** downloads a form document, builds a tree of UI nodes from it and binds the nodes to live server cursors. A customer's system is therefore mostly D4 and tags, with hand-customized forms and occasional C# extensions.

## Layers and assemblies

```
 ┌──────────────────────────── clients ─────────────────────────────┐
 │ Dataphoria IDE          Windows client                           │
 │ src/windows/Dataphoria  src/windows/WindowsClient (exe),         │
 │                         src/windows/Frontend.Client.Windows      │
 │        └── Frontend.Client  — node tree, form loader, Pipe       │
 │        └── DAE.Client.Controls — WinForms data-aware controls    │
 │        └── DAE.Client — DataSession, DataView, DataSource        │
 │        └── Client — WCF proxy + LocalServer (client catalog cache)│
 └───────────────────────────────┬──────────────────────────────────┘
        WCF, net.tcp, binary encoding: IDataphorService (88 operations)
 ┌───────────────────────────────┴──────────────────────────────────┐
 │ hosts: DAEServer (console) · DAE.Service (Windows service)       │
 │ Server — DataphorServiceHost, RemoteServer*, persistent catalog, │
 │          library loading, NativeCLI service, listener            │
 │ DAE (Engine) — D4 compiler, plan nodes, schema, memory device,   │
 │          catalog device, application transactions, debugger      │
 │ Base — utilities, BOP serializer                                 │
 └──────┬──────────────────────┬────────────────────────┬───────────┘
   catalog store          devices (D4 objects)      D4 libraries
   SQLite│MSSQL│PG         Memory, Simple, MSSQL,    libraries/<Name>/
   (27 DAE* tables)       PGSQL, …                  .d4l + Documents/ + dll
```

| Assembly | Project | Role |
|---|---|---|
| `Alphora.Dataphor` | `src/engine/Base` | Collections, exceptions, streams, reflection helpers; **BOP** (`Base/BOP`) |
| `Alphora.Dataphor.DAE` | `src/engine/DAE/DAE.csproj` | Engine: `Language*/` (lexer, parser, emitters for D4, SQL, RealSQL), `Compiling/` (`Compiler.cs` is 13k lines), `Runtime.Instructions/` (one class per operator, ~1,080 registered), `Schema/`, `Device.Memory/`, `Device.Catalog/`, `Store/`, `Server/` (`Engine`, `ServerSession`, `ServerProcess`, plans, cursors), `Debug/` |
| `AlphoraDataphorServer` | `src/engine/Server` | `Server : Engine` adds the persistent catalog (`ServerCatalogDevice*`), libraries (`Schema/LibraryUtility.cs`), instances (`InstanceManager`, `ServerConfiguration`), the remote object layer (`RemoteServer*`), WCF services (`Service/`), `NativeCLI/`, .NET connections (`Connection/`) |
| `AlphoraDataphorCommon` | `src/protocol/Common` | WCF binding and URI helpers (`DataphorServiceUtility`), channel factory cache, listener client |
| `AlphoraDataphorClient` | `src/protocol/Client` | `ClientServer` (WCF proxy) wrapped by `LocalServer` (client-side catalog cache, local plans/cursors/streams) |
| `Alphora.Dataphor.DAE.NativeCLI` | `src/protocol/NativeCLI` | Simplified, JSON-friendly call interface and its WCF client |
| `Alphora.Dataphor.DAE.Client` | `src/client/DAE.Client` | `DataSession`, `DataView`/`TableDataSet`, `DataSource`/`DataLink`, aliases (`AliasManager`) |
| store assemblies | `src/engine/Store.SQLite`, `Store.MSSQL`, `Store.PGSQL` | `SQLStore` implementations |
| library assemblies | `libraries/*/Source` (and `samples/`, `tests/libraries/`) | Devices, connections, Frontend server, System.* operators |

The engine can run without the server layer (`Engine` alone has an in-memory catalog); everything a real deployment uses goes through `Server`.

## Sessions, processes, plans

A client **connects** and gets a *session* (`ServerSession`, one per login). A session starts *processes* (`ServerProcess`): a process is one logical thread of execution with its own transaction stack and device sessions. A process **prepares** a statement or expression into a *plan* (compile → bind → optimize into a tree of `PlanNode`s), **executes** or **opens** it, and for table results gets a *cursor*. Every one of these has a remote counterpart (`RemoteServerSession`, `RemoteServerProcess`, …) that the WCF service exposes by integer handle (`Service/HandleManager.cs`). In-process callers and tests use the `IServer*` interfaces in `src/engine/DAE/CLI.cs` directly.

Compilation is entirely in `Compiler.cs`; execution is interpretation of the plan-node tree (`PlanNode.Execute`). There is **no IL generation**: a `ShouldEmitIL` session flag is plumbed end to end but nothing reads it, and the one remaining `EmitInstructionIL` method (`ArithmeticNodes.cs`) is commented out.

## Catalog and catalog store

The **catalog** is the set of all schema objects. In memory it is `Schema.Catalog`, loaded lazily: on a name-resolution miss the catalog device reads the object's *D4 script* from the store and compiles it (`ServerCatalogDeviceSession`, `NameResolutionCache`, `ClassLoader`). The **catalog store** (`Device.Catalog/CatalogStore.cs`) is a small ISAM-style layer over a SQL database: 27 `DAE*` tables — object headers and scripts (`DAEObjects`, `DAECatalogObjects`, `DAEOperators`, …), dependencies, loaded libraries and versions, registered classes, **and data that exists nowhere else**: users, roles, rights and assignments, device users with their encrypted external passwords, and server settings.

`SQLStore` (`DAE/Store`) abstracts the back end; `SQLStoreConnection` emulates nested transactions with an undo log of inverse statements; `SQLStoreCursor` implements seek/range navigation by generating `select … where … order by` statements.

**Store transactions** (`ServerCatalogDeviceSession`, "Catalog Store" and "Transaction" regions). A catalog device session takes its store connection from the pool in `CatalogStore` for each read or write and gives it back when it is done, except while it has a store transaction open:

- Reads run in autocommit; on SQLite in write-ahead-logging mode each statement reads its own committed snapshot and never waits for the writer.
- A process transaction's store write transaction begins at its first store write, or earlier, at its first statement that changes the catalog (`DDLNode.InternalBeforeExecute` → `CatalogDeviceSession.BeginStoreWriteTransaction`), and covers every open transaction level at once. On SQLite it is begun immediate, so it takes the store's single write lock and holds it until that process transaction ends; a transaction that only reads never takes it. The store's transaction count is 0, or the number of open levels down to the deepest one that has written; it never exceeds the session's open levels, and an inner level that did not write ends without touching the store.
- **Lock order: the store's write lock before the catalog's lock.** Drops and alters write the store inside `lock (program.Catalog)`, and a session holding the store's write lock may be waiting for that lock, so a session that waited for the store's lock inside it would deadlock until the busy timeout. That is why DDL statements take the store's lock before they run; in Debug builds an assertion at the store begin fails if the catalog's lock is held. DDL that already runs inside the catalog's lock (an application transaction's copy of a table, made when a statement first references the table) skips that early begin, and writes nothing to the store. Other engine locks (the library list's, for instance) are not checked, and library registration still writes inside one.
- Until the store transaction ends the session keeps its connection and every store read of the session goes through it, so a transaction sees its own uncommitted writes (the dependents check of a drop, for example). A transaction that read before its first write read committed data from before its write lock; conflicting creates are caught by the store's keys and the engine's catalog lock.
- D4 queries over the catalog tables that the store answers (`CatalogDeviceTable`) open a connection of their own and see committed data only, not the calling transaction's uncommitted store writes.
- A wait for the write lock longer than the store's `BusyTimeout` (30 s by default) raises 130103. A session whose client disappears while its transaction holds the write lock blocks every other session's catalog changes until the server reclaims the session (`port-abandoned-session-cleanup`).

The default store is **SQLite** (`Server.DefaultCatalogStoreClassName`, returned by `Server.GetCatalogStoreClassName()` when the setting is empty), a file at `<instance>\Catalog\DAECatalog.sqlite`. The store class and connection string are per-instance settings (`catalogstoreclassname`, `catalogstoreconnectionstring`; `%CatalogPath%` is substituted). An instance whose settings still name the 3.1 SQL Server Compact store, or a `.sdf` connection string, does not start (error 115108, checked in `ServerCatalogDevice.InternalStart`); a store class that cannot be loaded raises 115107.

## Libraries and class loading

A **library** is a directory `<Name>/` with `<Name>.d4l` (BOP XML: version, required libraries, files, default device, settings), `Documents/` and optionally `Upgrades/`. Registering a library (`LibraryUtility.RegisterLibrary`): register required libraries first; `Assembly.LoadFrom` each file marked `isassembly` from the library directory; read the assembly's `[DAERegister("…")]` attribute and call that class's static `GetClasses()` to get a *D4 class name → assembly-qualified type name* map; then run `Documents/Register.d4` with the library current, which creates the library's catalog objects (`create operator Foo(...) class "Lib.FooNode"`, `create device … class "MSSQLDevice.MSSQLDevice"`).

Everything D4 binds to C# goes through that name map (`ClassLoader.CreateObject` → `Type.GetType` → `Activator.CreateInstance` → set properties from attributes by reflection). Devices find their connection class the same way, so a device has no compile-time reference to a database driver. Assemblies are never unloaded. On the client, a class-loader miss downloads the library's files from the server and loads them (`LocalServer`, `Frontend.Client/Session.cs`), and a resolve hook (`Base.Windows/AssemblyUtility.cs`) matches assemblies by simple name.

## Devices

A device translates plan nodes it claims to support into a native command and leaves the rest to the engine (`Compiling/Visitors/DetermineDeviceVisitor`). `System.MemoryDevice` (in the engine) and `SimpleDevice` (memory plus file persistence) need nothing external. `SQLDevice` is the base for SQL translation (10k lines); `MSSQLDevice` and `PGSQLDevice` specialize it and call into a `SQLConnection` implementation (the Oracle, ADO and SAS device and connection libraries that did the same are parked under `legacy/libraries/` and not built). The connection base classes are in `src/engine/DAE/Connection` and `src/engine/Server/Connection` (the `.cs` files of the same names under `libraries/Connection/Source` are uncompiled leftovers).

## Frontend: from schema to screen

Server side (`libraries/Frontend`, assembly `Alphora.Dataphor.Frontend.Server`): a device that exposes each library's `Documents/` folder as tables, plus the derivation pipeline `Derivation → Elaboration → Structuring → Production` that turns a query expression and a page type into a form document.

A **form document** is BOP XML describing a node tree (`.dfd`). A **customized form** (`.dfdx`, DILX) names one or more ancestor documents — themselves D4 expressions such as `.Frontend.Derive('Lib.Table', 'Browse')` — and a delta to merge onto them.

Client side: `Frontend.Client.Session.SetApplication` asks the server which node types and library files the application needs; `Host.Load` evaluates the form's document expression through `Pipe` (which calls `LoadIfNecessary(expr, crc)` over the ordinary D4 session and keeps a CRC-validated cache), then `BOP.Deserializer` maps element names to .NET types via a node type table (`*.dnt`) and builds the tree. `Frontend.Client` holds the platform-neutral nodes (sources, actions, modules); `Frontend.Client.Windows` holds the WinForms implementations. A `ScriptAction` node compiles C# or VB at run time (CodeDom).

## Hosting, protocol, discovery

`DataphorServiceHost.Start()` loads the instance from `%ProgramData%\Alphora\Dataphor\Instances\ServerConfigurations.config`, starts the `Server`, and opens three self-hosted WCF services, all on one custom binding (binary message encoding over TCP, no security by default, 2 GB limits, infinite timeouts):

| Service | Address | Contract |
|---|---|---|
| Main | `net.tcp://<host>:<port>/<instance>/service` (default port 8061) | `IDataphorService`, 88 operations, handle-based |
| Native CLI | `…/<instance>/service/native` | `INativeCLIService`, 11 operations |
| Listener | `net.tcp://<host>:8060/listener/service` | `IListenerService`: instance name → URI |

The server interfaces (`src/engine/Server/Contracts`) and the client's asynchronous copies (`src/protocol/Client/Contracts`, `src/protocol/NativeCLI`) are separate declarations of the same contracts. `RemoteServerObject` still derives from `MarshalByRefObject` and manipulates .NET Remoting leases — a leftover from the pre-WCF protocol. A client alias (`Aliases.config`) is either a connection alias (host, instance, port) or an in-process alias that starts a server inside the client process.

## On disk

| Location | Content |
|---|---|
| `<install>\` | Binaries, with the native SQLite interop library |
| `<install>\Libraries\<Name>\` | Shipped libraries; library assemblies are built *into* these folders |
| `%ProgramData%\Alphora\Dataphor\Instances\ServerConfigurations.config` | Instance list |
| `%ProgramData%\Alphora\Dataphor\Instances\<name>\Catalog\` | Catalog store file |
| `%ProgramData%\Alphora\Dataphor\Instances\<name>\Libraries\`, `…\Log\` | Per-instance library data (documents device, SimpleDevice tables), logs |
| `%ProgramData%` and `%AppData%` `…\Alphora\Dataphor\Aliases.config` | Machine and user client aliases |

## What is alive

| State | Projects |
|---|---|
| Core, in use | Base, DAE, Server, Common, Client, DAE.Client, DAE.Client.Controls, Frontend.Client, Frontend.Client.Windows, WindowsClient, Dataphoria, DAEServer, DAEService, ConfigurationUtility, D4Runner; libraries Frontend, SQLDevice, MSSQLDevice, SimpleDevice, Connection, System.Platform/Internet/Integration, Security, General |
| Working, lightly used | DAE.Client.Provider, NativeCLI, Frontend.Client.WPF (one scheduler control), PGSQL device and connection, Store.MSSQL/Store.PGSQL stores, samples |
| Parked under `legacy/`, not built | Side projects (2015–2019): FHIR*, CQF*, PHINVADS* libraries (`legacy/libraries/`); `legacy/Dataphoria/` (MVC 5 / Web API 2 REST and "cloud" prototypes), with the cloud prototype's sample library `legacy/Processor/` and design documents `legacy/Design/`. Also the Oracle (Microsoft provider), ADO and SAS device and connection libraries, and the DocBook reference generator library `Documentation` (`legacy/libraries/`) |
| Cannot survive .NET 10 as written | WCF self-hosting, COM+ transaction class, Windows service installer class |

## Third-party code in the tree

ICSharpCode.TextEditor, DockPanel Suite (WeifenLuo) and Ascend controls, Npgsql 2.0.4 with Mono.Security (all binaries in `external/`), ADODB interop (used only by parked code under `legacy/`), System.Data.SQLite (NuGet `System.Data.SQLite.Core` 1.0.119, with its native interop for each architecture; 3.1 used a checked-in 1.0.60, 32-bit only), Newtonsoft.Json (NuGet 13.0.4; 3.1 used 9.0.1), Hl7.Fhir 0.9x (NuGet; used only by parked code under `legacy/`), plus a complete copy of WiX 3.5 under `Utilities/`. The NuGet versions of the converted projects are in `Directory.Packages.props`.
