description: Overwriting a picture or document value with shorter contents used to leave the tail of the old contents on the end; the write now replaces the whole value.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/ScalarValue.cs, tests/DAE.Server.Tests/StreamBackedRepresentationWriteTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
----
## What changed

One site. The setter of `Scalar.AsByteArray` in `src/engine/DAE/Runtime.Data/ScalarValue.cs` (now around line 664), in the branch taken when the value lives in a managed stream rather than in a byte array, opened the stream and wrote the new bytes over it without truncating it first. A value shorter than the one already there therefore kept whatever was past the end of the new bytes, and the read-back was longer than what was written.

The fix mirrors the `AsNative` setter a few hundred lines above in the same file, which was already correct: allocate the stream if the value has none, open it, `SetLength(0)`, then write. A comment at the site says why the truncation is there and names this ticket.

`Scalar.AsBase64String` is a thin wrapper over `AsByteArray`, and the D4 write accessor `SystemBinaryWriteAccessorNode` goes through that wrapper, so the one edit covers every route into the behaviour.

## The reproduction, measured both ways

Verified on this branch by removing the `SetLength(0)` again after the tests were written, rebuilding and running them: both `shorter over longer` cases failed, with the engine reporting `Expected 'qrvM3Q==' but read back 'qrvM3URVZnc='` - the four new bytes with the old value's four-byte tail still on the end - and the other four cases passed. The line was then restored and all six pass. That is the whole defect and the whole of what the tests claim.

## Scope - why this is two types and one accessor

`Binary` and `Graphic` are the only two D4 types the engine keeps in a managed stream. Each has exactly one writable representation, `Value` (base-64), and both are declared in `src/engine/DAE/Schema/DataTypes.d4` against the same write accessor class, so there is one code path, not two.

**`AsDisplayString` was checked and is read-only on both types**, so no second site was fixed: `BinaryAsDisplayStringWriteAccessorNode` and `GraphicAsDisplayStringWriteAccessorNode` in `src/engine/DAE/Runtime.Instructions/NativeAccessorNodes.cs` each do nothing but raise `ReadOnlyRepresentation`. There is therefore no generalised version of this defect left to find, and no test for `AsDisplayString` was added - that it raises is unconditional in the node.

## Tests added

One new fixture, `tests/DAE.Server.Tests/StreamBackedRepresentationWriteTest.cs` on `InProcessTestFixture`, six cases - three lengths across both types, each case a D4 script through `ExecuteScript` that writes through `.Value` and raises an `Error` naming both the expected and the read-back base-64 if they differ:

- `WritingAShorterValueReplacesTheWholeValue` (Binary, Graphic) - the reproduction: eight bytes replaced by four.
- `WritingAValueOfTheSameLengthReplacesTheWholeValue` (Binary, Graphic) - four bytes replaced by four different bytes; already worked, must keep working.
- `WritingALongerValueReplacesTheWholeValue` (Binary, Graphic) - four bytes replaced by eight; already worked, must keep working.

The lengths differ by four bytes rather than one, and the long value's trailing bytes are distinctive, so a fix that truncated by the wrong amount cannot pass.

The fixture had to be added to `<Compile Include=...>` in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` - that project lists its sources explicitly rather than globbing, and a new file not listed there compiles into nothing and runs as zero tests with no error. **Worth knowing while reviewing:** the first run of the new filter reported `No test matches the given testcase filter` rather than failing, which is what a missing entry looks like.

## What was run

- `dotnet build Dataphor.slnx` - 0 errors.
- `dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~StreamBackedRepresentationWriteTest|FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~ServerStreamManagerTest|FullyQualifiedName~ConveyorTest"` - 55 passed, 0 failed, 1 skipped, 7s.

The skipped case is `ServerStreamManagerTest.WritingASourceAndOneOfItsTwoReferencesLeavesThreeDistinctValues`, which carries a pre-existing `[Ignore]` naming the sibling ticket `bug-writing-a-large-value-changes-copies-of-it`. It was already ignored before this change and is still ignored; this change neither fixes nor affects it. The new tests assert only on what the value being written reads back as, never on a copy of it, so they pass whether or not that sibling has landed.

## Coverage manifest

Checked. `tests/DAE.Server.Tests/coverage-manifest.json` has one entry in this area, `StreamAllocation`, and it is `known-failure` for an unrelated reason - `Object "Coverage" not found`, tracked by `test-pre-3-1-scripts-policy` - so there was nothing to flip. No manifest change in this commit.

## Migration notes

`docs/migration-notes.md` gained one entry in the `D4, catalog and libraries` table, in the same commit: a write through a `Binary` or `Graphic` representation now replaces the whole value rather than overwriting its beginning, so an application written around the old behaviour - padding its contents to a fixed length, or relying on a short write leaving the rest of the old value readable - must be changed to write the whole value it wants.

## Honest gaps for the reviewer

- **No test covers a stream-backed value whose `StreamID` is null when written.** The fix adds the same `if (StreamID == StreamID.Null) StreamID = Manager.StreamManager.Allocate();` guard the `AsNative` setter has, so that the two setters read the same, but every route the tests exercise reaches the setter with a stream already allocated - the selector allocates one. The guard is therefore consistency rather than a fixed defect, and whether a nil stream-backed scalar can reach this setter at all was not established. A reviewer who thinks the guard is dead code is making a reasonable claim; the argument for keeping it is that the sibling setter has it and a reader comparing them should not have to work out why they differ.
- **The `.Value` round trip through base-64 is the only shape tested.** `AsByteArray` and `AsBase64String` are also public C# members an extension could call directly; no test calls them from C#, on the grounds that the D4 script route is the lowest layer that shows the behaviour and goes through the same line.
- Nothing was measured about performance. `SetLength(0)` on a stream that is about to be rewritten should cost nothing material, but no timing was taken.
