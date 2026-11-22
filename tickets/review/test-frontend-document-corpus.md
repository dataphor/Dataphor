description: Review the new tests that read every form file in the repository and prove that applying a form customization and then subtracting it again gets the original form back.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/FrontendFixture.cs, tests/DAE.Server.Tests/FormDocumentCorpusTest.cs, tests/DAE.Server.Tests/FormCustomizationTest.cs, tests/DAE.Server.Tests/TestEnvironment.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/modernization-plan.md
difficulty: medium
----
## What landed

All of it is new test code in `tests/DAE.Server.Tests` plus two paragraphs of documentation. No product code changed, so nothing an application, a deployment script or a C# extension can observe changed, and `docs/migration-notes.md` has no new entry.

Vocabulary, because the extensions are opaque: a **`.dfd`** is a *form document* — XML describing a form's node tree. A **`.dfdx`** is a *customized form document*: a small wrapper naming one or more **ancestors** (D4 expressions, in practice `Derive('<table>', '<page type>')`) plus a **delta** of changes to apply on top of them. Loading one means evaluating the ancestor expressions, merging the results, then merging the delta over that. The merge and its inverse are `Merge` and `Diff` in `src/engine/Base/BOP/Inheritance.cs`, reachable from D4 as `Frontend.Merge` and `Frontend.Difference`.

### Files

- **`tests/DAE.Server.Tests/FrontendFixture.cs`** — an abstract fixture over `InProcessTestFixture`: one in-process server carrying ten libraries (`System.Platform`, `Frontend`, `System.Internet`, `SimpleDevice`, `Security`, `Sample.Common`, `Sample.DetailAdd`, `Sample.Forms`, `Sample.IssueTracker`, `Sample.SendEmail`), plus four small helpers that call `Frontend.LoadAndProcess`, `Frontend.Merge` and `Frontend.Difference` with the documents passed as D4 parameters rather than as literals. No assertions: `test-frontend-derivation-golden` runs on this same fixture and uses its public `FrontendLibraries` list to enumerate surfaces.
- **`tests/DAE.Server.Tests/FormDocumentCorpusTest.cs`** — the no-server half, plus the `FormDocument` type and the directory walk both halves share.
- **`tests/DAE.Server.Tests/FormCustomizationTest.cs`** — the server half, on `FrontendFixture`.
- **`tests/DAE.Server.Tests/TestEnvironment.cs`** — one new accessor, `RepositoryDirectory`, reading a new `DataphorRepositoryDir` assembly-metadata entry that `DAE.Server.Tests.csproj` records; it checks `Dataphor.slnx` is there and says what to rebuild if not. No path is spelled in code.

## Tests added, and what each verifies

| Test | Cases | Verifies |
|---|---|---|
| `FormDocumentCorpusTest.Reads` | 76 (one per tracked `.dfd` and `.dfdx`) | A `.dfd` parses as XML, its root is `<interface>`, and it declares the BOP namespace. A `.dfdx` reads through `BOP.DilxDocument`, names at least one ancestor, and its embedded document parses as XML with an `<interface>` root. |
| `FormDocumentCorpusTest.TheWalkFindsBothKindsOfFormDocument` | 1 | The discovery walk found at least one of each extension, so a renamed folder cannot turn the corpus silently into zero cases. |
| `FormDocumentCorpusTest.CustomizationReadKeepsCharacterReferencedWhitespace` | 1 | A tab written as `&#x9;` inside an attribute of an embedded document survives being read; a tab the file stores literally arrives as a space. This is the mechanism the `NOTE` in `Dilx.cs` is about. |
| `FormCustomizationTest.LoadsThroughTheServer` | 26 (one per `.dfdx`) | For the 8 loadable ones: `Frontend.LoadAndProcess` returns a document whose root is `<interface>`, which covers the document device's read path, `DilxDocument`, `FrontendDevice.MergeAncestors` and `Inheritance.Merge` in one call. For the 18 excluded ones: the load still fails, so the exclusion list drains when a cause is fixed. |
| `FormCustomizationTest.CustomizationRoundTrips` | 8 | `Frontend.Merge(ancestors, Frontend.Difference(ancestors, loaded))` is XML-equal to `loaded`, where `ancestors` is the customization's ancestor expressions evaluated and folded. This is the property: the delta a designer would write from a customized form reproduces that form when merged back. |
| `FormCustomizationTest.SyntheticRoundTrip_ElementAdded` | 1 | Same round trip over a hand-written pair whose only change is an element added. |
| `FormCustomizationTest.SyntheticRoundTrip_SiblingsReordered` | 1 | Same, two existing sibling elements swapped. |
| `FormCustomizationTest.SyntheticRoundTrip_AttributeRemoved` | 1 | Same, one attribute removed. |
| `FormCustomizationTest.SyntheticRoundTrip_ElementRemoved_IsNotExpressible` | 1 | A removal produces an **empty** difference and merging it back leaves the original unchanged — see *Measured* below. |
| `FormCustomizationTest.MultiLineScriptInAnAttributeSurvivesLoading` | 1 | The one document in the tree with a multi-line script in an attribute value keeps its line breaks and its quotes through a load, and its one literal tab arrives as a single space. Asserted on the attribute value, not only through document equality. |
| `FormCustomizationTest.DeriveRejectsThePageType` | 3 | `Frontend.Derive` refuses `Manage`, `Preview` and `Filter` with an error naming the page type. |
| `FormCustomizationTest.DeriveAcceptsTheBrowsePageType` | 1 | The other side of that: `Browse` on the same surface does derive, so the refusal is about the page type and not the surface. |
| `FormCustomizationTest.EveryExclusionNamesADocumentInTheTree` | 1 | No exclusion entry names a file that is no longer in the repository. |

122 cases in total (78 + 44).

## How to run and validate

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~FormDocumentCorpusTest|FullyQualifiedName~FormCustomizationTest"
```

Expect `Failed: 0, Passed: 122`. `FormDocumentCorpusTest` alone runs in about a second and needs no server; `FormCustomizationTest` starts one server and took about 8 s.

The whole suite was run in the three parts AGENTS.md names, each in the foreground, all green:

| Part | Result |
|---|---|
| everything but the coverage scripts | 575 passed, 0 failed (`DAE.Server.Tests`), 13 passed (`Common.Tests`) — 5 m 10 s |
| coverage scripts A–R | 67 passed, 11 skipped, 0 failed — 3 m 8 s |
| coverage scripts not A–R | 49 passed, 13 skipped, 0 failed — 3 m 7 s |

No pre-existing failure surfaced, so `tickets/.pre-existing-error.md` was not written.

## What was measured

- **BOP inheritance has no operation that removes an element.** `Inheritance.Diff` walks the children of the *modified* document, so a child only the original has contributes nothing to the delta, and the merge leaves it where it was. Measured: the difference between the synthetic base and a copy with one `<textbox>` removed is an empty `<interface/>`, and merging it back reproduces the base, element and all. This is the machinery as designed — a node of a derived form is overridden, never deleted — so it is recorded as its own case (`SyntheticRoundTrip_ElementRemoved_IsNotExpressible`) rather than filed as a defect. A later attempt to add a deletion operation has to come past that case.
- **Literal tabs in an attribute value are lost before Dataphor sees them.** `Dilx.cs` converts tabs in an embedded document back into `&#x9;` so a later XML reader cannot normalize them away. That works for a tab the file writes as a character reference; it cannot recover a tab the file stores literally, because XML attribute-value normalization has already replaced it with a space by the time the reader hands the value over. `samples/Sample.DetailAdd/Documents/DetailBrowse.dfdx` stores literal tabs in its `script` attribute, so its loaded script is indented with single spaces. This is XML-spec behaviour and identical on .NET Framework, not a porting defect; both tests carry the explanation, and `Dilx.cs` was deliberately left untouched (the ticket said no product changes). A reviewer who wants the clarification at the source site could add one line to that `NOTE`.
- **No form document in the tree can be read with `XDocument.Load(path)`.** 74 of the 76 declare `encoding="utf-16"` while holding UTF-8 bytes, which an encoding-honouring reader refuses. The product reads them as text through a `StreamReader` and parses from there (`FrontendDevice.LoadAndProcessDocument`), and so do these tests; the comment at that site says so.
- **The 8 loadable `.dfdx` and the 18 excluded ones** are exactly as the implement ticket predicted. The eight that load and round-trip: `Security.RoleBrowse`, `Security.UserBrowse`, `Security.UserDeviceUserBrowse`, `Security.UserRightBrowse`, `Security.UserRoleBrowse`, `Sample.DetailAdd.DetailBrowse`, `Sample.Forms.CustomerAdd`, `Sample.Forms.CustomerEdit`.
- **The registration order matters as recorded.** `Frontend` registers before `System.Internet`; the fixture's comment names `backlog/bug-system-internet-registration-needs-frontend` and says to drop the special ordering when that lands. Every other library's requisites are already ahead of it in the list, so `EnsureLibraryRegistered` never has to pull one in on its own.
- **Part 1 of the three-part suite now takes 5 m 10 s on this machine.** AGENTS.md's figure for it is "about 2.5 minutes" on the slow machine, measured several tickets ago; parts 2 and 3 came in under their recorded figures. The 122 new cases account for about 10 s of part 1. Still inside the 8-minute guidance, but the margin is smaller than the document implies.

## Judgement calls a reviewer should weigh

- **Whether all four synthetic round-trip cases earn their place.** The implement ticket asked for four and said to drop any the eight real cases make redundant. By inspection of the eight real deltas: *element added* and *attribute removed* both appear in them (`Security.RoleBrowse` inserts a `<trigger>` and a `<showformaction>` and resets `OnDefault` with `bop:default-OnDefault`; `Sample.DetailAdd.DetailBrowse` resets three `sourcelink` attributes), and *element removed* appears in none because it cannot. *Two existing siblings reordered* appears in none: the `ibop:order` attributes in the real deltas are insertions at a position, not swaps of two nodes that both exist in the base. All four were kept anyway, on the argument that a real case failing gives a 5 KB document diff while a synthetic one names the operation that broke. A reviewer who weighs that differently should cut the two redundant shapes.
- **The exclusion list asserts "does not load", not "fails for the recorded reason".** Most of the 18 excluded documents belong to libraries the fixture does not even carry, so the error that actually comes back is about the missing library rather than about the document; the recorded reason is for the reader. Asserting the reason would mean carrying an expected error substring per entry, which buys little while the libraries are absent. The comment at the site says this.
- **`ParserRoundTripCorpusTest` still finds the repository root its own way**, by walking up from `AppContext.BaseDirectory` looking for `Dataphor.slnx`. `TestEnvironment.RepositoryDirectory` now does the same job from build metadata. Folding that test onto the accessor is a two-line change in a file this ticket otherwise does not touch, so it was left alone.
- **`FormDocument` and the walk live in `FormDocumentCorpusTest.cs`**, which `FormCustomizationTest` reaches into for them. A separate file for the two would be tidier; keeping them next to the test that defines the corpus was judged clearer. `EnumerateFormDocuments` takes its root directories as an argument, as the implement ticket asked, so `compat-corpus-harness` adds a caller rather than a second walk.

## Interactions to check

- **The golden ticket's fixture.** `test-frontend-derivation-golden` is written against this `FrontendFixture` and expects it unchanged: the same ten libraries, the same registration order, one server, not `[Parallelizable]`. Each derived fixture gets its own server, which is what the implement ticket measured at 18.1 s for both together.
- **`tests/goldens/frontend/`.** The golden ticket writes several hundred derived-form files there with the `.xml` extension. The walk here only looks in the `Documents` folder of a library folder directly under `libraries/`, `samples/` or `tests/libraries/`, and only at `.dfd` and `.dfdx`, so those files are out of this corpus twice over. Both properties have to stay that way.
- **`debt-security-stale-form-documents`** owns `libraries/Security/Documents/GroupRoleBrowse.dfdx` and `GroupDeviceUserBrowse.dfdx`. When it deletes them, their two exclusion entries go in the same commit or `EveryExclusionNamesADocumentInTheTree` fails.
- **`test-samples-corpus`** already carries the `Sample.Rental` write-up; nothing was added to it here.
