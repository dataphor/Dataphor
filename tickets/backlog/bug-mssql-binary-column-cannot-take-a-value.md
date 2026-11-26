description: A table column declared with the shared "MSSQLBinary" type on a SQL Server database cannot be given a value - every insert fails with an internal cast error - because the code that converts the value only handles one of the two shapes Dataphor uses for binary data.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLTypes.cs, libraries/SQLDevice/Source/SQLTypes.cs, libraries/SQLDevice/Documents/Register.d4, tests/DAE.Server.Tests/LiveDeviceTest.cs
repro: static
severity: edge-case
likelihood: unusual
tradeoffs: The type is a niche one that nothing in the repository exercises and that no live pass has ever reached, so a maintainer may reasonably conclude no application uses it and remove it instead of repairing it; and the tidy repair changes the base class of a published device scalar-type class, which a custom C# extension deriving from it would notice.
----
## What is wrong

`SQLDevice.MSSQLBinary` is a D4 type the shared SQL-device library publishes - `create type MSSQLBinary like System.Binary` in `libraries/SQLDevice/Documents/Register.d4` - for a fixed-length binary column. Each SQL device maps it with a scalar-type class of its own, and the SQL Server device's is `MSSQLMSSQLBinary` in `libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLTypes.cs`. All three of its conversion operators cast the value straight to a byte array:

```csharp
public override object ToScalar(IValueManager AManager, object AValue) { return (byte[])AValue; }
public override object FromScalar(IValueManager AManager, object AValue) { return (byte[])AValue; }
public override string ToLiteral(IValueManager AManager, object AValue) { ... Convert.ToBase64String((byte[])AValue) ... }
```

A D4 value of a type declared `like System.Binary` reaches a device in one of two shapes: a byte array, or a `StreamID` naming a stream the engine holds. A value written in D4 - `MSSQLBinary("07f9b0d7ea0843708a9e24245b19e12e")` - arrives stream-backed, whatever its length. So the cast throws

```
Unable to cast object of type 'Alphora.Dataphor.DAE.Streams.StreamID' to type 'System.Byte[]'
```

from `DeviceSession.InsertRow`, before any SQL is generated. The column can be created; it cannot be given a value.

The shared base class for the same D4 base type, `SQLBinary` in `libraries/SQLDevice/Source/SQLTypes.cs`, converts both shapes in both directions and allocates a stream on the way back for a value at or above its own threshold. `MSSQLMSSQLBinary` does not derive from it and does not copy that part of it.

## The evidence, and why no test caught it

Measured on the **PostgreSQL** device, whose `PostgreSQLMSSQLBinary` was a line-for-line copy of the same three operators. `device-pgsql-port`'s review (2026-11-26) added `LivePGSQLDeviceTest.TheDeviceRoundTripsAByteArrayColumn`, which creates such a column on a real server, inserts one value and reads it back; it failed with exactly the cast above, and passes now that `PostgreSQLMSSQLBinary` derives from `SQLBinary` and inherits the conversion. Nothing else in that commit changed the value path.

The SQL Server copy is untouched and is the same code, so the same insert fails the same way there; that is an inference from reading it, not a run, which is why `repro: static`. What would confirm it is the same create-insert-read case against a SQL Server device - a sibling of the PostgreSQL one, in `LiveDeviceTest` or beside it.

Two reasons nothing noticed in eighteen years. The device-coverage corpus has seventeen `MSSQLBinary` cases, but `TestMSSQLDevice.d4` raises an error before reaching the corpus at all, so `LiveDeviceTest` has never run one (that is `test-pre-3-1-scripts-policy`'s third arm). And the translation goldens never execute anything; they only compare generated SQL, and the corpus they generate from has no column of this type.

## The shape of the fix

The point fix is to give `MSSQLMSSQLBinary` the two-shape conversion. The better one, and what the PostgreSQL side now does, is to stop having the conversion twice: derive the class from `SQLBinary` and override only what is actually device business - the column type (`binary(Storage.Length)`) and the SQL parameter type (`SQLByteArrayType`). Then there is one implementation of "a D4 Binary value in either shape" rather than a copy per device, and the next device added cannot get it wrong.

Worth settling at the same time, because they are the same duplication:

- `SQLBinary` and `SQLGraphic` in `libraries/SQLDevice/Source/SQLTypes.cs` each carry their own private copy of the same `GetNativeValue` helper. One would do.
- All of these `ToLiteral` implementations put base64 inside a cast - `cast('B/mw1+oIQ3CKniQkWxnhLg==' as binary(30))` - which is not a literal T-SQL or PostgreSQL accepts for binary data. It is unreachable today, because a scalar type's `ToLiteral` is only substituted into a statement when the command has parameters turned off, and both devices leave `UseParametersForCursors` on; there is a `NOTE:` to that effect on `PostgreSQLMSSQLBinary`. Whoever is in this code should decide whether to fix it or to delete the dead implementations.
- Whether the type should exist at all. It is a SQL-Server-shaped name published by the shared library, its comparison operators are implemented in the SQL Server device's assembly (`bug-binary-comparison-needs-the-mssql-device`), and nothing in the repository or its tests uses it. Removing it would be a compatibility decision rather than a repair, and would need the `blocked/` route.
