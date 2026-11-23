description: A test was added that records the exact point at which the upgraded JSON reader starts refusing deeply nested data, so that a customer with deeper data is not the one who discovers the new limit.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4, docs/migration-notes.md
difficulty: easy
----
## What landed

Two files changed, nothing else.

**`tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4`** — a block of assertions appended to the existing coverage script, exactly the block the plan ticket carried, plus two extra comment lines saying that the library's other JSON reader (`JSONObjectConveyor.cs`) is unreachable and is owned by `bug-json-conveyor-declares-itself-non-streaming`, so a reader of the test does not go looking for a conveyor case. The block asserts four things:

- a value nested in exactly 64 objects reads (`GetType(Dynamic(...)) = 'Object'`);
- 65 levels are refused by the `Dynamic(String)` selector, with an error whose message contains `MaxDepth of 64 has been exceeded`;
- 65 levels are refused the same way by the `Content` write accessor, the other JSON reader D4 can reach;
- writing is not limited, so a 71-level value built by repeated `Set` serializes through `Content` and then cannot be read back — the asymmetry is the finding most likely to surprise someone, and the comment above it says so.

Each failure assertion is `if IndexOf(LCaught, '...') < 0 then raise`, which catches both "it did not fail at all" (`LCaught` stays empty) and "it failed for some other reason" (the raised message carries whatever did arrive). The expected substring is deliberately the library's own wording minus the `Path` and `position` parts, which vary with the document's shape.

**`docs/migration-notes.md`** — one row appended to the `## D4, catalog and libraries` table, as the plan ticket proposed it verbatim: what the limit is, that Newtonsoft.Json 13 sets it and version 9 had none, that writing is unlimited so a value can be built deeper than it can be read, and that an affected application needs its documents or nesting flattened and should report the problem.

Source hygiene: `Internet.d4` is tabs, CRLF, no byte-order mark, and was appended in that form (verified by `cat -A` and a hex dump of the first bytes). `migration-notes.md` is CRLF with a byte-order mark and keeps both.

## Tests

| Test | What it verifies |
|---|---|
| The block appended to `Internet.d4` (one coverage case, `Internet`) | Where the Newtonsoft.Json 13 nesting limit sits (64 reads, 65 refused), that both reachable D4 JSON readers carry it, and that the write path does not — so a deep value the engine builds itself cannot be read back. |

That is the whole test debt this ticket owed: it is a characterization test for a behaviour change that came with a dependency upgrade, and there is no other new code to cover.

## Validation run

- `dotnet build tools/layout/Layout.csproj` — 0 errors (141 pre-existing warnings, unchanged).
- `dotnet test tests/DAE.Server.Tests --no-build --filter Name=Internet --logger "console;verbosity=normal"` — `pass: 1 passed, 0 failed`, case time 4 s, which matches the plan ticket's measurement of 4 s with and without the block. So suite part 3 gains no meaningful time.

The rest of suite part 1 was deliberately not re-run: the only changes are one coverage script and one markdown document, and the plan ticket says to re-run part 1 only if something outside those two changed. The `Internet` case itself is the part-3 coverage the change affects, and it was run.

No pre-existing failures surfaced; `tickets/.pre-existing-error.md` was not written.

## Known gaps, honestly

- **The assertion is tied to the library's wording.** `MaxDepth of 64 has been exceeded` is Newtonsoft's message text. A later upgrade that moves the limit or rewords the message breaks this assertion — on purpose, that is the signal, and `debt-library-upgrades-coverage` is the ticket that would meet it. The comment above the block names the version measured (13.0.4) so whoever meets the failure knows what to re-measure. A reviewer who dislikes asserting on a third party's message text has a real objection, but the alternative (asserting only that *some* error was raised) would pass if the reader failed for an unrelated reason, which is worse.
- **Only one nesting shape is tested.** Objects, not arrays. The limit was measured identical for both and only the message's `Path`/`position` differ, which the assertion does not use. An array case on a typed `Dynamic` variable was not measured — the type is declared `from class "Newtonsoft.Json.Linq.JObject"`, so whether a `JArray` can be assigned to one is an open question; do not add that case without checking it.
- **The conveyor's JSON reader is untested because it is unreachable.** `libraries/System.Internet/Source/JSONObjectConveyor.cs` implements the streaming read/write pair but leaves `IsStreaming` false, so no engine call site reaches it. Out of scope here, filed as `bug-json-conveyor-declares-itself-non-streaming` in `backlog/`.
- **A deep value stored in a table is not asserted either way.** It was measured to round-trip fine in process, because the memory device keeps the value as-is and never takes a physical representation, so no reader runs. An assertion here would be recording the memory device's storage strategy rather than the JSON limit. Worth knowing that a *persistent* device, which does take a physical representation, was not measured — but that path runs through a conveyor, which brings us back to the unreachable-conveyor defect above.

## The limit itself has no ticket behind it

This ticket deliberately does not propose raising the limit or exposing a setting for it, and nothing was filed to do so. Version 13 lowered the default as a security measure and a server reading JSON it did not produce wants that protection. The migration note therefore asks an affected application author to *report* the problem rather than promising a setting: a real report is what would justify making the limit configurable, and until one arrives the cheaper answer is to flatten the document. A reviewer who thinks a configurable `MaxDepth` should be queued now is reversing that judgement, not catching an omission — say so in the findings rather than treating it as a gap.

## No tripwires parked

Nothing conditional was noticed in the code touched; no `NOTE:` comments were added.
