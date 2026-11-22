description: A new test gives each of the twelve example applications in the repository a server of its own, installs it there, and builds every screen it can, so that a change which breaks one of the examples fails the test run instead of being found when somebody opens it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/SamplesCorpusTest.cs, tests/DAE.Server.Tests/CorpusLibrary.cs, tests/DAE.Server.Tests/TestEnvironment.cs, tests/DAE.Server.Tests/TestServerInstance.cs, tests/DAE.Server.Tests/FrontendFixture.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, AGENTS.md, docs/modernization-plan.md
----
## What landed

Layer **L7** of the WS3 test-suite layer table, for the example applications this repository ships. Two new files, three small changes to existing test helpers, two documentation edits. Everything the plan ticket settled was implemented as settled; no decision was reopened and nothing was left out.

**`tests/DAE.Server.Tests/CorpusLibrary.cs`** (147 lines) — the reusable half, which knows nothing about samples, this repository or NUnit. A `CorpusLibrary` is one library folder: its `Name` (the folder's name, which is the library name a server uses), its `Directory` (the folder to copy) and its `Requisites` (the library names its `.d4l` lists, in order). Three statics:

- `DiscoverLibraries(rootDirectory)` — every subfolder holding a `.d4l` named after it, ordered by name. A folder with no such file is not a library and is skipped; a root that does not exist yields nothing.
- `FindLibrary(rootDirectory, name)` — that one folder, or null. Null is how "the build left this library out of the runtime layout" is told from "the build assembled it".
- `RequisiteClosure(rootDirectory, libraryName)` — the transitive closure over the `.d4l` files under that root, depth first and in the order each file lists them, every library after the ones it requires and the named library last. So the result can be copied into a server's library directory as it stands, and registering its last entry pulls in the rest. `System` and `General` are skipped (one is built into the engine, the other the server creates); any other library with no folder throws naming the library, the requirer and the root.

The `.d4l` files are read with the engine's own deserializer (`Schema.LibraryUtility.LoadFromStream`), not as hand-parsed XML, so a corpus pass and the server cannot disagree about what a descriptor says. `LoadFromStream` rather than `LoadFromFile` because the latter also reads a per-instance `.d4l.info` out of a server's instance directory, and a corpus walk has no instance.

**`tests/DAE.Server.Tests/SamplesCorpusTest.cs`** (408 lines) — the pass. One NUnit case per library folder discovered under `samples/`, generated from a `TestCaseSource` at discovery time (discovery reads directories and needs no server, which is why this pass can do what the golden pass could not). A case starts a server of its own carrying that sample's requisite closure and nothing else, registers the sample, enumerates its surfaces from the catalog and derives all eight page types of each.

**Three changes to existing helpers**, each the smallest that would do:

- `TestEnvironment.CopyLibraryFolders(targetDirectory, sourceDirectories)` is new and does the copying; `CopyLibraries(targetDirectory, names)` is now a wrapper that maps each name to `Libraries/<name>` and calls it. The name-aware "The runtime layout has no library …" message and its `nameof(names)` parameter name are kept in the wrapper deliberately, so no existing fixture's failure reads differently. All six existing callers are unchanged.
- `TestServerInstance` gained a constructor taking `IEnumerable<string>` of source folders beside the one taking layout library names; both now delegate to a private constructor holding the shared body (new directory, copy, configure, remove the directory again if any of that throws), so the failure path exists once.
- `FrontendFixture.FlattenMessages` went from `protected` to `public`. Its own `NOTE:` says this assembly already walks an exception chain in seven places and that an eighth caller should get a shared helper rather than another copy — so this pass calls that one instead of adding a copy, and the `NOTE:` now records that and says a ninth caller is where the extraction should actually happen.

**Documentation.** A new paragraph in AGENTS.md's "Build and test", beside the parser-corpus, form-document-corpus and derived-form-goldens paragraphs; and the same paragraph's claim that "nothing else in the suite is skipped" amended, because the three Inconclusive samples are now also reported as Skipped by VSTest. The WS3 ticket-table row for `test-samples-corpus` in `docs/modernization-plan.md` carries a **Done** summary in the shape its siblings use.

**No `docs/migration-notes.md` entry.** Checked against the finished diff: everything changed is in the `DAE.Server.Tests` assembly, which `tools/layout/Layout.csproj` does not ship and nothing outside the suite references. No D4 semantics, class name, assembly name, file format, error code, WCF contract, port or on-disk layout is touched.

## What the pass actually checks, and what it deliberately does not

The derivation check is **shallow by design**: `Frontend.Derive` must return a non-empty string, that string must parse as XML, and its root element must be `<report>` for the `Report` page type and `<interface>` for the other seven. There is no golden comparison, because `test-frontend-derivation-golden` already compares every derived form of every surface any sample defines against a checked-in file, and a second set of expected documents would be a second set to keep in step. What this pass adds instead is **discovery** (a sample library added to `samples/` becomes a case with no code change; the golden fixture's library list is hand-written), **isolation** (one server per sample, so a sample that only registers because a sibling created something first fails here), and **the four samples the golden fixture does not carry at all**.

The `Report`/`interface` split is live rather than decorative: 16 of the 128 derivations take the report branch, so a mistake in either direction would fail 16 or 112 cases.

## The three recorded lists

Each is a name → reason dictionary, and each is asserted against the tree in both directions so that none can rot into dead text hiding a sample nobody looks at.

| List | Entries today | Fails the run when |
|---|---|---|
| `NeedsADatabaseServer` | `Sample.Forum`, `Sample.Pubs` | the sample is not discovered under `samples/`; or its requisite closure holds no SQL device library (`MSSQLDevice` or `PGSQLDevice`), so the entry's premise has gone |
| `DefinesNoTableVariable` | `Sample.Components`, `Sample.Demo`, `Sample.Rental`, `Sample.ScriptAction` | the sample is not discovered; or it now exposes a surface — in which case the message also says the library must be added to `FrontendDerivationGoldenTest.CorpusLibraries` in the same change, or the new surface has no golden |
| `NotInTheRuntimeLayout` | `Sample.Shipping` | the sample is not discovered; or the layout now *does* hold its folder, so the entry must go and the sample becomes an ordinary case |

A sample in none of the three must register, enumerate and derive. A sample in more than one fails as well.

The two directions are split across two cases for a reason worth knowing before reviewing it: "it now exposes a surface" needs a registered server to answer, so it lives in the per-sample case (`CheckTheSurfacesAgreeWithTheList`, which also fails a sample with *no* entry that exposes no surface). Everything answerable from files alone — every entry names a real sample, the layout premise, the SQL-device premise, no sample in two lists — is in the one server-free `EverySampleIsAccountedFor`.

## Tests added

Per this project's testing rule, this whole ticket *is* a characterization test for the sample libraries, so the "tests" are the pass plus the two guards that keep it honest. Three cases, nothing more:

| Case | What it verifies |
|---|---|
| `TheSampleRegistersAndDerives(sample)`, one per discovered sample (12 today) | the sample registers on a fresh server carrying only its own requisite closure; the surfaces it then defines agree with the `DefinesNoTableVariable` list in both directions; and every page type of every surface derives into a parseable form with the right root element. A sample in `NotInTheRuntimeLayout` or `NeedsADatabaseServer` reports Inconclusive with its reason and starts no server |
| `EverySampleIsAccountedFor` | the three lists and the tree agree in every direction answerable without a server (above) |
| `TheSamplesDirectoryHoldsLibraries` | discovery found at least one library, so a renamed or moved `samples/` cannot turn the pass into one green case covering nothing |

Deliberately **not** added, and a reviewer should not add them: a test for `CorpusLibrary`'s property getters, for the `.d4l` parsing in isolation, or for the closure walk — all three are exercised by every per-sample case and would fail loudly if wrong (a wrong closure means a server missing a library, which means registration fails) — and no golden comparison.

## How to validate

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --logger "console;verbosity=normal" -- "NUnit.Where=class == Alphora.Dataphor.DAE.Server.Tests.SamplesCorpusTest"
```

Expect 11 passed, 3 skipped (`Sample.Forum`, `Sample.Pubs`, `Sample.Shipping`, each printing its reason), in about 38 s.

Worth doing by hand to see the guards bite, since none of them can be proved by a passing run:

- Delete the `Sample.Rental` entry from `DefinesNoTableVariable` → `Sample.Rental` fails saying it defines no table variable and asking for an entry.
- Add a made-up entry (say `Sample.Nonesuch`) to any of the three lists → `EverySampleIsAccountedFor` fails saying the entry names no library folder under `samples/`.
- Delete the `Sample.Shipping` entry from `NotInTheRuntimeLayout` → two failures: the sample's own case throws from the closure walk (no folder in the layout), and `EverySampleIsAccountedFor` says nothing explains the missing folder.
- Rename `samples/` → `TheSamplesDirectoryHoldsLibraries` fails and every per-sample case disappears, which is exactly what that case exists to make visible.

## The whole suite

Built from `Dataphor.slnx` in Debug, then the three parts the root `AGENTS.md` gives, each as its own foreground command:

- part 1 (everything but the coverage scripts): **576 passed, 0 failed** in `DAE.Server.Tests` in 6 m 9 s, and 13 passed in `Common.Tests`. Was 565 passed before this ticket; the 11 new passes are this pass's 9 registering samples plus its two guard cases.
- part 2 (coverage scripts A–R): **67 passed, 11 skipped, 0 failed**, 3 m 18 s.
- part 3 (the rest): **50 passed, 12 skipped, 0 failed**, 3 m 8 s.

No pre-existing failures were met, so `tickets/.pre-existing-error.md` was not written.

Honest about the sequencing: parts 2 and 3 were run on the build one edit earlier than the final tree. That last edit extracted the derivation loop out of the test method into `DeriveEverySurface` with no change to any assertion, and it touches only `SamplesCorpusTest.cs`, which the coverage-script parts do not run; part 1 and the fixture itself were both re-run green on the final tree.

## Measurements

Debug, one slow development machine, no SQL Server installed or reachable, 2026-11-22. Per-sample case durations as `--logger "console;verbosity=normal"` reports them:

| Sample | Surfaces | Case |
|---|---:|---|
| `Sample.Common` | 1 | 5 s |
| `Sample.Components` | 0 | 3 s |
| `Sample.Demo` | 0 | 4 s |
| `Sample.DetailAdd` | 3 | 3 s |
| `Sample.Forms` | 1 | 3 s |
| `Sample.IssueTracker` | 9 | 3 s |
| `Sample.Rental` | 0 | 4 s |
| `Sample.ScriptAction` | 0 | 3 s |
| `Sample.SendEmail` | 2 | 4 s |
| **the pass, whole fixture** | **16** | **38 s** (37.9 s and 39 s over two runs) |

So 16 surfaces × 8 page types = **128 derivations**, and the cost is dominated by server start (3–5 s of each case), as the plan ticket measured. The pass came in a little under the plan's estimate of about 42 s.

Part 1 went from 5 m 35 s (AGENTS.md's re-measured best, 2026-11-21) to 6 m 1 s and 6 m 9 s over two runs here — about the 38 s this pass costs, and still inside the 8-minute guidance that paragraph gives. **Read those as one machine on one afternoon, not as the part's cost**: AGENTS.md records part 1 ranging from 5 m 20 s to 7 m 32 s over several runs of the same build.

The 54.6 s that decision 4 buys back (33.6 s for `Sample.Forum` and 21.0 s for `Sample.Pubs`, each the SQL Server driver's connect timeout against a server that is not there) was **not re-measured**; it is the plan ticket's figure, recorded in the `NOTE:` at the site as the reason for the tradeoff.

## Known gaps, and things a reviewer should weigh

Listed because this is a starting point, not a finish line.

- **The two SQL Server samples are not attempted at all.** This is decision 4 of the plan ticket and it is recorded as an accepted-tradeoff `NOTE:` at the `NeedsADatabaseServer` site with its revisit condition (`test-device-live`, which owns running anything against a real database server and the name of the variable that selects one). The cost is stated there: a regression that breaks those two samples' registration in some *other* way is invisible here. Not a finding to re-file.
- **Converting `Sample.Shipping` is unowned.** Its C# project is not converted, so the build leaves its library folder out of the runtime layout; `docs/modernization-plan.md`'s runtime-layout paragraph records that exclusion and names *this* slug for it, which becomes a pointer to an archived ticket once this lands. The other three exclusions in that sentence name live tickets (`device-pgsql-port`, `device-connection-cleanup`). **A reviewer should decide** whether to file a `backlog/` ticket for the conversion and re-point that sentence at it, or leave it as is; this ticket deliberately did not invent an owner. Note also that converting it does not make the sample an ordinary case: its `.d4l` requires `MSSQLDevice` and carries `'ServerName'='.'`, so its entry moves from `NotInTheRuntimeLayout` to `NeedsADatabaseServer`.
- **Three undeletable temporary folders per run are expected, not a leak.** The run prints three lines like `Could not delete …corpus-Sample.Common-1; the next test run deletes it. Access to the path 'Alphora.Dataphor.Frontend.Server.dll' is denied.` A library assembly stays mapped until the process exits, so the first server to load each of `Alphora.Dataphor.Frontend.Server.dll`, `Alphora.Dataphor.DAE.Connection.dll` and `System.Internet.dll` cannot delete its folder. `TestEnvironment` already expects this, reports it and leaves the folder for the next run's sweep. The count does not grow with the number of samples: only the *first* loader of each assembly is affected, which is why it is three and not nine.
- **`TestServerInstance`'s two overloads have a silent preference.** An argument of static type `string[]` binds to the `params string[] libraryNames` constructor, not to the new `IEnumerable<string> libraryDirectories` one — the compiler prefers the exact array match. So a future caller holding an array of *folders* would have them read as layout library names. There is a `NOTE:` at the site saying so, and the failure is loud (`The runtime layout has no library "C:\…"`), but a reviewer who dislikes the shape could make the new one a named static factory instead. The plan ticket asked for a constructor.
- **`FrontendFixture.FlattenMessages` is now public rather than extracted.** Its `NOTE:` asks for one shared helper when an eighth caller appears. This pass is that eighth caller and reuses the existing copy rather than adding a ninth; draining the other six into a shared helper is a wider refactor than this ticket's scope and the `NOTE:` now says a ninth caller is where to do it. A reviewer may judge the extraction worth doing now.
- **No negative test for the pass itself.** The four by-hand checks under *How to validate* are the honest substitute; writing them as tests would mean a test that mutates the lists, which is worse than a reviewer running them once.
- **The closure walk is not cross-checked against the engine's own.** `CorpusLibrary.RequisiteClosure` reads `.d4l` files; the server resolves requisites itself during `EnsureLibraryRegistered`. Nothing asserts the two agree directly, because a closure that is wrong produces a server missing a library and registration fails — which is the per-sample case failing. Worth knowing when reading the code, not worth a test.
