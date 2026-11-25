description: Running Dataphor's own 1180-case test suite for SQL Server databases against a real server for the first time in years, about ten cases fail: sorting a table that lives in the database gives a different order from sorting the same rows in memory, and the current date and time read back from the database is not the one that was stored.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: tests/libraries/TestFramework.Coverage.Devices/Documents/InsertDeviceCase.d4, tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs, libraries/SQLDevice/Source/SQLDevice.cs, libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLTypes.cs, tests/DAE.Server.Tests/LiveDeviceTest.cs
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: Two of the failing cases have test data with a tie in the column being ordered, so part of what looks like a defect may be a test that was never deterministic; and the ordering answer may turn out to be "a device orders by the database's rules, and an application that needs the engine's rules sorts in the engine", which is documentation rather than code. A maintainer may reasonably want the test data pinned down before any device code is touched.
----
## Where this came from

Dataphor's SQL Server device has a test corpus of its own, 1180 recorded cases, in the test library `TestFramework.Coverage.Devices`: one case per scalar type and per operator, each creating a table on the device, putting rows in it and asserting what comes back. It has not been run since before the move to .NET 10 - it needs a real SQL Server, and nothing in the suite could ask for one. `test-device-live` built the fixture that runs it (`tests/DAE.Server.Tests/LiveDeviceTest.cs`) and ran it for the first time.

**Result: 8 to 12 of the 1180 cases fail, and the set varies between identical runs.** Measured 2026-11-25 on `modernization`, against SQL Server 2016 LocalDB (`Data Source=(localdb)\MSSQLLocalDB;Integrated Security=SSPI`), four runs of the same build. The whole corpus takes about 10 s.

Two of the four failure families already have owners and were appended to them as arms rather than repeated here:

- a table keyed on a `System.Binary` column refuses a second, *different* value as a duplicate - `bug-binary-comparison-needs-the-mssql-device`;
- a `System.String` column on the device compares case-insensitively - `bug-mssql-device-text-columns-lose-characters`.

This ticket owns the other two.

## Arm one: a table on the device sorts differently from the same rows in memory

Most of the failures say

```
Device Case <n> failed: Next() returned an unexpected row.
```

Every one of them is one of the corpus's 34 *ordering* cases - the pair at sequence 300 and 301 of each scalar type. Each creates two tables with the same columns, one in the in-memory device and one on the SQL Server device, puts the same four rows in both, and then walks both cursors side by side through `TestNavigable(...)`, which is where that message comes from (`tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs`). The message means the two cursors handed back different rows at the same position: the engine and the database sorted the same four rows differently.

The types whose pair failed in at least one run: `Byte`, `Integer`, `Long`, `Short`, `Guid` and `String`. Which members of a pair fail changes from run to run, which is the first thing to understand before touching anything.

Two different causes are already visible in the test data, and they want different answers:

- **A tie in the ordering column.** The `Byte` pair orders by a column holding 42, 255, 254, 42 - two rows share a value, so the order is not total and the engine and the database are each free to break the tie their own way. A case like that was never deterministic, and the varying failure set is what that looks like. Whether the fix is to the data or to what the case asserts is the first question, and it touches a recorded test corpus, so it is a decision rather than an edit (see *Never edit an expected value* in `tickets/rules/10-dataphor.md`).
- **Collation against ordinal.** The `String` pair orders by values beginning `.`, `A`, `T`, `Z`; the engine compares ordinally and SQL Server by the database's collation, which sorts punctuation elsewhere. There is no tie and nothing random: a device simply sorts by the database's rules. The same question is behind the case-insensitive arm on `bug-mssql-device-text-columns-lose-characters`, and the two should be answered together.

What has to be decided, in one sentence: whether a D4 `order` on a table variable held in a SQL device is promised to give the same sequence as the same `order` evaluated in the engine. If it is, the device has to stop pushing the order down for any type whose D4 comparison it cannot reproduce in SQL; if it is not, that belongs in the user's guide beside the device, and these cases have to say so.

## Arm two: the current date and time does not survive the round trip

```
Device Case <n> failed: System.DateTime current time values were not retrieved properly.
```

One case. It inserts `DateTime()` into a `System.DateTime` column of a table on the device and then asserts the value read back is still equal to `DateTime()`. Two things could be behind it and the ticket should establish which before anything is changed:

- the case compares against a *second* call to `DateTime()`, so unless that operator is fixed for the duration of a statement the case cannot pass on any device, and its memory-device sibling would fail the same way;
- or the device's `datetime` mapping loses precision: SQL Server's `datetime` has a resolution of about 3.3 ms, where D4's `DateTime` is a tick count, so a value written and read back is rounded. That one would be a real and ordinary data-fidelity defect - a timestamp a customer stored is not the timestamp they get back - and the remedy is the device mapping `DateTime` onto `datetime2`, which is a change to the DDL the device emits and so to its translation goldens.

The second is the one worth measuring first, because it is visible without the corpus: write a known tick count into a `DateTime` column on a SQL Server device and read it back.

## How to reproduce

The corpus cannot be reached from the committed suite yet. `LiveDeviceTest` is recorded as a known failure because the script's third statement cannot succeed on any version, which is `test-pre-3-1-scripts-policy`'s third arm - a one-statement ruling. With that statement commented out, and `DATAPHOR_TEST_MSSQL` set to a SQL Server:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~LiveDeviceTest" --logger "console;verbosity=normal"
```

The fixture prints the whole roll-up: one block per failed case, with the case's `DeviceCase.ID` and its message.

**Do not map those printed ids onto the `/*<n>ID, */` comments in `InsertDeviceCase.d4`.** The ids are handed out by `GetNextGenerator('DeviceCase.ID')` as the rows are inserted, and the comments have drifted out of step with them by varying amounts - verified: the two "compared insensitively" messages printed as ids 761 and 762 belong to the rows commented 759 and 760, while id 70's message belongs to the row commented 70. Read `Coverage.Devices.DeviceCase` from the running server instead, or match on the message text, which is unique to each case.
