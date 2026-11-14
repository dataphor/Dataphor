# net-spike — does Dataphor run on modern .NET?

A disposable harness that answers that question with evidence, without changing any source in the repository. It is the evidence behind [docs/modernization-plan.md](../../docs/modernization-plan.md), and until `test-harness-inprocess` replaces it, it is how a ticket shows that a conversion changed nothing. **Delete this folder when the SDK-style build of every project and the in-process test harness exist.**

## Run it

Needs Python 3 and the .NET 10 SDK (the repository's `global.json` pins it). The work directory must be outside the repository; use a fresh one for a result you will report.

```
python tools/net-spike/run.py C:\temp\dataphor-spike --smoke --coverage
python tools/net-spike/run.py C:\temp\dataphor-spike --only Server,DAE.SQLite   # just compile some projects
dotnet run --project tools/net-spike/EngineCheck -c Debug                         # resource names + bare engine
```

`--tfm` sets the wrappers' target framework (default `net10.0`). The converted projects target `net10.0` only, so a lower framework fails restore with `NU1201` for every wrapper that references one; plan decision D1 settles on `net10.0`.

## What it does

1. `projects.txt` lists 30 projects. Those already converted to SDK-style project files (Base, Base.Windows, DAE, Common, NativeCLI, Client, SQLDevice, System.Platform, System.Internet, System.Integration) are **used in place**: they build from their real project files with the repository's `Directory.Build.props`, into the repository's git-ignored `artifacts/` folder. That is the only thing the spike writes inside the repository.
2. `convert.py` wraps each project still in legacy format in an SDK-style project under `<work>/out/<Name>/`. The wrapper compiles exactly the `Compile` and `EmbeddedResource` items the legacy csproj lists (by absolute path), maps framework references to NuGet packages, and keeps the Debug `DefineConstants`. It references converted projects by their real path and takes package versions from the repository's `Directory.Packages.props` where that file lists the package, so it never pins a lower version than they use (that fails restore with `NU1605`).
3. `run.py` writes patched copies of four source files to `<work>/out/_overrides/` (the `PATCHES` table — each one is a change the real port has to make) and the wrappers compile those instead of the originals. A patch to a file that a converted project compiles would be ignored, so `run.py` stops with a message if `PATCHES` or `STUBS` names one: land the change in the repository with the conversion and remove the entry.
4. `stubs/Server.cs` supplies empty stand-ins for API that no longer exists (.NET Remoting lifetime services, WCF `ServiceHost`, COM+ `ServicedComponent`) so the compiler gets past the missing types and reports what else is wrong. Code behind a stub compiles but **must not be taken to work**.
5. `summ.py` reduces each build log to distinct errors by code.
6. `--smoke` builds `Smoke/`: starts a `Server` in-process on a SQLite catalog store in a temp instance directory, evaluates D4 expressions, runs DDL, then hammers it from 8 threads with create/query/drop cycles.
7. `--coverage` builds `Cov/`: copies the needed D4 libraries to a scratch directory together with their freshly built assemblies (a converted project's assembly is located by asking MSBuild for its Debug `TargetPath`), registers `Frontend`, and runs every script that `tests/DAE.Server.Tests/CoverageTest.cs` runs.

`Smoke` and `Cov` are built in the work directory, so they do not inherit the repository's props and run without NLS globalization, as the baseline was measured.

`EngineCheck/` is a separate check of the converted projects alone: it compares every converted assembly's manifest resource names with the legacy names, and runs D4 on a bare in-process engine (the client-side catalog without the `Server` assembly, so its counts — 1,060 operators, 23 scalar types — differ from the full server's).

## Results at the time of writing (.NET 10.0.15, Windows 11)

| Step | Result |
|---|---|
| Base, Base.Windows, DAE (the engine, 164k lines), Common, NativeCLI, Client, SQLDevice, System.Platform/Internet/Integration | converted; built in place with 0 errors |
| DAE.Client, DAE.Client.Provider, SQLite store | compile with 0 errors (SQLite store: one patched file, `ResultCode` and "begin immediate") |
| Server | 0 errors **with stubs**; real work is 4 WCF host files, remoting leases in 3 files, one COM+ class |
| MSSQLDevice, SimpleDevice, Connection, Frontend server, Diagnostics | 0 errors (two one-line patches) |
| DAE.Client.Controls, Frontend.Client, Frontend.Client.WPF, Frontend.Client.Windows | **0 errors on .NET 10** (with the checked-in third-party binaries referenced). On .NET 8 they stopped at the removed WinForms types `ContextMenu`, `MenuItem`, `ToolBar`, `StatusBar`. **.NET 10 brought those types back only as shells that throw `PlatformNotSupportedException` when constructed** (obsolete, diagnostic `WFDEV006`) — compiling is not working |
| Dataphoria | 1 error on .NET 10 (`TreeNode.ContextMenu` no longer exists to override) |
| DAEServer | 0 errors with C# 12; 4 with C# 14, where `field` became a keyword inside property accessors (vendored `CommandLineArguments.cs`) |
| DAE.Service | needs `System.Configuration.Install` replacement |
| D4Runner, test projects | blocked by `DAE.Client` requiring a Windows target and by the missing NUnit package reference |
| Smoke | engine starts in about 1 s, 1,080 operators and 23 scalar types registered, D4 DDL/DML/views/operators work |
| Concurrency (8 threads × 15 DDL cycles) | 0 errors in 1.7 s with WAL + immediate transactions + the emitter fix; 39–47 errors in 60–80 s without them. A rare "Collection was modified" (K3, the operator resolution cache) is fixed in the repo |
| Resolution cache (8 threads × 1 s of add/clear) | 0 errors. Reproduces K3 directly: before the fix, about 1.4 million "Collection was modified" errors in 5 s |
| Coverage scripts | 62 pass, 14 fail (12 need the helper library that requires a live SQL Server; 1 uses `Delegate.BeginInvoke`; 1 needs that same helper library). Unchanged since the engine is built in place with the constants 3.1 shipped |
| EngineCheck (Debug) | `RESULT: pass`: legacy resource names in all ten converted assemblies, 1,060 operators and 23 scalar types on the bare engine |
