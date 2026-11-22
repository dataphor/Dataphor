description: Catalog identifiers used to come from one counter belonging to the whole program; this step gave each engine a counter object of its own and moved about forty of the places that hand out identifiers onto it, deliberately leaving every engine pointing at one shared counter so that no number changes yet.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/ObjectIDGenerator.cs, src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Schema/SchemaException.cs, src/engine/DAE/Schema/SchemaException.resx, src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Compiling/Plan.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Runtime.Instructions/RenameNode.cs, src/engine/DAE/Runtime.Instructions/TableNode.cs, src/engine/DAE/Runtime.Instructions/AdornNode.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Server/Server.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/DAE.csproj, tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs, docs/migration-notes.md
----
## What landed

The first of four steps towards `bug-object-id-counter-is-process-wide`. It introduces the type that will own catalog identifier allocation, puts one on `Engine`, gives the compiler a way to reach it, and converts the allocation sites that already had an engine in hand. **By design nothing about the running system changes yet** - every engine's generator is literally the same object as the counter the unconverted sites still use.

### The generator

`src/engine/DAE/Schema/ObjectIDGenerator.cs` (new, and added to `src/engine/DAE/DAE.csproj`, which lists its compile items explicitly):

- `Allocate()` - `Interlocked.Increment` over a private `int`, so the first identifier is 1 and 0 is never handed out. That is what lets the next two tickets use 0 to mean "no identifier".
- `Allocate(MetaData)` - honours a `DAE.ObjectID` tag and removes it, else allocates. This is the body that used to be `Schema.Object.GetObjectID(MetaData)`, moved unchanged.
- `Reset()` - back to zero, so the next identifier is 1 again. For the engine whose built-in objects must be numbered identically on every load of its catalog store.
- `EnsureAllocatedThrough(int)` - raises the counter to the given identifier if it is below it, never lowers it.
- `SetLastAllocated(int)` - **internal, and not in the ticket's design.** See *Where I departed from the ticket*.

`Reset` and `EnsureAllocatedThrough` take a private lock; `Allocate` stays lock-free, as today.

### Where it lives, and how call sites reach it

- `Engine.ObjectIDGenerator`, assigned in the `Engine` constructor from `Schema.Object.SharedObjectIDGenerator` (a new `internal static readonly` instance next to the counter it replaces) and never reassigned. There is a comment on both saying this sharing is the transition and which ticket ends it.
- `Plan.GetNextObjectID()` and `Plan.GetObjectID(MetaData)`, which go through `ServerProcess.ServerSession.Server.ObjectIDGenerator`.
- `Schema.Object.GetNextObjectID()`, `GetObjectID(MetaData)` and `SetNextObjectID(int)` stay on the public surface and now delegate to that shared instance, so the sites not yet converted draw from the same counter.

### The three guards

`Schema.Object.SaveObjectID`, `ServerCatalogDeviceSession.InternalInsertPersistentObject` and `CatalogDeviceSession.CacheCatalogObject` now refuse an object whose identifier is 0, with the new `SchemaException` code 113249. Nothing can produce such an object today; the later tickets in the chain inherit the guards.

### Call sites converted (about 40)

`Compiler.cs` (5 `GetNextObjectID`, 27 `GetObjectID`), `AdornNode.cs` (1; the commented-out one at line 178 left alone), `RenameNode.cs` (2), `TableNode.cs` (2), `DDLNodes.cs` (1, via `program.Plan`), `ServerProcess.cs` (1), `Engine.cs` (3 allocations plus the reset in `InitializeCatalog`, which stays exactly where and when it was, comment included), `Server.cs` (1 allocation plus `LoadServerState`), `TestNodes.cs` (8, via `program.Plan`).

Also: the dead three-argument `Schema.Property(int, string, IDataType)` constructor is deleted. It chained to `base(name)` and so threw away the identifier it was given; nothing called it - a grep for `new Schema.Property(` and `new Property(` across `src`, `libraries`, `tests`, `tools` and `samples` finds the two-argument call in `Compiler.cs` and nothing else.

Left for the later tickets, as the ticket says: `Schema.Object.GetNextObjectID()` in the `ResultTableVar` constructor (`src/engine/DAE/Schema/TableVar.cs:2896`), the implicit allocation in `Schema.Object(string name)`, and the save-and-restore workarounds in `tests/DAE.Server.Tests/TestEnvironment.cs` and `tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs`. A grep for the three statics finds exactly those and nothing else.

## Where I departed from the ticket

Three things, all deliberate; please weigh them.

**Two error codes, not one.** The ticket asks for one new `SchemaException` code at 113249, used both by the three guards and by the overflow check in `Allocate`. The two conditions cannot share a message - one names an object, the other names no object at all - so there are two: `ObjectIDRequired = 113249` ("Object "{0}" has not been given an object identifier.") for the guards, and `ObjectIDsExhausted = 113250` ("The object identifiers of this catalog have been exhausted.") for the overflow. `docs/reference/error-codes.md` lists prefix ranges only and needed no edit.

**A fifth, internal member on the generator.** `Schema.Object.SetNextObjectID(int)` has to keep behaving exactly as it does, including being able to move the counter *down*: `TestEnvironment.cs` and `BenchmarkServerInstance.cs` read the counter and put it back. Neither `Reset` nor `EnsureAllocatedThrough` can do that, so `internal void SetLastAllocated(int)` exists for `SetNextObjectID` alone, with a `NOTE:` saying it goes when that method goes. The alternative - leaving `SetNextObjectID` on a separate static counter - is exactly the two-counters state the staging exists to avoid.

**One identifier changes after a store load.** `Server.LoadServerState` used to call `SetNextObjectID(GetMaxObjectID() + 1)`, which made the *next* allocated identifier `max + 2`, because the counter holds the last identifier allocated, not the next one. The ticket's `EnsureAllocatedThrough(GetMaxObjectID())` makes it `max + 1`. No identifier collides either way - the old code simply reserved one it never handed out - but it is the one number in the tree that is not identical to before, and the migration note says so. The new call is also strictly safer: `EnsureAllocatedThrough` cannot lower the counter, where the old call would have, if a store's maximum were ever below the count of built-in objects created before the store was read (reachable only through `Engine.ClearCatalog`, which calls `LoadServerState` unconditionally - `Engine.Start` calls it only when `!_firstRun`).

## Validation performed

- `dotnet build Dataphor.slnx` - 0 errors, 143 warnings, none in the files touched.
- The three suite parts from `AGENTS.md`, each in the foreground: **661 passed / 0 failed** (part 1, 6 m 14 s), **67 / 0** (part 2, 3 m 10 s), **50 / 0** (part 3, 3 m 7 s). Skip counts unchanged.
- `python tools/net-spike/run.py <scratch> --smoke` - matches the baseline in `AGENTS.md` exactly: converted projects and the six client projects 0 errors, `Dataphoria` 1 (`TreeNode.ContextMenu`), `DAE.Service.ConfigurationUtility` 1 (`NotifyIcon.ContextMenu`), `D4Runner` blocked on `NU1201`, smoke 1,080 operators, concurrency 0 errors, resolution cache 0 errors.
- `dotnet run --project tools/net-spike/EngineCheck -c Debug` - `RESULT: pass`, 1,060 operators and 23 scalar types, so the two new resource strings did not disturb resource naming.

No pre-existing failures surfaced; `tickets/.pre-existing-error.md` was not written.

## No new tests, and what that leaves uncovered

Per the ticket. Nothing here changes behaviour, and the allocation this re-plumbs runs for every catalog object the suite creates - 778 passing tests across the three parts is the characterization. `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart` in particular stops a server and starts it again against its own catalog store, which is the property the reset's placement protects, and it exercises both branches of `EnsureAllocatedThrough` (raise on a restart against a populated store, leave alone on a first run).

Two things are therefore unexercised **by construction**, and a reviewer may reasonably want a small unit test on `ObjectIDGenerator` for them:

- The overflow check in `Allocate`. Unreachable without about two billion allocations in one catalog, so a test would have to drive the counter there through `SetLastAllocated` or reflection.
- The three identifier-0 guards. Nothing in the tree can build an object with identifier 0 until `engine-object-id-keys-and-orders` lands.

## What to check

- **The claim this ticket rests on:** that no catalog can be fed by two different counters during the transition. Confirm `Engine.ObjectIDGenerator` is assigned `Schema.Object.SharedObjectIDGenerator` and never anything else, and that the three `Schema.Object` statics delegate to that same field.
- **The reset.** `Engine.InitializeCatalog` must still reset as the first thing it does, before `CreateCatalogDevice`, so that the catalog device is identifier 1 on every load of a store and the built-ins created before the store is read line up with the rows already in it.
- **The 32 conversions in `Compiler.cs`.** Every one relies on the enclosing static method's `Plan plan` parameter; there is no `Plan` field on `Compiler` and no local named `plan` in that file, so the compiler checked them. Worth a skim anyway for a site where the enclosing method's plan is not the plan the object belongs to.
- **`TestNodes.cs`.** The ticket said this library builds only through the spike; it does not - `tests/libraries/TestFramework.Coverage.Base/Source/DAE.Diagnostics.csproj` is in `Dataphor.slnx`, so the compiler did check those eight sites and `program.Plan` resolves.
- **File hygiene.** Every edited file keeps its tabs, CRLF line endings and byte-order-mark state; the new `ObjectIDGenerator.cs` is UTF-8 with a byte-order mark and CRLF, like `Schema.cs`. Note that `tickets/.garden-report.md` is modified in the working tree by something other than this ticket.
