description: A transaction that changed one of a storage device's settings and was rolled back left the running device enforcing the rolled-back value; the running device now goes back together with the saved catalog, and so does a statement of that kind that fails half-way through.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Device.Catalog/ClassLoader.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
repro: verified
----
## What was wrong

Some D4 objects are backed by a .NET class, and the statement that makes one carries a *class definition*: a class name plus named attributes that are applied to the live object as properties. A memory device's row limit is one of those - `alter device D alter class { create "MaxRowCount" = "100" }` sets `MaxRowCount` on the running device object and records the attribute in the catalog.

Rolled back, the attribute came out of the class definition and the property stayed on the running object, so the server went on enforcing a row limit its own catalog did not hold until it was restarted. The cause was in the undo: `AlterClassDefinitionInstruction.Undo` built the inverse alteration and replayed it, and the inverse of a created attribute is a *drop* - and the drop loop in `AlterNode.AlterClassDefinition` only edits the class definition, where the create and alter loops also set the property on the live object.

A second defect at the same site, found while writing the ticket's required part-way-failure case: the instruction was built before the change but **added to the log after it**, and the inverse alteration could not have undone a half-applied change anyway. `ClassLoader.SetProperty` converts each attribute's string to the property's type and throws on one it cannot convert, so a statement with two attributes whose second value is bad had already applied the first - and with nothing in the log, neither the class definition nor the live object was put back. Even with the recording moved earlier the inverse would have failed: it would try to drop an attribute the throwing create never added (`Attributes.RemoveAt(IndexOf(name))` with `IndexOf` answering -1), and the rollback loop logs that exception and moves on, abandoning the rest of that instruction's undo.

## What changed

**`src/engine/DAE/Device.Catalog/ClassLoader.cs`** - new `public static bool TryGetProperty(object, string, out string)` beside `SetProperty`: the named property's value in the string form `SetProperty` takes, or false when the object exposes no readable property of that name.

**`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`** - `AlterClassDefinitionInstruction` now records, at construction, a copy of the class definition *and* the live instance's own value for every attribute the alteration names (create, alter and drop alike); `Undo` puts the copy back wholesale and then sets the recorded values on the instance. `CatalogDeviceSession.AlterClassDefinition` adds the instruction to the log **before** applying the change, as `AlterMetaData` has always done.

**`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`** - one `NOTE:` above the forward `DropAttributes` loop, which is otherwise untouched: a forward drop still leaves the property the dropped attribute set, and whether it should is `backlog/bug-dropped-class-attribute-stays-on-the-live-object`.

**`docs/migration-notes.md`** - one row under *D4, catalog and libraries* (a rolled-back or half-applied device setting change no longer sticks) and one under *Custom C# extensions* (the new `TryGetProperty`).

## Three deviations from the implement ticket, each deliberate

**The undo restores a copy of the class definition instead of applying the inverse alteration.** The ticket said to keep the inverse and add the instance restore after it. That would still have failed the part-way case above: the inverse's drop of an attribute the throwing create never added raises out of the undo before the instance restore could run. Restoring the recorded copy is total, order-independent, and leaves `AlterNode.AlterClassDefinition` - which the forward path shares - untouched, so no forward D4 behaviour moves. `_alterClassDefinition` and the `originalClassDefinition` constructor parameter go with it; the instruction clones the definition itself, so a caller cannot hand it a stale copy.

**`TryGetProperty`, not a throwing `GetProperty`.** The ticket asked for `public static string GetProperty(object, string)` raising `PropertyNotFound`, and flagged the risk itself. The recording now runs *before* the alteration, so a throwing read would fail a statement in the recording rather than where it failed before - and for a property an object exposes with no getter it would fail a statement that works today, which is a D4 behaviour change this project's rules do not allow without a ticket saying so. A `GetProperty` that only the capture calls and that the capture then has to catch around would be worse than either. One new public member, used; the skip and its reason are a comment at the capture.

**The tests are a new fixture, not additions to `CatalogStoreTest.cs`.** That fixture is about the SQLite catalog *store* and starts a server per test; this behaviour is in-memory undo, and the sibling `bug-server-link-user-change-survives-rollback` put the same shape in its own `ServerLinkUserRollbackTest` over `InProcessTestFixture` (one server per fixture, one session per test). `DeviceClassAttributeRollbackTest.cs` follows it, with its `<Compile Include="..." />` entry after `DeferredDeviceStopTest.cs`.

## Tests

`tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs`, new. The memory device is the subject because its `MaxRowCount` needs nothing outside the engine; the SQL devices take class attributes of their own through the same code.

| Test | What it verifies | Before the fix |
|---|---|---|
| `TestRolledBackCreateKeepsTheOldMaxRowCount` | the reproduction: a rolled-back `create` attribute leaves the live device's `MaxRowCount` at -1, and the emitted `create device` statement without the attribute | fails, `Expected: -1 But was: 100` |
| `TestFailedAlterKeepsTheOldSettings` | the second defect: an `alter class` whose second attribute value cannot be converted leaves both of the device's original values, with no explicit transaction - the statement's implicit one undoes it | fails, `Expected: True But was: False` |
| `TestRolledBackSecondStatementDoesNotKeepTheFirstsValue` | two `alter class` statements on one device in one transaction undo to the state before the first: reverse replay lands the earliest-recorded value last | fails, `Expected: -1 But was: 100` |
| `TestNestedRollbackKeepsTheOuterChange` | an inner rollback undoes its own level only (the outer level's 100 survives it), and the outer rollback then restores -1 | fails on the final assertion, `Expected: -1 But was: 100` |
| `TestRolledBackCreateWithNoInstanceRestoresTheClassDefinition` | the no-live-instance path (an operator's class definition) restores on its own, with nothing to put back on an instance | **passes before and after** |

The last one is characterization, not a reproduction: the ticket asked for it only if the coverage scripts did not already reach that path, and they do not - `grep -rn "alter class" --include=*.d4 tests/libraries/` finds three lines, all `alter device System.Temp alter class { alter "MaxRowCount" = "-1" }`, which is the *live-instance* path. It is worth having because the first deviation above changes how the no-instance path is restored. Reviewers: cut it if you disagree.

The "before the fix" column was measured, not inferred: the pre-fix instruction and session method were put back, the solution rebuilt and the fixture run - 1 passed, 4 failed with the messages above - and the fix then restored and re-run.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors, nothing suppressed; no warning from any of the three edited engine files or the new test file (grepped the build output for each file name).
- `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~DeviceClassAttributeRollbackTest"`: 5 passed.
- The sibling fixture, re-run because it exercises the same undo log: `FullyQualifiedName~ServerLinkUserRollbackTest`, 8 passed.
- The three part commands from the root `AGENTS.md`, each in the foreground: part 1 697 passed, 33 skipped, 0 failed, 8 m 6 s; part 2 67 passed, 11 skipped, 0 failed, 3 m 1 s; part 3 50 passed, 12 skipped, 0 failed, 3 m 4 s. The skips are the coverage manifest's known-failure, unstable and fragment cases. No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written. Part 1 at 8 m 6 s is close to the runner's ten-minute idle limit and worth watching, as `AGENTS.md` already says of it.
- Source hygiene: tabs, CRLF and byte-order-mark state preserved in every edited file, checked at byte level (no lone LF anywhere; byte-order mark unchanged on each - present on `CatalogDeviceSession.cs`, `ClassLoader.cs`, the new test and the `csproj`, absent on `DDLNodes.cs` and `migration-notes.md`). The one space-indented added line is the `csproj` entry, matching that file.

## What to look at in review

Read the diff, then: the *Instructions* and *Transactions* regions of `CatalogDeviceSession.cs` (the replay loop in `InternalRollbackTransaction`, which **logs and swallows** an exception out of any one `Undo` and carries on with the next instruction - which is why the abandoned-undo defect above was silent; `undidDDL`; `InvalidateCachesAfterUndo`), `AlterNode.AlterClassDefinition` with `AlterDeviceNode`, `AlterOperatorNode` and `AlterScalarTypeNode` in `DDLNodes.cs`, `ClassDefinition` and `ClassAttributeDefinitions` in `Language.D4/Language.cs`, and `ClassLoader.CreateObject`.

Specific things worth a second pair of eyes:

- **The three live-instance call sites** are all in `AlterDeviceNode`: the device itself, a `Schema.DeviceScalarType` and a `Schema.DeviceOperator`. Each of those objects is itself created from the class definition it carries - the device by `ClassLoader.CreateObject`, the two map objects by `plan.CreateObject` in `Compiler.CompileDeviceScalarTypeMap` and its operator counterpart - and that applies every attribute with `SetProperty`. That is the argument that an attribute sitting in a class definition is always a property of its instance, and so that the capture's skip never triggers in practice. Established by reading, not proved.
- **`Attributes.Clear()` then re-adding clones** replaces the attribute objects inside the live class definition. Nothing was found that holds a reference to one across a statement (the forward alter loop mutates `Attributes[name].AttributeValue` in place and keeps nothing; the emitter and `CreateObject` only read), but it is the one thing the wholesale restore does that the inverse alteration did not.
- **The undo now always assigns `ClassName`**, where the inverse only did so when the forward alteration had changed it. Same value when unchanged. The forward path still refuses a class-name change on an object that has a live instance (`UnimplementedAlterCommand`).
- **Nothing is recorded when no transaction is open** - the file's standard `(!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction` guard, unchanged. In practice that is narrow: every statement runs inside an implicit transaction unless the process has turned implicit transactions off, which is what `TestFailedAlterKeepsTheOldSettings` relies on.

## Known gaps

- **A forward `drop` of a class attribute still leaves the property it set on the live object.** Left alone deliberately: it is a separate decision (should a drop restore the class's default, or the value the object held before the attribute was created?) and it is filed as `backlog/bug-dropped-class-attribute-stays-on-the-live-object`, with the `NOTE:` at the drop loop pointing there. The *undo* of a drop is correct - the undo re-creates the attribute and also restores the recorded instance value.
- **A device started inside the rolled-back transaction** is stopped by the rollback's deferred device stops and has its `Registered` flag cleared by `StartDeviceInstruction` / `RegisterDeviceInstruction`. The implement ticket recorded those as already correct; this change touches neither, and they were not re-measured.
- **No SQL device was exercised.** The fix is at the site both share and nothing in it is device-specific, but a test over a SQL device's own class attributes would need a database server.
- **The class behind this bug is already tracked**: `tickets/implement/2-debt-rollback-leaves-catalog-as-it-was.md` proposes the general test (script the catalog, run a statement in a transaction, roll it back, script again, require the scripts identical). Nothing new was filed for the class. One thing that ticket should know: a catalog script would **not** have caught this bug - the emitted script was identical before and after, which is exactly why it went unnoticed - so its "scripts identical" check needs a companion for state *derived* from a definition, and this ticket's `MaxRowCount` is the worked example.
- No tripwires were parked and no tickets filed. The one conditional concern at the site is the drop loop, which already has the backlog ticket above.
