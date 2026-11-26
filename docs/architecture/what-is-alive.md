# What is alive

Which projects are in use, which are parked, and what the port removed. Part of the [Dataphor architecture](../architecture.md).

| State | Projects |
|---|---|
| Core, in use | Base, DAE, Server, Common, Client, DAE.Client, DAE.Client.Controls, Frontend.Client, Frontend.Client.Windows, WindowsClient, Dataphoria, DAEServer, DAEService, ConfigurationUtility, D4Runner; libraries Frontend, SQLDevice, MSSQLDevice, SimpleDevice, Connection, System.Platform/Internet/Integration, Security, General |
| Working, lightly used | DAE.Client.Provider, NativeCLI, Frontend.Client.WPF (one scheduler control), PGSQL device and connection, Store.MSSQL/Store.PGSQL stores, samples |
| Parked under `legacy/`, not built | Side projects (2015–2019): FHIR*, CQF*, PHINVADS* libraries (`legacy/libraries/`); `legacy/Dataphoria/` (MVC 5 / Web API 2 REST and "cloud" prototypes), with the cloud prototype's sample library `legacy/Processor/` and design documents `legacy/Design/`. Also the Oracle (Microsoft provider), ADO and SAS device and connection libraries, and the DocBook reference generator library `Documentation` (`legacy/libraries/`) |
| Removed by the port, with what replaced it | The COM+ distributed-transaction class `ServerDTCTransaction` and the .NET Remoting plumbing around the remote server objects, both gone (`server-remove-remoting-and-dtc`); setting `UseDTC` now raises error 106119, and an abandoned session is reclaimed by the server's own idle watcher instead of a remoting lease. The Windows service installer class is gone too, replaced by `ServiceUtility` driving the Windows service control manager. The engine-side and server-side JSON marshalling helpers that served the parked web prototype - `RESTResult` and `RESTMarshal` - are deleted with no replacement (`hygiene-remove-rest-leftovers`); nothing in the product marshals engine values to JSON. What hosts and serves the server now is in [Hosting, protocol, discovery](hosting-protocol-discovery.md) |
