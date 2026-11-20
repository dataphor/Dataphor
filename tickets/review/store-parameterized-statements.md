description: The catalog store now passes the values of its insert, update and delete statements to the database as parameters instead of pasting them into the SQL text, which fixes a value that the old quoting could not carry.
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, src/engine/Store.PGSQL/PGSQLStoreConnection.cs, tests/DAE.Server.Tests/StoreContractTests.cs, docs/migration-notes.md, docs/modernization-plan.md, tickets/plan/store-mssql-port.md
----
## What landed

The catalog store's generated insert, update and delete carry their values as ADO.NET parameters. The `TODO: Statement parameterization?` at the old `src/engine/DAE/Store/SQLStoreConnection.cs:391` is gone.

Everything goes through the existing Dataphor connection abstraction rather than raw ADO.NET: `SQLCommand` already holds an `SQLParameters` collection, rewrites `@name` markers per back end (`SQLCommand.PrepareStatement`) and binds values (`DotNetCommand.SetParameters`), and the SQLite command binds positionally (`SQLiteCommand` sets `_useOrdinalBinding`). The ticket's sketch of an `IDbCommand BuildParameterizedStatement` would have gone around all of that; nothing in the store sees an `IDbCommand`.

New on `SQLStoreConnection`:

- `ExecuteStatement(string statement, SQLParameters parameters)`. The old one-argument overload delegates to it with null and still serves seeding and scripts. Both, and `ExecuteScalar`, now set the command up through one private `PrepareExecuteCommand`, which **clears** the command's parameters first — the execute command lives as long as the connection, and a parameter left behind would bind to an `@name` in a later statement.
- `virtual SQLParameter NativeToParameter(string name, object value)` — the per-store mapping point, the counterpart of the existing `NativeToLiteralValue`. Maps `bool`, `string`, `int`, `long`, `short`, `byte`; an unmapped type is `Error.Fail`, not a silent `ToString()`.
- `private AppendValue(statement, parameters, value)` — appends one value site as either a marker or a literal.
- `private ExecuteWriteStatement(statement, parameters, Func<string> literalStatement)` — executes and, on a generic SQL failure, re-raises naming the statement with its values rendered in.

`GenerateInsertStatement`, `GenerateUpdateStatement`, `GenerateDeleteStatement` (all `protected virtual`) and the private `AppendKeyCondition` each take an extra last argument, an `SQLParameters`: non-null gives the parameterized form and fills the collection, null gives exactly the old literal text. One generation tree, one branch per value site. `PerformInsert`/`PerformUpdate`/`PerformDelete` pass a collection; `LogInsert`/`LogUpdate`/`LogDelete` pass null, because the undo log is replayed as text.

Three decisions the ticket left to implement time, and why:

- **Two shapes of generator, not literal substitution into parameterized text.** The ticket offered either. Re-rendering through the same tree needs no scan of the SQL for `@P1` versus `@P10`, and the literal form is then byte-identical to what shipped before — which is what makes the undo log and the error path safe.
- **Parameter names are `@P0`, `@P1`, … positional in order of appearance.** Capital `P` matches `ServerCatalogDevice.TranslateScalarParameter`, which already parameterizes the catalog's *read* path as `P1`, `P2`, …, and matches `MSSQLCommand.ConvertParameterName`, which escapes a `Pnnn` name to avoid colliding with the provider's own scheme. Positional because the SQLite command binds by position.
- **A boolean goes as the integer 1 or 0, with `SQLIntegerType(1)`.** Identical to the catalog read path's `System.Boolean` → `SQLIntegerType(1)` and to what the old literal wrote, and right for the `tinyint` columns the catalog declares. The PostgreSQL store, whose catalog declares them `boolean`, gets an override.

Two places keep literals on purpose, each with a `NOTE:` at the site:

- **A null key value** stays `is null` in `AppendKeyCondition`. A parameter makes it `= @P`, which is unknown rather than true against a null, so the statement would name no row.
- **A null value** anywhere stays the literal `null`. Every back end spells it the same, it carries no quoting or culture question, and a parameter bound to `DBNull` would have to be given a type the value itself does not state. This was not in the ticket; it is the reason `NativeToParameter` is never called with null.

## Verification

- `dotnet build Dataphor.slnx --no-incremental`: 0 errors, 143 warnings — the same count as HEAD.
- Suite part 1 (`class != CoverageScripts`): 369 passed, 0 failed in `DAE.Server.Tests`; 13 passed, 0 failed in `Common.Tests`. 2 m 59 s and 3 m 20 s on two runs of the final code.
- Part 2 (coverage scripts `[A-Ra-r]`): 67 passed, 0 failed, 11 skipped, 3 m 14 s.
- Part 3 (coverage scripts `[^A-Ra-r]`): 48 passed, 0 failed, 14 skipped, 3 m 14 s.
- `SQLiteStoreContractTests`: 14 green (11 inherited, 3 new). `CatalogStoreTest` and `CatalogStoreConcurrencyTest`: 29 green.
- No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written. The three records in `tickets/.pre-existing-known.md` are all `one-off` and none appeared.

Skipped counts in parts 2 and 3 are the manifest's known/unstable/fragment coverage scripts, unchanged.

## Tests added, and what each one pins

| Test | Pins | Shown able to fail |
|---|---|---|
| `AwkwardStringRoundTripsThroughWrites` | A string holding `';'`, `');'` and an embedded NUL survives an insert, an update, a key condition that names the row by it, and a delete. | **Yes, on HEAD.** Before the change it failed on the insert with `SQL logic error / unrecognized token: "'a'';''b'');''c"` — the NUL ends the text the provider prepares, so the statement was truncated mid-literal. This is the behaviour the change fixes. |
| `BooleanRoundTripsThroughWrites` | A boolean survives the values list of an insert and the set list of an update, against a `tinyint` column. | **Yes**, by mutation: writing the bool as text (`SQLStringType(5)`, `ToString()`) fails this test and only this test. See the caveat below. |
| `CursorEndsScanWhenUpdateMovesRowOutOfRange` | An `Update()` that moves the cursor's row out of its range ends the scan on `Next()`. This is the umbrella's open case the previous tickets left, and is independent of parameterization. | **Yes**, by mutation: dropping the end-of-range term from `SQLStoreCursor.Next()` fails it (along with `CursorMovesFromDeletedRow` and `CursorMovesFromUpdatedRow`). Passes unchanged on HEAD, as a characterization test should. |

Two helpers were added beside the existing ones: `OpenCursorKeyedOnValue` (a cursor that names rows by the text column, the shape of `DAEUsers` and `DAELoadedLibraries`, so that a generated key condition carries a string) and `CreateBooleanTable`/`OpenBooleanCursor`.

No other test was added. The parameterized path itself is exercised by every one of the 369 part-1 tests, because every server start populates a catalog through it.

## Gaps and judgements for the reviewer

**`NativeToLiteralValue` was not deleted, and the culture `NOTE:` stays.** The ticket asked for both once nothing called it; three things still call it, confirmed by `grep -rn NativeToLiteralValue --include=*.cs`:

- `SQLStoreCursor.GetReaderStatement` renders a ranged reader's origin values as literals. That is the *read* path and the ticket put it out of scope ("Reaches only the statement path").
- The literal shape of the three generators, for the undo log.
- `PGSQLStoreConnection` overrides it.

So the method is still live and the invariant-culture warning is still true of it. The `NOTE:` was reworded to say which statements still come through it and that the writes no longer do, and a second `NOTE:` at `GetReaderStatement` names it as the store's remaining literal site with a pointer to `NativeToParameter`. **If the reviewer thinks the reader statement should have been in scope, that is a real scope question, not an oversight** — it is a read of values this same store wrote, so it is not an injection surface, but it is the last place a `double` column would become culture-dependent.

**The error-path improvement is narrower than the ticket assumed, and the ticket's `CatalogException` claim is wrong.** Probed empirically with a throwaway test (written, run, deleted):

- A duplicate primary key comes back as a plain `DataphorException`, severity User, code 500000, message `constraint failed`, **with no statement text at all** — `SQLiteConnection.InternalWrapException` maps a `Constraint` result code to that shape. That was true before this change too. So the commonest store write failure never named the row, with literals or with parameters, and `ExecuteWriteStatement` deliberately does not touch it.
- A generic SQL failure (probed with a value count that does not match the table) comes back as `ConnectionException` and now carries `insert into T values (2, 2)` — the literal-rendered form, where the on-wire text was `insert into T values (@P0, @P1)`. So the re-raise works, for that class of failure.
- A store lock still comes back as `StoreException` 130103 (`StoreLockWaitReportsStoreLocked`, green), untouched, because the catch is narrowed to `ConnectionException`.
- The ticket's "three catalog error codes from `CatalogException`" does not correspond to anything: every `CatalogException` code is about store configuration, not statement failure. The three that exist and were run green are `SQLCESettingsAreRejected` (115108), `UnloadableStoreClassIsReported` (115107) and `SetObjectOwnerOfUnknownObjectReportsCatalogObjectExpected` (113177, a `CompilerException`).

**Worth a reviewer's eye on `ExecuteWriteStatement`.** It rethrows `new ConnectionException(SQLException, severity, inner, literalStatement)` with the original as inner exception, so the outer object is a new `ConnectionException` of the same code. Nothing in the store or catalog catches a specific exception type from a write (both relevant catch sites, `CatalogStore.cs:124` and `CatalogDeviceSession.cs:1502`, catch `Exception`), and the connection's transaction-failure flag is already set by `WrapException` before the rethrow. But it does nest a `ConnectionException` inside a `ConnectionException` and would flatten a provider-specific subclass to the base. A reviewer may prefer a different shape; the alternative considered was leaving the message naming `@P0`, which loses the row.

**The boolean test cannot distinguish 1/0 from a bound bool on SQLite.** Measured: mutating `NativeToParameter` to bind the `bool` itself (`SQLBooleanType`, what the PostgreSQL store does) leaves all 14 tests green, because `System.Data.SQLite` stores a bool as an integer anyway. So on the only store we build, this test pins "a boolean round-trips" and not "the mapping is 1/0". It is not vacuous (mutation B above), and the mapping it documents is the thing a *new* store would get wrong, which is the fixture's stated job. The reviewer may judge it as not earning its place; the ticket left this to "reviewer's call" and the 1/0 mapping is also covered end-to-end by every catalog test.

**`PGSQLStoreConnection.NativeToParameter` is uncompiled and untested.** `src/engine/Store.PGSQL/DAE.PGSQL.csproj` is in neither `Dataphor.slnx` nor `tools/net-spike/projects.txt`, so nothing builds that file today. The six-line override was still written, rather than left as a note, because without it that store would send integers to the `boolean` columns its own `SystemStoreCatalog.sql` declares — a defect that fires the moment anyone revives the project, not a conditional one. It mirrors the `NativeToLiteralValue` override immediately above it. The file is not UTF-8 (its copyright sign is a single `0xa9` byte); that was preserved.

**`Error.Fail` rather than a new store error code** for a value type with no parameter mapping. It throws `AssertionException("Internal Error: …")`, matches `SQLStoreCursor.CompareKeyValues` a few hundred lines away, and adds no error code to a compatibility surface for a branch that nothing can reach today. Grepped to confirm nothing can: every column in `src/engine/Store.SQLite/SystemStoreCatalog.sql` is `int`, `tinyint`, `nvarchar`, `nchar` or `ntext` (37 `int` and 8 `tinyint` by tally), every `CatalogStore.InsertRow` call site passes ints, strings or bools, and the cursor path writes back what `StoreToNativeValue` produced. **No `double`, `date` or `decimal` reaches the statement path**, which was a TODO item; nothing has snuck in since `bug-store-nested-rollback-keeps-rows`. A reviewer who wants a real error code instead would need `StoreException` 130104 plus its `.resx` entry plus a migration-notes line.

**One allocation per write that could be removed.** `ExecuteWriteStatement` takes a `Func<string>` so the literal form is rendered only on failure; that is a closure and a display class per write. The alternative is three near-identical try/catch blocks in `PerformInsert`/`PerformUpdate`/`PerformDelete`. Left as the closure.

## Measurement

`StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart`, four runs each side, the printed `register+security+DDL` window:

| | runs (ms) |
|---|---|
| before | 975, 1461, 1441, 1466 |
| after | 916, 1420, 1361, 1328 |

Suite part 1 wall time (NUnit's reported duration for `DAE.Server.Tests`), which is the part the store change can move because almost every test there starts a server and populates a catalog:

| | runs |
|---|---|
| before (HEAD engine, tests present) | 2 m 54 s, 3 m 01 s |
| after | 2 m 59 s, 3 m 14 s, 3 m 20 s, 3 m 24 s, 3 m 25 s, 3 m 35 s |

**Read: no detectable change either way.** The bands overlap and this machine's spread is wider than any effect — the same `StoreRebuildWorkflowTest` varies 975–1466 ms run to run. Worth saying plainly because an earlier, smaller sample (before the 2 m 59 s reading) looked like a consistent 11 % slowdown, and the `NOTE:` and the plan row were first written that way and then corrected; the numbers above are the full set.

The ticket's expectation of 20–40 % faster on batched writes **did not appear.** The reason is parked as a tripwire at `AppendValue`: `DotNetCommand.PrepareCommand` assigns `IDbCommand.CommandText` on every execute even when the text has not changed, so the reuse of one prepared statement across rows — the whole source of the hoped-for gain — may never happen. That is in `DotNetCommand`, shared with every device connection, and well outside this ticket.

The before-numbers for parts 2 and 3 were not captured. Taking them needs the engine change reverted for about fifteen minutes of test running, and a run killed in that window would leave the tree without the implementation; the part-1 measurement was taken that way with the two files hashed and restored (verified `sha256sum -c`), and the exposure was kept to one part. Parts 2 and 3 are coverage scripts whose cost is per-script server startup, so they would say less than part 1 does.

## Tripwires recorded

- `SQLStoreConnection.AppendValue` — per-value allocation here and in the provider, with the measurements above and what to check first if catalog write throughput ever matters.
- `SQLStoreCursor.GetReaderStatement` — the store's remaining literal site, why it is harmless today, and where the parameter form is if a ranged read ever misbehaves on a value a literal cannot carry.
- `SQLStoreConnection.AppendKeyCondition` — why `is null` must stay a literal.
- `SQLStoreConnection.ModifyOperation` — why the undo log keeps literal statement text.

## Tickets filed: none. One arm appended

`grep -rl` over the open stages for the paths in `files:` hit `tickets/plan/store-mssql-port.md`, which already lists `MSSQLStoreConnection.cs` and `StoreContractTests.cs`. Appended one bullet to its *Edge cases* section rather than filing anything: the base's integer mapping is right for that store's `tinyint` columns so it needs no override, but `MSSQLCommand.PrepareParameters` maps `SQLStringType` to `SqlDbType.VarChar` while every text column in that store's catalog is `nvarchar`. That mapping is pre-existing and shared with the catalog read path (`ServerCatalogDevice` has always passed `SQLStringType(200)` against the same columns), so it is not a defect this change introduced — but writes now go through it too, and `AwkwardStringRoundTripsThroughWrites` is the test most likely to find it. Its two "11 tests" counts were corrected to 14 at the same time.

Nothing else reached the filing bar: there is no class-level invariant left to name, and the one real latent defect found (the PostgreSQL boolean mapping) was fixed in place.

## Docs

- `docs/migration-notes.md` — one entry under *Custom C# extensions*: the three `Generate...Statement` signatures, the two new members, what stays literal, and that a custom store overriding a generator adds the argument while one with boolean columns overrides `NativeToParameter`.
- `docs/modernization-plan.md` — a **Done** annotation on the WS4 row carrying what the scope did not anticipate (the two deliberate literals, the reader statement left out, and the measurement), and the WS11 security finding bullet rewritten from "the catalog store builds SQL by string concatenation" to what is now true.

## Source hygiene

Every changed file keeps its byte-order mark, CRLF endings and tab indentation; `git diff --numstat` shows only the intended lines, no renormalization. `core.autocrlf` is `true` here and `.gitattributes` deliberately marks no text types, which is why a working-tree CRLF file can sit on an LF blob — checked per file against `git cat-file`.
