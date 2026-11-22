description: The last group of places that take a catalog identifier from the counter shared by the whole program are the throwaway table descriptions every query builds and a handful of objects created by name alone; this step moves them onto the engine that owns the catalog.
prereq: engine-object-id-keys-and-orders
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Runtime.Data/TableValue.cs, src/engine/DAE/Runtime.Instructions/CatalogNodes.cs, src/engine/DAE/Debug/DebugNodes.cs, src/engine/Server/Runtime.Instructions/CatalogNodes.cs, src/engine/Server/Runtime.Instructions/LibraryNodes.cs, src/engine/Server/Schema/LibraryUtility.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/Compiling/OperatorMap.cs, src/engine/DAE/Device.Catalog/ClassLoader.cs, src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs, libraries/SQLDevice/Source/SQLDevice.cs, libraries/System.Platform/Source/PlatformNodes.cs
difficulty: medium
----
## What this step changes

Two groups are left after `engine-object-id-keys-and-orders`, and together they are the last users of the identifier counter shared by the whole program.

### The result table variable of every relational operator

`Schema.ResultTableVar` is the schema stub for the intermediate result of a relational operator - a restriction, a join, a projection. Every table-valued node builds one while its data type is being worked out, and its constructor allocates an identifier: `public ResultTableVar(TableNode node) : base(Schema.Object.GetNextObjectID(), Schema.Object.GetUniqueName())` in `src/engine/DAE/Schema/TableVar.cs`. There are 62 places that construct one (measured: `grep -rcE "new Schema\.ResultTableVar\(" --include=*.cs src libraries tests`), all of the form

```csharp
_tableVar = new Schema.ResultTableVar(this);
```

inside a `DetermineDataType(Plan plan)` or an equivalent method, and almost all of them set `_tableVar.Owner = plan.User` on the next line - so a `Plan` is in scope at every one. Checked, every one: the only two that are not inside such a method are `src/engine/DAE/Runtime.Data/TableValue.cs` (`CopyNativeAs`, which has `Program` and so `Program.Plan`) and two in `libraries/SQLDevice/Source/SQLDevice.cs` (which have `plan` under another name).

Change the constructor to take the plan and allocate from it:

```csharp
public ResultTableVar(Plan plan, TableNode node) : base(plan.GetNextObjectID(), Schema.Object.GetUniqueName())
```

and delete the one-argument constructor, so the compiler names every site. They are all in projects that build in `Dataphor.slnx` (`src/engine/DAE`, `src/engine/Server`, and the `Frontend`, `SQLDevice`, `System.Internet` and `System.Platform` libraries), so the build is a complete census. It is also a source-compatibility break for a custom C# table node, which is why it gets a migration note.

### Objects created by name alone

`Schema.Object` has a constructor that takes only a name and allocates an identifier for itself (`Schema.cs:230`), and about twenty of its subclasses expose a name-only constructor that chains to it: `LoadedLibrary`, `Library`, `Role`, `Operator`, `AggregateOperator`, `SessionObject`, `BaseTableVar`, `DerivedTableVar`, `CatalogConstraint`, `TransitionConstraint`, `TableVarEventHandler`, `DerivedReference`, `OperatorMap`, `RegisteredClass` among them. There are about 35 places that use one; `grep -rnE "new (Schema\.)?(LoadedLibrary|Library|Role|Operator|AggregateOperator|SessionObject|BaseTableVar|DerivedTableVar|CatalogConstraint|TransitionConstraint|TableVarEventHandler|DerivedReference|OperatorMap|RegisteredClass)\(" --include=*.cs src libraries tests` finds them; discard the hits that already pass an identifier.

Give `Schema.Object(string name)` `Schema.Object.UnassignedID` - added by the previous ticket - instead of an allocation, and keep it, for the same reason the previous ticket kept the key and order constructors: these are public constructors, some of their callers are in projects outside `Dataphor.slnx`, and the guards from `engine-object-id-generator` catch the cases that matter. Then apply the same rule as the previous ticket at each call site:

> An object constructed where a `Plan` or a `Program` is in scope takes its identifier from that engine. Everywhere else it gets none.

The sites that **must** get a real identifier, because the object they create is registered in the catalog:

- `src/engine/DAE/Server/Engine.cs` - the user role (`_userRole = new Schema.Role(UserRoleName)`), the system loaded library, the system library. Inside `Engine`, so `ObjectIDGenerator.Allocate()`.
- `src/engine/Server/Schema/LibraryUtility.cs` (2 loaded libraries) and `src/engine/Server/Runtime.Instructions/LibraryNodes.cs` (4 libraries), `src/engine/DAE/Runtime.Instructions/LibraryNodes.cs` (1), `src/engine/Server/Runtime.Instructions/SecurityNodes.cs` (1 role), `src/engine/Server/Server/Server.cs` (the General library) - all have a `program` or an engine.
- `src/engine/DAE/Compiling/Compiler.cs` and `src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs` - the session objects (`new Schema.SessionObject(sessionName, globalName)`, 8 sites) have `plan` or `ServerProcess`.

The sites where there is no engine and the object never enters a catalog, which should come out with `UnassignedID` explicitly: the "dummy" and "temp" stand-ins in `Compiler.cs` (around lines 2325, 2334, 4563, 5302, 6220), `DDLNodes.cs` (1562, 2365, 2535), the operator maps in `src/engine/DAE/Compiling/OperatorMap.cs` and `src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs`, the registered classes in `src/engine/DAE/Device.Catalog/ClassLoader.cs`, the marshalling stand-ins in `src/engine/DAE/Runtime.Data/ObjectMarshal.cs` and `src/engine/Server/NativeCLI/NativeMarshal.cs`, and `Schema.Library.Clone`. Read each one before deciding - the test is whether the object can reach `CacheCatalogObject` or the catalog store, and the guards will say so at run time if the judgement is wrong.

## Edge cases and interactions

- **Generated names.** `Schema.Object.GetGeneratedName(seed, objectID)` makes a too-long derived name unique by appending the identifier, and `TableNode` and `RenameNode` use it for derived reference names on result table variables. Those sites allocate from a plan, so they keep real identifiers; check that nothing now given `UnassignedID` feeds a generated name, because two such names would collide. Verified by inspection of the `GetGeneratedName` callers.
- **`ResultTableVar` identity.** A result table variable is never persisted and never enters a catalog, but its identifier is still used as a uniquifier for derived names and it appears in plan-local structures. Keep it a real allocation from the plan, not `UnassignedID`: 62 result table variables all numbered `0` would be a different kind of bug. Verified by the coverage scripts, which exercise reference derivation across the relational operators.
- **The numbering of built-in objects shifts again.** As in the previous ticket: the sites that stop allocating change every identifier allocated after them, including during engine start-up, so a catalog store written by an earlier build of 4.0 will not line up with one built from this commit and must be rebuilt from its scripts; the migration note for that belongs to the last ticket in the chain. What must still hold is that the numbering is the same on every start of this build, which `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart` checks. Verified by that test, plus inspection of which sites stop allocating.
- **A custom C# table node.** Deleting the one-argument `ResultTableVar` constructor breaks one at compile time, which is the point - it has a `Plan` in hand in `DetermineDataType` and should pass it. Migration note below.
- **A missed engine-path site.** It comes out with `UnassignedID` and the guards from `engine-object-id-generator` throw if it reaches a catalog store or a catalog cache index. Verified by the suite plus the guards.
- **Behaviour on engine paths is otherwise unchanged.** Every converted site still draws from the single shared generator instance, so it gets today's number. Verified by the suite.

## TODO

- Read `AGENTS.md` and this chain's earlier tickets, `engine-object-id-generator` and `engine-object-id-keys-and-orders`. Preserve each file's tabs, CRLF line endings and byte-order-mark state.
- Change `Schema.ResultTableVar`'s constructor to `(Plan plan, TableNode node)`, delete the one-argument form, and fix the 62 call sites the build then names.
- Give `Schema.Object(string name)` `UnassignedID` instead of an allocation.
- Work the name-only construction sites, applying the rule above; make the no-engine ones pass `Schema.Object.UnassignedID` explicitly.
- Add the `docs/migration-notes.md` entry: `Schema.ResultTableVar` now takes the plan as its first constructor argument, so a custom C# table node must pass the plan it is given in `DetermineDataType`; and a `Schema.Object` subclass constructed from a name alone no longer takes an identifier from a process-wide counter and carries none, so an extension that registers such an object in the catalog must pass `plan.GetNextObjectID()`.
- No new tests. This step changes no behaviour on an engine path, and result table variables and inferred references are exercised by a large share of the coverage scripts; the characterization tests belong to `bug-object-id-counter-is-process-wide`.
- `dotnet build Dataphor.slnx`, then the three suite parts from `AGENTS.md`, each as its own foreground command. Then `python tools/net-spike/run.py <work-dir> --smoke`.
