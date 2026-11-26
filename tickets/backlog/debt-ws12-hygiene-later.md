description: The last four repository clean-up jobs, each of which has to wait for other work to finish first: deleting source files nothing compiles, deleting the old build files, replacing the last checked-in third-party binaries, and throwing away the temporary harness that proved the code runs on the new runtime.
architecture: docs/modernization-plan.md#ws12-repository-hygiene
tradeoffs: Nothing here changes behaviour, and every item gets cheaper the longer it waits, so a maintainer may reasonably let all four trail the release; the cost of waiting is that the tree keeps describing a build that no longer exists.
----
## What this is

The remainder of the repository hygiene umbrella after its five ready items were planned and filed (`hygiene-silverlight-conditionals`, `hygiene-retire-web-silverlight-client-types`, `hygiene-remove-rest-leftovers`, `hygiene-remove-dead-compiled-files`, `hygiene-scrub-inherited-identifiers`). Each item below is blocked on work that is not on the board yet, so none of them can be planned honestly today.

**Promote when** the work each item names has landed. The first three wait on tickets that do not exist yet because their workstreams are still held as backlog umbrellas (`feat-ws6-windows-client-and-ide`, `feat-ws9-installer-and-release`); promote this ticket once those umbrellas have been planned and the named slugs are on the board. The fourth is different and is explained under its own heading.

When this ticket is promoted, the planning agent should again file one ticket per item whose blocker has cleared and leave the rest here.

## Work items

### `build-globbing-cleanup` (WS1)

Delete the source files no project compiles, then switch projects to compiling whatever is beside them instead of naming each file. Needs `ui-dataphoria-port`.

What is left for it, from the 2026-11-21 audit that diffed every compile list in `Dataphor.slnx` against the `.cs` files beside it, and from that audit's review:

- Eight files in `src/engine/DAE` that `DAE.csproj` does not compile: `Runtime.Data/Index.cs`, `Runtime.Data/Scan.cs`, `Runtime.Data/TableBuffer.cs`, `Runtime.Data/TestValues.cs`, `Runtime.Data/UnimplementedTableValues.cs`, `Runtime.Instructions/GeneratorNodes.cs`, `Runtime.Instructions/TestNodes.cs` and `Runtime/DataVar.cs`.
- `src/protocol/Client/Server/ServerFactory.cs`, which `Client.csproj` does list but whose whole body sits inside a block comment, so it contributes nothing.
- Two non-source strays in `libraries/Frontend/Source`: `app.config`, which names the old .NET Framework version and which nothing reads, and `EveryNode.dil`, a form-document fragment nothing in the tree references.

Those three projects, 23 files, were the whole of it across the solution's 26 projects; every other project's compile list matched its folder exactly. Two library projects also excluded source files when the audit ran, and both sets have since been deleted — the connection library's six files and a resource file by `device-connection-cleanup`, and the Frontend server library's nine `System.Web` files with the folder they emptied by `library-frontend-web-leftovers` — so each of those projects can switch to default globbing with no deletion first.

**Delete a remaining set before switching its own project to globbing, not after.** `Runtime.Instructions/GeneratorNodes.cs` still has a `using` for a remoting namespace that does not exist on .NET 10, so globbing would pick up a file that cannot compile.

### `build-remove-legacy-build` (WS1)

Delete the remaining legacy solutions and `packages.config` files once every live project is converted. Needs `ui-dataphoria-port` and `installer-msi`. The root `Dataphor.sln` is the main one: it no longer builds, because its .NET Framework projects reference the converted ones, and `AGENTS.md` names this item as its owner.

This item no longer covers the Visual Studio 2003-format copy of the ODBC connection project; `device-connection-cleanup` deleted that in the change that converted the project it was a copy of, which is where it belonged.

### `hygiene-vendored-binaries`

Final sweep over `external/`: either it is empty, or each remaining file has a recorded reason. Needs `ui-editor-and-docking` and `installer-msi`. Decision D11 is the policy — a checked-in third-party binary is replaced by a package as each consumer is converted — and this item is the audit that closes it out.

### `hygiene-remove-net-spike`

Delete `tools/net-spike/`, the throwaway harness that proved the code builds and runs on .NET 10 without changing any repository source, and update the places that point at it: `AGENTS.md` (the tools table and the *Build and test* section), the plan's Appendix A and the plan's WS1 notes.

**Its two named prerequisites are complete, and it still is not ready.** `test-harness-inprocess` and `store-sqlite-concurrency` both landed, so the coverage scripts and the store concurrency check no longer need the spike. But the spike's own README states the real gate — delete this folder when every project has an SDK-style build — and that is not true yet. Nine of its 32 projects are still in legacy format and are in no in-repository solution: the two data-aware and WinForms client assemblies, the WPF scheduler control, the IDE, the tray configuration tool, the command-line runner and the client provider among them. For those, a wrapper build under the spike is the only compile check that exists, and it is the check two other tickets rely on (the Silverlight conditional sweep uses it for the two client projects it edits). Deleting the spike before they are converted would remove the only evidence that they still compile.

So this item needs the WS6 client and IDE conversions, not the two tickets originally named: `ui-dataphoria-port`, `ui-windows-client-port` and `ui-config-utility`, which between them cover every project still in legacy format (the command-line runner and the client provider come with `ui-client-layering`, which `ui-windows-client-port` already waits on). The plan's WS12 table now records that prerequisite list. Re-derive it from `tools/net-spike/projects.txt` against `Dataphor.slnx` when this comes up for planning rather than trusting the count above.

## Cutting rules, when this is planned

- `prereq:` only on slugs that are on the board. Most of these items suit a lower-capability agent and can carry `difficulty: easy`; `build-globbing-cleanup` is the one with real judgement in it, because deleting a file that a project does not compile is only safe once nothing is going to want it.
- Dead `#define` switches and unused `using` directives are still cleaned only in files otherwise being edited, never as a sweep.
