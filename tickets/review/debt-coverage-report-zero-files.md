description: The coverage report now has a section naming every source file that no test touched at all, so a wholly untested file can no longer hide inside a well-covered part of the engine; review the new section and its option.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tools/coverage-report/coverage_report.py, tools/coverage-report/test_coverage_report.py, tools/coverage-report/README.md, docs/coverage-report.md
----
## What landed

The coverage tool printed a per-file table only under `## Gaps, file by file`, and only for areas it had already classified `gap`. An area's rate is a weighted average over its files, so a file at exactly 0 % sitting in an area that averages 66 % appeared in **no** table in the whole report. On the 2026-11-21 run that hid 21 files, 1,300 coverable lines between them, and a human had to add the list to the published report by hand.

The per-file numbers were already computed for every area and already emitted by `--json`; only the Markdown rendering withheld them. So this is a rendering change plus one option, and no number in the rollup moved.

Five changes in `tools/coverage-report/coverage_report.py`:

- `untested_files(analysis)`, beside `classify`: every `(path, Totals, Area)` across `analysis.areas` with coverable lines and no covered line, sorted worst first by coverable count, ties by path ascending. A file with **zero** coverable lines is left out - its rate is `None`, and a row for it would read as a finding rather than as "nothing to measure".
- `_untested_files_section`, rendered between `## Engine areas` and `## Gaps, file by file`: intro prose, a four-column table (File / Coverable / Area / Area rate), a closing count for the files below the floor, or the positive sentence `No source file in an engine area is wholly untested.` when nothing qualifies.
- `--untested-min-lines N`, default 20, validated `>= 0`, carried as a fourth field on the `Options` namedtuple. Deliberately **not** `--min-lines`: that one asks whether a whole *area* is too small to call a gap, at a default an order of magnitude larger.
- A sentence in the report's own preamble, where the gap and thin thresholds are already explained, that distinguishes the two thresholds for a reader.
- `untested_min_lines` added to the `thresholds` object in `to_json`, and nothing else - every file's numbers are already under `areas[].files[]`, so a derived list would be a second copy of data a consumer can filter itself.

## What a reviewer should check first

**The section renders as specified.** Here it is, from a synthetic run (`Debugger` is 250 lines at 0 % and so a `gap`; `Schema` averages 59.9 % and so is `covered`; `Tiny.cs` is 7 lines, under the floor; `Empty.cs` has no coverable lines at all):

```
## Wholly untested files

Every source file in an engine area that the run never entered, wherever it sits. An area's rate above is a weighted average over its files, so a file at 0 % inside a well-covered area appears in no other table here. Each row names the file's area and that area's rate: a zero inside an area already marked `gap` is expected, and a zero inside a `covered` one is the finding this section exists for. Files of fewer than 20 coverable lines are counted at the end rather than listed.

| File | Coverable | Area | Area rate |
|---|---:|---|---:|
| src/engine/DAE/Debug/Debugger.cs | 250 | Debugger | 0.0 % |
| src/engine/DAE/Schema/Order.cs | 30 | Schema | 59.9 % |
| src/engine/DAE/Schema/Plan.cs | 30 | Schema | 59.9 % |

One further wholly untested file, of 7 coverable lines, is below the 20-line floor and is not listed.
```

**Nothing gates.** A wholly untested file must not change the exit code and must not touch either boundary check or the report-suppression rule. The three new tests all assert `code == 0`, and the four pre-existing boundary tests (`UnassignedTest`, `UnlistedAssemblyTest`) pass unchanged.

**The deliberate double print.** A file at 0 % inside a `gap` area is printed twice - once in this section, once in that gap's own per-file table. That is the settled decision in the plan ticket, not an oversight: it makes the section's claim unconditional (*this is the complete list of files nothing executed*) with no "except the ones already below" caveat, and the area-rate column is what tells the two kinds of zero apart. A test asserts the count is exactly 2.

**Scope, which the README states and the rendered report does not.** `analyse` accumulates per-file lines only for assemblies on the `engine` list, so a file in a `reported` assembly, under an `excluded` path, or under `src/` that no area claims cannot appear in this section - the last case is what the `unassigned` boundary check is for. A reviewer tempted to widen the claim in the rendered prose should check that constraint in `analyse` first.

## Use cases to exercise by hand

Everything below runs against the synthetic fixtures in about a tenth of a second; no coverage collection is needed, and none was done (see *Deliberately not done*).

- `python -m unittest discover -s tools/coverage-report` - 13 tests, all pass.
- `python tools/coverage-report/coverage_report.py --help` - two distinct help lines for `--min-lines` and `--untested-min-lines`.
- `--untested-min-lines 0` - every qualifying file listed, no closing sentence, and the intro's floor sentence suppressed rather than reading "fewer than 0". Verified by hand, not by a test.
- `--untested-min-lines -1` - exits 2 with `--untested-min-lines cannot be negative`. Verified by hand.
- A run where every file has at least one hit line - the heading still prints, with the positive sentence.

## Tests added

Three cases in a new `UntestedFilesTest` in `tools/coverage-report/test_coverage_report.py`, all through `main()` because the defect was in the rendering, not in the analysis:

| Test | What it pins |
|---|---|
| `test_a_zero_inside_a_covered_area_is_listed_with_its_area` | the defect itself: a 0 % file in a `covered` area is listed, with its area name and rate. Also, in the same case: a fully covered file is never listed; a file with zero coverable lines is never listed; the two equal-sized files come out in path order so two runs of one build render identically; a 0 % file in a `gap` area appears exactly twice in the report; the exit code is still 0 |
| `test_the_floor_lists_a_file_at_it_and_counts_the_ones_below` | the boundary: exactly 20 coverable lines is listed, 19 and 5 are not, and the closing sentence carries the right file count (2) and line total (24) |
| `test_the_section_prints_a_positive_line_when_nothing_qualifies` | the empty case prints the heading and `No source file in an engine area is wholly untested.`, so a reader can tell "nothing to report" from "not reported here" |

`OPTIONS` in that file gained the fourth field; it was the only direct `Options(...)` construction in the tests.

Not tested, by the plan ticket's own table: that an area whose rate is `None` cannot own a listed row (the coverable guard drops its only possible files), that the `-o`/`--json` writes stay UTF-8 with CRLF (those calls are untouched), and the `--untested-min-lines 0` case above.

## Documentation

- `tools/coverage-report/README.md`: the opening paragraph names the new section; the options table gained a `--untested-min-lines` row; the *weighted average* bullet under *What the numbers mean* was rewritten - it used to say the per-file gap table answered this, which was the defect - and now states the section's scope (engine areas only).
- `docs/coverage-report.md`: only the two sentences that are about the *tool* were corrected. Both said this ticket was still outstanding; they now say the tool prints the section, that the generated half of that page predates the change and was not re-collected, and that the hand-written list stands in until a later pass collects. The second one also says to delete the hand-written section when a fresh generated half arrives, and that the investigation paragraphs under it are the part worth keeping. The dated 2026-11-21 findings prose and the tickets table were left exactly as they are: that is a record of a run.
- No `docs/migration-notes.md` entry and no `docs/architecture/` page: this is a developer tool under `tools/`, with no public type, no D4 surface and nothing an application or deployment script can observe. The tool's own README is where it is documented.

## Deliberately not done, and why

- **No coverage was collected.** A merged Cobertura collection is roughly 14 minutes of wall clock over three `dotnet test` commands - past the runner's 10-minute idle timeout - and the rates would have moved since 2026-11-21 anyway. The published `docs/coverage-report.md` therefore still carries its 2026-11-21 generated half and its hand-written list of zeros. **Do not regenerate it in review either**; whichever later pass collects for its own reasons picks this up.
- **No build, no C# change.** Nothing in `src/` or `tests/` was touched, so `dotnet build Dataphor.slnx` and the .NET test suite are not in this ticket's scope. The only validation that applies is the Python unit test file.
- **No tripwires and no new tickets.** Nothing conditional surfaced; the one contradiction in the plan ticket (print the section *and* do not re-collect) was resolved in the ticket itself and the resolution is what landed.

## Honest gaps

- The intro prose and the closing sentence are long, and their wording is from the plan ticket. If a reviewer thinks the four-sentence intro is more than a reader needs above a short table, that is a judgment call worth making now rather than after the next collection bakes it into a published page.
- The closing sentence has two grammatical branches (one file, several files). The several-files branch is covered by a test; the single-file branch was only read and eyeballed in the smoke run above.
- `untested_files` returns the `Area` object itself as the third element, so the renderer reads `area.name` and `area.totals` off it. That is the plan ticket's signature. It couples the selection to the `Area` class where a `(name, rate)` pair would not, but it keeps the function one comprehension and the renderer able to format the rate with the existing `_rate`.
