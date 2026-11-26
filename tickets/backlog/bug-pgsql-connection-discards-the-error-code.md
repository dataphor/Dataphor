description: When a statement fails against a PostgreSQL database, Dataphor throws away the database's own error code, so it cannot tell a rule the user broke from a connection that has died - and it does not notice that PostgreSQL has abandoned the whole transaction, so everything after the failure fails too.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/PGSQLConnection/Source/PostgreSQLConnection.cs, src/engine/DAE/Connection/SQLConnection.cs, src/engine/Server/Connection/MSSQLConnection.cs, libraries/SQLDevice/Source/SQLDevice.cs, tests/DAE.Server.Tests/LivePGSQLDeviceTest.cs
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: The first half is small but it changes the exception an application sees from a PostgreSQL device, which a D4 handler or a client could be matching on; the second half is a question about what a nested D4 transaction means at the database, and answering it properly means savepoints in shared code that both devices go through - much larger than the symptom suggests. A maintainer may reasonably take the first half alone and document the second.
----
## What is wrong

`PostgreSQLConnection.InternalWrapException` flattens every driver exception into a plain `Exception` carrying only the message text:

```csharp
return new ConnectionException(ConnectionException.Codes.SQLException, ErrorSeverity.Application, new Exception(exception.Message), statement);
```

That is 3.1 code and the port left it alone deliberately, with a `NOTE:` saying so, because nothing could see the difference until a device ran against a server. One now has. Two things follow, and the second is much the more serious.

**One: the error code is gone.** `PostgresException.SqlState` is the only thing that says what kind of failure it was. `MSSQLConnection` reads the equivalent and answers two questions with it - `IsUserCorrectableError`, which decides the severity the engine reports to a user, and `IsTransactionFailure`, below - and `PostgreSQLConnection` answers neither. A unique-constraint violation, a permission failure and a dropped connection all arrive as the same application-severity exception with the server's text inside it.

**Two: a PostgreSQL transaction is dead after any failed statement, and nothing notices.** This part has no SQL Server equivalent. On SQL Server a failed statement usually leaves the transaction usable, so the engine can catch the error and carry on. On PostgreSQL the transaction enters an aborted state and every later statement on that connection fails with

```
25P02: current transaction is aborted, commands ignored until end of transaction block
```

until the transaction ends. `SQLConnection.WrapException` has the hook for exactly this - it sets `_transactionFailure` when `IsTransactionFailure(exception)` says so, which is how `SQLDeviceSession.IsTransactionFailure` tells the engine the transaction is gone - and the base implementation returns `false`, so for PostgreSQL it is never set.

**Measured blast radius.** Verified 2026-11-26 against PostgreSQL 13.23, 14.25, 16.16 and 18.7, identically on all four. In the 1,180-case device-coverage corpus (`LivePGSQLDeviceTest`), which the engine runs inside one transaction, the *first* server-side failure is followed by about sixty cascaded ones: five more cases report 25P02 directly and fifty-three report `An object named "..." is already defined`, because the case that failed could not run its own `drop table` either. One real failure, sixty-odd reported. An application does the same thing on a smaller scale whenever it catches a device error inside a transaction and carries on.

## The two halves

**The classification.** Override `IsTransactionFailure` to answer true for the failures that have ended the transaction, and stop flattening - or flatten while keeping the code. PostgreSQL's SQLSTATE is five characters whose first two are the class, which makes this a short table rather than a list of numbers: class `25` is an invalid transaction state, class `40` a rollback (`40001` serialization failure, `40P01` deadlock detected), class `08` a connection exception. Strictly, *any* error leaves a PostgreSQL transaction aborted unless a savepoint is in play, so the honest answer may be "true whenever `InTransaction`", which is both simpler and a bigger behavioural change than a table. Decide which, and say why, at the site. The flattening's own stated reason - "so that it crosses the boundary" - predates the current serialization rules and should be checked rather than preserved: the SQL Server connection class passes the driver's exception through as the inner exception and nothing goes wrong.

**What the engine then does about it.** Telling the engine the transaction is gone makes a failure *honest*; it does not make a nested D4 transaction recoverable. `SQLDeviceSession.InternalBeginTransaction`, `InternalCommitTransaction` and `InternalRollbackTransaction` all act only at `Transactions.Count == 1`: a nested D4 transaction is not reflected at the database at all, so there is nothing for the engine to roll back to. On SQL Server that costs nothing visible; on PostgreSQL it is why the corpus's own per-case `try ... commit; except ...` cannot save the connection. Making it work means emitting `savepoint` and `rollback to savepoint` for the nested levels, which is a change in `libraries/SQLDevice` that both devices go through and that needs savepoint support on `SQLConnection`. `src/engine/DAE/Store/SQLStore.cs` opens with a note saying the *store* layer logs and replays nested transactions "to make up for the lack of savepoint support in the target DBMS", so the shape of the problem is already recognised one layer over.

Whoever takes this should land the first half on its own and file the second if it is not done with it: the first half is what turns a silent cascade into a diagnosable one.

## How to reproduce

```
docker run -d -e POSTGRES_PASSWORD=<a throwaway password> -p 15418:5432 postgres:18
dotnet build Dataphor.slnx
DATAPHOR_TEST_PGSQL='Host=localhost;Port=15418;Username=postgres;Password=<the same>' dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~LivePGSQLDeviceTest"
```

The roll-up the fixture reports as a warning shows the cascade: one `42883` followed by `25P02` and `already defined`. `LivePGSQLDeviceTest.KnownFailureFamilies` records the `25P02` message, so a fix here makes that fixture fail until its entry is removed. Turning on statement logging on the server (`alter system set log_statement='all';` then `select pg_reload_conf();`, and read `docker logs`) is how the cascade was read statement by statement, and is the fastest way to see it again.
