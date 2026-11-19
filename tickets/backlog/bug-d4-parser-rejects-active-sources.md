description: The D4 parser rejects thirty-eight documents in the repository at parse time, including the three active catalog and SQL-device-mapping scripts that the server reads at bootstrap.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/Parser.cs, src/engine/DAE/Language.D4/Lexer.cs, src/engine/DAE/Schema/SQLDeviceMappings.d4, libraries/MSSQLDevice/Source/SystemCatalog.d4, libraries/PGSQLDevice/Source/SystemCatalog.d4, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: Some of these files are legacy or parked (FHIR.Server, OracleDevice, SASDevice, TestCache, PHINVADS.Test, TestCQL) and a maintainer might reasonably move them under `legacy/` and skip them permanently rather than fix the parser. Others — the SQL device catalog scripts and the `TestFramework.Coverage.Scripts` documents — are active; if the parser cannot read them, something registers them through a different path or they do not run at all. Both outcomes deserve a look before any fix.
----
## What this is

`ParserRoundTripCorpusTest` in `tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs` quarantines thirty-eight `.d4` files under category `parse`: `Parser.ParseScript` emits one or more entries on `ParserMessages` on the first read, which means the parser could not produce a clean parse tree. The compatibility rule keeps D4 semantics unchanged, so any fix is at the parser — not by editing the `.d4` file.

The highest-priority entries ship with the server:

- `src/engine/DAE/Schema/SQLDeviceMappings.d4` — the SQL device mapping catalog that the server loads at bootstrap. Uses integer literals with a device prefix the parser does not accept.
- `libraries/MSSQLDevice/Source/SystemCatalog.d4` — the SQL Server device's system catalog.
- `libraries/PGSQLDevice/Source/SystemCatalog.d4` — the PostgreSQL device's system catalog.

Then roughly a dozen `tests/libraries/TestFramework.Coverage.Scripts/Documents/` scripts (`Domains`, `EntryLibrary`, `ForeignSuperKeysAndConstantReferences`, `IStringLibrary`, `LibraryDomains`, `PresentationLibrary`, `Serialization`, `SerializationDomains`), several `samples/Sample.Shipping/` documents, and the FHIR.Server / PHINVADS.Test / OracleDevice / SASDevice / TestCache / TestCQL files under `legacy/libraries/`.

## Scope

Decide, per sub-group, whether the fix is in the parser or in the repository layout. Two likely answers:

- **Parser gaps.** A grammar or lexer rule is missing or wrong — e.g. the device-prefixed integer literal in `SQLDeviceMappings.d4` suggests the lexer should accept `L<n>` or similar. Fix at the parser.
- **Files that depend on a grammar extension that never shipped.** Move under `legacy/` with the other parked libraries if they cannot be read, and drop from the `AllowList`.

Do not edit the `.d4` content to make it parse — the scripts are the oracle (plan D2).

## Reproducing

Remove any one entry from the `AllowList` and re-run `dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest"`; the failure includes the first `ParserMessages` entry, which is the parser's own error at the offending token.

## Verification

Each addressed file either passes with its `AllowList` entry removed, or has moved to a location the corpus walk excludes (and its `AllowList` entry is gone with it).
