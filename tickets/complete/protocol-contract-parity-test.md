description: The server and the client each declare the same three network interfaces in their own source file, and nothing checked that the two copies still agree; a test now compares them and fails when they drift apart.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: tests/DAE.Server.Tests/ContractParityTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, AGENTS.md, docs/architecture.md, docs/modernization-plan.md
----
## What landed

One new fixture, `tests/DAE.Server.Tests/ContractParityTest.cs` (439 lines), with three tests — one per contract pair. No production source is edited, so there is no `docs/migration-notes.md` entry.

| Test | Server declaration (assembly) | Client declaration (assembly) | Operations compared |
|---|---|---|---|
| `DataphorServiceDeclarationsMatch` | `IDataphorService` (`AlphoraDataphorServer`) | `IClientDataphorService` (`AlphoraDataphorClient`) | 88 |
| `NativeCLIServiceDeclarationsMatch` | `INativeCLIService` (`AlphoraDataphorServer`) | `IClientNativeCLIService` (`Alphora.Dataphor.DAE.NativeCLI`) | 11 |
| `ListenerServiceDeclarationsMatch` | `IListenerService` (`AlphoraDataphorServer`) | `IClientListenerService` (`AlphoraDataphorCommon`) | 3 |

Each test reports every difference it finds in one failure message rather than stopping at the first, and compares:

- **Contract identity** — the effective `Name` and `Namespace` of the `[ServiceContract]`. "Effective" means the attribute's value where it sets one and the value WCF would use otherwise (the interface's own name; `http://tempuri.org/` for the namespace), so dropping either argument is itself a difference rather than a comparison of two nulls.
- **The operation set**, in both directions separately, because an operation the client is missing and one the server is missing are fixed on opposite sides.
- **The signature** — parameter types position by position, and the return type, compared by type identity and never by name. A reported mismatch names each type's assembly, because two same-named types from different assemblies are one of the drifts the test exists to catch.
- **The message exchange** — the `Action`, `ReplyAction` and `IsOneWay` properties of the `[OperationContract]`. Added in review; see the findings below.
- **The declared faults** — the set of `[FaultContract]` detail types, also by identity.

A client operation is read as an `[OperationContract(AsyncPattern = true)]` method named `Begin<Name>` paired with an `End<Name>`; its wire parameters are Begin's without the trailing `AsyncCallback` and `object`, and its return type is End's. The pairing and the one-operation-per-name assumption are checked rather than relied on: a `Begin` with no `End`, an unattributed method that is not the `End` half of a pair, an `End` half that does not take exactly `(IAsyncResult)`, an operation declared without `AsyncPattern`, an `AsyncPattern` method not named `Begin…`, a `Begin` not ending in `(AsyncCallback, object)`, and two operations resolving to one wire name all produce their own message.

Two project references were added to `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` (`Client.csproj` and `DAE.NativeCLI.csproj`) and the new file to its `Compile` list. `AlphoraDataphorCommon`, which holds `IClientListenerService`, arrives transitively.

The implement stage is commit `ticket(implement): protocol-contract-parity-test`. Review changed four things in that work, listed below.

## The mismatch the test found

The three pairs do not fully agree, contrary to the implement ticket's expectation. Operation sets, parameter types and return types match exactly on all three pairs. Fault declarations do not: `IDataphorService.Start`, `Stop` and `GetState` each carry `[FaultContract(typeof(DataphorFault))]` on the server and nothing on the client, while all 85 other operations agree. The server raises `FaultException<DataphorFault>` from all three (`src/engine/Server/Service/DataphorService.cs`, lines 85-119) and the client catches `FaultException<DataphorFault>` around all three (`src/protocol/Client/Client/ClientServer.cs`, lines 359-405) — a catch that cannot fire, because WCF only deserializes a fault into the typed `FaultException<T>` when the operation declares that detail type.

The ticket forbade editing either declaration to make the test green, so the three differences are recorded in the fixture's `KnownFaultDifferences` list — the same idiom the repository already uses for a known difference in `coverage-manifest.json` and `ParserRoundTripCorpusTest`'s allow list — and the defect is filed as `debt-client-contract-missing-fault-declarations`, named in the comment above the list. An entry is matched only by its exact server-only and client-only fault sets, and an entry whose difference is no longer there fails the test with a message saying to remove it, so the fix cannot land without draining its entry.

## How to exercise it

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ContractParityTest" --logger "console;verbosity=normal"
```

Three tests, under a second, no server and no socket.

## Tests

| Test | What it verifies |
|---|---|
| `ContractParityTest.DataphorServiceDeclarationsMatch` | `IDataphorService` and `IClientDataphorService` declare the same contract identity, the same 88 operations, the same signature and message exchange for each, and the same faults apart from the three recorded in `KnownFaultDifferences`. |
| `ContractParityTest.NativeCLIServiceDeclarationsMatch` | The same for `INativeCLIService` / `IClientNativeCLIService`, 11 operations, no recorded differences. |
| `ContractParityTest.ListenerServiceDeclarationsMatch` | The same for `IListenerService` / `IClientListenerService`, 3 operations, no recorded differences. |

Review added no test and cut none. The three tests are the contract; a fourth test of the comparator's own reading would be a test of a test, and the comparator's branches were instead re-exercised by temporary edits that were reverted (below). The fixture carries a `NOTE:` saying they have no committed test.

## Review findings

**Checked by reading and running.** The implement diff was read before the handoff summary. Then: every one of the six interface declarations against what the fixture claims about them; the `KnownFaultDifferences` mechanism, run both ways; the two statements in the handoff that turned out to be wrong; the uncompared properties of `[ServiceContract]` and `[OperationContract]`, against what each of the six interfaces actually sets; the three documentation files that describe the two contract copies; and the whole test suite in its three parts.

**Minor — fixed in this pass.**

- **The comparison ignored the `[OperationContract]` properties that decide the action URIs and the message exchange: `Action`, `ReplyAction` and `IsOneWay`.** `Action` overrides outright the URI the fixture's own doc comment says it is guarding, and a one-way operation carries no reply message, so a side that disagrees waits for a reply the other never sends. None of the six interfaces sets any of them today, which is why the implement stage did not see them, but setting one on a single copy compiles on both sides and mismatches only on the wire — the exact failure the fixture exists to catch. Now compared in `CompareMessageExchange`. Confirmed by temporarily putting `Action = "urn:drift", IsOneWay = true` on `IDataphorService.Start`: both differences were reported with their intended messages, and the file was then restored byte for byte.
- **Three documents still described the old reality.** The implement diff touched no documentation. `docs/modernization-plan.md`'s WS5 work-item row for `protocol-contract-parity-test` now carries the `**Done**` note the plan's other finished items carry, saying what is and is not compared. `AGENTS.md`'s compatibility-surfaces bullet and `docs/architecture.md`'s hosting section both located every client copy in `src/protocol/Client/Contracts`; the listener's copy is in `src/protocol/Common/Contracts` and the native CLI's in `src/protocol/NativeCLI`. Both now name all three locations and name the test that holds the copies in step; `AGENTS.md` also says that a fix to the recorded fault difference has to drain its entries.

**Major — none.** No finding reached the filing bar: the operation sets and signatures agree, the reading of the asynchronous pattern is right on all 102 operations, and the one real defect found was already filed by the implement stage.

**Re-weighed and left as it is: the decision to record the three fault differences rather than leave the test red.** This was the implement stage's main judgement call and the first thing reviewed. The alternative — a red `DataphorServiceDeclarationsMatch` in the tree and in CI until the filed ticket lands — conflicts with the requirement that the suite pass, and recording is what this repository already does with a known failure. The recording is not a silent allowance: with the client's three missing `[FaultContract]` attributes temporarily added, the test failed with three messages naming the stale entries and telling the reader to remove them (run, not inferred; the file was restored byte for byte afterwards). Fixing it inline was not open to this pass either — the client contract is a named compatibility surface, and the fix needs a `docs/migration-notes.md` entry and a hosted test that the tree cannot run yet.

**Tripwires parked (conditional; not tickets).** Both at their site in `ContractParityTest.cs`:

- The `NOTE:` on `CompareOperations`, which the implement stage put there, is extended: what is *not* compared is now the complete list — `[ServiceKnownType]`, base contract interfaces, and the `ProtectionLevel` settings on `[ServiceContract]` and `[OperationContract]` — with `protocol-transport-security` named as the work item most likely to need the protection levels compared, since it owns the secured-connection options.
- A new `NOTE:` in the fixture's class remarks: the comparison logic itself has no committed test. Its branches — the Begin/End pairing, the malformed-client reports, the overload detection, the known-difference matching — were each confirmed against purpose-built broken interface pairs that the implement ticket directed be left uncommitted, so a later edit to the reading is unguarded except by the check that the server side yielded at least one operation. The note says to build the synthetic pairs again before reworking it.

**Accepted-tradeoff notes met at a finding's site: none.** No `NOTE:` of that kind exists in any file this change touches.

**Two statements in the implement handoff were wrong** and are corrected here so they are not propagated: the three pairs do not all agree (the fault declarations, above), and the six interfaces are not all in the namespace `Alphora.Dataphor.DAE.Contracts` — `IClientNativeCLIService` is in `Alphora.Dataphor.DAE.NativeCLI`. Neither affects the test, which keys on types rather than namespaces.

**Considered and not changed.**

- *The `DAE.NativeCLI.csproj` project reference is redundant* — the server reference already brings that assembly in transitively. It stays explicit because the test project now compiles against it, with a comment in the csproj saying so.
- *The explicit `[OperationContract(Name = …)]` path is read but unexercised*, since no operation on any of the six interfaces sets it. It is one `??` on each side; covered by inspection only, as the implement ticket expected.
- *A declaration test cannot see the wire.* That the action URI really is derived from contract namespace, contract name and the `Begin`-stripped operation name is WCF's rule, assumed here; `protocol-corewcf-proof` already has confirming it against CoreWCF on its list. The fixture says so in its own remarks.
- *Fixture placement.* It is in `tests/DAE.Server.Tests` as the ticket directed, and runs in the first suite part. `test-client-server` may move it later; nothing here pre-empts that.

## Verification done

**Full build, clean.** `dotnet build Dataphor.slnx`: 0 errors, no new warning from the fixture.

**The whole suite, in its three parts, each in the foreground** (the part commands in the root `AGENTS.md`), all after the temporary contract edits were restored:

| Part | Result |
|---|---|
| Everything but the coverage scripts | `DAE.Server.Tests` 388 passed, 0 failed, 0 skipped (3 m 4 s); `Common.Tests` 13 passed |
| Coverage scripts A-R | 67 passed, 11 skipped, 0 failed (3 m 16 s) |
| Coverage scripts outside A-R | 48 passed, 14 skipped, 0 failed (3 m 26 s) |

The skips are the manifest's known-failure, unstable and fragment entries, which is what a green run looks like. No pre-existing failure surfaced, so nothing was written to `tickets/.pre-existing-error.md`.

**Both temporary drift experiments were reverted from a byte-for-byte copy** rather than by a git command, and `git status` confirmed each file unchanged before the suite ran.

## Board note

`debt-client-contract-missing-fault-declarations` was promoted from `backlog/` to `fix/` by the board owner while this review was running, with a `prereq: protocol-corewcf-host` added — it needs a hosted server to verify its fix. That move and the matching `tickets/.garden-report.md` edit were in the working tree when this run finished and were left untouched.
