description: Prove that a server running on .NET 10 can still be reached over the network by the clients we already ship, before the rest of the server hosting work is built on the assumption that it can.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: src/protocol/Common/Contracts/DataphorServiceUtility.cs, src/engine/Server/Service/DataphorServiceHost.cs, src/engine/Server/Service/DataphorService.cs, src/engine/Server/Service/ListenerService.cs, src/engine/Server/Service/ListenerServiceHost.cs, src/engine/Server/Contracts/IDataphorService.cs, src/engine/Server/Contracts/IListenerService.cs, src/engine/DAE/Contracts/DataphorFault.cs, src/protocol/Client/Contracts/IClientDataphorService.cs, src/protocol/Client/Client/ClientServer.cs, src/engine/Server/Server.csproj, Directory.Packages.props
difficulty: hard
----
## Why this is first

The whole of WS5 assumes the existing network protocol can be served from .NET 10 by CoreWCF, with the wire unchanged, so that a 3.1-era client connects to a 4.0 server and vice versa. If that assumption is wrong, every later ticket in the workstream is wrong, and the choice of what to do instead belongs to the owner, not to an implementer. So this item proves the assumption on the smallest possible host and then stops. The plan says it plainly: *if the binding cannot be reproduced, stop and bring the options to the owner; do not improvise a new protocol.*

Nothing else in WS5 is cut as an `implement/` ticket until this one has landed. The one exception already filed is `protocol-contract-parity-test`, which reads interface declarations and never opens a socket, so it is worth having whatever the transport turns out to be.

## What the protocol is today, exactly

Read this as the specification; it is what must still be true afterwards.

**One binding, built in code** — `DataphorServiceUtility.DefaultBuildBinding` in `src/protocol/Common/Contracts/DataphorServiceUtility.cs`:

- `CustomBinding(BinaryMessageEncodingBindingElement, TcpTransportBindingElement)` — binary (not text) message encoding over TCP, in that element order.
- On the encoder: `ReaderQuotas.MaxArrayLength` and `ReaderQuotas.MaxStringContentLength` both `2147483647` (the constant `MaxMessageLength`).
- On the transport: `MaxBufferSize` and `MaxReceivedMessageSize` both `2147483647`. `TransferMode` is left at its default (buffered) and no security is configured, so the channel is unsecured and messages cross whole.
- `SendTimeout` and `ReceiveTimeout` both `TimeSpan.MaxValue`.
- Client and server both get the binding from the same `GetBinding()`, memoized in a static. Three hooks (`BuildBindingCall`, `BuildEndpointIdentityCall`, `ServiceEndpointHook`) let a host replace the binding; nothing in the repository sets them.

**Three endpoints**, addresses built by the same file. Ports 8060 and 8061 are a compatibility surface.

| Service | Address | Contract | Instancing |
|---|---|---|---|
| Main | `net.tcp://<machine>:<port>/<instance>/service`, port from the instance configuration, default 8061 | `IDataphorService`, 88 operations | one singleton object, handed to the host (`new ServiceHost(_service)`) |
| Native CLI | `net.tcp://<machine>:<port>/<instance>/service/native` | `INativeCLIService`, 11 operations | singleton object |
| Listener | `net.tcp://<machine>:8060/listener/service` | `IListenerService` | by type (`new ServiceHost(typeof(ListenerService))`), so per call |

**Contract shape.** `[ServiceContract(Name = "IDataphorService", Namespace = "http://dataphor.org/dataphor/3.0/")]`, and the same pattern for the other two; almost every operation carries `[FaultContract(typeof(DataphorFault))]` (`src/engine/DAE/Contracts/DataphorFault.cs`). Server behaviour attributes: `DataphorService` and `NativeCLIService` are `[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]`; `ListenerService` is `[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]`. The singleton instancing is not decoration: `DataphorService` holds the handle manager and the connection manager that map client-side handles to server objects, and every session shares them.

**The client declares the same contracts separately** (`src/protocol/Client/Contracts/IClientDataphorService.cs`), with all 88 operations as `[OperationContract(AsyncPattern = true)]` `BeginX`/`EndX` pairs under the same contract name and namespace. The wire is therefore identical only if the message action URIs still come out the same: WCF derives an action from the contract namespace, the contract name and the operation name, and for an `AsyncPattern` operation the name comes from the `Begin` method with the prefix stripped. Confirm that rather than assume it.

**Streams are not WCF streams.** Stream support is handle-based and buffered (`OpenStream`, `ReadStream`, `WriteStream` over `byte[]`; `src/engine/Server/Contracts/IDataphorService.cs` from about line 833). No contract operation takes or returns a `Stream`, so transfer mode does not come into it.

## What to settle

Answer these against the current CoreWCF release before writing anything, and record each answer with its source and the package version in the write-up:

- Does CoreWCF accept a `CustomBinding` composed of `BinaryMessageEncodingBindingElement` over its NetTcp transport, with those quotas, limits and infinite timeouts? If a custom binding is not supported, is there a supported binding that produces the same wire, and how would the server be pointed at it without changing the client's binding?
- `InstanceContextMode.Single` with a supplied instance, and `ConcurrencyMode.Multiple`: supported, with the same meaning?
- A 2 GB `MaxReceivedMessageSize`: accepted, and are there separate CoreWCF or Kestrel-side limits (buffer pool, connection, request queue) that cap it lower?
- A `FaultException<DataphorFault>` raised by the service: does it reach the existing client as the same typed fault? The client's error handling depends on it.
- Infinite send and receive timeouts: honoured, or clamped by an idle-connection timeout somewhere in the stack? This interacts with `port-abandoned-session-cleanup`, which assumes a connection stays open between calls.
- Does the existing `AsyncPattern` client contract bind to a CoreWCF-hosted service with no edits? Same question for `NativeCLIClient`, which the server itself uses for server links.
- Which packages and versions, in `Directory.Packages.props`, and what do they pull in? CoreWCF needs `Microsoft.Extensions.Hosting`, which `host-generic-host` brings in properly later; a reference added here is acceptable, a second hosting model is not.

## The proof itself

What the resulting implement ticket must demonstrate, in a test that runs under `dotnet test` on a dynamically allocated port — not by hand, and not against a machine-wide instance:

- A host for `IListenerService` and for enough of `IDataphorService` to carry one whole session: connect, open a session, prepare and execute a statement, open a cursor and fetch rows, allocate a stream and read it back, shut down.
- The client side is the shipped client code (`ClientServer`/`DataSession`, or `ClientConnection`, with `DataphorServiceUtility.GetBinding()` untouched), not a hand-rolled channel.
- A deliberate server-side error arriving at the client as the typed `DataphorFault` it arrives as today.
- A message above WCF's default 64 KB cap — a row set of a few megabytes will do — crossing in both directions, so the quotas are shown to be in effect rather than merely set.

The write-up is a section appended to `docs/modernization-plan.md` under WS5, not a loose file. Keep the proof's host and its test in the tree: `protocol-corewcf-host` grows into them rather than starting again.

## Routing

- **Feasible:** emit one `implement/` ticket that keeps this slug, with the questions above already answered in its body so the implementer has no research left. Do not fold `protocol-corewcf-host` into it — the proof is deliberately smaller than the full host, and that is the point of doing it first.
- **Not feasible, in whole or in part:** `blocked/` with a proposal. Lead with which of the four properties (binary encoding over TCP, singleton instancing, 2 GB messages, the client's asynchronous contract) cannot be had; then what each alternative costs an existing deployment, what happens if we do nothing, and how reversible each choice is. The plan's fallback is "a designed change to the transport, decided by the owner", so the alternatives to lay out are at least: a different supported binding plus a client-side change; the same contracts with a different encoding; and a new transport (gRPC or HTTP) with a compatibility shim for existing clients. Recommend one, but do not implement it.

## Interactions to keep in view

- `src/engine/Server/Server.csproj` excludes six files, each with an XML comment naming this workstream. The proof may need one or two of them; whichever it brings back, the remaining comments and the interim `docs/migration-notes.md` entry about the missing host classes must still read true. `protocol-corewcf-host` finishes that job.
- The listener is a machine-wide singleton on port 8060, and `ListenerServiceHost` swallows the exception when the port is taken — by design, because another instance on the machine may already run one. A test must not depend on owning 8060.
- `ListenerServiceHost` takes `requireSecureConnection` and `useCrossDomainService` parameters and uses neither, and `DataphorServiceHost` never reads `instance.RequireSecureConnection`. The secured-connection settings have no effect on the server as shipped. That is `protocol-transport-security`'s problem; do not quietly start honouring them here.

## TODO

- Answer every question under *What to settle*, with sources and versions.
- If all answers are yes: write the `implement/` ticket (same slug), sized to one agent run, with those answers in it.
- If any answer is no: write the `blocked/` ticket as described, and do not file the implement ticket.
