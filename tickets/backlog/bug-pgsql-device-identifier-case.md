description: Asking Dataphor to re-read one named table from a PostgreSQL database finds nothing, because it looks for the table under a lower-cased name while the table it created has capital letters in it.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLDevice.cs, libraries/SQLDevice/Source/SQLDevice.cs, src/engine/Store.PGSQL/PGSQLStoreConnection.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: Whichever way it is resolved, one of two kinds of schema stops working as it does today - one the device created, whose names have capital letters, or one a person created with unquoted names, whose names are lower case - so this is a choice about which to favour rather than a correction; and a per-table reconciliation is not something most deployments do, so a maintainer may reasonably leave it and say in the documentation that reconciliation on this device works per device rather than per table.
----
## What is wrong

PostgreSQL folds an unquoted identifier to lower case and preserves a quoted one. The PostgreSQL device does both, in opposite directions:

- **It creates** its tables with quoted identifiers, so a D4 table variable `ListMoneyTestTable` becomes a PostgreSQL table literally called `ListMoneyTestTable`. That is the shared emitter's doing (`EmitIdentifier` quotes every name) and it is visible in every golden under `tests/goldens/devices/PGSQL/`.
- **It reads** a single named table by a lower-cased name. All three per-table reconciliation queries in `PostgreSQLDevice.cs` end with a filter built as `ToSQLIdentifier(tableVar).ToLower()`: `GetDeviceTablesExpression`, `GetDeviceIndexesExpression` and `GetDeviceForeignKeysExpression`.

So a per-table reconciliation of a table the device itself created matches nothing. Verified 2026-11-26 against PostgreSQL 18.7, running the filter the device builds against a table the device had created in the same database:

```
postgres=# create table "MixedCase" ("ID" integer not null);
postgres=# select count(*) from information_schema.columns where table_name = lower('MixedCase');
 count
-------
     0
postgres=# select count(*) from information_schema.columns where table_name = 'MixedCase';
 count
-------
     1
```

The path that reaches it is `SQLDevice.GetDeviceCatalog(process, serverCatalog, tableVar)` with a table variable - the per-table reconciliation, which is what `Reconcile(device, table)` and the engine's own reconciliation of one table variable use. The whole-device path (`GetDeviceCatalog` with a null table variable, which `AvailableTables` and `Reconcile(device)` use) passes no filter at all and therefore works: it lists everything in the non-system schemas, mixed-case names included. That is why nothing noticed - the device-coverage corpus and every other pass reconcile per device or not at all.

A reconciled table whose columns silently come back empty is the bad shape here: `GetDeviceTables` adds what the query returned, so the result is a table variable with no columns, or no table variable at all, rather than an error.

## Why the lower-casing is there, and the decision

It is not a slip. A PostgreSQL schema a person wrote by hand has unquoted, therefore lower-case, names, and a deployment adopting such a schema needs exactly this filter: `ToSQLIdentifier` gives the D4 name in its own case, and lower-casing it is what matches `invoices` for a table variable called `Invoices`. The catalog store has the same line for the same reason (`PGSQLStoreConnection.HasTable` lower-cases before querying `PG_TABLES`), and there it is right, because that store's own catalog tables are in one case throughout.

So the device supports two kinds of schema and the filter can only match one:

- **Tables the device created.** Quoted, mixed case. These need the name as it stands.
- **Tables a person created unquoted.** Folded, lower case. These need the lower-cased name.

Three ways out, and the choice is the ticket:

1. **Match either.** Filter on `table_name in (<name>, lower(<name>))`, or compare case-insensitively (`lower(table_name) = lower(<name>)`) and let the device sort out which row it got. Cheapest, and it makes an adopted lower-case schema and a device-created mixed-case one both work - at the cost of two tables differing only in case becoming ambiguous, which is legal in PostgreSQL and which the device would then have to choose between.
2. **Stop quoting.** Make this device emit unquoted identifiers, so everything it creates is lower case and the existing filter is right. That changes every statement the device generates and every golden, and it breaks every table a PostgreSQL device has already created.
3. **Match exactly, and say that an adopted schema needs its names spelled as the database holds them** - which for a lower-case schema means D4 table variables with lower-case names, or a `Storage.Name` tag. Correct and the least code; it moves work onto the deployment.

The first keeps both kinds of schema working. Whoever takes it should change the two sibling filters at the same time - the index and foreign-key queries carry the same line - and add the case to a live fixture, because nothing in the suite reconciles a single table on this device today.
