description: Add the two small configuration files that tell Git which files are binary and tell editors the repository's existing formatting conventions (tabs, Windows line endings), written to match the files as they are so that adding them changes no existing file.
prereq: repo-move-to-new-layout
architecture: docs/modernization-plan.md#ws12-repository-hygiene
files: .gitattributes, .editorconfig, src/.editorconfig, Dataphor.sln, AGENTS.md, tickets/rules/10-dataphor.md
difficulty: easy
----
# `.gitattributes` and `.editorconfig` without renormalizing

**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout of the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout). `src/.editorconfig` is today's `Dataphor/.editorconfig` (the move ticket's default destination for it; if the move put it elsewhere or deleted it, use what is there — `git ls-files '*.editorconfig'`). `Dataphor.sln` is today's `Dataphor/Dataphor.sln` (the move's default is the repository root; check). The measurements below were taken on the pre-move tree, restricted to files that survive the deletion and parking tickets; re-measure before writing the files.

## Goal

The repository has no `.gitattributes`, and its only `.editorconfig` (under today's `Dataphor/`) contradicts the tree in two settings. Two consequences: Git guesses which files are binary, and every contributor's `core.autocrlf` setting decides line endings; and an editor that honours the existing `.editorconfig` adds a byte-order mark and a final newline to files that have neither, re-encoding files nobody meant to change (`tickets/rules/10-dataphor.md`, "Source hygiene"). This ticket adds one root `.gitattributes` and one root `.editorconfig` that **describe the tree as it is** and leave every existing file byte-for-byte unchanged in the index and in a fresh checkout.

## Settled

- WS12 table: add `.gitattributes` **without renormalizing** existing files; declare binary types; pin `eol` only for new file types; one `.editorconfig` at the root.
- D12: no history rewrite; D1: no style or formatting churn.
- Source hygiene rule: preserve each file's tabs, line endings and byte-order mark state; never reformat or re-encode a file you are not otherwise changing.

No known defect is fixed. No migration-notes entry is owed (nothing an application or extension can notice). No test is owed (configuration only); "Done when" gives the verification instead.

## Conventions as measured

Measured with `git ls-files --eol` (index line endings) and a script reading each file's first three bytes and leading whitespace, over the files that remain after `repo-delete-dead-trees`, `library-remove-dead-devices` and `library-park-side-projects` (excluding `Legacy/`, `Documentation/`, `Setup/`, `Utilities/`, `Images/`, `External/` and the submodule `tess/`). This machine has `core.autocrlf=true` (system Git configuration); the **index** column is what matters, because it is what every clone receives.

| Type | Files | Line endings in the index | Byte-order mark | Indentation | No final newline |
|---|---|---|---|---|---|
| `.cs` | 840 | CRLF 811, LF 28, none 1 | 344 with, 496 without | tabs 768, spaces 66 | 207 |
| `.d4` | 304 | CRLF 268, mixed 24, LF 8, none 4 | 3 with | tabs 269, spaces 22 | 60 |
| `.d4l` | 32 | CRLF 31, LF 1 | 21 with | tabs 21 | 32 |
| `.csproj` | 40 | CRLF 40 | 39 with | two spaces | 39 |
| `.resx` | 90 | CRLF 90 | 6 with | mixed | 79 |
| `.dfd` / `.dfdx` | 50 / 26 | CRLF 49, LF 1 / CRLF 26 | rare | two spaces | almost all |
| `.config` | 20 | CRLF 14, LF 6 | 6 with | mixed | 6 |
| `.sln` | all | CRLF | none | tabs | — |
| `.txt` | 48 | CRLF 41, LF 5, other 2 | — | — | — |
| `.md`, `.py`, `.json` | 10, 3, 4 | LF only | none | — | — |

Two corrections to what the repository says about itself, which this ticket must also fix: `AGENTS.md` says C# files are "UTF-8 with BOM in most `.cs` files" — measured, 41 % have one (602 of 1,241 across the whole tree). And `src/.editorconfig` sets `charset = utf-8-bom` and `insert_final_newline = true` for C#, which would change 496 and 207 files respectively the next time each is saved in an editor that honours it.

Types Git already treats as binary (`i/-text` in `git ls-files --eol` over the whole tree): `png`, `bmp`, `gif`, `jpg`, `ico`, `psd`, `cdr`, `ai`, `clk`, `emf`, `dll`, `exe`, `pdb`, `lib`, `wixlib`, `zip`, `jar`, `chm`, `pdf`, `doc`, `docx`, `pptx`, `vsdx`, `woff`, `woff2`, `ttf`, `eot`, `cub`, `dat`, `sdf`, `snk`. Three types have a single binary file among text ones (`.txt`, `.xsl`, `.xslt`, all under `Legacy/` or `Documentation/`); do **not** declare those types binary.

Types that did not exist before the modernization: `.slnx`, `.props`, `.yml`/`.yaml`, `.sh`, `.ps1`. By the time this ticket runs, `build-core-sdk-projects` may have added `.slnx` and `.props` files (and `build-ci` `.yml`); check what is tracked.

## What to write

### `.gitattributes` (root)

- **No `* text=auto` line, and no `text` attribute on any type that has a file stored with CRLF in the index.** With `text` set, Git normalizes CRLF to LF when content is added; a CRLF blob then no longer matches what Git would store, so the file shows as modified once touched and `git add --renormalize` rewrites it. That is exactly the renormalization the plan forbids. (Git's protection for CRLF already in the index applies only to `text=auto`, and `text=auto` would still renormalize the 24 mixed `.d4` files and change behaviour for every contributor.)
- `binary` (Git's macro for `-text -diff -merge`) for each binary type in the list above, upper- and lower-case extensions covered (`*.png`, `*.PNG` — Git attribute patterns are case-sensitive; the tree has `.PNG`, `.ICO`, `.DLL`, `.PDF`). Adding `binary` to a file Git already detects as binary changes nothing in the index or the working tree.
- `text eol=…` only for a type for which `git ls-files --eol -- '*.<ext>'` shows **every** tracked file as `i/lf` (or no tracked file at all): `eol=crlf` for Windows-tool files (`*.slnx`, `*.props`, `*.targets` only if all `.targets` are LF in the index — today all six are CRLF, so leave `.targets` out); `eol=lf` for `*.yml`, `*.yaml`, `*.sh`. `.md`, `.py` and `.json` are all LF in the index today and could take `eol=lf` safely, but the plan says new types only: leave them unpinned unless the measurement and the "Done when" checks both pass, and say which you chose in the handoff.
- A short comment at the top saying why there is no `text=auto` (two lines, pointing at the WS12 section of the plan).

### `.editorconfig` (root)

- `root = true`.
- `[*]`: `indent_style = tab`. Nothing else at this level — no `end_of_line`, `charset`, `insert_final_newline` or `trim_trailing_whitespace` for all files, because the measurements show each varies within types.
- `[*.cs]`: `indent_style = tab`, `indent_size = 4`, `end_of_line = crlf` (the convention for new files; existing LF files keep LF when edited, per the source hygiene rule). **No `charset`** and **no `insert_final_newline`**. Carry over the `dotnet_*` and `csharp_*` preferences from `src/.editorconfig` unchanged (all are `silent` or `suggestion`; they describe the existing style and change nothing unless someone runs a formatter). Add a one-line comment that formatters (`dotnet format`, IDE code clean-up) must not be run over files not otherwise being changed.
- `[*.{d4,d4l}]`: `indent_style = tab`, `end_of_line = crlf`; no `charset` (`.d4l` is split 21/11 on the byte-order mark).
- `[*.{csproj,props,targets,slnx,dfd,dfdx}]`: `indent_style = space`, `indent_size = 2`, `end_of_line = crlf`. Do not list `.resx` or `.config` (mixed indentation; designer-generated or rarely edited).
- `[*.sln]`: `end_of_line = crlf` (tabs come from `[*]`).
- `[*.{md,py,json,yml,yaml,sh}]`: `end_of_line = lf`, `charset = utf-8`, `insert_final_newline = true`, `indent_style = space` (with `indent_size = 4` for `.py`, 2 for `.yml`/`.yaml`/`.json`). These are all-LF, BOM-free types, so stating it changes nothing; check the BOM count before writing it (`head -c3` on each tracked `.md`/`.py`/`.json`).

Then delete `src/.editorconfig` (a nested file would override the root one for `src/`). If `Dataphor.sln` lists `.editorconfig` as a solution item, point that line at the root file (relative to the solution's folder).

Fix `AGENTS.md`'s "Formatting" rule, and the same claim in the "Source hygiene" paragraph of `tickets/rules/10-dataphor.md` ("in most files, a UTF-8 byte-order mark"), to state the measured convention: tabs and CRLF in most source files; a byte-order mark in some `.cs` and `.d4l` files and not others; preserve whatever each file has.

## Done when

All checks run without touching the main working tree's other files. Do not use `git rm --cached -r .`, `git reset`, `git checkout`, `git restore`, `git stash` or `git clean` in the main working tree (`tess/agent-rules/tickets.md`).

- In the main working tree: `git ls-files --eol > <scratch>/eol-before.txt` before adding the files, and the same into `eol-after.txt` after. The `i/` and `w/` columns are identical for every path; only the `attr/` column changes. `git status --porcelain` shows only the new `.gitattributes` and `.editorconfig`, the deleted `src/.editorconfig`, and the edited `AGENTS.md` and `tickets/rules/10-dataphor.md` (and `Dataphor.sln` if changed).
- In a throwaway clone (outside the repository, for example under the system temp folder): `git clone -q <repo> <tmp>/a`; copy the new `.gitattributes`, `.editorconfig`, the edited `AGENTS.md` and the `src/.editorconfig` deletion into it; commit there (a local commit in a throwaway clone; never pushed). Then in that clone `git add --renormalize . && git status --porcelain` prints nothing: the attributes would renormalize no file.
- Fresh checkouts of that commit with both line-ending settings: `git -c core.autocrlf=false clone -q <tmp>/a <tmp>/b` and `git -c core.autocrlf=true clone -q <tmp>/a <tmp>/c`; `git -C <tmp>/b status --porcelain` and `git -C <tmp>/c status --porcelain` print nothing; `git -C <tmp>/b ls-files --eol` shows `attr/-text …` (from `binary`) on the declared binary types and no `attr/text` on any type that has CRLF blobs. Delete the three clones afterwards.
- Spot-check in an editor that honours EditorConfig (or by reading the file): a BOM-less `.cs` file saved without changes keeps no BOM and no added final newline.
- Put the measurement commands and their summarized output (the table above, re-measured) in the handoff.

## Edge cases & interactions

- **Files added by tickets that ran before this one** (`build-core-sdk-projects` may add `.slnx`, `.props`; `build-ci` may add `.yml` if it has landed). Measure their index line endings first; pin only what is uniform. Verified by the renormalize check in the clone.
- **Case-sensitive patterns.** `*.png` does not match `*.PNG`. Cover the upper-case extensions present in the tree (`git ls-files | grep -E '\.[A-Z]+$'` lists them). Verified by the `ls-files --eol` attribute column in the clone.
- **The `tess/` submodule** has its own repository; the root `.gitattributes` does not apply inside it. Nothing to do; verified by inspection.
- **Mixed-ending `.d4` files (24) and LF `.cs` files (28).** They stay as they are; the editorconfig `end_of_line` only governs lines typed in an editor. Do not "fix" them. Verified by the identical `i/` columns.
- **Contributors with `core.autocrlf=input` or `false` on Windows.** Without `text` attributes on CRLF types they get the index bytes (CRLF) as today. Verified by clone `b`.
- **`git diff` on binary files** now prints "Binary files differ" for types Git might have mis-detected as text before (none in the list today); no content effect.
- **Partial failure.** If any check shows a changed file, remove the attribute that caused it (usually an `eol` pin on a type with a CRLF blob) rather than committing the change.

## TODO

- Re-measure index line endings, byte-order marks and binary detection on the post-move tree (commands above); update the table in the handoff.
- Write `.gitattributes` (comment, `binary` lines including upper-case variants, `eol` pins only for uniform new types).
- Write the root `.editorconfig` from the measured conventions, carrying over the C# style preferences from `src/.editorconfig`; delete `src/.editorconfig`; fix the solution-item path if the solution lists it.
- Correct the "Formatting" rule in `AGENTS.md` and the byte-order-mark sentence in `tickets/rules/10-dataphor.md`.
- Run the before/after `ls-files --eol` comparison in the main tree and the three-clone check; delete the clones and any scratch files.
