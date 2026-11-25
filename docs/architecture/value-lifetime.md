# Value lifetime

Who owns a value the garbage collector will not reclaim, and what releases it. Part of the [Dataphor architecture](../architecture.md).

Most values the engine moves around are ordinary .NET values - an integer, a string, a `DateTime` - and nothing has to be done about them. Some are not. A `Binary` or a `Graphic` is held as a **stream**, a numbered buffer the server owns (`StreamID`, allocated and deallocated through the process's stream manager); a row, a list or a table is held as a structure (`Row`, `ListValue`, `TableValue`, or the native `NativeRow`, `NativeList`, `NativeTable` behind them) whose own columns or elements can themselves be streams. None of that is reclaimed by the garbage collector. Something has to release each one, **exactly once**: release it never and the server holds the memory until the session ends, release it twice and the second release fails - a stream the manager no longer knows about is reported as `StreamID (n) not found`, which is how most of these defects were found.

So every such value has, at every moment, exactly one owner. The rest of this page says who that owner is in each place a value sits, and how the two sides of a hand-over agree about which of them it was.

## A stack slot owns the value in it

A D4 variable is a slot in the process's stack (`Stack`, `src/engine/DAE/Runtime/Stack.cs`), and the slot owns what is in it. Two consequences, and the engine has a method for each:

- **Overwriting a slot releases what it displaced.** `Stack.PokeOwned` stores a value and releases the value it replaced, so that the store and the release cannot drift apart; it releases nothing when the slot already holds the value being stored. Every assignment goes through it. A plain `Poke` is for the sites that are *not* replacing a value the slot owns, and each of those says so where it is.
- **Leaving a scope releases what the scope declared.** `Stack.ReleaseAbove(depth, manager)` releases the values of every slot above a depth and leaves each slot null. The constructs that open a scope - a `begin ... end` block, a `try ... except` handler, an operator call, an aggregate call - call it in the `finally` that already pops the scope, so a scope left by a `raise`, an `exit`, a `break` or a `continue` releases its variables like one that ran to its end. Depth, not a compiled list of variables, is what makes that work for a block that raised before it had declared them all.

A slot can also hold a value it does **not** own: a `foreach` loop lends its loop variable the one row the iterator keeps re-reading into, and assigning to such a variable must release nothing. The compiler marks those slots (`Symbol.IsBorrowed`, set through `Symbols.SetIsBorrowed`) and `PokeOwned` leaves them alone.

## An operand's owner depends on its modifier and on its argument

A call's arguments are evaluated by the caller, pushed into the callee's slots by `CallNode`, then cleaned up by `InstructionNodeBase.CleanupOperand`, which reads the operand's modifier:

- `var` - the callee's slot is an alias of a caller's slot. Nothing is copied and nothing is released; the cleanup writes the slot's final value back with a plain `Poke`, because the caller's slot already owned it. That write-back runs on every exit path, including a `raise`, so the caller sees the callee's last assignment either way.
- `in` - the callee's slot holds a value of its own, and the cleanup releases it. `CallNode` hands the slot's current value back first, so a body that assigned to the operand released the displaced value itself and the cleanup releases its replacement.
- `const` - the callee's slot cannot be assigned to (the compiler makes a const operand's symbol constant), and whether the call owns the value is **not** a property of the modifier but of the plan node that produced the argument.

That last point is the one the engine used to be unable to express. A node that reads a stack slot, or a column of a row in one, can hand over the value it already holds rather than a copy; a node that *computes* a value - a literal, a conversion, another call - produces one nothing else holds. Only the node knows which, and the difference matters on nearly every call: the compiler promotes a by-value operand the body never assigns to to `const` (`Compiler.CompileOperatorBlock`), and every comparison, conversion, selector and property accessor generated for a scalar type declares `const` outright.

An argument that raises leaves the call unmade, and `ReleaseUncalledOperand` is that path's own rule: release the values the call owns among the arguments already evaluated, and leave every `var` operand alone - there is no callee value to write back, and writing one would run the type's constraints while an error unwinds.

## How a node says it is lending

Two virtuals on `PlanNode` (`src/engine/DAE/Runtime.Instructions/PlanNode.cs`):

- `SetByReference()` asks the node to return its value by reference rather than a copy, and answers whether it will.
- `ReturnsByReference` answers, afterwards, whether it does.

The const branch of `InstructionNodeBase.DetermineDataType` asks, once, while the call is compiled; `CleanupOperand` reads the answer back off the same node at run time and releases the value unless the node lent it. The store and the release therefore read one flag on one object.

The nodes that can lend are the three that have something to lend: `StackReferenceNode` (a variable), `StackColumnReferenceNode` (a column of a row-typed variable) and `ExtractColumnNode` (`LR.B`, a column read out of a row). The third lends only when the row it reads from outlives the call - a source row the node disposes on its way out owns its column values, so lending one would hand back a value released before it could be read. `IndexerNode` (a list element) overrides `ReturnsByReference` but deliberately has no `SetByReference`: nothing asks it, so it always copies, and the override is there so that whoever changes that meets this rule rather than a silent leak.

**The invariant worth stating plainly: a node lends only when it was asked, and only the `const` branch asks.** Everything else that receives a value - a variable, a row selector, a list, a by-value operand - therefore owns what it is given, which is why `CleanupOperand` can release an `in` operand's value unconditionally and why an assignment can release what it displaces.

## A row owns its columns, a list its elements

A `Row` or `ListValue` with `ValuesOwned` set releases its columns or elements when it is disposed, which is the default and the usual case; a value built as a window onto a native structure something else holds - every `DataValue.FromNative`, `FromNativeRow` and `FromNativeList` - clears the flag instead, and the three `DataValue.Copy` overloads set it on the container they build over a native structure of their own. So a column or element read out of a container is copied before it is handed to anything that will own it, and a value put *into* a container is copied too, so that the caller keeps owning what it passed. `DataValue.CopyValue` and `DataValue.CopyNative` make the copy, `DataValue.DisposeValue` and `DataValue.DisposeNative` perform a release, and all four dispatch on what the value actually is - an `IDataValue`, a bare `StreamID`, or a native structure whose members are walked.

Two consequences for the code that fills a container. A node that evaluates a list, a row or a cursor row and hands it over - a list selector, `ToList`, the return expression of a `for each ... return` - releases what it produced once the container has copied it. And a `foreach` over a list *lends* the loop variable's slot an element the list still owns, so the slot is nulled rather than released on the way out and the list's own release is what frees the element; a loop variable the script declared itself is therefore nil once the loop has finished, as it has always been for a `foreach` over a cursor.

`TableValue` is the exception to the paragraph above and deliberately so: it has no disposal of its own, because a D4 table variable's rows live in the memory device and are released when that device drops the native table, and a `TableValue` built by `DataValue.Copy` therefore releases nothing when it is disposed. The `NOTE:` at the class records the measurement behind that and why closing the gap would be worse than leaving it.

## Where to look

| Concern | Code |
|---|---|
| Stack slot ownership, the two rules above | `src/engine/DAE/Runtime/Stack.cs` (`PokeOwned`, `ReleaseAbove`) |
| Borrowed slots | `src/engine/DAE/Compiling/Symbols.cs` (`IsBorrowed`) |
| Operand cleanup, by modifier | `src/engine/DAE/Runtime.Instructions/InstructionNode.cs` (`CleanupOperand`) |
| The lending question | `src/engine/DAE/Runtime.Instructions/PlanNode.cs` (`SetByReference`, `ReturnsByReference`) |
| Operand slots and the result slot of a call | `src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs` (`CallNode`) |
| Copying and releasing a value of any shape | `src/engine/DAE/Runtime.Data/DataValue.cs` (`CopyValue`, `DisposeValue`) |
| Streams themselves | `src/engine/DAE/Streams/` |
