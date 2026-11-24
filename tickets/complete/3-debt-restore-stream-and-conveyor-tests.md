description: Two tests the original product shipped - one for how the engine shares and releases large values such as pictures and documents, one for how it turns each kind of value into bytes and back - had been switched off for years; they are back as ordinary tests, and bringing them back found a defect in how the engine shares those values.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/ServerStreamManagerTest.cs, tests/DAE.Server.Tests/ConveyorTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, src/engine/DAE/Streams/ServerStreamManager.cs, src/engine/Base/StreamUtility.cs
----
## What landed

Two NUnit fixtures in `tests/DAE.Server.Tests`, each driving its class directly with no server, plus their `Compile` entries in `DAE.Server.Tests.csproj`:

- `ServerStreamManagerTest.cs` - the assertions of 3.1's diagnostic operator `TestStreamManager()`, ported. 8 tests, one of them `[Ignore]`d as the reproduction of a defect the port found (below).
- `ConveyorTest.cs` - the assertions of 3.1's `TestConveyors()`, ported, as 23 cases over two data-driven tests: every conveyor in `src/engine/DAE/Streams/Conveyor.cs` at the boundary values of its type.

Nothing in `src/` changed behaviour. The engine diff is three comments: a `NOTE:` above `ServerStreamManager.Deallocate` recording the deallocate-while-open answer, and two corrections the review made to existing comments.

`tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs`, `DiagnosticsLogic.d4` and `DAE.d4` were left untouched, as the implement ticket asked - 3.1's C# stays where it is as the record of what was ported.

The implement stage is commit `ticket(implement): debt-restore-stream-and-conveyor-tests`. The review added one test and corrected two comments; everything else in that commit stands.

## The defect the port found

3.1 asserted that with a stream written, two references taken from it, and then a write through the **source**, the untouched reference still reads the value it was taken from. It does not: it reads the source's new value.

```
Expected: "This is the value for stream 1"
But was:  "This is the new value for stream 1"
```

`ServerStreamManager.Change` separates a stream from its source when the **reference** is written and does nothing when the **source** is written while references are outstanding, so the write lands in the provider's stream every reference is still reading through. Filed as `bug-writing-a-large-value-changes-copies-of-it` (now in `tickets/fix/`), root cause at that one site, with the two candidate fix shapes and the D4 route marked inferred rather than verified.

The assertion is left exactly as 3.1 wrote it. Its test carries `[Ignore]` naming that ticket, so every run prints the reason; the fix removes the `[Ignore]`.

## Review findings

Read the implement diff first with no reference to the handoff, then `ServerStreamManager`, `ServerStream`, `CoverStream`, `ManagedStream`, `StreamBase`, `Conveyor.cs` and 3.1's two original test methods in full; then the handoff, then the filed bug ticket. Three claims were checked by running code rather than reading: the defect, the deallocate-while-open outcome, and whether each ignored assertion is actually broken.

### Checked and sound

- **The defect is real and the ticket describes it exactly.** Removing the `[Ignore]`, rebuilding and running the fixture fails on precisely the untouched-reference assertion with precisely the strings the ticket quotes, and on nothing else. The attribute was then restored and the tree confirmed clean. The static reading behind it holds too: `Change` has one `if` and it tests `_referencingHeaders` for *the stream being written*, so the source-with-references case has no branch at all.
- **The `[Ignore]` is the right call, and it is not a buried failure.** The alternative is a permanently red CI, which hides every real failure after it. The repository's rule against skipping is a rule against burying a defect: here the assertion is unweakened, the reason string names the ticket, the ticket is in `fix/` - the top-priority stage, so it runs ahead of the rest of the streams work - and it carries the repro's path and the instruction not to weaken it. Nothing about the finding depends on anyone reading this ticket.
- **The port is faithful to 3.1, assertion for assertion.** Walked both original methods line by line against the new fixtures. Every value assertion 3.1 makes is carried, with its payload strings; the structure of the reference chain is reproduced including the detail that makes it meaningful - 3.1's "stream 3" is written through *before* the chain below it is taken, so it holds a provider stream of its own and `Deallocate` has to re-parent rather than just unlink. The one deliberate difference is the close count: 3.1 closed twice per open because its stream did not close itself, and today `ManagedStream.Close` calls the manager, so the port closes once. Same intent against a different API.
- **No conveyor was dropped.** 3.1 also exercised `DateConveyor` and `TimeConveyor`, which the handoff does not mention; both classes are gone from the engine (D4's `Date` and `Time` go through `DateTimeConveyor` now), so there is nothing to port. Against today's `Conveyor.cs` the fixture's claim to cover every conveyor in the file is exact - all twelve, with `JSONObjectConveyor` and `VersionNumberConveyor` excluded for the stated reasons.
- **The BOP case asserts something.** `Schema.LibraryReference.Equals` compares name *and* version, so the round trip through `BOPObjectConveyor` would fail if the version were lost - it is not a reference-equality tautology.
- **The deallocate-while-open `NOTE:` is a tripwire, not a demoted defect.** Verified by running the pinned test and by reading every `Deallocate` call site: the engine's own paths close first, the one that does not track ownership is behind an `#if` nothing defines, and the client stack's `LocalStreamManager.Deallocate` closes its local stream first too. The outcome is loud rather than a read of freed state. Comment corrected, below.
- **Source hygiene.** Tabs, CRLF and the UTF-8 byte-order mark preserved in every file edited; no file reformatted or re-encoded. `ServerStreamManagerTest.cs` is 376 lines and `ConveyorTest.cs` 144 after this pass, both well inside what this test project carries. The comments say why rather than what - the faithfulness record, why a manager per test makes `Count()` exact, why the guid's bytes are all distinct - which is the kind this review wants kept.
- **No docs were owed and none are stale.** Checked `docs/modernization-plan.md` (WS3 carries no work item for this ticket - it was filed out of `debt-streams-coverage`'s planning, and the plan names neither), `docs/coverage-report.md` (a dated whole-suite snapshot; leaving it alone is right, and its `debt-streams-coverage` row is still the number that run measured) and `docs/migration-notes.md` (nothing public changed, so nothing is owed). No page under `docs/architecture/` covers stream reference bookkeeping, so the size check had nothing to run against.

### Fixed in this pass

- **A passing assertion was parked along with the broken one.** The ignored test carries three of 3.1's assertions in one method, and one of them - a source with references outstanding reads back its own write - passes today and is pinned nowhere else. It is also the assertion most at risk from the fix: the re-parenting shape the bug ticket offers hands the provider's stream to one of the references, which leaves the source's own open stream to be pointed somewhere. So it was the worst one to have switched off while that fix is written. Added `WritingASourceThatHasReferencesLeavesItReadingItsOwnNewValue`, which asserts only that, and noted in `bug-writing-a-large-value-changes-copies-of-it` that the fix must keep it green. The third assertion - a written reference reads back its own value - is already covered by `WritingThroughAReferenceLeavesTheSourceWithItsOwnValue`, so nothing that currently passes is unpinned now.
- **The deallocate `NOTE:` overstated its own justification.** It said "every call site closes first, so nothing reaches that today", which is true of the engine but not of `DataphorService.DeallocateStream`, the operation a remote client calls: a client that deallocates while holding an open stream handle does reach it. The conclusion is unchanged - it gets a fault, not a read of freed state - but the load-bearing claim now says which call sites it is about and names the one path that is not one of them.
- **An existing inventory of a known class was missing a site this ticket now covers.** The `NOTE:` at `src/engine/Base/StreamUtility.cs:122` records the project's position on the `CA2022` "one `Stream.Read` fills the buffer" assumption and enumerates the sites that make it. `BinaryConveyor.Read` makes it too - the compiler flags it, at `Conveyor.cs:495` - and was not in the list, so a later sweep of that class would have missed it. Added to the enumeration. The site itself is as safe as the ones already blessed there: the streams it is handed are the whole-value streams `ScalarValue` opens, and the client stack's `DeferredWriteStream.Read` delegates to a local `MemoryStream`. No behaviour changed.

### Tripwires (parked as comments, not tickets)

- Deallocating a stream that is still open, as corrected above: `NOTE:` at `ServerStreamManager.Deallocate`.
- `BinaryConveyor.Read`'s inexact read: added to the existing `NOTE:` at `StreamUtility.cs:122` rather than given one of its own, so the class stays one greppable set with one revisit condition.

### Found and deliberately not filed

- **`ConveyorTest`'s byte-buffer cases overlap `InternalCoverageTests.TestByteArrayUtility`**, which already round-trips `Int16`, `Int32`, `Int64`, `Decimal`, `Guid` and `String` at offsets through `ByteArrayUtility` - the utility those conveyors are one-line delegations to. Kept, because the conveyor test asserts something the utility test does not: that `GetSize` agrees with the footprint `Write` actually touches, which is the contract the engine relies on when it packs a whole row into one buffer, and that sentinel check is what would catch a conveyor writing past its size. Recorded here so the next coverage pass does not read the two as duplicates and cut the wrong one.
- **Nothing here is a concurrency test**, and the fixture says so. Every public method of `ServerStreamManager` is `lock (this)` and every test drives it from one thread. A concurrent check would be a new claim about the class rather than part of restoring 3.1's, so none was added and none is claimed.
- **These tests say nothing about stream truncation.** Both helpers go through `StringConveyor`, whose length prefix makes a stale tail past the written length invisible. That is what 3.1 did, it is deliberate, and `bug-binary-write-accessor-keeps-the-old-values-tail` owns stale tails.
- No ticket was filed for anything in this pass. The one defect worth a ticket was already filed by the implement stage at the right site, and nothing else found reached the filing bar: the two comment corrections and the one missing test were cheaper to fix here than to describe. The adjacent `bug-assigning-a-binary-variable-leaks-its-stream`, already in `fix/`, was read to check it was not the same root cause - it is not: its site is stack-slot ownership in `ImperativeNodes.cs`, not `Change`.

### No tests were cut

Every test in the implement diff earns its place: the stream-manager cases reach `Change`, `MoveReferences` and the re-parenting inside `Deallocate`, which nothing else in the suite reaches at all, and the conveyor cases pin the physical representation of every value type the engine stores - which, with no reference runtime, is the only record of it there is. One test was added, for the reason above.

## Verification run

Build first: `dotnet build Dataphor.slnx` - **0 errors**, no new warnings.

Then the implement stage's targeted set plus the ring the review owes - the `ByteArrayUtility` fixture underneath the byte-buffer conveyors, and the coverage scripts in the Streams neighbourhood:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ServerStreamManagerTest|FullyQualifiedName~ConveyorTest|FullyQualifiedName~InternalCoverageTests"
  -> 35 tests: 34 passed, 1 skipped (the ignored reproduction, reason printed), 6 s
     (this filter also picks up JSONObjectConveyorTest, which the implement stage named)

dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~CoverageScripts.Serialization|FullyQualifiedName~CoverageScripts.KeyAffectingUpdate|FullyQualifiedName~CoverageScripts.RowAllocation|FullyQualifiedName~CoverageScripts.ScalarAllocation"
  -> 14 cases: 11 passed, 3 known-failures matching their recorded status and ticket, 39 s
```

The three-part full run was not attempted and is not owed: this ticket changes no engine behaviour, no build file beyond two `Compile` entries, and neither the coverage runner nor the manifest. CI runs the whole suite in Debug and Release on the push.

One extra run, discarded: the `[Ignore]` removed, built, and the fixture run to see the failure first-hand; attribute restored, rebuilt, re-run green, and `git status` confirmed the tree carries only the intended files.

## Area notes carried forward

The implement handoff's per-file account of what is left uncovered in the Streams area was spot-checked and stands; it is the next coverage pass's starting point rather than this ticket's work. The short version: `DeferredWriteStream` and `RemoteStream` are client-stack paths an engine fixture cannot reach, `FileStreamProvider` is unreachable by design (`#define UseFileStreamProvider` is commented out and the manager was deliberately not made configurable to let a test reach it), `StreamUtility.StreamsEqual` has one caller and it is WinForms, what is left of `ServerStreamManager` is stream-tracing machinery that nothing switches on, and `CopyOnWriteStream`/`TemporaryStream` are dead with their removal already an arm on `debt-ws12-hygiene-remainder`. The measured move, over the three fixtures only: the Streams area goes from 6.7 % to 44.0 % of 1,129 coverable lines, `gap` to `thin`. Measurement artifacts were deleted.
