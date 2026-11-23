description: Removing one of a storage device's settings takes it out of the saved definition but leaves the running device using the old value, so the server keeps enforcing a setting the administrator has just removed.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Device.Catalog/ClassLoader.cs, tests/DAE.Server.Tests/DeviceClassAttributeFixture.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: Every option is a change an administrator could notice - either a statement that silently did nothing useful starts raising an error, or the server starts constructing a throwaway object to find out what a setting's default is - and a maintainer may prefer today's behaviour, which at least is what the previous release did, with the limitation written down instead.
----
## What goes wrong

Some D4 objects are backed by a .NET class, and the statement that creates one carries a *class definition*: the class name plus named attributes that are applied to the live object as properties. A memory device's row limit is one of those, set by `alter device D alter class { create "MaxRowCount" = "100" }` or cleared by `alter device D alter class { drop "MaxRowCount" }`.

The clear only half works. The attribute is removed from the saved definition — the device scripts out as if the setting had never been there — and the live device keeps the value. Until the server is restarted it goes on enforcing a limit that nothing in the catalog mentions.

Measured on an in-process server on 2026-11-23: a memory device given `MaxRowCount = 100`, then `alter device D alter class { drop "MaxRowCount" };` outside any transaction. The emitted `create device` statement afterwards carried no attribute at all; the live device still reported `MaxRowCount = 100`.

## Where it is

`AlterNode.AlterClassDefinition` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:976`). Its three loops are not symmetric: the `alter` and `create` loops set the property on the live object through `ClassLoader.SetProperty` when there is one, and the `drop` loop only edits the definition. Present in the initial open-source commit; this is not a port consequence.

The *rollback* face of the same two lines — the undo of a `create` attribute, which the engine expresses as a drop — is fixed separately by `bug-rolled-back-class-attribute-stays-on-the-live-object`, which leaves a `NOTE:` at the drop loop pointing here. That ticket does not change the forward behaviour, because the forward fix needs the decision below.

## Why it is not simply fixed

What should a dropped attribute leave the live object at? The honest answer is "whatever it would have been had the attribute never been set", which is the property's default on a freshly constructed object of that class — and the server does not have one to read. The three options, none free:

- **Raise.** The same function already refuses to change the *class name* of an object that has a live instance (`RuntimeException.Codes.UnimplementedAlterCommand`); dropping an attribute could refuse the same way. It changes D4 semantics for a statement that works today, which is a compatibility surface — though nothing in this repository's libraries, samples or coverage scripts uses `alter class { drop … }` (checked: the only uses anywhere are three `alter "MaxRowCount"` lines in test libraries), so the practical risk is small.
- **Reset from a fresh instance.** Construct a throwaway object from the class definition to read the property's default, then set that. It runs a device constructor for its side effects, which is a worse thing to do on a live server than the defect.
- **Write the limitation down.** Record at the site that a dropped class attribute does not reach the live object until the server restarts, and leave the behaviour. Cheapest, and the behaviour the previous release had.

A maintainer should pick before this is worked. If the answer is the third, it is an accepted tradeoff and belongs as a `NOTE:` at the site rather than as a fix.

## How a fix would be confirmed

The reproduction above, as a case in `tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs`, which `bug-rolled-back-class-attribute-stays-on-the-live-object` added for the rollback face of these two lines and which already has the device helpers this needs: a memory device given a row limit, the limit dropped, and the live device's limit asserted. It needs no database server. The fixture's name says *rollback*; a forward case is the first thing in it that is not one, so either rename the fixture in the same commit or say in the handoff why it stays.

## Second arm: dropping a setting that is not there reports an array error

Found while reviewing `bug-remembered-statement-applies-a-changed-setting`, in the same two lines of the same loop, and verified on 2026-11-23 against an in-process server.

`alter device D alter class { drop "Bogus" };` — a name the object has no such setting for, which is what a typo looks like — fails with

```
Runtime error: Index was outside the bounds of the array.
```

wrapping a raw `System.IndexOutOfRangeException`. Nothing says which name was wrong, or that a name was the problem at all. The `alter` arm of the same statement, given the same bogus name, reports it properly: `Class "MemoryDevice" does not contain a property named "Bogus".`

The cause is one line. `AlterNode.AlterClassDefinition` drops by `classDefinition.Attributes.RemoveAt(classDefinition.Attributes.IndexOf(attributeDefinition.AttributeName))`, and `IndexOf` returns `-1` for a name that is not there, which the list's indexer turns into an array error. The `alter` arm reaches its attribute through `Attributes[name]`, whose own getter raises `ClassAttributeNotFound` naming the attribute — so the error this arm should raise already exists and is already what D4 says elsewhere in the same statement. Nothing is corrupted: the index is read before the list is modified, so the drop leaves the definition as it was, and the statement's rollback record was taken before any of this.

This is a separate defect from the first arm above and does not depend on how that one is decided, but it is the same loop and the same reading, so whoever picks up one should fix both. It was left for this ticket rather than fixed in the review that found it, because it changes what error a D4 statement raises, which this project's rules treat as a compatibility surface needing a ticket that says so. The practical risk is the same as the first arm records: nothing in this repository's libraries, samples or coverage scripts uses `alter class { drop … }` outside three `alter "MaxRowCount"` lines in test libraries.

A reproduction belongs in `tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs` alongside the first arm's, or in a forward-behaviour fixture over the same base — note that the device helpers this ticket's last section points at have since moved out of that file into `tests/DAE.Server.Tests/DeviceClassAttributeFixture.cs`, which both that fixture and `DeviceClassAttributeRerunTest` now derive from, so a new fixture can reuse them without the renaming question that section raises.
