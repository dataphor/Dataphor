description: Keys and orders used to take a catalog identifier from a counter shared by the whole program whenever anything created one, including the Windows client where there is no catalog at all; they now take it from the engine that owns the catalog, or carry none where there is no engine.
prereq: engine-object-id-generator
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/ExtendNode.cs, src/engine/DAE/Device.Memory/MemoryDevice.cs, docs/migration-notes.md
----
## What landed

The second of four steps towards `bug-object-id-counter-is-process-wide`. `engine-object-id-generator` put an identifier generator on `Engine` and gave compiled code `Plan.GetNextObjectID()`; this step moved every place in the tree that builds a `Schema.Key` or a `Schema.Order` onto it, and gave the places where there is no engine at all an explicit "no identifier".

The rule, as the ticket set it: **a key or an order constructed where a `Plan` or a `Program` is in scope takes its identifier from that engine; everywhere else it gets none.** "None" is the new `Schema.Object.UnassignedID`, which is `0` - a generator allocates from `1` upwards and `-1` already means "no containing object", so `0` was free.

The implementation is in `ticket(implement): engine-object-id-keys-and-orders`; the review pass is in this ticket's own commit. Of the 139 live construction sites, 112 now allocate from the engine and 27 carry `UnassignedID` explicitly (114 and 25 as the implementer left them, plus the two the review moved - see below). The 26 remaining sites are unreachable: 18 in four files no project compiles and 8 inside block comments.

Two deliberate departures from the ticket's letter, both already argued in the implement handoff and both confirmed sound here:

- **`Schema.TableVar.CopyTableVar` takes a `Plan`.** The ticket said not to invent a plan parameter on a `Schema` type, but two of this method's three callers are the compiler creating a table or a view whose keys and orders are written into the catalog store, so they cannot inherit the source's number or carry none. The signature is now `CopyTableVar(Plan, TableNode)` / `CopyTableVar(Plan, TableNode, bool)`; all three callers had a plan in hand. It is in `docs/migration-notes.md`.
- **`GetNextObjectID`, not `GetObjectID`, at the two `Order(MetaData)` sites in `Compiler.cs`.** `Order(MetaData)` always allocated a fresh identifier and left any `DAE.ObjectID` tag in the meta data; `GetObjectID` would consume the tag and change behaviour.

**Behavioural consequence, unchanged from the implement handoff:** identifier *values* move, because the sites that stop allocating no longer consume numbers and everything allocated after them shifts down. Some of those run while an engine is starting and running `SystemCatalog.d4`, so the built-in objects of an engine built from this commit are numbered differently from one built before it, and **a catalog store written by an earlier build of 4.0 has to be rebuilt from its scripts.** That is inside what the plan settled for 4.0 (catalogs are rebuilt, not migrated) and 4.0 has not shipped. What still holds, and is checked, is that the numbering is identical on every start of this build.

## Review findings

The implement diff was read first, with the handoff set aside, then the handoff's own claims were re-derived from the tree.

### Fixed in this pass (minor)

- **`ExtendNode`'s key-product comment was wrong about its own mechanism, and its seed key was numbered while every sibling was not.** The new comment said "the caller copies the ones it keeps onto the table variable"; the caller adds the products straight to `TableVar.Keys`. And the seed at the top of that loop took `plan.GetNextObjectID()` while every key `KeyProduct` derived from it took `UnassignedID` - so one collection held keys of identical role numbered two different ways, and in the common case (a source key with at least one column) the numbered seed was discarded and the identifier spent for nothing. The seed now carries `UnassignedID` like its siblings, with one comment that says what is actually true: these keys land on this node's own result table variable, which never enters the catalog, exactly as `TableNode.CopyKey`'s copies do. No behaviour change - nothing on that path reads a key's identifier.
- **The memory device spent a catalog identifier per candidate key, per compile, on orders it threw away.** `MemoryDevice.InternalPrepare` built a reversed copy of each candidate order purely to compare it with `node.RequestedOrder.Equivalent(...)`, and numbered each copy from the plan. `Equivalent` compares columns and sorts and never looks at an identifier (read), and the copy is discarded either way. `OrderNodes.cs:911` in the same diff already uses `UnassignedID` for the identical pattern, so this was also an inconsistency with the implementer's own treatment. Both probes now use `UnassignedID`; the real order the device attaches to the node (`node.Order`) still allocates, matching `OrderNode.DetermineOrder`. The implement handoff listed this as a known gap and said converting it "would have been defensible" - it is also the only treatment consistent with the rest of the diff.
- **`Schema.Object.UnassignedID`'s doc comment overstated the rule.** It said the run time "builds throwaway table variables for in-memory sorts and marshalled values. None of those objects enters a catalog, and none of them can be numbered" - but three run-time sites in the same diff *do* number theirs from `Program.Plan` (`OrderTables.cs`, which is the in-memory sort itself, plus `ExplodeTable.cs` and `UnionTable.cs`), exactly as the ticket told them to. Reworded to state what actually distinguishes the cases: what is in reach at the site, not the kind of object. The same overstatement in the `Key` and `Order` constructor notes was tightened to match.
- **`docs/migration-notes.md`** named "the run-time scaffolding that builds throwaway table variables" as the engine-side set that stops allocating; with the two fixes above that set is wider, so the sentence now reads "the engine-side code that builds a key or an order no catalog will ever hold". The entry's consequence for a deployment - rebuild the store from its scripts - is unchanged, so no new row.

### Checked and found sound

Each of these was a live risk on the implement handoff's own list or arose from reading the diff; all were verified against the tree rather than taken on the handoff's word.

- **An unassigned key or order reaching the catalog store.** `Key.EmitStatement(ForStorage)` and `Order.EmitStatement(ForStorage)` call `SaveObjectID`, which throws for `UnassignedID`, so a miss on a persisted path fails loudly. Traced every producer of an unassigned key or order: all callers of `TableNode.CopyKey`/`CopyOrder`/`CopyKeys`/`CopyOrders` are relational operator nodes whose `TableVar` is a `ResultTableVar` (enumerated; `TableNodes.cs` uses only `CopyTableVarColumns`), and every path on which a key or order does reach a persisted table variable allocates - `Compiler.EnsureKey`, `CompileTableVarKeys`, `KeyFromKeyColumnDefinitions`, `CopyTableVar`, and the SQLDevice reconcile sites. The one place that adds a plan-local order to a possibly-persisted table variable, `ServerProcess.CopyDataType`, can only receive an order from a device prepare or `Compiler.OrderFromKey`, both of which allocate.
- **`Compiler.EnsureKey` and `CompileTableVarKeys`**, the handoff's own "check this first": both allocate from the plan, and the four coverage scripts that declare a table with no key (`Cursors`, `EventHandlers`, `References`, `TableLibrary`) pass.
- **`CopyTableVar`.** Exactly three callers, as claimed (`Compiler.cs:4292`, `Compiler.cs:4422`, `ServerProcess.cs:891`); no fourth. A fresh identifier rather than the source's is right - the copy is a distinct catalog object, and two objects on one number is the defect the chain exists to fix. The third caller builds a `DerivedTableVar` for a remote describe, which is not persisted, so a fresh number there is harmless.
- **Two unassigned keys or orders in one collection.** `Schema.Keys` and `Schema.Orders` are `ValidatingBaseList<T>` with no identifier index; `Key.Equals`, `Key.GetHashCode`, `Order.Equals` and `Order.Equivalent` all ignore the identifier (read). `Device.CatalogIndex` is keyed by identifier, but it and `CacheCatalogObject` take `Schema.CatalogObject`, and a key or an order is not one.
- **Generated names.** `Schema.Object.GetGeneratedName(seed, id)` appends an identifier to make a long derived name unique, so two unnumbered objects would collide on the name. Every caller enumerated: device scalar-type and operator maps, event handlers, references, operators. No key or order feeds one. Key and order names are built from their columns (`EnsureKeyName`, `GetFullName`) and never contain an identifier.
- **A stale `DAE.ObjectID` tag copied with the meta data.** `CopyTableVar` merges or inherits the source key's meta data, which can carry the source's `DAE.ObjectID` tag after a `ForStorage` emit. `SaveObjectID` writes through `MetaData.Tags.AddOrUpdate`, which overwrites, so the copy's own number wins.
- **`Program.Plan` creating a `Plan` lazily** at the three run-time sites that now touch it. The precedent the handoff cited is real and in the same layer (`OrderTables.cs:701`, `RestrictTables.cs:198`, `TableValue.cs:797`), so no plan is constructed that was not constructed before.
- **The handoff's dead-code census.** Re-derived and confirmed: `Directory.Build.props` sets `EnableDefaultItems=false` and every `.csproj` lists its sources explicitly, so `src/client/DAE.Client/View-263.cs`, `src/engine/DAE/Runtime.Data/TableBuffer.cs`, `src/engine/DAE/Runtime.Instructions/GeneratorNodes.cs` and `src/engine/DAE/Runtime.Instructions/TestNodes.cs` are compiled by nothing; `DebugNodes.cs:476`, `LibraryNodes.cs:1318` and `:1391`, and `TestFramework.Coverage.Base/Source/TestNodes.cs:901-1210` sit inside block comments spanning lines 261-528, 1302-1449 and 768-1325 respectively (measured). The handoff is right that the ticket's per-file table counted dead code as work; a reader should use the handoff's inventory, not the ticket's.

### Parked as a tripwire: none new

The one conditional concern worth raising - that the engine spends catalog identifiers on compile-time objects no catalog will hold, so a long-lived engine moves towards the `ObjectIDsExhausted` error the chain's first step introduced - **is already recorded at its site**, in `ObjectIDGenerator.Allocate`, with its own order-of-magnitude estimate, and it is weighed again under "Identifier exhaustion" in `bug-object-id-counter-is-process-wide`. Re-filing or re-parking it would duplicate a call already made. The two fixes above reduce that consumption rather than add to it.

### Filed as tickets: none

No finding reached the filing bar. Everything found was a comment or a one-line allocation at a single site, fixable in this pass, and nothing named a class-level invariant that the chain's remaining two tickets do not already carry.

### Tests

**No new test, deliberately.** The ticket asked for none and that holds up: this step changes no behaviour on any engine path, and keys and orders are created and persisted by a large share of the coverage scripts already, so the conversion is covered by what exists. The project's rule that a behaviour port owes a characterization test is satisfied where the behaviour actually changes - `bug-object-id-counter-is-process-wide` owns that test - and the one property this step must not break, that an engine numbers its built-in objects the same on every start, is already pinned by `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart`, which stops a server and starts it again against its own store. A test asserting that a particular key comes out unnumbered would pin the implementation rather than a contract.

The implementer's tests were reviewed as code: the diff contains none, so there was nothing to cut.

The honest limit, as the handoff already said: a key or order built on a path the suite never persists could be unnumbered and nothing would notice. By the design that is harmless; it is unproven rather than proven.

## Validation

Run after the review's fixes, from a clean tree.

- `dotnet build Dataphor.slnx` - **0 errors, 143 warnings**, the unchanged baseline.
- The three suite parts from `AGENTS.md`, each in the foreground: **664 passed / 0 failed / 33 skipped** (part 1, 6 m 11 s), **67 / 0 / 11** (part 2, 3 m 6 s), **50 / 0 / 12** (part 3, 3 m 5 s). Identical to the baseline the implement and prereq commits recorded. `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart` is in part 1 and passed, which is the check that this build numbers its built-in objects the same on every start.
- `python tools/net-spike/run.py <scratch> --smoke` - the only build that covers the client and Windows sites, and the only one that smoke-runs an engine with the libraries loaded, which is what exercises the memory-device change. Matches the `AGENTS.md` baseline exactly: all converted and client projects 0 errors; `Dataphoria` 1 (`TreeNode.ContextMenu`), `DAE.Service.ConfigurationUtility` 1 (`NotifyIcon.ContextMenu`), `D4Runner` blocked on `NU1201`, all three pre-existing; smoke 1,080 operators, concurrency 8 threads x 15 iterations 0 errors, resolution cache 0 errors.
- `dotnet run --project tools/net-spike/EngineCheck -c Debug` - `RESULT: pass`, 1,060 operators and 23 scalar types, the recorded figures.
- No pre-existing failures surfaced; `tickets/.pre-existing-error.md` was not written.
- Source hygiene: each of the five files the review edited keeps its tabs, its CRLF line endings and its byte-order-mark state (`Schema.cs` has a mark, the other three `.cs` files do not; `docs/migration-notes.md` is LF and stayed LF, one line changed). No file was reformatted or re-encoded, and nothing was left behind in the tree.

## What the next ticket in the chain needs to know

`engine-object-id-transient-tablevars` is next, then `bug-object-id-counter-is-process-wide` flips the generator to one per engine and deletes the `Schema.Object` statics.

- `Schema.Object(string name)` **still allocates** from the process-wide counter; `engine-object-id-transient-tablevars` is the step that gives it `UnassignedID`. Until then, any `Schema.Object` subclass built from a name alone silently takes a number from the shared counter, which is why that ticket's build-breaking `ResultTableVar` signature change is the census it relies on.
- The three guards are the safety net and are now all spelled `Schema.Object.UnassignedID`, so the set is one grep: `Schema.Object.SaveObjectID`, `ServerCatalogDeviceSession.InternalInsertPersistentObject`, `CatalogDeviceSession.CacheCatalogObject`.
- `libraries/SQLDevice/Source/SQLDevice.cs` spells the constant fully qualified (`Alphora.Dataphor.DAE.Schema.Object.UnassignedID`) in `CreateSparseKeyFromOrder`, because `SQLDevice` has a `Schema` property that shadows the namespace in expression position and a bare `Object` would be ambiguous with `System.Object`. It is the only site like it; leave it alone.
