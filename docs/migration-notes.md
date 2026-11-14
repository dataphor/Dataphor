# Migration notes: 3.1 → 4.0

A running list of every change that an existing application, a deployment or a custom C# extension can notice when moving from Dataphor 3.1 (.NET Framework) to 4.0 (.NET 10). It exists so that moving an application later is a checklist, not an investigation. It will be turned into a proper guide before release (`docs-migration-guide-custom-code` in the [plan](modernization-plan.md)).

**How to maintain it.** Any change that alters something visible from outside the engine — a D4 class name, an operator, a setting, a file location, a public C# type or member that extensions use, a deployment step — adds an entry here *in the same commit*. One line saying what changed, one saying what to do about it. Mark an entry **planned** while it is only decided and **done** once the code has changed. Keep it generic: no customer names, paths or schema.

## Platform and deployment

| Status | Change | What to do |
|---|---|---|
| planned | Everything runs on .NET 10; nothing runs on .NET Framework. | Install with the 4.0 installer (self-contained; no separate runtime needed). 3.1 can stay installed side by side |
| planned | Clients and server must be the same major version; a 3.1 client is not expected to connect to a 4.0 server. | Upgrade the server and all clients together |
| planned | The embedded catalog store is SQLite; SQL Server Compact is gone and existing catalog files are not converted. | Create a new instance and rebuild the catalog from the application's scripts, as in a routine catalog rebuild. Remove any `catalogstoreclassname`/`catalogstoreconnectionstring` instance settings that name the SQL CE store |
| planned | The Windows service is registered by the installer or the `dataphor` command-line tool, not by an installer class inside the service executable. | Re-register services with the new tooling; existing command-line switches are kept as aliases where practical |
| done | The ASP.NET WebForms web client is removed. | Anything built on it must be rewritten against another front end |
| done | The Silverlight client is removed. | Nothing; it had no users |
| planned | The `AllowSilverlightClients` instance setting is removed. | Remove the setting |
| planned | ClickOnce deployment of the Windows client moves to the .NET publishing tools. | Republish the client with the new profile; existing ClickOnce installations will not update themselves across this change |
| planned | String comparison and culture behaviour are pinned to Windows NLS, as on .NET Framework. | Nothing, unless a host process is built by hand: set `System.Globalization.UseNls` |

## D4, catalog and libraries

| Status | Change | What to do |
|---|---|---|
| planned | The D4 class `Connection.SQLCEConnection` no longer exists. | Remove references to it |
| done | Distributed (DTC) transactions are not supported: setting `UseDTC` (process) or `DefaultUseDTC` (session) to true, through `System.Processes`/`System.Sessions` or the API, raises error 106119. They have not functioned since 2009. | Remove any use; leave the settings false |
| done | An update of `System.Processes` that reaches the end of the row no longer fails with "Column MaxCallDepth not found": the catalog device now reads the table's `MaxNestingLevel` column and passes a changed value to the process's maximum call depth, as it already did for `MaxStackDepth`. For the process running the update, both limits then apply only to the update statement itself; the next statement starts with the previous values. | Nothing; scripts that worked around the error by not updating `System.Processes` can use it |
| done | The FHIR, CQF and PHINVADS libraries are parked and not shipped, with every D4 class name they register (prefix `FHIR.`, `CQF.`, `PHINVADS.`). The general-purpose dynamic and JSON nodes in System.Internet keep shipping under their existing `FHIR.Core.` names. The names `FHIRCore.ObjectAsJSON*` and `FHIRCore.FHIRBaseDisplayString` were never registered by this codebase; they appear only in commented-out D4. | Check scripts for `class "FHIR…"` references outside comments; any that name a parked library need another implementation |
| done | The MySQL, DB2, DB2/400, InterSystems Caché, Linter and Fastore devices and connections, the DataDirect Oracle connection and the ODP.NET (`Oracle.DataAccess`) Oracle connection are removed, with the D4 class names they registered. | Move the data to a supported device (SQL Server, PostgreSQL, the in-memory device or SimpleDevice) before upgrading; remove `create device` statements that name these classes |
| done | Devices and connections for Oracle (through the Microsoft provider), ADO (COM) and SAS are parked and not shipped; their source is kept, unbuilt, under `legacy/libraries/`. | SQL Server, PostgreSQL, the in-memory device and SimpleDevice ship in 4.0 |
| planned | The PostgreSQL device moves from Npgsql 2.0 to the current Npgsql. Type mapping (notably timestamps and time zones), connection-string keywords and error details may differ. | Test against the production PostgreSQL version; specific differences will be listed here as the port finds them |
| planned | The SQL Server device uses `Microsoft.Data.SqlClient`. The intent is that existing device definitions connect unchanged, including to servers without a trusted certificate. | Test against the production SQL Server version; the minimum supported version will be documented |

## Custom C# extensions

| Status | Change | What to do |
|---|---|---|
| planned | Extensions must be recompiled for .NET 10 against the 4.0 assemblies (`net10.0`, or `net10.0-windows` for anything that touches the UI). Assembly names, namespaces and the signing key of the Dataphor assemblies are unchanged. | Rebuild from source; binaries built for 3.1 will not load |
| done | The Dataphor assemblies are version 4.0.0.0 (file version 4.0.0.x, informational version `4.0.0-preview+<commit>`) and are strong-named with the same `Dataphor.snk` key in every configuration; Debug builds were unsigned before. Done for the ten SDK-style projects; the rest follow as they are converted. The D4 `System` library still reports version 3.1 (now a fixed 3.1.0.0 instead of 3.1.<build>.<revision>), so library references to `System` 3 keep matching. | Reference the Dataphor assemblies at version 4.0.0.0 with public key token `5202040c6ce50aea`; remove binding redirects or exact-version references to 3.1 assemblies |
| planned | Third-party components used by an extension need versions that support .NET 10. | Inventory them early; UI and reporting suites built for .NET Framework are the usual blockers |
| done | Removed public members tied to .NET Remoting: `RemoteServer` no longer implements `ITrackingHandler` (`DisconnectedObject`, `MarshaledObject`, `UnmarshaledObject`); `FileStreamHeader` no longer implements `ISponsor`; the constants `RemoteServerObject.LeaseManagerPollTimeSeconds`, `RemoteServerConnection.InitialLeaseTimeSeconds` and `RemoteServerConnection.RenewOnCallTimeSeconds` and the `RemoteServerConnection.InitializeLifetimeService` override are gone. | Remove references |
| done | The interface `Alphora.Dataphor.DAE.Server.IServerDTCTransaction` and the class `ServerDTCTransaction` are removed. | Remove references |
| planned | .NET APIs that no longer exist or throw at run time: .NET Remoting, `AppDomain` creation, `Delegate.BeginInvoke`, `Thread.Abort`, `BinaryFormatter`, CodeDom compilation, code access security attributes, `System.Web` (except `HttpUtility`), WCF service hosting. | Replace before recompiling |
| planned | WinForms `ContextMenu`, `MenuItem`, `MainMenu`, `ToolBar` and `StatusBar` compile on .NET 10 but throw when constructed. Dataphor's own controls and form nodes move to the `…Strip` equivalents, and any public member typed with the old classes changes type. | Custom controls and form nodes must make the same replacement; specific member changes will be listed here as they happen |
| planned | Code that reaches through a Dataphor connection to `System.Data.SqlClient` types must use `Microsoft.Data.SqlClient`. | Change the namespace |
| done | Dataphor references Newtonsoft.Json 13 instead of 9 (the native client interface and the System.Internet library). | Build extensions that use Newtonsoft.Json against version 13 |
| done | The WCF client constructors that take an endpoint configuration name (`ServiceClient<T>(string)`, `DataphorServiceClient<T>(string)`, `NativeCLIClient(hostName, instanceName, endpointConfigurationName)`) throw `NotSupportedException`: the .NET WCF client cannot read endpoints from an application configuration file. | Construct clients with a binding and endpoint address, or with host, instance and port; check deployed configuration files for `<system.serviceModel>` sections |

## Forms and the Windows client

| Status | Change | What to do |
|---|---|---|
| planned | `ScriptAction` scripts are compiled with Roslyn instead of CodeDom. The script contract is intended to be unchanged. | Scripts that call APIs missing from .NET 10 need editing; test every form that has a script action |
| planned | Settings folders that include the version move with it: connection aliases from `%ProgramData%\Alphora\Dataphor\3\` and `%AppData%\Alphora\Dataphor\3\` to `…\4\`; Dataphoria settings and scratch pad, configuration utility settings and the server's temporary stream files from `…\3.1\` to `…\4.0\`. Instance configuration and instance directories carry no version and do not move. Takes effect when the client, IDE and server run against the 4.0 `Alphora.Dataphor.Windows` assembly. | Copy `Aliases.config` and the IDE settings from the 3.x folders to the 4.x folders to keep them; 4.0 does not read the 3.x folders |
| planned | Form documents (`.dfd`, `.dfdx`), node type tables (`.dnt`) and library files (`.d4l`) keep their formats. | Nothing |
| done | A connection alias with the `ClientConfigurationName` setting ("Config Name" in the alias editor) fails to connect with `NotSupportedException`; aliases that carry the setting still load. | Clear the setting and connect with the host name, instance name and port settings; check deployed client configuration files for `<system.serviceModel>` sections |

## Building from source

| Status | Change | What to do |
|---|---|---|
| done | The repository is reorganized: the product code is under `src/`, shipped D4 libraries under `libraries/`, sample libraries under `samples/`, test projects and test libraries under `tests/`, checked-in third-party binaries under `external/`, and the legacy solution is at the root as `Dataphor.sln`. The engine project file `Engine.csproj` is now `DAE.csproj`. Assembly names and namespaces are unchanged. | A custom extension whose project references Dataphor projects, or binaries in `Dataphor/bin`, by relative path must update those paths; see `AGENTS.md` and the repository layout section of the plan for where each folder went |
| planned | The repository builds with `dotnet build`; the old solutions and batch files are gone. | See `AGENTS.md` |
| done | The engine (Base, Base.Windows, DAE), the protocol projects (Common, NativeCLI, Client) and the SQLDevice, System.Platform, System.Internet and System.Integration libraries are SDK-style projects targeting `net10.0`, built with `dotnet build Dataphor.slnx`. Their assemblies now go to `artifacts/bin/<project file name>/<debug\|release>/` instead of a shared `bin` folder or the library folder, and a Debug engine no longer defines `ACCUMULATOR` or `USEVISIT` (Debug and Release now use the same constants, the set 3.1 shipped). | A custom extension that references these project files must itself be an SDK-style project targeting `net10.0`; one that references their built assemblies by path must use the new output folders |
