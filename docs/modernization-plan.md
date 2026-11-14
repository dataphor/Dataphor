# Dataphor modernization plan

Master plan for bringing Dataphor from .NET Framework 4.5.2 (last release line 3.1, last commit October 2019) to a maintained product on .NET 10, so that the applications built on it — first among them a medical system that has been unable to take platform updates for over a decade — can move to current operating systems and runtimes without being rewritten.

Companion documents: [architecture.md](architecture.md) (how the system works today) and [../AGENTS.md](../AGENTS.md) (working rules for agents). The evidence behind the claims here is reproducible with [../tools/net-spike](../tools/net-spike/README.md).

## How to use this document

- Work is organized into **workstreams** (WS1…WS14). Each has an objective, the current state, the target, design notes, and a table of **work items** sized to become one ticket each (or one `plan/` ticket that splits further). Work-item slugs are suggestions for ticket slugs.
- **Decisions** (D1…) are recommended defaults. Treat them as settled unless the owner overrides one; do not reopen them inside a ticket. **Open questions** (Q1…) need the owner; a work item that depends on one says so, and should be filed to `blocked/` with the proposed default if it is reached before the answer.
- **Known defects** (K1…) are bugs already found. Each names the workstream that fixes it.
- Tickets should carry `architecture: docs/modernization-plan.md#<section-slug>` (for example `#ws3-test-suite`).
- "Verified" means observed by running code during the baseline survey; "static" means inferred from reading code.

## Goals and non-goals

Goals, in priority order:

1. **Existing applications keep working.** D4 libraries, catalog rebuild scripts and form documents built for 3.x run on the new release with no changes, or with a short documented list of them ([migration-notes.md](migration-notes.md)). Custom C# extensions are recompiled; catalogs are rebuilt, not converted.
2. **Supported platform.** Everything shipped runs on .NET 10 (long-term support until November 2028) on current Windows, builds with the `dotnet` CLI alone, and has no dependency that is out of support.
3. **A safety net.** A comprehensive automated test suite, runnable by one command, that makes later change safe.
4. **Operability.** A real installer with upgrades, a server that installs and runs as a service without hand steps, usable diagnostics, current documentation in Markdown.
5. **A cleaner codebase** — dead code removed, dependencies from NuGet, one build, one version.

Non-goals for this program: redesigning the D4 language, the wire protocol or the UI; a new web or mobile client; Linux or container support for the server (do not make it harder, do not chase it); performance work beyond "not slower than 3.1".

## Where we are starting from

The baseline survey (November 2026) established the following. Details are in [Appendix A](#appendix-a-spike-results) and [Appendix B](#appendix-b-inventory).

| Fact | Consequence |
|---|---|
| Nothing in the repository builds with `dotnet build`; the legacy solution needs Visual Studio 2015+ and 4.5.2 targeting packs; there is no CI | WS1 comes first; nothing else can be verified until it lands |
| **The engine (164k lines), the client stack, the SQL devices and the system libraries compile on .NET 10 essentially unchanged** — seven one-line patches across 32 projects (verified on .NET 8.0.17 and 10.0.15) | The port is a re-hosting job, not a rewrite |
| **The WinForms client compiles on .NET 10 with no source changes and Dataphoria with one error — but that is misleading**: .NET 10 restored the removed menu, toolbar and status-bar types only as shells that throw when constructed (verified) | The UI can be made to *build* early; making it *run* still means replacing ~270 references (WS6) |
| The engine **boots and runs D4 on .NET 10 with a SQLite catalog**; **62 of the 76 D4 coverage scripts it was given pass** with no engine changes (verified) | The existing D4 scripts are a ready-made regression suite; behaviour carried over |
| The SQLite catalog store's concurrency failure is reproduced and understood; three small changes take a 16-thread DDL stress test from dozens of errors to zero and make it ~50× faster (verified) | SQL CE (which has no modern .NET version) can be dropped; see WS4 |
| What does **not** carry over: WCF *server* hosting, .NET Remoting leftovers, a COM+ transaction class, the Windows service installer class, runtime C# compilation via CodeDom, `Delegate.BeginInvoke`, removed WinForms controls (~270 references), third-party UI binaries, the WebForms web client, Silverlight | These are the real work: WS2, WS5, WS6 |
| Tests: 126 NUnit tests, of which 90 are one-liners that each run a D4 script; many need a local SQL Server, machine-wide state or a developer's own paths | WS3 rebuilds the harness around isolation |
| Reference documentation for operators, libraries and form nodes exists only in the legacy DocBook sources; the AsciiDoc books have 206 dead links to it | WS8 converts both sources |
| The installer is WiX 3.5 with no upgrade logic and per-build component GUIDs | WS9 replaces rather than repairs it |
| ~140 MB of the tree is vendored tools, binaries, build outputs and source art; a third of the projects are dead | WS12 |
| Defects found while surveying, including a cross-session race that can corrupt stored catalog scripts | [Known defects](#known-defects) |
| The first application to move runs a **3.1 build from late 2019** — in effect this codebase — on SQL Server, with the Windows client deployed by ClickOnce, many custom C# extensions, and script actions in roughly a hundred forms (owner's answers and a read-only look at the application) | No version gap to bridge; SQL Server is the device that matters; run-time script compilation is critical (WS6); application work itself is deferred (WS13) |
| The repository layout is an accident of history: product code under `Dataphor/`, a second unrelated `Dataphoria/` at the top level, libraries that are simultaneously source and build output, tools and binaries scattered | [Repository layout](#repository-layout): reorganize first, before any project is converted |

## Decisions

| # | Decision | Rationale |
|---|---|---|
| D1 | Target **.NET 10**, `net10.0` for engine/server/libraries and `net10.0-windows` for UI. Latest C# language version; nullable reference types stay **off**; no style or naming churn. | LTS; the code compiles as-is, so churn buys nothing and costs history |
| D2 | **.NET 10 is the only target. No .NET Framework build of anything.** | Owner's decision. It removes multi-targeting, conditional compilation for remoting/WCF hosting/SQL CE, and a doubled test matrix. The price is that there is no side-by-side reference runtime; the D4 coverage scripts (about 2,100 assertions written against the old runtime's behaviour) are the oracle instead, and Windows NLS globalization is selected explicitly so string comparison and culture behaviour match what .NET Framework did |
| D3 | **SQLite is the catalog store. SQL CE is deleted outright and existing catalogs are not migrated** — a catalog is rebuilt from the application's scripts, which is already routine maintenance for deployed applications. The SQL Server store stays if it ports trivially. | SQL CE has no modern .NET provider; the SQLite problem is solved (WS4); owner's decision on migration |
| D4 | **Keep the WCF contracts and wire format.** Server on CoreWCF, clients on the `System.ServiceModel.*` client packages. No protocol redesign in this program. Clients and server are upgraded together; interoperability with 3.1 binaries is not a requirement. | 88-operation contract with a client-side cache built around it; least change |
| D5 | **Keep WinForms** for the Windows client and Dataphoria, ported in place. Replace removed controls with their modern equivalents; do not redesign screens. | Form documents bind to these node implementations; pixel-level sameness is a feature for a validated medical application |
| D6 | **Remove** Silverlight and the WebForms web client. **Park** (exclude from the build, keep in the tree under `legacy/`) the top-level `Dataphoria/` web and cloud prototypes and the FHIR, CQF and PHINVADS libraries. | Confirmed by the owner. Anything built on the web client is rewritten separately |
| D7 | Product version **4.0**. Assembly names, namespaces, D4 class names and file formats unchanged. Sign every build (all configurations) with `Dataphor.snk`. | Names are recorded in catalogs, `.d4l` files and form documents; signing keeps one identity per assembly |
| D8 | **NUnit** stays the C# test framework; **D4 scripts** stay the main form of engine regression test. | Existing investment; scripts are runtime-independent |
| D9 | Documentation is **Markdown under `docs/`**, readable on GitHub with no site generator. A generator can be added later without changing the sources. | Lowest friction for agents and humans |
| D10 | Installer: **WiX SDK-style project**, built by `dotnet build`, one per-machine x64 MSI from a **self-contained** publish, `MajorUpgrade` with the existing `UpgradeCode`. | No runtime prerequisite on locked-down machines; upgrades work |
| D11 | Dependencies come from **NuGet with central package management**. A binary stays in git only when no package exists, with a note saying why. | Reproducible builds, vulnerability scanning |
| D12 | **No history rewrite.** Large and dead files are removed by ordinary commits. | 1,551 commits of `git blame` are an asset |
| D13 | Compiling the catalog or plans to assemblies is **research** (WS14), started only after the Windows client runs on .NET 10. | It is an optimization and a redesign; it needs the test suite and a baseline first |
| D14 | **Reorganize the repository first**, to the layout in [Repository layout](#repository-layout), by pure `git mv` commits. Assembly names, namespaces and D4 library names do not change — only directories and project file names. | Owner's request. Every later ticket then works in the final structure; moving after projects are converted would mean doing the path work twice |
| D15 | **SQL Server and PostgreSQL are the supported devices** for this program, SQL Server first. Oracle, ODBC and OLE DB support is kept only where it converts without effort; otherwise it is parked for later. | The application in hand uses SQL Server; PostgreSQL has other users (owner) |
| D16 | **Application work is deferred.** Changes an application or a custom extension could notice are recorded as they are made in [migration-notes.md](migration-notes.md). No customer names, paths or schema details are written into this repository. | Owner's sequencing: modernize Dataphor first. The repository is public |

## Open questions for the owner

Answered (November 2026):

| # | Question | Answer |
|---|---|---|
| Q1 | Which Dataphor version does the application run? | Thought to be 3.0-era; the deployed binaries are a 3.1 build from late 2019. Start from the current codebase and adapt the application later |
| Q2 | Which DBMS? | SQL Server, on the customer's own server. The server *version* is still unknown — see Q12. Separately, the PostgreSQL device has users and is to be made to work again (D15) |
| Q3 | Which clients? | Windows client, deployed with ClickOnce. A small web portal built on the web client exists and will be rewritten separately; the web client is retired (D6) |
| Q4 | Custom C#? | Yes, many extensions, some depending on third-party UI and reporting components. Deferred (D16); record what will affect them in the migration notes |
| Q5 | Compatibility corpus? | Available on the owner's machine; never copied into this repository. WS13 locates it by environment variable when that work starts |
| Q6 | `ScriptAction` use? | Heavy: on the order of 240 script actions across about 100 forms. `ui-scriptaction-roslyn` is not optional |
| Q7 | FHIR/CQF and web prototypes? | Park. One wrinkle: the application references four D4 class names registered by the FHIR core library (`FHIRCore.ObjectAsJSON*`, `FHIRCore.FHIRBaseDisplayString`); see `library-park-side-projects` |
| Q9 | Upgrade expectations? | Catalogs are rebuilt from scripts, not migrated; install side by side |
| Q10 | Continuous integration on the repository? | The owner controls the repository; assume yes |
| Q13 | History rewrite to shed old binaries? | No. History-preserving cleanup only (D12) |

Still open:

| # | Question | Why it matters | Default if unanswered |
|---|---|---|---|
| Q8 | Is the `UseServiceConfiguration` option (WCF endpoints from `app.config`) or secured transport used in production? | Scope of WS5 | Not used; plain TCP inside the LAN |
| Q11 | Current WiX releases carry an "Open Source Maintenance Fee" for commercial use; is that acceptable, or should we pin an earlier major version? | WS9 toolchain choice | Verify terms, pin the newest version that fits |
| Q12 | Which SQL Server version and patch level is in production? | Current drivers require TLS 1.2 and encrypt by default; an old server may refuse them | Test against the oldest version still in mainstream use and document the minimum |

## Sequencing

```
Phase 0  Foundation ............ this document, AGENTS.md, spike, tess setup, .NET 10 SDK   (done)
Phase 1  Green loop ............ repository reorganized (D14), then WS1 build + WS3 harness
                                 + WS4 store + first WS2 fixes
                                 gate G1: from a clean clone, `dotnet build` and `dotnet test` pass
                                 on .NET 10 with every D4 coverage script accounted for
Phase 2  Server complete ....... WS2, WS5, WS7 (SQL Server)
                                 gate G2: a hosted .NET 10 server passes the client/server tests,
                                 and a catalog rebuilt from library scripts on a fresh instance
                                 survives a restart
Phase 3  Windows client + IDE .. WS6
                                 gate G3: samples run end to end in the .NET 10 Windows client;
                                 Dataphoria opens, edits and executes
Phase 4  Shippable ............. WS9 installer, WS8 docs complete, WS10 tools
                                 gate G4: installed build passes the release checklist
Phase 5  Hardening ............. WS11 security, remaining WS12, WS14 research
Later    Application ........... WS13, when the owner starts it
```

The reorganization goes first because it touches every path and is cheapest while nothing else is in flight. WS8 (documentation) and the rest of WS12 have no code dependencies and run in parallel from Phase 1. Within a phase, the work-item tables give prerequisites.

## Repository layout

**Target.**

```
/                      AGENTS.md, README.md, LICENSE, CHANGELOG.md, Dataphor.slnx, global.json,
                       Directory.Build.props, Directory.Packages.props, .editorconfig, .gitattributes
src/
  engine/              Base, Base.Windows, DAE, Server, Store.SQLite, Store.MSSQL, Store.PGSQL
  protocol/            Common, Client, NativeCLI
  client/              DAE.Client, DAE.Client.Provider, Frontend.Client
  windows/             DAE.Client.Controls, Frontend.Client.Windows, Frontend.Client.WPF,
                       WindowsClient, Dataphoria, ConfigurationUtility
  hosts/               DAEServer, DAEService, D4Runner            (later: the `dataphor` CLI)
libraries/             shipped D4 libraries, one folder each: <Name>.d4l, Documents/, Upgrades/, Source/
samples/               Sample.* libraries
tests/                 C# test projects; tests/libraries/ holds the D4 test libraries (TestFramework.*)
installer/             WiX project
docs/                  all documentation
tools/                 build and maintenance scripts, editor support files
assets/                icons and images the product actually uses
external/              third-party binaries not yet replaced by packages (shrinks to nothing)
legacy/                parked code, excluded from the build
tess/, tickets/        ticket tooling
artifacts/             build output (ignored)
```

**Old → new.**

| Today | Becomes |
|---|---|
| `Dataphor/Base`, `Dataphor/Base.Windows`, `Dataphor/Server` | `src/engine/Base`, `src/engine/Base.Windows`, `src/engine/Server` |
| `Dataphor/DAE` (`Engine.csproj`) | `src/engine/DAE` (`DAE.csproj`) |
| `Dataphor/DAE.SQLite`, `Dataphor/DAE.MSSQL` | `src/engine/Store.SQLite`, `src/engine/Store.MSSQL` |
| `Dataphor/DAE.SQLCE` | deleted (D3) |
| `Dataphor/DAE.PGSQL` | `src/engine/Store.PGSQL` (ported with the PostgreSQL device; parked only if that proves non-trivial) |
| `Dataphor/Common`, `Dataphor/Client`, `Dataphor/DAE.NativeCLI` | `src/protocol/Common`, `src/protocol/Client`, `src/protocol/NativeCLI` |
| `Dataphor/DAE.Client`, `Dataphor/DAE.Client.Provider`, `Dataphor/Frontend.Client` | `src/client/…` |
| `Dataphor/DAE.Client.Controls`, `Dataphor/Frontend.Client.Windows`, `Dataphor/Frontend.Client.WPF`, `Dataphor/Dataphoria` | `src/windows/…` |
| `Dataphor/Frontend.Client.Windows.Executable` | `src/windows/WindowsClient` |
| `Dataphor/DAE.Service.ConfigurationUtility` | `src/windows/ConfigurationUtility` |
| `Dataphor/DAEServer`, `Dataphor/DAE.Service`, `Dataphor/D4Runner` | `src/hosts/DAEServer`, `src/hosts/DAEService`, `src/hosts/D4Runner` |
| `Dataphor/Language.SQL` | delete if confirmed unreferenced (only the old solution names it); otherwise `src/engine/` |
| `Dataphor/CompilerDefines.txt`, `Dataphor/Error Message Prefix Codes.txt` | `docs/reference/` (WS8) |
| `Libraries/` — Connection, SQLDevice, MSSQLDevice, PGSQLDevice, PGSQLConnection, SimpleDevice, Frontend, Security, General, System.Platform, System.Internet, System.Integration | `libraries/<Name>` (internal structure unchanged) |
| `Libraries/Sample.*` | `samples/` |
| `Libraries/TestFramework*` | `tests/libraries/` |
| `Libraries/` — Oracle, ODBC, ADO, SAS devices and connections, Documentation | `legacy/libraries/` (D15), except what `library-remove-dead-devices` deletes |
| `Libraries/FHIR*`, `CQF*`, `PHINVADS*`, `TestCQL`, `TestCache`; top-level `Dataphoria/`; `Processor/`; `Design/` | `legacy/` (D6) |
| `Tests/Common.Tests`, `Tests/DAE.Server.Tests`, `Tests/DAE.Client.Tests` | `tests/…` (restructured by WS3) |
| `Setup/` | replaced by `installer/` (WS9); old one deleted |
| `Images/` | `assets/` (referenced files only) |
| `External/`, binaries in `Dataphor/bin`, `Dataphor/Dataphoria/*.dll` | `external/` until replaced by packages |
| `Tooling/BBEdit` | `tools/editor-support/bbedit` |
| `Documentation/`, `Legacy/Documentation/` | stay where they are until WS8 converts and deletes them |
| `Utilities/`, `dataphor-mono/`, Silverlight projects, `Frontend.Client.Browser`, `Frontend.Client.Web*`, `Dataphor/BOP`, `Libraries/Libraries.sln`, `Dataphor.proj`, build batch files | deleted |

**Rules for the move.**

- Delete and park first, then move what is left; there is no point relocating dead code.
- A move commit contains **only renames** (`git mv`), no content edits, so that Git records exact renames and `git log --follow` and `git blame` keep working across the move. Reference fixes (project paths, solution, scripts, documentation) go in the commits after it.
- Case-only renames (`Libraries` → `libraries`, `Tests` → `tests`) need two steps on Windows (`git mv Libraries libraries.tmp`, then to `libraries`).
- Proof that the move changed nothing: `tools/net-spike` gives the same results before and after (update its path tables in the reference-fix commit).
- The same ticket updates `AGENTS.md`, `README.md` (its links into `Documentation/` and `Libraries/`), `docs/architecture.md`, `tickets/index-config.json` and the paths cited in this plan (the [known defects](#known-defects) table and the appendices cite pre-move paths until then).
- The server discovers libraries as the immediate subfolders of each directory in its `LibraryDirectories` setting (a semicolon-separated list), so splitting shipped, sample and test libraries into separate source folders is safe; the runtime layout recombines them.

## WS1 Build and workspace

**Objective.** One command builds everything that ships; one command tests it; both work on a clean clone with only the .NET 10 SDK.

**Current state.** 88 legacy-format csproj files with explicit file lists, seven solutions (one current), per-project `AssemblyInfo.cs` versions (`3.1.*`), signing only in Release, core assemblies built into `Dataphor/bin`, library assemblies built *into their source folders* next to the `.d4l`, four third-party DLLs tracked inside `Dataphor/bin`, packages restored to a gitignored `Dataphor/packages`. `Dataphor.proj` and two batch files assemble a `Deploy/` tree by wildcard copy.

**Target.**

- The directory structure in [Repository layout](#repository-layout).
- SDK-style projects with unchanged assembly names, sharing settings through a root `Directory.Build.props` (target framework, language version, version number, signing, deterministic builds, NLS globalization, warning baseline) and `Directory.Packages.props`. The warning baseline must leave `WFDEV006` and the `SYSLIB` obsoletion warnings visible: on .NET 10 several removed APIs compile and then throw at run time.
- One root solution, `Dataphor.slnx`, with solution filters for the engine/server set and the Windows UI set.
- `global.json` pinning the .NET 10 SDK (present since Phase 0).
- A **runtime layout** produced by the build: `artifacts/layout/<Configuration>/bin/` (hosts and core assemblies) and `artifacts/layout/<Configuration>/Libraries/<Name>/` (each library's `.d4l`, `Documents/`, `Upgrades/`, its assembly and that assembly's private dependencies). Tests, local runs and the installer all consume the layout. Nothing is built into source folders any more.
- Continuous integration on a Windows runner: build, test, publish test results and the MSI.

**Design notes.**

- Convert by generating from the legacy file: keep the exact `Compile` list first (set `EnableDefaultCompileItems=false`), get green, then a later item switches to globbing after deleting the stray files the explicit lists were hiding. [tools/net-spike/convert.py](../tools/net-spike/convert.py) already does the generation out of tree and is the reference.
- `GenerateAssemblyInfo=false` initially (the `AssemblyInfo.cs` files carry `DAERegister` attributes that must stay); a later item moves version, company and signing attributes into the props file and leaves only the Dataphor-specific attributes.
- `AssemblyVersion("3.1.*")` requires non-deterministic builds; replace with a fixed version from the props file.
- Preserve each project's `DefineConstants` exactly ([Appendix B](#appendix-b-inventory)); several are behaviour switches.
- Embedded resources keep their manifest names (`Alphora.Dataphor.DAE.SystemStoreCatalog.sql` is loaded by name; `.resx` exception messages are looked up by type name).
- The server project cannot wait for WS5: the test harness needs `Server` (persistent catalog, libraries) in-process. `build-server-project` therefore compiles it for .NET 10 with the WCF service and host classes left out of compilation until WS5 supplies CoreWCF, and with the remoting and COM+ code removed (the spike's `stubs/Server.cs` lists exactly the members involved).

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `repo-delete-dead-trees` | Delete what the layout table marks deleted (Silverlight, browser shell, WebForms client, empty and template projects, mono solution, stale solutions, `Utilities/NUnit*`, SQL CE project and native DLLs, committed catalog database and logs). Leave `Utilities/Wix` and `Setup/` until WS9 and the documentation trees until WS8. | — | easy |
| `repo-move-to-new-layout` | The moves in the layout table, as rename-only commits, followed by reference fixes; update AGENTS.md, architecture.md, the tess index configuration, the spike's path tables and the paths cited in this plan; spike results unchanged. | `repo-delete-dead-trees`, `library-park-side-projects`, `library-remove-dead-devices` | medium |
| `build-core-sdk-projects` | SDK-style projects for Base, Base.Windows, DAE, Common, NativeCLI, Client, Store.SQLite and the libraries Connection, SQLDevice, MSSQLDevice, SimpleDevice, Frontend, System.Platform/Internet/Integration; `Directory.Build.props`, `Directory.Packages.props`, `Dataphor.slnx`. Apply the Appendix A patches that are plain fixes. Rewrite the "Build and test" section of AGENTS.md. | `repo-move-to-new-layout` | hard |
| `build-server-project` | Server on .NET 10: remove remoting lifetime, tracking and sponsor code (K6) and the COM+ transaction class (K11, with a clear "not supported" error on the D4 surface that reached it); leave the WCF service and host classes out of compilation pending WS5; an in-process `Server` starts and runs D4. | `build-core-sdk-projects` | hard |
| `build-runtime-layout` | MSBuild targets that assemble `artifacts/layout/`; fix library projects whose Release output went to `bin\Release` (K15). | `build-server-project` | medium |
| `build-ci` | GitHub Actions workflow: restore, build, test, upload results. | `test-harness-inprocess` | easy |
| `build-version-and-signing` | Single version source (4.0.0 plus prerelease label), signing in all configurations, generated assembly attributes; remove per-project version attributes. | `build-core-sdk-projects` | easy |
| `build-globbing-cleanup` | Delete source files no project compiles, then switch projects to default globbing. | `ui-dataphoria-port`, `device-connection-cleanup` | medium |
| `build-remove-legacy-build` | Delete remaining legacy solutions and `packages.config` files once every live project is converted. | `ui-dataphoria-port`, `installer-msi` | easy |

**Done when** gate G1's build half holds and AGENTS.md states the real commands and the real layout.

## WS2 Engine and server port

**Objective.** `Alphora.Dataphor.DAE` and `AlphoraDataphorServer` run correctly on .NET 10, not merely compile.

**Current state.** The engine compiles with one dead `using` removed. The server compiles once WCF hosting, remoting and COM+ types are stubbed (WS5 owns WCF). Runtime differences found so far are K4–K9, K11 and K22.

**Design notes.**

- **Remoting leftovers.** `RemoteServerObject` derives from `MarshalByRefObject` and, in `Dispose`, calls `GetLifetimeService()` and `RemotingServices.Disconnect` — both throw on .NET 10. `RemoteServer` registers a remoting tracking handler; `FileStreamProvider` implements `ISponsor`. `build-server-project` removes all of it. What then reclaims sessions abandoned by a vanished client is an open design point (`Service/ConnectionManager.cs` and the client ping are the candidates): `port-abandoned-session-cleanup` establishes it and pins it with a test.
- **`Delegate.BeginInvoke`** (K5) in the D4 asynchronous-execution operators: replace with `Task.Run` or a dedicated thread, preserving the semantics the `DebugLibrary` script checks (exceptions surface on the target process).
- **COM+ / DTC.** `ServerDTCTransaction` is a `ServicedComponent` created by reflection under an assembly name that does not exist (K11), so distributed transactions cannot have worked since the 2009 Engine/Server split. `build-server-project` removes the class and makes the D4 surface that reaches it raise a clear "not supported" error.
- **Globalization.** .NET 10 on Windows uses ICU by default where .NET Framework used NLS; string comparison, casing and sort order can differ in edge cases, and D4 string semantics sit on top of them. Select NLS (`System.Globalization.UseNls`) in the props file for every executable and test host, and pin a handful of comparison cases in a test.
- **Assembly loading.** Libraries load with `Assembly.LoadFrom` and are found again by name through an `AppDomain.AssemblyResolve` hook (`Base.Windows/AssemblyUtility.cs`). This works unchanged on .NET 8 in the spike. Formalize it rather than redesign it: keep everything in the default load context (library assemblies share base types such as `SQLDevice`, and `ClassLoader` resolves by name), add a resolver that probes registered library directories for managed *and native* dependencies, and keep "no unloading". Per-library collectible contexts are out of scope.
- **Configuration.** `ConfigurationManager.GetSection` (K12) and `app.config` trace switches: keep working through the `System.Configuration.ConfigurationManager` package where a setting is real, delete where it is not.
- **Windows-only API** in the server (`EventLog`, `WindowsIdentity`, `Base.Windows.PathUtility`): keep, behind the existing platform check, with the `CA1416` analyzer enabled so that new unguarded uses are flagged.
- **Obsolete but working** API (`AesManaged`, `RNGCryptoServiceProvider`, `HttpWebRequest`, `FtpWebRequest`, serialization constructors on exceptions): leave alone here; WS11 and WS7 own the ones worth changing.

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `port-engine-runtime-fixes` | K2, K4, K5 (engine sites), K22, dead `using` directives; each with its reproduction test. | `test-harness-inprocess` | medium |
| `port-catalog-emitter-race` | K1 with a concurrency reproduction; search for other shared mutable statics in session-scoped code and report them. | `test-harness-inprocess` | easy |
| `port-abandoned-session-cleanup` | Establish and test what reclaims server sessions when a client disappears now that remoting leases are gone; add idle reaping if nothing does. | `protocol-corewcf-host` | hard |
| `port-library-loading` | Resolver for library directories including native assets; test: a library whose assembly has a private NuGet dependency and a native dependency registers from the runtime layout. | `build-runtime-layout` | medium |
| `port-concurrent-ddl-investigation` | K3: stress harness with stack capture until the "collection was modified" site is found; fix or file. | `port-catalog-emitter-race` | medium |
| `port-server-on-net10` | Whole server, including hosting, builds with nothing excluded; all server tests pass. | `port-abandoned-session-cleanup` | medium |

**Done when** every engine-level and server-level test passes on .NET 10 and no code path reaches an API that throws `PlatformNotSupportedException`.

## WS3 Test suite

**Objective.** A comprehensive, isolated, one-command test suite that defines "still works", plus the habit of extending it (see `tickets/rules/10-dataphor.md`).

**Current state.** `Tests/DAE.Server.Tests` starts a `Server` in-process and runs D4 scripts from `Libraries/TestFramework.Coverage.Scripts` (135 scripts, 23,500 lines, about 2,100 assertions written as `raise Error`). 90 are wired to NUnit by hand, 14 of those ignored, one names a script that does not exist, and 45 scripts are not wired at all. The fixture uses SQL CE (being deleted), writes to `%ProgramData%`, reads libraries from the source tree, and registers a helper library (`TestFramework.Coverage.Base`) that requires `TestFramework`, which creates a SQL Server device against `localhost` — so the "in-memory" suite fails without a local SQL Server (K14). Other fixtures depend on a developer's drive paths and server names (K18), a pre-configured machine alias, or a built `DAEServer.exe`.

**Target: layers.**

| Layer | What | How |
|---|---|---|
| L0 Unit | Base utilities, BOP round trips, lexer/parser/emitter | NUnit. One property-style test: every `.d4` file in the repository parses, emits, and re-parses to the same tree |
| L1 Engine scripts | The D4 coverage scripts | Data-driven: discover every script in the library; a checked-in manifest gives each script's expectation (`pass`, or `known-failure` with a ticket slug and the expected error). A script that is not in the manifest fails the run. No silent skips |
| L2 Catalog and store | Store contract (cursors, ranges, nested transaction undo), persistence across restart, concurrent sessions, a catalog rebuilt from library scripts on a fresh instance | NUnit against each store; the spike's concurrency loop becomes a test |
| L3 Devices | D4-to-SQL translation as golden text (no database needed); live execution against SQL Server and PostgreSQL when a connection string is supplied by environment variable | Golden files reviewed in diffs; live tests skipped *visibly* (reported as inconclusive, counted) when no server is configured; CI supplies SQL Server LocalDB |
| L4 Client/server | Hosted server in a child process, real WCF client: connect, plans, cursors, streams, catalog cache synchronization, abandoned-session cleanup | NUnit, ports allocated dynamically |
| L5 Frontend | Server side only: derive every page type for the sample libraries and compare the form XML to golden files; load every `.dfd`/`.dfdx` in the repository through the deserializer; customization merge/difference round trip | Pure engine tests — no UI needed for most of the Frontend's logic |
| L6 UI smoke | Windows client starts, opens a derived browse and an edit form on a sample library, saves a row; Dataphoria starts, executes a script | UI Automation, few tests, run on CI's interactive session |
| L7 Applications | Every sample library registers and derives on the in-memory and Simple devices; later, an external application corpus does the same (WS13) | Corpus location by environment variable |
| L8 Installer | Install the MSI on a clean VM or Windows Sandbox, start the service, connect, upgrade from the previous build, uninstall | Scripted; release gate rather than per-commit |

Cross-cutting requirements:

- **Isolation.** Each fixture gets a fresh temporary instance directory and reads libraries from the runtime layout, never from source folders; no test touches `%ProgramData%`, the registry or a fixed port.
- **The scripts are the oracle** (D2): there is no reference runtime to compare against, so an existing assertion that fails on .NET 10 is presumed to be a porting defect until shown otherwise, and expected values in scripts are never edited to make a run green without a ticket that explains the difference.
- **Decouple the helper library**: `TestFramework.Coverage.Base` must register with no external DBMS (move the SQL Server device into the device-coverage library).
- **Coverage and performance baselines**: line coverage reported (not gated) per assembly; the existing `Timing`/`Timings` scripts plus a small benchmark project record a baseline on .NET 10 so that later regressions are visible. (A like-for-like comparison with 3.1 would need an installed 3.1 server; do it once, by hand, during the pilot.)

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `test-harness-inprocess` | New isolated in-process fixture (temp instance, layout libraries, SQLite store), data-driven script discovery with manifest. Starting manifest: the 62 passing scripts as `pass`, the rest `known-failure`. Port the existing cursor and catalog tests onto it; delete machine-dependent tests after capturing what they intended to check. | `build-runtime-layout` | hard |
| `test-decouple-coverage-base` | K14. Expect the 12 blocked scripts to turn green or reveal real failures. | `test-harness-inprocess` | medium |
| `test-triage-known-failures` | One `fix/` ticket per remaining `known-failure` and per unwired script (group by cause); includes the 14 ignored tests (K20). | `test-decouple-coverage-base` | medium |
| `test-parser-roundtrip-corpus` | L0 property test over all `.d4` files. | `test-harness-inprocess` | easy |
| `test-store-contract` | L2 store contract and restart persistence; replaces `SQLStoreTest`. | `store-sqlite-concurrency` | medium |
| `test-device-sql-golden` | L3 golden translation tests for the SQL Server device; the harness takes the device as a parameter so PostgreSQL reuses it. | `test-harness-inprocess` | medium |
| `test-device-live` | L3 live tests, environment-driven, LocalDB in CI. | `device-mssql-port`, `build-ci` | medium |
| `test-client-server` | L4. | `protocol-corewcf-host` | hard |
| `test-frontend-derivation-golden` | L5. | `test-harness-inprocess` | medium |
| `test-ui-smoke` | L6. | `ui-windows-client-runs` | medium |
| `test-samples-corpus` | L7 for repository samples. | `test-frontend-derivation-golden` | medium |
| `test-perf-baseline` | Benchmarks and timing scripts; publish numbers in `docs/`. | `test-harness-inprocess` | medium |
| `test-coverage-gap-analysis` | With coverage data: list engine areas with no script coverage (candidates: application transactions, server links, security, library upgrade scripts, streams) and file a ticket per area. | `test-triage-known-failures` | medium |

**Done when** gate G1's test half holds, and each later gate's layer is in CI.

## WS4 Catalog store

**Objective.** A reliable embedded catalog store on .NET 10. Existing catalogs are not brought across (D3).

**Diagnosis of the SQLite problem (verified).** Eight sessions each running fifteen create/query/drop cycles against a SQLite-backed server produced 39–47 failures in 60–80 seconds. Three independent causes:

1. **Lock upgrades between connections.** Every catalog device session holds its own store connection and opens a store transaction for the life of the process transaction. SQLite transactions were begun deferred, so two sessions each take a read lock and then both try to write: one must fail with "database is locked", after waiting out the busy timeout. (Isolation level "read uncommitted" is mapped to *no* transaction at all in `SQLiteConnection`, which is harmless but means those statements autocommit.)
2. **Rollback-journal mode**, in which a writer blocks all readers.
3. **A race that has nothing to do with SQLite** (K1): one static `D4TextEmitter` shared by all sessions builds the object script that is stored in the catalog. Under concurrent DDL the text is interleaved. With SQLite the garbled script broke the generated `insert` statement ("SQL logic error … unrecognized token"); with SQL CE, which inserts through a cursor, the garbled text would be *stored* and fail only when the object is next loaded from the catalog.

| Configuration | Errors (120 cycles) | Time |
|---|---|---|
| As found | 39–47 | 60–80 s |
| + emitter fix | 33 ("database is locked" only, plus K3 once) | 72 s |
| + write-ahead logging and a 30 s busy timeout | 17 | 17 s |
| + store transactions begun immediate (write lock up front) | **0** | **1.6 s** |
| Same, 16 sessions × 25 cycles | **0** | 8 s |

**Target design.** SQLite in WAL mode; one writer at a time by construction. The spike's "begin immediate for everything" serializes even read-only store transactions and makes a long user transaction that has touched the catalog hold the write lock; the production version should (a) open the SQLite write transaction lazily, on the first store *write* inside a process transaction, (b) let reads run outside any SQLite transaction (WAL gives each statement a consistent committed snapshot, and the engine's own catalog locks already serialize conflicting DDL), and (c) map busy timeouts to a Dataphor error that names the blocking condition. Validate against the full L1 and L2 suites and a test with a deliberately long-running DDL transaction. Choose between `Microsoft.Data.Sqlite` and `System.Data.SQLite` in the ticket (the spike used the latter, version 1.0.119, unchanged apart from one renamed property).

**No migration.** Deployed applications rebuild their catalog from scripts as routine maintenance, including the users, roles, rights and device users that live only in the store, so a new installation starts from an empty SQLite catalog and the application's own rebuild procedure populates it. SQL CE support is deleted rather than ported: the store project, its native DLLs, the `Connection.SQLCEConnection` class registration in the base `Connection` library (K19), and the server's default store class name. What the product owes this workflow is that a rebuild is fast and scriptable from the command line (WS10) and that the administration guide describes it (WS8).

**Alternative considered — compile the catalog into an assembly.** Deferred to WS14: it replaces a working, now-fixed mechanism with a new one. Because catalogs are rebuilt from scripts anyway, the interesting question it could answer is rebuild and start-up *time*, which `store-rebuild-workflow` measures first.

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `store-sqlite-concurrency` | Implement the target design; concurrency and long-transaction tests; SQLite is the default store class. | `test-harness-inprocess`, `port-catalog-emitter-race` | hard |
| `store-remove-sqlce-references` | K19 and every remaining mention of SQL CE in code, configuration defaults, the instance editing forms and documentation; record the removed D4 class name in the migration notes. | `build-core-sdk-projects` | easy |
| `store-mssql-port` | Port the SQL Server catalog store to `Microsoft.Data.SqlClient` if trivial, else park it; run the store contract tests where a server is configured. | `test-store-contract` | medium |
| `store-rebuild-workflow` | Test and document rebuilding a catalog from scripts on a fresh instance (libraries, security, device users), including a restart afterwards; measure how long the sample set takes. | `store-sqlite-concurrency` | medium |
| `store-parameterized-statements` | The store builds SQL by string concatenation with quote doubling (a long-standing `TODO`). Parameterize inserts and updates; measure. | `test-store-contract` | medium |

**Done when** SQLite is the default, L2 is green, no SQL CE reference remains, and the rebuild workflow is tested.

## WS5 Client server protocol and hosting

**Objective.** A .NET 10 server process that existing and new clients can connect to, running as a console application or a Windows service.

**Current state.** Three self-hosted WCF services on a custom binding (binary encoding over TCP); see [architecture.md](architecture.md#hosting-protocol-discovery). On modern .NET the client side works with the `System.ServiceModel` packages except for two constructors that take an endpoint configuration name. The server side (`ServiceHost`, `ServiceBehavior`, `CustomServiceHost` reading `<system.serviceModel>`) does not exist. The Windows service uses `ServiceBase` and the removed `System.Configuration.Install` installer classes and writes its own registry keys.

**Target.**

- **CoreWCF** hosts the three existing contracts, unchanged, on the same binding, addresses and ports. *First work item is a proof:* a .NET 10 client using the existing client code completes a session (connect, prepare, execute, cursor fetch, stream read) against a CoreWCF-hosted server with the existing binding settings. If the binding cannot be reproduced, stop and bring the options to the owner; do not improvise a new protocol.
- One host built on the .NET Generic Host: `DAEServer` runs in a console; the same executable runs as a Windows service (`UseWindowsService`), with service registration done by the installer and by a CLI command rather than by an installer class.
- Server interface declarations and the client's asynchronous copies stay in step by a test that compares the two contract shapes.
- `UseServiceConfiguration` (endpoints from `app.config`) is removed unless Q8 says it is used.
- Logging through `Microsoft.Extensions.Logging` to the existing log file format and the Windows Event Log.

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `protocol-corewcf-proof` | Minimal CoreWCF host for `IListenerService` and a subset of `IDataphorService`; a full session demonstrated; write up findings. | `build-server-project` | hard |
| `protocol-corewcf-host` | Full hosting of all three services on `net10.0`; replace the four files in `Server/Service`; client constructors fixed. | `protocol-corewcf-proof` | hard |
| `protocol-contract-parity-test` | Reflection test: server and client contract declarations match operation for operation. | `build-server-project` | easy |
| `host-generic-host` | `DAEServer` on the Generic Host, console and service modes, graceful shutdown; retire `DAE.Service`'s installer class. | `protocol-corewcf-host` | medium |
| `host-instance-configuration` | Keep `ServerConfigurations.config` (BOP) readable and writable; document every setting; default new instances to the SQLite store. | `host-generic-host` | easy |
| `protocol-transport-security` | Make the secured-connection options work on CoreWCF, or document their removal (Q8). | `protocol-corewcf-host` | medium |

**Done when** gate G2's hosted-server half holds.

## WS6 Windows client and Dataphoria

**Objective.** The Windows client and the IDE run on `net10.0-windows` and look and behave as before.

**Current state.** About 100k lines of WinForms across `DAE.Client.Controls` (23k), `Frontend.Client.Windows` (21k), `Dataphoria` (29k), `Frontend.Client` (13k), `DAE.Client` (15k) and the service configuration utility. Against `net10.0-windows`, with the checked-in third-party binaries referenced as they are, `DAE.Client.Controls`, `Frontend.Client`, `Frontend.Client.WPF` and `Frontend.Client.Windows` compile with **no errors** and Dataphoria with one (verified). None of it has been *run*. Two things make the clean compile deceptive: the legacy menu, toolbar and status-bar types are present in .NET 10 only as obsolete shells whose constructors throw `PlatformNotSupportedException` (warning `WFDEV006`), and the third-party binaries are .NET Framework assemblies that compile as references but are unproven at run time.

**Consequence for sequencing.** Get the UI projects building first (cheap), keep `WFDEV006` as a visible warning, replace the shells project by project, and promote `WFDEV006` to an error in each project as it is finished so the shells cannot come back.

| Issue | Size | Approach |
|---|---|---|
| Removed controls: `ContextMenu` (103 references), `MenuItem` (127), `MainMenu`, `ToolBar`/`ToolBarButton`, `StatusBar`/`StatusBarPanel` | ~270 references in 4 projects | These compile on .NET 10 but throw when constructed. Mechanical replacement with `ContextMenuStrip`, `ToolStripMenuItem`, `MenuStrip`, `ToolStrip`, `StatusStrip`; Dataphor's own `ToolBar` control in `DAE.Client.Controls` needs a look first |
| Layering: `DAE.Client` and `Frontend.Client` reference WinForms and System.Drawing (designers, `UITypeEditor` attributes, `TerminalServices`, images); `Frontend.Client` references `DAE.Client.Controls`. Consequence: `D4Runner`, the ADO.NET provider and the client tests all need a Windows target | 49 files touch design-time namespaces | Move design-time-only code out or delete it (the Visual Studio designer integration cannot work with the out-of-process designer); make `DAE.Client` a plain `net10.0` library |
| Third-party binaries: ICSharpCode.TextEditor (two different declared versions, one DLL), DockPanel Suite 2.2, Ascend 1.0 | 3 dependencies | DockPanel Suite has a current NuGet package; replace the editor binary with a maintained source or package build of the same control (API-compatible forks exist) — evaluate before choosing; replace the single Ascend use (`FormDesigner/ToolBox`) with a standard control |
| `ScriptAction`: compiles C#/VB at run time with CodeDom | 1 file, and applications use it heavily (Q6) | Roslyn (`Microsoft.CodeAnalysis.CSharp`, `.VisualBasic`); keep the script contract and the reference-resolution rules; cache compiled scripts |
| `Delegate.BeginInvoke` in `ScriptExecutor`, `Thread.ResetAbort` in three catch blocks, `Process.Start(url)` in nine places, assembly-level code-access-security attributes | small | Direct fixes (K5, K7, K9) |
| Resources: 109 `.resx` files, some with binary-serialized content (image lists) | unknown | Build and load every form in a test; convert what the modern resource reader rejects |
| Client-side library download and `Assembly.LoadFrom` | 2 sites | Same resolver as WS2 |
| WPF scheduler hosted in WinForms; webcam capture via `avicap32`; embedded `WebBrowser` control | 3 features | Port as-is (all supported on Windows); test manually once |
| High-DPI behaviour changes between Framework and .NET | whole UI | Start in the same DPI mode as today (unaware); improving it is separate work |
| ClickOnce deployment of the Windows client | in real use (Q3) | Keep it possible: .NET 10 WinForms applications can be published with ClickOnce (`dotnet publish` with a ClickOnce profile, `dotnet-mage` for signing and updates). WS9 owns it |
| Custom form nodes in application assemblies, some wrapping third-party UI suites built for .NET Framework | outside this repository | Out of scope here (D16); keep the node base classes and interfaces source-compatible, and log every change to them in the migration notes |

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `ui-client-layering` | Separate design-time and WinForms code from `DAE.Client`; `DAE.Client`, `DAE.Client.Provider` and `D4Runner` become `net10.0`. | `build-core-sdk-projects` | medium |
| `ui-controls-port` | `DAE.Client.Controls` on `net10.0-windows`: SDK project, legacy-control replacements (then `WFDEV006` as error), resources; a test that constructs every control. | `ui-client-layering` | hard |
| `ui-frontend-client-port` | `Frontend.Client` and `Frontend.Client.WPF` on `net10.0-windows`; deserialize every repository form in a test. | `ui-controls-port` | medium |
| `ui-scriptaction-roslyn` | Roslyn-based `ScriptAction`; tests from `Sample.ScriptAction` plus VB. | `ui-frontend-client-port` | medium |
| `ui-windows-client-port` | `Frontend.Client.Windows` and its executable compile and start. | `ui-frontend-client-port` | hard |
| `ui-windows-client-runs` | Sample applications work end to end against a hosted .NET 10 server; fix what surfaces; record a manual checklist of every node type. | `ui-windows-client-port`, `protocol-corewcf-host` | hard |
| `ui-editor-and-docking` | Replace the text editor and docking binaries; remove Ascend. | `ui-controls-port` | hard |
| `ui-dataphoria-port` | Dataphoria compiles and starts; object tree, script editor and execution work. | `ui-editor-and-docking`, `ui-windows-client-port` | hard |
| `ui-dataphoria-designers` | Form designer, schema designer, debugger and analyzer verified and fixed. | `ui-dataphoria-port` | hard |
| `ui-config-utility` | Port or replace the tray configuration utility (see WS10). | `host-generic-host` | medium |

**Done when** gate G3 holds.

## WS7 Devices and libraries

**Objective.** The libraries a real deployment needs work on .NET 10 with supported drivers; the rest are removed or parked deliberately.

| Library | State | Action |
|---|---|---|
| SQLDevice, SimpleDevice, System.Platform, System.Integration, Security, General | Compile unchanged | Convert projects; tests |
| Connection | References SQL CE; holds 2,600 lines of uncompiled duplicates | K19; delete the duplicates |
| MSSQLDevice + `MSSQLConnection` (in Server) | `System.Data.SqlClient` | Move to `Microsoft.Data.SqlClient`. **Compatibility trap:** the new driver encrypts by default and validates the server certificate; a device definition that worked in 3.1 must still connect, so keep the old defaults unless the device's settings say otherwise, and document it. Check behaviour against the SQL Server version in production (Q12) |
| PGSQLDevice + PGSQLConnection (+ the PostgreSQL catalog store, which references the connection library) | Compiles against Npgsql 2.0.4 and Mono.Security, both checked-in binaries from about 2009 | **Port** (D15): current Npgsql from NuGet, Mono.Security dropped. Fifteen years of driver changes sit in between — type mapping (timestamps and time zones, numerics, booleans, large objects), parameter handling, connection-string keywords, error codes — so this is driven by the live tests, not by getting it to compile. The device has had little maintenance since 2017; expect to find its own defects too |
| OracleDevice and its connections | Default connection uses `System.Data.OracleClient` (gone) | Park under `legacy/libraries` (D15); a later self-contained ticket (managed ODP.NET) |
| ODBC, OLE DB connections | Available as packages (OLE DB is Windows-only) | Convert if it is a project-file change only; otherwise park |
| ADO (COM) connection, SAS device | COM interop | Park |
| System.Internet | `HttpWebRequest`, `FtpWebRequest`, `SmtpClient`, Newtonsoft.Json 9 | Works on .NET 10 as obsolete API; update Newtonsoft; modernizing HTTP is optional follow-up |
| Frontend (server) | Compiles with one name collision fixed; contains leftover `System.Web` page and handler classes | Delete the web leftovers; WS3 L5 covers derivation |
| MySQL, DB2, DB2400, DataDirect, ODP, Caché connections; MySQL, DB2, Caché, Linter, Fastore devices | Not built by any solution, or have no working driver | Remove (D12 keeps them in history) |
| FHIR*, CQF*, PHINVADS*, TestCQL, TestCache | Side projects on pre-1.0 HL7 packages | Park (D6). Before parking, move the general-purpose object-as-JSON nodes that the FHIR core library registers (`FHIRCore.ObjectAsJSONSelectorNode`, `…ReadAccessorNode`, `…WriteAccessorNode`, `FHIRCore.FHIRBaseDisplayString`) somewhere that stays in the build if they have no FHIR dependency, keeping the D4 class names; otherwise record them in the migration notes — an application is known to reference them |
| Samples | D4 and forms only, except Shipping | Keep; they are test corpus (WS3 L7). Samples that require SQL Server get a SimpleDevice variant where practical |

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `device-mssql-port` | `Microsoft.Data.SqlClient`, default-compatibility rules, golden and live tests. | `test-device-sql-golden` | medium |
| `device-connection-cleanup` | Delete uncompiled duplicates and the VS2003 project files; ODBC and OLE DB on packages. | `build-core-sdk-projects` | easy |
| `library-remove-dead-devices` | Delete the dead connections and devices listed above. | — | easy |
| `library-park-side-projects` | Move FHIR/CQF/PHINVADS, the top-level `Dataphoria/` tree and the Oracle, ADO and SAS libraries under `legacy/`, out of the build; handle the object-as-JSON nodes as described above. | — | medium |
| `device-pgsql-port` | PostgreSQL connection and device on current Npgsql: SDK projects, binaries removed, golden translation tests, live tests against a PostgreSQL server (environment-driven; in CI if the runner can supply one); document the minimum supported PostgreSQL version; port the PostgreSQL catalog store if it then needs only the store contract tests to pass. | `test-device-live` | hard |
| `device-sqlserver-version-matrix` | Establish and document the minimum supported SQL Server version and the connection settings needed for older servers (Q12). | `test-device-live` | medium |
| `library-frontend-web-leftovers` | Delete `System.Web` classes from the Frontend server library. | `test-frontend-derivation-golden` | easy |
| `library-upgrade-scripts` | Establish what runs `Upgrades/Upgrade.<version>.d4`, document it, and test a library upgrade from an older version. | `test-harness-inprocess` | medium |

## WS8 Documentation

**Objective.** All user, developer and reference documentation in Markdown under `docs/`, complete, with working links.

**Current state.** `Documentation/`: 75 AsciiDoc files, about 190,000 words — User's Guide (35k), Developer's Guide (142k), and a Reference that is mostly stubs — with 116 images (59 of them BMP, 30 MB), 649 anchors, 523 cross-references, about 750 literal blocks holding every code sample with no language tag, 62 tables, 33 footnotes, and 206 links to legacy `.html` pages that do not exist. `Legacy/Documentation/` is the DocBook 4.3 original, and the *only* source for: the system library reference (341 operator sections, plus 275 per-operator files), types and tables reference, device and connection library references, the Frontend and form-node (DIL) reference, keyboard shortcuts and several topic chapters. `Libraries/Documentation` contains a D4 operator that generates DocBook reference pages from a live catalog.

**Target tree.**

```
docs/
  README.md                 index
  architecture.md, modernization-plan.md
  users-guide/              from Documentation/UsersGuide
  developers-guide/         from Documentation/DevelopersGuide
  reference/
    d4-grammar.md, tags.md, error-codes.md, compiler-defines.md
    system-library/         from Legacy DocBook operator, type and table references
    libraries/              device, connection, Frontend, Security, System.* references
    frontend-nodes/         from the DIL reference
  admin/                    install, instances, services, rebuilding a catalog, security, troubleshooting
  migration-notes.md        running list of changes an application or extension can notice (D16)
  contributing/             build, test, writing docs, release checklist
  images/                   PNG and SVG only
```

**Design notes.** Convert with scripts kept under `tools/` until the sources are deleted: AsciiDoc → DocBook (Asciidoctor) → GitHub-flavoured Markdown (pandoc) is the high-fidelity route and handles the legacy DocBook with the same second step; a rewrite pass then maps anchors and cross-references to file-relative links, tags literal blocks as `d4`, `sql` or `xml`, converts BMP to PNG, and rewrites the 206 dead links to the converted reference pages. The source files declare `iso-8859-1`; check the actual bytes before converting. A link checker runs in CI. After conversion the generator in `Libraries/Documentation` gets a Markdown emitter so the system library reference can be regenerated from the catalog and compared with the hand-written text. Longer term, self-contained D4 samples in the docs are extracted and run as tests.

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `docs-convert-asciidoc` | Conversion tooling and the three books; images to PNG; anchors and cross-references resolved; link check clean within the converted set. | — | hard |
| `docs-convert-legacy-reference` | DocBook reference material into `docs/reference/`; repair the 206 links. | `docs-convert-asciidoc` | hard |
| `docs-remove-old-sources` | Delete `Documentation/`, `Legacy/Documentation/` and `book.json`; repoint the links in the root `README.md` to the converted pages. | `docs-convert-legacy-reference` | easy |
| `docs-admin-guide` | New: installation, instance configuration, running as a service, ports and firewall, backup, rebuilding a catalog from scripts, moving an application from 3.x. | `store-rebuild-workflow`, `installer-msi` | medium |
| `docs-contributing` | Build, test, layout, release checklist; fold `CompilerDefines.txt` and the error prefix list into `reference/`. | `build-core-sdk-projects` | easy |
| `docs-reference-generator` | Markdown emitter for the catalog documentation operator; regenerate and diff. | `docs-convert-legacy-reference`, `test-harness-inprocess` | medium |
| `docs-link-check-ci` | Link checker in CI. | `build-ci`, `docs-convert-asciidoc` | easy |
| `docs-migration-guide-custom-code` | Turn the accumulated migration notes into a guide for moving an application and recompiling custom C# extensions against 4.0. | `ui-windows-client-runs` | medium |

## WS9 Installer and release

**Objective.** A signed-off MSI that installs, upgrades and uninstalls cleanly, built by the normal build.

**Current state.** WiX 3.5 (a full copy of the toolset is vendored), hard-coded version, no `MajorUpgrade` (a new MSI installs beside the old one), a custom task that regenerates every component GUID on every build and puts many files in one component, a missing licence file, placeholder registry keys, an undefined working-directory reference in shortcuts, 32-bit Program Files, no service registration, features for two web applications that are being removed (K16).

**Target.**

- SDK-style WiX project fed from the runtime layout, with stable component identities (file-level harvesting), version from the build, `MajorUpgrade` using the existing upgrade code `B08DF609-D4F8-48CF-9620-A7F65D080EAB`, per-machine, x64.
- Features: Server (host executable, libraries, optional default instance registered as a Windows service), Dataphoria, Windows Client, Documentation.
- Self-contained publish (D10). Start-menu shortcuts for Dataphoria, the Windows client, and server administration.
- Service registration, firewall rules for the listener and instance ports, and Event Log source creation done by the installer; nothing requires elevation at first run.
- Instance data under `%ProgramData%\Alphora\Dataphor` is never removed on upgrade or uninstall.
- A 3.x installation is left alone and can remain installed side by side; the new instance starts with an empty catalog that the application's rebuild scripts populate.
- The Windows client can also be published for ClickOnce deployment, as existing sites deploy it today.
- A release checklist in `docs/contributing/` (L8 tests, version bump, change log, artifacts).

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `installer-publish-profiles` | `dotnet publish` profiles producing the install image from the layout. | `build-runtime-layout`, `host-generic-host` | medium |
| `installer-msi` | New WiX project; delete `Setup/` and `Utilities/Wix`. Depends on Q11. | `installer-publish-profiles` | hard |
| `installer-service-and-firewall` | Service, firewall and Event Log registration; default instance option. | `installer-msi` | medium |
| `installer-test` | L8 scripted install, upgrade and uninstall test. | `installer-service-and-firewall` | medium |
| `installer-clickonce-client` | ClickOnce publish profile and signing for the Windows client; documented update procedure. | `ui-windows-client-runs` | medium |
| `release-checklist-and-changelog` | Checklist, `CHANGELOG.md`, release workflow in CI. | `installer-msi`, `build-ci` | easy |

## WS10 Utilities and developer tooling

**Objective.** Administration and development do not require Dataphoria or hand-edited configuration files.

**Current state.** `D4Runner` (run a script), `DAEServer` (console host), `DAEService` (service with its own install switches), `DAEConfigUtil` (WinForms tray tool that edits instances and starts services), each with its own argument parsing — two of them carry a vendored 500-line parser.

**Target.** One command-line tool, `dataphor`, built on `System.CommandLine`, covering:

| Command | Purpose |
|---|---|
| `dataphor server run / install / uninstall / start / stop / list` | Host an instance; manage instances and their services |
| `dataphor d4 run <file>` / `dataphor d4 eval "<expr>"` / `dataphor d4 repl` | Run D4 against an instance or an embedded in-process engine; machine-readable output option |
| `dataphor catalog rebuild / export / verify` | Create a fresh instance and run an application's rebuild scripts against it; script the catalog; check that every stored object loads |
| `dataphor library list / register / validate / test` | Library operations; `test` runs a library's D4 test scripts with the L1 harness |
| `dataphor docs generate` | WS8 reference generation |

`D4Runner`, `DAEServer` and `DAEService` remain as thin shims with their old names and switches for existing scripts and service registrations. The tray utility becomes optional (Dataphoria already edits instances). Further ideas, each a backlog item rather than a commitment: a D4 language extension for VS Code (the grammar exists in three forms already: `.xshd`, the BBEdit module and the BNF), a D4 language server built on the real compiler, health and metrics endpoints on the host, structured logs.

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `tool-cli-skeleton` | `dataphor` CLI with `d4 run/eval` against the in-process engine and remote instances; replaces the vendored parsers. | `ui-client-layering` | medium |
| `tool-cli-server-commands` | Instance and service management commands. | `host-generic-host` | medium |
| `tool-cli-catalog-commands` | Rebuild, export, verify. | `store-rebuild-workflow`, `tool-cli-skeleton` | medium |
| `tool-cli-library-test` | `library validate` and `library test`. | `test-harness-inprocess`, `tool-cli-skeleton` | medium |
| `tool-d4-repl` | Interactive D4 prompt with history and table rendering. | `tool-cli-skeleton` | easy |
| `tool-vscode-d4-syntax` | Backlog: syntax highlighting extension. | — | medium |

## WS11 Security

**Objective.** No known-weak defaults in a system that holds medical data, without breaking existing catalogs.

Findings (static unless noted):

- **Password storage** (K13): user passwords and device-user passwords are AES-encrypted with a key and IV compiled into `DAE/Schema/Security.cs` and duplicated in `DAE.NativeCLI/SecurityUtility.cs`; a comment marks the keys as to be moved. Anyone with the binaries can decrypt every stored password.
- The built-in `Admin` user has an empty password by default.
- The wire protocol is unencrypted and unauthenticated at the transport level by default.
- Dependencies with known vulnerabilities (Newtonsoft.Json 9.0.1, Npgsql 2.0.4).
- `System.Platform` exposes process execution with alternate credentials as a D4 operator; it is governed by D4 rights like any operator — confirm the default grants.
- The catalog store builds SQL by string concatenation (inputs are engine-generated names and scripts with quote doubling; covered by `store-parameterized-statements`).

Target: user passwords stored as salted, iterated hashes, upgraded transparently at next successful login; device-user passwords (which must be recoverable) protected by a per-instance key held outside the catalog (Windows DPAPI); installer prompts for an administrator password for a new instance; transport security available and documented (WS5); package vulnerability audit in CI; a short threat model in `docs/admin/security.md`. Every item here touches a compatibility surface and needs its own design ticket — none of it is "fix inline".

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `security-password-storage-design` | Plan ticket: hash and key-protection design; owner sign-off. Catalog rebuild scripts create users and device users with passwords, some in the existing encrypted form, so the scripted forms are a compatibility surface. | `store-rebuild-workflow` | hard |
| `security-password-storage` | Implement. | `security-password-storage-design` | hard |
| `security-admin-default` | New instances require an administrator password; existing instances warned. | `host-instance-configuration` | medium |
| `security-dependency-audit` | `dotnet list package --vulnerable` in CI; update flagged packages. | `build-ci` | easy |
| `security-operator-rights-review` | Review default rights on file, process and network operators; document. | `test-harness-inprocess` | medium |
| `security-threat-model-doc` | `docs/admin/security.md`. | `protocol-transport-security` | medium |

## WS12 Repository hygiene

**Objective.** The tree contains what is built, tested or documented, and nothing else. The structural part of this — deleting dead trees and moving to the new layout — is in [Repository layout](#repository-layout) and WS1; what follows is the remainder.

| Item | Size | Action |
|---|---|---|
| `Utilities/Wix` (WiX 3.5) | 60 MB | Delete with the old `Setup/` when WS9 lands |
| `Legacy/Documentation` build tools, PDFs, CHMs, jars | most of 79 MB | Delete after WS8 |
| 46 `#if SILVERLIGHT` blocks | 26 files | Remove the dead branches once the build exists to prove nothing changed |
| Tracked third-party DLLs and executables | ~45 MB | Replace with packages as each consumer is converted (D11); `external/` ends empty |
| `Images/` source art (`.psd`, `.cdr`) | 11 MB | Keep the PNG/ICO actually referenced (`assets/`); delete the rest |
| `Dataphor/netsh_address_reservations.txt` | — | Delete (names a developer's machine account; describes an HTTP binding no code uses) |
| `.gitignore` | — | Done in Phase 0: no longer ignores `*.xml` |
| Line endings and encodings | — | Add `.gitattributes` *without* renormalizing existing files (declare binary types; pin `eol` only for new file types); one `.editorconfig` at the root |
| Dead `#define` switches and unused `using` directives | — | Opportunistic, in files otherwise being edited; never as a sweep |
| `README.adoc` | — | Done in Phase 0: replaced by `README.md` |

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `hygiene-silverlight-conditionals` | Remove `#if SILVERLIGHT` branches and the `AllowSilverlightClients` setting. | `test-harness-inprocess` | easy |
| `hygiene-large-binaries` | Netsh file, unreferenced art, anything else large that nothing references. | `repo-move-to-new-layout` | easy |
| `hygiene-gitattributes-editorconfig` | As described; verify `git status` stays clean. | `repo-move-to-new-layout` | easy |
| `hygiene-vendored-binaries` | Final sweep: `external/` is empty or each remaining file has a recorded reason. | `ui-editor-and-docking`, `installer-msi` | easy |

## WS13 Customer application compatibility

**Status: deferred by the owner (D16).** Dataphor is modernized first; the application follows, with minor adaptations expected. Until then the only obligation is to keep [migration-notes.md](migration-notes.md) current.

What is known already, from a read-only look at one deployed application (kept generic on purpose):

- It runs a 3.1 build from late 2019, so there is no version gap in Dataphor itself.
- It needs the libraries `System`, `Frontend`, `Security`, `System.Internet`, `System.Platform`, `Connection`, `SQLDevice`, `MSSQLDevice` and `SimpleDevice`, and references four class names from the FHIR core library (see WS7).
- About thirty D4 libraries, 350 scripts and 200 form documents, three quarters of them customizations (`.dfdx`); script actions in about half the forms.
- Custom assemblies exist only as binaries in the deployment; several wrap third-party .NET Framework UI and reporting suites, and one Dataphor reporting integration library is not in this repository. These are the hard part of the application's move.
- The Windows client is deployed with ClickOnce; a WebForms portal sits beside it.
- It has its own D4 unit-test libraries — a ready-made acceptance suite once the application is addressed.

When the work starts, the corpus is located by an environment variable (never committed), and the checks are, in order of cost: every library registers on a fresh instance through the application's own rebuild scripts; every stored form loads and every table and view derives each standard page type; the application's D4 unit tests pass; custom assemblies are rebuilt following the migration guide; a scripted walk through the main screens.

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `compat-corpus-harness` | L7 harness that takes a corpus path; registration, form loading and derivation checks; report format. Developed against repository samples. | `test-samples-corpus` | medium |
| `compat-application-run` | Backlog until the owner starts it: run against the real corpus; a ticket per finding. | `compat-corpus-harness`, `docs-migration-guide-custom-code` | medium |

## WS14 Research compiled catalog and precompilation

**Objective.** Decide, with measurements, whether compiling catalogs or plans into assemblies is worth building.

**What exists.** Less than remembered: execution is plan-node interpretation; the IL emission path was removed before or at the 2008 open-source import, leaving a session flag (`ShouldEmitIL`) that nothing reads and one commented-out method. There is nothing to "turn back on".

**Questions to answer.**

- *Startup and load cost.* How long do registration and first use take for a large catalog on .NET 10 with the SQLite store? (System catalog start is about one second today.) If lazy loading from the store is already fast, a compiled catalog buys little.
- *Catalog as an assembly.* Generate, per library version, an assembly whose metadata describes the library's objects, loaded instead of re-compiling stored D4 scripts. Costs: a second source of truth, a build step inside the server, an invalidation story for `alter` statements, and security data still needs a store.
- *Expression compilation.* Compile scalar expression subtrees (restriction predicates, `extend` and `redefine` column expressions, aggregates) to delegates with `System.Linq.Expressions` behind the existing `PlanNode` interface. This is incremental, measurable on the memory device, and independent of how the catalog is stored — the likeliest win.
- *Ahead-of-time plans.* Precompiling whole operators is only attractive if expression compilation shows a large gain.

| Work item | Scope | Prereq | Difficulty |
|---|---|---|---|
| `research-catalog-load-profile` | Measure catalog load and plan compile cost on realistic catalogs; report. | `test-perf-baseline` | medium |
| `research-expression-compilation-spike` | Prototype delegate compilation for a handful of scalar node types; benchmark; recommend. | `research-catalog-load-profile` | hard |

## Known defects

| # | Defect | Where | Status | Fix in |
|---|---|---|---|---|
| K1 | A static, non-thread-safe `D4TextEmitter` is shared by all catalog sessions; concurrent DDL interleaves the text of the object scripts written to the catalog store. Possible silent catalog corruption on any store. | `Dataphor/Server/Device.Catalog/ServerCatalogDeviceSession.cs`, field `_emitter` (~line 2615) | verified | WS2 `port-catalog-emitter-race` |
| K2 | `CopySection` computes the end offset from the start position instead of the line start when a section begins and ends on the same line, overshooting by the start column; `create operator` written on one indented line at the end of a script throws "Index and length must refer to a location within the string" (surfaced as an internal compiler error). | `Dataphor/DAE/Debug/SourceUtility.cs` | verified | WS2 |
| K3 | "Collection was modified; enumeration operation may not execute" from concurrent DDL, 1–4 times per 120 cycles before store writes were serialized. Site unknown. | engine or catalog device | verified, unlocated | WS2 |
| K4 | `SetThreadName` reflects on the private field `Thread.m_Name`, absent on modern .NET: null reference at server start in DEBUG builds. | `Dataphor/Base/ThreadUtility.cs` | verified | WS2 |
| K5 | `Delegate.BeginInvoke` is unsupported on modern .NET: D4 asynchronous execution operators and Dataphoria's script executor throw. | `Dataphor/DAE/Runtime.Instructions/SystemNodes.cs` (3 sites); `Dataphor/Dataphoria/TextEditor/ScriptExecutor.cs` | verified (engine), static (IDE) | WS2, WS6 |
| K6 | `RemoteServerObject.Dispose` calls remoting lifetime services, which throw on modern .NET — on every remote object disposal. | `Dataphor/Server/Server/RemoteServerObject.cs`, `RemoteServerConnection.cs`, `RemoteServer.cs`, `Streams/FileStreamProvider.cs` | static | WS1 `build-server-project` |
| K7 | `Thread.ResetAbort()` in catch blocks throws on modern .NET. | `Frontend.Client/Pipe.cs`, `Dataphoria/Program.cs`, `DAE.Service.ConfigurationUtility/Program.cs` | static | WS6 |
| K8 | `ScriptAction` compiles scripts with CodeDom, unsupported on modern .NET. | `Dataphor/Frontend.Client/ScriptAction.cs` | static | WS6 |
| K9 | `Process.Start` on a URL or document needs `UseShellExecute = true` on modern .NET (9 sites). | Dataphoria, Frontend.Client.Windows | static | WS6 |
| K10 | SQLite catalog store deadlocks under concurrent sessions. | `Dataphor/DAE.SQLite`, `Dataphor/DAE/Store` | verified, fix validated | WS4 |
| K11 | The distributed-transaction class is created by reflection under an assembly name that does not exist. | `Dataphor/DAE/Server/ServerProcess.cs` (~line 1268), `Dataphor/Server/Server/ServerDTCTransaction.cs` | static | WS2 |
| K12 | The service reads configuration section `instance`; the shipped `app.config` declares section `dataphor`. | `Dataphor/Server/Service/DataphorService.cs`, `Dataphor/DAE.Service/app.config` | static, unconfirmed | WS5 |
| K13 | Password encryption key and IV are compiled into the product, in two places. | `Dataphor/DAE/Schema/Security.cs`, `Dataphor/DAE.NativeCLI/SecurityUtility.cs` | static | WS11 |
| K14 | The coverage helper library cannot register without a SQL Server on `localhost`. | `Libraries/TestFramework/Documents`, `Libraries/TestFramework.Coverage.Base/*.d4l` | verified | WS3 |
| K15 | Six library projects write Release output where the server does not look. | CQF.Model, FHIR.API, FHIR.Server, PHINVADS.Core, PHINVADS.Device, TestFramework.Coverage.Base | static | WS1 |
| K16 | Installer: no upgrade logic, regenerated component GUIDs, missing licence file, placeholder registry keys, undefined shortcut directory, no service registration. | `Setup/` | static | WS9 |
| K17 | 206 documentation links point at pages that do not exist. | `Documentation/` | verified | WS8 |
| K18 | Tests contain a developer's drive paths and server names and depend on machine-wide state. | `Tests/DAE.Server.Tests/SQLStoreTest.cs`, `Tests/DAE.Client.Tests` | static | WS3 |
| K19 | The base `Connection` library has a hard reference to SQL CE. | `Libraries/Connection/Source/Register.cs` | verified | WS4 `store-remove-sqlce-references` |
| K20 | 14 coverage tests are ignored (one names a script that does not exist) and 45 scripts are not wired to any test. | `Tests/DAE.Server.Tests/CoverageTest.cs` | verified | WS3 |
| K21 | A 34 MB catalog database, server logs and a file naming a developer's machine account are committed. | `Dataphor/Dataphoria/Default Server Configurations`, `Dataphor/netsh_address_reservations.txt` | verified | WS12 |
| K23 | The vendored command-line parser uses `field` as a variable name inside a property accessor; `field` is a keyword there from C# 14, so the file does not compile with the .NET 10 default language version. | `Dataphor/DAEServer/CommandLineArguments.cs` (~line 1384); check the copy in `Dataphor/D4Runner` | verified | WS10 `tool-cli-skeleton` (rename earlier if it blocks WS1) |
| K24 | Legacy WinForms menu, toolbar and status-bar controls compile on .NET 10 but throw `PlatformNotSupportedException` when constructed (~270 references). | `DAE.Client.Controls`, `Frontend.Client.Windows`, `Dataphoria`, `DAE.Service.ConfigurationUtility` | verified | WS6 |
| K22 | `PathUtility` relies on `AppDomain.RelativeSearchPath`, always null on modern .NET. | `Dataphor/Base.Windows/PathUtility.cs` | static | WS2 |

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| CoreWCF cannot host the contracts as they are (binding, singleton instancing, 2 GB messages, asynchronous patterns) | Medium | Proof first (`protocol-corewcf-proof`); the fallback is a designed change to the transport, decided by the owner |
| The WinForms port hides a long tail behind the first wave of errors (designers, custom painting, DPI, resources) | High | Port bottom-up with a test that instantiates every node type; budget WS6 as the largest workstream; keep the manual checklist |
| Custom application extensions depend on .NET Framework-only third-party components | High | Out of scope here (D16); keep extension base classes source-compatible and the migration notes current so the later estimate is honest |
| An old SQL Server refuses the current driver (TLS versions, protocol levels) | Medium | Q12; `device-sqlserver-version-matrix` |
| Subtle runtime differences (culture and string comparison, floating-point formatting, `DateTime` parsing, sort stability) change D4 results with no reference runtime to compare against (D2) | Medium | NLS globalization selected explicitly; script assertions treated as the oracle; widen script coverage where it is thin (`test-coverage-gap-analysis`); one manual comparison against an installed 3.1 during the pilot |
| The reorganization breaks something silently (a relative path in a script, an embedded resource name derived from a folder) | Medium | Rename-only commits; spike results identical before and after; resource manifest names pinned in the project files |
| Lesser agents "fix" compatibility surfaces or reformat files | Medium | `tickets/rules/10-dataphor.md`; contract parity and golden tests fail loudly |
| Latent concurrency defects (K1, K3) are more widespread than found | Medium | Stress tests in L2; static review for shared statics |
| Scope creep into redesign | High | Non-goals; D13; research is time-boxed and last |

## Guidance for cutting tickets

- One work item → one ticket in `plan/` (if it says "design", "evaluate" or has an open choice) or `implement/` (if the scope here is already concrete). Items marked *hard* should go to `plan/` first and may split.
- Copy the prerequisites into `prereq:` using the slugs in the tables. Number Phase 1 tickets lowest.
- Anything depending on an open question that has no answer yet goes to `blocked/` stating the question number and the default from the table as the proposal.
- The order at the start is fixed: `repo-delete-dead-trees`, `library-remove-dead-devices` and `library-park-side-projects`, then `repo-move-to-new-layout`, then `build-core-sdk-projects`, `build-server-project`, `build-runtime-layout`, `test-harness-inprocess`. Nothing else can verify its work until the last of these lands, and nothing else should be in flight while paths are moving.
- WS8 items can run at any time (they write only under `docs/` and `tools/`) and, like most WS12 items, are good for lower-capability agents; WS2, WS4, WS5 and WS6 items marked *hard* are not.
- Each known defect is fixed inside the named work item with its reproduction test; do not file K-items as separate tickets unless the work item is split.
- Every ticket that changes something an application, a deployment script or a custom extension could notice appends an entry to `docs/migration-notes.md` in the same commit.

## Appendix A Spike results

Reproduce with `python tools/net-spike/run.py <work-dir> --smoke --coverage`. Run twice on Windows 11: against `net8.0` (SDK 8.0.411, runtime 8.0.17) and against `net10.0` (SDK 10.0.404, runtime 10.0.15). Runtime results — engine start, concurrency, and the 62/14 coverage split with the same 14 scripts failing for the same reasons — were identical. Compile results differ only where noted.

**Source patches the spike applies** (each is required by the real port):

| File | Change | Kind |
|---|---|---|
| `Dataphor/DAE/Runtime.Instructions/SecurityNodes.cs` (and its `Server` twin) | Unused `using System.Runtime.Remoting.Messaging` (handled by an empty stub namespace in the spike) | dead code |
| `Dataphor/Common/WCF/ServiceClient.cs`, `Dataphor/Client/Client/ClientServer.cs` | `ChannelFactory<T>(string endpointConfigurationName)` does not exist in the client packages | API gap |
| `Dataphor/Base/ThreadUtility.cs` | K4 | runtime bug |
| `Dataphor/DAE.SQLite/SQLiteConnection.cs` | `SQLiteException.ErrorCode` → `ResultCode`; begin transactions immediate | API drift; K10 |
| `Dataphor/Server/Device.Catalog/ServerCatalogDeviceSession.cs` | K1: emitter per call | defect |
| `Libraries/Frontend/Source/Device/FrontendDevice.cs` | `IStreamProvider` now ambiguous with `System.Xml.IStreamProvider` | name collision |
| `Libraries/Connection/Source/Register.cs` | K19 | dependency |

**Compile results** (distinct errors after the patches):

| Projects | Errors |
|---|---|
| Base, DAE (Engine), Language.SQL, Common, DAE.NativeCLI, Base.Windows, Client, DAE.Client, DAE.Client.Provider, DAEServer, DAE.SQLite, Connection, SQLDevice, MSSQLDevice, SimpleDevice, Frontend server, System.Platform, System.Internet, System.Integration, TestFramework.Coverage.Base | 0 |
| Server | 0 with stubs. Without them: 28 first-wave errors in `Service/*Host.cs`, `Service/*Service.cs` (WCF hosting), `Server/RemoteServer*.cs` and `Streams/FileStreamProvider.cs` (remoting), `Server/ServerDTCTransaction.cs` (COM+) |
| DAE.Service | 7 — `System.Configuration.Install` |
| DAE.Client.Controls, Frontend.Client, Frontend.Client.WPF, Frontend.Client.Windows | `net10.0-windows`: 0 (third-party binaries referenced as-is; legacy controls resolve to throwing shells, K24). `net8.0-windows`: stops at the removed WinForms types |
| Dataphoria | `net10.0-windows`: 1 — `BaseTree.cs` overrides `TreeNode.ContextMenu`, which no longer exists |
| DAEServer | 0 with C# 12; 4 with C# 14 (K23) |
| D4Runner, test projects | Blocked only by `DAE.Client` requiring a Windows target (layering, WS6) and the NUnit package reference |

**Runtime results.** Server start about 1 s (2.7 s cold); 1,080 operators and 23 scalar types registered; expressions, table DDL and DML, views and operators work. Concurrency results are in WS4. Coverage scripts: 62 pass; 12 fail because `TestFramework.Coverage.Base` could not register (K14) — Lexer, DML, Cursors, TransitionConstraintWithNamedRowVariables, Explode, TransitionConstraints, SystemCatalog, NodeOptimization, SemiTables, ScanTable, PlanCache, OperatorText; SecurityLibrary fails for the same reason at a later point; DebugLibrary fails on K5.

## Appendix B Inventory

**Size.** 4,452 tracked files; 1,237 C# files, about 400,000 lines; 387 D4 scripts; 88 project files; 1,551 commits from March 2008 to October 2019.

| Project | Lines of C# |
|---|---|
| Dataphor/DAE | 164,000 |
| Dataphor/Dataphoria | 28,600 |
| Dataphor/DAE.Client.Controls | 23,200 |
| Dataphor/Frontend.Client.Windows | 21,200 |
| Dataphor/Server | 18,900 |
| Dataphor/DAE.Client | 15,400 |
| Dataphor/Frontend.Client | 13,100 |
| Libraries/SQLDevice | 10,300 |
| Libraries/Frontend | 9,400 |
| Dataphor/Client | 7,700 |
| Dataphor/Base | 7,300 |

**Non-default compiler constants to preserve.** Engine: `USENATIVE;NATIVEROW;ACCUMULATOR;USEOBJECTVALIDATE;NILPROPOGATION;USEVISIT` (Debug; Release omits `ACCUMULATOR` and `USEVISIT` and adds `DISABLE_PERFORMANCE_COUNTERS`, which no code reads — decide deliberately which set ships, and test with it). SQLDevice: `USEVISIT`. Frontend server: `USEINCLUDETAG`. Dataphoria: `TRACEFOCUS`. `SIGNASSEMBLIES` (Release) becomes unconditional signing (D7). File-level `#define`s are catalogued in `Dataphor/CompilerDefines.txt`.

**Wire contracts.** `IDataphorService` 88 operations, `INativeCLIService` 11, `IListenerService` 3; 55 data-contract types, mostly in `Dataphor/DAE/Contracts`.

**Tests today.** 126 NUnit tests (112 active): 90 script runners, 12 cache tests, the rest cursor, store and client tests. 135 coverage scripts, 23,548 lines.

**Checked-in binaries and bulk.** 133 DLLs (31 MB), 34 executables (15 MB), 18 WiX SDK `.lib` files (39 MB), one 34 MB SQL CE database, 60 MB of BMP images. By directory: `Legacy/` 79 MB, `Utilities/` 63 MB, `Dataphor/` 61 MB, `Documentation/` 31 MB, `Libraries/` 18 MB, `Images/` 11 MB.

**Local toolchain.** .NET SDK 10.0.404 (installed during the survey with `winget install Microsoft.DotNet.SDK.10`; pinned by the root `global.json`) and 8.0.411; no Visual Studio, MSBuild or .NET Framework targeting packs; Node 24 (for tess); Python 3.14.
