# net-spike — does Dataphor run on modern .NET?

A disposable harness that answers that question with evidence, without touching the repository. It is the evidence behind [docs/modernization-plan.md](../../docs/modernization-plan.md) and the starting point for the ticket that creates the real SDK-style build. **Delete this folder when that build and its test harness exist.**

## Run it

Needs Python 3 and the .NET 10 SDK (the repository's `global.json` pins it). The work directory must be outside the repository.

```
python tools/net-spike/run.py C:\temp\dataphor-spike --smoke --coverage
python tools/net-spike/run.py C:\temp\dataphor-spike --only Base,Engine        # just compile some projects
python tools/net-spike/run.py C:\temp\dataphor-spike --tfm net8.0 --smoke      # other target framework
```

## What it does

1. `convert.py` wraps each legacy project in `projects.txt` in an SDK-style project under `<work>/out/<Name>/`. The wrapper compiles exactly the `Compile` and `EmbeddedResource` items the legacy csproj lists (by absolute path), maps framework references to NuGet packages, and keeps the Debug `DefineConstants`.
2. `run.py` writes patched copies of seven source files to `<work>/out/_overrides/` (the `PATCHES` table — each one is a change the real port has to make) and the wrappers compile those instead of the originals.
3. `stubs/` supplies empty stand-ins for API that no longer exists (.NET Remoting lifetime services, WCF `ServiceHost`, COM+ `ServicedComponent`) so the compiler gets past the missing types and reports what else is wrong. Code behind a stub compiles but **must not be taken to work**.
4. `summ.py` reduces each build log to distinct errors by code.
5. `--smoke` builds `Smoke/`: starts a `Server` in-process on a SQLite catalog store in a temp instance directory, evaluates D4 expressions, runs DDL, then hammers it from 8 threads with create/query/drop cycles.
6. `--coverage` builds `Cov/`: copies the needed D4 libraries to a scratch directory together with their freshly built assemblies, registers `Frontend`, and runs every script that `Tests/DAE.Server.Tests/CoverageTest.cs` runs.

## Results at the time of writing (identical on .NET 8.0.17 and .NET 10.0.15 unless noted; Windows 11)

| Step | Result |
|---|---|
| Base, Engine (164k lines), Common, NativeCLI, Client, DAE.Client, DAE.Client.Provider, SQLite store | compile with 0 errors (three one-line patches) |
| Server | 0 errors **with stubs**; real work is 4 WCF host files, remoting leases in 3 files, one COM+ class |
| SQLDevice, MSSQLDevice, SimpleDevice, Connection, Frontend server, System.Platform/Internet/Integration, Diagnostics | 0 errors (two one-line patches) |
| DAE.Client.Controls, Frontend.Client, Frontend.Client.WPF, Frontend.Client.Windows | **0 errors on .NET 10** (with the checked-in third-party binaries referenced). On .NET 8 they stop at the removed WinForms types `ContextMenu`, `MenuItem`, `ToolBar`, `StatusBar`. **.NET 10 brought those types back only as shells that throw `PlatformNotSupportedException` when constructed** (obsolete, diagnostic `WFDEV006`) — compiling is not working |
| Dataphoria | 1 error on .NET 10 (`TreeNode.ContextMenu` no longer exists to override) |
| DAEServer | 0 errors with C# 12; 4 with C# 14, where `field` became a keyword inside property accessors (vendored `CommandLineArguments.cs`) |
| DAE.Service | needs `System.Configuration.Install` replacement |
| Smoke | engine starts in about 1 s, 1,080 operators registered, D4 DDL/DML/views/operators work |
| Concurrency (8 threads × 15 DDL cycles) | 0 errors in 1.6 s with WAL + immediate transactions + the emitter fix; 39–47 errors in 60–80 s without them. A rare "Collection was modified" (K3, the operator resolution cache) is fixed in the repo |
| Resolution cache (8 threads × 1 s of add/clear) | 0 errors. Reproduces K3 directly: before the fix, about 1.4 million "Collection was modified" errors in 5 s |
| Coverage scripts | 62 pass, 14 fail (12 need the helper library that requires a live SQL Server; 1 uses `Delegate.BeginInvoke`; 1 needs that same helper library) |
