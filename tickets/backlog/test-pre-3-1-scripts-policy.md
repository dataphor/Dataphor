description: Sixteen engine test scripts fail because they could not have passed on Dataphor 3.1 either (an older form of the language, features that were removed, scratch notes, or broken text); they stay recorded as known failures, and this ticket owns making each of them pass.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/coverage-manifest.json, tests/libraries/TestFramework.Coverage.Scripts/Documents
difficulty: medium
----
## Decision (owner, 2026-11-18): keep them as known failures; this ticket fixes them

The owner decided against a new `retired` status and against deleting the scripts. The sixteen scripts stay `known-failure` in the manifest, pointing at this slug, and this ticket owns fixing them. The slug is kept so the manifest entries stay valid. The *Proposed text* and *Alternatives* sections below were the blocked proposal and are **not** adopted. The evidence tables are the starting point for the work.

When this is promoted, the plan stage splits it by the categories below. The slug stays on the ticket that fixes the last script, and each piece flips its scripts' manifest entries to `pass` in its own commit:

- **Live intent, broken text** (`Serialization`, `SerializationReferences`, `StreamAllocation`): the most useful work and the first to promote. Each needs some statements in the script itself changed. "Never edit an expected value" still holds. Whether statements may be repaired, and which, is still the owner's call: the plan ticket lists each proposed statement change with its evidence and asks for approval in `blocked/` before editing.
- **Earlier copies whose successors pass** (`Domains`, `SerializationDomains`, `LibraryDomains`) and **features 3.1 did not have** (`EntryLibrary`, `PresentationLibrary`, `IStringLibrary`, `DerivationMaps`, `MaximumRowCount`, `DAE`, `Exceptions`): making these pass means either rewriting the script or restoring an engine feature. Both are decisions for the owner, so the plan stage proposes one per script.
- **Not tests** (`ForeignSuperKeysAndConstantReferences`, `ViewDependencies`, `DebugTest`): same as the previous bullet.

## The question

The coverage scripts are the oracle for "behaves like 3.1" (plan D2). An assertion that fails on .NET 10 is presumed to be a porting defect until shown otherwise. `test-triage-known-failures` found sixteen failing scripts for which it is shown otherwise: their failure does not depend on the runtime at all, so they failed on 3.1 too. The plan's L1 layer allows only `pass` and `known-failure` (with the ticket that owns the cause) and says "no silent skips". AGENTS.md says "Never edit a coverage script to make it pass"; the plan (WS3, cross-cutting requirements) says expected values are "never edited to make a run green without a ticket that explains the difference", which allows an edit under a ticket. Neither says what happens to a script that tests nothing 3.1 did.

Leaving them `known-failure` forever misreports them: a known failure is read as a defect someone owns.

## The scripts and the evidence

How "could not have passed on 3.1" was shown: the D4 parser's grammar at tag `v3.1` (2018) differs from today's only by the added `foreach` expression; the D4 keyword list and the lexer at `v3.1` have no `domain`, `entry` or `presentation` keyword and no `"…"i` literal; and each script file is unchanged since the 2008 import apart from the repository move. The legacy run lists `tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4` and `InsertCases.d4` carry the original authors' notes.

**Earlier copies of scripts that pass today** (written when types were called domains):

| Script | Error | Successor (passes) |
|---|---|---|
| `Domains` | `Unknown create directive "domain"` | `Types`: the same file with `domain` → `type` and one tag name changed |
| `SerializationDomains` | `Unknown create directive "domain"` | `SerializationTypes`: the same file with `domain` → `type` in the keyword and the object names |
| `LibraryDomains` | `"," or ")" expected` (the `'…'i` literal) | `LibraryTypes`: the same tests without `i` literals, plus the `Directory` field; `All.Coverage.d4` runs `LibraryTypes` |

**Types and features that 3.1 did not have:**

| Script | What it tests | Evidence |
|---|---|---|
| `EntryLibrary` | the `entry` type | no `entry` keyword at `v3.1`; `RowLibrary` covers rows |
| `PresentationLibrary` | the `presentation` type | no `presentation` keyword at `v3.1` |
| `IStringLibrary` | the `IString` type and `"…"i` literals | no `IString` token in the lexer at `v3.1` or in the 2008 import |
| `DerivationMaps` | Frontend's `LibraryDerivationMaps` table | not in `Frontend` at `v3.1`; `All.Coverage.d4`: "no longer required (deprecated feature) BTR 6/6/2004" |
| `MaximumRowCount` | row-allocation counting through `UsedRowCount()` | returns 0 unless `USEROWMANAGER` is defined, and no project defined it at `v3.1` (its code does not even compile under the current naming); first check fails at once. Legacy: "Doesn't run 1/17/2004" |
| `DAE` | low-level engine diagnostics | `TestCatalog()` asserts that a hiding object name is rejected, which happens only under `DISALLOWAMBIGUOUSNAMES`, "(currently disabled)" in `CompilerDefines.txt` at `v3.1`. The other nine operators it calls (`TestRowManager` … `TestTableBuffer`) are commented out of `DiagnosticsLogic.d4` at `v3.1`, and the C# of seven of them is commented out of `TestNodes.cs` too (only `TestScalarManagerNode` and `TestStreamManagerNode` still compile); both files are unchanged since `v3.1` apart from the move. `TestStreams()` alone passes (scratch run). Legacy: "Rewrite for native values processing" |
| `Exceptions` | every exception class's error codes have a resource string | `TestExceptions` is commented out of `DiagnosticsLogic.d4` and its C# (`TestExceptionsNode`, written against APIs that no longer exist) of `TestNodes.cs` at `v3.1`. Legacy: "Rewrite for native values processing" |

**Not tests:**

| Script | What it is |
|---|---|
| `ForeignSuperKeysAndConstantReferences` | design notes ("Investigating Foreign Superkey and Constant References", 2015): no assertions, a missing `;` after line 19, and it creates tables `A`, `B` and `S` twice |
| `ViewDependencies` | an investigation: no assertions, `select`s only, and a column `Dependent_Object_ID` that `ObjectDependencies` has never had (it is `Dependency_Object_ID`, also at `v3.1`) |
| `DebugTest` | an infinite loop by design, for stepping in the IDE's debugger; the manifest stops it after 5 s |

**Live intent, broken text:**

| Script | Defect | What is behind it |
|---|---|---|
| `Serialization` | three debugging `select` lines without a `;` (lines 17–19, one with the comment "Why does this script the creation of a representation of MSSQLBinary?") | the driver for the nine `Serialization*` scripts: it scripts a library with `ScriptLibrary`, drops it, and replays it through `TestSerializer`. With the three lines removed and the `References` step skipped (scratch run), the first `ExecuteScript("Coverage.Scripts", …)` after `SetLibrary('TestSerialization')` fails: `Object reference "Coverage.Scripts" is ambiguous between the following names: "Coverage.Scripts", "TestFramework.Coverage.Scripts"`, and the rollback then fails with `Library "TestSerialization" is registered and cannot have requisites removed`. Legacy: "ScriptLibrary operator needs to be revisited" |
| `SerializationReferences` | creates table `Test` twice (lines 30 and 45): `An object named "General.Test" is already defined` | a step of `Serialization` |
| `StreamAllocation` | loads documents from a library named `Coverage`, the scripts library's name before it was split (no such library at `v3.1`) | with the name corrected (scratch run) it gets as far as running every script in the library through `Execute` and fails in `ApplicationTransactions` (`engine-at-cursor-no-device`); since it runs every script, including `DebugTest`, it cannot pass while any script fails or hangs. Legacy: "Doesn't run 1/17/2004" |

## Proposed text (recommended default)

For the plan, WS3 cross-cutting requirements, after "The scripts are the oracle":

> **Scripts that never ran on 3.1.** A script whose failure is shown not to depend on the runtime (it uses syntax, types, operators or catalog objects that 3.1 did not have, or it is not a test) is not a porting defect. It is marked `retired` in the manifest, with a note giving the evidence and, where one exists, the script that replaced it. A retired script is not run; it is reported as Skipped and counted in the run summary, and its file stays in the library. A script whose intent is still 3.1 behaviour but whose text cannot run on any version (a missing `;`, an object created twice, a library name from before a rename) may have those statements repaired under a ticket that names each change and shows why it could not have run; expected values are never changed, and the repair is a commit of its own.

For AGENTS.md and `tickets/rules/10-dataphor.md`, replace "Never edit a coverage script to make it pass" with: "Never edit an expected value in a coverage script. A script's statements may be repaired only under a ticket that shows the text could not have run on 3.1 (see the plan, WS3)."

Applied to the sixteen:

- **Retire:** `Domains`, `SerializationDomains`, `LibraryDomains`, `EntryLibrary`, `PresentationLibrary`, `IStringLibrary`, `DerivationMaps`, `MaximumRowCount`, `DAE`, `Exceptions`, `ForeignSuperKeysAndConstantReferences`, `ViewDependencies`, `DebugTest`, `StreamAllocation`.
- **Repair, then triage what is behind:** `Serialization` (remove the three debugging lines) and `SerializationReferences` (rename the second `Test`, with the reference that names it). One `fix/` ticket; it then owns the ambiguous-name and rollback errors above or files them.
- **Keep the purpose of two retired scripts alive elsewhere:** `Exceptions` checked that every error code has a message, and error codes are a compatibility surface; propose a small NUnit test that enumerates each exception class's `Codes` and checks its resource (an L0 test, an arm of `feat-ws3-test-layers`). `StreamAllocation` and `MaximumRowCount` were leak checks; list them for `test-coverage-gap-analysis`.

The `retired` status would be implemented like the `fragment` status that `test-triage-known-failures` adds (not run, reported as Skipped, counted; `note` required; `ticket` and `error` not allowed), in one small implement ticket with the manifest changes and the doc edits.

## Alternatives considered

- **Keep them all `known-failure` against this ticket, closed as "won't fix".** No code. Cost: a slug that never resolves, and 16 of the run's known failures that are not defects, which hides the real ones in the count.
- **Delete the files.** Clean, and git keeps them. Rejected as the default because it conflicts with "no silent skips" and loses the record in the library itself; acceptable if the owner prefers it for the earlier-dialect copies, whose successors pass.
- **Translate the old-dialect scripts to current D4.** For `Domains`, `SerializationDomains` and `LibraryDomains` the translation already exists and passes; for `entry`, `presentation` and `IString` there is nothing to translate to.
- **Rewrite the disabled diagnostic operators for `DAE` and `Exceptions`.** Several test engine parts that are compiled out (the row manager) or behaviour that 3.1 had switched off (rejecting hiding names); the useful part of `Exceptions` is cheaper as the NUnit test above.

## If nothing is decided

The sixteen stay `known-failure` against this slug and the run stays green; gate G1's "every script accounted for" holds, since each names a ticket on the board. Fully reversible: a status in a JSON file, two doc sentences, and (for the repairs) two script edits in their own commit.
