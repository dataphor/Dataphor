description: The code that turns a JSON value into bytes for storage or for sending to a client is wired up wrongly, so the engine calls the half of it that was never written and gets a flat refusal instead of the value.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/System.Internet/Source/JSONObjectConveyor.cs, src/engine/DAE/Streams/Conveyor.cs, src/engine/DAE/Runtime.Data/ScalarValue.cs, src/engine/DAE/Runtime.Data/RowValue.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4, docs/migration-notes.md
difficulty: easy
repro: verified
----
## What is wrong

A **conveyor** is the class the engine uses to turn a value of a scalar type into bytes and back - its "physical representation" - which is what happens when a value is written into a physical row or sent to a client. The base class, `src/engine/DAE/Streams/Conveyor.cs`, offers two ways to do it and a flag saying which one a given conveyor uses: a byte-buffer trio (`GetSize`, `Read(byte[], int)`, `Write(object, byte[], int)`) when `IsStreaming` is false, and a stream pair (`Read(Stream)`, `Write(object, Stream)`) when it is true. Every method a subclass does not override throws `NotSupportedException`.

`libraries/System.Internet/Source/JSONObjectConveyor.cs`, the conveyor for the `Dynamic` type that `System.Internet` declares, overrides only the stream pair but reports `IsStreaming` as `base.IsStreaming`, which is `false`. So the flag says "use the byte-buffer trio" while only the stream pair exists, and every engine call site that branches on the flag takes the branch that throws.

**There is a second defect in the same file, at the same two methods.** `Read` wraps the engine's stream in a `StreamReader` and `Write` wraps it in a `StreamWriter`, each inside a `using`, so disposing the reader or writer **closes the stream the engine handed in**. The engine's write path needs that stream afterwards: `ScalarValue.GetPhysicalSize` and `RowValue.GetPhysicalSize` both call `Write(value, stream)` into a `MemoryStream` of their own and then read `stream.Length` to size the buffer. So flipping the flag alone replaces one exception with another. The engine's other two streaming conveyors, `BOPObjectConveyor` and `BinaryConveyor` in `Conveyor.cs`, write to the stream they are given and leave it open - that is the contract this file breaks.

Neither defect comes from the .NET 10 move or the Newtonsoft.Json upgrade. The file has been this way since it was written.

## What is measured

All three measurements below are from a throwaway `InProcessTestFixture` carrying `System.Platform`, `Frontend`, `SimpleDevice` and `System.Internet`, on this branch at `911867ac`. No out-of-process client is needed: the engine's own physical-representation path reproduces it in process, which is the lowest layer that shows it.

- **The defect, reproduced.** Evaluating `Dynamic('{"a":1}')` on a process and then asking the result for its `AsPhysical` throws `NotSupportedException` with no message, from `Conveyor.GetSize` (`Conveyor.cs:35`) by way of `Scalar.GetPhysicalSize` (`ScalarValue.cs:721`). Same for a row with a `Dynamic` column.
- **`IsStreaming => true` on its own is not enough.** With only the flag flipped, the same call throws `ObjectDisposedException`, "Cannot access a closed Stream", from `MemoryStream.get_Length` at `ScalarValue.cs:719` - the `using` on the `StreamWriter` having closed the engine's stream.
- **Flag plus leaving the stream open round-trips.** With `IsStreaming => true` and both the reader and the writer built on their `leaveOpen` overloads, a `Dynamic` value round-trips through `AsPhysical` (8 bytes: a 1-byte scalar header and the 7 bytes of the JSON text) and so does a row with a `Dynamic` column (23 bytes). The value read back equals the value written.

The engine's read side needs no change: `ScalarValue.ReadFromPhysical` and both `RowValue` read branches hand the conveyor a `MemoryStream` over the right slice of the buffer - bounded by the written length prefix in the row case - and close it themselves afterwards.

## What a user would see today

A `NotSupportedException` with no message, surfacing from an operation that takes the physical representation of a `Dynamic` value: evaluating such a value through a client connection, or fetching a row with a `Dynamic` column across one. Not a wrong answer and not corruption - a refusal. There is no device scalar type mapping for `Dynamic`, so such a column can only live in the memory device - a session table, a process table or an in-memory table variable - and the memory device keeps values as they are and never asks for a physical representation. That is why nothing in process has met this.

## The fix

One `IsStreaming` override and two stream-ownership corrections in `libraries/System.Internet/Source/JSONObjectConveyor.cs`:

```csharp
public override bool IsStreaming { get { return true; } }

// UTF-8 without a byte-order mark: what StreamReader and StreamWriter default to,
// named because the leaveOpen overloads have no default encoding.
private static readonly Encoding TextEncoding = new UTF8Encoding(false, true);

// in Read: leaveOpen, because the engine owns the stream and reads its length after this returns
using (var reader = new StreamReader(stream, TextEncoding, true, -1, true))

// in Write: the same
using (var writer = new StreamWriter(stream, TextEncoding, -1, true))
```

The encoding has to be named because the `leaveOpen` overloads take no default, and it must be `new UTF8Encoding(false, …)` rather than `Encoding.UTF8`: the latter has a byte-order-mark preamble and would put three extra bytes at the front of every value, where `StreamReader` and `StreamWriter`'s own default is UTF-8 without one. `System.Text` is already imported in this file. Preserve the file's tabs, CRLF line endings and leading byte-order mark.

## The check that catches the class, not just the instance

`JSONObjectConveyor` was the one class in the repository whose flag and overrides disagreed, and nothing would have said so. Add **one reflection test** asserting that every concrete `Conveyor` subclass's declared overrides agree with its `IsStreaming`: a type that overrides only the stream pair must report `true`, one that overrides only the byte-buffer trio must report `false`. Compare each method's `DeclaringType` with `typeof(Conveyor)` to tell an override from an inherited throwing stub, and read `IsStreaming` off an instance from `Activator.CreateInstance` (the base class's own remarks require a parameterless constructor, and all fourteen have one; a future conveyor without one should fail here with a message that says so rather than with a bare `MissingMethodException`).

This shape is measured, on the same throwaway fixture. Sweeping the loaded assemblies from a fixture that has registered `System.Internet` finds **14** concrete conveyors - the 13 in `Alphora.Dataphor.DAE` (`VersionNumberConveyor` plus the 12 in `Conveyor.cs`) and `System.Internet`'s - in 276 ms, and reports exactly one mismatch: `JSONObjectConveyor`. So the check both catches this defect and holds for conveyors added later, including ones in library assemblies.

Two things keep that sweep honest and both belong in the test:

- **Assert that the sweep reached a library assembly**, by requiring `JSONObjectConveyor` to be among the types found. Otherwise a sweep that silently saw only the engine assembly would pass while covering nothing new.
- **Do not assert the count.** NUnit runs the whole project in one process, so another fixture's libraries may also be loaded and the set can be larger than 14. Finding more conveyors is the point; a fixed number would make the test fail for the wrong reason.

The 15th conveyor in the tree, `Sample.Shipping`'s `CoordinateConveyor`, is consistent (byte-buffer trio, flag left alone) but out of reach: that sample's C# project is not converted and the build does not assemble it. The four under `legacy/` are excluded from the build. Neither is in scope here.

## What was weighed and not done

**Making the bad state unrepresentable** - splitting `Conveyor` so a subclass implements one pair or the other and the flag is derived rather than declared - would remove this mistake for good, and is the reason the reflection test above is second best. It is not in this ticket: `Conveyor` is a public extension point that a custom scalar type subclasses, so changing its shape is a compatibility change, and this project's rules put that behind a ticket that says so explicitly. The reflection test catches the same class of mistake at the same moment - a test run - at no compatibility cost, so nothing is being deferred that the check does not already cover. A future ticket that does take the split should delete the reflection test rather than keep both.

## Documents to correct in the same commit

- **`docs/migration-notes.md`** needs an entry: this is a behaviour change a `System.Internet` application can notice. A `Dynamic` value's physical representation goes from throwing `NotSupportedException` to being the value's JSON text in UTF-8 without a byte-order mark, so an operation that previously failed now returns the value. `JSONObjectConveyor` is public, and its `IsStreaming` changes from false to true for anyone who subclasses or calls it.
- **`tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4`** carries a comment stating that the conveyor's JSON reader "is unreachable (it never reports itself as streaming), so there is nothing to assert about it here". That becomes false with this fix. Correct the comment and point it at the new test; do **not** add a conveyor case to the script and do not change any of its assertions - a D4 script cannot reach the physical representation on the memory device, which is what `debt-test-json-depth-boundary` already established.

`tickets/plan/debt-streams-coverage.md` describes this defect in its "Interactions" section and recommends exactly this check. It is a plan-stage ticket and will be rewritten by its own stage; leave it alone, but whoever works it next should know the check has landed here rather than there.

## Notes for whoever implements this

- `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` lists every source file in an explicit `Compile` item group, sorted by name. A new test file that is not added there compiles into nothing and its cases are silently not discovered.
- The fixture the reproduction needs is an `InProcessTestFixture` subclass with `Libraries` and `LibrariesToRegister` both set to `System.Platform`, `Frontend`, `SimpleDevice` and `System.Internet`. `Frontend` must come before `System.Internet` - `System.Internet`'s registration script calls the `Frontend.ExecuteScript` operator without declaring the dependency (`bug-system-internet-registration-needs-frontend`) - and `SimpleDevice` must be in the copied set or `Frontend`'s registration fails with `Object "SimpleDevice" not found`.
- One file can hold both the reproduction and the consistency sweep: they want the same fixture and the same library set.

## TODO

- Flip `IsStreaming` to `true` in `libraries/System.Internet/Source/JSONObjectConveyor.cs`, and rebuild `Read` and `Write` on the `leaveOpen` overloads with an explicitly named UTF-8-without-byte-order-mark encoding, as above. Preserve the file's tabs, CRLF endings and byte-order mark.
- Add a test file to `tests/DAE.Server.Tests` holding the reproduction: a `Dynamic` value, and a row with a `Dynamic` column, each round-tripped through `AsPhysical` and asserted equal to what went in. Assert on the value read back, not on the byte counts above - those are recorded as evidence, not as a specification.
- Add the conveyor consistency check to the same file: every concrete `Conveyor` subclass found in the loaded assemblies has overrides that agree with its `IsStreaming`, with `JSONObjectConveyor` asserted present and no assertion on the total.
- Add the new file to the `Compile` item group in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`, in name order.
- Correct the stale comment in `tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4`, changing no assertion in the script.
- Add the migration-notes entry described above.
- Build, then run the suite in the three parts the root `AGENTS.md` names, each as its own foreground command.
