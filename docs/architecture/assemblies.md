# Assemblies and projects

What each assembly holds and which project builds it; the layer diagram it goes with is in the parent, [Dataphor architecture](../architecture.md).

| Assembly | Project | Role |
|---|---|---|
| `Alphora.Dataphor` | `src/engine/Base` | Collections, exceptions, streams, reflection helpers; **BOP** (`Base/BOP`) |
| `Alphora.Dataphor.DAE` | `src/engine/DAE/DAE.csproj` | Engine: `Language*/` (lexer, parser, emitters for D4, SQL, RealSQL), `Compiling/` (`Compiler.cs` is 13k lines), `Runtime.Instructions/` (one class per operator, ~1,080 registered), `Schema/`, `Device.Memory/`, `Device.Catalog/`, `Store/`, `Server/` (`Engine`, `ServerSession`, `ServerProcess`, plans, cursors), `Debug/` |
| `AlphoraDataphorServer` | `src/engine/Server` | `Server : Engine` adds the persistent catalog (`ServerCatalogDevice*`), libraries (`Schema/LibraryUtility.cs`), instances (`InstanceManager`, `ServerConfiguration`), the remote object layer (`RemoteServer*`), WCF services (`Service/`), `NativeCLI/`, .NET connections (`Connection/`) |
| `Alphora.Dataphor.DAE.Server.Hosting` | `src/engine/Server.Hosting` | The two CoreWCF hosts (`DataphorServiceHost`, `ListenerServiceHost`) and the host side of the binding; needs the ASP.NET Core shared runtime |
| `Alphora.Dataphor.DAE.Host` | `src/hosts/Host` | The host program: `DataphorHost` (the Generic Host, the console and service lifetimes, the command line) and `ServiceUtility`, which registers, removes and inspects the Windows service through the service control manager. Both host executables are a one-line `Main` over it, and the tray configuration utility uses `ServiceUtility` |
| `AlphoraDataphorCommon` | `src/protocol/Common` | WCF binding and URI helpers (`DataphorServiceUtility`), channel factory cache, the guarded channel holder each client keeps its channel in, listener client |
| `AlphoraDataphorClient` | `src/protocol/Client` | `ClientServer` (WCF proxy) wrapped by `LocalServer` (client-side catalog cache, local plans/cursors/streams) |
| `Alphora.Dataphor.DAE.NativeCLI` | `src/protocol/NativeCLI` | Simplified, JSON-friendly call interface and its WCF client |
| `Alphora.Dataphor.DAE.Client` | `src/client/DAE.Client` | `DataSession`, `DataView`/`TableDataSet`, `DataSource`/`DataLink`, aliases (`AliasManager`) |
| store assemblies | `src/engine/Store.SQLite`, `Store.MSSQL`, `Store.PGSQL` | `SQLStore` implementations |
| library assemblies | `libraries/*/Source` (and `samples/`, `tests/libraries/`) | Devices, connections, Frontend server, System.* operators |

The engine can run without the server layer (`Engine` alone has an in-memory catalog); everything a real deployment uses goes through `Server`.
