description: The catalog store now hands the values of its insert, update and delete statements to the database as parameters instead of pasting them into the SQL text, which fixes a value that the old quoting could not carry.
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/Store.PGSQL/PGSQLStoreConnection.cs, tests/DAE.Server.Tests/StoreContractTests.cs, docs/migration-notes.md, docs/modernization-plan.md, tickets/plan/store-mssql-port.md
----
## What landed

The catalog store's generated insert, update and delete carry their values as ADO.NET parameters. The long-standing `TODO: Statement parameterization?` in `SQLStoreConnection` is gone. Implement stage: commit `ticket(implement): store-parameterized-statements`.

Everything goes through the existing Dataphor connection abstraction rather than raw ADO.NET: `SQLCommand` already holds an `SQLParameters` collection, rewrites `@name` markers per back end (`SQLCommand.PrepareStatement`) and binds values (`DotNetCommand.SetParameters`), and the SQLite command binds positionally.

New on `SQLStoreConnection`:

- `ExecuteStatement(string statement, SQLParameters parameters)`. The old one-argument overload delegates to it with null and still serves seeding and scripts. Both, and `ExecuteScalar`, set the command up through one private `PrepareExecuteCommand`, which clears the command's parameters first — the execute command lives as long as the connection, so a parameter left behind would bind to an `@name` in a later statement.
- `virtual SQLParameter NativeToParameter(string name, object value)` — the per-store mapping point, the counterpart of the existing `NativeToLiteralValue`. Maps `bool`, `string`, `int`, `long`, `short`, `byte`; an unmapped type is `Error.Fail`, not a silent `ToString()`.
- `private AppendValue(statement, parameters, value)` — appends one value site as either a marker or a literal.
- `private ExecuteWriteStatement(statement, parameters, Func<string> literalStatement)` — executes and, on a generic SQL failure, re-raises naming the statement with its values rendered in.

`GenerateInsertStatement`, `GenerateUpdateStatement`, `GenerateDeleteStatement` (all `protected virtual`) and the private `AppendKeyCondition` each take an extra last argument, an `SQLParameters`: non-null gives the parameterized form and fills the collection, null gives exactly the old literal text. One generation tree, one branch per value site. `PerformInsert`/`PerformUpdate`/`PerformDelete` pass a collection; `LogInsert`/`LogUpdate`/`LogDelete` pass null, because the undo log is replayed as text.

Three decisions the ticket left to implement time:

- **Two shapes of generator, not literal substitution into parameterized text.** Re-rendering through the same tree needs no scan of the SQL for `@P1` versus `@P10`, and the literal form is then byte-identical to what shipped before — which is what makes the undo log and the error path safe.
- **Parameter names are `@P0`, `@P1`, … positional in order of appearance.** Capital `P` matches the catalog read path (`ServerCatalogDevice.TranslateScalarParameter`) and `MSSQLCommand.ConvertParameterName`, which escapes a `Pnnn` name to avoid colliding with the provider's own scheme. Positional because the SQLite command binds by position.
- **A boolean goes as the integer 1 or 0, with `SQLIntegerType(1)`.** Right for the `tinyint` columns the catalog declares, and identical to the read path. The PostgreSQL store, whose catalog declares them `boolean`, gets an override.

Two places keep literals on purpose, each with a `NOTE:` at the site: a **null key value** stays `is null` in `AppendKeyCondition`, because a parameter makes it `= @P`, which is unknown rather than true against a null, so the statement would name no row; and a **null value** anywhere stays the literal `null`, because every back end spells it the same and a parameter bound to `DBNull` would have to be given a type the value itself does not state. The second is why `NativeToParameter` is never called with null.

`NativeToLiteralValue` stays, and so does its invariant-culture `NOTE:`: three callers remain — the ranged reader statement a store cursor opens (the read path, which the ticket put out of scope), the literal arm of the three generators, and the PostgreSQL override.

Three tests were added to `StoreContractTests` (11 → 14): `AwkwardStringRoundTripsThroughWrites` (the reproduction — a string holding `';'`, `');'` and an embedded NUL through an insert, an update, a key condition that names the row by it, and a delete; it fails on HEAD with `unrecognized token`, because the NUL ends the text the provider prepares), `BooleanRoundTripsThroughWrites`, and `CursorEndsScanWhenUpdateMovesRowOutOfRange` (the cursor-range case the previous tickets left open, independent of parameterization).

## Review findings

The implement diff was read before the handoff. Its claims were then checked against `SQLCommand`, `DotNetCommand`, `SQLiteCommand`, `MSSQLCommand`, `PostgreSQLCommand`, `SQLParameter`, `SQLConnection`/`SQLiteConnection` exception wrapping, both stores' `SystemStoreCatalog.sql`, and `CatalogStore`. No engine source was mutated during review; the four findings fixed below are comments, a test doc comment and documentation.

### Checked and sound

- **Parameter naming cannot collide.** `SQLCommand.PrepareStatement` reads a marker name greedily over identifier characters, so `@P1` and `@P10` are distinct names and the shorter cannot shadow the longer — the hazard the implementer avoided by re-rendering rather than substituting is genuinely absent from the chosen shape too.
- **Positional binding order is right.** `AppendValue` appends in statement order and adds nothing for a null (which stays a literal), so the collection's order is the markers' order of appearance; with `_useOrdinalBinding` the SQLite command creates its `?` parameters in exactly that order.
- **The values do get bound.** `SQLParameter(name, type, value)` is the right overload of six, and `SQLDirection.In` is the zero member of its enum, so the default direction passes `DotNetCommand.SetParameters`' `In || InOut` test. A wrong default there would have bound nothing, and that is the kind of thing a green suite can hide, since the catalog would then write nulls into columns it also reads back.
- **Every type `NativeToParameter` emits is mapped by every provider it can reach.** SQLite: `SQLStringType` → `DbType.String`, `SQLIntegerType` 1/2/4/8 → `Byte`/`Int16`/`Int32`/`Int64`. SQL Server: the same shapes, and `MSSQLCommand` runs `ConvertParameterName` over *both* the statement text and the `SqlParameter.ParameterName`, so the `Pnnn` escape cannot produce a mismatch. PostgreSQL: `PostgreSQLCommand` handles `SQLBooleanType`, and that store's `SystemStoreCatalog.sql` does declare its eight boolean columns `boolean` — the new override is correct and needed.
- **There is no injection surface left in a parameterized write.** The parameterized form contains no quoted string at all (only table and column names, the `null` keyword and `is null`), so `PrepareStatement`'s in-string tracking cannot be confused by a value, and no value reaches the statement text.
- **The signature change breaks nothing in the tree.** Grepped repository-wide, including the projects that are not built: nothing overrides `GenerateInsertStatement`, `GenerateUpdateStatement` or `GenerateDeleteStatement`, and the only `NativeToLiteralValue`/`NativeToParameter` overrides are the PostgreSQL store's. The migration note covers the out-of-tree case.
- **The handoff's error-path account is accurate**, including the parts that contradict the original ticket. A duplicate primary key comes back from `SQLiteConnection.InternalWrapException` as a plain `DataphorException` with no statement text — confirmed by reading that method: a `Constraint` result code is severity User and takes the `DataphorException(ApplicationError, message)` arm, which never reaches `ExecuteWriteStatement`'s `catch (ConnectionException)`. A lock takes the `StoreException` arm. So the re-raise helps generic SQL failures only, which is what the code claims.
- **`ExecuteWriteStatement`'s nested `ConnectionException` is the right shape, not a wart.** The handoff asked a reviewer to judge it. The obvious alternative — re-raise from `exception.InnerException` to avoid a layer — is worse: it would discard whatever a store's own `InternalWrapException` returned, and nothing is actually lost by nesting (code and severity are carried over, the provider exception is still reachable through the chain). Kept, with the reason added to the method's remarks so the next reader does not re-litigate it.
- **The three added tests all earn their place.** `AwkwardStringRoundTripsThroughWrites` is the reproduction of the defect fixed, at the lowest layer that shows it. `CursorEndsScanWhenUpdateMovesRowOutOfRange` pins a contract the cursor ticket left unverified. `BooleanRoundTripsThroughWrites` cannot distinguish 1/0 from a bound bool on SQLite — the handoff is right about that and offered it for cutting — but it lives in a fixture whose stated job is to be subclassed per store, the PostgreSQL override exists precisely because the mapping differs per store, and it does pin the round trip through both a parameterized insert and a parameterized update. Kept; only its doc comment was wrong (below).
- **Source hygiene.** `SQLStoreConnection.cs` and `StoreContractTests.cs` are 632 and 687 lines, both well inside the file sizes around them. Byte-order mark, line endings and tab indentation verified per file after every edit, including `PGSQLStoreConnection.cs`, which is not UTF-8 at all; `tickets/plan/store-mssql-port.md` is LF on an LF blob. `git diff --numstat` shows only intended lines.

### Fixed in this pass

- **The plan's security finding overstated what the change closed.** The WS11 bullet said the only remaining concatenation is a store cursor's ranged reader statement. Two more sites render values into statement text: the undo log (which the WS4 *Done* annotation does mention) and the four seed inserts `CatalogStore.Initialize` runs on a fresh catalog, one of which interpolates the configured instance name with a hand-written `Replace("'", "''")`. None is an injection surface and none is a defect — but a security finding that names one site when there are three is the kind of claim a later audit builds on. Bullet rewritten to name all three and why each is harmless, and a `NOTE:` added at the seed inserts saying they are the last writes that render their own values, with the overload to use if one ever carries a value from a catalog row.
- **The undo log's `NOTE:` gave the rationale but not the cost.** It explained why the log keeps literal statement text and concluded that parameters "would buy nothing". That is true of speed and false of correctness: the replay still renders values into SQL text, so a value a quoted literal cannot carry breaks the rollback even though the write it inverts now survives. Added the consequence, the name of the test that pins the class (`AwkwardStringRoundTripsThroughWrites`), the reason nothing can hit it today (every catalog column is an int, a tinyint, or text the engine generates from a D4 name, script or tag), and what to do if that changes.
- **A twelve-line measurement dump inside a four-line method.** The `NOTE:` in `AppendValue` carried every individual timing from both measurement runs, duplicating what the plan's WS4 row and this ticket already hold, and buried the one actionable sentence — check whether `DotNetCommand.PrepareCommand` re-prepares on every execute — under them. Trimmed to the finding and a pointer to where the runs live.
- **`BooleanRoundTripsThroughWrites`' doc comment claimed more than the test can show.** It said a store "whose provider binds a parameter's declared type strictly, has to map them and fails here if it does not" — but on SQLite, the only store built, binding the bool itself also passes, because the provider stores a bool as an integer anyway. Rewritten to say what it pins on every store and which store it would actually catch, so the next reader does not take a green run here as evidence of the 1/0 mapping.

### Recorded as tripwires, not tickets

Four `NOTE:` comments came from the implement stage: `AppendValue` on per-value allocation, `SQLStoreCursor.GetReaderStatement` on the remaining literal read path, `AppendKeyCondition` on why `is null` must stay literal, and `ModifyOperation` on the undo log. Review added two:

- **`SQLStoreConnection.NativeToParameter`** — a string parameter's declared size is that value's own length, so a back end that keys its plan cache on parameter type caches a plan per distinct string length. Harmless on SQLite, which ignores the size; `DotNetCommand.SetParameters` grows a declared size that is too small for its value, so declaring the column's width instead would also work. Nothing to do until a server-backed store is actually running.
- **`CatalogStore.Initialize`** — the seed inserts, above.

### Tickets filed: none. One arm extended

`grep -rl` over the open stages for the paths in `files:` hits `tickets/plan/store-mssql-port.md`, which the implement stage had already given an arm about `MSSQLCommand.PrepareParameters` mapping `SQLStringType` to `SqlDbType.VarChar` while every text column in that store's catalog is `nvarchar`. Added one clause to the same arm about the per-value parameter size, since the SQL Server port is where both questions will actually be answered and both live in the same mapping.

Nothing reached the filing bar. The one invariant worth wanting — "the store never renders a value into SQL text" — would need the reader statement and the undo log parameterized too, and neither is a defect today: both carry only values this same store generated and read back, and no catalog column can hold a value a literal cannot express. That is a tripwire's shape, not a ticket's. The one real latent defect found during implement (the PostgreSQL boolean mapping) was fixed in place; review found no new one.

### Not done, and why

`docs/architecture.md`'s paragraph on the store was read and left alone. Its two sentences about `SQLStoreConnection` (nested transactions emulated with an undo log of inverse statements) and `SQLStoreCursor` (seek and range by generated `select` statements) are both still exactly true, and literal-versus-parameter is a level of detail below what that one-paragraph overview carries. The change's reader-facing record is the migration note and the plan's WS4 row.

`PGSQLStoreConnection.NativeToParameter` is still uncompiled and untested: `DAE.PGSQL.csproj` is in neither `Dataphor.slnx` nor `tools/net-spike/projects.txt`. Review confirmed by reading that it would compile (`SQLParameter` and `SQLBooleanType` come from the `Alphora.Dataphor.DAE.Connection` using that the file's live `#if` arm carries) and that it is correct against that store's own schema and its provider's mapping. Writing it rather than leaving a note was the right call: without it, that store sends integers to its own `boolean` columns the moment anyone revives the project.

## Verification

- `dotnet build Dataphor.slnx`: 0 errors, 143 warnings — the count at HEAD.
- Suite part 1 (`class != CoverageScripts`): 369 passed, 0 failed in `DAE.Server.Tests` (2 m 54 s); 13 passed, 0 failed in `Common.Tests`.
- Part 2 (coverage scripts `[A-Ra-r]`): 67 passed, 0 failed, 11 skipped, 3 m 10 s.
- Part 3 (coverage scripts `[^A-Ra-r]`): 48 passed, 0 failed, 14 skipped, 3 m 22 s.
- `StoreContractTests`, `CatalogStoreTest`, `CatalogStoreConcurrencyTest` and `StoreRebuildWorkflowTest` together: 44 passed, 0 failed.
- Skipped counts in parts 2 and 3 are the manifest's known-failure, unstable and fragment coverage scripts, unchanged. No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written; the three records in `tickets/.pre-existing-known.md` are all `one-off` and none appeared.

## Measurement

`StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart`, four runs each side, the printed `register+security+DDL` window: before 975 / 1461 / 1441 / 1466 ms, after 916 / 1420 / 1361 / 1328 ms. Suite part 1 wall time, the part the change can move because almost every test there starts a server and populates a catalog: before 2 m 54 s / 3 m 01 s, after 2 m 54 s / 2 m 59 s / 3 m 14 s / 3 m 20 s / 3 m 24 s / 3 m 25 s / 3 m 35 s.

**No detectable change either way.** The bands overlap and this machine's run-to-run spread is wider than any effect — the same `StoreRebuildWorkflowTest` window varies 975–1466 ms. The ticket's expectation of 20–40 % faster on batched writes did not appear; the likely reason, parked as a tripwire at `AppendValue`, is that `DotNetCommand.PrepareCommand` assigns `IDbCommand.CommandText` on every execute even when the text has not changed, so the reuse of one prepared statement across rows — the whole source of the hoped-for gain — may never happen. That is in `DotNetCommand`, shared with every device connection, and well outside this ticket.

Before-numbers for parts 2 and 3 were not captured: taking them needs the engine change reverted for about fifteen minutes of test running, and a run killed in that window would leave the tree without the implementation. Parts 2 and 3 are coverage scripts whose cost is per-script server startup, so they would say less than part 1 does.

## Docs

- `docs/migration-notes.md` — one entry under *Custom C# extensions*: the three `Generate...Statement` signatures, the two new members, what stays literal, and that a custom store overriding a generator adds the argument while one with boolean columns overrides `NativeToParameter`.
- `docs/modernization-plan.md` — a **Done** annotation on the WS4 row carrying what the scope did not anticipate and the measurement, and the WS11 security finding bullet rewritten to what is now true (corrected in review to name all three remaining literal sites).
