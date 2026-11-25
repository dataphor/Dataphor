description: The coverage report lists individual files only for the subsystems it already judges badly covered, so a source file that no test touches at all stays invisible as long as its neighbours are well covered; make the report name every file nothing executed, wherever it sits.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tools/coverage-report/coverage_report.py, tools/coverage-report/test_coverage_report.py, tools/coverage-report/README.md, docs/coverage-report.md
difficulty: easy
----
## The hole, in one sentence

`render` in `tools/coverage-report/coverage_report.py` prints a per-file table only under `## Gaps, file by file`, and `analysis.gaps` is only the areas classified `gap`. An area's rate is a weighted average over its files, so a file at exactly 0 % sitting in an area that averages 66 % appears in no table in the report: not in *Engine areas*, which prints one rate per area, and not in a per-file table, because the area is not a gap.

On the first full run (2026-11-21, commit `f6200026`, published as `docs/coverage-report.md`) that hid 21 files, 1,300 coverable lines between them. Eleven were 20 lines or more. Two of the three largest turned out not to be untested code at all but code the engine can no longer reach - `debt-unreachable-redefine-nodes` and an arm on `debt-ws12-hygiene-remainder` came out of them - and the published report named none of them until a human added the list by hand.

The per-file numbers this needs are already computed: `analyse` fills `area.files` for **every** area, not only the gaps, and `to_json` already emits them. Only the Markdown rendering withholds them. So this is a rendering change plus one option, and nothing in the rollup moves.

## What to add

A new section, `## Wholly untested files`, listing every file in an engine area with **at least one coverable line and none of them covered**, worst first by coverable count, ties broken by path ascending. Each row carries the file's area and that area's rate, so a reader can tell at a glance whether the file's neighbours are healthy. Files below a line floor are not listed individually but counted in one closing sentence.

Shape of the rendered section:

```
## Wholly untested files

Every source file in an engine area that the run never entered, wherever it sits. An area's rate above is a weighted average over its files, so a file at 0 % inside a well-covered area appears in no other table here. Each row names the file's area and that area's rate: a zero inside an area already marked `gap` is expected, and a zero inside a `covered` one is the finding this section exists for. Files of fewer than 20 coverable lines are counted at the end rather than listed.

| File | Coverable | Area | Area rate |
|---|---:|---|---:|
| src/engine/DAE/Runtime.Instructions/RedefineNode.cs | 257 | Plan nodes | 66.6 % |
| src/engine/DAE/Runtime.Data/ObjectMarshal.cs | 183 | Runtime values | 60.4 % |

Another 10 wholly untested files of fewer than 20 coverable lines each, 93 lines between them, are not listed.
```

Settled decisions, so that none of these has to be re-opened during implementation or review:

- **Placement: immediately after `## Engine areas`, before `## Gaps, file by file`.** The section is a short cross-area index and the gap tables are long; a reader meets the area ranking, then the zeros, then the detail.
- **Every area, gaps included.** A file at 0 % inside a `gap` area is therefore printed twice: once here and once in that area's own per-file table below. That is deliberate. It makes the section's claim unconditional - this is the complete list of files nothing executed - with no "except the ones already below" caveat for a reader to carry, and the area-rate column is what distinguishes the two kinds of zero. It costs little: on the 2026-11-21 run the two wholly-uncovered gap areas held nine source files between them.
- **Engine areas only.** `analyse` accumulates per-file lines only for assemblies on the `engine` list, so files in `reported` assemblies cannot appear and must not be claimed to. Likewise files under an `excluded` path (deliberately out of the rollup, counted in *Excluded from the area rollup*) and files under `src/` that no area claims (reported as `unassigned`, which suppresses the whole report unless `--allow-unassigned` is passed). State this scope in `README.md`, not in every rendered report.
- **The floor is a new option, `--untested-min-lines`, default 20.** The 2026-11-21 distribution splits cleanly there: eleven files of 20 lines or more, the remaining ten at 17 lines or fewer, which are exception classes and small helpers. Boundary follows the convention `classify` already uses for `--min-lines`: a file with **exactly** `--untested-min-lines` coverable lines is listed, one fewer goes to the closing count. Validate `>= 0` as `--min-lines` is validated.
- **Do not reuse `--min-lines`.** It answers a different question - whether a whole *area* is small enough that a low rate should not be called a gap - and the two defaults (200 and 20) are an order of magnitude apart. Two names, two help lines.
- **Nothing gates.** The tool reports and never fails a build: a wholly untested file must not change the exit code, and must not touch either boundary check (`unassigned`, `unlisted assembly`) or the report-suppression rule. A report suppressed today stays suppressed, new section included.
- **Empty case prints a positive line, not silence.** When no file qualifies, print the heading and one sentence - `No source file in an engine area is wholly untested.` - following the precedent of `No engine assembly in this run.` under *Engine areas*. Silence is exactly how this blind spot went unnoticed: a reader cannot tell "nothing to report" from "this tool does not report that".
- **`--json`: add `untested_min_lines` to the `thresholds` object and nothing else.** Every file's numbers are already under `areas[].files[]`, so a derived list would be a second copy of data a consumer can filter for itself; the threshold belongs in the record because it is what produced the rendered section.

## Where the logic goes

Keep the floor in the renderer and the selection in a small module-level function next to `classify`, so the selection is testable without constructing `Options`:

```python
def untested_files(analysis):
    """Every file in an engine area with coverable lines and no covered line.

    [(path, Totals, Area)], worst first by coverable count, ties by path. A file with
    no coverable lines at all is not untested, it is unmeasurable, and is left out: its
    rate is None and a row for it would read as a zero.
    """
```

`render` then splits that list at `options.untested_min_lines` and renders the table and the closing count. `_rate` already prints `-` for a `None` rate and `_count` already groups thousands; reuse both.

## Edge cases & interactions

| Case | How it is verified |
|---|---|
| A file at 0 % in an area classified `covered` is listed, with that area's name and rate - the defect itself | test |
| A file with coverable lines, all of them hit, is never listed | assertion in the same test |
| A file with **zero** coverable lines is not listed (its rate is `None`; a `0 / 0` row would read as a finding) | assertion in the same test |
| Exactly `--untested-min-lines` lines is listed; one fewer is counted in the closing sentence, with the right file count and line total | test |
| A file at 0 % inside a `gap` area appears in both this section and the gap's per-file table | assertion in the first test |
| Two qualifying files of the same coverable count come out in path order, so two runs of one build render identically | assertion in the first test |
| The exit code is unchanged by the presence of a wholly untested file | assertion in the first test |
| No qualifying file: the section still prints, with the positive sentence | test |
| An area whose rate is `None` cannot own a listed row (its only files have no coverable lines, which the guard above drops) | inspection; no test |
| The two boundary checks and report suppression are untouched; the existing `UnassignedTest` and `UnlistedAssemblyTest` still pass unchanged | the existing tests, run unchanged |
| Output stays UTF-8 with CRLF for `-o` and `--json` | inspection; the write calls are not touched |
| `--untested-min-lines 0` lists every qualifying file and prints no closing sentence | inspection; the floor is a `>=` comparison and the count is zero |

## Tests

Three new cases in `tools/coverage-report/test_coverage_report.py`, in one new `UntestedFilesTest` class. The existing fixtures already build synthetic Cobertura documents (`cobertura`, `lines`, `Tree`), the existing `MAP` already has areas of both kinds - which of them classifies as a `gap` is only a matter of line counts - and `Tree.run` returns `(exit code, standard output)` with the report on standard output, so no new fixture machinery is needed. Run them with:

```
python -m unittest discover -s tools/coverage-report
```

That whole file runs in about 0.1 s, so run it rather than reasoning about it. **Do not collect coverage to land this.** A merged Cobertura collection is roughly 14 minutes of wall clock over three `dotnet test` commands, it would exceed the runner's idle timeout, and the rates would have moved since 2026-11-21 anyway. Develop against the synthetic fixtures; the published report gets refreshed by whichever pass next collects for its own reasons. There is no C# change here and nothing to build.

## The published report, and a contradiction to resolve

The plan ticket said both "when the tool prints it, that hand-written section goes" and "do not re-run the suite to land this". Those cannot both hold: without a collection there is no generated section to replace the hand-written one with, and deleting the hand-written list would leave the published report with no list of zeros at all, losing the investigation notes with it. The resolution, which is what to implement:

**Keep the hand-written section in `docs/coverage-report.md`.** Everything from *Line coverage per assembly* down is the 2026-11-21 generated half and this ticket does not regenerate it, so the hand-made list stands in as the published list until the next collection. Only the sentences that are about the *tool* go stale, and they are the edits:

- The last clause of the *What these numbers mean* paragraph, which today ends "and `debt-coverage-report-zero-files` is the ticket to make the tool print them": the tool now prints a `## Wholly untested files` section; say that, and say that the generated half below predates the change, so the hand-written list stands in until the next pass collects.
- The intro of `## Wholly untested files the area rates hide`, which today ends "and `debt-coverage-report-zero-files` exists so that the next run prints them without anyone having to remember to look": same correction, and add that when a later pass carries in a fresh generated half, this hand-written section is deleted and the generated `## Wholly untested files` replaces it, while the investigation paragraphs below the table - the `redefine` finding and the note that nine of the files are uninvestigated - are the part worth keeping.
- The row for this ticket in the tickets table under *Its review added two more* is a record of what that pass found and stays as it is. Do not rewrite the 2026-11-21 findings prose to the present tense; it is a dated record of a run.

`tools/coverage-report/README.md` describes the tool rather than a run, so it is corrected properly:

- The opening paragraph (`... then file by file for the areas with the biggest holes`) gains the new section.
- The options table gains a `--untested-min-lines` row, default 20.
- The bullet under *What the numbers mean, and what they do not* that reads "An area's rate is a weighted average of its files, so one large well-covered file can hide a wholly untested neighbour. That is what the per-file table under each gap is for." - the per-file gap table was never enough, which is this ticket. Rewrite it to point at the new section and record the scope: engine areas only, and a file that is excluded, unassigned or in a `reported` assembly is not in it.

No `docs/migration-notes.md` entry: this is a developer tool in `tools/`, with no public type, no D4 surface and nothing an application or a deployment script can observe. No `docs/architecture/` page either - the tool's own README is where it is documented.

## TODO

- Add `untested_files(analysis)` beside `classify` in `tools/coverage-report/coverage_report.py`: every `(path, Totals, Area)` across `analysis.areas` with `coverable > 0 and covered == 0`, sorted `(-coverable, path)`.
- Add `--untested-min-lines` (metavar `N`, type int, default 20) with a help line that distinguishes it from `--min-lines`; validate `>= 0` beside the existing `--min-lines` check; carry it as a fourth field on the `Options` namedtuple.
- Render `## Wholly untested files` between `## Engine areas` and `## Gaps, file by file`: the intro prose, the four-column table of files at or above the floor, the closing count sentence for those below it, or the positive sentence when the list is empty.
- Mention the new section in the report's own preamble, where the gap and thin thresholds are already explained, so a reader meets the rule before the table.
- Add `untested_min_lines` to the `thresholds` object in `to_json`.
- Add `UntestedFilesTest` to `tools/coverage-report/test_coverage_report.py` with the three cases and the assertions named under *Edge cases & interactions* above. Update `OPTIONS` and any other direct `coverage_report.Options(...)` construction for the new field.
- Run `python -m unittest discover -s tools/coverage-report` and confirm all tests pass, the four pre-existing boundary-check tests included.
- Update `tools/coverage-report/README.md`: opening paragraph, options table row, the weighted-average bullet.
- Update the two stale sentences in `docs/coverage-report.md` as described above. Leave the generated half and the dated findings alone.
- Preserve each file's existing line endings and byte-order-mark state. `coverage_report.py` and the test file are the files being changed; do not reformat anything else.
