description: The coverage report only lists individual files for the subsystems it already judges badly covered, so a source file that no test touches at all stays invisible as long as its neighbours are well covered - which is how a thousand lines of completely unexercised engine code went unreported.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tools/coverage-report/coverage_report.py, tools/coverage-report/areas.json, tools/coverage-report/test_coverage_report.py, docs/coverage-report.md
tradeoffs: The report already tells the truth about every area, and a maintainer may say that a file at 0 % inside a healthy area is usually a diagnostic helper or dead code rather than a coverage debt worth printing - against that, the first full run had eleven such files of 20 lines or more and two of the three largest turned out to be code the engine can no longer reach at all, which is exactly the kind of thing a coverage report exists to surface.
----
## What the tool does today

`tools/coverage-report/coverage_report.py` rolls the merged Cobertura data up per assembly and per engine area, sorts the areas worst-first, classifies each as `gap` (under 25 % with at least 200 coverable lines), `thin` or `covered`, and then prints a per-file breakdown **only for the areas classified `gap`**. Everything else is reported as a single rate per area.

That is a reasonable way to rank subsystems and a poor way to find untested code, because an area's rate is a weighted average. A file at exactly 0 % inside an area that averages 66 % appears nowhere in the report: not in the area table, which shows only the average, and not in a per-file table, because the area is not a gap.

## The evidence, from the first full run

Measured on the three-part run of 2026-11-21 at commit `f6200026` (`docs/coverage-report.md`): **21 source files in areas the report marks `covered` are themselves at 0 %, 1,300 coverable lines between them**, and the published report named none of them until its review added the list by hand. Eleven are 20 lines or more; the largest is 257.

Two of the three largest turned out, on investigation, not to be untested code at all:

- `src/engine/DAE/Runtime.Instructions/RedefineNode.cs` (257 lines) and `src/engine/DAE/Runtime.Data/RedefineTable.cs` (65) implement a D4 operator the compiler can no longer emit, because the branch that emits them sits behind a preprocessor symbol nobody defines. `debt-unreachable-redefine-nodes` carries that finding.
- `src/engine/Server/Connection/GenericDotNetConnection.cs` (111 lines), in a `thin` area rather than a covered one, is compiled and referenced by nothing at all; it is now an arm on `debt-ws12-hygiene-remainder`.

So the blind spot does not merely hide coverage debt - it hides **dead code**, which is the cheapest finding in the whole exercise and the one a report should hand over for free. The same pass did find three other dead files, but only because they happened to drag their own areas down far enough to be noticed.

## What the report should do instead

The rule to establish is that **a source file nothing executed is named, wherever it sits**. The area classification stays exactly as it is - it is a good ranking of subsystems and nothing here argues with it; what changes is that a zero is never averaged out of sight.

Shape, for whoever picks this up to settle:

- A section listing every file at 0 % coverable lines, across all areas, worst first by coverable count, with the area each belongs to and that area's rate - so a reader can see at once whether the file's neighbours are healthy.
- A floor, so the section is not twenty exception classes: the first run's distribution suggests somewhere around 20 lines, with the remainder reported as a single count. The existing `--min-lines` is a different thing (it stops a small *area* being called a gap) and should not be reused for this.
- The two existing boundary checks and the exit code are not involved: this is reporting, and the tool reports and never gates.
- `docs/coverage-report.md` carries a hand-written version of this list today, under *Wholly untested files the area rates hide*. When the tool prints it, that hand-written section goes, and the ticket's own run is the one that replaces it.

## Why this is the general fix and not five tickets

Three separate findings on the first full run - two dead files, one unreachable operator - were all the same mistake: a zero that no table printed. Printing the zeros catches the class once, for every future run, instead of depending on whoever reads the next report happening to query the JSON by hand. The per-file data is already in the analysis and already in `--json`; only the rendering withholds it.

## Interactions

- **The tool has its own unit tests**, `tools/coverage-report/test_coverage_report.py`, run with `python -m unittest discover -s tools/coverage-report`. The rendering rule is branching logic over the analysis, so it is the kind of thing those tests cover; the fixtures there already build small synthetic reports.
- **Do not re-run the suite to land this.** A merged Cobertura collection costs about 14 minutes of wall clock over three commands and the rates will have moved by then anyway. Develop against synthetic input in the unit tests; the published report gets refreshed by whichever pass next collects for its own reasons.
