description: Keys and orders used to take a catalog identifier from a counter shared by the whole program whenever anything created one, including the Windows client where there is no catalog at all; this step makes them take the identifier from the engine that owns the catalog, or none where there is no engine.
prereq: engine-object-id-generator
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/JoinNodes.cs, src/engine/DAE/Runtime.Instructions/CatalogNodes.cs, src/engine/DAE/Runtime.Instructions/TableNode.cs, src/engine/DAE/Runtime.Instructions/ExtendNode.cs, src/engine/DAE/Runtime.Instructions/OrderNodes.cs, src/engine/DAE/Runtime.Data/NativeTable.cs, src/engine/DAE/Runtime.Data/ObjectMarshal.cs, src/engine/DAE/Device.Memory/MemoryDevice.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/Server/Runtime.Instructions/CatalogNodes.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, libraries/SQLDevice/Source/SQLDevice.cs, libraries/Frontend/Source/Derivation/InterfaceBuilder.cs, libraries/Frontend/Source/Elaboration/Elaboration.cs, src/client/DAE.Client/DataSet.cs, src/client/DAE.Client/TableDataSet.cs, src/windows/DAE.Client.Controls/DBGrid.cs, docs/migration-notes.md
----
## What landed

The second of four steps towards `bug-object-id-counter-is-process-wide`. `engine-object-id-generator` put a generator on `Engine` and gave compiled code `Plan.GetNextObjectID()`; this step moves every place in the tree that builds a `Schema.Key` or a `Schema.Order` onto it, and gives the places where there is no engine at all an explicit "no identifier" instead.

The rule the ticket set, applied as written: **a key or an order constructed where a `Plan` or a `Program` is in scope takes its identifier from that engine; everywhere else it gets none.** "None" is the new `Schema.Object.UnassignedID`, which is `0` - a generator allocates from `1` upwards and `-1` already means "no containing object", so `0` was free.

Every converted engine-path site still draws from the one shared generator instance the transition keeps, in the same order, so it gets the number it got before. The sites that *stop* allocating shift everything allocated after them - see *The one behavioural change* below.

### The constant and the constructors

- `Schema.Object.UnassignedID` (`src/engine/DAE/Schema/Schema.cs`), with a doc comment saying what it means, which kinds of site legitimately carry it, and why `0` is safe.
- The three guards `engine-object-id-generator` added now test against it by name rather than against a bare `0`, so the whole set is one grep: `Schema.Object.SaveObjectID`, `ServerCatalogDeviceSession.InternalInsertPersistentObject`, `CatalogDeviceSession.CacheCatalogObject`.
- The `Key` and `Order` constructors that take no identifier now chain to `UnassignedID` instead of allocating, and each gained a counterpart that takes one first. Five of those counterparts are new: `Order(int, Order)`, `Order(int, Order, bool)`, `Order(int, Key)`, `Key(int, TableVarColumn[])`, `Key(int, MetaData, TableVarColumn[])`. The no-identifier forms are kept deliberately: the client and the data-access components use them and do not build through `Dataphor.slnx`, so removing one would break them with no compiler error in the main build.
- `Key.EmitStatement(EmitMode.ForStorage)` and `Order.EmitStatement(ForStorage)` call `SaveObjectID`, which is what makes an unnumbered key or order that belongs to a catalog fail loudly rather than silently.

### Where the sites went

165 construction sites in the tree; 139 of them are live. Of those, **114 now allocate** (112 changed here, plus the two `engine-object-id-generator` had already converted) and **25 carry `UnassignedID` explicitly**:

| Carrying `UnassignedID` | Why |
|---|---|
| `src/client/DAE.Client` (6), `src/client/Frontend.Client/FormAction.cs` (3), `src/windows/DAE.Client.Controls` (3) | A key or an order is only a description of how a list is sorted or identified. There is no engine and no catalog within reach, and there never will be. |
| `Schema/TableVar.cs` (6) | `TableVarColumnsBase.Intersect`/`Difference`/`Union` and `Keys.SuperKey` answer a derived column set to their caller and never add it to a table variable; two more are inside `#if USEINVARIANT`, which no project defines. No plan, and no plan parameter invented on a `Schema` type. |
| `Runtime.Instructions/TableNode.cs` (2) | `CopyKey` and `CopyOrder` populate this node's own result table variable, an artefact of the compile. `engine-object-id-transient-tablevars` does the same for the result table variable itself. |
| `Runtime.Instructions/ExtendNode.cs` (1), `OrderNodes.cs` (1) | `KeyProduct`'s intermediate products, and a reversed copy of an order emitted straight into a D4 expression and discarded. |
| `Runtime.Data/NativeTable.cs` (1), `ObjectMarshal.cs` (1) | Only an `IValueManager` is in reach, and it deliberately stays narrow (widening a public interface is a migration cost for no gain). These describe a native table's in-memory indexes and a marshalled value's key; neither reaches a catalog. |
| `libraries/SQLDevice/Source/SQLDevice.cs` (1) | `CreateSparseKeyFromOrder` builds a probe for `Keys.Contains`, which compares columns and sparseness, not identifiers. |

The 26 sites left alone are all unreachable: 18 in four files no project compiles (`src/client/DAE.Client/View-263.cs`, `src/engine/DAE/Runtime.Data/TableBuffer.cs`, `src/engine/DAE/Runtime.Instructions/GeneratorNodes.cs`, `src/engine/DAE/Runtime.Instructions/TestNodes.cs` - checked against every `.csproj`), and 8 inside `/* */` blocks (`Debug/DebugNodes.cs:476`, `Server/Runtime.Instructions/LibraryNodes.cs:1318,1391`, `tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs:901-1210`). **This differs from the ticket's table**, which counted several of those as live work - see *Where the ticket was wrong* below.

### Two departures from the ticket

**`Schema.TableVar.CopyTableVar` takes a `Plan`.** The ticket said to give the keys and orders built inside `Schema` "the identifier of the object they derive from where that is meaningful" and not to invent a plan parameter on a `Schema` type. That is right for the set-arithmetic helpers, but wrong for `CopyTableVar`: two of its three callers are the compiler creating a table (`Compiler.cs:4292`) or a view (`Compiler.cs:4422`), and those keys and orders are written into the catalog store as part of the table variable's definition. Inheriting the source's identifier would put two distinct catalog objects on one number, and `UnassignedID` would trip the guard. So the signature is now `CopyTableVar(Plan, TableNode)` / `CopyTableVar(Plan, TableNode, bool)`; all three callers already had a plan in hand. `Schema/Device.cs` already takes a `Plan` in a dozen places, so this is not a new dependency - `TableVar.cs` gains `using Alphora.Dataphor.DAE.Compiling;`, and it already reached into `Runtime.Instructions` for `TableNode`. The public-surface change is in `docs/migration-notes.md`.

**`GetNextObjectID`, not `GetObjectID`, at the two `Order(MetaData)` sites in `Compiler.cs`** (`EmitOrderNode`, `EmitBrowseNode`). `Order(MetaData)` has always allocated a fresh identifier and left any `DAE.ObjectID` tag in the meta data; only the sites already written as `GetObjectID(metaData)` consume and remove the tag. Using `GetObjectID` here would have changed behaviour.

### The one behavioural change

Identifier *values* move. Every engine-path site draws from the same generator in the same order as before, but the sites that stop allocating - the client ones and the run-time scaffolding now on `UnassignedID` - no longer consume numbers, so everything allocated after them shifts down. Some of those run while an engine is starting and running `SystemCatalog.d4`, so **the built-in objects of an engine built from this commit are numbered differently from one built from the commit before, and a catalog store written by an earlier build of 4.0 has to be rebuilt from its scripts.** That is within what the plan settled for 4.0 (catalogs are rebuilt, not migrated) and 4.0 has not shipped. What must still hold is that the numbering is identical on every start of *this* build; `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart` stops a server and starts it again against its own store, and passes.

## Where the ticket was wrong, and what a reviewer should check first

The ticket's per-file table was measured with a plain grep and counted dead code as work. A reviewer re-deriving it will get different numbers than the ticket states; the inventory in *Where the sites went* is the measured one. Specifically: `src/client/DAE.Client/View.cs` has 1 site, not 5 (the other 5 are in `View-263.cs`, which no project compiles); `src/engine/DAE/Runtime.Instructions/TestNodes.cs` (9) and `Runtime.Data/TableBuffer.cs` (3) are not compiled by any project; and all 5 sites in `tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs` are inside a block comment spanning lines 768-1325. None of those four files was touched.

Worth a sceptical look, in this order:

1. **`Compiler.EnsureKey` and `Compiler.CompileTableVarKeys`** - the inferred key of a table or view declared without one, which *is* written to the catalog store. Both allocate from the plan. Exercised: the coverage scripts `Cursors`, `EventHandlers`, `References` and `TableLibrary` each do `create table X { ID : Integer };` with no key declaration, and all four pass.
2. **`CopyTableVar`** - the departure above. Check that giving the copy a fresh identifier rather than the source's is right, and that no fourth caller exists (the spike builds the client projects and found none).
3. **`TableNode.CopyKey` / `CopyOrder` getting `UnassignedID`** - these are the most-used key-inference helpers in the engine. The claim is that a `TableNode`'s own `TableVar` is a `ResultTableVar` and never persisted, and that where an operator node's inferred keys *do* reach the catalog it is through `CopyTableVar`, which allocates. If that claim is wrong, the guard would have to fire somewhere the suite does not reach.
4. **Two unassigned keys in one collection.** `Key.Equals` compares columns and sparseness, `GetHashCode` hashes column names, and `NativeRowTreeList.IndexOf` compares orders with `Equivalent` - none looks at an identifier (read). Only `Device.CatalogIndex` is keyed by identifier, and only catalog objects go in it.
5. **Generated names.** The ticket warned that two unnumbered objects feeding `Schema.Object.GetGeneratedName` would collide on the name. Checked every caller: they pass device-map, event-handler, reference and operator identifiers. No key or order feeds one.

## Known gaps

- **No new test.** As the ticket specified: this step changes no behaviour on an engine path, and keys and orders are created and persisted by a large share of the coverage scripts already. The characterization tests for the numbering itself belong to `bug-object-id-counter-is-process-wide`, where the behaviour changes. The honest limit of that argument: a key or order built on a path the suite never persists could be unnumbered and nothing would notice. By the design that is harmless, but it is unproven rather than proven.
- **`MemoryDevice.InternalPrepare` now calls `plan.Plan.GetNextObjectID()` twice per candidate order** when probing for a backwards scan. Those two orders are `Equivalent` probes that are thrown away, so they consume identifiers for nothing - which is exactly what they did before this change, so it is parity, not a regression. Converting them to `UnassignedID` instead would have been defensible; the mechanical rule was followed to keep the numbering story simple.
- **`Program.Plan` creates a `Plan` lazily** if one does not exist, and three run-time sites now touch it (`ExplodeTable`, `UnionTable`, `OrderTables`, each in `InternalOpen`). This is established practice in that exact layer - `OrderTables.cs:701`, `RestrictTables.cs` and `TableValue.cs:797` already do it - so no new object is created in practice, but a reviewer who wants to be sure could check that a cursor open on those three paths does not construct an undisposed `Plan`.
- **`libraries/SQLDevice/Source/SQLDevice.cs:2227` spells the constant in full** (`Alphora.Dataphor.DAE.Schema.Object.UnassignedID`) because `SQLDevice` has a `Schema` property of its own that shadows the namespace in expression position, and a bare `Object` would be ambiguous with `System.Object`. Ugly, and the only site like it.

## Validation

- `dotnet build Dataphor.slnx -t:Rebuild` - **0 errors, 143 warnings**, the unchanged baseline, none in the files touched.
- The three suite parts from `AGENTS.md`, each in the foreground: **664 passed / 0 failed / 33 skipped** (part 1, 6 m 7 s), **67 / 0 / 11** (part 2, 3 m 7 s), **50 / 0 / 12** (part 3, 3 m 7 s). Identical to the baseline `engine-object-id-generator` recorded.
- `dotnet test tests/DAE.Server.Tests --filter Name=RebuildRegistersLibrariesAndSurvivesRestart` - passed, which is the check that this build numbers its built-in objects the same on every start.
- `dotnet test tests/DAE.Server.Tests --filter "Name=Cursors|Name=TableLibrary|Name=EventHandlers|Name=References"` - 4 passed, the keyless-`create table` path through `EnsureKey`.
- `python tools/net-spike/run.py <scratch> --smoke` - the only build that covers the client and Windows sites. Matches the `AGENTS.md` baseline exactly: the converted projects and the six client projects 0 errors, `Dataphoria` 1 (`TreeNode.ContextMenu`), `DAE.Service.ConfigurationUtility` 1 (`NotifyIcon.ContextMenu`), `D4Runner` blocked on `NU1201`, smoke 1,080 operators, concurrency 0 errors, resolution cache 0 errors.
- `dotnet run --project tools/net-spike/EngineCheck -c Debug` - `RESULT: pass`, 1,060 operators and 23 scalar types.
- No pre-existing failures surfaced; `tickets/.pre-existing-error.md` was not written.
- Source hygiene: every edited file keeps its tabs, its CRLF line endings and its byte-order-mark state (compared byte-for-byte against `HEAD` for all 46 files). The `git diff --check` "trailing whitespace" warnings are the CR at end of line; the prereq's own commit shows 124 of the same, so nothing new.
