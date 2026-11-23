description: A user's temporary operator created inside a rolled-back transaction no longer leaves the session broken, so disconnecting works again.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, docs/migration-notes.md
repro: verified
----
## What was wrong

A *session operator* belongs to one session and goes away when the session ends (`create session operator F() : Integer begin result := 1; end;`). The engine gives it a unique global name (`Object_<guid>`) in the catalog and keeps a per-session entry mapping the name the user typed (`General.F`) to that global name.

Creating one inside a transaction and rolling the transaction back removed the operator from the catalog but left the session entry behind. The session then held an alias for an operator that no longer existed, and disconnecting it threw `Object "Object_<guid>" not found` (`SchemaException`, code 113104) out of `Engine.Disconnect`.

The cause was an ordering disagreement. `CatalogDeviceSession.CreateOperator` inserted the catalog object *first* and recorded the session-operator instruction *second*; a rollback replays the undo log backwards, so the session entry's undo ran while the catalog object was still present. `Engine.DropSessionOperator` (`src/engine/DAE/Server/Engine.cs:598`) removes the entry only once the operator's global name is gone from the catalog — that guard was false, so the entry survived. The forward `DropOperator` already did the two steps in the opposite order, which is why dropping a session operator worked and undoing its creation did not.

## What changed

`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`, `CreateOperator`: the `CreateSessionOperator` call and its `CreateSessionOperatorInstruction` recording now come *before* `InsertCatalogObject`, with a comment stating why the order matters and naming `Engine.DropSessionOperator`'s guard. The undo log now replays in the same order as the forward `DropOperator`: catalog object cleared first, session entry removed second. Nothing else changed — the guard in `Engine.DropSessionOperator` was deliberately left alone (it is load-bearing: overloads of a session operator share one global name and one session entry, so dropping one overload must not remove the entry while another overload is still in the catalog), and the session *object* path (`CreateTable` and friends) was left alone because `Engine.DropSessionObject` guards on the entry's global name rather than on catalog membership and is already order-independent.

`docs/migration-notes.md`: one row under *D4, catalog and libraries*.

## Tests added

Both in `tests/DAE.Server.Tests/CatalogStoreTest.cs`, which already holds the catalog rollback cases. Each connects its own session so the test can read `ServerSession.SessionOperators` and so the disconnect itself is under test.

- `RolledBackSessionOperatorLeavesNoSessionEntry` (three `[TestCase]`s) — verifies that after a rolled-back `Isolated` transaction the session has no session-operator entry and disconnects cleanly, for: one session operator; two overloads of the same session operator created in the same transaction; a session operator created and then dropped in the same transaction (the undo of the drop re-adds the entry unconditionally, then the undo of the create must remove it).
- `InnerRollbackKeepsOuterSessionOperator` — verifies that a session operator created at an outer transaction level survives the rollback of an inner level that created one of its own: exactly one entry remains, named `General.OuterSOp`, and the session disconnects cleanly.

All four cases were confirmed to **fail before the fix** with the reported `Object "Object_<guid>" not found`, and to pass after it.

## Validation run

- `dotnet build Dataphor.slnx` — 0 errors.
- The four new cases: 4 passed.
- The three suite parts from the root `AGENTS.md`, each in the foreground: part 1 704 passed / 0 failed / 33 skipped (8 m 17 s); part 2 67 passed / 0 failed / 11 skipped; part 3 50 passed / 0 failed / 12 skipped. No new failures, no pre-existing failures surfaced.

## What to look at, and known gaps

- **The one judgement call worth a second opinion.** Moving `CreateSessionOperator` ahead of `InsertCatalogObject` means that if `InsertCatalogObject` ever threw, the session entry would already be in place. Inside a transaction this is harmless — the `CreateSessionOperatorInstruction` is recorded before the insert, so the rollback removes the entry (the catalog object is absent, so the guard passes). Outside a transaction or in a loading context no instruction is recorded, so a throwing `InsertCatalogObject` would leave a dangling entry; before this change the leak went the other way (no entry, catalog object present). Judged not worth guarding: DDL reaches here inside a transaction in normal operation, and a failing catalog insert during load aborts the load. If the reviewer disagrees, the alternative is a `try`/`catch` around the insert that removes the entry — say so rather than reverting the order, which reinstates the bug.
- No new test covers the plain non-session operator path (`SessionObjectName` is null, so neither branch runs); that is unchanged by inspection and the existing suite exercises it heavily.
- The three `[TestCase]`s share one test method because they are the same behaviour in three shapes; a reviewer who reads the drop-then-create case as a distinct behaviour may want it split out.
