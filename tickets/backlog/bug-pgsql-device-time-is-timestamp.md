description: A column holding a time of day in a PostgreSQL database can be written but not compared, so any part of an application that uses one fails against PostgreSQL.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/PGSQLDevice/Source/SystemCatalog.d4, libraries/PGSQLDevice/Source/SystemCatalog.sql, libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLTypes.cs, libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLDevice.cs, tests/DAE.Server.Tests/LivePGSQLDeviceTest.cs
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: Either half of the fix changes something a deployment can already have - the column type a PostgreSQL device creates for a D4 Time, or the translation strings the device writes into a catalog - so neither can be done quietly; and D4's Time is a niche type, so a maintainer may reasonably decide to document "do not use a Time column on a PostgreSQL device" and leave the mapping alone.
----
## What is wrong

The PostgreSQL device declares a D4 `Time` column as the PostgreSQL type `time` (`PostgreSQLTime.InternalNativeDomainName` in `PostgreSQLTypes.cs`), and then registers, over that column, a set of operators every one of which is written for `timestamp`. The two never meet. Verified 2026-11-26 against PostgreSQL 13.23, 14.25, 16.16 and 18.7 - identically on all four - through the device-coverage corpus:

```
Device Case <n> failed: 42883: operator does not exist: time without time zone <> timestamp without time zone
```

A `Time` value can be *written*: `device-pgsql-port` fixed the parameter binding, so an insert works. It cannot be compared, restricted on, joined on or selected by key, because each of those compiles to a comparison between the column and an operator result, and the operator results are timestamps.

The mismatch is in `SystemCatalog.d4`, the script the device runs to register its own type and operator maps, and in the support functions `SystemCatalog.sql` installs:

- `System.Time()` translates to `DAE_DTTimePart(now()::timestamp)`, and `DAE_DTTimePart` is declared `RETURNS timestamp`.
- `System.Time(Integer, Integer)` and the three-argument form translate to `DAE_DateTimeSelector(1900, 1, 1, ...)`, which also returns `timestamp`.
- `System.Time.AsString`, `WriteAsString`, `Ticks` and `WriteTicks` each translate to an expression ending `::timestamp` or built from a `timestamp` literal.
- `System.Time.WriteHour`, `WriteMinute` and `WriteSecond` are mapped to the `DAE_DTWriteHour`, `DAE_DTWriteMinute` and `DAE_DTWriteSecond` functions, which take and return `timestamp` and are shared with `System.DateTime`.
- `System.TimePart(System.DateTime)` is mapped to `DAE_DTTimePart`, so it is a `Time`-valued operator returning a `timestamp` too.

Eleven registered translations and four SQL functions disagree with one column type. The device's own `FindScalarType` has `// TODO: Support time type` with the `time` case commented out, which says the same thing from the reconciliation side: nothing about `Time` on this device was finished.

## The decision, which is why this is a ticket rather than a fix

There are two coherent shapes and each breaks something that exists.

**Make the column a `timestamp`.** One line in `PostgreSQLTypes.cs`, and everything above is already consistent with it: a `Time` would be a timestamp on 1900-01-01 whose date part is ignored, which is exactly what the support functions already build and what the SQL Server device does with its own `datetime`. The cost is that every `Time` column a PostgreSQL device has already created is a `time` column, so after the change the device's `create table` disagrees with the schema it created itself, and an adopted `time` column stops reconciling at all.

**Make the operators produce `time`.** Add `::time` to the seven translation strings that end in a timestamp, and give `DAE_DTTimePart` and the three `DAE_DTWrite*` functions `time`-returning siblings - they cannot simply change signature, because `System.DateTime` uses them too. Column types stay as they are and nothing on disk moves. The cost is that these translation strings are *catalog data* - a device's operator map is written when the device starts and carried in a rebuilt catalog - so the change arrives with a catalog rebuild, and eleven translation strings are eleven chances to get a dialect detail wrong with only the corpus to catch it.

The second preserves what deployments have, and is the larger piece of work. Whoever decides should also say what `System.TimePart(DateTime)` returns, since it is the one operator in the set that is `Time`-valued but lives among the `DateTime` maps.

## How to reproduce

```
docker run -d -e POSTGRES_PASSWORD=<a throwaway password> -p 15418:5432 postgres:18
dotnet build Dataphor.slnx
DATAPHOR_TEST_PGSQL='Host=localhost;Port=15418;Username=postgres;Password=<the same>' dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~LivePGSQLDeviceTest"
```

The fixture reports the whole roll-up as a warning; the message above is the first failure in the `Time` family. `LivePGSQLDeviceTest.KnownFailureFamilies` records that message, so **a fix here makes that fixture fail until its entry is removed**, which is the entry this ticket drains.

One thing to fix with it. Because a PostgreSQL transaction is unusable after any failed statement (`bug-pgsql-connection-discards-the-error-code`), this one failure currently hides about sixty later cases of the corpus behind it. Either ticket landing makes the other's measurement sharper; this one is the cheaper to try first.
