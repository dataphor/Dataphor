description: Catalog identifiers are handed out by one counter shared by the whole program, which goes wrong when a program runs two Dataphor engines at once; this first step gives each engine a counter of its own and moves the easy callers onto it, with no change in behaviour yet.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Schema/ObjectIDGenerator.cs, src/engine/DAE/Schema/SchemaException.cs, src/engine/DAE/Compiling/Plan.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Runtime.Instructions/RenameNode.cs, src/engine/DAE/Runtime.Instructions/TableNode.cs, src/engine/DAE/Runtime.Instructions/AdornNode.cs, src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Server/Server.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs
difficulty: medium
----
## Why this ticket exists

Every object in an engine's catalog - every table, view, type, operator, key, order, constraint - carries an integer identifier used as its surrogate key in the catalog. Those identifiers come from one counter that belongs to the whole program (`Schema.Object.GetNextObjectID`, a `static int` in `src/engine/DAE/Schema/Schema.cs`), and starting an engine sets that counter back to zero. A program that runs two engines at once therefore has the second one hand the first identifiers the first has already used, and the first engine's next schema change fails with `UNIQUE constraint failed: DAEObjects.ID`. `bug-object-id-counter-is-process-wide` owns that defect and the full story; this ticket is the first of four steps towards it.

The counter has to be per engine, because the reset is not an accident: an engine that persists its catalog creates a handful of built-in objects programmatically *before* it reads the store, and those must land on the same identifiers the store already holds for them, so re-initializing must start from zero again. A reset is right; a reset that reaches another engine is not. Making the counter per engine therefore means every allocation into a catalog has to come from that catalog's engine - there is no way to leave some allocations on a shared counter, because a counter that is reset and a counter that is not will sooner or later produce the same number for two objects in the same catalog.

That is about 300 call sites. This ticket does the easy third and establishes the shape; the two tickets after it do the rest; `bug-object-id-counter-is-process-wide` then flips the switch. **Nothing in this ticket changes behaviour:** during the chain, every engine's generator is one and the same shared instance, so the numbers handed out are exactly the numbers handed out today. Keep it that way - the whole point of the staging is that each step is verifiable by the existing suite.

## What to build

### The generator

A new type, `Alphora.Dataphor.DAE.Schema.ObjectIDGenerator`, in a new file `src/engine/DAE/Schema/ObjectIDGenerator.cs`:

```csharp
public sealed class ObjectIDGenerator
{
    /// <summary>Returns an identifier no object in this catalog has been given.</summary>
    public int Allocate();

    /// <summary>Honours a DAE.ObjectID tag in the given meta data, removing it; allocates when there is none.</summary>
    public int Allocate(MetaData metaData);

    /// <summary>Starts again from one, for an engine whose built-in objects must be numbered the same on every load of its catalog store.</summary>
    public void Reset();

    /// <summary>Ensures no identifier at or below the given one is allocated, for a catalog loaded from a store.</summary>
    public void EnsureAllocatedThrough(int iD);
}
```

`Allocate` keeps `Interlocked.Increment` over a private `int`, so the first identifier is 1 and `0` is never allocated - which is what lets `0` mean "no identifier" in the next two tickets. `Allocate(MetaData)` is today's `Schema.Object.GetObjectID(MetaData)`, moved. `Reset` and `EnsureAllocatedThrough` replace today's single `SetNextObjectID`, which is the method that can move the counter backwards; splitting it names the two things its callers actually mean.

Add an overflow check in `Allocate`: past `Int32.MaxValue` the increment wraps negative, and `-1` already means "this object has no parent", so a wrap would corrupt the catalog silently. Throw the new `SchemaException` code below instead. See *Edge cases and interactions* for why exhaustion is not a practical concern but is still worth refusing.

### Where it lives

On `Engine`, created in the `Engine` constructor and never replaced:

```csharp
public ObjectIDGenerator ObjectIDGenerator { get; }
```

**Not on `Schema.Catalog`.** `Engine.ClearCatalog` replaces the whole catalog, so a generator held there would restart on every catalog rebuild - which is the second half of the defect this chain is fixing, re-introduced by construction.

For this ticket the property returns a single shared instance held in a `static readonly` field (put it on `Schema.Object` next to the counter it will replace, with a `NOTE:` saying it is the transition's shared instance and which ticket splits it). `Schema.Object.GetNextObjectID`, `GetObjectID` and `SetNextObjectID` stay, delegating to that same instance, so converted and unconverted call sites keep drawing from one counter.

### How call sites reach it

Through the `Plan` the compiler already threads everywhere, and through `Program.Plan` at run time. On `Plan` (`src/engine/DAE/Compiling/Plan.cs`, alongside `IsEngine`, which reaches the engine the same way):

```csharp
public int GetNextObjectID();              // ServerProcess.ServerSession.Server.ObjectIDGenerator.Allocate()
public int GetObjectID(MetaData metaData);  // ... .Allocate(metaData)
```

A `Plan` always has a `ServerProcess` - its only constructor takes one - so there is no null case to handle.

### The two guards

With the next two tickets, objects built where no engine is in reach will carry no identifier. Three places must refuse such an object rather than write it, so that a missed call site fails loudly at the moment it matters instead of corrupting a catalog:

- `Schema.Object.SaveObjectID` (`Schema.cs`), called only when emitting an object for the catalog store.
- `ServerCatalogDeviceSession.InternalInsertPersistentObject` (`src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs`), which writes the identifier into `DAEObjects.ID`.
- `CatalogDeviceSession.CacheCatalogObject` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`), where it adds to `Device.CatalogIndex`.

Add one `SchemaException` code for this (the next free one is 113249; follow `ErrorApplyingDeviceSetting` at 113248 for the enum entry, its doc comment and its resource string). `docs/reference/error-codes.md` lists prefix ranges only, so it needs no edit. The guards will not fire until the later tickets land - write them now so those tickets inherit them.

### The call sites to convert

All of these already have a `Plan` or a `Program` in hand - checked, every one:

- `src/engine/DAE/Compiling/Compiler.cs` - 5 `GetNextObjectID()` and 27 `GetObjectID(...)`, every one inside a `static ... (Plan plan, ...)` method, so `plan.GetNextObjectID()` / `plan.GetObjectID(...)`.
- `src/engine/DAE/Runtime.Instructions/AdornNode.cs` - 1 live `GetObjectID` (a second is commented out; leave it).
- `src/engine/DAE/Runtime.Instructions/RenameNode.cs` - 2, in a method taking `plan`.
- `src/engine/DAE/Runtime.Instructions/TableNode.cs` - 2, in `CopySourceReference` and `CopyTargetReference`, which take `plan` and currently ignore it.
- `src/engine/DAE/Runtime.Instructions/DDLNodes.cs` - 1, in `AlterSpecials`, via `program.Plan`.
- `src/engine/DAE/Server/ServerProcess.cs` - 1, in `Compile`, which takes `plan`.
- `src/engine/DAE/Server/Engine.cs` - 3 (the temp device, the application-transaction device, `CreateCatalogDevice`) and the `SetNextObjectID(0)` in `InitializeCatalog`: all inside `Engine`, so `ObjectIDGenerator.Allocate()` and `ObjectIDGenerator.Reset()`. Keep the reset exactly where and when it happens today, and keep its comment, which explains why it must be the first thing the catalog does.
- `src/engine/Server/Server/Server.cs` - 1 in `CreateCatalogDevice`, and `LoadServerState`'s `SetNextObjectID(GetMaxObjectID() + 1)` becomes `ObjectIDGenerator.EnsureAllocatedThrough(GetMaxObjectID())`.
- `tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs` - 8, in nodes that have `program`. This test library builds only through the spike (`python tools/net-spike/run.py <dir> --smoke`), not through `Dataphor.slnx`, so the compiler will not catch a mistake here; read each site.

Leave alone, for the later tickets in the chain: `src/engine/DAE/Schema/TableVar.cs` (the `ResultTableVar` constructor), `tests/DAE.Server.Tests/TestEnvironment.cs` and `tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs` (the save-and-restore workarounds, which `bug-object-id-counter-is-process-wide` removes).

### A dead constructor with a defect in it

`Schema.Property(int iD, string name, IDataType dataType)` in `src/engine/DAE/Schema/ScalarType.cs` chains to `base(name)` instead of `base(iD, name)`, so it discards the identifier it is given and allocates a fresh one. Nothing calls it - the only `new Schema.Property` in the tree is the two-argument overload, at `Compiler.cs:4714` - so the defect is dormant. Delete the overload rather than fix it: it is unreachable, it is wrong, and leaving it is an invitation. No test is owed for code nothing calls.

## Edge cases and interactions

- **Two generators feeding one catalog.** The hazard this staging exists to avoid: if some allocations came from a per-engine generator and others from the shared counter, the two would eventually produce the same number for two objects in one catalog, and `DAEObjects.ID` would reject the second. Verified by inspection that this ticket cannot create that state - every engine's `ObjectIDGenerator` is literally the same instance as the counter the unconverted sites use. A reviewer should check exactly that.
- **Identifier exhaustion.** Identifiers are `Int32`, allocated monotonically and never reused, and a wrap goes negative into `-1`, the sentinel for "no parent object" (`Schema.Object.ParentObjectID`). Refused by the new check rather than measured: at the few dozen identifiers a statement consumes, exhaustion needs on the order of a hundred million statements in one process, which no deployment approaches. Verified by the check itself, not by a test.
- **Concurrency.** `Allocate` must stay lock-free and correct under concurrent compiles, since several processes of one engine compile at once: `Interlocked.Increment`, as today. `Reset` and `EnsureAllocatedThrough` run during engine start-up and catalog rebuild, and today's `SetNextObjectID` takes a lock, so keep them locked. Verified by inspection against today's code, which has the same shape.
- **The reset's placement.** `Engine.InitializeCatalog` resets before creating the catalog device, and the catalog device must come out as identifier 1 on every load of the same store; the built-in objects created before the store is read must likewise be numbered the same every time, or they will not line up with the rows the store already holds for them. Moving the reset, or making an allocation ahead of the store read depend on anything that varies, changes that numbering. Verified by `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart`, which stops a server and starts it again against its own catalog store - which is exactly this property, for one build. (What no test covers is a store written by an *earlier* build being read by a later one, because every test run builds a fresh store in a fresh run directory; the last ticket in this chain carries the migration note for that.)
- **`EnsureAllocatedThrough` versus `Reset`.** `Server.LoadServerState` runs after `InitializeCatalog`, so the generator sits above the built-ins and below the store's maximum, and the new call must raise it, never lower it. A store whose maximum is *below* the built-in count - an empty or first-run catalog - must leave the generator where it is, which is why the method is "ensure" and not "set". Verified by inspection plus a first-run engine start, which every fixture in the suite does.
- **The guards firing during this ticket.** They should not: nothing yet produces an object without an identifier. If one fires, something allocates outside the paths catalogued here - find it rather than loosening the guard.
- **Projects outside the solution.** `src/client`, `src/windows`, the client half of the frontend and the test libraries do not build through `Dataphor.slnx`. This ticket touches one of them (`TestNodes.cs`), so build the spike as well as the solution.

## TODO

- Read `AGENTS.md` and `docs/modernization-plan.md#ws2-engine-and-server-port`. Preserve each file's tabs, CRLF line endings and byte-order-mark state.
- Add `src/engine/DAE/Schema/ObjectIDGenerator.cs` with the four members above, the overflow check, and a `NOTE:` on the class saying identifiers are per catalog and why the generator cannot live on `Schema.Catalog`.
- Add the new `SchemaException` code (113249) with its doc comment and resource string, following 113248.
- Hold the transition's single shared `ObjectIDGenerator` in `Schema.Object`, with a `NOTE:` naming `bug-object-id-counter-is-process-wide` as the ticket that splits it; make `GetNextObjectID`, `GetObjectID` and `SetNextObjectID` delegate to it with no change in behaviour.
- Add `Engine.ObjectIDGenerator`, returning that shared instance for now, created in the constructor.
- Add `Plan.GetNextObjectID()` and `Plan.GetObjectID(MetaData)`.
- Add the three guards (`SaveObjectID`, `InternalInsertPersistentObject`, `CacheCatalogObject`).
- Convert the call sites listed above, file by file.
- Delete the dead three-argument `Schema.Property` constructor.
- Add the `docs/migration-notes.md` entry: a custom C# extension can now reach its engine's identifier generator through `Plan.GetNextObjectID()` and `Plan.GetObjectID(MetaData)`; the `Schema.Object` statics still work and a later ticket in this chain removes them; the three-argument `Schema.Property` constructor is gone, and never worked - it discarded the identifier given to it.
- No new tests. Nothing here changes behaviour, and the allocation this ticket re-plumbs is exercised by every catalog object the suite creates; the characterization tests belong to `bug-object-id-counter-is-process-wide`, where the behaviour changes.
- `dotnet build Dataphor.slnx`, then the three suite parts from `AGENTS.md`, each as its own foreground command. Then `python tools/net-spike/run.py <work-dir> --smoke` for the test library outside the solution.
