description: When something goes wrong inside a comparison of two rows or two tables, the server throws away the real error and reports an unhelpful internal message instead, so nobody can tell what actually failed.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime/Program.cs, src/engine/DAE/Runtime/Stack.cs, src/engine/DAE/Runtime/RuntimeStackWindow.cs, src/engine/DAE/Runtime.Instructions/RowComparisonNodes.cs, src/engine/DAE/Runtime.Instructions/TableComparisonNodes.cs
repro: verified
severity: edge-case
likelihood: normal-use
tradeoffs: Nothing computes a wrong answer and no data is at risk - this only costs whoever is diagnosing a failure, and they can get the real error back by attaching a debugger or by patching the engine locally, so a maintainer may reasonably rank it below anything that affects results.
----

## What happens

Every error the engine reports carries a source location, and the location is built from the locator of the stack window that is current when the error is raised. A handful of nodes push a stack window with **no** locator, and `Program.GetLocation` dereferences it without checking, so building the location raises a `NullReferenceException` of its own. The catch around it replaces the real error with

> `Could not determine the current location for program <guid>.`

and the original message - and its stack - is gone from everything a user, a test run or a CI log can see. Only the innermost `NullReferenceException: Object reference not set to an instance of an object.` survives, attributed to the wrong place.

This was met while diagnosing `bug-row-equality-change-breaks-implicitconversions`, where the real error was a perfectly clear "A nil was encountered where a value was expected" from a column read. Recovering it needed a local engine patch; the published failure said nothing useful at all. That cost is paid again by whoever hits the next error inside one of these constructs.

## Where

`Stack.PushWindow(int count)` (`src/engine/DAE/Runtime/Stack.cs`) builds a `RuntimeStackWindow` through the constructor that leaves `Locator` null; the sibling `PushWindow(int, PlanNode, DebugLocator)` is the one that sets it. `Program.GetLocation` (`src/engine/DAE/Runtime/Program.cs`) then passes that null straight into the `DebugLocator(DebugLocator, int, int)` constructor, which reads `locator.Locator` on the first line.

The run-time call sites that take the no-locator overload, and so are the constructs affected:

- `RowEqualNode.InternalCompare` and `CompoundScalarEqualNode.InternalExecute` - `=` and `<>` between two rows, and the equality the compiler generates for a scalar type whose single representation has more than one property (`src/engine/DAE/Runtime.Instructions/RowComparisonNodes.cs`).
- The table comparison nodes - `=`, `<>` and the four inclusion comparisons between two tables (`src/engine/DAE/Runtime.Instructions/TableComparisonNodes.cs`).
- `ServerTransaction`'s key-equality program (`src/engine/DAE/Server/ServerTransaction.cs`).

Row and table equality are not obscure: a transition constraint, a key comparison and `in` over a row-valued list all compile to them, so any error raised inside one of those comparisons - a conversion that overflows, a constraint that fails, a value that was released - reports the useless message rather than its own.

## Expected behaviour

An error raised while a window with no locator is current should report its own message and its own location as well as the engine can place it. Losing precision in the reported line is acceptable; losing the error is not. Whether the right answer is for `GetLocation` to tolerate a null locator, for the no-locator windows to inherit the enclosing window's locator, or for those call sites to pass one, is the design question this ticket is for - note that the enclosing window's locator is the one a user would expect to see, since these windows are an implementation detail of a single operator and not a call into D4 code with a source of its own.

Whatever the shape, `Program.GetLocation`'s `catch` must stop being able to swallow the exception it was called about. Today it converts *any* failure to place an error into a `ServerException` that replaces that error.

## How to reproduce

Any runtime error inside a row comparison. The one used to find it, against the engine as of `e321e388`:

```d4
begin
	var L : row { ID : Long } := row { 5 ID };
	var R : row { ID : Byte } := row { ToByte(5) ID };
	if L <> R then
		raise Error("unequal");
	select L.ID;   -- the real error here is reported as "Could not determine the current location"
end;
```

That particular error is a bug in its own right and is being fixed separately; once it is fixed this script no longer raises, so a reproduction for *this* ticket needs another error inside a comparison - for example a scalar type constraint that fails on a value produced inside the generated comparison.
