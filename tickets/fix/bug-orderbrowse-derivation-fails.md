description: When a user clicks the control that chooses how a list of records is sorted, the server fails to build the form that would let them choose, so sorting a browse list is unusable for almost every table.
architecture: docs/modernization-plan.md#ws3-test-suite
files: libraries/Frontend/Source/Production/DocumentBuilder.cs, libraries/Frontend/Source/Structuring/StructureBuilder.cs, libraries/Frontend/Source/Elaboration/Elaboration.cs
repro: verified
----
## What happens

Every browse form the Frontend server derives contains a search element whose order-lookup document is itself a derivation: `SearchStructureBuilder.GetOrderLookupDocument` (`libraries/Frontend/Source/Structuring/StructureBuilder.cs`, around line 771) builds a `Derive(<query>, 'OrderBrowse', …)` expression and puts it in the form. When the user opens that lookup — the "order by" chooser on a browse form — the client evaluates that expression, and the server raises:

```
Runtime error: Elaborated column "ID" not found.
```

The column named in the message is whichever column comes first in the table's order.

Measured on 2026-11-21 against a server with `Frontend`, `Security`, `Sample.Common`, `Sample.DetailAdd`, `Sample.Forms`, `Sample.IssueTracker` and `Sample.SendEmail` registered: of the 45 table variables those libraries define, `Frontend.Derive(<surface>, 'OrderBrowse')` fails on **40** and succeeds on **5** (`Security.ChangePassword`, `Security.ChangePasswordBase`, `Sample.IssueTracker.NewIssue`, `Sample.SendEmail.EmailSettings`, `Sample.SendEmail.SentEmail` — all of them surfaces with no visible order column). Every other page type (`Browse`, `List`, `Report`, `Add`, `Edit`, `View`, `Delete`) derives on all 45. Passing `false` for the elaborate flag and using the five-argument form of `Derive` both fail the same way.

## Where to look

`OrderBrowseDocumentBuilder.GetColumnTitle` in `libraries/Frontend/Source/Production/DocumentBuilder.cs` (around line 1203) reads:

```csharp
_derivationInfo.ElaboratedExpression.Columns[column.Name].GetTitleSeed()
```

`column` there is a `Schema.TableVarColumn` of the table variable, so `column.Name` is the column's own name (`ID`). `ElaboratedExpression.Columns` is keyed by *elaborated* name, which for the main table is qualified with the source name (`Main.ID`). That mismatch is the most likely cause of the message, and it is reached from `GetDefaultOrderTitle`, which is called for every visible order column — which is why the only surfaces that survive are the ones with no visible order column. This is inference from reading the code; the error text itself is verified. Confirm it before fixing, and check whether the same unqualified lookup appears elsewhere in the same file or in the elaboration code: if it is a class of mistake rather than one site, fix the class and say so.

Whatever the cause turns out to be, the fix belongs at one site in the derivation code. Do not change the error message or the `Derive` signature.

## Why it matters now

- It is reachable in normal use: it is the order chooser of every browse form, not an edge path.
- It is almost certainly not a porting defect — the code is unchanged from 3.1 — so the plan's "an assertion that fails on .NET 10 is a porting defect until shown otherwise" rule does not apply, but neither does "it must have worked before": the derivation engine has had no automated coverage, which is why this surfaced only now. Say which it is in the handoff if you can tell.
- There is no `prereq:` here on purpose, so this does not wait on the test work. **Check the board first**, because the two possible orders need different work:
  - If `test-frontend-derivation-golden` has already landed, it holds a one-row expected-failure table naming this slug, and these 40 cases are recorded as expected to fail. The fix then makes them succeed, which **fails** that test until 40 golden files are added and the row is removed. That is the intended forcing function: generate the goldens (`DATAPHOR_UPDATE_FRONTEND_GOLDENS=1`, then read the diff) and drain the row in the same commit.
  - If it has not landed, this ticket carries its own reproduction and nothing else; say so in the handoff, so that whoever implements the golden ticket afterwards measures 45 of 45 `OrderBrowse` cases deriving and writes no expected-failure table.

## What to produce

An `implement/` ticket that names the confirmed root cause and the one site, and carries the reproduction as a test. If `test-frontend-derivation-golden` has landed by then, it also drains that ticket's expected-failure row and generates the 40 new goldens.

The reproduction test belongs at the lowest layer that shows it: derive `OrderBrowse` for a surface with a visible order column and assert the form comes back. If the Frontend derivation fixture from `test-frontend-document-corpus` is already there, put the case on it; if not, a short fixture of its own on a `TestServerInstance` with `Frontend` registered is enough — `Frontend.Applications` reproduces it with no sample library at all. Once the golden set covers all 45 surfaces for `OrderBrowse`, that *is* the reproduction, so do not leave a second test asserting the same thing behind.

A behaviour change an application can observe — a form that used to error now opens — so it needs an entry in `docs/migration-notes.md` in the same commit.
