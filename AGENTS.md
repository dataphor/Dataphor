# Dataphor

Dataphor is a relational application platform: a database engine with its own language (**D4**) that federates SQL back ends, plus a declarative UI layer (**Frontend**) that derives forms from the schema, a WinForms client that renders them, and an IDE (**Dataphoria**). C#, ~400k lines, written 2000–2019 for .NET Framework. Customer applications are *D4 libraries + form documents*, not C# — their behaviour is what must not break.

**The repo is mid-modernization (.NET Framework 4.5.2 → .NET 10).** Before any non-trivial work read:

- [docs/modernization-plan.md](docs/modernization-plan.md) — the master plan: workstreams, decisions, sequencing, known defects. Tickets anchor to its sections.
- [docs/architecture.md](docs/architecture.md) — how the pieces fit, what talks to what, where things live on disk.
- [docs/migration-notes.md](docs/migration-notes.md) — running list of changes an application or custom extension will notice. If your change is one, add it there in the same commit.

Settled direction (details in the plan): .NET 10 only, no .NET Framework build; SQLite catalog store, SQL CE deleted, catalogs rebuilt from scripts rather than migrated; SQL Server and PostgreSQL are the supported devices; WinForms client and IDE kept; web client, Silverlight removed; FHIR and web prototypes parked.

## Layout

We own this codebase. The plan's [Repository layout](docs/modernization-plan.md#repository-layout) section gives the target tree, and its old → new table maps any path from before the reorganization (in an old ticket, commit or document) to where it is now.

| Path | What |
|---|---|
| `src/engine/Base` | Shared utilities and **BOP**, the XML object serializer used for forms, `.d4l` files and config (assembly `Alphora.Dataphor`). `src/engine/Base.Windows` holds the Windows-only helpers (assembly resolve hook, `PathUtility`) |
| `src/engine/DAE` (`DAE.csproj`) | The engine: D4 lexer/parser/compiler, plan nodes (`Runtime.Instructions/`), schema, memory device, catalog device, store abstraction |
| `src/engine/Server` | The full server on top of the engine: persistent catalog, library loading, WCF services, .NET ADO connections |
| `src/engine/Store.SQLite`, `Store.MSSQL`, `Store.PGSQL` | Catalog *store* back ends (where the catalog persists) — not to be confused with devices. The project files keep their old names: `DAE.SQLite.csproj`, `DAE.MSSQL.csproj`, `DAE.PGSQL.csproj` |
| `src/protocol/Common`, `Client`, `NativeCLI` | WCF contracts/binding, client proxy + client-side catalog cache, simplified "native" call interface (`DAE.NativeCLI.csproj`) |
| `src/client/DAE.Client`, `DAE.Client.Provider`, `Frontend.Client` | `DataSession`/`DataView` data-binding model, ADO.NET provider, UI node model + form loader |
| `src/windows/DAE.Client.Controls`, `Frontend.Client.Windows`, `Frontend.Client.WPF`, `WindowsClient` | WinForms data-aware controls, the WinForms renderer, the WPF scheduler control, and the Windows client exe (`WindowsClient/Frontend.Client.Windows.Executable.csproj`) |
| `src/windows/Dataphoria` | The IDE (WinForms) |
| `src/windows/ConfigurationUtility` | Tray config tool (`DAE.Service.ConfigurationUtility.csproj`) |
| `src/hosts/DAEServer`, `DAEService`, `D4Runner` | Console host, Windows service (`DAEService/DAE.Service.csproj`), script runner |
| `libraries/<Name>/` | Shipped D4 libraries: `<Name>.d4l` descriptor, `Documents/` (D4 scripts, forms), optional C# in `Source/`. Devices (`MSSQLDevice`, `SQLDevice`, …), connections, `Frontend` (server side of the UI), `System.*`, `Security`, `General` |
| `samples/` | The `Sample.*` libraries, same structure |
| `tests/` | NUnit projects; `DAE.Server.Tests` holds the in-process engine tests, among them the runner for the D4 coverage scripts and its manifest (`coverage-manifest.json`). `tests/libraries/` holds the D4 test libraries (`TestFramework*`); `TestFramework.Coverage.Scripts` has the coverage scripts |
| `external/` | Third-party binaries not yet replaced by NuGet packages (text editor, docking, Ascend, Npgsql, Mono.Security) |
| `docs/` | Plan, architecture, migration notes, and the user documentation converted from `Documentation/`: `users-guide/`, `developers-guide/`, `reference/` (D4 grammar, tags, error codes and stubs; also `CompilerDefines.txt` and `Error Message Prefix Codes.txt`), `glossary.md`, `bibliography.md`, `images/`; `README.md` is the index |
| `tools/` | `layout/` (`Layout.csproj`, the project that assembles the runtime layout), `net-spike/` (throwaway harness that builds and runs the code on modern .NET without changing repository source; build output goes to `artifacts/`), `docs-link-check/` (stdlib Python link checker for the Markdown under `docs/`), `docs-convert/` (run-by-hand converter from the AsciiDoc in `Documentation/` to Markdown under `docs/`; needs Node, pandoc and Pillow, see its README) and `editor-support/` |
| `Setup/` | WiX installer (v3.5, being replaced) |
| `Documentation/` (AsciiDoc), `legacy/Documentation/` (DocBook) | The old user documentation sources. The AsciiDoc books are converted: the Markdown under `docs/` is the copy to read and edit. Both trees remain only until `docs-remove-old-sources` (the DocBook reference material is still to be converted, by `docs-convert-legacy-reference`) |
| `legacy/` | Parked code, excluded from the build and not to be fixed: FHIR/CQF/PHINVADS libraries, the Oracle/ADO/SAS device and connection libraries and the DocBook generator library `Documentation` (`legacy/libraries/`), the web and cloud prototypes (`legacy/Dataphoria/`, `legacy/Processor/`, `legacy/Design/`), the DocBook sources (`legacy/Documentation/`) |
| `Utilities/Wix` | The copy of WiX 3.5 that `Setup/` builds with; deleted with `Setup/` when WS9 lands |

## Build and test

Needs only the .NET 10 SDK that `global.json` pins.

```
dotnet build Dataphor.slnx
dotnet build Dataphor.slnx -c Release
dotnet test Dataphor.slnx
```

CI (`.github/workflows/build.yml`, GitHub Actions on `windows-latest`) runs these same commands in Debug and Release on every push and pull request to `master` and `modernization` that changes more than `tickets/` or `docs/`, and uploads the TRX test results as the artifacts `test-results-Debug` and `test-results-Release`; change the workflow when you change these commands.

`dotnet test Dataphor.slnx` (add `-c Release` for Release, `--no-build` after a build) runs every test project in the solution through VSTest; test projects reference `Microsoft.NET.Test.Sdk`, `NUnit` and `NUnit3TestAdapter` directly (no shared test props file: `tests/` also holds library projects) and stay out of `tools/layout/Layout.csproj`. `DAE.Server.Tests` starts its servers in-process, each in a fresh folder under `%TEMP%\Dataphor.Tests\` with its own copy of the library folders it needs, logging off, and deletes the folder afterwards (a folder it cannot delete, because the process still has a library assembly from it loaded, is deleted by the next run). Set `DATAPHOR_TEST_LOGGING=1` to diagnose a failure: the servers then log, every folder is kept with its instance log, catalog and library copies (their paths print with `--logger "console;verbosity=normal"`), and the next run without the variable deletes them. The cost: in an elevated process a logging server also creates the Windows event-log source `Dataphor Server` and writes to the event log, which the default run never does.

**D4 coverage scripts.** `DAE.Server.Tests` runs every script in the layout's `Libraries/TestFramework.Coverage.Scripts/Documents/` as its own test case (named after the script), each on a fresh server copied from one template instance that the run builds first, and checks it against `tests/DAE.Server.Tests/coverage-manifest.json` (embedded in the test assembly; the format is described on `CoverageManifest` in `CoverageManifest.cs`). Each entry says `pass`, `known-failure` (with the slug of the ticket that owns the cause and a substring the error must contain) or `unstable` (varies between identical runs; reported, never fails). A green run means every `pass` script passed and every `known-failure` script failed in its recorded way; a known failure that now passes, or fails differently, fails the run, as does a script with no entry or an entry with no script. VSTest reports known and unstable cases as **Skipped**; nothing else in the suite is skipped. Change the manifest with the code: a fix that makes a script pass flips its entry to `pass` in the same commit, and a new script needs an entry (entries are sorted by script name in ordinal order). Never edit a coverage script to make it pass. The run takes about 2.5 minutes, 25 s of it the template waiting for a SQL Server on `localhost` before its helper library fails to register (K14, until `test-decouple-coverage-base`; on a machine where a default SQL Server instance accepts the connection the helper library registers, a local database is touched, and its 25 `known-failure` scripts fail as "now passes"). One script, or a few, with that same template cost:

```
dotnet test tests/DAE.Server.Tests --filter Name=Lexer
dotnet test tests/DAE.Server.Tests --filter "Name=Lexer|Name=Transaction.Atomicity.Run" --logger "console;verbosity=normal"
```

The per-ticket summary (pass and known-failure counts per ticket) and each known failure's error text print only with `--logger "console;verbosity=normal"`; at the default verbosity `dotnet test` shows the totals.

Name the `.slnx`: the legacy `Dataphor.sln` is still at the root, and it no longer builds (its .NET Framework projects reference the converted `net10.0` ones); `build-remove-legacy-build` deletes it. Output goes to `artifacts/bin/<project file name>/<debug|release>/`, intermediate files to `artifacts/obj/`; nothing is built into source folders. Shared settings are in `Directory.Build.props`, package versions in `Directory.Packages.props`.

**Runtime layout.** The build also assembles `artifacts/layout/<debug|release>/` (lower case, like the `artifacts/bin` folders): `bin/` holds the core and server assemblies with their package runtime files, and `Libraries/<Name>/` holds each library's tracked files (`.d4l`, `Documents/`, `Upgrades/`, …) with its assembly and that assembly's private package files, flattened for Windows x64. `Libraries/` combines the shipped, sample and test libraries (`libraries/`, `samples/`, `tests/libraries/`) in one folder, as the server expects, minus the four whose C# project is not converted yet (PGSQLConnection, PGSQLDevice, ODBCConnection, Sample.Shipping). `tools/layout/Layout.csproj` is the only project that writes there; it mirrors the folder (deletes what it did not put there) and fails the build if a `.d4l` lists a file the layout does not provide. Find the folder through the MSBuild property `DataphorLayoutDir` (`dotnet msbuild tools/layout/Layout.csproj -getProperty:DataphorLayoutDir`), never by spelling the path. `bin/` has no host yet (no `.exe`, `.deps.json` or `.runtimeconfig.json`) and nothing runs from it until `host-generic-host`. Tests and local runs copy `Libraries/` (or the library folders they need) and point `LibraryDirectories` at the copy, never at the layout or the repository: a server rewrites `.d4l` files and creates library folders in its library directory.

**Converted so far** (SDK-style, in `Dataphor.slnx`): Base, Base.Windows, DAE, Server (without its WCF service and host classes, which return with CoreWCF in `protocol-corewcf-host`), Store.SQLite, Common, NativeCLI, Client, Connection, SQLDevice, MSSQLDevice, SimpleDevice, Frontend (server side), System.Platform, System.Internet, System.Integration, TestFramework.Coverage.Base (`DAE.Diagnostics`), Common.Tests, DAE.Server.Tests. Everything else (the SQL Server and PostgreSQL catalog stores, the other libraries, `DAE.Client` and the UI, the hosts, the other tests) is still in legacy format and builds only through the spike:

```
python tools/net-spike/run.py <work-dir> --smoke
dotnet run --project tools/net-spike/EngineCheck -c Debug
```

The spike needs Python 3, and its work dir must be outside the repo. It builds the converted projects in place (into `artifacts/`) and wraps the other 11 of its 30 projects in generated projects in the work dir, then starts a server in-process on a SQLite catalog and runs concurrent DDL. (Its `--coverage` mode is gone: the coverage scripts run under `dotnet test`, above.) Expected today: the converted projects 0 errors; every wrapper that compiles `DAE.Client` (the client, UI and IDE) fails on the missing `DataphorServiceHost`, and `DAEServer` and `DAE.Service` fail on it too, until `protocol-corewcf-host`; smoke 1,080 operators and 23 scalar types; concurrency some "database is locked" errors that vary between runs (K10, until `store-sqlite-concurrency`); resolution cache 0 errors. The engine check compares the converted assemblies' resource names with the legacy names and runs D4 on a bare engine; expected `RESULT: pass`. See [tools/net-spike/README.md](tools/net-spike/README.md).

**Converting another project.** The reasons are in the `Directory.Build.props` comments and plan WS1.

- Generate the new file from the legacy one and keep its explicit `Compile` and `EmbeddedResource` items, in order: default items are off in `Directory.Build.props`, because uncompiled files sit next to compiled ones.
- Copy each resource's `DependentUpon` metadata. The SDK's dependent-upon naming convention is off, so resource names follow the legacy rules; verify them by listing `Assembly.GetManifestResourceNames()` against the legacy names, as the engine check does for the converted assemblies.
- Constants: the legacy Release set minus `SIGNASSEMBLIES` and `DISABLE_PERFORMANCE_COUNTERS`, the same in both configurations (so `TRACE` only where legacy Release had it), plus `DEBUG;TRACE` in Debug. Set `DefineConstants` outright, never appended to `$(DefineConstants)`.
- No `OutputPath`. `PackageReference` items carry no version; add the version to `Directory.Packages.props`.
- Warnings stay visible. Never suppress `CS0618`, `SYSLIB*`, `WFDEV*`, `CA1416` or `NU1900`–`NU1904`; any `NoWarn` code goes in `Directory.Build.props` with a reason.
- Output is keyed by project file name, so two projects with the same file name collide; today only `DAE.Connection.ODBC.csproj` appears twice, until `device-connection-cleanup`.
- Version, company, product, copyright and signing come from `Directory.Build.props` (version `4.0.0-preview`, `AssemblyVersion` `4.0.0.0`, signed with `Dataphor.snk` in every configuration, deterministic). In the project's `Properties/AssemblyInfo.cs` delete `AssemblyVersion`, `AssemblyFileVersion`, `AssemblyCompany`, `AssemblyProduct`, `AssemblyCopyright`, `AssemblyTrademark`, `AssemblyCulture`, `AssemblyConfiguration`, `AssemblyDelaySign`, `AssemblyKeyFile`, `AssemblyKeyName`, the `#if SIGNASSEMBLIES` block and the template comments about them; keep `AssemblyTitle`, `AssemblyDescription`, `DAERegister`, `ComVisible` and `Guid`, and the file's encoding and line endings. A leftover attribute fails the build with CS0579, a leftover `3.1.*` with CS8357.
- Add the project to `tools/layout/Layout.csproj`: to the Library list for a library project (in `<Name>/Source/`, next to `<Name>.d4l`), to the Bin list for anything else that ships (test projects stay out; a `net10.0-windows` project first needs the change the `NOTE:` in that file describes). For a library whose folder that file leaves out, remove the exclusion. A library missing from the list fails the build at the `.d4l` check.
- Add the project to `Dataphor.slnx`. Once its project file is SDK-style, the spike builds it in place and would ignore a spike `PATCHES` entry for its files, so remove each such entry in the same change, landing the fix in the repository unless another ticket owns it (`run.py` stops with a message otherwise).

## Rules that are easy to break

- **Compatibility surfaces — do not change without a ticket that says so:** D4 semantics and operator signatures; D4 class names (the strings in `create operator … class "Lib.XNode"`, registered by each assembly's `DAERegister.GetClasses()`); assembly names; `.d4l`/`.dfd`/`.dfdx`/`.dnt`/BOP formats; error codes (`docs/reference/Error Message Prefix Codes.txt`); WCF contracts in `src/engine/Server/Contracts` + `src/protocol/Client/Contracts` (two copies that must stay in step); ports 8060/8061.
- **Formatting:** tabs and CRLF in most source files; a UTF-8 byte-order mark in some `.cs` and `.d4l` files and not others (about 40 % of `.cs` files have one). Preserve whatever each file has. The root `.editorconfig` and `.gitattributes` describe this and deliberately set no charset, final-newline or `text=auto` rule. Never reformat or re-encode a file you are not otherwise changing.
- **A file on disk is not necessarily compiled.** Legacy csproj files list sources explicitly; strays exist (`src/client/DAE.Client/View-263.cs`, most of `libraries/Connection/Source`). Check the csproj before trusting or fixing a file.
- **Same-named files exist in both `src/engine/DAE` and `src/engine/Server`** (`SecurityNodes.cs`, `LibraryNodes.cs`, `CatalogNodes.cs`, …). They are different files; check both.
- **Behaviour switches are `#define`s at the top of individual files** plus csproj constants; `docs/reference/CompilerDefines.txt` is the index. `NILPROPOGATION` (sic) etc. are load-bearing.
- **A running server writes into its library directory** (creates `General/`, may rewrite `.d4l` files) and defaults its instance directory to `%ProgramData%\Alphora\Dataphor\Instances\<name>`. Tests and experiments must set `InstanceDirectory` to a temp path and point `LibraryDirectories` at a *copy* of the runtime layout's `Libraries/` (or of the library folders they need), never at the layout or the repo.
- **Catalog store ≠ device.** A *store* persists the server's own catalog (SQL CE by default today; SQLite going forward). A *device* is a D4 object that maps tables to an external DBMS or to memory.
- **On .NET 10, "it compiles" does not mean "it runs".** WinForms' old `ContextMenu`, `MenuItem`, `MainMenu`, `ToolBar` and `StatusBar` types exist again but throw `PlatformNotSupportedException` when constructed (warning `WFDEV006`); remoting and `Delegate.BeginInvoke` members compile and throw the same way. Never silence `WFDEV006` or `SYSLIB` warnings to get a build green.
- The engine's built-in admin user is `Admin` with an empty password; tests connect as `new SessionInfo("Admin", "")`.

## Tickets (tess)

Project uses [tess](tess/) for AI-driven ticket management. Read + follow ticket workflow rules in tess/agent-rules/tickets.md. Tickets in [tickets/](tickets/) directory. If asked to "tend the garden" or similar, see tess/agent-rules/tend.md.

Project-specific ticket rules (testing policy, compatibility, source hygiene) are in [tickets/rules/10-dataphor.md](tickets/rules/10-dataphor.md) and are appended to every ticket prompt.

## Code search (tess)

**First tool** for any "where / how / why" question about this codebase: local code-aware index wired to `mcp__code-search__*`. Reach for `grep`/`Glob` only when you know exact filename or literal string. Pick right sub-tool — not interchangeable.

**Decision rule:**

- Query identifier-shaped (single symbol, camelCase, snake_case, or name list like `fooBar bazQux`)? → `find_references`.
- Query prose ("where do we evict pages", "what handles JWT refresh", identifier unknown)? → `search_code`.
- About to run more than one `grep` to reconstruct context? → run `search_code` first instead. Payoff moment, even when you know an identifier.

`search_code` embeds query as natural language. Identifier-bag queries can work when identifiers co-locate in real code, but prose phrasing more reliable. If `search_code` returns weak-top warning, relative-percentage ranking unreliable — switch to `find_references` or rephrase as prose. Do **not** trust ordering on noisy results.

**Tools:**

- `search_code(query, k?, path_filter?)` — semantic search. Scores relative within each result set, not absolute. `k` defaults to 5 (max 50) — raise for broad sweeps, lower when top hit enough. `path_filter` is SQL LIKE pattern, e.g. `"packages/lamina/%"`.
- `find_references(symbol, max?, path_filter?)` — literal substring; `|` ORs alternatives (`Foo|Bar`). Returns every hit (capped by `max`, default 50, max 500). Indexed replacement for `grep` on identifiers.
- `read_chunk(path, start_line, end_line)` — expand snippet from either tool without separate `Read`.

**Fallbacks:**

- Use `grep`/`Glob` only for filename patterns, regex with anchors/lookarounds, or when you need *every* literal hit (index is chunk-granular, may miss adjacent matches inside one chunk).
- Never fall back to `grep` when `find_references` suffices — strictly slower, pulls more bytes.
- If the `mcp__code-search__*` tools are not connected in your session, the same index is reachable from a shell: `node tess/scripts/search.mjs "<prose query>"`, `node tess/scripts/search.mjs --refs "<symbol>"`.

**What's indexed:** git-tracked project source, minus `node_modules/`, `dist/`, `build/`, `.git/`, `tickets/`, `team/`, `docs/`, few cache dirs. If query about prose-heavy material (long-form architecture docs, design notes, nested READMEs) returns nothing, file may be outside indexed set — fall back to `Read`/`Glob` for those paths. Projects can override filter via `tickets/index-config.json` (see tess README § Customize what gets indexed).

This project's `tickets/index-config.json` adds D4 sources (`.d4`, `.d4l`) and WiX/MSBuild files (`.wxs`, `.props`, `.targets`), and drops dead, vendored or bulky trees (`legacy/`, `Utilities/`, `external/`, `Documentation/`, `tests/libraries/TestFramework.Coverage.Devices/Documents/`). Use `Read`/`Glob` for those.

## Mannered prose

Mannered prose substitutes metaphor and flourish for direct statement. Instead of "a parameter worth varying," the mannered writer produces "a dial worth turning." Instead of "this point still matters," they write "this point earns its keep." The phrases exist to display the writer, not to convey the idea, and readers can tell. That is why mannered prose irritates: it makes the reader work harder so the writer can perform. It is also imprecise. Metaphors drag in connotations the writer did not choose and cannot control. The fix is to say what you mean. When a literal phrase is available, use it.
