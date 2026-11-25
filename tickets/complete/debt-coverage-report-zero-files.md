description: The coverage report now has a section naming every source file that no test touched at all, so a wholly untested file can no longer hide inside a well-covered part of the engine.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tools/coverage-report/coverage_report.py, tools/coverage-report/test_coverage_report.py, tools/coverage-report/README.md, docs/coverage-report.md, AGENTS.md
----
## What landed

The coverage tool printed a per-file table only under `## Gaps, file by file`, and only for areas it had already classified `gap`. An area's rate is a weighted average over its files, so a file at exactly 0 % sitting in an area that averages 66 % appeared in no table in the whole report. On the 2026-11-21 run that hid 21 files, 1,300 coverable lines between them, and a person had to add the list to the published report by hand.

The per-file numbers were already computed for every area and already written by `--json`; only the Markdown rendering withheld them. So the change is a rendering change plus one option, and no number in the rollup moved.

In `tools/coverage-report/coverage_report.py`:

- `untested_files(analysis)` beside `classify`: every `(path, Totals, Area)` across the areas with coverable lines and no covered line, worst first by coverable count, ties by path. A file with zero coverable lines is left out - its rate is `None`, and a row for it would read as a finding rather than as "nothing to measure".
- `_untested_files_section`, rendered between `## Engine areas` and `## Gaps, file by file`: intro prose, a four-column table (File / Coverable / Area / Area rate), a closing count for the files below the floor, or the positive sentence `No source file in an engine area is wholly untested.` when nothing qualifies.
- `--untested-min-lines N`, default 20, validated non-negative, carried as a fourth field on the `Options` namedtuple. Deliberately not `--min-lines`: that one asks whether a whole *area* is too small to call a gap, at a default an order of magnitude larger.
- A sentence in the report's own preamble distinguishing the two thresholds for a reader, and `untested_min_lines` in the `thresholds` object of `--json`. Nothing else in the JSON: every file's numbers are already under `areas[].files[]`, so a derived list would be a second copy of data a consumer can filter itself.

A file at 0 % inside a `gap` area is printed twice, once here and once in that gap's own table. That is the settled decision from the plan ticket: it makes the section's claim unconditional - *this is the complete list of files nothing executed* - with no "except the ones already below" caveat, and the area-rate column is what tells the two kinds of zero apart.

Documentation: `tools/coverage-report/README.md` names the new section in its opening paragraph, has a row for the option, and its *weighted average* bullet - which used to say the per-file gap table answered this, the defect itself - now states the section's scope (engine areas only). `docs/coverage-report.md` had its two sentences about the *tool* corrected; its 2026-11-21 findings prose and tickets table were left as they are, being the record of a run. No `docs/migration-notes.md` entry and no `docs/architecture/` page: a developer tool under `tools/` with no public type, no D4 surface and nothing an application or deployment script can observe.

The implement stage is commit `ticket(implement): debt-coverage-report-zero-files`. Review changed four things in that work, below.

## Review findings

The implement diff was read before the handoff. The rendering was then exercised by hand at five values of `--untested-min-lines` (default 20, 0, 1, 400 and a single-file-below-floor run) against synthetic Cobertura fixtures, and the selection was checked by reading `analyse`, `Area`, `classify` and `Totals` for the claims the new prose makes about scope and about what an area rate can hide.

### Checked and sound

- **The section renders as the handoff says it does**, byte for byte at the default threshold, including the deliberate double print of a zero inside a `gap` area and the omission of a file with no coverable lines.
- **Nothing gates.** A wholly untested file leaves the exit code at 0 and touches neither boundary check nor the report-suppression rule: the four pre-existing boundary cases (`UnassignedTest`, `UnlistedAssemblyTest`) pass unchanged, and the new cases assert the zero exit themselves.
- **No file can be listed twice.** `analyse` keys engine lines by path and `area_of` resolves one area per path by longest matching prefix, so the comprehension over `analysis.areas` cannot see one file under two areas however the map is written. The two-areas-one-path case the map forbids is already rejected at load with exit 2.
- **No listed row can carry a `-` area rate.** A row exists only because its area holds a file with coverable lines, so that area's own `coverable` is non-zero and its rate is never `None` - the `_rate` fallback is unreachable from this table, not a latent blank.
- **`Options` grew a field and nothing else constructs it.** The only two constructions in the repository are `main` and the tests' `OPTIONS`; both were updated.
- **Ordering is deterministic.** Two equal-sized files sort by path, so two runs of one build render identically - which matters because this output is committed to `docs/`.
- **Source hygiene.** CRLF and no byte-order mark preserved in both Python files and in `AGENTS.md`; LF in the ticket files. `coverage_report.py` is 737 lines, up 9 net from the implement commit, and the module is still one cohesive thing - parse, merge, analyse, render - with the new code sitting beside the function it mirrors in each half. Nothing reformatted or re-encoded.
- **No regeneration of `docs/coverage-report.md`**, as the handoff asks. Collecting coverage is roughly 14 minutes over three `dotnet test` commands, past the runner's idle limit, and the rates have moved since 2026-11-21 anyway; whichever later pass collects for its own reasons picks the new section up.

### Fixed in this pass

- **The report described a floor it was not applying.** At `--untested-min-lines 0` the section intro correctly dropped its "Files of fewer than 0 coverable lines are counted at the end" sentence, but the report preamble still said the section names files "down to 0 coverable lines" - which the tool does not do and must not claim, since a file with no coverable line is deliberately never listed. At `1` both sentences printed, the second reading "fewer than 1 coverable lines". The predicate is not "a floor was given" but "the floor can hold a file back", which is `> 1` because a file needs a coverable line to be a candidate at all; that is now one named helper, `_floor_can_hold_a_file_back`, consulted at both sites, with the reason in its docstring. The handoff listed `--untested-min-lines 0` as a supported use case and had handled it in one of the two places, so this is the other half of that case.
- **The intro promised a table that was not there.** With a floor above every candidate (`--untested-min-lines 400` on the probe run) no table prints, and the intro still explained what each row's columns mean. The intro is now composed from the sentences that are true of the run - the column sentence only when a table follows - which also removed the `%%` escape the old single-format-string version needed.
- **`AGENTS.md` still described the old report.** Its *Coverage, on demand* paragraph lists what the tool prints ("per assembly and per engine area, worst first, with the files of each gap") and was not updated, so the one place an agent reads about this tool without opening it did not mention the new section. Corrected. The tool's README and `docs/coverage-report.md` were already handled by the implement stage; `docs/README.md` only links the published report and needed nothing.
- **A readability nit at the same site**, taken while the lines were open: the two list comprehensions that split listed files at the floor indexed `item[1]` where the loop below unpacks `path, totals, area`, and the preamble's conditional read as a double negative. Both now read positively.

### Filed as a ticket

- **Nothing runs this tool's tests.** `tools/coverage-report/test_coverage_report.py` has 14 cases and no workflow runs any of them: `build.yml` is .NET only, and `docs.yml` runs the architecture-page size check plus that one tool's tests. Three of the four Python tools under `tools/` are in that position. Pre-existing - the tool and its tests predate this ticket - but this change added cases to an ungated file, so it is the review that met it. Filed as `debt-tool-tests-not-run-by-ci` (backlog, `difficulty: easy`), written as the invariant rather than as "add this tool to a list": a step that discovers the test folders keeps the rule true for a tool added later. No open ticket claimed any of those paths (grepped). It is not the same thing as `docs-link-check-ci`, which puts the *link checker* into CI rather than that checker's unit tests; the ticket says so.

### Tripwires

None. The honest reason, rather than silence: the one performance question here - `untested_files` walks every area's file list on every render - is not conditional on anything in the future. It is one pass over the few thousand files a run produces inside a tool that already parses tens of megabytes of XML, so there is no condition under which it becomes work, and a `NOTE:` saying so would be noise. Nothing else in the change has a "fine now, matters if X" shape.

### Considered and left alone

- **`untested_files` returns the `Area` object** as its third element, which the handoff flags as coupling where a `(name, rate)` pair would do. Left: the renderer wants `area.name` and `area.totals` so it can format the rate with the existing `_rate`, both halves live in this one module, and a pair would duplicate the rate formatting at the selection site. The signature is the plan ticket's.
- **The four-sentence intro**, which the handoff explicitly put to the reviewer as a judgment call. Left as written. Each sentence answers a different question a cold reader of a published report has - what the section is, why it exists at all when the area table is right above it, how to read the two kinds of zero, and what the floor does - and the two that can be untrue of a run are now conditional, which was the real problem with its length. Trimming the rest would be churn against wording the plan ticket settled.
- **The deliberate double print** of a zero in a `gap` area. The plan ticket weighed it and the section's unconditional claim is worth the repetition; a test pins the count at exactly 2, so a later reader who disagrees will find the decision rather than a bug.

## Tests

One case added, none cut. `test_a_floor_that_holds_nothing_back_is_not_described_as_a_rule` is the reproduction of the first fix above - the project's rule is that a fix owes its reproduction - and it was confirmed to fail against the implement commit's `coverage_report.py` (`AssertionError: 'down to 0 coverable lines' unexpectedly found`) and to pass after. It asserts the three things that must hold together at a floor of zero: the smallest file is listed, no sentence anywhere in the report promises a floor, and a file with no coverable line is still not a finding.

The implementer's three cases were read as code under review and all three kept: each pins a contract rather than restating the implementation - the defect itself (a zero in a `covered` area is listed, with its area and rate), the floor boundary at exactly 20, and the empty case's positive sentence, which is the one that keeps the section from going silent the way the old report did. The `named`-empty case the second fix addresses is covered by the hand probe rather than by a fourth prose test: it needs a floor above every file in the run, a value no one passes, and a test of it would pin wording rather than behaviour.

Not tested, and why: that an area with a `None` rate cannot own a listed row (unreachable, as recorded above - the coverable guard drops its only possible files), the `-1` validation (argparse-level, mirroring the untested `--min-lines` check beside it), and that the `-o`/`--json` writes stay UTF-8 with CRLF (those calls are untouched).

## Validation run

- `dotnet build Dataphor.slnx` - 0 errors, 143 pre-existing warnings, 27 s. No C# changed; run because the project rule says the build is never left for CI.
- `python -m unittest discover -s tools/coverage-report` - 14 cases, all pass, 0.1 s.
- `python tools/coverage-report/coverage_report.py --help` - two distinct help lines for `--min-lines` and `--untested-min-lines`; `--untested-min-lines -1` exits 2 with `--untested-min-lines cannot be negative`.
- The rendering by hand at five threshold values, as described above.
- `python tools/docs-size-check/check_size.py` - 13 files, 0 over the limit. `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` - 0 errors, 203 pending (the converted reference pages `docs-remove-old-sources` owns; unchanged by this ticket, and the `--pending` list is why they are not errors).
- No .NET test command: nothing under `src/` or `tests/` is touched by either stage of this ticket, so no engine test could tell this change from its absence. The one ring wider the review stage normally runs has no ring here.
