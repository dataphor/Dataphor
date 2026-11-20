description: When a remote server fails to start, stop, or report its state, the error the client shows will be a bare networking message instead of the server's real explanation, because three operations on the client side of the network interface forgot to declare the error type they can carry.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: src/protocol/Client/Contracts/IClientDataphorService.cs, src/protocol/Client/Client/ClientServer.cs, src/engine/Server/Contracts/IDataphorService.cs, src/engine/Server/Service/DataphorService.cs, tests/DAE.Server.Tests/ContractParityTest.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: Only three of the 88 operations are affected, and they are administrative ones a normal application never calls, so a maintainer could reasonably leave the asymmetry alone rather than touch a declared compatibility surface for it.

## What is wrong

The main service contract is declared twice — synchronously in `src/engine/Server/Contracts/IDataphorService.cs` and as an asynchronous copy in `src/protocol/Client/Contracts/IClientDataphorService.cs`. On three operations the two copies disagree about errors:

| Operation | Server declares | Client declares |
|---|---|---|
| `Start` | `[FaultContract(typeof(DataphorFault))]` | nothing |
| `Stop` | `[FaultContract(typeof(DataphorFault))]` | nothing |
| `GetState` | `[FaultContract(typeof(DataphorFault))]` | nothing |

Every other one of the 88 operations declares the same fault on both sides. The new `tests/DAE.Server.Tests/ContractParityTest.cs` found this; it records the three differences in its `KnownFaultDifferences` list, naming this ticket, so it stays green on them and fails on any other drift.

The consequence is on the client. `DataphorService.Start`, `Stop` and `GetState` (`src/engine/Server/Service/DataphorService.cs`, lines 85-119) each convert a `DataphorException` into `throw new FaultException<DataphorFault>(...)`, and `ClientServer.Start`, `Stop` and the `State` getter (`src/protocol/Client/Client/ClientServer.cs`, lines 359-405) each wrap their call in `catch (FaultException<DataphorFault>)` and translate the detail back into the original Dataphor exception. But WCF only deserializes a fault into `FaultException<TDetail>` when the operation's contract declares that detail type. Without the declaration the client receives a plain `FaultException`, the `catch` never fires, and the caller gets a WCF-level exception carrying the fault reason text instead of the server's error with its code and severity. The three `catch` blocks are therefore dead code as the contract stands.

`repro: static` — read from the two declarations and from the WCF rule, not observed running. Confirming it needs a hosted server, which the build does not have yet (the WCF host classes are out of `src/engine/Server/Server.csproj` until `protocol-corewcf-host`), so the path is dormant rather than reachable today; it becomes reachable the moment a host exists. `protocol-corewcf-proof` already has "a `FaultException<DataphorFault>` raised by the service: does it reach the existing client as the same typed fault?" on its list, and a proof that uses `Start`, `Stop` or `State` for its deliberate error would see this; one that uses any other operation would not.

## What correct looks like

A `DataphorException` raised by the server inside `Start`, `Stop` or `GetState` reaches the client as that same Dataphor exception — same code, severity and message — exactly as it does for the other 85 operations.

The expected shape of the fix is to add `[FaultContract(typeof(DataphorFault))]` to `BeginStart`, `BeginStop` and `BeginGetState` on the client interface, and then remove the three entries from `KnownFaultDifferences` in `ContractParityTest.cs` in the same commit — the test fails on a stale entry, so it cannot be forgotten. Removing the attribute from the server instead would make the two copies agree while leaving the client unable to read an error the server still sends, so it is the wrong direction.

Two things the implementer has to weigh, which is why this is filed rather than fixed inline:

- The client contract file is a named compatibility surface (see the compatibility rules in `tickets/rules/10-dataphor.md`). The operation names, parameter types and action URIs do not change, so the wire for a successful call is untouched; what changes is the fault declaration, which appears in the contract's generated metadata and changes how an error is delivered to the client.
- An application that today catches whatever these three calls throw will start seeing a different exception type from them. That is the point of the fix, but it is a behaviour an application can notice, so it needs a `docs/migration-notes.md` entry in the same commit.

## How it would be verified

Through `protocol-corewcf-proof`'s or `protocol-corewcf-host`'s hosted-server test, not by a declaration test: make the server raise a `DataphorException` from one of the three operations and assert the client gets the Dataphor exception rather than a `FaultException`. Before this ticket there is nowhere in the tree to run that. `ContractParityTest` holds the declarations in step but cannot see what the wire does with them.
