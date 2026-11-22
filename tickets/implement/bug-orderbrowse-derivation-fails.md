description: When a user clicks the control that chooses how a list of records is sorted, the server fails to build the form that would let them choose, so sorting a browse list is unusable for almost every table. One wrong line in the form-building code looks a column up under the wrong name; correct it and record the 47 forms that start working.
architecture: docs/modernization-plan.md#ws3-test-suite
files: libraries/Frontend/Source/Production/DocumentBuilder.cs, tests/DAE.Server.Tests/FrontendDerivationGoldenTest.cs, tests/goldens/frontend/, AGENTS.md, docs/modernization-plan.md, docs/migration-notes.md
repro: verified
----
## The root cause, confirmed

One line, `libraries/Frontend/Source/Production/DocumentBuilder.cs:1211`, in `OrderBrowseDocumentBuilder.GetColumnTitle`:

```csharp
_derivationInfo.ElaboratedExpression.Columns[column.Name].GetTitleSeed()
```

`column` is a `Schema.TableVarColumn` of the table variable being derived, so `column.Name` is the column's own name (`ID`). `ElaboratedExpression.Columns` is an `ElaboratedTableVarColumns`, whose string indexer matches on `ElaboratedTableVarColumn.ElaboratedName` (`libraries/Frontend/Source/Elaboration/Elaboration.cs:1324`) - and that name is the column's name qualified with its elaborated table's name, so `Main.ID` for a column of the main table (`Elaboration.cs:1199` and `:1211`, the two constructors that call `Schema.Object.Qualify(column.Name, _elaboratedTableVar.ElaboratedName)`). The lookup misses and the indexer raises Frontend error 200127, `Elaborated column "{0}" not found`.

`GetColumnTitle` is reached from `GetDefaultOrderTitle`, which `GetOrderExpression` calls for every visible column of every visible order, which is why the only five surfaces that derived at all were the ones with no visible order column.

**The fix is to qualify the name before the lookup**, as the two sibling lookups in `SearchStructureBuilder.Build` already do (`libraries/Frontend/Source/Structuring/StructureBuilder.cs:828` and `:850`, which build the same kind of name with `Schema.Object.Qualify(…, DerivationUtility.MainElaboratedTableName)` and then index `Columns` with it):

```csharp
_derivationInfo.ElaboratedExpression.Columns[Schema.Object.Qualify(column.Name, DerivationUtility.MainElaboratedTableName)].GetTitleSeed()
```

Both `Schema.Object` and `DerivationUtility` are already used on neighbouring lines of the same method, so no new `using` is needed.

**Measured, 2026-11-21.** With exactly that change and nothing else, `dotnet build Dataphor.slnx` reported 0 errors and `dotnet test tests/DAE.Server.Tests --no-build --filter Name=EveryDerivedFormMatchesItsGolden` failed with exactly 48 problems: 47 of the form `OrderBrowse of <surface>: no golden file`, one for each of the 47 cases that fail today, plus one `The expected-failure row OrderBrowse of * (bug-orderbrowse-derivation-fails) matches no case that fails that way any more`. Nothing else in the 416-case corpus changed - the five `OrderBrowse` goldens that already exist still matched, and so did the other 364. So every one of the 47 derives after the change, and the change touches nothing that already worked. The experiment was then reverted; the fix is not in the tree.

**One site, not a class.** Every other string-keyed lookup into an `ElaboratedTableVarColumns` under `libraries/Frontend/Source` qualifies the name first: `Elaboration.cs:902`, `Elaboration.cs:1011`, `StructureBuilder.cs:828`, `StructureBuilder.cs:850`. This is the only one that does not, so fix the site and no more.

**Not a porting defect.** The unqualified lookup is in the initial open-source commit (`361ebd83`) and has only ever been touched by the identifier rename in `01f32a20` ("New coding conventions"). It is a 3.1-era defect that nothing had exercised, because the derivation engine had no automated coverage until `test-frontend-derivation-golden`. Say so in the handoff; do not describe it as a .NET 10 regression.

**What the new goldens should say.** `GetTitleSeed` walks the column's elaborated table variable's `ElaboratedReference` chain to build a group-title prefix; the main elaborated table variable is the root of that chain and has no reference, so the seed is empty for every column this path looks up. The fix therefore adds no prefix to any title: the order descriptions should read `by ID`, `by Name, ID`, `by ID (descending)` and so on, carried in the `expression` attribute of the form's `Main` source as a `table of { OrderName : String, Description : String }` selector with one `row` per visible order. Compare with `tests/goldens/frontend/Security/ChangePassword.OrderBrowse.xml`, one of the five that derive today, whose selector is empty because that surface has no visible order. A description that reads `by ` with nothing after it, or that carries a stray group title, means something other than this defect is in play - stop and say so rather than committing it.

## The forcing function, and what has to move with the fix

`FrontendDerivationGoldenTest` holds one expected-failure row for this slug, and a row that matches no failing case fails the run. So the fix, the 47 new golden files and the removal of that row are one commit. Generate the goldens with `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` (the run writes the files and then fails on purpose, so that an update cannot be mistaken for a pass), read the diff, and only then drain the row.

After the fix the corpus is 416 cases, all 416 deriving, 416 golden files (369 today), and no expected-failure rows. Four places record the current 369/47 shape in prose and each needs the new numbers:

- `tests/DAE.Server.Tests/FrontendDerivationGoldenTest.cs` - the `ExpectedFailures` row and the comment above it; the class remarks' corpus sentence ("416 cases, of which 369 derive and 47 fail, all of them OrderBrowse"); and the determinism paragraph, whose "all 369 forms that derive came out byte-identical" is a dated measurement over the set that derived then - either say that explicitly or re-measure, but do not silently restate it as 416.
- `AGENTS.md`, the *Derived form goldens* paragraph: "Today one entry covers the 47 `OrderBrowse` cases that fail (`bug-orderbrowse-derivation-fails`); the five that derive have goldens like any other case."
- `docs/modernization-plan.md`, the WS3 work-item row for `test-frontend-derivation-golden`: the file count and size, and "today one row covers the 47 `OrderBrowse` cases owned by `bug-orderbrowse-derivation-fails`".
- `docs/migration-notes.md`, under *Forms and the Windows client*: a `done` entry, because an application can see this - the order chooser of every derived browse form used to raise an error and now opens. Nothing for the application to do.

Keep the `ExpectedFailure` type, its `"*"` matching and the `NOTE:` above the array when the array empties: an empty array is where the next defect of this kind is recorded, and the surrounding code handles an empty one correctly (no row to match, so an unexpected failure is reported with its error).

Add the defect to the plan's *Known defects* table as the next free K number, in the same shape as its neighbours: what was wrong, the one file, `fixed`, and this slug.

**No new test.** The 47 goldens are the reproduction, at the lowest layer that shows the defect. Do not add a separate case asserting that `OrderBrowse` derives - the fix ticket's instruction, and a second assertion of the same thing is one more thing to maintain.

## TODO

- Qualify the column name at `libraries/Frontend/Source/Production/DocumentBuilder.cs:1211` with `DerivationUtility.MainElaboratedTableName`, as `StructureBuilder.cs:828` does. Preserve the file's tabs, CRLF line endings and absence of a byte-order mark.
- `dotnet build Dataphor.slnx` (0 errors expected).
- Generate the goldens: `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1 dotnet test tests/DAE.Server.Tests --no-build --filter Name=EveryDerivedFormMatchesItsGolden`. Expect "wrote 416 golden file(s) and deleted 0" and a deliberate failure.
- Read the diff: 47 new files, no change to the 369 existing ones. Check a handful of the new `expression` attributes against the shape described above before committing them.
- Drain the expected-failure row and update the class remarks' counts in `FrontendDerivationGoldenTest.cs`.
- Re-run the golden case with the variable unset; it must pass.
- Update the *Derived form goldens* paragraph in `AGENTS.md` and the `test-frontend-derivation-golden` row in `docs/modernization-plan.md#ws3-test-suite`.
- Add the K entry to the plan's *Known defects* table and the `done` entry to `docs/migration-notes.md` under *Forms and the Windows client*.
- Run the suite in the three parts `AGENTS.md` names, each as its own foreground command, after `dotnet build Dataphor.slnx`.
