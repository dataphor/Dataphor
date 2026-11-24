description: Two tests the original product shipped - one for how the engine shares and releases large values such as pictures and documents, one for how it turns each kind of value into bytes and back - are still in the repository but were switched off years ago and no longer compile; bring them back as ordinary tests so that part of the engine is checked again.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs, tests/DAE.Server.Tests/JSONObjectConveyorTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, src/engine/DAE/Streams/ServerStreamManager.cs, src/engine/DAE/Streams/Conveyor.cs, src/engine/DAE/Streams/ServerStream.cs, src/engine/DAE/Streams/MemoryStreamProvider.cs
difficulty: hard
----
## What this is

The engine's **Streams** area sits at 40.7 % line coverage over 1,129 coverable lines (full instrumented run of 2026-11-21 at commit `f6200026`, recorded in `docs/coverage-report.md`), and the two largest gaps in it are the stream manager's reference bookkeeping and the per-type byte readers and writers called **conveyors**.

Tests for both exist. They shipped with 3.1 as D4 diagnostic operators, `TestStreamManager()` and `TestConveyors()`, and their C# is still in the tree at `tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs` - but switched off, and so no longer compiled or compilable:

- `TestStreamManagerNode`, from line 503, sits inside `#if USESCALARMANAGER` (opened at line 481, closed at line 631). That symbol is defined nowhere, and the code inside it is written against the pre-3.0 engine API (`AProcess`, `new Scalar(AProcess, type, stream)`, `LValue.Conveyor`, a two-argument `Open`).
- `TestConveyorsNode`, from line 242, sits inside a block comment (lines 240 to 479) and leans on `ScalarManager`, which the engine no longer has.

Their `create operator` lines in `tests/libraries/TestFramework.Coverage.Base/Documents/DiagnosticsLogic.d4` are commented out to match, and the script that called them, `tests/libraries/TestFramework.Coverage.Scripts/Documents/DAE.d4`, is recorded in the coverage manifest as a known failure owned by `test-pre-3-1-scripts-policy`.

**What they assert is the specification.** There is no reference runtime for this engine, so a 3.1 assertion about how a referenced stream behaves is the record of what correct looks like. The job is to bring those assertions back, not to invent new ones.

## Bring them back as plain NUnit fixtures, not as D4 operators

Two new fixtures in `tests/DAE.Server.Tests`, each a direct test of the class it covers. `JSONObjectConveyorTest.cs` is the model for shape and for its remarks style.

This is a deliberate change of layer from 3.1's D4 operators, for a reason worth recording: neither class needs a server.

- `ServerStreamManager`'s constructor takes an `IServer` and **ignores it** (`src/engine/DAE/Streams/ServerStreamManager.cs`, around line 196 - it only builds a `MemoryStreamProvider`), so a test can construct one directly and drive `Allocate`, `Open(ownerID, streamID, mode)`, `Close(ownerID, streamID)`, `Reference`, `Deallocate` and `Count()` with no engine at all.
- The conveyors are a plain C# API in `Alphora.Dataphor.DAE.Streams` with no engine dependency; each is constructible with `new`.

Driving them directly is the lowest layer that shows the behaviour, it lets a test assert on `Count()` as an exact number, and it needs no library registration, no coverage script and no manifest entry. It also reaches paths a D4 script cannot: the planning pass that filed this established by measurement that a `Binary` or `Graphic` **table column** never holds a managed stream (the memory device stores the bytes natively), so the only stream-backed values a D4 script can make are variables - which means `MoveReferences` and the re-parenting of a referenced stream when its source is deallocated have no D4 route at all. `debt-streams-coverage` covers what D4 does reach, from a coverage script, and names this ticket as a prerequisite.

### `ServerStreamManagerTest`

Port `TestStreamManagerNode`'s assertions in the order it makes them. The payload it writes is not the point - it writes a string through a conveyor only because it needed some bytes - so use whatever payload is cleanest; `StringConveyor` is a byte-buffer conveyor (its `IsStreaming` is false), so if you keep the string, size a buffer with `GetSize`, fill it with `Write(value, buffer, offset)`, and write that buffer to the stream.

The assertions to preserve:

- A stream can be allocated, opened, written, closed, reopened and read back with the same contents.
- Two streams opened on the same identifier are isolated in position: reading through one does not move the other's position.
- Closing twice and then deallocating leaves the manager holding nothing.
- A stream referenced twice, then written through the source and through one of the references, leaves three distinct values: the source has its new value, the untouched reference still has the original, and the written reference has its own new value. This is the copy-on-write path - `ServerStream.Write` calls `ServerStreamManager.Change`, which copies the referencing stream off its source.
- A reference of a reference, two deep, still reads the value it was taken from.
- Deallocating a stream in the middle of a reference chain leaves the streams below it reading the value they had. This is `MoveReferences` and the re-parenting in `Deallocate`, and nothing else in the suite reaches either.

Add one assertion 3.1 did not make, because it is the whole point of the area: **`Count()` is back to its starting value when the fixture has deallocated everything it allocated.** Assert it at the end of each test, not only at the end of the fixture.

### `ConveyorTest`

Port `TestConveyorsNode`'s per-type round trips against the current API. Each conveyor declares which half of the API it implements through `IsStreaming`; use that half:

- Byte-buffer conveyors (`IsStreaming` false): size with `GetSize(value)`, write with `Write(value, buffer, offset)`, read back with `Read(buffer, offset)`. This is `BooleanConveyor`, `ByteConveyor`, `Int16Conveyor`, `Int32Conveyor`, `Int64Conveyor`, `DecimalConveyor`, `StringConveyor`, `DateTimeConveyor`, `TimeSpanConveyor` and `GuidConveyor`.
- Streaming conveyors (`IsStreaming` true): write with `Write(value, stream)` and read back with `Read(stream)`. This is `BinaryConveyor` and `BOPObjectConveyor`.

Round-trip the boundary values 3.1 used - `MinValue` and `MaxValue` for each numeric width, `false` and `true` for boolean, empty and non-empty for string and binary - and write at a non-zero offset in the buffer as well as at zero, since every engine call site writes these into a shared row buffer at an offset.

**Do not re-check that a conveyor's `IsStreaming` agrees with the methods it overrides.** `JSONObjectConveyorTest.EveryConveyorImplementsTheHalfItsFlagClaims` already asserts that over every concrete `Conveyor` subclass in the loaded assemblies, from `bug-json-conveyor-declares-itself-non-streaming`. This ticket covers what the conveyors *do*.

## Edge cases & interactions

- **A ported assertion that fails is a finding, not a test to weaken.** If 3.1's expectation does not hold on the current engine, that is a defect the port has found: file it (the planning pass that filed this ticket found two already, `bug-assigning-a-binary-variable-leaks-its-stream` and `bug-binary-write-accessor-keeps-the-old-values-tail`), and leave the assertion as 3.1 wrote it, with the test marked so the run is honest about it. Never edit an expected value to get a green run.
- **The stream manager locks on itself and nothing else.** Every public method of `ServerStreamManager` is `lock (this)`. A test that drives it from one thread says nothing about concurrent use; do not write a concurrency case here and do not claim one. If the port makes a single-threaded invariant obvious enough to be worth a concurrent check, say so in the handoff rather than adding it.
- **Deallocating a stream that is still open.** `Deallocate` removes the header and destroys the provider's stream without consulting `StreamCount` (the open count on the header), so a stream deallocated while a `ManagedStream` is still open on it leaves that stream pointing at a destroyed provider entry. 3.1's test always closed before deallocating. Establish what happens if it does not - an exception, or a read of freed state - and record the answer; if it is a defect, it is its own ticket, not an arm on this one.
- **`StreamID` values are allocation order.** Never assert on a stream identifier's value, only on counts and on contents. `_nextStreamID` starts at 1 and only goes up, so an assertion on an identifier would break the moment an earlier test in the same fixture allocates one more stream.
- **Stream tracing is off and stays off.** `ServerStreamManager.StreamTracingEnabled` builds the `StreamEvents` list and the `StreamOpens` list that `StreamOpensAsString()` formats. Nothing sets it, and nothing registers a D4 operator for `StreamOpensAsString` - `SystemStreamsOpenNode` exists in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs` but `src/engine/DAE/Schema/SystemCatalog.d4` has no `StreamsOpen` line, so the only D4-visible stream diagnostic is `Diagnostics.StreamCount()`. Turning tracing on inside a test to inspect the event list is allowed if it helps a diagnostic message; do not build the test's assertions on it, because production never runs with it on.
- **`FileStreamProvider` stays uncovered, deliberately.** `src/engine/Server/Streams/FileStreamProvider.cs` is 91 coverable lines at 0 %, constructed only inside `#if UseFileStreamProvider`, and that `#define` is commented out at the top of `ServerStreamManager.cs`. No test can reach it. Do not try, and do not make the stream manager configurable to let a test reach it - whether the engine still wants a file-backed stream provider is a design question outside this ticket.
- **Two files counted in this area are dead and are going.** `src/engine/DAE/Streams/CopyOnWriteStream.cs` and `src/engine/Base/TemporaryStream.cs` are compiled and referenced by nothing; their removal is an arm on `debt-ws12-hygiene-remainder` and they are already excluded from the coverage report's area rollup. Do not write tests for them.
- **`StreamUtility.StreamsEqual` has exactly one caller in the whole repository**, `src/windows/DAE.Client.Controls/DBFileForm.cs` line 537, which is WinForms code no test in the suite reaches. Nothing the engine does calls it: large-value equality in D4 goes through `BinaryEqualNode`'s own byte-by-byte loop (`src/engine/DAE/Runtime.Instructions/BinaryNodes.cs`, from line 68), not through it. It is one of the reasons `StreamUtility.cs` reads 22.1 %, and it is not this ticket's to cover - say so in the handoff so the next coverage pass does not re-file it.
- **An existing tripwire inside this area.** `src/engine/Base/StreamUtility.cs` line 122 carries a `NOTE:` recording that this and the other call sites warning CA2022 flags assume one `Stream.Read` fills the buffer - true for the memory, file and engine streams they read today, and only wrong for a stream that returns partial reads. The streams these fixtures exercise are exactly the ones the note says are safe, so nothing here trips it. Do not re-open it.

## Verification

Build first (`dotnet build Dataphor.slnx`); a build error is never left for CI. Then the targeted set for this change - the two new fixtures and the conveyor fixture next to them:

```bash
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ServerStreamManagerTest|FullyQualifiedName~ConveyorTest|FullyQualifiedName~JSONObjectConveyorTest" --logger "console;verbosity=normal"
```

Nothing here changes engine source, the build, the coverage runner or the manifest format, so the three-part full run is CI's job. Name the filters you used in the handoff.

Optional and worth doing once, to say what the change bought: collect coverage over that filter (`--collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/streams`, arguments before the `--`) and run `python tools/coverage-report/coverage_report.py artifacts/coverage`. Report the Streams area's before and after numbers in the handoff; do not update `docs/coverage-report.md`, which records whole-suite runs.

## TODO

- Read `TestStreamManagerNode` (lines 503 to 630 of `tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs`) and list its assertions before writing anything, so the port can be checked against the list rather than against a reading of the new code.
- Add `tests/DAE.Server.Tests/ServerStreamManagerTest.cs`: construct a `ServerStreamManager` directly, port the assertions in order, and assert `Count()` returns to its starting value in each test. Add its `Compile` entry to `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` (that project lists its files explicitly; a new file without an entry is silently not compiled).
- Add `tests/DAE.Server.Tests/ConveyorTest.cs`: one round trip per conveyor, boundary values, at offset zero and at a non-zero offset. Add its `Compile` entry.
- Settle the deallocate-while-open question above and record the answer, in a `NOTE:` at the site if it is a tripwire or in a new ticket if it is a defect.
- Leave `tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs`, `DiagnosticsLogic.d4` and `DAE.d4` untouched. The dead C# in that file is `build-globbing-cleanup`'s to remove and `DAE.d4` is `test-pre-3-1-scripts-policy`'s to decide on; this ticket copies assertions out of one of them and changes neither.
- Run the targeted tests above, then write the handoff: what each fixture covers, the filters you ran, anything a ported assertion revealed, and the `RemoteStream` and `StreamUtility` notes above so the next pass does not re-derive them.
