description: Review the newly added files that tell Git which files are binary and tell editors the repository's formatting conventions; they were written to match the existing files, so adding them changes no file.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws12-repository-hygiene
files: .gitattributes, .editorconfig, src/.editorconfig (deleted), Dataphor.sln, AGENTS.md, tickets/rules/10-dataphor.md
difficulty: easy
----
# Review: `.gitattributes` and `.editorconfig` without renormalizing

## What changed

- **`.gitattributes` (new, root).** A two-line comment explaining why there is no `* text=auto`. `binary` lines for each type Git already detects as binary, with upper-case variants where they exist in the tree (`.PNG`, `.BMP`, `.ICO`, `.DLL`, `.PDF`). `text eol=crlf` for `*.slnx`, `*.props`; `text eol=lf` for `*.yml`, `*.yaml`, `*.sh`. No file of any of these five types is tracked today, so the pins only affect files added later. `.targets` is not pinned: all its tracked files are CRLF in the index. `.md`, `.py` and `.json` are **left unpinned**: they are all LF today, but the plan says to pin new types only. `.dat` and `.sdf` from the ticket's list are not declared, because no file of either type is tracked any more and `.dat` is a name text files also use.
- **`.editorconfig` (new, root)**, written following the ticket's section list: `[*]` sets only `indent_style = tab`. `[*.cs]` has tabs, size 4 and `end_of_line = crlf`, with **no `charset` and no `insert_final_newline`**, plus a comment saying not to run formatters. It then carries every `dotnet_*`/`csharp_*` line from the old `src/.editorconfig` with the same values; only the `#####` banner lines were dropped and the section comments trimmed. After that come the d4/d4l, xml-project/form (two-space indent), sln, and md/py/json/yml/yaml/sh sections (LF, utf-8, final newline, spaces; indent 4 for `.py`, 2 otherwise). The file itself is LF.
- **`src/.editorconfig` deleted** (`git rm`, staged). `Dataphor.sln`'s Solution Items entry now reads `.editorconfig = .editorconfig`. The edit was byte-exact: CRLF kept, no BOM before or after.
- **`AGENTS.md` "Formatting" rule and the "Source hygiene" paragraph of `tickets/rules/10-dataphor.md`** now give the measured convention: some `.cs`/`.d4l` files have a byte-order mark and others do not, and each file keeps what it has.

No migration-notes entry (nothing an application or extension can notice). No test (configuration only); the checks below are the verification.

## Measurements (post-move tree, excluding `legacy/`, `Documentation/`, `Setup/`, `Utilities/`, `Images/`, `external/`, `tess/`)

Script: `git ls-files --eol -z`, plus a Python pass that reads each file's first three bytes, last byte and leading whitespace per line.

| Type | Files | Index line endings | BOM | Indentation (majority per file) | No final newline |
|---|---|---|---|---|---|
| `.cs` | 843 | CRLF 811, LF 31, none 1 | 344 | tabs 769, spaces 69 | 207 |
| `.d4` | 304 | CRLF 268, mixed 24, LF 8, none 4 | 3 | tabs 269, spaces 22 | 60 |
| `.d4l` | 32 | CRLF 31, LF 1 | 21 | tabs 21, spaces 11 | 32 |
| `.csproj` | 40 | CRLF 40 | 39 | spaces | 39 |
| `.resx` | 90 | CRLF 90 | 6 | tabs 33, spaces 57 | 79 |
| `.dfd` / `.dfdx` | 50 / 26 | CRLF 49, LF 1 / CRLF 26 | 1 / 0 | spaces (2 `.dfdx` tabs) | 49 / 26 |
| `.config` | 20 | CRLF 14, LF 6 | 6 | mixed | 6 |
| `.sln` | 1 | CRLF | 0 | tabs | 0 |
| `.md` / `.py` / `.json` | 47 / 3 / 4 | LF only | 0 | — | 0 |
| `.slnx`, `.props`, `.yml`, `.yaml`, `.sh`, `.ps1` | 0 | — | — | — | — |

Whole tree: 2088 `i/crlf`, 688 `i/lf`, 28 `i/mixed`, 21 `i/none`, 964 `i/-text`. Binary-detected types match the ticket's list, except that `.dat` and `.sdf` are gone. `.txt`, `.xsl` and `.xslt` each have 1–2 binary files among text files and are not declared.

## Verification done

- **Main tree:** compared `git ls-files --eol` before and after. The `i/` and `w/` columns are identical for every path; the only path that disappears is `src/.editorconfig`. `git status --porcelain` shows only the six expected paths.
- **Throwaway clones** (under the session scratchpad, since deleted). In clone `a`, the changes were committed locally. Fresh clones of it were made with `--config core.autocrlf=false` and with `--config core.autocrlf=true`, with and without the new commit:
  - A fresh checkout under either setting shows a clean `git status`.
  - `git add --renormalize .` under `autocrlf=false` changes **nothing**.
  - Under `autocrlf=true` (this machine's system Git configuration), `--renormalize` lists 2,116 files **both with and without the new attributes**; the sorted lists are identical when diffed. That is the existing effect of `core.autocrlf=true` on CRLF blobs, not something this change adds. Reviewer: note this if you rerun the ticket's literal check on a machine with system autocrlf=true. It will look like a failure unless you compare against the parent commit.
  - In `ls-files --eol` of the autocrlf=false clone, 960 binaries show `attr/-text` and no path shows `attr/text`. The 4 binaries without the attribute are the `.txt`/`.xsl`/`.xslt` ones.
- **Editor spot-check:** done by reading the file, not in an editor. No section that matches `.cs` sets `charset` or `insert_final_newline`, so a BOM-less `.cs` file keeps no BOM and gets no final newline added.

## Known gaps / for the reviewer

- In `[*.{md,py,json,…}]`, `indent_size = 2` also applies to `.md` and `.sh`, which the ticket did not specify. One `.md` file has tab-indented lines. Neither changes any existing bytes.
- `end_of_line = crlf` on `[*.cs]` and `[*.{d4,d4l}]` means an editor that applies `end_of_line` to the whole file on save would convert the 31 LF `.cs`, 8 LF `.d4` and 24 mixed `.d4` files when someone edits them. Most editors (VS, VS Code) apply it only to new lines or on explicit command, but some plugins convert the whole file. This is what the ticket asked for; flagging it in case the reviewer prefers to drop `end_of_line` for those types.
