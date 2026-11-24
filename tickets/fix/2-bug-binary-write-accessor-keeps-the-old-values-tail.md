description: Overwriting a picture or document value with shorter contents leaves the tail of the old contents stuck on the end, so what is read back - and stored - is not what was written.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/ScalarValue.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, tests/DAE.Server.Tests/InProcessTestFixture.cs
repro: verified
difficulty: easy
----
## What happens

The two D4 types the engine holds in managed streams, `Binary` and `Graphic`, each expose their contents through a representation called `Value`, which reads and writes the bytes as a base-64 string. Writing through it - `MyPicture.Value := '<base64>'` - writes the new bytes over the existing stream **without first shortening the stream to nothing.** If the new value is shorter than the value already there, the bytes of the old value past the end of the new one stay where they are and become part of the value.

## The measurement

Verified 2026-11-23 against the current `modernization` head, through a throwaway NUnit fixture on `InProcessTestFixture` (built, run, then removed again - the fixture is not in the tree). The values below are the base-64 the test read back with `ToBase64String`:

- A `Binary` variable holding 5 bytes (`AAECAwQ=` - `00 01 02 03 04`), written with 4 bytes (`BQYHCA==` - `05 06 07 08`), reads back as `BQYHCAQ=` - `05 06 07 08 04`. Five bytes, with the old last byte on the end.
- The same with a different value: a 5-byte value written with `CQkJCQ==` (`09 09 09 09`) reads back as `CQkJCQQ=` (`09 09 09 09 04`).
- Writing a value of the same length or longer is correct, which is why this has gone unnoticed.

The corrupt value is an ordinary value from that point on: inserting it into a table stores the extra bytes, and comparing it with the value that was written reports them unequal.

## Root cause

One site: the setter of `Scalar.AsByteArray`, `src/engine/DAE/Runtime.Data/ScalarValue.cs`, around line 657. In the non-native branch - the one taken when the value lives in a managed stream rather than in a byte array - it opens the stream and calls `stream.Write(value, 0, value.Length)` with no `stream.SetLength(0)` first.

The correct version of the same code is a few hundred lines above it in the same file: the setter of `Scalar.AsNative`, around line 150, opens the stream, calls `stream.SetLength(0)`, and then writes. Those are the only two places in `ScalarValue.cs` that write into an existing stream, so this is a point defect with an exact sibling to copy, not a pattern repeated across the file.

`Scalar.AsBase64String` is a thin wrapper over `AsByteArray`, and `SystemBinaryWriteAccessorNode` (`src/engine/DAE/Runtime.Instructions/SystemNodes.cs`, around line 532) is the D4 write accessor that calls it, so all three reach the same line. The `Binary(...)` and `Graphic(...)` selectors are **not** affected: they allocate a fresh, empty stream, so there is nothing stale to leave behind.

## Expected behaviour

Writing through a representation of a stream-backed value replaces the value: what `ToBase64String` returns afterwards is exactly what was written, at any length, shorter or longer than before.

## Tests this owes

Its reproduction, at the engine level: write a shorter value over a longer one through the `Value` representation of a `Binary` and of a `Graphic`, and assert the read-back equals what was written. Include the same-length and longer cases in the same test so the fix is pinned in both directions.

The class behind it is "a representation's write path leaves part of the previous value behind", and it is cheap to generalise: for every scalar type the engine holds in a stream, write a long value then a short one through each of that type's write accessors and assert the read-back each time. Today that is two types with two representations each (`Value` and `AsDisplayString`), so the general form is barely larger than the specific one - prefer it, and say in the handoff whether `AsDisplayString` had the same defect.

## Compatibility

**This changes what an existing application sees.** A D4 library that writes shorter contents through `.Value` gets a different value after the fix than before, and one that was written around the old behaviour - for instance by always writing padded contents - will behave differently. It needs a `docs/migration-notes.md` entry in the same commit saying that writing a shorter value through a `Binary` or `Graphic` representation now replaces the whole value rather than overwriting its beginning.

## Where this came from

The planning pass on `debt-streams-coverage`, checking whether a D4 script can reach the engine's copy-on-write path for shared streams. It can - and the value it read back from the copy was wrong.
