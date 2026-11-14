description: Review the new automatic build-and-test run on GitHub, which builds and tests the solution in Debug and Release for every push and pull request and keeps the test results.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: .github/workflows/build.yml, AGENTS.md, global.json, Directory.Build.props
difficulty: easy
----
# Continuous integration: build and test on GitHub Actions (review handoff)

## What landed

- `.github/workflows/build.yml` (new, LF, spaces, as `.editorconfig` asks for `.yml`). One job, `build`, on `windows-latest`, matrix `configuration: [Debug, Release]` with `fail-fast: false`, `timeout-minutes: 60`.
  - Triggers: `push` and `pull_request` on `master` and `modernization`, both with `paths-ignore: tickets/**, docs/**`, plus `workflow_dispatch`. A `NOTE:` comment records the branch-protection consequence of the path filter (a required check never runs for a ticket-only or docs-only pull request).
  - `permissions: contents: read`; `pull_request` only (a comment says never `pull_request_target`); no secrets.
  - `concurrency` grouped by workflow and ref, `cancel-in-progress: true`.
  - `env`: `DOTNET_NOLOGO`, `DOTNET_CLI_TELEMETRY_OPTOUT`. `ContinuousIntegrationBuild` is not set (`Directory.Build.props` sets it from `GITHUB_ACTIONS`).
  - Actions pinned by full commit SHA, tag in a trailing comment. Looked up on 2026-11-14 with `git ls-remote` (all four are lightweight tags, so the tag's SHA is the commit): checkout v7.0.1 `3d3c42e5…`, setup-dotnet v6.1.0 `0f4a354d…`, cache v6.1.0 `55cc8345…`, upload-artifact v7.0.2 `cf430e03…`. setup-dotnet v6.1.0 was released 2026-11-03, eleven days ago; the reviewer may prefer an older release if a very recent pin is a concern.
  - `setup-dotnet` with `global-json-file: global.json`; NuGet cache at `~/.nuget/packages`, key `nuget-<os>-hashFiles(Directory.Packages.props, global.json, **/*.csproj)` with an OS-only restore key. The `**/*.csproj` glob also matches legacy and parked project files; harmless, it only causes a cache miss when one of them changes.
  - Steps: `dotnet --info`; `dotnet restore Dataphor.slnx`; `dotnet build Dataphor.slnx -c <cfg> --no-restore -p:DataphorBuildNumber=${{ github.run_number }}` (the property exists, from `build-version-and-signing`); `dotnet test Dataphor.slnx -c <cfg> --no-build --logger trx --results-directory artifacts/TestResults/<cfg>`; `upload-artifact` with `if: always()`, name `test-results-<cfg>`, path `artifacts/TestResults/`, `if-no-files-found: ignore`, `retention-days: 14`.
  - No `-warnaserror`, no warning suppression.
- `AGENTS.md`, "Build and test": one paragraph after the command block naming the workflow, the configurations, the triggers and the artifact names.

## Verification done

- `actionlint` 1.7.12 (release binary downloaded to the session scratchpad, nothing added to the repo): no findings.
- Locally on Windows, the workflow's exact commands: restore, then build Debug and Release with `-p:DataphorBuildNumber=7`: 0 errors (141 warnings, the existing set). `dotnet test … --no-build --logger trx --results-directory artifacts/TestResults/<cfg>` in both configurations: Common.Tests 12 passed; DAE.Server.Tests 94 passed, 46 skipped (the manifest's known-failure and unstable coverage scripts), 0 failed; one `.trx` per test project written to the results folder (deleted afterwards). The local builds were incremental (already up to date), so the cold-build time on the runner is not measured.
- Not done (the agent does not push): a real run on GitHub. After a human pushes, confirm one green run per configuration with both TRX files in each artifact, and record the run link in the complete ticket.

## Things for the reviewer to check

- On the runner, the coverage template waits about 25 s for a SQL Server on `localhost` (K14). `windows-latest` is not expected to have a default SQL Server instance listening, so the 25 helper-library-dependent scripts should fail in their recorded way, as they do locally. If the image ever accepts that connection, those scripts fail the run as "now passes" (AGENTS.md describes this); `test-decouple-coverage-base` removes the dependency. Not verified until the first run.
- TRX file names contain the runner's user and machine name; nothing else machine-specific is printed, and no connection string is supplied.
- No test was added: this is CI configuration (the ticket says no automated test).

## Migration notes

None.
