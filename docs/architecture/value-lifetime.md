# Value lifetime

Who owns a value the garbage collector will not reclaim, and what releases it. Part of the [Dataphor architecture](../architecture.md).

Most values the engine moves around are ordinary .NET values - an integer, a string, a `DateTime` - and nothing has to be done about them. Some are not. A `Binary` or a `Graphic` is held as a **stream**, a numbered buffer the server owns (`StreamID`, allocated and deallocated through the process's stream manager); a row, a list or a table is held as a structure (`Row`, `ListValue`, `TableValue`, or the native `NativeRow`, `NativeList`, `NativeTable` behind them) whose own columns or elements can themselves be streams. None of that is reclaimed by the garbage collector. Something has to release each one, **exactly once**: release it never and the server holds the memory until the session ends, release it twice and the second release fails - a stream the manager no longer knows about is reported as `StreamID (n) not found`, which is how most of these defects were found.

So every such value has, at every moment, exactly one owner. The rest of this page says who that owner is in each place a value sits - a stack slot, a row, a list - and what releases it there. Who owns the value handed to a call's operand, and how the two sides of that hand-over agree about which of them it was, is [operand-ownership.md](operand-ownership.md).

## A stack slot owns the value in it

A D4 variable is a slot in the process's stack (`Stack`, `src/engine/DAE/Runtime/Stack.cs`), and the slot owns what is in it. Two consequences, and the engine has a method for each:

- **Overwriting a slot releases what it displaced.** `Stack.PokeOwned` stores a value and releases the value it replaced, so that the store and the release cannot drift apart; it releases nothing when the slot already holds the value being stored. Every assignment goes through it. A plain `Poke` is for the sites that are *not* replacing a value the slot owns, and each of those says so where it is.
- **Leaving a scope releases what the scope declared.** `Stack.ReleaseAbove(depth, manager)` releases the values of every slot above a depth and leaves each slot null. The constructs that open a scope - a `begin ... end` block, a `try ... except` handler, an operator call, an aggregate call - call it in the `finally` that already pops the scope, so a scope left by a `raise`, an `exit`, a `break` or a `continue` releases its variables like one that ran to its end. Depth, not a compiled list of variables, is what makes that work for a block that raised before it had declared them all.

A slot can also hold a value it does **not** own: a `foreach` lends its loop variable the row or element the iterator owns and goes on reading that value, so an assignment to the loop variable must release nothing. The compiler marks those slots (`Symbol.IsBorrowed`, set through `Symbols.SetIsBorrowed`) and `PokeOwned` leaves them alone. The loop pays for the loan at both ends. Before it lends, it releases whatever the slot already held - that value is the slot's own and nothing else would free it - unless the compiler says that slot's value is not the loop's to release, which is so for two of them: a slot an enclosing loop over the same variable is already lending, whose value belongs to the outer loop, and a `const` operand's slot, whose value the argument that produced it may only have lent to the call ([operand-ownership.md](operand-ownership.md)). Releasing either would be a release too many. And the mark reaches the body of the loop and no further: the loop leaves the slot nil, so the slot owns what is assigned to it next, and the compiler restores the mark to what it was once the body has been compiled.

## A row owns its columns, a list its elements

A `Row` or `ListValue` with `ValuesOwned` set releases its columns or elements when it is disposed, which is the default and the usual case; a value built as a window onto a native structure something else holds - every `DataValue.FromNative`, `FromNativeRow` and `FromNativeList` - clears the flag instead, and the three `DataValue.Copy` overloads set it on the container they build over a native structure of their own. So a column or element read out of a container is copied before it is handed to anything that will own it, and a value put *into* a container is copied too, so that the caller keeps owning what it passed. `DataValue.CopyValue` and `DataValue.CopyNative` make the copy, `DataValue.DisposeValue` and `DataValue.DisposeNative` perform a release, and all four dispatch on what the value actually is - an `IDataValue`, a bare `StreamID`, or a native structure whose members are walked.

Two consequences for the code that fills a container. A node that evaluates a list, a row or a cursor row and hands it over - a list selector, `ToList`, the return expression of a `for each ... return` - releases what it produced once the container has copied it. And a `foreach` over a list *lends* the loop variable's slot an element the list still owns, so the slot is nulled rather than released on the way out and the list's own release is what frees the element; a loop variable the script declared itself is therefore nil once the loop has finished, as it has always been for a `foreach` over a cursor.

`TableValue` is the exception to the paragraph above and deliberately so: it has no disposal of its own, because a D4 table variable's rows live in the memory device and are released when that device drops the native table, and a `TableValue` built by `DataValue.Copy` therefore releases nothing when it is disposed. The `NOTE:` at the class records the measurement behind that and why closing the gap would be worse than leaving it.

## Where to look

| Concern | Code |
|---|---|
| Stack slot ownership, the two rules above | `src/engine/DAE/Runtime/Stack.cs` (`PokeOwned`, `ReleaseAbove`) |
| Borrowed slots | `src/engine/DAE/Compiling/Symbols.cs` (`IsBorrowed`) |
| The loan a foreach takes, and the loop that pays for it | `src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs` (`ForEachNode`), `ListNodes.cs` (`ListForEachNode`) |
| Copying and releasing a value of any shape | `src/engine/DAE/Runtime.Data/DataValue.cs` (`CopyValue`, `DisposeValue`) |
| Streams themselves | `src/engine/DAE/Streams/` |
