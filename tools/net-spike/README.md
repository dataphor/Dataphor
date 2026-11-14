# net-spike — does Dataphor run on modern .NET?

A disposable harness that answers that question with evidence, without changing any source in the repository. It is the evidence behind [docs/modernization-plan.md](../../docs/modernization-plan.md), and until `test-harness-inprocess` replaces it, it is how a ticket shows that a conversion changed nothing. **Delete this folder when the SDK-style build of every project and the in-process test harness exist.**

## Run it

Needs Python 3 and the .NET 10 SDK (the repository's `global.json` pins it). The work directory must be outside the repository; use a fresh one for a result you will report.

```
python tools/net-spike/run.py C:\temp\dataphor-spike --smoke --coverage
python tools/net-spike/run.py C:\temp\dataphor-spike --only DAE.Client,DAE.Diagnostics   # just compile some projects
dotnet run --project tools/net-spike/EngineCheck -c Debug                                    # resource names + bare engine
```

`--tfm` sets the wrappers' target framework (default `net10.0`). The converted projects target `net10.0` only, so a lower framework fails restore with `NU1201` for every wrapper that references one; plan decision D1 settles on `net10.0`.

## What it does

1. `projects.txt` lists 30 projects. Those already converted to SDK-style project files (Base, Base.Windows, DAE, Server, DAE.SQLite, Common, NativeCLI, Client, DAE.Connection, SQLDevice, MSSQLDevice, SimpleDevice, Frontend.Server, System.Platform, System.Internet, System.Integration, DAE.Diagnostics) are **used in place**: they build from their real project files with the repository's `Directory.Build.props`, into the repository's git-ignored `artifacts/` folder. That folder, including the runtime layout under `artifacts/layout/`, is the only thing the spike writes inside the repository.
2. `convert.py` wraps each project still in legacy format in an SDK-style project under `<work>/out/<Name>/`. The wrapper compiles exactly the `Compile` and `EmbeddedResource` items the legacy csproj lists (by absolute path), maps framework references to NuGet packages, and keeps the Debug `DefineConstants`. It references converted projects by their real path and takes package versions from the repository's `Directory.Packages.props` where that file lists the package, so it never pins a lower version than they use (that fails restore with `NU1605`).
3. `run.py` can write patched copies of source files to `<work>/out/_overrides/` (the `PATCHES` table) for the wrappers to compile instead of the originals. The table is empty since `build-server-project`: each patch either landed in the repository with the conversion of the project that compiles its file, or was dropped and left to the ticket that owns the change (the shared-emitter fix, K1, to `port-catalog-emitter-race`; "begin immediate" store transactions, K10, to `store-sqlite-concurrency`). A patch to a file that a converted project compiles would be ignored, so `run.py` stops with a message if `PATCHES` names one. The server stubs (`stubs/Server.cs`) went with the same ticket: the converted server leaves its WCF host classes out of compilation instead.
4. `summ.py` reduces each build log to distinct errors by code.
5. `--smoke` builds `Smoke/`: starts a `Server` in-process on a SQLite catalog store in a temp instance directory, evaluates D4 expressions, runs DDL, then hammers it from 8 threads with create/query/drop cycles.
6. `--coverage` builds the runtime layout (`tools/layout/Layout.csproj`, Debug), copies the library folders the scripts need from its `Libraries/` folder (located by asking MSBuild for `DataphorLayoutDir`) to `<work>/run/covlibs/`, then builds `Cov/`, which registers `Frontend` and `TestFramework.Coverage.Base`, runs every script that `tests/DAE.Server.Tests/CoverageTest.cs` runs, and after them registers `Connection` and `System.Internet`, whose folders hold their private package dependencies (OLE DB; Newtonsoft.Json).

`Smoke` and `Cov` are built in the work directory, so they do not inherit the repository's props and run without NLS globalization, as the baseline was measured. Their project templates (`*.csproj.txt`) reference `..\<Name>\<Name>.csproj`, the wrappers' layout; `run.py` replaces each such reference to a converted project with that project's real path.

`EngineCheck/` is a separate check of the converted projects alone: it compares every converted assembly's manifest resource names with the legacy names, and runs D4 on a bare in-process engine (the client-side catalog, which does not register the classes in the `Server` assembly, so its counts — 1,060 operators, 23 scalar types — differ from the full server's).

## Results at the time of writing (.NET 10.0.15, Windows 11)

| Step | Result |
|---|---|
| Base, Base.Windows, DAE (the engine, 164k lines), Server, SQLite store, Common, NativeCLI, Client, Connection, SQLDevice, MSSQLDevice, SimpleDevice, Frontend server, System.Platform/Internet/Integration | converted; built in place with 0 errors. The server leaves out its six WCF service and host files until CoreWCF hosting (`protocol-corewcf-host`) |
| Diagnostics (TestFramework.Coverage.Base) | converted (`build-runtime-layout`); built in place with 0 errors. Before that, its wrapper also had 0 errors |
| DAE.Client, DAE.Client.Provider | 1 error since `build-server-project`: `DataphorServiceHost` (used by `DAE.Client`'s in-process connection) is no longer in the server assembly. Before that, 0 errors |
| DAE.Client.Controls, Frontend.Client, Frontend.Client.WPF, Frontend.Client.Windows | Since `build-server-project`, the wrappers that compile `DAE.Client` stop at the same `DataphorServiceHost` error (Frontend.Client.WPF, which does not, still has 0). Before that: **0 errors on .NET 10** (with the checked-in third-party binaries referenced). On .NET 8 they stopped at the removed WinForms types `ContextMenu`, `MenuItem`, `ToolBar`, `StatusBar`. **.NET 10 brought those types back only as shells that throw `PlatformNotSupportedException` when constructed** (obsolete, diagnostic `WFDEV006`) — compiling is not working |
| Dataphoria | 1 error on .NET 10 (`TreeNode.ContextMenu` no longer exists to override); since `build-server-project` it stops first at the `DataphorServiceHost` error in `DAE.Client` |
| DAEServer | 0 errors with C# 12; 4 with C# 14, where `field` became a keyword inside property accessors (vendored `CommandLineArguments.cs`); plus `DataphorServiceHost` since `build-server-project` |
| DAE.Service | needs `System.Configuration.Install` replacement; plus `DataphorServiceHost` since `build-server-project` |
| D4Runner, test projects | blocked by `DAE.Client` requiring a Windows target and by the missing NUnit package reference |
| Smoke | engine starts in about 1 s, 1,080 operators and 23 scalar types registered, the `System` library and its catalog version row report `3.1.0.0`, D4 DDL/DML/views/operators work |
| Concurrency (8 threads × 15 DDL cycles) | 0 errors in 1.7 s with WAL + immediate transactions + the emitter fix; 39–47 errors in 60–80 s without them. Since `build-server-project` the spike runs with WAL only: 10–24 errors in 7–27 s over two runs (mostly "database is locked", plus 2–3 garbled object scripts), until K1 and K10 are fixed. A rare "Collection was modified" (K3, the operator resolution cache) is fixed in the repo |
| Resolution cache (8 threads × 1 s of add/clear) | 0 errors. Reproduces K3 directly: before the fix, about 1.4 million "Collection was modified" errors in 5 s |
| Coverage scripts | 63 pass, 14 fail (12 need the helper library that requires a live SQL Server; 1 uses `Delegate.BeginInvoke`; 1 needs that same helper library). The 63rd pass is `DistributedTransactions`, added by `server-remove-remoting-and-dtc`. Unchanged since the engine is built in place with the constants 3.1 shipped, since the K1 and "begin immediate" patches were dropped, and since the libraries come from the runtime layout (`build-runtime-layout`). After the scripts, `REGISTERED Connection` and `REGISTERED System.Internet` |
| EngineCheck (Debug and Release) | `RESULT: pass`: legacy resource names in all seventeen converted assemblies, 1,060 operators and 23 scalar types on the bare engine |
