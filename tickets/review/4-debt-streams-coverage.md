description: The engine holds large values such as pictures and documents in managed streams and counts the holders of each one; a new language-level test now puts such values through the operations an application performs and asserts the engine is holding nothing extra afterwards.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4, tests/DAE.Server.Tests/coverage-manifest.json, tests/DAE.Server.Tests/RemoteStreamValueTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj
difficulty: medium
----
## What landed

Three files, no engine source changed.

**`tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4`** (new, 414 lines) - the D4-level coverage script this ticket exists for, named `StreamValues` because `StreamAllocation` is taken by the 2004 script it answers and because the three `*Allocation` siblings are a different thing. It uses `Diagnostics.StreamCount()` as its instrument, records a starting count per case rather than assuming 0, and asserts on counts and read-back values only - never on stream identifiers. Its cases, in file order:

- **Round trip at four sizes** (4, 8, 3,600 and 21,600 bytes), through one helper operator the script declares and drops: the value reads back identically through both `Value` and `ToBase64String`, `Length` reports the byte count it was given, the variable holds exactly one stream while it is in scope, and none after.
- **Sharing**: a variable initialised from another holds a second stream and compares equal to it; three variables with distinct values hold three streams and compare pairwise unequal.
- **Copy-on-write, three directions**: through the copy, through the middle of a chain of three, and through the original. Each asserts the written variable reads the new value, every other holder reads the value it was taken from, and the count does not move. The last of the three is the direction that did nothing at all before `bug-writing-a-large-value-changes-copies-of-it`; all three write a shorter value over a longer one, which is the direction `bug-binary-write-accessor-keeps-the-old-values-tail` got wrong.
- **Release through the library's own helper**: `TestStreamCount` from `TestFramework.Coverage.Base/Documents/DiagnosticsLogic.d4`, given one whole statement that declares, copies and writes. That helper was dead before this.
- **Rows, and a rolled-back transaction**, in one section with one table created and dropped inside it. See the gap below.
- **Equality over 21,600-byte values**: two values differing only in their last byte compare unequal, and a value compares equal to itself. This is the branch of `BinaryEqualNode` that opens both streams and walks them byte by byte; the fast path above it only runs for two native byte arrays, which a selected value never is.
- **Nil**: a `Binary` variable with no value holds no stream and `IsNil` says so; giving it a value takes one out.
- **Graphic**: one pass over declaration, round trip, sharing, write-through-the-original and nil.
- **A closing balance assertion** against the count read at the top of the file.

**`tests/DAE.Server.Tests/coverage-manifest.json`** - one entry, `{ "script": "StreamValues", "status": "pass", "note": ... }`, in ordinal position between `StreamAllocation` and `StringLibrary`. No preamble, no `register`, no `timeoutSeconds`: the script needs none (2 s of case time).

**`tests/DAE.Server.Tests/RemoteStreamValueTest.cs`** (new) - the one `ClientStackTestFixture` case the `RemoteStream` question called for, plus its `Compile Include` in the project file.

## The `RemoteStream` answer

**Yes, a client can be handed a stream identifier, and `RemoteServerExpressionPlan.Evaluate` is the caller that does it** - and not only for a row. `DataValue.AsPhysical` always writes with `expandStreams: false`, and `ScalarValue.WriteToPhysical` then writes the identifier for any value that is not native (`src/engine/DAE/Runtime.Data/ScalarValue.cs` around line 787). So evaluating a bare `Binary('...')` through the client stack hands the client an identifier; `LocalExpressionPlan.Evaluate` rebuilds the value against the client's own value manager, whose stream manager is a `LocalStreamManager`, and opening it builds a `LocalStream` whose only kind of source is a `RemoteStreamWrapper`.

The ticket's doubt about rows was well founded and is also now settled: `RowValue.WriteToPhysical` makes the same decision in the same place for a non-native column, but a `Binary` column on the in-memory device is stored natively, so a row marshalled out of a table carries bytes rather than an identifier. A scalar expression is therefore both the shortest way to the path and the only one that reaches it without a device that keeps its values in streams. Hence one case, not two, and a scalar rather than a row.

Measured rather than argued: collecting coverage for that one case alone reports 12 of the 36 coverable lines of `src/engine/DAE/Streams/RemoteStream.cs` hit (the constructor, the capability properties, `Length`, `Position`, `Read`, `ReadByte` and `Close`), where the file's whole-suite figure before this was 15 of 36. No `NOTE:` was added to `RemoteStream.cs`, because the reading supported the case rather than ruling it out.

## The `*Allocation` finding the policy ticket needs

`LockAllocation`, `RowAllocation` and `ScalarAllocation` cannot be answered the way `StreamValues` answers `StreamAllocation`. The instrument does not exist: `Diagnostics.LockCount()`, `Diagnostics.RowCount()` and `Diagnostics.UsedRowCount()` all return a hard-coded 0 today (`src/engine/DAE/Runtime.Instructions/SystemNodes.cs`, around lines 2452-2485) because the lock manager and row manager the 3.1 engine had are gone. So those three scripts have nothing to assert against, and their manifest note ("no assertions: the file holds only comments") is the whole truth about them. That is recorded in the new script's header comment as well as here; it is the fact `test-pre-3-1-scripts-policy` needs and it is not this ticket's to act on. `StreamAllocation` itself was not touched.

## Tests added, and what each verifies

| Test | What it verifies |
|---|---|
| `CoverageScripts.StreamValues` (the new `.d4` and its manifest entry) | every case listed above - the engine's D4-visible stream plumbing: the selector and `Value` accessor nodes, `BinaryLengthNode`, `BinaryToBase64StringNode`, both branches of `BinaryEqualNode`, the value-ownership path that references and releases a stream, and the copy-on-write separation in all three directions |
| `ParserRoundTripCorpusTest` case for `StreamValues.d4` | free, with no code change: the new file parses, re-emits and re-parses to the same tree. It passed first time, so no `AllowList` entry was needed and nothing in the script had to be rewritten for the emitter |
| `RemoteStreamValueTest.AClientOpensAStreamBackedValueThroughItsOwnStreamManager` | a client evaluating a stream-backed expression is handed the stream identifier rather than the bytes (the load-bearing assertion - a value marshalled inline would be native and no stream would be opened at all), reads it through its own stream manager rather than the engine's, and gets the right bytes back |

No existing test was changed and no expected value in any existing D4 script was edited.

## Gaps to look at, honestly

**The one case the script deliberately does not assert.** Writing it turned up a real leak, now filed as `fix/4.7-bug-a-stored-binary-is-never-given-back` with a verified reproduction: a committed insert of a stream-backed value into a table column raises the stream count by one per row and keeps it until the table is dropped - deleting the row does not give it back, and an update takes out another. The script's row section therefore asserts only what is settled (a column read back into a variable costs no stream; an update and a delete reach the row; a rolled-back insert costs nothing and leaves the variable intact) and creates and drops its own table inside the section, so that the section balances at its end and cannot quietly raise the floor under the cases after it. The reason is spelled out in a comment at the site, naming the ticket. **This is the judgement most worth a second opinion**: the alternatives were to fail the whole script, to record it as a known failure, or to write the leak into the script as an expected count - the project rules forbid the last two and the ticket forbids weakening an assertion, so the arm was narrowed to what is true today *and* will still be true after the fix. If you disagree, the committed-insert balance belongs exactly where the comment says, and should be added to this script when that ticket lands.

**`<>` is written `not(A = B)` throughout, and that is not style.** In a server with the shared SQL-device library registered and the SQL Server one not - which the coverage template is - `<>` on two `Binary` values does not run at all: it resolves through `MSSQLBinary like System.Binary` to an `iNotEqual` that `libraries/SQLDevice/Documents/Register.d4` declares over a class in the `MSSQLDevice` assembly, and fails with `Class alias "MSSQLDevice.MSSQLMSSQLBinaryNotEqualNode" not found`. Filed as `backlog/bug-binary-comparison-needs-the-mssql-device` (verified, with the four shapes a fix could take and what each costs), and the script carries a comment at the first comparison saying why the form is what it is. `=` is unaffected.

**What the script does not cover, and why.** `src/engine/Server/Streams/FileStreamProvider.cs` - 91 coverable lines at 0 % - is constructed only inside `#if UseFileStreamProvider`, whose `#define` is commented out at the top of `ServerStreamManager.cs`; no test can reach it and none should try. The `CopyOnWriteStream` class (57 lines, 0 %) is likewise unreferenced from any live path. `StreamsOpenAsString()` was not added to the system catalog, as the ticket instructed. The nil case is asserted over a variable and not over a table column: inserting `nil as Binary` into a column declared `B : Binary` is refused with `Value required for column "B"`, so that would first need a settled way to declare a nilable column of that type, which the ticket said not to spend itself on.

**Where a reviewer should be sceptical.** The script's claim that a `Binary` read out of a table column into a variable takes out no stream rests on two independent measurements (the ticket's, 2026-11-23, and mine, 2026-11-24) and is asserted by the script itself - but it sits oddly beside the fact that dropping the table releases the streams the inserts held, which says the stored rows *do* hold identifiers. Those two facts are not yet reconciled; `fix/4.7-bug-a-stored-binary-is-never-given-back` names the reconciliation as the first thing to do before a fix is written. Nothing in this script depends on the explanation, only on the measured numbers, but if you can see the answer it belongs in that ticket.

## Coverage

Not a full instrumented run - CI owns that. For the record, measured on this build from the targeted set below with `--collect:"Code Coverage;Format=cobertura"` and the project's own report tool: the **Streams** area at **56.1 % of 1,147 coverable lines**, against **40.7 % of 1,129** on the full instrumented run of 2026-11-21 at `f6200026` recorded in `docs/coverage-report.md`. Those two numbers are not strictly comparable - different build, and mine is a subset of the suite rather than all of it - but a subset can only undercount, so the area is now above 56 % and the gap verdict has moved from *thin* to *covered*. Per file, from that run: `ServerStreamManager.cs` 65.3 %, `Conveyor.cs` 92.4 %, `MemoryStreamProvider.cs` 95.8 %, `ServerStream.cs` 86.4 %, `ManagedStream.cs` 100 %, `CoverStream.cs` 60.7 %, `RemoteStream.cs` 33.3 %, `LocalStream.cs` 100 %, `LocalStreamManager.cs` 41.9 %, `FileStreamProvider.cs` and `CopyOnWriteStream.cs` 0 %. `docs/coverage-report.md` was not edited, since no full run was done to regenerate it.

## What I ran

`dotnet build Dataphor.slnx -c Debug` - 0 errors (143 warnings on the full rebuild, all pre-existing).

The targeted set, from `tests/DAE.Server.Tests --no-build -c Debug`:

```
--filter "Name=StreamValues|Name=StreamAllocation|Name=LockAllocation|Name=RowAllocation|Name=ScalarAllocation|Name=CursorLibrary|Name=DML|FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~RemoteStreamValueTest|FullyQualifiedName~ServerStreamManagerTest|FullyQualifiedName~StreamBackedRepresentationWriteTest|FullyQualifiedName~ConveyorTest"
```

437 tests, 417 passed, 20 skipped, 0 failed, 43 s. The 20 skipped are `StreamAllocation` at its recorded `known-failure` state plus the 19 parked-tree parser cases the corpus Ignores by design. Why this filter: the new script's own case; the three sibling `*Allocation` scripts and the two existing scripts that call `StreamCount()` (`CursorLibrary`, `DML`), so a change in what the instrument reports shows up; the whole parser corpus, because the new `.d4` becomes a case in it; and the Streams area's own C# fixtures from `debt-restore-stream-and-conveyor-tests` and the two write/ownership fixtures, because the new client-stack case touches the client half of the stream stack. The log is at `tickets/.logs/4-debt-streams-coverage.test.log`.

The three-part full run was not needed: no engine source, no build file, no test infrastructure, no coverage runner and no manifest-format change - one new D4 document, one manifest entry, one test file and its project entry. CI runs the whole suite in Debug and Release on the push.

No pre-existing test failure surfaced, so no `tickets/.pre-existing-error.md` was written.

## Compatibility

Nothing application-visible changed: no D4 semantics or operator signatures, no registered class names, no assembly names or namespaces, no file formats, no error codes, no contract shapes, no ports, no on-disk layout. No system-catalog change - in particular `StreamsOpen` was deliberately not registered. No entry in `docs/migration-notes.md` is owed, and no page under `docs/architecture/` covers the stream manager, so there was nothing to bring up to date there either.
