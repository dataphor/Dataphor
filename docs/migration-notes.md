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
| planned | Distributed (DTC) transactions raise "not supported". They have not functioned since 2009. | Remove any use |
| planned | The FHIR, CQF and PHINVADS libraries are parked and not shipped, with every D4 class name they register (prefix `FHIR.`, `CQF.`, `PHINVADS.`). The general-purpose dynamic and JSON nodes in System.Internet keep shipping under their existing `FHIR.Core.` names. The names `FHIRCore.ObjectAsJSON*` and `FHIRCore.FHIRBaseDisplayString` were never registered by this codebase; they appear only in commented-out D4. | Check scripts for `class "FHIR…"` references outside comments; any that name a parked library need another implementation |
| done | The MySQL, DB2, DB2/400, InterSystems Caché, Linter and Fastore devices and connections, the DataDirect Oracle connection and the ODP.NET (`Oracle.DataAccess`) Oracle connection are removed, with the D4 class names they registered. | Move the data to a supported device (SQL Server, PostgreSQL, the in-memory device or SimpleDevice) before upgrading; remove `create device` statements that name these classes |
| planned | Devices and connections for Oracle (through the Microsoft provider), ADO (COM) and SAS are parked and not shipped. | SQL Server, PostgreSQL, the in-memory device and SimpleDevice ship in 4.0 |
| planned | The PostgreSQL device moves from Npgsql 2.0 to the current Npgsql. Type mapping (notably timestamps and time zones), connection-string keywords and error details may differ. | Test against the production PostgreSQL version; specific differences will be listed here as the port finds them |
| planned | The SQL Server device uses `Microsoft.Data.SqlClient`. The intent is that existing device definitions connect unchanged, including to servers without a trusted certificate. | Test against the production SQL Server version; the minimum supported version will be documented |

## Custom C# extensions

| Status | Change | What to do |
|---|---|---|
| planned | Extensions must be recompiled for .NET 10 against the 4.0 assemblies (`net10.0`, or `net10.0-windows` for anything that touches the UI). Assembly names, namespaces and the signing key of the Dataphor assemblies are unchanged. | Rebuild from source; binaries built for 3.1 will not load |
| planned | Third-party components used by an extension need versions that support .NET 10. | Inventory them early; UI and reporting suites built for .NET Framework are the usual blockers |
| planned | .NET APIs that no longer exist or throw at run time: .NET Remoting, `AppDomain` creation, `Delegate.BeginInvoke`, `Thread.Abort`, `BinaryFormatter`, CodeDom compilation, code access security attributes, `System.Web` (except `HttpUtility`), WCF service hosting. | Replace before recompiling |
| planned | WinForms `ContextMenu`, `MenuItem`, `MainMenu`, `ToolBar` and `StatusBar` compile on .NET 10 but throw when constructed. Dataphor's own controls and form nodes move to the `…Strip` equivalents, and any public member typed with the old classes changes type. | Custom controls and form nodes must make the same replacement; specific member changes will be listed here as they happen |
| planned | Code that reaches through a Dataphor connection to `System.Data.SqlClient` types must use `Microsoft.Data.SqlClient`. | Change the namespace |

## Forms and the Windows client

| Status | Change | What to do |
|---|---|---|
| planned | `ScriptAction` scripts are compiled with Roslyn instead of CodeDom. The script contract is intended to be unchanged. | Scripts that call APIs missing from .NET 10 need editing; test every form that has a script action |
| planned | Form documents (`.dfd`, `.dfdx`), node type tables (`.dnt`) and library files (`.d4l`) keep their formats. | Nothing |

## Building from source

| Status | Change | What to do |
|---|---|---|
| planned | The repository is reorganized (`src/`, `libraries/`, `samples/`, `tests/`, …) and builds with `dotnet build`; the old solutions and batch files are gone. | See `AGENTS.md` and the repository layout section of the plan |
