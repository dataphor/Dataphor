description: A list of pictures or documents now gives back the memory its items use when the list goes out of scope, and an item added to such a list can be read back afterwards instead of failing. Needs a review pass over the ownership rules the change rests on.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/ListValue.cs, src/engine/DAE/Runtime.Data/TableValue.cs, src/engine/DAE/Runtime.Data/NativeSet.cs, src/engine/DAE/Runtime.Instructions/ListNodes.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime/Program.cs, src/engine/Server/NativeCLI/NativeMarshal.cs, tests/DAE.Server.Tests/ContainerValueOwnershipTest.cs, tests/DAE.Server.Tests/coverage-manifest.json, tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4, docs/architecture/value-lifetime.md, docs/migration-notes.md
repro: verified
difficulty: hard
----
## What the implementation did

One rule, applied at every site: **a list owns its elements, copies what it is given, and releases what it holds.** `ListValue` had no `Dispose(bool)` at all, so every element of every list of `Binary` or `Graphic` was held for the life of the session, and it took a plain native element as it stood while the call that handed it over released it - so `Add` and `Insert` left the list pointing at a released stream.

**`src/engine/DAE/Runtime.Data/ListValue.cs`** - mirrors `Row`: a `Dispose(bool)` that clears the elements when `ValuesOwned`; `ValuesOwned` set explicitly in all three constructors (`false` for the one taking a `NativeList`, which is the window every `DataValue.FromNative*` builds, `true` for the other two); `Add`, `Insert` and `this[int] = value` copy a plain native element with `DataValue.CopyNative`, as `Row`'s column setter does.

**`src/engine/Server/NativeCLI/NativeMarshal.cs`** - `NativeValueToDataValue`'s list branch sets `ValuesOwned = true` (it owns the `NativeList` it passes; the constructor is used only to get nil-ness from a null `Elements`) and disposes each element value the list has copied.

**`src/engine/DAE/Runtime.Instructions/ListNodes.cs`** - the callers that hand a value to a list now release it after the add: the list selector (`{ a, b }`, guarded on `ReturnsByReference`, which nothing sets for a selector's elements today), both branches of `ToList`, and the return expression of `for each ... return` (whose many-valued branch drops its now-redundant `CopyValue`). The selector also disposes the partial list if a later element node raises.

**The three `foreach` list branches** - `ForEachNode` (`ImperativeNodes.cs`), `ListForEachNode` and `TableForEachNode` (`ListNodes.cs`) - each null the loop variable's slot in the `finally` that pops it, as the cursor branch beside each has always done, and each release the list they evaluated on every exit path. The superseded `NOTE:` in `ForEachNode` is gone. **The D4-visible consequence**: `var LB : Binary; foreach LB in LL do ...` leaves `LB` nil, where it used to hold the last element. Only that form can observe it; `foreach var LB in ...` puts the variable out of scope at the loop.

**Notes rather than code.** `TableValue` carries a `NOTE:` saying why it deliberately has no `Dispose(bool)` (its rows belong to the memory device, measured as leaking nothing) and that `DataValue.Copy*` builds one whose disposal does nothing. The `Program.Stop` `NOTE:` now names the three leaks that still block the `DEBUG` stream-count check: `bug-an-argument-is-orphaned-when-a-later-one-raises`, `bug-a-stored-binary-is-never-given-back`, `bug-a-loop-variable-stays-borrowed-after-the-loop`.

**Documentation.** The container section of `docs/architecture/value-lifetime.md` is rewritten to what is now true (1,470 words of the 1,500 limit; `python tools/docs-size-check/check_size.py` reports 0 over). `docs/migration-notes.md` has two entries, one in the D4 section and one under custom C# extensions.

## Two defects met along the way, both at `Distinct`

`DistinctListNode` builds a `NativeSet` to find the duplicates with, and that set **releases every element it holds when it is dropped**. It was given the source list's own elements, so:

- **Fixed here, because this ticket would otherwise have shipped a regression.** Once a list releases its elements, the drop and the list's own release were two releases of one stream: `LL.Distinct()` over a `list(Binary)` raised `StreamID (n) not found` (reproduced, then fixed by handing the set `DataValue.CopyValue` of each element). Before this ticket the same code produced a result whose elements were released streams - a silent wrong answer rather than an error, which is why nothing caught it.
- **Filed, not fixed: `bug-distinct-leaves-duplicate-pictures-in-a-list`** (`tickets/backlog/`). `Distinct` does not in fact remove duplicates from a list of `Binary`, `Graphic`, rows or lists: the set's comparer hashes with `obj.GetHashCode()`, which for a `StreamID` or a native structure is identity rather than contents, so two equal values never meet to be compared. Measured: `{ Binary(a), Binary(b), Binary(a) }.Distinct()` returns 3 elements. The fix needs a decision about how to hash a value the engine does not keep inline, so it is a ticket rather than an edit; a `NOTE:` at `NativeSet`'s `GetHashCode` records the measurement and names it.

## Tests added

- **`tests/DAE.Server.Tests/ContainerValueOwnershipTest.cs`** (new, 5 cases, added to the csproj's `Compile` list). The `ValuesOwned` contract over the value classes directly, which is where it lives and where nothing checked it before: a `Row`, a `ListValue`, a list of rows and a row with a list column each release every stream-backed value they hold when disposed, and - the other half of the flag - a container built by `DataValue.FromNative` over a native structure it does not own reports `ValuesOwned` false and releases nothing when disposed. Exact stream counts from `Diagnostics.StreamCount()`, two values per container so that releasing only the first is not a pass.
- **A list section in `tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4`** (14 blocks, +271 lines): the measured shapes (a list in a variable, one element, a list evaluated and discarded, the three forms of `foreach`, a row with a list column, a list of rows, a list of lists, an element read into a variable, assignment over a list variable, a list returned from an operator), the `Add` and `Insert` read-back - which assert the **value**, the thing that failed - the loop variable being nil after a list `foreach` (through `IsNil`, since a comparison with nil yields nil), and the `Distinct` block above. Its manifest entry stays `pass`; its `note` now says the script covers lists.

Both were checked against the defect rather than only against the fix. With `ListValue.Dispose` not clearing, the D4 section fails with "A list of two Binary values left 2 stream(s) behind"; with `Add` not copying, it fails with `StreamID (n) not found`. Nothing else got a test: the constructors, the `ValuesOwned` assignments and the `ReturnsByReference` guards are reached through their real callers by these two.

## What was run

`dotnet build Dataphor.slnx` (0 errors), then, as the targeted implement-stage set:

- `--filter "FullyQualifiedName~OwnershipTest"` - 26 passed (the new fixture, `AssignmentValueOwnershipTest`, `BufferedOperatorColumnOwnershipTest`).
- `--filter "Name=ListLibrary|Name=StreamValues|Name=RowLibrary|Name=TableLibrary|Name=LanguageConstructs|Name=StringLibrary|Name=Tables|Name=Operators|Name=Functions|Name=NilLibrary|Name=RowAllocation|Name=ScalarAllocation|Name=Aggregates|Name=Cursors|Name=CursorLibrary|Name=Derivation|Name=LibraryFilesCoverage|Name=SystemCatalog|Name=Internet|Name=Explode|Name=Patterns|Name=ServerLink"` - 22 coverage scripts passed, 1 m 22 s. `ServerLink` was added to the set the ticket named: it is the one script that marshals a list over the native call interface (`EvaluateOn` with list parameters, and a list returned over a server link), which is the only coverage `NativeMarshal`'s list branch has.
- `--filter "FullyQualifiedName~NativeSession|FullyQualifiedName~RemoteStreamValue|FullyQualifiedName~TableAssignment|FullyQualifiedName~RemoteValidate|FullyQualifiedName~CoreWcfProtocol|FullyQualifiedName~ClientStack"` - 26 passed.
- `python tools/docs-size-check/check_size.py` - 12 files, 0 over the limit. The link checker's 203 errors are the pre-existing documentation-conversion stubs; none are in the two pages touched.

The full suite was not run locally; CI runs it in Debug and Release on the push.

## For the reviewer

Known gaps and judgement calls, in the order they are worth looking at:

- **`ListValue.DisposeValueAt` does not check `ValuesOwned`**, so `Clear()`, `RemoveAt` and the element setter release unconditionally. Disposal is still right (`Dispose` checks the flag before calling `Clear`), and no D4 path reaches `Clear` or `RemoveAt` on a window: `Clear(var AList)` and `RemoveAt(var AList, ...)` take a `var` operand, which only a stack reference can satisfy, and a variable's slot owns its value. `Row` has the same asymmetry in the other direction (its setter releases unconditionally, `ClearValue`/`ClearValues` check the flag), which is why the implementation left it alone rather than guessing. Worth deciding whether the guard or a `NOTE:` belongs there.
- **The row and table branches of `NativeMarshal.NativeValueToDataValue` have the shape the list branch had**: they hand a freshly built `IDataValue` straight into `row[index] =`, which copies, and never dispose it. Not changed, and the reason is that a value arriving over the native call interface is never stream-backed - a `Binary` comes over as a byte array, so `new Scalar(manager, type, bytes)` is native - which makes the discarded intermediate plain managed memory the garbage collector reclaims rather than a held stream. A reviewer may still want the symmetry.
- **`ToList` over a `Dynamic` (JSON) value now deep-clones each element**, because `JToken` implements `ICloneable` and `DataValue.CopyNative` clones an `ICloneable` native value; it used to put the `JArray`'s own tokens in the list. Correct by the ownership rule and cheap at JSON sizes, but `ToList(Dynamic)` has no test at all - the `Internet` coverage script does not call it - so this is reasoning, not a measurement.
- **`DistinctListNode` takes one extra copy per distinct element** (the set's own). For a `Binary` that is a stream reference and a deallocation per distinct element.
- **The list selector's new `catch { list.Dispose(); throw; }`** is an error path nothing measures: the ticket's numbers were all for scripts that ran to completion. Without it, an element node that raises leaves the elements already copied in a list nothing releases.
- **Deliberately still open, as the ticket scoped them**: a stream-backed value written into a table is not given back until the table is dropped (`bug-a-stored-binary-is-never-given-back`, whose numbers this change improves without finishing), `TableValue` has no disposal (settled with the `NOTE:`), and the compiler leaves a loop variable marked borrowed after its loop (`bug-a-loop-variable-stays-borrowed-after-the-loop`, which depends on this landing).

## Use cases to validate

- Build a list of pictures in a loop and throw it away repeatedly; the server's stream count must come back to where it started (`Diagnostics.StreamCount()` is the instrument, and `StreamValues.d4` is the worked example).
- `LL.Add(Binary(...))` then `LL[1].Value` - must read back the value added, where it raised `StreamID (n) not found`. Same for `Insert`.
- `var LB : Binary; foreach LB in LL do ...` then `IsNil(LB)` - now true. This is the one behaviour an existing script could notice; a script that wanted the last element must copy it inside the loop.
- A list arriving over the native call interface or a server link (`EvaluateOn('Test', 'select (list(Integer) { 1, 2 })')`) must still marshal and must not hold streams.
- `LL.Distinct()` over a list of pictures must return readable elements and leave no streams behind - it must **not** be expected to remove the duplicate; that is the filed ticket.
