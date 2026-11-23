description: The code that turns a JSON value into bytes for storage or for sending to a client is wired up wrongly, so the engine calls the half of it that was never written and gets a flat refusal instead of the value.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/System.Internet/Source/JSONObjectConveyor.cs, src/engine/DAE/Streams/Conveyor.cs, src/engine/DAE/Runtime.Data/RowValue.cs, src/engine/DAE/Runtime.Data/ScalarValue.cs
repro: static
severity: edge-case
likelihood: unusual
tradeoffs: Nothing is known to have hit it in the fifteen years the code has been shipping, it needs an application that puts a JSON value in a table and reads it across a process boundary, and the fix touches a class an extension author can subclass - so a maintainer may prefer to wait for a report, or to fold it into whatever ticket next covers conveyors.
----
## What is wrong

A **conveyor** is the class the engine uses to turn a value of a scalar type into bytes and back - its "physical representation" - which is what happens when a value is stored in a physical row or sent to a client. The base class, `src/engine/DAE/Streams/Conveyor.cs`, offers two ways to do it and a flag that says which one a given conveyor uses: a byte-buffer trio (`GetSize`, `Read(byte[], int)`, `Write(object, byte[], int)`) when `IsStreaming` is false, and a stream pair (`Read(Stream)`, `Write(object, Stream)`) when it is true. Every method the subclass does not override throws `NotSupportedException`.

`libraries/System.Internet/Source/JSONObjectConveyor.cs`, the conveyor for the `Dynamic` type that `System.Internet` declares, overrides only the stream pair - but its `IsStreaming` is

```csharp
public override bool IsStreaming { get { return base.IsStreaming; } }
```

and the base returns `false`. So the flag says "use the byte-buffer trio" while only the stream pair exists. Every call site in the engine that chooses between the two branches on `IsStreaming` (`RowValue.cs`, `ScalarValue.cs` and `ListValue.cs`), so the consequence is both halves of one mistake:

- the JSON reader and writer in this file are **never called**; and
- the path that *is* taken calls `GetSize`, which throws `NotSupportedException` with no message.

An override that returns the value it is overriding is a strong hint that `true` was meant. The two other streaming conveyors in the tree, `BOPObjectConveyor` and `BinaryConveyor` in `Conveyor.cs`, both return `true` and both override only the stream pair; the two non-streaming ones in library and sample code, `VersionNumberConveyor` and `Sample.Shipping`'s `CoordinateConveyor`, both override the byte-buffer trio and leave `IsStreaming` alone. `JSONObjectConveyor` is the one class in the repository whose flag and overrides disagree.

This is not a consequence of the .NET 10 move or of the Newtonsoft.Json upgrade. The file has been this way since it was written.

## What a user would see

A `NotSupportedException` surfacing from an operation on a table with a `Dynamic` column, rather than the value. Not a wrong answer and not corruption - a refusal.

## What is known about reaching it

Found by `debt-test-json-depth-boundary` while looking for a way to exercise the conveyor's JSON reader, and `repro: static` because the one experiment that was run did **not** reach it:

- **Measured.** A `Dynamic` value inserted into a session table and read back in process works, including a value nested 71 levels deep. The memory device keeps the value as it is and never asks for a physical representation, so no conveyor method is called at all.
- **Static, not verified.** The physical read and write paths in `RowValue.cs` are what a row crossing to a client goes through, and those are the ones that branch on `IsStreaming`. So the case that would confirm this is **a table with a `Dynamic` column read through a real client connection**, which the suite can do: the out-of-process fixture exists (`OutOfProcessInstance`, `ProtocolClient.cs` in `tests/DAE.Server.Tests`). Someone picking this up should run that first - if it throws `NotSupportedException` the defect is confirmed and the test is also its reproduction; if it does not, find out what path *does* take a physical representation of a scalar with a class before changing anything.

Whether a `Dynamic` column is a shape anyone uses is a fair question for triage. There is no device scalar type mapping for `Dynamic`, so such a column can only live in the memory device - a session table, a process table or an in-memory table variable - and an application that fetches JSON with `System.Internet` and holds it in one of those is the user who would meet this.

## What the fix looks like, and the choice in it

The small fix is `IsStreaming => true`, which makes the two methods already written the ones that get called. That is one line, and it should carry a reproduction.

The question worth settling first is whether to catch the class rather than the instance. Two rungs are available:

1. **A consistency test over conveyors.** One test that walks the `Conveyor` subclasses in the loaded assemblies and asserts that each one's declared overrides match its `IsStreaming` - a type that overrides only the stream pair must report `true`, one that overrides only the byte-buffer trio must report `false`. Five lines of reflection, and it holds for conveyors added later, including ones in extension assemblies the suite loads. This is the option this ticket recommends.
2. **Making the bad state unrepresentable** - splitting the base class so a conveyor implements one pair or the other and the flag is derived rather than declared. That removes the mistake for good, but `Conveyor` is a public extension point a custom scalar type subclasses, so it is a compatibility change and needs a ticket that says so.

Option 1 plus the one-line fix is a single coherent change. Option 2 is a larger decision and should not be taken inside a bug fix.

## Related

- `debt-streams-coverage` (plan) covers `src/engine/DAE/Streams/Conveyor.cs` - the engine's own conveyors for the system types, and the gap that none of them is exercised through a device that writes physical rows. Different site and different cause from this one, but the script it will write is the natural place for a conveyor consistency check to live, and whoever works either ticket should read the other.
- `debt-test-json-depth-boundary` records what the JSON readers that *are* reachable do, and deliberately writes no conveyor case.
