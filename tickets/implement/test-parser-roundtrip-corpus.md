description: An L0 property test that every D4 file in the repository parses, that re-emitting the parse tree and parsing it again gives the same tree, so that the lexer, parser and D4 emitter stay in agreement as the codebase evolves.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/Parser.cs, src/engine/DAE/Language.D4/D4TextEmitter.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/Common.Tests/
difficulty: easy
----
## What this is

L0 of the WS3 layer table: "every `.d4` file in the repository parses, emits, and re-parses to the same tree". One NUnit test with a `TestCaseSource` that walks the repository, parses each `.d4` file, emits the parse tree with `D4TextEmitter`, parses the emitted text and asserts that the two trees are equal. The test runs with no server (it is an L0 unit test): it exercises the parser, the lexer and the emitter in isolation, which is the cheapest layer that can show a regression in either side.

Count at the time of filing: 658 `.d4` files in the repository.

## Requirements

- One test case per `.d4` file. Case name is the file's repository-relative path with forward slashes, so a failure points at the file.
- Discovery walks the repository root (the directory containing `Dataphor.slnx`). Repository-relative path computed from that root, resolved from the compiled test assembly's location.
- Each case does:
  1. Read the file as text (preserve the file's existing encoding; the parser accepts UTF-8 with or without BOM).
  2. Parse the text to a script.
  3. Emit the script to text with `D4TextEmitter`.
  4. Parse the emitted text to a second script.
  5. Assert the two scripts are structurally equal.
- "Structurally equal" is a deep value comparison over the AST's own data, excluding source-position fields (`Line`, `LinePos`, `Token`, trailing whitespace). Use a dedicated visitor or a serializer that elides those fields; do not compare the two emitted strings, because the emitter is allowed to normalize whitespace and the point of the test is tree equivalence.
- A parse failure on step 2 fails the case with the parser's error and a one-line excerpt of the offending input. A parse failure on step 4 is a harness bug (the emitter produced unparseable text) and must be reported as such with the emitted text written to the test's TRX output.
- A file that is intentionally not parseable (if any are found during the first run) is excluded by an allow-list in the test fixture, each entry carrying a one-line reason. Expect this list to be empty; if any file needs it, record the entries in the handoff and in a tripwire `NOTE:` at the fixture.
- Keep the fixture in `tests/DAE.Server.Tests/` with the other L0/L1 code, or in `tests/Common.Tests/` if the parser and emitter are reachable from there without adding a server reference — whichever requires fewer project references. The parser lives in `src/engine/DAE/Language.D4/Parser.cs`.
- No new tests in other projects, and no edits to `.d4` files to make the test pass. A file that fails to round-trip is a defect in the parser or emitter, to fix at the lowest layer and report in the handoff.

## Edge cases & interactions

- **Files with no top-level statements.** A `.d4` that holds only a comment (nine coverage scripts are in this shape; see the `test-harness-inprocess` manifest) parses to an empty script and round-trips trivially. Verified by test.
- **Upgrade scripts.** `libraries/*/Upgrades/Upgrade.*.d4` files and their counterparts use the full `.d4` grammar; they are in scope. Verified by test.
- **Documents.** `libraries/*/Documents/*.d4` and `tests/libraries/TestFramework.Coverage.Scripts/Documents/*.d4` are in scope.
- **Encodings.** Files in the tree mix UTF-8 with and without BOM (see `tickets/rules/10-dataphor.md`). The parser accepts both; the test reads with `File.ReadAllText` under `Encoding.UTF8` (which strips a BOM if present). Verified by test; a file that depends on bytes outside the parser's input alphabet would fail step 2 visibly.
- **Build artifacts under the repo root.** Exclude `artifacts/`, `obj/`, `bin/`, `.git/`, `node_modules/` and `tickets/.logs/` from the walk (defensive — any `.d4` found in them is a build output copy and double-counts). Verified by test.
- **`external/`.** `external/` is a submodule tree whose files are not ours to characterize; exclude. Verified by test (fixture excludes the directory).
- **Test performance.** 658 cases in one fixture. Each case is a file read plus two parses, no server, no I/O beyond the file. NUnit reports one line per case at normal verbosity; at the default verbosity only the failures are listed. Record the total time in the handoff.

## Verification

Each case asserts tree equality through a deep-comparison helper tested by inspection (write one positive and one negative case against hand-crafted D4 input, keep them only if they add value beyond the 658 corpus cases). Do not mock the parser or the emitter.

## TODO

- Locate the parser entry point and the `D4TextEmitter` API; confirm they are reachable from the chosen host project.
- Write the deep-comparison helper (reflection over the AST node base class, skipping `Line`, `LinePos`, `Token` and any `OriginalText` or similar fields).
- Add the fixture with `[TestCaseSource]` over discovery; make case names the repository-relative path.
- Add the directory exclusions (`artifacts/`, `obj/`, `bin/`, `.git/`, `node_modules/`, `tickets/.logs/`, `external/`).
- Build and run; capture the number of cases and the total time.
- Fix any defect found in the parser or emitter at its layer, with the smallest reproduction D4 input as a permanent test (follows the project's testing rule); if none found, say so in the handoff.
- Update AGENTS.md's "Build and test" paragraph: one sentence that the L0 parser corpus test exists and covers every `.d4` in the repository.
