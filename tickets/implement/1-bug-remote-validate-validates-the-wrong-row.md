description: When a form running outside the server asks the server to check the row a user has just edited, the server checks the row as it was before the edit instead — so a bad value is accepted, a value that was already bad is reported against, and the user's edit can be silently replaced by the old one.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Server/RemoteServerCursor.cs, src/protocol/Client/Server/LocalCursor.cs, src/engine/DAE/Server/ServerCursor.cs, src/engine/DAE/Contracts/RemoteCLI.cs, tests/DAE.Server.Tests/
difficulty: easy
repro: verified
----
## What is wrong

While a user edits a row, the client asks the server three questions about it: what should this column default to, what follows from the user having changed this column, and is this row acceptable. The third is `Validate`, and it takes two rows — the row as it was and the row as the user has it now — plus the name of the column the user changed. Everything in the tree passes them in that order: `IServerCursor.Validate(oldRow, newRow, columnName)`, `ServerCursor.Validate`, `TableNode.Validate`, the data-binding layer that calls it when the user leaves a field (`src/client/DAE.Client/View.cs:158`) and before a row is posted (`View.cs:1036`), the client-side cursor that sends it (`LocalCursor.Validate`), the WCF proxy (`ClientCursor.Validate`), the service (`DataphorService.Validate`) and the remote interface that declares it (`RemoteCLI.cs:266`).

One method swaps them. `RemoteServerCursor.Validate`, the server's entry point for the call, is declared `Validate(RemoteRowBody oldRow, RemoteRowBody newRow, string column, ProcessCallInfo callInfo)` and its body reads:

```csharp
return InternalValidate(newRow, oldRow, column);
```

`InternalValidate` then builds the row it validates from its *second* argument, so the row that gets checked is the one the client sent as "before the edit", and the row sent back to the client is that same old row. Its two siblings in the same file, `Default` and `Change`, pass their arguments straight through and are correct.

Three consequences, all of them wrong in a different direction:

- The value the user just typed is never checked, so a row the server will refuse on saving is reported as acceptable when the user leaves the field. This is the same lost early report as `bug-remote-form-is-never-told-about-a-type-change`, reached by a different route.
- The row as it was *is* checked, so a check that the stored row already fails is reported against the user as though they had just typed it — on a field they may not have touched.
- When the call reports that it changed something (which it does whenever a validate handler modifies the row), the client writes the returned row over the row the user is editing. The returned row is the old one, so the user's edit disappears from the form with no error.

## What it does today, measured

Measured 2026-11-23 against an in-process server with the shipped client stack in front of it (a `LocalServer` over a `RemoteServer` over the engine — no port, no WCF; `RemoteServerCursor` is the same object on either transport).

The remote path is only reached when a validation is **not** remotable, because otherwise the client answers the question itself without a round trip. So the table carries the tag that denies the shortcut, on the column, which is where `Compiler.CompileTableVarColumn` reads it — on the constraint it is metadata of the constraint and leaves the column's own flag alone:

```
create table SwapTbl { ID : Integer, Amount : Integer tags { DAE.IsValidateRemotable = "false" }, key { ID } };
alter table SwapTbl { alter column Amount { create constraint IsPositive value > 0 } };
```

| `Validate(oldRow, newRow, "Amount")` | through the client stack | through the engine's own cursor |
|---|---|---|
| old `Amount` 5, new `Amount` -1 | **accepted** | refused: violation of column constraint `IsPositive` |
| old `Amount` -1, new `Amount` 5 | **refused**: violation of column constraint `IsPositive` | accepted |
| old `Amount` 5, new `Amount` 7 | accepted | accepted |

And the lost edit, on a table with a validate handler that sets another column:

```
create table ClobberTbl { ID : Integer, Amount : Integer tags { DAE.IsValidateRemotable = "false" }, Note : String, key { ID } };
create operator ClobberTbl_Validate(const AOldRow : row { ID : Integer, Amount : Integer, Note : String }, var ANewRow : row { ID : Integer, Amount : Integer, Note : String }, const AColumnName : String) : Boolean
  begin result := true; ANewRow.Note := 'seen'; end;
attach operator ClobberTbl_Validate to ClobberTbl on { validate };
```

The client sent the old row as `Amount` 10, `Note` 'old' and the new row as `Amount` 20, `Note` 'new', naming `Amount` as the changed column. The call returned true, and the client's new row then read `Amount=10 Note=seen` — the handler ran, and the user's 20 was replaced by the stored 10.

## Why nobody has met it

Two things hid it. The local shortcut answers the great majority of proposals without reaching the server at all, so this path needs a validation that is not remotable: a column or table variable tagged `DAE.IsValidateRemotable = "false"`, or a constraint or validate handler that is not remotable in itself. And no test in the suite asked any of the three proposal calls through the client stack — every existing proposal test goes straight to the engine, where this method is not involved.

The swap is in the initial open-source commit; the 2019 renaming commit `01f32a20` carried it across unchanged. It is not a consequence of the port.

## What to do

Pass the arguments through, as `Default` and `Change` already do. The row the client sent as the new row is the row to validate and the row to send back.

A shared test fixture is needed, and this ticket is the first of three that want it, so it belongs here: an in-process server with the shipped client stack in front of it, which is what makes `RemoteServerCursor` and `LocalCursor` reachable from a test. The shape that worked:

```csharp
_instance = new TestServerInstance(GetType().Name);
_instance.Server.Start();
_remoteServer = new RemoteServer(_instance.Server);
_localServer = new LocalServer(_remoteServer, false, "localhost", 60);
_clientSession = _localServer.Connect(new SessionInfo("Admin", ""));
_clientProcess = _clientSession.StartProcess(new ProcessInfo(_clientSession.SessionInfo));
```

Tear it down in that order reversed, and give the fixture a second session taken straight from `_instance.Server` as well — the other two tickets need a session that changes the catalog while the client holds a cursor open. This is deliberately *not* `ProtocolClient`, which goes over CoreWCF and needs a hosted instance with a port: the defects in all three tickets are above the transport, and `CoreWcfProtocolTest` covers the transport itself.

### TODO

- Add the shared client-stack fixture to `tests/DAE.Server.Tests` (see the shape above), named for what it is rather than for this bug, and list its file in `DAE.Server.Tests.csproj` — default compile items are off in this repository.
- Fix the argument order in `RemoteServerCursor.Validate`.
- Add the reproduction: one case for the unchecked new row, one for the old row reported against, and one for the edit that is replaced. Each case should assert against the engine's own answer for the same three rows rather than against a transcribed expectation, so that what it pins is "the client stack agrees with the engine".
- Check the siblings while you are in the file: `Default` and `Change` were read and are correct; say so in the handoff rather than adding tests for them.
- Add an entry to `docs/migration-notes.md`: a client that reached this path was being told the wrong answer, and an application that worked around it (for example by re-asserting the edited value after a validate) will now see the call behave as documented.
- Add the record to the known-defects table in `docs/modernization-plan.md`, as K44 or the next free number, and name this slug in it.
- Targeted tests: the new fixture's cases, plus `dotnet test tests/DAE.Server.Tests --filter "Name=CursorTest|FullyQualifiedName~CoreWcfProtocolTest"` for the client and cursor neighbours. Name the filters in the handoff.
