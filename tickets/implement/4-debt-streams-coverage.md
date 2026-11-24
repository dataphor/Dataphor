description: The engine holds large values such as pictures and documents in managed streams and counts the holders of each one, and almost nothing checks that bookkeeping; add a language-level test that puts such values through the operations an application performs and asserts the engine is holding nothing extra afterwards.
prereq: debt-restore-stream-and-conveyor-tests, bug-assigning-a-binary-variable-leaks-its-stream, bug-binary-write-accessor-keeps-the-old-values-tail, bug-an-operator-argument-is-released-by-the-wrong-owner, bug-an-error-inside-a-block-leaks-its-binary-values
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/, tests/DAE.Server.Tests/coverage-manifest.json, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/libraries/TestFramework.Coverage.Base/Documents/DiagnosticsLogic.d4, src/engine/DAE/Streams/ServerStreamManager.cs, src/engine/DAE/Runtime.Instructions/BinaryNodes.cs, src/engine/DAE/Runtime.Data/ScalarValue.cs
difficulty: medium
----
## What this is

The last arm of the Streams coverage work. `debt-restore-stream-and-conveyor-tests` covers the stream manager and the conveyors from C#, at the level of the classes themselves. This one covers what the **D4 language** reaches: it adds one coverage script that puts stream-backed values through the operations an application performs and asserts that the server is holding no streams it should not be.

The fourth prerequisite, `bug-an-operator-argument-is-released-by-the-wrong-owner`, is the reason to wait rather than write around it: until it lands, calling an operator with a freshly built stream-backed value - `F(Binary('0001'))`, and most declarations end up in the shape that leaks - holds one stream for the life of the session. A script written while that is true either avoids the commonest way an application passes such a value or bakes the leak into its expected counts.

The area read 40.7 % of 1,129 coverable lines on the full instrumented run of 2026-11-21 at commit `f6200026` (`docs/coverage-report.md`). What a script buys on top of the C# fixtures is the engine's own plumbing around the stream manager - the branches in `BinaryNodes.cs` and `SystemNodes.cs` that take a stream identifier rather than a byte array, and the value-ownership path that decides when a stream is referenced and when it is released.

## The instrument, and what it is worth

`Diagnostics.StreamCount()` returns the number of managed streams the server is holding. It is declared in `src/engine/DAE/Schema/SystemCatalog.d4` (around line 818), resolves unqualified inside a coverage script - `CursorLibrary.d4` already does `select StreamCount();` and passes - and in a server with nothing running it is **0**.

`TestFramework.Coverage.Base` carries the helper the 3.1 authors wrote for it, `TestStreamCount(const AStatement : String)`, in `Documents/DiagnosticsLogic.d4` around line 198: it records the count, `Execute`s the statement text it was given, and raises if the count changed. The coverage template registers that library, so a script can use it without a `register` entry.

`StreamOpensAsString()` is **not** available: `SystemStreamsOpenNode` exists in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs` but `SystemCatalog.d4` registers no `StreamsOpen` operator, so `StreamCount()` is the whole of the D4-visible surface. Do not add the registration to make a nicer assertion possible - that is a change to the system catalog, which is a compatibility surface.

## What D4 actually reaches - measured, not assumed

Verified 2026-11-23 against the current `modernization` head, through a throwaway NUnit fixture on `InProcessTestFixture` that evaluated `StreamCount()` between D4 statements (built, run, then removed again - the fixture is not in the tree). The numbers are what `StreamCount()` returned, in a session with nothing else running:

| D4 construct | streams held |
|---|---:|
| nothing running | 0 |
| one `Binary` or `Graphic` variable in scope with a value | 1 |
| a second variable initialised from the first | 2 |
| three variables with distinct values | 3 |
| a row variable with one `Binary` column | 1 |
| a 21,600-byte value in a variable | 1 |
| a variable holding `nil` | 0 |
| a `Binary` read out of a table column into a variable | 0 |
| inserting, copying between rows, updating or deleting rows with a `Binary` column | 0 |
| writing through `.Value` on a variable that was initialised from another | unchanged |

Two of those lines decide the shape of the script:

**A `Binary` or `Graphic` table column never holds a managed stream.** The memory device stores the bytes natively, so inserting, copying, updating and deleting rows do not move the count at all. The original plan for this ticket assumed rows were the lever for "sharing and release"; they are not. **Variables are.** A declaration takes out a stream, a copy takes out a second one referencing the first, leaving scope gives them back.

**Writing through a representation reaches the copy-on-write path, and it works.** `LB2.Value := '<base64>'` on a variable that was initialised from another leaves the count unchanged - the engine copies the referencing stream off its source in place (`ServerStream.Write` calls `ServerStreamManager.Change`) - and the other holders keep the value they had. Measured with a chain of three: writing to the middle one left the first and third reading the original.

## What the script should do

One new script in `tests/libraries/TestFramework.Coverage.Scripts/Documents/`. **Do not touch `StreamAllocation.d4`**: it is recorded in the manifest as a known failure (`Object "Coverage" not found`) and `test-pre-3-1-scripts-policy` owns its fate. Its 2004-vintage request is still visible at `tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4` line 73, carrying the comment `// TODO: Doesn't run 1/17/2004`; this script is the answer to it, under a new name.

Each case below is a block that records `StreamCount()`, does its work, and asserts the count is back where it started - or at the value the table above says it should be, where the point is that a stream *is* held. Raise `Error` with a message naming the case on failure, as the library's other helpers do.

- **Round trip.** A value put into a variable and read back with `ToBase64String` is the same value, and `Length` reports the same length. Both matter: a stream read that stops early gives a shorter value rather than an error, so `Length` catches what `ToBase64String` alone could miss.
- **Sizes.** A few bytes, a few kilobytes, and something large enough to be worth streaming - the measurement above used a 21,600-byte value built by concatenating a base-64 fragment in a loop. The point is to catch a path that only runs for one size.
- **Sharing.** Initialise a second variable from the first and assert the count is 2 and the two compare equal. This is the engine's reference path (`DataValue.CopyValue` turns a stream identifier into a `StreamManager.Reference`).
- **Copy-on-write.** Write through `.Value` on the copy; assert the copy has the new value, the original still has the old one, and the count has not moved. Then the same over a chain of three, writing to the middle. This arm reads back correctly only once `bug-binary-write-accessor-keeps-the-old-values-tail` has landed - before that fix, writing a shorter value leaves the tail of the old one behind.
- **Release.** Assert the count is back to its starting value once the blocks above have ended, and use `TestStreamCount` around at least one whole statement so the helper the library already carries is exercised rather than left dead.
- **Rollback.** A value in a variable that is alive across `BeginTransaction()`, an insert, and `RollbackTransaction()` survives with its value intact, and the count afterwards is what it was before the transaction. Note the syntax: transactions in D4 are the operators `BeginTransaction()`, `CommitTransaction()` and `RollbackTransaction()`, not a `begin transaction` statement - a block containing the latter is a syntax error.
- **Equality.** Two large values that differ only in their last byte compare unequal, and a large value compares equal to itself. Measured working today, and this is the branch of `BinaryEqualNode` that opens both streams and walks them a byte at a time (`src/engine/DAE/Runtime.Instructions/BinaryNodes.cs`, from line 93) rather than the byte-array fast path above it.
- **Nil.** A variable holding `nil` holds no stream, and `IsNil` says so.
- **`Graphic` as well as `Binary`.** Both are stream-backed, share the same conveyor and the same accessor nodes, and behave identically in every case measured. One pass of the cases above over `Graphic` is enough; it need not be the full set.

Assert on values read back and on counts, **never on stream identifiers**: those are allocation order and would make the script break the moment an earlier statement allocates one more stream.

## The one open question to settle, and how

`src/engine/DAE/Streams/RemoteStream.cs` (41.7 % of 36 lines) is the client's half of a managed stream, reached when a client receives a stream identifier in a row and opens it over the wire. The original plan for this ticket assumed a large `Binary` read through a real client connection would reach it. The measurement above casts doubt on that: rows hold the bytes natively, so a row marshalled to a client may never carry a stream identifier at all.

Settle it by reading, not by writing a speculative test. `RowValue.AsPhysical` and `ReadFromPhysical` (`src/engine/DAE/Runtime.Data/RowValue.cs`) have a "non-native standard value" case that writes a stream identifier when streams are not expanded; the callers are `src/engine/Server/Server/RemoteServerCursor.cs`, `RemoteServerPlan.cs`, `RemoteServerProcess.cs` and `src/protocol/Client/Server/LocalCursor.cs`. Determine whether any of them can hand a client a row with a stream identifier in it.

- If one can, add **one** case on `ClientStackTestFixture` (which stands the shipped client stack up in this process) that reaches it, and say which path it takes. Not a second script.
- If none can, say so in the handoff with the reason, and record it as a `NOTE:` at the top of `RemoteStream.cs` so the next coverage pass does not re-derive it. Do not contrive a case to raise a number.

## Edge cases & interactions

- **A new `.d4` file becomes a parser corpus case with no code change.** `ParserRoundTripCorpusTest` runs one case per `.d4` file in the repository - it parses the file, emits the parse tree, re-parses the emitted text and asserts the two trees are structurally equal. A construct the emitter cannot round-trip fails that test, not the coverage run. Run it against the new script and keep the script to constructs that round-trip; if one does not, prefer rewriting the script over adding an `AllowList` entry, and if an entry is genuinely needed, say in the handoff which construct and why.
- **The script needs a manifest entry, in the right place.** `tests/DAE.Server.Tests/coverage-manifest.json` is embedded in the test assembly and entries are sorted by script name in ordinal order; a script with no entry fails the run, as does an entry with no script. The entry is `"status": "pass"` - this script is new, so it has no excuse to be anything else.
- **No `.d4l` change is needed.** `tests/libraries/TestFramework.Coverage.Scripts/TestFramework.Coverage.Scripts.d4l` lists no documents, only its one library reference, so a file dropped into `Documents/` is picked up by the layout.
- **The script must not depend on what ran before it.** Each coverage script gets a fresh server copied from the run's template, and the count starts at 0 there - but assert against a recorded starting value rather than against the literal 0, so the script still says something true if the template ever registers something that holds a stream.
- **A nil `Binary` in a table column is a separate question.** Inserting `nil as Binary` into a column declared `B : Binary` was refused in the probe with `Value required for column "B"`, so if the nil case is wanted against a column rather than a variable, first establish how a nilable column of that type is declared. The variable form is enough for the stream assertion; do not spend the ticket on this.
- **The balance assertions depend on both prerequisite bug fixes.** `bug-assigning-a-binary-variable-leaks-its-stream` is why the count does not currently return to its starting value after an assignment, and `bug-binary-write-accessor-keeps-the-old-values-tail` is why the copy-on-write arm currently reads back the wrong value. If either has not landed when this ticket is worked, do not weaken the assertion and do not record the script as a known failure to get a green run - stop and say so.
- **Large values and the perf baseline.** `PerfBaselineTest` runs the coverage corpus's timing scripts, not every script, so a new script is not timed - but the coverage run's own wall clock is the thing to watch. A 21,600-byte value built by string concatenation in a D4 loop ran in well under a second; keep the large-value sizes in that range rather than reaching for megabytes, and do not add a `timeoutSeconds` to the manifest entry unless a measured run needs one.
- **`FileStreamProvider` is unreachable and stays that way.** `src/engine/Server/Streams/FileStreamProvider.cs` is 91 coverable lines at 0 %, constructed only inside `#if UseFileStreamProvider`, and that `#define` is commented out at the top of `ServerStreamManager.cs`. 91 of this area's uncovered lines can never be reached by any test. Do not try.
- **An existing tripwire inside this area.** `src/engine/Base/StreamUtility.cs` line 122 carries a `NOTE:` recording that this and the other call sites warning CA2022 flags assume one `Stream.Read` fills the buffer - true for the memory, file and engine streams they read today. A coverage script cannot trip it: the streams it exercises are exactly the ones the note says are safe. Do not re-open it.
- **The three other `*Allocation` scripts.** `LockAllocation`, `RowAllocation` and `ScalarAllocation` are recorded in the manifest as passing with the note "no assertions: the file holds only comments" - they were meant to count allocations of their own kind, and the engine's `Diagnostics.LockCount()`, `RowCount()` and `UsedRowCount()` all return a hard-coded 0 today (`src/engine/DAE/Runtime.Instructions/SystemNodes.cs`, around lines 2452 to 2485: the lock manager and row manager are gone). So the instrument this script uses has no equivalent for them, and they cannot be answered the same way. Say that in the handoff; it is the fact `test-pre-3-1-scripts-policy` needs, and it is not this ticket's to act on.

## Verification

Build first (`dotnet build Dataphor.slnx`). Then the targeted set: the new script's own case, the existing scripts that use `StreamCount()`, and the parser corpus.

```bash
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=<NewScriptName>|Name=CursorLibrary|Name=DML|FullyQualifiedName~ParserRoundTripCorpusTest" --logger "console;verbosity=normal"
```

Add the two C# fixtures from the prerequisite ticket to the filter if this change touches anything they cover. Nothing here changes engine source, the build, the coverage runner or the manifest format, so the three-part full run is CI's job. Name the filters you used in the handoff.

## TODO

- Read `tests/libraries/TestFramework.Coverage.Base/Documents/DiagnosticsLogic.d4` around line 198 for `TestStreamCount`, and `tests/libraries/TestFramework.Coverage.Scripts/Documents/CursorLibrary.d4` lines 51 and 77 for how an existing passing script uses `StreamCount()`.
- Write the new script into `tests/libraries/TestFramework.Coverage.Scripts/Documents/`, case by case in the order above, each block asserting its own count. Pick a name that is not `StreamAllocation` and that does not collide with the three `*Allocation` scripts.
- Add its entry to `tests/DAE.Server.Tests/coverage-manifest.json`, in ordinal position by script name, `"status": "pass"`.
- Settle the `RemoteStream` question by reading the physical-row callers, then either add the one `ClientStackTestFixture` case or record the `NOTE:` - whichever the reading supports.
- Run the targeted tests above, including the parser corpus, and fix the script rather than the corpus if it does not round-trip.
- Write the handoff: what the script covers, the filters you ran, the `RemoteStream` answer and where you recorded it, the `*Allocation` finding above, and the Streams area's coverage before and after if you collected it.
