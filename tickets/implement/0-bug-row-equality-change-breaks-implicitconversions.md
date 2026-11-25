description: Comparing two rows whose columns are of different but convertible number types destroys the first row, so the next thing that reads it fails. This broke an existing test, and the build has been failing on every commit since.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/RowComparisonNodes.cs, src/engine/DAE/Runtime.Instructions/RowNodes.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, docs/architecture/operand-ownership.md, tests/libraries/TestFramework.Coverage.Scripts/Documents/ImplicitConversions.d4
difficulty: medium
repro: verified
----

## The symptom

`dotnet test tests/DAE.Server.Tests --filter Name=ImplicitConversions` fails, in Debug and Release, on every commit since `e321e388` (`ticket(implement): bug-row-equality-leaks-the-rows-it-compares`). The reported error is useless on its own:

> `RuntimeException: Runtime error: Could not determine the current location for program <guid>. <= ServerException: Could not determine the current location for program <guid>. <= NullReferenceException: Object reference not set to an instance of an object.`

The statement in `ImplicitConversions.d4` that fails is the row block:

```d4
begin
	var ALeftRow : row { ID : Long } := row { 5 ID };
	var ARightRow : row { ID : Byte } := row { ToByte(5) ID };
	if ALeftRow <> ARightRow then
		raise Error("Row equal with implicit conversions failed.");

	if ARightRow <> ALeftRow then   -- this line fails
		raise Error("Row equal with implicit conversions failed.");
end;
```

The **first** comparison destroys `ALeftRow`'s value; the second one then reads a disposed row. Narrowed by hand: after one `ALeftRow <> ARightRow`, a plain `select ALeftRow.ID` fails the same way, while `select ARightRow.ID` is fine, and two comparisons of two rows of the *same* column type are fine. So it is specifically the side that the compiler wraps in an implicit conversion that survives, and the side it leaves alone that is destroyed.

## Root cause

One site: `RowEqualNode.DetermineDataType` in `src/engine/DAE/Runtime.Instructions/RowComparisonNodes.cs`, and the same shape in `CompoundScalarEqualNode.DetermineDataType` in the same file.

Both now begin with `base.DetermineDataType(plan)` and *then* rewrite their own argument nodes. `base.DetermineDataType`'s const branch is the one place in the engine that asks an argument node to lend the value it already holds rather than copy it (`Nodes[index].SetByReference()`, `src/engine/DAE/Runtime.Instructions/InstructionNode.cs`), and both operands of `iEqual` are const. So the sequence is:

1. `base.DetermineDataType` asks `Nodes[0]` - the `StackReferenceNode` for `ALeftRow` - to lend. It says yes and sets its own `ByReference`, so from now on it hands over the stack slot's own row rather than a copy.
2. The rewrite below then finds the two row types unequal and inserts an implicit conversion **between this node and that argument node**: `Nodes[1] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[1], context), Nodes[0].DataType)` in the failing case, so `Nodes[1]` becomes a conversion over the `ARightRow` reference and `Nodes[0]` stays the bare `ALeftRow` reference that was already told to lend.
3. The lend permission was granted to a node that is no longer the operand on the other side, and - the part that actually bites - the row the lending node lends is now read by a consumer that was not there when the question was asked.

The consumer is reached like this. `Compiler.ConvertNode` → `ConvertRowNode` → `EmitRedefineNode` (`Compiler.cs`), whose node-emitting branch is behind `#if USEREDEFINENODE`, which is defined nowhere, so the live rewrite runs instead: `source add { … } remove { … } rename { … }`. Over a row that is a `RowExtendNode` with a `RowProjectNode` and a `RowRenameNode` above it, and `RowExtendNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/RowNodes.cs`) ends with

```csharp
finally
{
	sourceRow.Dispose();
}
```

unconditionally. `RowExtendNode` is correct for every way the compiler reaches it today - nothing asks its source to lend, so its source hands it a copy that is its to release - but here the comparison node had already asked, one level up, so the row it disposes is the one `ALeftRow`'s slot still names.

(Confirmed, not inferred: with `RowEqualNode` left broken, guarding that one `Dispose` with `if (!Nodes[0].ReturnsByReference)` makes the whole script pass, which identifies `RowExtendNode` as the disposer. `RowRedefineNode` is **not** involved - it is the unreachable implementation that `plan/debt-unreachable-redefine-nodes` is about.)

Two further notes on why this was so hard to read, both of which belong in the handoff:

- The real failure is a `NullReferenceException` in `ExtractColumnNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/ExtractionNodes.cs`, at `int columnIndex = row.DataType.Columns.IndexOf(Identifier);`) - the disposed row answers `null` for `DataType`. `PlanNode.Execute`'s catch turns that into the readable "A nil was encountered where a value was expected".
- That readable message is then thrown away. The catch asks `program.GetLocation(this, true)`, which dereferences `((RuntimeStackWindow)_stack.CurrentStackWindow).Locator`, and the window that `RowEqualNode.InternalCompare` pushed with `program.Stack.PushWindow(0)` has a null `Locator`, so `GetLocation` raises and the original exception is replaced by "Could not determine the current location for program <guid>". That masking is a separate, pre-existing defect and is filed as `backlog/bug-a-runtime-error-inside-a-row-or-table-comparison-loses-its-message`; **do not fix it here**, and do not expect this ticket's work to improve any error message.

## The correction

Ask after the rewrite, not before: the lend has to be granted to the node that actually ends up in the operand slot. In both `DetermineDataType` overrides, put `DetermineModifiers(plan)` back at the top and move `base.DetermineDataType(plan)` to the very end, with the `_dataType` assignment after it. `DetermineModifiers` is idempotent (it only re-reads `Modifiers`), `base` guards its body on `Operator != null` as the generic-row branch needs, and `base` sets `_dataType` from `Operator.ReturnDataType`, which is why `_dataType = plan.DataTypes.SystemBoolean;` has to follow it rather than precede it.

For `RowEqualNode` that is:

```csharp
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);

			if (!Nodes[0].DataType.IsGeneric && !Nodes[1].DataType.IsGeneric) // Generic row comparison must be done at run-time
			{
				… unchanged: the upcast/conversion rewrite and the _comparisonNode compilation …
			}

			base.DetermineDataType(plan);
			_dataType = plan.DataTypes.SystemBoolean;
		}
```

and the same two moves in `CompoundScalarEqualNode.DetermineDataType` (its rewrite is not inside an `if`, so `base` goes after the `plan.Symbols.PopWindow()` that ends the `_comparisonNode` compilation).

Replace the comment that currently sits above the `base` call at the top with one above the call at the bottom saying *why it is at the bottom*: the const branch of `base.DetermineDataType` is the only place that asks an argument node to lend, this node replaces its own argument nodes with implicit conversions, and asking before that rewrite grants the lend to the node underneath the conversion - whose innermost consumer, `RowExtendNode`, disposes the row it is handed. Keep both halves of the leak fix intact: the ask still happens (so a variable's row is still lent rather than copied) and `InternalExecute` still releases each argument whose node answers `ReturnsByReference` false.

This was prototyped and verified before the ticket was written; the results are below. With the fix, the converted side is a conversion node that was never asked and therefore copies - exactly what happened before `e321e388` - so no value is released twice and nothing is leaked.

## What this is **not**

Do not instead make `RowExtendNode` respect `Nodes[0].ReturnsByReference`. That also makes the script pass (it was tried), but it leaves the grant going to the wrong node, so the next rewrite that interposes a consumer has the same bug. Record it as a tripwire instead - see the TODO below.

## Verification already done (Debug, on the prototype)

- `dotnet test tests/DAE.Server.Tests --filter "Name=ImplicitConversions|FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~OperandOwnershipShapeTest|FullyQualifiedName~ContainerValueOwnershipTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest"` - 41 passed, 0 failed.
- `dotnet test tests/DAE.Server.Tests --filter "Name=ImplicitConversions|Name=Conversions|Name=RowLibrary|Name=RowAllocation|Name=RowInsert|Name=RowParameterColumnOrder|Name=Types.CompoundEquality|Name=Types|Name=TestSargabilityWithConversion|Name=Operators|Name=EntryLibrary|Name=TableAndRowValuedAttributes|Name=StreamValues|Name=StreamAllocation|Name=ScalarAllocation|Name=Domains|Name=LibraryTypes"` - 14 passed, 0 failed, 3 skipped (`Domains`, `EntryLibrary`, `StreamAllocation`, which the coverage manifest already excludes).

Rerun both of those. Release was not run for the prototype; run `Name=ImplicitConversions` once in Release (`-c Release`) as well, since the "Done when" asks for it.

## The guard that was missing

`ImplicitConversions.d4` already covered this and neither the implement nor the review stage of the row-equality ticket ran it, which is the whole reason it reached CI. A D4 coverage script is the right layer for the behaviour but it is not in the set either stage chose, so add the case to `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs` instead - the fixture those stages *did* run, and the one that already owns the leak half of this rule. One test, covering the behaviour and not the mechanism: two row variables whose single column differs by a convertible scalar type, compared both ways round, then each variable read again, with `StreamCount()` unchanged across the scope so the leak half stays pinned in the same case. Do not add a new `.d4` script, and do not touch `ImplicitConversions.d4`.

## Documentation

- `docs/architecture/operand-ownership.md` owns this rule and needs one sentence: the ask happens once, on the node in the operand slot at the time, so a node that rewrites its own argument nodes must ask after the rewrite. **The page is at 1,499 words against a 1,500-word cap**, so trim an equivalent amount from the now-historical narration of `bug-row-equality-leaks-the-rows-it-compares` in the same edit, and run `python tools/docs-size-check/check_size.py`.
- No new `docs/migration-notes.md` entry. Nothing an application, a deployment script or a C# extension can see changes: the comparisons answer as they always did, and the entry `e321e388` added still reads true after this fix. Say so in the handoff rather than adding a row.

## TODO

- Move the ask after the argument rewrite in `RowEqualNode.DetermineDataType` and `CompoundScalarEqualNode.DetermineDataType` (`src/engine/DAE/Runtime.Instructions/RowComparisonNodes.cs`), as above, and replace the explanatory comment with one that says why the `base` call is last.
- Add the tripwire at the site: a `NOTE:` on `sourceRow.Dispose()` in `RowExtendNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/RowNodes.cs`) saying that this releases the source row unconditionally, which is right only because nothing asks its source node to lend; a rewrite that interposes this node under a consumer that did ask releases a row it was only lent, as the row-equality change did. Mention it in the handoff.
- Add the one characterization test to `AssignmentValueOwnershipTest`.
- Add the sentence to `docs/architecture/operand-ownership.md`, trim to stay under the cap, and run the size check.
- `dotnet build Dataphor.slnx`, then rerun the two filters above in Debug and `Name=ImplicitConversions` in Release. Name the filters in the handoff.
