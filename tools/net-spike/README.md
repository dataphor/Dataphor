# net-spike — does Dataphor run on modern .NET?

A disposable harness that answers that question with evidence, without changing any source in the repository. It is the evidence behind [docs/modernization-plan.md](../../docs/modernization-plan.md). Since `test-harness-inprocess` the D4 coverage scripts run under `dotnet test` (`tests/DAE.Server.Tests`, see AGENTS.md "Build and test"); the spike keeps the compile check of the projects still in legacy format, the smoke and concurrency run, and `EngineCheck`. **Delete this folder when every project has an SDK-style build.**

## Run it

Needs Python 3 and the .NET 10 SDK (the repository's `global.json` pins it). The work directory must be outside the repository; use a fresh one for a result you will report.

```
python tools/net-spike/run.py C:\temp\dataphor-spike --smoke
python tools/net-spike/run.py C:\temp\dataphor-spike --only DAE.Client,DAE.Diagnostics   # just compile some projects
dotnet run --project tools/net-spike/EngineCheck -c Debug                                    # resource names + bare engine
```

`--tfm` sets the wrappers' target framework (default `net10.0`). The converted projects target `net10.0` only, so a lower framework fails restore with `NU1201` for every wrapper that references one; plan decision D1 settles on `net10.0`.

## What it does

1. `projects.txt` lists 33 projects. Those already converted to SDK-style project files (Base, Base.Windows, DAE, Server, Server.Hosting, DAE.SQLite, Common, NativeCLI, Client, DAE.Host, DAEServer, DAE.Service, DAE.Connection, SQLDevice, MSSQLDevice, SimpleDevice, Frontend.Server, System.Platform, System.Internet, System.Integration, DAE.Diagnostics, Common.Tests, DAE.Server.Tests) are **used in place**: they build from their real project files with the repository's `Directory.Build.props`, into the repository's git-ignored `artifacts/` folder. That folder, including the runtime layout under `artifacts/layout/`, is the only thing the spike writes inside the repository.
2. `convert.py` wraps each project still in legacy format in an SDK-style project under `<work>/out/<Name>/`. The wrapper compiles exactly the `Compile` and `EmbeddedResource` items the legacy csproj lists (by absolute path), maps framework references to NuGet packages, and keeps the Debug `DefineConstants`. It references converted projects by their real path and takes package versions from the repository's `Directory.Packages.props` where that file lists the package, so it never pins a lower version than they use (that fails restore with `NU1605`).
3. `run.py` can write patched copies of source files to `<work>/out/_overrides/` (the `PATCHES` table) for the wrappers to compile instead of the originals. The table is empty since `build-server-project`: each patch either landed in the repository with the conversion of the project that compiles its file, or was dropped and left to the ticket that owns the change (the shared-emitter fix, K1, which landed in `port-catalog-emitter-race`; "begin immediate" store transactions, K10, which landed in `store-sqlite-wal-busy`). A patch to a file that a converted project compiles would be ignored, so `run.py` stops with a message if `PATCHES` names one. The server stubs (`stubs/Server.cs`) went with the same ticket: the converted server leaves its WCF host classes out of compilation instead.
4. `summ.py` reduces each build log to distinct errors by code.
5. `--smoke` builds `Smoke/`: starts a `Server` in-process on a SQLite catalog store in a temp instance directory, evaluates D4 expressions, runs DDL, then hammers it from 8 threads with create/query/drop cycles.
`Smoke` is built in the work directory, so it does not inherit the repository's props and runs without NLS globalization, as the baseline was measured. Its project template (`Smoke.csproj.txt`) references `..\<Name>\<Name>.csproj`, the wrappers' layout; `run.py` replaces each such reference to a converted project with that project's real path.

`EngineCheck/` is a separate check of the converted projects alone: it compares every converted assembly's manifest resource names with the legacy names, and runs D4 on a bare in-process engine (the client-side catalog, which does not register the classes in the `Server` assembly, so its counts — 1,060 operators, 23 scalar types — differ from the full server's).

## Results at the time of writing (.NET 10.0.15, Windows 11)

| Step | Result |
|---|---|
| Base, Base.Windows, DAE (the engine, 164k lines), Server, Server.Hosting, SQLite store, Common, NativeCLI, Client, Connection, SQLDevice, MSSQLDevice, SimpleDevice, Frontend server, System.Platform/Internet/Integration | converted; built in place with 0 errors. Since `protocol-corewcf-host` every WCF file is compiled: the three service classes in Server, the two hosts in Server.Hosting |
| Diagnostics (TestFramework.Coverage.Base) | converted (`build-runtime-layout`); built in place with 0 errors. Before that, its wrapper also had 0 errors |
| DAE.Client, DAE.Client.Provider | 0 errors. Between `build-server-project` and `protocol-corewcf-host` each had 1: `DataphorServiceHost`, used by `DAE.Client`'s in-process connection, was in no compiled assembly |
| DAE.Client.Controls, Frontend.Client, Frontend.Client.WPF, Frontend.Client.Windows | **0 errors on .NET 10** (with the checked-in third-party binaries referenced); between `build-server-project` and `protocol-corewcf-host`, the three that compile `DAE.Client` stopped at the same `DataphorServiceHost` error. On .NET 8 they stopped at the removed WinForms types `ContextMenu`, `MenuItem`, `ToolBar`, `StatusBar`. **.NET 10 brought those types back only as shells that throw `PlatformNotSupportedException` when constructed** (obsolete, diagnostic `WFDEV006`) — compiling is not working |
| Dataphoria | 1 error on .NET 10 (`TreeNode.ContextMenu` no longer exists to override) |
| DAE.Host, DAEServer, DAE.Service | converted; built in place with 0 errors. `host-service-registration` made DAE.Host, the assembly holding `ServiceUtility` and its string table; `host-generic-host` put the host program there and reduced both executables to a one-line `Main` over it. Before that, `DAE.Service` had 7 errors (the missing `System.Configuration.Install`, until its registration code moved to DAE.Host) and `DAEServer` had 4 with C# 14, where `field` became a keyword inside property accessors - in a vendored `CommandLineArguments.cs` that nothing in the project called, so the conversion deleted it (K23; the live copy in `D4Runner` stays) |
| DAE.Service.ConfigurationUtility | 1 error on .NET 10 (`NotifyIcon.ContextMenu`, one of the removed WinForms menu types). First measured by `host-service-registration`, which repointed its project reference from the service executable to DAE.Host; its own port is WS6 |
| Common.Tests | converted (`test-common-tests-sdk`); built in place with 0 errors |
| DAE.Server.Tests | converted (`test-server-fixture`); built in place with 0 errors. Since `test-harness-inprocess` it runs the D4 coverage scripts, replacing the spike's `--coverage` mode (whose last result, 63 pass and 14 fail, is in plan Appendix A) |
| D4Runner | blocked by `DAE.Client` requiring a Windows target (`NU1201`) |
| Smoke | engine starts in about 1 s, 1,080 operators and 23 scalar types registered, the `System` library and its catalog version row report `3.1.0.0`, D4 DDL/DML/views/operators work |
| Concurrency (8 threads × 15 DDL cycles) | 0 errors in 1.8–1.9 s (two runs) since `store-sqlite-wal-busy`, on the store's default connection options: the store applies WAL and its busy timeout itself and begins every store transaction immediate, so the spike no longer passes them. History: 0 errors in 1.7 s with the spike's patches (WAL + immediate transactions + the emitter fix); 39–47 errors in 60–80 s without them; from `build-server-project` until `store-sqlite-wal-busy`, with WAL only, 10–24 errors in 7–27 s over two runs (mostly "database is locked", plus 2–3 garbled object scripts, K1, fixed in `port-catalog-emitter-race`). A rare "Collection was modified" (K3, the operator resolution cache) is fixed in the repo |
| Resolution cache (8 threads × 1 s of add/clear) | 0 errors. Reproduces K3 directly: before the fix, about 1.4 million "Collection was modified" errors in 5 s |
| EngineCheck (Debug and Release) | `RESULT: pass`: legacy resource names in all seventeen converted assemblies, 1,060 operators and 23 scalar types on the bare engine |
