description: One general test that catches a whole family of defects at once: whenever a transaction that changed the database's own definitions is rolled back, the running server should be left exactly as it was, and until now that was only ever checked one defect at a time.
prereq: bug-rolled-back-class-attribute-stays-on-the-live-object, bug-rolled-back-column-default-is-never-applied, bug-rolled-back-session-operator-breaks-disconnect
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/InProcessTestFixture.cs
difficulty: medium
----
## What this test is for

Dataphor keeps its own definitions — its tables, types, operators, devices, users, rights and server links — in two places at once: a saved copy in a database, and a live copy in the running server's memory. A statement that changes them changes both. The saved copy is rolled back by the database's own transaction; the live copy is rolled back by the engine replaying a record of each in-memory change it made, one record per change, written at the time (`CatalogDeviceSession`, the `DDLInstruction` classes and `InternalRollbackTransaction`).

Every defect in this family has the same shape: some change in memory has no record, or its record restores the definition but not the state derived from the definition. The server and its own catalog then disagree, and the server believes the version that was rolled back, until it is restarted.

Six instances have been found so far, every one of them by reading code while making an unrelated change, which is not a method: object ownership (`bug-set-object-owner-rollback-keeps-owner`), the six server link user operators and `alter server`'s connection settings (`bug-server-link-user-change-survives-rollback`), and the three this ticket's prerequisites fix. This test replaces the reading with a check that runs on every build.

## The shape of the test

One case per kind of catalog-changing statement, each case:

1. run the setup outside any transaction,
2. take a snapshot of the running server's in-memory catalog,
3. begin a transaction, run the statement, roll the transaction back,
4. take the snapshot again,
5. require the two snapshots to be equal, and on a difference report which entry differed with both values.

A single parameterized case over a list of `(name, setup, statement)` triples, in the shape of the existing `[TestCase]` cases in `tests/DAE.Server.Tests/CatalogStoreTest.cs`, rather than a test method per statement. New file `tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs` over `InProcessTestFixture` (one in-process server per fixture, one session and process per test) with a `<Compile Include="…" />` entry in the project file in alphabetical position.

## The snapshot: read only memory

**Do not use `ScriptCatalog()`**, which the plan this ticket came from proposed. Two reasons, both measured:

- Its object *selection* comes from the catalog store, not from memory (`Schema.Catalog.EmitStatement` with no requested objects calls `session.SelectLibraryCatalogObjects`, which reads the store). An object wrongly left in memory after a rollback would therefore not appear in the script at all — the store no longer lists it.
- It emits each object's D4 statement, and the state this family of defects leaves behind is mostly *not* in the statement. A device's class definition scripts out correctly while the live device keeps the rolled-back value; a server link's metadata scripts out correctly while the settings derived from it do not; an object's owner is not in its `create` statement at all. The plan's claim that `alter server`'s connection settings show up in the script is wrong for exactly this reason.

Build the snapshot as a `Dictionary<string, string>` of entry key to value, reading nothing but memory. For every `Schema.Object` in `Server.Catalog`, keyed by the object's name and ID:

- **`emit:`** — the object's own D4 statement, `objectValue.EmitStatement(EmitMode.ForCopy)` through a `D4TextEmitter`. No session and no store read. This covers metadata tags, class definitions, keys, orders, constraints, representations, defaults and a server link's link users, and it reaches the object's children, which are not in `Server.Catalog` themselves. Catch an exception per object and record it as the entry's value, so one object that cannot emit does not blind the whole snapshot (measured: none of the 1,253 objects of a bare server failed to emit).
- **`props:`** — every readable public instance property of the object whose type is primitive, an enum, `string`, `decimal`, `DateTime`, `TimeSpan` or `Guid`, with no index parameters, as `Name=value` lines sorted by name, a property that throws recorded as `<threw …>`. This is the sensitive dimension: it is what catches the state derived from a definition rather than the definition itself, and it is what caught all three defects the prerequisites fix. Reflect rather than listing fields by hand — a hand-written list is a list of the defects we already know about.
- **`owner:`** — `Owner.ID` for a `Schema.CatalogObject`, which the emitted statement does not carry.
- **`linkusers:`** — for a `Schema.ServerLink`, its link users as local user, remote account and stored password, **sorted**: `ServerLink.Users` is a dictionary, so a mapping removed and put back by an undo can change enumeration order without changing meaning.
- **`deviceusers:`** — for a `Schema.Device`, its device users, each as its own reflected properties, sorted by user id, for the same reason.

Then, outside the per-object loop:

- **`loadedlibrary:`** — each entry of `Catalog.LoadedLibraries`, name to its sorted required library names.
- **`sessionobjects` / `sessionoperators`** — the fixture session's `SessionObjects` and `SessionOperators`, each entry as the session-local name and the global name, sorted. These are the dimension in which `bug-rolled-back-session-operator-breaks-disconnect` shows up, and nothing else sees them.

Compare by key: report entries missing after, entries new after, and entries whose value changed, with both values, truncated to something readable. A failure message that names the object and the differing property is the whole point — an equality assertion on two 1.4 MB strings is not a usable failure.

**Measured cost, so no sampling or restriction to non-system objects is needed:** on this machine a snapshot of a bare in-process server is 4,235 entries and 1.39 MB and takes 21–86 ms; two snapshots per case over 83 cases in one fixture ran in about 10 seconds of wall clock including the server start. Taking the whole catalog, system objects included, is affordable — and it is necessary, because a statement can change a system object (a sort or a conversion attaches to a scalar type that may be a system one).

## The case matrix

These 78 statements were each run through the loop above and are known to compile and execute on a bare in-process server in the `General` library, so the implementer does not have to rediscover the syntax. Setups run outside the transaction. Objects need distinct names per case because the fixture shares one server.

**Tables and views:** `create table`; `drop table`; `alter table T { alter column C alter tags { create … } }`; `alter table T { create key { C } }`; `alter table T { drop key { C } }`; `alter table T { create order { C } }`; `alter table T { drop order { C } }`; `alter table T { create constraint X not exists (…) }`; `alter table T { drop constraint X }`; `alter table T { create transition constraint X on insert … }`; `alter table T alter tags { alter … }`; `alter table T { alter column C { create default "x" } }`; `alter table T { alter column C { drop default } }`; `alter table T { alter column C { create constraint X value <> "" } }`; `alter table T { alter column C { drop constraint X } }`; `create view`; `drop view`.

**Types:** `create type` with a representation; `drop type`; `create type … like String`; `alter type T { create constraint X value.V > 0 }`; `alter type T { drop constraint X }`; `alter type T { create default T(0) }`; `alter type T { drop default }`; `alter type T { create special Maximum T(255) }`; `alter type T alter tags { alter … }`; `create sort T using CompareText(left.value, right.value)`; `create conversion Integer to T using T`; `drop conversion Integer to T`.

**Operators:** `create operator`; `alter operator`; `drop operator`; `create aggregate operator O(V : Integer) : Integer initialization begin end aggregation begin end finalization begin end`.

**References and catalog constraints:** `create reference`; `drop reference`; `create constraint`; `drop constraint`.

**Devices:** `create device D class "System.MemoryDevice"`; `drop device`; `alter device D alter class { create "MaxRowCount" = "100" }`; `alter device D alter reconciliation { mode = { none } }`; `alter device D alter reconciliation { master = both }`.

**Server links:** `create server`; `alter server L alter tags { alter HostName = … }`; `drop server`; `CreateServerLinkUser`. A link naming a host that does not answer is enough — none of these operators connects.

**Event handlers:** `attach operator H to T on { after insert }`; `detach operator H from T on { after insert }`; `detach operator H from C in T on { default }`.

**Security:** `create role`; `drop role`; `CreateRight`; `DropRight`; `CreateUser`; `DropUser`; `SetUserName`; `SetPassword`; `AddUserToRole`; `GrantRightToUser`; `SetObjectOwner`; `SetRightOwner`; `CreateDeviceUser`; `SetDeviceUserID`; `DropDeviceUser`.

**Session objects:** `create session table`; `drop table` of a session table; `create session view`; `create session operator`; `drop operator` of a session operator.

Five more statements were tried and are **not** in the list, with the reason, so they are not retried blind: `alter table T { create column … }` and `{ drop column … }` are refused by the engine for a table in a memory device ("Columns in a memory device cannot be created in this release"), so they need a device that supports them or they stay out; `alter type T { create representation … }` needs a valid accessor and the obvious form is rejected ("Default write accessor cannot be provided …"); `alter type T { alter sort using … }` and `alter device D { create type map System.Integer }` were both syntax errors as written. Add any of them that can be made to run; if one cannot, leave it out and say so in the handoff rather than leaving a case that never executes its statement.

Two more cases that are the same question at a different level, and belong in the same file:

- **A nested rollback undoes its own level only.** One case that changes the catalog at two levels, rolls back the inner one, and requires the snapshot to equal the one taken after the outer change (not the one taken before it), then rolls the outer level back and requires the original.
- **A snapshot of an untouched server is stable.** Two snapshots in a row with nothing in between must be equal. This is a guard on the snapshot itself: a property with a lazily computed or time-dependent value would make every other case flaky, and it is cheap to catch here instead. Measured stable with the dimensions above.

## What this test does not cover, and why

State the first three in the test fixture's own summary comment, so a reader of a passing test knows what it does not say.

- **Library descriptors** (`Catalog.Libraries`). `CreateLibrary`, `DropLibrary`, `SetDefaultDeviceName` and `SetLibraryDescriptor` write the library's folder and its `.d4l` file on disk as they go, and a rollback undoes neither the memory nor the disk, so memory and disk agree and a difference here is not this family. `DropLibrary` also deletes the library's folder, which a rollback cannot put back. Whether library DDL should be transactional at all is a decision, filed as `blocked/library-ddl-inside-a-transaction`. Leave the library-descriptor dimension out of the snapshot and leave those four statements out of the matrix; point at that ticket in a comment.
- **The cached user objects** (`CatalogDevice.UsersCache`, `protected internal`) and the cached answers to rights checks. Not reachable from the test assembly without widening access, and the test deliberately does not widen it. Every user change already records an undo instruction (`CreateUserInstruction`, `DropUserInstruction`, `SetUserNameInstruction`, `SetUserPasswordInstruction`), and the rights-cache questions are covered by three completed tickets — read `bug-right-cache-stale-across-transactions` and `bug-right-cache-shared-during-open-transaction` before deciding to take any of it on here.
- **A child object's own derived state.** Table columns, keys and representations are in the snapshot only through their parent's emitted statement and the parent's own reflected properties; a column's own `ShouldDefault` flag, for instance, is not compared. The prerequisite `bug-rolled-back-column-default-is-never-applied` covers the one case where that mattered. Widening the snapshot to children is a reasonable later change and is not needed to land this.
- **The caches `InvalidateCachesAfterUndo` clears wholesale** (the plan cache, the operator resolution cache, the conversion path cache) and the store's own rows. `CatalogStoreTest.NestedRollbackUndoesStatementWrites` already compares every row of every store table around a rolled-back create and drop; do not duplicate it.
- **The forward direction.** This test only asks what a rollback leaves. `backlog/bug-dropped-class-attribute-stays-on-the-live-object` is an example of the same derived-state problem on the forward path, which this test by construction cannot see.

## Edge cases & interactions

- **Fixture sharing.** One server per fixture and one session per test, so a statement's objects must not collide with another case's. Names are per case already; keep them that way rather than dropping objects in a teardown, which would itself change the catalog.
- **A case whose statement fails to compile or execute.** The snapshot would match trivially and the case would prove nothing. Fail the case if its statement does not execute, rather than passing quietly — this is how the five statements above were found.
- **Object identifiers advance across a rolled-back create** and are not rolled back (`engine-object-id-generator`). Snapshot entries are keyed by name and ID, so this only matters between cases, not within one; do not assert on identifier values.
- **Enumeration order.** `Server.Catalog` is enumerated into a dictionary keyed by name, so its order does not matter; the three collection dimensions are sorted for the reason given above. Do not compare any collection without sorting it first.
- **A property with a side effect.** Reflection reads every scalar property of every catalog object, including a device's. The stability case above is the guard; if a property turns out to start something or to change on first read, name it and exclude it by name with a comment, rather than dropping the reflection dimension.
- **A newly surfaced cause.** With the three prerequisites landed, the matrix was clean on everything measured. If a case still fails: fix it in this ticket when the fix is local and obvious (the project's "bugs met along the way" rule), and otherwise file the cause as its own ticket and leave that one statement out of the matrix with a `NOTE:` naming the ticket. **Never** `Ignore`, skip or soften a case to get a green run.
- **Run time.** The whole fixture was about 10 seconds; it joins part 1 of the three-part suite run, which is the part already closest to the runner's limit, so keep the case count to the matrix above rather than multiplying variants.

## TODO

- New `tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs` over `InProcessTestFixture`, with the snapshot and the difference report described above, and its `<Compile Include="…" />` entry in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`.
- The parameterized matrix, the nested-rollback case and the snapshot-stability case.
- A fixture summary comment stating what the test does not cover, including the pointer to `blocked/library-ddl-inside-a-transaction`.
- Confirm the matrix is green with the three prerequisites landed, and report in the handoff any statement left out and why.
- Build (`dotnet build Dataphor.slnx`), run the new fixture, then the three part commands from the root `AGENTS.md`, each in the foreground. No `docs/migration-notes.md` entry: this ticket adds a test and changes nothing an application could notice.
