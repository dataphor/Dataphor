description: Delete the leftover compile-time switches for a browser client the product no longer ships, so that the source stops describing a client that is gone.
architecture: docs/modernization-plan.md#ws12-repository-hygiene
files: src/engine/Base/BOP/BOPException.cs, src/engine/Base/BaseException.cs, src/engine/Base/DataphorException.cs, src/engine/Base/ExceptionUtility.cs, src/engine/Base/MemoryUtility.cs, src/engine/Base/ReflectionUtility.cs, src/engine/Base/ThreadUtility.cs, src/engine/Base/TimingUtility.cs, src/engine/DAE/Contracts/DataphorFaultUtility.cs, src/engine/DAE/Debug/Debugger.cs, src/engine/DAE/Runtime.Data/DataValue.cs, src/engine/DAE/Runtime.Instructions/StringNodes.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/SessionInfo.cs, src/protocol/Client/Client/ClientServer.cs, src/protocol/Client/Server/LocalServer.cs, src/protocol/Common/Contracts/DataphorServiceUtility.cs, src/client/DAE.Client/Alias.cs, src/client/DAE.Client/DAEDataSet.cs, src/client/DAE.Client/Design/Editor.cs, src/client/DAE.Client/ServerConnection.cs, src/client/DAE.Client/Session.cs, src/client/DAE.Client/TerminalServices.cs, src/client/Frontend.Client/ActionNode.cs, src/client/Frontend.Client/Pipe.cs, src/client/Frontend.Client/Session.cs, src/engine/Server/clientaccesspolicy.xml, libraries/System.Internet/Documents/Register.d4, libraries/System.Platform/Documents/Register.d4
difficulty: easy
----
## What this is

Release 3.x shipped a browser-hosted Frontend Client built on Silverlight. Release 4.0 does not ship it, and decision D6 removed it: the client projects are gone, its instance setting (`AllowSilverlightClients`) is gone, and the documentation now describes it only as history. What is left is the *conditional compilation* that supported building the shared assemblies twice, once for the desktop framework and once for Silverlight: 46 preprocessor directive lines across 26 files, plus one stray policy file and two stale comments.

**Why this is safe, and what the review should check.** The symbol `SILVERLIGHT` is defined nowhere: not by a project file (no `.csproj`, `.props`, `.targets` or `.slnx` in the repository mentions it) and not by a `#define` in any source file. Measured 2026-11-26. So the C# preprocessor already resolves every one of these sites one way: each `#if SILVERLIGHT` arm is excluded from every build, and each `#if !SILVERLIGHT` arm is included in every build. Removing the directives and the excluded arms therefore cannot change a compiled assembly. That is the property each edit must preserve, and checking it is a site-by-site read: **keep exactly the arm the compiler was already taking, delete the other, delete the directive lines**. No arm changes sides, nothing is rewritten, nothing is reformatted.

Because nothing changes behaviour, nothing here is owed a `docs/migration-notes.md` entry. The relevant entries are already there and already marked done (the removal of the client, and the removal of the instance setting). Nothing public gains or loses a member either: every member the build had, it keeps.

## The inventory

46 directive lines in 26 files. Seventeen of the files are in projects that `Dataphor.slnx` compiles, so `dotnet build Dataphor.slnx` checks them. Nine are in two client projects that are **not** in the solution yet (their conversion is WS6 work), so the only compile check available for them is the net-spike harness; see *How to verify*.

In the solution — 17 files, 29 directives:

| File | Directives |
|---|---|
| `src/engine/Base/BOP/BOPException.cs` | 2 |
| `src/engine/Base/BaseException.cs` | 1 |
| `src/engine/Base/DataphorException.cs` | 1 |
| `src/engine/Base/ExceptionUtility.cs` | 1 |
| `src/engine/Base/MemoryUtility.cs` | 1 |
| `src/engine/Base/ReflectionUtility.cs` | 4 |
| `src/engine/Base/ThreadUtility.cs` | 1 |
| `src/engine/Base/TimingUtility.cs` | 2 |
| `src/engine/DAE/Contracts/DataphorFaultUtility.cs` | 3 |
| `src/engine/DAE/Debug/Debugger.cs` | 1 |
| `src/engine/DAE/Runtime.Data/DataValue.cs` | 1 |
| `src/engine/DAE/Runtime.Instructions/StringNodes.cs` | 3 |
| `src/engine/DAE/Server/Engine.cs` | 1 |
| `src/engine/DAE/SessionInfo.cs` | 2 |
| `src/protocol/Client/Client/ClientServer.cs` | 2 |
| `src/protocol/Client/Server/LocalServer.cs` | 1 |
| `src/protocol/Common/Contracts/DataphorServiceUtility.cs` | 2 |

Not in the solution — 9 files, 17 directives:

| File | Directives |
|---|---|
| `src/client/DAE.Client/Alias.cs` | 1 |
| `src/client/DAE.Client/DAEDataSet.cs` | 1 |
| `src/client/DAE.Client/Design/Editor.cs` | 2 |
| `src/client/DAE.Client/ServerConnection.cs` | 5 |
| `src/client/DAE.Client/Session.cs` | 2 |
| `src/client/DAE.Client/TerminalServices.cs` | 3 |
| `src/client/Frontend.Client/ActionNode.cs` | 1 |
| `src/client/Frontend.Client/Pipe.cs` | 1 |
| `src/client/Frontend.Client/Session.cs` | 1 |

Regenerate the list rather than trusting these tables if the tree has moved on:

```
git grep -n -E "^[[:space:]]*#(if|elif).*SILVERLIGHT" -- 'src/*' 'libraries/*' 'samples/*' 'tests/*' 'tools/*'
```

## Two sites that are not a plain two-arm switch

**`src/protocol/Common/Contracts/DataphorServiceUtility.cs` line 185** is the only compound condition: `#if USENAMEDPIPES && !SILVERLIGHT`. `USENAMEDPIPES` is a real switch — commented out at the top of that same file, and a comment in `src/engine/Server.Hosting/NetTcpServiceHost.cs` depends on it staying a switch — so it stays. The edit is to drop the `&& !SILVERLIGHT` and leave `#if USENAMEDPIPES` with its `#else` arm untouched. Do not collapse this one to its live arm; that would delete a live switch.

**`src/engine/DAE/Runtime.Instructions/StringNodes.cs`** holds the only three sites whose arms differ in *behaviour* rather than in platform plumbing: the `ASCII` selector and the `ReadASCII` and `WriteASCII` accessors of `System.String` choose `UnicodeEncoding.UTF8` in the excluded arm and `UnicodeEncoding.ASCII` in the included one. Keep the ASCII arm. These three are already covered: the ASCII section of `StringLibrary.d4` exercises the selector and both accessors, so that coverage script is what proves the kept arm is the one in force. No new test is owed.

## Three more things that belong to this sweep

- **`src/engine/Server/clientaccesspolicy.xml`** — the Silverlight cross-domain access policy. It is in no project (the server conversion dropped it as a content item) and nothing reads it; the handoff of `build-server-project` says explicitly that deleting it belongs to whatever removes the Silverlight leftovers. Delete it.
- **`libraries/System.Internet/Documents/Register.d4` line 41 and `libraries/System.Platform/Documents/Register.d4` line 103** — two `TODO` comments saying the operators below them are in theory remotable but that a separate Silverlight assembly would have to be provided. Rewrite each to keep its point without naming the removed client: the operators are in theory remotable, but a client-side assembly providing them would be needed. These are D4 comments, and the parser drops comments, so `ParserRoundTripCorpusTest` is unaffected either way.
- **Nothing in `Setup/`.** `Setup/Files.wxs` lists a deployed copy of the policy file, and `Setup/` is deleted whole by the WS9 installer work (the plan says so in both the WS9 table and the layout table). Leave that folder alone.

## Mentions that stay, deliberately

A later reader greps for the name and finds these. They are records, not leftovers, and re-filing them would be noise:

- `docs/reference/error-codes.md` — the row reserving error prefix 207 so that the number is never handed out again. The page explains why.
- `docs/migration-notes.md` — the two `done` rows for the client and for the setting. This is the record of the removal.
- `docs/users-guide/frontend-clients.md` and `docs/users-guide/dataphor-service.md` — the historical notes `docs-retire-removed-client-text` left on purpose.
- `tests/DAE.Server.Tests/InstanceConfigurationTest.cs` — the configuration XML in that fixture carries `allowsilverlightclients` on purpose, to pin that a removed setting in an instance file is ignored rather than rejected. Changing it would delete the test subject.
- `src/windows/Frontend.Client.Windows/Forms/EditInstanceForm.Designer.cs` — a comment explaining the empty band where two removed checkboxes were.
- `src/engine/Base/AssemblyNameUtility.cs` — the `remarks` on that class record why the class exists at all. The class is live and its parsing semantics are not this ticket's business.

## Edge cases & interactions

- **An arm that declares a member the other does not.** `src/client/DAE.Client/ServerConnection.cs` has both shapes: a field pair declared only in the included arm (keep it, drop the directives) and a property declared differently in each arm (keep the included one, delete the excluded one). The built public surface must come out identical, which is verified by inspection since the preprocessor already chose.
- **Nested directives.** `StringNodes.cs` interleaves `NILPROPOGATION`, `USEISTRING` and `SILVERLIGHT` blocks. Delete only the `SILVERLIGHT` directive lines, and count braces and `#endif` lines per edit. A mis-paired `#endif` is a compile error in the solution half and a spike error in the client half, so it cannot pass unnoticed — but it can waste a run.
- **A `using` that only the deleted arm needed.** `src/client/Frontend.Client/ActionNode.cs` aliases `Image` to a Silverlight type in the excluded arm and has `using System.Drawing;` in the included one: keep the `using`, drop the alias. Read each file's `using` block after the edit. A `using` that the edit leaves unused is removed (the file is being edited, so the hygiene rule allows it) and one still needed is kept; the compiler reports neither, so this is a read, not a build result.
- **Formatting.** Several of these files have a UTF-8 byte-order mark and all of them use tabs and CRLF. Delete whole lines, re-indent nothing, and do not let an editor strip a byte-order mark or rewrite line endings. `git diff --stat` should show only the lines you meant to touch.
- **The nine client-stack files get a compile check and no test.** `src/client/DAE.Client` and `src/client/Frontend.Client` are in no test project reach today; the client stack the suite does exercise (`ClientStackTestFixture`) is `src/protocol/Client`, which is in the solution. Say that in the handoff rather than implying those nine are covered.
- **`src/engine/Base` is on every path.** `ReflectionUtility` is what BOP uses to deserialize form documents and instance configuration, so a slip there shows up in `FormCustomizationTest` and `InstanceConfigurationTest` rather than at the build.

## How to verify

Build first; a build error is never left for CI.

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=StringLibrary|Name=Exceptions|Name=DebugLibrary|Name=DebugStepping|Name=DebugBreakpoints"
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ClientStackTestFixture|FullyQualifiedName~CoreWcfProtocolTest|FullyQualifiedName~OutOfProcessProtocolTest|FullyQualifiedName~DebuggerSessionTest|FullyQualifiedName~FormCustomizationTest|FullyQualifiedName~InstanceConfigurationTest"
dotnet test tests/Common.Tests --no-build
```

Then the nine files the solution does not compile, through the spike. It needs Python 3, its work directory must be outside the repository, and `--only` takes project file base names:

```
python tools/net-spike/run.py C:\temp\silverlight-sweep --only DAE.Client,Frontend.Client
```

Expected: 0 errors for both, which is what the 2026-11-21 audit measured for them and what `tools/net-spike/README.md` records. If the spike cannot run on the machine, say so plainly in the handoff and name those two projects as unverified rather than claiming them.

Name the filters you used in the handoff. Do not run the whole suite; CI runs it after the commit.

## TODO

- Regenerate the directive list with the `git grep` above and reconcile it with the two tables; report any difference rather than silently following the tables.
- Edit the 17 in-solution files, one at a time, keeping the arm the preprocessor already took and deleting the directive lines and the other arm. Handle the compound condition in `DataphorServiceUtility.cs` as described.
- Edit the 9 client-stack files the same way, including the `using` alias in `ActionNode.cs`.
- Remove a `using` directive that the edit left unused, in those files only.
- Delete `src/engine/Server/clientaccesspolicy.xml`.
- Rewrite the two D4 `TODO` comments in `libraries/System.Internet/Documents/Register.d4` and `libraries/System.Platform/Documents/Register.d4`.
- Re-run the `git grep` and confirm the only remaining matches are the deliberate ones listed above.
- Mark `hygiene-silverlight-conditionals` done in the WS12 work-item table of `docs/modernization-plan.md`, and update the `46 #if SILVERLIGHT blocks` row of the size table in the same section to say it is done and which ticket did it.
- Build, then run the targeted tests and the spike. Record what you ran, what passed, and anything you could not verify.
