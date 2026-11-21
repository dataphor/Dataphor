description: A test script was being refused permission to act as a user it had just created, and the refusal turned out to be correct — the script had not yet saved that user when it tried to use it. The test runner now saves each step of the script before the next one runs, and the whole script passes.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/coverage-manifest.json, docs/modernization-plan.md
repro: verified
difficulty: easy
----
## What landed

No engine change. The fix ticket's premise — that the engine's right check gave a wrong answer — was wrong, and the implement ticket had already established why. The finding stands: the refusal was correct, and the defect was in how the coverage harness ran the script.

Two files changed, both documentation-or-test-configuration:

**`tests/DAE.Server.Tests/coverage-manifest.json`** — the `ServerLink` entry. Was `"status": "known-failure"` with `"ticket"` and `"error"` fields; is now:

```
{ "script": "ServerLink", "status": "pass", "batches": true, "linkedServer": true, "note": "batch by batch because the script creates a user and then runs a block as that user through ExecuteAs, which opens a session of its own: nested in one statement, the new session cannot see the uncommitted user" },
```

`batches` is a pre-existing manifest option, already documented in `CoverageManifest.cs` for exactly this case and already used by `ApplicationTransactions`. Nothing else in the manifest changed.

**`docs/modernization-plan.md`** — the two passages that described this as an engine defect. In the WS3 **What the script found** paragraph: the count became "All 126 of the `ServerLink` script's statements pass"; the sentence about the script stopping at line 43 on a rights check was replaced with the actual explanation (`ExecuteAs` opens a session of its own, which cannot see a user created by the enclosing statement's still-open transaction, so the runner runs the script batch by batch). One extra edit beyond the ticket's list: that paragraph's middle sentence read "One fails, and it is a real defect the script was written to catch" about the *already-fixed* session-reuse bug — with the count now at 126/126 that present tense contradicted itself, so it is in the past tense ("One did fail, and it was a real defect…"). In the **K35** row: the closing clause now says the coverage script passes whole, run batch by batch, instead of naming this ticket.

No new K row (no defect to record) and no `docs/migration-notes.md` entry (nothing an application, deployment script or C# extension could notice — a test manifest and the plan).

## Tests

**No new test.** The reproduction *is* the `ServerLink` coverage script, which this ticket converts from a known failure into a permanent passing assertion — the lowest layer that shows the behaviour. The underlying engine behaviour (a session cannot use a grant whose transaction has not committed) is already pinned by `UncommittedGrantNotUsableInOtherSession` in `tests/DAE.Server.Tests/CatalogStoreTest.cs`.

The implement ticket considered and declined a manifest guard that would fail any coverage entry whose script mentions `ExecuteAs`/`ExecuteOn`/`EvaluateOn` without `batches` or the implicit-transactions preamble: three entries handle this by hand today, the failure mode is loud, and a textual guard over D4 source would misfire on comments and on scripts that pass anyway. That judgement is unchanged and is **not** recorded as a code `NOTE:` — it is a hypothetical guard with no site to attach to, and the manifest note on the `ServerLink` entry already tells the next reader what the trap is. If a fourth script lands in it, that is the moment to reconsider.

## What to validate

The manifest is an **embedded resource**, so a rebuild is required between editing it and running the test — a stale run will report the old known-failure.

- `dotnet build Dataphor.slnx` — 0 errors (143 warnings, all pre-existing).
- `dotnet test tests/DAE.Server.Tests --no-build --filter Name=ServerLink --logger "console;verbosity=normal"` — observed `pass: 1 passed, 0 failed`, 9 s. Every block after the script's line 43, which had never executed in a whole-script run, now does.
- Suite in three parts per root `AGENTS.md`, each foreground. Observed: part 1 (non-coverage) 13 + 444 passed, 0 failed; part 2 (coverage A–R) 67 passed, 11 known-failure matched, 0 failed; part 3 (coverage, rest) 49 passed, 10 known-failure matched, 0 failed. Part 3 is the one holding `ServerLink`: its pass count is one higher and its known-failure count one lower than before, as expected, and the per-ticket breakdown no longer lists this ticket's slug.

## Known gaps for the reviewer

- **The script's own assertions are only as good as the script.** It now runs end to end, but the blocks after line 43 had never executed before this change, so this is the first run in which they assert anything. They passed; nobody has read them against the features they claim to cover. If the reviewer wants a check beyond "no error", reading the post-line-43 blocks of the `ServerLink` script is where the value is.
- **`batches` vs the preamble.** `preamble: "SetUseImplicitTransactions(false);"` would also work and was rejected in favour of `batches` (narrower, and matches how Dataphoria runs a script, which is how these scripts were written to be run). Reasonable to disagree with; it is one manifest field either way.
- The plan's prose edits are judgement calls about tone and tense, not facts — worth a read for whether the WS3 paragraph still scans as one argument after the tense change.
