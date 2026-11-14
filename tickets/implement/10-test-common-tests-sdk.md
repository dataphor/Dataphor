description: Turn the small existing test project for the shared utility library into a modern .NET 10 test project and make the standard .NET test command at the repository root run it, so the project has a working test command and a pattern the engine test harness then follows.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/Common.Tests/Common.Tests.csproj, tests/Common.Tests/FixedSizeCacheTest.cs, tests/Common.Tests/Properties/AssemblyInfo.cs, tests/Common.Tests/packages.config, Directory.Packages.props, Dataphor.slnx, AGENTS.md
difficulty: easy
----
# Test project conventions, starting with Common.Tests

First of three tickets that together make up `test-harness-inprocess` (this one, then `test-server-fixture`, then `test-harness-inprocess`, which keeps the slug). It sets up what every NUnit project in the repository uses — packages, project shape, the root test command — on the smallest test project, so the two later tickets only add the engine fixture.

## What exists

`tests/Common.Tests` is a legacy (.NET Framework 4.5.2, `packages.config`) project with one test file, `FixedSizeCacheTest.cs`: 13 `[Test]` methods on the fixed-size cache in `src/engine/Base` (assembly `Alphora.Dataphor`). No server, no files, no machine state. It references NUnit 3.12 through a `HintPath` into a `packages/` folder that does not exist. It is not in `Dataphor.slnx`, so nothing builds or runs it.

## Design (settled)

- **Packages (decision D11: NuGet, central package management).** Add to `Directory.Packages.props`: `NUnit` (4.x line), `NUnit3TestAdapter` (5.x line; despite its name it is the adapter for NUnit 4) and `Microsoft.NET.Test.Sdk`. Use the latest stable version of each on nuget.org that is at least two weeks old at the time of the change, and record the three versions in the handoff. Restore must show no `NU1900`–`NU1904` vulnerability warning for them. NUnit 4 rather than 3.14: it is the maintained line and the only cost is the mechanical assert change below. Tradeoff accepted: NUnit's `[Timeout]` does not work on .NET (it relies on thread abort), on either line; the engine harness (`test-harness-inprocess`) uses its own thread and join instead.
- **No shared test props file.** The three `PackageReference` items go in each test project. A `tests/Directory.Build.props` would also apply to `tests/libraries/TestFramework.Coverage.Base/Source/DAE.Diagnostics.csproj`, a library that must not reference test packages.
- **Project file.** SDK-style, generated from the legacy one by the AGENTS.md "Converting another project" rules where they apply: explicit `Compile` items (`FixedSizeCacheTest.cs`, `Properties\AssemblyInfo.cs`; default items are off), `ProjectReference` to `..\..\src\engine\Base\Base.csproj`, the three `PackageReference` items with no version, `<IsPackable>false</IsPackable>`. No `OutputType`, no `OutputPath`, no `DefineConstants` beyond what the legacy Release set had (it had only `TRACE`, so set `DefineConstants` to `TRACE` in Release and `DEBUG;TRACE` in Debug, outright). Target framework comes from `Directory.Build.props` (`net10.0`); signing, version and output location too. Keep the project file's UTF-8 byte-order mark and CRLF line endings. Test projects stay out of `tools/layout/Layout.csproj`.
- **AssemblyInfo.** In `Properties/AssemblyInfo.cs` delete `AssemblyConfiguration`, `AssemblyCompany`, `AssemblyProduct`, `AssemblyCopyright`, `AssemblyTrademark`, `AssemblyCulture`, `AssemblyVersion`, `AssemblyFileVersion` and the template comments about them; keep `AssemblyTitle`, `AssemblyDescription`, `ComVisible`, `Guid`, the byte-order mark and CRLF.
- **Asserts.** NUnit 4 moved the classic asserts to `NUnit.Framework.Legacy.ClassicAssert`. `FixedSizeCacheTest.cs` uses six of them (counted with `grep -o "Assert\.[A-Za-z]*" | sort | uniq -c`): `AreEqual` 209, `False` 45, `True` 36, `IsNull` 32, `AreNotEqual` 2, `IsNotNull` 1. Add `using NUnit.Framework.Legacy;` and change each `Assert.<name>(` of those six to `ClassicAssert.<name>(`. Nothing else in the file changes: no test logic, no expected value, no formatting. The file has a byte-order mark and CRLF line endings; keep both (check with `file` before and after).
- **Delete** `tests/Common.Tests/packages.config`.
- **Solution.** Add `tests/Common.Tests/Common.Tests.csproj` to the `/tests/` folder of `Dataphor.slnx` (CRLF, no byte-order mark, like the file today).
- **Test command.** `dotnet test Dataphor.slnx` (after `dotnet build Dataphor.slnx`, or on its own; add `--no-build` only after a build). On the .NET 10 SDK, `dotnet test` on a solution runs only the projects that are test projects (`Microsoft.NET.Test.Sdk` marks them), through VSTest; `global.json` selects no other test runner, and this ticket does not add one (`build-ci` uses the VSTest TRX logger).

## Edge cases & interactions

- **NLS globalization in the test host.** `Directory.Build.props` adds `System.Globalization.UseNls` as a `RuntimeHostConfigurationOption`; a test project is an executable for the host, so it must land in `artifacts/bin/Common.Tests/<debug|release>/Common.Tests.runtimeconfig.json` under `configProperties`. Verify by reading that file in both configurations and quote it in the handoff. If it is missing, find out why before going on: every later D4 test depends on it (plan WS2 "Globalization").
- **Signing.** The test assembly is strong-named by `Directory.Build.props` like every other; NUnit and the adapter load signed assemblies. Nothing to do; a load failure here would point at the signing settings.
- **Output collisions.** Output is keyed by project file name; `Common.Tests` does not collide with `Common` (`src/protocol/Common/Common.csproj`).
- **Release build.** `dotnet test Dataphor.slnx -c Release` passes too.
- **Warnings.** No new suppression. NUnit's analyzers are not added (they would add warnings to legacy test code that no ticket owns).

## Tests

No new test: this ticket converts a project. Its 13 existing tests are the check; they must all pass unchanged in Debug and Release.

## Migration notes

None: test infrastructure only.

## TODO

- Add the three package versions to `Directory.Packages.props`.
- Rewrite `tests/Common.Tests/Common.Tests.csproj` as an SDK-style project; trim `Properties/AssemblyInfo.cs`; delete `packages.config`.
- Switch the six classic assert names to `ClassicAssert` in `FixedSizeCacheTest.cs`, preserving its byte-order mark and CRLF.
- Add the project to `Dataphor.slnx`.
- `dotnet build Dataphor.slnx` and `dotnet test Dataphor.slnx`, Debug and Release; 13 passed each time; quote the summary lines.
- Read both `Common.Tests.runtimeconfig.json` files and confirm `System.Globalization.UseNls: true`.
- AGENTS.md "Build and test": add `dotnet test Dataphor.slnx` beside the build commands (one sentence: runs every test project in the solution through VSTest); add Common.Tests to the "Converted so far" list.
