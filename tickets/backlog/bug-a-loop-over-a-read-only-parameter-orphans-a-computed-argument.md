description: When an operator walks a list or a table using one of its own read-only parameters as the loop variable, and the caller passed a value it had just computed rather than a variable, the server keeps that picture or document for the rest of the session.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs
repro: verified
severity: edge-case
likelihood: contrived
tradeoffs: The shape is a programming error either way - a loop that overwrites the parameter it is walking discards the argument's value before reading it - so a maintainer may well decide that one stream per call of code nobody should write can wait; and the two candidate fixes both touch how a call and its operands agree about ownership, which is a design surface rather than a point change.
----
## What happens

A `foreach` can name, as its loop variable, a variable the enclosing block already declared - including one of the enclosing operator's own parameters. Walking a parameter is nonsense code (the loop overwrites it, so the argument's value is discarded unread), but it compiles and runs, and for a parameter declared `const` it leaves one managed stream behind per call when the argument was *computed* rather than read out of a variable:

```
create operator ZWalk(const AB : Binary) : Integer
begin
	var LCount : Integer := 0;
	foreach AB in { Binary('0001'), Binary('0002') } do
		LCount := LCount + 1;
	result := LCount;
end;

ZWalk(Binary('00FF'));            // one stream held for the session
var LB : Binary := Binary('00FF');
ZWalk(LB);                        // balanced - the caller's variable still owns its value
```

A `Binary` or a `Graphic` is what an application stores a picture, a document or an attachment in, and the engine keeps each one in a managed stream it will not reclaim until the session ends.

**Measured, not inferred.** On `modernization` with `bug-a-loop-variable-stays-borrowed-after-the-loop` landed, through a throwaway case on `InProcessTestFixture` reading `Diagnostics.StreamCount()` either side of each call (added, run, removed). `ZWalk(Binary('00FF'))` left one stream behind; `ZWalk(LB)` left none. The same two shapes were measured before that ticket landed and behaved identically, so this is older than it and not something it introduced. The same test run covered the two neighbouring modifiers, and both are balanced in both shapes: a by-value (`in`) parameter and a `var` parameter.

## Why the two halves disagree

Two rules meet at the slot the loop borrows, and each is right on its own.

A `foreach` leaves the slot it borrowed **nil**, on every path out. That is what makes the loop variable nil once the loop has finished, which is the documented behaviour and what a loop over a cursor has always done.

A call hands each operand slot's **current** value back to its own cleanup on the way out (`CallNode.InternalExecute`, the `finally` that fills `arguments`), and the cleanup then releases that value unless the plan node that produced the argument says it only lent it (`InstructionNodeBase.CleanupOperand`, the `const` branch). Handing back the slot's current value is what keeps an assignment in the body from being released twice: an assignment releases what it displaced itself, so the cleanup must see the replacement rather than the original.

Put together: the loop nulls the slot, the cleanup is handed nil, and the computed value the call owned is released by nobody. The loop cannot release it instead, because for a `const` operand that same value may belong to a caller's variable that lent it - releasing it there leaves the caller naming a released stream, which is what `AForEachOverAnOperandLeavesTheCallersValueAlone` in `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs` pins.

## The class behind it, which is the more interesting half

The defect is not really about `foreach`. It is that **a call's operand cleanup reads the slot's final value, so anything in a body that takes a value out of an operand slot without releasing it orphans that value.** A `foreach` is the only construct that does so today. Anything else that lends a slot later - and the lending model is deliberately general (`Symbol.IsBorrowed`) - would arrive at the same place. So the fix worth having is one that makes the hand-over explicit rather than one that special-cases loops, and the shapes to weigh are at least:

- The cleanup releases the value the call *started* with where the argument node did not lend, and the body's own releases are made to agree with that. This is the rule as the documentation states it; the hand-back exists because assignment broke it.
- A borrowed slot restores what it displaced rather than nilling it, so the cleanup sees the value it expects. This changes what a loop variable holds after the loop, which is D4-visible and recorded in `docs/migration-notes.md`, so it needs a decision rather than a patch.
- The compiler refuses `foreach` over a `const` parameter outright, which removes this shape rather than fixing the class - and is a D4 language change, so it would have to be routed as one.

`fix/bug-a-var-parameter-written-in-the-body-can-release-a-callers-loop-value` is the other defect at this seam and points the other way: there the callee releases a value the caller's loop still owns, because a `var` parameter's slot is an alias of the caller's and nothing tells the callee that the caller's slot was borrowed. Neither fix is the other's, but both turn on how a call and its operands agree about ownership.

Whoever takes this should read [architecture/operand-ownership.md](../../docs/architecture/operand-ownership.md), which is where the operand half of the rule is written down, and the `NOTE:` at `CallNode.ReferencesAnEarlierOperandsSlot`, which records a second, unrelated leak in the same hand-back whose fix needs the same decision.

## What to pin

A case beside `AForEachOverAnOperandLeavesTheCallersValueAlone`, which already covers the three modifiers with a *lent* argument: the same three with a **computed** argument, asserting the stream count is back where it started. Two of the three pass today (`in` and `var`); the `const` one is this defect.
