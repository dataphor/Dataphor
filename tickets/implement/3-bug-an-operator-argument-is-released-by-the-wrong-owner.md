description: Passing a picture or document value into a routine, or reading one out of a record, makes the engine clean it up at the wrong moment - either never, so the memory is held for the rest of the session, or twice, so a later read of the record fails with an error. The fix is to let the piece of the plan that produced the value say who owns it, instead of guessing from how the routine declared its parameter.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/PlanNode.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, src/engine/DAE/Runtime.Instructions/ExtractionNodes.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime.Instructions/ListNodes.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, docs/architecture/value-lifetime.md, docs/architecture.md, docs/migration-notes.md
repro: verified
difficulty: hard
----
## The one fact the engine cannot express

A value the engine keeps in a managed stream - the D4 types `Binary` and `Graphic`, and rows, lists and tables - is not reclaimed by the garbage collector. Something has to release it, exactly once. For a variable that something is its stack slot (`Stack.PokeOwned`, `Stack.ReleaseAbove`). For an argument of a call it is `InstructionNodeBase.CleanupOperand` (`src/engine/DAE/Runtime.Instructions/InstructionNode.cs`), which decides from the **operand's modifier** alone:

- `in` - release the value.
- `var` - write it back to the caller's slot, release nothing.
- `const` - do nothing at all.

But whether the call owns an argument's value is not a property of the modifier. It is a property of **the plan node that produced the argument**. A node that reads something out of a stack slot can hand over the slot's own value rather than a copy; a node that computes a value - a literal, a conversion, another call - produces one that nothing else holds. Two node classes carry that fact today, each as a public `bool` field set by a type test in `InstructionNodeBase.DetermineDataType` (`StackReferenceNode.ByReference`, `StackColumnReferenceNode.ByReference`), and a third - `ExtractColumnNode`, which is what `LR.B` compiles to for a row variable `LR` - hands over the row's own column value **without being asked and with no way to say so**. `CleanupOperand` never asks any of them.

Three failures follow, all verified and all older than the recent value-ownership fixes (see *Measurements* below). They are one bug: one missing fact, read in one place.

### Arm 1 - a `const` operand's computed value is never released

```
create operator F(const AB : Binary) : Integer begin result := Length(AB.Value); end;
F(Binary('0001'));              -- one stream held for the life of the session, every call
```

This reaches far more code than the word `const` suggests. The compiler **promotes** a by-value operand to `const` whenever the operator's body never assigns to it (`Compiler.CompileOperatorBlock`), so most ordinary declarations end up `const`. And a great many declarations say `const` outright: every comparison, conversion, selector and property accessor the engine generates for a scalar type, and most of the `Frontend` library.

### Arm 2 - a by-value operand given a record's column destroys the record's copy

```
create operator G(AB : Binary) begin AB := Binary('0002'); end;   -- assigns to it, so the operand stays by-value
var LR : row { B : Binary } := row { Binary('0001') B };
G(LR.B);
LR.B.Value;                     -- Runtime error: StreamID (n) not found
```

`ExtractColumnNode` hands `G` the row's own value, and by-value cleanup releases it.

### Arm 3 - reading a record's column into anything that will own it

Found while researching this ticket, and the worst of the three because the D4 is entirely ordinary - no operator, no modifier, just reading a field:

```
begin
    var LR : row { B : Binary } := row { Binary('0001') B };
    var LB : Binary := LR.B;    -- LB's slot and the row now hold one value
end;                            -- Runtime error: StreamID (n) not found, at the end of the block
```

The read succeeds; the block's exit releases the slot and then the row, and the second release fails. `LB := LR.B;` over an existing variable and `row { LR.B B }` fail the same way. Same cause as arm 2: `ExtractColumnNode` lends a value to a consumer that takes ownership of it.

## The fix: make ownership a question a node answers

Two virtuals on `PlanNode` (`src/engine/DAE/Runtime.Instructions/PlanNode.cs`):

```csharp
/// <summary>Whether Execute returns a value that belongs to something else - a stack slot, or a row in one - rather than one the caller owns and must release.</summary>
public virtual bool ReturnsByReference { get { return false; } }

/// <summary>Asks this node to return its value by reference rather than a copy, and reports whether it will.</summary>
public virtual bool SetByReference() { return false; }
```

Overridden by the nodes that can lend a value:

- `StackReferenceNode` and `StackColumnReferenceNode` (`ImperativeNodes.cs`): both over their existing `ByReference` field. `SetByReference()` sets it and returns `true`.
- `ExtractColumnNode` (`ExtractionNodes.cs`): a new private flag, set only through `SetByReference()`, and only when `_shouldDisposeSource` is `false` - a source row this node disposes on the way out cannot lend a column, because the column would be dead before the caller read it. `InternalExecute` returns `DataValue.CopyValue(program.ValueManager, row[columnIndex])` unless the flag is set. `_shouldDisposeSource` is `false` exactly when the source is a row-typed `StackReferenceNode`, which the node marks by-reference at the same time, so the two stay in step.

  Make the same change at the node's **table** return site, where `_shouldDisposeSource` is always `true` and the copy is therefore unconditional. That branch is dormant: the only place that builds an `ExtractColumnNode` is `Compiler.EmitColumnExtractorNode`, which raises `InvalidExtractionTarget` for anything but a row-typed source, so nothing reaches `InternalExecute`'s `objectValue as ITable` path today. It is worth correcting in passing rather than leaving as a trap, because the value it returns is one the row it then disposes owns - `TableValue.Select()` builds its row with `ValuesOwned` set, so disposing it clears the column - which means that path would hand back a released value the first time anything reached it. One `NOTE:` at the site saying it is unreachable from the compiler is enough; it needs no test.
- `IndexerNode` (`ListNodes.cs`): override `ReturnsByReference` over its existing public `ByReference` field, and **do not** add `SetByReference`. Nothing sets that field today, so the node always copies and the override is dead; it is there so that whoever does set it meets the ownership rule rather than a silent leak. A `NOTE:` at the site should say so and point at `bug-a-list-value-never-releases-its-elements`, which owns list element lifetime.

Then the two sites in `InstructionNode.cs`:

- `InstructionNodeBase.DetermineDataType`, `Modifier.Const` branch: `Nodes[index].SetByReference();` replaces the two type tests. The `Modifier.Var` branch keeps its own code - it requires a `StackReferenceNode` and raises a compiler error otherwise, so the type test there is the check, not an inference.
- `InstructionNodeBase.CleanupOperand`, new `Modifier.Const` branch: release the value unless `argumentNode.ReturnsByReference`.

**The invariant that makes the bad state unrepresentable, and the reason arms 2 and 3 close:** a node returns a borrowed value only when it was asked to, and only the `const` branch asks. So every other argument value, and every value handed to a variable, a row selector or a list, belongs to whoever received it. That is why `CleanupOperand`'s `in` branch can go on releasing unconditionally, and it is worth writing down at that site - it is the whole load-bearing claim. The store and the release then read one flag on one object, so they cannot drift apart the way `Modifier` and `ByReference` did.

## What this is not

The earlier research on this ticket recorded a third group of arguments - a `const` operand of an operator that "takes the value over" and stores it in what it returns, which a release would destroy - and concluded that making that group expressible was the work. **It does not exist.** Every generated operator that looked like a candidate copies its argument: `ScalarSelectorNode`, `ValidatingScalarSelectorNode`, `ScalarReadAccessorNode` and `ScalarWriteAccessorNode` return `DataValue.CopyValue(...)`, and `CompoundScalarSelectorNode` assigns through `Row`'s indexer, which copies (`RowValue.cs`). The breakage that reading attributed to selectors - `row { Binary('0001') B }` then losing its column - was arm 3 all along: the naive fix released a `const` operand's value whenever the argument node was not one of the two recognized classes, and `LR.B.Value` passes an `ExtractColumnNode` to a `const` operand. Giving that node a voice is what fixes it.

## Measurements

Each script run on its own, `Diagnostics.StreamCount()` read before and after, on an in-process Debug server (a throwaway fixture on `InProcessTestFixture`, since removed). `delta` is streams left behind; `error` is the error the script raised.

| script | at `6e4d5ee4` (before the assignment fix) | at head `998b5f10` | with the fix above |
|---|---|---|---|
| `F(Binary('0001'))`, function, `const` operand | delta 1 | delta 1 | delta 0 |
| the same as a procedure | delta 1 | delta 1 | delta 0 |
| the same with a nested call as the argument | delta 1 | delta 1 | delta 0 |
| `F(LB)`, variable argument (control) | delta 0 | delta 0 | delta 0 |
| arm 2, `G(LR.B)` | error | error | ok |
| arm 3, `var LB : Binary := LR.B` | error | error | ok |
| arm 3, `LB := LR.B` | error | error | ok |
| arm 3, `row { LR.B B }` | error | error | ok |
| compound scalar selector `ZComp(Binary('0001'), 3)` | delta 1 | delta 1 | delta 0 |
| reading a compound scalar's property | delta 2 | delta 2 | delta 0 |
| writing a compound scalar's property | delta 4 | delta 3 | delta 0 |
| `row { Binary('0001') B }` then `LR.B.Value` (control) | ok | ok | ok |
| `Binary('0001')` then `LB.Value` (control) | ok | ok | ok |
| `LB.Value := '0002'` (control) | ok | ok | ok |

Every arm is pre-existing: the three columns differ only where the recent fixes changed how much a broken script leaked on its way out, not whether it was broken.

A list of binaries read through its indexer went from delta 3 to delta 2 with the fix (the index expression's own leak closes); the remaining 2 belong to `bug-a-list-value-never-releases-its-elements` and are not this ticket's to close.

## Tests

The fixture that owns this ground is `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs`; its class remarks already describe the ownership rule and should gain a sentence about the operand half of it. Follow the shape of the cases already there: a loop of `AssignmentCount` (or `RowAssignmentCount` for the row cases) so that a per-call regression reports a full count rather than one, a value assertion beside every count assertion so a case that stops exercising the path cannot pass quietly, and `AssertScriptLeavesNoStreams` / `AssertRaisingScriptLeavesNoStreams` as the harness.

- A `const` operand given a computed value leaves the stream count where it found it. Both a function and a procedure: they were different bugs historically (the by-value-to-`const` promotion read the wrong symbol for every operator with a result until `bug-assigning-a-binary-variable-leaks-its-stream`), and a regression could reach one and not the other.
- A `const` operand given a variable still leaves the count alone and still sees the value - this is the by-reference path that must keep working, and nothing covers it today.
- A by-value operand given a row variable's column: the call completes and the row's column still reads back its original value (arm 2).
- A row variable's column read into a new variable, over an existing variable, and into a row selector: each leaves both the copy and the row's own column readable and the count balanced (arm 3).

That is four behaviours and one control; do not add a case per operator shape beyond them.

## Also in this change

- **`docs/migration-notes.md`** - `CleanupOperand`'s `const` branch now releases an argument value a custom C# instruction node used to be handed and nothing released. An extension node that stored such a value and read it after its own `InternalExecute` returned will now read a released value; it has to copy. `ExtractColumnNode` now copies a row column except when a `const` operand asked it not to, so an extension that depended on getting the row's own value sees a copy. Both are behind `PlanNode.ReturnsByReference` / `SetByReference`, which are new public virtuals - name them, since an extension node that lends values should override them.
- **A new architecture page, `docs/architecture/value-lifetime.md`**, and a line for it in the index list in `docs/architecture.md`. Nothing under `docs/architecture/` owns value lifetime today - no page mentions the stack, streams or who releases a value - and `sessions-processes-plans.md` is within fifty words of the 1,500-word cap, so it cannot absorb this. The page should say who owns a value in a stack slot, in an operand of each modifier, in a row and in a list, what releases each, and how a plan node declares that it is lending rather than giving. Run `python tools/docs-size-check/check_size.py` afterwards.
- `src/engine/DAE/Runtime/Stack.cs`'s `PokeOwned` remarks list the sites that use a plain `Poke` and why; the operand write-back entry stays right, but a reader arriving from the new page should find the two in agreement.

## Watch for

- `ImperativeNodes.cs` is also named by `bug-a-list-value-never-releases-its-elements`, which touches `ForEachNode` around lines 450-660 while this ticket touches `StackReferenceNode` and `StackColumnReferenceNode` around lines 1866 and 1989. Different regions; neither ticket needs to wait for the other.
- `InternalClone` must carry the new `ExtractColumnNode` flag, beside `_shouldDisposeSource`.
- Source hygiene: all four source files are tabs and CRLF, and `ExtractionNodes.cs`, `ImperativeNodes.cs`, `InstructionNode.cs` and `PlanNode.cs` each keep their current byte-order-mark state. `AssignmentValueOwnershipTest.cs` has a byte-order mark.

## Validation

`dotnet build Dataphor.slnx` first. Then, because this changes a decision every D4 script reaches:

- the ownership fixtures: `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest"` - 17 cases, about 11 s, all passing with the fix as prototyped;
- both halves of the coverage-script suite, which is the check that matters here because every D4 script in it calls generated `const` operators constantly. Measured with the fix as prototyped: part A-R 72 passed / 11 skipped in 3 m 56 s, part S-Z 50 passed / 12 skipped in 3 m 23 s - both green, no manifest entry needs flipping. The two commands are under *Agents run the suite in parts* in the root `AGENTS.md`.

Part 1 of that three-part split - everything except the coverage scripts - was also run against the prototype and was green: 915 passed, 0 failed, 33 skipped. It reported **11 m 13 s** on this machine, well past the 5 to 7 minutes the root `AGENTS.md` records for it and past the agent shell's own ten-minute command limit, so treat it as a run to leave to CI rather than one to plan a ticket around. The two coverage halves plus the two ownership fixtures are the set to run here.

## TODO

- Add `ReturnsByReference` and `SetByReference` to `PlanNode`, documented as the ownership question and its answer.
- Override both on `StackReferenceNode` and `StackColumnReferenceNode` over their existing `ByReference` fields.
- Give `ExtractColumnNode` a by-reference flag set only through `SetByReference()` and only when its source is borrowed; copy the column at both return sites otherwise; carry the flag in `InternalClone`.
- Override `ReturnsByReference` on `IndexerNode` with a `NOTE:` saying why it has no `SetByReference` and which ticket owns list element lifetime.
- Replace the two type tests in `InstructionNodeBase.DetermineDataType`'s `const` branch with `Nodes[index].SetByReference()`.
- Add the `Modifier.Const` branch to `CleanupOperand`, and record at the `Modifier.In` branch the invariant that lets it release unconditionally.
- Add the five test cases to `AssignmentValueOwnershipTest.cs` and extend its class remarks to cover the operand half of the ownership rule.
- Write `docs/architecture/value-lifetime.md`, add its line to `docs/architecture.md`, and run the docs size check.
- Add the `docs/migration-notes.md` entry.
- Validate as above and name the filters used in the handoff.
