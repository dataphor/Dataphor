description: A list of pictures or documents never gives back the memory its items use, so building one - or even evaluating one and throwing it away - costs memory for the rest of the session. Adding such a value to a list also leaves it unreadable afterwards, because the memory is handed back while the list is still pointing at it.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/ListValue.cs, src/engine/DAE/Runtime.Instructions/ListNodes.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/Server/NativeCLI/NativeMarshal.cs, src/engine/DAE/Runtime/Program.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4, docs/architecture/value-lifetime.md, docs/migration-notes.md
repro: verified
difficulty: hard
----
## The rule this breaks

The engine keeps a value too large to carry inline - a `Binary` or a `Graphic`, and rows, lists and tables - in a *stream*, a block of memory the server owns and hands back only when someone releases it. [docs/architecture/value-lifetime.md](../../docs/architecture/value-lifetime.md) states the rule in two halves, and a list breaks both:

- **A container releases what it holds.** A `DataValue` with `ValuesOwned` set releases its columns or elements when it is disposed. `Row` honours that (`Row.Dispose(bool)` clears its column values); **`ListValue` has no `Dispose(bool)` at all**, so releasing a list releases none of its elements. The machinery is already there and already correct - `DisposeValueAt(int)` releases one element and `Clear()` walks them - and nothing calls either on disposal.
- **A container copies what it is given, and the caller keeps owning its own value.** `Row` copies both halves: a `DataValue` through `CopyNative()` and a plain native value through `DataValue.CopyNative(...)`. `ListValue.Add`, `Insert` and `this[int] = value` copy a `DataValue` but **take a plain native value as-is**, so the two kinds of element have different owners. One caller reads it each way, and so one of the two is wrong whichever way you read it: the list selector (`{ a, b }`) hands its element values over and releases nothing, while the D4 operator `Add(var AList, AValue)` releases its argument after the call - and that release takes the stream the list is now pointing at.

Three groups of sites, below, and a fourth consequence that is a *second* release rather than a missing one: because neither the list nor the `foreach` releases the element in the loop variable's slot today, the scope that declared the variable releases it exactly once. Close either of those alone and that element is released twice, which the stream manager reports as `StreamID (n) not found` rather than as a leak. **So this all lands in one commit.**

## Measured

Every number below was measured on `modernization` at `6518e4a6`, on an in-process server, reading `Diagnostics.StreamCount()` (how many streams the server is holding) before and after each script through a throwaway fixture on `InProcessTestFixture` - added, run and removed again; it is not in the tree. "after" is the same measurement with the correction prototyped (the prototype is described under *The correction*; it was built and run, not sketched).

A script that declares and releases its own variables must leave the count where it found it, so any non-zero number is a leak.

| script | before | after |
|---|---:|---:|
| `begin var LB : Binary := Binary('0001'); end;` (control) | 0 | 0 |
| `begin var LL : list(Binary) := { Binary('0001'), Binary('0002') }; end;` | 2 | 0 |
| `begin var LL : list(Binary) := { Binary('0001') }; end;` | 1 | 0 |
| `begin var LI : Integer := ({ Binary('0001'), Binary('0002') }).Count(); end;` | 2 | 0 |
| `begin var LL : list(Binary) := { ... two ... }; var LB : Binary; foreach LB in LL do begin end; end;` | 3 | 0 |
| `begin var LL : list(Binary) := { ... two ... }; foreach var LB in LL do begin end; end;` | 4 | 0 |
| `begin foreach var LB in { Binary('0001'), Binary('0002') } do begin end; end;` | 2 | 0 |
| `begin var LR : row { L : list(Binary) } := row { { ... two ... } L }; end;` | 4 | 0 |
| `begin var LL : list(row { B : Binary }) := { row { Binary('0001') B }, row { Binary('0002') B } }; end;` | 4 | 0 |
| `begin var LL : list(list(Binary)) := { { Binary('0001') }, { Binary('0002') } }; end;` | 4 | 0 |
| `begin var LL : list(Binary) := { ... two ... }; var LB : Binary := LL[0]; end;` | 2 | 0 |
| `begin var LL : list(Binary) := { Binary('0001') }; LL := { Binary('0002') }; end;` | 2 | 0 |
| `begin var LL : list(Binary) := ZMakeList(); end;` (an operator returning a list) | 2 | 0 |

One per element, in every shape - and no loop is needed to provoke it: the third script never declares a list variable at all. A list of `Integer` costs nothing, as expected, and a `row { B : Binary }` on its own costs nothing, which is what a container that does release its contents looks like.

The loop numbers are higher than the list's own because reading a variable that was not asked to lend copies it (`StackReferenceNode.InternalExecute`), so a loop over a list *variable* evaluates a second, complete copy of the list that nothing releases either.

### The same site, as a wrong answer rather than a leak

```
begin var LL : list(Binary) := { Binary('0001') }; LL.Add(Binary('0002')); var LS : String := LL[1].Value; end;
```

fails today with `Runtime error: StreamID (2) not found.`, and the same script with `Insert` fails the same way: the call releases the value it was given for `AValue` (an `in` operand, released unconditionally) while the list is still pointing at the stream. `LL.Count()` afterwards says 2, so the element is in the list - it just cannot be read. With the correction both scripts read the value back and leave the count where they found it. `git log -S` puts both the unconditional `in` release and `ListValue.Add`'s missing copy at the repository move, so this is original behaviour and not a regression from the recent ownership work. A list of `Integer` is unaffected (nothing to release), which is why no existing test catches it: no coverage script puts a `Binary` in a list.

## The correction

One rule, applied at every site: **a list owns its elements, copies what it is given, and releases what it holds.** The prototype that produced the "after" column is four files and about 90 lines; it is reproduced where the shape is not obvious, but write it fresh rather than transcribing it.

**`ListValue` (`src/engine/DAE/Runtime.Data/ListValue.cs`), mirroring `Row`:**

- `Dispose(bool)`: `if (_list != null) { if (ValuesOwned) Clear(); _list = null; } base.Dispose(disposing);`
- The constructor that takes a `NativeList` sets `ValuesOwned = false` (it is a window onto a list something else holds - this is what `Row(manager, rowType, nativeRow)` does, and it is what keeps a `ListValue` built by `FromNative`, `FromNativeRow` or `FromNativeList` from releasing another owner's elements). The other two constructors set `ValuesOwned = true` explicitly, as `Row(manager, rowType)` does.
- `Add`, `Insert` and `this[int] = value` copy a plain native element: `DataValue.CopyNative(Manager, DataType.ElementType, value)`, the same call `Row`'s column setter makes.

Three places already compensate for the missing flag by setting `ValuesOwned = true` on a list built over a `NativeList` they own - `DataValue.CopyAs`, `Copy()` and `Copy(manager)` - and those stay as they are; they are correct and they are why a copied list is released. `NativeMarshal.NativeValueToDataValue` (`src/engine/Server/NativeCLI/NativeMarshal.cs`) uses the same constructor for a list it *does* own, only to get nil-ness from a null `NativeList`, so it needs `ValuesOwned = true` too - otherwise a list arriving over the native call interface stops releasing its elements.

**The callers that hand a value to a list must release it afterwards** (they already do not release it, which is why a list of rows leaks one stream per element over and above the elements themselves):

- `ListSelectorNode.InternalExecute` (`ListNodes.cs`, the `{ a, b }` literal) - release each element value after adding it, unless the node lent it (`if (!Nodes[index].ReturnsByReference)`, the idiom `InstructionNodeBase.CleanupOperand`'s const branch uses). Nothing asks a list selector's element nodes to lend, so the guard is documentation rather than a live path; write it anyway, because the alternative is a silent double release the day something does.
- `ToListNode` (`ListNodes.cs`) - both branches, `listValue.Add(CursorSelect(...))` and `listValue.Add(sourceTable.Select())`, hand over a row the node selected; release it after the add.
- `ListForEachNode` (`ListNodes.cs`, the `for each ... return` expression) - `listValue.Add(returnResult)` hands over the return value; release it after the add. Its two `listValue.Add(DataValue.CopyValue(program.ValueManager, ((IList)returnResult)[resultIndex]))` calls should drop the explicit `CopyValue` instead: once `Add` copies, that copy is a second one nothing releases.
- `NativeMarshal.NativeValueToDataValue` - the element it builds for each list element is now a value the list copies, so dispose it (`using`) after the add.

**The three `foreach` list branches** - `ForEachNode` (`ImperativeNodes.cs`), `ListForEachNode` and `TableForEachNode` (`ListNodes.cs`) - are the same twelve lines three times: each evaluates `(IList)Nodes[0].Execute(program)`, never releases it, and leaves the loop variable's slot holding the last element. Each needs both halves:

- `program.Stack.Poke(stackIndex, null);` in the `finally` that already pops the pushed slot, which is exactly what the cursor branch beside it does.
- a release of the list it evaluated, on every exit path: `catch (BreakError) {} finally { if (!Nodes[0].ReturnsByReference) DataValue.DisposeValue(program.ValueManager, tempValue); }` on the existing `try`. (A `catch` and a `finally` on one `try` is legal and reads better than a second nested `try`.)

Delete the `NOTE:` in `ForEachNode` that records the present arrangement; it describes what this ticket changes.

### The D4-visible change, which needs a line in the migration notes

Nulling the lent slot changes what a script sees. Measured both ways, with `IsNil` rather than a comparison (a comparison with a nil operand yields nil and would not fire):

| after the loop | today | with the correction |
|---|---|---|
| `var LB : Binary; foreach LB in LL do begin end;` | `LB` holds the last element | `LB` is nil |
| `var LR : row { ... }; foreach LR in cursor(...) do begin end;` | `LR` is nil | `LR` is nil (unchanged) |

So the list branch comes to agree with the cursor branch, which is the dominant form of `foreach` and has always nulled its slot. Only the form that loops over a variable the script declared itself can observe this - `foreach var LB in ...` puts the variable out of scope at the loop. The alternative, keeping the value by giving the slot an owned copy of each element, costs a copy per iteration (for a `Binary`, a stream reference and a deallocation per element) and has no form of `foreach` agreeing with it; this ticket chooses nulling, and the D4 script below pins it so it cannot change again by accident.

## Tests

**The contract, over the containers, in C#.** `ValuesOwned` is declared on `DataValue` and honoured by some subclasses and not others, and nothing in the build or the tests checks it - which is how a subclass came to omit it quietly. Add a fixture (a new file beside `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs`, which owns value ownership but is driven entirely by D4 scripts) that reaches the value classes directly: `IServerProcess` exposes `ValueManager` (`src/engine/DAE/CLI.cs`), so `Process.ValueManager` needs no cast, and `CoreWcfProtocolTest` already builds a `Scalar` that way. For each container that can hold other values, one case: build one holding stream-backed values, dispose it, assert the server is holding the number of streams it was. Cover a `Row`, a `ListValue`, a list of rows and a row with a list column, and the other half of the flag as well - a container built over a native structure it does not own must release *nothing* when it is disposed, because that is the assumption every `FromNative` window rests on.

**The D4 shapes, in `tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4`.** That script is already the D4-visible half of stream values and already asserts on `StreamCount()` block by block; it has no lists. Add a section for them: the seven shapes of the measured table, the `Add` and `Insert` read-back (asserting the value, which is what fails today), and one block asserting the loop variable is nil after a list `foreach`. Its manifest entry is `pass` and stays `pass`.

Nothing else needs a new test. The `ListValue` constructors, the `ValuesOwned` assignments and the `ReturnsByReference` guards are wiring that the contract test and the D4 blocks exercise through their real callers.

**What to run** (fix/implement stage: targeted). `dotnet build Dataphor.slnx` first. Then the new fixture and `AssignmentValueOwnershipTest` and `BufferedOperatorColumnOwnershipTest` (`--filter "FullyQualifiedName~OwnershipTest"`), and the coverage scripts for every area a list or a row passes through: `--filter "Name=ListLibrary|Name=StreamValues|Name=RowLibrary|Name=TableLibrary|Name=LanguageConstructs|Name=StringLibrary|Name=Tables|Name=Operators|Name=Functions|Name=NilLibrary|Name=RowAllocation|Name=ScalarAllocation|Name=Aggregates|Name=Cursors|Name=CursorLibrary|Name=Derivation|Name=LibraryFilesCoverage|Name=SystemCatalog|Name=Internet|Name=Explode|Name=Patterns"`, plus `--filter "FullyQualifiedName~NativeSession|FullyQualifiedName~RemoteStreamValue|FullyQualifiedName~TableAssignment|FullyQualifiedName~RemoteValidate"` for the marshalling and client-stack sides. All of those were run green against the prototype (104 tests in three batches, about two minutes). **Do not** run `--filter "Name=DAE"`: it matches 274 tests and takes six and a half minutes (they passed against the prototype too, which is the broadest signal this change has).

## Documentation

- `docs/architecture/value-lifetime.md` ends its container section with a paragraph saying a list's elements are the part that is still not right, naming this ticket. Replace it with what is now true. The page is at 1,274 words of a 1,500 limit, so this must be a replacement rather than an addition; run `python tools/docs-size-check/check_size.py` afterwards.
- `docs/migration-notes.md` gets two entries: a `foreach` over a list leaves its loop variable nil, as a `foreach` over a cursor always has; and a list now releases what it holds, so a custom C# extension reading a list's elements after disposing the list sees released values rather than live ones (and one that handed a native value to `Add` and then released it now keeps a readable list instead of a dangling one).
- `src/engine/DAE/Runtime/Program.cs` carries a `NOTE:` at `Program.Stop` saying that a `DEBUG` stream-count check there is held up by two leaks, this one and the operator-argument one (which has landed). **Do not add the check**: three leaks measured since would still fire it rather than anything new - `bug-an-argument-is-orphaned-when-a-later-one-raises`, `bug-a-stored-binary-is-never-given-back` and `bug-a-loop-variable-stays-borrowed-after-the-loop`. Update the `NOTE:` to name those three instead.

## Deliberately out of scope, each with its number

- **A list inside a table's row.** `begin var LT : table { ID : Integer, L : list(Binary) } := table { row { 1 ID, { ... two ... } L } }; end;` leaves 5 streams today and 2 with the correction, and `insert`ing a row with a list column into a created table leaves 4 today and 1 with it. That residue is a stored row's values, which `bug-a-stored-binary-is-never-given-back` owns; this correction improves its numbers (deleting a row with a list column now releases, where before it allocated more) and does not finish them. A table-valued variable with `Binary` columns leaves nothing behind either way.
- **`TableValue` has no `Dispose(bool)` either**, and this ticket should not give it one. Measured: a table-valued variable holding stream-backed columns leaks nothing, because a D4 table variable's rows live in the memory device and are released when the device drops the native table (`MemoryDevice`, `nativeTable.Drop`), not through `TableValue`. A `TableValue` is a window onto a `NativeTable` something else holds, and the one place it assumes ownership is its `AsNative` setter, which drops the table it replaces. Settle it with a `NOTE:` at the class recording that, the measurement, and that `DataValue.Copy*` builds one with `ValuesOwned = true` whose disposal does nothing - so that the next reader of the `ValuesOwned` contract does not read the gap as an oversight. Do not change its behaviour: dropping a native table that a device owns is a far worse failure than holding one.
- **A loop variable stays borrowed after its loop.** Two assignments to a loop variable after the loop leave 2 streams behind for a list `foreach` and 1 for a cursor `foreach`, against 0 for the same two assignments with no loop in front of them - the compiler marks the slot borrowed and never unmarks it. Unaffected by this change, reachable today on both branches, and filed as `bug-a-loop-variable-stays-borrowed-after-the-loop` with the measurements; its fix depends on this one landing first.

## TODO

### Phase 1 - the container

- Add `ListValue.Dispose(bool)` releasing elements when `ValuesOwned`, mirroring `Row.Dispose(bool)`.
- Set `ValuesOwned` explicitly in all three `ListValue` constructors: `false` for the one taking a `NativeList`, `true` for the other two.
- Make `ListValue.Add`, `Insert` and `this[int] = value` copy a plain native element with `DataValue.CopyNative`.
- Set `list.ValuesOwned = true` in `NativeMarshal.NativeValueToDataValue`, which owns the `NativeList` it passes.

### Phase 2 - the callers that hand a value to a list

- `ListSelectorNode` releases each element value after adding it, guarded on `ReturnsByReference`.
- `ToListNode` releases the row it selected, in both the cursor and the table branch.
- `ListForEachNode` releases the return value it adds, and drops the now-redundant `CopyValue` in its two list-return branches.
- `NativeMarshal.NativeValueToDataValue` disposes each element value after adding it.

### Phase 3 - the three foreach list branches, in the same commit as phases 1 and 2

- `ForEachNode` (`ImperativeNodes.cs`): null the lent slot, release the list it evaluated, delete the superseded `NOTE:`.
- `ListForEachNode` and `TableForEachNode` (`ListNodes.cs`): the same two changes each.

### Phase 4 - tests, documentation, and the notes

- New C# contract fixture over the containers, both directions of `ValuesOwned`.
- New list section in `StreamValues.d4`: the seven measured shapes, the `Add`/`Insert` read-back, the nil loop variable.
- Add the `NOTE:` at `TableValue` settling its disposal, with the measurement.
- Update the `Program.Stop` `NOTE:` to name the three leaks that still block the `DEBUG` check.
- Rewrite the closing paragraph of the container section in `docs/architecture/value-lifetime.md`; run the size check.
- Two entries in `docs/migration-notes.md`.
- Build, then run the targeted sets named under *Tests*, and name the filters used in the review handoff.
