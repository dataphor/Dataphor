description: Remove large or sensitive files that nothing in the product uses (image-editor source art, an unused code-generator executable, stray test-framework copies, and a note that names a developer's machine account), so the repository holds only what is built, tested or documented.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws12-repository-hygiene
files: Dataphor/netsh_address_reservations.txt, Images/, src/protocol/NativeCLI/Thrift/thrift-0.9.0.exe, src/protocol/NativeCLI/DAE.NativeCLI.csproj, tests/DAE.Server.Tests/nunit.framework.xml, tests/DAE.Client.Tests/nunit.framework.xml, tests/Common.Tests/nunit.framework.dll, tests/DAE.Server.Tests/nunit.framework.dll, tests/DAE.Client.Tests/nunit.framework.dll, tickets/index-config.json, docs/modernization-plan.md, AGENTS.md
difficulty: easy
----
# Remove unreferenced large files

**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout of the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout). Two entries deliberately keep their old paths because the move does not touch them (it says so in its inventory): `Dataphor/netsh_address_reservations.txt` and `Images/`. `src/protocol/NativeCLI/` is today's `Dataphor/DAE.NativeCLI/`; `tests/<Project>/` is today's `Tests/<Project>/`. If the move placed something differently, find it with `git ls-files | grep -i <file name>`.

## Goal

WS12's objective is that the tree contains what is built, tested or documented, and nothing else. This ticket deletes the remaining large or sensitive tracked files that nothing references, after `repo-delete-dead-trees` removed the dead projects and the committed catalog database. History keeps them (D12).

## Delete list (sizes measured with `stat -c %s` on today's tree)

| Path (target layout) | Size | Why it can go | How "unreferenced" was checked |
|---|---|---|---|
| `Dataphor/netsh_address_reservations.txt` | 242 bytes | K21: four `netsh http add urlacl` commands for ports 8060, 8600, 8062 and 8602 with a developer's machine account as the user. The server uses WCF over TCP (`net.tcp`), not HTTP URL reservations | `git grep -n netsh_address` finds only the plan; `git grep -n -I -E "http://\+:|urlacl"` finds nothing else |
| `Images/` (all 228 files) | 11,766,108 bytes: `.psd` 26 files 9.8 MB, `.bmp` 38 files 0.6 MB, `.png` 132 files 0.2 MB, `.cdr` 13, `.ai` 1, `.clk` 3, `.gif` 12, `.ico` 3 | source art and icon masters for the Windows client (`Images/ClientImages/`) and Dataphoria (`Images/DataphoriaImages/`, including `Working/`) | no file in the repository references a path under the top-level `Images/`: `git grep -n -I -F -e "ClientImages" -e "DataphoriaImages"` outside `Images/` finds nothing, and the many `../Images/…` links in `Documentation/` resolve to `Documentation/Images/`, a different folder. 26 of the 228 files have byte-identical copies elsewhere (compared by blob hash); those copies, inside the projects, are what the build embeds |
| `src/protocol/NativeCLI/Thrift/thrift-0.9.0.exe` | 4,129,792 bytes | the Apache Thrift 0.9.0 code generator, committed with an experiment ("Experimenting with thrift idl definition"); no build step runs it and no generated code exists | the project file has only `<Folder Include="Thrift\" />`; `git grep -n -i "thrift-0.9"` finds nothing. Keep `ThriftCLI.idl` beside it (a small source file, not in scope) |
| `tests/DAE.Server.Tests/nunit.framework.xml`, `tests/DAE.Client.Tests/nunit.framework.xml` | 527,156 bytes each | XML documentation for an old NUnit build | not named by any project file |
| `tests/Common.Tests/nunit.framework.dll`, `tests/DAE.Server.Tests/nunit.framework.dll`, `tests/DAE.Client.Tests/nunit.framework.dll` | 126,976 bytes each | an old NUnit assembly copied next to each test project | each project's `nunit.framework` reference has `HintPath` `…\packages\NUnit.3.12.0\lib\net45\nunit.framework.dll` (the restore folder), not the local copy. `nunit.mocks.dll` and `pnunit.framework.dll` in the same folders **are** referenced by `HintPath .\…` and stay |

Total about 17.1 MB.

**Who deletes the unreferenced art.** This ticket does. `repo-move-to-new-layout` leaves `Images/` in place and creates no `assets/` folder, because none of its files is referenced; the plan's "`Images/` → `assets/` (referenced files only)" therefore moves nothing. If the re-check below finds a file under `Images/` that something does reference, `git mv` that file to `assets/<same relative path>` in a rename-only commit, fix the reference in a following commit, and delete the rest.

## Find anything else that qualifies

Before deleting, re-run a sweep for other large tracked files nothing references, because the earlier tickets may have changed the picture:

```
git ls-files -z | xargs -0 stat -c '%s %n' | sort -rn | head -80
```

For each file above about 100 KB outside the excluded trees (below), check references by file name (`git grep -n -I -F "<file name>"`, plus the project file and `.d4l` of the folder it is in). Add a file to the delete list only if nothing references it and it is not data a D4 script loads (for example `samples/Sample.Shipping/Documents/zips.txt`, 1.9 MB, is loaded by `LoadDocument('Sample.Shipping', 'zips')` and stays; `tests/libraries/TestFramework.Coverage.Devices/Documents/InsertDeviceCase.d4` is test data and stays). Record every file you considered, with its size and the reason it stays or goes, in the handoff.

## Out of scope

- `legacy/`, `Legacy/` (if it still exists), `Utilities/Wix/`, `Setup/` (including `Setup/Files.wxs.bak`), `Documentation/` and `Documentation/Images/`: later tickets own them (WS8, WS9).
- `external/` (vendored third-party binaries and their `.pdb` files, such as `external/ICSharpCode.TextEditor.pdb`): `debt-ws12-hygiene-remainder` (`hygiene-vendored-binaries`), which replaces them with packages as consumers are converted.
- Any binary a live project references, including `tests/Frontend.Client.WPF.Tests/WPFToolkit.dll`, `nunit.mocks.dll`, `pnunit.framework.dll` and the DLLs under `external/`.
- Stray source files that no project compiles (`build-globbing-cleanup` in `debt-ws12-hygiene-remainder`).
- Rewriting the test projects onto the new harness: `test-harness-inprocess`.

## Settled decisions and known defects

- D12: ordinary delete commits, no history rewrite.
- D11: a binary stays in git only when no package exists, with a note saying why; unreferenced copies have no reason to stay.
- K21 (a file naming a developer's machine account is committed): fixed here; `repo-delete-dead-trees` fixed K21's catalog-database and log part. Mark K21 fixed in the plan's known-defects table, naming both tickets.

No migration-notes entry is owed: none of these files is shipped or read by the product. No test is owed (deleting unreferenced files changes no behaviour); "Done when" gives the verification.

## Done when

- Every path in the delete list (and anything the sweep added) is gone: `git ls-files <path>` prints nothing for each; `Images/` and `Dataphor/` no longer exist in `git ls-files | cut -d/ -f1 | sort -u` (if `Dataphor/` still holds other files, the move left them; list them in the handoff instead of deleting them).
- `git grep -n -I -F -e "ClientImages" -e "DataphoriaImages" -e "thrift-0.9" -e "netsh_address"` finds only `docs/modernization-plan.md` and `tickets/`.
- The spike still gives its baseline results — `python tools/net-spike/run.py <work-dir> --smoke --coverage` with `<work-dir>` outside the repository (engine, server and libraries 0 errors; concurrency 0 errors; coverage `pass=62 fail=14`, or whatever baseline the tickets landed before this one recorded). If the real build from `build-core-sdk-projects` exists by then, `dotnet build` of the root solution also still succeeds. These show no deleted file was a build input.
- `tickets/index-config.json` no longer excludes `Images/`; `AGENTS.md`'s layout table no longer lists `Images/` (and lists `assets/` only if this ticket created it); the plan's WS12 table rows for `Images/` source art and `Dataphor/netsh_address_reservations.txt` say done; K21 marked fixed.

## Edge cases & interactions

- **A referenced image turns up.** Move it to `assets/` (rename-only commit), fix the reference separately, then delete the rest. Verified by the `git grep` above and by building.
- **Test projects in transition.** If `test-harness-inprocess` has already replaced the test project files by the time this runs, re-check which NUnit files the new projects reference before deleting (they should reference the NuGet package, making all local NUnit copies unreferenced — then `nunit.mocks.dll` and `pnunit.framework.dll` may qualify too; include them only if nothing references them). Verified by `git grep` on the project files.
- **Files with spaces in their names** (for example `Images/DataphoriaImages/Working/Apps and Libraries.psd`): delete the folder with `git rm -r -q Images` rather than file by file.
- **Concurrent move work.** If `repo-move-to-new-layout` split into several tickets and some are still in flight, `prereq:` holds this ticket until the last one lands; if a path is not where the target layout says, find it by name and report the discrepancy.
- **Privacy.** Do not quote the contents of the netsh file (it names a person's account) in the handoff, commit message or any file.

## TODO

- Re-run the reference checks for each listed file and the large-file sweep; extend or shrink the list accordingly.
- Delete the files with `git rm -q` / `git rm -r -q`.
- Update `tickets/index-config.json`, `AGENTS.md` and the plan (WS12 rows, K21).
- Run the spike (and `dotnet build` if it exists); write the handoff with every file considered, its size and its disposition.
