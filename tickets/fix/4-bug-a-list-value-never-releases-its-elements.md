description: A list of pictures or documents never gives back the memory its items use. Building one, or just evaluating one and throwing it away, costs memory for the rest of the session - one item's worth per item - and a loop over such a list costs a second copy on top.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/ListValue.cs, src/engine/DAE/Runtime.Data/RowValue.cs, src/engine/DAE/Runtime.Data/TableValue.cs, src/engine/DAE/Runtime.Data/DataValue.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs
repro: verified
difficulty: medium
----
## What is wrong

The engine keeps values too large to carry inline - the D4 types `Binary` and `Graphic`, and rows, lists and tables - in a stream the server owns, and something has to hand the stream back. A structured value that holds other values hands back what it holds when it is itself released, and `DataValue` has a flag that says whether it should: `ValuesOwned` (`src/engine/DAE/Runtime.Data/DataValue.cs`). `Row` honours it - `Row.Dispose(bool)` clears its column values when `ValuesOwned` is set. **`ListValue` has no `Dispose(bool)` override at all**, so releasing a list releases none of its elements, even though the engine sets `ValuesOwned` on the lists it builds (`DataValue.cs`, four sites).

So every list of a stream-backed element type costs one stream for the life of the session, per element.

### Arm 1 - a list releases none of its elements

The site is `src/engine/DAE/Runtime.Data/ListValue.cs`. The machinery is already there and already correct: `DisposeValueAt(int)` releases one element and `Clear()` walks them. Nothing calls either when the value is disposed.

Measured with `Diagnostics.StreamCount()` on an in-process server, before -> after each script (`tests/DAE.Server.Tests/InProcessTestFixture`, Debug, head `3b5ec8db` plus this review's fix):

| script | streams left behind |
|---|---|
| `begin var LL : list(Binary) := { Binary('0001'), Binary('0002') }; end;` | 2 |
| `begin var LL : list(Binary) := { Binary('0001') }; end;` | 1 |
| `begin var LI : Integer := ({ Binary('0001'), Binary('0002') }).Count(); end;` | 2 |
| `begin var LB : Binary := Binary('0001'); end;` (control) | 0 |

One per element, and a loop is not needed to provoke it: the third script never declares a list variable at all.

### Arm 2 - a `foreach` over a list releases neither the list it evaluated nor the slot it lent

`ForEachNode.InternalExecute`'s list branch (`src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs`) evaluates the list expression and never releases the result, and - unlike the cursor branch beside it, which nulls the slot it lends - it leaves the loop variable's slot holding the last element, a value the list owns. A `NOTE:` at the site records this.

| script | streams left behind |
|---|---|
| `begin var LL : list(Binary) := { Binary('0001'), Binary('0002') }; var LB : Binary; foreach LB in LL do begin end; end;` | 3 |
| `begin var LL : list(Binary) := { Binary('0001'), Binary('0002') }; foreach var LB in LL do begin end; end;` | 4 |
| `begin foreach var LB in { Binary('0001'), Binary('0002') } do begin end; end;` | 2 |

Reading a variable that is not passed by reference copies its value (`StackReferenceNode.InternalExecute`), so a loop over a list *variable* evaluates a second, complete copy of the list - hence 2 for the list plus 2 for the copy, less whatever the loop variable's own slot happens to release. A loop over a list *literal* adds nothing over arm 1 on its own.

## The two arms have to land together

Today the element in the lent slot is released exactly once in total, by the scope that declared the loop variable - because neither the list nor the loop releases it. Fix either arm alone and that element is released **twice**, which the stream manager reports as an error rather than a leak. So one change: make a list release its elements, make `foreach` release the list it evaluated, and null the slot `foreach` lent, in the same commit.

## The class behind it, which is the more valuable half

`ValuesOwned` is a contract declared on `DataValue` and honoured by some of its subclasses and not others. Among the value types, `Row` and `Scalar` override `Dispose(bool)`; `ListValue` does not, and neither does `TableValue` (`src/engine/DAE/Runtime.Data/TableValue.cs`, the non-abstract one) - its `AsNative` setter drops the native table but its disposal does not. Nothing in the build or the tests checks the contract, so the next subclass can omit it just as quietly.

What this ticket should aim at is the check, not only the two instances: one test that takes each `DataValue` subclass that can hold other values, builds one holding stream-backed values, releases it and asserts the server's stream count is back where it started. That catches arm 1, catches whatever `TableValue` turns out to do, and catches the next one. The point fixes are then evidence rather than the deliverable.

## What finishing this looks like

- Every script in both tables above leaves `Diagnostics.StreamCount()` where it found it, and none of them errors.
- A test that pins the `ValuesOwned` contract across the `DataValue` subclasses, as above, rather than one case per script shape. `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs` is the fixture that owns value ownership and already carries the stream-count helpers; a contract test over the subclasses may belong beside it or in its own fixture.
- `TableValue`'s disposal is settled one way or the other - either it releases what it holds, or a `NOTE:` at the site says who does instead.
- The `NOTE:` at `Program.Stop` (`src/engine/DAE/Runtime/Program.cs`) names this ticket as one of the two leaks blocking a `DEBUG` stream-count check there. That check is the boundary invariant for this whole class of defect; with this ticket and `bug-an-operator-argument-is-released-by-the-wrong-owner` landed, it can go in.
- `docs/migration-notes.md` gets an entry if a public member changes; releasing what a list holds is a behaviour change a custom C# extension that reads a list after disposing it could notice.

## Relationship to the other open ownership tickets

`bug-an-operator-argument-is-released-by-the-wrong-owner` (`tickets/fix/`) is about who owns an *argument*; this one is about who owns the *elements of a structured value*. They touch different sites and neither blocks the other. That ticket owns writing the `docs/architecture/` page on value lifetime - there is no such page today - so this ticket should not start one.

## How this was found

Reviewing `bug-an-error-inside-a-block-leaks-its-binary-values`, whose implement stage measured five `foreach`-over-a-list shapes and recorded them as a `foreach` leak. Re-measuring with the loop removed showed the same counts, which is what pointed at `ListValue`.
