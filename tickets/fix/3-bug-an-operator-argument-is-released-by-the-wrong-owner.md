description: Passing a picture or document value straight into a routine - as a freshly built value rather than a variable - leaves it held in memory for the life of the session, and passing one that lives inside a record can destroy the record's copy of it; both come from the engine deciding who cleans up an argument from the wrong piece of information.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/InstructionNode.cs, src/engine/DAE/Runtime.Instructions/ExtractionNodes.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Compiling/Compiler.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs
repro: verified
difficulty: hard
----
## One decision, made from the wrong information

When a call finishes, `InstructionNodeBase.CleanupOperand` (`src/engine/DAE/Runtime.Instructions/InstructionNode.cs`) decides what to do with each argument's value, and it decides from the **operand's modifier** alone:

- `in` - release the value.
- `var` - write it back to the caller's slot, release nothing.
- `const` - do nothing at all.

But whether the call owns an argument's value is not a property of the modifier. It is a property of **the expression that produced the argument**. A reference to a variable hands over the variable's own value when the compiler marked it by-reference (`StackReferenceNode.ByReference`, set for `var` and `const` operands in `InstructionNodeBase.DetermineDataType`) and a *copy* otherwise; anything computed for the call - a literal, a conversion, another call - produces a value nothing else holds. The two pieces of information are related but not the same, and where they disagree the value is either released twice or never released at all.

Two shapes disagree today. Both are verified, and both are older than the assignment fix that met them (see *How this was found*).

### Arm 1 - a `const` operand's computed value is never released

A `const` operand given anything but a variable reference leaks one value per call, because nothing in `CleanupOperand` releases it and nothing else holds it.

```
create operator F(const AB : Binary) : Integer begin result := Length(AB.Value); end;
F(Binary('0001'));              -- Diagnostics.StreamCount() stays one higher, for the life of the session
```

This reaches far more code than the word `const` suggests, for two reasons. The engine **promotes** a by-value operand to `const` whenever the operator's body never assigns to it, so most ordinary declarations end up `const` anyway. And a great many declarations say `const` outright - every comparison, conversion and accessor the engine generates for a scalar type, and most of the `Frontend` library (`Image(...) : Graphic`, `SaveDocument(..., const ADocument : Scalar)`, and so on).

### Arm 2 - a by-value operand given a record's column releases a value the record owns

Reading a column out of a row variable hands over the row's own value rather than a copy: `ExtractColumnNode` sets `_shouldDisposeSource = false`, marks its source by-reference, and returns `row[columnIndex]`. Released as a by-value operand's value, the row variable is left pointing at a value that is gone:

```
create operator G(AB : Binary) begin AB := Binary('0002'); end;   -- assigns to it, so the operand stays by-value
var LR : row { B : Binary } := row { Binary('0001') B };
G(LR.B);
LR.B.Value;                     -- Runtime error: StreamID (n) not found
```

That is data loss on a plain read of a record's field, not only a leak.

## What makes this hard, and why the obvious fix is wrong

The obvious repair - release a `const` operand's value unless the argument node handed it over by reference - was written and measured during the review of `bug-assigning-a-binary-variable-leaks-its-stream`. It closes arm 1 completely (every leak case above goes to zero) **and it breaks the engine**: a row variable built from `row { Binary('0001') B }` then loses its column on the next read, with `StreamID (n) not found`.

The reason is a third group that neither the modifier nor the by-reference mark distinguishes: some `const` operands belong to an operator that **takes the value over**. A scalar type's selector (`Compiler.CompileScalarSelector`, which declares every one of its operands `const`) stores its arguments into the value it returns, and so does every other constructor-shaped generated operator. For those, releasing the argument destroys part of the result.

So there are three kinds of argument, and only two bits of information available to tell them apart:

| the argument | who owns its value after the call |
|---|---|
| was handed over by reference | the caller's slot |
| was computed for this call, and the operator only reads it | the call |
| was computed for this call, and the operator stores it in what it returns | the result |

The engine can express only the first against the other two. Making the third expressible is the work: an ownership fact carried by the argument, or declared by the operand, rather than inferred from `Modifier`. Naming that representation is this ticket's research, and it should aim to make the bad state unrepresentable rather than add a third special case at one site - the same decision is read in at least four places (`CleanupOperand`, `CallNode`'s operand push and its write-back, `AssignmentNode`'s borrowed-slot test, and `ExtractColumnNode._shouldDisposeSource`), and each of them infers it differently today.

## How this was found

Found reviewing `bug-assigning-a-binary-variable-leaks-its-stream`, which made every stack slot release the value it replaces and so turned both arms into visible failures. Each arm was then measured against the commit before that change (`6e4d5ee4`), in a worktree:

- Arm 1 with a **procedure** - a by-value `Binary` operand, called with `Binary('0001')`: leaked one stream before the change and after it. Pre-existing.
- Arm 1 with a **function** - an operator with a result, same shape: did **not** leak before the change and does now. That change corrected which symbol the by-value-to-`const` promotion reads; it had been reading the wrong one for every operator with a result, which could hand a body the caller's own value to release - a hard error. Correcting it promotes one more operand per function, and those operands then meet the pre-existing arm 1. Measured by reverting that one line on the current head and watching the case go from failing to passing.
- Arm 2: identical failure before the change and after. Pre-existing.

## What finishing this looks like

- Both scripts above leave `Diagnostics.StreamCount()` where they found it, and the second reads `'0001'` back out of the row.
- `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs` is the fixture that owns this ground; its remarks already describe the ownership rule and the exceptions to it. Each arm's reproduction belongs there.
- A scalar type's selector, its accessors and its comparisons keep working. The coverage-script suite is the check that matters, because every D4 script in it calls generated `const` operators constantly; the obvious-but-wrong fix above failed on the very first row-valued test.
- `docs/architecture/` has no page that owns value lifetime - nothing under it mentions the stack, streams, or who releases a value, and `sessions-processes-plans.md` is within fifty words of the 1,500-word cap. Once the ownership rule is complete and correct it is worth a page of its own: who owns a value in a stack slot, in an operand, in a row, and what releases each. That is better written with this ticket's answer in hand than before it.
- `docs/migration-notes.md` gets an entry: a custom C# extension node with `const` operands may be relying on nothing releasing its arguments.
