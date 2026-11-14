# Dataphor architecture

How the system fits together as of the last 3.1-era commit (2019), written as the orientation map for the modernization. Where the modernization changes something, the change is described in [modernization-plan.md](modernization-plan.md); this document describes what exists.

## The idea in one paragraph

An application is written as a **D4 library**: scripts that declare types, tables, views, constraints, references and operators, plus optional form documents. The **Dataphor server** compiles D4, stores the resulting catalog, and executes queries by pushing as much as it can down to SQL back ends through **devices** and doing the rest itself. The **Frontend** library looks at the catalog — keys, references, metadata tags — and *derives* user-interface forms (browse, edit, add, lookups, master/detail) as XML documents. A **client** downloads a form document, builds a tree of UI nodes from it and binds the nodes to live server cursors. A customer's system is therefore mostly D4 and tags, with hand-customized forms and occasional C# extensions.

## Layers and assemblies

```
 ┌──────────────────────────── clients ─────────────────────────────┐
 │ Dataphoria IDE        Windows client                             │
 │ Dataphor/Dataphoria   Frontend.Client.Windows(.Executable)       │
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
   SQLCE│SQLite│MSSQL│PG   Memory, Simple, MSSQL,    Libraries/<Name>/
   (27 DAE* tables)       Oracle, PGSQL, …          .d4l + Documents/ + dll
```

| Assembly | Project | Role |
|---|---|---|
| `Alphora.Dataphor` | `Dataphor/Base` | Collections, exceptions, streams, reflection helpers; **BOP** (`Base/BOP`) |
| `Alphora.Dataphor.DAE` | `Dataphor/DAE/Engine.csproj` | Engine: `Language*/` (lexer, parser, emitters for D4, SQL, RealSQL), `Compiling/` (`Compiler.cs` is 13k lines), `Runtime.Instructions/` (one class per operator, ~1,080 registered), `Schema/`, `Device.Memory/`, `Device.Catalog/`, `Store/`, `Server/` (`Engine`, `ServerSession`, `ServerProcess`, plans, cursors), `Debug/` |
| `AlphoraDataphorServer` | `Dataphor/Server` | `Server : Engine` adds the persistent catalog (`ServerCatalogDevice*`), libraries (`Schema/LibraryUtility.cs`), instances (`InstanceManager`, `ServerConfiguration`), the remote object layer (`RemoteServer*`), WCF services (`Service/`), `NativeCLI/`, .NET connections (`Connection/`) |
| `AlphoraDataphorCommon` | `Dataphor/Common` | WCF binding and URI helpers (`DataphorServiceUtility`), channel factory cache, listener client |
| `AlphoraDataphorClient` | `Dataphor/Client` | `ClientServer` (WCF proxy) wrapped by `LocalServer` (client-side catalog cache, local plans/cursors/streams) |
| `Alphora.Dataphor.DAE.NativeCLI` | `Dataphor/DAE.NativeCLI` | Simplified, JSON-friendly call interface and its WCF client |
| `Alphora.Dataphor.DAE.Client` | `Dataphor/DAE.Client` | `DataSession`, `DataView`/`TableDataSet`, `DataSource`/`DataLink`, aliases (`AliasManager`) |
| store assemblies | `Dataphor/DAE.SQLite`, `DAE.MSSQL`, `DAE.PGSQL` | `SQLStore` implementations |
| library assemblies | `Libraries/*/Source` | Devices, connections, Frontend server, System.* operators |

The engine can run without the server layer (`Engine` alone has an in-memory catalog); everything a real deployment uses goes through `Server`.

## Sessions, processes, plans

A client **connects** and gets a *session* (`ServerSession`, one per login). A session starts *processes* (`ServerProcess`): a process is one logical thread of execution with its own transaction stack and device sessions. A process **prepares** a statement or expression into a *plan* (compile → bind → optimize into a tree of `PlanNode`s), **executes** or **opens** it, and for table results gets a *cursor*. Every one of these has a remote counterpart (`RemoteServerSession`, `RemoteServerProcess`, …) that the WCF service exposes by integer handle (`Service/HandleManager.cs`). In-process callers and tests use the `IServer*` interfaces in `Dataphor/DAE/CLI.cs` directly.

Compilation is entirely in `Compiler.cs`; execution is interpretation of the plan-node tree (`PlanNode.Execute`). There is **no IL generation**: a `ShouldEmitIL` session flag is plumbed end to end but nothing reads it, and the one remaining `EmitInstructionIL` method (`ArithmeticNodes.cs`) is commented out.

## Catalog and catalog store

The **catalog** is the set of all schema objects. In memory it is `Schema.Catalog`, loaded lazily: on a name-resolution miss the catalog device reads the object's *D4 script* from the store and compiles it (`ServerCatalogDeviceSession`, `NameResolutionCache`, `ClassLoader`). The **catalog store** (`Device.Catalog/CatalogStore.cs`) is a small ISAM-style layer over a SQL database: 27 `DAE*` tables — object headers and scripts (`DAEObjects`, `DAECatalogObjects`, `DAEOperators`, …), dependencies, loaded libraries and versions, registered classes, **and data that exists nowhere else**: users, roles, rights and assignments, device users with their encrypted external passwords, and server settings.

`SQLStore` (`DAE/Store`) abstracts the back end; `SQLStoreConnection` emulates nested transactions with an undo log of inverse statements; `SQLStoreCursor` implements seek/range navigation by generating `select … where … order by` statements, or uses an updatable cursor where the back end has one (SQL CE). Each catalog device session holds its own store connection (pooled in `CatalogStore`) and opens a store transaction for every process transaction.

The default store is **SQL Server Compact 4.0** (`Server.GetCatalogStoreClassName()`), a file at `<instance>\Catalog\DAECatalog.sdf`. The store class and connection string are per-instance settings (`catalogstoreclassname`, `catalogstoreconnectionstring`; `%CatalogPath%` is substituted).

## Libraries and class loading

A **library** is a directory `<Name>/` with `<Name>.d4l` (BOP XML: version, required libraries, files, default device, settings), `Documents/` and optionally `Upgrades/`. Registering a library (`LibraryUtility.RegisterLibrary`): register required libraries first; `Assembly.LoadFrom` each file marked `isassembly` from the library directory; read the assembly's `[DAERegister("…")]` attribute and call that class's static `GetClasses()` to get a *D4 class name → assembly-qualified type name* map; then run `Documents/Register.d4` with the library current, which creates the library's catalog objects (`create operator Foo(...) class "Lib.FooNode"`, `create device … class "MSSQLDevice.MSSQLDevice"`).

Everything D4 binds to C# goes through that name map (`ClassLoader.CreateObject` → `Type.GetType` → `Activator.CreateInstance` → set properties from attributes by reflection). Devices find their connection class the same way, so a device has no compile-time reference to a database driver. Assemblies are never unloaded. On the client, a class-loader miss downloads the library's files from the server and loads them (`LocalServer`, `Frontend.Client/Session.cs`), and a resolve hook (`Base.Windows/AssemblyUtility.cs`) matches assemblies by simple name.

## Devices

A device translates plan nodes it claims to support into a native command and leaves the rest to the engine (`Compiling/Visitors/DetermineDeviceVisitor`). `System.MemoryDevice` (in the engine) and `SimpleDevice` (memory plus file persistence) need nothing external. `SQLDevice` is the base for SQL translation (10k lines); `MSSQLDevice`, `OracleDevice`, `PGSQLDevice` and others specialize it and call into a `SQLConnection` implementation. The connection base classes are in `Dataphor/DAE/Connection` and `Dataphor/Server/Connection` (the `.cs` files of the same names under `Libraries/Connection/Source` are uncompiled leftovers).

## Frontend: from schema to screen

Server side (`Libraries/Frontend`, assembly `Alphora.Dataphor.Frontend.Server`): a device that exposes each library's `Documents/` folder as tables, plus the derivation pipeline `Derivation → Elaboration → Structuring → Production` that turns a query expression and a page type into a form document.

A **form document** is BOP XML describing a node tree (`.dfd`). A **customized form** (`.dfdx`, DILX) names one or more ancestor documents — themselves D4 expressions such as `.Frontend.Derive('Lib.Table', 'Browse')` — and a delta to merge onto them.

Client side: `Frontend.Client.Session.SetApplication` asks the server which node types and library files the application needs; `Host.Load` evaluates the form's document expression through `Pipe` (which calls `LoadIfNecessary(expr, crc)` over the ordinary D4 session and keeps a CRC-validated cache), then `BOP.Deserializer` maps element names to .NET types via a node type table (`*.dnt`) and builds the tree. `Frontend.Client` holds the platform-neutral nodes (sources, actions, modules); `Frontend.Client.Windows` holds the WinForms implementations. A `ScriptAction` node compiles C# or VB at run time (CodeDom).

## Hosting, protocol, discovery

`DataphorServiceHost.Start()` loads the instance from `%ProgramData%\Alphora\Dataphor\Instances\ServerConfigurations.config`, starts the `Server`, and opens three self-hosted WCF services, all on one custom binding (binary message encoding over TCP, no security by default, 2 GB limits, infinite timeouts):

| Service | Address | Contract |
|---|---|---|
| Main | `net.tcp://<host>:<port>/<instance>/service` (default port 8061) | `IDataphorService`, 88 operations, handle-based |
| Native CLI | `…/<instance>/service/native` | `INativeCLIService`, 11 operations |
| Listener | `net.tcp://<host>:8060/listener/service` | `IListenerService`: instance name → URI |

The server interfaces (`Dataphor/Server/Contracts`) and the client's asynchronous copies (`Dataphor/Client/Contracts`, `Dataphor/DAE.NativeCLI`) are separate declarations of the same contracts. `RemoteServerObject` still derives from `MarshalByRefObject` and manipulates .NET Remoting leases — a leftover from the pre-WCF protocol. A client alias (`Aliases.config`) is either a connection alias (host, instance, port) or an in-process alias that starts a server inside the client process.

## On disk

| Location | Content |
|---|---|
| `<install>\` | Binaries, native SQL CE DLLs |
| `<install>\Libraries\<Name>\` | Shipped libraries; library assemblies are built *into* these folders |
| `%ProgramData%\Alphora\Dataphor\Instances\ServerConfigurations.config` | Instance list |
| `%ProgramData%\Alphora\Dataphor\Instances\<name>\Catalog\` | Catalog store file |
| `%ProgramData%\Alphora\Dataphor\Instances\<name>\Libraries\`, `…\Log\` | Per-instance library data (documents device, SimpleDevice tables), logs |
| `%ProgramData%` and `%AppData%` `…\Alphora\Dataphor\Aliases.config` | Machine and user client aliases |

## What is alive

| State | Projects |
|---|---|
| Core, in use | Base, DAE, Server, Common, Client, DAE.Client, DAE.Client.Controls, Frontend.Client, Frontend.Client.Windows(.Executable), Dataphoria, DAEServer, DAE.Service(.ConfigurationUtility), D4Runner; libraries Frontend, SQLDevice, MSSQLDevice, SimpleDevice, Connection, System.Platform/Internet/Integration, Security, General |
| Working, lightly used | DAE.Client.Provider, DAE.NativeCLI, Frontend.Client.WPF (one scheduler control), Oracle/PGSQL devices and connections, DAE.MSSQL/DAE.PGSQL stores, samples |
| Side projects (2015–2019) | FHIR*, CQF*, PHINVADS* libraries; top-level `Dataphoria/` (MVC 5 / Web API 2 REST and "cloud" prototypes) |
| Cannot survive .NET 10 as written | SQL CE store, WCF self-hosting, COM+ transaction class, Windows service installer class |

## Third-party code in the tree

ICSharpCode.TextEditor (binary, `External/`), DockPanel Suite and Ascend controls (binaries in `Dataphor/Dataphoria/`), System.Data.SQLite 1.0.60, Npgsql 2.0.4 with Mono.Security, ADODB interop, Newtonsoft.Json 9.0.1 (NuGet), Hl7.Fhir 0.9x (NuGet), plus a complete copy of WiX 3.5 under `Utilities/`.
