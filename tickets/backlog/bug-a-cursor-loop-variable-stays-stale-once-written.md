description: When a script walks the rows of a table and something inside the loop changes the loop variable, every later turn of the loop reads that changed value instead of the next row, silently and without an error. The same loop over a list behaves correctly.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: Writing to the variable a loop is walking is a programming error whatever the engine does with it, the behaviour is as old as the engine and an application could in principle be leaning on it, and the fix changes what a D4 script observes - so a maintainer may reasonably want the question decided rather than patched.
----
## What happens

A `foreach` over a cursor and a `foreach` over a list disagree about what their loop variable holds after something in the body has written to it. Over a list, the next turn of the loop puts the next element in the variable, so the write affects only the turn it happened on. Over a cursor, nothing puts the next row in the variable: the loop fills it once before it starts and then refills the same row in place, outside the variable. So a write to the variable survives, and every later turn of the loop reads what was written instead of the row it is on.

```
var LR : row { ID : Integer, B : Binary };
var LSeen : String := '';
var LCount : Integer := 0;
foreach LR in cursor(table { row { 1 ID, Binary('00F1') B }, row { 2 ID, Binary('00F2') B }, key { ID } }) do
begin
	LCount := LCount + 1;
	if LCount = 1 then
		LR := row { 9 ID, Binary('0009') B };
	LSeen := LSeen + LR.ID.ToString();
end;
// LSeen is '99'.  The loop ran twice and never saw row 2.
```

The same loop over a list of two elements, with the same write on the first turn, gives `'92'` - the write, then the second element.

No error is raised and nothing is left behind; the script simply processes the wrong row. That is the whole of the defect: a silent wrong result on a loop a reader would expect to walk every row.

**Measured, not inferred.** On `modernization` with `bug-a-var-parameter-written-in-the-body-can-release-a-callers-loop-value` landed, through a throwaway case on `InProcessTestFixture` (added, run, removed). The script above produced `'99'`; the list equivalent produced `'92'`. Both shapes behave the same way whether the write is a plain assignment or an operator writing back through a `var` parameter - which is how it was found, reviewing that ticket.

## Why

`ForEachNode.InternalExecute` has two branches, and only one of them refills the loop variable's slot.

The cursor branch creates one row, stores it in the slot **before** the iteration begins, and then has the cursor select into that row object on each turn. The row object in the slot and the row object being filled are the same one, so refilling it is enough - until something replaces what is in the slot, at which point the filling and the reading have come apart and nothing puts them back together.

The list branch stores the element into the slot **inside** the iteration, once per turn, so a write to the variable is displaced on the next turn the way a reader would expect.

The invariant the cursor branch is missing is the one the list branch has: *the loop variable's slot holds the loop's current row at the top of every turn.*

## What a fix has to settle

The mechanical change is small - the cursor branch storing its row into the slot on each turn rather than once - and it makes the two branches agree. Two things have to be decided with it:

- **It changes what a D4 script observes.** A script that writes to a cursor loop variable sees different rows afterwards. That is the point of the fix, but it is a behaviour change on a surface the project otherwise holds still, so it wants a decision rather than a patch. Weigh also the opposite reading: that writing to the variable a loop is walking should be refused by the compiler outright, which removes the shape instead of fixing it and is a D4 language change in its own right.
- **What the displaced value costs.** A store into a borrowed slot releases nothing (`AssignmentNode`'s borrowed branch, and `InstructionNodeBase.CleanupOperand`'s `var` write-back, both document why), so each turn's store leaks what it displaces. Refilling the slot per turn moves that leak from "the next store" to "the next turn" without changing its size, but the two accepted-tradeoff `NOTE:`s that describe it are written in terms of the current behaviour and would need rewording to match.

## What to pin

A case beside `AVarOperandGivenTheCallersLoopVariableLeavesTheLoopsValueAlone` in `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs`, asserting what the loop *saw* rather than what it leaked: the cursor script above and its list equivalent, both expected to walk every row. One case covers the plain assignment and the `var` write-back, since both are plain stores into the same slot.

That fixture is already past the roughly 1,500 lines its head `NOTE:` sets as the point to split it, so whoever takes this should expect to do the split first.

## Related

- `bug-a-loop-over-a-read-only-parameter-orphans-a-computed-argument` is the other open defect about what a `foreach` does to a slot it borrows. It is about the value the loop leaves behind *after* the loop; this one is about what the variable reads *during* it. Different sites, but anyone changing how a borrowed slot is handed back and forth should read both.
