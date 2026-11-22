description: The control that lets a user choose how a browse list is sorted could not be built by the server, so sorting was unusable for almost every table. One line that looked a column up under the wrong name is corrected, and the 47 forms that start working are now recorded as checked-in reference files.
architecture: docs/modernization-plan.md#ws3-test-suite
files: libraries/Frontend/Source/Production/DocumentBuilder.cs, tests/DAE.Server.Tests/FrontendDerivationGoldenTest.cs, tests/goldens/frontend/ (47 new OrderBrowse files), AGENTS.md, docs/modernization-plan.md, docs/migration-notes.md
repro: verified
----
## What changed

**One line of product code.** `OrderBrowseDocumentBuilder.GetColumnTitle` (`libraries/Frontend/Source/Production/DocumentBuilder.cs:1211`) indexed the elaborated expression's column collection with the column's own name (`ID`). That collection is keyed by the *elaborated* name - the column's name qualified with its elaborated table's name, so `Main.ID` - so the lookup missed and the indexer raised Frontend error 200127, `Elaborated column "ID" not found`. The name is now qualified with `DerivationUtility.MainElaboratedTableName` before the lookup, exactly as the two sibling lookups in `SearchStructureBuilder.Build` (`libraries/Frontend/Source/Structuring/StructureBuilder.cs:828` and `:850`) already did. No new `using`; both symbols were already used on neighbouring lines.

`GetColumnTitle` is reached for every visible column of every visible order, which is why the `OrderBrowse` page - the order chooser a user opens to sort a browse list - failed for 47 of the 52 surfaces in the derivation corpus. The five that derived were the surfaces with no visible order column, so nothing reached the lookup.

**47 new goldens.** `tests/goldens/frontend/*/*.OrderBrowse.xml`, generated with `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1`. These are the reproduction, at the lowest layer that shows the defect; no separate assertion that `OrderBrowse` derives was added, per the fix ticket.

**The expected-failure row is drained.** `FrontendDerivationGoldenTest.ExpectedFailures` is now `[]`, with the `NOTE:` above it kept and rewritten to say that an empty array is where the next derivation defect gets recorded. The `ExpectedFailure` type and its `"*"` matching are untouched.

**Prose counts.** The class remarks' corpus sentence (416 cases, every one derives) and determinism paragraph, `AGENTS.md`'s *Derived form goldens* paragraph, the plan's `test-frontend-derivation-golden` work-item row, the plan's *Known defects* table (new row **K36**) and `docs/migration-notes.md` under *Forms and the Windows client* (a `done` entry, nothing for an application to do).

**Not a porting defect.** The unqualified lookup is in the initial open-source commit and was only ever touched by the identifier rename in `01f32a20`. It is a 3.1-era defect that nothing exercised, because the derivation engine had no automated coverage until `test-frontend-derivation-golden`. K36 says so.

## What was validated, and how

- `dotnet build Dataphor.slnx` - 0 errors (143 warnings, all pre-existing).
- `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1 dotnet test tests/DAE.Server.Tests --no-build --filter Name=EveryDerivedFormMatchesItsGolden` - "wrote 416 golden file(s) and deleted 0", plus the deliberate update-run failure and the row-drain message. Exactly 47 files appeared; **no tracked golden changed**, so the fix touched nothing that already worked.
- `dotnet test tests/DAE.Server.Tests --no-build --filter Name=EveryDerivedFormMatchesItsGolden` with the variable unset - **passes**.
- Whole suite, the three parts `AGENTS.md` names, each in the foreground after the build: 13 passed (Common.Tests) + 561 passed (DAE.Server.Tests, part 1); 67 passed / 11 skipped (part 2); 49 passed / 13 skipped (part 3). **No failures, nothing pre-existing to report** - `tickets/.pre-existing-error.md` was not written.

## Determinism was re-measured, not restated

The class remarks' determinism paragraph previously claimed byte-identity over "all 369 forms that derive", measured when 47 did not derive. Rather than silently restating that over 416, it was re-measured: the update run was executed three times, each run creating its own server instance. Across all three, no tracked golden showed a modification, and the 52 `OrderBrowse` goldens hashed identically (sha256, 52 of 52) between the second and third runs. So "all 416 forms came out byte-identical on two independently created server instances" is measured. The within-one-instance half of the sentence is still attributed to the original 369-case measurement, which is what it covers; `DerivingTheSameFormTwiceGivesTheSameText` pins that property for one case.

## Use cases for a reviewer to check

**The derived form a user sees.** Open any derived browse form's order chooser. In golden terms, `tests/goldens/frontend/Security/Role.OrderBrowse.xml` now carries a `Main` source whose `expression` is a one-row selector, `table of { OrderName : String, Description : String } { row { "by Name" Description, "order { Name asc include nil }" OrderName } } order by { OrderName }`. Before the fix that derivation threw.

**The shape the fix ticket predicted, confirmed.** `GetTitleSeed` returns the empty string for every column this path looks up, because the main elaborated table variable is the root of the `ElaboratedReference` chain. So no title gained a group-title prefix. Spot-checked across all 47: every description reads `by <titles>`; none is a bare `by `; none carries a prefix. Multi-column examples: `by User ID, Role Name` (`Security/UserRole`), `by Product ID, Version, Priority` and `by Description, Priority` (`Sample.IssueTracker/Issue`), `by Library Name, Name` (`Frontend/Documents`).

**Worth a second opinion - duplicate titles.** `System.Internet/XMLElementParent.OrderBrowse.xml` reads `by Element ID, Element ID`: its order is `{ Parent_Element_ID, Element_ID }` and both columns take their title from the `XMLElementID` domain, so the two order columns are indistinguishable in the chooser. This is the engine's existing titling behaviour and not an artifact of the fix - the same surface's `Browse` golden has carried two `title="Element ID"` grid columns since the goldens were created, and that page did work in 3.1. Left as-is deliberately: deciding that an order column's title should be disambiguated would change titling for forms that work today, which is a compatibility surface and a separate ticket if anyone wants it. Flagged here rather than filed, because it is a judgement call a reviewer may see differently.

## Known gaps, honestly

- **`(descending)` is still unexercised.** `GetDefaultOrderTitle` appends " (descending)" for a descending order column. No table variable in any of the eight corpus libraries declares one, so not one of the 416 goldens contains the word. The branch is one line and reads correctly, but the goldens do not cover it. A library with a descending order would cover it for free; nothing was added to force it.
- **The "no expected-failure row covers it" branch is not exercised.** With the array empty, an unrecorded failure takes `Unexpected`'s first message. That branch has never run in a green state (before, every failure matched the `"*"` row). It is read-only string formatting; no test was added for it.
- **The plan's golden-tree size figure changed basis, not content.** The row said "369 files, 2.0 MB"; it now says "416 files, 1.2 MB of text". 1.2 MB is the summed byte length (1,261,532 bytes), of which the 369 pre-existing files are 1.13 MB and the 47 new ones 74.7 KB. The old "2.0 MB" cannot have been apparent size - it looks like a block-rounded `du` figure (current `du -sk` is 2.2 MB). The new number is honest and its basis is now stated in the row; if a reviewer prefers the on-disk basis for continuity, it is 2.2 MB.
- **The 47 forms never rendered in any shipped version**, so these goldens are not a record of ported 3.1 output - there was none. They are the record of what the engine's existing elaboration, structuring and titling rules produce once the lookup works, which is why the shape check above was done against the rules rather than against a reference.

## Source hygiene

`DocumentBuilder.cs` and `FrontendDerivationGoldenTest.cs` keep their tabs and CRLF line endings; the first has no byte-order mark and still has none, the second has one and still has it. `AGENTS.md` and `docs/modernization-plan.md` keep CRLF, `docs/migration-notes.md` keeps LF. The new goldens are UTF-8 with no byte-order mark and LF newlines, as the writer and the existing 369 are. No file was reformatted or re-encoded.
