description: Keys and orders quietly take a catalog identifier from a counter shared by the whole program whenever anything creates one, including the Windows client, where there is no catalog at all; this step makes them take the identifier from the engine that owns the catalog, or none where there is no engine.
prereq: engine-object-id-generator
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Runtime.Instructions/JoinNodes.cs, src/engine/DAE/Runtime.Instructions/CatalogNodes.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/Server/Runtime.Instructions/CatalogNodes.cs, libraries/SQLDevice/Source/SQLDevice.cs, src/engine/DAE/Debug/DebugNodes.cs, src/client/DAE.Client/View.cs, src/engine/DAE/Runtime.Data/TableBuffer.cs, libraries/Frontend/Source/Derivation/InterfaceBuilder.cs
difficulty: medium
----
## What this step changes

`Schema.Key` and `Schema.Order` are catalog objects: a table variable's keys and orders are part of the catalog, are written into the catalog store as part of their table's definition, and carry an identifier of their own. Both types have constructors that take an identifier and constructors that do not, and the ones that do not allocate from the counter shared by the whole program - the counter `engine-object-id-generator` is in the middle of replacing with one generator per engine. This step moves them over.

There are 165 places in the tree that construct a key or an order (measured: `grep -rcE "new (Schema\.)?(Key|Order)\(" --include=*.cs src libraries tests`). Seventeen of them are in the Windows client and the data-access components - `src/client/DAE.Client/View.cs`, `TableDataSet.cs`, `DataSet.cs`, `src/client/Frontend.Client/FormAction.cs`, `src/windows/DAE.Client.Controls/DBGrid.cs`, `IncrementalSearch.cs` - where a key or an order is only a description of how a list is sorted or identified. There is no engine and no catalog within reach there, and there never will be. So the rule cannot be "every key has an engine's identifier".

The rule is:

> A key or an order constructed where a `Plan` or a `Program` is in scope takes its identifier from that engine. Everywhere else it gets no identifier.

"No identifier" is `0`. The generator's first identifier is `1` and `-1` already means "this object has no containing object", so `0` is free and unambiguous. Add `public const int UnassignedID = 0;` to `Schema.Object`, give the no-identifier constructors of `Key` and `Order` that value instead of an allocation, and say at the call sites that have no engine which case they are - `new Schema.Key(Schema.Object.UnassignedID, columns)` - so a reader can tell "deliberately none" from "nobody thought about it", and so the set is greppable.

Keeping those constructors rather than deleting them is deliberate: the client and the data-access components do not build through `Dataphor.slnx` (only through `python tools/net-spike/run.py <dir> --smoke`), so removing a constructor they use would break them without a compiler error anywhere in the main build. The three guards `engine-object-id-generator` adds - in `SaveObjectID`, `InternalInsertPersistentObject` and `CacheCatalogObject` - are what catch an engine-path site that should have passed an identifier and did not: they throw when an object with no identifier is about to be written to a catalog store or indexed in a catalog cache.

**This step does not change behaviour on any engine path.** Every converted site draws from the same shared generator instance as before, so it gets the same number it gets today.

## Where the sites are

By file, from the same measurement (keys and orders together):

| File | Sites |
|---|---|
| `src/engine/DAE/Runtime.Instructions/JoinNodes.cs` | 22 |
| `src/engine/DAE/Runtime.Instructions/CatalogNodes.cs` | 13 |
| `src/engine/DAE/Compiling/Compiler.cs` | 11 |
| `src/engine/Server/Runtime.Instructions/CatalogNodes.cs` | 9 |
| `src/engine/DAE/Runtime.Instructions/TestNodes.cs` | 9 |
| `libraries/SQLDevice/Source/SQLDevice.cs` | 9 |
| `src/engine/DAE/Schema/TableVar.cs` | 8 |
| `src/engine/DAE/Debug/DebugNodes.cs` | 7 |
| `tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs` | 5 |
| `src/client/DAE.Client/View.cs` | 5 |
| the rest | 1-4 each, in `src/engine/DAE/Runtime.Instructions`, `src/engine/DAE/Runtime.Data`, `src/engine/DAE/Device.Memory`, `src/engine/Server`, `libraries/Frontend`, `libraries/System.Platform`, `libraries/System.Internet`, `src/client`, `src/windows` |

`grep -rnE "new (Schema\.)?(Key|Order)\(" --include=*.cs src libraries tests` gives the list; work it file by file.

Three groups need a decision rather than a mechanical edit:

- **`src/engine/DAE/Schema/TableVar.cs` (8).** Keys and orders constructed inside `Schema` itself - copy constructors, inference helpers, `Order(Key)`. These have no plan. Give them the identifier of the object they derive from where that is meaningful, and `UnassignedID` otherwise; do not invent a plan parameter on a `Schema` type.
- **`src/engine/DAE/Runtime.Data` and `src/engine/DAE/Device.Memory` (about 8: `TableBuffer.cs`, `NativeTable.cs`, `UnionTable.cs`, `ExplodeTable.cs`, `OrderTables.cs`, `ObjectMarshal.cs`, `MemoryDevice.cs`).** Run-time scaffolding - a throwaway table variable describing an in-memory sort or a marshalled value. Some have a `Program`, some only an `IValueManager`. Where a `Program` is in scope, allocate through `program.Plan`; where only an `IValueManager` is, use `UnassignedID` - these objects never enter a catalog and are never written to a store. Do **not** widen `IValueManager` for this; it is a public interface and a wider one is a migration cost for no gain.
- **`src/engine/DAE/Runtime.Instructions/TestNodes.cs` and `tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs` (14).** Nodes with a `program`; the coverage-base one builds only through the spike, so read each site rather than trusting the compiler.

## Edge cases and interactions

- **An engine-path site missed.** It gets `UnassignedID`, and the guards from `engine-object-id-generator` throw if that object reaches a catalog store or a catalog cache index. That is the safety net; the suite creates and persists plenty of keyed tables, so a miss on a persisted path should surface. A miss on a path that is never persisted is harmless by definition. Verified by the suite, and by the guards.
- **Two unassigned keys in one collection.** Safe: `Schema.Keys` and `Schema.Orders` are plain validating lists and `BaseObjects<T>` indexes by name, not by identifier (checked). Only `Device.CatalogIndex` is keyed by identifier, and only catalog objects go in there. Verified by inspection.
- **Generated names.** `Schema.Object.GetGeneratedName(seed, objectID)` uses an identifier to make a long derived name unique, and `TableNode`/`RenameNode` use it for derived reference names. Those sites allocate from a plan (`engine-object-id-generator` converted them) and are not affected, but check that no key or order newly given `UnassignedID` feeds a generated name - two such would collide on the name. Verified by inspection of the `GetGeneratedName` callers.
- **A key that is inferred and then persisted.** `Compiler.EnsureKey` adds an inferred key to a table variable, and when that variable is a table or view being created the key *is* written to the catalog store with its identifier. `EnsureKey` has a plan, so it must allocate. This is the single most important site in the ticket - a reviewer should check it first. Verified by a `create table` with no key declared, which the coverage scripts do.
- **Client-side keys and orders.** `src/client` and `src/windows` sites must come out with `UnassignedID` and must still compile; they do not build in `Dataphor.slnx`. Verified by the spike build.
- **Behaviour parity, and the numbering of built-in objects.** Every engine-path site draws from the same generator instance as today, so those numbers do not move. But the sites that stop allocating - the client ones, and the run-time scaffolding that gets `UnassignedID` - shift every identifier allocated after them, and some of them run while the engine is starting up and running `SystemCatalog.d4`. That means the built-in objects of an engine started by this commit are numbered differently from one started by the commit before, so **a catalog store written by an earlier build of 4.0 will not line up with an engine built from this one and has to be rebuilt from its scripts**. That is within what the plan already settled for 4.0 (catalogs are rebuilt, not migrated) and 4.0 has not shipped, so it is acceptable; the last ticket in this chain carries the migration note. What must still hold is that the numbering is the same on every start of *this* build, which `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart` checks by stopping a server and starting it again against its own store. Verified by that test, plus inspection of which sites stop allocating.

## TODO

- Read `AGENTS.md` and this chain's first ticket, `engine-object-id-generator`, whose generator, `Plan` accessors and guards this ticket builds on. Preserve each file's tabs, CRLF line endings and byte-order-mark state.
- Add `Schema.Object.UnassignedID` with a doc comment saying what it means and that `0` is never allocated.
- Change the no-identifier constructors of `Schema.Key` and `Schema.Order` (`src/engine/DAE/Schema/TableVar.cs`) to set `UnassignedID` instead of allocating.
- Work the call sites file by file, largest first, applying the rule above; make the no-engine ones pass `Schema.Object.UnassignedID` explicitly.
- Add the `docs/migration-notes.md` entry: `Schema.Key` and `Schema.Order` constructed without an identifier no longer take one from a process-wide counter and instead carry none; a custom C# extension that builds a key or an order for a table variable it creates in the catalog must pass `plan.GetNextObjectID()`, and the engine now refuses to store or cache a catalog object that has no identifier.
- No new tests. This step changes no behaviour, and keys and orders are created and persisted by a large share of the coverage scripts already; the characterization tests belong to `bug-object-id-counter-is-process-wide`.
- `dotnet build Dataphor.slnx`, then the three suite parts from `AGENTS.md`, each as its own foreground command. Then `python tools/net-spike/run.py <work-dir> --smoke`, which is the only build that covers the client sites.
