description: The small test project for the shared utility library is now a modern .NET 10 NUnit project in the solution, and the standard .NET test command at the repository root runs it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/Common.Tests/Common.Tests.csproj, tests/Common.Tests/FixedSizeCacheTest.cs, tests/Common.Tests/Properties/AssemblyInfo.cs, Directory.Packages.props, Dataphor.slnx, AGENTS.md, tools/net-spike/README.md
----
# Complete: Common.Tests as an SDK-style NUnit project

First of the three tickets that make up `test-harness-inprocess` (then `test-server-fixture`, then `test-harness-inprocess`). It sets the conventions every NUnit project here follows: test projects reference `Microsoft.NET.Test.Sdk`, `NUnit` and `NUnit3TestAdapter` directly with versions in `Directory.Packages.props`, are listed under `/tests/` in `Dataphor.slnx`, stay out of `tools/layout/Layout.csproj`, and run with `dotnet test Dataphor.slnx`.

## What landed (`ticket(implement): test-common-tests-sdk`)

- `Directory.Packages.props`: `Microsoft.NET.Test.Sdk` 18.11.0, `NUnit` 4.6.1, `NUnit3TestAdapter` 6.3.1. The adapter is on the 6.x line rather than the 5.x the plan ticket named: 5.x ended at 5.2.0, 6.x is the maintained line and runs NUnit 4.
- `tests/Common.Tests/Common.Tests.csproj`: SDK-style, explicit `Compile` items, `ProjectReference` to Base, versionless package references, `DefineConstants` set outright (`TRACE`; `DEBUG;TRACE` in Debug). `packages.config` deleted.
- `Properties/AssemblyInfo.cs`: version, company, product, copyright and the other attributes that `Directory.Build.props` now supplies removed.
- `FixedSizeCacheTest.cs`: NUnit 4's classic asserts (`ClassicAssert.X`, `using NUnit.Framework.Legacy;`); Code Access Security calls removed from four tests (the types do not exist on .NET 10 and the calls never affected the test; the bodies stay as plain blocks so indentation is unchanged).
- `AGENTS.md` and `tools/net-spike/README.md` updated (test command, Common.Tests in the converted lists, spike wraps 12 of its 30 projects).

## Review findings

- **Order-dependent test, fixed (defect, reproduced).** `RandomAccess` drove a fresh cache through random operations but then ran `ValidateList()` on the fixture's `FTestCache` field, which it never set: the check read whatever cache the previous test left behind, and `dotnet test --filter Name=RandomAccess` failed with `NullReferenceException` in `GetHead()`. Pre-existing (the legacy file has the same code). Fix: the test now assigns `FTestCache` and passes it to the random driver, so the check covers the cache that was exercised. Pointing the check at that cache exposed a second problem: the driver's indexer writes stored a random value under a key, and `ValidateList` requires each entry's value to match its key, so the driver now writes the key's own value (commented at the site). The indexer's replace path is still exercised. `RandomAccess` passed three runs alone after the fix (it uses an unseeded `Random`), and all 12 tests now pass one at a time, not only together.
- **Dead `using` lines removed (minor).** `using System.Security;` and `using System.Security.Permissions;` were left after the Code Access Security removal; no live code uses either (only the commented-out `ConcurrentAccess` test names those types, and it would need the same removal to be revived). Removed.
- **Adapter 6.x deviation: accepted.** The 5.x line has no releases after 5.2.0; 6.x runs NUnit 4 (12/12 pass in Debug and Release). Nothing else depends on the adapter line.
- **Code Access Security removal: accepted.** Checked the diff: only the permission lines, `try` and `finally` blocks went; every test statement is unchanged. The commented-out asserts were renamed to `ClassicAssert` along with the live ones, which is harmless and keeps them compilable if revived.
- **Project file and conventions.** Matches AGENTS.md's conversion rules (no `OutputPath`, no versions on package references, constants set outright, BOM and CRLF kept in all three edited files, verified with `file`). Signing comes from `Directory.Build.props`; Base is signed too, so no mismatch. `IsPackable=false` is redundant with the test SDK's defaults but harmless.
- **Docs.** Read AGENTS.md, `tools/net-spike/README.md`, `docs/modernization-plan.md` (WS3 and the spike-results appendix), `docs/architecture.md`, `docs/migration-notes.md`. The plan's appendix row "D4Runner, test projects … blocked" is a dated snapshot of the spike's results with later changes described below it; the current status lives in the spike README, which was updated, so the plan was left alone. No migration note: test infrastructure only, nothing an application or extension can notice.
- **Tests.** None added beyond the fix above: this is a project conversion, and the 12 existing tests are the check. None cut: each tests a distinct cache behaviour.
- **Tripwires.** None recorded: nothing found that is fine now but conditional on later change.
- **Validation.** `dotnet build Dataphor.slnx`: 0 errors. Non-incremental Release build of Common.Tests: no warnings from Common.Tests (the 8 shown come from Base). `dotnet test Dataphor.slnx`: `Passed! - Failed: 0, Passed: 12, Skipped: 0, Total: 12` in Debug and in Release. Each of the 12 tests passes when run alone with `--filter Name=<test>`.
