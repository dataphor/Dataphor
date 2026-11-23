description: Running the same statement twice can apply the wrong value the second time. The server keeps a parsed copy of each statement it has run; a later statement that changes the same setting edits that parsed copy in place, so the next run of the first statement quietly applies the changed value instead of the one it is written with.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Server/PlanCache.cs, tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs
repro: verified
----
## What goes wrong

Some D4 objects are backed by a .NET class, and the statement that makes one carries a *class definition*: a class name plus named attributes that are applied to the live object as properties. A storage device's settings are of that kind — a memory device's `MaxRowCount` row limit, a SQL device's server name and connection parameters.

The engine remembers the compiled form of a statement so that running the same text again is cheap (the **plan cache**, `src/engine/DAE/Server/PlanCache.cs`; a plan is handed back as long as no catalog change has advanced `Catalog.PlanCacheTimeStamp` since it was compiled). The compiled form holds the statement's parsed text, and the parsed text's class definition — the actual objects holding the attribute names and values — is handed to the catalog object as *its own* definition, not copied. A later `alter` statement writes the new value into that definition in place, which is also writing it into the first statement's remembered parsed text. Run the first statement again from the cache and it applies the value the later statement wrote.

Two sequences, both measured on 2026-11-23 against an in-process server, both run through the ordinary client execute path (`IServerProcess.Execute`, which is what a client and the Dataphor IDE use and which is the path that consults the plan cache):

**An `alter` that creates an attribute.** `alter device D alter class { create "MaxRowCount" = "100" }`, then `alter device D alter class { alter "MaxRowCount" = "200" }`, then `alter device D alter class { drop "MaxRowCount" }`, then the **first statement again, byte for byte**. The device's live row limit came out at **200**, and the device scripted out carrying `"MaxRowCount" = "200"`, from a statement that says 100.

**A `create` that carries attributes.** `create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" }`, then `alter device D alter class { alter "MaxRowCount" = "200" }`, then `drop device D`, then the **create statement again**. The new device came up with a row limit of **200**.

Nothing here involves a transaction or a rollback; both sequences commit normally throughout. The second one is the worse of the two, because a drop-and-recreate from a script is an ordinary deployment step: a catalog rebuild script that creates a device, later alters one of its settings, and is then re-run in the same session can build the device with the altered setting instead of the one the script states — and no error is raised either time.

## Root cause, and why this is one ticket

One invariant is missing: **a catalog object must own its class definition — never share the `ClassDefinition` or `ClassAttributeDefinition` objects of the statement that made it.** Two kinds of site break it, and both have to change together or the sequences above still reproduce through whichever one is left.

- **The forward create loop of `AlterNode.AlterClassDefinition`** (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1007`) adds the statement's own `ClassAttributeDefinition` to the live definition: `classDefinition.Attributes.Add(attributeDefinition)`. The `alter` loop two loops above it then writes through that shared object (`classDefinition.Attributes[name].AttributeValue = …`). `ClassDefinition.Clone()` and `ClassAttributeDefinition.Clone()` already exist (`src/engine/DAE/Language.D4/Language.cs`) and are what `ClassDefinition.Clone` itself uses.
- **The compiler hands a statement's `ClassDefinition` straight to the object it is building.** The sites found by reading `src/engine/DAE/Compiling/Compiler.cs`, each to be confirmed and changed or argued safe: `node.NewDevice.ClassDefinition = localStatement.ClassDefinition` (9378, the measured one), `node.ScalarType.ClassDefinition = localStatement.ClassDefinition` (5708), `node.CreateOperator.Block.ClassDefinition = localStatement.Block.ClassDefinition` (6559), the three aggregate-operator blocks (6911, 6925, 6948), `deviceScalarType.ClassDefinition = classDefinition` (9135) and `deviceOperator.ClassDefinition = …` (9188, 9218). One of them already clones (4861, a scalar type taking its class definition from a representation's property type), which is the shape the others should follow.

Whether to clone at each assignment or to make the catalog objects' `ClassDefinition` setters clone is the design question this ticket has to settle. A setter that clones is one change instead of nine and cannot be forgotten by the next site added, but it is a surprising setter and it costs a clone on catalog load, which assigns these for every object in the catalog; measure that before choosing it.

## Scope: what is and is not in this family

- **Metadata tags are the same shape and did not reproduce.** `AlterNode.AlterMetaData` also mutates `objectValue.MetaData.Tags` in place, and the compiler also assigns `localStatement.MetaData` directly. The sequence above, run on a table's tags (`create table T { … } tags { Foo = "1" }`, alter the tag, drop the table, re-create), came out with the right value. Whether that is because the metadata is copied somewhere on the way or because that particular plan was not handed back from the cache was **not** established — settle it as part of this ticket rather than assuming the metadata is safe.
- **This is not the rollback defect.** `bug-rolled-back-class-attribute-stays-on-the-live-object` (complete) fixed what a *rolled-back* class alteration leaves behind. Its undo rebuilds the live definition from clones, so an object that has been through a rolled-back alteration no longer shares anything with a statement — which narrows this defect's reach slightly but closes none of it, since both sequences above commit.
- **This is not the dropped-attribute defect.** `backlog/bug-dropped-class-attribute-stays-on-the-live-object` is the other half of the same two loops — a forward `drop` not reaching the live object — and is a separate decision with a separate root cause. Do not fold it in.
- Present since the initial open-source commit at every site read; not a port consequence.

## How a fix would be confirmed

Both sequences above as cases in `tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs`, or in a fixture of its own if that one's rollback framing no longer fits — they need `IServerProcess.Execute` rather than `ExecuteScript`, because only the former consults the plan cache, and the existing fixture already has the helper that reads a memory device's live row limit. Both were seen to fail before any fix, each with `Expected: 100 But was: 200`.

A fix is also a `docs/migration-notes.md` entry under *D4, catalog and libraries*: a script that re-runs a `create device … attributes { … }` after altering that setting gets a different device than it used to.
