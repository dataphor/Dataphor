description: One general test that would catch a whole family of defects at once: whenever a transaction that changed the database's own catalog is rolled back, the running server should be left exactly as it was, and today several kinds of change are not undone.
prereq: bug-server-link-user-change-survives-rollback
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
severity: wrong-result
likelihood: unusual
tradeoffs: It is a test, not a fix, and it will almost certainly fail on things nobody has reported, so landing it means either triaging whatever it turns up or recording those as known failures - a maintainer may prefer to keep fixing the instances as they are found.
----
## Why this is worth a general test

Dataphor keeps its own catalog - the definitions of its tables, types, operators, users, rights and server links - in two places at once: a saved copy in a database, and a live copy in the running server's memory. A transaction that changes the catalog changes both. The saved copy is rolled back by the database's own transaction; the live copy is rolled back by the engine replaying a record of each change it made, one record per change, written at the time (`CatalogDeviceSession`, the `DDLInstruction` classes and `InternalRollbackTransaction`).

The failure is always the same: some change in memory has no record, so the rollback does not undo it. The server and its own catalog then disagree, and the server believes the version that was rolled back, until it is restarted.

Three have been found so far, each by hand and each in a different part of the catalog: object ownership, fixed by `bug-set-object-owner-rollback-keeps-owner`; and the six server link user operators and `alter server`'s connection settings, both fixed by `bug-server-link-user-change-survives-rollback`.

Nothing suggests those were the last. All three were found by reading code while making an unrelated change, which is not a method.

## What the test would be

Script the catalog, run a statement inside a transaction, roll the transaction back, script the catalog again, and require the two scripts to be identical. The engine can already produce the script - `Schema.Catalog.EmitStatement`, which is what the catalog store writes and what the `ScriptCatalog` operators expose - and it covers everything that is persisted, which is the same thing a restart would read back. Both instances above show up in it: a link user appears as a `CreateServerLinkUserWithEncryptedPassword` call, a link's settings as its metadata tags.

Then one case per kind of catalog-changing statement: create and drop and alter for each kind of object, the security operators, the library operators, the device operators, the server link operators. A single parameterized case with a list of statements, in the shape of the existing `CatalogStoreTest` cases, rather than a case per statement.

Worth covering as part of the same test, because they are the same question asked at a different level:

- **A nested rollback** should undo its own level only. The record is replayed back to the level's marker, so a case that changes the catalog at two levels and rolls back the inner one pins that.
- **What the server cached** from the catalog, not just the catalog itself. A rollback that undoes anything calls `InvalidateCachesAfterUndo`; a rollback that undoes nothing does not, so a missing record silently leaves stale cached plans, resolutions and rights answers behind as well.

## What it does not cover

The script is of what is *persisted*. In-memory state that is neither persisted nor derivable from what is - a device that was started, a cached answer to a rights check, a connection held to another server - is invisible to this test, and each such thing needs its own case. Three completed tickets cover ones that were already wrong (`bug-catalog-deferred-device-stops-ignore-nesting`, `bug-right-cache-stale-across-transactions`, `bug-right-cache-shared-during-open-transaction`); read them before deciding how much of that to take on here.

## Expect it to fail when first written

That is the point, and it is also the cost: whatever it turns up has to be triaged into fixes or into recorded known failures before the test can be part of a green suite. Budget for that, and prefer landing it with the failures recorded and a ticket per cause to landing a narrower test that passes.
