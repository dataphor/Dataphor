# Call-level Interface

## Summary

The call-level interface, or CLI, is the lowest level at which a program talks to a Dataphor Server: a small set of .NET interfaces for connecting, preparing a statement or expression, executing it and reading a result. Everything else that reaches the server - the [Data Access Components](data-access-components.md), the ADO.NET provider, the Frontend clients, the IDE - is built on it. A program that only needs to run D4 and read rows can use it directly.

**This is an overview, not an API reference.** The 3.x documentation had a generated .NET API reference here, produced from the source comments by a tool that no longer exists, against a surface that has since moved; the published help file's chapter for it is a one-entry table of contents. Rather than publish a stale or empty reference, this page names the few types a caller starts from and says where they are, so that the source itself - which carries the documentation comments - can be read in the right place. For the shape of the objects and the rules they follow, [Sessions, processes, plans](../architecture/sessions-processes-plans.md) is the current description.

## The interfaces

All of these are in the namespace `Alphora.Dataphor.DAE`, declared in one file, `src/engine/DAE/CLI.cs`, in the assembly `Alphora.Dataphor.DAE`. They describe four nested lifetimes, each one started by the one above it:

| Interface | What it is |
|---|---|
| `IServer` | A Dataphor Server. Starts and stops, and hands out sessions. |
| `IServerSession` | One login's session on that server. Starts processes. |
| `IServerProcess` | One logical thread of execution, with its own transaction stack and its own sessions on the back-end databases. Prepares statements and expressions, and begins, commits and rolls back transactions. |
| `IServerScript`, `IServerPlan`, `IServerCursor` | What a process prepares: a script is a parsed D4 script of several statements; a plan is one prepared statement or expression, ready to execute; a cursor is the result of opening a plan that returns a table. |

Each of these is disposable, and disposing one disposes what it handed out. Only one call runs through a process at a time - disposing it is one of those calls - so stopping a process waits for a call already in flight on it.

The same interfaces serve an in-process server and a remote one. A caller that wants a server over the network asks `Alphora.Dataphor.DAE.Server.ServerFactory.Connect` (in `src/protocol/Client/Server/ServerFactory.cs`, assembly `AlphoraDataphorClient`) for an `IServer`, and gets back a local object that implements it over the wire. A caller hosting the engine in its own process builds a server directly instead; the host program in `src/hosts/Host` is the worked example. Either way the calling code is the same, which is the point of the interface.

## The native interface

There is a second, simpler entry point for callers that do not want the engine's own data types: `Alphora.Dataphor.DAE.NativeCLI` (`src/protocol/NativeCLI`, assembly `Alphora.Dataphor.DAE.NativeCLI`). It takes D4 text and parameters as plain .NET values and returns results as plain values and arrays, with no cursors and no plan objects. `NativeCLISession` is the stateful form, `NativeSessionCLIClient` and `NativeStatelessCLIClient` the two clients over the network. It is the interface to reach for from another language or another platform; it is not a replacement for the interfaces above, because it cannot open a cursor or hold a plan.

## Where it is explained

- [Sessions, processes, plans](../architecture/sessions-processes-plans.md) - what a session, process, plan and cursor are in this engine, what the remote counterparts are, and the transaction rules a caller has to know.
- [Dataphor Server](../users-guide/dataphor-server.md#data-access-layer-dal) - where the CLI sits in the product, in the User's Guide.
- [Dataphor Product Anatomy](../developers-guide/dataphor-product-anatomy.md) - the same, from the Developer's Guide.
- [Hosting, protocol, discovery](../architecture/hosting-protocol-discovery.md) - what serves the interface on the network and on which addresses.
