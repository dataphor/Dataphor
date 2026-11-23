description: Running the same statement twice can apply the wrong value the second time. When a statement adds one of a storage device's settings, the server stores the statement's own copy of that setting inside the live device; a later statement that changes the setting edits that stored copy, so the next run of the first statement quietly applies the changed value instead of the one it is written with.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Schema/Device.cs, src/engine/DAE/Schema/Operator.cs, src/engine/DAE/Schema/ScalarType.cs, tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs, docs/migration-notes.md
repro: verified
----
## What goes wrong

Some D4 objects are backed by a .NET class, and the statement that makes or alters one carries a *class definition*: a class name plus named attributes that are applied to the live object as properties. A storage device's settings are of that kind — a memory device's `MaxRowCount` row limit, a SQL device's server name and connection parameters.

`alter device D alter class { create "MaxRowCount" = "100" }` puts the statement's own attribute object — the object holding the name `MaxRowCount` and the text `100` — straight into the device's live class definition. The device and the statement now share it. A later `alter device D alter class { alter "MaxRowCount" = "200" }` writes `200` into that shared object, which is also writing `200` into the first statement. Run the first statement a second time and it applies 200.

Measured on 2026-11-23 against an in-process server, through two different ways of re-running a statement, both of which a client and the Dataphor IDE use:

- **Through the remembered-plan path.** The engine keeps the compiled form of a statement so that running the same text again is cheap (`src/engine/DAE/Server/PlanCache.cs`, consulted by `ServerProcess.PrepareStatement`, `src/engine/DAE/Server/ServerProcess.cs:818`, which `IServerProcess.Execute` goes through). `alter device D alter class { create "MaxRowCount" = "100" }`, then `alter … alter "MaxRowCount" = "200"`, then `alter … drop "MaxRowCount"`, then the first statement again byte for byte: the device's live row limit came out at **200**, and the device scripted out carrying `"MaxRowCount" = "200"`, from a statement that says 100.
- **Through a plan the caller prepared and executed twice.** `IServerProcess.PrepareStatement` on the same first statement, execute it, run the alter and the drop, execute the prepared plan again: 200 again. This route does not involve the remembered-plan cache at all, so it reproduces whatever that cache does.

Nothing here involves a transaction or a rollback; every statement commits normally.

## Root cause

`AlterNode.AlterClassDefinition` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1007`) adds the statement's `ClassAttributeDefinition` to the live definition rather than a copy of it:

```csharp
foreach (ClassAttributeDefinition attributeDefinition in alterClassDefinition.CreateAttributes)
{
    if (instance != null)
        ClassLoader.SetProperty(instance, attributeDefinition.AttributeName, attributeDefinition.AttributeValue);

    classDefinition.Attributes.Add(attributeDefinition);   // shares the statement's object
}
```

The `alter` loop twenty lines above then writes through whatever is in the live definition (`classDefinition.Attributes[name].AttributeValue = …`), so it writes into the first statement.

The invariant being broken is that **a catalog object owns its class definition, and never shares the `ClassDefinition` or `ClassAttributeDefinition` objects of a statement.** Present at this site since the initial open-source commit; not a port consequence.

## The fix, and what was measured about it

`ClassAttributeDefinition.Clone()` already exists (`src/engine/DAE/Language.D4/Language.cs`), and `ClassDefinition.Clone()` uses it. Adding `.Clone()` at the line above is the whole fix for the behaviour above: with that one line changed, both reproductions pass, through the remembered plan and through the prepared plan alike. Measured both ways on 2026-11-23, and measured failing again with the line reverted.

Two further things were measured while the cause was being found, and they shape the work:

**The compiler also hands statements' class definitions straight to the objects it builds** — `node.NewDevice.ClassDefinition = localStatement.ClassDefinition` (`src/engine/DAE/Compiling/Compiler.cs:9378`) and eight siblings (5708, 6559, 6911, 6925, 6948, 9135, 9188, 9218; 4861 already clones and is the shape the others should follow). That sharing turned out **not** to be observable through any sequence tried: a create statement's class definition is only read again by running that create statement again, and doing so hits different defects with different causes, filed as `implement/bug-recreated-device-comes-back-as-the-one-that-was-dropped` and `blocked/bug-prepared-create-statement-executes-twice` and explicitly **not** part of this ticket. So the compiler side is latent, and closing it here is hardening that keeps the invariant true for whatever lands next rather than a fix for an observed symptom.

The cheapest way to close it is to make the catalog objects' `ClassDefinition` setters clone — six properties instead of nine call sites, and the next site added cannot forget it. That was built and run with the clone above: parts 2 and 3 of the suite were green (3 m 09 s and 3 m 06 s, inside the range `AGENTS.md` records for them) and so was the frontend golden pass. Part 1 was run once in a strictly larger configuration — these setters plus the metadata copy described below — where its only failures were the metadata one and the reproductions of the separate defect above; it was not re-run with the metadata copy removed, so run it. No slowdown was observed on catalog load, which is the path that assigns these for every object it loads and which already parses and compiles each object's persisted create script before doing so. The clone it adds there is one small allocation against that.

**The same shape in metadata must not be fixed the same way.** A catalog object's `MetaData` is likewise shared with the statement that made it, and a copying setter on `Schema.Object.MetaData` (`src/engine/DAE/Schema/Schema.cs:455`) was tried: it builds, and it **changes 14 of the 416 derived forms** that `FrontendDerivationGoldenTest` pins — form titles and one page type on `Security.Role` and `Security.SetUserDeviceUserPassword`. The cause was isolated to one site, `src/engine/DAE/Compiling/Compiler.cs:4394`, and is filed as `backlog/bug-view-tags-lose-to-tags-inherited-from-the-source`. **Do not touch metadata ownership in this ticket**, and do not rewrite those goldens.

## TODO

- Clone the attribute at `src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1007`: `classDefinition.Attributes.Add(attributeDefinition.Clone());`, with a short comment saying why — the live definition's attributes stay owned by the object, the alter loop writes through them, and a statement the caller may run again still holds its own.
- Make the six catalog-side `ClassDefinition` setters clone, so the invariant holds without the compiler having to remember: `Schema.DeviceOperator` (`src/engine/DAE/Schema/Device.cs:129`), `Schema.DeviceScalarType` (`Device.cs:230`), `Schema.Device` (`Device.cs:643`), `Schema.OperatorBlock` (`src/engine/DAE/Schema/Operator.cs:104`), `Schema.ScalarType.FromClassDefinition` (`src/engine/DAE/Schema/ScalarType.cs:1386`) and `Schema.ScalarType.ClassDefinition` (`ScalarType.cs:1405`) — each `value == null ? null : (ClassDefinition)value.Clone()`, with a remark on the property saying that the object owns its definition and that callers mutate it through the getter. Leave the compiler's nine assignments alone; the setters cover them. If a setter that clones is judged too surprising, clone at those nine sites instead and say so in the handoff — but do not leave both the setters and the sites sharing.
- `Schema.Object.MetaData` stays exactly as it is.
- Add the two reproductions as cases. They need `IServerProcess.Execute` and `IServerProcess.PrepareStatement`, not `ExecuteScript`, because only those consult the remembered plan and hold a plan the caller can run twice. `tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs` already has the helper that reads a memory device's live row limit and the one that scripts it out, but its name and its summary are about rollback and neither case is one: either rename the fixture in the same commit or put the cases in a fixture of their own that reuses those helpers. The fixture's base class is `InProcessTestFixture`, whose `Process` is the `IServerProcess`; both cases need no database server and ran in under 100 ms each.
- Add an entry to `docs/migration-notes.md` under *D4, catalog and libraries*: a session that runs an `alter … alter class { create … }` statement a second time, after a statement that changed the same setting, now applies the value the statement states instead of the changed one. Note in the same entry that `Schema.Device.ClassDefinition`, `Schema.ScalarType.ClassDefinition`, `Schema.ScalarType.FromClassDefinition`, `Schema.OperatorBlock.ClassDefinition`, `Schema.DeviceScalarType.ClassDefinition` and `Schema.DeviceOperator.ClassDefinition` now store a clone of what is assigned, so a custom extension that assigns one of them and then mutates the object it assigned no longer changes the catalog object — it must mutate through the getter.
- Build, then run the suite in the three parts `AGENTS.md` gives, each as its own foreground command. Part 1 was measured at 9 m 03 s and 9 m 45 s on 2026-11-23, close to the runner's ten-minute idle limit; `fix/debt-test-part-one-near-the-runner-limit` is the ticket for that and it is not this ticket's business, but allow for it when planning the run.
