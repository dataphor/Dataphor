description: When a rule is attached to a data type that says what to do as soon as a user changes a value of that type, the rule never runs for anyone working outside the server — the user's screen shows the value they typed, and the rule only takes effect when the row is saved.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Server/ServerProcess.cs, tests/DAE.Server.Tests/RemoteEventHandlerCacheTest.cs
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: Fixing it moves work from the client to the server — a column of such a type would send every keystroke-level proposal to the server where today it answers locally — so a maintainer may prefer to leave the behaviour and write down that a type-level change handler is a server-side rule only, especially since nothing is ever stored wrong.
----
## What goes wrong

While a user edits a row, the client asks three questions about it: what should this column default to, what follows from the user having changed this column, is this row acceptable. To save a round trip on the many rows where the answer is "nothing", the server tells the client once, when a query is prepared, which of the three the client may answer out of its own copy of the schema.

An event handler never runs on a client. It is not remotable (`Schema.EventHandler.DetermineRemotable` sets `IsRemotable = false` unless the compiler constant `TRIGGERSREMOTABLE` is defined, and nothing defines it), so handlers are deliberately left out of the copy of the schema a client is sent — `Schema.TableVar.IncludeHandlers` filters on exactly that. The consequence is that whenever a handler exists, the server must tell the client "ask me", or the handler simply does not run for that user.

For a handler attached to a table variable or to one of its columns, that works. For a handler attached to a **data type**, it does not: the column keeps saying its change proposal is answerable on the client, the client answers it, and the handler never runs. The user sees the value they typed; the handler's effect appears only when the row is saved and the server does the work itself. Nothing is stored wrong — what is lost is the immediate effect, which is the whole point of a change handler.

A `validate` or `default` handler on the same type does *not* have this problem; the two sibling functions that compute those flags read the type's handlers correctly.

## Measured

2026-11-23, against an in-process server with the shipped client stack in front of it (a `LocalServer` over a `RemoteServer` over the engine — `tests/DAE.Server.Tests/ClientStackTestFixture.cs`), in the most favourable arrangement there is: the handler attached to the type *before* any table of that type existed, and the client's very first query over that table.

```
create type ZDT like Integer;
create operator ZDT_Change(var AValue : ZDT) : Boolean begin result := true; AValue := 99; end;
attach operator ZDT_Change to ZDT on { change };
create table ZDTbl { ID : Integer, Amount : ZDT, key { ID } };
```

Asked what follows from the user changing `Amount` from 1 to 2, through a cursor on `select ZDTbl capabilities { updateable }`:

```
through the client stack: nothing changed, Amount 2
through the engine:       changed, Amount 99
```

and, read off the two plans:

```
client: column IsChangeRemotable True, IsChangeCallRemotable("Amount") True
engine: column IsChangeRemotable True, IsChangeCallRemotable("Amount") True
```

The server itself believes the proposal is answerable on the client, so it tells the client so, and the client has no handler to run. This is not about staleness: there is nothing stale anywhere in that run.

## Where it is

Two things, and the first is one line.

**`Schema.TableVarColumn.SetIsChangeRemotable(ScalarType dataType)`** (`src/engine/DAE/Schema/TableVar.cs`, around line 184) walks `_eventHandlers` — the *column's* own handlers, which its caller has just walked — instead of `dataType.EventHandlers`. The parameter is otherwise unused outside an `#if USETYPEINHERITANCE` block, so the call does nothing at all. Its two siblings in the same file, `SetIsValidateRemotable(ScalarType)` and `SetIsDefaultRemotable()`, read the type's own handlers as intended, which is why only the `change` case is affected. It reads as a copy-paste slip and is present in the initial open-source commit; not a port consequence.

**Nothing recomputes the flag when a handler is attached to a type that columns already use.** The flag is computed when the column object is constructed (`SetColumn`) and when a table variable's characteristics are redetermined; the statement that attaches a handler holds the type, while the flags that change with it belong to every table with a column of that type, and there is no index from a type to those tables. This is the same shape of problem as K43's, which solved it for the `ShouldChange`/`ShouldDefault`/`ShouldValidate` summary with a catalog-wide counter and a lazy recompute on read (`Schema.Catalog.ShouldCallProposablesTimeStamp`, `Schema.TableVar.EnsureShouldCallProposables`). The remotable flags have no such key.

So fixing the first line alone corrects a table created *after* the handler is attached, and leaves a table created before it wrong. Both arms are needed for the defect to be closed, and whoever picks this up should say which they are doing.

## What the fix changes for an application

`ServerProcess.CopyDataType` sends the client `column.IsChangeRemotable || !column.ShouldChange`, so once the flag is right the client is told to call the server for every change proposal on a column of that type. That is a round trip per edited field where today there is none. The same is already true of a change handler attached to a column or a table variable, so this is bringing one case into line with the others rather than introducing a cost that did not exist — but on a type it can be many more columns, in many more tables, than the author of the handler had in mind.

## Related tickets, and what each does not cover

- `bug-attached-handler-never-reaches-a-remote-client` (K45) fixed the table-variable and column cases, by making the attach throw the server's compiled-plan cache away. It does not help here: the description the server rebuilds is the same wrong one.
- `bug-remote-form-is-never-told-about-a-type-change` makes a client send its proposals to the server when the plan it holds was prepared before a catalog change it has since heard of. That covers a form that was open across the attach, including for a type. It does not cover a plan prepared *after* the attach, which is what is measured above.

## How a fix would be confirmed

One case on the client-stack fixture, in `tests/DAE.Server.Tests/RemoteEventHandlerCacheTest.cs` beside its siblings: the script above, asking the same question of the client stack and of the engine and asserting they agree, as every case in that file does. A second case for the stale arm — the table created before the handler is attached, the client preparing afterwards — if that arm is taken. Both need no database server. Note that the fixture's existing cases assert the *client's* answer against the engine's, so a fix that changed where the work happens without changing the answer would still pass them, which is the intended property.
