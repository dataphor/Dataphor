description: A money amount with more than two decimal places - a unit price, a tax rate - is silently rounded when it is stored in a PostgreSQL database, and the rounded amount is what comes back.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLTypes.cs, libraries/PGSQLDevice/Source/SystemCatalog.d4, tests/goldens/devices/PGSQL/, tests/DAE.Server.Tests/LivePGSQLDeviceTest.cs
repro: verified
severity: corruption
likelihood: normal-use
tradeoffs: The fix changes the column type a PostgreSQL device creates for a D4 Money, and every existing PostgreSQL device table already has that column as `money`, so a deployment's schema and the device stop agreeing until the columns are altered; and the test corpus cannot see the loss, because both sides of its comparison are rounded by the same cast - so a maintainer may reasonably decide the deployments that exist all work in whole cents and leave it.
----
## What is wrong

The PostgreSQL device creates a D4 `Money` column as the PostgreSQL type `money` (`PostgreSQLMoney.InternalNativeDomainName`). PostgreSQL's `money` holds a fixed two decimal places, set by the server's `lc_monetary`:

```
postgres=# select 0.128::money as rounded, (0.128::money)::numeric as back;
 rounded | back
---------+------
   $0.13 | 0.13
```

Measured 2026-11-26 on PostgreSQL 18.7 with `lc_monetary = en_US.utf8`; the behaviour is the same on 13, 14 and 16. D4's `Money` carries more than two places, and the engine hands the full value to the driver, so the rounding happens in the server with nothing said about it. A unit price of `$0.128`, a tax rate, a currency with three minor digits, or any computed amount not already rounded to cents is stored as a different number from the one the application wrote, and that different number is what every later read returns.

SQL Server's `money` holds four decimal places, so the SQL Server device loses less and loses it further out. This ticket is about the PostgreSQL device, where two places is well inside what an ordinary application uses.

## Why no test catches it

The device-coverage corpus has a case for exactly this value - it inserts `$0.128` and asserts the stored value equals `$0.128` - and it **passes**. Both sides of that comparison go into the server as `money`, so the literal is rounded on the way in by the same cast that rounded the column, and `$0.13 = $0.13`. The corpus can only see an asymmetric loss. A test that would see this one has to read the value back into the engine and compare it there, which is worth writing with the fix.

## The decision

The honest column type for a D4 `Money` on PostgreSQL is `numeric` with a scale that covers it - `decimal(28, 8)` is what the device already uses for a D4 `Decimal`, and `tests/goldens/devices/PGSQL/Table.AllTypes.sql` shows the two side by side. Changing it is one line in `PostgreSQLTypes.cs` plus the goldens it moves, and it is a compatibility surface: every `Money` column a PostgreSQL device has created is a `money` column, so afterwards the device's `create table` no longer matches a schema it created itself. Reading an adopted one still works either way - `FindScalarType` maps `money` and `smallmoney` to `System.Money` - so what changes is the shape of new tables.

Two smaller things to settle with it. The `System.Money.AsString`, `ReadAsString` and `WriteAsString` translations in `SystemCatalog.d4` cast through `::money`, so they round too and would follow the column type. And `PostgreSQLMoney` sets `UseParametersForLiterals`, added by `device-pgsql-port` because PostgreSQL has no `money`/`numeric` comparison operator at all and a literal in the generated SQL therefore failed outright; on a `numeric` column that reason disappears, and whether to keep the parameter anyway deserves a sentence rather than a silent revert - the two goldens it moved are `Query.Restrict.Comparison.sql` and `Query.Extend.sql`.

Until it is decided, the user's guide says what it says now: use a `Decimal` column on a PostgreSQL device for an amount with more than two decimal places (`docs/users-guide/dataphor-service.md`, "Which PostgreSQL versions work").
