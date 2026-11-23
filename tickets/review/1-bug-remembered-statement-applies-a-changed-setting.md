description: Running the same statement twice could apply the wrong value the second time, because the live storage device kept the statement's own copy of one of its settings and a later statement edited that copy. The statement now keeps its own copy, and the catalog objects that hold a class definition now store a copy of whatever is assigned to them.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Schema/Device.cs, src/engine/DAE/Schema/Operator.cs, src/engine/DAE/Schema/ScalarType.cs, tests/DAE.Server.Tests/DeviceClassAttributeFixture.cs, tests/DAE.Server.Tests/DeviceClassAttributeRerunTest.cs, tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
----
## What was wrong, in one paragraph

Some D4 objects are backed by a .NET class, and the statement that makes or alters one carries a *class definition*: a class name plus named attributes applied to the live object as properties. A storage device's settings are of that kind - a memory device's `MaxRowCount` row limit, a SQL device's server name. `alter device D alter class { create "MaxRowCount" = "100" }` used to put the statement's own attribute object into the device's live class definition, so the device and the statement shared it; a later `alter ... alter "MaxRowCount" = "200"` wrote `200` into that shared object, which was also writing `200` into the first statement. The first statement, run again, then applied 200.

## What changed

**The fix.** `AlterNode.AlterClassDefinition` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1007`) adds `attributeDefinition.Clone()` instead of `attributeDefinition`. One line, with a comment saying why. That alone is what makes both reproductions below pass.

**The hardening the ticket asked for.** Six catalog-side `ClassDefinition` setters now store `value == null ? null : (ClassDefinition)value.Clone()`, each with a `<remarks>` stating that the object owns its definition and that callers mutate it through the getter: `Schema.DeviceOperator` (`Schema/Device.cs:137`), `Schema.DeviceScalarType` (`Device.cs:243`), `Schema.Device` (`Device.cs:661`), `Schema.OperatorBlock` (`Schema/Operator.cs:112`), `Schema.ScalarType.FromClassDefinition` (`Schema/ScalarType.cs:1394`) and `Schema.ScalarType.ClassDefinition` (`ScalarType.cs:1418`). The compiler's nine assignments are untouched, as the ticket directed - the setters cover them, and the next assignment site added cannot forget.

Checked before making the setters clone, because a cloning setter silently breaks an assign-then-mutate-the-source caller: every assignment to those six properties in `src/`, `tests/` and `libraries/` either reads the definition back through the getter afterwards (`Engine.cs:759` and `:770` add an attribute that way; `Compiler.cs:9188` and `:9218` call `CheckClassDependency`) or never touches the source again. No code compares a `ClassDefinition` by reference, no type derives from `ClassDefinition` (so the `virtual Clone()` returning a base instance loses nothing), and the rollback undo in `CatalogDeviceSession.AlterClassDefinitionInstruction` mutates the definition it was handed from the getter, so it still reaches the stored one.

**`Schema.Object.MetaData` was left exactly as it is**, as the ticket required. The 14 golden forms the ticket warns a copying metadata setter changes are untouched - the golden pass is green (below).

**Migration note.** One row under *D4, catalog and libraries* in `docs/migration-notes.md`: the D4-visible behaviour change, plus the six properties a custom C# extension could notice.

## Tests

Added, both as the reproduction of this bug at the lowest layer that shows it (an in-process engine, no database server, under 100 ms each):

- `DeviceClassAttributeRerunTest.TestStatementRunAgainFromThePlanCacheAppliesItsOwnValue` - the same statement text run twice through `IServerProcess.Execute`, with an alter and a drop in between; the second run must apply 100, and the device must script out carrying `"MaxRowCount" = "100"`. Covers the plan-cache route a client and the Dataphor IDE take.
- `DeviceClassAttributeRerunTest.TestPreparedPlanExecutedAgainAppliesItsOwnValue` - the same sequence through a plan the caller got from `IServerProcess.PrepareStatement` and executes twice. This route does not consult the plan cache, so it covers statement reuse independently of it.

Both were confirmed to fail without the one-line fix (reverted it, rebuilt, ran: `Expected: 100 / But was: 200` on each) and to pass with it. Neither opens a transaction; every statement commits.

**No test was added for the six cloning setters.** That sharing was not observable through any sequence the fix stage tried - a create statement's class definition is only read again by re-running that create statement, which hits the separate defects of `implement/bug-recreated-device-comes-back-as-the-one-that-was-dropped` and `blocked/bug-prepared-create-statement-executes-twice`. A test would therefore have to assert the clone itself rather than a behaviour, which is a test of the setter's implementation. Flagging it rather than papering over it: the setters ship with no test of their own, covered only by the existing fixtures that create and load every kind of catalog object.

**One fixture was restructured, not renamed.** The ticket offered either renaming `DeviceClassAttributeRollbackTest` or a new fixture reusing its helpers; renaming would mean moving a file and editing it in one commit, which this project's rules separate. So the six device-probing helpers moved into a new `DeviceClassAttributeFixture` base (the shape `FrontendFixture` already uses here), `DeviceClassAttributeRollbackTest` derives from it and keeps its five cases unchanged, and the new cases are in `DeviceClassAttributeRerunTest`. Both new files are listed in `DAE.Server.Tests.csproj`, since default compile items are off in this repo.

## What was run

`dotnet build Dataphor.slnx` - 0 errors, warning count unchanged. Then, per this project's implement-stage rule (targeted tests; CI runs the full suite after the push):

- `--filter "Name=DeviceClassAttributeRerunTest|Name=DeviceClassAttributeRollbackTest|Name=CatalogRollbackSnapshotTest|Name=ColumnChangeRollbackTest|Name=D4EmitterRoundTripTest|Name=DeferredDeviceStopTest|Name=SchemaCacheInvalidationTest|Name=LibraryLoadingTest|Name=JSONObjectConveyorTest|Name=PartialCommitTest"` - 172 passed, 1 m 06 s. The DDL-alter and rollback fixtures over the changed site, plus the emitter round trip and the fixtures that create devices and scalar types.
- `--filter "Name=Devices|Name=SerializationDevices|Name=Types|Name=SerializationTypes|Name=LibraryTypes|Name=Operators|Name=SerializationOperators|Name=AggregateOperators|Name=SerializationAggregateOperators|Name=OperatorSourceText|Name=OperatorText|Name=SystemCatalog|Name=SystemCatalogOperators|Name=ScalarAllocation|Name=MinMaxOperators"` - 15 coverage scripts, all pass, 1 m 08 s. These are the coverage scripts for every engine area the six setters touch.
- `--filter "Name=FrontendDerivationGoldenTest|Name=FormCustomizationTest"` - 30 passed, 17 s. All 416 derived forms still match their goldens.
- `--filter "Name=StoreRebuildWorkflowTest|Name=ConcurrentDDLTest|Name=MemoryDeviceConcurrencyTest|Name=CatalogStoreTest|Name=BufferedOperatorColumnOwnershipTest|Name=TableAssignmentEvaluationTest"` - 38 passed, 2 m 56 s. The catalog rebuild-from-scripts path is the one that assigns these setters for every object it loads.

No failures, so nothing was written to `tickets/.pre-existing-error.md`.

**The three-part suite run the ticket's TODO asks for was deliberately not done**, and it is the one thing a reviewer should weigh. This project's rules (owner, 2026-11-23) set implement-stage testing to the targeted set above and reserve the three parts for changes to the build or test infrastructure, the package or coverage-manifest files, or the lexer, parser or compiler; a `Schema` setter change is not on that list, and CI runs the whole suite in Debug and Release after the runner pushes. The ticket's own measurement of part 1 at 9 m 03 s and 9 m 45 s against the runner's ten-minute idle limit is the other half of the reason. If the reviewer judges the six setters cross-cutting enough to want the parts locally, that is the review stage's call.

## For the reviewer

- **Unmeasured:** the clone the setters add on catalog load. The ticket records that no slowdown was observed when they were built; this stage did not measure it either way. It is one small allocation per catalog object loaded, on a path that already parses and compiles each object's persisted create script. `PerfBaselineTest` does not cover catalog load today - `research-catalog-load-profile` is the scenario named for it in that fixture.
- `Compiler.cs:4861` now clones twice (`scalarType.ClassDefinition = (ClassDefinition)...ClassDefinition.Clone()` into a cloning setter). Harmless, and left alone because the ticket said to leave the compiler's assignments as they are; a reviewer may prefer to drop the explicit `.Clone()` there now that the setter carries it. `Compiler.cs:5591` clones into a *statement*, not a catalog object, and must stay.
- `implement/bug-recreated-device-comes-back-as-the-one-that-was-dropped` names this ticket as its prereq and touches the same file; nothing in this change conflicts with the plan-cache timestamp bump it will add to `DropDeviceNode`.
- Source hygiene: every edited file kept its tabs, CRLF endings and byte-order-mark state. The one new row in `docs/migration-notes.md` is CRLF, matching the 160 CRLF lines of that file rather than the single pre-existing LF line above it, which was left as it is.
