description: Add a test that starts a fresh server for each example application shipped in the repository, installs it, and builds every screen it can, so that a change which breaks one of the examples fails the test run instead of being found when somebody opens it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/SamplesCorpusTest.cs, tests/DAE.Server.Tests/CorpusLibrary.cs, tests/DAE.Server.Tests/TestEnvironment.cs, tests/DAE.Server.Tests/TestServerInstance.cs, tests/DAE.Server.Tests/FrontendDerivationGoldenTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, samples/, AGENTS.md, docs/modernization-plan.md
difficulty: medium
----
## What this is

Layer **L7** of the WS3 test-suite layer table: "Every sample library registers and derives on the in-memory and Simple devices; later, an external application corpus does the same (WS13)."

Vocabulary, for a reader without the ticket in front of them. A **library** is a folder holding a `.d4l` descriptor plus a `Documents/` folder of D4 scripts and form documents; the twelve under `samples/` are the example applications Dataphor ships. **Registering** a library is installing it into a running server: the server loads the library's assemblies, registers the libraries it requires, and then runs its `Documents/Register.d4` with the current library set to it, so the catalog objects that script creates belong to it. A **surface** is a table variable (a base table or a view) that a library defines. **Deriving** is the Frontend server library turning a surface into a form; the entry point is the D4 operator `Frontend.Derive(<expression>, <page type>)`, which returns the form as one line of XML. A **page type** is the kind of form to build (`Browse`, `Edit`, `Report`, …).

This pass gives each sample library a server of its own, carrying only the libraries that sample actually requires, registers it there, enumerates its surfaces from the catalog and derives every page type of each. It is **not** a second golden set: `test-frontend-derivation-golden` (landed) already compares every derived form of the sample libraries its fixture carries against a checked-in file, and - measured below - those files already cover *every* surface any sample defines today. What this pass adds that the golden pass cannot:

- **Discovery.** The libraries are found by walking a directory, so a sample library added to `samples/` becomes a test case with no code change. The golden pass's library list is hand-written, and its own handoff says so: "a table added to a carried library turns up as new cases, a whole new sample library does not. `test-samples-corpus` (plan, L7) owns that breadth."
- **Isolation.** The golden pass registers ten libraries on one server, so a sample that only registers because a sibling happened to create something first still passes. Here each sample gets a fresh server carrying its own requisite closure and nothing else.
- **The four samples the golden fixture does not carry at all** - `Sample.Components`, `Sample.Demo`, `Sample.Rental`, `Sample.ScriptAction`. None defines a surface, so they add no derivation, but registering them is itself the thing under test, and `Sample.Demo` is the only test anywhere that registers `MSSQLDevice` (with `Connection` and `SQLDevice` behind it).
- **The harness** the WS13 work item `compat-corpus-harness` (listed in `tickets/backlog/feat-ws13-application-compat.md`) will point at a directory outside this repository.

So the derivation check here is deliberately **shallow**: a form must be produced and must parse as XML with the right root element. Depth is the golden pass's job, and duplicating it would mean a second set of files to keep in step.

## Measured while planning

Everything in this section was measured on 2026-11-22 by a throwaway probe in `tests/DAE.Server.Tests` (Debug, development machine, no SQL Server installed or reachable); the logs are in `tickets/.logs/test-samples-corpus.probe-*.log` for as long as that directory's pruner keeps them. **Take these as given rather than re-measuring** - but if a number you meet disagrees, the measurement is wrong and the ticket should be corrected, not the test bent to fit.

### The twelve samples, and what each one does

| Sample | Runtime layout has it | Requisite closure, measured | Registers with no SQL Server | Surfaces | Verdict |
|---|---|---|---|---:|---|
| `Sample.Common` | yes | SimpleDevice, Frontend | yes | 1 | in the corpus |
| `Sample.Components` | yes | SimpleDevice, Frontend | yes | 0 | in the corpus; defines no table variable |
| `Sample.Demo` | yes | SimpleDevice, Frontend, Sample.Common, Connection, SQLDevice, MSSQLDevice | yes | 0 | in the corpus; defines no table variable |
| `Sample.DetailAdd` | yes | SimpleDevice, Frontend | yes | 3 | in the corpus |
| `Sample.Forms` | yes | SimpleDevice, Frontend | yes | 1 | in the corpus |
| `Sample.Forum` | yes | SimpleDevice, Frontend, Connection, SQLDevice, MSSQLDevice, Security | **no** | — | needs a database server |
| `Sample.IssueTracker` | yes | SimpleDevice, Frontend, Sample.Common | yes | 9 | in the corpus |
| `Sample.Pubs` | yes | Connection, SQLDevice, MSSQLDevice, SimpleDevice, Frontend | **no** | — | needs a database server |
| `Sample.Rental` | yes | SimpleDevice, Frontend | yes | 0 | in the corpus; defines no table variable |
| `Sample.ScriptAction` | yes | SimpleDevice, Frontend | yes | 0 | in the corpus; defines no table variable |
| `Sample.SendEmail` | yes | System.Internet, SimpleDevice, Frontend | yes | 2 | in the corpus |
| `Sample.Shipping` | **no** | — | — | — | not in the runtime layout |

So: **nine samples register with no database server**, two need one, and one is not in the layout. The nine define **16 surfaces in total**, which crossed with the eight page types the engine builds is **128 derivations**. `Sample.Common`'s single surface is `Generators`, the generator table its `Register.d4` creates by calling `EnsureGenerators()`.

The requisite closure is read from the `.d4l` files, not hard-coded. Two things in it are worth knowing: `Frontend` requires `SimpleDevice`, which is why every closure contains it, and `MSSQLDevice` requires `SQLDevice` and `Connection`, which is why `Sample.Demo`'s closure has six entries.

### The derivation golden set already covers all 16 surfaces

`tests/goldens/frontend/` holds 8 files for `Sample.Common`, 24 for `Sample.DetailAdd`, 8 for `Sample.Forms`, 72 for `Sample.IssueTracker` and 16 for `Sample.SendEmail` - 1 + 3 + 1 + 9 + 2 = 16 surfaces, exactly the 16 the probe found across all twelve samples. This is why the check here is shallow; see *What this is*.

### Registering produces no surfaces at all for four samples

`Sample.Components`, `Sample.Demo`, `Sample.Rental` and `Sample.ScriptAction` expose no table variable after registration. Each has its own reason, and none is a defect:

- **`Sample.Components`** and **`Sample.ScriptAction`** are libraries of form documents and scripts by design; neither has any `create table` to run.
- **`Sample.Demo`**'s `Documents/Register.d4` runs only `Documents/Base.d4`, which creates types and operators and no table. Its tables, views, events, constraints and data are in six further scripts that `Register.d4` lists **in a comment** for the reader to run by hand, or in `Documents/Device.d4`, which creates a SQL Server device first. That is how the sample is written.
- **`Sample.Rental`**'s `Documents/Register.d4` is a **zero-byte file**. The tutorial's schema is created by running the scripts in its `Documents/` folder in the order `Documents/RegisterCompleted.d4` lists, which is a thing the reader does from the IDE. Seven of those eighteen steps currently fail; see *Edge cases* below and `tickets/backlog/bug-table-assignment-evaluates-source-twice.md`, which owns the cause. None of that affects this pass, which only registers the library.

### `Report` derives a `<report>` root, every other page type an `<interface>` root

All eight page types derive for all 16 surfaces, but the `Report` page's document root is `<report>`, not `<interface>`. Confirmed against the checked-in goldens (`tests/goldens/frontend/Sample.Forms/Customer.Report.xml` opens `<report …>`, `Customer.Browse.xml` opens `<interface …>`). A shallow check that insists on `<interface>` for every page type fails 16 of 128 cases; this is the one piece of per-page-type knowledge the pass needs.

### Cost

| What | Measured |
|---|---|
| One sample: fresh server, start, register the closure, enumerate, derive every page type | 3.8–6.3 s, of which 3.2–4.9 s is the server start |
| The nine samples that register | **about 42 s in total** |
| `Sample.Forum`, failing to reach SQL Server | **33.6 s** |
| `Sample.Pubs`, failing to reach SQL Server | **21.0 s** |

The cost is dominated by server start, so per-sample isolation is affordable: nine servers cost about 42 s, which takes part 1 of the three-part suite run from the 5 m 35 s AGENTS.md records to roughly 6 m 17 s - still inside the 8-minute guidance that same paragraph gives, with no need to reuse one server or fall back on server resets.

The two SQL Server samples are a different matter: **54.6 s, more than everything else put together, to learn something already known** - the driver's connect timeout, twice over, against a server that is not there. `Sample.Pubs`'s device names a developer's machine (`MYNOCK\MYNOCK`) that will never resolve anywhere, and the samples may not be edited to shorten the timeout. Hence the decision below.

## Settled decisions

These were the plan ticket's open questions. They are settled; do not reopen them, and do not hand an implementer a choice.

**1. Which samples are in the corpus.** The nine in the table above that register with no database server. `Sample.Forum` and `Sample.Pubs` need one; `Sample.Shipping` is not in the runtime layout.

**2. What "derives" means here.** For each surface, for each of the eight page types in `FrontendDerivationGoldenTest.PageTypes`: `Frontend.Derive` returns a non-empty string, that string parses as XML, and its root element is `<report>` for the `Report` page type and `<interface>` for the other seven. No golden comparison - the golden pass owns that, and already covers all 16 surfaces.

**3. How surfaces are enumerated.** Reuse `FrontendDerivationGoldenTest.EnumerateSurfaces(IServerProcess, string libraryName)` unchanged. It runs `TableVars where Library_Name = '<library>' and not(IsGenerated) over { Name } order by { Name }`, which covers base tables and views in one query and leaves out the table variables the engine generates for application transactions and device reconciliation. Its own remark already says it takes the library name as an argument "so that a corpus of application libraries outside this repository (`compat-corpus-harness`) adds a caller rather than a second query" - this ticket is the first such caller. Move it to the shared helper described below if that reads better, but do not write a second query.

**4. The samples that need a database server are reported Inconclusive without being attempted.** Each is a recorded entry carrying the sample's name and the reason; the case calls `Assert.Inconclusive` with that reason and the slug that owns making it runnable, and starts no server. This is an **accepted tradeoff, recorded as a `NOTE:` at the site with its revisit condition**: it buys back the 54.6 s measured above, and the cost is that a regression which breaks those two samples' registration in some *other* way is invisible here. What makes that acceptable: their devices cannot connect from any machine as written, every `.d4` in the tree is already parsed by `ParserRoundTripCorpusTest`, and the WS7/WS3 work item `test-device-live` (listed in `tickets/backlog/feat-ws7-devices-and-libraries.md`) owns running anything against a real SQL Server. Revisit when that lands: it adds the live arm, and these entries become "attempt when a server is configured".

Do not invent an environment variable for this. `test-device-live` owns the name of the variable that selects a live database server, and guessing it now would have to be renamed.

**5. Isolation: one fresh server per sample.** Measured affordable (above). Each server carries the sample's requisite closure and the sample, and nothing else. Do not wrap the pass in `TestEnvironment.WithTheObjectIDCounterRestored`: that guard is for code that starts a *second* engine while one is already running, and these servers run one at a time, disposed before the next - the coverage-script pass already starts 136 sequential servers without it.

**6. The harness API, which `compat-corpus-harness` inherits.** A corpus is a directory holding library folders. Discovery is one method over a root directory; nothing about samples, the repository or NUnit is baked into it.

```csharp
/// <summary>One library folder a corpus pass registers: what the server will know it as, where to copy it from, and what it requires.</summary>
public sealed class CorpusLibrary
{
    /// <summary>The folder's name, which is the library name the server uses.</summary>
    public string Name { get; }

    /// <summary>The folder to copy into a test server's library directory.</summary>
    public string Directory { get; }

    /// <summary>The library names this library's .d4l requires, in the order it lists them.</summary>
    public IReadOnlyList<string> Requisites { get; }
}

/// <summary>
/// Every library folder directly under <paramref name="rootDirectory"/>: a subfolder holding a .d4l file named after it.
/// Ordered by name, so a run's case order does not depend on the file system. A subfolder with no such file is not a
/// library and is skipped. The one discovery rule, shared with the WS13 external-corpus pass, which passes its own root.
/// </summary>
public static IReadOnlyList<CorpusLibrary> DiscoverLibraries(string rootDirectory)
```

Three further notes on the shape, each settled:

- **Discovery root for this pass is the repository's `samples/`** (through `TestEnvironment.RepositoryDirectory`), not the runtime layout - the layout mixes the shipped, sample and test libraries in one folder and leaves out the unconverted ones, so it can neither name the sample set nor notice `Sample.Shipping`'s absence. The **copy source** is still the layout's `Libraries/<Name>` folder, as it is for every other fixture, because the layout is what the build assembles and what the product ships. For an external corpus the discovered folder is both.
- **The requisite closure** is the transitive closure of `Requisites` over the `.d4l` files in the layout, depth first, each library after the ones it requires. `System` and `General` have no folder of their own (`System` is built in; the server creates `General`) and are skipped rather than reported missing. Registering is then: `EnsureLibraryRegistered('Frontend')` first when `Frontend` is in the closure, then `EnsureLibraryRegistered('<sample>')`, which pulls the rest in itself. `Frontend` must go first because `System.Internet`'s registration script calls the `Frontend.ExecuteScript` operator without declaring the dependency - the same defect `FrontendFixture` works around, tracked by `tickets/backlog/bug-system-internet-registration-needs-frontend.md`; name that slug at the site and drop the hoist when it lands.
- **`TestServerInstance` needs to copy a folder by path, not only by name.** Add `TestEnvironment.CopyLibraryFolders(string targetDirectory, IEnumerable<string> sourceDirectories)` and make the existing `CopyLibraries(target, names)` a thin wrapper that maps each name to `Libraries/<name>` and calls it, so no existing fixture changes behaviour; then give `TestServerInstance` a constructor taking source directories beside the one taking names. This is the piece `compat-corpus-harness` cannot add for itself without reaching into the layout, and it is the only product-adjacent change in the ticket.

**7. One NUnit case per sample, from a `TestCaseSource`.** Discovery reads directories and needs no server, so it can run at NUnit's discovery time - which the golden pass could not do, because its case list depends on a catalog. One case per sample is what makes a single sample's Inconclusive or failure legible, and is what the plan's "skipped visibly, reported as inconclusive, counted" rule asks for. The fixture starts its server inside the case, not in `OneTimeSetUp`, because each case needs a different one.

**8. Two recorded lists, each asserted against the tree in both directions.** This is the discipline the sibling passes use (`FormCustomizationTest.EveryCustomizationIsAccountedFor`, the golden pass's expected-failure table) and the reason this pass cannot rot quietly:

| List | An entry says | Fails the run when |
|---|---|---|
| needs a database server | this sample's registration needs a SQL Server, with the reason | the sample is not discovered; or its requisite closure contains no SQL device library (`MSSQLDevice` today, `PGSQLDevice` later), so the entry's premise has gone |
| defines no table variable | this sample exposes no surface, with the reason | the sample is not discovered; or it now exposes a surface, so the entry must be deleted - **and its library added to `FrontendDerivationGoldenTest.CorpusLibraries` in the same change**, or that new surface has no golden |
| not in the runtime layout | the build does not assemble this library's folder, with the reason | the layout does have the folder, so the entry must be deleted and the sample becomes an ordinary case |

The third list has exactly one entry today, `Sample.Shipping`, whose C# project is not converted (the modernization plan's runtime-layout paragraph records the exclusion and names this slug for it). A sample discovered under `samples/` that is in none of the three lists is an ordinary case and must register, enumerate and derive.

## Edge cases & interactions

Each case below says how it is verified: by a test, by the two-way list agreement, or by inspection.

- **A sample added to `samples/` becomes a case with no code change.** Verified by construction (discovery walks the directory) and by a test that the walk found more than zero libraries, so a renamed or moved folder cannot turn the corpus silently into an empty pass.
- **A sample whose `.d4l` is named differently from its folder** would be discovered as nothing at all, because the server also requires folder name and library name to agree (`Schema.Library.GetLibraryDirectory` is the library directory plus the name). Verified by inspection; discovery's rule is the server's rule, so there is nothing to diverge.
- **A sample that registers only because a sibling created something first** is what per-sample isolation catches. Verified by construction: each server carries one sample's closure.
- **`Sample.Shipping`.** Discovered, not in the layout, Inconclusive with the reason. Verified by the two-way list agreement: when its C# project is converted and the layout gains the folder, the entry fails and says to delete it. Nothing else in the tree owns that conversion today - the plan's runtime-layout paragraph names *this* slug - so the implement handoff must say plainly that converting `Sample.Shipping` is still unowned, and that even once converted the sample needs a SQL Server (its `.d4l` requires `MSSQLDevice` and carries `'ServerName'='.'`), so it moves from the third list to the first rather than becoming an ordinary case.
- **`Sample.Demo`'s upgrade script.** `samples/Sample.Demo/Upgrades/Upgrade.2.0.1.d4` is the only `Upgrades/` folder under `samples/`, and it contains one statement: `alter table Contact alter tags { create Test = "Test" }`. `Contact` is created by `Documents/Tables.d4`, which registration does not run, so there is nothing for the upgrade to alter on a freshly registered server and running it here would only assert that it fails. **Do not run it.** The WS7 work item `library-upgrade-scripts` (listed in `tickets/backlog/feat-ws7-devices-and-libraries.md`) owns establishing what runs `Upgrades/Upgrade.<version>.d4` at all, and `tickets/plan/debt-library-upgrades-coverage.md` owns testing the upgrade machinery. Record the skip as a `NOTE:` naming both, so the next reader does not re-derive this.
- **A surface that stops deriving.** The case fails, naming the sample, the surface and the page type, with the engine's message. There is deliberately no expected-failure table here: the golden pass has one and it is empty, so a derivation defect belongs there where it is recorded against a ticket and a required error text.
- **Many servers in one run leave undeletable temporary folders.** Measured: of eleven probe servers, three could not delete their folder, each because the process had loaded a library assembly from it (`Alphora.Dataphor.Frontend.Server.dll`, `Alphora.Dataphor.DAE.Connection.dll`, `System.Internet.dll`) and a loaded assembly stays mapped until the process exits. `TestEnvironment` already expects this, reports it and leaves the folder for the next run's sweep; only the first server to load each assembly is affected, so the count does not grow with the number of samples. Verified by inspection of `TestEnvironment.TryDeleteDirectory` and by the probe. Nothing to do; worth one sentence in the handoff so a reviewer seeing three such lines does not read them as a leak.
- **Culture.** The test host's invariant culture is already pinned by the harness; samples that write culture-sensitive strings into forms rely on it. No change.
- **Concurrency.** The fixture must not be `[Parallelizable]`: two engines in one process share the engine's static state, as the `NOTE:` on `TestServerInstance` records. Verified by inspection; say so in a comment, as `FrontendFixture` does.
- **`compat-corpus-harness` reuse.** The API in decision 6 is the whole of what this ticket owes WS13. Keep it that narrow: no report format beyond NUnit's own Passed/Failed/Inconclusive, no environment variable, no knowledge of `samples/` inside `DiscoverLibraries`.

## Tests

Per this project's testing rule, a ticket that ports or re-hosts a subsystem owes a characterization test for it - and that is what the whole of this ticket is, so the "tests" here are the pass itself plus the guards that keep it honest. Nothing more:

- **One case per discovered sample** - the deliverable. Registers, enumerates, derives, or reports the recorded Inconclusive.
- **One case that the three lists and the tree agree**, in both directions, as the table in decision 8 sets out. This is one case, not three: the agreement is a single statement about the whole corpus, and a case that needed its siblings to have run first would give the wrong answer under the name filters AGENTS.md runs the suite with.
- **One case that discovery found at least one library**, so a moved or renamed `samples/` cannot make the pass vacuously green.

Do not add a test for `CorpusLibrary`'s property getters, for the `.d4l` parsing in isolation, or for the closure walk - the per-sample cases exercise all three and would fail loudly if any were wrong. Do not add a golden comparison.

## TODO

- Add `tests/DAE.Server.Tests/CorpusLibrary.cs`: the `CorpusLibrary` type and `DiscoverLibraries(string rootDirectory)` of decision 6, plus the requisite-closure walk over the layout's `.d4l` files.
- Add `TestEnvironment.CopyLibraryFolders(string targetDirectory, IEnumerable<string> sourceDirectories)`, make `CopyLibraries(target, names)` a wrapper over it, and give `TestServerInstance` a constructor taking source directories.
- Add `tests/DAE.Server.Tests/SamplesCorpusTest.cs`: the `TestCaseSource` over `samples/`, the per-sample case (fresh server, closure registration with `Frontend` hoisted, `EnsureLibraryRegistered`, enumerate, derive all eight page types, root-element check per decision 2), the three recorded lists with a reason each, the agreement case and the non-empty-walk case.
- Record the accepted tradeoff of decision 4 and the `Sample.Demo` upgrade skip as `NOTE:` comments at their sites, each with the slug that owns the follow-up and, for the tradeoff, its revisit condition.
- Add both new files to `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` (explicit `Compile` items; default items are off).
- Document the pass in AGENTS.md's "Build and test" section, beside the paragraphs for the parser corpus, the form-document corpus and the derived-form goldens: what it covers, the three lists, and that `samples/` is walked through `TestEnvironment.RepositoryDirectory` rather than a spelled path.
- Fill in the WS3 ticket-table row for `test-samples-corpus` in `docs/modernization-plan.md` with a **Done** summary, as the sibling rows carry: the twelve samples, the nine that register, the 16 surfaces and 128 derivations, the three lists, and the two measurements (about 42 s for the pass; 54.6 s saved by not attempting the SQL Server samples).
- No `docs/migration-notes.md` entry is expected: nothing an application, a deployment script or a C# extension can observe changes. `TestEnvironment` and `TestServerInstance` are test-assembly types. Check this holds once the diff exists, and add the entry if it does not.
- Build, then run the three part commands under "Agents run the suite in parts" in the root `AGENTS.md`, each as its own foreground command. Expect part 1 to grow by about 42 s.
- Hand off to `review/` with the measured per-sample timings, the part-1 figure, and the two items named above for the handoff: `Sample.Shipping`'s conversion being unowned, and the three undeletable temporary folders being expected.
