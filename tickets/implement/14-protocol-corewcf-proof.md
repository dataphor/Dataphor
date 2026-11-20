description: Show that a server running on .NET 10 can still be reached over the network by the client code we already ship, by standing up the smallest possible host and running one real session through it.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: Directory.Packages.props, src/engine/Server/Server.csproj, src/engine/Server/Service/DataphorService.cs, src/engine/Server/Service/ListenerService.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/CoreWcfProofHost.cs, tests/DAE.Server.Tests/CoreWcfProtocolTest.cs, tests/DAE.Server.Tests/TestServerInstance.cs, src/protocol/Common/Contracts/DataphorServiceUtility.cs, src/protocol/Client/Client/ClientServer.cs, src/protocol/Client/Server/LocalServer.cs, docs/modernization-plan.md, docs/migration-notes.md
difficulty: hard
----
## What this ticket is

The plan's first WS5 work item: prove that the existing network protocol can be served from .NET 10 by CoreWCF, with the wire unchanged, before the rest of the server hosting work is built on the assumption that it can. The research the plan asked for is **done and recorded below** — every question the plan ticket raised has an answer with its source. Nothing here is left for the implementer to decide; the work is to build the proof, run it, and write it up.

The verdict is **feasible**, with one server-side source change that is invisible on the wire: the two service classes the proof hosts must throw CoreWCF's `FaultException<T>` and carry CoreWCF's `[ServiceBehavior]` rather than `System.ServiceModel`'s. In each file that is a one-line change to a `using` directive.

## What the protocol is today

This is the specification; it is what must still be true afterwards. `DataphorServiceUtility.DefaultBuildBinding` in `src/protocol/Common/Contracts/DataphorServiceUtility.cs` builds one binding in code:

- `CustomBinding(BinaryMessageEncodingBindingElement, TcpTransportBindingElement)` — binary message encoding over TCP, in that element order.
- On the encoder, `ReaderQuotas.MaxArrayLength` and `ReaderQuotas.MaxStringContentLength` both `2147483647` (the constant `DataphorServiceUtility.MaxMessageLength`).
- On the transport, `MaxBufferSize` and `MaxReceivedMessageSize` both `2147483647`. `TransferMode` stays at its default (buffered) and no security element is configured, so the channel is unsecured and messages cross whole.
- `SendTimeout` and `ReceiveTimeout` both `TimeSpan.MaxValue`.

Three services, addresses built by the same file; ports 8060 and 8061 are a compatibility surface.

| Service | Address | Contract | Instancing today |
|---|---|---|---|
| Main | `net.tcp://<machine>:<port>/<instance>/service`, default port 8061 | `IDataphorService`, 88 operations | one singleton object handed to the host (`new ServiceHost(_service)`) |
| Native CLI | `net.tcp://<machine>:<port>/<instance>/service/native` | `INativeCLIService`, 11 operations | singleton object |
| Listener | `net.tcp://<machine>:8060/listener/service` | `IListenerService` | by type (`new ServiceHost(typeof(ListenerService))`), so per session |

All three contracts are `[ServiceContract(Name = "...", Namespace = "http://dataphor.org/dataphor/3.0/")]` and almost every operation carries `[FaultContract(typeof(DataphorFault))]` or its sibling fault type. Confirmed by inspection: no contract anywhere in `src/engine/Server/Contracts` or `src/protocol` sets `ProtectionLevel`, `SessionMode` or `[ServiceKnownType]`; no operation is one-way; no operation takes or returns a `Stream` or a `Message` (stream support is handle-based over `byte[]`, `IDataphorService.cs` from about line 833). The client declares the same three contracts separately as `[OperationContract(AsyncPattern = true)]` `BeginX`/`EndX` pairs under the same contract name and namespace.

## The answers

Researched against **CoreWCF 1.9.1** (published 2026-06-16; supports .NET Framework 4.6.2 and above, .NET 8, 9 and 10) and **System.ServiceModel.\* 10.1.0**, which `Directory.Packages.props` already pins. Sources are file paths in the `CoreWCF/CoreWCF` and `dotnet/wcf` repositories at `main`, read directly.

**A custom binding of binary encoding over TCP, with those quotas and limits: yes.** `CoreWCF.Channels.CustomBinding` has the same `params BindingElement[]` constructor (`src/CoreWCF.Primitives/src/CoreWCF/Channels/CustomBinding.cs`). `CoreWCF.Channels.BinaryMessageEncodingBindingElement` exposes a settable `ReaderQuotas` the same way. `CoreWCF.Channels.TcpTransportBindingElement` derives from `ConnectionOrientedTransportBindingElement`, which carries `MaxBufferSize` and `TransferMode`, and from `TransportBindingElement`, which carries `MaxReceivedMessageSize` as a `long`. CoreWCF's NetTcp listener explicitly supports a `CustomBinding` containing a `ConnectionOrientedTransportBindingElement`: `NetMessageFramingConnectionHandler.BuildAddressTable` (`src/CoreWCF.NetFramingBase/src/CoreWCF/Channels/Framing/NetMessageFramingConnectionHandler.cs`) looks for exactly that element and builds a service dispatcher from the custom binding.

**A 2 GB maximum message size: yes, and nothing else caps it lower.** `TransportBindingElement.MaxReceivedMessageSize` validates only `value <= 0`; `ConnectionOrientedTransportBindingElement.MaxBufferSize` likewise. `BuildHandshakeDelegateForDispatcher` in the same framing handler reads both off the transport element and sets them on each connection, so the configured values are what the receive path enforces — there is no separate framing-layer or Kestrel-side ceiling. Kestrel's HTTP limits are not in this path: CoreWCF's NetTcp runs as a Kestrel *connection handler*, not an HTTP pipeline. The one thing left at its default is `MaxBufferPoolSize` (512 KB), which only bounds pooling; the host opening without error is the check that `BufferManager.CreateBufferManager(512 KB, 2147483647)` is accepted.

**`InstanceContextMode.Single` with a supplied instance, and `ConcurrencyMode.Multiple`: yes, with the same meaning — but only through CoreWCF's own attribute.** `CoreWCF.ServiceBehaviorAttribute` has `InstanceContextMode` (default `PerSession`, as in WCF), `ConcurrencyMode` and `UseSynchronizationContext`, and applies all three to the `DispatchRuntime` in `ApplyDispatchBehavior`. `ServiceDescription.SetupSingleton<TService>(ServiceDescription, IServiceProvider)` resolves `TService` from dependency injection and makes it the well-known singleton when the behaviour says `Single`; so registering the pre-built instance with `services.AddSingleton(instance)` is how `new ServiceHost(_service)` is spelled now.

**The `[ServiceBehavior]` the server already carries does not count; the contract attributes do.** CoreWCF converts `System.ServiceModel` attributes to its own in `CustomAttributeProvider.GetDualCustomAttributes` (`src/CoreWCF.Primitives/src/CoreWCF/Description/CustomAttributeProvider.cs`), and the set it converts is exactly `ServiceContractAttribute`, `OperationContractAttribute`, `FaultContractAttribute`, `MessageContractAttribute`, `MessageHeaderAttribute`, `MessageHeaderArrayAttribute`, `MessagePropertyAttribute`, `MessageBodyMemberAttribute`, `MessageParameterAttribute`, `XmlSerializerFormatAttribute` and `ServiceKnownTypeAttribute`. `ServiceBehaviorAttribute` is **not** in that set, and `ServiceDescription.EnsureBehaviorAttribute` looks only for `CoreWCF.ServiceBehaviorAttribute`. So **the three contract interfaces need no change at all** — `Name`, `Namespace` and every `[FaultContract]` are read as they stand, which is what keeps the action URIs identical — while the service *implementation* classes need CoreWCF's behaviour attribute.

**A typed fault reaches the existing client as the same typed fault — once the service throws CoreWCF's `FaultException<T>`.** `ErrorBehavior.InitializeFault` tests `error is FaultException` against `CoreWCF.FaultException`, and `FaultFormatter.Serialize` walks the exception's base chain looking for `CoreWCF.FaultException<>`. A `System.ServiceModel.FaultException<DataphorFault>` is not in that hierarchy at all, so it would be serialized as a generic unhandled-exception fault, the client's `catch (FaultException<DataphorFault>)` would miss it, and every engine error would degrade into `ServerException.Codes.CommunicationFailure`. `CoreWCF.FaultException<TDetail>` has the same `(TDetail detail, string reason)` constructor the services use, so the fix is the `using` swap and no change to any of the throw sites. The detail type is still `DataphorFault` — one `[DataContract]` class in `src/engine/DAE/Contracts/DataphorFault.cs`, serialized by the data-contract serializer on both sides — so the wire is unchanged.

**Infinite send and receive timeouts: honoured.** `CoreWCF.Channels.Binding`'s timeout setters reject values above `TimeoutHelper.MaxWait`, but `TimeoutHelper.IsTooLarge` is `(timeout > MaxWait) && (timeout != TimeSpan.MaxValue)` — `TimeSpan.MaxValue` is the infinite case and is accepted. `ServerSessionConnectionReaderMiddleware` copies `ReceiveTimeout` and `SendTimeout` straight off the binding onto the session's transport settings, so infinity flows through. The one other idle timer in the stack, `ConnectionPoolSettings.IdleTimeout` (2 minutes) together with `ChannelInitializationTimeout` (30 seconds), applies in `ConnectionReuseHandler.ReuseConnectionAsync`, which runs only for a connection whose *session has already ended* and which is waiting to be reused; it does not reach an open session. A connection therefore stays open between calls, which is what `port-abandoned-session-cleanup` assumes.

**The client's asynchronous contract binds with no edits.** In `dotnet/wcf`, `TypeLoader.ProcessOpMethod` sets `isAsync` from `ServiceReflector.IsBegin(opAttr, methodInfo)` and records `BeginMethod`/`EndMethod` on the operation, and `ServiceChannelProxy` dispatches `MethodType.BeginService` and `MethodType.EndService` through `InvokeBeginService`/`InvokeEndService`. The action is derived from `ServiceReflector.GetLogicalName(methodInfo, isAsync, isTask)`, which strips the `Begin` prefix, so `BeginGetServerName` sends the same action as the server's `GetServerName`. That is the property the whole wire-compatibility claim rests on, and the thing the proof test checks empirically rather than by reading code. `NativeCLIClient`'s contract (`src/protocol/NativeCLI/IClientNativeCLIService.cs`) is structurally identical — same contract name and namespace pattern, every operation an `AsyncPattern = true` `Begin`/`End` pair with a `[FaultContract(typeof(NativeCLIFault))]` — so it is the same case and the same mechanism; `protocol-corewcf-host` hosts it.

**Packages.** `CoreWCF.NetTcp` 1.9.1 for the host, `CoreWCF.Primitives` 1.9.1 for the fault and behaviour types. Both target `netstandard2.0` only. `CoreWCF.Primitives` pulls 32 packages and `CoreWCF.NetTcp` 44, including `Microsoft.AspNetCore.*` 2.3.10 (Hosting, Kestrel, Kestrel.Transport.Sockets, Http, DataProtection, Authorization), `Microsoft.Extensions.*` 8.x, `Microsoft.IdentityModel.Tokens` and `.Saml` 8.19.1, `System.DirectoryServices`, `System.Security.Cryptography.Xml`, `System.Text.Json` 8.0.6 and `System.Web.Services.Description`. A project that also carries `<FrameworkReference Include="Microsoft.AspNetCore.App" />` gets the in-box ASP.NET Core 10 assemblies instead of the 2.3.10 package ones, through NuGet's package-override mechanism; that framework reference is what the proof's host project needs. This adds no second hosting model: there is one `IWebHost`, which `host-generic-host` later replaces with the Generic Host.

## What to build

### Service classes back into the server's compile list

`src/engine/Server/Server.csproj`: add `<PackageReference Include="CoreWCF.Primitives" />` and bring **two** of the six excluded files into the `Compile` list — `Service\DataphorService.cs` and `Service\ListenerService.cs`. Leave `Service\DataphorServiceHost.cs`, `Service\ListenerServiceHost.cs`, `Service\CustomServiceHost.cs` and `NativeCLI\NativeCLIService.cs` out: they are `protocol-corewcf-host`'s, and only what the proof actually hosts should come back. Update the XML comments so the remaining ones still name the right files, and extend the `System.ServiceModel.Primitives` comment — that package stays, because the contract interfaces in `Contracts\` use it — to say that `CoreWCF.Primitives` is what the service classes throw and are attributed with.

`src/engine/Server/Service/DataphorService.cs`:

- Replace `using System.ServiceModel;` with `using CoreWCF;`. That one line turns all 89 `FaultException<DataphorFault>` throws and the `[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]` attribute into CoreWCF's, with identical meaning.
- Delete the parameterless constructor and the then-unused `using System.Configuration;`. That constructor reads configuration section `instance` through `ConfigurationManager` — the site of known defect K12 — and is reachable only by hosting the type by name from an application configuration file, which is `UseServiceConfiguration`, which WS5 removes. Nothing in the repository constructs it: every call site is `new DataphorService(_remoteServer)`. Leaving it in would also mean adding the `System.Configuration.ConfigurationManager` package to keep dead code compiling.

`src/engine/Server/Service/ListenerService.cs`: the same `using` swap, and delete `using System.Configuration;` and `using System.ServiceModel.Channels;`, neither of which the file uses. `System.Configuration` in particular has no namespace in the shared framework, so an unused directive for it is a compile error once the file is compiled.

Let the compiler find any other `System.ServiceModel` residue in those two files; an inventory of both found only `FaultException<>` and `ServiceBehavior`.

`ContractParityTest` is unaffected: it compares the contract *interfaces*, which this ticket does not touch, and reads their `System.ServiceModel` attribute data, which is still what they carry.

### The proof host

A new `tests/DAE.Server.Tests/CoreWcfProofHost.cs`: a small `IDisposable` that takes a started `Alphora.Dataphor.DAE.Server.Server`, hosts `IDataphorService` and `IListenerService`, and exposes the two port numbers it ended up listening on. Put it in the test project, not in `src/engine/Server`: `CoreWCF.NetTcp` and the ASP.NET Core framework reference would otherwise land in the server assembly's dependency closure and from there, through `DAE.Client`'s reference to that assembly, in the WinForms client and the IDE. Whether that is acceptable is a real question, and it is `protocol-corewcf-host`'s to answer when it brings `DataphorServiceHost` back; this ticket should not pre-empt it. Write the class to be moved, not rewritten.

Shape:

- Build the CoreWCF binding from the same numbers as the client's: `new CoreWCF.Channels.CustomBinding(binaryEncoder, tcpTransport)` with `ReaderQuotas.MaxArrayLength`, `ReaderQuotas.MaxStringContentLength`, `MaxBufferSize` and `MaxReceivedMessageSize` all set from `DataphorServiceUtility.MaxMessageLength`, and `SendTimeout` and `ReceiveTimeout` both `TimeSpan.MaxValue`. Reference the constant; never re-spell `2147483647`. The two bindings are necessarily separate objects of two unrelated types, so a comment at the site should say that the client's copy in `DataphorServiceUtility.DefaultBuildBinding` is the original and this one must track it.
- One `IWebHost` (`new WebHostBuilder()`), `.UseKestrel()` for the `IServer` implementation, and `.UseNetTcp(Action<NetTcpOptions>)` with **two** `Listen` calls: one for the services' port and one for the listener's, matching today's two-port shape. Capture each `TcpListenOptions` from the `configure` callback.
- Listen on `net.tcp://127.0.0.1:0/` and read the real port from `TcpListenOptions.IPEndpoint.Port` after the host has started. Register the service endpoints with **relative** addresses (`"<instance>/service"`, `"listener/service"`) so that the base address CoreWCF adds on `IServiceBuilder.Opening` — `NetTcpFramingOptionsSetup.UpdateServiceBuilderBaseAddresses` turns the bound socket into `net.tcp://127.0.0.1:<real port>/` — supplies the port. If that ordering turns out not to hold, the symptom is the client failing to find an endpoint because the address table was built before the base address was known; fall back then to pre-picking a free port with a throwaway `TcpListener(IPAddress.Loopback, 0)` and registering absolute addresses. Say in the write-up which route was used.
- `services.AddServiceModelServices()`, `services.AddSingleton(dataphorServiceInstance)` so CoreWCF adopts the pre-built singleton, and `app.UseServiceModel(builder => ...)` with `AddService<DataphorService>()`, `AddService<ListenerService>()` and an `AddServiceEndpoint<TService, TContract>` for each. The listener keeps today's by-type hosting: no instance in dependency injection, so CoreWCF constructs it, and because its `[ServiceBehavior]` sets only `ConcurrencyMode` and `UseSynchronizationContext` the instancing stays `PerSession` exactly as in WCF.
- Keep the host quiet and self-contained: clear the logging providers, suppress the hosting status messages, set the content root to `AppContext.BaseDirectory`, and set the server-URLs setting to empty so `UseKestrel` does not also open a default HTTP port.
- Dispose stops the host first, then disposes the service, then the `RemoteServer`, in that order; the engine `Server` itself belongs to the caller's `TestServerInstance`.

### The proof test

A new `tests/DAE.Server.Tests/CoreWcfProtocolTest.cs`. Do not mark it `[Parallelizable]`: two servers in one process share engine static state, as the note on `TestServerInstance` records. Build on `TestServerInstance` for a server in a fresh temporary directory with a SQLite catalog, start it, wrap it in `RemoteServer`, construct `DataphorService`, and host it with `CoreWcfProofHost`.

The client side is the shipped client code, assembled the way the real client assembles it in `src/client/DAE.Client/ServerConnection.cs`: a `ClientServer("localhost", <instance name>, port, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default)` wrapped in `new LocalServer(clientServer, false, "localhost")`, which is an `IServer`. `DataphorServiceUtility.GetBinding()` is left untouched and no channel is hand-rolled. Host name does not have to match between the two sides: `HostNameComparisonMode` defaults to `StrongWildcard`, so the address table ignores it.

Cases, each named for what it proves:

- **A whole session over the wire.** Connect, start a process, prepare and execute a statement, open a cursor and fetch rows, allocate a stream through `IServerProcess` (which is an `IStreamManager`), write to it, read it back, and shut down cleanly. Keep the D4 to built-in operators and types over the memory device: anything that makes the client-side engine miss a class alias sends `LocalServer.ClassLoaderMissed` down the library-download path, which writes assemblies into `<test output>\LocalBin\<server name>`.
- **A message well above the 64 KB default, in both directions.** A value of a few megabytes sent as a statement parameter and read back in a cursor fetch or an `Evaluate`. A few megabytes clears every relevant default at once: `MaxReceivedMessageSize` 65536, `ReaderQuotas.MaxStringContentLength` 8192 and `MaxArrayLength` 16384. Prefer a parameter over a multi-megabyte D4 literal, so the case exercises the transport rather than the lexer. Note in the write-up that `LocalServer`'s catalog fetch at connect time already crosses 64 KB, so the quotas are in play before this case runs.
- **A deliberate server-side error arriving as the typed fault.** Execute D4 that fails in the engine and assert that the exception the caller sees is the engine's own exception type with its error code — which can only happen if `FaultException<DataphorFault>` reached the client and `DataphorFaultUtility.FaultToException` ran. Assert as well that no bare `CommunicationException` and no untyped `FaultException` escapes: that is precisely the shape an unrecognised fault would take, because `ClientServer`'s `catch (CommunicationException)` would turn it into `ServerException.Codes.CommunicationFailure`.
- **The listener contract answers on its own port.** Call `ListenerFactory.EnumerateInstances("localhost", <listener port>)` and assert it round-trips, and call `ListenerFactory.GetInstanceURI` for an instance name that is certainly not configured and assert the typed `ListenerFault` surfaces as the exception the client builds from it. Assert nothing about the contents of the instance list: `ListenerService` reads the machine-wide `ServerConfigurations.config`, so the list depends on what is installed on the build machine, and `InstanceManager.GetInstanceDirectory` creates `%ProgramData%\Alphora\Dataphor\Instances` if it is absent. Never depend on owning port 8060 or 8061.

Teardown must stop the host and dispose the instance even when an assertion fails, or the temporary instance directory stays locked and the next run inherits it.

### Project and package files

`Directory.Packages.props`: `CoreWCF.NetTcp` and `CoreWCF.Primitives`, both `1.9.1`, in the existing alphabetical list. `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`: `CoreWCF.NetTcp`, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, and the two new files in the `Compile` list, each with a comment saying why. The framework reference means running the suite now needs the ASP.NET Core shared runtime as well as the .NET one; that is part of any .NET 10 SDK install, so CI is unaffected, but say so in the write-up. `tools/layout/Layout.csproj` needs no change — the server's new package files simply appear in the layout's `bin/`, which the layout mirrors — but check that the layout still builds.

Warnings stay visible. `Directory.Build.props` does not treat warnings as errors, so a NuGet audit warning out of the new dependency closure will not break the build; if one appears, report it in the handoff and do not suppress it. Do not start honouring `RequireSecureConnection`, `RequireSecureListenerConnection` or `AllowSilverlightClients`: the proof host takes no such parameters and `protocol-transport-security` owns them.

### The write-up

A new subsection at the end of the WS5 section of `docs/modernization-plan.md`, before `## WS6` — not a loose file. It carries the verdict; the answers above, each with its source and the package version, trimmed to what a reader of the plan needs; which route the dynamic port took; and the three things it hands on.

Those three, stated plainly because `protocol-corewcf-host` depends on them:

- **Nested addresses on one port are unproven.** The native CLI endpoint `.../service/native` is a path *below* the main endpoint `.../service`. CoreWCF dispatches on `connection.Via` through a `UriPrefixTable` registered per endpoint base address (`NetMessageFramingConnectionHandler.BuildAddressTable`), which should resolve the longer registration first, but the proof hosts two sibling addresses on two ports and does not test the nested case. It is the first thing `protocol-corewcf-host` should check.
- **The binding is now declared twice**, once as `System.ServiceModel.Channels.CustomBinding` for the client and once as `CoreWCF.Channels.CustomBinding` for the host, because the two stacks have unrelated `Binding` types and `DataphorServiceUtility.GetBinding()` can only produce the client's. Keeping them in step needs either a shared settings source or a test; `protocol-corewcf-host` should settle which.
- **The server assembly's dependency closure now includes CoreWCF.Primitives**, and will include ASP.NET Core once the host classes come back to `src/engine/Server`. `DAE.Client` references the server assembly, so the WinForms client and the IDE inherit whatever lands there. Splitting a hosting assembly out is the alternative; it is a design call for `protocol-corewcf-host`, with WS6's `ui-client-layering` as the other interested party.

Also in `docs/modernization-plan.md`: mark the `protocol-corewcf-proof` row of the WS5 work-item table **Done** with a pointer to the new subsection, in the style the `protocol-contract-parity-test` row already uses; and update known defect K12 to record that its code site, `DataphorService`'s configuration-reading constructor, is gone, with the `src/hosts/DAEService/app.config` half left to `host-generic-host`.

`docs/migration-notes.md`: the interim entry that lists all six missing WCF classes must still read true, so narrow it to the four that are still absent. Add an entry for `DataphorService`'s removed parameterless constructor — an extension or a configuration file that hosted the type by name no longer can, and the remedy is to host through the server host with an instance — and one for the service classes now throwing CoreWCF faults, which a custom C# extension that catches `System.ServiceModel.FaultException<DataphorFault>` *on the server side* would notice. Client-side catches are unaffected, which is the whole point.

## Edge cases and interactions

Each of these must be handled, and the reviewer will look for it. Where a case is settled by inspection rather than by a test, that is said so.

- **A `System.ServiceModel.FaultException` escaping to CoreWCF.** The failure is silent and total: every engine error degrades to a communication failure. Verified by the typed-fault case above, which is the single most important assertion in this ticket.
- **An unconverted `[ServiceBehavior]`.** If the CoreWCF attribute is not in place, instancing silently falls back to `PerSession`, each session gets its own `DataphorService`, and handles minted on one connection are unknown on the next. The whole-session case would still pass, so this needs its own check: assert that the instance answering calls is the one the test constructed, by reading back through the client something only that object could know (for example a value the test sets on the server before the host opens). Verified by assertion, not by inspection.
- **Nested versus sibling endpoint addresses** — handed on, above; the proof uses siblings on two ports.
- **Port 0 and the ordering of base-address discovery** — primary route and fallback are specified above. Whichever is used, the test must never bind 8060 or 8061 and never depend on a machine-wide instance.
- **Two bindings, one specification.** Covered by building the host's binding from `DataphorServiceUtility.MaxMessageLength` and by the comment at the site; a test for the drift is handed to `protocol-corewcf-host`.
- **`MaxBufferPoolSize` at its default against a 2 GB `MaxBufferSize`.** Verified by the host opening at all: `BuildHandshakeDelegateForDispatcher` creates the buffer manager from both at open time.
- **Infinite timeouts.** Verified by inspection, with the citation in the write-up. An infinite timeout cannot be demonstrated by a test that is allowed to finish, and nothing here should add a sleep to the suite; say that plainly rather than implying the test proves it.
- **The listener touching machine state.** `InstanceManager.GetInstanceDirectory` creates `%ProgramData%\Alphora\Dataphor\Instances`; `LoadConfiguration` reads the file there if it exists and writes nothing. The listener case must therefore assert only round-tripping and the typed fault. Verified by how those assertions are written.
- **Client-side library download.** `LocalServer.ClassLoaderMissed` writes downloaded assemblies into `<bin>\LocalBin\<server name>` and calls `Assembly.LoadFrom`. Kept out of reach by the D4 the test uses; verified by inspection of what that D4 touches.
- **Shutdown order.** Host stopped before the service and `RemoteServer` are disposed, or an in-flight call reaches a disposed server; and teardown runs on the failure path too, or the temporary instance directory is left locked. Verified by the teardown code.
- **Two servers in one process.** No `[Parallelizable]` on the fixture.
- **Suite shape.** The new fixture is not `CoverageScripts`, so it runs in the first of the three part commands in the root `AGENTS.md`. It should stay well under a second or two; if the host takes noticeably long to open, say how long in the handoff.

## Build and test

Build, then run the three part commands under *Agents run the suite in parts* in the root `AGENTS.md`, each as its own foreground command. While iterating:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~CoreWcfProtocolTest" --logger "console;verbosity=normal"
```

A failure in a subsystem this ticket does not touch is handled by the pre-existing-failure protocol in the ticket rules; do not skip or loosen a test to get a green run.

## TODO

- Add `CoreWCF.NetTcp` and `CoreWCF.Primitives` 1.9.1 to `Directory.Packages.props`.
- `Server.csproj`: reference `CoreWCF.Primitives`, bring `Service\DataphorService.cs` and `Service\ListenerService.cs` into the compile list, correct the surrounding comments.
- `DataphorService.cs`: `using System.ServiceModel;` becomes `using CoreWCF;`; delete the parameterless constructor and `using System.Configuration;`.
- `ListenerService.cs`: the same `using` swap; delete the unused `using System.Configuration;` and `using System.ServiceModel.Channels;`.
- Write `tests/DAE.Server.Tests/CoreWcfProofHost.cs` as specified, with the binding settings taken from `DataphorServiceUtility.MaxMessageLength`.
- Write `tests/DAE.Server.Tests/CoreWcfProtocolTest.cs` with the four cases and the singleton-identity assertion; add both files and the package and framework references to the test project.
- Check that the runtime layout still builds and that the server's new package files land in `bin/`.
- Run the targeted test, then the full suite in its three parts.
- Append the write-up to WS5 in `docs/modernization-plan.md`, mark the work-item row Done, and update K12.
- Update `docs/migration-notes.md`: narrow the interim missing-classes entry to four, add the removed constructor and the server-side fault type.
