description: Deleting one of the units a Dataphor application is shipped in erases its folder from disk the moment the statement runs, so undoing the surrounding transaction cannot bring it back. The delete now refuses to run inside a transaction the user opened, and the rule is written down.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/Server/Schema/LibraryUtility.cs, src/engine/DAE/Schema/SchemaException.cs, src/engine/DAE/Schema/SchemaException.resx, docs/architecture/libraries-and-class-loading.md, docs/migration-notes.md, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryAdministration.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/LibraryUpgrades.d4, tests/DAE.Server.Tests/LibraryDropTransactionTest.cs, tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj
----
## What landed

A Dataphor *library* is the unit an application ships in: a folder in the server's library directory holding a descriptor file (`<name>.d4l`), the library's D4 documents and its upgrade scripts. The operators that create, drop or reconfigure a library write that folder as they run and record nothing for a rollback to replay. `DropLibrary` is the one case where that loses something - it deletes the folder recursively and no rollback can put a folder back - so it now refuses inside a transaction the session opened itself. The others are untouched: each is repaired by running its inverse.

**The predicate.** `ServerProcess.InExplicitTransaction`, public and read-only, beside `InTransaction`: true when any open transaction has `ImplicitCallDepth == 0`. That stamp is 0 for exactly two shapes - a transaction begun by a D4 `BeginTransaction()`, a `commit` block or a client's `BeginTransaction` call, and an implicit transaction the engine has left open because implicit transactions were switched off during its call (`EndTransactionalCall` resets the stamp for that case, which is what makes it count as explicit: it will outlive the statement and can be rolled back). It is deliberately **not** built on the nesting count or on the existing `InTransactionBeyondCurrentCall`: under the coverage harness a script's statements run two transactional calls deep inside one implicit transaction, so either of those would refuse every coverage script that drops a library. Public rather than internal because `LibraryUtility` is in `AlphoraDataphorServer` and `ServerProcess` in `Alphora.Dataphor.DAE`, with no `InternalsVisibleTo` between them.

**The guard.** First statement of `LibraryUtility.DropLibrary`, before the `lock` and before the library is resolved, so both callers - the `DropLibrary` operator and the catalog device's delete handler for `System.Libraries`, which is what `delete Libraries` and a cursor delete on that table run - are covered by one check, and so that the refusal is a property of the statement rather than of catalog state. Those are the only two callers.

**The error code.** `CannotDropLibraryInExplicitTransaction = 113251` in `SchemaException.cs` with its message in `SchemaException.resx`. `docs/reference/error-codes.md` lists prefix ranges only and needed no edit.

**The documentation.** A "Library DDL is not transactional" paragraph at the end of `docs/architecture/libraries-and-class-loading.md` and one `done` row in the "D4, catalog and libraries" section of `docs/migration-notes.md`. Three comments that said this question was undecided and named the ticket - in `LibraryAdministration.d4`, `LibraryUpgrades.d4` and `CatalogRollbackSnapshotTest.cs` - now state the rule and point at that architecture section.

**Tests.** Three arms, each a different caller state: an ordinary explicit transaction opened with `BeginTransaction()` inside a block (`LibraryAdministration.d4`, which also pins that the refusal precedes any removal, that the error displaces the two errors the drop would otherwise have raised first, and that the drop succeeds once the transaction is rolled back); a `DropLibrary` inside a library upgrade script, which is the one caller shape the change takes away from existing applications (`LibraryUpgrades.d4`); and the transaction nobody opens on purpose - the one `SetUseImplicitTransactions(false)` leaves open with its stamp reset, which only a top-level transactional call produces and so cannot be written as a coverage script (`LibraryDropTransactionTest.cs`). The positive case needs no test of its own: every other `DropLibrary` in the library coverage scripts runs inside the harness's single implicit transaction and must keep passing.

## Review findings

The implement-stage diff (`ticket(implement): library-ddl-inside-a-transaction`) was read before the handoff summary.

### Checked

**The predicate, which is the whole subject.** Traced `ImplicitCallDepth` through `BeginTransactionalCall`, `EndTransactionalCall` and `ServerTransaction`. Only the root transaction can ever carry a non-zero stamp: `BeginTransactionalCall` stamps a transaction only in the branch where it begins one at `Count == 0`, and `EndTransactionalCall` resets only `_transactions[0]`. So the predicate reduces to "the root transaction is one somebody opened or the engine left open, or a nested transaction exists" - and every nested transaction is one somebody opened. The two other loops in `ServerProcess` that begin transactions to catch a connection up (`DeviceConnect`, `EnlistRemoteSession`) begin them on a device session and on a remote session, never on `_transactions`, so the engine never leaves a nested implicit transaction behind for the predicate to misread. The stamp survives implicit transactions being switched back on, which is the correct answer: such a transaction still outlives its statement and can still roll back.

**Callers.** The two the handoff names are the only two. `ScriptDropLibrary` - the only other drop-a-library-shaped name in shipped D4, used by `libraries/Security/Upgrades/Upgrade.2.0.1.d4` and by the coverage libraries' `TestSerializer` - emits drop statements for a library's *objects* through `Catalog.EmitDropStatement` and never a `DropLibrary` call; there is no `DropLibraryStatement` in the AST at all. Those upgrade and serializer paths are untouched.

**The IDE path, which the handoff flagged as read-only and worth re-reading.** Traced end to end rather than trusted: `DataSet.Delete()` opens no transaction, `View.InternalDelete` either runs a configured delete statement or falls through to `DAEDataSet.InternalDelete` and `_cursor.Delete()`, and a grep for `BeginTransaction` over all of `src/client` and `src/windows` finds it in only four places - `DAEDataSet.InternalPost`, `TableDataSet.InternalInitializeRow`, `View.InternalChangeColumn` and the ADO.NET provider - none of them on the delete path. The IDE's library-tree Delete runs in an ordinary implicit transaction and is unaffected. Still not run in the real IDE: the claim is now read twice, not observed.

**Aspect sweep.** Nothing is allocated, so there is nothing to clean up; the loop is O(open transactions), which is the nesting depth; the unlocked read of `_transactions` matches `InTransaction` immediately above it and `InTransactionBeyondCurrentCall` below, and the property is only reachable from a statement running on the process. Error handling is one `throw` of the same `SchemaException` family as every other refusal in the file, and the drop's existing catch-and-restore blocks are untouched. `InProcessTestFixture` gives one server instance per fixture, named after the fixture type and disposed at teardown, so a library the new C# test leaves behind on a failure is confined to that instance and swept with it.

### Found and fixed in this pass

**A shipped D4 shape the change breaks that nothing recorded: `try ... commit;`.** The parser desugars a `commit` block into a `BeginTransaction()` followed by the body, so a `DropLibrary` written inside one is now refused - and the coverage libraries' own `NegativeTest` operator in `DiagnosticsLogic.d4` is written in exactly that shape. Nothing actually fails: no coverage script and no shipped library passes a library operator through `NegativeTest` or through any other `commit` block (checked across `tests/libraries`, `libraries` and `samples`). But both the architecture paragraph and the migration note listed only `BeginTransaction()` and a client's begin as "explicit", so an application whose cleanup reads `try DropLibrary('X'); commit;` had no way to find out why it had started failing. Both now name the `commit` block. No test: it desugars into the state the `LibraryAdministration.d4` arm already covers.

**The migration note understated the error reordering.** It recorded only that, inside such a transaction, a drop of a library the catalog has never heard of now reports 113251 instead of 113104. The guard is the method's first statement, so it equally displaces 113196, "is registered and cannot be dropped" - the one a user is far more likely to meet, since dropping a registered library is an ordinary mistake and dropping a nonexistent one is a typo. The note now states the rule and both codes, and the `LibraryAdministration.d4` block gained two arms pinning them. This was the one documented consequence of the guard's placement with nothing checking it.

**Comment hygiene.** One run of header comment lines in `LibraryAdministration.d4` that the new text had left at about 145 characters against the file's 125; re-wrapped.

### Parked as a tripwire

A `NOTE:` at the guard site records that extending this refusal to the other library operators would break the IDE's library Add and Edit forms: they reach `CreateLibrary` and `RenameLibrary` through the `System.Libraries` insert and update handlers from inside the explicit transaction `DAEDataSet.InternalPost` opens around every post. Conditional - it is work only if someone widens the guard, which is a plausible next thought for a reader of the new architecture paragraph.

### Not filed

**No tickets.** Nothing reached the filing bar. No latent defect was found; the one class-level question behind this work - whether library DDL should be transactional at all - is the decision this ticket settled, and the architecture paragraph is where it now lives.

**Size debt, weighed and left.** `ServerProcess.cs` is 2,568 lines (`wc -l`), 19 of them this ticket's. Pre-existing, no open ticket claims the file (grep over `tickets/backlog`, `fix`, `plan`, `implement`, `review` finds only an unrelated mention in `debt-realsql-coverage`), and a nine-line read-only property is not the evidence that makes splitting it worth doing.

### Tests

None cut. The implementer's three arms each pin a different caller state rather than restating the one `if`, and the project's rule owes a characterization test for a behaviour change; nothing mocks anything. Added the two error-ordering arms described above. Declined the handoff's offer of a fourth C# test for a client-side `BeginTransaction`: it reaches the same state as the D4 arm by a caller that opens the transaction the same way.

### Commands run

Build: `dotnet build Dataphor.slnx` - 0 errors, no new warnings (144 warnings, all pre-existing). The new `Assert.Catch(() => ...)` binds the `Action` overload, so it does not join the obsolete-`TestDelegate` warnings `ApplicationTransactionTest` carries.

Review set, one ring wider - the implement stage's targeted set plus the transaction-facing fixtures and coverage scripts a change to `ServerProcess`'s transaction state could disturb:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=LibraryAdministration|Name=LibraryCoverage|Name=LibraryDependencies|Name=LibraryRename|Name=LibraryUpgrades|Name=Serialization|Name=DerivationMaps|FullyQualifiedName~LibraryDropTransactionTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~LibraryAttachTest|FullyQualifiedName~LibraryLoadingTest|FullyQualifiedName~TransactionBeginTest|FullyQualifiedName~PartialCommitTest|FullyQualifiedName~ApplicationTransactionTest|FullyQualifiedName~ConnectionTransactionLevelTest|Name~Transaction" --logger "console;verbosity=normal"
```

184 tests, 173 passed, 11 skipped, in 2.7 minutes - run once before the fixes and again after. Every skip is accounted for: 6 match known-failure manifest entries exactly (`test-pre-3-1-scripts-policy` for `DerivationMaps` and `Serialization`, `feat-ws7-devices-and-libraries` for the three MSSQL atomicity scripts, `debt-memory-device-transaction-isolation` for the isolation script), with 0 mismatched and 0 unexpectedly passing, and the rest are the script fragments and the two NUnit-explicit cursor cases. No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written. No coverage-manifest entry changed.

The three part commands were not needed and were not run: nothing in the build or test infrastructure, `Directory.Build.props`, `Directory.Packages.props`, the coverage runner or manifest format, or the lexer, parser or compiler changed.

Documentation: `python tools/docs-size-check/check_size.py` - 13 files, 0 over the limit (`libraries-and-class-loading.md` is comfortably inside the 1,500-word cap after the edits). `python tools/docs-link-check/check_links.py` reports 203 errors across `docs/`, all pre-existing, all in `docs/developers-guide`, `docs/users-guide` and `docs/reference` pointing at a `reference/system-library/` tree that does not exist, and none in either file this ticket edited.
