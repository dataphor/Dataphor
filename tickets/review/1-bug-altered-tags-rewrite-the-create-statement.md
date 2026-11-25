description: A statement that creates something can give it named settings. Changing one of those settings afterwards quietly rewrote the statement itself, so a program that runs the same statement a second time creates the thing with the changed setting instead of the one the statement is written with. Fixed.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/Schema.cs, tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/architecture/catalog-and-store.md, docs/migration-notes.md
repro: verified
difficulty: medium
----
## What landed

Eleven assignments in `src/engine/DAE/Compiling/Compiler.cs` now assign a copy of the metadata they are given instead of the collection itself, so a catalog object no longer shares its tag list with the statement that created it:

```csharp
node.Table.MetaData = localStatement.MetaData == null ? null : localStatement.MetaData.Copy();
```

The eleven lines, after the edit (the four added comment lines at the table site shift every later line by 4 from the numbers the implement ticket gave): `defaultValue` 4042, `node.Table` 4219, `node.CreateOperator` 6499, `operatorValue` (aggregate) 6866, `node.Constraint` 7296, `node.Reference` 8694, `deviceScalarType` 9143, `deviceOperator` 9196, `node.ServerLink` 9333, `node.NewDevice` 9384, `node.Role` 9929. `node.View.MetaData` (now 4419) is deliberately untouched and belongs to `bug-view-tags-lose-to-tags-inherited-from-the-source`, the next ticket.

**Only one of the eleven has a measured consequence.** The `create table` site is the one the reproduction exercises; the other ten are the same line changed for the same reason, at the same shape of site, with no separate measurement each. That is the honest shape of this change, and it is not worth eleven tests - see *What is not tested* below.

Two pieces of prose were added so the invariant is not re-discovered:

- A `<remarks>` block on `Schema.Object.MetaData` (`src/engine/DAE/Schema/Schema.cs:455`) stating that an object owns its metadata, that a create statement therefore assigns a copy, and *why the setter deliberately does not copy for you* - that last part is the reasoning two completed tickets depend on, and it was only in a ticket body before.
- A four-line comment at the `create table` site saying what the copy is for, pointing at that property. The other ten sites carry no comment: eleven copies of one sentence in a 15,705-line file is noise, and `.Copy()` is not cryptic once the property says what the rule is.

Documentation: a 93-word paragraph in `docs/architecture/catalog-and-store.md` (that page's own convention is a bold lead-in, not an `##` heading, so it follows that), and one `done` row in the `## D4, catalog and libraries` table of `docs/migration-notes.md`. `python tools/docs-size-check/check_size.py` reports 13 files, 0 over the limit. No Known-defects row in the plan: this is a pre-existing, non-port engine defect fixed in passing, which is the category the review of `bug-remembered-statement-applies-a-changed-setting` settled as migration-notes-only, and nothing in the plan's WS2 text refers to this slug.

## The reproduction, and how to re-run it

`tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs`, one case, `TestPreparedCreateStatementExecutedAgainAppliesItsOwnTags`. It prepares `create table TagRerunTable { ID : Integer, key { ID } } tags { Frontend.Title = "Own" }`, executes it, alters the tag to `"Altered"`, drops the table, executes the held plan again, and asserts both that the live table variable carries `"Own"` and that the create script the catalog emits for it says `"Own"` - the first is what the running engine holds, the second is what a catalog rebuild would replay.

Confirmed both ways on 2026-11-25. With the eleven lines reverted the case fails with exactly `Expected: "Own" / But was: "Altered"`; with them in place it passes. The fixture's remarks explain why the statement has to go through a prepared plan: a create node executed twice inside one program re-registers the object the compiler built, so a loop would report the altered value whether or not the statement was edited, and the same text executed twice no longer reaches a cached plan (a drop evicts it, so the second run recompiles from the text - `SchemaCacheInvalidationTest.DropRoleInvalidatesCachedPlans` is that route and is green either way).

It derives from `DeviceClassAttributeFixture`, not from `InProcessTestFixture` as the implement ticket said, because the two helpers it needs - `CatalogObject<T>` and `Script` - live there, and `SchemaCacheInvalidationTest` already derives from it for the same two helpers. Moving them down to `InProcessTestFixture` would be a wider change than this ticket earns; if you would rather have them there, that is a judgement call worth making explicitly.

## What was validated, and with what

`dotnet build Dataphor.slnx`: 0 errors, no new warning code.

| What | Filter | Result |
|---|---|---|
| The new case | `--filter "Name=TestPreparedCreateStatementExecutedAgainAppliesItsOwnTags"` | passes; red before the fix with the expected message |
| The golden derived forms | `--filter "FullyQualifiedName~FrontendDerivationGoldenTest"` | 3 cases pass; `EveryDerivedFormMatchesItsGolden` walks all 416 derivations and no golden file changed (`git status` is clean under `tests/goldens`) |
| The fixture ring | one `--filter` over `CreateStatementTagRerunTest`, `DeviceClassAttributeRerunTest`, `DeviceClassAttributeRollbackTest`, `SchemaCacheInvalidationTest`, `D4EmitterRoundTripTest`, `ApplicationTransactionTest`, `LibraryLoadingTest`, `CatalogRollbackSnapshotTest`, `FormCustomizationTest`, `StoreRebuildWorkflowTest`, `PreparedPlanRecompileTest`, `ConcurrentDDLTest`, `ColumnChangeRollbackTest`, `ObjectIDGeneratorTest`, `ObjectIDPerEngineTest`, `ParserRoundTripCorpusTest` | 577 passed, 0 failed, 19 skipped, 1 m 38 s |
| Coverage scripts | `Name=` over `Tables`, `Views`, `References`, `Operators`, `Constraints`, `Devices`, `AggregateOperators`, `SecurityLibrary`, `ServerLink`, `Domains`, `Types`, `ApplicationTransactions` | 11 pass; `Domains` is a recorded known failure (`test-pre-3-1-scripts-policy`, unrelated and pre-existing) |
| Derivation and reference scripts | `Name=` over `Derivation`, `DerivationReferenceGroups`, `TableCallsWithStackReferences`, `DerivationMaps`, `ForeignSuperKeysAndConstantReferences`, `SerializationReferences` | 3 pass, 3 recorded known failures (same policy) |

The last row is not in the implement ticket's list and was added for a reason worth knowing: `DerivationReferenceGroups.d4` puts the deprecated tag `Storage.Enforced` on reference definitions inside `create table`, which is the one changed site whose behaviour is not purely "the statement keeps its own value" - see the next section.

## The one behaviour change that is not simply "the statement is left alone"

`GetEnforced`, `GetIsSparse` and `ProcessIsClusteredTag` (`Compiler.cs:3143`, `:3158`, `:3173`) rewrite a deprecated tag spelling by removing and adding tags *in the collection they are handed*, and warn once about each. `GetEnforced` is called with `node.Constraint.MetaData` and `node.Reference.MetaData` straight after those two assignments, so before this change the rewrite landed in the create statement's own tree; now it lands on the object's copy.

What that means, checked rather than assumed:

- **Nothing an application reads changes.** The object ends in the same state either way: the copy carries `Storage.Enforced`, `GetEnforced` removes it and adds `DAE.Enforced` with the inverted value, exactly as before. The emitted create script comes from the object, so it is unchanged, and `DerivationReferenceGroups` and `References` are green.
- **The deprecation warning now repeats.** The statement keeps the deprecated spelling, so a second compile of the same statement warns again where before it did not. That is arguably the correct behaviour - the statement really does still contain the old spelling - and no test asserts on the warning count, but it is a visible difference and the one thing in this change a reviewer should decide they are happy with. `tests/libraries/TestFramework.Coverage.Scripts/Documents/DerivationReferenceGroups.d4` and `tests/libraries/TestFramework.Coverage.Base/Documents/Tables.d4` are where the deprecated tags live if you want to see it.

## Edge cases from the implement ticket: what each check found

- **Assign-then-mutate, per site.** Every reference to the metadata after each of the eleven assignments is a *read* through the object (`MetaData.GetTag`, `plan.GetObjectID`, `GetEnforced`, `Schema.Operator.GetLocator`). The only mutations are `node.Table.MetaData.Tags.Update` for the three `DAE.Is*Remotable` tags (`Compiler.cs:4283`-`4301`), which read the tag off the object and write it back to the object - now to the copy, which is the intent. No site mutates the source after assigning it. Every read of `localStatement.MetaData` at the eleven sites happens *before* the assignment, including each `plan.GetObjectID(localStatement.MetaData)`, so the engine-written `DAE.ObjectID`, `DAE.GeneratorID` and `DAE.IsGenerated` tags now land only on the object, and nothing reads them back off the statement.
- **Null metadata.** The ternary keeps assigning null. `D4EmitterRoundTripTest` and `ParserRoundTripCorpusTest` are green, so nothing started emitting an empty `tags { }`.
- **Static tags.** `MetaData.Copy` goes through `Tags.CopyTo` to `Tag.Copy()`, which reconstructs the tag with its inherited and static flags intact (`MetaData.cs:90`-`:104`). `Copy()` does not carry `MetaData.Line`/`LinePos`; nothing in `src/` reads either, and the same `Copy()` is this file's existing idiom at `Compiler.cs:3919`. Exercised by `Tables`, `Types`, `AggregateOperators`, `References` and `ApplicationTransactions`, which all use `static tags` and are green. Note that `Domains`, which the implement ticket named for this, is a recorded known failure and proves nothing here - the other five carry it.
- **Catalog load.** One small allocation per loaded object, as expected; no tripwire added, per the ticket.
- **Rollback and application transactions.** `DeviceClassAttributeRollbackTest`, `CatalogRollbackSnapshotTest`, `ColumnChangeRollbackTest`, `ApplicationTransactionTest` and the `ApplicationTransactions` script are green. The application-transaction path is not reached through one of the eleven in a way that matters: `ApplicationTransaction.CopyTableVar` builds a *fresh* statement from `sourceTableVar.EmitStatement(EmitMode.ForCopy)`, and `TableVar.EmitStatement` already copies (`TableVar.cs:2831`, `:2843`), so the invariant was already held in that direction.
- **The four out-of-scope expression sites.** Read, and **no tripwire was added** - the ticket asked for one only if a mutation of the node's metadata could reach the expression, and none can. `AdornNode` only reads its `MetaData` (`AdornNode.cs:337`-`:367`), merges it with `_tableVar.MergeMetaData`, which copies, and emits it with `MetaData.Copy()` at `:532`. `RenameNode` and `RowRenameNode` likewise only merge. `Compiler.cs:14881` is parse-tree-to-parse-tree inside the compiler, and that metadata reaches columns through `MergeMetaData`.

## Found while implementing, and filed rather than folded in

`tickets/backlog/bug-altered-key-and-order-tags-rewrite-the-create-statement.md` - **the same defect, verified, at two sites the implement ticket's table does not cover**: a table's *keys* and *sort orders*. `CompileKeyDefinition` (`Compiler.cs:3188`) and `CompileOrderColumnDefinitions` (`:3425`) hand the live `Schema.Key`/`Schema.Order` the statement's own tag collection, and `AlterKeys`/`AlterOrders` (`DDLNodes.cs:1177`, `:1224`) mutate it in place.

Measured on 2026-11-25 with the eleven-line fix already in place, same prepared-plan probe: `create table KeyTagProbe { ID : Integer, key { ID } tags { Frontend.Title = "Own" } }` prepared, executed, altered to `"Altered"`, dropped and executed again gives a key carrying `"Altered"`, and the emitted script reads `key { .ID } tags { Frontend.Title = "Altered" }`. The `order { ID } tags { … }` arm gives the same. So the implement ticket's claim that "the compiler's create sites are exactly that set" is not quite right, and this handoff does not pretend otherwise.

It was **filed, not fixed here**, for one reason worth your agreement or disagreement: keys and orders feed form derivation, and part of the value of this change is that it moved none of the 416 golden derived forms, which is what keeps the next ticket's golden diff reviewable on its own. Widening the diff to two more sites would have given that up for a defect that is not a regression and that nobody is waiting on. The filed ticket carries both measurements, the `severity`/`likelihood`/`tradeoffs` triage fields, the deprecation-warning interaction above (it applies at the key site too), and a third site of the same shape recorded as *unmeasured* rather than claimed (`AdornNode.cs:389`, a reference built from an `adorn` expression's `references` clause). If you think it belongs in `fix/` instead of `backlog/`, that is a one-line move.

## The device arm of the next ticket: retired, re-measured here

`bug-view-tags-lose-to-tags-inherited-from-the-source` had an arm appended on 2026-11-25 claiming that a prepared `create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" }`, executed, altered to 200, dropped and executed again applied 200. **Re-measured here, independently, on 2026-11-25: it applies 100**, and the emitted script reads `"MaxRowCount" = "100"`. The cause is that `Schema.Device.ClassDefinition`'s setter already clones (`src/engine/DAE/Schema/Device.cs:137`), so `Compiler.cs:9384` does not share despite reading as though it does. There is nothing to fix at that line, and the clone at `DDLNodes.cs:1138` must stay - `DeviceClassAttributeRerunTest.TestOneProgramRunningTheSameAlterTwiceAppliesItsOwnValue` is its reproduction. That ticket already records the retirement in its own body, so **the reviewer of the next ticket should not go looking for device work.**

## What is not tested, deliberately

- **Ten of the eleven sites.** One reproduction covers the class; a test per create statement would be eleven tests of one line. If you disagree, the cheapest second case to add would be `create operator`, because an operator's metadata is read back into six of its own characteristics (`Compiler.cs:6576`-`:6581`) and so has the most to lose from a bad copy - but those reads are already green through the `Operators` and `AggregateOperators` scripts.
- **The deprecation-warning difference.** No test asserts how many times a deprecated tag warns, before or after. Pinning it would mean deciding that the repeated warning is the specified behaviour, which is the reviewer's call.
- **The key and order arms.** They fail today, and a failing test is not what an implement stage leaves behind; they belong with the ticket that fixes them, in the fixture this ticket added, which is what that ticket says.

## Where to look first

`git diff` is 11 one-line changes, one 4-line comment, one `<remarks>` block, one documentation paragraph and one migration-notes row; plus one new test file and one new backlog ticket. The three things that actually need a second pair of eyes are the deprecation-warning change, the decision to file the key and order arms rather than fix them, and whether `CreateStatementTagRerunTest` deriving from `DeviceClassAttributeFixture` is the right home for it.
