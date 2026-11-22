description: Design an L7 test that registers every sample library in the repository on a fresh server and derives its surfaces on the in-memory and Simple devices, so that a change that breaks the samples fails the test suite rather than being discovered when someone opens an application.
architecture: docs/modernization-plan.md#ws3-test-suite
prereq: test-frontend-derivation-golden
files: samples/, libraries/SimpleDevice/, tests/DAE.Server.Tests/TestServerInstance.cs
difficulty: medium
----
## What this is

L7 of the WS3 layer table: "Every sample library registers and derives on the in-memory and Simple devices; later, an external application corpus does the same (WS13)". The plan slug is `test-samples-corpus`; it becomes one or more implement tickets once the shape is settled. It is also the base for `compat-corpus-harness` in `feat-ws13-application-compat`, which later points the same harness at an external corpus directory.

The samples at filing are 11 directories under `samples/`: `Sample.Common`, `Sample.Components`, `Sample.Demo`, `Sample.DetailAdd`, `Sample.Forms`, `Sample.Forum`, `Sample.IssueTracker`, `Sample.Pubs`, `Sample.Rental`, `Sample.ScriptAction`, `Sample.SendEmail`, `Sample.Shipping`. `Sample.Shipping` is parked out of the runtime layout (its C# assembly is not built; the modernization plan's library table carries this exception).

## Open questions this plan must settle

1. **In-memory device variant.** Some samples target a SQL device by `.d4` script (Pubs, IssueTracker, Rental likely). The plan says "every sample registers and derives on the in-memory and Simple devices". Decide for each sample whether it:
   - already targets the in-memory or Simple device,
   - needs a SimpleDevice variant written (the plan says "where practical"), or
   - must be excluded with a reason.
   Produce the per-sample verdict in the implement ticket; route anything controversial to `blocked/`.
2. **What "derives" means for a sample.** Reuse the derivation harness that `test-frontend-derivation-golden` leaves behind: for every surface the sample exposes (operator, table, view), derive each supported page type and check it produces XML. No golden comparison here — the depth of coverage is "every surface produces a form". `test-frontend-derivation-golden` holds the golden comparison for the Frontend sample set, if the two overlap the implement ticket must say so.
3. **Which surfaces to enumerate.** The surfaces come from the catalog after the sample registers. Enumerate through the catalog device's tables (`DAEOperators`, `DAETables`, `DAEViews` filtered to the sample's library). Settle the exact query and the include/exclude list (e.g. system operators the sample inherits but did not define). Name it in the implement ticket.
4. **Corpus parameter.** Design the harness with a parameter so `compat-corpus-harness` can later point at an external directory. A single method that takes a list of library directories and returns a set of test cases, called twice — once with `samples/`, once (in WS13) with an environment-variable path. Settle the signature and the discovery rule now.
5. **Isolation.** One `TestServerInstance` per sample (otherwise the second sample's register may collide with the first's objects). Confirm the ~0.65–0.85 s per-script cost from `test-harness-inprocess` carries over, so the suite stays under ~30 s for 10 samples × (register + enumerate + derive). If it balloons, reuse one server per fixture with per-sample server resets instead.

## Expected output

One `implement/` ticket, prerequisite on `test-frontend-derivation-golden` (so it inherits the fixture and the derivation plumbing). The implement ticket enumerates the samples, says what counts as "derived" per the Open questions above, and lists the excluded samples with reasons.

## Edge cases & interactions

- **Sample.Shipping.** Parked until a port ticket (`library-shipping-port` or its successor) lands. Exclude with a one-line reason; name it in the handoff so a re-cut after that ticket lands can flip the exclusion.
- **Samples that reference SQL Server.** These must produce a visible Inconclusive (not a Failure) when no SQL Server device is configured, matching the plan's "skipped visibly, reported as inconclusive, counted" rule for live tests. Count how many samples fall into this bucket during planning.
- **Sample upgrade scripts.** `Sample.Demo/Upgrades/` carries an upgrade. The test should run the upgrade after registration, or skip it with a note.
- **`compat-corpus-harness` reuse (WS13).** The harness method taking a library-directory list is the explicit API this ticket owes `compat-corpus-harness`. Keep it narrow (no reporting formats baked in beyond NUnit's Inconclusive/Failed).
- **Frontend dependency.** Every sample needs `Frontend` registered first. The fixture the parent plan ticket `test-frontend-derivation-golden` builds already does this.
- **Culture.** The invariant culture of the test host is already pinned by the harness; samples that write culture-sensitive strings into forms rely on it.

## Measured while planning `test-frontend-derivation-golden`

Appended 2026-11-21 from a throwaway probe in `tests/DAE.Server.Tests` (Debug, development machine; output in `tickets/.logs/test-frontend-derivation-golden.probe*.log` while that directory's pruner keeps it). This answers parts of open questions 1, 3 and 5 for free; take it as given rather than re-measuring.

- **Thirteen libraries register on a server with no SQL Server**, in this order: `System.Platform`, `SimpleDevice`, `Frontend`, `System.Internet`, `Security`, `Sample.Common`, `Sample.DetailAdd`, `Sample.Forms`, `Sample.Rental`, `Sample.IssueTracker`, `Sample.Components`, `Sample.ScriptAction`, `Sample.SendEmail`. Note the order: `Frontend` must come before `System.Internet`, because `System.Internet`'s registration script calls the `Frontend.ExecuteScript` operator without declaring the dependency (`backlog/bug-system-internet-registration-needs-frontend`).
- **`Sample.IssueTracker` needs no SQL Server.** Its `.d4l` references only `Sample.Common`, which uses `SimpleDevice`; it registers and exposes 9 table variables. The plan's WS4 note lists `Sample.Demo`, `Sample.Forum`, `Sample.Pubs` and `Sample.Shipping` as the SQL Server set, and that holds: those four reference `MSSQLDevice`, the rest do not.
- **The surface-enumeration query that works** (open question 3): `TableVars where Library_Name = '<library>' and not(IsGenerated) over { Name } order by { Name }`. `TableVars` covers base tables and views in one go, and `IsGenerated` filters the ones the engine creates for application transactions and device reconciliation. Surfaces found: `Frontend` 11, `Security` 18, `Sample.Common` 1, `Sample.DetailAdd` 3, `Sample.Forms` 1, `Sample.IssueTracker` 9, `Sample.SendEmail` 2 — 45 in all.
- **Three samples expose no table variables at all** after registration and so have nothing to derive: `Sample.Components`, `Sample.ScriptAction` and `Sample.Rental`. The first two are document-and-script libraries by design. `Sample.Rental` is the interesting one — see below.
- **Cost** (open question 5): registering those libraries on each of two independent servers, running 720 derivations and loading the whole repository's customized-form corpus took **18.1 s** of one `dotnet test` run. Per-sample server isolation is affordable; no need to fall back on server resets.
- **The fixture already exists.** `InProcessTestFixture` in `tests/DAE.Server.Tests` takes a list of library folders to copy and a list to register. `test-frontend-document-corpus` adds the Frontend sister fixture on top of it; this ticket's harness should take the same route rather than building a third.

### `Sample.Rental` cannot be registered into a usable state

`samples/Sample.Rental/Documents/Register.d4` is an **empty file**. The tutorial's device, tables, views and operators are created by running the scripts in its `Documents` folder by hand, in the order `RegisterCompleted.d4` lists. Two things go wrong when a test tries to do that:

- Running `RegisterCompleted.d4` as one batch fails at its seventh step, `MovieActorTable`, with "The table Movie does not have a row with ID (1)." and the enclosing implicit transaction rolls the whole thing back. Running its 17 `ExecuteScript` statements as separate batches (what Dataphoria does) gets 11 through and still fails 6, beginning at the same step; the five later failures are all consequences (`Unknown identifier "Video"`, `"RentedVideo"`, `"VideoDetail"`).
- The objects the successful steps *do* create are not in the `Sample.Rental` library: after those 11 steps `TableVars where Library_Name = 'Sample.Rental'` is still empty, although `Movie` plainly exists (that is what the failure message is about). They land in the session's current library, so they cannot be enumerated per library.

So this sample needs a verdict of its own under open question 1, and the "Movie does not have a row with ID (1)" failure needs diagnosing before any of it can be corpus — the generator-based identifier allocation in `Generators.d4` is the first place to look. `test-frontend-document-corpus` excludes `Sample.Rental`'s four customized form documents for this reason.

## TODO

- Walk the samples once: per sample, note the devices it uses, the surfaces it exposes and whether it has an upgrade script. Produce the per-sample verdict table; the section above already covers the ten that register, the surface query and the three with no surfaces.
- Diagnose `Sample.Rental`'s `RegisterCompleted` failure, or give it a documented verdict of "excluded, with this cause".
- Settle the harness signature shared with `compat-corpus-harness`.
- Settle the surface-enumeration query against the catalog.
- Emit the implement ticket keeping the slug `test-samples-corpus`.
- Delete this plan ticket on the way out.
