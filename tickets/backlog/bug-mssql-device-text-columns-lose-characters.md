description: When Dataphor creates a table in a SQL Server database, it makes the text columns single-byte, so text stored in them still loses any character the server's character set cannot hold - Chinese or Greek characters come back as question marks - even though Dataphor now sends the text correctly.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/SQLDevice/Source/SQLTypes.cs, libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLTypes.cs, libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLDevice.cs, src/engine/Server/Connection/MSSQLCommand.cs, tests/goldens/devices/MSSQL/, tests/DAE.Server.Tests/MSSQLVersionMatrixTest.cs
repro: static
severity: corruption
likelihood: normal-use
tradeoffs: Making the device create Unicode text columns doubles the storage every existing ASCII deployment uses for text, changes the shape of every table D4 creates from now on, and churns the SQL Server device's translation goldens - so a maintainer may reasonably decide that a deployment wanting Unicode text should declare those columns itself in SQL Server and let the device reconcile to them, and close this with documentation instead.

## What is wrong

`device-mssql-string-parameters` fixed how a text value is **sent** to SQL Server: every string parameter is now described as `nvarchar` and every `SQLText` parameter as `ntext`, so the value arrives at the server with all of its characters. It left untouched how the SQL Server device **declares a column** when D4 creates the table. For a D4 `String` column the device emits `varchar(n)`, and for an `SQLText` column it emits `text`, both single-byte. SQL Server converts the Unicode parameter to the column's collation code page on the way in, so a character that code page has no room for is still stored as a question mark, with no error raised.

So a deployment that lets Dataphor create its tables - which is the ordinary case, a D4 `create table ... in <device>` - still loses non-ASCII text, exactly as before the fix, while the deployment that maps D4 onto a hand-written `nvarchar` schema is now safe. The migration note says this, but a reader naturally expects the headline ("text written to SQL Server now arrives as Unicode") to cover the tables Dataphor made itself.

There is a second cost on the same tables. An `nvarchar` parameter is the higher-precedence type, so comparing it against an indexed `varchar` column in a SQL collation can no longer seek that index; that regression was measured and accepted as the price of correct text (the accepted-tradeoff `NOTE:` on `MSSQLCommand.GetParamTypeDescriptor` records it, 625 logical reads against 3 on a 200,000-row table). For a table the device created, the deployment pays that price and gets none of the correctness it was paid for, and the remedy the migration note offers - declare the column `nvarchar` - cannot be said in D4 at all. The only way to reach it today is to alter the column in SQL Server by hand and rely on the device reconciling to it; the next table D4 creates is `varchar` again.

## The two arms

**The column types the device emits.** The native type for a D4 `String` is `varchar(n)` in `SQLString.InternalNativeDomainName` (`libraries/SQLDevice/Source/SQLTypes.cs`), which the SQL Server device inherits unchanged; `SQLText` is `text`, from `MSSQLText.InternalNativeDomainName` in `libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLTypes.cs`. Visible in the device's own translation goldens: `tests/goldens/devices/MSSQL/Table.Employee.sql` creates `"Name" varchar(40)`. The base `SQLString` is shared with every other SQL device, and `nvarchar` is not a PostgreSQL type, so anything done here belongs to the SQL Server device rather than to the shared class.

**The length reconciliation records for an existing Unicode column.** When the device imports a schema it stores the column's length from `sys.columns.max_length` (`MSSQLDevice.FindScalarType`, by way of the catalog query in the same file), and that is a **byte** count: measured against SQL Server 2016 LocalDB on 2026-11-25, `nvarchar(200)` reports 400, `nchar(10)` reports 20, and `nvarchar(max)` reports -1. So the `Storage.Length` tag on the D4 column is twice the column's real width, and every parameter sent against that column is described twice as wide as it needs to be. Harmless until now; with `nvarchar` parameters it also means that any `nvarchar` column wider than 2,000 characters is described as `nvarchar(max)`, because the doubled number passes the 4,000-character limit a descriptor can name. Nothing is lost - a value still arrives intact, and an index key cannot be that wide anyway - but the parameter becomes a large-object parameter where a plain one would do, and this is the one place the promise "nothing at all changes for a schema whose text columns are `nvarchar`" is not quite kept. The two arms share a visit because both are the device's idea of what a SQL Server text column is.

## What a fix has to decide

Whether the SQL Server device should declare D4 text columns as `nvarchar(n)` and `ntext`, so that the column it creates and the parameter it sends agree by construction and no text can be lost on a schema Dataphor owns. That is the shape that retires the whole class rather than this instance of it, and it is a change a deployment can notice: text storage for ASCII data doubles, every table created from then on has a different native type (older tables are untouched and still reconcile, since the device already accepts `varchar`, `char`, `nvarchar` and `nchar` as D4 `String`), and the SQL Server device's translation goldens change. The alternatives are to give an application a way to ask for a Unicode column - a `Storage.*` tag or a device-specific scalar type mapping, which is more surface for the same result - or to decide that Dataphor-created tables are single-byte by design and say so plainly in the user's guide beside the sentence that now promises Unicode text.

Whichever is chosen, the byte-count arm wants the character width recorded instead, so that a reconciled `nvarchar(200)` column is described as `nvarchar(200)`.

## What would confirm it

Nothing in the test suite writes a row to a SQL Server table **through the device** and reads it back, so this was read out of the code and the goldens rather than observed: `repro: static`. What would settle it is one D4 script against a live SQL Server device - create a table with a string column, insert a value holding Chinese or Cyrillic characters, select it back - which is the harness `test-device-live` is building. The byte-count arm is already measured (`sys.columns.max_length` above) and needs no further proof.

## A third arm, from the same place: a D4 String column compares case-insensitively on SQL Server

Verified 2026-11-25 on `modernization`, against SQL Server 2016 LocalDB with its default (case-insensitive) collation. `test-device-live` ran the device-coverage corpus of `TestFramework.Coverage.Devices` against a real SQL Server device for the first time (1180 cases), and two of the failures are this one defect:

```
Device Case 761 failed: System.String compared insensitively.
Device Case 762 failed: System.String compared insensitively.
```

Those two cases put `"TEST CASE"` in a `System.String` column of a table on the device, read it back and assert it does **not** equal `"test case"`, and the reverse. The device answers that it does. D4's `System.String` is case-sensitive - that is what these assertions record - while the `varchar(n)` the device emits carries the database's collation, which on a default SQL Server install is case-insensitive, so a restriction, a key check or a join on a `String` column that the device pushes down gives a different answer from the same expression evaluated in the engine. The corpus has the parallel cases for `SQLDevice.SQLIText` (which is *meant* to compare insensitively) and for `SQLDevice.SQLText` beside them, so the distinction the device has to keep is already written down.

This is the same site as the two arms above - what the SQL Server device thinks a D4 text column is, `SQLString.InternalNativeDomainName` and the SQL Server device's own type mapping - and the same kind of answer: the column the device creates has to carry the semantics the D4 type promises, which for `String` means a case-sensitive (binary or `_CS_AS`) collation on the column as well as a Unicode one. Hence an arm here rather than a ticket of its own. It is a change a deployment can notice in the same way, and more sharply: adding `COLLATE` to a column changes which indexes a query can use and makes the device's emitted DDL differ from every existing table, so the alternative - declaring that a Dataphor-created `String` column inherits the database's collation, and saying so where the user's guide describes the device - has to be weighed against it.

Worth noting for whoever triages: the device already has an `IsCaseSensitive` property, but `MSSQLDevice.RegisterSystemObjectMaps` hard-codes `"false"` into the `SystemCatalog.d4` it runs (the `USEISTRING` branch beside it is compiled out), and the one definition of a `CoverageDevice` kept in the test tree has `//"IsCaseSensitive" = "true"` commented out - so the question was already open in 2008 and nothing in the tree answers it.

Reproducing it needs the one statement that `test-pre-3-1-scripts-policy`'s third arm asks the owner to rule on; until then `LiveDeviceTest` does not reach these cases.

## A fourth arm: the byte-count arm is now verified, and it costs more than "nothing is lost"

Verified 2026-11-26 by `device-sqlserver-version-matrix`, on SQL Server 2016 LocalDB, 2019 (LocalDB and a Linux container) and 2022, all four giving the same answer. The new fixture `tests/DAE.Server.Tests/MSSQLVersionMatrixTest.cs` creates a table on the server directly and reconciles it; an `nvarchar(30)` column arrives as a D4 `String` with `Storage.Length` 60, as the byte-count arm above said it would.

What the measurement adds is a consequence that arm did not have. Reconciliation does not only record the length as a tag: `SQLDevice.GetDeviceTables` compiles a column constraint from it, `LengthValid`, asserting `Length(value) <= 60`. So D4 accepts a 45-character value for a column that holds 30, and the rejection arrives from SQL Server when the row is written rather than from Dataphor when the value is entered - which for a user editing a form means a server error instead of a field-level message, and for a batch insert means a failure partway through. So the arm is not only a wider parameter descriptor; it is a length constraint that does not say what the column can hold.

The fix is the one that arm already named - record the character width rather than the byte count - and the place that decides it is the same `MSSQLDevice.FindScalarType`. The reconciliation query is in `GetDeviceTablesExpression` in the same file, which selects `convert(integer, sc.max_length) as Length` on SQL Server 9 and above and `convert(integer, sc.length)` below it; both are byte counts, so a correction belongs where the number is interpreted or in both queries, not in one.

Whoever takes this: the fixture above pins the current behaviour, so the fix has to change its expectation for `NVarcharColumn` from 60 to 30, and that is the one change to it that is correct to make.
