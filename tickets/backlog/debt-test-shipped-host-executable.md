description: Nothing in the automated tests ever launches the server program the product actually ships, so a build whose server executable will not start would still look green; the one thing standing in the way is that the program has no way to be told not to write to the Windows event log.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/hosts/Host/DataphorHost.cs, src/engine/Server.Hosting/DataphorServiceHost.cs, src/engine/Server/Server/Server.cs, tests/DAE.Server.Tests/HostProgramTest.cs, tests/DAE.Server.Tests/OutOfProcessInstance.cs
tradeoffs: The program's start-and-stop behaviour is already pinned in process and its dependency file is already checked, so the only thing a case would add is that the published executable launches — which the build and one hand measurement have already shown once — and buying it costs a new way to configure the shipped host.
----
## What this is about

The product ships a server program. The test suite never starts it.

What the suite does cover: `tests/DAE.Server.Tests/HostProgramTest.cs` builds the same program's host **in the test's own process** and checks that starting it starts a server, that stopping it stops one, that a server which fails to start fails the host rather than leaving a process serving nothing, and that each executable's dependency file names the catalog-store assembly it loads by name. The out-of-process layer (`test-client-server-fixture` and the three tickets after it) starts a child server, but a small console program of the suite's own rather than the shipped one.

What nothing covers: that `artifacts/layout/<configuration>/bin/DAEServer.exe` — the file an installer picks up — actually launches, reads its instance from the machine's list, serves, and stops. That was measured once by hand while `host-generic-host` landed and recorded in `docs/modernization-plan.md` under *Generic host*; it is not re-measured by anything.

## Why it was left out, and what it needs first

One reason, and it is not a small one. A hosted server logs unless its host turns logging off, and when the process is elevated a logging server **creates the Windows event-log source `Dataphor Server` and writes to the Windows event log** (`src/engine/Server/Server/Server.cs`, in `StartLog` and `LogMessage`, both gated on an administrator check). Continuous-integration runners are elevated. A test must not create machine state, which is why every fixture in this suite sets `DataphorServiceHost.LoggingEnabled = false`, and why `AGENTS.md` records the event-log source as the price of the suite's `DATAPHOR_TEST_LOGGING=1` diagnosis switch.

The shipped program has no way to be told the same thing:

- not a command-line switch — `HostCommandLine` is the command line 3.1 took and a compatibility surface, and `host-generic-host` recorded it as not worth widening;
- not an instance setting — `ServerConfiguration` carries neither `LoggingEnabled` nor `HostName`, which `host-instance-directory` recorded as the reason `DataphorHost.BuildHost` keeps its factory parameter;
- not an environment variable — there is none for it today, although there is a precedent for exactly this kind of diagnostic facility in `DATAPHOR_INSTANCE_DIRECTORY`, which `host-instance-directory` added for the same sort of reason.

So this work item is two things in one: decide how the shipped host is told to run without logging (an environment variable is the obvious candidate, and the precedent exists; whoever takes it should weigh it against the alternative of pre-creating nothing and simply refusing to run the case when elevated), and then add the case.

A second, smaller thing the person doing this should know: the shipped host's listening address defaults to the machine name, which makes it bind every interface in both address families — deliberately, and measured; see the plan under *CoreWCF hosting*. With no `HostName` control either, a case that starts it opens a port other machines can reach for the duration. That is a dynamically chosen high port for a few seconds and the suite already makes the same exception once, for the linked-server case in `test-client-server`, so it is a cost to state rather than a blocker.

## What the case would then establish

Start `artifacts/layout/<configuration>/bin/DAEServer.exe -n <throwaway instance>` with `DATAPHOR_INSTANCE_DIRECTORY` pointing at a private folder holding an instance list the test wrote (instance on a reserved free port, `ShouldListen` false, library directory pointing at a copy of the layout's `Libraries`, instance directory under a temporary folder), connect once over the wire with the shipped client stack, run one statement, and stop it. The machinery for all of that exists: `tests/DAE.Server.Tests/OutOfProcessInstance.cs` already writes the private instance list, reserves the port, drains the child's output and kills the process tree, and `ProtocolClient` is the client.

Two facts it would pin that nothing else does: the published executable launches on a clean build with its dependency and runtime-configuration files as the layout wrote them, and the instance-list path through `DataphorServiceHost.Start()` works from outside the test process. The second is only partly new — the out-of-process layer's own child server already takes that path.

Stopping it is its own question, and the answer is not obvious: the shipped host is stopped by a console break, and closing its redirected input pipe no longer stops it (recorded in the plan under *CoreWCF hosting*). Sending a console control event to a child means putting it in its own process group, where the event Windows then delivers is not the one the .NET console lifetime listens for. Terminating the process is the fallback and is honest for a temporary instance — the catalog store survives an abrupt stop by design — but it means the case shows the program starting and serving, not shutting down cleanly. A case that wants the clean shutdown too has to settle that mechanism first.
