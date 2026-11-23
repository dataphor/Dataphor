description: The code that turns a JSON value into bytes for storage or for sending to a client was wired up wrongly, so the engine called the half of it that was never written and got a flat refusal instead of the value; it now works, and a new test checks every converter of its kind for the same mistake.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/System.Internet/Source/JSONObjectConveyor.cs, src/engine/DAE/Streams/Conveyor.cs, src/engine/DAE/Runtime.Data/ScalarValue.cs, tests/DAE.Server.Tests/JSONObjectConveyorTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4, docs/migration-notes.md
repro: verified
----
## What a conveyor is, for a reader without the context

A **conveyor** is the class the engine uses to turn a value of a scalar type into bytes and back - its "physical representation" - which is what happens when a value is written into a physical row or sent to a client. The base class `src/engine/DAE/Streams/Conveyor.cs` offers two ways to do it and a flag saying which one a given conveyor uses: a byte-buffer trio (`GetSize`, `Read(byte[], int)`, `Write(object, byte[], int)`) when `IsStreaming` is false, and a stream pair (`Read(Stream)`, `Write(object, Stream)`) when it is true. Every method a subclass does not override throws `NotSupportedException`.

## What landed

`JSONObjectConveyor` - the conveyor for the `Dynamic` type that the `System.Internet` library declares - overrode only the stream pair but reported `IsStreaming` as `base.IsStreaming`, which is false. Every engine call site that branches on the flag therefore took the byte-buffer branch and hit the base class's throwing stub. The same two methods also wrapped the engine's stream in a `StreamReader`/`StreamWriter` inside a `using`, so disposing them closed the stream the engine still needed: the write path calls `Write(value, stream)` into a `MemoryStream` of its own and then reads `stream.Length` to size the row buffer.

The fix, in `libraries/System.Internet/Source/JSONObjectConveyor.cs`, with nothing changed in the engine's own logic (`ticket(implement): bug-json-conveyor-declares-itself-non-streaming`):

- `IsStreaming` returns `true`.
- `Read` and `Write` use the `leaveOpen` overloads of `StreamReader`/`StreamWriter`, so the stream the engine handed in stays open - what the engine's other two streaming conveyors, `BOPObjectConveyor` and `BinaryConveyor`, already rely on.
- A named `TextEncoding` field, `new UTF8Encoding(false, true)`: UTF-8 with no byte-order mark, because the `leaveOpen` overloads take no default encoding and `Encoding.UTF8` would prepend a three-byte preamble to every value written.

Also in the implement commit: the `docs/migration-notes.md` entry, and a correction to a now-false comment in `tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4` (no assertion in that script was touched).

The review pass added, on top of that: the `Conveyor` base class's own remarks now state the real contract (a parameterless constructor, and a flag that names a set of methods the subclass overrides in full) instead of a 2008-era claim that a conveyor takes a `Stream` constructor parameter; a `NOTE:` at the one engine read site whose stream is not bounded to the value it is reading; and a strengthened sweep. The details are under *Review findings*.

## Tests

All three live in `tests/DAE.Server.Tests/JSONObjectConveyorTest.cs`, on an `InProcessTestFixture` carrying and registering `System.Platform`, `Frontend`, `System.Internet`, `SimpleDevice` in that order (`FrontendFixture` opens with the same four, for the same reason - `System.Internet`'s registration script calls a `Frontend` operator without declaring the dependency, which is `bug-system-internet-registration-needs-frontend`).

| Test | What it pins |
|---|---|
| `DynamicScalarRoundTripsThroughItsPhysicalRepresentation` | A `Dynamic` value survives `AsPhysical` -> `DataValue.FromPhysical`. The reproduction of the first defect at the lowest layer that shows it. |
| `RowWithDynamicColumnRoundTripsThroughItsPhysicalRepresentation` | The same for a row with a `Dynamic` column beside an `Integer` one, which is the path that reads the stream's length after `Write` returns (`RowValue.GetPhysicalSize`) - so it is the arm that covers the stream-ownership half. |
| `EveryConveyorImplementsTheHalfItsFlagClaims` | Every concrete `Conveyor` subclass in the loaded assemblies overrides, in full, the set of methods its `IsStreaming` names. The class-level check: it catches the same mistake in any conveyor added later, including one in a library assembly. |

All three were re-confirmed in the review pass to fail without the fix: with `IsStreaming` put back to `base.IsStreaming` and everything else left in place, the two round trips fail with `NotSupportedException` from `Conveyor.GetSize` and the sweep reports `JSONObjectConveyor reports IsStreaming as False but leaves GetSize(Object), Read(Byte[], Int32), Write(Object, Byte[], Int32) inherited`. The fix was restored and the whole suite re-run.

## Validation

`dotnet build Dataphor.slnx` clean (0 errors, 143 warnings - the same count as before the change), then the three suite parts from the root `AGENTS.md`, each in the foreground:

- part 1 (everything but the coverage scripts): 680 passed, 0 failed, 33 skipped, 7 m 41 s
- part 2 (coverage scripts A-R): 67 passed, 0 failed, 11 skipped, 3 m 10 s
- part 3 (the rest): 50 passed, 0 failed, 12 skipped, 3 m 14 s

No pre-existing failures surfaced in either the implement or the review run, so nothing was written to `tickets/.pre-existing-error.md`. Part 1 at roughly 8 minutes is the part to watch against the runner's 10-minute idle kill.

## Compatibility

No D4 semantics, operator signature, registered class name, assembly name, namespace, file format, error code, WCF contract shape, port or on-disk layout changed. The behaviour change is that an operation which used to throw now returns the value; the public `JSONObjectConveyor.IsStreaming` flips from false to true for anyone who subclasses or calls it, and its `Write` no longer closes the stream it is given. Both are in `docs/migration-notes.md` under *D4, catalog and libraries*.

## Review findings

**Read in this order:** the implement diff with no reference to the handoff; then `Conveyor.cs` and `IConveyor.cs`; then all thirteen `IsStreaming` call sites in `ScalarValue.cs`, `RowValue.cs` and `ListValue.cs`; then every `Conveyor` subclass in the built tree; then the handoff.

### Checked and clean

- **Every other conveyor in the built tree** - the twelve in `Conveyor.cs`, `VersionNumberConveyor` in `VersionNumber.cs`, and `JSONObjectConveyor` - implements exactly one of the two sets, in full, and its flag agrees. The strengthened sweep below passes over all of them.
- **Every engine site that branches on the flag.** `RowValue.ReadFromPhysical` and `ListValue.ReadFromPhysical` bound the `MemoryStream` they hand a streaming conveyor to the value's own recorded length, so a `Dynamic` column sitting beside other columns is read from its own bytes only. `RowValue.GetPhysicalSize` is the site that reads `stream.Length` after `Write` returns, which is why the row test is the arm that covers stream ownership.
- **The nesting of `JsonTextWriter`/`JsonTextReader` inside the reader and writer.** `CloseOutput`/`CloseInput` default to true, so the JSON reader and writer do close the `StreamReader`/`StreamWriter` they wrap - harmless now that neither owns the engine's stream, and confirmed by running the round trips rather than taken on trust.
- **`-1` as the buffer size on both `leaveOpen` overloads.** Both constructors map `-1` to their own default buffer size rather than rejecting it.
- **The `.d4` comment correction.** Accurate, and no assertion in `Internet.d4` was touched; part 2 of the suite covers that script and is green.
- **Compatibility and the migration note.** Checked against each surface the project rules list; the note's text matches what the code now does.
- **The three tests pay for themselves.** Two are the reproduction at the lowest layer that shows the defect (re-confirmed failing without the fix), one is the class-level invariant. None restates the implementation or verifies a mock, so none was cut, and no fourth was needed: the defects this pass found were in the sweep and in comments, and were fixed in place rather than covered by a new test.

### Minor, fixed in this pass

- **The sweep had the gap the handoff asked for a second opinion on, and one more besides.** It skipped any conveyor that overrode both sets or neither, and it accepted a *partially* overridden set, because it tested the three byte-buffer methods with `||` - a conveyor overriding `GetSize` and `Write(object, byte[], int)` but not `Read(byte[], int)` passed while still throwing on every read. Both go away under a simpler and strictly stronger invariant: **the set of methods `IsStreaming` names must be overridden in full.** That subsumes the old check (a flag pointing at the set the subclass did not implement now fails because that set is wholly inherited), catches partial sets, and needs no skip branch - a conveyor that overrides both sets passes, correctly, because both ways work, and one that overrides neither now fails, correctly, because every path through it throws. The test now carries the two sets as named tables and reports an offender's inherited methods with their parameter types, since `Read` appears in both.
- **The sweep's parameterless-constructor message cited `Conveyor`'s remarks, which say the opposite.** Those remarks claimed a conveyor provides "a single constructor which takes a Stream as its only parameter"; the real requirement comes from `ClassLoader.CreateObject`, which calls `Activator.CreateInstance(type, null)`. The message now cites that, and the remarks themselves are corrected - they are the documentation a custom scalar type's author reads, and they now also state the flag/override contract that this whole ticket is about.
- **The `TextEncoding` comment overclaimed.** It said the encoding is what both `StreamReader` and `StreamWriter` default to. It is exactly `StreamWriter`'s default, but `StreamReader`'s default decodes a malformed byte to U+FFFD where this one throws. The behaviour is right for a physical representation - a value that cannot be decoded should fail rather than change - so the comment was corrected rather than the code.
- **The library-ordering rationale was duplicated.** The new fixture restated `FrontendFixture`'s explanation of why `Frontend` must register before `System.Internet`; it now points at the sibling list, so `bug-system-internet-registration-needs-frontend` landing has one place to look and one to follow.
- **A plan ticket still described the defect as open.** `tickets/plan/debt-streams-coverage.md` said the conveyor "is wired wrongly" and recommended writing exactly this reflection check. Its bullet now records that the fix and the check have landed and names the test, so whoever works that ticket does not write a second one.

### Parked as a tripwire, not filed

- **`ScalarValue.ReadFromPhysical` hands a streaming conveyor a stream spanning the rest of the buffer**, where `RowValue` and `ListValue` bound theirs to the value's own length. Every caller today passes a buffer that ends with the value, so nothing reads past it, and the suite confirms the reachable paths. But a caller that did not would hand following columns' bytes to `JSONObjectConveyor`, whose `StreamReader` decodes a whole buffer at a time and would reject a byte sequence that is not valid UTF-8 - which an `Integer` column holding, say, 200 produces. Conditional, so a `NOTE:` at that site rather than a ticket; the fix if it ever trips is to bound the stream, as the two other readers do.

### Weighed and declined, with the reason

- **Making the bad state unrepresentable - splitting `Conveyor` so a subclass implements one set or the other and the flag is derived - is the right rung of the ladder and is still not worth taking.** The implementer's reasoning holds: `Conveyor` is a public extension point that a custom scalar type subclasses, so changing its shape is a compatibility change this project puts behind an explicit decision, which would be a `blocked/` proposal rather than a `debt-` ticket. With the sweep now asserting the full invariant at test time for every conveyor in every loaded assembly, including library ones, the split would buy compile-time enforcement for in-tree code at the cost of churning a published base class - so it is declined, not deferred. If it is ever taken, the sweep goes away with it rather than both being kept.
- **Nothing asserts the absence of the byte-order mark directly.** The round trips would pass either way, since the same conveyor reads what it wrote, and the honest alternative - an assertion on `physical.Length` - pins a byte count, which is evidence rather than specification. Left as the implementer had it. The preamble question is now stated in the code comment at the encoding, which is where someone changing the encoding will meet it.
- **No out-of-process or client-connection test.** The user-visible symptom arrived across a client connection, but the defect lived in the engine's in-process physical-representation path, which is what the two reproductions drive. A client-level test would cost a protocol fixture and pin nothing the in-process tests do not.
- **`Sample.Shipping`'s `CoordinateConveyor` is out of the sweep's reach**, because that sample's C# project is not converted and the build does not assemble it, so it never joins the loaded assemblies. It is consistent by inspection: the byte-buffer trio, flag left at the base class's false. The four conveyors under `legacy/` are excluded from the build.
