description: Review the deletion of the dead parts of the repository (Silverlight and web clients, empty or abandoned projects, old build scripts, the SQL Server Compact store and a committed 35 MB database), and check that the remaining solution, spike harness and docs are consistent with what is left.
architecture: docs/modernization-plan.md#repository-layout
files: Dataphor/Dataphor.sln, Dataphor/Dataphoria/Dataphoria.csproj, tools/net-spike/projects.txt, tools/net-spike/README.md, AGENTS.md, docs/architecture.md, docs/migration-notes.md, docs/modernization-plan.md, Libraries/Frontend/Documents/Silverlight.NodeTypes.dnt, Libraries/Frontend/Documents/Web.NodeTypes.dnt
difficulty: easy
----
# Review: delete dead trees

## What was done

- `git rm -r` of every path in the original delete list: 625 tracked files (all `D` in `git status`). Paths: `Dataphor/Frontend.Client.Silverlight/`, `Dataphor/Frontend.Client.Silverlight.Web/`, `Dataphor/SilverlightDAE/`, `Tests/Frontend.Client.Silverlight.Tests/`, `External/System.Windows.Controls.Layout.Toolkit.dll`, `Dataphor/Frontend.Client.Browser/`, `Dataphor/Frontend.Client.Web/`, `Dataphor/Frontend.Client.Web.Application/`, `Dataphor/BOP/`, `Dataphor/Language.SQL/`, `Dataphor/Dataphoria/Dataphoria.Web.API/`, `Dataphor/DAE.SQLCE/`, the four `amd64/`/`x86/` SQL CE native folders under `Tests/DAE.Server.Tests` and `Tests/DAE.Client.Tests`, `Dataphor/bin/System.Data.SqlServerCe.dll`, `Dataphor/Dataphoria/Default Server Configurations/`, `Utilities/NUnit-2.5.0.9122/`, `dataphor-mono/`, `Libraries/Libraries.sln`, `Dataphor.proj`, `BuildEXEs.bat`, `BuildSetup.bat`. No path under `Dataphor/DAE/` was touched (engine's own `Language.SQL` folder intact).
- `Dataphor/Dataphor.sln`: 112 whole lines removed (Project blocks, config-platform lines, nested-project lines, one self-dependency inside a deleted block) for the nine GUIDs. Each GUID count is now 0; all 72 remaining `.csproj` paths in the solution exist. Still ASCII, CRLF, no BOM; diff is deletions only.
- `Dataphor/Dataphoria/Dataphoria.csproj`: the five `Default Server Configurations\…` items removed (5 deletions, CRLF and BOM kept).
- `tools/net-spike/projects.txt`: BOP and Language.SQL lines removed. `tools/net-spike/README.md`: `Language.SQL` dropped from the first results row.
- `AGENTS.md`: "32 projects" → 30; `DAE.SQLCE` dropped from the store row; Silverlight/web/browser/`dataphor-mono/` dropped from the "Dead or parked" row (now `Dataphoria/` (top level), `Utilities/`).
- `docs/architecture.md`: "(web client, dead)" removed from the layers diagram; deleted items removed from the "Dead" row; WebForms removed from "Cannot survive .NET 10".
- `docs/modernization-plan.md`: `Dataphor/Language.SQL` row → "deleted (`repo-delete-dead-trees`; only the old solution referenced it)".
- `docs/migration-notes.md`: WebForms row → **done**; Silverlight row split into "The Silverlight client is removed." (**done**, nothing to do) and "The `AllowSilverlightClients` instance setting is removed." (**planned**).

**Note on commit boundaries:** while this ticket was running, a human commit (`55e3de0f`, "Correct the plan from the ticket survey…") was made from the shared working tree and picked up my edits to `docs/migration-notes.md` and `docs/modernization-plan.md`. Those two edits are therefore already in history under that commit, not in this ticket's commit. Their content is as described above; verify with `grep -n "Language.SQL\` |" docs/modernization-plan.md` and `grep -n -i silverlight docs/migration-notes.md`.

## Proof: spike before and after

`python tools/net-spike/run.py <scratch-dir> --smoke --coverage`, logs in `tickets/.logs/repo-delete-dead-trees.before.log` / `.after.log`.

- Before: `Concurrency: 8 threads x 15 iterations of DDL+query in 2453 ms; 0 errors`, `SUMMARY pass=62 fail=14 timeout=0 skipped-ignored=13`.
- After: `Concurrency: 8 threads x 15 iterations of DDL+query in 1903 ms; 0 errors`, `SUMMARY pass=62 fail=14 timeout=0 skipped-ignored=13`.
- Same 14 failing coverage scripts (sorted name lists identical). Engine, server, libraries 0 errors in both.
- Differences (after normalising timings and GUIDs), and only these: no BOP or Language.SQL wrapper is listed or built; `DAE.Server.Tests` now lists `System.Data.SqlServerCe` under `unmapped` instead of `binary` (its error list is unchanged — still the single NU1201 it had before); compiler error paths print relative instead of absolute because the second run was started from a different current-directory casing, and two error lines in `DAE.Service` are listed in a different order. No project's error list changed.

## Leftover-reference grep

`git grep -l -I -E "Frontend\.Client\.(Silverlight|Web|Browser)|SilverlightDAE|Dataphor[\\/]BOP|dataphor-mono|Libraries\.sln|Dataphor\.proj|BuildEXEs|BuildSetup|NUnit-2\.5|Default Server Configurations"`, excluding the allowed `tickets/`, `Setup/`, `Legacy/`, `Documentation/`, top-level `Dataphoria/`:

```
Dataphor/Error Message Prefix Codes.txt
Libraries/Frontend/Documents/Silverlight.NodeTypes.dnt
Libraries/Frontend/Documents/Web.NodeTypes.dnt
docs/modernization-plan.md
```

Not listed in the ticket and left alone:

- `Dataphor/Error Message Prefix Codes.txt` lines 34, 36, 37 name the Web and Silverlight exception classes for prefixes 204, 206, 207. Error codes are a compatibility surface and this file is the allocation record; keeping retired prefixes listed stops them being reused. Recommend leaving as is (perhaps marking them retired later).
- `Libraries/Frontend/Documents/Silverlight.NodeTypes.dnt` and `Web.NodeTypes.dnt`: node-type documents of the Frontend library that map node names to the deleted `Alphora.Dataphor.Frontend.Client.Silverlight` / `.Web` assemblies. Nothing builds or loads them for a live client now, but they ship in the Frontend library (also listed in `Setup/Files.wxs`), and removing library documents changes a library's document set, so it was not done here. No open ticket claims them (`grep -rl NodeTypes.dnt tickets/...` found nothing). Reviewer: decide whether this is a small inline deletion (plus a migration-notes line) or a ticket — `hygiene-silverlight-conditionals` / `debt-ws12-hygiene-remainder` is the nearest owner.

The `docs/modernization-plan.md` hits are the old → new table and Appendix A, as expected. No `#if SILVERLIGHT`-style hit matched this pattern.

## Known gaps / things to check

- No test added: nothing live changes behaviour; the spike comparison is the proof.
- Legacy project files still name deleted SQL CE artefacts (`Tests/DAE.*.Tests` `Content` items and `HintPath`, `Libraries/Connection/Source/DAE.Connection.csproj`, the Windows client's ClickOnce file list, top-level `Dataphoria/` projects) — owned by `store-remove-sqlce-references` / `build-core-sdk-projects` per the ticket.
- Solution folders whose children were all removed (other than `Silverlight`, which was removed) were not checked for emptiness; the ticket did not ask for that.
- `tickets/index-config.json` still excludes deleted paths (harmless; `repo-move-to-new-layout` rewrites it).
