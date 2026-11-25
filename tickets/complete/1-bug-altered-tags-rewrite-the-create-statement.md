description: A statement that creates something can give it named settings. Changing one of those settings afterwards quietly rewrote the statement itself, so a program that runs the same statement a second time creates the thing with the changed setting instead of the one the statement is written with. Fixed, reviewed and landed.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/Schema.cs, tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs, tests/DAE.Server.Tests/DeviceClassAttributeFixture.cs, docs/architecture/catalog-and-store.md, docs/migration-notes.md
----
## What landed

Eleven assignments in `src/engine/DAE/Compiling/Compiler.cs` assign a copy of the metadata they are given instead of the collection itself, so a catalog object no longer shares its tag list with the statement that created it:

```csharp
node.Table.MetaData = localStatement.MetaData == null ? null : localStatement.MetaData.Copy();
```

The sites, by the object they build: a table, a default, an operator, an aggregate operator, a catalog constraint, a reference, a device's type map, a device's operator map, a server link, a device, a role. `create view` is deliberately untouched and belongs to `bug-view-tags-lose-to-tags-inherited-from-the-source`.

The invariant behind them is written down twice: as a `<remarks>` block on `Schema.Object.MetaData` (`src/engine/DAE/Schema/Schema.cs`), which also says why the setter deliberately does not copy for the caller, and as a paragraph in `docs/architecture/catalog-and-store.md`. One comment at the `create table` site points at that property; the other ten carry none, because eleven copies of one sentence in a 15,700-line file is noise.

The reproduction is `tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs`, one case: prepare `create table … tags { Frontend.Title = "Own" }`, execute it, alter the tag to `"Altered"`, drop the table, execute the held plan again, and assert both that the live table variable carries `"Own"` and that the create script the catalog emits for it says `"Own"`.

`docs/migration-notes.md` carries one `done` row for the change.

## Review findings

Read the diff of `ticket(implement): bug-altered-tags-rewrite-the-create-statement` first, then the handoff. Everything below was found by reading the code around the eleven sites and by running the tests named at the end.

### Confirmed, and needed no change

- **The copy is a real deep copy.** In the configuration this engine compiles (`#define USESTRUCTTAG` at the top of `src/engine/DAE/Language.D4/MetaData.cs`), `Tag` is a **struct**, so `MetaData.Copy()` through `Tags.CopyTo` copies every tag by value, with its static and inherited flags intact. The handoff's claim is right, and it is stronger than it says: because a tag is a value type, the only shared mutable thing was ever the `Tags` collection object, which is exactly what the eleven lines stop sharing.
- **`X == null ? null : X.Copy()` is this repository's idiom, not a new one.** Forty-odd existing sites in `src/engine` spell it the same way, including the line immediately above one of the eleven. There is no helper to extract to; a static null-safe `MetaData.Copy(MetaData)` would be the odd one out.
- **No site mutates the source after assigning it.** Checked each of the eleven. Every later touch of the metadata is a read through the object (`MetaData.GetTag`, `GetEnforced`, `Schema.Operator.GetLocator`) except the three `DAE.Is*Remotable` updates at the table site, which read the tag off the object and write it back to the object - now to the copy, which is the intent.
- **The engine's own tags land where they should.** `plan.GetObjectID` *removes* `DAE.ObjectID` from the metadata it is handed, and at every one of the eleven sites it is called before the copy is taken, so the identifier is consumed out of the statement exactly as before and the copy never carries it. `SaveObjectID`, `SaveGeneratorID` and `SaveIsGenerated` write into the object's own collection, which is now not the statement's.
- **Null metadata, and `MetaData.Line`/`LinePos`.** The ternary keeps assigning null, so nothing started emitting an empty `tags { }` - `D4EmitterRoundTripTest` and `ParserRoundTripCorpusTest` are green. `Copy()` does not carry `Line`/`LinePos`; a repository-wide grep for a read of either field finds none, so they are dead and the omission costs nothing.
- **The sites that merge rather than assign were already safe, and are the majority.** A column's tags, a representation's, a property's, a special's, a scalar type's, a conversion's, an event handler's and a row or transition constraint's all go through `Schema.Object.MergeMetaData`, which adds into a collection of the object's own. Nothing to change there, which is why the eleven are the whole assign-the-collection set in the compiler apart from the view, the keys and the orders.
- **The reproduction is a reproduction.** Verified independently rather than taken on trust: with the `create table` line put back to the shared assignment the case fails with `Expected: "Own" / But was: "Altered"`, and with the copy restored it passes. The line was restored and the build and the case re-run green afterwards.
- **The reproduction is the only test the change owes, and it is at the right layer.** One case for eleven lines of one shape is the right count; the alternative - a case per create statement - would be eleven tests of one line. The fixture's remarks explain why a prepared plan and not a loop is the layer that shows the defect, which is the non-obvious part. No test was cut and none was added.

### Fixed in this pass

- **The architecture paragraph stated an invariant as fact that the code does not yet hold everywhere.** "A catalog object … never shares either" reads as a description of the current state, and a table's keys, its sort orders and a view's own tags still share. Reworded to the rule it is ("must never share"), with the two mechanisms that hold it named concretely. The page is still under the 1,500-word cap (`python tools/docs-size-check/check_size.py`: 13 files, 0 over).
- **The migration-notes row was silent on two things an application can notice.** It now says that the tags on a table's keys and sort orders are *not* covered - a reader of the old row would have concluded all create-statement tags were fixed - and that the deprecated-tag warning described below now repeats.
- **`DeviceClassAttributeFixture`'s own documentation went stale in the implement commit.** It said it was "what the fixtures over class definitions need to see" and named only `SchemaCacheInvalidationTest` as the exception; a third fixture now derives from it for two helpers that have nothing to do with class definitions. Its summary and remarks now say what it actually is, name both borrowing fixtures, and record the condition on which the two helpers should move down to `InProcessTestFixture` - which is the answer to the question the handoff left open: the home is right for now, the name is not, and renaming the file is a move that needs its own commit for less than it is worth.

### Weighed and accepted, recorded at the site

- **The deprecated-tag rewrite now repeats its warning.** `GetEnforced`, `GetIsSparse` and `ProcessIsClusteredTag` replace `Storage.*` tag spellings with their `DAE.*` equivalents *in the collection they are handed*, and warn once per compile. At the constraint and reference sites that collection is now the object's copy, so the statement keeps the deprecated spelling and a plan prepared once and executed more than once warns on each compile where before only the first compile warned. Checked rather than assumed: nothing an application reads out of the catalog changes, because the object still ends with the rewritten tag and the emitted create script comes from the object - `References`, `Constraints`, `Tables` and `DerivationReferenceGroups`, the scripts that carry deprecated tags, are all green. Accepted: the statement really does still contain the old spelling, so warning again is the more truthful behaviour. Recorded as an accepted-tradeoff `NOTE:` on `GetEnforced` with its revisit condition, and in the migration-notes row. No test pins the warning count, before or after, and none was added - pinning it would decide that once-per-compile is specified, which is a call for an owner and not for this ticket.

### Filed

- **`tickets/backlog/debt-compiling-a-statement-must-not-change-the-statement.md`** - the class-level rule behind this ticket and the three around it, and the guard that is missing. Filed rather than a point fix because reading around the eleven sites turned up two more instances of the same shape, neither of them about tags, and the pattern of each ticket finding the next one by hand is what a guard would end. The two: `CompileDeviceOperatorMap` writes the device's default class definition into the statement's own map definition, so a second compile of the same `alter device` sees a non-null definition and demands the host-implementation right that the first compile deliberately skipped; and `CompileCreateScalarTypeStatement` writes the representations, default, constraints, specials and parent class definition that a `create type … like` copies into the statement's parse tree, so a second run builds the type from the first run's snapshot of the parent. Both are read from the code, marked `repro: static`, and the ticket names what would confirm each. The ticket also records the one edit the compiler is allowed to make to a statement - consuming `DAE.ObjectID` - which any guard has to permit. The keys-and-orders ticket now points at it.
- **Nothing else was filed.** `bug-altered-key-and-order-tags-rewrite-the-create-statement` (filed by the implementer, verified, with both measurements) was re-read and left in `backlog/`: it is not a regression, nobody is waiting on it, and the implementer's reason for not folding it into this diff holds - keys and orders feed form derivation, and keeping all 416 golden derived forms unmoved is what makes the next ticket's golden diff reviewable on its own. The handoff's offer to move it to `fix/` is declined on the same grounds.

### Checked and empty

- **Resource cleanup, error paths and concurrency:** nothing in the diff opens, holds or releases anything; the ternary cannot throw; the copy is taken inside the same `try` as the assignment it replaced. No findings, and none expected from a change of this shape.
- **Rollback, application transactions and catalog load:** green (`DeviceClassAttributeRollbackTest`, `CatalogRollbackSnapshotTest`, `ColumnChangeRollbackTest`, the `ApplicationTransactions` script). The application-transaction path was already holding the invariant from the other direction - `ApplicationTransaction.CopyTableVar` builds a fresh statement from `EmitStatement(EmitMode.ForCopy)` and `TableVar.EmitStatement` already copies.
- **Performance:** one `MetaData` and one `Tags` allocation per created object, per compile. Not measured and not worth measuring at that size; no tripwire, as the implement ticket decided.
- **File size and comment hygiene:** `Compiler.cs` grew by 10 lines including this pass's `NOTE:`; its size is a standing fact of this file and no part of this change. The four-line comment at the table site says why the copy exists and points at the property that owns the rule, which is what a comment cannot be read off the code. No narrated statements to extract.
- **The device arm of the next ticket:** the implementer re-measured and retired it (the `ClassDefinition` setter already clones), and that ticket records the retirement in its own body. Confirmed as read; no work here.

## What was run

`dotnet build Dataphor.slnx`: 0 errors, no new warning code, before and after this pass's edits.

| What | Filter | Result |
|---|---|---|
| The implement stage's fixture ring, plus the recompile and round-trip neighbours | one `FullyQualifiedName~` filter over `CreateStatementTagRerunTest`, `DeviceClassAttributeRerunTest`, `DeviceClassAttributeRollbackTest`, `SchemaCacheInvalidationTest`, `D4EmitterRoundTripTest`, `PreparedPlanRecompileTest`, `CatalogRollbackSnapshotTest`, `ColumnChangeRollbackTest`, `FrontendDerivationGoldenTest`, `ParserRoundTripCorpusTest` | 535 passed, 0 failed, 19 skipped, 46 s |
| Coverage scripts for every engine area the eleven sites touch | `Name=` over `Tables`, `Views`, `References`, `Operators`, `Constraints`, `Devices`, `AggregateOperators`, `SecurityLibrary`, `ServerLink`, `Types`, `ApplicationTransactions`, `Derivation`, `DerivationReferenceGroups`, `TableCallsWithStackReferences`, `DerivationMaps`, `ForeignSuperKeysAndConstantReferences`, `SerializationReferences`, `Keys`, `Orders` | 14 passed, 0 failed, 3 skipped, 51 s. The three skipped are recorded known failures owned by `test-pre-3-1-scripts-policy` |
| The reproduction, negated and restored | `Name=TestPreparedCreateStatementExecutedAgainAppliesItsOwnTags` | fails `Expected: "Own" / But was: "Altered"` with the fix reverted; passes with it in place |
| After this pass's edits | the core ring plus the deprecated-tag scripts | 27 passed, 0 failed, 33 s; `git status` clean under `tests/goldens`, so no derived form moved |

The three part commands were not run: this change is eleven one-line edits inside the compiler's create statements plus prose, and it does not touch the build, the test infrastructure, the coverage runner or the lexer, parser or compiler paths every script goes through. CI runs the whole suite in Debug and Release on the push.

No `tickets/.pre-existing-error.md` was written: every failure seen was one of the three already recorded in `tickets/.pre-existing-known.md` under `test-pre-3-1-scripts-policy`.
