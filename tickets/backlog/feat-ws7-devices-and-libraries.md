description: Get the SQL Server and PostgreSQL devices working on current database drivers, with tests that check both the generated SQL and real execution, and tidy the remaining libraries.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
tradeoffs: The PostgreSQL port spans fifteen years of driver changes and has no production user in hand; a maintainer may defer it behind the SQL Server work.
----
## What this is

An umbrella for WS7 after Phase 1 (Phase 1 already removes dead devices and parks side projects). When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then.

**Promote when** gate G1 holds. SQL Server comes first (D15); it is part of gate G2.

## Work items

Scope per the plan's WS7 library table and work-item table.

- `test-device-sql-golden` (from WS3) — L3 golden translation tests for the SQL Server device, no database needed; the harness takes the device as a parameter so PostgreSQL reuses it.
- `device-mssql-port` — `Microsoft.Data.SqlClient`. Compatibility trap: the new driver encrypts by default and validates the server certificate; a device definition that connected under 3.1 must still connect unless its own settings say otherwise. Migration-notes entry exists as "planned". Also: the engine's `Engine.Dispose` (`src/engine/DAE/Server/Engine.cs`) calls `System.Data.SqlClient.SqlConnection.ClearAllPools()`, which is the only reason `DAE.csproj` references the `System.Data.SqlClient` package; when SQL Server access moves to `Microsoft.Data.SqlClient`, that call must clear the new driver's pools (or move into the code that owns them) and the engine's package reference must go.
- `test-device-live` (from WS3) — live tests driven by an environment variable, reported as inconclusive (visibly) when no server is configured; SQL Server LocalDB in CI.
  - Arm from `test-harness-inprocess` (its piece `test-server-fixture` deletes `tests/DAE.Server.Tests/CoverageDeviceTest.cs`; read it from git history): that test reset a SQL Server database `DataphorTests` on `localhost`, registered `TestFramework.Coverage.Devices` and ran its `TestMSSQLDevice` device-coverage script. Its replacement runs the same library and script against the configured server, on the in-process harness (`TestServerInstance`). The coverage scripts `Transaction.Atomicity.TestMSSQL`, `TestDAEMSSQL` and `TestMSSQLDAE` also need a SQL Server device (`Coverage.Base.CoverageDevice`) and are pointed here by `test-decouple-coverage-base`.
- `device-sqlserver-version-matrix` — minimum supported SQL Server version and settings for older servers. Q12 is answered: the production version is unknown, so support versions from roughly the last five years — SQL Server 2022 and later tested and supported, SQL Server 2019 documented as working if it needs no extra code — and keep 3.1's connection defaults so an older server still connects.
- `device-connection-cleanup` — delete the uncompiled duplicates and VS2003 project files in the Connection library; ODBC and OLE DB on packages if that is a project-file change only, else park.
- `device-pgsql-port` — current Npgsql from NuGet, Mono.Security dropped, golden and live tests, minimum PostgreSQL version documented; the PostgreSQL catalog store if it then needs only the store contract tests.
- `library-frontend-web-leftovers` — delete the `System.Web` classes from the Frontend server library (after `test-frontend-derivation-golden` exists, from `feat-ws3-test-layers`).
- `library-sample-shipping-convert` — convert the `Sample.Shipping` sample's C# project (`samples/Sample.Shipping/Source/Shipping.csproj`, three source files and an `AssemblyInfo.cs`) to the SDK format and add it to the layout's Library list, so that the build assembles the sample's library folder. Named by `tools/layout/Layout.csproj` and by the plan's runtime-layout paragraph as the owner of that exclusion. The sample needs a SQL Server either way (its `.d4l` requires `MSSQLDevice` and carries `'ServerName'='.'`), so converting the project moves its `SamplesCorpusTest` entry from `NotInTheRuntimeLayout` to `NeedsADatabaseServer` rather than making it an ordinary case - and `SamplesCorpusTest.EverySampleIsAccountedFor` fails until that entry is moved, which is how the conversion is noticed. `FormCustomizationTest.LibrariesNotCarried` has an entry for it too.
- `library-upgrade-scripts` — establish what runs `Upgrades/Upgrade.<version>.d4`, document it, test a library upgrade from an older version.
- Not in the plan's table but in its library table: update Newtonsoft.Json in System.Internet (also flagged by WS11's dependency audit). Done by `build-core-sdk-convert`: System.Internet and NativeCLI use Newtonsoft.Json 13.0.4 from `Directory.Packages.props`; nothing is left here unless a later version is needed.

## Cutting rules

Re-read the WS7 section and the current code; one ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`; an item whose prerequisite is in an umbrella not yet cut is parked as a new backlog ticket `feat-ws7-devices-and-libraries-later`.
