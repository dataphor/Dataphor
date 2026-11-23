description: If a user creates a temporary operator of their own inside a transaction and that transaction is rolled back, the server keeps a dead entry for it and then fails with an error when the user disconnects.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Server/Engine.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
repro: verified
difficulty: easy
----
## What goes wrong

A *session operator* is an operator that belongs to one session and goes away when that session ends: `create session operator F() : Integer begin result := 1; end;`. The engine gives it a unique global name of its own (`Object_<guid>`) and keeps a per-session entry mapping the name the user typed (`General.F`) to that global name.

Run that statement inside a transaction and roll the transaction back. The operator itself is correctly removed from the catalog, but the session's entry for it survives. From then on the session holds an alias pointing at an operator that no longer exists, and when the session is disconnected the engine walks that list to clean up, cannot find the operator and throws `Object "Object_<guid>" not found` (`SchemaException`, code 113104) out of `Engine.Disconnect`.

Measured on an in-process server on 2026-11-23: a `create session operator` in an `Isolated` transaction, rolled back, left the session entry `General.P_SOp3 -> Object_<guid>` in place, and `Engine.Disconnect` (`src/engine/DAE/Server/Engine.cs:890`) then threw 113104. `create session table` and `create session view` are **not** affected, and a rolled-back `drop session operator` is not affected; both were run the same way and left nothing behind.

## Why, exactly

Two orderings disagree.

`CatalogDeviceSession.CreateOperator` (around line 2486) does its work in this order:

1. `InsertCatalogObject(operatorValue)` — which records a `CreateCatalogObjectInstruction` in the undo log,
2. `CreateSessionOperator(operatorValue)` — which records a `CreateSessionOperatorInstruction`.

The rollback replays the undo log **backwards**, so `CreateSessionOperatorInstruction.Undo` runs first. It calls `Engine.DropSessionOperator` (`src/engine/DAE/Server/Engine.cs:598`), which removes the session entry only when the operator's global name is no longer in the catalog:

```csharp
if (!Catalog.OperatorMaps.ContainsName(operatorValue.OperatorName))
```

At that moment the catalog object has not been cleared yet — that is the *next* instruction — so the guard is false and the entry is left alone. Confirmed by measurement: inside the transaction `Catalog.OperatorMaps.ContainsName(<global name>)` is `true`; after the rollback it is `false` and the session entry is still there.

The forward `DropOperator` has the two steps the other way round (`DeleteCatalogObject` first, then `DropSessionOperator`), which is why dropping an operator works and undoing its creation does not.

## What to change

Record `CreateSessionOperatorInstruction` **before** `InsertCatalogObject` in `CreateOperator`, so that the undo replays in the same order as the forward `DropOperator`: catalog object cleared first, session entry removed second. That is a two-statement move in one method.

Do **not** remove or weaken the guard in `Engine.DropSessionOperator`. It is load-bearing: a session operator's overloads share one global name and one session entry, so dropping one overload must not remove the entry while another overload is still in the catalog.

Do **not** change `CreateTable` / the session *object* path to match. `Engine.DropSessionObject` guards on the entry's global name rather than on catalog membership, so it already removes the right entry whatever the order; both session-table and session-view rollbacks were measured clean.

## Edge cases & interactions

- **Two overloads of the same session operator created in one transaction, rolled back.** Each `create` records its own pair of instructions; the undo must leave no session entry. Verified by a test case.
- **A session operator created at an outer level and another at an inner level, inner level rolled back.** The replay stops at the level's `BeginTransactionInstruction`, so the outer one must survive. Verified by a test case.
- **A session operator created and then dropped in the same transaction, rolled back.** The undo of the drop re-adds the entry unconditionally (`CreateSessionOperator`), then the undo of the create must remove it. Covered by a test case.
- **An ordinary (non-session) operator.** `SessionObjectName` is null, so neither branch runs; no behaviour change. By inspection.
- **The session disconnects without a rollback.** Unchanged — this ticket only moves where an instruction is recorded.
- **Exceptions during rollback.** The rollback loop logs and continues, which is why this failure was silent until disconnect; after the fix nothing in the path throws, so no change is needed there.

## TODO

- Move the `CreateSessionOperatorInstruction` recording and the `CreateSessionObject`-style call in `CatalogDeviceSession.CreateOperator` to before `InsertCatalogObject`, with a comment saying why the order matters (naming `Engine.DropSessionOperator`'s guard).
- Add the reproduction to `tests/DAE.Server.Tests/CatalogStoreTest.cs`, which already holds the catalog rollback cases: a rolled-back `create session operator` leaves no session entry and the session disconnects without error. Assert on the session's `SessionOperators` collection and on a clean `Server.Disconnect`, and confirm the case fails before the fix.
- Add the three level/ordering cases above as test cases only where they are not already covered by the one reproduction; one test per behaviour.
- `docs/migration-notes.md`: one entry under *D4, catalog and libraries* — a rolled-back `create session operator` no longer leaves the session unable to disconnect.
- Build (`dotnet build Dataphor.slnx`), run the new cases, then the three part commands from the root `AGENTS.md`, each in the foreground.
