# Operand ownership

Who owns the value handed to a call's operand, and how the caller and the node that produced the argument agree about which of them releases it. The other half of [value lifetime](value-lifetime.md), which covers a value sitting in a stack slot, a row or a list. Part of the [Dataphor architecture](../architecture.md).

## An operand's owner depends on its modifier and on its argument

A call's arguments are evaluated by the caller, pushed into the callee's slots by `CallNode`, then cleaned up by `InstructionNodeBase.CleanupOperand`, which reads the operand's modifier:

- `var` - the callee's slot is an alias of a caller's slot. Nothing is copied and nothing is released; the cleanup writes the slot's final value back with a plain `Poke`, because the caller's slot already owned it. That write-back runs on every exit path, including a `raise`, so the caller sees the callee's last assignment either way. The cleanup releases nothing, but the body can, and does: an assignment to a `var` parameter - or a `foreach` over it - releases what the slot held, and the write-back then hands the caller the replacement. That is right only while the caller's slot owns the value it passed, and a callee cannot tell: where the caller passed its own loop variable, the release takes a row or element the caller's loop still owns, which is `bug-a-var-parameter-written-in-the-body-can-release-a-callers-loop-value`.
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

## Where to look

| Concern | Code |
|---|---|
| Operand cleanup, by modifier | `src/engine/DAE/Runtime.Instructions/InstructionNode.cs` (`CleanupOperand`) |
| The lending question | `src/engine/DAE/Runtime.Instructions/PlanNode.cs` (`SetByReference`, `ReturnsByReference`) |
| Operand slots and the result slot of a call | `src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs` (`CallNode`) |
