description: Tests that read every form file in the repository and prove that applying a form customization and then subtracting it again gets the original form back.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/FrontendFixture.cs, tests/DAE.Server.Tests/FormDocumentCorpusTest.cs, tests/DAE.Server.Tests/FormCustomizationTest.cs, tests/DAE.Server.Tests/TestEnvironment.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, AGENTS.md, docs/architecture.md, docs/modernization-plan.md
----
## What landed

The document half of layer L5 of the WS3 test suite: 106 test cases in `tests/DAE.Server.Tests`, plus documentation. No product code changed, so nothing an application, a deployment script or a C# extension can observe changed, and `docs/migration-notes.md` has no new entry.

Vocabulary, because the extensions are opaque: a **`.dfd`** is a *form document* — XML describing a form's node tree. A **`.dfdx`** is a *customized form document*: a wrapper naming one or more **ancestors** (D4 expressions, in practice `Derive('<table>', '<page type>')`) plus a **delta** of changes to apply on top of them. Loading one means evaluating the ancestor expressions, merging the results, then merging the delta over that. The merge and its inverse are `Merge` and `Diff` in `src/engine/Base/BOP/Inheritance.cs`, reachable from D4 as `Frontend.Merge` and `Frontend.Difference`.

### Files

- **`tests/DAE.Server.Tests/FrontendFixture.cs`** — an abstract fixture over `InProcessTestFixture`: one in-process server carrying ten libraries (`System.Platform`, `Frontend`, `System.Internet`, `SimpleDevice`, `Security`, `Sample.Common`, `Sample.DetailAdd`, `Sample.Forms`, `Sample.IssueTracker`, `Sample.SendEmail`), plus four helpers that call `Frontend.LoadAndProcess`, `Frontend.Merge` and `Frontend.Difference` with the documents passed as D4 parameters rather than as literals. No assertions: `test-frontend-derivation-golden` runs on this same fixture and uses its public `FrontendLibraries` list to enumerate surfaces.
- **`tests/DAE.Server.Tests/FormDocumentCorpusTest.cs`** — the no-server half, plus the `FormDocument` type and the directory walk both halves share.
- **`tests/DAE.Server.Tests/FormCustomizationTest.cs`** — the server half, on `FrontendFixture`.
- **`tests/DAE.Server.Tests/TestEnvironment.cs`** — one new accessor, `RepositoryDirectory`, reading a new `DataphorRepositoryDir` assembly-metadata entry that `DAE.Server.Tests.csproj` records; it checks `Dataphor.slnx` is there and says what to rebuild if not. No path is spelled in code. `ParserRoundTripCorpusTest`, which had its own walk-up search for the repository root, now uses it too, so the assembly has one way to find the tree and AGENTS.md can name it.

### What the cases verify

| Test | Cases | Verifies |
|---|---|---|
| `FormDocumentCorpusTest.Reads` | 76 (one per tracked `.dfd` and `.dfdx`) | A `.dfd` parses as XML, its root is `<interface>`, and it declares the BOP namespace. A `.dfdx` reads through `BOP.DilxDocument`, names at least one ancestor, and its embedded document parses as XML with an `<interface>` root. |
| `FormDocumentCorpusTest.TheWalkFindsBothKindsOfFormDocument` | 1 | The discovery walk found at least one of each extension, so a renamed folder cannot turn the corpus silently into zero cases. |
| `FormDocumentCorpusTest.CustomizationReadKeepsCharacterReferencedWhitespace` | 1 | A tab written as `&#x9;` inside an attribute of an embedded document survives being read; a tab the file stores literally arrives as a space. This is the mechanism the `NOTE` in `Dilx.cs` is about. |
| `FormCustomizationTest.LoadsThroughTheServer` | 10 (the `.dfdx` of a carried library) | For the 8 that load: `Frontend.LoadAndProcess` returns a document whose root is `<interface>`, which covers the document device's read path, `DilxDocument`, `FrontendDevice.MergeAncestors` and `Inheritance.Merge` in one call. For the 2 recorded failures: the load still fails, **and fails carrying the recorded error text**, so the entry drains when its cause is fixed and a server that has simply stopped working cannot satisfy it. |
| `FormCustomizationTest.CustomizationRoundTrips` | 8 | `Frontend.Merge(ancestors, Frontend.Difference(ancestors, loaded))` is XML-equal to `loaded`, where `ancestors` is the customization's ancestor expressions evaluated and folded. This is the property: the delta a designer would write from a customized form reproduces that form when merged back. |
| `FormCustomizationTest.SyntheticRoundTrip_ElementAdded` / `_SiblingsReordered` / `_AttributeRemoved` | 3 | The same round trip over hand-written pairs, one shape of change each. |
| `FormCustomizationTest.SyntheticRoundTrip_ElementRemoved_IsNotExpressible` | 1 | A removal produces an **empty** difference and merging it back leaves the original unchanged — see *Measured* below. |
| `FormCustomizationTest.MultiLineScriptInAnAttributeSurvivesLoading` | 1 | The one document in the tree with a multi-line script in an attribute value keeps its line breaks and its quotes through a load, and its one literal tab arrives as a single space. Asserted on the attribute value, not only through document equality. |
| `FormCustomizationTest.DeriveRejectsThePageType` | 3 | `Frontend.Derive` refuses `Manage`, `Preview` and `Filter` with an error naming the page type. |
| `FormCustomizationTest.DeriveAcceptsTheBrowsePageType` | 1 | The other side of that: `Browse` on the same surface does derive, so the refusal is about the page type and not the surface. |
| `FormCustomizationTest.EveryCustomizationIsAccountedFor` | 1 | Every `.dfdx` in the tree is a case here, a recorded failure, or the document of a library the fixture cannot carry; and no entry in either list has gone stale. |

106 cases in total (78 + 28).

## How to run and validate

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~FormDocumentCorpusTest|FullyQualifiedName~FormCustomizationTest"
```

Expect `Failed: 0, Passed: 106`, in about 8 s. `FormDocumentCorpusTest` alone runs in about a second and needs no server.

## Review findings

The implement-stage diff (`ticket(implement): test-frontend-document-corpus`) was read first, before the handoff summary. Build clean (`dotnet build Dataphor.slnx`, 0 errors; the 143 warnings are all pre-existing and in unrelated projects). The whole suite was run in the three parts AGENTS.md names, each in the foreground:

| Part | Result |
|---|---|
| everything but the coverage scripts | 559 passed, 0 failed (`DAE.Server.Tests`), 13 passed (`Common.Tests`) — 5 m 35 s |
| coverage scripts A–R | 67 passed, 11 skipped, 0 failed — 3 m 12 s |
| coverage scripts not A–R | 49 passed, 13 skipped, 0 failed — 3 m 48 s |

Part 1 was re-run after the last of the fixes below, since both files they touch are in it; parts 2 and 3 were run on the fully fixed tree except for those two edits, neither of which a coverage script can reach (a comment in `FrontendFixture` and a helper fold in `ParserRoundTripCorpusTest`). No pre-existing failure surfaced, so `tickets/.pre-existing-error.md` was not written.

### What was checked beyond running it

Every factual claim the tests or the handoff rest on was re-derived rather than taken on trust:

- **The walk is complete.** There is no `.dfd` or `.dfdx` anywhere in the tree outside the `Documents` folder of a library folder directly under `libraries/`, `samples/` or `tests/libraries/`, and none in a subfolder of one — so the 76 the walk finds are all of them, and the flat `Documents` layout is also why `QualifiedName` (library + file name) cannot collide today.
- **The encoding claim.** 74 of the 76 declare `encoding="utf-16"` while holding UTF-8 bytes; the two that do not are `libraries/Frontend/Documents/DocumentExpressionEditor.dfd` and `samples/Sample.ScriptAction/Documents/BCLAccess.dfd`. Reading as text and parsing from there, as the product's `FrontendDevice.LoadAndProcessDocument` does, is correct and the comment saying so is accurate.
- **`MergedAncestors` matches the product.** `FrontendDevice.MergeAncestors` takes the first ancestor as the base and merges each later one over it; the test's fold is the same, in the same direction.
- **The page-type refusal is not a porting defect.** `InterfaceBuilder.Structure()` has no case for `Manage`, `Preview` or `Filter` and ends in `default: throw UnknownPageType`; that code is unchanged from 3.1, so the cases pin long-standing behaviour rather than freezing a .NET 10 regression — which the project's "a failing assertion is a porting defect until shown otherwise" rule would otherwise require.
- **The hard-coded `\r\n` in `MultiLineScriptInAnAttributeSurvivesLoading` is safe.** `samples/Sample.DetailAdd/Documents/DetailBrowse.dfdx` writes the script's line breaks as `&#xD;&#xA;` character references, so the CRLF in the loaded value comes from the attribute content and not from the file's own line endings. (Checked the weaker property too: `.gitattributes` declares no `text` attribute for `.dfdx` and the blob stores CRLF, so the file survives a checkout under any `core.autocrlf` setting unchanged.)
- **Site-claim grep** over `backlog/`, `fix/`, `plan/`, `implement/` and `review/` for all the files and symbols involved: nothing open claims any of them.
- **Both new guards were negative-tested** by temporarily breaking them (a wrong expected error text, and a deleted scope entry) and confirming each fails with the message it promises, then restoring the file.

### Found and fixed in this pass

- **`LoadsThroughTheServer` claimed to check 26 documents and checked 10.** The exclusion list named 18 individual documents, but 16 of them belong to libraries the fixture does not carry at all (`Sample.Forum`, `Sample.Rental`, `Sample.Shipping`, `TestFramework.Coverage`): their load failed with `Object "<library>" not found`, so those 16 cases asserted only that an unregistered library is unregistered, while being named after a document and reported as passing. Their recorded reasons were also partly fiction — "Sample.Forum requires a SQL Server device" was not why that case failed. Fixed by splitting the one list in two: `LibrariesNotCarried` holds four library-level entries with a reason each and puts their documents out of scope, and `Exclusions` keeps the two documents that genuinely fail on this server. The scope now follows `FrontendFixture.FrontendLibraries`, so adding a library there turns its customizations into cases automatically.
- **The two real exclusions now assert *why* they fail,** not just that they do: each entry carries the text its failure must contain (`Unknown identifier "Security.GroupRole"`, and the same for `Security.GroupDeviceUser`). "Still fails" on its own would have been satisfied by the server having stopped working altogether. This became worth doing only once the 16 environment cases were gone — with them in, no shared expected text existed.
- **`EveryExclusionNamesADocumentInTheTree` was one-sided,** checking only that an entry still named a file. Replaced by `EveryCustomizationIsAccountedFor`, which states the whole agreement between the two lists and the tree: a customization of a library neither carried nor explained fails (so a newly added library cannot slip in unexamined), a `LibrariesNotCarried` entry for a library that is now carried fails (so the list drains), an entry naming no such library fails, and an exclusion that is not a carried customization fails.
- **`ParserRoundTripCorpusTest` had its own walk-up search for the repository root.** Folded onto the new `TestEnvironment.RepositoryDirectory` (13 lines deleted), so the assembly has one way to find the tree. The handoff had flagged this and left it; with AGENTS.md now naming the accessor it was worth closing. Re-ran part 1: the same 387 parser cases, still green.
- **Documentation.** AGENTS.md documents the D4 parser corpus and the coverage scripts in detail and had nothing for this pass, nor for `DataphorRepositoryDir` — which matters, because the same section tells agents never to spell the layout or test-harness paths in code and this is a third such mechanism; added a paragraph covering both. `docs/architecture.md` described the `.dfd`/`.dfdx` formats without the constraint this ticket measured; its form-document paragraph now records that the merge has no operation that removes a node, and names the case that pins it. AGENTS.md's measured part timings ("about 2.5, 3.5 and 6 minutes") were stale: measured today, 5.5, 3.5 and 4. Part 1 has roughly doubled since that figure was written — still inside the 8-minute guidance the same paragraph gives, but it is the part to watch, and the paragraph now says so. The plan's table row was updated to the corrected case counts.
- **An inaccuracy in a remark.** The `RejectedPageTypes` remark said the `Frontend.PageTypes` table and the derivation engine "disagree" as though in one direction; the disagreement runs both ways — `Structure()` also builds `Report`, which `PageTypes` does not offer. Said so, with why only the other direction is worth pinning.

### Recorded as tripwires, not tickets

- **`DeriveAcceptsTheBrowsePageType` becomes redundant when `test-frontend-derivation-golden` lands** — its golden set derives `Browse` for all 45 surfaces, which is the same control. `NOTE:` at the case saying to drop it then. (`DeriveRejectsThePageType` stays: the golden ticket explicitly defers the three refusals to this one.)
- **This test assembly now walks an exception chain in seven places** — `CoverageScripts.ErrorText`, four verbatim copies of `FindError`, `CatalogStoreConcurrencyTest.Innermost`, and `FrontendFixture.FlattenMessages`. Six of the seven predate this ticket and each is a few lines doing something slightly different, so folding them is not worth a ticket today; `NOTE:` on the newest copy saying an eighth should become one shared helper instead.

### Weighed and left alone

- **All four synthetic round-trip shapes were kept.** The implement ticket said to drop any the eight real cases make redundant, and two are: an element added and an attribute removed both occur in the real deltas. Kept anyway, for the reason the handoff gives and which holds up — a real case failing produces a 5 KB document diff, a synthetic one names the operation that broke — and because the other two earn their place outright: two existing siblings swapped appears in no real delta (the `ibop:order` attributes there are insertions at a position), and the removal case is the only record of a design constraint.
- **`CustomizationRoundTrips` mirrors `FrontendDevice.MergeAncestors` in the test** rather than asking the product for the merged ancestors. That makes the round trip self-consistent, so it pins `Merge`/`Diff` being inverses and not the product's ancestor fold. Adding an assertion that the product's fold equals the mirror's would be a test restating the implementation; the fold is three lines and verified by reading.
- **`FormDocument` and the walk live in `FormDocumentCorpusTest.cs`**, which `FormCustomizationTest` reaches into. A third file would be tidier; keeping them beside the test that defines the corpus reads better, and `EnumerateFormDocuments` already takes its roots as an argument so `compat-corpus-harness` adds a caller rather than a second walk.
- **The `IDataValue` that `ServerProcess.Evaluate` returns is not disposed** in `FrontendFixture.EvaluateString`. That is the established pattern throughout this test assembly (`CatalogStoreTest`, `CatalogStoreConcurrencyTest`, `AbandonedClientProcessTest`), and the values here are scalars holding strings, with no unmanaged resource behind them. Changing one call site would make the assembly less consistent, not more correct.
- **Nothing was changed in `Dilx.cs`.** The handoff noted a reviewer could extend its `NOTE` to say that the tab conversion cannot recover a tab a file stores literally. That fact is now stated in two test docstrings and the ticket was explicit about no product changes; adding a comment to a product file for it would be the only reason to touch `src/engine/Base/BOP`.

### Nothing found in these categories

- **Correctness of the assertions.** Every case was traced to the product code it exercises and each assertion is reachable and live (the two guards were broken on purpose to prove it). No test passes vacuously.
- **Resource cleanup and lifetime.** The fixture relies on `InProcessTestFixture` for the server, session and process, which it already tears down; the new code opens no file handle it does not close (`File.ReadAllText`) and no other resource.
- **Compatibility surfaces.** Nothing in the diff touches D4 semantics, operator signatures, `DAERegister` class names, assembly names, a file format, an error code, a WCF contract, a port or the instance layout.
- **Source hygiene.** The new files are 108, 231 and 386 lines; the comments say why rather than what; each edited file kept its own line endings, indentation and byte-order-mark state (`ParserRoundTripCorpusTest.cs` is LF with no mark, the rest CRLF with one).

## Interactions the next ticket should know

- **`test-frontend-derivation-golden` runs on this `FrontendFixture` and expects it unchanged**: the same ten libraries, the same registration order, one server, not `[Parallelizable]`. This review changed nothing in that file but a comment. Each derived fixture gets its own server, which the implement ticket measured at 18.1 s for both together.
- **`tests/goldens/frontend/`.** The golden ticket writes several hundred derived forms there with the `.xml` extension. The walk here only looks in the `Documents` folder of a library folder directly under `libraries/`, `samples/` or `tests/libraries/`, and only at `.dfd` and `.dfdx`, so those files are out of this corpus twice over. Both properties have to stay that way.
- **`debt-security-stale-form-documents`** owns `libraries/Security/Documents/GroupRoleBrowse.dfdx` and `GroupDeviceUserBrowse.dfdx`. When it deletes them, their two `Exclusions` entries go in the same commit or `EveryCustomizationIsAccountedFor` fails — and if it fixes them instead, `LoadsThroughTheServer` fails and says to remove the entries.
- **Converting `Sample.Shipping`, or giving `Sample.Rental`, `Sample.Forum` or `TestFramework.Coverage` a usable registration, is now visible here**: add the library to `FrontendFixture.FrontendLibraries` and its `LibrariesNotCarried` entry fails, which is the prompt to delete it and let its 1–6 customizations become cases.
- **`test-samples-corpus`** already carries the `Sample.Rental` write-up; nothing was added to it here.

## What was measured

- **BOP inheritance has no operation that removes an element.** `Inheritance.Diff` walks the children of the *modified* document, so a child only the original has contributes nothing to the delta, and the merge leaves it where it was. Measured: the difference between the synthetic base and a copy with one `<textbox>` removed is an empty `<interface/>`, and merging it back reproduces the base. This is the machinery as designed — a node of a derived form is overridden, never deleted — so it is a case (`SyntheticRoundTrip_ElementRemoved_IsNotExpressible`) and now a sentence in `docs/architecture.md`, rather than a defect ticket. A later attempt to add a deletion operation has to come past both.
- **Literal tabs in an attribute value are lost before Dataphor sees them.** `Dilx.cs` converts tabs in an embedded document back into `&#x9;` so a later XML reader cannot normalize them away. That works for a tab the file writes as a character reference; it cannot recover a tab the file stores literally, because XML attribute-value normalization has already replaced it with a space by the time the reader hands the value over. `samples/Sample.DetailAdd/Documents/DetailBrowse.dfdx` stores literal tabs in its `script` attribute, so its loaded script is indented with single spaces. This is XML-spec behaviour and identical on .NET Framework, not a porting defect.
- **No form document in the tree can be read with `XDocument.Load(path)`** — 74 of the 76 declare an encoding they do not use. The product reads them as text through a `StreamReader` and parses from there, and so do these tests.
- **The eight `.dfdx` that load and round-trip**: `Security.RoleBrowse`, `Security.UserBrowse`, `Security.UserDeviceUserBrowse`, `Security.UserRightBrowse`, `Security.UserRoleBrowse`, `Sample.DetailAdd.DetailBrowse`, `Sample.Forms.CustomerAdd`, `Sample.Forms.CustomerEdit`.
- **The registration order matters as recorded.** `Frontend` registers before `System.Internet`; the fixture's comment names `backlog/bug-system-internet-registration-needs-frontend` and says to drop the special ordering when that lands. Every other library's requisites are already ahead of it in the list, so `EnsureLibraryRegistered` never has to pull one in on its own.
