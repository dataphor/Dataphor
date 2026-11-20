description: The D4 text emitter produces text that re-parses to a different tree shape for fifty-three documents in the repository, so a round trip is not a fixed point of emit/parse.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, src/engine/DAE/Language.D4/Parser.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs
repro: verified
severity: edge-case
likelihood: normal-use
tradeoffs: The reshaped text still compiles and runs — the observed differences are statement-level flattening, a `try/commit` desugaring the emitter already performs, a `by` clause the emitter adds, and tag ordering. No user has reported broken behaviour from any of these. A maintainer might decline this in favour of adjusting the corpus test's comparator to accept known-benign reshapes (filter in `TreesEqual`), at the cost of a weaker regression net.
----
## What this is

`ParserRoundTripCorpusTest` in `tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs` quarantines fifty-three `.d4` files under category `emitter-reshape`: both parses succeed, but `TreesEqual` reports a structural difference. Four sub-patterns observed on first read:

- **Multi-variable statement flattening.** `var a : X, b : Y` parses as a `Block { VariableStatement, VariableStatement }`; the emitter emits each child on its own with its own `;`, so the re-parse produces two separate `VariableStatement` siblings in the enclosing block instead of one nested `Block`. Hits every `TestFramework.Coverage.Scripts` library script with multi-var declarations, including `IntegerLibrary.d4` and `SerializationOperators.d4`, which moved into this category when `bug-d4-emitter-malformed-output` cleared their malformed output.
- **`try … commit` desugaring.** The emitter rewrites `try X; commit;` into `begin BeginTransaction(); try X; CommitTransaction(); except begin RollbackTransaction(); raise end end`. The output is a different tree even though its semantics match. Hits `PlanCache`, `EventHandlers`, `TransitionConstraints`, both `DeferredConstraint*` scripts, and the `Sample.Shipping` schema and coverage documents.
- **`hasByClause` toggled.** `root.…expression.hasByClause: value differs (False vs True)` — the emitter adds a `by` clause the source did not carry. Hits `libraries/System.Integration/Documents/Register.d4` and `tests/libraries/TestFramework.Coverage.Scripts/Documents/SystemCatalog.d4`.
- **Metadata tag ordering.** `metaData.tags[0]: Tag differs (…=… static=True vs …=… static=False)` — the emitter reorders static and non-static tags, so the first-tag mismatch is an index shift. Hits `src/engine/DAE/Schema/{DataTypes,SystemCatalog}.d4`, `samples/Sample.IssueTracker/Documents/Register.d4`, `samples/Sample.Shipping/Documents/Schema.DataTypes.d4`, `tests/libraries/TestFramework.Coverage.Scripts/Documents/SerializationTypes.d4`.

These are visible every time the catalog store emits a script with a tag, a multi-var declaration or a `try/commit`; the stored text is not a byte-round-trip of the source. For most callers this is cosmetic — the plans are built from the parse tree, not the stored text — but any code that compares two stored scripts, diffs stored scripts, or expects the stored text to match the source will see the reshape.

## Scope

Each sub-pattern is one investigation and one fix at the emitter (not the parser; the compatibility rule keeps D4 language semantics unchanged). Four sub-patterns is a plausible split into four smaller tickets in `plan/` if the first attempt shows they are independent; keep one ticket if they share a fix.

Priority within the list: `src/engine/DAE/Schema/{DataTypes,SystemCatalog}.d4` are the active catalog scripts that run at server bootstrap. They ship. Fix those first.

## Reproducing

Remove any one entry from the `AllowList` and re-run the fixture; the test fails with the exact tree-difference path (e.g. `root.statements[0].statements[3]: list length differs (2 vs 1)`), which is the smallest reproduction the test has.

## Verification

Each fixed file passes with its `AllowList` entry removed. No new tests beyond what the corpus test already provides.
