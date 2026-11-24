description: Overwriting a picture or document value with shorter contents used to leave the tail of the old contents on the end, and the write now replaces the whole value. Review fixed two more defects in the same place: setting such a value when it had none yet crashed, and a value the server could not read left memory held for the rest of the session.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/ScalarValue.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, tests/DAE.Server.Tests/StreamBackedRepresentationWriteTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
----
## What landed

Writing through the `Value` representation of a `Binary` or a `Graphic` - `MyPicture.Value := '<base64>'` - now sets the value, in three senses that were each broken:

- **It replaces the whole value.** The setter of `Scalar.AsByteArray` (`src/engine/DAE/Runtime.Data/ScalarValue.cs`, the branch taken when the value lives in a managed stream) wrote the new bytes over the existing stream without truncating it first, so contents shorter than what was there kept the tail of the old value on the end. One `stream.SetLength(0)` before the write, mirroring the `AsNative` setter a few hundred lines above, which was already correct. This is the implement stage's work, commit `ticket(implement): bug-binary-write-accessor-keeps-the-old-values-tail`.
- **It works on a value that has none yet.** `var LV : Binary; LV.Value := '<base64>';` failed with "A nil was encountered where a value was expected" over a `NullReferenceException`. Found in review; fixed here.
- **It holds nothing when the value cannot be read.** A string that is not base-64 left a managed stream allocated for the life of the session, through the selector `Binary('<not base64>')` as well as through the write. Found in review; fixed here.

`Scalar.AsBase64String` is a thin wrapper over `AsByteArray` and the D4 accessor nodes go through that wrapper, so the three fixes sit on one code path shared by both types. `Binary` and `Graphic` are the only two D4 types the engine keeps in a managed stream, each declares exactly one writable representation, and `src/engine/DAE/Schema/DataTypes.d4` points both at the same `SystemBinarySelectorNode` and `SystemBinaryWriteAccessorNode`. Their other representation, `AsDisplayString`, is read-only on both types and raises unconditionally.

`docs/migration-notes.md` carries one row covering all three changes.

## Review findings

Read the implement diff before the handoff. The fix itself was checked by reading `Scalar`, `ServerStream`, `ServerStreamManager.Change` and every `OpenStream()` call site in the engine, and the behaviour was measured by running D4 through the in-process engine with each fix in and out.

### Checked and sound

- **The fix is at the only remaining site.** `ScalarValue.cs` has exactly two places that write into an existing managed stream - the `AsNative` setter and the `AsByteArray` setter - and both now truncate first. The only other `OpenStream()` callers in the engine are in `BinaryNodes.cs` and all three are reads.
- **The scope claim holds.** Read `DataTypes.d4`: `Binary` and `Graphic` each have one writable representation and both name the same accessor classes, so there is one path and not two. `BinaryAsDisplayStringWriteAccessorNode` and `GraphicAsDisplayStringWriteAccessorNode` do nothing but raise, as the handoff says.
- **The tests can fail.** `ServerProcess.ExecuteScript` goes through `ServerScript.Execute`, which propagates, so a D4 `raise` in these scripts reaches NUnit. Confirmed independently by the two mutations below, each of which failed exactly the cases it should and no others.
- **The truncation lands at offset zero whatever the stream's position was.** `SetLength` clamps a position past the new length down to it, on each of the providers these streams use, so the write that follows always starts at the beginning. That makes the fixed setter more robust than the old one, which wrote at whatever position the stream was opened at.
- **Calling `Change` twice is free.** `SetLength` and `Write` each call `ServerStreamManager.Change`, but `Change` only does work while the stream is still a reference and removes it from `_referencingHeaders` on the first call, so the second is a dictionary lookup.
- **Writing an empty value over a longer one works.** Probed; the read-back is empty. Not kept as a test - it is the degenerate end of the "shorter" case and goes through the same line.
- **Source hygiene.** Tabs, CRLF and the byte-order-mark state preserved in every file edited; no file reformatted or re-encoded.

### Fixed in this pass

- **A write to a value that had none yet crashed.** `SystemBinaryWriteAccessorNode` read its first argument - the value being written to - as a `StreamID` with no allowance for there being no value, so `var LV : Binary; LV.Value := '<base64>';` unboxed a null into a struct and threw. Measured: both type cases failed with a `NullReferenceException` reported as "A nil was encountered where a value was expected". The intended semantics are not in doubt - every native write accessor in `NativeAccessorNodes.cs` checks only the *new* value for nil and builds the result from it, because a representation write sets the value - so a nil must not propagate from the value being written to, and the node has to arrange a stream. Fixed by handing `StreamID.Null` to the `Scalar` and letting the setter's allocation guard do it. The two cases of `WritingToANilValueSetsIt` are the reproduction.
- **A value that could not be read leaked a stream.** Both the selector and the write accessor took a stream out of the stream manager *before* parsing the base-64 they were given, so a malformed string raised with that stream still allocated and held for the life of the session. Measured with the engine's own `StreamCount()`: one stream held per failed `Binary('not base64 at all!')`, never given back. The same one-line change fixes both - pass `StreamID.Null` and let the `AsByteArray` setter allocate, which happens only after `Convert.FromBase64String` has succeeded. Measured both ways: with the selector's allocation restored, `AValueThatDoesNotParseLeavesNoStreamBehind` fails for both types with "left 1 stream(s) behind"; with the fix the count does not move. This is also why the nil fix above allocates in the setter rather than in the node - allocating in the node would have added a third instance of the same leak.
- **The implement stage's open question is answered, and the guard it added is now live code.** The handoff flagged `if (StreamID == StreamID.Null) StreamID = ...Allocate();` as possibly dead, since nothing it tested reached the setter without a stream. It was dead, because the node crashed first - and both fixes above now route through it, so it is the single allocation site for a value that has none. The comment at the site was rewritten to say that, and to say why the allocation must stay below the base-64 parse.
- **The migration note was in the wrong column and malformed.** The new row's text ran past the cell boundary into the "What to do" column, and the row was missing the trailing pipe every other row in that table has. The row now has three cells like its neighbours, with the three behaviour changes in the Change column and the action in the action column.
- **A line-ending inconsistency.** The implement stage appended its migration-note row with a bare LF into a file whose working copy is otherwise CRLF. Invisible in git, which normalizes on commit, but it is what the project's source-hygiene rule asks to preserve. Normalized.
- **The test fixture repeated its own script.** The nil case needed a declaration with no initializer, and was written as a second copy of the helper's D4 script. Extracted: `AssertWriteSets(declaration, newValue)` holds the one script, and `AssertWriteReplaces(typeName, initialValue, newValue)` is three lines over it.

### Checked and deliberately left alone

- **The six length cases stay, including the `Graphic` half.** Three lengths over two types reads at first like one behaviour tested six times, and the `Graphic` cases do run the same C# line as the `Binary` ones. They stay because they pin something the C# cannot: that both types are stream-backed and both route through this accessor is a fact of `DataTypes.d4`, and a `Binary`-only test would still pass if `Graphic` were pointed at a different class. The three lengths pin the fixture's stated rule - the read-back is what was written, *at any length* - which is the sentence the defect broke. Cost measured: the whole fixture is ten cases in 50 ms.
- **No test calls `AsByteArray` or `AsBase64String` from C#.** The handoff names this gap and its reasoning is right: the D4 route is the lowest layer that shows the behaviour and goes through the same line. A C# test here would restate the implementation.
- **The inexact `stream.Read` in the `AsByteArray` getter.** `stream.Read(tempValue, 0, (int)stream.Length)` ignores the count returned, which warning CA2022 flags. Already a recorded decision: the `NOTE:` at `src/engine/Base/StreamUtility.cs` line 122 names "DAE Runtime.Data" among the call sites that assume one read fills the buffer, says that is true of the memory, file and engine streams they are handed, and says what to do if one of them is ever given a partial-read stream. No revisit condition has tripped, so it is left alone.
- **Nothing was timed.** `SetLength(0)` on a stream about to be rewritten should cost nothing material, but this review took no measurements either, and says so rather than claiming it.

### Tripwires: one, parked on the ticket that owns its site

A write that replaces the whole value still copies the old contents in full before discarding them: when the value is a copy-on-write reference, `ServerStreamManager.Change` copies the source's bytes into a stream of its own, and the `SetLength(0)` is what triggers that now rather than the `Write`. The cost is exactly what it was before the fix, so nothing regressed - but it informs an open design choice. `bug-writing-a-large-value-changes-copies-of-it`, in `fix/`, owns `Change` and deliberately leaves its fix shape open, so the observation was appended there as an arm rather than duplicated as a `NOTE:` at this site: whoever picks that ticket is the reader who needs it. Read from the code, not timed.

### Nothing filed

No new ticket. Both defects found were one-line changes at the site this ticket was already in, with the reproduction tests that go with them, which is what this project's rule for bugs met along the way asks for; nothing else found rises to a current-release anchor or names a class-level invariant. The near-class - "write accessor nodes that read their first argument without allowing for a nil" - was checked and is not one: every other `(StreamID)argument` in the engine is in `BinaryNodes.cs` or in the read accessor, and all of them guard it. There were two instances of the allocate-before-parse shape and both are fixed.

## What was run

Build first: `dotnet build Dataphor.slnx` - 0 errors.

The implement stage's targeted set plus the fixtures a regression in the value and stream paths would show up in:

```
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~StreamBackedRepresentationWriteTest|FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~ServerStreamManagerTest|FullyQualifiedName~ConveyorTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest|FullyQualifiedName~TableAssignmentEvaluationTest|FullyQualifiedName~ColumnChangeRollbackTest"
```

64 passed, 0 failed, 1 skipped, 20 s.

Then the coverage scripts for the engine areas the diff touches - the one script with a `Graphic` column, the type, nil and conversion scripts that go through representation accessors, and the parser corpus, since the selector change is in the file every script's plan nodes come from:

```
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "Name=KeyAffectingUpdate|Name=Types|Name=NilLibrary|Name=Conversions|Name=StringLibrary|Name=Internet|Name=DML|Name=LibraryFilesCoverage|FullyQualifiedName~ParserRoundTripCorpusTest"
```

370 passed, 0 failed, 19 skipped, 24 s. `python tools/docs-size-check/check_size.py` - 11 files checked, 0 over the limit.

The three-part full run was not needed: this change is one setter and two plan nodes, not the build, the test infrastructure, the coverage runner, or the lexer, parser and compiler every script goes through. CI covers it.

Two kinds of skip appear above and neither is new. `ServerStreamManagerTest.WritingASourceAndOneOfItsTwoReferencesLeavesThreeDistinctValues` carries a pre-existing `[Ignore]` naming `bug-writing-a-large-value-changes-copies-of-it`; nothing here affects it, and the new tests assert only on the value being written, never on a copy of it. `Domains` and `Serialization` are recorded in `tests/DAE.Server.Tests/coverage-manifest.json` as known failures owned by `test-pre-3-1-scripts-policy`, as are the 19 legacy documents the parser corpus skips. No `tickets/.pre-existing-error.md` was written: nothing failed that was not fixed here.

## Coverage manifest

Unchanged. The one entry in this area, `StreamAllocation`, is a `known-failure` for an unrelated reason (`Object "Coverage" not found`, owned by `test-pre-3-1-scripts-policy`), so there was nothing to flip. `debt-streams-coverage` is the ticket that adds the D4-level stream-count script and it names this ticket as a prerequisite: the copy-on-write arm it describes reads back correctly now, and the nil arm it plans - "a variable holding nil holds no stream" - is unaffected, since these fixes allocate only when a value is actually written.
