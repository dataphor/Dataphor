description: Review the conversion of the Dataphor server, its SQLite catalog store and the four libraries that depend on the server to modern .NET 10 projects, which lets a real server start inside a test process from the normal build output.
prereq: server-remove-remoting-and-dtc
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: src/engine/Server/Server.csproj, src/engine/Server/Properties/AssemblyInfo.cs, src/engine/Store.SQLite/DAE.SQLite.csproj, src/engine/Store.SQLite/SQLiteConnection.cs, src/engine/Store.SQLite/Properties/AssemblyInfo.cs, libraries/Connection/Source/DAE.Connection.csproj, libraries/Connection/Source/Register.cs, libraries/MSSQLDevice/Source/DAEDeviceMSSQL.csproj, libraries/SimpleDevice/Source/DAE.Device.Simple.csproj, libraries/Frontend/Source/Frontend.Server.csproj, libraries/Frontend/Source/Device/FrontendDevice.cs, src/engine/Base/StreamUtility.cs, Directory.Packages.props, Dataphor.slnx, tools/net-spike/run.py, tools/net-spike/convert.py, tools/net-spike/EngineCheck/EngineCheck.csproj, tools/net-spike/EngineCheck/Program.cs, tools/net-spike/README.md, AGENTS.md, docs/migration-notes.md, docs/modernization-plan.md
difficulty: hard
----
# Review: server project on .NET 10

## What was done

Six legacy projects are now SDK-style `net10.0` projects in `Dataphor.slnx`, built in place into `artifacts/`:

| Project file | Assembly | Notes |
|---|---|---|
| `src/engine/Server/Server.csproj` | `AlphoraDataphorServer` | Legacy compile order. The six WCF service and host files (`Service/CustomServiceHost.cs`, `ListenerService.cs`, `DataphorService.cs`, `DataphorServiceHost.cs`, `ListenerServiceHost.cs`, `NativeCLI/NativeCLIService.cs`) are left out, each with an XML comment at its place in the list naming `protocol-corewcf-host`. `ConnectionManager.cs`, `HandleManager.cs` and all of `Contracts/` stay compiled. Packages: `Newtonsoft.Json`, `System.Data.SqlClient`, `System.ServiceModel.Primitives` and `System.Diagnostics.EventLog` (see below). Constants `TRACE` (+ `DEBUG;TRACE` in Debug). `packages.config` deleted; the `clientaccesspolicy.xml` and `.idl` content items dropped. |
| `src/engine/Store.SQLite/DAE.SQLite.csproj` | `Alphora.Dataphor.DAE.SQLite` | `System.Data.SQLite.Core` 1.0.119 replaces the `HintPath` to `external/System.Data.SQLite.DLL` (1.0.60, x86), which is deleted. Resource `Alphora.Dataphor.DAE.SystemStoreCatalog.sql`. |
| `libraries/Connection/Source/DAE.Connection.csproj` | `Alphora.Dataphor.DAE.Connection` | The five legacy compile items only (the strays stay uncompiled). SQL CE project reference removed (K19). Packages `System.Data.OleDb` 10.0.15 and `System.Data.SqlClient` (`ClearAllPoolsNode` in `Operators.cs` uses `SqlConnection`). |
| `libraries/MSSQLDevice/Source/DAEDeviceMSSQL.csproj` | `AlphoraMSSQLDevice` | `<RootNamespace></RootNamespace>` set explicitly (the SDK sets its default in `Microsoft.NET.Sdk.props`, before the project body, so an empty value in the project wins; confirmed with `-getProperty:RootNamespace`). Resources `SystemCatalog.d4`, `SystemCatalog.sql`. |
| `libraries/SimpleDevice/Source/DAE.Device.Simple.csproj` | `Alphora.Dataphor.DAE.Device.Simple` | `SimpleException.resx` keeps `DependentUpon SimpleException.cs`: resource `Alphora.Dataphor.DAE.Device.Simple.SimpleDeviceException.resources`. |
| `libraries/Frontend/Source/Frontend.Server.csproj` | `Alphora.Dataphor.Frontend.Server` | `USEINCLUDETAG` in both configurations. `Device\FrontendDeviceException.resx` keeps its `DependentUpon`; `ServerException.resx` and `Strings.resx` had none. The `System.Web` leftovers are all in files the project does not compile, so nothing needed patching. |

Each `Properties/AssemblyInfo.cs` keeps only `AssemblyTitle`, `AssemblyDescription`, `DAERegister`, `ComVisible`, `Guid` (BOM and CRLF preserved; done by a script that cut everything after the last kept attribute and dropped the SDK-generated ones before it).

Source changes (Appendix A, compile-only):

- `SQLiteConnection.cs`: `ErrorCode` → `ResultCode` (two sites). The "begin immediate" half is **not** applied (`store-sqlite-concurrency`).
- `FrontendDevice.cs`: `IStreamProvider` qualified as `Alphora.Dataphor.DAE.Streams.IStreamProvider`.
- `Connection/Source/Register.cs`: the `SQLCEConnection` registration line deleted (K19). Nothing else in the file named it.
- The K1 emitter patch was **not** carried into the repository (`port-catalog-emitter-race`).

**Fix the real compile turned up (local, listed as the ticket asks):** `Server/Server.cs` uses `EventLog` / `EventLogEntryType`, which are in the `System.Diagnostics.EventLog` package on .NET 10 (CS1069). The spike hid this because every wrapper got `System.Configuration.ConfigurationManager`, which depends on that package. Added `System.Diagnostics.EventLog` 10.0.15 to `Directory.Packages.props` and the server project. No other compile error appeared.

Spike (`tools/net-spike`):

- `run.py`: `PATCHES` is now an empty list (mechanism and its guard kept); `STUBS`, its guard and the copying code are removed; `stubs/Server.cs` and the `stubs/` folder are deleted; `convert.py` no longer emits the `Stubs.cs` compile item. New `map_converted()` rewrites each `..\<Name>\<Name>.csproj` in the Smoke/Cov templates to the converted project's real path (implemented with plain string replacement; `chr(92)` stands in for the backslash).
- `convert.py`: the wrappers' fallback `System.Configuration.ConfigurationManager` version went from 8.0.1 to 10.0.15. With `System.Data.OleDb` 10.0.15 now in `Directory.Packages.props`, every wrapper took OleDb 10 (which needs ConfigurationManager ≥ 10.0.15) and failed restore with NU1605. A `NOTE:` there says to keep the two in step.
- EngineCheck references the six new projects and lists their legacy resource names. The stale "until Server is converted" comment is reworded: the check stays on the bare engine on purpose.

Docs: AGENTS.md (converted list, spike expectations, `external/` row), spike README, migration notes (SQL CE class **done**; WCF host classes missing from the server assembly **done**, marked as interim wording for `protocol-corewcf-host`; SQLite package **done**; signing count, Newtonsoft and "building from source" rows widened), plan (WS1 row **Done**, WS1 design note, WS5 `protocol-corewcf-host` row names the six files, K19 fixed, WS7 Connection row, Appendix A intro, patch rows and a post-conversion compile note, reorganization leftovers). The WS5 row already counted six files ("five" in `Server/Service` plus `NativeCLIService.cs`), not four as the ticket said. It now names them. Appended context to `feat-ws5-protocol-and-hosting` (Newtonsoft 13, interim migration note), `feat-ws6-windows-client-and-ide` (`DAE.Client` vs `DataphorServiceHost`), `port-catalog-emitter-race` (spike entry gone) and `test-harness-inprocess` (63/14 baseline without K1/K10).

## Validation (all run in this session)

- `dotnet build Dataphor.slnx --no-incremental` and the same with `-c Release`: 0 errors, 137 warnings each. The six projects add 76 of them; the other 61 come from projects converted earlier. By code: Server CA1416 ×10 (event log, `WindowsIdentity`/`WindowsPrincipal` in `Server.cs`), CS0618 ×15 (`System.Data.SqlClient` in `Connection/MSSQL*.cs`), CA2200 ×1 (`LibraryUtility.cs:625`); Connection CA1416 ×48 (OleDb), CS0618 ×1 (`Operators.cs`); SimpleDevice CA2022 ×1 (`SimpleDevice.cs:256`); Store.SQLite, MSSQLDevice and Frontend 0. None suppressed.
- `dotnet run --project tools/net-spike/EngineCheck -c Debug` and `-c Release`: `RESULT: pass`. All sixteen assemblies have exactly their legacy resource names (server: none; SQLite: `Alphora.Dataphor.DAE.SystemStoreCatalog.sql`; Connection: none; MSSQLDevice: `SystemCatalog.d4`, `SystemCatalog.sql`; SimpleDevice: `…Simple.SimpleDeviceException.resources`; Frontend: `…Device.FrontendDeviceException.resources`, `…ServerException.resources`, `…Strings.resources`). Adding the server to the check did not change the bare-engine step: 17 batches run and 3 skipped in `SystemCatalog.d4`, 21 in `Debug.d4`, 1,060 operators, 23 scalar types, as before.
- `python tools/net-spike/run.py <scratch> --smoke --coverage` (fresh work dir), then the host/test wrappers again after the `convert.py` fix, then `--only none --smoke --coverage` again:
  - Smoke: a Debug `Server` from the real projects started in 965 ms / 1,009 ms on .NET 10.0.15; 1,080 operators, 23 scalar types; both `System` version expressions `3.1.0.0`; table, view and operator DDL/DML correct; clean stop. This is the first full-server run built in Debug from the real server project; it exercises K4's `SetThreadName` (`[Conditional("DEBUG")]`, called from `ServerProcess`) with no error.
  - Concurrency (8 × 15, WAL and 30 s busy timeout, no K1 or K10 fix): **24 errors in 27 s** (21 "database is locked", 3 "SQL logic error … unrecognized token" from garbled object scripts, K1) and **10 errors in 7.5 s** (8 + 2) in the second run. Recorded, not gated, as the ticket says.
  - Resolution cache: 0 errors.
  - Coverage: `SUMMARY pass=63 fail=14 timeout=0 skipped-ignored=13` in both runs. The 14 failures are exactly the Appendix A list (12 + SecurityLibrary from the K14 registration failure, DebugLibrary from K5). Dropping the K1 patch changed no single-session script.
  - Wrappers: `DAE.Diagnostics` (coverage helper) 0 errors. `DAE.Client`, `DAE.Client.Provider`, `DAE.Client.Controls`, `Frontend.Client`, `Frontend.Client.Windows` and Dataphoria each fail with 1 error, `DataphorServiceHost` not found (`src/client/DAE.Client/ServerConnection.cs:129`). `Frontend.Client.WPF` still has 0. `DAEServer`: 6 (K23's 4 + `DataphorServiceHost` ×2 at `Program.cs:32`). `DAE.Service`: 8 (the `System.Configuration.Install` 7 + `DataphorServiceHost`). D4Runner, `DAE.Server.Tests`, `DAE.Client.Tests`: NU1201 as before; `Common.Tests`: 15 (NUnit missing) as before.
- SQLite native interop, by inspection of the smoke output: one `System.Data.SQLite.dll` (assembly `1.0.119.0`, token `db937bc2d44ff139`, from the package), `SQLite.Interop.dll` under `runtimes/{win-x64,win-x86,linux-x64,osx-x64}/native/`, and the deps file resolves both from `Stub.System.Data.SQLite.Core.NetStandard/1.0.119`. No second copy.
- Compiled sources of the six projects (84 files, read from their `Compile` lists): no `System.Runtime.Remoting`, `System.EnterpriseServices`, `System.ServiceModel.Web`, `ServiceHost`, `ServiceBehavior` or `namespace System` stubs.
- Project file names: none of the six collides with another project in `Dataphor.slnx`.
- `python tools/docs-link-check/check_links.py --pending tools/docs-convert/legacy-links.tsv`: 0 errors.

## Tests

No new test. Under the project rule the characterization owed here is the spike's smoke and coverage run, plus EngineCheck's resource-name step, which pins a named contract (resources are looked up by name). EngineCheck gained the six assemblies' expected legacy names. Permanent NUnit coverage arrives with `test-harness-inprocess`.

## Known gaps and things for the reviewer to look at

- **Concurrency regresses on purpose** (above) until `port-catalog-emitter-race` and `store-sqlite-concurrency`. The count varies a lot from run to run (10–24), so the docs give a range.
- **`DAE.Client` cannot compile against the real server** until WS5 (non-embedded in-process alias → `DataphorServiceHost`). `ui-client-layering` meets it first; noted in the WS6 umbrella.
- **Newtonsoft.Json 13** (was 9) changed defaults, such as `MaxDepth` 64 when reading. Neither `REST/RESTMarshal.cs` nor NativeCLI serialization is exercised by anything that runs today; noted in the WS5 umbrella for `test-client-server`.
- **Library package dependencies at run time:** the spike's coverage copies each library's main DLL only, so `System.Data.OleDb` is not beside `Alphora.Dataphor.DAE.Connection.dll`. Coverage does not touch OLE DB, so nothing failed. `build-runtime-layout` already plans for this dependency.
- `src/engine/Server/clientaccesspolicy.xml` (Silverlight cross-domain policy) is still on disk but no longer in any project. `Setup/Files.wxs` names a `..\Deploy\` copy that WS9 owns. Left alone; deleting it belongs to whatever removes Silverlight leftovers.
- `System.Data.OleDb` and `System.Diagnostics.EventLog` are pinned at 10.0.15 (the runtime the plan measured). The ServiceModel packages are at 10.1.0, set earlier; nothing here required aligning them.
- CA2022 at `SimpleDevice.cs:256` (`FileStream.Read` assumed to fill the buffer) has been added to the existing tripwire note on `StreamUtility.CopyStream` (`src/engine/Base/StreamUtility.cs`), a comment-only change there.
- Three Store.SQLite sources (`SQLiteCommand.cs`, `SQLiteStoreConnection.cs`, `SQLiteStoreCursor.cs`) are not UTF-8 (a © in a legacy code page in the header comment). They compile, and the byte is inside a comment. Untouched.
- The smoke's deps file lists every converted Dataphor assembly twice, once as a project and once as `<name>.Reference/4.0.0.0`. It applies to all converted assemblies, not just this ticket's. It is a quirk of Smoke being an out-of-repository project and has no effect at run time, since the files are identical. Not investigated further.
- `test-harness-inprocess` (plan stage) still says "76 scripts … 62 pass" in places written before `server-remove-remoting-and-dtc`. I appended the current baseline (63/14 with no spike patches) next to its "Spike patches" paragraph, without rewriting the rest.
