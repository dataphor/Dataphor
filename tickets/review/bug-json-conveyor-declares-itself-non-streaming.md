description: The code that turns a JSON value into bytes for storage or for sending to a client was wired up wrongly, so the engine called the half of it that was never written and got a flat refusal instead of the value; it now works, and a new test checks every converter of its kind for the same mistake.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/System.Internet/Source/JSONObjectConveyor.cs, tests/DAE.Server.Tests/JSONObjectConveyorTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4, docs/migration-notes.md
repro: verified
----
## What a conveyor is, for a reader without the context

A **conveyor** is the class the engine uses to turn a value of a scalar type into bytes and back — its "physical representation" — which is what happens when a value is written into a physical row or sent to a client. The base class `src/engine/DAE/Streams/Conveyor.cs` offers two ways to do it and a flag saying which one a given conveyor uses: a byte-buffer trio (`GetSize`, `Read(byte[], int)`, `Write(object, byte[], int)`) when `IsStreaming` is false, and a stream pair (`Read(Stream)`, `Write(object, Stream)`) when it is true. Every method a subclass does not override throws `NotSupportedException`.

## What was wrong and what changed

`JSONObjectConveyor` — the conveyor for the `Dynamic` type that the `System.Internet` library declares — overrode only the stream pair but reported `IsStreaming` as `base.IsStreaming`, which is false. Every engine call site that branches on the flag therefore took the byte-buffer branch and hit the base class's throwing stub. The same two methods also wrapped the engine's stream in a `StreamReader`/`StreamWriter` inside a `using`, so disposing them closed the stream the engine still needed: the write path calls `Write(value, stream)` into a `MemoryStream` of its own and then reads `stream.Length` to size the row buffer.

Three changes in `libraries/System.Internet/Source/JSONObjectConveyor.cs`, and nothing in the engine:

- `IsStreaming` returns `true`.
- `Read` and `Write` use the `leaveOpen` overloads of `StreamReader`/`StreamWriter`, so the stream the engine handed in stays open, which is what the engine's other two streaming conveyors (`BOPObjectConveyor`, `BinaryConveyor`) already do.
- A named `TextEncoding` field, `new UTF8Encoding(false, true)` — UTF-8 with no byte-order mark. The `leaveOpen` overloads take no default encoding, and `Encoding.UTF8` would have prepended three preamble bytes to every value, where `StreamReader`/`StreamWriter`'s own default is UTF-8 without one.

Also in this commit: the migration-notes entry, and a correction to a now-false comment in `tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4` (no assertion in that script was touched).

## Tests added, and what each verifies

All three are in the new `tests/DAE.Server.Tests/JSONObjectConveyorTest.cs`, on an `InProcessTestFixture` carrying and registering `System.Platform`, `Frontend`, `System.Internet`, `SimpleDevice` (that order matters — see the fixture's own comment).

| Test | What it pins |
|---|---|
| `DynamicScalarRoundTripsThroughItsPhysicalRepresentation` | A `Dynamic` value survives `AsPhysical` → `DataValue.FromPhysical`. The reproduction of the first defect at the lowest layer that shows it. |
| `RowWithDynamicColumnRoundTripsThroughItsPhysicalRepresentation` | The same for a row with a `Dynamic` column beside an `Integer` one, which is the path that reads the stream's length after `Write` returns — so it is the arm that covers the stream-ownership half. |
| `EveryConveyorImplementsTheHalfItsFlagClaims` | Every concrete `Conveyor` subclass in the loaded assemblies declares overrides that agree with its `IsStreaming`. This is the class-level check: it catches the same mistake in any conveyor added later, including one in a library assembly. |

**Both reproductions were confirmed to fail without the fix.** With `IsStreaming` put back to `base.IsStreaming` and everything else left in place, all three tests fail: the two round trips with `NotSupportedException` from `Conveyor.GetSize`, and the sweep reporting exactly one mismatch, `JSONObjectConveyor reports IsStreaming as False but overrides only the streaming Read/Write methods`. The fix was then restored and the suite re-run.

## Validation run

`dotnet build Dataphor.slnx` clean (0 errors, 143 pre-existing warnings), then the three suite parts from the root `AGENTS.md`, each in the foreground:

- part 1 (everything but the coverage scripts): 680 passed, 0 failed, 33 skipped, 8 m 34 s
- part 2 (coverage scripts A–R): 67 passed, 0 failed, 11 skipped, 3 m 3 s
- part 3 (the rest): 50 passed, 0 failed, 12 skipped, 3 m 36 s

No pre-existing failures surfaced, so nothing was written to `tickets/.pre-existing-error.md`. Part 1 at 8 m 34 s is within the band `AGENTS.md` records but is the part to watch.

## Where a reviewer should look hardest

- **The sweep's "both halves, or neither" skip.** `EveryConveyorImplementsTheHalfItsFlagClaims` asserts only the two unambiguous cases: a type that overrides only the stream pair must report `true`, one that overrides only the byte-buffer trio must report `false`. A type that overrides both halves, or neither, is skipped — the flag cannot be wrong against what is actually there. That is deliberate and no conveyor in the tree is in either case today, but it is the gap in the check and worth a second opinion.
- **The encoding choice is load-bearing and untested in isolation.** Nothing asserts the absence of the byte-order mark directly; the round trips would pass either way, since the same conveyor reads what it wrote. The ticket's own measurement (8 bytes for the scalar: a 1-byte header plus 7 bytes of JSON text) is the evidence that no preamble is written, and that figure is deliberately *not* asserted in the test — a byte count is evidence, not a specification. If a reviewer wants the preamble pinned, the honest place is an assertion on `physical.Length`, and that is a judgement call I left alone.
- **`Describe()` compares `ToString()` of the native values.** A `Dynamic` value's native form is a Newtonsoft `JToken`, which the test assembly does not reference, so the comparison goes through text. That is the right layer (text is what the conveyor writes), but it means a hypothetical JSON difference that `JToken.ToString()` normalizes away would not be caught.
- **`JsonTextWriter`/`JsonTextReader` still close the `StreamWriter`/`StreamReader` they wrap** (`CloseOutput`/`CloseInput` default to true). That is now harmless because the writer and reader no longer own the stream, but it is the kind of nesting a reviewer should confirm rather than take on trust.

## Known gaps, stated plainly

- **The bad state is still representable.** Splitting `Conveyor` so a subclass implements one pair or the other and the flag is derived rather than declared would remove this mistake for good. It is not done here because `Conveyor` is a public extension point a custom scalar type subclasses, so changing its shape is a compatibility change this project puts behind a ticket that says so. The reflection sweep catches the same class of mistake at test time at no compatibility cost; a future ticket that takes the split should delete the sweep rather than keep both.
- **`Sample.Shipping`'s `CoordinateConveyor` is out of the sweep's reach.** That sample's C# project is not converted and the build does not assemble it, so it is never in the loaded assemblies. It is consistent by inspection (byte-buffer trio, flag left alone). The four conveyors under `legacy/` are excluded from the build.
- **No out-of-process or client-connection test.** The user-visible symptom was a `NotSupportedException` arriving across a client connection; what is tested is the engine's physical-representation path in process, which is where the defect actually lived and the lowest layer that reproduces it. A client-level test would cost a protocol fixture and pin nothing the in-process test does not.
- **`tickets/plan/debt-streams-coverage.md` describes this defect in its "Interactions" section and recommends exactly this check.** It is a plan-stage ticket and was deliberately left untouched; whoever works it next should know the check has landed here rather than there.

## Compatibility

No D4 semantics, operator signature, registered class name, assembly name, namespace, file format, error code, WCF contract shape, port or on-disk layout changed. The behaviour change is an operation that used to throw now returning the value; the public `JSONObjectConveyor.IsStreaming` flips from false to true for anyone who subclasses or calls it. Both are in `docs/migration-notes.md` under *D4, catalog and libraries*.

## Review findings

Nothing parked as a tripwire. The one conditional concern worth knowing — the sweep's skip of conveyors that override both halves or neither — is stated above rather than in a code comment, because it is a property of the test itself and the test is the only place it could be read.
