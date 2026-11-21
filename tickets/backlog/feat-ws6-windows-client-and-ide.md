description: Make the Windows client and the Dataphoria IDE actually run on .NET 10, looking and behaving as before, by replacing the old menu and toolbar controls, run-time script compilation and outdated third-party components.
architecture: docs/modernization-plan.md#ws6-windows-client-and-dataphoria
tradeoffs: This is the largest workstream and its long tail (designers, painting, resources) is hard to estimate; a maintainer may want the server side fully proven first even though the layering and control work does not depend on it.
----
## What this is

An umbrella for WS6. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then.

**Promote when** gate G2 holds (a hosted .NET 10 server passes the client/server tests). The owner may promote it earlier: `ui-client-layering` and `ui-controls-port` need only the core build.

## Work items

Scope, prerequisites and difficulty per the plan's WS6 issue table and work-item table.

- `ui-client-layering`, `ui-controls-port`, `ui-frontend-client-port`, `ui-scriptaction-roslyn` (not optional: applications use script actions heavily, Q6; fixes K8), `ui-windows-client-port`, `ui-windows-client-runs` (gate G3), `ui-editor-and-docking`, `ui-dataphoria-port`, `ui-dataphoria-designers`, `ui-config-utility`.
- `test-ui-smoke` (from WS3, layer L6).
- Known defects fixed here: K5 (Dataphoria's `ScriptExecutor`), K7, K8, K9, K24.

Rules that matter here: `WFDEV006` stays a visible warning until a project's legacy controls are replaced, then becomes an error in that project; never suppress it. Form node base classes and interfaces stay source-compatible for custom extensions, and every change to them goes into `docs/migration-notes.md` (D16). Start in the same DPI mode as today.

Found in `build-server-project` (verified by compiling it in the spike): `DAE.Client` does not compile against the converted server, because `src/client/DAE.Client/ServerConnection.cs` creates a `DataphorServiceHost` for a non-embedded in-process alias, and that class is out of the server's compile list until `protocol-corewcf-host`. The embedded in-process alias (which creates a `Server` directly) does not need it. Every project that compiles `DAE.Client` (the provider, the controls, the Frontend client, the Windows client and Dataphoria) stops at this one error, so `ui-client-layering` meets it first: it either waits for `protocol-corewcf-host` or keeps the non-embedded path out of compilation the same way.

## Cutting rules

Re-read the WS6 section and the current code; one ticket per work item; *hard* items to `plan/`; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`; items waiting on an uncut umbrella are parked as a new backlog ticket `feat-ws6-windows-client-and-ide-later`.

## Client tests deleted with `tests/DAE.Client.Tests`, not replaced (from `test-client-server-cursors`)

The old out-of-process client test project is gone: it registered an instance in the machine's instance list, started `DAEServer.exe` from a hard-coded path under the installation directory, used the default port, and depended on helper classes that no longer exist. Its cursor coverage was rewritten against the converted client protocol assembly. Two of its three test classes could not be, because the projects they test are still unconverted and belong here, so what they intended is recorded for whichever WS6 ticket picks it up (`ui-client-layering` for the first, and the provider work for the second).

- **`Frontend/DataViewTest`, four cases, against `src/client/DAE.Client` (`DataSession`/`DataView`) and `src/windows/DAE.Client.Controls`.** `TestDataView`: open a `DataView` over a browse expression with application transactions off, move to the first row, edit a column, refresh, then insert a row, post it, assert the posted row is current, delete it, and assert the view is not then empty. `TestAttachedDataView`: the same edit, insert and post with a `DataSource` and a `DBGrid` on a shown WinForms form, pumping messages between steps, then delete the row through D4 on the session and refresh the view, asserting the view shows the remaining row — that is, that a control bound to a view survives each of those transitions. `TestDeleteAtEOF`: delete the last row of a browse view and assert the view is not empty afterwards. `TestCancelOfEmpty`: start an insert on a view over an empty table, set both columns, and cancel.
- **`Provider/ConnectionTest.OpenConnection`, against `src/client/DAE.Client.Provider`.** Open a `DAEConnection` from a connection string, create a command, and run `select Libraries group add { Count() LibCount }` as a scalar. As written it also depended on a connection alias existing in the machine's alias list, which a replacement must not do: the alias list is machine state, under the same per-version folder as the instance list, and the test plan's isolation requirement rules it out. A replacement should connect by host name, instance name and port against a child-process server, the way `tests/DAE.Server.Tests`' out-of-process fixture does.

Both are data-binding and provider behaviour rather than wire behaviour, so neither needs to be out of process to be worth having; what they need is the projects to compile. The machinery is in place either way — `tests/DAE.Server.Tests/OutOfProcessInstance.cs` starts a child server on a private instance list and a free loopback port, and `ProtocolClient.cs` assembles the shipped client stack against it.
