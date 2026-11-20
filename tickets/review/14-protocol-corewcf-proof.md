description: A server running on .NET 10 can still be reached over the network by the client program we already ship; this proves it by standing up the smallest possible host and running real sessions through it.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: Directory.Packages.props, src/engine/Server/Server.csproj, src/engine/Server/Service/DataphorService.cs, src/engine/Server/Service/ListenerService.cs, src/engine/Server/Server/RemoteServer.cs, src/engine/Server/Server/RemoteServerProcess.cs, tests/DAE.Server.Tests/CoreWcfProofHost.cs, tests/DAE.Server.Tests/CoreWcfProtocolTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/modernization-plan.md, docs/migration-notes.md, tickets/backlog/bug-object-id-counter-is-process-wide.md
----
## The verdict

**Feasible, and demonstrated.** `ClientServer` + `LocalServer` — the client code the product ships, on the binding `DataphorServiceUtility.GetBinding()` builds, with no hook touched and no channel hand-rolled — completes real work against a .NET 10 server whose `IDataphorService` and `IListenerService` are hosted by CoreWCF 1.9.1 on that same binding. Six tests, all passing, about a second of test time.

The write-up the ticket asked for is `### CoreWCF proof`, the last subsection of WS5 in `docs/modernization-plan.md` (line 379). It carries the verdict, each researched answer with its source, which claims rest on a test and which on inspection, the dynamic-port decision, and five items handed to `protocol-corewcf-host`.

## What landed

**Production source — four files, all small.**

| File | Change |
|---|---|
| `src/engine/Server/Service/DataphorService.cs` | `using System.ServiceModel;` → `using CoreWCF;`, which converts all 89 `FaultException<DataphorFault>` throws and the `[ServiceBehavior(InstanceContextMode = Single, ConcurrencyMode = Multiple, UseSynchronizationContext = false)]` at once. Parameterless constructor and `using System.Configuration;` deleted. |
| `src/engine/Server/Service/ListenerService.cs` | The same `using` swap; unused `using System.Configuration;` and `using System.ServiceModel.Channels;` deleted. |
| `src/engine/Server/Server.csproj` | `CoreWCF.Primitives` reference; the two service files back in the `Compile` list; the two surrounding comments rewritten to name the right files. |
| `src/engine/Server/Server/RemoteServer.cs` | Constructor `internal` → `public`, with a comment saying why. **Not in the ticket** — see *Judgment calls*. |
| `src/engine/Server/Server/RemoteServerProcess.cs` | One-line defect fix: `Execute` dropped every parameter value. **Not in the ticket** — see *Defects met along the way*. |

The three contract interfaces are untouched, which is what keeps the action URIs identical. `ContractParityTest` is unaffected (it compares the interfaces and reads their `System.ServiceModel` attribute data, which is still what they carry) and still passes.

**Test project — two new files.** `CoreWcfProofHost.cs` is an `IDisposable` that takes a started engine `Server`, wraps it in a `RemoteServer`, builds the `DataphorService`, and hosts both services on two reserved loopback ports; `CoreWcfProtocolTest.cs` drives it. Both are in `tests/DAE.Server.Tests` deliberately, so that `CoreWCF.NetTcp` and the ASP.NET Core framework reference stay out of the server assembly until `protocol-corewcf-host` decides where hosting belongs. The host is written to be moved.

**Packages.** `CoreWCF.NetTcp` and `CoreWCF.Primitives` 1.9.1 in `Directory.Packages.props`; `CoreWCF.NetTcp` and `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in the test project. The framework reference means running the suite needs the ASP.NET Core shared runtime as well as the .NET one — part of every .NET SDK install, so CI is unaffected. **No package-audit warning**: `dotnet restore Dataphor.slnx --force` reports none, and `dotnet build Dataphor.slnx` is 0 errors, 0 warnings.

**Docs.** The WS5 subsection; the `protocol-corewcf-proof` work-item row marked **Done** with a pointer to it; known defect K12 updated (its code site is gone, the `app.config` half left to `host-generic-host`); four `docs/migration-notes.md` entries (the interim missing-classes entry narrowed from six classes to four, the CoreWCF fault and behaviour types, the removed constructor plus the now-public `RemoteServer` constructor, and the parameter fix).

## How to exercise it

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~CoreWcfProtocolTest" --logger "console;verbosity=normal"
```

Six tests, 6.5 s wall clock of which about 1.2 s is the tests themselves; the rest is one `TestServerInstance` starting. It opens two ephemeral loopback ports, never 8060 or 8061, and depends on no machine-wide state.

To see the central assertion fail, change `using CoreWCF;` back to `using System.ServiceModel;` in `DataphorService.cs` — it still compiles — and run `TestServerErrorArrivesAsTypedFault`.

## Tests added

| Test | What it verifies |
|---|---|
| `CoreWcfProtocolTest.TestWholeSessionOverTheWire` | Connect, prepare and execute a statement, execute a second directly, fetch two rows through a cursor, allocate / open / write / read / deallocate a stream through the process's `IStreamManager`, and shut down — all over the wire through the shipped client. |
| `CoreWcfProtocolTest.TestLargeMessageInBothDirections` | A three-megabyte `String` goes in as a statement parameter and comes back through a cursor fetch unchanged (6.3 MB on the wire each way), clearing the 64 KB `MaxReceivedMessageSize`, 8 KB `MaxStringContentLength` and 16 KB `MaxArrayLength` defaults. Also the reproduction for the `RemoteServerProcess.Execute` fix below. |
| `CoreWcfProtocolTest.TestServerErrorArrivesAsTypedFault` | A D4 statement that fails in the engine surfaces to the caller as the engine's own `CompilerException` with code `UnknownIdentifier`, and neither an untyped `FaultException` nor a bare `CommunicationException` escapes. This is the assertion that the CoreWCF fault type matters: with the `System.ServiceModel` one the client's typed catch misses and every engine error degrades into `ServerException.Codes.CommunicationFailure`. |
| `CoreWcfProtocolTest.TestListenerAnswersOnItsOwnPort` | `ListenerFactory.EnumerateInstances` round-trips on the listener's own port, and `GetInstanceURI` for an unconfigured instance name surfaces the typed `ListenerFault` as the exception the client builds from it, carrying the service's message. Asserts nothing about which instances are listed. |
| `CoreWcfProtocolTest.TestOneServiceInstanceServesEveryConnection` | Two client connections get different connection handles, so one `HandleManager` — and therefore one `DataphorService` — served both, which is `InstanceContextMode.Single` doing its job; and both report this fixture's server name, which only the object built over its engine server could know. |
| `CoreWcfProtocolTest.TestListensOnlyOnTheReservedPorts` | `TcpListenOptions.IPEndpoint` matches the port each service was given, and neither is the default listener port. |

No existing test was changed. No test was added for the server-source changes beyond these: the `using` swaps have no behaviour of their own that the six cases do not already exercise, and the removed constructor had no caller.

## Verification done

- `dotnet build Dataphor.slnx` — 0 errors, 0 warnings; no warning from either new file.
- `dotnet restore Dataphor.slnx --force` — no `NU1900`–`NU1904`.
- **The full suite, in the three parts from the root `AGENTS.md`, each in the foreground:** part 1 `DAE.Server.Tests` 394 passed / 0 failed / 0 skipped (3 m 3 s) and `Common.Tests` 13 passed; part 2 67 passed / 11 skipped (3 m 9 s); part 3 48 passed / 14 skipped (3 m 1 s). No failures anywhere, so no `tickets/.pre-existing-error.md` was written.
- `tools/docs-link-check/check_links.py docs` — the 203 errors are the pre-existing missing reference pages (K17); none is in `modernization-plan.md` or `migration-notes.md`.
- `dotnet build tools/layout/Layout.csproj` — succeeds; the server's new package files appear in the layout's `bin/` (see the dependency-closure finding below).

## Judgment calls to re-weigh first

**1. `RemoteServer`'s constructor is now public.** The ticket said to "wrap it in `RemoteServer`" from the test project, which was not possible: the constructor was `internal` to `AlphoraDataphorServer` and `DataphorServiceHost` was its only caller. The alternatives were `InternalsVisibleTo` (needs the strong-name public key hard-coded into a shipped assembly) or moving the proof host into the server assembly (which the ticket forbade, for good reason). Public was chosen because `DataphorService`'s only public constructor already takes a `RemoteServer`, so the type was unusable from outside regardless, and because `protocol-corewcf-host` will need exactly this if hosting moves to its own assembly — which is one of the open questions handed to it. It is additive, and it has a migration-notes entry. If the reviewer prefers `InternalsVisibleTo`, the change is one word plus the attribute and the key.

**2. The Generic Host, not `WebHostBuilder`.** The ticket specified `new WebHostBuilder()` and one `IWebHost`. On .NET 10 those are obsolete (`ASPDEPR004`, `ASPDEPR008`) and produced three warnings in a brand-new file. `new HostBuilder().ConfigureWebHost(…)` configures CoreWCF identically, warns about nothing, and is the host `host-generic-host` is going to anyway; it is still one host and one hosting model, which was the ticket's stated reason for the constraint. The comment at the site says this.

**3. The dynamic port: reserved in advance, not port 0.** The ticket's primary route (base address `net.tcp://127.0.0.1:0/` plus relative endpoint addresses) cannot work here, for two reasons now written at the `ReserveLoopbackPorts` site and in the plan: the address table is built from the endpoint address before Kestrel binds, so the real port is not known in time; and two endpoints with relative addresses would both resolve against the first base address of the scheme, collapsing the product's two ports into one. So the host binds and releases two ephemeral loopback ports and registers absolute addresses on them, and `TestListensOnlyOnTheReservedPorts` checks the bound endpoints. The release-then-bind window is a `NOTE:` at the site. This was reasoned rather than tried both ways; a reviewer who wants the port-0 route attempted empirically has the symptom to look for (the client failing to find an endpoint).

**4. The stream case proves the handle protocol, not a byte round trip on the wire.** `Allocate`, the length probe the first write makes, the close and `Deallocate` are all calls on the wire; the bytes stay in `LocalStreamManager`'s deferred-write cache, because `CoverStream.Flush` is a no-op and no public client API forces a flush (`FlushStreams` is internal to the cursor paths). This is what the shipped client does, so the case is honest about what it covers, and the comment in the test says so. A reviewer wanting the bytes demonstrably crossing should ask for a cursor update with an overflow value, which is the production path that flushes.

## Defects met along the way

**Fixed here, with its reproduction: `RemoteServerProcess.Execute` dropped every parameter value.** It called `RemoteParamsToDataParams(paramsValue.Params)`, which reads only names, types and modifiers, instead of `RemoteParamDataToDataParams(paramsValue)`, which also decodes the values from `paramsValue.Data.Data`. So a parameterized statement run through `IServerProcess.Execute(statement, params)` over a network connection behaved as though every parameter were nil — "Value required for column …" on an insert, at every value size, measured from 16 bytes to 3 MB. The client encoded correctly throughout (verified: 43 physical bytes for a 16-character string, 6,291,467 for the 3 MB one), so this is purely the server-side conversion. `RemoteServerPlan.Execute`, `Evaluate`, the cursor opens and `RemoteServerBatch.Execute` all use the right converter and were never affected, and neither was in-process execution; the two `RemoteParamsToDataParams` calls that remain in `RemoteServerProcess` are *Prepare* calls handed a `RemoteParam[]` with no data, and are correct. A wrong API call with a one-line fix, so it was fixed here as the rules direct, with `TestLargeMessageInBothDirections` as the reproduction and a migration-notes entry. A `NOTE:` beside it records that the write-back re-encodes every parameter and not only the var and out ones, which `RemoteServerPlan.Execute` has always done too — fine now, worth narrowing if large input parameters ever show up as slow.

**Filed, because the fix is a design change: `tickets/backlog/bug-object-id-counter-is-process-wide.md`.** `Schema.Object`'s next-object-id counter is a process-wide `static int` and `Engine.InitializeCatalog` resets it to zero. `LocalServer` starts an engine of its own for the client-side catalog cache, so a process that runs an in-process server and then connects to it over the network hands the server identifiers its catalog already holds; the first `create table` in the proof failed with `UNIQUE constraint failed: DAEObjects.ID`. Reachable in a shipped configuration (the IDE with an embedded alias and a connection alias open at once), not reachable out of process, which is why it has survived. `severity: corruption`, `likelihood: unusual`. The proper fix threads an allocator through about fifteen call sites in the compiler and the plan nodes, so it is not a local fix; the proof's client helper works around it in three lines that hand the counter back, with a comment pointing at the ticket. Nothing open claimed the site (checked `tickets/backlog|fix|plan|implement|review`).

## Review findings

- **Tripwire parked** at `RemoteServerProcess.Execute` (`NOTE:`): the parameter write-back re-encodes input parameters too, so a large input parameter is now echoed back and discarded; narrow both this and `RemoteServerPlan.Execute` if that ever shows up as slow.
- **Tripwire parked** at `CoreWcfProofHost.ReserveLoopbackPorts` (`NOTE:`): another process could take a reserved port between the release and CoreWCF's bind.
- **Ticket filed:** `bug-object-id-counter-is-process-wide`, as above.
- **Measured and handed on, in the plan:** a client that connects by a host name resolving to IPv6 first pays **2.046 s per channel open** against a listener bound to IPv4 only, versus 0.016 s for the literal (isolated from Dataphor entirely with a bare socket test on this machine). Today's `ServiceHost` binds the IPv6 wildcard dual-stack and does not have this problem, and real clients connect by host name, so `protocol-corewcf-host` must get the listen address right. It is also why the test's client uses `127.0.0.1` rather than `localhost` — which means **the host-name wildcard path is not exercised** by any test here.
- **Measured and handed on, in the plan:** `CoreWCF.Primitives` adds **31 packages** to `AlphoraDataphorServer`'s runtime closure that nothing else there needed — eight `Microsoft.AspNetCore.*` **2.3.10**, twelve `Microsoft.Extensions.*`, five `Microsoft.IdentityModel.*` (including SAML), `System.DirectoryServices`, `System.Web.Services.Description` and others — and they are in the layout's `bin/` now. `DAE.Client` references the server assembly, so the WinForms client and the IDE inherit them. Nothing runs from `bin/` until `host-generic-host`, so this costs only disk today, but shipping ASP.NET Core 2.3.10 assemblies beside .NET 10 is the kind of thing `security-dependency-audit` will object to. A separate hosting assembly is the obvious answer and is already on `protocol-corewcf-host`'s list.

## Gaps and things to look at

- **Nested addresses on one port are untested.** The native CLI endpoint `.../service/native` sits below the main endpoint `.../service`; this proof hosts two *sibling* addresses on two ports. CoreWCF's `UriPrefixTable` should resolve the longer registration first, but that is inference. First thing for `protocol-corewcf-host`.
- **Infinite timeouts are established by inspection only**, with the citations in the plan, because a test that is allowed to finish cannot demonstrate an infinite timeout and nothing here should add a sleep to the suite. The plan says this plainly rather than implying otherwise.
- **The native CLI service is not hosted.** Only the two services the ticket named are, so `NativeCLIService` and `INativeCLIService` are still out of the server's compile list. Its client contract is structurally identical to the other two (same contract-name and namespace pattern, every operation an `AsyncPattern = true` `Begin`/`End` pair with a `[FaultContract]`), so it is the same case and the same mechanism — but it is not proven.
- **The binding is declared twice** and only the four size limits are shared (both copies read `DataphorServiceUtility.MaxMessageLength`). The element order and the two timeouts can still drift; a comment at the host's `BuildBinding` says the client's copy is the original. A test that compares the two is handed to `protocol-corewcf-host`.
- **`CoreWcfProofHost.Dispose` gives Kestrel a 10-second bounded stop** rather than waiting indefinitely, because a client channel may still be open and the binding's receive timeout is infinite. If a test ever leaves a channel open at teardown, that becomes a 10-second pause rather than a hang. No test does today; all six close their clients.
- **`using System.Collections;` stays in `DataphorService.cs`.** It looks dead next to the deleted constructor but is not: `BitArray` in the `Insert` and `Update` operations needs it (confirmed by removing it — four compile errors).
- **The fixture is not `[Parallelizable]`**, because two servers in one process share engine static state; `bug-object-id-counter-is-process-wide` is a second instance of that class, after the catalog emitter (K1) and the keyword tables (K25). Worth noting on the `TestServerInstance` comment that claims no other unsafe static was found — not changed here.
- **`tickets/.garden-report.md`** was modified in the working tree during this run by something other than this ticket and was left alone.
