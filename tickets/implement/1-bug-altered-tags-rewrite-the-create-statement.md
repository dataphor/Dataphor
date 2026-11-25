description: A statement that creates something can give it named settings. Changing one of those settings afterwards quietly rewrote the statement itself, so a program that runs the same statement a second time creates the thing with the changed setting instead of the one the statement is written with.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/architecture/catalog-and-store.md, docs/migration-notes.md
repro: verified
difficulty: medium
----
## What goes wrong

A D4 object can carry *tags*: named string settings written with the statement that creates the object, for example `create table T { ID : Integer, key { ID } } tags { Frontend.Title = "Own" }`. The tags can be changed later with `alter table T alter tags { alter Frontend.Title = "Altered" }`.

Eleven of the compiler's create statements hand the object they build the statement's *own* tag collection rather than a copy, so the object and the statement share one collection. `AlterNode.AlterMetaData` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1077`) changes tags by editing that collection in place — `Tags.UpdateRange`, `AddRange`, `RemoveRange`. Because it is shared, the edit lands in the create statement's own parse tree as well. The statement no longer says what it was written with, and anything that runs it again applies the changed value.

Measured against an in-process engine on 2026-11-25, using a statement plan the caller prepares and executes twice (the route a client, and the Dataphor IDE, takes):

```
plan = PrepareStatement("create table ShareProbe { ID : Integer, key { ID } } tags { Frontend.Title = \"Own\" };")
plan.Execute()                                                                   -> Frontend.Title = "Own"
Execute("alter table ShareProbe alter tags { alter Frontend.Title = \"Altered\" };")  -> Frontend.Title = "Altered"
Execute("drop table ShareProbe;")
plan.Execute()                                                                   -> Frontend.Title = "Altered"   <- should be "Own"
```

and the stored create script for the re-created table reads `tags { Frontend.Title = "Altered" }`. With the fix below in place the same probe gives `"Own"` both times and the stored script says `"Own"`.

This is the tag half of `bug-remembered-statement-applies-a-changed-setting`, which fixed the *class definition* half (a storage device's settings) and deliberately left tags alone. That ticket's review concluded tags had "no analogue of this defect at all", on the grounds that `Tag` is a struct so tag values are copied by value. That reasoning is incomplete: the `Tags` *list* is what the object and the statement share, and `AddOrUpdate`/`Update` replace an element of that shared list. The measurement above is the analogue.

Present since the initial open-source commit; not a port consequence.

## Where it is

Eleven assignments in `src/engine/DAE/Compiling/Compiler.cs`, each of the form `<object>.MetaData = <statement or definition>.MetaData`, all inside a create or alter compile:

| Line | Site | Statement |
|---|---|---|
| 4042 | `defaultValue.MetaData` | a column or scalar type default |
| 4215 | `node.Table.MetaData` | `create table` |
| 6495 | `node.CreateOperator.MetaData` | `create operator` |
| 6862 | `operatorValue.MetaData` | `create aggregate operator` |
| 7292 | `node.Constraint.MetaData` | `create constraint` |
| 8690 | `node.Reference.MetaData` | `create reference` |
| 9139 | `deviceScalarType.MetaData` | a device scalar type map |
| 9192 | `deviceOperator.MetaData` | a device operator map |
| 9329 | `node.ServerLink.MetaData` | `create server` |
| 9380 | `node.NewDevice.MetaData` | `create device` |
| 9925 | `node.Role.MetaData` | `create role` |

The twelfth site of the same shape, `node.View.MetaData` at `Compiler.cs:4415`, is **not** this ticket's: it has a second, separate consequence (a view's own tags losing to the tags it inherits from its source, which changes 14 checked-in derived forms) and it belongs to `bug-view-tags-lose-to-tags-inherited-from-the-source`, which names this ticket as its prereq. Leave line 4415 exactly as it is. Keeping it out is what makes that ticket's golden-form diff reviewable on its own.

## The fix

At each of the eleven lines, assign a copy:

```csharp
node.Table.MetaData = localStatement.MetaData == null ? null : localStatement.MetaData.Copy();
```

`MetaData.Copy()` (`src/engine/DAE/Language.D4/MetaData.cs:1507`) builds a new `MetaData` whose tags are copies, preserving each tag's `IsStatic` and `IsInherited`. The null-guarded ternary is the idiom this file already uses for the same purpose at `Compiler.cs:3919`.

Measured 2026-11-25 with exactly those eleven lines changed and nothing else: `dotnet build Dataphor.slnx` clean; all 416 cases of `FrontendDerivationGoldenTest.EveryDerivedFormMatchesItsGolden` still match their checked-in files (no derived form changes, which is the point of keeping the view site out); 204 cases green across `DeviceClassAttributeRerunTest`, `DeviceClassAttributeRollbackTest`, `SchemaCacheInvalidationTest`, `D4EmitterRoundTripTest`, `ApplicationTransactionTest`, `LibraryLoadingTest`, `CatalogRollbackSnapshotTest`, `FormCustomizationTest`, `StoreRebuildWorkflowTest`, `PreparedPlanRecompileTest` and `ConcurrentDDLTest`; and the nine coverage scripts `Tables`, `Views`, `References`, `Operators`, `Constraints`, `Devices`, `AggregateOperators`, `SecurityLibrary`, `ServerLink` green.

### Why not a copying setter on `Schema.Object.MetaData`

The sibling fix for class definitions put the copy in the six catalog-side `ClassDefinition` setters rather than at the compiler's assignment sites, so that a future assignment needs nothing. The same shape was considered here and rejected:

- `Schema.Object.MetaData` is the base property of *every* schema object, assigned in 89 places across 22 files, where `ClassDefinition` was six properties with a dozen call sites that could all be read in one pass. Many of those assignments are between schema objects rather than from a statement, and nothing shows that sharing is wrong there.
- It is a public settable property that a custom C# extension can assign, so a copying setter is itself a behaviour change for extensions.
- Two completed tickets in this family (`bug-remembered-statement-applies-a-changed-setting`, `bug-prepared-create-statement-executes-twice`) each required that `Schema.Object.MetaData` be left alone. Changing it now would reopen their reasoning as a side effect of a narrower fix.

The root cause is "a *create statement* hands the object it builds its own tag collection", and the compiler's create sites are exactly that set. Keep `Schema.Object.MetaData` a plain setter. Record the invariant in prose (below) instead of in a setter.

### What does not need changing, and why

- **Class definitions are already done.** `Schema.Device.ClassDefinition` and its five siblings clone in their setters (`src/engine/DAE/Schema/Device.cs:137`, `:243`, `:658`; `Schema/Operator.cs:109`; `Schema/ScalarType.cs` twice). So `node.NewDevice.ClassDefinition = localStatement.ClassDefinition` at `Compiler.cs:9381` does **not** share, despite reading as though it does.
- **The device arm appended to `bug-view-tags-lose-to-tags-inherited-from-the-source` on 2026-11-25 is retired by this ticket, not carried forward.** That arm claimed a measured consequence: a plan prepared from `create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" }`, executed, altered to 200, dropped and executed again, applying 200. Re-measured on 2026-11-25 against an in-process engine: the second execution applies **100**, and the stored script says `"MaxRowCount" = "100"`, because of the cloning setter above. The arm cited `DeviceClassAttributeRerunTest.TestPreparedPlanExecutedAgainAppliesItsOwnValue` as its measurement, but that case alters a class attribute through `alter device … alter class { create … }` and never runs a `create device` statement twice, so it measures something else. There is nothing to fix at `Compiler.cs:9381`.
- **The clone at `DDLNodes.cs:1138` stays.** It guards a different tree — the *alter* statement's own attribute object, which the create arm of `AlterClassDefinition` would otherwise install on the live object — and `DeviceClassAttributeRerunTest.TestOneProgramRunningTheSameAlterTwiceAppliesItsOwnValue` is its reproduction. Do not remove it.
- **Table and view columns already own their tags.** Every `Schema.TableVarColumn` constructor that takes metadata calls `MergeMetaData` (`src/engine/DAE/Schema/TableVar.cs:55`–`79`), which copies values into a new collection. No column site is in the table above.
- **`CompileCreateScalarTypeStatement` is the precedent that already does it right.** It never assigns the statement's `MetaData` to the scalar type: it inherits from the parent types (`Compiler.cs:5571`), which builds a collection of its own, and then merges the statement's tags on top (`:5717`). That is the shape every create statement should have.

## Edge cases & interactions

Each of these is to be covered by the implementation and will be checked at review. How each is verified is named; most are settled by inspection, and only the one behaviour below earns a test.

- **An assignment whose source is then mutated, expecting the object to see it.** A copying assignment breaks `X.MetaData = m; m.Tags.AddOrUpdate(...)`. Read the lines after each of the eleven sites for that pattern before changing it, and for the inverse (`X.MetaData.Tags…` mutated expecting the statement to see it — that one is the defect, and any code relying on it is a second finding worth reporting rather than preserving). Verified by inspection, per site.
- **A null metadata.** Most create statements carry no tags, so `localStatement.MetaData` is null on the common path; the ternary must keep assigning null rather than an empty `MetaData`, because several of these objects test `MetaData == null` and because an empty collection would start printing `tags { }` in emitted D4. Verified by inspection plus `D4EmitterRoundTripTest` and `ParserRoundTripCorpusTest`.
- **Static tags.** `tags` and `static tags` both reach these sites; `MetaData.Copy` preserves `IsStatic`, and a static tag must stay static or `Tag.Inherit` starts failing (`Error.Fail` on a static tag). Verified by inspection of `Tags.CopyTo` plus the `Domains` and `Types` coverage scripts, which use static tags.
- **`DAE.ObjectID` and the other engine-written tags.** `plan.GetObjectID(localStatement.MetaData)` is read from the statement *before* these assignments at several sites and `Schema.Object` writes `DAE.ObjectID`, `DAE.GeneratorID` and `DAE.IsGenerated` into its own collection afterwards. After the change those engine-written tags land on the object's copy and no longer on the statement, which is the intent; check that nothing reads them back off the statement. Verified by inspection and by `ObjectIDGeneratorTest`, `ObjectIDPerEngineTest`.
- **Catalog load.** Every object is rebuilt by compiling its stored create script at load, so these eleven lines run once per object loaded and now allocate one small collection each. The sibling ticket weighed the same cost for class definitions and recorded it as not worth measuring; do not add a tripwire unless something measurable turns up. Verified by inspection.
- **Rollback.** `CatalogDeviceSession`'s DDL undo records hold references to the live objects' metadata and mutate those collections in place; they must still reach the object and not a statement. Verified by `DeviceClassAttributeRollbackTest`, `CatalogRollbackSnapshotTest`, `ColumnChangeRollbackTest`.
- **Application transactions.** `Device.ApplicationTransaction/ApplicationTransaction.cs` assigns metadata when it builds an A/T copy of a table variable. It is not one of the eleven sites, so it is unchanged; confirm it is not reached through one of them. Verified by `ApplicationTransactionTest` and the `ApplicationTransactions` coverage script.
- **Four sites deliberately out of scope, to be checked and recorded rather than changed.** `renameExpression.MetaData` (`Compiler.cs:14881`), `renameNode.MetaData` (`:14933`, `:14939`) and `node.MetaData = adornExpression.MetaData` (`:15058`) hand a *plan node* an expression's metadata. These are expressions, not create statements, and no consequence has been measured. Read them once: if a mutation of the node's metadata could reach the expression, record a tripwire `NOTE:` at `:15058` saying so and say where in the handoff; if not, say that in the handoff and add nothing.

## The test

One case, which is the reproduction above and nothing more. The project's testing rule says a bug fix owes its reproduction at the lowest layer that shows it; the eleven assignments themselves are not worth a test each.

New fixture `tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs` over `InProcessTestFixture`, one `[Test]`: prepare the `create table … tags { Frontend.Title = "Own" }` statement, execute it, alter the tag to `"Altered"`, drop the table, execute the held plan again, and assert both that the live table variable's `Frontend.Title` is `"Own"` and that the create script the catalog emits for it carries `"Own"`. Both assertions, because the first is what the engine holds and the second is what a catalog rebuild would replay.

The fixture's summary should say why the statement has to be run through a prepared plan rather than a loop: a `create` node executed twice inside one program re-registers the catalog object the compiler built, so a loop would show the altered value whether or not the statement was edited, and would not be a reproduction of *this* defect. `DeviceClassAttributeRerunTest`'s three case summaries are the model for that kind of note.

## Documentation

- **Architecture.** `docs/architecture/catalog-and-store.md` owns schema objects and the catalog (1,084 words of its 1,500-word cap, so there is room). Add one short subsection — "Metadata and class definitions" is a reasonable heading — stating the invariant for both halves: a catalog object owns its metadata and its class definition, and never shares either with the statement that created it or with another catalog object; class definitions hold the invariant in six cloning setters, metadata holds it at the compiler's create sites, and a new create statement copies what it is given. Keep it to about 80 words and run `python tools/docs-size-check/check_size.py` afterwards. Do not add architecture text to the short root `docs/architecture.md`.
- **Migration notes.** One `done` row in the `## D4, catalog and libraries` section of `docs/migration-notes.md`: a statement that creates an object with tags is no longer edited when those tags are later altered, so a program that runs the same statement again creates the object with the tags the statement is written with. What to do: nothing, unless an application relied on the old behaviour to make a repeated create statement pick up an alteration, which was never documented.
- **No Known-defects row in the plan.** This is a pre-existing, non-port engine defect fixed in passing, which is the category the review of `bug-remembered-statement-applies-a-changed-setting` settled as migration-notes-only; nothing in the plan's WS2 text refers to this slug. State that in the handoff so it is not re-litigated.

## Things that cost time to find out

- `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` lists its sources explicitly. A new test file that is not added to it compiles into nothing and the test silently does not exist.
- `src/engine/DAE/Language.D4/MetaData.cs` has two implementations of `Tag` and `Tags`. `USESTRUCTTAG` is defined at the top of the file, so the **struct** version at lines 45–533 is the live one and the class version at 534–1412 is dead code. Read the first.
- `src/engine/DAE/Compiling/Compiler.cs` is CRLF with no byte-order mark, tab-indented, and 15,705 lines. Keep all of that; `sed` and `awk` in this environment strip the carriage returns, so edit it with a tool that works in bytes.

## TODO

- Read each of the eleven lines in the table and the code that follows it, for the assign-then-mutate patterns under *Edge cases & interactions*; change the eleven to the null-guarded copy.
- Read the four out-of-scope expression sites (`Compiler.cs:14881`, `:14933`, `:14939`, `:15058`) and either record a tripwire `NOTE:` at `:15058` or say in the handoff that none is needed.
- Add `tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs` with its one case, and its `<Compile Include>` entry in the test project.
- Confirm the fix by reverting it and watching the new case go red (`Expected: "Own" / But was: "Altered"`), then restoring it; say so in the handoff.
- `dotnet build Dataphor.slnx` — must be 0 errors and no new warning code.
- Targeted tests: the new case; `FrontendDerivationGoldenTest` (all 416 forms must still match — a changed golden here means the view site was touched or something else moved); the fixture ring and the nine coverage scripts named under *The fix*. Name the filters used in the handoff.
- Add the architecture subsection and the migration-notes row; run `python tools/docs-size-check/check_size.py`.
- In the handoff: say that the device arm of `bug-view-tags-lose-to-tags-inherited-from-the-source` was retired here with the re-measurement, so the reviewer of that ticket does not look for device work; and say plainly that only one of the eleven sites has a measured consequence and the other ten are the same line changed for the same reason.
