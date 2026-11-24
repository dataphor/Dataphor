description: Overwriting a picture or document value with shorter contents leaves the tail of the old contents stuck on the end, so what is read back - and stored - is not what was written.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/ScalarValue.cs, tests/DAE.Server.Tests/InProcessTestFixture.cs, docs/migration-notes.md
repro: verified
difficulty: easy
----
## What happens

The two D4 types the engine holds in managed streams, `Binary` and `Graphic`, each expose their contents through a representation whose property is called `Value`, which reads and writes the bytes as a base-64 string. Writing through it - `MyPicture.Value := '<base64>'` - writes the new bytes over the existing stream **without first shortening the stream to nothing.** If the new value is shorter than the value already there, the bytes of the old value past the end of the new one stay where they are and become part of the value.

## The measurement

Verified 2026-11-23 against the current `modernization` head, through a throwaway NUnit fixture on `InProcessTestFixture` (built, run, then removed again - the fixture is not in the tree). The values below are the base-64 the test read back with `ToBase64String`:

- A `Binary` variable holding 5 bytes (`AAECAwQ=` - `00 01 02 03 04`), written with 4 bytes (`BQYHCA==` - `05 06 07 08`), reads back as `BQYHCAQ=` - `05 06 07 08 04`. Five bytes, with the old last byte on the end.
- The same with a different value: a 5-byte value written with `CQkJCQ==` (`09 09 09 09`) reads back as `CQkJCQQ=` (`09 09 09 09 04`).
- Writing a value of the same length or longer is correct, which is why this has gone unnoticed.

The corrupt value is an ordinary value from that point on: inserting it into a table stores the extra bytes, and comparing it with the value that was written reports them unequal.

## Root cause - one site

The setter of `Scalar.AsByteArray`, `src/engine/DAE/Runtime.Data/ScalarValue.cs` line 657. In the non-native branch - the one taken when the value lives in a managed stream rather than in a byte array - it opens the stream and calls `stream.Write(value, 0, value.Length)` with no `stream.SetLength(0)` first.

The correct version of the same code is the setter of `Scalar.AsNative`, same file, around line 141: it allocates a stream if the value has none, opens it, calls `stream.SetLength(0)`, and then writes. Those are the only two places in `ScalarValue.cs` that write into an existing stream, so this is a point defect with an exact sibling to copy.

`Scalar.AsBase64String` is a thin wrapper over `AsByteArray`, and `SystemBinaryWriteAccessorNode` (`src/engine/DAE/Runtime.Instructions/SystemNodes.cs` line 532) is the D4 write accessor that calls it, so all three reach the same line.

Not affected, confirmed by reading the code:

- The `Binary(...)` and `Graphic(...)` selectors allocate a fresh, empty stream, so there is nothing stale to leave behind.
- The **`AsDisplayString` representation of both types is read-only.** `BinaryAsDisplayStringWriteAccessorNode` and `GraphicAsDisplayStringWriteAccessorNode` (`src/engine/DAE/Runtime.Instructions/NativeAccessorNodes.cs` lines 853 and 883) both do nothing but `throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, ...)`. So the generalised form the fix stage asked about - every writable representation of every stream-backed scalar type - is exactly two types with one writable representation each (`Value` on `Binary`, `Value` on `Graphic`, per `src/engine/DAE/Schema/DataTypes.d4` lines 456 and 480), both routed through the single `SystemBinaryWriteAccessorNode`. There is no second shape of this defect to find, and the general test and the specific test are the same test.

## Expected behaviour

Writing through a representation of a stream-backed value replaces the value: what `ToBase64String` returns afterwards is exactly what was written, at any length, shorter or longer than before.

## Relation to the sibling ticket

`bug-writing-a-large-value-changes-copies-of-it` reaches this same setter from the other direction, but its root cause is a different site (`ServerStreamManager.Change`), so the two are independent and need no ordering. Worth knowing while editing: `ServerStream.SetLength` calls `Change` just as `Write` does, so adding the `SetLength(0)` makes the copy-on-write separation happen a moment earlier in the write path. That is harmless either way, and this ticket's tests must pass whether or not that sibling has landed - assert only on what the value being written reads back as, never on what a copy of it reads.

## Tests

One new fixture on `InProcessTestFixture` in `tests/DAE.Server.Tests/`, following the shape of `AssignmentValueOwnershipTest.cs` (same directory) - a D4 script per case through `ExecuteScript`, raising a D4 `Error` when the read-back is wrong, which is the lowest layer that shows the behaviour. Cover, for **both** `Binary` and `Graphic`:

- shorter over longer - the reproduction;
- same length over same length;
- longer over shorter.

Each case asserts the read-back `Value` equals exactly the base-64 that was written. Use values whose lengths differ by more than one byte, and make the trailing bytes of the long value distinctive, so a partial fix cannot pass. Do not add a test for `AsDisplayString`: it raises, and that it raises is already what the node does unconditionally.

Also check whether any coverage-manifest entry in `tests/DAE.Server.Tests/coverage-manifest.json` covers this area and needs flipping; if you flip one, run that script.

## Compatibility and migration notes

**This changes what an existing application sees.** A D4 library that writes shorter contents through `.Value` gets a different value after the fix than before, and one that was written around the old behaviour - for instance by always writing padded contents - will behave differently. It needs a `docs/migration-notes.md` entry **in the same commit** saying that writing a shorter value through a `Binary` or `Graphic` representation now replaces the whole value rather than overwriting its beginning.

This is a defect fix, not a change to D4 semantics or to an operator signature, so it needs no `blocked/` decision: the documented and intended behaviour of a representation write is that it sets the value.

## TODO

- In the setter of `Scalar.AsByteArray` (`src/engine/DAE/Runtime.Data/ScalarValue.cs` line 657), in the non-native branch, mirror the `AsNative` setter: allocate the stream if `StreamID` is `StreamID.Null`, then `stream.SetLength(0)` before the `stream.Write`. The allocate guard is a no-op whenever a stream already exists and makes the two setters consistent; keep it inside the same `try`/`finally` shape the sibling uses.
- Preserve the file's tabs, CRLF line endings and byte-order-mark state.
- Add the new test fixture described above under `tests/DAE.Server.Tests/`, with a fixture-level summary comment naming this ticket's slug and stating the rule being pinned (the file's existing fixtures do this).
- Add the `docs/migration-notes.md` entry.
- `dotnet build Dataphor.slnx`, then run the new fixture by name plus `AssignmentValueOwnershipTest` and `ServerStreamManagerTest` (the existing fixtures over the same stream-backed value paths) with `--filter`. Name the filters in the handoff.
- In the handoff, state explicitly that `AsDisplayString` was checked and is read-only on both types, so no second site was fixed.
