description: The small test project for the shared utility library is now a modern .NET 10 NUnit project in the solution, and the standard .NET test command at the repository root runs it; review the conversion and the pattern it sets for later test projects.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/Common.Tests/Common.Tests.csproj, tests/Common.Tests/FixedSizeCacheTest.cs, tests/Common.Tests/Properties/AssemblyInfo.cs, Directory.Packages.props, Dataphor.slnx, AGENTS.md, tools/net-spike/README.md
----
# Review: Common.Tests as an SDK-style NUnit project

First of the three tickets that make up `test-harness-inprocess` (then `test-server-fixture`, then `test-harness-inprocess`). Sets the conventions every NUnit project here follows.

## What changed

- `Directory.Packages.props`: `Microsoft.NET.Test.Sdk` 18.11.0, `NUnit` 4.6.1, `NUnit3TestAdapter` 6.3.1 (checked on nuget.org on 2026-11-14: each is the newest stable release at least two weeks old within its line; 18.12.0 of the test SDK was 3 days old). Restore shows no `NU1900`–`NU1904`.
- **Deviation from the ticket: adapter 6.x, not 5.x.** The ticket said "5.x line" for the adapter, reasoning that it is the adapter for NUnit 4. The 5.x line ended at 5.2.0 (Oct 2025); 6.x (6.0 Dec 2025 → 6.3.1 Oct 2026) is the maintained line and also runs NUnit 4. If the reviewer wants 5.x, 5.2.0 is the drop-in; nothing else changes. NUnit stays on 4.x as settled although 5.0.0 exists (Sept 2026).
- `tests/Common.Tests/Common.Tests.csproj`: SDK-style, explicit `Compile` items, `ProjectReference` to Base, the three versionless `PackageReference` items, `IsPackable=false`, `DefineConstants` `TRACE` / `DEBUG;TRACE` outright. BOM + CRLF kept. No `OutputType`, `OutputPath`. Not in `Layout.csproj`.
- `Properties/AssemblyInfo.cs`: removed the attributes listed in the ticket and the version template comment block; BOM + CRLF kept.
- `FixedSizeCacheTest.cs`: `using NUnit.Framework.Legacy;`; 325 `Assert.X(` → `ClassicAssert.X(` for the six names (counts match the ticket: 209/45/36/32/2/1). BOM + CRLF kept.
- **Not in the ticket: Code Access Security removed from four tests.** `InternalAdd`, `InternalReference`, `InternalRemove`, `RandomAccess` wrapped their bodies in `try { new ReflectionPermission(...).Assert(); … } finally { CodeAccessPermission.RevertAssert(); }`. Those types are not in .NET 10 (CS1069/CS0103). Code Access Security does not exist on .NET (private reflection is unrestricted), so the calls did nothing to the test; I removed the permission lines, the `try` keyword and the `finally` block, leaving the body as a plain `{ … }` block so the indentation and every test statement are unchanged. Alternative rejected: referencing the `System.Security.Permissions` package to keep no-op stubs that raise `SYSLIB0003`. The `using System.Security.Permissions;` line stays (harmless; a commented-out test further down still mentions the types).
- `packages.config` deleted. Project added to `/tests/` in `Dataphor.slnx`.
- `AGENTS.md`: `dotnet test Dataphor.slnx` in Build and test plus one sentence on test projects; Common.Tests in "Converted so far"; spike wraps 12 (not 13) of its 30 projects.
- `tools/net-spike/README.md`: Common.Tests in the used-in-place list and its own results row (the spike detects SDK-style projects itself; `--only Common.Tests` built it in place with 0 errors).

## Validation (run during implement)

- `dotnet build Dataphor.slnx`: 0 errors; Common.Tests adds no warnings (checked with a non-incremental Release build of the project).
- `dotnet test Dataphor.slnx` (Debug): `Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, Duration: 759 ms - Common.Tests.dll (net10.0)`
- `dotnet build Dataphor.slnx -c Release` then `dotnet test Dataphor.slnx -c Release --no-build`: `Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, Duration: 600 ms - Common.Tests.dll (net10.0)`
- **12, not 13 tests**: the file has 12 live `[Test]` methods; the 13th match of the ticket's count is a commented-out `//[Test]` (line ~987). The legacy file at HEAD also has 12 live ones.
- `artifacts/bin/Common.Tests/debug/Common.Tests.runtimeconfig.json` `configProperties`: `"System.Globalization.UseNls": true, "MSTest.EnableParentProcessQuery": true, "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false`. The release file also has `"System.Globalization.UseNls": true`.

## Tests

None added: a project conversion. The 12 existing tests are the check, unchanged except for the assert class name and the CAS removal above.

## For the reviewer

- Confirm the adapter-line deviation and the CAS removal are acceptable.
- `MSTest.EnableParentProcessQuery` appears in the runtimeconfig (comes from the test SDK); harmless, noted only so it is not a surprise.
- Migration notes: none (test infrastructure only).
