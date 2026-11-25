description: The engine holds large values such as pictures and documents in managed streams and counts the holders of each one; a new language-level test now puts such values through the operations an application performs and asserts the engine is holding nothing extra afterwards.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4, tests/DAE.Server.Tests/coverage-manifest.json, tests/DAE.Server.Tests/RemoteStreamValueTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj
----
## What landed

Three files, no engine source changed. The implement stage is commit `ticket(implement): debt-streams-coverage`; the review changed three comments in the new script and corrected and extended one of the two tickets that stage filed. Everything else in that commit stands.

**`tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4`** (new, 419 lines) - the D4-level coverage script this ticket exists for, named `StreamValues` because `StreamAllocation` is taken by the 2004 script it answers and because the three `*Allocation` siblings are a different thing. It uses `Diagnostics.StreamCount()` as its instrument, records a starting count per case rather than assuming 0, and asserts on counts and read-back values only - never on stream identifiers. Its cases, in file order:

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

**Yes, a client can be handed a stream identifier, and `RemoteServerExpressionPlan.Evaluate` is the caller that does it** - and not only for a row. `DataValue.AsPhysical` always writes with `expandStreams: false`, and `ScalarValue.WriteToPhysical` then writes the identifier for any value that is not native. So evaluating a bare `Binary('...')` through the client stack hands the client an identifier; `LocalExpressionPlan.Evaluate` rebuilds the value against the client's own value manager, whose stream manager is a `LocalStreamManager`, and opening it builds a `LocalStream` whose only kind of source is the `RemoteStreamWrapper` that `src/engine/DAE/Streams/RemoteStream.cs` declares.

The implement ticket's doubt about rows was well founded and is also settled: `RowValue.WriteToPhysical` makes the same decision in the same place for a non-native column, but a `Binary` column on the in-memory device is stored as bytes, so a row marshalled out of a table carries bytes rather than an identifier. A scalar expression is therefore both the shortest way to the path and the only one that reaches it without a device that keeps its values in streams. Hence one case, not two, and a scalar rather than a row.

Measured rather than argued: collecting coverage for that one case alone reports 12 of the 36 coverable lines of `RemoteStream.cs` hit (the constructor, the capability properties, `Length`, `Position`, `Read`, `ReadByte` and `Close`), where the file's whole-suite figure before this was 15 of 36. No `NOTE:` was added to `RemoteStream.cs`, because the reading supported the case rather than ruling it out.

## The `*Allocation` finding the policy ticket needs

`LockAllocation`, `RowAllocation` and `ScalarAllocation` cannot be answered the way `StreamValues` answers `StreamAllocation`. The instrument does not exist: `Diagnostics.LockCount()`, `Diagnostics.RowCount()` and `Diagnostics.UsedRowCount()` all return a hard-coded 0 today (`src/engine/DAE/Runtime.Instructions/SystemNodes.cs`, lines 2470-2500) because the lock manager and row manager the 3.1 engine had are gone. So those three scripts have nothing to assert against, and their manifest note ("no assertions: the file holds only comments") is the whole truth about them. That is recorded in the new script's header comment as well as here; it is the fact `test-pre-3-1-scripts-policy` needs and it was not this ticket's to act on. `StreamAllocation` itself was not touched. Review re-read all three nodes and confirmed each returns 0, and that `SystemStreamsOpenNode` exists with no operator registered over it.

## Tickets this work filed

| Ticket | Why |
|---|---|
| `fix/bug-a-stored-binary-is-never-given-back` | a committed insert of a stream-backed value into a table column holds one stream per write until the table is dropped; verified reproduction, root cause not yet pinned |
| `backlog/bug-binary-comparison-needs-the-mssql-device` | in a server with the shared SQL-device library registered and the SQL Server one not, `<>` on two `Binary` values fails with a class-alias error instead of answering; verified |

Both are why the script is written the way it is: the row section asserts less than it could because of the first, and every `Binary` comparison in the script is written `not(A = B)` rather than `A <> B` because of the second. Each has a comment at its site in the script naming the ticket.

## Gaps left open, honestly

**The committed-insert balance is not asserted anywhere.** The script's row section asserts only what is settled (a column read back into a variable costs no stream; an update and a delete reach the row; a rolled-back insert costs nothing and leaves the variable intact) and creates and drops its own table inside the section, so the section balances at its end and cannot quietly raise the floor under the cases after it. `fix/bug-a-stored-binary-is-never-given-back` owes the reproduction, and that section is where the language-level assertion belongs once it lands. Review weighed the alternatives the implement stage rejected - failing the whole script, recording it as a known failure, or writing the leak in as an expected count - and agrees with the choice: the project rules forbid the middle two, and a narrowed arm that stays true after the fix is better than an arm the fix has to rewrite.

**What the script does not cover, and why.** `src/engine/Server/Streams/FileStreamProvider.cs` (91 coverable lines, 0 %) is constructed only inside `#if UseFileStreamProvider`, whose `#define` is commented out at the top of `ServerStreamManager.cs`; no test can reach it and none should try. `CopyOnWriteStream` (57 lines, 0 %) is likewise unreferenced from any live path and is already named on `debt-ws12-hygiene-remainder` for deletion. `StreamsOpenAsString()` was deliberately not registered in the system catalog, as the implement ticket instructed, because that is a compatibility surface. The nil case is asserted over a variable and not over a table column: inserting `nil as Binary` into a column declared `B : Binary` is refused with `Value required for column "B"`, so that would first need a settled way to declare a nilable column of that type.

## Coverage

Not a full instrumented run - CI owns that. Measured on the implement stage's build from the targeted set with `--collect:"Code Coverage;Format=cobertura"` and the project's own report tool: the **Streams** area at **56.1 % of 1,147 coverable lines**, against **40.7 % of 1,129** on the full instrumented run of 2026-11-21 at `f6200026` recorded in `docs/coverage-report.md`. Those two numbers are not strictly comparable - different build, and the second is a subset of the suite rather than all of it - but a subset can only undercount, so the area is now above 56 %. Per file, from that run: `ServerStreamManager.cs` 65.3 %, `Conveyor.cs` 92.4 %, `MemoryStreamProvider.cs` 95.8 %, `ServerStream.cs` 86.4 %, `ManagedStream.cs` 100 %, `CoverStream.cs` 60.7 %, `RemoteStream.cs` 33.3 %, `LocalStream.cs` 100 %, `LocalStreamManager.cs` 41.9 %, `FileStreamProvider.cs` and `CopyOnWriteStream.cs` 0 %. `docs/coverage-report.md` was not edited, since no full run was done to regenerate it and that document is the record of one dated run; no earlier coverage ticket has edited it either.

## Compatibility

Nothing application-visible changed: no D4 semantics or operator signatures, no registered class names, no assembly names or namespaces, no file formats, no error codes, no contract shapes, no ports, no on-disk layout. No system-catalog change. No entry in `docs/migration-notes.md` is owed, and no page under `docs/architecture/` covers the stream manager, so there was nothing to bring up to date there either - review checked `docs/architecture/value-lifetime.md`, `docs/coverage-report.md` and `docs/modernization-plan.md`, and none of them states anything this change makes untrue.

## Review findings

The implement-stage diff was read before the handoff summary. Every factual claim in the new script's header comment was checked against the engine source: the `StreamCount()` registration and the absence of a `StreamsOpen` one, the three hard-coded zeros, the `// TODO: Doesn't run 1/17/2004` line in `All.Coverage.d4`, the `Binary` and `Graphic` type declarations sharing one selector and one accessor pair, and `TestStreamCount`'s body. All held. Both filed tickets were re-read against their sites, and the site-claim grep was run for the files each names.

**The script's assertions were confirmed load-bearing, not merely passing.** A deliberate leak was injected into the runtime layout's copy of the script - one top-level `Binary` variable left in scope before the closing assertion - and the case failed with `The script as a whole left 1 stream(s) behind.`; the copy was then restored byte for byte. Three further probes were run and removed the same way, and their results are below. The working tree holds only the two intended modifications.

### Fixed in this pass

- **Six lines of the script ended in a bare LF** while the other 408 and every sibling script use CRLF - the trailing comment block about `not(A = B)` was appended with the wrong endings. Normalized; the file is now wholly CRLF, as its siblings are, and still has no byte-order mark.
- **The round-trip comment said "three sizes" over four calls** (4, 8, 3,600 and 21,600 bytes). Corrected, and it now says why 4 and 8 are both there: they are the pair the copy-on-write cases write one over the other.
- **The header asserted an explanation that the open fix ticket says is unreconciled.** It read "a `Binary` or `Graphic` *table column* stores its bytes natively, so ... no row operation ever shares a stream between two holders", which is a mechanism rather than a measurement, and it sits oddly beside the fact that dropping the table releases what the inserts held. Rewritten to state only what the cases measure, to say plainly that what the engine holds the stream for is not yet established, and to tell the reader not to infer a mechanism from those lines.

### Found and parked

- **`fix/bug-a-stored-binary-is-never-given-back` pointed at a line of enquiry the code rules out.** Its "where to look" bullet said `row.GetNativeValue(columnIndex)` yields "the stream identifier itself, not bytes". It does not: for a non-native column it goes through `ScalarValue.AsNative`, which opens the stream and reads it with `BinaryConveyor.Read` into a `byte[]`, and the `Row` indexer then stores `DataValue.CopyNative` of that, which for a `byte[]` is `Clone()` rather than `StreamManager.Reference`. Neither `Row`'s constructor nor `NativeRowIndex.DisposeKey`/`DisposeData` touches the stream manager for a `byte[]`. The bullet was corrected in place and three measurements taken here were added to the ticket: the release is bound to the one table (an unrelated create-and-drop released nothing, and `delete` of every row released nothing, while `drop table` released all of them at once); it is one stream per stream-backed *write* rather than per surviving row (insert, update, then delete left three held, and the drop gave back three); and inserting from a variable and then writing that variable leaves the stored value unchanged, so the leak is retention rather than a sharing defect a user could see - with the caveat that copy-on-write would read the same way if the identifiers were in fact shared. Net effect: the ticket now states a contradiction to resolve rather than a conclusion to confirm, which is what its own reproduction section asked for. A cross-reference to `debt-memory-device-transaction-isolation`, the other open ticket over the same three files, was added so neither is worked in ignorance of the other.
- **Tripwire, recorded at the site.** The large values are built by concatenating onto one growing string 1,199 times, so the build cost is quadratic in the fragment count; the whole script runs in about 2 s today. A `NOTE:` above `LLargeFragments` says to build by doubling rather than raising that count if a larger value is ever wanted.

### Checked and found nothing to do

- **The committed-insert gap.** Weighed against the alternatives and agreed with, as above. Not re-filed: `fix/bug-a-stored-binary-is-never-given-back` owns it and the script names it at the site.
- **Error paths.** The script asserts no balance after a raised error, and should not: `bug-an-error-inside-a-block-leaks-its-binary-values` put those assertions in `AssignmentValueOwnershipTest`, which is the lowest layer that shows them, and `StreamBackedRepresentationWriteTest` already covers a value that does not parse leaving no stream behind.
- **Duplication.** Ten blocks repeat the same record-count and assert-balance epilogue, and the `Graphic` section repeats four behaviours already pinned for `Binary`. Both were left. The only way to factor the epilogue in D4 is to pass statement text to `TestStreamCount`, which loses lexical access to the script's own variables and turns every assertion into a quoted string; and the `Graphic` pass exists to prove the declaration rather than the behaviour, which the script says at the site. Nothing was cut from the implementer's tests and nothing was added: the new script's cases each pin a distinct behaviour, and no new test was owed, because the review found no defect in the diff.
- **`RemoteStreamValueTest`'s negative assertion** (`Is.Not.InstanceOf<ManagedStream>`) is weaker than a positive one would be, but a positive one would have to name `CoverStream` or `LocalStream`, which is the client stream manager's own internal choice; the test's remarks say so, and the load-bearing assertion is the `IsNative` one beside it. Left as written.
- **Source hygiene.** The new C# file matches its siblings (byte-order mark, CRLF, tabs); the new `.d4` matches its siblings (no mark, CRLF, tabs) after the fix above. At 419 lines the script is mid-pack among the coverage scripts, which run from 50 to 2,586 lines, and its manifest note is shorter than five existing ones. Comments in both new files say why rather than what.
- **Docs.** No page is made untrue by this change, as the compatibility section above records.

### Tests run

Build first: `dotnet build Dataphor.slnx -c Debug`, 0 errors (143 warnings, all pre-existing).

One ring wider than the implement stage, from `tests/DAE.Server.Tests --no-build -c Debug`: that stage's whole filter - the new script's case, the three sibling `*Allocation` scripts, the two existing scripts that call `StreamCount()` (`CursorLibrary` and `DML`), the whole parser round-trip corpus, and the Streams area's C# fixtures `ServerStreamManagerTest`, `ConveyorTest`, `StreamBackedRepresentationWriteTest` and `RemoteStreamValueTest` - plus the value-ownership neighbours a stream-ownership regression would surface in (`AssignmentValueOwnershipTest`, `BufferedOperatorColumnOwnershipTest`, `ColumnChangeRollbackTest`, `TableAssignmentEvaluationTest`), the other `ClientStackTestFixture` fixtures (`RemoteValidateTest`, `RemoteProposalStalenessTest`), and `InternalCoverageTests`.

**470 tests, 450 passed, 20 skipped, 0 failed, 51 s.** The 20 skipped are `StreamAllocation` at its recorded `known-failure` state plus the 19 parked-tree parser cases the corpus ignores by design. Run again after the review's own edits, with the same result. The log is at `tickets/.logs/4-debt-streams-coverage.review.log`.

The three-part full run was not needed: no engine source, no build file, no test infrastructure, no coverage runner and no manifest-format change - one new D4 document, one manifest entry, one test file and its project entry. CI runs the whole suite in Debug and Release on the push.

No pre-existing test failure surfaced, so no `tickets/.pre-existing-error.md` was written.
