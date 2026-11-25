description: Deleting one of the units a Dataphor application is shipped in erases its folder from disk the moment the statement runs, so undoing the surrounding transaction cannot bring it back. The delete now refuses to run inside a transaction the user opened, and the rule is written down.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/Server/Schema/LibraryUtility.cs, src/engine/DAE/Schema/SchemaException.cs, src/engine/DAE/Schema/SchemaException.resx, docs/architecture/libraries-and-class-loading.md, docs/migration-notes.md, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryUpgrades.d4, tests/DAE.Server.Tests/LibraryDropTransactionTest.cs, tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj
difficulty: medium
----
## What landed

A Dataphor *library* is the unit an application ships in: a folder in the server's library directory holding a descriptor file (`<name>.d4l`), the library's D4 documents and its upgrade scripts. The operators that create, drop or reconfigure a library write that folder as they run and record nothing for a rollback to replay. `DropLibrary` is the one case where that loses something - it deletes the folder recursively and no rollback can put a folder back - so it now refuses inside a transaction the session opened itself. The others are untouched: each is repaired by running its inverse.

**The predicate.** `ServerProcess.InExplicitTransaction` (`src/engine/DAE/Server/ServerProcess.cs:1280`), public and read-only, beside `InTransaction`: true when any open transaction has `ImplicitCallDepth == 0`. That stamp is 0 for exactly two shapes - a transaction begun by a D4 `BeginTransaction()` or a client's `BeginTransaction` call, and an implicit transaction the engine has left open because implicit transactions were switched off during its call (`EndTransactionalCall` resets the stamp for that case, which is what makes it count as explicit: it will outlive the statement and can be rolled back). It is deliberately **not** built on the nesting count or on the existing `InTransactionBeyondCurrentCall`: under the coverage harness a script's statements run two transactional calls deep inside one implicit transaction, so either of those would refuse every coverage script that drops a library. Public rather than internal because `LibraryUtility` is in `AlphoraDataphorServer` and `ServerProcess` in `Alphora.Dataphor.DAE`, with no `InternalsVisibleTo` between them.

**The guard.** First statement of `LibraryUtility.DropLibrary` (`src/engine/Server/Schema/LibraryUtility.cs:263`), before the `lock` and before the library is resolved, so both callers - the `DropLibrary` operator and the catalog device's delete handler for `System.Libraries`, which is what `delete Libraries` and a cursor delete on that table run - are covered by one check, and so that the refusal is a property of the statement rather than of catalog state. Those are the only two callers (`grep -rn "LibraryUtility.DropLibrary" src/`, re-run during implementation).

**The error code.** `CannotDropLibraryInExplicitTransaction = 113251` in `SchemaException.cs` with its message in `SchemaException.resx`: `Library "{0}" cannot be dropped inside an explicit transaction, because dropping a library deletes its directory and a rollback cannot restore it.` `docs/reference/error-codes.md` lists prefix ranges only and needed no edit (checked).

**The documentation.** A new "Library DDL is not transactional" paragraph at the end of `docs/architecture/libraries-and-class-loading.md` (1,021 of 1,500 words after the edit) and one `done` row in the "D4, catalog and libraries" section of `docs/migration-notes.md`. Three comments that said this question was undecided and named the ticket - in `LibraryAdministration.d4`, `LibraryUpgrades.d4` and `CatalogRollbackSnapshotTest.cs` - now state the rule and point at that architecture section instead of at a ticket path.

## What a reader should check

The two kinds of transaction are the whole subject, so the thing to be adversarial about is whether `ImplicitCallDepth == 0` really separates them, and whether any shipped caller falls on the wrong side. The plan ticket's caller survey read the IDE's library tree, the IDE's and `D4Runner`'s script execution, every shipped `.d4`/`.d4l` in `libraries/` and `samples/`, the client and CLI paths, the coverage scripts and the C# fixtures, and found no shipped caller that drops a library inside an explicit transaction. **That survey was done by reading code, not by running the IDE** - nothing in this ticket started Dataphoria - so the IDE's Delete-key path over `.System.Libraries` is the one claim worth re-reading rather than trusting.

## Tests

Each new test, and what it pins:

- `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4`, a new block after the error section: an explicit transaction opened with `BeginTransaction()` **inside a block** (a standalone `BeginTransaction()` statement does not outlive its own statement), a `DropLibrary` refused with `:113251 --->` and the message, the library still in `Libraries` afterwards (the refusal precedes any removal), and the same drop succeeding once `RollbackTransaction()` has closed that transaction. This is the ordinary explicit-transaction shape.
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryUpgrades.d4`, a new block after the failing-upgrade sections: **a `DropLibrary` inside a library upgrade script is refused.** Not in the implement ticket's test list - added because this is the one caller shape the change takes away from existing applications, and the one the migration note promises. An upgrade script runs inside the transaction `InternalUpgradeLibrary` opens for it, which is explicit by the stamp above. The arm asserts the 113251 code and message out of `UpgradeLibrary`, that the library the script tried to drop is still there, and that the recorded version is left at the version the run started from. Both tail checks at the end of that script gained the two new library names.
- `tests/DAE.Server.Tests/LibraryDropTransactionTest.cs`, one test: the transaction nobody opens on purpose. `SetUseImplicitTransactions(false)` on a session and process of its own leaves that statement's own implicit transaction open with the stamp reset; the drop is refused with 113251; after `RollbackTransaction()` - implicit transactions still off, so no transaction at all - the drop runs. C# because the stamp reset fires only for a top-level transactional call on the process, which the coverage harness never gives a script (it runs each script as a nested `ExecuteScript`). Its own session and process so that the switched-off setting and the open transaction do not leak into other tests.

No new test for the positive case: every other `DropLibrary` in the library coverage scripts already runs inside the harness's single implicit transaction and must keep passing, which is the regression guard for the predicate reading the stamp rather than the count.

**Deliberately not tested, for the reviewer to weigh:**

- A **client-side** explicit transaction (`IServerProcess.BeginTransaction()` from a host, then a `DropLibrary` statement) exercises the same branch - an open transaction whose stamp is 0 - as the in-script `BeginTransaction()` the D4 arm covers. Judged equivalent rather than a third test; disagree and it is four lines in `LibraryDropTransactionTest`.
- **Error ordering.** Inside an explicit transaction, dropping a library the catalog has never heard of now reports 113251 rather than 113104 ("object not found"), because the guard runs before the library is resolved. That is the intended trade and it is in the migration note, but it is verified by inspection of the ordering only - there is no test for it.

## Commands run

Build: `dotnet build Dataphor.slnx` - 0 errors, no new warnings.

Targeted tests, the ticket's two filter sets in one run:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=LibraryAdministration|Name=LibraryCoverage|Name=LibraryDependencies|Name=LibraryRename|Name=LibraryUpgrades|Name=Serialization|Name=DerivationMaps|FullyQualifiedName~LibraryDropTransactionTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~LibraryAttachTest|FullyQualifiedName~LibraryLoadingTest" --logger "console;verbosity=normal"
```

119 tests, 117 passed, 2 skipped - `DerivationMaps` and `Serialization`, both matching their `test-pre-3-1-scripts-policy` known-failure entries in the coverage manifest exactly (0 mismatched, 0 unexpectedly passing). No pre-existing failure surfaced, so no `.pre-existing-error.md` was written. No coverage-manifest entry changed.

Documentation: `python tools/docs-size-check/check_size.py` (13 files, 0 over the limit). `python tools/docs-link-check/check_links.py` reports 203 errors across `docs/`, all pre-existing and all in `docs/developers-guide`, `docs/users-guide` and `docs/reference` pointing at a `reference/system-library/` tree that does not exist; none is in either file this ticket edited (checked by filtering the output for those two paths).

## Review set, one ring wider

The implement set above, plus the transaction-facing fixtures a change to `ServerProcess`'s transaction state could disturb: `TransactionBeginTest`, `PartialCommitTest`, `ApplicationTransactionTest`, `ConnectionTransactionLevelTest` and the `Transaction.*` coverage scripts. The added property is read-only and nothing outside `LibraryUtility.DropLibrary` reads it, so the risk is confined there, but the stamp it reads is the same field `BeginTransactionalCall` and `EndTransactionalCall` maintain. The three part commands are not needed: no build or test infrastructure changed, and nothing in the lexer, parser or compiler.

## Review findings

(for the reviewer to fill)
