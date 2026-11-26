# Build and test

The short version, for someone who has just cloned the repository. The *Build and test* section of [AGENTS.md](../../AGENTS.md#build-and-test) is the fuller reference: which tests to run at which point in a piece of work, what each part of the suite covers, and the conventions a newly converted project follows. Where this page needs that detail it links there instead of copying it.

## The three commands

```
dotnet build Dataphor.slnx
dotnet build Dataphor.slnx -c Release
dotnet test Dataphor.slnx
```

What they cost:

- **The build** takes well under a minute once the tree has been built before — measured at 25 seconds on a mid-range machine on 2026-11-26. The first build also restores packages, so it takes noticeably longer. The build is expected to be clean: it produces warnings, deliberately (several document real .NET 10 behaviour changes and are not suppressed), but no errors.
- **The whole test suite** takes from about 3.5 minutes on a fast machine to over 11 on a slow one (measured 2026-11-18), most of it in the D4 coverage scripts. Add `--no-build` after a build, and `-c Release` to test the Release build.

Timings on this suite vary a lot between identical runs. Take the best of two before concluding that something got slower.

## Running part of the suite

Eleven minutes is too long to wait for the answer to a small question, so run the part that covers what you changed:

```
dotnet test tests/DAE.Server.Tests --filter Name=Lexer
dotnet test tests/DAE.Server.Tests --filter "Name=Lexer|Name=Transaction.Atomicity.Run" --logger "console;verbosity=normal"
```

`--filter` is VSTest's own expression language, over test and class names. NUnit's richer selection is available by passing `NUnit.Where=…` after a bare `--`, which is how the suite is split for a machine that cannot spare eleven minutes:

```
dotnet test tests/DAE.Server.Tests --no-build -- "NUnit.Where=class =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.C'"
```

AGENTS.md carries a set of five such commands that together run the whole suite in parts, each short enough to watch, and explains why they partition it rather than overlapping. Per-test detail — the per-ticket summary of the coverage scripts, and the error text of each recorded known failure — prints only with `--logger "console;verbosity=normal"`.

## The runtime layout

The build does more than compile. It also assembles `artifacts/layout/<debug|release>/`: a complete server, with `bin/` holding the engine, server and host assemblies and `Libraries/<Name>/` holding each D4 library's files beside its assembly. The shipped, sample and test libraries all land in that one `Libraries/` folder, which is the shape the server expects.

One assembled copy serves everything that needs a working installation: the tests start their servers from it, a local run of the host starts from it, and the installer will be built from it. That is the point — a test and a release both see the same files.

Two rules about it:

- **`tools/layout/Layout.csproj` is the only thing that writes there.** It mirrors the folder, deleting anything it did not put there, and it fails the build if a library's `.d4l` descriptor lists a file the layout cannot provide. Do not add files to the layout by hand; add them to the project.
- **Never run a server against the layout itself.** A running server rewrites `.d4l` files and creates folders in its library directory. Copy `Libraries/` (or just the libraries you need) and point the server at the copy, as the tests do.

Find the folder through MSBuild rather than spelling the path:

```
dotnet msbuild tools/layout/Layout.csproj -getProperty:DataphorLayoutDir
```

Starting a server from a copy of it, and the two environment variables that keep a local run out of the machine's instance list, are covered under *Runtime layout* in [AGENTS.md](../../AGENTS.md#build-and-test).

## Environment variables

Three variables change which tests a run includes. All three are read in one place, so nothing else parses them.

| Variable | What it does |
|---|---|
| `DATAPHOR_TEST_LOGGING=1` | Diagnoses a failure. The test servers then log, and every temporary server folder is kept with its instance log, catalog and library copies instead of being deleted; their paths print with `--logger "console;verbosity=normal"`. The next run without the variable deletes them. The cost: in an elevated process a logging server also creates the Windows event-log source `Dataphor Server` and writes to the event log, which a default run never does. |
| `DATAPHOR_TEST_MSSQL` | Names a **SQL Server** — the server, not a database. For example `Data Source=(localdb)\MSSQLLocalDB;Integrated Security=SSPI`, after `sqllocaldb start MSSQLLocalDB`. Turns on the SQL Server catalog-store contract, the device's parameter-description pass, three transaction-atomicity coverage scripts, the live device pass, the version-matrix pass and three sample libraries. |
| `DATAPHOR_TEST_PGSQL` | The same for a **PostgreSQL** server, for example `Host=localhost;Port=5432;Username=postgres;Password=…`. Turns on the PostgreSQL store and device passes. |

**A pass that needs a server it was not given reports itself skipped, with the reason — it does not pass quietly.** That is deliberate: a green run with no variables set is a green run of a smaller suite, and the run says which cases it left out. The same is true in continuous integration, which starts a local SQL Server and PostgreSQL but stays green, with those cases skipped, if a runner image has dropped one.

AGENTS.md lists, case by case, exactly what each variable turns on and what each of those cases needs.

### Rewriting the checked-in comparison files

Three passes generate something from a running server and compare it, character for character, with a file checked into `tests/goldens/`: the derived forms, the SQL a device would send, and the D4 declarations of the System library. Each has a variable that **rewrites** its files instead of comparing them, deletes the ones nothing produces any more, and then fails the run anyway, so that an update can never be mistaken for a passing run.

| Variable | Which files |
|---|---|
| `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` | `tests/goldens/frontend/` — a derived form per page type per table variable. |
| `DATAPHOR_UPDATE_DEVICE_GOLDENS=1` | `tests/goldens/devices/` — the SQL each device generates for the shared translation corpus. |
| `DATAPHOR_UPDATE_REFERENCE_GOLDENS=1` | `tests/goldens/reference/` — the declaration of every System operator and scalar type, and the inventory of operator names. |

**The diff is the review.** Read it before committing it: these files are the only record of what the product used to produce, and there is no reference runtime to regenerate one from if it is overwritten by mistake.

## Two things that are run by hand

Neither is part of `dotnet test`; both are compiled by every build, so neither can rot unnoticed.

- **The coverage report.** Add `--collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/<part>` to any test command, then run `python tools/coverage-report/coverage_report.py artifacts/coverage`. It prints line coverage per assembly and per engine area, worst first, then the source files the run never entered. It reports and never gates, and continuous integration deliberately does not collect it. The last full measurement is written up in [docs/coverage-report.md](../coverage-report.md); the tool's own [README](../../tools/coverage-report/README.md) explains its output.
- **The benchmark program.** `artifacts/bin/DAE.Benchmarks/release/DAE.Benchmarks.exe` measures the three costs a D4 script inside an already-running server cannot reach: engine start, a representative mix of database work, and the catalog store cold and warm. It takes about 100 seconds and must be run in Release — it says so on standard error otherwise. Its numbers, and the timing scripts' numbers that the test suite records on every run, are in [docs/perf-baseline.md](../perf-baseline.md); the program's own [README](../../tests/Dataphor.Benchmarks/README.md) says what each benchmark does.

## What continuous integration runs

[`.github/workflows/build.yml`](../../.github/workflows/build.yml) runs the same two commands — `dotnet build Dataphor.slnx` and `dotnet test Dataphor.slnx` — in both Debug and Release, on Windows, on every push and pull request that changes more than `tickets/` or `docs/`. It uploads the test results as artifacts. So the full suite is covered on every push whether or not you ran it locally; what a local run buys you is finding a break before the push rather than after.

Change the workflow when you change these commands.
