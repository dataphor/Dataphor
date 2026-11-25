description: Three of the four small Python helper programs in this repository have unit tests that nothing runs automatically, so a change that breaks one of those helpers is only discovered the next time a person happens to use it.
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: .github/workflows/docs.yml, .github/workflows/build.yml, tools/coverage-report/test_coverage_report.py, tools/docs-link-check/test_check_links.py, tools/docs-convert/test_convert.py, tools/docs-size-check/test_check_size.py
difficulty: easy
tradeoffs: Nothing here ships to a user and every one of these helpers is run by hand, so a break announces itself the next time someone runs it; a maintainer may reasonably say a workflow step to guard four developer scripts is not worth the minute it adds to every push.
----
## What is missing

Four small standard-library Python programs live under `tools/`, and each one has a unit-test file beside it:

| Tool | Its tests | Run automatically today |
|---|---|---|
| `tools/docs-size-check/` | `test_check_size.py` | yes, by `.github/workflows/docs.yml` |
| `tools/coverage-report/` | `test_coverage_report.py` (14 cases) | no |
| `tools/docs-link-check/` | `test_check_links.py` | no |
| `tools/docs-convert/` | `test_convert.py` | no |

The two automated-build workflows are `build.yml`, which restores, builds and tests the .NET solution and has no Python step at all, and `docs.yml`, which runs the architecture-page size check and that one checker's own tests. So three of the four test files are run only when a person remembers to, which is the state the fourth was in before `docs.yml` existed.

That is the whole finding: the guard exists, it is written, and nothing pulls the trigger.

## Why it is worth closing

The rule worth having is **a tool with tests beside it has those tests run on every push** - not "add the coverage tool to a list", which is how the current one-tool shape came about and how the next tool will be forgotten in turn. A step that discovers the test files rather than naming them keeps the rule true for a tool added later without anyone editing a workflow again.

Found by the review of `debt-coverage-report-zero-files`, which added three cases to `test_coverage_report.py` - a file whose 14 cases nothing gates - and then fixed a defect in the same tool that those cases, had they been running, would not have caught either, because the defect was in report prose under a non-default option. That is the honest size of the prize: these tests guard developer helpers, not the product.

## Shape of the work

Belongs in `docs.yml` rather than `build.yml`: that workflow already has the Python step, needs no .NET toolchain and is quick, while `build.yml` deliberately ignores `docs/**` and would be skipped for exactly some of the changes worth checking. Renaming the workflow is part of the decision - it would no longer be only about documentation.

Two details whoever does this will meet:

- **The path filters.** `docs.yml` triggers on `docs/**`, `tools/docs-size-check/**` and its own file. Covering the other three tools means adding their paths, and the inclusion-filter warning already written into both workflow files applies: a push touching none of the listed paths gets no run at all.
- **Discovery, not a list.** `python3 -m unittest discover` run once per tool folder, over the folders found rather than over folders spelled out, so that a fifth tool is covered the day its tests land. The tests import their module by name from the folder they sit in, so each folder is its own discovery root.

## What this ticket is not

`docs-link-check-ci` (an arm of `feat-ws8-documentation-remainder`) puts the *link checker itself* into CI - running it over `docs/` to gate the links. That is a different thing from running that checker's unit tests, and the two can land in either order. Neither one makes the other unnecessary.
