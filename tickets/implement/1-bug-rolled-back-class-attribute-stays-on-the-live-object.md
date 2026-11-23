description: When a transaction that changed one of a storage device's settings is rolled back, the saved setting goes back to what it was but the running device keeps the rolled-back value, so the server enforces a limit its own catalog does not hold.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Device.Catalog/ClassLoader.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
repro: verified
difficulty: medium
----
## What goes wrong

Some D4 objects are backed by a .NET class, and the statement that creates one carries a *class definition*: the class name plus a list of named attributes that are applied to the live object as properties. A memory device's row limit is one of those: `alter device D alter class { create "MaxRowCount" = "100" }` sets `MaxRowCount` on the live device object as well as recording the attribute in the catalog.

Run that inside a transaction and roll the transaction back. The attribute is correctly removed from the class definition — the device scripts out exactly as it did before — but the live device object keeps `MaxRowCount = 100`. The running server now enforces a row limit that its own catalog does not contain, and goes on doing so until it is restarted.

Measured on an in-process server on 2026-11-23: `create device P_D3 class "System.MemoryDevice";` then, inside an `Isolated` transaction, `alter device P_D3 alter class { create "MaxRowCount" = "100" };`, rolled back. The device's emitted `create device` statement was identical before and after; `MaxRowCount` was `-1` before and `100` after.

This is the same shape as the two instances the sibling ticket `bug-server-link-user-change-survives-rollback` fixed, and the reason it was not caught by scripting the catalog: the *definition* is restored, and the state derived from the definition is not.

## Why, exactly

`AlterClassDefinitionInstruction.Undo` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs:612`) builds the inverse alteration and replays it through `AlterNode.AlterClassDefinition(classDefinition, undoClassDefinition, instance)`. The inverse of a `create` attribute is a `drop` attribute — and in `AlterNode.AlterClassDefinition` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:976`) the three loops are not symmetric:

```csharp
foreach (ClassAttributeDefinition attributeDefinition in alterClassDefinition.DropAttributes)
    classDefinition.Attributes.RemoveAt(classDefinition.Attributes.IndexOf(attributeDefinition.AttributeName));

foreach (ClassAttributeDefinition attributeDefinition in alterClassDefinition.AlterAttributes)
{
    if (instance != null)
        ClassLoader.SetProperty(instance, attributeDefinition.AttributeName, attributeDefinition.AttributeValue);
    …
}
```

The `alter` and `create` loops set the property on the live instance; the `drop` loop only edits the class definition. So any undo that is expressed as a drop — which is every undo of a `create` attribute — leaves the live object alone.

The undo of an `alter` attribute is correct, because it is replayed as an `alter` with the recorded original value. Only the create/drop pair is wrong.

## What to change

Give `AlterClassDefinitionInstruction` the values it needs and restore them on the instance itself, rather than relying on a drop to do it.

- Capture, **before** the change, the live instance's current value for every attribute the alteration will create, alter or drop. That needs a read counterpart to `ClassLoader.SetProperty` (`src/engine/DAE/Device.Catalog/ClassLoader.cs:41`) — a small `public static string GetProperty(object, string)` using the same `GetProperty`/`PropertyNotFound` shape, returning the value as a string. Add it next to `SetProperty`; it is new public surface, so it gets a line in `docs/migration-notes.md` under *Custom C# extensions*.
- In `Undo`, after putting the class definition back as it does today, set each captured value back on the instance with `ClassLoader.SetProperty`. Restore the **recorded** values rather than re-reading the class definition, for the same reason the sibling ticket's `ApplyServerLinkMetaDataInstruction` does: instructions replay in reverse, so an undo must not recompute from state a later-undone instruction will change. Put that reason in a comment on `Undo`.
- Capture when the instruction is constructed, and construct it **before** the change is applied, as `AlterMetaDataInstruction` does — the sibling ticket's review found a half-applied `alter server` left standing because the instruction was built before the change but added to the log after it. The same trap is here: `AlterClassDefinition` can throw part-way (`ClassLoader.SetProperty` converts the string to the property's type and throws on a value it cannot convert) after it has already set earlier attributes.
- Leave the forward `DropAttributes` loop as it is. A forward `alter device D alter class { drop "X" }` has the same symptom and is a separate decision, filed as `backlog/bug-dropped-class-attribute-stays-on-the-live-object`; put a `NOTE:` at the drop loop pointing at that ticket so the next reader does not re-find it.

## Edge cases & interactions

- **A class definition with no live instance** (`instance` is null — the two-argument overload, used for scalar type accessors and the like). Nothing to capture and nothing to restore; the undo must behave exactly as it does today. Covered by a test case only if one of the existing coverage scripts does not already exercise it — check before adding.
- **An attribute whose name is not a property of the live object.** `ClassLoader.SetProperty` raises `PropertyNotFound`; the forward statement therefore fails before anything is recorded, so there is nothing to capture. Verify by inspection that the capture loop does not raise on an attribute the forward path would have rejected — if it can, capture lazily per attribute and skip what cannot be read, recording why in a comment.
- **A statement that fails part-way.** The implicit transaction every statement runs in undoes it, which is what makes the "record before the change" rule above load-bearing. Pin it with a test case: an `alter class` that sets two attributes where the second value cannot be converted must leave the device with both original values.
- **Several `alter class` statements on the same device in one transaction, rolled back.** Each records its own captured values and they replay in reverse, so the earliest-recorded values land last. Verify by reading the replay order; a test case for two statements is worth having because it is the case a "recompute from the class definition" fix would get wrong.
- **Nested rollback.** The inner level's instructions only; the outer level's attribute must survive. Test case.
- **A device started inside the transaction.** `StartDeviceInstruction` already exists and the device's `Running` and `Registered` flags were measured to be restored correctly; this ticket changes nothing there. By inspection.
- **Devices other than the memory device.** The SQL devices take class attributes too (server name, connection parameters); the fix is at the shared site, so nothing is device-specific. A test against a SQL device would need a database server, so the reproduction uses the memory device's `MaxRowCount`, which needs nothing.

## TODO

- Add `ClassLoader.GetProperty(object, string) : string` beside `SetProperty`.
- Have `CatalogDeviceSession.AlterClassDefinition` construct `AlterClassDefinitionInstruction` with the captured instance values and add it to the log **before** applying the change; have `Undo` restore them on the instance.
- `NOTE:` at `AlterNode.AlterClassDefinition`'s `DropAttributes` loop naming `backlog/bug-dropped-class-attribute-stays-on-the-live-object`.
- Reproductions in `tests/DAE.Server.Tests/CatalogStoreTest.cs`: a rolled-back `alter device … alter class { create "MaxRowCount" = "100" }` leaves the live device's `MaxRowCount` as it was (seen to fail before the fix); the part-way failure case; the nested case. One test per behaviour.
- `docs/migration-notes.md`: one entry under *D4, catalog and libraries* (a rolled-back device setting change no longer sticks) and one under *Custom C# extensions* (the new `ClassLoader.GetProperty`).
- Build (`dotnet build Dataphor.slnx`), run the new cases, then the three part commands from the root `AGENTS.md`, each in the foreground.
