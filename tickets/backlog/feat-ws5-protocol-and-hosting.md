description: Make the .NET 10 server reachable over the network again, with the existing client protocol, as a console program or a Windows service.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
tradeoffs: The whole workstream hinges on CoreWCF reproducing the existing binding; if the proof fails the owner must choose a transport change, so planning past the proof before it lands may be wasted.
----
## What this is

An umbrella for the rest of WS5 (client/server protocol and hosting) plus the two WS2 items that need a hosted server. It is not worked directly: when promoted to `plan/`, the planning agent replaces it with one ticket per work item below, then deletes this file. Its slug retires at that point; other umbrellas that name it in `prereq:` only mean "plan after WS5 has been planned".

**Promote when** gate G1 holds (from a clean clone, `dotnet build` and `dotnet test` pass on .NET 10 with every D4 coverage script accounted for) — in practice when `build-server-project` and `test-harness-inprocess` have landed.

## Work items

Slugs, scope, prerequisites and difficulty are in the plan's WS5 and WS2 tables; use those slugs.

- `protocol-corewcf-proof` — first, and alone. If the existing binding (binary encoding over TCP, singleton instancing, 2 GB messages, the asynchronous client patterns) cannot be reproduced on CoreWCF, the ticket stops and goes to `blocked/` with the options for the owner. No other item here is cut as `implement/` until the proof has landed.
- `protocol-corewcf-host`, `protocol-contract-parity-test`, `host-generic-host`, `host-instance-configuration` (fixes K12), `protocol-transport-security`.
- `port-abandoned-session-cleanup` and `port-server-on-net10` (from WS2; both need the CoreWCF host).
- `test-client-server` (WS3 layer L4: hosted server in a child process, real WCF client, dynamically allocated ports).

Found while cutting Phase 1 (static, not run): the default instances directory, `%ProgramData%\Alphora\Dataphor\Instances`, does not include the version, so a 4.0 server installed beside 3.1 (as the plan's WS9 target and Q9 expect) would read and write 3.1's instance configuration file. `host-instance-configuration` must settle this, for example with a version-specific default directory or by refusing a 3.x configuration file. The plan does not decide which, so if the choice has a visible cost, take it to `blocked/` with a proposal.

Open question Q8 (is `UseServiceConfiguration` or secured transport used in production?) governs `protocol-transport-security` and the removal of `UseServiceConfiguration`. If unanswered when reached, file that item to `blocked/` with the plan's default (not used; plain TCP inside the LAN) as the proposal.

## Cutting rules

- Re-read the WS5 section and the current code first; the tree will have changed since the plan was written.
- One ticket per work item; *hard* items go to `plan/`, concrete ones to `implement/`, per the plan's "Guidance for cutting tickets".
- `prereq:` names only slugs that are on the board or in `tickets/.pruned-tickets.jsonl`. If an item's prerequisite belongs to a workstream that has not been cut yet, leave that item out and park it as a new `backlog/` ticket (`feat-ws5-protocol-and-hosting-later`) listing the leftovers and what they wait for.
- WCF contracts in `src/engine/Server` and `src/protocol/Client` (two copies) and ports 8060/8061 are compatibility surfaces (D4).
