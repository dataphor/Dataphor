description: Check the removal of the leftover compile-time switches for a browser client the product no longer ships.
architecture: docs/modernization-plan.md#ws12-repository-hygiene
files: src/engine/Base/BOP/BOPException.cs, src/engine/Base/BaseException.cs, src/engine/Base/DataphorException.cs, src/engine/Base/ExceptionUtility.cs, src/engine/Base/MemoryUtility.cs, src/engine/Base/ReflectionUtility.cs, src/engine/Base/ThreadUtility.cs, src/engine/Base/TimingUtility.cs, src/engine/DAE/Contracts/DataphorFaultUtility.cs, src/engine/DAE/Debug/Debugger.cs, src/engine/DAE/Runtime.Data/DataValue.cs, src/engine/DAE/Runtime.Instructions/StringNodes.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/SessionInfo.cs, src/protocol/Client/Client/ClientServer.cs, src/protocol/Client/Server/LocalServer.cs, src/protocol/Common/Contracts/DataphorServiceUtility.cs, src/client/DAE.Client/Alias.cs, src/client/DAE.Client/DAEDataSet.cs, src/client/DAE.Client/Design/Editor.cs, src/client/DAE.Client/ServerConnection.cs, src/client/DAE.Client/Session.cs, src/client/DAE.Client/TerminalServices.cs, src/client/Frontend.Client/ActionNode.cs, src/client/Frontend.Client/Pipe.cs, src/client/Frontend.Client/Session.cs, src/engine/Server/clientaccesspolicy.xml, libraries/System.Internet/Documents/Register.d4, libraries/System.Platform/Documents/Register.d4, docs/modernization-plan.md
difficulty: easy
----
## What landed

The 46 conditional-compilation directive lines for the removed browser client are gone from all 26 files, plus the stray policy file and the stale comments. 334 lines deleted, 3 added, across 29 files and one file deletion.

The property the edit had to preserve is that the symbol `SILVERLIGHT` is defined nowhere, so the C# preprocessor was already resolving every site one way. Each site therefore kept exactly the arm the compiler was already taking and lost the directive lines and the other arm. Nothing moved, nothing was re-indented, nothing was rewritten.

**The inventory was regenerated and matches the ticket's two tables exactly** — 46 directive lines; 17 in-solution files carrying 29 of them, 9 client-stack files carrying 17. No difference to report there.

**How the mechanical part was done.** A one-off script walked each file in binary mode, matched only `#if [!]SILVERLIGHT` lines, found the matching `#else`/`#endif` by nesting depth, kept the live arm verbatim and dropped the rest. That is why the nested `NILPROPOGATION`/`USEISTRING` blocks in `StringNodes.cs` and the `LOGFILECACHEEVENTS` blocks inside the kept arm of `LocalServer.cs` came through untouched, and why every file's byte-order-mark state and CRLF endings are unchanged (verified: all 28 surviving edited files are 100% CRLF, and the four that had a byte-order mark still have one). The script lives in the session scratchpad, not in the tree.

**Hand edits on top of the sweep, worth a careful look:**

- `src/protocol/Common/Contracts/DataphorServiceUtility.cs` — the one compound condition, `#if USENAMEDPIPES && !SILVERLIGHT`, became `#if USENAMEDPIPES`. The `#else` arm is untouched and `USENAMEDPIPES` is still a live switch (commented out at the top of the same file, and a comment in `NetTcpServiceHost.cs` depends on it staying one).
- `src/engine/Base/ReflectionUtility.cs` — one `<remarks>` line was deleted: "Under Silverlight, this call must be made on the main thread." It documented a constraint of the arm that is now gone (that arm loaded assemblies through `System.Windows.Deployment` and `Application.GetResourceStream`); the surviving arm just enumerates `AppDomain.CurrentDomain.GetAssemblies()` and has no such constraint. **This line was not named in the ticket** — it was found by the closing grep. Reviewer's call whether deleting a now-false remark belongs in this ticket.
- `src/engine/DAE/Contracts/DataphorFaultUtility.cs` — two comment lines were deleted: "Under Silverlight, a ConnectionException will come back as a DataphorException / The statement is still present in the Details." They sat inside the kept arm and described only what the removed client would see, so with the directive gone they read as a live caveat about a client that no longer exists. **Also not named in the ticket.** If the reviewer judges that the note described something still true of the wire format rather than of the removed client, it should come back reworded rather than verbatim.
- Five whitespace-only leftover lines were collapsed where a deleted directive left two blank lines in a row (`DataphorException.cs` twice, `DataValue.cs` once, `ClientServer.cs` twice). Whole-line deletions only, no re-indentation. A scan comparing blank-line runs against `HEAD` confirms no edited file gained a double blank.
- `src/engine/Server/clientaccesspolicy.xml` deleted (in no project, read by nothing).
- The two D4 `TODO` comments now read "a client-side assembly providing them" instead of naming the removed client, at `libraries/System.Internet/Documents/Register.d4:41` and `libraries/System.Platform/Documents/Register.d4:103`. Each kept its original slightly-broken grammar ("would need to provided") rather than being tidied, to hold the diff to the one phrase.
- `docs/modernization-plan.md` — the WS12 work-item row and the size-table row are both marked done.

## What the closing grep leaves behind

Every remaining mention of the removed client in `src/`, `libraries/`, `tests/` and `tools/` is deliberate. Five are on the ticket's "mentions that stay" list. The **one difference from the ticket** is that two more are not on that list and are not leftovers either:

- `libraries/Frontend/Documents/Register.d4:292` — the `Frontend.ClientTypes` row for the browser client.
- `libraries/Frontend/Documents/Silverlight.NodeTypes.dnt` — the node-type document for it.

Both are owned by a different WS12 ticket, `hygiene-retire-web-silverlight-client-types`, which the plan says removes the client-type rows and the two `.dnt` documents with a migration note. They are published documents and a compatibility surface, so this ticket left them alone. Nothing to file.

No `docs/migration-notes.md` entry is owed: no compiled assembly changed and no public member was gained or lost, because the preprocessor had already chosen every arm. The relevant notes (the client's removal, the instance setting's removal) are already there and already marked done.

## Tests

**No test was added.** The three behavioural sites — the `ASCII` selector and the `ReadASCII`/`WriteASCII` accessors of `System.String` in `StringNodes.cs`, where the dead arm chose UTF-8 and the kept arm chooses ASCII — are already covered by the ASCII section of the `StringLibrary` coverage script, which is what proves the kept arm is the one in force. Everything else is platform plumbing with no behaviour of its own.

What was run, and why:

| Command | Result |
|---|---|
| `dotnet build Dataphor.slnx` | 0 errors, 127 warnings, all pre-existing |
| `dotnet test tests/DAE.Server.Tests --filter "Name=StringLibrary\|Name=Exceptions\|Name=DebugLibrary\|Name=DebugStepping\|Name=DebugBreakpoints"` | 4 passed, 1 skipped — the coverage scripts for the three behavioural sites and for the exception and debugger files touched |
| `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~ClientStackTestFixture\|~CoreWcfProtocolTest\|~OutOfProcessProtocolTest\|~DebuggerSessionTest\|~FormCustomizationTest\|~InstanceConfigurationTest"` | 54 passed — the protocol and client-stack fixtures, plus the two that reach `ReflectionUtility` through BOP deserialization |
| `dotnet test tests/Common.Tests` | 18 passed |
| `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~ParserRoundTripCorpusTest"` | 696 passed, 19 skipped — run because two `.d4` documents were edited; comments are dropped by the parser, as expected |
| `python tools/net-spike/run.py C:/temp/silverlight-sweep --only DAE.Client,Frontend.Client` | 0 distinct errors for both, matching the 2026-11-21 audit baseline. Work directory deleted afterwards. |

The `Exceptions` coverage script was skipped, not failed: `tests/DAE.Server.Tests/coverage-manifest.json` marks it `known-failure` against `test-pre-3-1-scripts-policy`, because the helper library's `create operator TestExceptions` is commented out. Pre-existing and unrelated, and already tracked, so no pre-existing-error report was written. One practical note for whoever runs the spike next: its work-directory argument must use forward slashes, because a backslash path is rejected as "must be outside the repository".

## Known gaps, honestly

- **The nine client-stack files got a compile check and no test.** `src/client/DAE.Client` and `src/client/Frontend.Client` are in no project the solution builds and in no test project's reach; the client stack the suite does exercise is `src/protocol/Client`. For those nine the evidence is the net-spike compile (0 errors) and a site-by-site read, not a behavioural test. That is all that is available today — their conversion is WS6 work.
- **The equivalence argument is by inspection, not by measurement.** Nobody compiled the tree with `SILVERLIGHT` defined before and after; that would not build, which is rather the point. The claim "no compiled assembly changed" rests on the symbol being undefined (confirmed: no project file, props file, targets file, solution file or source `#define` mentions it, measured 2026-11-26) plus a read of each site. The highest-value thing a reviewer can do is re-read the arms in `src/protocol/Client/Server/LocalServer.cs` (the largest deletion, 57 lines), `src/engine/Base/ReflectionUtility.cs` (125 lines, and on BOP's deserialization path) and `src/client/DAE.Client/ServerConnection.cs` (the only file where the two arms declared different members) against `git show HEAD:<path>`.
- **Three comment edits are judgment, not mechanics** — the `ReflectionUtility` remark, the `DataphorFaultUtility` note, and the wording chosen for the two D4 `TODO`s. Those are the places to disagree.
