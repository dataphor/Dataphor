description: When Dataphor reads the shape of a SQL database it was pointed at, it silently leaves out any column whose type it does not recognise, so the Dataphor view of that table is missing columns and nothing says so. It happens with two of SQL Server's date-and-time types, and with several of PostgreSQL's ordinary ones.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/SQLDevice/Source/SQLDevice.cs, libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLDevice.cs, libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLTypes.cs, libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLDevice.cs, tests/DAE.Server.Tests/MSSQLVersionMatrixTest.cs
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: Mapping `datetime2` onto D4's `DateTime` loses precision rather than data (D4 `DateTime` holds ticks, so the sub-second part survives, but the operator translations in the device's catalog script are written against `datetime` and would have to be checked one by one), and `datetimeoffset` has no D4 type at all - D4 has no time-zone-aware type - so the honest answer there may be "refuse the column loudly" rather than "map it". A maintainer may reasonably decide that the whole fix is to raise an error naming the column instead of mapping it, and that an application wanting these types adds a view in SQL Server that casts them.
----
## What is wrong

A SQL Server device in a reconciliation mode whose master is the device reads the tables already in its database and creates a D4 table variable for each. For each column it asks `MSSQLDevice.ShouldIncludeColumn` whether it knows the column's SQL type, and **a column it does not know is left out of the table variable with nothing said about it** - `SQLDevice.GetDeviceTables` simply does not add it and goes on to the next column.

Two SQL Server types that are in ordinary use fall into that gap: `datetime2` and `datetimeoffset`. Both have existed since SQL Server 2008; `datetime2` is what Microsoft's own guidance has recommended in place of `datetime` for years, so a schema written any time in the last decade is likely to use it. In `ShouldIncludeColumn` both appear in the case list **commented out**, beside `date` and `time` which are accepted:

```
case "date": // 2008 (majorVersion >= 10)
case "time": // 2008
//case "datetime2": // 2008
//case "datetimeoffset": // 2008
```

`FindScalarType`, in the same file, has the matching pair commented out of its `datetime` arm, and `MSSQLTypes.cs` carries the note that explains both: *"TODO: Support for datetime2 - The new datetime2 type is still a datetime, but has greater scale and precision, so we need an MSSQLDateTime2 mapping to support it."* The comment is dated 2008 by the file's copyright header. So this is not a regression; it is unfinished work from the version of SQL Server that introduced the types, and nothing has exercised it since.

## Why it matters

The failure is silent, and the thing that goes missing is a column. A deployment pointing a device at an existing schema gets a D4 table variable that looks right, has the table's name and most of its columns, and cannot see the rest. The consequences follow from there: a form derived from that table variable has no field for the column, a D4 insert into it leaves the column to the server's default (or fails, if the column is not nullable and has none), and an application that reads the table to copy or report on it loses that data without an error anywhere.

A `datetimeoffset` column that is not nullable and has no default makes the table variable unusable for insert, which at least fails loudly. A nullable one does not.

## What was measured

Verified 2026-11-26 by `device-sqlserver-version-matrix`. The new fixture `tests/DAE.Server.Tests/MSSQLVersionMatrixTest.cs` creates this table on the server directly, through a connection of its own rather than through the device, and then runs `Reconcile`:

```sql
create table MatrixTypes (
	Id int not null primary key,
	DateTimeColumn datetime null,
	DateColumn date null,
	TimeColumn time null,
	DateTime2Column datetime2 null,
	DateTimeOffsetColumn datetimeoffset null,
	VarcharColumn varchar(30) null,
	NVarcharColumn nvarchar(30) null
);
```

The reconciled table variable has six columns, not eight. `DateTime2Column` and `DateTimeOffsetColumn` are absent; `Reconcile` returns an empty error list, so nothing anywhere reports a problem. Identical on SQL Server 2016 LocalDB (13.0.4001.0), 2019 LocalDB (15.0.4382.1), 2019 in a Linux container (15.0.4510.2) and 2022 in a Linux container (16.0.4325.4) - every version in the supported-version matrix has all of these types, so this is reachable everywhere rather than being a quirk of one release.

The fixture pins the current behaviour, which means a fix changes its expectations: `ExpectedColumns` in that file records `DateTime2Column` and `DateTimeOffsetColumn` as dropped, and a fix that maps them makes that case fail with the message *"DateTime2Column now reconciles, which it did not before: record what the device makes of it."*

## The two decisions a fix has to make

**What `datetime2` becomes.** D4's `System.DateTime` is backed by ticks, so `datetime2`'s precision fits it - which is more than can be said for `datetime`, whose 3.33 ms resolution the device already works around. The work is in the device's operator map: `libraries/MSSQLDevice/Source/SystemCatalog.d4` translates every `DateTime` operator into T-SQL written against `datetime` (`cast({0} as datetime)`, `datepart`, `convert(Datetime, …)` and a constant for the zero date), so a column stored as `datetime2` would be cast to `datetime` on the way through many of those translations and lose exactly the precision the type was chosen for. Mapping the column and leaving the translations alone would be a half-fix that reads as a whole one.

**What `datetimeoffset` becomes.** D4 has no time-zone-aware type, so there is nothing to map it to without choosing a convention (convert to UTC, drop the offset, or expose the pair as a row type). Refusing it with a named error may be the right answer, in which case the general problem below is the real fix.

## The general problem behind it, worth considering first

A column type the device does not know is dropped in silence, and that is the rung above this instance: whatever is decided about these two types, a schema using a type the device has never heard of (a user-defined type, `hierarchyid`, `geography`, `sql_variant`, `xml`) is reconciled the same silent way. Raising an error that names the table and the column, or recording the skipped column on the table variable so that something can report it, turns this whole class from data quietly missing into a message a deployment can act on - and it is the change that makes the next unknown type harmless. `SQLDevice.GetDeviceTables` is the one site: the `if (ShouldIncludeColumn(...))` whose else branch does not exist.

That change is itself one a deployment can notice - a device that reconciled a schema yesterday may refuse it tomorrow - so it is a decision rather than an obvious repair.

## What would confirm a fix

Extend `MSSQLVersionMatrixTest`'s table to every SQL Server type the device's case list names, plus two it does not, and assert the D4 type and length of each; that pins the whole mapping surface rather than the seven columns there now, and is the test the fix wants rather than a test per type.

## Second arm: the PostgreSQL device drops its own column types (added 2026-11-26 by `device-pgsql-device-golden`)

The same gap is wider in the PostgreSQL device, and it is wider in a way that is self-contradictory: **the device drops column types that it creates itself.**

`PostgreSQLDevice.ShouldIncludeColumn` and `PostgreSQLDevice.FindScalarType` were written by copying the SQL Server device's case lists and adding a few PostgreSQL spellings to them, so most of what they test for are T-SQL type names (`uniqueidentifier`, `image`, `nvarchar`, `smalldatetime`, `varbinary`). What the device asks PostgreSQL for is `information_schema.columns.data_type` (`PostgreSQLDevice.GetDeviceTablesExpression`), and PostgreSQL's spellings for several ordinary types are not in either list:

| PostgreSQL `data_type` | In the case list? |
|---|---|
| `bytea` | no - and this is what the device creates for a D4 `Binary` column |
| `uuid` | no - and this is what it creates for a D4 `Guid` column |
| `time without time zone` | no - and this is what it creates for a D4 `Time` column |
| `character` | no (the list has T-SQL's `char`) |
| `double precision` | no |
| `timestamp with time zone`, `time with time zone` | no |

So a device pointed at a table *it created itself* reconciles back fewer columns than it wrote. The three `no` rows at the top of that table are each visible in this repository's own checked-in output: `tests/goldens/devices/PGSQL/Table.AllTypes.sql` has the device issuing `"Payload" bytea`, `"Identifier" uuid` and `"Clock" time`, and none of those three strings appears in `ShouldIncludeColumn`.

`FindScalarType` carries a second, narrower defect behind the same door. Its `binary` and `timestamp` arms resolve the D4 type `SQLDevice.PostgreSQLBinary`, and **there is no such type**: `libraries/SQLDevice/Documents/Register.d4` declares `MSSQLBinary` and nothing named `PostgreSQLBinary`, so those two arms would fail to resolve rather than return a type. They are unreachable today only because `binary` and `timestamp` are T-SQL spellings that PostgreSQL's `information_schema` never reports - the same copy-paste that causes the dropped columns is what keeps this one dormant. A fix that adds `bytea` to the lists must not point it at that name.

`repro: static` for this arm, in contrast with the SQL Server arm above: the case lists and the generated column types are read off the code and the checked-in goldens, but **nothing has reconciled a real PostgreSQL table**, so the `data_type` spellings in the table above come from PostgreSQL's documented behaviour rather than from a measurement. What would confirm it is the obvious thing and `device-pgsql-port` is the ticket that can do it: create the corpus's `AllTypes` table through a PostgreSQL device, then reconcile it back and count the columns.

This arm does not change the root cause or the fix. Both devices drop an unknown column at the one site the section above names, `SQLDevice.GetDeviceTables`'s `if (ShouldIncludeColumn(...))` with no else branch, and the general change proposed there - name the table and the column rather than dropping it in silence - is what turns both devices' incomplete case lists from missing data into a message. The per-type mapping work is then separate, and for PostgreSQL it is mostly adding the spellings of types the device already knows how to write.
