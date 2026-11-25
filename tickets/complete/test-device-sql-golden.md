description: A test now captures, as checked-in text files, the SQL the engine generates for a SQL Server back end, so a change to the translation shows up as a reviewable difference instead of as a wrong query reaching a customer's database. No database server is needed to run it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.SQLRecorder/, tests/DAE.Server.Tests/DeviceTranslationGoldenTests.cs, tests/DAE.Server.Tests/MSSQLDeviceTranslationGoldenTests.cs, tests/DAE.Server.Tests/FrontendDerivationGoldenTest.cs, tests/goldens/devices/MSSQL/, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, Dataphor.slnx, tools/layout/Layout.csproj, tools/coverage-report/areas.json, libraries/SQLDevice/Source/SQLDevice.cs, AGENTS.md, docs/architecture/devices.md, docs/modernization-plan.md, tickets/backlog/debt-sql-translation-coverage.md
----
## What landed

Two pieces, built in `ticket(implement): test-device-sql-golden` and adjusted by this review pass.

**A test-only library whose connection class records instead of connecting**, `tests/libraries/TestFramework.SQLRecorder` (assembly `Alphora.Dataphor.DAE.SQLRecorder`, project `Source/DAE.SQLRecorder.csproj`). `RecordingConnection` is a `SQLConnection` registered as `SQLRecorder.RecordingConnection`, which is what a `create device` statement's `ConnectionClass` attribute carries; it records the connection string, reports itself idle and does nothing for begin/commit/rollback. `RecordingCommand` records `EXECUTE <statement>` or `OPEN <statement>` with a `PARAMETERS` line naming each parameter's name, direction and SQL type - names and types because those are the device's translation, not values, which are the test's own data. A cursor is *recorded* whenever it is opened but only *answered* from a canned row the test armed (`SQLRecorderArmCursor`); every other read throws with a message naming the statement and what is armed. Four D4 operators (`Register.d4`) are how a test reads the recording back, because the server loads the library's own copy of the assembly and a test assembly cannot reach that static.

**A device-agnostic fixture and one subclass**: `DeviceTranslationGoldenTests.cs` holds the corpus, the golden machinery and the four cases; `MSSQLDeviceTranslationGoldenTests.cs` supplies the device library, the `create device` statement, the golden folder and the expressions the device refuses. The fixture derives from `InProcessTestFixture` for the server but keeps one session and process for the whole fixture, because `SetLibrary` is session state and the device, its tables and every query have to compile with the device's library current.

**25 golden files** under `tests/goldens/devices/MSSQL/`: the `create table` + `create unique clustered index` pair for four tables, the `alter table ... add`, the `drop table`, the parameterized `insert` with the key-check select the engine puts in front of it, and the `select` behind a table read, restriction (comparison, nil, precedence, a quote inside a string literal), projection, rename, extension, operator translation, inner/left/right join, aggregation, union, correlated `exists`, order and an all-types table. Each is the generated SQL with the emitter's hard-coded CRLF turned into newlines (`.gitattributes` checks `tests/goldens/**` out as LF) and one newline at the end; nothing else is normalized.

**Wiring**: the project in `Dataphor.slnx` and in the Library list of `tools/layout/Layout.csproj`, the assembly on the `ignored` list of `tools/coverage-report/areas.json`, the two fixture files in the `Compile` list of `DAE.Server.Tests.csproj`.

## Use cases: how to exercise it

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~MSSQLDeviceTranslationGoldenTests"
```

To regenerate after a deliberate translation change - and then read every difference before committing it:

```
DATAPHOR_UPDATE_DEVICE_GOLDENS=1 dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~MSSQLDeviceTranslationGoldenTests.EveryGenerated"
```

That run writes the files, deletes the ones no case claims, and then **fails anyway**, exactly as `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` does.

## Tests, after review

Four cases, the count the implement stage left. One was strengthened; none was added or cut.

| Case | What it holds |
|---|---|
| `EveryGeneratedStatementMatchesItsGolden` | The whole corpus: 9 statement cases and 16 query cases, each compared character for character with its file, plus both directions of the set (a case with no golden, and a golden no case claims). One test rather than one per case because the statement cases have to run in order and the set comparison needs every case's answer - the same reason `FrontendDerivationGoldenTest` is one test. |
| `TheDeviceRefusesWhatItCannotTranslate` | The supported boundary, by error code, codes being a compatibility surface: `minus` raises `SQLException.Codes.QueryUnsupported` (126125) and an expression over a table in the server's own default device raises `QuerySupportedByDifferentDevice` (126124). |
| `ReadingADeviceTableIsRefusedRatherThanAnsweredWithNothing` | **Strengthened by this pass.** Still holds that a read the recorder has no answer for throws rather than returning an empty result - without which a device that generated nothing would look like one that generated the right thing. Now also holds the two emitting paths together; see the first review finding. |
| `TheDeviceConnectedOnlyThroughTheRecorder` | That the recorder is the connection the device actually used and the server it tried to reach is the unreachable placeholder - the evidence that these files were produced with no database server anywhere. |

No test covers the recorder's own plumbing (that `Register.cs` registers the class, that an operator returns a string): that is wiring, and the four cases fail loudly if any of it is wrong. Agreed with the implement stage on that.

## Review findings

The implement diff was read before the handoff. The goldens were read one by one and each type mapping traced to the device's own registration; the generating and consuming code paths in `libraries/SQLDevice/Source/SQLDevice.cs` were read rather than assumed; the fixture's behaviour was probed with three temporary edits (each reverted), and the one assertion added was checked to fail when the text it compares is wrong.

### Found and fixed in this pass

- **The contract under 16 of the 25 goldens was unverified: nothing held the two paths a device emits a statement down together.** A query golden comes from the `D4ToSQL` operator, which emits `((SQLDevicePlanNode)node.DeviceNode).Statement` as the compiler left it (`SQLDevice.cs:5096`). A real cursor open does not go that way: `SQLDeviceSession.InternalExecute` builds an `SQLTable`, whose constructor and `GetMainCursor` (`SQLDevice.cs:4672` and `:4756`) *rebuild* the statement before emitting it and can change it on the way - a deferred column becomes a `case when <col> is null then 0 else 1 end`, and a cursor restart appends a key condition. Nothing in the fixture reached that path: the one test that read a device table used `if not exists (Employee)`, which the engine sends as a scalar select emitted on the same path `D4ToSQL` uses. So a change touching only the rebuild would have left all 16 query goldens green while what a database server receives had moved. Fixed by making that test read the table for real (`foreach row in Employee do begin end;`) and assert that what the connection was handed contains the `D4ToSQL` text verbatim. Measured: for this corpus the two paths agree byte for byte, including `AllTypes` (its `image` column is not treated as deferred here), so the assertion passes as written - and it fails when pointed at a different table, which was checked. This needed one change in the recorder: a refused cursor open is now recorded before the arming is looked up, since a refused read is still a read the device asked for and its statement is the only way a test sees it. No golden changed (the one armed open in `DML.Insert.sql` is still recorded exactly once).
- **An update run deleted the golden of a case that failed to translate.** `claimed.Add` ran only after `Generate` succeeded, so a case that threw left its file unclaimed and the orphan sweep deleted it under `DATAPHOR_UPDATE_DEVICE_GOLDENS=1` - destroying the record of correct behaviour, which the project's rules protect more strongly than they protect an expected value from being edited, there being no reference runtime to regenerate it from. In compare mode the same gap reported one broken case twice. Fixed by claiming the file from the case's existence rather than from its success. `FrontendDerivationGoldenTest` had the identical shape (`FrontendDerivationGoldenTest.cs:342`); fixed there too, in the same one-line move - a defect with a local, obvious fix met along the way. An expected failure there has no golden to begin with, so claiming its path changes nothing.
- **Documentation**: `docs/architecture/devices.md` gained a paragraph on the two emitting paths and why one case reads a device table for real, because that is the fact the query half of the corpus rests on and it was nowhere written down. The page is still under the 1,500-word limit (`python tools/docs-size-check/check_size.py`: 13 files, 0 over).

### Tripwires parked, not filed

- **`NOTE:` at `TheDeviceConnectedOnlyThroughTheRecorder`** - the test asserts the recorder saw at least one connection since the fixture's last `SQLRecorderClear()`, and that is true only because `SQLDeviceSession.ReleaseConnection` disposes an execute-pool connection whenever no device transaction is open (`SQLDevice.cs:3738`), so each data-definition statement opens a fresh one. If that pooling ever keeps a connection alive across statements, the assertion fails for a reason unrelated to what it checks. The note says to clear and open a connection deliberately at that point rather than relaxing the assertion.
- Two tripwires the implement stage parked were read and left as they are: the `NOTE:` at the `DDLNode` branch of `SQLDeviceSession.InternalExecute` (the connection is requested before the reconcile mode is tested, which is why the recorder exists rather than a `mode = { none }` device), and the `NOTE:` at `RecordingCommand.InternalOpen` (what the canned cursor answers, what an update or delete asks for instead, and where a multi-column canned row would go). Both are accurate and at the right site.

### Checked and sound

- **The 25 goldens.** Read in full; every surprising mapping traced to the device's own registration rather than guessed at. `Boolean` emits `integer`, not `bit`, because `create type System.Boolean class "MSSQLDevice.MSSQLBoolean"` is commented out in `libraries/MSSQLDevice/Source/SystemCatalog.d4:56`, so `Boolean` falls back to the base `SQLDevice` map - `MSSQLBoolean`'s own native name *is* `bit`, so this is a deliberate omission in the catalog script and not a defect in the emitter. `Date` and `Time` both emit `datetime` through `MSSQLDate`/`MSSQLTime` (`:152`, `:166`) and `Binary` emits `image` through `MSSQLBinary` (`:198`). `Length(String)` emits `DataLength`, which the device's catalog script already explains. The right-outer-join case correctly projects the preserved side's `Department_ID` from `T3` rather than `T1`. The emitter's cosmetic quirks (a doubled space before `is null`, a trailing space after a `from` item) are now pinned, which is what a golden is for. Nothing in these files is wrong, so nothing was filed against the device.
- **The implement stage's two deviations from its own plan, both correct.** The `.d4l` requires only `System`, not `Connection`: `SQLConnection`, `SQLCommand` and `SQLCursor` are in `src/engine/DAE/Connection/SQLConnection.cs`, the engine assembly, not in the `Connection` library. And the project needs no `Server` reference: `DAERegisterAttribute` is in `src/engine/DAE/Server/DAERegisterAttribute.cs`, which is the **DAE** project. Both verified by locating the types.
- **The new `Register.d4` round-trips.** `ParserRoundTripCorpusTest` picks it up by tree walk and passes it - no new test was needed for that.
- **No migration note.** Agreed with the handoff and checked: no public type or member of a shipped assembly, no shipped library, no file format, no error code, no WCF contract and no port or on-disk layout changed. The only new surface is a test-only assembly and four operator names that did not exist before.

### Considered and declined

- **The magic error codes `126125` and `126124`** in the subclass rather than `SQLException.Codes.QueryUnsupported`/`QuerySupportedByDifferentDevice`. `DAE.Server.Tests` deliberately does not reference `SQLDevice.csproj`, and adding it to reach two constants would put a second copy of the device assembly beside the one the engine loads from the library folder. The comments name the constants. Left as written.
- **The `(ServerProcess)_deviceProcess` cast for `Evaluate`** - the established idiom in this assembly (`CatalogStoreTest`, `CatalogStoreConcurrencyTest`). Left.
- **The base fixture's per-test session and process go unused**, one pair created and torn down for each of the four cases. Measured cost is within the few milliseconds each case reports; adding a hook to the base fixture to suppress it would cost more than it saves.
- **File length** - `DeviceTranslationGoldenTests.cs` is 661 lines after this pass, seventh largest of the assembly's fixtures (`AssignmentValueOwnershipTest.cs` is 1,857). Measured with `wc -l`. The corpus is data and reads as data; no split.
- **No README under `tests/goldens/devices/`.** Agreed with the handoff: `tests/goldens/frontend/` has none either, and the never-edit rule is stated in the fixture's class remarks, in the failure message the pass prints, in `AGENTS.md` and on `docs/architecture/devices.md`.
- **Update and delete translation, and device-specific scalar types**, both deliberately out of scope with the reasoning recorded at the code. Re-read and agreed: answering an update or a delete means returning typed values for every mapped column, which is a database rather than a recorder, and belongs to `test-device-live`. `SQLDevice.MSSQLBinary` and its `binary(n)` mapping stay uncovered so the corpus compiles unchanged on the PostgreSQL device, which `device-pgsql-device-golden` needs.

### Nothing filed

No ticket came out of this pass. The two defects found were minor and fixed inline, the one conditional concern is a `NOTE:` at its site, and nothing in the goldens or the device turned out to be wrong. `tickets/backlog/debt-sql-translation-coverage.md` already carries the implement stage's note narrowing it to the base emitter, and was re-read against the corpus: the two sets do not overlap.

## Measurements

- **Build**: `dotnet build Dataphor.slnx` and `-c Release` both 0 errors, 144 warnings - the same count as at HEAD before this pass, so neither the implement work nor this review adds one.
- **Tests run, review tier (one ring wider than implement), all green**:
  - `--filter "FullyQualifiedName~MSSQLDeviceTranslationGoldenTests"` - 4 passed, about 7 s wall clock (the four cases are about 0.15 s; the rest is VSTest startup and the fixture's server start).
  - `--filter "FullyQualifiedName~MSSQLDeviceTranslationGoldenTests|FullyQualifiedName~FrontendDerivationGoldenTest|FullyQualifiedName~LibraryLoadingTest|FullyQualifiedName~HostProgramTest|FullyQualifiedName~DeviceClassAttribute|Name=Devices"` - 23 passed, 35 s. `FrontendDerivationGoldenTest` because this pass edits it; the layout readers because a new library folder changes the layout; the device-class and `Devices` cases because the diff touches `libraries/SQLDevice/Source/SQLDevice.cs`.
  - `--filter "FullyQualifiedName~DeviceClassAttribute|Name=Devices|Name=Device|FullyQualifiedName~SQLDevice"` - 20 passed, the neighbouring device area in full.
  - `--filter "FullyQualifiedName~ParserRoundTripCorpusTest"` - 365 passed, 19 skipped, 0 failed, 2 s; run because the change adds a `.d4` document the corpus walks.
- **Negative check on the one assertion added**: pointed at a different table it fails with its own message, so it compares real text rather than passing vacuously. Reverted.
- **Determinism** (carried from the implement stage, not re-measured): three separate `dotnet test` processes, two comparison runs green and a regeneration leaving all 25 files byte-identical, so no normalizer is needed beyond the line endings.
- **Checks**: `python tools/docs-size-check/check_size.py` - 13 files, 0 over the limit. `python tools/docs-link-check/check_links.py` reports 203 missing-file errors, all in the converted `docs/developers-guide`, `docs/users-guide` and `docs/reference` trees; the text this ticket adds contains no links, and the checker is not in CI yet (`.github/workflows/docs.yml` gates page size only, and `docs-link-check-ci` owns adding it). Not this ticket's.

## What the next device subclass inherits

`device-pgsql-device-golden` subclasses the fixture. Three things are worth knowing before it does. The corpus uses only system scalar types, so it compiles unchanged; `Cases()` is private and static today, so a device wanting cases of its own turns it into a virtual with a subclass hook - deliberately not added speculatively. The strengthened read test is device-agnostic: it reads `Employee` through whatever device the subclass creates and compares against that device's own `D4ToSQL`. And a case that cannot translate on a second device must be recorded through the unsupported-boundary mechanism rather than left as a missing golden, which the set assertions in both directions now enforce whether or not translating the case threw.
