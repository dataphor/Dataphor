description: Removed large or sensitive files that nothing in the product uses (image-editor source art, an unused code-generator executable, stray test-framework copies, unread database dumps, and a note that named a developer's machine account), so the repository holds only what is built, tested or documented.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws12-repository-hygiene
files: Images/ (deleted), Dataphor/netsh_address_reservations.txt (deleted), src/protocol/NativeCLI/Thrift/thrift-0.9.0.exe (deleted), tests/Common.Tests/nunit.framework.dll, tests/DAE.Server.Tests/nunit.framework.{dll,xml}, tests/DAE.Client.Tests/nunit.framework.{dll,xml} (deleted), libraries/MSSQLDevice/SybaseCatalogTables/ (deleted), tickets/index-config.json, AGENTS.md, docs/modernization-plan.md
difficulty: easy
----
# Remove unreferenced large files — review handoff

Deletions only, with `git rm` (no history rewrite, D12), plus three documentation/config edits. No code changed; no test is owed (no behaviour changed); no migration-notes entry (nothing shipped or read by the product).

## What was deleted (about 17.3 MB)

| Path | Size | Reference check (re-run in this ticket) |
|---|---|---|
| `Images/` (228 files) | 11.8 MB | `git grep -I -F -e ClientImages -e DataphoriaImages` outside `Images/` finds only the plan and tickets. Nothing referenced any file, so **no `assets/` folder was created** and nothing was moved |
| `Dataphor/netsh_address_reservations.txt` | 242 B | only the plan named it; `git grep -E "http://\+:|urlacl"` finds nothing else. `Dataphor/` is now gone from `git ls-files` (it held only this file) |
| `src/protocol/NativeCLI/Thrift/thrift-0.9.0.exe` | 4.1 MB | csproj has only `<Folder Include="Thrift\" />`; `ThriftCLI.idl` kept beside it |
| `tests/{DAE.Server,DAE.Client}.Tests/nunit.framework.xml` | 527 KB each | not named by any project |
| `tests/{Common,DAE.Server,DAE.Client}.Tests/nunit.framework.dll` | 127 KB each | each csproj's `nunit.framework` `HintPath` is `..\..\packages\NUnit.3.12.0\…`, not the local copy. `nunit.mocks.dll` and `pnunit.framework.dll` are referenced by `HintPath .\…` and **stay** |
| `libraries/MSSQLDevice/SybaseCatalogTables/` (27 files: `.txt` dumps of Sybase system tables plus `SYBASE_reconciliation.zip`) | 246,508 B total; `syslogs.txt` 150 KB | **Added by the sweep.** No file outside the folder names the folder or any of its files; no source under `src/` or `libraries/` mentions Sybase; the `.d4l` and csproj do not include it; no D4 script loads it. `tickets/plan/9-build-runtime-layout.md` line 62 already described it as "text dumps and a zip that no code reads" and lists it as not-to-copy — that mention is now stale but harmless; I did not edit another stage's ticket |

Reviewer judgment call: the Sybase folder is the one addition beyond the ticket's list. It was deleted as a unit (deleting only the >100 KB `syslogs.txt` would leave an incomplete dump set). Sybase is not a supported device per the plan. History keeps it; restore with `git checkout <parent> -- libraries/MSSQLDevice/SybaseCatalogTables` if a reviewer disagrees.

## Sweep: every file over ~100 KB considered (outside `legacy/`, `Utilities/Wix`, `Setup/`, `Documentation/`, `external/`)

Command: `git ls-files -z | xargs -0 stat -c '%s %n' | sort -rn | awk '$1>100000'`.

- Deleted: the rows above (all `Images/**` `.psd`/`.bmp`/`.ai` entries, thrift exe, NUnit copies, `syslogs.txt`).
- Stays — D4 data loaded by scripts or test data: `samples/Sample.Shipping/Documents/zips.txt` (1.9 MB, `LoadDocument`), `tests/libraries/TestFramework.Coverage.Devices/Documents/InsertDeviceCase.d4` (890 KB), `…/InsertIntoIdealDeviceOperatorsTable.d4` (110 KB), `tests/libraries/TestFramework.Coverage.Scripts/Documents/ViewUpdatability.d4` (126 KB).
- Stays — referenced binary: `tests/Frontend.Client.WPF.Tests/WPFToolkit.dll` (459 KB).
- Stays — compiled source / embedded resource / catalog scripts: `src/engine/DAE/Compiling/Compiler.cs`, `Schema/SystemCatalog.d4`, `Language.D4/Parser.cs`, `Language.D4/Language.cs`, `Language.D4/D4Emitter.cs`, `Schema/SQLDeviceMappings.d4`, `Runtime.Instructions/{JoinNodes,DDLNodes,TableNode}.cs`, `Device.Catalog/CatalogDeviceSession.cs`, `src/windows/Dataphoria/Analyzer/Analyzer.resx` (`EmbeddedResource` in `Dataphoria.csproj`), `libraries/SQLDevice/Source/SQLDevice.cs`, `src/windows/DAE.Client.Controls/DBGrid.cs`, `libraries/MSSQLDevice/Source/SystemCatalog.d4`, `libraries/PGSQLDevice/Source/SystemCatalog.d4`, `docs/modernization-plan.md`.
- Stays — out of scope: `src/client/DAE.Client/View-263.cs` (156 KB stray source; `build-globbing-cleanup` owns strays).

## Doc/config edits

- `tickets/index-config.json`: dropped the `Images/` exclusion.
- `AGENTS.md`: removed the `Images/`, `Dataphor/` layout row and `Images/` from the index-config sentence.
- `docs/modernization-plan.md`: old → new table rows for `Images/` and the netsh file now say deleted (no `assets/` created); WS12 rows for `Images/` source art and the netsh file say Done (the Images row also records the thrift/NUnit/Sybase deletions); K21 status `fixed`, "Fix in" names `repo-delete-dead-trees` (database, logs) and `hygiene-large-binaries` (the file). The target-tree sketch still lists `assets/` as a future folder — left as is. The historical size inventory (line ~736, "`Images/` 11 MB") is a past measurement and left as is.

## Verification done

- `git ls-files` for every deleted path prints nothing; top-level entries no longer include `Images` or `Dataphor`.
- `git grep -n -I -F -e ClientImages -e DataphoriaImages -e thrift-0.9 -e netsh_address` → only `docs/modernization-plan.md` and `tickets/`.
- Spike `python tools/net-spike/run.py <scratch dir> --smoke --coverage`: engine/server/libraries 0 errors, smoke OK, concurrency 0 errors, resolution cache 0 errors, coverage `SUMMARY pass=62 fail=14 timeout=0 skipped-ignored=13` — the recorded baseline. The test-project and host build errors it prints (Common.Tests can't find NUnit, NU1201 on DAE.Server/Client.Tests and D4Runner, DAEService `ServiceInstaller`, Dataphoria/DAEServer) are the spike's existing behaviour: its project summary shows `nunit.framework` as "unmapped" (it never used the local copy), so they are unrelated to these deletions. No SDK-style root build exists yet, so no `dotnet build` was run.

## Notes for reviewer

- `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` still has a `HintPath` `..\..\Dataphor\bin\System.Data.SqlServerCe.dll` — an untracked old build-output path, not one of these files; SQL CE removal owns it.
- The privacy-sensitive file's contents are not quoted anywhere.
