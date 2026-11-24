description: Two tests the original product shipped - one for how the engine shares and releases large values such as pictures and documents, one for how it turns each kind of value into bytes and back - had been switched off for years; they are back as ordinary tests, and bringing them back found a defect in how the engine shares those values.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/ServerStreamManagerTest.cs, tests/DAE.Server.Tests/ConveyorTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, src/engine/DAE/Streams/ServerStreamManager.cs
difficulty: hard
----
## What landed

Two new NUnit fixtures in `tests/DAE.Server.Tests`, each driving its class directly with no server, plus their `Compile` entries in `DAE.Server.Tests.csproj`:

- `ServerStreamManagerTest.cs` - 3.1's `TestStreamManager()` assertions, ported.
- `ConveyorTest.cs` - 3.1's `TestConveyors()` assertions, ported.

One engine change, and it is a comment: a `NOTE:` above `ServerStreamManager.Deallocate` recording the deallocate-while-open answer (below). Nothing else in `src/` changed, and the diff there is six lines of comment - `git diff` on the engine is comment-only, which is worth confirming first since it makes most of the regression surface of this ticket nil.

`tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs`, `DiagnosticsLogic.d4` and `DAE.d4` were left untouched as the ticket asked.

## The assertion inventory, and where each one went

3.1's `TestStreamManagerNode` makes nineteen steps of assertion in one long method. Listed in its order, with the test that now carries each:

| 3.1's assertion | Now in |
| --- | --- |
| allocate, open, write, close, reopen, read back the same contents | `AStreamReadsBackWhatWasWrittenAfterACloseAndReopen` |
| a second stream on the same identifier reads the contents | `TwoStreamsOpenedOnOneIdentifierDoNotShareAPosition` |
| reading through the second stream does not move the first one's position | same |
| close twice, deallocate, manager holds nothing | same (the `Count()` assertion that ends it) |
| source written, two references taken, source and one reference written, three distinct values | `WritingASourceAndOneOfItsTwoReferencesLeavesThreeDistinctValues` - **the one that fails**, see below |
| a reference of a reference reads the value it was taken from | `AReferenceTwoDeepReadsTheValueItWasTakenFrom` |
| deallocating a stream in the middle of a chain leaves the streams below it reading the value it had | `DeallocatingTheMiddleOfAReferenceChainLeavesTheStreamsBelowItReadingItsValue` |

Two tests exist that 3.1 had no separate case for. `WritingThroughAReferenceLeavesTheSourceWithItsOwnValue` is the half of the three-value assertion that still holds, kept on its own so the working direction of the copy-on-write stays pinned while the broken one is ignored. `DeallocatingAStreamThatIsStillOpenLeavesThatStreamUnusable` answers the question the ticket asked.

Every test asserts `Count()` is back to zero before it ends - a manager per test is what makes that exact.

**One faithfulness note for the reviewer.** 3.1 closed a stream twice per open (`LStream3.Close(); LStreamManager.Close(LStreamID3);`), because in the pre-3.0 API the stream it got back did not close itself. Today `ManagedStream.Close()` calls `ServerStreamManager.Close` itself, so the port closes once per open. That is the same intent - one close per open - against a different API, not a changed assertion.

## Each test and what it verifies

`ServerStreamManagerTest`:

- `AStreamReadsBackWhatWasWrittenAfterACloseAndReopen` - a stream's contents survive the close and reopen cycle, and the manager holds nothing after the deallocate.
- `TwoStreamsOpenedOnOneIdentifierDoNotShareAPosition` - two opens on one identifier share bytes and not position (each gets its own `CoverStream`), and two opens followed by two closes and a deallocate leave the manager empty - which is what says the header's open count actually reached zero rather than being abandoned.
- `AReferenceTwoDeepReadsTheValueItWasTakenFrom` - a reference, and a reference of that reference, both read the source's value.
- `WritingThroughAReferenceLeavesTheSourceWithItsOwnValue` - the copy-on-write path: writing through a reference copies it off its source and leaves the source's value alone.
- `WritingASourceAndOneOfItsTwoReferencesLeavesThreeDistinctValues` - **`[Ignore]`d**, the reproduction of the defect below.
- `DeallocatingTheMiddleOfAReferenceChainLeavesTheStreamsBelowItReadingItsValue` - `MoveReferences` and the re-parenting inside `Deallocate`, which nothing else in the suite reaches.
- `DeallocatingAStreamThatIsStillOpenLeavesThatStreamUnusable` - the outcome of the case 3.1 never tried.

`ConveyorTest`:

- `AByteBufferConveyorRoundTripsItsValueAtAnyOffset` - 20 cases, one per conveyor per boundary value (`BooleanConveyor`, `ByteConveyor`, `Int16Conveyor`, `Int32Conveyor`, `Int64Conveyor`, `DecimalConveyor`, `StringConveyor`, `DateTimeConveyor`, `TimeSpanConveyor`, `GuidConveyor`). Each case round-trips at offset zero, then round-trips at offset 3 inside a buffer filled with a sentinel byte and asserts the sentinel is intact on both sides of the value - so a conveyor that ignored the offset, or wrote past the size it asked for, fails rather than passing by reading back its own mistake.
- `AStreamingConveyorRoundTripsItsValueThroughAStream` - 3 cases: `BinaryConveyor` empty and non-empty, and `BOPObjectConveyor` carrying a `Schema.LibraryReference`, which is one of the three D4 types actually registered to that conveyor.

No test re-checks that a conveyor's `IsStreaming` agrees with the methods it overrides; `JSONObjectConveyorTest.EveryConveyorImplementsTheHalfItsFlagClaims` owns that.

## The finding: one ported assertion fails, and it is a defect

3.1's assertion for its "stream 2" does not hold on the current engine. With a stream written, two references taken from it, and then a write through the **source**, the untouched reference reads the source's new value instead of the value it was taken from:

```
Expected: "This is the value for stream 1"
But was:  "This is the new value for stream 1"
```

`ServerStreamManager.Change` separates a stream from its source when the **reference** is written, and does nothing when the **source** is written while references are outstanding - so the write lands in the provider's stream that every reference is still reading through. Filed as **`tickets/fix/2.5-bug-writing-a-large-value-changes-copies-of-it.md`** with the root cause at that one site, the two candidate fix shapes, and the D4 route marked as inferred rather than verified.

The assertion is left exactly as 3.1 wrote it. Its test carries `[Ignore]` naming that ticket, so every run prints the reason and the suite still gives a usable signal; the fix removes the `[Ignore]`. **A reviewer who disagrees with that call should say so** - the alternative is a permanently red CI, which is why it was not chosen.

## The deallocate-while-open answer

Established by running it, and now pinned by `DeallocatingAStreamThatIsStillOpenLeavesThatStreamUnusable`: `Deallocate` ignores the header's open count, destroys the provider's entry and removes the header, so a `ManagedStream` still open on it throws `ObjectDisposedException` on its next use and `StreamsException` when closed (the header its `Close` looks for is gone).

That is loud rather than a read of freed state, nothing in the engine does it - every call site closes first - and so it is recorded as a tripwire, not a ticket: a `NOTE:` above `ServerStreamManager.Deallocate` saying that if a call site ever stops closing first, the exception from its `finally` block will mask the original and the open count is the thing to add here.

## What the change bought, measured

Collected over the three fixtures with `--collect:"Code Coverage;Format=cobertura"` and rolled up with `tools/coverage-report/coverage_report.py`. Both runs were the same filter; "before" is it with only `JSONObjectConveyorTest`, which is what the filter covered before this ticket:

- Streams area **6.7 %** (76 of 1,129 coverable lines) → **44.0 %** (497 of 1,129). Verdict moves from `gap` to `thin`.
- For scale: the whole suite managed 40.7 % of this area on the instrumented run of 2026-11-21, so these two fixtures alone now cover more of it than everything else put together did.

`docs/coverage-report.md` was deliberately not updated - it records whole-suite runs. The measurement artifacts were deleted.

## Area notes, so the next coverage pass does not re-derive them

Per-file, from the run above, biggest hole first - what is left in the Streams area and why:

- `src/engine/Base/StreamUtility.cs` **14.7 %** (24/163). `StreamUtility.StreamsEqual` has exactly one caller in the repository, `src/windows/DAE.Client.Controls/DBFileForm.cs` line 537, which is WinForms code no test reaches; large-value equality in D4 goes through `BinaryEqualNode`'s own byte loop instead. Not this ticket's to cover and not a gap worth a ticket on its own. The `NOTE:` at line 122 about `Stream.Read` filling the buffer in one call was not re-opened - the streams these fixtures exercise are the ones it says are safe.
- `src/engine/DAE/Streams/ServerStreamManager.cs` **60.5 %** (188/311). What is left is almost entirely the stream-tracing machinery - `StreamEvents`, the three event classes and their `ToString`, `StreamOpensAsString` - plus `Register`/`Unregister`/`IsLocked`. `StreamTracingEnabled` is set by nothing and no D4 operator is registered for `StreamOpensAsString`, so production never runs with it on; covering it would be covering a diagnostic nothing uses.
- `src/engine/DAE/Streams/DeferredWriteStream.cs` **0 %** (108). Reached only by `src/protocol/Client/Streams/LocalStream.cs`, which derives from it - a client-stack path, not the engine's. It belongs to whichever pass covers the client stack.
- `src/engine/Server/Streams/FileStreamProvider.cs` **0 %** (91). Unreachable by design: constructed only inside `#if UseFileStreamProvider`, whose `#define` is commented out. Not attempted, and the stream manager was not made configurable to let a test reach it.
- `src/engine/DAE/Streams/CoverStream.cs` **40.4 %** (36/89). The offset-and-count constructor and its clamping paths are what is missing; these fixtures only ever take the whole-stream cover.
- `src/engine/DAE/Streams/RemoteStream.cs` **0 %** (36). `RemoteStreamWrapper`, used from `LocalStream` and `LocalServer.GetFile` - client stack again, unreachable from an engine fixture.
- `ConveyorException.cs` and `StreamsException.cs` are generated exception plumbing; `MemoryStreamProvider.cs` is at 95.8 %, `Conveyor.cs` at 92.4 %, `ManagedStream.cs` at 100 %.
- `CopyOnWriteStream.cs` and `TemporaryStream.cs` are dead and already excluded from the area rollup; no tests were written for them. Their removal is an arm on `debt-ws12-hygiene-remainder`.
- Not covered and deliberately so: `VersionNumberConveyor` (in `Runtime.Data`, outside this area, and not in 3.1's test either).

## Verification run

Build first: `dotnet build Dataphor.slnx` - **0 errors**, and no new warnings from either new file.

Then the targeted set named in the implement ticket:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ServerStreamManagerTest|FullyQualifiedName~ConveyorTest|FullyQualifiedName~JSONObjectConveyorTest" --logger "console;verbosity=normal"
```

**33 tests: 32 passed, 1 skipped** (the ignored reproduction, with its reason printed), 7.8 s. Nothing else was run: this ticket changes no engine behaviour, no build file beyond two `Compile` entries, and neither the coverage runner nor the manifest, so the three-part full run is CI's.

## What a reviewer should look at

- **The `[Ignore]` call.** It is the one judgement in this ticket. The assertion is intact and the ticket is filed; the question is only whether a known-failing test should be red or skipped in this repository.
- **Whether `WritingASourceAndOneOfItsTwoReferencesLeavesThreeDistinctValues` is a fair reading of 3.1.** It carries all three of 3.1's value assertions in one test rather than splitting them, so ignoring it also parks two assertions that pass - both are covered by the two tests either side of it, but that is worth checking rather than taking on trust.
- **`ReadString`'s use of the length prefix.** Both helpers go through `StringConveyor`, so a stale tail past the prefixed length is invisible to these tests by construction. That is what 3.1 did too, and it is deliberate - `bug-binary-write-accessor-keeps-the-old-values-tail` owns stale tails - but it means these tests say nothing about stream truncation.
- **Nothing here is a concurrency test.** Every public method of `ServerStreamManager` is `lock (this)` and every test drives it from one thread. The port did not make a single-threaded invariant look worth a concurrent check, so none was added and none is claimed.
- `bug-writing-a-large-value-changes-copies-of-it` is in `fix/`, the top-priority stage, so it runs ahead of `debt-streams-coverage` in `implement/` without anyone wiring a `prereq:`. If a gardener would rather make that explicit, that is an edit to `debt-streams-coverage`, not to this ticket - it was left alone deliberately.
