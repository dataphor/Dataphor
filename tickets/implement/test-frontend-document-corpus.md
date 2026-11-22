description: Add tests that read every form file in the repository so a change to the form file format is caught straight away, and that prove applying a form customization and then subtracting it again gets the original back.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/InProcessTestFixture.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, src/engine/Base/BOP/Dilx.cs, src/engine/Base/BOP/Inheritance.cs, libraries/Frontend/Source/Device/FrontendDevice.cs, libraries/Security/Documents/, samples/
difficulty: medium
----
## What this is

Two of the three concerns of layer L5 in the WS3 layer table: read every `.dfd` and `.dfdx` in the repository, and show that the customization machinery round-trips. The third concern, golden form text for derived pages, is `test-frontend-derivation-golden`, which depends on this ticket for the fixture it also uses. Everything here is new test code in `tests/DAE.Server.Tests` plus one sentence in the plan; no product code changes.

Vocabulary, because the file extensions are opaque: a **`.dfd`** is a *form document* — a BOP XML document describing a form's node tree. A **`.dfdx`** is a *customized form document*: a small XML wrapper (`dilx`, read by `DilxDocument` in `src/engine/Base/BOP/Dilx.cs`) that names one or more **ancestors** — D4 expressions, in practice `Derive('<table>', '<page type>')` — and carries a **delta**, the changes to apply on top of them. Loading one means evaluating each ancestor expression, merging the results, then merging the delta over that. The merge and its inverse live in `src/engine/Base/BOP/Inheritance.cs` as `Merge(original, diff)` and `Diff(original, modified)`, reached from D4 as the `Frontend.Merge` and `Frontend.Difference` operators.

Note that `Frontend.Difference(AForm, ADelta)` calls `Inheritance.Diff(original: AForm, modified: ADelta)`: despite the parameter name, its second argument is the *modified document*, not a delta, and its result is the delta. The round trip below relies on that reading, which was confirmed by running it.

## What was measured

All counts and results below come from a throwaway probe run in `tests/DAE.Server.Tests` on 2026-11-21 (Debug, development machine) and were deleted again; the probe output is in `tickets/.logs/test-frontend-derivation-golden.probe*.log` while that directory's pruner keeps it.

- **76 tracked form files**: 50 `.dfd` and 26 `.dfdx` (`git ls-files '*.dfd' '*.dfdx'`). None under `legacy/`. The earlier count of 140 in the plan ticket was wrong: it counted the copies the build writes into `artifacts/layout/debug` and `artifacts/layout/release`, which are not tracked.
- **The fixture already exists.** `InProcessTestFixture` takes a list of library folders to copy and a list of libraries to register; a sister fixture is a subclass with those two lists. Registering the ten libraries this ticket uses on each of two independent servers, running 720 derivations and loading the whole `.dfdx` corpus took **18.1 s** of one `dotnet test` run. No new fixture type is needed and no cost concern remains.
- **8 of the 26 `.dfdx` files load and round-trip exactly.** For each, `Frontend.Merge(base, Frontend.Difference(base, loaded))` is XML-equal to `loaded`, where `base` is the merged ancestors and `loaded` is what `Frontend.LoadAndProcess` returns: `Security.RoleBrowse`, `Security.UserBrowse`, `Security.UserDeviceUserBrowse`, `Security.UserRightBrowse`, `Security.UserRoleBrowse`, `Sample.DetailAdd.DetailBrowse`, `Sample.Forms.CustomerAdd`, `Sample.Forms.CustomerEdit`.
- **The other 18 cannot be loaded on a server with no SQL Server**, for five separate reasons, all listed in the exclusion table under *Test 2* below.
- Two of those 18 are **dead documents**: `libraries/Security/Documents/GroupRoleBrowse.dfdx` and `GroupDeviceUserBrowse.dfdx` name `Security.GroupRole` and `Security.GroupDeviceUser`, table variables the Security library no longer creates (its `Group` table is commented out in `libraries/Security/Documents/Security.d4`). `debt-security-stale-form-documents` in `backlog/` owns deciding what to do with them; this ticket excludes them by name with that slug.

## Design

### The fixture

Add `FrontendFixture` (name it as you like) in `tests/DAE.Server.Tests`, deriving from `InProcessTestFixture`, with:

- `Libraries` — the library folders to copy: `System.Platform`, `System.Internet`, `SimpleDevice`, `Frontend`, `Security`, `Sample.Common`, `Sample.DetailAdd`, `Sample.Forms`, `Sample.IssueTracker`, `Sample.SendEmail`.
- `LibrariesToRegister` — the same list **with `Frontend` before `System.Internet`**. Order matters: `System.Internet`'s registration script calls the `Frontend.ExecuteScript` operator, so registering it on a server that has no `Frontend` fails. That is a defect in `System.Internet`, filed as `backlog/bug-system-internet-registration-needs-frontend`; until it is fixed the fixture must register `Frontend` first, with a comment naming that slug.

`test-frontend-derivation-golden` uses this same fixture, so put it in its own file and keep it free of assertions.

### Test 1 — every form file in the repository is readable

One data-driven case per tracked `.dfd` and `.dfdx` file, named after its repository-relative path, in the style of `ParserRoundTripCorpusTest`. No server: this is a file-reading test, so it can be its own fixture.

Discovery walks the `Documents` folder of every library directory under `libraries/`, `samples/` and `tests/libraries/` in the *repository*, not the runtime layout — the layout omits the parked libraries, and the point is that nothing in the tree is missed. Find the repository root the way the project finds the layout: a new `AssemblyMetadata` entry in `DAE.Server.Tests.csproj` (`DataphorRepositoryDir`, value `$(MSBuildProjectDirectory)\..\..`), read through `TestEnvironment`. Never spell the path in code.

Assertions, per file:

- It parses as XML (`XDocument.Load`).
- A `.dfd`'s root element is `interface`, and it declares the BOP namespace `www.alphora.com/schemas/bop`.
- A `.dfdx` reads through `BOP.DilxDocument.Read` without error, has at least one ancestor, and its `Content` parses as XML with an `interface` root.

The test must also fail when the discovery finds no files at all, so that a future folder move cannot turn it into a silent no-op.

### Test 2 — every `.dfdx` of a registrable library loads through the server

On the fixture above, one case per `.dfdx` file of the ten registered libraries. `Frontend.LoadAndProcess('<library>', '<name>')` must return a document whose root element is `interface`. That one call resolves each ancestor's `Derive(...)` expression against the live catalog and merges the delta over the result, so it covers the document device's read path, `DilxDocument`, the ancestor resolution in `FrontendDevice.MergeAncestors` and `Inheritance.Merge` together.

Files excluded from this test carry a one-line reason and, where one exists, a ticket slug — a checked-in list in the fixture, in the style of `ParserRoundTripCorpusTest.AllowList`. An excluded file that now loads must fail the run so the exclusion is drained. The exclusion list is exactly:

| Files | Reason |
|---|---|
| `Security.GroupRoleBrowse`, `Security.GroupDeviceUserBrowse` | Name table variables the library no longer creates; `debt-security-stale-form-documents` |
| `Sample.Rental` (4 files) | The tutorial's tables are created by running its scripts by hand, not by registering the library; see *Sample.Rental* below |
| `Sample.Shipping` (6 files) | The library is parked: its C# assembly is not built, so it is not in the runtime layout |
| `Sample.Forum.Start` (1 file) | The library requires a SQL Server device |
| `TestFramework.Coverage` (5 files) | The library requires `TestFramework.Coverage.Devices`, which requires `MSSQLDevice` and so a SQL Server |

### Test 3 — the customization round trip

For each `.dfdx` that Test 2 loads, assert the round trip the probe verified:

1. `loaded` = `Frontend.LoadAndProcess(library, name)`.
2. `base` = the ancestors folded with `Frontend.Merge`: evaluate each ancestor expression of the file's `DilxDocument` in turn and merge it onto the accumulated document.
3. `rediff` = `Frontend.Difference(base, loaded)`.
4. `Frontend.Merge(base, rediff)` must be XML-equal to `loaded` (`XNode.DeepEquals` on parsed documents, not string equality: the writers differ in whitespace).

This is the property worth having — it says the diff the Dataphoria designer would write from a customized form reproduces that form when merged back — and the repository supplies eight real cases for it, so no synthetic corpus is needed for the common shapes.

Add **four** synthetic cases on top, for shapes the eight real ones may not contain, each a small hand-written base document and a modified copy, asserting the same round trip: an element added, an element removed, two sibling elements reordered, and an attribute removed. Keep them as string constants in the test file, not as files on disk — they are four small documents, and a reader should see them next to the assertion. Confirm by inspection which of the four the eight real cases already cover and drop any that are genuinely redundant, saying so in the handoff.

### Test 4 — the page types the derivation engine does not accept

One case asserting that `Frontend.Derive('<surface>', '<page type>')` raises an error naming the page type for `Manage`, `Preview` and `Filter`. This is not padding: `Frontend`'s own `PageTypes` table (`libraries/Frontend/Documents/Register.d4`) lists `Manage`, `Preview` and `Filter` as form types and omits `Report`, while `InterfaceBuilder.Structure()` accepts `Browse`, `List`, `Report`, `OrderBrowse`, `Add`, `Edit`, `View` and `Delete` and throws `UnknownPageType` for anything else (measured: "Unknown form type \"Manage\". Form types are case sensitive."). Pinning the `default:` arm records which list is the real one. Do **not** change either list: the `PageTypes` table is catalog content an application can read.

## Sample.Rental

Excluded, and worth recording because it looks as though it ought to work. `samples/Sample.Rental/Documents/Register.d4` is an empty file: the tutorial's device, tables, views and operators are created by running the scripts in its `Documents` folder by hand, in the order `RegisterCompleted.d4` lists. Running that script as one batch fails at `MovieActorTable` with "The table Movie does not have a row with ID (1)." and rolls everything back; running its 17 `ExecuteScript` statements as separate batches gets 11 through and still fails 6, starting at the same place. And the objects the successful steps create do not land in the `Sample.Rental` library — after those 11 steps `TableVars where Library_Name = 'Sample.Rental'` is still empty — so they cannot be enumerated per library either.

That is a finding for `test-samples-corpus`, which owns "every sample library registers and derives"; it has been appended to that ticket. Do not chase it here.

## Edge cases & interactions

- **The golden files of `test-frontend-derivation-golden` must not be picked up by Test 1.** That ticket writes several hundred derived-form files under `tests/goldens/frontend/`. Test 1 discovers only `Documents` folders of library directories, which that path is not, and the goldens use the `.xml` extension rather than `.dfd`. Keep both of those properties; a later move of the goldens into a `Documents` folder would silently enlarge this corpus.
- **One server, not parallel.** Two engines in one process share engine static state, including the process-wide object-id counter (`bug-object-id-counter-is-process-wide`); see the note on `TestServerInstance`. The fixture starts one server and must not be `[Parallelizable]`.
- **Culture.** The emitted XML carries numbers and booleans formatted without an explicit culture in places. The test host already pins invariant culture and NLS globalization through the runtime configuration; do not add a per-test culture switch, and do not rely on one either.
- **Significant whitespace in attribute values.** `DilxDocument` converts tab characters in the embedded document to `&#x9;` entities on read, deliberately, so that a later XML reader does not normalize them away (the comment in `Dilx.cs` says so). Any comparison must therefore be on parsed documents, never on text. One of the eight real cases, `Sample.DetailAdd.DetailBrowse`, has a `scriptaction` whose `script` attribute holds embedded newlines and quotes as entities (`&#xD;&#xA;`, `&quot;`), so the corpus already covers this; assert on that case's attribute value explicitly rather than only on document equality, so a later change of reader or writer settings cannot eat it silently.
- **`library-frontend-web-leftovers`** (backlog, WS7) deletes nine uncompiled `System.Web` files from `libraries/Frontend/Source`. Nothing here touches them: `Frontend.Server.csproj` compiles 16 files and none of the nine is among them (verified). The customization code this ticket exercises is in `src/engine/Base/BOP`, not in the uncompiled `Customization/` folder.
- **`compat-corpus-harness`** (backlog, WS13) will point a corpus of external application libraries at this same walk. Keep the discovery a method that takes a list of root directories and returns the files it found, so that ticket adds a caller rather than a second walk. Do not build a reporting format for it.
- **An empty or renamed library folder** must not quietly shrink the corpus — hence the "fails when discovery finds nothing" assertion above. A per-library expected minimum would be churn; a single non-empty check is enough.

## TODO

- Add the `DataphorRepositoryDir` assembly metadata entry and the `TestEnvironment` accessor for it.
- Add the sister fixture, with the library lists above and the comment about `Frontend` registering before `System.Internet`.
- Write Test 1 (file corpus), Test 2 (loads through the server, with the exclusion list), Test 3 (round trip, real cases plus the synthetic shapes) and Test 4 (rejected page types).
- Add the new source files to `DAE.Server.Tests.csproj` (its compile list is explicit).
- Build, then run the suite in the three parts AGENTS.md names, each as its own foreground command.
- Record in `docs/modernization-plan.md`, in the WS3 L5 row or just under the layer table, that server-side L5 covers reading and customizing form documents while instantiating a form's node tree from one belongs to `ui-frontend-client-port` in WS6, which already carries "deserialize every repository form in a test" — the node classes are in `src/client/Frontend.Client`, which has no server-side counterpart, so there is nothing for this layer to deserialize beyond the XML.
- No entry in `docs/migration-notes.md`: nothing an application, a deployment script or a C# extension can observe changes.
