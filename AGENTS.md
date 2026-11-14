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
| `tests/` | NUnit projects; `DAE.Server.Tests` runs the D4 coverage scripts in-process. `tests/libraries/` holds the D4 test libraries (`TestFramework*`); `TestFramework.Coverage.Scripts` has the coverage scripts |
| `external/` | Third-party binaries not yet replaced by NuGet packages (text editor, docking, Ascend, Npgsql, Mono.Security, System.Data.SQLite) |
| `docs/` | Plan, architecture, migration notes; `docs/reference/` holds `CompilerDefines.txt` and `Error Message Prefix Codes.txt` |
| `tools/` | `net-spike/` (throwaway harness that builds and runs the code on modern .NET without changing repository source; build output goes to `artifacts/`) and `editor-support/` |
| `Setup/` | WiX installer (v3.5, being replaced) |
| `Documentation/` (AsciiDoc), `legacy/Documentation/` (DocBook) | User docs, being converted to Markdown under `docs/` |
| `legacy/` | Parked code, excluded from the build and not to be fixed: FHIR/CQF/PHINVADS libraries, the Oracle/ADO/SAS device and connection libraries and the DocBook generator library `Documentation` (`legacy/libraries/`), the web and cloud prototypes (`legacy/Dataphoria/`, `legacy/Processor/`, `legacy/Design/`), the DocBook sources (`legacy/Documentation/`) |
| `Utilities/Wix` | The copy of WiX 3.5 that `Setup/` builds with; deleted with `Setup/` when WS9 lands |

## Build and test

Needs only the .NET 10 SDK that `global.json` pins.

```
dotnet build Dataphor.slnx
dotnet build Dataphor.slnx -c Release
```

Name the `.slnx`: the legacy `Dataphor.sln` is still at the root, and it no longer builds (its .NET Framework projects reference the converted `net10.0` ones); `build-remove-legacy-build` deletes it. Output goes to `artifacts/bin/<project file name>/<debug|release>/`, intermediate files to `artifacts/obj/`; nothing is built into source folders. A runtime layout with hosts and libraries side by side (`artifacts/layout/`) arrives with `build-runtime-layout`. Shared settings are in `Directory.Build.props`, package versions in `Directory.Packages.props`.

**Converted so far** (SDK-style, in `Dataphor.slnx`): Base, Base.Windows, DAE, Common, NativeCLI, Client, SQLDevice, System.Platform, System.Internet, System.Integration. Everything else (Server, the catalog stores, the other libraries, the UI, the hosts, the tests) is still in legacy format and builds only through the spike, which is also the only way to run the D4 coverage scripts until `test-harness-inprocess`:

```
python tools/net-spike/run.py <work-dir> --smoke --coverage
dotnet run --project tools/net-spike/EngineCheck -c Debug
```

The spike needs Python 3, and its work dir must be outside the repo. It builds the converted projects in place (into `artifacts/`) and wraps the other 20 of its 30 projects in generated projects in the work dir, boots the engine in-process on a SQLite catalog, runs concurrent DDL, then runs the coverage scripts. Expected today: engine/server/libraries 0 errors, smoke 1,080 operators and 23 scalar types, concurrency 0 errors, resolution cache 0 errors, coverage `pass=62 fail=14` (the 14 are listed in plan Appendix A). The engine check compares the converted assemblies' resource names with the legacy names and runs D4 on a bare engine; expected `RESULT: pass`. See [tools/net-spike/README.md](tools/net-spike/README.md).

**Converting another project.** The reasons are in the `Directory.Build.props` comments and plan WS1.

- Generate the new file from the legacy one and keep its explicit `Compile` and `EmbeddedResource` items, in order: default items are off in `Directory.Build.props`, because uncompiled files sit next to compiled ones.
- Copy each resource's `DependentUpon` metadata. The SDK's dependent-upon naming convention is off, so resource names follow the legacy rules; verify them by listing `Assembly.GetManifestResourceNames()` against the legacy names, as the engine check does for the ten.
- Constants: the legacy Release set minus `SIGNASSEMBLIES` and `DISABLE_PERFORMANCE_COUNTERS`, the same in both configurations (so `TRACE` only where legacy Release had it), plus `DEBUG;TRACE` in Debug. Set `DefineConstants` outright, never appended to `$(DefineConstants)`.
- No `OutputPath`. `PackageReference` items carry no version; add the version to `Directory.Packages.props`.
- Warnings stay visible. Never suppress `CS0618`, `SYSLIB*`, `WFDEV*`, `CA1416` or `NU1900`–`NU1904`; any `NoWarn` code goes in `Directory.Build.props` with a reason.
- Output is keyed by project file name, so two projects with the same file name collide; today only `DAE.Connection.ODBC.csproj` appears twice, until `device-connection-cleanup`.
- Add the project to `Dataphor.slnx`. Once its project file is SDK-style, the spike builds it in place and would ignore a spike `PATCHES` or `STUBS` entry for its files, so remove each such entry in the same change, landing the fix in the repository unless another ticket owns it (`run.py` stops with a message otherwise).

## Rules that are easy to break

- **Compatibility surfaces — do not change without a ticket that says so:** D4 semantics and operator signatures; D4 class names (the strings in `create operator … class "Lib.XNode"`, registered by each assembly's `DAERegister.GetClasses()`); assembly names; `.d4l`/`.dfd`/`.dfdx`/`.dnt`/BOP formats; error codes (`docs/reference/Error Message Prefix Codes.txt`); WCF contracts in `src/engine/Server/Contracts` + `src/protocol/Client/Contracts` (two copies that must stay in step); ports 8060/8061.
- **Formatting:** tabs and CRLF in most source files; a UTF-8 byte-order mark in some `.cs` and `.d4l` files and not others (about 40 % of `.cs` files have one). Preserve whatever each file has. The root `.editorconfig` and `.gitattributes` describe this and deliberately set no charset, final-newline or `text=auto` rule. Never reformat or re-encode a file you are not otherwise changing.
- **A file on disk is not necessarily compiled.** Legacy csproj files list sources explicitly; strays exist (`src/client/DAE.Client/View-263.cs`, most of `libraries/Connection/Source`). Check the csproj before trusting or fixing a file.
- **Same-named files exist in both `src/engine/DAE` and `src/engine/Server`** (`SecurityNodes.cs`, `LibraryNodes.cs`, `CatalogNodes.cs`, …). They are different files; check both.
- **Behaviour switches are `#define`s at the top of individual files** plus csproj constants; `docs/reference/CompilerDefines.txt` is the index. `NILPROPOGATION` (sic) etc. are load-bearing.
- **A running server writes into its library directory** (creates `General/`, may rewrite `.d4l` files) and defaults its instance directory to `%ProgramData%\Alphora\Dataphor\Instances\<name>`. Tests and experiments must set `InstanceDirectory` to a temp path and point `LibraryDirectories` at a *copy* of `libraries/` (plus `tests/libraries/` or `samples/` when needed), never the repo.
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
