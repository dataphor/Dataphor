description: The D4 text emitter produces text the parser cannot read for eight documents in the repository, so a round trip through emit/re-parse fails outright.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, src/engine/DAE/Language.D4/Parser.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: These eight files parse today and run today; the defect is only observed when the parse tree is emitted (as the catalog store does when persisting DDL). A maintainer might defer this behind the fifty-one `emitter-reshape` cases on the same `AllowList`, which look worse per-file but do not produce malformed text.
----
## What this is

`ParserRoundTripCorpusTest` in `tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs` quarantines eight `.d4` files under category `emitter-malformed`: the first parse succeeds, the emitter produces text, and the re-parse raises a syntax error (observed messages: `Syntax error near "{"`, and similar near other delimiters). The emitter is producing output the parser cannot read, which means the same emitter call on a persisted catalog script would also round-trip-fail — the catalog store emits through `D4TextEmitter` when it writes object scripts.

The files, each already proven to reproduce because the test attaches the emitted text as a TRX attachment when a case fails:

- `legacy/libraries/FHIR.Server/Documents/FHIR.LoadConformanceCache.d4`
- `legacy/libraries/FHIR.Server/Documents/ModelInfo.Operators.d4`
- `libraries/System.Internet/Documents/Dynamic.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/IntegerLibrary.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/Lexer.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/SecurityLibrary.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/SerializationOperators.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/ViewUpdatability.d4`

## Scope

Find the smallest D4 construct per failure that the emitter produces malformed text for, fix the emitter at that site, and drop the corresponding entry from the `AllowList` in `ParserRoundTripCorpusTest.cs` in the same commit. The compatibility rule says the D4 language semantics do not change; the fix is in the emitter, not in the grammar.

## Reproducing

Remove one entry from the `AllowList` dictionary in `ParserRoundTripCorpusTest.cs` and re-run with `dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest"` — the test attaches the emitted text to the failure as `emitted-<path>.d4`, which is the smallest reproduction the test has.

## Verification

Each fixed file passes with its `AllowList` entry removed. No new tests beyond what the corpus test already provides; this is exactly the bug class the corpus test was built to catch.
