description: Added root files that tell Git which files are binary and tell editors the repository's formatting conventions, written to match the existing files so that adding them changes no file.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws12-repository-hygiene
files: .gitattributes, .editorconfig, src/.editorconfig (deleted), Dataphor.sln, AGENTS.md, tickets/rules/10-dataphor.md, docs/modernization-plan.md
difficulty: easy
----
# `.gitattributes` and `.editorconfig` without renormalizing

## What landed

The work landed in `ticket(implement): hygiene-gitattributes-editorconfig`. That commit:

- Added a root `.gitattributes`. It declares `binary` for every type Git already detects as binary, including the upper-case extensions found in the tree. It pins `text eol=crlf` for `*.slnx` and `*.props`, and `text eol=lf` for `*.yml`, `*.yaml` and `*.sh`. No file of those five types is tracked today. It has no `* text=auto` and no `text` rule for any type that has CRLF files in the index.
- Added a root `.editorconfig`, which replaces `src/.editorconfig` (deleted). `[*.cs]` sets tabs, size 4 and CRLF for new lines, and **no** `charset` or `insert_final_newline`. All of the old file's `dotnet_*`/`csharp_*` style settings were carried over unchanged. There are also sections for D4, the XML project and form files, `.sln`, and the LF types (`.md`, `.py`, `.json`, `.yml`, `.yaml`, `.sh`).
- Changed the Solution Items entry in `Dataphor.sln` to the root file.
- Updated the formatting rule in `AGENTS.md` and the source-hygiene paragraph in `tickets/rules/10-dataphor.md` to the measured convention: some `.cs` and `.d4l` files have a byte-order mark and others do not, and each file keeps what it has.

The implement handoff has the measurements per type and the clone-based renormalize checks; they are not repeated here.

## Review findings

- **Diff read first.** All six changed paths match the handoff. The `.sln` edit is a single line and keeps CRLF.
- **Carried-over C# settings:** I compared the `dotnet_*`/`csharp_*` lines of the old `src/.editorconfig` (from the parent commit) with the new file, after stripping CR and trailing spaces. They are identical. The old file also had `charset = utf-8-bom` and `insert_final_newline = true` on `[*.{cs,csx}]`. Dropping both is intended, since about 60 % of `.cs` files have no BOM. No `.csx` files are tracked, so narrowing the section to `*.cs` changes nothing.
- **Binary declarations against the index:** `git ls-files --eol` shows no path that has `attr/-text` without also being `i/-text`, so no text file is mis-declared as binary. No path has `attr/text`, so none of the `eol` pins applies to an existing file. This covers the whole tree, not only the measured subset: `tess/` is a gitlink and contributes no files.
- **Other attribute or editorconfig files:** the only tracked ones are the two root files, so no nested `root = true` overrides the root file.
- **Renormalize check:** not rerun. The handoff's comparison against the parent commit under both `core.autocrlf` settings is sound. This machine has system `core.autocrlf=true` (from `C:/Program Files/Git/etc/gitconfig`), which is why `--renormalize` lists files there with or without the new attributes. I agree with the handoff that it is not caused by this change.
- **Docs (fixed inline):** `docs/modernization-plan.md` still said the root `.editorconfig` and `.gitattributes` "do not exist yet" (Repository layout status paragraph), and the WS12 line-endings row still read as to-do. Both now say what was done. `docs/architecture.md` and `docs/migration-notes.md` do not mention either file. No migration note is needed, because neither file can change what an application or extension sees.
- **Handoff gaps reviewed and accepted as is:**
  - `end_of_line = crlf` on `.cs`, `.d4`/`.d4l` and the XML types could make an editor plugin that rewrites the whole file on save convert the few LF or mixed files. The ticket asked for it, and the comment in `[*.cs]` says existing LF files keep LF. No change.
  - `indent_size = 2` for `.md`/`.sh`, and `[*] indent_style = tab` reaching `.resx`/`.config` files that mostly use spaces, only affect new indentation, never existing bytes. No change.
- **Tests:** none added or needed. This change is configuration only and has no behaviour for the D4 harness to show. The spike was not run, because no compiled or script file changed.
- **Tripwires:** none filed. **Tickets:** none filed.
