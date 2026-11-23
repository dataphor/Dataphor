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
 │ hosts: one program, two names - DAEServer.exe, DAEService.exe    │
 │ Server.Hosting — DataphorServiceHost, ListenerServiceHost        │
 │ Server — RemoteServer*, persistent catalog, WCF services,        │
 │          library loading, NativeCLI service                      │
 │ DAE (Engine) — D4 compiler, plan nodes, schema, memory device,   │
 │          catalog device, application transactions, debugger      │
 │ Base — utilities, BOP serializer                                 │
 └──────┬──────────────────────┬────────────────────────┬───────────┘
   catalog store          devices (D4 objects)      D4 libraries
   SQLite│MSSQL│PG         Memory, Simple, MSSQL,    libraries/<Name>/
   (27 DAE* tables)       PGSQL, …                  .d4l + Documents/ + dll
```

Each box above is one or more assemblies; the table of assembly, project and role, and the note that the engine runs without the server layer, is in [assemblies.md](architecture/assemblies.md).

## Sessions, processes, plans

A client connects and gets a session, a session starts processes, and a process prepares statements into plans and opens cursors over them, each with a remote counterpart: [sessions-processes-plans.md](architecture/sessions-processes-plans.md).

## Catalog and catalog store

The catalog is the set of all schema objects, loaded lazily from the catalog store - which also holds the users, rights and settings that exist nowhere else - and the transactions that write it: [catalog-and-store.md](architecture/catalog-and-store.md).

## Libraries and class loading

A library is a directory with a descriptor, documents and optionally assemblies; registering one loads those assemblies and reads the D4 class names they register, which is how D4 binds to C#: [libraries-and-class-loading.md](architecture/libraries-and-class-loading.md).

## Devices

A device translates the plan nodes it claims to support into a native command and leaves the rest to the engine; which devices ship, and what they build on, is in [devices.md](architecture/devices.md).

## Frontend: from schema to screen

The server derives a form document from a query expression and a page type, a customization merges a delta onto it, and the client deserializes the result into a node tree: [frontend.md](architecture/frontend.md).

## Hosting, protocol, discovery

What loads an instance and serves it: three WCF services on one binary-over-TCP binding, the CoreWCF hosts and the host program around them, the shutdown order and the contract declarations - [hosting-protocol-discovery.md](architecture/hosting-protocol-discovery.md).

## On disk

Where the binaries, the shipped libraries, the instance list, the catalog store file, the per-instance data and the client aliases live: [on-disk.md](architecture/on-disk.md).

## What is alive

Which projects are in use, which work but are lightly used, which are parked under `legacy/` and not built, and what the port removed: [what-is-alive.md](architecture/what-is-alive.md).

## Third-party code in the tree

The third-party libraries and binaries the tree carries, where each one lives and which version it is now: [third-party-code.md](architecture/third-party-code.md).
