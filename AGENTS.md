# Dataphor

Dataphor is a relational application platform: a database engine with its own language (**D4**) that federates SQL back ends, plus a declarative UI layer (**Frontend**) that derives forms from the schema, a WinForms client that renders them, and an IDE (**Dataphoria**). C#, ~400k lines, written 2000–2019 for .NET Framework. Customer applications are *D4 libraries + form documents*, not C# — their behaviour is what must not break.

**The repo is mid-modernization (.NET Framework 4.5.2 → .NET 10).** Before any non-trivial work read:

- [docs/modernization-plan.md](docs/modernization-plan.md) — the master plan: workstreams, decisions, sequencing, known defects. Tickets anchor to its sections.
- [docs/architecture.md](docs/architecture.md) — how the pieces fit, what talks to what, where things live on disk.
- [docs/migration-notes.md](docs/migration-notes.md) — running list of changes an application or custom extension will notice. If your change is one, add it there in the same commit.

Settled direction (details in the plan): .NET 10 only, no .NET Framework build; SQLite catalog store, SQL CE deleted, catalogs rebuilt from scripts rather than migrated; SQL Server and PostgreSQL are the supported devices; WinForms client and IDE kept; web client, Silverlight removed; FHIR and web prototypes parked.

## Layout

We own this codebase.  **This is the layout before the reorganization.** The plan's "Repository layout" section gives the target tree (`src/`, `libraries/`, `samples/`, `tests/`, …) and an old → new table; the ticket that performs the move rewrites this table.

| Path | What |
|---|---|
| `Dataphor/Base` | Shared utilities and **BOP**, the XML object serializer used for forms, `.d4l` files and config (assembly `Alphora.Dataphor`) |
| `Dataphor/DAE` (`Engine.csproj`) | The engine: D4 lexer/parser/compiler, plan nodes (`Runtime.Instructions/`), schema, memory device, catalog device, store abstraction |
| `Dataphor/Server` | The full server on top of the engine: persistent catalog, library loading, WCF services, .NET ADO connections |
| `Dataphor/DAE.SQLite`, `DAE.SQLCE`, `DAE.MSSQL`, `DAE.PGSQL` | Catalog *store* back ends (where the catalog persists) — not to be confused with devices |
| `Dataphor/Common`, `Client`, `DAE.NativeCLI` | WCF contracts/binding, client proxy + client-side catalog cache, simplified "native" call interface |
| `Dataphor/DAE.Client`, `DAE.Client.Controls`, `DAE.Client.Provider` | `DataSession`/`DataView` data-binding model, WinForms data-aware controls, ADO.NET provider |
| `Dataphor/Frontend.Client`, `Frontend.Client.Windows(.Executable)` | UI node model + form loader; the WinForms renderer and the Windows client exe |
| `Dataphor/Dataphoria` | The IDE (WinForms) |
| `Dataphor/DAEServer`, `DAE.Service`, `DAE.Service.ConfigurationUtility`, `D4Runner` | Console host, Windows service, tray config tool, script runner |
| `Libraries/<Name>/` | D4 libraries: `<Name>.d4l` descriptor, `Documents/` (D4 scripts, forms), optional C# in `Source/`. Devices (`MSSQLDevice`, `SQLDevice`, …), `Frontend` (server side of the UI), `System.*`, samples, and the D4 test scripts (`TestFramework.Coverage.Scripts`) |
| `Tests/` | NUnit projects; `DAE.Server.Tests` runs the D4 coverage scripts in-process |
| `Setup/` | WiX installer (v3.5, being replaced) |
| `Documentation/` (AsciiDoc), `Legacy/Documentation/` (DocBook) | User docs, being converted to Markdown under `docs/` |
| `Dataphoria/` (top level), `Dataphor/*Silverlight*`, `Frontend.Client.Web*`, `Frontend.Client.Browser`, `dataphor-mono/`, `Utilities/` | Dead or parked — see the plan before touching |
| `tools/net-spike/` | Throwaway harness that builds and runs the code on modern .NET without modifying the repo |

## Build and test

There is **no working in-repo build yet**: the legacy `Dataphor/Dataphor.sln` needs Visual Studio/MSBuild and .NET Framework 4.5.2 targeting packs, and nothing here builds with `dotnet build`. Plan workstream WS1 replaces it. **The ticket that lands the SDK-style build must rewrite this section.**

Until then the only build/test loop is the spike (needs Python 3 and the .NET 10 SDK pinned by `global.json`; work dir must be outside the repo):

```
python tools/net-spike/run.py <work-dir> --smoke --coverage
```

It compiles 32 projects against .NET 10, boots the engine in-process on a SQLite catalog, runs concurrent DDL, then runs the D4 coverage scripts. Expected today: engine/server/libraries 0 errors, concurrency 0 errors, coverage `pass=62 fail=14`. See [tools/net-spike/README.md](tools/net-spike/README.md).

## Rules that are easy to break

- **Compatibility surfaces — do not change without a ticket that says so:** D4 semantics and operator signatures; D4 class names (the strings in `create operator … class "Lib.XNode"`, registered by each assembly's `DAERegister.GetClasses()`); assembly names; `.d4l`/`.dfd`/`.dfdx`/`.dnt`/BOP formats; error codes (`Dataphor/Error Message Prefix Codes.txt`); WCF contracts in `Dataphor/Server/Contracts` + `Dataphor/Client/Contracts` (two copies that must stay in step); ports 8060/8061.
- **Formatting:** tabs, CRLF, UTF-8 with BOM in most `.cs` files. Preserve them. Never reformat or re-encode a file you are not otherwise changing.
- **A file on disk is not necessarily compiled.** Legacy csproj files list sources explicitly; strays exist (`DAE.Client/View-263.cs`, most of `Libraries/Connection/Source`). Check the csproj before trusting or fixing a file.
- **Same-named files exist in both `Dataphor/DAE` and `Dataphor/Server`** (`SecurityNodes.cs`, `LibraryNodes.cs`, `CatalogNodes.cs`, …). They are different files; check both.
- **Behaviour switches are `#define`s at the top of individual files** plus csproj constants; `Dataphor/CompilerDefines.txt` is the index. `NILPROPOGATION` (sic) etc. are load-bearing.
- **A running server writes into its library directory** (creates `General/`, may rewrite `.d4l` files) and defaults its instance directory to `%ProgramData%\Alphora\Dataphor\Instances\<name>`. Tests and experiments must set `InstanceDirectory` to a temp path and point `LibraryDirectories` at a *copy* of `Libraries/`, never the repo.
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

This project's `tickets/index-config.json` adds D4 sources (`.d4`, `.d4l`) and drops dead or vendored trees (`Legacy/`, `Utilities/`, `Documentation/`, Silverlight, vendored JavaScript, generated FHIR scripts). Use `Read`/`Glob` for those.

## Mannered prose

Mannered prose substitutes metaphor and flourish for direct statement. Instead of "a parameter worth varying," the mannered writer produces "a dial worth turning." Instead of "this point still matters," they write "this point earns its keep." The phrases exist to display the writer, not to convey the idea, and readers can tell. That is why mannered prose irritates: it makes the reader work harder so the writer can perform. It is also imprecise. Metaphors drag in connotations the writer did not choose and cannot control. The fix is to say what you mean. When a literal phrase is available, use it.
