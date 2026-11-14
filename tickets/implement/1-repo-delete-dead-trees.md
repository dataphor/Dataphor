description: Delete the parts of the repository that no longer serve the product (the Silverlight and web clients, empty or abandoned projects, old build scripts, the retired embedded catalog database and a 35 MB database file committed by accident), so the reorganization and the new build only have to deal with live code.
architecture: docs/modernization-plan.md#repository-layout
files: Dataphor/Dataphor.sln, Dataphor/Dataphoria/Dataphoria.csproj, tools/net-spike/projects.txt, tools/net-spike/README.md, AGENTS.md, docs/architecture.md, docs/migration-notes.md, docs/modernization-plan.md, Dataphor/Frontend.Client.Silverlight/, Dataphor/Frontend.Client.Silverlight.Web/, Dataphor/SilverlightDAE/, Tests/Frontend.Client.Silverlight.Tests/, Dataphor/Frontend.Client.Browser/, Dataphor/Frontend.Client.Web/, Dataphor/Frontend.Client.Web.Application/, Dataphor/BOP/, Dataphor/Language.SQL/, Dataphor/Dataphoria/Dataphoria.Web.API/, Dataphor/DAE.SQLCE/, Dataphor/Dataphoria/Default Server Configurations/, Utilities/NUnit-2.5.0.9122/, dataphor-mono/, Libraries/Libraries.sln, Dataphor.proj, BuildEXEs.bat, BuildSetup.bat
difficulty: easy
----
# Delete dead trees

## Goal

About a third of the projects in the tree are dead, and some large files were committed by mistake. The plan reorganizes the repository next (`repo-move-to-new-layout`) and then converts every live project to the new build; neither should spend effort relocating or converting code that will never ship. This ticket deletes everything the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout) table marks "deleted" that has no later owner, plus the SQL Server Compact (SQL CE) store project and its binaries, and leaves the remaining legacy solution, the spike harness and the docs consistent with what is left. History keeps every deleted file (D12), so nothing here is lost.

The change is deletions plus small edits to files that list the deleted paths. No live code changes.

## Delete list (verified against the tree)

625 tracked files, about 55.9 MB, measured with `git ls-files -z <paths> | xargs -0 cat | wc -c`. For each entry, "referenced by" lists every reference found outside the entry itself (`git grep` on project files, solutions, scripts, `.wxs`, `.py`, `.d4l`, `.cs`); all of them are dealt with below or are themselves deleted, parked or out of scope.

| Path | Files | What it is | Referenced by |
|---|---|---|---|
| `Dataphor/Frontend.Client.Silverlight/` | 71 | Silverlight client (D6) | `Dataphor/Dataphor.sln`; `Tests/Frontend.Client.Silverlight.Tests` |
| `Dataphor/Frontend.Client.Silverlight.Web/` | 6 | Silverlight host page | `Dataphor/Dataphor.sln` |
| `Dataphor/SilverlightDAE/` | 9 | Silverlight engine subset | `Dataphor/Dataphor.sln`; the Silverlight client |
| `Tests/Frontend.Client.Silverlight.Tests/` | 11 | Silverlight test app | nothing (not in any solution) |
| `External/System.Windows.Controls.Layout.Toolkit.dll` | 1 | Silverlight layout toolkit | nothing: the only project naming this assembly is the Silverlight client, which uses its own copy |
| `Dataphor/Frontend.Client.Browser/` | 282 | empty shell around vendored AngularJS | `Dataphor/Dataphor.sln` |
| `Dataphor/Frontend.Client.Web/`, `Dataphor/Frontend.Client.Web.Application/` | 26, 48 | WebForms web client (D6) | `Dataphor/Dataphor.sln`; each other; `Setup/Files.wxs` |
| `Dataphor/BOP/` | 1 | empty project (`BOP.csproj`, no source items, assembly `Alphora.Dataphor.BOP`; BOP itself lives in `Dataphor/Base/BOP`) | `tools/net-spike/projects.txt` line 2; uncompiled VS2003 project files in `Libraries/Connection/Source` and `Libraries/OracleConnection/Source` (by assembly name); a path string in `Dataphor/DAE/Runtime.Instructions/TestNodes.cs:587`. Not in `Dataphor.sln`, not in `Setup/Files.wxs` (never shipped) |
| `Dataphor/Language.SQL/` | 38 | stale standalone copy of the engine's SQL language classes (namespaces `Alphora.Dataphor.DAE.*`, assembly `Language.SQL`) | **only** `Dataphor/Dataphor.sln` (GUID `{715EB620-77B4-490F-A1BF-4CD857AA7110}`) and `tools/net-spike/projects.txt` line 10. No `ProjectReference`, `HintPath`, assembly-name string or installer entry anywhere. Confirmed deletable as the plan's layout table asks |
| `Dataphor/Dataphoria/Dataphoria.Web.API/` | 10 | ASP.NET Core 1.0 template (`.xproj`, `project.json`) inside the IDE folder | nothing; `Dataphoria.csproj` does not include it |
| `Dataphor/DAE.SQLCE/` | 21 | SQL CE catalog store, including `amd64/` and `x86/` native DLLs (D3) | see "SQL CE references left behind" |
| `Tests/DAE.Server.Tests/amd64/`, `Tests/DAE.Server.Tests/x86/`, `Tests/DAE.Client.Tests/amd64/`, `Tests/DAE.Client.Tests/x86/` | 24 | SQL CE native DLLs (`sqlce*40*.dll`), nothing else in these folders | `Content` items in the two test project files |
| `Dataphor/bin/System.Data.SqlServerCe.dll` | 1 | SQL CE managed provider | `HintPath` in `Dataphor/DAE.SQLCE` (deleted) and `Tests/DAE.Server.Tests/DAE.Server.Tests.csproj` |
| `Dataphor/Dataphoria/Default Server Configurations/` | 9 | a committed server instance: `Processor/Catalog/DAECatalog.sdf` (35,278,848 bytes), an empty `Processor/Log/Dataphor.log`, `ServerConfigurations.config`, four `.dat` and two `.bop` files (K21, catalog and log part) | five `None`/`Folder` items in `Dataphor/Dataphoria/Dataphoria.csproj` (lines 577–578, 625–627) |
| `Utilities/NUnit-2.5.0.9122/` | 62 | vendored NUnit 2.5 | nothing (the test projects reference NUnit 3.12 from the package folder) |
| `dataphor-mono/` | 1 | `dataphor-mono.sln`, a stale solution | nothing |
| `Libraries/Libraries.sln` | 1 | stale solution | nothing |
| `Dataphor.proj`, `BuildEXEs.bat`, `BuildSetup.bat` | 3 | MSBuild script and batch files that build `Dataphor.sln` and assemble `Deploy/` for the old installer | each other |

Do **not** delete `Dataphor/DAE/Language.SQL/` (and its siblings `Language.TSQL/` etc. inside `Dataphor/DAE/`): those are the engine's own compiled copies, listed in `Engine.csproj` lines 138–140 and 700–701. Only the top-level project folder `Dataphor/Language.SQL/` goes.

## Out of scope (with owners)

- `Utilities/Wix/` and `Setup/`: kept until the installer is replaced (`feat-ws9-installer-and-release`). `BuildSetup.bat` is deleted here; the old installer then cannot be built by script, which is accepted (nothing can build it on this toolchain today anyway). `Setup/Files.wxs` and `Setup/Files.wxs.bak` keep listing deleted binaries; leave them.
- `Documentation/`, `Legacy/Documentation/`: WS8 (`docs-convert-asciidoc`, `feat-ws8-documentation-remainder`).
- The dead MySQL, DB2, DB2/400, DataDirect, ODP.NET, Caché, Linter and Fastore libraries: `library-remove-dead-devices`.
- The top-level `Dataphoria/` tree, `Processor/`, `Design/`, FHIR/CQF/PHINVADS and the Oracle, ADO and SAS libraries: parked (moved, not deleted) by `library-park-side-projects`.
- Every other SQL CE mention — code, tests, legacy project items, docs — per the ownership ruling: `store-remove-sqlce-references`, except K19's change in `Libraries/Connection/Source/Register.cs` and that project's reference to the SQL CE project, which `build-core-sdk-projects` makes.
- `Dataphor/netsh_address_reservations.txt` (the other part of K21) and unreferenced source art in `Images/`: `hygiene-large-binaries`.
- `tickets/index-config.json` still excludes several deleted paths; harmless, and `repo-move-to-new-layout` rewrites that file.
- The `#if SILVERLIGHT` branches in live code and the `AllowSilverlightClients` setting: `hygiene-silverlight-conditionals` (in `debt-ws12-hygiene-remainder`).
- `Dataphor/DAE/Runtime.Instructions/TestNodes.cs:587` builds a path to the BOP assembly in an old test operator; it is a run-time string, not a build reference. Leave it; `repo-move-to-new-layout` lists that file's relative paths.

## Settled decisions and known defects

- D3: SQL CE is deleted outright; catalogs are rebuilt, not migrated.
- D6: Silverlight and the WebForms web client are removed.
- D12: no history rewrite; ordinary delete commits.
- D14: delete and park before the move.
- K21 (catalog database and server log committed under `Dataphor/Dataphoria/Default Server Configurations`): fixed here. The netsh file part of K21 stays with `hygiene-large-binaries`.

## Keeping what remains consistent

**`Dataphor/Dataphor.sln` stays consistent.** It is the only solution that survives this ticket, and later tickets (`store-remove-sqlce-references`, `library-park-side-projects`, `library-remove-dead-devices`, the move) read it. Remove these entries completely:

| Project | GUID |
|---|---|
| `DAE.SQLCE` | `788980F0-5A85-4F9A-9E6A-56746E7D37C7` |
| `SilverlightDAE` | `B190A64D-B46B-4C63-A217-1ABD2C97021A` |
| `Frontend.Client.Silverlight` | `A30486D2-B128-4459-A26A-FBD3601599F0` |
| `Frontend.Client.Silverlight.Web` | `514F81A0-F061-4DE7-A84A-91379BDA11E2` |
| `Frontend.Client.Web.Application` | `E54C5BA1-3146-43BD-8458-E442038F0FE5` |
| `Frontend.Client.Web` | `6968E0BF-BC20-43C5-AC68-5C2CF8231389` |
| `Language.SQL` | `715EB620-77B4-490F-A1BF-4CD857AA7110` |
| `Frontend.Client.Browser` | `89E4AC1E-BFC0-422D-9222-80C88D2F6499` |
| solution folder `Silverlight` (empty once the three above go) | `FC515183-B412-4C67-BF18-1035E42E9C5B` |

"Completely" means: the `Project(...)` … `EndProject` block, every `ProjectConfigurationPlatforms` line, every `NestedProjects` line, and any `ProjectDependencies` entry inside a *surviving* project's block that names one of these GUIDs. Afterwards `grep -c -i <GUID> Dataphor/Dataphor.sln` is 0 for each. The file is ASCII with CRLF line endings and no byte-order mark; keep it so. Do not re-sort or otherwise touch other entries.

**`Dataphor/Dataphoria/Dataphoria.csproj`:** delete the five items naming `Default Server Configurations\…` (two `None`, three `Folder`). Keep the file's BOM and CRLF.

**Spike (`tools/net-spike/`).** The spike compiles projects from `projects.txt`; `convert.py` parses each listed project file and would fail on a missing one. Remove `Dataphor/BOP/BOP.csproj` and `Dataphor/Language.SQL/Language.SQL.csproj` from `projects.txt`. Nothing else in the spike names a deleted path: `run.py`'s `PATCHES` touch only live files, the smoke and coverage harnesses use the SQLite store explicitly, and `COVERAGE_LIBS` copies only live libraries. In `tools/net-spike/README.md`, drop `Language.SQL` from the first row of the results table.

**Docs.**

- `AGENTS.md`: in "Build and test", "It compiles 32 projects" becomes 30. In the layout table, drop `DAE.SQLCE` from the catalog-store row and drop the deleted entries (`Dataphor/*Silverlight*`, `Frontend.Client.Web*`, `Frontend.Client.Browser`, `dataphor-mono/`) from the "Dead or parked" row; `repo-move-to-new-layout` rewrites the whole table later, so keep the edit minimal. The two `SQL CE` sentences elsewhere in `AGENTS.md` belong to `store-remove-sqlce-references`.
- `docs/architecture.md`: in "What is alive", remove the deleted items from the "Dead" row (Silverlight client and `SilverlightDAE`, `Frontend.Client.Browser`, `Dataphor/BOP`, `Dataphor/Dataphoria/Dataphoria.Web.API`, `dataphor-mono/`, `Libraries/Libraries.sln`) and the WebForms web client from the "Cannot survive .NET 10" row; in the layers diagram, drop "(web client, dead)". Leave the SQL CE store mentions to `store-remove-sqlce-references` and the dead connections and devices to `library-remove-dead-devices`.
- `docs/modernization-plan.md`: in the old → new table, change the `Dataphor/Language.SQL` row's right-hand side to "deleted (`repo-delete-dead-trees`; only the old solution referenced it)". No other plan edits; Appendix A records the survey as it was run.

## Migration notes

`docs/migration-notes.md`, "Platform and deployment":

- "The ASP.NET WebForms web client is removed." → **done**.
- Split "The Silverlight client and the `AllowSilverlightClients` instance setting are removed." into two rows: "The Silverlight client is removed." (**done**, what to do: nothing — it had no users), and "The `AllowSilverlightClients` instance setting is removed." (**planned**, what to do: remove the setting).
- Leave the SQL CE row as **planned**; the default store still names SQL CE until `store-remove-sqlce-references` changes it.

No entry for `Alphora.Dataphor.BOP` or `Language.SQL`: neither assembly was ever installed (absent from `Setup/Files.wxs`), and neither contains types.

## Proof that the deletion is safe

No test is owed (nothing live changes behaviour); the proof is the spike, which builds the live projects and runs the engine and the D4 coverage scripts:

```
python tools/net-spike/run.py <work-dir> --smoke --coverage
```

`<work-dir>` must be outside the repository, for example under the system temp folder. The run takes several minutes; run it in the foreground so its output streams. Run it once **before** deleting anything and once after, and compare. Save each run's output with `| tee tickets/.logs/repo-delete-dead-trees.before.log` / `.after.log`.

Expected, before and after: engine, server and libraries 0 errors; concurrency 0 errors; coverage `pass=62 fail=14`, with the same 14 scripts failing. Expected differences, and only these:

- no `BOP` or `Language.SQL` wrapper is generated or built;
- `DAE.Server.Tests` (which already fails to compile in the spike today, as does `DAE.Client.Tests`) now lists `System.Data.SqlServerCe` among its unmapped references in `convert.py`'s output, and may report additional errors in `SQLStoreTest.cs` for the `SqlCe*` types. No other project's error list may change.

If any other difference appears, a deleted path was live: stop and report it rather than deleting further.

## Done when

- Every path in the delete list is gone (`git ls-files <path>` prints nothing for each) and nothing else was deleted (`git status --porcelain` shows only `D` entries for those paths plus `M` for the files under "Keeping what remains consistent" and the migration notes).
- `Dataphor/Dataphor.sln` contains none of the nine GUIDs, and every remaining `Project(` line whose path ends in `.csproj` points at a file that exists (check with a short script; the top-level `Dataphoria/…` and the dead-device library entries still exist at this point and are removed by their own tickets).
- `git grep -n -I -E "Frontend\.Client\.(Silverlight|Web|Browser)|SilverlightDAE|Dataphor[\\/]BOP|dataphor-mono|Libraries\.sln|Dataphor\.proj|BuildEXEs|BuildSetup|NUnit-2\.5|Default Server Configurations"` finds only: `docs/modernization-plan.md`, `tickets/`, `Setup/`, `Legacy/`, `Documentation/`, the top-level `Dataphoria/` tree, `tickets/index-config.json`, and `#if SILVERLIGHT`-style mentions in live code (if any). Put the trimmed output in the handoff.
- The spike's before and after runs match as described above; quote both summaries in the handoff.
- `docs/migration-notes.md`, `AGENTS.md`, `docs/architecture.md`, `docs/modernization-plan.md` and the spike README are updated as above.

## Edge cases & interactions

- **Two `Language.SQL` folders.** Only `Dataphor/Language.SQL/` is deleted; `Dataphor/DAE/Language.SQL/` is engine source. Verified by inspection of the diff (`git diff --stat --cached` must show no path under `Dataphor/DAE/`) and by the spike (the engine still compiles).
- **Other tickets run on the same solution.** `library-remove-dead-devices` and `library-park-side-projects` also remove entries from `Dataphor/Dataphor.sln`. Remove only this ticket's GUIDs, and do it by deleting whole lines, so that the three edits do not conflict textually. Verified by inspection.
- **Legacy project files keep dangling SQL CE references.** `Libraries/Connection/Source/DAE.Connection.csproj`, `Tests/DAE.Server.Tests/DAE.Server.Tests.csproj` and `Tests/DAE.Client.Tests/DAE.Client.Tests.csproj` still name the deleted SQL CE project, DLL and native files; the Windows client project still names `System.Data.SqlServerCe` as a ClickOnce file; four project files under the top-level `Dataphoria/` reference the SQL CE project. These are left for their owners (see "Out of scope") because nothing builds the legacy projects today; the spike ignores `Content` items and records missing references without failing. Verified by the spike comparison.
- **The spike exits early if a patch target is missing.** None of the seven patched files is deleted. Verified by the "after" run reaching the coverage step.
- **Line endings and encodings.** The edited files keep their existing endings and byte-order mark state (`.sln`: CRLF, no BOM; `.csproj`: CRLF, BOM; `projects.txt`, `README.md`, `AGENTS.md`, `docs/*.md`: LF). Check with `git diff` showing only the intended lines (no whole-file rewrite) and `git ls-files --eol` on the edited files.
- **Partial failure.** If the "before" spike run does not match the expected results in `AGENTS.md`, record what it gave and use that as the baseline; the ticket's claim is "unchanged", not "matches AGENTS.md". If the spike cannot run at all on this machine, say so in the handoff and do not claim the proof.
- **Windows path with spaces.** `Default Server Configurations` contains spaces; quote it in every `git rm` command.

## TODO

- Run the spike once and keep the "before" log.
- Delete the paths in the delete list with `git rm -r -q` (quote paths with spaces).
- Edit `Dataphor/Dataphor.sln` to remove the nine GUIDs' entries; check each GUID's count is 0 and each remaining `.csproj` path exists.
- Remove the five `Default Server Configurations` items from `Dataphor/Dataphoria/Dataphoria.csproj`.
- Remove the BOP and Language.SQL lines from `tools/net-spike/projects.txt`; update the spike README table.
- Update `AGENTS.md`, `docs/architecture.md`, the Language.SQL row of the plan's layout table, and `docs/migration-notes.md`.
- Run the spike again; compare with the "before" log; run the `git grep` from "Done when".
- Write the handoff with both spike summaries, the grep output, and any leftover reference you found that this ticket did not list.
