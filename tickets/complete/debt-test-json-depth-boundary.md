description: A test was added that records the exact point at which the upgraded JSON reader starts refusing deeply nested data, so that a customer with deeper data is not the one who discovers the new limit.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4, docs/migration-notes.md
----
## What landed

Two files, no C# and no new test fixture.

**`tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4`** — a block appended to the existing coverage script that pins where the Newtonsoft.Json nesting limit sits and what crossing it looks like. It asserts five things:

- a value nested in exactly 64 objects reads (`GetType(Dynamic(...)) = 'Object'`);
- 65 levels are refused by the `Dynamic(String)` operator;
- 65 levels are refused by the `Content` representation selector, reached as `Dynamic(row { ... Content })`;
- 65 levels are refused by the `Content` write accessor (`LValue.Content := ...`);
- writing is not limited, so a 71-level value built by repeated `Set` serializes through the `Content` read accessor and then cannot be read back — the asymmetry is the part most likely to surprise someone, and the comment above it says so.

Those are all three JSON reader call sites the library exposes to D4 (the `JsonConvert.DeserializeObject` calls in `DynamicNodes.cs`: the representation selector, the `Dynamic(String)` operator and the `Content` write accessor). The library's fourth reader, in `JSONObjectConveyor.cs`, is unreachable — it never reports itself as streaming — and is owned by `bug-json-conveyor-declares-itself-non-streaming` in `backlog/`; a comment in the script says so, so a reader of the test does not go looking for a conveyor case.

Each failure assertion is `if IndexOf(LCaught, 'MaxDepth of 64 has been exceeded') < 0 then raise`, which catches both "it did not fail at all" (`LCaught` stays empty) and "it failed for some other reason" (the raised message carries whatever did arrive). The expected substring is the library's own wording minus the `Path` and `position` parts, which vary with the document's shape.

**`docs/migration-notes.md`** — one row in the `## D4, catalog and libraries` table: what the limit is, which three operators carry it, that Newtonsoft.Json 13 sets it and version 9 had none, that writing is unlimited so a value can be built deeper than it can be read, and that an affected application needs its documents or nesting flattened and should report the problem.

## Tests

| Test | What it verifies |
|---|---|
| The block in `Internet.d4` (one coverage case, `Internet`) | Where the Newtonsoft.Json 13 nesting limit sits (64 reads, 65 refused), that all three D4-reachable JSON readers carry it, and that the write path does not — so a deep value the engine builds itself cannot be read back. |

No test was added or kept beyond that: this is a characterization test for a behaviour change that arrived with a dependency upgrade, and there is no other new code to cover.

## Validation run

- `dotnet build tools/layout/Layout.csproj` — 0 errors, 141 pre-existing warnings, unchanged. No C# changed, so no wider build was needed.
- `dotnet test tests/DAE.Server.Tests --no-build --filter Name=Internet --logger "console;verbosity=normal"` — `pass: 1 passed, 0 failed`, case time 4 s, the same 4 s the plan measured with and without the block. Run again after each edit in this pass; green each time.
- `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` — 78 files, 1026 links, **0 errors**, 203 pending (unconverted reference pages, pre-existing). This is the documentation acceptance check and it is what stands in for a lint pass here; the repository has no other linter.

The rest of the suite was deliberately not re-run, with a reason rather than silently: the only functional change is inside one coverage script's own case, and the harness gives every script a server of its own copied from one template specifically so that a script's result does not depend on any other script (see the class comment on `CoverageScripts` in `tests/DAE.Server.Tests/CoverageScripts.cs`). So running the whole part that holds `Internet` would re-run roughly a hundred unrelated cases for no information. The other changed file is a markdown document no test reads.

No pre-existing failures surfaced; `tickets/.pre-existing-error.md` was not written.

## Review findings

**Major — none filed, and nothing met the filing bar.** The one substantive finding was cheap to fix in place, so it was, and no ticket was opened.

**Minor, fixed in this pass:**

- **A third reachable JSON reader was uncovered, and the test said it did not exist.** `ticket(implement): debt-test-json-depth-boundary` asserted in a comment that the `Content` write accessor was "the other reader D4 can reach", and the plan it inherited that from said the representation selector's signature was identical to the `Dynamic(String)` operator and so not reachable separately. That is wrong: `DynamicContentSelectorNode` takes a **row** (`((IRow)argument1)["Content"]`), not a string, so it is reached by a row-valued call — `libraries/System.Internet/Documents/DynamicMusings.d4` already calls it that way. It is a distinct `JsonConvert.DeserializeObject` site and carries the same limit. Verified rather than assumed, with a negative control: with the new arm pointed at 64 levels the case **fails** with an empty `LCaught` (the read succeeded), and at 65 levels it passes with Newtonsoft's MaxDepth message. So the arm is real coverage of a distinct site, not a vacuous assertion. Fixed: added the arm, corrected both comments, and widened the migration-note row, which had named two of the three affected operators — that row is what an application author reads to find out whether their code is affected, so a missing operator there is the part that actually costs someone.

- **The migration note gained a byte-order mark it should not have.** The implement handoff stated that `docs/migration-notes.md` is "CRLF with a byte-order mark and keeps both". It has no byte-order mark. My first edit of it wrote one (a `utf-8-sig` round trip), the diff showed line 1 changing, and it was removed; that file's final diff is the single table row. Flagged because the handoff's hygiene claim about that file is false and the next agent to edit it would repeat the mistake.

**Checked and found clean:**

- `tests/DAE.Server.Tests/coverage-manifest.json` needs no change — the `Internet` entry is `{"status": "pass", "register": ["System.Internet"]}` and both still hold. The harness fails any script whose entry does not match, so the green run is the check.
- No golden file governs this script: `tests/goldens/frontend/System.Internet` holds form goldens (`XML*.xml`) only, nothing for the coverage script's output.
- No other JSON reader exists in the tree. A grep for `DeserializeObject`, `JObject.Parse`, `JToken.Parse` and `JArray.Parse` across `src/` and `libraries/` hits exactly the three sites in `DynamicNodes.cs`; `RESTNodes.cs` and `InternetNodes.cs` do not read JSON at all. So the test's coverage claim is now complete, not merely broader.
- Source hygiene of the implement commit was correct as claimed for `Internet.d4` — tabs, CRLF, no byte-order mark — and is preserved. Zero lines carry a real trailing space or tab. `git diff --check` reports every CRLF line of that file as "trailing whitespace", including the 63 lines the implement commit itself added, so that output is noise here and not a finding.
- The error-message substring choice holds up. Asserting only that *some* error was raised would pass if the reader failed for an unrelated reason, which is worse than being tied to a third party's wording, and the `IndexOf(...) < 0` shape already reports whatever error did arrive.
- The duplication across the four try/except/`IndexOf` arms was weighed and left alone: D4 has no way to pass the body as a value, so the alternative is not a loop but a more contrived script. The one thing worth sharing, the expected substring, is already a variable (`LMaxDepthError`).
- File size: `Internet.d4` is 92 lines. Nothing to split.

**Tripwires parked (not filed):**

- The assertions match Newtonsoft's own message text, so an upgrade that moves the limit or rewords the message breaks them — on purpose, that is the signal. The implement pass left a comment naming the measured version (13.0.4) but did not tag it, so it was not in the greppable set; a `NOTE:` line was added at the top of the block pointing at `debt-library-upgrades-coverage` as the ticket that would meet the failure.

**Gaps left open deliberately, each with a reason:**

- **Only object nesting is tested, not arrays.** The limit was measured identical for both and the assertion uses neither the `Path` nor the `position`, which are the only parts that differ. An array case on a typed `Dynamic` variable was not measured, and the type is declared `from class "Newtonsoft.Json.Linq.JObject"`, so whether a `JArray` can be assigned to one is an open question — do not add that case without checking it first.
- **A deep value stored in a table is asserted neither way.** It round-trips fine in process because the in-memory device keeps the value as-is and never takes a physical representation, so no reader runs; an assertion here would record the memory device's storage strategy rather than the JSON limit. A *persistent* device does take a physical representation, but that path runs through the conveyor, which brings it back to the unreachable-conveyor defect already filed.
- **The conveyor's reader is untested because it is unreachable**, and is owned by `bug-json-conveyor-declares-itself-non-streaming`.

**Considered and declined — not re-filed:**

- The implement pass deliberately did not propose raising the limit or exposing a setting for it, on the grounds that version 13 lowered the default as a security measure, that a server reading JSON it did not produce wants that protection, and that a real report is what would justify making it configurable. That reasoning holds and the migration note already asks an affected author to report the problem rather than promising a setting. Nothing was filed to make the limit configurable.

**Correction to the handoff, no code effect:** both the handoff and the plan say the `Internet` case belongs to suite part 3. It belongs to **part 2** — part 2 matches coverage scripts whose name begins with `A`-`R` and part 3 matches the rest (`AGENTS.md`, "Agents run the suite in parts"). Nothing follows from it here, because the case was run directly by name, but a future agent re-running "the part that holds `Internet`" would run the wrong one.
