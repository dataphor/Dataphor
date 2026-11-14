description: An automatic build-and-test run on GitHub now builds and tests the solution in Debug and Release for every push and pull request and keeps the test results.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws1-build-and-workspace
files: .github/workflows/build.yml, AGENTS.md, global.json, Directory.Build.props
difficulty: easy
----
# Continuous integration: build and test on GitHub Actions

## What landed

`ticket(implement): build-ci` added `.github/workflows/build.yml`: one `build` job on `windows-latest`, matrix Debug and Release (`fail-fast: false`, 60-minute timeout), triggered by `push` and `pull_request` on `master` and `modernization` (both ignoring changes confined to `tickets/**` and `docs/**`) and by `workflow_dispatch`. Read-only token, `pull_request` only, no secrets; runs of the same ref cancel each other. Actions are pinned by commit SHA (checkout v7.0.1, setup-dotnet v6.1.0, cache v6.1.0, upload-artifact v7.0.2). Steps: `dotnet --info`, restore, build with `-p:DataphorBuildNumber=<run number>`, `dotnet test --no-build --logger trx` into `artifacts/TestResults/<cfg>`, and an always-run upload of the artifact `test-results-<cfg>` (kept 14 days). `ContinuousIntegrationBuild` comes from `Directory.Build.props` (keyed on `GITHUB_ACTIONS`). `AGENTS.md` "Build and test" names the workflow, triggers and artifacts.

Still to confirm by a human after the push: one green run per configuration, each artifact holding both TRX files (Common.Tests and DAE.Server.Tests), and that the coverage scripts that need SQL Server fail in their recorded way on the runner (no default SQL Server instance expected on `windows-latest`).

## Review findings

- **Diff read first**, then the handoff. Workflow commands match the AGENTS.md commands; the AGENTS.md paragraph matches the workflow (triggers, path filter, artifact names). `DataphorBuildNumber` and the `GITHUB_ACTIONS` condition exist in `Directory.Build.props` as the workflow comment says. File is LF, ASCII, spaces, as `.editorconfig` asks for YAML.
- **Security:** `permissions: contents: read`, no `pull_request_target`, no secrets, no untrusted input (`github.head_ref`, PR title) interpolated into `run:` — only `matrix.configuration` and `github.run_number`. Actions pinned by SHA. Nothing found.
- **Correctness:** NuGet cache path `~/.nuget/packages` is the default global-packages folder on the Windows runner; the key covers the central package versions and every project file. `concurrency` keyed by ref is correct for both push and pull-request refs. `if-no-files-found: ignore` keeps a failed build from masking its own error with an upload error. Nothing found.
- **Tripwire (parked in the workflow):** the `docs/**` path filter means a documentation job — the link checker that `docs-link-check-ci` will add — must not go into this workflow, or it would never run on the changes it checks. Added a `NOTE:` next to the filter. The implementer's existing `NOTE:` about branch protection and the path filter is kept.
- **Tripwire (noted here only):** `FileVersion`'s fourth component is limited to 65535, and the build number is `github.run_number`; not reachable for many years of runs, and `installer-msi` will revisit build numbering anyway, so no comment added.
- **Docs:** read `AGENTS.md`, `docs/modernization-plan.md` (WS1, WS3, WS8, security rows referring to CI) and `docs/migration-notes.md`. The plan's opening problem table still says "there is no CI"; it records the starting state, so it is left alone. No migration note: nothing an application or extension can notice.
- **Tests:** none added or cut — CI configuration has no behaviour a test in this repository could pin. Ran locally after the review edit: `dotnet build Dataphor.slnx` 0 errors; `dotnet test Dataphor.slnx --no-build` DAE.Server.Tests 94 passed, 46 skipped (manifest known-failure and unstable), 0 failed. `actionlint` was not re-run; the review changed only a YAML comment.
- **Action pin age:** setup-dotnet v6.1.0 is eleven days old; no known problem, kept.
- **Not verified:** a real run on GitHub (the agent does not push); listed above for the human.
