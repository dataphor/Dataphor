description: A test claimed every D4 script file in the repository could be read, but thirty-eight of them could not. None turned out to be a defect in the reader, so the test now knows the real reasons: three files it had given up on are back under test, one dead file was deleted, and the rest are named with the exact line that stops the reader.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, docs/modernization-plan.md, src/engine/DAE/Schema/SQLDeviceMappings.d4
repro: verified
----
## What landed

Everything the implement ticket asked for, in one commit. No parser change: the investigation's finding that none of the thirty-eight failures is a parser defect held up, and nothing in `src/engine/DAE/Language.D4/` was touched.

**`tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs`** — four changes, all in the fixture:

- **`FormatTemplates`**, a new path → `object[]` list, keyed like `AllowList`. A file in it is passed through `String.Format` before the first parse, with the argument list its own device's C# call site passes; everything after that (emit, re-parse, compare trees) is unchanged. Three entries: the SQL Server and PostgreSQL `SystemCatalog.d4` with four arguments each, and the parked Oracle one with two. All three now appear as ordinary passing cases, so this is coverage recovered rather than a new exclusion.
- **The parked-tree rule** in `RoundTrips`: when the first parse produces messages *and* the path is under `legacy/`, the case reports `Assert.Ignore` with the path and the first message instead of failing. The twenty `legacy/` entries are gone from `AllowList`. The ordering matters and is deliberate — a parked document that *does* parse still has to round-trip through the emitter, which is what keeps the 58 readable parked documents testing the emitter.
- **`TheParkedTreeIsNoLessReadableThanItWas`**, the ratchet: walks the parked tree with the same enumeration and the same template formatting the cases use, counts the documents whose first parse produces messages, and asserts the count is at most `ParkedDocumentsTheParserCannotRead = 19`. It also asserts the walk found at least one parked file at all, so the ratchet cannot pass vacuously if `legacy/` is moved or renamed. The failure message lists every unreadable path, so a reader sees which one changed rather than only that the number did.
- **`EveryExcludedFileIsStillWhatItsReasonSays`**, the two list-versus-tree assertions plus a third: every path in `AllowList` or `FormatTemplates` names a file that exists; no path is in both lists (that would silently exclude a template rather than format it, since `D4Files` skips the allow list); and every `AllowList` path still produces at least one parser message, failing with "these files now parse; remove their entries".

`AllowList` is now fifteen entries, each carrying the file's nature plus the line and token the parser stops at, and naming `test-pre-3-1-scripts-policy` on the eight it owns. The three-category history (the two emitter classes and the thirty-eight-file parse class) is kept as a comment so a reader meets it where the list lives.

**Deleted `src/engine/DAE/Schema/SQLDeviceMappings.d4`** (1,657 lines) with `git rm`, and dropped its `AllowList` entry. Both facts the implement ticket asked to be re-confirmed before deleting were re-confirmed, and are worth re-checking in review because the deletion rests on them:

- `src/engine/DAE/DAE.csproj` names its embedded resources one by one with no globs (lines 275–296). `Schema\DataTypes.d4` and `Schema\SystemCatalog.d4` are there; `Schema\SQLDeviceMappings.d4` is not.
- A repository-wide text search for `SQLDeviceMappings`, excluding `artifacts/` and `tickets/`, found exactly one hit before the change — the allow-list entry itself. Nothing else: no `.csproj` item, no `.d4l`, no C#. After the change the only hit is the plan's own account of the deletion.

So **no `docs/migration-notes.md` entry**: the file was never compiled into an assembly, never listed by a `.d4l` and never read by any code, so nothing an application, a deployment script or a C# extension does can notice it going. Git keeps it if that judgement turns out wrong.

**`docs/modernization-plan.md`** — the `test-parser-roundtrip-corpus` row (line 330): the case count moved 350 → 372, and the trailing "one named allow list of 38 files the parser cannot read at all … owned by `bug-d4-parser-rejects-active-sources`" is replaced by what is now true, split so the thirty-eight add up without double-counting (3 templates + 19 parked + 1 deleted orphan + 15 named = 38; the Oracle template is both a template and a parked document, which is where the arithmetic is easy to get wrong).

## Measured

Build clean. The corpus pass:

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest"
```

- Before: 352 tests, 352 passed, 38 files excluded.
- After: 376 tests, 357 passed, 19 Ignored, 0 failed. That is 372 corpus cases (350 + 22: the twenty-three entries dropped from the list, less the deleted orphan, which contributes no case) plus the two comparator smokes plus the two new tests.
- The nineteen Ignored are exactly the twenty former `legacy/` entries less the Oracle template, which now passes.
- The three template cases pass: `libraries/MSSQLDevice/Source/SystemCatalog.d4`, `libraries/PGSQLDevice/Source/SystemCatalog.d4`, `legacy/libraries/OracleDevice/Source/SystemCatalog.d4`.

The whole suite, as the three parts in the root `AGENTS.md`, each its own foreground command: part 1 `0 failed, 661 passed, 19 skipped` in 6 m 6 s; part 2 `0 failed, 67 passed, 11 skipped` in 3 m 6 s; part 3 `0 failed, 50 passed, 12 skipped` in 3 m 6 s. Part 1's nineteen skips are the new parked Ignores; parts 2 and 3's are the pre-existing coverage-script known failures. No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written.

## Tests added, and what each verifies

Two, both encoding a claim the hand-written lists make about the tree. Neither is wiring: they assert something no type or compiler check can, and both were confirmed to bite by temporary mutation (described below) rather than assumed to.

| Test | Verifies |
|---|---|
| `TheParkedTreeIsNoLessReadableThanItWas` | The number of parked documents the parser cannot read is at most 19, and the walk finds parked files at all. This is the only thing standing between the new `Assert.Ignore` rule and the parked tree silently stopping to test anything. |
| `EveryExcludedFileIsStillWhatItsReasonSays` | Every path in either list exists; no path is in both; every `AllowList` path still fails to parse. The last arm is what stops a reason outliving its cause — the way a suppression quietly becomes permanent. |

The three format templates deliberately get **no test of their own**: they are three ordinary corpus cases now, and a separate test would assert the same thing twice.

### Mutation checks run (and reverted)

Each of these was applied, built, run, and reverted from a scratchpad copy of the file — no `git checkout`, `restore` or `stash` was used anywhere in this ticket.

- `ParkedDocumentsTheParserCannotRead` lowered to 18 → the ratchet fails with "19 of the 77 parked documents do not parse, up from 18 … raise `ParkedDocumentsTheParserCannotRead` in the same commit", and lists all nineteen.
- An `AllowList` entry added for `samples/Sample.Common/Documents/Register.d4`, which parses → "These files now parse; remove their AllowList entries so the corpus covers them", naming it.
- An `AllowList` entry added for a path that does not exist → "These entries name no file under … Delete the entry, or find out what became of the file", naming it.
- The MSSQL template's argument list cut from four to two → the `FormatException` arrives naming the file and saying the argument list has to match its device's C# call site, rather than `String.Format`'s bare index message. (The wording was then tidied: the inner message now trails the instruction rather than colliding with its full stop.)

## What a reviewer should look at hardest

- **The deletion.** It is the only irreversible-looking change, and it rests on the two re-confirmations above. Re-run the grep and re-read `DAE.csproj`'s resource list if there is any doubt; the file is one `git revert` away if the judgement is wrong. The implement ticket's rejected alternative — moving it under `legacy/` — still looks right to reject: `legacy/` holds parked *libraries* (a `.d4l` with its sources), and this file belongs to no library.
- **The Ignore rule's placement.** It is inside the `if (firstMessages.Count > 0)` branch, after the first parse, not a path-based skip in `D4Files`. Move it earlier and the parked tree stops testing the emitter — 58 cases' worth.
- **A parked document that makes the parser *throw*** (rather than collect messages) still fails its case. That asymmetry is intentional: with a non-null `ParserMessages`, `ParseScript` turns every syntax error into a message and returns (`src/engine/DAE/Language.D4/Parser.cs:55`), so a throw from a parked file would be something else entirely and worth seeing. It is untested because nothing in the tree does it.
- **The fifteen reasons** are prose written from parsing each file individually on 2026-11-22. The assertion holds that each file still *fails*, not that it fails *for the stated reason* — a reason could drift from its cause without anything noticing. Pinning the message text instead was considered and not done: the parser's message text is not a compatibility surface, and a test that pins it would fail on an unrelated message reword.

## Known gaps

- **The ratchet bounds a count, not a set.** A regression that breaks one parked document while a different one starts parsing keeps the count at 19 and passes. Recorded as a `NOTE:` at the `Assert.Ignore` site, with what to do if the parked trees ever matter more than that (hold the set of paths).
- **The seven supported-tree documents are not repaired**, by design: whether a D4 document's statements may be edited to make it run is an open owner decision on `test-pre-3-1-scripts-policy`, which carries these seven as its second arm. `samples/Sample.Common/Documents/D4LanguageTour.d4` is the one with user-visible weight — a reader who executes the whole guided tour rather than one statement at a time gets a syntax error at line 133.
- **`coverage-manifest.json` untouched**, as the implement ticket directed: its eight Coverage.Scripts entries already point at `test-pre-3-1-scripts-policy` with matching error text, and changing them is that ticket's job.
- **`SamplesCorpusTest`'s accepted-tradeoff note** partly rests on "every `.d4` file in the tree is already parsed by `ParserRoundTripCorpusTest`". Checked rather than assumed: no `Sample.Forum` or `Sample.Pubs` path appears in either list, and neither is under `legacy/`, so both samples' `.d4` files are still ordinary cases. No edit needed there.

## Tripwires recorded

- The ratchet's count-not-set blind spot — `NOTE:` at the `Assert.Ignore` site in `RoundTrips`, as above.
