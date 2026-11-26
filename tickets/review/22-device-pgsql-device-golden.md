description: The PostgreSQL device library is compiled, shipped and its generated SQL captured in checked-in text files, so the translation is pinned before anyone tries it against a live database.
prereq: device-pgsql-connection-port, test-device-sql-golden
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/PGSQLDevice/Source/DAEDevicePGSQL.csproj, libraries/PGSQLDevice/Source/Properties/AssemblyInfo.cs, libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLDevice.cs, libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLOperators.cs, libraries/PGSQLDevice/Source/Language.PGSQL/Emitter.cs, tests/DAE.Server.Tests/PGSQLDeviceTranslationGoldenTests.cs, tests/DAE.Server.Tests/DeviceTranslationGoldenTests.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/goldens/devices/PGSQL/, tests/goldens/devices/MSSQL/Connection.sql, Dataphor.slnx, tools/layout/Layout.csproj, tools/coverage-report/areas.json, AGENTS.md, docs/architecture/devices.md, docs/migration-notes.md, docs/modernization-plan.md, tickets/backlog/bug-mssql-device-drops-datetime2-columns.md
difficulty: medium
----
## What landed

`libraries/PGSQLDevice` is an SDK project, in `Dataphor.slnx` and in the layout's Library list, and its translation is pinned by 26 checked-in files under `tests/goldens/devices/PGSQL/`. **No PostgreSQL server took part and none is needed**; see *What this does not say* below, which is the single most important thing for the reviewer to keep hold of.

The implement stage is commit `ticket(implement): device-pgsql-device-golden`.

### The conversion

`DAEDevicePGSQL.csproj` is `DAEDeviceMSSQL.csproj` with this device's files: assembly name `AlphoraPGSQLDevice`, `RootNamespace` left empty (the device loads `SystemCatalog.d4` and `SystemCatalog.sql` by their bare names), the constants block (the legacy Release set minus `SIGNASSEMBLIES` leaves none, plus `DEBUG;TRACE` in Debug), the explicit `Compile` list, the two `EmbeddedResource` items. `AssemblyInfo.cs` is trimmed to title, description, `DAERegister` and `Guid`, keeping its byte-order mark and CRLF endings.

One reference the legacy project did not have: `DAE.Connection.PGSQL.csproj`. The device names its connection class as the string `"PostgreSQLConnection.PostgreSQLConnection"`, which the server resolves through the registered library, so 3.1 compiled without it; the reference is there so that building the device builds the connection library and both reach the layout. Nothing in the assembly binds to a type in it, and the project file says so.

The `DataphorD4Library Remove` line for `PGSQLDevice.d4l` is gone from `tools/layout/Layout.csproj`, which means the `.d4l` check is now what enforces the two halves being in step. **Every library folder in `libraries/`, `samples/` and `tests/libraries/` is now assembled** - this was the last exclusion - and `AGENTS.md`, the plan's runtime-layout paragraph and the comment in that file all say the mechanism stays for the next such library rather than that nothing is ever excluded.

`AlphoraPGSQLDevice` is in `areas.json`'s `reported` list beside `AlphoraMSSQLDevice`.

### The fixture

`PGSQLDeviceTranslationGoldenTests` is the second subclass of `DeviceTranslationGoldenTests`: the device library, the golden folder, the create-device statement with the recorder as `ConnectionClass` and the three connect-on-start settings off, and the same two unsupported expressions the SQL Server subclass refuses. It overrides `Libraries` to carry `PGSQLConnection` as well, which `PGSQLDevice.d4l` requires.

Two settings worth the reviewer's eye. `ShouldDetermineVersion` **already defaults to false** in this device, where the SQL Server device defaults it true - the implement ticket said all three default to true, which is wrong for this device; it is set off anyway so that a change to that default cannot make the fixture start connecting. And `Port` and `SearchPath` are deliberately not given, so the goldens pin what a definition naming only a server and a database gets.

### The one change to the shared fixture

A third case kind, `TranslationCaseKind.Connection`, and one corpus case using it: the connection string the device handed its connection class, pinned character for character. The ticket asked for the device's connection string to be pinned, and the shared corpus is where it belongs rather than in one subclass - the alternative would have given the PostgreSQL device a check the SQL Server device does not have, which is the asymmetry the ticket's own edge-case list says to avoid. So **the SQL Server device gains one golden too** (`tests/goldens/devices/MSSQL/Connection.sql`) and the reviewer should read both files:

```
MSSQL: server=no.such.server.invalid;database=DataphorDeviceGoldens;application name=Dataphor Server;integrated security=true;Encrypt=False
PGSQL: Server=no.such.server.invalid;Port=5432;Database=dataphordevicegoldens;SearchPath=;User Id=Admin;Password=
```

The PostgreSQL string is the exact six-keyword string the review of `device-pgsql-connection-port` measured against the pinned driver keyword by keyword, which is why the legend was left alone (the implement ticket's own paragraph, corrected by that review, says the same). `Admin` with an empty password is the engine's built-in administrator; nothing machine-specific or secret is in either file. The existing `EveryConnectionCarriesTheDevicesCompatibilityDefaults` test is kept as the narrower statement of *why* `Encrypt=False` is in the SQL Server string; this device defaults nothing, so it passes with its stated reason.

Collecting those strings also turned the `NOTE:` on `RecordedConnections` - that each data-definition statement opens a fresh connection, because `SQLDeviceSession.ReleaseConnection` disposes an execute-pool connection when no device transaction is open - into an assertion made for every statement case rather than a premise. If that pooling ever changes, the set-up fails and says so.

## What this does not say

**A green run says the PostgreSQL device generates the text in those files. It says nothing about whether PostgreSQL accepts any of it.** Nothing in this repository has sent a statement to a PostgreSQL server since about 2017, and this ticket did not change that: `device-pgsql-port` is the live pass. The device must not be called working on the strength of this.

The construct most worth the live pass's attention first is in `tests/goldens/devices/PGSQL/DML.Insert.sql`: the key-check select the engine puts in front of an insert carries `for update` inside an `exists` sub-query that sits in the outer statement's select list. That is this device's equivalent of the SQL Server device's `(updlock)` hint and comes from `PostgreSQLRetrieve`. PostgreSQL restricts where a locking clause may appear, and **whether it is accepted there was not verified** - it needs a server. It is called out here because it is the one case where a plausible rejection would affect every insert into a PostgreSQL device table rather than an edge case.

Also unverified, and already recorded by the prerequisite: the device creates a `time` column for a D4 `Time` (visible in `Table.AllTypes.sql`), and the driver converts a PostgreSQL `time` only to `TimeOnly` and `TimeSpan`, so both directions of the `Time` scalar type are wrong. The corpus's own `AllTypes` table therefore reaches that defect the moment there is a server.

## What the goldens showed, case by case

Every one of the 26 cases generated, so **the fixture's unsupported-boundary mechanism records no exceptions and no expected-failure table was needed.** That was the open question the ticket was written around ("a device that has had almost no maintenance since 2017 will have some") and the answer is none: the device registers, its operator map runs, and the corpus translates.

The useful way to review the files is to diff the two devices' folders - the case sets are identical, so every difference is a dialect difference. All of them, and nothing else, are:

- `character varying` for `varchar`, `timestamp` for `datetime`, `date`/`time` where SQL Server gets `datetime`, `uuid` for `uniqueidentifier`, `bytea` for `image`.
- `create unique index` without `clustered`, which the PostgreSQL emitter drops (the line is commented out in its `EmitCreateIndexStatement`); PostgreSQL has no clustered index.
- `for update` where SQL Server has `(updlock)`.
- `char_length(...)` where SQL Server has `DataLength(...)`.
- `union all` with a `distinct` on the outer select where SQL Server has plain `union` - `PostgreSQLUnion`'s documented workaround for PostgreSQL 9.1 not optimizing `union`.
- `nulls first` on an order column that can be nil (only the right-join case has one), because this device sets `SupportsOrderByNullsFirstLast`.
- No `option (fast 1)`, and no trailing space where the SQL Server emitter leaves room for that hint.

**Three facts came out of producing the files, all recorded as `NOTE:` tripwires at their sites and in the plan's WS7 row, none filed as a ticket:**

- `PostgreSQLDevice.TranslateOrder` adds an `option (fast 1)` modifier to every ordered statement - a T-SQL hint copied from `MSSQLDevice`. It reaches no statement: only the T-SQL emitter writes a statement's `OptimizerHints` modifier and this device's emitter does not derive from it, which the goldens confirm. Conditional, so a `NOTE:` at the site saying what to do if the emitter ever learns to write modifiers.
- The PostgreSQL emitter's `EmitAlgebraicFromClause` omits the dummy `from` clause, but `SQLDevice` builds that clause as a `CalculusFromClause` in three of its four places (including the key-check select), and the emitter does not override `EmitCalculusFromClause` - so `from (select 0 as "dummy1") as "dummy1"` is still emitted there, exactly as the SQL Server device emits it. Valid PostgreSQL, so nothing is wrong today. `NOTE:` at the site.
- **Only 2 of the 61 operator classes in `PostgreSQLOperators.cs` are reachable** (`PostgreSQLRetrieve` and `PostgreSQLUnion`). Measured: 13 live `PostgreSQLDevice.*` class references in `SystemCatalog.d4`, 11 of them device scalar types, every other operator class either unnamed or named only on a commented-out line. This matters for anyone auditing the device: several unreached classes emit T-SQL that PostgreSQL would reject (`PostgreSQLToday` emits `Round(GetDate(), 0, 1)`; the four `To*` classes emit `Convert(<type>, x)`), and **they are dead rather than latent** - what the map needs of the rest it translates through `Storage.TranslationString` tags written in PostgreSQL syntax. The `NOTE:` at the top of that file says to check the map before calling any class there a live defect.

## The one finding that went on the board

**An arm appended to `tickets/backlog/bug-mssql-device-drops-datetime2-columns.md`, not a new ticket.** That ticket already names the root cause and the one site (`SQLDevice.GetDeviceTables`'s `if (ShouldIncludeColumn(...))` with no else branch, dropping an unrecognised column in silence); the PostgreSQL device is the next instance of the same class, so the rules say append rather than file. Its `description:` was widened from "a SQL Server database" to "a SQL database" and `files:` gained `PostgreSQLDevice.cs`.

The substance: `PostgreSQLDevice.ShouldIncludeColumn` and `FindScalarType` are the SQL Server lists with a few PostgreSQL spellings added, and they are missing `bytea`, `uuid`, `time without time zone`, `character`, `double precision` and the two `with time zone` types - so **the device drops column types it creates itself**, which the checked-in `Table.AllTypes.sql` makes visible. `repro: static` for that arm: the case lists and the generated types are read off the code and the goldens, but no real table has been reconciled. A second, dormant defect behind the same door: `FindScalarType`'s `binary` and `timestamp` arms resolve `SQLDevice.PostgreSQLBinary`, and no such D4 type exists (`SQLDevice/Documents/Register.d4` declares `MSSQLBinary` and nothing else) - unreachable only because those two spellings are T-SQL ones PostgreSQL never reports, and a trap for whoever adds `bytea` to the lists.

## Things to check in review

- **Read all 26 PostgreSQL goldens and both `Connection.sql` files**, not just the diff summary above. The ticket's own warning applies: a case whose text looks wrong probably is wrong, and this is the cheapest point in the whole PostgreSQL port at which to notice.
- **The shared-fixture change is the riskiest part of the diff**, because it touches what the SQL Server device is held to. The `Connection` case kind carries an empty `D4` string, which no other kind does; judge whether that is acceptable or whether the case wants its own shape.
- **The golden extension.** A connection string is not SQL, but it is stored as `Connection.sql` because the fixture has one extension and `ExistingGoldens` reports any file it cannot account for. A second extension per case kind was considered and judged more machinery than the problem is worth; disagree if you think the filename misleads.
- **`System.Boolean` maps to `integer`, not `boolean`.** True of both devices, so not a regression here, but for PostgreSQL it puts the device at odds with the PostgreSQL *catalog store*, whose boolean columns are declared `boolean` - which `device-pgsql-port`'s expectation list already says to settle. The device's own `PostgreSQLBoolean` class is commented out of the map. Recorded in `docs/architecture/devices.md` and the plan row; deliberately not changed here, because changing a type mapping without a server to check it against is the mistake this ticket exists to avoid.
- **`SystemCatalog.d4` and `SystemCatalog.sql` were not touched**, so `ParserRoundTripCorpusTest`'s entries for the template are unaffected (it passes). The resource names were verified directly - `Assembly.GetManifestResourceNames()` on the built assembly returns exactly `SystemCatalog.d4` and `SystemCatalog.sql` - and the fixture is now a standing check on them: a renamed resource would stop the operator map loading and the device would not register.
- **`tools/net-spike` was not changed** because `PGSQLDevice` is not in its `projects.txt` and `PATCHES` is empty, and `EngineCheck`'s resource-name table was not extended because that harness is throwaway and the fixture now covers the same ground live.

## New tests

One fixture and one shared case, which is the point of the ticket rather than incidental to it - the project rules' characterization test for a subsystem being re-hosted, at the lowest layer that shows the behaviour.

| Test | What it verifies |
|---|---|
| `PGSQLDeviceTranslationGoldenTests` (5 cases, inherited) | That the PostgreSQL device registers, its operator map runs, and all 26 corpus cases generate the text in `tests/goldens/devices/PGSQL/`; that it refuses `minus` and a table in another device with the recorded error codes; that a real table read is refused by the recorder rather than answered with nothing, and hands the connection the same text `D4ToSQL` emits; and that every connection it opened went to the unreachable placeholder server |
| `Connection` corpus case (both devices) | The exact connection string the device built from its own settings and its connection-string builder's legend - the only check on it that needs no database server |

No test for the project conversion, the layout entry or the `areas.json` line: the build, the layout's `.d4l` check and the coverage report's own assertion already enforce those.

## What was run

Build first, every command in the foreground.

- `dotnet build Dataphor.slnx` - 0 errors, 127 warnings; `dotnet build Dataphor.slnx -c Release` - 0 errors, 127 warnings. Same count as the prerequisite reported, and a grep over the output shows no warning from any file in this diff.
- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~DeviceTranslationGoldenTests|FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~LibraryLoadingTest|FullyQualifiedName~HostProgramTest"` - **383 passed, 19 skipped, 0 failed, 17 s.** The ticket's named set. The 19 skips are `ParserRoundTripCorpusTest`'s allow-list. Both golden fixtures are 10 of those cases and about 0.2 s between them.
- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~FormCustomizationTest|FullyQualifiedName~FormDocumentCorpusTest|FullyQualifiedName~SamplesCorpusTest|FullyQualifiedName~InstanceConfigurationTest|FullyQualifiedName~LibraryAttachTest"` - **125 passed, 0 failed, 1 m 12 s.** Not in the ticket's list, added because `Layout.csproj` is in the diff and these are the fixtures that assert a library list against the layout or against the tree.
- **Churn check**: two `DATAPHOR_UPDATE_DEVICE_GOLDENS=1` runs in separate processes, `sha256sum` over all 52 golden files both times - identical. The translation is deterministic across processes for this device as it is for the SQL Server one.
- `python -m unittest discover -s tools/coverage-report` - 14 tests, OK.
- `python tools/docs-size-check/check_size.py` - 13 files, 0 over the limit (`devices.md` is 1,235 words of 1,500).
- `python tools/docs-link-check/check_links.py` - 203 errors, the same count the prerequisite reported and all pre-existing: every one is a link into a `docs/reference/` stub the outstanding DocBook conversion has not produced. Nothing written to `.pre-existing-error.md`; it is not a test failure and the cause is a tracked work item.
- Layout contents listed in both configurations: `Libraries/PGSQLDevice/` holds `AlphoraPGSQLDevice.dll`, its `.pdb`, `PGSQLDevice.d4l` and `Documents/`, the same shape as `Libraries/MSSQLDevice/`.
- `git ls-files --eol` over every file in the diff: all working copies CRLF, matching their siblings, and the byte-order-mark state of each edited source file preserved.

No pre-existing test failure was met. The full suite was not run: per the project rules that is CI's job after the stage's push.

## Documentation

- `AGENTS.md`: `PGSQLDevice` added to the converted list and removed from the spike's "everything else"; the runtime-layout paragraph no longer names an excluded library; the device-goldens paragraph rewritten for two fixtures, 26 cases and the connection case.
- `docs/architecture/devices.md`: one paragraph on what is proved about the PostgreSQL device and what is not, with the two mappings worth knowing (`Boolean` as `integer`, and the unreachable operator classes).
- `docs/migration-notes.md`: a new `done` entry - the `PGSQLDevice` library ships again, the D4 class name is unchanged, what has been checked is the generated SQL and not what a server does with it, and do not put it into production before `device-pgsql-port`. The `PGSQLConnection` entry's forward reference, which said the device was not shipped again until that ticket, is corrected.
- `docs/modernization-plan.md`: the WS7 library row records the device half with its three measured facts; the `device-pgsql-port` work-item row says two thirds are done and names what is left; the `test-device-sql-golden` row and the runtime-layout paragraph are brought up to date.
