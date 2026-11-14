description: Add an automatic build-and-test run on GitHub for every push and pull request, so a change that breaks the build or the tests is seen immediately rather than by the next person who builds.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: .github/workflows/build.yml, global.json, Dataphor.slnx, Directory.Build.props, AGENTS.md
difficulty: easy
----
# Continuous integration: build and test on GitHub Actions

## Goal

The repository has no continuous integration. After `test-harness-inprocess`, one command builds everything that is converted and one command runs the tests. This ticket runs both on a GitHub-hosted Windows runner for every push and pull request and publishes the test results, so gate G1 ("from a clean clone, `dotnet build` and `dotnet test` pass") is checked on every change instead of by hand. The owner controls the repository and agreed to CI (plan Q10).

**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout from [Repository layout](../../docs/modernization-plan.md#repository-layout).

## Scope

In scope: one workflow file, `.github/workflows/build.yml`, with a single job that restores, builds `Dataphor.slnx`, runs the tests and uploads the results; one line in the "Build and test" section of `AGENTS.md` saying that CI runs the same commands.

Out of scope, each owned by a later ticket that adds its own job or workflow: the documentation link checker (`docs-link-check-ci`, in `feat-ws8-documentation-remainder`); the package vulnerability audit (`security-dependency-audit`, in `feat-ws11-security`); live SQL Server tests on LocalDB (`test-device-live`, in `feat-ws3-test-layers` and `feat-ws7-devices-and-libraries`); building and publishing the MSI and the release workflow (`installer-msi`, `release-checklist-and-changelog`, in `feat-ws9-installer-and-release`); UI smoke tests on an interactive session (`test-ui-smoke`, in `feat-ws3-test-layers`). No SQL Server, no secrets and no deployment here.

## Design (settled)

- **Runner:** `windows-latest`. The product targets Windows (D1, D10), the UI projects will need `net10.0-windows`, and NLS globalization (D2) exists only on Windows.
- **Triggers:** `push` and `pull_request` for the branches `master` and `modernization` (the current work branch), plus `workflow_dispatch`. Use `paths-ignore` for `tickets/**` and `docs/**`, because the ticket runner commits ticket moves on the work branch and those commits change no code. (If branch protection later requires this check, a pull request that touches only ignored paths will wait for a check that never runs; note this in a comment in the workflow.)
- **Permissions:** `permissions: contents: read` at workflow level. Use `pull_request`, never `pull_request_target`: the repository is public, and pull requests from forks must run with a read-only token and no secrets. The workflow needs no secrets.
- **Concurrency:** `concurrency: group: ${{ github.workflow }}-${{ github.ref }}`, `cancel-in-progress: true`.
- **Actions:** `actions/checkout`, `actions/setup-dotnet` (with `global-json-file: global.json`, so the SDK is the one the repository pins: `10.0.100` with `rollForward: latestFeature`), `actions/cache` for `~/.nuget/packages` keyed on the hash of `Directory.Packages.props`, `global.json` and the project files, and `actions/upload-artifact`. Pin each action to a full commit SHA with the version tag in a trailing comment; for a public repository this stops a moved tag from changing what runs.
- **Matrix:** `configuration: [Debug, Release]`, `fail-fast: false`. Code under `#if DEBUG` and `[Conditional("DEBUG")]` differs between the two (K4 was a Debug-only failure), and the engine constants chosen by `build-core-sdk-projects` may differ by configuration.
- **Steps**, using exactly the commands that the "Build and test" section of `AGENTS.md` documents after `test-harness-inprocess` (that section is the source of truth; if it names a different test runner mode or arguments, follow it):
  - `dotnet --info` (records the SDK actually used);
  - `dotnet restore Dataphor.slnx`;
  - `dotnet build Dataphor.slnx -c ${{ matrix.configuration }} --no-restore`, adding `-p:DataphorBuildNumber=${{ github.run_number }}` if `build-version-and-signing` defined that property;
  - `dotnet test Dataphor.slnx -c ${{ matrix.configuration }} --no-build` with a TRX logger writing to `artifacts/TestResults/${{ matrix.configuration }}`;
  - `actions/upload-artifact` with `if: always()`, name `test-results-${{ matrix.configuration }}`, path `artifacts/TestResults/`, `retention-days: 14`.
- **Environment:** `DOTNET_NOLOGO: true`, `DOTNET_CLI_TELEMETRY_OPTOUT: true`. `ContinuousIntegrationBuild` is switched on by `Directory.Build.props` when `GITHUB_ACTIONS` is `true` (set by `build-version-and-signing`); do not set it again here.
- **Job timeout:** `timeout-minutes: 60`.
- Do not add `-warnaserror`, and do not suppress any warning in the workflow: `WFDEV006`, `SYSLIB` and the NuGet vulnerability warnings must stay visible in the log (AGENTS.md).

## Edge cases & interactions

- **Known-failure scripts.** The harness's manifest marks the coverage scripts that fail today as `known-failure` with their expected error; the run is green only if each fails in the expected way. CI adds nothing to that logic. Before merging, confirm by inspection that the test command exits 0 at the current commit on a local Windows machine; if it does not, the workflow is not the place to fix it (report through `tickets/.pre-existing-error.md` as the ticket rules describe).
- **Tests that need a SQL Server.** None should run here; the harness skips them visibly (reported, counted) when no connection string is supplied by environment variable. Verify by reading the uploaded TRX of the first run: skipped live tests appear as skipped or inconclusive, not as failures and not silently absent.
- **Isolation on a shared runner.** The harness uses temporary instance directories and dynamically allocated ports and never touches `%ProgramData%` (plan WS3 cross-cutting requirements). Nothing in the workflow sets an instance directory. Verified by inspection of the harness configuration.
- **SDK drift.** The runner image has its own preinstalled SDKs; `setup-dotnet` with `global-json-file` installs the pinned one, and `global.json` makes `dotnet` select it. The `dotnet --info` step shows which SDK ran.
- **Line endings.** Do not add a renormalization or `core.autocrlf` step; the checkout uses the repository's `.gitattributes` (from `hygiene-gitattributes-editorconfig`). C# files are CRLF in the repository and D4 scripts are read as text; a checkout that changed line endings could change source offsets reported by the engine.
- **Failure paths.** A failed build skips the test step; the upload still runs (`if: always()`) and finds no results, which is acceptable (set `if-no-files-found: ignore`). A failed test run uploads its TRX.
- **Cancelled runs.** A superseded run on the same ref is cancelled by the concurrency group; uploaded artifacts of a cancelled run may be partial.
- **Public logs.** Workflow logs of a public repository are public. The harness must not print machine-specific paths beyond the runner's, or any connection string; nothing here supplies one.

## Tests

No automated test: this is CI configuration. Verification: lint the workflow (`actionlint` if it is available, otherwise careful review against the GitHub Actions schema); run the same `dotnet restore`, `build` and `test` commands locally in Debug and Release and confirm they pass; after a human pushes the branch, confirm one green run per configuration with the TRX artifacts attached, and record the run in the handoff. The agent does not push.

## Migration notes

None: CI changes nothing an application, deployment or extension can notice.

## TODO

- Look up the current commit SHAs for `actions/checkout`, `actions/setup-dotnet`, `actions/cache` and `actions/upload-artifact`.
- Write `.github/workflows/build.yml` as designed: triggers with `paths-ignore`, read-only permissions, concurrency, Windows runner, Debug/Release matrix, restore/build/test, always-upload of TRX results, timeout.
- Take the build and test commands verbatim from the "Build and test" section of `AGENTS.md`; pass `DataphorBuildNumber` only if the property exists.
- Lint the workflow; run the commands locally in both configurations.
- Add one line to the "Build and test" section of `AGENTS.md` naming the workflow and saying it runs the same commands.
