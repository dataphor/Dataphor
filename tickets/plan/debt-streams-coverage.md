description: The engine holds large values such as images and documents in managed streams and counts the references to each one, and almost nothing tests that bookkeeping - a leak or an early release would show up as a server slowly filling with memory, or as a value read back empty.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Streams/ServerStreamManager.cs, src/engine/Base/StreamUtility.cs, src/engine/DAE/Streams/Conveyor.cs, src/engine/DAE/Streams/CoverStream.cs, src/engine/DAE/Streams/StreamManager.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamAllocation.d4
tradeoffs: Nothing is known to be broken here, and the parts a D4 script can reach are the parts already partly covered, so a maintainer may see this as insurance rather than a fix; against that, a stream leak is exactly the kind of defect that only shows up after a server has been running for a week.
----
## The area

**Streams**, `src/engine/DAE/Streams/`, `src/engine/Server/Streams/`, plus two files in `src/engine/Base`. Measured **40.7 % line coverage of 1,129 coverable lines, 669 uncovered**, on the full three-part run of 2026-11-21 at commit `f6200026` (`docs/coverage-report.md`).

| File | Line coverage | Coverable |
|---|---:|---:|
| `src/engine/DAE/Streams/ServerStreamManager.cs` | 45.3 % | 311 |
| `src/engine/Base/StreamUtility.cs` | 22.1 % | 163 |
| `src/engine/DAE/Streams/Conveyor.cs` | 29.5 % | 132 |
| `src/engine/Server/Streams/FileStreamProvider.cs` | 0.0 % | 91 |
| `src/engine/DAE/Streams/CoverStream.cs` | 41.6 % | 89 |
| `src/engine/DAE/Streams/StreamManager.cs` | 34.8 % | 46 |
| `src/engine/DAE/Streams/RemoteStream.cs` | 41.7 % | 36 |

Two things to know before reading those numbers, both established by the pass that filed this:

- **91 of the 669 uncovered lines cannot be reached at all.** `FileStreamProvider` is only ever constructed inside `#if UseFileStreamProvider`, and that `#define` is commented out at the top of `ServerStreamManager.cs`. No test can cover it and none should try. Whether the engine still wants a file-backed stream provider is a design question, not a coverage one.
- Two files that were in this area are now excluded from the report: `src/engine/Base/TemporaryStream.cs` and `src/engine/DAE/Streams/CopyOnWriteStream.cs` are compiled and referenced by nothing, and their removal is an arm on `debt-ws12-hygiene-remainder`. That is why the area reads 40.7 % rather than the 35.7 % the same run gives with them counted.

So the live target is roughly 578 lines, most of it in the stream manager, the copy helpers and the conveyors.

## What the suite does and does not reach today

The engine allocates a managed stream whenever it holds a value too large to carry inline - a `Binary` or `Graphic` column, a serialized row - and the stream manager keeps a header per stream with a reference count, so a value copied between rows shares one stream and a value dropped releases it. `ServerStreamManager` has explicit allocate, reference, deallocate, open and close events and a `StreamOpensAsString()` diagnostic, which is how a leak is meant to be visible.

The 45 % on the stream manager is the allocation and read path that ordinary scripts touch in passing. What no test reaches is the bookkeeping itself: nothing checks that the counts come back to where they started, nothing exercises the copy-on-reference path, and nothing provokes a double release or a read of a deallocated stream.

**This is what `StreamAllocation.d4` was for.** It is in the library, it is one of the scripts the triage pass recorded, and it does not run: the manifest records it as a known failure, `Object "Coverage" not found`, owned by `test-pre-3-1-scripts-policy`. Its name says it was meant to count stream allocations. A new script should do what that one meant to, and the ticket that decides the old file's fate is `test-pre-3-1-scripts-policy`, not this one - do not edit, retire or delete it here.

## What a coverage script should exercise

A D4 script through the in-process harness. `Binary` and `Graphic` are the two stream-backed scalar types, both granted to role `User`, with `Length`, `iEqual` and `ToBase64String` operators over them.

- **Round-tripping a value.** Insert `Binary` values into a table, read them back, and require equality - both with `iEqual` and by `Length`, since a stream read that stops early gives a shorter value rather than an error.
- **Sizes that cross the inline boundary.** A few bytes, a few kilobytes, and something large enough to be worth streaming. The point is to catch a path that only runs for one of those.
- **Sharing and release.** Copy a value from one row to another, update a row to a new value, delete a row - each changes a reference count. Then check the counts: whatever `StreamOpensAsString()` or the system tables expose is the assertion this area has been missing, and finding the D4-visible form of it is part of the work.
- **Rollback.** Allocate values inside a transaction and roll it back; the streams the aborted work allocated must not still be held. This is the leak a long-running server would actually accumulate.
- **Equality and comparison** of two large values that differ only in their last byte, which is what `StreamUtility.StreamsEqual` is for and what its 22 % suggests nothing checks.
- **The conveyors.** `Conveyor.cs` holds one physical reader/writer per system type - boolean, byte, the three integer widths, decimal, string, date-time, time-span. At 29.5 %, some are exercised and some are not. A script that stores and reads back a value of every system scalar type through a device that writes physical rows covers the set, and is a better shape than a test per conveyor.

Assert on values read back and on counts, never on stream identifiers: those are allocation order and would make the test brittle.

## Interactions

- **The three `*Allocation` scripts.** `LockAllocation`, `RowAllocation` and `ScalarAllocation` are three of the nine scripts the manifest records as asserting nothing - each holds only comments. They were meant to count allocations of their own kind. This ticket covers streams only; if a script here finds a usable D4-visible way to read allocation counts, say so in its handoff, because that is the piece the other three would need too.
- **An existing tripwire at one of these sites.** `src/engine/Base/StreamUtility.cs` line 122 carries a `NOTE:` recording that this and the other call sites flagged by warning CA2022 assume one `Stream.Read` fills the buffer - true for the memory, file and engine streams they read today, and only wrong for a stream that returns partial reads. That is not a finding this ticket re-opens, and a D4 coverage script cannot trip it: the streams it will exercise are exactly the ones the note says are safe. Worth knowing so nobody writes a script expecting to catch it.
- **One conveyor in the tree is wired wrongly, and a script here would be the place to catch the class.** `libraries/System.Internet/Source/JSONObjectConveyor.cs` overrides only the streaming `Read`/`Write` pair but leaves `IsStreaming` returning the base class's `false`, so the engine takes the byte-buffer branch and calls a `GetSize` that throws. Filed as `bug-json-conveyor-declares-itself-non-streaming` (backlog), which recommends a reflection check that every `Conveyor` subclass's overrides agree with its `IsStreaming` flag. That check belongs next to whatever this ticket writes for the conveyors, so read that ticket before designing this part; it is a different site and a different cause, not an arm of this one.
- **`RemoteStream` and `CoverStream`** are the client-side halves, reached over the wire. The out-of-process fixture exists (`OutOfProcessInstance`), so a large `Binary` value read through a real client connection is the case that covers them - worth one case, not a second script.
