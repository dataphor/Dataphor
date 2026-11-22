description: Save a copy of every form the Dataphor server generates from the sample database schemas, so that any change to the form generator shows up as a reviewable difference in the next test run instead of being discovered by a user.
prereq: test-frontend-document-corpus
architecture: docs/modernization-plan.md#ws3-test-suite
files: libraries/Frontend/Source/Derivation/InterfaceBuilder.cs, libraries/Frontend/Source/Production/DocumentBuilder.cs, libraries/Frontend/Source/Elaboration/Elaboration.cs, libraries/Frontend/Source/Structuring/StructureBuilder.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, AGENTS.md
difficulty: medium
----
## What this is

The golden half of layer L5 in the WS3 layer table: derive every page type for every surface of the sample libraries and compare the resulting form XML to a checked-in file. `test-frontend-document-corpus` lands first and brings the fixture, which this ticket reuses unchanged.

Vocabulary: **derivation** is the Frontend server library turning a D4 table expression into a form. A **surface** here is a table variable (a table or a view) a library defines; a **page type** is the kind of form to build from it (`Browse`, `Edit`, …). The entry point is the D4 operator `Frontend.Derive(<expression>, <page type>)`, which returns the form as a single-line XML string.

## What was measured

From a throwaway probe run in `tests/DAE.Server.Tests` on 2026-11-21 (Debug, development machine), since deleted; output in `tickets/.logs/test-frontend-derivation-golden.probe*.log` while that directory's pruner keeps it. These numbers settle the questions the plan ticket left open, so take them as given rather than re-deriving them.

- **The corpus is 45 surfaces** across seven libraries, enumerated from the catalog with `TableVars where Library_Name = '<library>' and not(IsGenerated) over { Name } order by { Name }`: `Frontend` 11, `Security` 18, `Sample.Common` 1, `Sample.DetailAdd` 3, `Sample.Forms` 1, `Sample.IssueTracker` 9, `Sample.SendEmail` 2. The fixture registers ten libraries; the other three (`System.Platform`, `System.Internet`, `SimpleDevice`) define no table variables of their own, so they contribute no cases. Three further sample libraries are not in the fixture at all because they expose nothing after registration: `Sample.Components` and `Sample.ScriptAction` are document-and-script libraries by design, and `Sample.Rental` cannot be registered into a usable state (written up in `test-frontend-document-corpus` and appended to `test-samples-corpus`).
- **Eight page types are derivable**: `Browse`, `List`, `Report`, `OrderBrowse`, `Add`, `Edit`, `View`, `Delete`. `Manage`, `Preview` and `Filter` are rejected outright by `InterfaceBuilder.Structure()`; `test-frontend-document-corpus` pins that.
- **320 of the 360 cases produce a form; 40 fail, and all 40 are `OrderBrowse`.** Each fails with `Elaborated column "<name>" not found`. `OrderBrowse` succeeds on exactly five surfaces: `Security.ChangePassword`, `Security.ChangePasswordBase`, `Sample.IssueTracker.NewIssue`, `Sample.SendEmail.EmailSettings`, `Sample.SendEmail.SentEmail`. It is a real defect, reachable in the shipped product — every derived browse form embeds a `Derive(..., 'OrderBrowse', ...)` expression as the document the order selector opens — and it is filed as `fix/bug-orderbrowse-derivation-fails`. Passing `false` for the elaborate flag, or using the five-argument form of `Derive`, does not avoid it.
- **The output is deterministic.** All 320 forms came out byte-identical when derived on two independently created server instances, and identical when derived twice in one. So **no normalization is needed**: no timestamps, identifiers or machine names appear in the output, and tag and element ordering is insertion-ordered rather than hash-ordered (the `USEHASHTABLEFORTAGS` compile constant, which would make `Tags` iterate a hash table, is defined nowhere in the tree). Do not write a normalizer; if a field ever does drift, add one then and name the field.
- **Size**: the 320 forms are 1,011,446 characters in total — mean 3,160, largest 8,479 (`Sample.IssueTracker.IssueDetail`, `Browse`). About 1 MB over about 320 files, which is why one golden per case is affordable.

## Design

### Golden files

One file per case: `tests/goldens/frontend/<Library>/<unqualified surface>.<PageType>.xml`. For example `tests/goldens/frontend/Security/UserRole.Browse.xml`.

- `tests/goldens/` is new and deliberately at the top of the test tree rather than inside the test project, so that `test-device-sql-golden` (WS3 L3, not yet filed) can put its golden SQL beside it as `tests/goldens/device-sql/`.
- Find the directory through a new `AssemblyMetadata` entry in `DAE.Server.Tests.csproj` (`DataphorGoldensDir`, value `$(MSBuildProjectDirectory)\..\goldens`), read through `TestEnvironment`, the way the layout and the test harness path are found. Never spell the path in code.
- The extension is `.xml`, not `.dfd`, so that these files are not form documents and cannot be mistaken for library content — and so that the repository-wide `.dfd` walk in `test-frontend-document-corpus` does not pick them up.

### The stored form of a golden

`Frontend.Derive` returns `XmlDocument.OuterXml`: one line, no indentation. A 3 KB single-line file makes every change look like a whole-file rewrite, which defeats the point of reviewing goldens in diffs. So the stored form is the *printed* form:

- Parse the derived string and write it back through `XDocument.Save` with `Indent = true`, `IndentChars = "\t"`, `NewLineChars = "\n"`, UTF-8 **without** a byte-order mark, and the default `NewLineHandling.Replace` so that a carriage return inside an attribute value stays an entity.
- Compare the printed text of the fresh derivation with the file's text, character for character. Both sides go through the same printer, so a real change in the form still fails the comparison; only whitespace between elements is normalized away.
- Say this in a comment at the top of the test: the goldens are not the engine's literal output, and the reason is reviewability.

A derived form can carry a newline inside an attribute value (`expression` holds emitted D4; a `scriptaction`'s `script` holds C#). Add one assertion that a golden containing such an attribute re-reads to the same value, so a later change of writer settings cannot silently eat it.

### Case discovery, and no silent gaps

The cases are the cross product of the enumerated surfaces and the eight page types — enumerated from the catalog at run time, not from a checked-in list, so a new table in a sample library turns up as a new case.

The set of golden files and the set of cases must agree exactly, in the spirit of `coverage-manifest.json`:

- A case that derives and has no golden file **fails**, naming the file to create and the update switch below.
- A golden file that no case claims **fails**, naming the file to delete.
- A case that raises an error must match an entry in a checked-in expected-failure list; one that does not **fails** with the error.
- An expected-failure entry whose case now succeeds **fails**, so the entry is drained when the defect is fixed.

The expected-failure list is a small static table in the fixture, in the style of `ParserRoundTripCorpusTest.AllowList`: page-type pattern, surface pattern, ticket slug, expected error substring. As measured it needs exactly one row — any surface, `OrderBrowse`, `bug-orderbrowse-derivation-fails`, `Elaborated column` — and the five surfaces whose `OrderBrowse` does derive simply have goldens, so no per-surface rows are needed.

`fix/bug-orderbrowse-derivation-fails` carries no `prereq:` on this ticket, so it may land first. **Generate the goldens before writing that row**: if all 45 `OrderBrowse` cases derive, the defect is already fixed, the table is empty, and the four set-agreement failures above are the whole mechanism. Say which of the two you found in the handoff.

### Updating the goldens

Setting `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` makes the test **write** each golden instead of comparing it, and delete the ones no case claims. It then still fails the run, with a message saying how many files it wrote, so that an update run can never be mistaken for a passing one. Document the variable in AGENTS.md under "Build and test", next to `DATAPHOR_TEST_LOGGING`, with the one-line rule: the diff is the review, so read it before committing it.

## Edge cases & interactions

- **The `System` library is deliberately not in the corpus.** Deriving its catalog tables is what the IDE does most, so it is tempting, but those surfaces change with every engine change and their goldens would churn on work that has nothing to do with the Frontend. Record that as an accepted tradeoff in a `NOTE:` comment at the corpus list, with the revisit condition: if a derivation defect is ever found on a catalog table and not on a library table, add the `System` library.
- **Cross-server determinism is the property the goldens depend on**, and it holds today by measurement rather than by construction. Keep one case that derives the same surface and page type twice in one process and asserts equality, so that a future change introducing hash-ordering or a timestamp fails here rather than as a confusing golden mismatch on every file at once.
- **Registration order.** The fixture must register `Frontend` before `System.Internet` (`backlog/bug-system-internet-registration-needs-frontend`); `test-frontend-document-corpus` establishes that and the comment explaining it.
- **One server, not parallel.** Two engines in one process share static state, including the process-wide object-id counter (`bug-object-id-counter-is-process-wide`); the fixture starts one server and is not `[Parallelizable]`.
- **Culture.** Numbers and booleans in the output are formatted without an explicit culture in places. The test host pins invariant culture and NLS globalization through its runtime configuration; nothing here should add or rely on a per-test switch.
- **Run cost.** The probe ran 720 derivations across two servers in 18.1 s of one `dotnet test` run; 360 on one server is a fraction of that. Report the measured figure in the handoff. The cases land in suite part 1 (everything but the coverage scripts) — if that part grows past about 8 minutes, AGENTS.md says to split it further.
- **`library-frontend-web-leftovers`** (backlog, WS7) deletes nine uncompiled `System.Web` files from `libraries/Frontend/Source`. None of them is in `Frontend.Server.csproj`'s compile list (verified), so this test must still build and pass after that deletion; nothing to do beyond not depending on them.
- **`test-samples-corpus`** (plan, L7) covers breadth — every surface of every sample derives at all — against this ticket's depth. It must not duplicate the golden comparison; its own ticket already defers to this one.
- **`compat-corpus-harness`** (backlog, WS13) points an external corpus at the same harness later. Keep the surface enumeration a method taking a library name, so that ticket adds callers.

## TODO

- Add the `DataphorGoldensDir` assembly metadata entry and its `TestEnvironment` accessor.
- Write the fixture: enumerate surfaces per library, cross with the eight page types, derive, print, compare.
- Implement the four set-agreement failures and the expected-failure table with its single `OrderBrowse` row.
- Implement `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1`, including deleting unclaimed goldens and failing the run afterwards.
- Generate the goldens with it and read the diff before committing: spot-check a handful against the derivation code rather than trusting the generator, and say in the handoff which ones you read.
- Add the new source files to `DAE.Server.Tests.csproj` (its compile list is explicit).
- Document the environment variable in AGENTS.md under "Build and test".
- Build, then run the suite in the three parts AGENTS.md names, each as its own foreground command.
- No entry in `docs/migration-notes.md`: nothing an application, a deployment script or a C# extension can observe changes.
